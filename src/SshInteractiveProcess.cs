using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace ProGo
{
    // Explicit user interaction only. No shell, redirected streams or passphrase API.
    // The user's keystrokes go directly to OpenSSH's own, separate Windows console.
    internal static class SshInteractiveProcess
    {
        private static void Native(bool success)
        { if (!success) throw new Win32Exception(Marshal.GetLastWin32Error()); }
        private static void Deadline(Stopwatch watch, int timeoutMs, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (watch.ElapsedMilliseconds >= timeoutMs) throw new TimeoutException("Interactive SSH deadline exceeded.");
        }
        private static IntPtr EnvironmentBlock(string agentEndpoint)
        {
            var values = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
                values[(string)entry.Key] = (string)entry.Value;
            // Never inherit an askpass override or fall back to a different agent
            // if the process environment changes after the user's selection.
            values.Remove("SSH_ASKPASS"); values.Remove("DISPLAY"); values.Remove("WAYLAND_DISPLAY");
            values["SSH_ASKPASS_REQUIRE"] = "never";
            values.Remove("SSH_AUTH_SOCK");
            if (!String.IsNullOrEmpty(agentEndpoint)) values["SSH_AUTH_SOCK"] = agentEndpoint;
            var block = new StringBuilder();
            foreach (var value in values) block.Append(value.Key).Append('=').Append(value.Value).Append('\0');
            block.Append('\0');
            return Marshal.StringToHGlobalUni(block.ToString());
        }
        private static void Settle(SafeFileHandle job)
        {
            Native(TerminateJobObject(job, 1));
            var watch = Stopwatch.StartNew();
            while (true) {
                Accounting info; uint returned;
                Native(QueryInformationJobObject(job, 1, out info, Marshal.SizeOf(typeof(Accounting)), out returned));
                if (info.ActiveProcesses == 0) return;
                if (watch.ElapsedMilliseconds >= 2000) throw new IOException("Interactive SSH cleanup could not be confirmed.");
                Thread.Sleep(10);
            }
        }
        // Call on a worker. started exposes only an owned PID, never console IO.
        internal static int Run(string executable, string arguments, string agentEndpoint,
            int timeoutMs, CancellationToken token, Action<int> started = null)
        {
            if (timeoutMs < 1 || timeoutMs > 300000) throw new ArgumentOutOfRangeException("timeoutMs");
            token.ThrowIfCancellationRequested();
            if (String.IsNullOrEmpty(executable) || !Path.IsPathRooted(executable) ||
                !String.Equals(Path.GetFullPath(executable), executable, StringComparison.OrdinalIgnoreCase) ||
                executable.IndexOf('\0') >= 0 || arguments == null || arguments.IndexOf('\0') >= 0 ||
                (agentEndpoint != null && agentEndpoint.IndexOf('\0') >= 0))
                throw new ArgumentException("Interactive SSH requires an absolute executable and valid arguments.");
            var watch = Stopwatch.StartNew();
            using (var job = CreateJobObject(IntPtr.Zero, null)) {
                if (job.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error());
                var limits = new ExtendedLimits(); limits.Basic.LimitFlags = 0x2000; // KILL_ON_JOB_CLOSE, no breakaway.
                Native(SetInformationJobObject(job, 9, ref limits, Marshal.SizeOf(typeof(ExtendedLimits))));
                SafeFileHandle process = null, thread = null;
                IntPtr environment = IntPtr.Zero; bool assigned = false, cleanupAttempted = false;
                try {
                    environment = EnvironmentBlock(agentEndpoint);
                    var startup = new Startup(); startup.Size = Marshal.SizeOf(typeof(Startup));
                    // No STARTF_USESTDHANDLES: Windows creates real console handles.
                    // No inheritable app handles, shell, PowerShell, stdin pipe or capture.
                    ProcessInfo child; Deadline(watch, timeoutMs, token);
                    Native(CreateProcess(executable, new StringBuilder(SshConnection.Quote(executable) + " " + arguments),
                        IntPtr.Zero, IntPtr.Zero, false, 0x414, environment, Path.GetDirectoryName(executable), ref startup, out child));
                    // NEW_CONSOLE | SUSPENDED | UNICODE_ENVIRONMENT. Contain before any child code.
                    process = new SafeFileHandle(child.Process, true); thread = new SafeFileHandle(child.Thread, true);
                    Native(AssignProcessToJobObject(job, process)); assigned = true;
                    Deadline(watch, timeoutMs, token);
                    if (ResumeThread(thread) == UInt32.MaxValue) throw new Win32Exception(Marshal.GetLastWin32Error());
                    if (started != null) started(unchecked((int)child.ProcessId));
                    while (true) {
                        Deadline(watch, timeoutMs, token);
                        uint wait = WaitForSingleObject(process, (uint)Math.Max(1, Math.Min(50, timeoutMs - watch.ElapsedMilliseconds)));
                        if (wait == 0) break;
                        if (wait != 258) throw new Win32Exception(Marshal.GetLastWin32Error());
                    }
                    uint code; Native(GetExitCodeProcess(process, out code));
                    cleanupAttempted = true; Settle(job);
                    Deadline(watch, timeoutMs, token);
                    return unchecked((int)code);
                } finally {
                    try {
                        if (assigned && !cleanupAttempted) Settle(job);
                        else if (!assigned && process != null) {
                            Native(TerminateProcess(process, 1));
                            if (WaitForSingleObject(process, 2000) != 0) throw new IOException("Suspended interactive SSH cleanup failed.");
                        }
                    } finally {
                        if (process != null) process.Dispose(); if (thread != null) thread.Dispose();
                        if (environment != IntPtr.Zero) Marshal.FreeHGlobal(environment);
                    }
                }
            }
        }
        [StructLayout(LayoutKind.Sequential)] private struct BasicLimits { internal long ProcessTime, JobTime; internal uint LimitFlags; internal UIntPtr MinimumWorkingSet, MaximumWorkingSet; internal uint ActiveProcessLimit; internal UIntPtr Affinity; internal uint PriorityClass, SchedulingClass; }
        [StructLayout(LayoutKind.Sequential)] private struct IoCounters { internal ulong ReadOperations, WriteOperations, OtherOperations, ReadBytes, WriteBytes, OtherBytes; }
        [StructLayout(LayoutKind.Sequential)] private struct ExtendedLimits { internal BasicLimits Basic; internal IoCounters Io; internal UIntPtr ProcessMemory, JobMemory, PeakProcessMemory, PeakJobMemory; }
        [StructLayout(LayoutKind.Sequential)] private struct Accounting { internal long UserTime, KernelTime, PeriodUserTime, PeriodKernelTime; internal uint PageFaults, TotalProcesses, ActiveProcesses, TerminatedProcesses; }
        [StructLayout(LayoutKind.Sequential)] private struct Startup { internal int Size; internal IntPtr Reserved, Desktop, Title; internal uint X, Y, Width, Height, XChars, YChars, Fill, Flags; internal ushort ShowWindow, ReservedSize; internal IntPtr ReservedData, Input, Output, Error; }
        [StructLayout(LayoutKind.Sequential)] private struct ProcessInfo { internal IntPtr Process, Thread; internal uint ProcessId, ThreadId; }
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern SafeFileHandle CreateJobObject(IntPtr security, string name);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool SetInformationJobObject(SafeFileHandle job, int type, ref ExtendedLimits info, int size);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool AssignProcessToJobObject(SafeFileHandle job, SafeFileHandle process);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool TerminateJobObject(SafeFileHandle job, uint code);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool QueryInformationJobObject(SafeFileHandle job, int type, out Accounting info, int size, out uint returned);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool CreateProcess(string app, StringBuilder command, IntPtr processSecurity, IntPtr threadSecurity, bool inherit, uint flags, IntPtr environment, string directory, ref Startup startup, out ProcessInfo info);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern uint ResumeThread(SafeFileHandle thread);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern uint WaitForSingleObject(SafeFileHandle handle, uint milliseconds);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool GetExitCodeProcess(SafeFileHandle process, out uint code);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool TerminateProcess(SafeFileHandle process, uint code);
    }
}

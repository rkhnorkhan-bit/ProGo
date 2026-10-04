using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace ProGo
{
    internal sealed class DiagnosticProcessResult
    {
        internal string Output, Error;
        internal int ExitCode;
        internal bool Truncated;
    }
    // Windows-only, short-lived diagnostics. Never use this owner for user terminals
    // or persistent SSH tunnels. The child is suspended until job containment succeeds.
    internal static class DiagnosticProcess
    {
        internal const int CaptureLimit = 128 * 1024;
        private sealed class Capture { internal string Text; internal bool Truncated; }
        private static Task<Capture> Drain(StreamReader reader)
        {
            // .NET Framework anonymous pipes are synchronous handles. Dedicated readers
            // avoid its BeginRead/EndRead fallback race and thread-pool starvation.
            var done = new TaskCompletionSource<Capture>(TaskCreationOptions.RunContinuationsAsynchronously);
            var worker = new Thread(() => {
                try {
                    var text = new StringBuilder(); var buffer = new char[4096]; bool truncated = false;
                    int count;
                    while ((count = reader.Read(buffer, 0, buffer.Length)) != 0) {
                        int keep = Math.Min(count, CaptureLimit - text.Length);
                        if (keep > 0) text.Append(buffer, 0, keep);
                        if (keep < count) truncated = true; // Drain both pipes with bounded memory.
                    }
                    done.TrySetResult(new Capture { Text = text.ToString(), Truncated = truncated });
                } catch (Exception ex) { done.TrySetException(ex); }
            }) { IsBackground = true, Name = "ProGo diagnostic output" };
            worker.Start(); return done.Task;
        }
        private static void CheckDeadline(Stopwatch watch, int timeoutMs, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (watch.ElapsedMilliseconds >= timeoutMs) throw new TimeoutException("Diagnostic process deadline exceeded.");
        }
        private static void Native(bool success)
        { if (!success) throw new Win32Exception(Marshal.GetLastWin32Error()); }
        private static string Resolve(string executable)
        {
            if (Path.IsPathRooted(executable)) return executable;
            var buffer = new StringBuilder(32768);
            uint length = SearchPath(null, executable, null, buffer.Capacity, buffer, IntPtr.Zero);
            if (length == 0 || length >= buffer.Capacity) throw new Win32Exception(Marshal.GetLastWin32Error());
            return buffer.ToString();
        }
        private static uint Active(SafeFileHandle job)
        {
            Accounting info; uint returned;
            Native(QueryInformationJobObject(job, 1, out info, Marshal.SizeOf(typeof(Accounting)), out returned));
            return info.ActiveProcesses;
        }
        private static void Settle(SafeFileHandle job)
        {
            Native(TerminateJobObject(job, 1));
            var watch = Stopwatch.StartNew();
            while (Active(job) != 0) {
                if (watch.ElapsedMilliseconds >= 2000) throw new IOException("Diagnostic process cleanup could not be confirmed.");
                Thread.Sleep(10);
            }
        }
        internal static DiagnosticProcessResult Run(string executable, string arguments, int timeoutMs, CancellationToken token)
        {
            if (timeoutMs < 1) throw new ArgumentOutOfRangeException("timeoutMs");
            var watch = Stopwatch.StartNew(); CheckDeadline(watch, timeoutMs, token);
            string path = Resolve(executable); CheckDeadline(watch, timeoutMs, token);
            using (var job = CreateJobObject(IntPtr.Zero, null))
            using (var output = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable))
            using (var error = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable))
            using (var input = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable))
            using (var stdout = new StreamReader(output, Encoding.UTF8))
            using (var stderr = new StreamReader(error, Encoding.UTF8)) {
                if (job.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error());
                var limits = new ExtendedLimits(); limits.Basic.LimitFlags = 0x2000; // KILL_ON_JOB_CLOSE; no breakaway.
                Native(SetInformationJobObject(job, 9, ref limits, Marshal.SizeOf(typeof(ExtendedLimits))));
                ProcessInfo child = new ProcessInfo(); SafeFileHandle process = null, thread = null;
                IntPtr attributes = IntPtr.Zero, handles = IntPtr.Zero; bool initialized = false, assigned = false, cleanupAttempted = false;
                Task<Capture> readOutput = null, readError = null;
                try {
                    var inherited = new[] { input.ClientSafePipeHandle.DangerousGetHandle(), output.ClientSafePipeHandle.DangerousGetHandle(), error.ClientSafePipeHandle.DangerousGetHandle() };
                    foreach (var handle in inherited) Native(SetHandleInformation(handle, 1, 1));
                    UIntPtr size = UIntPtr.Zero;
                    InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref size);
                    attributes = Marshal.AllocHGlobal(checked((int)size.ToUInt64()));
                    Native(InitializeProcThreadAttributeList(attributes, 1, 0, ref size)); initialized = true;
                    handles = Marshal.AllocHGlobal(IntPtr.Size * inherited.Length); Marshal.Copy(inherited, 0, handles, inherited.Length);
                    Native(UpdateProcThreadAttribute(attributes, 0, new IntPtr(0x20002), handles, new UIntPtr((uint)(IntPtr.Size * inherited.Length)), IntPtr.Zero, IntPtr.Zero));
                    var startup = new StartupEx(); startup.Info.Size = Marshal.SizeOf(typeof(StartupEx)); startup.Attributes = attributes;
                    startup.Info.Flags = 0x100; startup.Info.Input = inherited[0]; startup.Info.Output = inherited[1]; startup.Info.Error = inherited[2];
                    CheckDeadline(watch, timeoutMs, token);
                    // Restrict inheritance to three pipe handles, then assign before any child code runs.
                    Native(CreateProcess(path, new StringBuilder(SshConnection.Quote(path) + " " + arguments), IntPtr.Zero, IntPtr.Zero, true,
                        0x08080004, IntPtr.Zero, null, ref startup, out child)); // NO_WINDOW | EXTENDED_STARTUPINFO | SUSPENDED
                    process = new SafeFileHandle(child.Process, true); thread = new SafeFileHandle(child.Thread, true);
                    Native(AssignProcessToJobObject(job, process)); assigned = true;
                    output.DisposeLocalCopyOfClientHandle(); error.DisposeLocalCopyOfClientHandle(); input.DisposeLocalCopyOfClientHandle(); input.Dispose();
                    readOutput = Drain(stdout); readError = Drain(stderr);
                    CheckDeadline(watch, timeoutMs, token);
                    if (ResumeThread(thread) == UInt32.MaxValue) throw new Win32Exception(Marshal.GetLastWin32Error());
                    while (true) {
                        CheckDeadline(watch, timeoutMs, token);
                        uint wait = WaitForSingleObject(process, (uint)Math.Max(1, Math.Min(50, timeoutMs - watch.ElapsedMilliseconds)));
                        if (wait == 0) break;
                        if (wait != 258) throw new Win32Exception(Marshal.GetLastWin32Error());
                    }
                    uint code; Native(GetExitCodeProcess(process, out code));
                    // A Match exec helper must not survive its parent or hold its pipe open.
                    cleanupAttempted = true; Settle(job);
                    while (!readOutput.IsCompleted || !readError.IsCompleted) {
                        CheckDeadline(watch, timeoutMs, token); Thread.Sleep(10);
                    }
                    CheckDeadline(watch, timeoutMs, token);
                    var o = readOutput.GetAwaiter().GetResult(); var e = readError.GetAwaiter().GetResult();
                    return new DiagnosticProcessResult { Output = o.Text, Error = e.Text, ExitCode = unchecked((int)code), Truncated = o.Truncated || e.Truncated };
                }
                finally {
                    try {
                        if (assigned && !cleanupAttempted) Settle(job);
                        else if (!assigned && process != null) { Native(TerminateProcess(process, 1)); if (WaitForSingleObject(process, 2000) != 0) throw new IOException("Suspended diagnostic process cleanup failed."); }
                    } finally {
                        if (readOutput != null) readOutput.ContinueWith(t => { var observed = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
                        if (readError != null) readError.ContinueWith(t => { var observed = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
                        if (process != null) process.Dispose(); if (thread != null) thread.Dispose();
                        if (initialized) DeleteProcThreadAttributeList(attributes);
                        if (attributes != IntPtr.Zero) Marshal.FreeHGlobal(attributes); if (handles != IntPtr.Zero) Marshal.FreeHGlobal(handles);
                    }
                }
            }
        }
        [StructLayout(LayoutKind.Sequential)] private struct BasicLimits { internal long ProcessTime, JobTime; internal uint LimitFlags; internal UIntPtr MinimumWorkingSet, MaximumWorkingSet; internal uint ActiveProcessLimit; internal UIntPtr Affinity; internal uint PriorityClass, SchedulingClass; }
        [StructLayout(LayoutKind.Sequential)] private struct IoCounters { internal ulong ReadOperations, WriteOperations, OtherOperations, ReadBytes, WriteBytes, OtherBytes; }
        [StructLayout(LayoutKind.Sequential)] private struct ExtendedLimits { internal BasicLimits Basic; internal IoCounters Io; internal UIntPtr ProcessMemory, JobMemory, PeakProcessMemory, PeakJobMemory; }
        [StructLayout(LayoutKind.Sequential)] private struct Accounting { internal long UserTime, KernelTime, PeriodUserTime, PeriodKernelTime; internal uint PageFaults, TotalProcesses, ActiveProcesses, TerminatedProcesses; }
        [StructLayout(LayoutKind.Sequential)] private struct Startup { internal int Size; internal IntPtr Reserved, Desktop, Title; internal uint X, Y, Width, Height, XChars, YChars, Fill, Flags; internal ushort ShowWindow, ReservedSize; internal IntPtr ReservedData, Input, Output, Error; }
        [StructLayout(LayoutKind.Sequential)] private struct StartupEx { internal Startup Info; internal IntPtr Attributes; }
        [StructLayout(LayoutKind.Sequential)] private struct ProcessInfo { internal IntPtr Process, Thread; internal uint ProcessId, ThreadId; }
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern SafeFileHandle CreateJobObject(IntPtr security, string name);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool SetInformationJobObject(SafeFileHandle job, int type, ref ExtendedLimits info, int size);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool AssignProcessToJobObject(SafeFileHandle job, SafeFileHandle process);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool TerminateJobObject(SafeFileHandle job, uint code);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool QueryInformationJobObject(SafeFileHandle job, int type, out Accounting info, int size, out uint returned);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern uint SearchPath(string path, string file, string extension, int size, StringBuilder buffer, IntPtr filePart);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool SetHandleInformation(IntPtr handle, uint mask, uint flags);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool InitializeProcThreadAttributeList(IntPtr list, int count, int flags, ref UIntPtr size);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool UpdateProcThreadAttribute(IntPtr list, int flags, IntPtr attribute, IntPtr value, UIntPtr size, IntPtr previous, IntPtr returnedSize);
        [DllImport("kernel32.dll")] private static extern void DeleteProcThreadAttributeList(IntPtr list);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool CreateProcess(string app, StringBuilder command, IntPtr processSecurity, IntPtr threadSecurity, bool inherit, uint flags, IntPtr environment, string directory, ref StartupEx startup, out ProcessInfo info);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern uint ResumeThread(SafeFileHandle thread);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern uint WaitForSingleObject(SafeFileHandle handle, uint milliseconds);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool GetExitCodeProcess(SafeFileHandle process, out uint code);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool TerminateProcess(SafeFileHandle process, uint code);
    }
}

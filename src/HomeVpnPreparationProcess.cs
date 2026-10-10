using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace ProGo
{
    // Only the preparatory SCP console, before any server configuration command.
    // User terminals and persistent tunnels must not use this owner.
    // Remote waiting is allowed only with an already durable recoverable request ID.
    internal static class HomeVpnPreparationProcess
    {
        internal const int TimeoutMs = 300000;
        internal static Task CopyAsync(string executable, string arguments, int timeoutMs, CancellationToken token)
        {
            return Task.Run(delegate {
                var script = "$p=Start-Process -FilePath " + Literal(executable) + " -ArgumentList " + Literal(arguments)
                    + " -NoNewWindow -PassThru -Wait; if($p.ExitCode -ne 0){ Write-Host 'SCP failed. Check SSH access above.'; Start-Sleep -Seconds 5 }; exit $p.ExitCode";
                var shell = Path.Combine(Environment.SystemDirectory, @"WindowsPowerShell\v1.0\powershell.exe");
                int code = Run(shell, "-NoProfile -EncodedCommand " + Convert.ToBase64String(Encoding.Unicode.GetBytes(script)), timeoutMs, token);
                if (code != 0) throw new IOException("Копирование не завершено. Команды настройки VPS не запускались. Проверьте SSH, пароль и подтверждение ключа в его окне, затем повторите подготовку.");
            });
        }
        internal static Task CommandAsync(string executable, string arguments, string output, int timeoutMs, CancellationToken token)
        {
            return Task.Run(delegate {
                var script = "$p=Start-Process -FilePath " + Literal(executable) + " -ArgumentList " + Literal(arguments)
                    + " -NoNewWindow -PassThru -Wait -RedirectStandardOutput " + Literal(output) + "; exit $p.ExitCode";
                var shell = Path.Combine(Environment.SystemDirectory, @"WindowsPowerShell\v1.0\powershell.exe");
                int code = Run(shell, "-NoProfile -EncodedCommand " + Convert.ToBase64String(Encoding.Unicode.GetBytes(script)), timeoutMs, token, true);
                if (code != 0) throw new HomeVpnSetupPendingException("SSH не подтвердил результат. Команда VPS могла завершиться; запрос сохранён. Проверьте прежнюю настройку.");
            });
        }
        private static string Literal(string value) { return "'" + value.Replace("'", "''") + "'"; }
        private static void Native(bool success) { if (!success) throw new Win32Exception(Marshal.GetLastWin32Error()); }
        private static void Deadline(Stopwatch watch, int timeoutMs, CancellationToken token, bool serverWait)
        {
            token.ThrowIfCancellationRequested();
            if (watch.ElapsedMilliseconds >= timeoutMs)
                throw new TimeoutException(serverWait ? "Время ожидания SSH истекло. Команда VPS могла завершиться; запрос сохранён для проверки." : "Время копирования истекло. Команды настройки VPS не запускались. Проверьте SSH и повторите подготовку.");
        }
        private static void Settle(SafeFileHandle job, bool serverWait)
        {
            Native(TerminateJobObject(job, 1));
            var watch = Stopwatch.StartNew();
            while (true) {
                Accounting info; uint returned;
                Native(QueryInformationJobObject(job, 1, out info, Marshal.SizeOf(typeof(Accounting)), out returned));
                if (info.ActiveProcesses == 0) return;
                if (watch.ElapsedMilliseconds >= 2000)
                    throw new IOException(serverWait ? "Остановка локальных процессов SSH не подтверждена. Запрос VPS сохранён; результат не подтверждён." : "Остановка процессов копирования не подтверждена. Команды настройки VPS не запускались; отмена пока не подтверждена.");
                Thread.Sleep(10);
            }
        }
        internal static int Run(string executable, string arguments, int timeoutMs, CancellationToken token)
        { return Run(executable, arguments, timeoutMs, token, false); }
        internal static int Run(string executable, string arguments, int timeoutMs, CancellationToken token, bool serverWait)
        {
            if (timeoutMs < 1) throw new ArgumentOutOfRangeException("timeoutMs");
            var watch = Stopwatch.StartNew(); Deadline(watch, timeoutMs, token, serverWait);
            using (var job = CreateJobObject(IntPtr.Zero, null)) {
                if (job.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error());
                var limits = new ExtendedLimits(); limits.Basic.LimitFlags = 0x2000; // KILL_ON_JOB_CLOSE; no breakaway.
                Native(SetInformationJobObject(job, 9, ref limits, Marshal.SizeOf(typeof(ExtendedLimits))));
                SafeFileHandle process = null, thread = null; bool assigned = false, settled = false;
                try {
                    var startup = new Startup { Size = Marshal.SizeOf(typeof(Startup)), Title = serverWait ? "ProGo — ожидание SSH" : "ProGo — подготовка VPS" };
                    ProcessInfo child;
                    Deadline(watch, timeoutMs, token, serverWait);
                    // A real new console preserves OpenSSH password and host-key prompts.
                    // Assign the suspended root before PowerShell/SCP can spawn descendants.
                    Native(CreateProcess(executable, new StringBuilder(HomeVpnService.Argument(executable) + " " + arguments),
                        IntPtr.Zero, IntPtr.Zero, false, 0x14, IntPtr.Zero, null, ref startup, out child)); // NEW_CONSOLE | SUSPENDED
                    process = new SafeFileHandle(child.Process, true); thread = new SafeFileHandle(child.Thread, true);
                    Native(AssignProcessToJobObject(job, process)); assigned = true;
                    Deadline(watch, timeoutMs, token, serverWait);
                    if (ResumeThread(thread) == UInt32.MaxValue) throw new Win32Exception(Marshal.GetLastWin32Error());
                    while (true) {
                        Deadline(watch, timeoutMs, token, serverWait);
                        uint wait = WaitForSingleObject(process, 50);
                        if (wait == 0) break;
                        if (wait != 258) throw new Win32Exception(Marshal.GetLastWin32Error());
                    }
                    uint code; Native(GetExitCodeProcess(process, out code));
                    Settle(job, serverWait); settled = true; Deadline(watch, timeoutMs, token, serverWait);
                    return unchecked((int)code);
                }
                finally {
                    try {
                        if (assigned && !settled) Settle(job, serverWait);
                        else if (!assigned && process != null) {
                            Native(TerminateProcess(process, 1));
                            if (WaitForSingleObject(process, 2000) != 0) throw new IOException(serverWait ? "Остановка процесса SSH не подтверждена. Запрос VPS сохранён." : "Остановка процесса подготовки не подтверждена.");
                        }
                    } finally { if (process != null) process.Dispose(); if (thread != null) thread.Dispose(); }
                }
            }
        }
        [StructLayout(LayoutKind.Sequential)] private struct BasicLimits { internal long ProcessTime, JobTime; internal uint LimitFlags; internal UIntPtr MinimumWorkingSet, MaximumWorkingSet; internal uint ActiveProcessLimit; internal UIntPtr Affinity; internal uint PriorityClass, SchedulingClass; }
        [StructLayout(LayoutKind.Sequential)] private struct IoCounters { internal ulong ReadOperations, WriteOperations, OtherOperations, ReadBytes, WriteBytes, OtherBytes; }
        [StructLayout(LayoutKind.Sequential)] private struct ExtendedLimits { internal BasicLimits Basic; internal IoCounters Io; internal UIntPtr ProcessMemory, JobMemory, PeakProcessMemory, PeakJobMemory; }
        [StructLayout(LayoutKind.Sequential)] private struct Accounting { internal long UserTime, KernelTime, PeriodUserTime, PeriodKernelTime; internal uint PageFaults, TotalProcesses, ActiveProcesses, TerminatedProcesses; }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] private struct Startup { internal int Size; internal IntPtr Reserved, Desktop; [MarshalAs(UnmanagedType.LPWStr)] internal string Title; internal uint X, Y, Width, Height, XChars, YChars, Fill, Flags; internal ushort ShowWindow, ReservedSize; internal IntPtr ReservedData, Input, Output, Error; }
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

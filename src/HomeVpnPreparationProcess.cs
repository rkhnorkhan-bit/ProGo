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
    internal sealed class HomeVpnOwnerUnconfirmedException : InvalidOperationException
    {
        internal const string UnconfirmedMessage = "Результат команды VPS не подтверждён. Она могла завершиться или продолжать работу. ProGo не повторяет команду автоматически и не откатывает изменения. Проверьте VPS перед следующим действием.";
        internal HomeVpnOwnerUnconfirmedException() : base(UnconfirmedMessage) { }
        internal HomeVpnOwnerUnconfirmedException(string message) : base(message) { }
    }

    internal enum HomeVpnWaitPurpose { Copy, RecoveryCopy, SetupCommand, ListCopy, ListCommand, AdminCommand, OwnerCommand, ShareCommand }

    // Owns only short-lived SCP/SSH console waits.
    // User terminals and persistent tunnels must not use this owner.
    // Setup/issuance have recovery requests; ordinary owner commands promise
    // only bounded local waiting and an honest uncertain remote outcome.
    internal static class HomeVpnPreparationProcess
    {
        internal const int TimeoutMs = 300000;
        internal static Task CopyAsync(string executable, string arguments, int timeoutMs, CancellationToken token)
        { return CopyAsync(executable, arguments, timeoutMs, token, false); }
        internal static Task CopyAsync(string executable, string arguments, int timeoutMs, CancellationToken token, bool list)
        {
            return Task.Run(delegate {
                var script = "$p=Start-Process -FilePath " + Literal(executable) + " -ArgumentList " + Literal(arguments)
                    + " -NoNewWindow -PassThru -Wait; if($p.ExitCode -ne 0){ Write-Host 'SCP failed. Check SSH access above.'; Start-Sleep -Seconds 5 }; exit $p.ExitCode";
                var shell = Path.Combine(Environment.SystemDirectory, @"WindowsPowerShell\v1.0\powershell.exe");
                int code = Run(shell, "-NoProfile -EncodedCommand " + Convert.ToBase64String(Encoding.Unicode.GetBytes(script)), timeoutMs, token,
                    list ? HomeVpnWaitPurpose.ListCopy : HomeVpnWaitPurpose.Copy);
                if (code != 0) throw new IOException(list ? "Не удалось подготовить получение списка. Существующий доступ и текущий список сохранены. Проверьте SSH и повторите получение."
                    : "Копирование не завершено. Команды настройки VPS не запускались. Проверьте SSH, пароль и подтверждение ключа в его окне, затем повторите подготовку.");
            });
        }
        internal static Task CommandAsync(string executable, string arguments, string output, int timeoutMs, CancellationToken token)
        { return CommandAsync(executable, arguments, output, timeoutMs, token, HomeVpnWaitPurpose.SetupCommand); }
        internal static Task ListAsync(string executable, string arguments, string output, int timeoutMs, CancellationToken token)
        { return CommandAsync(executable, arguments, output, timeoutMs, token, HomeVpnWaitPurpose.ListCommand); }
        internal static Task AdminAsync(string executable, string arguments, string output, int timeoutMs, CancellationToken token)
        { return CommandAsync(executable, arguments, output, timeoutMs, token, HomeVpnWaitPurpose.AdminCommand); }
        internal static Task OwnerAsync(string executable, string arguments, string output, int timeoutMs, CancellationToken token)
        { return CommandAsync(executable, arguments, output, timeoutMs, token, HomeVpnWaitPurpose.OwnerCommand); }
        internal static Task ShareAsync(string executable, string arguments, string output, int timeoutMs, CancellationToken token)
        { return CommandAsync(executable, arguments, output, timeoutMs, token, HomeVpnWaitPurpose.ShareCommand); }
        private static Task CommandAsync(string executable, string arguments, string output, int timeoutMs, CancellationToken token, HomeVpnWaitPurpose purpose)
        {
            return Task.Run(delegate {
                var script = "$p=Start-Process -FilePath " + Literal(executable) + " -ArgumentList " + Literal(arguments)
                    + " -NoNewWindow -PassThru -Wait -RedirectStandardOutput " + Literal(output) + "; exit $p.ExitCode";
                var shell = Path.Combine(Environment.SystemDirectory, @"WindowsPowerShell\v1.0\powershell.exe");
                int code = Run(shell, "-NoProfile -EncodedCommand " + Convert.ToBase64String(Encoding.Unicode.GetBytes(script)), timeoutMs, token, purpose);
                if (code != 0) {
                    if (purpose == HomeVpnWaitPurpose.ListCommand) throw new IOException("SSH не вернул список. Существующий доступ и текущий список сохранены; повторите получение после проверки SSH.");
                    if (purpose == HomeVpnWaitPurpose.AdminCommand) throw new HomeVpnAdminPendingException(HomeVpnAdminRecovery.PendingMessage);
                    if (purpose == HomeVpnWaitPurpose.OwnerCommand) throw new HomeVpnOwnerUnconfirmedException();
                    if (purpose == HomeVpnWaitPurpose.ShareCommand) throw new HomeVpnSharePendingException(HomeVpnShareRecovery.PendingMessage);
                    throw new HomeVpnSetupPendingException("SSH не подтвердил результат. Команда VPS могла завершиться; запрос сохранён. Проверьте прежнюю настройку.");
                }
            });
        }
        private static string Literal(string value) { return "'" + value.Replace("'", "''") + "'"; }
        private static void Native(bool success) { if (!success) throw new Win32Exception(Marshal.GetLastWin32Error()); }
        private static bool List(HomeVpnWaitPurpose purpose) { return purpose == HomeVpnWaitPurpose.ListCopy || purpose == HomeVpnWaitPurpose.ListCommand; }
        private static void Deadline(Stopwatch watch, int timeoutMs, CancellationToken token, HomeVpnWaitPurpose purpose)
        {
            token.ThrowIfCancellationRequested();
            if (watch.ElapsedMilliseconds >= timeoutMs)
                throw new TimeoutException(List(purpose) ? "Время получения списка истекло. Существующий доступ и текущий список сохранены; повторите получение."
                    : purpose == HomeVpnWaitPurpose.AdminCommand ? "Время ожидания выдачи истекло. Запрос сохранён; команда VPS могла завершиться. Проверьте прежнюю выдачу без нового приглашения."
                    : purpose == HomeVpnWaitPurpose.OwnerCommand ? "Время ожидания SSH истекло. " + HomeVpnOwnerUnconfirmedException.UnconfirmedMessage
                    : purpose == HomeVpnWaitPurpose.ShareCommand ? "Время ожидания SSH истекло. " + HomeVpnShareRecovery.PendingMessage
                    : purpose == HomeVpnWaitPurpose.SetupCommand ? "Время ожидания SSH истекло. Команда VPS могла завершиться; запрос сохранён для проверки." : "Время копирования истекло. Команды настройки VPS не запускались. Проверьте SSH и повторите подготовку.");
        }
        private static void Settle(SafeFileHandle job, HomeVpnWaitPurpose purpose)
        {
            Native(TerminateJobObject(job, 1));
            var watch = Stopwatch.StartNew();
            while (true) {
                Accounting info; uint returned;
                Native(QueryInformationJobObject(job, 1, out info, Marshal.SizeOf(typeof(Accounting)), out returned));
                if (info.ActiveProcesses == 0) return;
                if (watch.ElapsedMilliseconds >= 2000) {
                    if (purpose == HomeVpnWaitPurpose.OwnerCommand) throw new HomeVpnOwnerUnconfirmedException("Остановка локальных процессов SSH не подтверждена. " + HomeVpnOwnerUnconfirmedException.UnconfirmedMessage);
                    if (purpose == HomeVpnWaitPurpose.ShareCommand) throw new HomeVpnSharePendingException("Остановка локальных процессов SSH не подтверждена. " + HomeVpnShareRecovery.PendingMessage);
                    throw new IOException(List(purpose) ? "Остановка процессов получения списка не подтверждена. Существующий доступ сохранён; отмена пока не подтверждена."
                        : purpose == HomeVpnWaitPurpose.AdminCommand ? "Остановка локальных процессов выдачи не подтверждена. Запрос сохранён; результат VPS не подтверждён."
                        : purpose == HomeVpnWaitPurpose.SetupCommand ? "Остановка локальных процессов SSH не подтверждена. Запрос VPS сохранён; результат не подтверждён." : "Остановка процессов копирования не подтверждена. Команды настройки VPS не запускались; отмена пока не подтверждена.");
                }
                Thread.Sleep(10);
            }
        }
        internal static int Run(string executable, string arguments, int timeoutMs, CancellationToken token)
        { return Run(executable, arguments, timeoutMs, token, false); }
        internal static int Run(string executable, string arguments, int timeoutMs, CancellationToken token, bool serverWait)
        { return Run(executable, arguments, timeoutMs, token, serverWait ? HomeVpnWaitPurpose.SetupCommand : HomeVpnWaitPurpose.Copy); }
        internal static int Run(string executable, string arguments, int timeoutMs, CancellationToken token, HomeVpnWaitPurpose purpose)
        {
            if (timeoutMs < 1) throw new ArgumentOutOfRangeException("timeoutMs");
            var watch = Stopwatch.StartNew(); Deadline(watch, timeoutMs, token, purpose);
            using (var job = CreateJobObject(IntPtr.Zero, null)) {
                if (job.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error());
                var limits = new ExtendedLimits(); limits.Basic.LimitFlags = 0x2000; // KILL_ON_JOB_CLOSE; no breakaway.
                Native(SetInformationJobObject(job, 9, ref limits, Marshal.SizeOf(typeof(ExtendedLimits))));
                SafeFileHandle process = null, thread = null; bool assigned = false, settled = false;
                try {
                    var startup = new Startup { Size = Marshal.SizeOf(typeof(Startup)), Title = List(purpose) ? "ProGo — получение списка" : purpose == HomeVpnWaitPurpose.ShareCommand ? "ProGo — настройка HTTPS" : purpose == HomeVpnWaitPurpose.AdminCommand ? "ProGo — доступ друзей" : purpose == HomeVpnWaitPurpose.OwnerCommand ? "ProGo — команда владельца VPS" : purpose == HomeVpnWaitPurpose.SetupCommand ? "ProGo — ожидание SSH" : "ProGo — подготовка VPS" };
                    ProcessInfo child;
                    Deadline(watch, timeoutMs, token, purpose);
                    // A real new console preserves OpenSSH password and host-key prompts.
                    // Assign the suspended root before PowerShell/SCP can spawn descendants.
                    Native(CreateProcess(executable, new StringBuilder(HomeVpnService.Argument(executable) + " " + arguments),
                        IntPtr.Zero, IntPtr.Zero, false, 0x14, IntPtr.Zero, null, ref startup, out child)); // NEW_CONSOLE | SUSPENDED
                    process = new SafeFileHandle(child.Process, true); thread = new SafeFileHandle(child.Thread, true);
                    Native(AssignProcessToJobObject(job, process)); assigned = true;
                    Deadline(watch, timeoutMs, token, purpose);
                    if (ResumeThread(thread) == UInt32.MaxValue) throw new Win32Exception(Marshal.GetLastWin32Error());
                    while (true) {
                        Deadline(watch, timeoutMs, token, purpose);
                        uint wait = WaitForSingleObject(process, 50);
                        if (wait == 0) break;
                        if (wait != 258) throw new Win32Exception(Marshal.GetLastWin32Error());
                    }
                    uint code; Native(GetExitCodeProcess(process, out code));
                    Settle(job, purpose); settled = true; Deadline(watch, timeoutMs, token, purpose);
                    return unchecked((int)code);
                }
                finally {
                    try {
                        if (assigned && !settled) Settle(job, purpose);
                        else if (!assigned && process != null) {
                            Native(TerminateProcess(process, 1));
                            if (WaitForSingleObject(process, 2000) != 0) throw new IOException(List(purpose) ? "Остановка процесса получения списка не подтверждена. Существующий доступ сохранён."
                                : purpose == HomeVpnWaitPurpose.OwnerCommand ? "Остановка локального процесса SSH не подтверждена. " + HomeVpnOwnerUnconfirmedException.UnconfirmedMessage
                                : purpose == HomeVpnWaitPurpose.ShareCommand ? "Остановка локального процесса SSH не подтверждена. " + HomeVpnShareRecovery.PendingMessage
                                : purpose == HomeVpnWaitPurpose.AdminCommand ? "Остановка процесса выдачи не подтверждена. Запрос сохранён; результат VPS не подтверждён."
                                : purpose == HomeVpnWaitPurpose.SetupCommand ? "Остановка процесса SSH не подтверждена. Запрос VPS сохранён." : "Остановка процесса подготовки не подтверждена.");
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

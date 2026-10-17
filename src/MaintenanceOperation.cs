using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace ProGo
{
    public sealed class MaintenanceHandoffResult
    {
        public bool Accepted { get; internal set; }
        public string Error { get; internal set; }
    }
    // Also compiled by the installed PowerShell scripts: one ownership protocol.
    public sealed class MaintenanceOperation : IDisposable
    {
        private const string PermitVariable = "PROGO_MAINTENANCE_PERMIT";
        private const string ReadyVariable = "PROGO_MAINTENANCE_READY";
        private readonly Mutex mutex;
        private EventWaitHandle permit;
        private string previousPermit;
        private bool owned, disposed;
        private static SecurityIdentifier User { get { return WindowsIdentity.GetCurrent().User; } }
        private static string Prefix { get { return "Global\\ProGo.Maintenance." + User.Value + "."; } }

        private static Mutex CreateMutex(string name)
        {
            var security = new MutexSecurity();
            security.SetAccessRuleProtection(true, false);
            security.AddAccessRule(new MutexAccessRule(User, MutexRights.FullControl, AccessControlType.Allow));
            security.AddAccessRule(new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null), MutexRights.FullControl, AccessControlType.Allow));
            bool created;
            return new Mutex(false, name, out created, security);
        }
        private static EventWaitHandle CreateEvent(string name)
        {
            var security = new EventWaitHandleSecurity();
            security.SetAccessRuleProtection(true, false);
            security.AddAccessRule(new EventWaitHandleAccessRule(User, EventWaitHandleRights.FullControl, AccessControlType.Allow));
            security.AddAccessRule(new EventWaitHandleAccessRule(new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null), EventWaitHandleRights.FullControl, AccessControlType.Allow));
            bool created;
            return new EventWaitHandle(false, EventResetMode.ManualReset, name, out created, security);
        }
        private MaintenanceOperation(int timeout)
        {
            mutex = CreateMutex(Prefix + "Owner");
            try { owned = mutex.WaitOne(timeout); }
            catch (AbandonedMutexException) { owned = true; }
        }
        public static MaintenanceOperation Enter()
        {
            var operation = new MaintenanceOperation(0);
            try
            {
                if (!operation.owned) throw new InvalidOperationException("ProGo: update, restore or uninstall is already in progress. Try again when it finishes.");
                string name = Prefix + "Permit." + Guid.NewGuid().ToString("N");
                operation.permit = CreateEvent(name); operation.permit.Set();
                operation.previousPermit = Environment.GetEnvironmentVariable(PermitVariable);
                Environment.SetEnvironmentVariable(PermitVariable, name);
                return operation;
            }
            catch { operation.Dispose(); throw; }
        }
        public static void ConfirmHandoff()
        {
            string ready = Environment.GetEnvironmentVariable(ReadyVariable);
            if (String.IsNullOrEmpty(ready)) return; // direct command-line operation
            if (!ready.StartsWith(Prefix + "Ready.", StringComparison.Ordinal)) throw new InvalidOperationException("Invalid maintenance handoff.");
            // Only acknowledge after validation, immediately before waiting for
            // the initiating app to exit. A timed-out initiator leaves no event.
            using (var signal = EventWaitHandle.OpenExisting(ready)) signal.Set();
            Environment.SetEnvironmentVariable(ReadyVariable, null);
        }
        public static bool TryEnterStartup(out IDisposable startup)
        {
            // Startup owners keep this gate only until the lifetime mutex is acquired.
            // Serialize concurrent normal starts rather than misreport maintenance.
            var operation = new MaintenanceOperation(3000);
            if (operation.owned) { startup = operation; return true; }
            operation.Dispose(); startup = null;
            string name = Environment.GetEnvironmentVariable(PermitVariable);
            if (String.IsNullOrEmpty(name) || !name.StartsWith(Prefix + "Permit.", StringComparison.Ordinal)) return false;
            try { using (var signal = EventWaitHandle.OpenExisting(name)) return signal.WaitOne(0); }
            catch (WaitHandleCannotBeOpenedException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
        }
        public static void RequireApplicationStopped(string installDir)
        {
            // New builds hold this mutex for their full lifetime, including tray mode.
            using (var instance = CreateMutex("Global\\ProGo.Instance." + User.Value))
            {
                bool acquired;
                try { acquired = instance.WaitOne(0); }
                catch (AbandonedMutexException) { acquired = true; }
                if (!acquired) throw new InvalidOperationException("ProGo is still running. Close it before updating or restoring.");
                instance.ReleaseMutex();
            }
            // Compatibility with earlier builds that have no instance mutex.
            string exe = Path.GetFullPath(Path.Combine(installDir, "ProGo.exe"));
            foreach (var process in Process.GetProcessesByName("ProGo"))
                using (process)
                {
                    if (process.HasExited) continue;
                    if (String.Equals(process.MainModule.FileName, exe, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Installed ProGo is still running. Close it before updating or restoring.");
                }
        }
        public static void RequireProxyCleanupCompleted(string installDir)
        {
            if (!Directory.Exists(installDir)) return;
            foreach (var file in Directory.GetFiles(installDir, "*-backup.json"))
                if (String.Equals(Path.GetFileName(file), "system-proxy-backup.json", StringComparison.OrdinalIgnoreCase) ||
                    String.Equals(Path.GetFileName(file), "proxy-environment-backup.json", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Очистка прокси ProGo не завершена. Файлы приложения сохранены. Откройте ProGo и повторите выключение Windows/CLI, затем повторите операцию.");
        }
        public static void StopApplication(string installDir)
        {
            bool running;
            using (var instance = CreateMutex("Global\\ProGo.Instance." + User.Value)) {
                bool acquired;
                try { acquired = instance.WaitOne(0); } catch (AbandonedMutexException) { acquired = true; }
                running = !acquired;
                if (acquired) instance.ReleaseMutex();
            }
            if (running) {
                bool confirmed = false;
                try {
                    using (var client = new NamedPipeClientStream(".", "ProGo.Show." + User.Value, PipeDirection.InOut, PipeOptions.Asynchronous)) {
                        client.Connect(3000); client.WriteByte(2); client.Flush();
                        var buffer = new byte[1]; var read = client.ReadAsync(buffer, 0, 1);
                        confirmed = read.Wait(15000) && read.Result == 1 && buffer[0] == 2;
                    }
                } catch (IOException) { } catch (TimeoutException) { }
                catch (UnauthorizedAccessException) { } catch (AggregateException) { }
                if (!confirmed) throw new InvalidOperationException("ProGo не подтвердил очистку прокси. Удаление отменено, приложение и его файлы сохранены. Откройте ProGo, устраните ошибку выключения Windows/CLI и повторите удаление.");
                var deadline = DateTime.UtcNow.AddSeconds(10);
                while (true) {
                    try { RequireApplicationStopped(installDir); break; }
                    catch (InvalidOperationException) { if (DateTime.UtcNow >= deadline) throw; Thread.Sleep(100); }
                }
            }
            // Older builds without IPC are refused while running; never kill by process name.
            RequireApplicationStopped(installDir);
            RequireProxyCleanupCompleted(installDir);
        }
        public static bool StartHandoff(ProcessStartInfo info)
        {
            if (info.UseShellExecute) throw new ArgumentException("Maintenance handoff requires a child environment.");
            string name = Prefix + "Ready." + Guid.NewGuid().ToString("N");
            using (var ready = CreateEvent(name))
            {
                info.EnvironmentVariables[ReadyVariable] = name;
                using (var process = Process.Start(info))
                {
                    if (process == null) return false;
                    var deadline = DateTime.UtcNow.AddSeconds(15);
                    while (DateTime.UtcNow < deadline)
                    {
                        if (ready.WaitOne(100)) return !process.HasExited;
                        if (process.HasExited) return false;
                    }
                    return false;
                }
            }
        }
        public static MaintenanceHandoffResult StartOwnedHandoff(ProcessStartInfo info, CancellationToken cancellation)
        {
            if (info.UseShellExecute) throw new ArgumentException("Maintenance handoff requires a child environment.");
            cancellation.ThrowIfCancellationRequested();
            string name = Prefix + "Ready." + Guid.NewGuid().ToString("N");
            using (var ready = CreateEvent(name)) {
                info.EnvironmentVariables[ReadyVariable] = name;
                using (var process = new OwnedHandoffProcess(info)) {
                    var deadline = Stopwatch.StartNew();
                    while (deadline.ElapsedMilliseconds < 15000) {
                        // Ready is the acceptance boundary. A late Cancel does
                        // not stop a helper that already owns its verified copy.
                        if (ready.WaitOne(100) && !process.HasExited) { process.Accept(); return new MaintenanceHandoffResult { Accepted = true }; }
                        if (process.HasExited) return new MaintenanceHandoffResult { Error = "Помощник восстановления завершился без подтверждения. Приложение остаётся запущенным." };
                        if (cancellation.IsCancellationRequested) break;
                    }
                    if (ready.WaitOne(0) && !process.HasExited) { process.Accept(); return new MaintenanceHandoffResult { Accepted = true }; }
                    bool cancelled = cancellation.IsCancellationRequested;
                    // Only this suspended-and-contained process tree is stopped.
                    process.StopAndSettle();
                    return new MaintenanceHandoffResult { Error = cancelled ?
                        "Подготовка восстановления отменена до подтверждения помощника. Приложение остаётся запущенным." :
                        "Помощник восстановления не подтвердил готовность за 15 секунд и остановлен. Приложение остаётся запущенным; повторите после проверки журнала." };
                }
            }
        }
        // Kept in this standalone installed source: maintenance scripts compile
        // it without application dependencies. No child code runs before job
        // containment. Accepted helpers are detached; pending trees are settled.
        private sealed class OwnedHandoffProcess : IDisposable
        {
            private SafeFileHandle job, process, thread;
            private bool assigned, accepted;
            internal OwnedHandoffProcess(ProcessStartInfo info)
            {
                IntPtr environment = IntPtr.Zero;
                try {
                    job = CreateJobObject(IntPtr.Zero, null);
                    if (job.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error());
                    // No breakaway. Explicit confirmed cleanup is required before
                    // closing this handle; accepted helpers must survive app exit.
                    var limits = new ExtendedLimits();
                    Native(SetInformationJobObject(job, 9, ref limits, Marshal.SizeOf(typeof(ExtendedLimits))));
                    var names = new List<string>(); foreach (DictionaryEntry pair in info.EnvironmentVariables) names.Add((string)pair.Key);
                    names.Sort(StringComparer.OrdinalIgnoreCase);
                    var values = new StringBuilder(); foreach (var key in names) values.Append(key).Append('=').Append(info.EnvironmentVariables[key]).Append('\0');
                    values.Append('\0'); environment = Marshal.StringToHGlobalUni(values.ToString());
                    var file = Path.GetFullPath(ResolveExecutable(info.FileName));
                    var startup = new Startup(); startup.Size = Marshal.SizeOf(typeof(Startup));
                    ProcessInfo child;
                    Native(CreateProcess(file, new StringBuilder("\"" + file + "\" " + info.Arguments), IntPtr.Zero, IntPtr.Zero, false,
                        0x08000404, environment, String.IsNullOrEmpty(info.WorkingDirectory) ? null : info.WorkingDirectory,
                        ref startup, out child)); // NO_WINDOW | UNICODE_ENVIRONMENT | SUSPENDED
                    process = new SafeFileHandle(child.Process, true); thread = new SafeFileHandle(child.Thread, true);
                    Native(AssignProcessToJobObject(job, process)); assigned = true;
                    if (ResumeThread(thread) == UInt32.MaxValue) throw new Win32Exception(Marshal.GetLastWin32Error());
                } catch { Dispose(); throw; }
                finally { if (environment != IntPtr.Zero) Marshal.FreeHGlobal(environment); }
            }
            internal bool HasExited {
                get { uint wait = WaitForSingleObject(process, 0); if (wait == 0) return true;
                    if (wait != 258) throw new Win32Exception(Marshal.GetLastWin32Error()); return false; }
            }
            internal void Accept()
            {
                accepted = true;
            }
            internal void StopAndSettle()
            {
                if (process == null || accepted) return;
                if (!assigned) {
                    while (true) {
                        TerminateProcess(process, 1);
                        if (WaitForSingleObject(process, 100) == 0) break;
                        Thread.Sleep(100);
                    }
                    return;
                }
                // Keep this job and caller stage owned until the entire tree is
                // confirmed empty. UI shutdown independently refuses after 3s.
                while (true) {
                    TerminateJobObject(job, 1);
                    Accounting state; uint returned;
                    if (QueryInformationJobObject(job, 1, out state, Marshal.SizeOf(typeof(Accounting)), out returned) && state.ActiveProcesses == 0) break;
                    Thread.Sleep(100);
                }
            }
            public void Dispose()
            {
                try { if (!accepted) StopAndSettle(); }
                finally { if (thread != null) thread.Dispose(); if (process != null) process.Dispose(); if (job != null) job.Dispose(); }
            }
            private static string ResolveExecutable(string value)
            {
                if (Path.IsPathRooted(value)) return value;
                var path = new StringBuilder(32768); uint size = SearchPath(null, value, null, path.Capacity, path, IntPtr.Zero);
                if (size == 0 || size >= path.Capacity) throw new Win32Exception(Marshal.GetLastWin32Error()); return path.ToString();
            }
            private static void Native(bool value) { if (!value) throw new Win32Exception(Marshal.GetLastWin32Error()); }
            [StructLayout(LayoutKind.Sequential)] private struct BasicLimits { internal long ProcessTime, JobTime; internal uint LimitFlags; internal UIntPtr MinimumWorkingSet, MaximumWorkingSet; internal uint ActiveProcessLimit; internal UIntPtr Affinity; internal uint PriorityClass, SchedulingClass; }
            [StructLayout(LayoutKind.Sequential)] private struct IoCounters { internal ulong ReadOperations, WriteOperations, OtherOperations, ReadBytes, WriteBytes, OtherBytes; }
            [StructLayout(LayoutKind.Sequential)] private struct ExtendedLimits { internal BasicLimits Basic; internal IoCounters Io; internal UIntPtr ProcessMemory, JobMemory, PeakProcessMemory, PeakJobMemory; }
            [StructLayout(LayoutKind.Sequential)] private struct Accounting { internal long UserTime, KernelTime, PeriodUserTime, PeriodKernelTime; internal uint PageFaults, TotalProcesses, ActiveProcesses, TerminatedProcesses; }
            [StructLayout(LayoutKind.Sequential)] private struct Startup { internal int Size; internal IntPtr Reserved, Desktop, Title; internal uint X, Y, Width, Height, XChars, YChars, Fill, Flags; internal ushort ShowWindow, ReservedSize; internal IntPtr ReservedData, Input, Output, Error; }
            [StructLayout(LayoutKind.Sequential)] private struct ProcessInfo { internal IntPtr Process, Thread; internal uint ProcessId, ThreadId; }
            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern SafeFileHandle CreateJobObject(IntPtr security, string name);
            [DllImport("kernel32.dll", SetLastError = true)] private static extern bool SetInformationJobObject(SafeFileHandle job, int type, ref ExtendedLimits value, int size);
            [DllImport("kernel32.dll", SetLastError = true)] private static extern bool AssignProcessToJobObject(SafeFileHandle job, SafeFileHandle process);
            [DllImport("kernel32.dll", SetLastError = true)] private static extern bool TerminateJobObject(SafeFileHandle job, uint code);
            [DllImport("kernel32.dll", SetLastError = true)] private static extern bool QueryInformationJobObject(SafeFileHandle job, int type, out Accounting info, int size, out uint returned);
            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern uint SearchPath(string path, string file, string extension, int size, StringBuilder buffer, IntPtr part);
            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool CreateProcess(string app, StringBuilder command, IntPtr processSecurity, IntPtr threadSecurity, bool inherit, uint flags, IntPtr environment, string directory, ref Startup startup, out ProcessInfo child);
            [DllImport("kernel32.dll", SetLastError = true)] private static extern uint ResumeThread(SafeFileHandle thread);
            [DllImport("kernel32.dll", SetLastError = true)] private static extern uint WaitForSingleObject(SafeFileHandle process, uint milliseconds);
            [DllImport("kernel32.dll", SetLastError = true)] private static extern bool TerminateProcess(SafeFileHandle process, uint code);
        }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (permit != null)
            {
                permit.Reset(); permit.Dispose();
                Environment.SetEnvironmentVariable(PermitVariable, previousPermit);
            }
            if (owned) mutex.ReleaseMutex();
            mutex.Dispose();
        }
    }
}

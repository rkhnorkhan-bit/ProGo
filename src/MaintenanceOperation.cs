using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;

namespace ProGo
{
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

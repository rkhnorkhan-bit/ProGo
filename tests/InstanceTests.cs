using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;

namespace ProGo
{
    internal static class InstanceTests
    {
        private static int passed;
        private static void Check(bool value, string name) { if (!value) throw new Exception(name); passed++; Console.WriteLine("PASS: " + name); }
        private delegate bool WindowVisitor(IntPtr window, IntPtr state);
        [DllImport("user32.dll")] private static extern bool EnumWindows(WindowVisitor callback, IntPtr state);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr window, StringBuilder text, int count);
        [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr window);
        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr window, int command);

        private static void Wait(Func<bool> condition, string name)
        {
            var deadline = DateTime.UtcNow.AddSeconds(10);
            while (!condition())
            {
                if (DateTime.UtcNow >= deadline) throw new Exception("Timed out: " + name);
                System.Threading.Thread.Sleep(30);
            }
        }
        private static Process Start(string executable, string arguments)
        {
            return Process.Start(new ProcessStartInfo(executable, arguments) { UseShellExecute = false, CreateNoWindow = true });
        }
        private static int Exit(Process process)
        {
            using (process)
            {
                if (!process.WaitForExit(7000)) { process.Kill(); throw new Exception("Secondary launch did not exit"); }
                return process.ExitCode;
            }
        }
        private static string Snapshot()
        {
            if (!Directory.Exists(AppPaths.Root)) return "";
            using (var hash = SHA256.Create())
                return String.Join("\n", Directory.GetFiles(AppPaths.Root, "*", SearchOption.AllDirectories).OrderBy(p => p)
                    .Select(p => p + ":" + Convert.ToBase64String(hash.ComputeHash(File.ReadAllBytes(p)))).ToArray());
        }
        private static IntPtr[] Windows(int pid)
        {
            var found = new System.Collections.Generic.List<IntPtr>();
            EnumWindows(delegate(IntPtr window, IntPtr state) {
                uint owner; GetWindowThreadProcessId(window, out owner);
                var title = new StringBuilder(256); GetWindowText(window, title, title.Capacity);
                if (owner == pid && title.ToString() == "ProGo · Ваше подключение") found.Add(window);
                return true;
            }, IntPtr.Zero);
            return found.ToArray();
        }
        private static bool PortOpen(int port)
        {
            try { using (var client = new TcpClient()) { client.Connect(IPAddress.Loopback, port); return true; } }
            catch (SocketException) { return false; }
        }
        private static int AppliedPort()
        {
            try
            {
                var settings = SettingsService.DeserializeSettings(File.ReadAllText(AppPaths.SettingsPath));
                return CliProxyEnvironmentService.IsAppliedToUserEnvironment(settings.HttpProxyPort) && PortOpen(settings.HttpProxyPort) ? settings.HttpProxyPort : 0;
            }
            catch (IOException) { return 0; }
        }
        private static void Stop(Process process)
        {
            if (process == null) return;
            if (!process.HasExited) { process.Kill(); process.WaitForExit(5000); }
            process.Dispose();
        }
        [STAThread]
        private static int Main(string[] args)
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true")
            { Console.WriteLine("SKIP: instance integration requires isolated Windows CI"); return 0; }
            string installed = Path.Combine(AppPaths.Root, "ProGo.exe");
            Process primary = null, replacement = null;
            var originalSettings = File.Exists(AppPaths.SettingsPath) ? File.ReadAllBytes(AppPaths.SettingsPath) : null;
            var originals = CliProxyEnvironmentService.Names.ToDictionary(n => n, n => Environment.GetEnvironmentVariable(n, EnvironmentVariableTarget.User));
            var listener = new TcpListener(IPAddress.Loopback, 0);
            if (File.Exists(installed) || Process.GetProcessesByName("ProGo").Length != 0 || File.Exists(CliProxyEnvironmentService.BackupPath))
                throw new Exception("Instance fixture is not isolated");
            try
            {
                AppPaths.EnsureDirectories(); File.Copy(args[0], installed);
                string scriptRoot = args[1];
                string powershell = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "WindowsPowerShell", "v1.0", "powershell.exe");
                var snapshot = Snapshot();
                using (var owner = new ApplicationInstance())
                {
                    Check(owner.IsOwner, "first launch owns the per-user lifetime mutex");
                    Check(Exit(Start(installed, "--show")) == 0, "secondary activation succeeds while the owner is still starting");
                    int activations = 0;
                    owner.Attach(delegate { System.Threading.Interlocked.Increment(ref activations); });
                    Check(activations == 1, "startup activation is queued until the UI attaches");
                    foreach (var arguments in new[] { "", "--show", "/show", "show" })
                        Check(Exit(Start(installed, arguments)) == 0, "normal and compatible show launches reuse the owner: " + arguments);
                    Wait(() => activations == 5, "normal activation callbacks");
                    var parallel = Enumerable.Range(0, 4).Select(i => Start(installed, "--show")).ToArray();
                    foreach (var child in parallel) Check(Exit(child) == 0, "concurrent secondary launch exits after activation");
                    Wait(() => activations == 9, "concurrent activation callbacks");
                    foreach (var script in new[] { "Start-ProGo.ps1", "Show-ProGo.ps1" })
                        Check(Exit(Start(powershell, "-NoProfile -File \"" + Path.Combine(scriptRoot, script) + "\"")) == 0, "launcher script reuses existing owner: " + script);
                    Wait(() => activations == 11, "script activation callbacks");
                    Check(Exit(Start(installed, "--self-check")) == 3, "secondary self-check refuses shared state without creating services");
                    using (var client = new NamedPipeClientStream(".", "ProGo.Show." + WindowsIdentity.GetCurrent().User.Value, PipeDirection.InOut))
                    {
                        client.Connect(3000); client.WriteByte(99);
                        Check(client.ReadByte() == 0, "IPC rejects commands other than window activation");
                    }
                    using (var idle = new NamedPipeClientStream(".", "ProGo.Show." + WindowsIdentity.GetCurrent().User.Value, PipeDirection.InOut))
                    {
                        idle.Connect(3000);
                        Check(idle.ReadByte() == 0, "idle IPC client is timed out without blocking the owner indefinitely");
                    }
                    Check(Exit(Start(installed, "--show")) == 0, "activation remains available after malformed and idle clients");
                    Wait(() => activations == 12, "activation after invalid clients");
                    Check(Snapshot() == snapshot, "secondary launches leave settings, snapshots, backups and logs unchanged");
                }
                Check(Exit(Start(installed, "--self-check")) == 0, "clean owner disposal releases the mutex for a later launch");

                listener.Start();
                var configured = AppSettings.Defaults(); configured.AutoCliProxy = true; configured.AutoRestartSocks = false;
                configured.SocksPort = ((IPEndPoint)listener.LocalEndpoint).Port;
                using (var settings = new SettingsService()) settings.Save(configured);
                primary = Start(installed, "");
                Wait(() => !primary.HasExited && AppliedPort() > 0, "actual primary proxy startup");
                int port = AppliedPort();
                string settingsBefore = File.ReadAllText(AppPaths.SettingsPath);
                string backupBefore = File.ReadAllText(CliProxyEnvironmentService.BackupPath);
                Check(Exit(Start(installed, "--show")) == 0, "actual repeated launch activates the existing application");
                Wait(() => Windows(primary.Id).Length == 1, "primary dashboard activation");
                var window = Windows(primary.Id).Single(); ShowWindow(window, 6);
                Wait(() => IsIconic(window), "dashboard minimize");
                Check(Exit(Start(installed, "--show")) == 0, "repeat show exits without another UI owner");
                Wait(() => !IsIconic(window), "dashboard restored from minimized state");
                Check(Windows(primary.Id).Length == 1 && Process.GetProcessesByName("ProGo").Count(p => !p.HasExited) == 1, "one persistent process and one dashboard remain");
                Check(!primary.HasExited && AppliedPort() == port && File.ReadAllText(AppPaths.SettingsPath) == settingsBefore &&
                    File.ReadAllText(CliProxyEnvironmentService.BackupPath) == backupBefore,
                    "exiting secondary launch preserves the primary proxy and its original settings backup");
                Stop(primary); primary = null;
                replacement = Start(installed, "--show");
                Wait(() => !replacement.HasExited && AppliedPort() > 0 && Windows(replacement.Id).Length == 1, "recovery after crashed owner");
                Check(Process.GetProcessesByName("ProGo").Count(p => !p.HasExited) == 1, "abandoned mutex permits a new owner after a crash");
                Console.WriteLine("Instance tests PASS: " + passed); return 0;
            }
            catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
            finally
            {
                Stop(primary); Stop(replacement); listener.Stop();
                CliProxyEnvironmentService.ClearUserEnvironmentIfOwned();
                foreach (var pair in originals) Environment.SetEnvironmentVariable(pair.Key, pair.Value, EnvironmentVariableTarget.User);
                if (originalSettings != null) File.WriteAllBytes(AppPaths.SettingsPath, originalSettings);
                else if (File.Exists(AppPaths.SettingsPath)) File.Delete(AppPaths.SettingsPath);
                File.Delete(installed);
            }
        }
    }
}

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Microsoft.Win32;

namespace ProGo
{
    internal static class ShutdownTests
    {
        private static int passed;
        private static void Check(bool value, string name) { if (!value) throw new Exception(name); passed++; Console.WriteLine("PASS: " + name); }
        private static void Wait(Func<bool> test, string name) {
            var deadline = DateTime.UtcNow.AddSeconds(12);
            while (!test()) { if (DateTime.UtcNow >= deadline) throw new Exception("Timed out: " + name); System.Threading.Thread.Sleep(40); }
        }
        private static int Uninstall(string script) {
            string shell = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "WindowsPowerShell", "v1.0", "powershell.exe");
            using (var process = Process.Start(new ProcessStartInfo(shell, "-NoProfile -File \"" + script + "\"") {
                UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true
            })) {
                var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
                if (!process.WaitForExit(30000)) { process.Kill(); throw new Exception("Uninstall fixture timed out"); }
                output.Wait(); error.Wait();
                // Fixture failures are expected; never print runtime proxy settings or user paths.
                return process.ExitCode;
            }
        }
        private static bool PortOpen(int port) {
            try { using (var client = new TcpClient()) { client.Connect(IPAddress.Loopback, port); return true; } }
            catch (SocketException) { return false; }
        }
        private static void StopFixture(Process process) {
            if (process == null) return;
            try { if (!process.HasExited) { process.Kill(); process.WaitForExit(5000); } } finally { process.Dispose(); }
        }
        private static int Main(string[] args) {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") { Console.WriteLine("SKIP: shutdown tests require disposable Windows CI"); return 0; }
            const string keyPath = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings";
            string installed = Path.Combine(AppPaths.Root, "ProGo.exe"), scripts = Path.Combine(AppPaths.Root, "scripts");
            string startup = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "ProGo.lnk");
            string menu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "ProGo");
            if (File.Exists(installed) || Directory.Exists(scripts) || File.Exists(startup) || Directory.Exists(menu) ||
                File.Exists(SystemProxyService.BackupPath) || File.Exists(CliProxyEnvironmentService.BackupPath) || Process.GetProcessesByName("ProGo").Length != 0)
                throw new Exception("Shutdown fixture is not isolated");
            var beforeWindows = SystemProxyService.ReadCurrent();
            var beforeEnvironment = CliProxyEnvironmentService.Names.ToDictionary(n => n, n => Environment.GetEnvironmentVariable(n, EnvironmentVariableTarget.User));
            byte[] beforeSettings = File.Exists(AppPaths.SettingsPath) ? File.ReadAllBytes(AppPaths.SettingsPath) : null;
            var listener = new TcpListener(IPAddress.Loopback, 0); Process primary = null;
            try {
                AppPaths.EnsureDirectories(); Directory.CreateDirectory(scripts);
                foreach (string file in new[] { "Uninstall-ProGo.ps1", "Maintenance-ProGo.ps1", "MaintenanceOperation.cs" }) File.Copy(Path.Combine(args[1], file), Path.Combine(scripts, file));
                string uninstall = Path.Combine(scripts, "Uninstall-ProGo.ps1");
                File.Copy(args[0], installed);
                Directory.CreateDirectory(Path.GetDirectoryName(startup)); Directory.CreateDirectory(menu);
                File.WriteAllText(startup, "synthetic startup fixture"); File.WriteAllText(Path.Combine(menu, "ProGo.lnk"), "synthetic menu fixture");
                foreach (var name in CliProxyEnvironmentService.Names) Environment.SetEnvironmentVariable(name, null, EnvironmentVariableTarget.User);
                Environment.SetEnvironmentVariable("HTTPS_PROXY", "http://prior.example.org:8080", EnvironmentVariableTarget.User);
                using (var key = Registry.CurrentUser.CreateSubKey(keyPath)) {
                    key.SetValue("ProxyEnable", 0, RegistryValueKind.DWord);
                    key.SetValue("ProxyServer", "prior.example.org:8080", RegistryValueKind.String);
                    key.SetValue("ProxyOverride", "prior.example.org", RegistryValueKind.String);
                    key.SetValue("AutoDetect", 0, RegistryValueKind.DWord); key.DeleteValue("AutoConfigURL", false);
                }
                var baseline = SystemProxyService.ReadCurrent();
                var environment = CliProxyEnvironmentService.Names.ToDictionary(n => n, n => Environment.GetEnvironmentVariable(n, EnvironmentVariableTarget.User));
                using (var lease = MaintenanceOperation.Enter())
                    Check(Uninstall(uninstall) != 0 && File.Exists(installed) && File.Exists(startup), "competing uninstall refuses before touching application or shortcuts");

                listener.Start();
                System.Threading.Tasks.Task.Run(async delegate {
                    try { while (true) using (var client = await listener.AcceptTcpClientAsync()) {
                        var stream = client.GetStream(); stream.ReadTimeout = 1500;
                        if (stream.ReadByte() == 5 && stream.ReadByte() == 1 && stream.ReadByte() == 0) stream.Write(new byte[] { 5, 0 }, 0, 2);
                    } } catch (SocketException) { } catch (ObjectDisposedException) { }
                });
                var prefs = AppSettings.Defaults(); prefs.AutoCliProxy = true; prefs.AutoSystemProxy = true;
                prefs.AutoStartSocks = false; prefs.AutoRestartSocks = false;
                prefs.SocksPort = ((IPEndPoint)listener.LocalEndpoint).Port; prefs.TestEndpoint = "http://127.0.0.1:1/";
                var reserved = new TcpListener(IPAddress.Loopback, 0); reserved.Start(); prefs.HttpProxyPort = ((IPEndPoint)reserved.LocalEndpoint).Port; reserved.Stop();
                using (var settings = new SettingsService()) settings.Save(prefs);
                primary = Process.Start(new ProcessStartInfo(installed) { UseShellExecute = false, CreateNoWindow = true });
                Wait(() => !primary.HasExited && File.Exists(SystemProxyService.BackupPath) && SystemProxyService.IsApplied(prefs) &&
                    CliProxyEnvironmentService.IsAppliedToUserEnvironment(prefs.HttpProxyPort) && PortOpen(prefs.HttpProxyPort), "actual proxy modes ready");
                string settingsBefore = File.ReadAllText(AppPaths.SettingsPath);
                using (var locked = File.Open(SystemProxyService.BackupPath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    Check(Uninstall(uninstall) != 0, "actual uninstall reports refused cleanup of a locked journal");
                    Check(!primary.HasExited && PortOpen(prefs.HttpProxyPort) && File.Exists(installed) && File.Exists(startup) && Directory.Exists(menu),
                        "failed uninstall retains running local proxy, executable and both shortcuts");
                    Check(File.ReadAllText(AppPaths.SettingsPath) == settingsBefore && File.Exists(SystemProxyService.BackupPath), "failed uninstall preserves preferences and retry journal");
                }
                Check(Uninstall(uninstall) == 0 && primary.WaitForExit(10000), "retry receives IPC cleanup confirmation and waits for the actual owner to exit");
                primary.Dispose(); primary = null;
                Check(!File.Exists(installed) && !File.Exists(startup) && !Directory.Exists(menu) && File.Exists(AppPaths.SettingsPath), "successful uninstall removes executable/shortcuts and retains user data");
                Check(!File.Exists(SystemProxyService.BackupPath) && !File.Exists(CliProxyEnvironmentService.BackupPath) && !PortOpen(prefs.HttpProxyPort), "successful uninstall leaves no owned backup or dead local listener");
                foreach (var name in SystemProxyService.FieldNames) Check(SystemProxyService.ReadCurrent().Values[name].Matches(baseline.Values[name]), "uninstall restores original Windows field: " + name);
                foreach (var pair in environment) Check(Environment.GetEnvironmentVariable(pair.Key, EnvironmentVariableTarget.User) == pair.Value, "new-terminal user environment restored: " + pair.Key);

                File.Copy(args[0], installed);
                primary = Process.Start(new ProcessStartInfo(installed) { UseShellExecute = false, CreateNoWindow = true });
                Wait(() => !primary.HasExited && File.Exists(SystemProxyService.BackupPath) && SystemProxyService.IsApplied(prefs) && CliProxyEnvironmentService.IsAppliedToUserEnvironment(prefs.HttpProxyPort), "external-edit fixture ready");
                using (var key = Registry.CurrentUser.OpenSubKey(keyPath, true)) {
                    key.SetValue("ProxyServer", "external.example.org:9090", RegistryValueKind.String);
                    key.SetValue("ProxyOverride", "external.example.org", RegistryValueKind.String);
                    key.SetValue("AutoConfigURL", "https://external.example.org/proxy.pac", RegistryValueKind.String);
                }
                Environment.SetEnvironmentVariable("HTTP_PROXY", CliProxyBridgeService.UrlFor(1881), EnvironmentVariableTarget.User);
                var external = SystemProxyService.ReadCurrent();
                Check(Uninstall(uninstall) == 0 && primary.WaitForExit(10000), "uninstall also exits cleanly after later external proxy changes");
                primary.Dispose(); primary = null;
                foreach (var name in SystemProxyService.FieldNames) Check(SystemProxyService.ReadCurrent().Values[name].Matches(external.Values[name]), "external Windows field survives uninstall: " + name);
                Check(Environment.GetEnvironmentVariable("HTTP_PROXY", EnvironmentVariableTarget.User) == CliProxyBridgeService.UrlFor(1881), "later terminal proxy at the old default port survives repeated uninstall cleanup");

                File.Copy(args[0], installed); File.WriteAllText(SystemProxyService.BackupPath, "{}");
                Check(Uninstall(uninstall) != 0 && File.Exists(installed) && File.Exists(SystemProxyService.BackupPath), "stopped/crashed application with unresolved cleanup cannot be uninstalled");
                File.Delete(SystemProxyService.BackupPath); // Isolated synthetic journal only.
                using (var owner = new ApplicationInstance()) {
                    owner.Attach(() => { }); // Startup owner has not attached a cleanup handler.
                    Check(Uninstall(uninstall) != 0 && File.Exists(installed), "missing cleanup handler cannot masquerade as successful removal");
                }
                Console.WriteLine("Shutdown tests PASS: " + passed); return 0;
            } catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
            finally {
                StopFixture(primary); listener.Stop();
                CliProxyEnvironmentService.ClearUserEnvironmentIfOwned();
                foreach (var pair in beforeEnvironment) Environment.SetEnvironmentVariable(pair.Key, pair.Value, EnvironmentVariableTarget.User);
                SystemProxyService.RestoreSnapshot(beforeWindows);
                if (File.Exists(SystemProxyService.BackupPath)) File.Delete(SystemProxyService.BackupPath);
                if (beforeSettings == null) File.Delete(AppPaths.SettingsPath); else File.WriteAllBytes(AppPaths.SettingsPath, beforeSettings);
                File.Delete(installed); File.Delete(startup);
                if (Directory.Exists(menu)) Directory.Delete(menu, true);
                if (Directory.Exists(scripts)) Directory.Delete(scripts, true);
            }
        }
    }
}

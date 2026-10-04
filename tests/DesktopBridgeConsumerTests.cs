using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        private static void AssertPortReleased(int port, string message)
        {
            var released = Occupy(port);
            try { Check(true, message); } finally { released.Stop(); }
        }
        private static void SharedBridgeConsumers(SettingsService settings)
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") return;
            var original = settings.Current.Clone();
            var registry = SystemProxyService.ReadCurrent();
            var environment = CliProxyEnvironmentService.Names.ToDictionary(n => n, n => Environment.GetEnvironmentVariable(n, EnvironmentVariableTarget.User));
            if (File.Exists(CliProxyEnvironmentService.BackupPath) || File.Exists(SystemProxyService.BackupPath) || CodexProxyService.IsOwned) throw new Exception("Bridge fixture is not isolated");
            try {
                foreach (var name in CliProxyEnvironmentService.Names) Environment.SetEnvironmentVariable(name, null, EnvironmentVariableTarget.User);
                var prefs = original.Clone(); prefs.AutoCliProxy = prefs.AutoSystemProxy = false; settings.Save(prefs);
                using (var proxy = new ProxyService(() => settings.Current, s => settings.Save(s), "unused-test-ssh", () => DateTime.UtcNow, false))
                using (var bridge = new CliProxyBridgeService(settings))
                using (var relay = new Ikev2RelayService())
                using (var home = new HomeVpnService(relay))
                using (var clipboard = new ClipboardService(settings))
                using (var context = new UpdateAwareTrayApplicationContext(settings, proxy, bridge, home, clipboard, false)) {
                    context.RequestShowStatus(); var main = (MainWindow)Field(context, "mainWindow");
                    var consumers = (AppProxyConsumers)Field(context, "appConsumers");
                    Call(context, "EnableFeature", ProxyFeature.Cli); Call(context, "EnableFeature", ProxyFeature.Windows);
                    int port = bridge.Port;
                    Call(context, "Execute", "cli-off");
                    Check(bridge.IsRunning && SystemProxyService.IsApplied(settings.Current) && !CliProxyEnvironmentService.HasProxyEndpoint(port), "CLI off retains the independent Windows consumer");
                    AssertBridge(port); main.RefreshConnectionState();
                    Check(((Button)Field(main, "cliToggle")).Text == "Запустить CLI" && ((Label)Field(main, "recovery")).Text.Contains("работает для: Windows"), "dashboard distinguishes CLI off from the shared Windows service");
                    Shot(main, "main-cli-off-windows-retained");
                    Call(context, "Execute", "windows-off");
                    Check(!bridge.IsRunning, "Windows off releases the last consumer");
                    AssertPortReleased(port, "last Windows off frees the actual listening port");
                    Call(context, "EnableFeature", ProxyFeature.Cli); Call(context, "EnableFeature", ProxyFeature.Windows);
                    Call(context, "Execute", "windows-off");
                    Check(bridge.IsRunning && CliProxyEnvironmentService.HasProxyEndpoint(bridge.Port), "Windows off preserves ordinary CLI");
                    Call(context, "Execute", "cli-off");
                    Check(!bridge.IsRunning, "reverse consumer release also stops the last listener");

                    // A malformed/pending journal is retained until an explicit successful cleanup.
                    string error; Check(bridge.Start(out error), "cleanup journal fixture starts listener"); consumers.Observe();
                    File.WriteAllText(CliProxyEnvironmentService.BackupPath, "invalid fixture journal"); consumers.ReleaseIfUnused();
                    Check(bridge.IsRunning, "uncertain CLI cleanup retains the listener rather than cutting access");
                    var warning = CleanupDialog(context, "Execute", "cli-off");
                    Check(warning.Length > 0 && consumers.CliCleanupPending, "failed actual CLI off records an explicit pending cleanup consumer");
                    File.Delete(CliProxyEnvironmentService.BackupPath); consumers.ReleaseIfUnused();
                    Check(bridge.IsRunning, "cleanup failure keeps access even if its journal becomes unavailable");
                    Call(context, "Execute", "cli-off");
                    Check(!bridge.IsRunning && !consumers.CliCleanupPending, "successful explicit cleanup retry releases the retained service");
                    Check(bridge.Start(out error), "NO_PROXY fixture starts listener"); consumers.Observe();
                    Environment.SetEnvironmentVariable("NO_PROXY", "localhost,127.0.0.1,::1", EnvironmentVariableTarget.User);
                    consumers.ReleaseIfUnused();
                    Check(!bridge.IsRunning, "NO_PROXY alone is not an HTTP proxy consumer");
                    Environment.SetEnvironmentVariable("NO_PROXY", null, EnvironmentVariableTarget.User);

                    Check(bridge.Start(out error), "partial environment fixture starts listener");
                    Environment.SetEnvironmentVariable("HTTPS_PROXY", bridge.ProxyUrl, EnvironmentVariableTarget.User); consumers.ReleaseIfUnused();
                    Check(bridge.IsRunning, "even one actual proxy endpoint retains the service");
                    Environment.SetEnvironmentVariable("HTTPS_PROXY", null, EnvironmentVariableTarget.User); consumers.ReleaseIfUnused();
                    Check(!bridge.IsRunning, "external removal of the last endpoint releases the service");

                    Call(context, "EnableFeature", ProxyFeature.Cli); CodexProxyService.Enable(bridge.Port);
                    Call(context, "Execute", "cli-off");
                    Check(bridge.IsRunning && consumers.Summary == "ярлык Codex", "owned optional shortcut retains its shared endpoint without CLI environment");
                    Call(context, "Execute", "codex-shortcut-off");
                    Check(!bridge.IsRunning && !CodexProxyService.IsOwned, "removing the last shortcut releases the endpoint");

                    // Exercise the actual scoped PowerShell launch and process-handle handoff.
                    Check(bridge.Start(out error), "scoped window fixture starts listener");
                    Process window;
                    Check(CliProxyEnvironmentService.OpenPowerShellWithEnvironment(bridge.Port, out error, out window) && window != null, "scoped launch returns the actual child process handle");
                    consumers.TrackWindow(window);
                    using (var observer = Process.GetProcessById(window.Id)) {
                        try {
                            Call(context, "Execute", "cli-off");
                            Check(bridge.IsRunning && consumers.WindowCount == 1 && consumers.Summary == "отдельные окна: 1", "CLI off preserves the live explicitly opened terminal");
                            AssertBridge(bridge.Port);
                            // Only terminate this disposable fixture's freshly launched child.
                            observer.Kill(); Check(observer.WaitForExit(5000), "scoped fixture child exits");
                            PumpUntil(() => !bridge.IsRunning);
                            Check(consumers.WindowCount == 0, "UI timer frees the listener after the last tracked window closes");
                        } finally { if (!observer.HasExited) { observer.Kill(); observer.WaitForExit(5000); } }
                    }
                    AssertPortReleased(settings.Current.HttpProxyPort, "last tracked window releases the port");

                    Check(bridge.Start(out error), "full stop window fixture starts listener");
                    Check(CliProxyEnvironmentService.OpenPowerShellWithEnvironment(bridge.Port, out error, out window), "full stop fixture opens a scoped child");
                    consumers.TrackWindow(window);
                    using (var observer = Process.GetProcessById(window.Id)) {
                        try {
                            Call(context, "Execute", "stop");
                            Check(!bridge.IsRunning && consumers.WindowCount == 0 && !observer.HasExited, "explicit full desktop stop closes the service without killing the user's terminal");
                            main.RefreshConnectionState();
                            Check(((Label)Field(main, "recovery")).Text.Contains("служба остановлена"), "dashboard honestly reports a stopped shared service");
                            Shot(main, "main-shared-proxy-stopped");
                        } finally { if (!observer.HasExited) { observer.Kill(); observer.WaitForExit(5000); } }
                    }
                    main.Close();
                }
            } finally {
                CodexProxyService.Disable();
                if (File.Exists(CliProxyEnvironmentService.BackupPath)) File.Delete(CliProxyEnvironmentService.BackupPath);
                SystemProxyService.RestoreSnapshot(registry);
                if (File.Exists(SystemProxyService.BackupPath)) File.Delete(SystemProxyService.BackupPath);
                foreach (var pair in environment) Environment.SetEnvironmentVariable(pair.Key, pair.Value, EnvironmentVariableTarget.User);
                settings.Save(original);
            }
        }
    }
}

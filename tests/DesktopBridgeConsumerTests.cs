using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        private static void AssertPortReleased(int port, string message)
        {
            try
            {
                var released = Occupy(port);
                try { Check(true, message); } finally { released.Stop(); }
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                // Keep the same strict rebind assertion; collect evidence before
                // PowerShell truncates a native stderr exception to its first line.
                Console.WriteLine("Port rebind failed: " + port + "; " + message + "; " + ex.SocketErrorCode);
                var network = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties();
                foreach (var endpoint in network.GetActiveTcpListeners().Where(e => e.Port == port))
                    Console.WriteLine("Remaining listener: " + endpoint);
                foreach (var connection in network.GetActiveTcpConnections().Where(c => c.LocalEndPoint.Port == port || c.RemoteEndPoint.Port == port))
                    Console.WriteLine("Remaining connection: " + connection.LocalEndPoint + " -> " + connection.RemoteEndPoint + "; " + connection.State);
                Console.WriteLine(ex.ToString());
                throw;
            }
        }
        private static void SettleConsumers(AppProxyConsumers consumers, CliProxyBridgeService bridge)
        {
            PumpUntil(delegate { consumers.ReleaseIfUnused(); return !bridge.IsRunning || !consumers.ObservationPending; });
        }
        private static void ConsumerObserverRegressions(SettingsService settings)
        {
            var saved = settings.Current.Clone();
            try {
                var automatic = saved.Clone(); automatic.AutoHttpProxyPort = true; settings.Save(automatic);
                ConsumerObserverMemoryAndClock(settings);
                ConsumerObserverExpiredEmpty(settings);
                ConsumerObserverStaleResult(settings);
                ConsumerObserverDispose(settings);
                ConsumerObserverByteBudget();
                ConsumerObserverReparse(settings);
                ConsumerObserverFilesystem(settings);
            } finally { settings.Save(saved); }
        }
        private static void ConsumerObserverMemoryAndClock(SettingsService settings)
        {
            int reads = 0, ticks = 0;
            var clock = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var bridge = new CliProxyBridgeService(settings))
            using (var consumers = new AppProxyConsumers(bridge, settings, delegate(AppSettings captured, int port) {
                Interlocked.Increment(ref reads); entered.Set(); release.Wait();
                return new AppProxyConsumerSnapshot(false, false, true);
            }, () => clock))
            using (var heartbeat = new System.Windows.Forms.Timer { Interval = 15 }) {
                try {
                    string error; Check(bridge.Start(out error), "consumer observer fixture binds its actual bridge");
                    consumers.Observe(); consumers.ReleaseIfUnused(); PumpUntil(() => entered.IsSet);
                    for (int i = 0; i < 1000; i++) { var summary = consumers.Summary; consumers.ReleaseIfUnused(); }
                    heartbeat.Tick += delegate { ticks++; }; heartbeat.Start(); PumpUntil(() => ticks >= 3);
                    Check(Volatile.Read(ref reads) == 1 && bridge.IsRunning && consumers.Summary.Contains("проверяем"),
                        "1000 summary/release calls and native UI ticks do not wait or duplicate a blocked integration read");
                    // Advancing the clock while blocked still cannot spawn additional readers.
                    clock = clock.AddMinutes(1); consumers.ReleaseIfUnused();
                    Check(Volatile.Read(ref reads) == 1, "elapsed observer deadline does not accumulate threads behind blocked I/O");
                    release.Set(); SettleConsumers(consumers, bridge);
                    Check(consumers.Summary.Contains("нужна проверка") && Volatile.Read(ref reads) == 1, "old completed observation publishes uncertainty instead of stale owned status");
                    for (int i = 0; i < 1000; i++) { var summary = consumers.Summary; consumers.ReleaseIfUnused(); }
                    clock = clock.AddSeconds(9); consumers.ReleaseIfUnused();
                    Check(Volatile.Read(ref reads) == 1, "unchanged integration is not reread for nine seconds or dashboard refreshes");
                    clock = clock.AddSeconds(1); SettleConsumers(consumers, bridge);
                    Check(Volatile.Read(ref reads) == 2 && consumers.Summary == "ярлык Codex", "fresh external observation recovers owned status once at its ten-second interval");
                    consumers.Invalidate(); SettleConsumers(consumers, bridge);
                    Check(Volatile.Read(ref reads) == 3 && bridge.IsRunning, "explicit integration mutation immediately refreshes its cached ownership");
                } finally { release.Set(); heartbeat.Stop(); }
            }
        }
        private static void ConsumerObserverExpiredEmpty(SettingsService settings)
        {
            int reads = 0, desired = 0;
            var clock = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var bridge = new CliProxyBridgeService(settings))
            using (var consumers = new AppProxyConsumers(bridge, settings, delegate(AppSettings captured, int port) {
                bool owned = Volatile.Read(ref desired) != 0;
                if (Interlocked.Increment(ref reads) == 1) { entered.Set(); release.Wait(); }
                return new AppProxyConsumerSnapshot(false, false, owned);
            }, () => clock)) {
                try {
                    string error; Check(bridge.Start(out error), "expired empty observation fixture binds its actual listener");
                    int retainedPort = bridge.Port; long revision = bridge.ConsumerRevision;
                    consumers.Observe(); consumers.ReleaseIfUnused(); PumpUntil(() => entered.IsSet);
                    // Simulate an external consumer arriving during blocked I/O:
                    // no ProGo invalidation or bridge revision accompanies it.
                    Interlocked.Exchange(ref desired, 1); clock = clock.AddSeconds(6); release.Set();
                    SettleConsumers(consumers, bridge);
                    Check(bridge.IsRunning && bridge.Port == retainedPort && bridge.ConsumerRevision == revision && reads == 1 &&
                        consumers.Summary.Contains("нужна проверка"),
                        "expired captured-empty result cannot stop an externally needed listener without invalidation");
                    clock = clock.AddSeconds(9); consumers.ReleaseIfUnused();
                    Check(reads == 1 && bridge.IsRunning, "expired result waits the bounded retry interval without another reader");
                    clock = clock.AddSeconds(1); SettleConsumers(consumers, bridge);
                    Check(reads == 2 && bridge.IsRunning && consumers.Summary == "ярлык Codex",
                        "fresh observation confirms the external consumer after stale empty evidence is discarded");
                } finally { release.Set(); }
            }
        }
        private static void ConsumerObserverStaleResult(SettingsService settings)
        {
            int reads = 0, desired = 0;
            var clock = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            using (var firstEntered = new ManualResetEventSlim())
            using (var firstRelease = new ManualResetEventSlim())
            using (var portEntered = new ManualResetEventSlim())
            using (var portRelease = new ManualResetEventSlim())
            using (var sessionEntered = new ManualResetEventSlim())
            using (var sessionRelease = new ManualResetEventSlim())
            using (var bridge = new CliProxyBridgeService(settings))
            using (var consumers = new AppProxyConsumers(bridge, settings, delegate(AppSettings captured, int port) {
                int call = Interlocked.Increment(ref reads); bool owned = Volatile.Read(ref desired) != 0;
                if (call == 1) { firstEntered.Set(); firstRelease.Wait(); }
                if (call == 3) { portEntered.Set(); portRelease.Wait(); }
                if (call == 5) { sessionEntered.Set(); sessionRelease.Wait(); }
                return new AppProxyConsumerSnapshot(false, false, owned);
            }, () => clock)) {
                try {
                    string error; Check(bridge.Start(out error), "stale consumer fixture starts");
                    consumers.Observe(); consumers.ReleaseIfUnused(); PumpUntil(() => firstEntered.IsSet);
                    Interlocked.Exchange(ref desired, 1); consumers.Invalidate(); firstRelease.Set(); SettleConsumers(consumers, bridge);
                    Check(bridge.IsRunning && reads == 2 && consumers.Summary == "ярлык Codex", "stale empty observation cannot stop a newly invalidated owned consumer");

                    Interlocked.Exchange(ref desired, 0); clock = clock.AddSeconds(10); consumers.ReleaseIfUnused(); PumpUntil(() => portEntered.IsSet);
                    int previousPort = bridge.Port;
                    Interlocked.Exchange(ref desired, 1);
                    var proposed = settings.Current.Clone(); Check(bridge.Reconfigure(proposed, true, out error) && bridge.Port != previousPort, "stale observation fixture changes the real listening port");
                    consumers.ReleaseIfUnused(); portRelease.Set(); SettleConsumers(consumers, bridge);
                    Check(bridge.IsRunning && reads == 4 && consumers.Summary == "ярлык Codex", "old-port empty result cannot release the replacement listener");

                    Interlocked.Exchange(ref desired, 0); clock = clock.AddSeconds(10); consumers.ReleaseIfUnused(); PumpUntil(() => sessionEntered.IsSet);
                    int retainedPort = bridge.Port;
                    bridge.Stop(); Check(bridge.Start(out error) && bridge.Port == retainedPort, "consumer fixture restarts a new listener on the same port");
                    Interlocked.Exchange(ref desired, 1); consumers.ReleaseIfUnused(); sessionRelease.Set(); SettleConsumers(consumers, bridge);
                    Check(bridge.IsRunning && reads == 6 && consumers.Summary == "ярлык Codex", "old-session empty result cannot stop a same-port replacement listener");
                } finally { firstRelease.Set(); portRelease.Set(); sessionRelease.Set(); }
            }
        }
        private static void ConsumerObserverDispose(SettingsService settings)
        {
            int reads = 0;
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var finished = new ManualResetEventSlim())
            using (var bridge = new CliProxyBridgeService(settings)) {
                string error; Check(bridge.Start(out error), "disposed observer fixture starts");
                var consumers = new AppProxyConsumers(bridge, settings, delegate(AppSettings captured, int port) {
                    Interlocked.Increment(ref reads); entered.Set(); release.Wait(); finished.Set();
                    return new AppProxyConsumerSnapshot(false, false, false);
                });
                try {
                    consumers.Observe(); consumers.ReleaseIfUnused(); PumpUntil(() => entered.IsSet);
                    consumers.Dispose();
                    Check(bridge.IsRunning, "consumer disposal returns while its reader is blocked and does not stop the user's service");
                    release.Set(); PumpUntil(() => finished.IsSet); consumers.ReleaseIfUnused();
                    Check(bridge.IsRunning && reads == 1, "late empty observer result after disposal cannot mutate the listener or schedule another read");
                } finally { release.Set(); consumers.Dispose(); }
            }
        }
        private sealed class ConsumerByteProbe : MemoryStream
        {
            internal long ReportedLength;
            internal int BytesRead;
            internal bool GrowAfterRead;
            internal ConsumerByteProbe(byte[] bytes, long reportedLength) : base(bytes, false) { ReportedLength = reportedLength; }
            public override long Length { get { return ReportedLength; } }
            public override int Read(byte[] buffer, int offset, int count)
            {
                int received = base.Read(buffer, offset, count); BytesRead += received;
                if (GrowAfterRead) ReportedLength++;
                return received;
            }
        }
        private static void ConsumerObserverByteBudget()
        {
            string launcher = CodexProxyService.LauncherContent(31881);
            var prefix = System.Text.Encoding.ASCII.GetBytes(launcher);
            using (var large = new ConsumerByteProbe(prefix, 8 * 1024 * 1024)) {
                bool refused = false;
                try { AppProxyConsumers.ReadCodexOwnership(large); } catch (IOException) { refused = true; }
                Check(refused && large.BytesRead == 0, "oversized marker-bearing launcher is refused before any data read, independent of its file size");
            }
            var boundary = System.Text.Encoding.ASCII.GetBytes(launcher.PadRight(64 * 1024, ' '));
            using (var allowed = new ConsumerByteProbe(boundary, boundary.Length))
                Check(AppProxyConsumers.ReadCodexOwnership(allowed) && allowed.BytesRead == 64 * 1024,
                    "observer accepts the generated ownership signature at the exact 64 KiB byte boundary");
            using (var growing = new ConsumerByteProbe(boundary, boundary.Length) { GrowAfterRead = true }) {
                bool refused = false;
                try { AppProxyConsumers.ReadCodexOwnership(growing); } catch (IOException) { refused = true; }
                Check(refused && growing.BytesRead == 64 * 1024, "growth during observation remains uncertain without reading beyond the byte budget");
            }
            var unicode = System.Text.Encoding.Unicode.GetPreamble().Concat(System.Text.Encoding.Unicode.GetBytes(launcher)).ToArray();
            using (var bom = new MemoryStream(unicode, false))
                Check(AppProxyConsumers.ReadCodexOwnership(bom), "bounded observer retains BOM ownership compatibility with the fresh File.ReadAllText checks");
        }
        private static void ConsumerObserverReparse(SettingsService settings)
        {
            string root = Path.Combine(work, "consumer-junction-" + Guid.NewGuid().ToString("N"));
            string target = Path.Combine(root, "target"), junction = Path.Combine(root, "linked");
            try {
                Directory.CreateDirectory(target);
                File.WriteAllText(Path.Combine(target, "Codex-ProGo.cmd"), CodexProxyService.LauncherContent(31881));
                var start = new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe"),
                    "/D /C mklink /J \"" + junction + "\" \"" + target + "\"") {
                    UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true
                };
                using (var command = Process.Start(start)) {
                    bool exited = command.WaitForExit(5000);
                    if (!exited) { command.Kill(); command.WaitForExit(5000); }
                    Check(exited && command.ExitCode == 0 && (File.GetAttributes(junction) & FileAttributes.ReparsePoint) != 0,
                        "consumer fixture creates an actual Windows junction without changing system configuration");
                }
                using (var bridge = new CliProxyBridgeService(settings))
                using (var consumers = new AppProxyConsumers(bridge, settings, delegate(AppSettings captured, int port) {
                    return new AppProxyConsumerSnapshot(false, false,
                        AppProxyConsumers.ReadCodexOwnership(Path.Combine(junction, "Codex-ProGo.cmd")));
                })) {
                    string error; Check(bridge.Start(out error), "junction observer fixture binds its actual listener");
                    int port = bridge.Port; long revision = bridge.ConsumerRevision;
                    consumers.Observe(); SettleConsumers(consumers, bridge);
                    Check(bridge.IsRunning && bridge.Port == port && bridge.ConsumerRevision == revision &&
                        consumers.Summary == "потребители прокси: нужна проверка",
                        "reparse parent with a real owned launcher yields safe uncertainty and retains the unchanged listener");
                }
            } finally {
                // Remove the junction itself before recursively deleting our data.
                if (Directory.Exists(junction)) Directory.Delete(junction);
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }
        private static void ConsumerObserverFilesystem(SettingsService settings)
        {
            int reads = 0;
            var clock = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            using (var bridge = new CliProxyBridgeService(settings))
            using (var consumers = new AppProxyConsumers(bridge, settings, delegate(AppSettings captured, int port) {
                Interlocked.Increment(ref reads); return AppProxyConsumers.ReadConsumers(captured, port);
            }, () => clock)) {
                try {
                    string error; Check(bridge.Start(out error), "locked launcher observer fixture starts");
                    CodexProxyService.Enable(bridge.Port);
                    using (var held = new FileStream(CodexProxyService.LauncherPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) {
                        consumers.Observe(); SettleConsumers(consumers, bridge);
                        Check(bridge.IsRunning && consumers.Summary.Contains("нужна проверка"), "actual locked Codex launcher becomes visible uncertain ownership and retains its listener");
                        for (int i = 0; i < 1000; i++) { var summary = consumers.Summary; consumers.ReleaseIfUnused(); }
                        clock = clock.AddSeconds(9); consumers.ReleaseIfUnused();
                        Check(reads == 1 && bridge.IsRunning, "locked launcher is not reopened by UI refreshes or rapid failed-observation retries");
                        clock = clock.AddSeconds(1); SettleConsumers(consumers, bridge);
                        Check(reads == 2 && bridge.IsRunning && consumers.Summary.Contains("нужна проверка"), "failed ownership observation retries only at its bounded interval");
                    }
                    clock = clock.AddSeconds(10); SettleConsumers(consumers, bridge);
                    Check(reads == 3 && consumers.Summary == "ярлык Codex", "released real file lock recovers without restarting ProGo");
                    int retainedPort = bridge.Port; long retainedRevision = bridge.ConsumerRevision;
                    using (var large = new FileStream(CodexProxyService.LauncherPath, FileMode.Open, FileAccess.Write, FileShare.None))
                        large.SetLength(8 * 1024 * 1024);
                    clock = clock.AddSeconds(10); SettleConsumers(consumers, bridge);
                    Check(reads == 4 && bridge.IsRunning && bridge.Port == retainedPort && bridge.ConsumerRevision == retainedRevision &&
                        consumers.Summary == "потребители прокси: нужна проверка" && new FileInfo(CodexProxyService.LauncherPath).Length == 8 * 1024 * 1024,
                        "actual oversized external file retaining the ProGo signature is unknown, unchanged, and cannot release its listener");
                    for (int i = 0; i < 1000; i++) { var summary = consumers.Summary; consumers.ReleaseIfUnused(); }
                    clock = clock.AddSeconds(9); consumers.ReleaseIfUnused();
                    Check(reads == 4 && bridge.IsRunning, "oversized ownership failure does not reread the file during UI refreshes or rapid retries");
                    File.WriteAllText(CodexProxyService.LauncherPath, CodexProxyService.LauncherContent(bridge.Port));
                    clock = clock.AddSeconds(1); SettleConsumers(consumers, bridge);
                    Check(reads == 5 && bridge.IsRunning && consumers.Summary == "ярлык Codex", "normal-size owned launcher recovers at the bounded retry without restarting the listener");
                    const string external = "@echo controlled external launcher fixture";
                    File.WriteAllText(CodexProxyService.LauncherPath, external);
                    CodexProxyService.MoveOwned(31881); CodexProxyService.Disable();
                    Check(File.ReadAllText(CodexProxyService.LauncherPath) == external, "destructive shortcut actions use fresh ownership and preserve an external replacement despite cached owned status");
                    clock = clock.AddSeconds(9); consumers.ReleaseIfUnused();
                    Check(bridge.IsRunning && reads == 5, "external change retains the listener until the next bounded read");
                    clock = clock.AddSeconds(1); SettleConsumers(consumers, bridge);
                    Check(!bridge.IsRunning && reads == 6 && File.ReadAllText(CodexProxyService.LauncherPath) == external,
                        "periodic observation detects external launcher replacement and releases only the confirmed last consumer");
                } finally {
                    // The fixture created both files; restore its marker so the
                    // normal ownership cleanup can remove its controlled shortcut.
                    if (File.Exists(CodexProxyService.LauncherPath)) File.WriteAllText(CodexProxyService.LauncherPath, CodexProxyService.LauncherContent(settings.Current.HttpProxyPort));
                    CodexProxyService.Disable();
                }
            }
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
                ConsumerObserverRegressions(settings);
                using (var proxy = new ProxyService(() => settings.Current, s => settings.Save(s), "unused-test-ssh", () => DateTime.UtcNow, false))
                using (var bridge = new CliProxyBridgeService(settings))
                using (var relay = new Ikev2RelayService())
                using (var home = new HomeVpnService(relay))
                using (var clipboard = new ClipboardService(settings))
                using (var context = new UpdateAwareTrayApplicationContext(settings, proxy, bridge, home, clipboard, false)) {
                    context.RequestShowStatus(); var main = (MainWindow)Field(context, "mainWindow");
                    var consumers = (AppProxyConsumers)Field(context, "appConsumers");
                    Call(context, "EnableFeature", ProxyFeature.Cli); WaitIntegration(context); Call(context, "EnableFeature", ProxyFeature.Windows); WaitIntegration(context);
                    int port = bridge.Port;
                    Call(context, "Execute", "cli-off"); WaitIntegration(context);
                    SettleConsumers(consumers, bridge);
                    Check(bridge.IsRunning && SystemProxyService.IsApplied(settings.Current) && !CliProxyEnvironmentService.HasProxyEndpoint(port), "CLI off retains the independent Windows consumer");
                    AssertBridge(port); main.RefreshConnectionState();
                    Check(((Button)Field(main, "cliToggle")).Text == "Запустить CLI" && ((Label)Field(main, "recovery")).Text.Contains("работает для: Windows"), "dashboard distinguishes CLI off from the shared Windows service");
                    main.Refresh(); Shot(main, "main-cli-off-windows-retained");
                    Call(context, "Execute", "windows-off"); WaitIntegration(context);
                    SettleConsumers(consumers, bridge);
                    Check(!bridge.IsRunning, "Windows off releases the last consumer");
                    AssertPortReleased(port, "last Windows off frees the actual listening port");
                    Call(context, "EnableFeature", ProxyFeature.Cli); WaitIntegration(context); Call(context, "EnableFeature", ProxyFeature.Windows); WaitIntegration(context);
                    Call(context, "Execute", "windows-off"); WaitIntegration(context);
                    SettleConsumers(consumers, bridge);
                    Check(bridge.IsRunning && CliProxyEnvironmentService.HasProxyEndpoint(bridge.Port), "Windows off preserves ordinary CLI");
                    Call(context, "Execute", "cli-off"); WaitIntegration(context);
                    SettleConsumers(consumers, bridge);
                    Check(!bridge.IsRunning, "reverse consumer release also stops the last listener");

                    // A malformed/pending journal is retained until an explicit successful cleanup.
                    string error; Check(bridge.Start(out error), "cleanup journal fixture starts listener"); consumers.Observe();
                    File.WriteAllText(CliProxyEnvironmentService.BackupPath, "invalid fixture journal"); consumers.Invalidate(); SettleConsumers(consumers, bridge);
                    Check(bridge.IsRunning, "uncertain CLI cleanup retains the listener rather than cutting access");
                    var warning = CleanupDialog(context, "Execute", "cli-off");
                    Check(warning.Length > 0 && consumers.CliCleanupPending, "failed actual CLI off records an explicit pending cleanup consumer");
                    File.Delete(CliProxyEnvironmentService.BackupPath); consumers.Invalidate(); SettleConsumers(consumers, bridge);
                    Check(bridge.IsRunning, "cleanup failure keeps access even if its journal becomes unavailable");
                    Call(context, "Execute", "cli-off"); WaitIntegration(context);
                    SettleConsumers(consumers, bridge);
                    Check(!bridge.IsRunning && !consumers.CliCleanupPending, "successful explicit cleanup retry releases the retained service");
                    Check(bridge.Start(out error), "NO_PROXY fixture starts listener"); consumers.Observe();
                    Environment.SetEnvironmentVariable("NO_PROXY", "localhost,127.0.0.1,::1", EnvironmentVariableTarget.User);
                    consumers.Invalidate(); SettleConsumers(consumers, bridge);
                    Check(!bridge.IsRunning, "NO_PROXY alone is not an HTTP proxy consumer");
                    Environment.SetEnvironmentVariable("NO_PROXY", null, EnvironmentVariableTarget.User);

                    Check(bridge.Start(out error), "partial environment fixture starts listener");
                    Environment.SetEnvironmentVariable("HTTPS_PROXY", bridge.ProxyUrl, EnvironmentVariableTarget.User); consumers.Invalidate(); SettleConsumers(consumers, bridge);
                    Check(bridge.IsRunning, "even one actual proxy endpoint retains the service");
                    Environment.SetEnvironmentVariable("HTTPS_PROXY", null, EnvironmentVariableTarget.User); consumers.Invalidate(); SettleConsumers(consumers, bridge);
                    Check(!bridge.IsRunning, "external removal of the last endpoint releases the service");

                    Call(context, "EnableFeature", ProxyFeature.Cli); WaitIntegration(context); CodexProxyService.Enable(bridge.Port);
                    Call(context, "Execute", "cli-off"); WaitIntegration(context);
                    SettleConsumers(consumers, bridge);
                    Check(bridge.IsRunning && consumers.Summary == "ярлык Codex", "owned optional shortcut retains its shared endpoint without CLI environment");
                    Call(context, "Execute", "codex-shortcut-off");
                    SettleConsumers(consumers, bridge);
                    Check(!bridge.IsRunning && !CodexProxyService.IsOwned, "removing the last shortcut releases the endpoint");

                    // Exercise the actual scoped PowerShell launch and process-handle handoff.
                    Check(bridge.Start(out error), "scoped window fixture starts listener");
                    Process window;
                    Check(CliProxyEnvironmentService.OpenPowerShellWithEnvironment(bridge.Port, out error, out window) && window != null, "scoped launch returns the actual child process handle");
                    consumers.TrackWindow(window);
                    using (var observer = Process.GetProcessById(window.Id)) {
                        try {
                            Call(context, "Execute", "cli-off"); WaitIntegration(context);
                            SettleConsumers(consumers, bridge);
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
                            Call(context, "Execute", "stop"); WaitIntegration(context);
                            Check(!bridge.IsRunning && consumers.WindowCount == 0 && !observer.HasExited, "explicit full desktop stop closes the service without killing the user's terminal");
                            main.RefreshConnectionState();
                            Check(((Label)Field(main, "recovery")).Text.Contains("служба остановлена"), "dashboard honestly reports a stopped shared service");
                            main.Refresh(); Shot(main, "main-shared-proxy-stopped");
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

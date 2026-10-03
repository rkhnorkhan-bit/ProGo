using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        private static byte[] HealthRead(NetworkStream stream, int count)
        {
            var bytes = new byte[count]; int offset = 0;
            while (offset < count) { int read = stream.Read(bytes, offset, count - offset); if (read == 0) throw new EndOfStreamException(); offset += read; }
            return bytes;
        }
        private static void HealthRefresh(ConnectionHealthMonitor monitor)
        {
            monitor.RequestRefresh(true); PumpUntil(() => !(bool)Field(monitor, "busy"));
        }
        private static void HealthAcceptGreeting(NetworkStream stream)
        {
            var greeting = HealthRead(stream, 2);
            if (greeting[0] != 5 || !HealthRead(stream, greeting[1]).Contains((byte)0)) throw new IOException("Fixture requires SOCKS5 with no-auth method");
        }
        private static void HealthGreeting(byte[] reply, bool expected, string description, int delay = 0)
        {
            var listener = Occupy(0); var config = AppSettings.Defaults(); config.SocksHost = "127.0.0.1"; config.SocksPort = Number(listener);
            var server = Task.Run(delegate {
                try {
                    using (var client = listener.AcceptTcpClient()) {
                        var stream = client.GetStream(); stream.ReadTimeout = 2000;
                        HealthRead(stream, 3);
                        if (reply == null) { Thread.Sleep(900); return; }
                        foreach (var value in reply) { stream.WriteByte(value); if (delay > 0) Thread.Sleep(delay); }
                    }
                } catch (IOException) { } catch (SocketException) { }
            });
            try { Check(ConnectionHealthMonitor.CheckSocks(config, CancellationToken.None) == expected, description); Check(server.Wait(3000), "greeting fixture finishes: " + description); }
            finally { listener.Stop(); }
        }
        private static void HealthTransport()
        {
            HealthGreeting(new byte[] { 5, 0 }, true, "valid fragmented SOCKS greeting is protocol evidence", 30);
            HealthGreeting(new byte[] { 72, 84 }, false, "foreign HTTP listener cannot claim SOCKS readiness");
            HealthGreeting(new byte[] { 5, 255 }, false, "SOCKS method rejection is not readiness");
            HealthGreeting(new byte[] { 5 }, false, "truncated SOCKS greeting is not readiness");
            HealthGreeting(null, false, "silent open TCP port has a bounded SOCKS deadline");
            var config = AppSettings.Defaults(); config.SocksHost = "::1"; config.TestEndpoint = "https://example.org/";
            string arguments = ConnectionHealthMonitor.InternetArguments(config);
            Check(arguments.Contains("[::1]:") && arguments.Contains("--noproxy \"\"") && arguments.Contains("--max-time 8"), "explicit IPv6 SOCKS route cannot inherit a direct NO_PROXY bypass");
            foreach (var endpoint in new[] { "file:///tmp/example", "https://user:secret@example.org/", "missing-scheme" }) {
                config.TestEndpoint = endpoint; bool refused = false;
                try { ConnectionHealthMonitor.InternetArguments(config); } catch (ArgumentException) { refused = true; }
                Check(refused, "invalid or credential-bearing health endpoint is rejected");
            }
            config.TestEndpoint = "https://example.org/"; config.SocksHost = "bad\" --url";
            bool invalidHost = false; try { ConnectionHealthMonitor.InternetArguments(config); } catch (ArgumentException) { invalidHost = true; }
            Check(invalidHost, "health host cannot inject curl arguments");

            string bypass = Environment.GetEnvironmentVariable("NO_PROXY");
            try {
                Environment.SetEnvironmentVariable("NO_PROXY", "*");
                foreach (int status in new[] { 200, 401 }) {
                    var listener = Occupy(0); bool head = false, remoteName = false; string received = "", destination = "";
                    var server = Task.Run(delegate {
                        using (var client = listener.AcceptTcpClient()) {
                            var stream = client.GetStream(); stream.ReadTimeout = 4000;
                            HealthAcceptGreeting(stream); stream.Write(new byte[] { 5, 0 }, 0, 2);
                            var request = HealthRead(stream, 4);
                            if (request[3] == 3) { var name = HealthRead(stream, stream.ReadByte()); destination = Encoding.ASCII.GetString(name); remoteName = destination == "probe.example.invalid"; }
                            else HealthRead(stream, request[3] == 1 ? 4 : 16);
                            HealthRead(stream, 2); stream.Write(new byte[] { 5, 0, 0, 1, 127, 0, 0, 1, 0, 1 }, 0, 10);
                            var builder = new StringBuilder();
                            while (!builder.ToString().EndsWith("\r\n\r\n")) { int value = stream.ReadByte(); if (value < 0) throw new EndOfStreamException(); builder.Append((char)value); }
                            received = builder.ToString().Split('\r')[0]; head = received.StartsWith("HEAD /health HTTP/");
                            var answer = Encoding.ASCII.GetBytes("HTTP/1.1 " + status + " Fixture\r\nContent-Length: 0\r\nConnection: close\r\n\r\n"); stream.Write(answer, 0, answer.Length);
                        }
                    });
                    try {
                        config.SocksHost = "127.0.0.1"; config.SocksPort = Number(listener); config.TestEndpoint = "http://probe.example.invalid/health";
                        var result = ConnectionHealthMonitor.CheckInternet(config, CancellationToken.None);
                        Check(server.Wait(5000) && head && remoteName && result.Responded && result.HttpStatus == status,
                            "actual curl proves HTTP " + status + " through SOCKS despite NO_PROXY=* without external traffic; response=" + result.Responded + "/" + result.HttpStatus + "; destination=" + destination + "; request=" + received);
                    } finally { listener.Stop(); }
                }
            } finally { Environment.SetEnvironmentVariable("NO_PROXY", bypass); }

            var refusedListener = Occupy(0);
            var rejectingServer = Task.Run(delegate {
                using (var client = refusedListener.AcceptTcpClient()) {
                    var stream = client.GetStream(); stream.ReadTimeout = 4000;
                    HealthAcceptGreeting(stream); stream.Write(new byte[] { 5, 0 }, 0, 2);
                    var request = HealthRead(stream, 4);
                    HealthRead(stream, request[3] == 3 ? stream.ReadByte() : request[3] == 1 ? 4 : 16); HealthRead(stream, 2);
                    stream.Write(new byte[] { 5, 5, 0, 1, 0, 0, 0, 0, 0, 0 }, 0, 10);
                }
            });
            try {
                config.SocksHost = "127.0.0.1"; config.SocksPort = Number(refusedListener); config.TestEndpoint = "http://probe.example.invalid/health";
                var result = ConnectionHealthMonitor.CheckInternet(config, CancellationToken.None);
                Check(rejectingServer.Wait(5000) && !result.Responded && result.HttpStatus == 0, "actual curl cannot turn rejected remote CONNECT into internet readiness");
            } finally { refusedListener.Stop(); }

            var stalled = Occupy(0);
            using (var entered = new ManualResetEventSlim())
            using (var cancellation = new CancellationTokenSource()) {
                var server = Task.Run(delegate {
                    using (var client = stalled.AcceptTcpClient()) {
                        var stream = client.GetStream(); HealthRead(stream, 3); entered.Set();
                        try { while (stream.ReadByte() >= 0) { } } catch (IOException) { }
                    }
                });
                try {
                    config.SocksHost = "127.0.0.1"; config.SocksPort = Number(stalled);
                    var probe = Task.Run(() => ConnectionHealthMonitor.CheckInternet(config, cancellation.Token));
                    Check(entered.Wait(4000), "actual curl enters the stalled SOCKS fixture");
                    cancellation.Cancel();
                    Check(probe.Wait(3000) && !probe.Result.Responded && server.Wait(3000), "cancelling actual egress check kills curl and closes its socket");
                } finally { stalled.Stop(); }
            }
        }
        private static void HealthChecks(SettingsService settings)
        {
            HealthTransport();
            var config = AppSettings.Defaults(); var clock = new DateTime(2020, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            bool local = true, internet = false; int calls = 0, worker = 0; int ui = Thread.CurrentThread.ManagedThreadId;
            using (var monitor = new ConnectionHealthMonitor(() => config,
                delegate { worker = Thread.CurrentThread.ManagedThreadId; return local; },
                delegate { Interlocked.Increment(ref calls); return new InternetProbeResult(internet, internet ? 401 : 0); }, () => clock)) {
                Check(!monitor.Current.SocksReady && !monitor.Current.InternetVerified, "untested monitor invents no readiness");
                HealthRefresh(monitor);
                Check(monitor.Current.SocksReady && !monitor.Current.InternetVerified && monitor.Current.Internet == ConnectionProbeState.Failed,
                    "SOCKS success with unavailable egress is not fully working");
                internet = true; HealthRefresh(monitor);
                var verified = monitor.Current;
                Check(verified.InternetVerified && verified.HttpStatus == 401 && worker != ui, "HTTP transport proof is separate from app authorization and runs off UI");
                clock = clock.AddSeconds(6); int before = calls; monitor.RequestRefresh(); PumpUntil(() => !(bool)Field(monitor, "busy"));
                Check(calls == before && monitor.Current.InternetCheckedUtc == verified.InternetCheckedUtc && monitor.Current.SocksCheckedUtc == clock,
                    "cached egress preserves its completion time while local evidence refreshes");
                clock = clock.AddSeconds(25); internet = false; monitor.RequestRefresh(); PumpUntil(() => !(bool)Field(monitor, "busy"));
                Check(calls == before + 1 && !monitor.Current.InternetVerified && monitor.Current.InternetCheckedUtc == clock,
                    "thirty-second egress expiry starts a real replacement and removes old success");
                before = calls;
                clock = clock.AddSeconds(16);
                Check(!monitor.Current.SocksReady && !monitor.Current.InternetVerified, "expired local evidence cannot remain green");
                local = false; HealthRefresh(monitor);
                Check(!monitor.Current.SocksReady && calls == before && monitor.Current.Internet == ConnectionProbeState.Unknown,
                    "failed local protocol skips egress and clears previous internet proof");
                local = true; internet = true; HealthRefresh(monitor); config.TestEndpoint = "https://example.org/changed";
                Check(!monitor.Current.InternetVerified, "changing endpoint immediately invalidates cached proof");
            }
            int generationCalls = 0;
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var monitor = new ConnectionHealthMonitor(() => config, delegate { return true; }, delegate {
                if (Interlocked.Increment(ref generationCalls) == 1) { entered.Set(); release.Wait(4000); return new InternetProbeResult(true, 200); }
                return new InternetProbeResult(false);
            })) {
                monitor.RequestRefresh(true); PumpUntil(() => entered.IsSet);
                Check(monitor.Current.SocksReady && !monitor.Current.InternetVerified, "local readiness is published before the slow egress check finishes");
                int ticks = 0;
                using (var timer = new System.Windows.Forms.Timer { Interval = 20 }) {
                    timer.Tick += delegate { ticks++; }; timer.Start(); PumpUntil(() => ticks >= 3);
                    var watch = Stopwatch.StartNew(); for (int i = 0; i < 1000; i++) { var ignored = monitor.Current; }
                    Check(watch.ElapsedMilliseconds < 500 && !monitor.Current.InternetVerified, "cached reads and UI heartbeat stay responsive during egress");
                }
                config.SocksPort++; var unknown = monitor.Current;
                Check(!unknown.SocksReady && !unknown.InternetVerified, "configuration change immediately clears in-flight evidence");
                release.Set(); PumpUntil(() => monitor.Current.Internet == ConnectionProbeState.Failed && !(bool)Field(monitor, "busy"));
                Check(generationCalls == 2 && monitor.Current.Key == unknown.Key && !monitor.Current.InternetVerified, "stale successful probe is discarded and one replacement checks the new configuration");
            }
            using (var entered = new ManualResetEventSlim())
            using (var finished = new ManualResetEventSlim()) {
                int events = 0;
                var monitor = new ConnectionHealthMonitor(() => config, delegate { return true; }, delegate(AppSettings s, CancellationToken token) {
                    entered.Set(); token.WaitHandle.WaitOne(4000); finished.Set(); token.ThrowIfCancellationRequested(); return new InternetProbeResult(true, 200);
                });
                monitor.Changed += delegate { Interlocked.Increment(ref events); };
                monitor.RequestRefresh(true); PumpUntil(() => entered.IsSet); int before = events;
                monitor.Dispose(); PumpUntil(() => finished.IsSet && !(bool)Field(monitor, "busy"));
                Check(events == before && !monitor.Current.InternetVerified, "disposing monitor cancels in-flight probe and suppresses late publication");
            }
            HealthUi(settings);
        }
        private static void HealthUi(SettingsService settings)
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") return;
            var saved = settings.Current.Clone();
            var values = CliProxyEnvironmentService.Names.ToDictionary(n => n, n => Environment.GetEnvironmentVariable(n, EnvironmentVariableTarget.User));
            bool local = true, internet = false;
            try {
                var config = saved.Clone(); config.AutoCliProxy = false; config.AutoSystemProxy = false; config.AutoRestartSocks = false; settings.Save(config);
                foreach (var name in CliProxyEnvironmentService.Names) Environment.SetEnvironmentVariable(name, null, EnvironmentVariableTarget.User);
                using (var monitor = new ConnectionHealthMonitor(() => settings.Current, delegate { return local; }, delegate { return new InternetProbeResult(internet, internet ? 200 : 0); }))
                using (var proxy = new ProxyService(() => settings.Current, s => settings.Save(s), "unused-test-ssh", () => DateTime.UtcNow, false))
                using (var bridge = new CliProxyBridgeService(settings))
                using (var relay = new Ikev2RelayService())
                using (var home = new HomeVpnService(relay))
                using (var clipboard = new ClipboardService(settings))
                using (var context = new UpdateAwareTrayApplicationContext(settings, proxy, bridge, home, clipboard, false, monitor)) {
                    context.RequestShowStatus(); var main = (MainWindow)Field(context, "mainWindow"); var tray = (NotifyIcon)Field(context, "tray");
                    var title = (Label)Field(main, "connection"); var terminal = (Label)Field(main, "terminalState");
                    HealthRefresh(monitor); PumpUntil(() => tray.Text.Contains("выход не проверен"));
                    Check(title.Text == "Прокси отвечает" && title.ForeColor != UiTheme.Accent, "window and tray publish protocol-only state without a green internet claim");
                    Shot(main, "main-health-local-only");
                    foreach (var name in CliProxyEnvironmentService.Names) {
                        Environment.SetEnvironmentVariable(name, name == "NO_PROXY" ? "localhost,127.0.0.1,::1" : CliProxyBridgeService.UrlFor(config.HttpProxyPort), EnvironmentVariableTarget.User);
                    }
                    Check(CliProxyEnvironmentService.IsAppliedToUserEnvironment(config.HttpProxyPort), "complete four-variable CLI configuration is applied evidence");
                    foreach (var name in CliProxyEnvironmentService.Names) {
                        var expected = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User);
                        Environment.SetEnvironmentVariable(name, "foreign-value", EnvironmentVariableTarget.User);
                        Check(!CliProxyEnvironmentService.IsAppliedToUserEnvironment(config.HttpProxyPort) && CliProxyEnvironmentService.IsPartiallyApplied(config.HttpProxyPort), "each inconsistent CLI variable prevents full status: " + name);
                        Environment.SetEnvironmentVariable(name, expected, EnvironmentVariableTarget.User);
                    }
                    main.RefreshConnectionState();
                    Check(terminal.Text == "Настроено" && !monitor.Current.InternetVerified, "applied CLI settings do not imply verified internet");
                    Environment.SetEnvironmentVariable("HTTP_PROXY", "foreign-value", EnvironmentVariableTarget.User); main.RefreshConnectionState();
                    Check(terminal.Text == "Частично" && ((Button)Field(main, "cliToggle")).Text == "Запустить CLI", "partial CLI state retains the ordinary Start CLI repair action");
                    Shot(main, "main-cli-partial");
                    internet = true; HealthRefresh(monitor); PumpUntil(() => tray.Text.Contains("выход проверен"));
                    Check(title.Text == "Выход в интернет проверен" && title.ForeColor == UiTheme.Accent, "shared background internet evidence updates window and tray without opening settings");
                    Shot(main, "main-health-verified");
                    local = false; HealthRefresh(monitor); PumpUntil(() => tray.Text.Contains("прокси не отвечает"));
                    Check(title.Text == "Прокси не отвечает" && title.ForeColor == UiTheme.Error, "lost SOCKS readiness clears the prior green window and tray state");
                    Shot(main, "main-health-unavailable"); main.Close();
                }
            } finally {
                settings.Save(saved);
                foreach (var item in values) Environment.SetEnvironmentVariable(item.Key, item.Value, EnvironmentVariableTarget.User);
            }
        }
    }
}

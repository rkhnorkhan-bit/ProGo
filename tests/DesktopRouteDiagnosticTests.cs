using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        private static void RouteDiagnostics(SettingsService settings)
        {
            var current = AppSettings.Defaults(); current.TestEndpoint = "http://probe.example.invalid/check";
            foreach (int code in new[] { 200, 401, 403, 500 }) {
                var text = RouteTester.Test(current, null, CancellationToken.None, (args, limit, token) => new DiagnosticProcessResult { Output = code.ToString(), Error = "", ExitCode = 0 });
                Check(text.Contains("HTTP " + code) && !text.Contains("Не удалось"), "route accepts transport evidence HTTP " + code);
            }
            var failed = RouteTester.Test(current, null, CancellationToken.None, (args, limit, token) => new DiagnosticProcessResult { Output = "200", Error = "fixture failed", ExitCode = 7 });
            Check(failed.Contains("Не удалось"), "route cannot turn failed curl stdout into transport proof");
            var malformed = RouteTester.Test(current, null, CancellationToken.None, (args, limit, token) => new DiagnosticProcessResult { Output = "invalid 200 header", Error = "", ExitCode = 0 });
            Check(malformed.Contains("разобрать"), "route requires a complete numeric HTTP status");
            var truncated = RouteTester.Test(current, null, CancellationToken.None, (args, limit, token) => new DiagnosticProcessResult { Output = "200", Error = "", ExitCode = 0, Truncated = true });
            Check(truncated.Contains("размер"), "route refuses truncated output");
            string error; bool configured = false;
            var speed = ConnectionMetrics.MeasureDownloadMbps(current, CancellationToken.None, out error, (args, limit, token) => {
                configured = limit == 40000 && args.Contains("--max-time 35") && args.Contains("--noproxy \"\"") && args.Contains("speed.cloudflare.com") && args.Contains("%{speed_download}") && !args.Contains("--head");
                return new DiagnosticProcessResult { Output = "1250000", Error = "", ExitCode = 0 };
            });
            Check(configured && speed == 10.0 && error == null, "speed preserves download request and converts bytes to Mbps through explicit SOCKS");
            foreach (string output in new[] { "NaN", "Infinity", "-1", "0", "bad" }) {
                speed = ConnectionMetrics.MeasureDownloadMbps(current, CancellationToken.None, out error, (args, limit, token) => new DiagnosticProcessResult { Output = output, Error = "", ExitCode = 0 });
                Check(!speed.HasValue && error != null, "speed refuses invalid measurement " + output);
            }
            RouteCurlTransport(current);
            LatencyDeadlines(current);
            RouteProcessDeadlines(current);
            RouteCancellationUi(settings);
        }
        private static void RouteCurlTransport(AppSettings config)
        {
            var listener = Occupy(0); bool head = false, remoteDns = false;
            var server = Task.Run(() => {
                using (var client = listener.AcceptTcpClient()) {
                    var stream = client.GetStream(); stream.ReadTimeout = 5000;
                    HealthAcceptGreeting(stream); stream.Write(new byte[] { 5, 0 }, 0, 2);
                    var request = HealthRead(stream, 4);
                    if (request[3] == 3) remoteDns = Encoding.ASCII.GetString(HealthRead(stream, stream.ReadByte())) == "probe.example.invalid";
                    else HealthRead(stream, request[3] == 1 ? 4 : 16);
                    HealthRead(stream, 2); stream.Write(new byte[] { 5, 0, 0, 1, 127, 0, 0, 1, 0, 0 }, 0, 10);
                    var text = new StringBuilder();
                    while (!text.ToString().EndsWith("\r\n\r\n")) { int value = stream.ReadByte(); if (value < 0) throw new EndOfStreamException(); text.Append((char)value); }
                    head = text.ToString().StartsWith("HEAD /check HTTP/");
                    var answer = Encoding.ASCII.GetBytes("HTTP/1.1 401 Fixture\r\nContent-Length: 0\r\nConnection: close\r\n\r\n"); stream.Write(answer, 0, answer.Length);
                }
            });
            string previous = Environment.GetEnvironmentVariable("NO_PROXY");
            try {
                Environment.SetEnvironmentVariable("NO_PROXY", "*");
                var s = config.Clone(); s.SocksHost = "127.0.0.1"; s.SocksPort = Number(listener);
                string result = RouteTester.Test(s, null, CancellationToken.None);
                Check(server.Wait(5000) && head && remoteDns && result.Contains("HTTP 401"), "actual owned curl resolves destination through SOCKS despite NO_PROXY=* without external traffic");
            } finally { listener.Stop(); Environment.SetEnvironmentVariable("NO_PROXY", previous); }
        }
        private static void LatencyDeadlines(AppSettings config)
        {
            foreach (bool cancel in new[] { false, true }) {
                var listener = Occupy(0); var s = config.Clone(); s.SocksHost = "127.0.0.1"; s.SocksPort = Number(listener);
                using (var cts = new CancellationTokenSource())
                using (var entered = new ManualResetEventSlim()) {
                    var server = Task.Run(() => {
                        using (var client = listener.AcceptTcpClient()) {
                            var stream = client.GetStream(); stream.ReadTimeout = 2500;
                            HealthRead(stream, 3); entered.Set();
                            try {
                                // Each fragment arrives inside a per-read timeout; the total deadline must still win.
                                foreach (byte value in new byte[] { 5, 0 }) { stream.WriteByte(value); Thread.Sleep(150); }
                                HealthRead(stream, 4); // domain CONNECT follows
                                HealthRead(stream, stream.ReadByte() + 2);
                                foreach (byte value in new byte[] { 5, 0, 0, 1, 127, 0, 0, 1, 0, 0 }) { stream.WriteByte(value); Thread.Sleep(150); }
                                while (stream.ReadByte() >= 0) { }
                            } catch (IOException) { } catch (SocketException) { }
                        }
                    });
                    try {
                        var watch = Stopwatch.StartNew();
                        var measurement = Task.Run(() => ConnectionMetrics.MeasureSocksLatencyMs(s, cancel ? 2500 : 400, cts.Token));
                        PumpUntil(() => entered.IsSet); if (cancel) cts.Cancel();
                        PumpUntil(() => measurement.IsCompleted); bool cancelled = false; int? value = null;
                        try { value = measurement.GetAwaiter().GetResult(); } catch (OperationCanceledException) { cancelled = true; }
                        Check(cancel ? cancelled : !value.HasValue && watch.ElapsedMilliseconds < 1500, cancel ? "latency cancellation interrupts a partial SOCKS exchange" : "latency uses one deadline across fragmented greeting and CONNECT");
                        Check(server.Wait(2500), "latency releases its socket after " + (cancel ? "cancel" : "timeout"));
                    } finally { cts.Cancel(); listener.Stop(); }
                }
            }
        }
        private static void RouteProcessDeadlines(AppSettings config)
        {
            string marker = Path.Combine(work, "route-process"); Directory.CreateDirectory(marker);
            string previous = Environment.GetEnvironmentVariable("PROGO_DIAGNOSTIC_MARKER");
            Environment.SetEnvironmentVariable("PROGO_DIAGNOSTIC_MARKER", marker);
            try {
                foreach (bool download in new[] { false, true }) {
                    DiagnosticMarkers(marker);
                    Func<string, int, CancellationToken, DiagnosticProcessResult> runner = (args, limit, token) => DiagnosticProcess.Run(Application.ExecutablePath, "-G fixture-tree", 900, token);
                    var watch = Stopwatch.StartNew(); string error;
                    string result = download ? ConnectionMetrics.MeasureDownloadMbps(config, CancellationToken.None, out error, runner).ToString() + error : RouteTester.Test(config, null, CancellationToken.None, runner);
                    Check(result.Contains("лимит") || result.Contains("Тайм-аут"), "curl wrapper reports its bounded deadline " + download);
                    Check(watch.ElapsedMilliseconds < 3200 && DiagnosticGone(Path.Combine(marker, "root.pid")) && DiagnosticGone(Path.Combine(marker, "child.pid")), "curl wrapper settles root and descendants on deadline " + download);
                    DiagnosticMarkers(marker);
                    using (var cts = new CancellationTokenSource()) {
                        runner = (args, limit, token) => DiagnosticProcess.Run(Application.ExecutablePath, "-G fixture-tree", 3000, token);
                        var task = Task.Run(() => { if (download) { string e; ConnectionMetrics.MeasureDownloadMbps(config, cts.Token, out e, runner); } else RouteTester.Test(config, null, cts.Token, runner); });
                        PumpUntil(() => File.Exists(Path.Combine(marker, "child.pid"))); cts.Cancel(); PumpUntil(() => task.IsCompleted);
                        bool cancelled = false; try { task.GetAwaiter().GetResult(); } catch (OperationCanceledException) { cancelled = true; }
                        Check(cancelled && DiagnosticGone(Path.Combine(marker, "root.pid")) && DiagnosticGone(Path.Combine(marker, "child.pid")), "curl wrapper propagates cancel and settles its tree " + download);
                    }
                }
            } finally { Environment.SetEnvironmentVariable("PROGO_DIAGNOSTIC_MARKER", previous); }
        }
        private static void RouteCancellationUi(SettingsService settings)
        {
            int routeCalls = 0, speedCalls = 0, ticks = 0; string routeSnapshot = null;
            var saved = settings.Current.Clone(); var clock = new DateTime(2020, 1, 1, 12, 0, 0);
            try {
                using (var proxy = new ProxyService(settings))
                using (var form = new StatusForm(settings, proxy, false, (s, p, token) => {
                    routeSnapshot = s.TestEndpoint; int call = Interlocked.Increment(ref routeCalls);
                    if (call != 2) { token.WaitHandle.WaitOne(); token.ThrowIfCancellationRequested(); }
                    return "Fixture route answer";
                }, () => clock, null, (s, token) => { token.WaitHandle.WaitOne(); token.ThrowIfCancellationRequested(); return null; }, (s, token) => {
                    int call = Interlocked.Increment(ref speedCalls);
                    if (call != 2) { token.WaitHandle.WaitOne(); token.ThrowIfCancellationRequested(); }
                    return Tuple.Create((double?)10.0, (string)null);
                }))
                using (var timer = new System.Windows.Forms.Timer { Interval = 20 }) {
                    timer.Tick += delegate { ticks++; }; timer.Start(); form.Show(); Call(form, "StartSpeedTest"); Call(form, "QueueRouteMeasure");
                    PumpUntil(() => routeCalls == 1 && speedCalls == 1 && ticks >= 3);
                    settings.Current.TestEndpoint = "https://other.example.org/changed";
                    var check = (Button)Field(form, "checkButton"); var speed = (Button)Field(form, "speedButton");
                    Check(check.Enabled && speed.Enabled && check.Text.Contains("Отменить") && speed.Text.Contains("Отменить"), "busy route and speed keep accessible cancel controls and a UI heartbeat");
                    Call(form, "QueueRouteMeasure"); Call(form, "StartSpeedTest");
                    Check(routeCalls == 1 && speedCalls == 1 && routeSnapshot == saved.TestEndpoint, "duplicate guards and diagnostic settings snapshots survive edits");
                    form.Refresh(); Shot(form, "route-diagnostics-pending");
                    check.PerformClick(); speed.PerformClick(); PumpUntil(() => form.RouteWork.IsCompleted && form.SpeedWork.IsCompleted && form.PingWork.IsCompleted);
                    Check(((Label)Field(form, "route")).Text.Contains("отменена") && ((Label)Field(form, "speed")).Text.Contains("отменено") && ((Label)Field(form, "checkedAt")).Text == "Ещё не проверен", "cancelled diagnostics do not invent a completion timestamp");
                    Check(check.Text == "Проверить маршрут" && speed.Text == "Измерить скорость", "cancel restores explicit repeat commands");
                    form.Refresh(); Shot(form, "route-diagnostics-cancelled");
                    check.PerformClick(); speed.PerformClick(); PumpUntil(() => form.RouteWork.IsCompleted && form.SpeedWork.IsCompleted && routeCalls == 2 && speedCalls == 2);
                    Check(((Label)Field(form, "route")).Text == "Fixture route answer" && ((Label)Field(form, "speed")).Text.Contains(10.0.ToString("0.0")) && ((Label)Field(form, "checkedAt")).Text.Contains("2020-01-01"), "route and speed can complete a fresh repeat after cancellation");
                    form.Refresh(); Shot(form, "route-diagnostics-repeated");
                    // Close while ping and a new speed measurement are still waiting on their tokens.
                    speed.PerformClick(); check.PerformClick(); PumpUntil(() => speedCalls == 3 && routeCalls == 3);
                    var speedWork = form.SpeedWork; var pingWork = form.PingWork; var routeWork = form.RouteWork;
                    var watch = Stopwatch.StartNew(); form.Close();
                    Check(watch.ElapsedMilliseconds < 500, "closing diagnostics never waits on measurements in the UI thread");
                    PumpUntil(() => speedWork.IsCompleted && pingWork.IsCompleted && routeWork.IsCompleted);
                    Check(form.IsDisposed && (int)Field(form, "routeInFlight") == 0 && (int)Field(form, "speedInFlight") == 0 && (int)Field(form, "pingInFlight") == 0, "closing diagnostics cancels workers and suppresses late UI updates");
                }
            } finally { settings.Current.TestEndpoint = saved.TestEndpoint; }
        }
    }
}

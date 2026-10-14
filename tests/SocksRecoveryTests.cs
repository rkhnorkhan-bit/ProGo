using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using ProGo;

internal static class SocksRecoveryTests
{
    private static readonly string Executable = Process.GetCurrentProcess().MainModule.FileName;
    private static int passed;
    private static int assertions;

    private static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "-N") return FakeSsh(args);
        try
        {
            Test("existing settings enable recovery; explicit opt-out survives serialization", SettingsMigration);
            Test("idle application does not start SSH", IdleDoesNotStart);
            Test("crashed child is restarted once after delay", CrashRecovery);
            Test("crash loop backoff is capped and stable recovery resets it", Backoff);
            Test("manual stop and disposal cancel scheduled recovery", StopAndDispose);
            Test("opt-out cancels retries without stopping the live tunnel", Toggle);
            Test("startup grace and repeated missing-listener checks", MissingListener);
            Test("an open TCP port without SOCKS is not considered healthy", InvalidSocksListener);
            Test("external port owner is neither killed nor adopted", ExternalListener);
            Test("manual and automatic hidden attempts are noninteractive", BatchMode);
            Test("asynchronous slow startup shares one process and waits for SOCKS", AsyncSlowStartup);
            Test("stop cancels startup and immediate retry starts one fresh attempt", AsyncCancellation);
            Test("asynchronous stop returns while an owned process gate is busy and serializes retry", AsyncStopSerialization);
            Test("startup timeout stops its owned child and gives guidance", AsyncTimeout);
            Test("SSH refusal explains key authorization without a hidden prompt", AsyncRefusal);
            Test("permanent SSH refusal pauses recovery until explicit retry", PermanentRefusal);
            Test("asynchronous startup refuses a foreign non-SOCKS listener", AsyncForeignListener);
            Test("async launch failure schedules the first recovery delay once", AsyncLaunchFailure);
            Test("recovery respects configured profile fallback", Fallback);
            Test("manual profile fallback remains available", ManualFallback);
            Test("structured SSH fields reach native child and automatic recovery", StructuredArguments);
            Test("editing SSH fields cancels stale readiness under the same profile ID", StructuredEditCancellation);
            Test("SOCKS endpoint forms preserve IPv4 and localhost without constructor resolution", EndpointFormsAndCompatibility);
            Test("raw and bracketed IPv6 start a native child and complete SOCKS CONNECT", IPv6ReadinessAndMetrics);
            Test("IPv6 crash recovery retains bracketed forwarding and stop releases the port", IPv6RecoveryAndStop);
            Test("IPv6 pending startup cancellation and timeout clean up before retry", IPv6CancellationAndTimeout);
            Test("IPv6 foreign listeners remain external and silent probes are bounded and cancellable", IPv6ForeignProbes);
            Test("native OpenSSH parses isolated bracketed IPv6 dynamic forwarding locally", NativeIPv6DynamicForward);
            Test("asynchronous DNS stays inside the connection budget and cannot dial after cancellation", DelayedDnsCancellationAndDeadline);
            Test("permanent refusals leave permitted manual profile fallback available", PermanentManualFallback);
            Test("automatic fallback advances through permanent and transient failures without retrying refused candidates", PermanentRecoveryFallback);
            Test("fallback publishes a profile only after its SOCKS greeting", FallbackHandshakePublication);
            Test("stop and external cancellation cancel gated fallback without publication or resurrection", FallbackCancellation);
            Test("stop cancels automatic fallback adoption while its SOCKS greeting is in flight", TimerFallbackStop);
            Test("disabled single and exhausted permanent fallback retain explicit retry pause", FallbackPermanentPause);
            Test("a foreign valid SOCKS listener is neither adopted nor returned as owned startup", ForeignReadySocks);
            Test("selected and backup profile edits invalidate pending fallback", FallbackEditCancellation);
            Console.WriteLine("SOCKS recovery tests PASS: " + passed);
            Console.WriteLine("SOCKS recovery assertions PASS: " + assertions);
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }

    private static void Test(string name, Action action)
    {
        int before = assertions;
        action(); passed++; Console.WriteLine("PASS: " + name + " (" + (assertions - before) + " assertions)");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        assertions++;
    }

    private static void WaitFor(Func<bool> condition)
    {
        var watch = Stopwatch.StartNew();
        while (!condition())
        {
            if (watch.ElapsedMilliseconds > 5000) throw new Exception("Timed out waiting for test child state");
            Thread.Sleep(20);
        }
    }

    private sealed class Fixture : IDisposable
    {
        public AppSettings Settings = AppSettings.Defaults();
        public DateTime Now = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public readonly ProxyService Proxy;
        public int ProfileSaves;
        public Fixture(int startupTimeoutMs = 20000, string host = "127.0.0.1")
        {
            Settings.SocksHost = host;
            bool ipv6 = host == "::1" || host == "[::1]" || (host == "localhost" && Environment.GetEnvironmentVariable("PROGO_TEST_SSH_LOCALHOST_FAMILY") == "v6");
            var reserve = new TcpListener(ipv6 ? IPAddress.IPv6Loopback : IPAddress.Loopback, 0); reserve.Start();
            Settings.SocksPort = ((IPEndPoint)reserve.LocalEndpoint).Port; reserve.Stop();
            Settings.SshProfile = "ready";
            Proxy = new ProxyService(delegate { return Settings; }, delegate(AppSettings s) { Settings = s; ProfileSaves++; },
                Executable, delegate { return Now; }, false, "", startupTimeoutMs);
        }
        public void Tick(int seconds) { Now = Now.AddSeconds(seconds); Proxy.PollRecovery(); }
        public void Ready()
        {
            WaitFor(delegate { return Proxy.CurrentPid.HasValue && Proxy.IsListening(); });
            Tick(0);
        }
        public void Start() { Proxy.StartTunnel(false); Ready(); }
        public void Crash()
        {
            Assert(Proxy.CurrentPid.HasValue, "Expected an owned child before crash");
            using (var child = Process.GetProcessById(Proxy.CurrentPid.Value))
            {
                child.Kill(); Assert(child.WaitForExit(3000), "Test child did not exit");
            }
            Tick(0);
        }
        public void Dispose() { Proxy.Dispose(); }
    }

    private static void SettingsMigration()
    {
        var json = new JavaScriptSerializer();
        Assert(json.Deserialize<AppSettings>("{\"AutoStartSocks\":false}").AutoRestartSocks, "Old settings did not opt in");
        var disabled = json.Deserialize<AppSettings>("{\"AutoRestartSocks\":false}");
        Assert(!json.Deserialize<AppSettings>(json.Serialize(disabled)).AutoRestartSocks, "Explicit false lost");
        Assert(!disabled.AutoStartSocks, "Recovery must not enable automatic startup");
    }

    private static void IdleDoesNotStart()
    {
        using (var f = new Fixture())
        {
            f.Tick(3600); Assert(!f.Proxy.CurrentPid.HasValue && !f.Proxy.NextRecoveryUtc.HasValue && !f.Proxy.ConnectionRequested, "Idle app started SSH or created connection intent");
            using (var cancelled = new CancellationTokenSource()) { cancelled.Cancel(); Assert(f.Proxy.StartTunnelAsync(cancelled.Token).IsCanceled && !f.Proxy.ConnectionRequested, "Pre-cancelled start created user connection intent"); }
        }
    }

    private static void CrashRecovery()
    {
        using (var f = new Fixture())
        {
            f.Start(); f.Crash();
            Assert(f.Proxy.NextRecoveryUtc == f.Now.AddSeconds(5), "First recovery delay must be five seconds");
            f.Tick(4); Assert(!f.Proxy.CurrentPid.HasValue, "Restarted before the delay elapsed");
            f.Tick(1); f.Ready();
            var pid = f.Proxy.CurrentPid;
            f.Proxy.StartTunnel(false); f.Proxy.StartTunnel(false);
            var threads = new List<Thread>();
            for (var i = 0; i < 8; i++)
            {
                var thread = new Thread(f.Proxy.PollRecovery); threads.Add(thread); thread.Start();
            }
            foreach (var thread in threads) Assert(thread.Join(5000), "Overlapping watchdog checks stalled");
            Assert(f.Proxy.AutomaticRestarts == 1 && f.Proxy.CurrentPid == pid, "Duplicate SSH process or retry");
        }
    }

    private static void Backoff()
    {
        using (var f = new Fixture())
        {
            f.Start();
            foreach (var delay in new[] { 5, 10, 20, 40, 60, 60 })
            {
                f.Crash(); Assert(f.Proxy.NextRecoveryUtc == f.Now.AddSeconds(delay), "Incorrect crash-loop delay");
                f.Tick(delay); f.Ready();
            }
            f.Tick(60); f.Crash();
            Assert(f.Proxy.NextRecoveryUtc == f.Now.AddSeconds(5), "Stable tunnel did not reset backoff");
        }
    }

    private static void StopAndDispose()
    {
        using (var f = new Fixture())
        {
            f.Start(); f.Crash(); f.Proxy.StopTunnel(); f.Tick(600);
            Assert(!f.Proxy.CurrentPid.HasValue && !f.Proxy.NextRecoveryUtc.HasValue, "Manual stop resurrected SSH");
            f.Start(); f.Crash(); f.Proxy.Dispose(); f.Tick(600); f.Proxy.StartTunnel(false);
            Assert(!f.Proxy.CurrentPid.HasValue && !f.Proxy.NextRecoveryUtc.HasValue && !f.Proxy.ConnectionRequested, "Dispose resurrected SSH or user intent");
        }
    }

    private static void Toggle()
    {
        using (var f = new Fixture())
        {
            f.Start(); var pid = f.Proxy.CurrentPid;
            f.Proxy.SetAutoRestart(false); f.Tick(600);
            Assert(f.Proxy.CurrentPid == pid && !f.Settings.AutoRestartSocks, "Opt-out stopped a live tunnel");
            f.Crash(); f.Tick(600); Assert(!f.Proxy.CurrentPid.HasValue, "Disabled watchdog restarted SSH");
            f.Proxy.SetAutoRestart(true); f.Tick(0); f.Tick(5); f.Ready();
            f.Crash(); f.Proxy.SetAutoRestart(false); f.Tick(600);
            Assert(!f.Proxy.CurrentPid.HasValue, "Pending retry survived opt-out");
        }
    }

    private static void MissingListener()
    {
        using (var f = new Fixture())
        {
            f.Settings.SshProfile = "quiet"; f.Proxy.StartTunnel(false);
            Assert(f.Proxy.CurrentPid.HasValue, "Quiet test child missing");
            f.Tick(19); f.Tick(1); f.Tick(5);
            Assert(f.Proxy.CurrentPid.HasValue, "Startup grace/consecutive failure threshold ignored");
            f.Tick(5); Assert(!f.Proxy.CurrentPid.HasValue, "Unresponsive listener was not stopped");
            f.Settings.SshProfile = "ready"; f.Tick(5); f.Ready();
        }
    }

    private static void ExternalListener()
    {
        using (var f = new Fixture())
        {
            var listener = new TcpListener(IPAddress.Loopback, f.Settings.SocksPort); listener.Start();
            try
            {
                f.Proxy.StartTunnel(false); f.Tick(600);
                Assert(!f.Proxy.CurrentPid.HasValue, "External listener was adopted");
            }
            finally { listener.Stop(); }
            f.Tick(600); Assert(!f.Proxy.CurrentPid.HasValue, "Watchdog armed itself for an external tunnel");
            f.Start(); f.Crash(); listener.Start();
            try
            {
                f.Tick(5); Assert(!f.Proxy.CurrentPid.HasValue && f.Proxy.IsListening(), "Occupied recovery port was replaced");
            }
            finally { listener.Stop(); }
            f.Tick(5); f.Ready();
        }
    }

    private static void InvalidSocksListener()
    {
        using (var f = new Fixture())
        {
            f.Settings.SshProfile = "wrong"; f.Proxy.StartTunnel(false);
            WaitFor(f.Proxy.IsListening);
            f.Tick(20); f.Tick(5);
            Assert(f.Proxy.CurrentPid.HasValue, "Invalid listener stopped before failure threshold");
            f.Tick(5);
            Assert(!f.Proxy.CurrentPid.HasValue && f.Proxy.NextRecoveryUtc.HasValue, "TCP-only listener counted as SOCKS");
        }
    }

    private static void BatchMode()
    {
        using (var f = new Fixture())
        {
            f.Settings.SshProfile = "batch-only"; f.Proxy.StartTunnel(false);
            f.Ready(); f.Crash(); f.Tick(5); f.Ready();
            Assert(f.Proxy.AutomaticRestarts == 1, "Automatic retry did not use BatchMode");
        }
    }

    private static void AsyncSlowStartup()
    {
        // Gate readiness explicitly: a busy runner can pause this test thread
        // longer than a fixed simulated SSH delay after the call already returned.
        var release = Path.Combine(Path.GetTempPath(), "progo-socks-ready-" + Guid.NewGuid().ToString("N"));
        var previous = Environment.GetEnvironmentVariable("PROGO_TEST_SSH_RELEASE");
        Environment.SetEnvironmentVariable("PROGO_TEST_SSH_RELEASE", release);
        try {
            using (var f = new Fixture()) {
                f.Settings.SshProfile = "slow";
                var watch = Stopwatch.StartNew(); var task = f.Proxy.StartTunnelAsync(CancellationToken.None);
                Assert(watch.ElapsedMilliseconds < 2000, "Start call blocked: " + watch.ElapsedMilliseconds + "ms");
                Assert(!task.IsCompleted && f.Proxy.IsConnecting, "Readiness was declared before the fixture released SOCKS");
                Assert(ReferenceEquals(task, f.Proxy.StartTunnelAsync(CancellationToken.None)), "Concurrent starts did not share their pending operation");
                File.WriteAllText(release, "ready");
                Assert(task.Wait(5000) && task.Result && !f.Proxy.IsConnecting, "Slow SOCKS was not awaited");
                var pid = f.Proxy.CurrentPid;
                Assert(f.Proxy.StartTunnelAsync(CancellationToken.None).Result && f.Proxy.CurrentPid == pid, "Repeated ready request created another child");
            }
        } finally {
            Environment.SetEnvironmentVariable("PROGO_TEST_SSH_RELEASE", previous);
            if (File.Exists(release)) File.Delete(release);
        }
    }
    private static void AsyncCancellation()
    {
        using (var f = new Fixture()) {
            f.Settings.SshProfile = "quiet";
            var task = f.Proxy.StartTunnelAsync(CancellationToken.None); WaitFor(() => f.Proxy.CurrentPid.HasValue);
            f.Proxy.StopTunnel(); f.Settings.SshProfile = "ready";
            var retry = f.Proxy.StartTunnelAsync(CancellationToken.None);
            Assert(retry.Wait(5000) && retry.Result && f.Proxy.CurrentPid.HasValue, "Immediate retry after cancellation failed or resurrected the former intent");
            Assert(task.IsCanceled, "Stopped asynchronous request did not cancel");
        }
    }
    private static void AsyncStopSerialization()
    {
        foreach (bool cancelQueued in new[] { false, true }) using (var f = new Fixture()) {
            f.Start(); int previousPid = f.Proxy.CurrentPid.Value;
            Assert(f.Proxy.ConnectionRequested, "A running explicit connection lost its user intent");
            var gate = typeof(ProxyService).GetField("gate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(f.Proxy);
            using (var entered = new ManualResetEventSlim()) using (var release = new ManualResetEventSlim()) {
                var held = System.Threading.Tasks.Task.Run(delegate { lock (gate) { entered.Set(); release.Wait(5000); } });
                Assert(entered.Wait(3000), "Fixture gate was not held");
                System.Threading.Tasks.Task<bool> stop = null, retry = null;
                try {
                    var watch = Stopwatch.StartNew(); stop = f.Proxy.StopTunnelAsync();
                    Assert(watch.ElapsedMilliseconds < 200 && !stop.IsCompleted && f.Proxy.IsStopping, "Async stop waited for a busy ownership gate");
                    Assert(!f.Proxy.ConnectionRequested, "Stop did not clear user intent before background cleanup");
                    Assert(!(bool)typeof(ProxyService).GetField("wanted", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(f.Proxy), "Stop did not cancel recovery intent immediately");
                    retry = f.Proxy.StartTunnelAsync(CancellationToken.None);
                    Assert(!retry.IsCompleted && f.Proxy.ConnectionRequested, "Queued startup did not expose intent independently of cleanup");
                    using (var child = Process.GetProcessById(previousPid)) Assert(!child.HasExited, "Blocked cleanup lost its owned process");
                    if (cancelQueued) { Assert(Object.ReferenceEquals(stop, f.Proxy.StopTunnelAsync()), "Repeated stop did not share ongoing cleanup"); Assert(!f.Proxy.ConnectionRequested, "A later stop failed to cancel queued connection intent"); }
                } finally { release.Set(); Assert(held.Wait(3000), "Fixture gate failed to release"); }
                Assert(stop.Wait(5000) && stop.Result, "Owned cleanup did not complete");
                if (cancelQueued) {
                    WaitFor(() => retry.IsCompleted);
                    Assert(retry.IsCanceled && !f.Proxy.CurrentPid.HasValue && !f.Proxy.ConnectionRequested, "A late stop resurrected its queued startup or user intent");
                    Assert(f.Proxy.StartTunnelAsync(CancellationToken.None).Result, "Fresh explicit startup after cancelled queue failed");
                } else Assert(retry.Wait(5000) && retry.Result && f.Proxy.CurrentPid.HasValue && f.Proxy.CurrentPid != previousPid, "Old cleanup stopped or reused the fresh SSH process");
                try { using (var child = Process.GetProcessById(previousPid)) Assert(child.HasExited, "Old owned SSH process was retained after cleanup"); } catch (ArgumentException) { }
                f.Tick(600); Assert(f.Proxy.CurrentPid.HasValue, "A delayed stop killed the new startup");
            }
        }
    }
    private static void AsyncTimeout()
    {
        using (var f = new Fixture(500)) {
            f.Settings.SshProfile = "quiet"; f.Settings.AutoRestartSocks = false;
            var task = f.Proxy.StartTunnelAsync(CancellationToken.None);
            Assert(task.Wait(5000) && !task.Result && !f.Proxy.CurrentPid.HasValue && !f.Proxy.IsConnecting && f.Proxy.StartupError.Contains("отведённое время"), "Timeout was unbounded, kept its child, or gave no next step");
        }
    }
    private static void AsyncRefusal()
    {
        using (var f = new Fixture()) {
            f.Settings.SshProfile = "denied"; f.Settings.AutoRestartSocks = false;
            var task = f.Proxy.StartTunnelAsync(CancellationToken.None);
            Assert(task.Wait(5000) && !task.Result && f.Proxy.StartupError.Contains("ключ не принят") && f.Proxy.StartupError.Contains("Первый вход"), "SSH key refusal did not give actionable visible-login guidance");
        }
    }
    private static void PermanentRefusal()
    {
        foreach (var mode in new[] { "denied", "bad-host" }) using (var f = new Fixture()) {
            f.Settings.SshProfile = mode; f.Settings.AutoRestartSocks = true;
            var task = f.Proxy.StartTunnelAsync(CancellationToken.None);
            Assert(task.Wait(5000) && !task.Result, "Permanent refusal must fail noninteractive startup");
            f.Tick(0);
            Assert(!f.Proxy.CurrentPid.HasValue && !f.Proxy.NextRecoveryUtc.HasValue && f.Proxy.RecoveryStatus.Contains("приостановлено"), "Permanent refusal must leave a visible pause and no scheduled restart");
            int restarts = f.Proxy.AutomaticRestarts;
            for (int i = 0; i < 20; i++) f.Tick(600);
            Assert(f.Proxy.AutomaticRestarts == restarts && !f.Proxy.CurrentPid.HasValue, "Permanent refusal must not spawn repeated SSH children");
            f.Proxy.SetAutoRestart(false); f.Proxy.SetAutoRestart(true); f.Tick(600);
            Assert(f.Proxy.AutomaticRestarts == restarts, "Recovery preference toggle cannot silently bypass an authentication pause");
            f.Settings.SshProfile = "ready";
            var retry = f.Proxy.StartTunnelAsync(CancellationToken.None);
            Assert(retry.Wait(5000) && retry.Result && !f.Proxy.RecoveryStatus.Contains("приостановлено"), "Explicit connection after fixing profile must clear the pause and authenticate through SOCKS readiness");
        }
    }
    private static void AsyncLaunchFailure()
    {
        var config = AppSettings.Defaults(); config.SshProfile = "ready";
        var now = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var reserve = new TcpListener(IPAddress.Loopback, 0); reserve.Start();
        config.SocksPort = ((IPEndPoint)reserve.LocalEndpoint).Port; reserve.Stop();
        using (var proxy = new ProxyService(() => config, delegate { }, "missing-fixture-ssh.exe", () => now, false)) {
            Assert(!proxy.StartTunnelAsync(CancellationToken.None).GetAwaiter().GetResult(), "Missing executable must fail");
            Assert(proxy.NextRecoveryUtc == now.AddSeconds(5), "Async wrapper must not increase initial backoff twice");
        }
    }

    private static void AsyncForeignListener()
    {
        using (var f = new Fixture()) {
            var listener = new TcpListener(IPAddress.Loopback, f.Settings.SocksPort); listener.Start();
            try {
                var task = f.Proxy.StartTunnelAsync(CancellationToken.None);
                Assert(task.Wait(5000) && !task.Result && !f.Proxy.CurrentPid.HasValue && f.Proxy.StartupError.Contains("Порт SOCKS занят") && f.Proxy.IsListening(), "Foreign TCP listener was adopted, removed or accepted as a ready route");
            } finally { listener.Stop(); }
        }
    }

    private static void Fallback()
    {
        using (var f = new Fixture())
        {
            f.Start(); f.Settings.SshProfile = "die"; f.Settings.AutoSwitchSshProfile = true;
            f.Settings.SshProfiles.Add(new SshProfileSetting { Name = "Backup", Target = "batch-only" });
            f.Crash(); f.Tick(5); WaitFor(delegate { return !f.Proxy.CurrentPid.HasValue; });
            f.Tick(0); f.Tick(10); f.Ready();
            Assert(f.Settings.SshProfile == "batch-only", "Working fallback was not selected");
        }
    }

    private static void ManualFallback()
    {
        using (var f = new Fixture())
        {
            f.Settings.SshProfile = "die"; f.Settings.AutoSwitchSshProfile = true;
            f.Settings.SshProfiles.Add(new SshProfileSetting { Name = "Backup", Target = "ready" });
            f.Start(); Assert(f.Settings.SshProfile == "ready", "Manual profile fallback regressed");
        }
    }

    private sealed class FallbackCapture : IDisposable
    {
        internal readonly string Folder = Path.Combine(Path.GetTempPath(), "progo-fallback-" + Guid.NewGuid().ToString("N"));
        internal readonly string Seen, Release;
        private readonly string[] names = { "PROGO_TEST_SSH_LAUNCH_DIR", "PROGO_TEST_SSH_GREETING_SEEN", "PROGO_TEST_SSH_GREETING_RELEASE" };
        private readonly string[] previous = new string[3];
        internal FallbackCapture()
        {
            Directory.CreateDirectory(Folder); Seen = Path.Combine(Folder, "seen"); Release = Path.Combine(Folder, "release");
            var values = new[] { Folder, Seen, Release };
            for (int i = 0; i < names.Length; i++) { previous[i] = Environment.GetEnvironmentVariable(names[i]); Environment.SetEnvironmentVariable(names[i], values[i]); }
        }
        internal int Count(string mode = null)
        {
            int count = 0;
            foreach (string path in Directory.GetFiles(Folder, "*.json")) {
                var entry = new JavaScriptSerializer().Deserialize<string[]>(File.ReadAllText(path));
                if (mode == null || entry[0] == mode) count++;
            }
            return count;
        }
        internal bool AllBatch()
        {
            foreach (string path in Directory.GetFiles(Folder, "*.json"))
                if (new JavaScriptSerializer().Deserialize<string[]>(File.ReadAllText(path))[2] != "batch") return false;
            return true;
        }
        internal void Unblock() { File.WriteAllText(Release, "reply"); }
        public void Dispose()
        {
            for (int i = 0; i < names.Length; i++) Environment.SetEnvironmentVariable(names[i], previous[i]);
            Directory.Delete(Folder, true);
        }
    }

    // New fallback scenarios may visit a 5 s unresponsive candidate before their backup.
    // Existing fixtures retain their original 5 s guards and production deadlines.
    private static void WaitForFallback(Func<bool> condition)
    {
        var watch = Stopwatch.StartNew();
        while (!condition()) { if (watch.ElapsedMilliseconds > 10000) throw new Exception("Timed out waiting for fallback fixture"); Thread.Sleep(20); }
    }
    private static void AddFallback(Fixture f, string selected, params string[] backups)
    {
        f.Settings.SshProfile = selected; f.Settings.AutoSwitchSshProfile = true;
        foreach (string target in backups) f.Settings.SshProfiles.Add(new SshProfileSetting { Name = target, Target = target });
    }

    private static void PermanentManualFallback()
    {
        foreach (string mode in new[] { "denied", "bad-host" }) using (var capture = new FallbackCapture()) using (var f = new Fixture()) {
            AddFallback(f, mode, "ready");
            var task = f.Proxy.StartTunnelAsync(CancellationToken.None);
            Assert(task.Wait(10000) && task.Result, "Permanent refusal prevented permitted manual fallback: " + mode);
            Assert(f.Settings.SshProfile == "ready" && f.ProfileSaves == 1, "Successful manual fallback was not published exactly once");
            Assert(ConnectionHealthMonitor.CheckSocks(f.Settings, CancellationToken.None), "Published manual backup did not answer SOCKS");
            Assert(capture.Count(mode) == 1 && capture.Count("ready") == 1 && capture.Count() == 2 && capture.AllBatch(), "Manual fallback repeated a candidate or lost noninteractive SSH");
            Assert(!f.Proxy.RecoveryStatus.Contains("приостановлено") && !f.Proxy.NextRecoveryUtc.HasValue, "Refused primary paused a working manual backup");
        }
    }

    private static void PermanentRecoveryFallback()
    {
        foreach (string mode in new[] { "denied", "bad-host" }) using (var capture = new FallbackCapture()) using (var f = new Fixture()) {
            f.Settings.SshProfile = "batch-only"; f.Start();
            AddFallback(f, mode, "die", "ready"); f.Crash();
            Assert(f.Proxy.NextRecoveryUtc == f.Now.AddSeconds(5), "Fallback changed the initial recovery delay");
            int before = f.Proxy.AutomaticRestarts; bool paused = false;
            WaitForFallback(delegate {
                f.Tick(f.Proxy.NextRecoveryUtc.HasValue ? 60 : 0); paused |= f.Proxy.RecoveryStatus.Contains("приостановлено");
                return f.Proxy.CurrentPid.HasValue && ConnectionHealthMonitor.CheckSocks(f.Settings, CancellationToken.None);
            }); f.Ready();
            Assert(!paused && f.Settings.SshProfile == "ready" && f.ProfileSaves == 1, "A permanent refusal paused or mispublished automatic backup");
            Assert(capture.Count(mode) == 1 && capture.Count("die") == 1 && capture.Count("ready") == 1, "Recovery skipped a permitted candidate or retried permanent refusal before the others");
            Assert(f.Proxy.AutomaticRestarts == before + 3 && capture.AllBatch(), "Automatic attempts duplicated ownership or lost noninteractive mode");
            f.Proxy.StopTunnel(); f.Tick(600);
            Assert(!f.Proxy.HasOwnedProcess && !f.Proxy.NextRecoveryUtc.HasValue && capture.Count() == 4, "Stopped automatic fallback resurrected a candidate");
        }
    }

    private static void FallbackHandshakePublication()
    {
        using (var capture = new FallbackCapture()) using (var f = new Fixture()) {
            AddFallback(f, "wrong", "greeting-gated");
            var task = f.Proxy.StartTunnelAsync(CancellationToken.None); WaitForFallback(() => File.Exists(capture.Seen));
            Assert(f.Proxy.IsListening() && !task.IsCompleted, "Backup fixture did not expose a pending TCP-only route");
            Assert(f.Settings.SshProfile == "wrong" && f.ProfileSaves == 0, "Fallback published a TCP listener before its SOCKS response");
            capture.Unblock();
            Assert(task.Wait(10000) && task.Result && f.Settings.SshProfile == "greeting-gated" && f.ProfileSaves == 1, "Handshake did not publish the proven backup exactly once");
            Assert(ConnectionHealthMonitor.CheckSocks(f.Settings, CancellationToken.None) && capture.Count("wrong") == 1 && capture.Count("greeting-gated") == 1, "TCP-only primary was adopted or backup lacks SOCKS evidence");
        }
    }

    private static void FallbackCancellation()
    {
        foreach (bool external in new[] { false, true }) using (var capture = new FallbackCapture()) using (var f = new Fixture()) using (var source = new CancellationTokenSource()) {
            AddFallback(f, "denied", "greeting-gated");
            var task = f.Proxy.StartTunnelAsync(source.Token); WaitForFallback(() => File.Exists(capture.Seen));
            Assert(!task.IsCompleted && f.ProfileSaves == 0, "Fallback cancellation fixture was already published");
            if (external) source.Cancel();
            else {
                var watch = Stopwatch.StartNew(); var stop = f.Proxy.StopTunnelAsync();
                Assert(watch.ElapsedMilliseconds < 200 && !f.Proxy.ConnectionRequested, "Stopping fallback blocked the caller or retained connection intent");
                Assert(stop.Wait(10000) && stop.Result, "Stopping gated fallback failed owned cleanup");
            }
            WaitForFallback(() => task.IsCompleted);
            Assert(task.IsCanceled && !f.Proxy.HasOwnedProcess && !f.Proxy.NextRecoveryUtc.HasValue, "Cancelled fallback retained ownership or scheduled retry");
            capture.Unblock(); f.Tick(600);
            Assert(f.Settings.SshProfile == "denied" && f.ProfileSaves == 0 && capture.Count() == 2 && !f.Proxy.IsListening(), "Late SOCKS reply resurrected or published cancelled fallback");
            var reserve = new TcpListener(IPAddress.Loopback, f.Settings.SocksPort); reserve.Start(); reserve.Stop();
            f.Settings.SshProfile = "ready"; f.Settings.AutoSwitchSshProfile = false;
            var retry = f.Proxy.StartTunnelAsync(CancellationToken.None);
            Assert(retry.Wait(10000) && retry.Result, "Fresh explicit retry after fallback cancellation failed");
        }
    }

    private static void TimerFallbackStop()
    {
        using (var capture = new FallbackCapture()) using (var f = new Fixture()) {
            f.Settings.SshProfile = "batch-only"; f.Start();
            AddFallback(f, "die", "greeting-gated"); f.Crash(); f.Tick(5);
            WaitForFallback(() => !f.Proxy.CurrentPid.HasValue); f.Tick(0); f.Tick(10);
            WaitForFallback(() => f.Proxy.CurrentPid.HasValue && f.Proxy.IsListening());
            Assert(capture.Count("greeting-gated") == 1 && !File.Exists(capture.Seen), "Automatic backup was missing or already probed before the timer gate");
            var watch = Stopwatch.StartNew(); var poll = Task.Run(delegate { f.Proxy.PollRecovery(); });
            WaitForFallback(() => File.Exists(capture.Seen));
            var stopWatch = Stopwatch.StartNew(); var stop = f.Proxy.StopTunnelAsync();
            capture.Unblock();
            Assert(stopWatch.ElapsedMilliseconds < 200 && !f.Proxy.ConnectionRequested, "Stop waited for the automatic SOCKS probe or retained intent");
            Assert(watch.ElapsedMilliseconds < 600, "Timer gate was not released inside the unchanged 700 ms SOCKS probe budget");
            Assert(poll.Wait(10000) && stop.Wait(10000) && stop.Result, "Automatic probe and owned stop did not settle");
            Assert(f.Settings.SshProfile == "die" && f.ProfileSaves == 0, "A late automatic SOCKS response published fallback after Stop");
            f.Tick(600);
            Assert(!f.Proxy.HasOwnedProcess && !f.Proxy.NextRecoveryUtc.HasValue && !f.Proxy.IsListening() && capture.Count() == 3, "Stopped timer fallback retained ownership or resurrected a candidate");
        }
    }

    private static void FallbackPermanentPause()
    {
        foreach (string variant in new[] { "disabled", "single", "exhausted" }) using (var capture = new FallbackCapture()) using (var f = new Fixture()) {
            AddFallback(f, "denied", variant == "disabled" ? "ready" : "bad-host");
            if (variant == "disabled") f.Settings.AutoSwitchSshProfile = false;
            if (variant == "single") f.Settings.SshProfiles.Clear();
            var task = f.Proxy.StartTunnelAsync(CancellationToken.None);
            Assert(task.Wait(10000) && !task.Result, "Permanent refusal unexpectedly authenticated: " + variant); f.Tick(0);
            Assert(!f.Proxy.HasOwnedProcess && !f.Proxy.NextRecoveryUtc.HasValue && f.Proxy.RecoveryStatus.Contains("приостановлено"), "Permanent candidate exhaustion lost visible pause: " + variant);
            int expected = variant == "exhausted" ? 2 : 1;
            Assert(capture.Count() == expected && capture.Count("denied") == 1, "Disabled/single/exhausted fallback launched an unexpected candidate");
            for (int i = 0; i < 3; i++) f.Tick(600);
            f.Proxy.SetAutoRestart(false); f.Proxy.SetAutoRestart(true); f.Tick(600);
            Assert(capture.Count() == expected && !f.Proxy.HasOwnedProcess, "Timer or preference toggle bypassed permanent refusal pause");
            f.Settings.SshProfile = "ready"; var retry = f.Proxy.StartTunnelAsync(CancellationToken.None);
            Assert(retry.Wait(10000) && retry.Result && !f.Proxy.RecoveryStatus.Contains("приостановлено"), "Explicit corrected retry did not clear exhausted fallback pause");
        }
    }

    private static void ForeignReadySocks()
    {
        using (var capture = new FallbackCapture()) using (var f = new Fixture()) {
            using (var foreign = Process.Start(new ProcessStartInfo(Executable, "-N -D 127.0.0.1:" + f.Settings.SocksPort.ToString(CultureInfo.InvariantCulture) + " ready") { UseShellExecute = false, CreateNoWindow = true })) {
                try {
                    WaitForFallback(() => ConnectionHealthMonitor.CheckSocks(f.Settings, CancellationToken.None));
                    var task = f.Proxy.StartTunnelAsync(CancellationToken.None);
                    Assert(task.Wait(10000) && !task.Result && !f.Proxy.HasOwnedProcess && f.Proxy.StartupError.Contains("другим процессом"), "Foreign valid SOCKS was adopted as owned startup or mislabeled as unresponsive");
                    Assert(!foreign.HasExited && ConnectionHealthMonitor.CheckSocks(f.Settings, CancellationToken.None) && capture.Count() == 1, "Starting ProGo killed or duplicated the foreign SOCKS owner");
                    f.Proxy.StopTunnel(); f.Tick(600);
                    Assert(!foreign.HasExited && ConnectionHealthMonitor.CheckSocks(f.Settings, CancellationToken.None) && !f.Proxy.NextRecoveryUtc.HasValue, "Stopping ProGo altered a foreign valid SOCKS route");
                } finally { if (!foreign.HasExited) foreign.Kill(); if (!foreign.WaitForExit(3000)) throw new Exception("Foreign fixture did not exit"); }
            }
        }
    }

    private static void FallbackEditCancellation()
    {
        foreach (bool backupEdit in new[] { false, true }) using (var capture = new FallbackCapture()) using (var f = new Fixture()) {
            f.Settings.SshProfile = "gated-wrong"; f.Settings.AutoSwitchSshProfile = true;
            f.Settings.SshProfiles.Add(new SshProfileSetting { Target = "backup-id", Server = "ready", User = "ubuntu", Port = 22 });
            var task = f.Proxy.StartTunnelAsync(CancellationToken.None); WaitForFallback(() => File.Exists(capture.Seen));
            var changed = f.Settings.Clone();
            if (backupEdit) changed.SshProfiles[0].Server = "batch-only"; else changed.SshProfile = "owner-choice";
            f.Settings = changed; capture.Unblock(); WaitForFallback(() => task.IsCompleted);
            Assert(task.IsCanceled && !f.Proxy.HasOwnedProcess && !f.Proxy.NextRecoveryUtc.HasValue, "Changed fallback plan did not cancel captured startup");
            Assert(f.ProfileSaves == 0 && capture.Count() == 1 && capture.Count("ready") == 0 && capture.Count("batch-only") == 0, "Stale startup launched or published the old/edited backup");
            Assert(f.Settings.SshProfile == (backupEdit ? "gated-wrong" : "owner-choice") && f.Settings.SshProfiles[0].Server == (backupEdit ? "batch-only" : "ready"), "Cancelled fallback overwrote newer owner settings");
            f.Tick(600); Assert(capture.Count() == 1, "Settings invalidation left a stale automatic retry");
        }
    }

    private static void StructuredArguments()
    {
        var capture = Path.Combine(Path.GetTempPath(), "progo-argv-" + Guid.NewGuid().ToString("N"));
        var before = Environment.GetEnvironmentVariable("PROGO_TEST_SSH_ARGV");
        try {
            Environment.SetEnvironmentVariable("PROGO_TEST_SSH_ARGV", capture);
            using (var f = new Fixture()) {
                var p = new SshProfileSetting { Target = "progo-id", Server = "ready", User = "ubuntu", Port = 2222,
                    IdentityFile = Path.Combine(Path.GetTempPath(), "O'Brien $key", "key file") };
                f.Settings.SshProfile = p.Target; f.Settings.SshProfiles.Add(p); f.Start();
                var args = new JavaScriptSerializer().Deserialize<string[]>(File.ReadAllText(capture));
                Assert(args[Array.IndexOf(args, "-p") + 1] == "2222" && args[Array.IndexOf(args, "-l") + 1] == "ubuntu" && args[Array.IndexOf(args, "-i") + 1] == p.IdentityFile && args[args.Length - 1] == "ready", "Native SSH arguments corrupted explicit fields/key path");
                File.Delete(capture); f.Crash(); f.Tick(5); f.Ready();
                args = new JavaScriptSerializer().Deserialize<string[]>(File.ReadAllText(capture));
                Assert(args[Array.IndexOf(args, "-p") + 1] == "2222" && args[Array.IndexOf(args, "-i") + 1] == p.IdentityFile && Array.IndexOf(args, "BatchMode=yes") >= 0, "Recovery lost structured fields or hidden BatchMode");
            }
        } finally { Environment.SetEnvironmentVariable("PROGO_TEST_SSH_ARGV", before); if (File.Exists(capture)) File.Delete(capture); }
    }

    private static void StructuredEditCancellation()
    {
        var release = Path.Combine(Path.GetTempPath(), "progo-edit-ready-" + Guid.NewGuid().ToString("N"));
        var previous = Environment.GetEnvironmentVariable("PROGO_TEST_SSH_RELEASE");
        Environment.SetEnvironmentVariable("PROGO_TEST_SSH_RELEASE", release);
        try {
            using (var f = new Fixture()) {
                f.Settings.SshProfile = "progo-slow";
                f.Settings.SshProfiles.Add(new SshProfileSetting { Target = "progo-slow", Server = "slow", User = "ubuntu", Port = 22 });
                var task = f.Proxy.StartTunnelAsync(CancellationToken.None);
                WaitFor(delegate { return f.Proxy.CurrentPid.HasValue; });
                var changed = f.Settings.Clone(); changed.SshProfiles[0].Port = 2222; f.Settings = changed;
                try { task.Wait(5000); } catch (AggregateException e) { Assert(e.InnerException is OperationCanceledException, "Unexpected startup error after edit"); }
                Assert(task.IsCanceled && !f.Proxy.CurrentPid.HasValue && !f.Proxy.NextRecoveryUtc.HasValue, "Old profile's pending readiness survived a port edit");
            }
        } finally { Environment.SetEnvironmentVariable("PROGO_TEST_SSH_RELEASE", previous); if (File.Exists(release)) File.Delete(release); }
    }

    private sealed class SshCapture : IDisposable
    {
        internal readonly string Arguments = Path.Combine(Path.GetTempPath(), "progo-ipv6-argv-" + Guid.NewGuid().ToString("N"));
        internal readonly string Connect = Path.Combine(Path.GetTempPath(), "progo-ipv6-connect-" + Guid.NewGuid().ToString("N"));
        private readonly string previousArguments = Environment.GetEnvironmentVariable("PROGO_TEST_SSH_ARGV");
        private readonly string previousConnect = Environment.GetEnvironmentVariable("PROGO_TEST_SSH_CONNECT");
        internal SshCapture()
        {
            Environment.SetEnvironmentVariable("PROGO_TEST_SSH_ARGV", Arguments);
            Environment.SetEnvironmentVariable("PROGO_TEST_SSH_CONNECT", Connect);
        }
        internal void ForwardIsIPv6(int port)
        {
            var args = new JavaScriptSerializer().Deserialize<string[]>(File.ReadAllText(Arguments));
            int index = Array.IndexOf(args, "-D");
            Assert(index >= 0 && index + 1 < args.Length, "Native child did not receive a dynamic-forward argument");
            Assert(args[index + 1] == "[::1]:" + port.ToString(CultureInfo.InvariantCulture), "Native IPv6 dynamic forwarding was unbracketed or bracketed twice");
            Assert(Array.IndexOf(args, "BatchMode=yes") >= 0, "IPv6 startup or recovery lost hidden BatchMode");
        }
        public void Dispose()
        {
            Environment.SetEnvironmentVariable("PROGO_TEST_SSH_ARGV", previousArguments);
            Environment.SetEnvironmentVariable("PROGO_TEST_SSH_CONNECT", previousConnect);
            if (File.Exists(Arguments)) File.Delete(Arguments);
            if (File.Exists(Connect)) File.Delete(Connect);
        }
    }

    private static void EndpointFormsAndCompatibility()
    {
        Assert(SocksEndpoint.Host("::1") == "::1" && SocksEndpoint.Host("[::1]") == "::1", "Valid IPv6 endpoint forms do not share the same socket host");
        Assert(SocksEndpoint.Host("[2001:DB8::2]") == "2001:DB8::2" && SocksEndpoint.Host("[fe80::1%12]") == "fe80::1%12", "Valid IPv6 spelling or scope was changed");
        foreach (var host in new[] { "[localhost]", "[127.0.0.1]", "[[::1]]", "[::1", "::1]", " [::1] " })
            Assert(SocksEndpoint.Host(host) == host, "Malformed or non-IPv6 brackets were silently stripped: " + host);
        var previous = Thread.CurrentThread.CurrentCulture;
        try {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("ar-SA");
            Assert(SocksEndpoint.Format("::1", 12345) == "[::1]:12345" && SocksEndpoint.Format("[::1]", 12345) == "[::1]:12345", "IPv6 endpoint formatting depends on spelling or current culture");
            Assert(SocksEndpoint.Format("127.0.0.1", 12345) == "127.0.0.1:12345" && SocksEndpoint.Format("localhost", 12345) == "localhost:12345", "IPv4 or domain forwarding changed");
        } finally { Thread.CurrentThread.CurrentCulture = previous; }
        foreach (var host in new[] { "127.0.0.1", "::1", "[::1]", "::ffff:127.0.0.1", "[::ffff:127.0.0.1]" }) using (var client = SocksEndpoint.CreateClient(host))
            Assert(client.Client.AddressFamily == (host == "127.0.0.1" ? AddressFamily.InterNetwork : AddressFamily.InterNetworkV6) && !client.Connected,
                "Literal endpoint constructed the wrong client family or connected in its constructor: " + host);
        using (var client = SocksEndpoint.CreateClient("localhost")) {
            Assert(!client.Connected && !client.Client.IsBound, "Domain client constructor opened a connection");
            Assert(client.Client.AddressFamily == (Socket.OSSupportsIPv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork) &&
                (!Socket.OSSupportsIPv6 || client.Client.DualMode), "Domain client does not select the available dual-stack family");
        }
        foreach (var host in new[] { "127.0.0.1", "localhost" }) using (var f = new Fixture(20000, host)) {
            // The child maps localhost to IPv4 without resolving any external name.
            // This exercises domain BeginConnect against an IPv4-only listener.
            f.Settings.TestEndpoint = "https://progo-metrics.invalid:8443/";
            var start = f.Proxy.StartTunnelAsync(CancellationToken.None);
            bool completed = start.Wait(5000), ready = completed && start.Result, listening = ready && f.Proxy.IsListening();
            if (host == "localhost" && (!completed || !ready || !listening)) LocalhostFailureDiagnostics(f, start, completed, ready, listening);
            Assert(completed && ready && listening, "IPv4 or localhost asynchronous readiness regressed: " + host);
            Assert(ConnectionHealthMonitor.CheckSocks(f.Settings, CancellationToken.None), "IPv4 or localhost SOCKS greeting failed: " + host);
            Assert(ConnectionMetrics.MeasureSocksLatencyMs(f.Settings, 2000).HasValue, "IPv4 or localhost SOCKS CONNECT failed: " + host);
        }
        using (var f = new Fixture()) {
            f.Start();
            foreach (var host in new[] { "::ffff:127.0.0.1", "[::ffff:127.0.0.1]" }) {
                // Keep the child's real -D endpoint IPv4-only. These probes test
                // mapped IPv6 clients, not an unsupported mapped SSH bind claim.
                var mapped = f.Settings.Clone(); mapped.SocksHost = host; mapped.TestEndpoint = "https://progo-metrics.invalid:8443/";
                Assert(ConnectionHealthMonitor.CheckSocks(mapped, CancellationToken.None), "Mapped IPv6 greeting did not reach the IPv4-only listener: " + host);
                Assert(ConnectionMetrics.MeasureSocksLatencyMs(mapped, 2000).HasValue, "Mapped IPv6 metrics did not complete through the IPv4-only listener: " + host);
            }
        }
        LocalhostIPv6Only();
    }

    private static void LocalhostIPv6Only()
    {
        string previous = Environment.GetEnvironmentVariable("PROGO_TEST_SSH_LOCALHOST_FAMILY");
        try {
            Environment.SetEnvironmentVariable("PROGO_TEST_SSH_LOCALHOST_FAMILY", "v6");
            using (var capture = new SshCapture()) using (var f = new Fixture(20000, "localhost")) {
                // The child's independent parser selects ::1 and disables dual
                // mode. Production resolves localhost through the real DNS API.
                f.Settings.TestEndpoint = "https://progo-metrics.invalid:8443/";
                var start = f.Proxy.StartTunnelAsync(CancellationToken.None);
                Assert(start.Wait(5000) && start.Result && f.Proxy.IsListening(), "Localhost startup could not reach an IPv6-only child");
                Assert(ConnectionHealthMonitor.CheckSocks(f.Settings, CancellationToken.None), "Localhost SOCKS greeting could not reach its IPv6-only listener");
                Assert(ConnectionMetrics.MeasureSocksLatencyMs(f.Settings, 2000).HasValue, "Localhost SOCKS CONNECT could not reach its IPv6-only listener");
                var request = new JavaScriptSerializer().Deserialize<string[]>(File.ReadAllText(capture.Connect));
                Assert(request.Length == 3 && request[0] == "progo-metrics.invalid" && request[1] == "8443" && request[2] == "InterNetworkV6", "Localhost metrics did not complete CONNECT in the actual IPv6 child");
                using (var client = SocksEndpoint.Connect("localhost", f.Settings.SocksPort, 2000, CancellationToken.None)) {
                    Assert(client.Connected && ((IPEndPoint)client.Client.RemoteEndPoint).AddressFamily == AddressFamily.InterNetworkV6, "Localhost connector did not return the real connected IPv6 winner");
                    var stream = client.GetStream(); stream.ReadTimeout = stream.WriteTimeout = 2000;
                    stream.Write(new byte[] { 5, 1, 0 }, 0, 3);
                    Assert(stream.ReadByte() == 5 && stream.ReadByte() == 0, "Returned localhost winner could not complete the SOCKS greeting");
                }
                var stop = f.Proxy.StopTunnelAsync();
                Assert(stop.Wait(5000) && stop.Result && !f.Proxy.HasOwnedProcess && !f.Proxy.IsListening(), "Localhost IPv6 stop did not settle and release its child");
                var reserve = new TcpListener(IPAddress.IPv6Loopback, f.Settings.SocksPort);
                try { reserve.Start(); Assert(((IPEndPoint)reserve.LocalEndpoint).Port == f.Settings.SocksPort, "Stopped localhost IPv6 port was not reusable"); } finally { reserve.Stop(); }
            }
        } finally { Environment.SetEnvironmentVariable("PROGO_TEST_SSH_LOCALHOST_FAMILY", previous); }
    }

    private static void DelayedDnsCancellationAndDeadline()
    {
        var ipv4 = new TcpListener(IPAddress.Loopback, 0); ipv4.Start();
        int port = ((IPEndPoint)ipv4.LocalEndpoint).Port;
        TcpListener ipv6 = null;
        try {
            ipv6 = new TcpListener(IPAddress.IPv6Loopback, port); ipv6.Server.DualMode = false; ipv6.Start();
            Assert(!ipv4.Pending() && !ipv6.Pending(), "Delayed-DNS listeners were not initially idle");
            foreach (bool cancel in new[] { false, true }) using (var entered = new ManualResetEventSlim()) using (var cancellation = new CancellationTokenSource()) {
                var resolved = new TaskCompletionSource<IPAddress[]>(); int calls = 0;
                Func<string, Task<IPAddress[]>> resolver = delegate(string host) { Interlocked.Increment(ref calls); entered.Set(); return resolved.Task; };
                var watch = Stopwatch.StartNew();
                var connect = Task.Run(() => SocksEndpoint.Connect("localhost", port, 700, cancellation.Token, resolver));
                try {
                    Assert(entered.Wait(2000), "Delayed DNS resolver was never entered");
                    if (cancel) { watch.Restart(); cancellation.Cancel(); }
                    bool completed;
                    using (var handle = ((IAsyncResult)connect).AsyncWaitHandle) completed = handle.WaitOne(2000);
                    Assert(completed && watch.ElapsedMilliseconds < 2000 && !resolved.Task.IsCompleted, "DNS extended the connection budget or had to complete before cancellation/deadline");
                    bool cancelled = false, timedOut = false;
                    try { using (var unexpected = connect.GetAwaiter().GetResult()) { } }
                    catch (OperationCanceledException) { cancelled = true; }
                    catch (TimeoutException) { timedOut = true; }
                    Assert(cancel ? cancelled : timedOut, "Stalled DNS produced the wrong cancellation/deadline result");
                    Assert(calls == 1, "A single stalled connection resolved the same host more than once");
                    resolved.SetResult(new[] { IPAddress.IPv6Loopback, IPAddress.Loopback });
                    Assert(!ipv4.Pending() && !ipv6.Pending(), "A late DNS result immediately dialed after connection completion");
                    // Bounded native regression observation, in addition to the
                    // synchronous connector's completed ownership boundary.
                    bool lateConnection = false; watch.Restart();
                    while (watch.ElapsedMilliseconds < 250) { lateConnection |= ipv4.Pending() || ipv6.Pending(); Thread.Sleep(10); }
                    Assert(!lateConnection, "A late DNS continuation connected to an actual listener after cancellation/deadline");
                } finally {
                    cancellation.Cancel(); resolved.TrySetResult(new[] { IPAddress.IPv6Loopback, IPAddress.Loopback });
                    WaitFor(() => connect.IsCompleted);
                    if (connect.Status == TaskStatus.RanToCompletion) connect.Result.Dispose();
                    else { var observed = connect.Exception; }
                }
            }
            using (var cancellation = new CancellationTokenSource()) {
                cancellation.Cancel(); int calls = 0; bool cancelled = false;
                Func<string, Task<IPAddress[]>> resolver = delegate(string host) { Interlocked.Increment(ref calls); return Task.FromResult(new[] { IPAddress.Loopback }); };
                try { using (var unexpected = SocksEndpoint.Connect("localhost", port, 700, cancellation.Token, resolver)) { } }
                catch (OperationCanceledException) { cancelled = true; }
                Assert(cancelled && calls == 0, "A pre-cancelled connection invoked DNS or failed to preserve cancellation");
                Assert(!ipv4.Pending() && !ipv6.Pending(), "A pre-cancelled connection dialed an actual listener");
            }
        } finally { if (ipv6 != null) ipv6.Stop(); ipv4.Stop(); }
    }

    // Failure-only native diagnostics. Keep the original acceptance and timeout;
    // observe localhost resolution/socket behavior without changing production policy.
    private static void LocalhostFailureDiagnostics(Fixture fixture, Task<bool> start, bool completed, bool ready, bool listening)
    {
        var error = (fixture.Proxy.StartupError ?? "none").Replace("\r", " ").Replace("\n", " ");
        if (error.Length > 180) error = error.Substring(0, 180);
        Console.WriteLine("LOCALHOST_DIAGNOSTIC task=" + start.Status + "; wait5000=" + completed + "; result=" + ready +
            "; listening=" + listening + "; owned=" + fixture.Proxy.HasOwnedProcess + "; pid=" + (fixture.Proxy.CurrentPid ?? 0) +
            "; port=" + fixture.Settings.SocksPort + "; startup=" + error);
        var watch = Stopwatch.StartNew();
        var dns = Task.Run(() => Dns.GetHostAddresses("localhost"));
        dns.ContinueWith(task => { var ignored = task.Exception; }, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
        try {
            if (!dns.Wait(2000)) Console.WriteLine("LOCALHOST_DNS status=timeout; elapsed=" + watch.ElapsedMilliseconds);
            else {
                var addresses = dns.GetAwaiter().GetResult();
                Console.WriteLine("LOCALHOST_DNS status=complete; count=" + addresses.Length + "; elapsed=" + watch.ElapsedMilliseconds);
                for (int i = 0; i < Math.Min(8, addresses.Length); i++) {
                    var address = addresses[i]; bool loopback = IPAddress.IsLoopback(address) ||
                        (address.IsIPv4MappedToIPv6 && IPAddress.IsLoopback(address.MapToIPv4()));
                    Console.WriteLine("LOCALHOST_DNS index=" + i + "; family=" + address.AddressFamily + "; loopback=" + loopback +
                        (loopback ? "; address=" + address : ""));
                }
            }
        } catch (Exception ex) { Console.WriteLine("LOCALHOST_DNS status=error; type=" + ex.GetType().Name + "; elapsed=" + watch.ElapsedMilliseconds); }
        foreach (var host in new[] { "localhost", "::ffff:127.0.0.1", "127.0.0.1" }) {
            watch.Restart();
            try {
                using (var client = new TcpClient(AddressFamily.InterNetworkV6)) {
                    client.Client.DualMode = true;
                    var connect = client.BeginConnect(host, fixture.Settings.SocksPort, null, null);
                    using (connect.AsyncWaitHandle) {
                        if (!connect.AsyncWaitHandle.WaitOne(1000)) {
                            Console.WriteLine("LOCALHOST_APM host=" + host + "; status=timeout; elapsed=" + watch.ElapsedMilliseconds); continue;
                        }
                        client.EndConnect(connect);
                        var peer = (IPEndPoint)client.Client.RemoteEndPoint;
                        bool loopback = IPAddress.IsLoopback(peer.Address) ||
                            (peer.Address.IsIPv4MappedToIPv6 && IPAddress.IsLoopback(peer.Address.MapToIPv4()));
                        Console.WriteLine("LOCALHOST_APM host=" + host + "; status=connected; family=" + client.Client.AddressFamily +
                            "; peerFamily=" + peer.AddressFamily + "; peerLoopback=" + loopback + "; elapsed=" + watch.ElapsedMilliseconds);
                    }
                }
            } catch (SocketException ex) {
                Console.WriteLine("LOCALHOST_APM host=" + host + "; status=socket-error; error=" + ex.SocketErrorCode + "; native=" + ex.ErrorCode + "; elapsed=" + watch.ElapsedMilliseconds);
            } catch (Exception ex) { Console.WriteLine("LOCALHOST_APM host=" + host + "; status=error; type=" + ex.GetType().Name + "; elapsed=" + watch.ElapsedMilliseconds); }
        }
    }

    private static void IPv6ReadinessAndMetrics()
    {
        using (var capture = new SshCapture()) foreach (var host in new[] { "::1", "[::1]" }) using (var f = new Fixture(20000, host)) {
            f.Settings.TestEndpoint = "https://progo-metrics.invalid:8443/";
            var start = f.Proxy.StartTunnelAsync(CancellationToken.None);
            Assert(start.Wait(5000) && start.Result && f.Proxy.CurrentPid.HasValue && !f.Proxy.IsConnecting, "IPv6 asynchronous startup did not await its real listener: " + host);
            capture.ForwardIsIPv6(f.Settings.SocksPort);
            Assert(f.Proxy.IsListening() && ConnectionHealthMonitor.CheckSocks(f.Settings, CancellationToken.None), "IPv6 TCP or SOCKS greeting probe failed: " + host);
            // An independent literal-address client proves the fixture really bound
            // IPv6; a fixture that accidentally listens only on IPv4 cannot pass.
            using (var client = new TcpClient(AddressFamily.InterNetworkV6)) {
                var connect = client.BeginConnect(IPAddress.IPv6Loopback, f.Settings.SocksPort, null, null);
                using (connect.AsyncWaitHandle) { Assert(connect.AsyncWaitHandle.WaitOne(2000), "Independent IPv6 connect exceeded its deadline"); client.EndConnect(connect); }
                Assert(((IPEndPoint)client.Client.RemoteEndPoint).AddressFamily == AddressFamily.InterNetworkV6, "Native child did not accept an IPv6 socket");
                var stream = client.GetStream(); stream.ReadTimeout = stream.WriteTimeout = 2000;
                stream.Write(new byte[] { 5, 1, 0 }, 0, 3);
                Assert(stream.ReadByte() == 5 && stream.ReadByte() == 0, "Native IPv6 child did not reply to a real SOCKS greeting");
            }
            int? latency = ConnectionMetrics.MeasureSocksLatencyMs(f.Settings, 2000);
            Assert(latency.HasValue && latency.Value >= 1, "IPv6 latency probe did not complete the SOCKS CONNECT reply: " + host);
            var request = new JavaScriptSerializer().Deserialize<string[]>(File.ReadAllText(capture.Connect));
            Assert(request.Length == 3 && request[0] == "progo-metrics.invalid" && request[1] == "8443" && request[2] == "InterNetworkV6",
                "Metrics did not send its exact target through the native IPv6 SOCKS child");
            int? pid = f.Proxy.CurrentPid;
            var repeat = f.Proxy.StartTunnelAsync(CancellationToken.None);
            Assert(repeat.Wait(5000) && repeat.Result && f.Proxy.CurrentPid == pid, "Repeated IPv6 readiness created a duplicate child");
        }
    }

    private static void IPv6RecoveryAndStop()
    {
        using (var capture = new SshCapture()) foreach (var host in new[] { "::1", "[::1]" }) using (var f = new Fixture(20000, host)) {
            f.Start(); int pid = f.Proxy.CurrentPid.Value;
            File.Delete(capture.Arguments); f.Crash();
            Assert(f.Proxy.NextRecoveryUtc == f.Now.AddSeconds(5), "IPv6 recovery lost the initial retry delay");
            f.Tick(4); Assert(!f.Proxy.CurrentPid.HasValue, "IPv6 recovery restarted before its delay");
            f.Tick(1); f.Ready(); capture.ForwardIsIPv6(f.Settings.SocksPort);
            Assert(f.Proxy.CurrentPid != pid && f.Proxy.AutomaticRestarts == 1 && ConnectionHealthMonitor.CheckSocks(f.Settings, CancellationToken.None), "IPv6 recovery did not create exactly one healthy replacement");
            var stop = f.Proxy.StopTunnelAsync();
            Assert(stop.Wait(5000) && stop.Result && !f.Proxy.HasOwnedProcess && !f.Proxy.ConnectionRequested && !f.Proxy.IsListening(), "IPv6 stop did not settle ownership and release its listener");
            var reserve = new TcpListener(IPAddress.IPv6Loopback, f.Settings.SocksPort);
            try { reserve.Start(); Assert(((IPEndPoint)reserve.LocalEndpoint).Port == f.Settings.SocksPort, "Stopped IPv6 port was not reusable"); } finally { reserve.Stop(); }
            f.Tick(600); Assert(!f.Proxy.CurrentPid.HasValue && !f.Proxy.NextRecoveryUtc.HasValue, "Stopped IPv6 recovery resurrected a child");
        }
    }

    private static void IPv6CancellationAndTimeout()
    {
        foreach (var host in new[] { "::1", "[::1]" }) {
            using (var f = new Fixture(20000, host)) using (var cancellation = new CancellationTokenSource()) {
                f.Settings.SshProfile = "quiet";
                var start = f.Proxy.StartTunnelAsync(cancellation.Token);
                WaitFor(() => f.Proxy.CurrentPid.HasValue);
                cancellation.Cancel(); WaitFor(() => start.IsCompleted);
                Assert(start.IsCanceled && !f.Proxy.HasOwnedProcess && !f.Proxy.IsConnecting && !f.Proxy.NextRecoveryUtc.HasValue, "IPv6 cancellation kept its child, readiness or retry");
                f.Settings.SshProfile = "ready";
                var retry = f.Proxy.StartTunnelAsync(CancellationToken.None);
                Assert(retry.Wait(5000) && retry.Result && f.Proxy.CurrentPid.HasValue && ConnectionHealthMonitor.CheckSocks(f.Settings, CancellationToken.None), "Fresh IPv6 startup after cancellation failed");
            }
            using (var f = new Fixture(500, host)) {
                f.Settings.SshProfile = "quiet"; f.Settings.AutoRestartSocks = false;
                var start = f.Proxy.StartTunnelAsync(CancellationToken.None);
                Assert(start.Wait(5000) && !start.Result && !f.Proxy.HasOwnedProcess && !f.Proxy.IsConnecting && f.Proxy.StartupError.Contains("отведённое время"), "IPv6 startup timeout kept its owned process or lacked guidance");
            }
        }
    }

    private static void IPv6ForeignProbes()
    {
        foreach (var host in new[] { "::1", "[::1]" }) using (var f = new Fixture(20000, host)) {
            var listener = new TcpListener(IPAddress.IPv6Loopback, f.Settings.SocksPort); listener.Start();
            try {
                Assert(f.Proxy.IsListening(), "IPv6 foreign port was invisible to the TCP probe");
                Assert(!ConnectionHealthMonitor.CheckSocks(f.Settings, CancellationToken.None), "Silent IPv6 TCP listener was accepted as SOCKS");
                var start = f.Proxy.StartTunnelAsync(CancellationToken.None);
                Assert(start.Wait(5000) && !start.Result && !f.Proxy.HasOwnedProcess && f.Proxy.StartupError.Contains("Порт SOCKS занят"), "Foreign IPv6 listener was adopted or replaced");
                f.Proxy.StopTunnel(); f.Tick(600);
                Assert(f.Proxy.IsListening() && !f.Proxy.CurrentPid.HasValue && !f.Proxy.NextRecoveryUtc.HasValue, "Stopping IPv6 modified an external listener or scheduled recovery");
                f.Settings.TestEndpoint = "https://progo-metrics.invalid:8443/";
                var watch = Stopwatch.StartNew();
                Assert(!ConnectionMetrics.MeasureSocksLatencyMs(f.Settings, 400).HasValue && watch.ElapsedMilliseconds < 2000, "Silent IPv6 metrics exceeded or ignored their deadline");
                // Drain prior TCP-only probes, then observe this metrics request's
                // actual greeting before cancellation: no timing-only cancellation.
                while (listener.Pending()) using (var client = listener.AcceptTcpClient()) { }
                using (var cancellation = new CancellationTokenSource()) {
                    var metrics = Task.Run(() => ConnectionMetrics.MeasureSocksLatencyMs(f.Settings, 5000, cancellation.Token));
                    try {
                        WaitFor(listener.Pending);
                        using (var peer = listener.AcceptTcpClient()) {
                            var stream = peer.GetStream(); stream.ReadTimeout = 2000;
                            Assert(stream.ReadByte() == 5 && stream.ReadByte() == 1 && stream.ReadByte() == 0, "Cancellable IPv6 metrics did not reach the real SOCKS greeting");
                            cancellation.Cancel(); WaitFor(() => metrics.IsCompleted);
                            bool cancelled = false;
                            try { metrics.GetAwaiter().GetResult(); } catch (OperationCanceledException) { cancelled = true; }
                            Assert(cancelled, "In-flight IPv6 metrics swallowed cancellation or returned success");
                        }
                    } finally { cancellation.Cancel(); WaitFor(() => metrics.IsCompleted); }
                }
                Assert(f.Proxy.IsListening() && !f.Proxy.HasOwnedProcess, "Cancelled IPv6 metrics removed or adopted the foreign listener");
            } finally { listener.Stop(); }
        }
    }

    private static void NativeIPv6DynamicForward()
    {
        const int timeoutMs = 7000, port = 12345;
        // Numeric target, disabled canonicalization and NUL config make -G a
        // parser-only check: no user Match exec, DNS, authentication or connection.
        string executable = OpenSshClient.Executable;
        string arguments = "-G -F NUL -o CanonicalizeHostname=no -D " + SshConnection.Quote("[::1]:12345") + " 127.0.0.1";
        var watch = Stopwatch.StartNew();
        DiagnosticProcessResult result;
        try { result = DiagnosticProcess.Run(executable, arguments, timeoutMs, CancellationToken.None); }
        catch (Exception ex) { throw new Exception("Native IPv6 SSH parser smoke failed: client=" + Path.GetFileName(executable) + "; elapsed=" + watch.ElapsedMilliseconds + "ms; exception=" + ex.GetType().Name); }
        string details = "; client=" + Path.GetFileName(executable) + "; elapsed=" + watch.ElapsedMilliseconds + "ms; exit=" + result.ExitCode + "; truncated=" + result.Truncated;
        Assert(result.ExitCode == 0 && !result.Truncated, "Native OpenSSH rejected bracketed IPv6 dynamic forwarding" + details);
        string forwarding = null;
        foreach (var line in result.Output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
            if (line.StartsWith("dynamicforward ", StringComparison.OrdinalIgnoreCase)) forwarding = line.Substring("dynamicforward ".Length);
        Assert(forwarding != null && forwarding.Contains("::1") && forwarding.Contains(port.ToString(CultureInfo.InvariantCulture)), "Native OpenSSH did not retain the requested IPv6 dynamic forward" + details);
    }

    private static IPEndPoint FakeForwardEndpoint(string endpoint)
    {
        // Independent OpenSSH-style oracle. In particular, do not accept raw
        // IPv6 by merely taking its last colon, or call production SocksEndpoint.
        string host, portText;
        IPAddress address;
        if (endpoint.StartsWith("[", StringComparison.Ordinal)) {
            int close = endpoint.IndexOf(']');
            if (close < 2 || close != endpoint.LastIndexOf(']') || close + 1 >= endpoint.Length || endpoint[close + 1] != ':') throw new FormatException("Invalid bracketed forward");
            host = endpoint.Substring(1, close - 1); portText = endpoint.Substring(close + 2);
            if (!IPAddress.TryParse(host, out address) || address.AddressFamily != AddressFamily.InterNetworkV6) throw new FormatException("Forward brackets require IPv6");
        } else {
            int colon = endpoint.IndexOf(':');
            if (colon < 1 || colon != endpoint.LastIndexOf(':')) throw new FormatException("IPv6 forward must be bracketed");
            host = endpoint.Substring(0, colon); portText = endpoint.Substring(colon + 1);
            if (host == "localhost") address = Environment.GetEnvironmentVariable("PROGO_TEST_SSH_LOCALHOST_FAMILY") == "v6" ? IPAddress.IPv6Loopback : IPAddress.Loopback;
            else if (!IPAddress.TryParse(host, out address) || address.AddressFamily != AddressFamily.InterNetwork) throw new FormatException("Fixture expects a literal address or localhost");
        }
        int port;
        if (!Int32.TryParse(portText, NumberStyles.None, CultureInfo.InvariantCulture, out port) || port < 1 || port > 65535) throw new FormatException("Invalid forward port");
        if (!IPAddress.IsLoopback(address)) throw new FormatException("Fixture must stay on loopback");
        return new IPEndPoint(address, port);
    }

    private static byte[] FakeReadExact(Stream stream, int count)
    {
        var bytes = new byte[count]; int offset = 0;
        while (offset < count) {
            int read = stream.Read(bytes, offset, count - offset);
            if (read == 0) throw new EndOfStreamException();
            offset += read;
        }
        return bytes;
    }

    private static int FakeSsh(string[] args)
    {
        var capture = Environment.GetEnvironmentVariable("PROGO_TEST_SSH_ARGV");
        if (!String.IsNullOrEmpty(capture)) File.WriteAllText(capture, new JavaScriptSerializer().Serialize(args));
        var mode = args[args.Length - 1];
        var launches = Environment.GetEnvironmentVariable("PROGO_TEST_SSH_LAUNCH_DIR");
        if (!String.IsNullOrEmpty(launches)) {
            string pid = Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture);
            string path = Path.Combine(launches, pid + "-" + Guid.NewGuid().ToString("N") + ".json");
            File.WriteAllText(path + ".pending", new JavaScriptSerializer().Serialize(new[] { mode, pid, Array.IndexOf(args, "BatchMode=yes") >= 0 ? "batch" : "interactive" }));
            File.Move(path + ".pending", path);
        }
        if (mode == "bad-host") { Console.Error.WriteLine("Host key verification failed."); return 1; }
        if (mode == "denied") { Console.Error.WriteLine("Permission denied (publickey)."); return 1; }
        if (mode == "die" || (mode == "batch-only" && Array.IndexOf(args, "BatchMode=yes") < 0)) return 1;
        if (mode == "quiet") { Thread.Sleep(Timeout.Infinite); return 0; }
        if (mode == "slow") {
            var release = Environment.GetEnvironmentVariable("PROGO_TEST_SSH_RELEASE");
            if (String.IsNullOrEmpty(release)) Thread.Sleep(1200);
            else while (!File.Exists(release)) Thread.Sleep(20);
        }
        var endpoint = args[Array.IndexOf(args, "-D") + 1];
        IPEndPoint forward;
        try { forward = FakeForwardEndpoint(endpoint); }
        catch (FormatException ex) { Console.Error.WriteLine("Invalid test dynamic forward: " + ex.Message); return 1; }
        var listener = new TcpListener(forward);
        if (forward.AddressFamily == AddressFamily.InterNetworkV6) listener.Server.DualMode = false;
        listener.Start();
        while (true)
        {
            var client = listener.AcceptTcpClient();
            ThreadPool.QueueUserWorkItem(delegate
            {
                using (client)
                {
                    try
                    {
                        var stream = client.GetStream(); stream.ReadTimeout = stream.WriteTimeout = 1000;
                        if (stream.ReadByte() == 5 && stream.ReadByte() == 1 && stream.ReadByte() == 0)
                        {
                            if (mode == "greeting-gated" || mode == "gated-wrong") {
                                var seen = Environment.GetEnvironmentVariable("PROGO_TEST_SSH_GREETING_SEEN");
                                var release = Environment.GetEnvironmentVariable("PROGO_TEST_SSH_GREETING_RELEASE");
                                if (!File.Exists(seen)) File.WriteAllText(seen, "greeting");
                                while (!File.Exists(release)) Thread.Sleep(20);
                            }
                            stream.Write(new byte[] { mode == "wrong" || mode == "gated-wrong" ? (byte)4 : (byte)5, 0 }, 0, 2); stream.Flush();
                            int version = stream.ReadByte();
                            if (version == -1) return; // Readiness needs only a greeting.
                            var header = FakeReadExact(stream, 3);
                            if (version != 5 || header[0] != 1 || header[1] != 0 || header[2] != 3) return;
                            int length = stream.ReadByte(); if (length < 1) return;
                            var destination = FakeReadExact(stream, length + 2);
                            string host = Encoding.ASCII.GetString(destination, 0, length);
                            int port = (destination[length] << 8) | destination[length + 1];
                            var connectCapture = Environment.GetEnvironmentVariable("PROGO_TEST_SSH_CONNECT");
                            if (!String.IsNullOrEmpty(connectCapture)) File.WriteAllText(connectCapture, new JavaScriptSerializer().Serialize(new[] {
                                host, port.ToString(CultureInfo.InvariantCulture), forward.AddressFamily.ToString() }));
                            byte[] bound = forward.Address.GetAddressBytes();
                            var reply = new byte[4 + bound.Length + 2];
                            reply[0] = 5; reply[3] = forward.AddressFamily == AddressFamily.InterNetworkV6 ? (byte)4 : (byte)1;
                            Buffer.BlockCopy(bound, 0, reply, 4, bound.Length);
                            reply[reply.Length - 2] = (byte)(forward.Port >> 8); reply[reply.Length - 1] = (byte)forward.Port;
                            stream.Write(reply, 0, reply.Length); stream.Flush();
                        }
                    }
                    catch { }
                }
            });
        }
    }
}

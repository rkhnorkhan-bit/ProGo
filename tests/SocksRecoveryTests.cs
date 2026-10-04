using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Web.Script.Serialization;
using ProGo;

internal static class SocksRecoveryTests
{
    private static readonly string Executable = Process.GetCurrentProcess().MainModule.FileName;
    private static int passed;

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
            Test("startup timeout stops its owned child and gives guidance", AsyncTimeout);
            Test("SSH refusal explains key authorization without a hidden prompt", AsyncRefusal);
            Test("asynchronous startup refuses a foreign non-SOCKS listener", AsyncForeignListener);
            Test("async launch failure schedules the first recovery delay once", AsyncLaunchFailure);
            Test("recovery respects configured profile fallback", Fallback);
            Test("manual profile fallback remains available", ManualFallback);
            Test("structured SSH fields reach native child and automatic recovery", StructuredArguments);
            Test("editing SSH fields cancels stale readiness under the same profile ID", StructuredEditCancellation);
            Console.WriteLine("SOCKS recovery tests PASS: " + passed);
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }

    private static void Test(string name, Action action)
    {
        action(); passed++; Console.WriteLine("PASS: " + name);
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
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
        public Fixture(int startupTimeoutMs = 20000)
        {
            var reserve = new TcpListener(IPAddress.Loopback, 0); reserve.Start();
            Settings.SocksPort = ((IPEndPoint)reserve.LocalEndpoint).Port; reserve.Stop();
            Settings.SshProfile = "ready";
            Proxy = new ProxyService(delegate { return Settings; }, delegate(AppSettings s) { Settings = s; },
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
            f.Tick(3600); Assert(!f.Proxy.CurrentPid.HasValue && !f.Proxy.NextRecoveryUtc.HasValue, "Idle app started SSH");
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
            Assert(!f.Proxy.CurrentPid.HasValue && !f.Proxy.NextRecoveryUtc.HasValue, "Dispose resurrected SSH");
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

    private static int FakeSsh(string[] args)
    {
        var capture = Environment.GetEnvironmentVariable("PROGO_TEST_SSH_ARGV");
        if (!String.IsNullOrEmpty(capture)) File.WriteAllText(capture, new JavaScriptSerializer().Serialize(args));
        var mode = args[args.Length - 1];
        if (mode == "denied") { Console.Error.WriteLine("Permission denied (publickey)."); return 1; }
        if (mode == "die" || (mode == "batch-only" && Array.IndexOf(args, "BatchMode=yes") < 0)) return 1;
        if (mode == "quiet") { Thread.Sleep(Timeout.Infinite); return 0; }
        if (mode == "slow") {
            var release = Environment.GetEnvironmentVariable("PROGO_TEST_SSH_RELEASE");
            if (String.IsNullOrEmpty(release)) Thread.Sleep(1200);
            else while (!File.Exists(release)) Thread.Sleep(20);
        }
        var endpoint = args[Array.IndexOf(args, "-D") + 1];
        var port = Int32.Parse(endpoint.Substring(endpoint.LastIndexOf(':') + 1));
        var listener = new TcpListener(IPAddress.Loopback, port);
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
                        var stream = client.GetStream(); stream.ReadTimeout = 1000;
                        if (stream.ReadByte() == 5 && stream.ReadByte() == 1 && stream.ReadByte() == 0)
                        {
                            stream.Write(new byte[] { mode == "wrong" ? (byte)4 : (byte)5, 0 }, 0, 2); stream.Flush();
                        }
                    }
                    catch { }
                }
            });
        }
    }
}

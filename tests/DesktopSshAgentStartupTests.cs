using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        private sealed class AgentStartupContextFixture : IDisposable
        {
            internal readonly string Root = Path.Combine(Path.GetTempPath(), "ProGo-agent-startup-" + Guid.NewGuid().ToString("N"));
            internal readonly UpdateAwareTrayApplicationContext Context;
            private readonly ProxyService proxy;
            private readonly CliProxyBridgeService bridge;
            private readonly Ikev2RelayService relay;
            private readonly HomeVpnService home;
            private readonly ClipboardService clipboard;
            private readonly ConnectionHealthMonitor health;
            internal AgentStartupContextFixture(SettingsService settings)
            {
                Directory.CreateDirectory(Root);
                proxy = new ProxyService(() => settings.Current, value => settings.Save(value), "unused-agent-startup-ssh", () => DateTime.UtcNow, false);
                bridge = new CliProxyBridgeService(settings); relay = new Ikev2RelayService(); home = new HomeVpnService(relay);
                clipboard = new ClipboardService(settings); health = new ConnectionHealthMonitor(() => settings.Current);
                Context = new UpdateAwareTrayApplicationContext(settings, proxy, bridge, home, clipboard, false, health,
                    windowsRestore: () => new WindowsProxyRestoreResult(), cliRestore: () => { },
                    dailyRootProvider: () => Path.Combine(Root, "daily"));
            }
            public void Dispose()
            {
                Context.Dispose(); AgentProbeSettled(Context.SshAgentStartupWork); AgentProbeSettled(Context.DailyWork);
                bridge.Dispose(); clipboard.Dispose(); home.Dispose(); relay.Dispose(); health.Dispose(); proxy.Dispose();
                Directory.Delete(Root, true);
            }
        }
        private sealed class AgentStartupProbeFixture : IDisposable
        {
            internal readonly ManualResetEventSlim Entered = new ManualResetEventSlim(), Release = new ManualResetEventSlim();
            internal readonly SshAgentServiceProbe Probe;
            internal AgentStartNativeFixture Native;
            internal CancellationToken NativeToken;
            internal int Calls, Reads, Worker;
            internal bool Background, Clean;
            internal AgentStartupProbeFixture(string hold = null, Action<AgentStartNativeFixture> configure = null)
            {
                Probe = new SshAgentServiceProbe(token => { Interlocked.Increment(ref Reads); return new SshAgentServiceSnapshot(); }, token => {
                    Interlocked.Increment(ref Calls); Worker = Thread.CurrentThread.ManagedThreadId;
                    Background = Thread.CurrentThread.IsBackground; NativeToken = token;
                    using (var native = new AgentStartNativeFixture()) {
                        Native = native; if (configure != null) configure(native);
                        native.After = phase => { if (phase == hold) {
                            Entered.Set(); if (!Release.Wait(10000)) throw new TimeoutException("Startup native hold was not released.");
                        } };
                        try { return SshAgentServiceStart.TryStart(token, native); }
                        finally { Clean = native.Clean; }
                    }
                });
            }
            public void Dispose()
            { Release.Set(); AgentProbeSettled(Probe.PendingWork); Entered.Dispose(); Release.Dispose(); }
        }
        private static void SshAgentStartupWorkflow(SettingsService settings)
        {
            Console.WriteLine("SSH-agent startup checks BEGIN"); int baseline = passed;
            var original = settings.Current.Clone(); bool previous = Control.CheckForIllegalCrossThreadCalls;
            try {
                var configured = AppSettings.Defaults(); configured.AutoStartSocks = configured.AutoCliProxy = configured.AutoSystemProxy = false;
                configured.TrayCloseExplained = true; settings.Save(configured); Control.CheckForIllegalCrossThreadCalls = true;
                AgentStartupBoundaries(settings);
                AgentStartupConcurrentReservation();
                AgentStartupNativeOwner(settings);
                AgentStartupOutcomes(settings);
                AgentStartupTimeout(settings);
                AgentStartupClose(settings, false);
                AgentStartupClose(settings, true);
                AgentStartupShutdown(settings, false);
                AgentStartupShutdown(settings, true);
                AgentStartupForeignRead(settings);
                AgentStartupLogOwnership(settings);
            } finally { Control.CheckForIllegalCrossThreadCalls = previous; settings.Save(original); }
            Console.WriteLine("SSH-agent startup checks PASS: " + (passed - baseline));
        }
        private static void AgentStartupBoundaries(SettingsService settings)
        {
            using (var native = new AgentStartupProbeFixture()) using (var owner = new AgentStartupContextFixture(settings)) {
                var context = owner.Context;
                Check(context.SshAgentStartupWork.IsCompleted && context.SshAgentStartupState == SshAgentStartupState.NotStarted &&
                    context.SshAgentStartupResult == null && native.Calls == 0 && Field(context, "mainWindow") == null,
                    "constructing the application context never checks or starts the agent or creates a dashboard");
                bool invalid = true;
                foreach (int budget in new int[] { -1, 0, 30001 }) {
                    bool rejected = false; try { context.StartSshAgentAsync(native.Probe, budget, message => { }); }
                    catch (ArgumentOutOfRangeException) { rejected = true; } invalid &= rejected;
                }
                Check(invalid && native.Calls == 0 && context.SshAgentStartupState == SshAgentStartupState.NotStarted,
                    "startup rejects invalid deadlines before reserving a native operation");
                var wrongThread = Task.Run(() => {
                    try { context.StartSshAgentAsync(native.Probe, 3000, message => { }); return false; }
                    catch (InvalidOperationException) { return true; }
                }); AgentProbeSettled(wrongThread);
                Check(wrongThread.Result && native.Calls == 0 && context.SshAgentStartupWork.IsCompleted,
                    "only the UI owner can schedule the application startup operation");
                context.Dispose(); var late = context.StartSshAgentAsync(native.Probe, 3000, message => { });
                Check(late.IsCompleted && native.Calls == 0 && context.SshAgentStartupState == SshAgentStartupState.NotStarted,
                    "a disposed application owner cannot create its first startup operation");
            }
            using (var native = new AgentStartupProbeFixture()) {
                var session = new SshAgentStartupSession(native.Probe); session.Close(); int queued = 0;
                Check(session.Schedule(action => queued++).IsCompleted && queued == 0 && native.Calls == 0 && session.State == SshAgentStartupState.Cancelled,
                    "a closed unstarted session never queues or reopens its lifetime");
                var failed = new SshAgentStartupSession(native.Probe);
                var work = failed.Schedule(action => { throw new InvalidOperationException("PRIVATE_DISPATCH_FAILURE"); }); AgentProbeSettled(work);
                Check(!work.IsFaulted && failed.Result.State == SshAgentProbeState.Unavailable && native.Calls == 0 &&
                    !failed.Result.ToDisplay().Contains("PRIVATE_"), "lost startup dispatch settles safely without native work or raw exception data");
                failed.Close();
            }
        }
        private static void AgentStartupConcurrentReservation()
        {
            using (var native = new AgentStartupProbeFixture()) {
                var session = new SshAgentStartupSession(native.Probe); Action queued = null; int dispatches = 0;
                var results = new Task[12];
                Action<Action> enqueue = action => { Interlocked.Increment(ref dispatches); queued = action; };
                var callers = Enumerable.Range(0, 12).Select(i => Task.Run(() => { results[i] = session.Schedule(enqueue); })).ToArray();
                AgentProbeSettled(Task.WhenAll(callers));
                Check(dispatches == 1 && native.Calls == 0 && results.All(t => Object.ReferenceEquals(t, session.Work)) && !session.Work.IsCompleted,
                    "twelve parallel startup callers reserve one shared task before dispatch");
                queued(); queued(); AgentProbeSettled(session.Work);
                Check(native.Calls == 1 && native.Reads == 0 && native.Clean && session.Result.StartAttempt.State == SshAgentStartState.Running &&
                    Object.ReferenceEquals(session.Work, session.Schedule(enqueue)), "duplicate dispatch and later schedules cannot start a second startup worker");
                session.Close();
            }
        }
        private static void AgentStartupNativeOwner(SettingsService settings)
        {
            using (var native = new AgentStartupProbeFixture("before-query")) using (var owner = new AgentStartupContextFixture(settings))
            using (var timer = new System.Windows.Forms.Timer { Interval = 15 }) {
                int ticks = 0, reports = 0, reportThread = 0, ui = Thread.CurrentThread.ManagedThreadId;
                var context = owner.Context; timer.Tick += delegate { ticks++; }; timer.Start();
                var work = context.StartSshAgentAsync(native.Probe, 10000, message => { reports++; reportThread = Thread.CurrentThread.ManagedThreadId; });
                Check(!work.IsCompleted && native.Calls == 0 && context.SshAgentStartupState == SshAgentStartupState.Queued &&
                    Object.ReferenceEquals(work, context.StartSshAgentAsync(native.Probe)), "startup reserves its complete task before the first UI dispatch");
                try {
                    PumpUntil(() => native.Entered.IsSet && ticks >= 3);
                    Check(!work.IsCompleted && !native.Probe.PendingWork.IsCompleted && native.Calls == 1 && native.Worker != ui && native.Background &&
                        context.SshAgentStartupState == SshAgentStartupState.Checking && context.SshAgentStartupResult == null && ticks >= 3,
                        "UI heartbeat continues while the startup service operation is physically held on its native worker");
                    native.Release.Set(); AgentProbeSettled(work);
                    Check(!work.IsFaulted && native.Clean && native.Native.Starts == 1 && context.SshAgentStartupResult.StartAttempt.State == SshAgentStartState.Running &&
                        context.SshAgentStartupState == SshAgentStartupState.Completed && reports == 1 && reportThread != ui,
                        "startup result and background log settle without a UI pump after actual service cleanup");
                    Check(Object.ReferenceEquals(work, context.StartSshAgentAsync(native.Probe)) && native.Calls == 1 &&
                        !context.SshAgentStartupResult.ToDisplay().Contains("аутентифицирован"), "completed startup stays one-shot and does not prove SSH authentication");
                } finally { native.Release.Set(); AgentProbeSettled(work); }
            }
        }
        private static void AgentStartupOutcomes(SettingsService settings)
        {
            for (int variant = 0; variant < 5; variant++) {
                int current = variant; string report = null;
                using (var native = new AgentStartupProbeFixture(configure: n => {
                    if (current == 0) n.Read.State = 4;
                    if (current == 2) n.Read.Start = 4;
                    if (current == 3) n.Read.StatusOpenError = 1060;
                    if (current == 4) n.OpenServiceError = 5;
                })) using (var owner = new AgentStartupContextFixture(settings)) {
                    var context = owner.Context; var work = context.StartSshAgentAsync(native.Probe, 3000, message => report = message);
                    PumpUntil(() => work.IsCompleted); var result = context.SshAgentStartupResult;
                    var expected = variant < 2 ? SshAgentStartState.Running : variant == 2 ? SshAgentStartState.Disabled : variant == 3 ? SshAgentStartState.Missing : SshAgentStartState.AccessDenied;
                    Check(result.State == SshAgentProbeState.Completed && result.StartAttempt.State == expected && native.Native.Starts == (variant == 1 ? 1 : 0) &&
                        native.Clean && native.Reads == 0 && report.StartsWith("SSH-agent startup result: ") && report.Contains(result.ToDisplay()),
                        "application startup preserves the typed service outcome and safe next-action log: " + expected + "/" + variant);
                    Check(Object.ReferenceEquals(work, context.StartSshAgentAsync(native.Probe)) && native.Calls == 1,
                        "startup outcome never causes an implicit retry loop: " + variant);
                }
            }
        }
        private static void AgentStartupTimeout(SettingsService settings)
        {
            using (var native = new AgentStartupProbeFixture("before-query")) using (var owner = new AgentStartupContextFixture(settings)) {
                int reports = 0; var context = owner.Context;
                var work = context.StartSshAgentAsync(native.Probe, 1000, message => reports++);
                try {
                    PumpUntil(() => native.Entered.IsSet); AgentProbeSettled(work);
                    var result = context.SshAgentStartupResult; var raw = native.Probe.PendingWork;
                    Check(result.State == SshAgentProbeState.TimedOut && !raw.IsCompleted && native.NativeToken.IsCancellationRequested && reports == 1 &&
                        context.SshAgentStartupState == SshAgentStartupState.Completed, "startup timeout records an unknown service result while the probe retains its pending native owner");
                    var busy = native.Probe.ReadAsync(1000, CancellationToken.None).GetAwaiter().GetResult();
                    Check(busy.State == SshAgentProbeState.Busy && native.Calls == 1 && Object.ReferenceEquals(work, context.StartSshAgentAsync(native.Probe)),
                        "diagnostics and repeated startup cannot bypass a timed-out native startup owner");
                    native.Release.Set(); AgentProbeSettled(raw);
                    Check(native.Clean && native.Native.Starts == 0 && Object.ReferenceEquals(result, context.SshAgentStartupResult) && reports == 1,
                        "late native timeout cleanup cannot overwrite the recorded startup timeout or start the service");
                } finally { native.Release.Set(); AgentProbeSettled(native.Probe.PendingWork); }
            }
        }
        private static void AgentStartupClose(SettingsService settings, bool queued)
        {
            using (var native = new AgentStartupProbeFixture("before-query")) using (var owner = new AgentStartupContextFixture(settings)) {
                int reports = 0; var context = owner.Context; var work = context.StartSshAgentAsync(native.Probe, 10000, message => reports++);
                try {
                    if (!queued) PumpUntil(() => native.Entered.IsSet);
                    var watch = Stopwatch.StartNew(); context.Dispose(); AgentProbeSettled(work);
                    Check(watch.ElapsedMilliseconds < 1500 && !work.IsFaulted && context.SshAgentStartupState == SshAgentStartupState.Cancelled &&
                        context.SshAgentStartupResult == null && reports == 0 && (queued ? native.Calls == 0 : native.NativeToken.IsCancellationRequested && !native.Probe.PendingWork.IsCompleted),
                        "owner close settles its queued/waiter task without UI pump or false native completion: " + queued);
                    native.Release.Set(); AgentProbeSettled(native.Probe.PendingWork); Application.DoEvents();
                    Check(native.Calls == (queued ? 0 : 1) && (queued || native.Clean && native.Native.Starts == 0) && reports == 0 &&
                        context.SshAgentStartupResult == null && Object.ReferenceEquals(work, context.StartSshAgentAsync(native.Probe)),
                        "late dispatch/native return cannot publish or restart after owner disposal: " + queued);
                } finally { native.Release.Set(); AgentProbeSettled(native.Probe.PendingWork); }
            }
        }
        private static void AgentStartupShutdown(SettingsService settings, bool queued)
        {
            using (var native = new AgentStartupProbeFixture("before-query")) using (var owner = new AgentStartupContextFixture(settings)) {
                var context = owner.Context; int reports = 0; var work = context.StartSshAgentAsync(native.Probe, 10000, message => reports++);
                try {
                    if (!queued) PumpUntil(() => native.Entered.IsSet);
                    var shutdown = context.RequestShutdownAsync(); PumpUntil(() => shutdown.IsCompleted); AgentProbeSettled(work);
                    Check(shutdown.Result && context.SshAgentStartupState == SshAgentStartupState.Cancelled && context.SshAgentStartupResult == null && reports == 0 &&
                        (queued ? native.Calls == 0 : native.NativeToken.IsCancellationRequested && !native.Probe.PendingWork.IsCompleted),
                        "confirmed application shutdown cancels startup without waiting for or stopping the shared native service: " + queued);
                    context.CancelShutdown(); PumpUntil(() => !(bool)Field(context, "shutdownPrepared"));
                    Check(Object.ReferenceEquals(work, context.StartSshAgentAsync(native.Probe)) && native.Calls == (queued ? 0 : 1),
                        "cancelled shutdown does not create an automatic second startup attempt: " + queued);
                    native.Release.Set(); AgentProbeSettled(native.Probe.PendingWork);
                    Check((queued || native.Clean && native.Native.Starts == 0) && reports == 0 && context.SshAgentStartupResult == null,
                        "released startup after shutdown cancellation still performs no late service start or publication: " + queued);
                } finally { native.Release.Set(); AgentProbeSettled(native.Probe.PendingWork); }
            }
        }
        private static void AgentStartupForeignRead(SettingsService settings)
        {
            using (var foreign = new AgentProbeFixture()) using (var owner = new AgentStartupContextFixture(settings)) {
                var reading = foreign.Probe.ReadAsync(10000, CancellationToken.None);
                try {
                    Check(foreign.Entered.Wait(3000), "foreign diagnostic read owns the service slot before startup begins");
                    var work = owner.Context.StartSshAgentAsync(foreign.Probe, 3000, message => { }); PumpUntil(() => work.IsCompleted);
                    owner.Context.Dispose();
                    Check(owner.Context.SshAgentStartupResult.State == SshAgentProbeState.Busy && !foreign.NativeToken.IsCancellationRequested && !reading.IsCompleted && foreign.Calls == 1,
                        "busy startup and owner disposal never cancel or join another diagnostic caller's native operation");
                } finally { foreign.Release.Set(); AgentProbeSettled(reading); }
            }
        }
        private static void AgentStartupLogOwnership(SettingsService settings)
        {
            using (var native = new AgentStartupProbeFixture()) using (var owner = new AgentStartupContextFixture(settings))
            using (var entered = new ManualResetEventSlim()) using (var release = new ManualResetEventSlim()) {
                int ui = Thread.CurrentThread.ManagedThreadId, logger = 0;
                var work = owner.Context.StartSshAgentAsync(native.Probe, 3000, message => {
                    logger = Thread.CurrentThread.ManagedThreadId; entered.Set(); if (!release.Wait(10000)) throw new TimeoutException();
                });
                try {
                    PumpUntil(() => entered.IsSet); var result = owner.Context.SshAgentStartupResult; var watch = Stopwatch.StartNew(); owner.Context.Dispose();
                    Check(!work.IsCompleted && native.Probe.PendingWork.IsCompleted && logger != ui && watch.ElapsedMilliseconds < 1500 &&
                        result.StartAttempt.State == SshAgentStartState.Running, "a held background log keeps its work owned but never blocks the UI or native service slot on close");
                    release.Set(); AgentProbeSettled(work);
                    Check(!work.IsFaulted && Object.ReferenceEquals(result, owner.Context.SshAgentStartupResult) && native.Calls == 1,
                        "released background log settles without replacing the already observed result or reopening the owner");
                } finally { release.Set(); AgentProbeSettled(work); }
            }
            using (var native = new AgentStartupProbeFixture()) using (var owner = new AgentStartupContextFixture(settings)) {
                var work = owner.Context.StartSshAgentAsync(native.Probe, 3000, message => { throw new IOException("PRIVATE_LOG_FAILURE"); });
                PumpUntil(() => work.IsCompleted);
                Check(!work.IsFaulted && owner.Context.SshAgentStartupResult.StartAttempt.State == SshAgentStartState.Running && native.Clean &&
                    !owner.Context.SshAgentStartupResult.ToDisplay().Contains("PRIVATE_"), "background logger failure cannot erase the service result or fault an abandoned startup task");
            }
        }
    }
}

using System;
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
        private static void AgentProbeSettled(Task task)
        { if (!SpinWait.SpinUntil(() => task.IsCompleted, 5000)) throw new TimeoutException("Agent probe fixture did not settle without a UI pump."); }
        private sealed class AgentProbeFixture : IDisposable
        {
            internal readonly ManualResetEventSlim Entered = new ManualResetEventSlim(), Release = new ManualResetEventSlim();
            internal readonly SshAgentServiceProbe Probe;
            internal int Calls, Worker;
            internal bool Background, Clean;
            internal CancellationToken NativeToken;
            internal AgentServiceNativeFixture LastNative;
            internal AgentProbeFixture(bool held = true)
            {
                Probe = new SshAgentServiceProbe(token => {
                    int call = Interlocked.Increment(ref Calls); Worker = Thread.CurrentThread.ManagedThreadId;
                    Background = Thread.CurrentThread.IsBackground; NativeToken = token;
                    using (var native = new AgentServiceNativeFixture { State = call == 1 ? 4u : 1u }) {
                        LastNative = native;
                        native.After = stage => {
                            if (stage != "status-query") return;
                            Entered.Set();
                            if (held && call == 1 && !Release.Wait(10000)) throw new TimeoutException("Held SCM fixture release deadline.");
                        };
                        try { return SshAgentServiceStatus.Read(token, native); }
                        finally { Clean = native.Clean; }
                    }
                });
            }
            public void Dispose()
            { Release.Set(); AgentProbeSettled(Probe.PendingWork); Release.Dispose(); Entered.Dispose(); }
        }
        private sealed class AgentProbeNoDispatchContext : SynchronizationContext
        {
            internal int Posts;
            public override void Post(SendOrPostCallback callback, object state) { Interlocked.Increment(ref Posts); }
        }

        private static void SshAgentProbeWorkflow()
        {
            AgentProbeBoundaries();
            AgentProbeTimeout();
            AgentProbeCancellation();
            AgentProbeBusyOwnership();
            AgentProbeForms();
        }
        private static void AgentProbeBoundaries()
        {
            int calls = 0;
            var unused = new SshAgentServiceProbe(token => { calls++; return new SshAgentServiceSnapshot(); });
            foreach (int budget in new[] { -1, 0, 30001 }) {
                bool rejected = false;
                try { unused.ReadAsync(budget, CancellationToken.None).GetAwaiter().GetResult(); }
                catch (ArgumentOutOfRangeException) { rejected = true; }
                Check(rejected && calls == 0 && unused.PendingWork.IsCompleted, "agent probe rejects an invalid wait budget before native work: " + budget);
            }
            using (var source = new CancellationTokenSource()) {
                source.Cancel(); bool cancelled = false;
                try { unused.ReadAsync(1000, source.Token).GetAwaiter().GetResult(); }
                catch (OperationCanceledException) { cancelled = true; }
                Check(cancelled && calls == 0 && unused.PendingWork.IsCompleted, "pre-cancelled agent waiter starts no native worker");
            }
            var previous = SynchronizationContext.Current; var context = new AgentProbeNoDispatchContext();
            using (var fixture = new AgentProbeFixture(false)) {
                int owner = Thread.CurrentThread.ManagedThreadId;
                try {
                    SynchronizationContext.SetSynchronizationContext(context);
                    var read = fixture.Probe.ReadAsync(3000, CancellationToken.None); AgentProbeSettled(read);
                    var result = read.GetAwaiter().GetResult();
                    Check(result.State == SshAgentProbeState.Completed && result.Snapshot.RuntimeState == SshAgentRuntimeState.Running &&
                        result.Snapshot.StartMode == SshAgentStartMode.Manual && fixture.Clean && fixture.Probe.PendingWork.IsCompleted,
                        "agent probe returns the actual typed SCM result after native resource cleanup");
                    Check(fixture.Worker != owner && fixture.Background && context.Posts == 0,
                        "agent inspection and its waiter settle on background work without the caller synchronization context");
                } finally { SynchronizationContext.SetSynchronizationContext(previous); }
            }
            var failed = new SshAgentServiceProbe(token => { throw new InvalidOperationException("PRIVATE_AGENT_WORKER_FAILURE"); });
            var failure = failed.ReadAsync(3000, CancellationToken.None).GetAwaiter().GetResult();
            Check(failure.State == SshAgentProbeState.Unavailable && failure.Snapshot == null && failed.PendingWork.IsCompleted && !failed.PendingWork.IsFaulted &&
                !failure.ToDisplay().Contains("PRIVATE_"), "unexpected worker failure settles as safe unknown data without an unobserved fault");
            var missing = new SshAgentServiceProbe(token => null);
            Check(missing.ReadAsync(3000, CancellationToken.None).GetAwaiter().GetResult().State == SshAgentProbeState.Unavailable,
                "a missing native snapshot never becomes a completed service result");
        }
        private static void AgentProbeTimeout()
        {
            using (var fixture = new AgentProbeFixture()) {
                var watch = Stopwatch.StartNew(); var read = fixture.Probe.ReadAsync(1000, CancellationToken.None);
                Check(fixture.Entered.Wait(3000), "held production SCM query enters its controlled native boundary");
                var raw = fixture.Probe.PendingWork; AgentProbeSettled(read); var result = read.GetAwaiter().GetResult();
                Check(result.State == SshAgentProbeState.TimedOut && result.Snapshot == null && watch.ElapsedMilliseconds < 3500,
                    "agent wait returns a bounded timeout without inventing a service snapshot");
                Check(!raw.IsCompleted && fixture.NativeToken.IsCancellationRequested && fixture.Calls == 1 &&
                    result.ToDisplay().Contains("ещё может выполняться"), "timed-out native work remains owned and honestly reported until physical completion");
                var requests = Enumerable.Range(0, 12).Select(i => Task.Run(() => fixture.Probe.ReadAsync(1000, CancellationToken.None))).ToArray();
                var busy = Task.WhenAll(requests); AgentProbeSettled(busy);
                Check(busy.GetAwaiter().GetResult().All(r => r.State == SshAgentProbeState.Busy && r.Snapshot == null) &&
                    fixture.Calls == 1 && Object.ReferenceEquals(raw, fixture.Probe.PendingWork),
                    "concurrent retries after timeout cannot replace or duplicate the held SCM operation");
                fixture.Release.Set(); AgentProbeSettled(raw);
                Check(fixture.Clean && !raw.IsFaulted && !fixture.LastNative.Calls.Contains("config-open"),
                    "late SCM return observes cancellation before the next query and releases its handles without a UI pump");
                var fresh = fixture.Probe.ReadAsync(3000, CancellationToken.None); AgentProbeSettled(fresh); var next = fresh.GetAwaiter().GetResult();
                Check(next.State == SshAgentProbeState.Completed && next.Snapshot.RuntimeState == SshAgentRuntimeState.Stopped && fixture.Calls == 2 && fixture.Clean &&
                    !Object.ReferenceEquals(raw, fixture.Probe.PendingWork), "retry after full settlement performs a fresh read rather than publishing the abandoned result");
            }
        }
        private static void AgentProbeCancellation()
        {
            using (var fixture = new AgentProbeFixture())
            using (var source = new CancellationTokenSource()) {
                var read = fixture.Probe.ReadAsync(10000, source.Token);
                Check(fixture.Entered.Wait(3000), "cancellable SCM probe reaches the held native call");
                var raw = fixture.Probe.PendingWork; var watch = Stopwatch.StartNew(); source.Cancel(); AgentProbeSettled(read);
                bool cancelled = false; try { read.GetAwaiter().GetResult(); } catch (OperationCanceledException) { cancelled = true; }
                Check(cancelled && read.IsCanceled && watch.ElapsedMilliseconds < 1500 && !raw.IsCompleted && fixture.NativeToken.IsCancellationRequested,
                    "caller cancellation ends only the wait promptly while the native worker retains ownership");
                Check(fixture.Probe.ReadAsync(1000, CancellationToken.None).GetAwaiter().GetResult().State == SshAgentProbeState.Busy && fixture.Calls == 1,
                    "cancellation cannot free the single-flight slot before native cleanup finishes");
                fixture.Release.Set(); AgentProbeSettled(raw);
                Check(fixture.Clean && !raw.IsFaulted && fixture.Calls == 1, "cancelled native worker settles its resources without a live UI context or duplicate query");
            }
        }
        private static void AgentProbeBusyOwnership()
        {
            using (var fixture = new AgentProbeFixture())
            using (var rejected = new CancellationTokenSource()) {
                var first = fixture.Probe.ReadAsync(10000, CancellationToken.None);
                Check(fixture.Entered.Wait(3000), "first caller owns the active SCM read");
                var other = fixture.Probe.ReadAsync(1000, rejected.Token).GetAwaiter().GetResult(); rejected.Cancel();
                Check(other.State == SshAgentProbeState.Busy && !fixture.NativeToken.IsCancellationRequested && fixture.Calls == 1,
                    "a rejected busy caller cannot cancel the existing owner's native read");
                fixture.Release.Set(); AgentProbeSettled(first);
                Check(first.GetAwaiter().GetResult().State == SshAgentProbeState.Completed && fixture.Clean,
                    "the original owner receives its typed result after an unrelated busy caller leaves");
            }
        }
        private static SshProfileDiagnosticResult AgentProbeProfile(SshProfileSetting profile, CancellationToken token, SshAgentServiceProbe probe, int budget)
        { return SshProfileDiagnostics.Check(profile, token, Application.ExecutablePath, 1500, probe, budget); }
        private static void AgentProbeForms()
        {
            bool previous = Control.CheckForIllegalCrossThreadCalls; Control.CheckForIllegalCrossThreadCalls = true;
            try {
                AgentProbeTimeoutForms();
                AgentProbeQueuedCancel();
                AgentProbeClosedForm(false);
                AgentProbeClosedForm(true);
            } finally { Control.CheckForIllegalCrossThreadCalls = previous; }
        }
        private static void AgentProbeTimeoutForms()
        {
            using (var fixture = new AgentProbeFixture())
            using (var form = new SshDiagnosticForm(new SshProfileSetting { Target = "fixture-ok" }, (p, t) => AgentProbeProfile(p, t, fixture.Probe, 1000)))
            using (var timer = new System.Windows.Forms.Timer { Interval = 15 }) {
                int ticks = 0; timer.Tick += delegate { ticks++; }; timer.Start(); form.Show();
                PumpUntil(() => fixture.Entered.IsSet && ticks >= 3);
                Check(!form.Work.IsCompleted && !fixture.Probe.PendingWork.IsCompleted && ticks >= 3,
                    "SSH diagnostic UI heartbeat continues while the real service reader is held");
                PumpUntil(() => form.Work.IsCompleted);
                var report = (TextBox)Field(form, "report"); var retry = (Button)Field(form, "retry"); var close = (Button)Field(form, "cancel");
                Check(report.Text.Contains("Время ожидания службы") && report.Text.Contains("fixture.example.org") && report.Text.Contains("Проверка ключей отложена") &&
                    retry.Enabled && close.Text == "Закрыть" && !fixture.Probe.PendingWork.IsCompleted,
                    "service timeout preserves the selected SSH configuration and restores actions without reporting ready keys");
                Shot(form, "ssh-agent-service-timeout");
                using (var another = new SshDiagnosticForm(new SshProfileSetting { Target = "fixture-ok" }, (p, t) => AgentProbeProfile(p, t, fixture.Probe, 1000))) {
                    another.Show(); PumpUntil(() => another.Work != null && another.Work.IsCompleted);
                    Check(((TextBox)Field(another, "report")).Text.Contains("Предыдущая проверка службы") && fixture.Calls == 1 && !fixture.Probe.PendingWork.IsCompleted,
                        "another diagnostic window shares the service gate and reports busy instead of starting a second read");
                    Shot(another, "ssh-agent-service-busy"); another.Close();
                }
                retry.PerformClick(); PumpUntil(() => form.Work.IsCompleted);
                Check(report.Text.Contains("Предыдущая проверка службы") && fixture.Calls == 1,
                    "Retry after a displayed service timeout also respects the still-owned native query");
                form.Close(); fixture.Release.Set(); AgentProbeSettled(fixture.Probe.PendingWork);
                Check(fixture.Clean, "closing timeout results leaves the native owner responsible for final resource cleanup");
            }
        }
        private static void AgentProbeQueuedCancel()
        {
            using (var fixture = new AgentProbeFixture())
            using (var form = new SshDiagnosticForm(new SshProfileSetting { Target = "fixture-ok" }, (p, t) => AgentProbeProfile(p, t, fixture.Probe, 10000))) {
                form.Show(); PumpUntil(() => fixture.Entered.IsSet);
                fixture.Release.Set(); AgentProbeSettled(form.CheckWork);
                Check(!form.Work.IsCompleted, "SSH result publication waits for its UI owner after the background check settles");
                ((Button)Field(form, "cancel")).PerformClick(); PumpUntil(() => form.Work.IsCompleted);
                Check(((TextBox)Field(form, "report")).Text.Length == 0 && ((Label)Field(form, "status")).Text.Contains("отменена") &&
                    ((Button)Field(form, "retry")).Enabled, "Cancel before queued publication suppresses a completed result rather than publishing stale configuration");
                form.Close();
            }
        }
        private static void AgentProbeClosedForm(bool dispose)
        {
            using (var fixture = new AgentProbeFixture())
            using (var form = new SshDiagnosticForm(new SshProfileSetting { Target = "fixture-ok" }, (p, t) => AgentProbeProfile(p, t, fixture.Probe, 10000))) {
                form.Show(); PumpUntil(() => fixture.Entered.IsSet);
                int late = 0; bool closed = false;
                ((TextBox)Field(form, "report")).TextChanged += delegate { if (closed) late++; };
                var work = form.Work; var raw = fixture.Probe.PendingWork;
                if (dispose) form.Dispose(); else form.Close(); closed = true;
                AgentProbeSettled(work); work.GetAwaiter().GetResult();
                Check(form.IsDisposed && form.CheckWork.IsCompleted && !raw.IsCompleted && fixture.NativeToken.IsCancellationRequested,
                    "SSH Close or Dispose settles its diagnostic worker without a UI pump while retaining the raw SCM owner: " + dispose);
                fixture.Release.Set(); AgentProbeSettled(raw); Application.DoEvents();
                Check(late == 0 && fixture.Clean && fixture.Calls == 1 && !raw.IsFaulted,
                    "late service completion after Close or Dispose releases resources without publishing to the dead window: " + dispose);
            }
        }

        private static void SshAgentCoordinationWorkflow()
        {
            int before = passed; Console.WriteLine("SSH agent coordination checks BEGIN");
            string root = Path.Combine(work, "agent-coordination-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try {
                File.WriteAllText(Path.Combine(root, "ssh-add.exe"), "");
                File.WriteAllText(Path.Combine(root, "ssh-keygen.exe"), "");
                for (int operation = 0; operation < 3; operation++) AgentCoordinationKeyOwner(root, operation);
                foreach (bool starting in new[] { false, true }) foreach (bool cancel in new[] { false, true })
                    AgentCoordinationServiceOwner(root, starting, cancel);
                AgentCoordinationFailures();
                AgentCoordinationRace(root);
            } finally { Directory.Delete(root, true); }
            Console.WriteLine("SSH agent coordination checks PASS: " + (passed - before));
        }
        private static bool AgentCoordinationKeysBusy(string root)
        {
            int calls = 0;
            Func<string, string, int, CancellationToken, DiagnosticProcessResult> run = (e, a, ms, t) => {
                calls++; return KeyMatchCapture(AgentFingerprintA);
            };
            var inventory = SshAgentInventory.Read("unused.exe", CancellationToken.None, run);
            var matching = SshProfileKeyMatching.Check(KeyMatchProfile(root, "selected key"), CancellationToken.None, run);
            var addition = SshAgentKeyAdd.Run(KeyAddRequest(root), CancellationToken.None, (e, a, ep, ms, t) => { calls++; return 0; });
            return inventory.State == SshAgentInventoryState.Busy && matching.State == SshProfileKeyMatchState.Busy &&
                addition.State == SshAgentKeyAddState.Busy && calls == 0;
        }
        private static void AgentCoordinationKeyOwner(string root, int operation)
        {
            using (var entered = new ManualResetEventSlim()) using (var release = new ManualResetEventSlim())
            using (var cancellation = new CancellationTokenSource()) {
                int calls = 0;
                Action hold = () => { entered.Set(); if (!release.Wait(15000)) throw new TimeoutException("Key coordination fixture release deadline"); };
                Func<string, string, int, CancellationToken, DiagnosticProcessResult> run = (e, a, ms, t) => { hold(); return KeyMatchCapture(AgentFingerprintA); };
                var owner = Task.Factory.StartNew(() => {
                    try {
                        if (operation == 0) SshAgentInventory.Read("unused.exe", cancellation.Token, run);
                        else if (operation == 1) SshProfileKeyMatching.Check(KeyMatchProfile(root, "selected key"), cancellation.Token, run);
                        else return SshAgentKeyAdd.Run(KeyAddRequest(root), cancellation.Token, (e, a, ep, ms, t) => { hold(); return 0; }).State == SshAgentKeyAddState.Cancelled;
                        return false;
                    } catch (OperationCanceledException) { return true; }
                }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
                var probe = new SshAgentServiceProbe(t => { Interlocked.Increment(ref calls); return new SshAgentServiceSnapshot(); },
                    t => { Interlocked.Increment(ref calls); return new SshAgentServiceStartResult { Snapshot = new SshAgentServiceSnapshot() }; });
                try {
                    Check(entered.Wait(3000), "key operation owns the common slot before SCM competitors: " + operation);
                    var watch = Stopwatch.StartNew();
                    using (var rejected = new CancellationTokenSource()) {
                        var read = probe.ReadAsync(3000, rejected.Token).GetAwaiter().GetResult();
                        var start = probe.StartAsync(3000, rejected.Token).GetAwaiter().GetResult(); rejected.Cancel();
                        Check(read.State == SshAgentProbeState.Busy && start.State == SshAgentProbeState.Busy && calls == 0 &&
                            probe.PendingWork.IsCompleted && !cancellation.IsCancellationRequested && watch.ElapsedMilliseconds < 1500,
                            "SCM read/start return Busy without native work or cancelling the key owner: " + operation);
                    }
                    var startup = new SshAgentStartupSession(probe);
                    try {
                        var work = startup.Schedule(action => action()); AgentProbeSettled(work);
                        Check(startup.State == SshAgentStartupState.Completed && startup.Result.State == SshAgentProbeState.Busy &&
                            calls == 0 && Object.ReferenceEquals(work, startup.Schedule(action => action())),
                            "one-shot startup shares the key owner and cannot retry or start behind it: " + operation);
                    } finally { startup.Close(); }
                    cancellation.Cancel();
                    Check(!owner.IsCompleted && AgentCoordinationKeysBusy(root) &&
                        probe.ReadAsync(1000, CancellationToken.None).GetAwaiter().GetResult().State == SshAgentProbeState.Busy &&
                        probe.StartAsync(1000, CancellationToken.None).GetAwaiter().GetResult().State == SshAgentProbeState.Busy && calls == 0,
                        "cancelled key work retains ownership against every operation until its runner settles: " + operation);
                } finally { release.Set(); AgentProbeSettled(owner); }
                Check(owner.Result && !owner.IsFaulted, "late key success cannot replace cancellation after shared cleanup: " + operation);
                Check(probe.ReadAsync(3000, CancellationToken.None).GetAwaiter().GetResult().State == SshAgentProbeState.Completed &&
                    probe.StartAsync(3000, CancellationToken.None).GetAwaiter().GetResult().State == SshAgentProbeState.Completed && calls == 2,
                    "SCM read and start can acquire a fresh slot after key cleanup: " + operation);
            }
        }
        private static void AgentCoordinationServiceOwner(string root, bool starting, bool cancel)
        {
            using (var reading = new AgentProbeFixture()) using (var startup = new AgentStartupProbeFixture("before-query"))
            using (var source = new CancellationTokenSource()) {
                var probe = starting ? startup.Probe : reading.Probe;
                var entered = starting ? startup.Entered : reading.Entered;
                var release = starting ? startup.Release : reading.Release;
                int competitors = 0;
                var other = new SshAgentServiceProbe(t => { competitors++; return new SshAgentServiceSnapshot(); },
                    t => { competitors++; return new SshAgentServiceStartResult { Snapshot = new SshAgentServiceSnapshot() }; });
                var waiter = starting ? probe.StartAsync(cancel ? 10000 : 1000, source.Token) : probe.ReadAsync(cancel ? 10000 : 1000, source.Token);
                try {
                    Check(entered.Wait(3000) && AgentCoordinationKeysBusy(root),
                        "held native SCM work prevents inventory, fingerprint and add runners: " + starting + "/" + cancel);
                    var raw = probe.PendingWork;
                    Check(other.ReadAsync(1000, CancellationToken.None).GetAwaiter().GetResult().State == SshAgentProbeState.Busy &&
                        other.StartAsync(1000, CancellationToken.None).GetAwaiter().GetResult().State == SshAgentProbeState.Busy && competitors == 0,
                        "separate probe instances cannot bypass the process-wide native owner: " + starting + "/" + cancel);
                    if (cancel) source.Cancel();
                    AgentProbeSettled(waiter); bool expected;
                    try { expected = !cancel && waiter.GetAwaiter().GetResult().State == SshAgentProbeState.TimedOut; }
                    catch (OperationCanceledException) { expected = cancel && waiter.IsCanceled; }
                    var nativeToken = starting ? startup.NativeToken : reading.NativeToken;
                    Check(expected && nativeToken.IsCancellationRequested && !raw.IsCompleted && AgentCoordinationKeysBusy(root) &&
                        other.ReadAsync(1000, CancellationToken.None).GetAwaiter().GetResult().State == SshAgentProbeState.Busy && competitors == 0,
                        "timeout or cancellation ends only the waiter and preserves SCM ownership against keys and other probes: " + starting + "/" + cancel);
                    release.Set(); AgentProbeSettled(raw);
                    Check(!raw.IsFaulted && (starting ? startup.Clean && startup.Native.Starts == 0 : reading.Clean),
                        "late SCM return cleans native handles without a post-cancel start: " + starting + "/" + cancel);
                    Check(other.ReadAsync(3000, CancellationToken.None).GetAwaiter().GetResult().State == SshAgentProbeState.Completed && competitors == 1 &&
                        SshAgentInventory.Read("unused.exe", CancellationToken.None, (e, a, ms, t) => KeyMatchCapture(AgentFingerprintA)).State == SshAgentInventoryState.Available,
                        "fresh SCM and key work can enter only after physical native cleanup: " + starting + "/" + cancel);
                } finally { release.Set(); AgentProbeSettled(probe.PendingWork); }
            }
        }
        private static void AgentCoordinationFailures()
        {
            for (int variant = 0; variant < 3; variant++) {
                int current = variant;
                var probe = new SshAgentServiceProbe(t => {
                    if (current == 0) throw new IOException("PRIVATE_COORDINATION_FAILURE");
                    if (current == 1) return null;
                    return new SshAgentServiceSnapshot();
                });
                var result = (variant == 2 ? probe.StartAsync(3000, CancellationToken.None) : probe.ReadAsync(3000, CancellationToken.None)).GetAwaiter().GetResult();
                using (var lease = SshReadOnlyLease.TryAcquire(CancellationToken.None)) {
                    Check(result.State == SshAgentProbeState.Unavailable && !result.ToDisplay().Contains("PRIVATE_") &&
                        probe.PendingWork.IsCompleted && !probe.PendingWork.IsFaulted && lease != null,
                        "exception, missing snapshot or unsupported start releases only its own common lease: " + variant);
                }
            }
        }
        private static void AgentCoordinationRace(string root)
        {
            using (var begin = new ManualResetEventSlim()) using (var entered = new ManualResetEventSlim()) using (var release = new ManualResetEventSlim()) {
                int nativeCalls = 0, busy = 0;
                Action hold = () => { Interlocked.Increment(ref nativeCalls); entered.Set(); if (!release.Wait(15000)) throw new TimeoutException("Mixed coordination race deadline"); };
                var owners = Enumerable.Range(0, 12).Select(i => Task.Factory.StartNew(() => {
                    if (!begin.Wait(10000)) throw new TimeoutException("Mixed coordination race start");
                    bool rejected;
                    Func<string, string, int, CancellationToken, DiagnosticProcessResult> run = (e, a, ms, t) => { hold(); return KeyMatchCapture(AgentFingerprintA); };
                    if (i % 5 < 2) {
                        var probe = new SshAgentServiceProbe(t => { hold(); return new SshAgentServiceSnapshot(); },
                            t => { hold(); return new SshAgentServiceStartResult { Snapshot = new SshAgentServiceSnapshot() }; });
                        var result = (i % 5 == 0 ? probe.ReadAsync(10000, CancellationToken.None) : probe.StartAsync(10000, CancellationToken.None)).GetAwaiter().GetResult();
                        rejected = result.State == SshAgentProbeState.Busy;
                        if (!rejected && result.State != SshAgentProbeState.Completed) throw new Exception("Mixed SCM winner did not complete");
                    } else if (i % 5 == 2) rejected = SshAgentInventory.Read("unused.exe", CancellationToken.None, run).State == SshAgentInventoryState.Busy;
                    else if (i % 5 == 3) rejected = SshProfileKeyMatching.Check(KeyMatchProfile(root, "selected key"), CancellationToken.None, run).State == SshProfileKeyMatchState.Busy;
                    else rejected = SshAgentKeyAdd.Run(KeyAddRequest(root), CancellationToken.None, (e, a, ep, ms, t) => { hold(); return 0; }).State == SshAgentKeyAddState.Busy;
                    if (rejected) Interlocked.Increment(ref busy);
                    return rejected;
                }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray();
                var all = Task.WhenAll(owners);
                try {
                    begin.Set();
                    Check(entered.Wait(3000) && SpinWait.SpinUntil(() => Volatile.Read(ref busy) == 11, 5000) && Volatile.Read(ref nativeCalls) == 1 && !all.IsCompleted,
                        "twelve concurrent mixed SCM/key requests admit exactly one native owner and eleven immediate Busy results");
                } finally { release.Set(); AgentProbeSettled(all); }
                using (var lease = SshReadOnlyLease.TryAcquire(CancellationToken.None)) {
                    Check(all.Result.Count(rejected => rejected) == 11 && nativeCalls == 1 && lease != null,
                        "mixed contenders finish without duplicate native work or a leaked shared lease");
                }
            }
        }
    }
}

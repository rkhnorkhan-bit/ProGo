using System;
using System.Diagnostics;
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
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProGo
{
    internal enum SshAgentStartupState { NotStarted, Queued, Checking, Completed, Cancelled }

    // One startup reservation per application owner. Work includes its queued
    // dispatch, waiter, best-effort background log and CTS cleanup. The probe
    // separately owns any native call that outlives cancellation/deadline.
    internal sealed class SshAgentStartupSession
    {
        private readonly object gate = new object();
        private readonly SshAgentServiceProbe probe;
        private readonly int timeoutMilliseconds;
        private readonly Action<string> report;
        private readonly Task idle = Task.FromResult(false);
        private Task work = Task.FromResult(false);
        private TaskCompletionSource<Task> reservation;
        private CancellationTokenSource cancellation;
        private bool scheduled, started, closed;
        private SshAgentStartupState state;
        private SshAgentProbeResult result;
        private readonly TaskCompletionSource<bool> checkedAgent = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        internal SshAgentStartupSession(SshAgentServiceProbe probe, int timeoutMilliseconds = SshAgentServiceProbe.DefaultTimeoutMilliseconds,
            Action<string> report = null)
        {
            if (probe == null) throw new ArgumentNullException("probe");
            if (timeoutMilliseconds < 1 || timeoutMilliseconds > 30000) throw new ArgumentOutOfRangeException("timeoutMilliseconds");
            this.probe = probe; this.timeoutMilliseconds = timeoutMilliseconds; this.report = report;
        }
        internal Task Work { get { lock (gate) return work; } }
        // The bounded waiter settles before best-effort logging. It is not an
        // assertion that the service is running or a selected key can authenticate.
        internal Task CheckCompletion { get { return checkedAgent.Task; } }
        internal SshAgentStartupState State { get { lock (gate) return state; } }
        internal SshAgentProbeResult Result { get { lock (gate) return result; } }

        internal Task Schedule(Action<Action> enqueue)
        {
            if (enqueue == null) throw new ArgumentNullException("enqueue");
            lock (gate) {
                if (scheduled || closed) return work;
                scheduled = true; state = SshAgentStartupState.Queued;
                cancellation = new CancellationTokenSource();
                reservation = new TaskCompletionSource<Task>(TaskCreationOptions.RunContinuationsAsynchronously);
                work = reservation.Task.Unwrap();
                try { enqueue(Begin); }
                catch (Exception) {
                    // A destroyed dispatcher cannot leave an unsettled queued
                    // task. If an inline dispatcher already began, keep its owner.
                    if (!started && !closed) {
                        result = new SshAgentProbeResult(SshAgentProbeState.Unavailable);
                        state = SshAgentStartupState.Completed;
                        cancellation.Dispose(); cancellation = null;
                        reservation.TrySetResult(idle); checkedAgent.TrySetResult(false);
                    }
                }
                return work;
            }
        }
        private void Begin()
        {
            lock (gate) {
                if (closed || started || reservation.Task.IsCompleted) return;
                started = true; state = SshAgentStartupState.Checking;
                var source = cancellation; var token = source.Token;
                try {
                    // Neither SCM nor SafeLog's disk I/O runs on the UI owner.
                    reservation.TrySetResult(Task.Run(() => Run(source, token)));
                } catch (Exception) {
                    result = new SshAgentProbeResult(SshAgentProbeState.Unavailable);
                    state = SshAgentStartupState.Completed;
                    source.Dispose(); cancellation = null; reservation.TrySetResult(idle); checkedAgent.TrySetResult(false);
                }
            }
        }
        private bool Publish(SshAgentProbeResult value, CancellationToken token)
        {
            lock (gate) {
                if (closed || token.IsCancellationRequested) return false;
                result = value; state = SshAgentStartupState.Completed; checkedAgent.TrySetResult(true); return true;
            }
        }
        private void Report(SshAgentProbeResult value, CancellationToken token)
        {
            lock (gate) { if (closed || token.IsCancellationRequested) return; }
            // A log already inside disk I/O may finish after Close. It describes
            // an observed result, never publishes UI or starts another operation.
            try { if (report != null) report("SSH-agent startup result: " + value.ToDisplay()); }
            catch (Exception) { } // A failed log must not replace the SCM result.
        }
        private async Task Run(CancellationTokenSource source, CancellationToken token)
        {
            try {
                token.ThrowIfCancellationRequested();
                var value = await probe.StartAsync(timeoutMilliseconds, token).ConfigureAwait(false);
                if (Publish(value, token)) Report(value, token);
            } catch (OperationCanceledException) {
                lock (gate) { if (result == null) state = SshAgentStartupState.Cancelled; }
            } catch (Exception) {
                var value = new SshAgentProbeResult(SshAgentProbeState.Unavailable);
                if (Publish(value, token)) Report(value, token);
            } finally {
                lock (gate) { checkedAgent.TrySetResult(false); if (ReferenceEquals(cancellation, source)) cancellation = null; source.Dispose(); }
            }
        }
        internal void Close()
        {
            lock (gate) {
                if (closed) return;
                closed = true; checkedAgent.TrySetResult(false);
                if (result == null) state = SshAgentStartupState.Cancelled;
                if (cancellation != null) {
                    try { cancellation.Cancel(); } catch (AggregateException) { }
                    if (!started) { cancellation.Dispose(); cancellation = null; }
                }
                if (!started && reservation != null) reservation.TrySetResult(idle);
            }
        }
    }
}

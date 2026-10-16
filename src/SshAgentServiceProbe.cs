using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ProGo
{
    internal enum SshAgentProbeState { Completed, Busy, TimedOut, Unavailable }

    internal sealed class SshAgentProbeResult
    {
        internal readonly SshAgentProbeState State;
        internal readonly SshAgentServiceSnapshot Snapshot;
        internal SshAgentProbeResult(SshAgentProbeState state, SshAgentServiceSnapshot snapshot = null)
        { State = state; Snapshot = snapshot; }
        internal string ToDisplay()
        {
            if (State == SshAgentProbeState.Completed) return Snapshot.ToDisplay();
            if (State == SshAgentProbeState.Busy)
                return "Предыдущая проверка службы ssh-agent ещё выполняется. Дождитесь её завершения и повторите проверку.";
            if (State == SshAgentProbeState.TimedOut)
                return "Время ожидания службы ssh-agent истекло. Запрос Windows ещё может выполняться; повторите проверку позже.";
            return "Состояние службы ssh-agent не подтверждено: проверка завершилась с ошибкой. Проверьте OpenSSH и права на чтение службы.";
        }
    }

    // One process-wide read, including native cleanup. Cancelling or timing out a
    // waiter never releases this slot while an OS call is still running.
    internal sealed class SshAgentServiceProbe
    {
        internal const int DefaultTimeoutMilliseconds = 3000;
        internal static readonly SshAgentServiceProbe Shared = new SshAgentServiceProbe(token => SshAgentServiceStatus.Read(token));
        private readonly object gate = new object();
        private readonly Func<CancellationToken, SshAgentServiceSnapshot> inspect;
        private Attempt active;
        private readonly Task idle = Task.FromResult(false);

        private sealed class Attempt : IDisposable
        {
            private readonly object lifetime = new object();
            private readonly CancellationTokenSource source = new CancellationTokenSource();
            private bool disposed;
            internal readonly CancellationToken Token;
            internal Task<SshAgentProbeResult> Work;
            internal Attempt() { Token = source.Token; }
            internal void Cancel()
            {
                lock (lifetime) {
                    if (disposed) return;
                    // Production SCM reads only poll this token between calls.
                    // A failed callback must not replace a caller's timeout/cancel.
                    try { source.Cancel(); } catch (AggregateException) { }
                }
            }
            public void Dispose()
            { lock (lifetime) { if (!disposed) { disposed = true; source.Dispose(); } } }
        }

        internal SshAgentServiceProbe(Func<CancellationToken, SshAgentServiceSnapshot> inspect)
        { if (inspect == null) throw new ArgumentNullException("inspect"); this.inspect = inspect; }
        internal Task PendingWork
        { get { lock (gate) return active == null ? idle : active.Work; } }

        private SshAgentProbeResult Inspect(Attempt attempt)
        {
            try {
                attempt.Token.ThrowIfCancellationRequested();
                var snapshot = inspect(attempt.Token);
                attempt.Token.ThrowIfCancellationRequested();
                return new SshAgentProbeResult(snapshot == null ? SshAgentProbeState.Unavailable : SshAgentProbeState.Completed, snapshot);
            } catch (Exception) {
                // The abandoned worker always settles to data; raw exceptions,
                // native paths and late cancellation cannot become unobserved faults.
                return new SshAgentProbeResult(SshAgentProbeState.Unavailable);
            } finally { attempt.Dispose(); }
        }

        internal async Task<SshAgentProbeResult> ReadAsync(int timeoutMilliseconds, CancellationToken token)
        {
            if (timeoutMilliseconds < 1 || timeoutMilliseconds > 30000) throw new ArgumentOutOfRangeException("timeoutMilliseconds");
            token.ThrowIfCancellationRequested();
            var watch = Stopwatch.StartNew();
            Attempt attempt;
            lock (gate) {
                token.ThrowIfCancellationRequested();
                if (active != null && !active.Work.IsCompleted) return new SshAgentProbeResult(SshAgentProbeState.Busy);
                attempt = new Attempt();
                try {
                    // A slow local SCM call occupies at most one dedicated worker,
                    // never the caller/UI thread or a growing set of pool workers.
                    attempt.Work = Task.Factory.StartNew(() => Inspect(attempt), CancellationToken.None,
                        TaskCreationOptions.LongRunning, TaskScheduler.Default);
                    active = attempt;
                } catch (Exception) { attempt.Dispose(); return new SshAgentProbeResult(SshAgentProbeState.Unavailable); }
            }
            using (var deadline = new CancellationTokenSource()) {
                var cancelled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                using (token.Register(() => cancelled.TrySetResult(true))) {
                    try {
                        int remaining = (int)Math.Max(0, timeoutMilliseconds - watch.ElapsedMilliseconds);
                        var timeout = Task.Delay(remaining, deadline.Token);
                        var finished = await Task.WhenAny(attempt.Work, timeout, cancelled.Task).ConfigureAwait(false);
                        token.ThrowIfCancellationRequested();
                        if (finished != attempt.Work || watch.ElapsedMilliseconds >= timeoutMilliseconds) {
                            attempt.Cancel(); return new SshAgentProbeResult(SshAgentProbeState.TimedOut);
                        }
                        return await attempt.Work.ConfigureAwait(false);
                    } catch (OperationCanceledException) { attempt.Cancel(); throw; }
                    finally { deadline.Cancel(); }
                }
            }
        }
    }
}

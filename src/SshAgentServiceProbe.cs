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
        internal readonly SshAgentServiceStartResult StartAttempt;
        internal SshAgentProbeResult(SshAgentProbeState state, SshAgentServiceSnapshot snapshot = null, SshAgentServiceStartResult startAttempt = null)
        { State = state; Snapshot = snapshot; StartAttempt = startAttempt; }
        internal string ToDisplay()
        {
            if (State == SshAgentProbeState.Completed) return StartAttempt == null ? Snapshot.ToDisplay() : StartAttempt.ToDisplay();
            if (State == SshAgentProbeState.Busy)
                return "Предыдущая проверка службы или другая операция с SSH-agent ещё выполняется. Дождитесь её завершения и повторите проверку.";
            if (State == SshAgentProbeState.TimedOut)
                return "Время ожидания службы ssh-agent истекло. Запрос Windows ещё может выполняться; повторите проверку позже.";
            return "Состояние службы ssh-agent не подтверждено: проверка завершилась с ошибкой. Проверьте OpenSSH и права на чтение службы.";
        }
    }

    // SCM reads/starts, including startup, share the key-operation lease. Cancelling
    // or timing out a waiter cannot release it while native work/cleanup is running.
    internal sealed class SshAgentServiceProbe
    {
        internal const int DefaultTimeoutMilliseconds = 3000;
        internal static readonly SshAgentServiceProbe Shared = new SshAgentServiceProbe(
            token => SshAgentServiceStatus.Read(token), token => SshAgentServiceStart.TryStart(token));
        private readonly object gate = new object();
        private readonly Func<CancellationToken, SshAgentServiceSnapshot> inspect;
        private readonly Func<CancellationToken, SshAgentServiceStartResult> start;
        private Attempt active;
        private readonly Task idle = Task.FromResult(false);

        private sealed class Attempt : IDisposable
        {
            private readonly object lifetime = new object();
            private readonly CancellationTokenSource source;
            private bool disposed;
            internal readonly CancellationToken Token;
            internal Task<SshAgentProbeResult> Work;
            internal Attempt(CancellationToken ownerToken)
            {
                // Link before scheduling: cancellation cannot be missed in the
                // gap between worker launch and the waiter's registration.
                source = CancellationTokenSource.CreateLinkedTokenSource(ownerToken);
                Token = source.Token;
            }
            internal void ArmStartDeadline(int milliseconds)
            { lock (lifetime) { if (!disposed) source.CancelAfter(milliseconds); } }
            internal void Cancel()
            {
                lock (lifetime) {
                    if (disposed) return;
                    // Production SCM operations only poll this token between calls.
                    // A failed callback must not replace a caller's timeout/cancel.
                    try { source.Cancel(); } catch (AggregateException) { }
                }
            }
            public void Dispose()
            { lock (lifetime) { if (!disposed) { disposed = true; source.Dispose(); } } }
        }

        internal SshAgentServiceProbe(Func<CancellationToken, SshAgentServiceSnapshot> inspect,
            Func<CancellationToken, SshAgentServiceStartResult> start = null)
        { if (inspect == null) throw new ArgumentNullException("inspect"); this.inspect = inspect; this.start = start; }
        internal Task PendingWork
        { get { lock (gate) return active == null ? idle : active.Work; } }

        private SshAgentProbeResult Inspect(Attempt attempt, bool starting, SshReadOnlyLease lease)
        {
            try {
                attempt.Token.ThrowIfCancellationRequested();
                var startResult = starting && start != null ? start(attempt.Token) : null;
                var snapshot = starting ? startResult == null ? null : startResult.Snapshot : inspect(attempt.Token);
                attempt.Token.ThrowIfCancellationRequested();
                return new SshAgentProbeResult(snapshot == null ? SshAgentProbeState.Unavailable : SshAgentProbeState.Completed, snapshot, startResult);
            } catch (Exception) {
                // The abandoned worker always settles to data; raw exceptions,
                // native paths and late cancellation cannot become unobserved faults.
                return new SshAgentProbeResult(SshAgentProbeState.Unavailable);
            } finally {
                try { attempt.Dispose(); }
                finally { lease.Dispose(); }
            }
        }

        internal Task<SshAgentProbeResult> ReadAsync(int timeoutMilliseconds, CancellationToken token)
        { return RunAsync(false, timeoutMilliseconds, token); }
        internal Task<SshAgentProbeResult> StartAsync(int timeoutMilliseconds, CancellationToken token)
        { return RunAsync(true, timeoutMilliseconds, token); }

        private async Task<SshAgentProbeResult> RunAsync(bool starting, int timeoutMilliseconds, CancellationToken token)
        {
            if (timeoutMilliseconds < 1 || timeoutMilliseconds > 30000) throw new ArgumentOutOfRangeException("timeoutMilliseconds");
            token.ThrowIfCancellationRequested();
            var watch = Stopwatch.StartNew();
            Attempt attempt;
            lock (gate) {
                token.ThrowIfCancellationRequested();
                if (active != null && !active.Work.IsCompleted) return new SshAgentProbeResult(SshAgentProbeState.Busy);
                // Reserve before scheduling so a key operation cannot enter the
                // gap before the dedicated worker starts. Transfer only to it.
                var lease = SshReadOnlyLease.TryAcquire(token);
                if (lease == null) return new SshAgentProbeResult(SshAgentProbeState.Busy);
                attempt = null;
                try {
                    attempt = new Attempt(token);
                    if (starting) attempt.ArmStartDeadline((int)Math.Max(1, timeoutMilliseconds - watch.ElapsedMilliseconds));
                    // A slow local SCM call occupies at most one dedicated worker,
                    // never the caller/UI thread or a growing set of pool workers.
                    attempt.Work = Task.Factory.StartNew(() => Inspect(attempt, starting, lease), CancellationToken.None,
                        TaskCreationOptions.LongRunning, TaskScheduler.Default);
                    active = attempt;
                } catch (Exception) {
                    try { if (attempt != null) attempt.Dispose(); }
                    finally { lease.Dispose(); }
                    return new SshAgentProbeResult(SshAgentProbeState.Unavailable);
                }
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

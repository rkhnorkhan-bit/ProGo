using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ProGo
{
    // Owns auxiliary work independently of the opening/saving task. Only immutable
    // identity/date metadata crosses this boundary; no session, PIN or dataset is retained.
    internal sealed class VaultDailyRuntime
    {
        private readonly object gate = new object();
        private readonly Func<string> rootProvider;
        private readonly Func<DateTime> clock;
        private readonly Action<CancellationToken> beforeCleanup;
        private readonly long retryTicks;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private readonly List<VaultDailyExpiryInvalidation> pending = new List<VaultDailyExpiryInvalidation>();
        private Task<VaultDailyExpiryResult> work;
        private VaultDailyExpiryResult lastResult;
        private bool running, requested, paused, closed, sealedForMaintenance, draining;
        private bool unresolvedSweep;
        private long retryAfter;
        private DateTime? observedDay;
        private Guid knownGeneration;
        private DateTime knownDay;
        private long registrationVersion, epoch;
        // The application has one raw opening at a time. Retaining the first two
        // different dates since its capture is sufficient for any eventual lease day.
        private long openingEpoch;
        private bool openingCaptured;
        private DateTime? firstOpeningDay, secondOpeningDay;

        internal VaultDailyRuntime(Func<string> rootProvider, Func<DateTime> clock,
            Action<CancellationToken> beforeCleanup = null, int retryMilliseconds = 30000)
        {
            if (rootProvider == null || clock == null) throw new ArgumentNullException();
            if (retryMilliseconds < 1 || retryMilliseconds > 3600000) throw new ArgumentOutOfRangeException("retryMilliseconds");
            this.rootProvider = rootProvider; this.clock = clock; this.beforeCleanup = beforeCleanup;
            retryTicks = Math.Max(1, (long)((double)retryMilliseconds * Stopwatch.Frequency / 1000));
        }
        internal Task Work { get { lock (gate) return work; } }
        internal bool IsPaused { get { lock (gate) return paused; } }
        internal bool IsClosed { get { lock (gate) return closed; } }
        internal bool HasPendingExpiry { get { lock (gate) return pending.Count != 0; } }

        internal void Start()
        { lock (gate) { if (closed) return; requested = true; EnsureRunner(); } }

        internal void CheckRetry()
        {
            lock (gate) {
                if (closed || paused || running || !unresolvedSweep || Stopwatch.GetTimestamp() < retryAfter) return;
                requested = true; EnsureRunner();
            }
        }

        internal void ObserveDate(DateTime now)
        {
            VaultDailyPolicy.RequireLocalTime(now);
            var day = DateTime.SpecifyKind(now.Date, DateTimeKind.Unspecified);
            lock (gate) {
                if (closed || sealedForMaintenance) return;
                bool changed = !observedDay.HasValue || observedDay.Value != day;
                if (changed) { epoch++; RememberOpeningDay(day); requested = true; }
                observedDay = day;
                int pendingBefore = pending.Count;
                if (knownGeneration != Guid.Empty && knownDay != day)
                    AddPending(new VaultDailyExpiryInvalidation(knownGeneration, knownDay, day));
                if (changed || pending.Count != pendingBefore) EnsureRunner();
            }
        }

        internal long CaptureEpoch()
        {
            lock (gate) {
                openingCaptured = true; openingEpoch = epoch;
                firstOpeningDay = secondOpeningDay = null; return openingEpoch;
            }
        }
        internal long BeginOpening(DateTime now)
        {
            // Establish the worker's actual opening day before the fence. A stale
            // owner tick from before this point cannot expire a fresh later-day binding.
            lock (gate) {
                if (closed || sealedForMaintenance) throw new OperationCanceledException();
                ObserveDate(now); return CaptureEpoch();
            }
        }
        internal bool RegisterOpenedLease(VaultDailySessionPersistence lease, long capturedEpoch)
        {
            if (lease == null) throw new ArgumentNullException("lease");
            lock (gate) {
                if (closed || sealedForMaintenance) { lease.Revoke(); return false; }
                RegisterMetadata(lease);
                DateTime? different = null;
                if (openingCaptured && capturedEpoch == openingEpoch) {
                    if (firstOpeningDay.HasValue && firstOpeningDay.Value != lease.LocalDay) different = firstOpeningDay;
                    else if (secondOpeningDay.HasValue && secondOpeningDay.Value != lease.LocalDay) different = secondOpeningDay;
                }
                if (different.HasValue) {
                    lease.ObserveExpiry(different.Value); AddPending(lease.Invalidation);
                }
                bool rejected = capturedEpoch != epoch && (!openingCaptured || capturedEpoch != openingEpoch);
                foreach (var item in pending)
                    if (item.Generation == lease.Generation && item.LocalDay == lease.LocalDay) { rejected = true; break; }
                if (different.HasValue || lease.Invalidation != null) rejected = true;
                if (rejected) lease.Revoke();
                if (pending.Count != 0) EnsureRunner();
                return !rejected;
            }
        }
        internal void RegisterLease(VaultDailySessionPersistence lease)
        {
            if (lease == null) throw new ArgumentNullException("lease");
            lock (gate) {
                if (closed || sealedForMaintenance) { lease.Revoke(); return; }
                RegisterMetadata(lease);
                if (pending.Count != 0) EnsureRunner();
            }
        }
        internal void Invalidate(VaultDailySessionPersistence lease, DateTime observedNow)
        {
            if (lease == null) throw new ArgumentNullException("lease");
            // The lease rejects UTC before publishing a token. A failed clock does
            // not authorize a made-up tombstone day, but this lease still loses UI access.
            try { lease.ObserveExpiry(observedNow); }
            catch (ArgumentException) { }
            finally { lease.Revoke(); }
            lock (gate) {
                if (closed) return;
                AddPending(lease.Invalidation); requested = true; EnsureRunner();
            }
        }

        internal async Task<VaultDailyExpiryResult> PrepareFake(CancellationToken token)
        {
            while (true) {
                Task<VaultDailyExpiryResult> wait;
                lock (gate) {
                    if (closed || token.IsCancellationRequested) return Failed(VaultDailyExpiryStatus.Cancelled);
                    if (paused) return Failed(VaultDailyExpiryStatus.Busy);
                    requested = true; EnsureRunner(); wait = work;
                }
                var cancelled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                using (token.Register(() => cancelled.TrySetResult(true))) {
                    if (await Task.WhenAny(wait, cancelled.Task).ConfigureAwait(false) != wait)
                        return Failed(VaultDailyExpiryStatus.Cancelled);
                }
                VaultDailyExpiryResult result;
                try { result = await wait.ConfigureAwait(false); }
                catch (OperationCanceledException) { return Failed(VaultDailyExpiryStatus.Cancelled); }
                catch (OutOfMemoryException) { throw; }
                catch (Exception) { return Failed(VaultDailyExpiryStatus.FileUnavailable); }
                lock (gate) {
                    if (closed || token.IsCancellationRequested) return Failed(VaultDailyExpiryStatus.Cancelled);
                    if (paused) return Failed(VaultDailyExpiryStatus.Busy);
                    if (result.Status > VaultDailyExpiryStatus.Expired) return result;
                    // A date event may have queued a new runner after the awaited
                    // result settled; fake authentication must await that work as well.
                    if (pending.Count == 0 && !running && !requested) return result;
                }
            }
        }

        internal void Pause() { lock (gate) paused = true; }
        internal Task<VaultDailyExpiryResult> SealAndDrain()
        {
            lock (gate) {
                if (closed) return Task.FromResult(Failed(VaultDailyExpiryStatus.Cancelled));
                // Source opening/saving must have settled before this final barrier.
                // Freeze new owner observations, then drain every accepted obligation.
                paused = true; sealedForMaintenance = true; draining = true;
                requested = true; EnsureRunner(); return work;
            }
        }
        internal void Resume()
        { lock (gate) { if (closed) return; paused = false; sealedForMaintenance = false; draining = false; requested = true; EnsureRunner(); } }
        internal void Close()
        {
            lock (gate) { if (closed) return; closed = true; paused = true; sealedForMaintenance = true; draining = false; }
            // Never synchronously await the raw task during forced owner disposal.
            lifetime.Cancel();
        }

        private void RegisterMetadata(VaultDailySessionPersistence lease)
        {
            knownGeneration = lease.Generation; knownDay = lease.LocalDay; registrationVersion++;
            AddPending(lease.Invalidation);
            lease.SetExpiryObserver(ObserveLeaseInvalidation);
        }
        private void ObserveLeaseInvalidation(VaultDailyExpiryInvalidation item)
        {
            lock (gate) {
                if (closed) return;
                // The lease retains this metadata callback; the runtime retains no
                // lease or dataset. A saving worker may publish B after its UI closed.
                AddPending(item); requested = true; EnsureRunner();
            }
        }
        private void RememberOpeningDay(DateTime day)
        {
            if (!openingCaptured) return;
            if (!firstOpeningDay.HasValue) firstOpeningDay = day;
            else if (firstOpeningDay.Value != day && !secondOpeningDay.HasValue) secondOpeningDay = day;
        }
        private void AddPending(VaultDailyExpiryInvalidation item)
        {
            if (item == null) return;
            foreach (var existing in pending)
                if (existing.Generation == item.Generation && existing.LocalDay == item.LocalDay) return;
            pending.Add(item); // The first observed different date cannot be overwritten by ABA.
        }
        private void EnsureRunner()
        {
            if (closed || (paused && !draining) || running || (!requested && pending.Count == 0)) return;
            running = true;
            var completion = new TaskCompletionSource<VaultDailyExpiryResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            work = completion.Task;
            Task.Run(() => Run(completion));
        }
        private void Run(TaskCompletionSource<VaultDailyExpiryResult> completion)
        {
            VaultDailyExpiryResult result = null;
            try {
                while (true) {
                    VaultDailyExpiryInvalidation invalidation; long version;
                    lock (gate) {
                        if (closed) { Complete(completion, Failed(VaultDailyExpiryStatus.Cancelled)); return; }
                        if (paused && !draining) { Complete(completion, lastResult ?? Failed(VaultDailyExpiryStatus.Busy)); return; }
                        invalidation = pending.Count == 0 ? null : pending[0];
                        requested = false; version = registrationVersion;
                    }
                    lifetime.Token.ThrowIfCancellationRequested();
                    if (beforeCleanup != null) beforeCleanup(lifetime.Token);
                    lifetime.Token.ThrowIfCancellationRequested();
                    result = VaultDailyExpiry.Run(rootProvider(), clock, lifetime.Token, invalidation);
                    lock (gate) {
                        foreach (var observed in result.ObservedInvalidations) AddPending(observed);
                        lastResult = result;
                        if (result.Status > VaultDailyExpiryStatus.Expired || closed) { Complete(completion, result); return; }
                        if (invalidation != null) pending.Remove(invalidation);
                        if (version == registrationVersion) {
                            knownGeneration = result.HasFakeBinding ? result.Stamp.Generation : Guid.Empty;
                            knownDay = result.HasFakeBinding ? result.Stamp.LocalDay : default(DateTime);
                        }
                        if ((paused && !draining) || (pending.Count == 0 && !requested)) { Complete(completion, result); return; }
                    }
                }
            } catch (OperationCanceledException) { result = Failed(VaultDailyExpiryStatus.Cancelled); }
            catch (OutOfMemoryException ex) {
                lock (gate) { running = false; completion.TrySetException(ex); } return;
            } catch (Exception) { result = Failed(VaultDailyExpiryStatus.FileUnavailable); }
            lock (gate) {
                Complete(completion, result);
            }
        }
        private void Complete(TaskCompletionSource<VaultDailyExpiryResult> completion, VaultDailyExpiryResult result)
        {
            lastResult = result; running = false; draining = false;
            unresolvedSweep = !closed && result.Status > VaultDailyExpiryStatus.Expired;
            retryAfter = unresolvedSweep ? Stopwatch.GetTimestamp() + retryTicks : 0;
            if (unresolvedSweep) requested = false;
            completion.TrySetResult(result);
        }
        private static VaultDailyExpiryResult Failed(VaultDailyExpiryStatus status)
        { return VaultDailyExpiryResult.Failed(status); }
    }
}

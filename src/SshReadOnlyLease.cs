using System;
using System.Threading;

namespace ProGo
{
    // One SCM read/start (including startup), inventory, fingerprint phase OR
    // explicit key addition per process, held through actual native/job cleanup.
    // Historical name retained for existing callers. Acquisition never waits;
    // only the operation owner may dispose its lease, not a timed-out waiter.
    internal sealed class SshReadOnlyLease : IDisposable
    {
        private static readonly SemaphoreSlim slot = new SemaphoreSlim(1, 1);
        private int released;
        private SshReadOnlyLease() { }
        internal static SshReadOnlyLease TryAcquire(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return slot.Wait(0, token) ? new SshReadOnlyLease() : null;
        }
        public void Dispose()
        { if (Interlocked.Exchange(ref released, 1) == 0) slot.Release(); }
    }
}

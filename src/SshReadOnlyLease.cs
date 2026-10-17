using System;
using System.Threading;

namespace ProGo
{
    // One inventory, fingerprint phase OR explicit key addition through cleanup.
    // Coordination with SCM/startup remains separate work before UI integration.
    // Historical name retained for the accepted read-only callers. The same lease
    // also serializes explicit key addition through its full console/job cleanup.
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

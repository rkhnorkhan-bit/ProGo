using System;
using System.Threading;

namespace ProGo
{
    // One inventory OR fingerprint phase, through the owned runner's cleanup.
    // SCM and future mutating ssh-add coordination remain separate work.
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

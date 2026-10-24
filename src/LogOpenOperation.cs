using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ProGo
{
    // Publish this operation on its UI owner before Begin. Cancellation never
    // claims that a blocked file operation or an admitted native launch stopped.
    internal sealed class LogOpenOperation
    {
        private readonly string requestedPath;
        private readonly string root;
        private readonly bool directoryOnly;
        private readonly Action prepareDirectories;
        private readonly Action<string> launch;
        private readonly Action<CancellationToken> beforeIo;
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        private readonly CancellationToken token;
        private readonly TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        private int begun, admission;
        internal Task<bool> Work { get { return completion.Task; } }
        internal Exception Error { get; private set; }
        internal bool IsCancellationRequested { get { return token.IsCancellationRequested; } }

        internal LogOpenOperation(string root, string path, Action prepareDirectories, Action<string> launch,
            Action<CancellationToken> beforeIo = null, bool directoryOnly = false)
        {
            this.root = root; requestedPath = path; this.prepareDirectories = prepareDirectories;
            this.launch = launch; this.beforeIo = beforeIo; this.directoryOnly = directoryOnly; token = cancellation.Token;
        }
        internal Task<bool> Begin()
        {
            if (Interlocked.CompareExchange(ref begun, 1, 0) == 0) Task.Run((Action)Run);
            return Work;
        }
        internal void Cancel()
        {
            // Admission is the linearization point. Cancel never waits on the
            // native Process.Start; its owner must await Work before handoff.
            Interlocked.CompareExchange(ref admission, 2, 0);
            try { cancellation.Cancel(); } catch (ObjectDisposedException) { }
        }
        private void Run()
        {
            bool success = false;
            try {
                token.ThrowIfCancellationRequested();
                if (beforeIo != null) beforeIo(token);
                token.ThrowIfCancellationRequested();
                prepareDirectories();
                string path = requestedPath;
                if (!directoryOnly && String.Equals(path, Path.Combine(root, "update.log"), StringComparison.OrdinalIgnoreCase))
                    path = BoundedLog.UpdaterLogPath(root);
                token.ThrowIfCancellationRequested();
                // Existing logs can be read-only. CreateNew prevents a racing
                // writer's newly-created file from ever being truncated. Folder
                // requests never perform a log-file probe/create at their path.
                if (!directoryOnly && !File.Exists(path)) {
                    token.ThrowIfCancellationRequested();
                    try { using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete)) { } }
                    catch (IOException) { if (!File.Exists(path)) throw; }
                }
                token.ThrowIfCancellationRequested();
                if (Interlocked.CompareExchange(ref admission, 1, 0) != 0) return;
                launch(path); success = true;
            }
            catch (OperationCanceledException) { }
            catch (Exception error) { Error = error; }
            finally { cancellation.Dispose(); completion.TrySetResult(success); }
        }
    }
}

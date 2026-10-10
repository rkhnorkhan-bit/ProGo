using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ProGo
{
    internal sealed class RestorePreview
    {
        internal string[] Program, Data, All;
        internal bool HasData { get { return Data != null; } }
        internal string[] Names(string scope)
        { return scope == "Program" ? Program : scope == "Data" ? Data : All; }
        internal static RestorePreview Read(string directory, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var result = new RestorePreview { Program = BackupIntegrity.RestoreNames(directory, "Program", true) };
            token.ThrowIfCancellationRequested();
            try { result.Data = BackupIntegrity.RestoreNames(directory, "Data", true); }
            catch (InvalidDataException) { result.Data = null; }
            token.ThrowIfCancellationRequested();
            if (result.Data != null) result.All = BackupIntegrity.RestoreNames(directory, "All", true);
            token.ThrowIfCancellationRequested(); return result;
        }
    }

    internal sealed class RestoreChoice
    {
        internal string Scope = "Program";
        internal bool ConfirmData;
    }

    internal sealed class RestorePreparedInfo
    {
        internal PreparedBackup Copy;
        internal string Version, Scope;
        internal bool ConfirmData;
        internal string[] Names;
    }

    // One context-owned backup actor may use this session. Workers own all disk
    // work and exact prepared paths; disposal never waits on the UI thread.
    internal sealed class BackupWorkSession : IDisposable
    {
        private readonly object gate = new object();
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        private readonly CancellationToken lifetime;
        private readonly List<PreparedBackup> owned = new List<PreparedBackup>();
        private readonly Action<string, CancellationToken> beforeIo;
        private Task current = Task.FromResult(false), finish;
        private bool ending;
        internal string CleanupError { get; private set; }
        internal CancellationToken Token { get { return lifetime; } }
        internal Task CurrentWork { get { lock (gate) return finish ?? current; } }
        internal BackupWorkSession(Action<string, CancellationToken> beforeIo = null) { this.beforeIo = beforeIo; lifetime = cancellation.Token; }
        internal void Cancel() { try { cancellation.Cancel(); } catch (ObjectDisposedException) { } }

        internal Task<T> Run<T>(string phase, Func<CancellationToken, T> operation)
        {
            lock (gate) {
                if (ending) throw new OperationCanceledException();
                if (!current.IsCompleted) throw new InvalidOperationException("Предыдущая операция с копией ещё не завершена.");
                var work = Task.Run(delegate {
                    using (var deadline = CancellationTokenSource.CreateLinkedTokenSource(lifetime)) {
                        deadline.CancelAfter(120000);
                        var token = deadline.Token; token.ThrowIfCancellationRequested();
                        Probe(phase, token); token.ThrowIfCancellationRequested();
                        // Each operation checks cancellation before its own steps.
                        // Accepted helper acknowledgement is not revoked by a late Cancel.
                        return operation(token);
                    }
                });
                current = work; return work;
            }
        }
        internal void BeforeOwnerCompletion() { Probe("backup-owner-completion", lifetime); }
        private void Probe(string phase, CancellationToken token)
        { if (beforeIo != null) beforeIo(phase, token); }
        internal RestorePreparedInfo Prepare(string directory, string scope, bool confirmData, CancellationToken token)
        {
            var copy = BackupIntegrity.Prepare(directory, token, delegate(string phase, CancellationToken active) {
                Probe(phase, active);
            }, delegate(PreparedBackup value) { lock (gate) owned.Add(value); });
            token.ThrowIfCancellationRequested();
            var names = BackupIntegrity.RestoreNames(copy.Path, scope, confirmData);
            token.ThrowIfCancellationRequested();
            var version = File.ReadAllText(Path.Combine(copy.Path, "VERSION")).Trim();
            token.ThrowIfCancellationRequested();
            return new RestorePreparedInfo { Copy = copy, Names = names, Version = version, Scope = scope, ConfirmData = confirmData };
        }
        internal Task FinishAsync()
        {
            lock (gate) {
                if (finish != null) return finish;
                ending = true; Cancel();
                finish = FinishCore(current); return finish;
            }
        }
        internal void ReleaseCopy(PreparedBackup copy)
        { lock (gate) { if (ending || !owned.Remove(copy)) throw new InvalidOperationException("Подготовленная копия уже передана очистке."); } }
        private async Task FinishCore(Task pending)
        {
            try { await pending.ConfigureAwait(false); } catch (Exception) { }
            await Task.Run(delegate {
                PreparedBackup[] copies; lock (gate) copies = owned.ToArray();
                foreach (var copy in copies) try { Probe("restore-cleanup", CancellationToken.None); copy.Dispose(); }
                    catch (Exception ex) {
                        CleanupError = "Не удалось удалить временную копию подготовки. Она сохранена; подробности — в журнале ProGo.";
                        SafeLog.Error("Owned restore stage cleanup incomplete: " + copy.Path + ".", ex);
                    }
                cancellation.Dispose();
            }).ConfigureAwait(false);
        }
        public void Dispose() { FinishAsync(); }
    }
}

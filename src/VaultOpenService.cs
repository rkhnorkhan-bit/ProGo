using System;
using System.IO;
using System.Threading;

namespace ProGo
{
    internal enum VaultProbeStatus { Ready, CreateRequired, FileUnavailable }
    internal sealed class VaultProbeResult
    {
        internal VaultProbeStatus Status { get; private set; }
        internal string ErrorMessage { get { return Status == VaultProbeStatus.FileUnavailable ? "Не удалось проверить доступность хранилища." : null; } }
        internal VaultProbeResult(VaultProbeStatus status) { Status = status; }
    }
    internal enum VaultOpenStatus
    { Success, InvalidPin, WrongPin, FileUnavailable, InvalidData, UnsupportedFormat, Busy, Conflict, Cancelled, DateChanged }
    internal sealed class VaultOpenResult
    {
        internal VaultOpenStatus Status { get; private set; }
        internal VaultSession Session { get; private set; }
        internal string ErrorMessage {
            get {
                switch (Status) {
                    case VaultOpenStatus.Success: return null;
                    case VaultOpenStatus.InvalidPin: return "PIN-код должен содержать ровно 4 цифры.";
                    case VaultOpenStatus.WrongPin: return "Неверный пин-код";
                    case VaultOpenStatus.InvalidData: return "Формат или данные хранилища повреждены.";
                    case VaultOpenStatus.UnsupportedFormat: return "Формат или параметры хранилища не поддерживаются.";
                    case VaultOpenStatus.Busy: return "Хранилище занято. Повторите операцию.";
                    case VaultOpenStatus.Conflict: return "Хранилище изменилось. Повторите операцию.";
                    case VaultOpenStatus.Cancelled: return "Операция отменена.";
                    case VaultOpenStatus.DateChanged: return "Дата изменилась. Повторите открытие хранилища.";
                    default: return "Не удалось открыть хранилище. Проверьте доступность файла и права доступа.";
                }
            }
        }
        private VaultOpenResult(VaultOpenStatus status, VaultSession session) { Status = status; Session = session; }
        internal static VaultOpenResult Succeeded(VaultSession session)
        { if (session == null) throw new ArgumentNullException("session"); return new VaultOpenResult(VaultOpenStatus.Success, session); }
        internal static VaultOpenResult Failed(VaultOpenStatus status)
        {
            if (status <= VaultOpenStatus.Success || status > VaultOpenStatus.DateChanged) throw new ArgumentException("Invalid opening status.");
            return new VaultOpenResult(status, null);
        }
    }

    // Synchronous worker boundary. No UI, global paths, logs, or implicit directories.
    internal static class VaultOpenService
    {
        internal static VaultProbeResult Probe(string realPath, CancellationToken token)
        {
            try {
                token.ThrowIfCancellationRequested(); var path = CheckedRealPath(realPath);
                try { RequireRegular(path); }
                catch (FileNotFoundException) {
                    token.ThrowIfCancellationRequested();
                    if (path != CheckedRealPath(realPath)) throw new IOException();
                    return new VaultProbeResult(VaultProbeStatus.CreateRequired);
                }
                token.ThrowIfCancellationRequested(); return new VaultProbeResult(VaultProbeStatus.Ready);
            } catch (OutOfMemoryException) { throw; }
            catch (Exception) { return new VaultProbeResult(VaultProbeStatus.FileUnavailable); }
        }
        internal static VaultOpenResult Create(string realPath, string pin, CancellationToken token)
        {
            VaultSession created = null;
            try {
                token.ThrowIfCancellationRequested();
                if (!VaultDailyPolicy.IsValidPin(pin)) return Fail(VaultOpenStatus.InvalidPin);
                var path = CheckedRealPath(realPath);
                var probe = Probe(path, token); token.ThrowIfCancellationRequested();
                if (probe.Status != VaultProbeStatus.CreateRequired) return Fail(VaultOpenStatus.FileUnavailable);
                token.ThrowIfCancellationRequested(); created = VaultService.CreateAtPath(path, pin);
                token.ThrowIfCancellationRequested(); return VaultOpenResult.Succeeded(created);
            } catch (OperationCanceledException) { if (created != null) created.DropSensitive(); return Fail(VaultOpenStatus.Cancelled); }
            catch (OutOfMemoryException) { throw; }
            catch (Exception) { if (created != null) created.DropSensitive(); return Fail(VaultOpenStatus.FileUnavailable); }
        }
        internal static VaultOpenResult Open(string realPath, string root, string pin, Func<DateTime> clock,
            Func<VaultData> freshFactory, CancellationToken token)
        {
            try {
                token.ThrowIfCancellationRequested();
                if (!VaultDailyPolicy.IsValidPin(pin)) return Fail(VaultOpenStatus.InvalidPin);
                var path = CheckedRealPath(realPath); var lastRetry = VaultOpenStatus.Conflict;
                for (int attempt = 0; attempt < 3; attempt++) {
                    token.ThrowIfCancellationRequested(); RequireRegular(path);
                    var real = VaultService.Read(path, pin); token.ThrowIfCancellationRequested();
                    // Correct authentication never depends on the auxiliary store, factory or clock.
                    if (real.Status == VaultReadStatus.Success) {
                        var openedReal = new VaultSession(real.Data, pin, true, null, path);
                        try { token.ThrowIfCancellationRequested(); return VaultOpenResult.Succeeded(openedReal); }
                        catch { openedReal.DropSensitive(); throw; }
                    }
                    if (real.Status != VaultReadStatus.AuthenticationMismatch) return Fail(ReadStatus(real.Status));
                    var now = LocalNow(clock); var recovered = VaultDailyRecovery.Recover(root);
                    token.ThrowIfCancellationRequested();
                    if (recovered.Status > VaultDailyRecoveryStatus.RecoveredNext) return Fail(RecoveryStatus(recovered.Status));
                    var stamp = recovered.Stamp;
                    var state = recovered.Record == null ? VaultDailyState.Empty(now) : recovered.Record.State;
                    if (state.LocalDay != now.Date) {
                        var expired = VaultDailyStore.CompareExchange(root, stamp, VaultDailyState.Empty(now));
                        token.ThrowIfCancellationRequested();
                        if (expired.Status == VaultDailyCommitStatus.Conflict) { lastRetry = VaultOpenStatus.Conflict; continue; }
                        if (expired.Status != VaultDailyCommitStatus.Success) return Fail(CommitStatus(expired.Status));
                        stamp = expired.Stamp; state = expired.Record.State;
                    }
                    if (LocalNow(clock).Date != now.Date) { lastRetry = VaultOpenStatus.DateChanged; continue; }
                    var transition = VaultDailyPolicy.Evaluate(state, now, pin, real, freshFactory);
                    token.ThrowIfCancellationRequested();
                    if (transition.Outcome == VaultDailyOutcome.Rejected)
                        return Fail(transition.ErrorMessage == "Неверный пин-код" ? VaultOpenStatus.WrongPin :
                            transition.ErrorMessage == "Формат или данные хранилища повреждены." ? VaultOpenStatus.InvalidData : VaultOpenStatus.FileUnavailable);
                    if (LocalNow(clock).Date != now.Date) { lastRetry = VaultOpenStatus.DateChanged; continue; }
                    if (transition.RequiresFakeCommit) {
                        var committed = VaultDailyStore.CompareExchange(root, stamp, transition.PendingFakeState);
                        if (committed.Status == VaultDailyCommitStatus.Conflict) { lastRetry = VaultOpenStatus.Conflict; continue; }
                        if (committed.Status != VaultDailyCommitStatus.Success) return Fail(CommitStatus(committed.Status));
                        stamp = committed.Stamp; state = committed.Record.State;
                    }
                    var opened = new VaultSession(state.FakeData, pin, false,
                        new VaultDailySessionPersistence(root, stamp, clock));
                    try {
                        var after = LocalNow(clock);
                        if (after.Date != now.Date) {
                            opened.DropSensitive();
                            var expired = VaultDailyStore.CompareExchange(root, stamp, VaultDailyState.Empty(after));
                            token.ThrowIfCancellationRequested();
                            if (expired.Status != VaultDailyCommitStatus.Success && expired.Status != VaultDailyCommitStatus.Conflict)
                                return Fail(CommitStatus(expired.Status));
                            lastRetry = VaultOpenStatus.DateChanged; continue;
                        }
                        token.ThrowIfCancellationRequested(); return VaultOpenResult.Succeeded(opened);
                    } catch { opened.DropSensitive(); throw; }
                }
                return Fail(lastRetry);
            } catch (OperationCanceledException) { return Fail(VaultOpenStatus.Cancelled); }
            catch (OutOfMemoryException) { throw; }
            catch (Exception) { return Fail(VaultOpenStatus.FileUnavailable); }
        }
        internal static DateTime LocalNow(Func<DateTime> clock)
        { if (clock == null) throw new ArgumentNullException("clock"); var now = clock(); VaultDailyPolicy.RequireLocalTime(now); return now; }
        internal static string CheckedRealPath(string path)
        {
            if (String.IsNullOrEmpty(path)) throw new IOException();
            var name = Path.GetFileName(path);
            if (String.IsNullOrEmpty(name) || name == "." || name == ".." || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new IOException();
            var parent = Path.GetDirectoryName(VaultDailyFile.CheckedPath(Path.GetDirectoryName(path)));
            var full = Path.GetFullPath(path);
            if (!String.Equals(full, Path.Combine(parent, name), StringComparison.OrdinalIgnoreCase)) throw new IOException();
            return full;
        }
        private static void RequireRegular(string path)
        { if ((File.GetAttributes(path) & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0) throw new IOException(); }
        private static VaultOpenResult Fail(VaultOpenStatus status) { return VaultOpenResult.Failed(status); }
        private static VaultOpenStatus ReadStatus(VaultReadStatus status)
        { return status == VaultReadStatus.InvalidData ? VaultOpenStatus.InvalidData : status == VaultReadStatus.UnsupportedFormat ? VaultOpenStatus.UnsupportedFormat : VaultOpenStatus.FileUnavailable; }
        private static VaultOpenStatus RecoveryStatus(VaultDailyRecoveryStatus status)
        {
            switch (status) {
                case VaultDailyRecoveryStatus.Busy: return VaultOpenStatus.Busy;
                case VaultDailyRecoveryStatus.Conflict: return VaultOpenStatus.Conflict;
                case VaultDailyRecoveryStatus.InvalidData: return VaultOpenStatus.InvalidData;
                case VaultDailyRecoveryStatus.UnsupportedFormat: return VaultOpenStatus.UnsupportedFormat;
                default: return VaultOpenStatus.FileUnavailable;
            }
        }
        private static VaultOpenStatus CommitStatus(VaultDailyCommitStatus status)
        {
            switch (status) {
                case VaultDailyCommitStatus.Conflict: return VaultOpenStatus.Conflict;
                case VaultDailyCommitStatus.Busy: return VaultOpenStatus.Busy;
                case VaultDailyCommitStatus.InvalidData: return VaultOpenStatus.InvalidData;
                case VaultDailyCommitStatus.UnsupportedFormat: return VaultOpenStatus.UnsupportedFormat;
                default: return VaultOpenStatus.FileUnavailable;
            }
        }
    }

    // Only a successfully accepted binding can construct this writer lease.
    internal sealed class VaultDailySessionPersistence : IVaultSessionPersistence
    {
        private readonly string root;
        private readonly DateTime day;
        private readonly Func<DateTime> clock;
        private readonly object gate = new object();
        private VaultDailyFileStamp stamp;
        private volatile bool revoked;
        internal VaultDailySessionPersistence(string root, VaultDailyFileStamp accepted, Func<DateTime> clock)
        {
            if (accepted == null || accepted.IsMissing || !accepted.BelongsTo(VaultDailyFile.CheckedPath(root)) || clock == null)
                throw new ArgumentException("Invalid persistence lease.");
            this.root = root; stamp = accepted; day = accepted.LocalDay; this.clock = clock;
        }
        public bool IsCurrent(DateTime localNow)
        { VaultDailyPolicy.RequireLocalTime(localNow); return !revoked && localNow.Date == day; }
        public void Save(VaultData data, string pin)
        {
            try { SaveCore(data, pin); }
            catch (OutOfMemoryException) { throw; }
            catch (Exception) { throw SaveFailure(); }
        }
        private void SaveCore(VaultData data, string pin)
        {
            lock (gate) {
                if (revoked) throw SaveFailure();
                var before = VaultOpenService.LocalNow(clock);
                if (before.Date != day) {
                    revoked = true; VaultDailyStore.CompareExchange(root, stamp, VaultDailyState.Empty(before));
                    throw SaveFailure();
                }
                var committed = VaultDailyStore.CompareExchange(root, stamp, VaultDailyState.Bind(day, pin, data));
                if (committed.Status != VaultDailyCommitStatus.Success) throw SaveFailure();
                stamp = committed.Stamp;
                var after = VaultOpenService.LocalNow(clock);
                if (after.Date != day) {
                    revoked = true;
                    VaultDailyStore.CompareExchange(root, stamp, VaultDailyState.Empty(after));
                    throw SaveFailure();
                }
            }
        }
        private static IOException SaveFailure() { return new IOException("Не удалось сохранить изменения хранилища. Повторите открытие хранилища."); }
    }
}

using System;
using System.Collections.Generic;
using System.Threading;

namespace ProGo
{
    internal sealed class VaultDailyExpiryInvalidation
    {
        internal Guid Generation { get; private set; }
        internal DateTime LocalDay { get; private set; }
        internal DateTime FirstDifferentDay { get; private set; }
        internal VaultDailyExpiryInvalidation(Guid generation, DateTime localDay, DateTime firstDifferentDay)
        {
            VaultDailyPolicy.RequireLocalTime(localDay); VaultDailyPolicy.RequireLocalTime(firstDifferentDay);
            if (generation == Guid.Empty || localDay.Date == firstDifferentDay.Date)
                throw new ArgumentException("Invalid vault expiration observation.");
            Generation = generation;
            LocalDay = DateTime.SpecifyKind(localDay.Date, DateTimeKind.Unspecified);
            FirstDifferentDay = DateTime.SpecifyKind(firstDifferentDay.Date, DateTimeKind.Unspecified);
        }
        internal bool Matches(VaultDailyFileStamp stamp)
        { return stamp != null && !stamp.IsMissing && stamp.Generation == Generation && stamp.LocalDay == LocalDay; }
    }

    internal enum VaultDailyExpiryStatus
    { Unchanged, Missing, Expired, Busy, Conflict, FileUnavailable, InvalidData, UnsupportedFormat, RevisionExhausted, Cancelled, DateChanged }

    internal sealed class VaultDailyExpiryResult
    {
        private readonly VaultDailyExpiryInvalidation[] observations;
        internal VaultDailyExpiryStatus Status { get; private set; }
        internal VaultDailyFileStamp Stamp { get; private set; }
        internal bool HasFakeBinding { get; private set; }
        internal VaultDailyExpiryInvalidation[] ObservedInvalidations
        { get { return (VaultDailyExpiryInvalidation[])observations.Clone(); } }
        internal string ErrorMessage {
            get {
                switch (Status) {
                    case VaultDailyExpiryStatus.Unchanged:
                    case VaultDailyExpiryStatus.Missing:
                    case VaultDailyExpiryStatus.Expired: return null;
                    case VaultDailyExpiryStatus.Busy: return "Хранилище занято. Повторите операцию.";
                    case VaultDailyExpiryStatus.Conflict: return "Хранилище изменилось. Повторите операцию.";
                    case VaultDailyExpiryStatus.InvalidData: return "Формат или данные хранилища повреждены.";
                    case VaultDailyExpiryStatus.UnsupportedFormat: return "Формат или параметры хранилища не поддерживаются.";
                    case VaultDailyExpiryStatus.Cancelled: return "Операция отменена.";
                    case VaultDailyExpiryStatus.DateChanged: return "Дата изменилась. Повторите операцию.";
                    default: return "Не удалось подготовить хранилище. Проверьте доступность файла и права доступа.";
                }
            }
        }
        private VaultDailyExpiryResult(VaultDailyExpiryStatus status, VaultDailyFileStamp stamp, bool bound,
            VaultDailyExpiryInvalidation[] observed)
        {
            Status = status; Stamp = stamp; HasFakeBinding = bound;
            observations = observed == null ? new VaultDailyExpiryInvalidation[0] : (VaultDailyExpiryInvalidation[])observed.Clone();
            foreach (var item in observations) if (item == null) throw new ArgumentException("Invalid vault expiration observations.");
        }
        internal static VaultDailyExpiryResult Safe(VaultDailyFileReadResult read, bool changed,
            VaultDailyExpiryInvalidation[] observed = null)
        {
            if (read == null || read.Stamp == null ||
                (read.Status != VaultDailyFileReadStatus.Success && read.Status != VaultDailyFileReadStatus.Missing))
                throw new ArgumentException("Invalid vault expiration result.");
            bool bound = read.Record != null && read.Record.State.HasFake;
            return new VaultDailyExpiryResult(read.Status == VaultDailyFileReadStatus.Missing ? VaultDailyExpiryStatus.Missing :
                changed && !bound ? VaultDailyExpiryStatus.Expired : VaultDailyExpiryStatus.Unchanged, read.Stamp, bound, observed);
        }
        internal static VaultDailyExpiryResult Failed(VaultDailyExpiryStatus status, VaultDailyExpiryInvalidation[] observed = null)
        {
            if (status <= VaultDailyExpiryStatus.Expired || status > VaultDailyExpiryStatus.DateChanged)
                throw new ArgumentException("Invalid vault expiration failure.");
            return new VaultDailyExpiryResult(status, null, false, observed);
        }
    }

    // Explicit worker boundary: no UI, logs, application paths, factory or dataset in its result.
    internal static class VaultDailyExpiry
    {
        internal static VaultDailyExpiryResult Run(string root, Func<DateTime> clock, CancellationToken token,
            VaultDailyExpiryInvalidation invalidation = null)
        { return RunForTest(root, clock, token, invalidation, null, null); }

        internal static VaultDailyExpiryResult RunForTest(string root, Func<DateTime> clock, CancellationToken token,
            VaultDailyExpiryInvalidation invalidation = null, Action beforeCompareExchange = null, Action beforePublish = null)
        {
            bool changed = false;
            var retry = VaultDailyExpiryStatus.Conflict;
            var observed = invalidation;
            var observations = new List<VaultDailyExpiryInvalidation>();
            if (invalidation != null) observations.Add(invalidation);
            Func<VaultDailyExpiryStatus, VaultDailyExpiryResult> fail = status => VaultDailyExpiryResult.Failed(status, observations.ToArray());
            try {
                token.ThrowIfCancellationRequested();
                for (int attempt = 0; attempt < 3; attempt++) {
                    token.ThrowIfCancellationRequested();
                    var recovery = VaultDailyRecovery.Recover(root); token.ThrowIfCancellationRequested();
                    if (recovery.Status > VaultDailyRecoveryStatus.RecoveredNext) return fail(RecoveryStatus(recovery.Status));
                    var read = VaultDailyFile.Read(root); token.ThrowIfCancellationRequested();
                    if (!SafeRead(read)) return fail(ReadStatus(read.Status));
                    var now = Now(clock); Observe(ref observed, read, now, observations);
                    var confirmed = Now(clock); token.ThrowIfCancellationRequested(); Observe(ref observed, read, confirmed, observations);
                    if (read.Status == VaultDailyFileReadStatus.Missing) {
                        if (now.Date != confirmed.Date) { retry = VaultDailyExpiryStatus.DateChanged; continue; }
                        return VaultDailyExpiryResult.Safe(read, changed, observations.ToArray());
                    }
                    if (!NeedsExpiry(read, confirmed, observed)) {
                        if (now.Date != confirmed.Date) { retry = VaultDailyExpiryStatus.DateChanged; continue; }
                        return VaultDailyExpiryResult.Safe(read, changed, observations.ToArray());
                    }
                    if (beforeCompareExchange != null) beforeCompareExchange();
                    token.ThrowIfCancellationRequested(); now = Now(clock); Observe(ref observed, read, now, observations);
                    confirmed = Now(clock); token.ThrowIfCancellationRequested(); Observe(ref observed, read, confirmed, observations);
                    if (!NeedsExpiry(read, confirmed, observed)) {
                        if (now.Date != confirmed.Date) { retry = VaultDailyExpiryStatus.DateChanged; continue; }
                        return VaultDailyExpiryResult.Safe(read, changed, observations.ToArray());
                    }
                    now = confirmed;
                    var nextDay = observed != null && observed.Matches(read.Stamp) && read.Record.State.HasFake &&
                        now.Date == read.Record.State.LocalDay ? observed.FirstDifferentDay : now.Date;
                    bool movedBeforePublish = false, invalidClock = false;
                    var committed = VaultDailyStore.CompareExchange(root, read.Stamp, VaultDailyState.Empty(nextDay), delegate {
                        if (beforePublish != null) beforePublish();
                        token.ThrowIfCancellationRequested();
                        DateTime publishNow;
                        try { publishNow = Now(clock); }
                        catch (ArgumentException) { invalidClock = true; throw; }
                        Observe(ref observed, read, publishNow, observations);
                        if (publishNow.Date != now.Date) { movedBeforePublish = true; throw new InvalidOperationException("Vault date changed."); }
                    });
                    token.ThrowIfCancellationRequested();
                    if (invalidClock) return fail(VaultDailyExpiryStatus.InvalidData);
                    if (movedBeforePublish) { retry = VaultDailyExpiryStatus.DateChanged; continue; }
                    if (committed.Status == VaultDailyCommitStatus.Conflict) { retry = VaultDailyExpiryStatus.Conflict; continue; }
                    if (committed.Status != VaultDailyCommitStatus.Success) return fail(CommitStatus(committed.Status));
                    changed = true;
                    // Verify the actual current target and endpoint clock, including ABA normalization.
                    read = VaultDailyFile.Read(root); token.ThrowIfCancellationRequested();
                    if (!SafeRead(read)) return fail(ReadStatus(read.Status));
                    now = Now(clock); Observe(ref observed, read, now, observations);
                    confirmed = Now(clock); token.ThrowIfCancellationRequested(); Observe(ref observed, read, confirmed, observations);
                    if (now.Date == confirmed.Date &&
                        (read.Status == VaultDailyFileReadStatus.Missing || !NeedsExpiry(read, confirmed, observed)))
                        return VaultDailyExpiryResult.Safe(read, changed, observations.ToArray());
                    retry = VaultDailyExpiryStatus.DateChanged;
                }
                return fail(retry);
            } catch (OperationCanceledException) { return fail(VaultDailyExpiryStatus.Cancelled); }
            catch (OutOfMemoryException) { throw; }
            catch (ArgumentException) { return fail(VaultDailyExpiryStatus.InvalidData); }
            catch (Exception) { return fail(VaultDailyExpiryStatus.FileUnavailable); }
        }
        private static DateTime Now(Func<DateTime> clock)
        { if (clock == null) throw new ArgumentNullException("clock"); var now = clock(); VaultDailyPolicy.RequireLocalTime(now); return now; }
        private static bool SafeRead(VaultDailyFileReadResult read)
        { return read.Status == VaultDailyFileReadStatus.Success || read.Status == VaultDailyFileReadStatus.Missing; }
        private static bool NeedsExpiry(VaultDailyFileReadResult read, DateTime now, VaultDailyExpiryInvalidation observed)
        { return read.Record.State.LocalDay != now.Date || (read.Record.State.HasFake && observed != null && observed.Matches(read.Stamp)); }
        private static void Observe(ref VaultDailyExpiryInvalidation observed, VaultDailyFileReadResult read, DateTime now,
            List<VaultDailyExpiryInvalidation> observations)
        {
            if (read.Record == null || !read.Record.State.HasFake) return;
            foreach (var prior in observations) if (prior.Matches(read.Stamp)) { observed = prior; return; }
            observed = null;
            if (read.Record.State.LocalDay != now.Date) {
                observed = new VaultDailyExpiryInvalidation(read.Stamp.Generation, read.Stamp.LocalDay, now);
                observations.Add(observed);
            }
        }
        private static VaultDailyExpiryStatus ReadStatus(VaultDailyFileReadStatus status)
        { return status == VaultDailyFileReadStatus.InvalidData ? VaultDailyExpiryStatus.InvalidData :
            status == VaultDailyFileReadStatus.UnsupportedFormat ? VaultDailyExpiryStatus.UnsupportedFormat : VaultDailyExpiryStatus.FileUnavailable; }
        private static VaultDailyExpiryStatus RecoveryStatus(VaultDailyRecoveryStatus status)
        { return status == VaultDailyRecoveryStatus.Busy ? VaultDailyExpiryStatus.Busy :
            status == VaultDailyRecoveryStatus.Conflict ? VaultDailyExpiryStatus.Conflict :
            status == VaultDailyRecoveryStatus.InvalidData ? VaultDailyExpiryStatus.InvalidData :
            status == VaultDailyRecoveryStatus.UnsupportedFormat ? VaultDailyExpiryStatus.UnsupportedFormat : VaultDailyExpiryStatus.FileUnavailable; }
        private static VaultDailyExpiryStatus CommitStatus(VaultDailyCommitStatus status)
        { return status == VaultDailyCommitStatus.Conflict ? VaultDailyExpiryStatus.Conflict :
            status == VaultDailyCommitStatus.Busy ? VaultDailyExpiryStatus.Busy :
            status == VaultDailyCommitStatus.InvalidData ? VaultDailyExpiryStatus.InvalidData :
            status == VaultDailyCommitStatus.UnsupportedFormat ? VaultDailyExpiryStatus.UnsupportedFormat :
            status == VaultDailyCommitStatus.RevisionExhausted ? VaultDailyExpiryStatus.RevisionExhausted : VaultDailyExpiryStatus.FileUnavailable; }
    }
}

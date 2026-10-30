using System;
using System.IO;
using System.Threading;

namespace ProGo
{
    internal enum VaultDailyRecoveryStatus
    { NoRecovery, RecoveredPrevious, RecoveredNext, Busy, FileUnavailable, InvalidData, UnsupportedFormat, Conflict }
    internal enum VaultDailyRecoveryPhase { BeforeCleanup, BeforeJournalRemoval }

    internal sealed class VaultDailyRecoveryResult
    {
        internal VaultDailyRecoveryStatus Status { get; private set; }
        internal VaultDailyRecord Record { get; private set; }
        internal VaultDailyFileStamp Stamp { get; private set; }
        internal string ErrorMessage { get { return Status <= VaultDailyRecoveryStatus.RecoveredNext ? null :
            Status == VaultDailyRecoveryStatus.Busy ? "Суточное хранилище занято. Повторите операцию." :
            Status == VaultDailyRecoveryStatus.UnsupportedFormat ? "Формат восстановления суточного хранилища не поддерживается." :
            Status == VaultDailyRecoveryStatus.InvalidData ? VaultDailyJournalCodec.InvalidMessage :
            "Не удалось восстановить суточное хранилище."; } }
        private VaultDailyRecoveryResult(VaultDailyRecoveryStatus status, VaultDailyRecord record, VaultDailyFileStamp stamp)
        { Status = status; Record = record; Stamp = stamp; }
        internal static VaultDailyRecoveryResult Safe(VaultDailyRecoveryStatus status, VaultDailyFileReadResult read)
        {
            if (status < VaultDailyRecoveryStatus.NoRecovery || status > VaultDailyRecoveryStatus.RecoveredNext || read == null || read.Stamp == null ||
                (read.Status != VaultDailyFileReadStatus.Success &&
                    (status != VaultDailyRecoveryStatus.NoRecovery || read.Status != VaultDailyFileReadStatus.Missing)))
                throw new ArgumentException("Invalid safe recovery result.");
            return new VaultDailyRecoveryResult(status, read.Record, read.Stamp);
        }
        internal static VaultDailyRecoveryResult Failed(VaultDailyRecoveryStatus status)
        {
            if (status <= VaultDailyRecoveryStatus.RecoveredNext || status > VaultDailyRecoveryStatus.Conflict)
                throw new ArgumentException("Invalid recovery failure.");
            return new VaultDailyRecoveryResult(status, null, null);
        }
    }

    // Explicit worker-thread recovery. No clock, adoption, UI or directory enumeration.
    // The shared gate coordinates ProGo; this is not a hostile same-user filesystem sandbox.
    internal static class VaultDailyRecovery
    {
        private enum Image { Missing, Previous, Next, Reserved }
        private sealed class Failure : Exception
        {
            internal readonly VaultDailyRecoveryStatus Status;
            internal Failure(VaultDailyRecoveryStatus status) { Status = status; }
        }
        internal static VaultDailyRecoveryResult Recover(string root)
        { return RecoverForTest(root, null); }
        internal static VaultDailyRecoveryResult RecoverForTest(string root, Action<VaultDailyRecoveryPhase> checkpoint)
        {
            Mutex gate = null; bool owned = false; byte[] evidence = null;
            try {
                string path = VaultDailyFile.CheckedPath(root);
                gate = VaultDailyStore.CreateGate();
                try { owned = gate.WaitOne(1000); }
                catch (AbandonedMutexException) { owned = true; }
                if (!owned) return Failed(VaultDailyRecoveryStatus.Busy);
                CheckScope(root, path);
                string marker = path + ".recovery";
                if (Absent(marker)) return Safe(VaultDailyRecoveryStatus.NoRecovery, VaultDailyFile.Read(root));
                evidence = ReadEvidence(marker);
                var decoded = VaultDailyJournalCodec.Decode(evidence, path);
                if (decoded.Status != VaultDailyJournalDecodeStatus.Success)
                    return Failed(decoded.Status == VaultDailyJournalDecodeStatus.UnsupportedFormat ?
                        VaultDailyRecoveryStatus.UnsupportedFormat : VaultDailyRecoveryStatus.InvalidData);
                var journal = decoded.Journal;
                var target = Classify(root, VaultDailyFile.FileName, journal, true, true, false);
                Classify(root, journal.StageFileName, journal, false, true, false);
                var backup = Classify(root, journal.BackupFileName, journal, true, false, !journal.Previous.IsMissing);
                if (journal.Previous.IsMissing && backup != Image.Missing) throw Fail(VaultDailyRecoveryStatus.Conflict);
                Image chosen;
                if (target == Image.Next) chosen = Image.Next;
                else if (target == Image.Previous) chosen = Image.Previous;
                else if (backup == Image.Previous) {
                    CheckEvidence(root, path, marker, evidence);
                    if (Classify(root, VaultDailyFile.FileName, journal, true, true, false) != Image.Missing ||
                        Classify(root, journal.BackupFileName, journal, true, false, false) != Image.Previous)
                        throw Fail(VaultDailyRecoveryStatus.Conflict);
                    File.Move(Child(path, journal.BackupFileName), path); chosen = Image.Previous;
                } else if (Classify(root, journal.StageFileName, journal, false, true, false) == Image.Next) {
                    CheckEvidence(root, path, marker, evidence);
                    if (Classify(root, VaultDailyFile.FileName, journal, true, true, false) != Image.Missing)
                        throw Fail(VaultDailyRecoveryStatus.Conflict);
                    File.Move(Child(path, journal.StageFileName), path); chosen = Image.Next;
                } else throw Fail(VaultDailyRecoveryStatus.Conflict);

                if (checkpoint != null) checkpoint(VaultDailyRecoveryPhase.BeforeCleanup);
                VerifyTarget(root, journal, chosen);
                CheckEvidence(root, path, marker, evidence);
                // Complete preflight again after the hook, before the first cleanup mutation.
                Classify(root, journal.StageFileName, journal, false, true, false);
                backup = Classify(root, journal.BackupFileName, journal, true, false, !journal.Previous.IsMissing);
                if (journal.Previous.IsMissing && backup != Image.Missing) throw Fail(VaultDailyRecoveryStatus.Conflict);
                DeleteVerified(root, path, journal.StageFileName, journal, false, true, false);
                DeleteVerified(root, path, journal.BackupFileName, journal, true, false, !journal.Previous.IsMissing);
                if (checkpoint != null) checkpoint(VaultDailyRecoveryPhase.BeforeJournalRemoval);
                VerifyTarget(root, journal, chosen);
                if (Classify(root, journal.StageFileName, journal, false, true, false) != Image.Missing ||
                    Classify(root, journal.BackupFileName, journal, true, false, !journal.Previous.IsMissing) != Image.Missing)
                    throw Fail(VaultDailyRecoveryStatus.Conflict);
                CheckEvidence(root, path, marker, evidence);
                File.Delete(marker); // Last removal; a retry can finish any prior partial cleanup.
                var final = VaultDailyFile.Read(root);
                RequireRead(final);
                if (final.Status != VaultDailyFileReadStatus.Success || !Wanted(journal, chosen).Matches(final.Stamp))
                    throw Fail(VaultDailyRecoveryStatus.Conflict);
                return VaultDailyRecoveryResult.Safe(chosen == Image.Next ? VaultDailyRecoveryStatus.RecoveredNext :
                    VaultDailyRecoveryStatus.RecoveredPrevious, final);
            } catch (Failure ex) { return Failed(ex.Status); }
            catch (OutOfMemoryException) { throw; }
            catch (Exception) { return Failed(VaultDailyRecoveryStatus.FileUnavailable); }
            finally {
                if (evidence != null) Array.Clear(evidence, 0, evidence.Length);
                try { if (owned) gate.ReleaseMutex(); } finally { if (gate != null) gate.Dispose(); }
            }
        }
        private static Image Classify(string root, string fileName, VaultDailyJournal journal,
            bool allowPrevious, bool allowNext, bool allowReserved)
        {
            var read = VaultDailyFile.ReadCandidate(root, fileName);
            if (read.Status == VaultDailyFileReadStatus.InvalidData && allowReserved &&
                IsReservedZero(root, journal.DataPath, fileName)) return Image.Reserved;
            RequireRead(read);
            if (read.Status == VaultDailyFileReadStatus.Missing) return Image.Missing;
            if (allowNext && journal.Next.Matches(read.Stamp)) return Image.Next;
            if (allowPrevious && !journal.Previous.IsMissing && journal.Previous.Matches(read.Stamp)) return Image.Previous;
            throw Fail(VaultDailyRecoveryStatus.Conflict);
        }
        private static void VerifyTarget(string root, VaultDailyJournal journal, Image chosen)
        {
            var current = VaultDailyFile.ReadCandidate(root, VaultDailyFile.FileName);
            RequireRead(current);
            if (current.Status != VaultDailyFileReadStatus.Success || !Wanted(journal, chosen).Matches(current.Stamp))
                throw Fail(VaultDailyRecoveryStatus.Conflict);
        }
        private static VaultDailyFileStamp Wanted(VaultDailyJournal journal, Image chosen)
        { return chosen == Image.Next ? journal.Next : journal.Previous; }
        private static void DeleteVerified(string root, string path, string name, VaultDailyJournal journal,
            bool previous, bool next, bool reserved)
        {
            CheckScope(root, path);
            if (Classify(root, name, journal, previous, next, reserved) == Image.Missing) return;
            string child = Child(path, name); Regular(child); File.Delete(child);
        }
        private static bool IsReservedZero(string root, string path, string name)
        {
            CheckScope(root, path); string child = Child(path, name); Regular(child);
            using (var stream = new FileStream(child, FileMode.Open, FileAccess.Read, FileShare.Read))
                return stream.Length == 0 && stream.ReadByte() == -1;
        }
        private static byte[] ReadEvidence(string marker)
        {
            byte[] bytes = null; bool returned = false;
            try {
                Regular(marker);
                using (var stream = new FileStream(marker, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    long length = stream.Length;
                    if (length < 16 || length > VaultDailyJournalCodec.MaxEnvelopeBytes) throw Fail(VaultDailyRecoveryStatus.InvalidData);
                    bytes = new byte[(int)length]; int offset = 0;
                    while (offset < bytes.Length) {
                        int read = stream.Read(bytes, offset, bytes.Length - offset);
                        if (read == 0) throw Fail(VaultDailyRecoveryStatus.InvalidData);
                        offset += read;
                    }
                    if (stream.ReadByte() != -1) throw Fail(VaultDailyRecoveryStatus.InvalidData);
                }
                returned = true; return bytes;
            } finally { if (!returned && bytes != null) Array.Clear(bytes, 0, bytes.Length); }
        }
        private static void CheckEvidence(string root, string path, string marker, byte[] original)
        {
            CheckScope(root, path); byte[] current = null;
            try {
                current = ReadEvidence(marker); int difference = current.Length ^ original.Length;
                if (current.Length == original.Length)
                    for (int i = 0; i < current.Length; i++) difference |= current[i] ^ original[i];
                if (difference != 0) throw Fail(VaultDailyRecoveryStatus.Conflict);
            } finally { if (current != null) Array.Clear(current, 0, current.Length); }
        }
        private static void CheckScope(string root, string path)
        {
            if (!String.Equals(path, VaultDailyFile.CheckedPath(root), StringComparison.OrdinalIgnoreCase) ||
                !Absent(path + ".recovery.previous")) throw Fail(VaultDailyRecoveryStatus.FileUnavailable);
        }
        private static bool Absent(string path)
        { try { File.GetAttributes(path); return false; } catch (FileNotFoundException) { return true; } }
        private static void Regular(string path)
        { if ((File.GetAttributes(path) & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0) throw Fail(VaultDailyRecoveryStatus.FileUnavailable); }
        private static string Child(string path, string name) { return Path.Combine(Path.GetDirectoryName(path), name); }
        private static void RequireRead(VaultDailyFileReadResult read)
        {
            if (read.Status == VaultDailyFileReadStatus.InvalidData) throw Fail(VaultDailyRecoveryStatus.InvalidData);
            if (read.Status == VaultDailyFileReadStatus.UnsupportedFormat) throw Fail(VaultDailyRecoveryStatus.UnsupportedFormat);
            if (read.Status == VaultDailyFileReadStatus.FileUnavailable) throw Fail(VaultDailyRecoveryStatus.FileUnavailable);
        }
        private static VaultDailyRecoveryResult Safe(VaultDailyRecoveryStatus status, VaultDailyFileReadResult read)
        { RequireRead(read); return VaultDailyRecoveryResult.Safe(status, read); }
        private static VaultDailyRecoveryResult Failed(VaultDailyRecoveryStatus status) { return VaultDailyRecoveryResult.Failed(status); }
        private static Failure Fail(VaultDailyRecoveryStatus status) { return new Failure(status); }
    }
}

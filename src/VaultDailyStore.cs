using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;

namespace ProGo
{
    internal enum VaultDailyCommitStatus
    { Success, Conflict, Busy, FileUnavailable, InvalidData, UnsupportedFormat, RevisionExhausted }

    internal sealed class VaultDailyCommitResult
    {
        internal VaultDailyCommitStatus Status { get; private set; }
        internal VaultDailyRecord Record { get; private set; }
        internal VaultDailyFileStamp Stamp { get; private set; }
        internal string ErrorMessage {
            get {
                switch (Status) {
                    case VaultDailyCommitStatus.Success: return null;
                    case VaultDailyCommitStatus.Conflict: return "Суточное хранилище изменилось. Повторите операцию.";
                    case VaultDailyCommitStatus.Busy: return "Суточное хранилище занято. Повторите операцию.";
                    case VaultDailyCommitStatus.UnsupportedFormat: return "Формат суточного хранилища не поддерживается.";
                    case VaultDailyCommitStatus.RevisionExhausted: return "Не удалось продолжить запись суточного хранилища.";
                    case VaultDailyCommitStatus.InvalidData: return VaultDailyRecordCodec.InvalidMessage;
                    default: return "Не удалось сохранить суточное хранилище.";
                }
            }
        }
        private VaultDailyCommitResult(VaultDailyCommitStatus status, VaultDailyRecord record, VaultDailyFileStamp stamp)
        { Status = status; Record = record; Stamp = stamp; }
        internal static VaultDailyCommitResult Failed(VaultDailyCommitStatus status)
        {
            if (status <= VaultDailyCommitStatus.Success || status > VaultDailyCommitStatus.RevisionExhausted)
                throw new ArgumentException("Invalid commit status.");
            return new VaultDailyCommitResult(status, null, null);
        }
        internal static VaultDailyCommitResult Committed(VaultDailyFileReadResult accepted)
        {
            if (accepted == null || accepted.Status != VaultDailyFileReadStatus.Success || accepted.Stamp == null)
                throw new ArgumentException("Invalid committed record.");
            return new VaultDailyCommitResult(VaultDailyCommitStatus.Success, accepted.Record, accepted.Stamp);
        }
    }

    // Worker-thread seam only. The caller must not mutate nextState's dataset during this call.
    // The mutex coordinates ProGo writers; it does not prevent a user from restoring older files.
    internal static class VaultDailyStore
    {
        internal static VaultDailyCommitResult CompareExchange(string root, VaultDailyFileStamp expected, VaultDailyState nextState)
        { return CompareExchange(root, expected, nextState, null); }

        // Narrow deterministic test hook: after closing staging, before native publication.
        internal static VaultDailyCommitResult CompareExchange(string root, VaultDailyFileStamp expected,
            VaultDailyState nextState, Action beforePublish)
        {
            Mutex gate = null; bool owned = false, ownStaging = false, ownWitness = false, ownBackup = false;
            string path = null, staging = null, witness = null, backup = null; byte[] envelope = null;
            try {
                if (expected == null || nextState == null) return Failed(VaultDailyCommitStatus.InvalidData);
                path = VaultDailyFile.CheckedPath(root);
                if (!expected.BelongsTo(path)) return Failed(VaultDailyCommitStatus.Conflict);
                gate = CreateGate();
                try { owned = gate.WaitOne(1000); }
                catch (AbandonedMutexException) { owned = true; }
                if (!owned) return Failed(VaultDailyCommitStatus.Busy);

                var current = VaultDailyFile.Read(root);
                if (current.Status != VaultDailyFileReadStatus.Success && current.Status != VaultDailyFileReadStatus.Missing)
                    return Failed(ReadFailure(current.Status));
                if (!expected.Matches(current.Stamp)) return Failed(VaultDailyCommitStatus.Conflict);

                Guid generation = Guid.NewGuid(); long revision = 0;
                if (current.Status == VaultDailyFileReadStatus.Success) {
                    var previous = current.Record;
                    if (previous.State.LocalDay == nextState.LocalDay) {
                        if (previous.State.HasFake && (!nextState.HasFake ||
                            !String.Equals(previous.State.FakePin, nextState.FakePin, StringComparison.Ordinal)))
                            return Failed(VaultDailyCommitStatus.InvalidData);
                        if (previous.Revision == Int64.MaxValue) return Failed(VaultDailyCommitStatus.RevisionExhausted);
                        generation = previous.Generation; revision = checked(previous.Revision + 1);
                    } else {
                        if (nextState.HasFake) return Failed(VaultDailyCommitStatus.InvalidData);
                        while (generation == previous.Generation || generation == Guid.Empty) generation = Guid.NewGuid();
                    }
                }

                VaultDailyDecodeResult snapshot;
                try {
                    envelope = VaultDailyRecordCodec.Encode(new VaultDailyRecord(nextState, generation, revision));
                    snapshot = VaultDailyRecordCodec.Decode(envelope);
                } catch (InvalidDataException) { return Failed(VaultDailyCommitStatus.InvalidData); }
                if (snapshot.Status != VaultDailyDecodeStatus.Success)
                    return Failed(snapshot.Status == VaultDailyDecodeStatus.UnsupportedFormat ?
                        VaultDailyCommitStatus.UnsupportedFormat : VaultDailyCommitStatus.InvalidData);
                // Prepare the detached result and its immutable digest before changing the file.
                var accepted = VaultDailyCommitResult.Committed(VaultDailyFileReadResult.Loaded(path, snapshot.Record, envelope));
                staging = path + ".new-" + Guid.NewGuid().ToString("N");
                using (var stream = new FileStream(staging, FileMode.CreateNew, FileAccess.Write,
                    FileShare.None, 4096, FileOptions.WriteThrough)) {
                    ownStaging = true;
                    stream.Write(envelope, 0, envelope.Length); stream.Flush(true);
                }
                if (beforePublish != null) beforePublish();
                if (!String.Equals(path, VaultDailyFile.CheckedPath(root), StringComparison.OrdinalIgnoreCase))
                    return Failed(VaultDailyCommitStatus.FileUnavailable);
                if (!TargetIsRegular(path, current.Status == VaultDailyFileReadStatus.Missing))
                    return Failed(VaultDailyCommitStatus.FileUnavailable);
                var finalRead = VaultDailyFile.Read(root);
                if (finalRead.Status != VaultDailyFileReadStatus.Success && finalRead.Status != VaultDailyFileReadStatus.Missing)
                    return Failed(ReadFailure(finalRead.Status));
                if (!expected.Matches(finalRead.Stamp)) return Failed(VaultDailyCommitStatus.Conflict);
                // An ambiguous native publication failure must never look like a new Missing day.
                witness = Path.Combine(Path.GetDirectoryName(path), VaultDailyFile.RecoveryFileName);
                using (var marker = new FileStream(witness, FileMode.CreateNew, FileAccess.Write,
                    FileShare.None, 4096, FileOptions.WriteThrough)) { ownWitness = true; marker.Flush(true); }
                if (current.Status == VaultDailyFileReadStatus.Success) {
                    backup = Path.Combine(Path.GetDirectoryName(path), VaultDailyFile.RecoveryBackupFileName);
                    using (var reserved = new FileStream(backup, FileMode.CreateNew, FileAccess.Write,
                        FileShare.None, 4096, FileOptions.WriteThrough)) { ownBackup = true; reserved.Flush(true); }
                }
                if (!String.Equals(path, VaultDailyFile.CheckedPath(root), StringComparison.OrdinalIgnoreCase) ||
                    !TargetIsRegular(path, current.Status == VaultDailyFileReadStatus.Missing) || !TargetIsRegular(witness, false) ||
                    (ownBackup && !TargetIsRegular(backup, false)))
                    return Failed(VaultDailyCommitStatus.FileUnavailable);
                if (current.Status == VaultDailyFileReadStatus.Missing) File.Move(staging, path);
                else File.Replace(staging, path, backup);
                ownStaging = false;
                if (ownBackup) {
                    if (!TargetIsRegular(backup, false)) return Failed(VaultDailyCommitStatus.FileUnavailable);
                    File.Delete(backup); ownBackup = false;
                }
                if (!ownWitness || !TargetIsRegular(witness, false)) return Failed(VaultDailyCommitStatus.FileUnavailable);
                File.Delete(witness); // Only the stable marker created by this call, after full publication.
                ownWitness = false;
                return accepted;
            } catch (OutOfMemoryException) { throw; }
            catch (Exception) { return Failed(VaultDailyCommitStatus.FileUnavailable); }
            finally {
                if (envelope != null) Array.Clear(envelope, 0, envelope.Length);
                try { if (ownStaging) CleanupStaging(root, path, staging); }
                finally { try { if (owned) gate.ReleaseMutex(); } finally { if (gate != null) gate.Dispose(); } }
            }
        }

        private static Mutex CreateGate()
        {
            SecurityIdentifier user;
            using (var identity = WindowsIdentity.GetCurrent()) user = identity.User;
            if (user == null) throw new InvalidOperationException("Windows identity is unavailable.");
            var security = new MutexSecurity(); security.SetAccessRuleProtection(true, false);
            security.AddAccessRule(new MutexAccessRule(user, MutexRights.FullControl, AccessControlType.Allow));
            security.AddAccessRule(new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
                MutexRights.FullControl, AccessControlType.Allow));
            bool created;
            var gate = new Mutex(false, "Global\\ProGo.VaultDaily." + user.Value + ".Writer", out created, security);
            try { if (!created) CheckGateSecurity(gate, user); return gate; }
            catch { gate.Dispose(); throw; }
        }
        private static void CheckGateSecurity(Mutex gate, SecurityIdentifier user)
        {
            var acl = gate.GetAccessControl(); bool hasUser = false, hasSystem = false;
            var system = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
            if (!acl.AreAccessRulesProtected) throw new IOException("Invalid writer gate security.");
            foreach (MutexAccessRule rule in acl.GetAccessRules(true, true, typeof(SecurityIdentifier))) {
                if (rule.IsInherited || rule.AccessControlType != AccessControlType.Allow || rule.MutexRights != MutexRights.FullControl)
                    throw new IOException("Invalid writer gate security.");
                bool isUser = user.Equals(rule.IdentityReference), isSystem = system.Equals(rule.IdentityReference);
                if (!isUser && !isSystem) throw new IOException("Invalid writer gate security.");
                hasUser |= isUser; hasSystem |= isSystem;
            }
            if (!hasUser || !hasSystem) throw new IOException("Invalid writer gate security.");
        }
        private static bool TargetIsRegular(string path, bool mayBeMissing)
        {
            try { return (File.GetAttributes(path) & (FileAttributes.Directory | FileAttributes.ReparsePoint)) == 0; }
            catch (FileNotFoundException) { return mayBeMissing; }
        }
        private static void CleanupStaging(string root, string path, string staging)
        {
            try {
                if (String.Equals(path, VaultDailyFile.CheckedPath(root), StringComparison.OrdinalIgnoreCase) &&
                    TargetIsRegular(staging, false)) File.Delete(staging);
            } catch (Exception) { } // Cleanup must not prevent releasing the owned writer gate.
        }
        private static VaultDailyCommitStatus ReadFailure(VaultDailyFileReadStatus status)
        {
            if (status == VaultDailyFileReadStatus.InvalidData) return VaultDailyCommitStatus.InvalidData;
            if (status == VaultDailyFileReadStatus.UnsupportedFormat) return VaultDailyCommitStatus.UnsupportedFormat;
            return VaultDailyCommitStatus.FileUnavailable;
        }
        private static VaultDailyCommitResult Failed(VaultDailyCommitStatus status)
        { return VaultDailyCommitResult.Failed(status); }
    }
}

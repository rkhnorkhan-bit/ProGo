using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;

namespace ProGo
{
    internal static class AppPaths
    {
        internal static int Accesses;
        public static string VaultPath { get { Accesses++; throw new InvalidOperationException("Vault path guard"); } }
        public static void EnsureDirectories() { Accesses++; throw new InvalidOperationException("Vault path guard"); }
    }
    internal static class SafeLog
    {
        internal static int Accesses;
        public static void Info(string message) { Accesses++; throw new InvalidOperationException("Vault log guard"); }
    }
    internal static class VaultDailyRecoveryTests
    {
        private static int passed;
        private static string phase = "startup";
        private static readonly DateTime Day = new DateTime(2026, 10, 12);
        private static VaultDailyState State(string notes)
        { return VaultDailyState.Bind(Day, "1234", new VaultData { version = 1, entries = new List<VaultEntry> { new VaultEntry { id = "synthetic", notes = notes, secret = "synthetic-secret" } } }); }
        private static void Check(bool condition, string name)
        { if (!condition) { Console.Error.WriteLine("ASSERT: " + name); throw new Exception(); } passed++; Console.WriteLine("PASS: " + name); }
        private sealed class Fixture : IDisposable
        {
            internal readonly string Root = Path.Combine(Path.GetTempPath(), "ProGo-DailyRecovery-" + Guid.NewGuid().ToString("N"));
            internal readonly bool Initial;
            internal readonly byte[] Old, Next;
            internal readonly VaultDailyJournal Journal;
            internal string PathName { get { return Path.Combine(Root, VaultDailyFile.FileName); } }
            internal string Marker { get { return PathName + ".recovery"; } }
            internal string Stage { get { return Path.Combine(Root, Journal.StageFileName); } }
            internal string Backup { get { return Path.Combine(Root, Journal.BackupFileName); } }
            internal Fixture(bool initial = false)
            {
                Directory.CreateDirectory(Root); Initial = initial; var generation = Guid.NewGuid();
                var previous = new VaultDailyRecord(State("previous"), generation, 4);
                var next = new VaultDailyRecord(State("next"), generation, initial ? 0 : 5);
                Old = VaultDailyRecordCodec.Encode(previous); Next = VaultDailyRecordCodec.Encode(next);
                Journal = VaultDailyJournal.Create(PathName, Guid.NewGuid(), initial ? VaultDailyFileReadResult.Absent(PathName).Stamp :
                    VaultDailyFileReadResult.Loaded(PathName, previous, Old).Stamp, VaultDailyFileReadResult.Loaded(PathName, next, Next).Stamp);
            }
            internal void Witness() { var bytes = VaultDailyJournalCodec.Encode(Journal); try { File.WriteAllBytes(Marker, bytes); } finally { Array.Clear(bytes, 0, bytes.Length); } }
            internal void Arrange(bool targetNext = false, bool targetMissing = false, bool stage = true, bool backup = true)
            { if (!targetMissing) File.WriteAllBytes(PathName, targetNext ? Next : Old); if (stage) File.WriteAllBytes(Stage, Next); if (backup) File.WriteAllBytes(Backup, targetNext ? Old : new byte[0]); Witness(); }
            public void Dispose() { Array.Clear(Old, 0, Old.Length); Array.Clear(Next, 0, Next.Length); Directory.Delete(Root, true); }
        }
        private static Dictionary<string, byte[]> Snapshot(Fixture f)
        { return Directory.GetFiles(f.Root).ToDictionary(path => Path.GetFileName(path), path => File.ReadAllBytes(path)); }
        private static void SameFiles(Fixture f, Dictionary<string, byte[]> before, string name)
        { var after = Snapshot(f); Check(before.Count == after.Count && before.All(pair => after.ContainsKey(pair.Key) && pair.Value.SequenceEqual(after[pair.Key])), name); }
        private static void Failure(VaultDailyRecoveryResult result, VaultDailyRecoveryStatus status, string name)
        {
            Check(result.Status == status, name + " status");
            Check(result.Record == null && result.Stamp == null && !String.IsNullOrEmpty(result.ErrorMessage), name + " publishes no record or stamp");
            var expected = status == VaultDailyRecoveryStatus.Busy ? "Суточное хранилище занято. Повторите операцию." :
                status == VaultDailyRecoveryStatus.UnsupportedFormat ? "Формат восстановления суточного хранилища не поддерживается." :
                status == VaultDailyRecoveryStatus.InvalidData ? VaultDailyJournalCodec.InvalidMessage : "Не удалось восстановить суточное хранилище.";
            Check(result.ErrorMessage == expected, name + " static error");
        }
        private static void Reject(Fixture f, VaultDailyRecoveryStatus status, string name)
        { var before = Snapshot(f); Failure(VaultDailyRecovery.Recover(f.Root), status, name); SameFiles(f, before, name + " preserves all files"); }
        private static bool SameAccess(FileSecurity first, FileSecurity second)
        {
            if (first.AreAccessRulesProtected != second.AreAccessRulesProtected || first.AreAccessRulesCanonical != second.AreAccessRulesCanonical) return false;
            var left = first.GetAccessRules(true, true, typeof(SecurityIdentifier)); var right = second.GetAccessRules(true, true, typeof(SecurityIdentifier));
            if (left.Count != right.Count) return false;
            for (int i = 0; i < left.Count; i++) {
                var a = (FileSystemAccessRule)left[i]; var b = (FileSystemAccessRule)right[i];
                if (!a.IdentityReference.Equals(b.IdentityReference) || a.FileSystemRights != b.FileSystemRights ||
                    a.AccessControlType != b.AccessControlType || a.InheritanceFlags != b.InheritanceFlags ||
                    a.PropagationFlags != b.PropagationFlags || a.IsInherited != b.IsInherited) return false;
            }
            return true;
        }
        private static void Recovered(Fixture f, VaultDailyRecoveryStatus status, byte[] image, string name)
        {
            var result = VaultDailyRecovery.Recover(f.Root);
            Check(result.Status == status && result.Record != null && result.Stamp != null && result.ErrorMessage == null, name + " result");
            Check(File.ReadAllBytes(f.PathName).SequenceEqual(image), name + " exact ciphertext");
            var read = VaultDailyFile.Read(f.Root);
            Check(read.Status == VaultDailyFileReadStatus.Success && read.Stamp.Matches(result.Stamp), name + " fresh detached accepted stamp");
            Check(!File.Exists(f.Marker) && !File.Exists(f.Stage) && !File.Exists(f.Backup), name + " removes only known transaction artifacts");
            var again = VaultDailyRecovery.Recover(f.Root);
            Check(again.Status == VaultDailyRecoveryStatus.NoRecovery && again.Stamp.Matches(result.Stamp) && File.ReadAllBytes(f.PathName).SequenceEqual(image), name + " idempotent next recovery");
            result.Record.State.FakeData.entries[0].notes = "caller-edit";
            Check(VaultDailyFile.Read(f.Root).Stamp.Matches(result.Stamp) && read.Record.State.FakeData.entries[0].notes != "caller-edit", name + " result mutation cannot change disk or other read");
        }
        private static void CleanAndKnownStates()
        {
            using (var f = new Fixture(true)) {
                var result = VaultDailyRecovery.Recover(f.Root);
                Check(result.Status == VaultDailyRecoveryStatus.NoRecovery && result.Record == null && result.Stamp.IsMissing && Directory.GetFiles(f.Root).Length == 0, "clean missing root stays missing without creation");
                File.WriteAllBytes(f.PathName, f.Next); string orphan = f.PathName + ".new-" + Guid.NewGuid().ToString("N"); File.WriteAllBytes(orphan, new byte[] { 3, 2, 1 });
                var before = Snapshot(f); result = VaultDailyRecovery.Recover(f.Root);
                Check(result.Status == VaultDailyRecoveryStatus.NoRecovery && result.Record != null && result.Stamp.Matches(f.Journal.Next), "clean existing target is safe");
                SameFiles(f, before, "unknown pre-journal staging remains untouched");
            }
            using (var f = new Fixture()) { f.Arrange(); Recovered(f, VaultDailyRecoveryStatus.RecoveredPrevious, f.Old, "old intact aborts pending update"); }
            using (var f = new Fixture()) { f.Arrange(stage: false); Recovered(f, VaultDailyRecoveryStatus.RecoveredPrevious, f.Old, "old intact tolerates missing new-stage"); }
            using (var f = new Fixture()) { f.Arrange(targetNext: true, stage: false); Recovered(f, VaultDailyRecoveryStatus.RecoveredNext, f.Next, "published new image keeps revision"); }
            using (var f = new Fixture()) { f.Arrange(targetNext: true); File.WriteAllBytes(f.Backup, new byte[0]); Recovered(f, VaultDailyRecoveryStatus.RecoveredNext, f.Next, "new target accepts journal-owned empty reserve"); }
            using (var f = new Fixture()) { f.Arrange(targetMissing: true); File.WriteAllBytes(f.Backup, f.Old); Recovered(f, VaultDailyRecoveryStatus.RecoveredPrevious, f.Old, "missing target restores old backup before staged next"); }
            using (var f = new Fixture()) { f.Arrange(targetMissing: true); Recovered(f, VaultDailyRecoveryStatus.RecoveredNext, f.Next, "missing target and empty old reserve publish verified stage"); }
            using (var f = new Fixture(true)) { f.Arrange(targetMissing: true, backup: false); Recovered(f, VaultDailyRecoveryStatus.RecoveredNext, f.Next, "initial staged first PIN is completed once"); }
            using (var f = new Fixture(true)) { f.Arrange(targetNext: true, stage: false, backup: false); Recovered(f, VaultDailyRecoveryStatus.RecoveredNext, f.Next, "initial already-published first PIN is accepted once"); }
            using (var f = new Fixture()) { f.Arrange(targetMissing: true, stage: false, backup: false); Reject(f, VaultDailyRecoveryStatus.Conflict, "journal with no known image never becomes Missing"); }
            using (var f = new Fixture()) { f.Arrange(targetMissing: true, stage: false); Reject(f, VaultDailyRecoveryStatus.Conflict, "empty reserve cannot restore missing old target without next-stage"); }
            using (var f = new Fixture(true)) { f.Arrange(targetMissing: true); Reject(f, VaultDailyRecoveryStatus.InvalidData, "initial journal rejects even empty backup"); }
        }
        private static void UnknownArtifactsAndJournal()
        {
            foreach (var name in new[] { "target", "stage", "backup" }) using (var f = new Fixture()) {
                f.Arrange(); var bytes = VaultDailyRecordCodec.Encode(new VaultDailyRecord(State("unrecognized"), f.Journal.Next.Generation, f.Journal.Next.Revision));
                File.WriteAllBytes(name == "target" ? f.PathName : name == "stage" ? f.Stage : f.Backup, bytes);
                Reject(f, VaultDailyRecoveryStatus.Conflict, "valid but unknown " + name + " image");
            }
            foreach (var name in new[] { "stage", "backup" }) using (var f = new Fixture()) {
                f.Arrange(); File.WriteAllBytes(name == "stage" ? f.Stage : f.Backup, new byte[] { 1, 2, 3 });
                Reject(f, VaultDailyRecoveryStatus.InvalidData, "corrupt " + name + " blocks all cleanup");
            }
            using (var f = new Fixture()) { f.Arrange(); var bytes = (byte[])f.Next.Clone(); bytes[8] = 2; File.WriteAllBytes(f.Stage, bytes); Reject(f, VaultDailyRecoveryStatus.UnsupportedFormat, "unsupported staged record"); }
            foreach (int length in new[] { 0, 15, VaultDailyJournalCodec.MaxEnvelopeBytes + 1 }) using (var f = new Fixture()) {
                f.Arrange(); File.WriteAllBytes(f.Marker, new byte[length]); Reject(f, VaultDailyRecoveryStatus.InvalidData, "bounded invalid journal length");
            }
            using (var f = new Fixture()) { f.Arrange(); var bytes = File.ReadAllBytes(f.Marker); bytes[8] = 2; File.WriteAllBytes(f.Marker, bytes); Reject(f, VaultDailyRecoveryStatus.UnsupportedFormat, "unsupported journal format"); }
            using (var f = new Fixture()) { f.Arrange(); var bytes = File.ReadAllBytes(f.Marker); bytes[bytes.Length - 1] ^= 1; File.WriteAllBytes(f.Marker, bytes); Reject(f, VaultDailyRecoveryStatus.InvalidData, "native DPAPI journal corruption"); }
            using (var f = new Fixture()) using (var other = new Fixture()) { f.Arrange(); other.Arrange(); File.WriteAllBytes(other.Marker, File.ReadAllBytes(f.Marker)); Reject(other, VaultDailyRecoveryStatus.InvalidData, "journal for foreign root"); }
            using (var f = new Fixture()) { File.WriteAllBytes(f.Backup, f.Old); File.WriteAllBytes(f.PathName + ".recovery.previous", new byte[0]); Reject(f, VaultDailyRecoveryStatus.FileUnavailable, "legacy backup without journal"); }
            using (var f = new Fixture()) { f.Arrange(); File.WriteAllBytes(f.PathName + ".recovery.previous", f.Old); Reject(f, VaultDailyRecoveryStatus.FileUnavailable, "legacy backup blocks valid journal"); }
        }
        private static void RetryAndNativeLocks()
        {
            using (var f = new Fixture()) {
                f.Arrange(); Failure(VaultDailyRecovery.RecoverForTest(f.Root, delegate(VaultDailyRecoveryPhase point) {
                    if (point == VaultDailyRecoveryPhase.BeforeJournalRemoval) throw new IOException("Synthetic checkpoint");
                }), VaultDailyRecoveryStatus.FileUnavailable, "interruption after auxiliary cleanup");
                Check(File.Exists(f.Marker) && !File.Exists(f.Stage) && !File.Exists(f.Backup) && File.ReadAllBytes(f.PathName).SequenceEqual(f.Old), "partial cleanup retains journal and unchanged old target");
                Recovered(f, VaultDailyRecoveryStatus.RecoveredPrevious, f.Old, "partial cleanup retry");
            }
            using (var f = new Fixture()) {
                f.Arrange(); FileStream locked = null; var before = Snapshot(f);
                try {
                    Failure(VaultDailyRecovery.RecoverForTest(f.Root, delegate(VaultDailyRecoveryPhase point) {
                        if (point == VaultDailyRecoveryPhase.BeforeCleanup) locked = new FileStream(f.Stage, FileMode.Open, FileAccess.Read, FileShare.Read);
                    }), VaultDailyRecoveryStatus.FileUnavailable, "real stage delete-sharing lock");
                    SameFiles(f, before, "delete lock preserves journal target and all auxiliary bytes");
                } finally { if (locked != null) locked.Dispose(); }
                Recovered(f, VaultDailyRecoveryStatus.RecoveredPrevious, f.Old, "delete lock release permits retry");
            }
            using (var f = new Fixture()) {
                f.Arrange(); FileStream locked = null;
                try {
                    Failure(VaultDailyRecovery.RecoverForTest(f.Root, delegate(VaultDailyRecoveryPhase point) {
                        if (point == VaultDailyRecoveryPhase.BeforeJournalRemoval) locked = new FileStream(f.Marker, FileMode.Open, FileAccess.Read, FileShare.Read);
                    }), VaultDailyRecoveryStatus.FileUnavailable, "real journal delete-sharing lock");
                    Check(File.Exists(f.Marker) && !File.Exists(f.Stage) && !File.Exists(f.Backup), "journal delete failure preserves final retry witness");
                } finally { if (locked != null) locked.Dispose(); }
                Recovered(f, VaultDailyRecoveryStatus.RecoveredPrevious, f.Old, "journal lock retry");
            }
            using (var f = new Fixture()) {
                f.Arrange(); var bytes = VaultDailyRecordCodec.Encode(new VaultDailyRecord(State("changed-at-hook"), f.Journal.Next.Generation, f.Journal.Next.Revision));
                Failure(VaultDailyRecovery.RecoverForTest(f.Root, delegate(VaultDailyRecoveryPhase point) { if (point == VaultDailyRecoveryPhase.BeforeCleanup) File.WriteAllBytes(f.Backup, bytes); }), VaultDailyRecoveryStatus.Conflict, "changed backup after initial preflight");
                Check(File.ReadAllBytes(f.Stage).SequenceEqual(f.Next) && File.ReadAllBytes(f.Backup).SequenceEqual(bytes) && File.Exists(f.Marker), "second preflight prevents stage deletion before discovering unknown backup");
            }
            using (var f = new Fixture()) {
                f.Arrange(); var changed = VaultDailyJournalCodec.Encode(f.Journal);
                Failure(VaultDailyRecovery.RecoverForTest(f.Root, delegate(VaultDailyRecoveryPhase point) { if (point == VaultDailyRecoveryPhase.BeforeCleanup) File.WriteAllBytes(f.Marker, changed); }), VaultDailyRecoveryStatus.Conflict, "journal bytes changed after initial load");
                Check(File.ReadAllBytes(f.Stage).SequenceEqual(f.Next) && new FileInfo(f.Backup).Length == 0 && File.ReadAllBytes(f.Marker).SequenceEqual(changed), "changed journal is retained before any auxiliary cleanup");
                Recovered(f, VaultDailyRecoveryStatus.RecoveredPrevious, f.Old, "fresh verified journal retry");
            }
            using (var f = new Fixture()) {
                f.Arrange(targetMissing: true); File.WriteAllBytes(f.Backup, f.Old); var before = Snapshot(f);
                using (var locked = new FileStream(f.Backup, FileMode.Open, FileAccess.Read, FileShare.Read))
                    Failure(VaultDailyRecovery.Recover(f.Root), VaultDailyRecoveryStatus.FileUnavailable, "real backup move-sharing lock");
                SameFiles(f, before, "exclusive Move refusal keeps target missing and proofs intact");
                Recovered(f, VaultDailyRecoveryStatus.RecoveredPrevious, f.Old, "backup move lock retry");
            }
            using (var f = new Fixture()) {
                f.Arrange(); var before = Snapshot(f); using (var locked = new FileStream(f.Marker, FileMode.Open, FileAccess.Read, FileShare.None))
                    Failure(VaultDailyRecovery.Recover(f.Root), VaultDailyRecoveryStatus.FileUnavailable, "real journal exclusive read lock");
                SameFiles(f, before, "exclusive journal lock preserves files");
                var acl = File.GetAccessControl(f.Marker, AccessControlSections.Access); string sddl = acl.GetSecurityDescriptorSddlForm(AccessControlSections.Access);
                var denied = File.GetAccessControl(f.Marker, AccessControlSections.Access);
                using (var identity = WindowsIdentity.GetCurrent()) denied.AddAccessRule(new FileSystemAccessRule(identity.User, FileSystemRights.ReadData, AccessControlType.Deny));
                try { File.SetAccessControl(f.Marker, denied); Failure(VaultDailyRecovery.Recover(f.Root), VaultDailyRecoveryStatus.FileUnavailable, "real owner journal read-deny ACL"); }
                finally { var restored = new FileSecurity(); restored.SetSecurityDescriptorSddlForm(sddl, AccessControlSections.Access); File.SetAccessControl(f.Marker, restored); }
                Check(SameAccess(acl, File.GetAccessControl(f.Marker, AccessControlSections.Access)), "journal ACL ordered permissions and protection restored exactly");
                SameFiles(f, before, "read-deny ACL restoration preserves all bytes");
                Recovered(f, VaultDailyRecoveryStatus.RecoveredPrevious, f.Old, "restored ACL permits recovery");
            }
        }
        private static void WriterCheckpoints()
        {
            foreach (bool initial in new[] { false, true }) foreach (VaultDailyWritePhase stop in Enum.GetValues(typeof(VaultDailyWritePhase))) using (var f = new Fixture(initial)) {
                if (!initial) File.WriteAllBytes(f.PathName, f.Old); var before = VaultDailyFile.Read(f.Root);
                var result = VaultDailyStore.CompareExchangeForTest(f.Root, before.Stamp, State("next"), delegate(VaultDailyWritePhase point) { if (point == stop) throw new IOException("Synthetic writer checkpoint"); });
                Check(result.Status == VaultDailyCommitStatus.FileUnavailable && result.Record == null && result.Stamp == null, "writer checkpoint returns no false accepted state");
                Check(VaultDailyFile.Read(f.Root).Status == VaultDailyFileReadStatus.FileUnavailable, "writer checkpoint requires explicit recovery");
                var journal = VaultDailyJournalCodec.Decode(File.ReadAllBytes(f.Marker), f.PathName).Journal;
                bool previous = !initial && stop == VaultDailyWritePhase.JournalPublished;
                var recovered = VaultDailyRecovery.Recover(f.Root);
                Check(recovered.Status == (previous ? VaultDailyRecoveryStatus.RecoveredPrevious : VaultDailyRecoveryStatus.RecoveredNext) && recovered.Stamp.Matches(previous ? journal.Previous : journal.Next), "writer phase recovery chooses exact declared image");
                Check(recovered.Record.State.FakePin == "1234" && recovered.Record.State.FakeData.entries[0].notes == (previous ? "previous" : "next"), "writer phase recovery preserves one PIN and complete dataset");
                Check(VaultDailyRecovery.Recover(f.Root).Status == VaultDailyRecoveryStatus.NoRecovery, "writer phase recovery completes idempotently");
            }
        }
        private static void WriterOwnershipEdges()
        {
            foreach (var edge in new[] { "journal", "stage", "settled-target" }) using (var f = new Fixture()) {
                File.WriteAllBytes(f.PathName, f.Old); string changedPath = null; byte[] changed = null;
                var result = VaultDailyStore.CompareExchangeForTest(f.Root, VaultDailyFile.Read(f.Root).Stamp, State("next"), delegate(VaultDailyWritePhase point) {
                    if ((edge != "settled-target" && point != VaultDailyWritePhase.JournalPublished) ||
                        (edge == "settled-target" && point != VaultDailyWritePhase.AuxiliariesRemoved)) return;
                    var journal = VaultDailyJournalCodec.Decode(File.ReadAllBytes(f.Marker), f.PathName).Journal;
                    changedPath = edge == "journal" ? f.Marker : edge == "stage" ? Path.Combine(f.Root, journal.StageFileName) : f.PathName;
                    changed = edge == "journal" ? VaultDailyJournalCodec.Encode(journal) :
                        VaultDailyRecordCodec.Encode(new VaultDailyRecord(State("foreign-image"), journal.Next.Generation, journal.Next.Revision));
                    File.WriteAllBytes(changedPath, changed);
                });
                Check(result.Status == VaultDailyCommitStatus.FileUnavailable && result.Record == null && result.Stamp == null, "writer rejects changed ownership edge without false success");
                Check(File.Exists(f.Marker) && File.ReadAllBytes(changedPath).SequenceEqual(changed), "writer retains changed journal stage or target bytes");
                Check(VaultDailyFile.Read(f.Root).Status == VaultDailyFileReadStatus.FileUnavailable, "changed ownership edge remains blocked for explicit recovery");
                if (edge != "settled-target") Check(File.ReadAllBytes(f.PathName).SequenceEqual(f.Old), "pre-publish ownership change preserves old target");
            }
        }
        private static void Junction(string link, string target)
        {
            using (var process = Process.Start(new ProcessStartInfo { FileName = "cmd.exe", Arguments = "/d /c mklink /J \"" + link + "\" \"" + target + "\"", UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true })) {
                if (!process.WaitForExit(5000)) { process.Kill(); process.WaitForExit(5000); throw new Exception(); }
                if (process.ExitCode != 0) throw new Exception();
            }
        }
        private static void ReparseAndPaths()
        {
            foreach (var name in new[] { "journal", "stage", "backup" }) using (var f = new Fixture()) using (var other = new Fixture()) {
                f.Arrange(); File.WriteAllBytes(other.PathName, other.Old); string link = name == "journal" ? f.Marker : name == "stage" ? f.Stage : f.Backup;
                File.Delete(link); Junction(link, other.Root);
                try { Failure(VaultDailyRecovery.Recover(f.Root), VaultDailyRecoveryStatus.FileUnavailable, "native junction at known artifact"); Check(File.ReadAllBytes(other.PathName).SequenceEqual(other.Old) && File.ReadAllBytes(f.PathName).SequenceEqual(f.Old), "junction refusal preserves foreign and current targets"); }
                finally { Directory.Delete(link); }
            }
            using (var f = new Fixture()) using (var other = new Fixture()) {
                other.Arrange(); string link = Path.Combine(f.Root, "linked-root"); Junction(link, other.Root);
                try { Failure(VaultDailyRecovery.Recover(link), VaultDailyRecoveryStatus.FileUnavailable, "native root junction"); Check(File.ReadAllBytes(other.PathName).SequenceEqual(other.Old) && File.Exists(other.Marker), "root junction preserves target transaction"); }
                finally { Directory.Delete(link); }
            }
            using (var f = new Fixture()) { var absent = Path.Combine(f.Root, "never-created"); Failure(VaultDailyRecovery.Recover(absent), VaultDailyRecoveryStatus.FileUnavailable, "missing explicit root"); Check(!Directory.Exists(absent), "recovery never creates root"); }
        }
        private static Mutex Gate(bool broad = false)
        {
            SecurityIdentifier user; using (var identity = WindowsIdentity.GetCurrent()) user = identity.User;
            var acl = new MutexSecurity(); acl.SetAccessRuleProtection(true, false); acl.AddAccessRule(new MutexAccessRule(user, MutexRights.FullControl, AccessControlType.Allow));
            acl.AddAccessRule(new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null), MutexRights.FullControl, AccessControlType.Allow));
            if (broad) acl.AddAccessRule(new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), MutexRights.FullControl, AccessControlType.Allow));
            bool created; return new Mutex(false, "Global\\ProGo.VaultDaily." + user.Value + ".Writer", out created, acl);
        }
        private static void Await(Func<bool> ready)
        { var watch = Stopwatch.StartNew(); while (!ready()) { if (watch.ElapsedMilliseconds > 10000) throw new Exception(); Thread.Sleep(20); } }
        private static Process Holder(string mode, Fixture f)
        { return Process.Start(new ProcessStartInfo { FileName = Assembly.GetExecutingAssembly().Location, Arguments = mode + " \"" + f.Root + "\"", UseShellExecute = false, CreateNoWindow = true }); }
        private static void SharedGate()
        {
            using (var f = new Fixture()) using (var keep = Gate()) {
                f.Arrange(); Process holder = null; var before = Snapshot(f);
                try {
                    holder = Holder("hold", f);
                    Await(delegate { return File.Exists(Path.Combine(f.Root, "ready")); }); var watch = Stopwatch.StartNew();
                    Failure(VaultDailyRecovery.Recover(f.Root), VaultDailyRecoveryStatus.Busy, "shared writer gate held by real child");
                    Check(watch.ElapsedMilliseconds >= 900 && watch.ElapsedMilliseconds < 4000, "recovery gate wait is bounded");
                    File.WriteAllText(Path.Combine(f.Root, "release"), "release"); Check(holder.WaitForExit(15000) && holder.ExitCode == 0, "owned holder exits");
                } finally { if (holder != null) { try { if (!holder.HasExited) { holder.Kill(); holder.WaitForExit(5000); } } finally { holder.Dispose(); } } }
                File.Delete(Path.Combine(f.Root, "ready")); File.Delete(Path.Combine(f.Root, "release")); SameFiles(f, before, "busy recovery publishes nothing");
                Recovered(f, VaultDailyRecoveryStatus.RecoveredPrevious, f.Old, "shared gate released");
                f.Arrange(); holder = null;
                try {
                    holder = Holder("abandon", f); Await(delegate { return File.Exists(Path.Combine(f.Root, "ready")); });
                    File.WriteAllText(Path.Combine(f.Root, "release"), "release"); Check(holder.WaitForExit(15000) && holder.ExitCode == 0, "owned child abandons held gate");
                    File.Delete(Path.Combine(f.Root, "ready")); File.Delete(Path.Combine(f.Root, "release"));
                    Recovered(f, VaultDailyRecoveryStatus.RecoveredPrevious, f.Old, "abandoned gate triggers fresh recovery inspection");
                } finally { if (holder != null) { try { if (!holder.HasExited) { holder.Kill(); holder.WaitForExit(5000); } } finally { holder.Dispose(); } } }
            }
            using (var f = new Fixture()) using (var broad = Gate(true)) {
                f.Arrange(); Reject(f, VaultDailyRecoveryStatus.FileUnavailable, "broad precreated gate ACL");
                Check(broad.GetAccessControl().GetAccessRules(true, true, typeof(SecurityIdentifier)).Cast<MutexAccessRule>().Any(rule => new SecurityIdentifier(WellKnownSidType.WorldSid, null).Equals(rule.IdentityReference)), "recovery never repairs foreign gate ACL");
            }
        }
        private static int Main(string[] args)
        {
            try {
                if (args.Length == 2 && (args[0] == "hold" || args[0] == "abandon")) { using (var gate = Gate()) { gate.WaitOne(); File.WriteAllText(Path.Combine(args[1], "ready"), "ready"); Await(delegate { return File.Exists(Path.Combine(args[1], "release")); }); if (args[0] == "abandon") Environment.Exit(0); gate.ReleaseMutex(); } return 0; }
                phase = "known states"; CleanAndKnownStates(); phase = "unknown evidence"; UnknownArtifactsAndJournal();
                phase = "retry locks ACL"; RetryAndNativeLocks(); phase = "writer checkpoints"; WriterCheckpoints();
                phase = "writer ownership edges"; WriterOwnershipEdges();
                phase = "reparse paths"; ReparseAndPaths(); phase = "shared gate"; SharedGate();
                Check(AppPaths.Accesses == 0 && SafeLog.Accesses == 0, "recovery never touches application paths or logs");
                Console.WriteLine("PASS: VaultDailyRecoveryTests (" + passed + " checks)"); return 0;
            } catch (Exception ex) { Console.Error.WriteLine("FAIL: VaultDailyRecoveryTests phase=" + phase + " type=" + ex.GetType().Name + " passed=" + passed); return 1; }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;

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
    internal static class VaultDailyFileTests
    {
        private static int passed;
        private enum TestPhase { Startup, Paths, ExactRead, Failures, AclOriginal, AclOriginalSddl, AclDeny,
            AclDenyPersist, AclDeniedRead, AclRestoreCreate, AclRestoreDescriptor, AclRestorePersist,
            AclRestoredRead, AclRestoredAccess, AclRestoredMetadata, AclExactCheck,
            AclMatrix, DirectoryTarget, Recovery, Reparse, AppGuards }
        private static TestPhase phase;
        private static string work;
        private static readonly DateTime Day = new DateTime(2026, 10, 12);
        private static readonly Guid Generation = new Guid("0c419b44-470b-421e-a737-8c2be10fa0a8");
        private static void Check(bool condition, string name)
        { if (!condition) { Console.Error.WriteLine("FAIL: " + name); throw new Exception("Static test assertion failed."); }
            passed++; Console.WriteLine("PASS: " + name); }
        private static bool SameAccessPermissions(FileSecurity first, FileSecurity second)
        {
            if (first == null || second == null || first.AreAccessRulesProtected != second.AreAccessRulesProtected ||
                first.AreAccessRulesCanonical != second.AreAccessRulesCanonical) return false;
            var a = first.GetAccessRules(true, true, typeof(SecurityIdentifier));
            var b = second.GetAccessRules(true, true, typeof(SecurityIdentifier));
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++) {
                var left = (FileSystemAccessRule)a[i]; var right = (FileSystemAccessRule)b[i];
                bool identitySame = left.IdentityReference.Equals(right.IdentityReference), rightsSame = left.FileSystemRights == right.FileSystemRights,
                    typeSame = left.AccessControlType == right.AccessControlType,
                    flagsSame = left.InheritanceFlags == right.InheritanceFlags && left.PropagationFlags == right.PropagationFlags,
                    isInheritedSame = left.IsInherited == right.IsInherited;
                if (!identitySame || !rightsSame || !typeSame || !flagsSame || !isInheritedSame) {
                    Console.WriteLine("CHECK: access rule mismatch index=" + i + " identitySame=" + identitySame + " rightsSame=" + rightsSame +
                        " typeSame=" + typeSame + " flagsSame=" + flagsSame + " isInheritedSame=" + isInheritedSame); return false;
                }
            }
            return true;
        }
        private static FileSecurity MemoryAcl(SecurityIdentifier sid, FileSystemRights rights)
        { var acl = new FileSecurity(); acl.AddAccessRule(new FileSystemAccessRule(sid, rights, AccessControlType.Allow)); return acl; }
        private static void AccessComparison()
        {
            var sid = new SecurityIdentifier("S-1-5-21-1-2-3-1001");
            var original = MemoryAcl(sid, FileSystemRights.ReadData); var clone = new FileSecurity();
            clone.SetSecurityDescriptorSddlForm(original.GetSecurityDescriptorSddlForm(AccessControlSections.Access), AccessControlSections.Access);
            Check(SameAccessPermissions(original, clone), "ordered permission comparer accepts an equivalent Access descriptor clone");
            var rights = MemoryAcl(sid, FileSystemRights.WriteData);
            Check(rights.GetAccessRules(true, true, typeof(SecurityIdentifier)).Count == 1 && !SameAccessPermissions(original, rights),
                "ordered permission comparer rejects changed rights with unchanged rule count");
            var identity = MemoryAcl(new SecurityIdentifier("S-1-5-21-1-2-3-1002"), FileSystemRights.ReadData);
            Check(identity.GetAccessRules(true, true, typeof(SecurityIdentifier)).Count == 1 && !SameAccessPermissions(original, identity),
                "ordered permission comparer rejects changed SID with unchanged rule count");
            clone.SetAccessRuleProtection(true, true);
            Check(clone.GetAccessRules(true, true, typeof(SecurityIdentifier)).Count == 1 && !SameAccessPermissions(original, clone),
                "ordered permission comparer rejects changed protection with unchanged rule count");
        }
        private static string Root(string name)
        { var root = Path.Combine(work, name); Directory.CreateDirectory(root); return root; }
        private static string DataPath(string root) { return Path.Combine(root, VaultDailyFile.FileName); }
        private static VaultDailyRecord Record(long revision = 7, Guid? generation = null, DateTime? day = null)
        {
            var data = new VaultData { version = 1, entries = new List<VaultEntry> { new VaultEntry {
                id = "synthetic-id", name = "Имя", secret = "synthetic-secret", notes = "saved edit", tags = "" } } };
            return new VaultDailyRecord(VaultDailyState.Bind(day ?? Day, "5678", data), generation ?? Generation, revision);
        }
        private static byte[] Publish(string root, VaultDailyRecord record = null)
        { var bytes = VaultDailyRecordCodec.Encode(record ?? Record()); File.WriteAllBytes(DataPath(root), bytes); return bytes; }
        private static void Failed(string root, VaultDailyFileReadStatus status, string name)
        {
            var result = VaultDailyFile.Read(root);
            Check(result.Status == status && result.Record == null && result.Stamp == null, name);
            var message = status == VaultDailyFileReadStatus.FileUnavailable ? "Не удалось прочитать суточное хранилище." :
                status == VaultDailyFileReadStatus.UnsupportedFormat ? "Формат суточного хранилища не поддерживается." :
                "Суточное хранилище повреждено или недоступно.";
            Check(result.ErrorMessage == message, name + " returns static error without path or payload");
        }
        private static void MissingAndPaths()
        {
            var root = Root("absent"); var path = DataPath(root);
            var first = VaultDailyFile.Read(root); var second = VaultDailyFile.Read(root);
            Check(first.Status == VaultDailyFileReadStatus.Missing && first.Record == null && first.ErrorMessage == null &&
                first.Stamp != null && first.Stamp.IsMissing, "absent exact child has missing stamp without record");
            Check(first.Stamp.Matches(second.Stamp) && first.Stamp.BelongsTo(path), "repeated absence stamps match canonical scope");
            Check(!first.Stamp.Matches(null) && !File.Exists(path) && Directory.GetFileSystemEntries(root).Length == 0,
                "reader never creates missing storage or staging files");
            Check(VaultDailyFile.CheckedPath(root + Path.DirectorySeparatorChar) == path &&
                VaultDailyFile.CheckedPath(root.Replace('\\', '/')) == path, "local qualified path normalizes separators and trailing separator");
            var other = Root("other-absent");
            Check(!first.Stamp.Matches(VaultDailyFile.Read(other).Stamp) && !first.Stamp.BelongsTo(DataPath(other)),
                "missing stamp cannot be used in another root");
            Failed(Path.Combine(root, "not-created"), VaultDailyFileReadStatus.FileUnavailable, "missing parent is unavailable rather than missing child");
            Check(!Directory.Exists(Path.Combine(root, "not-created")), "missing parent remains absent");
            foreach (var bad in new string[] { null, "", ".", "relative", "C:relative", "\\rooted", "\\\\server\\share", "\\\\?\\C:\\temp",
                "\\\\.\\C:\\temp", "C:\\temp:stream", "C:/temp:stream", "C:\\te?mp", "C:\\te*mp", "C:\\te\0mp" })
                Failed(bad, VaultDailyFileReadStatus.FileUnavailable, "nonlocal ambiguous or device path is unavailable");
            var regularFile = Path.Combine(root, "not-directory"); File.WriteAllText(regularFile, "synthetic");
            Failed(regularFile, VaultDailyFileReadStatus.FileUnavailable, "file used as root is unavailable");
            Directory.Delete(other);
            Failed(other, VaultDailyFileReadStatus.FileUnavailable, "removed root never produces missing stamp");
        }
        private static void ExactReadAndStamp()
        {
            var root = Root("exact"); var encoded = Publish(root); var before = File.ReadAllBytes(DataPath(root));
            var first = VaultDailyFile.Read(root); var second = VaultDailyFile.Read(root);
            Check(first.Status == VaultDailyFileReadStatus.Success && first.Record != null && first.Stamp != null &&
                !first.Stamp.IsMissing && first.ErrorMessage == null, "valid protected file opens with success stamp");
            Check(first.Record.Generation == Generation && first.Record.Revision == 7 && first.Record.State.LocalDay == Day &&
                first.Record.State.FakePin == "5678" && first.Record.State.FakeData.entries[0].notes == "saved edit",
                "reader preserves exact bound metadata and saved edit");
            Check(first.Stamp.Generation == Generation && first.Stamp.Revision == 7 && first.Stamp.LocalDay == Day &&
                first.Stamp.Matches(second.Stamp), "repeated byte images have immutable matching metadata and digest");
            Check(first.Stamp.BelongsTo(DataPath(root).ToUpperInvariant()), "scope comparison follows Windows ordinal case-insensitivity");
            Check(!Object.ReferenceEquals(first.Record.State.FakeData, second.Record.State.FakeData) &&
                !Object.ReferenceEquals(first.Record.State.FakeData.entries[0], second.Record.State.FakeData.entries[0]),
                "each successful read owns a detached dataset and entries");
            first.Record.State.FakeData.entries[0].notes = "caller edit";
            Check(second.Record.State.FakeData.entries[0].notes == "saved edit" && first.Stamp.Matches(second.Stamp) &&
                File.ReadAllBytes(DataPath(root)).SequenceEqual(before), "caller edits do not mutate another read stamp or on-disk image");
            var factoryBytes = (byte[])encoded.Clone();
            var factory = VaultDailyFileReadResult.Loaded(DataPath(root), second.Record, factoryBytes);
            Array.Clear(factoryBytes, 0, factoryBytes.Length);
            Check(factory.Stamp.Matches(second.Stamp), "stamp defensively owns digest independently of caller envelope array");
            var other = Root("same-bytes-other-scope"); File.WriteAllBytes(DataPath(other), encoded);
            Check(!second.Stamp.Matches(VaultDailyFile.Read(other).Stamp), "same byte image in different root never matches");
            Publish(root, Record());
            var rerandomized = VaultDailyFile.Read(root);
            Check(!second.Stamp.Matches(rerandomized.Stamp) && rerandomized.Stamp.Revision == second.Stamp.Revision &&
                rerandomized.Stamp.Generation == second.Stamp.Generation, "same metadata in fresh DPAPI image still differs by digest");
            foreach (var replacement in new VaultDailyRecord[] { Record(8), Record(7, Guid.NewGuid()), Record(7, day: Day.AddDays(1)) }) {
                Publish(root, replacement); var current = VaultDailyFile.Read(root);
                Check(current.Status == VaultDailyFileReadStatus.Success && !second.Stamp.Matches(current.Stamp),
                    "revision generation or local day replacement changes stamp");
            }
            File.Delete(DataPath(root));
            Check(!second.Stamp.Matches(VaultDailyFile.Read(root).Stamp), "success and missing stamps cannot match");
            Publish(root, new VaultDailyRecord(VaultDailyState.Empty(Day), Generation, 0));
            Check(!VaultDailyFile.Read(root).Record.State.HasFake, "protected empty tombstone is success rather than missing");
            Check(Directory.GetFileSystemEntries(root).Length == 1, "successful reads leave only the pre-existing data file");
        }
        private static void CorruptionAndAvailability()
        {
            var root = Root("failures"); var path = DataPath(root);
            foreach (int length in new int[] { 0, 1, 15 }) {
                File.WriteAllBytes(path, new byte[length]); Failed(root, VaultDailyFileReadStatus.InvalidData, "empty or truncated file is invalid");
            }
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
                stream.SetLength((long)VaultDailyRecordCodec.MaxEnvelopeBytes + 1);
            Failed(root, VaultDailyFileReadStatus.InvalidData, "over-cap length is refused before reading body");
            Check(new FileInfo(path).Length == (long)VaultDailyRecordCodec.MaxEnvelopeBytes + 1, "over-cap refusal leaves file bytes and size intact");
            var bad = Publish(root); bad[bad.Length - 1] ^= 1; File.WriteAllBytes(path, bad);
            Failed(root, VaultDailyFileReadStatus.InvalidData, "native DPAPI corruption is invalid rather than missing");
            var unsupported = Publish(root); unsupported[8] = 2; File.WriteAllBytes(path, unsupported);
            Failed(root, VaultDailyFileReadStatus.UnsupportedFormat, "well-framed unknown envelope maps unsupported format");
            var malformed = unsupported.Concat(new byte[] { 1 }).ToArray(); File.WriteAllBytes(path, malformed);
            Failed(root, VaultDailyFileReadStatus.InvalidData, "unknown version with invalid framing stays invalid");
            Publish(root);
            using (var holder = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
                Failed(root, VaultDailyFileReadStatus.FileUnavailable, "actual exclusive file handle maps unavailable");
            using (var holder = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                Check(VaultDailyFile.Read(root).Status == VaultDailyFileReadStatus.Success, "compatible read sharing preserves stable successful image");
            phase = TestPhase.AclOriginal;
            var originalAcl = File.GetAccessControl(path, AccessControlSections.Access);
            phase = TestPhase.AclOriginalSddl;
            var originalSddl = originalAcl.GetSecurityDescriptorSddlForm(AccessControlSections.Access);
            phase = TestPhase.AclDeny;
            var denied = File.GetAccessControl(path, AccessControlSections.Access);
            using (var identity = WindowsIdentity.GetCurrent())
                denied.AddAccessRule(new FileSystemAccessRule(identity.User, FileSystemRights.ReadData, AccessControlType.Deny));
            try {
                phase = TestPhase.AclDenyPersist; File.SetAccessControl(path, denied);
                phase = TestPhase.AclDeniedRead; Failed(root, VaultDailyFileReadStatus.FileUnavailable, "actual owner read-deny ACL maps unavailable");
            }
            finally {
                // An unmodified FileSecurity snapshot would persist no DACL changes.
                var beforeRestore = phase;
                phase = TestPhase.AclRestoreCreate;
                var restored = new FileSecurity();
                phase = TestPhase.AclRestoreDescriptor;
                restored.SetSecurityDescriptorSddlForm(originalSddl, AccessControlSections.Access);
                phase = TestPhase.AclRestorePersist;
                File.SetAccessControl(path, restored);
                phase = beforeRestore;
            }
            phase = TestPhase.AclRestoredRead;
            var restoredRead = VaultDailyFile.Read(root);
            Console.WriteLine("CHECK: restored daily read status=" + restoredRead.Status);
            Check(restoredRead.Status == VaultDailyFileReadStatus.Success, "owned file ACL is restored and readable after denied test");
            phase = TestPhase.AclRestoredAccess;
            var restoredAcl = File.GetAccessControl(path, AccessControlSections.Access);
            phase = TestPhase.AclRestoredMetadata;
            Console.WriteLine("CHECK: original DACL protected=" + originalAcl.AreAccessRulesProtected + " canonical=" + originalAcl.AreAccessRulesCanonical +
                " rules=" + originalAcl.GetAccessRules(true, true, typeof(SecurityIdentifier)).Count);
            Console.WriteLine("CHECK: restored DACL protected=" + restoredAcl.AreAccessRulesProtected + " canonical=" + restoredAcl.AreAccessRulesCanonical +
                " rules=" + restoredAcl.GetAccessRules(true, true, typeof(SecurityIdentifier)).Count);
            phase = TestPhase.AclExactCheck;
            Check(SameAccessPermissions(originalAcl, restoredAcl),
                "original ordered access rules and protection are restored after denied test");
            phase = TestPhase.DirectoryTarget;
            File.Delete(path); Directory.CreateDirectory(path);
            Failed(root, VaultDailyFileReadStatus.FileUnavailable, "directory at data filename is unavailable");
            Directory.Delete(path);
        }
        private static void Junction(string link, string target)
        {
            using (var process = Process.Start(new ProcessStartInfo("cmd.exe", "/D /C mklink /J \"" + link + "\" \"" + target + "\"") {
                UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true })) {
                if (!process.WaitForExit(10000)) { process.Kill(); process.WaitForExit(5000); throw new Exception("Owned junction fixture deadline"); }
                Check(process.ExitCode == 0 && (File.GetAttributes(link) & FileAttributes.ReparsePoint) != 0,
                    "owned fixture creates actual Windows directory junction");
            }
        }
        private static void RecoveryWitness()
        {
            var root = Root("recovery-witness"); var bytes = Publish(root);
            var marker = Path.Combine(root, VaultDailyFile.RecoveryFileName);
            File.WriteAllBytes(marker, new byte[0]);
            Failed(root, VaultDailyFileReadStatus.FileUnavailable, "existing recovery witness blocks valid ciphertext adoption");
            Check(File.ReadAllBytes(DataPath(root)).SequenceEqual(bytes) && File.Exists(marker) && new FileInfo(marker).Length == 0,
                "reader preserves original ciphertext and zero-byte recovery witness");
            File.Delete(DataPath(root));
            Failed(root, VaultDailyFileReadStatus.FileUnavailable, "recovery witness blocks new missing binding");
            Check(!File.Exists(DataPath(root)) && File.Exists(marker), "blocked missing read creates and repairs nothing");
            File.Delete(marker); Directory.CreateDirectory(marker); Publish(root);
            Failed(root, VaultDailyFileReadStatus.FileUnavailable, "directory recovery witness also blocks adoption");
            Directory.Delete(marker);
            Check(VaultDailyFile.Read(root).Status == VaultDailyFileReadStatus.Success, "owned fixture removal of witness permits valid read again");
            bytes = File.ReadAllBytes(DataPath(root));
            var backup = Path.Combine(root, VaultDailyFile.RecoveryBackupFileName); File.WriteAllBytes(backup, bytes);
            Failed(root, VaultDailyFileReadStatus.FileUnavailable, "orphan native backup blocks valid data without stable witness");
            Check(File.ReadAllBytes(DataPath(root)).SequenceEqual(bytes) && new FileInfo(backup).Length == bytes.Length && !File.Exists(marker),
                "orphan backup refusal preserves ciphertext and creates no stable witness");
            File.Delete(DataPath(root));
            Failed(root, VaultDailyFileReadStatus.FileUnavailable, "orphan native backup blocks missing binding without stable witness");
            Check(!File.Exists(DataPath(root)) && File.Exists(backup), "orphan backup refusal repairs and creates nothing");
            File.Delete(backup); Directory.CreateDirectory(backup); Publish(root);
            Failed(root, VaultDailyFileReadStatus.FileUnavailable, "directory at native backup filename also blocks adoption");
            Directory.Delete(backup);
        }
        private static void ReparseGuards()
        {
            var target = Root("junction-target"); var bytes = Publish(target); var nested = Path.Combine(target, "nested"); Directory.CreateDirectory(nested);
            var link = Path.Combine(work, "linked-root");
            try {
                Junction(link, target);
                Failed(link, VaultDailyFileReadStatus.FileUnavailable, "root junction is unavailable");
                Failed(Path.Combine(link, "nested"), VaultDailyFileReadStatus.FileUnavailable, "junction ancestor of normal root is unavailable");
            } finally { if (Directory.Exists(link)) Directory.Delete(link); }
            var root = Root("linked-data"); var dataLink = DataPath(root);
            try {
                Junction(dataLink, target);
                Failed(root, VaultDailyFileReadStatus.FileUnavailable, "junction at data filename is unavailable");
            } finally { if (Directory.Exists(dataLink)) Directory.Delete(dataLink); }
            Check(File.ReadAllBytes(DataPath(target)).SequenceEqual(bytes) && Directory.Exists(nested),
                "reader and junction cleanup preserve target bytes and subdirectory");
        }
        public static int Main()
        {
            try {
                phase = TestPhase.Startup; work = Path.Combine(Path.GetTempPath(), "ProGo-daily-file-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(work);
                phase = TestPhase.AclMatrix; AccessComparison();
                phase = TestPhase.Paths; MissingAndPaths(); phase = TestPhase.ExactRead; ExactReadAndStamp();
                phase = TestPhase.Failures; CorruptionAndAvailability(); phase = TestPhase.Recovery; RecoveryWitness();
                phase = TestPhase.Reparse; ReparseGuards(); phase = TestPhase.AppGuards;
                Check(AppPaths.Accesses == 0 && SafeLog.Accesses == 0, "reader never touches application paths or logging");
                Console.WriteLine("PASS: VaultDailyFileTests (" + passed + " checks)"); return 0;
            } catch (Exception ex) { Console.Error.WriteLine("FAIL: VaultDailyFileTests phase=" + phase + " type=" + ex.GetType().Name + " passed=" + passed); return 1; }
            finally { if (Directory.Exists(work)) Directory.Delete(work, true); }
        }
    }
}

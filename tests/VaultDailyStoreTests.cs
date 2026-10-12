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
    internal static class VaultDailyStoreTests
    {
        private static int passed;
        private static readonly DateTime Day = new DateTime(2026, 10, 12);
        private const string Pin = "1111";
        private static void Check(bool value, string name)
        { if (!value) throw new Exception(name); passed++; Console.WriteLine("PASS: " + name); }
        private sealed class Fixture : IDisposable
        {
            internal readonly string Root = Path.Combine(Path.GetTempPath(), "ProGo-DailyStore-" + Guid.NewGuid().ToString("N"));
            internal string PathName { get { return Path.Combine(Root, VaultDailyFile.FileName); } }
            internal Fixture() { Directory.CreateDirectory(Root); }
            public void Dispose() { Directory.Delete(Root, true); }
        }
        private static VaultData Data(string notes = "initial")
        { return new VaultData { version = 1, entries = new List<VaultEntry> { new VaultEntry { id = "synthetic", notes = notes, secret = "synthetic-secret" } } }; }
        private static VaultDailyState Bound(DateTime? day = null, string pin = Pin, VaultData data = null)
        { return VaultDailyState.Bind(day ?? Day, pin, data ?? Data()); }
        private static VaultDailyFileReadResult Read(Fixture f) { return VaultDailyFile.Read(f.Root); }
        private static VaultDailyCommitResult Save(Fixture f, VaultDailyFileStamp expected, VaultDailyState state)
        { return VaultDailyStore.CompareExchange(f.Root, expected, state); }
        private static void Failed(VaultDailyCommitResult result, VaultDailyCommitStatus status, string name)
        {
            Check(result.Status == status, name + " status");
            Check(result.Record == null && result.Stamp == null && !String.IsNullOrEmpty(result.ErrorMessage), name + " publishes no state");
        }
        private static void Success(Fixture f, VaultDailyCommitResult result, string name)
        {
            Check(result.Status == VaultDailyCommitStatus.Success && result.Record != null && result.Stamp != null && result.ErrorMessage == null, name);
            var read = Read(f);
            Check(read.Status == VaultDailyFileReadStatus.Success && read.Stamp.Matches(result.Stamp), name + " durable stamp");
            Check(Directory.GetFiles(f.Root, "*.new-*").Length == 0 && !File.Exists(f.PathName + ".recovery") && !File.Exists(f.PathName + ".recovery.previous"), name + " leaves no staging or recovery artifacts");
        }
        private static void Put(Fixture f, VaultDailyRecord record)
        { var bytes = VaultDailyRecordCodec.Encode(record); try { File.WriteAllBytes(f.PathName, bytes); } finally { Array.Clear(bytes, 0, bytes.Length); } }
        private static void Unchanged(Fixture f, byte[] before, string name)
        { Check(File.ReadAllBytes(f.PathName).SequenceEqual(before), name + " preserves ciphertext"); Check(Directory.GetFiles(f.Root, "*.new-*").Length == 0, name + " removes own staging"); }

        private static void InitialAndEdits()
        {
            using (var f = new Fixture()) {
                var missing = Read(f); var data = Data(); var source = Bound(data: data);
                var first = Save(f, missing.Stamp, source); Success(f, first, "first exclusive creation");
                Check(first.Record.Revision == 0 && first.Record.Generation != Guid.Empty && first.Record.State.HasFake, "initial generation and revision");
                Check(Object.ReferenceEquals(source.FakeData, data) && data.entries[0].notes == "initial", "save leaves caller data untouched");
                Check(!Object.ReferenceEquals(data, first.Record.State.FakeData) && !Object.ReferenceEquals(data.entries[0], first.Record.State.FakeData.entries[0]), "commit returns detached data and entries");
                data.entries[0].notes = "caller-after-save";
                Check(Read(f).Record.State.FakeData.entries[0].notes == "initial", "caller changes cannot alter durable snapshot");
                first.Record.State.FakeData.entries[0].notes = "returned-edit";
                Check(Read(f).Record.State.FakeData.entries[0].notes == "initial" && Read(f).Stamp.Matches(first.Stamp), "result changes cannot alter durable snapshot or immutable stamp");
                var edited = Save(f, first.Stamp, first.Record.State); Success(f, edited, "edited dataset replacement");
                Check(edited.Record.Generation == first.Record.Generation && edited.Record.Revision == 1 && edited.Record.State.FakeData.entries[0].notes == "returned-edit", "same day preserves generation and increments revision");
                var deleted = Save(f, edited.Stamp, Bound(data: VaultData.Empty())); Success(f, deleted, "delete all entries");
                Check(deleted.Record.State.HasFake && deleted.Record.State.FakePin == Pin && deleted.Record.State.FakeData.entries.Count == 0, "empty entry list preserves bound PIN");
                var before = File.ReadAllBytes(f.PathName);
                Failed(Save(f, deleted.Stamp, Bound(pin: "2222")), VaultDailyCommitStatus.InvalidData, "same day PIN rebind"); Unchanged(f, before, "rebind rejection");
                Failed(Save(f, deleted.Stamp, VaultDailyState.Empty(Day)), VaultDailyCommitStatus.InvalidData, "same day bound reset"); Unchanged(f, before, "reset rejection");
                Failed(Save(f, missing.Stamp, Bound()), VaultDailyCommitStatus.Conflict, "stale missing token");
                Failed(Save(f, first.Stamp, Bound()), VaultDailyCommitStatus.Conflict, "stale revision token");
            }
            using (var f = new Fixture()) {
                var first = Save(f, Read(f).Stamp, VaultDailyState.Empty(Day)); Success(f, first, "initial unbound tombstone");
                var empty = Save(f, first.Stamp, VaultDailyState.Empty(Day)); Success(f, empty, "same day unbound update");
                Check(empty.Record.Generation == first.Record.Generation && empty.Record.Revision == 1 && !empty.Record.State.HasFake, "unbound revision advances without binding");
                var bound = VaultDailyStore.CompareExchange(Path.Combine(f.Root, "."), empty.Stamp, Bound()); Success(f, bound, "canonical directory alias");
                Check(bound.Record.Revision == 2 && bound.Record.Generation == first.Record.Generation, "unbound can become bound on same day");
            }
        }
        private static void StaleAndScope()
        {
            using (var f = new Fixture()) using (var other = new Fixture()) {
                Failed(Save(f, Read(other).Stamp, Bound()), VaultDailyCommitStatus.Conflict, "foreign missing scope");
                Check(!File.Exists(f.PathName), "foreign token creates no file");
                var saved = Save(f, Read(f).Stamp, Bound());
                Failed(Save(other, saved.Stamp, Bound()), VaultDailyCommitStatus.Conflict, "foreign loaded scope");
                var observed = Read(f);
                Put(f, new VaultDailyRecord(Bound(data: Data("different-content")), observed.Record.Generation, observed.Record.Revision));
                var before = File.ReadAllBytes(f.PathName);
                Failed(Save(f, observed.Stamp, Bound()), VaultDailyCommitStatus.Conflict, "same metadata changed digest"); Unchanged(f, before, "digest conflict");
                observed = Read(f); Put(f, new VaultDailyRecord(observed.Record.State, Guid.NewGuid(), observed.Record.Revision));
                Failed(Save(f, observed.Stamp, Bound()), VaultDailyCommitStatus.Conflict, "changed generation token");
                observed = Read(f); Put(f, new VaultDailyRecord(observed.Record.State, observed.Record.Generation, observed.Record.Revision + 1));
                Failed(Save(f, observed.Stamp, Bound()), VaultDailyCommitStatus.Conflict, "changed revision token");
                observed = Read(f); Put(f, new VaultDailyRecord(Bound(Day.AddDays(1)), observed.Record.Generation, observed.Record.Revision));
                Failed(Save(f, observed.Stamp, Bound()), VaultDailyCommitStatus.Conflict, "changed calendar token");
            }
        }
        private static void DateResetAndExhaustion()
        {
            using (var f = new Fixture()) {
                var first = Save(f, Read(f).Stamp, Bound()); var before = File.ReadAllBytes(f.PathName);
                Failed(Save(f, first.Stamp, Bound(Day.AddDays(1))), VaultDailyCommitStatus.InvalidData, "new day cannot bind directly"); Unchanged(f, before, "direct new day binding");
                var reset = Save(f, first.Stamp, VaultDailyState.Empty(Day.AddDays(1))); Success(f, reset, "new day empty tombstone");
                Check(reset.Record.Generation != first.Record.Generation && reset.Record.Revision == 0 && !reset.Record.State.HasFake && reset.Record.State.LocalDay == Day.AddDays(1), "reset rotates generation and clears binding");
                Failed(Save(f, first.Stamp, first.Record.State), VaultDailyCommitStatus.Conflict, "pre-reset writer token");
                var newBound = Save(f, reset.Stamp, Bound(Day.AddDays(1), "2222")); Success(f, newBound, "new day first binding");
                Check(newBound.Record.State.FakePin == "2222" && newBound.Record.Revision == 1, "new binding follows committed reset");
                var backward = Save(f, newBound.Stamp, VaultDailyState.Empty(Day)); Success(f, backward, "calendar change backwards reset");
                Check(backward.Record.Generation != newBound.Record.Generation && backward.Record.Revision == 0, "backward reset is a new generation");
                Put(f, new VaultDailyRecord(Bound(), Guid.NewGuid(), Int64.MaxValue)); var exhausted = Read(f); before = File.ReadAllBytes(f.PathName);
                Failed(Save(f, exhausted.Stamp, Bound()), VaultDailyCommitStatus.RevisionExhausted, "revision overflow"); Unchanged(f, before, "overflow rejection");
                reset = Save(f, exhausted.Stamp, VaultDailyState.Empty(Day.AddDays(1))); Success(f, reset, "new generation after exhausted revision");
                Check(reset.Record.Revision == 0 && reset.Record.Generation != exhausted.Record.Generation, "exhausted day can rotate safely");
            }
        }
        private static void Failures()
        {
            using (var f = new Fixture()) {
                var expected = Read(f).Stamp;
                Failed(Save(f, null, Bound()), VaultDailyCommitStatus.InvalidData, "null expected token");
                Failed(Save(f, expected, null), VaultDailyCommitStatus.InvalidData, "null candidate");
                var invalid = Data(); var state = Bound(data: invalid); invalid.version = 2;
                Failed(Save(f, expected, state), VaultDailyCommitStatus.InvalidData, "mutated invalid dataset");
                Check(!File.Exists(f.PathName) && Directory.GetFiles(f.Root).Length == 0 && invalid.version == 2, "invalid candidate publishes nothing and remains unchanged");
                File.WriteAllBytes(f.PathName, new byte[] { 1, 2, 3 }); var before = File.ReadAllBytes(f.PathName);
                Failed(Save(f, expected, Bound()), VaultDailyCommitStatus.InvalidData, "corrupt current read"); Unchanged(f, before, "corrupt read rejection");
                Put(f, new VaultDailyRecord(Bound(), Guid.NewGuid(), 0)); before = File.ReadAllBytes(f.PathName); Buffer.BlockCopy(BitConverter.GetBytes(2), 0, before, 8, 4); File.WriteAllBytes(f.PathName, before);
                Failed(Save(f, expected, Bound()), VaultDailyCommitStatus.UnsupportedFormat, "unsupported current read"); Unchanged(f, before, "unsupported read rejection");
                File.Delete(f.PathName); Directory.CreateDirectory(f.PathName);
                Failed(Save(f, expected, Bound()), VaultDailyCommitStatus.FileUnavailable, "target directory cannot be overwritten"); Check(Directory.Exists(f.PathName), "target directory preserved");
                var nonexistent = Path.Combine(f.Root, "absent");
                Failed(VaultDailyStore.CompareExchange(nonexistent, expected, Bound()), VaultDailyCommitStatus.FileUnavailable, "missing root rejected"); Check(!Directory.Exists(nonexistent), "root is never implicitly created");
            }
        }
        private static void NativePublishFailures()
        {
            using (var f = new Fixture()) {
                var saved = Save(f, Read(f).Stamp, Bound()); var before = File.ReadAllBytes(f.PathName); FileStream locked = null;
                try {
                    var result = VaultDailyStore.CompareExchange(f.Root, saved.Stamp, Bound(data: Data("attempt")), delegate {
                        Check(Directory.GetFiles(f.Root, "*.new-*").Length == 1, "staging exists before publication");
                        locked = new FileStream(f.PathName, FileMode.Open, FileAccess.Read, FileShare.Read);
                    });
                    Failed(result, VaultDailyCommitStatus.FileUnavailable, "native target delete-lock"); Unchanged(f, before, "failed native replace");
                    Check(File.Exists(f.PathName + ".recovery") && File.Exists(f.PathName + ".recovery.previous") && Read(f).Status == VaultDailyFileReadStatus.FileUnavailable, "failed native replace retains stable witness and backup and blocks reads");
                } finally { if (locked != null) locked.Dispose(); }
                Failed(Save(f, saved.Stamp, Bound()), VaultDailyCommitStatus.FileUnavailable, "unresolved witness blocks another writer");
                File.Delete(f.PathName + ".recovery.previous");
                File.Delete(f.PathName + ".recovery"); // Explicit synthetic fixture recovery after proving old ciphertext is intact.
                Success(f, Save(f, saved.Stamp, Bound()), "writer gate released after publication failure");
            }
            using (var f = new Fixture()) {
                var expected = Read(f).Stamp;
                var result = VaultDailyStore.CompareExchange(f.Root, expected, Bound(), delegate { File.WriteAllBytes(f.PathName, new byte[] { 9, 8, 7 }); });
                Failed(result, VaultDailyCommitStatus.InvalidData, "corrupt target appeared before final read"); Unchanged(f, new byte[] { 9, 8, 7 }, "final read preserves newly appeared corrupt target");
                Check(!File.Exists(f.PathName + ".recovery"), "pre-publication rejection creates no witness");
                File.Delete(f.PathName);
                result = VaultDailyStore.CompareExchange(f.Root, expected, Bound(), delegate { Directory.CreateDirectory(f.PathName); });
                Failed(result, VaultDailyCommitStatus.FileUnavailable, "target directory appeared before publish");
                Check(Directory.Exists(f.PathName) && Directory.GetFiles(f.Root, "*.new-*").Length == 0, "final directory check preserves foreign target and cleans staging");
            }
            using (var f = new Fixture()) {
                var expected = Read(f).Stamp;
                var result = VaultDailyStore.CompareExchange(f.Root, expected, Bound(), delegate { throw new IOException("Synthetic pre-publication fault"); });
                Failed(result, VaultDailyCommitStatus.FileUnavailable, "fault before witness and publication");
                Check(!File.Exists(f.PathName) && !File.Exists(f.PathName + ".recovery") && Read(f).Status == VaultDailyFileReadStatus.Missing, "known pre-witness failure leaves initial target missing");
                File.WriteAllBytes(f.PathName + ".recovery", new byte[0]); // Synthetic persisted crash checkpoint after witness, before initial Move.
                Check(Read(f).Status == VaultDailyFileReadStatus.FileUnavailable, "persisted witness blocks missing target after incomplete initial publication");
                Failed(Save(f, expected, Bound()), VaultDailyCommitStatus.FileUnavailable, "incomplete first publication blocks retry");
                Check(Directory.GetFiles(f.Root, "*.new-*").Length == 0, "fault cleans only owned staging");
            }
            foreach (var suffix in new[] { ".recovery", ".recovery.previous" }) using (var f = new Fixture()) {
                var expected = Read(f).Stamp; var marker = f.PathName + suffix; File.WriteAllBytes(marker, new byte[] { 7, 6, 5 });
                Failed(Save(f, expected, Bound()), VaultDailyCommitStatus.FileUnavailable, "preexisting recovery artifact");
                Check(File.ReadAllBytes(marker).SequenceEqual(new byte[] { 7, 6, 5 }) && !File.Exists(f.PathName), "foreign recovery witness is never overwritten or removed");
            }
        }

        // Real child processes share only synthetic fixture roots and coordination markers.
        private static Mutex Gate(bool broad = false)
        {
            SecurityIdentifier user; using (var identity = WindowsIdentity.GetCurrent()) user = identity.User;
            var security = new MutexSecurity(); security.SetAccessRuleProtection(true, false);
            security.AddAccessRule(new MutexAccessRule(user, MutexRights.FullControl, AccessControlType.Allow));
            security.AddAccessRule(new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null), MutexRights.FullControl, AccessControlType.Allow));
            if (broad) security.AddAccessRule(new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), MutexRights.FullControl, AccessControlType.Allow));
            bool created; return new Mutex(false, "Global\\ProGo.VaultDaily." + user.Value + ".Writer", out created, security);
        }
        private static void Await(Func<bool> ready)
        { var watch = Stopwatch.StartNew(); while (!ready()) { if (watch.ElapsedMilliseconds > 10000) throw new Exception("Child readiness deadline"); Thread.Sleep(20); } }
        private static Process Child(string mode, Fixture f, string id = "holder")
        { return Process.Start(new ProcessStartInfo { FileName = Assembly.GetExecutingAssembly().Location, Arguments = mode + " \"" + f.Root + "\" " + id, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true }); }
        private static string Finish(Process process)
        { if (!process.WaitForExit(15000)) throw new Exception("Child completion deadline"); var output = process.StandardOutput.ReadToEnd().Trim(); var error = process.StandardError.ReadToEnd(); if (process.ExitCode != 0 || error.Length != 0) throw new Exception("Child execution failed"); return output; }
        private static void Stop(Process process)
        { if (process == null) return; try { if (!process.HasExited) { process.Kill(); process.WaitForExit(5000); } } finally { process.Dispose(); } }
        private static int ChildMain(string[] args)
        {
            var root = args[1]; var id = args[2];
            if (args[0] == "writer") {
                var expected = VaultDailyFile.Read(root).Stamp; File.WriteAllText(Path.Combine(root, id + ".ready"), "ready");
                Await(delegate { return File.Exists(Path.Combine(root, "go")); });
                var result = VaultDailyStore.CompareExchange(root, expected, Bound(data: Data(id)));
                Console.WriteLine(result.Status); return result.Status == VaultDailyCommitStatus.Success || result.Status == VaultDailyCommitStatus.Conflict ? 0 : 2;
            }
            using (var gate = Gate()) {
                gate.WaitOne(); File.WriteAllText(Path.Combine(root, "holder.ready"), "ready");
                Await(delegate { return File.Exists(Path.Combine(root, "release")); });
                if (args[0] == "abandon") Environment.Exit(0);
                gate.ReleaseMutex(); return 0;
            }
        }
        private static void CrossProcess()
        {
            using (var f = new Fixture()) {
                var first = Save(f, Read(f).Stamp, Bound()); Process a = null, b = null;
                try {
                    a = Child("writer", f, "first"); b = Child("writer", f, "second");
                    Await(delegate { return File.Exists(Path.Combine(f.Root, "first.ready")) && File.Exists(Path.Combine(f.Root, "second.ready")); }); File.WriteAllText(Path.Combine(f.Root, "go"), "go");
                    var outputs = new[] { Finish(a), Finish(b) };
                    Check(outputs.Count(v => v == "Success") == 1 && outputs.Count(v => v == "Conflict") == 1, "two real writers accept exactly one stale token");
                    var after = Read(f); Check(after.Record.Revision == first.Record.Revision + 1 && after.Record.Generation == first.Record.Generation, "concurrent writers publish one complete revision");
                    Check(after.Record.State.FakeData.entries[0].notes == "first" || after.Record.State.FakeData.entries[0].notes == "second", "concurrent winner dataset is complete");
                } finally { Stop(a); Stop(b); }
            }
            using (var f = new Fixture()) using (var other = new Fixture()) using (var keepGate = Gate()) {
                var expected = Read(other).Stamp; Process holder = null;
                try {
                    holder = Child("hold", f); Await(delegate { return File.Exists(Path.Combine(f.Root, "holder.ready")); });
                    var watch = Stopwatch.StartNew(); Failed(Save(other, expected, Bound()), VaultDailyCommitStatus.Busy, "global SID gate across directories");
                    Check(watch.ElapsedMilliseconds >= 900 && watch.ElapsedMilliseconds < 4000 && !File.Exists(other.PathName), "busy wait is bounded and publishes nothing");
                    File.WriteAllText(Path.Combine(f.Root, "release"), "release"); Finish(holder);
                } finally { Stop(holder); }
                File.Delete(Path.Combine(f.Root, "holder.ready")); File.Delete(Path.Combine(f.Root, "release"));
                try {
                    holder = Child("abandon", f); Await(delegate { return File.Exists(Path.Combine(f.Root, "holder.ready")); });
                    var fresh = Save(other, expected, Bound()); // Still held; only the later abandoned acquisition may publish.
                    Failed(fresh, VaultDailyCommitStatus.Busy, "owner is alive before abandonment");
                    File.WriteAllText(Path.Combine(f.Root, "release"), "release"); Finish(holder);
                    Success(other, Save(other, expected, Bound()), "abandoned owner permits fresh read and commit");
                } finally { Stop(holder); }
            }
        }
        private static void ExistingGateSecurity()
        {
            using (var f = new Fixture()) using (var broad = Gate(true)) {
                var expected = Read(f).Stamp;
                Failed(Save(f, expected, Bound()), VaultDailyCommitStatus.FileUnavailable, "precreated broad writer ACL rejected");
                Check(!File.Exists(f.PathName) && Directory.GetFiles(f.Root).Length == 0, "unsafe gate publishes no files");
                var acl = broad.GetAccessControl();
                Check(acl.GetAccessRules(true, true, typeof(SecurityIdentifier)).Cast<MutexAccessRule>().Any(rule =>
                    new SecurityIdentifier(WellKnownSidType.WorldSid, null).Equals(rule.IdentityReference)), "unsafe existing gate ACL is not mutated");
            }
        }
        private static int Main(string[] args)
        {
            try {
                if (args.Length == 3) return ChildMain(args);
                InitialAndEdits(); StaleAndScope(); DateResetAndExhaustion(); Failures(); NativePublishFailures(); CrossProcess(); ExistingGateSecurity();
                Check(AppPaths.Accesses == 0 && SafeLog.Accesses == 0, "guarded owner paths and logs remain untouched");
                Console.WriteLine("PASS: VaultDailyStoreTests (" + passed + " checks)"); return 0;
            } catch (Exception) { Console.Error.WriteLine("FAIL: Vault daily store test failed."); return 1; }
        }
    }
}

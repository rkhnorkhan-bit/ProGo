using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web.Script.Serialization;

namespace ProGo
{
    internal static class AppPaths
    {
        internal static int Accesses;
        public static string VaultPath { get { Accesses++; throw new InvalidOperationException("Path guard"); } }
        public static void EnsureDirectories() { Accesses++; throw new InvalidOperationException("Path guard"); }
    }
    internal static class SafeLog
    {
        internal static int Accesses;
        public static void Info(string message) { Accesses++; throw new InvalidOperationException("Log guard"); }
    }
    internal static class VaultOpenServiceTests
    {
        private static int passed;
        private static readonly DateTime Day = new DateTime(2026, 10, 12, 12, 0, 0);
        private static readonly CancellationToken Token = CancellationToken.None;
        private static void Check(bool value, string name)
        { if (!value) { Console.Error.WriteLine("CHECK-FAIL: " + name); throw new Exception("Open assertion failed"); } passed++; Console.WriteLine("PASS: " + name); }
        private sealed class Fixture : IDisposable
        {
            internal readonly string Root = Path.Combine(Path.GetTempPath(), "ProGo-Open-" + Guid.NewGuid().ToString("N"));
            internal string Real { get { return Path.Combine(Root, "vault.enc.json"); } }
            internal string Daily { get { return Path.Combine(Root, VaultDailyFile.FileName); } }
            internal Fixture(bool create = true)
            { Directory.CreateDirectory(Root); if (create) Check(VaultOpenService.Create(Real, "1234", Token).Status == VaultOpenStatus.Success, "owned real fixture created"); }
            public void Dispose() { Directory.Delete(Root, true); }
        }
        private static VaultOpenResult Open(Fixture fixture, string pin = "4321", Func<DateTime> clock = null,
            Func<VaultData> factory = null, CancellationToken? token = null)
        { return VaultOpenService.Open(fixture.Real, fixture.Root, pin, clock ?? (() => Day), factory ?? (() => VaultFakeDataFactory.Create(Day)), token ?? Token); }
        private static void Failure(VaultOpenResult result, VaultOpenStatus status, string name)
        {
            Check(result.Status == status, name + " status");
            Check(result.Session == null && !String.IsNullOrEmpty(result.ErrorMessage) &&
                !result.ErrorMessage.Contains("суточ") && !result.ErrorMessage.Contains("ложн"), name + " publishes no session and ordinary static message");
        }
        private static void Success(VaultOpenResult result, bool real, string name)
        { Check(result.Status == VaultOpenStatus.Success && result.Session != null && result.Session.CanPersist == real && result.ErrorMessage == null, name); }
        private static int Main()
        {
            try {
                ProbeAndCreate(); RealPriorityAndErrors(); BindingAndPersistence(); Cancellation(); Calendar(); Conflicts(); RecoveryAndBusy(); Generation();
                Check(AppPaths.Accesses == 0 && SafeLog.Accesses == 0, "opening never accesses global paths or logs");
                Console.WriteLine("Vault open service tests PASS: " + passed); return 0;
            } catch (Exception ex) { Console.Error.WriteLine("FAIL: vault open service " + ex.GetType().Name + " passed=" + passed); return 1; }
        }
        private static void ProbeAndCreate()
        {
            using (var f = new Fixture(false)) {
                var absent = VaultOpenService.Probe(f.Real, Token);
                Check(absent.Status == VaultProbeStatus.CreateRequired && absent.ErrorMessage == null && Directory.GetFiles(f.Root).Length == 0,
                    "exact missing child permits creation without writing");
                Failure(VaultOpenService.Create(f.Real, "bad", Token), VaultOpenStatus.InvalidPin, "invalid create PIN");
                Success(VaultOpenService.Create(f.Real, "1234", Token), true, "exclusive explicit create");
                var bytes = File.ReadAllBytes(f.Real);
                Check(VaultOpenService.Probe(f.Real, Token).Status == VaultProbeStatus.Ready, "ordinary existing real file probes ready");
                Failure(VaultOpenService.Create(f.Real, "5678", Token), VaultOpenStatus.FileUnavailable, "duplicate create");
                Check(bytes.SequenceEqual(File.ReadAllBytes(f.Real)), "duplicate creation preserves exact real ciphertext");
                foreach (var path in new[] { "relative\\vault.enc.json", f.Real + ":stream", Path.Combine(f.Root, "absent", "vault.enc.json"), f.Root })
                    Check(VaultOpenService.Probe(path, Token).Status == VaultProbeStatus.FileUnavailable, "invalid path or parent is never Missing");
                using (var c = new CancellationTokenSource()) {
                    c.Cancel(); Check(VaultOpenService.Probe(f.Real, c.Token).Status == VaultProbeStatus.FileUnavailable, "cancelled probe publishes no creation route");
                    Failure(VaultOpenService.Create(f.Real, "1234", c.Token), VaultOpenStatus.Cancelled, "cancelled create");
                }
            }
        }
        private static void RealPriorityAndErrors()
        {
            using (var f = new Fixture()) {
                var original = File.ReadAllBytes(f.Real); int factories = 0;
                Func<VaultData> forbidden = delegate { factories++; throw new Exception("fixture-private-error"); };
                var success = VaultOpenService.Open(f.Real, "invalid auxiliary root", "1234", null, forbidden, Token);
                Success(success, true, "correct PIN bypasses unavailable auxiliary root and clock");
                Check(success.Session.StoragePath == f.Real && success.Session.Persistence == null && factories == 0,
                    "real session keeps explicit storage ownership without factory or lease");
                File.WriteAllBytes(f.Daily + ".recovery", new byte[] { 1 });
                Success(Open(f, "1234", null, forbidden), true, "correct PIN wins over blocked recovery evidence");
                Failure(Open(f, "4321", null, forbidden), VaultOpenStatus.InvalidData, "mismatch cannot bypass invalid recovery evidence");
                Check(factories == 0 && original.SequenceEqual(File.ReadAllBytes(f.Real)), "blocked auxiliary routes never generate or modify real data");
                File.Delete(f.Daily + ".recovery"); File.WriteAllText(f.Real, "not-json");
                Failure(Open(f, "4321", null, forbidden), VaultOpenStatus.InvalidData, "malformed real ciphertext");
                Check(!File.Exists(f.Daily) && factories == 0, "malformed real data consumes no fake binding");
                var json = new JavaScriptSerializer(); var fields = json.Deserialize<Dictionary<string, object>>(System.Text.Encoding.UTF8.GetString(original));
                fields["iterations"] = 1; File.WriteAllText(f.Real, json.Serialize(fields));
                Failure(Open(f, "4321", null, forbidden), VaultOpenStatus.UnsupportedFormat, "unsupported real KDF");
                File.WriteAllBytes(f.Real, original);
                using (var locked = new FileStream(f.Real, FileMode.Open, FileAccess.Read, FileShare.None))
                    Failure(Open(f, "4321", null, forbidden), VaultOpenStatus.FileUnavailable, "actual real read lock");
                File.Delete(f.Real); Failure(Open(f, "4321", null, forbidden), VaultOpenStatus.FileUnavailable, "missing real file");
                Check(factories == 0 && !File.Exists(f.Daily), "all nonauthentication failures leave daily binding absent");
            }
        }
        private static void BindingAndPersistence()
        {
            using (var f = new Fixture()) {
                var original = File.ReadAllBytes(f.Real); int generated = 0;
                Func<VaultData> factory = delegate { generated++; return VaultFakeDataFactory.Create(Day); };
                Failure(Open(f, "12x4", null, factory), VaultOpenStatus.InvalidPin, "malformed open PIN");
                Check(generated == 0 && !File.Exists(f.Daily), "invalid PIN consumes no binding");
                var first = Open(f, "4321", null, factory); Success(first, false, "first mismatch opens only committed binding");
                var initial = VaultDailyFile.Read(f.Root);
                Check(initial.Record.State.FakePin == "4321" && first.Session.Persistence != null && first.Session.StoragePath == null,
                    "fake session owns a daily lease and never the real path");
                var entries = first.Session.Data.entries.Select(e => e.Clone()).ToList(); entries[0].notes = "accepted edit";
                var candidate = first.Session.CopyWithData(new VaultData { version = 1, entries = entries });
                VaultService.Save(candidate);
                var repeat = Open(f, "4321", null, factory); Success(repeat, false, "new opening reloads same accepted binding");
                Check(repeat.Session.Data.entries[0].notes == "accepted edit" && repeat.Session.Data.entries[0].id == first.Session.Data.entries[0].id &&
                    !Object.ReferenceEquals(repeat.Session.Data, first.Session.Data) && generated == 1, "restart-style read preserves committed identities and edits without regeneration");
                var wrong = Open(f, "5678", null, factory); Failure(wrong, VaultOpenStatus.WrongPin, "second distinct mismatch");
                Check(wrong.ErrorMessage == "Неверный пин-код" && generated == 1, "other mismatches show the exact approved error");
                Success(Open(f, "1234", null, factory), true, "real PIN still opens after binding");
                var current = VaultDailyFile.Read(f.Root);
                Check(current.Record.State.FakePin == "4321" && current.Record.State.FakeData.entries[0].notes == "accepted edit" &&
                    original.SequenceEqual(File.ReadAllBytes(f.Real)), "fake writes and real open preserve real bytes and daily binding");
                VaultService.Save(repeat.Session.CopyWithData(repeat.Session.Data)); current = VaultDailyFile.Read(f.Root);
                bool rejected = false;
                try { VaultService.Save(first.Session.CopyWithData(VaultData.Empty())); } catch (IOException) { rejected = true; }
                Check(rejected && VaultDailyFile.Read(f.Root).Stamp.Matches(current.Stamp), "stale session CAS refuses to overwrite a newer accepted edit");
                VaultService.Save(repeat.Session.CopyWithData(VaultData.Empty()));
                var empty = Open(f, "4321", null, factory); Success(empty, false, "deleting all records preserves the bound PIN");
                Check(empty.Session.Data.entries.Count == 0 && generated == 1, "empty bound dataset is not regenerated");
            }
        }
        private static void Cancellation()
        {
            using (var f = new Fixture())
            using (var c = new CancellationTokenSource()) {
                c.Cancel(); Failure(Open(f, "4321", null, null, c.Token), VaultOpenStatus.Cancelled, "pre-cancelled opening");
                Check(!File.Exists(f.Daily), "pre-cancellation creates no binding");
            }
            using (var f = new Fixture())
            using (var c = new CancellationTokenSource()) {
                var result = Open(f, "4321", null, delegate { c.Cancel(); return VaultFakeDataFactory.Create(Day); }, c.Token);
                Failure(result, VaultOpenStatus.Cancelled, "cancellation after generation");
                Check(!File.Exists(f.Daily), "cancelled provisional factory result is not committed");
            }
            using (var f = new Fixture())
            using (var c = new CancellationTokenSource()) {
                Func<DateTime> clock = delegate {
                    var read = VaultDailyFile.Read(f.Root);
                    if (read.Status == VaultDailyFileReadStatus.Success && read.Record.State.HasFake) c.Cancel();
                    return Day;
                };
                Failure(Open(f, "4321", clock, null, c.Token), VaultOpenStatus.Cancelled, "cancellation after durable commit");
                Check(VaultDailyFile.Read(f.Root).Record.State.FakePin == "4321", "late cancellation retains the accepted binding instead of blind rollback");
                Success(Open(f, "4321"), false, "accepted binding can be reopened after cancellation");
            }
        }
        private static void Calendar()
        {
            using (var f = new Fixture()) {
                var first = Open(f); var id = first.Session.Data.entries[0].id;
                var tomorrow = Open(f, "5678", () => Day.AddDays(1), () => VaultFakeDataFactory.Create(Day.AddDays(1)));
                Success(tomorrow, false, "forward day change clears and rebinds");
                Check(tomorrow.Session.Data.entries[0].id != id && VaultDailyFile.Read(f.Root).Record.State.FakePin == "5678",
                    "new day produces new independent records");
                var backwards = Open(f, "9999", () => Day, () => VaultFakeDataFactory.Create(Day));
                Success(backwards, false, "backward date change also rotates durable state");
                Check(backwards.Session.Data.entries[0].id != id && VaultDailyFile.Read(f.Root).Record.State.FakePin == "9999",
                    "returning to an old date never resurrects previous binding");
                Check(backwards.Session.IsCurrent(Day.AddHours(11).AddMinutes(59).AddSeconds(59)) && !backwards.Session.IsCurrent(Day.AddDays(1)),
                    "lease accepts last second of its day and rejects next calendar day");
            }
            using (var f = new Fixture()) {
                int generated = 0; DateTime now = Day;
                Func<DateTime> clock = delegate {
                    var read = VaultDailyFile.Read(f.Root);
                    if (read.Status == VaultDailyFileReadStatus.Success && read.Record.State.HasFake && read.Record.State.LocalDay == Day.Date)
                        now = Day.AddDays(1);
                    return now;
                };
                var result = Open(f, "4321", clock, delegate { generated++; return VaultFakeDataFactory.Create(now); });
                Success(result, false, "clock change observed after commit is rotated before adoption");
                Check(generated == 2 && VaultDailyFile.Read(f.Root).Record.State.LocalDay == Day.AddDays(1).Date &&
                    result.Session.IsCurrent(Day.AddDays(1)), "publication belongs to the newly accepted day");
            }
            using (var f = new Fixture()) {
                int calls = 0;
                Failure(Open(f, "4321", () => calls++ % 2 == 0 ? Day : Day.AddDays(1)), VaultOpenStatus.DateChanged, "continually changing date is bounded");
                Check(calls <= 6 && !File.Exists(f.Daily), "date retries publish no stale binding and stop after three attempts");
                Failure(Open(f, "4321", () => DateTime.SpecifyKind(Day, DateTimeKind.Utc)), VaultOpenStatus.FileUnavailable, "UTC is not a local calendar input");
            }
            using (var f = new Fixture()) {
                var now = Day; var result = Open(f, "4321", () => now); var lease = result.Session.Persistence;
                now = Day.AddDays(1); bool failed = false;
                try { lease.Save(VaultData.Empty(), "4321"); } catch (IOException ex) { failed = ex.InnerException == null && !ex.Message.Contains("суточ"); }
                Check(failed && !VaultDailyFile.Read(f.Root).Record.State.HasFake, "expired save revokes lease and commits the empty next-day baseline");
                now = Day; Check(!lease.IsCurrent(now), "clock reversal does not revive an explicitly revoked lease");
            }
            using (var f = new Fixture()) {
                bool savePhase = false;
                Func<DateTime> clock = delegate {
                    var read = VaultDailyFile.Read(f.Root);
                    return savePhase && read.Status == VaultDailyFileReadStatus.Success && read.Record.Revision > 0 ? Day.AddDays(1) : Day;
                };
                var opened = Open(f, "4321", clock); savePhase = true; bool rejected = false;
                try { VaultService.Save(opened.Session.CopyWithData(VaultData.Empty())); } catch (IOException) { rejected = true; }
                var final = VaultDailyFile.Read(f.Root);
                Check(rejected && !final.Record.State.HasFake && final.Record.State.LocalDay == Day.AddDays(1).Date,
                    "date crossing after a successful save commits expiry instead of publishing an old-day edit");
                Check(!opened.Session.IsCurrent(Day), "postcommit expiry permanently revokes the old lease");
            }
        }
        private static void Conflicts()
        {
            using (var f = new Fixture()) {
                int factories = 0;
                Func<VaultData> raced = delegate {
                    factories++;
                    var current = VaultDailyFile.Read(f.Root);
                    Check(VaultDailyStore.CompareExchange(f.Root, current.Stamp, VaultDailyState.Bind(Day, "5678", VaultFakeDataFactory.Create(Day))).Status == VaultDailyCommitStatus.Success,
                        "controlled competing writer commits before candidate CAS");
                    return VaultFakeDataFactory.Create(Day);
                };
                var result = Open(f, "4321", null, raced);
                Failure(result, VaultOpenStatus.WrongPin, "fresh retry respects a competing PIN binding");
                Check(factories == 1 && VaultDailyFile.Read(f.Root).Record.State.FakePin == "5678", "conflict never replaces the winning PIN or regenerates after binding");
            }
            using (var f = new Fixture()) {
                File.WriteAllBytes(f.Daily, new byte[] { 1 }); int calls = 0;
                Failure(Open(f, "4321", null, delegate { calls++; return VaultFakeDataFactory.Create(Day); }), VaultOpenStatus.InvalidData, "corrupt auxiliary ciphertext");
                Check(calls == 0 && File.ReadAllBytes(f.Daily).SequenceEqual(new byte[] { 1 }), "corrupt auxiliary data is never replaced by a new binding");
            }
        }
        private static void Generation()
        {
            var first = VaultFakeDataFactory.Create(Day); var second = VaultFakeDataFactory.Create(Day);
            Check(VaultDailyPolicy.IsValidData(first) && first.entries.Count == 3 && second.entries.Count == 3, "factory returns a small complete valid dataset");
            Check(!Object.ReferenceEquals(first, second) && !Object.ReferenceEquals(first.entries, second.entries) &&
                first.entries.All(e => second.entries.All(other => !Object.ReferenceEquals(e, other) && e.id != other.id && e.secret != other.secret)),
                "new generation owns fresh IDs secrets lists and entry objects");
            Check(first.entries.All(e => !e.secret.StartsWith("sample-") && !e.created_at.StartsWith("1970")), "generation has no legacy sample markers or fixed epoch");
            using (var f = new Fixture()) {
                Failure(Open(f, "4321", null, () => null), VaultOpenStatus.FileUnavailable, "null factory output");
                Failure(Open(f, "4321", null, delegate { throw new Exception("private-fixture-value"); }), VaultOpenStatus.FileUnavailable, "throwing factory");
                Check(!File.Exists(f.Daily), "failed generation consumes no binding");
            }
        }
        private static void RecoveryAndBusy()
        {
            using (var f = new Fixture()) {
                var first = Open(f); var oldId = first.Session.Data.entries[0].id; var read = VaultDailyFile.Read(f.Root);
                var changed = VaultDailyState.Bind(Day, "4321", VaultFakeDataFactory.Create(Day));
                var interrupted = VaultDailyStore.CompareExchangeForTest(f.Root, read.Stamp, changed,
                    delegate(VaultDailyWritePhase phase) { if (phase == VaultDailyWritePhase.JournalPublished) throw new IOException("owned interruption"); });
                Check(interrupted.Status == VaultDailyCommitStatus.FileUnavailable && File.Exists(f.Daily + ".recovery"), "owned transaction interruption leaves verified recovery evidence");
                var reopened = Open(f); Success(reopened, false, "opening explicitly recovers before adopting persisted data");
                Check(reopened.Session.Data.entries[0].id == oldId && !File.Exists(f.Daily + ".recovery"), "recovery preserves the intact previous binding and completes owned cleanup");
                using (var locked = new FileStream(f.Daily, FileMode.Open, FileAccess.Read, FileShare.None))
                    Failure(Open(f), VaultOpenStatus.FileUnavailable, "actual auxiliary read lock refuses opening");
                using (var ready = new ManualResetEvent(false))
                using (var release = new ManualResetEvent(false)) {
                    var worker = new Thread(delegate() {
                        using (var gate = VaultDailyStore.CreateGate()) {
                            gate.WaitOne(); ready.Set(); release.WaitOne(5000); gate.ReleaseMutex();
                        }
                    });
                    worker.IsBackground = true; worker.Start();
                    try {
                        Check(ready.WaitOne(5000), "owned worker holds the shared gate");
                        Success(Open(f, "1234"), true, "correct PIN is independent of a busy auxiliary gate");
                        Failure(Open(f), VaultOpenStatus.Busy, "fake opening reports the actual busy gate");
                    } finally { release.Set(); Check(worker.Join(5000), "owned gate worker is settled"); }
                }
            }
        }
    }
}

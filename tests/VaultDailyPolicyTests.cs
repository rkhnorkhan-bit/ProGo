using System;
using System.Collections.Generic;

namespace ProGo
{
    // This independent harness compiles only Vault.cs, the policy and these guards.
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

    internal static class VaultDailyPolicyTests
    {
        private static int passed;
        private static readonly DateTime Day = new DateTime(2026, 10, 11, 12, 0, 0, DateTimeKind.Unspecified);
        private const string UnicodePin = "\u0661\u0662\u0663\u0664";
        private static VaultReadResult Mismatch { get { return VaultReadResult.Failed(VaultReadStatus.AuthenticationMismatch); } }
        private static VaultData Data(string id)
        { return new VaultData { version = 1, entries = new List<VaultEntry> { new VaultEntry { id = id, notes = "initial" } } }; }
        private static void Check(bool condition, string name)
        { if (!condition) throw new Exception(name); passed++; Console.WriteLine("PASS: " + name); }
        private static VaultDailyState Bound(DateTime date, string pin, VaultData data)
        {
            var pending = VaultDailyPolicy.Evaluate(VaultDailyState.Empty(date), date, pin, Mismatch, delegate { return data; });
            Check(pending.RequiresFakeCommit && !pending.CanOpen, "new binding is provisional");
            // Simulate a successful future atomic store; the policy itself never performs this adoption.
            return pending.PendingFakeState;
        }
        private static void Rejected(VaultDailyTransition result, string name)
        {
            Check(result.Outcome == VaultDailyOutcome.Rejected && !result.CanOpen && !result.RequiresFakeCommit &&
                result.CandidateData == null && result.PendingFakeState == null && !String.IsNullOrEmpty(result.ErrorMessage), name);
        }
        private static int Main()
        {
            try {
                Provisional(); SameDay(); PinFormat(); CalendarExpiry(); FailedGeneration(); Invariants();
                Check(AppPaths.Accesses == 0 && SafeLog.Accesses == 0, "policy never accesses application paths or logs");
                Console.WriteLine("Vault daily policy tests PASS: " + passed); return 0;
            } catch (Exception ex) {
                Console.WriteLine("FAIL: vault daily policy: " + ex.GetType().Name); return 1;
            }
        }

        private static void Provisional()
        {
            var empty = VaultDailyState.Empty(Day); var fake = Data("first"); int calls = 0;
            var result = VaultDailyPolicy.Evaluate(empty, Day, "1234", Mismatch, delegate { calls++; return fake; });
            Check(result.RequiresFakeCommit && !result.CanOpen && result.ErrorMessage == null, "candidate cannot open before commit");
            Check(Object.ReferenceEquals(result.StateAfterExpiry, empty) && !empty.HasFake && empty.FakePin == null,
                "evaluation leaves caller state unbound");
            Check(result.PendingFakeState.HasFake && result.PendingFakeState.FakePin == "1234" &&
                Object.ReferenceEquals(result.PendingFakeState.FakeData, fake) && Object.ReferenceEquals(result.CandidateData, fake) && calls == 1,
                "first accepted mismatch proposes one dataset and PIN");
            var other = Data("abandoned");
            var retry = VaultDailyPolicy.Evaluate(result.StateAfterExpiry, Day, "5678", Mismatch, delegate { calls++; return other; });
            Check(retry.PendingFakeState.FakePin == "5678" && Object.ReferenceEquals(retry.CandidateData, other) && calls == 2,
                "abandoned proposal does not consume first mismatch");
            Check(fake.entries[0].notes == "initial" && fake.entries.Count == 1, "proposal leaves supplied data untouched");
        }

        private static void SameDay()
        {
            var fake = Data("editable"); var state = Bound(Day, "1234", fake); int calls = 0;
            Func<VaultData> unused = delegate { calls++; return Data("unexpected"); };
            fake.entries[0].notes = "edited"; fake.entries.Add(new VaultEntry { id = "added" });
            var repeat = VaultDailyPolicy.Evaluate(state, Day.AddHours(3), "1234", Mismatch, unused);
            Check(repeat.CanOpen && repeat.Outcome == VaultDailyOutcome.FakeOpened && !repeat.RequiresFakeCommit &&
                Object.ReferenceEquals(repeat.CandidateData, fake) && repeat.CandidateData.entries[0].notes == "edited" &&
                repeat.CandidateData.entries.Count == 2 && Object.ReferenceEquals(repeat.StateAfterExpiry, state) && calls == 0,
                "repeat uses exact edited fake identity without generation");
            var wrong = VaultDailyPolicy.Evaluate(state, Day, "5678", Mismatch, unused);
            Rejected(wrong, "another mismatch has no dataset or pending state");
            Check(wrong.ErrorMessage == "Неверный пин-код" && Object.ReferenceEquals(wrong.StateAfterExpiry, state) && calls == 0,
                "another mismatch has exact approved error and preserves binding");
            var real = Data("real");
            foreach (string pin in new[] { "1234", "5678", UnicodePin }) {
                var opened = VaultDailyPolicy.Evaluate(state, Day, pin, VaultReadResult.Succeeded(real), unused);
                Check(opened.Outcome == VaultDailyOutcome.RealOpened && opened.CanOpen && Object.ReferenceEquals(opened.CandidateData, real) &&
                    Object.ReferenceEquals(opened.StateAfterExpiry, state) && opened.PendingFakeState == null && opened.ErrorMessage == null,
                    "real success always wins and preserves same-day fake");
            }
            var afterReal = VaultDailyPolicy.Evaluate(state, Day, "1234", Mismatch, unused);
            Check(Object.ReferenceEquals(afterReal.CandidateData, fake) && state.FakePin == "1234" && calls == 0,
                "real opening does not reset or regenerate fake binding");
            foreach (var status in new[] { VaultReadStatus.FileUnavailable, VaultReadStatus.InvalidData, VaultReadStatus.UnsupportedFormat, (VaultReadStatus)999 }) {
                var error = VaultDailyPolicy.Evaluate(state, Day, "1234", VaultReadResult.Failed(status), unused);
                Rejected(error, "reader failure never opens fake");
                Check(Object.ReferenceEquals(error.StateAfterExpiry, state) && calls == 0 && fake.entries.Count == 2,
                    "reader failure preserves caller state and edits");
                var unbound = VaultDailyPolicy.Evaluate(VaultDailyState.Empty(Day), Day, "1234", VaultReadResult.Failed(status), unused);
                Rejected(unbound, "reader failure cannot establish first fake");
                Check(!unbound.StateAfterExpiry.HasFake && calls == 0, "reader failure never invokes factory");
            }
        }

        private static void PinFormat()
        {
            int calls = 0; var empty = VaultDailyState.Empty(Day);
            foreach (string pin in new[] { null, "", "123", "12345", "12a4", "12 4", "1\u00b23\u2074", "\uD835\uDFD9\uD835\uDFDA" }) {
                var malformed = VaultDailyPolicy.Evaluate(empty, Day, pin, Mismatch, delegate { calls++; return Data("unused"); });
                Rejected(malformed, "malformed PIN rejected");
                Check(malformed.ErrorMessage == "PIN-код должен содержать ровно 4 цифры." &&
                    Object.ReferenceEquals(malformed.StateAfterExpiry, empty) && calls == 0, "malformed PIN does not consume daily binding");
            }
            foreach (string pin in new[] { UnicodePin, "\uFF11\uFF12\uFF13\uFF14", "1\u06623\uFF14" }) {
                var accepted = VaultDailyPolicy.Evaluate(empty, Day, pin, Mismatch, delegate { calls++; return Data("unicode"); });
                Check(accepted.RequiresFakeCommit && accepted.PendingFakeState.FakePin == pin, "legacy Unicode digits remain exact strings");
            }
            var state = Bound(Day, UnicodePin, Data("arabic"));
            var lookalike = VaultDailyPolicy.Evaluate(state, Day, "1234", Mismatch, delegate { calls++; return Data("unused"); });
            Check(lookalike.ErrorMessage == "Неверный пин-код", "digit lookalikes are not normalized");
            var same = VaultDailyPolicy.Evaluate(state, Day, UnicodePin, Mismatch, null);
            Check(same.CanOpen && Object.ReferenceEquals(same.CandidateData, state.FakeData), "same Unicode PIN matches ordinally");
        }

        private static void CalendarExpiry()
        {
            var fake = Data("old-day"); var state = Bound(Day, "1234", fake); int calls = 0;
            var lastSecond = Day.Date.AddDays(1).AddSeconds(-1);
            var lastTick = Day.Date.AddDays(1).AddTicks(-1);
            foreach (var instant in new[] { lastSecond, lastTick, DateTime.SpecifyKind(lastTick, DateTimeKind.Local) }) {
                var repeat = VaultDailyPolicy.Evaluate(state, instant, "1234", Mismatch, delegate { calls++; return Data("unused"); });
                Check(repeat.CanOpen && Object.ReferenceEquals(repeat.CandidateData, fake) && calls == 0, "fake remains valid through final day tick");
            }
            var fresh = Data("next-day"); var next = Day.Date.AddDays(1);
            var reset = VaultDailyPolicy.Evaluate(state, next, "5678", Mismatch, delegate { calls++; return fresh; });
            Check(!reset.StateAfterExpiry.HasFake && reset.StateAfterExpiry.LocalDay == next && reset.RequiresFakeCommit && calls == 1 &&
                Object.ReferenceEquals(reset.CandidateData, fresh) && reset.PendingFakeState.FakePin == "5678", "midnight expires before first new mismatch");
            Check(state.HasFake && Object.ReferenceEquals(state.FakeData, fake) && fake.entries[0].notes == "initial", "expiry never mutates previous data or state");
            var real = Data("real-expiry");
            foreach (int direction in new[] { -1, 1 }) {
                var changed = Day.AddDays(direction);
                var attempts = new[] {
                    VaultDailyPolicy.Evaluate(state, changed, "bad", Mismatch, null),
                    VaultDailyPolicy.Evaluate(state, changed, "9999", VaultReadResult.Succeeded(real), null),
                    VaultDailyPolicy.Evaluate(state, changed, "1234", VaultReadResult.Failed(VaultReadStatus.FileUnavailable), null),
                    VaultDailyPolicy.Evaluate(state, changed, "1234", Mismatch, delegate { throw new InvalidOperationException("private factory marker"); })
                };
                foreach (var attempt in attempts) {
                    Check(!attempt.StateAfterExpiry.HasFake && attempt.StateAfterExpiry.FakePin == null &&
                        attempt.StateAfterExpiry.LocalDay == changed.Date && attempt.PendingFakeState == null &&
                        attempt.StateAfterExpiry.FakeData == null,
                        "every changed-day route returns mandatory expired baseline");
                    var regenerated = Data("clock-return");
                    var rollback = VaultDailyPolicy.Evaluate(attempt.StateAfterExpiry, Day, "1234", Mismatch, delegate { return regenerated; });
                    Check(rollback.RequiresFakeCommit && Object.ReferenceEquals(rollback.CandidateData, regenerated) &&
                        !Object.ReferenceEquals(rollback.CandidateData, fake), "adopted expiry prevents resurrection when clock returns");
                }
            }
        }

        private static void FailedGeneration()
        {
            var expired = Data("expired"); var state = Bound(Day, "1234", expired); var next = Day.AddDays(1); var real = Data("real-owned");
            var invalid = new[] { null, new VaultData { version = 2, entries = new List<VaultEntry>() },
                new VaultData { version = 1 }, new VaultData { version = 1, entries = new List<VaultEntry> { null } },
                expired, real, new VaultData { version = 1, entries = expired.entries },
                new VaultData { version = 1, entries = new List<VaultEntry> { real.entries[0] } }, Data(null), Data(""),
                new VaultData { version = 1, entries = new List<VaultEntry> { new VaultEntry { id = "duplicate" }, new VaultEntry { id = "duplicate" } } } };
            foreach (var data in invalid) {
                int calls = 0;
                var failed = VaultDailyPolicy.Evaluate(state, next, "5678", Mismatch, delegate { calls++; return data; }, real);
                Rejected(failed, "invalid or aliased factory dataset rejected safely");
                Check(!failed.StateAfterExpiry.HasFake && failed.StateAfterExpiry.LocalDay == next.Date && calls == 1 &&
                    failed.ErrorMessage == "Не удалось подготовить хранилище.", "failed generation recognizes expiry without binding");
                var retry = VaultDailyPolicy.Evaluate(failed.StateAfterExpiry, next, "9999", Mismatch, delegate { return Data("retry"); }, real);
                Check(retry.RequiresFakeCommit && retry.PendingFakeState.FakePin == "9999", "failed generation leaves first fake attempt available");
            }
            var noFactory = VaultDailyPolicy.Evaluate(state, next, "1234", Mismatch, null, real);
            Rejected(noFactory, "missing factory is safe failure");
            var thrown = VaultDailyPolicy.Evaluate(state, next, "1234", Mismatch, delegate { throw new Exception("private path PIN payload"); }, real);
            Rejected(thrown, "factory exception is safe failure");
            Check(thrown.ErrorMessage == noFactory.ErrorMessage && !thrown.ErrorMessage.Contains("private") &&
                state.HasFake && expired.entries[0].notes == "initial" && real.entries[0].notes == "initial", "factory failures expose no payload and mutate no data");
            var empty = VaultDailyState.Empty(Day);
            var sameDayFailure = VaultDailyPolicy.Evaluate(empty, Day, "1234", Mismatch, delegate { throw new Exception("private"); });
            Check(Object.ReferenceEquals(sameDayFailure.StateAfterExpiry, empty) && !empty.HasFake,
                "same-day factory failure preserves unbound caller state");
            Check(VaultDailyPolicy.Evaluate(sameDayFailure.StateAfterExpiry, Day, "5678", Mismatch, delegate { return Data("fresh"); }).RequiresFakeCommit,
                "factory exception never consumes first daily mismatch");
            var bound = Bound(Day, "1234", Data("bound"));
            var alias = VaultDailyPolicy.Evaluate(bound, Day, "1234", Mismatch, null, bound.FakeData);
            Rejected(alias, "caller-held real dataset cannot be returned as fake");
            bound.FakeData.entries = null;
            var mutated = VaultDailyPolicy.Evaluate(bound, Day, "1234", Mismatch, delegate { return Data("unexpected"); });
            Rejected(mutated, "invalid edited fake shape cannot open or rebind");
            Check(Object.ReferenceEquals(mutated.StateAfterExpiry, bound), "invalid edited fake preserves state metadata");
        }

        private static void Invariants()
        {
            var empty = VaultDailyState.Empty(Day); int calls = 0;
            foreach (var bad in new[] { new VaultData { version = 2, entries = new List<VaultEntry>() },
                new VaultData { version = 1 }, new VaultData { version = 1, entries = new List<VaultEntry> { null } }, Data(null), Data(""),
                new VaultData { version = 1, entries = new List<VaultEntry> { new VaultEntry { id = "duplicate" }, new VaultEntry { id = "duplicate" } } } }) {
                var rejected = VaultDailyPolicy.Evaluate(empty, Day, "1234", VaultReadResult.Succeeded(bad), delegate { calls++; return Data("unused"); });
                Rejected(rejected, "real success requires valid dataset shape");
                Check(Object.ReferenceEquals(rejected.StateAfterExpiry, empty) && calls == 0, "invalid real data never falls back to fake");
            }
            var emptyData = VaultData.Empty();
            var emptyReal = VaultDailyPolicy.Evaluate(empty, Day, "1234", VaultReadResult.Succeeded(emptyData), null);
            Check(emptyReal.CanOpen && emptyReal.Outcome == VaultDailyOutcome.RealOpened && Object.ReferenceEquals(emptyReal.CandidateData, emptyData),
                "valid real empty entries remain allowed");
            var ordinalIds = new VaultData { version = 1, entries = new List<VaultEntry> {
                new VaultEntry { id = "entry" }, new VaultEntry { id = "ENTRY" }, new VaultEntry { id = " " } } };
            Check(VaultDailyPolicy.Evaluate(empty, Day, "1234", Mismatch, delegate { return ordinalIds; }).RequiresFakeCommit,
                "entry IDs use ordinal uniqueness without whitespace normalization");
            Rejected(VaultDailyPolicy.Evaluate(empty, Day, "1234", null, null), "missing read result fails safely");
            bool utcRejected = false, nullRejected = false, bindRejected = false;
            try { VaultDailyPolicy.Evaluate(empty, DateTime.SpecifyKind(Day, DateTimeKind.Utc), "1234", Mismatch, null); }
            catch (ArgumentException ex) { utcRejected = ex.Message.Contains("Local calendar time") && !ex.Message.Contains("1234"); }
            try { VaultDailyPolicy.Evaluate(null, Day, "1234", Mismatch, null); }
            catch (ArgumentNullException) { nullRejected = true; }
            try { VaultDailyState.Bind(Day, "bad", Data("unused")); }
            catch (ArgumentException ex) { bindRejected = ex.Message == "Invalid daily vault state."; }
            Check(utcRejected && nullRejected && bindRejected && calls == 0, "invalid contract inputs fail without PIN details or factory invocation");
            bool utcStateRejected = false;
            try { VaultDailyState.Empty(DateTime.SpecifyKind(Day, DateTimeKind.Utc)); }
            catch (ArgumentException) { utcStateRejected = true; }
            Check(utcStateRejected && empty.LocalDay.Kind == DateTimeKind.Unspecified && empty.LocalDay == Day.Date,
                "state uses explicit normalized local calendar date without OS lookup");
        }
    }
}

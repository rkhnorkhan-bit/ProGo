using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
        public static void Info(string value) { Accesses++; throw new InvalidOperationException("Vault log guard"); }
    }
    internal static class VaultDailyExpiryTests
    {
        private static readonly DateTime A = new DateTime(2026, 10, 13, 12, 0, 0, DateTimeKind.Unspecified);
        private static readonly DateTime B = A.AddDays(1), C = A.AddDays(2);
        private static int passed;
        private static string phase = "startup";
        private static void Check(bool condition, string name)
        { if (!condition) throw new Exception("Owned expiration assertion."); passed++; Console.WriteLine("PASS: " + name); }
        private sealed class Fixture : IDisposable
        {
            internal readonly string Root = Path.Combine(Path.GetTempPath(), "ProGo-DailyExpiry-" + Guid.NewGuid().ToString("N"));
            internal string Target { get { return Path.Combine(Root, VaultDailyFile.FileName); } }
            internal Fixture() { Directory.CreateDirectory(Root); }
            public void Dispose() { if (Directory.Exists(Root)) Directory.Delete(Root, true); }
        }
        private static VaultDailyState Bound(DateTime day, string pin = "4321", bool empty = false)
        {
            var data = VaultData.Empty();
            if (!empty) { var entry = VaultEntry.New(); entry.name = "Синтетическая запись"; entry.secret = "synthetic-owned-expiry-value"; data.entries.Add(entry); }
            return VaultDailyState.Bind(day, pin, data);
        }
        private static VaultDailyCommitResult Commit(Fixture f, VaultDailyState state)
        {
            var read = VaultDailyFile.Read(f.Root); var committed = VaultDailyStore.CompareExchange(f.Root, read.Stamp, state);
            Check(committed.Status == VaultDailyCommitStatus.Success, "owned fixture commits an actual protected record"); return committed;
        }
        private static Dictionary<string, byte[]> Snapshot(Fixture f)
        { return Directory.GetFiles(f.Root).ToDictionary(Path.GetFileName, File.ReadAllBytes); }
        private static void Same(Fixture f, Dictionary<string, byte[]> before, string name)
        {
            var after = Snapshot(f);
            Check(before.Count == after.Count && before.All(p => after.ContainsKey(p.Key) && p.Value.SequenceEqual(after[p.Key])), name);
        }
        private static void Failed(VaultDailyExpiryResult result, VaultDailyExpiryStatus status, string name)
        {
            Check(result.Status == status, name + " status");
            Check(result.Stamp == null && !result.HasFakeBinding && !String.IsNullOrEmpty(result.ErrorMessage) &&
                !result.ErrorMessage.Contains("суточ") && !result.ErrorMessage.Contains("ложн") && !result.ErrorMessage.Contains("4321"),
                name + " publishes only a static ordinary error");
        }
        private static void EmptyAccepted(Fixture f, VaultDailyExpiryResult result, DateTime day, Guid oldGeneration, string name)
        {
            var read = VaultDailyFile.Read(f.Root);
            Check(result.Status == VaultDailyExpiryStatus.Expired && result.Stamp != null && !result.HasFakeBinding && result.ErrorMessage == null,
                name + " safe expiration result");
            Check(read.Status == VaultDailyFileReadStatus.Success && read.Stamp.Matches(result.Stamp) &&
                read.Record.State.LocalDay == day.Date && !read.Record.State.HasFake && read.Record.State.FakePin == null &&
                read.Record.State.FakeData == null && read.Record.Generation != oldGeneration,
                name + " fresh protected tombstone has no previous PIN or dataset");
        }
        private static void CleanAndCurrent()
        {
            using (var f = new Fixture()) {
                File.WriteAllBytes(f.Target + ".new-" + Guid.NewGuid().ToString("N"), new byte[] { 4, 5, 6 });
                var before = Snapshot(f); var result = VaultDailyExpiry.Run(f.Root, () => A, CancellationToken.None);
                Check(result.Status == VaultDailyExpiryStatus.Missing && result.Stamp.IsMissing && !result.HasFakeBinding,
                    "missing startup keeps a missing stamp without creating a target");
                Same(f, before, "missing startup leaves unknown pre-journal artifacts alone");
            }
            foreach (int mode in new[] { 0, 1, 2 }) using (var f = new Fixture()) {
                var initial = Commit(f, mode == 0 ? VaultDailyState.Empty(A) : Bound(A, empty: mode == 2));
                var before = Snapshot(f); var result = VaultDailyExpiry.Run(f.Root, () => A, CancellationToken.None);
                Check(result.Status == VaultDailyExpiryStatus.Unchanged && result.Stamp.Matches(initial.Stamp) && result.HasFakeBinding == (mode != 0),
                    "current-day empty, populated and bound-zero datasets keep their exact accepted stamp");
                Same(f, before, "current-day observation does not rewrite ciphertext or generate data");
            }
            Check(typeof(VaultDailyExpiryResult).GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .All(p => p.PropertyType != typeof(VaultData) && p.PropertyType != typeof(VaultDailyRecord)),
                "retained expiry results expose metadata without a dataset or record");
        }
        private static void CalendarTransitions()
        {
            foreach (var next in new[] { B, A.AddDays(-1) }) using (var f = new Fixture()) {
                var old = Commit(f, Bound(A)); var result = VaultDailyExpiry.Run(f.Root, () => next, CancellationToken.None);
                EmptyAccepted(f, result, next, old.Stamp.Generation, "forward or backward local calendar transition");
                Check(result.Stamp.Revision == 0, "calendar transition starts a new generation at revision zero");
                var again = VaultDailyExpiry.Run(f.Root, () => next, CancellationToken.None);
                Check(again.Status == VaultDailyExpiryStatus.Unchanged && again.Stamp.Matches(result.Stamp), "repeated startup keeps the completed tombstone");
                var stale = VaultDailyStore.CompareExchange(f.Root, old.Stamp, Bound(A));
                Check(stale.Status == VaultDailyCommitStatus.Conflict && !VaultDailyFile.Read(f.Root).Record.State.HasFake,
                    "late old-generation CRUD cannot resurrect the expired binding");
            }
        }
        private static void ObservedAbaAndCompetitors()
        {
            using (var f = new Fixture()) {
                var old = Commit(f, Bound(A));
                var token = new VaultDailyExpiryInvalidation(old.Stamp.Generation, A, B);
                var advanced = Commit(f, Bound(A));
                Check(advanced.Stamp.Generation == old.Stamp.Generation && advanced.Stamp.Revision > old.Stamp.Revision,
                    "actual late CRUD advances revision inside the same original binding generation");
                EmptyAccepted(f, VaultDailyExpiry.Run(f.Root, () => A, CancellationToken.None, token), A, old.Stamp.Generation,
                    "latched A-to-B-to-A expiration revokes the original generation even after late CRUD");
            }
            using (var f = new Fixture()) {
                var old = Commit(f, Bound(A)); int reads = 0;
                EmptyAccepted(f, VaultDailyExpiry.Run(f.Root, () => ++reads == 1 ? B : A, CancellationToken.None), A, old.Stamp.Generation,
                    "first fresh-read clock observation remains latched when the next clock returns to A");
            }
            using (var f = new Fixture()) {
                var old = Commit(f, Bound(A)); var token = new VaultDailyExpiryInvalidation(old.Stamp.Generation, A, B);
                Commit(f, VaultDailyState.Empty(B)); Commit(f, VaultDailyState.Empty(A)); var competitor = Commit(f, Bound(A, "2222", true));
                var before = Snapshot(f); var result = VaultDailyExpiry.Run(f.Root, () => A, CancellationToken.None, token);
                Check(result.Status == VaultDailyExpiryStatus.Unchanged && result.HasFakeBinding && result.Stamp.Matches(competitor.Stamp),
                    "stale invalidation cannot erase a new current-day competitor including its bound-zero dataset");
                Same(f, before, "new competitor ciphertext stays exact under an unrelated invalidation");
                int reads = 0;
                EmptyAccepted(f, VaultDailyExpiry.Run(f.Root, () => ++reads == 1 ? B : A, CancellationToken.None, token), A, competitor.Stamp.Generation,
                    "stale external invalidation cannot suppress a fresh observed calendar expiration of the new generation");
            }
            using (var f = new Fixture()) {
                var old = Commit(f, Bound(A)); var token = new VaultDailyExpiryInvalidation(old.Stamp.Generation, A, B); int hooks = 0;
                VaultDailyFileStamp competitor = null;
                var result = VaultDailyExpiry.RunForTest(f.Root, () => A, CancellationToken.None, token, delegate {
                    if (++hooks != 1) return;
                    Commit(f, VaultDailyState.Empty(B)); Commit(f, VaultDailyState.Empty(A)); competitor = Commit(f, Bound(A, "2222")).Stamp;
                });
                Check(hooks == 1 && result.Status == VaultDailyExpiryStatus.Unchanged && result.HasFakeBinding && result.Stamp.Matches(competitor),
                    "CAS conflict rereads and preserves a genuinely new current-day competitor");
                Check(VaultDailyFile.Read(f.Root).Record.State.FakePin == "2222", "the expired old token is never rebound to the competitor PIN");
            }
        }
        private static void HeldPublication(bool cancel)
        {
            using (var f = new Fixture()) using (var source = new CancellationTokenSource())
            using (var entered = new ManualResetEventSlim()) using (var release = new ManualResetEventSlim()) {
                var old = Commit(f, Bound(A)); var before = File.ReadAllBytes(f.Target); int dayOffset = 1, hooks = 0;
                var worker = Task.Run(() => VaultDailyExpiry.RunForTest(f.Root, () => A.AddDays(Volatile.Read(ref dayOffset)), source.Token, beforePublish: delegate {
                    if (Interlocked.Increment(ref hooks) != 1) return;
                    entered.Set(); if (!release.Wait(15000)) throw new TimeoutException("Owned expiration publication deadline.");
                }));
                try {
                    Check(entered.Wait(10000), "actual expiry worker reaches the staged native publication checkpoint");
                    Check(!worker.IsCompleted && VaultDailyFile.Read(f.Root).Stamp.Matches(old.Stamp),
                        "held publication preserves the accepted original target before commit");
                    if (cancel) source.Cancel(); else Volatile.Write(ref dayOffset, 2);
                    release.Set(); Check(worker.Wait(15000), "owned native expiry worker settles after release");
                    if (cancel) {
                        Failed(worker.Result, VaultDailyExpiryStatus.Cancelled, "cancel before native publication");
                        Check(File.ReadAllBytes(f.Target).SequenceEqual(before) && Directory.GetFiles(f.Root).Length == 1,
                            "pre-publication cancellation preserves ciphertext and cleans only its own uncommitted stage");
                    } else EmptyAccepted(f, worker.Result, C, old.Stamp.Generation,
                        "clock change inside staged publication retries to the actual endpoint day");
                } finally { release.Set(); source.Cancel(); if (!worker.Wait(15000)) throw new TimeoutException("Owned expiration cleanup deadline."); }
            }
        }
        private static void RetryAndEndpoints()
        {
            using (var f = new Fixture()) {
                var old = Commit(f, Bound(A)); int hooks = 0;
                var result = VaultDailyExpiry.RunForTest(f.Root, () => B, CancellationToken.None, beforeCompareExchange: delegate {
                    hooks++; Commit(f, Bound(A));
                });
                Failed(result, VaultDailyExpiryStatus.Conflict, "three actual competing CAS writes exhaust the bounded retry");
                var read = VaultDailyFile.Read(f.Root);
                Check(hooks == 3 && read.Record.Generation == old.Stamp.Generation && read.Record.State.HasFake && read.Record.Revision == old.Stamp.Revision + 3,
                    "bounded conflict failure does not clear or overwrite the concurrent original-generation edits");
                Check(result.ObservedInvalidations.Length == 1 && result.ObservedInvalidations[0].FirstDifferentDay == B.Date,
                    "bounded conflict retains its first observed different day as metadata");
                EmptyAccepted(f, VaultDailyExpiry.Run(f.Root, () => A, CancellationToken.None, result.ObservedInvalidations[0]), A,
                    old.Stamp.Generation, "explicit retry after clock returns to A still revokes the failed cleanup generation");
            }
            using (var f = new Fixture()) {
                Commit(f, Bound(A)); var before = Snapshot(f); int clockReads = 0, staged = 0;
                var result = VaultDailyExpiry.RunForTest(f.Root, () => A.AddDays(++clockReads), CancellationToken.None,
                    beforePublish: () => staged++);
                Failed(result, VaultDailyExpiryStatus.DateChanged, "continuously moving endpoint clock");
                Check(staged == 3, "calendar drift performs at most three actual staged CAS attempts");
                Same(f, before, "unstable endpoint publishes no stale-day success or target replacement");
            }
            using (var f = new Fixture()) {
                var empty = Commit(f, VaultDailyState.Empty(B)); int reads = 0, hooks = 0;
                var result = VaultDailyExpiry.RunForTest(f.Root, () => ++reads <= 2 ? A : B, CancellationToken.None,
                    beforeCompareExchange: () => hooks++);
                Check(result.Status == VaultDailyExpiryStatus.Unchanged && result.Stamp.Matches(empty.Stamp) && reads >= 4 && hooks == 1,
                    "return after the pre-CAS clock changes checks two stable endpoints without an unnecessary empty rewrite");
            }
            HeldPublication(false); HeldPublication(true);
        }
        private static void RetainedFailureObservations()
        {
            using (var f = new Fixture()) using (var source = new CancellationTokenSource()) {
                var old = Commit(f, Bound(A)); var before = Snapshot(f); int reads = 0;
                var result = VaultDailyExpiry.RunForTest(f.Root, () => ++reads == 1 ? B : A, source.Token,
                    beforeCompareExchange: () => source.Cancel());
                Failed(result, VaultDailyExpiryStatus.Cancelled, "cancellation after observing a different local day");
                var observed = result.ObservedInvalidations;
                Check(observed.Length == 1 && observed[0].Generation == old.Stamp.Generation &&
                    observed[0].LocalDay == A.Date && observed[0].FirstDifferentDay == B.Date,
                    "cancelled worker retains immutable first-B metadata instead of losing its observed expiration");
                var retained = observed[0]; observed[0] = null;
                Check(result.ObservedInvalidations[0] == retained, "caller cannot mutate a retained worker observation array");
                Same(f, before, "cancelled observation does not pretend the target was already expired");
                EmptyAccepted(f, VaultDailyExpiry.Run(f.Root, () => A, CancellationToken.None, retained), A,
                    old.Stamp.Generation, "runtime-style retry cannot revive cancelled first-B expiration at clock A");
            }
            using (var f = new Fixture()) using (var held = VaultDailyStore.CreateGate())
            using (var entered = new ManualResetEventSlim()) using (var release = new ManualResetEventSlim()) {
                var old = Commit(f, Bound(A)); var before = Snapshot(f); Task holder = null;
                VaultDailyExpiryResult result;
                try {
                    result = VaultDailyExpiry.RunForTest(f.Root, () => B, CancellationToken.None, beforeCompareExchange: delegate {
                        holder = Task.Run(delegate {
                            held.WaitOne(); try {
                                entered.Set(); if (!release.Wait(15000)) throw new TimeoutException();
                            } finally { held.ReleaseMutex(); }
                        });
                        if (!entered.Wait(10000)) throw new TimeoutException();
                    });
                    Failed(result, VaultDailyExpiryStatus.Busy, "writer becomes busy after first-B calendar observation");
                    Check(result.ObservedInvalidations.Length == 1 && result.ObservedInvalidations[0].FirstDifferentDay == B.Date,
                        "native writer busy failure keeps first-B metadata for a later clock-A retry");
                    Same(f, before, "late busy writer leaves the original binding bytes unchanged");
                } finally { release.Set(); if (holder != null && !holder.Wait(15000)) throw new TimeoutException(); }
                EmptyAccepted(f, VaultDailyExpiry.Run(f.Root, () => A, CancellationToken.None, result.ObservedInvalidations[0]), A,
                    old.Stamp.Generation, "retry of native Busy at clock A does not forget observed expiration B");
            }
        }
        private static void RecoveryAccessAndCancellation()
        {
            using (var f = new Fixture()) using (var source = new CancellationTokenSource()) {
                var old = Commit(f, Bound(A));
                var interrupted = VaultDailyStore.CompareExchangeForTest(f.Root, old.Stamp, VaultDailyState.Empty(B), phase => {
                    if (phase == VaultDailyWritePhase.JournalPublished) throw new IOException("Owned interruption.");
                });
                Check(interrupted.Status == VaultDailyCommitStatus.FileUnavailable && File.Exists(f.Target + ".recovery"),
                    "actual writer interruption leaves its protected recovery proof before target replacement");
                var before = Snapshot(f); source.Cancel();
                Failed(VaultDailyExpiry.Run(f.Root, () => B, source.Token), VaultDailyExpiryStatus.Cancelled, "already-cancelled startup recovery");
                Same(f, before, "pre-cancelled recovery preserves every journal stage backup and target byte");
                EmptyAccepted(f, VaultDailyExpiry.Run(f.Root, () => B, CancellationToken.None), B, old.Stamp.Generation,
                    "startup resolves the known interrupted transaction before calendar expiration");
                Check(!File.Exists(f.Target + ".recovery") && Directory.GetFiles(f.Root).Length == 1,
                    "successful recovery and expiry remove only their proven transaction artifacts");
            }
            using (var f = new Fixture()) {
                var old = Commit(f, Bound(A)); var oldBytes = File.ReadAllBytes(f.Target);
                using (var locked = File.Open(f.Target, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    Failed(VaultDailyExpiry.Run(f.Root, () => B, CancellationToken.None), VaultDailyExpiryStatus.FileUnavailable,
                        "actual sharing lock rejects native expiry replacement");
                    Check(File.ReadAllBytes(f.Target).SequenceEqual(oldBytes) && File.Exists(f.Target + ".recovery"),
                        "failed native replacement retains the original target and recovery proofs");
                }
                EmptyAccepted(f, VaultDailyExpiry.Run(f.Root, () => B, CancellationToken.None), B, old.Stamp.Generation,
                    "retry after native sharing lock release recovers and expires the original binding");
            }
            using (var f = new Fixture()) using (var source = new CancellationTokenSource()) {
                var old = Commit(f, Bound(A));
                var result = VaultDailyExpiry.Run(f.Root, delegate {
                    var read = VaultDailyFile.Read(f.Root);
                    if (read.Status == VaultDailyFileReadStatus.Success && read.Record.State.LocalDay == B.Date) source.Cancel();
                    return B;
                }, source.Token);
                Failed(result, VaultDailyExpiryStatus.Cancelled, "cancellation after actual durable expiration");
                Check(!VaultDailyFile.Read(f.Root).Record.State.HasFake && VaultDailyFile.Read(f.Root).Record.Generation != old.Stamp.Generation,
                    "late cancellation never rolls back a confirmed tombstone or resurrects its PIN");
            }
        }
        private static void UnknownProofAndClockKinds()
        {
            foreach (bool unsupported in new[] { false, true }) using (var f = new Fixture()) {
                Commit(f, Bound(A));
                if (unsupported) { var bytes = File.ReadAllBytes(f.Target); bytes[8] = 2; File.WriteAllBytes(f.Target, bytes); }
                else File.WriteAllBytes(f.Target + ".recovery", new byte[] { 1, 2, 3 });
                File.WriteAllBytes(f.Target + ".previous-" + Guid.NewGuid().ToString("N"), new byte[] { 9, 8, 7 });
                var before = Snapshot(f);
                Failed(VaultDailyExpiry.Run(f.Root, () => B, CancellationToken.None), unsupported ? VaultDailyExpiryStatus.UnsupportedFormat : VaultDailyExpiryStatus.InvalidData,
                    "unknown recovery evidence or unsupported record");
                Same(f, before, "failed expiry does not delete unknown proofs or replace inaccessible data");
            }
            using (var f = new Fixture()) {
                Commit(f, Bound(A)); var before = Snapshot(f);
                Failed(VaultDailyExpiry.Run(f.Root, () => DateTime.SpecifyKind(B, DateTimeKind.Utc), CancellationToken.None),
                    VaultDailyExpiryStatus.InvalidData, "UTC is not an accepted local calendar");
                Same(f, before, "invalid local clock does not expire or rewrite the existing binding");
                bool rejected = false;
                try { new VaultDailyExpiryInvalidation(Guid.Empty, A, B); } catch (ArgumentException) { rejected = true; }
                Check(rejected, "invalidation requires an actual accepted generation"); rejected = false;
                try { new VaultDailyExpiryInvalidation(Guid.NewGuid(), A, A); } catch (ArgumentException) { rejected = true; }
                Check(rejected, "invalidation requires a first actually different day");
            }
            using (var f = new Fixture()) using (var held = VaultDailyStore.CreateGate())
            using (var entered = new ManualResetEventSlim()) using (var release = new ManualResetEventSlim()) {
                Commit(f, Bound(A)); var before = Snapshot(f);
                var owner = Task.Run(delegate {
                    held.WaitOne(); try { entered.Set(); if (!release.Wait(15000)) throw new TimeoutException(); } finally { held.ReleaseMutex(); }
                });
                try {
                    Check(entered.Wait(10000), "owned background holder acquires the actual shared writer gate");
                    var watch = Stopwatch.StartNew(); var result = VaultDailyExpiry.Run(f.Root, () => B, CancellationToken.None); watch.Stop();
                    Failed(result, VaultDailyExpiryStatus.Busy, "actual held shared writer gate");
                    Check(watch.ElapsedMilliseconds >= 900 && watch.ElapsedMilliseconds < 4000, "startup recovery gate deadline remains bounded");
                    Same(f, before, "busy startup recovery mutates no durable files");
                } finally { release.Set(); if (!owner.Wait(15000)) throw new TimeoutException(); }
            }
        }
        private static int Main()
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") {
                Console.WriteLine("SKIP: daily expiry fixtures require isolated native Windows CI"); return 0;
            }
            try {
                phase = "clean/current"; CleanAndCurrent(); phase = "calendar transitions"; CalendarTransitions();
                phase = "observed ABA and competitor"; ObservedAbaAndCompetitors(); phase = "retry/clock endpoints"; RetryAndEndpoints();
                phase = "retained failed observation"; RetainedFailureObservations();
                phase = "recovery/access/cancellation"; RecoveryAccessAndCancellation(); phase = "unknown proof/clock/gate"; UnknownProofAndClockKinds();
                Check(AppPaths.Accesses == 0 && SafeLog.Accesses == 0, "expiry worker touches no application paths or logs");
                Console.WriteLine("PASS: VaultDailyExpiryTests (" + passed + " checks)"); return 0;
            } catch (Exception ex) {
                Console.Error.WriteLine("FAIL: VaultDailyExpiryTests phase=" + phase + " type=" + ex.GetType().Name + " passed=" + passed); return 1;
            }
        }
    }
}

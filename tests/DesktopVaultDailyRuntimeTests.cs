using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        private static readonly DateTime VaultRuntimeDay = new DateTime(2026, 10, 13, 12, 0, 0, DateTimeKind.Unspecified);
        private static void VaultDailyRuntimeWorkflow()
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") {
                Console.WriteLine("SKIP: daily runtime ownership fixtures require isolated native Windows CI"); return;
            }
            var root = Path.Combine(work, "vault-daily-runtime-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try {
                VaultRuntimeMissing(root); VaultRuntimeClosedBinding(root); VaultRuntimeOpeningFence(root);
                VaultRuntimeFailureRetention(root); VaultRuntimeCallerCancellation(root); VaultRuntimeNewGeneration(root);
                VaultRuntimeStaleOwnerDate(root);
                VaultRuntimeRetryBackoff(root);
                VaultRuntimeMaintenanceDrain(root, false); VaultRuntimeMaintenanceDrain(root, true);
                VaultRuntimeClosedSaveObservation(root);
                VaultRuntimeContextSettlement(root, false); VaultRuntimeContextSettlement(root, true);
                VaultRuntimeContextCancelledShutdown(root); VaultRuntimeContextSingleDateSnapshot(root);
                VaultRuntimeContextFailedSeal(root); VaultRuntimeContextUnknownSidecar(root);
            } finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }
        private static string VaultRuntimeRoot(string root, string name)
        { var value = Path.Combine(root, name); Directory.CreateDirectory(value); return value; }
        private static VaultDailyFileStamp VaultRuntimeBind(string root, DateTime day, string pin = "4321")
        {
            var read = VaultDailyFile.Read(root);
            var saved = VaultDailyStore.CompareExchange(root, read.Stamp, VaultDailyState.Bind(day, pin, VaultDurableSeed()));
            Check(saved.Status == VaultDailyCommitStatus.Success, "owned runtime fixture binds a production DPAPI daily record");
            return saved.Stamp;
        }
        private static VaultDailyExpiryResult VaultRuntimePrepare(VaultDailyRuntime runtime, CancellationToken token = default(CancellationToken))
        {
            var pending = runtime.PrepareFake(token); PumpAsyncRestore(() => pending.IsCompleted, 15000);
            return pending.GetAwaiter().GetResult();
        }
        private static void VaultRuntimeStop(VaultDailyRuntime runtime, ManualResetEventSlim release = null)
        {
            runtime.Close(); if (release != null) release.Set();
            var pending = runtime.Work; if (pending != null) WaitAsyncRestoreWithoutPump(pending, 15000);
        }
        private static void VaultRuntimeMissing(string root)
        {
            var owned = VaultRuntimeRoot(root, "missing"); int owner = Thread.CurrentThread.ManagedThreadId, ticks = 0, wrongThread = 0;
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var pulse = new System.Windows.Forms.Timer { Interval = 10 }) {
                var runtime = new VaultDailyRuntime(() => {
                    if (Thread.CurrentThread.ManagedThreadId == owner) Interlocked.Increment(ref wrongThread); return owned;
                }, () => {
                    if (Thread.CurrentThread.ManagedThreadId == owner) Interlocked.Increment(ref wrongThread); return VaultRuntimeDay;
                }, token => { entered.Set(); VaultSaveHeld(release); });
                try {
                    pulse.Tick += delegate { ticks++; }; pulse.Start(); runtime.Start(); var pending = runtime.Work;
                    PumpAsyncRestore(() => entered.IsSet && ticks >= 3);
                    Check(pending != null && !pending.IsCompleted && ReferenceEquals(pending, runtime.Work) && wrongThread == 0,
                        "daily startup owns a separate raw worker while the owner heartbeat remains responsive");
                    release.Set(); var result = VaultRuntimePrepare(runtime);
                    Check(result.Status == VaultDailyExpiryStatus.Missing && result.Stamp.IsMissing && !result.HasFakeBinding && wrongThread == 0 &&
                        !File.Exists(Path.Combine(owned, VaultDailyFile.FileName)),
                        "missing startup cleanup runs root and clock only off the owner thread without creating a daily binding");
                    Check(!typeof(VaultDailyRuntime).GetFields(PrivateInstance).Any(f => f.FieldType == typeof(VaultSession) || f.FieldType == typeof(VaultData)),
                        "the long-lived daily coordinator retains metadata without a session or dataset field");
                } finally { pulse.Stop(); VaultRuntimeStop(runtime, release); }
            }
        }
        private static void VaultRuntimeClosedBinding(string root)
        {
            var owned = VaultRuntimeRoot(root, "closed-binding"); var stamp = VaultRuntimeBind(owned, VaultRuntimeDay);
            var path = Path.Combine(owned, VaultDailyFile.FileName); var before = File.ReadAllBytes(path);
            DateTime now = VaultRuntimeDay; var lease = new VaultDailySessionPersistence(owned, stamp, () => now);
            var runtime = new VaultDailyRuntime(() => owned, () => now);
            try {
                runtime.ObserveDate(now); runtime.RegisterLease(lease);
                Check(VaultRuntimePrepare(runtime).Status == VaultDailyExpiryStatus.Unchanged && File.ReadAllBytes(path).SequenceEqual(before),
                    "same-day startup and explicit fake preparation preserve the complete accepted DPAPI bytes");
                runtime.Pause(); var watch = Stopwatch.StartNew(); now = VaultRuntimeDay.AddDays(1); runtime.ObserveDate(now);
                now = VaultRuntimeDay; runtime.ObserveDate(now); watch.Stop();
                Check(watch.ElapsedMilliseconds < 500 && runtime.IsPaused,
                    "closed-window ABA observation queues identity metadata promptly while maintenance prevents a new cleanup worker");
                Check(VaultRuntimePrepare(runtime).Status == VaultDailyExpiryStatus.Busy && File.ReadAllBytes(path).SequenceEqual(before),
                    "paused fake preparation cannot bypass pending cleanup or mutate the accepted record");
                runtime.Resume(); var expired = VaultRuntimePrepare(runtime); var read = VaultDailyFile.Read(owned);
                Check(expired.Status <= VaultDailyExpiryStatus.Expired && !expired.HasFakeBinding && read.Record.State.LocalDay == VaultRuntimeDay.Date &&
                    !read.Record.State.HasFake && read.Stamp.Generation != stamp.Generation,
                    "resuming a closed-window ABA expires the known old generation irreversibly and normalizes the current local day");
                var fresh = VaultRuntimeBind(owned, VaultRuntimeDay, "2222"); runtime.ObserveDate(now);
                Check(VaultRuntimePrepare(runtime).HasFakeBinding && VaultDailyFile.Read(owned).Stamp.Matches(fresh),
                    "a new current-day binding survives routine preparation after the old ABA generation was expired");
            } finally { VaultRuntimeStop(runtime); }
        }
        private static void VaultRuntimeOpeningFence(string root)
        {
            var owned = VaultRuntimeRoot(root, "opening-fence"); var stamp = VaultRuntimeBind(owned, VaultRuntimeDay);
            DateTime now = VaultRuntimeDay; var runtime = new VaultDailyRuntime(() => owned, () => now);
            try {
                runtime.Pause(); runtime.ObserveDate(now); var capture = runtime.CaptureEpoch();
                now = VaultRuntimeDay.AddDays(1); runtime.ObserveDate(now); now = VaultRuntimeDay; runtime.ObserveDate(now);
                var lease = new VaultDailySessionPersistence(owned, stamp, () => now);
                Check(!runtime.RegisterOpenedLease(lease, capture) && !lease.IsCurrent(now) &&
                    lease.Invalidation != null && lease.Invalidation.FirstDifferentDay == VaultRuntimeDay.AddDays(1).Date,
                    "an opening held across ABA cannot publish its late lease and preserves the first different day");
                runtime.Invalidate(lease, DateTime.SpecifyKind(now, DateTimeKind.Utc));
                Check(lease.Invalidation.FirstDifferentDay == VaultRuntimeDay.AddDays(1).Date,
                    "invalid owner clock revokes access while retaining an existing valid first-day invalidation instead of inventing a UTC day");
                var freshCapture = runtime.CaptureEpoch(); var same = new VaultDailySessionPersistence(owned, stamp, () => now);
                Check(!runtime.RegisterOpenedLease(same, freshCapture) && !same.IsCurrent(now),
                    "capturing a fresh epoch cannot authorize an old generation whose durable invalidation is still pending");
                runtime.Resume(); Check(!VaultRuntimePrepare(runtime).HasFakeBinding,
                    "the opening fence schedules actual durable cleanup before another fake attempt can be admitted");
            } finally { VaultRuntimeStop(runtime); }
        }
        private static void VaultRuntimeFailureRetention(string root)
        {
            var owned = VaultRuntimeRoot(root, "failed-observation"); var stamp = VaultRuntimeBind(owned, VaultRuntimeDay);
            int calls = 0, phase = 0; Func<DateTime> clock = () => {
                if (Volatile.Read(ref phase) == 0) return Interlocked.Increment(ref calls) == 1 ? VaultRuntimeDay.AddDays(1) :
                    DateTime.SpecifyKind(VaultRuntimeDay, DateTimeKind.Utc);
                return VaultRuntimeDay;
            };
            var runtime = new VaultDailyRuntime(() => owned, clock);
            try {
                var failure = VaultRuntimePrepare(runtime);
                Check(failure.Status == VaultDailyExpiryStatus.InvalidData && failure.ObservedInvalidations.Any(i => i.Generation == stamp.Generation &&
                    i.FirstDifferentDay == VaultRuntimeDay.AddDays(1).Date) && VaultDailyFile.Read(owned).Record.State.HasFake,
                    "a worker that observed a different day then rejected its clock exposes durable identity metadata without clearing the binding");
                runtime.ObserveDate(VaultRuntimeDay); var firstTick = runtime.Work;
                PumpAsyncRestore(() => firstTick.IsCompleted);
                var settledTick = runtime.Work; int beforeTicks = calls;
                for (int i = 0; i < 20; i++) runtime.ObserveDate(VaultRuntimeDay);
                Check(ReferenceEquals(settledTick, runtime.Work) && calls == beforeTicks,
                    "a persistent cleanup failure does not trigger filesystem retries on every same-day timer tick");
                Interlocked.Exchange(ref phase, 1); var retry = VaultRuntimePrepare(runtime); var after = VaultDailyFile.Read(owned);
                Check(retry.Status <= VaultDailyExpiryStatus.Expired && !after.Record.State.HasFake && after.Stamp.Generation != stamp.Generation,
                    "an explicit retry at the original day still expires the first observed generation instead of reviving it");
            } finally { VaultRuntimeStop(runtime); }
        }
        private static void VaultRuntimeCallerCancellation(string root)
        {
            var owned = VaultRuntimeRoot(root, "caller-cancel");
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var cancellation = new CancellationTokenSource()) {
                var runtime = new VaultDailyRuntime(() => owned, () => VaultRuntimeDay,
                    token => { entered.Set(); VaultSaveHeld(release); });
                try {
                    var caller = runtime.PrepareFake(cancellation.Token); PumpAsyncRestore(() => entered.IsSet);
                    var raw = runtime.Work; cancellation.Cancel(); PumpAsyncRestore(() => caller.IsCompleted);
                    Check(caller.Result.Status == VaultDailyExpiryStatus.Cancelled && !raw.IsCompleted && ReferenceEquals(raw, runtime.Work),
                        "cancelled fake preparation releases only its waiter while the separately owned cleanup task remains tracked");
                    release.Set(); PumpAsyncRestore(() => raw.IsCompleted);
                    Check(!raw.IsFaulted && VaultRuntimePrepare(runtime).Status == VaultDailyExpiryStatus.Missing,
                        "a cancelled opening does not poison another caller or cancel the shared startup cleanup owner");
                } finally { VaultRuntimeStop(runtime, release); }
            }
        }
        private static void VaultRuntimeNewGeneration(string root)
        {
            var owned = VaultRuntimeRoot(root, "competing-generation"); var old = VaultRuntimeBind(owned, VaultRuntimeDay);
            DateTime now = VaultRuntimeDay; var runtime = new VaultDailyRuntime(() => owned, () => now);
            try {
                runtime.Pause(); runtime.ObserveDate(now); runtime.RegisterLease(new VaultDailySessionPersistence(owned, old, () => now));
                runtime.ObserveDate(VaultRuntimeDay.AddDays(1)); runtime.ObserveDate(VaultRuntimeDay);
                Check(VaultDailyExpiry.Run(owned, () => VaultRuntimeDay.AddDays(1), CancellationToken.None).Status == VaultDailyExpiryStatus.Expired &&
                    VaultDailyExpiry.Run(owned, () => VaultRuntimeDay, CancellationToken.None).Status == VaultDailyExpiryStatus.Expired,
                    "a competing owned writer safely advances the two daily tombstones before making a fresh binding");
                var fresh = VaultRuntimeBind(owned, VaultRuntimeDay, "2222"); var before = File.ReadAllBytes(Path.Combine(owned, VaultDailyFile.FileName));
                runtime.Resume(); var result = VaultRuntimePrepare(runtime);
                Check(result.HasFakeBinding && result.Stamp.Matches(fresh) && File.ReadAllBytes(Path.Combine(owned, VaultDailyFile.FileName)).SequenceEqual(before),
                    "queued invalidation of a stale generation cannot erase the competing accepted current-day generation");
            } finally { VaultRuntimeStop(runtime); }
        }
        private static void VaultRuntimeStaleOwnerDate(string root)
        {
            var owned = VaultRuntimeRoot(root, "stale-owner-date"); var day = VaultRuntimeDay.AddDays(1);
            var stamp = VaultRuntimeBind(owned, day); var before = File.ReadAllBytes(Path.Combine(owned, VaultDailyFile.FileName));
            var runtime = new VaultDailyRuntime(() => owned, () => day);
            try {
                runtime.Pause(); runtime.ObserveDate(VaultRuntimeDay); var captured = runtime.BeginOpening(day);
                var lease = new VaultDailySessionPersistence(owned, stamp, () => day);
                Check(runtime.RegisterOpenedLease(lease, captured) && lease.IsCurrent(day) && lease.Invalidation == null,
                    "a date observed before opening capture cannot invalidate a fresh binding created on the later actual day");
                runtime.Resume(); var prepared = VaultRuntimePrepare(runtime);
                Check(prepared.HasFakeBinding && prepared.Stamp.Matches(stamp) && File.ReadAllBytes(Path.Combine(owned, VaultDailyFile.FileName)).SequenceEqual(before),
                    "safe worker completion does not apply a stale pre-registration owner date to the new generation");
                runtime.ObserveDate(day); Check(VaultRuntimePrepare(runtime).HasFakeBinding,
                    "the next fresh owner date observation preserves the valid current-day generation");
            } finally { VaultRuntimeStop(runtime); }
        }
        private static void VaultRuntimeRetryBackoff(string root)
        {
            var owned = VaultRuntimeRoot(root, "bounded-retry"); var stamp = VaultRuntimeBind(owned, VaultRuntimeDay);
            var path = Path.Combine(owned, VaultDailyFile.FileName); DateTime now = VaultRuntimeDay.AddDays(1); int calls = 0;
            var runtime = new VaultDailyRuntime(() => owned, () => now, token => Interlocked.Increment(ref calls), retryMilliseconds: 1000);
            FileStream denied = null;
            try {
                runtime.Pause(); runtime.ObserveDate(VaultRuntimeDay); runtime.RegisterLease(new VaultDailySessionPersistence(owned, stamp, () => now));
                runtime.ObserveDate(now); denied = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                runtime.Resume(); var failure = VaultRuntimePrepare(runtime); var oldWork = runtime.Work; int oldCalls = calls;
                Check(failure.Status == VaultDailyExpiryStatus.FileUnavailable && oldCalls >= 1,
                    "an actual native exclusive lock leaves durable expiry queued as a retryable cleanup failure");
                runtime.CheckRetry();
                Check(ReferenceEquals(oldWork, runtime.Work) && calls == oldCalls,
                    "same-owner retry polling cannot start another filesystem worker before its monotonic backoff is due");
                denied.Dispose(); denied = null;
                PumpAsyncRestore(() => { runtime.CheckRetry(); return !ReferenceEquals(oldWork, runtime.Work) && runtime.Work.IsCompleted; }, 5000);
                var read = VaultDailyFile.Read(owned); var settled = runtime.Work; int completedCalls = calls;
                Check(completedCalls == oldCalls + 1 && !read.Record.State.HasFake && read.Record.State.LocalDay == now.Date,
                    "one bounded automatic retry expires the preserved binding after its native file lock is released");
                for (int i = 0; i < 20; i++) runtime.CheckRetry();
                Check(ReferenceEquals(settled, runtime.Work) && calls == completedCalls,
                    "healthy cleanup settlement removes the retry schedule instead of repeatedly rescanning the daily file");
            } finally { if (denied != null) denied.Dispose(); VaultRuntimeStop(runtime); }
        }
        private static void VaultRuntimeMaintenanceDrain(string root, bool locked)
        {
            var owned = VaultRuntimeRoot(root, locked ? "maintenance-failed-drain" : "maintenance-safe-drain");
            var stamp = VaultRuntimeBind(owned, VaultRuntimeDay); var path = Path.Combine(owned, VaultDailyFile.FileName);
            var runtime = new VaultDailyRuntime(() => owned, () => VaultRuntimeDay); FileStream denied = null;
            try {
                runtime.Pause(); runtime.ObserveDate(VaultRuntimeDay); runtime.RegisterLease(new VaultDailySessionPersistence(owned, stamp, () => VaultRuntimeDay));
                runtime.ObserveDate(VaultRuntimeDay.AddDays(1)); runtime.ObserveDate(VaultRuntimeDay);
                if (locked) denied = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                var drain = runtime.SealAndDrain(); PumpAsyncRestore(() => drain.IsCompleted, 15000);
                if (locked) {
                    Check(drain.Result.Status == VaultDailyExpiryStatus.FileUnavailable && runtime.HasPendingExpiry && runtime.IsPaused,
                        "failed sealed cleanup retains the accepted ABA obligation so maintenance cannot treat raw settlement as durable expiry");
                    denied.Dispose(); denied = null; runtime.Resume();
                    Check(VaultRuntimePrepare(runtime).Status <= VaultDailyExpiryStatus.Expired && !runtime.HasPendingExpiry,
                        "rollback of a failed maintenance reservation resumes the original pending expiry after its native lock clears");
                } else Check(drain.Result.Status <= VaultDailyExpiryStatus.Expired && !runtime.HasPendingExpiry && runtime.IsPaused,
                    "final maintenance sealing drains the ABA queued while paused before its raw barrier can authorize handoff");
                var read = VaultDailyFile.Read(owned);
                Check(!read.Record.State.HasFake && read.Stamp.Generation != stamp.Generation && read.Record.State.LocalDay == VaultRuntimeDay.Date,
                    "maintenance drain leaves the original-day store durably unbound so a fresh process cannot revive the old generation");
                if (!locked) {
                    var settled = runtime.Work; var before = File.ReadAllBytes(path); runtime.ObserveDate(VaultRuntimeDay.AddDays(1));
                    Check(ReferenceEquals(settled, runtime.Work) && File.ReadAllBytes(path).SequenceEqual(before),
                        "a sealed maintenance owner rejects fresh UI observations after its final accepted obligations have settled");
                }
            } finally { if (denied != null) denied.Dispose(); VaultRuntimeStop(runtime); }
        }
        private static void VaultRuntimeClosedSaveObservation(string root)
        {
            var owned = VaultRuntimeRoot(root, "closed-save-observation"); var stamp = VaultRuntimeBind(owned, VaultRuntimeDay);
            var path = Path.Combine(owned, VaultDailyFile.FileName); int owner = Thread.CurrentThread.ManagedThreadId, held = 0;
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim()) {
                Func<DateTime> leaseClock = () => {
                    if (Thread.CurrentThread.ManagedThreadId != owner && Interlocked.CompareExchange(ref held, 1, 0) == 0) {
                        entered.Set(); VaultSaveHeld(release); return VaultRuntimeDay.AddDays(1);
                    }
                    return VaultRuntimeDay;
                };
                var lease = new VaultDailySessionPersistence(owned, stamp, leaseClock);
                var ownerSession = new VaultSession(VaultDurableSeed(), "4321", false, lease);
                var runtime = new VaultDailyRuntime(() => owned, () => VaultRuntimeDay); FileStream denied = null; Task save = null; bool rejected = false;
                try {
                    runtime.Pause(); runtime.RegisterLease(lease);
                    save = Task.Run(() => { try { lease.Save(VaultDurableSeed(), "4321"); } catch (IOException) { rejected = true; } });
                    PumpAsyncRestore(() => entered.IsSet); ownerSession.DropSensitive();
                    denied = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                    release.Set(); PumpAsyncRestore(() => save.IsCompleted, 15000); denied.Dispose(); denied = null;
                    Check(rejected && ownerSession.IsDiscarded && runtime.HasPendingExpiry && lease.Invalidation != null &&
                        lease.Invalidation.FirstDifferentDay == VaultRuntimeDay.AddDays(1).Date && VaultDailyFile.Read(owned).Record.State.HasFake,
                        "a production save lease publishes its first B to the metadata owner after UI references were dropped and native expiry CAS failed");
                    var drained = runtime.SealAndDrain(); PumpAsyncRestore(() => drained.IsCompleted, 15000); var read = VaultDailyFile.Read(owned);
                    Check(drained.Result.Status <= VaultDailyExpiryStatus.Expired && !runtime.HasPendingExpiry && !read.Record.State.HasFake &&
                        read.Record.State.LocalDay == VaultRuntimeDay.Date && read.Stamp.Generation != stamp.Generation,
                        "final maintenance drain consumes a late closed-save observation even when every later worker clock read has returned to A");
                } finally {
                    if (denied != null) denied.Dispose(); release.Set(); ownerSession.DropSensitive();
                    if (save != null) WaitAsyncRestoreWithoutPump(save, 15000); VaultRuntimeStop(runtime, release);
                }
            }
        }
        private static void VaultRuntimeContextSettlement(string root, bool dispose)
        {
            var owned = VaultRuntimeRoot(root, dispose ? "context-dispose" : "context-shutdown");
            var original = VaultDurableCreate(owned); original.DropSensitive();
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var pulse = new System.Windows.Forms.Timer { Interval = 10 }) {
                int ticks = 0, owner = Thread.CurrentThread.ManagedThreadId, worker = 0;
                CancellationToken observed = default(CancellationToken);
                using (var fixture = new AsyncRestoreContextFixture(root, dailyRootProvider: () => owned,
                    vaultClock: () => VaultRuntimeDay, beforeDailyCleanup: token => {
                        observed = token; worker = Thread.CurrentThread.ManagedThreadId; entered.Set(); VaultSaveHeld(release);
                    })) {
                    pulse.Tick += delegate { ticks++; }; pulse.Start();
                    try {
                        PumpAsyncRestore(() => entered.IsSet && ticks >= 3); var raw = fixture.Context.DailyWork;
                        Check(raw != null && !raw.IsCompleted && worker != owner && fixture.Context.VaultWork == null &&
                            VaultContextAllowed(fixture.Context, "CanOpenVault"),
                            "actual application startup tracks daily cleanup independently while correct-PIN opening remains available");
                        var proper = Task.Run(() => fixture.Context.OpenVaultForWorker(Path.Combine(owned, "vault.enc.json"), owned, "1234", CancellationToken.None));
                        PumpAsyncRestore(() => proper.IsCompleted, 15000);
                        try {
                            Check(proper.Result.Status == VaultOpenStatus.Success && proper.Result.Session.CanPersist && !raw.IsCompleted,
                                "actual correct authentication bypasses the held startup daily worker and its clock-dependent barrier");
                        } finally { if (proper.Result.Session != null) proper.Result.Session.DropSensitive(); }
                        if (dispose) {
                            var watch = Stopwatch.StartNew(); fixture.Context.Dispose(); watch.Stop();
                            Check(watch.ElapsedMilliseconds < 500 && observed.IsCancellationRequested && !raw.IsCompleted &&
                                ReferenceEquals(raw, fixture.Context.DailyWork),
                                "forced application disposal cancels daily ownership promptly and retains its unsettled raw task");
                            release.Set(); WaitAsyncRestoreWithoutPump(raw, 15000);
                            Check(!raw.IsFaulted && fixture.CliCleanup == 0 && fixture.WindowsCleanup == 0 && fixture.InstalledUnchanged() &&
                                AsyncRestoreTreeMatches(fixture.Source, fixture.SourceHashes) && !File.Exists(Path.Combine(owned, VaultDailyFile.FileName)),
                                "ownerless daily cleanup settles without pumping native UI or changing installed data and proxy journals");
                        } else {
                            var watch = Stopwatch.StartNew(); var shutdown = fixture.Context.RequestShutdownAsync();
                            var duplicate = fixture.Context.RequestShutdownAsync(); PumpAsyncRestore(() => shutdown.IsCompleted, 10000); watch.Stop();
                            Check(ReferenceEquals(shutdown, duplicate) && !shutdown.Result && !raw.IsCompleted &&
                                fixture.CliCleanup == 0 && fixture.WindowsCleanup == 0 && watch.ElapsedMilliseconds >= 2800 && watch.ElapsedMilliseconds < 6000 && ticks >= 20,
                                "actual shutdown waits its three-second aggregate barrier and refuses proxy cleanup until startup daily IO settles");
                            release.Set(); PumpAsyncRestore(() => raw.IsCompleted, 15000);
                            var retry = fixture.Context.RequestShutdownAsync(); PumpAsyncRestore(() => retry.IsCompleted, 10000);
                            Check(retry.Result && fixture.CliCleanup == 1 && fixture.WindowsCleanup == 1 && fixture.InstalledUnchanged() &&
                                AsyncRestoreTreeMatches(fixture.Source, fixture.SourceHashes),
                                "explicit shutdown retry starts one proxy cleanup only after the separately owned daily worker settles");
                            fixture.Context.CancelShutdown();
                        }
                    } finally { release.Set(); fixture.Context.Dispose(); if (fixture.Context.DailyWork != null) WaitAsyncRestoreWithoutPump(fixture.Context.DailyWork, 15000); pulse.Stop(); }
                }
            }
        }
        private static void VaultRuntimeContextCancelledShutdown(string root)
        {
            var owned = VaultRuntimeRoot(root, "context-cancelled-shutdown");
            var original = VaultDurableCreate(owned); original.DropSensitive(); DateTime now = VaultRuntimeDay;
            using (var fixture = new AsyncRestoreContextFixture(root, dailyRootProvider: () => owned, vaultClock: () => now)) {
                try {
                    PumpAsyncRestore(() => fixture.Context.DailyWork.IsCompleted, 15000);
                    var shutdown = fixture.Context.RequestShutdownAsync(); PumpAsyncRestore(() => shutdown.IsCompleted, 10000);
                    Check(shutdown.Result && ((VaultDailyRuntime)Field(fixture.Context, "dailyRuntime")).IsPaused,
                        "successful shutdown preparation freezes the separate daily owner before a reversible handoff");
                    fixture.Context.CancelShutdown();
                    PumpAsyncRestore(() => !(bool)Field(fixture.Context, "shutdownPrepared") &&
                        !((VaultDailyRuntime)Field(fixture.Context, "dailyRuntime")).IsPaused, 10000);
                    var opened = Task.Run(() => fixture.Context.OpenVaultForWorker(Path.Combine(owned, "vault.enc.json"), owned, "4321", CancellationToken.None));
                    PumpAsyncRestore(() => opened.IsCompleted, 15000);
                    try { Check(opened.Result.Status == VaultOpenStatus.Success && opened.Result.Session.Persistence != null,
                        "cancelling the prepared shutdown resumes fake opening instead of leaving its cleanup owner permanently paused"); }
                    finally { if (opened.Result.Session != null) opened.Result.Session.DropSensitive(); }
                    now = VaultRuntimeDay.AddDays(1); fixture.Context.ObserveVaultDate();
                    PumpAsyncRestore(() => fixture.Context.DailyWork.IsCompleted, 15000);
                    Check(!VaultDailyFile.Read(owned).Record.State.HasFake && fixture.InstalledUnchanged(),
                        "the resumed actual context observes the next date and durably expires a subsequently accepted binding");
                } finally { fixture.Context.Dispose(); WaitAsyncRestoreWithoutPump(fixture.Context.DailyWork, 15000); }
            }
        }
        private static void VaultRuntimeContextSingleDateSnapshot(string root)
        {
            var owned = VaultRuntimeRoot(root, "context-one-date-snapshot"); var original = VaultDurableCreate(owned); original.DropSensitive();
            int phase = 0, observed = 0; Func<DateTime> clock = () => Volatile.Read(ref phase) == 0 ? VaultRuntimeDay :
                (Interlocked.CompareExchange(ref observed, 1, 0) == 0 ? VaultRuntimeDay.AddDays(1) : VaultRuntimeDay);
            using (var fixture = new AsyncRestoreContextFixture(root, dailyRootProvider: () => owned, vaultClock: clock)) {
                VaultForm form = null; VaultSession session = null;
                try {
                    var opened = Task.Run(() => fixture.Context.OpenVaultForWorker(Path.Combine(owned, "vault.enc.json"), owned, "4321", CancellationToken.None));
                    PumpAsyncRestore(() => opened.IsCompleted, 15000); session = opened.Result.Session;
                    Check(opened.Result.Status == VaultOpenStatus.Success && session != null,
                        "one-date snapshot fixture obtains its accepted lease through the production actual context");
                    PumpAsyncRestore(() => fixture.Context.DailyWork.IsCompleted, 15000);
                    var lease = (VaultDailySessionPersistence)session.Persistence; var generation = lease.Generation;
                    form = new VaultForm(session, fixture.Clipboard, fixture.Settings, clock: () => VaultRuntimeDay,
                        expired: (value, date) => Call(fixture.Context, "OnVaultExpired", value, date));
                    typeof(UpdateAwareTrayApplicationContext).GetField("vaultForm", PrivateInstance).SetValue(fixture.Context, form);
                    typeof(UpdateAwareTrayApplicationContext).GetField("vaultSession", PrivateInstance).SetValue(fixture.Context, session);
                    form.Show(); Application.DoEvents(); Interlocked.Exchange(ref phase, 1); fixture.Context.ObserveVaultDate();
                    Check(session.IsDiscarded && (!form.Visible || form.IsDisposed) && !lease.IsCurrent(VaultRuntimeDay) &&
                        lease.Invalidation != null && lease.Invalidation.FirstDifferentDay == VaultRuntimeDay.AddDays(1).Date,
                        "one actual context clock snapshot closes the active lease on B even when the next clock read has returned to A");
                    PumpAsyncRestore(() => fixture.Context.DailyWork.IsCompleted, 15000); var read = VaultDailyFile.Read(owned);
                    Check(!read.Record.State.HasFake && read.Stamp.Generation != generation && read.Record.State.LocalDay == VaultRuntimeDay.Date,
                        "the actual shared snapshot preserves B for durable ABA cleanup after the UI has already dropped its private session");
                } finally {
                    if (form != null) form.Dispose(); if (session != null) session.DropSensitive();
                    fixture.Context.Dispose(); WaitAsyncRestoreWithoutPump(fixture.Context.DailyWork, 15000);
                }
            }
        }
        private static void VaultRuntimeContextFailedSeal(string root)
        {
            var owned = VaultRuntimeRoot(root, "context-failed-seal"); var original = VaultDurableCreate(owned); original.DropSensitive();
            using (var fixture = new AsyncRestoreContextFixture(root, dailyRootProvider: () => owned, vaultClock: () => VaultRuntimeDay)) {
                FileStream denied = null;
                try {
                    var opened = Task.Run(() => fixture.Context.OpenVaultForWorker(Path.Combine(owned, "vault.enc.json"), owned, "4321", CancellationToken.None));
                    PumpAsyncRestore(() => opened.IsCompleted, 15000); Check(opened.Result.Status == VaultOpenStatus.Success,
                        "failed maintenance seal fixture obtains an actual accepted context lease");
                    opened.Result.Session.DropSensitive(); PumpAsyncRestore(() => fixture.Context.DailyWork.IsCompleted, 15000);
                    var runtime = (VaultDailyRuntime)Field(fixture.Context, "dailyRuntime"); runtime.Pause();
                    runtime.ObserveDate(VaultRuntimeDay.AddDays(1)); runtime.ObserveDate(VaultRuntimeDay);
                    denied = new FileStream(Path.Combine(owned, VaultDailyFile.FileName), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                    var failed = fixture.Context.RequestShutdownAsync(); PumpAsyncRestore(() => failed.IsCompleted, 10000);
                    Check(!failed.Result && runtime.HasPendingExpiry && !runtime.IsPaused && fixture.CliCleanup == 0 && fixture.WindowsCleanup == 0 &&
                        !(bool)Field(fixture.Context, "shutdownPrepared"),
                        "actual application refuses maintenance after failed durable sealing and resumes its preserved expiry owner without proxy cleanup");
                    denied.Dispose(); denied = null;
                    var retry = fixture.Context.RequestShutdownAsync(); PumpAsyncRestore(() => retry.IsCompleted, 10000);
                    Check(retry.Result && !runtime.HasPendingExpiry && !VaultDailyFile.Read(owned).Record.State.HasFake &&
                        fixture.CliCleanup == 1 && fixture.WindowsCleanup == 1 && fixture.InstalledUnchanged(),
                        "retrying actual maintenance drains the queued ABA before exactly one proxy cleanup and native handoff authorization");
                    fixture.Context.CancelShutdown();
                } finally { if (denied != null) denied.Dispose(); fixture.Context.Dispose(); WaitAsyncRestoreWithoutPump(fixture.Context.DailyWork, 15000); }
            }
        }
        private static void VaultRuntimeContextUnknownSidecar(string root)
        {
            var owned = VaultRuntimeRoot(root, "context-unknown-sidecar"); var original = VaultDurableCreate(owned); original.DropSensitive();
            var path = Path.Combine(owned, VaultDailyFile.FileName); var bytes = new byte[] { 0, 1, 2, 3, 4 }; File.WriteAllBytes(path, bytes);
            using (var fixture = new AsyncRestoreContextFixture(root, dailyRootProvider: () => owned, vaultClock: () => VaultRuntimeDay)) {
                try {
                    PumpAsyncRestore(() => fixture.Context.DailyWork.IsCompleted, 15000);
                    var runtime = (VaultDailyRuntime)Field(fixture.Context, "dailyRuntime");
                    var proper = Task.Run(() => fixture.Context.OpenVaultForWorker(Path.Combine(owned, "vault.enc.json"), owned, "1234", CancellationToken.None));
                    PumpAsyncRestore(() => proper.IsCompleted, 15000);
                    try { Check(proper.Result.Status == VaultOpenStatus.Success && !runtime.HasPendingExpiry && File.ReadAllBytes(path).SequenceEqual(bytes),
                        "unknown damaged sidecar does not prevent actual correct authentication or invent an expiry obligation"); }
                    finally { if (proper.Result.Session != null) proper.Result.Session.DropSensitive(); }
                    var shutdown = fixture.Context.RequestShutdownAsync(); PumpAsyncRestore(() => shutdown.IsCompleted, 10000);
                    Check(shutdown.Result && fixture.CliCleanup == 1 && fixture.WindowsCleanup == 1 && File.ReadAllBytes(path).SequenceEqual(bytes),
                        "real-only maintenance can proceed past unknown auxiliary damage while preserving every undecoded proof byte");
                    fixture.Context.CancelShutdown();
                } finally { fixture.Context.Dispose(); WaitAsyncRestoreWithoutPump(fixture.Context.DailyWork, 15000); }
            }
        }
    }
}

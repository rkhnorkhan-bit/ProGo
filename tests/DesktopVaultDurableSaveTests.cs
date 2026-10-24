using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        private static readonly DateTime VaultDurableDay = new DateTime(2026, 10, 13, 12, 0, 0, DateTimeKind.Unspecified);

        private static void VaultDurableSaveWorkflow()
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") {
                Console.WriteLine("SKIP: durable vault save fixtures require isolated native Windows CI"); return;
            }
            var root = Path.Combine(work, "vault-save-owned-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            bool previousCrossThread = Control.CheckForIllegalCrossThreadCalls;
            try {
                Control.CheckForIllegalCrossThreadCalls = true;
                VaultDurableCrud(root);
                VaultDurableRejectedWrites(root);
                VaultDurableShutdownWaits(root);
                VaultDurableDisposedOwner(root);
            } finally {
                Control.CheckForIllegalCrossThreadCalls = previousCrossThread;
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        private static VaultData VaultDurableSeed()
        {
            var data = VaultData.Empty(); var entry = VaultEntry.New();
            entry.name = "Исходная синтетическая запись"; entry.secret = "synthetic-owned-vault-secret";
            data.entries.Add(entry); return data;
        }

        private static string VaultDurableJson(VaultData data)
        { return new JavaScriptSerializer().Serialize(data); }

        private static VaultSession VaultDurableCreate(string root)
        {
            Directory.CreateDirectory(root);
            var created = VaultOpenService.Create(Path.Combine(root, "vault.enc.json"), "1234", CancellationToken.None);
            Check(created.Status == VaultOpenStatus.Success && created.Session != null,
                "owned durable fixture creates a real vault through the production explicit-path service");
            var session = created.Session;
            var seeded = session.CopyWithData(VaultDurableSeed());
            try { VaultService.Save(seeded); session.Data.entries = seeded.Data.entries; }
            finally { seeded.DropSensitive(); }
            return session;
        }

        private static VaultSession VaultDurableOpen(string root, string pin, Func<DateTime> clock = null)
        {
            var opened = VaultOpenService.Open(Path.Combine(root, "vault.enc.json"), root, pin,
                clock ?? (() => VaultDurableDay), VaultDurableSeed, CancellationToken.None);
            Check(opened.Status == VaultOpenStatus.Success && opened.Session != null,
                "production service reopens the accepted owned durable record");
            return opened.Session;
        }

        private static void VaultDurableSelect(VaultForm form, string id)
        {
            var grid = (DataGridView)Field(form, "grid");
            var row = grid.Rows.Cast<DataGridViewRow>().Single(r => ((VaultEntry)r.Tag).id == id);
            grid.ClearSelection(); grid.CurrentCell = row.Cells[0]; row.Selected = true;
        }

        private static void VaultDurableCheckReopen(string root, VaultSession session, VaultForm form,
            string pin, byte[] realBefore, string action)
        {
            var reopened = VaultDurableOpen(root, pin);
            try {
                var grid = (DataGridView)Field(form, "grid");
                Check(VaultDurableJson(reopened.Data) == VaultDurableJson(session.Data) &&
                    grid.Rows.Count == session.Data.entries.Count && grid.Rows.Cast<DataGridViewRow>()
                        .Select(r => ((VaultEntry)r.Tag).id).OrderBy(id => id)
                        .SequenceEqual(session.Data.entries.Select(e => e.id).OrderBy(id => id)),
                    "actual durable " + action + " publishes exactly the records that reopen from disk");
                if (pin != "1234") Check(File.ReadAllBytes(Path.Combine(root, "vault.enc.json")).SequenceEqual(realBefore),
                    "actual auxiliary durable " + action + " never changes the separate real ciphertext");
            } finally { reopened.DropSensitive(); }
        }

        private static void VaultDurableCrud(string root)
        {
            foreach (bool auxiliary in new[] { false, true }) {
                var owned = Path.Combine(root, auxiliary ? "crud-auxiliary" : "crud-real");
                int ownerThread = Thread.CurrentThread.ManagedThreadId, holdLease = 0, leaseTicks = 0;
                bool checkedLease = false, currentLease = false; long leaseElapsed = -1;
                using (var leaseEntered = new ManualResetEventSlim())
                using (var leaseRelease = new ManualResetEventSlim())
                using (var heartbeat = new System.Windows.Forms.Timer { Interval = 10 }) {
                    Func<DateTime> leaseClock = () => {
                        if (Thread.CurrentThread.ManagedThreadId != ownerThread && Interlocked.CompareExchange(ref holdLease, 0, 1) == 1) {
                            leaseEntered.Set(); VaultHeldRelease(leaseRelease);
                        }
                        return VaultDurableDay;
                    };
                    var original = VaultDurableCreate(owned); var session = auxiliary ? VaultDurableOpen(owned, "4321", leaseClock) : original;
                    var pin = auxiliary ? "4321" : "1234"; var realBefore = File.ReadAllBytes(Path.Combine(owned, "vault.enc.json"));
                    using (var settings = new SettingsService())
                    using (var clipboard = new ClipboardService(settings))
                    using (var form = new VaultForm(session, clipboard, settings, clock: () => VaultDurableDay)) {
                        try {
                            form.Show(); Application.DoEvents();
                            Check((session.Persistence != null) == auxiliary && session.CanPersist == !auxiliary,
                                "production UI retains the correct persistence route for its accepted durable session");
                            var initialId = session.Data.entries.Single().id;
                            if (auxiliary) {
                                heartbeat.Tick += delegate {
                                    if (!leaseEntered.IsSet || checkedLease || ++leaseTicks < 3) return;
                                    var watch = Stopwatch.StartNew(); currentLease = session.IsCurrent(VaultDurableDay);
                                    watch.Stop(); leaseElapsed = watch.ElapsedMilliseconds;
                                    checkedLease = true; leaseRelease.Set();
                                };
                                Interlocked.Exchange(ref holdLease, 1); heartbeat.Start();
                            }
                            VaultWriteAction(form, "AddEntry", "Добавленная синтетическая запись", false);
                            if (auxiliary) Check(checkedLease && currentLease && leaseElapsed >= 0 && leaseElapsed < 500 && leaseTicks >= 3,
                                "actual persistence lease keeps current-session checks and native heartbeat responsive while its save gate is held");
                            Check(session.Data.entries.Count == 2 && session.Data.entries.Any(e => e.id == initialId),
                                "actual durable Add retains the original ID and creates one new record");
                            var addedId = session.Data.entries.Single(e => e.id != initialId).id;
                            VaultDurableCheckReopen(owned, session, form, pin, realBefore, "Add");
                            VaultDurableSelect(form, initialId);
                            VaultWriteAction(form, "EditSelected", "Изменённая синтетическая запись", false);
                            Check(session.Data.entries.Single(e => e.id == initialId).name == "Изменённая синтетическая запись" &&
                                session.Data.entries.Any(e => e.id == addedId), "actual durable Edit preserves both record identities");
                            VaultDurableCheckReopen(owned, session, form, pin, realBefore, "Edit");
                            VaultDurableSelect(form, initialId); VaultWriteAction(form, "DeleteSelected", null, false);
                            Check(session.Data.entries.Count == 1 && session.Data.entries.Single().id == addedId,
                                "actual durable Delete removes only the selected record");
                            VaultDurableCheckReopen(owned, session, form, pin, realBefore, "Delete");
                            if (auxiliary) {
                                VaultDurableSelect(form, addedId); VaultWriteAction(form, "DeleteSelected", null, false);
                                int unexpectedFactoryCalls = 0;
                                var empty = VaultOpenService.Open(Path.Combine(owned, "vault.enc.json"), owned, pin,
                                    () => VaultDurableDay, () => { unexpectedFactoryCalls++; return VaultDurableSeed(); }, CancellationToken.None);
                                try {
                                    Check(empty.Status == VaultOpenStatus.Success && empty.Session != null &&
                                        empty.Session.Persistence != null && empty.Session.Data.entries.Count == 0 && unexpectedFactoryCalls == 0 &&
                                        session.Data.entries.Count == 0 && ((DataGridView)Field(form, "grid")).Rows.Count == 0 &&
                                        File.ReadAllBytes(Path.Combine(owned, "vault.enc.json")).SequenceEqual(realBefore),
                                        "deleting the last auxiliary UI record reopens the same bound empty dataset without regeneration or real ciphertext changes");
                                } finally { if (empty.Session != null) empty.Session.DropSensitive(); }
                                var rejected = VaultOpenService.Open(Path.Combine(owned, "vault.enc.json"), owned, "2222",
                                    () => VaultDurableDay, VaultDurableSeed, CancellationToken.None);
                                Check(rejected.Status == VaultOpenStatus.WrongPin && rejected.Session == null && rejected.ErrorMessage == "Неверный пин-код",
                                    "another auxiliary PIN remains rejected after the UI deletes every bound record");
                            }
                        } finally {
                            leaseRelease.Set(); heartbeat.Stop(); form.Dispose();
                            if (form.Work != null) WaitAsyncRestoreWithoutPump(form.Work);
                            if (form.PresentationWork != null) WaitAsyncRestoreWithoutPump(form.PresentationWork);
                            session.DropSensitive(); original.DropSensitive();
                        }
                    }
                }
            }
        }

        private static void VaultDurableRejectedWrites(string root)
        {
            foreach (bool auxiliary in new[] { false, true }) {
                var owned = Path.Combine(root, auxiliary ? "failure-auxiliary" : "failure-real");
                var original = VaultDurableCreate(owned); var session = auxiliary ? VaultDurableOpen(owned, "4321") : original;
                var realPath = Path.Combine(owned, "vault.enc.json"); var realBefore = File.ReadAllBytes(realPath);
                var before = VaultDurableJson(session.Data); var entry = session.Data.entries.Single();
                string committedOther = null;
                if (auxiliary) {
                    var current = VaultDailyFile.Read(owned); var other = VaultDurableSeed();
                    other.entries[0].name = "Сохранено другим владельцем";
                    var changed = VaultDailyStore.CompareExchange(owned, current.Stamp, VaultDailyState.Bind(VaultDurableDay, "4321", other));
                    Check(changed.Status == VaultDailyCommitStatus.Success, "a competing owned durable writer commits before stale UI save");
                    committedOther = VaultDurableJson(changed.Record.State.FakeData);
                }
                using (var settings = new SettingsService())
                using (var clipboard = new ClipboardService(settings))
                using (var form = new VaultForm(session, clipboard, settings, clock: () => VaultDurableDay)) {
                    FileStream locked = null;
                    try {
                        form.Show(); Application.DoEvents();
                        if (!auxiliary) locked = File.Open(realPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                        foreach (var action in new[] { "AddEntry", "EditSelected", "DeleteSelected" }) {
                            VaultDurableSelect(form, entry.id);
                            var warning = VaultWriteAction(form, action, "Отклонённая синтетическая запись", true);
                            var grid = (DataGridView)Field(form, "grid");
                            Check(warning.Contains("Не удалось сохранить") && warning.Contains("окне не изменены") &&
                                !warning.Contains(entry.secret) && !warning.Contains(realPath) && !warning.Contains("суточ") && !warning.Contains("ложн"),
                                "actual rejected durable " + action + " reports a generic warning without private paths or session hints");
                            Check(VaultDurableJson(session.Data) == before && grid.Rows.Count == 1 &&
                                Object.ReferenceEquals(grid.Rows[0].Tag, entry) && Convert.ToString(grid.Rows[0].Cells[0].Value) == entry.name,
                                "actual rejected durable " + action + " keeps the original session and row without phantom changes");
                        }
                        Check(File.ReadAllBytes(realPath).SequenceEqual(realBefore),
                            "native real-lock and auxiliary-CAS failures leave the original real ciphertext unchanged");
                        if (auxiliary) Check(VaultDurableJson(VaultDailyFile.Read(owned).Record.State.FakeData) == committedOther,
                            "stale durable UI never overwrites the record accepted by the competing writer");
                    } finally {
                        if (locked != null) locked.Dispose();
                        form.Close(); session.DropSensitive(); original.DropSensitive();
                    }
                }
            }
        }

        private static VaultForm VaultDurableContextForm(AsyncRestoreContextFixture fixture, VaultSession session,
            Action<CancellationToken> beforeSave)
        {
            var form = new VaultForm(session, fixture.Clipboard, fixture.Settings, beforeSave: beforeSave,
                canStart: () => VaultContextAllowed(fixture.Context, "CanOpenVault"),
                canPublish: () => VaultContextAllowed(fixture.Context, "CanPublishVault"),
                workStarted: task => Call(fixture.Context, "TrackVaultWork", task), clock: () => VaultDurableDay);
            typeof(UpdateAwareTrayApplicationContext).GetField("vaultForm", PrivateInstance).SetValue(fixture.Context, form);
            typeof(UpdateAwareTrayApplicationContext).GetField("vaultSession", PrivateInstance).SetValue(fixture.Context, session);
            form.Show(); Application.DoEvents(); return form;
        }

        private static void VaultDurableBeginHeldSave(VaultForm form, VaultSession session)
        {
            var entries = session.Data.entries.Select(e => e.Clone()).ToList();
            entries[0].name = "Непринятое синтетическое изменение";
            Call(form, "SaveEntries", entries);
        }

        private static bool VaultDurableResultDropped(Task pending)
        {
            var result = pending.GetType().GetProperty("Result").GetValue(pending, null);
            return result != null && Field(result, "Data") == null;
        }

        private static void VaultDurableShutdownWaits(string root)
        {
            var owned = Path.Combine(root, "shutdown-save"); var session = VaultDurableCreate(owned);
            var path = Path.Combine(owned, "vault.enc.json"); var baseline = File.ReadAllBytes(path);
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var fixture = new AsyncRestoreContextFixture(root))
            using (var pulse = new System.Windows.Forms.Timer { Interval = 10 }) {
                int ticks = 0, launches = 0, ownerThread = Thread.CurrentThread.ManagedThreadId, workerThread = 0;
                CancellationToken observed = default(CancellationToken);
                using (var form = VaultDurableContextForm(fixture, session, token => {
                    observed = token; workerThread = Thread.CurrentThread.ManagedThreadId;
                    Interlocked.Increment(ref launches); entered.Set(); VaultHeldRelease(release);
                })) {
                    pulse.Tick += delegate { ticks++; }; pulse.Start(); VaultDurableBeginHeldSave(form, session);
                    var pending = form.Work;
                    try {
                        PumpAsyncRestore(() => entered.IsSet && ticks >= 3);
                        Check(Object.ReferenceEquals(fixture.Context.VaultWork, pending) && fixture.Context.IsVaultRunning &&
                            workerThread != ownerThread && form.IsBusy && launches == 1,
                            "actual application owns a single raw save worker while the native form heartbeat continues");
                        var watch = Stopwatch.StartNew(); var shutdown = fixture.Context.RequestShutdownAsync();
                        var duplicate = fixture.Context.RequestShutdownAsync(); PumpAsyncRestore(() => shutdown.IsCompleted, 10000); watch.Stop();
                        Check(Object.ReferenceEquals(shutdown, duplicate) && !shutdown.Result && observed.IsCancellationRequested &&
                            !pending.IsCompleted && fixture.Context.IsVaultRunning && form.IsBusy && form.Visible &&
                            fixture.CliCleanup == 0 && fixture.WindowsCleanup == 0 && !(bool)Field(fixture.Context, "shutdownPrepared"),
                            "shutdown cancels a pending save and refuses cleanup while the real save worker still owns its candidate");
                        Check(watch.ElapsedMilliseconds >= 2800 && watch.ElapsedMilliseconds < 6000 && ticks >= 20 &&
                            !VaultContextAllowed(fixture.Context, "CanOpenVault"),
                            "the real three-second save shutdown deadline keeps message pumping and prevents duplicate work");
                        release.Set(); PumpAsyncRestore(() => pending.IsCompleted && form.PresentationWork.IsCompleted && !fixture.Context.IsVaultRunning);
                        Check(session.IsDiscarded && session.Pin == null && launches == 1 && VaultDurableResultDropped(pending) && File.ReadAllBytes(path).SequenceEqual(baseline) &&
                            fixture.InstalledUnchanged(), "late cancelled save settles without adopting a candidate or changing the owned ciphertext");
                        var retry = fixture.Context.RequestShutdownAsync(); PumpAsyncRestore(() => retry.IsCompleted);
                        Check(retry.Result && fixture.CliCleanup == 1 && fixture.WindowsCleanup == 1 &&
                            (bool)Field(fixture.Context, "shutdownPrepared") && !VaultContextAllowed(fixture.Context, "CanPublishVault") &&
                            AsyncRestoreTreeMatches(fixture.Source, fixture.SourceHashes),
                            "explicit shutdown retry starts exactly one native cleanup after the raw save worker settles");
                        fixture.Context.CancelShutdown();
                    } finally {
                        release.Set(); fixture.Context.Dispose(); WaitAsyncRestoreWithoutPump(pending);
                        if (form.PresentationWork != null) WaitAsyncRestoreWithoutPump(form.PresentationWork);
                        session.DropSensitive();
                    }
                }
            }
        }

        private static void VaultDurableDisposedOwner(string root)
        {
            var owned = Path.Combine(root, "disposed-save"); var session = VaultDurableCreate(owned);
            var path = Path.Combine(owned, "vault.enc.json"); var baseline = File.ReadAllBytes(path);
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var fixture = new AsyncRestoreContextFixture(root)) {
                CancellationToken observed = default(CancellationToken); int lateUi = 0;
                using (var form = VaultDurableContextForm(fixture, session, token => {
                    observed = token; entered.Set(); VaultHeldRelease(release);
                })) {
                    VaultDurableBeginHeldSave(form, session); var pending = form.Work;
                    try {
                        PumpAsyncRestore(() => entered.IsSet);
                        var grid = (DataGridView)Field(form, "grid"); grid.RowsAdded += delegate { lateUi++; };
                        var watch = Stopwatch.StartNew(); fixture.Context.Dispose(); watch.Stop(); lateUi = 0;
                        Check(watch.ElapsedMilliseconds < 500 && observed.IsCancellationRequested && !pending.IsCompleted && form.IsDisposed &&
                            Object.ReferenceEquals(fixture.Context.VaultWork, pending) && !VaultContextAllowed(fixture.Context, "CanPublishVault"),
                            "forced application disposal cancels the save owner promptly and retains its actual raw worker");
                        release.Set(); WaitAsyncRestoreWithoutPump(pending); WaitAsyncRestoreWithoutPump(form.PresentationWork);
                        Check(!pending.IsFaulted && session.IsDiscarded && session.Pin == null && VaultDurableResultDropped(pending) && lateUi == 0 &&
                            fixture.CliCleanup == 0 && fixture.WindowsCleanup == 0 && File.ReadAllBytes(path).SequenceEqual(baseline) &&
                            fixture.InstalledUnchanged() && AsyncRestoreTreeMatches(fixture.Source, fixture.SourceHashes),
                            "ownerless save settles without native UI pumping, installed writes, cleanup or late row publication");
                        Application.DoEvents(); Check(lateUi == 0 && form.IsDisposed,
                            "resumed native dispatch cannot revive the disposed vault save owner");
                    } finally {
                        release.Set(); fixture.Context.Dispose(); WaitAsyncRestoreWithoutPump(pending);
                        if (form.PresentationWork != null) WaitAsyncRestoreWithoutPump(form.PresentationWork);
                        session.DropSensitive();
                    }
                }
            }
        }
    }
}

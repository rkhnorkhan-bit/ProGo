using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        private sealed class VaultSaveLease : IVaultSessionPersistence
        {
            internal int Calls, WorkerThread;
            internal bool Fail;
            internal VaultData Saved;
            internal DateTime Day = new DateTime(2026, 10, 13);
            public bool IsCurrent(DateTime value) { return value.Date == Day.Date; }
            public void Save(VaultData data, string pin)
            {
                Interlocked.Increment(ref Calls); WorkerThread = Thread.CurrentThread.ManagedThreadId;
                if (Fail) throw new InvalidOperationException("synthetic-private-save-error");
                Saved = data;
            }
        }
        private static VaultSession VaultSaveSession(VaultSaveLease lease)
        {
            var data = VaultData.Empty(); var entry = VaultEntry.New();
            entry.name = "Исходная запись"; entry.secret = "synthetic-private-save-value"; data.entries.Add(entry);
            return new VaultSession(data, "1234", false, lease);
        }
        private static List<VaultEntry> VaultSaveEntries(VaultSession session)
        {
            var entries = session.Data.entries.Select(entry => entry.Clone()).ToList();
            var added = VaultEntry.New(); added.name = "Новая запись"; added.secret = "synthetic-candidate-value"; entries.Add(added);
            return entries;
        }
        private static void VaultSaveWait(VaultForm form)
        {
            PumpUntil(() => form.PresentationWork != null && form.PresentationWork.IsCompleted);
            form.PresentationWork.GetAwaiter().GetResult();
        }
        private static object VaultSaveResult(VaultForm form)
        { return form.Work.GetType().GetProperty("Result").GetValue(form.Work, null); }
        private static void VaultSaveHeld(ManualResetEventSlim release)
        { if (!release.Wait(15000)) throw new TimeoutException("owned vault save fixture deadline"); }

        private static void VaultAsyncSave(SettingsService settings)
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") {
                Console.WriteLine("SKIP: asynchronous vault save fixtures require isolated native CI"); return;
            }
            var previousContext = SynchronizationContext.Current; bool previousCheck = Control.CheckForIllegalCrossThreadCalls;
            try {
                Control.CheckForIllegalCrossThreadCalls = true;
                VaultSaveHeartbeat(settings); VaultSaveFailure(settings); VaultSaveOwnerGates(settings); VaultSaveSetupOwnerLoss(settings); VaultSaveReentrantEnable(settings);
                VaultSaveClockAndCommitHook(settings); VaultSaveEditorOwnerClose(settings);
                foreach (string action in new[] { "close-button", "lock-button", "escape", "title-close", "dispose" }) {
                    VaultSaveCancelled(settings, action, false); VaultSaveCancelled(settings, action, true);
                }
            } finally {
                SynchronizationContext.SetSynchronizationContext(previousContext);
                Control.CheckForIllegalCrossThreadCalls = previousCheck;
            }
        }
        private static void VaultSaveHeartbeat(SettingsService settings)
        {
            int owner = Thread.CurrentThread.ManagedThreadId, ticks = 0, wrongThread = 0, starts = 0;
            var previousContext = SynchronizationContext.Current;
            var lease = new VaultSaveLease(); var session = VaultSaveSession(lease); var originalData = session.Data;
            var entries = VaultSaveEntries(session); var candidateEntry = entries[1];
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var heartbeat = new System.Windows.Forms.Timer { Interval = 20 })
            using (var clipboard = new ClipboardService(settings))
            using (var form = new VaultForm(session, clipboard, settings, token => { entered.Set(); VaultSaveHeld(release); },
                workStarted: task => { starts++; if (Thread.CurrentThread.ManagedThreadId != owner) wrongThread++; }, clock: () => lease.Day)) {
                try {
                    form.Show(); Application.DoEvents();
                    foreach (var control in Descendants(form)) control.TextChanged += delegate {
                        if (Thread.CurrentThread.ManagedThreadId != owner) Interlocked.Increment(ref wrongThread);
                    };
                    heartbeat.Tick += delegate { ticks++; }; heartbeat.Start(); SynchronizationContext.SetSynchronizationContext(null);
                    Call(form, "SaveEntries", entries); Call(form, "SaveEntries", VaultSaveEntries(session));
                    Call(form, "AddEntry"); Call(form, "EditSelected"); Call(form, "DeleteSelected"); Call(form, "CopySelected");
                    candidateEntry.name = "Изменено после запуска"; entries.Clear();
                    PumpUntil(() => entered.IsSet && ticks >= 3);
                    var grid = (DataGridView)Field(form, "grid");
                    Check(form.IsBusy && !form.Work.IsCompleted && starts == 1 && lease.Calls == 0,
                        "vault saves are single-flight and wait before actual persistence while UI heartbeat runs");
                    Check(originalData.entries.Count == 1 && grid.Rows.Count == 1 && ReferenceEquals(grid.Rows[0].Tag, originalData.entries[0]),
                        "pending save and blocked Add Edit Delete Copy leave original session and grid untouched");
                    Check(!((TextBox)Field(form, "search")).Enabled && !((ComboBox)Field(form, "typeFilter")).Enabled && !grid.Enabled &&
                        !((Button)Field(form, "add")).Enabled && !((Button)Field(form, "copy")).Enabled && ((Button)Field(form, "close")).Enabled,
                        "pending durable save disables edits filters and copy while preserving closure request");
                    Check(((Label)Field(form, "status")).AccessibilityObject.Name == "Состояние сохранения хранилища" && wrongThread == 0,
                        "busy save status is accessible and all tracking and control updates remain on owner STA");
                    Shot(form, "vault-save-pending"); release.Set(); VaultSaveWait(form);
                    Check(lease.Calls == 1 && lease.WorkerThread != owner && lease.Saved.entries.Count == 2 && lease.Saved.entries[1].name == "Новая запись" &&
                        !ReferenceEquals(lease.Saved.entries[1], candidateEntry), "worker persists an isolated list and cloned entries captured before caller mutation");
                    Check(!form.IsBusy && session.Data.entries.Count == 2 && grid.Rows.Count == 2 && ((Button)Field(form, "add")).Enabled &&
                        ((Label)Field(form, "status")).Text == "Изменения сохранены.", "settled successful save alone publishes new records and enables fresh actions");
                    Check(wrongThread == 0 && Field(VaultSaveResult(form), "Data") == null,
                        "lost ambient synchronization context preserves STA publication and clears raw task result references");
                } finally { release.Set(); SynchronizationContext.SetSynchronizationContext(previousContext); }
            }
            lease.Saved = null;
        }
        private static void VaultSaveFailure(SettingsService settings)
        {
            var lease = new VaultSaveLease { Fail = true }; var session = VaultSaveSession(lease); var data = session.Data;
            bool warned = false; string text = "";
            using (var clipboard = new ClipboardService(settings))
            using (var form = new VaultForm(session, clipboard, settings, clock: () => lease.Day))
            using (var timer = new System.Windows.Forms.Timer { Interval = 20 }) {
                timer.Tick += delegate {
                    var dialog = FindWindow("#32770", "Сохранение хранилища"); if (dialog == IntPtr.Zero) return;
                    warned = true; var content = new System.Text.StringBuilder();
                    EnumChildWindows(dialog, delegate(IntPtr child, IntPtr state) {
                        var value = new System.Text.StringBuilder(2048); GetWindowText(child, value, value.Capacity); content.AppendLine(value.ToString()); return true;
                    }, IntPtr.Zero); text = content.ToString(); PostMessage(dialog, 0x0010, IntPtr.Zero, IntPtr.Zero);
                };
                form.Show(); Application.DoEvents(); timer.Start(); Call(form, "SaveEntries", VaultSaveEntries(session)); VaultSaveWait(form);
                Check(warned && text.Contains("Не удалось сохранить") && text.Contains("окне не изменены") && !text.Contains("synthetic-private"),
                    "failed durable save shows static actionable warning without private error details or content");
                Check(session.Data.entries.Count == 1 && ReferenceEquals(session.Data, data) && ((DataGridView)Field(form, "grid")).Rows.Count == 1 &&
                    ReferenceEquals(((DataGridView)Field(form, "grid")).Rows[0].Tag, data.entries[0]), "failed save creates no phantom data or grid publication");
                Check(!form.IsBusy && form.Visible && ((Button)Field(form, "add")).Enabled && Field(VaultSaveResult(form), "Data") == null,
                    "save failure permits an explicit retry and releases failed worker result references");
                lease.Fail = false; Call(form, "SaveEntries", VaultSaveEntries(session)); VaultSaveWait(form);
                Check(lease.Calls == 2 && session.Data.entries.Count == 2, "a new explicit durable save can succeed after failure");
            }
            lease.Saved = null;
        }
        private static void VaultSaveCancelled(SettingsService settings, string action, bool committed)
        {
            int ticks = 0, lateChanges = 0; bool ownerClosed = false;
            var lease = new VaultSaveLease(); var session = VaultSaveSession(lease); var callerData = session.Data;
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var heartbeat = new System.Windows.Forms.Timer { Interval = 20 })
            using (var clipboard = new ClipboardService(settings))
            using (var form = new VaultForm(session, clipboard, settings,
                beforeSave: committed ? (Action<CancellationToken>)null : token => { entered.Set(); VaultSaveHeld(release); },
                clock: () => lease.Day, afterSave: committed ? (Action)(() => { entered.Set(); VaultSaveHeld(release); }) : null)) {
                try {
                    form.Show(); Application.DoEvents();
                    foreach (var control in Descendants(form)) control.TextChanged += delegate { if (ownerClosed) lateChanges++; };
                    heartbeat.Tick += delegate { ticks++; }; heartbeat.Start(); Call(form, "SaveEntries", VaultSaveEntries(session));
                    PumpUntil(() => entered.IsSet && ticks >= 3);
                    if (action == "close-button") ((Button)Field(form, "close")).PerformClick();
                    else if (action == "lock-button") ((Button)Field(form, "lockButton")).PerformClick();
                    else if (action == "escape") Call(form, "ProcessDialogKey", Keys.Escape);
                    else if (action == "title-close") form.Close();
                    else { form.Dispose(); ownerClosed = true; }
                    if (action != "dispose") Check(form.IsBusy && form.Visible && !form.Work.IsCompleted && session.Pin != null &&
                        ((Button)Field(form, "close")).DialogResult == DialogResult.None && !((DataGridView)Field(form, "grid")).Visible &&
                        ((DataGridView)Field(form, "grid")).Rows.Count == 0 && !((TextBox)Field(form, "search")).Visible,
                        "busy " + action + " requests closure without early modal or raw settlement (committed=" + committed + ")");
                    else Check(!form.Work.IsCompleted && VaultUiDropped(session), "forced Dispose drops owner references without pretending pending save settled");
                    release.Set(); VaultSaveWait(form); ownerClosed = true; Application.DoEvents();
                    Check(lease.Calls == (committed ? 1 : 0) && (bool)Field(VaultSaveResult(form), "Committed") == committed,
                        "closure " + action + " preserves actual commit outcome and skips only work cancelled before save (committed=" + committed + ")");
                    Check(VaultUiDropped(session) && callerData.entries.Count == 1 && Field(VaultSaveResult(form), "Data") == null && lateChanges == 0,
                        "closure " + action + " discards late save publication and task refs without mutating caller dataset (committed=" + committed + ")");
                } finally { release.Set(); }
            }
            lease.Saved = null;
        }
        private static void VaultSaveOwnerGates(SettingsService settings)
        {
            foreach (string phase in new[] { "start-denied", "publish-denied", "start-dispose", "publish-dispose", "publish-cancel" }) {
                var lease = new VaultSaveLease(); var session = VaultSaveSession(lease); var data = session.Data;
                VaultForm form = null; int owner = Thread.CurrentThread.ManagedThreadId, wrongThread = 0;
                using (var clipboard = new ClipboardService(settings))
                using (form = new VaultForm(session, clipboard, settings, canStart: () => {
                    if (Thread.CurrentThread.ManagedThreadId != owner) wrongThread++;
                    if (phase == "start-dispose") form.Dispose(); return phase != "start-denied";
                }, canPublish: () => {
                    if (Thread.CurrentThread.ManagedThreadId != owner) wrongThread++;
                    if (phase == "publish-dispose") form.Dispose(); if (phase == "publish-cancel") form.CancelCurrent();
                    return phase != "publish-denied";
                }, clock: () => lease.Day)) {
                    form.Show(); Application.DoEvents(); Call(form, "SaveEntries", VaultSaveEntries(session));
                    bool starts = phase.StartsWith("publish", StringComparison.Ordinal);
                    if (starts) VaultSaveWait(form);
                    Check(wrongThread == 0 && lease.Calls == (starts ? 1 : 0) && (starts || form.Work == null),
                        "owner gate " + phase + " is evaluated on STA and checked again after its reentrant callback");
                    Check(data.entries.Count == 1 && (!starts || (VaultUiDropped(session) && Field(VaultSaveResult(form), "Data") == null)),
                        "owner gate " + phase + " cannot publish or continue stale UI after committed or rejected work");
                }
                lease.Saved = null;
            }
        }
        private static void VaultSaveSetupOwnerLoss(SettingsService settings)
        {
            foreach (bool disposeOwner in new[] { false, true }) {
                var lease = new VaultSaveLease(); var session = VaultSaveSession(lease); var data = session.Data; bool invoked = false;
                using (var clipboard = new ClipboardService(settings))
                using (var form = new VaultForm(session, clipboard, settings, clock: () => lease.Day)) {
                    form.Show(); Application.DoEvents(); var search = (TextBox)Field(form, "search");
                    search.EnabledChanged += delegate {
                        if (search.Enabled || invoked) return;
                        invoked = true; if (disposeOwner) form.Dispose(); else form.CancelCurrent();
                    };
                    Call(form, "SaveEntries", VaultSaveEntries(session));
                    Check(invoked && form.Work == null && lease.Calls == 0 && VaultUiDropped(session) && data.entries.Count == 1,
                        "busy setup control callback " + (disposeOwner ? "Dispose" : "Cancel") + " discards prepared candidate before launching raw IO");
                }
            }
        }
        private static void VaultSaveReentrantEnable(SettingsService settings)
        {
            var lease = new VaultSaveLease(); var session = VaultSaveSession(lease); int phase = 0, started = 0; bool capturedCommittedData = false;
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var clipboard = new ClipboardService(settings))
            using (var form = new VaultForm(session, clipboard, settings, beforeSave: token => {
                if (Interlocked.Increment(ref started) == 2) { entered.Set(); VaultSaveHeld(release); }
            }, clock: () => lease.Day)) {
                try {
                    form.Show(); Application.DoEvents(); var grid = (DataGridView)Field(form, "grid");
                    grid.EnabledChanged += delegate {
                        if (!grid.Enabled || phase != 0) return;
                        phase = 1; capturedCommittedData = session.Data.entries.Count == 2;
                        Call(form, "SaveEntries", VaultSaveEntries(session));
                    };
                    Call(form, "SaveEntries", VaultSaveEntries(session)); PumpUntil(() => entered.IsSet);
                    Check(capturedCommittedData && form.IsBusy && !form.Work.IsCompleted && !((Button)Field(form, "add")).Enabled &&
                        !grid.Enabled && session.Data.entries.Count == 2,
                        "reentrant enable callback sees committed data and outer publication does not re-enable a newer busy save");
                    release.Set(); VaultSaveWait(form);
                    Check(started == 2 && lease.Calls == 2 && session.Data.entries.Count == 3 && grid.Rows.Count == 3 && !form.IsBusy &&
                        Field(VaultSaveResult(form), "Data") == null, "reentrant save retains its own raw and presentation task until the second commit is adopted");
                } finally { release.Set(); }
            }
            lease.Saved = null;
        }
        private static void VaultSaveClockAndCommitHook(SettingsService settings)
        {
            var lease = new VaultSaveLease(); var session = VaultSaveSession(lease); var day = lease.Day;
            using (var clipboard = new ClipboardService(settings))
            using (var form = new VaultForm(session, clipboard, settings, clock: () => day,
                afterSave: () => { day = day.AddDays(1); throw new OperationCanceledException("synthetic-after-commit"); })) {
                form.Show(); Application.DoEvents(); Call(form, "SaveEntries", VaultSaveEntries(session)); VaultSaveWait(form);
                Check(lease.Calls == 1 && (bool)Field(VaultSaveResult(form), "Committed") && VaultUiDropped(session) && !form.Visible,
                    "after-commit hook exception does not erase commit and midnight lease prevents stale adoption");
                Check(Field(VaultSaveResult(form), "Data") == null, "midnight discarded save releases candidate data from raw task result");
            }
            lease.Saved = null;
            // A proper session's IsCurrent does not consult a clock; use owned synthetic persistence-free memory mode.
            var realCompatible = new VaultSession(VaultData.Empty(), "1234", false); int clockCalls = 0;
            using (var clipboard = new ClipboardService(settings))
            using (var form = new VaultForm(realCompatible, clipboard, settings, clock: () => { clockCalls++; throw new Exception("clock fixture"); })) {
                form.Show(); Application.DoEvents(); Call(form, "SaveEntries", VaultSaveEntries(realCompatible));
                Check(clockCalls == 0 && form.Work == null && realCompatible.Data.entries.Count == 1,
                    "legacy nonpersistent session preserves synchronous memory save and stays independent of clock");
            }
        }
        private static void VaultSaveEditorOwnerClose(SettingsService settings)
        {
            var lease = new VaultSaveLease(); var session = VaultSaveSession(lease); var data = session.Data; bool seen = false;
            using (var clipboard = new ClipboardService(settings))
            using (var form = new VaultForm(session, clipboard, settings, clock: () => lease.Day))
            using (var timer = new System.Windows.Forms.Timer { Interval = 20 }) {
                timer.Tick += delegate {
                    var editor = Application.OpenForms.OfType<EntryForm>().FirstOrDefault(); if (editor == null || seen) return;
                    seen = true; Check(ReferenceEquals(editor.Owner, form), "entry editor has explicit vault owner"); form.CancelCurrent();
                };
                form.Show(); Application.DoEvents(); timer.Start(); Call(form, "AddEntry");
                Check(seen && form.Work == null && lease.Calls == 0 && VaultUiDropped(session) && data.entries.Count == 1 &&
                    !Application.OpenForms.OfType<EntryForm>().Any(), "closing vault while nested editor is open cancels child and forbids delayed save");
            }
        }
    }
}

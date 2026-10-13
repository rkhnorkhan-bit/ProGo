using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        private sealed class VaultExpiryLease : IVaultSessionPersistence, IVaultSessionRevocation
        {
            internal readonly DateTime Day = new DateTime(2026, 10, 13);
            internal volatile bool Revoked;
            internal int Revocations, Saves;
            internal VaultData Saved;
            public bool IsCurrent(DateTime value)
            {
                VaultDailyPolicy.RequireLocalTime(value);
                if (value.Date != Day) Revoked = true;
                return !Revoked;
            }
            public void Revoke() { Revoked = true; Interlocked.Increment(ref Revocations); }
            public void Save(VaultData data, string pin)
            {
                if (Revoked) throw new InvalidOperationException("synthetic expired lease");
                Interlocked.Increment(ref Saves); Saved = data;
            }
        }
        private static VaultSession VaultExpirySession(VaultExpiryLease lease)
        {
            var data = VaultData.Empty(); var entry = VaultEntry.New(); entry.name = "Синтетическая запись";
            entry.login = "synthetic-owner"; entry.secret = "synthetic-expiry-secret"; data.entries.Add(entry);
            return new VaultSession(data, "4321", false, lease);
        }
        private static bool VaultExpiryHidden(VaultForm form)
        {
            var grid = (DataGridView)Field(form, "grid");
            return !grid.Visible && grid.Rows.Count == 0 && !((TextBox)Field(form, "search")).Visible;
        }
        private static void VaultActiveExpiry(SettingsService settings)
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") {
                Console.WriteLine("SKIP: active vault expiry fixtures require isolated native Windows CI"); return;
            }
            var priorContext = SynchronizationContext.Current; bool priorCheck = Control.CheckForIllegalCrossThreadCalls;
            try {
                Control.CheckForIllegalCrossThreadCalls = true;
                foreach (int direction in new[] { 1, -1 }) VaultExpiryIdle(settings, direction);
                VaultExpiryActivation(settings); VaultExpiryNestedEditor(settings); VaultExpiryRealClock(settings);
                foreach (bool throwing in new[] { true, false }) VaultExpiryInvalidClock(settings, throwing);
                foreach (bool committed in new[] { false, true }) VaultExpiryPendingSave(settings, committed);
                foreach (string action in new[] { "throw", "dispose", "cancel" }) VaultExpiryCallback(settings, action);
                VaultExpiryOffOwner(settings); VaultExpiryFrozenAfterCancel(settings); VaultExpiryDuringRowsAdded(settings); VaultExpiryPendingOpen();
            } finally {
                SynchronizationContext.SetSynchronizationContext(priorContext); Control.CheckForIllegalCrossThreadCalls = priorCheck;
            }
        }
        private static void VaultExpiryIdle(SettingsService settings, int direction)
        {
            var lease = new VaultExpiryLease(); var session = VaultExpirySession(lease); var original = session.Data;
            var prepared = session.CopyWithData(VaultData.Empty()); int notified = 0; bool capturedLease = false;
            DateTime observed = default(DateTime); int owner = Thread.CurrentThread.ManagedThreadId;
            using (var clipboard = new ClipboardService(settings))
            using (var form = new VaultForm(session, clipboard, settings, clock: () => lease.Day, expired: (value, local) => {
                notified++; observed = local; capturedLease = ReferenceEquals(value.Persistence, lease) && lease.Revoked && Thread.CurrentThread.ManagedThreadId == owner;
            })) {
                form.Show(); Application.DoEvents(); bool current = form.CheckExpiry(lease.Day.AddDays(direction));
                Check(!current && notified == 1 && capturedLease && observed == lease.Day.AddDays(direction) && VaultUiDropped(session) && VaultExpiryHidden(form),
                    "idle fake vault expires forward or backwards with hidden rows and captured revoked owner lease direction=" + direction +
                    " current=" + current + " notified=" + notified + " captured=" + capturedLease + " dateMatches=" + (observed == lease.Day.AddDays(direction)) +
                    " revoked=" + lease.Revoked + " dropped=" + VaultUiDropped(session) + " gridRows=" + ((DataGridView)Field(form, "grid")).Rows.Count +
                    " gridVisible=" + ((DataGridView)Field(form, "grid")).Visible + " searchVisible=" + ((TextBox)Field(form, "search")).Visible + " disposed=" + form.IsDisposed);
                Check(!form.CheckExpiry(lease.Day) && !prepared.IsCurrent(lease.Day) && lease.Revocations == 1 && notified == 1 && original.entries.Count == 1,
                    "observed A B A never revives the closed owner or a prepared shared lease direction=" + direction);
                bool refused = false; try { VaultService.Save(prepared); } catch (InvalidOperationException) { refused = true; }
                Check(refused && lease.Saves == 0 && form.Work == null, "revoked prepared fake wrapper cannot start persistence after clock reversal");
            }
            prepared.DropSensitive();
        }
        private static void VaultExpiryActivation(SettingsService settings)
        {
            var lease = new VaultExpiryLease(); var session = VaultExpirySession(lease); var now = lease.Day; int notifications = 0;
            using (var clipboard = new ClipboardService(settings))
            using (var form = new VaultForm(session, clipboard, settings, clock: () => now, expired: (value, date) => notifications++)) {
                form.Show(); Application.DoEvents(); now = now.AddDays(1); Call(form, "OnActivated", EventArgs.Empty);
                Check(notifications == 1 && VaultUiDropped(session) && VaultExpiryHidden(form) && !form.Visible,
                    "native owner Activated event observes midnight before any additional user action");
            }
        }
        private static void VaultExpiryNestedEditor(SettingsService settings)
        {
            var lease = new VaultExpiryLease(); var session = VaultExpirySession(lease); var original = session.Data;
            var now = lease.Day; bool editorSeen = false; int notified = 0;
            using (var clipboard = new ClipboardService(settings))
            using (var form = new VaultForm(session, clipboard, settings, clock: () => now, expired: (value, date) => notified++))
            using (var timer = new System.Windows.Forms.Timer { Interval = 20 }) {
                timer.Tick += delegate {
                    var editor = Application.OpenForms.OfType<EntryForm>().FirstOrDefault(); if (editor == null || editorSeen) return;
                    editorSeen = true; now = now.AddDays(1); Call(editor, "OnActivated", EventArgs.Empty);
                };
                form.Show(); Application.DoEvents(); var grid = (DataGridView)Field(form, "grid");
                grid.CurrentCell = grid.Rows[0].Cells[0]; grid.Rows[0].Selected = true; timer.Start(); Call(form, "EditSelected");
                Check(editorSeen && notified == 1 && lease.Revoked && VaultUiDropped(session) && VaultExpiryHidden(form) &&
                    !Application.OpenForms.OfType<EntryForm>().Any(), "expiry observed through an activated owned editor cancels the child and hides its vault owner");
                Check(form.Work == null && lease.Saves == 0 && original.entries.Count == 1,
                    "nested editor midnight cannot publish or save its abandoned candidate");
            }
        }
        private static void VaultExpiryRealClock(SettingsService settings)
        {
            var session = new VaultSession(VaultData.Empty(), "1234", true); int clocks = 0, notifications = 0;
            using (var clipboard = new ClipboardService(settings))
            using (var form = new VaultForm(session, clipboard, settings, clock: () => { clocks++; throw new Exception("synthetic clock"); },
                expired: (value, local) => notifications++)) {
                form.Show(); Application.DoEvents(); Call(form, "OnActivated", EventArgs.Empty);
                Check(form.CheckCurrent() && form.CheckExpiry(DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc)) &&
                    clocks == 0 && notifications == 0 && !session.IsDiscarded && form.Visible,
                    "proper vault owner remains usable through activation and invalid date without consulting the clock");
            }
        }
        private static void VaultExpiryInvalidClock(SettingsService settings, bool throwing)
        {
            var lease = new VaultExpiryLease(); var session = VaultExpirySession(lease); var original = session.Data;
            bool invalid = false; int notified = 0; DateTime observed = lease.Day;
            using (var clipboard = new ClipboardService(settings))
            using (var form = new VaultForm(session, clipboard, settings, clock: () => {
                if (!invalid) return lease.Day;
                if (throwing) throw new Exception("synthetic private clock error");
                return DateTime.SpecifyKind(lease.Day, DateTimeKind.Utc);
            }, expired: (value, date) => { notified++; observed = date; })) {
                form.Show(); Application.DoEvents(); invalid = true; bool current = form.CheckCurrent();
                Check(!current && notified == 1 && lease.Revoked && observed.Kind == DateTimeKind.Utc && VaultUiDropped(session) && VaultExpiryHidden(form),
                    "invalid fake clock fails closed and reports invalid metadata without an invented local expiry day throwing=" + throwing);
                Check(!throwing || observed == DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc),
                    "throwing clock uses explicit invalid UTC sentinel for cleanup retry without writing that sentinel");
                Check(original.entries.Count == 1 && lease.Saves == 0 && form.Work == null,
                    "clock failure closes owner without modifying shared caller dataset or pretending an IO cleanup happened");
            }
        }
        private static void VaultExpiryPendingSave(SettingsService settings, bool committed)
        {
            var lease = new VaultExpiryLease(); var session = VaultExpirySession(lease); var original = session.Data;
            var prepared = session.CopyWithData(VaultData.Empty()); int notified = 0, ticks = 0; var now = lease.Day;
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var heartbeat = new System.Windows.Forms.Timer { Interval = 20 })
            using (var clipboard = new ClipboardService(settings))
            using (var form = new VaultForm(session, clipboard, settings,
                beforeSave: committed ? (Action<CancellationToken>)null : token => { entered.Set(); VaultSaveHeld(release); },
                clock: () => now, afterSave: committed ? (Action)(() => { entered.Set(); VaultSaveHeld(release); }) : null,
                expired: (value, local) => notified++)) {
                try {
                    form.Show(); Application.DoEvents(); heartbeat.Tick += delegate { ticks++; }; heartbeat.Start();
                    Call(form, "SaveEntries", VaultSaveEntries(session)); PumpUntil(() => entered.IsSet && ticks >= 3);
                    now = now.AddDays(1); var watch = Stopwatch.StartNew(); bool current = form.CheckExpiry(now); watch.Stop();
                    Check(!current && watch.ElapsedMilliseconds < 500 && notified == 1 && lease.Revoked && form.IsBusy && form.Visible &&
                        !form.Work.IsCompleted && VaultExpiryHidden(form), "midnight revokes and hides a pending fake save without blocking heartbeat or its raw owner committed=" + committed);
                    now = lease.Day; Check(!form.CheckExpiry(now) && !prepared.IsCurrent(now) && notified == 1,
                        "clock reversal cannot revive fake save while actual worker is still held committed=" + committed);
                    release.Set(); VaultSaveWait(form);
                    Check(VaultUiDropped(session) && original.entries.Count == 1 && lease.Saves == (committed ? 1 : 0) &&
                        (bool)Field(VaultSaveResult(form), "Committed") == committed && Field(VaultSaveResult(form), "Data") == null,
                        "expired fake save settles and discards publication while retaining its actual commit outcome committed=" + committed);
                } finally { release.Set(); }
            }
            lease.Saved = null; prepared.DropSensitive();
        }
        private static void VaultExpiryCallback(SettingsService settings, string action)
        {
            var lease = new VaultExpiryLease(); var session = VaultExpirySession(lease); int notified = 0; bool captured = false; VaultForm form = null;
            using (var clipboard = new ClipboardService(settings))
            using (form = new VaultForm(session, clipboard, settings, clock: () => lease.Day, expired: (value, local) => {
                notified++; captured = ReferenceEquals(value.Persistence, lease) && lease.Revoked;
                if (action == "throw") throw new Exception("synthetic private callback");
                if (action == "dispose") form.Dispose(); else form.CancelCurrent();
            })) {
                form.Show(); Application.DoEvents(); form.CheckExpiry(lease.Day.AddDays(1));
                Check(notified == 1 && captured && lease.Revocations == 1 && VaultUiDropped(session) && VaultExpiryHidden(form),
                    "expiry metadata is captured before reentrant callback teardown and cannot reopen content action=" + action);
            }
        }
        private static void VaultExpiryOffOwner(SettingsService settings)
        {
            var lease = new VaultExpiryLease(); var session = VaultExpirySession(lease); int notified = 0;
            using (var clipboard = new ClipboardService(settings))
            using (var form = new VaultForm(session, clipboard, settings, clock: () => lease.Day, expired: (value, local) => notified++)) {
                form.Show(); Application.DoEvents(); var attempt = Task.Run(() => form.CheckExpiry(lease.Day.AddDays(1)));
                if (!attempt.Wait(5000)) throw new TimeoutException("owned off-owner expiry fixture deadline");
                Check(!attempt.Result && !lease.Revoked && notified == 0 && !session.IsDiscarded && form.Visible,
                    "off-owner expiry call performs no clock lease or native-control mutation");
                Check(!form.CheckExpiry(lease.Day.AddDays(1)) && notified == 1, "subsequent owner expiry performs the single guarded revocation");
            }
        }
        private static void VaultExpiryFrozenAfterCancel(SettingsService settings)
        {
            var lease = new VaultExpiryLease(); var session = VaultExpirySession(lease); var now = lease.Day; int clocks = 0, notifications = 0;
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var clipboard = new ClipboardService(settings))
            using (var form = new VaultForm(session, clipboard, settings, token => { entered.Set(); VaultSaveHeld(release); },
                clock: () => { clocks++; return now; }, expired: (value, local) => notifications++)) {
                try {
                    form.Show(); Application.DoEvents(); Call(form, "SaveEntries", VaultSaveEntries(session)); PumpUntil(() => entered.IsSet);
                    form.CancelCurrent(); int frozenClocks = clocks; now = now.AddDays(1);
                    bool current = form.CheckCurrent(), expiry = form.CheckExpiry(now); Call(form, "OnActivated", EventArgs.Empty);
                    Check(!current && !expiry && clocks == frozenClocks && notifications == 0 && !lease.Revoked && form.IsBusy &&
                        form.Visible && !form.Work.IsCompleted && VaultExpiryHidden(form),
                        "cancelled owner freezes clock and expiry callbacks until its actual pending save settles");
                    release.Set(); VaultSaveWait(form);
                } finally { release.Set(); }
            }
        }
        private static void VaultExpiryDuringRowsAdded(SettingsService settings)
        {
            var lease = new VaultExpiryLease(); var session = VaultExpirySession(lease); int notifications = 0; bool invoked = false;
            using (var clipboard = new ClipboardService(settings))
            using (var form = new VaultForm(session, clipboard, settings, clock: () => lease.Day, expired: (value, local) => notifications++)) {
                form.Show(); Application.DoEvents(); var grid = (DataGridView)Field(form, "grid");
                grid.RowsAdded += delegate {
                    if (invoked) return;
                    invoked = true; form.CheckExpiry(lease.Day.AddDays(1));
                };
                Call(form, "SaveEntries", VaultSaveEntries(session)); VaultSaveWait(form);
                Check(invoked && notifications == 1 && VaultUiDropped(session) && VaultExpiryHidden(form) && !form.Visible &&
                    !form.PresentationWork.IsFaulted && Field(VaultSaveResult(form), "Data") == null,
                    "expiry inside native RowsAdded stops adoption and clears rows after the reentrant grid callback settles");
            }
            lease.Saved = null;
        }
        private static void VaultExpiryPendingOpen()
        {
            var lease = new VaultExpiryLease(); var candidate = VaultExpirySession(lease); var now = lease.Day;
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var form = new VaultUnlockForm(token => VaultReady(), (creating, pin, token) => {
                entered.Set(); VaultSaveHeld(release); return VaultOpenResult.Succeeded(candidate);
            }, clock: () => now)) {
                try {
                    form.Show(); VaultWaitPresentation(form); VaultTypeAndSubmit(form, "4321"); PumpUntil(() => entered.IsSet);
                    Check(!candidate.IsCurrent(now.AddDays(1)), "held open candidate observes changed day before its fake UI adoption");
                    now = lease.Day; release.Set(); VaultWaitPresentation(form);
                    Check(form.AcceptedSession == null && form.Visible && VaultUiDropped(candidate) &&
                        ((Label)Field(form, "status")).Text == VaultOpenResult.Failed(VaultOpenStatus.DateChanged).ErrorMessage,
                        "held open candidate stays rejected after observed date reversal and drops unadopted fake data");
                } finally { release.Set(); }
            }
        }
    }
}

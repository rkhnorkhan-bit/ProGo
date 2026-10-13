using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        private static VaultProbeResult VaultReady()
        { return new VaultProbeResult(VaultProbeStatus.Ready); }
        private sealed class VaultUiLease : IVaultSessionPersistence
        {
            private readonly DateTime day;
            internal VaultUiLease(DateTime day) { this.day = day.Date; }
            public bool IsCurrent(DateTime localNow) { return localNow.Date == day; }
            public void Save(VaultData data, string pin) { throw new InvalidOperationException("UI fixture never saves"); }
        }
        private static bool VaultUiDropped(VaultSession session)
        { return session.IsDiscarded && session.Pin == null && session.Persistence == null && session.StoragePath == null && session.Data.entries.Count == 0; }
        private static VaultSession VaultUiSession()
        {
            var data = VaultData.Empty(); var entry = VaultEntry.New();
            entry.name = "Синтетическая запись"; entry.secret = "synthetic-ui-secret"; data.entries.Add(entry);
            return new VaultSession(data, "1234", false);
        }
        private static void VaultWaitPresentation(VaultUnlockForm form)
        {
            PumpUntil(() => form.PresentationWork != null && form.PresentationWork.IsCompleted);
            form.PresentationWork.GetAwaiter().GetResult();
        }
        private static void VaultTypeAndSubmit(VaultUnlockForm form, string value = "1234")
        {
            ((TextBox)Field(form, "pin")).Text = value;
            ((Button)Field(form, "primary")).PerformClick();
        }
        private static void VaultHeldRelease(ManualResetEventSlim release)
        { if (!release.Wait(15000)) throw new TimeoutException("owned vault UI fixture deadline"); }

        private static void VaultAsyncUnlock()
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") {
                Console.WriteLine("SKIP: asynchronous vault UI fixtures require isolated native CI"); return;
            }
            var previousContext = SynchronizationContext.Current;
            bool previousCrossThread = Control.CheckForIllegalCrossThreadCalls;
            try {
                Control.CheckForIllegalCrossThreadCalls = true;
                VaultUnlockHeartbeat(); VaultUnlockProbeAndCreate(); VaultUnlockInputFeedback(); VaultUnlockRetry(); VaultUnlockOwnerGates();
                VaultUnlockLeaseAndClock(); VaultUnlockReentrantOwners(); VaultUnlockCreateRace();
                foreach (string action in new[] { "button", "escape", "title-close", "dispose" }) VaultUnlockCancelled(action, false);
                VaultUnlockCancelled("escape", true);
            } finally {
                SynchronizationContext.SetSynchronizationContext(previousContext);
                Control.CheckForIllegalCrossThreadCalls = previousCrossThread;
            }
        }

        private static void VaultUnlockHeartbeat()
        {
            var previousContext = SynchronizationContext.Current;
            int owner = Thread.CurrentThread.ManagedThreadId, workerThread = owner, calls = 0, ticks = 0, wrongThread = 0, starts = 0;
            var day = new DateTime(2026, 10, 12); var template = VaultUiSession();
            var candidate = new VaultSession(template.Data, template.Pin, false, new VaultUiLease(day));
            template.DropSensitive();
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var heartbeat = new System.Windows.Forms.Timer { Interval = 20 })
            using (var form = new VaultUnlockForm(token => VaultReady(), (creating, pin, token) => {
                workerThread = Thread.CurrentThread.ManagedThreadId; Interlocked.Increment(ref calls);
                entered.Set(); VaultHeldRelease(release); return VaultOpenResult.Succeeded(candidate);
            }, workStarted: task => { if (Thread.CurrentThread.ManagedThreadId != owner) wrongThread++; starts++; }, clock: () => day.AddHours(12))) {
                try {
                    form.Show(); VaultWaitPresentation(form);
                    foreach (var control in Descendants(form)) control.TextChanged += delegate {
                        if (Thread.CurrentThread.ManagedThreadId != owner) Interlocked.Increment(ref wrongThread);
                    };
                    heartbeat.Tick += delegate { ticks++; }; heartbeat.Start();
                    SynchronizationContext.SetSynchronizationContext(null); VaultTypeAndSubmit(form);
                    ((Button)Field(form, "primary")).PerformClick(); Call(form, "Submit");
                    PumpUntil(() => entered.IsSet && ticks >= 3);
                    Check(workerThread != owner && calls == 1 && starts == 2 && wrongThread == 0,
                        "vault probe and single-flight open run off UI; work tracking stays on owner after context loss");
                    Check(form.IsBusy && !form.Work.IsCompleted && form.AcceptedSession == null && form.DialogResult == DialogResult.None,
                        "held durable fake commit keeps candidate out of accepted session and dialog result");
                    Check(!((TextBox)Field(form, "pin")).Enabled && !((Button)Field(form, "primary")).Enabled &&
                        ((Button)Field(form, "cancel")).Enabled, "vault busy state preserves cancellation and blocks another PIN");
                    Check(((TextBox)Field(form, "pin")).Text == "" && ((TextBox)Field(form, "pin")).UseSystemPasswordChar &&
                        ((Label)Field(form, "status")).AccessibilityObject.Name == "Состояние открытия хранилища",
                        "busy vault clears captured PIN from masked input and exposes accessible status");
                    Shot(form, "vault-unlock-pending");
                    string pending = ((Label)Field(form, "status")).Text;
                    ((TextBox)Field(form, "pin")).Text = "5678";
                    ((TextBox)Field(form, "confirm")).Text = "5678";
                    Check(((Label)Field(form, "status")).Text == pending && calls == 1 && form.IsBusy,
                        "programmatic edits of frozen vault inputs cannot replace the pending status or start work");
                    release.Set(); VaultWaitPresentation(form);
                    Check(form.Work.IsCompleted && ReferenceEquals(form.AcceptedSession, candidate) && form.DialogResult == DialogResult.OK && !form.Visible,
                        "only settled current session closes vault unlock successfully");
                    Check(wrongThread == 0 && candidate.Data != null && candidate.Pin != null,
                        "successful owner publication preserves adopted data and never updates controls on worker");
                } finally { release.Set(); SynchronizationContext.SetSynchronizationContext(previousContext); }
            }
            candidate.DropSensitive();
        }

        private static void VaultUnlockProbeAndCreate()
        {
            int probes = 0, opens = 0; bool creatingSeen = false;
            using (var form = new VaultUnlockForm(token => Interlocked.Increment(ref probes) == 1
                ? new VaultProbeResult(VaultProbeStatus.FileUnavailable) : new VaultProbeResult(VaultProbeStatus.CreateRequired),
                (creating, pin, token) => { creatingSeen = creating; Interlocked.Increment(ref opens); return VaultOpenResult.Succeeded(VaultUiSession()); })) {
                form.Show(); VaultWaitPresentation(form);
                var pin = (TextBox)Field(form, "pin"); var confirm = (TextBox)Field(form, "confirm");
                var primary = (Button)Field(form, "primary"); var status = (Label)Field(form, "status");
                Check(opens == 0 && !pin.Enabled && primary.Enabled && primary.Text == "Повторить проверку" && form.AcceptedSession == null,
                    "failed vault probe never offers implicit creation or runs authentication");
                Shot(form, "vault-unlock-unavailable"); primary.PerformClick(); VaultWaitPresentation(form);
                Check(probes == 2 && pin.Enabled && confirm.Enabled && confirm.Visible && primary.Text == "Создать",
                    "explicit probe retry enables named masked creation confirmation after genuine missing result");
                Check(confirm.AccessibilityObject.Name == "Повторите PIN-код" && confirm.UseSystemPasswordChar &&
                    primary.DialogResult == DialogResult.None && ((Button)Field(form, "cancel")).DialogResult == DialogResult.None,
                    "async vault controls retain masked accessible input and never auto-close a pending action");
                pin.Text = "1234"; confirm.Text = "4321"; primary.PerformClick();
                Check(opens == 0 && form.Visible && status.Text == "PIN-коды не совпадают.",
                    "creation confirmation mismatch stays inline and never starts a worker");
                confirm.Text = "1234";
                Check(opens == 0 && !form.IsBusy && status.Text.Contains("повторите его") &&
                    status.AccessibleDescription == status.Text,
                    "editing confirmation removes the stale local mismatch and restores accessible creation instructions without work");
                var probeWork = form.Work; var probePresentation = form.PresentationWork;
                pin.Text = "12x4"; confirm.Text = "12x4"; primary.PerformClick();
                Check(opens == 0 && ReferenceEquals(form.Work, probeWork) && ReferenceEquals(form.PresentationWork, probePresentation) &&
                    status.Text == "PIN-код должен содержать ровно 4 цифры." && status.AccessibleDescription == status.Text && pin.Focused,
                    "invalid PIN syntax keeps one accessible precise format error and starts no worker");
                Check(Descendants(form).OfType<Label>().Count(label => label.Visible && label.Text.Contains("4 цифры")) == 1,
                    "creation format error does not repeat the four-digit rule in a persistent hint");
                Shot(form, "vault-unlock-create-invalid");
                pin.Text = confirm.Text = "1234";
                Check(opens == 0 && ReferenceEquals(form.Work, probeWork) && !form.IsBusy &&
                    status.Text.Contains("повторите его") && !status.Text.Contains("должен содержать") &&
                    status.AccessibleDescription == status.Text &&
                    Descendants(form).OfType<Label>().Count(label => label.Visible && label.Text.Contains("4 цифр")) == 1,
                    "corrected creation input restores one four-digit instruction before the native create screenshot without implicit submission");
                Shot(form, "vault-unlock-create"); primary.PerformClick(); VaultWaitPresentation(form);
                Check(opens == 1 && creatingSeen && form.AcceptedSession != null && form.DialogResult == DialogResult.OK,
                    "only confirmed create mode is passed to the worker and adopted after settlement");
                form.AcceptedSession.DropSensitive();
            }
        }

        private static void VaultUnlockInputFeedback()
        {
            int opens = 0; bool startAllowed = true;
            using (var form = new VaultUnlockForm(token => VaultReady(), (creating, value, token) => {
                Interlocked.Increment(ref opens); return VaultOpenResult.Failed(VaultOpenStatus.WrongPin);
            }, () => startAllowed)) {
                form.Show(); VaultWaitPresentation(form);
                var pin = (TextBox)Field(form, "pin"); var primary = (Button)Field(form, "primary");
                var status = (Label)Field(form, "status"); var probeWork = form.Work;
                pin.Text = "12x4"; primary.PerformClick();
                Check(opens == 0 && ReferenceEquals(form.Work, probeWork) &&
                    status.Text == "PIN-код должен содержать ровно 4 цифры." && status.AccessibleDescription == status.Text,
                    "unlock format errors are accessible and do not start authentication");
                pin.Text = "1";
                Check(opens == 0 && ReferenceEquals(form.Work, probeWork) && !form.IsBusy &&
                    status.Text.Contains("для открытия") && status.Text.Contains("4 цифр") && status.AccessibleDescription == status.Text,
                    "editing even an unfinished PIN restores instructions without inline validation or work");
                primary.PerformClick(); startAllowed = false; primary.PerformClick();
                string blocked = status.Text; pin.Text = "1234";
                Check(opens == 0 && ReferenceEquals(form.Work, probeWork) && blocked.Contains("текущей операции ProGo") &&
                    status.Text == blocked && status.AccessibleDescription == blocked,
                    "input editing cannot erase an owner gate message that replaced an earlier local format error");
                startAllowed = true; primary.PerformClick(); VaultWaitPresentation(form);
                Check(opens == 1 && status.Text == "Неверный пин-код",
                    "corrected input still requires explicit submission and preserves the authentication error");
                pin.Text = "5678";
                Check(opens == 1 && status.Text == "Неверный пин-код" && status.AccessibleDescription == status.Text,
                    "editing a rejected PIN does not erase the settled authentication error");
            }
            foreach (string action in new[] { "edit", "dispose" }) {
                int calls = 0;
                using (var form = new VaultUnlockForm(token => VaultReady(), (creating, value, token) => {
                    Interlocked.Increment(ref calls); return VaultOpenResult.Failed(VaultOpenStatus.WrongPin);
                })) {
                    form.Show(); VaultWaitPresentation(form);
                    var pin = (TextBox)Field(form, "pin"); var status = (Label)Field(form, "status"); var probeWork = form.Work;
                    status.TextChanged += delegate {
                        if (status.Text != "PIN-код должен содержать ровно 4 цифры.") return;
                        if (action == "edit") pin.Text = "1234";
                        else form.Dispose();
                    };
                    pin.Text = "12x4"; ((Button)Field(form, "primary")).PerformClick();
                    Check(calls == 0 && ReferenceEquals(form.Work, probeWork) && form.AcceptedSession == null &&
                        (action == "edit" ? form.Visible && status.Text.Contains("для открытия") && status.AccessibleDescription == status.Text : form.IsDisposed),
                        "reentrant local-error feedback cannot focus disposed input or start a worker: " + action);
                }
            }
        }

        private static void VaultUnlockRetry()
        {
            int calls = 0;
            using (var form = new VaultUnlockForm(token => VaultReady(), (creating, value, token) => {
                int request = Interlocked.Increment(ref calls);
                if (request == 1) return VaultOpenResult.Failed(VaultOpenStatus.WrongPin);
                if (request == 2) throw new InvalidOperationException("synthetic-private-exception-and-path");
                return VaultOpenResult.Succeeded(VaultUiSession());
            })) {
                form.Show(); VaultWaitPresentation(form); VaultTypeAndSubmit(form); VaultWaitPresentation(form);
                Check(form.Visible && form.AcceptedSession == null && !form.IsBusy && ((TextBox)Field(form, "pin")).Enabled &&
                    ((Label)Field(form, "status")).Text == "Неверный пин-код", "wrong PIN keeps the same vault dialog available for explicit retry");
                var pin = (TextBox)Field(form, "pin"); var primary = (Button)Field(form, "primary"); var cancel = (Button)Field(form, "cancel");
                KeyboardWalk(form, new Control[] { pin, primary, cancel }, "async vault unlock retry"); Shot(form, "vault-unlock-wrong-pin");
                VaultTypeAndSubmit(form, "5678"); VaultWaitPresentation(form);
                Check(calls == 2 && form.Visible && form.AcceptedSession == null &&
                    !String.Join(" ", Descendants(form).Select(c => c.Text)).Contains("synthetic-private-exception-and-path"),
                    "worker exceptions expose a static vault error without private exception details");
                string failed = ((Label)Field(form, "status")).Text; pin.Text = "1234";
                Check(calls == 2 && ((Label)Field(form, "status")).Text == failed && failed.Contains("Не удалось открыть") &&
                    ((Label)Field(form, "status")).AccessibleDescription == failed,
                    "editing after a worker failure preserves its accessible retry error until explicit submission");
                VaultTypeAndSubmit(form); VaultWaitPresentation(form);
                Check(calls == 3 && form.AcceptedSession != null && form.DialogResult == DialogResult.OK,
                    "ordinary real-PIN retry can succeed after rejected and failed attempts");
                form.AcceptedSession.DropSensitive();
            }
        }

        private static void VaultUnlockOwnerGates()
        {
            int owner = Thread.CurrentThread.ManagedThreadId, wrongThread = 0, calls = 0;
            bool startAllowed = false, publishAllowed = false; VaultSession returned = null;
            using (var form = new VaultUnlockForm(token => VaultReady(), (creating, pin, token) => {
                Interlocked.Increment(ref calls); returned = VaultUiSession(); return VaultOpenResult.Succeeded(returned);
            }, () => { if (Thread.CurrentThread.ManagedThreadId != owner) wrongThread++; return startAllowed; },
                () => { if (Thread.CurrentThread.ManagedThreadId != owner) wrongThread++; return publishAllowed; })) {
                form.Show(); Application.DoEvents();
                Check(form.Work == null && !form.IsBusy && form.AcceptedSession == null,
                    "application start gate prevents even a mode probe during maintenance");
                startAllowed = true; ((Button)Field(form, "primary")).PerformClick(); VaultWaitPresentation(form);
                VaultTypeAndSubmit(form); VaultWaitPresentation(form);
                Check(calls == 1 && wrongThread == 0 && form.Visible && form.AcceptedSession == null && VaultUiDropped(returned),
                    "owner publication gate drops unadopted data and PIN without an off-thread callback");
                publishAllowed = true; VaultTypeAndSubmit(form); VaultWaitPresentation(form);
                Check(calls == 2 && wrongThread == 0 && form.AcceptedSession != null,
                    "fresh explicit attempt can publish after owner maintenance gate reopens");
                form.AcceptedSession.DropSensitive();
            }
        }

        private static void VaultUnlockLeaseAndClock()
        {
            var real = VaultUiSession(); int clockCalls = 0;
            using (var form = new VaultUnlockForm(token => VaultReady(), (creating, value, token) => VaultOpenResult.Succeeded(real),
                clock: () => { clockCalls++; throw new InvalidOperationException("private-clock-fixture"); })) {
                form.Show(); VaultWaitPresentation(form); VaultTypeAndSubmit(form); VaultWaitPresentation(form);
                Check(clockCalls == 0 && ReferenceEquals(form.AcceptedSession, real) && form.DialogResult == DialogResult.OK,
                    "real session adoption preserves authentication priority independently of broken clock");
            }
            real.DropSensitive();
            var day = new DateTime(2026, 10, 12); DateTime now = day.AddHours(23).AddMinutes(59).AddSeconds(59);
            var template = VaultUiSession(); var oldData = template.Data;
            var fake = new VaultSession(oldData, template.Pin, false, new VaultUiLease(day)); template.DropSensitive();
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var form = new VaultUnlockForm(token => VaultReady(), (creating, value, token) => {
                entered.Set(); VaultHeldRelease(release); return VaultOpenResult.Succeeded(fake);
            }, clock: () => now)) {
                try {
                    form.Show(); VaultWaitPresentation(form); VaultTypeAndSubmit(form); PumpUntil(() => entered.IsSet);
                    now = day.AddDays(1); release.Set(); VaultWaitPresentation(form);
                    Check(form.Visible && form.AcceptedSession == null && VaultUiDropped(fake) && oldData.entries.Count == 1 &&
                        ((Label)Field(form, "status")).Text == VaultOpenResult.Failed(VaultOpenStatus.DateChanged).ErrorMessage,
                        "midnight before owner adoption discards expired fake session without mutating a caller's dataset");
                    form.CancelCurrent();
                } finally { release.Set(); }
            }
        }

        private static void VaultUnlockReentrantOwners()
        {
            foreach (string edge in new[] { "start-dispose", "publish-dispose", "publish-cancel", "clock-dispose" }) {
                VaultUnlockForm form = null; VaultSession returned = null; bool disposeAtStart = false; int opens = 0;
                var day = new DateTime(2026, 10, 12);
                form = new VaultUnlockForm(token => VaultReady(), (creating, pin, token) => {
                    Interlocked.Increment(ref opens); var value = VaultUiSession();
                    returned = edge == "clock-dispose" ? new VaultSession(value.Data, value.Pin, false, new VaultUiLease(day)) : value;
                    return VaultOpenResult.Succeeded(returned);
                }, () => {
                    if (edge == "start-dispose" && disposeAtStart) form.Dispose(); return true;
                }, () => {
                    if (edge == "publish-dispose") form.Dispose();
                    if (edge == "publish-cancel") form.CancelCurrent(); return true;
                }, clock: () => { if (edge == "clock-dispose") form.Dispose(); return day; });
                try {
                    form.Show(); VaultWaitPresentation(form); disposeAtStart = true; VaultTypeAndSubmit(form);
                    if (edge != "start-dispose") VaultWaitPresentation(form);
                    Check(form.AcceptedSession == null && !form.Visible && (edge == "start-dispose" ? opens == 0 : VaultUiDropped(returned)),
                        "reentrant owner callback cannot start or publish after cancellation/disposal: " + edge);
                } finally { form.Dispose(); }
            }
        }

        private static void VaultUnlockCreateRace()
        {
            int probes = 0, creates = 0, opens = 0;
            using (var form = new VaultUnlockForm(token => new VaultProbeResult(Interlocked.Increment(ref probes) == 1
                ? VaultProbeStatus.CreateRequired : VaultProbeStatus.Ready), (creating, pin, token) => {
                    if (creating) { Interlocked.Increment(ref creates); return VaultOpenResult.Failed(VaultOpenStatus.FileUnavailable); }
                    Interlocked.Increment(ref opens); return VaultOpenResult.Succeeded(VaultUiSession());
                })) {
                form.Show(); VaultWaitPresentation(form); ((TextBox)Field(form, "confirm")).Text = "1234";
                VaultTypeAndSubmit(form); VaultWaitPresentation(form);
                Check(creates == 1 && opens == 0 && form.Visible && !((TextBox)Field(form, "pin")).Enabled &&
                    ((Button)Field(form, "primary")).Text == "Повторить проверку", "lost creation race requires a fresh explicit probe");
                ((Button)Field(form, "primary")).PerformClick(); VaultWaitPresentation(form);
                Check(probes == 2 && opens == 0 && ((Button)Field(form, "primary")).Text == "Открыть" && !((TextBox)Field(form, "confirm")).Visible,
                    "fresh probe switches from stale Create to Open without reusing the former PIN");
                VaultTypeAndSubmit(form); VaultWaitPresentation(form);
                Check(opens == 1 && creates == 1 && form.AcceptedSession != null, "new explicit PIN attempt opens a vault created by the competing writer");
                form.AcceptedSession.DropSensitive();
            }
        }

        private static void VaultUnlockCancelled(string action, bool probing)
        {
            int calls = 0, updates = 0, wrongThread = 0, owner = Thread.CurrentThread.ManagedThreadId;
            CancellationToken seen = default(CancellationToken); VaultSession returned = null;
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim()) {
                var form = new VaultUnlockForm(token => {
                    if (probing) { seen = token; Interlocked.Increment(ref calls); entered.Set(); VaultHeldRelease(release); }
                    return VaultReady();
                }, (creating, pin, token) => {
                    seen = token; Interlocked.Increment(ref calls); entered.Set(); VaultHeldRelease(release);
                    returned = VaultUiSession(); return VaultOpenResult.Succeeded(returned);
                });
                try {
                    form.Show();
                    if (!probing) { VaultWaitPresentation(form); VaultTypeAndSubmit(form); }
                    PumpUntil(() => entered.IsSet);
                    foreach (var control in Descendants(form)) control.TextChanged += delegate {
                        updates++; if (Thread.CurrentThread.ManagedThreadId != owner) wrongThread++;
                    };
                    if (action == "button") ((Button)Field(form, "cancel")).PerformClick();
                    else if (action == "escape") typeof(Form).GetMethod("ProcessDialogKey", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(form, new object[] { Keys.Escape });
                    else if (action == "title-close") form.Close();
                    else form.Dispose();
                    int afterCancel = updates;
                    Check(seen.IsCancellationRequested && !form.Work.IsCompleted && form.AcceptedSession == null && calls == 1,
                        "vault cancellation requests token without inventing worker completion: " + action + "/probe=" + probing);
                    if (action != "dispose") Check(form.Visible && form.IsBusy && !((Button)Field(form, "cancel")).Enabled,
                        "busy vault close waits native settlement with a live dialog: " + action + "/probe=" + probing);
                    release.Set(); VaultWaitPresentation(form);
                    Check(form.Work.IsCompleted && form.AcceptedSession == null && !form.Visible && wrongThread == 0,
                        "cancelled or disposed vault owner cannot accept a late successful worker: " + action + "/probe=" + probing);
                    if (returned != null) Check(VaultUiDropped(returned),
                        "late unadopted vault session drops managed sensitive references: " + action);
                    if (action == "dispose") Check(updates == afterCancel,
                        "disposed vault controls receive no late text updates");
                } finally { release.Set(); form.Dispose(); }
            }
        }
    }
}

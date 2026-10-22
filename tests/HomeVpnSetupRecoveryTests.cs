using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace ProGo
{
    // Real DPAPI and native forms, isolated transports; never connects to a VPS.
    internal static class HomeVpnSetupRecoveryTests
    {
        private static Action<bool, string> check;
        private static readonly JavaScriptSerializer Json = new JavaScriptSerializer();
        private static HomeVpnOwner Owner { get { return new HomeVpnOwner { Host = "vpn.example.org", Port = 22, Login = "root", KeyFile = "" }; } }
        private static string PendingPath { get { return Path.Combine(HomeVpnPrivateFiles.Root, HomeVpnSetupRecovery.StorageName + ".dat"); } }
        internal static void Run(Action<bool, string> assert, string token, ClipboardService clipboard, string work)
        {
            check = assert; var saved = new Dictionary<string, byte[]>();
            foreach (string name in new[] { "access", "owner", "home-address", HomeVpnSetupRecovery.StorageName })
                foreach (string suffix in new[] { ".dat", ".dat.new" }) Backup(saved, Path.Combine(HomeVpnPrivateFiles.Root, name + suffix));
            File.Delete(PendingPath);
            string helpers = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scripts", "home-vpn", "server"); Directory.CreateDirectory(helpers);
            foreach (string file in new[] { "home_vpn_setup.py", "ikev2_relay.py", "install-ikev2-relay.sh", "profile_share_setup.py", "profile_share.py", "qrcodegen.py", "QR_LICENSE.txt" }) {
                string target = Path.Combine(helpers, file); Backup(saved, target);
                File.Copy(Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Application.ExecutablePath)), "server", file), target, true);
            }
            try {
                LostResponse(token); StatusGuards(token); DispatchGuards(); CorruptJournal(); NativeSetupSnapshot(clipboard); OwnedConsumption(token); AdminRequestSnapshot(token, work); AdminTransportDispatch(work); OwnedAdminTasks(work);
                // NativeRecovery calls Application.Run/ExitThread. Keep it last:
                // later modeless timer fixtures would inherit an exited UI context.
                NativeRecovery(token, clipboard, work);
                check(!Directory.GetDirectories(HomeVpnPrivateFiles.Root, "admin-*").Any()
                    && !Directory.GetFiles(HomeVpnPrivateFiles.Root, "setup-request.dat.*.new").Any(), "recovery settles its private local work and atomic-save temporary files");
            } finally {
                foreach (var item in saved) { if (item.Value == null) File.Delete(item.Key); else File.WriteAllBytes(item.Key, item.Value); }
            }
        }
        private static void NativeSetupSnapshot(ClipboardService clipboard)
        {
            foreach (string route in new[] { "ready", "cancel-retry", "cancel-edited-retry", "close", "dispose" }) {
                File.Delete(PendingPath); HomeVpnSetupRecovery.Register(Owner, "My iPhone");
                byte[] journal = File.ReadAllBytes(PendingPath);
                using (var relay = new Ikev2RelayService())
                using (var service = new HomeVpnService(relay))
                using (var entered = new ManualResetEventSlim())
                using (var release = new ManualResetEventSlim()) {
                    HomeVpnWizardForm form = null; int reads = 0, worker = 0, phase = 0, turns = 0, ui = Thread.CurrentThread.ManagedThreadId;
                    bool closeExpected = route == "close" || route == "dispose", closedAfterRead = false, retry = route.StartsWith("cancel-", StringComparison.Ordinal), editedRetry = route == "cancel-edited-retry"; Task<bool> first = null;
                    try {
                        Pump(() => service.InitializationWork.IsCompleted); service.InitializationWork.GetAwaiter().GetResult();
                        typeof(HomeVpnService).GetProperty("Owner", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(service,
                            new HomeVpnOwner { Host = "other-account.example", Port = 2222, Login = "different", KeyFile = "unrelated-key" }, null);
                        var watch = Stopwatch.StartNew();
                        form = new HomeVpnWizardForm(service, clipboard, delegate {
                            Interlocked.Exchange(ref worker, Thread.CurrentThread.ManagedThreadId);
                            int attempt = Interlocked.Increment(ref reads);
                            if (attempt != 1 && !(editedRetry && attempt == 2)) return;
                            entered.Set(); if (!release.Wait(5000)) throw new TimeoutException("Wizard initial setup snapshot fixture deadline");
                        });
                        first = form.InitializationWork;
                        check(watch.ElapsedMilliseconds < 500, "wizard constructor returns before held private setup read: " + route);
                        form.FormClosed += delegate { closedAfterRead = form.SetupReadWork.IsCompleted && !(bool)Field(form, "busy"); };
                        HomeVpnWaitingTests.ModalWizardCase(form, delegate {
                            if (phase == 0) {
                                if (!entered.IsSet || ++turns < 3) return false;
                                check(form.Modal && form.Visible && worker != ui && !first.IsCompleted && !((Button)Field(form, "next")).Enabled &&
                                    ((Button)Field(form, "cancelWait")).DialogResult == DialogResult.None && journal.SequenceEqual(File.ReadAllBytes(PendingPath)),
                                    "real modal initial setup read keeps UI responsive and owns unchanged request bytes: " + route);
                                phase = 1; turns = 0;
                                if (route == "close") form.Close();
                                else if (route == "dispose") form.Dispose();
                                else if (retry) ((Button)Field(form, "cancelWait")).PerformClick();
                                return false;
                            }
                            if (phase == 1) {
                                if (++turns < 3 || route == "dispose" && form.Modal) return false;
                                check(!first.IsCompleted && (route == "dispose" ? form.IsDisposed : form.Modal && form.Visible),
                                    "initial read cancel/close retains its physical worker until settlement: " + route);
                                release.Set(); phase = 2; return false;
                            }
                            if (phase == 2) {
                                if (!first.IsCompleted || !form.SetupReadWork.IsCompleted || (bool)Field(form, "busy")) return false;
                                bool loaded = first.GetAwaiter().GetResult();
                                check(journal.SequenceEqual(File.ReadAllBytes(PendingPath)) && reads == 1 && loaded == (route == "ready"),
                                    "initial setup completion reports only its actual uncancelled snapshot and never changes the request: " + route);
                                if (closeExpected) {
                                    if (route == "close" && form.Modal) return false;
                                    check(route == "dispose" ? form.IsDisposed && Field(form, "setupOwnerSnapshot") == null : closedAfterRead && !form.Visible,
                                        "explicit modal close joins its read; disposed wizard receives no cached owner: " + route); return true;
                                }
                                check(form.Modal && form.Visible, "cancelled initial setup read does not close ShowDialog");
                                if (retry) {
                                    if (editedRetry) { entered.Reset(); release.Reset(); }
                                    while ((int)Field(form, "step") > 0) ((Button)Field(form, "back")).PerformClick();
                                    ((FlowLayoutPanel)Field(form, "body")).Controls.OfType<Button>().Single(button => button.Text == "Добавить свой VPS").PerformClick();
                                    var nextRead = form.SetupSnapshotWork;
                                    check(!Object.ReferenceEquals(first, nextRead), "retry through the actual own-VPS choice owns a new snapshot completion"); turns = 0; phase = editedRetry ? 5 : 3; return false;
                                }
                                phase = 3;
                            }
                            if (phase == 5) {
                                if (!entered.IsSet || ++turns < 3) return false;
                                ((TextBox)Field(form, "host")).Text = "typed-before-result.example"; ((NumericUpDown)Field(form, "port")).Value = 2022;
                                ((TextBox)Field(form, "login")).Text = "typed"; ((TextBox)Field(form, "key")).Text = "explicit-key";
                                check(!form.SetupSnapshotWork.IsCompleted && ((Control)Field(form, "body")).Enabled && !((Button)Field(form, "next")).Enabled, "first successful retry retains editing while its real snapshot is still held");
                                release.Set(); phase = 3; return false;
                            }
                            if (!form.SetupSnapshotWork.IsCompleted) return false;
                            if (phase == 4) {
                                check(form.SetupSnapshotWork.GetAwaiter().GetResult() && ((TextBox)Field(form, "host")).Text == "edited.example" && ((NumericUpDown)Field(form, "port")).Value == 2222
                                    && ((TextBox)Field(form, "login")).Text == "edited" && ((TextBox)Field(form, "key")).Text == "fixture-key" && reads == (retry ? 3 : 2), "explicit recheck preserves edited owner fields and performs exactly one fresh read: " + route); return true;
                            }
                            check(editedRetry ? ((TextBox)Field(form, "host")).Text == "typed-before-result.example" && ((NumericUpDown)Field(form, "port")).Value == 2022 && ((TextBox)Field(form, "login")).Text == "typed" && ((TextBox)Field(form, "key")).Text == "explicit-key"
                                : ((TextBox)Field(form, "host")).Text == Owner.Host && ((NumericUpDown)Field(form, "port")).Value == Owner.Port && ((TextBox)Field(form, "login")).Text == Owner.Login && ((TextBox)Field(form, "key")).Text == "",
                                "first successful snapshot restores pending endpoint without borrowing another VPS key or overwriting newer user edits: " + route);
                            check(form.SetupSnapshotWork.GetAwaiter().GetResult() && (int)Field(form, "step") == 1 && ((Button)Field(form, "next")).Enabled,
                                "ready or retried setup snapshot restores the cached recovery action: " + route);
                            var snapshot = form.SetupSnapshotWork; int count = reads;
                            using (File.Open(PendingPath, FileMode.Open, FileAccess.Read, FileShare.None)) {
                                var fieldsWatch = Stopwatch.StartNew();
                                ((TextBox)Field(form, "host")).Text = "edited.example"; ((TextBox)Field(form, "login")).Text = "edited";
                                ((TextBox)Field(form, "key")).Text = "fixture-key"; ((NumericUpDown)Field(form, "port")).Value = 2222;
                                typeof(HomeVpnWizardForm).GetMethod("RefreshStatus", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(form, null);
                                check(fieldsWatch.ElapsedMilliseconds < 500 && reads == count && Object.ReferenceEquals(snapshot, form.SetupSnapshotWork) && ((Button)Field(form, "next")).Enabled,
                                    "typing and status use only cached setup state even while the actual request file is exclusively locked: " + route);
                            }
                            check(((TextBox)Field(form, "host")).Text == "edited.example" && reads == (retry ? 2 : 1),
                                "old cancelled snapshot cannot overwrite new editable fields or launch parallel reads: " + route);
                            ((Button)Field(form, "back")).PerformClick();
                            ((FlowLayoutPanel)Field(form, "body")).Controls.OfType<Button>().Single(button => button.Text == "Добавить свой VPS").PerformClick();
                            phase = 4; return false;
                        }, () => closeExpected);
                    } finally {
                        release.Set(); if (form != null) form.Dispose();
                        try {
                            Pump(() => form == null || form.InitializationWork.IsCompleted && form.SetupSnapshotWork.IsCompleted && form.SetupReadWork.IsCompleted && ((Task)Field(form, "setupSnapshotDriver")).IsCompleted);
                            if (form != null) { form.InitializationWork.GetAwaiter().GetResult(); form.SetupSnapshotWork.GetAwaiter().GetResult(); }
                        } finally {
                            service.Dispose(); var stop = service.StopAsync(); Pump(() => stop.IsCompleted);
                            check(stop.GetAwaiter().GetResult(), "initial setup snapshot fixture joins only its owned resources: " + route);
                        }
                    }
                }
            }
            File.Delete(PendingPath);
        }
        private static void OwnedConsumption(string token)
        {
            var paths = new[] { "access", "owner", HomeVpnSetupRecovery.StorageName }.Select(name => Path.Combine(HomeVpnPrivateFiles.Root, name + ".dat")).ToArray();
            var previous = paths.ToDictionary(path => path, path => File.Exists(path) ? File.ReadAllBytes(path) : null);
            try { OwnedConsumptionCases(token); }
            finally { foreach (var pair in previous) { if (pair.Value == null) File.Delete(pair.Key); else File.WriteAllBytes(pair.Key, pair.Value); } }
        }
        private static void OwnedConsumptionCases(string token)
        {
            using (var host = new Form())
            using (var heartbeat = new System.Windows.Forms.Timer { Interval = 20 }) {
                int ticks = 0, ui = Thread.CurrentThread.ManagedThreadId; heartbeat.Tick += delegate { ticks++; }; host.Show(); heartbeat.Start();
                try {
                    foreach (string mode in new[] { "complete", "cancel-read", "cancel-consume", "scope-refused", "locked", "changed-request", "changed-credentials" })
                    using (var entered = new ManualResetEventSlim())
                    using (var release = new ManualResetEventSlim())
                    using (var cancellation = new CancellationTokenSource()) {
                        File.Delete(PendingPath); var owner = Owner;
                        HomeVpnPrivateFiles.Save("access", token.Trim()); HomeVpnPrivateFiles.Save("owner", Json.Serialize(owner));
                        var request = HomeVpnSetupRecovery.Register(owner, "My iPhone"); HomeVpnSetupRecovery.RetainResult(owner, request, token);
                        byte[] journal = File.ReadAllBytes(PendingPath), expectedJournal = journal, beforeAccess = File.ReadAllBytes(Path.Combine(HomeVpnPrivateFiles.Root, "access.dat")), beforeOwner = File.ReadAllBytes(Path.Combine(HomeVpnPrivateFiles.Root, "owner.dat"));
                        byte[] expectedAccess = beforeAccess; Task operation = null; FileStream held = null; int worker = 0, allowed = 1, lastStage = 0;
                        var savedOwner = Json.Deserialize<HomeVpnOwner>(HomeVpnPrivateFiles.Load("owner"));
                        var savedRequest = HomeVpnSetupRecovery.Load(owner, "My iPhone");
                        bool accessMatches = HomeVpnPrivateFiles.Load("access") == token.Trim();
                        bool ownerMatches = savedOwner != null && String.Equals(savedOwner.Host, owner.Host, StringComparison.OrdinalIgnoreCase) && savedOwner.Port == owner.Port && savedOwner.Login == owner.Login && savedOwner.KeyFile == owner.KeyFile;
                        bool requestMatches = savedRequest != null && savedRequest.RequestId == request.RequestId && savedRequest.Result == token.Trim();
                        check(accessMatches && ownerMatches && requestMatches, "setup consumption starts from exact real saved inputs: " + mode + "; access=" + accessMatches + "/owner=" + ownerMatches + "/request=" + requestMatches);
                        try {
                            var watch = Stopwatch.StartNew();
                            operation = HomeVpnSetupRecovery.ConfirmConsumedAsync(owner, token, cancellation.Token, () => Volatile.Read(ref allowed) != 0, stage => {
                                Interlocked.Exchange(ref lastStage, stage == "read" ? 1 : stage == "consume" ? 2 : 3);
                                if (stage != (mode == "cancel-read" ? "read" : "consume")) return;
                                Interlocked.Exchange(ref worker, Thread.CurrentThread.ManagedThreadId); entered.Set();
                                if (!release.Wait(5000)) throw new TimeoutException("Owned setup consumption fixture deadline");
                            });
                            check(watch.ElapsedMilliseconds < 500, "setup consumption returns owned work before private receipt/credential I/O: " + mode);
                            int beforeTicks = ticks; var wait = Stopwatch.StartNew();
                            while (!entered.IsSet || ticks < beforeTicks + 3) {
                                if (operation.IsCompleted) {
                                    string failureType = "none"; try { operation.GetAwaiter().GetResult(); } catch (Exception ex) { failureType = ex.GetType().Name; }
                                    throw new InvalidOperationException("Setup consumption settled before its held barrier: mode=" + mode + "; stage=" + Volatile.Read(ref lastStage) + "; entered=" + entered.IsSet + "; ticks=" + (ticks - beforeTicks) + "; status=" + operation.Status + "; failure=" + failureType);
                                }
                                if (wait.ElapsedMilliseconds > 3000) throw new TimeoutException("Setup consumption heartbeat deadline: mode=" + mode + "; stage=" + Volatile.Read(ref lastStage) + "; entered=" + entered.IsSet + "; ticks=" + (ticks - beforeTicks) + "; status=" + operation.Status);
                                Application.DoEvents(); Thread.Sleep(1);
                            }
                            owner.KeyFile = "changed-after-capture";
                            check(worker != ui && !operation.IsCompleted && journal.SequenceEqual(File.ReadAllBytes(PendingPath)), "held real setup consumption retains its original request while native owner UI stays responsive: " + mode);
                            if (mode.StartsWith("cancel-", StringComparison.Ordinal)) cancellation.Cancel();
                            if (mode == "scope-refused") Interlocked.Exchange(ref allowed, 0);
                            if (mode == "locked") held = File.Open(PendingPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                            if (mode == "changed-request") {
                                request.RequestId = new string('d', 32); HomeVpnPrivateFiles.Save(HomeVpnSetupRecovery.StorageName, Json.Serialize(request)); expectedJournal = File.ReadAllBytes(PendingPath);
                            }
                            if (mode == "changed-credentials") { HomeVpnPrivateFiles.Save("access", "changed-private-fixture"); expectedAccess = File.ReadAllBytes(Path.Combine(HomeVpnPrivateFiles.Root, "access.dat")); }
                            release.Set(); JoinConsumption(operation); Exception failure = null; try { operation.GetAwaiter().GetResult(); } catch (Exception ex) { failure = ex; }
                            check(mode == "complete" ? failure == null && !File.Exists(PendingPath) : failure != null && expectedJournal.SequenceEqual(File.ReadAllBytes(PendingPath)), "only exact saved credentials and live admission consume the setup request; cancel/real lock/new request retain it: " + mode);
                            check((mode.StartsWith("cancel-", StringComparison.Ordinal) || mode == "scope-refused" ? failure is OperationCanceledException : mode == "complete" || failure is HomeVpnSetupPendingException)
                                && expectedAccess.SequenceEqual(File.ReadAllBytes(Path.Combine(HomeVpnPrivateFiles.Root, "access.dat"))) && beforeOwner.SequenceEqual(File.ReadAllBytes(Path.Combine(HomeVpnPrivateFiles.Root, "owner.dat"))),
                                "consumption is read/delete only, respects copied owner and never overwrites credentials or starts another setup: " + mode);
                        } finally { release.Set(); cancellation.Cancel(); if (held != null) held.Dispose(); if (operation != null) JoinConsumption(operation); }
                    }
                } finally { heartbeat.Stop(); host.Close(); }
            }
        }
        private static void JoinConsumption(Task operation)
        {
            var watch = Stopwatch.StartNew();
            while (!operation.IsCompleted) { if (watch.ElapsedMilliseconds > 5000) throw new TimeoutException("Owned setup consumption cleanup deadline"); Application.DoEvents(); Thread.Sleep(1); }
            try { operation.GetAwaiter().GetResult(); } catch (Exception) { /* Asserted separately, observed before restoring owned fixture bytes. */ }
        }
        private static void Backup(Dictionary<string, byte[]> saved, string path) { saved[path] = File.Exists(path) ? File.ReadAllBytes(path) : null; }
        private static void AdminRequestSnapshot(string token, string work)
        {
            var saved = new[] { "access", HomeVpnSetupRecovery.StorageName, HomeVpnAdminRecovery.StorageName, HomeVpnShareRecovery.StorageName }
                .ToDictionary(name => Path.Combine(HomeVpnPrivateFiles.Root, name + ".dat"), path => File.Exists(path) ? File.ReadAllBytes(path) : null);
            var access = HomeVpnAccess.Parse(token);
            using (var form = new Form())
            using (var heartbeat = new System.Windows.Forms.Timer { Interval = 20 }) {
                int ticks = 0, ui = Thread.CurrentThread.ManagedThreadId; heartbeat.Tick += delegate { ticks++; }; form.Show(); heartbeat.Start();
                try {
                    foreach (string mode in new[] { "setup", "invite", "share", "empty", "missing-setup", "missing-invite", "missing-share", "pending-invite", "pending-share", "cancel", "corrupt", "missing-key" }) {
                        File.Delete(PendingPath); File.Delete(Path.Combine(HomeVpnPrivateFiles.Root, HomeVpnAdminRecovery.StorageName + ".dat")); File.Delete(Path.Combine(HomeVpnPrivateFiles.Root, HomeVpnShareRecovery.StorageName + ".dat"));
                        HomeVpnPrivateFiles.Save("access", token);
                        var owner = new HomeVpnOwner { Host = access.Host, Port = access.Port, Login = "root", KeyFile = "" };
                        string action = mode == "setup" ? "setup" : mode == "invite" || mode == "share" ? "recover-" + mode
                            : mode.StartsWith("missing-", StringComparison.Ordinal) ? "recover-" + mode.Substring(8)
                            : mode.StartsWith("pending-", StringComparison.Ordinal) ? mode.Substring(8) : mode == "corrupt" ? "recover-setup" : "list";
                        string requestId = null;
                        if (mode == "setup") requestId = HomeVpnSetupRecovery.Register(owner, "My iPhone").RequestId;
                        if (mode == "invite" || mode == "pending-invite") requestId = HomeVpnAdminRecovery.Register(owner, "My iPhone", null).RequestId;
                        if (mode == "share" || mode == "pending-share") requestId = HomeVpnShareRecovery.Register(owner, "profiles.example").RequestId;
                        if (mode == "corrupt") File.WriteAllBytes(PendingPath, new byte[] { 1, 2, 3 });
                        if (mode == "missing-key") owner.KeyFile = Path.Combine(work, "absent-selected-key-" + Guid.NewGuid().ToString("N"));
                        string capturedKey = owner.KeyFile;
                        var before = Directory.GetFiles(HomeVpnPrivateFiles.Root).ToDictionary(path => path, File.ReadAllBytes);
                        Func<Task<IDisposable>> acquire = action.Contains("invite") ? (Func<Task<IDisposable>>)HomeVpnAdminRecovery.AcquireAsync
                            : action.Contains("share") ? (Func<Task<IDisposable>>)HomeVpnShareRecovery.AcquireAsync : HomeVpnSetupRecovery.AcquireAsync;
                        using (var entered = new ManualResetEventSlim())
                        using (var release = new ManualResetEventSlim())
                        using (var cancellation = new CancellationTokenSource()) {
                            IDisposable lease = null; Task<HomeVpnAdminRequestSnapshot> operation = null; int worker = 0;
                            try {
                                lease = acquire().GetAwaiter().GetResult(); int beforeTicks = ticks; var watch = Stopwatch.StartNew();
                                operation = HomeVpnAdminRequestSnapshot.LoadAsync(owner, action, "My iPhone", cancellation.Token, delegate {
                                    Interlocked.Exchange(ref worker, Thread.CurrentThread.ManagedThreadId); entered.Set();
                                    if (!release.Wait(5000)) throw new TimeoutException("Admin request snapshot fixture deadline");
                                });
                                check(watch.ElapsedMilliseconds < 500, "admin request snapshot returns owned read before selected-key/private-file I/O: " + mode);
                                Pump(() => entered.IsSet && ticks >= beforeTicks + 3, 3000);
                                owner.Host = "changed-after-capture.example"; owner.KeyFile = "changed-after-capture";
                                check(worker != ui && !operation.IsCompleted && Failure(acquire()) is InvalidOperationException,
                                    "held admin snapshot keeps UI responsive and caller's existing family lease retained: " + mode);
                                if (mode == "cancel") cancellation.Cancel(); release.Set(); Pump(() => operation.IsCompleted, 5000);
                                HomeVpnAdminRequestSnapshot snapshot = null; Exception failure = null; try { snapshot = operation.GetAwaiter().GetResult(); } catch (Exception ex) { failure = ex; }
                                bool success = mode == "setup" || mode == "invite" || mode == "share" || mode == "empty";
                                if (success) {
                                    var copy = snapshot == null ? null : snapshot.CopyOwner();
                                    check(failure == null && copy != null && copy.Host == access.Host && copy.Port == access.Port && copy.KeyFile == capturedKey && snapshot.Checking == (mode != "empty")
                                        && (mode != "setup" || snapshot.CopySetup().RequestId == requestId) && (mode != "invite" || snapshot.CopyInvite().RequestId == requestId) && (mode != "share" || snapshot.CopyShare().RequestId == requestId),
                                        "admin worker reads the actual matching protected request and original copied owner: " + mode);
                                    copy.Host = "edited-result.example";
                                    check(snapshot.CopyOwner().Host == access.Host, "returned admin owner copies cannot change the retained snapshot: " + mode);
                                } else check(mode == "cancel" ? failure is OperationCanceledException : mode == "missing-key" ? failure is ArgumentException
                                    : action.Contains("invite") ? failure is HomeVpnAdminPendingException : action.Contains("share") ? failure is HomeVpnSharePendingException : failure is HomeVpnSetupPendingException,
                                    "admin snapshot preserves exact missing/pending/corrupt/key/cancel refusal type without a remote action: " + mode);
                                var after = Directory.GetFiles(HomeVpnPrivateFiles.Root);
                                check(before.Count == after.Length && before.All(item => File.Exists(item.Key) && item.Value.SequenceEqual(File.ReadAllBytes(item.Key))),
                                    "read-only admin snapshot never registers/consumes/stages or changes actual protected bytes: " + mode);
                            } finally {
                                release.Set();
                                try { if (operation != null) { Pump(() => operation.IsCompleted, 5000); try { operation.GetAwaiter().GetResult(); } catch (Exception) { } } }
                                finally { if (lease != null) lease.Dispose(); }
                            }
                            using (var released = acquire().GetAwaiter().GetResult()) check(released != null, "caller releases family lease only after actual snapshot settles: " + mode);
                        }
                    }
                } finally { heartbeat.Stop(); form.Close(); foreach (var item in saved) { if (item.Value == null) File.Delete(item.Key); else File.WriteAllBytes(item.Key, item.Value); } }
            }
        }
        private static void AdminTransportDispatch(string work)
        {
            foreach (string route in new[] { "ready", "before-dispose", "during-dispose", "file-fault", "worker-only" })
            using (var form = new Form())
            using (var dispatcher = new HomeProfileUiDispatcher(form))
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var published = new ManualResetEventSlim()) {
                string path = Path.Combine(work, "admin-transport-" + Guid.NewGuid().ToString("N") + ".txt");
                byte[] previous = new byte[] { 1, 2, 3, 4 }; Task physical = null, operation = null; FileStream held = null;
                int ui = Thread.CurrentThread.ManagedThreadId, startupThread = 0, ioThread = 0, starts = 0, phase = 0, turns = 0;
                bool closeExpected = route == "before-dispose" || route == "during-dispose";
                var originalContext = SynchronizationContext.Current;
                try {
                    if (route == "file-fault") { File.WriteAllBytes(path, previous); held = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None); }
                    Func<Task> start = delegate {
                        Interlocked.Exchange(ref startupThread, Thread.CurrentThread.ManagedThreadId); Interlocked.Increment(ref starts);
                        physical = Task.Run(delegate {
                            if (!published.Wait(5000)) throw new TimeoutException("Admin transport publication fixture deadline");
                            Interlocked.Exchange(ref ioThread, Thread.CurrentThread.ManagedThreadId); entered.Set();
                            if (!release.Wait(5000)) throw new TimeoutException("Admin transport physical fixture deadline");
                            File.WriteAllText(path, "actual owned local transport");
                        });
                        // The worker must not expose entry before its actual Task
                        // is assigned, including the worker-only transport case.
                        published.Set(); return physical;
                    };
                    HomeVpnWaitingTests.ModalWizardCase(form, delegate {
                        if (phase == 0) {
                            check(form.Modal, "admin transport contract runs inside actual ShowDialog: " + route);
                            SynchronizationContext.SetSynchronizationContext(null);
                            if (route == "before-dispose") form.Dispose();
                            operation = Task.Run(() => HomeVpnService.DispatchAdminTransportAsync(route == "worker-only" ? null : dispatcher, start));
                            phase = 1; return false;
                        }
                        if (route == "before-dispose") {
                            if (!operation.IsCompleted) return false;
                            check(Failure(operation) is OperationCanceledException && starts == 0 && physical == null && !File.Exists(path) && dispatcher.PublicationCount == 0,
                                "closed dispatcher refuses a new transport before startup without a late callback or file I/O"); return true;
                        }
                        if (phase == 1) {
                            if (!entered.IsSet || ++turns < 3) return false;
                            check(starts == 1 && physical != null && !physical.IsCompleted && !operation.IsCompleted && ioThread != ui
                                && (route == "worker-only" ? startupThread != ui && dispatcher.PublicationCount == 0 : startupThread == ui && dispatcher.PublicationCount == 1),
                                "admin transport starts once on its explicit owner and retains real worker I/O with lost UI context: " + route);
                            if (route == "during-dispose") form.Dispose();
                            turns = 0; phase = 2; return false;
                        }
                        if (phase == 2) {
                            if (++turns < 3) return false;
                            check(!physical.IsCompleted && !operation.IsCompleted && (route == "during-dispose" ? form.IsDisposed : form.Visible && form.Modal),
                                "owner loss never completes or discards an already admitted transport task: " + route);
                            release.Set(); phase = 3; return false;
                        }
                        if (!operation.IsCompleted || !physical.IsCompleted) return false;
                        Exception failure = null; try { operation.GetAwaiter().GetResult(); } catch (Exception ex) { failure = ex; }
                        if (held != null) { held.Dispose(); held = null; }
                        check(route == "file-fault" ? failure is IOException && previous.SequenceEqual(File.ReadAllBytes(path))
                            : failure == null && File.ReadAllText(path) == "actual owned local transport",
                            "admitted admin transport preserves actual success or original native file error despite later owner loss: " + route);
                        check(starts == 1 && dispatcher.PublicationCount == (route == "worker-only" ? 0 : 1), "settled transport performs no late UI dispatch or duplicate action: " + route);
                        return true;
                    }, () => closeExpected);
                } finally {
                    published.Set(); release.Set(); form.Dispose();
                    try {
                        if (operation != null) { Pump(() => operation.IsCompleted, 5000); try { operation.GetAwaiter().GetResult(); } catch (OperationCanceledException) { } catch (IOException) { } }
                        if (physical != null) { Pump(() => physical.IsCompleted, 5000); try { physical.GetAwaiter().GetResult(); } catch (IOException) { } }
                    } finally { if (held != null) held.Dispose(); if (physical == null || physical.IsCompleted) File.Delete(path); SynchronizationContext.SetSynchronizationContext(originalContext); }
                }
            }
        }
        private static void OwnedAdminTasks(string work)
        {
            using (var form = new Form())
            using (var pulse = new System.Windows.Forms.Timer { Interval = 20 }) {
                int ticks = 0, ui = Thread.CurrentThread.ManagedThreadId; pulse.Tick += delegate { ticks++; }; form.Show(); pulse.Start();
                try {
                    foreach (string mode in new[] { "complete", "stop", "dispose", "lock", "typed", "queued-stop", "queued-dispose", "multiple" })
                    using (var relay = new Ikev2RelayService())
                    using (var service = new HomeVpnService(relay, true))
                    using (var entered = new ManualResetEventSlim())
                    using (var release = new ManualResetEventSlim()) {
                        string path = Path.Combine(work, "admin-owned-" + Guid.NewGuid().ToString("N") + ".txt"), secondPath = path + ".second";
                        var previous = new byte[] { 4, 3, 2, 1 }; Task<string> operation = null, second = null; Task<bool> stop = null; FileStream held = null;
                        int factories = 0, worker = 0; bool queued = mode.StartsWith("queued-", StringComparison.Ordinal), multiple = mode == "multiple";
                        try {
                            Pump(() => service.InitializationWork.IsCompleted, 5000); service.InitializationWork.GetAwaiter().GetResult();
                            var before = Directory.Exists(HomeVpnPrivateFiles.Root) ? Directory.GetFiles(HomeVpnPrivateFiles.Root).ToDictionary(p => p, File.ReadAllBytes) : new Dictionary<string, byte[]>();
                            if (mode == "lock") { File.WriteAllBytes(path, previous); held = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None); }
                            Action wait = delegate {
                                Interlocked.Exchange(ref worker, Thread.CurrentThread.ManagedThreadId); entered.Set();
                                if (!release.Wait(5000)) throw new TimeoutException("Owned Admin fixture deadline");
                            };
                            Func<string, Task<string>> body = destination => {
                                int count = Interlocked.Increment(ref factories);
                                if (!multiple || count == 2) wait();
                                else if (!release.Wait(5000)) throw new TimeoutException("Second owned Admin fixture deadline");
                                if (mode == "typed") throw new HomeVpnPreparationCancelledException(true);
                                File.WriteAllText(destination, "actual owned Admin file"); return Task.FromResult(destination);
                            };
                            int beforeTicks = ticks; var watch = Stopwatch.StartNew();
                            operation = service.RunAdminOwnedAsync(() => body(path), queued ? wait : null);
                            if (multiple) second = service.RunAdminOwnedAsync(() => body(secondPath));
                            check(watch.ElapsedMilliseconds < 500 && service.IsAdminWorking, "Admin factory task is published before scheduling or local file work: " + mode);
                            Pump(() => entered.IsSet && ticks >= beforeTicks + 3, 3000);
                            check(worker != ui && !operation.IsCompleted && service.HasOwnedResources && factories == (queued ? 0 : multiple ? 2 : 1),
                                "held Admin factory runs off UI, keeps native heartbeat and tracks every accepted body: " + mode);
                            if (mode.Contains("stop") || mode.Contains("dispose") || multiple || mode == "lock" || mode == "typed") {
                                watch.Restart(); if (mode.Contains("dispose")) service.Dispose(); stop = service.StopAsync();
                                check(watch.ElapsedMilliseconds < 500 && !stop.IsCompleted && Object.ReferenceEquals(stop, service.StopAsync()),
                                    "public Stop/Dispose retains one drain until actual Admin bodies settle: " + mode);
                                int late = 0; var refused = service.RunAdminOwnedAsync(() => { late++; return Task.FromResult("unstarted"); });
                                check(Failure(refused) is OperationCanceledException && late == 0, "pending Admin drain refuses a new factory without running it: " + mode);
                            }
                            release.Set(); Pump(() => operation.IsCompleted && (second == null || second.IsCompleted) && (stop == null || stop.IsCompleted), 5000);
                            Exception failure = null; string result = null; try { result = operation.GetAwaiter().GetResult(); } catch (Exception ex) { failure = ex; }
                            if (held != null) { held.Dispose(); held = null; }
                            if (queued) check(failure is OperationCanceledException && factories == 0 && !File.Exists(path), "queued Stop/Dispose prevents the old unadmitted factory from later file work: " + mode);
                            else if (mode == "lock") check(failure is IOException && previous.SequenceEqual(File.ReadAllBytes(path)), "actual locked Admin output preserves original file and exact body failure");
                            else if (mode == "typed") check(failure is HomeVpnPreparationCancelledException && failure.Message.Contains("Прежняя команда"), "owned Admin task preserves the original typed recovery cancellation rather than generic cancellation");
                            else check(failure == null && result == path && File.ReadAllText(path) == "actual owned Admin file"
                                && (second == null || second.GetAwaiter().GetResult() == secondPath && File.ReadAllText(secondPath) == "actual owned Admin file"),
                                "accepted Admin body outcome stays honest after Stop/Dispose and all accepted families drain: " + mode);
                            if (stop != null) check(stop.GetAwaiter().GetResult(), "Admin body error/outcome cannot lose the service Stop owner: " + mode);
                            check(!service.IsAdminWorking && !service.HasOwnedResources && !relay.IsRunning && before.All(item => item.Value.SequenceEqual(File.ReadAllBytes(item.Key))),
                                "settled Admin accounting leaves no pending work, late relay or altered protected bytes: " + mode);
                        } finally {
                            release.Set(); if (held != null) held.Dispose(); service.Dispose(); var cleanup = service.StopAsync();
                            Pump(() => cleanup.IsCompleted && (operation == null || operation.IsCompleted) && (second == null || second.IsCompleted), 5000);
                            if (!cleanup.GetAwaiter().GetResult()) throw new Exception("Owned Admin task cleanup incomplete");
                            foreach (var task in new[] { operation, second }) if (task != null) try { task.GetAwaiter().GetResult(); } catch (OperationCanceledException) { } catch (IOException) { }
                            File.Delete(path); File.Delete(secondPath);
                        }
                    }
                } finally { pulse.Stop(); form.Close(); }
            }
        }
        private static void Pump(Func<bool> ready, int timeout = 12000)
        {
            var watch = Stopwatch.StartNew(); while (!ready()) { Application.DoEvents(); Thread.Sleep(10); if (watch.ElapsedMilliseconds > timeout) throw new Exception("Setup recovery fixture timeout"); } Application.DoEvents();
        }
        private static Exception Failure(Task task)
        {
            Pump(() => task.IsCompleted);
            try { task.GetAwaiter().GetResult(); } catch (Exception ex) { return ex; }
            throw new Exception("Expected recovery refusal");
        }
        private static HomeVpnSetupRequest Pending() { return HomeVpnSetupRecovery.Load(Owner, "My iPhone"); }
        private static Task<string> Admin(string action, Func<string, string, string, Task> transport, Func<string, string, Task> copy = null, HomeVpnOwner owner = null, string label = "My iPhone")
        {
            return HomeVpnService.AdminAsync(owner ?? Owner, action, label, null, delegate { }, copy ?? ((exe, args) => Task.FromResult(0)), transport);
        }
        private static Dictionary<string, object> Status(HomeVpnSetupRequest request, string state = "succeeded", bool available = true)
        {
            return new Dictionary<string, object> { { "Version", 1 }, { "RequestId", request.RequestId }, { "Action", state == "not-found" ? null : "setup" },
                { "State", state }, { "Started", state == "not-found" ? null : "2026-01-01T00:00:00.123456+00:00" },
                { "Finished", state == "running" || state == "not-found" ? null : "2026-01-01T00:00:01+00:00" }, { "ResultAvailable", available } };
        }
        private static Task Write(string path, string value) { File.WriteAllText(path, value, new UTF8Encoding(false)); return Task.FromResult(0); }
        private static void LostResponse(string token)
        {
            File.Delete(PendingPath); int copies = 0, issued = 0; string id = null;
            using (var relay = new Ikev2RelayService())
            using (var service = new HomeVpnService(relay)) {
                service.UseToken(token, null); byte[] before = File.ReadAllBytes(Path.Combine(HomeVpnPrivateFiles.Root, "access.dat"));
                var task = Admin("setup", (exe, args, output) => {
                    var request = Pending(); id = request.RequestId; issued++;
                    check(exe == "ssh.exe" && args.Contains("setup --host") && args.Contains("--request-id " + id)
                        && request.Result == null, "stable setup request is protected and saved before SSH dispatch");
                    check(!args.Contains(token) && !args.Contains(HomeVpnAccess.Parse(token).Password), "setup command arguments do not contain the VPN access");
                    throw new IOException("fixture-secret-response");
                }, (exe, args) => { copies++; check(!HomeVpnSetupRecovery.HasPending(), "fresh copy precedes registration; cancellation can leave no pending setup"); return Task.FromResult(0); });
                var error = Failure(task);
                check(error is HomeVpnSetupPendingException && !error.Message.Contains("fixture-secret") && Pending().RequestId == id && Pending().Result == null,
                    "lost SSH response preserves the original ID and displays a fixed recovery instruction");
                check(before.SequenceEqual(File.ReadAllBytes(Path.Combine(HomeVpnPrivateFiles.Root, "access.dat"))) && service.Owner == null,
                    "unknown setup never replaces the saved VPN access");
                string protectedText = Encoding.UTF8.GetString(File.ReadAllBytes(PendingPath));
                check(!protectedText.Contains(token) && !protectedText.Contains(Owner.Host) && !protectedText.Contains(id)
                    && Directory.GetAccessControl(HomeVpnPrivateFiles.Root).AreAccessRulesProtected, "pending metadata is DPAPI-protected in the private credential directory");
                var calls = new List<string>();
                task = Admin("recover-setup", (exe, args, output) => {
                    calls.Add(args); check(args.Contains("--request-id " + id) && !args.Contains("setup --host"), "recovery queries keep the original request and cannot submit setup");
                    return Write(output, args.Contains("operation-status") ? Json.Serialize(Status(Pending())) : token);
                }); Pump(() => task.IsCompleted);
                check(task.GetAwaiter().GetResult() == token && issued == 1 && copies == 1 && calls.Count == 2
                    && calls[0].Contains("operation-status") && calls[1].Contains("operation-result"), "lost setup recovers its original token without a second issuance");
                check(Pending().Result == token && !Encoding.UTF8.GetString(File.ReadAllBytes(PendingPath)).Contains("PROGO1."),
                    "validated result remains privately cached until local access consumption");
                bool premature = false; try { HomeVpnSetupRecovery.ConfirmConsumed(Owner, token); } catch (HomeVpnSetupPendingException) { premature = true; }
                check(premature && Pending().RequestId == id, "consumption refuses to clear a result before matching access and owner are saved");
                using (var held = File.Open(Path.Combine(HomeVpnPrivateFiles.Root, "owner.dat"), FileMode.Open, FileAccess.Read, FileShare.None)) {
                    bool failed = false; try { service.UseToken(token, Owner); } catch (IOException) { failed = true; }
                    check(failed && Pending().RequestId == id && Pending().Result == token && service.Owner == null,
                        "failure of the second local access save retains the recoverable ID and result");
                }
                File.Delete(Path.Combine(HomeVpnPrivateFiles.Root, "owner.dat.new"));
                service.UseToken(token, Owner);
                using (var held = File.Open(PendingPath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    bool retained = false; try { HomeVpnSetupRecovery.ConfirmConsumed(Owner, token); } catch (HomeVpnSetupPendingException) { retained = true; }
                    check(retained && Pending().RequestId == id && HomeVpnPrivateFiles.Load("access") == token,
                        "locked journal refuses a completed consumption claim and retains the saved original access");
                }
                HomeVpnSetupRecovery.ConfirmConsumed(Owner, token);
                check(!HomeVpnSetupRecovery.HasPending() && HomeVpnPrivateFiles.Load("access") == token
                    && Json.Deserialize<HomeVpnOwner>(HomeVpnPrivateFiles.Load("owner")).Host == Owner.Host, "request is consumed only after both protected access and owner saves succeed");
            }
        }
        private static void StatusGuards(string token)
        {
            var request = HomeVpnSetupRecovery.Register(Owner, "My iPhone");
            var states = new List<Dictionary<string, object>> { Status(request, "running", false), Status(request, "not-found", false),
                Status(request, "unconfirmed", false), Status(request, "succeeded", false) };
            var busy = Status(request, "running", false); busy["Action"] = busy["Started"] = null; states.Add(busy);
            foreach (var change in new[] { new KeyValuePair<string, object>("RequestId", new string('e', 32)), new KeyValuePair<string, object>("Version", 2),
                new KeyValuePair<string, object>("State", "failed"), new KeyValuePair<string, object>("Action", "invite"),
                new KeyValuePair<string, object>("Started", null), new KeyValuePair<string, object>("Finished", "invalid"),
                new KeyValuePair<string, object>("ResultAvailable", "true"), new KeyValuePair<string, object>("Extra", token) }) {
                var status = Status(request); status[change.Key] = change.Value; states.Add(status);
            }
            foreach (var status in states) {
                int commands = 0;
                var error = Failure(Admin("setup", (exe, args, output) => { commands++; check(args.Contains("operation-status"), "existing setup request switches even legacy setup action to a status query"); return Write(output, Json.Serialize(status)); }));
                check(error is HomeVpnSetupPendingException && !error.Message.Contains(token) && commands == 1
                    && Pending().RequestId == request.RequestId && Pending().Result == null, "unknown, running, unavailable or malformed status preserves the request without issuing or reading access");
            }
            foreach (string result in new[] { "PROGO1.invalid", ChangeTokenHost(token, "other.example.org") }) {
                int commands = 0;
                var error = Failure(Admin("recover-setup", (exe, args, output) => { commands++; return Write(output, args.Contains("operation-status") ? Json.Serialize(Status(request)) : result); }));
                check(error is HomeVpnSetupPendingException && commands == 2 && Pending().Result == null && Pending().RequestId == request.RequestId
                    && !error.Message.Contains(result), "invalid or wrong-VPS result cannot be cached, consumed or replaced by fresh issuance");
            }
            int lost = 0;
            check(Failure(Admin("recover-setup", (exe, args, output) => { lost++; if (lost == 1) return Write(output, Json.Serialize(Status(request))); throw new IOException("fixture lost result"); })) is HomeVpnSetupPendingException
                && lost == 2 && Pending().RequestId == request.RequestId, "loss of the result query retains the original request for another read-only check");
            foreach (byte[] data in new[] { new byte[] { 255 }, new byte[32769] }) {
                int commands = 0;
                var error = Failure(Admin("recover-setup", (exe, args, output) => { commands++; File.WriteAllBytes(output, data); return Task.FromResult(0); }));
                check(error is HomeVpnSetupPendingException && commands == 1 && Pending().RequestId == request.RequestId, "invalid UTF-8 or oversized SSH output refuses recovery without a new command");
            }
            using (var held = File.Open(PendingPath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                int commands = 0;
                var error = Failure(Admin("recover-setup", (exe, args, output) => { commands++; return Write(output, args.Contains("operation-status") ? Json.Serialize(Status(request)) : token); }));
                check(error is HomeVpnSetupPendingException && commands == 2 && Pending().RequestId == request.RequestId && Pending().Result == null,
                    "failed atomic result storage retains the original ID without publishing or issuing access");
            }
            HomeVpnSetupRecovery.RetainResult(Owner, request, token);
            int revoked = 0;
            check(Failure(Admin("recover-setup", (exe, args, output) => { revoked++; return Write(output, Json.Serialize(Status(request, "succeeded", false))); })) is HomeVpnSetupPendingException
                && revoked == 1 && Pending().Result == token, "a cached token cannot bypass the live revoked/unavailable result check");
            foreach (var changed in new[] { new HomeVpnOwner { Host = "other.example.org", Port = 22, Login = "root" },
                new HomeVpnOwner { Host = Owner.Host, Port = 2222, Login = "root" }, new HomeVpnOwner { Host = Owner.Host, Port = 22, Login = "admin" } }) {
                int calls = 0;
                var error = Failure(Admin("setup", (exe, args, output) => { calls++; return Task.FromResult(0); }, (exe, args) => { calls++; return Task.FromResult(0); }, changed));
                check(error is HomeVpnSetupPendingException && calls == 0 && Pending().RequestId == request.RequestId, "changing the endpoint or account cannot abandon an unresolved setup ID");
            }
            check(Failure(Admin("setup", (exe, args, output) => { throw new Exception("must not dispatch"); }, null, Owner, "Other name")) is HomeVpnSetupPendingException
                && Pending().RequestId == request.RequestId, "changing the setup label cannot rebind or replace the request");
            var changedKey = Owner; changedKey.KeyFile = PendingPath;
            check(HomeVpnSetupRecovery.Load(changedKey, "My iPhone").RequestId == request.RequestId, "changing only the local SSH-key selection keeps the same frozen request");
            File.Delete(PendingPath);
        }
        private static string ChangeTokenHost(string token, string host)
        {
            string encoded = token.Substring(7).Replace('-', '+').Replace('_', '/');
            var fields = Json.Deserialize<Dictionary<string, object>>(Encoding.UTF8.GetString(Convert.FromBase64String(encoded.PadRight((encoded.Length + 3) / 4 * 4, '='))));
            fields["Host"] = host; return "PROGO1." + Convert.ToBase64String(Encoding.UTF8.GetBytes(Json.Serialize(fields))).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }
        private static void DispatchGuards()
        {
            int commands = 0, copies = 0;
            var error = Failure(Admin("recover-setup", (exe, args, output) => { commands++; return Task.FromResult(0); }, (exe, args) => { copies++; return Task.FromResult(0); }));
            check(error is HomeVpnSetupPendingException && copies == 0 && commands == 0 && !HomeVpnSetupRecovery.HasPending(), "recovery-only action with no journal cannot silently run fresh setup");
            error = Failure(Admin("setup", (exe, args, output) => { commands++; return Task.FromResult(0); }, (exe, args) => { throw new HomeVpnPreparationCancelledException(); }));
            check(error is HomeVpnPreparationCancelledException && commands == 0 && !HomeVpnSetupRecovery.HasPending(), "fresh preparation cancellation creates no setup ID or remote command");
            error = Failure(Admin("setup", (exe, args, output) => { commands++; return Task.FromResult(0); }, (exe, args) => { Directory.CreateDirectory(PendingPath); return Task.FromResult(0); }));
            try { check(error is HomeVpnSetupPendingException && commands == 0, "failed local request storage after copying prevents SSH dispatch"); }
            finally { Directory.Delete(PendingPath); }
            var completion = new TaskCompletionSource<object>();
            var first = Admin("setup", (exe, args, output) => { commands++; return completion.Task; });
            var request = Pending(); int before = commands;
            error = Failure(Admin("setup", (exe, args, output) => { commands++; return Task.FromResult(0); }));
            check(error is HomeVpnSetupPendingException && commands == before && !first.IsCompleted, "concurrent setup is refused while the first transport owns its request");
            completion.SetException(new IOException("fixture response lost")); Failure(first);
            error = Failure(Admin("recover-setup", (exe, args, output) => { commands++; return Task.FromResult(0); }, (exe, args) => { throw new HomeVpnPreparationCancelledException(); }));
            check(error is HomeVpnPreparationCancelledException && error.Message.Contains("могла завершиться") && !error.Message.Contains("не запускались")
                && commands == before && Pending().RequestId == request.RequestId, "cancelling recovery preparation preserves the prior command without claiming rollback");
            File.Delete(PendingPath);
        }
        private static void CorruptJournal()
        {
            var request = HomeVpnSetupRecovery.Register(Owner, "My iPhone"); var valid = Json.Serialize(request);
            foreach (var change in new[] { new KeyValuePair<string, object>("RequestId", 12), new KeyValuePair<string, object>("Port", "22"),
                new KeyValuePair<string, object>("Name", "invalid\tname"), new KeyValuePair<string, object>("Host", "VPN.example.org"),
                new KeyValuePair<string, object>("Version", 2), new KeyValuePair<string, object>("Extra", "invalid") }) {
                var fields = Json.Deserialize<Dictionary<string, object>>(valid); fields[change.Key] = change.Value;
                File.WriteAllBytes(PendingPath, ProtectedData.Protect(Encoding.UTF8.GetBytes(Json.Serialize(fields)), null, DataProtectionScope.CurrentUser));
                CheckCorrupt();
            }
            File.WriteAllBytes(PendingPath, new byte[] { 1, 2, 3 }); CheckCorrupt();
            File.WriteAllBytes(PendingPath, new byte[100001]); CheckCorrupt(); File.Delete(PendingPath);
        }
        private static void CheckCorrupt()
        {
            int calls = 0; byte[] before = File.ReadAllBytes(PendingPath);
            var error = Failure(Admin("setup", (exe, args, output) => { calls++; return Task.FromResult(0); }, (exe, args) => { calls++; return Task.FromResult(0); }));
            check(error is HomeVpnSetupPendingException && calls == 0 && before.SequenceEqual(File.ReadAllBytes(PendingPath)), "invalid protected journal is preserved and refuses all fallback setup or copying");
        }
        private static object Field(object target, string name) { return target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target); }
        private static IEnumerable<Control> Controls(Control root) { foreach (Control c in root.Controls) { yield return c; foreach (var child in Controls(c)) yield return child; } }
        private static void Shot(Form form, string work, string name) { using (var image = new Bitmap(form.Width, form.Height)) { form.DrawToBitmap(image, new Rectangle(Point.Empty, image.Size)); image.Save(Path.Combine(work, name + ".png")); } }
        private static void NativeRecovery(string token, ClipboardService clipboard, string work)
        {
            // A top-level DoEvents pump uninstalls its UI context when it ends.
            // Keep a real outer message loop, as the desktop does, while these
            // fixtures pump nested messages and await channel startup.
            var previous = SynchronizationContext.Current;
            bool previousThreadCheck = Control.CheckForIllegalCrossThreadCalls;
            bool previousAutoInstall = WindowsFormsSynchronizationContext.AutoInstall;
            Exception failure = null;
            using (var loop = new ApplicationContext()) {
                EventHandler start = null;
                start = delegate {
                    Application.Idle -= start;
                    try {
                        check(Application.MessageLoop, "native recovery owns a running Windows Forms message loop");
                        // Earlier modeless tests can leave the thread's automatic
                        // installation bookkeeping stale. Set a fresh context
                        // inside the running loop, before any async UI operation.
                        using (var ui = new WindowsFormsSynchronizationContext()) {
                            var before = SynchronizationContext.Current;
                            SynchronizationContext.SetSynchronizationContext(ui);
                            try { NativeRecoveryWindows(token, clipboard, work); }
                            finally { SynchronizationContext.SetSynchronizationContext(before); }
                        }
                    }
                    catch (Exception ex) { failure = ex; Console.Error.WriteLine("Native recovery fixture: " + ex.GetType().Name + "\n" + ex.StackTrace); }
                    finally { loop.ExitThread(); }
                };
                Control.CheckForIllegalCrossThreadCalls = true;
                WindowsFormsSynchronizationContext.AutoInstall = true;
                Application.Idle += start;
                try { Application.Run(loop); }
                finally {
                    Application.Idle -= start;
                    Control.CheckForIllegalCrossThreadCalls = previousThreadCheck;
                    WindowsFormsSynchronizationContext.AutoInstall = previousAutoInstall;
                    SynchronizationContext.SetSynchronizationContext(previous);
                }
            }
            if (failure != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        }
        private static void NativeRecoveryWindows(string token, ClipboardService clipboard, string work)
        {
            var request = HomeVpnSetupRecovery.Register(Owner, "My iPhone");
            using (var relay = new Ikev2RelayService())
            using (var service = new HomeVpnService(relay, Path.Combine(work, "absent-ssh-fixture.exe"), 15000, 14500, 17878))
            using (var form = new HomeVpnWizardForm(service, clipboard))
            using (var heartbeat = new System.Windows.Forms.Timer { Interval = 20 }) {
                form.Show(); HomeVpnFixtureFiles.ConfigureReady(form); int ticks = 0, commands = 0; var response = new TaskCompletionSource<object>();
                check(SynchronizationContext.Current is WindowsFormsSynchronizationContext, "native recovery has the Windows Forms UI continuation context");
                heartbeat.Tick += delegate { ticks++; }; heartbeat.Start();
                var next = (Button)Field(form, "next"); var state = (Label)Field(form, "setupState"); var status = (Label)Field(form, "status");
                check((int)Field(form, "step") == 1 && ((TextBox)Field(form, "host")).Text == Owner.Host && ((TextBox)Field(form, "login")).Text == Owner.Login
                    && ((NumericUpDown)Field(form, "port")).Value == 22, "reopening the wizard restores the pending endpoint even when older access exists");
                check(next.Text == "Проверить прошлую настройку" && next.AccessibilityObject.Name == next.Text && state.AccessibilityObject.Description == state.Text
                    && Controls(form).OfType<Button>().Count(b => b.Text == next.Text) == 1 && !Controls(form).OfType<Button>().Any(b => b.Text == "Настроить VPS и продолжить"),
                    "recovery has one explicit accessible next action rather than duplicate issuance controls");
                Shot(form, work, "vps-recovery-wizard"); form.ClientSize = new Size(684, 581); Application.DoEvents();
                Shot(form, work, "vps-recovery-wizard-minimum");
                var body = (FlowLayoutPanel)Field(form, "body");
                var actionBounds = form.RectangleToClient(next.RectangleToScreen(next.ClientRectangle));
                check(actionBounds.Bottom <= form.ClientSize.Height, "minimum recovery wizard retains its action: action=" + actionBounds + "; client=" + form.ClientSize);
                check(!body.HorizontalScroll.Visible, "minimum recovery wizard has no horizontal scrolling: viewport=" + body.ClientSize + "; content=" + body.DisplayRectangle.Size
                    + (body.HorizontalScroll.Visible ? "; children=" + String.Join("; ", body.Controls.Cast<Control>().Select(c => c.GetType().Name + ":" + c.Bounds + "/preferred=" + c.PreferredSize + "/max=" + c.MaximumSize)) : ""));
                form.ClientSize = new Size(750, 650); Application.DoEvents();
                check(!body.HorizontalScroll.Visible, "recovery wizard can grow again without horizontal scrolling");
                form.ClientSize = new Size(684, 581); Application.DoEvents();
                check(!body.HorizontalScroll.Visible, "recovery wizard can shrink again without retaining a stale scroll extent");
                typeof(HomeVpnWizardForm).GetField("preparedToken", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(form, token);
                typeof(HomeVpnWizardForm).GetField("preparedOwner", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(form, Json.Serialize(Owner));
                form.Admin = (owner, action, label, id, progress) => {
                    check(action == "recover-setup", "wizard retains a recovery-only action after preview");
                    return HomeVpnService.AdminAsync(owner, action, label, id, progress, (exe, args) => Task.FromResult(0),
                        (exe, args, output) => { commands++; check(args.Contains("operation-status"), "cached token cannot bypass the wizard status query"); File.WriteAllText(output, Json.Serialize(Status(request, "running", false))); return response.Task; });
                };
                next.PerformClick(); Pump(() => ticks >= 3);
                check((bool)Field(form, "busy") && !next.Enabled && !((Control)Field(form, "body")).Enabled && !((Button)Field(form, "cancelWait")).Visible,
                    "pending SSH recovery keeps UI responsive and protects the unchanged remote-wait boundary");
                response.SetResult(null); Pump(() => !(bool)Field(form, "busy"));
                check(commands == 1 && (int)Field(form, "step") == 1 && next.Enabled && next.Text == "Проверить прошлую настройку"
                    && status.Text.Contains("ещё выполняется") && Pending().RequestId == request.RequestId, "unfinished recovery stays on the same step with the same ID and no cached-token fallback");
                form.Admin = (owner, action, label, id, progress) => HomeVpnService.AdminAsync(owner, action, label, id, progress,
                    (exe, args) => Task.FromResult(0), (exe, args, output) => { commands++; return Write(output, args.Contains("operation-status") ? Json.Serialize(Status(request)) : token); });
                next.PerformClick(); Pump(() => !(bool)Field(form, "busy"));
                check(commands == 3 && !HomeVpnSetupRecovery.HasPending() && HomeVpnPrivateFiles.Load("access") == token && service.Owner.Host == Owner.Host,
                    "wizard consumes the original result after saving it even if subsequent isolated channel startup fails");
                check(SynchronizationContext.Current is WindowsFormsSynchronizationContext && !form.InvokeRequired,
                    "channel outcome is observed on the native UI thread with cross-thread checks enabled");
                check((int)Field(form, "step") == 1 && next.Text == "Запустить канал и продолжить" && state.Text.Contains("Доступ к VPS уже сохранён"),
                    "channel failure offers channel startup with saved access rather than misleading repeat setup");
                next.PerformClick(); Pump(() => !(bool)Field(form, "busy"));
                check(commands == 3 && !HomeVpnSetupRecovery.HasPending(), "channel retry never reruns the completed VPS setup"); form.Close();
            }
            HomeVpnSetupRecovery.Register(Owner, "My iPhone");
            var ready = new TaskCompletionSource<object>();
            using (var form = new HomeVpnPreparationForm(ct => ready.Task, true)) {
                form.Show(); Application.DoEvents(); var status = (Label)Field(form, "status"); var cancel = (Button)Field(form, "cancel");
                check(status.Text.Contains("могла завершиться") && !status.Text.Contains("не запускались") && status.AccessibilityObject.Description == status.Text,
                    "native recovery preparation explains only copy cancellation, without denying earlier server effects");
                Shot(form, work, "vps-recovery-preparation"); form.ClientSize = new Size(440, 270); Application.DoEvents();
                check(form.RectangleToClient(cancel.RectangleToScreen(cancel.ClientRectangle)).Bottom <= form.ClientSize.Height
                    && !((FlowLayoutPanel)Field(form, "viewport")).HorizontalScroll.Visible, "recovery preparation keeps cancellation visible at minimum size"); Shot(form, work, "vps-recovery-preparation-minimum");
                cancel.PerformClick(); ready.SetCanceled(); Pump(() => form.Completion.IsCompleted);
                check(form.Completion.IsFaulted && form.Completion.Exception.GetBaseException().Message.Contains("могла завершиться") && HomeVpnSetupRecovery.HasPending(),
                    "native recovery copy cancellation preserves the pending request");
            }
            File.WriteAllBytes(PendingPath, new byte[] { 1, 2, 3 });
            using (var relay = new Ikev2RelayService())
            using (var service = new HomeVpnService(relay))
            using (var form = new HomeVpnWizardForm(service, clipboard)) {
                form.Show(); HomeVpnFixtureFiles.ConfigureReady(form);
                check((int)Field(form, "step") == 1 && !((Button)Field(form, "next")).Enabled && ((Label)Field(form, "setupState")).Text.Contains("повреждён")
                    && service.Access != null, "corrupt pending journal opens a blocked recovery step while preserving saved access"); form.Close();
            }
            File.Delete(PendingPath);
        }
    }
}

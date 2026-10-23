using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace ProGo
{
    // Uses the production dispatcher and owned Windows process runner. Only SCP
    // and the SSH executable are replaced; no server or owner credentials are used.
    internal static class HomeVpnAdminRecoveryTests
    {
        private const string FixtureVariable = "PROGO_ADMIN_WAIT_FIXTURE";
        private static readonly JavaScriptSerializer Json = new JavaScriptSerializer();
        private static Action<bool, string> check;
        private static string folder, response;
        private static int copies;
        private static HomeVpnOwner Owner { get { return new HomeVpnOwner { Host = "vpn.example.org", Port = 22, Login = "root", KeyFile = "" }; } }
        private static string PendingPath { get { return Path.Combine(HomeVpnPrivateFiles.Root, HomeVpnAdminRecovery.StorageName + ".dat"); } }
        private static string At(string name) { return Path.Combine(folder, name); }

        internal static bool Fixture(string[] args)
        {
            if (args.Length == 1 && args[0] == "admin-recovery-unrelated") { Thread.Sleep(300000); return true; }
            bool child = args.Length == 2 && args[0] == "admin-recovery-child";
            string path = child ? args[1] : Environment.GetEnvironmentVariable(FixtureVariable);
            if (!child && (args.Length == 0 || args[0] != "-o" || String.IsNullOrEmpty(path))) return false;
            if (child) { HomeVpnFixtureFiles.Publish(Path.Combine(path, "child"), Process.GetCurrentProcess().Id.ToString()); Thread.Sleep(60000); return true; }
            string command = args.Last();
            string kind = command.Contains("home_vpn_setup.py invite ") ? "invite"
                : command.Contains("home_vpn_setup.py operation-status ") ? "status"
                : command.Contains("home_vpn_setup.py operation-result ") ? "result"
                : command.Contains("home_vpn_setup.py list ") ? "list" : null;
            if (kind == null || !args.Contains("-T") || !args.Contains("root@vpn.example.org")) throw new Exception("Unexpected production SSH dispatch in admin fixture");
            File.AppendAllText(Path.Combine(path, "calls"), kind + "\n");
            File.AppendAllText(Path.Combine(path, "argv"), Json.Serialize(args) + "\n");
            HomeVpnFixtureFiles.Publish(Path.Combine(path, "pid"), Process.GetCurrentProcess().Id.ToString());
            var request = HomeVpnAdminRecovery.Pending();
            if (kind != "list") {
                var ids = Regex.Matches(command, @"--request-id ([0-9a-f]{32})").Cast<Match>().Select(match => match.Groups[1].Value).ToArray();
                if (request == null || ids.Length != (kind == "invite" ? 3 : 1) || ids.Any(id => id != request.RequestId)
                    || !command.Contains("trap 'rm -rf -- /tmp/")) throw new Exception("SSH fixture lost the protected stable request ID or remote cleanup");
                if (kind == "invite") {
                    if (ids.Length != 3 || !command.Contains("operation-status") || !command.Contains("operation-result")
                        || !command.Contains("trap 'rm -rf -- /tmp/")
                        || command.IndexOf("home_vpn_setup.py invite ", StringComparison.Ordinal) >= command.IndexOf("home_vpn_setup.py operation-status ", StringComparison.Ordinal)
                        || command.IndexOf("home_vpn_setup.py operation-status ", StringComparison.Ordinal) >= command.IndexOf("home_vpn_setup.py operation-result ", StringComparison.Ordinal)
                        || !command.Contains("--name '" + request.Name + "'")
                        || (request.SourceInviteId == null ? command.Contains(" --id ") : !command.Contains(" --id " + request.SourceInviteId)))
                        throw new Exception("Invite fixture expected the frozen production compound command");
                    string record = Path.Combine(path, "receipt");
                    if (File.Exists(record)) throw new Exception("Fixture observed a duplicate invitation submission");
                    File.WriteAllText(record, Json.Serialize(request));
                } else if (command.Contains("home_vpn_setup.py invite ")) throw new Exception("Recovery tried to submit an invitation");
            }
            string mode = File.ReadAllText(Path.Combine(path, "mode"));
            if (kind == "invite" && mode == "lost") { Environment.Exit(7); return true; }
            if (kind == "invite" && mode == "hold") {
                using (var descendant = Process.Start(new ProcessStartInfo(Application.ExecutablePath,
                    "admin-recovery-child " + HomeVpnService.Argument(path)) { UseShellExecute = false, CreateNoWindow = true })) {
                    var watch = Stopwatch.StartNew();
                    while (!File.Exists(Path.Combine(path, "child"))) { Thread.Sleep(10); if (watch.ElapsedMilliseconds > 5000) throw new Exception("Admin fixture child did not start"); }
                    Thread.Sleep(60000);
                }
                return true;
            }
            Console.OutputEncoding = new UTF8Encoding(false);
            string token = File.ReadAllText(Path.Combine(path, "response-token"));
            if (command.Contains(token) || command.Contains(HomeVpnAccess.Parse(token).Password) || command.Contains(HomeVpnAccess.Parse(token).PrivateKey))
                throw new Exception("Production SSH command exposed private token material");
            if (kind == "list") Console.WriteLine("[]");
            else if (kind == "status") Console.WriteLine(Json.Serialize(Status(request, mode == "not-found" ? "not-found" : "succeeded", mode != "not-found" && mode != "unavailable")));
            else if (kind == "result") Console.WriteLine(token);
            else { Console.WriteLine(Json.Serialize(Status(request, "succeeded", true))); Console.WriteLine(token); }
            return true;
        }

        internal static void Run(Action<bool, string> assert, string token, ClipboardService clipboard, string work)
        {
            check = assert; folder = Path.Combine(work, "admin-recovery"); HomeVpnPrivateFiles.SecureDirectory(folder);
            response = ChangeToken(token, "InviteId", new string('c', 24));
            var previous = new Dictionary<string, byte[]>();
            foreach (string name in new[] { "access", "owner", "home-address", HomeVpnSetupRecovery.StorageName, HomeVpnAdminRecovery.StorageName })
                Backup(previous, Path.Combine(HomeVpnPrivateFiles.Root, name + ".dat"));
            string helpers = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scripts", "home-vpn", "server"); Directory.CreateDirectory(helpers);
            foreach (string file in new[] { "home_vpn_setup.py", "ikev2_relay.py", "install-ikev2-relay.sh", "profile_share_setup.py", "profile_share.py", "qrcodegen.py", "QR_LICENSE.txt" }) {
                string target = Path.Combine(helpers, file); Backup(previous, target);
                File.Copy(Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Application.ExecutablePath)), "server", file), target, true);
            }
            string oldFixture = Environment.GetEnvironmentVariable(FixtureVariable);
            Exception threadFailure = null; ThreadExceptionEventHandler onThreadFailure = (sender, args) => { threadFailure = args.Exception; };
            Application.ThreadException += onThreadFailure;
            var unrelated = Process.Start(new ProcessStartInfo(Application.ExecutablePath, "admin-recovery-unrelated") { UseShellExecute = false, CreateNoWindow = true });
            try {
                File.Delete(Path.Combine(HomeVpnPrivateFiles.Root, HomeVpnSetupRecovery.StorageName + ".dat"));
                HomeVpnPrivateFiles.Save("access", token); HomeVpnPrivateFiles.Save("owner", Json.Serialize(Owner));
                Environment.SetEnvironmentVariable(FixtureVariable, folder);
                PendingStateWorker();
                ConsumptionWorker();
                InitialPendingUi(clipboard);
                RepeatedPendingUi(clipboard);
                CreateUncertaintyUi(clipboard);
                ReissueUncertaintyUi(clipboard);
                RecoverNameUi(clipboard);
                RecoverConsumptionUi(clipboard);
                foreach (string route in new[] { "button", "escape", "close", "dispose", "deadline" }) NativeWait(route, previous);
                LostResponseAndReopen(token, clipboard, previous);
                ReissueBoundary(token); FrozenOwner(); DefaultHandoff(clipboard); LateDisposedUi(clipboard); ReissueHandoffMessage(token, clipboard); StatusAndStorage(token);
                check(threadFailure == null, "native invite waiting, recovery and modal handoff cause no UI thread exception");
                check(!unrelated.HasExited, "invite cancellation, deadline and recovery preserve an unrelated native process");
                check(!Directory.GetDirectories(HomeVpnPrivateFiles.Root, "admin-*").Any()
                    && !Directory.GetFiles(HomeVpnPrivateFiles.Root, "admin-request.dat.*.new").Any(), "invite recovery settles private work and atomic-save temporary files");
            } finally {
                Application.ThreadException -= onThreadFailure;
                Environment.SetEnvironmentVariable(FixtureVariable, oldFixture); KillFixtures();
                if (!unrelated.HasExited) { unrelated.Kill(); unrelated.WaitForExit(2000); } unrelated.Dispose();
                foreach (var item in previous) { if (item.Value == null) File.Delete(item.Key); else File.WriteAllBytes(item.Key, item.Value); }
            }
        }
        private static void Backup(Dictionary<string, byte[]> saved, string path) { saved[path] = File.Exists(path) ? File.ReadAllBytes(path) : null; }
        private static object Field(object target, string name) { return target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target); }
        private static void Shot(Form form, string name)
        {
            using (var bitmap = new Bitmap(form.Width, form.Height)) {
                form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size)); bitmap.Save(Path.Combine(Path.GetDirectoryName(folder), name + ".png"));
            }
        }
        private static void PendingStateWorker()
        {
            byte[] original = File.Exists(PendingPath) ? File.ReadAllBytes(PendingPath) : null;
            try {
                foreach (string mode in new[] { "empty", "pending", "completed", "corrupt", "locked", "cancel" }) {
                    File.Delete(PendingPath);
                    if (mode != "empty" && mode != "corrupt") {
                        var request = HomeVpnAdminRecovery.Register(Owner, "My iPhone", null);
                        if (mode == "completed") HomeVpnAdminRecovery.RetainResult(Owner, request, response);
                    }
                    if (mode == "corrupt") File.WriteAllBytes(PendingPath, new byte[] { 1, 2, 3 });
                    var before = Directory.GetFiles(HomeVpnPrivateFiles.Root).ToDictionary(path => path, File.ReadAllBytes);
                    using (var form = new Form())
                    using (var entered = new ManualResetEventSlim())
                    using (var release = new ManualResetEventSlim())
                    using (var cancellation = new CancellationTokenSource())
                    using (var lease = HomeVpnAdminRecovery.AcquireAsync().GetAwaiter().GetResult()) {
                        FileStream held = null; Task<HomeVpnAdminPendingState> operation = null; int ui = Thread.CurrentThread.ManagedThreadId, worker = 0, phase = 0, turns = 0;
                        var phaseWatch = new Stopwatch();
                        try {
                            if (mode == "locked") held = File.Open(PendingPath, FileMode.Open, FileAccess.Read, FileShare.None);
                            HomeVpnWaitingTests.ModalWizardCase(form, delegate {
                                if (phase == 0) {
                                    check(form.Modal, "invitation pending-state read uses actual native modal loop: " + mode);
                                    var watch = Stopwatch.StartNew(); phaseWatch.Restart();
                                    operation = HomeVpnAdminRecovery.ReadPendingStateAsync(cancellation.Token, delegate {
                                        Interlocked.Exchange(ref worker, Thread.CurrentThread.ManagedThreadId); entered.Set();
                                        if (!release.Wait(5000)) throw new TimeoutException("Invitation pending-state fixture deadline");
                                    });
                                    check(watch.ElapsedMilliseconds < 500, "pending-state API returns owned work before actual private status/DPAPI read: " + mode);
                                    phase = 1; return false;
                                }
                                if (phase == 1) {
                                    if (!entered.IsSet || ++turns < 3) {
                                        if (phaseWatch.ElapsedMilliseconds >= 3000) throw new TimeoutException("Invitation pending-state heartbeat deadline: " + mode + "; task=" + operation.Status);
                                        return false;
                                    }
                                    check(worker != ui && !operation.IsCompleted && form.Visible && form.Modal,
                                        "held read-only pending snapshot keeps modal heartbeat and does not take the caller's family lease: " + mode);
                                    if (mode == "cancel") cancellation.Cancel(); release.Set(); phaseWatch.Restart(); phase = 2; return false;
                                }
                                if (!operation.IsCompleted) {
                                    if (phaseWatch.ElapsedMilliseconds >= 5000) throw new TimeoutException("Invitation pending-state settlement deadline: " + mode + "; task=" + operation.Status);
                                    return false;
                                }
                                HomeVpnAdminPendingState state = null; Exception failure = null; try { state = operation.GetAwaiter().GetResult(); } catch (Exception ex) { failure = ex; }
                                if (held != null) { held.Dispose(); held = null; }
                                check(mode == "cancel" ? failure is OperationCanceledException && state == null
                                    : failure == null && state != null && state.Pending == (mode != "empty") && state.Unreadable == (mode == "corrupt" || mode == "locked"),
                                    "pending snapshot distinguishes missing/pending/completed/unreadable/cancelled without inventing a safe empty state: " + mode);
                                check(before.Count == Directory.GetFiles(HomeVpnPrivateFiles.Root).Length && before.All(item => item.Value.SequenceEqual(File.ReadAllBytes(item.Key))),
                                    "status snapshot preserves actual encrypted request/result/access bytes and creates or consumes no private file: " + mode);
                                return true;
                            }, () => false);
                        } finally {
                            cancellation.Cancel(); release.Set(); if (held != null) held.Dispose(); form.Dispose();
                            if (operation != null) { Pump(() => operation.IsCompleted, 5000); try { operation.GetAwaiter().GetResult(); } catch (OperationCanceledException) { } }
                        }
                    }
                }
            } finally { if (original == null) File.Delete(PendingPath); else File.WriteAllBytes(PendingPath, original); }
        }
        private static void InitialPendingUi(ClipboardService clipboard)
        {
            byte[] original = File.Exists(PendingPath) ? File.ReadAllBytes(PendingPath) : null;
            try {
                foreach (string mode in new[] { "empty", "pending", "corrupt", "locked", "close-retry", "dispose", "stop", "service-dispose" }) {
                    File.Delete(PendingPath);
                    if (mode != "empty" && mode != "corrupt") HomeVpnAdminRecovery.Register(Owner, "Fixture friend", null);
                    if (mode == "corrupt") File.WriteAllBytes(PendingPath, new byte[] { 1, 2, 3 });
                    var before = Directory.GetFiles(HomeVpnPrivateFiles.Root).ToDictionary(path => path, File.ReadAllBytes);
                    using (var relay = new Ikev2RelayService())
                    using (var service = new HomeVpnService(relay, Application.ExecutablePath, 0, 0, 0))
                    using (var entered = new ManualResetEventSlim())
                    using (var release = new ManualResetEventSlim()) {
                        FileStream held = null; HomeInvitationsForm form = null; Task<bool> stop = null;
                        int reads = 0, calls = 0, phase = 0, turns = 0, worker = 0, ui = Thread.CurrentThread.ManagedThreadId;
                        bool closing = mode == "close-retry", disposing = mode == "dispose";
                        try {
                            if (mode == "locked") held = File.Open(PendingPath, FileMode.Open, FileAccess.Read, FileShare.None);
                            form = new HomeInvitationsForm(new HomeVpnInvitation[0], (action, label, id) => {
                                ++calls; throw new Exception("Initial pending read must not invoke Admin");
                            }, clipboard, service, delegate {
                                Interlocked.Increment(ref reads); worker = Thread.CurrentThread.ManagedThreadId; entered.Set();
                                if (!release.Wait(5000)) throw new TimeoutException("Initial invitation read fixture deadline");
                            });
                            HomeVpnWaitingTests.ModalWizardCase(form, delegate {
                                if (phase == 0) {
                                    if (!entered.IsSet || ++turns < 3) return false;
                                    check(form.Modal && form.Visible && worker != ui && reads == 1 && calls == 0
                                        && !form.InitializationWork.IsCompleted && !form.PendingReadWork.IsCompleted && service.IsAdminWorking,
                                        "initial invitation read has one actual worker and a responsive modal owner: " + mode);
                                    foreach (string button in new[] { "create", "revoke", "reissue", "refresh", "recover" })
                                        check(!((Button)Field(form, button)).Enabled, "pending read blocks premature action: " + mode + "/" + button);
                                    check(((Button)Field(form, "close")).Enabled, "initial read permits explicit close: " + mode);
                                    var watch = Stopwatch.StartNew();
                                    if (closing) form.Close();
                                    if (disposing) form.Dispose();
                                    if (mode == "service-dispose") service.Dispose();
                                    if (mode == "stop" || mode == "service-dispose") stop = service.StopAsync();
                                    check(watch.ElapsedMilliseconds < 500 && !form.PendingReadWork.IsCompleted
                                        && (!closing || !form.IsDisposed) && (stop == null || !stop.IsCompleted),
                                        "close Dispose and Stop retain actual blocked read without blocking UI: " + mode);
                                    turns = 0; phase = 1; return false;
                                }
                                if (phase == 1) {
                                    if (++turns < 3) return false;
                                    check(!form.PendingReadWork.IsCompleted && (stop == null || !stop.IsCompleted), "cancel cannot abandon held invitation read: " + mode);
                                    release.Set(); phase = 2; return false;
                                }
                                if (!form.InitializationWork.IsCompleted || (stop != null && !stop.IsCompleted)) return false;
                                form.InitializationWork.GetAwaiter().GetResult();
                                if (stop != null) check(stop.GetAwaiter().GetResult(), "Stop settles actual pending read: " + mode);
                                bool cancelled = closing || disposing || stop != null;
                                if (!disposing && !closing) {
                                    check((bool)Field(form, "pendingAdmin") == (cancelled || mode != "empty")
                                        && (bool)Field(form, "pendingUnreadable") == (cancelled || mode == "corrupt" || mode == "locked"),
                                        "initial pending UI publishes only admitted state; cancellation never invents empty: " + mode);
                                    check(((Button)Field(form, "create")).Enabled == (mode == "empty"), "mutations require a proven empty snapshot: " + mode);
                                }
                                check(reads == 1 && calls == 0 && !service.IsAdminWorking, "settled initial read releases ownership without duplicate action: " + mode);
                                check(true, "invitation initial pending UI PASS: " + mode); return true;
                            }, () => closing || disposing);
                        } finally {
                            release.Set(); if (held != null) held.Dispose(); if (form != null) form.Dispose();
                            if (form != null) { Pump(() => form.InitializationWork.IsCompleted && form.PendingReadWork.IsCompleted, 5000); form.InitializationWork.GetAwaiter().GetResult(); }
                            service.Dispose(); var cleanup = service.StopAsync(); Pump(() => cleanup.IsCompleted, 5000);
                            check(cleanup.GetAwaiter().GetResult() && !service.HasOwnedResources, "initial invitation fixture joins its actual work and cleanup: " + mode);
                        }
                    }
                    check(before.Count == Directory.GetFiles(HomeVpnPrivateFiles.Root).Length && before.All(item => item.Value.SequenceEqual(File.ReadAllBytes(item.Key))),
                        "initial UI read preserves protected request and private files: " + mode);
                    if (mode == "close-retry") {
                        using (var relay = new Ikev2RelayService())
                        using (var service = new HomeVpnService(relay, Application.ExecutablePath, 0, 0, 0))
                        using (var retry = new HomeInvitationsForm(new HomeVpnInvitation[0], (action, label, id) => { throw new Exception("Retry must not invoke Admin"); }, clipboard, service)) {
                            try {
                                HomeVpnWaitingTests.ModalWizardCase(retry, delegate {
                                    if (!retry.InitializationWork.IsCompleted) return false;
                                    retry.InitializationWork.GetAwaiter().GetResult();
                                    check((bool)Field(retry, "pendingAdmin") && !(bool)Field(retry, "pendingUnreadable") && ((Button)Field(retry, "recover")).Enabled,
                                        "close then reopen retries actual pending read without stale cancelled publication"); return true;
                                }, () => false);
                            } finally {
                                retry.Dispose(); Pump(() => retry.InitializationWork.IsCompleted, 5000);
                                retry.InitializationWork.GetAwaiter().GetResult(); service.Dispose(); var cleanup = service.StopAsync();
                                Pump(() => cleanup.IsCompleted, 5000); check(cleanup.GetAwaiter().GetResult(), "retry fixture cleanup joins actual owned read");
                            }
                        }
                    }
                }
            } finally { if (original == null) File.Delete(PendingPath); else File.WriteAllBytes(PendingPath, original); }
        }
        private static void RepeatedPendingUi(ClipboardService clipboard)
        {
            byte[] original = File.Exists(PendingPath) ? File.ReadAllBytes(PendingPath) : null;
            try {
                foreach (string mode in new[] { "refresh-empty", "refresh-pending", "refresh-corrupt", "refresh-locked", "post-complete", "post-failure", "post-pending", "close-retry", "dispose", "stop-retry", "service-dispose", "stale-scope" }) {
                    File.Delete(PendingPath);
                    using (var relay = new Ikev2RelayService())
                    using (var service = new HomeVpnService(relay, Application.ExecutablePath, 0, 0, 0))
                    using (var entered = new ManualResetEventSlim())
                    using (var release = new ManualResetEventSlim()) {
                        HomeInvitationsForm form = null; FileStream held = null; Task<bool> stop = null; Task first = null;
                        var delayedList = new TaskCompletionSource<string>();
                        int reads = 0, calls = 0, phase = 0, turns = 0, worker = 0, ui = Thread.CurrentThread.ManagedThreadId, changes = 0, closedChanges = 0;
                        bool closing = mode == "close-retry", disposing = mode == "dispose", post = mode.StartsWith("post-");
                        var row = new HomeVpnInvitation { Id = new string('d', 24), Name = "Fixture friend" };
                        string rows = Json.Serialize(new[] { new HomeVpnInvitation { Id = row.Id, Name = row.Name } });
                        Dictionary<string, byte[]> before = null;
                        try {
                            form = new HomeInvitationsForm(new[] { row }, (action, label, id) => {
                                ++calls; check(action == (post ? "revoke" : "list"), "repeated read preserves selected existing transport: " + mode);
                                if (post) { check(id == row.Id, "post-action read cannot redirect the selected ID: " + mode); if (mode == "post-failure") throw new InvalidOperationException(); return Task.FromResult("Access revoked."); }
                                return mode == "stale-scope" && calls == 1 ? delayedList.Task : Task.FromResult(rows);
                            }, clipboard, service, delegate {
                                int n = Interlocked.Increment(ref reads); if (n != 2) return;
                                worker = Thread.CurrentThread.ManagedThreadId; entered.Set();
                                if (!release.Wait(5000)) throw new TimeoutException("Repeated invitation read fixture deadline");
                            });
                            form.Confirm = value => true;
                            ((Label)Field(form, "status")).TextChanged += delegate { ++changes; };
                            HomeVpnWaitingTests.ModalWizardCase(form, delegate {
                                if (phase == 0) {
                                    if (!form.InitializationWork.IsCompleted) return false; form.InitializationWork.GetAwaiter().GetResult();
                                    check(reads == 1 && calls == 0, "initial read settles before the real repeated UI route: " + mode);
                                    var view = (HomeInvitationList)Field(form, "list"); ((TextBox)Field(view, "search")).Text = "Fixture";
                                    ((ListBox)Field(view, "list")).SelectedIndex = 0; ((TextBox)Field(form, "name")).Text = "Preserved draft";
                                    if (mode == "refresh-pending" || mode == "refresh-locked" || mode == "post-pending") HomeVpnAdminRecovery.Register(Owner, "Fixture friend", null);
                                    if (mode == "refresh-corrupt") File.WriteAllBytes(PendingPath, new byte[] { 1, 2, 3 });
                                    before = Directory.GetFiles(HomeVpnPrivateFiles.Root).ToDictionary(path => path, File.ReadAllBytes);
                                    if (mode == "refresh-locked") held = File.Open(PendingPath, FileMode.Open, FileAccess.Read, FileShare.None);
                                    ((Button)Field(form, post ? "revoke" : "refresh")).PerformClick(); first = form.ActionWork;
                                    if (mode == "stale-scope") { stop = service.StopAsync(); phase = 5; return false; }
                                    phase = 1; return false;
                                }
                                if (phase == 5) {
                                    if (!stop.IsCompleted) return false; check(stop.GetAwaiter().GetResult(), "Stop settles before late list response");
                                    delayedList.SetResult(rows); phase = 6; return false;
                                }
                                if (phase == 6) {
                                    if (!first.IsCompleted) return false; first.GetAwaiter().GetResult();
                                    check(reads == 1 && !entered.IsSet && (bool)Field(form, "pendingAdmin") && (bool)Field(form, "pendingUnreadable"),
                                        "old transport Stop scope refuses a fresh pending read and cannot publish late rows");
                                    phase = 4;
                                }
                                if (phase == 1) {
                                    if (!entered.IsSet || ++turns < 3) return false;
                                    check(form.Modal && form.Visible && worker != ui && reads == 2 && calls == 1 && !first.IsCompleted
                                        && !form.PendingReadWork.IsCompleted && service.IsAdminWorking,
                                        "repeated actual private read retains responsive modal UI and service owner: " + mode);
                                    foreach (string button in new[] { "create", "revoke", "reissue", "refresh", "recover" })
                                        check(!((Button)Field(form, button)).Enabled, "repeated read retains mutation and duplicate gate: " + mode + "/" + button);
                                    ((Button)Field(form, "refresh")).PerformClick(); check(calls == 1 && reads == 2, "held pending read cannot admit parallel refresh: " + mode);
                                    var watch = Stopwatch.StartNew();
                                    if (closing) form.Close(); if (disposing) { form.Dispose(); closedChanges = changes; }
                                    if (mode == "service-dispose") service.Dispose();
                                    if (mode == "stop-retry" || mode == "service-dispose") stop = service.StopAsync();
                                    check(watch.ElapsedMilliseconds < 500 && !first.IsCompleted && !form.PendingReadWork.IsCompleted
                                        && (!closing || !form.IsDisposed) && (stop == null || !stop.IsCompleted),
                                        "repeated read close Dispose Stop return promptly and retain actual work: " + mode);
                                    turns = 0; phase = 2; return false;
                                }
                                if (phase == 2) {
                                    if (++turns < 3) return false;
                                    check(!first.IsCompleted && (stop == null || !stop.IsCompleted), "cancel cannot discard physical repeated read: " + mode);
                                    release.Set(); phase = 3; return false;
                                }
                                if (phase == 3) {
                                    if (!first.IsCompleted || (stop != null && !stop.IsCompleted)) return false; first.GetAwaiter().GetResult();
                                    if (stop != null) check(stop.GetAwaiter().GetResult(), "Stop drains actual repeated read: " + mode);
                                    bool cancelled = closing || disposing || stop != null;
                                    if (!closing && !disposing) {
                                        bool pending = cancelled || mode == "refresh-pending" || mode == "refresh-corrupt" || mode == "refresh-locked" || mode == "post-pending";
                                        bool unreadable = cancelled || mode == "refresh-corrupt" || mode == "refresh-locked";
                                        check((bool)Field(form, "pendingAdmin") == pending && (bool)Field(form, "pendingUnreadable") == unreadable,
                                            "repeated read preserves unreadable/cancelled gating rather than guessing empty: " + mode);
                                        check(((Button)Field(form, "create")).Enabled == (!pending && mode != "post-failure"), "only admitted snapshot and reconciliation release mutations: " + mode);
                                    }
                                    if (disposing) check(changes == closedChanges, "disposed repeated read cannot publish a late status");
                                    check(reads == 2 && calls == 1 && !service.IsAdminWorking, "each action or Refresh performs exactly one pending read: " + mode);
                                    if (mode == "stop-retry") {
                                        ((Button)Field(form, "refresh")).PerformClick(); phase = 4; return false;
                                    }
                                    phase = 4;
                                }
                                if (!form.ActionWork.IsCompleted) return false; form.ActionWork.GetAwaiter().GetResult();
                                if (mode == "stop-retry") check(reads == 3 && calls == 2 && !((bool)Field(form, "pendingAdmin")) && ((Button)Field(form, "create")).Enabled
                                    && first.IsCompleted, "explicit same-window Retry publishes new snapshot without old cancelled overwrite");
                                if (!closing && !disposing) {
                                    var view = (HomeInvitationList)Field(form, "list");
                                    check(view.Selected != null && view.Selected.Id == row.Id && ((TextBox)Field(view, "search")).Text == "Fixture"
                                        && ((TextBox)Field(form, "name")).Text == "Preserved draft", "refresh/pending settlement preserves selected ID search and draft: " + mode);
                                }
                                if (held != null) { held.Dispose(); held = null; }
                                check(before.Count == Directory.GetFiles(HomeVpnPrivateFiles.Root).Length && before.All(item => item.Value.SequenceEqual(File.ReadAllBytes(item.Key))),
                                    "repeated read never consumes or rewrites protected request/access files: " + mode);
                                check(true, "invitation repeated pending UI PASS: " + mode); return true;
                            }, () => closing || disposing);
                        } finally {
                            release.Set(); delayedList.TrySetResult(rows); if (held != null) held.Dispose(); if (form != null) form.Dispose();
                            if (form != null) { Pump(() => form.InitializationWork.IsCompleted && form.ActionWork.IsCompleted && form.PendingReadWork.IsCompleted, 5000); form.InitializationWork.GetAwaiter().GetResult(); form.ActionWork.GetAwaiter().GetResult(); }
                            service.Dispose(); var cleanup = service.StopAsync(); Pump(() => cleanup.IsCompleted, 5000);
                            check(cleanup.GetAwaiter().GetResult() && !service.HasOwnedResources, "repeated invitation fixture joins logical physical and cleanup work: " + mode);
                        }
                        if (mode == "close-retry") {
                            using (var retryRelay = new Ikev2RelayService())
                            using (var retryService = new HomeVpnService(retryRelay, Application.ExecutablePath, 0, 0, 0))
                            using (var retry = new HomeInvitationsForm(new[] { row }, (action, label, id) => { throw new Exception("Reopen does not invoke Admin"); }, clipboard, retryService)) {
                                try {
                                    HomeVpnWaitingTests.ModalWizardCase(retry, delegate {
                                        if (!retry.InitializationWork.IsCompleted) return false; retry.InitializationWork.GetAwaiter().GetResult();
                                        check(!((bool)Field(retry, "pendingAdmin")) && !((bool)Field(retry, "pendingUnreadable")) && ((Button)Field(retry, "create")).Enabled,
                                            "close then reopen performs actual new read without cancelled old gate overwrite"); return true;
                                    }, () => false);
                                } finally {
                                    retry.Dispose(); Pump(() => retry.InitializationWork.IsCompleted, 5000); retry.InitializationWork.GetAwaiter().GetResult();
                                    retryService.Dispose(); var cleanup = retryService.StopAsync(); Pump(() => cleanup.IsCompleted, 5000);
                                    check(cleanup.GetAwaiter().GetResult(), "repeated close Retry fixture joins actual cleanup");
                                }
                            }
                        }
                    }
                }
            } finally { if (original == null) File.Delete(PendingPath); else File.WriteAllBytes(PendingPath, original); }
        }
        private static void CreateUncertaintyUi(ClipboardService clipboard)
        {
            byte[] original = File.Exists(PendingPath) ? File.ReadAllBytes(PendingPath) : null;
            try {
                foreach (string mode in new[] { "empty", "pending", "corrupt", "locked", "close-retry", "dispose", "stop-retry", "service-dispose" }) {
                    File.Delete(PendingPath);
                    using (var relay = new Ikev2RelayService())
                    using (var service = new HomeVpnService(relay, Application.ExecutablePath, 0, 0, 0))
                    using (var entered = new ManualResetEventSlim())
                    using (var release = new ManualResetEventSlim()) {
                        HomeInvitationsForm form = null; FileStream held = null; Task first = null; Task<bool> stop = null;
                        Dictionary<string, byte[]> before = null;
                        int reads = 0, mutations = 0, queries = 0, handoffs = 0, phase = 0, turns = 0, worker = 0, ui = Thread.CurrentThread.ManagedThreadId, updates = 0, closedUpdates = 0;
                        bool closing = mode == "close-retry", disposing = mode == "dispose", pending = mode != "empty" && mode != "stop-retry";
                        try {
                            form = new HomeInvitationsForm(new HomeVpnInvitation[0], (action, label, id) => {
                                if (action == "list") { ++queries; return Task.FromResult("[]"); }
                                ++mutations; check(action == "invite" && label == "Fixture friend" && id == null, "uncertain Create retains its exact original intent: " + mode);
                                throw new IOException("PRIVATE_UNCERTAINTY_CANARY");
                            }, clipboard, service, delegate {
                                if (Interlocked.Increment(ref reads) != 2) return;
                                worker = Thread.CurrentThread.ManagedThreadId; entered.Set();
                                if (!release.Wait(5000)) throw new TimeoutException("Uncertain invitation pending read fixture deadline");
                            });
                            form.ShowToken = value => { ++handoffs; throw new Exception("Unconfirmed Create cannot hand off a token"); };
                            ((Label)Field(form, "status")).TextChanged += delegate { ++updates; };
                            HomeVpnWaitingTests.ModalWizardCase(form, delegate {
                                if (phase == 0) {
                                    if (!form.InitializationWork.IsCompleted) return false; form.InitializationWork.GetAwaiter().GetResult();
                                    ((TextBox)Field(form, "name")).Text = "Fixture friend";
                                    // Own fixture state models a request retained before the lost native response; no real mutation occurs.
                                    if (pending && mode != "corrupt") HomeVpnAdminRecovery.Register(Owner, "Fixture friend", null);
                                    if (mode == "corrupt") File.WriteAllBytes(PendingPath, new byte[] { 1, 2, 3 });
                                    before = Directory.GetFiles(HomeVpnPrivateFiles.Root).ToDictionary(path => path, File.ReadAllBytes);
                                    if (mode == "locked") held = File.Open(PendingPath, FileMode.Open, FileAccess.Read, FileShare.None);
                                    ((Button)Field(form, "create")).PerformClick(); first = form.ActionWork; phase = 1; return false;
                                }
                                if (phase == 1) {
                                    if (!entered.IsSet || ++turns < 3) return false;
                                    check(form.Modal && form.Visible && worker != ui && reads == 2 && mutations == 1 && queries == 0 && handoffs == 0
                                        && !first.IsCompleted && !form.PendingReadWork.IsCompleted && service.IsAdminWorking,
                                        "uncertain Create awaits one actual pending read on responsive explicit modal owner: " + mode);
                                    foreach (string button in new[] { "create", "revoke", "reissue", "refresh", "recover" })
                                        check(!((Button)Field(form, button)).Enabled, "uncertainty read retains admission gate: " + mode + "/" + button);
                                    ((Button)Field(form, "create")).PerformClick(); check(mutations == 1, "uncertain held Create cannot repeat issuing action: " + mode);
                                    var watch = Stopwatch.StartNew();
                                    if (closing) form.Close(); if (disposing) { form.Dispose(); closedUpdates = updates; }
                                    if (mode == "service-dispose") service.Dispose();
                                    if (mode == "stop-retry" || mode == "service-dispose") stop = service.StopAsync();
                                    check(watch.ElapsedMilliseconds < 500 && !first.IsCompleted && !form.PendingReadWork.IsCompleted
                                        && (!closing || !form.IsDisposed) && (stop == null || !stop.IsCompleted),
                                        "uncertain read Close Stop Dispose return promptly without abandoning real work: " + mode);
                                    turns = 0; phase = 2; return false;
                                }
                                if (phase == 2) {
                                    if (++turns < 3) return false;
                                    check(!first.IsCompleted && (stop == null || !stop.IsCompleted), "uncertainty cancellation retains its held read: " + mode);
                                    release.Set(); phase = 3; return false;
                                }
                                if (phase == 3) {
                                    if (!first.IsCompleted || (stop != null && !stop.IsCompleted)) return false; first.GetAwaiter().GetResult();
                                    if (stop != null) check(stop.GetAwaiter().GetResult(), "Stop joins uncertain Create pending read: " + mode);
                                    if (!closing && !disposing) {
                                        check((bool)Field(form, "needsRefresh") && !((Button)Field(form, "create")).Enabled,
                                            "lost Create response never permits blind reissue even when pending snapshot is empty: " + mode);
                                        check((bool)Field(form, "pendingAdmin") == (pending || stop != null)
                                            && (bool)Field(form, "pendingUnreadable") == (mode == "corrupt" || mode == "locked" || stop != null),
                                            "uncertainty snapshot separates retained unreadable and cancelled requests: " + mode);
                                        check(!((Label)Field(form, "status")).Text.Contains("PRIVATE_UNCERTAINTY_CANARY") && ((TextBox)Field(form, "name")).Text == "Fixture friend",
                                            "uncertainty guidance preserves draft without raw transport detail: " + mode);
                                    }
                                    if (disposing) check(updates == closedUpdates, "disposed uncertainty owner receives no late status");
                                    check(reads == 2 && mutations == 1 && handoffs == 0 && !service.IsAdminWorking,
                                        "uncertain Create performs exactly one snapshot without post-action duplicate or token consumption: " + mode);
                                    if (mode == "stop-retry") { ((Button)Field(form, "refresh")).PerformClick(); phase = 4; return false; }
                                    phase = 4;
                                }
                                if (!form.ActionWork.IsCompleted) return false; form.ActionWork.GetAwaiter().GetResult();
                                if (mode == "stop-retry") check(queries == 1 && reads == 3 && mutations == 1 && !((bool)Field(form, "pendingAdmin"))
                                    && !((bool)Field(form, "needsRefresh")) && ((Button)Field(form, "create")).Enabled,
                                    "explicit read-only Retry after Stop reconciles without automatic Create replay or stale gate overwrite");
                                if (held != null) { held.Dispose(); held = null; }
                                check(before.Count == Directory.GetFiles(HomeVpnPrivateFiles.Root).Length && before.All(item => item.Value.SequenceEqual(File.ReadAllBytes(item.Key))),
                                    "uncertainty read preserves exact protected request identity/result/access bytes: " + mode);
                                check(true, "invitation Create uncertainty UI PASS: " + mode); return true;
                            }, () => closing || disposing);
                        } finally {
                            release.Set(); if (held != null) held.Dispose(); if (form != null) form.Dispose();
                            if (form != null) { Pump(() => form.InitializationWork.IsCompleted && form.ActionWork.IsCompleted && form.PendingReadWork.IsCompleted, 5000); form.InitializationWork.GetAwaiter().GetResult(); form.ActionWork.GetAwaiter().GetResult(); }
                            service.Dispose(); var cleanup = service.StopAsync(); Pump(() => cleanup.IsCompleted, 5000);
                            check(cleanup.GetAwaiter().GetResult() && !service.HasOwnedResources, "uncertainty fixture joins actual body and cleanup: " + mode);
                        }
                    }
                    if (mode == "close-retry") {
                        using (var retryRelay = new Ikev2RelayService())
                        using (var retryService = new HomeVpnService(retryRelay, Application.ExecutablePath, 0, 0, 0))
                        using (var retry = new HomeInvitationsForm(new HomeVpnInvitation[0], (action, label, id) => { throw new Exception("Reopen must not repeat Create"); }, clipboard, retryService)) {
                            try {
                                HomeVpnWaitingTests.ModalWizardCase(retry, delegate {
                                    if (!retry.InitializationWork.IsCompleted) return false; retry.InitializationWork.GetAwaiter().GetResult();
                                    check((bool)Field(retry, "pendingAdmin") && !(bool)Field(retry, "pendingUnreadable") && ((Button)Field(retry, "recover")).Enabled
                                        && !((Button)Field(retry, "create")).Enabled, "closed uncertain Create reopens exact retained request without replay or stale empty publication"); return true;
                                }, () => false);
                            } finally {
                                retry.Dispose(); Pump(() => retry.InitializationWork.IsCompleted, 5000); retry.InitializationWork.GetAwaiter().GetResult();
                                retryService.Dispose(); var cleanup = retryService.StopAsync(); Pump(() => cleanup.IsCompleted, 5000);
                                check(cleanup.GetAwaiter().GetResult(), "uncertain Create reopen fixture joins actual cleanup");
                            }
                        }
                    }
                }
            } finally { if (original == null) File.Delete(PendingPath); else File.WriteAllBytes(PendingPath, original); }
        }
        private static void ReissueUncertaintyUi(ClipboardService clipboard)
        {
            byte[] original = File.Exists(PendingPath) ? File.ReadAllBytes(PendingPath) : null;
            try {
                foreach (string mode in new[] { "empty", "pending", "corrupt", "locked", "close-retry", "dispose", "stop-retry", "service-dispose" }) {
                    File.Delete(PendingPath);
                    using (var relay = new Ikev2RelayService())
                    using (var service = new HomeVpnService(relay, Application.ExecutablePath, 0, 0, 0))
                    using (var entered = new ManualResetEventSlim())
                    using (var release = new ManualResetEventSlim()) {
                        HomeInvitationsForm form = null; FileStream held = null; Task first = null; Task<bool> stop = null;
                        Dictionary<string, byte[]> before = null;
                        int reads = 0, revokes = 0, issues = 0, queries = 0, handoffs = 0, phase = 0, turns = 0, worker = 0, ui = Thread.CurrentThread.ManagedThreadId, updates = 0, closedUpdates = 0;
                        bool closing = mode == "close-retry", disposing = mode == "dispose", pending = mode != "empty" && mode != "stop-retry";
                        try {
                            var originalRow = new HomeVpnInvitation { Id = new string('d', 24), Name = "Fixture friend" };
                            var sibling = new HomeVpnInvitation { Id = new string('e', 24), Name = "Fixture friend" };
                            form = new HomeInvitationsForm(new[] { originalRow, sibling }, (action, label, id) => {
                                if (action == "list") { ++queries; return Task.FromResult("[{\"id\":\"" + originalRow.Id + "\",\"name\":\"Fixture friend\",\"revoked\":true},{\"id\":\"" + sibling.Id + "\",\"name\":\"Fixture friend\"}]"); }
                                if (action == "revoke") { ++revokes; check(id == originalRow.Id && label == null, "uncertain Reissue revokes only frozen selected ID: " + mode); return Task.FromResult("Access revoked."); }
                                ++issues; check(action == "invite" && label == originalRow.Name && id == originalRow.Id && revokes == 1, "uncertain Reissue retains its exact original intent: " + mode);
                                throw new IOException("PRIVATE_UNCERTAINTY_CANARY");
                            }, clipboard, service, delegate {
                                if (Interlocked.Increment(ref reads) != 2) return;
                                worker = Thread.CurrentThread.ManagedThreadId; entered.Set();
                                if (!release.Wait(5000)) throw new TimeoutException("Uncertain invitation pending read fixture deadline");
                            });
                            form.Confirm = value => true;
                            form.ShowToken = value => { ++handoffs; throw new Exception("Unconfirmed Reissue cannot hand off a token"); };
                            ((Label)Field(form, "status")).TextChanged += delegate { ++updates; };
                            HomeVpnWaitingTests.ModalWizardCase(form, delegate {
                                if (phase == 0) {
                                    if (!form.InitializationWork.IsCompleted) return false; form.InitializationWork.GetAwaiter().GetResult();
                                    ((TextBox)Field(form, "name")).Text = "Different draft";
                                    var rows = (HomeInvitationList)Field(form, "list");
                                    ((TextBox)Field(rows, "search")).Text = "Fixture";
                                    ((ListBox)Field(rows, "list")).SelectedIndex = 0;
                                    // Own fixture state models a request retained before the lost native response; no real mutation occurs.
                                    if (pending && mode != "corrupt") HomeVpnAdminRecovery.Register(Owner, originalRow.Name, originalRow.Id);
                                    if (mode == "corrupt") File.WriteAllBytes(PendingPath, new byte[] { 1, 2, 3 });
                                    before = Directory.GetFiles(HomeVpnPrivateFiles.Root).ToDictionary(path => path, File.ReadAllBytes);
                                    if (mode == "locked") held = File.Open(PendingPath, FileMode.Open, FileAccess.Read, FileShare.None);
                                    ((Button)Field(form, "reissue")).PerformClick(); first = form.ActionWork; phase = 1; return false;
                                }
                                if (phase == 1) {
                                    if (!entered.IsSet || ++turns < 3) return false;
                                    check(form.Modal && form.Visible && worker != ui && reads == 2 && revokes == 1 && issues == 1 && queries == 0 && handoffs == 0
                                        && !first.IsCompleted && !form.PendingReadWork.IsCompleted && service.IsAdminWorking,
                                        "uncertain Reissue awaits one actual pending read on responsive explicit modal owner: " + mode);
                                    check(originalRow.Revoked && !sibling.Revoked, "replacement uncertainty does not restore old access or revoke its same-name sibling: " + mode);
                                    foreach (string button in new[] { "create", "revoke", "reissue", "refresh", "recover" })
                                        check(!((Button)Field(form, button)).Enabled, "uncertainty read retains admission gate: " + mode + "/" + button);
                                    ((Button)Field(form, "reissue")).PerformClick(); check(revokes == 1 && issues == 1, "uncertain held Reissue cannot repeat issuing action: " + mode);
                                    var watch = Stopwatch.StartNew();
                                    if (closing) form.Close(); if (disposing) { form.Dispose(); closedUpdates = updates; }
                                    if (mode == "service-dispose") service.Dispose();
                                    if (mode == "stop-retry" || mode == "service-dispose") stop = service.StopAsync();
                                    check(watch.ElapsedMilliseconds < 500 && !first.IsCompleted && !form.PendingReadWork.IsCompleted
                                        && (!closing || !form.IsDisposed) && (stop == null || !stop.IsCompleted),
                                        "uncertain read Close Stop Dispose return promptly without abandoning real work: " + mode);
                                    turns = 0; phase = 2; return false;
                                }
                                if (phase == 2) {
                                    if (++turns < 3) return false;
                                    check(!first.IsCompleted && (stop == null || !stop.IsCompleted), "uncertainty cancellation retains its held read: " + mode);
                                    release.Set(); phase = 3; return false;
                                }
                                if (phase == 3) {
                                    if (!first.IsCompleted || (stop != null && !stop.IsCompleted)) return false; first.GetAwaiter().GetResult();
                                    if (stop != null) check(stop.GetAwaiter().GetResult(), "Stop joins uncertain Reissue pending read: " + mode);
                                    if (!closing && !disposing) {
                                        check((bool)Field(form, "needsRefresh") && !((Button)Field(form, "create")).Enabled,
                                            "lost Reissue response never permits blind reissue even when pending snapshot is empty: " + mode);
                                        check((bool)Field(form, "pendingAdmin") == (pending || stop != null)
                                            && (bool)Field(form, "pendingUnreadable") == (mode == "corrupt" || mode == "locked" || stop != null),
                                            "uncertainty snapshot separates retained unreadable and cancelled requests: " + mode);
                                        check(!((Label)Field(form, "status")).Text.Contains("PRIVATE_UNCERTAINTY_CANARY") && ((TextBox)Field(form, "name")).Text == "Different draft",
                                            "uncertainty guidance preserves draft without raw transport detail: " + mode);
                                    }
                                    if (!closing && !disposing) {
                                        var rows = (HomeInvitationList)Field(form, "list");
                                        check(rows.Selected != null && rows.Selected.Id == originalRow.Id && originalRow.Revoked && !sibling.Revoked
                                            && ((ListBox)Field(rows, "list")).Items.Count == 2 && ((TextBox)Field(rows, "search")).Text == "Fixture"
                                            && !((Button)Field(form, "reissue")).Enabled,
                                            "uncertain Reissue preserves exact selection, confirmed revocation, sibling and search without a replacement: " + mode);
                                    }
                                    if (disposing) check(updates == closedUpdates, "disposed uncertainty owner receives no late status");
                                    check(reads == 2 && revokes == 1 && issues == 1 && handoffs == 0 && !service.IsAdminWorking,
                                        "uncertain Reissue performs exactly one snapshot without post-action duplicate or token consumption: " + mode);
                                    if (mode == "stop-retry") { ((Button)Field(form, "refresh")).PerformClick(); phase = 4; return false; }
                                    phase = 4;
                                }
                                if (!form.ActionWork.IsCompleted) return false; form.ActionWork.GetAwaiter().GetResult();
                                if (mode == "stop-retry") check(queries == 1 && reads == 3 && revokes == 1 && issues == 1 && !((bool)Field(form, "pendingAdmin"))
                                    && !((bool)Field(form, "needsRefresh")) && ((Button)Field(form, "create")).Enabled,
                                    "explicit read-only Retry after Stop reconciles without automatic Reissue replay or stale gate overwrite");
                                if (mode == "stop-retry") {
                                    var rows = (HomeInvitationList)Field(form, "list");
                                    check(rows.Selected == null && ((ListBox)Field(rows, "list")).Items.Cast<HomeVpnInvitation>().Single(item => item.Id == originalRow.Id).Revoked
                                        && !((Button)Field(form, "reissue")).Enabled && ((TextBox)Field(form, "name")).Text == "Different draft",
                                        "explicit reconciliation keeps revoked identity and draft, requiring deliberate reselection before any reissue");
                                }
                                if (held != null) { held.Dispose(); held = null; }
                                check(before.Count == Directory.GetFiles(HomeVpnPrivateFiles.Root).Length && before.All(item => item.Value.SequenceEqual(File.ReadAllBytes(item.Key))),
                                    "uncertainty read preserves exact protected request identity/result/access bytes: " + mode);
                                check(true, "invitation Reissue uncertainty UI PASS: " + mode); return true;
                            }, () => closing || disposing);
                        } finally {
                            release.Set(); if (held != null) held.Dispose(); if (form != null) form.Dispose();
                            try {
                                if (form != null) { Pump(() => form.InitializationWork.IsCompleted && form.ActionWork.IsCompleted && form.PendingReadWork.IsCompleted, 5000); form.InitializationWork.GetAwaiter().GetResult(); form.ActionWork.GetAwaiter().GetResult(); }
                            } finally {
                                service.Dispose(); var cleanup = service.StopAsync(); Pump(() => cleanup.IsCompleted, 5000);
                                check(cleanup.GetAwaiter().GetResult() && !service.HasOwnedResources, "uncertainty fixture joins actual body and cleanup: " + mode);
                            }
                        }
                    }
                    if (mode == "close-retry") {
                        using (var retryRelay = new Ikev2RelayService())
                        using (var retryService = new HomeVpnService(retryRelay, Application.ExecutablePath, 0, 0, 0))
                        using (var retry = new HomeInvitationsForm(new HomeVpnInvitation[0], (action, label, id) => { throw new Exception("Reopen must not repeat Reissue"); }, clipboard, retryService)) {
                            try {
                                HomeVpnWaitingTests.ModalWizardCase(retry, delegate {
                                    if (!retry.InitializationWork.IsCompleted) return false; retry.InitializationWork.GetAwaiter().GetResult();
                                    check((bool)Field(retry, "pendingAdmin") && !(bool)Field(retry, "pendingUnreadable") && ((Button)Field(retry, "recover")).Enabled
                                        && !((Button)Field(retry, "create")).Enabled, "closed uncertain Reissue reopens exact retained request without replay or stale empty publication"); return true;
                                }, () => false);
                            } finally {
                                retry.Dispose(); Pump(() => retry.InitializationWork.IsCompleted, 5000); retry.InitializationWork.GetAwaiter().GetResult();
                                retryService.Dispose(); var cleanup = retryService.StopAsync(); Pump(() => cleanup.IsCompleted, 5000);
                                check(cleanup.GetAwaiter().GetResult(), "uncertain Reissue reopen fixture joins actual cleanup");
                            }
                        }
                    }
                }
            } finally { if (original == null) File.Delete(PendingPath); else File.WriteAllBytes(PendingPath, original); }
        }
        private static void RecoverNameUi(ClipboardService clipboard)
        {
            byte[] original = File.Exists(PendingPath) ? File.ReadAllBytes(PendingPath) : null;
            try {
                foreach (string mode in new[] { "complete", "missing", "corrupt", "locked", "close-retry", "dispose", "stop-retry", "service-dispose", "stop-result" }) {
                    File.Delete(PendingPath); var request = HomeVpnAdminRecovery.Register(Owner, "Retained friend", new string('d', 24));
                    using (var relay = new Ikev2RelayService())
                    using (var service = new HomeVpnService(relay, Application.ExecutablePath, 0, 0, 0))
                    using (var entered = new ManualResetEventSlim())
                    using (var release = new ManualResetEventSlim()) {
                        HomeInvitationsForm form = null; FileStream held = null; Task first = null; Task<bool> stop = null;
                        var result = new TaskCompletionSource<string>();
                        int reads = 0, calls = 0, queries = 0, handoffs = 0, phase = 0, turns = 0, worker = 0, ui = Thread.CurrentThread.ManagedThreadId, changes = 0, closedChanges = 0;
                        bool closing = mode == "close-retry", disposing = mode == "dispose";
                        var row = new HomeVpnInvitation { Id = new string('e', 24), Name = "Same-name survivor" };
                        Dictionary<string, byte[]> before = null;
                        try {
                            form = new HomeInvitationsForm(new[] { row }, (action, label, id) => {
                                if (action == "list") { ++queries; return Task.FromResult(Json.Serialize(new[] { row })); }
                                ++calls; check(action == "recover-invite" && label == null && id == null,
                                    "name read admits only existing read-only recovery, never invite/revoke: " + mode);
                                check(HomeVpnAdminRecovery.Pending().RequestId == request.RequestId, "recovery keeps the exact durable request ID: " + mode);
                                return mode == "stop-result" ? result.Task : Task.FromResult(response);
                            }, clipboard, service, delegate {
                                if (Interlocked.Increment(ref reads) != 2) return;
                                worker = Thread.CurrentThread.ManagedThreadId; entered.Set();
                                if (!release.Wait(5000)) throw new TimeoutException("Recovery name fixture read deadline");
                            });
                            form.ShowToken = value => {
                                ++handoffs;
                                var rows = (HomeInvitationList)Field(form, "list"); var access = HomeVpnAccess.Parse(response);
                                check(value == response && rows.Selected != null && rows.Selected.Id == access.InviteId && rows.Selected.Name == request.Name
                                    && ((TextBox)Field(form, "name")).Text == "Different draft",
                                    "recovery publishes the original name with exact returned ID, never the editable draft: " + mode);
                                // Stop before the separate consumption boundary; retain the protected request for explicit retry.
                                throw new HomeVpnAdminPendingException("Fixture handoff intentionally unconfirmed");
                            };
                            ((Label)Field(form, "status")).TextChanged += delegate { ++changes; };
                            HomeVpnWaitingTests.ModalWizardCase(form, delegate {
                                if (phase == 0) {
                                    if (!form.InitializationWork.IsCompleted) return false; form.InitializationWork.GetAwaiter().GetResult();
                                    check(((Button)Field(form, "recover")).Enabled, "readable retained request enables explicit recovery: " + mode);
                                    ((TextBox)Field(form, "name")).Text = "Different draft";
                                    var rows = (HomeInvitationList)Field(form, "list"); ((ListBox)Field(rows, "list")).SelectedIndex = 0;
                                    if (mode == "missing") File.Delete(PendingPath);
                                    if (mode == "corrupt") File.WriteAllBytes(PendingPath, new byte[] { 1, 2, 3 });
                                    before = Directory.GetFiles(HomeVpnPrivateFiles.Root).ToDictionary(path => path, File.ReadAllBytes);
                                    if (mode == "locked") held = File.Open(PendingPath, FileMode.Open, FileAccess.Read, FileShare.None);
                                    ((Button)Field(form, "recover")).PerformClick(); first = form.ActionWork; phase = 1; return false;
                                }
                                if (phase == 1) {
                                    if (!entered.IsSet || ++turns < 3) return false;
                                    var rows = (HomeInvitationList)Field(form, "list");
                                    check(form.Modal && form.Visible && worker != ui && calls == 0 && reads == 2 && service.IsAdminWorking
                                        && !first.IsCompleted && !form.PendingReadWork.IsCompleted && rows.Selected != null && rows.Selected.Id == row.Id,
                                        "actual modal recovery name read stays responsive, owned and preserves selection before dispatch: " + mode);
                                    foreach (string button in new[] { "create", "revoke", "reissue", "refresh", "recover" })
                                        check(!((Button)Field(form, button)).Enabled, "recovery name read retains mutation/reentry gate: " + mode + "/" + button);
                                    ((Button)Field(form, "recover")).PerformClick(); check(calls == 0, "held name read cannot duplicate recovery: " + mode);
                                    var watch = Stopwatch.StartNew();
                                    if (closing) form.Close(); if (disposing) { form.Dispose(); closedChanges = changes; }
                                    if (mode == "service-dispose") service.Dispose();
                                    if (mode == "stop-retry" || mode == "service-dispose") stop = service.StopAsync();
                                    check(watch.ElapsedMilliseconds < 500 && !first.IsCompleted && (stop == null || !stop.IsCompleted)
                                        && (!closing || !form.IsDisposed), "Close Stop Dispose do not abandon held recovery name read: " + mode);
                                    turns = 0; phase = 2; return false;
                                }
                                if (phase == 2) {
                                    if (++turns < 3) return false;
                                    check(!first.IsCompleted && (stop == null || !stop.IsCompleted), "cancel retains physical name read until settlement: " + mode);
                                    release.Set(); phase = 3; return false;
                                }
                                if (phase == 3) {
                                    if (mode == "stop-result") {
                                        if (calls != 1) return false;
                                        check(!first.IsCompleted && handoffs == 0, "recovery result is retained before Stop");
                                        stop = service.StopAsync(); result.TrySetResult(response); phase = 4; return false;
                                    }
                                    phase = 4;
                                }
                                if (phase == 4) {
                                    if (!first.IsCompleted || (stop != null && !stop.IsCompleted)) return false; first.GetAwaiter().GetResult();
                                    if (stop != null) check(stop.GetAwaiter().GetResult(), "Stop joins owned recovery name read: " + mode);
                                    bool shown = mode == "complete";
                                    check(reads == 2 && calls == (shown || mode == "stop-result" ? 1 : 0) && handoffs == (shown ? 1 : 0)
                                        && !service.IsAdminWorking, "name read never dispatches after refusal/cancel or consumes/replays a request: " + mode);
                                    if (!closing && !disposing) {
                                        check((bool)Field(form, "needsRefresh") && !((Button)Field(form, "create")).Enabled && !((Button)Field(form, "reissue")).Enabled
                                            && ((TextBox)Field(form, "name")).Text == "Different draft", "recovery outcome keeps reconciliation and draft gates: " + mode);
                                        if (!shown) { var rows = (HomeInvitationList)Field(form, "list"); check(rows.Selected != null && rows.Selected.Id == row.Id,
                                            "refused or stopped recovery cannot replace selected access: " + mode); }
                                    }
                                    if (disposing) check(changes == closedChanges, "disposed recovery name owner receives no late status");
                                    if (mode == "stop-retry") { ((Button)Field(form, "refresh")).PerformClick(); phase = 5; return false; }
                                    phase = 6;
                                }
                                if (phase == 5) {
                                    if (!form.ActionWork.IsCompleted) return false; form.ActionWork.GetAwaiter().GetResult();
                                    check(queries == 1 && reads == 3 && calls == 0 && ((Button)Field(form, "recover")).Enabled,
                                        "explicit Refresh after Stop restores recovery without mutating/replaying the request");
                                    ((Button)Field(form, "recover")).PerformClick(); phase = 6; return false;
                                }
                                if (!form.ActionWork.IsCompleted) return false; form.ActionWork.GetAwaiter().GetResult();
                                if (mode == "stop-retry") check(reads == 4 && calls == 1 && handoffs == 1,
                                    "explicit recovery Retry reads original name once, with no stale cancelled result or duplicate dispatch");
                                if (held != null) { held.Dispose(); held = null; }
                                check(before.Count == Directory.GetFiles(HomeVpnPrivateFiles.Root).Length && before.All(item => item.Value.SequenceEqual(File.ReadAllBytes(item.Key))),
                                    "recovery name path preserves exact protected ID/request/result/access bytes: " + mode);
                                check(true, "invitation Recover name UI PASS: " + mode); return true;
                            }, () => closing || disposing);
                        } finally {
                            release.Set(); result.TrySetResult(response); if (held != null) held.Dispose(); if (form != null) form.Dispose();
                            try { if (form != null) { Pump(() => form.InitializationWork.IsCompleted && form.ActionWork.IsCompleted && form.PendingReadWork.IsCompleted, 5000); form.ActionWork.GetAwaiter().GetResult(); } }
                            finally { service.Dispose(); var cleanup = service.StopAsync(); Pump(() => cleanup.IsCompleted, 5000);
                                check(cleanup.GetAwaiter().GetResult() && !service.HasOwnedResources, "recovery name fixture joins body and exact cleanup: " + mode); }
                        }
                    }
                    if (mode == "close-retry") {
                        using (var retryRelay = new Ikev2RelayService())
                        using (var retryService = new HomeVpnService(retryRelay, Application.ExecutablePath, 0, 0, 0))
                        using (var retry = new HomeInvitationsForm(new HomeVpnInvitation[0], (action, label, id) => { throw new Exception("Reopen cannot automatically recover or issue"); }, clipboard, retryService)) {
                            try {
                                HomeVpnWaitingTests.ModalWizardCase(retry, delegate {
                                    if (!retry.InitializationWork.IsCompleted) return false; retry.InitializationWork.GetAwaiter().GetResult();
                                    check(((Button)Field(retry, "recover")).Enabled && !((Button)Field(retry, "create")).Enabled
                                        && HomeVpnAdminRecovery.Pending().RequestId == request.RequestId, "reopen retains original recovery ID and explicit recovery admission"); return true;
                                }, () => false);
                            } finally { retry.Dispose(); Pump(() => retry.InitializationWork.IsCompleted, 5000);
                                retryService.Dispose(); var cleanup = retryService.StopAsync(); Pump(() => cleanup.IsCompleted, 5000); check(cleanup.GetAwaiter().GetResult(), "recovery name reopen joins cleanup"); }
                        }
                    }
                }
            } finally { if (original == null) File.Delete(PendingPath); else File.WriteAllBytes(PendingPath, original); }
        }
        private static void ConsumptionWorker()
        {
            byte[] original = File.Exists(PendingPath) ? File.ReadAllBytes(PendingPath) : null;
            try {
                foreach (string mode in new[] { "complete", "cancel-read", "cancel-consume", "refused", "locked", "changed-request", "changed-result", "missing" }) {
                    File.Delete(PendingPath);
                    if (mode != "missing") { var request = HomeVpnAdminRecovery.Register(Owner, "My iPhone", null); HomeVpnAdminRecovery.RetainResult(Owner, request, response); }
                    byte[] expectedJournal = File.Exists(PendingPath) ? File.ReadAllBytes(PendingPath) : null;
                    var before = Directory.GetFiles(HomeVpnPrivateFiles.Root).Where(path => path != PendingPath).ToDictionary(path => path, File.ReadAllBytes);
                    using (var form = new Form())
                    using (var entered = new ManualResetEventSlim())
                    using (var release = new ManualResetEventSlim())
                    using (var cancellation = new CancellationTokenSource()) {
                        FileStream held = null; Task<bool> operation = null; int ui = Thread.CurrentThread.ManagedThreadId, worker = 0, phase = 0, turns = 0, allowed = 1, admissions = 0;
                        var phaseWatch = new Stopwatch();
                        try {
                            HomeVpnWaitingTests.ModalWizardCase(form, delegate {
                                if (phase == 0) {
                                    check(form.Modal, "invitation consumption worker runs under actual ShowDialog: " + mode);
                                    phaseWatch.Restart(); var watch = Stopwatch.StartNew();
                                    operation = HomeVpnAdminRecovery.ConfirmConsumedAsync(response, cancellation.Token,
                                        () => { Interlocked.Increment(ref admissions); return Volatile.Read(ref allowed) != 0; }, stage => {
                                            if (stage != (mode == "cancel-read" || mode == "missing" ? "read" : "consume")) return;
                                            Interlocked.Exchange(ref worker, Thread.CurrentThread.ManagedThreadId); entered.Set();
                                            if (!release.Wait(5000)) throw new TimeoutException("Invitation consumption fixture deadline");
                                        });
                                    check(watch.ElapsedMilliseconds < 500, "invitation consumption publishes owned work before real receipt read/delete: " + mode);
                                    phase = 1; return false;
                                }
                                if (phase == 1) {
                                    if (!entered.IsSet || ++turns < 3) {
                                        if (phaseWatch.ElapsedMilliseconds >= 3000) throw new TimeoutException("Invitation consumption heartbeat deadline: " + mode + "; task=" + operation.Status);
                                        return false;
                                    }
                                    check(worker != ui && !operation.IsCompleted && admissions == 0 && Failure(HomeVpnAdminRecovery.AcquireAsync()) is HomeVpnAdminPendingException,
                                        "held consumption retains its actual family lease and responsive modal before memory admission: " + mode);
                                    if (mode == "cancel-read" || mode == "cancel-consume") cancellation.Cancel();
                                    if (mode == "refused") Interlocked.Exchange(ref allowed, 0);
                                    if (mode == "locked") held = File.Open(PendingPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                                    if (mode == "changed-request" || mode == "changed-result") {
                                        var changed = HomeVpnAdminRecovery.Pending();
                                        if (mode == "changed-request") changed.RequestId = Guid.NewGuid().ToString("N");
                                        else changed.Result = ChangeToken(response, "InviteId", new string('d', 24));
                                        HomeVpnPrivateFiles.Save(HomeVpnAdminRecovery.StorageName, Json.Serialize(changed)); expectedJournal = File.ReadAllBytes(PendingPath);
                                    }
                                    release.Set(); phaseWatch.Restart(); phase = 2; return false;
                                }
                                if (!operation.IsCompleted) {
                                    if (phaseWatch.ElapsedMilliseconds >= 5000) throw new TimeoutException("Invitation consumption settlement deadline: " + mode + "; task=" + operation.Status);
                                    return false;
                                }
                                bool consumed = false; Exception failure = null; try { consumed = operation.GetAwaiter().GetResult(); } catch (Exception ex) { failure = ex; }
                                if (held != null) { held.Dispose(); held = null; }
                                if (mode == "complete") check(failure == null && consumed && !File.Exists(PendingPath) && admissions == 1, "accepted invitation consumption deletes its actual matching protected receipt once");
                                else if (mode == "missing") check(failure == null && !consumed && !File.Exists(PendingPath) && admissions == 0, "missing receipt returns explicit no-op without inventing a consumed request");
                                else {
                                    check((mode.StartsWith("cancel-", StringComparison.Ordinal) || mode == "refused" ? failure is OperationCanceledException : failure is HomeVpnAdminPendingException)
                                        && !consumed && expectedJournal.SequenceEqual(File.ReadAllBytes(PendingPath)) && admissions == (mode == "locked" || mode == "refused" ? 1 : 0),
                                        "cancel/refusal/real delete lock/changed request or result preserves the actual previous or newer receipt: " + mode);
                                }
                                var after = Directory.GetFiles(HomeVpnPrivateFiles.Root).Where(path => path != PendingPath).ToArray();
                                check(before.Count == after.Length && before.All(item => item.Value.SequenceEqual(File.ReadAllBytes(item.Key))), "consumption preserves access/owner/other protected file set and bytes: " + mode);
                                using (var released = HomeVpnAdminRecovery.AcquireAsync().GetAwaiter().GetResult()) check(released != null, "consumption releases its family lease only after actual read/delete settles: " + mode);
                                return true;
                            }, () => false);
                        } finally {
                            cancellation.Cancel(); release.Set(); if (held != null) held.Dispose(); form.Dispose();
                            if (operation != null) { Pump(() => operation.IsCompleted, 5000); try { operation.GetAwaiter().GetResult(); } catch (OperationCanceledException) { } catch (HomeVpnAdminPendingException) { } }
                        }
                    }
                }
            } finally { if (original == null) File.Delete(PendingPath); else File.WriteAllBytes(PendingPath, original); }
        }
        private static void RecoverConsumptionUi(ClipboardService clipboard)
        {
            byte[] original = File.Exists(PendingPath) ? File.ReadAllBytes(PendingPath) : null;
            try {
                foreach (string mode in new[] { "complete", "close-retry", "dispose", "stop-retry", "service-dispose", "locked", "changed-request", "changed-result" }) {
                    File.Delete(PendingPath); var request = HomeVpnAdminRecovery.Register(Owner, "Retained friend", new string('d', 24));
                    HomeVpnAdminRecovery.RetainResult(Owner, request, response); byte[] receipt = File.ReadAllBytes(PendingPath);
                    var before = Directory.GetFiles(HomeVpnPrivateFiles.Root).Where(path => path != PendingPath).ToDictionary(path => path, File.ReadAllBytes);
                    int calls = 0, handoffs = 0;
                    using (var relay = new Ikev2RelayService())
                    using (var service = new HomeVpnService(relay, Application.ExecutablePath, 0, 0, 0))
                    using (var entered = new ManualResetEventSlim())
                    using (var release = new ManualResetEventSlim()) {
                        HomeInvitationsForm form = null; FileStream held = null; Task first = null; Task<bool> stop = null;
                        int consumeReads = 0, phase = 0, turns = 0, worker = 0, ui = Thread.CurrentThread.ManagedThreadId, changes = 0, closedChanges = 0;
                        bool closing = mode == "close-retry", disposing = mode == "dispose", retrying = mode == "stop-retry" || mode == "locked";
                        Func<string, string, string, Task<string>> admin = (action, label, id) => {
                            if (action == "list") return Task.FromResult("[]");
                            ++calls; check(action == "recover-invite" && label == null && id == null && HomeVpnAdminRecovery.Pending().RequestId == request.RequestId,
                                "consumption Retry queries only the original recovery ID, never issues/revokes: " + mode);
                            return Task.FromResult(response);
                        };
                        try {
                            form = new HomeInvitationsForm(new HomeVpnInvitation[0], admin, clipboard, service);
                            form.ShowToken = value => { ++handoffs; check(value == response, "consumption follows an explicit handoff of the exact recovered token: " + mode); };
                            form.BeforeConsumeIo = stage => {
                                if (stage != "consume" || Interlocked.Increment(ref consumeReads) != 1) return;
                                worker = Thread.CurrentThread.ManagedThreadId; entered.Set();
                                if (!release.Wait(5000)) throw new TimeoutException("Recovery consumption fixture deadline");
                            };
                            ((Label)Field(form, "status")).TextChanged += delegate { ++changes; };
                            HomeVpnWaitingTests.ModalWizardCase(form, delegate {
                                if (phase == 0) {
                                    if (!form.InitializationWork.IsCompleted) return false; form.InitializationWork.GetAwaiter().GetResult();
                                    ((TextBox)Field(form, "name")).Text = "Different draft";
                                    ((Button)Field(form, "recover")).PerformClick(); first = form.ActionWork; phase = 1; return false;
                                }
                                if (phase == 1) {
                                    if (!entered.IsSet || ++turns < 3) return false;
                                    check(form.Modal && form.Visible && worker != ui && calls == 1 && handoffs == 1 && !first.IsCompleted
                                        && !form.ConsumptionWork.IsCompleted && service.IsAdminWorking && Failure(HomeVpnAdminRecovery.AcquireAsync()) is HomeVpnAdminPendingException,
                                        "held Recover consumption keeps actual lease/work and responsive explicit modal owner: " + mode);
                                    foreach (string button in new[] { "create", "revoke", "reissue", "refresh", "recover" })
                                        check(!((Button)Field(form, button)).Enabled, "consumption retains reentry/mutation gate: " + mode + "/" + button);
                                    ((Button)Field(form, "recover")).PerformClick(); check(calls == 1 && consumeReads == 1, "held consumption never repeats recovery or delete admission: " + mode);
                                    if (mode == "locked") held = File.Open(PendingPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                                    if (mode.StartsWith("changed-", StringComparison.Ordinal)) {
                                        var changed = HomeVpnAdminRecovery.Pending();
                                        if (mode == "changed-request") changed.RequestId = Guid.NewGuid().ToString("N");
                                        else changed.Result = ChangeToken(response, "InviteId", new string('f', 24));
                                        HomeVpnPrivateFiles.Save(HomeVpnAdminRecovery.StorageName, Json.Serialize(changed)); receipt = File.ReadAllBytes(PendingPath);
                                    }
                                    var watch = Stopwatch.StartNew();
                                    if (closing) form.Close(); if (disposing) { form.Dispose(); closedChanges = changes; }
                                    if (mode == "service-dispose") service.Dispose();
                                    if (mode == "stop-retry" || mode == "service-dispose") stop = service.StopAsync();
                                    check(watch.ElapsedMilliseconds < 500 && !first.IsCompleted && !form.ConsumptionWork.IsCompleted
                                        && (stop == null || !stop.IsCompleted) && (!closing || !form.IsDisposed), "Close Stop Dispose retain physical consumption without blocking UI: " + mode);
                                    turns = 0; phase = 2; return false;
                                }
                                if (phase == 2) {
                                    if (++turns < 3) return false;
                                    check(!first.IsCompleted && (stop == null || !stop.IsCompleted), "cancelled consumption stays owned until actual settlement: " + mode);
                                    release.Set(); phase = 3; return false;
                                }
                                if (phase == 3) {
                                    if (!first.IsCompleted || (stop != null && !stop.IsCompleted)) return false; first.GetAwaiter().GetResult();
                                    if (stop != null) check(stop.GetAwaiter().GetResult(), "Stop joins actual receipt consumption: " + mode);
                                    bool consumed = mode == "complete";
                                    check((consumed ? form.ConsumptionWork.Status == TaskStatus.RanToCompletion && !File.Exists(PendingPath)
                                        : File.Exists(PendingPath) && receipt.SequenceEqual(File.ReadAllBytes(PendingPath))) && calls == 1 && handoffs == 1 && !service.IsAdminWorking,
                                        "accepted delete completes once; cancelled/refused/newer request remains exact and no late recovery repeats: " + mode);
                                    if (disposing) check(changes == closedChanges, "disposed consumption owner receives no late status");
                                    if (!closing && !disposing) check((bool)Field(form, "needsRefresh") && !((Button)Field(form, "create")).Enabled
                                        && ((TextBox)Field(form, "name")).Text == "Different draft", "settled consumption preserves draft and reconciliation gate: " + mode);
                                    if (held != null) { held.Dispose(); held = null; }
                                    if (retrying) { ((Button)Field(form, "refresh")).PerformClick(); phase = 4; return false; }
                                    phase = 5;
                                }
                                if (phase == 4) {
                                    if (!form.ActionWork.IsCompleted) return false; form.ActionWork.GetAwaiter().GetResult();
                                    check(((Button)Field(form, "recover")).Enabled && calls == 1 && File.Exists(PendingPath), "explicit Refresh restores recovery admission without repeated consumption");
                                    ((Button)Field(form, "recover")).PerformClick(); phase = 5; return false;
                                }
                                if (!form.ActionWork.IsCompleted) return false; form.ActionWork.GetAwaiter().GetResult();
                                if (retrying) check(calls == 2 && handoffs == 2 && consumeReads == 2 && !File.Exists(PendingPath)
                                    && form.ConsumptionWork.Status == TaskStatus.RanToCompletion, "explicit Retry consumes the same accepted request once after cancellation/lock, without stale overwrite");
                                check(before.Count == Directory.GetFiles(HomeVpnPrivateFiles.Root).Count(path => path != PendingPath)
                                    && before.All(item => item.Value.SequenceEqual(File.ReadAllBytes(item.Key))), "consumption preserves all access/owner/other private bytes: " + mode);
                                using (var lease = HomeVpnAdminRecovery.AcquireAsync().GetAwaiter().GetResult()) check(lease != null, "consumption UI releases family only after physical settlement: " + mode);
                                check(true, "invitation Recover consumption UI PASS: " + mode); return true;
                            }, () => closing || disposing);
                        } finally {
                            release.Set(); if (held != null) held.Dispose(); if (form != null) form.Dispose();
                            try { if (form != null) { Pump(() => form.InitializationWork.IsCompleted && form.ActionWork.IsCompleted && form.ConsumptionWork.IsCompleted, 5000); form.ActionWork.GetAwaiter().GetResult(); } }
                            finally { service.Dispose(); var cleanup = service.StopAsync(); Pump(() => cleanup.IsCompleted, 5000);
                                check(cleanup.GetAwaiter().GetResult() && !service.HasOwnedResources, "consumption fixture joins physical work/cleanup: " + mode); }
                        }
                    }
                    if (mode == "close-retry") {
                        using (var retryRelay = new Ikev2RelayService())
                        using (var retryService = new HomeVpnService(retryRelay, Application.ExecutablePath, 0, 0, 0))
                        using (var retry = new HomeInvitationsForm(new HomeVpnInvitation[0], (action, label, id) => {
                            ++calls; check(action == "recover-invite" && HomeVpnAdminRecovery.Pending().RequestId == request.RequestId,
                                "reopen Retry uses original accepted request without issuing"); return Task.FromResult(response);
                        }, clipboard, retryService)) {
                            int phase = 0; retry.ShowToken = value => { ++handoffs; check(value == response, "reopen handoff retains exact result"); };
                            try {
                                HomeVpnWaitingTests.ModalWizardCase(retry, delegate {
                                    if (phase == 0) { if (!retry.InitializationWork.IsCompleted) return false; retry.InitializationWork.GetAwaiter().GetResult();
                                        ((Button)Field(retry, "recover")).PerformClick(); phase = 1; return false; }
                                    if (!retry.ActionWork.IsCompleted) return false; retry.ActionWork.GetAwaiter().GetResult();
                                    check(calls == 2 && handoffs == 2 && !File.Exists(PendingPath) && retry.ConsumptionWork.Status == TaskStatus.RanToCompletion,
                                        "Cancel Close then explicit reopen Retry consumes original request once without late cancelled publication"); return true;
                                }, () => false);
                            } finally { retry.Dispose(); Pump(() => retry.InitializationWork.IsCompleted && retry.ActionWork.IsCompleted && retry.ConsumptionWork.IsCompleted, 5000);
                                retryService.Dispose(); var cleanup = retryService.StopAsync(); Pump(() => cleanup.IsCompleted, 5000); check(cleanup.GetAwaiter().GetResult(), "consumption reopen joins cleanup"); }
                        }
                    }
                }
            } finally { if (original == null) File.Delete(PendingPath); else File.WriteAllBytes(PendingPath, original); }
        }
        private static void Pump(Func<bool> ready, int timeout = 15000)
        {
            var watch = Stopwatch.StartNew(); while (!ready()) { Application.DoEvents(); Thread.Sleep(10); if (watch.ElapsedMilliseconds > timeout) throw new Exception("Admin recovery fixture timeout"); } Application.DoEvents();
        }
        private static Exception Failure(Task task)
        {
            Pump(() => task.IsCompleted); try { task.GetAwaiter().GetResult(); } catch (Exception ex) { return ex; }
            throw new Exception("Expected admin recovery refusal");
        }
        private static bool Gone(int id) { try { using (var process = Process.GetProcessById(id)) return process.HasExited; } catch (ArgumentException) { return true; } }
        private static int Id(string name) { return Int32.Parse(File.ReadAllText(At(name))); }
        private static void KillFixtures()
        {
            foreach (string name in new[] { "pid", "child" }) {
                if (!File.Exists(At(name))) continue;
                try { using (var process = Process.GetProcessById(Id(name))) { if (!process.HasExited) { process.Kill(); process.WaitForExit(2000); } } } catch (ArgumentException) { }
            }
        }
        private static void Reset(string mode)
        {
            KillFixtures(); foreach (string path in Directory.GetFiles(folder)) File.Delete(path);
            File.Delete(PendingPath); copies = 0; File.WriteAllText(At("mode"), mode); File.WriteAllText(At("response-token"), response);
        }
        private static string[] Calls() { return File.Exists(At("calls")) ? File.ReadAllLines(At("calls")) : new string[0]; }
        private static Task<string> Admin(string action, string label = null, string source = null, HomeVpnOwner owner = null, int timeout = 20000)
        {
            return HomeVpnService.AdminAsync(owner ?? Owner, action, label, source, delegate { }, (exe, arguments) => {
                copies++; check(exe == "scp.exe" && arguments.Contains("ConnectTimeout=15") && arguments.Contains(" -P 22 "), "invite keeps the production OpenSSH copy options");
                return Task.FromResult(0);
            }, Application.ExecutablePath, timeout);
        }
        private static Dictionary<string, byte[]> SourceSnapshot(Dictionary<string, byte[]> paths)
        {
            var result = new Dictionary<string, byte[]>();
            foreach (string path in paths.Keys.Where(path => path != PendingPath && !path.EndsWith("setup-request.dat", StringComparison.Ordinal))) Backup(result, path);
            return result;
        }
        private static bool Unchanged(Dictionary<string, byte[]> before)
        { return before.All(pair => pair.Value == null ? !File.Exists(pair.Key) : File.Exists(pair.Key) && pair.Value.SequenceEqual(File.ReadAllBytes(pair.Key))); }
        private static void NativeWait(string route, Dictionary<string, byte[]> paths)
        {
            Reset("hold"); var before = SourceSnapshot(paths); int ticks = 0, root = 0, child = 0; byte[] journal = null; Exception controllerFailure = null;
            using (var host = new Form())
            using (var heartbeat = new System.Windows.Forms.Timer { Interval = 20 })
            using (var controller = new System.Windows.Forms.Timer { Interval = 20 }) {
                host.Show(); heartbeat.Tick += delegate { ticks++; }; heartbeat.Start();
                controller.Tick += delegate {
                    var wait = Application.OpenForms.OfType<HomeVpnPreparationForm>().FirstOrDefault();
                    try {
                        if (wait == null || !File.Exists(At("child")) || ticks < 3) return;
                        controller.Stop(); root = Id("pid"); child = Id("child"); journal = File.ReadAllBytes(PendingPath);
                        var cancel = (Button)Field(wait, "cancel"); var status = (Label)Field(wait, "status");
                        check(cancel.Enabled && wait.CancelButton == cancel && wait.AcceptButton == null && status.Text.Contains("5 минут")
                            && status.Text.IndexOf("запрос", StringComparison.OrdinalIgnoreCase) >= 0 && status.AccessibilityObject.Description == status.Text,
                            "production invite wait remains responsive and describes the retained request");
                        if (route == "button") {
                            Shot(wait, "vps-invite-wait-pending"); wait.ClientSize = new Size(440, 270); Application.DoEvents();
                            check(wait.RectangleToClient(cancel.RectangleToScreen(cancel.ClientRectangle)).Bottom <= wait.ClientSize.Height,
                                "minimum invitation wait keeps the actual cancellation button visible");
                            Shot(wait, "vps-invite-wait-minimum"); cancel.PerformClick();
                        }
                        else if (route == "escape") typeof(Form).GetMethod("ProcessDialogKey", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(wait, new object[] { Keys.Escape });
                        else if (route == "close") wait.Close(); else if (route == "dispose") wait.Dispose();
                    } catch (Exception ex) { controllerFailure = ex; controller.Stop(); if (wait != null && !wait.IsDisposed) wait.Close(); }
                }; controller.Start();
                var failure = Failure(Admin("invite", "Frozen friend", null, null, route == "deadline" ? 5000 : 20000));
                if (controllerFailure != null) throw new Exception("Invite wait controller failed", controllerFailure);
                var pending = HomeVpnAdminRecovery.Pending();
                check(failure is HomeVpnAdminPendingException && journal != null && journal.SequenceEqual(File.ReadAllBytes(PendingPath))
                    && pending.Name == "Frozen friend" && pending.Result == null && ticks >= 3 && Gone(root) && Gone(child)
                    && copies == 1 && Calls().SequenceEqual(new[] { "invite" }) && Unchanged(before),
                    "owned invite wait cancellation/deadline settles its tree, preserves the frozen request/access/helpers and never retries: " + route);
                check(failure.Message.Contains(route == "deadline" ? "Время ожидания SSH истекло" : "Ожидание SSH прервано")
                    && failure.Message.Contains("прежнюю выдачу"), "invitation wait distinguishes deadline from requested cancellation and directs recovery: " + route);
                check(!Encoding.UTF8.GetString(journal).Contains(pending.RequestId) && !Encoding.UTF8.GetString(journal).Contains(Owner.Host)
                    && Directory.GetAccessControl(HomeVpnPrivateFiles.Root).AreAccessRulesProtected, "native invite request is protected by DPAPI and private ACL before dispatch");
                host.Close();
            }
        }
        private static void LostResponseAndReopen(string token, ClipboardService clipboard, Dictionary<string, byte[]> paths)
        {
            Reset("lost"); var before = SourceSnapshot(paths); var failure = Failure(Admin("invite", "Original friend"));
            var pending = HomeVpnAdminRecovery.Pending(); byte[] frozen = File.ReadAllBytes(PendingPath); string id = pending.RequestId;
            check(failure is HomeVpnAdminPendingException && pending.Result == null && Calls().SequenceEqual(new[] { "invite" })
                && Json.Deserialize<HomeVpnAdminRequest>(File.ReadAllText(At("receipt"))).RequestId == id && Unchanged(before),
                "lost native SSH response records one issuance and preserves its original protected request");
            int beforeCopies = copies, beforeCalls = Calls().Length;
            foreach (var owner in new[] { new HomeVpnOwner { Host = "other.example.org", Port = 22, Login = "root" },
                new HomeVpnOwner { Host = Owner.Host, Port = 2200, Login = "root" }, new HomeVpnOwner { Host = Owner.Host, Port = 22, Login = "admin" } })
                check(Failure(Admin("recover-invite", null, null, owner)) is HomeVpnAdminPendingException, "recovery rejects a changed VPS, SSH port or owner before dispatch");
            check(Failure(Admin("invite", "Edited friend")) is HomeVpnAdminPendingException && copies == beforeCopies && Calls().Length == beforeCalls
                && frozen.SequenceEqual(File.ReadAllBytes(PendingPath)), "pending invitation blocks a changed name and owner without copying, replaying or rewriting its frozen journal");
            File.WriteAllText(At("mode"), "complete");
            using (var reopened = new HomeInvitationsForm(new HomeVpnInvitation[0], (action, label, source) => Admin(action, label, source), clipboard)) {
                reopened.Show(); var create = (Button)Field(reopened, "create"); var refresh = (Button)Field(reopened, "refresh");
                var recover = AllControls(reopened).OfType<Button>().Single(button => button.Text == "Проверить прежнюю выдачу");
                ((TextBox)Field(reopened, "name")).Text = "Edited draft";
                check(!create.Enabled && recover.Enabled, "reopened Friends discovers the persistent issuance gate and offers only its explicit recovery");
                Shot(reopened, "vps-invite-reopened-pending");
                refresh.PerformClick(); Pump(() => !(bool)Field(reopened, "working"));
                check(!create.Enabled && recover.Enabled && frozen.SequenceEqual(File.ReadAllBytes(PendingPath))
                    && Calls().Last() == "list", "a fresh native list cannot clear a pending issuance or change its identity");
                Shot(reopened, "vps-invite-list-retains-pending");
                int shown = 0; reopened.ShowToken = value => { shown++; check(value == response, "recovery hands off only the original validated result"); throw new IOException("fixture handoff failed"); };
                int offset = Calls().Length, copyOffset = copies; recover.PerformClick(); Pump(() => !(bool)Field(reopened, "working"));
                check(shown == 1 && copies - copyOffset == 2 && Calls().Skip(offset).SequenceEqual(new[] { "status", "result" }) && HomeVpnAdminRecovery.Pending().RequestId == id
                    && HomeVpnAdminRecovery.Pending().Name == "Original friend" && HomeVpnAdminRecovery.Pending().Result == response && !create.Enabled && Unchanged(before),
                    "two real read-only SSH queries recover one original token; a failed modal handoff retains it across closing");
                reopened.Close();
            }
            byte[] retained = File.ReadAllBytes(PendingPath);
            using (var restarted = new HomeInvitationsForm(new HomeVpnInvitation[0], (action, label, source) => Admin(action, label, source), clipboard)) {
                restarted.Show(); var recover = AllControls(restarted).OfType<Button>().Single(button => button.Text == "Проверить прежнюю выдачу");
                int shown = 0; restarted.ShowToken = value => { shown++; check(value == response, "reopened recovery presents the exact retained token"); };
                foreach (string mode in new[] { "unavailable", "not-found" }) {
                    File.WriteAllText(At("mode"), mode); int offset = Calls().Length, copyOffset = copies;
                    recover.PerformClick(); Pump(() => !(bool)Field(restarted, "working"));
                    check(shown == 0 && copies - copyOffset == 1 && Calls().Skip(offset).SequenceEqual(new[] { "status" }) && retained.SequenceEqual(File.ReadAllBytes(PendingPath)),
                        "cached token is not shown for unavailable or missing receipts and no invitation is resubmitted: " + mode);
                }
                File.WriteAllText(At("mode"), "complete"); int start = Calls().Length, copied = copies;
                recover.PerformClick(); Pump(() => !(bool)Field(restarted, "working"));
                check(shown == 1 && copies - copied == 2 && Calls().Skip(start).SequenceEqual(new[] { "status", "result" }) && !HomeVpnAdminRecovery.HasPending()
                    && Calls().Count(kind => kind == "invite") == 1 && Unchanged(before), "intentional successful token handoff consumes the original request without another issuance");
                restarted.Close();
            }
        }
        private static IEnumerable<Control> AllControls(Control root)
        { yield return root; foreach (Control child in root.Controls) foreach (Control nested in AllControls(child)) yield return nested; }
        private static void ReissueBoundary(string token)
        {
            Reset("complete"); string source = HomeVpnAccess.Parse(token).InviteId;
            var task = Admin("invite", "Chosen friend", source); Pump(() => task.IsCompleted);
            var pending = HomeVpnAdminRecovery.Pending(); var receipt = Json.Deserialize<HomeVpnAdminRequest>(File.ReadAllText(At("receipt")));
            check(task.GetAwaiter().GetResult() == response && pending.Name == "Chosen friend" && pending.SourceInviteId == source
                && receipt.Name == pending.Name && receipt.SourceInviteId == source && receipt.RequestId == pending.RequestId
                && Calls().SequenceEqual(new[] { "invite" }), "reissue issuance freezes the selected source ID/name in DPAPI and real compound SSH argv");
            HomeVpnAdminRecovery.ConfirmConsumed(response);
        }
        private static void DefaultHandoff(ClipboardService clipboard)
        {
            foreach (string route in new[] { "close", "dispose" }) {
                Reset("complete"); int shown = 0, closing = 0; Exception controllerFailure = null;
                using (var friends = new HomeInvitationsForm(new HomeVpnInvitation[0], (action, label, source) => Admin(action, label, source), clipboard))
                using (var controller = new System.Windows.Forms.Timer { Interval = 20 }) {
                    var watch = Stopwatch.StartNew();
                    friends.Show(); ((TextBox)Field(friends, "name")).Text = "Modal friend";
                    controller.Tick += delegate {
                        var modal = Application.OpenForms.Cast<Form>().FirstOrDefault(form => form.Text == "Личный токен для друга");
                        try {
                            if (watch.ElapsedMilliseconds > 12000) {
                                controllerFailure = new TimeoutException("Default token modal did not appear"); controller.Stop();
                                foreach (var window in Application.OpenForms.Cast<Form>().Where(form => form != friends && form.Modal).ToArray()) window.Close();
                                return;
                            }
                            if (modal == null) return; controller.Stop(); shown++;
                            var field = AllControls(modal).OfType<TextBox>().Single();
                            check(field.ReadOnly && field.UseSystemPasswordChar && field.Text == response
                                && HomeVpnAdminRecovery.Pending().Result == response, "default token modal presents the protected result before consuming its request");
                            if (route == "close") Shot(modal, "vps-invite-default-token-handoff");
                            modal.FormClosing += delegate(object sender, FormClosingEventArgs args) { if (args.CloseReason == CloseReason.UserClosing) closing++; };
                            if (route == "close") AllControls(modal).OfType<Button>().Single(button => button.Text == "Закрыть").PerformClick();
                            else modal.Dispose();
                        } catch (Exception ex) { controllerFailure = ex; controller.Stop(); if (modal != null && !modal.IsDisposed) modal.Close(); }
                    }; controller.Start(); ((Button)Field(friends, "create")).PerformClick(); Pump(() => !(bool)Field(friends, "working"));
                    if (controllerFailure != null) throw new Exception("Default token handoff controller failed", controllerFailure);
                    check(shown == 1 && closing == (route == "close" ? 1 : 0) && copies == 1 && Calls().SequenceEqual(new[] { "invite" })
                        && HomeVpnAdminRecovery.HasPending() == (route == "dispose"), "default modal consumes only an intentional close, while disposal retains the recoverable request: " + route);
                    if (route == "dispose") check(HomeVpnAdminRecovery.Pending().Result == response && !((Button)Field(friends, "create")).Enabled,
                        "default modal disposal leaves the exact protected token and persistent issuance gate");
                    friends.Close();
                }
            }
        }
        private static void FrozenOwner()
        {
            Reset("complete"); var caller = Owner;
            var task = HomeVpnService.AdminAsync(caller, "invite", "Frozen owner", null, delegate { }, (exe, arguments) => {
                copies++; check(exe == "scp.exe" && arguments.Contains(" -P 22 ") && arguments.Contains("root@vpn.example.org"), "copy uses the initial owner snapshot before the caller changes it");
                caller.Host = "other.example.org"; caller.Login = "admin"; caller.Port = 2200; caller.KeyFile = At("nonexistent-key");
                return Task.FromResult(0);
            }, Application.ExecutablePath, 20000);
            Pump(() => task.IsCompleted); var request = HomeVpnAdminRecovery.Pending();
            string[] argv = Json.Deserialize<string[]>(File.ReadAllLines(At("argv")).Single()); int port = Array.IndexOf(argv, "-p");
            check(task.GetAwaiter().GetResult() == response && request.Host == Owner.Host && request.Port == 22 && request.Login == "root"
                && request.Name == "Frozen owner" && port >= 0 && argv[port + 1] == "22" && !argv.Contains("-i") && !argv.Last().Contains("other.example.org")
                && copies == 1 && Calls().SequenceEqual(new[] { "invite" }), "caller owner mutation during copy cannot redirect the protected request or actual SSH command");
            HomeVpnAdminRecovery.ConfirmConsumed(response);
        }
        private static void LateDisposedUi(ClipboardService clipboard)
        {
            foreach (string action in new[] { "invite", "recover-invite" })
            foreach (bool fail in new[] { false, true }) {
                Reset("complete");
                if (action == "recover-invite") { var issued = Admin("invite", "Late friend"); Pump(() => issued.IsCompleted); check(issued.GetAwaiter().GetResult() == response, "late recovery fixture starts with a result retained by real native dispatch"); }
                var completion = new TaskCompletionSource<string>(); int submitted = 0, shown = 0, changes = 0, ticks = 0;
                using (var friends = new HomeInvitationsForm(new HomeVpnInvitation[0], (operation, label, source) => {
                    check(operation == action, "disposed-UI supplement starts only its selected action"); submitted++; return completion.Task;
                }, clipboard))
                using (var heartbeat = new System.Windows.Forms.Timer { Interval = 20 }) {
                    friends.Show(); heartbeat.Tick += delegate { ticks++; }; heartbeat.Start(); friends.ShowToken = value => { shown++; };
                    ((TextBox)Field(friends, "name")).Text = "Late friend";
                    var button = action == "invite" ? (Button)Field(friends, "create") : AllControls(friends).OfType<Button>().Single(control => control.Text == "Проверить прежнюю выдачу");
                    button.PerformClick();
                    check(submitted == 1 && (bool)Field(friends, "working") && !completion.Task.IsCompleted, "actual Friends controls await the callback before disposal");
                    var context = SynchronizationContext.Current as WindowsFormsSynchronizationContext;
                    check(context != null, "pending Friends callback captures the actual Windows UI context before disposal");
                    if (action == "invite") { var issued = Admin("invite", "Late friend"); Pump(() => issued.IsCompleted); check(issued.GetAwaiter().GetResult() == response, "late create fixture persists its result through real native dispatch before disposal"); }
                    byte[] frozen = File.ReadAllBytes(PendingPath); var controls = AllControls(friends).ToArray();
                    var view = Field(friends, "list"); var rows = (ListBox)Field(view, "list");
                    var model = (List<HomeVpnInvitation>)Field(view, "items"); int modelCount = model.Count;
                    friends.Dispose(); int rowCount = rows.Items.Count;
                    var texts = controls.ToDictionary(control => control, control => control.Text);
                    foreach (var control in controls) { control.TextChanged += delegate { changes++; }; control.VisibleChanged += delegate { changes++; }; control.EnabledChanged += delegate { changes++; }; }
                    check(friends.IsDisposed && !completion.Task.IsCompleted, "disposed Friends retains a genuine pending callback for the captured Windows UI context");
                    if (fail) completion.SetException(new IOException("late synthetic refusal")); else completion.SetResult(response);
                    Pump(() => !(bool)Field(friends, "working")); bool drained = false; int initialTicks = ticks;
                    context.Post(delegate { drained = true; }, null); Pump(() => drained && ticks >= initialTicks + 2);
                    check(shown == 0 && changes == 0 && texts.All(pair => pair.Key.Text == pair.Value) && rows.Items.Count == rowCount && model.Count == modelCount
                        && submitted == 1 && HomeVpnAdminRecovery.HasPending() && frozen.SequenceEqual(File.ReadAllBytes(PendingPath))
                        && Calls().Count(kind => kind == "invite") == 1,
                        "late success/failure after actual Friends disposal cannot publish controls/list/token or consume the durable result: " + action + "/" + fail);
                }
            }
        }
        private static void ReissueHandoffMessage(string token, ClipboardService clipboard)
        {
            Reset("complete"); string source = HomeVpnAccess.Parse(token).InviteId;
            const string message = "Передача прежнего токена не завершена. Запрос сохранён для проверки.";
            using (var friends = new HomeInvitationsForm(new[] { new HomeVpnInvitation { Id = source, Name = "Selected friend" } },
                (action, label, id) => action == "revoke" ? Task.FromResult("Access revoked.") : Admin(action, label, id), clipboard)) {
                friends.Show(); friends.Confirm = value => true;
                friends.ShowToken = value => { throw new HomeVpnAdminPendingException(message); };
                ((ListBox)Field(Field(friends, "list"), "list")).SelectedIndex = 0;
                ((Button)Field(friends, "reissue")).PerformClick(); Pump(() => !(bool)Field(friends, "working"));
                check(((Label)Field(friends, "status")).Text == message && HomeVpnAdminRecovery.Pending().Result == response
                    && HomeVpnAdminRecovery.Pending().SourceInviteId == source && Calls().SequenceEqual(new[] { "invite" }),
                    "reissue modal handoff preserves the known Russian pending message and retained source-bound result");
                friends.Close();
            }
        }
        private static Dictionary<string, object> Status(HomeVpnAdminRequest request, string state, bool available)
        {
            return new Dictionary<string, object> { { "Version", 1 }, { "RequestId", request.RequestId }, { "Action", state == "not-found" ? null : "invite" },
                { "State", state }, { "Started", state == "not-found" ? null : "2026-01-01T00:00:00.123456+00:00" },
                { "Finished", state == "running" || state == "not-found" ? null : "2026-01-01T00:00:01+00:00" }, { "ResultAvailable", available } };
        }
        private static bool Refused(Action action)
        { try { action(); return false; } catch (HomeVpnAdminPendingException) { return true; } }
        private static string ChangeToken(string token, string field, object value)
        {
            string encoded = token.Substring(7).Replace('-', '+').Replace('_', '/'); encoded = encoded.PadRight((encoded.Length + 3) / 4 * 4, '=');
            var fields = Json.Deserialize<Dictionary<string, object>>(Encoding.UTF8.GetString(Convert.FromBase64String(encoded))); fields[field] = value;
            if (field == "InviteId") fields["User"] = "pgv" + (string)value;
            return "PROGO1." + Convert.ToBase64String(Encoding.UTF8.GetBytes(Json.Serialize(fields))).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }
        private static void StatusAndStorage(string token)
        {
            Reset("complete"); var request = HomeVpnAdminRecovery.Register(Owner, "Stored friend", null); byte[] frozen = File.ReadAllBytes(PendingPath);
            HomeVpnAdminRecovery.RequireCompleted(Json.Serialize(Status(request, "succeeded", true)), request);
            var states = new List<Dictionary<string, object>> { Status(request, "running", false), Status(request, "not-found", false),
                Status(request, "unconfirmed", false), Status(request, "succeeded", false) };
            var busy = Status(request, "running", false); busy["Action"] = busy["Started"] = null; states.Add(busy);
            var stale = Status(request, "unconfirmed", false); stale["Finished"] = null; states.Add(stale);
            foreach (var change in new[] { new KeyValuePair<string, object>("RequestId", new string('e', 32)), new KeyValuePair<string, object>("Version", 2),
                new KeyValuePair<string, object>("Action", "setup"), new KeyValuePair<string, object>("Finished", "invalid"),
                new KeyValuePair<string, object>("Started", null), new KeyValuePair<string, object>("ResultAvailable", "true"), new KeyValuePair<string, object>("Extra", token) }) {
                var value = Status(request, "succeeded", true); value[change.Key] = change.Value; states.Add(value);
            }
            foreach (var state in states) check(Refused(() => HomeVpnAdminRecovery.RequireCompleted(Json.Serialize(state), request))
                && frozen.SequenceEqual(File.ReadAllBytes(PendingPath)), "unknown, running, revoked or malformed receipt cannot consume or alter the protected issuance");
            foreach (string field in new[] { "Version", "Host", "Port", "Login", "ServerId", "Action", "Name", "SourceInviteId" }) {
                var changed = Json.Deserialize<HomeVpnAdminRequest>(Json.Serialize(request));
                typeof(HomeVpnAdminRequest).GetProperty(field).SetValue(changed, field == "Version" ? (object)2 : field == "Port" ? (object)2200 : field == "ServerId" ? new string('d', 32)
                    : field == "SourceInviteId" ? new string('d', 24) : field == "Host" ? "other.example.org" : field == "Action" ? "setup" : "changed", null);
                string changedToken = field == "Host" ? ChangeToken(response, "Host", changed.Host) : field == "Port" ? ChangeToken(response, "Port", changed.Port) : response;
                check(Refused(() => HomeVpnAdminRecovery.RetainResult(Owner, changed, changedToken)) && frozen.SequenceEqual(File.ReadAllBytes(PendingPath)),
                    "a same-ID mutable request cannot change the persisted owner, server or invitation binding: " + field);
            }
            using (var held = File.Open(PendingPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                check(Refused(() => HomeVpnAdminRecovery.RetainResult(Owner, request, response)) && frozen.SequenceEqual(File.ReadAllBytes(PendingPath)),
                    "failed atomic result storage keeps the original frozen request and blocks a successful handoff claim");
            HomeVpnAdminRecovery.RetainResult(Owner, request, response); byte[] result = File.ReadAllBytes(PendingPath);
            check(Refused(() => HomeVpnAdminRecovery.ConfirmConsumed(token)) && result.SequenceEqual(File.ReadAllBytes(PendingPath)), "consumption requires the exact retained result token");
            using (var held = File.Open(PendingPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                check(Refused(() => HomeVpnAdminRecovery.ConfirmConsumed(response)) && result.SequenceEqual(File.ReadAllBytes(PendingPath)), "locked journal keeps the result when consumption cannot persist");
            HomeVpnAdminRecovery.ConfirmConsumed(response);
            foreach (var change in new[] { new KeyValuePair<string, object>("Version", 2), new KeyValuePair<string, object>("Port", "22"),
                new KeyValuePair<string, object>("SourceInviteId", 123), new KeyValuePair<string, object>("ServerId", null),
                new KeyValuePair<string, object>("Extra", true), new KeyValuePair<string, object>("MissingResult", null) }) {
                var fields = Json.Deserialize<Dictionary<string, object>>(Json.Serialize(request));
                if (change.Key == "MissingResult") fields.Remove("Result"); else fields[change.Key] = change.Value;
                var bytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(Json.Serialize(fields)), null, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(PendingPath, bytes);
                check(Refused(() => HomeVpnAdminRecovery.Pending()) && Failure(Admin("recover-invite")) is HomeVpnAdminPendingException
                    && bytes.SequenceEqual(File.ReadAllBytes(PendingPath)) && copies == 0 && Calls().Length == 0,
                    "malformed protected request schema blocks SSH without discarding its evidence: " + change.Key);
            }
            var oversizedFields = Json.Deserialize<Dictionary<string, object>>(Json.Serialize(request)); oversizedFields["Name"] = new string('x', 100001);
            var oversized = ProtectedData.Protect(Encoding.UTF8.GetBytes(new JavaScriptSerializer { MaxJsonLength = 200000 }.Serialize(oversizedFields)), null, DataProtectionScope.CurrentUser);
            check(oversized.Length > 100000, "oversized storage fixture is a real DPAPI envelope beyond the journal limit");
            foreach (var bytes in new[] { new byte[] { 1, 2, 3 }, oversized }) {
                File.WriteAllBytes(PendingPath, bytes);
                check(Refused(() => HomeVpnAdminRecovery.Pending()) && Refused(() => HomeVpnAdminRecovery.Register(Owner, "New friend", null))
                    && bytes.SequenceEqual(File.ReadAllBytes(PendingPath)), "undecryptable or oversized pending storage fails closed without discarding or replacing the journal");
            }
            File.Delete(PendingPath);
            Directory.CreateDirectory(PendingPath);
            try { check(Refused(() => HomeVpnAdminRecovery.Pending()) && Refused(() => HomeVpnAdminRecovery.HasPending()), "a folder cannot replace the protected issuance journal or clear its mutation gate"); }
            finally { Directory.Delete(PendingPath); }
        }
    }
}

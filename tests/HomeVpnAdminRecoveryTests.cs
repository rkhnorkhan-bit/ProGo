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
            if (child) { File.WriteAllText(Path.Combine(path, "child"), Process.GetCurrentProcess().Id.ToString()); Thread.Sleep(60000); return true; }
            string command = args.Last();
            string kind = command.Contains("home_vpn_setup.py invite ") ? "invite"
                : command.Contains("home_vpn_setup.py operation-status ") ? "status"
                : command.Contains("home_vpn_setup.py operation-result ") ? "result"
                : command.Contains("home_vpn_setup.py list ") ? "list" : null;
            if (kind == null || !args.Contains("-T") || !args.Contains("root@vpn.example.org")) throw new Exception("Unexpected production SSH dispatch in admin fixture");
            File.AppendAllText(Path.Combine(path, "calls"), kind + "\n");
            File.AppendAllText(Path.Combine(path, "argv"), Json.Serialize(args) + "\n");
            File.WriteAllText(Path.Combine(path, "pid"), Process.GetCurrentProcess().Id.ToString());
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
        private static void Pump(Func<bool> ready)
        {
            var watch = Stopwatch.StartNew(); while (!ready()) { Application.DoEvents(); Thread.Sleep(10); if (watch.ElapsedMilliseconds > 15000) throw new Exception("Admin recovery fixture timeout"); } Application.DoEvents();
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

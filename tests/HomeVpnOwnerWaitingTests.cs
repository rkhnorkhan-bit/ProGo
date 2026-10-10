using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace ProGo
{
    // Real production dispatch and owned Windows process trees; only SCP and
    // the SSH executable are replaced. No live VPS or owner credentials are used.
    internal static class HomeVpnOwnerWaitingTests
    {
        private const string Variable = "PROGO_OWNER_WAIT_FIXTURE";
        private const string OldId = "aaaaaaaaaaaaaaaaaaaaaaaa";
        private static readonly JavaScriptSerializer Json = new JavaScriptSerializer();
        private static Action<bool, string> check;
        private static string folder, replacement, sharePath;
        private static string[] inputPaths;
        private static int copies;
        private static HomeVpnOwner Owner { get { return new HomeVpnOwner { Host = "vpn.example.org", Login = "root", Port = 22, KeyFile = "" }; } }
        private static string At(string name) { return Path.Combine(folder, name); }
        private static string PendingPath { get { return Path.Combine(HomeVpnPrivateFiles.Root, "admin-request.dat"); } }

        internal static bool Fixture(string[] args)
        {
            if (args.Length == 1 && args[0] == "owner-wait-unrelated") { Thread.Sleep(300000); return true; }
            bool child = args.Length == 2 && args[0] == "owner-wait-child";
            bool copy = args.Length == 2 && args[0] == "owner-wait-copy";
            string path = child || copy ? args[1] : Environment.GetEnvironmentVariable(Variable);
            if (!child && !copy && (args.Length == 0 || args[0] != "-o" || String.IsNullOrEmpty(path))) return false;
            if (child) { File.WriteAllText(Path.Combine(path, "child"), Process.GetCurrentProcess().Id.ToString()); Thread.Sleep(60000); return true; }
            string command = copy ? "" : args.Last();
            string kind = copy ? "copy" : new[] { "revoke", "repair", "share", "invite" }.FirstOrDefault(action => command.Contains("home_vpn_setup.py " + action + " "));
            if (kind == null || (!copy && (!args.Contains("-T") || !args.Contains("root@vpn.example.org") || !args.Contains("22")
                || !command.Contains("trap 'rm -rf -- /tmp/") || !command.Contains(" --output /tmp/")))) throw new Exception("Unexpected production owner SSH dispatch");
            File.AppendAllText(Path.Combine(path, "calls"), kind + "\n");
            File.AppendAllText(Path.Combine(path, "argv"), Json.Serialize(args) + "\n");
            File.WriteAllText(Path.Combine(path, "pid"), Process.GetCurrentProcess().Id.ToString());
            string token = File.ReadAllText(Path.Combine(path, "response"));
            var access = HomeVpnAccess.Parse(token);
            if (!copy && (command.Contains(token) || command.Contains(access.Password) || command.Contains(access.PrivateKey)
                || args.Any(value => value == "StrictHostKeyChecking=no"))) throw new Exception("Owner dispatch exposed a secret or disabled host-key checking");
            if (kind == "invite") {
                var request = HomeVpnAdminRecovery.Pending();
                var ids = Regex.Matches(command, @"--request-id ([0-9a-f]{32})").Cast<Match>().Select(match => match.Groups[1].Value).ToArray();
                if (request == null || request.SourceInviteId != OldId || request.Name != "Друг" || ids.Length != 3 || ids.Any(id => id != request.RequestId)
                    || !command.Contains(" --id " + OldId) || !command.Contains(" --name 'Друг'")
                    || command.IndexOf("home_vpn_setup.py invite ", StringComparison.Ordinal) >= command.IndexOf("home_vpn_setup.py operation-status ", StringComparison.Ordinal)
                    || command.IndexOf("home_vpn_setup.py operation-status ", StringComparison.Ordinal) >= command.IndexOf("home_vpn_setup.py operation-result ", StringComparison.Ordinal))
                    throw new Exception("Reissue lost the acknowledged source ID or frozen production receipt command");
            } else if (!copy && (command.Contains("--request-id") || command.Contains("operation-status") || command.Contains("operation-result")))
                throw new Exception("An ordinary owner action fabricated a recovery request");
            if (kind == "revoke" && !command.Contains(" --id " + OldId)) throw new Exception("Revoke did not retain the selected ID");
            if (kind == "share" && !command.Contains(" --domain 'qr.example.org'")) throw new Exception("Share did not retain its normalized domain");
            string mode = File.ReadAllText(Path.Combine(path, "mode"));
            if (mode == "hold" && kind != "invite") {
                using (var descendant = Process.Start(new ProcessStartInfo(Application.ExecutablePath, "owner-wait-child " + HomeVpnService.Argument(path)) { UseShellExecute = false, CreateNoWindow = true })) {
                    var watch = Stopwatch.StartNew();
                    while (!File.Exists(Path.Combine(path, "child"))) { Thread.Sleep(10); if (watch.ElapsedMilliseconds > 5000) throw new Exception("Owner fixture child did not start"); }
                    Thread.Sleep(60000);
                }
                return true;
            }
            if (mode == "failure") { Console.Error.WriteLine("synthetic-private-owner-error"); Environment.Exit(7); return true; }
            Console.OutputEncoding = new UTF8Encoding(false);
            if (mode == "oversized") { Console.Write(new string('x', 1000000)); return true; }
            if (kind == "invite") {
                var request = HomeVpnAdminRecovery.Pending();
                Console.WriteLine(Json.Serialize(new Dictionary<string, object> { { "Version", 1 }, { "RequestId", request.RequestId }, { "Action", "invite" },
                    { "State", "succeeded" }, { "Started", "2026-01-01T00:00:00+00:00" }, { "Finished", "2026-01-01T00:00:01+00:00" }, { "ResultAvailable", true } }));
                Console.WriteLine(token);
            } else Console.Write(kind == "revoke" ? "Access revoked." : kind == "share" ? "https://qr.example.org" : "VPN network rules refreshed. Reconnect the phone and test internet access.");
            return true;
        }

        internal static void Run(Action<bool, string> assert, string token, ClipboardService clipboard, string work)
        {
            check = assert; folder = Path.Combine(work, "owner-wait"); HomeVpnPrivateFiles.SecureDirectory(folder);
            replacement = Replacement(token); sharePath = Path.Combine(HomeVpnPrivateFiles.Root, "share-" + HomeVpnAccess.Parse(token).ServerId + ".dat");
            var saved = new Dictionary<string, byte[]>();
            foreach (string name in new[] { "access", "owner", "home-address", "setup-request", "admin-request" }) Save(saved, Path.Combine(HomeVpnPrivateFiles.Root, name + ".dat"));
            Save(saved, sharePath);
            string helpers = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scripts", "home-vpn", "server"); Directory.CreateDirectory(helpers);
            foreach (string file in new[] { "home_vpn_setup.py", "ikev2_relay.py", "install-ikev2-relay.sh", "profile_share_setup.py", "profile_share.py", "qrcodegen.py", "QR_LICENSE.txt" }) {
                string target = Path.Combine(helpers, file); Save(saved, target);
                File.Copy(Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Application.ExecutablePath)), "server", file), target, true);
            }
            inputPaths = saved.Keys.ToArray();
            string previous = Environment.GetEnvironmentVariable(Variable); Exception threadFailure = null;
            ThreadExceptionEventHandler handler = (sender, args) => { threadFailure = args.Exception; }; Application.ThreadException += handler;
            using (var unrelated = Process.Start(new ProcessStartInfo(Application.ExecutablePath, "owner-wait-unrelated") { UseShellExecute = false, CreateNoWindow = true })) {
                try {
                    File.Delete(Path.Combine(HomeVpnPrivateFiles.Root, "setup-request.dat")); File.Delete(PendingPath);
                    HomeVpnPrivateFiles.Save("access", token); HomeVpnPrivateFiles.Save("owner", Json.Serialize(Owner));
                    Environment.SetEnvironmentVariable(Variable, folder);
                    foreach (string action in new[] { "revoke", "repair", "share" }) Entry(token, clipboard, action, "button", false, false);
                    foreach (string route in new[] { "escape", "close", "dispose", "deadline" }) Entry(token, clipboard, "repair", route, false, false);
                    Entry(token, clipboard, "reissue", "button", false, false);
                    Entry(token, clipboard, "reissue", "complete", false, true);
                    foreach (string action in new[] { "revoke", "reissue" }) Entry(token, clipboard, action, "button", true, false);
                    Outputs(saved); AcceptedCancellation();
                    check(threadFailure == null, "ordinary owner waiting and UI completion cause no thread exception");
                    check(!unrelated.HasExited, "owner cancellation and deadlines preserve an unrelated process");
                    check(Directory.GetDirectories(HomeVpnPrivateFiles.Root, "admin-*").Length == 0, "owner waits settle their private dispatch work");
                } finally {
                    Application.ThreadException -= handler; Environment.SetEnvironmentVariable(Variable, previous); KillFixtures();
                    if (!unrelated.HasExited) { unrelated.Kill(); unrelated.WaitForExit(2000); }
                    foreach (var item in saved) { if (item.Value == null) File.Delete(item.Key); else File.WriteAllBytes(item.Key, item.Value); }
                }
            }
        }
        private static string Replacement(string token)
        {
            string encoded = token.Substring(7).Replace('-', '+').Replace('_', '/'); encoded = encoded.PadRight((encoded.Length + 3) / 4 * 4, '=');
            var value = Json.Deserialize<Dictionary<string, object>>(Encoding.UTF8.GetString(Convert.FromBase64String(encoded)));
            value["InviteId"] = new string('c', 24); value["User"] = "pgv" + new string('c', 24);
            return "PROGO1." + Convert.ToBase64String(Encoding.UTF8.GetBytes(Json.Serialize(value))).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }
        private static void Save(Dictionary<string, byte[]> saved, string path) { saved[path] = File.Exists(path) ? File.ReadAllBytes(path) : null; }
        private static Dictionary<string, byte[]> Snapshot(IEnumerable<string> paths)
        { var saved = new Dictionary<string, byte[]>(); foreach (string path in paths) Save(saved, path); return saved; }
        private static bool Unchanged(Dictionary<string, byte[]> saved)
        { return saved.All(item => item.Value == null ? !File.Exists(item.Key) : File.Exists(item.Key) && item.Value.SequenceEqual(File.ReadAllBytes(item.Key))); }
        private static object Field(object target, string name) { return target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target); }
        private static IEnumerable<Control> Controls(Control target)
        { foreach (Control child in target.Controls) { yield return child; foreach (var nested in Controls(child)) yield return nested; } }
        private static bool Gone(int id) { try { using (var process = Process.GetProcessById(id)) return process.HasExited; } catch (ArgumentException) { return true; } }
        private static int Id(string name) { return Int32.Parse(File.ReadAllText(At(name))); }
        private static string[] Calls() { return File.Exists(At("calls")) ? File.ReadAllLines(At("calls")) : new string[0]; }
        private static void KillFixtures()
        {
            foreach (string name in new[] { "pid", "child" }) {
                if (!File.Exists(At(name))) continue;
                try { using (var process = Process.GetProcessById(Id(name))) { if (!process.HasExited) { process.Kill(); process.WaitForExit(2000); } } } catch (ArgumentException) { }
            }
        }
        private static void Reset(string mode)
        { KillFixtures(); foreach (string file in Directory.GetFiles(folder)) File.Delete(file); File.Delete(PendingPath); copies = 0; File.WriteAllText(At("mode"), mode); File.WriteAllText(At("response"), replacement); }
        private static Task<string> Admin(HomeVpnOwner owner, string action, string label, string id, Action<string> progress, int timeout, bool realCopy)
        {
            return HomeVpnService.AdminAsync(owner, action, label, id, progress, (executable, arguments) => {
                copies++; check(executable == "scp.exe" && arguments.Contains("ConnectTimeout=15") && arguments.Contains(" -P 22 "), "owner dispatch keeps production OpenSSH copy options");
                return realCopy ? HomeVpnPreparationForm.CopyAsync(Form.ActiveForm, Application.ExecutablePath, "owner-wait-copy " + HomeVpnService.Argument(folder)) : Task.FromResult(0);
            }, Application.ExecutablePath, timeout);
        }
        private static void Shot(Form form, string name)
        { using (var image = new Bitmap(form.Width, form.Height)) { form.DrawToBitmap(image, new Rectangle(Point.Empty, image.Size)); image.Save(Path.Combine(Path.GetDirectoryName(folder), name + ".png")); } }
        private sealed class Observation { internal int Ticks, Root, Child; internal bool Waiting; internal string Status; }
        // Own the outer loop so closing a nested waiting modal cannot end a
        // modeless Pump and lose the button handler's captured UI continuation.
        private static Observation Loop(Form main, Action start, Func<bool> done, string route, string shot, Func<string> status)
        {
            var result = new Observation(); bool started = false; Exception failure = null; var watch = Stopwatch.StartNew();
            using (var timer = new System.Windows.Forms.Timer { Interval = 20 }) {
                main.Shown += delegate { main.BeginInvoke(new Action(delegate { started = true; try { start(); } catch (Exception ex) { failure = ex; } })); };
                timer.Tick += delegate {
                    result.Ticks++;
                    try {
                        if (!started) return;
                        var wait = Application.OpenForms.OfType<HomeVpnPreparationForm>().FirstOrDefault();
                        if (!result.Waiting && wait != null && File.Exists(At("child")) && result.Ticks >= 3) {
                            result.Waiting = true; result.Root = Id("pid"); result.Child = Id("child");
                            var cancel = (Button)Field(wait, "cancel"); var text = (Label)Field(wait, "status");
                            check(cancel.Enabled && wait.CancelButton == cancel && wait.AcceptButton == null && text.AccessibilityObject.Description == text.Text,
                                "owner waiting exposes an accessible responsive cancellation control");
                            if (!Calls().Contains("copy")) check(text.Text.Contains("5 минут") && !text.Text.Contains("запрос сохранён") && !text.Text.Contains("Запрос сохранён"),
                                "ordinary owner progress gives a bounded wait without a fabricated saved request");
                            if (shot != null) { Shot(wait, shot + "-waiting"); wait.ClientSize = new Size(440, 270); Application.DoEvents();
                                check(wait.RectangleToClient(cancel.RectangleToScreen(cancel.ClientRectangle)).Bottom <= wait.ClientSize.Height, "minimum owner wait retains its actual cancellation button"); Shot(wait, shot + "-minimum"); }
                            if (route == "button") cancel.PerformClick();
                            else if (route == "escape") typeof(Form).GetMethod("ProcessDialogKey", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(wait, new object[] { Keys.Escape });
                            else if (route == "close") wait.Close(); else if (route == "dispose") wait.Dispose();
                        }
                        if (failure == null && watch.ElapsedMilliseconds > 20000) failure = new TimeoutException("Owner UI fixture did not settle");
                        if (failure != null) {
                            timer.Stop(); if (wait != null && !wait.IsDisposed) wait.Dispose(); KillFixtures(); main.Dispose(); Application.ExitThread(); return;
                        }
                        if (done()) {
                            result.Status = status == null ? "" : status(); if (shot != null) Shot(main, shot + "-cancelled"); timer.Stop(); main.Close();
                        }
                    } catch (Exception ex) { failure = ex; }
                };
                timer.Start(); Application.Run(main);
            }
            if (failure != null) throw new Exception("Owner native UI controller failed", failure);
            return result;
        }
        private static void Entry(string token, ClipboardService clipboard, string action, string route, bool realCopy, bool success)
        {
            Reset(success ? "complete" : "hold"); int verified = 0, shown = 0; HomeVpnInvitation first = null, second = null;
            using (var relay = new Ikev2RelayService())
            using (var service = new HomeVpnService(relay)) {
                service.UseToken(token, Owner); var before = Snapshot(inputPaths);
                Func<HomeVpnOwner, string, string, string, Action<string>, Task<string>> admin = (owner, kind, label, id, progress) => Admin(owner, kind, label, id, progress, route == "deadline" ? 5000 : 20000, realCopy);
                Form main; Button button; Func<bool> done; Func<string> status;
                if (action == "repair") {
                    var wizard = new HomeVpnWizardForm(service, clipboard); wizard.Admin = admin;
                    typeof(HomeVpnWizardForm).GetMethod("ShowStep", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(wizard, new object[] { 4 });
                    main = wizard; button = Controls(wizard).OfType<Button>().Single(control => control.Text == "Исправить выход VPN в интернет");
                    done = () => !(bool)Field(wizard, "busy"); status = () => {
                        check(wizard.Visible && (int)Field(wizard, "step") == 4 && ((Control)Field(wizard, "body")).Enabled,
                            "cancelled repair restores the existing wizard step without a queued close or completion claim");
                        return ((Label)Field(wizard, "status")).Text;
                    };
                } else if (action == "share") {
                    main = HomeProfileShare.CreateConfigureForm(service, admin, (origin, access) => { verified++; return Task.FromResult(0); });
                    Controls(main).OfType<TextBox>().Single().Text = "QR.EXAMPLE.ORG";
                    button = Controls(main).OfType<Button>().Single(control => control.Text == "Настроить HTTPS на VPS");
                    var address = Controls(main).OfType<TextBox>().Single(); done = () => address.Enabled;
                    status = () => {
                        check(main.Visible && main.DialogResult == DialogResult.None && Controls(main).OfType<Button>().All(control => control.Enabled),
                            "cancelled share restores independent actions without confirming or saving its HTTPS address");
                        return Controls(main).OfType<Label>().Single(control => control.AccessibleName == "Результат настройки HTTPS-выдачи").Text;
                    };
                } else {
                    first = new HomeVpnInvitation { Id = OldId, Name = "Друг" }; second = new HomeVpnInvitation { Id = new string('d', 24), Name = "Друг" };
                    var friends = new HomeInvitationsForm(new[] { first, second }, (kind, label, id) => admin(Owner, kind, label, id, delegate { }), clipboard);
                    friends.Confirm = text => true; friends.ShowToken = value => { shown++; check(value == replacement, "acknowledged phase-one reissue presents only its new verified token"); };
                    var list = (HomeInvitationList)Field(friends, "list"); ((ListBox)Field(list, "list")).SelectedIndex = 0;
                    ((TextBox)Field(friends, "name")).Text = "Другой черновик";
                    main = friends; button = (Button)Field(friends, action == "reissue" ? "reissue" : "revoke");
                    done = () => !(bool)Field(friends, "working"); status = () => {
                        if (!success) check(friends.Visible && list.Selected == first && ((TextBox)Field(friends, "name")).Text == "Другой черновик"
                            && ((Button)Field(friends, "create")).Enabled == realCopy && ((Button)Field(friends, "revoke")).Enabled == realCopy
                            && ((Button)Field(friends, "reissue")).Enabled == realCopy,
                            "friends retain selection and draft, and distinguish pre-SSH cancellation from uncertain remote mutation: " + action);
                        return ((Label)Field(friends, "status")).Text;
                    };
                }
                using (main) {
                    var result = Loop(main, () => button.PerformClick(), done, route,
                        route == "button" && !realCopy && action != "reissue" ? "vps-owner-" + action : null, status);
                    if (success) check(shown == 1 && first.Revoked && !second.Revoked && Calls().SequenceEqual(new[] { "revoke", "invite" })
                        && copies == 2 && !File.Exists(PendingPath) && Unchanged(before), "real phase-one ACK permits exactly one source-bound replacement and consumes only its completed handoff");
                    else {
                        check(result.Waiting && result.Ticks >= 3 && Gone(result.Root) && Gone(result.Child) && copies == 1
                            && Calls().SequenceEqual(new[] { realCopy ? "copy" : action == "reissue" ? "revoke" : action }) && shown == 0 && verified == 0 && Unchanged(before),
                            "native owner cancellation settles its tree, preserves private files and makes one attempt: " + action + "/" + route + "/copy=" + realCopy);
                        check(Directory.GetDirectories(HomeVpnPrivateFiles.Root, "admin-*").Length == 0 && !File.Exists(PendingPath), "ordinary cancellation creates no issuance journal and cleans local work");
                        if (realCopy) check(result.Status.Contains("не запускались"), "pre-dispatch copy cancellation reports that server commands did not start: " + action);
                        else check(result.Status.Contains("подтвержд") && !result.Status.Contains("запрос сохранён") && !result.Status.Contains("Запрос сохранён")
                            && !result.Status.Contains("Правила выхода VPN обновлены"), "owner cancellation reports uncertainty without fabricated recovery or success: " + action + "/" + route);
                        if (action == "repair" && !realCopy) check(result.Status.Contains(route == "deadline" ? "Время ожидания SSH истекло" : "Ожидание SSH прервано"),
                            "ordinary waiting distinguishes the deadline from requested cancellation: " + route);
                        if (first != null) check(!first.Revoked && !second.Revoked, "unconfirmed phase-one revoke never publishes a revoked row or mutates its duplicate");
                    }
                    check(!relay.IsRunning && !Directory.GetDirectories(HomeVpnPrivateFiles.Root, "session-*").Any(), "owner management does not start a tunnel");
                }
            }
        }
        private static void Outputs(Dictionary<string, byte[]> paths)
        {
            foreach (string mode in new[] { "complete", "failure", "oversized" })
            foreach (string action in new[] { "revoke", "repair", "share" }) {
                Reset(mode); var before = Snapshot(paths.Keys); Task<string> task = null;
                using (var host = new Form()) {
                    Loop(host, () => task = Admin(Owner, action, null, action == "revoke" ? OldId : action == "share" ? "QR.EXAMPLE.ORG" : null, delegate { }, 20000, false), () => task != null && task.IsCompleted, "complete", null, null);
                    Exception failure = null; string text = null; try { text = task.GetAwaiter().GetResult(); } catch (Exception ex) { failure = ex; }
                    check(copies == 1 && Calls().SequenceEqual(new[] { action }) && Unchanged(before) && Directory.GetDirectories(HomeVpnPrivateFiles.Root, "admin-*").Length == 0,
                        "production ordinary output makes one attempt and preserves private inputs/helpers: " + action + "/" + mode);
                    if (mode == "complete") check(failure == null && text == (action == "revoke" ? "Access revoked." : action == "share" ? "https://qr.example.org" : "VPN network rules refreshed. Reconnect the phone and test internet access."),
                        "ordinary production SSH returns its actual successful output: " + action);
                    else check(failure is HomeVpnOwnerUnconfirmedException && failure.Message.Contains("подтвержд") && !failure.Message.Contains("synthetic-private")
                        && !failure.Message.Contains("запрос сохранён"), "ordinary nonzero/oversized output fails honestly without leaking details or claiming a journal: " + action + "/" + mode);
                }
            }
        }
        private static void AcceptedCancellation()
        {
            var ready = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (var wait = new HomeVpnPreparationForm(token => ready.Task, HomeVpnWaitPurpose.OwnerCommand)) {
                bool started = false; Exception failure = null; var watch = Stopwatch.StartNew();
                using (var timer = new System.Windows.Forms.Timer { Interval = 20 }) {
                    timer.Tick += delegate {
                        if (watch.ElapsedMilliseconds > 5000) { failure = new TimeoutException("Queued owner completion did not settle"); timer.Stop(); wait.Dispose(); return; }
                        if (started) return; started = true; ((Button)Field(wait, "cancel")).PerformClick(); ready.SetResult(null);
                    };
                    timer.Start(); Application.Run(wait);
                }
                if (failure != null || !wait.Completion.IsCompleted) throw new Exception("Queued owner cancellation fixture did not settle", failure);
                try { wait.Completion.GetAwaiter().GetResult(); } catch (Exception ex) { failure = ex; }
                check(started && failure is HomeVpnOwnerUnconfirmedException, "accepted owner cancellation cannot publish a queued successful command completion");
            }
        }
    }
}

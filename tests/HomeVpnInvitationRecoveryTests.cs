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
    // Real private DPAPI storage and native UI; only SSH/SCP are replaced.
    internal static class HomeVpnInvitationRecoveryTests
    {
        private static Action<bool, string> check;
        private static readonly JavaScriptSerializer Json = new JavaScriptSerializer();
        private static HomeVpnOwner Owner { get { return new HomeVpnOwner { Host = "vpn.example.org", Port = 22, Login = "root", KeyFile = "" }; } }
        private static string PendingPath { get { return Path.Combine(HomeVpnPrivateFiles.Root, HomeVpnInvitationRecovery.StorageName + ".dat"); } }
        private static HomeVpnInvitationRequest Pending() { return HomeVpnInvitationRecovery.Load(Owner); }
        private static object Field(object value, string name) { return value.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(value); }
        private static IEnumerable<Control> Controls(Control root) { foreach (Control child in root.Controls) { yield return child; foreach (var item in Controls(child)) yield return item; } }
        private static Task Write(string path, string value) { File.WriteAllText(path, value, new UTF8Encoding(false)); return Task.FromResult(0); }
        private static Task<string> Admin(string action, Func<string, string, string, Task> transport, Func<string, string, Task> copy = null, HomeVpnOwner owner = null, string label = "Друг")
        { return HomeVpnService.AdminAsync(owner ?? Owner, action, label, null, delegate { }, copy ?? ((exe, args) => Task.FromResult(0)), transport); }
        private static void Pump(Func<bool> ready) {
            var watch = Stopwatch.StartNew(); while (!ready()) { Application.DoEvents(); Thread.Sleep(10); if (watch.ElapsedMilliseconds > 12000) throw new Exception("Invitation recovery fixture timeout"); } Application.DoEvents();
        }
        private static Exception Failure(Task task) {
            Pump(() => task.IsCompleted); try { task.GetAwaiter().GetResult(); } catch (Exception ex) { return ex; } throw new Exception("Expected invitation refusal");
        }
        private static string Result(Task<string> task) { Pump(() => task.IsCompleted); return task.GetAwaiter().GetResult(); }
        private static Dictionary<string, object> Status(HomeVpnInvitationRequest request, string state = "succeeded", bool available = true) {
            return new Dictionary<string, object> { { "Version", 1 }, { "RequestId", request.RequestId }, { "Action", state == "not-found" ? null : "invite" },
                { "State", state }, { "Started", state == "not-found" ? null : "2026-01-01T00:00:00.123456+00:00" },
                { "Finished", state == "running" || state == "not-found" ? null : "2026-01-01T00:00:01+00:00" }, { "ResultAvailable", available } };
        }
        internal static void Run(Action<bool, string> assert, string token, ClipboardService clipboard, string work)
        {
            check = assert;
            byte[] saved = File.Exists(PendingPath) ? File.ReadAllBytes(PendingPath) : null;
            var accessBefore = PrivateSnapshot(); File.Delete(PendingPath);
            try {
                LostResponse(token); StatusGuards(token); DispatchGuards(token); NativeFriends(token, clipboard, work);
                NativeWizardEntry(token, clipboard); NativeToken(token, clipboard, work); NativeRefusals(clipboard);
                check(!Directory.GetDirectories(HomeVpnPrivateFiles.Root, "admin-*").Any()
                    && !Directory.GetFiles(HomeVpnPrivateFiles.Root, "invitation-request.dat.*.new").Any(), "invitation recovery settles private work and unique atomic-save files");
                var after = PrivateSnapshot(); check(accessBefore.Keys.OrderBy(p => p).SequenceEqual(after.Keys.OrderBy(p => p))
                    && accessBefore.All(p => after[p.Key].SequenceEqual(p.Value)), "friend issuance and consumption never replace the owner's saved access, endpoint or setup request");
            } finally { if (saved == null) File.Delete(PendingPath); else File.WriteAllBytes(PendingPath, saved); }
        }
        private static Dictionary<string, byte[]> PrivateSnapshot() {
            var result = new Dictionary<string, byte[]>();
            foreach (string name in new[] { "access", "owner", "home-address", HomeVpnSetupRecovery.StorageName }) {
                string path = Path.Combine(HomeVpnPrivateFiles.Root, name + ".dat"); if (File.Exists(path)) result[path] = File.ReadAllBytes(path);
            }
            return result;
        }
        private static void LostResponse(string token)
        {
            int copies = 0, issued = 0; string id = null;
            var error = Failure(Admin("create-invite", (exe, args, output) => {
                var request = Pending(); id = request.RequestId; issued++;
                check(request.Result == null && request.Name == "Друг" && args.Contains("invite --host") && args.Contains("--request-id " + id),
                    "separate invitation saves original parameters and request ID before its one SSH mutation");
                check(!args.Contains(token) && !args.Contains(HomeVpnAccess.Parse(token).Password), "invitation credentials are never command arguments");
                throw new IOException("fixture-secret-response");
            }, (exe, args) => { copies++; check(Pending() == null, "fresh invitation copy precedes request registration"); return Task.FromResult(0); }));
            check(error is HomeVpnInvitationPendingException && !error.Message.Contains("fixture-secret") && Pending().RequestId == id && Pending().Result == null,
                "lost invitation response retains original ID with fixed recovery guidance");
            string protectedText = Encoding.UTF8.GetString(File.ReadAllBytes(PendingPath));
            check(!protectedText.Contains(id) && !protectedText.Contains(Owner.Host) && !protectedText.Contains("PROGO1.")
                && Directory.GetAccessControl(HomeVpnPrivateFiles.Root).AreAccessRulesProtected, "invitation journal uses CurrentUser DPAPI and a private protected directory");
            var calls = new List<string>();
            check(Result(Admin("recover-invite", (exe, args, output) => {
                calls.Add(args); check(args.Contains("--request-id " + id) && !args.Contains("invite --host"), "invitation recovery submits only original-ID status/result queries");
                return Write(output, args.Contains("operation-status") ? Json.Serialize(Status(Pending())) : token);
            })) == token && copies == 1 && issued == 1 && calls.Count == 2 && calls[0].Contains("operation-status") && calls[1].Contains("operation-result"),
                "lost invitation recovers its exact original token without another issuance");
            check(Pending().Result == token && !Encoding.UTF8.GetString(File.ReadAllBytes(PendingPath)).Contains("PROGO1."), "validated friend token remains DPAPI-protected until explicit local confirmation");
            var retainedRequest = Pending(); int commands = 0;
            check(Failure(Admin("recover-invite", (exe, args, output) => { commands++; return Write(output, Json.Serialize(Status(retainedRequest, "succeeded", false))); })) is HomeVpnInvitationPendingException
                && commands == 1 && Pending().Result == token, "cached token cannot bypass live availability checks or resurrect revoked access");
            bool refused = false; try { HomeVpnInvitationRecovery.ConfirmSaved(Owner, "PROGO1.invalid"); } catch (HomeVpnInvitationPendingException) { refused = true; }
            check(refused && Pending().RequestId == id, "different token cannot consume a pending invitation");
            using (var held = File.Open(PendingPath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                refused = false; try { HomeVpnInvitationRecovery.ConfirmSaved(Owner, token); } catch (HomeVpnInvitationPendingException) { refused = true; }
                check(refused && Pending().RequestId == id, "locked local confirmation preserves the original request and token for retry");
            }
            HomeVpnInvitationRecovery.ConfirmSaved(Owner, token); check(Pending() == null, "explicit matching local-save confirmation consumes only the friend request");
        }
        private static string WrongHost(string token) {
            string text = token.Substring(7); var data = Json.Deserialize<Dictionary<string, object>>(Encoding.UTF8.GetString(Convert.FromBase64String(text.Replace('-', '+').Replace('_', '/') + new string('=', (4 - text.Length % 4) % 4))));
            data["Host"] = "other.example.org"; return "PROGO1." + Convert.ToBase64String(Encoding.UTF8.GetBytes(Json.Serialize(data))).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }
        private static void StatusGuards(string token)
        {
            var request = HomeVpnInvitationRecovery.Register(Owner, "Друг");
            var states = new List<Dictionary<string, object>> { Status(request, "running", false), Status(request, "not-found", false), Status(request, "unconfirmed", false), Status(request, "succeeded", false) };
            var busy = Status(request, "running", false); busy["Action"] = busy["Started"] = null; states.Add(busy);
            foreach (var change in new[] { new KeyValuePair<string, object>("Version", 2), new KeyValuePair<string, object>("RequestId", new string('e', 32)),
                new KeyValuePair<string, object>("Action", "setup"), new KeyValuePair<string, object>("State", "failed"), new KeyValuePair<string, object>("Started", null),
                new KeyValuePair<string, object>("Finished", "invalid"), new KeyValuePair<string, object>("ResultAvailable", "true"), new KeyValuePair<string, object>("Extra", token) }) {
                var state = Status(request); state[change.Key] = change.Value; states.Add(state);
            }
            foreach (var state in states) {
                int commands = 0;
                var error = Failure(Admin("create-invite", (exe, args, output) => { commands++; check(args.Contains("operation-status"), "even create-invite switches a pending request to status-only recovery"); return Write(output, Json.Serialize(state)); }));
                check(error is HomeVpnInvitationPendingException && !error.Message.Contains(token) && commands == 1 && Pending().RequestId == request.RequestId && Pending().Result == null,
                    "unknown, unavailable, running or malformed invitation status cannot issue or consume access");
            }
            foreach (string value in new[] { "PROGO1.invalid", WrongHost(token) }) {
                int commands = 0;
                check(Failure(Admin("recover-invite", (exe, args, output) => { commands++; return Write(output, args.Contains("operation-status") ? Json.Serialize(Status(request)) : value); })) is HomeVpnInvitationPendingException
                    && commands == 2 && Pending().Result == null, "invalid or wrong-VPS friend result cannot be retained or consumed");
            }
            foreach (byte[] bytes in new[] { new byte[] { 255 }, new byte[32769] }) {
                int commands = 0;
                check(Failure(Admin("recover-invite", (exe, args, output) => { commands++; File.WriteAllBytes(output, bytes); return Task.FromResult(0); })) is HomeVpnInvitationPendingException
                    && commands == 1 && Pending().RequestId == request.RequestId, "invalid UTF-8 and oversized invitation status preserve the request");
            }
            int lost = 0;
            check(Failure(Admin("recover-invite", (exe, args, output) => { lost++; if (lost == 1) return Write(output, Json.Serialize(Status(request))); throw new IOException("private result failure"); })) is HomeVpnInvitationPendingException
                && lost == 2 && Pending().RequestId == request.RequestId, "lost original-token query can be repeated without another invitation");
            File.Delete(PendingPath);
        }
        private static void DispatchGuards(string token)
        {
            int copies = 0, commands = 0;
            Func<string, string, Task> copy = (exe, args) => { copies++; return Task.FromResult(0); };
            Func<string, string, string, Task> command = (exe, args, output) => { commands++; return Write(output, token); };
            check(Failure(Admin("recover-invite", command, copy)) is HomeVpnInvitationPendingException && copies == 0 && commands == 0, "recovery without a journal cannot fall back to issuance");
            foreach (string label in new[] { new string('x', 81), "Друг\t" })
                check(Failure(Admin("create-invite", command, copy, label: label)) is ArgumentException && copies == 0 && commands == 0, "invalid invitation names are rejected before copying or registration");
            var request = HomeVpnInvitationRecovery.Register(Owner, "Друг");
            var wrong = Owner; wrong.Port = 2222;
            foreach (string action in new[] { "invite", "revoke" }) {
                var pendingMutation = HomeVpnService.AdminAsync(Owner, action, "Друг", action == "revoke" ? new string('a', 24) : null, delegate { }, copy, command);
                check(Failure(pendingMutation) is HomeVpnInvitationPendingException && copies == 0 && commands == 0 && Pending().RequestId == request.RequestId,
                    "pending separate invitation blocks legacy friend mutation at the service boundary");
            }
            check(Failure(Admin("create-invite", command, copy, wrong)) is HomeVpnInvitationPendingException && copies == 0 && commands == 0 && Pending().RequestId == request.RequestId,
                "changed endpoint cannot abandon a pending invitation");
            check(Failure(Admin("create-invite", command, copy, label: "Другой")) is HomeVpnInvitationPendingException && copies == 0 && commands == 0,
                "changed invitation name cannot authorize a new request");
            using (var held = File.Open(PendingPath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                check(Failure(Admin("recover-invite", (exe, args, output) => Write(output, args.Contains("operation-status") ? Json.Serialize(Status(request)) : token))) is HomeVpnInvitationPendingException
                    && Pending().Result == null, "failed atomic friend-result retention preserves recoverable original metadata");
            }
            File.Delete(PendingPath);
            check(Failure(Admin("create-invite", command, (exe, args) => { throw new HomeVpnPreparationCancelledException(); })) is HomeVpnPreparationCancelledException
                && Pending() == null && commands == 0, "fresh invitation copy cancellation leaves no request or remote mutation");
            var gate = new TaskCompletionSource<object>();
            var first = Admin("create-invite", command, (exe, args) => gate.Task);
            check(Failure(Admin("create-invite", command, copy)) is HomeVpnInvitationPendingException && copies == 0 && commands == 0,
                "concurrent invitation dispatch is refused before a second preparation");
            gate.SetException(new HomeVpnPreparationCancelledException()); check(Failure(first) is HomeVpnPreparationCancelledException && Pending() == null, "cancelled first preparation releases its exclusive invitation lease");
            gate = new TaskCompletionSource<object>();
            var legacy = HomeVpnService.AdminAsync(Owner, "revoke", null, new string('a', 24), delegate { }, (exe, args) => gate.Task, command);
            check(Failure(Admin("create-invite", command, copy)) is HomeVpnInvitationPendingException && copies == 0 && commands == 0 && Pending() == null,
                "in-flight legacy friend mutation holds the same lease before new invitation registration");
            gate.SetException(new HomeVpnPreparationCancelledException()); check(Failure(legacy) is HomeVpnPreparationCancelledException && Pending() == null,
                "cancelled legacy preparation releases the friend-mutation lease without issuing access");
            request = HomeVpnInvitationRecovery.Register(Owner, "Друг");
            var error = Failure(Admin("recover-invite", command, (exe, args) => { throw new HomeVpnPreparationCancelledException(); }));
            check(error is HomeVpnPreparationCancelledException && error.Message.Contains("могла завершиться") && !error.Message.Contains("не запускались") && Pending().RequestId == request.RequestId,
                "cancelled recovery preparation retains earlier issuance uncertainty");
            using (var lease = HomeVpnInvitationRecovery.AcquireAsync().GetAwaiter().GetResult()) {
                bool refused = false; try { HomeVpnInvitationRecovery.ConfirmSaved(Owner, token); } catch (HomeVpnInvitationPendingException) { refused = true; }
                check(refused && Pending().RequestId == request.RequestId, "confirmation cannot race an active invitation query");
            }
            File.WriteAllBytes(PendingPath, new byte[] { 1, 2, 3 });
            check(Failure(Admin("create-invite", command, copy)) is HomeVpnInvitationPendingException && copies == 0 && commands == 0, "corrupt DPAPI invitation journal blocks new work instead of deleting evidence");
            File.Delete(PendingPath);
            Directory.CreateDirectory(PendingPath);
            check(Failure(Admin("create-invite", command, copy)) is HomeVpnInvitationPendingException && copies == 0 && commands == 0, "directory at invitation record path is refused before work");
            Directory.Delete(PendingPath);
            FileStream locked = null; string cleanupDirectory = null;
            try {
                error = Failure(Admin("create-invite", (exe, args, output) => { cleanupDirectory = Path.GetDirectoryName(output); locked = File.Open(output, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None); throw new IOException("secret cleanup detail"); }));
                check(error is HomeVpnInvitationPendingException && error.Message.Contains("не удалось удалить") && !error.Message.Contains("secret") && Pending() != null,
                    "failed private-output cleanup refuses completion and retains the original invitation request");
            } finally { if (locked != null) locked.Dispose(); if (cleanupDirectory != null) Directory.Delete(cleanupDirectory, true); File.Delete(PendingPath); }
        }
        private static HomeInvitationsForm Form(Func<string, string, string, Task<string>> admin, ClipboardService clipboard) {
            return new HomeInvitationsForm(new HomeVpnInvitation[0], admin, clipboard, Owner);
        }
        private static void Shot(Form form, string work, string name) {
            Application.DoEvents(); using (var bitmap = new Bitmap(form.Width, form.Height)) { form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size)); bitmap.Save(Path.Combine(work, name + ".png")); }
        }
        private static void NativeFriends(string token, ClipboardService clipboard, string work)
        {
            int issued = 0, statusQueries = 0, resultQueries = 0; string shown = null, id = null;
            var statusGate = new TaskCompletionSource<object>(); bool wait = true; var calls = new List<string>();
            Func<string, string, string, Task<string>> admin = (action, label, ignored) => {
                calls.Add(action);
                if (action == "list") return Task.FromResult(Json.Serialize(new[] { new HomeVpnInvitation { Id = HomeVpnAccess.Parse(token).InviteId, Name = "Друг", Created = "2026-01-01T00:00:00Z" } }));
                return Admin(action, (exe, args, output) => {
                    if (args.Contains("invite --host")) { issued++; id = Pending().RequestId; throw new IOException("lost first response"); }
                    if (args.Contains("operation-status")) { statusQueries++; File.WriteAllText(output, Json.Serialize(Status(Pending()))); return wait ? (Task)statusGate.Task : Task.FromResult(0); }
                    resultQueries++; return Write(output, token);
                }, label: label);
            };
            using (var form = Form(admin, clipboard))
            using (var timer = new System.Windows.Forms.Timer { Interval = 20 }) {
                form.ShowToken = value => shown = value;
                int ticks = 0; timer.Tick += delegate { ticks++; }; timer.Start(); form.Show(); Application.DoEvents();
                var create = (Button)Field(form, "create"); var refresh = (Button)Field(form, "refresh"); var name = (TextBox)Field(form, "name"); var status = (Label)Field(form, "status");
                check(create.Text == "Создать отдельный токен" && name.Enabled && form.AcceptButton == null, "fresh native friends view retains explicit creation without Enter mutation");
                create.PerformClick(); Pump(() => !(bool)Field(form, "working"));
                check(issued == 1 && shown == null && create.Text == "Проверить выдачу токена" && create.Enabled && !name.Enabled && name.Text == "Друг"
                    && status.Text.Contains("Запрос сохранён") && status.AccessibilityObject.Description == status.Text, "lost friend response offers one accessible original-result action and freezes its name");
                refresh.PerformClick(); Pump(() => !(bool)Field(form, "working"));
                var rows = Controls(form).OfType<ListBox>().Single(); rows.SelectedIndex = 0;
                check(rows.Items.Count == 1 && !((Button)Field(form, "revoke")).Enabled && !((Button)Field(form, "reissue")).Enabled
                    && create.Text == "Проверить выдачу токена" && !name.Enabled && Pending().RequestId == id, "successful list refresh cannot clear invitation recovery or unlock another mutation");
                check(Controls(form).OfType<Button>().Count(b => b.Text == "Проверить выдачу токена") == 1 && !Controls(form).OfType<Button>().Any(b => b.Text == "Создать отдельный токен"),
                    "pending friends view has no duplicate fresh-create button");
                Shot(form, work, "invitation-recovery-friends"); form.Size = form.MinimumSize; Application.DoEvents();
                var panel = name.Parent as FlowLayoutPanel; create.Focus(); Application.DoEvents();
                check(!panel.HorizontalScroll.Visible && form.ClientRectangle.Contains(form.RectangleToClient(((Button)form.CancelButton).RectangleToScreen(((Button)form.CancelButton).ClientRectangle))),
                    "minimum invitation recovery has wrapped content and a visible Close footer"); Shot(form, work, "invitation-recovery-friends-minimum");
                create.PerformClick(); Pump(() => ticks >= 3);
                check((bool)Field(form, "working") && !create.Enabled && !((Button)form.CancelButton).Enabled && statusQueries == 1 && resultQueries == 0,
                    "protected invitation query is responsive and cannot skip status while waiting");
                form.Close(); Application.DoEvents(); check(form.Visible, "invitation close cannot abandon the protected in-flight SSH contract");
                wait = false; statusGate.SetResult(null); Pump(() => !(bool)Field(form, "working"));
                check(shown == token && issued == 1 && resultQueries == 1 && rows.Items.Count == 1 && Pending().RequestId == id && Pending().Result == token,
                    "recovered friend token is retained and the same invitation ID is not duplicated in the list");
                check(!form.InvokeRequired && SynchronizationContext.Current is WindowsFormsSynchronizationContext, "friend recovery continuations remain on the native UI thread"); form.Close();
            }
            using (var reopened = Form(admin, clipboard)) {
                reopened.Show(); Application.DoEvents(); var create = (Button)Field(reopened, "create");
                check(create.Text == "Проверить выдачу токена" && !((TextBox)Field(reopened, "name")).Enabled && ((TextBox)Field(reopened, "name")).Text == "Друг",
                    "reopening friends restores the original unfinished invitation rather than issuing a replacement");
                reopened.ShowToken = value => shown = value; create.PerformClick(); Pump(() => !(bool)Field(reopened, "working"));
                check(statusQueries == 2 && resultQueries == 2 && issued == 1 && Pending().RequestId == id, "even a cached-token reopen performs both live original-ID queries"); reopened.Close();
            }
        }
        private static void Key(Form form, Keys key) { typeof(Form).GetMethod("ProcessDialogKey", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(form, new object[] { key }); Application.DoEvents(); }
        private static void NativeWizardEntry(string token, ClipboardService clipboard)
        {
            var saved = new Dictionary<string, byte[]>();
            foreach (string name in new[] { "access", "owner" }) {
                string path = Path.Combine(HomeVpnPrivateFiles.Root, name + ".dat"); saved[path] = File.Exists(path) ? File.ReadAllBytes(path) : null;
            }
            try {
                HomeVpnPrivateFiles.Save("access", token); HomeVpnPrivateFiles.Save("owner", Json.Serialize(Owner));
                using (var relay = new Ikev2RelayService())
                using (var service = new HomeVpnService(relay))
                using (var wizard = new HomeVpnWizardForm(service, clipboard))
                using (var close = new System.Windows.Forms.Timer { Interval = 20 }) {
                    int requests = 0; bool seen = false;
                    wizard.Admin = (owner, action, label, id, progress) => { requests++; throw new IOException("fixture initial SSH unavailable"); };
                    close.Tick += delegate {
                        var friends = Application.OpenForms.Cast<Form>().OfType<HomeInvitationsForm>().FirstOrDefault(); if (friends == null) return;
                        seen = ((Button)Field(friends, "create")).Text == "Проверить выдачу токена"; ((Button)friends.CancelButton).PerformClick();
                    };
                    wizard.Show(); Application.DoEvents(); close.Start();
                    var task = (Task)typeof(HomeVpnWizardForm).GetMethod("ManageInvitations", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(wizard, null);
                    Pump(() => task.IsCompleted); task.GetAwaiter().GetResult();
                    check(seen && requests == 0 && Pending() != null, "wizard opens saved invitation recovery without depending on an initial SSH list response"); wizard.Close();
                }
            } finally { foreach (var item in saved) { if (item.Value == null) File.Delete(item.Key); else File.WriteAllBytes(item.Key, item.Value); } }
        }
        private static void NativeToken(string token, ClipboardService clipboard, string work)
        {
            int confirmed = 0;
            Action finish = () => { HomeVpnInvitationRecovery.ConfirmSaved(Owner, token); confirmed++; };
            using (var dialog = HomeInvitationList.TokenDialog(token, clipboard, finish)) {
                dialog.Show(); Application.DoEvents();
                var input = Controls(dialog).OfType<TextBox>().Single(); var confirm = Controls(dialog).OfType<Button>().Single(b => b.Text == "Токен сохранён — завершить");
                check(input.UseSystemPasswordChar && input.ReadOnly && dialog.AcceptButton == null && confirm.AccessibilityObject.Description.Contains("локальный")
                    && Controls(dialog).All(c => !(c.AccessibilityObject.Name ?? "").Contains(token) && !(c.AccessibilityObject.Description ?? "").Contains(token)),
                    "recoverable token is masked and confirmation metadata never includes credentials");
                input.Focus(); Key(dialog, Keys.Enter); check(dialog.Visible && confirmed == 0 && Pending() != null, "Enter in token field cannot confirm local receipt");
                Shot(dialog, work, "invitation-recovery-token"); dialog.Size = dialog.MinimumSize; Application.DoEvents();
                var footer = confirm.Parent; check(footer.ClientRectangle.Contains(confirm.Bounds) && footer.ClientRectangle.Contains(((Button)dialog.CancelButton).Bounds)
                    && !((FlowLayoutPanel)input.Parent).HorizontalScroll.Visible, "minimum token view keeps confirmation and Close visible with wrapped guidance"); Shot(dialog, work, "invitation-recovery-token-minimum");
                Key(dialog, Keys.Escape); check(!dialog.Visible && confirmed == 0 && Pending() != null, "token Escape closes without consuming the saved request");
            }
            using (var dialog = HomeInvitationList.TokenDialog(token, clipboard, finish)) {
                dialog.Show(); Application.DoEvents(); var confirm = Controls(dialog).OfType<Button>().Single(b => b.Text == "Токен сохранён — завершить");
                using (var held = File.Open(PendingPath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    confirm.PerformClick(); Application.DoEvents();
                    check(dialog.Visible && confirmed == 0 && Pending() != null && Controls(dialog).OfType<Label>().Any(l => l.Text.Contains("не удалось сохранить")),
                        "failed token confirmation keeps the native window, request and fixed retry guidance");
                }
                confirm.PerformClick(); Application.DoEvents(); check(!dialog.Visible && dialog.DialogResult == DialogResult.OK && confirmed == 1 && Pending() == null,
                    "explicit successful confirmation closes and consumes only the local original-token request");
            }
            using (var form = Form((a,n,i) => { throw new Exception("No native confirmation request expected"); }, clipboard)) {
                form.Show(); Application.DoEvents(); check(((Button)Field(form, "create")).Enabled && ((Button)Field(form, "create")).Text == "Создать отдельный токен" && ((TextBox)Field(form, "name")).Enabled,
                    "next friends window allows a separate invitation after explicit local completion"); form.Close();
            }
        }
        private static void NativeRefusals(ClipboardService clipboard)
        {
            File.WriteAllBytes(PendingPath, new byte[] { 1, 2, 3 }); int commands = 0;
            using (var form = Form((a,n,i) => { commands++; return Task.FromResult("[]"); }, clipboard)) {
                form.Show(); Application.DoEvents(); check(!((Button)Field(form, "create")).Enabled && !((TextBox)Field(form, "name")).Enabled && ((Label)Field(form, "status")).Text.Contains("повреждён"),
                    "corrupt friend request opens readable refusal without fresh issuance");
                ((Button)Field(form, "refresh")).PerformClick(); Pump(() => !(bool)Field(form, "working"));
                check(commands == 1 && !((Button)Field(form, "create")).Enabled && ((Button)form.CancelButton).Enabled, "fresh list cannot erase corrupt-request protection"); form.Close();
            }
            File.Delete(PendingPath); HomeVpnInvitationRecovery.Register(Owner, "Друг");
            using (var form = Form((a,n,i) => Admin(a, (exe,args,output) => { commands++; return Write(output, "[]"); }, label: n), clipboard)) {
                form.Show(); Application.DoEvents(); File.Delete(PendingPath);
                ((Button)Field(form, "create")).PerformClick(); Pump(() => !(bool)Field(form, "working"));
                check(commands == 1 && !((Button)Field(form, "create")).Enabled && ((Label)Field(form, "status")).Text.Contains("исчез"),
                    "stale recovery presentation cannot silently become fresh issuance when its journal disappears"); form.Close();
            }
        }
    }
}

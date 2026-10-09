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
                LostResponse(token); StatusGuards(token); DispatchGuards(); CorruptJournal(); NativeRecovery(token, clipboard, work);
                check(!Directory.GetDirectories(HomeVpnPrivateFiles.Root, "admin-*").Any()
                    && !Directory.GetFiles(HomeVpnPrivateFiles.Root, "setup-request.dat.*.new").Any(), "recovery settles its private local work and atomic-save temporary files");
            } finally {
                foreach (var item in saved) { if (item.Value == null) File.Delete(item.Key); else File.WriteAllBytes(item.Key, item.Value); }
            }
        }
        private static void Backup(Dictionary<string, byte[]> saved, string path) { saved[path] = File.Exists(path) ? File.ReadAllBytes(path) : null; }
        private static void Pump(Func<bool> ready)
        {
            var watch = Stopwatch.StartNew(); while (!ready()) { Application.DoEvents(); Thread.Sleep(10); if (watch.ElapsedMilliseconds > 12000) throw new Exception("Setup recovery fixture timeout"); } Application.DoEvents();
        }
        private static Exception Failure(Task task)
        {
            Pump(() => task.IsCompleted); if (!task.IsFaulted) throw new Exception("Expected recovery refusal"); return task.Exception.GetBaseException();
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
                service.UseToken(token, Owner); HomeVpnSetupRecovery.ConfirmConsumed(Owner, token);
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
            var request = HomeVpnSetupRecovery.Register(Owner, "My iPhone");
            using (var relay = new Ikev2RelayService())
            using (var service = new HomeVpnService(relay, Path.Combine(work, "absent-ssh-fixture.exe"), 15000, 14500, 17878))
            using (var form = new HomeVpnWizardForm(service, clipboard))
            using (var heartbeat = new System.Windows.Forms.Timer { Interval = 20 }) {
                form.Show(); int ticks = 0, commands = 0; var response = new TaskCompletionSource<object>();
                heartbeat.Tick += delegate { ticks++; }; heartbeat.Start();
                var next = (Button)Field(form, "next"); var state = (Label)Field(form, "setupState"); var status = (Label)Field(form, "status");
                check((int)Field(form, "step") == 1 && ((TextBox)Field(form, "host")).Text == Owner.Host && ((TextBox)Field(form, "login")).Text == Owner.Login
                    && ((NumericUpDown)Field(form, "port")).Value == 22, "reopening the wizard restores the pending endpoint even when older access exists");
                check(next.Text == "Проверить прошлую настройку" && next.AccessibilityObject.Name == next.Text && state.AccessibilityObject.Description == state.Text
                    && Controls(form).OfType<Button>().Count(b => b.Text == next.Text) == 1 && !Controls(form).OfType<Button>().Any(b => b.Text == "Настроить VPS и продолжить"),
                    "recovery has one explicit accessible next action rather than duplicate issuance controls");
                Shot(form, work, "vps-recovery-wizard"); form.ClientSize = new Size(684, 581); Application.DoEvents();
                check(form.RectangleToClient(next.RectangleToScreen(next.ClientRectangle)).Bottom <= form.ClientSize.Height
                    && !((FlowLayoutPanel)Field(form, "body")).HorizontalScroll.Visible, "minimum recovery wizard retains its action without horizontal scrolling"); Shot(form, work, "vps-recovery-wizard-minimum");
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
                form.Show(); Application.DoEvents();
                check((int)Field(form, "step") == 1 && !((Button)Field(form, "next")).Enabled && ((Label)Field(form, "setupState")).Text.Contains("повреждён")
                    && service.Access != null, "corrupt pending journal opens a blocked recovery step while preserving saved access"); form.Close();
            }
            File.Delete(PendingPath);
        }
    }
}

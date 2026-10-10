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
    // Uses the real dispatcher and Windows job ownership. Only external SCP,
    // the SSH executable and HTTPS verification are replaced with local fixtures.
    internal static class HomeVpnShareRecoveryTests
    {
        private const string Variable = "PROGO_SHARE_WAIT_FIXTURE", Origin = "https://qr.example.org";
        private static readonly JavaScriptSerializer Json = new JavaScriptSerializer();
        private static Action<bool, string> check;
        private static string folder, token, sharePath;
        private static string[] inputs;
        private static int copies;
        private static HomeVpnOwner Owner { get { return new HomeVpnOwner { Host = "vpn.example.org", Login = "root", Port = 22, KeyFile = "" }; } }
        private static string PendingPath { get { return Path.Combine(HomeVpnPrivateFiles.Root, "share-request.dat"); } }
        private static string At(string name) { return Path.Combine(folder, name); }

        internal static bool Fixture(string[] args)
        {
            if (args.Length == 1 && args[0] == "share-wait-unrelated") { Thread.Sleep(300000); return true; }
            bool child = args.Length == 2 && args[0] == "share-wait-child";
            string path = child ? args[1] : Environment.GetEnvironmentVariable(Variable);
            if (!child && (args.Length == 0 || args[0] != "-o" || String.IsNullOrEmpty(path))) return false;
            if (child) { File.WriteAllText(Path.Combine(path, "child"), Process.GetCurrentProcess().Id.ToString()); Thread.Sleep(60000); return true; }
            string command = args.Last();
            string kind = command.Contains("home_vpn_setup.py share ") ? "share" : command.Contains("home_vpn_setup.py operation-status ") ? "status"
                : command.Contains("home_vpn_setup.py operation-result ") ? "result" : null;
            var request = HomeVpnShareRecovery.Pending();
            var ids = Regex.Matches(command, @"--request-id ([0-9a-f]{32})").Cast<Match>().Select(match => match.Groups[1].Value).ToArray();
            if (kind == null || request == null || !args.Contains("-T") || !args.Contains("root@vpn.example.org") || !args.Contains("22")
                || !command.Contains("trap 'rm -rf -- /tmp/") || ids.Length != (kind == "share" ? 3 : 1) || ids.Any(id => id != request.RequestId)
                || args.Any(value => value == "StrictHostKeyChecking=no")) throw new Exception("Unexpected production HTTPS recovery dispatch");
            string secret = File.ReadAllText(Path.Combine(path, "token")); var access = HomeVpnAccess.Parse(secret);
            if (command.Contains(secret) || command.Contains(access.Password) || command.Contains(access.PrivateKey)) throw new Exception("Share command exposed private token material");
            if (kind == "share") {
                if (request.Domain != "qr.example.org" || request.Host != Owner.Host || request.Port != Owner.Port || request.Login != Owner.Login || request.ServerId != access.ServerId
                    || !command.Contains(" --domain 'qr.example.org'") || File.Exists(Path.Combine(path, "receipt"))
                    || command.IndexOf("home_vpn_setup.py share ", StringComparison.Ordinal) >= command.IndexOf("home_vpn_setup.py operation-status ", StringComparison.Ordinal)
                    || command.IndexOf("home_vpn_setup.py operation-status ", StringComparison.Ordinal) >= command.IndexOf("home_vpn_setup.py operation-result ", StringComparison.Ordinal))
                    throw new Exception("Share mutation lost its frozen request or was replayed");
                File.WriteAllText(Path.Combine(path, "receipt"), Json.Serialize(request));
            } else if (command.Contains("home_vpn_setup.py share ") || command.Contains(" --domain ")) throw new Exception("Recovery submitted a new domain installation");
            File.AppendAllText(Path.Combine(path, "calls"), kind + "\n"); File.AppendAllText(Path.Combine(path, "argv"), Json.Serialize(args) + "\n");
            File.WriteAllText(Path.Combine(path, "pid"), Process.GetCurrentProcess().Id.ToString());
            string mode = File.ReadAllText(Path.Combine(path, "mode"));
            if (kind == "share" && mode == "lost") { Environment.Exit(7); return true; }
            if (mode == "hold" || (kind == "status" && mode == "hold-status")) {
                using (var descendant = Process.Start(new ProcessStartInfo(Application.ExecutablePath, "share-wait-child " + HomeVpnService.Argument(path)) { UseShellExecute = false, CreateNoWindow = true })) {
                    var watch = Stopwatch.StartNew(); while (!File.Exists(Path.Combine(path, "child"))) { Thread.Sleep(10); if (watch.ElapsedMilliseconds > 5000) throw new Exception("Share child did not start"); }
                    Thread.Sleep(60000);
                }
                return true;
            }
            Console.OutputEncoding = new UTF8Encoding(false);
            if (kind != "result") Console.WriteLine(Json.Serialize(Status(request, mode)));
            if (kind != "status") Console.Write(mode == "wrong-result" ? "https://other.example.org" : Origin);
            return true;
        }
        private static Dictionary<string, object> Status(HomeVpnShareRequest request, string mode)
        {
            string state = mode == "not-found" ? "not-found" : mode == "busy" || mode == "running" ? "running" : mode == "stale" ? "unconfirmed" : "succeeded";
            var result = new Dictionary<string, object> { { "Version", 1 }, { "RequestId", request.RequestId }, { "Action", state == "not-found" || mode == "busy" ? null : "share" },
                { "State", state }, { "Started", state == "not-found" || mode == "busy" ? null : "2026-01-01T00:00:00.123456+00:00" },
                { "Finished", state == "succeeded" ? "2026-01-01T00:00:01+00:00" : null }, { "ResultAvailable", state == "succeeded" && mode != "unavailable" } };
            if (mode == "wrong-id") result["RequestId"] = new string('f', 32);
            if (mode == "wrong-action") result["Action"] = "repair";
            if (mode == "wrong-time") result["Started"] = "not-a-date";
            return result;
        }
        internal static void Run(Action<bool, string> assert, string sourceToken, string work)
        {
            check = assert; token = sourceToken; folder = Path.Combine(work, "share-recovery"); HomeVpnPrivateFiles.SecureDirectory(folder);
            sharePath = Path.Combine(HomeVpnPrivateFiles.Root, "share-" + HomeVpnAccess.Parse(token).ServerId + ".dat");
            var previous = new Dictionary<string, byte[]>();
            foreach (string name in new[] { "access", "owner", "home-address", "setup-request", "admin-request", "share-request" }) Backup(previous, Path.Combine(HomeVpnPrivateFiles.Root, name + ".dat"));
            Backup(previous, sharePath);
            string helpers = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scripts", "home-vpn", "server"); Directory.CreateDirectory(helpers);
            foreach (string name in new[] { "home_vpn_setup.py", "ikev2_relay.py", "install-ikev2-relay.sh", "profile_share_setup.py", "profile_share.py", "qrcodegen.py", "QR_LICENSE.txt" }) {
                string target = Path.Combine(helpers, name); Backup(previous, target); File.Copy(Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Application.ExecutablePath)), "server", name), target, true);
            }
            inputs = previous.Keys.Where(path => path != PendingPath).ToArray(); string oldVariable = Environment.GetEnvironmentVariable(Variable); Exception threadFailure = null;
            ThreadExceptionEventHandler handler = (sender, args) => { threadFailure = args.Exception; }; Application.ThreadException += handler;
            using (var unrelated = Process.Start(new ProcessStartInfo(Application.ExecutablePath, "share-wait-unrelated") { UseShellExecute = false, CreateNoWindow = true })) {
                try {
                    File.Delete(Path.Combine(HomeVpnPrivateFiles.Root, "setup-request.dat")); File.Delete(Path.Combine(HomeVpnPrivateFiles.Root, "admin-request.dat")); File.Delete(PendingPath);
                    HomeVpnPrivateFiles.Save("access", token); HomeVpnPrivateFiles.Save("owner", Json.Serialize(Owner));
                    HomeVpnPrivateFiles.Save("share-" + HomeVpnAccess.Parse(token).ServerId, "https://previous.example.org");
                    Environment.SetEnvironmentVariable(Variable, folder);
                    InvalidDomains();
                    foreach (string route in new[] { "escape", "close", "dispose", "deadline" }) Cancelled(route, false);
                    Cancelled("button", true); PendingGate();
                    foreach (string mode in new[] { "not-found", "busy", "running", "stale", "unavailable", "wrong-id", "wrong-action", "wrong-time", "wrong-result" }) RecoverForm(mode, false, false);
                    RecoverForm("complete", true, false); RecoverForm("complete", false, true); RecoverForm("complete", false, false);
                    foreach (bool failure in new[] { false, true }) DisposedVerification(failure);
                    ProofAndStorage();
                    check(threadFailure == null && !unrelated.HasExited, "HTTPS recovery settles without a UI exception or touching an unrelated native process");
                    check(Directory.GetDirectories(HomeVpnPrivateFiles.Root, "admin-*").Length == 0 && Directory.GetFiles(HomeVpnPrivateFiles.Root, "share-request.dat.*.new").Length == 0,
                        "HTTPS recovery cleans owned private work and atomic journal temporaries");
                } finally {
                    Application.ThreadException -= handler; Environment.SetEnvironmentVariable(Variable, oldVariable); Kill();
                    if (!unrelated.HasExited) { unrelated.Kill(); unrelated.WaitForExit(2000); }
                    foreach (var item in previous) { if (item.Value == null) File.Delete(item.Key); else File.WriteAllBytes(item.Key, item.Value); }
                }
            }
        }
        private static void Backup(Dictionary<string, byte[]> result, string path) { result[path] = File.Exists(path) ? File.ReadAllBytes(path) : null; }
        private static Dictionary<string, byte[]> Snapshot() { var result = new Dictionary<string, byte[]>(); foreach (string path in inputs) Backup(result, path); return result; }
        private static bool Same(Dictionary<string, byte[]> before)
        { return before.All(item => item.Value == null ? !File.Exists(item.Key) : File.Exists(item.Key) && item.Value.SequenceEqual(File.ReadAllBytes(item.Key))); }
        private static object Field(object value, string name) { return value.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(value); }
        private static IEnumerable<Control> Controls(Control value) { foreach (Control child in value.Controls) { yield return child; foreach (Control nested in Controls(child)) yield return nested; } }
        private static Button Button(Form form, string text) { return Controls(form).OfType<Button>().Single(value => value.Text == text); }
        private static Label StatusLabel(Form form) { return Controls(form).OfType<Label>().Single(value => value.AccessibleName == "Результат настройки HTTPS-выдачи"); }
        private static string[] Calls() { return File.Exists(At("calls")) ? File.ReadAllLines(At("calls")) : new string[0]; }
        private static int Id(string name) { return Int32.Parse(File.ReadAllText(At(name))); }
        private static bool Gone(int id) { try { using (var process = Process.GetProcessById(id)) return process.HasExited; } catch (ArgumentException) { return true; } }
        private static void Kill()
        { foreach (string name in new[] { "pid", "child" }) { if (!File.Exists(At(name))) continue; try { using (var process = Process.GetProcessById(Id(name))) { if (!process.HasExited) { process.Kill(); process.WaitForExit(2000); } } } catch (ArgumentException) { } } }
        private static void Mode(string mode) { Kill(); foreach (string name in new[] { "pid", "child" }) File.Delete(At(name)); File.WriteAllText(At("mode"), mode); }
        private static void Reset(string mode) { Kill(); foreach (string file in Directory.GetFiles(folder)) File.Delete(file); File.Delete(PendingPath); copies = 0; File.WriteAllText(At("token"), token); Mode(mode); }
        private static Task<string> Admin(HomeVpnOwner owner, string action, string label, string id, Action<string> progress, int timeout = 20000)
        {
            return HomeVpnService.AdminAsync(owner, action, label, id, progress, (exe, args) => {
                copies++; check(exe == "scp.exe" && args.Contains("ConnectTimeout=15") && args.Contains(" -P 22 "), "HTTPS keeps the production OpenSSH copy options"); return Task.FromResult(0);
            }, Application.ExecutablePath, timeout);
        }
        private static void Shot(Form form, string name)
        { using (var bitmap = new Bitmap(form.Width, form.Height)) { form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size)); bitmap.Save(Path.Combine(Path.GetDirectoryName(folder), name + ".png")); } }
        private sealed class Observation { internal int Root, Child, Ticks; internal bool Waiting; }
        private static Observation Loop(Form main, Action start, Func<bool> done, string route = null, Action settled = null, Action tick = null)
        {
            var result = new Observation(); bool started = false; Exception failure = null; var watch = Stopwatch.StartNew();
            using (var timer = new System.Windows.Forms.Timer { Interval = 20 }) {
                main.Shown += delegate { main.BeginInvoke(new Action(delegate { started = true; try { start(); } catch (Exception ex) { failure = ex; } })); };
                timer.Tick += delegate {
                    result.Ticks++;
                    try {
                        if (!started) return;
                        var wait = Application.OpenForms.OfType<HomeVpnPreparationForm>().FirstOrDefault();
                        if (route != null && !result.Waiting && wait != null && File.Exists(At("child")) && result.Ticks >= 3) {
                            result.Waiting = true; result.Root = Id("pid"); result.Child = Id("child"); var cancel = (Button)Field(wait, "cancel"); var status = (Label)Field(wait, "status");
                            check(cancel.Enabled && wait.CancelButton == cancel && status.Text.Contains("HTTPS-запрос сохранён") && status.AccessibilityObject.Description == status.Text,
                                "real HTTPS wait remains responsive and describes its retained request");
                            if (route == "button") { Shot(wait, "vps-https-recovery-wait"); wait.ClientSize = new Size(440, 270); Application.DoEvents();
                                check(wait.RectangleToClient(cancel.RectangleToScreen(cancel.ClientRectangle)).Bottom <= wait.ClientSize.Height, "minimum HTTPS wait retains its cancellation action"); Shot(wait, "vps-https-recovery-minimum"); }
                            if (route == "button") cancel.PerformClick(); else if (route == "escape") typeof(Form).GetMethod("ProcessDialogKey", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(wait, new object[] { Keys.Escape });
                            else if (route == "close") wait.Close(); else if (route == "dispose") wait.Dispose();
                        }
                        if (tick != null) tick();
                        if (failure == null && watch.ElapsedMilliseconds > 20000) failure = new TimeoutException("HTTPS UI fixture did not settle");
                        if (failure != null) { timer.Stop(); if (wait != null && !wait.IsDisposed) wait.Dispose(); Kill(); main.Dispose(); Application.ExitThread(); return; }
                        if (done()) { if (settled != null) settled(); timer.Stop(); main.Close(); }
                    } catch (Exception ex) { failure = ex; }
                };
                timer.Start(); Application.Run(main);
            }
            if (failure != null) throw new Exception("HTTPS native fixture failed", failure); return result;
        }
        private static Exception Dispatch(string action, HomeVpnOwner owner = null, string domain = null, string route = null, int timeout = 20000)
        {
            Task<string> task = null; using (var host = new Form()) {
                Loop(host, () => task = Admin(owner ?? Owner, action, null, domain, delegate { }, timeout), () => task != null && task.IsCompleted, route);
                try { task.GetAwaiter().GetResult(); return null; } catch (Exception ex) { return ex; }
            }
        }
        private static byte[] Lost()
        {
            Reset("lost"); var before = Snapshot(); var failure = Dispatch("share", null, "QR.EXAMPLE.ORG");
            var request = HomeVpnShareRecovery.Pending(); byte[] bytes = File.ReadAllBytes(PendingPath);
            check(failure is HomeVpnSharePendingException && copies == 1 && Calls().SequenceEqual(new[] { "share" }) && Same(before)
                && request.Domain == "qr.example.org" && request.ServerId == HomeVpnAccess.Parse(token).ServerId && !Encoding.UTF8.GetString(bytes).Contains(request.RequestId)
                && !Encoding.UTF8.GetString(bytes).Contains(request.Domain), "lost HTTPS response leaves exactly one frozen DPAPI request before any recovery");
            return bytes;
        }
        private static void Cancelled(string route, bool recovery)
        {
            byte[] journal = recovery ? Lost() : null; if (recovery) Mode("hold-status"); else Reset("hold");
            var before = Snapshot(); Task<string> task = null; Observation observed;
            using (var host = new Form()) observed = Loop(host, () => task = Admin(Owner, recovery ? "recover-share" : "share", null, recovery ? null : Origin, delegate { }, route == "deadline" ? 5000 : 20000), () => task != null && task.IsCompleted, route);
            Exception failure = null; try { task.GetAwaiter().GetResult(); } catch (Exception ex) { failure = ex; }
            check(observed.Waiting && observed.Ticks >= 3 && Gone(observed.Root) && Gone(observed.Child) && failure is HomeVpnSharePendingException && Same(before)
                && Calls().SequenceEqual(recovery ? new[] { "share", "status" } : new[] { "share" }) && copies == (recovery ? 2 : 1), "HTTPS cancellation/deadline settles its owned tree and never repeats installation: " + route + "/recover=" + recovery);
            if (journal != null) check(journal.SequenceEqual(File.ReadAllBytes(PendingPath)), "cancelled readonly recovery preserves the exact original journal");
            check(failure.Message.Contains(route == "deadline" ? "Время ожидания SSH истекло" : "Ожидание SSH прервано"), "HTTPS deadline and user cancellation remain distinguishable: " + route);
        }
        private static void PendingGate()
        {
            byte[] journal = Lost(); var before = Snapshot(); int verified = 0, saved = 0;
            using (var relay = new Ikev2RelayService())
            using (var service = new HomeVpnService(relay))
            using (var form = HomeProfileShare.CreateConfigureForm(service, (owner, action, name, id, progress) => Admin(owner, action, name, id, progress),
                (origin, access) => { verified++; return Task.FromResult(0); }, origin => { saved++; service.SetShareOrigin(origin); })) {
                Loop(form, delegate { }, () => true, null, delegate {
                    var install = Button(form, "Настроить HTTPS на VPS"); var verify = Button(form, "Адрес уже настроен — проверить"); var recover = Button(form, "Проверить прежнюю настройку HTTPS");
                    check(!install.Enabled && !verify.Enabled && recover.Enabled && recover.Visible && form.AcceptButton == null, "reopened HTTPS form gates installation and health-only verification while offering readonly recovery");
                    Controls(form).OfType<TextBox>().Single().Text = "another.example.org"; install.PerformClick(); verify.PerformClick();
                    Shot(form, "vps-https-pending-form"); form.ClientSize = new Size(440, 270); Application.DoEvents();
                    ((FlowLayoutPanel)recover.Parent).ScrollControlIntoView(recover); Shot(form, "vps-https-pending-form-minimum");
                });
            }
            var blocked = Dispatch("share", null, "another.example.org");
            foreach (var owner in new[] { new HomeVpnOwner { Host = "other.example.org", Login = "root", Port = 22 }, new HomeVpnOwner { Host = Owner.Host, Login = "admin", Port = 22 }, new HomeVpnOwner { Host = Owner.Host, Login = "root", Port = 2022 } })
                check(Dispatch("recover-share", owner) is HomeVpnSharePendingException, "recovery with changed owner binding is refused before copy or SSH");
            check(blocked is HomeVpnSharePendingException && copies == 1 && Calls().SequenceEqual(new[] { "share" }) && verified == 0 && saved == 0 && Same(before)
                && journal.SequenceEqual(File.ReadAllBytes(PendingPath)), "pending HTTPS survives a new form and changed domain without SSH, health-only clearing or replay");
        }
        private static void RecoverForm(string mode, bool verifyFailure, bool saveFailure)
        {
            byte[] journal = Lost(); Mode(mode); var before = Snapshot(); int verified = 0, saved = 0; bool boundary = false; string message = null;
            using (var relay = new Ikev2RelayService())
            using (var service = new HomeVpnService(relay))
            using (var form = HomeProfileShare.CreateConfigureForm(service, (owner, action, name, id, progress) => {
                check(action == "recover-share" && name == null && id == null, "recover button uses only its frozen saved request"); return Admin(owner, action, name, id, progress);
            }, (origin, access) => { verified++; if (verifyFailure) throw new IOException("Контрольная ошибка проверки HTTPS."); return Task.FromResult(0); }, origin => {
                saved++; boundary = File.Exists(PendingPath) && verified == 1 && journal.SequenceEqual(File.ReadAllBytes(PendingPath));
                if (saveFailure) throw new IOException("synthetic-private-save-error"); service.SetShareOrigin(origin);
            })) {
                var address = Controls(form).OfType<TextBox>().Single(); address.Text = "another.example.org";
                Loop(form, () => Button(form, "Проверить прежнюю настройку HTTPS").PerformClick(), () => address.Enabled, null, delegate {
                    message = StatusLabel(form).Text;
                    if (mode == "not-found" || saveFailure) Shot(form, saveFailure ? "vps-https-save-error" : "vps-https-recovery-error");
                    bool success = mode == "complete" && !verifyFailure && !saveFailure;
                    check(form.DialogResult == (success ? DialogResult.OK : DialogResult.None), "HTTPS UI acknowledges completion only after successful verification and local save: " + mode);
                });
                bool completed = mode == "complete" && !verifyFailure && !saveFailure;
                bool resultRead = mode == "complete" || mode == "wrong-result";
                check(Calls().SequenceEqual(resultRead ? new[] { "share", "status", "result" } : new[] { "share", "status" }) && copies == (resultRead ? 3 : 2)
                    && Calls().Count(value => value == "share") == 1, "native recovery queries only status/result and never submits another share: " + mode);
                if (completed) check(verified == 1 && saved == 1 && boundary && !File.Exists(PendingPath) && service.ShareOrigin == Origin
                    && Same(before.Where(item => item.Key != sharePath).ToDictionary(item => item.Key, item => item.Value)), "receipt + exact origin + HTTPS + successful save consume only the matching request");
                else check(journal.SequenceEqual(File.ReadAllBytes(PendingPath)) && Same(before) && verified == (mode == "complete" ? 1 : 0)
                    && saved == (saveFailure ? 1 : 0) && !message.Contains("synthetic-private-save-error"), "uncertain status, wrong result or verification/save failure retains the journal and former origin: " + mode);
            }
        }
        private static void DisposedVerification(bool lateFailure)
        {
            byte[] journal = Lost(); Mode("complete"); var before = Snapshot(); int verified = 0, saved = 0, updates = 0, drain = 0; bool disposed = false, settled = false;
            var ready = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (var relay = new Ikev2RelayService())
            using (var service = new HomeVpnService(relay))
            using (var host = new Form())
            using (var form = HomeProfileShare.CreateConfigureForm(service, (owner, action, name, id, progress) => Admin(owner, action, name, id, progress),
                (origin, access) => { verified++; return ready.Task; }, origin => { saved++; service.SetShareOrigin(origin); })) {
                Loop(host, delegate { form.Show(host); Button(form, "Проверить прежнюю настройку HTTPS").PerformClick(); }, () => settled && drain >= 3, null, null, delegate {
                    if (verified == 1 && !disposed) {
                        check(SynchronizationContext.Current is WindowsFormsSynchronizationContext, "disposed HTTPS fixture retains a live owner UI context");
                        var retained = Controls(form).ToArray(); form.Dispose(); disposed = true;
                        foreach (var control in retained) { control.TextChanged += delegate { updates++; }; control.EnabledChanged += delegate { updates++; }; control.VisibleChanged += delegate { updates++; }; }
                        if (lateFailure) ready.SetException(new IOException("Контрольная поздняя ошибка HTTPS.")); else ready.SetResult(null);
                    }
                    if (disposed && !settled) {
                        var probe = HomeVpnShareRecovery.AcquireAsync();
                        if (probe.IsCompleted && !probe.IsFaulted) { probe.GetAwaiter().GetResult().Dispose(); settled = true; }
                        else if (probe.IsFaulted) { var observed = probe.Exception; }
                    }
                    if (settled) drain++;
                });
            }
            check(disposed && verified == 1 && saved == 0 && updates == 0 && journal.SequenceEqual(File.ReadAllBytes(PendingPath)) && Same(before)
                && Calls().SequenceEqual(new[] { "share", "status", "result" }), "disposing actual HTTPS form during verification prevents late save/publication and retains recovery: failure=" + lateFailure);
        }
        private static bool Refused(Task task)
        { try { task.GetAwaiter().GetResult(); return false; } catch (HomeVpnSharePendingException) { return true; } }
        private static void Protect(Dictionary<string, object> value)
        { File.WriteAllBytes(PendingPath, ProtectedData.Protect(Encoding.UTF8.GetBytes(new JavaScriptSerializer { MaxJsonLength = 100000 }.Serialize(value)), null, DataProtectionScope.CurrentUser)); }
        private static void InvalidDomains()
        {
            Reset("complete"); var before = Snapshot();
            foreach (string domain in new[] { "old.example.org.", "old..example.org", "-old.example.org", "old-.example.org", new string('a', 64) + ".example.org" })
                check(Dispatch("share", null, domain) is ArgumentException && copies == 0 && Calls().Length == 0 && !File.Exists(PendingPath) && Same(before),
                    "invalid DNS labels fail before helper copy or HTTPS request registration");
        }
        private static void ProofAndStorage()
        {
            byte[] journal = Lost(); var request = HomeVpnShareRecovery.Pending(); int verified = 0, saved = 0; var access = HomeVpnAccess.Parse(token);
            Func<Task> verify = () => { verified++; return Task.FromResult(0); }; Action<string> save = origin => { saved++; };
            HomeVpnShareRecovery.RequireResult(Owner, request, Origin);
            check(Refused(HomeVpnShareRecovery.ConfirmAsync(Owner, access, Origin, verify, save, () => true)) && verified == 0 && saved == 0
                && journal.SequenceEqual(File.ReadAllBytes(PendingPath)), "health and an exact URL without completed receipt proof cannot consume a request");
            HomeVpnShareRecovery.RequireCompleted(Json.Serialize(Status(request, "complete")), request); HomeVpnShareRecovery.RequireResult(Owner, request, Origin);
            access.ServerId = new string('d', 32);
            check(Refused(HomeVpnShareRecovery.ConfirmAsync(Owner, access, Origin, verify, save, () => true)) && verified == 0 && saved == 0, "a different ServerId cannot verify or consume the saved HTTPS request");
            access = HomeVpnAccess.Parse(token);
            check(Refused(HomeVpnShareRecovery.ConfirmAsync(Owner, access, Origin, verify, origin => { saved++; access.ServerId = new string('d', 32); }, () => true))
                && verified == 1 && saved == 1 && journal.SequenceEqual(File.ReadAllBytes(PendingPath)), "a save callback that changes the access binding cannot consume the completed request");
            access = HomeVpnAccess.Parse(token); verified = saved = 0;
            foreach (var change in new Dictionary<string, object> { { "Version", 2 }, { "RequestId", new string('d', 32) }, { "Action", "repair" }, { "Host", "other.example.org" },
                { "Port", 2022 }, { "Login", "admin" }, { "Domain", "other.example.org" }, { "ServerId", new string('d', 32) } }) {
                File.WriteAllBytes(PendingPath, journal); HomeVpnShareRecovery.RequireCompleted(Json.Serialize(Status(request, "complete")), request); HomeVpnShareRecovery.RequireResult(Owner, request, Origin);
                var fields = Json.Deserialize<Dictionary<string, object>>(Json.Serialize(request)); fields[change.Key] = change.Value; Protect(fields); byte[] changed = File.ReadAllBytes(PendingPath);
                check(Refused(HomeVpnShareRecovery.ConfirmAsync(Owner, access, Origin, verify, save, () => true)) && verified == 0 && saved == 0 && changed.SequenceEqual(File.ReadAllBytes(PendingPath)),
                    "changed immutable journal field cannot consume the original completed proof: " + change.Key);
            }
            foreach (string mode in new[] { "missing", "extra", "type", "invalid", "oversized", "corrupt" }) {
                File.WriteAllBytes(PendingPath, journal); var fields = Json.Deserialize<Dictionary<string, object>>(Json.Serialize(request));
                if (mode == "missing") fields.Remove("Domain"); else if (mode == "extra") fields["Result"] = Origin; else if (mode == "type") fields["Port"] = "22";
                else if (mode == "invalid") fields["Domain"] = "https://qr.example.org/path"; else if (mode == "oversized") fields["Domain"] = new string('x', 50000);
                if (mode == "corrupt") File.WriteAllBytes(PendingPath, new byte[] { 1, 2, 3 }); else Protect(fields);
                byte[] changed = File.ReadAllBytes(PendingPath); int count = copies;
                check(Dispatch("recover-share") is HomeVpnSharePendingException && copies == count && Calls().SequenceEqual(new[] { "share" }) && changed.SequenceEqual(File.ReadAllBytes(PendingPath)),
                    "malformed protected HTTPS storage blocks copy/SSH without forgetting the request: " + mode);
            }
            File.WriteAllBytes(PendingPath, journal);
            using (var locked = new FileStream(PendingPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                check(Dispatch("recover-share") is HomeVpnSharePendingException && copies == 1, "locked HTTPS storage blocks recovery before dispatch");
            check(journal.SequenceEqual(File.ReadAllBytes(PendingPath)), "a locked HTTPS request retains its exact protected bytes");
            File.Delete(PendingPath); Directory.CreateDirectory(PendingPath);
            try { check(Dispatch("recover-share") is HomeVpnSharePendingException && copies == 1, "a folder cannot impersonate a pending request or unblock a new installation"); }
            finally { Directory.Delete(PendingPath); File.WriteAllBytes(PendingPath, journal); }
        }
    }
}

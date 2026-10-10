using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProGo
{
    internal static class HomeVpnPreparationTests
    {
        private static Action<bool, string> check;
        private static string folder;
        internal static bool Fixture(string[] args)
        {
            bool list = args.Length > 0 && args[0] == "-o" && !String.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROGO_LIST_WAIT_FIXTURE"));
            if (!list && (args.Length < 3 || (args[0] != "prepare-fixture" && args[0] != "prepare-child"))) return false;
            string path = list ? Environment.GetEnvironmentVariable("PROGO_LIST_WAIT_FIXTURE") : args[1];
            string mode = list ? File.ReadAllText(Path.Combine(path, "list-mode")) : args[2];
            if (list) {
                if (!args.Last().Contains("home_vpn_setup.py list ") || args.Last().Contains("--request-id")) throw new Exception("Fixture expected a read-only production list command");
                File.AppendAllText(Path.Combine(path, "list-calls"), "list\n");
            }
            if (args[0] == "prepare-child") {
                File.WriteAllText(Path.Combine(path, "child"), Process.GetCurrentProcess().Id.ToString()); Thread.Sleep(60000); return true;
            }
            File.WriteAllText(Path.Combine(path, "pid"), Process.GetCurrentProcess().Id.ToString());
            File.WriteAllText(Path.Combine(path, "console"), GetConsoleCP().ToString());
            if (mode == "complete") {
                if (list) { Console.OutputEncoding = new System.Text.UTF8Encoding(false); Console.WriteLine(File.ReadAllText(Path.Combine(path, "list-response"))); }
                return true;
            }
            if (mode == "fail") { Environment.Exit(7); return true; }
            using (var child = Process.Start(new ProcessStartInfo(Application.ExecutablePath,
                "prepare-child " + HomeVpnService.Argument(path) + " " + mode) { UseShellExecute = false, CreateNoWindow = true })) {
                var watch = Stopwatch.StartNew();
                while (!File.Exists(Path.Combine(path, "child"))) { Thread.Sleep(10); if (watch.ElapsedMilliseconds > 3000) throw new Exception("Fixture child did not start"); }
                if (mode == "orphan") return true;
                Thread.Sleep(60000); return true;
            }
        }
        internal static void Run(Action<bool, string> assert, string token, ClipboardService clipboard, string work)
        {
            check = assert; folder = Path.Combine(work, "vps-preparation"); Directory.CreateDirectory(folder);
            var previous = new Dictionary<string, byte[]>();
            foreach (string name in new[] { "access", "owner", "home-address", HomeVpnSetupRecovery.StorageName }) {
                string path = Path.Combine(HomeVpnPrivateFiles.Root, name + ".dat"); previous[path] = File.Exists(path) ? File.ReadAllBytes(path) : null;
            }
            File.Delete(Path.Combine(HomeVpnPrivateFiles.Root, HomeVpnSetupRecovery.StorageName + ".dat"));
            string helpers = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scripts", "home-vpn", "server"); Directory.CreateDirectory(helpers);
            foreach (var file in new[] { "home_vpn_setup.py", "ikev2_relay.py", "install-ikev2-relay.sh", "profile_share_setup.py", "profile_share.py", "qrcodegen.py", "QR_LICENSE.txt" }) {
                string target = Path.Combine(helpers, file); previous[target] = File.Exists(target) ? File.ReadAllBytes(target) : null;
                File.Copy(Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Application.ExecutablePath)), "server", file), target, true);
            }
            var unrelated = Process.Start(new ProcessStartInfo(Application.ExecutablePath, "unrelated-wait") { UseShellExecute = false, CreateNoWindow = true });
            try {
                foreach (string route in new[] { "button", "escape", "close", "dispose" }) NativeCancel(route, work);
                foreach (string route in new[] { "button", "escape", "close", "dispose" }) RemoteCancel(route, work);
                RemoteBoundary(); RemoteDeadline(); ProcessChecks(); CompletionRace(); WizardBoundary(token, clipboard); CleanupFailure();
                ListWaiting(token, clipboard, work);
                check(!unrelated.HasExited, "preparation cancellation, deadline and normal completion preserve an unrelated process");
            } finally {
                KillFixtures(); if (!unrelated.HasExited) { unrelated.Kill(); unrelated.WaitForExit(2000); } unrelated.Dispose();
                foreach (var item in previous) { if (item.Value == null) File.Delete(item.Key); else File.WriteAllBytes(item.Key, item.Value); }
            }
        }
        private static object Field(object target, string name) { return target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target); }
        private static void Call(object target, string name, params object[] args) { target.GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, args); }
        private static void Pump(Func<bool> ready, int timeout = 12000)
        {
            var watch = Stopwatch.StartNew(); while (!ready()) { Application.DoEvents(); Thread.Sleep(10); if (watch.ElapsedMilliseconds > timeout) throw new Exception("Preparation fixture timeout"); } Application.DoEvents();
        }
        private static string Args(string mode) { return "prepare-fixture " + HomeVpnService.Argument(folder) + " " + mode; }
        private static int Id(string file) { return Int32.Parse(File.ReadAllText(Path.Combine(folder, file))); }
        private static bool Gone(int id) { try { using (var process = Process.GetProcessById(id)) return process.HasExited; } catch (ArgumentException) { return true; } }
        private static void KillFixtures()
        {
            foreach (string file in new[] { "pid", "child" }) {
                if (!File.Exists(Path.Combine(folder, file))) continue;
                try { using (var process = Process.GetProcessById(Id(file))) { if (!process.HasExited) { process.Kill(); process.WaitForExit(2000); } } } catch (ArgumentException) { }
            }
        }
        private static void Reset() { KillFixtures(); foreach (string name in new[] { "pid", "child", "console" }) File.Delete(Path.Combine(folder, name)); }
        private static void Shot(Form form, string work, string name) { using (var image = new Bitmap(form.Width, form.Height)) { form.DrawToBitmap(image, new Rectangle(Point.Empty, image.Size)); image.Save(Path.Combine(work, name + ".png")); } }
        private static void NativeCancel(string route, string work)
        {
            Reset();
            using (var form = new HomeVpnPreparationForm(token => HomeVpnPreparationProcess.CopyAsync(Application.ExecutablePath, Args("hold"), 20000, token)))
            using (var heartbeat = new System.Windows.Forms.Timer { Interval = 20 }) {
                int ticks = 0; heartbeat.Tick += delegate { ticks++; }; heartbeat.Start(); form.Show();
                Pump(() => File.Exists(Path.Combine(folder, "child")) && ticks >= 3);
                var cancel = (Button)Field(form, "cancel"); var status = (Label)Field(form, "status");
                int root = Id("pid"), child = Id("child");
                check(Int32.Parse(File.ReadAllText(Path.Combine(folder, "console"))) != 0, "preparation child has a real console for SSH prompts");
                check(form.AcceptButton == null && form.CancelButton == cancel && cancel.AccessibilityObject.Name == "Отменить подготовку"
                    && status.AccessibilityObject.Description == status.Text && status.Text.Contains("5 минут") && status.Text.Contains("ещё не запускались"),
                    "preparation dialog has an accessible bounded wait and no implicit Enter action");
                cancel.Focus(); check(form.ActiveControl == cancel && cancel.Enabled, "preparation cancellation is reachable by keyboard while worker is pending");
                if (route == "button") {
                    Shot(form, work, "vps-preparation-pending"); form.ClientSize = new Size(440, 270); Application.DoEvents();
                    check(form.RectangleToClient(cancel.RectangleToScreen(cancel.ClientRectangle)).Bottom <= form.ClientSize.Height,
                        "preparation cancellation remains visible at minimum size"); Shot(form, work, "vps-preparation-minimum");
                    var heading = (Label)Field(form, "heading"); var viewport = (FlowLayoutPanel)Field(form, "viewport");
                    check(heading.Visible && heading.Top >= 0 && heading.Bottom <= viewport.ClientSize.Height
                        && heading.GetPreferredSize(new Size(heading.Width, 0)).Height <= heading.Height,
                        "minimum preparation window retains its complete visible heading");
                    check(!viewport.HorizontalScroll.Visible && heading.Right <= viewport.ClientSize.Width && status.Right <= viewport.ClientSize.Width
                        && status.GetPreferredSize(new Size(status.Width, 0)).Height <= status.Height,
                        "minimum preparation text wraps without clipping or horizontal scrolling");
                }
                var watch = Stopwatch.StartNew();
                if (route == "button") cancel.PerformClick();
                else if (route == "escape") typeof(Form).GetMethod("ProcessDialogKey", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(form, new object[] { Keys.Escape });
                else if (route == "close") form.Close(); else form.Dispose();
                if (route != "dispose") check(form.Visible && !cancel.Enabled && !form.Completion.IsCompleted, "preparation close/cancel waits for owned process settlement");
                Pump(() => form.Completion.IsCompleted);
                check(watch.ElapsedMilliseconds < 3500 && Gone(root) && Gone(child), "preparation cancellation confirms root and descendant exit before returning");
                check(form.Completion.IsFaulted && form.Completion.Exception.GetBaseException() is HomeVpnPreparationCancelledException
                    && form.Completion.Exception.GetBaseException().Message.Contains("не запускались"), "confirmed preparation cancellation is distinct from server mutation or failure");
                check(!form.Visible, "preparation dialog closes after settlement without late disposed UI access");
            }
            Reset(); using (var retry = new HomeVpnPreparationForm(token => HomeVpnPreparationProcess.CopyAsync(Application.ExecutablePath, Args("complete"), 20000, token))) {
                retry.Show(); Pump(() => retry.Completion.IsCompleted); retry.Completion.GetAwaiter().GetResult();
                check(Gone(Id("pid")), "fresh explicit preparation retry succeeds and releases its child");
            }
        }
        private static void RemoteCancel(string route, string work)
        {
            Reset(); var owner = new HomeVpnOwner { Host = "vpn.example.org", Login = "root", Port = 22 };
            var request = HomeVpnSetupRecovery.Register(owner, "My iPhone");
            var journal = File.ReadAllBytes(Path.Combine(HomeVpnPrivateFiles.Root, HomeVpnSetupRecovery.StorageName + ".dat"));
            using (var form = new HomeVpnPreparationForm(ct => HomeVpnPreparationProcess.CommandAsync(Application.ExecutablePath,
                Args("hold"), Path.Combine(folder, "result"), 20000, ct), true, true))
            using (var heartbeat = new System.Windows.Forms.Timer { Interval = 20 }) {
                int ticks = 0; heartbeat.Tick += delegate { ticks++; }; heartbeat.Start(); form.Show();
                Pump(() => File.Exists(Path.Combine(folder, "child")) && ticks >= 3);
                int root = Id("pid"), child = Id("child");
                var cancel = (Button)Field(form, "cancel"); var status = (Label)Field(form, "status");
                check(form.AcceptButton == null && form.CancelButton == cancel && cancel.AccessibilityObject.Name == "Прервать ожидание SSH"
                    && status.AccessibilityObject.Description == status.Text && status.Text.Contains("могла уже"),
                    "remote wait describes uncertainty and provides explicit keyboard cancellation without implicit Enter");
                if (route == "button") {
                    Shot(form, work, "vps-ssh-wait-pending"); form.ClientSize = new Size(440, 270); Application.DoEvents();
                    var heading = (Label)Field(form, "heading"); var viewport = (FlowLayoutPanel)Field(form, "viewport");
                    check(cancel.Bottom <= form.ClientSize.Height && heading.Visible && heading.Top >= 0 && heading.Bottom <= viewport.ClientSize.Height
                        && !viewport.HorizontalScroll.Visible && status.Right <= viewport.ClientSize.Width,
                        "minimum remote wait retains heading, wrapped status and cancellation");
                    Shot(form, work, "vps-ssh-wait-minimum"); cancel.PerformClick();
                } else if (route == "escape") typeof(Form).GetMethod("ProcessDialogKey", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(form, new object[] { Keys.Escape });
                else if (route == "close") form.Close(); else form.Dispose();
                if (route != "dispose") check(form.Visible && !form.Completion.IsCompleted && !cancel.Enabled, "remote wait stays open until local process settlement");
                Pump(() => form.Completion.IsCompleted);
                check(form.Completion.IsFaulted && form.Completion.Exception.GetBaseException() is HomeVpnSetupPendingException
                    && form.Completion.Exception.GetBaseException().Message.Contains("могла завершиться") && Gone(root) && Gone(child),
                    "remote wait cancellation settles only owned processes and reports an uncertain VPS result");
                check(journal.SequenceEqual(File.ReadAllBytes(Path.Combine(HomeVpnPrivateFiles.Root, HomeVpnSetupRecovery.StorageName + ".dat")))
                    && HomeVpnSetupRecovery.Load(owner, "My iPhone").RequestId == request.RequestId,
                    "all cancellation routes preserve the exact protected original request");
            }
            File.Delete(Path.Combine(HomeVpnPrivateFiles.Root, HomeVpnSetupRecovery.StorageName + ".dat"));
        }
        private static void RemoteBoundary()
        {
            Reset(); var owner = new HomeVpnOwner { Host = "vpn.example.org", Login = "root", Port = 22 }; int commands = 0;
            var task = HomeVpnService.AdminAsync(owner, "setup", "My iPhone", null, delegate { },
                (exe, args) => Task.FromResult(0), async (exe, args, output) => {
                    commands++; check(HomeVpnSetupRecovery.HasPending() && args.Contains("--request-id"), "durable request precedes cancellable SSH dispatch");
                    using (var form = new HomeVpnPreparationForm(ct => {
                        var done = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                        ct.Register(() => done.TrySetCanceled()); return done.Task;
                    }, true, true)) {
                        form.Show(); ((Button)Field(form, "cancel")).PerformClick(); await form.Completion;
                    }
                });
            Pump(() => task.IsCompleted);
            check(task.IsFaulted && task.Exception.GetBaseException() is HomeVpnSetupPendingException && commands == 1 && HomeVpnSetupRecovery.HasPending()
                && !Directory.GetDirectories(HomeVpnPrivateFiles.Root, "admin-*").Any(), "cancelled SSH keeps recovery request, cleans private work and cannot commit access");
            var id = HomeVpnSetupRecovery.Load(owner, "My iPhone").RequestId;
            var retry = HomeVpnService.AdminAsync(owner, "setup", "My iPhone", null, delegate { }, (exe, args) => Task.FromResult(0), (exe, args, output) => {
                commands++; check(args.Contains("operation-status") && args.Contains(id) && !args.Contains(" setup "), "explicit retry queries original status instead of resubmitting setup");
                throw new IOException("fixture status unavailable");
            });
            Pump(() => retry.IsCompleted); check(retry.IsFaulted && commands == 2 && HomeVpnSetupRecovery.HasPending(), "unavailable recovery never discards uncertain request");
            File.Delete(Path.Combine(HomeVpnPrivateFiles.Root, HomeVpnSetupRecovery.StorageName + ".dat"));
            var ready = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (var race = new HomeVpnPreparationForm(ct => ready.Task, true, true)) {
                race.Show(); ((Button)Field(race, "cancel")).PerformClick(); ready.SetResult(null); Pump(() => race.Completion.IsCompleted);
                check(race.Completion.IsFaulted && race.Completion.Exception.GetBaseException() is HomeVpnSetupPendingException, "accepted remote cancellation beats a queued successful local response");
            }
        }
        private static void RemoteDeadline()
        {
            Reset(); var timeout = Task.Run(() => HomeVpnPreparationProcess.Run(Application.ExecutablePath, Args("hold"), 3000, CancellationToken.None, true));
            Pump(() => File.Exists(Path.Combine(folder, "child"))); int root = Id("pid"), child = Id("child"); Pump(() => timeout.IsCompleted);
            check(timeout.IsFaulted && timeout.Exception.GetBaseException() is TimeoutException && timeout.Exception.GetBaseException().Message.Contains("могла завершиться")
                && !timeout.Exception.GetBaseException().Message.Contains("не запускались") && Gone(root) && Gone(child), "SSH deadline settles owned tree without claiming remote rollback");
            Reset(); var complete = HomeVpnPreparationProcess.CommandAsync(Application.ExecutablePath, Args("complete"), Path.Combine(folder, "result"), 20000, CancellationToken.None);
            Pump(() => complete.IsCompleted); complete.GetAwaiter().GetResult(); check(Gone(Id("pid")), "successful SSH waiting releases owned console");
        }
        private static void ListWaiting(string token, ClipboardService clipboard, string work)
        {
            string previous = Environment.GetEnvironmentVariable("PROGO_LIST_WAIT_FIXTURE");
            Environment.SetEnvironmentVariable("PROGO_LIST_WAIT_FIXTURE", folder);
            File.WriteAllText(Path.Combine(folder, "list-response"), new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(new[] {
                new HomeVpnInvitation { Id = new string('a', 24), Name = "Друг", Revoked = true },
                new HomeVpnInvitation { Id = new string('b', 24), Name = "Друг" } }));
            try {
                foreach (string route in new[] { "button", "escape", "close", "deadline" }) ListWizardWaiting(token, clipboard, work, route);
                foreach (string route in new[] { "button", "escape", "close" }) ListRefreshWaiting(clipboard, work, route);
                ListCopyWaiting();
                var ready = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                using (var race = new HomeVpnPreparationForm(ct => ready.Task, HomeVpnWaitPurpose.ListCommand)) {
                    race.Show(); ((Button)Field(race, "cancel")).PerformClick(); ready.SetResult(null); Pump(() => race.Completion.IsCompleted);
                    check(race.Completion.IsFaulted && race.Completion.Exception.GetBaseException() is HomeVpnListCancelledException,
                        "accepted list cancellation cannot publish a queued successful response");
                }
            } finally { Reset(); Environment.SetEnvironmentVariable("PROGO_LIST_WAIT_FIXTURE", previous); }
        }
        private static void ListCopyWaiting()
        {
            Reset(); int root = 0, child = 0; bool observed = false; Exception callbackFailure = null;
            using (var controller = new System.Windows.Forms.Timer { Interval = 20 }) {
                controller.Tick += delegate {
                    var wait = Application.OpenForms.OfType<HomeVpnPreparationForm>().FirstOrDefault();
                    try {
                        if (wait == null || !File.Exists(Path.Combine(folder, "child"))) return;
                        controller.Stop(); observed = true; root = Id("pid"); child = Id("child");
                        var status = (Label)Field(wait, "status");
                        check(status.Text.Contains("не создаёт и не отзывает") && !status.Text.Contains("запрос сохранён") && !status.Text.Contains("прежн"),
                            "production list preparation uses read-only copy without a false recovery-request promise");
                        ((Button)Field(wait, "cancel")).PerformClick();
                    } catch (Exception error) { callbackFailure = error; controller.Stop(); if (wait != null && !wait.IsDisposed) wait.Close(); }
                }; controller.Start();
                var copy = HomeVpnPreparationForm.CopyForListAsync(null, Application.ExecutablePath, Args("hold"));
                Pump(() => copy.IsCompleted);
                if (callbackFailure != null) throw new Exception("List copy UI controller failed", callbackFailure);
                check(observed && copy.IsFaulted && copy.Exception.GetBaseException() is HomeVpnListCancelledException && Gone(root) && Gone(child),
                    "production list-copy cancellation settles its owned tree and has its own retryable read-only result");
            }
        }
        private static Dictionary<string, byte[]> ListPrivateSnapshot()
        {
            return new[] { "access", "owner", "home-address", HomeVpnSetupRecovery.StorageName }
                .Select(name => Path.Combine(HomeVpnPrivateFiles.Root, name + ".dat"))
                .ToDictionary(path => path, path => File.Exists(path) ? File.ReadAllBytes(path) : null);
        }
        private static bool ListPrivateUnchanged(Dictionary<string, byte[]> previous)
        {
            return previous.All(pair => pair.Value == null ? !File.Exists(pair.Key) :
                File.Exists(pair.Key) && pair.Value.SequenceEqual(File.ReadAllBytes(pair.Key)));
        }
        private static int ListCalls() { return File.Exists(Path.Combine(folder, "list-calls")) ? File.ReadAllLines(Path.Combine(folder, "list-calls")).Length : 0; }
        private static void ListMode(string mode, bool fresh)
        {
            Reset(); File.WriteAllText(Path.Combine(folder, "list-mode"), mode);
            if (fresh) File.Delete(Path.Combine(folder, "list-calls"));
        }
        private static void CancelList(HomeVpnPreparationForm form, string route)
        {
            if (route == "button") ((Button)Field(form, "cancel")).PerformClick();
            else if (route == "escape") typeof(Form).GetMethod("ProcessDialogKey", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(form, new object[] { Keys.Escape });
            else if (route == "close") form.Close();
        }
        private static void ListWizardWaiting(string token, ClipboardService clipboard, string work, string route)
        {
            ListMode("hold", true); int copies = 0, root = 0, child = 0, ticks = 0; bool observed = false;
            Exception callbackFailure = null; var progressMessages = new List<string>();
            using (var relay = new Ikev2RelayService())
            using (var service = new HomeVpnService(relay))
            using (var wizard = new HomeVpnWizardForm(service, clipboard))
            using (var heartbeat = new System.Windows.Forms.Timer { Interval = 20 })
            using (var controller = new System.Windows.Forms.Timer { Interval = 20 }) {
                service.UseToken(token, new HomeVpnOwner { Host = "vpn.example.org", Login = "root", Port = 22 });
                wizard.Admin = (owner, action, label, id, progress) => {
                    check(action == "list", "initial Friends action dispatches only a read-only list");
                    return HomeVpnService.AdminAsync(owner, action, label, id, message => { progressMessages.Add(message); progress(message); },
                        (exe, args) => { copies++; return Task.FromResult(0); }, Application.ExecutablePath, route == "deadline" ? 5000 : 20000);
                };
                Call(wizard, "ShowStep", 4); wizard.Show();
                var before = ListPrivateSnapshot(); heartbeat.Tick += delegate { ticks++; }; heartbeat.Start();
                controller.Tick += delegate {
                    var wait = Application.OpenForms.OfType<HomeVpnPreparationForm>().FirstOrDefault();
                    try {
                        if (wait == null || !File.Exists(Path.Combine(folder, "child")) || ticks < 3) return;
                        controller.Stop(); observed = true; root = Id("pid"); child = Id("child");
                        var status = (Label)Field(wait, "status"); var cancel = (Button)Field(wait, "cancel");
                        check(cancel.Enabled && wait.CancelButton == cancel && status.AccessibilityObject.Description == status.Text
                            && status.Text.Contains("5 минут") && !status.Text.Contains("запрос сохранён") && !status.Text.Contains("могла"),
                            "production list dispatch provides responsive accessible cancellation and honest read-only progress");
                        if (route == "button") { Shot(wait, work, "vps-list-wait-pending"); wait.ClientSize = new Size(440, 270); Application.DoEvents();
                            check(cancel.Bottom <= wait.ClientSize.Height, "minimum list wait retains visible cancellation"); Shot(wait, work, "vps-list-wait-minimum"); }
                        CancelList(wait, route);
                    } catch (Exception error) { callbackFailure = error; controller.Stop(); if (wait != null && !wait.IsDisposed) wait.Close(); }
                }; controller.Start();
                var open = ((Control)Field(wizard, "body")).Controls.OfType<Button>().Single(b => b.Text == "Доступ друзей…"); open.PerformClick();
                Pump(() => !(bool)Field(wizard, "busy"));
                if (callbackFailure != null) throw new Exception("List waiting UI controller failed", callbackFailure);
                var result = (Label)Field(wizard, "status");
                check(progressMessages.Count == 2 && progressMessages[0].StartsWith("Подготовка получения списка друзей")
                    && progressMessages[1].StartsWith("Получение списка друзей с VPS")
                    && progressMessages.All(message => !message.Contains("Настройка VPS") && !message.Contains("запрос сохранён")),
                    "production list progress describes both read-only phases without promising a retained setup request");
                bool rootGone = Gone(root), childGone = Gone(child), unchanged = ListPrivateUnchanged(before);
                int calls = ListCalls(), currentStep = (int)Field(wizard, "step"), remainingWork = Directory.GetDirectories(HomeVpnPrivateFiles.Root, "admin-*").Length;
                check(observed && ticks >= 3 && copies == 1 && calls == 1 && rootGone && childGone
                    && wizard.Visible && currentStep == 4 && unchanged && remainingWork == 0,
                    "initial Friends cancellation/deadline settles the real owned tree, preserves the wizard/access/journal, and never retries: " + route
                    + "; observed=" + observed + "; ticks=" + ticks + "; copies=" + copies + "; calls=" + calls
                    + "; rootGone=" + rootGone + "; childGone=" + childGone + "; visible=" + wizard.Visible
                    + "; step=" + currentStep + "; privateUnchanged=" + unchanged + "; remainingWork=" + remainingWork);
                check(result.Text.Contains(route == "deadline" ? "Время получения списка истекло" : "Получение списка отменено")
                    && !result.Text.Contains("запрос сохранён") && result.ForeColor == (route == "deadline" ? UiTheme.Error : UiTheme.Muted),
                    "initial list outcome distinguishes requested cancellation from its deadline without server-recovery claims");
                if (route == "button") {
                    Shot(wizard, work, "vps-list-first-cancelled"); ListMode("complete", false);
                    controller.Tick += delegate {
                        var friends = Application.OpenForms.OfType<HomeInvitationsForm>().FirstOrDefault();
                        try { if (friends != null) { controller.Stop(); friends.Close(); } }
                        catch (Exception error) { callbackFailure = error; controller.Stop(); }
                    }; controller.Start(); open.PerformClick(); Pump(() => !(bool)Field(wizard, "busy"));
                    if (callbackFailure != null) throw new Exception("List retry UI controller failed", callbackFailure);
                    check(copies == 2 && ListCalls() == 2 && ListPrivateUnchanged(before) && wizard.Visible,
                        "only an explicit retry of initial Friends opens the recovered list without reissuing access");
                }
                wizard.Close();
            }
        }
        private static void ListRefreshWaiting(ClipboardService clipboard, string work, string route)
        {
            ListMode("hold", true); int copies = 0, root = 0, child = 0, ticks = 0; bool observed = false, gated = route != "button";
            Exception callbackFailure = null;
            var first = new HomeVpnInvitation { Id = new string('a', 24), Name = "Друг" };
            var second = new HomeVpnInvitation { Id = new string('b', 24), Name = "Друг" };
            var owner = new HomeVpnOwner { Host = "vpn.example.org", Login = "root", Port = 22 };
            using (var friends = new HomeInvitationsForm(new[] { first, second }, (action, label, id) => {
                check(action == "list", "Friends Refresh dispatches only a read-only list");
                return HomeVpnService.AdminAsync(owner, action, label, id, delegate { }, (exe, args) => { copies++; return Task.FromResult(0); }, Application.ExecutablePath, 20000);
            }, clipboard))
            using (var heartbeat = new System.Windows.Forms.Timer { Interval = 20 })
            using (var controller = new System.Windows.Forms.Timer { Interval = 20 }) {
                friends.Show(); var view = (HomeInvitationList)Field(friends, "list"); var rows = (ListBox)Field(view, "list");
                var search = (TextBox)Field(view, "search"); var name = (TextBox)Field(friends, "name");
                var refresh = (Button)Field(friends, "refresh"); var create = (Button)Field(friends, "create");
                search.Text = "Друг"; rows.SelectedIndex = 0; name.Text = "Сохранённый черновик";
                if (gated) Call(friends, "RequireRefresh", "Нужно проверить прошлое изменение");
                var before = ListPrivateSnapshot(); heartbeat.Tick += delegate { ticks++; }; heartbeat.Start();
                controller.Tick += delegate {
                    var wait = Application.OpenForms.OfType<HomeVpnPreparationForm>().FirstOrDefault();
                    try {
                        if (wait == null || !File.Exists(Path.Combine(folder, "child")) || ticks < 3) return;
                        controller.Stop(); observed = true; root = Id("pid"); child = Id("child"); CancelList(wait, route);
                    } catch (Exception error) { callbackFailure = error; controller.Stop(); if (wait != null && !wait.IsDisposed) wait.Close(); }
                }; controller.Start(); refresh.PerformClick(); Pump(() => refresh.Enabled);
                if (callbackFailure != null) throw new Exception("List refresh UI controller failed", callbackFailure);
                check(observed && ticks >= 3 && copies == 1 && ListCalls() == 1 && Gone(root) && Gone(child) && friends.Visible
                    && rows.Items.Count == 2 && rows.Items[0] == first && rows.Items[1] == second && view.Selected == first
                    && search.Text == "Друг" && name.Text == "Сохранённый черновик" && (bool)Field(friends, "needsRefresh") == gated
                    && create.Enabled == !gated && ListPrivateUnchanged(before),
                    "cancelled Refresh preserves actual rows/selection/search/name/access and the existing mutation gate: " + route);
                check(((Label)Field(friends, "status")).Text.StartsWith("Получение списка отменено"), "Friends exposes a clear explicit-refresh retry after list cancellation");
                if (route == "button") Shot(friends, work, "vps-list-refresh-cancelled");
                ListMode("complete", false); refresh.PerformClick(); Pump(() => refresh.Enabled);
                check(copies == 2 && ListCalls() == 2 && create.Enabled && !(bool)Field(friends, "needsRefresh") && view.Selected == null
                    && rows.Items.Count == 2 && name.Text == "Сохранённый черновик" && ListPrivateUnchanged(before),
                    "one explicit successful Refresh clears reconciliation without choosing/reissuing an identity or losing draft");
                friends.Close();
            }
        }
        private static void ProcessChecks()
        {
            Reset(); using (var source = new CancellationTokenSource()) {
                source.Cancel(); bool cancelled = false;
                try { HomeVpnPreparationProcess.Run(Application.ExecutablePath, Args("hold"), 1000, source.Token); } catch (OperationCanceledException) { cancelled = true; }
                check(cancelled && !File.Exists(Path.Combine(folder, "pid")), "pre-cancelled preparation creates no process");
            }
            Reset(); var timeout = Task.Run(() => HomeVpnPreparationProcess.Run(Application.ExecutablePath, Args("hold"), 1500, CancellationToken.None));
            Pump(() => File.Exists(Path.Combine(folder, "child"))); int root = Id("pid"), child = Id("child"); Pump(() => timeout.IsCompleted);
            check(timeout.IsFaulted && timeout.Exception.GetBaseException() is TimeoutException && Gone(root) && Gone(child), "preparation deadline kills its process tree and reports timeout instead of cancellation");
            Reset(); int code = HomeVpnPreparationProcess.Run(Application.ExecutablePath, Args("orphan"), 5000, CancellationToken.None);
            check(code == 0 && Gone(Id("pid")) && Gone(Id("child")), "normal preparation parent exit cannot leave a surviving descendant");
            Reset(); var fail = HomeVpnPreparationProcess.CopyAsync(Application.ExecutablePath, Args("fail"), 20000, CancellationToken.None); Pump(() => fail.IsCompleted);
            check(fail.IsFaulted && fail.Exception.GetBaseException() is IOException && fail.Exception.GetBaseException().Message.Contains("не запускались")
                && !fail.Exception.GetBaseException().Message.Contains(folder), "copy refusal reports a safe pre-configuration failure without private paths");
        }
        private static void WizardBoundary(string token, ClipboardService clipboard)
        {
            using (var relay = new Ikev2RelayService())
            using (var service = new HomeVpnService(relay))
            using (var form = new HomeVpnWizardForm(service, clipboard))
            using (var timer = new System.Windows.Forms.Timer { Interval = 30 }) {
                Reset(); service.UseToken(token, null);
                var accessBefore = File.ReadAllBytes(Path.Combine(HomeVpnPrivateFiles.Root, "access.dat")); int commands = 0, copies = 0;
                form.Admin = (owner, action, label, id, progress) => HomeVpnService.AdminAsync(owner, action, label, id, progress,
                    async (exe, args) => {
                        copies++; check(exe == "scp.exe" && args.Contains("ConnectTimeout=15") && args.Contains(" -P 22 "), "preparation retains existing SSH copy options");
                        using (var wait = new HomeVpnPreparationForm(ct => HomeVpnPreparationProcess.CopyAsync(Application.ExecutablePath, Args("hold"), 20000, ct))) {
                            wait.ShowDialog(form); await wait.Completion;
                        }
                    }, (exe, args, output) => { commands++; throw new Exception("Remote mutation must not start"); });
                typeof(HomeVpnWizardForm).GetField("own", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(form, true);
                Call(form, "ShowStep", 1); form.Show(); ((TextBox)Field(form, "host")).Text = "vpn.example.org";
                ((TextBox)Field(form, "login")).Text = "root"; ((TextBox)Field(form, "key")).Text = "";
                timer.Tick += delegate {
                    var wait = Application.OpenForms.OfType<HomeVpnPreparationForm>().FirstOrDefault();
                    if (wait != null && File.Exists(Path.Combine(folder, "child"))) { timer.Stop(); ((Button)Field(wait, "cancel")).PerformClick(); }
                }; timer.Start(); ((Button)Field(form, "next")).PerformClick(); Pump(() => !(bool)Field(form, "busy"));
                var status = (Label)Field(form, "status");
                check(copies == 1 && commands == 0 && (int)Field(form, "step") == 1 && Field(form, "preparedToken") == null,
                    "cancelled own-VPS preparation never launches setup, commits a token or advances the wizard");
                check(status.ForeColor == UiTheme.Muted && status.Text.StartsWith("Подготовка отменена") && ((TextBox)Field(form, "host")).Text == "vpn.example.org",
                    "wizard restores a retryable draft and explicit pre-configuration cancellation outcome");
                Shot(form, Path.GetDirectoryName(folder), "vps-preparation-cancelled");
                check(accessBefore.SequenceEqual(File.ReadAllBytes(Path.Combine(HomeVpnPrivateFiles.Root, "access.dat")))
                    && !Directory.GetDirectories(HomeVpnPrivateFiles.Root, "admin-*").Any(), "cancelled preparation preserves saved access and removes its private local work directory");
                var pending = new TaskCompletionSource<object>();
                form.Admin = (owner, action, label, id, progress) => HomeVpnService.AdminAsync(owner, action, label, id, progress,
                    (exe, args) => Task.FromResult(0), (exe, args, output) => { commands++; return pending.Task; });
                ((Button)Field(form, "next")).PerformClick(); form.Close();
                check(commands == 1 && form.Visible && (bool)Field(form, "busy") && !((Button)Field(form, "cancelWait")).Visible
                    && status.Text.Contains("не отменяет"), "successful preparation crosses into protected remote setup, where closing cannot claim rollback");
                pending.SetException(new IOException("Remote result not confirmed")); Pump(() => !(bool)Field(form, "busy")); form.Close();
            }
        }
        private static void CompletionRace()
        {
            var ready = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (var form = new HomeVpnPreparationForm(token => ready.Task)) {
                form.Show(); ((Button)Field(form, "cancel")).PerformClick(); ready.SetResult(null);
                Pump(() => form.Completion.IsCompleted);
                check(form.Completion.IsFaulted && form.Completion.Exception.GetBaseException() is HomeVpnPreparationCancelledException,
                    "accepted cancellation wins over a queued successful copy completion before remote handoff");
            }
            bool spawned = false; var neverShown = new HomeVpnPreparationForm(token => { spawned = true; return Task.FromResult(0); });
            neverShown.Dispose();
            check(!spawned && neverShown.Completion.IsFaulted && neverShown.Completion.Exception.GetBaseException() is HomeVpnPreparationCancelledException,
                "disposing a never-shown preparation window releases its source without starting work");
            var settling = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (var modal = new HomeVpnPreparationForm(token => settling.Task))
            using (var timer = new System.Windows.Forms.Timer { Interval = 50 }) {
                bool requested = false;
                timer.Tick += delegate {
                    if (!requested) {
                        requested = true; ((Button)Field(modal, "cancel")).PerformClick();
                        check(modal.Visible && !modal.Completion.IsCompleted, "modal cancel button leaves the waiting window open until settlement");
                    } else {
                        timer.Stop(); check(modal.Visible, "modal preparation cannot close through an implicit DialogResult while worker settles");
                        settling.SetCanceled();
                    }
                };
                timer.Start(); modal.ShowDialog();
                check(modal.Completion.IsFaulted && modal.Completion.Exception.GetBaseException() is HomeVpnPreparationCancelledException,
                    "modal cancellation returns its confirmed preparation outcome after settlement");
            }
        }
        private static void CleanupFailure()
        {
            FileStream held = null; string work = null;
            var owner = new HomeVpnOwner { Host = "vpn.example.org", Login = "root", Port = 22 };
            var task = HomeVpnService.AdminAsync(owner, "setup", null, null, delegate { },
                (exe, args) => {
                    work = Directory.GetDirectories(HomeVpnPrivateFiles.Root, "admin-*").Single();
                    held = File.Open(Directory.GetFiles(work, "home_vpn_setup.py", SearchOption.AllDirectories).Single(), FileMode.Open, FileAccess.Read, FileShare.None);
                    throw new HomeVpnPreparationCancelledException();
                }, (exe, args, output) => { throw new Exception("Remote command must not run"); });
            try {
                Pump(() => task.IsCompleted);
                check(task.IsFaulted && task.Exception.GetBaseException() is IOException && task.Exception.GetBaseException().Message.Contains("не удалось удалить"),
                    "locked preparation work refuses a successful cancellation claim and explains unconfirmed local cleanup");
            } finally { if (held != null) held.Dispose(); if (work != null) Directory.Delete(work, true); }
        }
        [DllImport("kernel32.dll")] private static extern uint GetConsoleCP();
    }
}

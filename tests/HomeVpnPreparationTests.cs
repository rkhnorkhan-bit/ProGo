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
            if (args.Length < 3 || (args[0] != "prepare-fixture" && args[0] != "prepare-child")) return false;
            string path = args[1], mode = args[2];
            if (args[0] == "prepare-child") {
                File.WriteAllText(Path.Combine(path, "child"), Process.GetCurrentProcess().Id.ToString()); Thread.Sleep(60000); return true;
            }
            File.WriteAllText(Path.Combine(path, "pid"), Process.GetCurrentProcess().Id.ToString());
            File.WriteAllText(Path.Combine(path, "console"), GetConsoleCP().ToString());
            if (mode == "complete") return true;
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
            foreach (string name in new[] { "access", "owner", "home-address" }) {
                string path = Path.Combine(HomeVpnPrivateFiles.Root, name + ".dat"); previous[path] = File.Exists(path) ? File.ReadAllBytes(path) : null;
            }
            string helpers = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scripts", "home-vpn", "server"); Directory.CreateDirectory(helpers);
            foreach (var file in new[] { "home_vpn_setup.py", "ikev2_relay.py", "install-ikev2-relay.sh", "profile_share_setup.py", "profile_share.py", "qrcodegen.py", "QR_LICENSE.txt" }) {
                string target = Path.Combine(helpers, file); previous[target] = File.Exists(target) ? File.ReadAllBytes(target) : null;
                File.Copy(Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Application.ExecutablePath)), "server", file), target, true);
            }
            var unrelated = Process.Start(new ProcessStartInfo(Application.ExecutablePath, "unrelated-wait") { UseShellExecute = false, CreateNoWindow = true });
            try {
                foreach (string route in new[] { "button", "escape", "close", "dispose" }) NativeCancel(route, work);
                ProcessChecks(); CompletionRace(); WizardBoundary(token, clipboard); CleanupFailure();
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace ProGo
{
    // Isolated client consoles and real DPAPI/native UI. No live SSH/VPS access.
    internal static class HomeVpnSetupWaitTests
    {
        private static Action<bool, string> check;
        private static string folder;
        private static readonly JavaScriptSerializer Json = new JavaScriptSerializer();
        private static HomeVpnOwner Owner { get { return new HomeVpnOwner { Host = "vpn.example.org", Login = "root", Port = 22, KeyFile = "" }; } }
        private static string PendingPath { get { return Path.Combine(HomeVpnPrivateFiles.Root, HomeVpnSetupRecovery.StorageName + ".dat"); } }
        private static HomeVpnSetupRequest Pending() { return HomeVpnSetupRecovery.Load(Owner, "My iPhone"); }
        internal static bool Fixture(string[] args)
        {
            if (args.Length != 3 || args[0] != "setup-wait-fixture") return false;
            string path = args[1], mode = args[2];
            HomeVpnPreparationTests.PublishMarker(Path.Combine(path, "pid"), Process.GetCurrentProcess().Id.ToString());
            HomeVpnPreparationTests.PublishMarker(Path.Combine(path, "console"), GetConsoleCP().ToString());
            Console.WriteLine("PRIVATE-RESULT"); Console.Out.Flush(); Console.Error.WriteLine("Isolated SSH progress");
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
        // Called inside the recovery harness's real, explicitly owned UI loop.
        internal static void Run(Action<bool, string> assert, string token, ClipboardService clipboard, string work)
        {
            check = assert; folder = Path.Combine(work, "vps-setup-wait"); Directory.CreateDirectory(folder);
            check(Application.MessageLoop && SynchronizationContext.Current is WindowsFormsSynchronizationContext
                && Control.CheckForIllegalCrossThreadCalls, "setup wait uses the live UI loop with cross-thread checks enabled");
            var saved = new Dictionary<string, byte[]>();
            foreach (string name in new[] { "access", "owner", "home-address", HomeVpnSetupRecovery.StorageName }) {
                string path = Path.Combine(HomeVpnPrivateFiles.Root, name + ".dat"); saved[path] = File.Exists(path) ? File.ReadAllBytes(path) : null;
            }
            File.Delete(PendingPath);
            var unrelated = Process.Start(new ProcessStartInfo(Application.ExecutablePath, "unrelated-wait") { UseShellExecute = false, CreateNoWindow = true });
            try {
                foreach (string route in new[] { "button", "escape", "close", "dispose" }) NativeStop(route, work);
                ProcessChecks(); CompletionRaces(); AdminBoundaries(token); WizardRetry(token, clipboard, work); CleanupFailure();
                check(!unrelated.HasExited, "SSH wait cancellation, deadlines and completion leave unrelated processes alive");
            } finally {
                Reset(); if (!unrelated.HasExited) { unrelated.Kill(); unrelated.WaitForExit(2000); } unrelated.Dispose();
                foreach (var item in saved) { if (item.Value == null) File.Delete(item.Key); else File.WriteAllBytes(item.Key, item.Value); }
            }
        }
        private static object Field(object target, string name) { return target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target); }
        private static void Pump(Func<bool> ready, int timeout = 16000)
        {
            var watch = Stopwatch.StartNew();
            while (!ready()) { Application.DoEvents(); Thread.Sleep(10); if (watch.ElapsedMilliseconds > timeout) throw new Exception("Setup wait fixture timeout"); }
            Application.DoEvents();
        }
        private static Exception Failure(Task task)
        {
            Pump(() => task.IsCompleted);
            try { task.GetAwaiter().GetResult(); } catch (Exception ex) { return ex; }
            throw new Exception("Expected setup wait refusal");
        }
        private static string Args(string mode) { return "setup-wait-fixture " + HomeVpnService.Argument(folder) + " " + mode; }
        private static int Pid(string name) { return Int32.Parse(File.ReadAllText(Path.Combine(folder, name))); }
        private static bool Gone(int id) { try { using (var process = Process.GetProcessById(id)) return process.HasExited; } catch (ArgumentException) { return true; } }
        private static void Reset()
        {
            foreach (string name in new[] { "pid", "child" }) {
                if (!File.Exists(Path.Combine(folder, name))) continue;
                try { using (var process = Process.GetProcessById(Pid(name))) { if (!process.HasExited) { process.Kill(); process.WaitForExit(2000); } } } catch (ArgumentException) { }
            }
            foreach (string name in new[] { "pid", "child", "console", "output" }) File.Delete(Path.Combine(folder, name));
        }
        private static Task Command(string mode, string output, CancellationToken token, int timeout = 20000)
        {
            return HomeVpnSetupWaitProcess.CommandAsync(Application.ExecutablePath, Args(mode), output, timeout, token);
        }
        private static Task Command(string mode, CancellationToken token, int timeout = 20000) { return Command(mode, Path.Combine(folder, "output"), token, timeout); }
        private static void Shot(Form form, string work, string name)
        {
            using (var image = new Bitmap(form.Width, form.Height)) { form.DrawToBitmap(image, new Rectangle(Point.Empty, image.Size)); image.Save(Path.Combine(work, name + ".png")); }
        }
        private static void NativeStop(string route, string work)
        {
            Reset(); File.Delete(PendingPath); var request = HomeVpnSetupRecovery.Register(Owner, "My iPhone");
            using (var form = new HomeVpnSetupWaitForm(ct => Command("hold", ct)))
            using (var heartbeat = new System.Windows.Forms.Timer { Interval = 20 }) {
                int ticks = 0; heartbeat.Tick += delegate { ticks++; }; heartbeat.Start(); form.Show();
                Pump(() => File.Exists(Path.Combine(folder, "child")) && ticks >= 3);
                int root = Pid("pid"), child = Pid("child"); var cancel = (Button)Field(form, "cancel"); var status = (Label)Field(form, "status");
                check(Pid("console") != 0 && File.Exists(Path.Combine(folder, "output")),
                    "SSH wait preserves a prompt console and redirects the result only to its local file");
                check(form.AcceptButton == null && form.CancelButton == cancel && cancel.DialogResult == DialogResult.None
                    && cancel.AccessibilityObject.Name == "Остановить ожидание" && status.AccessibilityObject.Description == status.Text
                    && status.Text.Contains("10 минут") && status.Text.Contains("не подтверждает отмену"), "wait window has one accessible local-only cancellation action and an explicit deadline");
                cancel.Focus(); check(form.ActiveControl == cancel && ticks >= 3 && SynchronizationContext.Current is WindowsFormsSynchronizationContext,
                    "SSH waiting keeps the UI heartbeat and keyboard cancellation responsive");
                if (route == "button") {
                    Shot(form, work, "vps-setup-wait"); form.ClientSize = new Size(440, 270); Application.DoEvents();
                    var viewport = (FlowLayoutPanel)Field(form, "viewport"); var heading = (Label)Field(form, "heading");
                    check(form.RectangleToClient(cancel.RectangleToScreen(cancel.ClientRectangle)).Bottom <= form.ClientSize.Height
                        && !viewport.HorizontalScroll.Visible && heading.Bottom <= viewport.ClientSize.Height
                        && heading.GetPreferredSize(new Size(heading.Width, 0)).Height <= heading.Height
                        && status.GetPreferredSize(new Size(status.Width, 0)).Height <= status.Height,
                        "minimum SSH wait wraps all text without horizontal scrolling and retains the heading and action");
                    Shot(form, work, "vps-setup-wait-minimum");
                }
                var watch = Stopwatch.StartNew();
                if (route == "button") cancel.PerformClick();
                else if (route == "escape") typeof(Form).GetMethod("ProcessDialogKey", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(form, new object[] { Keys.Escape });
                else if (route == "close") form.Close(); else form.Dispose();
                if (route != "dispose") check(form.Visible && !cancel.Enabled && !form.Completion.IsCompleted, "SSH wait close/cancel stays open until its local process tree settles");
                var error = Failure(form.Completion);
                check(error is HomeVpnSetupWaitCancelledException && watch.ElapsedMilliseconds < 3500 && Gone(root) && Gone(child),
                    "confirmed wait cancellation stops the client and descendants before returning its distinct outcome");
                // PowerShell owns its stdout file while the client is running.
                // Production reads it only after waiting; cancellation may lose
                // buffered output, but must release the local file for cleanup.
                using (var output = File.Open(Path.Combine(folder, "output"), FileMode.Open, FileAccess.Read, FileShare.None))
                    check(output.CanRead, "confirmed wait cancellation releases the stdout file before result handling or cleanup");
                check(error.Message.Contains("могла завершиться или прерваться") && !error.Message.Contains("PRIVATE-RESULT")
                    && Pending().RequestId == request.RequestId && Pending().Result == null && !form.Visible,
                    "local cancellation keeps the request and makes no claim about server rollback or new access");
            }
        }
        private static void ProcessChecks()
        {
            Reset(); using (var source = new CancellationTokenSource()) {
                source.Cancel(); var error = Failure(Command("hold", source.Token));
                check(error is OperationCanceledException && !File.Exists(Path.Combine(folder, "pid")), "pre-cancelled SSH waiting creates no process");
            }
            Reset(); var task = Command("hold", CancellationToken.None, 4000); Pump(() => File.Exists(Path.Combine(folder, "child")));
            int root = Pid("pid"), child = Pid("child"); var failure = Failure(task);
            check(failure is HomeVpnSetupPendingException && failure.Message.Contains("Время ожидания") && Gone(root) && Gone(child)
                && Pending() != null, "SSH deadline settles its tree and offers original-request recovery instead of repeated setup");
            // Exercise root exit at the owned job boundary. Start-Process -Wait
            // intentionally waits for its client tree, so that wrapper is not
            // an exiting-root fixture while a client descendant is still alive.
            Reset(); var orphan = Task.Run(() => HomeVpnConsoleProcess.Run(Application.ExecutablePath, Args("orphan"), 5000,
                CancellationToken.None, "ProGo — isolated console", "Isolated deadline", "Isolated settlement failure"));
            Pump(() => orphan.IsCompleted);
            check(orphan.GetAwaiter().GetResult() == 0 && Gone(Pid("pid")) && Gone(Pid("child")), "normal owned console root completion cannot leave an orphan client descendant");
            Reset(); failure = Failure(Command("fail", CancellationToken.None));
            check(failure is HomeVpnSetupPendingException && !failure.Message.Contains("PRIVATE-RESULT") && !failure.Message.Contains(folder),
                "nonzero SSH result uses fixed recovery guidance without result text or private paths");
            Reset(); task = Command("complete", CancellationToken.None); Pump(() => task.IsCompleted); task.GetAwaiter().GetResult();
            check(File.ReadAllText(Path.Combine(folder, "output")).Trim() == "PRIVATE-RESULT" && Gone(Pid("pid")), "successful owned SSH command retains its exact stdout result");
        }
        private static void CompletionRaces()
        {
            var ready = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (var form = new HomeVpnSetupWaitForm(ct => ready.Task)) {
                form.Show(); ((Button)Field(form, "cancel")).PerformClick(); ready.SetResult(null);
                check(Failure(form.Completion) is HomeVpnSetupWaitCancelledException, "accepted wait cancellation wins over a queued successful response");
            }
            bool spawned = false; var unseen = new HomeVpnSetupWaitForm(ct => { spawned = true; return Task.FromResult(0); }); unseen.Dispose();
            check(!spawned && Failure(unseen.Completion) is HomeVpnSetupWaitCancelledException, "disposing a never-shown wait window starts no SSH work");
            var settling = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (var form = new HomeVpnSetupWaitForm(ct => settling.Task))
            using (var timer = new System.Windows.Forms.Timer { Interval = 50 }) {
                bool stopped = false;
                timer.Tick += delegate {
                    if (!stopped) { stopped = true; ((Button)Field(form, "cancel")).PerformClick(); check(form.Visible && !form.Completion.IsCompleted, "modal wait cancellation retains the window during settlement"); }
                    else { timer.Stop(); check(form.Visible, "modal wait does not escape through DialogResult before settlement"); settling.SetCanceled(); }
                };
                timer.Start(); form.ShowDialog(); check(Failure(form.Completion) is HomeVpnSetupWaitCancelledException, "modal wait reports cancellation only after settlement");
            }
            using (var form = new HomeVpnSetupWaitForm(ct => Task.FromResult(0))) {
                form.Show(); Pump(() => form.Completion.IsCompleted); form.Completion.GetAwaiter().GetResult(); check(!form.Visible, "normal wait completion closes without an implicit cancellation");
            }
        }
        private static Dictionary<string, object> Status(HomeVpnSetupRequest request)
        {
            return new Dictionary<string, object> { { "Version", 1 }, { "RequestId", request.RequestId }, { "Action", "setup" },
                { "State", "succeeded" }, { "Started", "2026-01-01T00:00:00+00:00" }, { "Finished", "2026-01-01T00:00:01+00:00" }, { "ResultAvailable", true } };
        }
        private static Task Write(string path, string text) { File.WriteAllText(path, text, new UTF8Encoding(false)); return Task.FromResult(0); }
        private static void AdminBoundaries(string token)
        {
            using (var relay = new Ikev2RelayService())
            using (var service = new HomeVpnService(relay)) {
                service.UseToken(token, null); byte[] before = File.ReadAllBytes(Path.Combine(HomeVpnPrivateFiles.Root, "access.dat"));
                foreach (string stopAt in new[] { "setup", "status", "result" }) {
                    File.Delete(PendingPath); HomeVpnSetupRequest request = stopAt == "setup" ? null : HomeVpnSetupRecovery.Register(Owner, "My iPhone");
                    int commands = 0, copies = 0;
                    var task = HomeVpnService.AdminAsync(Owner, stopAt == "setup" ? "setup" : "recover-setup", "My iPhone", null, delegate { },
                        (exe, args) => { copies++; return Task.FromResult(0); }, (exe, args, output) => {
                            commands++; request = Pending(); check(request != null && args.Contains("--request-id " + request.RequestId), "every interruptible command has its saved original request before transport");
                            if (stopAt != "setup") check(!args.Contains("setup --host"), "recovery wait never dispatches a setup mutation");
                            if (stopAt == "result" && commands == 1) return Write(output, Json.Serialize(Status(request)));
                            return Task.FromException(new HomeVpnSetupWaitCancelledException());
                        });
                    check(Failure(task) is HomeVpnSetupWaitCancelledException && commands == (stopAt == "result" ? 2 : 1) && copies == 1
                        && Pending().RequestId == request.RequestId && Pending().Result == null, "setup/status/result cancellation stops the command chain and retains the same pending ID");
                    check(before.SequenceEqual(File.ReadAllBytes(Path.Combine(HomeVpnPrivateFiles.Root, "access.dat"))) && !Directory.GetDirectories(HomeVpnPrivateFiles.Root, "admin-*").Any(),
                        "uncertain SSH cancellation leaves prior access unchanged and removes private local work");
                }
                string original = Pending().RequestId; int queries = 0;
                var recovered = HomeVpnService.AdminAsync(Owner, "recover-setup", "My iPhone", null, delegate { }, (exe, args) => Task.FromResult(0),
                    (exe, args, output) => { queries++; check(args.Contains("--request-id " + original) && !args.Contains("setup --host"), "retry queries the original request after wait cancellation"); return Write(output, queries == 1 ? Json.Serialize(Status(Pending())) : token); });
                Pump(() => recovered.IsCompleted); check(recovered.GetAwaiter().GetResult() == token && queries == 2 && Pending().Result == token,
                    "an explicit recovery after cancellation returns the original validated result without new setup");
            }
        }
        private static void WizardRetry(string token, ClipboardService clipboard, string work)
        {
            Reset(); File.Delete(PendingPath);
            using (var relay = new Ikev2RelayService())
            using (var service = new HomeVpnService(relay)) {
                service.UseToken(token, null); byte[] before = File.ReadAllBytes(Path.Combine(HomeVpnPrivateFiles.Root, "access.dat"));
                using (var wizard = new HomeVpnWizardForm(service, clipboard))
                using (var timer = new System.Windows.Forms.Timer { Interval = 25 }) {
                    typeof(HomeVpnWizardForm).GetField("own", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(wizard, true);
                    typeof(HomeVpnWizardForm).GetMethod("ShowStep", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(wizard, new object[] { 1 });
                    wizard.Show(); ((TextBox)Field(wizard, "host")).Text = Owner.Host; ((TextBox)Field(wizard, "login")).Text = Owner.Login;
                    ((TextBox)Field(wizard, "key")).Text = ""; int issued = 0;
                    wizard.Admin = (owner, action, label, id, progress) => HomeVpnService.AdminAsync(owner, action, label, id, progress,
                        (exe, args) => Task.FromResult(0), async (exe, args, output) => {
                            issued++; check(action == "setup" && Pending().Result == null, "wizard registers setup before entering its owned SSH wait");
                            using (var wait = new HomeVpnSetupWaitForm(ct => Command("hold", output, ct))) { wait.ShowDialog(wizard); await wait.Completion; }
                        });
                    timer.Tick += delegate {
                        var wait = Application.OpenForms.OfType<HomeVpnSetupWaitForm>().FirstOrDefault();
                        if (wait != null && File.Exists(Path.Combine(folder, "child"))) { timer.Stop(); ((Button)Field(wait, "cancel")).PerformClick(); }
                    };
                    timer.Start(); ((Button)Field(wizard, "next")).PerformClick(); Pump(() => !(bool)Field(wizard, "busy"));
                    string original = Pending().RequestId; var next = (Button)Field(wizard, "next"); var status = (Label)Field(wizard, "status");
                    check(issued == 1 && (int)Field(wizard, "step") == 1 && Field(wizard, "preparedToken") == null && status.ForeColor == UiTheme.Muted
                        && status.Text.StartsWith("Ожидание SSH остановлено") && next.Text == "Проверить прошлую настройку" && next.AccessibilityObject.Name == next.Text,
                        "wizard stays on setup with one recovery action and an explicit local-only cancellation outcome");
                    check(!wizard.InvokeRequired && SynchronizationContext.Current is WindowsFormsSynchronizationContext
                        && before.SequenceEqual(File.ReadAllBytes(Path.Combine(HomeVpnPrivateFiles.Root, "access.dat"))), "cancelled wizard continuation stays on the UI thread and preserves existing access");
                    Shot(wizard, work, "vps-setup-wait-cancelled"); wizard.ClientSize = new Size(684, 581); Application.DoEvents(); Shot(wizard, work, "vps-setup-wait-cancelled-minimum");
                    wizard.Close();
                    using (var reopened = new HomeVpnWizardForm(service, clipboard)) {
                        reopened.Show(); check(((TextBox)Field(reopened, "host")).Text == Owner.Host && ((Button)Field(reopened, "next")).Text == "Проверить прошлую настройку",
                            "reopening after wait cancellation restores the original endpoint and recovery action");
                        int queries = 0; reopened.Admin = (owner, action, label, id, progress) => HomeVpnService.AdminAsync(owner, action, label, id, progress,
                            (exe, args) => Task.FromResult(0), (exe, args, output) => {
                                queries++; check(action == "recover-setup" && args.Contains("--request-id " + original) && !args.Contains("setup --host"), "reopened wizard cannot turn wait cancellation into repeated setup");
                                return Task.FromException(new HomeVpnSetupWaitCancelledException());
                            });
                        ((Button)Field(reopened, "next")).PerformClick(); Pump(() => !(bool)Field(reopened, "busy"));
                        check(queries == 1 && Pending().RequestId == original && Field(reopened, "preparedToken") == null, "cancelling a repeated status query keeps the original request for a later check"); reopened.Close();
                    }
                }
            }
        }
        private static void CleanupFailure()
        {
            File.Delete(PendingPath); FileStream held = null; string directory = null;
            var task = HomeVpnService.AdminAsync(Owner, "setup", "My iPhone", null, delegate { }, (exe, args) => Task.FromResult(0),
                (exe, args, output) => { directory = Path.GetDirectoryName(output); held = File.Open(output, FileMode.CreateNew, FileAccess.Write, FileShare.None); throw new HomeVpnSetupWaitCancelledException(); });
            try {
                var error = Failure(task); check(error is HomeVpnSetupPendingException && error.Message.Contains("не удалось удалить") && Pending() != null,
                    "locked result cleanup refuses a confirmed cancellation outcome while retaining server recovery");
            } finally { if (held != null) held.Dispose(); if (directory != null) Directory.Delete(directory, true); }
        }
        [DllImport("kernel32.dll")] private static extern uint GetConsoleCP();
    }
}

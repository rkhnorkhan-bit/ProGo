using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProGo
{
    internal static class HomeVpnWaitingTests
    {
        private static string marker;
        private static Action<bool, string> check;
        // This executable is also the owned SSH substitute, never a live SSH client.
        internal static bool Fixture(string[] args)
        {
            int index = Array.IndexOf(args, "-D");
            if (index < 0) return false;
            string folder = Environment.GetEnvironmentVariable("PROGO_HOME_WAIT_FIXTURE");
            if (String.IsNullOrEmpty(folder)) throw new Exception("Missing isolated fixture folder");
            File.WriteAllText(Path.Combine(folder, "pid"), Process.GetCurrentProcess().Id.ToString());
            string mode = File.ReadAllText(Path.Combine(folder, "mode"));
            if (mode == "no-listener") { Thread.Sleep(60000); return true; }
            int port = Int32.Parse(args[index + 1].Split(':').Last());
            var listener = new TcpListener(IPAddress.Loopback, port); listener.Start();
            while (true) {
                var client = listener.AcceptTcpClient();
                Task.Run(delegate {
                    using (client) {
                        try {
                            var stream = client.GetStream(); byte[] greeting = Read(stream, 3);
                            if (greeting[0] != 5) return;
                            stream.Write(new byte[] { 5, 0 }, 0, 2);
                            byte[] request = Read(stream, 10); // Readiness probe closes after greeting.
                            if (request[0] != 5) return;
                            File.WriteAllText(Path.Combine(folder, "receiver"), "waiting");
                            if (mode == "silent") { while (stream.ReadByte() >= 0) { } return; }
                            stream.Write(new byte[] { 5, 0, 0, 1, 127, 0, 0, 1, 0, 0 }, 0, 10);
                            var hello = Read(stream, 6); stream.Write(hello, 0, hello.Length);
                        } catch (IOException) { } catch (SocketException) { }
                    }
                });
            }
        }
        private static byte[] Read(Stream stream, int count)
        {
            var bytes = new byte[count]; int offset = 0;
            while (offset < count) { int n = stream.Read(bytes, offset, count - offset); if (n == 0) throw new IOException(); offset += n; }
            return bytes;
        }
        internal static void Run(Action<bool, string> assert, string token, ClipboardService clipboard, string work)
        {
            check = assert; marker = Path.Combine(work, "channel-wait"); Directory.CreateDirectory(marker);
            var previous = Environment.GetEnvironmentVariable("PROGO_HOME_WAIT_FIXTURE");
            var saved = new Dictionary<string, byte[]>();
            foreach (string name in new[] { "access", "owner", "home-address" }) {
                var path = Path.Combine(HomeVpnPrivateFiles.Root, name + ".dat");
                saved[path] = File.Exists(path) ? File.ReadAllBytes(path) : null;
            }
            Environment.SetEnvironmentVariable("PROGO_HOME_WAIT_FIXTURE", marker);
            try {
                foreach (string mode in new[] { "no-listener", "silent" }) {
                    foreach (string route in new[] { "button", "escape", "close" }) NativeCancel(token, clipboard, work, mode, route);
                }
                ImportCancel(token, clipboard);
                CleanupFailure(token, clipboard);
                ServiceChecks(token);
                ProtectedOperation(clipboard);
            }
            finally {
                KillFixture(); Environment.SetEnvironmentVariable("PROGO_HOME_WAIT_FIXTURE", previous);
                foreach (var item in saved) { if (item.Value == null) File.Delete(item.Key); else File.WriteAllBytes(item.Key, item.Value); }
            }
        }
        private static HomeVpnService Service(Ikev2RelayService relay)
        {
            return new HomeVpnService(relay, Application.ExecutablePath, UdpPort(), UdpPort(), 17878);
        }
        private static int UdpPort() { using (var socket = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0))) return ((IPEndPoint)socket.Client.LocalEndPoint).Port; }
        private static void Mode(string value)
        {
            KillFixture(); File.Delete(Path.Combine(marker, "pid")); File.Delete(Path.Combine(marker, "receiver"));
            File.WriteAllText(Path.Combine(marker, "mode"), value);
        }
        private static int Pid() { return Int32.Parse(File.ReadAllText(Path.Combine(marker, "pid"))); }
        private static bool Gone(int pid) { try { using (var p = Process.GetProcessById(pid)) return p.HasExited; } catch (ArgumentException) { return true; } }
        private static void KillFixture() { if (!File.Exists(Path.Combine(marker, "pid"))) return; try { using (var p = Process.GetProcessById(Pid())) { if (!p.HasExited) { p.Kill(); p.WaitForExit(2000); } } } catch (ArgumentException) { } }
        private static void Pump(Func<bool> ready, int timeout = 16000)
        {
            var watch = Stopwatch.StartNew();
            while (!ready()) { Application.DoEvents(); Thread.Sleep(10); if (watch.ElapsedMilliseconds > timeout) throw new Exception("Home wait fixture timeout"); }
            Application.DoEvents();
        }
        private static object Field(object target, string name) { return target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target); }
        private static void Invoke(object target, string name, params object[] args) { target.GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, args); }
        private static IEnumerable<Control> Controls(Control root) { foreach (Control c in root.Controls) { yield return c; foreach (var child in Controls(c)) yield return child; } }
        private static void Shot(Form form, string work, string name) { using (var image = new Bitmap(form.Width, form.Height)) { form.DrawToBitmap(image, new Rectangle(Point.Empty, image.Size)); image.Save(Path.Combine(work, name + ".png")); } }
        private static void Clean(HomeVpnService service, int pid)
        {
            check(Gone(pid) && !service.Relay.IsRunning, "cancelled home startup closes owned process and never publishes a relay");
            check(Field(service, "proxy") == null && Field(service, "sessionDirectory") == null, "confirmed cancellation releases proxy ownership and private temporary session");
            check(HomeVpnPrivateFiles.Load("access") != null && service.Access != null, "cancelled startup retains protected saved access");
        }
        private static void NativeCancel(string token, ClipboardService clipboard, string work, string mode, string route)
        {
            Mode(mode);
            using (var relay = new Ikev2RelayService())
            using (var service = Service(relay))
            using (var form = new HomeVpnWizardForm(service, clipboard))
            using (var heartbeat = new System.Windows.Forms.Timer { Interval = 20 }) {
                service.UseToken(token, null); Invoke(form, "ShowStep", 4); form.Show();
                var accessBefore = File.ReadAllBytes(Path.Combine(HomeVpnPrivateFiles.Root, "access.dat"));
                int ticks = 0; heartbeat.Tick += delegate { ticks++; }; heartbeat.Start();
                Controls(form).OfType<Button>().Single(b => b.Text == "Запустить канал").PerformClick();
                Pump(() => File.Exists(Path.Combine(marker, mode == "silent" ? "receiver" : "pid")) && ticks >= 3);
                int pid = Pid(); var cancel = (Button)Field(form, "cancelWait"); var status = (Label)Field(form, "status");
                check(cancel.Visible && cancel.Enabled && !(bool)((Control)Field(form, "body")).Enabled && !(bool)((Button)Field(form, "next")).Enabled,
                    "waiting startup offers scoped cancellation and blocks competing actions: " + mode + "/" + route);
                check(form.AcceptButton == null && form.CancelButton == cancel && status.Text.Contains("10 секунд")
                    && status.AccessibilityObject.Description == status.Text && cancel.AccessibilityObject.Name == "Отменить запуск канала",
                    "waiting startup is accessible, bounded and has no implicit Enter action");
                cancel.Focus(); check(form.ActiveControl == cancel, "cancel remains a reachable keyboard target outside disabled wizard body");
                if (mode == "silent" && route == "button") {
                    Shot(form, work, "home-wait-pending"); form.ClientSize = new Size(684, 581); Application.DoEvents();
                    check(form.RectangleToClient(cancel.RectangleToScreen(cancel.ClientRectangle)).Bottom <= form.ClientSize.Height,
                        "waiting cancellation remains visible at minimum size"); Shot(form, work, "home-wait-minimum");
                }
                var watch = Stopwatch.StartNew();
                if (route == "button") cancel.PerformClick();
                else if (route == "escape") typeof(Form).GetMethod("ProcessDialogKey", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(form, new object[] { Keys.Escape });
                else form.Close();
                Pump(() => !(bool)Field(form, "busy"));
                check(watch.ElapsedMilliseconds < 3500, "native cancellation settles without waiting for the network deadline"); Clean(service, pid);
                check(accessBefore.SequenceEqual(File.ReadAllBytes(Path.Combine(HomeVpnPrivateFiles.Root, "access.dat"))), "startup cancellation does not rewrite saved credentials");
                if (route == "close") check(form.IsDisposed, "title close waits for cleanup before closing");
                else {
                    check(form.Visible && !cancel.Visible && ((Control)Field(form, "body")).Enabled && status.Text.Contains("отменён") && status.ForeColor == UiTheme.Muted,
                        "button and Escape cancellation keep a retryable wizard with a cancellation outcome");
                    if (mode == "silent" && route == "button") Shot(form, work, "home-wait-cancelled");
                    Mode("ready"); Controls(form).OfType<Button>().Single(b => b.Text == "Запустить канал").PerformClick();
                    Pump(() => !(bool)Field(form, "busy"));
                    check(relay.IsRunning && status.Text.Contains("на телефоне отдельно"), "explicit retry starts a fresh channel without claiming phone internet"); service.Stop(); form.Close();
                }
            }
        }
        private static void ImportCancel(string token, ClipboardService clipboard)
        {
            Mode("silent");
            using (var relay = new Ikev2RelayService())
            using (var service = Service(relay))
            using (var form = new HomeVpnWizardForm(service, clipboard)) {
                form.Show(); Invoke(form, "ShowStep", 1);
                ((TextBox)Field(form, "token")).Text = token;
                ((Button)Field(form, "next")).PerformClick(); Pump(() => File.Exists(Path.Combine(marker, "receiver")));
                ((Button)Field(form, "cancelWait")).PerformClick(); Pump(() => !(bool)Field(form, "busy"));
                check((int)Field(form, "step") == 1 && ((TextBox)Field(form, "token")).Text == token && ((TextBox)Field(form, "token")).UseSystemPasswordChar,
                    "import cancellation preserves the masked draft and does not advance to router setup");
                check(HomeVpnPrivateFiles.Load("access") == token && ((Label)Field(form, "status")).Text.Contains("Сохранённый доступ"),
                    "import cancellation describes already saved access without a rollback claim"); form.Close();
            }
        }
        private static void ServiceChecks(string token)
        {
            Mode("silent");
            using (var relay = new Ikev2RelayService())
            using (var service = Service(relay)) {
                service.UseToken(token, null);
                using (var cts = new CancellationTokenSource()) {
                    cts.Cancel(); var task = service.StartAsync(cts.Token); Pump(() => task.IsCompleted);
                    check(task.IsCanceled && !File.Exists(Path.Combine(marker, "pid")), "pre-cancelled startup creates no process or private session");
                }
                var timeout = service.StartAsync(); Pump(() => timeout.IsCompleted, 15000);
                check(timeout.IsFaulted && !relay.IsRunning && Field(service, "proxy") == null && Field(service, "sessionDirectory") == null,
                    "silent receiver deadline reports failure and cleans startup ownership");
                check(relay.LastError != null, "deadline failure remains distinct from requested cancellation");
                Mode("ready"); var success = service.StartAsync(); Pump(() => success.IsCompleted); success.GetAwaiter().GetResult();
                using (var cts = new CancellationTokenSource()) {
                    cts.Cancel(); var task = service.StartAsync(cts.Token); Pump(() => task.IsCompleted);
                    check(task.IsCanceled && relay.IsRunning, "pre-cancelled request preserves an already running phone channel");
                }
                service.Stop();
            }
        }
        private static void CleanupFailure(string token, ClipboardService clipboard)
        {
            Mode("silent");
            using (var relay = new Ikev2RelayService())
            using (var service = Service(relay))
            using (var form = new HomeVpnWizardForm(service, clipboard)) {
                service.UseToken(token, null); Invoke(form, "ShowStep", 4); form.Show();
                Controls(form).OfType<Button>().Single(b => b.Text == "Запустить канал").PerformClick();
                Pump(() => File.Exists(Path.Combine(marker, "receiver")));
                string session = (string)Field(service, "sessionDirectory");
                using (var held = File.Open(Path.Combine(session, "ssh_config"), FileMode.Open, FileAccess.Read, FileShare.None)) {
                    form.Close(); Pump(() => !(bool)Field(form, "busy"));
                    var status = (Label)Field(form, "status");
                    check(form.Visible && !relay.IsRunning && Gone(Pid()) && status.ForeColor == UiTheme.Error && status.Text.Contains("отмена пока не подтверждена"),
                        "cleanup failure refuses successful cancellation and queued close while explaining the unsettled result");
                    check((string)Field(service, "sessionDirectory") == session && HomeVpnPrivateFiles.Load("access") == token,
                        "cleanup failure retains private session ownership and saved access for recovery");
                }
                service.Stop(); check(!Directory.Exists(session), "explicit stop retries private session removal after its lock is released"); form.Close();
            }
        }
        private static void ProtectedOperation(ClipboardService clipboard)
        {
            using (var relay = new Ikev2RelayService())
            using (var service = Service(relay))
            using (var form = new HomeVpnWizardForm(service, clipboard)) {
                var pending = new TaskCompletionSource<int>(); form.Show();
                var task = (Task)typeof(HomeVpnWizardForm).GetMethod("RunStep", BindingFlags.NonPublic | BindingFlags.Instance)
                    .Invoke(form, new object[] { new Func<Task>(() => pending.Task) });
                form.Close();
                check(form.Visible && !((Button)Field(form, "cancelWait")).Visible && ((Label)Field(form, "status")).Text.Contains("не отменяет") && !task.IsCompleted,
                    "server-changing operation cannot be cancelled or declared rolled back by title close");
                pending.SetResult(0); Pump(() => task.IsCompleted);
                check(form.Visible && !(bool)Field(form, "busy"), "protected operation completion restores controls without a queued close"); form.Close();
            }
        }
    }
}

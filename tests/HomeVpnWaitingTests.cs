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
            // An unrelated fixture outlives its owner's test scope, rather than
            // dying on an arbitrary wall clock during a still-bounded scenario.
            // Parent exit also closes this private pipe and releases the child.
            if (args.Length == 1 && args[0] == "unrelated-wait") { Console.In.ReadLine(); return true; }
            int index = Array.IndexOf(args, "-D");
            if (index < 0) return false;
            string folder = Environment.GetEnvironmentVariable("PROGO_HOME_WAIT_FIXTURE");
            if (String.IsNullOrEmpty(folder)) throw new Exception("Missing isolated fixture folder");
            HomeVpnFixtureFiles.Publish(Path.Combine(folder, "pid"), Process.GetCurrentProcess().Id.ToString());
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
            var unrelated = StartUnrelatedFixture();
            try {
                PublicationReadiness();
                foreach (string mode in new[] { "no-listener", "silent" }) {
                    foreach (string route in new[] { "button", "escape", "close", "dispose" }) NativeCancel(token, clipboard, work, mode, route);
                }
                ImportCancel(token, clipboard);
                CleanupFailure(token, clipboard);
                ServiceChecks(token);
                OwnedPreparationStop(token, false);
                OwnedPreparationStop(token, true);
                OwnedCleanupStop(token);
                foreach (string route in new[] { "stay", "close", "dispose" }) WizardOwnedStop(token, clipboard, route);
                RelayOwnedLifecycle(false);
                RelayOwnedLifecycle(true);
                ProtectedOperation(clipboard);
                check(!unrelated.HasExited, "all channel cancellation and cleanup paths preserve an unrelated process");
            }
            finally {
                try { KillFixture(); Environment.SetEnvironmentVariable("PROGO_HOME_WAIT_FIXTURE", previous); }
                finally {
                    try { StopUnrelatedFixture(unrelated); }
                    finally { foreach (var item in saved) { if (item.Value == null) File.Delete(item.Key); else File.WriteAllBytes(item.Key, item.Value); } }
                }
            }
        }
        internal static Process StartUnrelatedFixture()
        {
            return Process.Start(new ProcessStartInfo(Application.ExecutablePath, "unrelated-wait") {
                UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true });
        }
        internal static void StopUnrelatedFixture(Process process)
        {
            try {
                if (!process.HasExited) {
                    process.StandardInput.Close();
                    if (!process.WaitForExit(500)) {
                        process.Kill();
                        if (!process.WaitForExit(1500)) throw new TimeoutException("Owned unrelated fixture did not exit after pipe close or targeted cleanup");
                    }
                }
            } finally { process.Dispose(); }
        }
        private static HomeVpnService Service(Ikev2RelayService relay)
        {
            return new HomeVpnService(relay, Application.ExecutablePath, UdpPort(), UdpPort(), 17878);
        }
        private static int UdpPort() { using (var socket = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0))) return ((IPEndPoint)socket.Client.LocalEndPoint).Port; }
        private static void PublicationReadiness()
        {
            string path = Path.Combine(marker, "publication-probe"); File.Delete(path);
            using (var staged = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim()) {
                var writer = Task.Run(delegate {
                    HomeVpnFixtureFiles.Publish(path, Process.GetCurrentProcess().Id.ToString(), delegate { staged.Set(); release.Wait(); });
                });
                bool unpublished = false;
                try {
                    Pump(() => staged.IsSet || writer.IsCompleted);
                    unpublished = staged.IsSet && !writer.IsCompleted && !File.Exists(path);
                } finally { release.Set(); Pump(() => writer.IsCompleted); }
                writer.GetAwaiter().GetResult();
                check(unpublished, "fixture PID readiness stays unpublished while the staged writer is held open");
                check(Int32.Parse(File.ReadAllText(path)) == Process.GetCurrentProcess().Id
                    && Directory.GetFiles(marker, "publication-probe.*.tmp").Length == 0,
                    "fixture PID readiness publishes complete readable data only after its writer closes");
                staged.Reset(); release.Reset();
                int next = Process.GetCurrentProcess().Id + 1;
                writer = Task.Run(delegate {
                    HomeVpnFixtureFiles.Publish(path, next.ToString(), delegate { staged.Set(); release.Wait(); });
                });
                bool oldComplete = false;
                try {
                    Pump(() => staged.IsSet || writer.IsCompleted);
                    oldComplete = staged.IsSet && !writer.IsCompleted && Int32.Parse(File.ReadAllText(path)) == Process.GetCurrentProcess().Id;
                } finally { release.Set(); Pump(() => writer.IsCompleted); }
                writer.GetAwaiter().GetResult();
                check(oldComplete && Int32.Parse(File.ReadAllText(path)) == next
                    && Directory.GetFiles(marker, "publication-probe.*.tmp").Length == 0,
                    "fixture recovery phases atomically replace a previous complete PID without exposing a staged writer");
            }
            File.Delete(path);
        }
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
                else if (route == "close") form.Close();
                else form.Dispose();
                Pump(() => !(bool)Field(form, "busy"));
                check(watch.ElapsedMilliseconds < 3500, "native cancellation settles without waiting for the network deadline"); Clean(service, pid);
                check(accessBefore.SequenceEqual(File.ReadAllBytes(Path.Combine(HomeVpnPrivateFiles.Root, "access.dat"))), "startup cancellation does not rewrite saved credentials");
                if (route == "close" || route == "dispose") check(form.IsDisposed, "close or disposal finishes without late UI access");
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
                    int oldPid = Pid(); Controls(form).OfType<Button>().Single(b => b.Text == "Запустить канал").PerformClick();
                    Pump(() => !(bool)Field(form, "busy"));
                    check((string)Field(service, "sessionDirectory") == session && Pid() == oldPid && !relay.IsRunning && status.Text.Contains("ещё не очищен"),
                        "retry cannot discard an unsettled private session or spawn another process while its file stays locked");
                }
                service.Stop(); check(!Directory.Exists(session), "explicit stop retries private session removal after its lock is released"); form.Close();
            }
        }
        private static void OwnedPreparationStop(string token, bool dispose)
        {
            Mode("ready");
            using (var relay = new Ikev2RelayService())
            using (var service = Service(relay))
            using (var form = new Form())
            using (var heartbeat = new System.Windows.Forms.Timer { Interval = 20 })
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            {
                service.UseToken(token, null);
                var accessBefore = File.ReadAllBytes(Path.Combine(HomeVpnPrivateFiles.Root, "access.dat"));
                form.Show(); int ticks = 0; heartbeat.Tick += delegate { ticks++; }; heartbeat.Start();
                service.BeforeSessionIo = delegate(string stage) {
                    if (stage == "config") { entered.Set(); if (!release.Wait(5000)) throw new TimeoutException("Owned session preparation barrier timed out"); }
                };
                Task start = null; Task<bool> stop = null;
                try
                {
                    var watch = Stopwatch.StartNew(); start = service.StartAsync();
                    check(watch.ElapsedMilliseconds < 500 && Object.ReferenceEquals(start, service.StartWork),
                        "phone start publishes its tracked worker without synchronous private I/O");
                    Pump(() => entered.IsSet && ticks >= 3, 3000);
                    string session = (string)Field(service, "sessionDirectory");
                    check(!start.IsCompleted && File.Exists(Path.Combine(session, "access"))
                        && !File.Exists(Path.Combine(session, "ssh_config")) && !File.Exists(Path.Combine(marker, "pid")),
                        "held real session preparation retains its private files before any native child");
                    check(Object.ReferenceEquals(start, service.StartAsync()), "duplicate phone start observes the same owned preparation task");
                    watch.Restart();
                    if (dispose) { service.Dispose(); stop = service.StopWork; } else stop = service.StopAsync();
                    check(watch.ElapsedMilliseconds < 500 && Object.ReferenceEquals(stop, service.StopAsync()),
                        "phone Stop or Dispose returns promptly and retains one shared cleanup task: " + dispose);
                    int before = ticks; Pump(() => ticks >= before + 3, 1000);
                    var refused = service.StartAsync(); bool blocked = false;
                    try { refused.GetAwaiter().GetResult(); } catch (ObjectDisposedException) { blocked = dispose; } catch (InvalidOperationException) { blocked = true; }
                    check(blocked && !stop.IsCompleted && !start.IsCompleted && Directory.Exists(session),
                        "pending phone stop preserves session ownership and refuses a competing start: " + dispose);
                    release.Set(); Pump(() => start.IsCompleted && stop.IsCompleted, 8000);
                    check(start.IsCanceled && stop.GetAwaiter().GetResult() && !Directory.Exists(session)
                        && Field(service, "proxy") == null && Field(service, "sessionDirectory") == null && !relay.IsRunning
                        && !File.Exists(Path.Combine(marker, "pid")),
                        "cancelled preparation settles before cleanup without late config or native launch: " + dispose);
                    check(accessBefore.SequenceEqual(File.ReadAllBytes(Path.Combine(HomeVpnPrivateFiles.Root, "access.dat"))),
                        "async phone cancellation preserves the exact saved DPAPI access: " + dispose);
                    if (!dispose)
                    {
                        service.BeforeSessionIo = null;
                        var retry = service.StartAsync(); Pump(() => retry.IsCompleted, 8000); retry.GetAwaiter().GetResult();
                        check(relay.IsRunning && !Object.ReferenceEquals(retry, start), "phone retry admits one fresh operation only after the cancelled worker settled");
                        int pid = Pid(); stop = service.StopAsync(); Pump(() => stop.IsCompleted, 5000);
                        check(stop.GetAwaiter().GetResult() && Gone(pid), "async phone stop confirms its own native child exit before success");
                    }
                }
                finally
                {
                    release.Set(); service.BeforeSessionIo = null;
                    try { var cleanup = service.StopAsync(); Pump(() => cleanup.IsCompleted, 8000); if (!cleanup.GetAwaiter().GetResult()) throw new Exception("Owned phone fixture cleanup incomplete"); }
                    finally { heartbeat.Stop(); form.Close(); }
                }
            }
        }

        private static void OwnedCleanupStop(string token)
        {
            Mode("ready");
            using (var relay = new Ikev2RelayService())
            using (var service = Service(relay))
            using (var form = new Form())
            using (var heartbeat = new System.Windows.Forms.Timer { Interval = 20 })
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            {
                service.UseToken(token, null); form.Show();
                int ticks = 0; heartbeat.Tick += delegate { ticks++; }; heartbeat.Start();
                try
                {
                    var start = service.StartAsync(); Pump(() => start.IsCompleted, 8000); start.GetAwaiter().GetResult();
                    int pid = Pid(); string session = (string)Field(service, "sessionDirectory");
                    service.BeforeSessionIo = delegate(string stage) {
                        if (stage == "cleanup") { entered.Set(); if (!release.Wait(5000)) throw new TimeoutException("Owned session cleanup barrier timed out"); }
                    };
                    var watch = Stopwatch.StartNew(); var stop = service.StopAsync();
                    check(watch.ElapsedMilliseconds < 500, "phone cleanup queues native disposal and directory removal outside the UI thread");
                    Pump(() => entered.IsSet, 3000); int before = ticks; Pump(() => ticks >= before + 3, 1000);
                    check(!stop.IsCompleted && Object.ReferenceEquals(stop, service.StopAsync()) && Gone(pid)
                        && !relay.IsRunning && Directory.Exists(session),
                        "phone cleanup heartbeat continues while its exact private session remains owned after child exit");
                    release.Set(); Pump(() => stop.IsCompleted, 5000);
                    check(stop.GetAwaiter().GetResult() && !Directory.Exists(session), "phone cleanup success follows actual directory removal");
                    service.BeforeSessionIo = null;
                    start = service.StartAsync(); Pump(() => start.IsCompleted, 8000); start.GetAwaiter().GetResult();
                    session = (string)Field(service, "sessionDirectory"); pid = Pid();
                    using (var held = File.Open(Path.Combine(session, "ssh_config"), FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        stop = service.StopAsync(); Pump(() => stop.IsCompleted, 5000);
                        check(!stop.GetAwaiter().GetResult() && Gone(pid) && Directory.Exists(session)
                            && (string)Field(service, "sessionDirectory") == session,
                            "real locked session makes async phone stop fail honestly while retaining its exact cleanup owner");
                    }
                    stop = service.StopAsync(); Pump(() => stop.IsCompleted, 5000);
                    check(stop.GetAwaiter().GetResult() && !Directory.Exists(session), "explicit phone stop retry removes the previously locked owned session");
                }
                finally
                {
                    release.Set(); service.BeforeSessionIo = null;
                    try { var cleanup = service.StopAsync(); Pump(() => cleanup.IsCompleted, 8000); if (!cleanup.GetAwaiter().GetResult()) throw new Exception("Owned phone fixture cleanup incomplete"); }
                    finally { heartbeat.Stop(); form.Close(); }
                }
            }
        }

        private static void WizardOwnedStop(string token, ClipboardService clipboard, string route)
        {
            Mode("ready");
            using (var relay = new Ikev2RelayService())
            using (var service = Service(relay))
            using (var form = new HomeVpnWizardForm(service, clipboard))
            using (var heartbeat = new System.Windows.Forms.Timer { Interval = 20 })
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            {
                service.UseToken(token, null); Invoke(form, "ShowStep", 4); form.Show();
                int ticks = 0; heartbeat.Tick += delegate { ticks++; }; heartbeat.Start();
                try
                {
                    var start = service.StartAsync(); Pump(() => start.IsCompleted, 8000); start.GetAwaiter().GetResult();
                    int pid = Pid(); string session = (string)Field(service, "sessionDirectory");
                    service.BeforeSessionIo = delegate(string stage) {
                        if (stage == "cleanup") { entered.Set(); if (!release.Wait(5000)) throw new TimeoutException("Wizard phone cleanup barrier timed out"); }
                    };
                    var stopButton = Controls(form).OfType<Button>().Single(b => b.Text == "Остановить VPN для телефона");
                    var watch = Stopwatch.StartNew(); stopButton.PerformClick();
                    check(watch.ElapsedMilliseconds < 500, "actual wizard phone Stop click returns while native cleanup remains owned: " + route);
                    Pump(() => entered.IsSet, 3000); int before = ticks; Pump(() => ticks >= before + 3, 1000);
                    var status = (Label)Field(form, "status");
                    check((bool)Field(form, "busy") && !((Control)Field(form, "body")).Enabled && !service.StopWork.IsCompleted
                        && Gone(pid) && Directory.Exists(session) && status.Text.Contains("Дождитесь"),
                        "actual phone Stop keeps responsive UI and honest pending cleanup without duplicate actions: " + route);
                    if (route == "close")
                    {
                        form.Close(); check(form.Visible && !form.IsDisposed && status.Text.Contains("подтверждения остановки"),
                            "phone Stop title close waits for local cleanup rather than claiming completion or cancelling it");
                    }
                    else if (route == "dispose") form.Dispose();
                    release.Set(); Pump(() => service.StopWork.IsCompleted && !(bool)Field(form, "busy"), 8000);
                    check(service.StopWork.GetAwaiter().GetResult() && !Directory.Exists(session) && !service.HasOwnedResources,
                        "wizard close or disposal cannot abandon its actual phone cleanup: " + route);
                    if (route != "stay") check(form.IsDisposed, "queued phone close and forced disposal settle without late UI access: " + route);
                    else
                    {
                        check(form.Visible && ((Control)Field(form, "body")).Enabled && status.Text.Contains("временные файлы очищены"),
                            "completed phone Stop restores explicit retry and reports success only after cleanup");
                        service.BeforeSessionIo = null;
                        start = service.StartAsync(); Pump(() => start.IsCompleted, 8000); start.GetAwaiter().GetResult();
                        session = (string)Field(service, "sessionDirectory");
                        using (var held = File.Open(Path.Combine(session, "ssh_config"), FileMode.Open, FileAccess.Read, FileShare.None))
                        {
                            stopButton.PerformClick(); Pump(() => !(bool)Field(form, "busy"), 5000);
                            check(form.Visible && status.ForeColor == UiTheme.Error && status.Text.Contains("остановлен не полностью")
                                && Directory.Exists(session) && ((Control)Field(form, "body")).Enabled,
                                "actual phone Stop shows locked-file failure and restores explicit retry without claiming cleanup");
                        }
                        stopButton.PerformClick(); Pump(() => !(bool)Field(form, "busy"), 5000);
                        check(!Directory.Exists(session) && status.Text.Contains("временные файлы очищены"),
                            "actual wizard retry cleans the same previously locked phone session");
                    }
                }
                finally
                {
                    release.Set(); service.BeforeSessionIo = null;
                    try { var cleanup = service.StopAsync(); Pump(() => cleanup.IsCompleted, 8000); if (!cleanup.GetAwaiter().GetResult()) throw new Exception("Wizard phone fixture cleanup incomplete"); }
                    finally { heartbeat.Stop(); form.Dispose(); }
                }
            }
        }

        private static void RelayOwnedLifecycle(bool dispose)
        {
            using (var relay = new Ikev2RelayService())
            using (var form = new Form())
            using (var pulse = new System.Windows.Forms.Timer { Interval = 20 })
            using (var probeEntered = new ManualResetEventSlim())
            using (var stopping = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            {
                var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start();
                TcpClient accepted = null;
                var peer = Task.Run(delegate {
                    using (var client = listener.AcceptTcpClient()) {
                        Interlocked.Exchange(ref accepted, client); client.ReceiveTimeout = 4000;
                        var stream = client.GetStream(); var hello = Read(stream, 3);
                        if (hello[0] != 5 || hello[1] != 1 || hello[2] != 0) throw new IOException("Unexpected owned probe greeting");
                        probeEntered.Set();
                        if (stream.ReadByte() != -1) throw new IOException("Cancelled probe sent unexpected data");
                    }
                });
                int socksPort = ((IPEndPoint)listener.LocalEndpoint).Port, ikePort = UdpPort(), natPort = UdpPort();
                while (natPort == ikePort) natPort = UdpPort();
                int ticks = 0; pulse.Tick += delegate { ticks++; }; form.Show(); pulse.Start();
                relay.BeforeStopComplete = delegate { stopping.Set(); if (!release.Wait(5000)) throw new TimeoutException("Relay stop finalization barrier"); };
                try
                {
                    var start = relay.StartAsync("127.0.0.1", socksPort, ikePort, natPort, 17878, CancellationToken.None);
                    check(Object.ReferenceEquals(start, relay.StartAsync("127.0.0.1", socksPort, ikePort, natPort, 17878, CancellationToken.None)),
                        "duplicate pending relay startup shares its actual native probe task");
                    Pump(() => probeEntered.IsSet && ticks >= 3, 3000);
                    var watch = Stopwatch.StartNew();
                    if (dispose) relay.Dispose(); else relay.StopAsync();
                    var stop = relay.StopWork;
                    check(watch.ElapsedMilliseconds < 500 && Object.ReferenceEquals(stop, relay.StopAsync()),
                        "relay Stop or Dispose reserves one lifetime without waiting on pending native I/O: " + dispose);
                    Pump(() => stopping.IsSet && peer.IsCompleted, 3000); peer.GetAwaiter().GetResult();
                    int before = ticks; Pump(() => ticks >= before + 3, 1000);
                    check(start.IsCanceled && !relay.IsRunning && !stop.IsCompleted && relay.LastError == null,
                        "real stalled SOCKS probe closes before relay stop reports completion and native UI stays responsive: " + dispose);
                    bool refused = false;
                    try { relay.StartAsync("127.0.0.1", socksPort, ikePort, natPort, 17878, CancellationToken.None).GetAwaiter().GetResult(); }
                    catch (ObjectDisposedException) { refused = dispose; }
                    catch (InvalidOperationException) { refused = true; }
                    check(refused && !stop.IsCompleted, "relay cannot admit a competing start while its stop owner is unfinished: " + dispose);
                    foreach (int port in new[] { ikePort, natPort }) using (var available = new UdpClient(new IPEndPoint(IPAddress.Loopback, port))) { }
                    release.Set(); Pump(() => stop.IsCompleted, 5000); stop.GetAwaiter().GetResult();
                    check(!stop.IsFaulted && relay.PendingStopCount == 0, "relay stop completes only after startup and retiring network tasks settle: " + dispose);
                }
                finally
                {
                    release.Set(); relay.BeforeStopComplete = null;
                    try { var cleanup = relay.StopAsync(); Pump(() => cleanup.IsCompleted, 5000); cleanup.GetAwaiter().GetResult(); }
                    finally {
                        pulse.Stop(); form.Close(); listener.Stop();
                        var client = Interlocked.CompareExchange(ref accepted, null, null); if (client != null) client.Close();
                        Pump(() => peer.IsCompleted, 5000); var observed = peer.Exception;
                    }
                }
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
                check(form.Visible && !((Button)Field(form, "cancelWait")).Visible && !((Button)Field(form, "cancelWait")).Enabled
                    && ((Label)Field(form, "status")).Text.Contains("не отменяет") && !task.IsCompleted,
                    "server-changing operation cannot be cancelled or declared rolled back by title close");
                pending.SetResult(0); Pump(() => task.IsCompleted);
                check(form.Visible && !(bool)Field(form, "busy"), "protected operation completion restores controls without a queued close"); form.Close();
            }
        }
    }
}

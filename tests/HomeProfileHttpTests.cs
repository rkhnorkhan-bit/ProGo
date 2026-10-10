using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace ProGo
{
    // Real local HTTP/TLS sockets, with the production HTTPS URI checked before
    // routing only this request to the fixture. Never alters global TLS trust.
    internal static class HomeProfileHttpTests
    {
        private const string Origin = "https://vpn.example.org";
        private static Action<bool, string> check;
        private static readonly object assertionGate = new object();
        private static HomeVpnAccess access;
        private static string link, work;
        private sealed class WireRequest { internal string Method, Path, Body; internal Dictionary<string, string> Headers; }
        private sealed class Server : IDisposable
        {
            private readonly TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            private readonly CancellationTokenSource stopped = new CancellationTokenSource();
            private readonly List<TcpClient> clients = new List<TcpClient>();
            private readonly List<WireRequest> requests = new List<WireRequest>();
            private readonly List<Task> handlers = new List<Task>();
            private readonly Func<WireRequest, Stream, Server, Task> response;
            private readonly X509Certificate2 certificate;
            private readonly Task accept;
            internal readonly ManualResetEventSlim PeerClosed = new ManualResetEventSlim();
            internal int Factories, Accepted, Sent;
            internal bool PeerCertificateSeen;
            internal SslPolicyErrors PolicyErrors;
            internal WireRequest[] Requests { get { lock (requests) return requests.ToArray(); } }
            internal CancellationToken Token { get { return stopped.Token; } }
            internal Server(Func<WireRequest, Stream, Server, Task> response, X509Certificate2 certificate = null)
            { this.response = response; this.certificate = certificate; listener.Start(); accept = Accept(); }
            internal HttpWebRequest Factory(Uri uri)
            {
                check(uri.Scheme == "https" && uri.Host == "vpn.example.org" && (uri.AbsolutePath == "/health" || uri.AbsolutePath == "/api/share"), "HTTP factory receives the exact production HTTPS endpoint");
                Interlocked.Increment(ref Factories);
                var target = new Uri((certificate == null ? "http" : "https") + "://127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port + uri.PathAndQuery);
                var request = (HttpWebRequest)WebRequest.Create(target); request.ServicePoint.Expect100Continue = false;
                if (certificate != null) request.ServerCertificateValidationCallback = delegate(object sender, X509Certificate peer, X509Chain chain, SslPolicyErrors errors) {
                    PeerCertificateSeen = peer != null; PolicyErrors = errors; return errors == SslPolicyErrors.None;
                };
                return request;
            }
            private async Task Accept()
            {
                try {
                    while (!stopped.IsCancellationRequested) {
                        var client = await listener.AcceptTcpClientAsync().ConfigureAwait(false); Interlocked.Increment(ref Accepted);
                        lock (clients) clients.Add(client); var handler = Handle(client); lock (handlers) handlers.Add(handler);
                    }
                } catch (SocketException) { } catch (ObjectDisposedException) { }
            }
            private async Task Handle(TcpClient client)
            {
                try {
                    using (client)
                    using (var network = client.GetStream()) {
                        Stream stream = network;
                        using (var ssl = certificate == null ? null : new SslStream(network, false)) {
                            if (ssl != null) { await ssl.AuthenticateAsServerAsync(certificate, false, SslProtocols.Tls12, false).ConfigureAwait(false); stream = ssl; }
                            var header = new List<byte>(); var one = new byte[1];
                            while (header.Count < 16384) {
                                if (await stream.ReadAsync(one, 0, 1, stopped.Token).ConfigureAwait(false) == 0) { PeerClosed.Set(); return; }
                                header.Add(one[0]); int n = header.Count;
                                if (n >= 4 && header[n - 4] == 13 && header[n - 3] == 10 && header[n - 2] == 13 && header[n - 1] == 10) break;
                            }
                            string[] lines = Encoding.ASCII.GetString(header.ToArray()).Split(new[] { "\r\n" }, StringSplitOptions.None); string[] first = lines[0].Split(' ');
                            var request = new WireRequest { Method = first[0], Path = first[1], Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) };
                            foreach (string line in lines.Skip(1)) { int at = line.IndexOf(':'); if (at > 0) request.Headers[line.Substring(0, at)] = line.Substring(at + 1).Trim(); }
                            string length; int count = request.Headers.TryGetValue("Content-Length", out length) ? Int32.Parse(length) : 0; if (count > 4096) throw new IOException();
                            byte[] body = new byte[count]; int read = 0;
                            while (read < count) { int size = await stream.ReadAsync(body, read, count - read, stopped.Token).ConfigureAwait(false); if (size == 0) throw new IOException(); read += size; }
                            request.Body = Encoding.UTF8.GetString(body); lock (requests) requests.Add(request);
                            await response(request, stream, this).ConfigureAwait(false);
                        }
                    }
                } catch (IOException) { PeerClosed.Set(); } catch (OperationCanceledException) { } catch (ObjectDisposedException) { } catch (AuthenticationException) { PeerClosed.Set(); }
            }
            internal async Task Hold(Stream stream)
            { byte[] one = new byte[1]; if (await stream.ReadAsync(one, 0, 1, stopped.Token).ConfigureAwait(false) == 0) PeerClosed.Set(); }
            internal async Task Reply(Stream stream, string text, int delay = 0, int trickle = 0, bool gzip = false, int status = 200)
            {
                if (delay != 0) await Task.Delay(delay, stopped.Token).ConfigureAwait(false);
                byte[] bytes = Encoding.UTF8.GetBytes(text);
                if (gzip) using (var output = new MemoryStream()) { using (var zip = new GZipStream(output, CompressionMode.Compress, true)) zip.Write(bytes, 0, bytes.Length); bytes = output.ToArray(); }
                byte[] header = Encoding.ASCII.GetBytes("HTTP/1.1 " + status + " Fixture\r\nContent-Type: application/json\r\nContent-Length: " + bytes.Length
                    + "\r\nConnection: close\r\n" + (gzip ? "Content-Encoding: gzip\r\n" : "") + (status == 302 ? "Location: /redirected\r\n" : "") + "\r\n");
                await stream.WriteAsync(header, 0, header.Length, stopped.Token).ConfigureAwait(false);
                if (trickle == 0) { await stream.WriteAsync(bytes, 0, bytes.Length, stopped.Token).ConfigureAwait(false); Interlocked.Add(ref Sent, bytes.Length); }
                else for (int i = 0; i < bytes.Length; i++) { await stream.WriteAsync(bytes, i, 1, stopped.Token).ConfigureAwait(false); Interlocked.Increment(ref Sent); await Task.Delay(trickle, stopped.Token).ConfigureAwait(false); }
            }
            public void Dispose()
            {
                stopped.Cancel(); listener.Stop(); lock (clients) foreach (var client in clients) client.Close();
                try { accept.Wait(2000); } catch (AggregateException) { }
                Task[] active; lock (handlers) active = handlers.ToArray(); try { Task.WaitAll(active, 2000); } catch (AggregateException) { }
                stopped.Dispose(); PeerClosed.Dispose();
            }
        }
        internal static void Run(Action<bool, string> assert, string token, string folder)
        {
            check = (condition, name) => { lock (assertionGate) assert(condition, name); };
            access = HomeVpnAccess.Parse(token); work = folder; link = File.ReadAllText(Path.Combine(work, "qr.json"));
            var validation = ServicePointManager.ServerCertificateValidationCallback; Exception threadFailure = null;
            ThreadExceptionEventHandler handler = (sender, args) => { threadFailure = args.Exception; }; Application.ThreadException += handler;
            try {
                Success(); Delays(); Cancellation(); Bounds(); Errors(); Tls();
                foreach (string route in new[] { "button", "escape", "close", "dispose", "null-context" }) Waiting(route);
                WaitingSuccess(); Configure(token, false); Configure(token, true); QueuedSuccess(token, false); QueuedSuccess(token, true);
                check(threadFailure == null && ServicePointManager.ServerCertificateValidationCallback == validation, "HTTP fixtures cause no UI exception and preserve global certificate validation");
            } finally { Application.ThreadException -= handler; }
        }
        private static string Health { get { return new JavaScriptSerializer().Serialize(new { ServerId = access.ServerId }); } }
        private static Exception Finish(Task task, int timeout = 5000)
        {
            try { if (!task.Wait(timeout)) throw new TimeoutException("Local HTTP fixture did not settle"); } catch (AggregateException) { }
            try { task.GetAwaiter().GetResult(); return null; } catch (Exception ex) { return ex; }
        }
        private static void Ready(Func<bool> ready)
        { var watch = Stopwatch.StartNew(); while (!ready()) { if (watch.ElapsedMilliseconds > 4000) throw new Exception("Local HTTP fixture did not receive its request"); Thread.Sleep(10); } }
        private static bool Safe(Exception failure)
        { return failure != null && !failure.Message.Contains(access.Password) && !failure.Message.Contains(access.PrivateKey) && !failure.Message.Contains(access.User); }
        private static void Success()
        {
            using (var server = new Server((request, stream, owner) => owner.Reply(stream, request.Path == "/health" ? Health : link))) {
                var previous = SynchronizationContext.Current; Task<PhoneProfileLink> result;
                try { SynchronizationContext.SetSynchronizationContext(null); result = HomeProfileShare.CreateAsync(Origin, access, "home.example.org", CancellationToken.None, 5000, server.Factory); }
                finally { SynchronizationContext.SetSynchronizationContext(previous); }
                check(Finish(result) == null && result.Result.Url == new JavaScriptSerializer().Deserialize<PhoneProfileLink>(link).Url, "real health + POST return the validated QR without a synchronization context");
                var requests = server.Requests; string authorization;
                check(requests.Length == 2 && server.Factories == 2 && requests[0].Method == "GET" && requests[0].Path == "/health" && !requests[0].Headers.ContainsKey("Authorization")
                    && requests[1].Method == "POST" && requests[1].Path == "/api/share" && requests[1].Body.Contains("home.example.org")
                    && requests[1].Headers.TryGetValue("Authorization", out authorization) && authorization == "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(access.User + ":" + access.Password)),
                    "actual wire sends one anonymous health and one authenticated POST with the frozen home address");
            }
            using (var server = new Server((request, stream, owner) => owner.Reply(stream, "{}"))) {
                check(Finish(HomeProfileShare.RevokeAsync(Origin, access, CancellationToken.None, 5000, server.Factory)) == null && server.Requests.Length == 1
                    && server.Requests[0].Method == "DELETE" && server.Requests[0].Path == "/api/share", "real revoke sends exactly one DELETE");
            }
        }
        private static void Delays()
        {
            using (var server = new Server((request, stream, owner) => owner.Reply(stream, Health, 2000))) {
                var watch = Stopwatch.StartNew(); var failure = Finish(HomeProfileShare.VerifyAsync(Origin, access, CancellationToken.None, 600, server.Factory));
                check(Safe(failure) && failure.Message.Contains("Время проверки HTTPS истекло") && watch.ElapsedMilliseconds < 3000 && server.Requests.Length == 1 && server.Factories == 1,
                    "delayed headers obey the bounded whole-operation deadline without retry");
            }
            using (var server = new Server((request, stream, owner) => owner.Reply(stream, Health, 0, 50))) {
                var failure = Finish(HomeProfileShare.VerifyAsync(Origin, access, CancellationToken.None, 600, server.Factory));
                check(Safe(failure) && failure.Message.Contains("Время проверки HTTPS истекло") && server.Sent >= 3 && server.Requests.Length == 1,
                    "continuous trickle cannot renew the total HTTP deadline");
            }
            using (var server = new Server((request, stream, owner) => owner.Reply(stream, request.Path == "/health" ? Health : link, 700))) {
                var watch = Stopwatch.StartNew(); var failure = Finish(HomeProfileShare.CreateAsync(Origin, access, "home.example.org", CancellationToken.None, 1200, server.Factory));
                check(Safe(failure) && failure.Message.Contains("Создание QR не подтверждено") && watch.ElapsedMilliseconds < 4000 && server.Requests.Select(request => request.Method).SequenceEqual(new[] { "GET", "POST" }) && server.Factories == 2,
                    "health and POST share one deadline and report possible mutation without replay");
            }
        }
        private static void Cancellation()
        {
            foreach (string action in new[] { "verify", "create", "revoke" })
            using (var source = new CancellationTokenSource())
            using (var server = new Server((request, stream, owner) => owner.Hold(stream))) {
                Task task = action == "verify" ? HomeProfileShare.VerifyAsync(Origin, access, source.Token, 5000, server.Factory)
                    : action == "create" ? (Task)HomeProfileShare.CreateAsync(Origin, access, "home.example.org", source.Token, 5000, server.Factory)
                    : HomeProfileShare.RevokeAsync(Origin, access, source.Token, 5000, server.Factory);
                Ready(() => server.Requests.Length == 1); source.Cancel(); var failure = Finish(task);
                check(failure is HomeProfileHttpCancelledException && Safe(failure) && server.PeerClosed.Wait(2000) && server.Requests.Length == 1 && server.Factories == 1,
                    "cancellation aborts the actual owned socket and never retries: " + action);
                if (action == "create") check(server.Requests[0].Method == "GET" && failure.Message.Contains("до отправки"), "cancelled health never sends the POST or claims QR mutation");
                if (action == "revoke") check(failure.Message.Contains("не подтверждён"), "cancelled DELETE retains honest remote uncertainty");
            }
            using (var source = new CancellationTokenSource())
            using (var blocked = new Server((request, stream, owner) => owner.Hold(stream)))
            using (var independent = new Server((request, stream, owner) => owner.Reply(stream, Health))) {
                var pending = HomeProfileShare.VerifyAsync(Origin, access, source.Token, 5000, blocked.Factory); Ready(() => blocked.Requests.Length == 1);
                var success = HomeProfileShare.VerifyAsync(Origin, access, CancellationToken.None, 5000, independent.Factory); source.Cancel();
                check(Finish(pending) is HomeProfileHttpCancelledException && Finish(success) == null && independent.Requests.Length == 1,
                    "aborting one HTTP operation preserves an independent request");
            }
        }
        private static void Bounds()
        {
            foreach (int length in new[] { 65536, 65537 })
            using (var server = new Server((request, stream, owner) => owner.Reply(stream, new string('x', length), 0, 0, true)))
            using (var http = new HomeProfileHttp(HomeProfileHttpPurpose.Verify, CancellationToken.None, 5000, server.Factory)) {
                var task = http.RequestAsync(Origin, "/health", "GET", null, null); var failure = Finish(task);
                check((length == 65536 ? failure == null && task.Result.Length == length : Safe(failure)) && server.Requests.Length == 1 && server.Factories == 1,
                    "gzip response enforces the decoded 64 KiB boundary: " + length);
            }
        }
        private static void Errors()
        {
            foreach (int status in new[] { 302, 401 })
            using (var server = new Server((request, stream, owner) => owner.Reply(stream, access.Password, 0, 0, false, status))) {
                var failure = Finish(HomeProfileShare.RevokeAsync(Origin, access, CancellationToken.None, 5000, server.Factory));
                check(Safe(failure) && server.Requests.Length == 1 && server.Factories == 1 && (status != 401 || failure.Message.Contains("отозван")),
                    "redirect/auth failure makes one attempt and never echoes private response material: " + status);
            }
        }
        private static void Tls()
        {
            string executable = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Git", "usr", "bin", "openssl.exe");
            if (!File.Exists(executable)) executable = "openssl.exe";
            string path = Path.Combine(work, "http-fixture.pfx");
            using (var process = Process.Start(new ProcessStartInfo(executable, "pkcs12 -export -inkey " + HomeVpnService.Argument(Path.Combine(work, "ca-key"))
                + " -in " + HomeVpnService.Argument(Path.Combine(work, "ca-cert")) + " -out " + HomeVpnService.Argument(path)
                + " -passout pass:progo-fixture -keypbe PBE-SHA1-3DES -certpbe PBE-SHA1-3DES -macalg sha1") { UseShellExecute = false, CreateNoWindow = true })) {
                if (!process.WaitForExit(5000)) { process.Kill(); process.WaitForExit(2000); throw new Exception("Local TLS certificate fixture timed out"); }
                if (process.ExitCode != 0) throw new Exception("Cannot create local TLS certificate fixture");
            }
            try {
                using (var certificate = new X509Certificate2(path, "progo-fixture", X509KeyStorageFlags.UserKeySet))
                using (var server = new Server((request, stream, owner) => owner.Reply(stream, Health), certificate)) {
                    var failure = Finish(HomeProfileShare.VerifyAsync(Origin, access, CancellationToken.None, 5000, server.Factory));
                    check(Safe(failure) && failure.Message.Contains("сертификат") && server.Accepted == 1 && server.Factories == 1 && server.Requests.Length == 0
                        && server.PeerCertificateSeen && server.PolicyErrors != SslPolicyErrors.None,
                        "actual TLS handshake rejects the untrusted self-signed fixture without any certificate-validation bypass");
                }
            } finally { File.Delete(path); }
        }
        private static object Field(object value, string name) { return value.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(value); }
        private static IEnumerable<Control> Controls(Control target) { foreach (Control child in target.Controls) { yield return child; foreach (Control nested in Controls(child)) yield return nested; } }
        private static void Shot(Form form, string name) { using (var bitmap = new Bitmap(form.Width, form.Height)) { form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size)); bitmap.Save(Path.Combine(work, name + ".png")); } }
        private static void Loop(Form host, Action start, Func<bool> done, Action tick, Action settled = null)
        {
            bool started = false; Exception failure = null; var watch = Stopwatch.StartNew();
            using (var timer = new System.Windows.Forms.Timer { Interval = 20 }) {
                host.Shown += delegate { host.BeginInvoke(new Action(delegate { started = true; try { start(); } catch (Exception ex) { failure = ex; } })); };
                timer.Tick += delegate {
                    try {
                        if (!started) return; if (tick != null) tick();
                        if (watch.ElapsedMilliseconds > 12000) failure = new TimeoutException("HTTP native UI did not settle");
                        if (failure != null) { timer.Stop(); foreach (var form in Application.OpenForms.Cast<Form>().Where(form => form != host).ToArray()) form.Dispose(); host.Dispose(); Application.ExitThread(); return; }
                        if (done()) { if (settled != null) settled(); timer.Stop(); host.Close(); }
                    } catch (Exception ex) { failure = ex; }
                }; timer.Start(); Application.Run(host);
            }
            if (failure != null) throw new Exception("HTTP UI fixture failed", failure);
        }
        private static void Waiting(string route)
        {
            int ticks = 0; bool cancelled = false; Task<int> task = null;
            using (var server = new Server((request, stream, owner) => owner.Hold(stream)))
            using (var host = new Form()) {
                Loop(host, delegate {
                    task = HomeProfileHttpWaitForm.WaitAsync(host, "HTTP fixture wait", async token => {
                        if (route == "null-context") { SynchronizationContext.SetSynchronizationContext(null);
                            check(SynchronizationContext.Current == null, "actual owned HTTP work starts without a synchronization context"); }
                        await HomeProfileShare.VerifyAsync(Origin, access, token, 5000, server.Factory).ConfigureAwait(false); return 1;
                    });
                }, () => task != null && task.IsCompleted, delegate {
                    ticks++; var wait = Application.OpenForms.OfType<HomeProfileHttpWaitForm>().FirstOrDefault();
                    if (cancelled || wait == null || server.Requests.Length != 1 || ticks < 3) return; cancelled = true;
                    var cancel = (Button)wait.CancelButton; var status = Controls(wait).OfType<Label>().Single(value => value.AccessibleName == "Ход HTTPS-запроса");
                    check(cancel.Enabled && wait.AcceptButton == null && status.AccessibilityObject.Description == status.Text, "actual HTTP wait offers responsive accessible cancellation");
                    if (route == "button") { Shot(wait, "vps-http-wait-pending"); wait.ClientSize = new Size(440, 270); Application.DoEvents();
                        check(wait.RectangleToClient(cancel.RectangleToScreen(cancel.ClientRectangle)).Bottom <= wait.ClientSize.Height, "minimum HTTP wait retains its cancellation button"); Shot(wait, "vps-http-wait-minimum"); }
                    if (route == "escape") typeof(Form).GetMethod("ProcessDialogKey", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(wait, new object[] { Keys.Escape });
                    else if (route == "close") wait.Close(); else if (route == "dispose") wait.Dispose(); else cancel.PerformClick();
                });
                check(cancelled && ticks >= 3 && Finish(task) is HomeProfileHttpCancelledException && server.PeerClosed.Wait(2000) && server.Requests.Length == 1,
                    "actual HTTP wait cancellation settles its socket and owner continuation: " + route);
            }
        }
        private static void WaitingSuccess()
        {
            int ticks = 0; Task<int> task = null;
            using (var server = new Server((request, stream, owner) => owner.Reply(stream, Health, 300)))
            using (var host = new Form()) {
                Loop(host, delegate {
                    task = HomeProfileHttpWaitForm.WaitAsync(host, "HTTP fixture success", async token => {
                        await HomeProfileShare.VerifyAsync(Origin, access, token, 5000, server.Factory).ConfigureAwait(false); return 17;
                    });
                }, () => task != null && task.IsCompleted, () => { ticks++; });
                check(Finish(task) == null && task.Result == 17 && ticks >= 3 && server.Requests.Length == 1 && server.Factories == 1,
                    "actual successful HTTP wait closes itself and returns its result without disposal converting success into cancellation");
            }
        }
        private static void Configure(string token, bool dispose)
        {
            string pending = Path.Combine(HomeVpnPrivateFiles.Root, "share-request.dat"), share = Path.Combine(HomeVpnPrivateFiles.Root, "share-" + access.ServerId + ".dat");
            var paths = new[] { pending, share, Path.Combine(HomeVpnPrivateFiles.Root, "access.dat"), Path.Combine(HomeVpnPrivateFiles.Root, "owner.dat"), Path.Combine(HomeVpnPrivateFiles.Root, "setup-request.dat") };
            var original = paths.ToDictionary(path => path, path => File.Exists(path) ? File.ReadAllBytes(path) : null);
            try {
                File.Delete(pending); File.Delete(Path.Combine(HomeVpnPrivateFiles.Root, "setup-request.dat"));
                using (var relay = new Ikev2RelayService())
                using (var service = new HomeVpnService(relay))
                using (var host = new Form())
                using (var server = new Server((request, stream, owner) => owner.Hold(stream))) {
                    var owner = new HomeVpnOwner { Host = access.Host, Login = "root", Port = access.Port, KeyFile = "" }; service.UseToken(token, owner);
                    var request = HomeVpnShareRecovery.Register(owner, "vpn.example.org"); byte[] journal = File.ReadAllBytes(pending), oldShare = File.Exists(share) ? File.ReadAllBytes(share) : null;
                    int saved = 0, admin = 0, ticks = 0, updates = 0; bool closed = false, settled = false;
                    using (var form = HomeProfileShare.CreateConfigureForm(service, (current, action, name, id, progress) => {
                        admin++; check(action == "recover-share", "HTTP configuration checks the retained HTTPS request without new installation");
                        HomeVpnShareRecovery.RequireCompleted(new JavaScriptSerializer().Serialize(new { Version = 1, RequestId = request.RequestId, Action = "share", State = "succeeded",
                            Started = "2026-01-01T00:00:00+00:00", Finished = "2026-01-01T00:00:01+00:00", ResultAvailable = true }), request);
                        return Task.FromResult(HomeVpnShareRecovery.RequireResult(owner, request, Origin));
                    }, (origin, current, cancellation) => HomeProfileShare.VerifyAsync(origin, current, cancellation, 5000, server.Factory), origin => { saved++; service.SetShareOrigin(origin); })) {
                        Loop(host, delegate { form.Show(host); SynchronizationContext.SetSynchronizationContext(null);
                            Controls(form).OfType<Button>().Single(value => value.Text == "Проверить прежнюю настройку HTTPS").PerformClick();
                        }, () => settled && ticks >= 3, delegate {
                            if (!closed && server.Requests.Length == 1) {
                                var retained = Controls(form).ToArray(); if (dispose) form.Dispose(); else { Shot(form, "vps-http-configure-pending"); form.Close(); }
                                closed = true; foreach (var control in retained) { control.TextChanged += delegate { updates++; }; control.EnabledChanged += delegate { updates++; }; control.VisibleChanged += delegate { updates++; }; }
                            }
                            if (closed && server.PeerClosed.IsSet) {
                                var probe = HomeVpnShareRecovery.AcquireAsync();
                                if (probe.IsCompleted && !probe.IsFaulted) { probe.GetAwaiter().GetResult().Dispose(); settled = true; } else if (probe.IsFaulted) { var observed = probe.Exception; }
                            }
                            if (settled) ticks++;
                        });
                    }
                    check(closed && saved == 0 && admin == 1 && server.Factories == 1 && server.Requests.Length == 1 && journal.SequenceEqual(File.ReadAllBytes(pending))
                        && (oldShare == null ? !File.Exists(share) : oldShare.SequenceEqual(File.ReadAllBytes(share))) && (!dispose || updates == 0),
                        "closing/disposing real configuration during HTTP verification preserves the N journal and former address without late save: dispose=" + dispose);
                }
            } finally { foreach (var item in original) { if (item.Value == null) File.Delete(item.Key); else File.WriteAllBytes(item.Key, item.Value); } }
        }
        private static void QueuedSuccess(string token, bool dispose)
        {
            string pending = Path.Combine(HomeVpnPrivateFiles.Root, "share-request.dat"), share = Path.Combine(HomeVpnPrivateFiles.Root, "share-" + access.ServerId + ".dat");
            var paths = new[] { pending, share, Path.Combine(HomeVpnPrivateFiles.Root, "access.dat"), Path.Combine(HomeVpnPrivateFiles.Root, "owner.dat"), Path.Combine(HomeVpnPrivateFiles.Root, "setup-request.dat") };
            var original = paths.ToDictionary(path => path, path => File.Exists(path) ? File.ReadAllBytes(path) : null);
            try {
                File.Delete(pending); File.Delete(Path.Combine(HomeVpnPrivateFiles.Root, "setup-request.dat"));
                using (var relay = new Ikev2RelayService())
                using (var service = new HomeVpnService(relay))
                using (var source = new CancellationTokenSource())
                using (var ready = new ManualResetEventSlim())
                using (var target = new Form())
                using (var host = new Form())
                using (var dispatcher = new HomeProfileUiDispatcher(target))
                using (var server = new Server((request, stream, owner) => owner.Reply(stream, Health))) {
                    var owner = new HomeVpnOwner { Host = access.Host, Login = "root", Port = access.Port, KeyFile = "" }; service.UseToken(token, owner);
                    var request = HomeVpnShareRecovery.Register(owner, "vpn.example.org"); byte[] journal = File.ReadAllBytes(pending), oldShare = File.Exists(share) ? File.ReadAllBytes(share) : null;
                    HomeVpnShareRecovery.RequireCompleted(new JavaScriptSerializer().Serialize(new { Version = 1, RequestId = request.RequestId, Action = "share", State = "succeeded",
                        Started = "2026-01-01T00:00:00+00:00", Finished = "2026-01-01T00:00:01+00:00", ResultAvailable = true }), request);
                    HomeVpnShareRecovery.RequireResult(owner, request, Origin);
                    var acceptance = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                    Action queued = null; Task task = null; int saves = 0, runs = 0, ticks = 0; bool released = false;
                    Loop(host, delegate {
                        target.Show(host);
                        task = HomeVpnShareRecovery.ConfirmAsync(owner, service.Access, Origin,
                            () => HomeProfileShare.VerifyAsync(Origin, service.Access, source.Token, 5000, server.Factory),
                            origin => { saves++; service.SetShareOrigin(origin); }, () => !target.IsDisposed && !target.Disposing,
                            source.Token, action => { queued = action; ready.Set(); return acceptance.Task; });
                    }, () => task != null && task.IsCompleted, delegate {
                        ticks++; if (released || !ready.IsSet) return; released = true;
                        check(server.Requests.Length == 1 && server.Factories == 1 && queued != null, "real health has completed before the queued owner acceptance is released");
                        if (dispose) target.Dispose(); else source.Cancel();
                        var published = dispatcher.DispatchAsync(delegate { runs++; queued(); });
                        published.ContinueWith(completion => {
                            try { acceptance.TrySetResult(completion.GetAwaiter().GetResult()); } catch (Exception ex) { acceptance.TrySetException(ex); }
                        }, TaskScheduler.Default);
                    });
                    var failure = Finish(task);
                    check(released && ticks > 0 && (dispose ? failure is HomeVpnSharePendingException && runs == 0 : failure is OperationCanceledException && runs == 1)
                        && saves == 0 && journal.SequenceEqual(File.ReadAllBytes(pending))
                        && (oldShare == null ? !File.Exists(share) : oldShare.SequenceEqual(File.ReadAllBytes(share))),
                        "Cancel/Dispose after successful HTTP but before owner acceptance prevents save and journal consumption: dispose=" + dispose);
                }
            } finally { foreach (var item in original) { if (item.Value == null) File.Delete(item.Key); else File.WriteAllBytes(item.Key, item.Value); } }
        }
    }
}

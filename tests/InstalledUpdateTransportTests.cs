using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ProGo
{
    internal static class InstalledUpdateTransportTests
    {
        private static int passed;
        private static void Check(bool condition, string name)
        {
            if (!condition) throw new Exception(name);
            passed++; Console.WriteLine("PASS: " + name);
        }
        private static T Finish<T>(Task<T> task)
        {
            if (Task.WhenAny(task, Task.Delay(5000)).GetAwaiter().GetResult() != task)
                throw new Exception("Transport exceeded fixture watchdog.");
            return task.GetAwaiter().GetResult();
        }
        private static void Finish(Task task)
        {
            if (Task.WhenAny(task, Task.Delay(5000)).GetAwaiter().GetResult() != task)
                throw new Exception("Transport exceeded fixture watchdog.");
            task.GetAwaiter().GetResult();
        }
        private sealed class Server : IDisposable
        {
            private readonly TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            private readonly ManualResetEventSlim stop = new ManualResetEventSlim();
            internal readonly ManualResetEventSlim Requested = new ManualResetEventSlim();
            internal readonly ManualResetEventSlim BodyStarted = new ManualResetEventSlim();
            private readonly Task worker;
            private TcpClient client;
            internal Uri Endpoint;
            internal string Request;
            internal Server(byte[] body, string mode = "complete", int status = 200, string location = null)
            {
                listener.Start();
                Endpoint = new Uri("http://127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port + "/release");
                var accept = listener.AcceptTcpClientAsync();
                worker = Task.Run(async delegate {
                    try {
                        using (var accepted = await accept.ConfigureAwait(false)) {
                            client = accepted; accepted.ReceiveTimeout = 5000;
                            using (var stream = accepted.GetStream()) {
                                var headers = new StringBuilder();
                                while (!headers.ToString().EndsWith("\r\n\r\n", StringComparison.Ordinal)) {
                                    int value = stream.ReadByte();
                                    if (value < 0 || headers.Length >= 16384) throw new IOException("Invalid fixture request.");
                                    headers.Append((char)value);
                                }
                                Request = headers.ToString(); Requested.Set();
                                if (mode == "headers-stall") { stop.Wait(5000); return; }
                                int declared = mode == "declared-oversize" ? 65537 :
                                    (mode == "truncated" ? body.Length + 10 : body.Length);
                                string framing = mode == "chunked" ? "Transfer-Encoding: chunked\r\n" : "Content-Length: " + declared + "\r\n";
                                string redirect = location == null ? "" : "Location: " + location + "\r\n";
                                byte[] prefix = Encoding.ASCII.GetBytes("HTTP/1.1 " + status + " Fixture\r\n" + framing + redirect + "Connection: close\r\n\r\n");
                                stream.Write(prefix, 0, prefix.Length);
                                if (mode == "body-stall") stream.Write(body, 0, 1);
                                else if (mode == "drip") {
                                    for (int i = 0; i < body.Length; i++) {
                                        stream.Write(body, i, 1); stream.Flush(); BodyStarted.Set();
                                        if (stop.Wait(100)) return;
                                    }
                                }
                                else if (mode == "chunked") {
                                    byte[] chunk = Encoding.ASCII.GetBytes(body.Length.ToString("x") + "\r\n");
                                    stream.Write(chunk, 0, chunk.Length); stream.Write(body, 0, body.Length);
                                    byte[] end = Encoding.ASCII.GetBytes("\r\n0\r\n\r\n"); stream.Write(end, 0, end.Length);
                                }
                                else if (mode != "declared-oversize") stream.Write(body, 0, body.Length);
                                stream.Flush(); BodyStarted.Set();
                                if (mode == "body-stall" || mode == "declared-oversize") stop.Wait(5000);
                            }
                        }
                    }
                    catch (IOException) { if (!stop.IsSet && Request == null) throw; }
                    catch (SocketException) { if (!stop.IsSet) throw; }
                    catch (ObjectDisposedException) { if (!stop.IsSet) throw; }
                });
            }
            public void Dispose()
            {
                stop.Set(); listener.Stop(); if (client != null) client.Close();
                if (!worker.Wait(5000)) throw new Exception("Fixture server did not stop.");
                Requested.Dispose(); BodyStarted.Dispose(); stop.Dispose();
            }
        }
        private static byte[] Text(string value) { return Encoding.UTF8.GetBytes(value); }
        private static void Interrupted(string root, string mode, bool cancel, bool package)
        {
            string file = Path.Combine(root, Guid.NewGuid().ToString("N") + ".zip");
            using (var server = new Server(Text(new String('x', 100)), mode))
            using (var cancellation = new CancellationTokenSource()) {
                var clock = Stopwatch.StartNew();
                Task task = package ? InstalledUpdateTransport.DownloadPackageAsync(server.Endpoint, file,
                    cancel ? 4000 : 1000, 65536, cancellation.Token) :
                    (Task)InstalledUpdateTransport.ReadMetadataAsync(server.Endpoint, cancel ? 4000 : 1000, 65536, cancellation.Token);
                Check(server.Requested.Wait(3000), mode + " reaches socket for " + (package ? "package" : "metadata"));
                if (mode != "headers-stall") Check(server.BodyStarted.Wait(3000), "partial body arrived");
                if (cancel) cancellation.Cancel();
                try { Finish(task); throw new Exception("Interrupted request succeeded."); }
                catch (OperationCanceledException error) {
                    Check(cancel && error.CancellationToken == cancellation.Token && task.IsCanceled,
                        "caller cancellation retains identity and canceled task state");
                }
                catch (TimeoutException error) {
                    Check(!cancel && error.Message.Contains("Повторите") && !error.Message.Contains("127.0.0.1"),
                        "total deadline reports a retry without endpoint details");
                }
                Check(clock.ElapsedMilliseconds < 3500, "interrupted transport finishes within watchdog");
                if (package) Check(!File.Exists(file), "partial package removed before completion");
            }
        }
        private static int Main()
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") {
                Console.WriteLine("SKIP: requires isolated Windows CI"); return 0;
            }
            string root = Path.Combine(Path.GetTempPath(), "ProGo-transport-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try {
                Check(InstalledUpdateTransport.MetadataTimeoutMilliseconds == 10000 &&
                    InstalledUpdateTransport.PackageTimeoutMilliseconds == 180000, "production requests have finite total deadlines");
                Check(InstalledUpdateTransport.MaxMetadataBytes == 2 * 1024 * 1024 &&
                    InstalledUpdateTransport.MaxPackageBytes == 64 * 1024 * 1024, "production metadata and compressed package caps");
                using (var server = new Server(Text("\uFEFF{\"tag_name\":\"v0.3.0\",\"body\":\"Описание\"}"))) {
                    string reply = Finish(InstalledUpdateTransport.ReadMetadataAsync(server.Endpoint, 3000, 65536, CancellationToken.None));
                    Check(reply.StartsWith("{") && reply.Contains("Описание"), "metadata UTF8 and BOM preserved");
                    Check(server.Request.StartsWith("GET /release HTTP/1.1\r\n") &&
                        server.Request.Contains("User-Agent: ProGo-Updater\r\n") &&
                        server.Request.Contains("Accept: application/vnd.github+json\r\n"), "metadata uses expected request headers");
                }
                byte[] payload = new byte[65536]; new Random(23).NextBytes(payload);
                string target = Path.Combine(root, "package.zip");
                foreach (string mode in new[] { "complete", "chunked" })
                    using (var server = new Server(payload, mode)) {
                        Finish(InstalledUpdateTransport.DownloadPackageAsync(server.Endpoint, target, 3000, payload.Length, CancellationToken.None));
                        Check(Convert.ToBase64String(File.ReadAllBytes(target)) == Convert.ToBase64String(payload), mode + " package remains byte-exact at size limit");
                        File.Delete(target);
                    }
                foreach (bool package in new[] { false, true })
                    foreach (string mode in new[] { "headers-stall", "body-stall", "drip" }) {
                        Interrupted(root, mode, false, package); Interrupted(root, mode, true, package);
                    }
                foreach (bool package in new[] { false, true })
                    foreach (string mode in new[] { "declared-oversize", "chunked", "truncated" })
                        using (var server = new Server(mode == "truncated" ? Text("short") : new byte[65537], mode)) {
                            Task task = package ? InstalledUpdateTransport.DownloadPackageAsync(server.Endpoint, target, 3000, 65536, CancellationToken.None) :
                                (Task)InstalledUpdateTransport.ReadMetadataAsync(server.Endpoint, 3000, 65536, CancellationToken.None);
                            try { Finish(task); throw new Exception("Invalid body accepted."); }
                            catch (Exception error) {
                                Check(error is InvalidDataException || error is IOException,
                                    mode + " rejected for " + (package ? "package" : "metadata"));
                            }
                            if (package) Check(!File.Exists(target), "invalid package leaves no partial file");
                        }
                using (var server = new Server(Text("private fixture detail"), "complete", 503)) {
                    try { Finish(InstalledUpdateTransport.DownloadPackageAsync(server.Endpoint, target, 3000, 65536, CancellationToken.None)); throw new Exception("HTTP error accepted."); }
                    catch (IOException error) {
                        Check(!error.Message.Contains("private fixture") && !error.Message.Contains("127.0.0.1"), "HTTP error does not expose body or endpoint");
                        Check(!File.Exists(target), "HTTP error removes partial package");
                    }
                }
                using (var destination = new Server(Text("redirected")))
                using (var redirect = new Server(new byte[0], "complete", 302, destination.Endpoint.ToString())) {
                    Check(Finish(InstalledUpdateTransport.ReadMetadataAsync(redirect.Endpoint, 3000, 65536, CancellationToken.None)) == "redirected",
                        "allowed redirect preserves bounded transfer");
                }
                using (var redirect = new Server(new byte[0], "complete", 302, "file:///fixture.zip")) {
                    try { Finish(InstalledUpdateTransport.ReadMetadataAsync(redirect.Endpoint, 3000, 65536, CancellationToken.None)); throw new Exception("Non-web redirect accepted."); }
                    catch (InvalidDataException) { Check(true, "non-web redirect refused before following"); }
                }
                using (var server = new Server(Text("retry"))) {
                    Finish(InstalledUpdateTransport.DownloadPackageAsync(server.Endpoint, target, 3000, 65536, CancellationToken.None));
                    Check(File.ReadAllText(target) == "retry", "fresh attempt succeeds after network failures");
                }
                using (var server = new Server(Text("replacement"))) {
                    try { Finish(InstalledUpdateTransport.DownloadPackageAsync(server.Endpoint, target, 3000, 65536, CancellationToken.None)); throw new Exception("Existing file overwritten."); }
                    catch (IOException) { Check(File.ReadAllText(target) == "retry" && !server.Requested.IsSet, "existing target is preserved without a request"); }
                }
                File.Delete(target);
                using (var cancellation = new CancellationTokenSource())
                using (var server = new Server(Text("unused"))) {
                    cancellation.Cancel();
                    try { Finish(InstalledUpdateTransport.DownloadPackageAsync(server.Endpoint, target, 3000, 65536, cancellation.Token)); throw new Exception("Pre-canceled download succeeded."); }
                    catch (OperationCanceledException) { Check(!server.Requested.IsSet && !File.Exists(target), "pre-canceled download sends no request or creates file"); }
                }
                try { InstalledUpdateTransport.ReadMetadata("http://127.0.0.1/release", CancellationToken.None); throw new Exception("Production HTTP accepted."); }
                catch (ArgumentException) { Check(true, "public metadata API refuses unencrypted URL"); }
                try { InstalledUpdateTransport.DownloadPackage("http://127.0.0.1/package", target, CancellationToken.None); throw new Exception("Production HTTP accepted."); }
                catch (ArgumentException) { Check(!File.Exists(target), "public package API refuses unencrypted URL before file creation"); }
                Console.WriteLine("Installed update transport tests PASS: " + passed);
                return 0;
            }
            catch (Exception error) { Console.WriteLine("FAIL: " + error); return 1; }
            finally { Directory.Delete(root, true); }
        }
    }
}

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
    internal static class UpdateCheckTests
    {
        private static int passed;
        private static void Check(bool value, string name)
        {
            if (!value) throw new Exception(name);
            passed++; Console.WriteLine("PASS: " + name);
        }
        private static T Finish<T>(Task<T> task)
        {
            if (Task.WhenAny(task, Task.Delay(5000)).GetAwaiter().GetResult() != task)
                throw new Exception("Update check did not finish within the fixture watchdog.");
            return task.GetAwaiter().GetResult();
        }
        private sealed class Server : IDisposable
        {
            private readonly TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            private readonly ManualResetEventSlim stop = new ManualResetEventSlim();
            internal readonly ManualResetEventSlim Requested = new ManualResetEventSlim();
            internal readonly ManualResetEventSlim ResponseStarted = new ManualResetEventSlim();
            private readonly Task worker;
            internal string Request;
            internal Uri Endpoint;
            private TcpClient client;
            internal Server(string body, string mode = "complete", int status = 200, int requests = 1)
            {
                listener.Start();
                Endpoint = new Uri("http://" + IPAddress.Loopback + ":" + ((IPEndPoint)listener.LocalEndpoint).Port + "/release");
                var accept = listener.AcceptTcpClientAsync();
                worker = Task.Run(async delegate {
                    try
                    {
                        for (int round = 0; round < requests; round++)
                        using (var accepted = await (round == 0 ? accept : listener.AcceptTcpClientAsync()).ConfigureAwait(false))
                        {
                            client = accepted; accepted.ReceiveTimeout = 5000;
                            using (var stream = accepted.GetStream())
                            {
                                var headers = new StringBuilder();
                                while (!headers.ToString().EndsWith("\r\n\r\n", StringComparison.Ordinal))
                                {
                                    int next = stream.ReadByte();
                                    if (next < 0) throw new IOException("Fixture request ended before headers.");
                                    headers.Append((char)next);
                                    if (headers.Length > 16384) throw new IOException("Fixture request too large.");
                                }
                                Request = headers.ToString(); Requested.Set();
                                if (mode != "headers-stall")
                                {
                                    byte[] payload = Encoding.UTF8.GetBytes(body);
                                    int length = mode == "declared-oversize" ? 2 * 1024 * 1024 + 1 : payload.Length;
                                    string framing = mode == "stream-oversize" ? "Transfer-Encoding: chunked\r\n" : "Content-Length: " + length + "\r\n";
                                    byte[] prefix = Encoding.ASCII.GetBytes("HTTP/1.1 " + (requests > 1 && round == requests - 1 ? 200 : status) + " Fixture\r\nContent-Type: application/json; charset=utf-8\r\n" + framing + "Connection: close\r\n\r\n");
                                    stream.Write(prefix, 0, prefix.Length);
                                    if (mode == "body-stall") stream.Write(payload, 0, 1);
                                    else if (mode == "stream-oversize")
                                    {
                                        var block = Encoding.ASCII.GetBytes("2000\r\n" + new String('x', 8192) + "\r\n");
                                        for (int i = 0; i < 257; i++) stream.Write(block, 0, block.Length);
                                    }
                                    else if (mode != "declared-oversize") stream.Write(payload, 0, payload.Length);
                                    stream.Flush(); ResponseStarted.Set();
                                }
                                if (mode != "complete") stop.Wait(5000);
                            }
                        }
                    }
                    catch (IOException) { if (!stop.IsSet && (Request == null || mode == "complete")) throw; }
                    catch (SocketException) { if (!stop.IsSet) throw; }
                    catch (ObjectDisposedException) { if (!stop.IsSet) throw; }
                });
            }
            public void Dispose()
            {
                stop.Set(); listener.Stop(); if (client != null) client.Close();
                if (!worker.Wait(5000)) throw new Exception("Fixture server did not stop.");
                Requested.Dispose(); ResponseStarted.Dispose(); stop.Dispose();
            }
        }
        private static UpdateCheckResult Reply(string body, string local = "0.2.2", int status = 200)
        {
            using (var server = new Server(body, "complete", status))
            {
                var result = Finish(UpdateLauncher.CheckForUpdateAsync(server.Endpoint, local, 3000, CancellationToken.None));
                Check(server.Request.StartsWith("GET /release HTTP/1.1\r\n", StringComparison.Ordinal), "metadata is fetched with GET");
                Check(server.Request.Contains("User-Agent: ProGo-Updater\r\n") &&
                    server.Request.Contains("Accept: application/vnd.github+json\r\n"), "GitHub request headers preserved");
                Check(result.LocalVersion == local, "installed version preserved in result");
                return result;
            }
        }
        private static void Stalled(string mode, bool cancel)
        {
            using (var server = new Server("{\"tag_name\":\"v0.3.0\"}", mode))
            using (var cancellation = new CancellationTokenSource())
            {
                var clock = Stopwatch.StartNew();
                var task = UpdateLauncher.CheckForUpdateAsync(server.Endpoint, "0.2.2", cancel ? 4000 : 1500, cancellation.Token);
                Check(server.Requested.Wait(3000), mode + " request reached actual socket");
                if (mode == "body-stall") Check(server.ResponseStarted.Wait(3000), "partial response body arrived before cancellation/deadline");
                if (cancel)
                {
                    cancellation.Cancel();
                    try { Finish(task); throw new Exception("Cancellation returned a normal update result."); }
                    catch (OperationCanceledException ex)
                    {
                        Check(ex.CancellationToken == cancellation.Token, mode + " retains caller cancellation identity");
                        Check(task.IsCanceled, mode + " cancellation is a canceled task, not a failure result");
                    }
                }
                else
                {
                    var result = Finish(task);
                    Check(result.Availability == UpdateAvailability.Error && result.RemoteVersion == null,
                        mode + " timeout never advertises an update");
                    Check(result.ErrorMessage.Contains("Время ожидания") && result.ErrorMessage.Contains("Повторите"),
                        mode + " timeout explains retry without endpoint details");
                }
                Check(clock.ElapsedMilliseconds < 4000, mode + " finishes within a bounded wait");
            }
        }
        private static int Main()
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true")
            { Console.WriteLine("SKIP: update check transport fixture requires isolated Windows CI"); return 0; }
            var beforeSettings = File.Exists(AppPaths.SettingsPath) ? File.ReadAllBytes(AppPaths.SettingsPath) : null;
            var beforeVault = File.Exists(AppPaths.VaultPath) ? File.ReadAllBytes(AppPaths.VaultPath) : null;
            try
            {
                var newer = Reply("{\"tag_name\":\" v0.3.0 \",\"body\":\"Описание\"}");
                Check(newer.Availability == UpdateAvailability.Available && newer.RemoteVersion == "0.3.0", "newer normalized version is available");
                Check(Reply("\uFEFF{\"tag_name\":\"0.2.2\"}").Availability == UpdateAvailability.UpToDate, "equal version is current");
                Check(Reply("{\"tag_name\":\"0.1.0\"}").Availability == UpdateAvailability.UpToDate, "older release never offers downgrade");
                Check(Reply("{\"tag_name\":\"bad\"}").Availability == UpdateAvailability.Error, "invalid remote version rejected");
                Check(Reply("null").Availability == UpdateAvailability.Error, "null metadata rejected");
                Check(Reply("{bad-json").Availability == UpdateAvailability.Error, "malformed metadata rejected");
                Check(Reply("{\"tag_name\":\"0.3.0\"}", "bad").Availability == UpdateAvailability.Error, "invalid installed version rejected");
                var failure = Reply("fixture private detail", "0.2.2", 503);
                Check(failure.Availability == UpdateAvailability.Error && !failure.ErrorMessage.Contains("fixture private detail"), "HTTP failure is a generic retryable error");
                using (var server = new Server("{\"tag_name\":\"0.3.0\"}", "complete", 503, 4))
                    for (int attempt = 0; attempt < 4; attempt++)
                    {
                        var retry = Finish(UpdateLauncher.CheckForUpdateAsync(server.Endpoint, "0.2.2", 3000, CancellationToken.None));
                        Check(retry.Availability == (attempt < 3 ? UpdateAvailability.Error : UpdateAvailability.Available),
                            "same endpoint retry " + attempt + " does not retain failed HTTP responses");
                    }
                foreach (string mode in new[] { "headers-stall", "body-stall" })
                {
                    Stalled(mode, false); Stalled(mode, true);
                    Check(Reply("{\"tag_name\":\"0.3.0\"}").Availability == UpdateAvailability.Available, mode + " can retry after timeout and cancellation");
                }
                using (var server = new Server("{\"tag_name\":\"0.3.0\"}"))
                using (var cancellation = new CancellationTokenSource())
                {
                    cancellation.Cancel();
                    try { Finish(UpdateLauncher.CheckForUpdateAsync(server.Endpoint, "0.2.2", 3000, cancellation.Token)); throw new Exception("Pre-canceled check completed."); }
                    catch (OperationCanceledException) { Check(!server.Requested.IsSet, "pre-canceled check sends no request"); }
                }
                foreach (string mode in new[] { "declared-oversize", "stream-oversize" })
                    using (var server = new Server("", mode))
                    {
                        var result = Finish(UpdateLauncher.CheckForUpdateAsync(server.Endpoint, "0.2.2", 3000, CancellationToken.None));
                        Check(result.Availability == UpdateAvailability.Error && result.RemoteVersion == null, mode + " metadata is bounded and rejected");
                    }
                Check(Same(beforeSettings, AppPaths.SettingsPath), "update checks preserve settings bytes and existence");
                Check(Same(beforeVault, AppPaths.VaultPath), "update checks preserve vault bytes and existence");
                Console.WriteLine("Update check tests PASS: " + passed); return 0;
            }
            catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        }
        private static bool Same(byte[] before, string path)
        {
            if (before == null) return !File.Exists(path);
            if (!File.Exists(path)) return false;
            return Convert.ToBase64String(before) == Convert.ToBase64String(File.ReadAllBytes(path));
        }
    }
}

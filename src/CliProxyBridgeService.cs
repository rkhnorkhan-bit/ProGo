using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace ProGo
{
    internal sealed class CliProxyBridgeService : IDisposable
    {
        public const string Host = "127.0.0.1";
        public const int Port = 1881;
        private const int BufferSize = 32 * 1024;
        private const int ConnectTimeoutMs = 10000;
        // Zero means no read/write timeout. Long-lived Codex WebSocket streams must not be cut after idle periods.
        private const int SocketTimeoutMs = 0;

        private readonly SettingsService settings;
        private TcpListener listener;
        private Thread acceptThread;
        private volatile bool running;
        private readonly HashSet<TcpClient> clients = new HashSet<TcpClient>();

        public CliProxyBridgeService(SettingsService settingsService)
        {
            settings = settingsService;
        }

        public bool IsRunning
        {
            get { return running; }
        }

        public string ProxyUrl
        {
            get { return "http://" + Host + ":" + Port; }
        }

        public bool Start(out string message)
        {
            message = null;
            if (running) return true;

            try
            {
                listener = new TcpListener(IPAddress.Loopback, Port);
                listener.Start(64);
                running = true;
                var server = listener;
                acceptThread = new Thread(delegate() { AcceptLoop(server); });
                acceptThread.IsBackground = true;
                acceptThread.Name = "ProGo CLI proxy bridge";
                acceptThread.Start();
                SafeLog.Info("CLI HTTP CONNECT proxy started. listen=" + Host + ":" + Port + "; socks=" + settings.Current.SocksHost + ":" + settings.Current.SocksPort + ".");
                return true;
            }
            catch (Exception ex)
            {
                running = false;
                try { if (listener != null) listener.Stop(); } catch { }
                listener = null;
                SafeLog.Error("CLI HTTP CONNECT proxy start failed.", ex);
                message = "Не удалось запустить CLI/Codex proxy на " + Host + ":" + Port + ". Проверьте, что порт свободен.";
                return false;
            }
        }

        public void Stop()
        {
            running = false;
            try { if (listener != null) listener.Stop(); } catch { }
            listener = null;
            lock (clients) { foreach (var client in clients) try { client.Close(); } catch { } clients.Clear(); }
            SafeLog.Info("CLI HTTP CONNECT proxy stopped.");
        }

        private void AcceptLoop(TcpListener server)
        {
            while (running && ReferenceEquals(listener, server))
            {
                try
                {
                    var client = server.AcceptTcpClient();
                    lock (clients)
                    {
                        if (!running || clients.Count >= 128) { client.Close(); continue; }
                        clients.Add(client);
                    }
                    client.NoDelay = true;
                    client.ReceiveTimeout = ConnectTimeoutMs;
                    client.SendTimeout = SocketTimeoutMs;
                    ThreadPool.QueueUserWorkItem(HandleClient, client);
                }
                catch (SocketException)
                {
                    if (running) Thread.Sleep(250);
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    if (running) SafeLog.Error("CLI proxy accept failed.", ex);
                    Thread.Sleep(250);
                }
            }
        }

        private void HandleClient(object state)
        {
            var client = state as TcpClient;
            if (client == null) return;

            TcpClient upstream = null;
            try
            {
                using (client)
                {
                    var clientStream = client.GetStream();
                    var header = ReadHttpHeader(clientStream);
                    string targetHost;
                    int targetPort;
                    bool connect = TryParseConnectTarget(header, out targetHost, out targetPort);
                    string forwardHeader = null;
                    if (!connect && !TryParseHttpTarget(header, out targetHost, out targetPort, out forwardHeader))
                    {
                        WriteAscii(clientStream, "HTTP/1.1 400 Bad Request\r\nConnection: close\r\n\r\n");
                        return;
                    }
                    upstream = ConnectThroughSocks(targetHost, targetPort);
                    client.ReceiveTimeout = SocketTimeoutMs;
                    if (connect) WriteAscii(clientStream, "HTTP/1.1 200 Connection Established\r\n\r\n");
                    else WriteAscii(upstream.GetStream(), forwardHeader);
                    Relay(client, upstream);
                }
            }
            catch (Exception ex)
            {
                SafeLog.Error("CLI proxy request failed.", ex);
                try
                {
                    if (client.Connected) WriteAscii(client.GetStream(), "HTTP/1.1 502 Bad Gateway\r\nConnection: close\r\n\r\n");
                }
                catch { }
            }
            finally
            {
                try { if (upstream != null) upstream.Close(); } catch { }
                lock (clients) clients.Remove(client);
            }
        }

        private static string ReadHttpHeader(NetworkStream stream)
        {
            var bytes = new byte[8192];
            var count = 0;
            while (count < bytes.Length)
            {
                var value = stream.ReadByte();
                if (value < 0) break;
                bytes[count++] = (byte)value;
                if (count >= 4 && bytes[count - 4] == '\r' && bytes[count - 3] == '\n' && bytes[count - 2] == '\r' && bytes[count - 1] == '\n')
                {
                    break;
                }
            }

            if (count < 4 || bytes[count - 4] != '\r' || bytes[count - 3] != '\n' || bytes[count - 2] != '\r' || bytes[count - 1] != '\n') throw new IOException("Incomplete or oversized HTTP header.");
            return Encoding.ASCII.GetString(bytes, 0, count);
        }

        private static bool TryParseConnectTarget(string header, out string host, out int port)
        {
            host = null;
            port = 0;
            if (String.IsNullOrWhiteSpace(header)) return false;

            var firstLineEnd = header.IndexOf("\r\n", StringComparison.Ordinal);
            var firstLine = firstLineEnd >= 0 ? header.Substring(0, firstLineEnd) : header;
            var parts = firstLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3) return false;
            if (!String.Equals(parts[0], "CONNECT", StringComparison.OrdinalIgnoreCase)) return false;

            var target = parts[1].Trim();
            string portText;

            if (target.StartsWith("[", StringComparison.Ordinal))
            {
                var close = target.IndexOf(']');
                if (close < 0 || close + 2 > target.Length || target[close + 1] != ':') return false;
                host = target.Substring(1, close - 1);
                portText = target.Substring(close + 2);
            }
            else
            {
                var colon = target.LastIndexOf(':');
                if (colon <= 0 || colon >= target.Length - 1) return false;
                host = target.Substring(0, colon);
                portText = target.Substring(colon + 1);
            }

            if (String.IsNullOrWhiteSpace(host)) return false;
            if (!Int32.TryParse(portText, out port)) return false;
            return port > 0 && port <= 65535;
        }

        internal static bool TryParseHttpTarget(string header, out string host, out int port, out string forwarded)
        {
            host = null; port = 0; forwarded = null;
            if (header == null || !header.EndsWith("\r\n\r\n", StringComparison.Ordinal)) return false;
            var lines = header.Split(new[] { "\r\n" }, StringSplitOptions.None);
            var request = lines[0].Split(' ');
            if (request.Length != 3 || request[0] == "CONNECT" || (request[2] != "HTTP/1.1" && request[2] != "HTTP/1.0")) return false;
            foreach (char c in request[0]) if (c < 'A' || c > 'Z') return false;
            Uri uri;
            if (!Uri.TryCreate(request[1], UriKind.Absolute, out uri) || uri.Scheme != "http" || !String.IsNullOrEmpty(uri.UserInfo) || !String.IsNullOrEmpty(uri.Fragment)) return false;
            host = uri.DnsSafeHost; port = uri.Port;
            var result = new StringBuilder(request[0] + " " + uri.PathAndQuery + " " + request[2] + "\r\nHost: " + uri.Authority + "\r\nConnection: close\r\n");
            for (int i = 1; i < lines.Length && lines[i].Length > 0; i++)
            {
                int colon = lines[i].IndexOf(':'); if (colon <= 0 || Char.IsWhiteSpace(lines[i][0])) return false;
                string name = lines[i].Substring(0, colon);
                if (name.Equals("Host", StringComparison.OrdinalIgnoreCase) || name.Equals("Connection", StringComparison.OrdinalIgnoreCase) || name.StartsWith("Proxy-", StringComparison.OrdinalIgnoreCase)) continue;
                result.Append(lines[i]).Append("\r\n");
            }
            result.Append("\r\n"); forwarded = result.ToString(); return true;
        }

        private TcpClient ConnectThroughSocks(string targetHost, int targetPort)
        {
            var current = settings.Current;
            var socks = new TcpClient();
            socks.NoDelay = true;
            socks.ReceiveTimeout = ConnectTimeoutMs;
            socks.SendTimeout = SocketTimeoutMs;
            try
            {
            ConnectWithTimeout(socks, current.SocksHost, current.SocksPort, ConnectTimeoutMs);

            var stream = socks.GetStream();
            stream.Write(new byte[] { 0x05, 0x01, 0x00 }, 0, 3);

            var greetingReply = new byte[2];
            ReadExact(stream, greetingReply, 0, 2);
            if (greetingReply[0] != 0x05 || greetingReply[1] != 0x00)
            {
                socks.Close();
                throw new InvalidOperationException("SOCKS server rejected no-auth handshake.");
            }

            var request = BuildSocksConnectRequest(targetHost, targetPort);
            stream.Write(request, 0, request.Length);

            var replyHeader = new byte[4];
            ReadExact(stream, replyHeader, 0, 4);
            if (replyHeader[0] != 0x05 || replyHeader[1] != 0x00)
            {
                socks.Close();
                throw new InvalidOperationException("SOCKS connect failed. rep=0x" + replyHeader[1].ToString("x2"));
            }

            var addressBytes = 0;
            if (replyHeader[3] == 0x01)
            {
                addressBytes = 4;
            }
            else if (replyHeader[3] == 0x04)
            {
                addressBytes = 16;
            }
            else if (replyHeader[3] == 0x03)
            {
                var length = new byte[1];
                ReadExact(stream, length, 0, 1);
                addressBytes = length[0];
            }
            else
            {
                socks.Close();
                throw new InvalidOperationException("SOCKS reply used unsupported address type.");
            }

            var remainder = new byte[addressBytes + 2];
            ReadExact(stream, remainder, 0, remainder.Length);
            socks.ReceiveTimeout = SocketTimeoutMs;
            return socks;
            }
            catch { socks.Close(); throw; }
        }

        private static byte[] BuildSocksConnectRequest(string targetHost, int targetPort)
        {
            IPAddress ip;
            byte atyp;
            byte[] address;
            if (IPAddress.TryParse(targetHost, out ip))
            {
                address = ip.GetAddressBytes();
                atyp = address.Length == 4 ? (byte)0x01 : (byte)0x04;
            }
            else
            {
                address = Encoding.ASCII.GetBytes(targetHost);
                if (address.Length < 1 || address.Length > 255) throw new InvalidOperationException("Target host name is too long for SOCKS5.");
                atyp = 0x03;
            }

            var extra = atyp == 0x03 ? 1 : 0;
            var request = new byte[4 + extra + address.Length + 2];
            request[0] = 0x05;
            request[1] = 0x01;
            request[2] = 0x00;
            request[3] = atyp;
            var offset = 4;
            if (atyp == 0x03)
            {
                request[offset++] = (byte)address.Length;
            }
            Buffer.BlockCopy(address, 0, request, offset, address.Length);
            offset += address.Length;
            request[offset++] = (byte)((targetPort >> 8) & 0xff);
            request[offset] = (byte)(targetPort & 0xff);
            return request;
        }

        private static void ConnectWithTimeout(TcpClient client, string host, int port, int timeoutMs)
        {
            var ar = client.BeginConnect(host, port, null, null);
            if (!ar.AsyncWaitHandle.WaitOne(timeoutMs))
            {
                try { client.Close(); } catch { }
                throw new TimeoutException("Timed out connecting to SOCKS endpoint.");
            }
            client.EndConnect(ar);
        }

        private static void Relay(TcpClient left, TcpClient right)
        {
            var done = new ManualResetEvent(false);
            ThreadPool.QueueUserWorkItem(delegate { CopyAndSignal(left, right, done); });
            ThreadPool.QueueUserWorkItem(delegate { CopyAndSignal(right, left, done); });
            done.WaitOne();
            try { left.Close(); } catch { }
            try { right.Close(); } catch { }
        }

        private static void CopyAndSignal(TcpClient sourceClient, TcpClient destinationClient, ManualResetEvent done)
        {
            try
            {
                var source = sourceClient.GetStream();
                var destination = destinationClient.GetStream();
                var buffer = new byte[BufferSize];
                while (true)
                {
                    var read = source.Read(buffer, 0, buffer.Length);
                    if (read <= 0) break;
                    destination.Write(buffer, 0, read);
                }
            }
            catch
            {
            }
            finally
            {
                done.Set();
            }
        }

        private static void ReadExact(NetworkStream stream, byte[] buffer, int offset, int count)
        {
            var read = 0;
            while (read < count)
            {
                var current = stream.Read(buffer, offset + read, count - read);
                if (current <= 0) throw new EndOfStreamException("Unexpected end of stream.");
                read += current;
            }
        }

        private static void WriteAscii(Stream stream, string text)
        {
            var bytes = Encoding.ASCII.GetBytes(text);
            stream.Write(bytes, 0, bytes.Length);
        }

        private static string SafeTarget(string host, int port)
        {
            if (String.IsNullOrWhiteSpace(host)) return "unknown";
            return host + ":" + port;
        }

        public void Dispose()
        {
            Stop();
        }
    }

    internal static class CliProxyEnvironmentService
    {
        private const int HWND_BROADCAST = 0xffff;
        private const int WM_SETTINGCHANGE = 0x001A;
        private const int SMTO_ABORTIFHUNG = 0x0002;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SendMessageTimeout(IntPtr hWnd, int Msg, IntPtr wParam, string lParam, int fuFlags, int uTimeout, out IntPtr lpdwResult);

        public static string ProxyUrl
        {
            get { return "http://" + CliProxyBridgeService.Host + ":" + CliProxyBridgeService.Port; }
        }

        private static string BackupPath { get { return Path.Combine(AppPaths.Root, "proxy-environment-backup.json"); } }
        private static readonly string[] Names = { "ALL_PROXY", "HTTPS_PROXY", "HTTP_PROXY", "NO_PROXY" };
        private static string Expected(string name) { return name == "NO_PROXY" ? "localhost,127.0.0.1,::1" : ProxyUrl; }
        public static void ApplyUserEnvironment()
        {
            AppPaths.EnsureDirectories();
            if (!File.Exists(BackupPath))
            {
                var saved = new Dictionary<string, string>();
                foreach (var name in Names)
                {
                    string value = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User);
                    saved[name] = String.Equals(value, Expected(name), StringComparison.OrdinalIgnoreCase) ? null : value;
                }
                File.WriteAllText(BackupPath, new JavaScriptSerializer().Serialize(saved));
            }
            foreach (var name in Names) SetUser(name, Expected(name));
            BroadcastEnvironmentChange();
            SafeLog.Info("Proxy environment applied for new terminals.");
        }

        public static bool IsAppliedToUserEnvironment()
        {
            return IsUserValue("ALL_PROXY", ProxyUrl) ||
                   IsUserValue("HTTPS_PROXY", ProxyUrl) ||
                   IsUserValue("HTTP_PROXY", ProxyUrl) ||
                   IsUserValue("all_proxy", ProxyUrl) ||
                   IsUserValue("https_proxy", ProxyUrl) ||
                   IsUserValue("http_proxy", ProxyUrl);
        }

        public static void ClearUserEnvironmentIfOwned()
        {
            Dictionary<string, string> saved = null;
            if (File.Exists(BackupPath)) saved = new JavaScriptSerializer().Deserialize<Dictionary<string, string>>(File.ReadAllText(BackupPath));
            // Windows environment names are case-insensitive. Restore only values still owned by ProGo.
            foreach (var name in Names)
            {
                string previous = null;
                if (saved != null) saved.TryGetValue(name, out previous);
                if (IsUserValue(name, Expected(name))) Environment.SetEnvironmentVariable(name, previous, EnvironmentVariableTarget.User);
            }
            if (File.Exists(BackupPath)) File.Delete(BackupPath);
            BroadcastEnvironmentChange();
            SafeLog.Info("Previous proxy environment restored where still owned by ProGo.");
        }

        public static bool OpenPowerShellWithEnvironment(out string message)
        {
            message = null;
            var powerShell = ResolvePowerShell();
            try
            {
                var psi = new ProcessStartInfo(powerShell)
                {
                    UseShellExecute = false,
                    WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    Arguments = "-NoExit -Command \"Write-Host 'ProGo CLI proxy is active: " + ProxyUrl + "'; Get-ChildItem Env: | Where-Object { $_.Name -match 'proxy' } | Sort-Object Name\""
                };
                ApplyProcessEnvironment(psi);
                Process.Start(psi);
                SafeLog.Info("PowerShell opened with CLI proxy environment. proxy=" + ProxyUrl + ".");
                return true;
            }
            catch (Exception ex)
            {
                SafeLog.Error("Open PowerShell with CLI proxy failed.", ex);
                message = "Не удалось открыть PowerShell с CLI proxy environment.";
                return false;
            }
        }

        internal static void ApplyProcessEnvironment(ProcessStartInfo psi)
        {
            psi.EnvironmentVariables["ALL_PROXY"] = ProxyUrl;
            psi.EnvironmentVariables["HTTPS_PROXY"] = ProxyUrl;
            psi.EnvironmentVariables["HTTP_PROXY"] = ProxyUrl;
            psi.EnvironmentVariables["all_proxy"] = ProxyUrl;
            psi.EnvironmentVariables["https_proxy"] = ProxyUrl;
            psi.EnvironmentVariables["http_proxy"] = ProxyUrl;
            psi.EnvironmentVariables["NO_PROXY"] = "localhost,127.0.0.1,::1";
            psi.EnvironmentVariables["no_proxy"] = "localhost,127.0.0.1,::1";
        }

        private static string ResolvePowerShell()
        {
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var pwsh = Path.Combine(programFiles, "PowerShell", "7", "pwsh.exe");
            if (File.Exists(pwsh)) return pwsh;

            var windowsPowerShell = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "WindowsPowerShell", "v1.0", "powershell.exe");
            if (File.Exists(windowsPowerShell)) return windowsPowerShell;

            return "powershell.exe";
        }

        private static void SetUser(string name, string value)
        {
            Environment.SetEnvironmentVariable(name, value, EnvironmentVariableTarget.User);
        }

        private static bool IsUserValue(string name, string expected)
        {
            var value = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User);
            return String.Equals(value, expected, StringComparison.OrdinalIgnoreCase);
        }

        private static void ClearUserIfOwned(string name, string expected)
        {
            if (IsUserValue(name, expected))
            {
                Environment.SetEnvironmentVariable(name, null, EnvironmentVariableTarget.User);
            }
        }

        private static void BroadcastEnvironmentChange()
        {
            IntPtr result;
            SendMessageTimeout((IntPtr)HWND_BROADCAST, WM_SETTINGCHANGE, IntPtr.Zero, "Environment", SMTO_ABORTIFHUNG, 5000, out result);
        }
    }
}

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
using Microsoft.Win32;

namespace ProGo
{
    internal enum BridgeTransitionPhase { BeforeCommit, AfterCommit }

    internal sealed class CliProxyBridgeService : IDisposable
    {
        public const string Host = "127.0.0.1";
        private const int BufferSize = 32 * 1024;
        private const int ConnectTimeoutMs = 10000;
        // Zero means no read/write timeout. Long-lived Codex WebSocket streams must not be cut after idle periods.
        private const int SocketTimeoutMs = 0;

        private readonly SettingsService settings;
        private readonly Action<BridgeTransitionPhase> transitionProbe;
        private volatile ListenerSession active;
        private long consumerRevision;
        internal long ConsumerRevision { get { return Interlocked.Read(ref consumerRevision); } }
        private readonly HashSet<TcpClient> clients = new HashSet<TcpClient>();
        private readonly object retentionGate = new object();
        private int nativeMutationRetentions;
        private readonly List<PreparedBridgeConfiguration> pendingCleanup = new List<PreparedBridgeConfiguration>();
        internal bool CleanupPending { get { lock (retentionGate) return pendingCleanup.Count != 0; } }
        internal int[] RetainedPorts {
            get { lock (retentionGate) {
                var ports = new List<int>(); var current = active;
                if (current != null) ports.Add(current.Port);
                foreach (var value in pendingCleanup) if (value.Candidate != null && !ports.Contains(value.Candidate.Port)) ports.Add(value.Candidate.Port);
                return ports.ToArray();
            } }
        }
        internal IDisposable RetainForNativeMutation()
        {
            lock (retentionGate) nativeMutationRetentions++;
            return new NativeRetention(this);
        }
        private sealed class NativeRetention : IDisposable
        {
            private CliProxyBridgeService owner;
            internal NativeRetention(CliProxyBridgeService owner) { this.owner = owner; }
            public void Dispose() {
                var value = Interlocked.Exchange(ref owner, null);
                if (value != null) lock (value.retentionGate) value.nativeMutationRetentions--;
            }
        }
        internal sealed class ListenerSession
        {
            internal readonly TcpListener Server;
            internal readonly int Port;
            internal volatile bool Running;
            internal ListenerSession(TcpListener server) { Server = server; Port = ((IPEndPoint)server.LocalEndpoint).Port; }
            internal void Stop() { Running = false; try { Server.Stop(); } catch { } }
        }
        internal sealed class PreparedBridgeConfiguration
        {
            internal ListenerSession Candidate;
            internal ProxyIntegrationState Integrations;
            internal SettingsCommitReceipt Commit;
            internal bool KeepRunning;
            internal int Published;
        }
        public CliProxyBridgeService(SettingsService settingsService, Action<BridgeTransitionPhase> transitionProbe = null)
        { settings = settingsService; this.transitionProbe = transitionProbe; }
        public bool IsRunning { get { var value = active; return value != null && value.Running; } }
        public int Port { get { var value = active; return value == null ? settings.Current.HttpProxyPort : value.Port; } }
        public string ProxyUrl { get { return UrlFor(Port); } }
        public static string UrlFor(int port)
        {
            if (port < 1 || port > 65535) throw new ArgumentOutOfRangeException("port");
            return "http://" + Host + ":" + port;
        }
        public bool Start(out string message)
        {
            message = null; if (IsRunning) return true;
            SettingsSaveError error; var expected = settings.Capture();
            var prepared = PrepareConfiguration(expected.Settings, false, true, expected, false, CancellationToken.None, out error);
            if (prepared != null) PublishPrepared(prepared);
            message = error == null ? null : error.Message; return prepared != null;
        }
        public bool Reconfigure(AppSettings proposed, bool pickFreePort, out string message)
        {
            SettingsSaveError error; bool applied = ReconfigureDetailed(proposed, pickFreePort, out error);
            message = error == null ? null : error.Message; return applied;
        }
        internal bool ReconfigureDetailed(AppSettings proposed, bool pickFreePort, out SettingsSaveError error)
        {
            var prepared = PrepareConfiguration(proposed, pickFreePort, IsRunning, settings.Capture(), true, CancellationToken.None, out error);
            if (prepared != null) PublishPrepared(prepared);
            return prepared != null;
        }
        private static TcpListener Bind(int port)
        {
            var server = new TcpListener(IPAddress.Loopback, port);
            try { server.ExclusiveAddressUse = true; server.Start(64); return server; }
            catch { server.Stop(); throw; }
        }
        internal PreparedBridgeConfiguration PrepareConfiguration(AppSettings input, bool pickFreePort, bool keepRunning,
            SettingsRevisionSnapshot expected, bool persist, CancellationToken token, out SettingsSaveError error)
        {
            error = SettingsValidation.Check(input); if (error != null) return null;
            if (token.IsCancellationRequested) { error = new SettingsSaveError(SettingsField.General, "Применение настроек отменено."); return null; }
            if (!RetryPendingCleanup(out error)) return null;
            if (!IsRunning) {
                try {
                    var recovery = ProxyIntegrationState.ReadPortRecoveryMessage(expected.Settings.HttpProxyPort, token);
                    if (recovery != null) { error = new SettingsSaveError(SettingsField.Applications, recovery); return null; }
                } catch (OperationCanceledException) { error = new SettingsSaveError(SettingsField.General, "Применение настроек отменено."); return null; }
                catch (Exception ex) {
                    SafeLog.Error("Owned proxy port recovery check failed.", ex);
                    error = new SettingsSaveError(SettingsField.Applications, "Не удалось проверить копии восстановления прокси. Проверьте доступ к ним; новое подключение не запущено."); return null;
                }
            }
            var proposed = input.Clone(); var prepared = new PreparedBridgeConfiguration { KeepRunning = keepRunning };
            var failedField = SettingsField.HttpProxyPort;
            try {
                token.ThrowIfCancellationRequested(); UrlFor(proposed.HttpProxyPort);
                bool reuse = IsRunning && !pickFreePort && proposed.HttpProxyPort == Port;
                if (!reuse) {
                    TcpListener server;
                    try { server = Bind(pickFreePort ? 0 : proposed.HttpProxyPort); }
                    catch (SocketException ex) {
                        if (!proposed.AutoHttpProxyPort || (ex.SocketErrorCode != SocketError.AddressAlreadyInUse && ex.SocketErrorCode != SocketError.AccessDenied)) throw;
                        server = Bind(0);
                    }
                    prepared.Candidate = new ListenerSession(server); proposed.HttpProxyPort = prepared.Candidate.Port;
                    // Start before changing integrations or committing settings. The reserved
                    // endpoint can already serve requests, but Port still publishes the old one.
                    var session = prepared.Candidate; session.Running = true;
                    var thread = new Thread(delegate() { AcceptLoop(session); });
                    thread.IsBackground = true; thread.Name = "ProGo app proxy"; thread.Start();
                }
                if (proposed.HttpProxyPort != expected.Settings.HttpProxyPort) {
                    failedField = SettingsField.Applications; prepared.Integrations = ProxyIntegrationState.Capture();
                    prepared.Integrations.MoveOwned(proposed.HttpProxyPort, token);
                }
                if (transitionProbe != null) transitionProbe(BridgeTransitionPhase.BeforeCommit);
                token.ThrowIfCancellationRequested(); failedField = SettingsField.SettingsFile;
                if (persist || proposed.HttpProxyPort != expected.Settings.HttpProxyPort) {
                    if (!settings.TrySave(expected.Revision, proposed, out prepared.Commit))
                        throw new SettingsRevisionConflictException();
                } else if (settings.Current.HttpProxyPort != proposed.HttpProxyPort) throw new SettingsRevisionConflictException();
                // Durable commit is the cancellation boundary. Finish publication even
                // when Cancel or forced owner disposal arrives after this point.
                if (transitionProbe != null) try { transitionProbe(BridgeTransitionPhase.AfterCommit); }
                    catch (Exception ex) { SafeLog.Error("Bridge completion probe failed after commit.", ex); }
                return prepared;
            } catch (Exception ex) {
                bool cleanupFailed = false;
                if (prepared.Integrations != null) try { prepared.Integrations.Restore(); }
                    catch (Exception rollback) { cleanupFailed = true; SafeLog.Error("Owned proxy transition cleanup incomplete.", rollback); }
                if (cleanupFailed) { lock (retentionGate) pendingCleanup.Add(prepared); }
                else if (prepared.Candidate != null) prepared.Candidate.Stop();
                var socket = ex as SocketException; string message;
                if (cleanupFailed) { failedField = SettingsField.Applications;
                    message = "Перенос прокси не завершён: часть собственных изменений ещё не восстановлена. Порты сохранены: " +
                        String.Join(", ", Array.ConvertAll(RetainedPorts, value => value.ToString(System.Globalization.CultureInfo.InvariantCulture))) +
                        ". Проверьте права записи и повторите команду; ProGo сначала завершит очистку.";
                } else if (ex is OperationCanceledException) message = "Применение настроек отменено. Позднейшие изменения вне ProGo сохранены.";
                else if (ex is SettingsRevisionConflictException) message = "Настройки изменились во время сохранения. Более свежие значения сохранены; проверьте текущие параметры и повторите сохранение.";
                else if (failedField == SettingsField.HttpProxyPort && socket != null && (socket.SocketErrorCode == SocketError.AddressAlreadyInUse || socket.SocketErrorCode == SocketError.AccessDenied))
                    message = "Порт " + proposed.HttpProxyPort + " занят или зарезервирован Windows. Нажмите «Подобрать свободный» или включите автоматический выбор.";
                else if (failedField == SettingsField.SettingsFile) message = "Не удалось сохранить файл настроек. Проверьте доступ к папке приложения и не открыт ли файл другой программой. Подробности — в журнале ProGo.";
                else if (failedField == SettingsField.Applications) message = "Не удалось обновить настройки приложений. Собственные изменения восстановлены; позднейшие внешние настройки сохранены. Подробности — в журнале ProGo.";
                else message = "Не удалось применить настройки. Проверьте текущее состояние и журнал ProGo.";
                if (!(ex is OperationCanceledException) && !(ex is SettingsRevisionConflictException)) SafeLog.Error("Application proxy configuration failed.", ex);
                error = new SettingsSaveError(failedField, message); return null;
            } finally { Interlocked.Increment(ref consumerRevision); }
        }
        private sealed class SettingsRevisionConflictException : Exception { }
        internal bool RetryPendingCleanup(out SettingsSaveError error)
        {
            error = null; PreparedBridgeConfiguration[] pending;
            lock (retentionGate) pending = pendingCleanup.ToArray();
            foreach (var value in pending) {
                try {
                    value.Integrations.Restore();
                    if (value.Candidate != null) value.Candidate.Stop();
                    lock (retentionGate) pendingCleanup.Remove(value);
                    Interlocked.Increment(ref consumerRevision);
                } catch (Exception ex) {
                    SafeLog.Error("Pending port transition cleanup failed.", ex);
                    error = new SettingsSaveError(SettingsField.Applications, "Очистка прежнего переноса прокси ещё не завершена. Порты и копии сохранены; проверьте права записи и повторите команду."); return false;
                }
            }
            return true;
        }
        internal void PublishPrepared(PreparedBridgeConfiguration value)
        {
            if (value == null || Interlocked.Exchange(ref value.Published, 1) != 0) return;
            lock (retentionGate) {
                if (value.Candidate != null) {
                    if (value.KeepRunning) { var old = active; active = value.Candidate; if (old != null) old.Stop(); }
                    else value.Candidate.Stop();
                }
            }
            Interlocked.Increment(ref consumerRevision);
        }
        public void Stop()
        {
            lock (retentionGate) {
                if (pendingCleanup.Count != 0) return;
                var value = active; active = null; if (value != null) value.Stop();
            }
            Interlocked.Increment(ref consumerRevision);
            lock (clients) { foreach (var client in clients) try { client.Close(); } catch { } clients.Clear(); }
            SafeLog.Info("CLI HTTP CONNECT proxy stopped.");
        }
        private void AcceptLoop(ListenerSession session)
        {
            while (session.Running) {
                try {
                    var client = session.Server.AcceptTcpClient();
                    lock (clients) {
                        if (!session.Running || clients.Count >= 128) { client.Close(); continue; }
                        clients.Add(client);
                    }
                    client.NoDelay = true; client.ReceiveTimeout = ConnectTimeoutMs; client.SendTimeout = SocketTimeoutMs;
                    ThreadPool.QueueUserWorkItem(HandleClient, client);
                } catch (SocketException) { if (session.Running) Thread.Sleep(250); }
                catch (ObjectDisposedException) { return; }
                catch (Exception ex) { if (session.Running) { SafeLog.Error("CLI proxy accept failed.", ex); Thread.Sleep(250); } }
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


        public void Dispose()
        {
            lock (retentionGate) {
                if (nativeMutationRetentions != 0) return;
                Stop();
            }
        }
    }

    internal static class CliProxyEnvironmentService
    {
        private const int HWND_BROADCAST = 0xffff;
        private const int WM_SETTINGCHANGE = 0x001A;
        private const int SMTO_ABORTIFHUNG = 0x0002;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SendMessageTimeout(IntPtr hWnd, int Msg, IntPtr wParam, string lParam, int fuFlags, int uTimeout, out IntPtr lpdwResult);

        internal static string BackupPath { get { return Path.Combine(AppPaths.Root, "proxy-environment-backup.json"); } }
        internal static readonly string[] Names = { "ALL_PROXY", "HTTPS_PROXY", "HTTP_PROXY", "NO_PROXY" };
        private const string OwnedPortKey = "ProGoAppliedPort";
        private const string PendingTypedValuesKey = "ProGoPendingWindowsCorrections";
        private static string Expected(string name, int port) { return name == "NO_PROXY" ? "localhost,127.0.0.1,::1" : CliProxyBridgeService.UrlFor(port); }
        private static Dictionary<string, string> ReadBackup()
        {
            if (!File.Exists(BackupPath)) return null;
            var saved = new JavaScriptSerializer().Deserialize<Dictionary<string, string>>(File.ReadAllText(BackupPath));
            if (saved == null) throw new IOException("Не удалось прочитать прежние настройки прокси.");
            return saved;
        }
        private static int OwnedPort(Dictionary<string, string> saved)
        {
            // 0.2.0 backups are plain dictionaries and always refer to port 1881.
            string value; int port;
            if (saved == null || !saved.TryGetValue(OwnedPortKey, out value)) return 1881;
            if (!Int32.TryParse(value, out port) || port < 1 || port > 65535) throw new IOException("Некорректная запись порта прокси.");
            return port;
        }
        public static void ApplyUserEnvironment(int port)
        {
            ApplyUserEnvironment(port, SystemProxyService.WriteValue, BroadcastEnvironmentChange);
        }
        internal static void ApplyUserEnvironment(int port, Action<RegistryKey, string, WindowsProxyValue> writer, Action notification)
        {
            CliProxyBridgeService.UrlFor(port);
            AppPaths.EnsureDirectories();
            var saved = ReadBackup();
            if (saved == null)
            {
                saved = new Dictionary<string, string>();
                foreach (var name in Names)
                {
                    string value = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User);
                    saved[name] = String.Equals(value, Expected(name, port), StringComparison.OrdinalIgnoreCase) ? null : value;
                }
            }
            var corrections = ReadTypedCorrections(saved);
            if (corrections.Count != 0) {
                try { SystemProxyService.RetryTypedValues(corrections, writer); }
                finally { SaveTypedCorrections(saved, corrections); }
            }
            saved[OwnedPortKey] = port.ToString(System.Globalization.CultureInfo.InvariantCulture);
            bool correctionFailed = PreserveLiveWindowsTypes(saved, delegate {
                foreach (var name in Names) SetUser(name, Expected(name, port));
                notification();
            }, writer);
            if (correctionFailed) throw new IOException("Не удалось сохранить типы настроек Windows после включения CLI. Копия сохранена; повторите выключение после проверки прав записи.");
            SafeLog.Info("Proxy environment applied for new terminals.");
        }

        internal static void MoveOwned(int port)
        {
            var saved = ReadBackup();
            if (saved == null) return;
            int before = OwnedPort(saved);
            foreach (var name in Names)
                if (IsUserValue(name, Expected(name, before))) SetUser(name, Expected(name, port));
            saved[OwnedPortKey] = port.ToString(System.Globalization.CultureInfo.InvariantCulture);
            File.WriteAllText(BackupPath, new JavaScriptSerializer().Serialize(saved));
            BroadcastEnvironmentChange();
        }

        public static bool IsAppliedToUserEnvironment(int port)
        {
            foreach (var name in Names) if (!IsUserValue(name, Expected(name, port))) return false;
            return true;
        }
        internal static bool HasProxyEndpoint(int port)
        {
            foreach (var name in Names) if (name != "NO_PROXY" && IsUserValue(name, Expected(name, port))) return true;
            return false;
        }
        internal static bool IsPartiallyApplied(int port)
        {
            if (IsAppliedToUserEnvironment(port)) return false;
            foreach (var name in Names) if (IsUserValue(name, Expected(name, port))) return true;
            return false;
        }

        public static void ClearUserEnvironmentIfOwned()
        {
            ClearUserEnvironmentIfOwned(SystemProxyService.WriteValue, BroadcastEnvironmentChange);
        }
        // Native regression seams deny a correction or reproduce an observed notification
        // normalization while still executing the real user-environment cleanup.
        internal static void ClearUserEnvironmentIfOwned(Action<RegistryKey, string, WindowsProxyValue> writer,
            Action notification)
        {
            var saved = ReadBackup();
            if (saved == null) return; // A completed cleanup must not reclaim a later matching external value.
            int port = OwnedPort(saved);
            var corrections = ReadTypedCorrections(saved);
            if (corrections.Count != 0) {
                try { SystemProxyService.RetryTypedValues(corrections, writer); }
                finally { SaveTypedCorrections(saved, corrections); }
            }
            bool correctionFailed = PreserveLiveWindowsTypes(saved, delegate {
                // The .NET setter also broadcasts; protect the complete loop, not just the final notification.
                // Windows names are case-insensitive. Restore only values still owned by ProGo.
                foreach (var name in Names) {
                    string previous; saved.TryGetValue(name, out previous);
                    if (IsUserValue(name, Expected(name, port))) SetUser(name, previous);
                }
                notification();
            }, writer);
            if (correctionFailed) throw new IOException("Не удалось сохранить типы настроек Windows после выключения CLI. Копия сохранена; повторите выключение после проверки прав записи.");
            // A failed notification or typed-value correction must retain the retry journal.
            if (File.Exists(BackupPath)) File.Delete(BackupPath);
            SafeLog.Info("Previous proxy environment restored where still owned by ProGo.");
        }

        private static bool PreserveLiveWindowsTypes(Dictionary<string, string> saved, Action notification,
            Action<RegistryKey, string, WindowsProxyValue> writer)
        {
            var failures = new List<WindowsProxyFieldBackup>();
            SystemProxyService.PreserveTypedValues(notification, writer,
                delegate(WindowsProxyFieldBackup correction, Exception failure) {
                    failures.Add(correction);
                    SafeLog.Info("CLI notification typed-value correction pending: " + correction.Name + ".");
                }, delegate(Dictionary<string, WindowsProxyValue> before) {
                    // Persist intended known normalization before the first setter.
                    // A later failed replacement cannot discard these expectations.
                    SaveTypedCorrections(saved, PrepareTypedCorrections(before));
                });
            // Narrow the guard only after the complete correction pass. If this
            // replacement fails, the prepared receipt remains valid across restart.
            SaveTypedCorrections(saved, failures);
            return failures.Count != 0;
        }
        internal static List<WindowsProxyFieldBackup> PrepareTypedCorrections(Dictionary<string, WindowsProxyValue> before)
        {
            var guards = new List<WindowsProxyFieldBackup>();
            foreach (var name in SystemProxyService.FieldNames) {
                var expected = before[name]; WindowsProxyValue normalized = null;
                if (expected.Exists && expected.Kind == RegistryValueKind.ExpandString)
                    normalized = new WindowsProxyValue { Exists = true, Kind = RegistryValueKind.String, Data = expected.Data };
                else if (name == "AutoDetect" && expected.Exists && expected.Kind == RegistryValueKind.DWord &&
                    (expected.Data == "0" || expected.Data == "1")) normalized = new WindowsProxyValue();
                if (normalized != null && SystemProxyService.IsTypedNormalization(name, expected, normalized))
                    guards.Add(new WindowsProxyFieldBackup { Name = name, Original = expected, Applied = normalized, Pending = true });
            }
            return guards;
        }

        internal static List<WindowsProxyFieldBackup> ReadTypedCorrections(Dictionary<string, string> saved)
        {
            string json;
            if (!saved.TryGetValue(PendingTypedValuesKey, out json)) return new List<WindowsProxyFieldBackup>();
            var corrections = new JavaScriptSerializer().Deserialize<List<WindowsProxyFieldBackup>>(json);
            if (corrections == null || corrections.Count > SystemProxyService.FieldNames.Length)
                throw new IOException("Некорректная копия исправлений типов настроек Windows.");
            var names = new HashSet<string>();
            foreach (var correction in corrections) {
                if (correction == null || Array.IndexOf(SystemProxyService.FieldNames, correction.Name) < 0 ||
                    !names.Add(correction.Name) || !SystemProxyService.IsTypedNormalization(correction.Name, correction.Original, correction.Applied))
                    throw new IOException("Некорректная копия исправлений типов настроек Windows.");
            }
            return corrections;
        }
        private static void SaveTypedCorrections(Dictionary<string, string> saved, List<WindowsProxyFieldBackup> corrections)
        {
            var pending = corrections.FindAll(f => f.Pending);
            if (pending.Count == 0) saved.Remove(PendingTypedValuesKey);
            else saved[PendingTypedValuesKey] = new JavaScriptSerializer().Serialize(pending);
            string staging = BackupPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try {
                File.WriteAllText(staging, new JavaScriptSerializer().Serialize(saved));
                if (File.Exists(BackupPath)) File.Replace(staging, BackupPath, null); else File.Move(staging, BackupPath);
            } finally { if (File.Exists(staging)) File.Delete(staging); }
        }

        public static bool OpenPowerShellWithEnvironment(int port, out string message)
        {
            Process process;
            bool opened = OpenPowerShellWithEnvironment(port, out message, out process);
            if (process != null) process.Dispose();
            return opened;
        }
        internal static bool OpenPowerShellWithEnvironment(int port, out string message, out Process process)
        {
            message = null; process = null;
            var proxyUrl = CliProxyBridgeService.UrlFor(port);
            var powerShell = ResolvePowerShell();
            try
            {
                var psi = new ProcessStartInfo(powerShell)
                {
                    UseShellExecute = false,
                    WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    Arguments = "-NoExit -Command \"Write-Host 'ProGo CLI proxy is active: " + proxyUrl + "'; Get-ChildItem Env: | Where-Object { $_.Name -match 'proxy' } | Sort-Object Name\""
                };
                ApplyProcessEnvironment(psi, port);
                process = Process.Start(psi);
                if (process == null) throw new InvalidOperationException("PowerShell process was not created.");
                SafeLog.Info("PowerShell opened with CLI proxy environment. proxy=" + proxyUrl + ".");
                return true;
            }
            catch (Exception ex)
            {
                SafeLog.Error("Open PowerShell with CLI proxy failed.", ex);
                message = "Не удалось открыть PowerShell с CLI proxy environment.";
                return false;
            }
        }

        internal static void ApplyProcessEnvironment(ProcessStartInfo psi, int port)
        {
            var proxyUrl = CliProxyBridgeService.UrlFor(port);
            psi.EnvironmentVariables["ALL_PROXY"] = proxyUrl;
            psi.EnvironmentVariables["HTTPS_PROXY"] = proxyUrl;
            psi.EnvironmentVariables["HTTP_PROXY"] = proxyUrl;
            psi.EnvironmentVariables["all_proxy"] = proxyUrl;
            psi.EnvironmentVariables["https_proxy"] = proxyUrl;
            psi.EnvironmentVariables["http_proxy"] = proxyUrl;
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


        internal static void BroadcastEnvironmentChange()
        {
            IntPtr result;
            SendMessageTimeout((IntPtr)HWND_BROADCAST, WM_SETTINGCHANGE, IntPtr.Zero, "Environment", SMTO_ABORTIFHUNG, 5000, out result);
        }
    }
}

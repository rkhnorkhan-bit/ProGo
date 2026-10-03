using System;
using System.Diagnostics;
using System.Globalization;
using System.Net.Sockets;
using CancellationToken = System.Threading.CancellationToken;
using CancellationTokenSource = System.Threading.CancellationTokenSource;
using WaitHandle = System.Threading.WaitHandle;
using System.Threading.Tasks;

namespace ProGo
{
    internal enum ConnectionProbeState { Unknown, Ready, Failed }

    internal sealed class InternetProbeResult
    {
        internal readonly bool Responded;
        internal readonly int HttpStatus;
        internal InternetProbeResult(bool responded, int httpStatus = 0) { Responded = responded; HttpStatus = httpStatus; }
    }

    // Immutable, in-memory evidence. Applied preferences are intentionally not network evidence.
    internal sealed class ConnectionHealthSnapshot
    {
        internal readonly string Key;
        internal readonly ConnectionProbeState Socks, Internet;
        internal readonly DateTime? SocksCheckedUtc, InternetCheckedUtc;
        internal readonly int HttpStatus;
        internal readonly string Endpoint;
        internal ConnectionHealthSnapshot(string key, ConnectionProbeState socks, ConnectionProbeState internet,
            DateTime? socksTime = null, DateTime? internetTime = null, int httpStatus = 0, string endpoint = "")
        {
            Key = key; Socks = socks; Internet = internet; SocksCheckedUtc = socksTime;
            InternetCheckedUtc = internetTime; HttpStatus = httpStatus; Endpoint = endpoint;
        }
        internal bool SocksReady { get { return Socks == ConnectionProbeState.Ready; } }
        internal bool InternetVerified { get { return SocksReady && Internet == ConnectionProbeState.Ready; } }
        internal string Title { get { return InternetVerified ? "Выход в интернет проверен" : SocksReady ? "Прокси отвечает" : Socks == ConnectionProbeState.Failed ? "Прокси не отвечает" : "Подключение не проверено"; } }
        internal string Summary
        {
            get {
                string local = SocksReady ? "Прокси: ответ SOCKS" : Socks == ConnectionProbeState.Failed ? "Прокси: нет ответа SOCKS" : "Прокси: ещё не проверен";
                string internet = InternetVerified ? "Выход: HTTP " + HttpStatus : Internet == ConnectionProbeState.Failed ? "Выход: нет ответа" : "Выход: ещё не проверен";
                var time = InternetCheckedUtc ?? SocksCheckedUtc;
                return local + " · " + internet + (time.HasValue ? " · " + time.Value.ToLocalTime().ToString("HH:mm:ss") : "");
            }
        }
        internal string TrayText { get { return "ProGo — " + (InternetVerified ? "выход проверен" : SocksReady ? "прокси отвечает; выход не проверен" : Socks == ConnectionProbeState.Failed ? "прокси не отвечает" : "подключение не проверено"); } }
    }

    internal sealed class ConnectionHealthMonitor : IDisposable
    {
        private readonly Func<AppSettings> read;
        private readonly Func<DateTime> now;
        private readonly Func<AppSettings, CancellationToken, bool> socksProbe;
        private readonly Func<AppSettings, CancellationToken, InternetProbeResult> internetProbe;
        private readonly object gate = new object();
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        private System.Threading.Timer timer;
        private ConnectionHealthSnapshot snapshot = new ConnectionHealthSnapshot("", ConnectionProbeState.Unknown, ConnectionProbeState.Unknown);
        private long generation;
        private bool busy, pending, disposed;
        internal event Action Changed;

        internal ConnectionHealthMonitor(Func<AppSettings> read,
            Func<AppSettings, CancellationToken, bool> socksProbe = null,
            Func<AppSettings, CancellationToken, InternetProbeResult> internetProbe = null, Func<DateTime> clock = null)
        {
            this.read = read; now = clock ?? (() => DateTime.UtcNow);
            this.socksProbe = socksProbe ?? new Func<AppSettings, CancellationToken, bool>(CheckSocks);
            this.internetProbe = internetProbe ?? new Func<AppSettings, CancellationToken, InternetProbeResult>(CheckInternet);
        }
        private static string Key(AppSettings s) { return s.SocksHost + "\n" + s.SocksPort + "\n" + s.SshProfile + "\n" + s.TestEndpoint; }
        private void Match(string key)
        {
            if (snapshot.Key == key) return;
            generation++; pending = busy;
            snapshot = new ConnectionHealthSnapshot(key, ConnectionProbeState.Unknown, ConnectionProbeState.Unknown);
        }
        internal ConnectionHealthSnapshot Current
        {
            get {
                var key = Key(read());
                lock (gate) {
                    if (disposed) return new ConnectionHealthSnapshot(key, ConnectionProbeState.Unknown, ConnectionProbeState.Unknown);
                    Match(key);
                    var clock = now();
                    if (snapshot.SocksCheckedUtc.HasValue && clock - snapshot.SocksCheckedUtc.Value > TimeSpan.FromSeconds(15))
                        return new ConnectionHealthSnapshot(key, ConnectionProbeState.Unknown, ConnectionProbeState.Unknown);
                    if (snapshot.InternetCheckedUtc.HasValue && clock - snapshot.InternetCheckedUtc.Value > TimeSpan.FromSeconds(45))
                        return new ConnectionHealthSnapshot(key, snapshot.Socks, ConnectionProbeState.Unknown, snapshot.SocksCheckedUtc);
                    return snapshot;
                }
            }
        }
        internal void Start()
        {
            lock (gate) {
                if (disposed || timer != null) return;
                timer = new System.Threading.Timer(delegate { RequestRefresh(); }, null, 5000, 5000);
            }
            RequestRefresh();
        }
        internal void Invalidate()
        {
            var key = Key(read());
            lock (gate) { if (disposed) return; generation++; snapshot = new ConnectionHealthSnapshot(key, ConnectionProbeState.Unknown, ConnectionProbeState.Unknown); pending = busy; }
            Notify(); RequestRefresh(true);
        }
        internal void RequestRefresh(bool forceInternet = false)
        {
            var config = read().Clone(); var key = Key(config);
            long version; ConnectionHealthSnapshot previous;
            lock (gate) {
                if (disposed) return;
                Match(key);
                if (busy) { pending |= forceInternet; return; }
                if (!forceInternet && snapshot.SocksCheckedUtc.HasValue && now() - snapshot.SocksCheckedUtc.Value < TimeSpan.FromSeconds(5)) return;
                previous = snapshot; version = generation; busy = true;
            }
            System.Threading.ThreadPool.QueueUserWorkItem(delegate {
                try {
                    var token = cancellation.Token;
                    bool local = false;
                    try { local = socksProbe(config, token); } catch { }
                    token.ThrowIfCancellationRequested();
                    var checkedAt = now();
                    bool useCached = local && !forceInternet && previous.InternetCheckedUtc.HasValue && checkedAt - previous.InternetCheckedUtc.Value < TimeSpan.FromSeconds(30);
                    var update = new ConnectionHealthSnapshot(key, local ? ConnectionProbeState.Ready : ConnectionProbeState.Failed,
                        useCached ? previous.Internet : ConnectionProbeState.Unknown, checkedAt,
                        useCached ? previous.InternetCheckedUtc : null, useCached ? previous.HttpStatus : 0, config.TestEndpoint);
                    if (!Publish(update, version) || !local || useCached) return;
                    InternetProbeResult result;
                    try { result = internetProbe(config, token); } catch { result = new InternetProbeResult(false); }
                    token.ThrowIfCancellationRequested();
                    Publish(new ConnectionHealthSnapshot(key, ConnectionProbeState.Ready, result.Responded ? ConnectionProbeState.Ready : ConnectionProbeState.Failed,
                        checkedAt, now(), result.HttpStatus, config.TestEndpoint), version);
                }
                catch (OperationCanceledException) { }
                finally {
                    bool again;
                    lock (gate) { busy = false; again = pending && !disposed; pending = false; if (disposed) cancellation.Dispose(); }
                    if (again) RequestRefresh(true);
                }
            });
        }
        private bool Publish(ConnectionHealthSnapshot value, long version)
        {
            var key = Key(read());
            lock (gate) {
                Match(key);
                if (disposed || generation != version || key != value.Key) return false;
                snapshot = value;
            }
            Notify(); return true;
        }
        private void Notify() { var changed = Changed; if (changed != null) try { changed(); } catch { /* A subscriber may be closing. */ } }

        internal static bool CheckSocks(AppSettings s, CancellationToken token)
        {
            try {
                using (var client = new TcpClient())
                using (token.Register(delegate { client.Close(); })) {
                    var connect = client.BeginConnect(s.SocksHost, s.SocksPort, null, null);
                    using (connect.AsyncWaitHandle) {
                        if (WaitHandle.WaitAny(new[] { connect.AsyncWaitHandle, token.WaitHandle }, 700) != 0) return false;
                        client.EndConnect(connect);
                    }
                    using (var stream = client.GetStream()) {
                        stream.ReadTimeout = 700; stream.WriteTimeout = 700;
                        stream.Write(new byte[] { 5, 1, 0 }, 0, 3);
                        return stream.ReadByte() == 5 && stream.ReadByte() == 0;
                    }
                }
            }
            catch { return false; }
        }
        internal static string InternetArguments(AppSettings s)
        {
            Uri endpoint;
            if (!Uri.TryCreate(s.TestEndpoint, UriKind.Absolute, out endpoint) ||
                (endpoint.Scheme != "http" && endpoint.Scheme != "https") || !String.IsNullOrEmpty(endpoint.UserInfo))
                throw new ArgumentException("Для проверки выхода нужен HTTP или HTTPS адрес без логина и пароля.");
            string host = s.SocksHost;
            if (String.IsNullOrWhiteSpace(host) || host.IndexOfAny(new[] { '\r', '\n', '"' }) >= 0 || s.SocksPort < 1 || s.SocksPort > 65535)
                throw new ArgumentException("Проверьте адрес локального прокси.");
            if (Uri.CheckHostName(host.Trim('[', ']')) == UriHostNameType.Unknown) throw new ArgumentException("Проверьте адрес локального прокси.");
            if (host.Contains(":") && !host.StartsWith("[")) host = "[" + host + "]";
            // NO_PROXY must never bypass the explicit SOCKS route, including custom local test endpoints.
            return "--socks5-hostname \"" + host + ":" + s.SocksPort + "\" --noproxy \"\" --connect-timeout 3 --max-time 8 --head --silent --show-error --output NUL --write-out \"%{http_code}\" --url \"" + endpoint.AbsoluteUri + "\"";
        }
        internal static InternetProbeResult CheckInternet(AppSettings s, CancellationToken token)
        {
            using (var deadline = CancellationTokenSource.CreateLinkedTokenSource(token)) {
                deadline.CancelAfter(9000);
                var psi = new ProcessStartInfo("curl.exe", InternetArguments(s)) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
                using (var process = Process.Start(psi))
                using (deadline.Token.Register(delegate { try { if (!process.HasExited) process.Kill(); } catch { } })) {
                    var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
                    if (!process.WaitForExit(10000)) { try { process.Kill(); } catch { } return new InternetProbeResult(false); }
                    if (deadline.IsCancellationRequested || !Task.WaitAll(new Task[] { output, error }, 1000)) return new InternetProbeResult(false);
                    int status;
                    bool responded = process.ExitCode == 0 && Int32.TryParse(output.Result.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out status) && status >= 100 && status <= 599;
                    // A 401 or 403 is transport evidence, not an authenticated app/API check.
                    return new InternetProbeResult(responded, responded ? Int32.Parse(output.Result.Trim(), CultureInfo.InvariantCulture) : 0);
                }
            }
        }
        public void Dispose()
        {
            lock (gate) {
                if (disposed) return; disposed = true; generation++; if (timer != null) timer.Dispose();
                cancellation.Cancel();
                if (!busy) cancellation.Dispose();
                // The last worker releases a source still used by an active probe.
            }
        }
    }
}

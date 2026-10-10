using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading.Tasks;
using CancellationToken = System.Threading.CancellationToken;
using CancellationTokenSource = System.Threading.CancellationTokenSource;

namespace ProGo
{
    internal sealed class ProxyService : IDisposable
    {
        private readonly Func<AppSettings> readSettings;
        private readonly Action<AppSettings> saveSettings;
        private readonly string sshExecutable;
        private readonly Func<DateTime> utcNow;
        private readonly string sshOptions;
        private readonly object gate = new object();
        private readonly object startupGate = new object();
        private readonly int startupTimeoutMs;
        private Task<bool> startupTask;
        private CancellationTokenSource startupCancellation;
        private volatile bool connecting;
        private volatile string startupError, sshError;
        private string recoveryPause;
        internal bool IsConnecting { get { return connecting; } }
        internal string StartupError { get { return startupError; } }
        private readonly System.Threading.Timer recoveryTimer;
        private Process sshProcess;
        private volatile bool wanted;
        private volatile bool disposed;
        private DateTime startedAt;
        private DateTime? healthySince;
        private DateTime? retryAt;
        private int failures;
        private int missingListener;
        private int automaticRestarts;
        private bool portOccupied;
        private string ownedHost;
        private int ownedPort;
        private string ownedTarget;
        private string selectedAtStart;
        private string selectedConnectionAtStart;

        public ProxyService(SettingsService settings)
            : this(delegate { return settings.Current; }, settings.Save, "ssh.exe", delegate { return DateTime.UtcNow; }, true)
        {
        }

        // The test harness uses a local child process and a clock, never real SSH credentials.
        internal ProxyService(Func<AppSettings> read, Action<AppSettings> save, string executable,
            Func<DateTime> clock, bool startTimer, string extraOptions = "", int startupTimeoutMs = 20000)
        {
            readSettings = read; saveSettings = save; sshExecutable = executable; utcNow = clock;
            sshOptions = extraOptions;
            this.startupTimeoutMs = startupTimeoutMs;
            if (startTimer)
                recoveryTimer = new System.Threading.Timer(delegate { PollRecovery(); }, null, 5000, 5000);
        }

        public bool IsListening()
        {
            var current = readSettings();
            return IsTcpOpen(current.SocksHost, current.SocksPort, 700);
        }

        private bool IsOwnedProcessAlive()
        {
            try { return sshProcess != null && !sshProcess.HasExited; }
            catch { return false; }
        }

        public int? CurrentPid
        {
            get {
                if (!System.Threading.Monitor.TryEnter(gate)) return null;
                try { return IsOwnedProcessAlive() ? (int?)sshProcess.Id : null; }
                finally { System.Threading.Monitor.Exit(gate); }
            }
        }

        // Cleanup confirmation must wait for ownership rather than use the nonblocking UI PID snapshot.
        internal bool HasOwnedProcess { get { lock (gate) return sshProcess != null; } }

        internal DateTime? NextRecoveryUtc { get { lock (gate) { return retryAt; } } }
        internal int AutomaticRestarts { get { lock (gate) { return automaticRestarts; } } }

        public string RecoveryStatus
        {
            get
            {
                if (connecting) return "Подключаемся к серверу…";
                if (!System.Threading.Monitor.TryEnter(gate)) return "Проверяем состояние подключения…";
                try
                {
                    if (!readSettings().AutoRestartSocks) return "Выключено";
                    if (recoveryPause != null) return "Автовосстановление приостановлено: " + recoveryPause + " После исправления нажмите «Подключить».";
                    if (!wanted) return "Ожидает запуска SOCKS";
                    if (portOccupied) return "Ожидание: порт занят другим процессом";
                    if (retryAt.HasValue)
                        return "Повтор через " + Math.Max(0, (int)Math.Ceiling((retryAt.Value - utcNow()).TotalSeconds)) + " с";
                    return "Включено; восстановлений: " + automaticRestarts;
                } finally { System.Threading.Monitor.Exit(gate); }
            }
        }

        public void SetAutoRestart(bool enabled)
        {
            lock (gate)
            {
                var current = readSettings();
                current.AutoRestartSocks = enabled;
                saveSettings(current);
                SafeLog.Info("SOCKS automatic recovery " + (enabled ? "enabled." : "disabled."));
            }
        }

        public void StartTunnel(bool showErrors)
        {
            StartTunnelCore(showErrors, CancellationToken.None);
        }

        internal Task<bool> StartTunnelAsync(CancellationToken token)
        {
            lock (startupGate) {
                if (disposed) return Task.FromResult(false);
                if (startupTask != null && !startupTask.IsCompleted) {
                    if (startupCancellation != null && startupCancellation.IsCancellationRequested) return ResumeAfterCancellation(startupTask, token);
                    return startupTask;
                }
                var source = CancellationTokenSource.CreateLinkedTokenSource(token);
                startupCancellation = source; connecting = true; startupError = null;
                startupTask = Task.Run(async delegate {
                    bool ready = false;
                    using (var deadline = CancellationTokenSource.CreateLinkedTokenSource(source.Token)) {
                        deadline.CancelAfter(startupTimeoutMs);
                        try {
                            var current = readSettings().Clone();
                            if (BuildProfileTargets(current).Count == 0 && !IsListening()) {
                                startupError = "Сервер не выбран. Откройте «Подключения» и добавьте сервер."; return false;
                            }
                            StartTunnelCore(false, deadline.Token);
                            while (true) {
                                deadline.Token.ThrowIfCancellationRequested();
                                var changed = readSettings();
                                if (changed.SocksHost != current.SocksHost || changed.SocksPort != current.SocksPort ||
                                    (changed.SshProfile == current.SshProfile && SshConnection.Signature(changed) != SshConnection.Signature(current))) { source.Cancel(); source.Token.ThrowIfCancellationRequested(); }
                                if (ConnectionHealthMonitor.CheckSocks(current, deadline.Token)) { ready = true; return true; }
                                bool alive; lock (gate) { alive = IsOwnedProcessAlive(); }
                                if (!alive) {
                                    // Let redirected stderr delivery finish after a quick SSH refusal.
                                    await Task.Delay(100, deadline.Token);
                                    startupError = IsListening() ? "Порт SOCKS занят, но прокси не отвечает. Выберите другой порт в «Подключениях»." :
                                        sshError ?? "SSH не подключился. Проверьте доступность сервера и SSH-ключ; для первого входа откройте «Подключения → Первый вход».";
                                    return false;
                                }
                                await Task.Delay(150, deadline.Token);
                            }
                        }
                        catch (OperationCanceledException) {
                            if (source.IsCancellationRequested) throw;
                            startupError = "Сервер не подготовил SOCKS за отведённое время. Проверьте сервер и SSH-ключ в «Подключениях»."; return false;
                        }
                        catch (Exception ex) {
                            startupError = "Не удалось запустить SSH. Проверьте наличие OpenSSH и настройки подключения.";
                            SafeLog.Error("SSH asynchronous startup failed.", ex); return false;
                        }
                        finally {
                            if (!ready) lock (gate) {
                                StopProcessOnly();
                                if (source.IsCancellationRequested) { wanted = false; retryAt = null; }
                                else if (wanted && !retryAt.HasValue) ScheduleRecovery();
                            }
                            lock (startupGate) { connecting = false; startupCancellation = null; source.Dispose(); }
                        }
                    }
                });
                return startupTask;
            }
        }
        private void CancelStartup()
        {
            lock (startupGate) { if (startupCancellation != null) startupCancellation.Cancel(); }
        }
        private async Task<bool> ResumeAfterCancellation(Task<bool> previous, CancellationToken token)
        {
            try { await previous; } catch (OperationCanceledException) { }
            token.ThrowIfCancellationRequested();
            return await StartTunnelAsync(token);
        }

        private void StartTunnelCore(bool showErrors, CancellationToken token)
        {
            lock (gate)
            {
                token.ThrowIfCancellationRequested();
                if (disposed) return;
                var current = readSettings();
                recoveryPause = null; sshError = null;
                var targets = BuildProfileTargets(current);
                if (targets.Count == 0)
                {
                    if (showErrors)
                        System.Windows.Forms.MessageBox.Show("Сервер не выбран. Откройте «Настройки → Подключение → Добавить». Укажите имя подключения SSH или адрес user@vpn.example.org.", AppConstants.ProductName);
                    return;
                }
                // Never launch a second SSH process while the first is still connecting.
                if (IsOwnedProcessAlive()) { wanted = true; return; }
                if (!StopProcessOnly()) return;
                if (IsListening())
                {
                    wanted = false;
                    SafeLog.Info("SOCKS port is already in use; external process is not managed by ProGo.");
                    return;
                }

                wanted = true; retryAt = null; failures = 0; portOccupied = false;
                foreach (var target in targets)
                {
                    token.ThrowIfCancellationRequested();
                    if (!wanted || disposed) return;
                    if (StartSingleTunnel(target, false))
                    {
                        if (!current.AutoSwitchSshProfile) return;
                        for (var i = 0; i < 10 && wanted && !disposed; i++)
                        {
                            token.ThrowIfCancellationRequested();
                            System.Threading.Thread.Sleep(500);
                            if (IsOwnedProcessAlive() && IsListening())
                            {
                                SelectWorkingProfile(current, target);
                                return;
                            }
                            if (!IsOwnedProcessAlive()) break;
                        }
                        if (!StopProcessOnly()) return;
                    }
                    if (!current.AutoSwitchSshProfile) break;
                }
                if (!wanted || disposed) return;
                ScheduleRecovery();
                if (showErrors)
                    System.Windows.Forms.MessageBox.Show("Не удалось запустить SSH/SOCKS. Проверьте профиль и соединение. Если автовосстановление включено, ProGo повторит попытку в фоне.", AppConstants.ProductName);
            }
        }

        public void StopTunnel()
        {
            // Cancel intent before waiting for a probe/start already inside the gate.
            wanted = false;
            CancelStartup();
            lock (gate)
            {
                wanted = false; retryAt = null; failures = 0; portOccupied = false;
                StopProcessOnly();
            }
        }

        public void RestartTunnel()
        {
            StopTunnel();
            StartTunnel(true);
        }

        internal void PollRecovery()
        {
            // Timer callbacks never queue behind a manual operation or another probe.
            if (!System.Threading.Monitor.TryEnter(gate)) return;
            try
            {
                if (disposed || connecting || !wanted || recoveryPause != null || !readSettings().AutoRestartSocks) return;
                var now = utcNow();
                if (sshProcess != null)
                {
                    if (!IsOwnedProcessAlive())
                    {
                        if (StopProcessOnly()) ScheduleRecovery();
                        return;
                    }
                    if (IsSocksResponsive(ownedHost, ownedPort))
                    {
                        missingListener = 0;
                        if (!healthySince.HasValue)
                        {
                            healthySince = now;
                            var current = readSettings();
                            if (current.AutoSwitchSshProfile && current.SshProfile == selectedAtStart && SshConnection.Signature(current) == selectedConnectionAtStart &&
                                current.SocksHost == ownedHost && current.SocksPort == ownedPort)
                                SelectWorkingProfile(current, ownedTarget);
                            SafeLog.Info("SOCKS listener is ready.");
                        }
                        // A brief successful connection must not reset a crash loop's backoff.
                        if ((now - healthySince.Value).TotalSeconds >= 60) failures = 0;
                        return;
                    }
                    healthySince = null;
                    if ((now - startedAt).TotalSeconds < 20) return;
                    if (++missingListener < 3) return;
                    if (StopProcessOnly()) ScheduleRecovery();
                    return;
                }

                if (sshError != null) { PauseRecovery(); return; }
                if (!retryAt.HasValue) ScheduleRecovery();
                if (!retryAt.HasValue || utcNow() < retryAt.Value) return;
                // Do not kill/adopt another listener that has claimed the configured port.
                if (IsListening())
                {
                    if (!portOccupied) SafeLog.Info("SOCKS recovery waits for an occupied port.");
                    portOccupied = true;
                    retryAt = utcNow().AddSeconds(5);
                    return;
                }
                portOccupied = false;
                if (!wanted || disposed || !readSettings().AutoRestartSocks) return;
                var settings = readSettings();
                var targets = BuildProfileTargets(settings);
                if (targets.Count == 0) { wanted = false; retryAt = null; return; }
                var index = settings.AutoSwitchSshProfile ? Math.Max(0, failures - 1) % targets.Count : 0;
                retryAt = null;
                if (StartSingleTunnel(targets[index], true)) automaticRestarts++;
                else ScheduleRecovery();
            }
            catch (Exception ex)
            {
                SafeLog.Error("SOCKS recovery check failed.", ex);
                if (wanted && !disposed && sshProcess == null) ScheduleRecovery();
            }
            finally { System.Threading.Monitor.Exit(gate); }
        }

        private void PauseRecovery()
        {
            // Only fixed, classified guidance is retained; raw stderr is never exported.
            recoveryPause = sshError; retryAt = null;
            SafeLog.Info("SOCKS recovery paused after permanent SSH refusal.");
        }
        private void ScheduleRecovery()
        {
            if (!wanted || disposed) { retryAt = null; return; }
            if (sshError != null) { PauseRecovery(); return; }
            failures = Math.Min(failures + 1, 1000);
            var seconds = Math.Min(60, 5 * (1 << Math.Min(failures - 1, 4)));
            retryAt = utcNow().AddSeconds(seconds);
            if (readSettings().AutoRestartSocks)
                SafeLog.Info("SOCKS interrupted; next recovery attempt in " + seconds + " seconds.");
        }

        private bool StartSingleTunnel(string target, bool automatic)
        {
            if (!wanted || disposed || IsOwnedProcessAlive()) return false;
            try
            {
                var current = readSettings();
                var endpoint = current.SocksHost + ":" + current.SocksPort;
                var args = sshOptions + String.Format("-N -D {0} -o ExitOnForwardFailure=yes -o ConnectTimeout=10 -o ServerAliveInterval=30 -o ServerAliveCountMax=3 -o BatchMode=yes -o StrictHostKeyChecking=yes {1}",
                    SshConnection.Quote(endpoint), SshConnection.CommandArguments(SshConnection.Resolve(current, target)));
                var psi = new ProcessStartInfo(sshExecutable, args)
                {
                    UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden, RedirectStandardError = true
                };
                sshError = null;
                sshProcess = Process.Start(psi);
                if (sshProcess == null) return false;
                sshProcess.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs e) {
                    if (!ReferenceEquals(sender, sshProcess)) return;
                    var line = (e.Data ?? "").ToLowerInvariant();
                    if (line.Contains("permission denied")) sshError = "SSH-ключ не принят или недоступен фоновому процессу. Проверьте выбранный алиас и путь ключа. Для ключа с passphrase запустите ssh-agent и загрузите ключ через ssh-add; фоновое подключение не запрашивает пароль. «Первый вход» проверяет доступ в отдельном окне.";
                    else if (line.Contains("host key verification failed") || line.Contains("remote host identification has changed"))
                        sshError = "Не подтверждён или изменился ключ сервера. Сверьте отпечаток с сервером и используйте «Подключения → Первый вход».";
                };
                sshProcess.BeginErrorReadLine();
                startedAt = utcNow(); healthySince = null; missingListener = 0;
                ownedHost = current.SocksHost; ownedPort = current.SocksPort; ownedTarget = target;
                selectedAtStart = current.SshProfile; selectedConnectionAtStart = SshConnection.Signature(current);
                SafeLog.Info(automatic ? "SOCKS automatic restart requested." : "SOCKS start requested.");
                return true;
            }
            catch (Exception ex)
            {
                SafeLog.Error("Failed to start SSH tunnel.", ex);
                return false;
            }
        }

        private bool StopProcessOnly()
        {
            if (sshProcess == null) return true;
            try
            {
                if (!sshProcess.HasExited)
                {
                    sshProcess.Kill();
                    if (!sshProcess.WaitForExit(1000)) return false;
                }
                sshProcess.Dispose(); sshProcess = null;
                healthySince = null; missingListener = 0;
                return true;
            }
            catch (Exception ex)
            {
                // Retain ownership on failure; never spawn a duplicate or kill by port/name.
                SafeLog.Error("Failed to stop SSH tunnel.", ex);
                return false;
            }
        }

        private void SelectWorkingProfile(AppSettings current, string target)
        {
            if (!String.Equals(current.SshProfile, target, StringComparison.OrdinalIgnoreCase))
            {
                current.SshProfile = target;
                saveSettings(current);
                SafeLog.Info("SSH profile auto-switched after successful listener startup.");
            }
        }

        private static List<string> BuildProfileTargets(AppSettings settings)
        {
            var targets = new List<string>();
            AddTarget(targets, settings.SshProfile);
            if (settings.SshProfiles != null)
                foreach (var profile in settings.SshProfiles)
                    if (profile != null) AddTarget(targets, profile.Target);
            return targets;
        }

        private static void AddTarget(List<string> targets, string target)
        {
            target = (target ?? String.Empty).Trim();
            if (String.IsNullOrWhiteSpace(target)) return;
            foreach (var existing in targets)
                if (String.Equals(existing, target, StringComparison.OrdinalIgnoreCase)) return;
            targets.Add(target);
        }

        private static bool IsTcpOpen(string host, int port, int timeoutMs)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    var ar = client.BeginConnect(host, port, null, null);
                    using (ar.AsyncWaitHandle)
                    {
                        if (!ar.AsyncWaitHandle.WaitOne(timeoutMs)) return false;
                        client.EndConnect(ar);
                        return true;
                    }
                }
            }
            catch { return false; }
        }

        private static bool IsSocksResponsive(string host, int port)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    var ar = client.BeginConnect(host, port, null, null);
                    using (ar.AsyncWaitHandle)
                    {
                        if (!ar.AsyncWaitHandle.WaitOne(700)) return false;
                        client.EndConnect(ar);
                    }
                    using (var stream = client.GetStream())
                    {
                        stream.ReadTimeout = 700; stream.WriteTimeout = 700;
                        stream.Write(new byte[] { 5, 1, 0 }, 0, 3);
                        return stream.ReadByte() == 5 && stream.ReadByte() == 0;
                    }
                }
            }
            catch { return false; }
        }

        public void Dispose()
        {
            disposed = true; wanted = false;
            CancelStartup();
            if (recoveryTimer != null) recoveryTimer.Dispose();
            lock (gate) { retryAt = null; StopProcessOnly(); }
        }
    }
}

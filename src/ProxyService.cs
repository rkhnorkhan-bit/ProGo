using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CancellationToken = System.Threading.CancellationToken;
using CancellationTokenSource = System.Threading.CancellationTokenSource;

namespace ProGo
{
    // Bootstrap/native process fixtures depend only on this observation contract,
    // not the diagnostic UI, agent protocol or configuration-probe implementation.
    // A matching observation is freshness evidence, never server authorization.
    internal interface ISshLaunchPreparationObservation
    {
        string AgentEndpoint { get; }
        bool MatchesLaunchRequest(SshProfileSetting candidate, string executable, string agentEndpoint);
        string ToDisplay();
    }

    internal sealed class ProxyService : IDisposable
    {
        internal delegate SettingsLaunchRevisionState AcquireLaunchRevision(SettingsRevisionSnapshot expected, out IDisposable lease);
        private readonly Func<SshProfileSetting, string, CancellationToken, ISshLaunchPreparationObservation> prepareAuthentication;
        private readonly AcquireLaunchRevision acquireLaunchRevision;
        private readonly Func<AppSettings> readSettings;
        private readonly Action<AppSettings> saveSettings;
        private readonly Func<SettingsRevisionSnapshot> captureSettings;
        private readonly Func<SettingsRevisionSnapshot, string, bool> trySelectProfile;
        private readonly Action<bool> setAutoRestart;
        private SettingsRevisionSnapshot ownedSettingsSnapshot;
        private readonly string sshExecutable;
        private readonly Func<DateTime> utcNow;
        private readonly string sshOptions;
        private readonly object gate = new object();
        private readonly object startupGate = new object();
        private readonly int startupTimeoutMs;
        private Task<bool> startupTask, stopTask;
        private long operationEpoch;
        private volatile bool stopping;
        // User intent is independent of the opportunistic PID/readiness snapshots.
        // Settings can read it while a worker owns the process gate.
        private volatile bool connectionRequested;
        internal bool ConnectionRequested { get { return connectionRequested; } }
        internal bool IsStopping { get { return stopping; } }
        private CancellationTokenSource startupCancellation;
        private volatile bool connecting;
        private volatile string startupError;
        // Redirected output belongs to the process that produced it, even after cleanup.
        private sealed class SshAttempt
        {
            internal readonly Process Process;
            internal readonly string Identity;
            internal readonly TaskCompletionSource<bool> ErrorFinished = new TaskCompletionSource<bool>();
            internal volatile string Error;
            internal SshAttempt(Process process, string identity) { Process = process; Identity = identity; }
        }
        private sealed class StartupSettingsContext
        {
            internal SettingsRevisionSnapshot Expected;
            internal readonly CancellationTokenSource Source;
            internal readonly long Epoch;
            internal StartupSettingsContext(SettingsRevisionSnapshot expected, CancellationTokenSource source = null, long epoch = -1)
            { Expected = expected; Source = source; Epoch = epoch; }
        }
        private sealed class LaunchPreparation
        {
            internal readonly CancellationTokenSource Source;
            internal readonly CancellationToken Token;
            internal readonly long Epoch;
            internal ISshLaunchPreparationObservation Observation;
            internal string ObservationDisplay;
            internal readonly string AgentEndpoint;
            internal readonly TaskCompletionSource<bool> Settled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            internal LaunchPreparation(CancellationToken token, long epoch, int timeoutMs)
            {
                Source = CancellationTokenSource.CreateLinkedTokenSource(token); Token = Source.Token;
                Epoch = epoch; AgentEndpoint = Environment.GetEnvironmentVariable("SSH_AUTH_SOCK"); Source.CancelAfter(timeoutMs);
            }
            internal void Cancel() { try { Source.Cancel(); } catch (ObjectDisposedException) { } }
        }
        private volatile LaunchPreparation launchPreparation;
        private volatile LaunchPreparation acceptedAuthenticationPreparation;
        internal ISshLaunchPreparationObservation AuthenticationPreparation {
            get {
                var accepted = acceptedAuthenticationPreparation;
                return accepted != null && !disposed && !stopping && connectionRequested && !accepted.Token.IsCancellationRequested &&
                    accepted.Epoch == System.Threading.Interlocked.Read(ref operationEpoch) ? accepted.Observation : null;
            }
        }
        private volatile SshAttempt ownedAttempt, lastFailedAttempt;
        private readonly Dictionary<string, SshAttempt> failedAttempts = new Dictionary<string, SshAttempt>(StringComparer.Ordinal);
        private int nextCandidate;
        private string sshError { get { var attempt = ownedAttempt ?? lastFailedAttempt; return attempt == null ? null : attempt.Error; } }
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

        public ProxyService(SettingsService settings,
            Func<SshProfileSetting, string, CancellationToken, ISshLaunchPreparationObservation> prepareAuthentication = null)
            : this(delegate { return settings.Current; }, settings.Save, OpenSshClient.Executable, delegate { return DateTime.UtcNow; }, true,
                "", 20000, settings.Capture, settings.TrySelectWorkingProfile, settings.SetAutoRestart,
                prepareAuthentication, settings.TryAcquireLaunchRevision)
        {
        }

        // The test harness uses a local child process and a clock, never real SSH credentials.
        internal ProxyService(Func<AppSettings> read, Action<AppSettings> save, string executable,
            Func<DateTime> clock, bool startTimer, string extraOptions = "", int startupTimeoutMs = 20000,
            Func<SettingsRevisionSnapshot> captureSettings = null, Func<SettingsRevisionSnapshot, string, bool> trySelectProfile = null,
            Action<bool> setAutoRestart = null,
            Func<SshProfileSetting, string, CancellationToken, ISshLaunchPreparationObservation> prepareAuthentication = null,
            AcquireLaunchRevision acquireLaunchRevision = null)
        {
            this.prepareAuthentication = prepareAuthentication; this.acquireLaunchRevision = acquireLaunchRevision;
            this.captureSettings = captureSettings; this.trySelectProfile = trySelectProfile; this.setAutoRestart = setAutoRestart;
            readSettings = read; saveSettings = save; sshExecutable = executable; utcNow = clock;
            // The prefix is an argument sequence, not part of the next token.
            sshOptions = String.IsNullOrEmpty(extraOptions) ? "" : extraOptions + " ";
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
                if (stopping) return "Останавливаем подключение…";
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
                if (setAutoRestart != null) setAutoRestart(enabled);
                else { var current = readSettings().Clone(); current.AutoRestartSocks = enabled; saveSettings(current); }
                SafeLog.Info("SOCKS automatic recovery " + (enabled ? "enabled." : "disabled."));
            }
        }

        public void StartTunnel(bool showErrors)
        {
            long epoch;
            lock (startupGate) { if (disposed) return; connectionRequested = true; epoch = operationEpoch; }
            var snapshot = CaptureConnectionSettings();
            StartTunnelCore(showErrors, CancellationToken.None, snapshot, new StartupSettingsContext(snapshot, null, epoch));
        }

        internal Task<bool> StartTunnelAsync(CancellationToken token)
        {
            lock (startupGate) return StartTunnelAsync(token, operationEpoch);
        }
        private Task<bool> StartTunnelAsync(CancellationToken token, long epoch)
        {
            lock (startupGate) {
                if (epoch != operationEpoch || token.IsCancellationRequested) return CancelledStartup();
                if (disposed) return Task.FromResult(false);
                connectionRequested = true;
                if (stopTask != null && !stopTask.IsCompleted) return ResumeAfterStop(stopTask, token, epoch);
                if (startupTask != null && !startupTask.IsCompleted) {
                    if (startupCancellation != null && startupCancellation.IsCancellationRequested) return ResumeAfterCancellation(startupTask, token, epoch);
                    return startupTask;
                }
                var previousPreparation = launchPreparation;
                if (previousPreparation != null) return ResumeAfterPreparation(previousPreparation.Settled.Task, token, epoch);
                var source = CancellationTokenSource.CreateLinkedTokenSource(token);
                startupCancellation = source; connecting = true; startupError = null;
                startupTask = Task.Run(async delegate {
                    bool ready = false;
                    using (var deadline = CancellationTokenSource.CreateLinkedTokenSource(source.Token)) {
                        deadline.CancelAfter(startupTimeoutMs);
                        try {
                            var snapshot = CaptureConnectionSettings();
                            var current = snapshot.Settings;
                            var operation = new StartupSettingsContext(snapshot, source, epoch);
                            if (BuildProfileTargets(current).Count == 0) {
                                startupError = "Сервер не выбран. Откройте «Подключения» и добавьте сервер."; return false;
                            }
                            StartTunnelCore(false, deadline.Token, snapshot, operation);
                            while (true) {
                                ThrowIfStartupSettingsChanged(operation, deadline.Token);
                                bool alive; lock (gate) { alive = IsOwnedEndpointAlive(current); }
                                if (alive && ConnectionHealthMonitor.CheckSocks(current, deadline.Token)) {
                                    ThrowIfStartupSettingsChanged(operation, deadline.Token);
                                    lock (gate) { alive = IsOwnedEndpointAlive(current); }
                                    if (alive && wanted) { ready = true; return true; }
                                }
                                if (!alive) {
                                    ObserveAttemptError(ownedAttempt ?? lastFailedAttempt, deadline.Token);
                                    startupError = IsListening() ? "Порт SOCKS занят другим процессом. Выберите другой порт в «Подключениях»." :
                                        WithAuthenticationPreparation(sshError ?? "SSH не подключился. Проверьте доступность сервера и SSH-ключ; для первого входа откройте «Подключения → Первый вход».");
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
        private async Task<bool> ResumeAfterCancellation(Task<bool> previous, CancellationToken token, long epoch)
        {
            try { await previous.ConfigureAwait(false); } catch (OperationCanceledException) { }
            token.ThrowIfCancellationRequested();
            return await StartTunnelAsync(token, epoch).ConfigureAwait(false);
        }
        private async Task<bool> ResumeAfterPreparation(Task previous, CancellationToken token, long epoch)
        {
            await previous.ConfigureAwait(false); token.ThrowIfCancellationRequested();
            return await StartTunnelAsync(token, epoch).ConfigureAwait(false);
        }
        private Task CancelLaunchPreparation()
        {
            var pending = launchPreparation;
            if (pending == null) return Task.FromResult(false);
            pending.Cancel(); return pending.Settled.Task;
        }
        private static Task<bool> CancelledStartup()
        {
            var result = new TaskCompletionSource<bool>(); result.SetCanceled(); return result.Task;
        }
        private async Task<bool> ResumeAfterStop(Task<bool> previous, CancellationToken token, long epoch)
        {
            if (!await previous.ConfigureAwait(false)) { startupError = "Не удалось остановить прежний SSH-процесс. Повторите отключение; новый процесс не запущен."; return false; }
            token.ThrowIfCancellationRequested();
            return await StartTunnelAsync(token, epoch).ConfigureAwait(false);
        }

        private SettingsRevisionSnapshot CaptureConnectionSettings()
        { return captureSettings == null ? new SettingsRevisionSnapshot(readSettings().Clone(), -1) : captureSettings(); }

        private void StartTunnelCore(bool showErrors, CancellationToken token, SettingsRevisionSnapshot snapshot = null,
            StartupSettingsContext operation = null)
        {
            lock (gate)
            {
                token.ThrowIfCancellationRequested();
                if (disposed) return;
                snapshot = snapshot ?? CaptureConnectionSettings();
                var current = snapshot.Settings;
                operation = operation ?? new StartupSettingsContext(snapshot, null, System.Threading.Interlocked.Read(ref operationEpoch));
                // Both private callers own exactly one process-gate entry. A manual
                // request waits for an older recovery preparation outside that gate.
                while (launchPreparation != null) {
                    var pending = launchPreparation.Settled.Task;
                    System.Threading.Monitor.Exit(gate);
                    try { pending.Wait(token); }
                    finally { System.Threading.Monitor.Enter(gate); }
                    token.ThrowIfCancellationRequested();
                }
                ThrowIfStartupSettingsChanged(operation, token);
                if (stopping) return;
                acceptedAuthenticationPreparation = null;
                recoveryPause = null; lastFailedAttempt = null; failedAttempts.Clear(); nextCandidate = 0;
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
                    ThrowIfStartupSettingsChanged(operation, token);
                    if (!wanted || disposed) return;
                    if (StartSingleTunnel(target, false, snapshot, token, operation))
                    {
                        if (!current.AutoSwitchSshProfile) return;
                        using (var candidateDeadline = CancellationTokenSource.CreateLinkedTokenSource(token)) {
                            candidateDeadline.CancelAfter(5000);
                            try {
                                while (wanted && !disposed) {
                                    ThrowIfStartupSettingsChanged(operation, candidateDeadline.Token);
                                    if (!IsOwnedProcessAlive()) { ObserveAttemptError(ownedAttempt, candidateDeadline.Token); break; }
                                    if (ConnectionHealthMonitor.CheckSocks(current, candidateDeadline.Token) && IsOwnedProcessAlive()) {
                                        ThrowIfStartupSettingsChanged(operation, candidateDeadline.Token);
                                        if (!wanted || disposed) return;
                                        if (SelectWorkingProfile(target)) {
                                            var selected = operation.Expected.Settings.Clone(); selected.SshProfile = target;
                                            operation.Expected = new SettingsRevisionSnapshot(selected,
                                                operation.Expected.Revision < 0 ? -1 : operation.Expected.Revision + 1);
                                        }
                                        return;
                                    }
                                    candidateDeadline.Token.WaitHandle.WaitOne(500);
                                }
                            } catch (OperationCanceledException) {
                                token.ThrowIfCancellationRequested();
                                if (!candidateDeadline.IsCancellationRequested) throw;
                            }
                        }
                        if (!StopProcessOnly()) return;
                    }
                }
                if (!wanted || disposed) return;
                ScheduleRecovery();
                if (showErrors)
                    System.Windows.Forms.MessageBox.Show("Не удалось запустить SSH/SOCKS. Проверьте профиль и соединение. Если автовосстановление включено, ProGo повторит попытку в фоне.", AppConstants.ProductName);
            }
        }

        // The UI cancels intent immediately, then awaits owned-process cleanup without
        // waiting on a background probe's gate. A queued start keeps its epoch so a
        // later Stop cannot accidentally be followed by that stale start.
        internal Task<bool> StopTunnelAsync()
        {
            lock (startupGate) {
                connectionRequested = false; wanted = false; System.Threading.Interlocked.Increment(ref operationEpoch);
                if (startupCancellation != null) startupCancellation.Cancel();
                if (stopTask != null && !stopTask.IsCompleted) return stopTask;
                var previous = startupTask; var preparation = CancelLaunchPreparation(); stopping = true;
                stopTask = Task.Run(async delegate {
                    try {
                        if (previous != null) try { await previous.ConfigureAwait(false); } catch (OperationCanceledException) { }
                        await preparation.ConfigureAwait(false);
                        lock (gate) {
                            wanted = false; retryAt = null; failures = 0; portOccupied = false;
                            return StopProcessOnly();
                        }
                    } finally { stopping = false; }
                });
                return stopTask;
            }
        }
        internal Task<bool> RestartTunnelAsync(CancellationToken token)
        {
            lock (startupGate) {
                StopTunnelAsync();
                return StartTunnelAsync(token, operationEpoch);
            }
        }
        // Synchronous compatibility is restricted to CLI/test teardown, never UI actions.
        public void StopTunnel() { StopTunnelAsync().GetAwaiter().GetResult(); }

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
                if (disposed || connecting || launchPreparation != null || !wanted || recoveryPause != null || !readSettings().AutoRestartSocks) return;
                var now = utcNow();
                if (sshProcess != null)
                {
                    if (!IsOwnedProcessAlive())
                    {
                        ObserveAttemptError(ownedAttempt, CancellationToken.None);
                        if (StopProcessOnly()) ScheduleRecovery();
                        return;
                    }
                    if (IsSocksResponsive(ownedHost, ownedPort))
                    {
                        var current = readSettings();
                        if (!wanted || disposed || !IsOwnedEndpointAlive(current) ||
                            !BuildProfileTargets(current).Exists(delegate(string candidate) {
                                return String.Equals(candidate, ownedTarget, StringComparison.OrdinalIgnoreCase);
                            })) return;
                        missingListener = 0;
                        if (!healthySince.HasValue)
                        {
                            healthySince = now; nextCandidate = 0;
                            if (current.AutoSwitchSshProfile && current.SshProfile == selectedAtStart && SshConnection.Signature(current) == selectedConnectionAtStart &&
                                current.SocksHost == ownedHost && current.SocksPort == ownedPort)
                                SelectWorkingProfile(ownedTarget);
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
                var snapshot = CaptureConnectionSettings();
                var settings = snapshot.Settings;
                var targets = BuildProfileTargets(settings);
                if (targets.Count == 0) { wanted = false; retryAt = null; return; }
                string target = null;
                for (int i = 0; i < targets.Count; i++) {
                    int index = (nextCandidate + i) % targets.Count;
                    if (PermanentRefusal(settings, targets[index]) != null) continue;
                    target = targets[index]; nextCandidate = (index + 1) % targets.Count; break;
                }
                retryAt = null;
                if (target == null) { ScheduleRecovery(); return; }
                if (StartSingleTunnel(target, true, snapshot, CancellationToken.None, null)) automaticRestarts++;
                else if (!retryAt.HasValue) ScheduleRecovery();
            }
            catch (OperationCanceledException)
            {
                // No SSH attempt was made after invalidation/admission timeout;
                // do not count that as a permanent refusal or a crashed child.
                if (wanted && !disposed && sshProcess == null) retryAt = utcNow().AddSeconds(5);
            }
            catch (Exception ex)
            {
                SafeLog.Error("SOCKS recovery check failed.", ex);
                if (wanted && !disposed && sshProcess == null) ScheduleRecovery();
            }
            finally { System.Threading.Monitor.Exit(gate); }
        }

        private string WithAuthenticationPreparation(string error)
        {
            // Formatting was snapshotted by the worker outside every shared gate.
            // A local signing observation never replaces the actual SSH failure.
            var accepted = acceptedAuthenticationPreparation;
            if (accepted == null || AuthenticationPreparation == null || String.IsNullOrWhiteSpace(accepted.ObservationDisplay)) return error;
            return error + "\r\nЛокальная подготовка входа: " + accepted.ObservationDisplay +
                "\r\nОткройте «Подключения → Проверить»; после исправления нажмите «Подключить».";
        }

        private void PauseRecovery(string error)
        {
            // Only fixed, classified guidance is retained; raw stderr is never exported.
            recoveryPause = WithAuthenticationPreparation(error); retryAt = null;
            SafeLog.Info("SOCKS recovery paused after permanent SSH refusal.");
        }
        private void ScheduleRecovery()
        {
            if (!wanted || disposed) { retryAt = null; return; }
            var settings = readSettings();
            var targets = BuildProfileTargets(settings);
            string error = null; bool available = false;
            foreach (var target in targets) {
                var refusal = PermanentRefusal(settings, target);
                if (refusal == null) { available = true; break; }
                error = refusal;
            }
            if (!available) {
                if (error != null) PauseRecovery(error);
                else { wanted = false; retryAt = null; }
                return;
            }
            failures = Math.Min(failures + 1, 1000);
            var seconds = Math.Min(60, 5 * (1 << Math.Min(failures - 1, 4)));
            retryAt = utcNow().AddSeconds(seconds);
            if (readSettings().AutoRestartSocks)
                SafeLog.Info("SOCKS interrupted; next recovery attempt in " + seconds + " seconds.");
        }

        private bool StartSingleTunnel(string target, bool automatic, SettingsRevisionSnapshot snapshot, CancellationToken token, StartupSettingsContext operation)
        {
            if (!wanted || disposed || IsOwnedProcessAlive()) return false;
            var current = snapshot.Settings;
            string args;
            try {
                var endpoint = SocksEndpoint.Format(current.SocksHost, current.SocksPort);
                args = sshOptions + String.Format("-N -D {0} -o ExitOnForwardFailure=yes -o ConnectTimeout=10 -o ServerAliveInterval=30 -o ServerAliveCountMax=3 -o BatchMode=yes -o StrictHostKeyChecking=yes {1}",
                    SshConnection.Quote(endpoint), SshConnection.CommandArguments(SshConnection.Resolve(current, target)));
            } catch (ArgumentException) {
                var invalid = new SshAttempt(null, TargetIdentity(current, target)); invalid.Error = ArgumentError;
                invalid.ErrorFinished.TrySetResult(true);
                failedAttempts[invalid.Identity] = invalid; lastFailedAttempt = invalid;
                SafeLog.Info("SSH startup rejected invalid connection arguments."); return false;
            }
            var preparation = new LaunchPreparation(token, System.Threading.Interlocked.Read(ref operationEpoch), startupTimeoutMs);
            acceptedAuthenticationPreparation = null;
            launchPreparation = preparation;
            try
            {
                var psi = new ProcessStartInfo(sshExecutable, args)
                {
                    UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden, RedirectStandardError = true
                };
                // Capture the actual child endpoint before any probe can race an
                // environment edit. Null removes a later inherited override.
                if (preparation.AgentEndpoint == null) psi.EnvironmentVariables.Remove("SSH_AUTH_SOCK");
                else psi.EnvironmentVariables["SSH_AUTH_SOCK"] = preparation.AgentEndpoint;
                IDisposable revisionLease;
                if (!PrepareLaunchAdmission(preparation, snapshot, target, automatic, operation, out revisionLease)) return false;
                try {
                    preparation.Token.ThrowIfCancellationRequested();
                    if (!wanted || disposed || stopping || preparation.Epoch != System.Threading.Interlocked.Read(ref operationEpoch))
                        throw new OperationCanceledException(preparation.Token);
                    lastFailedAttempt = null;
                    sshProcess = Process.Start(psi);
                }
                finally { if (revisionLease != null) revisionLease.Dispose(); }
                if (sshProcess == null) return false;
                var attempt = new SshAttempt(sshProcess, TargetIdentity(current, target));
                ownedAttempt = attempt;
                attempt.Process.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs e) {
                    if (e.Data == null) { attempt.ErrorFinished.TrySetResult(true); return; }
                    var error = ClassifySshErrorLine(e.Data);
                    if (error != null) attempt.Error = error;
                };
                sshProcess.BeginErrorReadLine();
                startedAt = utcNow(); healthySince = null; missingListener = 0;
                ownedHost = current.SocksHost; ownedPort = current.SocksPort; ownedTarget = target;
                selectedAtStart = current.SshProfile; selectedConnectionAtStart = SshConnection.Signature(current);
                ownedSettingsSnapshot = snapshot;
                if (preparation.Token.IsCancellationRequested || !wanted || disposed || stopping ||
                    preparation.Epoch != System.Threading.Interlocked.Read(ref operationEpoch) ||
                    preparation.AgentEndpoint != Environment.GetEnvironmentVariable("SSH_AUTH_SOCK")) {
                    StopProcessOnly(); preparation.Token.ThrowIfCancellationRequested(); return false;
                }
                SafeLog.Info(automatic ? "SOCKS automatic restart requested." : "SOCKS start requested.");
                return true;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                SafeLog.Error("Failed to start SSH tunnel.", ex);
                return false;
            }
            finally {
                if (Object.ReferenceEquals(launchPreparation, preparation)) launchPreparation = null;
                preparation.Source.Dispose(); preparation.Settled.TrySetResult(true);
            }
        }

        // Called only by StartSingleTunnel while one StartTunnelCore/PollRecovery
        // process-gate entry is held. Every released entry is reacquired in finally;
        // the outer caller retains its existing ownership and cleanup contract.
        private bool PrepareLaunchAdmission(LaunchPreparation preparation, SettingsRevisionSnapshot snapshot, string target,
            bool automatic, StartupSettingsContext operation, out IDisposable lease)
        {
            lease = null; ISshLaunchPreparationObservation observation = null; bool occupied;
            preparation.Token.ThrowIfCancellationRequested();
            if (!wanted || disposed || stopping || preparation.Epoch != System.Threading.Interlocked.Read(ref operationEpoch))
                throw new OperationCanceledException(preparation.Token);
            ThrowIfStartupSettingsChanged(operation ?? new StartupSettingsContext(snapshot, null, preparation.Epoch), preparation.Token);
            System.Threading.Monitor.Exit(gate);
            try {
                preparation.Token.ThrowIfCancellationRequested();
                if (prepareAuthentication != null) {
                    try { observation = prepareAuthentication(SshConnection.Resolve(snapshot.Settings, target).Clone(), sshExecutable, preparation.Token); }
                    catch (OperationCanceledException) { throw; }
                    catch { /* An unconfirmed advisory probe never forbids ordinary OpenSSH authentication. */ }
                }
                if (observation != null) {
                    try { preparation.ObservationDisplay = observation.ToDisplay(); }
                    catch { /* Presentation failure must not forbid ordinary OpenSSH authentication. */ }
                }
                preparation.Token.ThrowIfCancellationRequested();
                occupied = IsTcpOpen(snapshot.Settings.SocksHost, snapshot.Settings.SocksPort, 700);
            }
            finally { System.Threading.Monitor.Enter(gate); }
            while (true) {
                preparation.Token.ThrowIfCancellationRequested();
                if (!wanted || disposed || stopping || preparation.Epoch != System.Threading.Interlocked.Read(ref operationEpoch))
                    throw new OperationCanceledException(preparation.Token);
                ThrowIfStartupSettingsChanged(operation ?? new StartupSettingsContext(snapshot, null, preparation.Epoch), preparation.Token);
                string endpoint = Environment.GetEnvironmentVariable("SSH_AUTH_SOCK");
                if (endpoint != preparation.AgentEndpoint || observation != null &&
                    !observation.MatchesLaunchRequest(SshConnection.Resolve(snapshot.Settings, target), sshExecutable, endpoint)) {
                    if (operation != null && operation.Source != null) operation.Source.Cancel();
                    throw new OperationCanceledException(preparation.Token);
                }
                if (occupied) {
                    portOccupied = true;
                    if (automatic) retryAt = utcNow().AddSeconds(5); else wanted = false;
                    return false;
                }
                var state = acquireLaunchRevision == null ? SettingsLaunchRevisionState.Acquired : acquireLaunchRevision(snapshot, out lease);
                if (state == SettingsLaunchRevisionState.Acquired) {
                    preparation.Observation = observation; acceptedAuthenticationPreparation = preparation; return true;
                }
                if (state == SettingsLaunchRevisionState.Changed) {
                    if (operation != null && operation.Source != null) operation.Source.Cancel();
                    throw new OperationCanceledException(preparation.Token);
                }
                // Busy is not an authentication gate. Retry admission within the
                // existing launch budget without holding either shared gate.
                System.Threading.Monitor.Exit(gate);
                try {
                    preparation.Token.WaitHandle.WaitOne(20); preparation.Token.ThrowIfCancellationRequested();
                    occupied = IsTcpOpen(snapshot.Settings.SocksHost, snapshot.Settings.SocksPort, 700);
                }
                finally { System.Threading.Monitor.Enter(gate); }
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
                var attempt = ownedAttempt;
                if (attempt != null) { failedAttempts[attempt.Identity] = attempt; lastFailedAttempt = attempt; }
                sshProcess.Dispose(); sshProcess = null; ownedAttempt = null;
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

        private bool SelectWorkingProfile(string target)
        {
            var expected = ownedSettingsSnapshot;
            if (expected == null) return false;
            bool changed;
            if (trySelectProfile != null) changed = trySelectProfile(expected, target);
            else
            {
                // Compatibility for independent options/test delegates. Production
                // uses the atomic revision check supplied by SettingsService.
                var current = readSettings();
                if (!current.AutoSwitchSshProfile || current.SshProfile != expected.Settings.SshProfile ||
                    current.SocksHost != expected.Settings.SocksHost || current.SocksPort != expected.Settings.SocksPort ||
                    SshConnection.Signature(current) != SshConnection.Signature(expected.Settings) ||
                    String.Equals(current.SshProfile, target, StringComparison.OrdinalIgnoreCase)) return false;
                var candidate = current.Clone(); candidate.SshProfile = target;
                saveSettings(candidate); changed = true;
            }
            if (changed) SafeLog.Info("SSH profile auto-switched after successful listener startup.");
            return changed;
        }

        private static List<string> BuildProfileTargets(AppSettings settings)
        {
            var targets = new List<string>();
            AddTarget(targets, settings.SshProfile);
            if (settings.AutoSwitchSshProfile && settings.SshProfiles != null)
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

        private const string ConfigurationError = "Ошибка SSH config. Проверьте синтаксис и значения параметров, наличие файла конфигурации и права доступа к нему; откройте «Подключения → Проверить», затем нажмите «Подключить».";
        private const string ArgumentError = "Параметры SSH-подключения некорректны. Проверьте адрес, логин, SSH/SOCKS-порт и путь ключа в «Подключениях», затем нажмите «Подключить».";
        private static string ClassifySshErrorLine(string value)
        {
            // Match OpenSSH's fatal forms, never arbitrary marker text in paths/banners.
            if (String.IsNullOrWhiteSpace(value) || value.Length > 4096) return null;
            var line = value.Trim();
            var flags = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
            if (String.Equals(line, "Host key verification failed.", StringComparison.OrdinalIgnoreCase) ||
                Regex.IsMatch(line, @"\A@*\s*warning:\s*remote host identification has changed!\s*@*\z", flags))
                return "Не подтверждён или изменился ключ сервера. Сверьте отпечаток с сервером и используйте «Подключения → Первый вход».";
            if (Regex.IsMatch(line, @"\A(?:warning:|debug[123]?:|banner:)", flags)) return null;
            if (Regex.IsMatch(line, @"\A(?:[^\s]+@[^\s]+:\s*)?permission denied \([a-z0-9@._,+-]*\)\.\z", flags))
                return "SSH-ключ не принят или недоступен фоновому процессу. Проверьте выбранный алиас и путь ключа. Для ключа с passphrase запустите ssh-agent и загрузите ключ через ssh-add; фоновое подключение не запрашивает пароль. «Первый вход» проверяет доступ в отдельном окне.";
            if (Regex.IsMatch(line, @"\A.+: terminating, [0-9]+ bad configuration options\z", flags) ||
                Regex.IsMatch(line, @"\A.+(?:\sline|:\s*line) [0-9]+:\s*(?:bad configuration option:|bad port\b|no argument after keyword\b|missing argument\b|missing yes/no argument\b|bad yes/no argument\b|garbage at end of line\b)", flags) ||
                Regex.IsMatch(line, @"\A(?:bad owner or permissions on |can't open user config file )", flags))
                return ConfigurationError;
            if (Regex.IsMatch(line, @"\A(?:bad port '|bad (?:dynamic|local|remote|stdio) forwarding specification '|unknown (?:cipher|mac) type ')", flags))
                return ArgumentError;
            return null;
        }

        private static string TargetIdentity(AppSettings settings, string target)
        {
            var candidate = settings.Clone(); candidate.SshProfile = target;
            return settings.SocksHost + "\n" + settings.SocksPort + "\n" + SshConnection.Signature(candidate);
        }
        private string PermanentRefusal(AppSettings settings, string target)
        {
            SshAttempt attempt;
            return failedAttempts.TryGetValue(TargetIdentity(settings, target), out attempt) ? attempt.Error : null;
        }
        private static void ObserveAttemptError(SshAttempt attempt, CancellationToken token)
        {
            if (attempt != null) attempt.ErrorFinished.Task.Wait(200, token);
        }
        private bool IsOwnedEndpointAlive(AppSettings settings)
        {
            var attempt = ownedAttempt;
            return IsOwnedProcessAlive() && attempt != null && ownedHost == settings.SocksHost && ownedPort == settings.SocksPort &&
                attempt.Identity == TargetIdentity(settings, ownedTarget);
        }
        private void ThrowIfStartupSettingsChanged(StartupSettingsContext operation, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (operation.Epoch >= 0 && operation.Epoch != System.Threading.Interlocked.Read(ref operationEpoch)) {
                if (operation.Source != null) operation.Source.Cancel();
                throw new OperationCanceledException(token);
            }
            var expected = operation.Expected;
            var actual = CaptureConnectionSettings();
            var old = expected.Settings; var current = actual.Settings;
            bool changed = old.SshProfile != current.SshProfile || old.AutoSwitchSshProfile != current.AutoSwitchSshProfile ||
                old.SocksHost != current.SocksHost || old.SocksPort != current.SocksPort;
            var before = BuildProfileTargets(old); var after = BuildProfileTargets(current);
            changed |= before.Count != after.Count;
            for (int i = 0; !changed && i < before.Count; i++)
                changed = TargetIdentity(old, before[i]) != TargetIdentity(current, after[i]);
            if (!changed) return;
            if (operation.Source != null) { operation.Source.Cancel(); operation.Source.Token.ThrowIfCancellationRequested(); }
            throw new OperationCanceledException(token);
        }

        private static bool IsTcpOpen(string host, int port, int timeoutMs)
        {
            try
            {
                using (var client = SocksEndpoint.Connect(host, port, timeoutMs, CancellationToken.None)) return true;
            }
            catch { return false; }
        }

        private static bool IsSocksResponsive(string host, int port)
        {
            try
            {
                using (var client = SocksEndpoint.Connect(host, port, 700, CancellationToken.None))
                {
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
            lock (startupGate) { disposed = true; connectionRequested = false; wanted = false; System.Threading.Interlocked.Increment(ref operationEpoch); CancelStartup(); CancelLaunchPreparation(); }
            if (recoveryTimer != null) recoveryTimer.Dispose();
            lock (gate) { retryAt = null; StopProcessOnly(); }
        }
    }
}

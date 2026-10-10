using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using CancellationTokenSource = System.Threading.CancellationTokenSource;
using CancellationToken = System.Threading.CancellationToken;
using System.Windows.Forms;

namespace ProGo
{
    internal sealed class UpdateAwareTrayApplicationContext : ApplicationContext
    {
        private readonly SettingsService settings;
        private readonly ProxyService proxy;
        private readonly CliProxyBridgeService cliProxy;
        private readonly HomeVpnService homeVpn;
        private readonly AppProxyConsumers appConsumers;
        private readonly ClipboardService clipboard;
        private readonly NotifyIcon tray;
        private readonly System.Drawing.Icon icon;
        private readonly ConnectionHealthMonitor health;
        private readonly Func<WindowsProxyRestoreResult> restoreWindows;
        private readonly Action restoreCli;
        private readonly Action<ProxyFeature, AppSettings> applyIntegration;
        private readonly System.Threading.SemaphoreSlim integrationGate = new System.Threading.SemaphoreSlim(1, 1);
        private readonly CancellationTokenSource integrationWaitCancellation = new CancellationTokenSource();
        private readonly object integrationLifetimeGate = new object();
        private readonly TaskCompletionSource<bool> integrationOwnerClosed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        private long cliIntent, windowsIntent;
        private int integrationPending;
        internal bool IntegrationPending { get { return System.Threading.Volatile.Read(ref integrationPending) != 0; } }
        internal Task IntegrationWork { get; private set; }
        private readonly Func<UpdateCheckForm> createUpdateForm;
        private UpdateCheckForm updateForm;
        private readonly bool ownsHealth;
        private readonly Timer statusTimer = new Timer { Interval = 1000 };
        private Timer startupShowTimer;
        private readonly Dictionary<AppCommand, long> pendingRoutes = new Dictionary<AppCommand, long>();
        private long nextRouteRequest;
        private readonly Dictionary<AppCommand, ToolStripItem> commandItems = new Dictionary<AppCommand, ToolStripItem>();
        private event Action CommandStateChanged;
        private readonly CancellationTokenSource routeLifetime = new CancellationTokenSource();
        private volatile bool closing;
        private bool shutdownPrepared, shutdownPreparing, disposed;
        private Task<bool> shutdownTask;
        private readonly Action<string, CancellationToken> beforeBackupCopy;
        private readonly int backupShutdownTimeoutMilliseconds;
        private CancellationTokenSource backupCancellation;
        private Task<BackupResult> backupWorker;
        private Task backupCompletion = Task.FromResult(false);
        private BackupCreationForm backupForm;
        private readonly Func<bool, HomeVpnPortableForm> createPortableForm;
        private HomeVpnPortableForm portableForm;
        internal Task PortableWork { get; private set; }
        internal bool IsPortableRunning { get { return PortableWork != null && !PortableWork.IsCompleted; } }
        private readonly TaskCompletionSource<bool> backupOwnerClosed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly Action<CancellationToken> beforeBaselineProbe;
        private bool startupBackupScheduled, backupQueued;
        private Task startupBackupCompletion = Task.FromResult(false);
        private TaskCompletionSource<bool> startupBackupCompletionSource;
        private string backupOperationKind, backupStatus = "";
        private BackupResult lastBackupResult;
        private bool lastBackupCancelled, lastBackupFailed;
        private ToolStripMenuItem backupStatusItem;
        internal bool IsBackupRunning { get { return backupQueued || backupWorker != null; } }
        internal bool IsManualBackupRunning { get { return IsBackupRunning; } }
        internal Task ManualBackupCompletion { get { return backupCompletion; } }
        internal Task StartupBackupCompletion { get { return startupBackupCompletion; } }
        internal string BackupStatus { get { return backupStatus; } }
        internal string BackupOperationKind { get { return backupOperationKind; } }
        internal int PendingRouteCount { get { return pendingRoutes.Count; } }

        public UpdateAwareTrayApplicationContext(SettingsService settingsService, ProxyService proxyService, CliProxyBridgeService cliProxyService, HomeVpnService homeVpnService, ClipboardService clipboardService, bool showStatusOnStartup, ConnectionHealthMonitor health = null, Func<WindowsProxyRestoreResult> windowsRestore = null, Func<UpdateCheckForm> updateFormFactory = null, Action<string, CancellationToken> beforeBackupCopy = null, int backupShutdownTimeoutMilliseconds = 3000, Action<CancellationToken> beforeBaselineProbe = null, Action cliRestore = null, Action<ProxyFeature, AppSettings> integrationApply = null, int settingsShutdownTimeoutMilliseconds = 3000, Func<bool, HomeVpnPortableForm> portableFormFactory = null)
        {
            settings = settingsService;
            if (settingsShutdownTimeoutMilliseconds < 1 || settingsShutdownTimeoutMilliseconds > 30000) throw new ArgumentOutOfRangeException("settingsShutdownTimeoutMilliseconds");
            this.settingsShutdownTimeoutMilliseconds = settingsShutdownTimeoutMilliseconds;
            this.beforeBackupCopy = beforeBackupCopy;
            this.beforeBaselineProbe = beforeBaselineProbe;
            if (backupShutdownTimeoutMilliseconds < 1 || backupShutdownTimeoutMilliseconds > 30000) throw new ArgumentOutOfRangeException("backupShutdownTimeoutMilliseconds");
            this.backupShutdownTimeoutMilliseconds = backupShutdownTimeoutMilliseconds;
            createPortableForm = portableFormFactory ?? (exporting => new HomeVpnPortableForm(exporting, HomeVpnPrivateFiles.Root));
            PortableWork = Task.FromResult(false);
            createUpdateForm = updateFormFactory ?? (() => new UpdateCheckForm());
            restoreWindows = windowsRestore ?? (() => SystemProxyService.RestoreOwned());
            restoreCli = cliRestore ?? (() => CliProxyEnvironmentService.ClearUserEnvironmentIfOwned());
            applyIntegration = integrationApply ?? delegate(ProxyFeature feature, AppSettings current) {
                if (feature == ProxyFeature.Cli) CliProxyEnvironmentService.ApplyUserEnvironment(current.HttpProxyPort);
                else { string message; if (!SystemProxyService.Apply(current, out message)) throw new InvalidOperationException(message); }
            };
            IntegrationWork = Task.FromResult(false);
            proxy = proxyService;
            cliProxy = cliProxyService;
            appConsumers = new AppProxyConsumers(cliProxy, settings);
            homeVpn = homeVpnService;
            clipboard = clipboardService;
            ownsHealth = health == null;
            this.health = health ?? new ConnectionHealthMonitor(() => settings.Current);
            icon = BrandIcon.Create(SystemInformation.SmallIconSize.Width);
            activationDispatcher.CreateControl();

            tray = new NotifyIcon
            {
                Icon = icon,
                Text = AppConstants.ProductName,
                Visible = true,
                ContextMenuStrip = BuildMenu()
            };
            tray.DoubleClick += delegate { ExecuteCommand(AppCommand.ShowMain); };
            UpdateTooltip();
            this.health.Changed += HealthChanged;
            statusTimer.Tick += delegate { appConsumers.ReleaseIfUnused(); UpdateTooltip(); RefreshPendingRoutes(); };
            statusTimer.Start();
            if (ownsHealth) this.health.Start();

            if (showStatusOnStartup)
            {
                startupShowTimer = new Timer { Interval = 500 };
                startupShowTimer.Tick += delegate
                {
                    startupShowTimer.Stop();
                    startupShowTimer.Dispose();
                    startupShowTimer = null;
                    ShowStatus();
                };
                startupShowTimer.Start();
            }
        }

        private readonly AutomationPlan automation = new AutomationPlan();
        private readonly Timer automationTimer = new Timer { Interval = 1000 };
        private MainWindow mainWindow;
        private readonly int ownerThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
        private readonly Control activationDispatcher = new Control();
        private readonly object uiCompletionGate = new object();
        private readonly HashSet<Action> uiCompletions = new HashSet<Action>();

        private ContextMenuStrip BuildMenu()
        {
            var menu = new ContextMenuStrip();
            Item(menu.Items, AppCommand.ShowMain);
            menu.Items.Add(new ToolStripSeparator());
            var connection = new ToolStripMenuItem("Подключение");
            Item(connection.DropDownItems, AppCommand.Connect);
            Item(connection.DropDownItems, AppCommand.StopDesktop);
            Item(connection.DropDownItems, AppCommand.StopAll);
            Item(connection.DropDownItems, AppCommand.Reconnect);
            Item(connection.DropDownItems, AppCommand.CheckRoute);
            Item(connection.DropDownItems, AppCommand.Diagnostics);
            menu.Items.Add(connection);
            var apps = new ToolStripMenuItem("Прокси для приложений");
            Item(apps.DropDownItems, AppCommand.EnableWindows); Item(apps.DropDownItems, AppCommand.DisableWindows);
            apps.DropDownItems.Add(new ToolStripSeparator());
            Item(apps.DropDownItems, AppCommand.StartCli); Item(apps.DropDownItems, AppCommand.StopCli);
            Item(apps.DropDownItems, AppCommand.OpenTerminal);
            apps.DropDownItems.Add(new ToolStripSeparator());
            var extra = new ToolStripMenuItem("Дополнительно: ярлык Codex");
            Item(extra.DropDownItems, AppCommand.CreateCodexShortcut); Item(extra.DropDownItems, AppCommand.RemoveCodexShortcut); apps.DropDownItems.Add(extra);
            Item(apps.DropDownItems, AppCommand.OpenCodex); menu.Items.Add(apps);
            Item(menu.Items, AppCommand.Phone);
            Item(menu.Items, AppCommand.StopPhone);
            Item(menu.Items, AppCommand.Vault);
            Item(menu.Items, AppCommand.Settings);
            menu.Items.Add(new ToolStripSeparator());
            var backups = new ToolStripMenuItem("Резервные копии");
            backupStatusItem = new ToolStripMenuItem("Состояние копии: ещё не проверена");
            backupStatusItem.Name = "BackupProgress";
            backupStatusItem.Click += delegate { ShowBackupProgress(); };
            backups.DropDownItems.Add(backupStatusItem);
            Item(backups.DropDownItems, AppCommand.CreateBackup);
            Item(backups.DropDownItems, AppCommand.RestoreBackup);
            Item(backups.DropDownItems, AppCommand.OpenBackups);
            Item(backups.DropDownItems, AppCommand.CleanupBackups);
            backups.DropDownItems.Add(new ToolStripSeparator());
            Item(backups.DropDownItems, AppCommand.ExportHomeVpn); Item(backups.DropDownItems, AppCommand.ImportHomeVpn); menu.Items.Add(backups);
            menu.Items.Add(BuildLogsMenu());
            Item(menu.Items, AppCommand.Update);
            menu.Items.Add(new ToolStripSeparator());
            Item(menu.Items, AppCommand.Exit);
            menu.Opening += delegate { RefreshPendingRoutes(); };
            UiTheme.Menu(menu); return menu;
        }
        private void Item(ToolStripItemCollection items, AppCommand command)
        {
            var item = items.Add(AppCommands.Get(command).Label, null, delegate { ExecuteCommand(command); });
            item.ToolTipText = AppCommands.Get(command).Effect;
            if (item.Owner != null) item.Owner.ShowItemToolTips = true;
            item.AccessibleDescription = item.ToolTipText;
            item.Tag = command; commandItems.Add(command, item);
        }
        private ToolStripMenuItem BuildLogsMenu()
        {
            var logs = new ToolStripMenuItem("Помощь и журналы");
            Item(logs.DropDownItems, AppCommand.Help);
            Item(logs.DropDownItems, AppCommand.ExportDiagnostics);
            Item(logs.DropDownItems, AppCommand.OpenAppLog);
            Item(logs.DropDownItems, AppCommand.OpenUpdateLog);
            Item(logs.DropDownItems, AppCommand.OpenFolder); return logs;
        }
        private void ShowStatus()
        {
            if (mainWindow != null && !mainWindow.IsDisposed) { mainWindow.Show(); mainWindow.WindowState = FormWindowState.Normal; mainWindow.Activate(); return; }
            mainWindow = new MainWindow(settings, proxy, homeVpn, ExecuteCommand, cliProxy, automation, health, appConsumers, GetCommandState, () => BackupStatus, ShowBackupProgress);
            RefreshPendingRoutes();
            mainWindow.FormClosing += DashboardClosing;
            mainWindow.FormClosed += delegate { mainWindow = null; };
            mainWindow.Show(); UpdateTooltip();
        }
        internal void RequestShowStatus()
        {
            if (closing || activationDispatcher.IsDisposed) return;
            if (System.Threading.Thread.CurrentThread.ManagedThreadId != ownerThreadId)
            {
                try { activationDispatcher.BeginInvoke(new Action(delegate { if (!closing) ShowStatus(); })); }
                catch (InvalidOperationException) { }
            }
            else ShowStatus();
        }
        private void DashboardClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason != CloseReason.UserClosing || e.Cancel || settings.Current.TrayCloseExplained) return;
            tray.ShowBalloonTip(6000, "ProGo остаётся в трее", "Закрытие окна не отключает подключения. Откройте ProGo значком рядом с часами. Для отключения используйте команды остановки, для выхода — «Завершить работу ProGo».", ToolTipIcon.Info);
            var updated = settings.Current.Clone(); updated.TrayCloseExplained = true;
            try { settings.Save(updated); } catch (Exception ex) { SafeLog.Error("Tray explanation preference could not be saved.", ex); }
        }
        private void ShowSettings(SettingsSection section = SettingsSection.Automation)
        {
            var before = settings.Current;
            using (var form = new SshProfilesSettingsForm(settings, section))
            {
                form.ManualActionRequested += ExecuteCommand;
                form.CommandState = GetCommandState;
                form.ProxyEndpointText = cliProxy.ProxyUrl;
                form.CurrentProxyEndpoint = delegate { return cliProxy.ProxyUrl; };
                form.SaveRequestedAsync = delegate(SettingsApplyRequest request, CancellationToken token) {
                    return ApplySettingsRequestAsync(request, token);
                };
                CommandStateChanged += form.RefreshCommandAvailability;
                try {
                    if (form.ShowDialog(mainWindow) == DialogResult.OK)
                    {
                        // The accepted owner publication already updates recovery
                        // and automation once. Do not reset a pending auto-apply
                        // that the message pump may have started before dialog close.
                        if (!before.AutoStartSocks && settings.Current.AutoStartSocks) proxy.StartTunnelAsync(System.Threading.CancellationToken.None);
                    }
                } finally { CommandStateChanged -= form.RefreshCommandAvailability; }
            }
            UpdateTooltip();
        }
        private volatile CancellationTokenSource settingsOperationCancellation;
        private readonly int settingsShutdownTimeoutMilliseconds;
        internal Task<SettingsSaveError> SettingsWork { get; private set; }
        internal bool SettingsPending { get { return IntegrationPending; } }
        internal Task<SettingsSaveError> ApplySettingsAsync(AppSettings proposed, bool pickFree, CancellationToken token)
        {
            return ApplySettingsRequestAsync(new SettingsApplyRequest { Proposed = proposed.Clone(), Expected = settings.Capture(), PickFree = pickFree }, token);
        }
        private Task<SettingsSaveError> ApplySettingsRequestAsync(SettingsApplyRequest request, CancellationToken token)
        {
            if (closing || shutdownPrepared || shutdownPreparing || IntegrationPending)
                return Task.FromResult(new SettingsSaveError(SettingsField.General, "Сейчас ProGo изменяет настройки прокси или завершает работу. Дождитесь окончания операции и повторите сохранение."));
            var consumers = appConsumers.BeginMutation();
            System.Threading.Interlocked.Increment(ref integrationPending); RefreshPendingRoutes();
            var work = RunSettingsApply(request, token, consumers);
            SettingsWork = work; IntegrationWork = work; return work;
        }
        internal Task<SettingsSaveError> EnsureBridgeAsync(CancellationToken token)
        {
            if (closing || shutdownPreparing || shutdownPrepared || IntegrationPending)
                return Task.FromResult(new SettingsSaveError(SettingsField.General, "Сейчас ProGo изменяет настройки прокси или завершает работу. Дождитесь окончания операции и повторите команду."));
            var consumers = appConsumers.BeginMutation();
            System.Threading.Interlocked.Increment(ref integrationPending); RefreshPendingRoutes();
            var work = RunSettingsApply(null, token, consumers);
            SettingsWork = work; IntegrationWork = work; return work;
        }
        private async Task<SettingsSaveError> RunSettingsApply(SettingsApplyRequest request, CancellationToken token, IDisposable consumers)
        {
            var result = await RunSettingsApplyCore(request, token, consumers).ConfigureAwait(false);
            // Framework csc targets C# 5: await is not allowed in finally.
            // Keep the tracked Work alive until owner pending publication settles.
            await IntegrationUi(RefreshPendingRoutes).ConfigureAwait(false);
            return result;
        }
        private async Task<SettingsSaveError> RunSettingsApplyCore(SettingsApplyRequest request, CancellationToken token, IDisposable consumers)
        {
            bool acquired = false; IDisposable retention = null;
            var lifetime = integrationWaitCancellation.Token;
            using (var cancellation = CancellationTokenSource.CreateLinkedTokenSource(token, lifetime)) {
                settingsOperationCancellation = cancellation;
                cancellation.CancelAfter(30000);
                try {
                    await integrationGate.WaitAsync(cancellation.Token).ConfigureAwait(false); acquired = true;
                    SettingsRevisionSnapshot expected = null; int previousPort = 0; bool keepRunning = true, reconnect = false, changed = false;
                    if (!await IntegrationUi(delegate {
                        if (closing) return;
                        expected = request == null ? settings.Capture() : request.Expected;
                        previousPort = cliProxy.Port; keepRunning = request == null || cliProxy.IsRunning;
                        if (request != null) {
                            var current = settings.Current;
                            changed = request.Proposed.SocksHost != current.SocksHost || request.Proposed.SocksPort != current.SocksPort || SshConnection.Signature(request.Proposed) != SshConnection.Signature(current);
                            reconnect = changed && proxy.ConnectionRequested;
                        }
                        lock (integrationLifetimeGate) if (!closing) retention = cliProxy.RetainForNativeMutation();
                    }).ConfigureAwait(false) || expected == null || retention == null)
                        return new SettingsSaveError(SettingsField.General, "Применение настроек отменено: окно приложения закрыто.");
                    var prepared = await PrepareBridgeWorker(request, expected, keepRunning, cancellation.Token).ConfigureAwait(false);
                    if (prepared.Error != null) return prepared.Error;
                    bool published = await IntegrationUi(delegate {
                        cliProxy.PublishPrepared(prepared.Value);
                        appConsumers.Observe(); NotifyPortChange(previousPort);
                        if (request != null && changed) {
                            pendingRoutes.Clear(); ObserveStop(proxy.StopTunnelAsync()); health.Invalidate();
                            if (reconnect) proxy.StartTunnelAsync(CancellationToken.None);
                        }
                        if (request != null) {
                            homeVpn.AutoRestart = settings.Current.AutoRestartSocks;
                            automation.Update(expected.Settings, settings.Current);
                        }
                    }).ConfigureAwait(false);
                    // Only service memory/socket ownership is published after a lost
                    // owner; never call tray, consumers, proxy or a disposed form.
                    if (!published) cliProxy.PublishPrepared(prepared.Value);
                    return null;
                } catch (OperationCanceledException) {
                    return new SettingsSaveError(SettingsField.General, "Применение настроек отменено.");
                } catch (Exception ex) {
                    SafeLog.Error("Async settings application failed.", ex);
                    return new SettingsSaveError(SettingsField.General, "Не удалось завершить применение настроек. Проверьте текущее состояние и журнал ProGo.");
                } finally {
                    consumers.Dispose(); System.Threading.Interlocked.Decrement(ref integrationPending);
                    if (retention != null) retention.Dispose();
                    if (acquired) integrationGate.Release();
                    if (ReferenceEquals(settingsOperationCancellation, cancellation)) settingsOperationCancellation = null;
                }
            }
        }
        private sealed class PreparedBridgeResult
        {
            internal CliProxyBridgeService.PreparedBridgeConfiguration Value;
            internal SettingsSaveError Error;
        }
        private Task<PreparedBridgeResult> PrepareBridgeWorker(SettingsApplyRequest request, SettingsRevisionSnapshot expected, bool keepRunning, CancellationToken token)
        {
            return Task.Run(delegate {
                ApplicationShortcuts.StartupChange startup = null;
                var result = new PreparedBridgeResult();
                try {
                    token.ThrowIfCancellationRequested();
                    if (!cliProxy.RetryPendingCleanup(out result.Error)) return result;
                    if (request != null && request.Startup != null && request.StartupSnapshot != null && request.AutoLaunch != request.StartupSnapshot.Registered) {
                        try { startup = request.Startup.ChangeStartup(request.StartupSnapshot, request.AutoLaunch); }
                        catch { result.Error = new SettingsSaveError(SettingsField.Startup, "Не удалось изменить автозапуск. Ярлык недоступен или изменился вне ProGo. Проверьте его и снова откройте настройки; остальные параметры не применены."); return result; }
                    }
                    if (request == null && cliProxy.IsRunning) {
                        result.Value = new CliProxyBridgeService.PreparedBridgeConfiguration(); return result;
                    }
                    result.Value = cliProxy.PrepareConfiguration(request == null ? expected.Settings : request.Proposed, request != null && request.PickFree,
                        keepRunning, expected, request != null, token, out result.Error);
                    return result;
                } finally {
                    if (result.Value == null && startup != null && !startup.TryRollback())
                        result.Error = new SettingsSaveError(SettingsField.Startup, "Настройки не удалось полностью применить, а автозапуск — вернуть к прежнему состоянию. Позднейшие изменения сохранены; проверьте автозагрузку Windows.");
                }
            });
        }
        internal void StartAutomation()
        {
            homeVpn.AutoRestart = settings.Current.AutoRestartSocks;
            automation.Update(null, settings.Current);
            automationTimer.Tick += delegate
            {
                if (closing || shutdownPreparing || shutdownPrepared || IntegrationPending) return;
                bool ready = health.Current.SocksReady;
                foreach (ProxyFeature feature in Enum.GetValues(typeof(ProxyFeature)))
                {
                    var attempt = automation.BeginApply(feature, ready);
                    if (attempt == null) continue;
                    IntegrationWork = CompleteAutomation(feature, attempt); break;
                }
            };
            automationTimer.Start();
        }
        private void Execute(string action)
        {
            if (closing || shutdownPrepared) return;
            AppCommandDefinition command;
            if (!AppCommands.TryResolve(action, out command)) { SafeLog.Info("Unknown user action ignored."); return; }
            ExecuteCommand(command.Command);
        }
        private void ExecuteCommand(AppCommand action)
        {
            if (closing || shutdownPrepared) return;
            var command = AppCommands.Get(action);
            if (!AppCommands.CanExecute(action, GetCommandState())) return;
            if (command.RequiresRoute) { BeginRouteAction(action); return; }
            string navigation = command.Navigation;
            bool isPage = navigation != null;
            if (isPage && mainWindow != null) mainWindow.SetNavigation(navigation);
            try
            {
                switch (command.Command)
                {
                    case AppCommand.ShowMain: ShowStatus(); break;
                    case AppCommand.CreateBackup: CreateBackup(); break;
                    case AppCommand.RestoreBackup: StartRestore(); break;
                    case AppCommand.OpenBackups: OpenBackups(); break;
                    case AppCommand.CleanupBackups: CleanupBackups(); break;
                    case AppCommand.ExportHomeVpn: ShowPortableHomeVpn(true); break;
                    case AppCommand.ImportHomeVpn: ShowPortableHomeVpn(false); break;
                    case AppCommand.ExportDiagnostics: ShowDiagnosticPreview(); break;
                    case AppCommand.OpenAppLog: OpenLogFile(AppPaths.LogPath, "журнал приложения"); break;
                    case AppCommand.OpenUpdateLog: OpenLogFile(Path.Combine(AppPaths.Root, "update.log"), "журнал обновления"); break;
                    case AppCommand.OpenFolder: OpenProGoFolder(); break;
                    case AppCommand.Exit: ExitProGo(); break;
                    case AppCommand.StopDesktop:
                        StopDesktop(); break;
                    case AppCommand.StopPhone: homeVpn.Stop(); break;
                    case AppCommand.StopAll: try { StopDesktop(); } finally { homeVpn.Stop(); } break;
                    case AppCommand.Settings: ShowSettings(); break;
                    case AppCommand.Connections: ShowSettings(SettingsSection.Connections); break;
                    case AppCommand.WindowsSettings: ShowSettings(SettingsSection.Windows); break;
                    case AppCommand.Vault: ShowVault(); break;
                    case AppCommand.Phone: using (var form = new HomeVpnWizardForm(homeVpn, clipboard)) form.ShowDialog(mainWindow); break;
                    case AppCommand.Diagnostics:
                    case AppCommand.CheckRoute: using (var form = new StatusForm(settings, proxy, command.Command == AppCommand.CheckRoute, null, null, health, null, null, () => BackupStatus)) form.ShowDialog(mainWindow); break;
                    case AppCommand.StopCli: DisableCli(); break;
                    case AppCommand.DisableWindows: DisableWindows(); break;
                    case AppCommand.RemoveCodexShortcut:
                        try { CodexProxyService.Disable(); }
                        finally { appConsumers.Observe(); appConsumers.ReleaseIfUnused(); }
                        break;
                    case AppCommand.Help: ShowHelp(); break;
                    case AppCommand.Update: StartUpdate(); break;
                    default: throw new InvalidOperationException("Команда ProGo не поддерживается.");
                }
                UpdateTooltip();
            }
            catch (Exception ex) { SafeLog.Error("User action failed: " + command.LegacyId, ex); MessageBox.Show(ex.Message, "ProGo", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            finally { if (isPage && mainWindow != null && !mainWindow.IsDisposed) mainWindow.SetNavigation("home"); }
        }
        private void StopDesktop()
        {
            pendingRoutes.Clear(); RefreshPendingRoutes();
            foreach (ProxyFeature feature in Enum.GetValues(typeof(ProxyFeature))) automation.Cancel(feature);
            IntegrationWork = StopDesktopAsync();
            health.Invalidate();
        }
        private void BeginRouteAction(AppCommand action)
        {
            if (!AppCommands.CanExecute(action, GetCommandState())) return;
            long request = ++nextRouteRequest; pendingRoutes[action] = request;
            // A reconnect invalidates all previous requests before a new connection is awaited.
            if (action == AppCommand.Reconnect || (action == AppCommand.Connect && proxy.CurrentPid.HasValue)) {
                pendingRoutes.Clear(); pendingRoutes[action] = request; ObserveStop(proxy.StopTunnelAsync());
            }
            health.Invalidate(); RefreshPendingRoutes();
            CompleteRouteAction(action, request);
        }
        private async void CompleteRouteAction(AppCommand action, long request)
        {
            bool ready = false, cancelled = false; Exception failure = null;
            IDisposable routeConsumers = null;
            try { ready = await proxy.StartTunnelAsync(routeLifetime.Token).ConfigureAwait(false); }
            catch (OperationCanceledException) { cancelled = true; }
            catch (Exception ex) { failure = ex; }
            if (closing) return;
            if (ready && !cancelled && failure == null && (action == AppCommand.StartCli || action == AppCommand.EnableWindows)) {
                try {
                    var work = await DispatchUi(delegate {
                        long active;
                        if (!pendingRoutes.TryGetValue(action, out active) || active != request) return (Task)null;
                        var feature = action == AppCommand.StartCli ? ProxyFeature.Cli : ProxyFeature.Windows;
                        automation.Cancel(feature);
                        return EnableFeatureAsync(feature);
                    }).ConfigureAwait(false);
                    if (work != null) await work.ConfigureAwait(false);
                } catch (Exception ex) { failure = ex; }
            }
            if (ready && !cancelled && failure == null && (action == AppCommand.CreateCodexShortcut || action == AppCommand.OpenCodex || action == AppCommand.OpenTerminal)) {
                try {
                    var bridgeWork = await DispatchUi(delegate {
                        long active;
                        if (!pendingRoutes.TryGetValue(action, out active) || active != request) return (Task<SettingsSaveError>)null;
                        // Keep the endpoint between asynchronous preparation and
                        // the owner opening/tracking its actual scoped consumer.
                        routeConsumers = appConsumers.BeginMutation();
                        return EnsureBridgeAsync(routeLifetime.Token);
                    }).ConfigureAwait(false);
                    if (bridgeWork != null) { var bridgeError = await bridgeWork.ConfigureAwait(false); if (bridgeError != null) throw new InvalidOperationException(bridgeError.Message); }
                } catch (Exception ex) { failure = ex; }
            }
            try {
                // Modal dialogs may replace a captured WinForms synchronization context.
                // Always finish against this application's persistent UI dispatcher.
                activationDispatcher.BeginInvoke(new Action(delegate {
                    long active;
                    try {
                        if (closing || !pendingRoutes.TryGetValue(action, out active) || active != request) return;
                        if (cancelled) return;
                        if (failure != null) throw failure;
                        if (!ready) throw new InvalidOperationException(proxy.StartupError ?? "Прокси не готов. Проверьте сервер и SSH-ключ в «Подключениях».");
                        switch (action) {
                            case AppCommand.Connect:
                            case AppCommand.Reconnect: break;
                            case AppCommand.StartCli: CliReadyNotice(); break;
                            case AppCommand.EnableWindows: break;
                            case AppCommand.CreateCodexShortcut:
                                try { CodexProxyService.Enable(cliProxy.Port); }
                                finally { appConsumers.Invalidate(); }
                                break;
                            case AppCommand.OpenCodex: appConsumers.TrackWindow(CodexProxyService.Open(cliProxy.Port)); break;
                            case AppCommand.OpenTerminal: string error; Process window; if (!CliProxyEnvironmentService.OpenPowerShellWithEnvironment(cliProxy.Port, out error, out window)) throw new InvalidOperationException(error); appConsumers.TrackWindow(window); break;
                            default: throw new InvalidOperationException("Команда подключения ProGo не поддерживается.");
                        }
                        health.Invalidate();
                    }
                    catch (Exception ex) {
                        SafeLog.Error("User connection action failed: " + AppCommands.Get(action).LegacyId, ex);
                        MessageBox.Show(mainWindow, ex.Message, "Подключение ProGo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    finally {
                        if (routeConsumers != null) routeConsumers.Dispose();
                        if (pendingRoutes.TryGetValue(action, out active) && active == request) pendingRoutes.Remove(action);
                        appConsumers.ReleaseIfUnused();
                        if (!closing) RefreshPendingRoutes();
                    }
                }));
            }
            catch (InvalidOperationException ex) {
                if (routeConsumers != null) routeConsumers.Dispose();
                if (!closing) SafeLog.Error("Connection result could not reach the application UI.", ex);
            }
        }
        private AppCommandState GetCommandState()
        { return new AppCommandState(pendingRoutes.Keys, proxy.IsConnecting || proxy.IsStopping, closing || shutdownPrepared || shutdownPreparing, IsManualBackupRunning || IsPortableRunning, IntegrationPending); }
        private void RefreshPendingRoutes()
        {
            var state = GetCommandState();
            foreach (var item in commandItems) item.Value.Enabled = AppCommands.CanExecute(item.Key, state);
            if (backupStatusItem != null) {
                backupStatusItem.Text = "Состояние копии: " + (IsBackupRunning ? "проверка и копирование…" :
                    String.IsNullOrEmpty(backupStatus) ? "ещё не проверена" : lastBackupFailed ? "ошибка" : lastBackupCancelled ? "отменено" : "готова");
                backupStatusItem.Enabled = !String.IsNullOrEmpty(backupStatus);
                backupStatusItem.ToolTipText = backupStatus + " Открыть состояние копии. Ручная копия создаётся отдельной командой после окончания проверки.";
                backupStatusItem.AccessibleDescription = backupStatusItem.ToolTipText;
            }
            if (mainWindow != null && !mainWindow.IsDisposed) mainWindow.RefreshConnectionState();
            if (CommandStateChanged != null) CommandStateChanged();
        }
        private void CancelPendingRoute(AppCommand action)
        {
            pendingRoutes.Remove(action);
            if (pendingRoutes.Count == 0 && (proxy.IsConnecting || proxy.IsStopping)) ObserveStop(proxy.StopTunnelAsync());
            RefreshPendingRoutes();
        }
        private void NotifyPortChange(int previous)
        {
            if (previous == settings.Current.HttpProxyPort) return;
            tray.ShowBalloonTip(7000, "Новый порт приложений", "Адрес: " + cliProxy.ProxyUrl + ". Настройки прокси ProGo обновлены. Перезапустите открытые терминалы и Codex.", ToolTipIcon.Info);
        }
        private void EnableFeature(ProxyFeature feature)
        {
            IntegrationWork = ObserveIntegration(EnableFeatureAsync(feature), null);
        }
        private void DisableCli()
        {
            CancelPendingRoute(AppCommand.StartCli);
            automation.Cancel(ProxyFeature.Cli);
            IntegrationWork = ObserveIntegration(DisableFeatureAsync(ProxyFeature.Cli), delegate {
                var remaining = appConsumers.Summary;
                tray.ShowBalloonTip(7000, "Прокси для новых терминалов выключен", "Полностью перезапустите уже открытые терминалы и Codex." +
                    (cliProxy.IsRunning ? " Общий прокси продолжает работать для: " + remaining + ". Для полной остановки нажмите «Отключить прокси на ПК»." : " Порт приложений освобождён."), ToolTipIcon.Info);
            });
        }
        private void CliReadyNotice()
        {
            tray.ShowBalloonTip(6000, "CLI-прокси включён", "Codex можно запускать обычным способом. Полностью перезапустите уже открытый терминал или приложение с Codex. Отдельный ярлык не нужен.", ToolTipIcon.Info);
        }
        private sealed class IntegrationResult
        {
            internal bool Cli, Windows, Skipped;
            internal Exception CliFailure, WindowsFailure;
            internal WindowsProxyRestoreResult WindowsRestore;
            internal Task<bool> TunnelStop;
            internal void ThrowIfFailed(bool stopping)
            {
                var errors = new List<string>();
                if (CliFailure != null) errors.Add("Не удалось изменить настройки терминалов. Повторите выключение CLI после проверки прав записи.");
                if (WindowsFailure != null) errors.Add(WindowsRestore == null ? "Не удалось изменить настройки прокси Windows. Проверьте права записи и повторите команду." : WindowsRestore.Message);
                if (errors.Count != 0) throw new InvalidOperationException(String.Join("\n\n", errors.ToArray()) +
                    (stopping ? "\n\nПрокси на ПК продолжает работать, чтобы не оборвать доступ. Повторите отключение после устранения ошибки." : ""), CliFailure ?? WindowsFailure);
            }
        }
        private void DisableWindows()
        {
            CancelPendingRoute(AppCommand.EnableWindows); automation.Cancel(ProxyFeature.Windows);
            IntegrationWork = ObserveIntegration(DisableFeatureAsync(ProxyFeature.Windows), null);
        }
        private async Task DisableFeatureAsync(ProxyFeature feature)
        {
            var result = await QueueIntegration(feature == ProxyFeature.Cli, feature == ProxyFeature.Windows, false, false).ConfigureAwait(false);
            result.ThrowIfFailed(false);
        }
        private async Task EnableFeatureAsync(ProxyFeature feature)
        {
            var result = await QueueIntegration(feature == ProxyFeature.Cli, feature == ProxyFeature.Windows, true, false).ConfigureAwait(false);
            result.ThrowIfFailed(false);
        }
        private async Task StopDesktopAsync()
        {
            Exception failure = null;
            try {
                var result = await QueueIntegration(true, true, false, true).ConfigureAwait(false);
                result.ThrowIfFailed(true);
                if (result.TunnelStop != null) await ObserveStopAsync(result.TunnelStop).ConfigureAwait(false);
            } catch (Exception ex) { failure = ex; }
            if (failure != null)
                await IntegrationUi(delegate { RefreshPendingRoutes(); SafeLog.Error("Desktop proxy cleanup failed.", failure); MessageBox.Show(failure.Message, "ProGo", MessageBoxButtons.OK, MessageBoxIcon.Warning); }).ConfigureAwait(false);
        }
        private async Task ObserveIntegration(Task work, Action succeeded)
        {
            Exception failure = null;
            try { await work.ConfigureAwait(false); } catch (Exception ex) { failure = ex; }
            await IntegrationUi(delegate {
                RefreshPendingRoutes();
                if (failure != null) { SafeLog.Error("Proxy integration action failed.", failure); MessageBox.Show(failure.Message, "ProGo", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
                else if (succeeded != null) succeeded();
            }).ConfigureAwait(false);
        }
        private async Task CompleteAutomation(ProxyFeature feature, object attempt)
        {
            Exception failure = null;
            try { await EnableFeatureAsync(feature).ConfigureAwait(false); } catch (Exception ex) { failure = ex; }
            await IntegrationUi(delegate {
                RefreshPendingRoutes();
                Exception error; var result = automation.CompleteApply(feature, attempt, failure, out error);
                if (error == null) return;
                SafeLog.Error("Automatic proxy setup failed: " + feature + ".", error);
                if (automation.FailureCount(feature) == 1 || result == AutomationResult.Paused)
                    tray.ShowBalloonTip(6000, "Автонастройка " + AutomationPlan.FeatureName(feature), error.Message +
                        (result == AutomationResult.Paused ? "\nПовторы остановлены. Проверьте настройки и нажмите «Включить»." : "\nProGo повторит попытку автоматически."), ToolTipIcon.Warning);
            }).ConfigureAwait(false);
        }
        private Task<IntegrationResult> QueueIntegration(bool cli, bool windows, bool enable, bool stopBridge)
        {
            if (closing) return Task.FromResult(new IntegrationResult { Skipped = true });
            long expectedCli = cli ? System.Threading.Interlocked.Increment(ref cliIntent) : System.Threading.Interlocked.Read(ref cliIntent);
            long expectedWindows = windows ? System.Threading.Interlocked.Increment(ref windowsIntent) : System.Threading.Interlocked.Read(ref windowsIntent);
            var consumers = appConsumers.BeginMutation();
            System.Threading.Interlocked.Increment(ref integrationPending); RefreshPendingRoutes();
            var work = RunIntegration(cli, windows, enable, stopBridge, expectedCli, expectedWindows, consumers);
            IntegrationWork = work; return work;
        }
        private async Task<IntegrationResult> RunIntegration(bool cli, bool windows, bool enable, bool stopBridge,
            long expectedCli, long expectedWindows, IDisposable consumers)
        {
            int released = 0;
            Action release = delegate {
                if (System.Threading.Interlocked.Exchange(ref released, 1) != 0) return;
                consumers.Dispose(); System.Threading.Interlocked.Decrement(ref integrationPending);
            };
            IDisposable retention = null; bool acquired = false;
            var result = new IntegrationResult { Skipped = true };
            try {
                try { await integrationGate.WaitAsync(integrationWaitCancellation.Token).ConfigureAwait(false); }
                catch (OperationCanceledException) { return result; }
                acquired = true;
                AppSettings current = null; long revision = 0;
                if (!await IntegrationUi(delegate {
                    if (enable) {
                        cli = cli && System.Threading.Interlocked.Read(ref cliIntent) == expectedCli;
                        windows = windows && System.Threading.Interlocked.Read(ref windowsIntent) == expectedWindows;
                        if (!cli && !windows) return;
                    }
                    current = settings.Current.Clone(); current.HttpProxyPort = cliProxy.Port; revision = cliProxy.ConsumerRevision;
                }).ConfigureAwait(false) || current == null) return result;
                lock (integrationLifetimeGate) {
                    if (closing) return result;
                    if (enable) {
                        cli = cli && System.Threading.Interlocked.Read(ref cliIntent) == expectedCli;
                        windows = windows && System.Threading.Interlocked.Read(ref windowsIntent) == expectedWindows;
                        if (!cli && !windows) return result;
                    }
                    retention = cliProxy.RetainForNativeMutation();
                }
                SettingsSaveError cleanupError;
                if (!await Task.Run(delegate { return cliProxy.RetryPendingCleanup(out cleanupError); }).ConfigureAwait(false))
                    throw new InvalidOperationException("Очистка переноса прокси не завершена. Порты сохранены; проверьте права записи и повторите команду.");
                if (enable) {
                    var expected = settings.Capture(); int previousPort = cliProxy.Port;
                    PreparedBridgeResult prepared;
                    using (var preparationCancellation = CancellationTokenSource.CreateLinkedTokenSource(integrationWaitCancellation.Token)) {
                        settingsOperationCancellation = preparationCancellation; preparationCancellation.CancelAfter(30000);
                        try { prepared = await PrepareBridgeWorker(null, expected, true, preparationCancellation.Token).ConfigureAwait(false); }
                        finally { if (ReferenceEquals(settingsOperationCancellation, preparationCancellation)) settingsOperationCancellation = null; }
                    }
                    if (prepared.Error != null) throw new InvalidOperationException(prepared.Error.Message);
                    if (!await IntegrationUi(delegate { cliProxy.PublishPrepared(prepared.Value); appConsumers.Observe(); NotifyPortChange(previousPort); }).ConfigureAwait(false)) {
                        cliProxy.PublishPrepared(prepared.Value); return result;
                    }
                }
                if (!await IntegrationUi(delegate {
                    if (enable) {
                        cli = cli && System.Threading.Interlocked.Read(ref cliIntent) == expectedCli;
                        windows = windows && System.Threading.Interlocked.Read(ref windowsIntent) == expectedWindows;
                    }
                    current = settings.Current.Clone(); current.HttpProxyPort = cliProxy.Port; revision = cliProxy.ConsumerRevision;
                }).ConfigureAwait(false) || (enable && !cli && !windows)) return result;
                // These delegates perform only fresh native/file ownership transactions.
                // No consumer, listener, tray or form is touched by the worker.
                result = await Task.Run(() => RestoreIntegrations(cli, windows, enable, current)).ConfigureAwait(false);
                await IntegrationUi(delegate {
                    try {
                        ApplyIntegrationResult(result, enable, stopBridge, expectedCli, expectedWindows, revision);
                    } finally { release(); RefreshPendingRoutes(); }
                }).ConfigureAwait(false);
                return result;
            } finally {
                release();
                if (retention != null) retention.Dispose();
                if (acquired) integrationGate.Release();
            }
        }
        private IntegrationResult RestoreIntegrations(bool cli, bool windows, bool enable, AppSettings current)
        {
            var result = new IntegrationResult { Cli = cli, Windows = windows };
            if (cli) try {
                if (enable) applyIntegration(ProxyFeature.Cli, current); else restoreCli();
            } catch (Exception ex) { result.CliFailure = ex; SafeLog.Error("Environment proxy transaction failed.", ex); }
            if (windows) try {
                if (enable) applyIntegration(ProxyFeature.Windows, current);
                else {
                    result.WindowsRestore = restoreWindows();
                    if (!result.WindowsRestore.Completed) result.WindowsFailure = new InvalidOperationException(result.WindowsRestore.Message);
                }
            } catch (Exception ex) { result.WindowsFailure = ex; SafeLog.Error("Windows proxy transaction failed.", ex); }
            return result;
        }
        private void ApplyIntegrationResult(IntegrationResult result, bool enable, bool stopBridge, long expectedCli, long expectedWindows, long revision)
        {
            bool cliCurrent = System.Threading.Interlocked.Read(ref cliIntent) == expectedCli;
            bool windowsCurrent = System.Threading.Interlocked.Read(ref windowsIntent) == expectedWindows;
            if (result.Cli) appConsumers.CliCleanupPending = result.CliFailure != null || cliProxy.CleanupPending;
            if (result.Windows) appConsumers.WindowsCleanupPending = result.WindowsFailure != null || cliProxy.CleanupPending;
            appConsumers.Observe();
            if (!enable && windowsCurrent && result.WindowsRestore != null && result.WindowsRestore.Completed && result.WindowsRestore.PreservedExternal)
                tray.ShowBalloonTip(5000, "Настройки Windows сохранены", result.WindowsRestore.Message, ToolTipIcon.Info);
            if (stopBridge && cliCurrent && windowsCurrent && revision == cliProxy.ConsumerRevision && result.CliFailure == null && result.WindowsFailure == null) {
                cliProxy.Stop(); appConsumers.ForgetWindows();
                // Accept stop intent on the owner before pending state is released.
                // A newer reconnect may then join cleanup without a stale worker
                // continuation cancelling that newer request.
                result.TunnelStop = proxy.StopTunnelAsync();
            }
        }
        private async Task<bool> IntegrationUi(Action action)
        {
            if (closing) return false;
            var work = DispatchUi(delegate { action(); return true; });
            if (await Task.WhenAny(work, integrationOwnerClosed.Task).ConfigureAwait(false) != work) return false;
            return await work.ConfigureAwait(false);
        }
        private void ShowHelp()
        {
            using (var form = new HelpForm(ExecuteCommand)) form.ShowDialog();
        }

        private void ShowDiagnosticPreview()
        {
            try { DiagnosticPreview.Show(AppPaths.Root, typeof(DiagnosticPreview).Assembly.GetName().Version.ToString(3)); }
            catch { MessageBox.Show("Не удалось подготовить отчёт. Личный журнал можно открыть на этом компьютере.", AppConstants.ProductName); }
        }

        private void ShowVault()
        {
            VaultSession session;
            if (!PinForm.OpenSession(out session)) return;
            using (var form = new VaultForm(session, clipboard, settings)) form.ShowDialog();
        }

        private Task<bool> PrepareShutdownAsync(bool dialog)
        {
            if (shutdownTask != null && !shutdownTask.IsCompleted) return shutdownTask;
            if (closing) return Task.FromResult(false);
            if (shutdownPrepared) return Task.FromResult(true);
            shutdownTask = PrepareShutdownCore(dialog); return shutdownTask;
        }
        private async Task<bool> PrepareShutdownCore(bool dialog)
        {
            shutdownPreparing = true;
            pendingRoutes.Clear(); RefreshPendingRoutes();
            foreach (ProxyFeature feature in Enum.GetValues(typeof(ProxyFeature))) automation.Cancel(feature);
            Exception failure = null;
            try {
                CancelBackup();
                CancelPortable(); var portable = PortableWork;
                if (portable != null && !portable.IsCompleted && await Task.WhenAny(portable, Task.Delay(backupShutdownTimeoutMilliseconds)).ConfigureAwait(false) != portable)
                    throw new InvalidOperationException("Перенос VPN ещё не остановлен: накопитель не завершил текущую операцию. ProGo остаётся запущенным. Дождитесь завершения переноса и повторите выход или обслуживание.");
                if (portable != null) try { await portable.ConfigureAwait(false); } catch (Exception) { }
                var backup = backupWorker;
                if (backup != null)
                {
                    if (await Task.WhenAny(backup, Task.Delay(backupShutdownTimeoutMilliseconds)).ConfigureAwait(false) != backup)
                        throw new InvalidOperationException("Копирование ещё не остановлено: накопитель не завершил текущую операцию. ProGo остаётся запущенным. Дождитесь завершения копирования и повторите выход или обслуживание.");
                    try { await backup.ConfigureAwait(false); }
                    catch (OperationCanceledException) { }
                    catch (Exception ex) { SafeLog.Error("Backup ended with an error before shutdown.", ex); }
                }
                var settingsCancellation = settingsOperationCancellation;
                if (settingsCancellation != null) try { settingsCancellation.Cancel(); } catch (ObjectDisposedException) { }
                var settingsWork = SettingsWork;
                if (settingsWork != null && !settingsWork.IsCompleted && await Task.WhenAny(settingsWork, Task.Delay(settingsShutdownTimeoutMilliseconds)).ConfigureAwait(false) != settingsWork)
                    throw new InvalidOperationException("Настройки ещё применяются: Windows или накопитель не завершили текущую операцию. ProGo остаётся запущенным. Дождитесь окончания применения и повторите выход или обслуживание.");
                var integrationWork = IntegrationWork;
                if (integrationWork != null && !integrationWork.IsCompleted && await Task.WhenAny(integrationWork, Task.Delay(settingsShutdownTimeoutMilliseconds)).ConfigureAwait(false) != integrationWork)
                    throw new InvalidOperationException("Изменение прокси ещё не завершено. Порты и копии сохранены; ProGo остаётся запущенным. Дождитесь текущей операции Windows и повторите выход или обслуживание.");
                var cleanup = await DispatchUi(() => QueueIntegration(true, true, false, true)).ConfigureAwait(false);
                if (cleanup == null) return false;
                var integrations = await cleanup.ConfigureAwait(false);
                if (integrations.Skipped || closing) return false;
                integrations.ThrowIfFailed(true);
                if (integrations.TunnelStop == null || !await integrations.TunnelStop.ConfigureAwait(false)) throw new InvalidOperationException("Не удалось остановить SSH-процесс. ProGo остаётся запущенным; повторите отключение.");
            } catch (Exception ex) { failure = ex; }
            return await DispatchUi(delegate {
                try {
                    if (failure != null) {
                        SafeLog.Error("Shutdown refused: owned proxy cleanup incomplete.", failure);
                        if (dialog) MessageBox.Show(failure.Message, "ProGo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        else tray.ShowBalloonTip(10000, "ProGo остаётся запущенным", failure.Message, ToolTipIcon.Warning);
                        return false;
                    }
                    shutdownPrepared = true;
                    if (updateForm != null && !updateForm.IsDisposed) updateForm.CancelAndClose();
                    return true;
                } finally { shutdownPreparing = false; RefreshPendingRoutes(); }
            }).ConfigureAwait(false);
        }
        // A modal dialog may remove or replace SynchronizationContext. The persistent
        // application control is the owner of every lifecycle/UI completion instead.
        private Task<T> DispatchUi<T>(Func<T> action)
        {
            if (closing || activationDispatcher.IsDisposed) return Task.FromResult(default(T));
            // InvokeRequired becomes false after handle destruction, even on a
            // worker. The constructor's thread is the only direct UI owner.
            if (System.Threading.Thread.CurrentThread.ManagedThreadId == ownerThreadId) return Task.FromResult(action());
            var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            Action drop = delegate { completion.TrySetResult(default(T)); };
            lock (uiCompletionGate) {
                if (closing) return Task.FromResult(default(T));
                uiCompletions.Add(drop);
            }
            try {
                activationDispatcher.BeginInvoke(new Action(delegate {
                    try {
                        if (closing || activationDispatcher.IsDisposed) { drop(); return; }
                        completion.TrySetResult(action());
                    }
                    catch (Exception ex) { completion.TrySetException(ex); }
                    finally { lock (uiCompletionGate) uiCompletions.Remove(drop); }
                }));
            } catch (InvalidOperationException) {
                lock (uiCompletionGate) uiCompletions.Remove(drop);
                drop();
            }
            return completion.Task;
        }
        // Pipe worker may await the UI-owned cleanup; no synchronous wait runs on UI.
        internal Task<bool> RequestShutdownAsync()
        {
            if (activationDispatcher.IsDisposed || closing) return Task.FromResult(false);
            if (System.Threading.Thread.CurrentThread.ManagedThreadId != ownerThreadId) return RequestShutdownFromWorker();
            return PrepareShutdownAsync(false);
        }
        private async Task<bool> RequestShutdownFromWorker()
        {
            var work = await DispatchUi(() => PrepareShutdownAsync(false)).ConfigureAwait(false);
            return work != null && await work.ConfigureAwait(false);
        }
        internal bool RequestShutdown()
        {
            var result = RequestShutdownAsync();
            if (System.Threading.Thread.CurrentThread.ManagedThreadId != ownerThreadId) return result.GetAwaiter().GetResult();
            return result.IsCompleted && result.GetAwaiter().GetResult();
        }
        private async void ObserveStop(Task<bool> work)
        {
            await ObserveStopAsync(work).ConfigureAwait(false);
        }
        private async Task ObserveStopAsync(Task<bool> work)
        {
            bool stopped = false;
            try { stopped = await work.ConfigureAwait(false); }
            catch (Exception ex) { SafeLog.Error("SSH asynchronous stop failed.", ex); }
            await DispatchUi(delegate {
                if (!stopped) tray.ShowBalloonTip(7000, "Подключение ещё не остановлено", "Не удалось остановить SSH-процесс. Повторите отключение; ProGo сохраняет управление этим процессом.", ToolTipIcon.Warning);
                RefreshPendingRoutes(); return true;
            }).ConfigureAwait(false);
        }
        internal void CompleteShutdown()
        {
            if (closing || activationDispatcher.IsDisposed) return;
            try { activationDispatcher.BeginInvoke(new Action(delegate {
                if (!shutdownPrepared || closing) return;
                closing = true; homeVpn.Stop(); tray.Visible = false; ExitThread();
            })); } catch (InvalidOperationException) { }
        }
        internal void CancelShutdown()
        {
            if (closing || activationDispatcher.IsDisposed) return;
            try { activationDispatcher.BeginInvoke(new Action(delegate { if (!closing) { shutdownPrepared = false; RefreshPendingRoutes(); } })); }
            catch (InvalidOperationException) { }
        }
        private async Task<bool> BeginMaintenance(Func<bool> launch)
        {
            if (!await PrepareShutdownAsync(true).ConfigureAwait(false)) return false;
            return await DispatchUi(delegate {
                bool handedOff = false;
                try { handedOff = launch(); if (handedOff) CompleteShutdown(); return handedOff; }
                finally { if (!handedOff) { shutdownPrepared = false; RefreshPendingRoutes(); } }
            }).ConfigureAwait(false);
        }
        private async void ExitProGo()
        {
            if (await PrepareShutdownAsync(true).ConfigureAwait(false)) CompleteShutdown();
        }

        private void OpenLogFile(string path, string title)
        {
            try
            {
                AppPaths.EnsureDirectories();
                if (String.Equals(path, Path.Combine(AppPaths.Root, "update.log"), StringComparison.OrdinalIgnoreCase))
                    path = BoundedLog.UpdaterLogPath(AppPaths.Root);
                if (!File.Exists(path)) File.WriteAllText(path, "");
                Process.Start("notepad.exe", path);
            }
            catch (Exception ex)
            {
                SafeLog.Error("Open log failed: " + title + ".", ex);
                MessageBox.Show("Не удалось открыть " + title + ".", AppConstants.ProductName);
            }
        }

        private void OpenProGoFolder()
        {
            try
            {
                AppPaths.EnsureDirectories();
                Process.Start("explorer.exe", AppPaths.Root);
            }
            catch (Exception ex)
            {
                SafeLog.Error("Open ProGo folder failed.", ex);
                MessageBox.Show("Не удалось открыть папку ProGo.", AppConstants.ProductName);
            }
        }

        private void OpenBackups()
        {
            try
            {
                Directory.CreateDirectory(BackupService.BackupsRoot);
                Process.Start("explorer.exe", BackupService.BackupsRoot);
            }
            catch (Exception ex)
            {
                SafeLog.Error("Open backups failed.", ex);
                MessageBox.Show("Не удалось открыть папку резервных копий.", AppConstants.ProductName);
            }
        }

        private void CleanupBackups()
        {
            try
            {
                var plan = BackupRetention.Plan(BackupService.BackupsRoot);
                if (plan.Candidates.Count == 0)
                {
                    MessageBox.Show("Старых автоматических копий для удаления нет. Ручные и неизвестные папки сохраняются.",
                        "Очистка резервных копий ProGo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                using (var preview = new BackupCleanupForm(plan))
                    if (preview.ShowDialog() != DialogResult.OK) return;
                var cleanup = BackupService.ApplyCleanupPlan(plan, true);
                MessageBox.Show(cleanup.Message, "Очистка резервных копий ProGo", MessageBoxButtons.OK, cleanup.Failed > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                SafeLog.Error("Backup cleanup failed.", ex);
                MessageBox.Show("Не удалось очистить резервные копии. Подробности записаны в журнал.", "Очистка резервных копий ProGo", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CreateBackup() { CreateManualBackupAsync(); }
        private void CancelPortable()
        { var form = portableForm; if (form != null && !form.IsDisposed) form.CancelCurrentOperation(); }

        private void ShowPortableHomeVpn(bool exporting)
        {
            if (closing || shutdownPrepared || shutdownPreparing || IsBackupRunning) return;
            if (portableForm != null && !portableForm.IsDisposed) { portableForm.Activate(); return; }
            if (IsPortableRunning) return; // A forced form disposal still owns its worker.
            var form = createPortableForm(exporting); portableForm = form;
            form.CanStart = () => !closing && !shutdownPrepared && !shutdownPreparing && !IsBackupRunning && !IsPortableRunning;
            form.WorkStarted += delegate { PortableWork = form.Work; if (!closing) RefreshPendingRoutes(); };
            form.ResultApplied += delegate { if (!closing) RefreshPendingRoutes(); };
            form.FormClosed += delegate { if (ReferenceEquals(portableForm, form)) portableForm = null; if (!closing) RefreshPendingRoutes(); };
            form.Show(mainWindow);
        }

        // Program queues this only after instance.Attach. No disk work starts until
        // the owner dispatcher receives its first message from Application.Run.
        internal Task StartStartupBackupAsync()
        {
            if (startupBackupScheduled) return startupBackupCompletion;
            if (closing || shutdownPrepared || shutdownPreparing) return Task.FromResult(false);
            if (IsPortableRunning) return Task.FromResult(false);
            if (activationDispatcher.InvokeRequired) throw new InvalidOperationException("Стартовая копия запускается владельцем интерфейса.");
            if (IsBackupRunning) return backupCompletion;
            if (backupForm != null && !backupForm.IsDisposed) backupForm.Close();
            startupBackupScheduled = true; backupQueued = true; backupOperationKind = "baseline";
            backupStatus = "Стартовая резервная копия: проверка ожидает запуска интерфейса.";
            var cancellation = new CancellationTokenSource(); backupCancellation = cancellation;
            var source = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            startupBackupCompletionSource = source;
            startupBackupCompletion = backupCompletion = source.Task;
            RefreshPendingRoutes();
            try { activationDispatcher.BeginInvoke(new Action(delegate {
                backupQueued = false;
                if (closing) { cancellation.Dispose(); source.TrySetResult(false); return; }
                if (shutdownPrepared || shutdownPreparing || cancellation.IsCancellationRequested) {
                    backupStatus = "Проверка стартовой копии отменена. Текущие данные не заменены.";
                    backupCancellation = null; cancellation.Dispose();
                    lastBackupCancelled = true; lastBackupFailed = false; lastBackupResult = null;
                    if (backupForm != null && !backupForm.IsDisposed) backupForm.Complete(null, null, true, false);
                    RefreshPendingRoutes(); source.TrySetResult(false); return;
                }
                BeginBackup(true, cancellation, source);
            })); }
            catch (InvalidOperationException) {
                backupQueued = false; backupCancellation = null; cancellation.Dispose();
                backupStatus = "Проверка стартовой копии не запущена: владелец интерфейса закрывается.";
                lastBackupCancelled = true; lastBackupFailed = false; lastBackupResult = null;
                if (!closing) RefreshPendingRoutes(); source.TrySetResult(false);
            }
            return startupBackupCompletion;
        }

        internal Task CreateManualBackupAsync()
        {
            if (closing || shutdownPrepared || shutdownPreparing) return Task.FromResult(false);
            if (IsPortableRunning) return Task.FromResult(false);
            if (IsBackupRunning) { ShowBackupProgress(); return backupCompletion; }
            return BeginBackup(false, new CancellationTokenSource(), null);
        }

        private Task BeginBackup(bool baseline, CancellationTokenSource cancellation, TaskCompletionSource<bool> startupSource)
        {
            // Progress may already be open for the queued baseline. Closing a busy
            // form requests cancellation, so keep it when its own actor starts.
            if (backupForm != null && !backupForm.IsDisposed && !backupForm.IsBusy) backupForm.Close();
            backupCancellation = cancellation; backupOperationKind = baseline ? "baseline" : "manual";
            lastBackupResult = null; lastBackupCancelled = lastBackupFailed = false;
            backupStatus = baseline ? "Стартовая резервная копия: проверяем и при необходимости создаём в фоне." : "Ручная резервная копия создаётся в фоне.";
            var token = cancellation.Token;
            var worker = Task.Run(() => {
                try {
                    var path = baseline ? BackupService.EnsureVersionBackupExists("startup", token, beforeBackupCopy, beforeBaselineProbe) :
                        BackupService.CreateBackup("manual", token, beforeBackupCopy);
                    // A returned path crossed the accepted-copy/retention boundary.
                    // Late cancellation cannot relabel that committed copy as aborted.
                    var result = new BackupResult { Path = path, Contents = path == null ? null : BackupIntegrity.Contents(path), Existing = path == null };
                    if (baseline) SafeLog.Info(path == null ? "Existing version baseline backup verified." : "Version baseline backup created: " + path + ".");
                    return result;
                } catch (OperationCanceledException) { throw; }
                catch (Exception ex) { SafeLog.Error(baseline ? "Version baseline backup failed." : "Manual backup failed.", ex); throw; }
            });
            backupWorker = worker;
            var completion = CompleteBackupAsync(worker, cancellation, baseline);
            backupCompletion = startupSource == null ? completion : startupSource.Task;
            if (startupSource != null) CompleteStartupBackupAsync(completion, startupSource);
            if (!baseline) ShowBackupProgress();
            RefreshPendingRoutes(); return backupCompletion;
        }

        private async void CompleteStartupBackupAsync(Task completion, TaskCompletionSource<bool> source)
        {
            try { await completion.ConfigureAwait(false); source.TrySetResult(true); }
            catch (Exception) { source.TrySetResult(false); }
        }

        private sealed class BackupResult { internal string Path, Contents; internal bool Existing; }

        private void CancelBackup()
        { var cancellation = backupCancellation; if (cancellation != null) try { cancellation.Cancel(); } catch (ObjectDisposedException) { } }

        internal void ShowBackupProgress()
        {
            if (closing || String.IsNullOrEmpty(backupOperationKind)) return;
            if (backupForm != null && !backupForm.IsDisposed) { backupForm.Activate(); return; }
            var form = new BackupCreationForm(CancelBackup, backupOperationKind == "baseline"); backupForm = form;
            form.FormClosed += delegate { if (ReferenceEquals(backupForm, form)) backupForm = null; };
            if (!IsBackupRunning) form.Complete(lastBackupResult == null ? null : lastBackupResult.Path,
                lastBackupResult == null ? null : lastBackupResult.Contents, lastBackupCancelled, lastBackupFailed,
                lastBackupResult != null && lastBackupResult.Existing);
            form.Show(mainWindow);
        }

        private async Task CompleteBackupAsync(Task<BackupResult> worker, CancellationTokenSource cancellation, bool baseline)
        {
            BackupResult result = null; bool cancelled = false, failed = false;
            try
            {
                try { result = await worker.ConfigureAwait(false); }
                catch (OperationCanceledException) { cancelled = true; }
                catch (Exception) { failed = true; }
                var completion = DispatchUi(delegate {
                    if (!ReferenceEquals(backupWorker, worker)) return false;
                    backupWorker = null; backupCancellation = null;
                    lastBackupResult = result; lastBackupCancelled = cancelled; lastBackupFailed = failed;
                    backupStatus = cancelled ? "Резервное копирование отменено. Текущие данные не заменены." :
                        failed ? "Резервная копия не готова: ошибка проверки или копирования. Откройте состояние копии и журнал." :
                        (result.Existing ? "Стартовая копия уже есть и проверена. Новая копия не создавалась." :
                        (baseline ? "Стартовая резервная копия создана и проверена." : "Ручная резервная копия создана и проверена."));
                    if (backupForm != null && !backupForm.IsDisposed) backupForm.Complete(result == null ? null : result.Path,
                        result == null ? null : result.Contents, cancelled, failed, result != null && result.Existing);
                    RefreshPendingRoutes(); return true;
                });
                // A queued WinForms delegate may be discarded during Dispose. The
                // worker still settles without waiting for that vanished UI owner.
                if (await Task.WhenAny(completion, backupOwnerClosed.Task).ConfigureAwait(false) == completion)
                    await completion.ConfigureAwait(false);
            }
            catch (Exception) { }
            finally { cancellation.Dispose(); }
        }

        private async void StartRestore()
        {
            var backups = BackupService.ListBackups();
            if (backups.Count == 0)
            {
                MessageBox.Show("Резервные копии не найдены. Сначала создайте резервную копию или дождитесь следующего обновления версии.", "Восстановление ProGo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string backupDir;
            if (!BackupPickerForm.TryPick(backups, out backupDir)) return;

            PreparedBackup prepared = null;
            try
            {
                try
                {
                    string scope; bool dataConfirmed;
                    using (var options = new RestoreOptionsForm(backupDir))
                    {
                        if (options.ShowDialog() != DialogResult.OK) return;
                        prepared = options.TakePreparedCopy();
                        if (closing) return;
                        scope = options.Scope; dataConfirmed = options.DataConfirmed;
                        var names = BackupIntegrity.RestoreNames(prepared.Path, scope, dataConfirmed);
                        var result = MessageBox.Show(
                            "Копия проверена. Версия в копии: " + File.ReadAllText(Path.Combine(prepared.Path, "VERSION")).Trim() +
                            "\n\nБудет восстановлено:\n" + String.Join("\n", names) +
                            (scope == "Program" ? "\n\nТекущие настройки и хранилище сохранятся." : "\n\nПеречисленные пользовательские данные будут заменены данными из копии.") +
                            "\nТекущие VPN-доступы и снимки прокси сохранятся; архивные не импортируются." +
                            "\n\nProGo закроется и запустится снова. Начать восстановление?",
                            "Подтвердите восстановление", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                        if (result != DialogResult.Yes) return;
                    } // The UI form is disposed on its owner thread before asynchronous cleanup.
                    if (!await BeginMaintenance(() => BackupService.StartRestore(prepared.Path, scope, dataConfirmed)).ConfigureAwait(false)) return;
                    SafeLog.Info("Restore requested by user. scope=" + scope + ".");
                } finally { if (prepared != null) prepared.Dispose(); }
            }
            catch (Exception ex)
            {
                SafeLog.Error("Restore preparation or cleanup failed.", ex);
                DispatchUi(delegate { MessageBox.Show("Восстановление не запущено: " + ex.Message, "Восстановление ProGo", MessageBoxButtons.OK, MessageBoxIcon.Warning); return true; });
            }
        }

        private async void StartUpdate()
        {
            if (updateForm != null && !updateForm.IsDisposed) { updateForm.Activate(); return; }
            UpdateCheckResult check;
            using (var form = createUpdateForm()) {
                updateForm = form;
                try {
                    if (form.ShowDialog(mainWindow) != DialogResult.OK || closing || shutdownPrepared) return;
                    check = form.AcceptedResult;
                } finally { updateForm = null; }
            }
            if (check == null || check.Availability != UpdateAvailability.Available) return;
            if (!await BeginMaintenance(UpdateLauncher.StartUpdater).ConfigureAwait(false)) return;
            SafeLog.Info("Update requested by user. local=" + check.LocalVersion + "; remote=" + check.RemoteVersion + ".");
        }

        private void UpdateTooltip()
        {
            tray.Text = health.Current.TrayText;
        }
        private void HealthChanged()
        {
            if (activationDispatcher.IsDisposed || !activationDispatcher.IsHandleCreated) return;
            try { activationDispatcher.BeginInvoke((Action)delegate {
                if (activationDispatcher.IsDisposed) return;
                UpdateTooltip();
                if (mainWindow != null && !mainWindow.IsDisposed) mainWindow.RefreshConnectionState();
            }); } catch (InvalidOperationException) { }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (disposed) return; // WinForms and Program's using scope can both dispose the context.
                disposed = true;
                lock (integrationLifetimeGate) closing = true;
                integrationWaitCancellation.Cancel();
                integrationWaitCancellation.Dispose();
                integrationOwnerClosed.TrySetResult(true);
                lock (uiCompletionGate) {
                    foreach (var drop in uiCompletions) drop();
                    uiCompletions.Clear();
                }
                CancelBackup(); backupOwnerClosed.TrySetResult(true);
                CancelPortable(); if (portableForm != null && !portableForm.IsDisposed) portableForm.Dispose();
                if (backupWorker == null) {
                    if (startupBackupCompletionSource != null) startupBackupCompletionSource.TrySetResult(false);
                    if (backupCancellation != null) { backupCancellation.Dispose(); backupCancellation = null; }
                }
                if (backupForm != null && !backupForm.IsDisposed) backupForm.Dispose();
                if (updateForm != null && !updateForm.IsDisposed) updateForm.CancelAndClose();
                pendingRoutes.Clear(); routeLifetime.Cancel(); routeLifetime.Dispose();
                health.Changed -= HealthChanged;
                if (ownsHealth) health.Dispose();
                statusTimer.Stop(); statusTimer.Dispose();
                if (startupShowTimer != null)
                {
                    startupShowTimer.Stop();
                    startupShowTimer.Dispose();
                    startupShowTimer = null;
                }
                automationTimer.Stop(); automationTimer.Dispose();
                activationDispatcher.Dispose();
                if (mainWindow != null) mainWindow.Dispose();
                // Prepared shutdown already settled settings and stopped the listener.
                // Forced teardown cannot start another mutation after losing its owner.
                // Active workers retain the listener; journals remain for explicit retry.
                if (!shutdownPrepared) SafeLog.Info("Application owner closed without confirmed proxy cleanup; ownership journals retained.");
                appConsumers.Dispose();
                // The application owns this menu and its system-theme subscription.
                if (tray.ContextMenuStrip != null) tray.ContextMenuStrip.Dispose();
                tray.Dispose();
                icon.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}

using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using CancellationTokenSource = System.Threading.CancellationTokenSource;
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
        internal int PendingRouteCount { get { return pendingRoutes.Count; } }

        public UpdateAwareTrayApplicationContext(SettingsService settingsService, ProxyService proxyService, CliProxyBridgeService cliProxyService, HomeVpnService homeVpnService, ClipboardService clipboardService, bool showStatusOnStartup, ConnectionHealthMonitor health = null, Func<WindowsProxyRestoreResult> windowsRestore = null, Func<UpdateCheckForm> updateFormFactory = null)
        {
            settings = settingsService;
            createUpdateForm = updateFormFactory ?? (() => new UpdateCheckForm());
            restoreWindows = windowsRestore ?? (() => SystemProxyService.RestoreOwned());
            proxy = proxyService;
            cliProxy = cliProxyService;
            appConsumers = new AppProxyConsumers(cliProxy, settings);
            homeVpn = homeVpnService;
            clipboard = clipboardService;
            ownsHealth = health == null;
            this.health = health ?? new ConnectionHealthMonitor(() => settings.Current);
            icon = BrandIcon.Create();
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
        private readonly Control activationDispatcher = new Control();

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
            Item(backups.DropDownItems, AppCommand.CreateBackup);
            Item(backups.DropDownItems, AppCommand.RestoreBackup);
            Item(backups.DropDownItems, AppCommand.OpenBackups);
            Item(backups.DropDownItems, AppCommand.CleanupBackups); menu.Items.Add(backups);
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
            mainWindow = new MainWindow(settings, proxy, homeVpn, ExecuteCommand, cliProxy, automation, health, appConsumers, GetCommandState);
            RefreshPendingRoutes();
            mainWindow.FormClosing += DashboardClosing;
            mainWindow.FormClosed += delegate { mainWindow = null; };
            mainWindow.Show(); UpdateTooltip();
        }
        internal void RequestShowStatus()
        {
            if (activationDispatcher.IsDisposed) return;
            if (activationDispatcher.InvokeRequired)
            {
                try { activationDispatcher.BeginInvoke(new Action(ShowStatus)); }
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
            bool reconnectAfterSave = false;
            using (var form = new SshProfilesSettingsForm(settings, section))
            {
                form.ManualActionRequested += ExecuteCommand;
                form.CommandState = GetCommandState;
                form.ProxyEndpointText = cliProxy.ProxyUrl;
                form.CurrentProxyEndpoint = delegate { return cliProxy.ProxyUrl; };
                form.SaveRequested = delegate(AppSettings proposed, bool pickFree) {
                    bool changed = proposed.SocksHost != settings.Current.SocksHost || proposed.SocksPort != settings.Current.SocksPort || SshConnection.Signature(proposed) != SshConnection.Signature(settings.Current);
                    var oldPort = settings.Current.HttpProxyPort;
                    SettingsSaveError error;
                    if (!cliProxy.ReconfigureDetailed(proposed, pickFree, out error)) return error;
                    if (changed) {
                        reconnectAfterSave = proxy.CurrentPid.HasValue || proxy.IsConnecting;
                        pendingRoutes.Clear(); ObserveStop(proxy.StopTunnelAsync()); RefreshPendingRoutes(); health.Invalidate();
                    }
                    NotifyPortChange(oldPort);
                    return null;
                };
                CommandStateChanged += form.RefreshCommandAvailability;
                try {
                    if (form.ShowDialog(mainWindow) == DialogResult.OK)
                    {
                        homeVpn.AutoRestart = settings.Current.AutoRestartSocks;
                        automation.Update(before, settings.Current);
                        if (reconnectAfterSave || (!before.AutoStartSocks && settings.Current.AutoStartSocks)) proxy.StartTunnelAsync(System.Threading.CancellationToken.None);
                    }
                } finally { CommandStateChanged -= form.RefreshCommandAvailability; }
            }
            UpdateTooltip();
        }
        internal void StartAutomation()
        {
            homeVpn.AutoRestart = settings.Current.AutoRestartSocks;
            automation.Update(null, settings.Current);
            automationTimer.Tick += delegate
            {
                bool ready = health.Current.SocksReady;
                foreach (ProxyFeature feature in Enum.GetValues(typeof(ProxyFeature)))
                {
                    Exception error;
                    var result = automation.TryApply(feature, ready, EnableFeature, out error);
                    if (error == null) continue;
                    SafeLog.Error("Automatic proxy setup failed: " + feature + ".", error);
                    if (automation.FailureCount(feature) == 1 || result == AutomationResult.Paused)
                        tray.ShowBalloonTip(6000, "Автонастройка " + AutomationPlan.FeatureName(feature), error.Message +
                            (result == AutomationResult.Paused ? "\nПовторы остановлены. Проверьте настройки и нажмите «Включить»." : "\nProGo повторит попытку автоматически."), ToolTipIcon.Warning);
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
                    case AppCommand.CheckRoute: using (var form = new StatusForm(settings, proxy, command.Command == AppCommand.CheckRoute, null, null, health)) form.ShowDialog(mainWindow); break;
                    case AppCommand.StopCli: DisableCli(); break;
                    case AppCommand.DisableWindows: CancelPendingRoute(AppCommand.EnableWindows); automation.Cancel(ProxyFeature.Windows); RestoreWindowsProxy(); appConsumers.Observe(); appConsumers.ReleaseIfUnused(); break;
                    case AppCommand.RemoveCodexShortcut: CodexProxyService.Disable(); appConsumers.Observe(); appConsumers.ReleaseIfUnused(); break;
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
            DisconnectApps(); ObserveStop(proxy.StopTunnelAsync());
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
            try { ready = await proxy.StartTunnelAsync(routeLifetime.Token).ConfigureAwait(false); }
            catch (OperationCanceledException) { cancelled = true; }
            catch (Exception ex) { failure = ex; }
            if (closing) return;
            try {
                // Modal dialogs may replace a captured WinForms synchronization context.
                // Always finish against this application's persistent UI dispatcher.
                activationDispatcher.BeginInvoke(new Action(delegate {
                    long active;
                    if (closing || !pendingRoutes.TryGetValue(action, out active) || active != request) return;
                    try {
                        if (cancelled) return;
                        if (failure != null) throw failure;
                        if (!ready) throw new InvalidOperationException(proxy.StartupError ?? "Прокси не готов. Проверьте сервер и SSH-ключ в «Подключениях».");
                        switch (action) {
                            case AppCommand.Connect:
                            case AppCommand.Reconnect: break;
                            case AppCommand.StartCli: EnableFeature(ProxyFeature.Cli); automation.Cancel(ProxyFeature.Cli); CliReadyNotice(); break;
                            case AppCommand.EnableWindows: EnableFeature(ProxyFeature.Windows); automation.Cancel(ProxyFeature.Windows); break;
                            case AppCommand.CreateCodexShortcut: EnsureBridge(); CodexProxyService.Enable(cliProxy.Port); break;
                            case AppCommand.OpenCodex: EnsureBridge(); appConsumers.TrackWindow(CodexProxyService.Open(cliProxy.Port)); break;
                            case AppCommand.OpenTerminal: EnsureBridge(); string error; Process window; if (!CliProxyEnvironmentService.OpenPowerShellWithEnvironment(cliProxy.Port, out error, out window)) throw new InvalidOperationException(error); appConsumers.TrackWindow(window); break;
                            default: throw new InvalidOperationException("Команда подключения ProGo не поддерживается.");
                        }
                        health.Invalidate();
                    }
                    catch (Exception ex) {
                        SafeLog.Error("User connection action failed: " + AppCommands.Get(action).LegacyId, ex);
                        MessageBox.Show(mainWindow, ex.Message, "Подключение ProGo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    finally {
                        if (pendingRoutes.TryGetValue(action, out active) && active == request) pendingRoutes.Remove(action);
                        appConsumers.ReleaseIfUnused();
                        if (!closing) RefreshPendingRoutes();
                    }
                }));
            }
            catch (InvalidOperationException ex) { if (!closing) SafeLog.Error("Connection result could not reach the application UI.", ex); }
        }
        private AppCommandState GetCommandState()
        { return new AppCommandState(pendingRoutes.Keys, proxy.IsConnecting || proxy.IsStopping, closing || shutdownPrepared || shutdownPreparing); }
        private void RefreshPendingRoutes()
        {
            var state = GetCommandState();
            foreach (var item in commandItems) item.Value.Enabled = AppCommands.CanExecute(item.Key, state);
            if (mainWindow != null && !mainWindow.IsDisposed) mainWindow.RefreshConnectionState();
            if (CommandStateChanged != null) CommandStateChanged();
        }
        private void CancelPendingRoute(AppCommand action)
        {
            pendingRoutes.Remove(action);
            if (pendingRoutes.Count == 0 && (proxy.IsConnecting || proxy.IsStopping)) ObserveStop(proxy.StopTunnelAsync());
            RefreshPendingRoutes();
        }
        private void EnsureBridge()
        {
            var oldPort = settings.Current.HttpProxyPort;
            string message; if (!cliProxy.Start(out message)) throw new InvalidOperationException(message);
            appConsumers.Observe();
            NotifyPortChange(oldPort);
        }
        private void NotifyPortChange(int previous)
        {
            if (previous == settings.Current.HttpProxyPort) return;
            tray.ShowBalloonTip(7000, "Новый порт приложений", "Адрес: " + cliProxy.ProxyUrl + ". Настройки прокси ProGo обновлены. Перезапустите открытые терминалы и Codex.", ToolTipIcon.Info);
        }
        private void EnableFeature(ProxyFeature feature)
        {
            EnsureBridge();
            try {
                if (feature == ProxyFeature.Cli) CliProxyEnvironmentService.ApplyUserEnvironment(cliProxy.Port);
                else { string message; if (!SystemProxyService.Apply(settings.Current, out message)) throw new InvalidOperationException(message); }
            } finally { appConsumers.ReleaseIfUnused(); }
        }
        private void DisableCli()
        {
            CancelPendingRoute(AppCommand.StartCli);
            automation.Cancel(ProxyFeature.Cli);
            RestoreCliEnvironment();
            appConsumers.Observe(); appConsumers.ReleaseIfUnused();
            var remaining = appConsumers.Summary;
            tray.ShowBalloonTip(7000, "Прокси для новых терминалов выключен",
                "Полностью перезапустите уже открытые терминалы и Codex." +
                (cliProxy.IsRunning ? " Общий прокси продолжает работать для: " + remaining + ". Для полной остановки нажмите «Отключить прокси на ПК»." : " Порт приложений освобождён."), ToolTipIcon.Info);
        }
        private void CliReadyNotice()
        {
            tray.ShowBalloonTip(6000, "CLI-прокси включён", "Codex можно запускать обычным способом. Полностью перезапустите уже открытый терминал или приложение с Codex. Отдельный ярлык не нужен.", ToolTipIcon.Info);
        }
        private void RestoreCliEnvironment()
        {
            try {
                CliProxyEnvironmentService.ClearUserEnvironmentIfOwned();
                appConsumers.CliCleanupPending = false;
            } catch { appConsumers.CliCleanupPending = true; throw; }
        }
        private void RestoreWindowsProxy()
        {
            try {
                var result = restoreWindows();
                if (!result.Completed) throw new InvalidOperationException(result.Message);
                appConsumers.WindowsCleanupPending = false;
                if (result.PreservedExternal) tray.ShowBalloonTip(5000, "Настройки Windows сохранены", result.Message, ToolTipIcon.Info);
            } catch { appConsumers.WindowsCleanupPending = true; throw; }
        }
        private void DisconnectApps()
        {
            var errors = new List<string>();
            try { RestoreCliEnvironment(); }
            catch (Exception ex) { SafeLog.Error("Environment restore failed.", ex); errors.Add("Не удалось восстановить настройки терминалов. Повторите выключение CLI."); }
            try { RestoreWindowsProxy(); }
            catch (Exception ex) { SafeLog.Error("Windows proxy cleanup incomplete.", ex); errors.Add(ex.Message); }
            // Keep the local service alive while owned settings may still reference it.
            if (errors.Count != 0) throw new InvalidOperationException(String.Join("\n\n", errors.ToArray()) + "\n\nПрокси на ПК продолжает работать, чтобы не оборвать доступ. Повторите отключение после устранения ошибки.");
            cliProxy.Stop(); appConsumers.ForgetWindows();
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
            try {
                DisconnectApps();
                if (!await proxy.StopTunnelAsync()) throw new InvalidOperationException("Не удалось остановить SSH-процесс. ProGo остаётся запущенным; повторите отключение.");
                if (closing) return false;
                shutdownPrepared = true;
                if (updateForm != null && !updateForm.IsDisposed) updateForm.CancelAndClose();
                return true;
            }
            catch (Exception ex) {
                SafeLog.Error("Shutdown refused: owned proxy cleanup incomplete.", ex);
                if (!closing) {
                    if (dialog) MessageBox.Show(ex.Message, "ProGo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    else tray.ShowBalloonTip(10000, "ProGo остаётся запущенным", ex.Message, ToolTipIcon.Warning);
                }
                return false;
            }
            finally { shutdownPreparing = false; if (!closing) RefreshPendingRoutes(); }
        }
        // Pipe worker may await the UI-owned cleanup; no synchronous wait runs on UI.
        internal Task<bool> RequestShutdownAsync()
        {
            if (activationDispatcher.IsDisposed || closing) return Task.FromResult(false);
            if (activationDispatcher.InvokeRequired)
                return (Task<bool>)activationDispatcher.Invoke(new Func<Task<bool>>(() => PrepareShutdownAsync(false)));
            return PrepareShutdownAsync(false);
        }
        internal bool RequestShutdown()
        {
            var result = RequestShutdownAsync();
            if (activationDispatcher.InvokeRequired) return result.GetAwaiter().GetResult();
            return result.IsCompleted && result.GetAwaiter().GetResult();
        }
        private async void ObserveStop(Task<bool> work)
        {
            try { if (!await work && !closing) tray.ShowBalloonTip(7000, "Подключение ещё не остановлено", "Не удалось остановить SSH-процесс. Повторите отключение; ProGo сохраняет управление этим процессом.", ToolTipIcon.Warning); }
            catch (Exception ex) { if (!closing) SafeLog.Error("SSH asynchronous stop failed.", ex); }
            finally { if (!closing) RefreshPendingRoutes(); }
        }
        internal void CompleteShutdown()
        {
            if (activationDispatcher.IsDisposed) return;
            activationDispatcher.BeginInvoke(new Action(delegate {
                if (!shutdownPrepared || closing) return;
                closing = true; homeVpn.Stop(); tray.Visible = false; ExitThread();
            }));
        }
        internal void CancelShutdown()
        {
            if (!activationDispatcher.IsDisposed)
                activationDispatcher.BeginInvoke(new Action(delegate { if (!closing) { shutdownPrepared = false; RefreshPendingRoutes(); } }));
        }
        private async Task<bool> BeginMaintenance(Func<bool> launch)
        {
            if (!await PrepareShutdownAsync(true)) return false;
            bool handedOff = false;
            try { handedOff = launch(); if (handedOff) CompleteShutdown(); return handedOff; }
            finally { if (!handedOff) { shutdownPrepared = false; RefreshPendingRoutes(); } }
        }
        private async void ExitProGo()
        {
            if (await PrepareShutdownAsync(true)) CompleteShutdown();
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

        private void CreateBackup()
        {
            try
            {
                var dir = BackupService.CreateBackup("manual");
                MessageBox.Show("Резервная копия создана:\n" + dir, "Резервная копия ProGo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                SafeLog.Error("Manual backup failed.", ex);
                MessageBox.Show("Не удалось создать резервную копию. Подробности записаны в журнал.", "Резервная копия ProGo", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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

            try
            {
                using (var options = new RestoreOptionsForm(backupDir))
                {
                    if (options.ShowDialog() != DialogResult.OK) return;
                    using (var prepared = options.TakePreparedCopy())
                    {
                        if (closing) return;
                        var names = BackupIntegrity.RestoreNames(prepared.Path, options.Scope, options.DataConfirmed);
                        var result = MessageBox.Show(
                            "Копия проверена. Версия в копии: " + File.ReadAllText(Path.Combine(prepared.Path, "VERSION")).Trim() +
                            "\n\nБудет восстановлено:\n" + String.Join("\n", names) +
                            (options.Scope == "Program" ? "\n\nТекущие настройки и хранилище сохранятся." : "\n\nПеречисленные пользовательские данные будут заменены данными из копии.") +
                            "\n\nProGo закроется и запустится снова. Начать восстановление?",
                            "Подтвердите восстановление", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                        if (result != DialogResult.Yes) return;
                        if (!await BeginMaintenance(() => BackupService.StartRestore(prepared.Path, options.Scope, options.DataConfirmed))) return;
                        SafeLog.Info("Restore requested by user. scope=" + options.Scope + ".");
                    }
                }
            }
            catch (Exception ex)
            {
                SafeLog.Error("Restore preparation or cleanup failed.", ex);
                if (!closing) MessageBox.Show("Восстановление не запущено: " + ex.Message, "Восстановление ProGo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            if (!await BeginMaintenance(UpdateLauncher.StartUpdater)) return;
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
                closing = true;
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
                try { DisconnectApps(); }
                catch (Exception ex) {
                    SafeLog.Error("Application teardown left pending proxy cleanup.", ex);
                    MessageBox.Show(ex.Message, "Очистка ProGo не завершена", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
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

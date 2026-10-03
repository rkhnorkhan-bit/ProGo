using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace ProGo
{
    internal sealed class UpdateAwareTrayApplicationContext : ApplicationContext
    {
        private readonly SettingsService settings;
        private readonly ProxyService proxy;
        private readonly CliProxyBridgeService cliProxy;
        private readonly HomeVpnService homeVpn;
        private readonly ClipboardService clipboard;
        private readonly NotifyIcon tray;
        private readonly System.Drawing.Icon icon;
        private readonly ConnectionHealthMonitor health;
        private readonly bool ownsHealth;
        private readonly Timer statusTimer = new Timer { Interval = 1000 };
        private Timer startupShowTimer;

        public UpdateAwareTrayApplicationContext(SettingsService settingsService, ProxyService proxyService, CliProxyBridgeService cliProxyService, HomeVpnService homeVpnService, ClipboardService clipboardService, bool showStatusOnStartup, ConnectionHealthMonitor health = null)
        {
            settings = settingsService;
            proxy = proxyService;
            cliProxy = cliProxyService;
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
            tray.DoubleClick += delegate { ShowStatus(); };
            UpdateTooltip();
            this.health.Changed += HealthChanged;
            statusTimer.Tick += delegate { UpdateTooltip(); };
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
            menu.Items.Add("Открыть ProGo", null, delegate { ShowStatus(); });
            menu.Items.Add(new ToolStripSeparator());
            var connection = new ToolStripMenuItem("Подключение");
            Item(connection, "Подключиться к серверу", "connect");
            Item(connection, "Отключить прокси на ПК", "stop");
            Item(connection, "Остановить все подключения", "stop-all");
            Item(connection, "Переподключиться", "restart");
            Item(connection, "Проверить маршрут", "route-check");
            Item(connection, "Открыть диагностику и скорость", "diagnostics");
            menu.Items.Add(connection);
            var apps = new ToolStripMenuItem("Прокси для приложений");
            Item(apps, "Windows — включить", "windows-on"); Item(apps, "Windows — выключить", "windows-off");
            apps.DropDownItems.Add(new ToolStripSeparator());
            Item(apps, "Запустить CLI (терминалы и Codex)", "cli-start"); Item(apps, "Выключить прокси для терминалов и Codex", "cli-off");
            Item(apps, "Открыть терминал с прокси", "terminal-open");
            apps.DropDownItems.Add(new ToolStripSeparator());
            var extra = new ToolStripMenuItem("Дополнительно: ярлык Codex");
            Item(extra, "Создать отдельный ярлык", "codex-shortcut-on"); Item(extra, "Удалить отдельный ярлык", "codex-shortcut-off"); apps.DropDownItems.Add(extra);
            Item(apps, "Открыть Codex через ProGo", "codex-open"); menu.Items.Add(apps);
            menu.Items.Add("iPhone через домашний ПК…", null, delegate { Execute("iphone"); });
            menu.Items.Add("Остановить VPN для телефона", null, delegate { Execute("phone-stop"); });
            menu.Items.Add("Хранилище паролей и ключей…", null, delegate { Execute("vault"); });
            menu.Items.Add("Настройки и автоматика…", null, delegate { Execute("settings"); });
            menu.Items.Add(new ToolStripSeparator());
            var backups = new ToolStripMenuItem("Резервные копии");
            backups.DropDownItems.Add("Создать копию сейчас", null, delegate { CreateBackup(); });
            backups.DropDownItems.Add("Восстановить из копии…", null, delegate { StartRestore(); });
            backups.DropDownItems.Add("Открыть папку с копиями", null, delegate { OpenBackups(); });
            backups.DropDownItems.Add("Удалить старые автоматические копии…", null, delegate { CleanupBackups(); }); menu.Items.Add(backups);
            menu.Items.Add(BuildLogsMenu());
            menu.Items.Add("Проверить обновления…", null, delegate { StartUpdate(); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Завершить работу ProGo", null, delegate { ExitProGo(); });
            UiTheme.Menu(menu); return menu;
        }
        private void Item(ToolStripMenuItem menu, string text, string action)
        {
            menu.DropDownItems.Add(text, null, delegate { Execute(action); });
        }
        private ToolStripMenuItem BuildLogsMenu()
        {
            var logs = new ToolStripMenuItem("Помощь и журналы");
            logs.DropDownItems.Add("Антивирус и обновления…", null, delegate { ShowHelp(); });
            logs.DropDownItems.Add("Журнал приложения", null, delegate { OpenLogFile(AppPaths.LogPath, "журнал приложения"); });
            logs.DropDownItems.Add("Журнал обновления", null, delegate { OpenLogFile(Path.Combine(AppPaths.Root, "update.log"), "журнал обновления"); });
            logs.DropDownItems.Add("Папка приложения", null, delegate { OpenProGoFolder(); }); return logs;
        }
        private void ShowStatus()
        {
            if (mainWindow != null && !mainWindow.IsDisposed) { mainWindow.Show(); mainWindow.WindowState = FormWindowState.Normal; mainWindow.Activate(); return; }
            mainWindow = new MainWindow(settings, proxy, homeVpn, Execute, cliProxy, automation, health);
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
            using (var form = new SshProfilesSettingsForm(settings, section))
            {
                form.ManualActionRequested += Execute;
                form.ProxyEndpointText = cliProxy.ProxyUrl;
                form.SaveRequested = delegate(AppSettings proposed, bool pickFree) {
                    var oldPort = settings.Current.HttpProxyPort;
                    string message;
                    if (!cliProxy.Reconfigure(proposed, pickFree, out message)) return message;
                    NotifyPortChange(oldPort);
                    return null;
                };
                if (form.ShowDialog(mainWindow) == DialogResult.OK)
                {
                    homeVpn.AutoRestart = settings.Current.AutoRestartSocks;
                    automation.Update(before, settings.Current);
                    if (!before.AutoStartSocks && settings.Current.AutoStartSocks) proxy.StartTunnel(false);
                    if (before.SocksPort != settings.Current.SocksPort || before.SocksHost != settings.Current.SocksHost || before.SshProfile != settings.Current.SshProfile)
                    {
                        if (proxy.CurrentPid.HasValue) proxy.RestartTunnel();
                    }
                }
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
            string navigation = action == "windows-settings" ? "settings" : action == "route-check" ? "diagnostics" : action;
            bool isPage = navigation == "settings" || navigation == "connections" || navigation == "vault" || navigation == "iphone" || navigation == "diagnostics";
            if (isPage && mainWindow != null) mainWindow.SetNavigation(navigation);
            try
            {
                switch (action)
                {
                    case "connect": health.Invalidate(); if (proxy.IsListening()) proxy.RestartTunnel(); else proxy.StartTunnel(true); health.Invalidate(); break;
                    case "restart": health.Invalidate(); proxy.RestartTunnel(); health.Invalidate(); break;
                    case "stop":
                        StopDesktop(); break;
                    case "phone-stop": homeVpn.Stop(); break;
                    case "stop-all": try { StopDesktop(); } finally { homeVpn.Stop(); } break;
                    case "settings": ShowSettings(); break;
                    case "connections": ShowSettings(SettingsSection.Connections); break;
                    case "windows-settings": ShowSettings(SettingsSection.Windows); break;
                    case "vault": ShowVault(); break;
                    case "iphone": using (var form = new HomeVpnWizardForm(homeVpn)) form.ShowDialog(mainWindow); break;
                    case "diagnostics":
                    case "route-check": using (var form = new StatusForm(settings, proxy, action == "route-check", null, null, health)) form.ShowDialog(mainWindow); break;
                    case "cli-start":
                    case "terminal-on":
                    case "codex-on": RequireRoute(); EnableFeature(ProxyFeature.Cli); automation.Cancel(ProxyFeature.Cli); CliReadyNotice(); break;
                    case "cli-off":
                    case "terminal-off":
                    case "codex-off": DisableCli(); break;
                    case "windows-on": RequireRoute(); EnableFeature(ProxyFeature.Windows); automation.Cancel(ProxyFeature.Windows); break;
                    case "windows-off": automation.Cancel(ProxyFeature.Windows); string m; if (!SystemProxyService.Restore(out m)) throw new InvalidOperationException(m); break;
                    case "codex-shortcut-on": RequireRoute(); EnsureBridge(); CodexProxyService.Enable(cliProxy.Port); break;
                    case "codex-shortcut-off": CodexProxyService.Disable(); break;
                    case "codex-open": RequireRoute(); EnsureBridge(); CodexProxyService.Open(cliProxy.Port); break;
                    case "terminal-open": RequireRoute(); EnsureBridge(); string error; if (!CliProxyEnvironmentService.OpenPowerShellWithEnvironment(cliProxy.Port, out error)) throw new InvalidOperationException(error); break;
                    case "help": ShowHelp(); break;
                    case "update": StartUpdate(); break;
                }
                UpdateTooltip();
            }
            catch (Exception ex) { SafeLog.Error("User action failed: " + action, ex); MessageBox.Show(ex.Message, "ProGo", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            finally { if (isPage && mainWindow != null && !mainWindow.IsDisposed) mainWindow.SetNavigation("home"); }
        }
        private void StopDesktop()
        {
            foreach (ProxyFeature feature in Enum.GetValues(typeof(ProxyFeature))) automation.Cancel(feature);
            DisconnectApps(); proxy.StopTunnel();
            health.Invalidate();
        }
        private void RequireRoute()
        {
            if (proxy.IsListening()) return;
            proxy.StartTunnel(true);
            if (!proxy.IsListening()) throw new InvalidOperationException("Подключение к серверу ещё не готово. Завершите вход в окне SSH и повторите действие.");
        }
        private void EnsureBridge()
        {
            var oldPort = settings.Current.HttpProxyPort;
            string message; if (!cliProxy.Start(out message)) throw new InvalidOperationException(message);
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
            if (feature == ProxyFeature.Cli) CliProxyEnvironmentService.ApplyUserEnvironment(cliProxy.Port);
            else { string message; if (!SystemProxyService.Apply(settings.Current, out message)) throw new InvalidOperationException(message); }
        }
        private void DisableCli()
        {
            automation.Cancel(ProxyFeature.Cli);
            CliProxyEnvironmentService.ClearUserEnvironmentIfOwned();
        }
        private void CliReadyNotice()
        {
            tray.ShowBalloonTip(6000, "CLI-прокси включён", "Codex можно запускать обычным способом. Полностью перезапустите уже открытый терминал или приложение с Codex. Отдельный ярлык не нужен.", ToolTipIcon.Info);
        }
        private void DisconnectApps()
        {
            try { CliProxyEnvironmentService.ClearUserEnvironmentIfOwned(); }
            catch (Exception ex) { SafeLog.Error("Environment restore failed.", ex); }
            if (SystemProxyService.IsOwned) { string message; SystemProxyService.Restore(out message); }
            cliProxy.Stop();
        }
        private void ShowHelp()
        {
            using (var form = new HelpForm(delegate { OpenLogFile(Path.Combine(AppPaths.Root, "update.log"), "журнал обновления"); })) form.ShowDialog();
        }

        private void ShowVault()
        {
            VaultSession session;
            if (!PinForm.OpenSession(out session)) return;
            using (var form = new VaultForm(session, clipboard, settings)) form.ShowDialog();
        }

        private void ExitProGo()
        {
            homeVpn.Stop();
            DisconnectApps();
            tray.Visible = false;
            ExitThread();
        }

        private void OpenLogFile(string path, string title)
        {
            try
            {
                AppPaths.EnsureDirectories();
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
            var result = MessageBox.Show(
                "ProGo удалит старые автоматические резервные копии.\n\nБудут сохранены ручные копии, исходная копия и последняя копия перед обновлением.\n\nПродолжить?",
                "Очистка резервных копий ProGo",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes) return;

            try
            {
                var cleanup = BackupService.CleanupOldBackups(true);
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

        private void StartRestore()
        {
            var backups = BackupService.ListBackups();
            if (backups.Count == 0)
            {
                MessageBox.Show("Резервные копии не найдены. Сначала создайте резервную копию или дождитесь следующего обновления версии.", "Восстановление ProGo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string backupDir;
            if (!BackupPickerForm.TryPick(backups, out backupDir)) return;

            var result = MessageBox.Show(
                "ProGo будет закрыт, восстановит выбранную резервную копию и запустится заново.\n\nВыбранная копия:\n" + backupDir + "\n\nПродолжить?",
                "Восстановление ProGo",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes) return;

            if (!BackupService.StartRestore(backupDir)) return;

            SafeLog.Info("Restore requested by user: " + backupDir + ".");
            tray.Visible = false;
            ExitThread();
        }

        private bool checkingUpdate;
        private async void StartUpdate()
        {
            if (checkingUpdate) return;
            checkingUpdate = true;
            UpdateCheckResult check;
            try { check = await System.Threading.Tasks.Task.Run(() => UpdateLauncher.CheckForUpdate()); }
            finally { checkingUpdate = false; }


            if (check.Availability == UpdateAvailability.Error)
            {
                MessageBox.Show(
                    (check.ErrorMessage ?? "Не удалось проверить наличие обновлений.") +
                    "\n\nТекущая версия: " + (check.LocalVersion ?? "неизвестна") +
                    "\n\nПроверьте подключение к GitHub и повторите попытку.",
                    "Проверка обновлений ProGo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (check.Availability == UpdateAvailability.UpToDate)
            {
                MessageBox.Show(
                    "У вас установлена актуальная версия ProGo.\n\nВерсия: " + check.LocalVersion,
                    "Обновление ProGo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show(
                "Доступна новая версия ProGo — " + check.RemoteVersion + ".\n\n" +
                "Текущая версия: " + check.LocalVersion + "\n\n" +
                "Установить обновление сейчас?",
                "Доступно обновление ProGo",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes) return;

            if (!UpdateLauncher.StartUpdater()) return;

            SafeLog.Info("Update requested by user. local=" + check.LocalVersion + "; remote=" + check.RemoteVersion + ".");
            tray.Visible = false;
            ExitThread();
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
                DisconnectApps();
                tray.Dispose();
                icon.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}

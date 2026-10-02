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
        private Timer startupShowTimer;

        public UpdateAwareTrayApplicationContext(SettingsService settingsService, ProxyService proxyService, CliProxyBridgeService cliProxyService, HomeVpnService homeVpnService, ClipboardService clipboardService, bool showStatusOnStartup)
        {
            settings = settingsService;
            proxy = proxyService;
            cliProxy = cliProxyService;
            homeVpn = homeVpnService;
            clipboard = clipboardService;
            icon = BrandIcon.Create();

            tray = new NotifyIcon
            {
                Icon = icon,
                Text = AppConstants.ProductName,
                Visible = true,
                ContextMenuStrip = BuildMenu()
            };
            tray.DoubleClick += delegate { ShowStatus(); };
            UpdateTooltip();

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

        private ContextMenuStrip BuildMenu()
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("Открыть ProGo", null, delegate { ShowStatus(); });
            menu.Items.Add(new ToolStripSeparator());
            var connection = new ToolStripMenuItem("Подключение");
            Item(connection, "Подключиться к серверу", "connect");
            Item(connection, "Отключиться", "stop");
            Item(connection, "Переподключиться", "restart");
            Item(connection, "Проверить маршрут и скорость", "diagnostics");
            menu.Items.Add(connection);
            var apps = new ToolStripMenuItem("Прокси для приложений");
            Item(apps, "Windows — включить", "windows-on"); Item(apps, "Windows — выключить", "windows-off");
            apps.DropDownItems.Add(new ToolStripSeparator());
            Item(apps, "Командная строка — включить", "terminal-on"); Item(apps, "Командная строка — выключить", "terminal-off");
            Item(apps, "Открыть терминал с прокси", "terminal-open");
            apps.DropDownItems.Add(new ToolStripSeparator());
            Item(apps, "Codex — настроить ярлык", "codex-on"); Item(apps, "Codex — убрать ярлык", "codex-off");
            Item(apps, "Открыть Codex через ProGo", "codex-open"); menu.Items.Add(apps);
            menu.Items.Add("iPhone через домашний ПК…", null, delegate { Execute("iphone"); });
            menu.Items.Add("Хранилище паролей и ключей…", null, delegate { ShowVault(); });
            menu.Items.Add("Настройки и автоматика…", null, delegate { ShowSettings(); });
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
            mainWindow = new MainWindow(settings, proxy, homeVpn, Execute, cliProxy);
            mainWindow.FormClosed += delegate { mainWindow = null; };
            mainWindow.Show(); UpdateTooltip();
        }
        private void ShowSettings()
        {
            var before = settings.Current;
            using (var form = new SshProfilesSettingsForm(settings))
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
                if (form.ShowDialog() == DialogResult.OK)
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
                bool ready = proxy.IsListening();
                foreach (ProxyFeature feature in Enum.GetValues(typeof(ProxyFeature)))
                    if (automation.Take(feature, ready))
                        try { EnableFeature(feature); }
                        catch (Exception ex) { SafeLog.Error("Automatic proxy setup failed.", ex); tray.ShowBalloonTip(5000, "ProGo", ex.Message, ToolTipIcon.Warning); }
            };
            automationTimer.Start();
        }
        private void Execute(string action)
        {
            try
            {
                switch (action)
                {
                    case "connect": if (proxy.IsListening()) proxy.RestartTunnel(); else proxy.StartTunnel(true); break;
                    case "restart": proxy.RestartTunnel(); break;
                    case "stop":
                        foreach (ProxyFeature feature in Enum.GetValues(typeof(ProxyFeature))) automation.Cancel(feature);
                        DisconnectApps(); proxy.StopTunnel(); break;
                    case "settings": ShowSettings(); break;
                    case "vault": ShowVault(); break;
                    case "iphone": using (var form = new HomeVpnWizardForm(homeVpn)) form.ShowDialog(); break;
                    case "diagnostics": using (var form = new StatusForm(settings, proxy)) form.ShowDialog(); break;
                    case "terminal-on": RequireRoute(); EnableFeature(ProxyFeature.Terminal); break;
                    case "terminal-off": automation.Cancel(ProxyFeature.Terminal); CliProxyEnvironmentService.ClearUserEnvironmentIfOwned(); break;
                    case "windows-on": RequireRoute(); EnableFeature(ProxyFeature.Windows); break;
                    case "windows-off": automation.Cancel(ProxyFeature.Windows); string m; if (!SystemProxyService.Restore(out m)) throw new InvalidOperationException(m); break;
                    case "codex-on": RequireRoute(); EnableFeature(ProxyFeature.Codex); break;
                    case "codex-off": automation.Cancel(ProxyFeature.Codex); CodexProxyService.Disable(); break;
                    case "codex-open": RequireRoute(); EnsureBridge(); CodexProxyService.Open(cliProxy.Port); break;
                    case "terminal-open": RequireRoute(); EnsureBridge(); string error; if (!CliProxyEnvironmentService.OpenPowerShellWithEnvironment(cliProxy.Port, out error)) throw new InvalidOperationException(error); break;
                    case "help": ShowHelp(); break;
                    case "update": StartUpdate(); break;
                }
                UpdateTooltip();
            }
            catch (Exception ex) { SafeLog.Error("User action failed: " + action, ex); MessageBox.Show(ex.Message, "ProGo", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
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
            if (feature == ProxyFeature.Terminal) CliProxyEnvironmentService.ApplyUserEnvironment(cliProxy.Port);
            else if (feature == ProxyFeature.Codex) CodexProxyService.Enable(cliProxy.Port);
            else { string message; if (!SystemProxyService.Apply(settings.Current, out message)) throw new InvalidOperationException(message); }
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
            tray.Text = AppConstants.ProductName + " — туннель: " + (proxy.IsListening() ? "работает" : "остановлен");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (startupShowTimer != null)
                {
                    startupShowTimer.Stop();
                    startupShowTimer.Dispose();
                    startupShowTimer = null;
                }
                automationTimer.Stop(); automationTimer.Dispose();
                if (mainWindow != null) mainWindow.Dispose();
                DisconnectApps();
                tray.Dispose();
                icon.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}

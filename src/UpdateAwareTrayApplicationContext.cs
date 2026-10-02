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
        private readonly Ikev2RelayService ikev2Relay;
        private readonly ClipboardService clipboard;
        private readonly NotifyIcon tray;
        private readonly System.Drawing.Icon icon;
        private Timer startupShowTimer;

        public UpdateAwareTrayApplicationContext(SettingsService settingsService, ProxyService proxyService, CliProxyBridgeService cliProxyService, Ikev2RelayService ikev2RelayService, ClipboardService clipboardService, bool showStatusOnStartup)
        {
            settings = settingsService;
            proxy = proxyService;
            cliProxy = cliProxyService;
            ikev2Relay = ikev2RelayService;
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

        private ContextMenuStrip BuildMenu()
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("Состояние", null, delegate { ShowStatus(); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Запустить SOCKS", null, delegate { proxy.StartTunnel(true); UpdateTooltip(); });
            menu.Items.Add("Остановить SOCKS", null, delegate { ikev2Relay.Stop(); proxy.StopTunnel(); UpdateTooltip(); });
            menu.Items.Add("Перезапустить SOCKS", null, delegate { ikev2Relay.Stop(); proxy.RestartTunnel(); UpdateTooltip(); });
            var autoRestart = new ToolStripMenuItem("Автовосстановление SOCKS")
            {
                Checked = settings.Current.AutoRestartSocks
            };
            autoRestart.Click += delegate
            {
                proxy.SetAutoRestart(!settings.Current.AutoRestartSocks);
                autoRestart.Checked = settings.Current.AutoRestartSocks;
            };
            menu.Opening += delegate { autoRestart.Checked = settings.Current.AutoRestartSocks; };
            menu.Items.Add(autoRestart);
            menu.Items.Add("Изменить порт SOCKS...", null, delegate { ChangePort(); });
            menu.Items.Add("Применить proxy environment", null, delegate { ApplyCliProxyEnvironment(); });
            menu.Items.Add("Включить системный прокси Windows", null, delegate { EnableSystemProxy(); });
            menu.Items.Add("Отключить системный прокси Windows", null, delegate { DisableSystemProxy(); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Запустить CLI/Codex proxy", null, delegate { StartCliProxy(true); });
            menu.Items.Add("Остановить CLI/Codex proxy", null, delegate { StopCliProxy(); });
            menu.Items.Add("Применить CLI proxy env", null, delegate { ApplyCliProxyEnvironment(); });
            menu.Items.Add("Открыть PowerShell с CLI proxy", null, delegate { OpenPowerShellWithCliProxy(); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Проверить соединение", null, delegate { MessageBox.Show(RouteTester.Test(settings.Current, proxy), "Проверка соединения"); });
            menu.Items.Add("VPN для iPhone...", null, delegate { using (var form = new Ikev2RelayForm(settings, ikev2Relay)) form.ShowDialog(); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Хранилище секретов", null, delegate { ShowVault(); });
            menu.Items.Add("Настройки", null, delegate { ShowSettings(); });
            menu.Items.Add(BuildLogsMenu());
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Создать резервную копию", null, delegate { CreateBackup(); });
            menu.Items.Add("Откатить из резервной копии...", null, delegate { StartRestore(); });
            menu.Items.Add("Открыть папку резервных копий", null, delegate { OpenBackups(); });
            menu.Items.Add("Удалить старые резервные копии...", null, delegate { CleanupBackups(); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Обновить ProGo", null, delegate { StartUpdate(); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Выход", null, delegate { ExitProGo(); });
            return menu;
        }

        private ToolStripMenuItem BuildLogsMenu()
        {
            var logs = new ToolStripMenuItem("Открыть логи");
            logs.DropDownItems.Add("Журнал приложения: progo.log", null, delegate { OpenLogFile(AppPaths.LogPath, "журнал приложения"); });
            logs.DropDownItems.Add("Журнал обновления: update.log", null, delegate { OpenLogFile(Path.Combine(AppPaths.Root, "update.log"), "журнал обновления"); });
            logs.DropDownItems.Add("Старый журнал обновления: progo-update.log", null, delegate { OpenLogFile(Path.Combine(AppPaths.Root, "progo-update.log"), "старый журнал обновления"); });
            logs.DropDownItems.Add("Открыть папку ProGo", null, delegate { OpenProGoFolder(); });
            return logs;
        }

        private void ShowStatus()
        {
            using (var form = new StatusForm(settings, proxy)) form.ShowDialog();
            UpdateTooltip();
        }

        private void ShowSettings()
        {
            using (var form = new SshProfilesSettingsForm(settings)) form.ShowDialog();
            UpdateTooltip();
        }

        private void ShowVault()
        {
            VaultSession session;
            if (!PinForm.OpenSession(out session)) return;
            using (var form = new VaultForm(session, clipboard, settings)) form.ShowDialog();
        }

        private void ChangePort()
        {
            var systemProxyWasApplied = SystemProxyService.IsApplied(settings.Current);
            int port;
            if (!PortForm.TryGetPort(settings.Current.SocksPort, out port)) return;
            ikev2Relay.Stop();
            var next = settings.Current;
            next.SocksPort = port;
            settings.Save(next);
            proxy.RestartTunnel();
            EnsureCliProxyEnvironment(false);

            if (systemProxyWasApplied)
            {
                string proxyMessage;
                if (!SystemProxyService.Apply(settings.Current, out proxyMessage))
                {
                    MessageBox.Show(proxyMessage ?? "Порт SOCKS изменён, но системный прокси Windows не удалось обновить.", "Системный прокси Windows", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            MessageBox.Show("Порт SOCKS изменён на " + port + ".", "Порт SOCKS");
            UpdateTooltip();
        }

        private void EnableSystemProxy()
        {
            if (!proxy.IsListening())
            {
                var start = MessageBox.Show(
                    "SOCKS-туннель сейчас не слушает порт " + settings.Current.SocksPort + ".\n\nЗапустить SOCKS перед включением системного прокси Windows?",
                    "Системный прокси Windows",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (start == DialogResult.Yes)
                {
                    proxy.StartTunnel(true);
                    UpdateTooltip();
                }
            }

            EnsureCliProxyEnvironment(false);

            string message;
            if (!SystemProxyService.Apply(settings.Current, out message))
            {
                MessageBox.Show(message ?? "Не удалось включить системный прокси Windows.", "Системный прокси Windows", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            MessageBox.Show(
                "Системный прокси Windows включён для текущего пользователя.\n\n" +
                "ProxyServer: socks=" + settings.Current.SocksHost + ":" + settings.Current.SocksPort + "\n\n" +
                "Для браузеров/WinINet откройте новое окно или перезапустите приложение. Для Codex CLI используйте пункт «Запустить CLI/Codex proxy».",
                "Системный прокси Windows",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void DisableSystemProxy()
        {
            string message;
            if (!SystemProxyService.Restore(out message))
            {
                MessageBox.Show(message ?? "Не удалось восстановить системный прокси Windows.", "Системный прокси Windows", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            MessageBox.Show(
                "Предыдущие Windows proxy-настройки текущего пользователя восстановлены.\n\nУже запущенным приложениям может потребоваться перезапуск.",
                "Системный прокси Windows",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private bool StartCliProxy(bool showDialog)
        {
            if (!proxy.IsListening())
            {
                var start = MessageBox.Show(
                    "SOCKS-туннель сейчас не слушает порт " + settings.Current.SocksPort + ".\n\nЗапустить SOCKS перед включением CLI/Codex proxy?",
                    "CLI/Codex proxy",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (start == DialogResult.Yes)
                {
                    proxy.StartTunnel(true);
                    UpdateTooltip();
                }
            }

            if (!proxy.IsListening())
            {
                if (showDialog) MessageBox.Show("CLI/Codex proxy не запущен: сначала нужен рабочий SOCKS-туннель.", "CLI/Codex proxy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            string message;
            if (!cliProxy.Start(out message))
            {
                if (showDialog) MessageBox.Show(message ?? "Не удалось запустить CLI/Codex proxy.", "CLI/Codex proxy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            CliProxyEnvironmentService.ApplyUserEnvironment();
            if (showDialog)
            {
                MessageBox.Show(
                    "CLI/Codex proxy запущен.\n\n" +
                    "HTTP proxy: " + CliProxyEnvironmentService.ProxyUrl + "\n" +
                    "SOCKS backend: " + settings.Current.SocksHost + ":" + settings.Current.SocksPort + "\n\n" +
                    "Для текущей сессии используйте пункт «Открыть PowerShell с CLI proxy» или откройте новое окно PowerShell/cmd после применения env.",
                    "CLI/Codex proxy",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }

            return true;
        }

        private void StopCliProxy()
        {
            cliProxy.Stop();
            CliProxyEnvironmentService.ClearUserEnvironmentIfOwned();
            MessageBox.Show(
                "CLI/Codex proxy остановлен. ProGo-owned user-level proxy env очищен для новых процессов.\n\n" +
                "Уже запущенный Codex/терминал сохраняет старое окружение до перезапуска.",
                "CLI/Codex proxy",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private bool EnsureCliProxyEnvironment(bool showErrors)
        {
            string message;
            if (!cliProxy.IsRunning && !cliProxy.Start(out message))
            {
                if (showErrors)
                {
                    MessageBox.Show(
                        message ?? "Не удалось запустить CLI/Codex proxy.",
                        "CLI/Codex proxy",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
                return false;
            }

            CliProxyEnvironmentService.ApplyUserEnvironment();
            return true;
        }

        private void ApplyCliProxyEnvironment()
        {
            if (!EnsureCliProxyEnvironment(true)) return;

            MessageBox.Show(
                "Proxy environment применён для текущего пользователя через HTTP CONNECT bridge.\n\n" +
                "HTTPS_PROXY=" + CliProxyEnvironmentService.ProxyUrl + "\n" +
                "HTTP_PROXY=" + CliProxyEnvironmentService.ProxyUrl + "\n" +
                "ALL_PROXY=" + CliProxyEnvironmentService.ProxyUrl + "\n\n" +
                "Прямой SOCKS остаётся внутренним endpoint. Уже открытые терминалы не получат новое окружение автоматически.",
                "CLI/Codex proxy",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void ExitProGo()
        {
            ikev2Relay.Stop();
            cliProxy.Stop();
            CliProxyEnvironmentService.ClearUserEnvironmentIfOwned();
            tray.Visible = false;
            ExitThread();
        }

        private void OpenPowerShellWithCliProxy()
        {
            if (!cliProxy.IsRunning && !StartCliProxy(false)) return;

            string message;
            if (!CliProxyEnvironmentService.OpenPowerShellWithEnvironment(out message))
            {
                MessageBox.Show(message ?? "Не удалось открыть PowerShell с CLI proxy.", "CLI/Codex proxy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
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
                "ProGo удалит старые автоматические резервные копии.\n\nБудут сохранены ручные копии, последний baseline и последний pre-update backup.\n\nПродолжить?",
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
                MessageBox.Show("Резервные копии не найдены. Сначала создайте резервную копию или дождитесь следующего обновления версии.", "Откат ProGo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string backupDir;
            if (!BackupPickerForm.TryPick(backups, out backupDir)) return;

            var result = MessageBox.Show(
                "ProGo будет закрыт, восстановит выбранную резервную копию и запустится заново.\n\nВыбранная копия:\n" + backupDir + "\n\nПродолжить?",
                "Откат ProGo",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes) return;

            if (!BackupService.StartRestore(backupDir)) return;

            SafeLog.Info("Restore requested by user: " + backupDir + ".");
            tray.Visible = false;
            ExitThread();
        }

        private void StartUpdate()
        {
            var check = UpdateLauncher.CheckForUpdate();

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
            tray.Text = AppConstants.ProductName + " — SOCKS: " + (proxy.IsListening() ? "работает" : "остановлен");
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
                tray.Dispose();
                icon.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}

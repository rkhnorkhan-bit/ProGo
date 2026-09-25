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
        private readonly ClipboardService clipboard;
        private readonly NotifyIcon tray;
        private readonly System.Drawing.Icon icon;
        private Timer startupShowTimer;

        public UpdateAwareTrayApplicationContext(SettingsService settingsService, ProxyService proxyService, ClipboardService clipboardService, bool showStatusOnStartup)
        {
            settings = settingsService;
            proxy = proxyService;
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
            menu.Items.Add("Остановить SOCKS", null, delegate { proxy.StopTunnel(); UpdateTooltip(); });
            menu.Items.Add("Перезапустить SOCKS", null, delegate { proxy.RestartTunnel(); UpdateTooltip(); });
            menu.Items.Add("Изменить порт SOCKS...", null, delegate { ChangePort(); });
            menu.Items.Add("Применить настройки прокси", null, delegate { EnvironmentProxyService.Apply(settings.Current); MessageBox.Show("Настройки прокси применены. Уже запущенным процессам может потребоваться перезапуск.", AppConstants.ProductName); });
            menu.Items.Add("Проверить соединение", null, delegate { MessageBox.Show(RouteTester.Test(settings.Current, proxy), "Проверка соединения"); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Хранилище секретов", null, delegate { ShowVault(); });
            menu.Items.Add("Настройки", null, delegate { ShowSettings(); });
            menu.Items.Add("Открыть журнал", null, delegate { OpenLog(); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Создать резервную копию", null, delegate { CreateBackup(); });
            menu.Items.Add("Откатить из резервной копии...", null, delegate { StartRestore(); });
            menu.Items.Add("Открыть папку резервных копий", null, delegate { OpenBackups(); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Обновить ProGo", null, delegate { StartUpdate(); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Выход", null, delegate { ExitThread(); });
            return menu;
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
            int port;
            if (!PortForm.TryGetPort(settings.Current.SocksPort, out port)) return;
            var next = settings.Current;
            next.SocksPort = port;
            settings.Save(next);
            proxy.RestartTunnel();
            EnvironmentProxyService.Apply(settings.Current);
            MessageBox.Show("Порт SOCKS изменён на " + port + ".", "Порт SOCKS");
            UpdateTooltip();
        }

        private void OpenLog()
        {
            try
            {
                AppPaths.EnsureDirectories();
                if (!File.Exists(AppPaths.LogPath)) File.WriteAllText(AppPaths.LogPath, "");
                Process.Start("notepad.exe", AppPaths.LogPath);
            }
            catch (Exception ex)
            {
                SafeLog.Error("Open log failed.", ex);
                MessageBox.Show("Не удалось открыть журнал.", AppConstants.ProductName);
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
            var result = MessageBox.Show(
                "ProGo проверит версию в GitHub. Если обновление есть, будет создана резервная копия текущей версии, затем приложение обновится и запустится заново.\n\nПродолжить?",
                "Обновление ProGo",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes) return;

            if (!UpdateLauncher.StartUpdater()) return;

            SafeLog.Info("Update requested by user.");
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

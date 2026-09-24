using System;
using System.Diagnostics;
using System.Drawing;
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

        public UpdateAwareTrayApplicationContext(SettingsService settingsService, ProxyService proxyService, ClipboardService clipboardService)
        {
            settings = settingsService;
            proxy = proxyService;
            clipboard = clipboardService;

            tray = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Text = AppConstants.ProductName,
                Visible = true,
                ContextMenuStrip = BuildMenu()
            };
            tray.DoubleClick += delegate { ShowStatus(); };
            UpdateTooltip();
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
            using (var form = new SettingsForm(settings)) form.ShowDialog();
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

        private void StartUpdate()
        {
            var result = MessageBox.Show(
                "ProGo скачает свежую версию из GitHub, пересоберёт приложение и заменит установленный файл. Хранилище и настройки будут сохранены.\n\nПродолжить?",
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
            if (disposing) tray.Dispose();
            base.Dispose(disposing);
        }
    }
}

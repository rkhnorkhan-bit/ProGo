using System;
using System.Windows.Forms;

namespace ProGo
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            AppPaths.EnsureDirectories();
            SafeLog.Info("ProGo started.");
            BackupService.EnsureVersionBackupExists("startup");

            using (var settingsService = new SettingsService())
            using (var proxyService = new ProxyService(settingsService))
            using (var clipboardService = new ClipboardService(settingsService))
            using (var context = new UpdateAwareTrayApplicationContext(settingsService, proxyService, clipboardService))
            {
                if (settingsService.Current.AutoApplyProxy)
                {
                    EnvironmentProxyService.Apply(settingsService.Current);
                }

                if (settingsService.Current.AutoStartSocks)
                {
                    proxyService.StartTunnel(false);
                }

                Application.Run(context);
            }

            SafeLog.Info("ProGo stopped.");
        }
    }
}

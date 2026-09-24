using System;
using System.Windows.Forms;

namespace ProGo
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            try
            {
                AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs args)
                {
                    var ex = args.ExceptionObject as Exception;
                    SafeLog.Error("Fatal unhandled exception.", ex ?? new Exception(String.valueOf(args.ExceptionObject)));
                };

                Application.ThreadException += delegate(object sender, System.Threading.ThreadExceptionEventArgs args)
                {
                    SafeLog.Error("Fatal UI thread exception.", args.Exception);
                    ShowFatal(args.Exception);
                };

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                AppPaths.EnsureDirectories();
                SafeLog.Info("ProGo started.");

                // Backup creation must never block tray startup. It logs internally on failure.
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
            catch (Exception ex)
            {
                try { SafeLog.Error("ProGo startup failed.", ex); }
                catch { }

                ShowFatal(ex);
            }
        }

        private static void ShowFatal(Exception ex)
        {
            try
            {
                MessageBox.Show(
                    "ProGo не смог запуститься. Подробности записаны в журнал:\n" + AppPaths.LogPath + "\n\n" + ex.GetType().Name + ": " + ex.Message,
                    AppConstants.ProductName,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch
            {
            }
        }
    }
}

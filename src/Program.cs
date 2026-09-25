using System;
using System.IO;
using System.Windows.Forms;

namespace ProGo
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            var selfCheck = HasArg(args, "--self-check") || HasArg(args, "/self-check") || HasArg(args, "self-check");

            try
            {
                AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs eventArgs)
                {
                    var ex = eventArgs.ExceptionObject as Exception;
                    SafeLog.Error("Fatal unhandled exception.", ex ?? new Exception(Convert.ToString(eventArgs.ExceptionObject)));
                };

                Application.ThreadException += delegate(object sender, System.Threading.ThreadExceptionEventArgs eventArgs)
                {
                    SafeLog.Error("Fatal UI thread exception.", eventArgs.Exception);
                    if (!selfCheck) ShowFatal(eventArgs.Exception);
                };

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                AppPaths.EnsureDirectories();

                if (selfCheck)
                {
                    RunSelfCheck();
                    return;
                }

                SafeLog.Info("ProGo started.");

                // Backup creation must never block tray startup. It logs internally on failure.
                BackupService.EnsureVersionBackupExists("startup");

                var showStatusOnStartup = HasArg(args, "--show") || HasArg(args, "/show") || HasArg(args, "show");

                using (var settingsService = new SettingsService())
                using (var proxyService = new ProxyService(settingsService))
                using (var clipboardService = new ClipboardService(settingsService))
                using (var context = new UpdateAwareTrayApplicationContext(settingsService, proxyService, clipboardService, showStatusOnStartup))
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
                try { SafeLog.Error(selfCheck ? "ProGo self-check failed." : "ProGo startup failed.", ex); }
                catch { }

                if (selfCheck)
                {
                    Environment.Exit(2);
                    return;
                }

                ShowFatal(ex);
            }
        }

        private static void RunSelfCheck()
        {
            if (String.IsNullOrWhiteSpace(AppPaths.Root)) throw new InvalidOperationException("App root is empty.");
            AppPaths.EnsureDirectories();

            using (var settingsService = new SettingsService())
            {
                if (settingsService.Current == null) throw new InvalidOperationException("Settings are not available.");
                if (settingsService.Current.SocksPort < 1 || settingsService.Current.SocksPort > 65535) throw new InvalidOperationException("Invalid SOCKS port.");
                if (settingsService.Current.SshProfiles == null) throw new InvalidOperationException("SSH profile list is not available.");
            }

            var exePath = Application.ExecutablePath;
            if (String.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath)) throw new FileNotFoundException("Executable path is not available.", exePath);

            SafeLog.Info("Self-check completed.");
            Environment.Exit(0);
        }

        private static bool HasArg(string[] args, string value)
        {
            if (args == null) return false;

            foreach (var arg in args)
            {
                if (String.Equals(arg, value, StringComparison.OrdinalIgnoreCase)) return true;
            }

            return false;
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

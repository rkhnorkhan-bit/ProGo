using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows.Forms;

namespace ProGo
{
    internal static class UpdateLauncher
    {
        private const string UpdateScriptName = "Update-ProGo.ps1";
        private const string RawUpdateScriptUrl = "https://raw.githubusercontent.com/rkhnorkhan-bit/ProGo/main/scripts/Update-ProGo.ps1";

        public static bool StartUpdater()
        {
            try
            {
                var scriptPath = ResolveUpdateScriptPath();
                if (String.IsNullOrEmpty(scriptPath) || !File.Exists(scriptPath))
                {
                    MessageBox.Show(
                        "Скрипт обновления не найден и не смог быть скачан из GitHub. Запустите установку из GitHub один раз вручную.",
                        "Обновление ProGo",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return false;
                }

                var powershell = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "WindowsPowerShell", "v1.0", "powershell.exe");
                if (!File.Exists(powershell)) powershell = "powershell.exe";

                var currentPid = Process.GetCurrentProcess().Id;
                var args = "-NoProfile -ExecutionPolicy Bypass -File \"" + scriptPath + "\" -WaitPid " + currentPid;
                var psi = new ProcessStartInfo(powershell, args)
                {
                    UseShellExecute = false,
                    CreateNoWindow = false,
                    WorkingDirectory = AppPaths.Root
                };

                Process.Start(psi);
                return true;
            }
            catch (Exception ex)
            {
                SafeLog.Error("Updater launch failed.", ex);
                MessageBox.Show(
                    "Не удалось запустить обновление. Подробности записаны в журнал.",
                    "Обновление ProGo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }
        }

        private static string ResolveUpdateScriptPath()
        {
            var installedScript = Path.Combine(AppPaths.Root, "scripts", UpdateScriptName);
            if (File.Exists(installedScript)) return installedScript;

            var executableDir = Path.GetDirectoryName(Application.ExecutablePath) ?? AppPaths.Root;
            var besideExeScript = Path.Combine(executableDir, "scripts", UpdateScriptName);
            if (File.Exists(besideExeScript))
            {
                TryCopyScriptToInstalledLocation(besideExeScript, installedScript);
                return File.Exists(installedScript) ? installedScript : besideExeScript;
            }

            if (TryDownloadUpdateScript(installedScript)) return installedScript;

            return String.Empty;
        }

        private static void TryCopyScriptToInstalledLocation(string source, string destination)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.Copy(source, destination, true);
                SafeLog.Info("Updater script copied to install data folder.");
            }
            catch (Exception ex)
            {
                SafeLog.Error("Updater script copy failed.", ex);
            }
        }

        private static bool TryDownloadUpdateScript(string destination)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                using (var client = new WebClient())
                {
                    client.Headers.Add("User-Agent", "ProGo-Updater");
                    client.DownloadFile(RawUpdateScriptUrl, destination);
                }

                SafeLog.Info("Updater script downloaded from GitHub.");
                return File.Exists(destination);
            }
            catch (Exception ex)
            {
                SafeLog.Error("Updater script download failed.", ex);
                return false;
            }
        }
    }
}

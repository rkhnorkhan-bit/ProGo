using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace ProGo
{
    internal static class UpdateLauncher
    {
        private const string UpdateScriptName = "Update-ProGo.ps1";

        public static bool StartUpdater()
        {
            try
            {
                var scriptPath = Path.Combine(AppPaths.Root, "scripts", UpdateScriptName);
                if (!File.Exists(scriptPath))
                {
                    MessageBox.Show(
                        "Скрипт обновления не найден. Установите ProGo из GitHub заново, чтобы добавить update-контур.",
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
    }
}

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace ProGo
{
    internal enum UpdateAvailability
    {
        Error,
        UpToDate,
        Available
    }

    internal sealed class UpdateCheckResult
    {
        public UpdateAvailability Availability { get; set; }
        public string LocalVersion { get; set; }
        public string RemoteVersion { get; set; }
        public string ErrorMessage { get; set; }
    }

    internal static class UpdateLauncher
    {
        private const string UpdateScriptName = "Update-ProGo.ps1";
        private const string RawUpdateScriptUrl = "https://raw.githubusercontent.com/rkhnorkhan-bit/ProGo/main/scripts/Update-ProGo.ps1";
        private const string LatestReleaseApiUrl = "https://api.github.com/repos/rkhnorkhan-bit/ProGo/releases/latest";

        private sealed class GitHubReleaseInfo
        {
            public string tag_name { get; set; }
            public bool draft { get; set; }
            public bool prerelease { get; set; }
        }

        public static UpdateCheckResult CheckForUpdate()
        {
            var localVersion = ReadInstalledVersion();

            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                string json;
                using (var client = new WebClient())
                {
                    client.Headers.Add("User-Agent", "ProGo-Updater");
                    client.Headers.Add("Accept", "application/vnd.github+json");
                    json = client.DownloadString(LatestReleaseApiUrl);
                }

                var serializer = new JavaScriptSerializer();
                var release = serializer.Deserialize<GitHubReleaseInfo>(json);
                var remoteVersion = NormalizeVersion(release == null ? null : release.tag_name);

                Version local;
                Version remote;
                if (!Version.TryParse(localVersion, out local))
                {
                    return ErrorResult(localVersion, remoteVersion, "Не удалось определить установленную версию ProGo.");
                }
                if (!Version.TryParse(remoteVersion, out remote))
                {
                    return ErrorResult(localVersion, remoteVersion, "GitHub вернул некорректную версию релиза.");
                }

                var availability = remote > local ? UpdateAvailability.Available : UpdateAvailability.UpToDate;
                SafeLog.Info("Update check completed. local=" + localVersion + "; remote=" + remoteVersion + "; result=" + availability + ".");
                return new UpdateCheckResult
                {
                    Availability = availability,
                    LocalVersion = localVersion,
                    RemoteVersion = remoteVersion
                };
            }
            catch (Exception ex)
            {
                SafeLog.Error("Update check failed.", ex);
                return ErrorResult(localVersion, null, "Не удалось проверить обновления на GitHub.");
            }
        }

        private static UpdateCheckResult ErrorResult(string localVersion, string remoteVersion, string message)
        {
            return new UpdateCheckResult
            {
                Availability = UpdateAvailability.Error,
                LocalVersion = localVersion,
                RemoteVersion = remoteVersion,
                ErrorMessage = message
            };
        }

        private static string ReadInstalledVersion()
        {
            try
            {
                var path = Path.Combine(AppPaths.Root, "VERSION");
                if (!File.Exists(path)) return "0.0.0";
                return NormalizeVersion(File.ReadAllText(path));
            }
            catch
            {
                return "0.0.0";
            }
        }

        private static string NormalizeVersion(string value)
        {
            value = (value ?? String.Empty).Trim();
            if (value.StartsWith("v", StringComparison.OrdinalIgnoreCase)) value = value.Substring(1);
            return value;
        }

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
                    CreateNoWindow = true,
                    WorkingDirectory = AppPaths.Root
                };

                SafeLog.Info("Launching updater. PowerShell=" + powershell + "; script=" + scriptPath + "; workingDirectory=" + AppPaths.Root + ".");
                var process = Process.Start(psi);
                if (process == null)
                {
                    throw new InvalidOperationException("Updater process was not created.");
                }

                SafeLog.Info("Updater process started. PID=" + process.Id + ".");

                if (process.WaitForExit(1200))
                {
                    var exitCode = process.ExitCode;
                    SafeLog.Error("Updater process exited before handoff. ExitCode=" + exitCode + ".", new InvalidOperationException("Updater process exited before handoff."));
                    MessageBox.Show(
                        "Обновление не стартовало. ProGo останется запущенным. Подробности записаны в журнал.",
                        "Обновление ProGo",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return false;
                }

                SafeLog.Info("Updater handoff confirmed. PID=" + process.Id + ".");
                return true;
            }
            catch (Win32Exception ex)
            {
                SafeLog.Error("Updater launch failed. NativeErrorCode=" + ex.NativeErrorCode + ".", ex);
                MessageBox.Show(
                    "Не удалось запустить обновление. Подробности записаны в журнал.",
                    "Обновление ProGo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
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

            // Always try to refresh the updater first. This prevents a stale local
            // Update-ProGo.ps1 from keeping older update behavior forever.
            if (TryDownloadUpdateScript(installedScript)) return installedScript;

            if (File.Exists(installedScript)) return installedScript;

            var executableDir = Path.GetDirectoryName(Application.ExecutablePath) ?? AppPaths.Root;
            var besideExeScript = Path.Combine(executableDir, "scripts", UpdateScriptName);
            if (File.Exists(besideExeScript))
            {
                TryCopyScriptToInstalledLocation(besideExeScript, installedScript);
                return File.Exists(installedScript) ? installedScript : besideExeScript;
            }

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

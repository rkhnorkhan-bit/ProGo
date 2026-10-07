using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
        private const string LatestReleaseApiUrl = "https://api.github.com/repos/rkhnorkhan-bit/ProGo/releases/latest";

        private sealed class GitHubReleaseInfo
        {
            public string tag_name { get; set; }
            public bool draft { get; set; }
            public bool prerelease { get; set; }
        }

        private const int UpdateCheckTimeoutMilliseconds = 15000;
        private const int MaxReleaseMetadataBytes = 2 * 1024 * 1024;

        public static UpdateCheckResult CheckForUpdate()
        {
            return CheckForUpdateAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        public static Task<UpdateCheckResult> CheckForUpdateAsync(CancellationToken cancellationToken)
        {
            return CheckForUpdateAsync(new Uri(LatestReleaseApiUrl), ReadInstalledVersion(),
                UpdateCheckTimeoutMilliseconds, cancellationToken);
        }

        // An explicit endpoint/deadline permits real loopback transport tests without
        // changing the application's fixed release URL or installing an update.
        internal static async Task<UpdateCheckResult> CheckForUpdateAsync(Uri endpoint,
            string localVersion, int timeoutMilliseconds, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (timeoutMilliseconds <= 0) throw new ArgumentOutOfRangeException("timeoutMilliseconds");
            if (endpoint == null || (endpoint.Scheme != Uri.UriSchemeHttps &&
                !(endpoint.IsLoopback && endpoint.Scheme == Uri.UriSchemeHttp)))
                throw new ArgumentException("Release metadata requires HTTPS or a loopback fixture.", "endpoint");

            using (var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                deadline.CancelAfter(timeoutMilliseconds);
                try
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    var request = (HttpWebRequest)WebRequest.Create(endpoint);
                    request.UserAgent = "ProGo-Updater";
                    request.Accept = "application/vnd.github+json";
                    // Fixtures must not depend on the current user's proxy settings.
                    if (endpoint.IsLoopback) request.Proxy = null;
                    string json;
                    // HttpWebRequest's Timeout does not bound asynchronous requests.
                    // Abort also interrupts pending headers and response-body reads.
                    using (deadline.Token.Register(request.Abort))
                    using (var response = (HttpWebResponse)await request.GetResponseAsync().ConfigureAwait(false))
                    using (var stream = response.GetResponseStream())
                    using (deadline.Token.Register(stream.Close))
                    using (var bytes = new MemoryStream())
                    {
                        if (response.ContentLength > MaxReleaseMetadataBytes)
                            throw new InvalidDataException("Release metadata exceeds the size limit.");
                        var buffer = new byte[8192];
                        while (true)
                        {
                            deadline.Token.ThrowIfCancellationRequested();
                            int count = await stream.ReadAsync(buffer, 0, buffer.Length, deadline.Token).ConfigureAwait(false);
                            if (count == 0) break;
                            if (bytes.Length + count > MaxReleaseMetadataBytes)
                                throw new InvalidDataException("Release metadata exceeds the size limit.");
                            bytes.Write(buffer, 0, count);
                        }
                        bytes.Position = 0;
                        using (var reader = new StreamReader(bytes, Encoding.UTF8, true))
                            json = reader.ReadToEnd();
                    }
                    deadline.Token.ThrowIfCancellationRequested();
                    var serializer = new JavaScriptSerializer();
                    var release = serializer.Deserialize<GitHubReleaseInfo>(json);
                    var remoteVersion = NormalizeVersion(release == null ? null : release.tag_name);

                    Version local;
                    Version remote;
                    if (!Version.TryParse(localVersion, out local))
                        return ErrorResult(localVersion, remoteVersion, "Не удалось определить установленную версию ProGo.");
                    if (!Version.TryParse(remoteVersion, out remote))
                        return ErrorResult(localVersion, remoteVersion, "GitHub вернул некорректную версию релиза.");

                    deadline.Token.ThrowIfCancellationRequested();
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
                    // User cancellation is not a failed update check. Abort may surface
                    // as WebException/IOException instead of OperationCanceledException.
                    cancellationToken.ThrowIfCancellationRequested();
                    if (deadline.IsCancellationRequested)
                    {
                        SafeLog.Info("Update check timed out.");
                        return ErrorResult(localVersion, null,
                            "Время ожидания ответа GitHub истекло. Повторите проверку обновлений.");
                    }
                    SafeLog.Error("Update check failed.", ex);
                    return ErrorResult(localVersion, null, "Не удалось проверить обновления на GitHub.");
                }
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
                        "Файл обновления отсутствует. Откройте «Помощь → Антивирус» для восстановления установки.",
                        "Обновление ProGo",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return false;
                }

                var powershell = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "WindowsPowerShell", "v1.0", "powershell.exe");
                if (!File.Exists(powershell)) powershell = "powershell.exe";

                var currentPid = Process.GetCurrentProcess().Id;
                var args = "-NoProfile -File \"" + scriptPath + "\" -WaitPid " + currentPid;
                var psi = new ProcessStartInfo(powershell, args)
                {
                    UseShellExecute = false,
                    CreateNoWindow = false,
                    WorkingDirectory = AppPaths.Root
                };

                SafeLog.Info("Launching updater. PowerShell=" + powershell + "; script=" + scriptPath + "; workingDirectory=" + AppPaths.Root + ".");
                if (!MaintenanceOperation.StartHandoff(psi))
                {
                    MessageBox.Show("Обновление не получило управление. Возможно, уже выполняется обновление или восстановление. ProGo останется запущенным.", "Обновление ProGo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
                SafeLog.Info("Updater ownership handoff confirmed.");
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

        private static string BuildEarlyExitMessage(int exitCode, bool likelyAntivirusBlock)
        {
            if (likelyAntivirusBlock)
            {
                return
                    "Обновление не стартовало: процесс обновления завершился слишком рано.\n\n" +
                    "Похоже, антивирус или система защиты заблокировал загрузку/запуск обновления.\n\n" +
                    "Что сделать:\n" +
                    "Откройте отчёт антивируса и сохраните название обнаружения.\n" +
                    "В разделе «Помощь» есть официальный выпуск и инструкция проверки.\n\n" +
                    "ExitCode: " + exitCode + "\n" +
                    "Подробности записаны в update.log и progo.log.";
            }

            return
                "Обновление не стартовало. ProGo останется запущенным.\n\n" +
                "Процесс обновления завершился до передачи управления. Код: " + exitCode + "\n\n" +
                "Подробности записаны в update.log и progo.log.";
        }

        private static bool LooksLikeAntivirusBlock(string text)
        {
            if (String.IsNullOrWhiteSpace(text)) return false;
            var lower = text.ToLowerInvariant();
            return lower.IndexOf("antivirus", StringComparison.Ordinal) >= 0 ||
                   lower.IndexOf("forbidden by antivirus", StringComparison.Ordinal) >= 0 ||
                   lower.IndexOf("request has been forbidden", StringComparison.Ordinal) >= 0 ||
                   lower.IndexOf(" 499", StringComparison.Ordinal) >= 0 ||
                   lower.IndexOf("(499)", StringComparison.Ordinal) >= 0 ||
                   lower.IndexOf("access is denied", StringComparison.Ordinal) >= 0 ||
                   lower.IndexOf("access denied", StringComparison.Ordinal) >= 0 ||
                   lower.IndexOf("отказано в доступе", StringComparison.Ordinal) >= 0;
        }

        private static string ReadRecentUpdaterLogs()
        {
            return BoundedLog.ReadTail(BoundedLog.UpdaterLogPath(AppPaths.Root), 12000);
        }

        private static string ResolveUpdateScriptPath()
        {
            var besideExe = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "scripts", UpdateScriptName);
            if (File.Exists(besideExe)) return besideExe;
            throw new FileNotFoundException("Файл обновления отсутствует. Откройте официальный выпуск в разделе «Помощь» и восстановите установку.");
        }
    }
}

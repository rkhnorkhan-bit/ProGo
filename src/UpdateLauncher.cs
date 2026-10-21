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
        public string ReleaseNotes { get; set; }
    }

    internal static class UpdateLauncher
    {
        private const string UpdateScriptName = "Update-ProGo.ps1";
        private const string LatestReleaseApiUrl = "https://api.github.com/repos/rkhnorkhan-bit/ProGo/releases/latest";

        private sealed class GitHubReleaseInfo
        {
            public string tag_name { get; set; }
            public string body { get; set; }
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
                HttpWebRequest request = null;
                try
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    request = (HttpWebRequest)WebRequest.Create(endpoint);
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
                        RemoteVersion = remoteVersion,
                        ReleaseNotes = SummarizeReleaseNotes(release.body)
                    };
                }
                catch (Exception ex)
                {
                    if (request != null) request.Abort();
                    var webError = ex as WebException;
                    if (webError != null && webError.Response != null) webError.Response.Close();
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

        internal static string ReadInstalledVersion()
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

        internal static string SummarizeReleaseNotes(string value)
        {
            if (String.IsNullOrWhiteSpace(value)) return "Описание изменений для этой версии не опубликовано.";
            var text = new StringBuilder();
            bool shortened = false;
            foreach (char character in value) {
                if (character == '\r') continue;
                if (Char.IsControl(character) && character != '\n' && character != '\t') continue;
                if (text.Length == 3000) { shortened = true; break; }
                text.Append(character);
            }
            var result = text.ToString().Trim();
            if (result.Length == 0) return "Описание изменений для этой версии не опубликовано.";
            return result + (shortened ? "\n\n… Описание сокращено." : "");
        }

        private static string NormalizeVersion(string value)
        {
            value = (value ?? String.Empty).Trim();
            if (value.StartsWith("v", StringComparison.OrdinalIgnoreCase)) value = value.Substring(1);
            return value;
        }

        private const string UpdaterLaunchFailure = "Не удалось подготовить или подтвердить запуск помощника обновления. Приложение остаётся запущенным; проверьте журнал перед повтором.";

        // Call from a tracked worker: resolving installed files, starting a child
        // and awaiting acknowledgement must never run on the UI thread.
        internal static MaintenanceHandoffResult LaunchUpdater(CancellationToken token, Func<ProcessStartInfo> createInfo = null)
        {
            try {
                token.ThrowIfCancellationRequested();
                var info = createInfo == null ? CreateUpdaterStartInfo() : createInfo();
                token.ThrowIfCancellationRequested();
                if (info == null) return new MaintenanceHandoffResult { Error = UpdaterLaunchFailure };
                var result = MaintenanceOperation.StartOwnedHandoff(info, token);
                // ACK is the accepted ownership boundary. Do not turn accepted
                // work into cancellation/failure because of a later token or log.
                if (result.Accepted) return result;
                return new MaintenanceHandoffResult { Error = UpdaterHandoffError(result.Error) };
            }
            catch (OperationCanceledException) {
                return new MaintenanceHandoffResult { Error = "Запуск помощника обновления отменён до передачи управления. Приложение остаётся запущенным." };
            }
            catch (FileNotFoundException ex) {
                LogUpdaterFailure(ex);
                return new MaintenanceHandoffResult { Error = "Файл обновления отсутствует. Откройте «Помощь → Антивирус» для восстановления установки. Приложение остаётся запущенным." };
            }
            catch (Exception ex) {
                LogUpdaterFailure(ex);
                // Cleanup errors must not claim that a helper was stopped.
                return new MaintenanceHandoffResult { Error = UpdaterLaunchFailure };
            }
        }

        private static void LogUpdaterFailure(Exception exception)
        {
            try { SafeLog.Error("Updater ownership handoff failed.", exception); }
            catch (Exception) { } // Logging cannot replace the protocol result.
        }

        internal static string UpdaterHandoffError(string error)
        {
            // These are fixed outputs of StartOwnedHandoff, never child stderr.
            // Preserve its actual cleanup claims; unrecognized errors stay unknown.
            if (error == "Помощник восстановления завершился без подтверждения. Приложение остаётся запущенным.")
                return "Помощник обновления завершился без подтверждения. Приложение остаётся запущенным.";
            if (error == "Подготовка восстановления отменена до подтверждения помощника. Приложение остаётся запущенным.")
                return "Подготовка обновления отменена до подтверждения помощника. Приложение остаётся запущенным.";
            if (error == "Помощник восстановления не подтвердил готовность за 15 секунд и остановлен. Приложение остаётся запущенным; повторите после проверки журнала.")
                return "Помощник обновления не подтвердил готовность за 15 секунд и остановлен. Приложение остаётся запущенным; повторите после проверки журнала.";
            return UpdaterLaunchFailure;
        }

        private static ProcessStartInfo CreateUpdaterStartInfo()
        {
            var scriptPath = ResolveUpdateScriptPath();
            var powershell = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "WindowsPowerShell", "v1.0", "powershell.exe");
            if (!File.Exists(powershell)) powershell = "powershell.exe";
            return new ProcessStartInfo(powershell, "-NoProfile -File \"" + scriptPath + "\" -WaitPid " + Process.GetCurrentProcess().Id) {
                UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = AppPaths.Root
            };
        }

        // Compatibility entry point. The application context should use LaunchUpdater
        // from a worker and owns its progress, cancellation and UI publication.
        public static bool StartUpdater()
        {
            var result = LaunchUpdater(CancellationToken.None);
            if (result.Accepted) return true;
            MessageBox.Show(result.Error ?? UpdaterLaunchFailure, "Обновление ProGo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
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

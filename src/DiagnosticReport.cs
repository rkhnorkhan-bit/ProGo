using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace ProGo
{
    // Export is an allowlisted projection, never a regex-cleaned copy of raw logs.
    public sealed class DiagnosticReport
    {
        public const int SourceBytes = 64 * 1024;
        public const int EventsPerSource = 60;
        public string Text { get; private set; }
        private DiagnosticReport(string text) { Text = text; }
        private static readonly string[,] Events = {
            { "ProGo started.", "APP_START" }, { "ProGo stopped.", "APP_STOP" },
            { "ProGo startup failed.", "APP_START_FAILED" }, { "ProGo self-check failed.", "SELF_CHECK_FAILED" },
            { "Self-check completed.", "SELF_CHECK_OK" }, { "Fatal unhandled exception.", "APP_FATAL" },
            { "Fatal UI thread exception.", "UI_FATAL" },
            { "SOCKS listener is ready.", "SOCKS_READY" }, { "SOCKS start requested.", "SOCKS_START" },
            { "SOCKS automatic restart requested.", "SOCKS_RESTART" },
            { "SOCKS interrupted;", "SOCKS_INTERRUPTED" }, { "SOCKS recovery check failed.", "SOCKS_CHECK_FAILED" },
            { "SOCKS recovery waits for an occupied port.", "SOCKS_PORT_BUSY" },
            { "SOCKS port is already in use;", "SOCKS_PORT_EXTERNAL" },
            { "Failed to start SSH tunnel.", "SSH_START_FAILED" }, { "Failed to stop SSH tunnel.", "SSH_STOP_FAILED" },
            { "SSH asynchronous startup failed.", "SSH_START_FAILED" },
            { "Application HTTP proxy started.", "HTTP_PROXY_START" }, { "CLI HTTP CONNECT proxy stopped.", "HTTP_PROXY_STOP" },
            { "Application proxy configuration failed.", "HTTP_PROXY_CONFIG_FAILED" },
            { "CLI proxy request failed.", "HTTP_REQUEST_FAILED" }, { "CLI proxy accept failed.", "HTTP_ACCEPT_FAILED" },
            { "Proxy environment applied for new terminals.", "CLI_ENV_ON" },
            { "Previous proxy environment restored where still owned by ProGo.", "CLI_ENV_RESTORED" },
            { "Environment restore failed.", "CLI_ENV_RESTORE_FAILED" },
            { "Current-user Windows proxy enabled.", "WINDOWS_PROXY_ON" },
            { "Windows proxy cleanup incomplete.", "WINDOWS_PROXY_CLEANUP_FAILED" },
            { "Automatic proxy setup failed:", "AUTO_PROXY_FAILED" },
            { "User connection action failed:", "CONNECTION_ACTION_FAILED" },
            { "User action failed:", "USER_ACTION_FAILED" },
            { "Shutdown refused: owned proxy cleanup incomplete.", "SHUTDOWN_CLEANUP_FAILED" },
            { "Update check completed.", "UPDATE_CHECK_OK" }, { "Update check failed.", "UPDATE_CHECK_FAILED" },
            { "Launching updater.", "UPDATER_LAUNCH" }, { "Updater launch failed.", "UPDATER_LAUNCH_FAILED" },
            { "Installed updater failed:", "UPDATER_FAILED" }, { "Updater ownership handoff confirmed.", "UPDATE_HANDOFF" },
            { "ProGo transactional update started.", "UPDATE_START" },
            { "Version check:", "UPDATE_VERSION_CHECK" }, { "ProGo is already up to date.", "UPDATE_CURRENT" },
            { "Downloading published package:", "UPDATE_DOWNLOAD" }, { "Package SHA-256 verified:", "UPDATE_HASH_OK" },
            { "Creating intermediate staging copy:", "UPDATE_STAGE" },
            { "Staging validation PASS.", "UPDATE_STAGE_OK" },
            { "Installing validated staging copy into main application directory.", "UPDATE_INSTALL" },
            { "ProGo transactional update completed.", "UPDATE_COMPLETE" }, { "TRANSACTION FAILED:", "UPDATE_FAILED" },
            { "Rolling back main application from backup:", "UPDATE_ROLLBACK" },
            { "Rollback completed after failed main commit.", "UPDATE_ROLLBACK_COMPLETE" },
            { "Rollback warning", "ROLLBACK_WARNING" }, { "Cleanup warning:", "CLEANUP_WARNING" },
            { "Backup created:", "BACKUP_CREATED" }, { "Installed-state backup created:", "BACKUP_CREATED" },
            { "Manual backup failed.", "BACKUP_FAILED" }, { "Backup cleanup failed.", "BACKUP_CLEANUP_FAILED" },
            { "ProGo restore started.", "RESTORE_START" }, { "Protective backup verified:", "RESTORE_BACKUP_OK" },
            { "ProGo restore completed.", "RESTORE_COMPLETE" }, { "RESTORE FAILED:", "RESTORE_FAILED" },
            { "Restore launch failed.", "RESTORE_LAUNCH_FAILED" },
            { "Restore preparation or cleanup failed.", "RESTORE_PREPARE_FAILED" },
            { "ERROR:", "OPERATION_ERROR" }
        };

        public static DiagnosticReport Build(string root, string version)
        {
            var output = new StringBuilder();
            output.AppendLine("ProGo — диагностический отчёт / формат 1");
            output.AppendLine("Версия: " + (Regex.IsMatch(version ?? "", @"\A\d{1,3}\.\d{1,3}\.\d{1,3}\z") ? version : "неизвестна"));
            output.AppendLine("Только известные коды событий и время UTC. Адреса, пути, параметры и текст ошибок исключены.");
            output.AppendLine("Неизвестные строки не экспортируются. Это ограниченная выборка, не полный журнал.");
            output.AppendLine("APP — приложение; SOCKS/SSH — туннель; HTTP/CLI — прокси терминалов; UPDATE — обновление; RESTORE — восстановление.");
            output.AppendLine("FAILED/ERROR/FATAL — ошибка; WARNING — предупреждение; START/STOP — запуск/остановка; OK/COMPLETE — завершение этапа.");
            string update = "update.log";
            try { if (!File.Exists(Path.Combine(root, update)) && File.Exists(Path.Combine(root, "progo-update.log"))) update = "progo-update.log"; }
            catch { }
            string[] names = { "progo.log", update, "progo-restore.log" };
            string[] labels = { "Приложение", "Обновление", "Восстановление" };
            for (int i = 0; i < names.Length; i++)
                for (int generation = 2; generation >= 0; generation--)
                    AddSource(output, root, names[i] + (generation == 0 ? "" : "." + generation),
                        labels[i] + (generation == 0 ? " / текущий" : " / архив " + generation));
            return new DiagnosticReport(output.ToString());
        }

        private static void AddSource(StringBuilder output, string root, string name, string label)
        {
            output.AppendLine(); output.AppendLine("[" + label + "]");
            try
            {
                string path = Path.Combine(root, name);
                if (!File.Exists(path)) { output.AppendLine("Нет доступного файла."); return; }
                if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                    throw new IOException();
                string text;
                using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    long length = file.Length;
                    int count = (int)Math.Min(length, SourceBytes);
                    byte[] data = new byte[count]; file.Position = length - count;
                    int read = 0, part;
                    while (read < count && (part = file.Read(data, read, count - read)) > 0) read += part;
                    text = Encoding.UTF8.GetString(data, 0, read);
                    if (length > count)
                    {
                        // Never interpret a partial first line as a complete event.
                        int first = text.IndexOf('\n'); text = first < 0 ? "" : text.Substring(first + 1);
                        output.AppendLine("Взят только конец файла (до 64 КиБ).");
                    }
                }
                var events = new Queue<string>(); int omitted = 0, older = 0;
                foreach (string raw in text.Split('\n'))
                {
                    string line = raw.Trim('\r', '\uFEFF');
                    if (String.IsNullOrWhiteSpace(line)) continue;
                    string stamp = "время неизвестно";
                    DateTimeOffset parsed;
                    if (line.Length >= 27 && DateTimeOffset.TryParseExact(line.Substring(0, 26), "yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed) && line[26] == ' ')
                    {
                        stamp = parsed.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture);
                        line = line.Substring(27);
                    }
                    foreach (string level in new[] { "[INFO] ", "[ERROR] ", "[WARN] " })
                        if (line.StartsWith(level, StringComparison.Ordinal)) { line = line.Substring(level.Length); break; }
                    string code = null;
                    for (int i = 0; i < Events.GetLength(0); i++)
                        if (line.StartsWith(Events[i, 0], StringComparison.Ordinal)) { code = Events[i, 1]; break; }
                    if (code == null) { omitted++; continue; }
                    events.Enqueue(stamp + " | " + code);
                    if (events.Count > EventsPerSource) { events.Dequeue(); older++; }
                }
                foreach (string entry in events) output.AppendLine(entry);
                output.AppendLine("Событий: " + events.Count + "; неизвестных строк исключено: " + omitted + "; более ранних событий пропущено: " + older + ".");
            }
            catch { output.AppendLine("Файл недоступен. Его содержимое не включено."); }
        }
    }
}

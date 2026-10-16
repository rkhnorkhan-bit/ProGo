using System;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.Text;

namespace ProGo
{
    internal sealed class SshProfileDiagnosticResult
    {
        public string Target { get; set; }
        public string ConfigPath { get; set; }
        public string ResolvedHostName { get; set; }
        public string ResolvedUser { get; set; }
        public string ResolvedPort { get; set; }
        public string ResolvedIdentityFile { get; set; }
        internal readonly List<string> IdentityFiles = new List<string>();
        internal readonly Dictionary<string, string> IdentityPaths = new Dictionary<string, string>(StringComparer.Ordinal);
        internal bool IdentityListLimited;
        internal string OriginalHost;
        public string ExecutablePath { get; set; }
        public string AgentKeys { get; set; }
        public string IdentityAgent { get; set; }
        public string IdentitiesOnly { get; set; }
        public string AgentState { get; set; }
        internal SshAgentServiceSnapshot AgentService;
        public string KeyStatus { get; set; }
        public bool SshAvailable { get; set; }
        public bool SshResolved { get; set; }
        public string Error { get; set; }

        public string ToReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("SSH target: " + (Target ?? ""));
            var assembly = System.Reflection.Assembly.GetEntryAssembly() ?? typeof(SshProfileDiagnosticResult).Assembly;
            var buildIdentity = (System.Reflection.AssemblyInformationalVersionAttribute)Attribute.GetCustomAttribute(assembly, typeof(System.Reflection.AssemblyInformationalVersionAttribute));
            sb.AppendLine("Сборка запущенного ProGo: " + (buildIdentity == null ? assembly.GetName().Version.ToString() : buildIdentity.InformationalVersion));
            sb.AppendLine();
            if (ConfigPath != null) {
                sb.AppendLine("Явный файл SSH config:"); sb.AppendLine(ConfigPath);
                sb.AppendLine("Include, Host и Match обрабатывает выбранный OpenSSH.");
            } else sb.AppendLine("Конфигурацию обрабатывает выбранный OpenSSH: пользовательские и системные файлы, Include, Host и Match.");

            sb.AppendLine();
            sb.AppendLine("Проверка ssh.exe -G:");
            sb.AppendLine("ssh.exe доступен: " + (SshAvailable ? "да" : "не подтверждено"));
            sb.AppendLine("Исполняемый файл: " + (ExecutablePath ?? "не определён"));
            sb.AppendLine("Конфигурация обработана OpenSSH: " + (SshResolved ? "да" : "нет"));

            if (!String.IsNullOrWhiteSpace(ResolvedHostName)) sb.AppendLine("hostname: " + ResolvedHostName);
            if (!String.IsNullOrWhiteSpace(ResolvedPort)) sb.AppendLine("Порт SSH: " + ResolvedPort);
            if (!String.IsNullOrWhiteSpace(ResolvedUser)) sb.AppendLine("user: " + ResolvedUser);
            if (IdentityFiles.Count > 0) foreach (var identity in IdentityFiles) {
                sb.AppendLine("IdentityFile (OpenSSH): " + identity);
                string path;
                if (IdentityPaths.TryGetValue(identity, out path)) sb.AppendLine("  Проверяемый путь: " + path);
                sb.AppendLine("  " + InspectIdentity(identity));
            }
            else sb.AppendLine("Файл ключа: " + (KeyStatus ?? "не проверен"));
            if (IdentityListLimited) sb.AppendLine("Список IdentityFile ограничен: показаны первые 32 значения; остальные пути не проверялись.");
            sb.AppendLine("ssh-agent: " + (AgentState ?? "не проверен"));
            sb.AppendLine("Ключи стандартного агента: " + (AgentKeys ?? "не проверены"));
            if (!String.IsNullOrWhiteSpace(IdentityAgent)) sb.AppendLine("IdentityAgent: " +
                (IdentityAgent == "none" ? "Использование агента отключено этим SSH-профилем." : "Указан отдельный агент; его доступность проверяется при подключении."));
            if (!String.IsNullOrWhiteSpace(IdentitiesOnly)) sb.AppendLine("IdentitiesOnly: " + IdentitiesOnly);
            sb.AppendLine("Для зашифрованного ключа: загрузите его через ssh-add в обычном терминале. Passphrase вводится только в OpenSSH.");
            sb.AppendLine("Если агент остановлен: с согласия владельца попробуйте Start-Service ssh-agent в обычном PowerShell. При отказе в доступе обратитесь к владельцу ПК; запуск зависит от имеющихся прав.");
            sb.AppendLine("Если агент отключён: с согласия владельца выполните Set-Service ssh-agent -StartupType Manual, затем Start-Service ssh-agent. Без прав администратора обратитесь к владельцу ПК.");
            sb.AppendLine("Наличие файла или работающего агента ещё не подтверждает аутентификацию на VPS.");
            if (!String.IsNullOrWhiteSpace(Error))
            {
                sb.AppendLine();
                sb.AppendLine("Ошибка:");
                sb.AppendLine(Error);
            }

            sb.AppendLine();
            sb.AppendLine("Успешная обработка параметров не подтверждает наличие отдельного алиаса, DNS, доступность сервера или аутентификацию.");
            return sb.ToString();
        }
        private string InspectIdentity(string identity)
        {
            string status;
            return IdentityStatuses.TryGetValue(identity, out status) ? status : "не проверен";
        }
        internal readonly Dictionary<string, string> IdentityStatuses = new Dictionary<string, string>(StringComparer.Ordinal);
    }

    internal static class SshProfileDiagnostics
    {
        public static SshProfileDiagnosticResult Check(string target)
        {
            return Check(new SshProfileSetting { Target = (target ?? String.Empty).Trim() });
        }
        public static SshProfileDiagnosticResult Check(SshProfileSetting profile)
        {
            return Check(profile, CancellationToken.None);
        }
        internal static SshProfileDiagnosticResult Check(SshProfileSetting profile, CancellationToken token, string executable = null, int timeoutMs = 7000)
        {
            var result = CheckConfiguration(profile, token, executable, timeoutMs);
            if (String.IsNullOrWhiteSpace(result.Target)) return result;
            token.ThrowIfCancellationRequested();
            result.AgentService = SshAgentServiceStatus.Read(token);
            result.AgentState = result.AgentService.ToDisplay();
            result.AgentKeys = SshAgentDiagnostics.Keys(OpenSshClient.AgentExecutable(result.ExecutablePath), token);
            token.ThrowIfCancellationRequested(); return result;
        }

        // The same owned configuration phase is available without agent inventory.
        internal static SshProfileDiagnosticResult CheckConfiguration(SshProfileSetting profile, CancellationToken token,
            string executable = null, int timeoutMs = 7000, string configurationFile = null)
        {
            token.ThrowIfCancellationRequested();
            profile = profile == null ? null : profile.Clone();
            var result = new SshProfileDiagnosticResult
            {
                Target = profile == null ? "" : profile.Address,
                ConfigPath = configurationFile,
                OriginalHost = profile == null ? null : profile.IsDirect ? profile.Server :
                    (profile.Target ?? "").Substring((profile.Target ?? "").LastIndexOf('@') + 1)
            };

            if (String.IsNullOrWhiteSpace(result.Target))
            {
                result.Error = "SSH-профиль не выбран.";
                return result;
            }

            RunSshG(result, profile, token, executable ?? OpenSshClient.Executable, timeoutMs);
            token.ThrowIfCancellationRequested();
            foreach (var identity in result.IdentityFiles) {
                token.ThrowIfCancellationRequested(); result.IdentityStatuses[identity] = InspectIdentity(identity, result);
            }
            string firstStatus;
            result.KeyStatus = result.ResolvedIdentityFile != null && result.IdentityStatuses.TryGetValue(result.ResolvedIdentityFile, out firstStatus)
                ? firstStatus : InspectIdentity(null, result);
            token.ThrowIfCancellationRequested(); return result;
        }

        internal static string InspectKey(string value) { return InspectIdentity(value, null); }
        private static string InspectIdentity(string value, SshProfileDiagnosticResult context)
        {
            if (String.IsNullOrWhiteSpace(value)) return "Явный ключ не задан; SSH может использовать агент или стандартные ключи.";
            if (String.Equals(value, "none", StringComparison.OrdinalIgnoreCase)) return "Для этой записи файл ключа не используется (IdentityFile none).";
            string path;
            if (!TryResolveIdentityPath(value, context, out path))
                return "Не проверено: итоговый путь зависит от контекста OpenSSH; локальный файл не проверялся.";
            if (context != null) context.IdentityPaths[value] = path;
            try {
                if (!File.Exists(path)) return "Файл не найден. При переносе на другой ПК проверьте старый абсолютный путь или используйте ~/.ssh/имя_ключа.";
                using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)) { }
                return "Файл доступен для чтения. Содержимое ключа не считывалось.";
            } catch { return "Доступность файла не подтверждена. Проверьте путь и права текущего пользователя."; }
        }
        private static bool TryResolveIdentityPath(string value, SshProfileDiagnosticResult context, out string path)
        {
            path = null;
            // -G dumps these templates before client-specific home/environment expansion.
            const int maxPathLength = 32768;
            if (value.Length > maxPathLength || value[0] == '~' || value.Contains("${") || value.IndexOfAny(new[] { '\r', '\n', '\0' }) >= 0) return false;
            var expanded = new StringBuilder();
            for (int i = 0; i < value.Length; i++) {
                if (value[i] != '%') {
                    if (expanded.Length == maxPathLength) return false;
                    expanded.Append(value[i]); continue;
                }
                if (++i == value.Length) return false;
                string replacement;
                switch (value[i]) {
                    case '%': replacement = "%"; break;
                    case 'h': replacement = context == null ? null : context.ResolvedHostName; break;
                    case 'r': replacement = context == null ? null : context.ResolvedUser; break;
                    case 'p': replacement = context == null ? null : context.ResolvedPort; break;
                    case 'n': replacement = context == null ? null : context.OriginalHost; break;
                    default: return false;
                }
                if (String.IsNullOrEmpty(replacement) || replacement.Length > maxPathLength - expanded.Length ||
                    replacement.IndexOfAny(new[] { '\r', '\n', '\0' }) >= 0) return false;
                expanded.Append(replacement); // Replacement text is deliberately not expanded again.
            }
            try {
                var candidate = expanded.ToString();
                if (!Path.IsPathRooted(candidate)) return false;
                if (Path.DirectorySeparatorChar == '\\') {
                    // Root-relative, drive-relative, UNC and device paths need other context or network I/O.
                    if (candidate.Length < 3 || !((candidate[0] >= 'A' && candidate[0] <= 'Z') || (candidate[0] >= 'a' && candidate[0] <= 'z')) ||
                        candidate[1] != ':' || (candidate[2] != '\\' && candidate[2] != '/')) return false;
                }
                path = Path.GetFullPath(candidate); return true;
            } catch { return false; }
        }

        private static void RunSshG(SshProfileDiagnosticResult result, SshProfileSetting profile, CancellationToken token, string executable, int timeoutMs)
        {
            try
            {
                // OpenSSH evaluates Host/Match/Include; the shared runner owns the diagnostic tree.
                result.ExecutablePath = DiagnosticProcess.Resolve(executable);
                var config = result.ConfigPath == null ? "" : "-F " + SshConnection.Quote(result.ConfigPath) + " ";
                var captured = DiagnosticProcess.Run(result.ExecutablePath, "-G " + config + SshConnection.CommandArguments(profile), timeoutMs, token);
                result.SshAvailable = true;
                if (captured.ExitCode != 0) {
                    result.Error = "OpenSSH не смог прочитать профиль. Проверьте синтаксис SSH config и выбранные параметры. Код: " + captured.ExitCode + ".";
                    return;
                }
                if (captured.Truncated) { result.Error = "Вывод SSH превышает допустимый размер. Проверка не завершена."; return; }
                result.SshResolved = true; ParseSshG(captured.Output, result);
            }
            catch (OperationCanceledException) { throw; }
            catch (TimeoutException) { result.Error = "Проверка SSH превысила лимит времени. Её процессы остановлены; можно повторить."; }
            catch { result.Error = "Не удалось запустить OpenSSH. Проверьте наличие ssh.exe и права доступа к нему."; }
        }

        private static void ParseSshG(string text, SshProfileDiagnosticResult result)
        {
            if (String.IsNullOrEmpty(text)) return;

            foreach (var rawLine in text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                var line = rawLine.TrimStart();
                var space = line.IndexOf(' ');
                if (space <= 0) continue;

                var key = line.Substring(0, space).Trim();
                var value = line.Substring(space + 1);

                if (String.Equals(key, "hostname", StringComparison.OrdinalIgnoreCase)) result.ResolvedHostName = value;
                else if (String.Equals(key, "port", StringComparison.OrdinalIgnoreCase)) result.ResolvedPort = value;
                else if (String.Equals(key, "user", StringComparison.OrdinalIgnoreCase)) result.ResolvedUser = value;
                else if (String.Equals(key, "identityfile", StringComparison.OrdinalIgnoreCase)) {
                    if (String.IsNullOrWhiteSpace(result.ResolvedIdentityFile)) result.ResolvedIdentityFile = value;
                    if (!result.IdentityFiles.Contains(value)) {
                        if (result.IdentityFiles.Count < 32) result.IdentityFiles.Add(value);
                        else result.IdentityListLimited = true;
                    }
                }
                else if (String.Equals(key, "identityagent", StringComparison.OrdinalIgnoreCase)) result.IdentityAgent = value;
                else if (String.Equals(key, "identitiesonly", StringComparison.OrdinalIgnoreCase)) result.IdentitiesOnly = value;
            }
        }

    }
    // Read-only native service inspection. No elevation, service mutation or key access.
    internal static class SshAgentDiagnostics
    {
        internal static string Keys(string executable, CancellationToken token,
            Func<string, string, int, CancellationToken, DiagnosticProcessResult> run = null)
        {
            token.ThrowIfCancellationRequested();
            if (String.IsNullOrWhiteSpace(executable)) return "ssh-add.exe из этой установки OpenSSH не найден. Наличие ключей не проверено.";
            try {
                if (run == null) run = DiagnosticProcess.Run;
                var result = run(executable, "-l", 3000, token);
                token.ThrowIfCancellationRequested();
                if (result.Truncated) return "Ответ агента превышает допустимый размер. Наличие ключей не подтверждено.";
                if (result.ExitCode == 0) {
                    int count = (result.Output ?? "").Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).Length;
                    return count > 0 ? "Агент отвечает; ключей: " + count + ". Это ещё не подтверждает доступ к выбранному VPS."
                        : "Агент не вернул ключей. Загрузите нужный ключ командой ssh-add в терминале.";
                }
                return result.ExitCode == 1 ? "Агент не вернул доступных ключей. Загрузите нужный ключ через ssh-add."
                    : "ssh-add не получил ответ агента. Проверьте службу и SSH_AUTH_SOCK; фоновый SSH не запрашивает passphrase.";
            }
            catch (OperationCanceledException) { throw; }
            catch (TimeoutException) { return "Агент не ответил за 3 секунды. Проверка остановлена; можно повторить."; }
            catch { return "Не удалось проверить ключи агента. Проверьте ssh-add.exe и права доступа."; }
        }
        internal static string Describe(uint state, bool disabled)
        {
            return new SshAgentServiceSnapshot {
                RuntimeState = SshAgentServiceStatus.RuntimeState(state), RawRuntimeState = state,
                StartMode = disabled ? SshAgentStartMode.Disabled : SshAgentStartMode.Unknown
            }.ToDisplay();
        }
        internal static string Read()
        { return SshAgentServiceStatus.Read(CancellationToken.None).ToDisplay(); }
    }

}

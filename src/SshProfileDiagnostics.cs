using System;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace ProGo
{
    internal sealed class SshProfileDiagnosticResult
    {
        public string Target { get; set; }
        public bool LooksDirectTarget { get; set; }
        public bool FoundInConfig { get; set; }
        public string ConfigPath { get; set; }
        public string ResolvedHostName { get; set; }
        public string ResolvedUser { get; set; }
        public string ResolvedPort { get; set; }
        public string ResolvedIdentityFile { get; set; }
        internal readonly List<string> IdentityFiles = new List<string>();
        public string ExecutablePath { get; set; }
        public string AgentKeys { get; set; }
        public string IdentityAgent { get; set; }
        public string IdentitiesOnly { get; set; }
        public string AgentState { get; set; }
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
            sb.AppendLine("Что это:");
            if (LooksDirectTarget)
            {
                sb.AppendLine("- Похоже на прямой SSH target вида user@host.");
            }
            else
            {
                sb.AppendLine("- Похоже на алиас из ~/.ssh/config.");
            }

            sb.AppendLine();
            sb.AppendLine("Файл SSH config:");
            sb.AppendLine(ConfigPath);
            sb.AppendLine("Найден в config: " + (FoundInConfig ? "да" : "нет"));

            sb.AppendLine();
            sb.AppendLine("Проверка ssh.exe -G:");
            sb.AppendLine("ssh.exe доступен: " + (SshAvailable ? "да" : "не подтверждено"));
            sb.AppendLine("Исполняемый файл: " + (ExecutablePath ?? "не определён"));
            sb.AppendLine("профиль резолвится: " + (SshResolved ? "да" : "нет"));

            if (!String.IsNullOrWhiteSpace(ResolvedHostName)) sb.AppendLine("hostname: " + ResolvedHostName);
            if (!String.IsNullOrWhiteSpace(ResolvedPort)) sb.AppendLine("Порт SSH: " + ResolvedPort);
            if (!String.IsNullOrWhiteSpace(ResolvedUser)) sb.AppendLine("user: " + ResolvedUser);
            if (IdentityFiles.Count > 0) foreach (var identity in IdentityFiles) {
                sb.AppendLine("Файл ключа: " + identity);
                sb.AppendLine("  " + InspectIdentity(identity));
            }
            else sb.AppendLine("Файл ключа: " + (KeyStatus ?? "не проверен"));
            sb.AppendLine("ssh-agent: " + (AgentState ?? "не проверен"));
            sb.AppendLine("Ключи стандартного агента: " + (AgentKeys ?? "не проверены"));
            if (!String.IsNullOrWhiteSpace(IdentityAgent)) sb.AppendLine("IdentityAgent: " +
                (IdentityAgent == "none" ? "Использование агента отключено этим SSH-профилем." : "Указан отдельный агент; его доступность проверяется при подключении."));
            if (!String.IsNullOrWhiteSpace(IdentitiesOnly)) sb.AppendLine("IdentitiesOnly: " + IdentitiesOnly);
            sb.AppendLine("Для зашифрованного ключа: загрузите его через ssh-add в обычном терминале. Passphrase вводится только в OpenSSH.");
            sb.AppendLine("Если агент остановлен: с согласия владельца запустите Start-Service ssh-agent в PowerShell администратора.");
            sb.AppendLine("Если агент отключён: с согласия владельца выполните Set-Service ssh-agent -StartupType Manual, затем Start-Service ssh-agent. Без прав администратора обратитесь к владельцу ПК.");
            sb.AppendLine("Наличие файла или работающего агента ещё не подтверждает аутентификацию на VPS.");
            if (!String.IsNullOrWhiteSpace(Error))
            {
                sb.AppendLine();
                sb.AppendLine("Ошибка:");
                sb.AppendLine(Error);
            }

            sb.AppendLine();
            sb.AppendLine("Важно: эта проверка не подключается к серверу. Она проверяет, что ssh.exe понимает выбранный профиль.");
            return sb.ToString();
        }
        private string InspectIdentity(string identity)
        {
            string status;
            return IdentityStatuses.TryGetValue(identity, out status) ? status : "не проверен";
        }
        internal readonly Dictionary<string, string> IdentityStatuses = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
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
            token.ThrowIfCancellationRequested();
            var result = new SshProfileDiagnosticResult
            {
                Target = profile == null ? "" : profile.Address,
                ConfigPath = GetConfigPath()
            };

            if (String.IsNullOrWhiteSpace(result.Target))
            {
                result.Error = "SSH-профиль не выбран.";
                return result;
            }

            result.LooksDirectTarget = profile.IsDirect || LooksLikeDirectTarget(result.Target);
            result.FoundInConfig = IsTargetDeclaredInConfig(result.Target, result.ConfigPath);
            RunSshG(result, profile, token, executable ?? OpenSshClient.Executable, timeoutMs);
            token.ThrowIfCancellationRequested();
            result.AgentState = SshAgentDiagnostics.Read();
            result.KeyStatus = InspectKey(result.ResolvedIdentityFile);
            foreach (var identity in result.IdentityFiles) { token.ThrowIfCancellationRequested(); result.IdentityStatuses[identity] = InspectKey(identity); }
            result.AgentKeys = SshAgentDiagnostics.Keys(OpenSshClient.AgentExecutable(result.ExecutablePath), token);
            token.ThrowIfCancellationRequested(); return result;
        }

        internal static string InspectKey(string value)
        {
            if (String.IsNullOrWhiteSpace(value) || value == "none") return "Явный ключ не задан; SSH может использовать агент или стандартные ключи.";
            try {
                var path = SshConnection.KeyPath(value);
                if (!File.Exists(path)) return "Файл не найден. При переносе на другой ПК проверьте старый абсолютный путь или используйте ~/.ssh/имя_ключа.";
                using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)) { }
                return "Файл доступен для чтения. Содержимое ключа не считывалось.";
            } catch { return "Доступность файла не подтверждена. Проверьте путь и права текущего пользователя."; }
        }
        private static string GetConfigPath()
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, ".ssh", "config");
        }

        private static bool LooksLikeDirectTarget(string target)
        {
            if (String.IsNullOrWhiteSpace(target)) return false;
            if (target.IndexOf("@", StringComparison.Ordinal) >= 0) return true;
            if (Regex.IsMatch(target, @"^\d{1,3}(\.\d{1,3}){3}$")) return true;
            if (target.IndexOf(".", StringComparison.Ordinal) >= 0 && target.IndexOf(" ", StringComparison.Ordinal) < 0) return true;
            return false;
        }

        private static bool IsTargetDeclaredInConfig(string target, string configPath)
        {
            try
            {
                if (String.IsNullOrWhiteSpace(target)) return false;
                if (String.IsNullOrWhiteSpace(configPath) || !File.Exists(configPath)) return false;

                foreach (var rawLine in File.ReadAllLines(configPath))
                {
                    var line = rawLine.Trim();
                    if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)) continue;
                    if (!line.StartsWith("Host ", StringComparison.OrdinalIgnoreCase)) continue;

                    var hosts = line.Substring(5).Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var host in hosts)
                    {
                        if (String.Equals(host, target, StringComparison.OrdinalIgnoreCase)) return true;
                        if (HostPatternMatches(host, target)) return true;
                    }
                }
            }
            catch
            {
            }

            return false;
        }

        private static bool HostPatternMatches(string pattern, string target)
        {
            if (String.IsNullOrWhiteSpace(pattern) || String.IsNullOrWhiteSpace(target)) return false;
            if (pattern.IndexOf('*') < 0 && pattern.IndexOf('?') < 0) return false;

            var regex = "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
            return Regex.IsMatch(target, regex, RegexOptions.IgnoreCase);
        }

        private static void RunSshG(SshProfileDiagnosticResult result, SshProfileSetting profile, CancellationToken token, string executable, int timeoutMs)
        {
            try
            {
                // ssh.exe -G resolves configuration only; the shared runner owns this diagnostic tree.
                result.ExecutablePath = DiagnosticProcess.Resolve(executable);
                var captured = DiagnosticProcess.Run(result.ExecutablePath, "-G " + SshConnection.CommandArguments(profile), timeoutMs, token);
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
                var line = rawLine.Trim();
                var space = line.IndexOf(' ');
                if (space <= 0) continue;

                var key = line.Substring(0, space).Trim();
                var value = line.Substring(space + 1).Trim();

                if (String.Equals(key, "hostname", StringComparison.OrdinalIgnoreCase)) result.ResolvedHostName = value;
                else if (String.Equals(key, "port", StringComparison.OrdinalIgnoreCase)) result.ResolvedPort = value;
                else if (String.Equals(key, "user", StringComparison.OrdinalIgnoreCase)) result.ResolvedUser = value;
                else if (String.Equals(key, "identityfile", StringComparison.OrdinalIgnoreCase)) {
                    if (String.IsNullOrWhiteSpace(result.ResolvedIdentityFile)) result.ResolvedIdentityFile = value;
                    if (result.IdentityFiles.Count < 32 && !result.IdentityFiles.Contains(value)) result.IdentityFiles.Add(value);
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
            if (state == 4) return "Running — запущен; загрузка нужного ключа ещё не проверена.";
            if (disabled) return "Disabled — отключён; включение требует согласия владельца и прав администратора.";
            if (state == 1) return "Stopped — остановлен; запуск требует согласия владельца и прав администратора.";
            return "Служба меняет состояние; повторите проверку.";
        }
        internal static string Read()
        {
            IntPtr manager = IntPtr.Zero, service = IntPtr.Zero;
            try {
                manager = OpenSCManager(null, null, 1);
                if (manager == IntPtr.Zero) return "Не удалось прочитать состояние; проверьте права пользователя.";
                service = OpenService(manager, "ssh-agent", 4);
                if (service == IntPtr.Zero) return Marshal.GetLastWin32Error() == 1060 ? "Служба отсутствует; проверьте установку Windows OpenSSH." : "Состояние не подтверждено; проверьте права пользователя.";
                Status status; if (!QueryServiceStatus(service, out status)) return "Не удалось прочитать состояние службы.";
                bool disabled = false;
                using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\ssh-agent"))
                    if (key != null) disabled = Object.Equals(key.GetValue("Start"), 4);
                return Describe(status.State, disabled);
            } catch { return "Состояние агента не подтверждено; проверьте службу Windows OpenSSH."; }
            finally { if (service != IntPtr.Zero) CloseServiceHandle(service); if (manager != IntPtr.Zero) CloseServiceHandle(manager); }
        }
        [StructLayout(LayoutKind.Sequential)] private struct Status { internal uint Type, State, Controls, ExitCode, ServiceExitCode, Checkpoint, WaitHint; }
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern IntPtr OpenSCManager(string machine, string database, uint access);
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern IntPtr OpenService(IntPtr manager, string name, uint access);
        [DllImport("advapi32.dll", SetLastError = true)] private static extern bool QueryServiceStatus(IntPtr service, out Status status);
        [DllImport("advapi32.dll")] private static extern bool CloseServiceHandle(IntPtr handle);
    }

}

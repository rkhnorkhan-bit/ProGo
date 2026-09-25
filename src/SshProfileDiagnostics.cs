using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

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
        public string ResolvedIdentityFile { get; set; }
        public bool SshAvailable { get; set; }
        public bool SshResolved { get; set; }
        public string Error { get; set; }

        public string ToReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("SSH target: " + (Target ?? ""));
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
            sb.AppendLine("ssh.exe доступен: " + (SshAvailable ? "да" : "нет"));
            sb.AppendLine("профиль резолвится: " + (SshResolved ? "да" : "нет"));

            if (!String.IsNullOrWhiteSpace(ResolvedHostName)) sb.AppendLine("hostname: " + ResolvedHostName);
            if (!String.IsNullOrWhiteSpace(ResolvedUser)) sb.AppendLine("user: " + ResolvedUser);
            if (!String.IsNullOrWhiteSpace(ResolvedIdentityFile)) sb.AppendLine("identityfile: " + ResolvedIdentityFile);

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
    }

    internal static class SshProfileDiagnostics
    {
        public static SshProfileDiagnosticResult Check(string target)
        {
            var result = new SshProfileDiagnosticResult
            {
                Target = (target ?? String.Empty).Trim(),
                ConfigPath = GetConfigPath()
            };

            if (String.IsNullOrWhiteSpace(result.Target))
            {
                result.Error = "SSH-профиль не выбран.";
                return result;
            }

            result.LooksDirectTarget = LooksLikeDirectTarget(result.Target);
            result.FoundInConfig = IsTargetDeclaredInConfig(result.Target, result.ConfigPath);
            RunSshG(result);
            return result;
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

        private static void RunSshG(SshProfileDiagnosticResult result)
        {
            try
            {
                var psi = new ProcessStartInfo("ssh.exe", "-G " + QuoteArg(result.Target))
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    if (process == null)
                    {
                        result.Error = "Не удалось запустить ssh.exe.";
                        return;
                    }

                    result.SshAvailable = true;

                    var output = process.StandardOutput.ReadToEnd();
                    var error = process.StandardError.ReadToEnd();
                    if (!process.WaitForExit(7000))
                    {
                        try { process.Kill(); }
                        catch { }
                        result.Error = "ssh.exe -G не завершился за 7 секунд.";
                        return;
                    }

                    if (process.ExitCode != 0)
                    {
                        result.Error = String.IsNullOrWhiteSpace(error) ? ("ssh.exe -G завершился с кодом " + process.ExitCode + ".") : error.Trim();
                        return;
                    }

                    result.SshResolved = true;
                    ParseSshG(output, result);
                }
            }
            catch (Exception ex)
            {
                result.Error = ex.GetType().Name + ": " + ex.Message;
            }
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
                else if (String.Equals(key, "user", StringComparison.OrdinalIgnoreCase)) result.ResolvedUser = value;
                else if (String.Equals(key, "identityfile", StringComparison.OrdinalIgnoreCase) && String.IsNullOrWhiteSpace(result.ResolvedIdentityFile)) result.ResolvedIdentityFile = value;
            }
        }

        private static string QuoteArg(string value)
        {
            if (value == null) return "\"\"";
            if (value.IndexOfAny(new[] { ' ', '\t', '"' }) < 0) return value;
            return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        private static string ConfigQuoted(string value) { return "\"" + value.Replace('\\', '/') + "\""; }
        private static string OracleOption(string text, string key)
        {
            foreach (string line in text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                if (line.StartsWith(key + " ", StringComparison.Ordinal)) return line.Substring(key.Length + 1);
            throw new Exception("Native ssh -G oracle omitted " + key);
        }
        private static string[] OracleIdentities(string text)
        { return text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).Where(s => s.StartsWith("identityfile ", StringComparison.Ordinal)).Select(s => s.Substring(13)).ToArray(); }
        private static string SafeConfigOracleError(string value, string fixtureDirectory)
        {
            value = (value ?? "").Replace('\\', '/');
            var paths = new[] { fixtureDirectory, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) };
            for (int i = 0; i < paths.Length; i++) {
                string path = (paths[i] ?? "").Replace('\\', '/').TrimEnd('/');
                if (path.Length == 0) continue;
                value = System.Text.RegularExpressions.Regex.Replace(value, System.Text.RegularExpressions.Regex.Escape(path),
                    i == 0 ? "[fixture]" : "[user-profile]", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant);
            }
            var safe = new StringBuilder();
            foreach (char character in value) {
                if (safe.Length == 1024) break;
                safe.Append(Char.IsControl(character) || character == '\u2028' || character == '\u2029' ? ' ' : character);
            }
            return safe.ToString();
        }
        private static SshProfileDiagnosticResult ConfigMatchesNative(string file, string target, string host, string user, string port, out string[] identities)
        {
            // Independent argv and output extraction: no production configuration
            // parser, profile argument builder, owner config or agent is involved.
            var oracle = DiagnosticProcess.Run(OpenSshClient.Executable, "-G -F " + ConfigQuoted(file) + " -o CanonicalizeHostname=no " + target, 7000, CancellationToken.None);
            if (oracle.ExitCode != 0 || oracle.Truncated)
                Console.WriteLine("INFO: isolated ssh-G oracle exit=" + oracle.ExitCode + "; truncated=" + oracle.Truncated +
                    "; stdoutLength=" + (oracle.Output ?? "").Length + "; stderrLength=" + (oracle.Error ?? "").Length +
                    "; stderr=" + SafeConfigOracleError(oracle.Error, Path.GetDirectoryName(file)));
            Check(oracle.ExitCode == 0 && !oracle.Truncated, "isolated native OpenSSH resolves its configuration oracle: " + target);
            var actual = SshProfileDiagnostics.CheckConfiguration(new SshProfileSetting { Target = target }, CancellationToken.None, OpenSshClient.Executable, 7000, file);
            Check(actual.SshAvailable && actual.SshResolved && String.IsNullOrEmpty(actual.Error), "production SSH configuration check accepts the same isolated native profile: " + target);
            Check(actual.ResolvedHostName == OracleOption(oracle.Output, "hostname") && actual.ResolvedUser == OracleOption(oracle.Output, "user") && actual.ResolvedPort == OracleOption(oracle.Output, "port") &&
                actual.ResolvedHostName == host && actual.ResolvedUser == user && actual.ResolvedPort == port,
                "effective SSH host user and port match native first-value Include and Match semantics: " + target);
            Check(actual.ConfigPath == file && String.IsNullOrEmpty(actual.AgentState) && String.IsNullOrEmpty(actual.AgentKeys), "configuration-only diagnostics use the explicit file and do not inventory an agent: " + target);
            identities = OracleIdentities(oracle.Output); return actual;
        }
        private static void SshConfigAuthority()
        {
            string root = Path.Combine(work, "ssh-config-authority-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
            try {
                string parts = Path.Combine(root, "parts"); Directory.CreateDirectory(parts);
                File.WriteAllText(Path.Combine(parts, "00-first.conf"), "Host fixture.dotted\n HostName 127.0.0.11\n User first_user\n Port 2201\nHost fixture-*\n User wildcard_user\n Port 2202\nHost fixture-* !fixture-closed\n HostName 127.0.0.12\nMatch originalhost match-only\n HostName 127.0.0.13\n User match_user\n Port 2203\nHost *\n", new UTF8Encoding(false));
                File.WriteAllText(Path.Combine(parts, "10-later.conf"), "Host fixture.dotted\n HostName 127.0.0.99\n User later_user\n Port 2299\nHost fixture-closed\n HostName 127.0.0.14\n", new UTF8Encoding(false));
                string file = Path.Combine(root, "config");
                File.WriteAllText(file, "Include " + ConfigQuoted(Path.Combine(parts, "*.conf")) + "\nHost *\n CanonicalizeHostname no\n User default_user\n Port 2244\n IdentityFile none\n IdentityAgent none\n", new UTF8Encoding(false));
                string[][] cases = { new[] { "fixture.dotted", "127.0.0.11", "first_user", "2201" }, new[] { "fixture-one", "127.0.0.12", "wildcard_user", "2202" },
                    new[] { "fixture-closed", "127.0.0.14", "wildcard_user", "2202" }, new[] { "match-only", "127.0.0.13", "match_user", "2203" },
                    new[] { "unknown-progo.invalid", "unknown-progo.invalid", "default_user", "2244" } };
                foreach (var item in cases) {
                    string[] identities; var result = ConfigMatchesNative(file, item[0], item[1], item[2], item[3], out identities);
                    Check(!result.ToReport().Contains("Найден в config:") && !result.ToReport().Contains("Похоже на прямой SSH target"), "dotted and unknown targets do not invent an alias declaration or direct-host classification: " + item[0]);
                }
                // User Include paths are relative to ~/.ssh, not adjacent to -F.
                // A unique leaf cannot collide with an existing owner include.
                string leaf = "progo-adjacent-" + Guid.NewGuid().ToString("N") + ".conf";
                File.WriteAllText(Path.Combine(root, leaf), "Host relative.test\n HostName 127.0.0.99\n Port 2299\n", new UTF8Encoding(false));
                File.WriteAllText(file, "Include " + leaf + "\nHost *\n CanonicalizeHostname no\n HostName 127.0.0.21\n User relative_user\n Port 2211\n IdentityFile none\n IdentityAgent none\n", new UTF8Encoding(false));
                string[] relativeIdentities; ConfigMatchesNative(file, "relative.test", "127.0.0.21", "relative_user", "2211", out relativeIdentities);
                SshIdentitySafety(root);
            } finally { Directory.Delete(root, true); }
        }
        private static void SshIdentitySafety(string root)
        {
            const string secret = "PRIVATE_FIXTURE_BYTES_MUST_NOT_APPEAR";
            string file = Path.Combine(root, "identities.conf"), prefix = root.Replace('\\', '/') + "/";
            var raw = new[] { prefix + "absolute-key", prefix + "id_%h_%r_%p", prefix + "literal%%h", prefix + "original_%n", prefix + "case_%h", prefix + "case_%H", "none",
                "~/.ssh/progo-never-read", prefix + "unknown_%d", prefix + "unknown_%D", prefix + "unknown_%q", "relative-progo-key", prefix + "unknown_${PROGO_NEVER_EXPAND}" };
            var paths = new[] { Path.Combine(root, "absolute-key"), Path.Combine(root, "id_127.0.0.21_outer@fixture_2227"), Path.Combine(root, "literal%h"), Path.Combine(root, "original_Identity.Case"), Path.Combine(root, "case_127.0.0.21") };
            foreach (string path in paths) File.WriteAllText(path, secret);
            // Existing literal token filenames catch a false filesystem-positive
            // result when a template has not actually been resolved.
            foreach (int i in new[] { 5, 8, 9, 10, 12 }) File.WriteAllText(raw[i], secret);
            string body = "Host *\n HostName 127.0.0.21\n User ignored_config_user\n Port 2227\n CanonicalizeHostname no\n IdentityAgent none\n";
            foreach (string identity in raw) body += " IdentityFile " + ConfigQuoted(identity) + "\n";
            File.WriteAllText(file, body, new UTF8Encoding(false));
            string[] native; var actual = ConfigMatchesNative(file, "outer@fixture@Identity.Case", "127.0.0.21", "outer@fixture", "2227", out native);
            Check(actual.IdentityFiles.SequenceEqual(native) && native.SequenceEqual(raw) && !actual.IdentityListLimited, "all raw IdentityFile values and native order survive multiple paths none and templates");
            for (int i = 0; i < paths.Length; i++) {
                string proven;
                Check(actual.IdentityPaths.TryGetValue(raw[i], out proven) && String.Equals(Path.GetFullPath(proven), Path.GetFullPath(paths[i]), StringComparison.Ordinal) && actual.IdentityStatuses[raw[i]].Contains("доступен"),
                    "confirmed identity path follows native context and one-pass token expansion without reading key bytes: " + i);
            }
            for (int i = 5; i < raw.Length; i++) {
                Check(!actual.IdentityPaths.ContainsKey(raw[i]) && (i == 6 ? actual.IdentityStatuses[raw[i]].Contains("IdentityFile none") : actual.IdentityStatuses[raw[i]].StartsWith("Не проверено", StringComparison.OrdinalIgnoreCase)),
                    "none home environment and unknown identity templates never become guessed filesystem paths: " + i);
            }
            Check(actual.IdentityStatuses.Count == raw.Length && actual.IdentityStatuses[raw[4]] != actual.IdentityStatuses[raw[5]], "case-sensitive raw identity status maps distinguish supported percent-h from unsupported percent-H");
            Check(!actual.ToReport().Contains(secret) && actual.ToReport().Contains("IdentityFile (OpenSSH):") && actual.ToReport().Contains("Проверяемый путь:"), "identity report separates raw configuration and confirmed paths without fixture key contents");
            foreach (string value in new[] { "C:progo-drive-relative", "\\progo-root-relative", "relative-progo-key" })
                Check(SshProfileDiagnostics.InspectKey(value).StartsWith("Не проверено", StringComparison.OrdinalIgnoreCase), "drive-relative root-relative and client-relative identities stay unconfirmed: " + value);
            body = "Host limited.test\n HostName 127.0.0.22\n User limited_user\n Port 2228\n CanonicalizeHostname no\n IdentityAgent none\n";
            for (int i = 0; i < 35; i++) body += " IdentityFile " + ConfigQuoted(prefix + "limited-" + i) + "\n";
            File.WriteAllText(file, body, new UTF8Encoding(false));
            var limited = ConfigMatchesNative(file, "limited.test", "127.0.0.22", "limited_user", "2228", out native);
            Check(native.Length == 35 && limited.IdentityFiles.SequenceEqual(native.Take(32)) && limited.IdentityListLimited && limited.IdentityStatuses.Count == 32,
                "identity diagnosis preserves its first thirty-two native entries and explicitly marks the remaining list incomplete");
            var watch = Stopwatch.StartNew();
            var expanded = SshProfileDiagnostics.CheckConfiguration(new SshProfileSetting { Target = "fixture-expanded-identity" }, CancellationToken.None, Application.ExecutablePath, 1500);
            Check(expanded.SshResolved && expanded.IdentityFiles.Count == 1 && watch.ElapsedMilliseconds < 3000, "controlled oversized identity expansion completes the bounded diagnostic pipeline");
            Check(expanded.IdentityPaths.Count == 0 && expanded.IdentityStatuses[expanded.IdentityFiles[0]].StartsWith("Не проверено", StringComparison.OrdinalIgnoreCase),
                "oversized expanded identity remains unconfirmed instead of becoming a filesystem probe");
        }
    }
}

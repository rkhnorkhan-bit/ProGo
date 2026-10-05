using System;
using System.IO;
using System.Text;
using System.Linq;

namespace ProGo
{
    internal static class DiagnosticReportTests
    {
        private static int passed;
        private static void Check(bool value, string name) { if (!value) throw new Exception(name); passed++; Console.WriteLine("PASS: " + name); }
        private static int Main()
        {
            string root = Path.Combine(Path.GetTempPath(), "ProGo-diagnostic-fixture-" + Guid.NewGuid().ToString("N"));
            try
            {
                var empty = DiagnosticReport.Build(root, "0.2.2");
                Check(!Directory.Exists(root), "report creation never creates missing application state");
                Check(empty.Text.Contains("Нет доступного файла.") && empty.Text.Contains("0.2.2"), "missing logs have an honest fixed status");
                Directory.CreateDirectory(root);
                string path = Path.Combine(root, "progo.log");
                string[] privateValues = { "fixture-password-873", "pgv1.fixture-invitation", "fixture-bearer-789", "192.0.2.88", "2001:db8::99",
                    "server.fixture.invalid", "person@fixture.invalid", Path.Combine(root, "private folder", "client.key"),
                    "Fixture Private Name", "arbitrary-unknown-secret-943", "not-a-real-key", "session-cookie-fixture", "c2VjcmV0LWZpeHR1cmU=" };
                string secrets = String.Join(" | ", privateValues);
                string raw = "2026-01-02 03:04:05 +03:00 [ERROR] Failed to start SSH tunnel. | Password=" + secrets + "\n" +
                    "2026-01-02 03:04:06 +03:00 [INFO] Application HTTP proxy started. endpoint=https://" + privateValues[5] + "/" + privateValues[1] + "\n" +
                    "authorization: Bearer " + privateValues[2] + "\n-----BEGIN PRIVATE KEY-----\nnot-a-real-key\n-----END PRIVATE KEY-----\n" +
                    "unknown format " + secrets + "\n" +
                    "2026-01-02 03:04:07 +03:00 [ERROR] User action failed: " + secrets;
                File.WriteAllText(path, raw, new UTF8Encoding(true));
                string report = DiagnosticReport.Build(root, "0.2.2").Text;
                Check(report.Contains("SSH_START_FAILED") && report.Contains("HTTP_PROXY_START") && report.Contains("USER_ACTION_FAILED"), "known events survive as fixed diagnostic codes");
                Check(report.Contains("2026-01-02 00:04:05 UTC"), "validated timestamp is normalized to UTC");
                foreach (string secret in privateValues) Check(!report.Contains(secret), "private fixture value excluded (case " + Array.IndexOf(privateValues, secret) + ")");
                Check(!report.Contains("BEGIN PRIVATE KEY") && !report.Contains("authorization:"), "multiline keys and arbitrary raw lines are not exported");
                Check(File.ReadAllText(path) == raw, "report preparation preserves the original local log");
                Check(!DiagnosticReport.Build(root, secrets).Text.Contains(privateValues[0]), "untrusted version cannot inject report content");
                Check(DiagnosticReport.Build(root, "1.2.3\ninjected").Text.Contains("Версия: неизвестна"), "version allowlist rejects multiline values");

                string legacy = Path.Combine(root, "progo-update.log"), update = Path.Combine(root, "update.log");
                File.WriteAllText(legacy, "TRANSACTION FAILED: " + secrets);
                Check(DiagnosticReport.Build(root, "0.2.2").Text.Contains("UPDATE_FAILED"), "legacy updater log is projected when current is absent");
                File.WriteAllText(update, "ProGo transactional update completed. " + secrets);
                report = DiagnosticReport.Build(root, "0.2.2").Text;
                Check(report.Contains("UPDATE_COMPLETE") && !report.Contains("UPDATE_FAILED"), "active update log takes precedence over legacy content");
                File.WriteAllText(update + ".1", "Package SHA-256 verified: " + secrets);
                File.WriteAllText(update + ".2", "Downloading published package: " + secrets);
                File.WriteAllText(Path.Combine(root, "progo-restore.log"), "RESTORE FAILED: " + secrets);
                report = DiagnosticReport.Build(root, "0.2.2").Text;
                Check(report.Contains("UPDATE_HASH_OK") && report.Contains("UPDATE_DOWNLOAD") && report.Contains("RESTORE_FAILED"), "bounded archives and restore events join the report");

                File.WriteAllText(path, String.Join("\n", Enumerable.Range(0, 500).Select(i => "SOCKS listener is ready. " + secrets)));
                report = DiagnosticReport.Build(root, "0.2.2").Text;
                Check(report.Split(new[] { "SOCKS_READY" }, StringSplitOptions.None).Length - 1 == DiagnosticReport.EventsPerSource, "per-source event history is bounded to recent entries");
                Check(report.Length < 40000 && report.Contains("64 КиБ"), "large source yields a bounded report with truncation notice");
                using (var file = new FileStream(path, FileMode.Create, FileAccess.Write)) {
                    file.SetLength(8 * 1024 * 1024); file.Position = file.Length;
                    byte[] ending = Encoding.UTF8.GetBytes("\nSOCKS start requested. " + secrets); file.Write(ending, 0, ending.Length);
                }
                report = DiagnosticReport.Build(root, "0.2.2").Text;
                Check(report.Contains("SOCKS_START") && !report.Contains("\0"), "huge historical source drops partial first line and retains a complete final event");
                using (var file = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                    Check(DiagnosticReport.Build(root, "0.2.2").Text.Contains("Файл недоступен."), "locked log becomes sanitized unavailable status");
                Check(!DiagnosticReport.Build(root, "0.2.2").Text.Contains(root), "root and exception paths never enter the final report");
                Console.WriteLine("Diagnostic report tests PASS: " + passed); return 0;
            }
            catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }
    }
}

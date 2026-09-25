using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace ProGo
{
    internal sealed class BackupInfo
    {
        public string Path { get; set; }
        public string DisplayName { get; set; }
        public string Version { get; set; }
        public string TargetVersion { get; set; }
        public string Reason { get; set; }
        public string CreatedBy { get; set; }
        public string Result { get; set; }
        public string Kind { get; set; }
        public string Created { get; set; }
        public DateTime LastWriteTime { get; set; }

        public bool IsManual
        {
            get { return String.Equals(CreatedBy, "manual", StringComparison.OrdinalIgnoreCase) || String.Equals(Result, "manual", StringComparison.OrdinalIgnoreCase); }
        }

        public bool IsBaseline
        {
            get { return String.Equals(Kind, "baseline", StringComparison.OrdinalIgnoreCase) || String.Equals(Result, "baseline", StringComparison.OrdinalIgnoreCase); }
        }

        public bool IsPreUpdate
        {
            get { return String.Equals(Kind, "pre-update", StringComparison.OrdinalIgnoreCase) || ReasonText.IndexOf("update", StringComparison.OrdinalIgnoreCase) >= 0; }
        }

        public string ReasonText
        {
            get { return Reason ?? String.Empty; }
        }
    }

    internal sealed class BackupCleanupResult
    {
        public int Deleted { get; set; }
        public int Kept { get; set; }
        public int Failed { get; set; }
        public string Message { get; set; }
    }

    internal static class BackupService
    {
        private const int MaxAutomaticBackups = 10;

        public static string BackupsRoot
        {
            get { return System.IO.Path.Combine(AppPaths.Root, "backups"); }
        }

        public static string CurrentVersion
        {
            get
            {
                try
                {
                    var file = System.IO.Path.Combine(AppPaths.Root, "VERSION");
                    if (File.Exists(file)) return File.ReadAllText(file).Trim();
                }
                catch
                {
                }

                return "0.0.0";
            }
        }

        public static void EnsureVersionBackupExists(string reason)
        {
            try
            {
                var version = CurrentVersion;
                if (HasBackupForVersion(version)) return;

                var dir = CreateBackup(reason);
                SafeLog.Info("Version baseline backup created: " + dir + ".");
            }
            catch (Exception ex)
            {
                SafeLog.Error("Version baseline backup failed.", ex);
            }
        }

        public static bool HasBackupForVersion(string version)
        {
            foreach (var backup in ListBackups())
            {
                if (String.Equals(backup.Version, version, StringComparison.OrdinalIgnoreCase) && backup.IsBaseline) return true;
            }

            return false;
        }

        public static string CreateBackup(string reason)
        {
            AppPaths.EnsureDirectories();
            Directory.CreateDirectory(BackupsRoot);

            var version = CurrentVersion;
            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var safeVersion = Sanitize(version);
            var kind = ClassifyKind(reason, String.Empty);
            var result = kind == "baseline" ? "baseline" : (kind == "manual" ? "manual" : "created");
            var createdBy = kind == "manual" ? "manual" : "app";
            var backupDir = System.IO.Path.Combine(BackupsRoot, "backup-" + timestamp + "-v" + safeVersion);

            var suffix = 1;
            var original = backupDir;
            while (Directory.Exists(backupDir))
            {
                backupDir = original + "-" + suffix;
                suffix++;
            }

            Directory.CreateDirectory(backupDir);

            CopyFileIfExists("ProGo.exe", backupDir);
            CopyFileIfExists("ProGo.ico", backupDir);
            CopyFileIfExists("VERSION", backupDir);
            CopyFileIfExists("vault.enc.json", backupDir);
            CopyFileIfExists("settings.json", backupDir);
            CopyFileIfExists("progo.log", backupDir);
            CopyFileIfExists("update.log", backupDir);
            CopyFileIfExists("progo-update.log", backupDir);
            CopyDirectoryIfExists(System.IO.Path.Combine(AppPaths.Root, "scripts"), System.IO.Path.Combine(backupDir, "scripts"));

            WriteManifest(backupDir, version, String.Empty, reason, createdBy, result, kind);

            CleanupOldBackups(false);
            SafeLog.Info("Backup created: " + backupDir + ".");
            return backupDir;
        }

        public static List<BackupInfo> ListBackups()
        {
            var result = new List<BackupInfo>();
            if (!Directory.Exists(BackupsRoot)) return result;

            var dirs = Directory.GetDirectories(BackupsRoot);
            Array.Sort(dirs);
            Array.Reverse(dirs);

            foreach (var dir in dirs)
            {
                result.Add(ReadBackupInfo(dir));
            }

            return result;
        }

        public static BackupCleanupResult CleanupOldBackups(bool includeSummary)
        {
            Directory.CreateDirectory(BackupsRoot);
            var backups = ListBackups();
            var keep = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var backup in backups)
            {
                if (backup.IsManual) keep.Add(backup.Path);
            }

            var latestBaseline = FirstOrNull(backups, delegate(BackupInfo b) { return b.IsBaseline; });
            if (latestBaseline != null) keep.Add(latestBaseline.Path);

            var latestPreUpdate = FirstOrNull(backups, delegate(BackupInfo b) { return b.IsPreUpdate; });
            if (latestPreUpdate != null) keep.Add(latestPreUpdate.Path);

            var automaticKept = 0;
            foreach (var backup in backups)
            {
                if (backup.IsManual) continue;
                if (automaticKept < MaxAutomaticBackups)
                {
                    keep.Add(backup.Path);
                    automaticKept++;
                }
            }

            var deleted = 0;
            var failed = 0;
            foreach (var backup in backups)
            {
                if (keep.Contains(backup.Path)) continue;

                try
                {
                    Directory.Delete(backup.Path, true);
                    deleted++;
                }
                catch (Exception ex)
                {
                    failed++;
                    SafeLog.Error("Backup cleanup failed: " + backup.Path + ".", ex);
                }
            }

            var output = new BackupCleanupResult
            {
                Deleted = deleted,
                Kept = keep.Count,
                Failed = failed,
                Message = "Удалено: " + deleted + "; сохранено: " + keep.Count + "; ошибок: " + failed + "."
            };

            if (includeSummary) SafeLog.Info("Backup cleanup finished. " + output.Message);
            return output;
        }

        public static bool StartRestore(string backupDir)
        {
            try
            {
                var script = System.IO.Path.Combine(AppPaths.Root, "scripts", "Restore-ProGoBackup.ps1");
                if (!File.Exists(script))
                {
                    MessageBox.Show(
                        "Скрипт отката не найден. Обновите ProGo из GitHub один раз после исправления rollback-контура.",
                        "Откат ProGo",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return false;
                }

                if (String.IsNullOrEmpty(backupDir) || !Directory.Exists(backupDir))
                {
                    MessageBox.Show("Резервная копия не найдена.", "Откат ProGo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                var powershell = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "WindowsPowerShell", "v1.0", "powershell.exe");
                if (!File.Exists(powershell)) powershell = "powershell.exe";

                var currentPid = Process.GetCurrentProcess().Id;
                var args = "-NoProfile -ExecutionPolicy Bypass -File \"" + script + "\" -BackupDir \"" + backupDir + "\" -WaitPid " + currentPid;

                var psi = new ProcessStartInfo(powershell, args)
                {
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Minimized,
                    WorkingDirectory = AppPaths.Root
                };

                Process.Start(psi);
                return true;
            }
            catch (Exception ex)
            {
                SafeLog.Error("Restore launch failed.", ex);
                MessageBox.Show("Не удалось запустить откат. Подробности записаны в журнал.", "Откат ProGo", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private static BackupInfo ReadBackupInfo(string dir)
        {
            var info = new BackupInfo
            {
                Path = dir,
                LastWriteTime = Directory.GetLastWriteTime(dir),
                Version = ReadManifestValue(dir, "version"),
                TargetVersion = ReadManifestValue(dir, "target_version"),
                Reason = ReadManifestValue(dir, "reason"),
                CreatedBy = ReadManifestValue(dir, "created_by"),
                Result = ReadManifestValue(dir, "update_result"),
                Kind = ReadManifestValue(dir, "backup_kind"),
                Created = ReadManifestValue(dir, "created")
            };

            if (String.IsNullOrEmpty(info.Version)) info.Version = InferVersionFromName(dir);
            if (String.IsNullOrEmpty(info.TargetVersion)) info.TargetVersion = InferTargetVersionFromName(dir);
            if (String.IsNullOrEmpty(info.Reason)) info.Reason = InferReasonFromName(dir);
            if (String.IsNullOrEmpty(info.Kind)) info.Kind = ClassifyKind(info.Reason, info.TargetVersion);
            if (String.IsNullOrEmpty(info.CreatedBy)) info.CreatedBy = info.Kind == "manual" ? "manual" : "unknown";
            if (String.IsNullOrEmpty(info.Result)) info.Result = InferResult(info.Kind, info.Reason);
            if (String.IsNullOrEmpty(info.Created)) info.Created = info.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");

            info.DisplayName = FormatDisplayName(info);
            return info;
        }

        private static string FormatDisplayName(BackupInfo info)
        {
            var name = System.IO.Path.GetFileName(info.Path);
            var target = String.IsNullOrEmpty(info.TargetVersion) || info.TargetVersion == "unknown" ? String.Empty : " → " + info.TargetVersion;
            var status = String.IsNullOrEmpty(info.Result) ? "unknown" : info.Result;
            var kind = String.IsNullOrEmpty(info.Kind) ? "backup" : info.Kind;
            return name + " | " + kind + " | v" + info.Version + target + " | " + status + " | " + info.Created;
        }

        private static BackupInfo FirstOrNull(List<BackupInfo> backups, Predicate<BackupInfo> predicate)
        {
            foreach (var backup in backups)
            {
                if (predicate(backup)) return backup;
            }
            return null;
        }

        private static void WriteManifest(string backupDir, string version, string targetVersion, string reason, string createdBy, string updateResult, string backupKind)
        {
            var manifest = new StringBuilder();
            manifest.AppendLine("product=ProGo");
            manifest.AppendLine("version=" + SafeManifest(version));
            manifest.AppendLine("target_version=" + SafeManifest(targetVersion));
            manifest.AppendLine("created=" + DateTimeOffset.Now.ToString("o"));
            manifest.AppendLine("reason=" + SafeManifest(reason));
            manifest.AppendLine("created_by=" + SafeManifest(createdBy));
            manifest.AppendLine("update_result=" + SafeManifest(updateResult));
            manifest.AppendLine("backup_kind=" + SafeManifest(backupKind));
            manifest.AppendLine("contains=ProGo.exe,ProGo.ico,VERSION,scripts,vault.enc.json,settings.json,progo.log,update.log,progo-update.log");
            File.WriteAllText(System.IO.Path.Combine(backupDir, "manifest.txt"), manifest.ToString(), Encoding.UTF8);
        }

        private static string ClassifyKind(string reason, string targetVersion)
        {
            var text = (reason ?? String.Empty).ToLowerInvariant();
            if (text.IndexOf("manual", StringComparison.OrdinalIgnoreCase) >= 0) return "manual";
            if (text.IndexOf("baseline", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("startup", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("version", StringComparison.OrdinalIgnoreCase) >= 0) return "baseline";
            if (!String.IsNullOrEmpty(targetVersion)) return "pre-update";
            if (text.IndexOf("update", StringComparison.OrdinalIgnoreCase) >= 0) return "pre-update";
            return "automatic";
        }

        private static string InferResult(string kind, string reason)
        {
            if (String.Equals(kind, "manual", StringComparison.OrdinalIgnoreCase)) return "manual";
            if (String.Equals(kind, "baseline", StringComparison.OrdinalIgnoreCase)) return "baseline";
            if (String.Equals(kind, "pre-update", StringComparison.OrdinalIgnoreCase)) return "pre-update";
            if ((reason ?? String.Empty).IndexOf("failed", StringComparison.OrdinalIgnoreCase) >= 0) return "failed";
            return "created";
        }

        private static string InferReasonFromName(string dir)
        {
            var name = System.IO.Path.GetFileName(dir) ?? String.Empty;
            if (name.IndexOf("-to-v", StringComparison.OrdinalIgnoreCase) >= 0) return "before-update";
            return "legacy-backup";
        }

        private static string InferVersionFromName(string dir)
        {
            var name = System.IO.Path.GetFileName(dir) ?? String.Empty;
            var marker = "-v";
            var index = name.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index < 0) return "unknown";
            var value = name.Substring(index + marker.Length);
            var to = value.IndexOf("-to-v", StringComparison.OrdinalIgnoreCase);
            if (to >= 0) value = value.Substring(0, to);
            return String.IsNullOrEmpty(value) ? "unknown" : value;
        }

        private static string InferTargetVersionFromName(string dir)
        {
            var name = System.IO.Path.GetFileName(dir) ?? String.Empty;
            var marker = "-to-v";
            var index = name.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index < 0) return String.Empty;
            var value = name.Substring(index + marker.Length);
            return String.IsNullOrEmpty(value) ? String.Empty : value;
        }

        private static void CopyFileIfExists(string fileName, string backupDir)
        {
            var source = System.IO.Path.Combine(AppPaths.Root, fileName);
            if (File.Exists(source))
            {
                File.Copy(source, System.IO.Path.Combine(backupDir, fileName), true);
            }
        }

        private static void CopyDirectoryIfExists(string source, string destination)
        {
            if (!Directory.Exists(source)) return;

            Directory.CreateDirectory(destination);
            foreach (var dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            {
                var rel = dir.Substring(source.Length).TrimStart(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
                Directory.CreateDirectory(System.IO.Path.Combine(destination, rel));
            }

            foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                var rel = file.Substring(source.Length).TrimStart(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
                var target = System.IO.Path.Combine(destination, rel);
                var targetDir = System.IO.Path.GetDirectoryName(target);
                if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);
                File.Copy(file, target, true);
            }
        }

        private static string ReadManifestValue(string backupDir, string key)
        {
            try
            {
                var manifest = System.IO.Path.Combine(backupDir, "manifest.txt");
                if (!File.Exists(manifest)) return String.Empty;

                foreach (var line in File.ReadAllLines(manifest))
                {
                    if (line.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase))
                    {
                        return line.Substring(key.Length + 1).Trim();
                    }
                }
            }
            catch
            {
            }

            return String.Empty;
        }

        private static string Sanitize(string value)
        {
            if (String.IsNullOrEmpty(value)) return "unknown";

            var builder = new StringBuilder();
            foreach (var ch in value)
            {
                if (Char.IsLetterOrDigit(ch) || ch == '.' || ch == '-' || ch == '_') builder.Append(ch);
                else builder.Append('_');
            }

            return builder.ToString();
        }

        private static string SafeManifest(string value)
        {
            return (value ?? String.Empty).Replace("\r", " ").Replace("\n", " ").Trim();
        }
    }

    internal sealed class BackupPickerForm : Form
    {
        private readonly ListBox list;
        private readonly TextBox details;
        private readonly List<BackupInfo> backups;

        public string SelectedBackupPath { get; private set; }

        public BackupPickerForm(List<BackupInfo> items)
        {
            backups = items;
            Text = "Откат ProGo";
            Width = 840;
            Height = 460;
            StartPosition = FormStartPosition.CenterScreen;

            list = new ListBox
            {
                Dock = DockStyle.Top,
                Height = 235
            };
            list.SelectedIndexChanged += delegate { UpdateDetails(); };

            foreach (var backup in backups)
            {
                list.Items.Add(backup.DisplayName);
            }

            details = new TextBox
            {
                Dock = DockStyle.Top,
                Height = 110,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical
            };

            var ok = new Button
            {
                Text = "Откатить",
                Width = 110,
                Left = 590,
                Top = 365
            };
            ok.Click += delegate { Accept(); };

            var cancel = new Button
            {
                Text = "Отмена",
                Width = 110,
                Left = 710,
                Top = 365
            };
            cancel.Click += delegate { DialogResult = DialogResult.Cancel; };

            Controls.Add(details);
            Controls.Add(list);
            Controls.Add(ok);
            Controls.Add(cancel);

            if (list.Items.Count > 0) list.SelectedIndex = 0;
        }

        private void UpdateDetails()
        {
            if (list.SelectedIndex < 0 || list.SelectedIndex >= backups.Count)
            {
                details.Text = String.Empty;
                return;
            }

            var backup = backups[list.SelectedIndex];
            details.Text =
                "Папка: " + backup.Path + Environment.NewLine +
                "Версия: " + backup.Version + Environment.NewLine +
                "Целевая версия: " + (String.IsNullOrEmpty(backup.TargetVersion) ? "—" : backup.TargetVersion) + Environment.NewLine +
                "Тип: " + backup.Kind + Environment.NewLine +
                "Статус: " + backup.Result + Environment.NewLine +
                "Причина: " + backup.Reason + Environment.NewLine +
                "Создано: " + backup.Created;
        }

        private void Accept()
        {
            if (list.SelectedIndex < 0 || list.SelectedIndex >= backups.Count)
            {
                MessageBox.Show("Выберите резервную копию.", "Откат ProGo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SelectedBackupPath = backups[list.SelectedIndex].Path;
            DialogResult = DialogResult.OK;
        }

        public static bool TryPick(List<BackupInfo> backups, out string backupPath)
        {
            backupPath = null;
            using (var form = new BackupPickerForm(backups))
            {
                if (form.ShowDialog() != DialogResult.OK) return false;
                backupPath = form.SelectedBackupPath;
                return !String.IsNullOrEmpty(backupPath);
            }
        }
    }
}

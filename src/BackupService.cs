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
    }

    internal static class BackupService
    {
        private const int MaxBackups = 20;

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
                if (String.Equals(backup.Version, version, StringComparison.OrdinalIgnoreCase)) return true;
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
            CopyDirectoryIfExists(System.IO.Path.Combine(AppPaths.Root, "scripts"), System.IO.Path.Combine(backupDir, "scripts"));

            var manifest = new StringBuilder();
            manifest.AppendLine("product=ProGo");
            manifest.AppendLine("version=" + version);
            manifest.AppendLine("created=" + DateTimeOffset.Now.ToString("o"));
            manifest.AppendLine("reason=" + reason);
            manifest.AppendLine("contains=ProGo.exe,ProGo.ico,VERSION,scripts,vault.enc.json,settings.json,progo.log");
            File.WriteAllText(System.IO.Path.Combine(backupDir, "manifest.txt"), manifest.ToString(), Encoding.UTF8);

            TrimOldBackups();
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
                var version = ReadManifestValue(dir, "version");
                if (String.IsNullOrEmpty(version)) version = "unknown";

                var created = ReadManifestValue(dir, "created");
                var name = System.IO.Path.GetFileName(dir);
                var display = name + " — v" + version;
                if (!String.IsNullOrEmpty(created)) display += " — " + created;

                result.Add(new BackupInfo
                {
                    Path = dir,
                    DisplayName = display,
                    Version = version
                });
            }

            return result;
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

        private static void TrimOldBackups()
        {
            if (!Directory.Exists(BackupsRoot)) return;

            var dirs = Directory.GetDirectories(BackupsRoot);
            Array.Sort(dirs);
            Array.Reverse(dirs);

            for (var i = MaxBackups; i < dirs.Length; i++)
            {
                try { Directory.Delete(dirs[i], true); }
                catch { }
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
    }

    internal sealed class BackupPickerForm : Form
    {
        private readonly ListBox list;
        private readonly List<BackupInfo> backups;

        public string SelectedBackupPath { get; private set; }

        public BackupPickerForm(List<BackupInfo> items)
        {
            backups = items;
            Text = "Откат ProGo";
            Width = 720;
            Height = 360;
            StartPosition = FormStartPosition.CenterScreen;

            list = new ListBox
            {
                Dock = DockStyle.Top,
                Height = 250
            };

            foreach (var backup in backups)
            {
                list.Items.Add(backup.DisplayName);
            }

            var ok = new Button
            {
                Text = "Откатить",
                Width = 110,
                Left = 470,
                Top = 270
            };
            ok.Click += delegate { Accept(); };

            var cancel = new Button
            {
                Text = "Отмена",
                Width = 110,
                Left = 590,
                Top = 270
            };
            cancel.Click += delegate { DialogResult = DialogResult.Cancel; };

            Controls.Add(list);
            Controls.Add(ok);
            Controls.Add(cancel);

            if (list.Items.Count > 0) list.SelectedIndex = 0;
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

using System;
using System.IO;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        private static void BackupCreationIntegrity()
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") return;
            AppPaths.EnsureDirectories();
            var names = new[] { "ProGo.exe", "VERSION", "settings.json", "vault.enc.json", "progo.log",
                "scripts/Start-ProGo.ps1", "scripts/Restore-ProGoBackup.ps1", "scripts/Update-ProGo.Core.ps1",
                "scripts/Maintenance-ProGo.ps1", "scripts/MaintenanceOperation.cs" };
            var originals = names.ToDictionary(n => n, n => File.Exists(Path.Combine(AppPaths.Root, n)) ? File.ReadAllBytes(Path.Combine(AppPaths.Root, n)) : null);
            var hadScripts = Directory.Exists(Path.Combine(AppPaths.Root, "scripts"));
            string backup = null;
            try
            {
                foreach (var name in names.Where(n => n != "progo.log"))
                {
                    var file = Path.Combine(AppPaths.Root, name);
                    Directory.CreateDirectory(Path.GetDirectoryName(file));
                    File.WriteAllText(file, name == "VERSION" ? "0.0.1" : "opaque synthetic fixture; never execute");
                }
                backup = BackupService.CreateBackup("manual");
                string error;
                Check(BackupService.TryValidateRestore(backup, out error), "actual app backup writer produces a complete shared digest index");
                Check(File.ReadAllText(Path.Combine(backup, "manifest.txt")).Contains("contains=" + BackupIntegrity.Contents(backup)), "app manifest describes actual copied roots");
                var installedVault = File.ReadAllBytes(AppPaths.VaultPath);
                File.AppendAllText(Path.Combine(backup, "vault.enc.json"), "damage");
                Check(!BackupService.TryValidateRestore(backup, out error) && error.Contains("vault.enc.json"), "application preflight rejects damaged opaque vault with a specific message");
                Check(File.ReadAllBytes(AppPaths.VaultPath).SequenceEqual(installedVault), "application preflight never mutates installed vault");
                Check(!BackupService.TryValidateRestore(Path.Combine(work, "missing-backup"), out error), "application preflight reports missing copy without throwing to UI");
            }
            finally
            {
                if (backup != null && Directory.Exists(backup)) Directory.Delete(backup, true);
                foreach (var pair in originals)
                {
                    var file = Path.Combine(AppPaths.Root, pair.Key);
                    if (pair.Value != null) File.WriteAllBytes(file, pair.Value);
                    else if (File.Exists(file)) File.Delete(file);
                }
                if (!hadScripts && Directory.Exists(Path.Combine(AppPaths.Root, "scripts"))) Directory.Delete(Path.Combine(AppPaths.Root, "scripts"));
            }
        }

        private static void BackupCleanupPreview()
        {
            var root = Path.Combine(work, "backup-preview"); Directory.CreateDirectory(root);
            for (int i = 0; i < 24; i++)
            {
                var dir = Path.Combine(root, "backup-20260101-" + i.ToString("D4")); Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, "manifest.txt"), "product=ProGo\nbackup_kind=automatic\n");
            }
            var plan = BackupRetention.Plan(root);
            using (var form = new BackupCleanupForm(plan))
            {
                form.Show(); Application.DoEvents();
                var paths = form.Controls.Find("CleanupCandidates", true).OfType<TextBox>().Single();
                Check(paths.ReadOnly && paths.ScrollBars == ScrollBars.Both && paths.Lines.SequenceEqual(plan.Candidates.Select(c => c.Path)), "cleanup preview shows every exact path in a scrollable read-only list");
                Check(form.AcceptButton == null && form.CancelButton != null, "cleanup requires explicit confirmation and supports cancel");
                Check(paths.SelectionLength == 0 && ((Button)form.CancelButton).Focused, "preview focuses cancel instead of selecting every path");
                Snapshot(form, "backup-cleanup-preview", false);
                form.Scale(new SizeF(1.5f, 1.5f)); Application.DoEvents(); Shot(form, "backup-cleanup-preview-150");
                var cancel = (Button)form.CancelButton;
                Check(paths.RectangleToScreen(paths.ClientRectangle).Bottom < cancel.RectangleToScreen(cancel.ClientRectangle).Top && cancel.Visible,
                    "scaled cleanup list and actions do not overlap");
                cancel.PerformClick();
                Check(form.DialogResult == DialogResult.Cancel && Directory.GetDirectories(root).Length == 24, "cancel leaves all candidates untouched");
            }
        }
    }
}

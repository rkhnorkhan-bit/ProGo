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
                Directory.Delete(backup, true); backup = null;
                backup = BackupService.CreateBackup("baseline");
                Check(BackupService.HasBackupForVersion("0.0.1"), "complete indexed baseline satisfies version backup readiness");
                File.Delete(Path.Combine(backup, BackupIntegrity.IndexName));
                Check(!BackupService.HasBackupForVersion("0.0.1"), "legacy baseline cannot prevent creation of a new verifiable baseline");
                File.Delete(Path.Combine(AppPaths.Root, "ProGo.exe"));
                var incompleteRefused = false;
                var before = Directory.Exists(BackupService.BackupsRoot) ? Directory.GetDirectories(BackupService.BackupsRoot) : new string[0];
                try { BackupService.CreateBackup("manual"); } catch (System.IO.InvalidDataException) { incompleteRefused = true; }
                finally
                {
                    foreach (var dir in Directory.GetDirectories(BackupService.BackupsRoot).Except(before)) Directory.Delete(dir, true);
                }
                Check(incompleteRefused, "app refuses to report incomplete source installation as a ready copy");
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

        private static void RestoreScopeAndPreparationUi()
        {
            var root = Path.Combine(work, "restore-options-fixture"); Directory.CreateDirectory(root);
            Directory.CreateDirectory(Path.Combine(root, "scripts"));
            File.WriteAllText(Path.Combine(root, "ProGo.exe"), "fixture; never execute");
            File.WriteAllText(Path.Combine(root, "VERSION"), "0.0.1");
            File.WriteAllText(Path.Combine(root, "manifest.txt"), "product=ProGo\nversion=0.0.1\n");
            File.WriteAllText(Path.Combine(root, "settings.json"), "{}");
            File.WriteAllText(Path.Combine(root, "vault.enc.json"), "opaque encrypted fixture");
            foreach (var name in new[] { "Start-ProGo.ps1", "Restore-ProGoBackup.ps1", "Update-ProGo.Core.ps1" })
                File.WriteAllText(Path.Combine(root, "scripts", name), "fixture; never execute");
            // Long enough to exercise UI pumping during real local digest/copy work.
            File.WriteAllBytes(Path.Combine(root, "progo.log"), new byte[16 * 1024 * 1024]);
            BackupIntegrity.Write(root);
            using (var form = new RestoreOptionsForm(root))
            {
                form.Show(); Application.DoEvents();
                var consent = Descendants(form).OfType<CheckBox>().Single();
                var data = Descendants(form).OfType<RadioButton>().Single(r => r.Name == "RestoreData");
                var all = Descendants(form).OfType<RadioButton>().Single(r => r.Name == "RestoreAll");
                var program = Descendants(form).OfType<RadioButton>().Single(r => r.Name == "RestoreProgram");
                var prepare = Descendants(form).OfType<Button>().Single(b => b.Text == "Проверить и подготовить копию");
                var contents = Descendants(form).OfType<TextBox>().Single();
                Check(form.Scope == "Program" && !form.DataConfirmed && !consent.Enabled && prepare.Enabled, "restore defaults to program without consent to replace user data");
                Check(form.AcceptButton == null && ((Button)form.CancelButton).Focused, "restore options do not grant destructive Enter confirmation");
                Check(!contents.Text.Contains("vault.enc.json") && !contents.Text.Contains("progo.log"), "program preview excludes user payloads and historic logs");
                Shot(form, "restore-scope-program");
                data.Checked = true;
                Check(!prepare.Enabled && consent.Enabled && !consent.Checked && contents.Text.Contains("vault.enc.json") && !contents.Text.Contains("ProGo.exe"), "data restore exposes exact payloads and requires separate consent");
                consent.Checked = true;
                Check(form.DataConfirmed && prepare.Enabled, "data consent enables preparation without initiating restoration");
                all.Checked = true;
                Check(!consent.Checked && !prepare.Enabled, "changing data scope clears previous consent");
                Shot(form, "restore-scope-all");
                form.Scale(new SizeF(1.5f, 1.5f)); Application.DoEvents(); Shot(form, "restore-scope-all-150");
                Check(contents.RectangleToScreen(contents.ClientRectangle).Bottom < prepare.RectangleToScreen(prepare.ClientRectangle).Top && consent.RectangleToScreen(consent.ClientRectangle).Bottom <= contents.RectangleToScreen(contents.ClientRectangle).Top, "scaled scope preview and consent do not overlap actions");
                program.Checked = true;
                Check(!consent.Enabled && !form.DataConfirmed, "returning to program scope removes data authorization");
                ((Button)form.CancelButton).PerformClick();
                Check(form.DialogResult == DialogResult.Cancel && File.ReadAllText(Path.Combine(root, "settings.json")) == "{}", "scope cancellation leaves original copy untouched");
            }
            using (var form = new RestoreOptionsForm(root))
            using (var pulse = new System.Windows.Forms.Timer { Interval = 10 })
            {
                int pulses = 0; pulse.Tick += delegate { pulses++; };
                form.Shown += delegate { pulse.Start(); Descendants(form).OfType<Button>().Single(b => b.Text == "Проверить и подготовить копию").PerformClick(); };
                Check(form.ShowDialog() == DialogResult.OK && pulses > 0, "real snapshot preparation keeps the native modal UI responsive");
                using (var prepared = form.TakePreparedCopy())
                {
                    BackupIntegrity.Validate(prepared.Path);
                    Check(File.ReadAllText(Path.Combine(prepared.Path, "settings.json")) == "{}", "prepared result reaches caller with original verified payload");
                    var args = BackupService.RestoreArguments("restore.ps1", prepared.Path, "Program", false, 123);
                    Check(args.Contains("-Scope Program") && !args.Contains("-ConfirmData"), "normal restore launcher carries a safe explicit scope");
                    args = BackupService.RestoreArguments("restore.ps1", prepared.Path, "All", true, 123);
                    Check(args.Contains("-Scope All -ConfirmData"), "confirmed data scope reaches installed helper explicitly");
                    bool rejected = false;
                    try { BackupService.RestoreArguments("restore.ps1", prepared.Path, "All", false, 123); } catch (InvalidDataException) { rejected = true; }
                    Check(rejected, "launcher refuses missing data consent before starting a helper");
                }
            }
            using (var form = new RestoreOptionsForm(root))
            {
                form.Shown += delegate {
                    Descendants(form).OfType<Button>().Single(b => b.Text == "Проверить и подготовить копию").PerformClick();
                    ((Button)form.CancelButton).PerformClick();
                };
                Check(form.ShowDialog() == DialogResult.Cancel, "cancel during real asynchronous preparation cannot complete as an accepted restore");
            }
            using (var form = new RestoreOptionsForm(Path.Combine(root, "missing-copy")))
                Check(!Descendants(form).OfType<Button>().Single(b => b.Text == "Проверить и подготовить копию").Enabled, "missing source disables preparation instead of throwing from the chooser");
            File.Delete(Path.Combine(root, "settings.json")); File.Delete(Path.Combine(root, "vault.enc.json"));
            using (var form = new RestoreOptionsForm(root))
                Check(Descendants(form).OfType<RadioButton>().Where(r => r.Name != "RestoreProgram").All(r => !r.Enabled), "copy without user payloads offers program scope only");
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

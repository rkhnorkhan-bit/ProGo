using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ProGo
{
    internal static partial class DesktopTests
    {
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
                Snapshot(form, "backup-cleanup-preview", false);
                ((Button)form.CancelButton).PerformClick();
                Check(form.DialogResult == DialogResult.Cancel && Directory.GetDirectories(root).Length == 24, "cancel leaves all candidates untouched");
            }
        }
    }
}

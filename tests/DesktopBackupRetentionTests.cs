using System;
using System.IO;
using System.Drawing;
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

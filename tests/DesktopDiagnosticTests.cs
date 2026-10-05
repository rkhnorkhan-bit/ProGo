using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        private static void DiagnosticPreviewWorkflow()
        {
            string root = Path.Combine(work, "diagnostic-preview"); Directory.CreateDirectory(root);
            File.WriteAllText(Path.Combine(root, "progo.log"), "2026-01-02 03:04:05 +00:00 [ERROR] Failed to start SSH tunnel. password=fixture-private-content");
            var report = DiagnosticReport.Build(root, "0.2.2");
            int copies = 0, saves = 0; string copied = null, saved = null;
            using (var form = new DiagnosticPreviewForm(report, s => { copies++; copied = s; }, s => { saves++; saved = s; return true; }))
            {
                form.Show(); Application.DoEvents();
                var preview = Descendants(form).OfType<TextBox>().Single();
                var buttons = Descendants(form).OfType<Button>().ToArray();
                Check(copies == 0 && saves == 0, "opening diagnostic preview does not copy or save");
                Check(preview.ReadOnly && preview.Text == report.Text && !preview.Text.Contains("fixture-private-content"), "preview contains only immutable projected report");
                Check(form.AcceptButton == null && form.CancelButton != null && preview.Focused, "preview starts in text with no implicit Enter export");
                Check(preview.AccessibilityObject.Name == "Предпросмотр диагностического отчёта", "report preview has an accessible purpose");
                foreach (var size in new[] { new Size(820, 600), new Size(700, 500) })
                {
                    form.ClientSize = size; Application.DoEvents();
                    Check(buttons.All(b => b.Visible && b.Width >= TextRenderer.MeasureText(b.Text, b.Font).Width), "diagnostic action labels fit at width " + size.Width);
                    Check(buttons.All(b => preview.RectangleToScreen(preview.ClientRectangle).Bottom <= b.RectangleToScreen(b.ClientRectangle).Top), "diagnostic text does not overlap actions at width " + size.Width);
                    Shot(form, "diagnostic-preview-" + size.Width);
                }
                File.AppendAllText(Path.Combine(root, "progo.log"), "\nsecret added after preview");
                buttons.Single(b => b.Text == "Копировать отчёт").PerformClick();
                Check(copies == 1 && copied == preview.Text && saves == 0, "copy exports exactly the reviewed snapshot without rereading logs");
                buttons.Single(b => b.Text == "Сохранить как…").PerformClick();
                Check(saves == 1 && saved == copied, "save exports the same reviewed text");
                form.Scale(new SizeF(1.5f, 1.5f)); Application.DoEvents(); Shot(form, "diagnostic-preview-150");
                Check(buttons.All(b => preview.RectangleToScreen(preview.ClientRectangle).Bottom <= b.RectangleToScreen(b.ClientRectangle).Top), "scaled preview retains separate action area");
                buttons.Single(b => b.Text == "Закрыть").PerformClick();
                Check(copies == 1 && saves == 1, "closing preview adds no export action");
            }
            using (var form = new DiagnosticPreviewForm(report, s => { throw new IOException("private-copy-path"); }, s => { throw new IOException("private-save-path"); }))
            {
                form.Show(); Application.DoEvents();
                foreach (var caption in new[] { "Копировать отчёт", "Сохранить как…" }) {
                    Descendants(form).OfType<Button>().Single(b => b.Text == caption).PerformClick();
                    Check(!String.Join(" ", Descendants(form).Select(c => c.Text)).Contains("private-"), "export errors never echo private exception text: " + caption);
                }
                Check(form.Visible, "failed exports leave preview available for retry"); form.Close();
            }
            using (var form = new DiagnosticPreviewForm(report, s => copies++, s => false))
            {
                form.Show(); Application.DoEvents();
                Descendants(form).OfType<Button>().Single(b => b.Text == "Сохранить как…").PerformClick();
                Check(Descendants(form).OfType<Label>().Any(l => l.Text == "Сохранение отменено."), "cancelled file chooser is not reported as saved");
                form.Close();
            }
        }
    }
}

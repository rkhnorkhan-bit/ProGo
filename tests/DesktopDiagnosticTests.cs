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
            bool privateRootExisted = Directory.Exists(HomeVpnPrivateFiles.Root); var privateBefore = WizardPrivateSnapshot();
            var key = typeof(Form).GetMethod("ProcessDialogKey", PrivateInstance);
            string root = Path.Combine(work, "diagnostic-preview"); Directory.CreateDirectory(root);
            File.WriteAllText(Path.Combine(root, "progo.log"), "2026-01-02 03:04:05 +00:00 [ERROR] Failed to start SSH tunnel. password=fixture-private-content");
            var report = DiagnosticReport.Build(root, "0.2.2");
            int copies = 0, saves = 0; string copied = null, saved = null;
            using (var form = new DiagnosticPreviewForm(report, s => { copies++; copied = s; }, s => { saves++; saved = s; return true; }))
            {
                form.Show(); Application.DoEvents();
                var preview = Descendants(form).OfType<TextBox>().Single();
                var buttons = Descendants(form).OfType<Button>().ToArray();
                var copy = buttons.Single(b => b.Text == "Копировать отчёт"); var save = buttons.Single(b => b.Text == "Сохранить как…"); var close = (Button)form.CancelButton;
                Check(preview.AccessibilityObject.Description.Contains("Только чтение") && preview.AccessibilityObject.Description.Contains("не перечитываются") &&
                    copy.AccessibilityObject.Description.Contains("исходные журналы не копируются") && save.AccessibilityObject.Description.Contains("Отмена выбора") &&
                    close.AccessibilityObject.Description.Contains("не отключается"), "diagnostic action descriptions distinguish reviewed export, cancellation and connection scope");
                Check(Descendants(form).OfType<Label>().Any(l => l.AccessibilityObject.Name == "Перед передачей диагностики" && l.AccessibilityObject.Description == l.Text), "diagnostic transfer guidance has a stable accessible purpose");
                DiagnosticAccessibleStatus(form, "initial guidance");
                Check(copies == 0 && saves == 0, "opening diagnostic preview does not copy or save");
                Check(preview.ReadOnly && preview.Text == report.Text && !preview.Text.Contains("fixture-private-content"), "preview contains only immutable projected report");
                Check(form.AcceptButton == null && form.CancelButton != null && preview.Focused, "preview starts in text with no implicit Enter export");
                Check(preview.AccessibilityObject.Name == "Предпросмотр диагностического отчёта", "report preview has an accessible purpose");
                foreach (var size in new[] { new Size(820, 600), new Size(700, 500) })
                {
                    form.ClientSize = size; Application.DoEvents();
                    Check(buttons.All(b => b.Visible && b.Width >= TextRenderer.MeasureText(b.Text, b.Font).Width), "diagnostic action labels fit at width " + size.Width);
                    Check(buttons.All(b => preview.RectangleToScreen(preview.ClientRectangle).Bottom <= b.RectangleToScreen(b.ClientRectangle).Top), "diagnostic text does not overlap actions at width " + size.Width);
                    KeyboardWalk(form, new Control[] { preview, copy, save, close }, "diagnostic preview " + size.Width);
                    Check(buttons.All(b => b.Parent.ClientRectangle.Contains(b.Bounds)), "diagnostic keyboard actions stay fully visible at width " + size.Width);
                    Shot(form, "diagnostic-preview-" + size.Width);
                }
                preview.Focus(); key.Invoke(form, new object[] { Keys.Enter }); Application.DoEvents();
                Check(form.Visible && copies == 0 && saves == 0 && preview.Text == report.Text, "diagnostic Enter in reviewed text neither exports nor changes the snapshot");
                File.AppendAllText(Path.Combine(root, "progo.log"), "\nsecret added after preview");
                buttons.Single(b => b.Text == "Копировать отчёт").PerformClick();
                Check(copies == 1 && copied == preview.Text && saves == 0, "copy exports exactly the reviewed snapshot without rereading logs");
                DiagnosticAccessibleStatus(form, "copy completed");
                buttons.Single(b => b.Text == "Сохранить как…").PerformClick();
                Check(saves == 1 && saved == copied, "save exports the same reviewed text");
                DiagnosticAccessibleStatus(form, "save completed");
                form.Scale(new SizeF(1.5f, 1.5f)); Application.DoEvents(); Shot(form, "diagnostic-preview-150");
                Check(buttons.All(b => preview.RectangleToScreen(preview.ClientRectangle).Bottom <= b.RectangleToScreen(b.ClientRectangle).Top), "scaled preview retains separate action area");
                close.Focus(); key.Invoke(form, new object[] { Keys.Enter }); Application.DoEvents();
                Check(!form.Visible, "diagnostic Enter on Close dismisses the reviewed report");
                Check(copies == 1 && saves == 1, "closing preview adds no export action");
            }
            var sourceBefore = File.ReadAllBytes(Path.Combine(root, "progo.log"));
            bool failCopy = true, failSave = true; int errorCopies = 0, errorSaves = 0;
            using (var form = new DiagnosticPreviewForm(report, s => { errorCopies++; if (failCopy) throw new IOException("private-copy-path"); },
                s => { errorSaves++; if (failSave) throw new IOException("private-save-path"); return true; }))
            {
                form.ClientSize = new Size(700, 500); form.Show(); Application.DoEvents();
                var preview = Descendants(form).OfType<TextBox>().Single(); var close = (Button)form.CancelButton;
                var copy = Descendants(form).OfType<Button>().Single(b => b.Text == "Копировать отчёт");
                var save = Descendants(form).OfType<Button>().Single(b => b.Text == "Сохранить как…");
                foreach (var button in new[] { copy, save }) {
                    button.PerformClick(); DiagnosticAccessibleStatus(form, "failed " + button.Text);
                    Check(!String.Join(" ", Descendants(form).Select(c => c.Text)).Contains("private-"), "export errors never echo private exception text: " + button.Text);
                    KeyboardWalk(form, new Control[] { preview, copy, save, close }, "diagnostic failed " + button.Text);
                    var status = (Label)Field(form, "status");
                    Check(status.Parent.ClientRectangle.Contains(status.Bounds) && close.Parent.ClientRectangle.Contains(close.Bounds),
                        "diagnostic correction and Close stay fully visible after " + button.Text);
                    Shot(form, button == copy ? "keyboard-diagnostic-copy-error" : "keyboard-diagnostic-save-error");
                }
                Check(form.Visible && errorCopies == 1 && errorSaves == 1, "failed exports leave preview available for explicit retry");
                failCopy = failSave = false;
                copy.PerformClick(); DiagnosticAccessibleStatus(form, "copy retry completed");
                Check(((Label)Field(form, "status")).Text.StartsWith("Отчёт скопирован"), "diagnostic copy retry replaces stale failure with current success");
                save.PerformClick(); DiagnosticAccessibleStatus(form, "save retry completed");
                Check(((Label)Field(form, "status")).Text.StartsWith("Отчёт сохранён") && errorCopies == 2 && errorSaves == 2 && preview.Text == report.Text,
                    "diagnostic save retry preserves reviewed snapshot and replaces stale failure");
                preview.Focus(); key.Invoke(form, new object[] { Keys.Escape }); Application.DoEvents();
                Check(!form.Visible && form.DialogResult == DialogResult.Cancel && errorCopies == 2 && errorSaves == 2,
                    "diagnostic Escape after retries adds no export action");
            }
            int cancelledSaves = 0, beforeCopies = copies;
            using (var form = new DiagnosticPreviewForm(report, s => copies++, s => { cancelledSaves++; return false; }))
            {
                form.Show(); Application.DoEvents();
                var preview = Descendants(form).OfType<TextBox>().Single(); var close = (Button)form.CancelButton;
                var copy = Descendants(form).OfType<Button>().Single(b => b.Text == "Копировать отчёт");
                var save = Descendants(form).OfType<Button>().Single(b => b.Text == "Сохранить как…");
                save.PerformClick();
                Check(Descendants(form).OfType<Label>().Any(l => l.Text == "Сохранение отменено."), "cancelled file chooser is not reported as saved");
                DiagnosticAccessibleStatus(form, "save cancelled");
                KeyboardWalk(form, new Control[] { preview, copy, save, close }, "diagnostic cancelled save");
                preview.Focus(); key.Invoke(form, new object[] { Keys.Escape }); Application.DoEvents();
                Check(!form.Visible && cancelledSaves == 1 && copies == beforeCopies, "diagnostic Escape after cancelled save neither retries nor copies");
            }
            Check(sourceBefore.SequenceEqual(File.ReadAllBytes(Path.Combine(root, "progo.log"))), "diagnostic keyboard navigation and export actions preserve source log bytes");
            var privateAfter = WizardPrivateSnapshot();
            Check(privateRootExisted == Directory.Exists(HomeVpnPrivateFiles.Root) && privateBefore.Keys.OrderBy(p => p).SequenceEqual(privateAfter.Keys.OrderBy(p => p)) &&
                privateBefore.All(pair => privateAfter[pair.Key].SequenceEqual(pair.Value)), "diagnostic preview keyboard fixtures preserve private access files");
        }
        private static void DiagnosticAccessibleStatus(Form form, string state)
        {
            var status = (Label)Field(form, "status");
            Check(status.AccessibilityObject.Name == "Результат передачи диагностики" && status.AccessibilityObject.Description == status.Text &&
                !status.AccessibilityObject.Description.Contains("private-"), "diagnostic accessible result follows " + state);
        }
    }
}

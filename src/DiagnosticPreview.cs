using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace ProGo
{
    public static class DiagnosticPreview
    {
        public static void Show(string root, string version)
        {
            using (var form = new DiagnosticPreviewForm(DiagnosticReport.Build(root, version))) form.ShowDialog();
        }
    }

    internal sealed class DiagnosticPreviewForm : ProGoForm
    {
        private readonly DiagnosticReport report;
        private readonly Label status;
        internal DiagnosticPreviewForm(DiagnosticReport report, Action<string> copy = null, Func<string, bool> save = null)
        {
            if (report == null) throw new ArgumentNullException("report");
            this.report = report;
            Text = "Передать диагностику · ProGo"; ClientSize = new Size(820, 600); MinimumSize = new Size(700, 500);
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 1, RowCount = 4 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var notice = new Label { AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 12),
                Text = "Просмотрите отчёт перед передачей. Здесь только время и известные события — без исходного текста ошибок, адресов и личных путей. Оригиналы остаются на компьютере. Ничего не отправляется автоматически." };
            var preview = new TextBox { Name = "diagnosticText", Multiline = true, ReadOnly = true, WordWrap = false,
                ScrollBars = ScrollBars.Both, Dock = DockStyle.Fill, Text = report.Text, TabIndex = 0,
                AccessibleName = "Предпросмотр диагностического отчёта", AccessibleDescription = "Именно этот текст будет скопирован или сохранён." };
            status = new Label { AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 10, 0, 10), Text = "Проверьте текст, затем выберите действие.", AccessibleName = "Результат передачи диагностики" };
            var actions = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = true, Margin = new Padding(0), TabIndex = 1 };
            var copyButton = UiTheme.Button("Копировать отчёт", delegate {
                try { (copy ?? new Action<string>(Clipboard.SetText))(this.report.Text); status.Text = "Отчёт скопирован. Внешняя отправка остаётся за вами."; }
                catch { status.Text = "Не удалось скопировать. Буфер может быть занят — попробуйте ещё раз."; }
            }, true);
            var saveButton = UiTheme.Button("Сохранить как…", delegate {
                try { status.Text = (save ?? new Func<string, bool>(SaveReport))(this.report.Text) ? "Отчёт сохранён в выбранный файл." : "Сохранение отменено."; }
                catch { status.Text = "Не удалось сохранить отчёт. Проверьте доступ к выбранной папке."; }
            }, false);
            var close = UiTheme.Button("Закрыть", delegate { Close(); }, false); close.DialogResult = DialogResult.Cancel;
            actions.Controls.Add(copyButton); actions.Controls.Add(saveButton); actions.Controls.Add(close);
            layout.Controls.Add(notice, 0, 0); layout.Controls.Add(preview, 0, 1); layout.Controls.Add(status, 0, 2); layout.Controls.Add(actions, 0, 3);
            Controls.Add(layout); CancelButton = close; AcceptButton = null;
            layout.SizeChanged += delegate { notice.MaximumSize = new Size(Math.Max(200, layout.ClientSize.Width - layout.Padding.Horizontal), 0); };
            Shown += delegate { preview.Focus(); preview.SelectionStart = 0; preview.SelectionLength = 0; };
            UiTheme.ConfigureKeyboardOrder(this);
        }

        private bool SaveReport(string text)
        {
            using (var dialog = new SaveFileDialog { Title = "Сохранить диагностический отчёт", Filter = "Текстовый отчёт (*.txt)|*.txt",
                FileName = "ProGo-diagnostics.txt", DefaultExt = "txt", AddExtension = true, OverwritePrompt = true, RestoreDirectory = true })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return false;
                File.WriteAllText(dialog.FileName, text, new UTF8Encoding(true)); return true;
            }
        }
    }
}

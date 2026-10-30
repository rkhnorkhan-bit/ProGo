using System;
using System.Linq;
using System.Windows.Forms;

namespace ProGo
{
    internal sealed class BackupCleanupForm : ProGoForm
    {
        public BackupCleanupForm(BackupRetentionPlan plan)
        {
            Text = "Удалить старые автоматические копии";
            Width = 840; Height = 500; StartPosition = FormStartPosition.CenterScreen;
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), ColumnCount = 1, RowCount = 4 };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
            root.Controls.Add(UiTheme.Label("Будет удалено копий: " + plan.Candidates.Count, UiTheme.Heading, UiTheme.Text), 0, 0);
            var guidance = UiTheme.Label("Ниже — полный список папок для удаления. Ручные и неизвестные папки, 10 последних автоматических копий, последняя исходная и последняя копия перед обновлением сохраняются.", UiTheme.Body, UiTheme.Muted);
            guidance.AutoSize = false; guidance.Dock = DockStyle.Fill;
            root.Controls.Add(guidance, 0, 1);
            var paths = new TextBox { Name = "CleanupCandidates", AccessibleName = "Копии для удаления",
                AccessibleDescription = "Только чтение. Полный список папок, которые будут удалены после вашего подтверждения; его можно выделить и скопировать.", Dock = DockStyle.Fill, Multiline = true, ReadOnly = true,
                ScrollBars = ScrollBars.Both, WordWrap = false,
                Text = String.Join(Environment.NewLine, plan.Candidates.Select(c => c.Path)) };
            root.Controls.Add(paths, 0, 2);
            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 8, 0, 0) };
            var cancel = UiTheme.Button("Отмена", delegate { DialogResult = DialogResult.Cancel; }, false);
            var remove = UiTheme.Button("Удалить эти копии", delegate { DialogResult = DialogResult.OK; }, true);
            remove.Enabled = plan.Candidates.Count > 0;
            actions.Controls.Add(cancel); actions.Controls.Add(remove); root.Controls.Add(actions, 0, 3);
            UiTheme.ConfigureKeyboardOrder(root);
            Controls.Add(root); CancelButton = cancel;
            Shown += delegate { paths.Select(0, 0); cancel.Focus(); };
            // Enter must not implicitly confirm a destructive list.
        }
    }
}

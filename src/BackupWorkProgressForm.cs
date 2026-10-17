using System;
using System.Windows.Forms;

namespace ProGo
{
    internal sealed class BackupWorkProgressForm : ProGoForm
    {
        private readonly Label status = UiTheme.Label("", UiTheme.Body, UiTheme.Text);
        private readonly Button cancel;
        private readonly Action requestCancel;
        internal bool IsBusy { get; private set; }
        internal BackupWorkProgressForm(Action requestCancel)
        {
            this.requestCancel = requestCancel; IsBusy = true;
            Text = "Операция с копиями ProGo"; Width = 760; Height = 290;
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), RowCount = 2, ColumnCount = 1 };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
            status.Name = "BackupWorkStatus"; status.AutoSize = false; status.Dock = DockStyle.Fill;
            status.AccessibleName = "Состояние операции с копиями";
            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            cancel = UiTheme.Button("Отменить операцию", delegate { if (IsBusy) Cancel(); else Close(); }, false);
            actions.Controls.Add(cancel); root.Controls.Add(status, 0, 0); root.Controls.Add(actions, 0, 1);
            Controls.Add(root); CancelButton = cancel; UiTheme.ConfigureKeyboardOrder(root);
            FormClosing += delegate(object sender, FormClosingEventArgs e) { if (IsBusy) { e.Cancel = true; Cancel(); } };
        }
        private void Cancel()
        { requestCancel(); status.Text = "Отмена запрошена. Ждём завершения текущей операции и очистки временной копии; интерфейс продолжает работать."; }
        internal void SetStatus(string text) { status.Text = text; status.AccessibleDescription = text; }
        internal void Complete(string text) { IsBusy = false; cancel.Text = "Закрыть"; SetStatus(text); }
    }

    internal sealed class RestoreConfirmationForm : ProGoForm
    {
        internal RestoreConfirmationForm(RestorePreparedInfo prepared)
        {
            Text = "Подтвердите восстановление ProGo"; Width = 790; Height = 460;
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), RowCount = 2, ColumnCount = 1 };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
            var text = new TextBox { ReadOnly = true, Multiline = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill,
                AccessibleName = "Подтверждение состава восстановления", Text = "Копия проверена. Версия в копии: " + prepared.Version +
                "\r\n\r\nБудет восстановлено:\r\n" + String.Join("\r\n", prepared.Names) +
                (prepared.Scope == "Program" ? "\r\n\r\nТекущие настройки и хранилище сохранятся." : "\r\n\r\nПеречисленные пользовательские данные будут заменены данными из копии.") +
                "\r\nТекущие VPN-доступы и снимки прокси сохранятся; архивные не импортируются.\r\n\r\nProGo закроется и запустится снова. Начать восстановление?" };
            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            var cancel = UiTheme.Button("Не восстанавливать", delegate { DialogResult = DialogResult.Cancel; }, false);
            var confirm = UiTheme.Button("Начать восстановление", delegate { DialogResult = DialogResult.OK; }, true);
            actions.Controls.Add(cancel); actions.Controls.Add(confirm); root.Controls.Add(text, 0, 0); root.Controls.Add(actions, 0, 1);
            Controls.Add(root); CancelButton = cancel; AcceptButton = null;
            Shown += delegate { cancel.Focus(); }; UiTheme.ConfigureKeyboardOrder(root);
        }
    }
}

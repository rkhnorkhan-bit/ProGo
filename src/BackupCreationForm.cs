using System;
using System.Windows.Forms;

namespace ProGo
{
    internal sealed class BackupCreationForm : ProGoForm
    {
        private readonly Action cancelCopy;
        private readonly bool baseline;
        private readonly Label status = UiTheme.Label("Создаём копию в фоне. Приложение и подключения продолжают работать.", UiTheme.Body, UiTheme.Text);
        private readonly TextBox details = new TextBox { Name = "BackupResultDetails", Dock = DockStyle.Fill, Multiline = true,
            ReadOnly = true, ScrollBars = ScrollBars.Vertical, AccessibleName = "Результат создания копии" };
        private readonly Button cancel;
        internal bool IsBusy { get; private set; }
        internal string Message { get { return status.Text + "\n" + details.Text; } }
        internal event Action ResultApplied;

        internal BackupCreationForm(Action cancelCopy, bool baseline = false)
        {
            this.cancelCopy = cancelCopy; this.baseline = baseline; IsBusy = true;
            Text = "Резервная копия ProGo"; Width = 760; Height = 360;
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), ColumnCount = 1, RowCount = 3 };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
            status.Dock = DockStyle.Fill; status.AutoSize = false; status.AccessibleName = "Состояние создания копии";
            if (baseline) status.Text = "Проверяем стартовую копию в фоне. Если подходящей проверенной копии нет, создаём её. Интерфейс продолжает работать.";
            details.Text = (baseline ? "Это проверка стартовой копии, не запрос ручной копии. После её окончания можно создать отдельную ручную копию.\r\n" : "") + "Программа, настройки и хранилище копируются без замены текущих данных.\r\nVPN-файлы и снимки прокси — только архив; автоматического импорта нет. DPAPI не обеспечивает перенос VPN на другой ПК/пользователя.";
            details.AccessibleDescription = "Только чтение. После окончания здесь появятся папка и фактический состав копии.";
            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 8, 0, 0) };
            cancel = UiTheme.Button("Отменить копирование", delegate { if (IsBusy) RequestCancel(); else Close(); }, false);
            cancel.Name = "BackupCancel"; cancel.AccessibleDescription = "Отмена проверяется между операциями с файлами. Прежние данные и копии сохраняются.";
            actions.Controls.Add(cancel); root.Controls.Add(status, 0, 0); root.Controls.Add(details, 0, 1); root.Controls.Add(actions, 0, 2);
            Controls.Add(root); CancelButton = cancel; UiTheme.ConfigureKeyboardOrder(root);
            FormClosing += delegate(object sender, FormClosingEventArgs e) { if (IsBusy) { e.Cancel = true; RequestCancel(); } };
            Shown += delegate { cancel.Focus(); };
        }

        private void RequestCancel()
        {
            cancelCopy(); cancel.Enabled = false;
            status.Text = "Отменяем копирование. Если накопитель занят, ждём завершения текущей операции; интерфейс продолжает работать.";
        }

        internal void Complete(string message, string copiedContents, bool cancelled, bool failed, bool existing = false)
        {
            IsBusy = false; cancel.Enabled = true; cancel.Text = "Закрыть";
            status.Text = cancelled ? "Копирование отменено. Текущие данные и прежние копии сохранены." :
                (failed ? (baseline ? "Не удалось проверить или создать стартовую копию. Текущие данные не заменены." : "Не удалось создать копию. Текущие данные не заменены.") :
                (existing ? "Стартовая копия уже есть и проверена. Новая копия не создавалась." :
                (baseline ? "Стартовая резервная копия создана и проверена." : "Резервная копия создана и проверена.")));
            if (cancelled) details.Clear();
            else if (failed) details.Text = "Подробности записаны в журнал приложения. Автоматических повторов нет. После устранения причины можно создать ручную копию отдельной командой.";
            else if (existing) details.Text = "Подходящая стартовая копия этой версии прошла проверку состава и контрольных сумм. Файлы повторно не копировались. Это не создание ручной копии; для неё используйте отдельную команду.";
            else details.Text = "Папка: " + message + "\r\nСостав: " + copiedContents +
                "\r\nVPN-файлы и снимки прокси — только архив; автоматического импорта нет. DPAPI не обеспечивает перенос VPN на другой ПК/пользователя.";
            if (ResultApplied != null) ResultApplied();
        }
    }
}

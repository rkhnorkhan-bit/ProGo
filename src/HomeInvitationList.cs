using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ProGo
{
    internal sealed class HomeInvitationList : UserControl
    {
        private readonly List<HomeVpnInvitation> items;
        private readonly TextBox search = new TextBox { Dock = DockStyle.Fill, AccessibleName = "Поиск друга по имени или идентификатору" };
        private readonly ListBox list = new ListBox { Dock = DockStyle.Fill, HorizontalScrollbar = true, AccessibleName = "Приглашения друзей" };
        private readonly Label details = new Label { AutoSize = true, Dock = DockStyle.Fill, AccessibleName = "Сведения о выбранном доступе" };
        private readonly Label count = new Label { AutoSize = true, Dock = DockStyle.Fill, AccessibleName = "Результаты поиска друзей" };
        internal event EventHandler SelectionChanged;
        internal bool CanRevoke { get { return Selected != null && !Selected.Revoked; } }
        internal HomeVpnInvitation Selected { get { return list.SelectedItem as HomeVpnInvitation; } }

        internal HomeInvitationList(IEnumerable<HomeVpnInvitation> values)
        {
            items = values.Where(i => i != null).ToList();
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < 5; i++) layout.RowStyles.Add(new RowStyle(i == 2 ? SizeType.Percent : SizeType.AutoSize, i == 2 ? 100 : 0));
            layout.Controls.Add(new Label { AutoSize = true, Text = "Найти друга: имя или идентификатор" }, 0, 0);
            layout.Controls.Add(search, 0, 1); layout.Controls.Add(list, 0, 2); layout.Controls.Add(details, 0, 3); layout.Controls.Add(count, 0, 4);
            Controls.Add(layout);
            layout.SizeChanged += delegate {
                details.MaximumSize = count.MaximumSize = new Size(Math.Max(1, layout.ClientSize.Width - 12), 0);
            };
            search.TextChanged += delegate { RefreshItems(Selected == null ? null : Selected.Id); };
            list.SelectedIndexChanged += delegate { DescribeSelection(); };
            RefreshItems(null);
            UiTheme.ConfigureKeyboardOrder(this);
        }
        internal void Add(HomeVpnInvitation item) { items.Add(item); search.Text = ""; RefreshItems(item.Id); }
        internal void RefreshSelection() { RefreshItems(Selected == null ? null : Selected.Id); }
        private void RefreshItems(string selectedId)
        {
            string query = search.Text.Trim();
            list.BeginUpdate(); list.Items.Clear();
            foreach (var item in items.Where(i => (i.Name ?? "").IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                (i.Id ?? "").IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)) {
                int index = list.Items.Add(item);
                if (selectedId != null && item.Id == selectedId) list.SelectedIndex = index;
            }
            list.EndUpdate();
            count.Text = items.Count == 0 ? "Приглашений пока нет. Создайте отдельный токен для друга." :
                list.Items.Count == 0 ? "Совпадений нет. Измените поиск." : "Показано: " + list.Items.Count + " из " + items.Count;
            DescribeSelection();
        }
        private void DescribeSelection()
        {
            var item = Selected;
            details.Text = item == null ? "Выберите запись, чтобы увидеть дату, статус и права доступа." :
                "Имя: " + item.Name + "\r\nСоздан: " + item.CreatedText + "\r\nСтатус: " + item.Status + "\r\nID: " + item.Id +
                "\r\nПрава: VPN через ProGo; без командной оболочки, SFTP и управления VPS. Отзыв закрывает соединения этого приглашения.";
            var handler = SelectionChanged; if (handler != null) handler(this, EventArgs.Empty);
        }
        internal static Form TokenDialog(string value, ClipboardService clipboard)
        {
            var form = new ProGoForm { Text = "Личный токен для друга", ClientSize = new Size(650, 400), StartPosition = FormStartPosition.CenterParent };
            var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };
            panel.Controls.Add(new Label { AutoSize = true, MaximumSize = new Size(600, 0), Text =
                "Токен показывается только сейчас. Передайте его другу лично до закрытия окна: повторно показать этот токен нельзя. Он выберет «Подключиться к готовому VPS». Токен действует до отзыва владельцем и даёт только VPN-доступ." });
            panel.Controls.Add(new Label { AutoSize = true, MaximumSize = new Size(600, 0), Text =
                "Это приглашение в ProGo, а не QR-ссылка установки профиля. QR создаётся отдельно и действует ограниченное время. При потере токена отзовите старый доступ и создайте новый. Не публикуйте токен." });
            panel.Controls.Add(new TextBox { Width = 600, UseSystemPasswordChar = true, ReadOnly = true, Text = value, AccessibleName = "Личный токен приглашения" });
            var copy = new Button { AutoSize = true, Text = "Скопировать токен", MinimumSize = new Size(230, 38) };
            var notice = new Label { AutoSize = true, MaximumSize = new Size(600, 0), Text = clipboard.CopyNotice };
            clipboard.BindSecretCopy(copy, delegate { return value; }, notice);
            panel.Controls.Add(copy); panel.Controls.Add(notice); form.Controls.Add(panel);
            UiTheme.ConfigureKeyboardOrder(form); return form;
        }
    }
}

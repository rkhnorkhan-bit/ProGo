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
            search.AccessibleDescription = "Фильтрует список по имени или ID без изменения доступа. Одинаковые имена могут принадлежать разным приглашениям.";
            list.AccessibleDescription = "Стрелки выбирают запись; Tab выходит из списка. Проверьте полный ID и статус в сведениях о выбранном доступе.";
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
        internal void Add(HomeVpnInvitation item) {
            var existing = items.FirstOrDefault(i => i.Id == item.Id);
            if (existing == null) items.Add(item);
            else { existing.Name = item.Name; existing.Revoked = item.Revoked; if (item.Created != null) existing.Created = item.Created; }
            search.Text = ""; RefreshItems(item.Id);
        }
        internal void Replace(IEnumerable<HomeVpnInvitation> values) { items.Clear(); items.AddRange(values.Where(i => i != null)); search.Text = ""; RefreshItems(null); }
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
            count.AccessibleDescription = count.Text;
            DescribeSelection();
        }
        private void DescribeSelection()
        {
            var item = Selected;
            details.Text = item == null ? "Выберите запись, чтобы увидеть дату, статус и права доступа." :
                "Имя: " + item.Name + "\r\nСоздан: " + item.CreatedText + "\r\nСтатус: " + item.Status + "\r\nID: " + item.Id +
                "\r\nПрава: VPN через ProGo; без командной оболочки, SFTP и управления VPS. Отзыв закрывает соединения этого приглашения.";
            details.AccessibleDescription = details.Text;
            var handler = SelectionChanged; if (handler != null) handler(this, EventArgs.Empty);
        }
        internal static Form TokenDialog(string value, ClipboardService clipboard, Action confirmSaved = null)
        {
            var form = new ProGoForm { Text = "Личный токен для друга", ClientSize = new Size(650, 400), StartPosition = FormStartPosition.CenterParent };
            var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };
            panel.Controls.Add(new Label { AutoSize = true, MaximumSize = new Size(600, 0), Text =
                confirmSaved == null
                    ? "Токен показывается только сейчас. Передайте его другу лично до закрытия окна: повторно показать этот токен нельзя. Он выберет «Подключиться к готовому VPS». Токен действует до отзыва владельцем и даёт только VPN-доступ."
                    : "Сохраните токен для передачи другу, затем нажмите «Токен сохранён — завершить». Простое закрытие оставит запрос в ProGo: позже можно проверить исходную выдачу и получить тот же токен, если доступ не отозван. Друг выберет «Подключиться к готовому VPS»." });
            panel.Controls.Add(new Label { AutoSize = true, MaximumSize = new Size(600, 0), Text =
                confirmSaved == null
                    ? "Это приглашение в ProGo, а не QR-ссылка установки профиля. QR создаётся отдельно и действует ограниченное время. При потере токена выберите друга в списке и нажмите «Перевыпустить потерянный токен». Не публикуйте токен."
                    : "Это личное приглашение в ProGo. QR установки профиля создаётся отдельно. Подтверждение завершает только локальное сохранение запроса; оно не проверяет получение токена другом. После подтверждения потерянный токен потребуется перевыпустить. Не публикуйте токен." });
            var token = new TextBox { Width = 600, UseSystemPasswordChar = true, ReadOnly = true, Text = value, AccessibleName = "Личный токен приглашения",
                AccessibleDescription = "Только чтение. Токен скрыт; передайте его через кнопку копирования до закрытия этого окна." };
            token.Enter += delegate { panel.ScrollControlIntoView(token); }; panel.Controls.Add(token);
            var copy = new Button { AutoSize = true, Text = "Скопировать токен", MinimumSize = new Size(230, 38) };
            var notice = new Label { AutoSize = true, MaximumSize = new Size(600, 0), Text = clipboard.CopyNotice };
            copy.AccessibleDescription = "Копирует личный токен для передачи другу. " + clipboard.CopyNotice;
            copy.Enter += delegate { panel.ScrollControlIntoView(copy); };
            notice.AccessibleName = "Результат копирования токена"; notice.AccessibleDescription = notice.Text;
            notice.TextChanged += delegate { notice.AccessibleDescription = notice.Text; };
            clipboard.BindSecretCopy(copy, delegate { return value; }, notice);
            var close = new Button { AutoSize = true, Text = "Закрыть", MinimumSize = new Size(230, 38), DialogResult = DialogResult.Cancel,
                AccessibleDescription = confirmSaved == null ? "Закрывает окно без отзыва доступа. Повторно показать этот токен нельзя; потерянный токен придётся перевыпустить."
                    : "Закрывает окно без отзыва доступа и без завершения выдачи. Запрос сохранён: можно проверить исходный результат позже." };
            close.Click += delegate { form.Close(); }; form.CancelButton = close;
            panel.Controls.Add(copy);
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = Padding.Empty };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var footer = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = false, FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(18, 0, 18, 12), Margin = Padding.Empty };
            footer.Controls.Add(notice);
            if (confirmSaved != null) {
                var finish = new Button { AutoSize = true, Text = "Токен сохранён — завершить", MinimumSize = new Size(230, 38),
                    AccessibleDescription = "Подтверждает, что вы сохранили токен, и удаляет локальный незавершённый запрос. Не проверяет передачу другу и не отзывает доступ." };
                finish.Click += delegate {
                    try { confirmSaved(); form.DialogResult = DialogResult.OK; form.Close(); }
                    catch (HomeVpnInvitationPendingException ex) { notice.Text = ex.Message; }
                    catch { notice.Text = "Не удалось подтвердить локальное завершение. Запрос сохранён; повторите подтверждение."; }
                };
                footer.Controls.Add(finish);
            }
            footer.Controls.Add(close); layout.Controls.Add(panel, 0, 0); layout.Controls.Add(footer, 0, 1); form.Controls.Add(layout);
            if (confirmSaved != null) {
                form.MinimumSize = new Size(520, 420);
                panel.SizeChanged += delegate {
                    int width = Math.Max(1, panel.ClientSize.Width - panel.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth);
                    token.Width = width;
                    foreach (Label label in panel.Controls.OfType<Label>()) label.MaximumSize = new Size(width, 0);
                };
                footer.SizeChanged += delegate { notice.MaximumSize = new Size(Math.Max(1, footer.ClientSize.Width - footer.Padding.Horizontal), 0); };
            }
            UiTheme.ConfigureKeyboardOrder(form); return form;
        }
    }
}

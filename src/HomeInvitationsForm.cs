using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace ProGo
{
    internal sealed class HomeInvitationsForm : ProGoForm
    {
        private readonly HomeInvitationList list;
        private readonly TextBox name = new TextBox { Width = 740, Text = "Друг", MaxLength = 80, AccessibleName = "Имя нового приглашения" };
        private readonly Label nameLabel = new Label { AutoSize = true, Text = "Имя для нового приглашения (перевыпуск сохраняет имя выбранного друга)" };
        private readonly Button create = new Button { AutoSize = true, Text = "Создать отдельный токен" };
        private readonly Button revoke = new Button { AutoSize = true, Text = "Отозвать выбранный доступ" };
        private readonly Button reissue = new Button { AutoSize = true, Text = "Перевыпустить потерянный токен" };
        private readonly Button refresh = new Button { AutoSize = true, Text = "Обновить список" };
        private readonly Button close = new Button { AutoSize = true, Text = "Закрыть", MinimumSize = new Size(230, 38), DialogResult = DialogResult.Cancel };
        private readonly Label status = new Label { AutoSize = true, MaximumSize = new Size(740, 0), AccessibleName = "Результат операции с доступом" };
        private readonly Func<string, string, string, Task<string>> admin;
        private readonly HomeVpnOwner owner;
        private HomeVpnInvitationRequest pending;
        private bool recoveryBlocked, recoveryOnly, trackedToken;
        private bool working, needsRefresh;
        internal Func<string, bool> Confirm;
        internal Action<string> ShowToken;

        internal HomeInvitationsForm(HomeVpnInvitation[] items, Func<string, string, string, Task<string>> admin, ClipboardService clipboard, HomeVpnOwner owner = null)
        {
            this.admin = admin; this.owner = owner;
            Text = "Доступ друзей"; ClientSize = new Size(800, 760); StartPosition = FormStartPosition.CenterParent;
            var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };
            list = new HomeInvitationList(items) { Width = 740, Height = 365 };
            panel.Controls.Add(list);
            panel.Controls.Add(nameLabel);
            name.AccessibleDescription = "Имя для отдельного нового приглашения. Перевыпуск сохраняет имя выбранного друга, а не этот текст.";
            create.AccessibleDescription = "Создаёт отдельное приглашение на VPS и показывает новый личный токен. Другие доступы сохраняются.";
            reissue.AccessibleDescription = "После подтверждения отзывает выбранный ID и выдаёт новый токен с тем же именем. Старый доступ не восстанавливается при ошибке выдачи.";
            refresh.AccessibleDescription = "Получает текущий список с VPS и снимает блокировку изменений только после успешного ответа. Не создаёт и не отзывает приглашения.";
            close.AccessibleDescription = "Закрывает окно без изменения доступа друзей. Пока операция выполняется, закрытие недоступно.";
            status.AccessibleDescription = status.Text; status.TextChanged += delegate { status.AccessibleDescription = status.Text; };
            close.Click += delegate { Close(); }; CancelButton = close;
            panel.Controls.Add(name); panel.Controls.Add(create); panel.Controls.Add(revoke); panel.Controls.Add(reissue); panel.Controls.Add(refresh);
            foreach (Control input in new Control[] { list, name, create, revoke, reissue, refresh }) {
                var target = input; target.Enter += delegate { panel.ScrollControlIntoView(target); };
            }
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = Padding.Empty };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var footer = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = false, FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(16, 0, 16, 12), Margin = Padding.Empty };
            footer.Controls.Add(status); footer.Controls.Add(close); layout.Controls.Add(panel, 0, 0); layout.Controls.Add(footer, 0, 1); Controls.Add(layout);
            if (owner != null) {
                MinimumSize = new Size(600, 620);
                panel.SizeChanged += delegate {
                    int width = Math.Max(1, panel.ClientSize.Width - panel.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth);
                    list.MaximumSize = new Size(width, 0); list.Width = width;
                    // FlowLayout measures a TextBox's preferred width as well as
                    // its current bounds. Constrain both when the window shrinks.
                    name.MaximumSize = new Size(width, 0); name.Width = width;
                    nameLabel.MaximumSize = new Size(width, 0);
                };
                footer.SizeChanged += delegate { status.MaximumSize = new Size(Math.Max(1, footer.ClientSize.Width - footer.Padding.Horizontal - status.Margin.Horizontal), 0); };
            }
            Confirm = text => MessageBox.Show(this, text, "Изменение доступа", MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes;
            ShowToken = token => {
                var request = owner == null ? null : HomeVpnInvitationRecovery.Load(owner);
                if (trackedToken && (request == null || request.Result != token.Trim())) throw new HomeVpnInvitationPendingException(HomeVpnInvitationRecovery.PendingMessage);
                Action finish = trackedToken
                    ? (Action)(() => { HomeVpnInvitationRecovery.ConfirmSaved(owner, token); recoveryOnly = false; }) : null;
                using (var dialog = HomeInvitationList.TokenDialog(token, clipboard, finish)) dialog.ShowDialog(this);
            };
            list.SelectionChanged += delegate { UpdateActions(); };
            FormClosing += delegate(object sender, FormClosingEventArgs e) { if (working) e.Cancel = true; };
            refresh.Click += async delegate { await RunAsync(RefreshAsync); };
            create.Click += async delegate { await RunAsync(CreateAsync); };
            revoke.Click += async delegate {
                var selected = list.Selected;
                ReadPending(false); UpdateActions();
                if (selected == null || needsRefresh || working || recoveryOnly || recoveryBlocked) return;
                if (!Confirm(Identity(selected) + "\r\nОтозвать этот доступ? Его соединения будут закрыты.")) return;
                await RunAsync(async delegate {
                    try {
                        if (await admin("revoke", null, selected.Id) != "Access revoked.") throw new InvalidOperationException();
                        selected.Revoked = true; list.RefreshSelection(); status.Text = "Выбранный доступ отозван."; }
                    catch { RequireRefresh("Завершение отзыва не подтверждено. Обновите список. Если статус уже «Отозван», выберите запись и нажмите «Повторить отзыв», чтобы завершить операцию без выдачи нового токена."); }
                });
            };
            reissue.Click += async delegate {
                var selected = list.Selected;
                ReadPending(false); UpdateActions();
                if (selected == null || needsRefresh || working || recoveryOnly || recoveryBlocked) return;
                // Snapshot identity before awaiting. The editable new-invitation name
                // must never redirect a selected friend's reissue.
                var id = selected.Id; var label = selected.Name;
                if (!Confirm(Identity(selected) + "\r\nСтарый токен перестанет работать; соединения этого доступа будут закрыты. Затем будет создан новый токен с тем же именем. Другу потребуется заменить токен в ProGo и заново установить профиль телефона. Остальные приглашения не изменятся.\r\nЕсли выдача не завершится, старый доступ не восстановится. Продолжить?")) return;
                await RunAsync(async delegate {
                    var result = await HomeInvitationReissue.RunAsync(id, label, admin, value => status.Text = value);
                    status.Text = result.Message;
                    if (result.State != InvitationReissueState.RevokeUnconfirmed) selected.Revoked = true;
                    list.RefreshSelection();
                    if (result.State == InvitationReissueState.Complete) {
                        list.Add(result.Replacement); ShowToken(result.Token);
                    } else RequireRefresh(result.Message);
                });
            };
            ReadPending(true); UpdateActions(); UiTheme.ConfigureKeyboardOrder(this);
        }
        private static string Identity(HomeVpnInvitation item) {
            return "Доступ «" + item.Name + "»\r\nID: " + item.Id + "\r\nСоздан: " + item.CreatedText;
        }
        private void UpdateActions() {
            list.Enabled = !working;
            name.Enabled = !working && !recoveryOnly && !recoveryBlocked;
            nameLabel.Text = !recoveryOnly ? "Имя для нового приглашения (перевыпуск сохраняет имя выбранного друга)" : "Имя друга в сохранённом запросе";
            name.AccessibleName = !recoveryOnly ? "Имя нового приглашения" : "Имя друга в сохранённом запросе";
            name.AccessibleDescription = !recoveryOnly
                ? "Имя для отдельного нового приглашения. Перевыпуск сохраняет имя выбранного друга, а не этот текст."
                : "Исходное имя сохранённого запроса. Во время проверки его нельзя изменить; другой доступ не создаётся.";
            refresh.Enabled = close.Enabled = !working;
            create.Text = !recoveryOnly ? "Создать отдельный токен" : "Проверить выдачу токена";
            create.AccessibleDescription = !recoveryOnly
                ? "Создаёт отдельное приглашение на VPS и показывает новый личный токен. Другие доступы сохраняются."
                : "Проверяет сохранённый исходный запрос и получает тот же токен, если доступ активен. Не создаёт другое приглашение.";
            create.Enabled = !working && !recoveryBlocked && (recoveryOnly || !needsRefresh);
            revoke.Enabled = !working && !needsRefresh && !recoveryOnly && !recoveryBlocked && list.Selected != null;
            revoke.Text = list.Selected != null && list.Selected.Revoked ? "Повторить отзыв" : "Отозвать выбранный доступ";
            revoke.AccessibleDescription = list.Selected != null && list.Selected.Revoked
                ? "После подтверждения повторно завершает отзыв выбранного ID на VPS. Новый токен не создаётся."
                : "После подтверждения отзывает выбранный ID на VPS и закрывает соединения этого приглашения. Другие доступы сохраняются.";
            reissue.Enabled = !working && !needsRefresh && !recoveryOnly && !recoveryBlocked && list.Selected != null;
        }
        private void ReadPending(bool opening) {
            if (owner == null) return;
            try {
                pending = HomeVpnInvitationRecovery.Load(owner); recoveryBlocked = false;
                if (pending != null) {
                    recoveryOnly = true;
                    name.Text = pending.Name;
                    if (opening) status.Text = HomeVpnInvitationRecovery.PendingMessage;
                } else if (recoveryOnly) { recoveryBlocked = true; status.Text = "Исходный запрос выдачи исчез. Создание другого доступа заблокировано; восстановите сохранённый запрос перед проверкой."; }
            } catch (HomeVpnInvitationPendingException ex) { pending = null; recoveryBlocked = true; status.Text = ex.Message; }
        }
        private void RequireRefresh(string message) { needsRefresh = true; status.Text = message; }
        private async Task RunAsync(Func<Task> action) {
            if (working) return;
            working = true; status.Text = "Выполнение операции с доступом…"; UpdateActions();
            try { await action(); }
            catch (HomeVpnInvitationPendingException ex) { RequireRefresh(ex.Message); }
            catch { RequireRefresh("Операция не завершена. Обновите список перед следующим изменением доступа. Подробности SSH доступны в его окне."); }
            finally { working = false; ReadPending(false); UpdateActions(); }
        }
        private async Task RefreshAsync() {
            status.Text = "Получение списка с VPS…";
            try {
                var items = new JavaScriptSerializer().Deserialize<HomeVpnInvitation[]>(await admin("list", null, null));
                if (items == null) throw new InvalidOperationException();
                list.Replace(items); needsRefresh = false;
                status.Text = recoveryOnly || recoveryBlocked
                    ? "Список обновлён. Сохранённый запрос выдачи остаётся незавершённым: проверка списка не подтверждает получение токена. Нажмите «Проверить выдачу токена»."
                    : "Список обновлён. Проверьте дату и полный ID: одинаковое имя не означает тот же доступ. При потерянном ответе перевыпуска отзовите ненужные новые записи перед созданием ещё одного токена.";
            } catch { RequireRefresh("Не удалось обновить список. Изменения доступа заблокированы: повторите «Обновить список» после восстановления SSH."); }
        }
        private async Task CreateAsync() {
            status.Text = "Создание отдельного токена…";
            try {
                var label = pending == null ? name.Text : pending.Name;
                var token = await admin(owner == null ? "invite" : !recoveryOnly ? "create-invite" : "recover-invite", label, null);
                var access = HomeVpnAccess.Parse(token);
                list.Add(new HomeVpnInvitation { Id = access.InviteId, Name = label });
                needsRefresh = false;
                status.Text = "Токен готов. Сохраните его для передачи другу.";
                trackedToken = owner != null;
                try { ShowToken(token); } finally { trackedToken = false; }
                if (owner != null) status.Text = HomeVpnInvitationRecovery.Load(owner) != null
                    ? "Запрос и токен сохранены в ProGo. Нажмите «Проверить выдачу токена», чтобы получить исходный результат и подтвердить его сохранение."
                    : "Локальное завершение выдачи подтверждено. Передайте сохранённый токен другу.";
            } catch (HomeVpnInvitationPendingException ex) { RequireRefresh(ex.Message); }
            catch (HomeVpnPreparationCancelledException ex) { status.Text = ex.Message; }
            catch {
                RequireRefresh(owner == null
                    ? "Ответ выдачи не получен или не прошёл проверку. VPS мог создать приглашение. Обновите список и проверьте новые ID; отзовите ненужные записи. Не повторяйте создание вслепую."
                    : "Операция выдачи не завершена. Проверьте SSH и сохранённый запрос перед повтором. Подробности доступны в окне SSH.");
            }
        }
    }
}

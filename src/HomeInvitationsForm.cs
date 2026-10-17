using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace ProGo
{
    internal sealed class HomeInvitationsForm : ProGoForm
    {
        private readonly OwnerUiDispatcher ownerUi = new OwnerUiDispatcher();
        private readonly HomeInvitationList list;
        private readonly TextBox name = new TextBox { Width = 740, Text = "Друг", MaxLength = 80, AccessibleName = "Имя нового приглашения" };
        private readonly Button create = new Button { AutoSize = true, Text = "Создать отдельный токен" };
        private readonly Button revoke = new Button { AutoSize = true, Text = "Отозвать выбранный доступ" };
        private readonly Button reissue = new Button { AutoSize = true, Text = "Перевыпустить потерянный токен" };
        private readonly Button refresh = new Button { AutoSize = true, Text = "Обновить список" };
        private readonly Button recover = new Button { AutoSize = true, Text = "Проверить прежнюю выдачу" };
        private readonly Button close = new Button { AutoSize = true, Text = "Закрыть", MinimumSize = new Size(230, 38), DialogResult = DialogResult.Cancel };
        private readonly Label status = new Label { AutoSize = true, MaximumSize = new Size(740, 0), AccessibleName = "Результат операции с доступом" };
        private readonly Func<string, string, string, Task<string>> admin;
        private volatile bool working;
        private bool needsRefresh;
        private bool pendingAdmin, pendingUnreadable;
        internal Func<string, bool> Confirm;
        internal Action<string> ShowToken;

        internal HomeInvitationsForm(HomeVpnInvitation[] items, Func<string, string, string, Task<string>> admin, ClipboardService clipboard)
        {
            this.admin = admin;
            Text = "Доступ друзей"; ClientSize = new Size(800, 760); StartPosition = FormStartPosition.CenterParent;
            var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };
            list = new HomeInvitationList(items) { Width = 740, Height = 365 };
            panel.Controls.Add(list);
            panel.Controls.Add(new Label { AutoSize = true, Text = "Имя для нового приглашения (перевыпуск сохраняет имя выбранного друга)" });
            name.AccessibleDescription = "Имя для отдельного нового приглашения. Перевыпуск сохраняет имя выбранного друга, а не этот текст.";
            create.AccessibleDescription = "Создаёт отдельное приглашение на VPS и показывает новый личный токен. Другие доступы сохраняются.";
            reissue.AccessibleDescription = "После подтверждения отзывает выбранный ID и выдаёт новый токен с тем же именем. Старый доступ не восстанавливается при ошибке выдачи.";
            refresh.AccessibleDescription = "Получает текущий список с VPS. Не снимает блокировку сохранённого запроса выдачи. Не создаёт и не отзывает приглашения.";
            recover.AccessibleDescription = "Проверяет сохранённый запрос выдачи и получает тот же токен. Не создаёт новый доступ, не повторяет отзыв и не восстанавливает отозванный токен.";
            close.AccessibleDescription = "Закрывает окно без изменения доступа друзей. Пока операция выполняется, закрытие недоступно.";
            status.AccessibleDescription = status.Text; status.TextChanged += delegate { status.AccessibleDescription = status.Text; };
            close.Click += delegate { Close(); }; CancelButton = close;
            panel.Controls.Add(name); panel.Controls.Add(create); panel.Controls.Add(revoke); panel.Controls.Add(reissue); panel.Controls.Add(refresh); panel.Controls.Add(recover);
            foreach (Control input in new Control[] { list, name, create, revoke, reissue, refresh, recover }) {
                var target = input; target.Enter += delegate { panel.ScrollControlIntoView(target); };
            }
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = Padding.Empty };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var footer = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = false, FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(16, 0, 16, 12), Margin = Padding.Empty };
            footer.Controls.Add(status); footer.Controls.Add(close); layout.Controls.Add(panel, 0, 0); layout.Controls.Add(footer, 0, 1); Controls.Add(layout);
            Confirm = text => MessageBox.Show(this, text, "Изменение доступа", MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes;
            ShowToken = token => {
                using (var dialog = HomeInvitationList.TokenDialog(token, clipboard)) {
                    bool intentional = false;
                    dialog.FormClosing += delegate(object sender, FormClosingEventArgs e) { if (!e.Cancel && e.CloseReason == CloseReason.UserClosing) intentional = true; };
                    dialog.ShowDialog(this);
                    if (!intentional) throw new HomeVpnAdminPendingException("Передача токена не завершена. Запрос сохранён; можно проверить прежнюю выдачу без нового доступа.");
                }
            };
            list.SelectionChanged += delegate { UpdateActions(); };
            FormClosing += delegate(object sender, FormClosingEventArgs e) { if (working) e.Cancel = true; };
            refresh.Click += async delegate { await RunAsync(RefreshAsync); };
            recover.Click += async delegate { await RunAsync(RecoverAsync); };
            create.Click += async delegate { await RunAsync(CreateAsync); };
            revoke.Click += async delegate {
                var selected = list.Selected;
                if (selected == null || needsRefresh || pendingAdmin || working) return;
                if (!Confirm(Identity(selected) + "\r\nОтозвать этот доступ? Его соединения будут закрыты.")) return;
                await RunAsync(async delegate {
                    try {
                        if (await ownerUi.Await(admin("revoke", null, selected.Id)) != "Access revoked.") throw new InvalidOperationException();
                        if (ownerUi.Closed || IsDisposed || Disposing) return;
                        selected.Revoked = true; list.RefreshSelection(); status.Text = "Выбранный доступ отозван."; }
                    catch (HomeVpnPreparationCancelledException ex) { if (!ownerUi.Closed && !IsDisposed && !Disposing) status.Text = ex.Message; }
                    catch (HomeVpnOwnerUnconfirmedException ex) { if (!ownerUi.Closed && !IsDisposed && !Disposing) RequireRefresh(ex.Message + " Обновите список и выберите тот же ID для явного повторного отзыва; запись «Отозван» ещё не подтверждает завершение всех его действий."); }
                    catch { if (!ownerUi.Closed && !IsDisposed && !Disposing) RequireRefresh("Завершение отзыва не подтверждено. Обновите список. Если статус уже «Отозван», выберите запись и нажмите «Повторить отзыв», чтобы завершить операцию без выдачи нового токена."); }
                });
            };
            reissue.Click += async delegate {
                var selected = list.Selected;
                if (selected == null || needsRefresh || pendingAdmin || working) return;
                // Snapshot identity before awaiting. The editable new-invitation name
                // must never redirect a selected friend's reissue.
                var id = selected.Id; var label = selected.Name;
                if (!Confirm(Identity(selected) + "\r\nСтарый токен перестанет работать; соединения этого доступа будут закрыты. Затем будет создан новый токен с тем же именем. Другу потребуется заменить токен в ProGo и заново установить профиль телефона. Остальные приглашения не изменятся.\r\nЕсли выдача не завершится, старый доступ не восстановится. Продолжить?")) return;
                await RunAsync(async delegate {
                    var result = await ownerUi.Await(HomeInvitationReissue.RunAsync(id, label, admin, value => ownerUi.PostUpdate(delegate { if (!ownerUi.Closed && !IsDisposed && !Disposing) status.Text = value; })));
                    if (ownerUi.Closed || IsDisposed || Disposing) return;
                    status.Text = result.Message;
                    if (result.State != InvitationReissueState.RevokeUnconfirmed) selected.Revoked = true;
                    list.RefreshSelection();
                    if (result.State == InvitationReissueState.Complete) {
                        list.Add(result.Replacement); ShowToken(result.Token); HomeVpnAdminRecovery.ConfirmConsumed(result.Token);
                    } else RequireRefresh(result.State == InvitationReissueState.IssueUnconfirmed && HomeVpnAdminRecovery.HasPending()
                        ? "Старый доступ отозван; выдача замены не подтверждена. " + HomeVpnAdminRecovery.PendingMessage : result.Message);
                });
            };
            RefreshPending();
            if (pendingAdmin) status.Text = pendingUnreadable ? "Запрос прежней выдачи недоступен. Новые изменения доступа заблокированы; существующие записи сохранены."
                : HomeVpnAdminRecovery.PendingMessage;
            UpdateActions(); UiTheme.ConfigureKeyboardOrder(this);
        }
        private static string Identity(HomeVpnInvitation item) {
            return "Доступ «" + item.Name + "»\r\nID: " + item.Id + "\r\nСоздан: " + item.CreatedText;
        }
        private void UpdateActions() {
            list.Enabled = name.Enabled = !working;
            refresh.Enabled = close.Enabled = !working;
            create.Enabled = !working && !needsRefresh && !pendingAdmin;
            revoke.Enabled = !working && !needsRefresh && !pendingAdmin && list.Selected != null;
            recover.Enabled = !working && pendingAdmin && !pendingUnreadable; recover.Visible = pendingAdmin;
            revoke.Text = list.Selected != null && list.Selected.Revoked ? "Повторить отзыв" : "Отозвать выбранный доступ";
            revoke.AccessibleDescription = list.Selected != null && list.Selected.Revoked
                ? "После подтверждения повторно завершает отзыв выбранного ID на VPS. Новый токен не создаётся."
                : "После подтверждения отзывает выбранный ID на VPS и закрывает соединения этого приглашения. Другие доступы сохраняются.";
            reissue.Enabled = !working && !needsRefresh && !pendingAdmin && list.Selected != null;
        }
        private void RefreshPending() {
            try { pendingAdmin = HomeVpnAdminRecovery.HasPending(); pendingUnreadable = pendingAdmin && HomeVpnAdminRecovery.Pending() == null; }
            catch (HomeVpnAdminPendingException) { pendingAdmin = pendingUnreadable = true; }
        }
        private void RequireRefresh(string message) { needsRefresh = true; status.Text = message; }
        private async Task RunAsync(Func<Task> action) {
            if (working || ownerUi.Closed || IsDisposed || Disposing) return;
            working = true; status.Text = "Выполнение операции с доступом…"; UpdateActions();
            try { await ownerUi.Await(action()); }
            catch (HomeVpnAdminPendingException ex) { if (!ownerUi.Closed && !IsDisposed && !Disposing) RequireRefresh(ex.Message); }
            catch (HomeVpnPreparationCancelledException ex) { if (!ownerUi.Closed && !IsDisposed && !Disposing) status.Text = ex.Message; }
            catch { if (!ownerUi.Closed && !IsDisposed && !Disposing) RequireRefresh("Операция не завершена. Обновите список перед следующим изменением доступа. Подробности SSH доступны в его окне."); }
            finally { working = false; if (!ownerUi.Closed && !IsDisposed && !Disposing) { RefreshPending(); UpdateActions(); } }
        }
        private async Task RefreshAsync() {
            status.Text = "Получение списка с VPS…";
            try {
                var items = new JavaScriptSerializer().Deserialize<HomeVpnInvitation[]>(await ownerUi.Await(admin("list", null, null)));
                if (items == null) throw new InvalidOperationException();
                if (ownerUi.Closed || IsDisposed || Disposing) return;
                RefreshPending(); list.Replace(items); needsRefresh = pendingAdmin;
                status.Text = "Список обновлён. Проверьте дату и полный ID: одинаковое имя не означает тот же доступ. При потерянном ответе выдачи отзовите ненужные новые записи перед созданием ещё одного токена.";
                if (pendingAdmin) status.Text = "Список обновлён. Сохранённый запрос выдачи ещё не завершён: этот список не разрешает новую выдачу. Проверьте прежний запрос.";
            } catch (HomeVpnListCancelledException ex) {
                // Cancellation of a read-only query adds no new uncertainty and
                // must not clear an existing mutation/reconciliation gate.
                if (!ownerUi.Closed && !IsDisposed && !Disposing) status.Text = ex.Message;
            } catch (HomeVpnLocalCleanupException ex) {
                if (!ownerUi.Closed && !IsDisposed && !Disposing) RequireRefresh(ex.Message);
            } catch { if (!ownerUi.Closed && !IsDisposed && !Disposing) RequireRefresh("Не удалось обновить список. Изменения доступа заблокированы: повторите «Обновить список» после восстановления SSH."); }
        }
        private async Task CreateAsync() {
            status.Text = "Создание отдельного токена…";
            try {
                var label = name.Text;
                var token = await ownerUi.Await(admin("invite", label, null));
                if (ownerUi.Closed || IsDisposed || Disposing) return;
                var access = HomeVpnAccess.Parse(token);
                list.Add(new HomeVpnInvitation { Id = access.InviteId, Name = label });
                status.Text = "Токен создан. Передайте его другу до закрытия окна токена.";
                ShowToken(token); HomeVpnAdminRecovery.ConfirmConsumed(token);
            } catch (HomeVpnAdminPendingException ex) { if (!ownerUi.Closed && !IsDisposed && !Disposing) RequireRefresh(ex.Message); }
            catch (HomeVpnPreparationCancelledException ex) { if (!ownerUi.Closed && !IsDisposed && !Disposing) status.Text = ex.Message; }
            catch { if (!ownerUi.Closed && !IsDisposed && !Disposing) RequireRefresh(HomeVpnAdminRecovery.HasPending() ? HomeVpnAdminRecovery.PendingMessage
                : "Ответ выдачи не получен или не прошёл проверку. VPS мог создать приглашение. Обновите список и проверьте новые ID; отзовите ненужные записи. Не повторяйте создание вслепую."); }
        }
        private async Task RecoverAsync() {
            status.Text = "Проверка прежней выдачи без нового приглашения…";
            try {
                var request = HomeVpnAdminRecovery.Pending();
                if (request == null) throw new HomeVpnAdminPendingException("Сохранённый запрос выдачи отсутствует. Новая выдача не запускалась.");
                var token = await ownerUi.Await(admin("recover-invite", null, null)); var access = HomeVpnAccess.Parse(token);
                if (ownerUi.Closed || IsDisposed || Disposing) return;
                list.Add(new HomeVpnInvitation { Id = access.InviteId, Name = request.Name }); ShowToken(token);
                HomeVpnAdminRecovery.ConfirmConsumed(token); needsRefresh = true;
                status.Text = "Прежний токен получен без новой выдачи. Обновите список перед следующим изменением доступа.";
            } catch (HomeVpnAdminPendingException ex) { if (!ownerUi.Closed && !IsDisposed && !Disposing) RequireRefresh(ex.Message); }
            catch { if (!ownerUi.Closed && !IsDisposed && !Disposing) RequireRefresh("Результат прежней выдачи не получен. Запрос сохранён; новый доступ и повторный отзыв не запрашивались."); }
        }
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            ownerUi.Close(); base.OnFormClosed(e);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) ownerUi.Dispose();
            base.Dispose(disposing);
        }

    }
}

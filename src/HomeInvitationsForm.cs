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
        private readonly Button create = new Button { AutoSize = true, Text = "Создать отдельный токен" };
        private readonly Button revoke = new Button { AutoSize = true, Text = "Отозвать выбранный доступ" };
        private readonly Button reissue = new Button { AutoSize = true, Text = "Перевыпустить потерянный токен" };
        private readonly Button refresh = new Button { AutoSize = true, Text = "Обновить список" };
        private readonly Label status = new Label { AutoSize = true, MaximumSize = new Size(740, 0), AccessibleName = "Результат операции с доступом" };
        private readonly Func<string, string, string, Task<string>> admin;
        private bool working, needsRefresh;
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
            panel.Controls.Add(name); panel.Controls.Add(create); panel.Controls.Add(revoke); panel.Controls.Add(reissue); panel.Controls.Add(refresh); panel.Controls.Add(status);
            Controls.Add(panel);
            Confirm = text => MessageBox.Show(this, text, "Изменение доступа", MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes;
            ShowToken = token => { using (var dialog = HomeInvitationList.TokenDialog(token, clipboard)) dialog.ShowDialog(this); };
            list.SelectionChanged += delegate { UpdateActions(); };
            FormClosing += delegate(object sender, FormClosingEventArgs e) { if (working) e.Cancel = true; };
            refresh.Click += async delegate { await RunAsync(RefreshAsync); };
            create.Click += async delegate { await RunAsync(CreateAsync); };
            revoke.Click += async delegate {
                var selected = list.Selected;
                if (selected == null || selected.Revoked || needsRefresh || working) return;
                if (!Confirm(Identity(selected) + "\r\nОтозвать этот доступ? Его соединения будут закрыты.")) return;
                await RunAsync(async delegate {
                    try { await admin("revoke", null, selected.Id); selected.Revoked = true; list.RefreshSelection(); status.Text = "Выбранный доступ отозван."; }
                    catch { RequireRefresh("Завершение отзыва не подтверждено. Обновите список. Если статус уже «Отозван», повторный перевыпуск сначала завершит отзыв, затем создаст новый токен."); }
                });
            };
            reissue.Click += async delegate {
                var selected = list.Selected;
                if (selected == null || needsRefresh || working) return;
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
            UpdateActions(); UiTheme.ConfigureKeyboardOrder(this);
        }
        private static string Identity(HomeVpnInvitation item) {
            return "Доступ «" + item.Name + "»\r\nID: " + item.Id + "\r\nСоздан: " + item.CreatedText;
        }
        private void UpdateActions() {
            list.Enabled = name.Enabled = !working;
            refresh.Enabled = !working;
            create.Enabled = !working && !needsRefresh;
            revoke.Enabled = !working && !needsRefresh && list.CanRevoke;
            reissue.Enabled = !working && !needsRefresh && list.Selected != null;
        }
        private void RequireRefresh(string message) { needsRefresh = true; status.Text = message; }
        private async Task RunAsync(Func<Task> action) {
            if (working) return;
            working = true; UpdateActions();
            try { await action(); }
            catch { RequireRefresh("Операция не завершена. Обновите список перед следующим изменением доступа. Подробности SSH доступны в его окне."); }
            finally { working = false; UpdateActions(); }
        }
        private async Task RefreshAsync() {
            status.Text = "Получение списка с VPS…";
            try {
                var items = new JavaScriptSerializer().Deserialize<HomeVpnInvitation[]>(await admin("list", null, null));
                if (items == null) throw new InvalidOperationException();
                list.Replace(items); needsRefresh = false;
                status.Text = "Список обновлён. Проверьте дату и полный ID: одинаковое имя не означает тот же доступ. При потерянном ответе выдачи отзовите ненужные новые записи перед созданием ещё одного токена.";
            } catch { RequireRefresh("Не удалось обновить список. Изменения доступа заблокированы: повторите «Обновить список» после восстановления SSH."); }
        }
        private async Task CreateAsync() {
            status.Text = "Создание отдельного токена…";
            try {
                var label = name.Text;
                var token = await admin("invite", label, null);
                var access = HomeVpnAccess.Parse(token);
                list.Add(new HomeVpnInvitation { Id = access.InviteId, Name = label });
                status.Text = "Токен создан. Передайте его другу до закрытия окна токена.";
                ShowToken(token);
            } catch { RequireRefresh("Ответ выдачи не получен или не прошёл проверку. VPS мог создать приглашение. Обновите список и проверьте новые ID; отзовите ненужные записи. Не повторяйте создание вслепую."); }
        }
    }
}

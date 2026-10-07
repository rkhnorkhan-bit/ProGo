using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        [DllImport("user32.dll", EntryPoint = "SendMessageW")]
        private static extern IntPtr InvitationListKey(IntPtr window, int message, IntPtr key, IntPtr data);

        // Real dialog keys start at the focused child. Calling Form directly skips
        // traversal inside the nested HomeInvitationList ContainerControl.
        private static void FriendKeyboardWalk(Form form, Control[] controls, string name)
        {
            Check(controls[0].Focus(), name + " first field accepts focus");
            var dialogKey = typeof(Control).GetMethod("ProcessDialogKey", PrivateInstance);
            Action<Keys> send = value => {
                var focused = Descendants(form).Single(c => c.Focused);
                dialogKey.Invoke(focused, new object[] { value }); Application.DoEvents();
            };
            for (int i = 1; i < controls.Length; i++) {
                send(Keys.Tab);
                Check(controls[i].ContainsFocus, name + " Tab reaches " + controls[i].GetType().Name + " " + controls[i].AccessibilityObject.Name);
            }
            for (int i = controls.Length - 2; i >= 0; i--) {
                send(Keys.Tab | Keys.Shift);
                Check(controls[i].ContainsFocus, name + " Shift+Tab returns to " + controls[i].GetType().Name + " " + controls[i].AccessibilityObject.Name);
            }
        }

        private static void FriendsAccessibility(SettingsService settings)
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") {
                Console.WriteLine("SKIP: friends keyboard fixtures require isolated native CI"); return;
            }
            bool existed = Directory.Exists(HomeVpnPrivateFiles.Root); var before = WizardPrivateSnapshot();
            var key = typeof(Form).GetMethod("ProcessDialogKey", PrivateInstance);
            var first = new HomeVpnInvitation { Id = new string('a', 24), Name = "Друг", Created = "2026-01-01T00:00:00Z" };
            var second = new HomeVpnInvitation { Id = new string('b', 24), Name = "Друг", Revoked = true };
            var calls = new List<string>(); var gate = new TaskCompletionSource<string>(); bool failedList = true;
            using (var clipboard = new ClipboardService(settings)) {
                using (var form = new HomeInvitationsForm(new[] { first, second }, (action, label, id) => {
                    calls.Add(action);
                    if (action == "revoke") { Check(id == first.Id, "friends revoke retains the explicitly selected identity despite duplicate names"); return gate.Task; }
                    if (action != "list") throw new Exception("Unexpected fixture mutation");
                    if (failedList) throw new IOException("fixture-private-detail");
                    return Task.FromResult(new JavaScriptSerializer().Serialize(new[] {
                        new HomeVpnInvitation { Id = first.Id, Name = first.Name, Created = first.Created, Revoked = true }, second }));
                }, clipboard)) {
                    form.ShowToken = value => { throw new Exception("Keyboard navigation must not issue tokens"); };
                    form.Show(); Application.DoEvents();
                    var view = (HomeInvitationList)Field(form, "list"); var search = (TextBox)Field(view, "search"); var rows = (ListBox)Field(view, "list");
                    var details = (Label)Field(view, "details"); var count = (Label)Field(view, "count"); var name = (TextBox)Field(form, "name");
                    var create = (Button)Field(form, "create"); var revoke = (Button)Field(form, "revoke"); var reissue = (Button)Field(form, "reissue");
                    var refresh = (Button)Field(form, "refresh"); var close = (Button)form.CancelButton; var status = (Label)Field(form, "status");
                    Action<string> walk = state => FriendKeyboardWalk(form, new Control[] { search, rows, name, create, revoke, reissue, refresh, close }.Where(c => c.Enabled).ToArray(), "friends " + state);
                    Check(form.AcceptButton == null && !revoke.Enabled && !reissue.Enabled && view.Selected == null, "friends starts without implicit mutation or destructive selection");
                    Check(search.AccessibilityObject.Description.Contains("без изменения") && rows.AccessibilityObject.Description.Contains("Стрелки") &&
                        name.AccessibilityObject.Description.Contains("а не этот текст"), "friend search, selection and new-name field explain independent effects");
                    walk("no selection"); name.Text = "Черновик нового имени"; name.Focus(); key.Invoke(form, new object[] { Keys.Enter }); Application.DoEvents();
                    Check(calls.Count == 0, "friends Enter in name field does not create an invitation");
                    rows.SelectedIndex = -1; rows.Focus(); InvitationListKey(rows.Handle, 0x100, new IntPtr(0x28), new IntPtr(1)); Application.DoEvents();
                    Check(view.Selected == first && details.AccessibilityObject.Description == details.Text && details.Text.Contains(first.Id) && revoke.Enabled && reissue.Enabled,
                        "native friends Down selects first identity and updates current full-ID details");
                    InvitationListKey(rows.Handle, 0x100, new IntPtr(0x28), new IntPtr(1)); Application.DoEvents();
                    Check(view.Selected == second && revoke.AccessibilityObject.Name == "Повторить отзыв" && revoke.AccessibilityObject.Description.Contains("повторно завершает") &&
                        details.AccessibilityObject.Description.Contains("Отозван"), "native friends Down distinguishes revoked duplicate and names repeat-revoke effect");
                    InvitationListKey(rows.Handle, 0x100, new IntPtr(0x26), new IntPtr(1)); Application.DoEvents();
                    Check(view.Selected == first && revoke.AccessibilityObject.Name == "Отозвать выбранный доступ" &&
                        revoke.AccessibilityObject.Description.Contains("закрывает соединения"), "native friends Up restores active identity and current revoke wording");
                    walk("selected"); search.Text = "нет такого друга";
                    Check(rows.Items.Count == 0 && view.Selected == null && !revoke.Enabled && !reissue.Enabled && count.AccessibilityObject.Description == count.Text &&
                        count.Text.StartsWith("Совпадений нет") && name.Text == "Черновик нового имени", "friend empty results expose corrective text without changing draft or access");
                    walk("empty search"); search.Text = ""; rows.SelectedIndex = 0;
                    Check(count.AccessibilityObject.Description == count.Text && count.Text == "Показано: 2 из 2", "friend count description follows the current search");
                    string confirmation = null; form.Confirm = text => { confirmation = text; return false; };
                    reissue.PerformClick(); revoke.PerformClick(); Application.DoEvents();
                    Check(calls.Count == 0 && confirmation.Contains(first.Id) && !first.Revoked && second.Revoked,
                        "cancelled friend confirmations retain both identities and make no requests");
                    form.Confirm = text => true; revoke.PerformClick(); Application.DoEvents();
                    Check(calls.SequenceEqual(new[] { "revoke" }) && !close.Enabled && Descendants(form).Where(c => c.TabStop).All(c => !c.Enabled) &&
                        status.AccessibilityObject.Description == "Выполнение операции с доступом…", "pending friend operation exposes progress and skips every keyboard action");
                    form.Close(); Application.DoEvents(); Check(form.Visible && !form.IsDisposed, "friend pending-close guard retains the operation window");
                    gate.SetException(new IOException("fixture-private-detail")); PumpUntil(() => refresh.Enabled);
                    Check(!create.Enabled && !revoke.Enabled && !reissue.Enabled && close.Enabled && status.Text.Contains("Обновите список") &&
                        status.AccessibilityObject.Description == status.Text && !status.Text.Contains("fixture-private"), "uncertain revoke exposes current correction and blocks mutations until reconciliation");
                    walk("uncertain revoke"); close.Focus(); Application.DoEvents();
                    Check(close.Parent.ClientRectangle.Contains(close.Bounds) && status.Parent.ClientRectangle.Contains(status.Bounds),
                        "friend corrective result and Close stay fully visible outside scrolling actions");
                    Shot(form, "keyboard-friends-uncertain"); refresh.PerformClick(); Application.DoEvents();
                    Check(!create.Enabled && close.Enabled && status.AccessibilityObject.Description == status.Text && status.Text.StartsWith("Не удалось обновить список"),
                        "failed friend refresh retains named error and read-only mutation gate");
                    walk("failed reconciliation"); failedList = false; refresh.PerformClick(); Application.DoEvents();
                    Check(create.Enabled && view.Selected == null && rows.Items.Count == 2 && status.AccessibilityObject.Description == status.Text &&
                        name.Text == "Черновик нового имени", "successful friend refresh restores creation without choosing an identity or losing draft");
                    rows.SelectedIndex = 0; walk("reconciled revoked selection");
                    Check(revoke.AccessibilityObject.Name == "Повторить отзыв" && revoke.AccessibilityObject.Description.Contains("Новый токен не создаётся") &&
                        status.AccessibilityObject.Name == "Результат операции с доступом" && create.AccessibilityObject.Description.Contains("Другие доступы сохраняются") &&
                        reissue.AccessibilityObject.Description.Contains("Старый доступ не восстанавливается"), "friend action names and descriptions follow reconciled evidence and explain scope");
                    Shot(form, "keyboard-friends-reconciled"); close.Focus(); key.Invoke(form, new object[] { Keys.Enter }); Application.DoEvents();
                    Check(!form.Visible && calls.SequenceEqual(new[] { "revoke", "list", "list" }) && !first.Revoked && second.Revoked,
                        "friends Enter on Close neither reissues nor mutates another invitation");
                }
                using (var empty = new HomeInvitationsForm(new HomeVpnInvitation[0], (a,n,i) => { throw new Exception("No empty-view request expected"); }, clipboard)) {
                    empty.Show(); Application.DoEvents(); var view = (HomeInvitationList)Field(empty, "list"); var count = (Label)Field(view, "count");
                    Check(count.AccessibilityObject.Description == count.Text && count.Text.StartsWith("Приглашений пока нет"), "empty friend list names its invitation guidance");
                    ((TextBox)Field(empty, "name")).Focus(); key.Invoke(empty, new object[] { Keys.Escape }); Application.DoEvents();
                    Check(!empty.Visible && empty.DialogResult == DialogResult.Cancel, "empty friends Escape closes without issuing access");
                }
                using (var token = HomeInvitationList.TokenDialog("synthetic-private-invitation", clipboard)) {
                    token.Show(); Application.DoEvents(); var input = Descendants(token).OfType<TextBox>().Single();
                    var copy = Descendants(token).OfType<Button>().Single(b => b.Text == "Скопировать токен"); var close = (Button)token.CancelButton;
                    var notice = Descendants(token).OfType<Label>().Single(l => l.AccessibleName == "Результат копирования токена");
                    Check(input.ReadOnly && input.UseSystemPasswordChar && input.AccessibilityObject.Name == "Личный токен приглашения" &&
                        input.AccessibilityObject.Description.Contains("Только чтение") && token.AcceptButton == null, "friend token has a named masked read-only input without a copy default");
                    Check(copy.AccessibilityObject.Description.Contains(clipboard.CopyNotice) && close.AccessibilityObject.Description.Contains("Повторно показать") &&
                        close.AccessibilityObject.Description.Contains("без отзыва") && notice.AccessibilityObject.Description == notice.Text,
                        "friend token actions explain one-time display, dismissal and the current clipboard policy");
                    Check(Descendants(token).All(c => !(c.AccessibilityObject.Name ?? "").Contains(input.Text) && !(c.AccessibilityObject.Description ?? "").Contains(input.Text)),
                        "friend token metadata never includes the masked secret");
                    FriendKeyboardWalk(token, new Control[] { input, copy, close }, "friend token");
                    input.Focus(); key.Invoke(token, new object[] { Keys.Enter }); Application.DoEvents();
                    Check(notice.Text == clipboard.CopyNotice && token.Visible, "friend token Enter in read-only field does not copy or close");
                    copy.Focus(); Application.DoEvents(); Check(copy.Parent.ClientRectangle.Contains(copy.Bounds), "friend token copy is scrolled fully into view for keyboard focus");
                    close.Focus(); Application.DoEvents(); Check(close.Parent.ClientRectangle.Contains(close.Bounds) && notice.Parent.ClientRectangle.Contains(notice.Bounds),
                        "friend token Close and clipboard notice remain fully visible outside scrolling content");
                    Shot(token, "keyboard-friend-token"); key.Invoke(token, new object[] { Keys.Escape }); Application.DoEvents();
                    Check(!token.Visible && token.DialogResult == DialogResult.Cancel, "friend token Escape closes without copying or revoking access");
                }
            }
            var after = WizardPrivateSnapshot();
            Check(existed == Directory.Exists(HomeVpnPrivateFiles.Root) && before.Keys.OrderBy(p => p).SequenceEqual(after.Keys.OrderBy(p => p)) &&
                before.All(pair => after[pair.Key].SequenceEqual(pair.Value)), "friends keyboard fixtures leave private access files unchanged");
        }
    }
}

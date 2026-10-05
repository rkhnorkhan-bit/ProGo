using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace ProGo
{
    internal static class HomeInvitationReissueTests
    {
        private const string OldId = "aaaaaaaaaaaaaaaaaaaaaaaa";
        private const string OtherId = "bbbbbbbbbbbbbbbbbbbbbbbb";
        private static HomeVpnInvitation[] Items() {
            return new[] { new HomeVpnInvitation { Id = OldId, Name = "Друг", Created = "2026-01-01T00:00:00Z" },
                new HomeVpnInvitation { Id = OtherId, Name = "Друг" } };
        }
        internal static void Run(Action<bool, string> check, string token, ClipboardService clipboard, string work)
        {
            var access = HomeVpnAccess.Parse(token);
            var calls = new List<string>();
            var progress = new List<string>();
            var gate = new TaskCompletionSource<string>();
            var pending = HomeInvitationReissue.RunAsync(OldId, "Друг", (action, name, id) => {
                calls.Add(action + ":" + (id ?? name));
                return action == "revoke" ? gate.Task : Task.FromResult(token);
            }, progress.Add);
            check(!pending.IsCompleted && calls.SequenceEqual(new[] { "revoke:" + OldId }), "reissue waits for revoke acknowledgement before issuing anything");
            gate.SetResult("Access revoked."); var done = pending.GetAwaiter().GetResult();
            check(done.State == InvitationReissueState.Complete && done.Token == token && done.Replacement.Id == access.InviteId,
                "reissue returns the validated new token and separate identity");
            check(calls.SequenceEqual(new[] { "revoke:" + OldId, "invite:Друг" }) && progress.Count == 2,
                "reissue performs one revoke then one invite with visible stages");
            check(Items()[1].Id == OtherId && !calls.Any(c => c.Contains(OtherId)), "duplicate names never redirect revocation to the other identity");
            foreach (var failure in new[] { "revoke", "invite", "parse" }) {
                calls.Clear();
                var result = HomeInvitationReissue.RunAsync(OldId, "Друг", (action, name, id) => {
                    calls.Add(action);
                    if (action == failure) throw new IOException("fixture-secret-not-for-ui");
                    return Task.FromResult(action == "revoke" ? "Access revoked." : failure == "parse" ? "fixture-secret-not-for-ui" : token);
                }, delegate { }).GetAwaiter().GetResult();
                check(result.State == (failure == "revoke" ? InvitationReissueState.RevokeUnconfirmed : InvitationReissueState.IssueUnconfirmed)
                    && result.Token == null && result.Replacement == null && !result.Message.Contains("fixture-secret"),
                    "partial failure is explicit without leaking or returning a token: " + failure);
                check(calls.SequenceEqual(failure == "revoke" ? new[] { "revoke" } : new[] { "revoke", "invite" }),
                    "partial failure never silently retries: " + failure);
            }
            calls.Clear();
            var same = HomeInvitationReissue.RunAsync(access.InviteId, "Друг", (a, n, i) => Task.FromResult(a == "revoke" ? "Access revoked." : token), delegate { }).GetAwaiter().GetResult();
            check(same.State == InvitationReissueState.IssueUnconfirmed && same.Token == null, "old identity cannot masquerade as a replacement token");
            var uncertain = HomeInvitationReissue.RunAsync(OldId, "Друг", (a,n,i) => {
                calls.Add(a); return Task.FromResult("unexpected acknowledgement");
            }, delegate { }).GetAwaiter().GetResult();
            check(uncertain.State == InvitationReissueState.RevokeUnconfirmed && calls.SequenceEqual(new[] { "revoke" }),
                "unexpected revoke response cannot authorize issuing a new token");
            calls.Clear();
            var failedGate = new TaskCompletionSource<string>();
            var delayedFailure = HomeInvitationReissue.RunAsync(OldId, "Друг", (a,n,i) => { calls.Add(a); return failedGate.Task; }, delegate { });
            failedGate.SetException(new IOException("fixture-secret-delayed"));
            check(delayedFailure.GetAwaiter().GetResult().State == InvitationReissueState.RevokeUnconfirmed && calls.Count == 1,
                "asynchronous transport failure remains unconfirmed without issuing");
            calls.Clear();
            bool invalid = false;
            try { HomeInvitationReissue.RunAsync("bad", "Друг", (a,n,i) => { calls.Add(a); return Task.FromResult(token); }, delegate { }).GetAwaiter().GetResult(); }
            catch (ArgumentException) { invalid = true; }
            check(invalid && calls.Count == 0, "invalid identity is rejected before any destructive operation");

            Native(check, token, clipboard, work);
        }
        private static void Native(Action<bool, string> check, string token, ClipboardService clipboard, string work)
        {
            var calls = new List<string>(); var revokeGate = new TaskCompletionSource<string>();
            var inviteGate = new TaskCompletionSource<string>(); string shown = null, confirmation = null;
            var items = Items();
            using (var form = new HomeInvitationsForm(items, (a,n,i) => {
                calls.Add(a + ":" + (i ?? n)); return a == "revoke" ? revokeGate.Task : inviteGate.Task;
            }, clipboard)) {
                form.ShowToken = value => shown = value;
                form.Confirm = value => { confirmation = value; return false; };
                form.Show(); Application.DoEvents();
                var list = Controls(form).OfType<ListBox>().Single();
                var reissue = Button(form, "Перевыпустить потерянный токен");
                check(!reissue.Enabled, "native reissue requires explicit selected friend");
                list.SelectedIndex = 0; reissue.PerformClick();
                check(calls.Count == 0 && confirmation.Contains(OldId) && confirmation.Contains("старый доступ не восстановится"),
                    "cancelled native confirmation makes no call and explains identity and irreversibility");
                form.Confirm = value => true;
                Controls(form).OfType<TextBox>().Single(t => t.AccessibleName == "Имя нового приглашения").Text = "Другой";
                reissue.PerformClick(); Application.DoEvents();
                check(!reissue.Enabled && !list.Enabled && Controls(form).OfType<Button>().All(b => !b.Enabled),
                    "native pending reissue freezes selection and all competing actions");
                form.Close(); check(!form.IsDisposed, "closing cannot discard a pending reissue result");
                revokeGate.SetResult("Access revoked."); PumpUntil(() => calls.Count == 2);
                check(calls[1] == "invite:Друг" && shown == null && Labels(form).Contains("Шаг 2 из 2"),
                    "native second stage retains selected name and does not show premature success");
                inviteGate.SetResult(token); PumpUntil(() => shown != null);
                check(shown == token && items[0].Revoked && !items[1].Revoked && list.Items.Count == 3,
                    "native success marks only old selection revoked and presents new token");
                check(((HomeVpnInvitation)list.SelectedItem).Id == HomeVpnAccess.Parse(token).InviteId && reissue.Enabled,
                    "native success selects replacement and restores actions");
                Shot(form, work, "friends-reissue-success.png"); form.Close();
            }
            calls.Clear(); bool failList = true;
            using (var form = new HomeInvitationsForm(Items(), (a,n,i) => {
                calls.Add(a);
                if (a == "invite" || (a == "list" && failList)) throw new IOException("fixture-private-detail");
                return Task.FromResult(a == "list" ? new JavaScriptSerializer().Serialize(new[] {
                    new HomeVpnInvitation { Id = OldId, Name = "Друг", Revoked = true },
                    new HomeVpnInvitation { Id = OtherId, Name = "Друг" },
                    new HomeVpnInvitation { Id = HomeVpnAccess.Parse(token).InviteId, Name = "Друг" } }) : "Access revoked.");
            }, clipboard)) {
                form.Confirm = value => true;
                form.ShowToken = value => { throw new Exception("must not display an uncertain token"); };
                form.Show(); Application.DoEvents();
                var list = Controls(form).OfType<ListBox>().Single(); list.SelectedIndex = 0;
                Button(form, "Перевыпустить потерянный токен").PerformClick(); Application.DoEvents();
                check(Labels(form).Contains("VPS мог создать новую запись") && !Labels(form).Contains("fixture-private-detail")
                    && !Button(form, "Создать отдельный токен").Enabled && !Button(form, "Перевыпустить потерянный токен").Enabled,
                    "lost issue response blocks blind retry and explains possible orphan access");
                Shot(form, work, "friends-reissue-uncertain.png");
                Button(form, "Обновить список").PerformClick(); Application.DoEvents();
                check(!Button(form, "Создать отдельный токен").Enabled && Button(form, "Обновить список").Enabled,
                    "failed reconciliation remains read-only and can be retried");
                failList = false; Button(form, "Обновить список").PerformClick(); Application.DoEvents();
                check(list.Items.Count == 3 && list.SelectedIndex == -1 && Button(form, "Создать отдельный токен").Enabled,
                    "successful explicit refresh shows server identities without choosing a destructive target");
                list.SelectedIndex = 0;
                check(Button(form, "Перевыпустить потерянный токен").Enabled && !Button(form, "Отозвать выбранный доступ").Enabled,
                    "already revoked selection can complete a previously uncertain revoke before reissue");
                check(calls.SequenceEqual(new[] { "revoke", "invite", "list", "list" }), "reconciliation does not create or revoke other invitations");
                form.Close();
            }
            calls.Clear();
            using (var form = new HomeInvitationsForm(Items(), (a,n,i) => { calls.Add(a); throw new IOException(); }, clipboard)) {
                form.Confirm = value => true; form.Show(); Application.DoEvents();
                Controls(form).OfType<ListBox>().Single().SelectedIndex = 0;
                Button(form, "Перевыпустить потерянный токен").PerformClick(); Application.DoEvents();
                check(calls.SequenceEqual(new[] { "revoke" }) && Labels(form).Contains("Новый токен не запрашивался")
                    && !Button(form, "Создать отдельный токен").Enabled, "native lost revoke response neither issues nor offers blind retry");
                form.Close();
            }
        }
        private static Button Button(Control form, string text) { return Controls(form).OfType<Button>().Single(b => b.Text == text); }
        private static string Labels(Control form) { return String.Join(" ", Controls(form).OfType<Label>().Select(l => l.Text)); }
        private static IEnumerable<Control> Controls(Control parent) {
            foreach (Control child in parent.Controls) { yield return child; foreach (var nested in Controls(child)) yield return nested; }
        }
        private static void PumpUntil(Func<bool> done) {
            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (!done() && DateTime.UtcNow < deadline) { Application.DoEvents(); System.Threading.Thread.Sleep(10); }
            if (!done()) throw new Exception("Native invitation workflow did not settle");
            Application.DoEvents();
        }
        private static void Shot(Form form, string work, string file) {
            using (var shot = new Bitmap(form.Width, form.Height)) { form.DrawToBitmap(shot, new Rectangle(Point.Empty, shot.Size)); shot.Save(Path.Combine(work, file)); }
        }
    }
}

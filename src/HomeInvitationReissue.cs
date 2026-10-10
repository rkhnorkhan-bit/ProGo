using System;
using System.Threading.Tasks;

namespace ProGo
{
    internal enum InvitationReissueState { Complete, RevokeUnconfirmed, IssueUnconfirmed }

    internal sealed class InvitationReissueResult
    {
        internal InvitationReissueState State;
        internal string Token;
        internal HomeVpnInvitation Replacement;
        internal string Message {
            get {
                if (State == InvitationReissueState.Complete)
                    return "Старый доступ отозван. Новый токен готов: передайте его другу и заново подключите его ProGo.";
                if (State == InvitationReissueState.RevokeUnconfirmed)
                    return "Завершение отзыва не подтверждено. Новый токен не запрашивался. Обновите список и выберите тот же ID для повторного перевыпуска — даже если запись уже отмечена как отозванная. Сначала ProGo повторно завершит отзыв.";
                return "Старый доступ отозван, но новый токен не получен или не прошёл проверку. VPS мог создать новую запись. Автоматического повтора нет. Обновите список, проверьте новые ID и отзовите ненужные записи. Для записи с потерянным токеном выберите перевыпуск; если новой записи нет — создайте отдельный токен.";
            }
        }
    }

    internal static class HomeInvitationReissue
    {
        // An on-disk Revoked flag alone is not proof that server credential reload
        // finished. Require the existing revoke command's successful acknowledgement.
        internal static async Task<InvitationReissueResult> RunAsync(string id, string name,
            Func<string, string, string, Task<string>> admin, Action<string> progress)
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(id ?? "", @"\A[0-9a-f]{24}\z"))
                throw new ArgumentException("Выберите приглашение с корректным ID.");
            if (name == null || name.Length > 80 || Array.Exists(name.ToCharArray(), Char.IsControl))
                throw new ArgumentException("Имя приглашения: до 80 символов без управляющих знаков.");
            progress("Шаг 1 из 2: отзыв старого доступа…");
            try {
                if (await admin("revoke", null, id) != "Access revoked.") throw new InvalidOperationException();
            }
            catch (HomeVpnPreparationCancelledException) { throw; }
            catch { return new InvitationReissueResult { State = InvitationReissueState.RevokeUnconfirmed }; }
            progress("Старый доступ отозван. Шаг 2 из 2: создание нового токена…");
            try {
                // Source ID binds the replacement receipt to this acknowledged
                // revoke. Recovery never repeats either of these mutations.
                var token = await admin("invite", name, id);
                var access = HomeVpnAccess.Parse(token);
                if (access.InviteId == id) throw new InvalidOperationException();
                return new InvitationReissueResult { State = InvitationReissueState.Complete, Token = token,
                    Replacement = new HomeVpnInvitation { Id = access.InviteId, Name = name } };
            }
            catch { return new InvitationReissueResult { State = InvitationReissueState.IssueUnconfirmed }; }
        }
    }
}

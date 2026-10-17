using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace ProGo
{
    internal sealed class HomeVpnAdminPendingException : InvalidOperationException
    {
        internal HomeVpnAdminPendingException(string message) : base(message) { }
    }

    internal sealed class HomeVpnAdminRequest
    {
        public int Version { get; set; }
        public string RequestId { get; set; }
        public string Action { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public string Login { get; set; }
        public string Name { get; set; }
        public string SourceInviteId { get; set; }
        public string ServerId { get; set; }
        public string Result { get; set; }
    }

    // Only invite issuance, including reissue after an acknowledged revoke.
    // Repair/share/revoke retain their existing protected command path.
    internal static class HomeVpnAdminRecovery
    {
        private static readonly SemaphoreSlim gate = new SemaphoreSlim(1, 1);
        internal const string StorageName = "admin-request";
        internal const string PendingMessage = "Выдача доступа не подтверждена. Запрос сохранён. Нажмите «Проверить прежнюю выдачу»: ProGo получит результат прежней команды без нового приглашения.";
        private static JavaScriptSerializer Json { get { return new JavaScriptSerializer { MaxJsonLength = 70000, RecursionLimit = 8 }; } }
        private static string PathFor() { return Path.Combine(HomeVpnPrivateFiles.Root, StorageName + ".dat"); }
        private static bool Exists(string path)
        {
            try {
                if ((File.GetAttributes(path) & (FileAttributes.ReparsePoint | FileAttributes.Directory)) != 0) throw new IOException();
                return true;
            } catch (FileNotFoundException) { return false; }
            catch (DirectoryNotFoundException) { return false; }
        }
        private static void CheckRoot()
        {
            if (Directory.Exists(HomeVpnPrivateFiles.Root) && (File.GetAttributes(HomeVpnPrivateFiles.Root) & FileAttributes.ReparsePoint) != 0) throw new IOException();
        }
        private static void Owner(HomeVpnOwner owner)
        {
            if (owner == null || !HomeVpnAccess.ValidHost(owner.Host) || owner.Port < 1 || owner.Port > 65535
                || !Regex.IsMatch(owner.Login ?? "", @"\A[a-z_][a-z0-9_-]{0,31}\z")) throw new ArgumentException("Проверьте исходный VPS, SSH-порт и пользователя выдачи.");
        }
        internal static async Task<IDisposable> AcquireAsync()
        {
            if (!await gate.WaitAsync(0)) throw new HomeVpnAdminPendingException("Выдача или проверка прежнего доступа уже выполняется. Дождитесь результата.");
            return new Lease();
        }
        private sealed class Lease : IDisposable
        {
            private bool released;
            public void Dispose() { if (!released) { released = true; gate.Release(); } }
        }
        internal static bool HasPending()
        {
            try { CheckRoot(); return Exists(PathFor()); }
            catch (Exception ex) { if (ex is OutOfMemoryException) throw; throw new HomeVpnAdminPendingException("Сохранённый запрос выдачи недоступен. Новые приглашения заблокированы; существующий доступ сохранён."); }
        }
        internal static HomeVpnAdminRequest Pending()
        {
            try {
                CheckRoot(); string path = PathFor(); if (!Exists(path)) return null;
                if (new FileInfo(path).Length > 100000) throw new FormatException();
                string text = new UTF8Encoding(false, true).GetString(ProtectedData.Unprotect(File.ReadAllBytes(path), null, DataProtectionScope.CurrentUser));
                var fields = Json.Deserialize<Dictionary<string, object>>(text);
                if (fields == null || fields.Count != 10) throw new FormatException();
                foreach (string name in new[] { "Version", "RequestId", "Action", "Host", "Port", "Login", "Name", "SourceInviteId", "ServerId", "Result" })
                    if (!fields.ContainsKey(name)) throw new FormatException();
                if (!(fields["Version"] is int) || !(fields["Port"] is int)) throw new FormatException();
                foreach (string name in new[] { "RequestId", "Action", "Host", "Login", "Name", "ServerId" }) if (!(fields[name] is string)) throw new FormatException();
                foreach (string name in new[] { "SourceInviteId", "Result" }) if (fields[name] != null && !(fields[name] is string)) throw new FormatException();
                var value = Json.Deserialize<HomeVpnAdminRequest>(text);
                if (value.Version != 1 || value.Action != "invite" || !Regex.IsMatch(value.RequestId ?? "", @"\A[0-9a-f]{32}\z")
                    || !Regex.IsMatch(value.ServerId ?? "", @"\A[0-9a-f]{32}\z") || value.Host != value.Host.ToLowerInvariant()
                    || value.Name.Length > 80 || Array.Exists(value.Name.ToCharArray(), Char.IsControl)
                    || (value.SourceInviteId != null && !Regex.IsMatch(value.SourceInviteId, @"\A[0-9a-f]{24}\z"))) throw new FormatException();
                Owner(new HomeVpnOwner { Host = value.Host, Port = value.Port, Login = value.Login });
                if (value.Result != null) ValidateResult(value, value.Result);
                return value;
            } catch (Exception ex) {
                if (ex is OutOfMemoryException) throw;
                throw new HomeVpnAdminPendingException("Сохранённый запрос выдачи недоступен или повреждён. Новые приглашения заблокированы; восстановите исходные данные запроса. Доступ к VPN не удалён.");
            }
        }
        internal static HomeVpnAdminRequest Load(HomeVpnOwner owner)
        {
            Owner(owner); var value = Pending();
            if (value != null && (value.Host != owner.Host.ToLowerInvariant() || value.Port != owner.Port || value.Login != owner.Login))
                throw new HomeVpnAdminPendingException("Прежняя выдача относится к другому VPS или SSH-пользователю. Проверьте её с исходными адресом, портом и пользователем. Новый запрос не создан.");
            return value;
        }
        internal static HomeVpnAdminRequest Register(HomeVpnOwner owner, string name, string source)
        {
            Owner(owner); name = name ?? "My iPhone";
            if (name.Length > 80 || Array.Exists(name.ToCharArray(), Char.IsControl)
                || (source != null && !Regex.IsMatch(source, @"\A[0-9a-f]{24}\z"))) throw new ArgumentException("Проверьте имя и исходный ID приглашения.");
            if (HasPending()) throw new HomeVpnAdminPendingException(PendingMessage);
            if (HomeVpnSetupRecovery.HasPending()) throw new HomeVpnAdminPendingException("Сначала проверьте прежнюю настройку VPS. Новая выдача не запускалась.");
            HomeVpnAccess access;
            try { access = HomeVpnAccess.Parse(HomeVpnPrivateFiles.Load("access")); }
            catch { throw new HomeVpnAdminPendingException("Исходный сохранённый доступ к VPS недоступен. Новая выдача не запускалась."); }
            if (!String.Equals(access.Host, owner.Host, StringComparison.OrdinalIgnoreCase) || access.Port != owner.Port)
                throw new HomeVpnAdminPendingException("Сохранённый доступ относится к другому VPS. Новая выдача не запускалась.");
            var value = new HomeVpnAdminRequest { Version = 1, RequestId = Guid.NewGuid().ToString("N"), Action = "invite",
                Host = owner.Host.ToLowerInvariant(), Port = owner.Port, Login = owner.Login, Name = name, SourceInviteId = source, ServerId = access.ServerId };
            Save(value); return value;
        }
        private static void Save(HomeVpnAdminRequest value)
        {
            string temporary = null;
            try {
                CheckRoot(); HomeVpnPrivateFiles.SecureDirectory(HomeVpnPrivateFiles.Root); string path = PathFor(); Exists(path);
                byte[] bytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(Json.Serialize(value)), null, DataProtectionScope.CurrentUser);
                temporary = path + "." + Guid.NewGuid().ToString("N") + ".new";
                using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough)) { file.Write(bytes, 0, bytes.Length); file.Flush(true); }
                if (Exists(path)) File.Replace(temporary, path, null); else File.Move(temporary, path);
            } catch (Exception ex) {
                if (ex is OutOfMemoryException) throw;
                throw new HomeVpnAdminPendingException("Запрос или результат выдачи не удалось сохранить. Если SSH уже запускался, проверьте прежний запрос. Существующий доступ сохранён.");
            } finally { if (temporary != null) { try { File.Delete(temporary); } catch { } } }
        }
        private static void ValidateResult(HomeVpnAdminRequest value, string token)
        {
            HomeVpnAccess access;
            try { access = HomeVpnAccess.Parse(token); }
            catch { throw new HomeVpnAdminPendingException("VPS вернул неподтверждённый токен. Запрос сохранён; новый доступ не запрашивался."); }
            if (!String.Equals(access.Host, value.Host, StringComparison.OrdinalIgnoreCase) || access.Port != value.Port
                || access.ServerId != value.ServerId || access.InviteId == value.SourceInviteId)
                throw new HomeVpnAdminPendingException("Токен не соответствует исходному VPS или приглашению. Запрос сохранён; новый доступ не запрашивался.");
        }
        internal static string RetainResult(HomeVpnOwner owner, HomeVpnAdminRequest request, string token)
        {
            var current = Load(owner); token = token.Trim();
            if (current == null || request == null || current.Version != request.Version || current.RequestId != request.RequestId
                || current.Action != request.Action || current.Host != request.Host || current.Port != request.Port || current.Login != request.Login
                || current.ServerId != request.ServerId || current.Name != request.Name || current.SourceInviteId != request.SourceInviteId
                || (current.Result != null && current.Result != token)) throw new HomeVpnAdminPendingException("Сохранённый запрос или его результат изменился. Повторная выдача не запускалась.");
            ValidateResult(current, token); current.Result = token; Save(current); return token;
        }
        // Called only after the token's modal window closed intentionally.
        // Failure/crash before that handoff keeps the exact protected result.
        internal static void ConfirmConsumed(string token)
        {
            using (var lease = AcquireAsync().GetAwaiter().GetResult()) {
                var value = Pending(); if (value == null) return; // isolated callback-only fixtures
                if (value.Result == null || value.Result != token.Trim()) throw new HomeVpnAdminPendingException(PendingMessage);
                try { File.Delete(PathFor()); }
                catch { throw new HomeVpnAdminPendingException("Токен показан, но завершение запроса не удалось сохранить. Запрос остаётся для проверки; новая выдача заблокирована."); }
            }
        }
        private static bool Timestamp(object value, bool optional)
        {
            if (value == null) return optional; var text = value as string; DateTimeOffset parsed;
            return text != null && Regex.IsMatch(text, @"\A\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d{6})?\+00:00\z")
                && DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed);
        }
        internal static void RequireCompleted(string text, HomeVpnAdminRequest request)
        {
            try {
                var fields = Json.Deserialize<Dictionary<string, object>>(text); object version, id, action, state, started, finished, available;
                if (fields == null || fields.Count != 7 || !fields.TryGetValue("Version", out version) || !(version is int) || (int)version != 1
                    || !fields.TryGetValue("RequestId", out id) || !Object.Equals(id, request.RequestId)
                    || !fields.TryGetValue("Action", out action) || (action != null && !Object.Equals(action, request.Action))
                    || !fields.TryGetValue("State", out state) || !(state is string)
                    || !fields.TryGetValue("Started", out started) || !fields.TryGetValue("Finished", out finished)
                    || !Timestamp(started, true) || !Timestamp(finished, true)
                    || !fields.TryGetValue("ResultAvailable", out available) || !(available is bool)) throw new FormatException();
                string current = (string)state;
                if (current == "succeeded") {
                    if (!Object.Equals(action, request.Action) || started == null || finished == null) throw new FormatException();
                    if (!(bool)available) throw new HomeVpnAdminPendingException("Прежняя выдача завершена, но её токен недоступен, например отозван. Запрос сохранён; новый токен не создан.");
                    return;
                }
                if ((bool)available || (current != "running" && current != "not-found" && current != "unconfirmed")) throw new FormatException();
                if (current == "not-found" && (action != null || started != null || finished != null)) throw new FormatException();
                if (current != "not-found" && (action == null ? started != null || finished != null || current != "running" : started == null)) throw new FormatException();
                if (current == "running" && finished != null) throw new FormatException();
                throw new HomeVpnAdminPendingException(current == "running" ? "Прежняя выдача ещё выполняется. Дождитесь завершения и повторите только проверку запроса."
                    : "Результат прежней выдачи пока не подтверждён. Запрос сохранён; новая выдача заблокирована. Проверьте состояние VPS через SSH.");
            } catch (HomeVpnAdminPendingException) { throw; }
            catch (Exception ex) { if (ex is OutOfMemoryException) throw; throw new HomeVpnAdminPendingException("VPS вернул неподтверждённый статус выдачи. Запрос сохранён; новая выдача не запускалась."); }
        }
    }
}

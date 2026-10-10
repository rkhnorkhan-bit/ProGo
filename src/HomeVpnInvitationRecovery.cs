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
    internal sealed class HomeVpnInvitationPendingException : InvalidOperationException
    {
        internal HomeVpnInvitationPendingException(string message) : base(message) { }
    }

    internal sealed class HomeVpnInvitationRequest
    {
        public int Version { get; set; }
        public string RequestId { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public string Login { get; set; }
        public string Name { get; set; }
        public string Result { get; set; }
    }

    // Separate invitation creation only. Reissue/revocation retain their protected
    // command path; this record cannot authorize a new mutation after uncertainty.
    internal static class HomeVpnInvitationRecovery
    {
        internal const string StorageName = "invitation-request";
        internal const string PendingMessage = "Ответ выдачи токена ещё не подтверждён. Запрос сохранён. Нажмите «Проверить выдачу токена»: ProGo получит исходный результат, без создания другого доступа.";
        private static readonly SemaphoreSlim gate = new SemaphoreSlim(1, 1);
        private static JavaScriptSerializer Json { get { return new JavaScriptSerializer { MaxJsonLength = 70000, RecursionLimit = 8 }; } }
        private static string PathFor() { return Path.Combine(HomeVpnPrivateFiles.Root, StorageName + ".dat"); }
        internal static async Task<IDisposable> AcquireAsync()
        {
            if (!await gate.WaitAsync(0)) throw new HomeVpnInvitationPendingException("Выдача или проверка токена уже выполняется. Дождитесь её результата.");
            return new Lease();
        }
        private sealed class Lease : IDisposable
        {
            private bool released;
            public void Dispose() { if (!released) { released = true; gate.Release(); } }
        }
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
        private static void ValidateOwner(HomeVpnOwner owner)
        {
            if (owner == null) throw new ArgumentException("Нужны SSH-данные владельца VPS.");
            new HomeVpnOwner { Host = owner.Host, Port = owner.Port, Login = owner.Login }.Validate();
        }
        private static HomeVpnInvitationRequest Read()
        {
            try {
                CheckRoot(); string path = PathFor(); if (!Exists(path)) return null;
                if (new FileInfo(path).Length > 100000) throw new FormatException();
                string text = new UTF8Encoding(false, true).GetString(ProtectedData.Unprotect(File.ReadAllBytes(path), null, DataProtectionScope.CurrentUser));
                var fields = Json.Deserialize<Dictionary<string, object>>(text);
                if (fields == null || fields.Count != 7) throw new FormatException();
                foreach (string name in new[] { "Version", "RequestId", "Host", "Port", "Login", "Name", "Result" })
                    if (!fields.ContainsKey(name)) throw new FormatException();
                if (!(fields["Version"] is int) || !(fields["Port"] is int) || (fields["Result"] != null && !(fields["Result"] is string))) throw new FormatException();
                foreach (string name in new[] { "RequestId", "Host", "Login", "Name" }) if (!(fields[name] is string)) throw new FormatException();
                var request = Json.Deserialize<HomeVpnInvitationRequest>(text);
                if (request.Version != 1 || !Regex.IsMatch(request.RequestId, @"\A[0-9a-f]{32}\z") || request.Host != request.Host.ToLowerInvariant()
                    || request.Name.Length > 80 || Array.Exists(request.Name.ToCharArray(), Char.IsControl)) throw new FormatException();
                ValidateOwner(new HomeVpnOwner { Host = request.Host, Port = request.Port, Login = request.Login });
                if (request.Result != null) ValidateResult(request, request.Result);
                return request;
            } catch (Exception ex) {
                if (ex is OutOfMemoryException) throw;
                throw new HomeVpnInvitationPendingException("Сохранённый запрос выдачи токена недоступен или повреждён. Создание другого доступа заблокировано. Восстановите исходный запрос; существующий VPN-доступ сохранён.");
            }
        }
        internal static HomeVpnInvitationRequest Load(HomeVpnOwner owner, string label = null)
        {
            ValidateOwner(owner); var request = Read();
            if (request != null && (request.Host != owner.Host.ToLowerInvariant() || request.Port != owner.Port || request.Login != owner.Login
                || (label != null && label != request.Name)))
                throw new HomeVpnInvitationPendingException("Сначала проверьте прежнюю выдачу токена с исходными VPS, SSH-портом, пользователем и именем друга. Запрос не изменён; новый доступ не создан.");
            return request;
        }
        internal static HomeVpnInvitationRequest Register(HomeVpnOwner owner, string label)
        {
            ValidateOwner(owner); label = label ?? "Друг";
            if (label.Length > 80 || Array.Exists(label.ToCharArray(), Char.IsControl)) throw new ArgumentException("Имя приглашения: до 80 символов без управляющих знаков.");
            if (Read() != null) throw new HomeVpnInvitationPendingException(PendingMessage);
            var request = new HomeVpnInvitationRequest { Version = 1, RequestId = Guid.NewGuid().ToString("N"), Host = owner.Host.ToLowerInvariant(),
                Port = owner.Port, Login = owner.Login, Name = label };
            Save(request); return request;
        }
        private static void Save(HomeVpnInvitationRequest request)
        {
            string temporary = null;
            try {
                CheckRoot(); HomeVpnPrivateFiles.SecureDirectory(HomeVpnPrivateFiles.Root);
                string path = PathFor(); Exists(path);
                byte[] bytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(Json.Serialize(request)), null, DataProtectionScope.CurrentUser);
                temporary = path + "." + Guid.NewGuid().ToString("N") + ".new";
                using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough)) {
                    file.Write(bytes, 0, bytes.Length); file.Flush(true);
                }
                if (Exists(path)) File.Replace(temporary, path, null); else File.Move(temporary, path);
            } catch (Exception ex) {
                if (ex is OutOfMemoryException) throw;
                throw new HomeVpnInvitationPendingException("Не удалось сохранить запрос или результат выдачи токена. Если команда запускалась, проверьте её исходный результат. Существующий VPN-доступ сохранён.");
            } finally { if (temporary != null) { try { File.Delete(temporary); } catch { } } }
        }
        private static void ValidateResult(HomeVpnInvitationRequest request, string token)
        {
            HomeVpnAccess access;
            try { access = HomeVpnAccess.Parse(token); }
            catch (FormatException) { throw new HomeVpnInvitationPendingException(PendingMessage); }
            if (!String.Equals(access.Host, request.Host, StringComparison.OrdinalIgnoreCase) || access.Port != request.Port)
                throw new HomeVpnInvitationPendingException("Результат выдачи не соответствует исходному VPS. Запрос сохранён; другой доступ не запрашивался.");
        }
        internal static string RetainResult(HomeVpnOwner owner, HomeVpnInvitationRequest request, string token)
        {
            var current = Load(owner, request.Name);
            if (current == null || current.RequestId != request.RequestId) throw new HomeVpnInvitationPendingException(PendingMessage);
            ValidateResult(current, token); current.Result = token.Trim(); Save(current); return current.Result;
        }
        internal static void ConfirmSaved(HomeVpnOwner owner, string token)
        {
            using (var lease = AcquireAsync().GetAwaiter().GetResult()) {
                var request = Load(owner);
                if (request == null || request.Result == null || request.Result != (token ?? "").Trim()) throw new HomeVpnInvitationPendingException(PendingMessage);
                try { File.Delete(PathFor()); }
                catch { throw new HomeVpnInvitationPendingException("Завершение выдачи не удалось сохранить. Запрос и токен остаются в ProGo; закройте программы, использующие его файл, и повторите подтверждение."); }
            }
        }
        internal static string ReadOutput(string path)
        {
            if (new FileInfo(path).Length > 32768) throw new HomeVpnInvitationPendingException(PendingMessage);
            string result = File.ReadAllText(path, new UTF8Encoding(false, true)).Trim();
            if (result.Length == 0) throw new HomeVpnInvitationPendingException(PendingMessage); return result;
        }
        private static bool Timestamp(object value)
        {
            if (value == null) return true;
            string text = value as string; DateTimeOffset parsed;
            return text != null && Regex.IsMatch(text, @"\A\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d{6})?\+00:00\z")
                && DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed);
        }
        internal static void RequireCompleted(string text, HomeVpnInvitationRequest request)
        {
            try {
                var fields = Json.Deserialize<Dictionary<string, object>>(text);
                object version, id, action, state, started, finished, available;
                if (fields == null || fields.Count != 7 || !fields.TryGetValue("Version", out version) || !(version is int) || (int)version != 1
                    || !fields.TryGetValue("RequestId", out id) || !Object.Equals(id, request.RequestId)
                    || !fields.TryGetValue("Action", out action) || (action != null && !Object.Equals(action, "invite"))
                    || !fields.TryGetValue("State", out state) || !(state is string)
                    || !fields.TryGetValue("Started", out started) || !Timestamp(started) || !fields.TryGetValue("Finished", out finished) || !Timestamp(finished)
                    || !fields.TryGetValue("ResultAvailable", out available) || !(available is bool)) throw new FormatException();
                string current = (string)state;
                if (current == "succeeded") {
                    if (!Object.Equals(action, "invite") || started == null || finished == null) throw new FormatException();
                    if (!(bool)available) throw new HomeVpnInvitationPendingException("Прежняя выдача завершена, но токен недоступен, например доступ уже отозван. Запрос сохранён; ProGo не создал замену.");
                    return;
                }
                if ((bool)available || (current != "running" && current != "not-found" && current != "unconfirmed")) throw new FormatException();
                if (current == "not-found" && (action != null || started != null || finished != null)) throw new FormatException();
                if (current != "not-found" && (action == null ? started != null || finished != null || current != "running" : started == null)) throw new FormatException();
                if (current == "running" && finished != null) throw new FormatException();
                throw new HomeVpnInvitationPendingException(current == "running"
                    ? "Прежняя выдача токена ещё выполняется. Дождитесь завершения и нажмите «Проверить выдачу токена». Другой доступ не запрашивался."
                    : "Результат прежней выдачи пока не подтверждён. Запрос сохранён; новый доступ не создавался. Проверьте сервер через SSH перед дальнейшими изменениями.");
            } catch (HomeVpnInvitationPendingException) { throw; }
            catch (Exception ex) { if (ex is OutOfMemoryException) throw; throw new HomeVpnInvitationPendingException(PendingMessage); }
        }
    }
}

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
    internal sealed class HomeVpnSetupPendingException : InvalidOperationException
    {
        internal HomeVpnSetupPendingException(string message) : base(message) { }
    }

    internal sealed class HomeVpnSetupRequest
    {
        public int Version { get; set; }
        public string RequestId { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public string Login { get; set; }
        public string Name { get; set; }
        public string Result { get; set; }
    }

    // Only own-VPS setup uses this protocol in F23i. Other owner actions retain
    // their existing protected command path until their consumption is integrated.
    internal static class HomeVpnSetupRecovery
    {
        private static readonly SemaphoreSlim gate = new SemaphoreSlim(1, 1);
        private const int BlobLimit = 100000;
        internal const string PendingMessage = "Ответ настройки VPS не подтверждён. ID запроса сохранён. Нажмите «Проверить прошлую настройку»: ProGo проверит прежнюю команду, без повторной выдачи доступа.";
        private static JavaScriptSerializer Json { get { return new JavaScriptSerializer { MaxJsonLength = 70000, RecursionLimit = 8 }; } }

        internal static async Task<IDisposable> AcquireAsync()
        {
            if (!await gate.WaitAsync(0)) throw new HomeVpnSetupPendingException("Проверка или настройка VPS уже выполняется. Дождитесь её результата.");
            return new Lease();
        }
        private sealed class Lease : IDisposable
        {
            private bool released;
            public void Dispose() { if (!released) { released = true; gate.Release(); } }
        }

        internal const string StorageName = "setup-request";
        private static void ValidateOwner(HomeVpnOwner owner)
        {
            if (owner == null || !HomeVpnAccess.ValidHost(owner.Host) || owner.Port < 1 || owner.Port > 65535
                || !Regex.IsMatch(owner.Login ?? "", @"\A[a-z_][a-z0-9_-]{0,31}\z"))
                throw new ArgumentException("Проверьте адрес VPS, SSH-порт и имя пользователя.");
        }

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
            if (Directory.Exists(HomeVpnPrivateFiles.Root) && (File.GetAttributes(HomeVpnPrivateFiles.Root) & FileAttributes.ReparsePoint) != 0)
                throw new IOException();
        }
        internal static bool HasPending()
        {
            try { CheckRoot(); return Exists(PathFor()); }
            catch (Exception ex) { if (ex is OutOfMemoryException) throw; throw new HomeVpnSetupPendingException("Сохранённую настройку VPS не удалось проверить. Новая команда не запускалась."); }
        }

        private static HomeVpnSetupRequest Read()
        {
            try {
                CheckRoot(); string path = PathFor();
                if (!Exists(path)) return null;
                if (new FileInfo(path).Length > BlobLimit) throw new FormatException();
                var text = new UTF8Encoding(false, true).GetString(ProtectedData.Unprotect(File.ReadAllBytes(path), null, DataProtectionScope.CurrentUser));
                var fields = Json.Deserialize<Dictionary<string, object>>(text);
                if (fields == null || fields.Count != 7 || !fields.ContainsKey("Result")) throw new FormatException();
                foreach (string name in new[] { "Version", "RequestId", "Host", "Port", "Login", "Name", "Result" })
                    if (!fields.ContainsKey(name)) throw new FormatException();
                if (!(fields["Version"] is int) || !(fields["Port"] is int)
                    || (fields["Result"] != null && !(fields["Result"] is string))) throw new FormatException();
                foreach (string name in new[] { "RequestId", "Host", "Login", "Name" })
                    if (!(fields[name] is string)) throw new FormatException();
                var value = Json.Deserialize<HomeVpnSetupRequest>(text);
                if (value == null || value.Version != 1 || !Regex.IsMatch(value.RequestId ?? "", @"\A[0-9a-f]{32}\z")
                    || value.Host == null || value.Host != value.Host.ToLowerInvariant()
                    || value.Name == null || value.Name.Length > 80 || Array.Exists(value.Name.ToCharArray(), Char.IsControl)) throw new FormatException();
                ValidateOwner(new HomeVpnOwner { Host = value.Host, Port = value.Port, Login = value.Login });
                if (value.Result != null) ValidateResult(value, value.Result);
                return value;
            } catch (Exception ex) {
                if (ex is OutOfMemoryException) throw;
                throw new HomeVpnSetupPendingException("Сохранённый запрос настройки VPS недоступен или повреждён. Повторная настройка заблокирована; восстановите исходные данные запроса. Доступ к VPN не удалён.");
            }
        }

        internal static HomeVpnSetupRequest Load(HomeVpnOwner owner, string label)
        {
            ValidateOwner(owner);
            var value = Read();
            if (value != null && (value.Host != owner.Host.ToLowerInvariant() || value.Port != owner.Port || value.Login != owner.Login
                || value.Name != (label ?? "My iPhone")))
                throw new HomeVpnSetupPendingException("Сначала проверьте прежнюю настройку с её исходными адресом VPS, SSH-портом и пользователем. Новый запрос не создан; сохранённый запрос не изменён.");
            return value;
        }
        internal static HomeVpnOwner PendingOwner()
        {
            var value = Read();
            return value == null ? null : new HomeVpnOwner { Host = value.Host, Port = value.Port, Login = value.Login, KeyFile = "" };
        }

        internal static HomeVpnSetupRequest Register(HomeVpnOwner owner, string label)
        {
            ValidateOwner(owner);
            label = label ?? "My iPhone";
            if (label.Length > 80 || Array.Exists(label.ToCharArray(), Char.IsControl)) throw new ArgumentException("Проверьте название доступа: до 80 символов без управляющих знаков.");
            if (HasPending()) throw new HomeVpnSetupPendingException(PendingMessage);
            var value = new HomeVpnSetupRequest { Version = 1, RequestId = Guid.NewGuid().ToString("N"),
                Host = owner.Host.ToLowerInvariant(), Port = owner.Port, Login = owner.Login, Name = label };
            Save(value);
            return value;
        }

        private static void Save(HomeVpnSetupRequest value)
        {
            string temporary = null;
            try {
                CheckRoot(); HomeVpnPrivateFiles.SecureDirectory(HomeVpnPrivateFiles.Root);
                string path = PathFor(); Exists(path);
                var bytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(Json.Serialize(value)), null, DataProtectionScope.CurrentUser);
                temporary = path + "." + Guid.NewGuid().ToString("N") + ".new";
                using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough)) {
                    file.Write(bytes, 0, bytes.Length); file.Flush(true);
                }
                if (Exists(path)) File.Replace(temporary, path, null); else File.Move(temporary, path);
            } catch (Exception ex) {
                if (ex is OutOfMemoryException) throw;
                throw new HomeVpnSetupPendingException("Не удалось сохранить запрос или результат настройки VPS. Если команда уже запускалась, её результат ещё нужно проверить. Существующий доступ к VPN не удалён.");
            } finally { if (temporary != null) { try { File.Delete(temporary); } catch { } } }
        }

        private static void ValidateResult(HomeVpnSetupRequest request, string token)
        {
            HomeVpnAccess access;
            try { access = HomeVpnAccess.Parse(token); }
            catch (FormatException) { throw new HomeVpnSetupPendingException("VPS вернул неподтверждённый доступ. Запрос сохранён; повторная выдача не запускалась."); }
            if (!String.Equals(access.Host, request.Host, StringComparison.OrdinalIgnoreCase) || access.Port != request.Port)
                throw new HomeVpnSetupPendingException("Результат настройки не соответствует выбранному VPS. Запрос сохранён; повторная выдача не запрашивалась.");
        }
        internal static string RetainResult(HomeVpnOwner owner, HomeVpnSetupRequest request, string token)
        {
            ValidateResult(request, token);
            var current = Load(owner, request.Name);
            if (current == null || current.RequestId != request.RequestId) throw new HomeVpnSetupPendingException("Сохранённый запрос настройки изменился. Повторный доступ не запрашивался; результат прежнего запроса нужно проверить.");
            request.Result = token.Trim(); Save(request);
            return token;
        }

        internal static void ConfirmConsumed(HomeVpnOwner owner, string token)
        {
            using (var lease = AcquireAsync().GetAwaiter().GetResult()) {
                var request = Load(owner, "My iPhone");
                if (request == null) return; // Imported tokens and fixture transports have no journal.
                if (request.Result == null || request.Result != token.Trim()) throw new HomeVpnSetupPendingException(PendingMessage);
                try {
                    var savedOwner = Json.Deserialize<HomeVpnOwner>(HomeVpnPrivateFiles.Load("owner") ?? "null");
                    if (HomeVpnPrivateFiles.Load("access") != request.Result || savedOwner == null
                        || !String.Equals(savedOwner.Host, owner.Host, StringComparison.OrdinalIgnoreCase) || savedOwner.Port != owner.Port
                        || savedOwner.Login != owner.Login || savedOwner.KeyFile != owner.KeyFile) throw new IOException();
                } catch (Exception ex) {
                    if (ex is OutOfMemoryException) throw;
                    throw new HomeVpnSetupPendingException("Доступ и SSH-данные ещё не подтверждены в локальном сохранении. Запрос сохранён; повторная выдача не требуется.");
                }
                try { File.Delete(PathFor()); }
                catch { throw new HomeVpnSetupPendingException("Доступ сохранён, но завершение запроса не удалось записать. Можно проверить прежнюю настройку; повторная выдача не требуется."); }
            }
        }

        private static bool Timestamp(object value, bool optional)
        {
            if (value == null) return optional;
            var text = value as string; DateTimeOffset parsed;
            return text != null && Regex.IsMatch(text, @"\A\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d{6})?\+00:00\z")
                && DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed);
        }
        internal static void RequireCompleted(string text, HomeVpnSetupRequest request)
        {
            try {
                var fields = Json.Deserialize<Dictionary<string, object>>(text);
                object version, id, action, state, started, finished, available;
                if (fields == null || fields.Count != 7 || !fields.TryGetValue("Version", out version) || !(version is int) || (int)version != 1
                    || !fields.TryGetValue("RequestId", out id) || !Object.Equals(id, request.RequestId)
                    || !fields.TryGetValue("Action", out action) || (action != null && !Object.Equals(action, "setup"))
                    || !fields.TryGetValue("State", out state) || !(state is string)
                    || !fields.TryGetValue("Started", out started) || !fields.TryGetValue("Finished", out finished)
                    || !Timestamp(started, true) || !Timestamp(finished, true)
                    || !fields.TryGetValue("ResultAvailable", out available) || !(available is bool)) throw new FormatException();
                string current = (string)state;
                if (current == "succeeded") {
                    if (!Object.Equals(action, "setup") || started == null || finished == null) throw new FormatException();
                    if (!(bool)available) throw new HomeVpnSetupPendingException("Прежняя команда завершена, но её доступ уже недоступен, например отозван. Запрос сохранён; ProGo не создавал замену.");
                    return;
                }
                if ((bool)available || (current != "running" && current != "not-found" && current != "unconfirmed")) throw new FormatException();
                if (current == "not-found" && (action != null || started != null || finished != null)) throw new FormatException();
                if (current != "not-found" && (action == null ? started != null || finished != null || current != "running" : started == null)) throw new FormatException();
                if (current == "running" && finished != null) throw new FormatException();
                if (current == "running") throw new HomeVpnSetupPendingException("Прежняя настройка VPS ещё выполняется. Дождитесь завершения и нажмите «Проверить прошлую настройку». Новая команда не запускалась.");
                throw new HomeVpnSetupPendingException("Результат прежней настройки VPS пока не подтверждён. Запрос сохранён; новая команда не запускалась. Проверьте состояние сервера через SSH перед дальнейшими изменениями.");
            } catch (HomeVpnSetupPendingException) { throw; }
            catch (Exception ex) {
                if (ex is OutOfMemoryException) throw;
                throw new HomeVpnSetupPendingException("VPS вернул неподтверждённый статус настройки. Запрос сохранён; новая команда не запускалась.");
            }
        }
    }
}

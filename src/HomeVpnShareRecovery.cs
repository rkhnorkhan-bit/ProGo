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
    internal sealed class HomeVpnSharePendingException : InvalidOperationException
    {
        internal HomeVpnSharePendingException(string message) : base(message) { }
    }
    internal sealed class HomeVpnShareRequest
    {
        public int Version { get; set; }
        public string RequestId { get; set; }
        public string Action { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public string Login { get; set; }
        public string Domain { get; set; }
        public string ServerId { get; set; }
    }

    // Only HTTPS installation admission, never a universal owner-action journal.
    // Unknown outcomes preserve the request; only its read-only result may recover it.
    internal static class HomeVpnShareRecovery
    {
        internal const string StorageName = "share-request";
        internal const string PendingMessage = "Настройка HTTPS не подтверждена. Запрос сохранён. Нажмите «Проверить прежнюю настройку HTTPS»: новая настройка не запускается. Команда VPS могла завершиться или продолжать работу; ProGo не повторяет и не откатывает её.";
        private static readonly SemaphoreSlim gate = new SemaphoreSlim(1, 1);
        private static HomeVpnShareRequest completed;
        private static string completedOrigin;
        private static JavaScriptSerializer Json { get { return new JavaScriptSerializer { MaxJsonLength = 4096, RecursionLimit = 8 }; } }
        private static string PathFor() { return Path.Combine(HomeVpnPrivateFiles.Root, StorageName + ".dat"); }
        private static bool Exists(string path)
        {
            try { if ((File.GetAttributes(path) & (FileAttributes.ReparsePoint | FileAttributes.Directory)) != 0) throw new IOException(); return true; }
            catch (FileNotFoundException) { return false; }
            catch (DirectoryNotFoundException) { return false; }
        }
        private static void CheckRoot()
        { if (Directory.Exists(HomeVpnPrivateFiles.Root) && (File.GetAttributes(HomeVpnPrivateFiles.Root) & FileAttributes.ReparsePoint) != 0) throw new IOException(); }
        internal static async Task<IDisposable> AcquireAsync()
        {
            if (!await gate.WaitAsync(0)) throw new HomeVpnSharePendingException("Настройка или проверка прежнего HTTPS-запроса уже выполняется. Дождитесь результата.");
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
            catch (Exception ex) { if (ex is OutOfMemoryException) throw; throw new HomeVpnSharePendingException("Сохранённый HTTPS-запрос недоступен. Новая настройка заблокирована; прежний адрес и доступ сохранены."); }
        }
        internal static HomeVpnShareRequest Pending()
        {
            try {
                CheckRoot(); string path = PathFor(); if (!Exists(path)) return null;
                if (new FileInfo(path).Length > 16384) throw new FormatException();
                string text = new UTF8Encoding(false, true).GetString(ProtectedData.Unprotect(File.ReadAllBytes(path), null, DataProtectionScope.CurrentUser));
                var fields = Json.Deserialize<Dictionary<string, object>>(text);
                if (fields == null || fields.Count != 8 || !fields.ContainsKey("Version") || !(fields["Version"] is int)
                    || !fields.ContainsKey("Port") || !(fields["Port"] is int)) throw new FormatException();
                foreach (string name in new[] { "RequestId", "Action", "Host", "Login", "Domain", "ServerId" })
                    if (!fields.ContainsKey(name) || !(fields[name] is string)) throw new FormatException();
                var value = Json.Deserialize<HomeVpnShareRequest>(text);
                if (value.Version != 1 || value.Action != "share" || !Regex.IsMatch(value.RequestId, @"\A[0-9a-f]{32}\z")
                    || !Regex.IsMatch(value.ServerId, @"\A[0-9a-f]{32}\z") || value.Host != value.Host.ToLowerInvariant()
                    || !HomeVpnAccess.ValidHost(value.Host) || value.Port < 1 || value.Port > 65535
                    || !Regex.IsMatch(value.Login, @"\A[a-z_][a-z0-9_-]{0,31}\z")
                    || HomeProfileShare.Origin(value.Domain) != "https://" + value.Domain) throw new FormatException();
                return value;
            } catch (Exception ex) { if (ex is OutOfMemoryException) throw; throw new HomeVpnSharePendingException("Сохранённый HTTPS-запрос недоступен или повреждён. Новая настройка заблокирована; восстановите исходный запрос. Прежний адрес и доступ сохранены."); }
        }
        internal static HomeVpnShareRequest Load(HomeVpnOwner owner)
        {
            var value = Pending();
            if (value != null && (owner == null || value.Host != (owner.Host ?? "").ToLowerInvariant() || value.Port != owner.Port || value.Login != owner.Login))
                throw new HomeVpnSharePendingException("Прежняя настройка HTTPS относится к другому VPS или SSH-пользователю. Проверьте её с исходными адресом, портом и пользователем. Новая команда не создана.");
            return value;
        }
        internal static HomeVpnShareRequest Register(HomeVpnOwner owner, string domain)
        {
            owner.Validate(); domain = new Uri(HomeProfileShare.Origin(domain)).Host;
            if (HasPending()) throw new HomeVpnSharePendingException(PendingMessage);
            if (HomeVpnSetupRecovery.HasPending()) throw new HomeVpnSharePendingException("Сначала проверьте прежнюю настройку VPS. Настройка HTTPS не запускалась.");
            HomeVpnAccess access;
            try { access = HomeVpnAccess.Parse(HomeVpnPrivateFiles.Load("access")); }
            catch { throw new HomeVpnSharePendingException("Исходный доступ к VPS недоступен. Настройка HTTPS не запускалась."); }
            if (!String.Equals(access.Host, owner.Host, StringComparison.OrdinalIgnoreCase) || access.Port != owner.Port)
                throw new HomeVpnSharePendingException("Сохранённый доступ относится к другому VPS. Настройка HTTPS не запускалась.");
            var value = new HomeVpnShareRequest { Version = 1, RequestId = Guid.NewGuid().ToString("N"), Action = "share",
                Host = owner.Host.ToLowerInvariant(), Port = owner.Port, Login = owner.Login, Domain = domain, ServerId = access.ServerId };
            string temporary = null;
            try {
                CheckRoot(); HomeVpnPrivateFiles.SecureDirectory(HomeVpnPrivateFiles.Root); string path = PathFor(); Exists(path);
                byte[] bytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(Json.Serialize(value)), null, DataProtectionScope.CurrentUser);
                temporary = path + "." + Guid.NewGuid().ToString("N") + ".new";
                using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough)) { file.Write(bytes, 0, bytes.Length); file.Flush(true); }
                File.Move(temporary, path); completed = null; completedOrigin = null; return value;
            } catch (Exception ex) { if (ex is OutOfMemoryException) throw; throw new HomeVpnSharePendingException("HTTPS-запрос не удалось сохранить. Команда настройки не запускалась; прежний адрес и доступ сохранены."); }
            finally { if (temporary != null) { try { File.Delete(temporary); } catch { } } }
        }
        private static bool Same(HomeVpnShareRequest a, HomeVpnShareRequest b)
        { return a != null && b != null && a.Version == b.Version && a.RequestId == b.RequestId && a.Action == b.Action && a.Host == b.Host && a.Port == b.Port && a.Login == b.Login && a.Domain == b.Domain && a.ServerId == b.ServerId; }
        private static bool SameAccess(HomeVpnShareRequest request, HomeVpnAccess access)
        { return request != null && access != null && access.ServerId == request.ServerId && access.Port == request.Port && String.Equals(access.Host, request.Host, StringComparison.OrdinalIgnoreCase); }
        internal static string RequireResult(HomeVpnOwner owner, HomeVpnShareRequest request, string result)
        {
            if (!Same(Load(owner), request) || result != "https://" + request.Domain)
                throw new HomeVpnSharePendingException("Результат HTTPS не соответствует сохранённому запросу или его домену. Запрос сохранён; новая настройка не запускалась.");
            if (Same(completed, request)) completedOrigin = result;
            return result;
        }
        private static bool Timestamp(object value, bool optional)
        {
            DateTimeOffset parsed; var text = value as string;
            return value == null ? optional : text != null && Regex.IsMatch(text, @"\A\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d{6})?\+00:00\z")
                && DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed);
        }
        internal static void RequireCompleted(string text, HomeVpnShareRequest request)
        {
            try {
                var f = Json.Deserialize<Dictionary<string, object>>(text); object version, id, action, state, started, finished, available;
                if (f == null || f.Count != 7 || !f.TryGetValue("Version", out version) || !(version is int) || (int)version != 1
                    || !f.TryGetValue("RequestId", out id) || !Object.Equals(id, request.RequestId)
                    || !f.TryGetValue("Action", out action) || (action != null && !Object.Equals(action, request.Action))
                    || !f.TryGetValue("State", out state) || !(state is string)
                    || !f.TryGetValue("Started", out started) || !f.TryGetValue("Finished", out finished)
                    || !Timestamp(started, true) || !Timestamp(finished, true)
                    || !f.TryGetValue("ResultAvailable", out available) || !(available is bool)) throw new FormatException();
                string current = (string)state;
                if (current == "succeeded") {
                    if (!Object.Equals(action, "share") || started == null || finished == null || !(bool)available) throw new FormatException();
                    completed = new HomeVpnShareRequest { Version = request.Version, RequestId = request.RequestId, Action = request.Action,
                        Host = request.Host, Port = request.Port, Login = request.Login, Domain = request.Domain, ServerId = request.ServerId };
                    completedOrigin = null;
                    return;
                }
                if ((bool)available || (current != "running" && current != "not-found" && current != "unconfirmed")) throw new FormatException();
                if (current == "not-found" && (action != null || started != null || finished != null)) throw new FormatException();
                if (current != "not-found" && (action == null ? started != null || finished != null || current != "running" : started == null)) throw new FormatException();
                if (current == "running" && finished != null) throw new FormatException();
                throw new HomeVpnSharePendingException(current == "running" ? "Прежняя настройка HTTPS ещё выполняется. Повторяйте только проверку сохранённого запроса."
                    : "Завершение прежней настройки HTTPS не подтверждено. Запрос сохранён; новая настройка заблокирована. Проверьте VPS через SSH.");
            } catch (HomeVpnSharePendingException) { throw; }
            catch (Exception ex) { if (ex is OutOfMemoryException) throw; throw new HomeVpnSharePendingException("VPS вернул неподтверждённый статус HTTPS. Запрос сохранён; новая настройка не запускалась."); }
        }
        internal static Task ConfirmAsync(HomeVpnOwner owner, HomeVpnAccess access, string origin, Func<Task> verify, Action<string> save, Func<bool> canSave)
        { return ConfirmAsync(owner, access, origin, verify, save, canSave, CancellationToken.None, action => { action(); return Task.FromResult(true); }); }
        internal static async Task ConfirmAsync(HomeVpnOwner owner, HomeVpnAccess access, string origin, Func<Task> verify, Action<string> save, Func<bool> canSave,
            CancellationToken token, Func<Action, Task<bool>> finalizeOnOwner)
        {
            using (var lease = await AcquireAsync().ConfigureAwait(false)) {
                var request = Load(owner);
                if (!Same(request, completed) || completedOrigin != origin || !SameAccess(request, access)) throw new HomeVpnSharePendingException(PendingMessage);
                RequireResult(owner, request, origin);
                await verify().ConfigureAwait(false);
                // Save and consumption have one owner-thread acceptance boundary.
                // A Cancel/Dispose callback cannot interleave between these checks.
                bool accepted = await finalizeOnOwner(delegate {
                    token.ThrowIfCancellationRequested();
                    if (!canSave() || !SameAccess(request, access)) throw new HomeVpnSharePendingException(PendingMessage);
                    RequireResult(owner, request, origin);
                    try { save(origin); }
                    catch (Exception ex) { if (ex is OutOfMemoryException) throw; throw new HomeVpnSharePendingException("Подтверждённый HTTPS-адрес не удалось сохранить. Запрос сохранён; повторяйте только проверку прежней настройки."); }
                    if (!canSave() || !SameAccess(request, access)) throw new HomeVpnSharePendingException(PendingMessage);
                    RequireResult(owner, request, origin);
                    try { File.Delete(PathFor()); completed = null; completedOrigin = null; }
                    catch { throw new HomeVpnSharePendingException("HTTPS-адрес сохранён, но завершение запроса не удалось сохранить. Запрос остаётся для проверки; новая настройка заблокирована."); }
                }).ConfigureAwait(false);
                if (!accepted) throw new HomeVpnSharePendingException(PendingMessage);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Script.Serialization;

namespace ProGo
{
    internal sealed class HomeVpnPortableException : InvalidOperationException
    {
        internal HomeVpnPortableException(string message) : base(message) { }
    }

    internal sealed class HomeVpnPortablePreview
    {
        internal string[] Files { get; private set; }
        internal string[] Pending { get; private set; }
        internal string Server { get; private set; }
        private readonly string[] pendingDescriptions;
        internal HomeVpnPortablePreview(string[] files, string[] pending, string server, string[] descriptions)
        { Files = files; Pending = pending; Server = server; pendingDescriptions = descriptions; }
        internal string Description {
            get {
                return "Состав: " + String.Join(", ", Files) + "\r\nИсходный VPS: " + Server +
                    "\r\nНезавершённые запросы: " + (Pending.Length == 0 ? "нет" : String.Join("\r\n", pendingDescriptions)) +
                    "\r\nSSH-ключ владельца выбирается на этом ПК отдельно. Архив не содержит .ssh, настройки Windows, vault или журналы владения прокси." +
                    "\r\nИмпорт не запускает подключение и не изменяет VPS. Доступность и отзыв доступа на сервере ещё не проверены.";
            }
        }
    }

    internal sealed class HomeVpnPortablePrepared : IDisposable
    {
        internal Dictionary<string, byte[]> Entries { get; private set; }
        internal HomeVpnPortablePreview Preview { get; private set; }
        internal HomeVpnPortablePrepared(Dictionary<string, byte[]> entries, HomeVpnPortablePreview preview)
        { Entries = entries; Preview = preview; }
        public void Dispose()
        {
            if (Entries == null) return;
            foreach (var bytes in Entries.Values) Array.Clear(bytes, 0, bytes.Length);
            Entries.Clear(); Entries = null;
        }
    }

    // Separate portable format. Existing DPAPI files, vault encryption and ordinary
    // program/data rollback have no dependency on this format or its password.
    internal static class HomeVpnPortableArchive
    {
        private const int Iterations = 600000, MaxPasswordBytes = 1024, MaxEntries = 128;
        private const int MaxEntryBytes = 131072, MaxPayloadBytes = 4194304;
        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("PROGO-VPN-EXPORT\0");
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);
        private static readonly SemaphoreSlim gate = new SemaphoreSlim(1, 1);
        private static readonly string[] Requests = { "setup-request.dat", "admin-request.dat", "share-request.dat" };
        private const string InvalidArchive = "Неверная парольная фраза либо архив повреждён. Импорт не выполнен.";
        private static JavaScriptSerializer Json { get { return new JavaScriptSerializer { MaxJsonLength = MaxEntryBytes, RecursionLimit = 8 }; } }

        internal static void ValidatePassword(byte[] password)
        {
            char[] chars = null;
            try {
                if (password == null || password.Length > MaxPasswordBytes) throw new FormatException();
                chars = Utf8.GetChars(password); int count = 0;
                for (int i = 0; i < chars.Length; i++, count++) if (Char.IsHighSurrogate(chars[i])) i++;
                if (count < 16) throw new FormatException();
            } catch (Exception ex) {
                if (ex is OutOfMemoryException) throw;
                throw new HomeVpnPortableException("Введите отдельную длинную парольную фразу: не менее 16 символов и не более 1024 байт UTF-8. PIN и passphrase SSH не используются.");
            } finally { if (chars != null) Array.Clear(chars, 0, chars.Length); }
        }

        internal static HomeVpnPortablePreview Export(string sourceRoot, string destination, byte[] password, CancellationToken cancellation,
            Action<string, CancellationToken> beforeRead = null)
        {
            ValidatePassword(password); Enter(); HomeVpnPortablePrepared snapshot = null;
            byte[] payload = null, salt = null, iv = null, material = null, cipher = null, header = null, tag = null;
            string temporary = null; bool committed = false;
            try {
                cancellation.ThrowIfCancellationRequested();
                destination = Full(destination); RejectLinks(Path.GetDirectoryName(destination));
                if (File.Exists(destination) || Directory.Exists(destination)) throw new HomeVpnPortableException("Файл экспорта уже существует. Выберите новое имя; прежний файл не заменён.");
                snapshot = ReadSource(sourceRoot, cancellation, beforeRead);
                payload = Pack(snapshot.Entries); salt = Random(32); iv = Random(16);
                material = Derive(password, salt); cancellation.ThrowIfCancellationRequested();
                using (var aes = Aes.Create()) {
                    var key = Slice(material, 0, 32);
                    try {
                        aes.Key = key; aes.IV = iv; aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.PKCS7;
                        using (var encrypt = aes.CreateEncryptor()) cipher = encrypt.TransformFinalBlock(payload, 0, payload.Length);
                    } finally { Clear(key); }
                }
                header = Header(salt, iv, cipher.Length); tag = Mac(material, header, cipher);
                RejectLinks(Path.GetDirectoryName(destination));
                temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
                cancellation.ThrowIfCancellationRequested();
                using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65536, FileOptions.WriteThrough)) {
                    Write(file, header, cancellation); Write(file, cipher, cancellation); Write(file, tag, cancellation); file.Flush(true);
                }
                cancellation.ThrowIfCancellationRequested(); RejectLinks(Path.GetDirectoryName(destination));
                File.Move(temporary, destination); committed = true;
                return snapshot.Preview;
            } catch (OperationCanceledException) { throw; }
            catch (HomeVpnPortableException) { throw; }
            catch (Exception ex) { if (ex is OutOfMemoryException) throw; throw new HomeVpnPortableException("Защищённый экспорт VPN не создан. Проверьте доступ к исходным данным и выбранной папке. Прежние файлы сохранены."); }
            finally {
                if (!committed && temporary != null) try { File.Delete(temporary); } catch { }
                if (snapshot != null) snapshot.Dispose(); Clear(payload, salt, iv, material, cipher, header, tag); gate.Release();
            }
        }

        internal static HomeVpnPortablePrepared Prepare(string archive, byte[] password, CancellationToken cancellation,
            Action<string, CancellationToken> beforeRead = null)
        {
            ValidatePassword(password); Enter(); byte[] salt = null, iv = null, cipher = null, tag = null, material = null, header = null, actual = null, payload = null;
            Dictionary<string, byte[]> entries = null;
            try {
                archive = Full(archive); cancellation.ThrowIfCancellationRequested();
                if (beforeRead != null) beforeRead(archive, cancellation);
                cancellation.ThrowIfCancellationRequested(); RejectLinks(archive);
                using (var input = new FileStream(archive, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var reader = new BinaryReader(input, Utf8, true)) {
                    var magic = Exact(reader, Magic.Length);
                    if (!Equal(magic, Magic) || reader.ReadInt32() != 1 || reader.ReadInt32() != Iterations) throw new FormatException();
                    int length = reader.ReadInt32();
                    if (length < 16 || length > MaxPayloadBytes + 16 || length % 16 != 0 || input.Length != Magic.Length + 12 + 32 + 16 + length + 32) throw new FormatException();
                    salt = Exact(reader, 32); iv = Exact(reader, 16); cipher = Exact(reader, length, cancellation); tag = Exact(reader, 32);
                    if (input.ReadByte() != -1) throw new FormatException();
                }
                cancellation.ThrowIfCancellationRequested(); header = Header(salt, iv, cipher.Length);
                material = Derive(password, salt); cancellation.ThrowIfCancellationRequested(); actual = Mac(material, header, cipher);
                // All 32 bytes are compared before the decryptor or plaintext parser exists.
                if (!Equal(actual, tag)) throw new FormatException();
                using (var aes = Aes.Create()) {
                    var key = Slice(material, 0, 32);
                    try {
                        aes.Key = key; aes.IV = iv; aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.PKCS7;
                        using (var decrypt = aes.CreateDecryptor()) payload = decrypt.TransformFinalBlock(cipher, 0, cipher.Length);
                    } finally { Clear(key); }
                }
                cancellation.ThrowIfCancellationRequested(); entries = Unpack(payload);
                var preview = ValidateEntries(entries, false);
                var result = new HomeVpnPortablePrepared(entries, preview); entries = null; return result;
            } catch (OperationCanceledException) { throw; }
            catch (Exception ex) { if (ex is OutOfMemoryException) throw; throw new HomeVpnPortableException(InvalidArchive); }
            finally { if (entries != null) foreach (var bytes in entries.Values) Clear(bytes); Clear(salt, iv, cipher, tag, material, header, actual, payload); gate.Release(); }
        }

        internal static void Import(HomeVpnPortablePrepared prepared, string destinationRoot, CancellationToken cancellation,
            Action<string, CancellationToken> beforeWrite = null)
        {
            Enter(); string staging = null; bool committed = false, ownsStaging = false;
            try {
                if (prepared == null || prepared.Entries == null) throw new HomeVpnPortableException("Сначала проверьте защищённый архив VPN.");
                ValidateEntries(prepared.Entries, false); destinationRoot = Full(destinationRoot); cancellation.ThrowIfCancellationRequested();
                RejectLinks(Path.GetDirectoryName(destinationRoot));
                if (Directory.Exists(destinationRoot) || File.Exists(destinationRoot)) throw new HomeVpnPortableException("На этом ПК уже есть каталог личных данных VPN. Импорт предназначен для чистой установки; текущий доступ и сохранённые запросы не заменены.");
                staging = destinationRoot + ".import-" + Guid.NewGuid().ToString("N");
                if (Directory.Exists(staging) || File.Exists(staging)) throw new IOException();
                Directory.CreateDirectory(staging); ownsStaging = true; HomeVpnPrivateFiles.SecureDirectory(staging);
                foreach (var pair in prepared.Entries.OrderBy(p => p.Key, StringComparer.Ordinal)) {
                    cancellation.ThrowIfCancellationRequested();
                    var path = Path.Combine(staging, pair.Key); if (beforeWrite != null) beforeWrite(path, cancellation);
                    cancellation.ThrowIfCancellationRequested(); byte[] protectedBytes = null, verification = null, check = null;
                    try {
                        protectedBytes = ProtectedData.Protect(pair.Value, null, DataProtectionScope.CurrentUser);
                        using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65536, FileOptions.WriteThrough)) {
                            Write(file, protectedBytes, cancellation); file.Flush(true);
                        }
                        verification = ReadBounded(path, cancellation, null);
                        check = ProtectedData.Unprotect(verification, null, DataProtectionScope.CurrentUser);
                        if (!Equal(check, pair.Value)) throw new FormatException();
                    } finally { Clear(protectedBytes, verification, check); }
                }
                cancellation.ThrowIfCancellationRequested(); RejectLinks(Path.GetDirectoryName(destinationRoot)); RejectLinks(staging);
                // One same-volume publication. No existing private root is renamed,
                // replaced or deleted, and cancellation cannot undo an accepted import.
                Directory.Move(staging, destinationRoot); committed = true;
            } catch (OperationCanceledException) { throw; }
            catch (HomeVpnPortableException) { throw; }
            catch (Exception ex) { if (ex is OutOfMemoryException) throw; throw new HomeVpnPortableException("Импорт VPN не выполнен. Проверьте права и доступ к папке. Текущий доступ, настройки и данные сохранены."); }
            finally {
                if (!committed && ownsStaging) try {
                    RejectLinks(staging);
                    foreach (var path in Directory.GetFileSystemEntries(staging)) {
                        var attributes = File.GetAttributes(path);
                        if ((attributes & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0) throw new IOException();
                    }
                    Directory.Delete(staging, true);
                } catch { }
                gate.Release();
            }
        }

        private static void Enter()
        { if (!gate.Wait(0)) throw new HomeVpnPortableException("Экспорт или импорт VPN уже выполняется. Дождитесь завершения текущей операции."); }
        private static string Full(string path)
        {
            if (String.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path)) throw new FormatException();
            string full = Path.GetFullPath(path), root = Path.GetPathRoot(full);
            if (String.Equals(full.TrimEnd(Path.DirectorySeparatorChar), root.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase)
                || full.Substring(root.Length).Contains(":")) throw new FormatException();
            return full.TrimEnd(Path.DirectorySeparatorChar);
        }
        private static void RejectLinks(string path)
        {
            for (var current = path; !String.IsNullOrEmpty(current); current = Path.GetDirectoryName(current))
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) throw new FormatException();
        }
        private static bool Known(string name)
        {
            if (name == "access.dat" || name == "owner.dat" || name == "home-address.dat" || Requests.Contains(name)) return true;
            return Regex.IsMatch(name ?? "", @"\Ashare-[0-9a-f]{32}\.dat\z");
        }
        private static Dictionary<string, string> Names(string root, CancellationToken cancellation)
        {
            RejectLinks(root); var result = new Dictionary<string, string>(StringComparer.Ordinal); int visited = 0;
            foreach (var path in Directory.EnumerateFileSystemEntries(root)) {
                cancellation.ThrowIfCancellationRequested(); if (++visited > MaxEntries + 32) throw new FormatException();
                var attributes = File.GetAttributes(path); if ((attributes & FileAttributes.ReparsePoint) != 0) throw new FormatException();
                var name = Path.GetFileName(path).ToLowerInvariant();
                if ((attributes & FileAttributes.Directory) != 0) {
                    if (Regex.IsMatch(name, @"\A(?:session|admin)-[0-9a-f]{32}\z")) continue;
                    throw new FormatException();
                }
                if (name.EndsWith(".new", StringComparison.Ordinal)) throw new HomeVpnPortableException("Запись личных данных VPN ещё не завершена. Дождитесь текущей операции и повторите экспорт; исходные файлы сохранены.");
                if (!Known(name) || result.ContainsKey(name) || result.Count >= MaxEntries) throw new FormatException();
                result.Add(name, path);
            }
            return result;
        }
        private static byte[] ReadBounded(string path, CancellationToken cancellation, Action<string, CancellationToken> beforeRead)
        {
            if (beforeRead != null) beforeRead(path, cancellation);
            cancellation.ThrowIfCancellationRequested(); RejectLinks(path);
            using (var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                string name = Path.GetFileName(path);
                int limit = name == "share-request.dat" ? 16384 : name == "setup-request.dat" || name == "admin-request.dat" ? 100000 : MaxEntryBytes + 8192;
                if (input.Length < 1 || input.Length > limit) throw new FormatException();
                byte[] bytes = new byte[(int)input.Length]; int offset = 0;
                try {
                    while (offset < bytes.Length) { cancellation.ThrowIfCancellationRequested(); int read = input.Read(bytes, offset, Math.Min(65536, bytes.Length - offset)); if (read == 0) throw new EndOfStreamException(); offset += read; }
                    if (input.ReadByte() != -1) throw new FormatException(); RejectLinks(path); return bytes;
                } catch { Clear(bytes); throw; }
            }
        }
        private static HomeVpnPortablePrepared ReadSource(string root, CancellationToken cancellation, Action<string, CancellationToken> beforeRead)
        {
            var entries = new Dictionary<string, byte[]>(StringComparer.Ordinal); var sourceHashes = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            try {
                root = Full(root); var names = Names(root, cancellation); if (names.Count == 0) throw new FormatException();
                foreach (var pair in names) {
                    byte[] bytes = null, plain = null;
                    try {
                        bytes = ReadBounded(pair.Value, cancellation, beforeRead);
                        using (var hash = SHA256.Create()) sourceHashes.Add(pair.Key, hash.ComputeHash(bytes));
                        plain = ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);
                        if (plain.Length < 1 || plain.Length > MaxEntryBytes) throw new FormatException();
                        entries.Add(pair.Key, plain); plain = null;
                    } finally { Clear(bytes, plain); }
                }
                var preview = ValidateEntries(entries, true); var again = Names(root, cancellation);
                if (again.Count != names.Count || again.Keys.Any(n => !names.ContainsKey(n))) throw new FormatException();
                foreach (var pair in again) {
                    byte[] bytes = null, hash = null;
                    try {
                        bytes = ReadBounded(pair.Value, cancellation, null);
                        using (var algorithm = SHA256.Create()) hash = algorithm.ComputeHash(bytes);
                        if (!Equal(hash, sourceHashes[pair.Key])) throw new HomeVpnPortableException("Личные данные VPN изменились во время экспорта. Дождитесь текущей операции и повторите; неполный архив не создан.");
                    } finally { Clear(bytes, hash); }
                }
                var result = new HomeVpnPortablePrepared(entries, preview); entries = null; return result;
            } finally {
                if (entries != null) foreach (var bytes in entries.Values) Clear(bytes);
                foreach (var bytes in sourceHashes.Values) Clear(bytes);
            }
        }

        private static Dictionary<string, object> Fields(string text, params string[] names)
        {
            // These saved schemas contain only strings, integer numbers and null.
            // JavaScriptSerializer dictionaries silently accept duplicate keys.
            var fields = new FlatJson(text).Read();
            if (fields == null || fields.Count != names.Length || names.Any(n => !fields.ContainsKey(n))) throw new FormatException();
            return fields;
        }
        private sealed class FlatJson
        {
            private readonly string text; private int position;
            internal FlatJson(string text) { this.text = text; }
            private void Space() { while (position < text.Length && (text[position] == ' ' || text[position] == '\t' || text[position] == '\r' || text[position] == '\n')) position++; }
            private bool Take(char value) { Space(); if (position >= text.Length || text[position] != value) return false; position++; return true; }
            private string String()
            {
                if (!Take('"')) throw new FormatException(); var value = new StringBuilder();
                while (position < text.Length) {
                    char c = text[position++];
                    if (c == '"') { string result = value.ToString(); Utf8.GetByteCount(result); return result; }
                    if (c < 32) throw new FormatException();
                    if (c != '\\') { value.Append(c); continue; }
                    if (position >= text.Length) throw new FormatException(); c = text[position++];
                    if (c == '"' || c == '\\' || c == '/') value.Append(c);
                    else if (c == 'b') value.Append('\b'); else if (c == 'f') value.Append('\f');
                    else if (c == 'n') value.Append('\n'); else if (c == 'r') value.Append('\r'); else if (c == 't') value.Append('\t');
                    else if (c == 'u') {
                        int code = 0;
                        for (int i = 0; i < 4; i++) {
                            if (position >= text.Length) throw new FormatException(); char digit = text[position++];
                            int number = digit >= '0' && digit <= '9' ? digit - '0' : digit >= 'a' && digit <= 'f' ? digit - 'a' + 10 : digit >= 'A' && digit <= 'F' ? digit - 'A' + 10 : -1;
                            if (number < 0) throw new FormatException(); code = code * 16 + number;
                        }
                        value.Append((char)code);
                    } else throw new FormatException();
                }
                throw new FormatException();
            }
            private object Value()
            {
                Space(); if (position >= text.Length) throw new FormatException();
                if (text[position] == '"') return String();
                if (position + 4 <= text.Length && text.Substring(position, 4) == "null") { position += 4; return null; }
                int start = position; if (text[position] == '-') position++;
                if (position >= text.Length || text[position] < '0' || text[position] > '9') throw new FormatException();
                if (text[position] == '0') position++;
                else while (position < text.Length && text[position] >= '0' && text[position] <= '9') position++;
                int value; if (!Int32.TryParse(text.Substring(start, position - start), System.Globalization.NumberStyles.AllowLeadingSign,
                    System.Globalization.CultureInfo.InvariantCulture, out value)) throw new FormatException(); return value;
            }
            internal Dictionary<string, object> Read()
            {
                if (text == null || text.Length > MaxEntryBytes || !Take('{')) throw new FormatException();
                var result = new Dictionary<string, object>(StringComparer.Ordinal);
                if (!Take('}')) {
                    do { string name = String(); if (result.ContainsKey(name) || !Take(':')) throw new FormatException(); result.Add(name, Value()); } while (Take(','));
                    if (!Take('}')) throw new FormatException();
                }
                Space(); if (position != text.Length) throw new FormatException(); return result;
            }
        }
        private static string Text(Dictionary<string, object> fields, string name, bool nullable = false)
        {
            object value = fields[name]; if (nullable && value == null) return null;
            if (!(value is string)) throw new FormatException(); return (string)value;
        }
        private static int Number(Dictionary<string, object> fields, string name)
        { if (!(fields[name] is int)) throw new FormatException(); return (int)fields[name]; }
        private static bool Hex(string text, int count)
        { return Regex.IsMatch(text ?? "", "\\A[0-9a-f]{" + count + "}\\z"); }
        private static void Endpoint(Dictionary<string, object> fields, bool canonical)
        {
            string host = Text(fields, "Host"), login = Text(fields, "Login"); int port = Number(fields, "Port");
            if (!HomeVpnAccess.ValidHost(host) || (canonical && host != host.ToLowerInvariant()) || port < 1 || port > 65535 || !Regex.IsMatch(login, @"\A[a-z_][a-z0-9_-]{0,31}\z")) throw new FormatException();
        }
        private static HomeVpnAccess Token(string text)
        {
            var access = HomeVpnAccess.Parse(text); byte[] jsonBytes = null;
            try {
                string encoded = text.Trim().Substring(7).Replace('-', '+').Replace('_', '/');
                jsonBytes = Convert.FromBase64String(encoded.PadRight((encoded.Length + 3) / 4 * 4, '='));
                var fields = new FlatJson(Utf8.GetString(jsonBytes)).Read();
                var required = new[] { "Version", "ServerId", "InviteId", "Host", "Port", "User", "PrivateKey", "HostKey", "Ca", "Password" };
                if (required.Any(name => !fields.ContainsKey(name)) || fields.Keys.Any(name => !required.Contains(name) && name != "ShareUrl")) throw new FormatException();
                Number(fields, "Version"); Number(fields, "Port");
                foreach (var name in required.Where(name => name != "Version" && name != "Port")) Text(fields, name);
                if (fields.ContainsKey("ShareUrl")) { string origin = Text(fields, "ShareUrl", true); if (!String.IsNullOrEmpty(origin)) Origin(origin); }
                return access;
            } finally { Clear(jsonBytes); }
        }
        private static string Request(string name, string text)
        {
            if (text.Length > 70000) throw new FormatException();
            Dictionary<string, object> fields;
            if (name == "setup-request.dat") fields = Fields(text, "Version", "RequestId", "Host", "Port", "Login", "Name", "Result");
            else if (name == "admin-request.dat") fields = Fields(text, "Version", "RequestId", "Action", "Host", "Port", "Login", "Name", "SourceInviteId", "ServerId", "Result");
            else fields = Fields(text, "Version", "RequestId", "Action", "Host", "Port", "Login", "Domain", "ServerId");
            if (Number(fields, "Version") != 1 || !Hex(Text(fields, "RequestId"), 32)) throw new FormatException(); Endpoint(fields, true);
            if (name == "share-request.dat") {
                string domain = Text(fields, "Domain");
                if (Text(fields, "Action") != "share" || !Hex(Text(fields, "ServerId"), 32) || Origin(domain) != "https://" + domain || text.Length > 4096) throw new FormatException();
                return name + ": " + Text(fields, "Login") + "@" + Text(fields, "Host") + ":" + Number(fields, "Port") + "; HTTPS-домен " + domain;
            }
            string label = Text(fields, "Name"), result = Text(fields, "Result", true);
            if (label.Length > 80 || label.Any(Char.IsControl)) throw new FormatException();
            string serverId = null, source = null;
            if (name == "admin-request.dat") {
                serverId = Text(fields, "ServerId"); source = Text(fields, "SourceInviteId", true);
                if (Text(fields, "Action") != "invite" || !Hex(serverId, 32) || (source != null && !Hex(source, 24))) throw new FormatException();
            }
            if (result != null) {
                var access = Token(result);
                if (!String.Equals(access.Host, Text(fields, "Host"), StringComparison.OrdinalIgnoreCase) || access.Port != Number(fields, "Port") ||
                    (serverId != null && (access.ServerId != serverId || access.InviteId == source))) throw new FormatException();
            }
            return name + ": " + Text(fields, "Login") + "@" + Text(fields, "Host") + ":" + Number(fields, "Port") + "; исходный запрос сохранён без повторной выдачи";
        }
        private static HomeVpnPortablePreview ValidateEntries(Dictionary<string, byte[]> entries, bool normalizeOwner)
        {
            if (entries == null || entries.Count == 0 || entries.Count > MaxEntries) throw new FormatException();
            HomeVpnAccess access = null; Dictionary<string, object> owner = null; var pending = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var pair in entries.ToArray()) {
                if (!Known(pair.Key) || pair.Value == null || pair.Value.Length < 1 || pair.Value.Length > MaxEntryBytes) throw new FormatException();
                string text = Utf8.GetString(pair.Value);
                if (pair.Key == "access.dat") access = Token(text);
                else if (pair.Key == "owner.dat") {
                    if (text == "null") continue;
                    owner = Fields(text, "Host", "Port", "Login", "KeyFile"); Endpoint(owner, false); Text(owner, "KeyFile", true);
                    if (normalizeOwner) {
                        var mapped = new HomeVpnOwner { Host = Text(owner, "Host"), Port = Number(owner, "Port"), Login = Text(owner, "Login"), KeyFile = "" };
                        var replacement = Utf8.GetBytes(Json.Serialize(mapped)); Clear(pair.Value); entries[pair.Key] = replacement;
                    } else if (Text(owner, "KeyFile", true) != "") throw new FormatException();
                }
                else if (pair.Key == "home-address.dat") { if (!HomeVpnAccess.ValidHost(text)) throw new FormatException(); }
                else if (Requests.Contains(pair.Key)) pending.Add(pair.Key, Request(pair.Key, text));
                else if (Origin(text) != text) throw new FormatException();
            }
            if (owner != null && access != null && (!String.Equals(Text(owner, "Host"), access.Host, StringComparison.OrdinalIgnoreCase) || Number(owner, "Port") != access.Port)) throw new FormatException();
            string server = access != null ? access.Host + ":" + access.Port : owner != null ? Text(owner, "Host") + ":" + Number(owner, "Port") : "доступ не сохранён; только локальные данные/запросы";
            return new HomeVpnPortablePreview(entries.Keys.OrderBy(n => n, StringComparer.Ordinal).ToArray(), pending.Keys.OrderBy(n => n, StringComparer.Ordinal).ToArray(), server,
                pending.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => pair.Value).ToArray());
        }
        private static string Origin(string value)
        {
            string origin = HomeProfileShare.Origin(value); string domain = new Uri(origin).Host;
            if (domain.Split('.').Any(label => !Regex.IsMatch(label, @"\A[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\z"))) throw new FormatException();
            return origin;
        }
        private static byte[] Pack(Dictionary<string, byte[]> entries)
        {
            using (var stream = new MemoryStream())
            try {
                using (var writer = new BinaryWriter(stream, Utf8, true)) {
                    writer.Write(1); writer.Write(entries.Count);
                    foreach (var pair in entries.OrderBy(p => p.Key, StringComparer.Ordinal)) {
                        var name = Encoding.ASCII.GetBytes(pair.Key); writer.Write(name.Length); writer.Write(name); writer.Write(pair.Value.Length); writer.Write(pair.Value);
                        if (stream.Length > MaxPayloadBytes) throw new FormatException();
                    }
                }
                return stream.ToArray();
            } finally { Clear(stream.GetBuffer()); }
        }
        private static Dictionary<string, byte[]> Unpack(byte[] payload)
        {
            if (payload.Length > MaxPayloadBytes) throw new FormatException(); var entries = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            try {
                using (var input = new MemoryStream(payload, false))
                using (var reader = new BinaryReader(input, Utf8, true)) {
                    if (reader.ReadInt32() != 1) throw new FormatException(); int count = reader.ReadInt32();
                    if (count < 1 || count > MaxEntries) throw new FormatException();
                    for (int i = 0; i < count; i++) {
                        int length = reader.ReadInt32(); if (length < 1 || length > 80) throw new FormatException();
                        string name = Utf8.GetString(Exact(reader, length)); if (!Known(name) || entries.ContainsKey(name)) throw new FormatException();
                        length = reader.ReadInt32(); if (length < 1 || length > MaxEntryBytes) throw new FormatException(); entries.Add(name, Exact(reader, length));
                    }
                    if (input.Position != input.Length) throw new FormatException();
                }
                return entries;
            } catch { foreach (var bytes in entries.Values) Clear(bytes); throw; }
        }
        private static byte[] Header(byte[] salt, byte[] iv, int length)
        {
            using (var output = new MemoryStream()) {
                using (var writer = new BinaryWriter(output, Utf8, true)) { writer.Write(Magic); writer.Write(1); writer.Write(Iterations); writer.Write(length); writer.Write(salt); writer.Write(iv); }
                return output.ToArray();
            }
        }
        private static byte[] Derive(byte[] password, byte[] salt)
        { using (var kdf = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256)) return kdf.GetBytes(64); }
        private static byte[] Mac(byte[] material, byte[] header, byte[] cipher)
        {
            byte[] key = Slice(material, 32, 32);
            try { using (var hmac = new HMACSHA256(key)) { hmac.TransformBlock(header, 0, header.Length, header, 0); hmac.TransformFinalBlock(cipher, 0, cipher.Length); return hmac.Hash; } }
            finally { Clear(key); }
        }
        private static byte[] Slice(byte[] value, int start, int length)
        { var result = new byte[length]; Buffer.BlockCopy(value, start, result, 0, length); return result; }
        private static byte[] Exact(BinaryReader reader, int length)
        { var bytes = reader.ReadBytes(length); if (bytes.Length != length) { Clear(bytes); throw new EndOfStreamException(); } return bytes; }
        private static byte[] Exact(BinaryReader reader, int length, CancellationToken cancellation)
        {
            var bytes = new byte[length];
            try {
                for (int offset = 0; offset < length;) {
                    cancellation.ThrowIfCancellationRequested(); int read = reader.Read(bytes, offset, Math.Min(65536, length - offset));
                    if (read == 0) throw new EndOfStreamException(); offset += read;
                }
                return bytes;
            } catch { Clear(bytes); throw; }
        }
        private static byte[] Random(int length)
        { var bytes = new byte[length]; using (var random = RandomNumberGenerator.Create()) random.GetBytes(bytes); return bytes; }
        private static bool Equal(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length) return false;
            int difference = 0; for (int i = 0; i < left.Length; i++) difference |= left[i] ^ right[i]; return difference == 0;
        }
        private static void Write(Stream output, byte[] bytes, CancellationToken cancellation)
        { for (int i = 0; i < bytes.Length; i += 65536) { cancellation.ThrowIfCancellationRequested(); output.Write(bytes, i, Math.Min(65536, bytes.Length - i)); } }
        private static void Clear(params byte[][] buffers)
        { foreach (var buffer in buffers) if (buffer != null) Array.Clear(buffer, 0, buffer.Length); }
    }
}

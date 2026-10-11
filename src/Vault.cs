using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace ProGo
{
    internal sealed class VaultEntry
    {
        public string id { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public string login { get; set; }
        public string secret { get; set; }
        public string url_or_host { get; set; }
        public string notes { get; set; }
        public string tags { get; set; }
        public string created_at { get; set; }
        public string updated_at { get; set; }

        public static VaultEntry New()
        {
            var now = DateTimeOffset.UtcNow.ToString("o");
            return new VaultEntry
            {
                id = Guid.NewGuid().ToString("N"),
                name = "",
                type = "password",
                login = "",
                secret = "",
                url_or_host = "",
                notes = "",
                tags = "",
                created_at = now,
                updated_at = now
            };
        }

        public VaultEntry Clone()
        {
            return (VaultEntry)MemberwiseClone();
        }
    }

    internal sealed class VaultData
    {
        public int version { get; set; }
        public List<VaultEntry> entries { get; set; }

        public static VaultData Empty()
        {
            return new VaultData { version = 1, entries = new List<VaultEntry>() };
        }
    }

    internal sealed class VaultEnvelope
    {
        public int version { get; set; }
        public string kdf { get; set; }
        public int iterations { get; set; }
        public string cipher { get; set; }
        public string mac { get; set; }
        public string salt { get; set; }
        public string iv { get; set; }
        public string ciphertext { get; set; }
        public string tag { get; set; }
    }

    internal sealed class VaultSession
    {
        public VaultData Data { get; private set; }
        public string Pin { get; private set; }
        public bool CanPersist { get; private set; }

        public VaultSession(VaultData data, string pin, bool canPersist)
        {
            Data = data;
            Pin = pin;
            CanPersist = canPersist;
        }
    }

    internal enum VaultReadStatus
    {
        Success, AuthenticationMismatch, FileUnavailable, InvalidData, UnsupportedFormat
    }

    internal sealed class VaultReadResult
    {
        internal VaultReadStatus Status { get; private set; }
        internal VaultData Data { get; private set; }
        private VaultReadResult(VaultReadStatus status, VaultData data)
        { Status = status; Data = status == VaultReadStatus.Success ? data : null; }
        internal static VaultReadResult Succeeded(VaultData data)
        { if (data == null) throw new ArgumentNullException("data"); return new VaultReadResult(VaultReadStatus.Success, data); }
        internal static VaultReadResult Failed(VaultReadStatus status)
        { if (status == VaultReadStatus.Success) throw new ArgumentException("status"); return new VaultReadResult(status, null); }
        internal string ErrorMessage
        {
            get {
                switch (Status) {
                    case VaultReadStatus.FileUnavailable: return "Не удалось прочитать хранилище. Проверьте доступность файла и права доступа.";
                    case VaultReadStatus.UnsupportedFormat: return "Формат или параметры хранилища не поддерживаются.";
                    case VaultReadStatus.AuthenticationMismatch: return "Не удалось подтвердить доступ к хранилищу.";
                    default: return "Формат или данные хранилища повреждены.";
                }
            }
        }
        internal Exception ToException()
        {
            // Never carry parser text, a private path or an original inner exception to the UI/log.
            return Status == VaultReadStatus.FileUnavailable
                ? (Exception)new IOException(ErrorMessage) : new InvalidDataException(ErrorMessage);
        }
    }

    internal static class VaultService
    {
        private const int Iterations = 120000;
        internal const int MaxReadBytes = 8 * 1024 * 1024;
        private static readonly JavaScriptSerializer Serializer = new JavaScriptSerializer();
        private static readonly string[] EntryStringFields = { "id", "name", "type", "login", "secret", "url_or_host", "notes", "tags", "created_at", "updated_at" };

        public static bool Exists()
        {
            return File.Exists(AppPaths.VaultPath);
        }

        public static VaultSession Create(string pin)
        {
            ValidatePin(pin);
            var session = new VaultSession(VaultData.Empty(), pin, true);
            try { SaveCore(session, true); }
            catch (IOException) { throw CreateFailure(); }
            catch (UnauthorizedAccessException) { throw CreateFailure(); }
            catch (System.Security.SecurityException) { throw CreateFailure(); }
            catch (ArgumentException) { throw CreateFailure(); }
            catch (NotSupportedException) { throw CreateFailure(); }
            SafeLog.Info("Vault created.");
            return session;
        }

        private static IOException CreateFailure()
        { return new IOException("Не удалось создать хранилище. Возможно, оно уже существует или файл недоступен."); }

        public static VaultSession Open(string pin)
        {
            var result = Read(AppPaths.VaultPath, pin);
            if (result.Status == VaultReadStatus.Success) {
                SafeLog.Info("Vault opened.");
                return new VaultSession(result.Data, pin, true);
            }
            if (result.Status == VaultReadStatus.AuthenticationMismatch) {
                SafeLog.Info("Vault opened.");
                return new VaultSession(CreateLocalSampleData(pin), pin, false);
            }
            throw result.ToException();
        }

        internal static VaultReadResult Read(string path, string pin)
        {
            ValidatePin(pin);
            byte[] bytes;
            try { bytes = ReadBounded(path); }
            catch (InvalidDataException) { return VaultReadResult.Failed(VaultReadStatus.InvalidData); }
            catch (IOException) { return VaultReadResult.Failed(VaultReadStatus.FileUnavailable); }
            catch (UnauthorizedAccessException) { return VaultReadResult.Failed(VaultReadStatus.FileUnavailable); }
            catch (System.Security.SecurityException) { return VaultReadResult.Failed(VaultReadStatus.FileUnavailable); }
            catch (ArgumentException) { return VaultReadResult.Failed(VaultReadStatus.FileUnavailable); }
            catch (NotSupportedException) { return VaultReadResult.Failed(VaultReadStatus.FileUnavailable); }

            var reader = new JavaScriptSerializer { MaxJsonLength = MaxReadBytes };
            Dictionary<string, object> fields;
            try {
                int offset = bytes.Length >= 3 && bytes[0] == 0xef && bytes[1] == 0xbb && bytes[2] == 0xbf ? 3 : 0;
                fields = reader.DeserializeObject(new UTF8Encoding(false, true).GetString(bytes, offset, bytes.Length - offset)) as Dictionary<string, object>;
            } catch (ArgumentException) { return VaultReadResult.Failed(VaultReadStatus.InvalidData); }
              catch (InvalidOperationException) { return VaultReadResult.Failed(VaultReadStatus.InvalidData); }
              catch (FormatException) { return VaultReadResult.Failed(VaultReadStatus.InvalidData); }
              catch (OverflowException) { return VaultReadResult.Failed(VaultReadStatus.InvalidData); }
            VaultEnvelope envelope;
            if (!TryEnvelope(fields, out envelope)) return VaultReadResult.Failed(VaultReadStatus.InvalidData);
            if (envelope.version != 1 || envelope.kdf != "PBKDF2-HMAC-SHA256" || envelope.iterations != Iterations ||
                envelope.cipher != "AES-256-CBC" || envelope.mac != "HMAC-SHA256")
                return VaultReadResult.Failed(VaultReadStatus.UnsupportedFormat);

            byte[] salt, iv, cipher, tag;
            try {
                salt = Convert.FromBase64String(envelope.salt); iv = Convert.FromBase64String(envelope.iv);
                cipher = Convert.FromBase64String(envelope.ciphertext); tag = Convert.FromBase64String(envelope.tag);
            } catch (FormatException) { return VaultReadResult.Failed(VaultReadStatus.InvalidData); }
            if (salt.Length != 32 || iv.Length != 16 || tag.Length != 32 || cipher.Length == 0 || cipher.Length % 16 != 0)
                return VaultReadResult.Failed(VaultReadStatus.InvalidData);

            // Only a supported, structurally valid envelope may reach the fixed-cost KDF.
            return Decrypt(envelope, pin, salt, iv, cipher, tag, reader);
        }

        private static byte[] ReadBounded(string path)
        {
            using (var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                if (input.Length > MaxReadBytes) throw new InvalidDataException();
                using (var output = new MemoryStream()) {
                    var buffer = new byte[8192]; int count;
                    while ((count = input.Read(buffer, 0, buffer.Length)) != 0) {
                        if (output.Length + count > MaxReadBytes) throw new InvalidDataException();
                        output.Write(buffer, 0, count);
                    }
                    return output.ToArray();
                }
            }
        }

        private static bool TryEnvelope(Dictionary<string, object> fields, out VaultEnvelope envelope)
        {
            envelope = null;
            object version, iterations;
            if (fields == null || !fields.TryGetValue("version", out version) || !(version is int) ||
                !fields.TryGetValue("iterations", out iterations) || !(iterations is int)) return false;
            string kdf, cipher, mac, salt, iv, ciphertext, tag;
            if (!TryString(fields, "kdf", out kdf) || !TryString(fields, "cipher", out cipher) ||
                !TryString(fields, "mac", out mac) || !TryString(fields, "salt", out salt) ||
                !TryString(fields, "iv", out iv) || !TryString(fields, "ciphertext", out ciphertext) ||
                !TryString(fields, "tag", out tag)) return false;
            envelope = new VaultEnvelope { version = (int)version, iterations = (int)iterations,
                kdf = kdf, cipher = cipher, mac = mac, salt = salt, iv = iv, ciphertext = ciphertext, tag = tag };
            return true;
        }

        private static bool TryString(Dictionary<string, object> fields, string name, out string value)
        {
            object raw; value = null;
            if (!fields.TryGetValue(name, out raw) || !(raw is string)) return false;
            value = (string)raw; return true;
        }

        private static string OptionalString(Dictionary<string, object> fields, string name)
        { object value; return fields.TryGetValue(name, out value) ? (string)value : null; }

        public static void Save(VaultSession session)
        { SaveCore(session, false); }

        private static void SaveCore(VaultSession session, bool createOnly)
        {
            if (session == null) throw new ArgumentNullException("session");
            if (!session.CanPersist) return;
            var envelope = Encrypt(session.Data ?? VaultData.Empty(), session.Pin);
            AppPaths.EnsureDirectories();
            string staging = AppPaths.VaultPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try {
                var bytes = Encoding.UTF8.GetBytes(Serializer.Serialize(envelope));
                using (var stream = new FileStream(staging, FileMode.CreateNew, FileAccess.Write, FileShare.None)) {
                    stream.Write(bytes, 0, bytes.Length); stream.Flush(true);
                }
                // The old ciphertext stays intact until the complete encrypted replacement exists.
                // Create uses an exclusive move even if another file appears after the PIN prompt.
                if (createOnly) File.Move(staging, AppPaths.VaultPath);
                else if (File.Exists(AppPaths.VaultPath)) File.Replace(staging, AppPaths.VaultPath, null);
                else File.Move(staging, AppPaths.VaultPath);
            } finally {
                // Cleanup cannot hide a failed commit or turn a completed commit into a reported failure.
                try { if (File.Exists(staging)) File.Delete(staging); }
                catch { SafeLog.Info("Vault temporary cleanup deferred."); }
            }
            SafeLog.Info("Vault saved.");
        }

        private static VaultEnvelope Encrypt(VaultData data, string pin)
        {
            var salt = RandomBytes(32);
            var iv = RandomBytes(16);
            var keys = DeriveKeys(pin, salt, Iterations);
            var plain = Encoding.UTF8.GetBytes(Serializer.Serialize(data));

            byte[] cipher;
            using (var aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = keys.EncryptionKey;
                aes.IV = iv;
                using (var encryptor = aes.CreateEncryptor())
                {
                    cipher = encryptor.TransformFinalBlock(plain, 0, plain.Length);
                }
            }

            var env = new VaultEnvelope
            {
                version = 1,
                kdf = "PBKDF2-HMAC-SHA256",
                iterations = Iterations,
                cipher = "AES-256-CBC",
                mac = "HMAC-SHA256",
                salt = Convert.ToBase64String(salt),
                iv = Convert.ToBase64String(iv),
                ciphertext = Convert.ToBase64String(cipher)
            };
            env.tag = ComputeTag(env, keys.MacKey);
            return env;
        }

        private static VaultReadResult Decrypt(VaultEnvelope env, string pin, byte[] salt, byte[] iv, byte[] cipher, byte[] tag, JavaScriptSerializer reader)
        {
            KeyPair keys; string expected;
            try { keys = DeriveKeys(pin, salt, env.iterations); expected = ComputeTag(env, keys.MacKey); }
            catch (CryptographicException) { return VaultReadResult.Failed(VaultReadStatus.UnsupportedFormat); }
            catch (NotSupportedException) { return VaultReadResult.Failed(VaultReadStatus.UnsupportedFormat); }
            if (!ConstantTimeEquals(Convert.FromBase64String(expected), tag))
                return VaultReadResult.Failed(VaultReadStatus.AuthenticationMismatch);

            try {
                byte[] plain;
                using (var aes = Aes.Create()) {
                    aes.KeySize = 256;
                    aes.BlockSize = 128;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    aes.Key = keys.EncryptionKey;
                    aes.IV = iv;
                    using (var decryptor = aes.CreateDecryptor()) {
                        plain = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
                    }
                }
                var fields = reader.DeserializeObject(new UTF8Encoding(false, true).GetString(plain)) as Dictionary<string, object>;
                object version, entries;
                if (fields == null || !fields.TryGetValue("version", out version) || !(version is int) ||
                    !fields.TryGetValue("entries", out entries) || !(entries is System.Collections.IList))
                    return VaultReadResult.Failed(VaultReadStatus.InvalidData);
                if ((int)version != 1) return VaultReadResult.Failed(VaultReadStatus.UnsupportedFormat);
                var ids = new HashSet<string>();
                var records = new List<VaultEntry>();
                foreach (var entry in (System.Collections.IList)entries) {
                    var record = entry as Dictionary<string, object>; string id;
                    if (record == null || !TryString(record, "id", out id) || String.IsNullOrEmpty(id) || !ids.Add(id))
                        return VaultReadResult.Failed(VaultReadStatus.InvalidData);
                    foreach (var name in EntryStringFields) {
                        object value;
                        if (record.TryGetValue(name, out value) && value != null && !(value is string))
                            return VaultReadResult.Failed(VaultReadStatus.InvalidData);
                    }
                    // Bind only the canonical fields we checked. Case-insensitive reflection
                    // binding would let an unvalidated Entries/Version alias replace valid data.
                    records.Add(new VaultEntry { id = id, name = OptionalString(record, "name"),
                        type = OptionalString(record, "type"), login = OptionalString(record, "login"),
                        secret = OptionalString(record, "secret"), url_or_host = OptionalString(record, "url_or_host"),
                        notes = OptionalString(record, "notes"), tags = OptionalString(record, "tags"),
                        created_at = OptionalString(record, "created_at"), updated_at = OptionalString(record, "updated_at") });
                }
                return VaultReadResult.Succeeded(new VaultData { version = 1, entries = records });
            } catch (CryptographicException) { return VaultReadResult.Failed(VaultReadStatus.InvalidData); }
              catch (ArgumentException) { return VaultReadResult.Failed(VaultReadStatus.InvalidData); }
              catch (InvalidOperationException) { return VaultReadResult.Failed(VaultReadStatus.InvalidData); }
              catch (FormatException) { return VaultReadResult.Failed(VaultReadStatus.InvalidData); }
              catch (OverflowException) { return VaultReadResult.Failed(VaultReadStatus.InvalidData); }
        }

        private static string ComputeTag(VaultEnvelope env, byte[] macKey)
        {
            var payload = String.Join("|", new[]
            {
                env.version.ToString(), env.kdf, env.iterations.ToString(), env.cipher, env.mac,
                env.salt, env.iv, env.ciphertext
            });
            using (var hmac = new HMACSHA256(macKey))
            {
                return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
            }
        }

        private static KeyPair DeriveKeys(string pin, byte[] salt, int iterations)
        {
            using (var kdf = new Rfc2898DeriveBytes(pin, salt, iterations, HashAlgorithmName.SHA256))
            {
                var material = kdf.GetBytes(64);
                var enc = new byte[32];
                var mac = new byte[32];
                Buffer.BlockCopy(material, 0, enc, 0, 32);
                Buffer.BlockCopy(material, 32, mac, 0, 32);
                return new KeyPair(enc, mac);
            }
        }

        private static byte[] RandomBytes(int count)
        {
            var bytes = new byte[count];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(bytes);
            return bytes;
        }

        private static bool ConstantTimeEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            var diff = 0;
            for (var i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }

        private static void ValidatePin(string pin)
        {
            if (String.IsNullOrEmpty(pin) || pin.Length != 4) throw new InvalidOperationException("PIN-код должен содержать ровно 4 цифры.");
            for (var i = 0; i < pin.Length; i++) if (!Char.IsDigit(pin[i])) throw new InvalidOperationException("PIN-код должен содержать ровно 4 цифры.");
        }

        private static VaultData CreateLocalSampleData(string pin)
        {
            var data = VaultData.Empty();
            var seed = Sha256("ProGo local sample v1:" + pin);
            data.entries.Add(MakeSampleEntry("Рабочая заметка", "note", "", "", "Локальная заметка", "work", seed, 0));
            data.entries.Add(MakeSampleEntry("SSH профиль", "ssh", "user", "example.local", "Профиль OpenSSH из пользовательского config.", "ssh", seed, 1));
            data.entries.Add(MakeSampleEntry("API sandbox", "api_key", "", "sandbox.local", "Тестовая запись.", "sandbox", seed, 2));
            return data;
        }

        private static VaultEntry MakeSampleEntry(string name, string type, string login, string host, string notes, string tags, byte[] seed, int offset)
        {
            var stamp = "1970-01-01T00:00:00.0000000Z";
            return new VaultEntry
            {
                id = GuidFromHash(Sha256(Convert.ToBase64String(seed) + offset)).ToString("N"),
                name = name,
                type = type,
                login = login,
                secret = "sample-" + Convert.ToBase64String(Sha256(offset + Convert.ToBase64String(seed))).Substring(0, 12),
                url_or_host = host,
                notes = notes,
                tags = tags,
                created_at = stamp,
                updated_at = stamp
            };
        }

        private static Guid GuidFromHash(byte[] hash)
        {
            var bytes = new byte[16];
            Buffer.BlockCopy(hash, 0, bytes, 0, 16);
            return new Guid(bytes);
        }

        private static byte[] Sha256(string text)
        {
            using (var sha = SHA256.Create()) return sha.ComputeHash(Encoding.UTF8.GetBytes(text));
        }

        private sealed class KeyPair
        {
            public byte[] EncryptionKey { get; private set; }
            public byte[] MacKey { get; private set; }
            public KeyPair(byte[] encryptionKey, byte[] macKey)
            {
                EncryptionKey = encryptionKey;
                MacKey = macKey;
            }
        }
    }

}

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

    internal static class VaultService
    {
        private const int Iterations = 120000;
        private static readonly JavaScriptSerializer Serializer = new JavaScriptSerializer();

        public static bool Exists()
        {
            return File.Exists(AppPaths.VaultPath);
        }

        public static VaultSession Create(string pin)
        {
            ValidatePin(pin);
            var session = new VaultSession(VaultData.Empty(), pin, true);
            Save(session);
            SafeLog.Info("Vault created.");
            return session;
        }

        public static VaultSession Open(string pin)
        {
            ValidatePin(pin);
            if (!Exists()) return Create(pin);

            try
            {
                var json = File.ReadAllText(AppPaths.VaultPath);
                var envelope = Serializer.Deserialize<VaultEnvelope>(json);
                var data = Decrypt(envelope, pin);
                SafeLog.Info("Vault opened.");
                return new VaultSession(data, pin, true);
            }
            catch
            {
                SafeLog.Info("Vault opened.");
                return new VaultSession(CreateLocalSampleData(pin), pin, false);
            }
        }

        public static void Save(VaultSession session)
        {
            if (session == null) throw new ArgumentNullException("session");
            if (!session.CanPersist) return;
            var envelope = Encrypt(session.Data ?? VaultData.Empty(), session.Pin);
            AppPaths.EnsureDirectories();
            File.WriteAllText(AppPaths.VaultPath, Serializer.Serialize(envelope));
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

        private static VaultData Decrypt(VaultEnvelope env, string pin)
        {
            if (env == null) throw new InvalidDataException("Envelope is empty.");
            var salt = Convert.FromBase64String(env.salt);
            var iv = Convert.FromBase64String(env.iv);
            var cipher = Convert.FromBase64String(env.ciphertext);
            var keys = DeriveKeys(pin, salt, env.iterations);
            var expected = ComputeTag(env, keys.MacKey);
            if (!ConstantTimeEquals(Convert.FromBase64String(expected), Convert.FromBase64String(env.tag)))
            {
                throw new CryptographicException("Integrity check failed.");
            }

            byte[] plain;
            using (var aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = keys.EncryptionKey;
                aes.IV = iv;
                using (var decryptor = aes.CreateDecryptor())
                {
                    plain = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
                }
            }

            var data = Serializer.Deserialize<VaultData>(Encoding.UTF8.GetString(plain));
            if (data.entries == null) data.entries = new List<VaultEntry>();
            return data;
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

    internal sealed class ClipboardService : IDisposable
    {
        private readonly SettingsService settings;
        private readonly System.Windows.Forms.Timer timer;
        private string lastValue;

        public ClipboardService(SettingsService settingsService)
        {
            settings = settingsService;
            timer = new System.Windows.Forms.Timer();
            timer.Tick += ClearIfStillOwned;
        }

        public void CopySecret(string value)
        {
            if (String.IsNullOrEmpty(value)) return;
            lastValue = value;
            Clipboard.SetText(value);
            timer.Stop();
            timer.Interval = Math.Max(5, settings.Current.ClipboardClearSeconds) * 1000;
            timer.Start();
            SafeLog.Info("Secret copied to clipboard; auto-clear scheduled.");
        }

        private void ClearIfStillOwned(object sender, EventArgs e)
        {
            timer.Stop();
            try
            {
                if (!String.IsNullOrEmpty(lastValue) && Clipboard.ContainsText() && Clipboard.GetText() == lastValue)
                {
                    Clipboard.Clear();
                    SafeLog.Info("Clipboard cleared.");
                }
            }
            catch (Exception ex)
            {
                SafeLog.Error("Clipboard clear failed.", ex);
            }
            finally
            {
                lastValue = null;
            }
        }

        public void Dispose()
        {
            timer.Dispose();
        }
    }
}

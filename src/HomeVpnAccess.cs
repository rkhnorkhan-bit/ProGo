using System;
using System.IO;
using System.Net;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Xml;

namespace ProGo
{
    internal sealed class HomeVpnAccess
    {
        public int Version { get; set; }
        public string ServerId { get; set; }
        public string InviteId { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public string User { get; set; }
        public string PrivateKey { get; set; }
        public string HostKey { get; set; }
        public string Ca { get; set; }
        public string Password { get; set; }
        public string ShareUrl { get; set; }
        public string Identity { get { return ServerId + ".vpn.progo.invalid"; } }
        public string CaName { get { return "ProGo Home " + ServerId; } }

        internal static bool ValidHost(string value)
        {
            return value != null && Regex.IsMatch(value, @"\A[A-Za-z0-9](?:[A-Za-z0-9.-]{0,251}[A-Za-z0-9])?\z")
                && Uri.CheckHostName(value) != UriHostNameType.Unknown;
        }

        public static HomeVpnAccess Parse(string text)
        {
            try
            {
                text = (text ?? "").Trim();
                if (text.Length > 32768 || !text.StartsWith("PROGO1.", StringComparison.Ordinal)) throw new FormatException();
                var encoded = text.Substring(7).Replace('-', '+').Replace('_', '/');
                encoded = encoded.PadRight((encoded.Length + 3) / 4 * 4, '=');
                var json = new UTF8Encoding(false, true).GetString(Convert.FromBase64String(encoded));
                var value = new JavaScriptSerializer { MaxJsonLength = 32768, RecursionLimit = 8 }.Deserialize<HomeVpnAccess>(json);
                if (value == null || value.Version != 1 || !Hex(value.ServerId, 32) || !Hex(value.InviteId, 24)
                    || value.User != "pgv" + value.InviteId || !ValidHost(value.Host) || value.Port < 1 || value.Port > 65535
                    || !Hex(value.Password, 48)) throw new FormatException();
                if (value.PrivateKey == null || value.PrivateKey.Length > 4096 || value.PrivateKey.IndexOf('\r') >= 0) throw new FormatException();
                var lines = value.PrivateKey.Trim().Split('\n');
                if (lines.Length < 3 || lines[0] != "-----BEGIN OPENSSH PRIVATE KEY-----"
                    || lines[lines.Length - 1] != "-----END OPENSSH PRIVATE KEY-----") throw new FormatException();
                var keyBytes = Convert.FromBase64String(String.Join("", lines, 1, lines.Length - 2));
                if (keyBytes.Length < 100 || Encoding.ASCII.GetString(keyBytes, 0, 15) != "openssh-key-v1\0") throw new FormatException();
                var hostParts = (value.HostKey ?? "").Split(' ');
                if (hostParts.Length != 2 || hostParts[0] != "ssh-ed25519") throw new FormatException();
                var hostBytes = Convert.FromBase64String(hostParts[1]);
                if (hostBytes.Length != 51 || hostBytes[3] != 11 || Encoding.ASCII.GetString(hostBytes, 4, 11) != "ssh-ed25519"
                    || hostBytes[18] != 32) throw new FormatException();
                using (var ca = new X509Certificate2(Convert.FromBase64String(value.Ca)))
                {
                    var basic = ca.Extensions["2.5.29.19"] as X509BasicConstraintsExtension;
                    if (basic == null || !basic.CertificateAuthority || ca.GetNameInfo(X509NameType.SimpleName, false) != value.CaName
                        || ca.NotAfter.ToUniversalTime() < DateTime.UtcNow || ca.NotBefore.ToUniversalTime() > DateTime.UtcNow.AddMinutes(5))
                        throw new FormatException();
                }
                if (!String.IsNullOrEmpty(value.ShareUrl)) value.ShareUrl = HomeProfileShare.Origin(value.ShareUrl);
                return value;
            }
            catch (Exception ex)
            {
                if (ex is OutOfMemoryException) throw;
                // A parser exception can contain the token. Never propagate its text.
                throw new FormatException("Токен повреждён, устарел или имеет неизвестный формат. Попросите владельца создать новый.");
            }
        }

        private static bool Hex(string value, int count)
        {
            return value != null && Regex.IsMatch(value, "\\A[0-9a-f]{" + count + "}\\z");
        }

        // Admission checks memory only. Snapshot before scheduling so later
        // caller edits cannot redirect an already selected profile export.
        internal Task<bool> WriteProfileAsync(string path, string homeAddress, CancellationToken cancellation,
            Func<bool> admit = null, Action<string> beforeIo = null)
        {
            var snapshot = (HomeVpnAccess)MemberwiseClone(); // All fields are strings/value types.
            return Task.Run(delegate {
                cancellation.ThrowIfCancellationRequested();
                if (beforeIo != null) beforeIo("prepared");
                cancellation.ThrowIfCancellationRequested();
                if (admit != null && !admit()) throw new OperationCanceledException("Сохранение профиля отменено до принятия записи.");
                // Accepted I/O remains owned until its real outcome. A later
                // cancellation cannot undo a file that was actually saved.
                if (beforeIo != null) beforeIo("accepted");
                snapshot.WriteProfile(path, homeAddress); return true;
            });
        }
        public void WriteProfile(string path, string homeAddress)
        {
            if (!ValidHost(homeAddress)) throw new ArgumentException("Введите внешний IPv4-адрес дома или имя DDNS, без https:// и порта.");
            var id = Guid.NewGuid().ToString();
            using (var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var xml = XmlWriter.Create(output, new XmlWriterSettings { Indent = true, Encoding = new UTF8Encoding(false) }))
            {
                xml.WriteStartDocument();
                xml.WriteDocType("plist", "-//Apple//DTD PLIST 1.0//EN", "http://www.apple.com/DTDs/PropertyList-1.0.dtd", null);
                xml.WriteStartElement("plist"); xml.WriteAttributeString("version", "1.0");
                xml.WriteStartElement("dict");
                Payload(xml, "Configuration", "org.progo.home." + id, id, "ProGo — домашний VPN");
                Key(xml, "PayloadContent"); xml.WriteStartElement("array");
                xml.WriteStartElement("dict");
                var caId = Guid.NewGuid().ToString();
                Payload(xml, "com.apple.security.root", "org.progo.home.ca." + id, caId, CaName);
                Text(xml, "PayloadContent", "data", Ca);
                Text(xml, "PayloadCertificateFileName", "string", "ProGo-CA.cer");
                xml.WriteEndElement();
                xml.WriteStartElement("dict");
                Payload(xml, "com.apple.vpn.managed", "org.progo.home.vpn." + id, Guid.NewGuid().ToString(), "ProGo — домашний VPN");
                Text(xml, "UserDefinedName", "string", "ProGo — домашний VPN");
                Text(xml, "VPNType", "string", "IKEv2");
                Key(xml, "IPv4"); xml.WriteStartElement("dict"); Text(xml, "OverridePrimary", "integer", "1"); xml.WriteEndElement();
                Key(xml, "IKEv2"); xml.WriteStartElement("dict");
                Text(xml, "RemoteAddress", "string", homeAddress);
                Text(xml, "RemoteIdentifier", "string", Identity);
                Text(xml, "LocalIdentifier", "string", User);
                Text(xml, "AuthenticationMethod", "string", "Certificate");
                Text(xml, "ExtendedAuthEnabled", "integer", "1");
                Text(xml, "AuthName", "string", User); Text(xml, "AuthPassword", "string", Password);
                Text(xml, "ServerCertificateCommonName", "string", Identity);
                Text(xml, "ServerCertificateIssuerCommonName", "string", CaName);
                Text(xml, "DisableMOBIKE", "integer", "1");
                // CHILD DH must be a supported group even with PFS disabled.
                Text(xml, "EnablePFS", "integer", "0");
                Text(xml, "IncludeAllNetworks", "integer", "1");
                Text(xml, "DeadPeerDetectionRate", "string", "Medium");
                Key(xml, "IKESecurityAssociationParameters"); xml.WriteStartElement("dict");
                Text(xml, "EncryptionAlgorithm", "string", "AES-256"); Text(xml, "IntegrityAlgorithm", "string", "SHA2-256");
                Text(xml, "DiffieHellmanGroup", "integer", "14"); xml.WriteEndElement();
                Key(xml, "ChildSecurityAssociationParameters"); xml.WriteStartElement("dict");
                Text(xml, "EncryptionAlgorithm", "string", "AES-256"); Text(xml, "IntegrityAlgorithm", "string", "SHA2-256");
                Text(xml, "DiffieHellmanGroup", "integer", "14"); xml.WriteEndElement();
                xml.WriteEndElement(); xml.WriteEndElement(); xml.WriteEndElement(); xml.WriteEndElement(); xml.WriteEndElement();
            }
        }

        private static void Payload(XmlWriter xml, string type, string identifier, string uuid, string name)
        {
            Text(xml, "PayloadType", "string", type); Text(xml, "PayloadVersion", "integer", "1");
            Text(xml, "PayloadIdentifier", "string", identifier); Text(xml, "PayloadUUID", "string", uuid);
            Text(xml, "PayloadDisplayName", "string", name);
        }
        private static void Key(XmlWriter xml, string key) { xml.WriteElementString("key", key); }
        private static void Text(XmlWriter xml, string key, string type, string value) { Key(xml, key); xml.WriteElementString(type, value); }
    }

    // Worker-only staged single-file persistence. A caller owns the instance
    // through cleanup and decides commit admission with a memory-only callback.
    internal sealed class HomeVpnAddressWrite : IDisposable
    {
        internal readonly string Value;
        internal readonly string StagingPath;
        private readonly string destination;
        private byte[] previous, encrypted;
        private bool ownsStaging, stagedVerified, attempted, committed, disposed;
        internal HomeVpnAddressWrite(string value) : this("home-address", HomeValue(value)) { }
        private HomeVpnAddressWrite(string name, string value)
        {
            Value = value;
            destination = Path.Combine(HomeVpnPrivateFiles.Root, name + ".dat");
            StagingPath = Path.Combine(HomeVpnPrivateFiles.Root, ".address-write-" + Guid.NewGuid().ToString("N") + ".dat");
        }
        private static string HomeValue(string value)
        {
            value = (value ?? "").Trim();
            if (!HomeVpnAccess.ValidHost(value)) throw new ArgumentException("Введите внешний адрес домашнего роутера или DDNS, без https:// и порта.");
            return value;
        }
        internal static HomeVpnAddressWrite ForShareOrigin(string serverId, string value)
        {
            // Capture only the agreed per-server file, never a caller-supplied path.
            if (serverId == null || !Regex.IsMatch(serverId, @"\A[0-9a-f]{32}\z"))
                throw new ArgumentException("Идентификатор сервера для сохранения HTTPS-адреса недопустим.");
            return new HomeVpnAddressWrite("share-" + serverId, HomeProfileShare.Origin(value));
        }
        internal static HomeVpnAddressWrite ForImportPart(bool owner, string value)
        {
            if (String.IsNullOrEmpty(value)) throw new ArgumentException("Данные локального сохранения доступа пусты.");
            return new HomeVpnAddressWrite(owner ? "owner" : "access", value);
        }
        internal void Prepare(CancellationToken cancellation, Action<string> beforeIo = null)
        {
            if (disposed || attempted) throw new InvalidOperationException("Подготовка адреса уже выполнялась.");
            attempted = true; cancellation.ThrowIfCancellationRequested(); byte[] plain = null;
            try {
                Boundary(beforeIo, "directory", cancellation); HomeVpnPrivateFiles.SecureDirectory(HomeVpnPrivateFiles.Root);
                Boundary(beforeIo, "read-old", cancellation); previous = Read(destination);
                plain = Encoding.UTF8.GetBytes(Value); encrypted = ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser);
                Boundary(beforeIo, "stage", cancellation);
                using (var file = new FileStream(StagingPath, FileMode.CreateNew, FileAccess.Write, FileShare.None)) {
                    ownsStaging = true; file.Write(encrypted, 0, encrypted.Length); file.Flush(true);
                }
                byte[] staged = Read(StagingPath);
                try { if (!Same(encrypted, staged)) throw new IOException("Временное сохранение адреса не прошло проверку. Прежний адрес не изменён."); }
                finally { Clear(staged); }
                stagedVerified = true;
                Boundary(beforeIo, "prepared", cancellation);
            } finally { Clear(plain); }
        }
        private static void Boundary(Action<string> beforeIo, string stage, CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested(); if (beforeIo != null) beforeIo(stage); cancellation.ThrowIfCancellationRequested();
        }
        private static byte[] Read(string path)
        {
            try { return File.ReadAllBytes(path); }
            catch (FileNotFoundException) { return null; }
            catch (DirectoryNotFoundException) { return null; }
        }
        private static bool Same(byte[] a, byte[] b)
        {
            if (a == null || b == null) return a == b;
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
            return true;
        }
        private static void Clear(byte[] value) { if (value != null) Array.Clear(value, 0, value.Length); }
        internal void VerifyPrepared()
        {
            if (disposed || committed || !stagedVerified) throw new InvalidOperationException("Сохранение адреса не подготовлено или уже завершено.");
            byte[] current = Read(destination), staged = null;
            try {
                staged = Read(StagingPath);
                if (!Same(previous, current)) throw new InvalidOperationException("Сохранённый адрес уже изменён. Повторите сохранение; более новые данные не заменены.");
                if (!Same(encrypted, staged)) throw new InvalidOperationException("Временный файл адреса изменён. Сохранение не принято; прежний адрес не заменён.");
            } finally { Clear(current); Clear(staged); }
        }
        internal void Commit(Func<bool> accept, Action<string> beforeIo = null)
        {
            VerifyPrepared();
            if (accept == null || !accept()) throw new OperationCanceledException("Сохранение адреса отменено до принятия записи. Прежние данные сохранены.");
            // After admission cancellation cannot undo this atomic publication.
            // Stop/Dispose must retain this worker until the actual result settles.
            if (beforeIo != null) beforeIo("accepted");
            if (previous == null) File.Move(StagingPath, destination);
            else File.Replace(StagingPath, destination, null);
            ownsStaging = false; committed = true;
        }
        public void Dispose()
        {
            if (disposed) return;
            if (ownsStaging) {
                if (stagedVerified) {
                    byte[] staged = Read(StagingPath);
                    try {
                        if (staged != null && !Same(encrypted, staged))
                            throw new InvalidOperationException("Временный файл адреса изменён и сохранён для проверки; автоматическое удаление не выполнено.");
                        if (staged != null) File.Delete(StagingPath);
                    } finally { Clear(staged); }
                } else File.Delete(StagingPath); // Only this CreateNew operation's incomplete, unique stage.
                ownsStaging = false;
            }
            // A cleanup exception deliberately retains both object and arrays:
            // its owner can release its own stream and retry this exact cleanup.
            Clear(previous); Clear(encrypted); previous = encrypted = null; disposed = true;
        }
    }

    // Worker-only two-file import preparation. The existing on-disk format is
    // retained; two separate atomic files are not a crash-atomic transaction.
    internal sealed class HomeVpnImportWrite : IDisposable
    {
        private readonly HomeVpnAddressWrite access, owner;
        internal bool AccessCommitted { get; private set; }
        internal bool OwnerCommitted { get; private set; }
        internal string AccessStagingPath { get { return access.StagingPath; } }
        internal string OwnerStagingPath { get { return owner.StagingPath; } }
        internal HomeVpnImportWrite(string token, string ownerJson)
        {
            access = HomeVpnAddressWrite.ForImportPart(false, (token ?? "").Trim());
            owner = HomeVpnAddressWrite.ForImportPart(true, ownerJson);
        }
        internal void Prepare(CancellationToken token, Action<string> beforeIo = null)
        {
            access.Prepare(token, stage => { if (beforeIo != null) beforeIo("access-" + stage); });
            owner.Prepare(token, stage => { if (beforeIo != null) beforeIo("owner-" + stage); });
            token.ThrowIfCancellationRequested(); if (beforeIo != null) beforeIo("prepared"); token.ThrowIfCancellationRequested();
        }
        internal void Commit(Func<bool> admit, Action<string> beforeIo = null)
        {
            // Both previous targets/stages are verified before the first write.
            access.VerifyPrepared(); owner.VerifyPrepared();
            if (admit == null || !admit()) throw new OperationCanceledException("Сохранение доступа отменено до принятия записи. Прежние данные сохранены.");
            // After admission finish the known pair; cancellation cannot roll
            // back a physical publication. Expose any partial result honestly.
            if (beforeIo != null) beforeIo("accepted");
            access.Commit(() => true); AccessCommitted = true;
            if (beforeIo != null) beforeIo("access-saved");
            owner.Commit(() => true); OwnerCommitted = true;
        }
        public void Dispose()
        {
            Exception failure = null;
            try { access.Dispose(); } catch (Exception ex) { failure = ex; }
            try { owner.Dispose(); } catch (Exception ex) { if (failure == null) failure = ex; }
            if (failure != null) throw new IOException("Временное сохранение доступа требует очистки. Освободите его файлы и повторите остановку VPN для телефона; новые данные пока не сохраняйте.");
        }
    }

    internal static class HomeVpnPrivateFiles
    {
        internal static string Root { get { return Path.Combine(AppPaths.Root, "home-vpn-private"); } }
        internal static void SecureDirectory(string path)
        {
            Directory.CreateDirectory(path);
            var security = new DirectorySecurity();
            var user = WindowsIdentity.GetCurrent().User;
            security.SetOwner(user); security.SetAccessRuleProtection(true, false);
            security.AddAccessRule(new FileSystemAccessRule(user, FileSystemRights.FullControl,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
            Directory.SetAccessControl(path, security);
        }
        internal static void Save(string name, string value)
        {
            SecureDirectory(Root);
            var bytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(value), null, DataProtectionScope.CurrentUser);
            var path = Path.Combine(Root, name + ".dat");
            File.WriteAllBytes(path + ".new", bytes);
            if (File.Exists(path)) File.Replace(path + ".new", path, null);
            else File.Move(path + ".new", path);
        }
        internal static string Load(string name)
        {
            var path = Path.Combine(Root, name + ".dat");
            if (!File.Exists(path)) return null;
            return Encoding.UTF8.GetString(ProtectedData.Unprotect(File.ReadAllBytes(path), null, DataProtectionScope.CurrentUser));
        }
    }
}

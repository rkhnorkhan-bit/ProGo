using System;
using System.IO;
using System.Net;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace ProGo
{
    internal static class HomeVpnPortableArchiveTests
    {
        private static int passed;
        private static readonly byte[] Password = Encoding.UTF8.GetBytes("Synthetic independent portable fixture password");
        private static readonly JavaScriptSerializer Json = new JavaScriptSerializer();
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);
        private static string work, token;
        private static void Check(bool value, string message) { if (!value) throw new Exception(message); passed++; Console.WriteLine("PASS: " + message); }
        private static void Reject(Action action, string message)
        {
            bool refused = false;
            try { action(); } catch (HomeVpnPortableException ex) { refused = !ex.Message.Contains(token) && !ex.Message.Contains("fixture-password-secret"); }
            Check(refused, message);
        }
        [STAThread]
        private static int Main(string[] args)
        {
            using (var guard = new System.Threading.Timer(delegate { Console.WriteLine("FAIL: portable fixture deadline"); Environment.Exit(1); }, null, 600000, Timeout.Infinite))
            try {
                if (args.Length == 3 && args[0] == "--portable-child") {
                    byte[] password = Utf8.GetBytes(Console.ReadLine());
                    try { using (var prepared = HomeVpnPortableArchive.Prepare(args[1], password, CancellationToken.None)) HomeVpnPortableArchive.Import(prepared, args[2], CancellationToken.None); }
                    finally { Array.Clear(password, 0, password.Length); }
                    Console.WriteLine("PASS: child imported protected data"); return 0;
                }
                token = File.ReadAllText(args[0]); work = Path.GetFullPath(args[1]); Directory.CreateDirectory(work);
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException); Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
                RoundTrip(); MalformedSource(); ResourceAndSnapshotGuards(); AuthenticatedInvalidPayload(); NativeWorkflow(); ContextWorkflow();
                Console.WriteLine("NOT_CHECKED: portable import under another Windows account or on another PC");
                Console.WriteLine("Portable Home VPN tests PASS: " + passed); return 0;
            } catch (Exception ex) { Console.WriteLine("FAIL: " + ex.GetType().Name + ": " + ex.Message); return 1; }
        }
        private static string Fresh(string name) { var root = Path.Combine(work, name + "-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root); return root; }
        private static void Protect(string root, string name, string value)
        { File.WriteAllBytes(Path.Combine(root, name), ProtectedData.Protect(Utf8.GetBytes(value), null, DataProtectionScope.CurrentUser)); }
        private static string Unprotect(string path)
        { return Utf8.GetString(ProtectedData.Unprotect(File.ReadAllBytes(path), null, DataProtectionScope.CurrentUser)); }
        private static string Source(string name)
        {
            var root = Fresh(name); HomeVpnPrivateFiles.SecureDirectory(root);
            Protect(root, "access.dat", token);
            Protect(root, "owner.dat", Json.Serialize(new HomeVpnOwner { Host = "vpn.example.org", Port = 22, Login = "root", KeyFile = Path.Combine(work, "missing-owner-key") }));
            Protect(root, "home-address.dat", "home.example.org"); Protect(root, "share-" + new string('a', 32) + ".dat", "https://profiles.example.org");
            Protect(root, "setup-request.dat", Json.Serialize(new { Version = 1, RequestId = new string('d', 32), Host = "vpn.example.org", Port = 22, Login = "root", Name = "My iPhone", Result = token }));
            Protect(root, "admin-request.dat", Json.Serialize(new { Version = 1, RequestId = new string('e', 32), Action = "invite", Host = "vpn.example.org", Port = 22,
                Login = "root", Name = "Friend", SourceInviteId = new string('f', 24), ServerId = new string('a', 32), Result = token }));
            Protect(root, "share-request.dat", Json.Serialize(new { Version = 1, RequestId = new string('f', 32), Action = "share", Host = "vpn.example.org", Port = 22,
                Login = "root", Domain = "profiles.example.org", ServerId = new string('a', 32) }));
            var session = Path.Combine(root, "session-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(session); File.WriteAllText(Path.Combine(session, "key"), "excluded synthetic plaintext marker");
            return root;
        }
        private static Dictionary<string, string> Plain(string root)
        { return Directory.GetFiles(root, "*.dat").ToDictionary(p => Path.GetFileName(p), p => Unprotect(p), StringComparer.Ordinal); }
        private static string Archive(string source, string name)
        { string path = Path.Combine(work, name + "-" + Guid.NewGuid().ToString("N") + ".progo-vpn"); HomeVpnPortableArchive.Export(source, path, Password, CancellationToken.None); return path; }
        private static void RoundTrip()
        {
            string source = Source("source"), archive = null, target = Path.Combine(work, "clean-target-" + Guid.NewGuid().ToString("N"));
            var original = Plain(source); var encrypted = Directory.GetFiles(source, "*.dat").ToDictionary(p => Path.GetFileName(p), File.ReadAllBytes);
            try {
                var observed = new List<string>(); archive = Path.Combine(work, "round-trip-" + Guid.NewGuid().ToString("N") + ".progo-vpn");
                var preview = HomeVpnPortableArchive.Export(source, archive, Password, CancellationToken.None, (path, ct) => observed.Add(path));
                Check(preview.Files.Length == 7 && preview.Pending.Length == 3 && observed.All(p => Path.GetDirectoryName(p) == source), "export contains all declared DPAPI access and three mutation fences, excluding sessions and external SSH paths");
                Check(encrypted.All(p => p.Value.SequenceEqual(File.ReadAllBytes(Path.Combine(source, p.Key)))), "export preserves every original encrypted private file, including stale owner-key metadata");
                byte[] archiveBytes = File.ReadAllBytes(archive);
                Check(!Encoding.UTF8.GetString(archiveBytes).Contains(token) && !Encoding.UTF8.GetString(archiveBytes).Contains("fixture password") && !Encoding.UTF8.GetString(archiveBytes).Contains("missing-owner-key"), "portable file contains neither plaintext access nor password or stale key path");
                var decoded = Decode(archiveBytes);
                Check(decoded.Count == 7 && decoded["access.dat"] == token && Json.Deserialize<HomeVpnOwner>(decoded["owner.dat"]).KeyFile == "", "independent decoder verifies envelope, payload and cleared local owner SSH mapping");
                Check(new[] { "setup-request.dat", "admin-request.dat", "share-request.dat" }.All(n => decoded[n] == original[n]), "independent decoding preserves the exact pending requests, results and frozen bindings");
                Directory.Delete(source, true);
                using (var prepared = HomeVpnPortableArchive.Prepare(archive, Password, CancellationToken.None)) {
                    Check(prepared.Preview.Files.SequenceEqual(preview.Files) && !Directory.Exists(target), "preview authenticates declared composition without creating destination files");
                    HomeVpnPortableArchive.Import(prepared, target, CancellationToken.None);
                }
                var restored = Plain(target);
                Check(restored.Count == 7 && restored.All(p => p.Key == "owner.dat" ? Json.Deserialize<HomeVpnOwner>(p.Value).KeyFile == "" : p.Value == original[p.Key]), "clean import restores every declared value after the source root is gone");
                Check(Directory.GetAccessControl(target).AreAccessRulesProtected && encrypted.All(p => !p.Value.SequenceEqual(File.ReadAllBytes(Path.Combine(target, p.Key)))), "clean import creates new CurrentUser DPAPI envelopes under a protected destination ACL");
                var before = Directory.GetFiles(target).ToDictionary(Path.GetFileName, File.ReadAllBytes);
                using (var prepared = HomeVpnPortableArchive.Prepare(archive, Password, CancellationToken.None)) Reject(() => HomeVpnPortableArchive.Import(prepared, target, CancellationToken.None), "existing private root is refused without overwriting access or request fences");
                Check(before.All(p => p.Value.SequenceEqual(File.ReadAllBytes(Path.Combine(target, p.Key)))), "refused replacement preserves every current destination byte");
                string childTarget = Path.Combine(work, "child-target-" + Guid.NewGuid().ToString("N"));
                try {
                    var start = new ProcessStartInfo(Assembly.GetExecutingAssembly().Location, "--portable-child " + Quote(archive) + " " + Quote(childTarget)) {
                        UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true };
                    using (var child = Process.Start(start)) {
                        child.StandardInput.WriteLine(Utf8.GetString(Password)); child.StandardInput.Close();
                        if (!child.WaitForExit(60000)) { child.Kill(); throw new Exception("Portable child deadline"); }
                        string output = child.StandardOutput.ReadToEnd(); Check(child.ExitCode == 0 && output.Contains("PASS: child imported") && !output.Contains(token), "a separate process imports the portable file with a password passed only over stdin");
                    }
                    Check(Plain(childTarget).All(p => p.Value == restored[p.Key]), "separate-process import agrees with the clean DPAPI destination");
                } finally { if (Directory.Exists(childTarget)) Directory.Delete(childTarget, true); }
                TestInstalledReload(archive, original);
                foreach (int position in new[] { 0, 17, 21, 25, 29, 61, archiveBytes.Length - 33, archiveBytes.Length - 1 }) {
                    var changed = (byte[])archiveBytes.Clone(); changed[position] ^= 1; string path = Path.Combine(work, "tampered.progo-vpn"); File.WriteAllBytes(path, changed);
                    Reject(() => { using (HomeVpnPortableArchive.Prepare(path, Password, CancellationToken.None)) { } }, "tampering is refused before any import at byte " + position); File.Delete(path);
                }
                foreach (var invalid in new[] { archiveBytes.Take(archiveBytes.Length - 1).ToArray(), archiveBytes.Concat(new byte[] { 1 }).ToArray() }) {
                    string path = Path.Combine(work, "wrong-length.progo-vpn"); File.WriteAllBytes(path, invalid);
                    Reject(() => { using (HomeVpnPortableArchive.Prepare(path, Password, CancellationToken.None)) { } }, "truncated or trailing archive data is refused"); File.Delete(path);
                }
                var wrong = Utf8.GetBytes("fixture-password-secret-unrelated");
                try { Reject(() => { using (HomeVpnPortableArchive.Prepare(archive, wrong, CancellationToken.None)) { } }, "wrong password produces a fixed non-secret failure without destination writes"); } finally { Array.Clear(wrong, 0, wrong.Length); }
            } finally { if (Directory.Exists(source)) Directory.Delete(source, true); if (Directory.Exists(target)) Directory.Delete(target, true); if (archive != null) File.Delete(archive); }
        }
        private static void TestInstalledReload(string archive, Dictionary<string, string> expected)
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") { Console.WriteLine("NOT_CHECKED: actual application reload requires isolated CI"); return; }
            AppPaths.EnsureDirectories(); string root = HomeVpnPrivateFiles.Root, previous = root + ".portable-original-" + Guid.NewGuid().ToString("N"); bool moved = Directory.Exists(root);
            var unrelated = new[] { AppPaths.SettingsPath, AppPaths.VaultPath, SystemProxyService.BackupPath, CliProxyEnvironmentService.BackupPath }.ToDictionary(p => p, p => File.Exists(p) ? File.ReadAllBytes(p) : null);
            if (moved) Directory.Move(root, previous);
            try {
                using (var prepared = HomeVpnPortableArchive.Prepare(archive, Password, CancellationToken.None)) HomeVpnPortableArchive.Import(prepared, root, CancellationToken.None);
                using (var relay = new Ikev2RelayService()) using (var service = new HomeVpnService(relay)) {
                    Check(service.Access != null && service.Access.ServerId == new string('a', 32) && service.Owner != null && service.Owner.KeyFile == "" && service.HomeAddress == "home.example.org" &&
                        service.ShareOrigin == "https://profiles.example.org" && !relay.IsRunning && service.RecoveryStatus == "Не запущен", "a new real HomeVpnService loads clean imported data without activating SSH or relay");
                }
                Check(HomeVpnSetupRecovery.HasPending() && HomeVpnSetupRecovery.Load(new HomeVpnOwner { Host = "vpn.example.org", Port = 22, Login = "root" }, "My iPhone").RequestId == new string('d', 32) &&
                    HomeVpnAdminRecovery.HasPending() && HomeVpnAdminRecovery.Pending().RequestId == new string('e', 32), "actual setup and invite recovery retain the imported IDs and pending gates");
                bool blocked = false; try { HomeVpnAdminRecovery.Register(new HomeVpnOwner { Host = "vpn.example.org", Port = 22, Login = "root" }, "New", null); } catch (HomeVpnAdminPendingException) { blocked = true; }
                var share = HomeVpnShareRecovery.Load(new HomeVpnOwner { Host = "vpn.example.org", Port = 22, Login = "root" }); bool shareBlocked = false;
                try { HomeVpnShareRecovery.Register(new HomeVpnOwner { Host = "vpn.example.org", Port = 22, Login = "root" }, "other.example.org"); } catch (HomeVpnSharePendingException) { shareBlocked = true; }
                Check(blocked && shareBlocked && HomeVpnShareRecovery.HasPending() && share.RequestId == new string('f', 32) && share.Domain == "profiles.example.org" && share.ServerId == new string('a', 32) &&
                    Unprotect(Path.Combine(root, "share-request.dat")) == expected["share-request.dat"], "actual invite and share recovery gates retain original IDs and frozen bindings instead of admitting new commands");
                Check(unrelated.All(p => p.Value == null ? !File.Exists(p.Key) : p.Value.SequenceEqual(File.ReadAllBytes(p.Key))), "actual clean import leaves settings, vault and live proxy ownership journals unchanged");
            } finally { if (Directory.Exists(root)) Directory.Delete(root, true); if (moved) Directory.Move(previous, root); }
        }
        private static void MalformedSource()
        {
            var root = Source("malformed"); var owner = File.ReadAllBytes(Path.Combine(root, "owner.dat")); var share = File.ReadAllBytes(Path.Combine(root, "share-request.dat")); var access = File.ReadAllBytes(Path.Combine(root, "access.dat"));
            string output = Path.Combine(work, "must-not-exist.progo-vpn");
            try {
                Protect(root, "future-request.dat", "{}"); Reject(() => HomeVpnPortableArchive.Export(root, output, Password, CancellationToken.None), "unknown persistent request fails closed instead of losing a future fence"); File.Delete(Path.Combine(root, "future-request.dat"));
                Protect(root, "owner.dat", "{\"Host\":\"vpn.example.org\",\"Host\":\"vpn.example.org\",\"Port\":22,\"Login\":\"root\",\"KeyFile\":\"\"}");
                Reject(() => HomeVpnPortableArchive.Export(root, output, Password, CancellationToken.None), "duplicate private schema keys cannot be silently overwritten"); File.WriteAllBytes(Path.Combine(root, "owner.dat"), owner);
                string encoded = token.Substring(7).Replace('-', '+').Replace('_', '/'); string tokenJson = Utf8.GetString(Convert.FromBase64String(encoded.PadRight((encoded.Length + 3) / 4 * 4, '=')));
                string duplicate = "{\"Version\":1," + tokenJson.Substring(1);
                Protect(root, "access.dat", "PROGO1." + Convert.ToBase64String(Utf8.GetBytes(duplicate)).TrimEnd('=').Replace('+', '-').Replace('/', '_'));
                Reject(() => HomeVpnPortableArchive.Export(root, output, Password, CancellationToken.None), "embedded VPN token duplicate fields cannot bypass portable schema validation"); File.WriteAllBytes(Path.Combine(root, "access.dat"), access);
                Protect(root, "share-request.dat", Json.Serialize(new { Version = 1, RequestId = new string('f', 32), Action = "share", Host = "vpn.example.org", Port = 22, Login = "root", Domain = "profiles.example.org", ServerId = new string('a', 32), Result = token }));
                Reject(() => HomeVpnPortableArchive.Export(root, output, Password, CancellationToken.None), "share request refuses unrecognized result or secret-bearing fields"); File.WriteAllBytes(Path.Combine(root, "share-request.dat"), share);
                Protect(root, "share-request.dat", Json.Serialize(new { Version = 1, RequestId = new string('f', 32), Action = "share", Host = "vpn.example.org", Port = 22, Login = "root", Domain = "bad..example.org", ServerId = new string('a', 32) }));
                Reject(() => HomeVpnPortableArchive.Export(root, output, Password, CancellationToken.None), "invalid frozen HTTPS domain refuses export without rewriting the share request"); File.WriteAllBytes(Path.Combine(root, "share-request.dat"), share);
                File.WriteAllText(Path.Combine(root, "setup-request.dat.new"), "synthetic unfinished write");
                Reject(() => HomeVpnPortableArchive.Export(root, output, Password, CancellationToken.None), "unfinished private write is not accepted as a complete snapshot"); File.Delete(Path.Combine(root, "setup-request.dat.new"));
                using (var locked = File.Open(Path.Combine(root, "access.dat"), FileMode.Open, FileAccess.Read, FileShare.None)) Reject(() => HomeVpnPortableArchive.Export(root, output, Password, CancellationToken.None), "real locked DPAPI source refuses export without creating a partial destination");
                Check(!File.Exists(output) && !Directory.GetFiles(work, "must-not-exist.progo-vpn.*.tmp").Any(), "all source failures leave no advertised export or encrypted temporary file");
                byte[] shortPassword = Utf8.GetBytes("1234");
                try { Reject(() => HomeVpnPortableArchive.Export(root, output, shortPassword, CancellationToken.None), "vault-sized PIN cannot become the portable export password"); } finally { Array.Clear(shortPassword, 0, shortPassword.Length); }
            } finally { Directory.Delete(root, true); File.Delete(output); }
        }
        private static void ResourceAndSnapshotGuards()
        {
            byte[] sixteenScalars = Utf8.GetBytes(String.Concat(Enumerable.Repeat("\U0001F512", 16))), eightScalars = Utf8.GetBytes(String.Concat(Enumerable.Repeat("\U0001F512", 8))), oversized = Utf8.GetBytes(new string('x', 1025));
            try {
                HomeVpnPortableArchive.ValidatePassword(sixteenScalars); Check(true, "password minimum counts Unicode scalars rather than UTF-16 code units");
                Reject(() => HomeVpnPortableArchive.ValidatePassword(eightScalars), "eight surrogate pairs cannot satisfy the sixteen-character password minimum");
                Reject(() => HomeVpnPortableArchive.ValidatePassword(oversized), "portable password input has a bounded UTF-8 byte size");
                Reject(() => HomeVpnPortableArchive.ValidatePassword(new byte[] { 0xf0, 0x80, 0x80, 0x80 }), "invalid UTF-8 passwords are rejected without normalization");
            } finally { Array.Clear(sixteenScalars, 0, sixteenScalars.Length); Array.Clear(eightScalars, 0, eightScalars.Length); Array.Clear(oversized, 0, oversized.Length); }
            string root = Source("snapshot-guards"), output = Path.Combine(work, "changing-source.progo-vpn"), first = null;
            try {
                bool changed = false;
                Reject(() => HomeVpnPortableArchive.Export(root, output, Password, CancellationToken.None, (path, ct) => {
                    if (first == null) first = path;
                    else if (!changed) { Protect(root, Path.GetFileName(first), Unprotect(first)); changed = true; }
                }), "a real DPAPI replacement during source scanning refuses an inconsistent archive");
                Check(changed && !File.Exists(output), "source mutation does not publish a partial portable snapshot or retry indefinitely");
                string junction = Path.Combine(work, "portable-junction-" + Guid.NewGuid().ToString("N"));
                try {
                    using (var process = Process.Start(new ProcessStartInfo("cmd.exe", "/d /c mklink /J " + Quote(junction) + " " + Quote(root)) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true })) {
                        if (!process.WaitForExit(10000)) { process.Kill(); throw new Exception("Portable junction fixture deadline"); }
                        Check(process.ExitCode == 0 && (File.GetAttributes(junction) & FileAttributes.ReparsePoint) != 0, "native fixture creates an actual NTFS junction");
                    }
                    Reject(() => HomeVpnPortableArchive.Export(junction, output, Password, CancellationToken.None), "source junction cannot redirect private export into another directory");
                    string archive = Archive(root, "junction-parent");
                    try { using (var prepared = HomeVpnPortableArchive.Prepare(archive, Password, CancellationToken.None)) Reject(() => HomeVpnPortableArchive.Import(prepared, Path.Combine(junction, "must-not-create"), CancellationToken.None), "import refuses a junction ancestor before creating a protected staging root"); }
                    finally { File.Delete(archive); }
                    Check(!Directory.Exists(Path.Combine(root, "must-not-create")), "rejected junction import leaves the actual source directory unchanged");
                } finally { if (Directory.Exists(junction)) Directory.Delete(junction); }
                string excessive = Path.Combine(work, "excessive-header.progo-vpn");
                using (var file = File.Create(excessive)) using (var writer = new BinaryWriter(file)) {
                    writer.Write(Encoding.ASCII.GetBytes("PROGO-VPN-EXPORT\0")); writer.Write(1); writer.Write(600000); writer.Write(Int32.MaxValue);
                }
                try { Reject(() => { using (HomeVpnPortableArchive.Prepare(excessive, Password, CancellationToken.None)) { } }, "untrusted ciphertext length is bounded before allocation or password derivation"); }
                finally { File.Delete(excessive); }
            } finally { Directory.Delete(root, true); File.Delete(output); }
        }
        private static Dictionary<string, string> Decode(byte[] file)
        {
            using (var input = new MemoryStream(file)) using (var reader = new BinaryReader(input)) {
                byte[] magic = reader.ReadBytes(17); Check(Encoding.ASCII.GetString(magic) == "PROGO-VPN-EXPORT\0" && reader.ReadInt32() == 1 && reader.ReadInt32() == 600000, "independent decoder recognizes fixed version and KDF parameters");
                int length = reader.ReadInt32(); byte[] salt = reader.ReadBytes(32), iv = reader.ReadBytes(16), cipher = reader.ReadBytes(length), tag = reader.ReadBytes(32);
                byte[] key; using (var kdf = new Rfc2898DeriveBytes(Password, salt, 600000, HashAlgorithmName.SHA256)) key = kdf.GetBytes(64);
                try {
                    using (var mac = new HMACSHA256(key.Skip(32).ToArray())) Check(mac.ComputeHash(file.Take(file.Length - 32).ToArray()).SequenceEqual(tag), "independent decoder authenticates the complete canonical header and ciphertext");
                    using (var aes = Aes.Create()) {
                        aes.Key = key.Take(32).ToArray(); aes.IV = iv;
                        byte[] plain; using (var decrypt = aes.CreateDecryptor()) plain = decrypt.TransformFinalBlock(cipher, 0, cipher.Length);
                        try { using (var payload = new BinaryReader(new MemoryStream(plain))) {
                            Check(payload.ReadInt32() == 1, "independent decoder recognizes typed private payload"); int count = payload.ReadInt32(); var entries = new Dictionary<string, string>(StringComparer.Ordinal);
                            for (int i = 0; i < count; i++) { string name = Encoding.ASCII.GetString(payload.ReadBytes(payload.ReadInt32())); entries.Add(name, Utf8.GetString(payload.ReadBytes(payload.ReadInt32()))); }
                            Check(payload.BaseStream.Position == payload.BaseStream.Length, "independent decoder observes no undeclared trailing payload"); return entries;
                        } } finally { Array.Clear(plain, 0, plain.Length); }
                    }
                } finally { Array.Clear(key, 0, key.Length); }
            }
        }
        private static void AuthenticatedInvalidPayload()
        {
            foreach (var names in new[] { new[] { "home-address.dat", "home-address.dat" }, new[] { "../access.dat" }, new[] { "future-request.dat" } }) {
                string path = Path.Combine(work, "invalid-payload.progo-vpn"); byte[] payload;
                using (var output = new MemoryStream()) { using (var writer = new BinaryWriter(output, Encoding.ASCII, true)) {
                    writer.Write(1); writer.Write(names.Length); foreach (string name in names) { var encoded = Encoding.ASCII.GetBytes(name); var value = Utf8.GetBytes("home.example.org"); writer.Write(encoded.Length); writer.Write(encoded); writer.Write(value.Length); writer.Write(value); }
                } payload = output.ToArray(); }
                File.WriteAllBytes(path, Encode(payload)); Reject(() => { using (HomeVpnPortableArchive.Prepare(path, Password, CancellationToken.None)) { } }, "authenticated invalid paths, duplicates or unknown payload records remain inadmissible");
                File.Delete(path); Array.Clear(payload, 0, payload.Length);
            }
        }
        private static byte[] Encode(byte[] payload)
        {
            byte[] salt = new byte[32], iv = new byte[16]; using (var random = RandomNumberGenerator.Create()) { random.GetBytes(salt); random.GetBytes(iv); }
            byte[] key; using (var kdf = new Rfc2898DeriveBytes(Password, salt, 600000, HashAlgorithmName.SHA256)) key = kdf.GetBytes(64);
            try {
                byte[] cipher; using (var aes = Aes.Create()) { aes.Key = key.Take(32).ToArray(); aes.IV = iv; using (var encrypt = aes.CreateEncryptor()) cipher = encrypt.TransformFinalBlock(payload, 0, payload.Length); }
                using (var output = new MemoryStream()) { using (var writer = new BinaryWriter(output, Encoding.ASCII, true)) {
                    writer.Write(Encoding.ASCII.GetBytes("PROGO-VPN-EXPORT\0")); writer.Write(1); writer.Write(600000); writer.Write(cipher.Length); writer.Write(salt); writer.Write(iv); writer.Write(cipher);
                    using (var mac = new HMACSHA256(key.Skip(32).ToArray())) writer.Write(mac.ComputeHash(output.ToArray()));
                } return output.ToArray(); }
            } finally { Array.Clear(key, 0, key.Length); }
        }
        private static object Field(object target, string name) { return target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target); }
        private static void Pump(Func<bool> ready)
        { var watch = Stopwatch.StartNew(); while (!ready()) { Application.DoEvents(); Thread.Sleep(10); if (watch.ElapsedMilliseconds > 60000) throw new Exception("Portable native fixture deadline"); } Application.DoEvents(); }
        private static void NativeWorkflow()
        {
            var source = Source("native-source"); string cancelled = Path.Combine(work, "cancelled-export.progo-vpn"), archive = null;
            try {
                using (var entered = new ManualResetEventSlim()) using (var release = new ManualResetEventSlim())
                using (var form = new HomeVpnPortableForm(true, source, null, exporting => cancelled, (path, ct) => { entered.Set(); release.Wait(); }))
                using (var pulse = new System.Windows.Forms.Timer { Interval = 10 }) {
                    int pulses = 0; pulse.Tick += delegate { pulses++; }; pulse.Start(); form.Show(); Application.DoEvents();
                    ((TextBox)Field(form, "password")).Text = ((TextBox)Field(form, "repeat")).Text = Utf8.GetString(Password);
                    ((Button)Field(form, "prepare")).PerformClick(); Pump(() => entered.IsSet && pulses >= 3);
                    Check(form.IsBusy && !form.Work.IsCompleted && !((Button)Field(form, "prepare")).Enabled, "actual export worker stays single flight while a source read blocks and native UI heartbeats continue");
                    Reject(() => HomeVpnPortableArchive.Export(source, cancelled, Password, CancellationToken.None), "second portable request cannot create a competing source scan or KDF worker");
                    form.Close(); Application.DoEvents(); Check(form.Visible && form.IsBusy && !form.Work.IsCompleted, "close requests cancellation while retaining ownership of an unfinished disk worker");
                    release.Set(); Pump(() => form.Work.IsCompleted); Check(!form.Work.IsFaulted && !File.Exists(cancelled) && !Directory.GetFiles(work, "cancelled-export.progo-vpn.*.tmp").Any(), "cancelled native export settles the actual worker without publishing any file"); pulse.Stop();
                }
                using (var entered = new ManualResetEventSlim()) using (var release = new ManualResetEventSlim()) {
                    var form = new HomeVpnPortableForm(true, source, null, exporting => cancelled, (path, ct) => { entered.Set(); release.Wait(); }); int applied = 0;
                    form.ResultApplied += () => applied++; form.Show(); Application.DoEvents();
                    ((TextBox)Field(form, "password")).Text = ((TextBox)Field(form, "repeat")).Text = Utf8.GetString(Password);
                    ((Button)Field(form, "prepare")).PerformClick(); Pump(() => entered.IsSet); Task workTask = form.Work;
                    form.Dispose(); Check(!workTask.IsCompleted, "disposing the native owner cancels but never pretends an unfinished disk worker has stopped");
                    release.Set(); Check(workTask.Wait(10000) && !workTask.IsFaulted && applied == 0 && !File.Exists(cancelled), "disposed owner settles cancellation without a UI message pump, late callbacks or a published export");
                }
                using (var entered = new ManualResetEventSlim()) using (var release = new ManualResetEventSlim())
                using (var form = new HomeVpnPortableForm(true, source, null, exporting => cancelled, (path, ct) => { entered.Set(); release.Wait(); }, 1000)) {
                    form.Show(); Application.DoEvents(); ((TextBox)Field(form, "password")).Text = ((TextBox)Field(form, "repeat")).Text = Utf8.GetString(Password);
                    ((Button)Field(form, "prepare")).PerformClick(); Pump(() => entered.IsSet); var elapsed = Stopwatch.StartNew();
                    Pump(() => elapsed.ElapsedMilliseconds >= 1250);
                    Check(form.IsBusy && !form.Work.IsCompleted && !File.Exists(cancelled), "finite deadline requests cancellation while a held filesystem call remains honestly owned");
                    release.Set(); Pump(() => form.Work.IsCompleted);
                    Check(!form.Work.IsFaulted && !File.Exists(cancelled) && ((Label)Field(form, "status")).Text.Contains("Время переноса VPN истекло"), "deadline cancellation settles before publication with a fixed Russian timeout result and no automatic retry");
                    form.Close();
                }
                archive = Archive(source, "native-archive"); string target = Path.Combine(work, "native-clean-" + Guid.NewGuid().ToString("N")); int imported = 0;
                using (var form = new HomeVpnPortableForm(false, target, () => imported++, exporting => archive)) {
                    form.Show(); Application.DoEvents(); ((TextBox)Field(form, "password")).Text = Utf8.GetString(Password);
                    ((Button)Field(form, "prepare")).PerformClick(); Pump(() => form.Work.IsCompleted);
                    var text = ((TextBox)Field(form, "preview")).Text;
                    Check(!form.Work.IsFaulted && !Directory.Exists(target) && imported == 0 && ((Button)Field(form, "accept")).Enabled &&
                        text.Contains("share-request.dat") && text.Contains("vpn.example.org") && !text.Contains(token) && !text.Contains(Utf8.GetString(Password)), "native preview shows declared composition and pending state without importing or exposing secrets");
                    using (var bitmap = new Bitmap(form.Width, form.Height)) { form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size)); bitmap.Save(Path.Combine(work, "portable-home-vpn-preview.png")); }
                    ((Button)Field(form, "accept")).PerformClick(); Pump(() => form.Work.IsCompleted);
                    Check(!form.Work.IsFaulted && imported == 1 && Directory.Exists(target) && Plain(target)["access.dat"] == token, "only the explicit native import action publishes one verified clean DPAPI root");
                    form.Close();
                }
                Directory.Delete(target, true);
                using (var entered = new ManualResetEventSlim()) using (var release = new ManualResetEventSlim()) using (var cancellation = new CancellationTokenSource()) {
                    using (var prepared = HomeVpnPortableArchive.Prepare(archive, Password, CancellationToken.None)) {
                        var task = Task.Run(() => HomeVpnPortableArchive.Import(prepared, target, cancellation.Token, (path, ct) => { entered.Set(); release.Wait(); }), cancellation.Token);
                        Pump(() => entered.IsSet); cancellation.Cancel(); Check(!task.IsCompleted && !Directory.Exists(target), "cancelled staged import cannot advertise completion while an OS write is still owned");
                        release.Set(); Pump(() => task.IsCompleted); Check(task.IsCanceled && !Directory.Exists(target) && !Directory.GetDirectories(work, Path.GetFileName(target) + ".import-*").Any(), "precommit import cancellation removes only its new protected staging directory");
                    }
                }
                using (var committed = new ManualResetEventSlim()) using (var cancellation = new CancellationTokenSource())
                using (var watcher = new FileSystemWatcher(work) { NotifyFilter = NotifyFilters.DirectoryName })
                using (var prepared = HomeVpnPortableArchive.Prepare(archive, Password, CancellationToken.None)) {
                    var eventGate = new object(); bool listening = true;
                    RenamedEventHandler renamed = delegate(object sender, RenamedEventArgs args) {
                        lock (eventGate) if (listening && args.FullPath == target) { cancellation.Cancel(); committed.Set(); }
                    };
                    FileSystemEventHandler created = delegate(object sender, FileSystemEventArgs args) {
                        lock (eventGate) if (listening && args.FullPath == target) { cancellation.Cancel(); committed.Set(); }
                    };
                    try {
                        watcher.Renamed += renamed; watcher.Created += created; watcher.EnableRaisingEvents = true;
                        var task = Task.Run(() => HomeVpnPortableArchive.Import(prepared, target, cancellation.Token), cancellation.Token);
                        Pump(() => task.IsCompleted && committed.IsSet);
                        Check(task.Status == TaskStatus.RanToCompletion && cancellation.IsCancellationRequested && Plain(target)["access.dat"] == token,
                            "cancellation triggered by the actual published directory cannot undo an accepted complete import");
                    } finally {
                        lock (eventGate) listening = false;
                        watcher.EnableRaisingEvents = false; watcher.Renamed -= renamed; watcher.Created -= created;
                    }
                }
                Directory.Delete(target, true);
            } finally { Directory.Delete(source, true); File.Delete(cancelled); if (archive != null) File.Delete(archive); }
        }
        private static string Quote(string value) { return "\"" + value.Replace("\"", "\\\"") + "\""; }
        private static IEnumerable<ToolStripItem> MenuItems(ToolStripItemCollection items)
        {
            foreach (ToolStripItem item in items) {
                yield return item; var dropdown = item as ToolStripDropDownItem;
                if (dropdown != null) foreach (var child in MenuItems(dropdown.DropDownItems)) yield return child;
            }
        }
        private static void ContextWorkflow()
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") { Console.WriteLine("NOT_CHECKED: tray owner fixture requires isolated CI"); return; }
            string source = Source("tray-portable"), output = Path.Combine(work, "tray-portable-cancel.progo-vpn");
            using (var settings = new SettingsService())
            using (var proxy = new ProxyService(() => settings.Current, s => settings.Save(s), "unused-portable-test-ssh", () => DateTime.UtcNow, false))
            using (var bridge = new CliProxyBridgeService(settings))
            using (var relay = new Ikev2RelayService())
            using (var home = new HomeVpnService(relay))
            using (var clipboard = new ClipboardService(settings))
            using (var monitor = new ConnectionHealthMonitor(() => settings.Current))
            using (var entered = new ManualResetEventSlim()) using (var release = new ManualResetEventSlim())
            try {
                int cleanups = 0, calls = 0;
                Func<bool, HomeVpnPortableForm> factory = exporting => new HomeVpnPortableForm(exporting, source, null, selectingExport => output,
                    (path, ct) => { Interlocked.Increment(ref calls); entered.Set(); release.Wait(); });
                Func<Action<CancellationToken>, UpdateAwareTrayApplicationContext> contextFor = probe => new UpdateAwareTrayApplicationContext(settings, proxy, bridge, home, clipboard, false,
                    health: monitor, windowsRestore: () => { Interlocked.Increment(ref cleanups); return new WindowsProxyRestoreResult(); },
                    backupShutdownTimeoutMilliseconds: 100, beforeBaselineProbe: probe, cliRestore: () => { }, portableFormFactory: factory);
                using (var context = contextFor(null)) using (var pulse = new System.Windows.Forms.Timer { Interval = 10 }) {
                    var commands = MenuItems(((NotifyIcon)Field(context, "tray")).ContextMenuStrip.Items).Where(item => item.Tag is AppCommand).ToDictionary(item => (AppCommand)item.Tag);
                    Check(commands[AppCommand.ExportHomeVpn].ToolTipText.Contains("парольная") && commands[AppCommand.ImportHomeVpn].ToolTipText.Contains("чистую"), "actual tray exposes protected export and explicitly described clean import commands");
                    commands[AppCommand.ExportHomeVpn].PerformClick(); var form = (HomeVpnPortableForm)Field(context, "portableForm");
                    ((TextBox)Field(form, "password")).Text = ((TextBox)Field(form, "repeat")).Text = Utf8.GetString(Password);
                    int pulses = 0; pulse.Tick += delegate { pulses++; }; pulse.Start(); ((Button)Field(form, "prepare")).PerformClick(); Pump(() => entered.IsSet && pulses >= 3);
                    Check(context.IsPortableRunning && Object.ReferenceEquals(context.PortableWork, form.Work) && calls == 1 &&
                        new[] { AppCommand.CreateBackup, AppCommand.RestoreBackup, AppCommand.CleanupBackups, AppCommand.Update, AppCommand.ExportHomeVpn, AppCommand.ImportHomeVpn }.All(command => !commands[command].Enabled) &&
                        commands[AppCommand.Settings].Enabled && commands[AppCommand.StopDesktop].Enabled, "real tray retains the actual portable task and disables competing backup or maintenance while independent controls remain usable");
                    var manual = context.CreateManualBackupAsync(); var startup = context.StartStartupBackupAsync();
                    Check(manual.IsCompleted && startup.IsCompleted && !context.IsBackupRunning && context.IsPortableRunning, "direct manual and startup backup entry points cannot bypass the portable worker gate");
                    int before = pulses; var shutdown = context.RequestShutdownAsync(); Pump(() => shutdown.IsCompleted);
                    Check(!shutdown.Result && context.IsPortableRunning && !context.PortableWork.IsCompleted && cleanups == 0 && pulses > before && !(bool)Field(context, "shutdownPrepared"), "bounded shutdown refuses a held portable disk worker without blocking UI or handing off maintenance");
                    release.Set(); Pump(() => context.PortableWork.IsCompleted); Check(!context.PortableWork.IsFaulted && !File.Exists(output), "owner cancellation settles the actual export before publication");
                    shutdown = context.RequestShutdownAsync(); Pump(() => shutdown.IsCompleted); Check(shutdown.Result && cleanups == 1, "shutdown cleanup runs only after the retained portable operation actually settles"); pulse.Stop();
                }
                entered.Reset(); release.Reset(); calls = 0;
                using (var probeEntered = new ManualResetEventSlim()) using (var probeRelease = new ManualResetEventSlim())
                using (var context = contextFor(ct => { probeEntered.Set(); probeRelease.Wait(); })) {
                    var commands = MenuItems(((NotifyIcon)Field(context, "tray")).ContextMenuStrip.Items).Where(item => item.Tag is AppCommand).ToDictionary(item => (AppCommand)item.Tag);
                    commands[AppCommand.ExportHomeVpn].PerformClick(); var form = (HomeVpnPortableForm)Field(context, "portableForm");
                    ((TextBox)Field(form, "password")).Text = ((TextBox)Field(form, "repeat")).Text = Utf8.GetString(Password);
                    var startup = context.StartStartupBackupAsync(); ((Button)Field(form, "prepare")).PerformClick();
                    Check(context.IsBackupRunning && !context.IsPortableRunning && form.Work == null && calls == 0 && ((Label)Field(form, "status")).Text.Contains("резервного"), "an already open portable form cannot bypass a queued startup backup gate");
                    Pump(() => probeEntered.IsSet || startup.IsCompleted); Check(probeEntered.IsSet && !startup.IsCompleted, "native startup probe is actually held before testing the active backup gate");
                    ((Button)Field(form, "prepare")).PerformClick(); Check(!context.IsPortableRunning && form.Work == null && calls == 0, "an already open portable form also refuses to start during the real backup worker");
                    var stop = context.RequestShutdownAsync(); probeRelease.Set(); Pump(() => stop.IsCompleted && startup.IsCompleted); Check(stop.Result && !startup.IsFaulted, "shared owner cancels and waits the actual backup before shutdown without starting portable I/O");
                    form.Close();
                }
                entered.Reset(); release.Reset(); calls = 0;
                var forced = contextFor(null);
                try {
                    var commands = MenuItems(((NotifyIcon)Field(forced, "tray")).ContextMenuStrip.Items).Where(item => item.Tag is AppCommand).ToDictionary(item => (AppCommand)item.Tag);
                    commands[AppCommand.ExportHomeVpn].PerformClick(); var form = (HomeVpnPortableForm)Field(forced, "portableForm");
                    ((TextBox)Field(form, "password")).Text = ((TextBox)Field(form, "repeat")).Text = Utf8.GetString(Password);
                    ((Button)Field(form, "prepare")).PerformClick(); Pump(() => entered.IsSet); Task retained = forced.PortableWork;
                    forced.Dispose(); Check(!retained.IsCompleted && forced.IsPortableRunning, "forced tray disposal cancels the portable owner without losing or completing its held task");
                    release.Set(); Check(retained.Wait(10000) && !retained.IsFaulted && !File.Exists(output), "forced tray disposal settles the worker without a UI pump or late portable publication");
                } finally { release.Set(); forced.Dispose(); }
            } finally { release.Set(); Directory.Delete(source, true); File.Delete(output); }
        }
    }
}

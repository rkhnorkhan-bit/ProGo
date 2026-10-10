using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace ProGo
{
    // Shared by the app and installed PowerShell helpers. Digests detect damage;
    // they are not a publisher signature or permission to execute untrusted code.
    public static class BackupIntegrity
    {
        public const string IndexName = "backup-files.sha256";
        private const string Header = "ProGo backup integrity v1";
        private static readonly string[] Required = { "ProGo.exe", "VERSION",
            "scripts/Start-ProGo.ps1", "scripts/Restore-ProGoBackup.ps1",
            "scripts/Update-ProGo.Core.ps1" };
        private const string HomeArchive = "home-vpn-private";
        private static readonly string[] ProxyJournals = { "system-proxy-backup.json", "proxy-environment-backup.json" };
        // Windows DPAPI envelope header. This recognises encrypted storage, but does
        // not decrypt it or promise that another account/computer can import it.
        private static readonly byte[] DpapiHeader = { 1, 0, 0, 0, 208, 140, 157, 223, 1, 21,
            209, 17, 140, 122, 0, 192, 79, 194, 151, 235 };

        public static void CopyPersonalArchives(string sourceDirectory, string destinationDirectory)
        { CopyPersonalArchives(sourceDirectory, destinationDirectory, CancellationToken.None); }

        public static void CopyPersonalArchives(string sourceDirectory, string destinationDirectory, CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            var source = Root(sourceDirectory); var destination = Root(destinationDirectory);
            foreach (var name in ProxyJournals)
            {
                cancellation.ThrowIfCancellationRequested();
                var path = Path.Combine(source, name); FileAttributes attributes;
                if (!TryAttributes(path, out attributes)) continue;
                RejectReparse(path);
                if ((attributes & FileAttributes.Directory) != 0)
                    throw new InvalidDataException("Вместо снимка прокси обнаружена папка: " + name + ".");
                File.Copy(path, Path.Combine(destination, name), false);
                cancellation.ThrowIfCancellationRequested();
            }
            var home = Path.Combine(source, HomeArchive); FileAttributes homeAttributes;
            if (!TryAttributes(home, out homeAttributes)) return;
            RejectReparse(home);
            if ((homeAttributes & FileAttributes.Directory) == 0)
                throw new InvalidDataException("Папка личных данных VPN недоступна.");
            // Never recurse: live session/admin folders can hold plaintext SSH keys
            // and recovery output. Only the known persistent DPAPI files are copied.
            foreach (var entry in Directory.GetFileSystemEntries(home))
            {
                cancellation.ThrowIfCancellationRequested();
                // Windows opens persistent names case-insensitively. Canonicalise
                // the archive rather than silently omitting ACCESS.DAT after a move.
                var name = Path.GetFileName(entry).ToLowerInvariant();
                if (!IsHomeArchiveFile(name)) continue;
                RejectReparse(entry);
                if ((File.GetAttributes(entry) & FileAttributes.Directory) != 0)
                    throw new InvalidDataException("Вместо защищённого файла VPN обнаружена папка: " + name + ".");
                using (var input = new FileStream(entry, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    CheckProtectedEnvelope(input);
                    var target = Path.Combine(destination, HomeArchive);
                    Directory.CreateDirectory(target); RejectReparse(target);
                    using (var output = new FileStream(Path.Combine(target, name), FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    {
                        input.Position = 0; var buffer = new byte[65536]; int read;
                        while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                        { cancellation.ThrowIfCancellationRequested(); output.Write(buffer, 0, read); }
                        cancellation.ThrowIfCancellationRequested();
                    }
                }
            }
        }

        public static string[] ArchiveNames(string directory)
        {
            var names = new List<string>();
            foreach (var path in Inventory(Root(directory)))
                if (path.StartsWith(HomeArchive + "/", StringComparison.Ordinal) || Array.IndexOf(ProxyJournals, path) >= 0)
                    names.Add(path);
            return names.ToArray();
        }

        public static string[] CompositionLines(string directory)
        {
            var data = new List<string>(); var root = Root(directory);
            foreach (var name in new[] { "settings.json", "vault.enc.json" })
                if (File.Exists(Path.Combine(root, name))) data.Add(name);
            return new[] { "composition_version=2",
                "restore_program=" + String.Join(",", RestoreNames(root, "Program", false)),
                "restore_data=" + String.Join(",", data.ToArray()),
                "archived_only=" + String.Join(",", ArchiveNames(root)),
                "home_vpn_protection=DPAPI-CurrentUser;not-a-portable-export;no-automatic-import",
                "proxy_journals=archived-only;not-reapplied",
                "home_vpn_excluded=session-and-admin-folders;temporary-and-unknown-files" };
        }

        private static bool TryAttributes(string path, out FileAttributes attributes)
        {
            try { attributes = File.GetAttributes(path); return true; }
            catch (FileNotFoundException) { attributes = 0; return false; }
            catch (DirectoryNotFoundException) { attributes = 0; return false; }
        }

        private static bool IsHomeArchiveFile(string name)
        {
            if (name == "access.dat" || name == "owner.dat" || name == "home-address.dat" || name == "setup-request.dat" || name == "admin-request.dat" || name == "share-request.dat") return true;
            if (!name.StartsWith("share-", StringComparison.Ordinal) || !name.EndsWith(".dat", StringComparison.Ordinal) || name.Length != 42) return false;
            for (var i = 6; i < 38; i++)
                if (!((name[i] >= '0' && name[i] <= '9') || (name[i] >= 'a' && name[i] <= 'f'))) return false;
            return true;
        }

        private static void CheckProtectedEnvelope(Stream input)
        {
            foreach (var expected in DpapiHeader)
                if (input.ReadByte() != expected) throw new InvalidDataException(
                    "Файл личных данных VPN не имеет формата DPAPI. Открытые ключи и доступы не включены в копию.");
            if (input.ReadByte() < 0) throw new InvalidDataException("Защищённый файл VPN неполный.");
        }

        public static PreparedBackup Prepare(string directory)
        {
            return Prepare(directory, CancellationToken.None);
        }

        public static PreparedBackup Prepare(string directory, CancellationToken cancellation)
        { return Prepare(directory, cancellation, null, null); }

        internal static PreparedBackup Prepare(string directory, CancellationToken cancellation,
            Action<string, CancellationToken> beforeRead, Action<PreparedBackup> created)
        {
            cancellation.ThrowIfCancellationRequested();
            var root = Root(directory);
            Validate(root, cancellation, beforeRead);
            cancellation.ThrowIfCancellationRequested();
            // Capture the recorded evidence, never generate new digests from live input.
            var index = File.ReadAllBytes(Path.Combine(root, IndexName));
            var manifest = File.ReadAllBytes(Path.Combine(root, "manifest.txt"));
            var files = Inventory(root, cancellation);
            var copy = new PreparedBackup(Path.Combine(Path.GetTempPath(), "ProGo-restore-" + Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(copy.Path);
            try
            {
                if (created != null) created(copy);
                foreach (var relative in files)
                {
                    cancellation.ThrowIfCancellationRequested();
                    if (beforeRead != null) beforeRead("restore-copy:" + relative, cancellation);
                    cancellation.ThrowIfCancellationRequested();
                    var source = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
                    var target = Path.Combine(copy.Path, relative.Replace('/', Path.DirectorySeparatorChar));
                    // Check the complete path again immediately before reading it.
                    var parent = Path.GetDirectoryName(source);
                    while (!String.Equals(parent, root, StringComparison.OrdinalIgnoreCase))
                    { RejectReparse(parent); parent = Path.GetDirectoryName(parent); }
                    RejectReparse(root); RejectReparse(source);
                    Directory.CreateDirectory(Path.GetDirectoryName(target));
                    using (var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (var output = new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    {
                        var buffer = new byte[65536]; int read;
                        while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                        { cancellation.ThrowIfCancellationRequested(); output.Write(buffer, 0, read); }
                    }
                }
                File.WriteAllBytes(Path.Combine(copy.Path, IndexName), index);
                File.WriteAllBytes(Path.Combine(copy.Path, "manifest.txt"), manifest);
                cancellation.ThrowIfCancellationRequested();
                Validate(copy.Path, cancellation, beforeRead);
                cancellation.ThrowIfCancellationRequested();
                return copy;
            }
            catch { copy.Dispose(); throw; }
        }

        public static string[] RestoreNames(string directory, string scope, bool confirmData)
        {
            var root = Root(directory);
            if (scope != "Program" && scope != "Data" && scope != "All")
                throw new InvalidDataException("Неизвестный состав восстановления.");
            var names = new List<string>();
            if (scope != "Data")
            {
                names.Add("ProGo.exe"); names.Add("VERSION"); names.Add("scripts");
                if (File.Exists(Path.Combine(root, "ProGo.ico"))) names.Add("ProGo.ico");
            }
            if (scope != "Program")
            {
                if (!confirmData) throw new InvalidDataException("Замена настроек и хранилища требует отдельного подтверждения.");
                var count = names.Count;
                foreach (var name in new[] { "settings.json", "vault.enc.json" })
                    if (File.Exists(Path.Combine(root, name))) names.Add(name);
                if (count == names.Count) throw new InvalidDataException("В копии нет настроек или хранилища для восстановления.");
            }
            return names.ToArray();
        }

        internal static void DeletePrepared(string directory)
        {
            if (!Directory.Exists(directory)) return;
            var root = Root(directory);
            Inventory(root); // Refuse links before recursive deletion of our own copy.
            Directory.Delete(root, true);
        }

        public static void Write(string directory)
        { Write(directory, CancellationToken.None); }

        public static void Write(string directory, CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            var root = Root(directory);
            var files = Inventory(root, cancellation);
            var text = new StringBuilder(Header + "\n");
            foreach (var path in files) text.Append(Hash(root, path, cancellation)).Append('\t').Append(path).Append('\n');
            cancellation.ThrowIfCancellationRequested();
            var temporary = Path.Combine(root, IndexName + ".tmp");
            try
            {
                File.WriteAllText(temporary, text.ToString(), new UTF8Encoding(false));
                var destination = Path.Combine(root, IndexName);
                if (File.Exists(destination)) File.Replace(temporary, destination, null);
                else File.Move(temporary, destination);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }

        public static void Validate(string directory)
        { Validate(directory, CancellationToken.None); }

        public static void Validate(string directory, CancellationToken cancellation)
        { Validate(directory, cancellation, null); }

        private static void Validate(string directory, CancellationToken cancellation, Action<string, CancellationToken> beforeRead)
        {
            cancellation.ThrowIfCancellationRequested();
            var root = Root(directory);
            var index = Path.Combine(root, IndexName);
            if (!File.Exists(index)) throw new InvalidDataException(
                "В этой копии нет контрольных сумм. Создайте новую копию в обновлённой ProGo. Старая копия сохранена.");
            RejectReparse(index);
            var lines = File.ReadAllLines(index, new UTF8Encoding(false, true));
            if (lines.Length < 2 || lines[0] != Header) throw new InvalidDataException("Неверный формат контрольных сумм копии.");
            var expected = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 1; i < lines.Length; i++)
            {
                cancellation.ThrowIfCancellationRequested();
                var split = lines[i].IndexOf('\t');
                if (split != 64) throw new InvalidDataException("Повреждён список файлов копии.");
                var digest = lines[i].Substring(0, split);
                foreach (var c in digest) if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f')))
                    throw new InvalidDataException("Неверная контрольная сумма копии.");
                var path = lines[i].Substring(split + 1);
                CheckPath(path);
                if (expected.ContainsKey(path)) throw new InvalidDataException("Повторяющийся путь в списке файлов копии.");
                expected.Add(path, digest);
            }
            var actual = Inventory(root, cancellation);
            if (actual.Count != expected.Count) throw new InvalidDataException("Состав копии изменился: файлы добавлены или удалены.");
            foreach (var path in Required) if (!expected.ContainsKey(path))
                throw new InvalidDataException("Копия неполная: отсутствует " + path + ".");
            foreach (var path in actual)
            {
                string digest;
                cancellation.ThrowIfCancellationRequested();
                if (beforeRead != null) beforeRead("restore-hash:" + path, cancellation);
                cancellation.ThrowIfCancellationRequested();
                if (!expected.TryGetValue(path, out digest) || !String.Equals(Hash(root, path, cancellation), digest, StringComparison.Ordinal))
                    throw new InvalidDataException("Файл копии изменён или повреждён: " + path + ".");
            }
            var manifest = Path.Combine(root, "manifest.txt");
            RejectReparse(manifest);
            var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            cancellation.ThrowIfCancellationRequested();
            foreach (var line in File.ReadAllLines(manifest))
            {
                cancellation.ThrowIfCancellationRequested();
                var split = line.IndexOf('=');
                if (split < 1) continue;
                var key = line.Substring(0, split).Trim();
                if (metadata.ContainsKey(key)) throw new InvalidDataException("Неоднозначные данные manifest.txt.");
                metadata.Add(key, line.Substring(split + 1).Trim());
            }
            cancellation.ThrowIfCancellationRequested();
            string product, version;
            if (!metadata.TryGetValue("product", out product) || product != "ProGo" ||
                !metadata.TryGetValue("version", out version) || String.IsNullOrWhiteSpace(version) ||
                version != File.ReadAllText(Path.Combine(root, "VERSION")).Trim())
                throw new InvalidDataException("Название программы или версия копии не совпадает с manifest.txt.");
            cancellation.ThrowIfCancellationRequested();
        }

        public static string Contents(string directory)
        {
            // Metadata must describe what was actually copied, not a fixed wish list.
            var root = Root(directory);
            var names = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var path in Inventory(root)) names.Add(path.Split('/')[0]);
            return String.Join(",", new List<string>(names).ToArray());
        }

        private static string Root(string directory)
        {
            if (String.IsNullOrWhiteSpace(directory)) throw new InvalidDataException("Не выбрана папка копии.");
            var full = Path.GetFullPath(directory);
            var root = full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (root.Length < Path.GetPathRoot(full).Length) root = Path.GetPathRoot(full);
            if (!Directory.Exists(root)) throw new DirectoryNotFoundException("Папка копии не найдена.");
            RejectReparse(root);
            return root;
        }

        private static List<string> Inventory(string root)
        { return Inventory(root, CancellationToken.None); }

        private static List<string> Inventory(string root, CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            var files = new List<string>();
            Walk(root, "", files, cancellation);
            files.Sort(StringComparer.OrdinalIgnoreCase);
            return files;
        }

        private static void Walk(string root, string relative, List<string> files, CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            var directory = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
            RejectReparse(directory);
            foreach (var entry in Directory.GetFileSystemEntries(directory))
            {
                cancellation.ThrowIfCancellationRequested();
                RejectReparse(entry); // Inspect before recursion; never follow junctions.
                var name = Path.GetFileName(entry);
                var path = relative.Length == 0 ? name : relative + "/" + name;
                if (relative.Length == 0 && (name == "manifest.txt" || name == IndexName)) continue;
                CheckPath(path);
                if (Directory.Exists(entry))
                {
                    if (relative.Length == 0 && name != "scripts" && name != HomeArchive)
                        throw new InvalidDataException("Вместо файла копии обнаружена папка: " + name + ".");
                    if (relative == HomeArchive)
                        throw new InvalidDataException("В копии не допускаются вложенные папки личных данных VPN.");
                    Walk(root, path, files, cancellation);
                }
                else
                {
                    if (relative.Length == 0 && name == HomeArchive)
                        throw new InvalidDataException("Вместо папки личных данных VPN обнаружен файл.");
                    if (relative == HomeArchive)
                        using (var input = new FileStream(entry, FileMode.Open, FileAccess.Read, FileShare.Read)) CheckProtectedEnvelope(input);
                    files.Add(path);
                }
            }
        }

        private static void CheckPath(string path)
        {
            if (String.IsNullOrWhiteSpace(path) || path.IndexOf('\\') >= 0 || path.IndexOf(':') >= 0)
                throw new InvalidDataException("Небезопасный путь в копии.");
            foreach (var c in path) if (Char.IsControl(c)) throw new InvalidDataException("Небезопасный путь в копии.");
            foreach (var part in path.Split('/')) if (part.Length == 0 || part == "." || part == "..")
                throw new InvalidDataException("Небезопасный путь в копии.");
            if (path.StartsWith("scripts/", StringComparison.Ordinal) || path == "scripts") return;
            if (path == HomeArchive || (path.StartsWith(HomeArchive + "/", StringComparison.Ordinal) &&
                IsHomeArchiveFile(path.Substring(HomeArchive.Length + 1)))) return;
            if (Array.IndexOf(ProxyJournals, path) >= 0) return;
            foreach (var name in new[] { "ProGo.exe", "ProGo.ico", "VERSION", "vault.enc.json", "settings.json",
                "progo.log", "update.log", "progo-update.log" }) if (path == name) return;
            throw new InvalidDataException("Неизвестный файл в копии: " + path + ".");
        }

        private static string Hash(string root, string relative, CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            var path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
            RejectReparse(path);
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var algorithm = SHA256.Create())
            {
                var buffer = new byte[65536]; int read;
                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                { cancellation.ThrowIfCancellationRequested(); algorithm.TransformBlock(buffer, 0, read, buffer, 0); }
                cancellation.ThrowIfCancellationRequested();
                algorithm.TransformFinalBlock(new byte[0], 0, 0);
                return BitConverter.ToString(algorithm.Hash).Replace("-", "").ToLowerInvariant();
            }
        }

        private static void RejectReparse(string path)
        {
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("Копия содержит ссылку или junction: " + Path.GetFileName(path) + ".");
        }
    }

    public sealed class PreparedBackup : IDisposable
    {
        public string Path { get; private set; }
        internal PreparedBackup(string path) { Path = path; }
        public void Dispose() { BackupIntegrity.DeletePrepared(Path); }
    }

}

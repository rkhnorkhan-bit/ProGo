using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

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
            "scripts/Update-ProGo.Core.ps1", "scripts/Maintenance-ProGo.ps1",
            "scripts/MaintenanceOperation.cs" };

        public static void Write(string directory)
        {
            var root = Root(directory);
            var files = Inventory(root);
            var text = new StringBuilder(Header + "\n");
            foreach (var path in files) text.Append(Hash(root, path)).Append('\t').Append(path).Append('\n');
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
        {
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
            var actual = Inventory(root);
            if (actual.Count != expected.Count) throw new InvalidDataException("Состав копии изменился: файлы добавлены или удалены.");
            foreach (var path in Required) if (!expected.ContainsKey(path))
                throw new InvalidDataException("Копия неполная: отсутствует " + path + ".");
            foreach (var path in actual)
            {
                string digest;
                if (!expected.TryGetValue(path, out digest) || !String.Equals(Hash(root, path), digest, StringComparison.Ordinal))
                    throw new InvalidDataException("Файл копии изменён или повреждён: " + path + ".");
            }
            var manifest = Path.Combine(root, "manifest.txt");
            RejectReparse(manifest);
            var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in File.ReadAllLines(manifest))
            {
                var split = line.IndexOf('=');
                if (split < 1) continue;
                var key = line.Substring(0, split).Trim();
                if (metadata.ContainsKey(key)) throw new InvalidDataException("Неоднозначные данные manifest.txt.");
                metadata.Add(key, line.Substring(split + 1).Trim());
            }
            string product, version;
            if (!metadata.TryGetValue("product", out product) || product != "ProGo" ||
                !metadata.TryGetValue("version", out version) || String.IsNullOrWhiteSpace(version) ||
                version != File.ReadAllText(Path.Combine(root, "VERSION")).Trim())
                throw new InvalidDataException("Название программы или версия копии не совпадает с manifest.txt.");
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
            var root = Path.GetFullPath(directory);
            if (!Directory.Exists(root)) throw new DirectoryNotFoundException("Папка копии не найдена.");
            RejectReparse(root);
            return root;
        }

        private static List<string> Inventory(string root)
        {
            var files = new List<string>();
            Walk(root, "", files);
            files.Sort(StringComparer.OrdinalIgnoreCase);
            return files;
        }

        private static void Walk(string root, string relative, List<string> files)
        {
            var directory = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
            RejectReparse(directory);
            foreach (var entry in Directory.GetFileSystemEntries(directory))
            {
                RejectReparse(entry); // Inspect before recursion; never follow junctions.
                var name = Path.GetFileName(entry);
                var path = relative.Length == 0 ? name : relative + "/" + name;
                if (relative.Length == 0 && (name == "manifest.txt" || name == IndexName)) continue;
                CheckPath(path);
                if (Directory.Exists(entry))
                {
                    if (relative.Length == 0 && name != "scripts")
                        throw new InvalidDataException("Вместо файла копии обнаружена папка: " + name + ".");
                    Walk(root, path, files);
                }
                else files.Add(path);
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
            foreach (var name in new[] { "ProGo.exe", "ProGo.ico", "VERSION", "vault.enc.json", "settings.json",
                "progo.log", "update.log", "progo-update.log" }) if (path == name) return;
            throw new InvalidDataException("Неизвестный файл в копии: " + path + ".");
        }

        private static string Hash(string root, string relative)
        {
            var path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
            RejectReparse(path);
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var algorithm = SHA256.Create())
                return BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }

        private static void RejectReparse(string path)
        {
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("Копия содержит ссылку или junction: " + Path.GetFileName(path) + ".");
        }
    }
}

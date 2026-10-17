using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace ProGo
{
    // Shared with the installed PowerShell helpers. Logging is best effort:
    // a busy/locked log loses this record rather than delaying an operation.
    public static class BoundedLog
    {
        public const int MaxFileBytes = 1024 * 1024;
        public const int MaxRecordChars = 4096;
        private static readonly Encoding Utf8 = new UTF8Encoding(false);

        public static bool TryWrite(string path, string message)
        {
            bool owned = false;
            Mutex gate = null;
            try
            {
                path = Path.GetFullPath(path);
                using (var hash = SHA256.Create())
                {
                    string key = BitConverter.ToString(hash.ComputeHash(Utf8.GetBytes(path.ToUpperInvariant()))).Replace("-", "");
                    gate = new Mutex(false, "Local\\ProGo.Log." + key);
                }
                try { owned = gate.WaitOne(0); }
                catch (AbandonedMutexException) { owned = true; }
                if (!owned) return false;

                message = message ?? String.Empty;
                if (message.Length > MaxRecordChars)
                {
                    int length = MaxRecordChars;
                    if (Char.IsHighSurrogate(message[length - 1])) length--;
                    message = message.Substring(0, length) + " [truncated]";
                }
                byte[] record = Utf8.GetBytes(message + Environment.NewLine);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                RequireRegularFile(path);
                using (var current = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read))
                {
                    if (current.Length + record.Length > MaxFileBytes)
                    {
                        // Copy bounded tails, including migration from an old oversized
                        // active log. Do not truncate current until the archive succeeds.
                        if (File.Exists(path + ".1"))
                        {
                            RequireRegularFile(path + ".1");
                            using (var previous = new FileStream(path + ".1", FileMode.Open, FileAccess.Read, FileShare.Read))
                                WriteArchive(path + ".2", Tail(previous, MaxFileBytes));
                        }
                        WriteArchive(path + ".1", Tail(current, MaxFileBytes));
                        current.SetLength(0);
                    }
                    current.Position = current.Length;
                    current.Write(record, 0, record.Length);
                }
                return true;
            }
            catch { return false; }
            finally
            {
                if (gate != null)
                {
                    if (owned) gate.ReleaseMutex();
                    gate.Dispose();
                }
            }
        }

        private static void RequireRegularFile(string path)
        {
            if (File.Exists(path) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Log links are not writable.");
        }

        private static void WriteArchive(string path, byte[] data)
        {
            RequireRegularFile(path);
            using (var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read))
                file.Write(data, 0, data.Length);
        }

        private static byte[] Tail(FileStream file, int maxBytes)
        {
            int count = (int)Math.Min(file.Length, maxBytes);
            byte[] data = new byte[count];
            file.Position = file.Length - count;
            int read = 0, part;
            while (read < count && (part = file.Read(data, read, count - read)) > 0) read += part;
            if (read != count) Array.Resize(ref data, read);
            return data;
        }

        public static string ReadTail(string path, int maxBytes)
        {
            try
            {
                if (maxBytes <= 0) return String.Empty;
                using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    byte[] data = Tail(file, Math.Min(maxBytes, MaxFileBytes));
                    int start = 0;
                    // A bounded tail may start inside a UTF-8 character.
                    while (start < data.Length && (data[start] & 0xc0) == 0x80) start++;
                    return Utf8.GetString(data, start, data.Length - start);
                }
            }
            catch { return String.Empty; }
        }

        public static string UpdaterLogPath(string root)
        {
            string current = Path.Combine(root, "update.log");
            string legacy = Path.Combine(root, "progo-update.log");
            return !File.Exists(current) && File.Exists(legacy) ? legacy : current;
        }
    }
}

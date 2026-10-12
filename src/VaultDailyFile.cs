using System;
using System.IO;
using System.Security.Cryptography;

namespace ProGo
{
    internal enum VaultDailyFileReadStatus { Success, Missing, FileUnavailable, InvalidData, UnsupportedFormat }

    // A token identifies the bytes observed on disk, independently of later caller edits.
    internal sealed class VaultDailyFileStamp
    {
        private readonly string path;
        private readonly byte[] digest;
        private readonly Guid generation;
        private readonly long revision;
        private readonly DateTime localDay;
        internal bool IsMissing { get { return digest == null; } }
        internal Guid Generation { get { return generation; } }
        internal long Revision { get { return revision; } }
        internal DateTime LocalDay { get { return localDay; } }

        private VaultDailyFileStamp(string path, VaultDailyRecord record, byte[] hash)
        {
            if (String.IsNullOrEmpty(path)) throw new ArgumentException("Invalid daily vault stamp.");
            this.path = path;
            if (record != null) {
                generation = record.Generation; revision = record.Revision; localDay = record.State.LocalDay;
                digest = (byte[])hash.Clone();
            }
        }
        internal static VaultDailyFileStamp Create(string path, VaultDailyRecord record, byte[] envelope)
        {
            if (record == null) return new VaultDailyFileStamp(path, null, null);
            VaultDailyRecordCodec.Validate(record.State, record.Generation, record.Revision);
            if (envelope == null || envelope.Length < 16 || envelope.Length > VaultDailyRecordCodec.MaxEnvelopeBytes)
                throw new ArgumentException("Invalid daily vault stamp.");
            byte[] hash = null;
            try {
                using (var sha = SHA256.Create()) hash = sha.ComputeHash(envelope);
                return new VaultDailyFileStamp(path, record, hash);
            } finally { if (hash != null) Array.Clear(hash, 0, hash.Length); }
        }
        internal bool BelongsTo(string canonicalDataPath)
        { return String.Equals(path, canonicalDataPath, StringComparison.OrdinalIgnoreCase); }
        internal bool Matches(VaultDailyFileStamp other)
        {
            if (other == null || !BelongsTo(other.path) || IsMissing != other.IsMissing) return false;
            if (IsMissing) return true;
            if (generation != other.generation || revision != other.revision || localDay != other.localDay) return false;
            int difference = 0;
            for (int i = 0; i < digest.Length; i++) difference |= digest[i] ^ other.digest[i];
            return difference == 0;
        }
    }

    internal sealed class VaultDailyFileReadResult
    {
        internal VaultDailyFileReadStatus Status { get; private set; }
        internal VaultDailyRecord Record { get; private set; }
        internal VaultDailyFileStamp Stamp { get; private set; }
        internal string ErrorMessage { get; private set; }
        private VaultDailyFileReadResult(VaultDailyFileReadStatus status, VaultDailyRecord record,
            VaultDailyFileStamp stamp, string error)
        { Status = status; Record = record; Stamp = stamp; ErrorMessage = error; }
        internal static VaultDailyFileReadResult Loaded(string path, VaultDailyRecord record, byte[] envelope)
        {
            if (record == null) throw new ArgumentNullException("record");
            return new VaultDailyFileReadResult(VaultDailyFileReadStatus.Success, record,
                VaultDailyFileStamp.Create(path, record, envelope), null);
        }
        internal static VaultDailyFileReadResult Absent(string path)
        { return new VaultDailyFileReadResult(VaultDailyFileReadStatus.Missing, null, VaultDailyFileStamp.Create(path, null, null), null); }
        internal static VaultDailyFileReadResult Failed(VaultDailyFileReadStatus status)
        {
            string error;
            switch (status) {
                case VaultDailyFileReadStatus.FileUnavailable:
                    error = "Не удалось прочитать суточное хранилище."; break;
                case VaultDailyFileReadStatus.InvalidData:
                    error = VaultDailyRecordCodec.InvalidMessage; break;
                case VaultDailyFileReadStatus.UnsupportedFormat:
                    error = "Формат суточного хранилища не поддерживается."; break;
                default: throw new ArgumentException("Invalid daily vault read status.");
            }
            return new VaultDailyFileReadResult(status, null, null, error);
        }
    }

    // Local directory and file checks reduce accidental redirection. They are not a
    // handle-relative filesystem sandbox against an attacker replacing ancestors.
    internal static class VaultDailyFile
    {
        internal const string FileName = "vault.daily.dat";
        internal const string RecoveryFileName = FileName + ".recovery";
        internal const string RecoveryBackupFileName = FileName + ".recovery.previous";
        internal static string CheckedPath(string root)
        {
            if (Path.DirectorySeparatorChar != '\\' || String.IsNullOrEmpty(root) || root.Length < 3 ||
                !IsDriveLetter(root[0]) || root[1] != ':' || (root[2] != '\\' && root[2] != '/')) throw Unavailable();
            for (int i = 0; i < root.Length; i++) {
                char c = root[i];
                if ((c == ':' && i != 1) || c < 32 || c == '"' || c == '<' || c == '>' ||
                    c == '|' || c == '*' || c == '?') throw Unavailable();
            }
            var full = Path.GetFullPath(root).Replace('/', '\\');
            var drive = Path.GetPathRoot(full);
            if (drive == null || drive.Length != 3 || !IsDriveLetter(drive[0]) || drive[1] != ':' || drive[2] != '\\')
                throw Unavailable();
            if (new DriveInfo(drive).DriveType == DriveType.Network) throw Unavailable();
            if (full.Length > drive.Length) full = full.TrimEnd('\\');
            var current = full;
            while (true) {
                var attributes = File.GetAttributes(current);
                if ((attributes & FileAttributes.Directory) == 0 || (attributes & FileAttributes.ReparsePoint) != 0)
                    throw Unavailable();
                if (String.Equals(current, drive, StringComparison.OrdinalIgnoreCase)) break;
                var parent = Directory.GetParent(current);
                if (parent == null) throw Unavailable();
                current = parent.FullName;
            }
            return Path.Combine(full, FileName);
        }

        internal static VaultDailyFileReadResult Read(string root)
        {
            byte[] envelope = null;
            try {
                var path = CheckedPath(root);
                RequireNoRecovery(path);
                try {
                    var attributes = File.GetAttributes(path);
                    if ((attributes & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0) throw Unavailable();
                    using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                        var length = stream.Length;
                        if (length < 16 || length > VaultDailyRecordCodec.MaxEnvelopeBytes)
                            return VaultDailyFileReadResult.Failed(VaultDailyFileReadStatus.InvalidData);
                        envelope = new byte[(int)length];
                        int offset = 0;
                        while (offset < envelope.Length) {
                            int read = stream.Read(envelope, offset, envelope.Length - offset);
                            if (read == 0) return VaultDailyFileReadResult.Failed(VaultDailyFileReadStatus.InvalidData);
                            offset += read;
                        }
                        if (stream.ReadByte() != -1) return VaultDailyFileReadResult.Failed(VaultDailyFileReadStatus.InvalidData);
                    }
                    var decoded = VaultDailyRecordCodec.Decode(envelope);
                    RequireNoRecovery(path);
                    if (decoded.Status == VaultDailyDecodeStatus.Success)
                        return VaultDailyFileReadResult.Loaded(path, decoded.Record, envelope);
                    return VaultDailyFileReadResult.Failed(decoded.Status == VaultDailyDecodeStatus.UnsupportedFormat ?
                        VaultDailyFileReadStatus.UnsupportedFormat : VaultDailyFileReadStatus.InvalidData);
                } catch (FileNotFoundException) {
                    // Missing means this exact child is absent in a still valid root.
                    if (!String.Equals(path, CheckedPath(root), StringComparison.OrdinalIgnoreCase)) throw Unavailable();
                    RequireNoRecovery(path);
                    return VaultDailyFileReadResult.Absent(path);
                }
            } catch (Exception ex) {
                if (ex is OutOfMemoryException) throw;
                return VaultDailyFileReadResult.Failed(VaultDailyFileReadStatus.FileUnavailable);
            } finally { if (envelope != null) Array.Clear(envelope, 0, envelope.Length); }
        }
        private static bool IsDriveLetter(char c)
        { return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'); }
        private static void RequireNoRecovery(string path)
        {
            // Stable witness and a native backup are independent recovery evidence.
            RequireAbsent(path + ".recovery");
            RequireAbsent(path + ".recovery.previous");
        }
        private static void RequireAbsent(string path)
        {
            try { File.GetAttributes(path); }
            catch (FileNotFoundException) { return; }
            throw Unavailable();
        }
        private static IOException Unavailable()
        { return new IOException("Суточное хранилище недоступно."); }
    }
}

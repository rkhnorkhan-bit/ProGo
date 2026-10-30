using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ProGo
{
    // Persisted evidence owns names and exact old/new images, never a PIN or dataset.
    // Record/PIN validation still belongs to the writer before constructing this journal.
    internal sealed class VaultDailyJournal
    {
        internal string DataPath { get; private set; }
        internal Guid Operation { get; private set; }
        internal VaultDailyFileStamp Previous { get; private set; }
        internal VaultDailyFileStamp Next { get; private set; }
        internal string StageFileName { get { return VaultDailyFile.FileName + ".new-" + Operation.ToString("N"); } }
        internal string BackupFileName { get { return VaultDailyFile.FileName + ".previous-" + Operation.ToString("N"); } }

        private VaultDailyJournal(string path, Guid operation, VaultDailyFileStamp previous, VaultDailyFileStamp next)
        { DataPath = path; Operation = operation; Previous = previous; Next = next; }

        internal static VaultDailyJournal Create(string path, Guid operation, VaultDailyFileStamp previous,
            VaultDailyFileStamp next)
        {
            try {
                if (String.IsNullOrEmpty(path) || new UTF8Encoding(false, true).GetByteCount(path) > VaultDailyJournalCodec.MaxPathBytes ||
                    operation == Guid.Empty || previous == null || next == null || next.IsMissing ||
                    !previous.BelongsTo(path) || !next.BelongsTo(path)) throw Invalid();
                if (previous.IsMissing) {
                    if (next.Revision != 0) throw Invalid();
                } else if (previous.LocalDay == next.LocalDay) {
                    if (previous.Revision == Int64.MaxValue || next.Generation != previous.Generation ||
                        next.Revision != checked(previous.Revision + 1)) throw Invalid();
                } else if (next.Generation == previous.Generation || next.Revision != 0) throw Invalid();
                return new VaultDailyJournal(path, operation, previous, next);
            } catch (Exception ex) {
                if (ex is OutOfMemoryException) throw;
                throw Invalid();
            }
        }
        private static InvalidDataException Invalid()
        { return new InvalidDataException(VaultDailyJournalCodec.InvalidMessage); }
    }

    internal enum VaultDailyJournalDecodeStatus { Success, InvalidData, UnsupportedFormat }
    internal sealed class VaultDailyJournalDecodeResult
    {
        internal VaultDailyJournalDecodeStatus Status { get; private set; }
        internal VaultDailyJournal Journal { get; private set; }
        internal string ErrorMessage { get { return Status == VaultDailyJournalDecodeStatus.Success ? null :
            Status == VaultDailyJournalDecodeStatus.UnsupportedFormat ? "Формат восстановления суточного хранилища не поддерживается." :
            VaultDailyJournalCodec.InvalidMessage; } }
        private VaultDailyJournalDecodeResult(VaultDailyJournalDecodeStatus status, VaultDailyJournal journal)
        { Status = status; Journal = journal; }
        internal static VaultDailyJournalDecodeResult Succeeded(VaultDailyJournal journal)
        {
            if (journal == null) throw new ArgumentNullException("journal");
            return new VaultDailyJournalDecodeResult(VaultDailyJournalDecodeStatus.Success, journal);
        }
        internal static VaultDailyJournalDecodeResult Failed(VaultDailyJournalDecodeStatus status)
        {
            if (status != VaultDailyJournalDecodeStatus.InvalidData && status != VaultDailyJournalDecodeStatus.UnsupportedFormat)
                throw new ArgumentException("Invalid journal decode status.");
            return new VaultDailyJournalDecodeResult(status, null);
        }
    }

    // Pure memory codec. The caller supplies the already checked canonical data path.
    internal static class VaultDailyJournalCodec
    {
        internal const int MaxPathBytes = 4096;
        internal const int MaxPlainBytes = 8192;
        internal const int MaxEnvelopeBytes = MaxPlainBytes + 65536;
        internal const string InvalidMessage = "Данные восстановления суточного хранилища повреждены или недоступны.";
        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("PGODJNL1");
        private static readonly byte[] Purpose = Encoding.ASCII.GetBytes("ProGo/VaultDailyJournal/v1");
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);

        internal static byte[] Encode(VaultDailyJournal journal)
        {
            var stream = new MemoryStream(MaxPlainBytes);
            byte[] plain = null, protectedBytes = null, pathBytes = null, operationBytes = null;
            try {
                if (journal == null) throw Invalid();
                VaultDailyJournal.Create(journal.DataPath, journal.Operation, journal.Previous, journal.Next);
                pathBytes = Utf8.GetBytes(journal.DataPath); operationBytes = journal.Operation.ToByteArray();
                using (var writer = new BinaryWriter(stream, Utf8, true)) {
                    writer.Write(1); writer.Write(pathBytes.Length); writer.Write(pathBytes); writer.Write(operationBytes);
                    journal.Previous.WriteEvidence(writer); journal.Next.WriteEvidence(writer);
                    writer.Flush();
                }
                if (stream.Length > MaxPlainBytes) throw Invalid();
                plain = stream.ToArray();
                protectedBytes = ProtectedData.Protect(plain, Purpose, DataProtectionScope.CurrentUser);
                if (protectedBytes.Length == 0 || protectedBytes.Length > MaxEnvelopeBytes - 16) throw Invalid();
                var envelope = new byte[16 + protectedBytes.Length];
                using (var writer = new BinaryWriter(new MemoryStream(envelope, true))) {
                    writer.Write(Magic); writer.Write(1); writer.Write(protectedBytes.Length); writer.Write(protectedBytes);
                }
                return envelope;
            } catch (Exception ex) {
                if (ex is OutOfMemoryException) throw;
                throw Invalid();
            } finally {
                Clear(plain); Clear(protectedBytes); Clear(pathBytes); Clear(operationBytes);
                Clear(stream.GetBuffer()); stream.Dispose();
            }
        }

        internal static VaultDailyJournalDecodeResult Decode(byte[] envelope, string expectedCanonicalDataPath)
        {
            byte[] protectedBytes = null, plain = null, pathBytes = null, operationBytes = null;
            try {
                if (String.IsNullOrEmpty(expectedCanonicalDataPath) || Utf8.GetByteCount(expectedCanonicalDataPath) > MaxPathBytes ||
                    envelope == null || envelope.Length < 16 || envelope.Length > MaxEnvelopeBytes) throw Invalid();
                using (var reader = new BinaryReader(new MemoryStream(envelope, false), Utf8)) {
                    for (int i = 0; i < Magic.Length; i++) if (reader.ReadByte() != Magic[i]) throw Invalid();
                    int version = reader.ReadInt32(), size = reader.ReadInt32();
                    if (size <= 0 || size > MaxEnvelopeBytes - 16 || size != Remaining(reader)) throw Invalid();
                    if (version != 1) return VaultDailyJournalDecodeResult.Failed(VaultDailyJournalDecodeStatus.UnsupportedFormat);
                    protectedBytes = reader.ReadBytes(size);
                }
                plain = ProtectedData.Unprotect(protectedBytes, Purpose, DataProtectionScope.CurrentUser);
                if (plain.Length < 4 || plain.Length > MaxPlainBytes) throw Invalid();
                using (var reader = new BinaryReader(new MemoryStream(plain, false), Utf8)) {
                    if (reader.ReadInt32() != 1) return VaultDailyJournalDecodeResult.Failed(VaultDailyJournalDecodeStatus.UnsupportedFormat);
                    int size = reader.ReadInt32();
                    if (size <= 0 || size > MaxPathBytes || size > Remaining(reader)) throw Invalid();
                    pathBytes = reader.ReadBytes(size);
                    string path = Utf8.GetString(pathBytes);
                    if (!String.Equals(path, expectedCanonicalDataPath, StringComparison.OrdinalIgnoreCase)) throw Invalid();
                    operationBytes = reader.ReadBytes(16);
                    if (operationBytes.Length != 16) throw Invalid();
                    Guid operation = new Guid(operationBytes);
                    var previous = VaultDailyFileStamp.ReadEvidence(reader, expectedCanonicalDataPath);
                    var next = VaultDailyFileStamp.ReadEvidence(reader, expectedCanonicalDataPath);
                    if (Remaining(reader) != 0) throw Invalid();
                    return VaultDailyJournalDecodeResult.Succeeded(VaultDailyJournal.Create(expectedCanonicalDataPath, operation, previous, next));
                }
            } catch (Exception ex) {
                if (ex is OutOfMemoryException) throw;
                return VaultDailyJournalDecodeResult.Failed(VaultDailyJournalDecodeStatus.InvalidData);
            } finally { Clear(protectedBytes); Clear(plain); Clear(pathBytes); Clear(operationBytes); }
        }
        private static long Remaining(BinaryReader reader)
        { return reader.BaseStream.Length - reader.BaseStream.Position; }
        private static InvalidDataException Invalid()
        { return new InvalidDataException(InvalidMessage); }
        private static void Clear(byte[] bytes)
        { if (bytes != null) Array.Clear(bytes, 0, bytes.Length); }
    }
}

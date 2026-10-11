using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ProGo
{
    // Metadata is immutable; mutable FakeData must be owned/snapshotted by the later store.
    internal sealed class VaultDailyRecord
    {
        internal VaultDailyState State { get; private set; }
        internal Guid Generation { get; private set; }
        internal long Revision { get; private set; }
        internal VaultDailyRecord(VaultDailyState state, Guid generation, long revision)
        {
            VaultDailyRecordCodec.Validate(state, generation, revision);
            State = state; Generation = generation; Revision = revision;
        }
    }

    internal enum VaultDailyDecodeStatus { Success, InvalidData, UnsupportedFormat }
    internal sealed class VaultDailyDecodeResult
    {
        internal VaultDailyDecodeStatus Status { get; private set; }
        internal VaultDailyRecord Record { get; private set; }
        internal string ErrorMessage { get { return Status == VaultDailyDecodeStatus.Success ? null :
            Status == VaultDailyDecodeStatus.UnsupportedFormat ? "Формат суточного хранилища не поддерживается." :
            VaultDailyRecordCodec.InvalidMessage; } }
        private VaultDailyDecodeResult(VaultDailyDecodeStatus status, VaultDailyRecord record)
        { Status = status; Record = record; }
        internal static VaultDailyDecodeResult Succeeded(VaultDailyRecord record)
        { if (record == null) throw new ArgumentNullException("record"); return new VaultDailyDecodeResult(VaultDailyDecodeStatus.Success, record); }
        internal static VaultDailyDecodeResult Failed(VaultDailyDecodeStatus status)
        { if (status != VaultDailyDecodeStatus.InvalidData && status != VaultDailyDecodeStatus.UnsupportedFormat)
                throw new ArgumentException("Invalid decode status."); return new VaultDailyDecodeResult(status, null); }
    }

    internal static class VaultDailyRecordCodec
    {
        internal const int MaxPlainBytes = 4 * 1024 * 1024;
        internal const int MaxEnvelopeBytes = MaxPlainBytes + 65536;
        internal const int MaxEntries = 4096;
        internal const int MaxFieldBytes = 65536;
        internal const string InvalidMessage = "Суточное хранилище повреждено или недоступно.";
        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("PGODAILY");
        private static readonly byte[] Purpose = Encoding.ASCII.GetBytes("ProGo/VaultDailyRecord/v1");
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);

        internal static void Validate(VaultDailyState state, Guid generation, long revision)
        {
            if (state == null || state.LocalDay.Kind != DateTimeKind.Unspecified ||
                state.LocalDay.TimeOfDay != TimeSpan.Zero || generation == Guid.Empty || revision < 0 ||
                (!state.HasFake && (state.FakePin != null || state.FakeData != null)) ||
                (state.HasFake && (!VaultDailyPolicy.IsValidPin(state.FakePin) ||
                    state.FakeData.entries == null || state.FakeData.entries.Count > MaxEntries ||
                    !VaultDailyPolicy.IsValidData(state.FakeData)))) throw Invalid();
        }

        internal static byte[] Encode(VaultDailyRecord record)
        {
            // Fixed capacity avoids abandoned plaintext backing arrays during stream growth.
            var stream = new MemoryStream(MaxPlainBytes); byte[] plain = null, protectedBytes = null;
            try {
                if (record == null) throw Invalid();
                Validate(record.State, record.Generation, record.Revision);
                var writer = new BinaryWriter(stream, Utf8, true);
                writer.Write(1); writer.Write(record.State.LocalDay.Ticks);
                var generation = record.Generation.ToByteArray();
                try { writer.Write(generation); } finally { Clear(generation); }
                writer.Write(record.Revision); writer.Write((byte)(record.State.HasFake ? 1 : 0));
                if (record.State.HasFake) {
                    WriteString(writer, record.State.FakePin);
                    EnsureSpace(writer, 8);
                    writer.Write(record.State.FakeData.version); writer.Write(record.State.FakeData.entries.Count);
                    foreach (var entry in record.State.FakeData.entries) {
                        WriteString(writer, entry.id); WriteString(writer, entry.name); WriteString(writer, entry.type);
                        WriteString(writer, entry.login); WriteString(writer, entry.secret); WriteString(writer, entry.url_or_host);
                        WriteString(writer, entry.notes); WriteString(writer, entry.tags);
                        WriteString(writer, entry.created_at); WriteString(writer, entry.updated_at);
                    }
                }
                writer.Flush(); writer.Dispose(); plain = stream.ToArray();
                protectedBytes = ProtectedData.Protect(plain, Purpose, DataProtectionScope.CurrentUser);
                if (protectedBytes.Length == 0 || protectedBytes.Length > MaxEnvelopeBytes - 16) throw Invalid();
                var envelope = new byte[16 + protectedBytes.Length];
                using (var outer = new BinaryWriter(new MemoryStream(envelope, true))) {
                    outer.Write(Magic); outer.Write(1); outer.Write(protectedBytes.Length); outer.Write(protectedBytes);
                }
                return envelope;
            } catch (Exception ex) {
                if (ex is OutOfMemoryException) throw;
                throw Invalid();
            } finally { Clear(plain); Clear(protectedBytes); Clear(stream.GetBuffer()); stream.Dispose(); }
        }

        internal static VaultDailyDecodeResult Decode(byte[] envelope)
        {
            byte[] protectedBytes = null, plain = null;
            try {
                if (envelope == null || envelope.Length < 16 || envelope.Length > MaxEnvelopeBytes) throw Invalid();
                using (var reader = new BinaryReader(new MemoryStream(envelope, false), Utf8)) {
                    for (int i = 0; i < Magic.Length; i++) if (reader.ReadByte() != Magic[i]) throw Invalid();
                    var version = reader.ReadInt32(); var size = reader.ReadInt32();
                    if (size <= 0 || size > MaxEnvelopeBytes - 16 || size != Remaining(reader)) throw Invalid();
                    if (version != 1) return VaultDailyDecodeResult.Failed(VaultDailyDecodeStatus.UnsupportedFormat);
                    protectedBytes = reader.ReadBytes(size);
                }
                plain = ProtectedData.Unprotect(protectedBytes, Purpose, DataProtectionScope.CurrentUser);
                if (plain.Length > MaxPlainBytes || plain.Length < 37) throw Invalid();
                using (var reader = new BinaryReader(new MemoryStream(plain, false), Utf8)) {
                    if (reader.ReadInt32() != 1) return VaultDailyDecodeResult.Failed(VaultDailyDecodeStatus.UnsupportedFormat);
                    var day = new DateTime(reader.ReadInt64(), DateTimeKind.Unspecified);
                    if (day.TimeOfDay != TimeSpan.Zero) throw Invalid();
                    var generationBytes = reader.ReadBytes(16);
                    Guid generation;
                    try { if (generationBytes.Length != 16) throw Invalid(); generation = new Guid(generationBytes); }
                    finally { Clear(generationBytes); }
                    var revision = reader.ReadInt64(); var bound = reader.ReadByte();
                    if (generation == Guid.Empty || revision < 0 || bound > 1) throw Invalid();
                    var state = VaultDailyState.Empty(day);
                    if (bound == 1) {
                        var pin = ReadString(reader);
                        if (!VaultDailyPolicy.IsValidPin(pin)) throw Invalid();
                        var dataVersion = reader.ReadInt32(); var count = reader.ReadInt32();
                        if (dataVersion != 1 || count < 0 || count > MaxEntries || count > Remaining(reader) / 40) throw Invalid();
                        var entries = new List<VaultEntry>(count); var ids = new HashSet<string>(StringComparer.Ordinal);
                        for (int i = 0; i < count; i++) {
                            var id = ReadString(reader);
                            if (String.IsNullOrEmpty(id) || !ids.Add(id)) throw Invalid();
                            entries.Add(new VaultEntry { id = id, name = ReadString(reader), type = ReadString(reader),
                                login = ReadString(reader), secret = ReadString(reader), url_or_host = ReadString(reader),
                                notes = ReadString(reader), tags = ReadString(reader), created_at = ReadString(reader), updated_at = ReadString(reader) });
                        }
                        state = VaultDailyState.Bind(day, pin, new VaultData { version = dataVersion, entries = entries });
                    }
                    if (Remaining(reader) != 0) throw Invalid();
                    return VaultDailyDecodeResult.Succeeded(new VaultDailyRecord(state, generation, revision));
                }
            } catch (Exception ex) {
                if (ex is OutOfMemoryException) throw;
                return VaultDailyDecodeResult.Failed(VaultDailyDecodeStatus.InvalidData);
            } finally { Clear(protectedBytes); Clear(plain); }
        }

        private static void WriteString(BinaryWriter writer, string value)
        {
            var count = value == null ? 0 : Utf8.GetByteCount(value);
            if (count > MaxFieldBytes) throw Invalid();
            EnsureSpace(writer, 4L + count);
            writer.Write(value == null ? -1 : count);
            if (value == null) return;
            var bytes = Utf8.GetBytes(value);
            try { writer.Write(bytes); } finally { Clear(bytes); }
        }
        private static string ReadString(BinaryReader reader)
        {
            var size = reader.ReadInt32();
            if (size == -1) return null;
            if (size < 0 || size > MaxFieldBytes || size > Remaining(reader)) throw Invalid();
            var bytes = reader.ReadBytes(size);
            try { return Utf8.GetString(bytes); } finally { Clear(bytes); }
        }
        private static long Remaining(BinaryReader reader)
        { return reader.BaseStream.Length - reader.BaseStream.Position; }
        private static void EnsureSpace(BinaryWriter writer, long count)
        { if (writer.BaseStream.Length + count > MaxPlainBytes) throw Invalid(); }
        private static InvalidDataException Invalid() { return new InvalidDataException(InvalidMessage); }
        private static void Clear(byte[] bytes) { if (bytes != null) Array.Clear(bytes, 0, bytes.Length); }
    }
}

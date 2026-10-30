using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ProGo
{
    internal static class AppPaths
    {
        internal static int Accesses;
        public static string VaultPath { get { Accesses++; throw new InvalidOperationException("Vault path guard"); } }
        public static void EnsureDirectories() { Accesses++; throw new InvalidOperationException("Vault path guard"); }
    }
    internal static class SafeLog
    {
        internal static int Accesses;
        public static void Info(string message) { Accesses++; throw new InvalidOperationException("Vault log guard"); }
    }

    internal static class VaultDailyRecordTests
    {
        private static int passed;
        private static readonly DateTime Day = new DateTime(2026, 10, 11);
        private static readonly Guid Generation = new Guid("f531b344-9b1f-471d-ab61-1f5e19020741");
        private const string Pin = "\u0661\u0662\u0663\u0664";
        private static readonly byte[] Purpose = Encoding.ASCII.GetBytes("ProGo/VaultDailyRecord/v1");
        private static void Check(bool condition, string name)
        { if (!condition) throw new Exception(name); passed++; Console.WriteLine("PASS: " + name); }
        private static VaultDailyRecord Record(VaultData data, long revision = 0)
        { return new VaultDailyRecord(data == null ? VaultDailyState.Empty(Day) : VaultDailyState.Bind(Day, Pin, data), Generation, revision); }
        private static VaultData Data()
        { return new VaultData { version = 1, entries = new List<VaultEntry> { new VaultEntry {
            id = "record", name = "Имя \U0001f510", type = "password", login = "логин", secret = "synthetic-secret",
            url_or_host = "example.org", notes = "edited", tags = "", created_at = null, updated_at = "2026-10-11" } } }; }
        // Independently specified little-endian wire fixtures; never call codec encode here.
        private static byte[] Bytes(Action<BinaryWriter> emit)
        { using (var stream = new MemoryStream()) { using (var writer = new BinaryWriter(stream, new UTF8Encoding(false), true)) emit(writer); return stream.ToArray(); } }
        private static void Header(BinaryWriter writer, int version = 1, long? ticks = null, Guid? generation = null, long revision = 0, byte bound = 0)
        { writer.Write(version); writer.Write(ticks ?? Day.Ticks); writer.Write((generation ?? Generation).ToByteArray()); writer.Write(revision); writer.Write(bound); }
        private static void StringField(BinaryWriter writer, string value)
        { if (value == null) { writer.Write(-1); return; } var bytes = Encoding.UTF8.GetBytes(value); writer.Write(bytes.Length); writer.Write(bytes); }
        private static void Entry(BinaryWriter writer, string id)
        { StringField(writer, id); for (int i = 0; i < 9; i++) StringField(writer, i == 0 ? "independent" : null); }
        private static void Bound(BinaryWriter writer, string pin = Pin, int version = 1, int count = 1)
        { StringField(writer, pin); writer.Write(version); writer.Write(count); }
        private static byte[] Wrap(byte[] protectedBytes, int version = 1)
        { return Bytes(delegate(BinaryWriter w) { w.Write(Encoding.ASCII.GetBytes("PGODAILY")); w.Write(version); w.Write(protectedBytes.Length); w.Write(protectedBytes); }); }
        private static byte[] Protect(byte[] plain, byte[] purpose = null)
        { var protectedBytes = ProtectedData.Protect(plain, purpose ?? Purpose, DataProtectionScope.CurrentUser);
            try { return Wrap(protectedBytes); } finally { Array.Clear(protectedBytes, 0, protectedBytes.Length); } }
        private static void Failed(byte[] envelope, string name, VaultDailyDecodeStatus status = VaultDailyDecodeStatus.InvalidData)
        {
            var before = envelope == null ? null : (byte[])envelope.Clone();
            var result = VaultDailyRecordCodec.Decode(envelope);
            Check(result.Status == status && result.Record == null && !String.IsNullOrEmpty(result.ErrorMessage), name);
            Check(envelope == null || envelope.SequenceEqual(before), name + " preserves caller bytes");
            Check(result.ErrorMessage == (status == VaultDailyDecodeStatus.UnsupportedFormat ?
                "Формат суточного хранилища не поддерживается." : "Суточное хранилище повреждено или недоступно."), name + " has static error");
        }
        private static void BadPlain(Action<BinaryWriter> emit, string name)
        { Failed(Protect(Bytes(emit)), name); }
        private static void EncodeRejected(Action action, string name)
        { bool rejected = false; try { action(); } catch (InvalidDataException ex) { rejected = ex.Message == VaultDailyRecordCodec.InvalidMessage && ex.InnerException == null; }
            Check(rejected, name); }
        private static void RoundTrips()
        {
            foreach (var revision in new long[] { 0, 41, Int64.MaxValue }) {
                var encoded = VaultDailyRecordCodec.Encode(Record(null, revision)); var before = (byte[])encoded.Clone();
                var decoded = VaultDailyRecordCodec.Decode(encoded);
                Check(decoded.Status == VaultDailyDecodeStatus.Success && decoded.ErrorMessage == null, "empty protected record roundtrips");
                Check(decoded.Record.Generation == Generation && decoded.Record.Revision == revision && decoded.Record.State.LocalDay == Day &&
                    decoded.Record.State.LocalDay.Kind == DateTimeKind.Unspecified && !decoded.Record.State.HasFake &&
                    decoded.Record.State.FakePin == null && decoded.Record.State.FakeData == null, "empty metadata remains exact");
                Check(encoded.SequenceEqual(before), "successful decode preserves caller envelope");
            }
            var data = Data(); var source = Record(data, 7); var encodedBound = VaultDailyRecordCodec.Encode(source);
            var rebound = VaultDailyRecordCodec.Decode(encodedBound).Record;
            Check(rebound != null && rebound.State.HasFake && rebound.State.FakePin == Pin && rebound.Revision == 7, "Unicode PIN and bound metadata roundtrip");
            var actual = rebound.State.FakeData.entries[0]; var original = data.entries[0];
            Check(actual.id == original.id && actual.name == original.name && actual.type == original.type && actual.login == original.login &&
                actual.secret == original.secret && actual.url_or_host == original.url_or_host && actual.notes == original.notes &&
                actual.tags == "" && actual.created_at == null && actual.updated_at == original.updated_at, "all ten edited fields and null versus empty remain exact");
            Check(Object.ReferenceEquals(source.State.FakeData, data) && Object.ReferenceEquals(data.entries[0], original) && original.notes == "edited", "encode leaves caller dataset identity and edits intact");
            Check(!Object.ReferenceEquals(data, rebound.State.FakeData) && !Object.ReferenceEquals(original, actual), "decoded dataset owns distinct records");
            Check(!Encoding.UTF8.GetString(encodedBound).Contains("synthetic-secret") && !Encoding.UTF8.GetString(encodedBound).Contains(Pin), "public envelope conceals PIN and dataset");
            var deleted = VaultDailyRecordCodec.Decode(VaultDailyRecordCodec.Encode(Record(VaultData.Empty()))).Record;
            Check(deleted != null && deleted.State.HasFake && deleted.State.FakePin == Pin && deleted.State.FakeData.entries.Count == 0,
                "deleting every fake entry preserves binding distinct from empty tombstone");
            var independent = Protect(Bytes(delegate(BinaryWriter w) { Header(w, bound: 1); Bound(w); Entry(w, "fixture"); }));
            var fixture = VaultDailyRecordCodec.Decode(independent).Record;
            Check(fixture != null && fixture.State.FakePin == Pin && fixture.State.FakeData.entries[0].id == "fixture" &&
                fixture.State.FakeData.entries[0].name == "independent", "independent protected binary fixture decodes");
            byte[] protectedBytes = encodedBound.Skip(16).ToArray(); byte[] plain = null;
            try {
                plain = ProtectedData.Unprotect(protectedBytes, Purpose, DataProtectionScope.CurrentUser);
                using (var reader = new BinaryReader(new MemoryStream(plain, false))) {
                    Check(reader.ReadInt32() == 1 && reader.ReadInt64() == Day.Ticks && new Guid(reader.ReadBytes(16)) == Generation &&
                        reader.ReadInt64() == 7 && reader.ReadByte() == 1, "independent native unprotect observes protected inner metadata");
                }
            } finally { Array.Clear(protectedBytes, 0, protectedBytes.Length); if (plain != null) Array.Clear(plain, 0, plain.Length); }
        }
        private static void EnvelopeFailures()
        {
            Failed(null, "missing envelope"); Failed(new byte[0], "zero envelope"); Failed(new byte[16], "wrong magic");
            var encoded = VaultDailyRecordCodec.Encode(Record(null));
            for (int i = 0; i < 16; i++) Failed(encoded.Take(i).ToArray(), "truncated outer header " + i);
            var damaged = (byte[])encoded.Clone(); damaged[0] ^= 1; Failed(damaged, "changed magic");
            Failed(encoded.Concat(new byte[] { 0 }).ToArray(), "outer trailing bytes"); Failed(encoded.Take(encoded.Length - 1).ToArray(), "truncated protected body");
            foreach (var size in new[] { -1, 0, Int32.MaxValue, encoded.Length }) {
                var invalid = (byte[])encoded.Clone(); Buffer.BlockCopy(BitConverter.GetBytes(size), 0, invalid, 12, 4); Failed(invalid, "invalid declared body size " + size);
            }
            var unsupported = (byte[])encoded.Clone(); Buffer.BlockCopy(BitConverter.GetBytes(2), 0, unsupported, 8, 4);
            Failed(unsupported, "unknown outer version", VaultDailyDecodeStatus.UnsupportedFormat);
            Failed(unsupported.Concat(new byte[] { 0 }).ToArray(), "unknown version still requires exact framing");
            damaged = (byte[])encoded.Clone(); damaged[damaged.Length - 1] ^= 1; Failed(damaged, "DPAPI payload tamper");
            Failed(Protect(Bytes(delegate(BinaryWriter w) { Header(w); }), new byte[] { 1, 2, 3 }), "DPAPI purpose mismatch");
            Failed(new byte[VaultDailyRecordCodec.MaxEnvelopeBytes + 1], "envelope cap");
        }
        private static void ProtectedFailures()
        {
            Failed(Protect(Bytes(delegate(BinaryWriter w) { Header(w, version: 2); })), "unknown inner version", VaultDailyDecodeStatus.UnsupportedFormat);
            for (int i = 0; i < 37; i++) BadPlain(delegate(BinaryWriter w) { w.Write(new byte[i]); }, "short inner record " + i);
            foreach (var ticks in new[] { -1L, Int64.MaxValue, Day.AddTicks(1).Ticks, DateTime.SpecifyKind(Day, DateTimeKind.Utc).ToBinary() })
                BadPlain(delegate(BinaryWriter w) { Header(w, ticks: ticks); }, "invalid calendar day " + ticks);
            BadPlain(delegate(BinaryWriter w) { Header(w, generation: Guid.Empty); }, "empty generation");
            BadPlain(delegate(BinaryWriter w) { Header(w, revision: -1); }, "negative revision");
            BadPlain(delegate(BinaryWriter w) { Header(w, bound: 2); }, "unknown binding flag");
            BadPlain(delegate(BinaryWriter w) { Header(w); w.Write(0); }, "empty state cannot carry trailing data");
            foreach (var pin in new[] { null, "", "123", "12345", "1x34", "\ud801\udca0" })
                BadPlain(delegate(BinaryWriter w) { Header(w, bound: 1); Bound(w, pin); Entry(w, "id"); }, "invalid fake PIN shape");
            foreach (var count in new[] { -1, Int32.MaxValue, VaultDailyRecordCodec.MaxEntries + 1, 1 })
                BadPlain(delegate(BinaryWriter w) { Header(w, bound: 1); Bound(w, count: count); }, "invalid or truncated entry count " + count);
            BadPlain(delegate(BinaryWriter w) { Header(w, bound: 1); Bound(w, version: 2); Entry(w, "id"); }, "unsupported dataset version");
            foreach (var id in new[] { null, "" }) BadPlain(delegate(BinaryWriter w) { Header(w, bound: 1); Bound(w); Entry(w, id); }, "missing entry id");
            BadPlain(delegate(BinaryWriter w) { Header(w, bound: 1); Bound(w, count: 2); Entry(w, "same"); Entry(w, "same"); }, "duplicate ordinal id");
            BadPlain(delegate(BinaryWriter w) { Header(w, bound: 1); Bound(w); w.Write(2); w.Write(new byte[] { 0xc3, 0x28 }); for (int i = 0; i < 9; i++) w.Write(-1); }, "strict UTF8 rejects invalid byte sequence");
            foreach (var size in new[] { -2, Int32.MaxValue, VaultDailyRecordCodec.MaxFieldBytes + 1, 500 })
                BadPlain(delegate(BinaryWriter w) { Header(w, bound: 1); Bound(w); w.Write(size); w.Write(new byte[40]); }, "invalid field byte length " + size);
            BadPlain(delegate(BinaryWriter w) { Header(w, bound: 1); Bound(w); Entry(w, "id"); w.Write((byte)1); }, "bound state trailing bytes");
            var hugePlain = new byte[VaultDailyRecordCodec.MaxPlainBytes + 1];
            Failed(Protect(hugePlain), "protected plaintext cap");
        }
        private static void EncodeBounds()
        {
            EncodeRejected(delegate { new VaultDailyRecord(null, Generation, 0); }, "null state rejected");
            EncodeRejected(delegate { new VaultDailyRecord(VaultDailyState.Empty(Day), Guid.Empty, 0); }, "empty generation rejected on encode");
            EncodeRejected(delegate { Record(null, -1); }, "negative revision rejected on encode");
            EncodeRejected(delegate { VaultDailyRecordCodec.Encode(null); }, "null record encode has safe error");
            var data = Data(); var record = Record(data);
            data.version = 2; EncodeRejected(delegate { VaultDailyRecordCodec.Encode(record); }, "mutated dataset version revalidated"); data.version = 1;
            data.entries.Add(data.entries[0]); EncodeRejected(delegate { VaultDailyRecordCodec.Encode(record); }, "mutated duplicate ids revalidated"); data.entries.RemoveAt(1);
            data.entries[0].notes = new string('x', VaultDailyRecordCodec.MaxFieldBytes + 1);
            EncodeRejected(delegate { VaultDailyRecordCodec.Encode(record); }, "field write cap");
            data.entries[0].notes = "\ud800"; EncodeRejected(delegate { VaultDailyRecordCodec.Encode(record); }, "strict UTF8 rejects unpaired source surrogate");
            data.entries[0].notes = new string('\u0430', VaultDailyRecordCodec.MaxFieldBytes / 2);
            var boundary = VaultDailyRecordCodec.Decode(VaultDailyRecordCodec.Encode(record)).Record;
            Check(boundary != null && boundary.State.FakeData.entries[0].notes == data.entries[0].notes, "field cap measures UTF8 bytes and accepts exact boundary");
            data = VaultData.Empty();
            for (int i = 0; i < VaultDailyRecordCodec.MaxEntries; i++) data.entries.Add(new VaultEntry { id = "id" + i });
            record = Record(data);
            Check(VaultDailyRecordCodec.Decode(VaultDailyRecordCodec.Encode(record)).Record.State.FakeData.entries.Count == VaultDailyRecordCodec.MaxEntries, "exact entry count cap accepted");
            data.entries.Add(new VaultEntry { id = "extra" }); EncodeRejected(delegate { VaultDailyRecordCodec.Encode(record); }, "mutated entry count cap");
            data = VaultData.Empty(); var field = new string('x', VaultDailyRecordCodec.MaxFieldBytes);
            for (int i = 0; i < 65; i++) data.entries.Add(new VaultEntry { id = "large" + i, notes = field });
            record = Record(data); EncodeRejected(delegate { VaultDailyRecordCodec.Encode(record); }, "aggregate plaintext cap checked during writes");
            Check(data.entries.Count == 65 && data.entries[0].notes == field, "failed encode preserves caller data");
        }
        private static int Main()
        {
            try {
                RoundTrips(); EnvelopeFailures(); ProtectedFailures(); EncodeBounds();
                Check(AppPaths.Accesses == 0 && SafeLog.Accesses == 0, "codec never touches application paths or logging");
                Console.WriteLine("PASS: VaultDailyRecordTests (" + passed + " checks)"); return 0;
            } catch (Exception) { Console.Error.WriteLine("FAIL: VaultDailyRecordTests after " + passed + " checks: static assertion or native protection failure"); return 1; }
        }
    }
}

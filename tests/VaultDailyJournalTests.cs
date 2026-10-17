using System;
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
    internal static class VaultDailyJournalTests
    {
        private static int passed;
        private const string DataPath = @"C:\Synthetic-Daily\vault.daily.dat";
        private static readonly DateTime Day = new DateTime(2026, 10, 12);
        private static readonly Guid Generation = new Guid("0daf61d4-5c31-4a2f-87c8-650bb8ad9bbc");
        private static readonly Guid OtherGeneration = new Guid("a5f0b278-a957-46ce-aad2-32ee3917c01c");
        private static readonly Guid Operation = new Guid("bf49185c-48bd-428e-a3b5-f51ad9a9e1ed");
        private static readonly byte[] Purpose = Encoding.ASCII.GetBytes("ProGo/VaultDailyJournal/v1");
        private static readonly byte[] Hash = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();
        private static void Check(bool value, string name)
        { if (!value) { Console.Error.WriteLine("FAIL: " + name); throw new Exception(name); } passed++; Console.WriteLine("PASS: " + name); }
        private static byte[] Bytes(Action<BinaryWriter> emit)
        { using (var stream = new MemoryStream()) { using (var writer = new BinaryWriter(stream, new UTF8Encoding(false), true)) emit(writer); return stream.ToArray(); } }
        // Independently specified wire evidence, without calling journal Encode.
        private static void Evidence(BinaryWriter writer, byte kind = 1, long? ticks = null,
            Guid? generation = null, long revision = 0, byte[] hash = null)
        {
            writer.Write(kind); if (kind == 0) return;
            writer.Write(ticks ?? Day.Ticks); writer.Write((generation ?? Generation).ToByteArray());
            writer.Write(revision); writer.Write(hash ?? Hash);
        }
        private static VaultDailyFileStamp Stamp(long revision = 0, DateTime? day = null,
            Guid? generation = null, string path = DataPath, byte[] hash = null)
        {
            var bytes = Bytes(delegate(BinaryWriter w) { Evidence(w, ticks: (day ?? Day).Ticks, generation: generation, revision: revision, hash: hash); });
            try { using (var reader = new BinaryReader(new MemoryStream(bytes, false))) return VaultDailyFileStamp.ReadEvidence(reader, path); }
            finally { Array.Clear(bytes, 0, bytes.Length); }
        }
        private static void Header(BinaryWriter writer, int version = 1, string path = DataPath, Guid? operation = null)
        {
            writer.Write(version); var pathBytes = Encoding.UTF8.GetBytes(path);
            writer.Write(pathBytes.Length); writer.Write(pathBytes); writer.Write((operation ?? Operation).ToByteArray());
        }
        private static byte[] Wrap(byte[] protectedBytes, int version = 1)
        { return Bytes(delegate(BinaryWriter w) { w.Write(Encoding.ASCII.GetBytes("PGODJNL1")); w.Write(version); w.Write(protectedBytes.Length); w.Write(protectedBytes); }); }
        private static byte[] Protected(Action<BinaryWriter> emit, byte[] purpose = null)
        {
            var plain = Bytes(emit); byte[] protectedBytes = null;
            try { protectedBytes = ProtectedData.Protect(plain, purpose ?? Purpose, DataProtectionScope.CurrentUser); return Wrap(protectedBytes); }
            finally { Array.Clear(plain, 0, plain.Length); if (protectedBytes != null) Array.Clear(protectedBytes, 0, protectedBytes.Length); }
        }
        private static void Failed(byte[] bytes, string name, VaultDailyJournalDecodeStatus status = VaultDailyJournalDecodeStatus.InvalidData,
            string expectedPath = DataPath)
        {
            var before = bytes == null ? null : (byte[])bytes.Clone();
            var result = VaultDailyJournalCodec.Decode(bytes, expectedPath);
            Check(result.Status == status && result.Journal == null, name + " status/no journal");
            Check(bytes == null || bytes.SequenceEqual(before), name + " preserves caller bytes");
            Check(result.ErrorMessage == (status == VaultDailyJournalDecodeStatus.UnsupportedFormat ?
                "Формат восстановления суточного хранилища не поддерживается." : VaultDailyJournalCodec.InvalidMessage), name + " static error");
        }
        private static void BadPlain(Action<BinaryWriter> emit, string name)
        { Failed(Protected(emit), name); }
        private static void Rejected(Action action, string name)
        {
            bool rejected = false;
            try { action(); } catch (InvalidDataException ex) { rejected = ex.Message == VaultDailyJournalCodec.InvalidMessage && ex.InnerException == null; }
            Check(rejected, name);
        }
        private static void RoundTrips()
        {
            var missing = VaultDailyFileStamp.Create(DataPath, null, null); var next = Stamp();
            var source = VaultDailyJournal.Create(DataPath, Operation, missing, next);
            var bytes = VaultDailyJournalCodec.Encode(source); var before = (byte[])bytes.Clone();
            var result = VaultDailyJournalCodec.Decode(bytes, DataPath.ToLowerInvariant());
            Check(result.Status == VaultDailyJournalDecodeStatus.Success && result.ErrorMessage == null, "initial journal roundtrip with ordinal case-insensitive scope");
            var journal = result.Journal;
            Check(journal.Operation == Operation && journal.Previous.IsMissing && journal.Previous.Matches(missing) && journal.Next.Matches(next), "initial operation and exact immutable evidence remain");
            Check(journal.DataPath == DataPath.ToLowerInvariant() && bytes.SequenceEqual(before), "decode uses caller canonical scope without changing input");
            Check(journal.StageFileName == "vault.daily.dat.new-bf49185c48bd428ea3b5f51ad9a9e1ed" &&
                journal.BackupFileName == "vault.daily.dat.previous-bf49185c48bd428ea3b5f51ad9a9e1ed", "transaction names are exact derived fixed children");
            Check(source.Previous.Matches(missing) && source.Next.Matches(next) && !Encoding.UTF8.GetString(bytes).Contains(DataPath), "encode preserves source evidence and protects its path");
            var body = bytes.Skip(16).ToArray(); byte[] plain = null;
            try {
                plain = ProtectedData.Unprotect(body, Purpose, DataProtectionScope.CurrentUser);
                using (var reader = new BinaryReader(new MemoryStream(plain, false))) {
                    Check(reader.ReadInt32() == 1 && Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32())) == DataPath &&
                        new Guid(reader.ReadBytes(16)) == Operation, "independent native unprotect observes exact version path and operation");
                    Check(reader.ReadByte() == 0 && reader.ReadByte() == 1 && reader.ReadInt64() == Day.Ticks &&
                        new Guid(reader.ReadBytes(16)) == Generation && reader.ReadInt64() == 0 && reader.ReadBytes(32).SequenceEqual(Hash) &&
                        reader.BaseStream.Position == reader.BaseStream.Length, "independent native unprotect observes exact evidence layout and EOF");
                }
            } finally { Array.Clear(body, 0, body.Length); if (plain != null) Array.Clear(plain, 0, plain.Length); }
            foreach (var revision in new long[] { 0, 23, Int64.MaxValue - 1 }) {
                var previous = Stamp(revision); var successor = Stamp(revision + 1);
                var decoded = VaultDailyJournalCodec.Decode(VaultDailyJournalCodec.Encode(VaultDailyJournal.Create(DataPath, Operation, previous, successor)), DataPath);
                Check(decoded.Status == VaultDailyJournalDecodeStatus.Success && decoded.Journal.Previous.Matches(previous) && decoded.Journal.Next.Matches(successor), "same-day checked revision roundtrip");
            }
            foreach (var offset in new[] { -1, 1 }) {
                var previous = Stamp(Int64.MaxValue); var successor = Stamp(day: Day.AddDays(offset), generation: OtherGeneration);
                var decoded = VaultDailyJournalCodec.Decode(VaultDailyJournalCodec.Encode(VaultDailyJournal.Create(DataPath, Operation, previous, successor)), DataPath);
                Check(decoded.Status == VaultDailyJournalDecodeStatus.Success && decoded.Journal.Next.Matches(successor), "day rotation in either direction with exhausted old revision");
            }
            var independent = Protected(delegate(BinaryWriter w) { Header(w); Evidence(w, kind: 0); Evidence(w); });
            var independentResult = VaultDailyJournalCodec.Decode(independent, DataPath);
            Check(independentResult.Status == VaultDailyJournalDecodeStatus.Success && independentResult.Journal.Next.Matches(next), "independently protected binary fixture preserves exact digest");
            var hash = (byte[])Hash.Clone(); var detached = Stamp(hash: hash); Array.Clear(hash, 0, hash.Length);
            Check(detached.Matches(next), "evidence parser privately owns digest after caller buffer clearing");
            var differentHash = (byte[])Hash.Clone(); differentHash[31] ^= 1;
            Check(!journal.Next.Matches(Stamp(hash: differentHash)), "decoded evidence retains all digest bytes");
            var record = new VaultDailyRecord(VaultDailyState.Empty(Day), Generation, 0);
            var envelope = VaultDailyRecordCodec.Encode(record);
            var actualStamp = VaultDailyFileStamp.Create(DataPath, record, envelope);
            var actual = VaultDailyJournalCodec.Decode(VaultDailyJournalCodec.Encode(VaultDailyJournal.Create(DataPath, Operation, missing, actualStamp)), DataPath);
            Check(actual.Status == VaultDailyJournalDecodeStatus.Success && actual.Journal.Next.Matches(actualStamp), "journal binds actual protected-record ciphertext evidence");
            Array.Clear(envelope, 0, envelope.Length);
        }
        private static void EnvelopeFailures()
        {
            Failed(null, "null journal"); Failed(new byte[0], "zero legacy marker"); Failed(new byte[16], "wrong magic");
            var encoded = Protected(delegate(BinaryWriter w) { Header(w); Evidence(w, kind: 0); Evidence(w); });
            for (int i = 0; i < 16; i++) Failed(encoded.Take(i).ToArray(), "truncated outer header " + i);
            var changed = (byte[])encoded.Clone(); changed[7] ^= 1; Failed(changed, "changed magic");
            Failed(encoded.Concat(new byte[] { 0 }).ToArray(), "outer trailing byte");
            Failed(encoded.Take(encoded.Length - 1).ToArray(), "truncated protected body");
            foreach (int size in new[] { -1, 0, Int32.MaxValue, encoded.Length }) {
                changed = (byte[])encoded.Clone(); Buffer.BlockCopy(BitConverter.GetBytes(size), 0, changed, 12, 4);
                Failed(changed, "invalid protected length " + size);
            }
            changed = (byte[])encoded.Clone(); Buffer.BlockCopy(BitConverter.GetBytes(2), 0, changed, 8, 4);
            Failed(changed, "unsupported outer version", VaultDailyJournalDecodeStatus.UnsupportedFormat);
            Failed(changed.Concat(new byte[] { 0 }).ToArray(), "unsupported version still requires exact outer framing");
            changed = (byte[])encoded.Clone(); changed[changed.Length - 1] ^= 1; Failed(changed, "native DPAPI tamper");
            Failed(Protected(delegate(BinaryWriter w) { Header(w); Evidence(w, kind: 0); Evidence(w); }, new byte[] { 1, 2, 3 }), "DPAPI purpose mismatch");
            Failed(new byte[VaultDailyJournalCodec.MaxEnvelopeBytes + 1], "envelope cap");
            Failed(encoded, "foreign expected scope", expectedPath: @"C:\Different\vault.daily.dat");
            Failed(encoded, "null expected scope", expectedPath: null);
            Failed(encoded, "invalid expected UTF8", expectedPath: "\ud800");
        }
        private static void ProtectedFailures()
        {
            Failed(Protected(delegate(BinaryWriter w) { Header(w, version: 2); Evidence(w, kind: 0); Evidence(w); }),
                "unsupported protected version", VaultDailyJournalDecodeStatus.UnsupportedFormat);
            BadPlain(delegate(BinaryWriter w) { w.Write(new byte[3]); }, "short plaintext");
            foreach (int length in new[] { -1, 0, 4097, Int32.MaxValue, 500 })
                BadPlain(delegate(BinaryWriter w) { w.Write(1); w.Write(length); w.Write(new byte[] { 65 }); }, "invalid or incomplete path length " + length);
            BadPlain(delegate(BinaryWriter w) { w.Write(1); w.Write(2); w.Write(new byte[] { 0xc3, 0x28 }); w.Write(Operation.ToByteArray()); Evidence(w, kind: 0); Evidence(w); }, "strict UTF8 path");
            BadPlain(delegate(BinaryWriter w) { Header(w, path: @"C:\Other\vault.daily.dat"); Evidence(w, kind: 0); Evidence(w); }, "protected foreign scope");
            BadPlain(delegate(BinaryWriter w) { Header(w, operation: Guid.Empty); Evidence(w, kind: 0); Evidence(w); }, "empty operation");
            BadPlain(delegate(BinaryWriter w) { Header(w); Evidence(w, kind: 2); Evidence(w); }, "unknown previous evidence kind");
            BadPlain(delegate(BinaryWriter w) { Header(w); Evidence(w, kind: 0); Evidence(w, kind: 2); }, "unknown next evidence kind");
            BadPlain(delegate(BinaryWriter w) { Header(w); Evidence(w, kind: 0); Evidence(w, kind: 0); }, "missing next evidence");
            foreach (long ticks in new[] { -1L, Int64.MaxValue, Day.AddTicks(1).Ticks })
                BadPlain(delegate(BinaryWriter w) { Header(w); Evidence(w, kind: 0); Evidence(w, ticks: ticks); }, "invalid next day " + ticks);
            BadPlain(delegate(BinaryWriter w) { Header(w); Evidence(w, kind: 0); Evidence(w, generation: Guid.Empty); }, "empty next generation");
            BadPlain(delegate(BinaryWriter w) { Header(w); Evidence(w, kind: 0); Evidence(w, revision: -1); }, "negative next revision");
            BadPlain(delegate(BinaryWriter w) { Header(w); Evidence(w, kind: 0); Evidence(w, hash: new byte[31]); }, "short next digest");
            BadPlain(delegate(BinaryWriter w) { Header(w); Evidence(w, kind: 0); Evidence(w); w.Write((byte)0); }, "inner trailing byte");
            BadPlain(delegate(BinaryWriter w) { w.Write(new byte[VaultDailyJournalCodec.MaxPlainBytes + 1]); }, "protected plaintext cap");
            BadPlain(delegate(BinaryWriter w) { Header(w); Evidence(w, kind: 0); Evidence(w, revision: 1); }, "initial revision must be zero");
            BadPlain(delegate(BinaryWriter w) { Header(w); Evidence(w); Evidence(w, revision: 2); }, "same-day revision cannot skip");
            BadPlain(delegate(BinaryWriter w) { Header(w); Evidence(w); Evidence(w, generation: OtherGeneration, revision: 1); }, "same-day generation cannot rotate");
            BadPlain(delegate(BinaryWriter w) { Header(w); Evidence(w, revision: Int64.MaxValue); Evidence(w, revision: Int64.MaxValue); }, "same-day exhausted revision cannot wrap");
            BadPlain(delegate(BinaryWriter w) { Header(w); Evidence(w); Evidence(w, ticks: Day.AddDays(1).Ticks); }, "new day requires new generation");
            BadPlain(delegate(BinaryWriter w) { Header(w); Evidence(w); Evidence(w, ticks: Day.AddDays(1).Ticks, generation: OtherGeneration, revision: 1); }, "new-day revision must be zero");
        }
        private static void FactoryFailuresAndBounds()
        {
            var missing = VaultDailyFileStamp.Create(DataPath, null, null); var next = Stamp();
            Rejected(delegate { VaultDailyJournalCodec.Encode(null); }, "null encode has static error");
            Rejected(delegate { VaultDailyJournal.Create(null, Operation, missing, next); }, "null factory scope");
            Rejected(delegate { VaultDailyJournal.Create(DataPath, Guid.Empty, missing, next); }, "empty factory operation");
            Rejected(delegate { VaultDailyJournal.Create(DataPath, Operation, null, next); }, "null previous stamp");
            Rejected(delegate { VaultDailyJournal.Create(DataPath, Operation, missing, null); }, "null next stamp");
            Rejected(delegate { VaultDailyJournal.Create(DataPath, Operation, missing, missing); }, "factory refuses missing next");
            Rejected(delegate { VaultDailyJournal.Create(DataPath, Operation, Stamp(path: @"C:\Other\vault.daily.dat"), Stamp(1)); }, "foreign previous factory scope");
            Rejected(delegate { VaultDailyJournal.Create(DataPath, Operation, missing, Stamp(path: @"C:\Other\vault.daily.dat")); }, "foreign next factory scope");
            Rejected(delegate { VaultDailyJournal.Create("\ud800", Operation, missing, next); }, "invalid source UTF8 has static error");
            var boundaryPath = new string('\u0430', VaultDailyJournalCodec.MaxPathBytes / 2);
            var boundary = VaultDailyJournal.Create(boundaryPath, Operation, VaultDailyFileStamp.Create(boundaryPath, null, null), Stamp(path: boundaryPath));
            Check(VaultDailyJournalCodec.Decode(VaultDailyJournalCodec.Encode(boundary), boundaryPath).Status == VaultDailyJournalDecodeStatus.Success,
                "exact UTF8 path byte cap accepted without filesystem access");
            var tooLong = boundaryPath + "x";
            Rejected(delegate { VaultDailyJournal.Create(tooLong, Operation, VaultDailyFileStamp.Create(tooLong, null, null), Stamp(path: tooLong)); }, "factory UTF8 path cap");
        }
        private static int Main()
        {
            try {
                RoundTrips(); EnvelopeFailures(); ProtectedFailures(); FactoryFailuresAndBounds();
                Check(AppPaths.Accesses == 0 && SafeLog.Accesses == 0, "journal codec never touches application paths or logging");
                Console.WriteLine("PASS: VaultDailyJournalTests (" + passed + " checks)"); return 0;
            } catch (Exception) { Console.Error.WriteLine("FAIL: VaultDailyJournalTests after " + passed + " checks: static assertion or native protection failure"); return 1; }
        }
    }
}

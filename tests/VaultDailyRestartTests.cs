using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace ProGo
{
    internal static class AppPaths
    {
        internal static int Accesses;
        public static string VaultPath { get { Accesses++; throw new InvalidOperationException("Path guard"); } }
        public static void EnsureDirectories() { Accesses++; throw new InvalidOperationException("Path guard"); }
    }
    internal static class SafeLog
    {
        internal static int Accesses;
        public static void Info(string message) { Accesses++; throw new InvalidOperationException("Log guard"); }
    }

    // Independent processes use the production CurrentUser DPAPI codec and explicit paths.
    // PINs enter through stdin; expected DTOs live only in an owned, separately protected proof.
    internal static class VaultDailyRestartTests
    {
        private const string ProofName = "restart-fixture.proof";
        private const string ChildPrefix = "RESTART-STAGE-PASS:";
        private const string ChildFailurePrefix = "RESTART-STAGE-FAIL:";
        private static readonly byte[] ProofPurpose = Encoding.ASCII.GetBytes("ProGo/Test/VaultDailyRestart/v1");
        private static readonly DateTime Day = new DateTime(2026, 10, 13, 12, 0, 0, DateTimeKind.Unspecified);
        private static readonly CancellationToken Token = CancellationToken.None;
        private static int passed;
        private static bool child;
        private static bool unsettledChild;
        private static string failedCheck;
        private static int childPhase;
        private static int childExpiryStatus = -1;
        private static bool childInputHadBom;

        public sealed class Request
        {
            public string Proper { get; set; }
            public string First { get; set; }
            public string Second { get; set; }
        }
        public sealed class Proof
        {
            public string Sid { get; set; }
            public int InitialProcess { get; set; }
            public VaultData Real { get; set; }
            public VaultData Fake { get; set; }
            public string CurrentPin { get; set; }
            public string RealCipherHash { get; set; }
            public List<string> PastIds { get; set; }
            public List<string> PastSecrets { get; set; }
        }

        private static JavaScriptSerializer Json()
        { return new JavaScriptSerializer { MaxJsonLength = 1024 * 1024, RecursionLimit = 32 }; }
        private static void Check(bool value, string name)
        {
            if (!value) { failedCheck = name; throw new InvalidOperationException("Restart assertion failed"); }
            passed++;
            if (!child) Console.WriteLine("PASS: " + name);
        }
        private static string RealPath(string root) { return Path.Combine(root, "vault.enc.json"); }
        private static string DailyPath(string root) { return Path.Combine(root, VaultDailyFile.FileName); }
        private static VaultData Snapshot(VaultData data)
        { return new VaultData { version = data.version, entries = data.entries.Select(e => e.Clone()).ToList() }; }
        private static bool Same(VaultData first, VaultData second)
        { return first != null && second != null && String.Equals(Json().Serialize(first), Json().Serialize(second), StringComparison.Ordinal); }
        private static string Hash(byte[] bytes)
        { using (var sha = SHA256.Create()) return Convert.ToBase64String(sha.ComputeHash(bytes)); }
        private static string Sid()
        { using (var identity = WindowsIdentity.GetCurrent()) return identity.User.Value; }
        private static string ReadLimited(TextReader reader, int limit)
        {
            var text = new StringBuilder(); int value;
            while ((value = reader.Read()) >= 0) {
                if (text.Length >= limit) throw new InvalidDataException("Fixture input too large");
                text.Append((char)value);
            }
            return text.ToString();
        }
        private static Request ReadRequest()
        {
            childPhase = 4;
            // Framework Console.In uses the child's console codepage and does not detect
            // a transport BOM. A headless child must decode the private pipe explicitly.
            byte[] bytes = null, buffer = new byte[256];
            try {
                using (var stream = Console.OpenStandardInput())
                using (var input = new MemoryStream()) {
                    int count;
                    while ((count = stream.Read(buffer, 0, buffer.Length)) != 0) {
                        if (input.Length + count > 4096) throw new InvalidDataException("Fixture input too large");
                        input.Write(buffer, 0, count);
                    }
                    bytes = input.ToArray();
                }
                childInputHadBom = bytes.Length >= 3 && bytes[0] == 0xef && bytes[1] == 0xbb && bytes[2] == 0xbf;
                int offset = childInputHadBom ? 3 : 0;
                string text;
                using (var reader = new StreamReader(new MemoryStream(bytes, offset, bytes.Length - offset, false), new UTF8Encoding(false, true), false, 1024))
                    text = ReadLimited(reader, 1024);
                childPhase = 5;
                return Json().Deserialize<Request>(text);
            } finally {
                Array.Clear(buffer, 0, buffer.Length);
                if (bytes != null) Array.Clear(bytes, 0, bytes.Length);
            }
        }
        private static int ExceptionCode(Exception error)
        {
            if (error is DecoderFallbackException) return 8;
            if (error is ArgumentException) return 1;
            if (error is InvalidOperationException) return 2;
            if (error is InvalidDataException) return 3;
            if (error is IOException) return 4;
            if (error is System.Security.SecurityException) return 5;
            if (error is UnauthorizedAccessException) return 6;
            if (error is FormatException) return 7;
            if (error is CryptographicException) return 9;
            if (error is OutOfMemoryException) return 10;
            return 0;
        }
        private static void PrintChildFailure(string action, string frame)
        {
            var parts = frame.StartsWith(ChildFailurePrefix, StringComparison.Ordinal)
                ? frame.Substring(ChildFailurePrefix.Length).Split(':') : new string[0];
            var numbers = new int[5]; bool valid = parts.Length == numbers.Length;
            for (int i = 0; valid && i < numbers.Length; i++)
                valid = Int32.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out numbers[i]);
            valid = valid && numbers[0] >= 0 && numbers[0] <= 99 && numbers[1] >= 0 && numbers[1] <= 500 &&
                numbers[2] >= 0 && numbers[2] <= 10 && (numbers[3] == -1 || Enum.IsDefined(typeof(VaultDailyExpiryStatus), numbers[3])) &&
                (numbers[4] == 0 || numbers[4] == 1);
            if (!valid) { Console.Error.WriteLine("RESTART-CHILD-FAIL: " + action + " no safe diagnostic frame"); return; }
            var types = new[] { "Unknown", "Argument", "InvalidOperation", "InvalidData", "IO", "Security", "Access", "Format", "Decoder", "Cryptographic", "Memory" };
            Console.Error.WriteLine("RESTART-CHILD-FAIL: " + action + " phase=" + numbers[0] + " checks=" + numbers[1] +
                " type=" + types[numbers[2]] + " status=" + (numbers[3] == -1 ? "none" : Enum.GetName(typeof(VaultDailyExpiryStatus), numbers[3])) +
                " leadingUtf8Bom=" + numbers[4]);
        }
        private static void WriteProof(string root, Proof proof)
        {
            byte[] plain = null, encrypted = null;
            try {
                plain = new UTF8Encoding(false, true).GetBytes(Json().Serialize(proof));
                encrypted = ProtectedData.Protect(plain, ProofPurpose, DataProtectionScope.CurrentUser);
                using (var file = new FileStream(Path.Combine(root, ProofName), FileMode.Create, FileAccess.Write, FileShare.None)) {
                    file.Write(encrypted, 0, encrypted.Length); file.Flush(true);
                }
            } finally {
                if (plain != null) Array.Clear(plain, 0, plain.Length);
                if (encrypted != null) Array.Clear(encrypted, 0, encrypted.Length);
            }
        }
        private static Proof ReadProof(string root)
        {
            byte[] encrypted = null, plain = null;
            try {
                using (var file = new FileStream(Path.Combine(root, ProofName), FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    if (file.Length <= 0 || file.Length > 1024 * 1024) throw new InvalidDataException("Fixture proof size");
                    encrypted = new byte[(int)file.Length]; int offset = 0;
                    while (offset < encrypted.Length) {
                        int count = file.Read(encrypted, offset, encrypted.Length - offset);
                        if (count == 0) throw new InvalidDataException("Fixture proof truncated");
                        offset += count;
                    }
                }
                plain = ProtectedData.Unprotect(encrypted, ProofPurpose, DataProtectionScope.CurrentUser);
                var proof = Json().Deserialize<Proof>(new UTF8Encoding(false, true).GetString(plain));
                Check(proof != null && proof.Sid == Sid(), "restart uses the original Windows SID and native protected proof");
                Check(proof.InitialProcess != Process.GetCurrentProcess().Id, "reopen runs in a distinct native process");
                return proof;
            } finally {
                if (encrypted != null) Array.Clear(encrypted, 0, encrypted.Length);
                if (plain != null) Array.Clear(plain, 0, plain.Length);
            }
        }
        private static VaultOpenResult Open(string root, string pin, DateTime now, Func<VaultData> factory)
        { return VaultOpenService.Open(RealPath(root), root, pin, () => now, factory, Token); }
        private static VaultSession Opened(VaultOpenResult result, bool real, string name)
        {
            Check(result.Status == VaultOpenStatus.Success && result.Session != null && result.Session.CanPersist == real && result.ErrorMessage == null, name);
            return result.Session;
        }
        private static void Wrong(VaultOpenResult result)
        { Check(result.Status == VaultOpenStatus.WrongPin && result.Session == null && result.ErrorMessage == "Неверный пин-код", "another mismatch returns the exact approved error without a session"); }
        private static void RealUnchanged(string root, Proof proof)
        { Check(proof.RealCipherHash == Hash(File.ReadAllBytes(RealPath(root))), "daily transitions preserve the exact real ciphertext"); }
        private static void Settled(string root)
        {
            Check(!File.Exists(DailyPath(root) + ".recovery") && !File.Exists(DailyPath(root) + ".recovery.previous"), "production transaction leaves no recovery witness");
            Check(Directory.GetFiles(root).All(p => Path.GetFileName(p) == "vault.enc.json" ||
                Path.GetFileName(p) == VaultDailyFile.FileName || Path.GetFileName(p) == ProofName), "owned fixture has no transaction staging leftovers");
        }
        private static void AddPast(Proof proof, VaultData data)
        {
            proof.PastIds.AddRange(data.entries.Select(e => e.id));
            proof.PastSecrets.AddRange(data.entries.Select(e => e.secret));
        }
        private static void Save(VaultSession session, VaultData data)
        {
            var candidate = session.CopyWithData(Snapshot(data));
            try { VaultService.Save(candidate); }
            finally { candidate.DropSensitive(); }
        }

        private static void Missing(string root, Request input, DateTime now)
        {
            childPhase = 20;
            var result = VaultDailyExpiry.Run(root, () => now, Token);
            childExpiryStatus = (int)result.Status; childPhase = 21;
            Check(result.Status == VaultDailyExpiryStatus.Missing && result.Stamp != null && result.Stamp.IsMissing &&
                !result.HasFakeBinding && result.ErrorMessage == null, "startup missing daily state is a safe unbound result");
            Check(Directory.GetFiles(root).Length == 0, "startup missing state creates no file or directory");
            childPhase = 22;
            var created = Opened(VaultOpenService.Create(RealPath(root), input.Proper, Token), true, "owned missing fixture creates real storage");
            created.DropSensitive();
            childPhase = 23; result = VaultDailyExpiry.Run(root, () => now, Token); childExpiryStatus = (int)result.Status;
            Check(result.Status == VaultDailyExpiryStatus.Missing &&
                !File.Exists(DailyPath(root)), "a real vault alone does not create a daily tombstone");
        }
        private static void Seed(string root, Request input, DateTime now)
        {
            var real = Opened(VaultOpenService.Create(RealPath(root), input.Proper, Token), true, "first process creates the real vault");
            VaultSession fake = null;
            try {
                var realData = VaultFakeDataFactory.Create(now);
                foreach (var entry in realData.entries) { entry.name = "Owned real fixture"; entry.notes = "Real fixture metadata"; }
                Save(real, realData);
                int factories = 0; VaultData originalFake = null;
                fake = Opened(Open(root, input.First, now, delegate {
                    factories++; originalFake = VaultFakeDataFactory.Create(now); return originalFake;
                }), false, "first process commits the first mismatch binding");
                Check(factories == 1 && fake.Persistence != null && fake.StoragePath == null, "first binding uses the production lease and one independent factory");
                var data = Snapshot(fake.Data); var added = VaultFakeDataFactory.Create(now).entries[0].Clone();
                added.name = "Owned added fixture"; data.entries.Add(added); Save(fake, data);
                Check(VaultDailyFile.Read(root).Record.State.FakeData.entries.Count == originalFake.entries.Count + 1, "first process durably saves an added entry");
                data.entries[0].name = "Owned edited fixture"; data.entries[0].login = "edited fixture login";
                data.entries[0].notes = null; data.entries[0].tags = ""; Save(fake, data);
                Check(VaultDailyFile.Read(root).Record.State.FakeData.entries[0].name == data.entries[0].name, "first process durably saves edited fields");
                var removed = data.entries[1].id; data.entries.RemoveAt(1); Save(fake, data);
                var stored = VaultDailyFile.Read(root);
                Check(stored.Status == VaultDailyFileReadStatus.Success && stored.Record.State.FakePin == input.First &&
                    Same(stored.Record.State.FakeData, data) && !stored.Record.State.FakeData.entries.Any(e => e.id == removed), "first process durably saves deletion and exact DTOs");
                Check(stored.Record.Revision == 3, "three native CRUD writes advance the accepted binding revision");
                var proof = new Proof { Sid = Sid(), InitialProcess = Process.GetCurrentProcess().Id,
                    Real = Snapshot(realData), Fake = Snapshot(data), CurrentPin = input.First,
                    RealCipherHash = Hash(File.ReadAllBytes(RealPath(root))), PastIds = new List<string>(), PastSecrets = new List<string>() };
                AddPast(proof, originalFake); AddPast(proof, data); WriteProof(root, proof);
                Settled(root);
            } finally { real.DropSensitive(); if (fake != null) fake.DropSensitive(); }
        }
        private static void Reopen(string root, Request input, DateTime now, bool expectEmpty)
        {
            var proof = ReadProof(root); var before = VaultDailyFile.Read(root); int factories = 0;
            Func<VaultData> forbidden = delegate { factories++; throw new InvalidOperationException("Factory guard"); };
            var startup = VaultDailyExpiry.Run(root, () => now, Token);
            Check(startup.Status == VaultDailyExpiryStatus.Unchanged && startup.HasFakeBinding && startup.ErrorMessage == null &&
                before.Stamp.Matches(startup.Stamp), "same-day startup preserves the exact accepted binding");
            var fake = Opened(Open(root, proof.CurrentPin, now, forbidden), false, "new process opens the previously accepted mismatch");
            VaultSession real = null;
            try {
                Check(Same(fake.Data, proof.Fake), "new process reloads every ID field secret and null-versus-empty value exactly");
                Check(fake.Data.entries.Count == (expectEmpty ? 0 : proof.Fake.entries.Count), "reopened record count matches the persisted fixture");
                Check(fake.IsCurrent(now) && (expectEmpty || fake.IsCurrent(now.Date.AddDays(1).AddTicks(-1))), "accepted session remains current through its last local-day tick");
                Wrong(Open(root, proof.CurrentPin == input.First ? input.Second : input.First, now, forbidden));
                real = Opened(VaultOpenService.Open(RealPath(root), "invalid auxiliary root", input.Proper,
                    delegate { throw new InvalidOperationException("Clock guard"); }, forbidden, Token), true, "correct PIN opens independently of auxiliary path clock and factory");
                Check(Same(real.Data, proof.Real) && factories == 0, "correct and bound opens retain exact data without regeneration");
                Check(before.Stamp.Matches(VaultDailyFile.Read(root).Stamp), "reopening either route does not mutate the daily record");
                RealUnchanged(root, proof); Settled(root);
            } finally { fake.DropSensitive(); if (real != null) real.DropSensitive(); }
        }
        private static void CancelExpiry(string root, DateTime now)
        {
            var proof = ReadProof(root); var before = File.ReadAllBytes(DailyPath(root));
            using (var cancelled = new CancellationTokenSource()) {
                cancelled.Cancel();
                var result = VaultDailyExpiry.Run(root, () => now, cancelled.Token);
                Check(result.Status == VaultDailyExpiryStatus.Cancelled && result.Stamp == null && !result.HasFakeBinding &&
                    !String.IsNullOrEmpty(result.ErrorMessage), "pre-cancelled startup exposes no safe expiration result");
            }
            Check(before.SequenceEqual(File.ReadAllBytes(DailyPath(root))) && VaultDailyFile.Read(root).Record.State.HasFake,
                "pre-cancelled cleanup preserves the committed old binding and exact bytes");
            RealUnchanged(root, proof); Settled(root);
        }
        private static void Rotate(string root, Request input, DateTime now, bool backwards)
        {
            var proof = ReadProof(root); var before = VaultDailyFile.Read(root); int factories = 0;
            Check(before.Record.State.LocalDay != now.Date && before.Record.State.HasFake, "rotation starts from a committed binding on another local day");
            var result = VaultDailyExpiry.Run(root, () => now, Token); var cleared = VaultDailyFile.Read(root);
            Check(result.Status == VaultDailyExpiryStatus.Expired && !result.HasFakeBinding && result.ErrorMessage == null &&
                result.Stamp != null && result.Stamp.Matches(cleared.Stamp), "new process startup commits expiration before accepting any mismatch");
            Check(cleared.Status == VaultDailyFileReadStatus.Success && !cleared.Record.State.HasFake &&
                cleared.Record.State.FakePin == null && cleared.Record.State.FakeData == null && cleared.Record.State.LocalDay == now.Date,
                "expired native record contains no previous PIN or dataset");
            Check(cleared.Record.Generation != before.Record.Generation && cleared.Record.Revision == 0,
                "expiration replaces the generation with an unbound baseline");
            var real = Opened(Open(root, input.Proper, now, delegate { factories++; throw new InvalidOperationException("Factory guard"); }),
                true, "correct PIN still opens immediately after expiration");
            try { Check(Same(real.Data, proof.Real) && factories == 0 && cleared.Stamp.Matches(VaultDailyFile.Read(root).Stamp),
                "real authentication consumes no new daily mismatch"); } finally { real.DropSensitive(); }
            var pin = backwards ? input.First : input.Second;
            var fake = Opened(Open(root, pin, now, delegate { factories++; return VaultFakeDataFactory.Create(now); }), false,
                "the first mismatch after expiration obtains one fresh dataset");
            try {
                Check(factories == 1 && fake.Data.entries.Count > 0, "fresh post-expiration binding invokes the factory exactly once");
                Check(fake.Data.entries.All(e => !proof.PastIds.Contains(e.id) && !proof.PastSecrets.Contains(e.secret)),
                    "fresh generation reuses none of the previous IDs or secrets");
                var accepted = VaultDailyFile.Read(root);
                Check(accepted.Record.State.LocalDay == now.Date && accepted.Record.State.FakePin == pin && Same(accepted.Record.State.FakeData, fake.Data),
                    "fresh PIN and data are committed to the actual local day");
                Wrong(Open(root, pin == input.First ? input.Second : input.First, now, delegate { factories++; return VaultData.Empty(); }));
                Check(factories == 1, "another mismatch cannot regenerate or replace the fresh binding");
                proof.CurrentPin = pin; proof.Fake = Snapshot(fake.Data); AddPast(proof, fake.Data); WriteProof(root, proof);
                RealUnchanged(root, proof); Settled(root);
            } finally { fake.DropSensitive(); }
        }
        private static void Empty(string root, Request input, DateTime now)
        {
            var proof = ReadProof(root); int factories = 0;
            var fake = Opened(Open(root, proof.CurrentPin, now, delegate { factories++; return VaultData.Empty(); }), false,
                "emptying process opens the accepted binding without factory use");
            try {
                Save(fake, VaultData.Empty()); var stored = VaultDailyFile.Read(root);
                Check(factories == 0 && stored.Record.State.HasFake && stored.Record.State.FakePin == proof.CurrentPin &&
                    stored.Record.State.FakeData.entries.Count == 0, "deleting all records preserves a bound empty dataset rather than resetting PIN");
                Wrong(Open(root, proof.CurrentPin == input.First ? input.Second : input.First, now, delegate { factories++; return VaultData.Empty(); }));
                Check(factories == 0, "empty bound data never permits a second fake PIN");
                proof.Fake = VaultData.Empty(); WriteProof(root, proof); RealUnchanged(root, proof); Settled(root);
            } finally { fake.DropSensitive(); }
        }
        private static void Unavailable(string root, Request input, DateTime now)
        {
            var proof = ReadProof(root); var original = File.ReadAllBytes(DailyPath(root)); int factories = 0;
            using (var locked = new FileStream(DailyPath(root), FileMode.Open, FileAccess.Read, FileShare.None)) {
                var expiry = VaultDailyExpiry.Run(root, () => now.AddDays(1), Token);
                Check(expiry.Status == VaultDailyExpiryStatus.FileUnavailable && expiry.Stamp == null && !expiry.HasFakeBinding,
                    "native daily read lock exposes no successful cleanup or adoption");
                var fake = Open(root, proof.CurrentPin, now, delegate { factories++; return VaultData.Empty(); });
                Check(fake.Status == VaultOpenStatus.FileUnavailable && fake.Session == null && factories == 0,
                    "native daily read lock prevents fake regeneration and opening");
                var real = Opened(Open(root, input.Proper, now, delegate { factories++; return VaultData.Empty(); }), true,
                    "daily read lock leaves correct PIN authentication available");
                try { Check(Same(real.Data, proof.Real) && factories == 0, "locked auxiliary data never contaminates the authenticated real dataset"); }
                finally { real.DropSensitive(); }
            }
            Check(original.SequenceEqual(File.ReadAllBytes(DailyPath(root))), "failed locked cleanup preserves the actual daily bytes");
            RealUnchanged(root, proof); Settled(root);
        }
        private static void Corrupt(string root, Request input, DateTime now)
        {
            var proof = ReadProof(root); var original = File.ReadAllBytes(DailyPath(root));
            var damaged = (byte[])original.Clone(); damaged[damaged.Length - 1] ^= 1; int factories = 0;
            try {
                File.WriteAllBytes(DailyPath(root), damaged);
                var expiry = VaultDailyExpiry.Run(root, () => now.AddDays(1), Token);
                Check(expiry.Status == VaultDailyExpiryStatus.InvalidData && expiry.Stamp == null && !expiry.HasFakeBinding,
                    "actual DPAPI corruption is an error rather than an unbound startup state");
                var fake = Open(root, proof.CurrentPin, now, delegate { factories++; return VaultData.Empty(); });
                Check(fake.Status == VaultOpenStatus.InvalidData && fake.Session == null && factories == 0,
                    "corrupt protected daily data cannot create or replace a fake binding");
                var real = Opened(Open(root, input.Proper, now, delegate { factories++; return VaultData.Empty(); }), true,
                    "corrupt auxiliary DPAPI data leaves correct PIN authentication available");
                try { Check(Same(real.Data, proof.Real) && factories == 0, "corrupt auxiliary data never contaminates the authenticated real dataset"); }
                finally { real.DropSensitive(); }
                Check(damaged.SequenceEqual(File.ReadAllBytes(DailyPath(root))), "failed corrupt cleanup retains exact evidence without blind deletion");
                RealUnchanged(root, proof);
            } finally { File.WriteAllBytes(DailyPath(root), original); Array.Clear(original, 0, original.Length); Array.Clear(damaged, 0, damaged.Length); }
            Settled(root);
        }

        private static int ChildMain(string[] args)
        {
            child = true; childPhase = 1;
            if (args.Length != 4 || args[0] != "--child") throw new ArgumentException("Child framing");
            string action = args[1], root = args[2]; DateTime now;
            childPhase = 2;
            if (!DateTime.TryParseExact(args[3], "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out now))
                throw new ArgumentException("Child clock framing");
            childPhase = 3; VaultDailyFile.CheckedPath(root);
            var input = ReadRequest(); childPhase = 6;
            Check(input != null && VaultDailyPolicy.IsValidPin(input.Proper) && VaultDailyPolicy.IsValidPin(input.First) &&
                VaultDailyPolicy.IsValidPin(input.Second) && input.Proper != input.First && input.Proper != input.Second && input.First != input.Second,
                "child stdin carries three valid distinct fixture PINs");
            childPhase = 7;
            switch (action) {
                case "missing": Missing(root, input, now); break;
                case "seed": Seed(root, input, now); break;
                case "reopen": Reopen(root, input, now, false); break;
                case "cancel": CancelExpiry(root, now); break;
                case "forward": Rotate(root, input, now, false); break;
                case "empty": Empty(root, input, now); break;
                case "empty-reopen": Reopen(root, input, now, true); break;
                case "backward": Rotate(root, input, now, true); break;
                case "locked": Unavailable(root, input, now); break;
                case "corrupt": Corrupt(root, input, now); break;
                default: throw new ArgumentException("Child action");
            }
            childPhase = 8;
            Check(AppPaths.Accesses == 0 && SafeLog.Accesses == 0, "child operations use no application paths or logs");
            Console.WriteLine(ChildPrefix + passed.ToString(CultureInfo.InvariantCulture) + ":" + (childInputHadBom ? "1" : "0")); return 0;
        }
        private static void RunChild(string action, string root, DateTime now, Request input)
        {
            var output = new StringBuilder(); var errors = new StringBuilder(); bool overflow = false;
            // EOF callbacks can arrive after a failed process wait. These owned completion
            // sources stay safe for late callbacks and have no disposable wait-handle race.
            var stdoutEnd = new TaskCompletionSource<bool>();
            var stderrEnd = new TaskCompletionSource<bool>();
            using (var process = new Process()) {
                process.StartInfo = new ProcessStartInfo {
                    FileName = typeof(VaultDailyRestartTests).Assembly.Location,
                    Arguments = "--child " + action + " \"" + root + "\" " + now.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
                    UseShellExecute = false, CreateNoWindow = true,
                    RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true
                };
                process.OutputDataReceived += delegate(object sender, DataReceivedEventArgs e) {
                    if (e.Data == null) { stdoutEnd.TrySetResult(true); return; }
                    lock (output) { if (output.Length + e.Data.Length > 4096) overflow = true; else output.AppendLine(e.Data); }
                };
                process.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs e) {
                    if (e.Data == null) { stderrEnd.TrySetResult(true); return; }
                    lock (errors) { if (errors.Length + e.Data.Length > 4096) overflow = true; else errors.AppendLine(e.Data); }
                };
                bool started = false;
                try {
                    started = process.Start(); Check(started, "native restart child " + action + " starts");
                    unsettledChild = true;
                    process.BeginOutputReadLine(); process.BeginErrorReadLine();
                    var payload = Json().Serialize(input);
                    Check(payload.All(c => c <= 127) && process.StandardInput.Encoding.GetBytes(payload).SequenceEqual(new UTF8Encoding(false, true).GetBytes(payload)),
                        "private fixture input has identical ASCII and UTF8 transport bytes");
                    var preamble = process.StandardInput.Encoding.GetPreamble();
                    Check(preamble.Length == 0 || preamble.SequenceEqual(new byte[] { 0xef, 0xbb, 0xbf }),
                        "private fixture input permits only no preamble or the recognized UTF8 preamble");
                    if (action == "missing") Console.WriteLine("RESTART-TRANSPORT: writerPreambleBytes=" + preamble.Length);
                    process.StandardInput.Write(payload); process.StandardInput.Close();
                    var timer = Stopwatch.StartNew();
                    while (!process.WaitForExit(50) && timer.ElapsedMilliseconds < 45000) { }
                    Check(process.HasExited, "native restart child " + action + " settles within its deadline");
                    unsettledChild = false;
                    Check(stdoutEnd.Task.Wait(2000) && stderrEnd.Task.Wait(2000), "native restart child " + action + " closes its bounded pipes");
                    string text, errorText; lock (output) text = output.ToString().Trim(); lock (errors) errorText = errors.ToString().Trim();
                    var parts = text.StartsWith(ChildPrefix, StringComparison.Ordinal)
                        ? text.Substring(ChildPrefix.Length).Split(':') : new string[0];
                    int count = 0, bom = -1; bool framed = parts.Length == 2 &&
                        Int32.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out count) &&
                        Int32.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out bom) && (bom == 0 || bom == 1);
                    if (process.ExitCode != 0 || errorText.Length != 0) {
                        // Reconstruct only validated numeric metadata and whitelisted labels.
                        // Original child stderr, paths, input and DTOs are never forwarded.
                        PrintChildFailure(action, errorText);
                    }
                    Check(process.ExitCode == 0 && !overflow && errorText.Length == 0 && framed && count > 0 && count <= 500,
                        "native restart child " + action + " returns only a successful static result");
                    if (action == "missing") Console.WriteLine("RESTART-TRANSPORT: childLeadingUtf8Bom=" + bom);
                    passed += count;
                } finally {
                    if (started && !process.HasExited) {
                        unsettledChild = true;
                        try { process.Kill(); } catch (InvalidOperationException) { }
                        bool settled = process.WaitForExit(5000);
                        if (settled) {
                            unsettledChild = false;
                            // Bounded drain is useful diagnostic settlement; late callbacks
                            // remain safe even if a failed child does not close a pipe in time.
                            stdoutEnd.Task.Wait(2000); stderrEnd.Task.Wait(2000);
                        }
                        Check(settled, "owned timed-out child terminates before fixture cleanup");
                    } else if (started) {
                        unsettledChild = false;
                    }
                }
            }
        }
        private static int Main(string[] args)
        {
            try {
                if (args.Length != 0) return ChildMain(args);
                Check(Path.DirectorySeparatorChar == '\\', "restart suite runs on native Windows");
                var input = new Request { Proper = "1234", First = "4321", Second = "5678" };
                string root = Path.Combine(Path.GetTempPath(), "ProGo-DailyRestart-" + Guid.NewGuid().ToString("N"));
                string missing = Path.Combine(Path.GetTempPath(), "ProGo-DailyRestart-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(root); Directory.CreateDirectory(missing);
                try {
                    RunChild("missing", missing, Day, input);
                    RunChild("seed", root, Day, input);
                    RunChild("reopen", root, Day.Date.AddDays(1).AddSeconds(-1), input);
                    RunChild("cancel", root, Day.AddDays(1), input);
                    RunChild("forward", root, Day.AddDays(1), input);
                    RunChild("reopen", root, Day.AddDays(1), input);
                    RunChild("empty", root, Day.AddDays(1), input);
                    RunChild("empty-reopen", root, Day.AddDays(1), input);
                    RunChild("backward", root, Day, input);
                    RunChild("reopen", root, Day, input);
                    RunChild("locked", root, Day, input);
                    RunChild("corrupt", root, Day, input);
                } finally {
                    if (!unsettledChild) { Directory.Delete(root, true); Directory.Delete(missing, true); }
                    else Console.Error.WriteLine("RESTART-FIXTURE-RETAINED: owned child did not settle");
                }
                Check(AppPaths.Accesses == 0 && SafeLog.Accesses == 0, "parent fixture uses no application paths or logs");
                Console.WriteLine("Vault daily restart tests PASS: " + passed.ToString(CultureInfo.InvariantCulture)); return 0;
            } catch (Exception ex) {
                if (child) Console.Error.WriteLine(ChildFailurePrefix + childPhase.ToString(CultureInfo.InvariantCulture) + ":" +
                    passed.ToString(CultureInfo.InvariantCulture) + ":" + ExceptionCode(ex).ToString(CultureInfo.InvariantCulture) + ":" +
                    childExpiryStatus.ToString(CultureInfo.InvariantCulture) + ":" + (childInputHadBom ? "1" : "0"));
                else Console.Error.WriteLine("FAIL: vault daily restart " + ex.GetType().Name + (failedCheck == null ? "" : " at " + failedCheck));
                return 1;
            }
        }
    }
}

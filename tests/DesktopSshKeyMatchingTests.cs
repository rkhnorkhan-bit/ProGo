using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        private static bool KeyFingerprintAskpassFixture()
        {
            if (Path.GetFileName(Application.ExecutablePath) != "progo-fingerprint-askpass.exe") return false;
            string marker = Environment.GetEnvironmentVariable("PROGO_FP_ASKPASS_MARKER");
            if (!String.IsNullOrEmpty(marker)) File.WriteAllText(marker, "UNEXPECTED_ASKPASS");
            return true;
        }
        private static SshProfileDiagnosticResult KeyMatchProfile(string root, params string[] identities)
        {
            var result = new SshProfileDiagnosticResult { Target = "selected.fixture", SshResolved = true,
                ExecutablePath = Path.Combine(root, "ssh.exe"), AgentInventory = AgentInventoryParsed(AgentIdentityLine(AgentFingerprintA)) };
            foreach (string identity in identities) { result.IdentityFiles.Add(identity); result.IdentityPaths[identity] = Path.Combine(root, identity); }
            return result;
        }
        private static DiagnosticProcessResult KeyMatchCapture(string fingerprint)
        { return new DiagnosticProcessResult { Output = AgentIdentityLine(fingerprint) }; }
        private static void SshKeyMatchingWorkflow()
        {
            int before = passed; Console.WriteLine("SSH profile key matching checks BEGIN");
            string root = Path.Combine(work, "key-matching-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
            try {
                string fake = Path.Combine(root, "fake-client"); Directory.CreateDirectory(fake);
                File.WriteAllText(Path.Combine(fake, "ssh-keygen.exe"), "");
                KeyMatchingBoundaries(fake);
                KeyMatchingDeadlines(fake);
                KeyMatchingOwnership(fake);
                KeyMatchingForms(fake);
                KeyMatchingNative(root);
            } finally { Directory.Delete(root, true); }
            Console.WriteLine("SSH profile key matching checks PASS: " + (passed - before));
        }
        private static void KeyMatchingBoundaries(string root)
        {
            int calls = 0;
            Func<string, string, int, CancellationToken, DiagnosticProcessResult> run = (exe, args, ms, token) => {
                calls++; if (exe != Path.Combine(root, "ssh-keygen.exe") || ms < 1 || ms > 3000 ||
                    !args.StartsWith("-l -E sha256 -f ", StringComparison.Ordinal) || args.Contains("-P ") || args.Contains("-N ") || args.Contains("-y "))
                    throw new Exception("Unexpected fingerprint command");
                return KeyMatchCapture(args.Contains("second") ? AgentFingerprintB : AgentFingerprintA);
            };
            var profile = KeyMatchProfile(root, "first", "second");
            var result = SshProfileKeyMatching.Check(profile, CancellationToken.None, run);
            Check(result.State == SshProfileKeyMatchState.Completed && result.Identities.Count == 2 && calls == 2 &&
                result.Identities[0].Fingerprint == AgentFingerprintA && result.Identities[0].Presence == SshAgentFingerprintPresence.Present &&
                result.Identities[1].Presence == SshAgentFingerprintPresence.Absent, "effective identities match exact fingerprints instead of an unrelated agent key count");
            Check(!result.Describe("first").Contains("SHA256:") && result.Describe("first").Contains("есть в полученном списке") &&
                result.Describe("second").Contains("другие методы входа не исключены"), "matching display omits fingerprints and does not block alternative authentication");
            bool immutable = false;
            try { result.Identities.Add(result.Identities[0]); } catch (NotSupportedException) { immutable = true; }
            Check(immutable, "profile fingerprint observation list is immutable");
            profile.KeyMatches = result; profile.AgentKeys = profile.AgentInventory.ToDisplay();
            string report = profile.ToReport();
            Check(report.Contains("закрытый ключ, подпись и вход на сервер не проверены") && !report.Contains(AgentFingerprintA) &&
                !report.Contains("PRIVATE_AGENT_COMMENT"), "profile report labels the public-reference boundary without leaking fingerprints or comments");
            foreach (int budget in new[] { 0, 30001 }) {
                bool invalid = false; try { SshProfileKeyMatching.Check(profile, CancellationToken.None, run, budget); } catch (ArgumentOutOfRangeException) { invalid = true; }
                Check(invalid, "invalid profile fingerprint budget is rejected: " + budget);
            }
            using (var cancelled = new CancellationTokenSource()) {
                cancelled.Cancel(); int before = calls; bool rejected = false;
                try { SshProfileKeyMatching.Check(profile, cancelled.Token, run); } catch (OperationCanceledException) { rejected = true; }
                Check(rejected && calls == before, "pre-cancelled fingerprint phase launches no child");
            }
            var unknown = KeyMatchProfile(root, "first"); unknown.SshResolved = false; int originalCalls = calls;
            Check(SshProfileKeyMatching.Check(unknown, CancellationToken.None, run).State == SshProfileKeyMatchState.NotChecked && calls == originalCalls,
                "unresolved SSH configuration cannot claim a selected identity fingerprint");
            var paths = KeyMatchProfile(root, "none", "~/.ssh/unconfirmed", "relative", "device");
            paths.IdentityPaths.Remove("~/.ssh/unconfirmed"); paths.IdentityPaths["relative"] = "relative"; paths.IdentityPaths["device"] = @"\\.\pipe\unconfirmed";
            var skipped = SshProfileKeyMatching.Check(paths, CancellationToken.None, run);
            Check(skipped.Identities[0].State == SshIdentityFingerprintState.NotUsed && skipped.Identities.Skip(1).All(x => x.State == SshIdentityFingerprintState.PathNotResolved) && calls == originalCalls,
                "none home relative and device identities are not guessed or passed to keygen");
            var absentClient = KeyMatchProfile(Path.Combine(root, "absent-client"), "first");
            Check(SshProfileKeyMatching.Check(absentClient, CancellationToken.None, run).Identities[0].State == SshIdentityFingerprintState.MissingExecutable &&
                OpenSshClient.KeygenExecutable("ssh.exe") == null && calls == originalCalls, "fingerprinting never substitutes a PATH utility for a missing sibling keygen");
            foreach (string endpoint in new[] { "none", "SSH_AUTH_SOCK", "C:/fixture/agent" }) {
                var custom = KeyMatchProfile(root, "first"); custom.IdentityAgent = endpoint;
                var observed = SshProfileKeyMatching.Check(custom, CancellationToken.None, run).Identities[0];
                Check(observed.State == SshIdentityFingerprintState.Observed && observed.Presence == SshAgentFingerprintPresence.Unknown,
                    "explicit agent policy never matches against injected default inventory: " + endpoint);
            }
            var missingInventory = KeyMatchProfile(root, "first"); missingInventory.AgentInventory = new SshAgentInventoryResult(SshAgentInventoryState.Unavailable);
            Check(SshProfileKeyMatching.Check(missingInventory, CancellationToken.None, run).Identities[0].Presence == SshAgentFingerprintPresence.Unknown,
                "unavailable agent does not prevent public metadata or imply a missing selected key");
            int fallbackCalls = 0;
            var sidecar = SshProfileKeyMatching.Check(KeyMatchProfile(root, "first"), CancellationToken.None, (e, a, m, t) => {
                fallbackCalls++; return fallbackCalls == 1 ? new DiagnosticProcessResult { ExitCode = 1, Error = "PRIVATE_PRIMARY_FAILURE" } : KeyMatchCapture(AgentFingerprintA);
            }).Identities[0];
            Check(fallbackCalls == 2 && sidecar.State == SshIdentityFingerprintState.Observed && sidecar.SidecarFallback && sidecar.PrimaryExitCode == 1 &&
                sidecar.Presence == SshAgentFingerprintPresence.Unknown && sidecar.ToDisplay().Contains("исходный файл и его права не подтверждены"),
                "fallback public sidecar remains unbound even when its fingerprint appears in the agent");
            foreach (int variant in Enumerable.Range(0, 8)) {
                int attempts = 0;
                var entry = SshProfileKeyMatching.Check(KeyMatchProfile(root, "first"), CancellationToken.None, (e, a, m, t) => {
                    attempts++;
                    if (variant == 0) return new DiagnosticProcessResult { ExitCode = 2, Error = "PRIVATE_COMMAND_ERROR" };
                    if (variant == 1) return new DiagnosticProcessResult { Output = AgentIdentityLine(AgentFingerprintA), Truncated = true };
                    if (variant == 2) return new DiagnosticProcessResult { Output = AgentIdentityLine(AgentFingerprintA) + AgentIdentityLine(AgentFingerprintA) };
                    if (variant == 3) return new DiagnosticProcessResult { Output = AgentIdentityLine(AgentFingerprintA), Error = "PRIVATE_STDERR" };
                    if (variant == 4) return new DiagnosticProcessResult { Output = "PRIVATE_INVALID_OUTPUT" };
                    if (variant == 5) return null;
                    if (variant == 6) throw new TimeoutException("PRIVATE_TIMEOUT");
                    throw new IOException("PRIVATE_RUNNER_ERROR");
                }).Identities[0];
                var expected = variant == 0 ? SshIdentityFingerprintState.CommandFailed : variant == 2 ? SshIdentityFingerprintState.Ambiguous :
                    variant == 5 || variant == 7 ? SshIdentityFingerprintState.Failed : variant == 6 ? SshIdentityFingerprintState.TimedOut : SshIdentityFingerprintState.InvalidOutput;
                Check(entry.State == expected && entry.Fingerprint == null && entry.Presence == SshAgentFingerprintPresence.Unknown &&
                    attempts == (variant == 0 ? 2 : 1) && !entry.ToDisplay().Contains("PRIVATE_"), "unsafe keygen result has no selected fingerprint or false absence: case " + variant);
            }
            var same = KeyMatchProfile(root, "first", "same-file"); same.IdentityPaths["same-file"] = same.IdentityPaths["first"]; int cachedCalls = 0;
            var cached = SshProfileKeyMatching.Check(same, CancellationToken.None, (e, a, m, t) => { cachedCalls++; return KeyMatchCapture(AgentFingerprintA); });
            Check(cachedCalls == 1 && cached.Identities.Count == 2 && cached.Identities[1].Identity == "same-file",
                "two effective entries sharing the exact confirmed path reuse one native observation");
            int caseCalls = 0;
            var caseSensitive = SshProfileKeyMatching.Check(KeyMatchProfile(root, "Case", "case"), CancellationToken.None,
                (e, a, m, t) => { caseCalls++; return KeyMatchCapture(caseCalls == 1 ? AgentFingerprintA : AgentFingerprintB); });
            Check(caseCalls == 2 && caseSensitive.Identities[0].Fingerprint != caseSensitive.Identities[1].Fingerprint,
                "fingerprint cache does not conflate paths that differ by case");
            var capped = KeyMatchProfile(root, Enumerable.Range(0, 35).Select(i => "identity" + i).ToArray());
            var limited = SshProfileKeyMatching.Check(capped, CancellationToken.None, (e, a, m, t) => KeyMatchCapture(AgentFingerprintA), elapsedMilliseconds: () => 0);
            Check(limited.IdentityListLimited && limited.Identities.Count == 32, "fingerprint phase retains the native configuration list limit without inventing whole-profile readiness");
            var snapshot = KeyMatchProfile(root, "first", "second"); int snapshotCalls = 0;
            var frozen = SshProfileKeyMatching.Check(snapshot, CancellationToken.None, (e, a, m, t) => {
                snapshotCalls++; snapshot.IdentityFiles.Clear(); snapshot.IdentityPaths.Clear(); snapshot.IdentityAgent = "none";
                snapshot.AgentInventory = new SshAgentInventoryResult(SshAgentInventoryState.Unavailable);
                return KeyMatchCapture(AgentFingerprintA);
            });
            Check(snapshotCalls == 2 && frozen.Identities.All(x => x.Presence == SshAgentFingerprintPresence.Present),
                "profile identity paths agent policy and inventory are frozen before the first child runs");
        }
        private static void KeyMatchingDeadlines(string root)
        {
            long elapsed = 0; int calls = 0; var budgets = new List<int>();
            var profile = KeyMatchProfile(root, "first", "second");
            var shrinking = SshProfileKeyMatching.Check(profile, CancellationToken.None, (e, a, m, t) => {
                calls++; budgets.Add(m); elapsed = calls == 1 ? 1500 : 1600; return KeyMatchCapture(AgentFingerprintA);
            }, elapsedMilliseconds: () => elapsed);
            Check(budgets.SequenceEqual(new[] { 3000, 1500 }) && shrinking.Identities.All(x => x.State == SshIdentityFingerprintState.Observed),
                "all identity children share one shrinking phase budget instead of receiving three seconds each");
            elapsed = 0; calls = 0;
            var expired = SshProfileKeyMatching.Check(profile, CancellationToken.None, (e, a, m, t) => { calls++; elapsed = 3100; return KeyMatchCapture(AgentFingerprintA); }, elapsedMilliseconds: () => elapsed);
            Check(calls == 1 && expired.Identities.All(x => x.State == SshIdentityFingerprintState.TimedOut && x.Fingerprint == null),
                "late native output and unstarted remaining identities stay unconfirmed after the total deadline");
            calls = 0;
            var before = SshProfileKeyMatching.Check(profile, CancellationToken.None, (e, a, m, t) => { calls++; return KeyMatchCapture(AgentFingerprintA); }, elapsedMilliseconds: () => 3000);
            Check(calls == 0 && before.Identities.All(x => x.State == SshIdentityFingerprintState.TimedOut), "expired phase launches neither primary nor fallback children");
        }
        private static void KeyMatchingOwnership(string root)
        {
            using (var entered = new ManualResetEventSlim()) using (var release = new ManualResetEventSlim()) using (var source = new CancellationTokenSource()) {
                var owner = Task.Run(() => SshProfileKeyMatching.Check(KeyMatchProfile(root, "first"), source.Token, (e, a, m, t) => {
                    entered.Set(); release.Wait(); return KeyMatchCapture(AgentFingerprintA);
                }));
                try {
                    Check(entered.Wait(3000), "fingerprint owner entered its read-only child");
                    int calls = 0;
                    var busy = SshAgentInventory.Read("unused.exe", CancellationToken.None, (e, a, m, t) => { calls++; return KeyMatchCapture(AgentFingerprintA); });
                    var duplicate = SshProfileKeyMatching.Check(KeyMatchProfile(root, "first"), CancellationToken.None, (e, a, m, t) => { calls++; return KeyMatchCapture(AgentFingerprintA); });
                    Check(busy.State == SshAgentInventoryState.Busy && duplicate.State == SshProfileKeyMatchState.Busy && calls == 0,
                        "inventory and duplicate profile check cannot overlap an owned fingerprint child");
                    source.Cancel();
                    Check(!owner.IsCompleted && SshAgentInventory.Read("unused.exe", CancellationToken.None).State == SshAgentInventoryState.Busy,
                        "fingerprint cancellation retains the read-only lease until actual runner cleanup");
                } finally {
                    release.Set(); try { if (!owner.Wait(5000)) throw new Exception("Fingerprint owner did not settle"); }
                    catch (AggregateException ex) { ex.Handle(e => e is OperationCanceledException); }
                }
                bool cancelled = false; try { owner.GetAwaiter().GetResult(); } catch (OperationCanceledException) { cancelled = true; }
                Check(cancelled, "cancelled fingerprint owner cannot publish late captured output");
            }
            using (var lease = SshReadOnlyLease.TryAcquire(CancellationToken.None)) {
                Check(lease != null && SshProfileKeyMatching.Check(KeyMatchProfile(root, "first"), CancellationToken.None).State == SshProfileKeyMatchState.Busy,
                    "an existing read-only operation prevents a profile fingerprint child");
                lease.Dispose(); lease.Dispose();
            }
            Check(SshAgentInventory.Read("unused.exe", CancellationToken.None, (e, a, m, t) => KeyMatchCapture(AgentFingerprintA)).State == SshAgentInventoryState.Available,
                "read-only lease releases exactly once and inventory remains reusable");
        }
        private static void KeyMatchingForms(string root)
        {
            using (var entered = new ManualResetEventSlim()) using (var release = new ManualResetEventSlim())
            using (var form = new SshDiagnosticForm(new SshProfileSetting { Target = "selected.fixture" }, (p, token) => {
                var profile = KeyMatchProfile(root, "first");
                profile.KeyMatches = SshProfileKeyMatching.Check(profile, token, (e, a, m, t) => { entered.Set(); release.Wait(); return KeyMatchCapture(AgentFingerprintA); });
                return profile;
            })) using (var timer = new System.Windows.Forms.Timer { Interval = 15 }) {
                int ticks = 0; timer.Tick += delegate { ticks++; }; timer.Start(); Task running = null;
                try {
                    form.Show(); PumpUntil(() => entered.IsSet && ticks >= 3); running = form.Work;
                    Check(!running.IsCompleted && ((Label)Field(form, "status")).Text.Contains("отпечатки — 3 с"), "diagnostic UI heartbeat and separate phase budgets remain visible during fingerprint I/O");
                    var elapsed = Stopwatch.StartNew(); form.Close();
                    Check(form.IsDisposed && elapsed.ElapsedMilliseconds < 500 && !running.IsCompleted, "closing diagnostic UI does not wait for held fingerprint cleanup");
                } finally { release.Set(); var owned = running ?? form.Work; if (owned != null) AgentProbeSettled(owned); }
                Check(form.IsDisposed && SshAgentInventory.Read("unused.exe", CancellationToken.None, (e, a, m, t) => KeyMatchCapture(AgentFingerprintA)).State == SshAgentInventoryState.Available,
                    "closed fingerprint diagnostic settles without UI pumping or late reopening and releases its lease");
            }
        }
        private static byte[] KeyMatchWire(byte[] data)
        { return new[] { (byte)(data.Length >> 24), (byte)(data.Length >> 16), (byte)(data.Length >> 8), (byte)data.Length }.Concat(data).ToArray(); }
        private static string KeyMatchNativeFingerprint(string publicFile, out byte[] blob)
        {
            string[] fields = File.ReadAllText(publicFile).Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            blob = Convert.FromBase64String(fields[1]);
            using (var hash = SHA256.Create()) return "SHA256:" + Convert.ToBase64String(hash.ComputeHash(blob)).TrimEnd('=');
        }
        private static void KeyMatchingNative(string root)
        {
            string keygen = OpenSshClient.KeygenExecutable(OpenSshClient.Executable);
            Check(!String.IsNullOrEmpty(keygen) && Path.IsPathRooted(keygen), "native profile fingerprint oracle uses the selected OpenSSH sibling keygen");
            string marker = Path.Combine(root, "unexpected-askpass"), helper = Path.Combine(root, "progo-fingerprint-askpass.exe");
            File.Copy(Application.ExecutablePath, helper);
            string[] names = { "SSH_ASKPASS", "SSH_ASKPASS_REQUIRE", "DISPLAY", "PROGO_FP_ASKPASS_MARKER", "SSH_AUTH_SOCK" };
            string[] saved = names.Select(Environment.GetEnvironmentVariable).ToArray();
            try {
                Environment.SetEnvironmentVariable(names[0], helper); Environment.SetEnvironmentVariable(names[1], "force");
                Environment.SetEnvironmentVariable(names[2], "progo-fixture:0"); Environment.SetEnvironmentVariable(names[3], marker);
                string first = Path.Combine(root, "first identity"), second = Path.Combine(root, "second identity");
                foreach (string file in new[] { first, second }) {
                    var generated = DiagnosticProcess.Run(keygen, "-q -t ed25519 -N \"\" -C public-fixture -f " + SshConnection.Quote(file), 7000, CancellationToken.None);
                    Check(generated.ExitCode == 0 && !generated.Truncated, "native test-only key generation uses fresh isolated files and an empty non-secret passphrase");
                }
                byte[] firstBlob, secondBlob;
                string firstFingerprint = KeyMatchNativeFingerprint(first + ".pub", out firstBlob), secondFingerprint = KeyMatchNativeFingerprint(second + ".pub", out secondBlob);
                string originalFirstPublic = File.ReadAllText(first + ".pub"), otherPublic = File.ReadAllText(second + ".pub");
                string privateHash; using (var hash = SHA256.Create()) privateHash = Convert.ToBase64String(hash.ComputeHash(File.ReadAllBytes(first)));
                string config = Path.Combine(root, "config");
                File.WriteAllText(config, "Host profile.identity\n HostName 127.0.0.32\n User fixture_user\n CanonicalizeHostname no\n IdentitiesOnly yes\n IdentityFile " + ConfigQuoted(first) + "\n IdentityFile " + ConfigQuoted(second) + "\n", new UTF8Encoding(false));
                SshProfileDiagnosticResult profile;
                using (var agent = new AgentInventoryPipe(0, firstBlob)) {
                    Environment.SetEnvironmentVariable("SSH_AUTH_SOCK", @"\\.\pipe\" + agent.Name);
                    var probe = new SshAgentServiceProbe(t => new SshAgentServiceSnapshot { RuntimeState = SshAgentRuntimeState.Running });
                    profile = SshProfileDiagnostics.Check(new SshProfileSetting { Target = "profile.identity" }, CancellationToken.None,
                        OpenSshClient.Executable, 7000, probe, configurationFile: config);
                    AgentProbeSettled(agent.Work);
                    Check(profile.SshResolved && profile.ResolvedHostName == "127.0.0.32" && profile.IdentitiesOnly == "yes" && profile.IdentityFiles.Count == 2 &&
                        profile.AgentInventory.State == SshAgentInventoryState.Available && agent.Requests == 1, "full native diagnostic pipeline uses fixture-only alias config and a read-only public-only test agent");
                    Check(profile.KeyMatches.Identities[0].Fingerprint == firstFingerprint && profile.KeyMatches.Identities[0].Presence == SshAgentFingerprintPresence.Present &&
                        profile.KeyMatches.Identities[1].Fingerprint == secondFingerprint && profile.KeyMatches.Identities[1].Presence == SshAgentFingerprintPresence.Absent,
                        "real keygen public fingerprints match an independent SHA256 and the actual native agent inventory for each effective identity");
                }
                File.WriteAllText(first + ".pub", otherPublic);
                profile.AgentInventory = AgentInventoryParsed(AgentIdentityLine(secondFingerprint));
                var stale = SshProfileKeyMatching.Check(profile, CancellationToken.None);
                profile.KeyMatches = stale;
                Check(stale.Identities[0].Fingerprint == secondFingerprint && stale.Identities[0].Presence == SshAgentFingerprintPresence.Present &&
                    profile.ToReport().Contains("закрытый ключ, подпись и вход на сервер не проверены"), "stale sidecar follows native public-reference precedence without becoming private-key or authentication readiness");
                using (var hash = SHA256.Create()) Check(privateHash == Convert.ToBase64String(hash.ComputeHash(File.ReadAllBytes(first))), "fingerprint observation does not mutate the private identity while a sidecar disagrees");
                File.Delete(first + ".pub");
                profile.AgentInventory = AgentInventoryParsed(AgentIdentityLine(firstFingerprint));
                Check(SshProfileKeyMatching.Check(profile, CancellationToken.None).Identities[0].Fingerprint == firstFingerprint,
                    "native keygen reads the public envelope when the sidecar is absent without a passphrase prompt");
                string encrypted = Path.Combine(root, "synthetic-envelope");
                Func<byte[], byte[]> envelopeBytes = blob => Encoding.ASCII.GetBytes("openssh-key-v1\0").Concat(KeyMatchWire(Encoding.ASCII.GetBytes("aes256-ctr")))
                    .Concat(KeyMatchWire(Encoding.ASCII.GetBytes("bcrypt"))).Concat(KeyMatchWire(KeyMatchWire(new byte[16]).Concat(new byte[] { 0, 0, 0, 16 }).ToArray()))
                    .Concat(new byte[] { 0, 0, 0, 1 }).Concat(KeyMatchWire(blob)).Concat(KeyMatchWire(new byte[16])).ToArray();
                Func<byte[], string> armored = blob => "-----BEGIN OPENSSH PRIVATE KEY-----\n" + Convert.ToBase64String(envelopeBytes(blob)) + "\n-----END OPENSSH PRIVATE KEY-----\n";
                File.WriteAllText(encrypted, armored(firstBlob), new UTF8Encoding(false));
                File.SetAccessControl(encrypted, File.GetAccessControl(first));
                var envelope = new SshProfileDiagnosticResult { SshResolved = true, ExecutablePath = OpenSshClient.Executable, AgentInventory = profile.AgentInventory };
                envelope.IdentityFiles.Add(encrypted); envelope.IdentityPaths[encrypted] = encrypted;
                var captures = new List<DiagnosticProcessResult>();
                var synthetic = SshProfileKeyMatching.Check(envelope, CancellationToken.None, (e, a, m, t) => {
                    var captured = DiagnosticProcess.Run(e, a, m, t); captures.Add(captured); return captured;
                }).Identities[0];
                bool observed = synthetic.State == SshIdentityFingerprintState.Observed && synthetic.Fingerprint == firstFingerprint &&
                    synthetic.Presence == SshAgentFingerprintPresence.Present && !synthetic.SidecarFallback;
                bool rejected = synthetic.State == SshIdentityFingerprintState.CommandFailed && captures.Count == 2 && captures.All(c => c.ExitCode != 0 && !c.Truncated) &&
                    synthetic.SidecarFallback && synthetic.PrimaryExitCode != 0 && synthetic.Fingerprint == null && synthetic.Presence == SshAgentFingerprintPresence.Unknown;
                bool warned = synthetic.State == SshIdentityFingerprintState.InvalidOutput && captures.Count == 1 && captures[0].ExitCode == 0 &&
                    !captures[0].Truncated && !String.IsNullOrWhiteSpace(captures[0].Error) && synthetic.Fingerprint == null && synthetic.Presence == SshAgentFingerprintPresence.Unknown;
                Console.WriteLine("Native synthetic envelope observation: state=" + synthetic.State + "; attempts=" + captures.Count +
                    "; exits=" + String.Join(",", captures.Select(c => c.ExitCode.ToString())) + "; stderrPresent=" + captures.Any(c => !String.IsNullOrWhiteSpace(c.Error)));
                Check(observed || rejected || warned,
                    "synthetic unusable encrypted envelope yields only exact public metadata or an explicit unconfirmed native rejection");
                string corrupt = Path.Combine(root, "corrupt-envelope"); File.WriteAllText(corrupt, armored(new byte[] { 1, 2, 3 }), new UTF8Encoding(false));
                File.SetAccessControl(corrupt, File.GetAccessControl(first));
                envelope.IdentityFiles.Clear(); envelope.IdentityPaths.Clear(); envelope.IdentityFiles.Add(corrupt); envelope.IdentityPaths[corrupt] = corrupt;
                var invalidPublic = SshProfileKeyMatching.Check(envelope, CancellationToken.None).Identities[0];
                Check(invalidPublic.State == SshIdentityFingerprintState.CommandFailed && invalidPublic.Fingerprint == null && invalidPublic.Presence == SshAgentFingerprintPresence.Unknown,
                    "corrupt encrypted public envelope never exposes a fingerprint or a false selected-key absence");
                string multi = Path.Combine(root, "multiple-public"); File.WriteAllText(multi, originalFirstPublic + otherPublic);
                envelope.IdentityFiles.Clear(); envelope.IdentityPaths.Clear(); envelope.IdentityFiles.Add(multi); envelope.IdentityPaths[multi] = multi;
                Check(SshProfileKeyMatching.Check(envelope, CancellationToken.None).Identities[0].State == SshIdentityFingerprintState.Ambiguous,
                    "real keygen multi-key file cannot silently select its first fingerprint");
                File.Move(first, first + ".saved"); File.WriteAllText(first + ".pub", originalFirstPublic);
                var unbound = SshProfileKeyMatching.Check(profile, CancellationToken.None).Identities[0];
                Check(unbound.State == SshIdentityFingerprintState.Observed && unbound.SidecarFallback && unbound.PrimaryExitCode != 0 &&
                    unbound.Presence == SshAgentFingerprintPresence.Unknown, "missing primary identity can expose a sidecar only as explicitly unbound metadata");
                Check(!File.Exists(marker), "all native fingerprint paths leave the forced askpass spy untouched");
            } finally { for (int i = 0; i < names.Length; i++) Environment.SetEnvironmentVariable(names[i], saved[i]); }
        }
    }
}

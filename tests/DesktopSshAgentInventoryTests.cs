using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        private static readonly string AgentFingerprintA = "SHA256:" + new string('A', 43);
        private static readonly string AgentFingerprintB = "SHA256:" + Convert.ToBase64String(Enumerable.Repeat((byte)7, 32).ToArray()).TrimEnd('=');
        private static string AgentIdentityLine(string fingerprint) { return "256 " + fingerprint + " PRIVATE_AGENT_COMMENT (ED25519)\n"; }
        private static SshAgentInventoryResult AgentInventoryParsed(string output, int code = 0, string error = null, bool truncated = false)
        { return SshAgentInventory.Parse(new DiagnosticProcessResult { Output = output, ExitCode = code, Error = error, Truncated = truncated }); }

        private static void SshAgentInventoryWorkflow()
        {
            int before = passed;
            Console.WriteLine("SSH-agent inventory checks BEGIN");
            AgentInventoryParsing();
            AgentInventoryOwnership();
            AgentInventoryProfileScope();
            AgentInventoryNative();
            Console.WriteLine("SSH-agent inventory checks PASS: " + (passed - before));
        }
        private static void AgentInventoryParsing()
        {
            var good = AgentInventoryParsed(AgentIdentityLine(AgentFingerprintA) + AgentIdentityLine(AgentFingerprintB));
            Check(good.State == SshAgentInventoryState.Available && good.ExitCode == 0 && good.ListedIdentityCount == 2 &&
                good.Fingerprints.SequenceEqual(new[] { AgentFingerprintA, AgentFingerprintB }), "inventory retains canonical SHA256 identities in native order without comments");
            Check(good.ContainsFingerprint(AgentFingerprintA) == SshAgentFingerprintPresence.Present &&
                good.ContainsFingerprint("SHA256:" + new string('a', 42) + "A") == SshAgentFingerprintPresence.Absent,
                "inventory lookup compares exact case-sensitive fingerprints rather than key count or comments");
            Check(good.ContainsFingerprint("SHA256:short") == SshAgentFingerprintPresence.Unknown && good.ContainsFingerprint(null) == SshAgentFingerprintPresence.Unknown,
                "invalid or missing selected fingerprint never becomes a confirmed missing key");
            var duplicate = AgentInventoryParsed(AgentIdentityLine(AgentFingerprintA) + AgentIdentityLine(AgentFingerprintA));
            Check(duplicate.ListedIdentityCount == 2 && duplicate.Fingerprints.Count == 1, "duplicate agent entries retain listed count but not duplicate fingerprint facts");
            bool immutable = false;
            try { good.Fingerprints.Add(AgentFingerprintA); } catch (NotSupportedException) { immutable = true; }
            Check(immutable && good.Fingerprints.Count == 2, "published inventory fingerprint collection is immutable");
            var empty = AgentInventoryParsed("The agent has no identities.\r\n", 1);
            Check(empty.State == SshAgentInventoryState.NoIdentities && empty.ContainsFingerprint(AgentFingerprintA) == SshAgentFingerprintPresence.Absent,
                "only the precise native empty response plus exit1 confirms an empty inventory");
            foreach (var captured in new[] {
                new DiagnosticProcessResult { ExitCode = 1, Error = "PRIVATE_AGENT_FAILURE" },
                new DiagnosticProcessResult { ExitCode = 1, Output = "The agent has no identities.", Error = "PRIVATE_AGENT_WARNING" },
                new DiagnosticProcessResult { ExitCode = 1, Output = AgentIdentityLine(AgentFingerprintA) },
                new DiagnosticProcessResult { ExitCode = 23, Output = "The agent has no identities." }
            }) {
                var failed = SshAgentInventory.Parse(captured);
                Check(failed.State == SshAgentInventoryState.CommandFailed && failed.ExitCode == captured.ExitCode &&
                    failed.ContainsFingerprint(AgentFingerprintA) == SshAgentFingerprintPresence.Unknown && failed.Fingerprints.Count == 0,
                    "nonzero command failure does not imply an empty agent: exit" + captured.ExitCode);
            }
            Check(AgentInventoryParsed("PRIVATE_AGENT_OUTPUT", 2).State == SshAgentInventoryState.Unavailable,
                "exit2 distinguishes unavailable agent without relaying raw output");
            foreach (string invalid in new[] { "", " ", "PRIVATE_AGENT_JUNK\n", "256 SHA256:short comment (ED25519)\n",
                "0 " + AgentFingerprintA + " comment (ED25519)\n", "256 SHA256:" + new string('A', 42) + "B comment (ED25519)\n",
                "256 " + AgentFingerprintA + " comment\n", AgentIdentityLine(AgentFingerprintA) + "PRIVATE_AGENT_JUNK\n",
                "256 " + AgentFingerprintA + " PRIVATE_AGENT\0COMMENT (ED25519)\n" }) {
                var result = AgentInventoryParsed(invalid);
                Check(result.State == SshAgentInventoryState.InvalidOutput && result.Fingerprints.Count == 0 &&
                    result.ContainsFingerprint(AgentFingerprintA) == SshAgentFingerprintPresence.Unknown,
                    "malformed inventory rejects the whole response rather than trusting a partial prefix");
            }
            Check(AgentInventoryParsed(AgentIdentityLine(AgentFingerprintA), 0, "PRIVATE_AGENT_WARNING").State == SshAgentInventoryState.InvalidOutput,
                "unexpected stderr prevents a success-shaped inventory from becoming complete evidence");
            Check(AgentInventoryParsed(AgentIdentityLine(AgentFingerprintA), 0, null, true).State == SshAgentInventoryState.InvalidOutput,
                "truncated capture exposes no partial fingerprints");
            Check(AgentInventoryParsed(new string('x', DiagnosticProcess.CaptureLimit + 1)).State == SshAgentInventoryState.InvalidOutput,
                "inventory parser independently bounds input even with an injected runner");
            Check(AgentInventoryParsed(String.Concat(Enumerable.Repeat(AgentIdentityLine(AgentFingerprintA), SshAgentInventory.IdentityLimit))).State == SshAgentInventoryState.Available &&
                AgentInventoryParsed(String.Concat(Enumerable.Repeat(AgentIdentityLine(AgentFingerprintA), SshAgentInventory.IdentityLimit + 1))).State == SshAgentInventoryState.InvalidOutput,
                "identity limit is inclusive and overflow cannot prove key absence");
            Check(SshAgentInventory.Parse(null).State == SshAgentInventoryState.Failed, "missing capture is an explicit failed inventory");
            foreach (SshAgentInventoryState state in Enum.GetValues(typeof(SshAgentInventoryState))) {
                var result = new SshAgentInventoryResult(state);
                Check(!result.ToDisplay().Contains("SHA256:") && !result.ToDisplay().Contains("PRIVATE_") &&
                    result.ToDisplay().Length > 0, "inventory display is safe and nonempty for state " + state);
            }
            var report = new SshProfileDiagnosticResult { AgentInventory = good, AgentKeys = good.ToDisplay() }.ToReport();
            Check(report.Contains("ключей: 2") && report.Contains("выбранные identities проверяются отдельно") &&
                !report.Contains(AgentFingerprintA) && !report.Contains("PRIVATE_AGENT_COMMENT"), "SSH report does not leak inventory fingerprints or turn availability into selected-key readiness");
        }
        private static void AgentInventoryOwnership()
        {
            int calls = 0;
            Func<string, string, int, CancellationToken, DiagnosticProcessResult> success = (exe, args, ms, token) => {
                Interlocked.Increment(ref calls);
                if (args != "-l -E sha256" || ms != 3000) throw new Exception("Unexpected inventory command");
                return new DiagnosticProcessResult { Output = AgentIdentityLine(AgentFingerprintA) };
            };
            Check(SshAgentInventory.Read("fixture.exe", CancellationToken.None, success).State == SshAgentInventoryState.Available && calls == 1,
                "typed inventory issues only bounded read-only SHA256 listing");
            using (var cancelled = new CancellationTokenSource()) {
                cancelled.Cancel(); bool rejected = false;
                try { SshAgentInventory.Read("fixture.exe", cancelled.Token, success); } catch (OperationCanceledException) { rejected = true; }
                Check(rejected && calls == 1, "pre-cancelled typed inventory starts no process");
            }
            Check(SshAgentInventory.Read(null, CancellationToken.None, success).State == SshAgentInventoryState.MissingExecutable && calls == 1,
                "missing sibling executable never falls back to another SSH distribution");
            using (var entered = new ManualResetEventSlim()) using (var release = new ManualResetEventSlim())
            using (var source = new CancellationTokenSource()) {
                var running = Task.Run(() => SshAgentInventory.Read("fixture.exe", source.Token, (exe, args, ms, token) => {
                    Interlocked.Increment(ref calls); entered.Set(); release.Wait();
                    return new DiagnosticProcessResult { Output = AgentIdentityLine(AgentFingerprintA) };
                }));
                try {
                    Check(entered.Wait(3000), "first inventory owns its process slot");
                    var competitors = Enumerable.Range(0, 12).Select(i => Task.Run(() => SshAgentInventory.Read("fixture.exe", CancellationToken.None, success))).ToArray();
                    AgentProbeSettled(Task.WhenAll(competitors));
                    Check(competitors.All(t => t.Result.State == SshAgentInventoryState.Busy) && calls == 2,
                        "parallel inventories return Busy without duplicate processes or waiting behind the owner");
                    source.Cancel();
                    Check(!running.IsCompleted && SshAgentInventory.Read("fixture.exe", CancellationToken.None, success).State == SshAgentInventoryState.Busy,
                        "cancellation cannot release a process slot before the runner returns and cleans up");
                } finally {
                    release.Set();
                    try { if (!running.Wait(5000)) throw new Exception("Inventory owner did not settle"); }
                    catch (AggregateException ex) { ex.Handle(e => e is OperationCanceledException); }
                }
                bool cancelled = false;
                try { running.GetAwaiter().GetResult(); } catch (OperationCanceledException) { cancelled = true; }
                Check(cancelled, "late output after cancellation is discarded instead of becoming an inventory");
            }
            Check(SshAgentInventory.Read("fixture.exe", CancellationToken.None, success).State == SshAgentInventoryState.Available,
                "inventory slot becomes reusable after the cancelled owner's actual completion");
            Check(SshAgentInventory.Read("fixture.exe", CancellationToken.None, (e, a, m, t) => { throw new TimeoutException("PRIVATE_TIMEOUT"); }).State == SshAgentInventoryState.TimedOut,
                "owned runner timeout has a distinct inventory state");
            var failed = SshAgentInventory.Read("fixture.exe", CancellationToken.None, (e, a, m, t) => { throw new IOException("PRIVATE_FAILURE"); });
            Check(failed.State == SshAgentInventoryState.Failed && !failed.ToDisplay().Contains("PRIVATE_"), "runner exception is safe and does not masquerade as empty keys");
            Check(SshAgentInventory.Read("fixture.exe", CancellationToken.None, success).State == SshAgentInventoryState.Available,
                "failed and timed-out inventory owners release their slots after runner cleanup");
        }
        private static void AgentInventoryProfileScope()
        {
            int calls = 0;
            Func<string, string, int, CancellationToken, DiagnosticProcessResult> run = (e, a, m, t) => { calls++; return new DiagnosticProcessResult(); };
            Check(SshAgentInventory.ForProfile(new SshProfileDiagnosticResult(), CancellationToken.None, run).State == SshAgentInventoryState.NotChecked && calls == 0,
                "unresolved profile cannot obtain readiness from an unrelated default agent");
            var profile = new SshProfileDiagnosticResult { SshResolved = true, IdentityAgent = "none", IdentitiesOnly = "yes" };
            Check(SshAgentInventory.ForProfile(profile, CancellationToken.None, run).State == SshAgentInventoryState.DisabledByProfile && calls == 0,
                "IdentityAgent none prevents inventory without forbidding direct key authentication");
            foreach (string endpoint in new[] { "SSH_AUTH_SOCK", "$FIXTURE_AGENT", "${FIXTURE_AGENT}", "~/fixture-agent", "C:/fixture/agent", "NONE" }) {
                profile.IdentityAgent = endpoint;
                var inventory = SshAgentInventory.ForProfile(profile, CancellationToken.None, run);
                Check(inventory.State == SshAgentInventoryState.EndpointNotChecked && inventory.ContainsFingerprint(AgentFingerprintA) == SshAgentFingerprintPresence.Unknown && calls == 0,
                    "explicit profile endpoint cannot silently fall back to default agent inventory");
            }
            profile.IdentityAgent = null;
            Check(SshAgentInventory.ForProfile(profile, CancellationToken.None, run).State == SshAgentInventoryState.Deferred && calls == 0,
                "pending service probe does not launch uncoordinated profile inventory");
        }

        // Disposable read-only SSH-agent protocol endpoint; no service or private key exists.
        private sealed class AgentInventoryPipe : IDisposable
        {
            internal readonly string Name = "progo-inventory-" + Guid.NewGuid().ToString("N");
            internal readonly byte[] PublicBlob;
            internal readonly Task Work;
            internal int Requests;
            private readonly NamedPipeServerStream pipe;
            internal AgentInventoryPipe(int mode, byte[] publicBlob = null)
            {
                PublicBlob = publicBlob == null ? AgentWire(Encoding.ASCII.GetBytes("ssh-ed25519")).Concat(AgentWire(Enumerable.Range(1, 32).Select(i => (byte)i).ToArray())).ToArray() : (byte[])publicBlob.Clone();
                pipe = new NamedPipeServerStream(Name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                Work = Task.Run(delegate {
                    pipe.WaitForConnection(); byte[] length = AgentRead(pipe, 4);
                    int size = (length[0] << 24) | (length[1] << 16) | (length[2] << 8) | length[3];
                    if (size != 1 || AgentRead(pipe, size)[0] != 11) throw new IOException("Fixture permits only REQUEST_IDENTITIES");
                    Interlocked.Increment(ref Requests);
                    byte[] body;
                    if (mode == 2) body = new byte[] { 5 }; // AGENT_FAILURE, not no-identities.
                    else if (mode == 1) body = new byte[] { 12, 0, 0, 0, 0 };
                    else {
                        var entry = AgentWire(PublicBlob).Concat(AgentWire(Encoding.UTF8.GetBytes("PRIVATE_AGENT_COMMENT"))).ToArray();
                        body = new byte[] { 12, 0, 0, 0, 2 }.Concat(entry).Concat(entry).ToArray();
                    }
                    byte[] response = AgentWire(body); pipe.Write(response, 0, response.Length); pipe.Flush();
                });
            }
            private static byte[] AgentWire(byte[] bytes)
            { return new[] { (byte)(bytes.Length >> 24), (byte)(bytes.Length >> 16), (byte)(bytes.Length >> 8), (byte)bytes.Length }.Concat(bytes).ToArray(); }
            private static byte[] AgentRead(Stream stream, int length)
            {
                var bytes = new byte[length]; int offset = 0;
                while (offset < length) { int n = stream.Read(bytes, offset, length - offset); if (n == 0) throw new EndOfStreamException(); offset += n; }
                return bytes;
            }
            public void Dispose()
            {
                pipe.Dispose();
                try { if (!Work.Wait(5000)) throw new Exception("Agent fixture cleanup did not settle"); }
                catch (AggregateException ex) { ex.Handle(e => e is IOException || e is ObjectDisposedException); }
            }
        }
        private static void AgentInventoryNative()
        {
            string executable = OpenSshClient.AgentExecutable(OpenSshClient.Executable);
            Check(!String.IsNullOrEmpty(executable) && Path.IsPathRooted(executable), "native inventory oracle uses the selected OpenSSH sibling ssh-add");
            string previous = Environment.GetEnvironmentVariable("SSH_AUTH_SOCK");
            string root = Path.Combine(work, "ssh-agent-inventory-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
            try {
                string config = Path.Combine(root, "config");
                File.WriteAllText(config, "Host inventory.fixture\n HostName 127.0.0.31\n User fixture\n CanonicalizeHostname no\n IdentitiesOnly yes\n IdentityFile none\n", new UTF8Encoding(false));
                var profile = SshProfileDiagnostics.CheckConfiguration(new SshProfileSetting { Target = "inventory.fixture" },
                    CancellationToken.None, OpenSshClient.Executable, 7000, config);
                Check(profile.SshResolved && profile.ResolvedHostName == "127.0.0.31" && profile.IdentitiesOnly == "yes" &&
                    String.IsNullOrEmpty(profile.IdentityAgent), "native effective profile retains its default agent scope without reading owner config");
                profile.AgentProbe = new SshAgentProbeResult(SshAgentProbeState.Completed); // Boundary fact only; no service operation.
                for (int mode = 0; mode < 3; mode++) using (var fixture = new AgentInventoryPipe(mode)) {
                    Environment.SetEnvironmentVariable("SSH_AUTH_SOCK", @"\\.\pipe\" + fixture.Name);
                    var result = SshAgentInventory.ForProfile(profile, CancellationToken.None);
                    AgentProbeSettled(fixture.Work);
                    var expected = mode == 0 ? SshAgentInventoryState.Available : mode == 1 ? SshAgentInventoryState.NoIdentities : SshAgentInventoryState.CommandFailed;
                    Check(result.State == expected && result.ExitCode == (mode == 0 ? 0 : 1) && fixture.Requests == 1,
                        "real ssh-add against an isolated read-only named pipe distinguishes native inventory case " + mode);
                    if (mode == 0) using (var hash = SHA256.Create()) {
                        string fingerprint = "SHA256:" + Convert.ToBase64String(hash.ComputeHash(fixture.PublicBlob)).TrimEnd('=');
                        Check(result.ListedIdentityCount == 2 && result.Fingerprints.SequenceEqual(new[] { fingerprint }) &&
                            result.ContainsFingerprint(fingerprint) == SshAgentFingerprintPresence.Present && !result.ToDisplay().Contains("PRIVATE_"),
                            "native ssh-add fingerprints agree with an independent SHA256 of the public SSH wire blob");
                    }
                }
                Environment.SetEnvironmentVariable("SSH_AUTH_SOCK", @"\\.\pipe\progo-missing-" + Guid.NewGuid().ToString("N"));
                var unavailable = SshAgentInventory.Read(executable, CancellationToken.None);
                Check(unavailable.State == SshAgentInventoryState.Unavailable && unavailable.ExitCode == 2,
                    "real ssh-add reports exit2 for the isolated nonexistent agent without using a system service");
            } finally { Environment.SetEnvironmentVariable("SSH_AUTH_SOCK", previous); Directory.Delete(root, true); }
        }
    }
}

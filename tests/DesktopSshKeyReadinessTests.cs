using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
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
        private static int KeyReadinessEnvironmentFixture()
        {
            Console.OutputEncoding = new UTF8Encoding(false);
            Console.WriteLine(Environment.GetEnvironmentVariable("SSH_AUTH_SOCK") ?? "<default>");
            Console.WriteLine(Environment.GetEnvironmentVariable("SSH_ASKPASS") == null ? "no-askpass" : "has-askpass");
            Console.WriteLine(Environment.GetEnvironmentVariable("DISPLAY") == null ? "no-display" : "has-display");
            Console.WriteLine(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY") == null ? "no-wayland" : "has-wayland");
            Console.WriteLine(Environment.GetEnvironmentVariable("SSH_ASKPASS_REQUIRE") ?? "<unset>");
            return 0;
        }
        private static SshProfileDiagnosticResult ReadinessProfile(string directory, string identity)
        {
            var profile = new SshProfileDiagnosticResult { Target = "signing.fixture", SshResolved = true,
                ExecutablePath = Path.Combine(directory, "ssh.exe"), PubkeyAuthentication = "yes", PreferredAuthentications = "publickey" };
            profile.IdentityFiles.Add(identity); profile.IdentityPaths[identity] = Path.Combine(directory, identity);
            return profile;
        }
        private static void SshKeyReadinessWorkflow()
        {
            int before = passed; Console.WriteLine("SSH selected-key signing checks BEGIN");
            string root = Path.Combine(work, "key-readiness-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
            try {
                string fake = Path.Combine(root, "fake-client"); Directory.CreateDirectory(fake);
                File.WriteAllText(Path.Combine(fake, "ssh-add.exe"), "");
                ReadinessBoundaries(fake);
                ReadinessReparse(fake);
                ReadinessChanges(fake);
                ReadinessFinalFreshness(fake);
                ReadinessOwnership(fake);
                ReadinessNative(root);
            } finally { Directory.Delete(root, true); }
            Console.WriteLine("SSH selected-key signing checks PASS: " + (passed - before));
        }
        private static void ReadinessBoundaries(string root)
        {
            string file = Path.Combine(root, "selected key"); File.WriteAllText(file, "public reference fixture");
            int calls = 0;
            Func<string, string, string, int, CancellationToken, DiagnosticProcessResult> run = (exe, args, endpoint, ms, token) => {
                calls++;
                if (exe != Path.Combine(root, "ssh-add.exe") || args != "-T -- " + SshConnection.Quote(file) || ms < 1 || ms > 3000)
                    throw new Exception("Unexpected signing command");
                return new DiagnosticProcessResult();
            };
            var profile = ReadinessProfile(root, "selected key");
            var observed = SshKeyReadiness.Check(profile, "selected key", CancellationToken.None, run);
            Check(observed.State == SshKeyReadinessState.AgentSigningVerified && observed.ExitCode == 0 && calls == 1,
                "signing observation uses one selected path and sibling ssh-add without shell or key mutation flags");
            Check(observed.ToDisplay().Contains("закрытого файла и вход на сервер этим не подтверждены") &&
                !observed.ToDisplay().Contains(file) && !observed.ToDisplay().Contains("SHA256:"),
                "successful sign-and-verify remains separate from private-file validity and server authentication");
            int before = calls;
            Check(SshKeyReadiness.Check(null, "selected key", CancellationToken.None, run).State == SshKeyReadinessState.NotChecked,
                "missing profile cannot produce selected-key signing evidence");
            profile.SshResolved = false;
            Check(SshKeyReadiness.Check(profile, "selected key", CancellationToken.None, run).State == SshKeyReadinessState.NotChecked,
                "unresolved profile cannot launch a signing probe");
            profile.SshResolved = true; profile.PubkeyAuthentication = "no";
            Check(SshKeyReadiness.Check(profile, "selected key", CancellationToken.None, run).State == SshKeyReadinessState.NotUsedByAuthentication,
                "disabled publickey policy does not introduce an agent dependency for other authentication");
            profile.PubkeyAuthentication = "yes"; profile.PreferredAuthentications = "password";
            Check(SshKeyReadiness.Check(profile, "selected key", CancellationToken.None, run).State == SshKeyReadinessState.NotUsedByAuthentication,
                "preferred methods without publickey skip signing without blocking those methods");
            profile.PreferredAuthentications = null;
            Check(SshKeyReadiness.Check(profile, "selected key", CancellationToken.None, run).State == SshKeyReadinessState.AuthenticationPolicyUnconfirmed,
                "unknown client policy is not guessed as a signing-ready method");
            profile.PreferredAuthentications = "publickey"; profile.IdentityAgent = "none";
            Check(SshKeyReadiness.Check(profile, "selected key", CancellationToken.None, run).State == SshKeyReadinessState.DisabledByProfile,
                "IdentityAgent none remains compatible with direct private-key authentication");
            foreach (string endpoint in new[] { "SSH_AUTH_SOCK", "C:/fixture/agent" }) {
                profile.IdentityAgent = endpoint;
                Check(SshKeyReadiness.Check(profile, "selected key", CancellationToken.None, run).State == SshKeyReadinessState.UnsupportedEndpoint,
                    "custom profile agent is never replaced with the default signing endpoint");
            }
            profile.IdentityAgent = null;
            Check(SshKeyReadiness.Check(profile, "unselected key", CancellationToken.None, run).State == SshKeyReadinessState.IdentityNotSelected,
                "a path outside the effective identity list is not selected implicitly");
            profile.IdentityPaths["selected key"] = Path.Combine(root, "..", "selected key");
            Check(SshKeyReadiness.Check(profile, "selected key", CancellationToken.None, run).State == SshKeyReadinessState.PathUnresolved,
                "noncanonical relative escape cannot reach a signing child");
            profile = ReadinessProfile(root, "selected key"); profile.ExecutablePath = Path.Combine(root, "missing", "ssh.exe");
            Check(SshKeyReadiness.Check(profile, "selected key", CancellationToken.None, run).State == SshKeyReadinessState.MissingExecutable && calls == before,
                "a missing matching ssh-add distribution does not fall back to PATH");
            profile = ReadinessProfile(root, "absent key");
            Check(SshKeyReadiness.Check(profile, "absent key", CancellationToken.None, run).State == SshKeyReadinessState.MissingIdentity && calls == before,
                "confirmed absence differs from unreadable identity evidence and launches no child");
            string large = Path.Combine(root, "oversized"); File.WriteAllBytes(large, new byte[SshKeyReadiness.FileLimit + 1]);
            Check(SshKeyReadiness.Check(ReadinessProfile(root, "oversized"), "oversized", CancellationToken.None, run).State == SshKeyReadinessState.FileUnavailable && calls == before,
                "oversized identity evidence is bounded before launching OpenSSH");
            Directory.CreateDirectory(Path.Combine(root, "directory key"));
            Check(SshKeyReadiness.Check(ReadinessProfile(root, "directory key"), "directory key", CancellationToken.None, run).State == SshKeyReadinessState.FileUnavailable,
                "a directory cannot masquerade as missing or usable key evidence");
            using (var held = new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) {
                Check(SshKeyReadiness.Check(ReadinessProfile(root, "selected key"), "selected key", CancellationToken.None, run).State == SshKeyReadinessState.FileUnavailable && calls == before,
                    "an actually locked file is unavailable rather than missing or signing-ready");
            }
            foreach (int budget in new[] { 0, 30001 }) {
                bool rejected = false;
                try { SshKeyReadiness.Check(profile, "absent key", CancellationToken.None, run, budget); } catch (ArgumentOutOfRangeException) { rejected = true; }
                Check(rejected, "invalid signing probe budget is rejected");
            }
            bool emptyEndpointRejected = false;
            try { DiagnosticProcess.RunReadOnly("unused-readiness-child", "", "", 1000, CancellationToken.None); }
            catch (ArgumentException) { emptyEndpointRejected = true; }
            Check(emptyEndpointRejected, "an explicit empty read-only endpoint is rejected before launching a child or falling back to the default agent");
            for (int variant = 0; variant < 6; variant++) {
                int current = variant;
                var result = SshKeyReadiness.Check(ReadinessProfile(root, "selected key"), "selected key", CancellationToken.None, (e, a, s, m, t) => {
                    if (current == 0) return new DiagnosticProcessResult { ExitCode = 2, Error = "PRIVATE_AGENT_ERROR" };
                    if (current == 1) return new DiagnosticProcessResult { ExitCode = 1, Error = "PRIVATE_SIGNING_ERROR" };
                    if (current == 2) return new DiagnosticProcessResult { Output = "PRIVATE_OUTPUT" };
                    if (current == 3) return new DiagnosticProcessResult { Error = "PRIVATE_ERROR" };
                    if (current == 4) return new DiagnosticProcessResult { Truncated = true };
                    return null;
                });
                var expected = variant == 0 ? SshKeyReadinessState.AgentUnavailable : variant == 1 ? SshKeyReadinessState.CommandFailed :
                    variant == 5 ? SshKeyReadinessState.Failed : SshKeyReadinessState.InvalidOutput;
                Check(result.State == expected && !result.ToDisplay().Contains("PRIVATE_"),
                    "nonquiet partial failed or missing process results cannot establish signing and never leak raw output");
            }
        }
        private static void ReadinessReparse(string root)
        {
            string target = Path.Combine(root, "junction-target"), link = Path.Combine(root, "junction-link");
            Directory.CreateDirectory(target); File.WriteAllText(Path.Combine(target, "selected"), "public reference fixture");
            try {
                var info = new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe"),
                    "/D /C mklink /J \"" + link + "\" \"" + target + "\"") {
                    UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true
                };
                using (var process = Process.Start(info)) {
                    bool exited = process.WaitForExit(5000);
                    if (!exited) { process.Kill(); process.WaitForExit(2000); }
                    Check(exited && process.ExitCode == 0 && (File.GetAttributes(link) & FileAttributes.ReparsePoint) != 0,
                        "signing fixture creates an actual owned Windows junction");
                }
                int calls = 0; string identity = Path.Combine("junction-link", "selected");
                var result = SshKeyReadiness.Check(ReadinessProfile(root, identity), identity, CancellationToken.None,
                    (e, a, s, m, c) => { calls++; return new DiagnosticProcessResult(); });
                Check(result.State == SshKeyReadinessState.FileUnavailable && calls == 0 && File.ReadAllText(Path.Combine(target, "selected")) == "public reference fixture",
                    "an actual reparse parent cannot redirect selected-key observation or mutate the target");
            } finally { if (Directory.Exists(link)) Directory.Delete(link); Directory.Delete(target, true); }
        }
        private static void ReadinessChanges(string root)
        {
            string file = Path.Combine(root, "selected key");
            for (int variant = 0; variant < 5; variant++) {
                File.WriteAllText(file, "original reference");
                if (File.Exists(file + ".pub")) File.Delete(file + ".pub");
                var profile = ReadinessProfile(root, "selected key"); int current = variant;
                string old = Environment.GetEnvironmentVariable("SSH_AUTH_SOCK");
                try {
                    var result = SshKeyReadiness.Check(profile, "selected key", CancellationToken.None, (e, a, endpoint, m, t) => {
                        if (current == 0) File.WriteAllText(file, "changed reference!");
                        if (current == 1) File.WriteAllText(file + ".pub", "new sidecar");
                        if (current == 2) File.Delete(file);
                        if (current == 3) profile.ResolvedUser = "changed-fixture-user";
                        if (current == 4) Environment.SetEnvironmentVariable("SSH_AUTH_SOCK", "changed-fixture-endpoint");
                        return new DiagnosticProcessResult();
                    });
                    Check(result.State == SshKeyReadinessState.Changed,
                        "changed primary sidecar selected scope or agent environment invalidates a previously successful child result");
                } finally { Environment.SetEnvironmentVariable("SSH_AUTH_SOCK", old); }
            }
            File.WriteAllText(file, "original reference");
            long elapsed = 0;
            Check(SshKeyReadiness.Check(ReadinessProfile(root, "selected key"), "selected key", CancellationToken.None,
                (e, a, s, m, t) => { elapsed = 3000; return new DiagnosticProcessResult(); }, elapsedMilliseconds: () => elapsed).State == SshKeyReadinessState.TimedOut,
                "phase deadline invalidates a child success that arrived after the full signing budget");
            using (var cancel = new CancellationTokenSource()) {
                cancel.Cancel(); int calls = 0;
                Check(SshKeyReadiness.Check(ReadinessProfile(root, "selected key"), "selected key", cancel.Token,
                    (e, a, s, m, t) => { calls++; return new DiagnosticProcessResult(); }).State == SshKeyReadinessState.Cancelled && calls == 0,
                    "pre-cancelled signing preparation starts no filesystem or process work");
            }
        }
        private static void ReadinessFinalFreshness(string root)
        {
            for (int variant = 0; variant < 2; variant++) {
                var profile = ReadinessProfile(root, "selected key");
                bool postCommand = false; int postReads = 0; int current = variant;
                string oldEndpoint = Environment.GetEnvironmentVariable("SSH_AUTH_SOCK");
                try {
                    var result = SshKeyReadiness.Check(profile, "selected key", CancellationToken.None,
                        (e, a, s, m, token) => { postCommand = true; return new DiagnosticProcessResult(); },
                        elapsedMilliseconds: () => {
                            // First call is the deadline immediately after Run.
                            // The second begins the first post-command ReadStamp,
                            // after the earlier scope check has already passed.
                            if (postCommand && ++postReads == 2) {
                                if (current == 0) profile.ResolvedUser = "changed-during-post-read";
                                else Environment.SetEnvironmentVariable("SSH_AUTH_SOCK", "changed-during-post-read");
                            }
                            return 0;
                        });
                    Check(postReads >= 2 && result.State == SshKeyReadinessState.Changed,
                        "profile or agent endpoint changed inside a post-command file read cannot publish signing-ready evidence");
                } finally { Environment.SetEnvironmentVariable("SSH_AUTH_SOCK", oldEndpoint); }
            }
        }
        private static void ReadinessOwnership(string root)
        {
            using (var lease = SshReadOnlyLease.TryAcquire(CancellationToken.None)) {
                Check(lease != null && SshKeyReadiness.Check(ReadinessProfile(root, "selected key"), "selected key", CancellationToken.None).State == SshKeyReadinessState.Busy,
                    "signing shares the existing service inventory key-add ownership slot");
            }
            using (var entered = new ManualResetEventSlim()) using (var release = new ManualResetEventSlim()) using (var cancel = new CancellationTokenSource()) {
                var task = Task.Run(() => SshKeyReadiness.Check(ReadinessProfile(root, "selected key"), "selected key", cancel.Token,
                    (e, a, s, m, t) => { entered.Set(); if (!release.Wait(10000)) throw new TimeoutException(); t.ThrowIfCancellationRequested(); return new DiagnosticProcessResult(); }));
                try {
                    Check(entered.Wait(5000), "signing fixture reaches its actual owned child boundary"); cancel.Cancel();
                    Check(!task.IsCompleted && SshKeyReadiness.Check(ReadinessProfile(root, "selected key"), "selected key", CancellationToken.None).State == SshKeyReadinessState.Busy,
                        "cancelled waiter retains the slot while its actual child cleanup is still held");
                } finally { release.Set(); AgentProbeSettled(task); }
                Check(task.Result.State == SshKeyReadinessState.Cancelled && SshReadOnlyLeaseAvailable(),
                    "cancelled signing owner settles and releases exactly its own lease");
            }
        }
        private static bool SshReadOnlyLeaseAvailable()
        { using (var lease = SshReadOnlyLease.TryAcquire(CancellationToken.None)) return lease != null; }

        // Isolated agent endpoint. It owns a disposable RSA key; it can only sign
        // for that exact public blob, with strict RSA protocol flags and framing.
        private sealed class ReadinessAgent : IDisposable
        {
            internal readonly string Name = "progo-signing-" + Guid.NewGuid().ToString("N");
            internal readonly Task Work;
            internal readonly ManualResetEventSlim Entered = new ManualResetEventSlim(), Release = new ManualResetEventSlim();
            internal int Requests;
            private readonly NamedPipeServerStream pipe;
            internal ReadinessAgent(RSACryptoServiceProvider rsa, byte[] expectedBlob, int mode)
            {
                pipe = new NamedPipeServerStream(Name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                Work = Task.Factory.StartNew(() => {
                    pipe.WaitForConnection(); byte[] request = ReadinessReadFrame(pipe);
                    using (var bytes = new MemoryStream(request)) using (var reader = new BinaryReader(bytes)) {
                        if (reader.ReadByte() != 13) throw new IOException("Disposable signing agent accepts SIGN_REQUEST only");
                        byte[] blob = ReadinessReadString(reader), data = ReadinessReadString(reader);
                        uint flags = ReadinessReadUInt(reader);
                        if (bytes.Position != bytes.Length || data.Length < 1 || flags != 0 && flags != 2 && flags != 4)
                            throw new IOException("Invalid disposable signing request");
                        Interlocked.Increment(ref Requests); Entered.Set();
                        byte[] body;
                        if (mode == 2 || !blob.SequenceEqual(expectedBlob)) body = new byte[] { 5 };
                        else {
                            if (mode == 3 && !Release.Wait(10000)) throw new TimeoutException("Disposable signing fixture was not released");
                            string algorithm = flags == 2 ? "rsa-sha2-256" : flags == 4 ? "rsa-sha2-512" : "ssh-rsa";
                            byte[] signature = rsa.SignData(data, flags == 2 ? "SHA256" : flags == 4 ? "SHA512" : "SHA1");
                            if (mode == 1) signature[0] ^= 1;
                            var signed = ReadinessWire(Encoding.ASCII.GetBytes(algorithm)).Concat(ReadinessWire(signature)).ToArray();
                            body = new byte[] { 14 }.Concat(ReadinessWire(signed)).ToArray();
                        }
                        byte[] response = ReadinessWire(body); pipe.Write(response, 0, response.Length); pipe.Flush();
                    }
                }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }
            public void Dispose()
            {
                Release.Set(); pipe.Dispose();
                try { if (!Work.Wait(5000)) throw new Exception("Disposable signing endpoint did not settle"); }
                catch (AggregateException ex) { ex.Handle(e => e is IOException || e is ObjectDisposedException); }
                Entered.Dispose(); Release.Dispose();
            }
        }
        private static byte[] ReadinessWire(byte[] data)
        { return new[] { (byte)(data.Length >> 24), (byte)(data.Length >> 16), (byte)(data.Length >> 8), (byte)data.Length }.Concat(data).ToArray(); }
        private static uint ReadinessReadUInt(BinaryReader reader)
        { var b = reader.ReadBytes(4); if (b.Length != 4) throw new EndOfStreamException(); return (uint)b[0] << 24 | (uint)b[1] << 16 | (uint)b[2] << 8 | b[3]; }
        private static byte[] ReadinessReadString(BinaryReader reader)
        { uint count = ReadinessReadUInt(reader); if (count > 65536) throw new IOException("Oversized disposable signing field"); var value = reader.ReadBytes((int)count); if (value.Length != count) throw new EndOfStreamException(); return value; }
        private static byte[] ReadinessReadFrame(Stream stream)
        {
            var length = new byte[4]; int offset = 0;
            while (offset != 4) { int n = stream.Read(length, offset, 4 - offset); if (n == 0) throw new EndOfStreamException(); offset += n; }
            int count = length[0] << 24 | length[1] << 16 | length[2] << 8 | length[3];
            if (count < 1 || count > 131072) throw new IOException("Oversized disposable signing frame");
            var body = new byte[count]; offset = 0;
            while (offset != count) { int n = stream.Read(body, offset, count - offset); if (n == 0) throw new EndOfStreamException(); offset += n; }
            return body;
        }
        private static byte[] ReadinessMpint(byte[] data)
        { return ReadinessWire(data.Length != 0 && (data[0] & 128) != 0 ? new byte[] { 0 }.Concat(data).ToArray() : data); }
        private static byte[] ReadinessPublicBlob(RSACryptoServiceProvider rsa)
        { var p = rsa.ExportParameters(false); return ReadinessWire(Encoding.ASCII.GetBytes("ssh-rsa")).Concat(ReadinessMpint(p.Exponent)).Concat(ReadinessMpint(p.Modulus)).ToArray(); }
        private static void ReadinessNative(string root)
        {
            string[] names = { "SSH_AUTH_SOCK", "SSH_ASKPASS", "SSH_ASKPASS_REQUIRE", "DISPLAY", "WAYLAND_DISPLAY", "PROGO_FP_ASKPASS_MARKER" };
            string[] old = names.Select(Environment.GetEnvironmentVariable).ToArray();
            string marker = Path.Combine(root, "unexpected-askpass"), helper = Path.Combine(root, "progo-fingerprint-askpass.exe");
            File.Copy(Application.ExecutablePath, helper);
            try {
                Environment.SetEnvironmentVariable(names[1], helper); Environment.SetEnvironmentVariable(names[2], "force");
                Environment.SetEnvironmentVariable(names[3], "fixture-display"); Environment.SetEnvironmentVariable(names[4], "fixture-wayland");
                Environment.SetEnvironmentVariable(names[5], marker);
                string frozen = "frozen-агент-ключ-fixture-endpoint"; Environment.SetEnvironmentVariable(names[0], "later-fixture-endpoint");
                var sanitized = DiagnosticProcess.RunReadOnly(Application.ExecutablePath, "--key-readiness-env", frozen, 5000, CancellationToken.None);
                Check(sanitized.ExitCode == 0 && sanitized.Output.Replace("\r", "") == frozen + "\nno-askpass\nno-display\nno-wayland\nnever\n",
                    "real native read-only child preserves a frozen Unicode endpoint and cannot inherit forced askpass or display overrides");
                var defaults = DiagnosticProcess.RunReadOnly(Application.ExecutablePath, "--key-readiness-env", null, 5000, CancellationToken.None);
                Check(defaults.ExitCode == 0 && defaults.Output.StartsWith("<default>\r\n") && Environment.GetEnvironmentVariable(names[0]) == "later-fixture-endpoint" &&
                    Environment.GetEnvironmentVariable(names[2]) == "force", "null selected endpoint clears only the child override while the parent environment is preserved");
                var legacy = DiagnosticProcess.Run(Application.ExecutablePath, "--key-readiness-env", 5000, CancellationToken.None);
                Check(legacy.ExitCode == 0 && legacy.Output.Replace("\r", "") == "later-fixture-endpoint\nhas-askpass\nhas-display\nhas-wayland\nforce\n",
                    "the existing diagnostic Run contract continues to inherit its environment unchanged");
                string ssh = OpenSshClient.Executable; Check(!String.IsNullOrEmpty(OpenSshClient.AgentExecutable(ssh)), "native signing oracle uses selected OpenSSH sibling ssh-add");
                using (var rsa = new RSACryptoServiceProvider(2048)) {
                    rsa.PersistKeyInCsp = false; byte[] blob = ReadinessPublicBlob(rsa);
                    string selected = Path.Combine(root, "selected public key"); File.WriteAllText(selected, "ssh-rsa " + Convert.ToBase64String(blob) + " disposable-signing-fixture\n");
                    Func<string, SshProfileDiagnosticResult> profile = path => {
                        var p = ReadinessProfile(root, Path.GetFileName(path)); p.ExecutablePath = ssh; return p;
                    };
                    for (int mode = 0; mode < 3; mode++) using (var agent = new ReadinessAgent(rsa, blob, mode)) {
                        Environment.SetEnvironmentVariable(names[0], @"\\.\pipe\" + agent.Name);
                        var result = SshKeyReadiness.Check(profile(selected), Path.GetFileName(selected), CancellationToken.None, timeoutMs: 7000);
                        AgentProbeSettled(agent.Work);
                        Check(agent.Requests == 1 && result.State == (mode == 0 ? SshKeyReadinessState.AgentSigningVerified : SshKeyReadinessState.CommandFailed),
                            "real ssh-add verifies a valid signature and rejects altered signatures or an explicit isolated-agent refusal");
                    }
                    using (var other = new RSACryptoServiceProvider(2048)) {
                        other.PersistKeyInCsp = false; string wrong = Path.Combine(root, "other public key");
                        File.WriteAllText(wrong, "ssh-rsa " + Convert.ToBase64String(ReadinessPublicBlob(other)) + " disposable-other-fixture\n");
                        using (var agent = new ReadinessAgent(rsa, blob, 0)) {
                            Environment.SetEnvironmentVariable(names[0], @"\\.\pipe\" + agent.Name);
                            var result = SshKeyReadiness.Check(profile(wrong), Path.GetFileName(wrong), CancellationToken.None, timeoutMs: 7000);
                            AgentProbeSettled(agent.Work);
                            Check(result.State == SshKeyReadinessState.CommandFailed && agent.Requests == 1,
                                "an agent holding a different private key cannot establish readiness for the selected public identity");
                        }
                    }
                    string privatePlaceholder = Path.Combine(root, "unconfirmed private file");
                    File.WriteAllText(privatePlaceholder, "unconfirmed private bytes"); File.Copy(selected, privatePlaceholder + ".pub");
                    using (var agent = new ReadinessAgent(rsa, blob, 0)) {
                        Environment.SetEnvironmentVariable(names[0], @"\\.\pipe\" + agent.Name);
                        var result = SshKeyReadiness.Check(profile(privatePlaceholder), Path.GetFileName(privatePlaceholder), CancellationToken.None, timeoutMs: 7000);
                        AgentProbeSettled(agent.Work);
                        Check(result.State == SshKeyReadinessState.AgentSigningVerified && result.ToDisplay().Contains("закрытого файла и вход на сервер этим не подтверждены"),
                            "native sidecar signing proves only its public reference even when the primary private file is not usable");
                    }
                    for (int mode = 0; mode < 2; mode++) using (var agent = new ReadinessAgent(rsa, blob, 3)) using (var cancel = new CancellationTokenSource()) {
                        Environment.SetEnvironmentVariable(names[0], @"\\.\pipe\" + agent.Name);
                        var task = Task.Run(() => SshKeyReadiness.Check(profile(selected), Path.GetFileName(selected), cancel.Token, timeoutMs: mode == 0 ? 3000 : 10000));
                        try {
                            Check(agent.Entered.Wait(5000), "real signing child reaches the isolated blocked agent before cancellation or timeout");
                            if (mode == 1) cancel.Cancel();
                            AgentProbeSettled(task);
                            Check(task.Result.State == (mode == 0 ? SshKeyReadinessState.TimedOut : SshKeyReadinessState.Cancelled) && SshReadOnlyLeaseAvailable(),
                                "actual native signing timeout or cancellation settles its owned child before releasing the shared lease");
                        } finally { agent.Release.Set(); }
                    }
                }
                Check(!File.Exists(marker), "actual ssh-add signing never invokes an inherited forced askpass helper");
            } finally { for (int i = 0; i < names.Length; i++) Environment.SetEnvironmentVariable(names[i], old[i]); }
        }
    }
}

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
        private static void SshAuthenticationPreparationWorkflow()
        {
            int before = passed; Console.WriteLine("SSH advisory authentication preparation checks BEGIN");
            string root = Path.Combine(work, "authentication-preparation-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
            string previous = Environment.GetEnvironmentVariable("SSH_AUTH_SOCK");
            try {
                Environment.SetEnvironmentVariable("SSH_AUTH_SOCK", "preparation-fixture-endpoint");
                string client = Path.Combine(root, "fake-client"); Directory.CreateDirectory(client);
                File.WriteAllText(Path.Combine(client, "ssh.exe"), ""); File.WriteAllText(Path.Combine(client, "ssh-add.exe"), "");
                File.WriteAllText(Path.Combine(client, "selected public reference"), "synthetic public reference");
                AuthenticationPreparationSnapshots(client);
                AuthenticationPreparationAdvisories(client);
                AuthenticationPreparationBudgetAndFreshness(client);
                AuthenticationPreparationNative(root);
            } finally { Environment.SetEnvironmentVariable("SSH_AUTH_SOCK", previous); Directory.Delete(root, true); }
            Console.WriteLine("SSH advisory authentication preparation checks PASS: " + (passed - before));
        }
        private static SshAuthenticationPreparationRequest PreparationRequest(string root)
        { return new SshAuthenticationPreparationRequest(new SshProfileSetting { Target = "preparation.fixture" }, Path.Combine(root, "ssh.exe")); }
        private static SshProfileDiagnosticResult PreparationConfiguration(SshProfileSetting candidate, string executable, string configurationFile, string root)
        {
            var value = new SshProfileDiagnosticResult { Target = candidate.Address, SshAvailable = true, SshResolved = true,
                ExecutablePath = executable, ConfigPath = configurationFile, OriginalHost = candidate.Target,
                ResolvedHostName = "127.0.0.1", ResolvedUser = "fixture", ResolvedPort = "2222",
                PubkeyAuthentication = "yes", PreferredAuthentications = "publickey", IdentitiesOnly = "yes" };
            string identity = Path.Combine(root, "selected public reference"); value.IdentityFiles.Add(identity); value.IdentityPaths[identity] = identity;
            return value;
        }
        private static void AuthenticationPreparationSnapshots(string root)
        {
            var candidate = new SshProfileSetting { Target = "preparation.fixture", Name = "initial display" };
            string executable = Path.Combine(root, "ssh.exe");
            var request = new SshAuthenticationPreparationRequest(candidate, executable);
            candidate.Target = "later.fixture"; request.Candidate.Target = "discarded-copy.fixture";
            int reads = 0, signs = 0;
            SshProfileDiagnosticResult supplied = null;
            var result = SshAuthenticationPreparation.Prepare(request, CancellationToken.None,
                configuration: (copy, token, exe, ms, file) => { reads++; supplied = PreparationConfiguration(copy, exe, file, root); return supplied; },
                signing: (exe, arguments, endpoint, ms, token) => {
                    signs++; Check(exe == Path.Combine(root, "ssh-add.exe") && arguments == "-T -- " + SshConnection.Quote(Path.Combine(root, "selected public reference")) &&
                        endpoint == request.AgentEndpoint && ms > 0 && ms <= SshKeyReadiness.TimeoutMilliseconds,
                        "aggregate invokes only the readonly selected-identity signer with its frozen endpoint and bounded remaining budget");
                    return new DiagnosticProcessResult();
                });
            Check(result.State == SshAuthenticationPreparationState.LocalSigningObserved && reads == 2 && signs == 1 &&
                result.Configuration.Target == "preparation.fixture" && result.Signing.State == SshKeyReadinessState.AgentSigningVerified,
                "fresh native-configuration phases surround one local signing observation without redirecting an immutable candidate");
            Check(!request.Matches(candidate, executable, null, request.AgentEndpoint) &&
                request.Matches(new SshProfileSetting { Target = "preparation.fixture", Name = "renamed display" }, executable, null, request.AgentEndpoint) &&
                !request.Matches(request.Candidate, Path.Combine(root, "different-client", "ssh.exe"), null, request.AgentEndpoint) &&
                !request.Matches(request.Candidate, executable, null, "other-endpoint"),
                "explicit request matching rejects changed route executable or endpoint while display-only renaming does not redirect the route");
            int ambientCalls = 0;
            var relative = new SshAuthenticationPreparationRequest(request.Candidate, "ssh.exe");
            var unresolved = SshAuthenticationPreparation.Prepare(relative, CancellationToken.None,
                configuration: (copy, token, exe, ms, file) => { ambientCalls++; return null; });
            Check(unresolved.State == SshAuthenticationPreparationState.Unconfirmed && unresolved.Reason == SshAuthenticationPreparationReason.InvalidRequest && ambientCalls == 0 &&
                !relative.Matches(request.Candidate, "ssh.exe", null, relative.AgentEndpoint),
                "relative executable requests cannot preserve an ambient PATH-dependent client identity or enter the diagnostic runner");
            supplied.IdentityFiles.Clear(); supplied.ResolvedUser = "later-user";
            var escaped = result.Configuration.ForSigning(); escaped.IdentityFiles.Clear(); escaped.ResolvedUser = "caller-mutation";
            bool readOnly = false; try { ((ICollection<SshAuthenticationIdentity>)result.Configuration.Identities).Clear(); } catch (NotSupportedException) { readOnly = true; }
            Check(readOnly && result.Configuration.Identities.Count == 1 && result.Configuration.User == "fixture" && request.Candidate.Target == "preparation.fixture",
                "published effective identities, diagnostic copies and candidate copies cannot mutate the captured preparation evidence");
            Check(result.ToDisplay().Contains("вход на сервер не подтверждены") && !result.ToDisplay().Contains(root) && !result.ToDisplay().Contains(request.AgentEndpoint),
                "advisory signing display states its local proof boundary without raw paths or endpoint data");
        }
        private static void AuthenticationPreparationAdvisories(string root)
        {
            var cases = new Action<SshProfileDiagnosticResult>[] {
                p => p.PubkeyAuthentication = "false", p => p.PreferredAuthentications = "password,keyboard-interactive",
                p => p.PubkeyAuthentication = null, p => p.IdentityAgent = "none", p => p.IdentityAgent = "custom-agent",
                p => p.IdentityFiles.Clear(), p => { p.IdentityFiles.Clear(); p.IdentityFiles.Add("none"); },
                p => p.IdentityFiles.Add(Path.Combine(root, "second identity")), p => p.IdentityListLimited = true
            };
            var reasons = new[] { SshAuthenticationPreparationReason.PublicKeyNotUsed, SshAuthenticationPreparationReason.PublicKeyNotUsed,
                SshAuthenticationPreparationReason.AuthenticationPolicyUnconfirmed, SshAuthenticationPreparationReason.AgentDisabledByProfile,
                SshAuthenticationPreparationReason.CustomAgent, SshAuthenticationPreparationReason.NoIdentity, SshAuthenticationPreparationReason.NoIdentity,
                SshAuthenticationPreparationReason.MultipleIdentities, SshAuthenticationPreparationReason.IdentityListLimited };
            for (int index = 0; index < cases.Length; index++) {
                int signs = 0, reads = 0; int current = index;
                var result = SshAuthenticationPreparation.Prepare(PreparationRequest(root), CancellationToken.None,
                    configuration: (copy, token, exe, ms, file) => { reads++; var p = PreparationConfiguration(copy, exe, file, root); cases[current](p); return p; },
                    signing: (exe, args, endpoint, ms, token) => { signs++; throw new Exception("An advisory skip must not query any agent"); });
                Check(result.Reason == reasons[index] && result.State != SshAuthenticationPreparationState.LocalSigningObserved && result.Signing == null && reads == 1 && signs == 0,
                    "other-auth unknown policy none custom or ambiguous identities retain an advisory outcome without an automatic first-key or agent dependency " + index);
            }
            int calls = 0;
            var denied = SshAuthenticationPreparation.Prepare(PreparationRequest(root), CancellationToken.None,
                configuration: (copy, token, exe, ms, file) => PreparationConfiguration(copy, exe, file, root),
                signing: (exe, args, endpoint, ms, token) => { calls++; return new DiagnosticProcessResult { ExitCode = 1 }; });
            Check(calls == 1 && denied.State == SshAuthenticationPreparationState.Unconfirmed && denied.Reason == SshAuthenticationPreparationReason.SigningUnconfirmed &&
                denied.Signing.State == SshKeyReadinessState.CommandFailed && !denied.ToDisplay().Contains("зашифрован"),
                "a failed signature is not classified as encrypted-key absence or a prohibition on direct or other authentication");
            using (var lease = SshReadOnlyLease.TryAcquire(CancellationToken.None)) {
                var busy = SshAuthenticationPreparation.Prepare(PreparationRequest(root), CancellationToken.None,
                    configuration: (copy, token, exe, ms, file) => PreparationConfiguration(copy, exe, file, root));
                Check(busy.State == SshAuthenticationPreparationState.Busy && busy.Signing.State == SshKeyReadinessState.Busy,
                    "aggregate respects the existing shared agent lease instead of bypassing another owned operation");
            }
        }
        private static void AuthenticationPreparationBudgetAndFreshness(string root)
        {
            long clock = 0; int reads = 0, signs = 0, childBudget = 0; var budgets = new List<int>();
            var request = PreparationRequest(root);
            var observed = SshAuthenticationPreparation.Prepare(request, CancellationToken.None,
                configuration: (copy, token, exe, ms, file) => { budgets.Add(ms); reads++; clock = reads == 1 ? 5000 : 6900; return PreparationConfiguration(copy, exe, file, root); },
                signing: (exe, args, endpoint, ms, token) => { signs++; childBudget = ms; clock = 6500; return new DiagnosticProcessResult(); },
                elapsedMilliseconds: () => clock);
            Check(observed.State == SshAuthenticationPreparationState.LocalSigningObserved && budgets.SequenceEqual(new[] { 7000, 500 }) && childBudget == 2000 && signs == 1,
                "configuration signing and final configuration share one decreasing total budget rather than fresh per-phase deadlines");
            for (int mode = 0; mode < 3; mode++) {
                clock = 0; reads = signs = 0; int current = mode;
                var result = SshAuthenticationPreparation.Prepare(request, CancellationToken.None,
                    configuration: (copy, token, exe, ms, file) => { reads++; if (current == 0 || current == 2 && reads == 2) clock = 7000; return PreparationConfiguration(copy, exe, file, root); },
                    signing: (exe, args, endpoint, ms, token) => { signs++; if (current == 1) clock = 7000; return new DiagnosticProcessResult(); }, elapsedMilliseconds: () => clock);
                Check(result.State == SshAuthenticationPreparationState.TimedOut && (current != 0 || signs == 0),
                    "late configuration signing or final publication cannot turn an expired shared deadline into readiness " + mode);
            }
            using (var cancelled = new CancellationTokenSource()) {
                cancelled.Cancel(); int calls = 0;
                var result = SshAuthenticationPreparation.Prepare(request, cancelled.Token,
                    configuration: (copy, token, exe, ms, file) => { calls++; return null; });
                Check(result.State == SshAuthenticationPreparationState.Cancelled && calls == 0,
                    "pre-cancelled preparation touches no configuration runner or child");
            }
            var signingTimeout = SshAuthenticationPreparation.Prepare(request, CancellationToken.None,
                configuration: (copy, token, exe, ms, file) => PreparationConfiguration(copy, exe, file, root),
                signing: (exe, args, endpoint, ms, token) => { throw new TimeoutException(); }, elapsedMilliseconds: () => 0);
            Check(signingTimeout.State == SshAuthenticationPreparationState.TimedOut && signingTimeout.Reason == SshAuthenticationPreparationReason.SigningTimedOut &&
                !signingTimeout.ToDisplay().Contains("Общий срок"),
                "a bounded signing-phase timeout is distinct from expiration of the aggregate total budget");
            for (int mode = 0; mode < 3; mode++) {
                reads = 0; int current = mode;
                var changed = SshAuthenticationPreparation.Prepare(request, CancellationToken.None,
                    configuration: (copy, token, exe, ms, file) => {
                        reads++; var p = PreparationConfiguration(copy, exe, file, root);
                        if (reads == 2) { if (current == 0) p.ResolvedUser = "changed-user"; else if (current == 1) p.IdentityFiles.Add("second identity"); else p.SshResolved = false; }
                        return p;
                    }, signing: (exe, args, endpoint, ms, token) => new DiagnosticProcessResult());
                Check(changed.State == SshAuthenticationPreparationState.Changed && reads == 2,
                    "a changed effective scope or failed final configuration invalidates the earlier signing observation " + mode);
            }
            string previous = Environment.GetEnvironmentVariable("SSH_AUTH_SOCK");
            try {
                Environment.SetEnvironmentVariable("SSH_AUTH_SOCK", "later-endpoint"); int calls = 0;
                var stale = SshAuthenticationPreparation.Prepare(request, CancellationToken.None,
                    configuration: (copy, token, exe, ms, file) => { calls++; return null; });
                Check(stale.State == SshAuthenticationPreparationState.Changed && calls == 0,
                    "an immutable request with a stale endpoint is rejected before configuration or agent access");
                Environment.SetEnvironmentVariable("SSH_AUTH_SOCK", previous);
                var changed = SshAuthenticationPreparation.Prepare(request, CancellationToken.None,
                    configuration: (copy, token, exe, ms, file) => PreparationConfiguration(copy, exe, file, root),
                    signing: (exe, args, endpoint, ms, token) => { Environment.SetEnvironmentVariable("SSH_AUTH_SOCK", "during-signing"); return new DiagnosticProcessResult(); });
                Check(changed.State == SshAuthenticationPreparationState.Changed,
                    "endpoint mutation during signing cannot publish a current local signing observation");
            } finally { Environment.SetEnvironmentVariable("SSH_AUTH_SOCK", previous); }
        }
        private static string PreparationNativeConfig(string identity, string user = "fixture")
        { return "Host preparation.fixture\n HostName 127.0.0.1\n User " + user + "\n Port 2222\n IdentityFile " + ConfigQuoted(identity) +
            "\n IdentitiesOnly yes\n PreferredAuthentications publickey\n PubkeyAuthentication yes\n"; }
        private static void AuthenticationPreparationNative(string root)
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") { Console.WriteLine("SKIP: native advisory preparation requires isolated Windows CI"); return; }
            string previous = Environment.GetEnvironmentVariable("SSH_AUTH_SOCK"), executable = OpenSshClient.Executable;
            string config = Path.Combine(root, "native-config"), selected = Path.Combine(root, "native selected public key");
            try {
                using (var rsa = new RSACryptoServiceProvider(2048)) {
                    rsa.PersistKeyInCsp = false; byte[] blob = ReadinessPublicBlob(rsa);
                    File.WriteAllText(selected, "ssh-rsa " + Convert.ToBase64String(blob) + " disposable-preparation-fixture\n");
                    File.WriteAllText(config, PreparationNativeConfig(selected), new UTF8Encoding(false));
                    for (int mode = 0; mode < 3; mode++) using (var agent = new ReadinessAgent(rsa, blob, mode)) {
                        Environment.SetEnvironmentVariable("SSH_AUTH_SOCK", @"\\.\pipe\" + agent.Name);
                        var request = new SshAuthenticationPreparationRequest(new SshProfileSetting { Target = "preparation.fixture" }, executable, config);
                        var result = SshAuthenticationPreparation.Prepare(request, CancellationToken.None);
                        AgentProbeSettled(agent.Work);
                        Check(agent.Requests == 1 && result.Configuration.HostName == "127.0.0.1" && result.Configuration.User == "fixture" &&
                            result.Configuration.Identities.Count == 1 && result.Configuration.Identities[0].Path == selected &&
                            result.State == (mode == 0 ? SshAuthenticationPreparationState.LocalSigningObserved : SshAuthenticationPreparationState.Unconfirmed),
                            "actual isolated ssh-G and sibling ssh-add compose one valid rejected or corrupted local signing observation without an SSH connection " + mode);
                    }
                    for (int mode = 0; mode < 3; mode++) using (var agent = new ReadinessAgent(rsa, blob, 3)) using (var cancellation = new CancellationTokenSource()) {
                        Environment.SetEnvironmentVariable("SSH_AUTH_SOCK", @"\\.\pipe\" + agent.Name);
                        File.WriteAllText(config, PreparationNativeConfig(selected), new UTF8Encoding(false));
                        var request = new SshAuthenticationPreparationRequest(new SshProfileSetting { Target = "preparation.fixture" }, executable, config);
                        var pending = Task.Run(() => SshAuthenticationPreparation.Prepare(request, cancellation.Token));
                        try {
                            Check(agent.Entered.Wait(5000), "native aggregate reaches its isolated signing child after the actual configuration phase " + mode);
                            if (mode == 0) { File.WriteAllText(config, PreparationNativeConfig(selected, "changed-user"), new UTF8Encoding(false)); agent.Release.Set(); }
                            if (mode == 1) cancellation.Cancel();
                            AgentProbeSettled(pending);
                            Check(pending.Result.State == (mode == 0 ? SshAuthenticationPreparationState.Changed : mode == 1 ? SshAuthenticationPreparationState.Cancelled : SshAuthenticationPreparationState.TimedOut) &&
                                SshReadOnlyLeaseAvailable(),
                                "native changed config cancellation and deadline settle the owned signing child before releasing the shared lease " + mode);
                        } finally { agent.Release.Set(); }
                    }
                    Environment.SetEnvironmentVariable("SSH_AUTH_SOCK", @"\\.\pipe\progo-no-real-agent-" + Guid.NewGuid().ToString("N"));
                    File.WriteAllText(config, PreparationNativeConfig(selected) + " IdentityFile " + ConfigQuoted(selected + ".other") + "\n", new UTF8Encoding(false));
                    var multiple = SshAuthenticationPreparation.Prepare(new SshAuthenticationPreparationRequest(new SshProfileSetting { Target = "preparation.fixture" }, executable, config), CancellationToken.None);
                    Check(multiple.Reason == SshAuthenticationPreparationReason.MultipleIdentities && multiple.Signing == null && multiple.Configuration.Identities.Count == 2,
                        "actual effective multiple identities remain advisory without selecting the first or contacting a real agent");
                    File.WriteAllText(config, PreparationNativeConfig(selected) + " IdentityAgent none\n", new UTF8Encoding(false));
                    var none = SshAuthenticationPreparation.Prepare(new SshAuthenticationPreparationRequest(new SshProfileSetting { Target = "preparation.fixture" }, executable, config), CancellationToken.None);
                    Check(none.Reason == SshAuthenticationPreparationReason.AgentDisabledByProfile && none.Signing == null,
                        "actual IdentityAgent none profile has no mandatory default-agent probe or synthetic direct-key refusal");
                    string directKey = Path.Combine(root, "direct unencrypted fixture key");
                    var generated = DiagnosticProcess.RunReadOnly(OpenSshClient.KeygenExecutable(executable),
                        "-q -t ed25519 -N \"\" -C disposable-preparation -f " + SshConnection.Quote(directKey),
                        Environment.GetEnvironmentVariable("SSH_AUTH_SOCK"), 7000, CancellationToken.None);
                    Check(generated.ExitCode == 0 && !generated.Truncated && File.Exists(directKey),
                        "isolated sibling keygen creates a disposable unencrypted direct key without an agent or server connection");
                    File.WriteAllText(config, "Host *\n IdentityAgent none\n PreferredAuthentications publickey\n PubkeyAuthentication yes\n", new UTF8Encoding(false));
                    var direct = SshAuthenticationPreparation.Prepare(new SshAuthenticationPreparationRequest(new SshProfileSetting {
                        Target = "direct-fixture", Server = "127.0.0.1", User = "fixture", Port = 2222, IdentityFile = directKey }, executable, config), CancellationToken.None);
                    Check(direct.Reason == SshAuthenticationPreparationReason.AgentDisabledByProfile && direct.Signing == null &&
                        direct.Configuration.Identities.Any(identity => identity.Path == directKey) && direct.Configuration.IdentitiesOnly == "yes",
                        "actual direct-key arguments preserve the selected unencrypted key and IdentitiesOnly without making the unavailable agent mandatory; server authentication remains untested");
                }
            } finally { Environment.SetEnvironmentVariable("SSH_AUTH_SOCK", previous); }
        }
    }
}

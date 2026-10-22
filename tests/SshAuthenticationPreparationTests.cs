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
                AuthenticationPreparationRealTunnel(root);
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
            var launchObservation = (ISshLaunchPreparationObservation)result;
            Check(launchObservation.AgentEndpoint == request.AgentEndpoint &&
                launchObservation.MatchesLaunchRequest(request.Candidate, executable, request.AgentEndpoint) &&
                !launchObservation.MatchesLaunchRequest(candidate, executable, request.AgentEndpoint) &&
                !launchObservation.MatchesLaunchRequest(request.Candidate, executable, "changed-fixture-endpoint") &&
                launchObservation.ToDisplay() == result.ToDisplay(),
                "bootstrap observation retains request freshness and safe guidance without claiming private-key or server authorization");
            var cancelledObservation = new SshAuthenticationPreparationResult(SshAuthenticationPreparationState.Cancelled,
                SshAuthenticationPreparationReason.Cancelled, request, result.Configuration, result.Signing);
            var changedObservation = new SshAuthenticationPreparationResult(SshAuthenticationPreparationState.Changed,
                SshAuthenticationPreparationReason.Changed, request, result.Configuration, result.Signing);
            Check(!((ISshLaunchPreparationObservation)cancelledObservation).MatchesLaunchRequest(request.Candidate, executable, request.AgentEndpoint) &&
                !((ISshLaunchPreparationObservation)changedObservation).MatchesLaunchRequest(request.Candidate, executable, request.AgentEndpoint),
                "cancelled or changed preparation observations cannot admit a later SSH launch with otherwise identical request fields");
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
        private static void AuthenticationPreparationRealTunnel(string parent)
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") {
                Console.WriteLine("NOT_RUN: combined native SSH fixture requires isolated Windows CI"); return;
            }
            AuthenticationPreparationRealTunnelCase(parent, null);
            string previous = Environment.GetEnvironmentVariable("SSH_AUTH_SOCK");
            using (var rsa = new RSACryptoServiceProvider(2048)) {
                rsa.PersistKeyInCsp = false;
                using (var agent = new TunnelFixtureAgent(rsa)) try {
                    Environment.SetEnvironmentVariable("SSH_AUTH_SOCK", @"\\.\pipe\" + agent.Name);
                    AuthenticationPreparationRealTunnelCase(parent, agent);
                } finally { Environment.SetEnvironmentVariable("SSH_AUTH_SOCK", previous); }
            }
        }

        private static void AuthenticationPreparationRealTunnelCase(string parent, TunnelFixtureAgent agent)
        {
            string python = Environment.GetEnvironmentVariable("PROGO_TEST_SSH_PYTHON");
            string script = Environment.GetEnvironmentVariable("PROGO_TEST_SSH_FIXTURE_SCRIPT");
            if (String.IsNullOrEmpty(python) || !File.Exists(python) || String.IsNullOrEmpty(script) || !File.Exists(script))
                throw new InvalidOperationException("combined SSH fixture runtime was not prepared");
            string root = Path.Combine(parent, "real-tunnel-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
            string key = Path.Combine(root, "selected key"), config = Path.Combine(root, "config");
            string executable = OpenSshClient.Executable;
            if (agent == null) {
                var generated = DiagnosticProcess.RunReadOnly(OpenSshClient.KeygenExecutable(executable),
                    "-q -t rsa -b 2048 -N \"\" -C disposable-combined-ssh -f " + SshConnection.Quote(key), null, 10000, CancellationToken.None);
                Check(generated.ExitCode == 0 && File.Exists(key + ".pub"), "combined fixture generates only its own disposable direct key");
            } else {
                string reference = "ssh-rsa " + Convert.ToBase64String(agent.PublicBlob) + " disposable-agent-only\n";
                File.WriteAllText(key, reference); File.WriteAllText(key + ".pub", reference);
                Check(File.ReadAllText(key).StartsWith("ssh-rsa "), "combined agent fixture exposes public references only; private key exists only in its owned agent");
            }
            Process server = null; ProxyService proxy = null; Task<bool> start = null;
            Task<string> output = null, errors = null;
            try {
                server = Process.Start(new ProcessStartInfo(python, SshConnection.Quote(script) + " --public-key " +
                    SshConnection.Quote(key + ".pub") + " --directory " + SshConnection.Quote(root)) {
                    UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true
                });
                if (server == null) throw new IOException("owned SSH fixture process did not start");
                output = server.StandardOutput.ReadToEndAsync(); errors = server.StandardError.ReadToEndAsync();
                var ready = Path.Combine(root, "ready.json"); var watch = Stopwatch.StartNew();
                while (!File.Exists(ready)) {
                    if (server.HasExited || watch.ElapsedMilliseconds >= 7000) throw new TimeoutException("owned SSH server did not publish readiness within seven seconds");
                    Thread.Sleep(20);
                }
                var metadata = new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<Dictionary<string, object>>(File.ReadAllText(ready));
                int sshPort = Convert.ToInt32(metadata["ssh_port"]), httpPort = Convert.ToInt32(metadata["http_port"]);
                string known = Path.Combine(root, "known_hosts");
                File.WriteAllText(known, "[127.0.0.1]:" + sshPort + " " + (string)metadata["host_key"] + "\n");
                File.WriteAllText(config, "Host *\n" + (agent == null ? " IdentityAgent none\n" : "") + " StrictHostKeyChecking yes\n UserKnownHostsFile " +
                    ConfigQuoted(known) + "\n GlobalKnownHostsFile none\n PreferredAuthentications publickey\n PubkeyAuthentication yes\n ConnectTimeout 2\n");
                var settings = AppSettings.Defaults(); settings.SocksHost = "127.0.0.1"; settings.AutoSwitchSshProfile = false; settings.AutoRestartSocks = true;
                var reserve = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
                try {
                    reserve.Start(); settings.SocksPort = ((System.Net.IPEndPoint)reserve.LocalEndpoint).Port;
                } finally { reserve.Stop(); }
                settings.SshProfile = "owned-direct-fixture";
                settings.SshProfiles = new List<SshProfileSetting> { new SshProfileSetting { Target = settings.SshProfile,
                    Server = "127.0.0.1", User = "progo-fixture", Port = sshPort, IdentityFile = key } };
                DateTime now = DateTime.UtcNow; int preparations = 0;
                var preparationTimes = new System.Collections.Concurrent.ConcurrentQueue<long>();
                proxy = new ProxyService(() => settings, s => { throw new InvalidOperationException("fixture must not persist settings"); },
                    executable, () => now, false, "-F " + SshConnection.Quote(config),
                    prepareAuthentication: (candidate, file, token) => {
                        Interlocked.Increment(ref preparations);
                        var elapsed = Stopwatch.StartNew();
                        try { return SshAuthenticationPreparation.Prepare(new SshAuthenticationPreparationRequest(candidate, file, config), token); }
                        finally { preparationTimes.Enqueue(elapsed.ElapsedMilliseconds); }
                    });
                var startupWatch = Stopwatch.StartNew();
                start = proxy.StartTunnelAsync(CancellationToken.None);
                bool completed = start.Wait(15000);
                if (!completed || !start.Result || !proxy.HasOwnedProcess || preparations != 1) {
                    var observed = proxy.AuthenticationPreparation as SshAuthenticationPreparationResult;
                    Console.WriteLine("COMBINED_SSH_START_FAILURE completed=" + completed + ";status=" + start.Status +
                        ";preparations=" + preparations + ";owned=" + proxy.HasOwnedProcess + ";local_state=" + (observed == null ? "none" : observed.State.ToString()) +
                        ";local_reason=" + (observed == null ? "none" : observed.Reason.ToString()) + ";classified_ssh_reason=" + (proxy.StartupError ?? "none").Replace("\r", " ").Replace("\n", " "));
                }
                Check(completed && start.Result && proxy.HasOwnedProcess && preparations == 1,
                    "real preparation and common ProxyService startup reach owned native SSH and confirmed SOCKS");
                var preparation = proxy.AuthenticationPreparation as SshAuthenticationPreparationResult;
                Check(preparation != null && (agent == null ? preparation.Reason == SshAuthenticationPreparationReason.AgentDisabledByProfile && preparation.Signing == null :
                    preparation.State == SshAuthenticationPreparationState.LocalSigningObserved && agent.Signatures >= 2),
                    agent == null ? "direct unencrypted key reaches real authentication without making the default agent mandatory" :
                    "selected public reference signs in actual preparation and then authenticates real native SSH using only the owned agent");
                AuthenticationFixtureHttp(settings, httpPort);
                AuthenticationFixtureMetrics(proxy, agent == null ? "direct" : "agent", "startup", startupWatch.ElapsedMilliseconds, preparationTimes.ToArray());
                int oldPid = proxy.CurrentPid.Value;
                using (var child = Process.GetProcessById(oldPid)) { child.Kill(); Check(child.WaitForExit(3000), "only the fixture owned SSH child is interrupted"); }
                proxy.PollRecovery(); Check(proxy.NextRecoveryUtc.HasValue, "real owned SSH interruption schedules recovery");
                now = now.AddSeconds(6); startupWatch.Restart(); proxy.PollRecovery(); watch.Restart();
                while (!ConnectionHealthMonitor.CheckSocks(settings, CancellationToken.None)) {
                    if (watch.ElapsedMilliseconds >= 5000) throw new TimeoutException("real recovered SSH did not provide SOCKS within five seconds");
                    Thread.Sleep(20);
                }
                Check(preparations == 2 && proxy.AutomaticRestarts == 1 && proxy.CurrentPid.HasValue && proxy.CurrentPid != oldPid,
                    "automatic recovery repeats actual preparation and starts exactly one fresh native SSH child");
                AuthenticationFixtureHttp(settings, httpPort);
                AuthenticationFixtureMetrics(proxy, agent == null ? "direct" : "agent", "recovery", startupWatch.ElapsedMilliseconds, preparationTimes.ToArray());
                if (agent != null) Check(agent.Signatures >= 4, "real recovery repeats local signing and actual server authentication against the same selected owned agent key");
            }
            finally {
                try {
                    if (proxy != null) {
                        var stop = proxy.StopTunnelAsync();
                        Check(stop.Wait(5000) && stop.Result && (start == null || start.IsCompleted) && !proxy.HasOwnedProcess,
                            "combined fixture settles startup and owned SSH cleanup before server disposal");
                    }
                }
                finally {
                    try { if (proxy != null) proxy.Dispose(); }
                    finally {
                        if (server != null) try {
                            bool graceful = false;
                            try {
                                File.WriteAllText(Path.Combine(root, "stop"), "stop");
                                graceful = server.WaitForExit(7000);
                            } finally {
                                if (!server.HasExited) { server.Kill(); if (!server.WaitForExit(3000)) throw new TimeoutException("owned SSH server process did not terminate"); }
                            }
                            Check(output != null && errors != null && output.Wait(3000) && errors.Wait(3000) && graceful && server.ExitCode == 0 &&
                                output.Result.Contains("OWNED_SSH_FIXTURE_CLEANUP_COMPLETE"), "owned SSH server exits with explicit cleanup; timeout is never PASS");
                        } finally { server.Dispose(); }
                    }
                }
            }
        }

        private static void AuthenticationFixtureMetrics(ProxyService proxy, string mode, string phase, long elapsedMs, long[] preparationMs)
        {
            // Isolated runner measurements only, never owner-PC/HDD acceptance.
            try {
                using (var child = Process.GetProcessById(proxy.CurrentPid.Value)) {
                    Console.WriteLine("COMBINED_SSH_METRICS mode=" + mode + ";phase=" + phase + ";elapsed_ms=" + elapsedMs +
                        ";preparation_ms=" + String.Join(",", preparationMs.Select(x => x.ToString(System.Globalization.CultureInfo.InvariantCulture))) +
                        ";ssh_cpu_ms=" + child.TotalProcessorTime.TotalMilliseconds.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                        ";ssh_working_set_bytes=" + child.WorkingSet64 + ";ssh_peak_working_set_bytes=" + child.PeakWorkingSet64);
                }
            } catch (Exception error) {
                Console.WriteLine("NOT_RUN: combined SSH process counters unavailable: " + error.GetType().Name);
            }
        }

        // A multi-connection, test-owned Windows agent. LIST and SIGN are real
        // protocol operations; OpenSSH and the SSH server verify the signatures.
        // No mutating add/remove protocol or shared agent endpoint is available.
        private sealed class TunnelFixtureAgent : IDisposable
        {
            internal readonly string Name = "progo-tunnel-agent-" + Guid.NewGuid().ToString("N");
            internal readonly byte[] PublicBlob;
            internal int Signatures;
            private readonly RSACryptoServiceProvider rsa;
            private readonly object gate = new object();
            private readonly List<System.IO.Pipes.NamedPipeServerStream> pipes = new List<System.IO.Pipes.NamedPipeServerStream>();
            private readonly List<Task> clients = new List<Task>();
            private readonly ManualResetEventSlim ready = new ManualResetEventSlim();
            private readonly Task accept;
            private volatile bool stopping;

            internal TunnelFixtureAgent(RSACryptoServiceProvider rsa)
            {
                this.rsa = rsa; PublicBlob = ReadinessPublicBlob(rsa);
                accept = Task.Factory.StartNew(Accept, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
                if (!ready.Wait(3000)) { Dispose(); throw new TimeoutException("owned agent did not publish its pipe within three seconds"); }
            }
            private void Accept()
            {
                while (!stopping) {
                    System.IO.Pipes.NamedPipeServerStream pipe;
                    lock (gate) {
                        if (stopping) return;
                        pipe = new System.IO.Pipes.NamedPipeServerStream(Name, System.IO.Pipes.PipeDirection.InOut, 16,
                            System.IO.Pipes.PipeTransmissionMode.Byte, System.IO.Pipes.PipeOptions.Asynchronous);
                        pipes.Add(pipe);
                        ready.Set();
                    }
                    try { pipe.WaitForConnection(); }
                    catch (IOException) { if (stopping) return; throw; }
                    catch (ObjectDisposedException) { if (stopping) return; throw; }
                    lock (gate) {
                        if (stopping) return;
                        var ownedPipe = pipe;
                        clients.Add(Task.Factory.StartNew(() => Serve(ownedPipe), CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default));
                    }
                }
            }
            private void Serve(System.IO.Pipes.NamedPipeServerStream pipe)
            {
                try {
                    while (!stopping) {
                        byte[] request = ReadinessReadFrame(pipe), body = new byte[] { 5 };
                        if (request.Length == 1 && request[0] == 11) {
                            body = new byte[] { 12, 0, 0, 0, 1 }.Concat(ReadinessWire(PublicBlob))
                                .Concat(ReadinessWire(Encoding.ASCII.GetBytes("disposable-owned-agent"))).ToArray();
                        } else if (request[0] == 13) {
                            using (var bytes = new MemoryStream(request)) using (var reader = new BinaryReader(bytes)) {
                                reader.ReadByte(); byte[] blob = ReadinessReadString(reader), data = ReadinessReadString(reader); uint flags = ReadinessReadUInt(reader);
                                if (bytes.Position == bytes.Length && blob.SequenceEqual(PublicBlob) && data.Length > 0 && (flags == 0 || flags == 2 || flags == 4)) {
                                    string algorithm = flags == 2 ? "rsa-sha2-256" : flags == 4 ? "rsa-sha2-512" : "ssh-rsa";
                                    byte[] signature; lock (rsa) signature = rsa.SignData(data, flags == 2 ? "SHA256" : flags == 4 ? "SHA512" : "SHA1");
                                    Interlocked.Increment(ref Signatures);
                                    body = new byte[] { 14 }.Concat(ReadinessWire(ReadinessWire(Encoding.ASCII.GetBytes(algorithm)).Concat(ReadinessWire(signature)).ToArray())).ToArray();
                                }
                            }
                        }
                        byte[] response = ReadinessWire(body); pipe.Write(response, 0, response.Length); pipe.Flush();
                    }
                } catch (IOException) { /* Normal native client disconnect or owned cancellation. */ }
                catch (ObjectDisposedException) { if (!stopping) throw; }
                finally { pipe.Dispose(); }
            }
            public void Dispose()
            {
                lock (gate) { stopping = true; foreach (var pipe in pipes) pipe.Dispose(); }
                try {
                    if (!accept.Wait(5000)) throw new TimeoutException("owned agent accept loop cleanup deadline");
                } finally {
                    Task[] work; lock (gate) work = clients.ToArray();
                    try { if (!Task.WaitAll(work, 5000)) throw new TimeoutException("owned agent client cleanup deadline"); }
                    finally { ready.Dispose(); }
                }
            }
        }

        private static void AuthenticationFixtureHttp(AppSettings settings, int httpPort)
        {
            using (var client = new System.Net.Sockets.TcpClient()) {
                var connect = client.ConnectAsync("127.0.0.1", settings.SocksPort);
                if (!connect.Wait(3000)) throw new TimeoutException("owned SOCKS connect deadline");
                connect.GetAwaiter().GetResult(); client.ReceiveTimeout = 3000; client.SendTimeout = 3000;
                var stream = client.GetStream(); stream.Write(new byte[] { 5, 1, 0 }, 0, 3);
                Check(HealthRead(stream, 2).SequenceEqual(new byte[] { 5, 0 }), "real tunnel negotiates SOCKS5 before HTTP forwarding");
                byte[] target = { 5, 1, 0, 1, 127, 0, 0, 1, (byte)(httpPort >> 8), (byte)httpPort };
                stream.Write(target, 0, target.Length); var reply = HealthRead(stream, 10);
                Check(reply[0] == 5 && reply[1] == 0, "real authenticated SSH permits only its owned HTTP endpoint");
                byte[] request = Encoding.ASCII.GetBytes("GET / HTTP/1.0\r\nHost: fixture\r\n\r\n"); stream.Write(request, 0, request.Length);
                var response = new StringBuilder(); var buffer = new byte[4096]; int count;
                while ((count = stream.Read(buffer, 0, buffer.Length)) != 0) {
                    response.Append(Encoding.ASCII.GetString(buffer, 0, count));
                    if (response.Length > 65536) throw new IOException("owned HTTP response exceeded fixture bound");
                }
                Check(response.ToString().Contains("progo-owned-ssh-http-fixture"), "actual selected key and SSH carry SOCKS5 HTTP payload; local signing alone is insufficient");
            }
        }
    }
}

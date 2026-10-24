using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        private static void SshAuthPolicyWorkflow()
        {
            int before = passed; Console.WriteLine("SSH public-key policy checks BEGIN");
            string root = Path.GetFullPath(Path.Combine(work, "auth-policy-" + Guid.NewGuid().ToString("N")));
            CreateAuthPolicyFixture(root);
            try {
                File.WriteAllText(Path.Combine(root, "ssh-add.exe"), ""); File.WriteAllText(Path.Combine(root, "ssh-keygen.exe"), "");
                AuthPolicyValues(); AuthPolicyScope(root); AuthPolicyDiagnosticContract(); AuthPolicyNative(root); AuthPolicyForm();
            } finally { DeleteAuthPolicyFixture(root, root); }
            Console.WriteLine("SSH public-key policy checks PASS: " + (passed - before));
        }
        // Authorized test-only ACL creation is confined to a fresh fixture on Windows CI.
        // No owner setters, existing files, ancestor ACLs, user SSH files or runtime policy changes.
        private static void CheckAuthPolicyFixturePath(string root, string path)
        {
            string parent = Path.GetFullPath(work).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string fullRoot = Path.GetFullPath(root), full = Path.GetFullPath(path);
            Check(String.Equals(Path.GetDirectoryName(fullRoot) + Path.DirectorySeparatorChar, parent, StringComparison.OrdinalIgnoreCase)
                && System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileName(fullRoot), @"\Aauth-policy-[0-9a-f]{32}\z")
                && (String.Equals(full, fullRoot, StringComparison.OrdinalIgnoreCase)
                    || full.StartsWith(fullRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)), "auth fixture path stays within its exact new GUID directory");
            for (string current = full; !String.IsNullOrEmpty(current); current = Path.GetDirectoryName(current)) {
                try { Check((File.GetAttributes(current) & FileAttributes.ReparsePoint) == 0, "auth fixture path and ancestors have no reparse points"); }
                catch (FileNotFoundException) { }
                catch (DirectoryNotFoundException) { }
            }
        }
        private static void CreateAuthPolicyFixture(string root)
        {
            CheckAuthPolicyFixturePath(root, root);
            Check(!Directory.Exists(root) && !File.Exists(root), "auth fixture refuses pre-existing resources");
            bool isolatedCi = Environment.OSVersion.Platform == PlatformID.Win32NT
                && Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true"
                && Environment.GetEnvironmentVariable("RUNNER_OS") == "Windows";
            if (!isolatedCi) { Directory.CreateDirectory(root); return; } // Preserve local behavior; no local ACL mutation authorized.
            using (var current = WindowsIdentity.GetCurrent()) {
                var security = new DirectorySecurity(); security.SetAccessRuleProtection(true, false);
                foreach (var sid in new[] { current.User, new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
                    new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null) }.Distinct())
                    security.AddAccessRule(new FileSystemAccessRule(sid, FileSystemRights.FullControl,
                        InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
                Directory.CreateDirectory(root, security); // Security descriptor applies only while creating this new directory.
            }
            try { CheckAuthPolicyFixturePath(root, root); CheckAuthPolicyFixtureAcl(root, true); }
            catch { DeleteAuthPolicyFixture(root, root); throw; }
        }
        private static void CheckAuthPolicyFixtureAcl(string path, bool directory)
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true" || Environment.GetEnvironmentVariable("RUNNER_OS") != "Windows") return;
            var acl = directory ? (FileSystemSecurity)Directory.GetAccessControl(path, AccessControlSections.Access)
                : File.GetAccessControl(path, AccessControlSections.Access);
            using (var current = WindowsIdentity.GetCurrent()) {
                var system = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
                var admins = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
                var rules = acl.GetAccessRules(true, true, typeof(SecurityIdentifier)).Cast<FileSystemAccessRule>().ToArray();
                Check((!directory || acl.AreAccessRulesProtected) && rules.Length > 0
                    && rules.All(rule => rule.AccessControlType == AccessControlType.Allow
                        && (rule.IdentityReference.Equals(current.User) || rule.IdentityReference.Equals(system) || rule.IdentityReference.Equals(admins)))
                    && rules.Any(rule => rule.IdentityReference.Equals(current.User) && (rule.FileSystemRights & FileSystemRights.FullControl) == FileSystemRights.FullControl),
                    "new auth fixture ACL permits only test-user SYSTEM Administrators with usable test-user access");
            }
        }
        private static void DeleteAuthPolicyFixture(string root, string path)
        {
            CheckAuthPolicyFixturePath(root, path); // Fail closed; never traverse links or scan siblings.
            if (Directory.Exists(path)) {
                foreach (string entry in Directory.GetFileSystemEntries(path)) DeleteAuthPolicyFixture(root, entry);
                CheckAuthPolicyFixturePath(root, path); Directory.Delete(path, false);
            } else if (File.Exists(path)) { CheckAuthPolicyFixturePath(root, path); File.Delete(path); }
        }
        private static void AuthPolicyValues()
        {
            Check(SshPublicKeyPolicy.Read(false, "no", "password") == SshPublicKeyPolicyState.Unknown,
                "unresolved SSH output cannot establish client authentication policy");
            Check(SshPublicKeyPolicy.Read(true, "no", null) == SshPublicKeyPolicyState.Disabled &&
                SshPublicKeyPolicy.Read(true, " NO ", "PRIVATE_MALFORMED") == SshPublicKeyPolicyState.Disabled,
                "effective explicit public-key disable does not require an invented preferred-method default");
            Check(SshPublicKeyPolicy.Read(true, "false", "publickey") == SshPublicKeyPolicyState.Disabled,
                "native ssh-G boolean false is recognized independently of config keyword spelling");
            foreach (string mode in new[] { "yes", "true", "unbound", "host-bound" })
                Check(SshPublicKeyPolicy.Read(true, mode, "keyboard-interactive,publickey,password") == SshPublicKeyPolicyState.AllowedByClient,
                    "recognized client key mode permits observations without declaring readiness: " + mode);
            foreach (string list in new[] { "password,keyboard-interactive", "publickey-other", "hostbased,password" })
                Check(SshPublicKeyPolicy.Read(true, "yes", list) == SshPublicKeyPolicyState.Excluded,
                    "exact preferred-method list can exclude publickey without substring matching: " + list);
            foreach (string mode in new[] { "", "future-mode", "no\r\nPRIVATE_POLICY" })
                Check(SshPublicKeyPolicy.Read(true, mode, "publickey,password") == SshPublicKeyPolicyState.Unknown,
                    "missing future or malformed key mode stays unknown rather than claiming readiness");
            string[] bad = { null, "", "publickey,,password", "publickey,password ", "PUBLICKEY", "+publickey", "publickey\npassword", "publickey\0",
                new string('p', 2049), String.Join(",", Enumerable.Repeat("password", 33)), new string('p', 65) };
            foreach (string value in bad) Check(SshPublicKeyPolicy.Read(true, "yes", value) == SshPublicKeyPolicyState.Unknown,
                "missing malformed oversized or unsupported preferred syntax never silently disables key observations");
            Check(SshPublicKeyPolicy.Read(true, "yes", "publickey,publickey") == SshPublicKeyPolicyState.AllowedByClient &&
                SshPublicKeyPolicy.Read(true, null, "password") == SshPublicKeyPolicyState.Excluded,
                "duplicate publickey is exact presence while an explicit excluding list stands independently of missing key mode");
            foreach (SshPublicKeyPolicyState state in Enum.GetValues(typeof(SshPublicKeyPolicyState)))
                Check(!String.IsNullOrWhiteSpace(SshPublicKeyPolicy.ToDisplay(state)) && !SshPublicKeyPolicy.ToDisplay(state).Contains("PRIVATE_") &&
                    !SshPublicKeyPolicy.ToDisplay(state).Contains("Ключ готов"), "policy display is bounded safe text without an authentication claim: " + state);
        }
        private static SshProfileDiagnosticResult AuthPolicyProfile(string root, string mode, string preferred)
        {
            var profile = KeyMatchProfile(root, "first"); profile.PubkeyAuthentication = mode; profile.PreferredAuthentications = preferred;
            profile.AgentProbe = new SshAgentProbeResult(SshAgentProbeState.Completed, new SshAgentServiceSnapshot { RuntimeState = SshAgentRuntimeState.Running });
            return profile;
        }
        private static void AuthPolicyScope(string root)
        {
            int calls = 0;
            Func<string, string, int, CancellationToken, DiagnosticProcessResult> run = (e, a, m, t) => { calls++; return KeyMatchCapture(AgentFingerprintA); };
            foreach (bool disabled in new[] { true, false }) {
                var profile = AuthPolicyProfile(root, disabled ? "no" : "yes", disabled ? "publickey,password" : "password,keyboard-interactive");
                using (var held = SshReadOnlyLease.TryAcquire(CancellationToken.None)) {
                    Check(held != null, "policy exclusion test holds the existing read-only lease");
                    var inventory = SshAgentInventory.ForProfile(profile, CancellationToken.None, run);
                    var matches = SshProfileKeyMatching.Check(profile, CancellationToken.None, run);
                    Check(inventory.State == SshAgentInventoryState.NotUsedByAuthentication && matches.State == SshProfileKeyMatchState.NotUsedByAuthentication &&
                        calls == 0 && matches.Identities.Count == 0 && inventory.ContainsFingerprint(AgentFingerprintA) == SshAgentFingerprintPresence.Unknown,
                        "excluded key method returns a scoped skip instead of Busy empty agent missing key or a child");
                    profile.AgentInventory = inventory; profile.AgentKeys = inventory.ToDisplay(); profile.KeyMatches = matches;
                    string report = profile.ToReport();
                    Check(report.Contains(disabled ? "PubkeyAuthentication=no" : "нет publickey") && report.Contains("не проверялся") &&
                        !report.Contains("Start-Service") && !report.Contains("Set-Service") && !report.Contains("ssh-add в обычном") &&
                        !report.Contains("Сопоставление использует") && !report.Contains(AgentFingerprintA),
                        "excluded method report omits irrelevant key loading service commands and an unperformed matching claim");
                }
            }
            using (var cancelled = new CancellationTokenSource()) {
                cancelled.Cancel(); var profile = AuthPolicyProfile(root, "no", "password");
                bool inventory = false, matching = false;
                try { SshAgentInventory.ForProfile(profile, cancelled.Token, run); } catch (OperationCanceledException) { inventory = true; }
                try { SshProfileKeyMatching.Check(profile, cancelled.Token, run); } catch (OperationCanceledException) { matching = true; }
                Check(inventory && matching && calls == 0, "pre-cancellation takes precedence even over an authentication-policy skip");
            }
            var unresolved = AuthPolicyProfile(root, "no", "password"); unresolved.SshResolved = false;
            Check(SshAgentInventory.ForProfile(unresolved, CancellationToken.None, run).State == SshAgentInventoryState.NotChecked &&
                SshProfileKeyMatching.Check(unresolved, CancellationToken.None, run).State == SshProfileKeyMatchState.NotChecked && calls == 0,
                "unresolved profile remains not checked rather than a definitive disabled method");
            foreach (string mode in new[] { "yes", "future-mode", null }) {
                var profile = AuthPolicyProfile(root, mode, "publickey,password");
                var inventory = SshAgentInventory.ForProfile(profile, CancellationToken.None, run);
                var matches = SshProfileKeyMatching.Check(profile, CancellationToken.None, run);
                Check(inventory.State == SshAgentInventoryState.Available && matches.State == SshProfileKeyMatchState.Completed &&
                    matches.Identities[0].Presence == SshAgentFingerprintPresence.Present,
                    "allowed or unknown policy preserves bounded read-only observations and alternative-auth compatibility");
            }
            var custom = AuthPolicyProfile(root, "yes", "publickey"); custom.IdentityAgent = "none";
            Check(SshAgentInventory.ForProfile(custom, CancellationToken.None, run).State == SshAgentInventoryState.DisabledByProfile &&
                SshProfileKeyMatching.Check(custom, CancellationToken.None, run).Identities[0].Presence == SshAgentFingerprintPresence.Unknown,
                "allowed publickey does not override the independent IdentityAgent none restriction");
            var snapshot = KeyMatchProfile(root, "first", "second"); snapshot.PubkeyAuthentication = "yes"; snapshot.PreferredAuthentications = "publickey";
            int snapshotCalls = 0;
            var frozen = SshProfileKeyMatching.Check(snapshot, CancellationToken.None, (e, a, m, t) => {
                snapshotCalls++; snapshot.PubkeyAuthentication = "no"; snapshot.PreferredAuthentications = "password"; return KeyMatchCapture(AgentFingerprintA);
            });
            Check(snapshotCalls == 2 && frozen.Identities.All(x => x.State == SshIdentityFingerprintState.Observed),
                "policy scope is frozen before the first fingerprint child rather than changing mid-phase");
            var privatePolicy = AuthPolicyProfile(root, "PRIVATE_POLICY_MODE", "PRIVATE_POLICY_LIST");
            Check(!privatePolicy.ToReport().Contains("PRIVATE_POLICY"), "unknown raw auth policy values do not enter the user report");
            Check(!new SshAgentInventoryResult(SshAgentInventoryState.Available, 0, 1).ToDisplay().Contains("совпадение с ключом профиля не проверено"),
                "global inventory display does not contradict a separately completed selected-identity match");
        }
        // Test-only, readonly diagnostic allowlist. Never print native stderr, paths, SIDs or SDDL.
        private static string AuthPolicyFailureRoles(string error, string config, string include, string identity)
        {
            string normalized = (error ?? "").Replace('\\', '/');
            Func<string, bool> names = path => !String.IsNullOrEmpty(path) && normalized.IndexOf(path.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase) >= 0;
            return "configNamed=" + names(config) + "; includeNamed=" + names(include) + "; identityNamed=" + names(identity) +
                "; badOwnerPermissions=" + normalized.Contains("Bad owner or permissions") +
                "; badPermissionsBanner=" + normalized.Contains("BAD PERMISSIONS") + "; permissionDenied=" + normalized.Contains("Permission denied");
        }
        private static string AuthPolicyAclFacts(string path, bool directory = false)
        {
            try {
                var acl = directory ? (FileSystemSecurity)Directory.GetAccessControl(path, AccessControlSections.Owner | AccessControlSections.Access)
                    : File.GetAccessControl(path, AccessControlSections.Owner | AccessControlSections.Access);
                var owner = (SecurityIdentifier)acl.GetOwner(typeof(SecurityIdentifier));
                using (var current = WindowsIdentity.GetCurrent()) {
                    var system = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
                    var admins = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
                    int rules = 0, inherited = 0, denied = 0, otherAllow = 0, otherWrite = 0;
                    FileSystemRights writes = FileSystemRights.Write | FileSystemRights.DeleteSubdirectoriesAndFiles | FileSystemRights.Delete |
                        FileSystemRights.ChangePermissions | FileSystemRights.TakeOwnership;
                    foreach (FileSystemAccessRule rule in acl.GetAccessRules(true, true, typeof(SecurityIdentifier))) {
                        ++rules; if (rule.IsInherited) ++inherited;
                        if (rule.AccessControlType == AccessControlType.Deny) { ++denied; continue; }
                        bool trusted = rule.IdentityReference.Equals(current.User) || rule.IdentityReference.Equals(system) || rule.IdentityReference.Equals(admins);
                        if (!trusted) { ++otherAllow; if ((rule.FileSystemRights & writes) != 0) ++otherWrite; }
                    }
                    return "read=True; ownerCurrent=" + owner.Equals(current.User) + "; ownerSystem=" + owner.Equals(system) +
                        "; ownerAdmins=" + owner.Equals(admins) + "; protected=" + acl.AreAccessRulesProtected +
                        "; rules=" + rules + "; inherited=" + inherited + "; denied=" + denied + "; otherAllow=" + otherAllow + "; otherWrite=" + otherWrite;
                }
            } catch (Exception ex) {
                return "read=False; unauthorized=" + (ex is UnauthorizedAccessException) + "; io=" + (ex is IOException) + "; hresult=0x" + ex.HResult.ToString("X8");
            }
        }
        private static void AuthPolicyDiagnosticContract()
        {
            const string config = "fixture/config", include = "fixture/policy-include", identity = "fixture/locked identity";
            foreach (string role in new[] { "config", "include", "identity", "unknown" }) {
                string path = role == "config" ? config : role == "include" ? include : role == "identity" ? identity : "PRIVATE_DIAGNOSTIC_CANARY";
                string facts = AuthPolicyFailureRoles("Bad owner or permissions on " + path.Replace('/', '\\') + "; PRIVATE_DIAGNOSTIC_CANARY", config, include, identity);
                Check(facts.Contains("configNamed=" + (role == "config")) && facts.Contains("includeNamed=" + (role == "include")) &&
                    facts.Contains("identityNamed=" + (role == "identity")) && facts.Contains("badOwnerPermissions=True") &&
                    !facts.Contains("PRIVATE_") && !facts.Contains("fixture/"), "native policy diagnostic emits role flags only: " + role);
            }
        }
        private static void AuthPolicyNative(string root)
        {
            string identity = Path.Combine(root, "locked identity"), include = Path.Combine(root, "policy-include"), config = Path.Combine(root, "config");
            const string contents = "PRIVATE_POLICY_TEST_ONLY_NOT_A_KEY"; File.WriteAllText(identity, contents);
            File.WriteAllText(include, "Host policy.disabled\n PubkeyAuthentication no\n PreferredAuthentications publickey,password\n" +
                "Host policy.excluded\n PubkeyAuthentication yes\n PreferredAuthentications password,keyboard-interactive\n" +
                "Host policy.allowed\n PubkeyAuthentication yes\n PreferredAuthentications keyboard-interactive,publickey,password\n", new UTF8Encoding(false));
            File.WriteAllText(config, "Include " + ConfigQuoted(include) + "\nHost *\n HostName 127.0.0.41\n User fixture_policy\n CanonicalizeHostname no\n" +
                " PubkeyAuthentication yes\n PreferredAuthentications publickey\n IdentityFile " + ConfigQuoted(identity) + "\n", new UTF8Encoding(false));
            foreach (string path in new[] { config, include, identity }) {
                CheckAuthPolicyFixturePath(root, path); CheckAuthPolicyFixtureAcl(path, false);
            }
            using (var locked = new FileStream(identity, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) {
                foreach (string target in new[] { "policy.disabled", "policy.excluded", "policy.allowed" }) {
                    var selected = new SshProfileSetting { Target = target };
                    var oracle = DiagnosticProcess.Run(OpenSshClient.Executable,
                        "-G -F " + SshConnection.Quote(config) + " " + SshConnection.CommandArguments(selected), 7000, CancellationToken.None);
                    string facts = "; exit=" + oracle.ExitCode + "; hex=0x" + unchecked((uint)oracle.ExitCode).ToString("X8") +
                        "; truncated=" + oracle.Truncated + "; stdoutChars=" + (oracle.Output ?? "").Length + "; stderrChars=" + (oracle.Error ?? "").Length +
                        "; permissionsHint=" + (oracle.Error ?? "").Contains("Bad owner or permissions") +
                        "; configHint=" + (oracle.Error ?? "").Contains("Bad configuration");
                    if (oracle.ExitCode != 0 || oracle.Truncated) {
                        Console.WriteLine("Auth policy native failure roles: " + AuthPolicyFailureRoles(oracle.Error, config, include, identity));
                        Console.WriteLine("Auth policy fixture ACL: role=config; " + AuthPolicyAclFacts(config));
                        Console.WriteLine("Auth policy fixture ACL: role=include; " + AuthPolicyAclFacts(include));
                        Console.WriteLine("Auth policy fixture ACL: role=identity; " + AuthPolicyAclFacts(identity));
                        Console.WriteLine("Auth policy fixture ACL: role=directory; " + AuthPolicyAclFacts(root, true));
                    }
                    Check(oracle.ExitCode == 0 && !oracle.Truncated, "native fixture-only policy oracle resolves Include and first-value precedence: " + target + facts);
                    var result = SshProfileDiagnostics.CheckConfiguration(selected, CancellationToken.None, OpenSshClient.Executable, 7000, config);
                    var expected = target == "policy.disabled" ? SshPublicKeyPolicyState.Disabled : target == "policy.excluded" ? SshPublicKeyPolicyState.Excluded : SshPublicKeyPolicyState.AllowedByClient;
                    Console.WriteLine("Native auth policy observation: fixture=" + target + "; resolved=" + result.SshResolved + "; policy=" + result.PublicKeyPolicy +
                        "; identities=" + result.IdentityFiles.Count + "; confirmedPaths=" + result.IdentityPaths.Count);
                    Check(result.SshResolved && result.PublicKeyPolicy == expected && result.ResolvedHostName == "127.0.0.41" &&
                        result.PubkeyAuthentication == OracleOption(oracle.Output, "pubkeyauthentication") &&
                        result.PreferredAuthentications == OracleOption(oracle.Output, "preferredauthentications"),
                        "production policy retains actual native effective fields instead of parsing config files itself: " + target);
                    if (expected != SshPublicKeyPolicyState.AllowedByClient) {
                        Check(result.IdentityFiles.Count == 1 && result.IdentityStatuses.Count == 1 && result.IdentityPaths.Count == 0 &&
                            result.IdentityStatuses.Values.All(x => x == SshPublicKeyPolicy.SkippedFile) &&
                            result.KeyStatus == SshPublicKeyPolicy.SkippedFile, "disabled key method does not resolve or inspect even a locked identity file: " + target);
                        var probe = new SshAgentServiceProbe(t => new SshAgentServiceSnapshot { RuntimeState = SshAgentRuntimeState.Running });
                        var full = SshProfileDiagnostics.Check(selected, CancellationToken.None, OpenSshClient.Executable, 7000, probe, configurationFile: config);
                        Check(full.SshResolved && full.PublicKeyPolicy == expected && full.IdentityFiles.Count == 1 && full.AgentProbe.State == SshAgentProbeState.Completed && full.AgentInventory.State == SshAgentInventoryState.NotUsedByAuthentication &&
                            full.KeyMatches.State == SshProfileKeyMatchState.NotUsedByAuthentication && full.IdentityPaths.Count == 0,
                            "full native diagnostic preserves separate service facts while skipping all key-agent phases: " + target);
                    } else Check(result.IdentityPaths.Count == 1 && result.KeyStatus.Contains("Доступность файла не подтверждена"),
                        "allowed key method preserves ordinary locked-file availability diagnostics without a readiness claim");
                }
            }
            Check(File.ReadAllText(identity) == contents, "policy diagnostics preserve the isolated locked fixture without repairing ACL or content");
        }
        private static void AuthPolicyForm()
        {
            var profile = new SshProfileDiagnosticResult { SshResolved = true, PubkeyAuthentication = "no", PreferredAuthentications = "password" };
            profile.KeyMatches = SshProfileKeyMatching.Check(profile, CancellationToken.None);
            using (var form = new SshDiagnosticForm(new SshProfileSetting { Target = "policy.disabled" }, (p, t) => profile)) {
                form.Show(); PumpUntil(() => form.Work != null && form.Work.IsCompleted);
                string report = ((TextBox)Field(form, "report")).Text;
                Check(report.Contains("PubkeyAuthentication=no") && report.Contains("Другие методы требуют отдельной проверки") &&
                    !report.Contains("ssh-add в обычном") && !report.Contains("Start-Service"), "actual diagnostic form presents scoped policy rather than unnecessary key-unlock instructions");
                form.Close();
            }
        }
    }
}

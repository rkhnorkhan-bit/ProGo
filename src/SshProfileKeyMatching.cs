using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.IO;

namespace ProGo
{
    internal enum SshIdentityFingerprintState
    { Observed, NotUsed, PathNotResolved, MissingExecutable, CommandFailed, InvalidOutput, Ambiguous, TimedOut, Failed }
    internal enum SshProfileKeyMatchState { Completed, NotChecked, Busy }

    internal sealed class SshIdentityFingerprint
    {
        internal string Identity { get; private set; }
        internal SshIdentityFingerprintState State { get; private set; }
        internal string Fingerprint { get; private set; }
        internal SshAgentFingerprintPresence Presence { get; private set; }
        internal int? ExitCode { get; private set; }
        internal int? PrimaryExitCode { get; private set; }
        internal bool SidecarFallback { get; private set; }
        internal SshIdentityFingerprint(string identity, SshIdentityFingerprintState state, string fingerprint = null,
            SshAgentFingerprintPresence presence = SshAgentFingerprintPresence.Unknown, int? exitCode = null,
            bool sidecarFallback = false, int? primaryExitCode = null)
        {
            Identity = identity; State = state; Fingerprint = fingerprint; Presence = presence;
            ExitCode = exitCode; SidecarFallback = sidecarFallback; PrimaryExitCode = primaryExitCode;
        }
        internal SshIdentityFingerprint ForIdentity(string identity)
        { return new SshIdentityFingerprint(identity, State, Fingerprint, Presence, ExitCode, SidecarFallback, PrimaryExitCode); }
        internal string ToDisplay()
        {
            switch (State) {
                case SshIdentityFingerprintState.Observed:
                    string text = Presence == SshAgentFingerprintPresence.Present
                        ? "Отпечаток публичной identity есть в полученном списке агента."
                        : Presence == SshAgentFingerprintPresence.Absent
                            ? "Отпечатка публичной identity нет в полученном списке агента; другие методы входа не исключены."
                            : "Отпечаток публичной identity получен; сопоставление с агентом не подтверждено.";
                    return text + (SidecarFallback ? " Использован соседний .pub; исходный файл и его права не подтверждены." : "");
                case SshIdentityFingerprintState.NotUsed: return "IdentityFile none: отпечаток файла не проверяется.";
                case SshIdentityFingerprintState.PathNotResolved: return "Итоговый путь не подтверждён OpenSSH-контекстом; отпечаток не проверен.";
                case SshIdentityFingerprintState.MissingExecutable: return "ssh-keygen.exe из выбранной установки OpenSSH не найден; отпечаток не проверен.";
                case SshIdentityFingerprintState.CommandFailed: return "OpenSSH не прочитал публичную identity. Проверьте файл и права; причина отказа требует отдельной проверки.";
                case SshIdentityFingerprintState.Ambiguous: return "OpenSSH вернул несколько identities; один отпечаток не выбран.";
                case SshIdentityFingerprintState.InvalidOutput: return "Ответ OpenSSH неполон или имеет неподдерживаемый формат; отпечаток не подтверждён.";
                case SshIdentityFingerprintState.TimedOut: return "Общий лимит фазы отпечатков истёк; эта identity не проверена до конца.";
                default: return "Не удалось проверить публичную identity; отпечаток не подтверждён.";
            }
        }
    }
    internal sealed class SshProfileKeyMatchResult
    {
        internal SshProfileKeyMatchState State { get; private set; }
        internal IList<SshIdentityFingerprint> Identities { get; private set; }
        internal bool IdentityListLimited { get; private set; }
        internal SshProfileKeyMatchResult(SshProfileKeyMatchState state, List<SshIdentityFingerprint> identities = null, bool limited = false)
        { State = state; Identities = Array.AsReadOnly(identities == null ? new SshIdentityFingerprint[0] : identities.ToArray()); IdentityListLimited = limited; }
        internal string Describe(string identity)
        {
            foreach (var item in Identities) if (String.Equals(item.Identity, identity, StringComparison.Ordinal)) return item.ToDisplay();
            return State == SshProfileKeyMatchState.Busy ? "Другая read-only проверка SSH ещё выполняется; отпечаток не проверен."
                : "Отпечаток этой identity не проверен.";
        }
    }

    // Observations of OpenSSH's public reference (.pub may take precedence), not
    // private-key binding/possession/signing, not Ready and never an auth gate.
    internal static class SshProfileKeyMatching
    {
        internal const int TimeoutMilliseconds = 3000;
        internal const int IdentityLimit = 32;
        internal static SshProfileKeyMatchResult Check(SshProfileDiagnosticResult profile, CancellationToken token,
            Func<string, string, int, CancellationToken, DiagnosticProcessResult> run = null,
            int timeoutMs = TimeoutMilliseconds, Func<long> elapsedMilliseconds = null)
        {
            if (timeoutMs < 1 || timeoutMs > 30000) throw new ArgumentOutOfRangeException("timeoutMs");
            token.ThrowIfCancellationRequested();
            if (profile == null || !profile.SshResolved) return new SshProfileKeyMatchResult(SshProfileKeyMatchState.NotChecked);
            var watch = Stopwatch.StartNew();
            Func<long> elapsed = elapsedMilliseconds ?? (() => watch.ElapsedMilliseconds);
            // Freeze the entire input before any child can run or publish a result.
            var raw = new List<string>(profile.IdentityFiles);
            var paths = new Dictionary<string, string>(profile.IdentityPaths, StringComparer.Ordinal);
            string ssh = profile.ExecutablePath;
            var inventory = String.IsNullOrEmpty(profile.IdentityAgent) ? profile.AgentInventory : null;
            bool limited = profile.IdentityListLimited || raw.Count > IdentityLimit;
            if (raw.Count > IdentityLimit) raw.RemoveRange(IdentityLimit, raw.Count - IdentityLimit);
            using (var lease = SshReadOnlyLease.TryAcquire(token)) {
                if (lease == null) return new SshProfileKeyMatchResult(SshProfileKeyMatchState.Busy, limited: limited);
                var entries = new List<SshIdentityFingerprint>();
                var cache = new Dictionary<string, SshIdentityFingerprint>(StringComparer.Ordinal);
                string executable = null; bool resolvedExecutable = false;
                foreach (string identity in raw) {
                    token.ThrowIfCancellationRequested(); string path; SshIdentityFingerprint item;
                    if (identity == "none") item = new SshIdentityFingerprint(identity, SshIdentityFingerprintState.NotUsed);
                    else if (identity == null || !paths.TryGetValue(identity, out path) || !ConfirmedLocalPath(path))
                        item = new SshIdentityFingerprint(identity, SshIdentityFingerprintState.PathNotResolved);
                    else if (cache.TryGetValue(path, out item)) item = item.ForIdentity(identity);
                    else {
                        if (!resolvedExecutable) { executable = OpenSshClient.KeygenExecutable(ssh); resolvedExecutable = true; }
                        item = Read(identity, path, executable, inventory, token, run ?? DiagnosticProcess.Run, timeoutMs, elapsed);
                        cache[path] = item;
                    }
                    entries.Add(item);
                }
                token.ThrowIfCancellationRequested();
                return new SshProfileKeyMatchResult(SshProfileKeyMatchState.Completed, entries, limited);
            }
        }
        private static bool ConfirmedLocalPath(string path)
        {
            if (String.IsNullOrEmpty(path) || path.Length > 32768 || path.IndexOfAny(new[] { '\r', '\n', '\0' }) >= 0) return false;
            try {
                if (!Path.IsPathRooted(path)) return false;
                if (Path.DirectorySeparatorChar != '\\') return true;
                return path.Length >= 3 && Char.IsLetter(path[0]) && path[1] == ':' && (path[2] == '\\' || path[2] == '/');
            } catch { return false; }
        }
        private static SshIdentityFingerprint Read(string identity, string path, string executable, SshAgentInventoryResult inventory,
            CancellationToken token, Func<string, string, int, CancellationToken, DiagnosticProcessResult> run, int budget, Func<long> elapsed)
        {
            if (String.IsNullOrEmpty(executable)) return new SshIdentityFingerprint(identity, SshIdentityFingerprintState.MissingExecutable);
            int? primaryCode = null;
            for (int attempt = 0; attempt < 2; attempt++) {
                token.ThrowIfCancellationRequested(); long spent = elapsed();
                if (spent < 0 || spent >= budget) return new SshIdentityFingerprint(identity, SshIdentityFingerprintState.TimedOut);
                try {
                    var captured = run(executable, "-l -E sha256 -f " + SshConnection.Quote(attempt == 0 ? path : path + ".pub"),
                        (int)(budget - spent), token);
                    token.ThrowIfCancellationRequested();
                    if (elapsed() >= budget) return new SshIdentityFingerprint(identity, SshIdentityFingerprintState.TimedOut);
                    if (captured == null) return new SshIdentityFingerprint(identity, SshIdentityFingerprintState.Failed);
                    if (captured.Truncated || (captured.Output ?? "").Length > DiagnosticProcess.CaptureLimit ||
                        (captured.Error ?? "").Length > DiagnosticProcess.CaptureLimit)
                        return new SshIdentityFingerprint(identity, SshIdentityFingerprintState.InvalidOutput, exitCode: captured.ExitCode);
                    if (captured.ExitCode != 0) {
                        if (attempt == 0) { primaryCode = captured.ExitCode; continue; }
                        return new SshIdentityFingerprint(identity, SshIdentityFingerprintState.CommandFailed, exitCode: captured.ExitCode, sidecarFallback: true, primaryExitCode: primaryCode);
                    }
                    var parsed = SshAgentInventory.Parse(captured);
                    if (parsed.State != SshAgentInventoryState.Available)
                        return new SshIdentityFingerprint(identity, SshIdentityFingerprintState.InvalidOutput, exitCode: 0);
                    if (parsed.ListedIdentityCount != 1 || parsed.Fingerprints.Count != 1)
                        return new SshIdentityFingerprint(identity, SshIdentityFingerprintState.Ambiguous, exitCode: 0);
                    string fingerprint = parsed.Fingerprints[0];
                    // A second explicit .pub lookup is useful metadata, but its relation
                    // to a failed primary input is not proven. Never match it as that input.
                    var presence = inventory == null || attempt != 0 ? SshAgentFingerprintPresence.Unknown : inventory.ContainsFingerprint(fingerprint);
                    token.ThrowIfCancellationRequested();
                    if (elapsed() >= budget) return new SshIdentityFingerprint(identity, SshIdentityFingerprintState.TimedOut);
                    return new SshIdentityFingerprint(identity, SshIdentityFingerprintState.Observed, fingerprint, presence, 0, attempt != 0, primaryCode);
                }
                catch (OperationCanceledException) { throw; }
                catch (TimeoutException) { token.ThrowIfCancellationRequested(); return new SshIdentityFingerprint(identity, SshIdentityFingerprintState.TimedOut); }
                catch { token.ThrowIfCancellationRequested(); return new SshIdentityFingerprint(identity, SshIdentityFingerprintState.Failed); }
            }
            throw new InvalidOperationException("Unreachable fingerprint attempt state.");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;

namespace ProGo
{
    internal enum SshAgentInventoryState
    {
        NotChecked, DisabledByProfile, EndpointNotChecked, Deferred, MissingExecutable,
        Available, NoIdentities, Unavailable, CommandFailed, InvalidOutput, TimedOut, Failed, Busy, NotUsedByAuthentication
    }
    internal enum SshAgentFingerprintPresence { Unknown, Present, Absent }

    // Ephemeral inventory, not authentication/readiness. No comments, raw output or paths.
    internal sealed class SshAgentInventoryResult
    {
        internal SshAgentInventoryState State { get; private set; }
        internal int? ExitCode { get; private set; }
        internal int ListedIdentityCount { get; private set; }
        internal IList<string> Fingerprints { get; private set; }

        internal SshAgentInventoryResult(SshAgentInventoryState state, int? exitCode = null,
            int count = 0, List<string> fingerprints = null)
        {
            State = state; ExitCode = exitCode; ListedIdentityCount = count;
            Fingerprints = Array.AsReadOnly(fingerprints == null ? new string[0] : fingerprints.ToArray());
        }
        internal SshAgentFingerprintPresence ContainsFingerprint(string fingerprint)
        {
            if (!SshAgentInventory.IsFingerprint(fingerprint) ||
                (State != SshAgentInventoryState.Available && State != SshAgentInventoryState.NoIdentities))
                return SshAgentFingerprintPresence.Unknown;
            return Fingerprints.Contains(fingerprint) ? SshAgentFingerprintPresence.Present : SshAgentFingerprintPresence.Absent;
        }
        internal string ToDisplay()
        {
            switch (State) {
                case SshAgentInventoryState.Available:
                    return "Агент отвечает; ключей: " + ListedIdentityCount.ToString(CultureInfo.InvariantCulture) +
                        ". Это ещё не подтверждает доступ к выбранному VPS; выбранные identities проверяются отдельно.";
                case SshAgentInventoryState.NotUsedByAuthentication:
                    return "Политика профиля не использует вход по публичному ключу; ключи агента не проверялись.";
                case SshAgentInventoryState.NoIdentities:
                    return "Агент ответил: ключей нет. Загрузите нужный ключ через ssh-add.";
                case SshAgentInventoryState.DisabledByProfile:
                    return "Использование агента отключено этим SSH-профилем. Другие методы аутентификации не запрещены.";
                case SshAgentInventoryState.EndpointNotChecked:
                    return "В профиле явно указан агент. Его ключи не проверены; стандартный агент не подставлялся.";
                case SshAgentInventoryState.Deferred:
                    return "Проверка ключей отложена: состояние службы ещё не подтверждено. Другие методы SSH-аутентификации могут работать без агента.";
                case SshAgentInventoryState.MissingExecutable:
                    return "ssh-add.exe из этой установки OpenSSH не найден. Наличие ключей не проверено.";
                case SshAgentInventoryState.Unavailable:
                    return "ssh-add не получил ответ агента. Проверьте службу и SSH_AUTH_SOCK; фоновый SSH не запрашивает passphrase.";
                case SshAgentInventoryState.CommandFailed:
                    return "Команда проверки агента завершилась ошибкой. Пустой список ключей не подтверждён; проверьте OpenSSH и доступ к агенту.";
                case SshAgentInventoryState.InvalidOutput:
                    return "Ответ агента неполон или имеет неподдерживаемый формат. Наличие ключей не подтверждено.";
                case SshAgentInventoryState.TimedOut:
                    return "Агент не ответил за 3 секунды. Проверка остановлена; можно повторить.";
                case SshAgentInventoryState.Busy:
                    return "Другая операция с SSH-agent ещё выполняется. Повторите после её завершения.";
                case SshAgentInventoryState.NotChecked:
                    return "Конфигурация выбранного SSH-профиля не подтверждена; ключи агента не проверялись.";
                default:
                    return "Не удалось проверить ключи агента. Проверьте ssh-add.exe и права доступа.";
            }
        }
    }

    internal static class SshAgentInventory
    {
        internal const int TimeoutMilliseconds = 3000;
        internal const int IdentityLimit = 256;
        private static readonly Regex identity = new Regex(
            @"\A([0-9]{1,7})[ \t]+(SHA256:[A-Za-z0-9+/]{43})[ \t]+[^\r\n\0]*\([A-Za-z0-9@._+-]{1,64}\)\z",
            RegexOptions.CultureInvariant);

        internal static bool IsFingerprint(string value)
        {
            if (value == null || value.Length != 50 || !value.StartsWith("SHA256:", StringComparison.Ordinal)) return false;
            try {
                byte[] digest = Convert.FromBase64String(value.Substring(7) + "=");
                return digest.Length == 32 && "SHA256:" + Convert.ToBase64String(digest).TrimEnd('=') == value;
            } catch (FormatException) { return false; }
        }

        // Keep the whole response or reject it. An incomplete prefix cannot prove absence.
        internal static SshAgentInventoryResult Parse(DiagnosticProcessResult captured)
        {
            if (captured == null) return new SshAgentInventoryResult(SshAgentInventoryState.Failed);
            if (captured.Truncated || (captured.Output ?? "").Length > DiagnosticProcess.CaptureLimit ||
                (captured.Error ?? "").Length > DiagnosticProcess.CaptureLimit)
                return new SshAgentInventoryResult(SshAgentInventoryState.InvalidOutput, captured.ExitCode);
            if (captured.ExitCode == 2) return new SshAgentInventoryResult(SshAgentInventoryState.Unavailable, 2);
            // OpenSSH list_identities emits this precise stdout message for NO_IDENTITIES.
            // Exit 1 alone also covers a locked/malformed/failing agent, not just an empty one.
            if (captured.ExitCode == 1 && (captured.Output ?? "").Trim() == "The agent has no identities." &&
                String.IsNullOrWhiteSpace(captured.Error))
                return new SshAgentInventoryResult(SshAgentInventoryState.NoIdentities, 1);
            if (captured.ExitCode != 0) return new SshAgentInventoryResult(SshAgentInventoryState.CommandFailed, captured.ExitCode);
            if (!String.IsNullOrWhiteSpace(captured.Error)) return new SshAgentInventoryResult(SshAgentInventoryState.InvalidOutput, 0);
            var lines = (captured.Output ?? "").Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0 || lines.Length > IdentityLimit) return new SshAgentInventoryResult(SshAgentInventoryState.InvalidOutput, 0);
            var fingerprints = new List<string>();
            foreach (string line in lines) {
                var match = identity.Match(line); uint bits;
                if (!match.Success || !UInt32.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out bits) ||
                    bits == 0 || !IsFingerprint(match.Groups[2].Value))
                    return new SshAgentInventoryResult(SshAgentInventoryState.InvalidOutput, 0);
                string fingerprint = match.Groups[2].Value;
                if (!fingerprints.Contains(fingerprint)) fingerprints.Add(fingerprint);
            }
            return new SshAgentInventoryResult(SshAgentInventoryState.Available, 0, lines.Length, fingerprints);
        }

        internal static SshAgentInventoryResult ForProfile(SshProfileDiagnosticResult profile, CancellationToken token,
            Func<string, string, int, CancellationToken, DiagnosticProcessResult> run = null)
        {
            token.ThrowIfCancellationRequested();
            if (profile == null || !profile.SshResolved) return new SshAgentInventoryResult(SshAgentInventoryState.NotChecked);
            if (SshPublicKeyPolicy.NotUsed(profile.PublicKeyPolicy)) return new SshAgentInventoryResult(SshAgentInventoryState.NotUsedByAuthentication);
            if (profile.IdentityAgent == "none") return new SshAgentInventoryResult(SshAgentInventoryState.DisabledByProfile);
            // Explicit sockets/environment references need verified endpoint propagation first.
            // Never inspect the default agent and label it as this profile's custom agent.
            if (!String.IsNullOrEmpty(profile.IdentityAgent)) return new SshAgentInventoryResult(SshAgentInventoryState.EndpointNotChecked);
            if (profile.AgentProbe == null || profile.AgentProbe.State != SshAgentProbeState.Completed)
                return new SshAgentInventoryResult(SshAgentInventoryState.Deferred);
            return Read(OpenSshClient.AgentExecutable(profile.ExecutablePath), token, run);
        }

        // Synchronous owned process work: call only from the diagnostic/background worker.
        // The slot is held through Run's timeout/cancellation/JobObject cleanup, not just capture.
        internal static SshAgentInventoryResult Read(string executable, CancellationToken token,
            Func<string, string, int, CancellationToken, DiagnosticProcessResult> run = null)
        {
            token.ThrowIfCancellationRequested();
            if (String.IsNullOrWhiteSpace(executable)) return new SshAgentInventoryResult(SshAgentInventoryState.MissingExecutable);
            using (var lease = SshReadOnlyLease.TryAcquire(token)) {
                if (lease == null) return new SshAgentInventoryResult(SshAgentInventoryState.Busy);
                try {
                    token.ThrowIfCancellationRequested();
                    var captured = (run ?? DiagnosticProcess.Run)(executable, "-l -E sha256", TimeoutMilliseconds, token);
                    token.ThrowIfCancellationRequested();
                    var result = Parse(captured);
                    token.ThrowIfCancellationRequested();
                    return result;
                }
                catch (OperationCanceledException) { throw; }
                catch (TimeoutException) { token.ThrowIfCancellationRequested(); return new SshAgentInventoryResult(SshAgentInventoryState.TimedOut); }
                catch { token.ThrowIfCancellationRequested(); return new SshAgentInventoryResult(SshAgentInventoryState.Failed); }
            }
        }
    }
}

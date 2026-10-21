using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace ProGo
{
    // A request describes one attempt. It neither changes the selected profile
    // nor grants permission to launch: the caller still owns revision/intent.
    internal sealed class SshAuthenticationPreparationRequest
    {
        private readonly SshProfileSetting candidate;
        internal string Executable { get; private set; }
        internal string ConfigurationFile { get; private set; }
        internal string AgentEndpoint { get; private set; }
        internal SshProfileSetting Candidate { get { return candidate == null ? null : candidate.Clone(); } }
        internal static bool CanonicalExecutable(string value)
        {
            try {
                return !String.IsNullOrEmpty(value) && value.Length <= 32768 && value.IndexOfAny(new[] { '\0', '\r', '\n' }) < 0 &&
                    Path.IsPathRooted(value) && String.Equals(Path.GetFullPath(value), value, StringComparison.OrdinalIgnoreCase);
            } catch (ArgumentException) { return false; } catch (NotSupportedException) { return false; } catch (IOException) { return false; }
        }
        internal SshAuthenticationPreparationRequest(SshProfileSetting candidate, string executable, string configurationFile = null)
        {
            this.candidate = candidate == null ? null : candidate.Clone(); Executable = executable;
            ConfigurationFile = configurationFile == null ? null : Path.GetFullPath(configurationFile);
            AgentEndpoint = Environment.GetEnvironmentVariable("SSH_AUTH_SOCK");
        }
        internal bool Matches(SshProfileSetting current, string executable, string configurationFile, string agentEndpoint)
        {
            try {
                return candidate != null && current != null && CanonicalExecutable(Executable) && CanonicalExecutable(executable) &&
                    candidate.Target == current.Target && candidate.Server == current.Server &&
                    candidate.User == current.User && candidate.Port == current.Port && candidate.IdentityFile == current.IdentityFile &&
                    String.Equals(Executable, executable, StringComparison.OrdinalIgnoreCase) &&
                    String.Equals(ConfigurationFile, configurationFile == null ? null : Path.GetFullPath(configurationFile), StringComparison.OrdinalIgnoreCase) &&
                    AgentEndpoint == agentEndpoint;
            } catch (ArgumentException) { return false; } catch (NotSupportedException) { return false; } catch (IOException) { return false; }
        }
    }

    internal sealed class SshAuthenticationIdentity
    {
        internal string Identity { get; private set; }
        internal string Path { get; private set; }
        internal SshAuthenticationIdentity(string identity, string path) { Identity = identity; Path = path; }
    }

    internal sealed class SshAuthenticationConfiguration
    {
        private readonly SshProfileDiagnosticResult frozen;
        internal string Target { get { return frozen.Target; } }
        internal string Executable { get { return frozen.ExecutablePath; } }
        internal string HostName { get { return frozen.ResolvedHostName; } }
        internal string User { get { return frozen.ResolvedUser; } }
        internal string Port { get { return frozen.ResolvedPort; } }
        internal string IdentityAgent { get { return frozen.IdentityAgent; } }
        internal string IdentitiesOnly { get { return frozen.IdentitiesOnly; } }
        internal bool IdentityListLimited { get { return frozen.IdentityListLimited; } }
        internal SshPublicKeyPolicyState PublicKeyPolicy { get { return frozen.PublicKeyPolicy; } }
        internal ReadOnlyCollection<SshAuthenticationIdentity> Identities { get; private set; }
        internal SshAuthenticationConfiguration(SshProfileDiagnosticResult value)
        {
            frozen = Copy(value);
            var identities = new List<SshAuthenticationIdentity>();
            foreach (string identity in frozen.IdentityFiles) {
                string path; frozen.IdentityPaths.TryGetValue(identity, out path);
                identities.Add(new SshAuthenticationIdentity(identity, path));
            }
            Identities = identities.AsReadOnly();
        }
        internal SshProfileDiagnosticResult ForSigning() { return Copy(frozen); }
        internal bool Same(SshAuthenticationConfiguration other) { return other != null && Scope(frozen) == Scope(other.frozen); }
        private static SshProfileDiagnosticResult Copy(SshProfileDiagnosticResult value)
        {
            var copy = new SshProfileDiagnosticResult { Target = value.Target, ConfigPath = value.ConfigPath, OriginalHost = value.OriginalHost,
                ExecutablePath = value.ExecutablePath, ResolvedHostName = value.ResolvedHostName, ResolvedUser = value.ResolvedUser,
                ResolvedPort = value.ResolvedPort, IdentityAgent = value.IdentityAgent, IdentitiesOnly = value.IdentitiesOnly,
                PubkeyAuthentication = value.PubkeyAuthentication, PreferredAuthentications = value.PreferredAuthentications,
                SshResolved = value.SshResolved, SshAvailable = value.SshAvailable, IdentityListLimited = value.IdentityListLimited };
            foreach (string identity in value.IdentityFiles) {
                if (copy.IdentityFiles.Count == 32) { copy.IdentityListLimited = true; break; }
                copy.IdentityFiles.Add(identity); string path;
                if (value.IdentityPaths.TryGetValue(identity, out path)) copy.IdentityPaths[identity] = path;
            }
            return copy;
        }
        private static string Scope(SshProfileDiagnosticResult value)
        {
            var scope = new StringBuilder();
            foreach (string item in new[] { value.Target, value.ConfigPath, value.OriginalHost, value.ExecutablePath,
                value.ResolvedHostName, value.ResolvedUser, value.ResolvedPort, value.IdentityAgent, value.IdentitiesOnly,
                value.PubkeyAuthentication, value.PreferredAuthentications }) Append(scope, item);
            scope.Append(value.SshAvailable).Append(';').Append(value.SshResolved).Append(';').Append(value.IdentityListLimited).Append(';');
            foreach (string identity in value.IdentityFiles) {
                string path; value.IdentityPaths.TryGetValue(identity, out path); Append(scope, identity); Append(scope, path);
            }
            return scope.ToString();
        }
        private static void Append(StringBuilder scope, string item) { scope.Append(item == null ? -1 : item.Length).Append(':').Append(item).Append(';'); }
    }

    internal enum SshAuthenticationPreparationState { Unconfirmed, NotUsed, LocalSigningObserved, Busy, Changed, TimedOut, Cancelled, Failed }
    internal enum SshAuthenticationPreparationReason
    {
        InvalidRequest, ConfigurationUnconfirmed, PublicKeyNotUsed, AuthenticationPolicyUnconfirmed, AgentDisabledByProfile,
        CustomAgent, InvalidAgentEndpoint, NoIdentity, MultipleIdentities, IdentityListLimited, SigningUnconfirmed,
        LocalSigningObserved, Busy, Changed, SigningTimedOut, TimedOut, Cancelled, Failed
    }
    internal sealed class SshAuthenticationPreparationResult
    {
        internal SshAuthenticationPreparationState State { get; private set; }
        internal SshAuthenticationPreparationReason Reason { get; private set; }
        internal SshAuthenticationPreparationRequest Request { get; private set; }
        internal SshAuthenticationConfiguration Configuration { get; private set; }
        internal SshKeyReadinessResult Signing { get; private set; }
        internal SshAuthenticationPreparationResult(SshAuthenticationPreparationState state, SshAuthenticationPreparationReason reason,
            SshAuthenticationPreparationRequest request, SshAuthenticationConfiguration configuration, SshKeyReadinessResult signing)
        { State = state; Reason = reason; Request = request; Configuration = configuration; Signing = signing; }
        internal string ToDisplay()
        {
            switch (Reason) {
                case SshAuthenticationPreparationReason.LocalSigningObserved:
                    return "Получена локальная подпись единственной effective identity. Закрытый файл и вход на сервер не подтверждены; требуется проверка SSH/SOCKS.";
                case SshAuthenticationPreparationReason.PublicKeyNotUsed:
                    return "Политика профиля не использует publickey. Другие методы проверяет OpenSSH; агент не является обязательным условием.";
                case SshAuthenticationPreparationReason.AgentDisabledByProfile:
                    return "IdentityAgent none: агент не проверялся. Прямые ключи и другие способы входа остаются возможны.";
                case SshAuthenticationPreparationReason.CustomAgent:
                case SshAuthenticationPreparationReason.InvalidAgentEndpoint:
                    return "Выбранный endpoint агента не поддержан этой локальной проверкой. Стандартный агент не подставлялся; штатный OpenSSH не запрещён.";
                case SshAuthenticationPreparationReason.MultipleIdentities:
                case SshAuthenticationPreparationReason.IdentityListLimited:
                case SshAuthenticationPreparationReason.NoIdentity:
                    return "Единственная effective identity не установлена. Первый ключ автоматически не выбирался; штатный OpenSSH сохраняет свои методы и порядок входа.";
                case SshAuthenticationPreparationReason.Changed:
                    return "Effective профиль или endpoint агента изменился. Прежнее наблюдение не принято; повторите подготовку для текущего запроса.";
                case SshAuthenticationPreparationReason.TimedOut:
                    return "Общий срок локальной подготовки истёк. Готовность не подтверждена; завершение операции не разрешает поздний запуск SSH.";
                case SshAuthenticationPreparationReason.SigningTimedOut:
                    return "Срок локальной проверки подписи истёк. Готовность не подтверждена; штатные способы входа требуют отдельной проверки.";
                case SshAuthenticationPreparationReason.Cancelled:
                    return "Локальная подготовка отменена. Результат не подтверждает готовность и не разрешает поздний запуск SSH.";
                case SshAuthenticationPreparationReason.SigningUnconfirmed:
                case SshAuthenticationPreparationReason.Busy:
                    return Signing == null ? "Локальная подпись не подтверждена." : Signing.ToDisplay();
                default:
                    return "Подготовка выбранного способа входа не подтверждена. Прямой ключ, отдельный агент и другие методы проверяет штатный OpenSSH.";
            }
        }
    }

    internal static class SshAuthenticationPreparation
    {
        internal const int TimeoutMilliseconds = 7000;
        // Synchronous worker boundary. No SSH connection, service start, key
        // addition, authentication gate, or UI work is performed here.
        internal static SshAuthenticationPreparationResult Prepare(SshAuthenticationPreparationRequest request, CancellationToken token,
            int timeoutMs = TimeoutMilliseconds,
            Func<SshProfileSetting, CancellationToken, string, int, string, SshProfileDiagnosticResult> configuration = null,
            Func<string, string, string, int, CancellationToken, DiagnosticProcessResult> signing = null,
            Func<long> elapsedMilliseconds = null)
        {
            if (timeoutMs < 1 || timeoutMs > 30000) throw new ArgumentOutOfRangeException("timeoutMs");
            var watch = Stopwatch.StartNew(); Func<long> elapsed = elapsedMilliseconds ?? (() => watch.ElapsedMilliseconds);
            SshAuthenticationConfiguration snapshot = null; SshKeyReadinessResult observation = null;
            Func<SshAuthenticationPreparationState, SshAuthenticationPreparationReason, SshAuthenticationPreparationResult> result =
                (state, reason) => new SshAuthenticationPreparationResult(state, reason, request, snapshot, observation);
            Action deadline = delegate { token.ThrowIfCancellationRequested(); long spent = elapsed(); if (spent < 0 || spent >= timeoutMs) throw new TimeoutException(); };
            Action freshness = delegate {
                deadline();
                if (request != null && request.AgentEndpoint != Environment.GetEnvironmentVariable("SSH_AUTH_SOCK")) throw new PreparationChangedException();
            };
            Func<int> remaining = delegate {
                token.ThrowIfCancellationRequested(); long spent = elapsed();
                if (spent < 0 || spent >= timeoutMs) throw new TimeoutException();
                return (int)(timeoutMs - spent);
            };
            try {
                freshness();
                if (request == null || request.Candidate == null || !SshAuthenticationPreparationRequest.CanonicalExecutable(request.Executable))
                    return result(SshAuthenticationPreparationState.Unconfirmed, SshAuthenticationPreparationReason.InvalidRequest);
                string endpoint = request.AgentEndpoint;
                string executable = request.Executable;
                Func<SshProfileDiagnosticResult> read = delegate {
                    freshness();
                    var copy = request.Candidate; string target = copy.Address;
                    var value = configuration == null ? SshProfileDiagnostics.CheckConfiguration(copy, token, executable, remaining(), request.ConfigurationFile) :
                        configuration(copy, token, executable, remaining(), request.ConfigurationFile);
                    freshness();
                    if (value == null || !value.SshResolved || !value.SshAvailable || !String.IsNullOrEmpty(value.Error)) return null;
                    if (value.Target != target || !String.Equals(value.ExecutablePath, executable, StringComparison.OrdinalIgnoreCase) ||
                        !String.Equals(value.ConfigPath, request.ConfigurationFile, StringComparison.OrdinalIgnoreCase)) throw new PreparationChangedException();
                    return value;
                };
                var initial = read();
                if (initial == null) return result(SshAuthenticationPreparationState.Unconfirmed, SshAuthenticationPreparationReason.ConfigurationUnconfirmed);
                snapshot = new SshAuthenticationConfiguration(initial); freshness();
                SshAuthenticationPreparationReason? skip = null;
                if (SshPublicKeyPolicy.NotUsed(snapshot.PublicKeyPolicy)) skip = SshAuthenticationPreparationReason.PublicKeyNotUsed;
                else if (snapshot.PublicKeyPolicy != SshPublicKeyPolicyState.AllowedByClient) skip = SshAuthenticationPreparationReason.AuthenticationPolicyUnconfirmed;
                else if (snapshot.IdentityAgent == "none") skip = SshAuthenticationPreparationReason.AgentDisabledByProfile;
                else if (!String.IsNullOrEmpty(snapshot.IdentityAgent)) skip = SshAuthenticationPreparationReason.CustomAgent;
                else if (endpoint != null && (endpoint.Length == 0 || endpoint.Length > 32768 || endpoint.IndexOfAny(new[] { '\0', '\r', '\n' }) >= 0)) skip = SshAuthenticationPreparationReason.InvalidAgentEndpoint;
                else if (snapshot.IdentityListLimited) skip = SshAuthenticationPreparationReason.IdentityListLimited;
                else if (snapshot.Identities.Count == 0 || snapshot.Identities.Count == 1 && (String.IsNullOrEmpty(snapshot.Identities[0].Identity) || snapshot.Identities[0].Identity == "none")) skip = SshAuthenticationPreparationReason.NoIdentity;
                else if (snapshot.Identities.Count != 1) skip = SshAuthenticationPreparationReason.MultipleIdentities;
                if (skip.HasValue) {
                    freshness();
                    return result(skip.Value == SshAuthenticationPreparationReason.PublicKeyNotUsed ? SshAuthenticationPreparationState.NotUsed : SshAuthenticationPreparationState.Unconfirmed, skip.Value);
                }
                bool changedEndpoint = false;
                var scope = snapshot.ForSigning();
                int signingBudget = Math.Min(SshKeyReadiness.TimeoutMilliseconds, remaining());
                long signingStarted = elapsed();
                observation = SshKeyReadiness.Check(scope, snapshot.Identities[0].Identity, token,
                    (file, arguments, capturedEndpoint, budget, cancellation) => {
                        if (capturedEndpoint != endpoint || endpoint != Environment.GetEnvironmentVariable("SSH_AUTH_SOCK")) {
                            changedEndpoint = true; return null;
                        }
                        return (signing ?? DiagnosticProcess.RunReadOnly)(file, arguments, endpoint, budget, cancellation);
                    }, signingBudget, () => elapsed() - signingStarted);
                freshness();
                if (changedEndpoint || !snapshot.Same(new SshAuthenticationConfiguration(scope))) throw new PreparationChangedException();
                if (observation.State == SshKeyReadinessState.Cancelled) return result(SshAuthenticationPreparationState.Cancelled, SshAuthenticationPreparationReason.Cancelled);
                if (observation.State == SshKeyReadinessState.TimedOut) return result(SshAuthenticationPreparationState.TimedOut, SshAuthenticationPreparationReason.SigningTimedOut);
                if (observation.State == SshKeyReadinessState.Changed) throw new PreparationChangedException();
                var final = read();
                if (final == null || !snapshot.Same(new SshAuthenticationConfiguration(final))) throw new PreparationChangedException();
                freshness();
                if (observation.State == SshKeyReadinessState.AgentSigningVerified)
                    return result(SshAuthenticationPreparationState.LocalSigningObserved, SshAuthenticationPreparationReason.LocalSigningObserved);
                if (observation.State == SshKeyReadinessState.Busy) return result(SshAuthenticationPreparationState.Busy, SshAuthenticationPreparationReason.Busy);
                return result(SshAuthenticationPreparationState.Unconfirmed, SshAuthenticationPreparationReason.SigningUnconfirmed);
            } catch (OperationCanceledException) { return result(SshAuthenticationPreparationState.Cancelled, SshAuthenticationPreparationReason.Cancelled); }
            catch (TimeoutException) { return result(token.IsCancellationRequested ? SshAuthenticationPreparationState.Cancelled : SshAuthenticationPreparationState.TimedOut,
                token.IsCancellationRequested ? SshAuthenticationPreparationReason.Cancelled : SshAuthenticationPreparationReason.TimedOut); }
            catch (PreparationChangedException) { return result(SshAuthenticationPreparationState.Changed, SshAuthenticationPreparationReason.Changed); }
            catch { return result(token.IsCancellationRequested ? SshAuthenticationPreparationState.Cancelled : SshAuthenticationPreparationState.Failed,
                token.IsCancellationRequested ? SshAuthenticationPreparationReason.Cancelled : SshAuthenticationPreparationReason.Failed); }
        }
        private sealed class PreparationChangedException : Exception { }
    }
}

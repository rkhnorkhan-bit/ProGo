using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ProGo
{
    internal enum SshAgentKeyAddState
    {
        NotChecked, AuthenticationPolicyUnconfirmed, NotUsedByAuthentication, DisabledByProfile,
        UnsupportedEndpoint, IdentityNotSelected, PathUnresolved, MissingExecutable,
        Prepared, Busy, CommandCompleted, CommandFailed, AgentUnavailable, TimedOut, Cancelled, Failed
    }
    // An immutable selection, not evidence of private-key possession or authentication.
    internal sealed class SshAgentKeyAddRequest
    {
        internal string Executable { get; private set; }
        internal string IdentityPath { get; private set; }
        internal string AgentEndpoint { get; private set; }
        internal string Arguments { get { return "-k -- " + SshConnection.Quote(IdentityPath); } }
        private SshAgentKeyAddRequest(string executable, string identityPath, string agentEndpoint)
        { Executable = executable; IdentityPath = identityPath; AgentEndpoint = agentEndpoint; }
        private static bool Absolute(string path)
        {
            try { return Path.IsPathRooted(path) && String.Equals(Path.GetFullPath(path), path, StringComparison.OrdinalIgnoreCase); }
            catch (ArgumentException) { return false; }
            catch (NotSupportedException) { return false; }
            catch (PathTooLongException) { return false; }
        }

        internal static SshAgentKeyAddRequest Prepare(SshProfileDiagnosticResult profile, string identity,
            out SshAgentKeyAddState state)
        {
            state = SshAgentKeyAddState.NotChecked;
            if (profile == null || !profile.SshResolved) return null;
            if (SshPublicKeyPolicy.NotUsed(profile.PublicKeyPolicy)) { state = SshAgentKeyAddState.NotUsedByAuthentication; return null; }
            if (profile.PublicKeyPolicy != SshPublicKeyPolicyState.AllowedByClient) { state = SshAgentKeyAddState.AuthenticationPolicyUnconfirmed; return null; }
            if (profile.IdentityAgent == "none") { state = SshAgentKeyAddState.DisabledByProfile; return null; }
            if (!String.IsNullOrEmpty(profile.IdentityAgent)) { state = SshAgentKeyAddState.UnsupportedEndpoint; return null; }
            if (String.IsNullOrEmpty(identity) || !profile.IdentityFiles.Contains(identity)) { state = SshAgentKeyAddState.IdentityNotSelected; return null; }
            string path;
            if (!profile.IdentityPaths.TryGetValue(identity, out path) || String.IsNullOrEmpty(path) ||
                path.IndexOfAny(new[] { '\0', '\r', '\n' }) >= 0 || !Absolute(path)) {
                state = SshAgentKeyAddState.PathUnresolved; return null;
            }
            string executable = OpenSshClient.AgentExecutable(profile.ExecutablePath);
            if (String.IsNullOrEmpty(executable) || !Absolute(executable)) { state = SshAgentKeyAddState.MissingExecutable; return null; }
            state = SshAgentKeyAddState.Prepared;
            return new SshAgentKeyAddRequest(executable, path, Environment.GetEnvironmentVariable("SSH_AUTH_SOCK"));
        }
    }
    internal sealed class SshAgentKeyAddResult
    {
        internal SshAgentKeyAddState State { get; private set; }
        internal int? ExitCode { get; private set; }
        internal int? NativeError { get; private set; }
        internal SshAgentKeyAddResult(SshAgentKeyAddState state, int? exitCode = null, int? nativeError = null)
        { State = state; ExitCode = exitCode; NativeError = nativeError; }
        internal string ToDisplay()
        {
            switch (State) {
                case SshAgentKeyAddState.CommandCompleted:
                    return "ssh-add сообщил об успешном добавлении. Подпись ключом и вход на сервер ещё не проверены.";
                case SshAgentKeyAddState.Busy:
                    return "Другая операция с SSH-agent ещё выполняется. Дождитесь её завершения.";
                case SshAgentKeyAddState.AgentUnavailable:
                    return "ssh-add не смог связаться с агентом. Проверьте его доступность и повторите.";
                case SshAgentKeyAddState.CommandFailed:
                    return "ssh-add завершился с ошибкой. Проверьте файл ключа и права доступа; добавление не подтверждено.";
                case SshAgentKeyAddState.TimedOut:
                    return "Время ожидания ввода истекло; окно OpenSSH закрыто. Результат добавления нужно перепроверить.";
                case SshAgentKeyAddState.Cancelled:
                    return "Операция отменена. Результат добавления нужно перепроверить: уже принятое агентом добавление не отменяется.";
                default:
                    return "Добавление ключа не подтверждено. Повторите проверку профиля и доступности агента.";
            }
        }
    }
    internal static class SshAgentKeyAdd
    {
        // Backend for a future explicit Add Key UI action; never called at startup.
        // Cancellation cannot roll back an agent mutation. Do not delete shared keys.
        internal static Task<SshAgentKeyAddResult> RunAsync(SshAgentKeyAddRequest request, CancellationToken token,
            int timeoutMs = 120000)
        {
            Validate(request, timeoutMs);
            return Task.Factory.StartNew(() => Run(request, token, null, timeoutMs), CancellationToken.None,
                TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }
        private static void Validate(SshAgentKeyAddRequest request, int timeoutMs)
        {
            if (request == null) throw new ArgumentNullException("request");
            if (timeoutMs < 1 || timeoutMs > 300000) throw new ArgumentOutOfRangeException("timeoutMs");
        }
        internal static SshAgentKeyAddResult Run(SshAgentKeyAddRequest request, CancellationToken token,
            Func<string, string, string, int, CancellationToken, int> run = null, int timeoutMs = 120000)
        {
            Validate(request, timeoutMs);
            try {
                token.ThrowIfCancellationRequested();
                using (var lease = SshReadOnlyLease.TryAcquire(token)) {
                    if (lease == null) return new SshAgentKeyAddResult(SshAgentKeyAddState.Busy);
                    token.ThrowIfCancellationRequested();
                    int code = run == null ? SshInteractiveProcess.Run(request.Executable, request.Arguments, request.AgentEndpoint, timeoutMs, token) :
                        run(request.Executable, request.Arguments, request.AgentEndpoint, timeoutMs, token);
                    token.ThrowIfCancellationRequested();
                    return new SshAgentKeyAddResult(code == 0 ? SshAgentKeyAddState.CommandCompleted :
                        code == 2 ? SshAgentKeyAddState.AgentUnavailable : SshAgentKeyAddState.CommandFailed, code);
                }
            } catch (OperationCanceledException) { return new SshAgentKeyAddResult(SshAgentKeyAddState.Cancelled); }
            catch (TimeoutException) { return new SshAgentKeyAddResult(token.IsCancellationRequested ? SshAgentKeyAddState.Cancelled : SshAgentKeyAddState.TimedOut); }
            catch (Win32Exception ex) { return new SshAgentKeyAddResult(token.IsCancellationRequested ? SshAgentKeyAddState.Cancelled : SshAgentKeyAddState.Failed, null, ex.NativeErrorCode); }
            catch { return new SshAgentKeyAddResult(token.IsCancellationRequested ? SshAgentKeyAddState.Cancelled : SshAgentKeyAddState.Failed); }
        }
    }
}

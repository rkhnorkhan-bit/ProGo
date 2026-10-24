using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace ProGo
{
    internal enum SshKeyReadinessState
    {
        NotChecked, NotUsedByAuthentication, AuthenticationPolicyUnconfirmed, DisabledByProfile,
        UnsupportedEndpoint, IdentityNotSelected, PathUnresolved, MissingExecutable, MissingIdentity,
        FileUnavailable, AgentSigningVerified, Busy, Changed, InvalidOutput, CommandFailed,
        AgentUnavailable, TimedOut, Cancelled, Failed
    }
    internal sealed class SshKeyReadinessResult
    {
        internal SshKeyReadinessState State { get; private set; }
        internal int? ExitCode { get; private set; }
        internal SshKeyReadinessResult(SshKeyReadinessState state, int? exitCode = null)
        { State = state; ExitCode = exitCode; }
        internal string ToDisplay()
        {
            switch (State) {
                case SshKeyReadinessState.AgentSigningVerified:
                    return "Агент выполнил подпись, и OpenSSH проверил её для публичной identity выбранного файла. Содержимое закрытого файла и вход на сервер этим не подтверждены.";
                case SshKeyReadinessState.Busy:
                    return "Другая операция с SSH-agent ещё выполняется. Проверку подписи можно повторить после её завершения.";
                case SshKeyReadinessState.MissingIdentity:
                    return "Выбранный файл и соседний .pub не найдены. Проверьте локальный путь; другие способы входа не исключены.";
                case SshKeyReadinessState.FileUnavailable:
                    return "Файл не удалось безопасно прочитать для проверки подписи. Проверьте путь, права и размер; файл не изменялся.";
                case SshKeyReadinessState.Changed:
                    return "Профиль, выбранный файл или окружение агента изменились. Старый результат не принят; повторите проверку.";
                case SshKeyReadinessState.AgentUnavailable:
                    return "OpenSSH не получил ответ выбранного агента. Прямые ключи и другие способы входа остаются возможны.";
                case SshKeyReadinessState.CommandFailed:
                    return "Агент не подтвердил подпись выбранной публичной identity. Проверьте выбранный ключ и доступ к агенту; причина требует отдельной проверки.";
                case SshKeyReadinessState.TimedOut:
                    return "Время проверки подписи истекло. Готовность ключа не подтверждена; можно повторить.";
                case SshKeyReadinessState.Cancelled:
                    return "Проверка подписи отменена; готовность ключа не подтверждена.";
                case SshKeyReadinessState.NotUsedByAuthentication:
                    return "Политика профиля не использует вход по публичному ключу. Другие способы входа требуют проверки подключения.";
                case SshKeyReadinessState.DisabledByProfile:
                    return "IdentityAgent none: подпись агентом не проверялась. Прямой ключ и другие способы входа не запрещены.";
                case SshKeyReadinessState.UnsupportedEndpoint:
                    return "Профиль задаёт отдельный агент. Его подпись не проверялась; стандартный агент не подставлялся.";
                default:
                    return "Подпись выбранной identity не подтверждена. Проверка не запрещает другие способы SSH-аутентификации.";
            }
        }
    }

    // Ephemeral local signing observation, never a server authentication gate.
    // OpenSSH chooses a public reference (possibly .pub). Valid agent signatures
    // do not prove that this path contains a usable or matching private key.
    internal static class SshKeyReadiness
    {
        internal const int TimeoutMilliseconds = 3000;
        internal const int FileLimit = 256 * 1024;
        private sealed class Stamp
        {
            internal readonly bool Present;
            internal readonly string Digest;
            internal Stamp(bool present, string digest = null) { Present = present; Digest = digest; }
            internal bool Same(Stamp other) { return Present == other.Present && Digest == other.Digest; }
        }
        private static SshKeyReadinessResult Result(SshKeyReadinessState state, int? exit = null)
        { return new SshKeyReadinessResult(state, exit); }
        private static void Deadline(CancellationToken token, int timeout, Func<long> elapsed)
        {
            token.ThrowIfCancellationRequested();
            long spent = elapsed();
            if (spent < 0 || spent >= timeout) throw new TimeoutException();
        }
        private static bool LocalPath(string path)
        {
            if (String.IsNullOrEmpty(path) || path.Length > 32768 || path.IndexOfAny(new[] { '\0', '\r', '\n' }) >= 0) return false;
            try {
                if (!Path.IsPathRooted(path) || !String.Equals(Path.GetFullPath(path), path, StringComparison.OrdinalIgnoreCase)) return false;
                return Path.DirectorySeparatorChar != '\\' || path.Length >= 3 && Char.IsLetter(path[0]) && path[1] == ':' &&
                    (path[2] == '\\' || path[2] == '/');
            } catch { return false; }
        }
        private static void CheckAncestors(string path)
        {
            string parent = Path.GetDirectoryName(path);
            while (!String.IsNullOrEmpty(parent)) {
                try {
                    if ((File.GetAttributes(parent) & FileAttributes.ReparsePoint) != 0) throw new IOException("Unconfirmed identity path.");
                } catch (FileNotFoundException) { } catch (DirectoryNotFoundException) { }
                string next = Path.GetDirectoryName(parent);
                if (String.Equals(next, parent, StringComparison.OrdinalIgnoreCase)) break;
                parent = next;
            }
        }
        private static Stamp ReadStamp(string path, CancellationToken token, int timeout, Func<long> elapsed)
        {
            Deadline(token, timeout, elapsed); CheckAncestors(path);
            FileAttributes attributes;
            try { attributes = File.GetAttributes(path); }
            catch (FileNotFoundException) { return new Stamp(false); }
            catch (DirectoryNotFoundException) { return new Stamp(false); }
            if ((attributes & (FileAttributes.ReparsePoint | FileAttributes.Directory)) != 0) throw new IOException("Unconfirmed identity file.");
            using (var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var digest = SHA256.Create()) {
                if (input.Length > FileLimit) throw new IOException("Identity snapshot limit exceeded.");
                var buffer = new byte[4096];
                try {
                    int count, total = 0;
                    while (true) {
                        Deadline(token, timeout, elapsed); count = input.Read(buffer, 0, buffer.Length);
                        if (count == 0) break;
                        total += count;
                        if (total > FileLimit) throw new IOException("Identity snapshot limit exceeded.");
                        digest.TransformBlock(buffer, 0, count, buffer, 0);
                    }
                    digest.TransformFinalBlock(new byte[0], 0, 0);
                    Deadline(token, timeout, elapsed);
                    return new Stamp(true, Convert.ToBase64String(digest.Hash));
                } finally { Array.Clear(buffer, 0, buffer.Length); }
            }
        }
        private static string Scope(SshProfileDiagnosticResult profile, string identity)
        {
            string path; profile.IdentityPaths.TryGetValue(identity, out path);
            var text = new StringBuilder();
            foreach (string field in new[] { profile.Target, profile.OriginalHost, profile.ConfigPath, profile.ExecutablePath,
                profile.ResolvedHostName, profile.ResolvedUser, profile.ResolvedPort, profile.IdentityAgent, profile.IdentitiesOnly,
                profile.PubkeyAuthentication, profile.PreferredAuthentications, identity, path })
                text.Append(field == null ? -1 : field.Length).Append(':').Append(field).Append(';');
            text.Append(profile.SshResolved).Append(';').Append(profile.IdentityListLimited).Append(';');
            foreach (string item in profile.IdentityFiles) text.Append(item == null ? -1 : item.Length).Append(':').Append(item).Append(';');
            return text.ToString();
        }
        internal static SshKeyReadinessResult Check(SshProfileDiagnosticResult profile, string identity, CancellationToken token,
            Func<string, string, string, int, CancellationToken, DiagnosticProcessResult> run = null,
            int timeoutMs = TimeoutMilliseconds, Func<long> elapsedMilliseconds = null)
        {
            if (timeoutMs < 1 || timeoutMs > 30000) throw new ArgumentOutOfRangeException("timeoutMs");
            var watch = Stopwatch.StartNew(); Func<long> elapsed = elapsedMilliseconds ?? (() => watch.ElapsedMilliseconds);
            bool readingFiles = false;
            try {
                Deadline(token, timeoutMs, elapsed);
                if (profile == null || !profile.SshResolved) return Result(SshKeyReadinessState.NotChecked);
                if (SshPublicKeyPolicy.NotUsed(profile.PublicKeyPolicy)) return Result(SshKeyReadinessState.NotUsedByAuthentication);
                if (profile.PublicKeyPolicy != SshPublicKeyPolicyState.AllowedByClient) return Result(SshKeyReadinessState.AuthenticationPolicyUnconfirmed);
                if (profile.IdentityAgent == "none") return Result(SshKeyReadinessState.DisabledByProfile);
                if (!String.IsNullOrEmpty(profile.IdentityAgent)) return Result(SshKeyReadinessState.UnsupportedEndpoint);
                if (String.IsNullOrEmpty(identity) || identity == "none" || !profile.IdentityFiles.Contains(identity)) return Result(SshKeyReadinessState.IdentityNotSelected);
                string path;
                if (!profile.IdentityPaths.TryGetValue(identity, out path) || !LocalPath(path)) return Result(SshKeyReadinessState.PathUnresolved);
                string executable = OpenSshClient.AgentExecutable(profile.ExecutablePath);
                if (!LocalPath(executable)) return Result(SshKeyReadinessState.MissingExecutable);
                string scope = Scope(profile, identity), endpoint = Environment.GetEnvironmentVariable("SSH_AUTH_SOCK");
                if (endpoint != null && (endpoint.Length == 0 || endpoint.Length > 32768 || endpoint.IndexOfAny(new[] { '\0', '\r', '\n' }) >= 0))
                    return Result(SshKeyReadinessState.UnsupportedEndpoint);
                using (var lease = SshReadOnlyLease.TryAcquire(token)) {
                    if (lease == null) return Result(SshKeyReadinessState.Busy);
                    readingFiles = true;
                    var primary = ReadStamp(path, token, timeoutMs, elapsed);
                    var sidecar = ReadStamp(path + ".pub", token, timeoutMs, elapsed);
                    if (!primary.Present && !sidecar.Present) return Result(SshKeyReadinessState.MissingIdentity);
                    Deadline(token, timeoutMs, elapsed);
                    if (scope != Scope(profile, identity) || endpoint != Environment.GetEnvironmentVariable("SSH_AUTH_SOCK"))
                        return Result(SshKeyReadinessState.Changed);
                    readingFiles = false;
                    var capture = (run ?? DiagnosticProcess.RunReadOnly)(executable, "-T -- " + SshConnection.Quote(path), endpoint,
                        (int)Math.Max(1, timeoutMs - elapsed()), token);
                    Deadline(token, timeoutMs, elapsed); readingFiles = true;
                    if (scope != Scope(profile, identity) || endpoint != Environment.GetEnvironmentVariable("SSH_AUTH_SOCK") ||
                        !primary.Same(ReadStamp(path, token, timeoutMs, elapsed)) || !sidecar.Same(ReadStamp(path + ".pub", token, timeoutMs, elapsed)))
                        return Result(SshKeyReadinessState.Changed);
                    Deadline(token, timeoutMs, elapsed);
                    if (capture == null) return Result(SshKeyReadinessState.Failed);
                    if (capture.Truncated || (capture.Output ?? "").Length > DiagnosticProcess.CaptureLimit ||
                        (capture.Error ?? "").Length > DiagnosticProcess.CaptureLimit) return Result(SshKeyReadinessState.InvalidOutput, capture.ExitCode);
                    if (capture.ExitCode == 2) return Result(SshKeyReadinessState.AgentUnavailable, 2);
                    if (capture.ExitCode != 0) return Result(SshKeyReadinessState.CommandFailed, capture.ExitCode);
                    if (!String.IsNullOrWhiteSpace(capture.Output) || !String.IsNullOrWhiteSpace(capture.Error)) return Result(SshKeyReadinessState.InvalidOutput, 0);
                    // The post-command file reads may block. Recheck the selected
                    // scope after those reads, immediately before accepting proof.
                    Deadline(token, timeoutMs, elapsed);
                    if (scope != Scope(profile, identity) || endpoint != Environment.GetEnvironmentVariable("SSH_AUTH_SOCK"))
                        return Result(SshKeyReadinessState.Changed);
                    token.ThrowIfCancellationRequested();
                    return Result(SshKeyReadinessState.AgentSigningVerified, 0);
                }
            } catch (OperationCanceledException) { return Result(SshKeyReadinessState.Cancelled); }
            catch (TimeoutException) { return Result(token.IsCancellationRequested ? SshKeyReadinessState.Cancelled : SshKeyReadinessState.TimedOut); }
            catch (IOException) { return Result(readingFiles ? SshKeyReadinessState.FileUnavailable : SshKeyReadinessState.Failed); }
            catch (UnauthorizedAccessException) { return Result(SshKeyReadinessState.FileUnavailable); }
            catch { return Result(token.IsCancellationRequested ? SshKeyReadinessState.Cancelled : SshKeyReadinessState.Failed); }
        }
    }
}

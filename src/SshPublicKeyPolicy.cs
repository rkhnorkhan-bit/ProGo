using System;

namespace ProGo
{
    internal enum SshPublicKeyPolicyState { Unknown, Disabled, Excluded, AllowedByClient }

    // Effective client policy only. AllowedByClient is never key/auth readiness.
    internal static class SshPublicKeyPolicy
    {
        internal static SshPublicKeyPolicyState Read(bool resolved, string pubkey, string preferred)
        {
            if (!resolved) return SshPublicKeyPolicyState.Unknown;
            string mode = pubkey != null && pubkey.Length <= 32 && pubkey.IndexOfAny(new[] { '\r', '\n', '\0' }) < 0 ? pubkey.Trim() : "";
            if (String.Equals(mode, "no", StringComparison.OrdinalIgnoreCase) || String.Equals(mode, "false", StringComparison.OrdinalIgnoreCase))
                return SshPublicKeyPolicyState.Disabled;
            if (String.IsNullOrEmpty(preferred) || preferred.Length > 2048) return SshPublicKeyPolicyState.Unknown;
            string[] methods = preferred.Split(',');
            if (methods.Length > 32) return SshPublicKeyPolicyState.Unknown;
            bool publickey = false;
            foreach (string method in methods) {
                // Reject malformed/unknown list syntax rather than guessing defaults,
                // substrings, algorithm-list modifiers or case folding method names.
                if (method.Length == 0 || method.Length > 64) return SshPublicKeyPolicyState.Unknown;
                foreach (char c in method) if (!((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') ||
                    c == '-' || c == '_' || c == '.' || c == '@')) return SshPublicKeyPolicyState.Unknown;
                if (method == "publickey") publickey = true;
            }
            if (!publickey) return SshPublicKeyPolicyState.Excluded;
            return String.Equals(mode, "yes", StringComparison.OrdinalIgnoreCase) || String.Equals(mode, "true", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(mode, "unbound", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(mode, "host-bound", StringComparison.OrdinalIgnoreCase)
                ? SshPublicKeyPolicyState.AllowedByClient : SshPublicKeyPolicyState.Unknown;
        }
        internal static bool NotUsed(SshPublicKeyPolicyState state)
        { return state == SshPublicKeyPolicyState.Disabled || state == SshPublicKeyPolicyState.Excluded; }
        internal static string ToDisplay(SshPublicKeyPolicyState state)
        {
            if (state == SshPublicKeyPolicyState.Disabled)
                return "PubkeyAuthentication=no: вход по публичному ключу отключён профилем. Другие методы требуют отдельной проверки подключения.";
            if (state == SshPublicKeyPolicyState.Excluded)
                return "В PreferredAuthentications выбранного профиля нет publickey. Другие методы требуют отдельной проверки подключения.";
            if (state == SshPublicKeyPolicyState.AllowedByClient)
                return "Профиль допускает попытку входа по публичному ключу; пригодность ключа, подпись и вход на сервер не подтверждены.";
            return "Политика входа по публичному ключу не подтверждена; готовность ключа и других методов входа не определена.";
        }
        internal const string SkippedFile = "Файл ключа не проверялся: политика профиля не использует вход по публичному ключу.";
    }
}

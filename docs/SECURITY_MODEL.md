# Security Model

## Threat model

ProGo protects local vault data at rest against casual local access and accidental disclosure. It does not claim to defeat a fully compromised Windows account, malware, keyloggers, memory scraping, or an attacker with unrestricted runtime access.

## Local attacker assumptions

An attacker may obtain a copy of `%LOCALAPPDATA%\ProGo\vault.enc.json` and attempt offline guessing.

The current MVP uses a 4-digit PIN. This is a weak credential with 10,000 combinations. PBKDF2 slows guessing but does not turn a 4-digit PIN into a strong master password.

## Vault encryption

Current format:

- `version`: 1
- `kdf`: `PBKDF2-HMAC-SHA256`
- `iterations`: 120000
- `cipher`: `AES-256-CBC`
- `mac`: `HMAC-SHA256`
- structure: encrypt-then-MAC

Key material is derived from the PIN and per-vault random salt. The derived material is split into encryption and MAC keys.

## Integrity

Vault ciphertext and envelope metadata are authenticated with HMAC-SHA256 before decryption.

## Clipboard

Secrets are copied only through an explicit Copy Secret action. ProGo schedules cleanup and clears the clipboard only if it still contains the exact copied ProGo value.

## Logs

Logs may contain startup, shutdown, proxy lifecycle, PID, port, route status, vault open/lock/save, and safe errors.

Logs must not contain PINs, secrets, API keys, tokens, Authorization headers, decrypted vault data, or clipboard values.

## Current PIN and daily alternate data policy

The approved F09 contract is defined in [APPROVED_PLAN_31.md](APPROVED_PLAN_31.md#f09--настоящий-и-временный-ложный-pin).
One permanent four-digit PIN opens the real vault. The first correctly formatted
incorrect PIN of the local Windows day becomes that day's only alternate PIN.
Repeating it opens the same separately persisted alternate data; other incorrect
PINs show «Неверный пин-код». Opening the real vault does not reset the daily PIN.
The daily state survives restart and expires at the local day boundary, locking
an open session and removing expired alternate data before another open.

Real and daily data use separate atomic storage. Background I/O and KDF failures
must not appear as successful opening or saving. A MAC mismatch in an otherwise
valid real envelope cannot reliably distinguish an incorrect PIN from tampering.
No PIN, vault contents or daily-session identity is written to logs. This is not
a proven cryptographic plausible-deniability design.

The earlier sample-for-every-incorrect-PIN behavior and proposed two permanent
credentials are historical descriptions, superseded by the approved daily policy.
They are not implementation instructions. Code/CI/package/device acceptance remain
separate in [PROJECT_STATE.md](PROJECT_STATE.md).

## SSH boundaries

ProGo controls `ssh.exe` with an OpenSSH profile. It does not store SSH passwords or SSH private keys. SSH trust, host key verification, and key protection remain the user's OpenSSH responsibility.

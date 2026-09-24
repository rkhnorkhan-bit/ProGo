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

## Current decoy-style behavior

When the vault cannot be decrypted with the entered PIN, the UI opens a stable local sample dataset for that PIN instead of showing a PIN error. This is a usability and disclosure-reduction mechanism, not a proven cryptographic plausible-deniability design.

This behavior must not be disclosed through UI, tooltip, or logs.

## Future duress vault design

A stronger future design would use two independent credentials:

- real credential -> real encrypted vault;
- duress credential -> independent encrypted vault.

That change requires separate approval because it changes credential semantics and vault format.

## SSH boundaries

ProGo controls `ssh.exe` with an OpenSSH profile. It does not store SSH passwords or SSH private keys. SSH trust, host key verification, and key protection remain the user's OpenSSH responsibility.

# Data Format

## Settings

Path:

```text
%LOCALAPPDATA%\ProGo\settings.json
```

Shape:

```json
{
  "SocksHost": "127.0.0.1",
  "SocksPort": 1080,
  "SshProfile": "my-vps",
  "AutoStartSocks": false,
  "AutoRestartSocks": true,
  "AutoCliProxy": false,
  "ClipboardClearSeconds": 30,
  "TestEndpoint": "https://api.openai.com/v1/models"
}
```

Do not store SSH passwords, private keys, API keys, or production secrets in settings.

`AutoCliProxy` is the single automatic option for terminals and ordinary Codex
launches. When this field is absent, load migrates the old `AutoApplyProxy` and
`AutoCodexProxy` fields using logical OR. When it is present, its explicit value
wins over either legacy field. Save writes only `AutoCliProxy`; the legacy fields
are not retained, so a saved off cannot become on again after reloading. Clone
retains the canonical value. Windows proxy automation is independent.

`AutoRestartSocks` defaults to `true` when omitted by an older settings file.
An explicit `false` is preserved. Recovery only follows a SOCKS start requested
in the current ProGo session; it does not enable `AutoStartSocks`.

## Vault

Path:

```text
%LOCALAPPDATA%\ProGo\vault.enc.json
```

Envelope fields:

```json
{
  "version": 1,
  "kdf": "PBKDF2-HMAC-SHA256",
  "iterations": 120000,
  "cipher": "AES-256-CBC",
  "mac": "HMAC-SHA256",
  "salt": "base64",
  "iv": "base64",
  "ciphertext": "base64",
  "tag": "base64"
}
```

The decrypted payload contains:

```json
{
  "version": 1,
  "entries": [
    {
      "id": "stable-id",
      "name": "name",
      "type": "api_key | password | token | ssh | note | custom",
      "login": "login",
      "secret": "secret",
      "url_or_host": "url or host",
      "notes": "notes",
      "tags": "comma separated tags",
      "created_at": "ISO-8601 UTC",
      "updated_at": "ISO-8601 UTC"
    }
  ]
}
```

Internal type values are stable English identifiers. UI display names may be Russian.

## Logs

Path:

```text
%LOCALAPPDATA%\ProGo\progo.log
```

Logs are operational only and must not contain decrypted secret values.

## Home VPN access

`home-vpn-private/*.dat` contains current-user DPAPI-protected invitations, owner
connection metadata and the home entry address. These files are independent of
the vault. Runtime SSH keys are written into a current-user-only session directory
and removed on orderly stop. No administrator password is stored. A `PROGO1.` token
is a bearer credential, not a signed identity claim: accept it only from a trusted
VPS owner. It contains per-invitation access and a pinned server host key.

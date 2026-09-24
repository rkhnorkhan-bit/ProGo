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
  "AutoApplyProxy": false,
  "ClipboardClearSeconds": 30,
  "TestEndpoint": "https://api.openai.com/v1/models"
}
```

Do not store SSH passwords, private keys, API keys, or production secrets in settings.

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

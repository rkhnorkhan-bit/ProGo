# ProGo

ProGo is a small Windows tray application for fast SSH SOCKS proxy control and a local encrypted credentials vault.

## Status

Version: `0.1.28`.

This project has no vendor-operated backend, no telemetry, and no hosted secrets storage.

## Platform

- Windows 10/11 x64
- .NET Framework 4.8 runtime
- Windows OpenSSH client (`ssh.exe`)
- Windows PowerShell 5.1 or PowerShell 7.x for scripts

## Features

- Start, stop, and restart an SSH SOCKS tunnel from tray.
- Configurable SOCKS host, port, and SSH profile.
- User-level `ALL_PROXY` / `HTTPS_PROXY` / `HTTP_PROXY` variables routed through the loopback HTTP CONNECT bridge at `http://127.0.0.1:1881`.
- Current-user Windows system proxy toggle for WinINet/browser login flows.
- CLI/Codex HTTP CONNECT proxy bridge for tools that need a normal `http://` proxy instead of SOCKS.
- Experimental [home IKEv2 relay for iPhone](docs/HOME_IKEV2.md): transport encrypted VPN packets through the existing SSH/SOCKS tunnel to a self-hosted strongSwan server. Requires router forwarding and a small helper on the VPS; authentication stays on the VPS.
- Route check through `curl.exe --socks5-hostname`.
- Local encrypted vault at `%LOCALAPPDATA%\ProGo\vault.enc.json`.
- Vault entry types: `api_key`, `password`, `token`, `ssh`, `note`, `custom`.
- Copy secret by explicit action only.
- Clipboard auto-clear if the clipboard still contains the copied ProGo value.
- Russian UI.
- Direct install from GitHub.
- Tray item `Обновить ProGo`: installs the latest published release package with backup and rollback; a source build is available as a recovery fallback.
- Build/install/update/uninstall/test scripts.
- GitHub Actions Windows build.

## Security limitations

The current MVP uses a 4-digit PIN. This is convenient but weak: it has only 10,000 combinations. Do not treat it as a strong master password.

Vault data is encrypted at rest with:

- AES-256-CBC
- HMAC-SHA256
- PBKDF2-HMAC-SHA256
- encrypt-then-MAC structure

Logs are designed not to contain PINs, secrets, tokens, API keys, Authorization headers, decrypted vault data, or clipboard values.

## SSH setup

Create an SSH alias in your standard OpenSSH config:

```sshconfig
Host my-vps
  HostName example.com
  User deploy
  IdentityFile ~/.ssh/id_ed25519
```

Then set `my-vps` as the SSH profile in ProGo settings.

ProGo runs a command equivalent to:

```powershell
ssh.exe -N -D 127.0.0.1:1080 -o ExitOnForwardFailure=yes -o ServerAliveInterval=30 -o ServerAliveCountMax=3 my-vps
```

ProGo never stores SSH private keys or SSH passwords in source code.

## Windows system proxy

ProGo can enable a current-user Windows system proxy from the tray menu:

```text
Включить системный прокси Windows
```

This writes current-user WinINet settings under HKCU and points Windows proxy-aware applications to the local SSH SOCKS endpoint:

```text
socks=127.0.0.1:1080
```

Before changing Windows proxy settings, ProGo saves the previous current-user proxy values to `%LOCALAPPDATA%\ProGo\system-proxy-backup.json`. Use this tray action to restore them:

```text
Отключить системный прокси Windows
```

This mode is intended for browser/login flows. Restart any already-open browser that should pick up the proxy settings.

Scope and limits:

- current user only;
- no administrator rights required;
- no machine-wide WinHTTP changes;
- no VPN/TUN/WFP traffic interception;
- applications that ignore Windows proxy settings may still connect directly.

## CLI/Codex proxy

Some CLI tools do not use Windows system proxy settings and may not reliably use `socks5h://` proxy environment variables. ProGo can start a local HTTP CONNECT proxy bridge for those tools:

```text
Codex CLI -> http://127.0.0.1:1881 -> SOCKS 127.0.0.1:<SOCKS-port> -> SSH tunnel
```

Tray actions:

```text
Запустить CLI/Codex proxy
Применить CLI proxy env
Открыть PowerShell с CLI proxy
Остановить CLI/Codex proxy
```

`Запустить CLI/Codex proxy` starts a loopback-only listener at `127.0.0.1:1881`. It accepts HTTP `CONNECT` only and relays the stream through the current SOCKS tunnel.

`Применить CLI proxy env` writes user-level environment variables:

```text
ALL_PROXY=http://127.0.0.1:1881
HTTPS_PROXY=http://127.0.0.1:1881
HTTP_PROXY=http://127.0.0.1:1881
NO_PROXY=localhost,127.0.0.1,::1
```

Already-open terminals do not receive new user-level environment variables automatically. For immediate use, choose `Открыть PowerShell с CLI proxy`, then run `codex login` in the opened shell. ProGo restores the `127.0.0.1:1881` listener on startup when the user environment already points to it, and explicit CLI-proxy stop/ProGo exit clears ProGo-owned proxy env values to avoid stale loopback ports.

## Direct install from GitHub

Recommended standalone install on a fresh Windows machine:

```powershell
$script = Join-Path $env:TEMP "Install-FromGitHub.ps1"
Invoke-WebRequest "https://raw.githubusercontent.com/rkhnorkhan-bit/ProGo/main/scripts/Install-FromGitHub.ps1" -OutFile $script -UseBasicParsing
powershell.exe -NoProfile -ExecutionPolicy Bypass -File $script
```

The script downloads the current `main` branch from GitHub, builds `ProGo.exe` using the stock .NET Framework compiler, and installs it to:

```text
%LOCALAPPDATA%\ProGo
```

A startup shortcut is created by default. To disable it:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File $script -NoStartup
```

## Install from local clone

```powershell
git clone https://github.com/rkhnorkhan-bit/ProGo.git
cd ProGo
.\scripts\Install-ProGo.ps1
```

## Build

```powershell
.\scripts\Build-ProGo.ps1
```

The build script uses:

```text
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
```

## Update

Use the tray menu item:

```text
Обновить ProGo
```

The updater:

1. starts `%LOCALAPPDATA%\ProGo\scripts\Update-ProGo.ps1`;
2. exits the running ProGo process;
3. downloads the latest `main` branch from GitHub;
4. rebuilds locally;
5. replaces `%LOCALAPPDATA%\ProGo\ProGo.exe`;
6. starts the updated ProGo.

Existing `settings.json`, `vault.enc.json`, and logs are preserved.

CLI update:

```powershell
%LOCALAPPDATA%\ProGo\scripts\Update-ProGo.ps1
```

## Uninstall

```powershell
.\scripts\Uninstall-ProGo.ps1
```

By default, uninstall keeps user data. To request user data removal:

```powershell
.\scripts\Uninstall-ProGo.ps1 -RemoveUserData
```

The script then requires explicit `DELETE` confirmation.

## Test

```powershell
.\scripts\Test-ProGo.ps1
```

The test script performs repository hygiene checks and a Windows build smoke test.

## License

ProGo is source-available for personal and noncommercial use. Commercial use requires separate written permission from the repository owner. See `LICENSE.md` and `COMMERCIAL_USE.md`.

## Responsible disclosure

Do not publish real secrets, vault files, tokens, keys, or credentials in issues or pull requests. See `SECURITY.md`.

# ProGo

ProGo is a small Windows tray application for fast SSH SOCKS proxy control and a local encrypted credentials vault.

## Status

Version: `0.1.0` bootstrap MVP.

This project is intentionally small: no cloud backend, no telemetry, no hosted secrets storage.

## Platform

- Windows 10/11 x64
- .NET Framework 4.8 runtime
- Windows OpenSSH client (`ssh.exe`)
- Windows PowerShell 5.1 or PowerShell 7.x for scripts

## Features

- Start, stop, and restart an SSH SOCKS tunnel from tray.
- Configurable SOCKS host, port, and SSH profile.
- User-level proxy environment variables: `ALL_PROXY`, `HTTPS_PROXY`, `HTTP_PROXY` and lowercase aliases.
- Route check through `curl.exe --socks5-hostname`.
- Local encrypted vault at `%LOCALAPPDATA%\ProGo\vault.enc.json`.
- Vault entry types: `api_key`, `password`, `token`, `ssh`, `note`, `custom`.
- Copy secret by explicit action only.
- Clipboard auto-clear if the clipboard still contains the copied ProGo value.
- Russian UI.
- Direct install from GitHub.
- Tray item `Обновить ProGo`: downloads the latest `main` from GitHub, rebuilds locally, and replaces the installed `ProGo.exe`.
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

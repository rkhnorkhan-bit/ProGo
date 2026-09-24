# Development

## Requirements

- Windows 10/11 x64
- .NET Framework 4.8 Developer Pack or Windows compiler path available
- Windows PowerShell 5.1
- Optional: PowerShell 7.x
- OpenSSH client
- curl.exe

## Build

```powershell
.\scripts\Build-ProGo.ps1
```

## Test

```powershell
.\scripts\Test-ProGo.ps1
```

## Install locally

```powershell
.\scripts\Install-ProGo.ps1
```

## Uninstall locally

```powershell
.\scripts\Uninstall-ProGo.ps1
```

## Repository hygiene

Never commit:

- `vault*.json`
- `vault*.enc*`
- `.env`, `.env.*`
- `*.pem`, `*.key`, `*.pfx`, `*.p12`
- logs
- credentials
- real host/IP/login/token/password values in examples

## Change control

Separate approval is required for changes to:

- vault file format;
- encryption algorithm;
- KDF;
- PIN policy;
- decoy/duress semantics;
- license/commercial permissions;
- telemetry;
- auto-update;
- backend/cloud sync.

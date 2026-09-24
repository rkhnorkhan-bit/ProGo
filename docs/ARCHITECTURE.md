# Architecture

ProGo is a small Windows tray application.

## Runtime

- Language: C#
- UI: WinForms
- Runtime target: .NET Framework 4.8
- Build compiler: `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`
- Required external tools: `ssh.exe`, `curl.exe`

## Main components

| Component | Responsibility |
| --- | --- |
| `Program` | WinForms entry point and service wiring |
| `AppPaths` | `%LOCALAPPDATA%\ProGo` paths |
| `SettingsService` | JSON settings load/save |
| `SafeLog` | Redacted logging |
| `ProxyService` | SSH SOCKS process lifecycle |
| `EnvironmentProxyService` | user-level proxy environment variables |
| `RouteTester` | route check through SOCKS using `curl.exe` |
| `VaultService` | local encrypted vault load/save |
| `ClipboardService` | explicit copy and safe auto-clear |
| `TrayApplicationContext` | tray menu and form navigation |
| WinForms forms | settings, status, vault, entry editing, PIN, port |

## Data paths

```text
%LOCALAPPDATA%\ProGo\settings.json
%LOCALAPPDATA%\ProGo\vault.enc.json
%LOCALAPPDATA%\ProGo\progo.log
```

## Non-goals for 0.1.x

- no backend;
- no cloud sync;
- no telemetry;
- no auto-update;
- no migration from legacy applications;
- no storage of SSH passwords or SSH private keys in ProGo.

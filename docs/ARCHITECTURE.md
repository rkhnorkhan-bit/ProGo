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
| `CliProxyEnvironmentService` | user-level proxy environment variables |
| `RouteTester` | route check through SOCKS using `curl.exe` |
| `VaultService` | local encrypted vault load/save |
| `ClipboardService` | explicit copy and safe auto-clear |
| `UpdateAwareTrayApplicationContext` | tray menu and form navigation |
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

## 0.2 desktop and settings

`MainWindow` hosts the native dashboard; `UiTheme` and `BrandIcon` are shared by all
application forms. `AutomationPlan` keeps one pending startup action per enabled
option: one for the shared terminal/Codex environment and one for Windows proxy.
Explicit off cancels that action. `CliProxyEnvironmentService` applies and restores
the shared current-user environment for ordinary launches. `CodexProxyService`
only creates or opens an optional scoped launcher; it does not change Codex config.
Windows and terminal traffic use the local HTTP/CONNECT bridge.

`ApplicationInstance` holds a global, SID-scoped mutex for the normal application
lifetime. It is acquired before shared data and services. Repeat launches use a
same-user named pipe with one activation command; no file paths, credentials or
arbitrary actions are accepted. Startup activation is queued until the tray
context attaches and dispatches it to the UI thread. Update/restore handoff
serialization is a separate pending part of audit F03.

`UpdateLauncher` executes the installed updater file. The updater resolves a pinned
release asset and SHA-256 from GitHub metadata and validates paths before unpacking.
The existing backup/staging/rollback sequence remains. No source-build fallback or
remote script evaluation is used in the 0.2 runtime.

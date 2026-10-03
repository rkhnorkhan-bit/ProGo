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
.\scripts\Test-HomeVpnRelay.ps1
python .\scripts\check_public_content.py --release .\release
```

The relay tests and public-content check require Python 3.12 in CI. The content
check covers source files and the compiled release, including UTF-16 strings in
the executable. Use reserved documentation addresses and generic paths in public
examples; manual review is still required for names, hostnames, and context.

`Test-ProGo.ps1` also runs `tests/SocksRecoveryTests.cs` on .NET Framework.
The harness exercises the actual process manager using disposable loopback-only
child processes and a test clock, including crashes, backoff, manual stop,
opt-out, port conflicts, and profile fallback. It does not use real SSH servers.

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

## Home VPN validation

`Test-HomeVpnRelay.ps1` creates temporary certificates and keys under `build`,
checks token rejection, profile identity/routing, DPAPI and wizard navigation,
and then runs the socket relay integration suite. It requires OpenSSH and OpenSSL
(Git for Windows supplies OpenSSL in CI). Generated credentials never enter the package.

`tests/home_vpn_provision_smoke.py` is restricted to a disposable Ubuntu GitHub
Actions runner. It installs real strongSwan/OpenSSH, issues independent invites,
checks the allowed relay port and denied arbitrary ports, and verifies revocation.
Never run this provisioning test on an existing personal or production server.

## Desktop 0.2 validation

`Test-ProGo.ps1` also runs `DesktopTests.cs` for preference migration, all combinations
of CLI and Windows automatic actions, manual override, scoped Codex launch, HTTP/CONNECT
round trips through a loopback SOCKS fixture, native form screenshots and 150% scaling.
`UpdatePackageTests.ps1` loads only updater function definitions in a disposable test
scope and rejects corrupt digests, missing metadata, unexpected hosts and archive
traversal. Runtime code never evaluates downloaded scripts. Windows screenshots
are CI artifacts. They contain only synthetic connection settings and empty vaults.

The executable is unsigned. Do not claim antivirus clearance based on CI success;
record a vendor result separately. Do not introduce exclusions, obfuscation or
antivirus-disabling code. A future Authenticode rollout needs a real publisher
certificate and protected signing credentials; never commit such credentials.

### Audit stage F02

Desktop regressions also exercise automatic setup through a real occupied
loopback proxy port and a simulated configuration-write failure, then clear both
faults and verify successful application without restarting ProGo. The injected
clock checks delayed/bounded retries, permanent errors, manual cancellation,
explicit re-arming and reentrancy without sleeping or using a real VPS. Native
screenshots include the paused automatic-setup status and corrective guidance.
The other audit findings remain tracked separately in `AUDIT_PROGRESS.md`.

### Audit stage F01

Desktop regressions load all four combinations of the legacy terminal/Codex
switches from actual temporary settings files, verify OR migration and a single
automatic writer, save the unified off and reload without resurrecting old flags.
They check canonical-field precedence, clone compatibility and unrelated settings.
In isolated Windows CI, the actual dashboard-compatible command and legacy aliases
apply/restore the same user environment and cancel pending automation. A local
`codex.cmd` fixture runs as ordinary `codex` with the environment of a new shell,
without the scoped launcher, API credentials or external requests. Native screenshots
and button checks cover the single settings card and retained **Запустить CLI**.

### Audit stage F03a

`InstanceTests.cs` runs only in isolated Windows CI. It launches the compiled
ProGo executable via normal/show/Start/Show entry points and parallel repeats.
It verifies queued startup activation, same-owner window reuse, shared-file
immutability on secondary exit, preserved HTTP proxy ownership, minimized-window
restore, clean mutex release and replacement after a deliberately killed owner.
The fixture uses a disposable loopback listener and restores the original test
settings/environment in finally. It does not use SSH servers or account secrets.
### Audit stage F03b

`MaintenanceTests.ps1` runs only in disposable Windows CI. The three installed
update/restore entry points are launched as real competing PowerShell processes.
The local transaction driver executes their actual AST body with fixture network
metadata/package paths and suppressed modal dialogs, covering real compiled
staging/main self-checks, copying, restore, rollback and ownership release.
No external release is downloaded or installed on a user's computer. The runtime
class is shipped as installed source for PowerShell Add-Type; it is identical
to the class compiled into ProGo. Handoff and permits use same-user/SYSTEM ACLs.

### Audit stage F11/F12/F13

`DesktopUiWorkflowTests.cs` extends the isolated Windows desktop harness with
real command routing, modal navigation, symmetric dashboard mode controls,
independent desktop/phone/full stop and one-time tray guidance. A local SOCKS
relay fixture and injected route probes/clocks avoid real VPS or account access.
Screenshots include targeted settings and independent diagnostic timestamps.

### Audit stage F04

`DesktopHealthTests.cs` exercises actual SOCKS greetings and curl egress against
disposable loopback fixtures: fragmented/foreign/silent replies, HTTP 200/401,
NO_PROXY=* and cancellation. Injected probes/clocks verify independent cache
timestamps, expiry, discarded stale results and UI responsiveness. Native
window/tray checks distinguish applied preferences, partial CLI configuration,
SOCKS readiness and verified internet. The earlier ordinary Start CLI and
instance-ownership regressions still run. No health evidence is persisted.
These deadlines do not resolve the existing operations covered separately by F14.

### Audit stage F05a

The recovery child fixture also exercises asynchronous delayed startup, shared
requests, cancellation/retry, timeout, rejected keys and foreign listeners.
`DesktopStartupTests.cs` clicks ordinary Start CLI against that local child,
checks UI heartbeat and both modes on one connection, and cancels pending
requests before any late configuration write. Existing UI/manual aliases now
use a loopback SOCKS greeting fixture and await completion. The visible-login
launch configuration is validated without a real SSH server or credentials.
Structured profile fields remain F05b; broader deadlines/cancellation remain F14.

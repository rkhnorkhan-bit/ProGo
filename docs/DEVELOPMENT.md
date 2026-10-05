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

`tests/DesktopBridgeConsumerTests.cs` runs only on the disposable Windows CI
runner. It exercises real loopback listener release/rebinding, Windows/CLI settings,
scoped PowerShell process handles and the normal UI timer. Fixture cleanup may kill
only the child it explicitly created; production consumer tracking only disposes
handles. Native dashboard screenshots distinguish CLI off/Windows retained from a
stopped shared service. No real SSH endpoint or Codex installation is used.

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
### Audit stage F05b

`DesktopSshEditorTests.cs` checks legacy settings round trips, structured profile
cloning/persistence, distinct ports on one host and unchanged alias selection.
Native editor actions create/edit both modes, capture normal/scaled screenshots,
and verify visible fields. Actual Windows OpenSSH `-G` resolves an explicit
server/login/port/private-key path without making a network connection. A local
PowerShell spy evaluates the visible-login command with spaces, apostrophes and
dollar signs in the key path; no credentials or real SSH login are used.
The recovery child captures actual native argv for initial start and recovery,
and cancels stale startup after editing fields without changing the profile ID.
Broader deadlines/cancellation remain F14.

### Audit stage F06a

`DesktopWindowsRestoreTests.cs` runs only on disposable Windows CI. It reads and
restores the original fixture registry/environment in finally, using synthetic
routes and no live server. It exercises actual HKCU proxy fields, raw registry
types/absence, external endpoint/auxiliary edits, legacy backups, repeated apply,
per-field denial and a locked recovery journal. Retry tests deliberately give a
settled external field a ProGo-looking value; cleanup must not reclaim it.
Native stop/Quit actions show real cleanup-warning dialogs and retain the HTTP
bridge after a denied registry write, then clean up successfully on retry.
Uninstall/IPC and maintenance shutdown refusal are covered by F06b below.

Native CI shows that WinINet refresh normalizes REG_EXPAND_SZ proxy values to
REG_SZ and can remove AutoDetect. Apply/restore retain live pre-notification
values and correct only an unchanged-data kind conversion or a removed Boolean
AutoDetect value, after another live comparison. A correction failure during
cleanup is a pending, reported field; it never restores a stale external value.

### Audit stage F06b

`ShutdownTests.cs` runs only on disposable Windows CI, with a synthetic SOCKS
listener, temporary installed executable/helper scripts and fixture shortcuts.
It restores the original registry/settings/environment in finally. The real
uninstall entry point refuses a locked cleanup journal without killing ProGo or
removing shortcuts, then succeeds on retry. It verifies original values, external
changes (including the former default CLI port), no dangling owned listener,
retained user data, pending journals, competing leases and absent IPC handlers.
Native UI tests refuse update/restore handoff on cleanup failure; instance tests
cover cleanup acknowledgement/refusal alongside existing activation tests.
Maintenance transactions additionally reject a stopped owner's unresolved journal.
Uninstall acquires the same lease as update/restore, blocking normal starts while
removal is in progress. No fixture uses a live VPS or downloaded executable.

### Audit stage F07

`BackupRetentionTests.cs` exercises temporary mixed backup directories without
user settings, and `BackupRetentionTests.ps1` runs the updater's real backup
function in a temporary installation. Manual contents and unknown/legacy folders
survive a pool exceeding twenty entries. Tests cover shared ten-slot policy,
protected latest kinds, active backup pinning, manifest revalidation and an
unapproved new candidate. The installed C# policy source must hash-match the
app source. `DesktopBackupRetentionTests.cs` renders the exact cleanup list and
checks cancellation without deleting files.

During F07 validation two intermediate Windows runs failed the existing F06
shutdown assertion for external `AutoDetect` preservation (37197398077 and
37197771162). The fixture previously treated visible proxy registry fields as
startup completion, although Apply still had to finish its WinINet notification
and value correction. The fixture now waits for the completed-apply log from
the current process before editing/snapshotting the external route, runs that
scenario three times, and prints expected/actual typed values on any failure.
Assertions remain exact; no Windows proxy runtime logic changes belong to F07.

### Audit stage F08a

`BackupIntegrityTests.cs` validates the shared local SHA-256 inventory against
changed/missing/extra payloads, incomplete but freshly indexed copies, malformed
metadata, traversal, duplicate paths and exclusive file locks. The native
PowerShell junction fixture rejects traversal and preserves its target. Desktop
checks run the actual application backup writer, verify its manifest, refuse a
damaged opaque vault and retain installed data. The real updater backup function
also validates through this shared class; maintenance integration refuses a
corrupt copy before log writes/handoff and retains existing installed files.
Build/install/repair ship the same local source for PowerShell Add-Type.

The index detects damage, not authenticity. Existing copies without the index
are preserved but refused by the new automatic restore; create a new complete
copy after installing these helpers. An old installed helper does not gain these
checks until upgraded. F08b still owns restore staging, scope consent and rollback.

### Audit stage F08b1

`BackupIntegrityTests.cs` now tests independent prepared copies that retain the
recorded SHA-256 evidence, cancellation/disposal, explicit scopes and consent.
`DesktopBackupRetentionTests.cs` exercises the native restore chooser, safe
Program default, consent reset, background preparation with a UI heartbeat,
cancellation, launcher arguments and normal/enlarged screenshots. It uses a
synthetic opaque vault and never executes the copied fixture program.
`MaintenanceTests.ps1` executes the actual installed helper with independent
scope/consent checking, verifying exact preservation of current user data or
program/script bytes and independence from post-preparation source mutation.
Fixtures remain restricted to disposable Windows CI. F08b2 owns rollback and
installed self-check; no stage here may be described as a completed transaction.

### Audit stage F08b2

Maintenance integration runs the actual restore body with real Windows exclusive
and readable-but-nonreplaceable locks. Faults are injected at staging I/O, root
move, installed self-check and restart boundaries; commit/rollback remain the
production functions. Exact program, settings, opaque-vault, script directory
and absent-root states must match the original after failure. A rollback-denied
fixture must retain recovery roots and log their paths. The fixtures also verify
protective-snapshot integrity/retention and successful restoration after faults.
No tests decode vaults, use a real VPS, or replace user secrets. Forced termination
and power-loss recovery are outside the caught-error rollback claim.

### Audit stage F14a

`DesktopSshDiagnosticTests.cs` uses the harness itself as a local `ssh -G` and
pipe-holding descendant fixture. It checks process-tree cleanup on cancellation,
timeout and normal root exit, bounded parallel output capture and unrelated
process preservation. Native diagnostic windows exercise UI heartbeat, a frozen
profile snapshot, cancellation, retry and close during work. Screenshots cover
pending, cancelled and successful states. These tests run only in Windows CI.

`DiagnosticProcess` is exclusively for short-lived owned diagnostics, never user
terminals or persistent tunnels. It creates the process suspended, restricts
inherited handles, assigns a kill-on-close Windows job, then resumes execution.
See Microsoft's [job objects](https://learn.microsoft.com/en-us/windows/win32/procthread/job-objects),
[CreateProcessW](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-createprocessw)
and [creation flags](https://learn.microsoft.com/en-us/windows/win32/procthread/process-creation-flags)
for the ownership and startup guarantees. The separate cleanup confirmation
allowance is at most 2 seconds; cleanup failure must remain visible to the caller.

The diagnostic uses separate background threads for synchronous anonymous-pipe
readers on .NET Framework. This avoids its BeginRead/EndRead fallback (including
state changes at pipe EOF) and keeps both streams draining without consuming
blocked thread-pool workers. See the [Microsoft reference source](https://github.com/microsoft/referencesource/blob/main/System.Core/System/IO/Pipes/PipeStream.cs).
The oversized-output fixture repeats five times to exercise the completion edge.

### Audit stage F14b

`DesktopRouteDiagnosticTests.cs` runs real curl through a loopback SOCKS/HTTP
fixture, including NO_PROXY=* and remote DNS; it never contacts the test hostname.
Latency fixtures drip SOCKS fragments inside individual read timeouts and verify
the aggregate deadline plus socket closure. Route/speed wrappers use disposable
process descendants for timeout/cancel checks. Native windows verify heartbeat,
accessible cancellation buttons, duplicate suppression, settings snapshots,
repeat after cancellation, unchanged cancellation timestamps and close during
three active measurements. The existing CLI startup/alias checks remain required.
Route/speed use `DiagnosticProcess`; shared tunnel lifetime stays application-owned.

### Audit stage F15a

`DesktopSettingsActionTests.cs` runs only in disposable Windows CI. A loopback
SOCKS fixture and the actual application command handler verify that manual CLI
On uses saved settings while form fields/preferences remain uncommitted. Modal
Cancel retains that manual effect; explicit Off restores the captured environment.
Port-selection tests verify cancellation, mode/value preservation and rejected
Save retries without applying a real port migration. Native screenshots separate
preferences from immediate controls. The form refresh timer only reads state and
must stop/dispose on close; structured validation/error routing remains F15b.

### Audit stage F15b

`DesktopSettingsValidationTests.cs` checks the shared field/section validator and
runs native Windows fixtures for invalid form fields, an actual occupied port,
File.Replace denial and a locked integration journal. Refused application must
preserve the exact settings bytes, listening endpoint and existing Windows/CLI
state. A corrected condition succeeds from the same form. Native screenshots
show focused address/URL errors and general write/application errors.
Only explicit reconfiguration is validated; startup/load compatibility and the
existing transaction/rollback regressions remain required.

### Audit stage F16a

`DesktopDashboardLayoutTests.cs` checks native WinForms dashboard geometry and
command preservation at wide/minimum client sizes and a 1366 by 768-compatible
window, with long synthetic text. Constrained form.Scale 125/150/200% tests keep
the physical client area fixed, allow vertical scrolling and check that all
actions are reachable without horizontal scrolling. Screenshots include the
scrolled status area. The fixture allows larger native tracking bounds with
MaximumSize and uses SetWindowPos on its own HWND to bypass
Form.SetBoundsCore's MaxWindowTrackSize clamp on smaller CI virtual desktops.
GetClientRect must equal the requested size before capturing. Display settings
are unchanged. This verifies 1366-compatible geometry,
not a real 1366 display session. Do not describe this as native DPI coverage: display DPI,
work-area constraints and settings automation layout remain the next F16 stage.

### Audit stage F16b

`DesktopSettingsLayoutTests.cs` checks the automation settings viewport at normal
and minimum sizes, 1366-compatible geometry, long synthetic text and constrained
form.Scale 125/150/200%. It uses the F16a native HWND sizing fixture and requires
GetClientRect to match the requested client area. It checks non-overlapping rows,
wrapped captions/hints, vertical reachability and exact routing of all six manual
commands, pending-edit/settings-byte preservation, visible tab headers and
Save/error/retry footer controls. Widening must reflow the existing cards without
duplicating controls. Existing settings action, targeted Windows-entry and
structured validation/error tests stay required. No display settings, external
server or real credentials are used. This is not native DPI coverage; remaining
settings page layout and actual DPI/work-area validation remain separate F16 work.

### Audit stage F16c

`DesktopSettingsPagesLayoutTests.cs` covers the four non-automation settings pages
on isolated Windows CI. The native HWND sizing fixture verifies the actual client
area before checking bounds, row separation, wrapping and scroll reachability.
The matrix includes normal/minimum sizes, a 1366-compatible window, long synthetic
text and constrained form.Scale 125/150/200%. It also checks pending field values
across tab changes, port-pick cancellation, read-only effective-address copying
and refused Save preservation. Clipboard content is synthetic and cleared after
the copy assertion. Existing functional and validation fixtures remain required.
These screenshots and scaling checks do not establish actual DPI/display support.

## Contrast theme validation (F17a)

`DesktopContrastThemeTests.cs` runs only on the isolated Windows CI process.
A private test seam temporarily replaces the Windows high-contrast query without
changing OS settings; cleanup restores it. Fixtures dispatch the real preference
handler from a worker thread, inspect native controls and custom painting, switch
open windows back to the dark palette and verify pending edits, textual errors,
ordinary CLI routing and subscription disposal. Screenshots cover the dashboard,
all settings pages and a grid/progress/menu fixture. This is palette and lifecycle
coverage, not a real Windows contrast-theme or Narrator acceptance run.

## Settings keyboard validation (F17b)

`DesktopSettingsAccessibilityTests.cs` uses native dialog-key handling in the
isolated Windows CI process to traverse settings and SSH fields forward and
backward. It verifies accessible object names/descriptions, read-only field
reachability, skipping disabled controls, both SSH modes, contextual manual CLI
routing, port-pick state names, refused-Save focus and uncommitted field retention.
It does not alter OS accessibility settings or substitute for a Narrator run.

## Backup keyboard validation (F17c)

`DesktopBackupAccessibilityTests.cs` runs only on isolated native Windows CI.
Dialog-key fixtures cover restore scope arrows, Tab/Shift+Tab, unavailable scopes,
separate consent/reset, named read-only previews, cancellation and Enter without
implicit approval. Cleanup cases cover populated/empty lists and disabled removal.
Screenshots and accessible-object assertions do not establish Narrator acceptance.

## Phone verification states (F18)

`HomeVpnWizardTests.cs` exercises issuance without installation, expired/revoked
QR windows, explicit confirmation and guarded advancement, repeat issuance/reset,
user-reported internet choices and packet/no-reply status distinctions. The wizard
never treats relay counters as authentication or internet proof. Confirmation is
session-only and manual phone results are labelled as such. Tests use local QR
fixtures and a revoke stub; they do not perform real phone/provider acceptance.

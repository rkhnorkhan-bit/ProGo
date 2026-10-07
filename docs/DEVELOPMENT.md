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

## Local journal limits

`BoundedLog.cs` is compiled into the app and shipped as reviewed local source for
PowerShell helpers. `Log-ProGo.ps1` only defines a writer until first use, so it
cannot mutate files before maintenance ownership checks. Build/install/repair
ship both files; updater transactions already copy the entire scripts directory.

`progo.log`, `update.log` and `progo-restore.log` each retain at most 1 MiB active
plus two 1 MiB archives. Record text is capped at 4096 characters plus marker and
newline. Rotation is size-based, not an age-retention promise. A busy mutex or
file skips a record; disk/ACL errors cannot replace the operation's real result.
No retry loop, unbounded fallback, background thread or new setting is added.
Old oversized active files converge on successful rotation; old legacy logs and
backup copies are deliberately untouched. Raw logs may still contain personal
context; use the reviewed diagnostic export for sharing.

`BoundedLogTests.cs` exercises the production .NET implementation in disposable
directories. `BoundedLogTests.ps1` uses shipped writers and a disposable bootstrap
process, including locked-log error handling. Existing maintenance tests verify
that refused operations still make no filesystem changes.

## Diagnostic export contract

`DiagnosticReport` emits only fixed event codes, validated UTC timestamps/version
and counters. Do not pass dynamic parameters, raw exceptions or unmatched text
through to the output. New event prefixes require an explicit fixed code mapping;
unknown lines stay excluded. This deliberately trades detail for a bounded,
shareable summary. Do not describe it as a complete copy of the source journals.

The fixed source list covers application/update/restore current logs and two
archives. Legacy update.log fallback remains read-only. Each source reads up to
64 KiB, discards a partial initial line and retains at most 60 recognized events.
No settings, vault, keys, registry, environment, profile or network lookup is used.

The shared `DiagnosticPreview` displays an immutable snapshot. Explicit copy/save
must use that same snapshot; never reread raw logs at export time or fall back to
raw copying after an error. Save uses the native chooser with overwrite consent;
closing/cancelling does not export. Errors shown to the user omit exception text.
App and updater share the same form/theme and report source, packaged locally.
`DiagnosticReportTests`, `DesktopDiagnosticTests` and `DiagnosticPreviewTests.ps1`
cover privacy, bounded reads, UI action boundaries and the actual installed updater
entry point. Physical DPI/screen-reader acceptance remains separately scoped.

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

## Secret clipboard validation (F21)

`HomeVpnWizardTests.cs` exercises the actual Windows clipboard/timer with synthetic
values, later identical/different copies, repeat ownership, normal disposal,
shared invitation button binding and native QR copy/close. Only the current
owned clipboard entry is cleared; history/cloud copies and forced termination
are outside the cleanup guarantee. No fixture uses a real invitation or VPS.

## Task help and documentation version (F24)

`DesktopTests.cs` opens all six native help topics at normal/minimum sizes, checks
named scrollable text, log callbacks, compiled metadata and retained phone/scoped
Codex routing alongside the ordinary Start CLI regression. Screenshots do not
claim Narrator or physical DPI acceptance. `test_public_docs.py` checks VERSION
changes and missing/stale headings. Build checks both README files before clearing
build output, verifies assembly metadata and packages both instructions. Public
content validation also verifies package documentation against its VERSION file.

## Friend invitation presentation (F20a)

`HomeVpnWizardTests.cs` exercises native friend search/selection and token dialogs
with synthetic identities. It checks duplicate names, hidden/revoked selection,
UTC/unknown dates, empty results and shared clipboard lifetime. It does not issue
real invitations. `test_invitation_metadata.py` verifies the server list whitelist
and legacy compatibility without provisioning; the existing isolated Ubuntu smoke
suite still validates authorization and revocation. Reissue failure handling is covered separately by F20b below.

## Friend invitation reissue (F20b)

`HomeInvitationReissueTests.cs` exercises the production two-step coordinator with
acknowledgement gates, synchronous/asynchronous transport failures, malformed
responses, retained-name/ID selection and no blind retries. Native Windows tests
click the actual dialog actions, cancel confirmation, hold both operation stages,
refuse close while pending, reconcile failed operations, and capture success and
uncertain-outcome screenshots. No real credentials or VPS are used in UI tests.
The isolated Ubuntu provisioning smoke additionally repeats an existing revoke,
issues a fresh restricted identity, rejects the old token and confirms the other
live tunnel survives. Server authorization/token/state contracts are unchanged.

A missing revoke acknowledgement stops before issue, even when persisted state
might already be revoked. Missing/invalid issue output is treated as unknown:
no automatic retry/rollback or name-based attribution of server records. Mutations
remain disabled until an explicit successful list refresh, which clears selection.
Reissue can select an already revoked record and repeats the idempotent revoke
before issuing. Operations across the two SSH calls are not atomic; simultaneous
owner sessions and forced termination still require manual list reconciliation.
VPS waiting/cancellation remains the separate F23 scope.

## Shortcut migration (F26a)

`ApplicationShortcuts.cs` is shared by the app and installed PowerShell helper.
The installer must capture `IsExistingInstallation` before copying the executable,
VERSION or settings. Reinstallation never recreates or rewrites a Startup link;
its absence and Windows' separate disabled state are user choices. The first
installation keeps its previous default, with independent opt-out switches.
This is unrelated to `AutoStartSocks` and must not change that setting.

The installed app migrates only a recognized legacy menu entry. Portable builds,
self-check and a second instance must not run the migration. Check exact target
and arguments; preserve customized/unreadable shortcuts. Ensure the replacement
exists before removing the old link, and make a failed migration nonfatal.
Never recreate an intentionally removed main menu entry unless the user runs the
installer to restore it. `ApplicationShortcutsTests.ps1` uses the shipped helper,
real Shell links and an independent Shell automation reader in disposable folders; it does
not edit the user's real Startup/Programs folders or registry.

## Windows startup setting (F26b)

The settings checkbox stages a change to the current user's installed Startup
shortcut. It does not duplicate startup state in AppSettings and must not change
AutoStartSocks. Use an immutable owned snapshot; validate fields before changing
registration, roll it back if the ordinary settings apply fails, and never revert
a later external edit. An incomplete rollback is a visible error, not success.
Read failures/custom shortcuts disable only startup editing; unchanged or
unavailable startup must not block saving unrelated settings. Portable copies do
not register their executable or edit another installation.

Registration is not proof of effective Windows permission. Explain the distinction
and open `ms-settings:startupapps` on explicit request; do not write undocumented
StartupApproved registry values or bypass Windows/user restrictions. Keyboard and
synthetic layout checks include the new field, but actual sign-in and display/DPI
validation remain distinct from CI.

## Phone firewall cleanup (F26c)

`Firewall-ProGo.ps1` contains definitions only and is shipped with the installer,
repair and removal entry points. Removal selects local PersistentStore rules by
exact name, executable, direction/action, UDP and expected phone port. Do not
replace this with a name prefix, port-wide deletion, firewall reset or silent
query-error handling. Re-read candidates before deletion and verify the result.
NetSecurity does not provide an atomic compare-and-delete guarantee against an
administrator concurrently changing policy; this is not a policy locking system.

Uninstall holds its maintenance lease and first obtains the existing application
cleanup acknowledgement. It then runs firewall cleanup before file removal. Only
the firewall child is elevated; do not restart the entire per-user uninstaller as
a different administrator. An absent rule set requires no UAC. Failed or cancelled
cleanup retains the installation for retry, but does not automatically restart the
already stopped app or recreate rules removed earlier in the attempt.

`HomeFirewallTests.ps1` and `ShutdownTests.cs` may run only on disposable Windows
CI. Real rule fixtures are disabled, refuse existing names, and clean up in finally.
They never enable firewall access or contact a VPS. UAC denial and child-exit paths
use function/cmdlet boundary injection, not a real desktop UAC interaction. Actual
interactive elevation and Windows sign-in remain separate manual acceptance.

## Command dispatch boundary (F28a)

`AppCommands` is the single explicit ID-to-operation mapping. It has no WinForms,
settings, registry or network dependency. Definitions are immutable and canonical
IDs are case-sensitive. The legacy terminal/Codex on/off IDs must resolve to the
same objects as ordinary CLI on/off; do not register independent operations for
aliases. Do not use Enum.Parse on external/string IDs: numeric or unknown input
must never default to a valid command. Unknown IDs are not included in logs.

The application context uses typed commands in both dispatch and the pending
route dictionary. Pending work still finishes on the persistent UI dispatcher;
request-generation and stop/cancellation rules are unchanged. Presentation event
signatures remain string-based until the next bounded F28 stage; do not claim all
surfaces already use a shared name/availability catalogue. Backup/log/exit menu
callbacks remain separately scoped for that migration.

Native regression tests exercise ignored commands during pending and active CLI
alongside existing exact on/off, alias deduplication, shared route and cancellation
checks. Removed private helpers and the old port dialog had no repository callers
or reflection references; the active settings port tests remain required.

The shutdown fixture's completion barrier follows bounded-log rotation using a
unique pre-launch marker and shared reads. A previous startup's completion cannot
satisfy it; deterministic tests force real writer rotation. Probe the HTTP listener
only after that completion to avoid flooding the journal during the operation.
Keep the original readiness predicates/deadline; this is fixture synchronization,
not a change to application startup or the best-effort logging guarantee.

## Typed presentation callbacks (F28b)

MainWindow and SshProfilesSettingsForm callbacks carry AppCommand, never ad-hoc
string IDs. ExecuteCommand is the shared application entry; Execute(string)
remains the strict compatibility adapter. Keep these method names distinct for
legacy reflection fixtures. Typed UI commands must not bypass pending-route or
shutdown guards. Tray item Tag carries its command identity, not display text.

Full/compact/manual captions belong to the command definition. Compact/manual
variants are explicit context wording, not separate operations or settings.
Existing accessibility/layout and exact legacy ID tests remain useful across this
migration. Native integration additionally starts CLI from the tray and stops it
from settings while observing the dashboard. This does not yet centralize command
availability or migrate all direct backup/log/exit callbacks; those remain F28.

## Pending command availability (F28c)

AppCommandState copies the pending collection and is read only on the UI thread.
AppCommands.CanExecute is shared by route controls, tray, immediate settings and
the dispatcher; UI disabling alone is insufficient against stale events. Keep
Off/Stop usable during startup. Different route consumers may join one startup;
plain Connect cannot replace pending work, while explicit Reconnect intentionally
can. Prepared shutdown blocks catalogue commands until cancelled.

ShowSettings scopes CommandStateChanged subscription with finally. Refreshing
availability must neither save staged fields nor reset them. Existing timers and
menu Opening refresh background-startup state; no polling worker or network test
belongs here. Remaining direct backup/log/exit callbacks are not yet catalogue
commands and are separately scoped.

DesktopStartupTests uses a release-file-gated disposable SOCKS child to exercise
cross-surface availability and cancellation, retaining the original real startup
and stop regressions. It restores the fixture environment/settings in finally.

## Service command catalogue and effects (F28d)

Every actionable tray entry uses Item(collection, AppCommand); do not introduce
anonymous direct callbacks for a new global operation. Register its identity,
labels and effect, then handle it through ExecuteCommand. The string adapter is
compatibility-only, not a second implementation. Help routes journal/report
commands through the same dispatcher. Preserve operation-specific validation,
confirmation and cleanup refusal inside the existing service routines.

AppCommands and AppCommandState remain independent of WinForms. AppCommandUi
binds catalogue captions/effects to native buttons; tray effects are tooltips and
accessible descriptions. Context variants are presentation only. Local form
editing, public browser links and maintenance IPC are outside this UI catalogue.
Tests verify exact tray membership, descriptions and real restore/report dialog
cancellation; shutdown refusal now goes through the typed Exit command. Native
accessible metadata assertions do not substitute for physical Narrator acceptance.


## SSH diagnostic accessibility (F17d)

Keep report text read-only, named and keyboard-selectable. Diagnostic status must
expose its current text alongside a stable accessible name. Cancel becomes Close
after work settles; both its accessible name and explanation must follow that
transition and a subsequent retry. Explicit tab order follows visual content and
skips disabled Retry. No default Accept action may implicitly repeat diagnostics.
Escape cancels active work without closing results; after completion it closes.
The existing diagnostic process-tree tests exercise these native UI transitions.

The Windows startup checkbox is presented as “Запускать ProGo при входе в Windows”, with
an explicit user-sign-in explanation. It remains the F26b installed shortcut
preference, separate from automatic SSH connection and subject to Save/Cancel.


## Route diagnostic accessibility (F17e)

StatusForm's values expose their row names and current text through accessibility.
Route and speed buttons must announce their current action, including cancellation
scope, after every text transition. Their order follows the visible left-to-right
footer: route, speed, reconnect, close. Do not introduce a default Accept action
that reconnects or starts a download implicitly. Escape closes the modal window
and uses the existing FormClosed cancellation for all measurement workers.
DesktopRouteDiagnosticTests verifies native keyboard traversal and modal Escape
with isolated injected probes; it is not physical Narrator/DPI acceptance.


## Dashboard keyboard and button names (F17f)

UiTheme controls presentation, not AccessibleName. Leave native button caption
fallback intact unless a caller supplies a more precise purpose; never replace
that purpose during palette application. Dynamic explicit names are the caller's
responsibility. Dashboard status values have stable names and current descriptions.
Configure traversal in visual order and retain it after card reflow. Pending
availability comes from AppCommandState; keyboard work must not add another state
model, dispatch actions on focus, or change CLI/connection semantics.
DesktopDashboardLayoutTests covers wide/narrow and pending traversal with isolated
callbacks and injected health evidence. DesktopContrastThemeTests verifies the
explicit Windows action name survives live palette updates.


## Vault keyboard presentation (F17g)

Vault/Entry/PIN presentation must give fields their visible label as accessible
name and declare visual keyboard order. The read-only grid uses StandardTab and
native arrows; neither Tab nor focus selects an action. Do not copy secret/PIN
values into accessible names, descriptions or test focus logs. Show-secret changes
masking and its description only. Preserve ordinary Save/Cancel, PIN validation,
clipboard ownership and cryptographic behavior; their change controls still apply.
DesktopVaultAccessibilityTests uses synthetic non-persisting records only in native
isolated CI and checks unchanged vault bytes/existence. It is not actual Narrator,
DPI, security-policy or cryptography acceptance.


## VPN wizard keyboard presentation (F17h)

Rebuild visual Tab order after every wizard step rebuild. Keep progress informational
and out of Tab traversal; name the active step and current operation/issuance/phone
results. Field names must follow their visible labels; token metadata must not contain
its value. Describe each action next to its handler, including server/firewall changes
and the distinction between profile issuance, user confirmation and tested internet.
Preserve existing busy, installation and Back/Next semantics. Keyboard work must
not start SSH, change recovery or introduce a second verification state model.
DesktopWizardAccessibilityTests injects a pending task and synthetic presentation
only in isolated Windows CI, checking unchanged private access files. It does not
establish physical phone, Narrator, real DPI or real contrast acceptance.


### Audit stage F17i — profile-sharing keyboard contract

`HomeProfileShare.Configure` uses `CreateConfigureForm` for the same production
modal dialog and native fixture. Keep address validation, HTTPS ownership check,
SSH setup and save order unchanged. No Enter default may configure a server.
Close/Escape discards the address draft; its availability follows the existing
operation guard. Owner/friend mode skips invisible setup controls in Tab order.

Close stays in a separate footer outside the scrollable QR content, so a multiline
revoke error cannot leave the keyboard-focused Close partly outside the viewport.

`PhoneProfileQrForm` exposes the graphic purpose without the secret URL in names
or descriptions. Countdown, clipboard notice and revoke errors expose current
text. Closing only dismisses the window; it neither revokes a link nor confirms
installation. Keep copy/revoke/expiry gates and shared clipboard policy intact.
Native tests use nonfunctional QR images and injected revoke tasks; they never
contact a real share service or write the clipboard. Server tests verify real QR
encoding independently. These checks do not substitute for physical Narrator,
real DPI, scanning or live high-contrast acceptance.

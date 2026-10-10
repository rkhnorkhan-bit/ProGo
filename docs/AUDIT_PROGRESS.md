# Audit correction stages

Baseline: ProGo 0.2.2 (`4bb3365`). The approved audit compares 0.1.27,
0.1.29 and 0.2.2. Each stage is scoped to an audit finding and has its own
verification and review boundary.

## Stage 1 — F02: automatic setup lost after an error

Implementation:

- Complete an automatic action only after successful application.
- Retry temporary faults after 5, 15 and 45 seconds. Pause after four failures.
- Pause immediately for known access-denied or invalid-argument errors.
- Show the retry/paused status on the dashboard. Notify on the first failure and
  final pause; avoid a balloon on every timer tick.
- Cancel pending work on manual off, disabling the option or a successful manual
  application. An explicit automation off/on starts a new attempt budget.
- Preserve cancellation and avoid running the same action recursively.

Verification: `scripts/Test-ProGo.ps1`, including the desktop automation
regressions. They use a disposable loopback listener, a simulated write fault,
an injected clock and a test output file; no live VPS or account is required.

Acceptance: release the occupied port and clear the write fault; the task
completes without restarting ProGo. Cancel while waiting; no later tick applies
the setting. Persistent faults must pause with visible corrective guidance.

## Stage 2 — F01: one terminal and ordinary Codex mode

Implementation:

- One `AutoCliProxy` setting, card and automatic writer for the shared environment.
- Migrate `AutoApplyProxy OR AutoCodexProxy` when the new field is absent.
  Respect an explicit new false and stop saving legacy switches.
- Preserve **Запустить CLI** and route its old terminal/Codex aliases to the same
  on/off handlers. Keep scoped Codex launch/shortcut as additional actions.
- Remove the duplicated Codex on/off pair from the tray and settings.

Verification: all four legacy combinations, canonical precedence, clone/save/load,
one startup writer, actual shared manual handlers and cancellation. Ordinary
`codex` is exercised with a local CLI fixture and a new-shell user environment;
the test does not call OpenAI or require a real account. Native Windows screenshots
cover the unified card. F02 delayed retry and cancellation checks remain in place.

## Stage 3a — F03: one application instance per Windows user

Implementation:

- Acquire a per-user global lifetime mutex before settings, backups or services.
- A secondary launch only sends the existing owner a window-activation command,
  then exits. The named pipe grants access only to the same user and SYSTEM.
- Queue an activation received during startup until the UI attaches; show or
  restore the existing dashboard on its UI thread.
- Release the lifetime mutex during disposal and recover an abandoned mutex
  after a crashed owner. A secondary self-check exits without shared-state access.
- Start/Show scripts report a launch request; Start handles its no-argument case.

Verification: isolated Windows process tests launch the compiled executable
normally, with the show aliases, through Start/Show scripts and concurrently.
They check that secondaries do not change settings, snapshots, backups or logs,
that the existing proxy survives their exit, that the dashboard is reused and
restored from minimized state, and that a crashed owner can be replaced.

## Stage 3b — F03: update/restore ownership and handoff

- A shared per-user mutex serializes update and restore before network requests,
  log writes, backup creation or file changes. Losing contenders do not perform
  rollback, cleanup or an error-triggered relaunch of the winning operation.
- Program holds the startup gate until its lifetime mutex is acquired. During
  maintenance, normal starts refuse shared-state access. Only children carrying
  the current owner's short-lived event permit can self-check/restart.
- Both UI launchers wait for the helper to acknowledge actual ownership before
  closing ProGo. A failed/busy helper leaves the application running.
- Update/restore check that the old application has stopped before modifying
  files; rollback also refuses to overwrite an active application. All paths
  dispose ownership in finally, including failure and abandoned-helper recovery.
- The same installed C# class is used by Program and PowerShell helpers. No
  downloaded code, update trust rules or vault/backup formats are changed.

Verification: isolated Windows maintenance tests exercise all three competing
entry points, unchanged files/logs on refusal, blocked/authorized startup,
expired permits, handoff success/failure, abandoned ownership, real local
transaction staging/installed self-checks, restore and failed-commit rollback.
Only network metadata/download and modal test dialogs are replaced by fixtures.
This protocol applies to these builds; a restored older executable predating
F03 does not gain its lifetime/startup safeguards until upgraded.

## Stage 4 — F11/F12/F13: controls, stop scopes and route checks

- Connections opens the server tab; the Windows card opens and focuses its
  Windows controls. Highlight the active dialog and return navigation home on
  close. Windows and CLI cards expose symmetric manual on/off commands while
  preserving **Запустить CLI** when the shared mode is off.
- Name desktop, phone and full stop separately in the dashboard/tray/wizard.
  Desktop stop cancels desktop automation/recovery without stopping the phone;
  phone stop preserves desktop work; full stop cancels both channels. Explain
  once that closing the dashboard leaves ProGo and its connections in the tray.
- A route-check command starts one background check as diagnostics opens.
  Opening diagnostics alone shows an untested state. Route completion time and
  source are independent of status/ping refresh; speed shows its own source/time.
  Suppress overlapping route checks and ignore results after the window closes.

Verification: `DesktopUiWorkflowTests.cs` runs actual modal routes, active/home
navigation, Windows and CLI apply/restore through dashboard buttons, each stop
scope with a loopback relay, cancellation and persisted one-time tray guidance.
Injected route probes/clocks check first-run, repeat, independent timestamps,
concurrency and closing during an error. Native screenshots cover the changed
cards, targeted settings and diagnostics alongside the existing scaling checks.
F14 broader task cancellation/deadlines remains a separate finding.

## Stage 5 — F04: connection status requires evidence

- Separate applied Windows/CLI preferences, a successful SOCKS greeting and
  an HTTP response through that SOCKS route. Only the last state gets the green
  internet label. A partial four-variable CLI environment shows **Частично**
  and keeps the ordinary **Запустить CLI** repair action.
- Share immutable, in-memory health evidence between dashboard, diagnostics,
  automation readiness and tray. Publish changes without reopening settings.
  Refresh the SOCKS greeting every five seconds and egress every thirty seconds;
  discard local evidence after fifteen seconds and internet evidence after
  forty-five seconds. Changing connection settings invalidates old results.
- Perform the new probes in the background, with bounded connection/read/curl
  deadlines, no overlapping workers and cancellation on disposal. An inherited
  NO_PROXY cannot bypass the explicit SOCKS egress check. HTTP 401/403 confirms
  transport only; it does not prove account access or application authorization.
- Preserve the separate phone-channel status and its internet-test guidance.
  An open TCP port remains a port-ownership check, never a green health claim.

Verification: `DesktopHealthTests.cs` uses foreign/silent/fragmented SOCKS
listeners, actual curl against a loopback SOCKS/HTTP fixture, failed egress,
inherited bypass variables, cancellation and injected clocks/probes. It checks
cache timestamps, expiry, discarded stale results, UI heartbeat, each partial
CLI variable, and automatic shared dashboard/tray updates. Native screenshots
cover protocol-only, verified, unavailable and partial-configuration states.
The instance fixture now speaks SOCKS before automatic CLI setup can proceed.
F05 SSH startup readiness and F14 existing network tasks remain separate work.

## Stage 6a — F05: await desktop SSH readiness from one request

- Desktop connect/reconnect, ordinary **Запустить CLI**, Windows mode and scoped
  launch commands await a bounded asynchronous SOCKS-ready result. Completion
  uses the persistent application UI dispatcher, including after settings dialogs. Concurrent
  callers share one startup; repeated CLI aliases do not add another intent.
- Show pending connection/CLI controls while the UI remains responsive. Stop,
  manual off, changed connection settings and disposal discard pending actions,
  preventing a late environment/Windows write. A new attempt after cancellation
  waits for cleanup before creating its owned process.
- Hidden SSH starts always use BatchMode and strict host-key checking. The
  explicit **Первый вход** action opens a visible console with host-key prompts
  and configured forwardings disabled. Passwords are not stored. Explain that
  a password login does not by itself configure a key for background ProGo use.
- Startup timeout, foreign TCP listener and rejected SSH key have specific next
  steps. Preserve existing recovery/fallback behavior and earlier F04 evidence.

Verification: native Windows child-process fixtures delay/refuse SSH, exercise
shared startup, timeout, cancellation/retry and foreign listeners. Desktop tests
click the actual ordinary CLI button once, check a UI heartbeat, apply both modes
from one connection, stop pending requests and verify no delayed settings write.
Earlier fixtures now answer the SOCKS handshake and await command completion.
Native screenshots cover pending CLI and the first-login control/guidance.

## Stage 6b — F05: structured desktop SSH profiles

- New profiles default to **По адресу сервера**, with distinct server, SSH login,
  SSH port (22 by default) and optional private-key file controls. Explain the
  distinction from SOCKS, standard keys/agent and unattended key authorization.
  Passwords/key contents are not stored or uploaded; only the key path is saved.
- Preserve existing aliases/direct targets in **Из SSH config (для опытных)**;
  do not rewrite OpenSSH config. Stable selection IDs allow different ports/keys
  on one host. Clone/save/load retain every field; display labels show addresses.
- Background startup/recovery, visible first login and local `ssh.exe -G`
  diagnostics share validated arguments. Explicit key paths use correct Windows
  quoting; visible login uses literal PowerShell arguments. Keep BatchMode and
  strict host-key checks on hidden attempts. Diagnostics also reports SSH port.
- Editing the selected route's server/login/port/key restarts an active desktop
  tunnel, invalidates health evidence and cancels stale pending readiness.
  Renaming a connection does not restart it.

Verification: native Windows settings/UI tests, real OpenSSH configuration
resolution, an isolated PowerShell argv spy, native child argv capture on startup
and recovery, stale-startup cancellation, plus the earlier one-click CLI tests.
Screenshots cover new/legacy editor modes and enlarged controls. No real server
or account credentials are used. F14 broader blocking operations remain separate.

## Stage 7a — F06: owned Windows proxy restoration

- Every Windows-off/desktop-cleanup path uses per-field ownership checks instead
  of unconditional restoration. Preserve a later external endpoint together with
  its associated flags, bypass and PAC. Restore other fields only when their
  current values still match ProGo's recorded write; already-original fields
  require no mutation.
- Record original/applied values with absence and registry kinds, including
  unexpanded REG_EXPAND_SZ. Repeated applies/port changes preserve the original
  owned baseline; an explicit re-enable after an external route captures that
  newer baseline. Old AppliedServer backups migrate conservatively using their
  historical DWord/String contract; they cannot recover types never recorded.
- Return a result for each pending field. Keep failed fields in an atomic retry
  journal; settled external fields are not revisited on a later retry. Do not
  suppress registry deletion, journal-save or journal-delete errors.
- A failed manual desktop stop keeps its local bridge/tunnel alive, with a visible
  cleanup warning and retry instruction. Normal Quit refuses teardown on failure.
  Framework disposal reports failures and retains the journal; external forced
  termination cannot be prevented by this path.

Verification: isolated Windows registry fixtures cover changed endpoints,
flags/PAC/bypass, typed originals, repeated apply, missing/legacy backups, denied
writes, locked journals and retry ownership. Actual native stop/Quit dialogs show
failure and leave the local service running until a successful cleanup retry.
Earlier port transaction/rollback and desktop scope regressions remain required.
Registry comparison/write is best-effort: Windows provides no atomic per-value
compare-and-swap against concurrent writes by unrelated processes.

Stage 7a leaves uninstall/IPC and maintenance shutdown to stage 7b.

## Stage 7b — F06: graceful uninstall and maintenance cleanup

- Uninstall acquires the existing exclusive maintenance lease. Same-user/SYSTEM
  IPC requests owned Windows/CLI cleanup on the UI thread; only successful cleanup
  is acknowledged before exit is scheduled. Wait for the lifetime owner to exit
  before removing the executable or shortcuts. Never kill by process name.
- Refused/missing confirmation leaves the running app and installed files intact.
  A lost response reopens the cleaned app for retry rather than leaving controls
  locked. Keep the existing activation command and ACLs.
- Update/restore launch helpers only after successful cleanup. Failed cleanup
  keeps the local service/app running; failed helper handoff keeps the cleaned app
  open. Existing exclusive leases, validation, permits and rollback remain intact.
- Uninstall/update/restore refuse file changes when a stopped/crashed owner left a
  recovery journal. Open ProGo, retry Windows/CLI off, then repeat maintenance.
  Older live builds without the cleanup protocol must be closed manually; no
  force-stop fallback. CLI cleanup without a journal is an idempotent no-op.

Verification: native UI maintenance gate tests; same-user IPC refusal/success and
activation regressions; actual compiled ProGo with both proxy modes and the real
uninstall script. Lock a journal and verify retained service/executable/shortcuts,
then unlock/retry and check original Windows/user-environment values and removal.
Verify later external proxy values, competing maintenance, missing handlers and
stopped-owner journals. Update/restore transactions reject pending cleanup before
replacement and retain existing staging/self-check/rollback tests.

F06 is complete when these Windows checks and the Linux VPN regressions pass.
Version/release publishing is outside this stage.

## Stage 8 — F07: shared backup retention and cleanup preview

Scope: approved F07 only. App and updater use the same installed/compiled
`BackupRetention` class. Keep all manual and unrecognised folders, ten latest
known automatic copies, and the latest baseline and pre-update (ordered by the
existing timestamped names). Missing/unreadable/ambiguous manifests fail closed;
legacy display inference never grants automatic deletion eligibility. The copy
just created for an active operation is also pinned against clock reversal.

Manual cleanup previews every candidate path in a scrollable, read-only list
and requires an explicit click; cancellation changes nothing. Apply only the
confirmed subset after checking the current policy and unchanged manifests.
Changed/manual folders and junctions are skipped. No version/release changes.

Verification: more than twenty mixed copies, original manual payload retention,
unknown/legacy/ambiguous manifests, protected latest baseline/pre-update,
relabelled and unapproved candidates, native preview/cancel screenshot, and the
actual updater backup function plus existing update/rollback transactions.
Installed policy source must hash-match the class compiled into the app.
F07 is complete when the exact Windows and Linux CI run passes.

## Stage 9a — F08: backup integrity before restore shutdown

App and updater write the same SHA-256 inventory for the files actually copied.
Manifest `contains` lists actual payload roots; update outcome metadata may change
without invalidating immutable payload digests. Existing-file copy errors abort
updater backup creation instead of silently certifying partial data. Writers also
validate completeness before reporting success or applying retention. An old
baseline without digests cannot suppress creation of a verifiable new baseline.
Required helpers use the original program layout; newer ownership helpers are
verified when recorded but are not required in a complete older program copy.

The application validates the selected copy before confirmation and maintenance
cleanup. The installed restore helper revalidates before handoff and waiting for
exit. Missing/extra/changed files, absent required program/helpers, ambiguous
product/version metadata, unsafe paths and reparse points fail closed. No digest
is retroactively generated while checking a legacy copy. Legacy copies remain
available on disk, with clear refusal guidance; this is not a trust signature.

Verification: standalone corruption/completeness/metadata/path/lock tests, native
junction tests, actual application and updater backup writers, installed-source
hash parity and a real restore refusal leaving installed payload/logs unchanged.
Earlier Windows/CLI startup and maintenance ownership checks remain required.

Status after stage 9a: F08 was **not complete**; stage 9b still needed explicit program/data selection,
immutable staging, protective snapshot, installed self-check and rollback on
commit failure. Current restore still uses its earlier file-by-file commit;
preflight alone does not prevent mutation between validation and copying.
No vault format, encryption or decoy semantics change in stage 9a.

## Stage 9b1 — F08: explicit restore scope and prepared input

Restore now defaults to the program only. The native chooser offers program,
user data, or both, with the exact payload list. Data-bearing choices need a
separate unchecked consent box; switching scope clears that consent. A final
confirmation defaults to No. Absent user data and historical logs are preserved,
and a copy with no user payloads only offers program scope. The helper checks
scope/consent independently; command-line invocation defaults to Program.

The chooser prepares a separate copy asynchronously before maintenance cleanup.
Cancellation prevents handoff; a preparation error leaves the running app and
connections untouched. Preparation retains the original recorded digest index,
verifies the copied bytes, rejects links and never generates retroactive evidence.
The helper takes its own verified copy before acknowledging handoff, so disposing
the UI copy or later changing the source cannot affect its selected input. It
revalidates before commit and only copies the selected program/data roots.

Verification: standalone prepared-copy isolation/disposal/cancellation and scope
checks; native chooser defaults, consent reset, cancel, UI heartbeat and normal/
enlarged screenshots; real helper refusal without consent, preserved opaque
vault/settings in Program scope, preserved exe/version/scripts in Data scope,
and source mutation after pre-handoff preparation. Existing CLI and maintenance
regressions remain required. No vault encryption/format/decoy semantics change.

Status after stage 9b1: F08 remained **open**. Stage 9b2 still needed a protective current-state snapshot,
installed self-check and rollback after any commit failure. This stage does not
make the existing file-by-file commit atomic. No version bump or public release.

## Stage 9b2 — F08: protective snapshot, verified commit and rollback

The helper rejects locked selected payloads before the first replacement, saves
an independently verified full program/settings/opaque-vault snapshot in
`backups/backup-...-pre-restore-...`, and copies selected input to same-volume
staging. Recovery snapshots are preserved by the existing retention policy.
Commit moves existing roots aside instead of deleting scripts or overwriting
files in place. Exact file hashes, directory composition and missing roots are
verified after commit, then the installed executable runs `--self-check` under
the maintenance permit with a bounded wait. Selected bytes are checked again
before ordinary restart. No vault content is decoded or re-encrypted.

Failures reverse the recorded root moves and verify the original selected state,
including roots that were absent before the operation. Restart failure also
enters rollback. If another process prevents rollback, the helper reports an
incomplete recovery and preserves both previous roots and the protective backup;
it never reports success or deletes that recovery input. Cleanup failure is
reported without replacing the operation outcome. The ownership gate stays held
through commit, checks and rollback.

Validation: real exclusive/read-sharing Windows file locks; partial staging copy,
late prepared/staged input corruption, mid-commit move failure, installed-byte corruption, self-check refusal, restart
failure, original script restoration, originally absent vault, retained recovery
input after rollback refusal, subsequent successful recovery and shared
retention protection. These run the actual helper transaction in disposable CI.
Verified: full Windows/Linux CI passed, including 80 maintenance checks, 52
integrity checks, 374 desktop checks, 39 shutdown checks, ordinary Start CLI,
33 home-VPN checks and 18 relay integration tests. Windows short-path alias
expansion is covered by the real fixture; comparison uses relative child names
rather than slicing absolute paths. Additional late-input mutation checks require
revalidation of recorded evidence and selected staging immediately before commit.
F08 acceptance is complete for caught runtime
errors: invalid input and locked targets leave the payload unchanged; good copies
restore the declared scope; commit/check/restart failures recover original bytes.
The audit now has **11 of 30 findings closed** (F01–F08 and F11–F13).
This handles caught runtime failures; it does not claim filesystem-wide atomicity
or automatic recovery after a forced process termination/power loss. No version
bump or published release.

## Stage 10 — F10 shared application-proxy consumers

Scope: manual CLI off, Windows off and optional scoped consumers share one
loopback HTTP listener. No new transport or persisted setting is introduced.

- The listener is released after the last Windows/CLI consumer is disabled.
  Ordinary **Запустить CLI** and its aliases still use the shared environment.
- Windows, partial proxy endpoints, unfinished cleanup journals, windows explicitly
  opened through ProGo, and its owned Codex shortcut retain the endpoint. Window
  handles are pruned after exit; no PID lookup or user-process termination occurs.
- Dashboard service status names remaining consumers independently of CLI state.
  CLI off explains restarting old terminals and the existing explicit full-stop
  command. Full desktop stop closes the endpoint but leaves user windows open.

Verified: full Windows/Linux CI passed, including 413 desktop checks, 39 shutdown
checks, 88 maintenance checks, 52 backup-integrity checks, 33 home-VPN checks and
18 relay integration tests. Consumer fixtures rebind the real listening port,
exercise both release orders, ordinary CLI aliases, scoped PowerShell handles,
partial endpoints, optional shortcut removal, UI timer release and full stop.
Actual failed Windows/CLI cleanup records an explicit retained consumer; it cannot
be discarded by the timer even if the journal becomes unavailable. A successful
explicit retry settles it. Production tracking never kills user processes.

Native screenshots distinguish CLI off/Windows retained from the stopped service;
the fixture explicitly refreshes paint before capture. This stage does not claim
to change an already-running terminal's environment or track windows launched
outside ProGo. The audit now has **12 of 30 findings closed** (F01–F08 and
F10–F13); **18 remain**. No version bump or published release.

## Stage 11a — F14: cancellable SSH settings diagnostics

The profile chooser opens a background SSH settings check with cancel, retry and
close controls. A running check cannot be duplicated; it uses a profile snapshot
and does not claim server reachability. Closing the window requests cancellation
without waiting on the UI thread. Ordinary CLI startup remains unchanged.

A Windows job owns this short-lived diagnostic tree. Creation is suspended until
containment succeeds, with only the three standard pipe handles inherited.
Stdout/stderr drain concurrently with a 128K-character capture limit per stream. The
7-second execution deadline includes pipe completion; cancellation/timeout also
allows up to 2 seconds to confirm cleanup. Descendants cannot keep the pipe open
or survive normal root exit. No user terminal or persistent SSH tunnel uses this
runner. Cleanup failure is reported rather than presented as confirmed cancellation.

Validation: the Windows/Linux CI suite adds 23 desktop diagnostic checks to
existing OpenSSH, CLI, maintenance, shutdown, home-VPN and relay regressions.
Native fixtures cover stalled descendants, concurrent oversized output repeated
five times, timeout, cancellation, normal parent exit, unrelated-process
preservation, UI heartbeat, retry and closing during work. Pending/cancelled/
success screenshots are captured for native review. The .NET Framework
anonymous-pipe async fallback is avoided with two dedicated background readers.
No live VPS is used.

F14 remains **open**: route, latency and speed worker cancellation belongs to
stage 11b. Audit progress remains **12/30 closed; 18 remaining**. No release or
version bump is included.

## Stage 11b — F14: cancellable route, latency and speed measurements

The route and speed buttons remain enabled as cancellation commands while their
measurement is running. Duplicate requests are suppressed. Cancelled work does
not invent a completed route timestamp; a subsequent click starts a fresh run.
Closing/disposal requests cancellation for route, latency and speed immediately,
without waiting on the UI thread. Each worker owns a settings snapshot and
releases its cancellation source independently of UI completion. Late results
cannot touch a closed window.

Route (10 seconds) and speed (40 seconds) use the F14a short-lived process owner,
with concurrent bounded output and up to 2 additional seconds for owned-tree
cleanup. The SOCKS latency exchange has one 4-second deadline covering connect,
greeting, CONNECT and fragmented reads; cancellation closes its owned socket.
Explicit SOCKS arguments disable inherited NO_PROXY bypass, quote the endpoint,
and reject invalid/credential-bearing URLs. HTTP transport proof requires a
successful curl exit and a complete numeric status; oversized or malformed output
cannot claim success. Reconnection remains an explicit application operation;
closing the diagnostics does not stop an already-running shared SSH tunnel.

Validation covers native curl against a disposable loopback SOCKS/HTTP fixture,
fragmented/silent socket deadlines, real diagnostic process-tree timeout/cancel,
UI heartbeat, cancellation, repeat, settings snapshots, closing all three active
measurements and existing ordinary CLI/SSH startup regressions. Screenshots cover
pending, cancelled and repeated results. No live VPS or credentials are used.

Together with F14a and earlier asynchronous route/startup corrections, this stage
completes F14. After successful native CI/review preparation, progress is
**13/30 closed; 17 remaining**. No merge, release or version bump is included.

## Stage 12a — F15: saved preferences and immediate manual commands

The settings header explicitly distinguishes Save-only fields/preferences from
immediate manual commands and explains that Cancel does not undo those commands.
Each automation card separates its preference description from a marked immediate
control area. The cancel button is named **Отменить изменения**. Existing CLI,
Windows and reconnect/stop commands still route through the application owner.

The automation, connection and diagnostics pages display saved SSH/SOCKS/app-proxy
parameters and the saved test URL alongside uncommitted edits. A lightweight UI
timer reads current settings/application endpoint only; it performs no network or
writes and stops/disposes with the form. A manual command never commits edited
fields or toggles. Its asynchronous application can update the effective endpoint
without resetting those edits. These labels describe configuration, not proof of
server connectivity or internet access.

The one-time free-port button becomes **Отменить подбор** while selected. Cancelling
restores the exact entered port and the selected automatic/manual mode, without
binding a listener, migrating integrations or persisting anything. A rejected Save
keeps the request cancellable. Existing port transaction behavior is unchanged.

Native Windows fixtures exercise a real CLI command through a loopback SOCKS
server with different uncommitted settings, cancel the modal dialog and verify
that the applied manual effect remains until explicit Off. They also cover exact
field/preference preservation, live effective labels, in-place port-selection
cancellation in both modes, retries after a rejected Save and refresh-timer cleanup.
Screenshots cover immediate control and pending/cancelled port selection.

F15 remains **open**: structured field validation and error routing belong to
stage 12b. Progress remains **13/30 closed; 17 remaining**. No merge, release or
version bump is included.

## Stage 12b — F15: validation and structured Save errors

The form and explicit bridge reconfiguration validate SOCKS host/ports, diagnostic
HTTP(S) URL, clipboard duration and SSH settings before binding, migration or
persistence. Scheme/login/port text in the SOCKS host, malformed URLs, URL
credentials, whitespace/control characters and out-of-range fields are refused
with a named field/section. IPv4, IPv6 (including bracketed host literals), DNS,
HTTP and HTTPS remain supported. Validation does not test reachability or keys.
Legacy settings load normalization and ordinary bridge startup are unchanged;
explicitly saving invalid old fields requires correcting them first.

Save returns a structured error rather than an unclassified string. Port-binding
failures target app ports; integration failures target the application controls;
file-write failures are general Save errors and retain the current page. One
inline error area remains visible above Save/Cancel. Field failures select the
appropriate page and focus the field; all edits remain available for correction
and retry. Unexpected application callbacks are logged and shown as general
errors without claiming that a possibly applied operation was rolled back.
The existing bind-before-release transaction and rollback logic remain intact.

Native tests cover valid/invalid fields, refusal before an occupied-port bind,
exact preservation of listener/settings/Windows/CLI state, real occupied ports,
file replacement denial and retry, integration snapshot-read denial, field focus,
correct section selection and callback failures. Screenshots show address/URL,
file-write and integration errors. No live server or credentials are used.

Together with F15a, this completes F15 after successful native CI/review:
**14/30 closed; 16 remaining**. No merge, release or version bump is included.

## Stage 13a — F16: adaptive dashboard layout

The dashboard shell fits the client area instead of forcing a minimum internal
970-pixel canvas. Headings, connection details and recovery text wrap to their
container; stacked AutoSize rows replace fixed Y positions and section heights.
Status cards change from three columns to one when the content area is narrow,
and return to three columns when widened. Action/header rows wrap; navigation and
the main body permit vertical scrolling while the footer remains outside the
scrolling body. The window minimum is reduced to 760 by 560 pixels.

All command routes, ordinary **Запустить CLI**, state text, pending-action logic
and application-owned proxy/tunnel lifetime remain unchanged. No preferences or
network integrations are applied by layout.

Native Windows geometry fixtures cover normal client and minimum window sizes, a client area
that fits 1366 by 768, long synthetic status text and constrained form.Scale
125/150/200% stress cases. They check wrapping, row separation, footer visibility,
horizontal overflow, vertically reachable actions, navigation and resizing back
to three columns. Screenshots accompany the checks. These synthetic scaling
fixtures are explicitly not proof of native Windows DPI support.

F16 remains **open**: settings automation cards and a real DPI/display matrix are
separate work. Progress remains **14/30 closed; 16 remaining**. No merge, release
or version bump is included.

## Stage 13b — F16: adaptive automation settings

Automation cards use stacked AutoSize rows instead of fixed heights and Y
positions. Their width follows the scroll viewport, hints and preference captions
wrap, and manual action rows can wrap onto another line. Header/error rows and
the Save/Cancel footer size to content; the footer stays outside the scroll area.
Tab headers use multiple rows when needed to keep all five sections available.

This changes layout only. Saved preferences, immediate command routes, pending
edits, port-selection behavior and refresh-timer ownership remain unchanged.
The Windows-settings entry still scrolls its actual controls into view. Other
settings page contents are intentionally outside this bounded stage.

Native Windows fixtures cover normal/minimum sizes, 1366-compatible geometry,
long synthetic hints and constrained form.Scale 125/150/200% stress cases. They
check row separation, wrapping, no horizontal overflow, reachable manual actions,
exact routing of all six commands, retained pending edits, visible tab headers,
Save errors/retry controls and reflow on widening. Screenshots accompany the
checks. The existing saved/immediate command and validation fixtures remain
required. Synthetic scaling is not native Windows DPI evidence.

F16 remains **open**: the remaining settings pages and real DPI/display validation
are separate work. Progress remains **14/30 closed; 16 remaining**. No merge,
release or version bump is included.

## Stage 13c — F16: adaptive settings pages

Connection, Storage, Diagnostics and Application Port pages now use a vertically
scrollable, width-constrained body. Field captions sit above their controls;
AutoSize rows replace fixed row heights and the fixed label column. Hints wrap to
the available width, preference captions use measured wrapped height, and SSH and
port action groups can wrap onto additional lines. The footer stays outside each
page viewport. Existing settings values, commands and Save/error behavior remain
unchanged; clipboard, vault, SSH, port migration and updater contracts are intact.

Native Windows fixtures cover all four pages at normal/minimum sizes,
1366-compatible geometry and constrained form.Scale 125/150/200%, including long
synthetic hints/captions. Checks cover container bounds, row separation, complete
text, vertically reachable fields/actions, footer visibility, pending edits across
tab changes, port-pick cancellation, copying the effective address and refused
Save preservation. Screenshots capture page tops/bottoms and pending/error states.
Existing action, validation, SSH editor and automation layout checks stay required.

F16 remains **open** for actual Windows DPI/display validation. Progress remains
**14/30 closed; 16 remaining**. No merge, release or version bump is included.

## Stage 14a — F17: system contrast palette

Forms, surfaces and semantic labels select the Windows system palette when high
contrast is enabled. Fields/selectors, primary and secondary buttons, links,
grid headers/selection, tab drawing, wizard progress and empty-vault text use
matching system colors. The normal dark palette is retained for the return path;
status/error messages keep their text and latest semantic foreground.

Open forms and the tray menu observe system preference changes and queue refresh
on their UI thread. Window disposal releases the subscription and prevents queued
refresh from touching a closed control. Menu opening also refreshes the palette.
Theme changes do not commit pending settings or execute network/proxy commands.

Isolated Windows fixtures replace only the private contrast-state source in the
test process and exercise the same queued preference handler. They cover startup,
live transitions, semantic colors, custom drawing, nested menus, pending edits,
ordinary CLI routing and subscription disposal; screenshots accompany the checks.
They do not change Windows contrast settings or establish Narrator coverage.

F17 remains **open** for keyboard/field accessibility and real Windows contrast
and Narrator validation. F16 remains open for real DPI/display validation.
Progress remains **14/30 closed; 16 remaining**. No merge, release or version bump
is included.

## Stage 14b — F17: settings and SSH keyboard navigation

Settings and the structured SSH editor now assign Tab order from visual table
rows and action groups, including left-to-right traversal of reversed footers.
Native input internals remain untouched. Field captions supply explicit accessible
names, read-only paths remain focusable for copying, and same-caption manual
buttons describe their target and immediate effect. The port-pick button updates
its accessible action name when selection is pending or cancelled.

Isolated Windows fixtures exercise native dialog Tab/Shift+Tab traversal across
all five settings pages and both SSH modes, disabled port/SSH fields, accessible
objects, the ordinary CLI action and refused-Save focus/pending values. They do
not establish actual Narrator behavior. Existing persistence, validation, SSH,
proxy ownership and layout checks remain required.

F17 remains **open** for the remaining forms and real Windows contrast/Narrator
validation; F16 remains open for actual DPI/display validation. Progress remains
**14/30 closed; 16 remaining**. No merge, release or version bump is included.

## Stage 14c — F17: backup dialog keyboard navigation

Restore and cleanup previews now expose explicit accessible list names and
read-only descriptions. Restore status and consent explain their purpose.
The existing visual-row Tab-order helper is shared through UiTheme and applied
to both dialogs; settings and SSH use the same unchanged helper.

Windows fixtures exercise Tab/Shift+Tab, radio-group arrows, disabled scopes and
actions, consent reset, preview Enter behavior and cancellation without modifying
source data or deleting candidates. Safe initial cancel focus and the absence of
an implicit destructive Enter default remain unchanged. Existing real preparation,
retention, restore and maintenance checks stay required.

F17 remains **open** for other forms and actual Windows contrast/Narrator
validation. Progress remains **14/30 closed; 16 remaining**. No merge, release
or version bump is included.

## Stage 15 — F18: phone profile issuance and verification

Profile issuance, user-confirmed installation, incoming/returned relay packets
and user-reported phone internet results are separate states. QR creation and
file export only record issuance; QR closing, expiration and revocation never
confirm installation. A new issuance or route reset clears prior confirmations.
The profile step requires explicit installation confirmation before advancing.
The final page provides a short mobile-data/VPN/IP verification procedure and
labels its result as the user's check, not an automated phone test.

Final status distinguishes no incoming packets, incoming packets without a VPS
reply, replies without proof of VPN authorization/internet, and the user's report
of a connected VPN with no internet. Internet success requires confirmation and
an explicit report that the site opened with the VPS exit IP. Confirmation is
session-only; reopening the wizard does not restore a previous success claim.

Native Windows fixtures cover expired/revoked QR dialogs, guarded advancement,
repeat issuance, confirmation/reset controls and the final status matrix.
Existing profile export, QR rendering, relay and provisioning checks remain
required. F18 is closed by these state/UX criteria; this does not claim a real
phone internet test or close F19's provider/network acceptance requirements.

Progress: **15/30 closed; 15 remaining**. F16/F17 remain open for their stated
acceptance gaps. No merge, release or version bump is included.

## Stage 16 — F21: shared secret clipboard policy

Vault secrets, friend invitations and QR links use the same application-owned
clipboard service and configured timer. Native clipboard sequence ownership plus
text comparison preserves later copies, including identical text copied again.
Repeated secret copies restart the timer for the latest value. Closing a QR or
invitation window does not dispose the shared application service.

Normal application disposal attempts immediate owned-content cleanup. Busy
clipboard failures are reported without secret values; forced termination cannot
guarantee cleanup. UI explains that clipboard history and synchronized copies
remain, and QR expiry is independent of the local clipboard cleanup timer.
Ordinary local proxy address copying keeps its existing untimed behavior.

Windows fixtures exercise the actual timer, replacement/identical copies, repeat
copying, normal disposal, shared copy binding and native QR copy/close behavior.
F21 is closed after these checks pass. Progress: **16/30 closed; 14 remaining**.
No merge, release or version bump is included.

## Stage 17 — F24: task help, platform-neutral names and version consistency

The dashboard/tray/wizard use VPN for phone/Phone names, while iPhone-only file
export stays explicitly labelled. Codex and terminals share one name; the direct
Codex action describes its scoped proxy launch and the ordinary Start CLI remains.
Command identifiers/handlers are retained. Help starts with connection guidance
and provides six topics: connection, Codex/terminals, ports, phone, recovery and
antivirus. Local application/update logs have separate callbacks.

Both README files now match VERSION and current shared CLI environment, optional
shortcut, explicit SSH fields/key readiness, manual phone verification and QR
platform differences. Proxy/home instructions use the same names. The build
checks README headings before replacing local build output, generates assembly
metadata from VERSION, verifies compiled version and ships both instructions.
Public-content validation checks source/package headings against VERSION too.

Python regressions cover a version change, stale/missing headings and invalid
version source. Native Windows regressions cover help topics, wrapping/scrolling,
callbacks, compiled metadata and preserved phone/Codex command routing; normal
and minimum screenshots are inspected. F24 is closed after these checks pass.
Progress: **17/30 closed; 13 remaining**. No merge, release or version bump.

## Stage F20a — friend invitation visibility and selection

The invitation list now supports case-insensitive name/ID search and explicit
selection. Details show server creation time in UTC, status, full identity and
restricted VPN scope. Missing/invalid legacy timestamps remain unknown; a freshly
issued token's creation timestamp arrives when the list is reopened. The server
list exports only whitelisted public metadata, including its existing timestamp.
Filtering a selected row away clears the destructive-action selection; duplicate
names remain distinguishable. Confirmation includes ID/date. Search/name changes
are disabled during an existing create/revoke operation.

The issued-token dialog explicitly describes one-time display, personal delivery,
VPN-only rights, its distinction from QR and manual lost-token recovery. It uses
the shared clipboard cleanup service and a masked read-only field. New rows are
selected before the token dialog opens. Existing server authorization/revocation
contracts and token format remain unchanged.

Native Windows fixtures cover duplicate names, search/selection reset, revoked and
empty lists, UTC/legacy dates, token presentation/copy/cleanup. Python fixtures
verify metadata whitelisting and nonmutation. Isolated server CI still checks
invitation scope and independent revocation.

F20 remains **open** for the guided reissue workflow and its partial-failure
handling. Expiry, quotas and billing remain separately scoped server/product work.
Progress remains **17/30 closed; 13 remaining**. No merge, release or version bump.

## Stage F20b — lost-token reissue and explicit partial outcomes

The friend dialog offers a confirmed reissue for the selected full identity. It
snapshots the selected name/ID, completes the existing revoke command before a
single issue request, validates the replacement token/identity and presents it
through the shared one-time token dialog. No administrator credentials are shared.
Pending operations freeze editing/actions and prevent losing their result by
closing the dialog. Revoked records may be selected for a safe repeat of revoke;
a separate repeat-revoke action can complete it without creating unwanted access.

A lost/invalid revoke acknowledgement never proceeds to issue. A failed issue
leaves the old access revoked and explicitly warns that a new record may exist.
There is no automatic retry, rollback or attribution based on duplicate names.
An explicit successful list refresh is required before further mutations; it
clears selection and exposes the server records for review. The same reconciliation
gate also protects ordinary create/revoke errors. The operation is intentionally
not described as atomic, and multi-owner ambiguity is stated in the help.

Validation: production coordinator and real native-dialog actions exercise both
partial outcomes, deferred completions, cancellation before work, retry blocking,
selection identity, token verification and reconciliation. Isolated Ubuntu tests
repeat revoke and reissue, validate new restricted credentials, reject old access
and keep the other invitation connected. No real user's VPS is modified.

Together with F20a this completes the approved F20 scope. Expiry, quotas, billing
and last-use tracking are not introduced. Progress: **18/30 closed; 12 remaining**.
No merge, release or version bump. Native and server CI are required before review.

## Stage F25a — bounded application and maintenance journals

Application, update (including bootstrap failure) and restore writers share one
best-effort implementation. Each active journal is capped at 1 MiB with two
bounded recent archives (`.1`, `.2`); an individual record is limited to 4096
characters plus a truncation marker/newline. Rotation preserves the recent tail
of an oversized historical active file before truncating it. A named nonwaiting
mutex and exclusive writer handle serialize cooperating writers. Busy, read-only
or failed rotations skip the record; they never fall back to unlimited appending.
Filesystem I/O itself is synchronous; this is not a disk-stall timeout guarantee.

Updates write only `update.log`. An existing `progo-update.log` is retained
unchanged and remains available when the current file is absent. Recent-error
reading seeks to a bounded tail instead of loading a potentially huge legacy file.
Local log opening remains available. Historical legacy logs, backup copies and
unwritable oversized files are not purged automatically. An oversized active file
is bounded after its next successful rotation; a failure preserves its contents.

Native tests cover repeated rotation, Unicode/truncation, migration, concurrent
writers, locked/read-only targets and bounded reads. Installed PowerShell tests
exercise actual updater/restore writers and the real bootstrap's original-error
preservation with a locked log. Packaging verifies identical shared source;
existing maintenance refusal gates must still leave filesystem state unchanged.

F25 remains **open**: sanitized diagnostic export with preview is F25b. In
particular, existing Copy log is not yet a privacy-safe diagnostic export.
Progress remains **18/30 closed; 12 remaining**. No merge, release or version bump.

## Stage F25b — reviewed diagnostic export without raw personal context

Help/tray and the updater now open the same diagnostic preview. The former
updater Copy log action no longer copies a raw file. Users explicitly copy or
save the reviewed snapshot, cancel without side effects, or continue to open the
original local journals separately. Export failures show fixed actionable text,
never raw exception details. The dialog follows the existing theme/keyboard rules.

The report is an allowlisted projection of application/update/restore events,
including two rotated generations and legacy-update fallback. Only fixed event
codes, validated timestamps normalized to UTC, a validated version and counters
are emitted. Raw messages, dynamic parameters, exception text, addresses, paths,
identities and unknown lines are excluded instead of trusting regex redaction to
recognize every secret. Each source reads at most 64 KiB and contributes at most
60 recent events. The preview explicitly explains this loss of detail; missing,
locked and truncated sources have fixed statuses. Source files are never changed.
No automatic sending, upload, clipboard write or file creation occurs on opening.

Tests cover secret/address/path fixtures in known and unknown lines, multiline
keys, version injection, archives/legacy, large/locked files and read-only source
behavior. Native UI checks cover exact snapshot copy/save, errors/cancellation,
keyboard defaults and normal/minimum/scaled layout. A Windows PowerShell test
runs the actual updater action and verifies both cancellation and explicit copy
through the shipped shared preview. Build/install/repair ship identical sources.

F25 is complete after native/server CI and screenshot review pass. Together with
F25a: **19/30 closed; 11 remaining**. The earlier immediate CLI port-rebind test
conflict remains an unconfirmed cause; its strict assertion and diagnostic output
are retained. No merge, release or version bump.

## Stage F26a — shortcut migration and startup preference preservation

The installer now creates one main `ProGo.lnk` with `--show`, not a second
identical `ProGo Status.lnk`. It records whether an installation already exists
before copying files. Existing installs (including retained settings/version
markers after removal or repair) never recreate an absent Windows Startup link.
An existing Startup link is not rewritten, preserving custom arguments and the
Windows StartupApproved state. First installs retain the established default;
`-NoStartup` and `-NoStartMenuShortcut` remain independent opt-outs.

Installed application startup migrates the recognized old menu entry using the
same packaged Unicode Shell implementation as the installer. Migration verifies
both target and arguments, creates/verifies a primary before removing the old
entry, and preserves unrelated/customized/unreadable links. Failure cannot stop
application startup; a later launch can retry. Portable execution, self-check,
secondary-instance activation and an intentionally absent menu do not create
shortcuts. No connection setting, CLI entry point or Windows proxy setting changes.

Native PowerShell tests use actual Shell links under disposable directories,
including Unicode paths, first/repeated installation, independent opt-outs,
missing executable, foreign/custom/corrupt links, locked deletion/retry and
installed-versus-portable migration. Build/install/repair ship identical source.
Windows/server CI is required before the stage is ready for review.

F26 remains **open** for the visible Windows-startup preference and the remaining
uninstall phone-firewall acceptance. Progress remains **19/30 closed; 11 remaining**.
No merge, release or version bump.

## Stage F26b — explicit Windows startup preference

Settings → Connection now separates launching ProGo at Windows sign-in from
connecting to SSH when ProGo starts. The new checkbox reads the actual owned
Startup shortcut; no new JSON default can override a previous opt-out. Portable
copies and unreadable/customized shortcuts show an unavailable state without
blocking unrelated settings. A separate button opens Windows Startup settings;
the UI explicitly distinguishes shortcut registration from Windows' permission.
The application does not edit undocumented StartupApproved registry values.

Changes apply only on Save, after field validation. A failed settings/port apply
rolls registration back; removing registration restores exact prior shortcut bytes
if the downstream save fails. A stale snapshot refuses an external change before
other settings apply. Rollback preserves a later external edit and surfaces an
explicit incomplete-recovery error. Cancel, window close, status refresh and the
Windows-settings button cannot commit staged changes. The existing ordinary CLI
launch and SSH auto-connect behavior are preserved.

Tests extend the actual shipped Shell-link suite with explicit enable/disable,
rollback, external conflicts and unavailable/oversized inputs. Native UI tests
exercise Save/Cancel, real settings-file denial and retry, independent SSH choice,
keyboard traversal, failure focus and safe handling of portable/customized states.
Existing minimum/scaled connection layout tests include the new controls.
Windows/server CI and screenshot review are required before review readiness.

F26 remains **open** for uninstall phone-firewall acceptance. Progress remains
**19/30 closed; 11 remaining**. No merge, release or version bump.

## Stage F26c — uninstall phone firewall cleanup

Uninstall now confirms phone firewall cleanup after the existing same-user shutdown
handshake and before removing shortcuts, the executable or user data. Only local
PersistentStore rules with the two legacy names, the exact installed executable,
inbound Allow, UDP and the corresponding fixed phone port qualify. Same-named
rules for another executable or a different direction/action/protocol/port are
preserved with a warning; unrelated rules and policy stores are untouched.
Missing executables and already absent rules are valid cleanup states.

Policy read/removal failures stop uninstall instead of being treated as absence.
A non-administrator launches only the installed removal helper through Windows
UAC, carrying the original user's explicit executable path. Cancellation, a failed
child or remaining owned rules retains program files/shortcuts for retry. The app
may already have stopped and some rules may already be removed; retry completes
cleanup without re-opening ports. No execution-policy bypass or elevation of the
whole per-user uninstall is added. Maintenance ownership remains held throughout.

The shipped manual removal command uses the same policy; build/install/repair
ship the helper. Tests use real disabled Windows firewall rules on disposable CI
and cover exact selection, another installation, changed protocol/port/direction,
missing executable, repeat removal, provider/removal denial and successful retry.
Elevated launch cancellation/arguments/false success use injected command boundaries;
these do not claim actual interactive UAC acceptance. The real uninstall fixture
checks refusal before file deletion and removes two actual rules on successful retry.

After Windows/server CI passes, F26 implementation is complete across F26a–c:
**20/30 closed; 10 remaining**. Actual Windows sign-in and interactive UAC checks
remain manual acceptance, separate from CI. No merge, release or version bump.

## Stage F28a — typed dispatch and proven dead paths

The application now resolves every existing UI action ID through one immutable
command definition before dispatch. Definitions hold canonical identity, route
readiness requirement and navigation destination. The handler and pending route
queue use `AppCommand`, so the four legacy terminal/Codex aliases share the exact
CLI operation identity for start, deduplication and cancellation. Existing IDs
remain an explicit compatibility boundary for unchanged UI surfaces and tests.
Unknown/nonexact IDs are ignored before any route or preference mutation; they
cannot be parsed as enum numbers or default to Connect. Logs omit unknown input.

The ordinary Start CLI, independent desktop/phone/full stop, saved-setting manual
actions, async startup/cancellation and target-page behavior retain their existing
regressions. New checks cover catalogue identity/unique round trips and aliases,
invalid input, and actual ignored commands while CLI is pending and active.
The unused `PortForm` and private `SafeTarget`/`ClearUserIfOwned` methods are removed
only after repository-wide reference checks; current port settings remain intact.

The initial CI run 37498255925 passed all 1531 desktop assertions, then timed out
in the unchanged shutdown startup-log barrier. The limited failure output does
not prove the original cause. Review found that its current-file character offset
cannot survive F25 rotation and its exclusive reader can suppress best-effort
writes. The fixture now uses a unique pre-launch marker, shared bounded reads of
current/two archived generations, and probes the HTTP listener only after the
completion marker. Deterministic tests force rotation and reject stale completion.
All original readiness predicates and the 12-second deadline remain required;
no production logging, proxy or startup policy is changed.

This stage deliberately stops at dispatch. UI callbacks still emit compatibility
IDs; shared display names, effect/availability state and remaining direct menu
callbacks belong to the next F28 stage. F28 remains open. Windows/server CI is
required before review readiness; no additional visual or product behavior is
introduced. Progress remains **20/30 closed; 10 remaining**. No merge or release.

## Remaining stages

F09, F16–F17, F19, F22–F23 and F27–F30 remain separate work (10 findings). Preserve the ordinary
**Запустить CLI** entry point throughout.

F09 changes to the vault/decoy contract require a separate decision as described
in `DEVELOPMENT.md`. A public release/version bump follows verified stages;
preview changes must not be presented as already installed on users' devices.

## Stage F28b — typed presentation callbacks and shared captions

The dashboard, immediate settings controls and 20 catalogued tray items now send
`AppCommand` directly to the same application handler. Compatibility string IDs
remain a separate checked adapter for existing callers/tests; legacy aliases and
pending route ownership are unchanged. Menu items carry their typed identity,
so tests can check duplicates without treating visible wording as an identifier.

`AppCommands` owns full, compact and immediate-settings captions. Existing short
card labels, navigation wording and ordinary **Запустить CLI** are retained;
contextual wizard entry and transient pending-state text are still local. Tray
Help/Update now enter the same dispatcher as their dashboard counterparts; no
updater implementation, release flow or launch semantics change. Settings actions
still use saved values and do not save/cancel pending form edits.

Native tests retain exact legacy action expectations at typed callback boundaries,
check every migrated tray identity/caption and exercise tray CLI On followed by
settings Off with dashboard observation. Existing geometry, keyboard, contrast,
startup/cancellation and ordinary Codex environment tests remain required.
Local public-content validation passes; native Windows/server CI is required for
this branch. Shared availability/effect metadata and the remaining direct
backup/log/exit callbacks are separate F28 work. **20/30 closed; 10 remaining**.
No merge, version bump or release.

## Stage F28c — shared pending-command availability

The catalogue now evaluates an immutable UI-thread snapshot of pending commands,
connection startup and prepared shutdown. Dashboard route controls, catalogued
tray actions, settings manual controls and the dispatcher use the same rule.
Repeated pending operations are unavailable; CLI and Windows can still join the
same startup. Off/Stop and settings remain available during startup. Plain Connect
can no longer replace another waiting operation via a stale UI or legacy call;
explicit Reconnect retains its intentional replacement semantics.

Pending changes refresh open settings immediately through a scoped subscription,
removed in finally when the dialog closes. Existing UI timers handle background
startup state, and the tray rechecks on opening. Prepared/cancelled shutdown also
refreshes availability. This adds no retry loop, network probe, persisted setting,
new worker or startup/cleanup behavior.

Native regression coverage uses the existing disposable SSH child's release-file
gate, not a fixed sleep. It checks all three surfaces, stale compatibility calls,
shared CLI/Windows startup, scoped cancellation, unsaved settings preservation,
subscription disposal, successful retry and shutdown cancellation. Pure checks
cover snapshot ownership and cancellation/independent-mode rules. Public source
validation passes; Windows/server CI is required for this branch.

F28 remains open for effect metadata and remaining direct backup/log/exit commands.
Progress: **20/30 closed; 10 remaining**. No merge, version bump or release.

## Stage F28d — remaining service commands and operation descriptions

The remaining tray actions (main window, backup create/restore/folder/cleanup,
report preview, personal journals, application folder and exit) now enter the
same typed dispatcher. All 30 actionable tray items have catalogue identities;
Connections and Windows Settings add the two form-only entries. Help's journal
and report buttons also dispatch typed commands. Local field editors, browser
links and same-user maintenance IPC remain their separate, scoped interfaces.

All 32 operations declare their visible/context labels and effect description.
Tray tooltips and accessible descriptions, dashboard action descriptions and
immediate settings/help descriptions use this metadata. Short contextual labels
remain explicit variants of one operation, not separate handlers. Navigation
bindings are typed; action strings remain only in the checked compatibility map.
The catalogue/state model has no WinForms dependency; AppCommandUi handles the
small native button presentation adapter.

Backup selection, validation/confirmation, cleanup previews and shutdown refusal
bodies are preserved. Tests now invoke Exit through the actual command boundary
when cleanup fails, cancel Restore from the real tray chooser, close the real
report preview without export, and verify main-window reuse. Catalogue tests cover
all identities/descriptions and exact tray membership without alias duplication;
existing backup/restore/maintenance/ordinary CLI and pending-mode tests remain.

Together F28a-d complete the scoped F28 audit correction. Acceptance requires green
Windows/server CI for this head; no physical Narrator/DPI or antivirus claim is
made. Progress after acceptance: **21/30 closed; 9 remaining**: F09, F16, F17,
F19, F22, F23, F27, F29, F30. No merge, version bump or release.


## Stage F17d — SSH diagnostic keyboard access and startup wording

At the owner's request, Settings → Connection now labels the existing Windows
startup preference “Запускать вместе с системой”. Visible guidance and its
accessible description clarify that this launches ProGo after user sign-in;
SSH auto-connect remains a separate preference. Help uses the same wording.
This reuses F26b's owned shortcut and Save/Cancel/rollback implementation rather
than adding a second registration or changing an existing user's preference.

The SSH diagnostic report and progress have named accessibility surfaces.
Progress descriptions track current text; the Cancel/Close button's accessible
name and effect follow running, cancelled and completed states. Explicit keyboard
order follows report → retry → close, skipping retry while running. Escape keeps
its existing cancel-while-running / close-when-complete behavior.

Native regression coverage extends the existing process-tree diagnostic fixture:
Tab and Shift+Tab during work/after cancellation/after completion, Escape
cancellation and closing, current accessible action names, retained retry, and
owned child cleanup. Existing startup Save/Cancel/rollback tests now also verify
the requested wording and sign-in explanation. No new worker, setting, network
request or shell-registration implementation is introduced.

Windows/server CI is required for acceptance. F17 stays open for remaining forms
and actual Narrator/contrast-theme acceptance; synthetic accessibility checks do
not replace those. Progress remains **21/30 closed; 9 remaining**. No merge,
version bump or release.


## Stage F17e — route diagnostic accessibility and final startup label

The owner preferred the more precise startup wording after F17d. Settings/help
and the native startup fixture again use “Запускать ProGo при входе в Windows”.
Sign-in guidance, shortcut ownership, Save/Cancel and rollback remain unchanged.
This supersedes F17d's checkbox wording, not its behavior.

Route/speed diagnostics now expose a stable accessible name and current result
for every value. Route and speed action names/descriptions follow start, cancel
and retry, including the download traffic explanation and independent cancellation
scope. The explicit tab order follows the visual buttons from left to right.
Closing and reconnecting have separate effect descriptions. No probe, deadline,
connection, timer, cancellation implementation or default action changes.

Native coverage extends the existing injected-measurement fixture with forward
and backward keyboard traversal during work, after cancellation and completion,
and current accessible result/action checks. A separate actual modal window
checks Escape while route/latency/speed are all active: all three workers settle
without starting a repeat. Existing process/socket deadline tests are retained.

Windows/server CI is required for acceptance. F17 remains open for the remaining
forms and actual Narrator/contrast acceptance. Progress remains **21/30 closed;
9 remaining**. No merge, version bump or release.


## Stage F17f — dashboard keyboard order and accessible state

The dashboard now declares keyboard order through navigation, connection actions,
Windows/CLI/phone cards and footer actions. Card reflow retains the same logical
sequence in one or three columns. Status labels expose stable names and current
text. The local Home action also describes its purpose.

Button styling no longer assigns AccessibleName: native buttons derive their name
from the current caption unless the caller explicitly supplies a more precise
purpose. This removes stale Connect/Reconnect names and preserves the dashboard's
Windows proxy qualifier across initial/live palette application. Existing explicit
names, effect descriptions and handlers remain authoritative.

Native coverage walks Tab and Shift+Tab across both dashboard widths, verifies
pending commands are skipped and cancellation restores availability, checks fresh
connection evidence and named status values, and preserves ordinary CLI dispatch.
It also verifies live contrast refresh does not replace the Windows-specific name
and that traversal itself triggers no command. Existing layout and command-policy
suites remain in place. No settings, connection lifecycle or startup wording changes.

Windows/server CI is required for acceptance. These are native control and
synthetic contrast/layout checks, not physical Narrator or real DPI acceptance.
F17 remains open for other forms and physical acceptance. Progress remains
**21/30 closed; 9 remaining**. No merge, version bump or release.


## Stage F17g — vault, entry editor and PIN keyboard access

Vault search/type filters and the read-only record list now expose distinct names
and descriptions. The list uses arrows for row selection and StandardTab to move
to actions. Explicit visual keyboard order covers filters, list and footer; actions
explain confirmation, saving, closure and the existing clipboard cleanup policy.

Entry fields inherit their visible label as AccessibleName. The show-secret toggle
and secret description track hidden/visible presentation without including the
secret in the name or description. Save/Cancel expose their effects. Create/unlock
PIN fields have distinct names and masked-input guidance; their existing default
buttons, validation and four-digit policy are retained. No encryption, KDF, vault
format, decoy behavior, persistence, clipboard implementation or PIN policy changes.

Isolated native fixtures walk forward/backward through all three forms, including
empty search results and both PIN modes. Native grid key handling verifies arrow
selection and Tab/Shift+Tab exit. Search/type filters, masked/show/hide presentation,
editor cancellation and existing clone Save are covered. Records are synthetic;
the session cannot persist. Vault bytes/existence are checked unchanged. Focus
failure diagnostics log accessible control names instead of field text, so masked
input values are not written to test output. Screenshots retain masked inputs.

Windows/server CI is required for acceptance. Actual Narrator/contrast and real DPI
acceptance remain outstanding; F17 stays open. Progress remains **21/30 closed;
9 remaining**. No merge, version bump or release.


## Stage F17h — five-step phone VPN wizard accessibility

The VPN wizard gives owner/token/home fields and its read-only router instructions
accessible names. Tokens remain masked and their descriptions never include the
value. Actions describe local draft navigation, server setup, firewall elevation,
profile issuance, manual confirmation and phone-only stop scopes. The current
heading, operation result, issuance and phone counters expose stable names and
current descriptions. The progress indicator exposes the current step as static
text without participating in Tab traversal.

Keyboard order is rebuilt with each step. The router grid uses StandardTab; the
ordinary Back/Next handlers and busy/installation gates are retained. No SSH,
firewall, profile sharing, access persistence, verification or recovery logic changes.

Isolated native fixtures walk both entry modes and all five steps, with/without
installation confirmation and after an injected operation failure. Native router
Tab reaches the home field. Actual Back handlers retain owner/token/home drafts;
profile issuance and user internet evidence remain distinct. An injected pending
operation preserves the blocked-close behavior and names its corrective result.
Fixtures never execute SSH/firewall/QR operations; encrypted private access bytes
and directory existence are checked unchanged. Screenshots use synthetic fields
and mask the token. Existing VPN/wizard state suites remain required.

Windows/server CI is required for acceptance. F17 remains open for remaining
forms and actual Narrator/live system contrast acceptance; F16 real DPI is separate.
Progress remains **21/30 closed; 9 remaining**. No merge, version bump or release.


## Stage F17i — profile-sharing dialogs keyboard access

The HTTPS-origin dialog labels its address input and exposes the current validation
or setup result. Action descriptions distinguish SSH/server changes from verification
and saving only after verified ownership. Explicit keyboard order skips the setup
button for friends. A named Close/Escape action dismisses the draft without saving;
it is disabled during the existing operation/blocked-close guard. The same dialog
factory is used by the production modal entry point and the native UI fixture.

Close and revoke errors stay in a separate footer outside the scrollable QR content,
so a multiline error remains visible alongside the keyboard-focused Close. Starting
another revoke attempt clears the earlier error; a successful retry cannot keep
showing stale failure text.

The QR graphic has a stable accessible name and non-secret description, without a
Tab stop or URL in metadata. Named lifetime, clipboard and revoke results follow
their current text. Actions explain copying, server-side revocation and ordinary
closure. Close/Escape preserves the existing close behavior: it does not revoke a
link, confirm phone installation or stop an installed VPN. Explicit Tab order skips
unavailable copying/revocation. QR expiry, requests and clipboard policy are unchanged.

Isolated native fixtures walk owner/friend origin forms, validate invalid input
before any HTTPS/SSH request, check safe Enter defaults, and dismiss uncommitted
addresses via Escape. Synthetic QR fixtures exercise pending/rejected/successful
revocation, retry, expiry, action availability and closure without extra requests.
The QR matrix is intentionally nonfunctional; these tests verify control behavior,
not scanning. Existing independent QR-decoding/server tests remain required.
No external requests or clipboard writes occur in these fixtures; opaque private
access files and directory existence must remain unchanged. Screenshots cover
both origin modes and failed/expired QR presentation.

Windows/server CI is required for acceptance. Actual Narrator/live system contrast
and other forms remain outstanding; F17 stays open. Progress remains **21/30 closed;
9 remaining**. No merge, release or version change.


## Stage F17j — friends and token keyboard access

Friend search and selection explain filtering, native arrows, full identity and
revocation scope. Counts, selected details and operation results expose their
current text. Repeat revoke follows the current selected status; duplicate names
never replace the selected ID. Pending operations show a current busy result.

The manager and one-time token dialog provide Close/Escape without an implicit
issuance or copy default from a text field. Close and corrective/clipboard results
stay in a fixed footer outside scrolling actions. Focus brings body actions into
view. The existing busy-close guard, confirmation, uncertain-operation mutation
gate, server reconciliation, issuance and shared clipboard policy are preserved.
The token remains masked and absent from accessible names/descriptions.

Isolated Windows fixtures walk Tab/Shift+Tab in unselected, selected, empty-search,
uncertain, failed-refresh and reconciled states. Native list arrows distinguish
active/revoked duplicate names. Injected pending/rejected revoke and failed/successful
list responses verify existing identity, closure and mutation gates. The token
fixture checks masked metadata, safe text-field Enter, visible focused actions and
Escape. Fixtures never issue a token, execute SSH or write the clipboard; opaque
private access files and directory existence must remain unchanged. Existing
issuance/reissue, clipboard and server suites remain required.

Windows/server CI is required for acceptance. Actual Narrator/live contrast and
remaining forms are outstanding; F17 remains open. Progress remains **21/30 closed;
9 remaining**. No merge, release or version change.


## Stage F17k — diagnostic export preview keyboard access

The diagnostic preview exposes the transfer guidance and read-only snapshot purpose.
Copy, Save As and Close descriptions distinguish reviewed export, file-choice
cancellation, external sending and connection scope. The named export result
tracks its current guidance, failure, cancellation or success text. Existing action
handlers, report projection, snapshot lifetime, layout and Enter/Escape defaults
are retained; no log collection or export-policy changes.

Native coverage extends the existing preview fixture with Tab/Shift+Tab at normal
and minimum widths, visible actions, safe text-field Enter and focused Close Enter.
Injected copy/save failures retain keyboard access and generic corrective results;
explicit successful retries replace stale errors. Cancelled saving followed by
Escape adds no export. Source log bytes and opaque private access files/existence
must remain unchanged. Copy/save callbacks are isolated from real clipboard/file
exports; the existing updater helper fixture still checks actual reviewed clipboard
copying through the shared production dialog.

Windows/server CI is required for acceptance. Actual Narrator/live contrast and
remaining help navigation are outstanding; F17 remains open. Progress remains
**21/30 closed; 9 remaining**. No merge, version bump or release.


## Stage F17l — backup selection keyboard access

The backup selector exposes named selection guidance, its list and read-only
current details. Buttons explain the next verification step and cancellation;
selection itself does not restore data. Tab order follows the visible flow, with
the disabled next step skipped in an empty list. The empty details pane explains
that no copies are available. Native first-item selection and Enter/Escape defaults,
selected backing paths and all downstream verification/confirmation gates remain.

Isolated Windows fixtures exercise Tab/Shift+Tab, native list arrows and changing
details with duplicate display labels. The real modal TryPick route verifies Enter
returns the selected path and Escape returns none for populated/empty lists.
Named keyboard controls remain visible at the existing initial size. Fixtures do
not prepare/restore backups, launch maintenance or write real clipboard exports;
synthetic copy bytes, settings and opaque private access files/existence must remain
unchanged. Existing backup integrity, scoped restore and maintenance suites remain
required.

Windows/server CI is required for acceptance. Help navigation, physical Narrator
and live contrast remain outstanding; F17 stays open. Progress remains **21/30
closed; 9 remaining**. No merge, version bump or release.


## Stage F17m — help topics and keyboard reading

Help retains its six topics, instruction text and action routes. Each instruction
pane is named, keyboard focusable and exposes its read-only purpose. Up/Down,
PageUp/PageDown and Home/End scroll only the directly focused instruction. Native
topic arrows and Tab/Shift+Tab retain ordinary navigation; focused actions scroll
into view. A visible focus rectangle identifies the reading pane.

A fixed footer provides keyboard guidance and Close. Escape and focused Close Enter
dismiss help without changing settings or stopping connections. Enter in instruction
text has no implicit export/log/browsing action. Descriptions distinguish diagnostic
preview from private logs, page opening, file upload and package installation.

Native Windows coverage exercises all topics at normal client and minimum window sizes, topic
arrows, visual-order Tab/Shift+Tab, bounded line/page/end scrolling, visible focused
actions and independent closure. An injected focused preview command verifies the
existing route once; no real log/browser/clipboard or connection action is executed.
Settings and opaque private access files/existence must remain unchanged. Existing
help content, ordinary CLI and autostart wording contracts remain required.

Windows/server CI and native screenshot review are required for this substage.
Known help/backup selector gaps now have native fixture coverage, but physical
Narrator/live contrast and real DPI acceptance remain outstanding; F17 stays open.
Progress remains **21/30 closed; 9 remaining**. No merge, version bump or release.


## Stage F23a — deadline and cancellation for update checks

Update metadata checks now share an async transport with a 15-second total network
deadline. Abort/stream closure interrupts waiting for headers or a stalled body;
caller cancellation stays distinct from a failure result. Timeouts explain retry.
Declared and streamed metadata lengths are limited to 2 MiB before parsing.

The existing synchronous entry point delegates to that transport. The fixed GitHub
release URL, headers, version comparison and default proxy route remain; no update
installation, maintenance handoff, CLI behavior or automatic installation policy
changes belong here. The cancellation API is groundwork: this substage does not
add a visible Cancel button or claim that the whole F23 UI is finished.

An isolated Windows socket fixture covers valid/current/older releases, malformed
JSON and versions, HTTP failure, stalled headers/body with deadlines and cancellation,
pre-canceled requests, retries and declared/chunked oversized metadata. Settings and
vault bytes/existence remain unchanged. Acceptance requires Windows/server CI.

F23 remains open for visible progress/cancel controls, release notes and safe VPS
waiting cancellation. Progress remains **21/30 closed; 9 remaining**. No merge,
version bump or release.


## Stage F23b — visible update checks, cancellation and release notes

One update window now shows progress, installed/new versions, errors and explicit
retry, or a bounded plain-text “what's new” preview with missing/shortened notices.
Repeated dashboard/tray commands activate the existing check. The window exposes
Cancel while checking; Escape, title Close, disposal and successful app shutdown
cancel waiting work. Generation/handle checks suppress late results after closure.

No default installation button is assigned. Only explicit installation activation
returns the reviewed available result to the existing maintenance handoff. Existing
CLI routes and installer code remain. Synthetic fixtures do not install packages.

Native Windows acceptance covers each state, retries, cancellation/late results,
repeated modal commands, shutdown and keyboard/action visibility at normal/minimum
sizes. Settings/vault bytes and existence remain unchanged. Real socket metadata
fixtures also cover release-note decoding/normalization/limits. Windows/server CI
and native screenshot review are required for acceptance.

F23 remains open for download/install phase UX and safe VPS waiting cancellation.
Progress remains **21/30 closed; 9 remaining**. No merge, version bump or release.


## Stage F23c — readable phases in the installed update helper

The existing visible PowerShell helper now names ten reached phases: release
metadata, application shutdown/file access, download, package validation, backup,
staging, staging check, installation, installed check and restart. Each phase has
a Russian explanation. Phase numbers describe work, not byte percentages or a
promised duration. Plain console text remains if the host suppresses progress.
File replacement explicitly asks the user to wait and keep the window open.

Rollback is a separate unnumbered attempt, never presented as completed recovery.
NoLaunch explains manual launch instead of claiming the app restarted. Progress
clears before a result dialog and in finally, including failed rollback. Rendering
failure must not alter package verification, commit, rollback or lease cleanup.

An isolated Windows fixture executes the real installed transaction and package
checks with local synthetic network replies and final dialogs. It covers success,
current version, download/hash/staging/commit/restart/rollback failures and a host
that refuses progress rendering. It checks reached phase order, terminal outcome,
progress clearance, preserved settings/vault and released maintenance ownership.
Existing real package, application/maintenance and server suites remain required.

This bounded substage does not add a cancellable installation window, transport
deadline or safe VPS wait cancellation; those parts of F23 remain open. The CLI,
update trust policy and automatic installation behavior are unchanged. Progress
remains **21/30 closed; 9 remaining**. No merge, version bump or release.


## Stage F23d — bounded installed-helper network transport

The installed helper now obtains release metadata through a local compiled transport
with a 10-second total network deadline, and downloads the package with a 180-second
deadline. Limits are 2 MiB for metadata and 64 MiB for the compressed package, checked
against both declared and streamed lengths. These are failure bounds, not promised
download times. Metadata's deadline fits inside the existing 15-second handoff wait.

The transport API accepts caller cancellation; abort and stream closure interrupt
headers, stalled bodies and slow continuous responses. Cancellation keeps its caller
identity. Failed or interrupted downloads close and delete their own partial file;
an existing target is never overwritten or removed. HTTPS redirects are bounded and
share the same deadline; downgrade and non-web destinations are refused. Current-user
proxy routing remains, with proxy bypass only for internal loopback fixtures.

The core retains stable-release/version/asset-location/digest/archive checks and all
backup, staging, commit, rollback and maintenance ordering. No remote source is
compiled or evaluated. The helper source ships with the release and installer.
Real Windows sockets cover deadlines, cancellation, oversize/truncated responses,
HTTP errors, redirects, retry and byte-exact packages; PowerShell tests cover the
installed source loader/adapters. The real transaction regression suite remains.

This is transport groundwork for the installation window: there is no visible
Cancel button for downloads yet, and the core currently calls with no caller token.
F23 remains open for that window, pre-commit cancellation/relaunch integration and
safe VPS waiting cancellation. Progress remains **21/30 closed; 9 remaining**.
No merge, version bump or release.


## Stage F23e — native installation window and download cancellation

The installed helper shows one themed native window with the reached phase and a
read-only explanation. Dashboard/tray launch no longer opens a second console;
direct PowerShell invocation keeps its ordinary console text. No default action is
assigned. Accessible status/explanation names, keyboard cancellation, native scroll
and wrapping/resizing reuse the existing theme, icon and high-contrast handling.
The shipped local window source is compiled with the installed theme/icon sources.

The PowerShell transaction/mutex stays on its original thread. Only presentation
runs on a separate STA message loop. The Cancel download button is available only
while transferring the archive; button, Escape and title Close request the same
transport token. The window stays visible while the interrupted transfer unwinds.
Closing it cannot terminate validation, backup, staging, commit, rollback or restart.
An atomic cancellation/EndDownload boundary prevents an accepted cancellation from
crossing package validation. Presentation does not implement that safety decision.

Only a requested download cancellation before MainWasChanged becomes the dedicated
UpdateDownloadCancelledException marker. After closing the window and completing
temporary-work cleanup/lease disposal, the installed bootstrap treats that marker
as cancellation rather than failure. Existing recovery relaunches the installed
app only if its initiating process exited and NoLaunch is false. A surviving app is
kept; other OperationCanceledException errors cannot claim files were unchanged.
The cancellation notice makes no unverified claim that the app successfully started.

Native Windows fixtures exercise real socket/body cancellation for the three UI
paths, the validation race, protected-phase Close/Enter, layout and accessible names
at normal/minimum/scaled sizes and synthetic high contrast. Native screenshots are
required for review. Existing transaction fixtures add cancellation before/during
the completed-download boundary, opaque settings/vault/executable/helper preservation,
no extraction/backup, window/temporary cleanup, bootstrap outcome/relaunch rules and
released ownership. Network transport, package trust, CLI and server suites remain.

F23 remains open for safe VPS waiting cancellation. Physical Narrator/live contrast
and actual DPI acceptance remain separately open. Progress stays 21/30 closed,
9 remaining. No merge, automatic installation, version bump or release.


## Stage F23f — cancellable phone-channel startup waiting

The phone VPN wizard offers **Отменить запуск канала** while awaiting its local
SSH/SOCKS handshake or the VPS relay receiver. Button and Escape request the same
caller cancellation token; title Close keeps the window open until startup settles
and then closes it. The wizard remains responsive, blocks competing actions,
exposes current status to accessibility and restores retry after cancellation.
SOCKS readiness uses the existing asynchronous owned proxy worker with a 10-second
deadline instead of synchronously launching SSH and polling an open port. The
receiver retains its 10-second handshake deadline. Failure is distinct from a
requested cancellation; neither implies that phone internet has passed.

A linked caller token interrupts the relay probe socket and prevents publishing a
late relay. A cancelled startup cleans its owned SSH process and private temporary
session before reporting cancellation. Cleanup uses a blocking process-ownership
check; an unsettled process or locked private file remains an error with no queued
close or successful-cancellation claim. Retry retains unsettled ownership and refuses
to spawn another channel until cleanup succeeds. Existing saved access is preserved. During
import it may already have been committed before the channel wait; the message
explicitly describes retained saved access rather than rollback. Draft token input
stays masked, the wizard does not advance, and a fresh explicit retry is available.

Windows fixtures use this actual wizard/service/proxy/relay pipeline with a local
owned SSH substitute: absent listener, silent receiver, all three cancellation
routes, disposal/late UI suppression, responsive UI, preserved credentials, cleanup,
retry, pre-cancel, timeout, unrelated-process preservation,
already-running preservation and a real locked-file cleanup failure. Native images
cover pending, minimum-size and cancelled states. A pending protected server
operation refuses title-close cancellation without claiming remote rollback.

This bounded substage does not terminate interactive SCP/SSH provisioning, repair,
invitation or QR setup commands. Those can have already changed the server and need
separate interruption/reconciliation semantics. F23 remains open for that VPS
operation waiting work. Progress stays **21/30 closed; 9 remaining**. No merge,
version bump, release or changes to ordinary CLI/system-proxy settings.


## Stage F23g — safe cancellation before VPS configuration

Every owner operation first copies the public helper through SCP. That preparation
now has one themed native waiting window with **Отменить подготовку**, accessible
status, no default Enter action and a five-minute deadline. Button, Escape and title
Close request the same cancellation. The window waits for the owned process tree
to exit before returning; disposal suppresses late UI updates. OpenSSH password and
host-key prompts remain in a real separate console. The prepared SCP console is
contained before it can spawn children; cancellation, deadline, failure and normal
parent exit settle its job without targeting unrelated processes. This owner is not
used for user terminals, persistent tunnels or remote configuration commands.

A confirmed preparation cancellation never crosses the remote-command boundary.
The own-VPS wizard retains its entered draft and saved access, stays on the same
step and offers an explicit retry without claiming token creation. Local temporary
preparation files are removed before reporting cancellation; a real locked-file
cleanup failure remains an error. Partially uploaded public helper files can remain
in the remote temporary directory: no remote cleanup or rollback is claimed.

Windows fixtures cover the actual console, root and descendant path, button/Escape/Close/
disposal, UI heartbeat, native normal/minimum layout, accessible status, retry,
pre-cancel, deadline, nonzero exit, orphan cleanup and unrelated-process preservation.
The actual AdminAsync/own-VPS wizard boundary is exercised with isolated transports:
no setup call or token commit after copy cancellation, unchanged protected access,
private work cleanup, locked-file refusal and protected server-operation Close after
a successful copy. Existing CLI, desktop proxy, relay, updater and server checks stay.

Acceptance requires green Windows/server CI and review of native preparation images.
This bounded stage only interrupts SCP before configuration. SSH provisioning,
repair, invitation and QR setup remain protected once launched; interruption and
reconciliation of their server result are the next F23 substage. In the friends
window, the existing conservative refresh-before-mutation policy is retained even
if preparation was cancelled. F23 stays open; **21/30 closed, 9 remaining**.
No merge, version bump, release or changes to ordinary CLI/settings behavior.


## Stage F23h — recoverable VPS operation results

The server helper accepts an optional owner request ID for setup, invitation,
revocation, repair, listing and QR setup. It saves a private record before starting
the command and commits its exact result before exporting it to SSH. A repeated
ID with the same parameters returns that saved result without running the command
again; changed parameters, a concurrent request or an uncertain result are refused.
The existing owner lock still serializes different IDs and older desktop commands.
The boot network action remains separate and cannot accept a request ID.

Owner-only status queries contain state and timestamps, with no token, password,
private key, host, invitation name or parameter fingerprint. A separate private
result query retrieves the original result. Running records with no live owner,
partial failures and interrupted result commits stay unconfirmed; recovery never
assumes rollback or authorizes a blind new request. Missing status is a snapshot,
not proof that a previously submitted command cannot still arrive. Revoked access
cannot be recovered, recreated or reactivated through the stored result.

Records and locks live under root-owned `operations` inside private VPN state:
directories 0700, files 0600. Atomic replacement and file/directory sync precede
execution/export. Symlinks, hardlinks, special files, invalid records, unsafe modes,
ownership and oversized results are refused. Successful invitation records retain
an additional private copy of the token for recovery; they are not public downloads
or logs. This stage has no expiry, acknowledgment or automatic pruning. Record
retention must preserve duplicate protection; desktop consumption and credential
cleanup policy remain for the next integration stage.

Verification adds 15 Linux tests for concurrency, process death, failed storage,
private files, exact replay, changed parameters and revoked results. The isolated
server smoke test loses the CLI export after a real invitation, queries completion,
recovers the original token, verifies no second issuance and refuses revoked access.
The actual QR web account cannot read operation credentials. CI and release gates
run this suite alongside existing Windows, CLI, updater, relay and QR checks.

This is the server foundation only. The desktop does not submit request IDs yet;
interactive SSH configuration stays protected and has no new wait-cancel button.
That integration is the next bounded F23 substage. Acceptance requires green CI
on the exact source commit. F23 stays open; **21/30 closed, 9 remaining**.
No merge, version bump, release or changes to ordinary CLI/settings behavior.


## Stage F23i — own-VPS setup recovery in the Windows wizard

Own-VPS setup now registers a stable request after SCP preparation and before
launching SSH. One pending request is kept per Windows user in a private DPAPI
file: the ID, frozen VPS host, SSH port/account and setup name, plus a validated
result when available. SSH administrator passwords and local key contents are
not part of this record. Atomic local replacement and file flush precede dispatch;
this is not a claim of full power-loss durability for Windows directory metadata.

A lost response retains the original ID. Reopening the wizard restores its endpoint
and presents one **Проверить прошлую настройку** action. It copies only the public
helper, queries the original status and retrieves that original result. It never
resubmits setup, including when status is not-found, running, unconfirmed, malformed
or unavailable. Missing status is a snapshot, not proof that a prior command cannot
still arrive. Changing VPS address/account/name cannot abandon the pending request;
changing the local SSH-key selection is allowed for authenticating the same queries.
Recovery-only presentation cannot fall back to fresh setup if its journal disappears.

The result must pass existing token validation and match the frozen VPS host/port.
It remains protected until both access and owner data are saved and verified locally.
Only then is the pending request consumed. Failed storage retains it; a cached token
cannot bypass a live status/result check while that request remains pending. If the
subsequent channel startup fails, the wizard offers **Запустить канал и продолжить**
with already saved access, without misleadingly offering repeat VPS setup.

Fresh copy cancellation creates no request and never launches setup. Cancellation
of copying for recovery explains that the earlier server command may have finished;
it preserves that request and does not claim rollback. Corrupt local records block
new setup and preserve saved VPN access. Native Windows fixtures exercise these
boundaries with DPAPI, locked files, controlled transport failures, concurrency,
restart presentation, keyboard names, UI heartbeat and normal/minimum screenshots.
Server operation and public-content checks remain part of acceptance.

This stage is limited to own-VPS setup recovery. Already launched interactive SSH
waiting remains protected. Invitation, revocation, repair and QR setup retain their
previous command path; their client recovery and wait cancellation are separate
bounded F23 work. A failed status query may leave copied public helpers in the remote
temporary directory; no remote cleanup or rollback is promised. The server's private
result cache still has no expiry, consumption acknowledgment or automatic pruning.
There is no forget-and-reissue control for an uncertain request in this stage.
Acceptance requires green Windows/server CI on the exact commit and visual review
of native recovery images. F23 stays open; **21/30 closed, 9 remaining**.
No merge, version bump, release or changes to ordinary CLI/settings behavior.

## Stage F23j — bounded cancellation of recoverable own-VPS SSH waiting

Own-VPS setup and its read-only recovery queries now use the native waiting
window with a five-minute deadline and **Прервать ожидание SSH**. Button, Escape,
Close and disposal request cancellation and wait for the owned local console
process tree to settle. OpenSSH retains a real console for passphrase and host-key
prompts. A cancellation is never described as server rollback: the protected
original request is retained and the next action queries its status, never blindly
issues another setup. Cancellation accepted before local completion wins over a
queued response. Other owner operations remain protected until their recovery
journals exist; persistent tunnels and user terminals do not use this process owner.

Windows regressions cover native cancellation routes, heartbeat, minimum layout,
private-journal preservation, owned root/child exit, deadline, completion race and
actual AdminAsync dispatch/recovery boundaries. Acceptance remains pending exact
commit Windows/server CI and visual review of the new native images. No release,
version bump, merge, installation or user VPS change. Audit remains 21/30.

## Next stage — Windows Rescue SSH/proxy reliability and responsiveness

Owner-requested follow-up after F23j. Read ProxyService, SshProfileDiagnostics,
Core, settings, timers and agent integration; reproduce failures before changing
runtime. Determine the installed executable version separately from repository
VERSION. Record Windows/HDD/VHDX observations separately from isolated CI.

- Preserve OpenSSH alias resolution and resolve `~/.ssh` for the current Windows
  user. Diagnose missing legacy absolute key paths without replacing settings.
- Detect Windows ssh-agent Running, Stopped and Disabled; guide privileged service
  changes and ssh-add explicitly. Never store passphrases or remove key protection.
- Diagnose background noninteractive authentication failures; bound retry and
  prevent conflicts with unrelated local listeners.
- Profile UI thread work, synchronous SSH/PowerShell calls, polls, timers, repeated
  probes, logs/settings I/O and CPU/RAM/disk. Slow disk and offline/auth failures
  must leave the UI responsive; long work needs cancellation and deadlines.
- Verify independent tunnel/SOCKS5/HTTP CONNECT bridge and opt-in Windows proxy.
  Display actual configured ports (SOCKS default 1080, bridge initial 1881), process
  ownership, last refusal and recovery state. No global traffic capture by default.
- Show executable, alias, agent, key accessibility and noninteractive auth status
  with understandable Russian errors and secret-free diagnostics.
- Add regressions, build and run available suites; deliver PASS/FAIL/NOT_CHECKED
  with explicit live-machine gaps. Prepare an owner-review candidate, never
  publish/install automatically. Service changes require owner consent; no VPS
  configuration change without separate authorization. Existing settings survive.

Live acceptance: working OpenSSH alias; unlocked encrypted key via agent; ProGo
noninteractive connection; real SOCKS handshake and external proxy request;
responsive UI under offline VPS/auth errors; bounded retries and disk activity.
These cannot be marked PASS using synthetic CI alone. The owner's supplied real
endpoint, account and key path are intentionally absent from repository examples.

### F23j local verification (2026-10-10)

PASS: `git diff --check`, public-content scan (161 source files), 15 server
operation regressions, 3 home-profile tests, 2 invitation-metadata tests,
3 public-documentation tests and 2 profile-share synchronization tests (25 total).

NOT_CHECKED: Windows .NET Framework build, new native cancellation regressions,
full Windows desktop/VPN suites, screenshot review, isolated provisioning smoke,
installed Rescue version, physical HDD/VHDX profiling and live SSH/proxy route.
No observed local test failures; this is not a FULL PASS or release candidate.

The local F23j commit was created, but automatic approval review blocked pushing
its feature branch, interpreting the owner's no-publication-without-consent
constraint as also applying to source-branch publication. No push workaround,
remote PR, release, installation, service change or VPS operation was attempted.
The owner subsequently approved feature-branch publication and a draft PR on
2026-10-10. GitHub connector upload is authorized; release and installation remain
unapproved. Windows CI acceptance is still required.

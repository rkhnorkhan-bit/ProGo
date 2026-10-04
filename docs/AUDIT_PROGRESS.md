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

## Remaining stages

F09 and F14–F30 remain separate work (18 findings). Preserve the ordinary
**Запустить CLI** entry point throughout.

F09 changes to the vault/decoy contract require a separate decision as described
in `DEVELOPMENT.md`. A public release/version bump follows verified stages;
preview changes must not be presented as already installed on users' devices.

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

F06 remains partial. Stage 7b will add graceful IPC cleanup to uninstall and cover
maintenance/shutdown refusal paths before removing/stopping the application.
No uninstall script or IPC protocol is changed in this stage.

## Remaining stages

F06–F10 and F14–F30 remain separate work (22 findings). F06 is partial until
its uninstall/IPC and maintenance shutdown stage is verified. Preserve the ordinary
**Запустить CLI** entry point throughout.

F09 changes to the vault/decoy contract require a separate decision as described
in `DEVELOPMENT.md`. A public release/version bump follows verified stages;
preview changes must not be presented as already installed on users' devices.

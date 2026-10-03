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

F03 is **partial**: stage 3b must serialize update/restore operations and their
handoff. It is not counted as fully resolved by this application-lifetime change.

## Remaining stages

F03–F30 remain separate work. In particular, F05 (SSH readiness) and F06 (Windows
restore ownership) are not fixed by these stages. Preserve the ordinary
**Запустить CLI** entry point throughout.

F09 changes to the vault/decoy contract require a separate decision as described
in `DEVELOPMENT.md`. A public release/version bump follows verified stages;
preview changes must not be presented as already installed on users' devices.

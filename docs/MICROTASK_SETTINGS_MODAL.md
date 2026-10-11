# Microtask 03: asynchronous settings save and modal fixture failure exit

Scope: `tests/DesktopStartupTests.cs`, `tests/DesktopUiWorkflowTests.cs`. Application code is unchanged by this microtask.

Baseline: code `a2746268ca1ec74b82839e9ab4268b68bc7eb05d`, [CI run 38148211645](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38148211645), Windows job `114495929814`. Tray restore checks passed; the desktop watchdog later reported no progress at `AsyncCliStartup -> SettingsSaveConnectionIntent`. The enclosing CI step remained active until the job's 40-minute timeout.

## Cause and change

The settings fixture required a successful dialog result and saved SOCKS port immediately after clicking Save. Save now starts a tracked asynchronous operation, and its UI completion is published after `SaveWork` settles. The fixture now measures dispatch separately with the existing 400 ms threshold, then pumps the UI until both the tracked task and form pending state settle within the existing five-second deadline. Only a successful save result, accepted dialog, published port and preserved user connection intent satisfy the check.

The fixture still holds the proxy gate during acceptance. The existing bounded gate wait is unchanged; the acceptance check also proves the gate cannot be acquired, its worker has not completed, its explicit release has not been requested, and its wait has not expired. The expiry marker is published while still holding the gate. Reconnect/PID checks remain after release. This prevents natural expiry or a queued stop worker acquiring the released gate from producing a false success.

On inspection failure, the modal helper previously called Close once. A pending settings form deliberately rejects that close and requests cancellation; an error result leaves the form open. The helper now waits for the captured save task and UI settlement, then closes again. Cleanup failures retain the original inspection exception in an aggregate and dispose only the test-owned modal so the caller can reach its own fixture cleanup.

Two native regressions inject an inspection failure into a real modal settings form: a pending synthetic save settles after cancellation while UI timer ticks continue, and a deliberately stalled synthetic save exercises bounded cleanup failure/disposal. They check original error preservation, cancellation, modal release, unchanged saved settings and removal of the form's route subscription. The synthetic task is explicitly settled afterward; these regressions run before an owned SOCKS child is started.

## Evidence

- Source review, whitespace checks and public-content scan: PASS.
- Windows C# compilation and targeted native fixtures: pending CI.
- Full Windows and isolated VPN server CI: pending CI; the previous candidate did not pass.
- User PC/VPS and installed application: NOT_CHECKED, deferred until the complete test installer.

No F01–F31 acceptance status is closed by this test correction. Historical 22/31 remains the evidence baseline. Other prepared microtasks, including general CI process bounding, remain outside this checkpoint.

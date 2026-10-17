# Microtask 02: asynchronous restore tray command coverage

Scope: `tests/DesktopUiWorkflowTests.cs`. Application code is unchanged by this microtask.

Baseline: code `7440ec5eda97bf37708583af079be3694ea0039c`, Windows CI run `38146890018`, job `114492030367`, failed in `UiControls -> ServiceCommandDialogs` at `RestoreBackup`.

## Cause and change

The dialog helper assumed that a tray command displayed its modal dialog before returning. Restore now reads the backup list in the background, so command dispatch returns before the picker appears. The helper disposed its inspection timer immediately after dispatch and could never observe that asynchronous picker.

Keep the timer and Windows UI message pump alive until the expected dialog is observed, using the existing five-second fixture deadline. After canceling the real picker, await the tracked restore task and release of its operation owner before checking cancellation or deleting the fixture. Assert that preview/preparation and helper launch are not reached, that settings and the selected backup remain intact, and that the completed progress window can close before the next tray dialog.

Cleanup cancels and settles any newly started restore before removing the test directory, and restores the injected hooks even if cleanup reports a timeout. Only the completed progress window owned by this operation is explicitly closed; later dialog checks retain their existing coverage.

## Evidence

- Source review, whitespace checks and public-content scan: PASS.
- Windows compilation and native tray cancellation fixture: PASS. The real picker, tracked operation cancellation, absence of preparation/helper launch, preserved settings/backup, and completed progress closure all passed. The following diagnostics export command also passed.
- Desktop suite: FAIL later at its independent watchdog (90 seconds without a successful check), with last attempted check `actual settings save completes while background ownership is busy: False` in `AsyncCliStartup -> SettingsSaveConnectionIntent`, `tests/DesktopStartupTests.cs`. The suffix `False` is the fixture's `stopped` value; that assertion has no PASS record. Subsequent suites have no passing completion evidence from this run.
- Full Windows job: CANCELLED by its 40-minute timeout. After the desktop watchdog reported failure at 14:49:32 UTC, the enclosing CI step did not return before cancellation at 15:21:52 UTC. This is a separate pipeline failure; its cause is not established by this microtask. Cleanup reported an orphan `SocksRecoveryTests` process.
- Isolated VPN server CI: PASS.
- User PC/VPS and installed application: NOT_CHECKED, deferred until the complete test installer.

No F01–F31 acceptance status is closed by this test correction. Historical 22/31 remains the evidence baseline, not a claim that the full new candidate is ready. Other prepared microtasks remain outside this checkpoint.

Verified code: `a2746268ca1ec74b82839e9ab4268b68bc7eb05d` in [draft PR #88](https://github.com/rkhnorkhan-bit/ProGo/pull/88). Evidence: [CI run 38148211645](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38148211645), Windows job `114495929814`, server job `114495929607`. Tray restore checks passed at 14:46:06–14:46:07 UTC; full-candidate success is not claimed. The next separate microtask should investigate the settings-save check and safe failure propagation from the modal fixture; bounded CI process completion remains a separate necessary follow-up.

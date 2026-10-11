# Microtask 02: asynchronous restore tray command coverage

Scope: `tests/DesktopUiWorkflowTests.cs`. Application code is unchanged by this microtask.

Baseline: code `7440ec5eda97bf37708583af079be3694ea0039c`, Windows CI run `38146890018`, job `114492030367`, failed in `UiControls -> ServiceCommandDialogs` at `RestoreBackup`.

## Cause and change

The dialog helper assumed that a tray command displayed its modal dialog before returning. Restore now reads the backup list in the background, so command dispatch returns before the picker appears. The helper disposed its inspection timer immediately after dispatch and could never observe that asynchronous picker.

Keep the timer and Windows UI message pump alive until the expected dialog is observed, using the existing five-second fixture deadline. After canceling the real picker, await the tracked restore task and release of its operation owner before checking cancellation or deleting the fixture. Assert that preview/preparation and helper launch are not reached, that settings and the selected backup remain intact, and that the completed progress window can close before the next tray dialog.

Cleanup cancels and settles any newly started restore before removing the test directory, and restores the injected hooks even if cleanup reports a timeout. Only the completed progress window owned by this operation is explicitly closed; later dialog checks retain their existing coverage.

## Evidence

- Source review, whitespace checks and public-content scan: PASS.
- Windows compilation and native tray cancellation fixture: pending CI.
- Full Windows and isolated VPN server CI: pending CI; previous full candidate failed.
- User PC/VPS and installed application: NOT_CHECKED, deferred until the complete test installer.

No F01–F31 acceptance status is closed by this test correction. Historical 22/31 remains the evidence baseline, not a claim that the full new candidate is ready. Other prepared microtasks remain outside this checkpoint.

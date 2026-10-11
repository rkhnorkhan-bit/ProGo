# Microtask 05 — wait for the actual HTTPS recovery outcome

## Cause and scope

CI run `38156158147` reached
`HomeVpnShareRecoveryTests.RecoverForm("complete", false, false)` and failed
with `HTTPS native fixture failed`. Earlier complete-receipt scenarios with
injected verification/save errors passed.

The fixture waited only for `address.Enabled`. Production success publishes
`DialogResult.OK` after verification, save and journal consumption, leaving
controls disabled. Only a failed operation re-enables editing. Production uses
`ShowDialog`; this fixture uses a modeless form with its own UI loop. Waiting
for failure-only control state cannot recognize successful completion.

## Correction

The fixture waits for either actual success (`OK`) or restored editing, then
keeps its UI loop alive until a nonblocking recovery-lease probe succeeds. The
probe releases its lease immediately; three timer ticks drain UI work before
the fixture closes. This covers the asynchronous gap between owner-thread
success publication and `ConfirmAsync` lease release.

Existing receipt, verification, save-boundary, consumed/preserved journal,
former origin, private inputs and exactly-one-share assertions remain. An
additional assertion checks both terminal control states, the retained draft
and lease settlement. The shared UI controller now includes the scenario and
inner error type/message on failure. Its 20-second deadline is unchanged.

Application code is unchanged. The fixture observes lease release; it does not
claim direct observation of the configuration handler's private final task or
cancellation-source disposal. Production dispatcher disposal settles queued
callbacks when the fixture closes.

## Evidence at the original microtask 05 checkpoint

- Two independent source reviews: PASS. Whitespace and public-content scan: PASS.
- Windows compilation: PASS. Code head
  `778fff7a2948ad75d0f13c0c86ab63fdef74671c`; CI checked synthetic PR merge
  `21d3efa17622e50c636f38fa3f7b58aefcd46f24` against baseline
  `4bb336575bb1216d3047bd2b294aeecad9fdd65a`.
- [CI run 38158069018](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38158069018):
  full Windows job `114525051269` FAIL; isolated VPN server job
  `114525051002` PASS.
- Entire desktop suite: PASS, 3485 checks, 17:10:44 UTC. Entire
  `Test-ProGo.ps1`: PASS, 17:13:41 UTC. Previous owner/output-cap scenarios
  also PASS, including oversized revoke/repair/share and final owner checks.
- Native `RecoverForm` scenarios, including successful completion: NOT_CHECKED.
  CI stopped earlier in `PendingGate` at 17:16:12 UTC with
  `HTTPS native fixture failed [unlabelled]: NullReferenceException`.
  There are zero recovery acknowledgement or new terminal-state assertions
  in this run. The corrected predicate is compiled but not native-validated.
- Remaining HTTPS/wizard, portable harness and Windows Python relay integration:
  NOT_CHECKED. Final public release-content scan, release packing/upload:
  SKIPPED. Local public-source scan is separate and PASS.
- Screenshots/diagnostics: UPLOADED, artifacts `11709528169` / `11709453402`.
  Upload is not visual acceptance or an accepted installer.
- User PC/VPS and installed application: NOT_CHECKED.

Historical 22/31 is unchanged. No complete F01–F31 point or new installer is
accepted by this test correction. Other changes and F09/F22 implementation
remain separate microtasks.

## Next isolated blocker at that checkpoint

`PendingGate` publishes the same reopened-form assertion three times in
33 milliseconds. Its settled callback pumps `Application.DoEvents`, while
`Loop` stops its timer only after that callback returns. Source and logs
indicate reentrant finalization: a nested tick can close the form before the
outer callback accesses `recover.Parent`. The exact null dereference is not
proven by a stack trace. The next microtask must make finalization single-entry,
preserve failure cleanup if settled inspection throws, and prove those paths
with a deliberate nested-pump regression. No application change is justified
by this fixture failure alone.

## Subsequent native evidence — microtask 06

Code `a66a25ed079240f3d73ff34ca5c39466f451eb82` includes the single-entry
controller/owned-context correction documented in
[MICROTASK_HTTPS_FINALIZATION.md](MICROTASK_HTTPS_FINALIZATION.md).
[CI run 38161800807](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38161800807)
checked synthetic merge `acb2c84777346dcf99f6285ee0e305c259691ccf`.
All 12 native `RecoverForm` scenarios passed acknowledgement, terminal-state
and status/result-only assertions, 18:10:11–18:10:31 UTC. In particular,
`complete/verify=False/save=False` passed and consumed only the matching request
at 18:10:31 UTC. Microtask 05's corrected completion predicate is now
native-validated, without changing its deadline or removing earlier assertions.

The full Windows job still failed later in `ConfirmedCleanupFailure`; subsequent
fixtures remain NOT_CHECKED. Server CI passed. This later targeted evidence does
not revise the earlier failed run or close a complete F01–F31 point.

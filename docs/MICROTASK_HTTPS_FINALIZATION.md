# Microtask 06 — single-entry HTTPS fixture finalization

## Cause and scope

CI run `38158069018` stopped in `PendingGate` before any `RecoverForm`
scenario. The reopened-form assertion ran three times in 33 milliseconds,
then `Loop` reported `NullReferenceException`. The settled callback calls
`Application.DoEvents`, while the controller stopped its timer only after
that callback returned. A nested tick could inspect and close the form
before the outer callback reached its retained control's parent.

This change is confined to `HomeVpnShareRecoveryTests.cs`. Application code,
transport deadlines and the controller's 20-second deadline are unchanged.

## Correction and regression boundary

The controller rejects terminal and already-running ticks. It marks completion
and stops the timer before calling settled inspection. A start, tick or
inspection exception immediately stops the controller and attempts every
existing cleanup step, rather than requiring a later tick from a stopped timer.
Cleanup exceptions retain the original error in an aggregate.

The controller owns the same main-form `ApplicationContext` that `Run(Form)`
would create. After disposing its main form, it exits that context only if its
`ThreadExit` event has not already fired. This preserves a fallback for failed
disposal without calling the ambient `Application.ExitThread` after normal
context settlement. Microsoft documents main-form lifetime as the default
[ApplicationContext](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.applicationcontext)
exit trigger; native regression results remain the deciding evidence here.

Two native fixtures deliberately pump an independent UI heartbeat through
three ticks inside settled inspection. They require exactly one inspection,
live form/control-parent ownership throughout that pump, a disposed form on
return and completion within five seconds. The failure case throws a sentinel
after the pump and requires the same exception object as the wrapper's inner
error. The failure case runs first, so the following successful fixture also
proves a fresh UI loop/heartbeat after expected abort. The heartbeat's two-second
bound and controller's original deadline
prevent an unbounded regression. The actual pending form also asserts a single
inspection and has an explicit diagnostic scenario label.

These fixtures validate the test controller. They do not establish completion
of a full application feature, a new installer or user-machine acceptance.

## Evidence

- Two independent source reviews, whitespace and local public-source scan: PASS.
- First code head `5c943c67d98fdba67efb36ed35e565a088d689a5`,
  [CI run 38160835465](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38160835465),
  checked synthetic PR merge `faa4c6460c187c6d8b1c2c7ba7890e692f03f200`.
  Windows `114533212602` FAIL; server `114533212734` PASS. Desktop 3485 and
  complete Test-ProGo passed. Sentinel fixture passed at 17:52:56 UTC, but the
  immediately following successful fixture failed about 10 ms later. Pending
  form and all recovery scenarios were NOT_CHECKED. The timing is consistent
  with an additional ambient thread exit; that cause remains an inference
  until the owned-context correction has fresh native evidence.
- Final code head `a66a25ed079240f3d73ff34ca5c39466f451eb82`,
  [CI run 38161800807](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38161800807),
  checked synthetic PR merge `acb2c84777346dcf99f6285ee0e305c259691ccf`
  against baseline `4bb336575bb1216d3047bd2b294aeecad9fdd65a`.
- Windows compilation: PASS. Both nested-pump regressions: PASS at 18:09:59 UTC.
  Sentinel case returned in 157 ms, following success in 156 ms. Both recorded
  `inspections=1`, `pulses=3`, live controls and disposed form; the sentinel
  object identity assertion passed. This validates the corrected owned-context
  lifetime. The exact native quit-queue mechanism was not directly observed.
- Actual pending form: PASS, exactly one inspection at 18:10:09 UTC, retained
  journal/binding/draft checks and no new installation.
- All 12 `RecoverForm` scenarios: PASS, 18:10:11–18:10:31 UTC. Each reached both
  acknowledgement and terminal-state assertions plus status/result-only checks.
  Successful verification/save consumed only the matching request. This supplies
  the previously missing native evidence for microtask 05.
- Entire DesktopTests: PASS, 3485 checks, 18:04:22 UTC. Entire Test-ProGo: PASS,
  18:07:28 UTC. All prior output-cap/owner scenarios: PASS.
- Full Windows job `114536093148`: FAIL later, in `ConfirmedCleanupFailure`
  combined locked-cleanup assertion, 18:10:33 UTC. Isolated VPN server job
  `114536093329`: PASS.
- Later disposed-verification/proof-storage, HTTP/wizard/setup-recovery tail,
  portable harness and Windows Python relay integration: NOT_CHECKED. Final
  release-content scan, packing/upload: SKIPPED. Local public-source scan is
  separate and PASS, 207 source files and zero release files.
- Screenshots/diagnostics: UPLOADED, artifacts `11710928715` / `11710933727`;
  no visual acceptance or accepted installer is claimed.
- User PC/VPS and installed application: NOT_CHECKED.

Historical 22/31 remains unchanged. Microtasks 05/06 have passed their targeted
native assertions; the complete Windows candidate remains FAIL. No full F-point
or new installer is accepted.

## Next isolated blocker

`ConfirmedCleanupFailure` requires the original hardcoded
`https://previous.example.org`, although the immediately preceding successful
recovery has saved `https://qr.example.org`. `Reset` removes fixture files and
the pending journal, without restoring the persisted origin. This stale fixture
expectation is necessarily false after the successful recovery; it does not
establish a production cleanup defect. Other terms of the combined assertion
are still unknown. The next microtask must establish an isolated prior origin,
different from the attempted target, before taking its snapshot; then freeze
and compare that baseline. It must retain all cleanup/proof/journal/no-replay
conditions and expose named diagnostic results without printing private values
or widening deadlines. The existing cleanup clock includes wrapper/UI/proof
work after the SSH fixture exits, so it is not a direct DeleteAdminWork duration;
a timing defect is not established by this combined assertion.

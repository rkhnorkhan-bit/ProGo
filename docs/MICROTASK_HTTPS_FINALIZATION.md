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
- Windows compilation and both nested-pump fixtures: pending CI.
- Actual pending form, all recovery outcomes and remaining suites: pending CI.
- User PC/VPS and installed application: NOT_CHECKED.

Historical 22/31 remains unchanged. Microtask 05's native recovery outcome is
still NOT_CHECKED until a fresh run reaches and passes its assertions.

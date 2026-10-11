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

## Evidence

- Two independent source reviews: PASS. Whitespace and public-content scan: PASS.
- Windows compilation and native recovery scenarios: pending CI.
- Full Windows and isolated VPN server CI: pending CI.
- User PC/VPS and installed application: NOT_CHECKED.

Historical 22/31 is unchanged. No complete F01–F31 point or new installer is
accepted by this test correction. Other changes and F09/F22 implementation
remain separate microtasks.

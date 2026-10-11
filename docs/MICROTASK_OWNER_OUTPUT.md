# Microtask 04 — bound SSH command output before completion

## Scope and evidence before the change

The previous Windows job failed in `HomeVpnOwnerWaitingTests.Outputs()`,
`oversized/revoke`, after its fixture emitted 1,000,000 characters without a
newline. The outer UI controller started its 20-second deadline before the
command, which also had a 20-second timeout. Its failure hid the inner exception.

Production command output had a separate confirmed gap: `CommandAsync` waited
for PowerShell `Start-Process -RedirectStandardOutput` to finish before
`HomeVpnService` checked the resulting file's 32/64 KiB limit. No early output
limit existed. The exact reason for the previous 20-second wait is not proven;
in particular, a Windows PowerShell line-buffer defect is not claimed.

## Change

Only the command wrapper changes. A `System.Diagnostics.Process` inherits the
root's real console input and stderr; stdout alone is read as raw bytes in 4096
byte blocks and written to the private result file. Storage is capped at 32 KiB
for setup/list/ordinary owner commands and 64 KiB for issuance/HTTPS framing.
Overflow exits the root nonzero without waiting for the producer. The existing
Windows Job owner then settles the root and descendants before reporting a
result. Normal EOF still waits for the child's actual exit code. Copy commands,
password/host-key prompts, remote uncertainty and recovery semantics remain.

Native regressions cover both exact limits with UTF-8 byte equality, one-byte
overflow with a live descendant, real console stdin/stderr, process settlement
and a responsive UI. Existing million-character revoke/repair/share scenarios
now require a non-timeout refusal, local process termination and private work
cleanup. Their command timeout is 10 seconds, leaving the unchanged 20-second
controller room for owned-tree settlement and private cleanup. Controller
failure reports the scenario and inner error type/message.

## Validation

- Two independent source reviews, whitespace and public-source scan: PASS.
- Windows compilation and all four raw boundary cases: PASS. Both exact limits retained all UTF-8 bytes, and both one-byte overflows refused the output and settled a live descendant. `GetConsoleMode` confirmed real console stdin/stderr handles.
- All three million-character ordinary owner scenarios: PASS before their SSH deadline, including the prior `oversized/revoke` failure. Accepted cancellation and the final owner checks also passed.
- Full desktop suite: PASS, 3485 checks. The complete `Test-ProGo.ps1` step passed, including Instance/Maintenance/Shutdown/UpdatePackage.
- Full Windows job: FAIL later in `HomeVpnShareRecoveryTests.RecoverForm("complete", false, false)`, with `HTTPS native fixture failed`. The inner exception is hidden by the existing fixture wrapper; an HTTP protocol defect is not established.
- Remaining HTTPS/wizard checks, portable harness and Windows Python relay integration: NOT_CHECKED after that failure. Final public release-content scan, packing and release upload: SKIPPED. Local public-source scan separately passed.
- Isolated VPN server CI: PASS. Screenshots and diagnostics were uploaded; screenshot upload does not establish visual acceptance.
- User PC/VPS and installed application: NOT_CHECKED, deferred.

Verified code: `f3ad1c4458d86b4a96f6807ed94be75fd7ae8b43` in
[draft PR #88](https://github.com/rkhnorkhan-bit/ProGo/pull/88).
[CI run 38156158147](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38156158147)
checked synthetic PR merge `27fba56ac9c2d9b84326deda8ca84d12ff18acbd`, including
that head and baseline `4bb336575bb1216d3047bd2b294aeecad9fdd65a`.
Windows job `114519455884`; server job `114519456030`. Raw checks passed at
16:47:00–16:47:02 UTC; large-output and final owner checks at
16:47:06–16:47:07 UTC. Desktop finished at 16:42:37 UTC and `Test-ProGo` at
16:45:03 UTC. The later HTTPS failure was reported at 16:47:52 UTC.

The next separate blocker is successful completion/publication of the HTTPS
recovery configure form and its fixture's completion predicate. The two earlier
complete-receipt scenarios with injected verification/save errors passed;
`ConfirmedCleanupFailure`, `DisposedVerification`, `ProofAndStorage` and the
later HTTP/wizard/portable suites were not reached. No further correction is
included in this checkpoint.

This is one bounded correction under the approved plan. It does not close F20,
F31 or another complete plan point. Historical 22/31 remains unchanged. The
nearest independent candidates for additional Code/Dev completion are F09's
daily vault contract and F22's full repair/portable transfer; their remaining
criteria are not replaced by this output fix or by partial CI success.

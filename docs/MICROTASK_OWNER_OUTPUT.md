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
- Windows compilation and native checks: pending CI.
- Full Windows and isolated VPN server CI: pending CI.
- User PC/VPS and installed application: NOT_CHECKED, deferred.

This is one bounded correction under the approved plan. It does not close F20,
F31 or another complete plan point. Historical 22/31 remains unchanged. The
nearest independent candidates for additional Code/Dev completion are F09's
daily vault contract and F22's full repair/portable transfer; their remaining
criteria are not replaced by this output fix or by partial CI success.

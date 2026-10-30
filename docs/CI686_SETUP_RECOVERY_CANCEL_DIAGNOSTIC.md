# CI686 owned setup recovery Cancel/Retry: bounded distinguishing diagnostic

Source 5a6a23d33e784d929a370dd4a0daded4dc64cd6b, tested merge
54326758a24ca31f53d1898f9d3f07826213a7a8, run40147067305.
Full Windows job121030565409 log independently read: repository hygiene/build SUCCESS,
relay integration FAILURE. AuxiliaryRevalidation24 and all prior installer suites PASS;
Desktop6394/Instance30 PASS. Pack/public-content/upload steps SKIPPED. Ubuntu SUCCESS.
These are scoped results, not an overall green CI or target-PC acceptance.

## Evidence and limit

OwnedSetupRecovery status-pending completed. Cancel-retry reached actual modal owner,
copy-worker/lease and duplicate-admission checks, then the held-body cancellation/Stop
assert PASS. After release no following assertion PASS appeared. Safe original stack:
ModalWizardCase IL155 -> OwnedSetupRecovery IL1624 -> Run IL471 -> Main IL4958.
The next conditional branch's composite assertion checks six original conditions:
typed preparation cancellation, recovery warning, Retry enabled, wizard modal,
zero transport calls, lease released. The log has no individual values. IL alone is
not a source-level invariant map. Exact violated condition and causal root remain UNKNOWN.
No evidence links the failure to the separate installer-only previous diff; recovery
runtime and fixture were unchanged by that diff. No runtime fix is justified.

## Single test-only experiment

Only that assertion is instrumented. Its exact original short-circuit expression,
order and message remain, stored once as cancelReady; the same check throws on false.
Only on false collect all six named boolean observations/violated list (observations
follow the original sample and may differ under a race). Extra fixed state: operation,
Stop/import/setup-read/copy-completion task enums, phase/counts, UI disposal/handle/
modal/owner, busy/admin/cleanup/owned resources/core presence/release, failure type,
publication count. One atomic fixture observation immediately before the original
ct.ThrowIfCancellationRequested records token state (-1 not reached,0 false,1 true)
without changing the hold/deadline/cancellation sequence. Max6 actual sanitized managed stack method/IL entries, no messages.
Accepted request ID equality/result absence/journal equality/private input equality
and unrelated-process liveness are booleans only. Diagnostic collection failure emits
only incomplete/type; it never turns failure into PASS or masks the original check.
No secret, raw ID/PID/path/argv/journal/credential body is printed.

All waits, ShowDialog, Cancel/Retry sequence, ownership/no replay, Stop/Dispose,
accepted request, production code and existing finally cleanup are unchanged. No new
retry or extra operation is admitted. Diagnostic source is not a runtime fix.
Local static/review/parity/signatures/secrets/public checks separate from Windows:
new diagnostic branch/native execution NOT_RUN until the one exact-SHA CI.

Next: SAME new CI final logs and named conditions; if non-reproduced retain UNKNOWN,
do not declare cancellation fixed. Pending installed publication/new functions stay
blocked until verified PASS. Repair20s/sharing/Python/rebind incidents remain UNKNOWN;
PID and Marshal fixture fixes retain scoped verified PASS. BLOCKED_TRANSFER/no retry.
No live install/service/network/security/credential/release/upload action.

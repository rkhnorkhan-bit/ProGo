# CI669 partial-admission fixture diagnosis

Source320c598521993220ae5c057301f2ebdc9a340d3d, tested merge
107014f558643c211775afe1700b682f052d4c9c; run40108281318/job120914440601.
Full final Windows log independently read. Maintenance15 including copy-fail/partial-copy,
Stage8/Backup8/SelfCheck9/Payload9, Desktop6388/Instance30/Maintenance126/Shutdown47 PASS.
Rollback complete PASS (1/8), then InstallerRollbackTests.ps1:14 fails the original
real written/partial bytes retained in admission assertion. Relay/later suites NOT_RUN.

Separate fixture defect: nested write mock resolves `$mode` dynamically to owned setter's
FileMode.Open. Its partial branch does not run; no partial write exception is created,
writeFailed stays false and the first original conjunct fails. This does not prove a
runtime admission/hash loss. Setter still captures hash through exclusive held target
handle in finally before disposal, while outer test reads file hash after settlement.

Minimal test-only fix freezes scenario in a fixture-state hashtable; write mock uses its
Scenario, not shadowed mode. Original compound assertion and error-message check retained.
Added check proves exactly one injected exception and actual one-byte value42 before
rollback; original hash equality, real exclusive handle, exact restored bytes, foreign/
private preservation, snapshot/absence guards remain. No runtime/API/timeouts altered.
New exact Windows Rollback8/regressions NOT_RUN locally; require new CI logs, no inherited
PASS. Journal/retention deferred. CI662 root UNKNOWN; privatecandidate BLOCKED_TRANSFER.
Readiness implementation58–80/100, verified22/100, change0.

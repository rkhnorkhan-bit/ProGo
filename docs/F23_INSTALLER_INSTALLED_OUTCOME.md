# F23 installed terminal outcome (bounded success path)

CI674 independently verified complete Windows15475/Ubuntu578-line logs, both SUCCESS.
Source44bcf31080c685701e9eb8598fa25df7c3d5b57a, merge99137658839fa18f6440df840db94470bf9792ac,
run40117901265 Windows120942960299/Ubuntu120942960086. RestartGate8/Journal8/Rollback8/
Maintenance15/Payload9/Stage8/Backup8/SelfCheck9/Desktop6388/Instance30/HomeVPN4816/
Portable91/prior suites PASS. Pack runner-only/uploads-prune SKIPPED. CI662/670 Python
and CI671 rebind roots UNKNOWN; safe diagnostics retained, non-reproduction not fixes.

## Acceptance and boundary

Publish installed outcome only after original self-check and shortcuts, full snapshot
material/manifest verification, exact new runtime hashes and complete owned-write/admission
coverage. Persist new pending outcome with CreateNew/WriteThrough/Flush(true), exact same
handle readback, then validate all evidence and atomically Move to installed.json without
overwriting. Even a failure after writing/flushing all bytes leaves only pending, not a
false installed result before current-process rollback. Original install error/no success/
Launch remains, verified rollback/private/foreign guards unchanged. Stage/finally unchanged.

Success outcome binds normalized install-root SHA256 identity (no raw path), journal GUID,
exact whitelisted runtime inventory, snapshot name/hash and all ordered admission hashes.
Runtime preflight and terminal reader share the same unchanged list of payload names;
Payload9 loader explicitly loads that pure helper, original assertions retained.
Restart gate skips only an installed outcome with exact identity, known schema/phase,
complete unique whitelist, verified saved snapshot bytes/manifest and every record's digest,
sequence and inventory/entry identity. Missing/unknown/corrupt/moved/extra entries fail closed.
Private settings/vault/keys are never admitted or inspected by outcome validation.

A historical terminal attests prior completion; recognition reads its evidence/snapshot,
not old historical live runtime hashes (later valid updates may replace them). Any new
install still captures/verifies current runtime and enforces original exclusive ownership
checks immediately before mutation. Terminal does NOT authorize restoration or overwrite
of a file changed after new admission. A new pending journal still blocks another attempt.
Older intent-only journals have no invented completion and remain pending. No migration,
automatic recovery, rollback terminal or journal deletion is added in this bounded slice.

## Tests, not yet run on exact Windows candidate

Outcome10: complete/fresh, persist-fail/short/after-write-fail, foreign-runtime,
snapshot-corrupt/journal-corrupt, wrong identity/unknown phase. Actual own streams/files,
all inventory writes; exact before/after runtime/private parity, no false installed file,
correct restart skip versus refusal, new admission without deleting old terminal;
verified rollback after persistence error stays pending. Maintenance15 retained plus two
actual installer-tail terminal failure cases: before write and after full durable write;
exact previous bytes restored, no installed terminal, no success/Launch. Maintenance17.
RestartGate8/Journal8/Rollback8 and existing helper assertions/timeouts preserved.
Only new own temporary fixture paths and mocks; no real install/services/ACL/network/
security/credentials/owner-PC/VPS actions. Local static is not Windows execution.

Mandatory remainder: durable rollback/failed outcome and safe terminal recognition for it;
post-crash partial ownership proof/phase transitions, bounded interrupted recovery/no replay,
retention preserving unresolved evidence, auxiliary-state/shortcut transaction, remaining
F23/PGA04/updater fallback/package/owner acceptance gates. Full crash recovery/installer
production readiness NOT claimed. Privatecandidate BLOCKED_TRANSFER, no delivery retry.
Readiness58–80/22,+0, weights unchanged.

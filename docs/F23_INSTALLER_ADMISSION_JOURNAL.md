# F23 bounded durable admission-before-write

Acceptance: current-session own intent must be persisted with Flush(true), read back
exactly, and pinned before the corresponding runtime leaf is created or modified.
Unknown, missing, invalid or tampered ACTIVE journal fails closed before that write.
No claim of complete crash recovery, power-loss atomicity of directory metadata, or
re-entry admission across installer process restarts is made in this bounded slice.

CI672 independently verified full Windows15457/Ubuntu578-line logs, both SUCCESS.
Sourceefc4cc91c19baff6dfcbdc25ef666df13362a485, merge806d4512dd5355542806e1ad934f8c50e4181e45,
run40112760540 Windows120927737675/Ubuntu120927737758. Rollback8/maintenance15/Payload9/
Stage8/Backup8/SelfCheck9/Desktop6388/Instance30/Maintenance126/Shutdown47/HomeVPN4816/
Portable91 and prior suites PASS. Pack runner-only, uploads/prune SKIPPED. Python and
queued Stop passed; CI662/670 Python and CI671 owner/rebind roots remain UNKNOWN.
Safe diagnostics retained; successful non-reproduction is not an incident fix.

## Production contract

Lazy session journal created under own installer-journal-GUID child of guarded backups,
after full snapshot+original-runtime verification. Each immutable admit-NNNNNN.json embeds
Schema1, phase admitted, sequence, exact frozen inventory/snapshot manifest hash and exact
relative leaf old/new hashes/absence. No settings/vault/key material, secret or user config
included; only existing installer runtime inventory. Fresh install has null snapshot hash
and explicit absence inventory. Unknown entries are not admitted. No file is overwritten.

CreateNew + WriteThrough + Flush(true); exact byte count/content read back from SAME held
handle. Hold admission with FileShare.Read through runtime creation/write/partial-hash
capture; write/delete contenders refused by OS share contract. Runtime stream still
exclusive; actual full/partial hash is captured in existing finally before disposal.
All prior runtime ownership guards and snapshot validation unchanged. Unconfirmed intent
creates no runtime target or memory write admission. Failed intent may leave its own
incomplete journal record; it is retained and a subsequent current-session admission
refuses unknown/unverified records. Unknown schema/inventory or record tampering also
refuses. Intent is not proof of partial-byte ownership after a crash.

No journal cleanup/delete is added; confirmed snapshot and journal are retained on
success/failure/verified rollback. Original install error still rethrown, no false
Install OK or Launch after rollback. Current-process rollback uses existing exact
captured hash + full snapshot check; it does not trust an intent to overwrite foreign
changes. Stage cleanup and maintenance Stop/finally unchanged.

## Eight own temporary fixture cases

existing, fresh, persist-fail, short-write, unknown, invalid, tampered, partial.
Mock only persistence/write seams with real own streams/hashes/paths. Prove journal
write sees unchanged old bytes/absence and no mutation; runtime write sees one confirmed
record and held intent rejects concurrent writer; exact owned partial byte42 and hash;
rollback old bytes/absence; intent retained; settings/vault/key refs/foreign bytes intact.
No actual install, services, ACL/security/network/credentials or owner PC/VPS changes.
Maintenance15/Rollback8 remain registered unchanged; new Journal8 added once to harness.
Local static/review/signatures/docs3/JSON/diff/secrets/public-content checks are separate
from actual Windows parser/native execution, which is NOT_RUN until exact new CI logs.

Mandatory next slices: restart discovery and validation of pending journals before new
admission; durable post-write outcome/partial ownership proof and phase transitions;
bounded interrupted recovery with no replay/foreign overwrite; terminal-state recording,
retention and auxiliary-state/shortcut transaction. These remain OPEN and must gate any
production installer acceptance. This slice does not make the entire installer ready.
Privatecandidate BLOCKED_TRANSFER; no transfer retry/release/uploads/install/live actions.
Fixed readiness58–80/22,+0; no scoring increase for new unexecuted code/tests.

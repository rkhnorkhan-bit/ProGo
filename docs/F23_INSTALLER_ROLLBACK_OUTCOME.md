# F23 verified runtime rollback terminal (bounded path)

CI675 complete final Windows15488/Ubuntu578-line logs independently read, both SUCCESS.
Sourcea838d021fc631ca40bb9ec521f5b4f6409b68f57, merge5342f65d48e99bc85440e8356cf8d9e60f20ca4a,
run40120509957 Windows120950820808/Ubuntu120950821010. Outcome10/Maintenance17/RestartGate8/
Journal8/Rollback8/Payload9/Stage8/Backup8/SelfCheck9/Desktop6388/Instance30/HomeVPN4816/
Portable91/prior suites PASS. Pack runner-only/uploads-prune SKIPPED. CI662/670 Python
and CI671 rebind roots UNKNOWN; diagnostics retained, non-reproduction not fixes.

## Acceptance / production path

After current-process rollback and full actual original-byte/absence proof, verify journal,
snapshot material/manifest and one-to-one known own write/admission coverage. Persist only
rolled-back / RuntimeState original-verified with exact root identity, journal GUID,
runtime-only inventory, snapshot identity, ordered admission digests and captured owned
write hashes. Existing rollback still refuses foreign changes and verifies real old bytes.
Intent alone cannot create this terminal. Partial owned writes use their real captured hash.

CreateNew/WriteThrough/Flush(true), same-handle readback into rollback.pending.json, validate
proof then atomic Move without overwrite to rolled-back.json. Separate writer seam tests
persistence errors before/after flush; unconfirmed terminal stays pending. Original install
ErrorRecord is ALWAYS thrown even after terminal success; no Install OK or Launch. A terminal
persistence error is caught separately and never masks that original error or falsely
claims installation success. Safe rollbackTerminalVerified boolean diagnostic only.

Shared reader keeps installed requirements (complete full inventory admissions) and adds
rolled-back only with non-empty unique matching subset, complete original inventory,
known RuntimeState and all ownership/admission hashes. Restart gate permits a new explicit
attempt only on verified installed OR rolled-back evidence. It does not restore, repeat
invite/transport actions or delete history. Historical terminal is completion evidence,
not a request to overwrite later live files; each new attempt still captures and verifies
current runtime under unchanged exclusive ownership guards. Snapshot evidence retained.
Runtime-only rollback does NOT claim auxiliary-state/shortcut rollback complete.

Unknown extra journal files, missing/unconfirmed admissions, incomplete snapshot/identity/
ownership, bad old bytes or failed terminal persistence remain fail-closed. In particular
unfinished installed outcome pending files are not deleted/reinterpreted as rollback
success: that recovery/phase-transition case remains OPEN. No migration invents completion
for old intent-only journals. Empty/no-write failed attempts remain conservatively pending
when evidence is incomplete. No failed-install success terminal is added.

## Tests (new exact Windows NOT_RUN locally)

RollbackOutcome12: existing/fresh/partial/unrestored/foreign/snapshot/ownership/record-corrupt/
persist-fail/short/after-write-fail/identity. Actual own streams, partial bytes, snapshot,
original-byte/absence proof; terminal itself never changes runtime; correct restart
recognition/refusal and new explicit admission retains terminal; no installed marker,
private settings/vault/key refs/foreign bytes preserved. Unrestored/foreign state not fixed.
Maintenance17 retained plus2 actual installer-tail rollback terminal persistence cases
(Maintenance19), preserving precise original self-check refusal, actual old bytes and no
Launch. Existing copy-fail/partial-copy/self-check-fail/shortcut-fail additionally require
rolled-back marker and accepted restart gate. All original assertions/timeouts remain.
Installed Outcome10 and all prior suites still registered; no mocks replace real rollback.

Mandatory remaining: unfinished terminal/outcome phase reconciliation with independently
verified identity/ownership, post-crash partial ownership proof and bounded interrupted
recovery/no replay/foreign overwrite; retention protecting unresolved evidence; auxiliary-
state/shortcut transaction; remaining F23/PGA04/fallback/package/owner acceptance gates.
No full crash recovery or installer production readiness claimed. Only code and own temp
fixtures/mock OS boundaries; no install/services/ACL/security/network/credentials/PC/VPS
changes, release/uploads. BLOCKED_TRANSFER unchanged, no retry. Readiness58–80/22,+0.

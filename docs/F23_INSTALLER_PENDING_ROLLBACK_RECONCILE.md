# F23 bounded unfinished rollback-outcome reconciliation

CI677 full Windows15503/Ubuntu578-line logs independently read, both SUCCESS.
Source227d83774e82a101c2c2c67dee83a4120d1f2be6, tested merge
b6711df3c075c1fe6842be8c9922c4bc6e77d60f, run40124878568 Windows120964057190/Ubuntu120964057033.
SOCKS51/RollbackOutcome12/Maintenance19/Outcome10/RestartGate8/Journal8/Rollback8/
Payload9/Stage8/Backup8/SelfCheck9/Desktop6388/Instance30/HomeVPN4816/Portable91/prior
suites PASS. Pack runner-only/uploads-prune SKIPPED. CI676 sharing failure did not recur:
root UNKNOWN, not a runtime fix. Prior Python/rebind incidents UNKNOWN, diagnostics kept.

## Acceptance / minimal production path

Only complete publication of a fully saved rollback.pending.json, not an intent or
installed pending phase. A new explicit installer invocation acquires maintenance then
Resolve-ProGoInstallerPendingRollback before original pending gate and Stop/staging/write.
Read guarded direct journals, accept historical verified terminals unchanged; validate all
remaining entries with existing rollback terminal verifier (snapshot/identity/ownership/
admissions/strict namespace and complete runtime-only whitelist). Reconstruct backup solely
from saved proof, verify actual snapshot material and CURRENT exact old bytes/absence.
Collect candidates before any metadata change; reject unknown/corrupt/conflicting entries
and more than one pending rollback. Recheck snapshot, actual old bytes/absence and saved
proof immediately before atomic no-overwrite Move to rolled-back.json.

This never restores or deletes runtime bytes, never retries an external action, and never
turns intent into post-crash ownership. Pending installed/outcome phases, intent-only or
partial/invalid JSON stay closed. Foreign changed old bytes or appeared files in prior
absence stay untouched and blocked. Direct AssertNoPending/NewJournal retain prior strict
semantics; only a NEW production installer invocation performs this reconciliation. The
original failed invocation remains failed, and no Launch follows its rollback. Historical
terminal recognition still does not rewrite later live files. No claim against every
possible hostile external pathname race/power-loss directory metadata behavior is added.

## Reconcile13 / exact Windows execution pending

existing/fresh/partial/corrupt/snapshot/foreign/absence/identity/ownership/conflict/
intent-only/pending-installed/multiple. Build actual durable pending with real owned write,
actual rollback and injected error AFTER outcome Flush(true), then use actual installer
body as a new attempt with fake maintenance/Stop. No old in-memory admission is passed.
Three proven states alone reach mock Stop; it throws a sentinel so no actual setup follows.
All others refuse before Stop; lease disposed once, no success/Launch. Check atomic rename
retains exact evidence bytes; all runtime/private/settings/vault/key refs/foreign/snapshot/
other journal bytes and total file count unchanged. Multiple case has two independently
valid saved pending proofs and must not publish either. Only own temporary fixtures cleaned.
Existing Outcome10/RollbackOutcome12/Maintenance19 and all assertions/timeouts unchanged.
Local review/static is separate from actual Windows parser/native execution, NOT_RUN here.

Mandatory remaining: other unfinished installed/rollback commit phases and incomplete
admission reconciliation; independent durable post-crash partial-byte ownership proof and
bounded runtime recovery/no replay/foreign overwrite; retention protecting unresolved
material; auxiliary-state/shortcut transaction; remaining F23/PGA04/fallback/package/owner
acceptance. No full crash recovery/production installer acceptance claimed.
No actual install/services/ACL/security/network/credentials/PC/VPS/release/uploads actions.
Privatecandidate BLOCKED_TRANSFER, no retry. Readiness58–80/22,+0, weights unchanged.

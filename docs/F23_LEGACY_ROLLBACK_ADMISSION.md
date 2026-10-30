# F23 read-only Legacy rollback prerequisite

Acceptance: classify actual Legacy state only after verified durable Menu result and full current
snapshot/runtime/admission/receipt/identity/trusted-location/native-origin/bytes-or-absence evidence.
No deletion, restoration, source guessing, receipt rewrite, success/Launch or full rollback. Invalid,
unknown, incomplete, foreign or changed states return null. All old mutation/error/gating unchanged.

GetLegacyRollbackAdmission first reconstructs and verifies Menu result. The pending receipt and fixed
Menu result are bounded (1 MiB), strict UTF8, read-pinned. Full result text and receipt hash must match
the independent current proof. It then repeats full proof and result validation, including existing
fixed-schema/native Legacy verifier. Its observation binds exact receipt/Menu admission/result hashes,
Legacy location identity supplied by caller, origin/prior/current hashes, owned hash and replacement
linkage. No path/permission comes from a receipt. Only trusted fixed locations are consulted.

Preserved Legacy returns preserve-only. Valid removed Legacy returns removed-needs-source with
RestorationRequired true. Both return false MutationAllowed/RestorationAllowed/FullRollback/
InstallSucceeded/VerifiedRestoreSourceAvailable. The last flag means no verified restore-source
contract has been provided, not a claim that arbitrary files elsewhere do not exist. Current durable
LegacyInstallEvidence only retains hashes/native origin/linkage; original bytes are discarded by
its constructor and not stored in this receipt. A hash or valid intent cannot reconstruct original
shortcut bytes. Therefore the removed path remains blocked for restoration, even though its actual
absence and provenance can be truthfully classified. This is a prerequisite, not full Legacy rollback.

New LegacyAdmission19 fixtures use real existing payload/stage/backup/runtime writes, native shortcut
migration, durable receipt/Menu admission/owned rollback/result in own GUID directories. Cases:
absent-preserved, custom-preserved, removed, post-startup, missing/tampered Menu result, missing/wrong
origin, same-byte replacement with copied creation and different native object, Legacy appeared/
changed, snapshot/runtime changed, wrong location, busy receipt, corrupt admission, unknown receipt
field, wrong replacement and foreign admission. Three reads must yield exact stable evidence only
for four valid cases, never mutate or publish; full case inventory/count/hash and exclusive file
reopen prove private/foreign/native/metadata preservation and cleanup. Root/GUID/temp parent/reparse
guards restrict fixture cleanup. No live shortcuts/processes/services or user resources.

Local static/parity/aggregate12 is not Windows proof; new Legacy19/full30 NOT_RUN until exact source CI.
CI704 baseline integration19/result19/admission19/owned18/Menu32/Publication6/full29 PASS does not apply
to new code. All existing29 calls/tests/deadlines/strict aggregate/workflow/upload guards unchanged.

Next bounded dependency: verified original Legacy byte capture before existing migration deletion,
with native handle continuity, identity/origin/hash and durable receipt/source linkage. Old hash-only
receipts cannot be silently upgraded from untrusted or external replacement files. Then separate
owned restore and durable mutation/crash/result/error integration, followed by full auxiliary outcome.
Runtime recovery/retention/PGA04/standalone-updater fallback/immutable identity/package/owner gates,
remaining F31/portability/UI/signing and full approved commercial scope remain in the main registry.
UNKNOWN incidents and BLOCKED_TRANSFER remain open. No live install/shortcuts/services/network/ACL/
security/credentials/VPS, release/uploads or delivery retry in this stage.

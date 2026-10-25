# F23 verified pending-installed metadata publication

Baseline CI687 source a9f8b8feb2cd74749a1503b71a71974441f96b86, tested merge
774c0c6b5ee6d5d1f04fae6e08191d022219f346. Full Windows15612/Ubuntu578-line
logs independently read: both SUCCESS. Auxiliary24, all previous installer suites,
Desktop6394/Instance30/HomeVPN4816/Portable91 PASS; setup recovery cancel-retry
passed without diagnostic violation. CI686 causal root UNKNOWN, not a runtime fix.
Pack runner-only; uploads/prune SKIPPED. No owner-PC acceptance inferred.

## Acceptance and production boundary

Resolve-ProGoInstallerPendingInstalled runs under the existing installer maintenance
lease, before Stop and new runtime mutation. Scan all installer journals before any
publication: historical verified terminals skip; valid rollback-pending retains its
existing separate resolver. Unknown/conflicting/incomplete states cannot pass the
auxiliary reader, and multiple pending candidates refuse. No intent triggers runtime
restoration, shortcut creation/deletion or consumption/replay.

Exactly one fully completed installed pending candidate must pass the existing strict
auxiliary verifier twice, with identical pending metadata hash. Each pass verifies
snapshot material, install identity, complete runtime inventory/current bytes, linked
admissions and ownership plus Startup/Menu/Legacy receipts against independently
supplied platform locations. Unknown sibling pending states fail closed. Receipt
paths never select target/helper locations; the installer supplies the validated
payload's own shortcut helper explicitly.

The only mutation is File.Move(outcome.pending.json, installed.json), same directory,
atomic no-overwrite. Sharing/destination failures propagate. Successful metadata
publication permits the existing no-pending gate and a new attempt; it does not claim
the new attempt succeeded or Launch. Existing rollback failures retain their original
error/no Launch behavior. Runtime/settings/vault/keys/external objects are untouched.

Validation is repeated observation under the cooperative maintenance owner, not an
atomic lock of every independently mutable runtime/auxiliary object or authenticity
against a hostile journal writer. No blanket race-proof or whole F23 PASS is claimed.
Runtime crash recovery, retention and auxiliary rollback remain separate mandatory
work. No automatic repair of external changes is introduced.

## Regression scope

PendingPublication25 uses actual runtime owned writes/snapshot/stage and actual
shortcut adapter only in new temporary Programs/Startup roots. Real durable outcome
writer is interrupted after Flush, before publication; fixture phase markers are not
claimed actual self-check execution. Real installer body and new resolver run with
only independent fixture-location substitution and maintenance/Stop/Launch mocks.

Three success cases (created/custom-preserved/legacy-removed) require exact metadata
hash preserved by rename, terminal proof, no-pending gate and idempotent repeat.
Twenty-two refusal cases cover missing/invalid/incomplete/type/identity/location/
ownership data, external shortcut/runtime/snapshot/admission changes, terminal or
sibling conflict, invalid/oversized JSON, active writer and native rename sharing
failure. All cases require expected Stop admission count, maintenance Dispose once,
no Launch, unchanged file inventory/hash except the single expected rename, all
private/foreign bytes intact. Own locks and stage close finally; cleanup rejects
reparse roots/descendants. Existing assertions and timeouts unchanged.

New Windows compilation/parser/native tests NOT_RUN locally (environment lacks them).
Static/review/signatures/parity/secrets/public checks do not replace exact CI.
CI686/repair20s/sharing/Python/rebind incidents UNKNOWN with diagnostics retained;
PID/Marshal fixes scoped verified PASS. BLOCKED_TRANSFER, no delivery retry.
No live install/service/network/ACL/security/credential/release/upload action.

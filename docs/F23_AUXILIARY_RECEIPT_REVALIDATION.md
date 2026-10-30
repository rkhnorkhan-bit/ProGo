# F23 auxiliary durable receipt revalidation prerequisite

Baseline CI685 source 0b662b38bba5e799755f599ad5ab344a0ab067aa, tested merge
69fc0c87dac0d43d71d1df0b698c990e50796ce8. Both full final logs independently read:
Windows121023215009 (15587 lines), Ubuntu121023215090 (578 lines), both SUCCESS.
Legacy14/Menu14/Startup13/PendingProof11/Reconcile13/RollbackOutcome12/Maintenance19,
other installer suites, SOCKS51/Desktop6394/Instance30/HomeVPN4816/Portable91 PASS.
Pack runner-only; all uploads/prune SKIPPED. No target PC acceptance implied.

## Acceptance and boundary

New Test-ProGoInstallerPendingAuxiliaryProof is a production read-only prerequisite,
not a pending publisher and not wired to automatic recovery. Caller supplies expected
install root and trusted Programs/Startup locations (platform defaults in production;
explicit new temporary locations in tests). No location comes from receipt fields.
Existing installed-proof reader first and last verifies exact runtime inventory,
linked admissions, snapshot, identity, owned hashes, phase order and conflicts.
Sibling unknown/pending/conflicting journals block; verified historical terminal
journals use existing readers. No runtime restoration or retry permission is granted.

The selected outcome.pending.json is pinned FileShare.Read against write/deletion,
size1..1048576, strict UTF8 decoding, disposed in finally. Typed receipts accept only
known exact field names, integral schema1, actual booleans, strings/null and uppercase
SHA256 identities/hashes with consistent presence. No arbitrary type activation:
only three fixed internal receipt DTO types have private empty deserialization
constructors. Missing/unknown/ill-typed data returns false without mutation.
Existing Startup/Menu/Legacy native readonly verifiers recheck actual bytes/absence,
independently derived locations, target semantics and legacy origin/replacement.
Removed legacy replacement hash must also match the durable Menu receipt.

Positive evidence remains observational: it is not a cryptographic authenticity
claim about a hostile journal and does not lock all auxiliary/runtime objects across
a future publication. No new current installed-state claim or automatic pending
rename is added. Publication-time ownership/phase/final revalidation and concurrency
boundary remain a separate mandatory slice. Crash runtime recovery, retention and
auxiliary rollback remain separate; settings/vault/keys/external changes untouched.

## Targeted regression

AuxiliaryRevalidation24 uses real existing owned runtime writer/stage/snapshot and
actual shortcut adapter only in new temporary Programs/Startup roots. It interrupts
the real outcome writer after durable receipt flush and before publication, avoiding
live installation or self-check claims. Three positive variants: newly created,
custom-preserved, legacy-removed. Refusals: missing receipt/schema/unknown field/
boolean type/install identity/three location identities/disposition, changed startup/
menu/new legacy/runtime/snapshot/admission, same-journal conflict, sibling pending,
wrong independently trusted location, invalid JSON, oversized receipt, active writer.
Every case asserts no retry permission, unchanged full fixture file inventory/hashes,
private/foreign bytes, own-stage cleanup. Finally closes own lock and refuses recursive
cleanup through a reparse root/descendant. Original tests/assertions/timeouts unchanged.

Local static/review/signatures/parity/secrets/public checks are distinct from Windows
execution. New suite/compilation/native NOT_RUN until exact new CI completes.
Repair20s/sharing/Python/rebind incidents UNKNOWN; PID and SizeOf fixtures scoped PASS
on their verified Windows suites. Private candidate BLOCKED_TRANSFER, no retry.
No score increase, install/service/security/network/credential/release/upload action.

# F23 durable Startup-only observed result prerequisite

Baseline CI691 source330bd6dd31a35f01690a52dae4371a2e1538ac17, tested merge
7c0cce30dc5711e51dd674a17c36875c8aa2fef6. Full Windows15729/Ubuntu578 lines
independently read: both SUCCESS; OwnedStartup14/Admission25/Publication25/Auxiliary24
and prior installer/Desktop6394/Instance30/HomeVPN4816/Portable91 PASS. Pack runner-only,
uploads/prune SKIPPED. No full F23, owner-PC or production PASS implied.

## Acceptance and exact scope

Persist only observed original Startup state after actual current-state verification.
This is not a full auxiliary rollback outcome, applied-command receipt, install success,
permission to mutate or permission to retry. It does not infer who removed a link.
No Startup operation is repeated by the new writer/reader.

Get-ProGoInstallerStartupResultProof uses unchanged strict pending proof to verify
current new runtime/full inventory, snapshot, linked admissions, identity and ownership.
Original receipt is pinned read-only/bounded, strict DTOs read all three receipts.
New native readonly verifier proves Startup original absence for owned-created with
origin/hash/identity, or actual unchanged preserved state. Actual Menu/Legacy receipts
remain verified at independent caller/platform locations. Unknown/conflicting sibling
journals/results reject; journal fields never select a helper or operation target.

Complete-ProGoInstallerStartupResult writes one deterministic own-name sidecar under
verified backups, using CreateNew/WriteThrough/Flush(true), exact readback and repeat
physical proof. Fields bind install identity/journal/receipt hash/expected Startup
location/original origin/before presence/hash; phase startup-original-observed, runtime
new-verified, FullRollback=false/InstallSucceeded=false/MutationAllowed=false.
Test-ProGoInstallerStartupResult independently recomputes proof and compares bounded
exact saved evidence; flags alone or a file's presence are never sufficient.

Snapshot/admissions/identity/current auxiliary or runtime change, external Startup
reappearance/replacement, missing origin, malformed/short/conflicting/tampered result
cannot verify. Failed write/readback propagates, no false success; partial own artifacts
remain for diagnosis. Existing sidecar is never overwritten or deleted. No existing
snapshot/runtime/receipt/private bytes are modified.

Presence of any partial/unknown installer-startup-outcome prefix blocks existing
installed/rollback reconciliation, auxiliary preflight and no-pending/new-journal gate.
It cannot be confused with a completed runtime/auxiliary terminal. Existing pending
proof readers and original assertions remain unchanged. Before any sidecar exists,
a failed observational attempt does not invalidate an otherwise valid existing installed
completion proof; the new writer itself never publishes that proof.

## Regression

StartupResult13: created, preserved custom/absence, not rolled back, foreign Startup/
Menu, corrupt snapshot, changed runtime, persist failure, short write, preexisting
conflict, tampered FullRollback, missing origin. Actual existing owned rollback is
fixture setup, not reimplemented. Durable writer interrupted at existing receipt
boundary; synthetic phase markers are not live install/self-check acceptance.
Assert physical reader verification, exact identity/receipt and no full flags, rejection
without overwrite, original runtime/private/foreign file inventory preserved, no
implicit publication/retry, own stream exclusive reopen/cleanup. All fixtures in new
GUID temp roots; existing tests/assertions/timeouts unchanged.

Local static/review/signatures/parity/secrets/public checks are not Windows execution.
New native/result13/compiler/parser/regressions NOT_RUN until exact new CI.

## Remaining boundary

Durable pre-mutation auxiliary admission/crash settlement and complete auxiliary outcome
must still precede integration with original installer failure path. Current error/no
Launch/Stop/finally/cleanup are unchanged. Startup-only observation deliberately keeps
new attempts blocked; no automatic recovery from intent. Menu/Legacy rollback, retention
and general runtime recovery remain separate mandatory work. CI686/repair20s/sharing/
Python/rebind causes UNKNOWN, diagnostics retained; earlier fixture fixes scoped PASS.
BLOCKED_TRANSFER/no retry. No live shortcuts/install/services/network/ACL/security/
credentials/VPS/release/uploads; weights/readiness unchanged.

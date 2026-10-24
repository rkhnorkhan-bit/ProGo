# F23 auxiliary prerequisite: legacy migration evidence

Baseline source5f84ceb5f55d76a1f393e665de87053260b24aeb/checkpoint
8b92a750fdd5c2251e868ce25778a44f73b749d3. Full final CI681 logs independently read:
Windows120998709418/15558 lines, Ubuntu120998709215/578 lines; tested merge
01191ad24b207ce4f6191a65d34545ca71439128; both SUCCESS. MenuEvidence14/StartupEvidence13/
PendingProof11/Reconcile13/RollbackOutcome12/Maintenance19/Outcome10 and prior installer/
SOCKS51/Desktop6388/policy214/Instance30/Maintenance126/Shutdown47/HomeVPN4816/Portable91
suites PASS. Pack runner-only/uploads-prune SKIPPED. Baseline PASS never covers this change.

## Acceptance and production scope

Only installer auxiliary evidence for existing ProGo Status.lnk migration. Existing public
Install and Desktop/worker MigrateMenu APIs and their cancellation path remain unchanged.
The installer evidence path retains the existing primary EnsureLink and legacy-match behavior,
adding a narrow owned deletion admission instead of trusting a subsequent path-based Delete.
This is not a new user feature, automatic recovery, replay or auxiliary rollback.

A bounded read witness denies writes/deletes while the same native Matches oracle checks
source semantics. Before deletion, a separately acquired existing READ+DELETE handle must
have the same hashed volume/file-index/creation identity and exact observed bytes; it denies
writes/deletes while checked. Linked/directory handles are rejected. No DELETE_ON_CLOSE is
used. Only after those checks SetFileInformationByHandle(FileDispositionInfo) marks THAT
handle for deletion; using/finally closes it. Missing/changed/replaced source is fail-closed.
The verified primary result is pinned against write/delete until this deletion settles.
An externally appearing legacy afterward is never removed and prevents accepted completion.
No claim against every hostile ancestor-directory swap or future external edit is made.

This native contract was checked against Microsoft primary documentation:
- https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-setfileinformationbyhandle
- https://learn.microsoft.com/en-us/windows/win32/api/winbase/ns-winbase-file_disposition_info
- https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-getfileinformationbyhandle
- https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-createfilea
DELETE access is required; FileDispositionInfo is class4 and its BOOLEAN payload is one byte.
No ACL/owner/security-policy change is performed. Unsupported identity/admission fails closed.

Receipt stores schema/request/disposition, install/location identities, before/current hashes/
presence, hashed source origin, semantic match, exact owned deletion hash and replacement
hash. It contains no raw file IDs, user paths, shortcut bytes, argv or secrets. Removed requires
actual before ownership, after absence and still-verified exact primary replacement. Preserved
absent/custom/foreign/skipped observations never claim deletion ownership. Read-only verifier
checks caller's expected location/identity/current result and never repairs/deletes mismatches.

Legacy receipt is nested alongside Startup/Menu in the same durable payload as runtime full
inventory/admissions/ownership. Production refuses missing/wrong-root legacy evidence before
metadata creation. Existing maintenance mock gains only the synthetic adapter result shape;
its assertions/timeouts/scenarios are unchanged. Old Startup/Menu/pending/runtime guards and
actual original error/no Launch after rollback are retained. Pending admission remains blocked.

LegacyEvidence14 uses own GUID temp Programs/Startup, actual adapter/COM type and owned files:
absent/removed/skipped-owned/skipped-custom/foreign-target/malformed/locked-retry/
changed-admission/replaced-identical/appeared/replacement-changed/other-identity/other-location/
durable. Private reflection targets the actual admission helper for deterministic byte-change
and identical-byte object replacement; requires the specific ownership IOException, not an
arbitrary exception. No production diagnostic hook is introduced. Lock case requires actual
DELETE-admission denial, exact retained source, then explicit retry after own lock release.
Durable writer round trip preserves actual proof with root and runtime ownership, blocks missing legacy
and pending publication, preserves settings/vault/key-ref/foreign bytes. Finally closes only
own lock/stage/temp resources. No live known-folder shortcuts, installation or service changes.

Local review/static parity/signatures/PInvoke layout/diff/secrets/public-content PASS.
PowerShell parser/C# compilation/Windows native LegacyEvidence14 and exact regressions NOT_RUN
locally; new exact CI must establish results. Do not call this production acceptance.

## Exact remaining dependency

Next same new CI final evidence. Then bounded durable auxiliary receipt parsing and current
revalidation against trusted expected known-folder identities, full Startup/Menu/Legacy result,
phase/snapshot/inventory/admission/ownership/current runtime and conflict rules; only then safe
pending-installed metadata promotion. Intent or receipt presence alone never authorizes it.
Full crash/partial runtime recovery, retention and auxiliary rollback remain separate mandatory
F23-R02 work. Existing seven F23 gates/dependencies and distinct engineering ZIP/standalone/
commercial acceptance remain in F23_INSTALLER_MENU_COMPLETION_EVIDENCE.md; none is closed here.
Sharing/Python/rebind incidents UNKNOWN; BLOCKED_TRANSFER/no delivery retry. No live install/
services/network/ACL/security/credentials/release/uploads. No readiness percentage recalculated.

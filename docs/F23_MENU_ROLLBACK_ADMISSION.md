# F23 read-only Menu rollback admission prerequisite

Baseline source1f6b0bb7a9f73b1326fcceece1af80657035f8b3, CI694 tested merge
9e38b10d878348b2cfb2f495484e7ffa08b8193b. Independently checked full Windows15799/
Ubuntu578 logs: StartupFailure15/MutationAdmission14/Result13/OwnedStartup14/Admission25,
previous installer/Desktop6394/Instance30/Maintenance126/Shutdown47/HomeVPN4816/Portable91
PASS. Pack runner-only; uploads/prune skipped. No owner acceptance inferred.

## Missing guarantee and acceptance

Menu receipt had exact bytes and location identity, but no native origin of the object created
by this operation. Identical bytes after replacement do not establish ownership. The existing
capture-enabled EnsureLink path now uses the SAME read witness already used for Startup:
record volume/file index/creation-time digest from the new temporary object's handle across its
existing rename, and retain it in MenuInstallEvidence. No new shortcut operation is added.
Non-capture install/MigrateMenu paths remain unchanged. Preserved/custom/absent state receives
no origin or new ownership. All creation/destruction/native APIs remain unchanged.

VerifyDurableMenuRollbackEvidence strictly decodes the fixed DTO, verifies trusted install/Menu
identity, disposition/request/presence and exact actual bytes/native target. Created receipts also
require exact native origin and byte hash on the SAME pinned read handle. Missing/null/incorrect
origin or identical-byte replacement fails closed. Preserved state is observational preserve-only,
never authorization to delete. Old incomplete receipts lacking this field are not retroactively
upgraded or granted mutation authority. Historical terminal readers are unchanged.

Get-ProGoInstallerMenuRollbackAdmission is read-only, returns null on refusal, otherwise a
hash/boolean classification: created-from-absence or preserve-only, MutationAllowed=False.
No target path/shortcut bytes/commands are returned. It pins a bounded strict durable receipt,
rechecks snapshot/current runtime/full admissions/identity/ownership and independent trusted
Programs/Startup locations. It accepts either complete current auxiliary proof or already verified
Startup admission AND durable original-state result. Mere Startup absence is insufficient.
Native Menu identity/bytes and all context/receipt hashes/state are rechecked before return.
Unknown/conflicting/foreign/incomplete state fails closed; inspection creates no evidence files.
This is not a retained mutation lease: a future Menu operation must independently revalidate and
acquire exact ownership immediately before any mutation. No rollback or terminal/Launch is wired.

## Tests and boundary

MenuRollbackAdmission32 uses own GUID temporary roots, real existing payload/stage/snapshot/
owned runtime writes/receipt writer, native shortcut creation and read-only classifier. Positive
cases: created, custom-preserved, absent-preserved, legacy-removed preexisting scenario, and
post-startup (real owned Startup rollback plus verified durable observation as fixture setup).
Refusals retain all former25 ownership/receipt/snapshot/runtime/trusted-location/type/size/conflict/
active-writer conditions, plus identical Menu replacement, missing/null/wrong origin, replacement
after Startup settlement and unproved Startup absence. Three fresh observations per case must
match, preserve the entire file inventory and every byte, private/foreign data and blocked new
attempt. Exclusive reopening proves readers close. Cleanup only own guarded resources; no waits
or changed deadlines. Existing suites and assertions remain unchanged.

No Menu rollback, Legacy extension, retention/general runtime recovery, live shortcut operations,
installation/service/network/ACL/security/credential/VPS action, release or uploads. Existing
failure path preserves original error, no false success/Launch/full auxiliary outcome. Settings/
vault/keys/custom/foreign resources remain excluded. Mandatory next: owned Menu operation only
after exact-source Windows proof, then its durable admission/result/error integration; Legacy,
full auxiliary transaction, partial pre-receipt failures, retention/runtime recovery remain separate.

Local static/review/signatures/parity/secrets/public checks do not prove PowerShell/C# compilation
or native Windows32. Those stay NOT_RUN until the exact new CI. UNKNOWN incidents and
BLOCKED_TRANSFER remain open, no score increase.

## State publication correction

Published source1f6b0bb contained an old CI667 active_work snapshot because later state commits
were local only. This stage includes the latest local checkpoint history and replaces authoritative
current pointers, preserving all historical requirements/evidence/incident ledger. Current-tree
identity is resolved by git rev-parse HEAD and exact CI checkout; a commit cannot contain its own
SHA. Reviewed code SHA is fixed before the state commit, post-publication exact source/CI/checkpoint
is recorded locally and in PR metadata without causing a duplicate CI. Old snapshots are history,
not competing current plans. Readiness weights and credited score remain unchanged.

# F23 pending installed completion proof prerequisite

Baseline source abdaa54c6593da016e37bdeeb00a2dbfeb2e4344, checkpoint
3a4a7a793147cd451742e62aae27933f73c5ef71. Full CI678 final Windows15517/Ubuntu578
lines read independently: tested merge 0922838486cb76ed71a8661244516a2727f118d7,
both jobs SUCCESS. Reconcile13/RollbackOutcome12/Maintenance19/Outcome10/RestartGate8/
Journal8/Rollback8/Payload9/Stage8/Backup8/SelfCheck9/SOCKS51/Desktop6388/policy214/
Instance30/Maintenance126/Shutdown47/HomeVPN4816/Portable91 and prior suites PASS.
Pack runner-only; uploads/prune SKIPPED. These PASS results do not validate this change.

## Acceptance and dependency

Legacy outcome.pending.json does not persist completed self-check/shortcut phases or the
full owned-write hashes. Intent, a filename, inventory and admissions alone do not prove
that these phases completed. This slice adds only the prerequisite, not restart promotion.
The actual production caller passes the two ordered completion phases only AFTER both
existing operations return successfully. The existing durable writer stores phase names
and all captured owned hashes alongside the already verified snapshot/inventory/admissions.
An explicit production completion proof must pass the new reader before terminal rename.
Existing two-argument helper callers remain compatible; legacy terminals retain their
existing semantics, but their missing completion proof can NEVER pass the new reader.

The read-only pending proof reader first applies the unchanged strict installed proof
(identity, exact full inventory, all linked admissions, saved snapshot and directory scope).
It additionally requires completion schema1, exactly ordered self-check/shortcuts, unique
ownership for every inventory leaf with each owned hash equal to its intended new hash,
then revalidates saved snapshot and actual CURRENT full runtime bytes. Missing, malformed,
unknown, conflicting, foreign or stale state returns false. No mutation, replay, recovery
or pending metadata rename occurs. Even a valid pending proof remains blocked by the
existing new-install admission gate. Phase evidence records successful return boundaries;
it is not independent proof that mutable external shortcuts remain unchanged after crash.
Safe restart publication, including the required auxiliary-state contract, is still OPEN.

PendingProof11 uses the real owned-write and durable outcome writer in own GUID temporary
fixtures, injecting an interruption after Flush before publication. Cases complete/fresh/
legacy/phases/ownership/inventory/foreign-runtime/snapshot-corrupt/journal-corrupt/identity/
conflict. Only the first two pass read-only validation; ALL remain pending-gate blocked.
Whole fixture file counts and all runtime/private/foreign/evidence bytes stay identical.
Original Outcome10 and all old assertions/timeouts/fixtures are unchanged.

Local review/signatures/helper parity/diff/secrets/public-content PASS; no local PowerShell
or Windows available, so parser/compilation/PendingProof11/native regressions NOT_RUN.
The exact new CI must provide those results. No prior PASS inherited.

Remaining: safe pending-installed metadata publication with proven auxiliary completion;
other pending/partial crash states and durable ownership; bounded runtime recovery without
replay/foreign overwrite; retention protecting unresolved proof; auxiliary-state rollback;
remaining F23/PGA04/updater fallback/package/owner acceptance. CI676 sharing and earlier
Python/rebind incidents remain UNKNOWN with diagnostics. BLOCKED_TRANSFER unchanged.
No live install/services/ACL/security/network/credentials, release/uploads or delivery.
Readiness implementation58–80/100, verified22/100, change0; weights unchanged.

# F23 Startup durable pre-mutation admission / read-only crash settlement

Baseline CI692 sourcecf11c72d2f3dabebf2dfcae1b506447a758b0680, tested merge
2efbc5a4dee89d1687e0caffebd1b8d462b085f8. Full Windows15743/Ubuntu578 logs
independently read: both SUCCESS; StartupResult13/OwnedStartup14/Admission25 and
prior installer/Desktop6394/Instance30/HomeVPN4816/Portable91 PASS. Pack runner-only,
uploads/prune SKIPPED. No whole F23/production/owner-PC acceptance inferred.

## Acceptance and narrow boundary

Existing Invoke-ProGoInstallerStartupRollback now requires an exact durable admission
before calling its unchanged native mutation. Full snapshot/new runtime/admissions/
identity/ownership/receipt/native origin/expected locations are verified independently.
Open-ProGoInstallerStartupMutationAdmission creates own deterministic sidecar CreateNew,
WriteThrough/Flush(true)/bounded exact readback, then rechecks actual state and record.
The owned stream denies write/delete and is retained until native physical settlement;
finally always closes it. Existing valid not-applied admission can be reused only by
an explicit invocation after revalidation, never silently rewritten or auto-replayed.
Partial/tampered/foreign/sibling conflict refuses before mutation. Cancellation before
or after persistence but before native call keeps Startup unchanged; cancellation/result
semantics in native helper stay unchanged.

Record binds schema/phase/install/journal/receipt/location/origin/hash/original presence.
AutomaticMutationAllowed=false: intent alone is never authorization to restore/delete.
Read-only Get-ProGoInstallerStartupMutationState recomputes physical proof, validates
bounded exact durable record, then repeats physical proof while record is pinned:

- not-applied: exact original owned object is still present; no automatic operation.
- original-observed: actual original absence/preserved state verified; no replay.
- blocked: missing/unknown/incomplete/tampered/replaced/conflicting evidence.

Complete-ProGoInstallerStartupMutationSettlement can only record/reuse the already
implemented Startup-only observed result after original-observed. It never invokes
native mutation, runtime restore or shortcut creation. Repeated inspection/settlement
must preserve every existing byte and only create the known observed result if needed.
No full auxiliary rollback/success/Launch/retry permission follows from these states.

Automatic reconciliations/new install now block admission prefixes as well as partial
outcome prefixes. Read-only preflight may inspect admission presence but never treats
it as authority; native entry additionally validates exact content/identity/scope.
Current original error handler/Stop/finally/Launch logic is unchanged. Integration with
complete auxiliary outcome remains separate, as do pre-receipt partial failures,
Menu/Legacy, retention and general runtime recovery. No cryptographic authenticity or
privileged hostile-ancestor race boundary is claimed.

## Isolated regression

StartupMutationAdmission14: interruption before mutation, interruption after actual
owned mutation, preserved custom, persist failure, short write, tampered record,
identical-byte foreign replacement, changed runtime/snapshot, missing admission,
delete locked, cancellation before entry/after persistence, foreign admission conflict.
Own GUID temp fixtures and existing actual receipt/runtime writer/rollback only.
Three fresh read-only inspections require correct physical state and no file changes;
settlement records only verified observation, never replays; partial intent/result keeps
new install blocked. Exclusive stream reopen proves cleanup.

Existing OwnedStartup14 retains all assertions and now accounts for exactly one known,
independently validated new admission file (not arbitrary exclusions). Explicit locked
retry must retain its exact hash; old byte inventory assertions stay intact. New record
is the intended production side effect, not weakened coverage. All waits unchanged.
New Windows compilation/parser/native14/regressions NOT_RUN locally. Local review/static/
signatures/parity/secrets/public checks do not replace exact CI.
CI686/repair20s/sharing/Python/rebind UNKNOWN, diagnostics retained; prior fixtures scoped
PASS. BLOCKED_TRANSFER/no retry. No live shortcuts/install/services/network/ACL/security/
credentials/VPS/release/uploads. Fixed readiness weights unchanged.

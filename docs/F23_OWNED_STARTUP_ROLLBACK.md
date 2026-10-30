# F23-R02 owned Startup rollback with actual result verification

Baseline CI690 source dfff6e8cf3ce18739c3335ad5d8c8fd2814c8d9a, tested merge
0c9d485bfe6836489f8aaff3c6f22f2441d4ff3f. Full Windows15714/Ubuntu578 lines
independently read: both SUCCESS. StartupAdmission25/Publication25/Auxiliary24 and
all previous installer/Desktop6394/Instance30/HomeVPN4816/Portable91 PASS. Pack
runner-only, uploads/prune SKIPPED. No whole F23/production/owner-PC PASS implied.

## Narrow acceptance

Only the Startup owned-created operation is implemented, not full auxiliary rollback.
Invoke-ProGoInstallerStartupRollback reuses strict full admission and pins exact bounded
pending receipt through invocation; install/location/snapshot/runtime/admissions/
auxiliary proofs are unchanged. Independent trusted caller locations supply the manager,
never journal paths. Caller owns installer maintenance; current error handler is not
wired to this helper until durable auxiliary outcome dependencies are complete.
Original error/no Launch/Stop/finally remain unchanged.

Old Startup evidence had hashes only, insufficient to distinguish an identical-byte
external replacement. New OriginIdentity is captured from a native handle of the
newly generated temporary shortcut before the same object's no-overwrite move. The
handle denies writes; DELETE sharing permits that move. Existing non-capture paths
keep the original no-overwrite behavior. Actual created receipt verification checks
origin and bytes at the destination. Missing origin cannot authorize deletion; old
unresolved receipts fail closed, while historical terminal handling is unchanged.

TryRollbackDurableStartup accepts only strict Startup receipt fields. Preserved/custom
state is verified then left untouched. Created-from-absence requires original absence,
actual target, exact current hash and origin. A DELETE+READ handle opens only the expected
Startup leaf with OPEN_REPARSE_POINT and share READ (denies write/delete). Recheck native
origin/bytes on that same handle, check cancellation, set FileDispositionInfo, close
all owned handles, then verify actual original absence. No path-based File.Delete,
no unconditional retry, no Menu/Legacy modification. Reparse/changed/replaced/denied
state refuses. Cancellation before mutation propagates; after a mutation result
verification is not abandoned. Missing/failed deletion cannot become success.

Identity checks are at the actual leaf mutation boundary. Independent path guards,
maintenance ownership and native handle checks are retained; this does not claim a
security boundary against a hostile privileged actor swapping ancestor directories.
The receipt is not cryptographic authenticity and the in-memory admission is not a
replacement for durable auxiliary mutation/outcome recording.

## Isolated tests and limits

OwnedStartupRollback14 exercises actual receipt/runtime writer and native helpers only
inside new GUID fixture Programs/Startup roots. Actual outcome writer interrupted after
Flush; synthetic completed-phase marker is not a live install/self-check acceptance.
Cases: created, preserved custom/absence, changed, identical-byte replacement, missing
origin, delete locked, explicit locked retry, cancellation, native changed/replaced/
cancelled after full admission, foreign runtime, wrong trusted location.
Assertions require native origin witness, exact success/false/cancel distinction, real
absence only for owned-created, all remaining fixture/runtime/Menu/Legacy/snapshot/
receipt/private/foreign bytes intact, unresolved gate still blocked. Exclusive reopen
of receipt and surviving link proves own handles settled; finally closes own locks/token/
stage and rejects reparse cleanup roots/descendants. No waits/assertions weakened.
Local static/review/signatures/parity/secrets/public checks are not compiler/native proof;
new Windows compilation/14/native/regressions NOT_RUN until exact CI.

## Remaining dependencies

Durable auxiliary mutation admission/outcome, crash-safe interrupted Startup settlement
and integration with original installer failure path remain mandatory next work. Partial
shortcut failure before complete receipt needs its own durable pre-change evidence.
Menu/Legacy rollback, retention and general runtime crash recovery are separate; no full
auxiliary rollback or permission to retry is published here. Settings/vault/keys/foreign
resources untouched. CI686/repair20s/sharing/Python/rebind UNKNOWN, diagnostics retained;
PID/Marshal/restart fixture fixes scoped verified PASS. BLOCKED_TRANSFER/no retry.
No live shortcut/install/service/network/ACL/security/credential/VPS/release/upload action.

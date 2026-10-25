# F23 Startup-only failure-path integration

Baseline: source ddefeef9531b06c646e1c1b01edd8400a7377cf8, CI693 tested merge
7a3b592645d089c46d4f340cb98aeddbf98e55c1. Full Windows15758/Ubuntu578 logs verified:
MutationAdmission14, Result13, OwnedStartup14, Admission25 and previous installer suites,
Desktop6394, Instance30, HomeVPN4816, Portable91 PASS. Pack runner-only; uploads skipped.

## Acceptance and production boundary

The existing maintenance-owned installer catch now distinguishes errors before auxiliary work
from errors after its entry. Mark entry BEFORE Initialize-ProGoShortcuts, including throws that
produce no receipt. Before entry, the existing verified runtime rollback/terminal path is unchanged.
After entry, missing/incomplete/conflicting evidence blocks further dependent recovery. Never
publish full rolled-back, Install OK or Launch merely because Startup has been restored.

Invoke-ProGoInstallerFailureStartup returns only not-started, startup-observed or blocked.
Only the explicit false entry flag can return not-started. The running owner attempts the existing
owned Startup operation only from freshly verified snapshot/runtime/admissions/receipt/origin and
independent trusted Programs/Startup locations. Durable admission precedes deletion; the owned
native handle rechecks exact original identity/hash. An existing admission permits only read-only
settlement, even when not-applied: no automatic second deletion or replay. Missing/partial receipt,
external bytes or replacement, changed snapshot, conflicting admission or persistence failure blocks.

Startup-only observation is persisted/revalidated by the existing settlement, with its false full
rollback/success/mutation flags. Keep the current new runtime and snapshot after auxiliary entry:
these are required to verify this partial result, and Menu/Legacy rollback is not implemented here.
Thus the original runtime restore and its full terminal are deliberately NOT reached after auxiliary
entry. Original exception is rethrown unchanged. Stage and maintenance finally still run, Stop
precedes all writes, and no Launch occurs. The status explicitly reports Startup-only state and
auxiliaryRollbackVerified=False; this is not permission for a new installation.

## Targeted tests

InstallerStartupFailureTests runs the ACTUAL installer body from InstallDir through catch/finally
and success/Launch tail, real temporary runtime writes/snapshot/journal, native shortcut creation,
owned rollback and durable settlement. Only self-check executable, maintenance IPC, process launch
and independently supplied fixture locations are adapters; no live shortcuts or security changes.

StartupFailure15: created, custom-preserved, absent-preserved, receipt-missing, receipt-partial,
startup-changed, startup-replaced (same bytes, different origin, original retained), menu-changed,
snapshot-changed, admission-persist-fail, result-persist-fail, admission-not-applied, already-observed,
delete-locked, stop-refused. Admission experiments run after the failed receipt writer closes its
stream. Assertions require exact original error, actual Startup bytes/absence, private keys/vault/
settings/foreign bytes, untouched Menu/Legacy/snapshot, current runtime, truthful partial result,
no full terminal or Launch, next-attempt gate blocked, exclusive handle reopening and finally cleanup.
An existing not-applied admission cannot be replayed. A settled observation is safe to re-read.

The historical Maintenance19 suite already replaces all shortcuts with synthetic DTOs. It now
explicitly mocks the new auxiliary failure adapter as not-started, preserving EVERY original runtime
rollback assertion and scenario. This isolates those existing runtime-only fault contracts; it is
not evidence for the real auxiliary branch. StartupFailure15 executes that actual branch separately.
All former assertions, deadlines, native APIs and other fixture bodies remain unchanged.

Local review/static/signature/parity/diff/secrets/public-content checks are not a Windows run.
New PowerShell parse/compilation/native15 and exact-source regressions remain NOT_RUN until CI.

## Mandatory remainder

No complete auxiliary rollback or terminal, Menu/Legacy rollback, retention or general runtime
crash recovery is delivered here. Partial pre-receipt failures remain blocked; their ownership and
recovery evidence is separate required work. Verified Startup-only observation after failure does
not close those dependencies. Settings/vault/keys and foreign resources remain excluded.
Old Python/rebind/sharing/repair/setup cancellation incidents remain UNKNOWN; no non-reproduction
is called a fix. Private candidate remains BLOCKED_TRANSFER; no transfer retry, upload or install.

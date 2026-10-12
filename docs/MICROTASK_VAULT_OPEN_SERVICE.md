# Micro17 — worker opening and accepted session ownership

This bounded step adds the synchronous worker-only `VaultOpenService`, a fresh
independent record factory and the minimum lease required for durable false
session writes. It uses the native-tested policy/record/file/store/journal/recovery
foundations. It does not close F09: asynchronous Add/Edit/Delete, startup expiry,
active midnight revocation and their complete integration checks remain separate.

## Explicit paths and opening results

`Probe(realPath, token)` only checks an explicit local regular file. It never
reads ciphertext, derives keys or writes. Only exact child `FileNotFoundException`
under a rechecked existing parent permits `CreateRequired`; denied/missing parent,
directory/reparse/ADS/relative paths and cancellation fail closed. Canonical
parent checks reuse the existing local-drive/ancestor guard. These checks reduce
accidental redirection; they are not a hostile same-user filesystem sandbox.

`Create(realPath, pin, token)` uses exclusive `VaultService.CreateAtPath` in the
owned existing parent. It preserves the real-vault format/KDF/PIN compatibility.
No implicit global path, directory, logger or unrelated file is accessed.
`Open(realPath, dailyRoot, pin, localClock, freshFactory, token)` returns only
static ordinary vault errors. A failed result has no session. Neither result
errors nor service logs disclose the false-session mode, PIN, path or dataset.
The service performs no logging.

Correct real authentication has absolute priority: a real success bypasses the
auxiliary root, recovery, factory and clock entirely. Other real-read errors
also avoid the auxiliary/factory route. Only a supported, structurally valid
`AuthenticationMismatch` can enter the false-opening branch; the approved
MAC/wrong-PIN ambiguity remains unchanged.

## Durable daily adoption

The false branch explicitly recovers under the existing protected SID gate.
It accepts only safe recovered data/Missing evidence. A changed local calendar
day first commits an empty baseline using the accepted stamp. Policy evaluation
then selects the persisted exact binding or proposes the first allowed mismatch.
Provisional data are never published before successful compare-exchange.
New sessions use the detached accepted record returned by the atomic store;
repeated opening uses fresh recovered data and never regenerates an existing
binding. A different mismatch returns exactly `Неверный пин-код`.

Conflict and changing-day retries are bounded to three attempts. Each retry
re-authenticates the real file, performs fresh recovery, and re-evaluates the
current binding instead of blindly overwriting a competing writer. Clock is
checked after KDF, before binding CAS and after lease/session construction before
publication. A day change after commit drops the unpublished session references,
attempts an empty new-day CAS, and retries; no old-day dataset is published.
UTC input is rejected; the production caller supplies local Windows time.

Cancellation is checked at phase boundaries. Once observed it returns Cancelled
with no session; it does not undo a completed filesystem transaction. In
particular, cancellation after successful create or binding CAS may leave a
durable file/binding already accepted. This is explicitly not a rollback or a
claim that no writes occurred. The same accepted PIN reopens it, while another
mismatch is rejected. The GUI must also discard late results after its owner
closes or requests cancellation, irrespective of the returned status.

The factory receives only local time, never PIN or real data. Every generation
owns new records/list/IDs and cryptographically random secret bytes, which are
cleared after string conversion. There is no old deterministic PIN-derived seed,
fixed 1970 timestamp or `sample-` secret marker. Invented local host names are used; no network access is performed.

## Minimum persistence lease

`VaultDailySessionPersistence` captures an accepted scoped stamp/day and never a
Missing token. Session copies share their writer lease; independent reopenings
have independent observations. `IsCurrent` performs no I/O. `Save` performs
same-binding CAS, updates its stamp only after an accepted commit and throws
an ordinary static error on failure. It never writes the real path, creates a
new Missing binding, or adopts a competing revision. Caller data must not be
mutated concurrently during this worker operation.

An observed expiry during Save irrevocably revokes the lease and attempts the
empty new-day CAS. A postcommit expiry does not publish the old-day edit; failed
cleanup remains an error and requires reopening. Subsequent reversal of the
clock does not revive an explicitly revoked lease. Active idle sessions still
need the later GUI expiry/revocation owner; this intermediate lease alone is
not a midnight scheduler or startup sweep.

## Evidence boundary

The independent harness owns only GUID-scoped temporary roots and encrypted
fixtures. Global `AppPaths` and `SafeLog` guards throw/count on any use. Cases
cover exclusive creation/probing, real priority with blocked/invalid auxiliary
paths, exact PIN rules/reopening/accepted writes, real corruption/unsupported
KDF/native read locks, competing CAS, explicit interrupted transaction recovery,
actual busy gate, provisional and postcommit cancellation, forward/backward
dates, postcommit expiry, bounded unstable dates and independent generation.
Owned gate workers are released and joined; no owner environment is inspected.

At this source checkpoint native compilation and execution are **PENDING**.
Local source/whitespace review is not native PASS. Root records the completed
exact-head CI evidence after running the independent C#5 Windows harness.
Historical **22/31** and open F09 are unchanged; this is not a release, installer
or acceptance of the owner's installed application/PC/VPS/phone.

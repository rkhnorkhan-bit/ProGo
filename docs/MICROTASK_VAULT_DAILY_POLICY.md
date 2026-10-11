# Micro10 — intermediate daily vault policy

This step implements a deterministic transition engine in `VaultDailyPolicy.cs`
and an independent guarded harness. It does not change `VaultService.Open`,
create a session, generate fake records, persist daily state, or wire the UI.
F09 and the daily vault feature remain open.

## Inputs and transition ownership

`Evaluate(state, localNow, pin, realRead, fakeDataFactory, knownRealData)` accepts
the existing `VaultReadResult` and an explicit local calendar timestamp.
`DateTimeKind.Local` and `Unspecified` are accepted; UTC is a static argument
error. The engine never reads the OS clock or converts a timestamp to a timezone.
State stores an unspecified-kind calendar date and, when bound, only the fake
PIN and its dataset reference. A real PIN is never retained or logged.

`VaultDailyState` and transition metadata have private setters and checked
construction. `VaultData` remains mutable because accepted fake edits must keep
the same dataset identity. The engine does not mutate caller state or records.
The input state and timestamp must satisfy the contract; null state and UTC
timestamps are programmer errors, rather than opening transitions.

Every normal transition returns `StateAfterExpiry`. When the local date changes
in either direction, this is an empty state for the newly observed date before
PIN format, real-read status or factory evaluation. Its fake PIN and fake data
are absent; it retains no expired record/history references. A consumer must
adopt this expiry baseline even after a malformed PIN, reader failure or factory
failure. Once adopted, returning the clock to an old date cannot resurrect the
old binding. The old dataset itself is untouched; session revocation and clearing
caller-owned old references belong to later integration.

A first accepted mismatch returns `FakePendingCommit`, `PendingFakeState` and
`CandidateData`. It has `RequiresFakeCommit=true` and `CanOpen=false`.
The expiry baseline remains unbound. The later persistence layer must atomically
store the candidate before adopting its pending state or publishing a fake
session. Failure or abandonment must retain the baseline. This engine performs
no commit and cannot authorize publication merely because a candidate exists.

`RealOpened` and `FakeOpened` have `CanOpen=true`, carry the exact accepted data
reference, and have no pending state. `Rejected` carries a static error and no
candidate data or pending state. Real session creation and persistence stay
outside this policy.

## Rules implemented

- For a format-valid PIN, a valid real success always opens the real result,
  regardless of the same-day fake binding or attempted PIN. It does not reset
  that fake binding.
- The first format-valid `AuthenticationMismatch` in an unbound day invokes the
  factory once. Only a valid independent dataset produces a provisional binding.
- Repeating the exact bound fake PIN returns the same dataset and accepted edits,
  without calling the factory. Another mismatch returns exactly
  `Неверный пин-код`.
- PIN format matches the existing reader: four UTF-16 code units, each accepted
  by `Char.IsDigit`. Legacy Unicode digits remain distinct strings and are
  compared ordinally, without normalization. Malformed PINs consume no binding.
- `FileUnavailable`, `InvalidData`, `UnsupportedFormat`, an unknown status, or a
  missing result never creates or opens fake data. Failures preserve same-day
  state; a changed-day failure still returns the empty expiry baseline.
- A fake remains valid through 23:59:59 and the final tick of its local date.
  The first opening on another date resets the binding before any other rule.

Data at this boundary requires v1, a nonnull entries list, no null records and
nonempty, ordinal-unique entry IDs, matching the reader's mandatory ID rules.
Empty entries are allowed; IDs receive no trimming or whitespace normalization.
The established reader remains responsible for authenticated envelope and other
record validation. A malformed success cannot fall back to fake data. An edited
fake whose boundary shape becomes invalid is rejected without rebinding.

The factory must be side-effect-free with respect to caller-owned state/data and
must produce a fresh independent dataset for every accepted new generation.
The engine rejects a returned old fake dataset available in the input state, or
the optional caller-held `knownRealData`, including shared entry-list or record
identities. `knownRealData` is not retained and the consumer must supply it when
holding a real dataset. After adopting an empty expiry baseline there is no
historical dataset cache; freshness of arbitrary delegate output remains the
factory contract. A null factory, exception, malformed result or detected alias
returns a static failure without consuming the first mismatch or leaking the
factory exception text.

## Regression boundary and evidence

`tests/VaultDailyPolicyTests.cs` compiles only with `Vault.cs` and the new policy.
Its independent `AppPaths` and `SafeLog` stubs throw on any access; it does not
compile the real global paths, touch user files, or initialize Windows UI.
The memory-only fixtures cover provisional ownership and abandonment, edited
reference identity, exact wrong-PIN text, all read statuses, malformed and
legacy Unicode PINs, authoritative real success, midnight/final-tick boundaries,
forward and backward date changes before all opening routes, factory call
counts/failures/aliases, boundary shape and safe argument errors.

`Test-ProGo.ps1` compiles the independent harness explicitly as C#5 and uses its
owned process with a 90-second deadline before the recovery and desktop suites.
On deadline it kills that harness PID and waits up to five seconds for exit.
This is a single owned process boundary, not descendant-tree supervision.

Local source/whitespace checks and independent source review are the available
evidence. C#5 compilation and native Windows harness execution are
**NOT_CHECKED** in this development environment because no local .NET runtime
or PowerShell executable is available. Fresh native evidence remains pending.
Daily persistence/restart ownership, midnight session revocation, fake record
generation, UI async integration and stale-save revision control are outside
this intermediate step; no complete F09 or user-environment acceptance is claimed.

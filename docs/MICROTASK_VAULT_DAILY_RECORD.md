# Micro12 — protected daily vault record codec

This step adds a memory-only binary record codec and an independent guarded
Windows harness. It performs no file access, atomic storage, expiry, state
adoption, session creation, fake generation or UI integration. `VaultService.Open`
is unchanged. F09 and the complete daily vault feature remain open.

## Record ownership

`VaultDailyRecord` contains checked, immutable metadata: a `VaultDailyState`, a
nonempty generation GUID, and a nonnegative Int64 revision. Revision zero and
Int64.MaxValue are valid stored values. The later store must use checked revision
increments and generation/revision compare-and-swap; this codec neither
increments revisions nor prevents replay or stale saves.

The state stores an unspecified-kind calendar date at midnight. An unbound state
contains no PIN or dataset. A bound state contains only its fake PIN and dataset;
there is no real PIN parameter. Bound data with zero entries remains bound,
preserving the PIN after the user deletes every fake record. It is distinct from
an unbound tombstone.

`VaultData` remains mutable. Encode revalidates its current version, list, record
and ID shape. The later owning store must serialize access or take an owned
snapshot; this intermediate codec does not provide concurrent mutation safety.
Decode produces independent dataset/list/record objects. Neither operation
mutates caller data or input bytes.

## Binary contract and protection

All integers are little-endian. The public envelope is eight ASCII bytes
`PGODAILY`, Int32 envelope version 1, Int32 protected byte length, and exactly
that many protected bytes. The length must be positive and match the complete
remaining input; magic, input cap and exact framing are checked before DPAPI or
an unsupported-version result.

The whole inner record uses native `ProtectedData` with `CurrentUser` scope and
fixed purpose entropy `ProGo/VaultDailyRecord/v1`. The entropy separates this
record type from other protected application data; it is not a password.
Day, generation, revision, binding flag, fake PIN and every dataset field are
inside the protected payload. Only outer format and protected length are public.

The inner prefix is Int32 version 1, Int64 calendar date ticks, 16 GUID bytes,
Int64 revision and a binding byte 0 or 1. Calendar ticks must form a valid
midnight DateTime; UTC/local kind bits, negative ticks, out-of-range dates and
non-midnight timestamps are invalid. There is no timezone conversion or clock
read. A zero binding flag ends the payload immediately.

A binding flag of 1 adds a length-prefixed fake PIN, Int32 dataset version 1,
Int32 entry count, and these ten ordered fields for every record: `id`, `name`,
`type`, `login`, `secret`, `url_or_host`, `notes`, `tags`, `created_at`, `updated_at`.
Strings use strict UTF8 with an Int32 byte length; -1 means null and zero means
empty. Every other negative length is invalid. Entry IDs are nonempty and
ordinal-unique; optional fields retain null versus empty. Fake PIN validation
matches the existing four UTF-16 `Char.IsDigit` rule without normalization.
Trailing inner bytes, truncation, invalid UTF8 and invalid schema are rejected.

Limits are 4 MiB plaintext, 4 MiB + 65,536 bytes for the complete envelope,
4,096 entries, and 65,536 UTF8 bytes per string. Encode measures byte length and
remaining aggregate budget before allocating each encoded string or extending
the stream. Decode validates envelope sizes before copying/DPAPI, plaintext cap
after native unprotect, count before list allocation, and string lengths before
byte allocation. The native DPAPI allocation happens before the plaintext-cap
check; this codec caps the protected input but does not control internal native
allocations.

Decode returns `Success`, `InvalidData` or `UnsupportedFormat`. A record exists
only for success. Recognized framing with an unknown outer version is unsupported;
an unknown inner version is unsupported only after successful DPAPI and the
minimum common prefix length check. Other malformed inputs and native protection
failures return the same static error. Encode failures expose a static
`InvalidDataException` without an original inner exception. Out-of-memory errors
remain runtime failures. No exception text, path, PIN or payload is logged.

Owned plaintext arrays, protected copies, temporary string byte arrays and the
complete encoder MemoryStream backing buffer are cleared in `finally` blocks.
The encoder preallocates one 4 MiB backing buffer; budget checks prevent its
growth, avoiding abandoned plaintext arrays from MemoryStream reallocation.
Successful caller envelopes and caller datasets are preserved. This does not
claim erasure of immutable managed strings, retained caller references, native
DPAPI copies, or every runtime allocation.

## Regression boundary and evidence

`VaultDailyRecordTests.cs` compiles only `Vault.cs`, `VaultDailyPolicy.cs`, this
codec and its independent path/log guards. Fixtures operate in memory and use
native CurrentUser DPAPI; no application path, user file or Windows UI is used.
Tests cover empty and bound roundtrips, maximum revision, Unicode PIN and text,
all edited fields, null versus empty, independently constructed binary fixtures,
independent native unprotect, wrong-purpose protection, tampering, framing,
metadata, schema, UTF8, count/field/plaintext/envelope bounds and unchanged caller
bytes/data. An empty bound dataset remains distinct from a tombstone.

`Test-ProGo.ps1` compiles this harness explicitly as C#5 with `System.Security`
and gives its owned process a 90-second deadline, followed by a kill and up to
five seconds for that PID to exit. It does not supervise descendant trees.

Local whitespace/source checks and independent source review pass. Native C#5
compilation/DPAPI execution is pending at this checkpoint. There is no local
.NET runtime or PowerShell; runtime evidence must come from the isolated Windows
CI runner. Cross-account/machine DPAPI behavior and the owner's installed
application have not been tested.

Atomic storage/restart ownership, expiry cleanup, session revocation, fake
generation, UI integration and stale-save rejection are separate later steps.
This record format by itself authorizes no vault opening or state publication.

## Final native checkpoint — 11 October 2026

Functional head `42c7ab60c9a3e035a712196765fa74a565f41f1a` was tested in
[CI 38171560167](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38171560167),
merge `cd12b11e921477c85f068eeb8580f259a8eed742` with main
`4bb336575bb1216d3047bd2b294aeecad9fdd65a`.

Windows job `114565055972` completed successfully. The explicit C#5 harness
using System.Security and native CurrentUser DPAPI reported
`PASS: VaultDailyRecordTests (325 checks)` at 20:18:03.035 UTC. These are
assertions, not 325 independent scenarios. All 98 negative decode fixtures
verified static errors and unchanged caller bytes. Independent native unprotect,
empty-bound distinction, edited fields, metadata/schema/UTF8 limits and the
zero application-path/log guard passed. Existing reader 165 and policy 134
checks passed on this same candidate. No target compiler error, FAIL or deadline
failure was observed; independent source and completed-log reviews approved.

Full Windows and isolated server jobs are **PASS**. Public release scan checked
218 sources and 44 release files, and CI folder/ZIP artifacts were uploaded.
These results do not add cross-account/machine coverage, owner acceptance,
atomic file storage, expiry/session revocation, revision CAS or UI integration.
Micro12 has native **PASS**; F09 and historical **22/31** remain unchanged.

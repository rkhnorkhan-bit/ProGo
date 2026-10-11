# Micro08 — bounded vault read contract

Scope: the existing v1 vault reader, its error boundary, exclusive creation, and
an isolated regression harness. This is a partial F09 step; the daily false-PIN
contract is not integrated or declared complete.

## Result contract

`VaultService.Read(path, pin)` returns `Success`, `AuthenticationMismatch`,
`FileUnavailable`, `InvalidData`, or `UnsupportedFormat`. Only `Success` carries
decrypted `VaultData`. Invalid PIN format retains the existing static validation
error and `Char.IsDigit` behavior, including legacy four Unicode-digit PINs;
the PIN is neither normalized nor converted to ASCII.

The reader opens only the explicit input path. It does not create files, log,
or access application paths. File size is limited to 8 MiB **bytes**, including
a second limit during streaming. UTF-8 decoding is strict; a legacy UTF-8 BOM is
accepted. JSON must contain typed mandatory envelope fields. Before KDF, the
reader checks v1, PBKDF2-HMAC-SHA256/120000, AES-256-CBC, HMAC-SHA256, valid
Base64, salt32/IV16/tag32 and nonempty CBC ciphertext divisible by 16.

Only a MAC mismatch on a supported, structurally valid envelope is
`AuthenticationMismatch`: a wrong PIN and modified authenticated data cannot
be distinguished in that case. Authenticated invalid padding, UTF-8, JSON or
record structure gives a failure. Decrypted v1 data requires an entries array
and nonnull records with nonempty unique string IDs. Known entry values must be
strings or null. Missing optional strings remain null. No UI-specific field
lengths, URL rules or new PIN policy are introduced.

DTOs are built from checked, case-sensitive canonical fields. Unknown fields,
including casing aliases, are ignored and cannot overwrite checked values.
Parser exceptions, including malformed Unicode escapes, become static safe
results; the UI-facing failure has no source JSON, PIN, path or inner exception.

`Open` uses this result contract. It retains the preexisting transient sample
behavior only for `AuthenticationMismatch`; it does not implement daily false
data. Other failures throw a safe static exception and do not log successful
opening. A missing read never calls `Create`. The existing PIN dialog keeps its
explicit create/open choice.

`Create` publishes with exclusive `File.Move`, so a destination appearing after
the dialog opened is preserved. Its I/O failure is a static safe exception.
`Save` retains atomic replacement, flush and owned-stage cleanup. Encryption,
KDF, salt generation and the serialized v1 writer format are unchanged.

## Regression boundary

`tests/VaultReadTests.cs` is compiled only with `src/Vault.cs` and test-only
`AppPaths`/`SafeLog` guards. Direct reads cannot access either global dependency.
Legacy `Create`/`Save`/`Open` tests temporarily allow those guards to resolve only
to the harness's owned GUID directory under the temporary directory; no real
application paths, Windows account settings, UI, SSH or VPS are used.

Fixtures are fixed synthetic ciphertext/tag pairs independently generated and
verified with Python hashlib/cryptography, including ASCII and Unicode PINs,
valid-MAC bad JSON/padding/UTF-8/Unicode escapes, null or wrong-type records and
casing aliases. Other cases cover wrong PIN, modified MAC/ciphertext, envelope
schema/Base64/length limits, oversize and missing files, and unchanged source
bytes/file sets.

The maximum-cost case runs first in a dedicated child. The parent gives it
10 seconds to reject `Int32.MaxValue` iterations, then kills that owned PID on
expiry. The child also has an independent 10-second watchdog. The harness
process has a 90-second deadline in `Test-ProGo.ps1`, before SOCKS recovery and
desktop tests. These are bounded process waits and normal owned-child cleanup;
the script does not provide general descendant-tree ownership.

Windows-only cases exercise a real sharing lock and refused atomic replacement.
The public methods additionally check exclusive Create, staging cleanup and the
Unicode PIN writer round-trip. The harness guards prove isolated method
integration; they do not prove the actual UI routing, real `AppPaths`
initialization, or Windows filesystem behavior until executed on Windows.
Existing desktop fixtures remain a separate integration check.

## Evidence at handoff

- Independent fixed-vector HMAC/AES/plaintext verification: PASS.
- Owned diff whitespace and lexical delimiter/string checks: PASS.
- Writer, MAC, KDF and legacy PIN validation methods match the previous source.
- .NET Framework C#5 compilation and native Windows execution: NOT_CHECKED in
  the development environment; no local .NET runtime is available. The parent
  will collect fresh Windows CI evidence for the bundled microtasks.
- Daily persistence, restart/midnight ownership, UI async and stale-save
  revisions remain outside this step. F09 and the 31-point plan remain open.

## Fresh native validation

[CI 38164605392](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38164605392)
tested functional head `00915014df63213524ee793daa2d0a0eec8c852c` through synthetic
merge `e4098fdbc1d70cb823c5f4b2d79d61af020b3636` with baseline main
`4bb336575bb1216d3047bd2b294aeecad9fdd65a`.

- .NET Framework C#5 application and harness compilation: PASS.
- Guarded vault reader harness: **165 PASS**, Windows job `114544444410`,
  18:38:14 UTC. The actual Windows read lock and denied atomic replacement both
  executed. Fixed ASCII/Unicode vectors, canonical fields, malformed Unicode,
  bounded KDF child, static failure messages, source preservation, exclusive
  Create and legacy Create/Save/Open all passed.
- Actual desktop integration: **3485 PASS**, including existing vault atomic/UI
  fixtures. The entire `Test-ProGo.ps1` passed at 18:46:26 UTC.
- Bundled micro07 and the entire Home VPN harness: PASS, 975 checks. Isolated
  server job `114544444301`: PASS.
- Full Windows CI: FAIL later at portable separate-process import. The child
  assertion's failed operand is not logged; no cause is attributed to this
  reader. Remaining portable scenarios and Windows Python tests: NOT_CHECKED;
  final public release scan/packaging/uploads: SKIPPED.
- Local public-source scan: PASS, 210 sources, zero release files. This does not
  replace the skipped release-content scan.

Two independent source reviews and an independent final CI evidence review
passed. Screenshots were uploaded, without a new visual acceptance claim.
Historical 22/31 remains unchanged. Full daily F09, complete installer and user
PC/VPS acceptance remain pending; no user environment was inspected.

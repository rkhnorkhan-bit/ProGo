# CI696 attempt2: Menu origin mismatch proven; mechanism and old probe fault unresolved

Source4f75a48f356b2b49e823c5b38c1072f8b0453aeb, tested merge
8237cca8fc21678791d7517e8a96bf7374d849ef. Independently read full final attempt2
Windows121156450377 log10481 lines and Ubuntu121156452022 log578 lines. Windows FAILURE,
Ubuntu SUCCESS (same original Ubuntu checkout/log reused). Python gate passed this attempt:
non-reproduction, NOT an established fix of attempt1/CI662/670.

## Proven evidence and limits

legacy-removed: snapshot/runtime/pending/auxiliary/context=True; contextState=not-applied;
typedMenu/typedStartup/typedLegacy=True; durableMenu=False; originPresent=True;
originSame=False; bytesSame=True; legacyRemoved=True; replacementSame=True.
Thus a present recorded origin differs from the actual current native object witness while bytes
and other proofs match. Strict Menu ownership refusal is correct. This does NOT prove which
volume/index/creation component changed, when it changed, or that receipt can safely be refreshed.

The old independent probe failed within the broad rename-probe phase with an outer
MethodInvocationException and PowerShell wrapper stack only. That phase covered install, move,
method resolution, reflection invocation, PowerShell callback and after-read. Inner exception and
precise boundary were not emitted. Its exact failure cause remains unknown; no binder/thread/
filesystem mechanism is asserted without evidence. It did not produce before/after facts.
Menu32: created/custom-preserved PASS, legacy-removed FAIL, remaining29 NOT_RUN. Reached earlier
installer/StartupFailure15/Mutation14/Result13/Owned14/Admission25/Desktop6394/Instance30/
Maintenance126/Shutdown47/UpdaterPackage5 PASS. Later HomeVPN/Portable/relay/aggregate/pack NOT_RUN.

## Minimal test-only replacement

Replace only the separate reproduction bridge. A test-only C# helper receives the already loaded
ApplicationShortcuts Type, resolves exact constructor/Install/EnsureLink/native-info signatures,
and invokes the EXISTING EnsureLink with a CLR Action<SafeFileHandle> entirely inside C#.
No PowerShell callback scope or argument-array adaptation occurs in the reflected call. Each
boundary has a fixed phase: root guard, constructor, initial install, vacate, resolve, invoke,
witness read, publication return, published read, comparison. The same native APIs and actual
existing creation/rename sequence remain the oracle. No production method/API/identity changes.

Before/after read the same native origin and volume/index/creation fields, compare exact actual
bytes, and emit ONLY equality booleans, witness count, read-completion booleans and elapsed time.
Never emit raw origin/hash/index/timestamps/paths/arguments/receipt/credential contents. Require
one callback with both reads completed, exact signature and byte[] return type. One own GUID root,
reject existing/reparse parent; stream using/finally, original guarded fixture cleanup remains.
Failures expose up to4 inner exception TYPES and3 method/IL frames each, never Message/Data/
ToString/source paths. Bridge loading itself has a distinct PowerShell phase.

All32 scenarios, original expected outcomes/assertions/three reads/timeouts/coverage/order remain
unchanged. Original fixture read-only proof diagnostics retained. No receipt rewrite, hash-only
ownership, Menu deletion, additional retries or new rollback phase. A probe error cannot yield PASS.
Local static/signature/parity/secrets/public checks are not C# compilation/PowerShell/native proof.
New bridge execution remains NOT_RUN until exact new CI. Root mechanism remains UNKNOWN.
One new guarded-source automatic CI, no parallel same-SHA rerun. Next read its precise probe facts,
then only a minimal PROVEN production/fixture correction. No next feature before actual PASS.
UNKNOWN incidents/BLOCKED_TRANSFER/no delivery retry/scores unchanged. No live shortcuts/install/
services/network/security/ACL/credentials/VPS/release/uploads.

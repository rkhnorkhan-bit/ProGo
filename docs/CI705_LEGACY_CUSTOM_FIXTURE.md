## CURRENT CI705 fixture-only COM correction; new Windows verification pending

Baseline source b4327947646753ec4b20eef29bc99786b2add322, tree966ac450a99b4c785b9a2fc61021bea866d91718;
checkpoint106746d5d18fc52ef05091650ca3e1d3a1a8dc22 clean/preserved. PR88 head independently matched,
only root active; no active Git/compiler/PowerShell operation. New source CURRENT_TREE (git rev-parse HEAD),
reviewed local code c05b64105486b80a4603e40a6d02cc0b00fb3b88. Old worktrees untouched.

Full CI705 logs independently read: exact merge e371086cdaecf4773b188973e1f0d8414f8617ec.
Installer121253889508 FAILURE, Core121253889415 SUCCESS, Ubuntu121253889440 SUCCESS,
aggregate121263428068 FAILURE correctly rejecting installer failure despite matching all3 SHAs.
Run https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40221509929 . NOT cancellation.
All prior29 installer suites PASS; new Legacy19 absent-preserved PASS, custom-preserved ERROR,
remaining17 NOT_RUN. InstallWithStartupEvidence COM E_FAIL at Shortcuts-ProGo.ps1:26 (19:09:28 UTC).
Core Desktop6394/Instance30/Maintenance126/Shutdown47/HomeVPN4816/Portable91 PASS; Python38 PASS/2 SKIP;
Ubuntu aggregate12/operation16/profile13/provisioning PASS. Public395/44 PASS. Pack runner-only,
uploads/prune SKIPPED; runner ZIP is NOT a globally successful candidate, no delivery or PC acceptance.

Proven fixture mismatch: three cases wrote plain text into Legacy .lnk then requested migration.
MigrateLegacyWithEvidence -> ObserveLegacy(true) -> Matches -> IPersistFile.Load has no COM catch;
plain text is not a loadable Shell Link. Earlier custom Menu fixtures suppressed Menu creation/migration;
they do not establish that malformed Legacy text should load. Native fail-closed contract unchanged.
Only fixture seeding changed: existing native ApplicationShortcuts creates real .lnk to an owned
synthetic foreign-app/ProGo.exe, moved to Legacy before actual migration. Native Matches must load
and recognize only the foreign fixture target, not current install; hash and preserved/nonmatched
receipt must survive migration. Same-bytes-replaced and legacy-changed retain original negative
faults and all strict native-origin/refusal/assertions. No retries/sleeps/timeouts or runtime changes.
See CI705_LEGACY_CUSTOM_FIXTURE.md. New19 and original29 cases/order/deadlines/aggregate preserved.

Local review/static/signatures/parity/reverse diff/aggregate12/secrets/public-content/JSON/diff PASS.
New Windows Legacy19/full30/exact-SHA aggregate NOT_RUN until new automatic CI final logs.
ONE expected-HEAD guarded publication and automatic PR CI only; no rerun/dispatch/duplicate/full wait.
Next SAME new CI all4 final logs/exact merge/Legacy19/full30. Verified Legacy bytes capture remains
BLOCKED until whole CI PASS, then separate stage. No Menu/Legacy mutation or next functional stage.
UNKNOWN incidents (Python startup/rebind/receipt sharing/repair timeout/setup retry) retained;
BLOCKED_TRANSFER/no retry unchanged. All31/nested55 scope, weights and scores retained delta0.
No live install/shortcuts/services/network/ACL/security/credentials/VPS/release/uploads/transfer.

Acceptance: real valid customized Shell Link is preserved with exact bytes and native origin; equal-byte replacement remains refused. All19 cases must actually execute before declaring Legacy19 PASS. No production exception handling or ownership policy changes.

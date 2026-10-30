## CURRENT CI706 fixture reflection type correction; Windows verification pending

Baseline source2c1d1c8a109b7a0c0d0d43c70719830a7081f6f7/tree58449213c2fcadba2513b52beb6f03ad0eebc915;
checkpoint1953371687acc051519441d1a73d15ec692d2a6a clean/preserved. PR88 independently exact baseline/open/unmerged;
sole root/no Git/compiler/PowerShell writer; no AGENTS found. New source CURRENT_TREE (git rev-parse HEAD),
reviewed local code785b830e3fafc72215aad8604d6ef9cc6cc5d0cc. Old worktrees preserved.

Full CI706 four logs independently read at exact tested merge ed297af988418b6d50d6c56d1a1ee8f0eafcdcf6:
Installer121266462003 FAILURE (718lines), Core121266462158 SUCCESS (15409), Ubuntu121266462237 SUCCESS (607),
aggregate121275705073 FAILURE (168), correctly rejects installer despite identical tested SHAs.
Run https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40225698283 . Not timeout/cancellation.
Prior29 installer suites PASS; Legacy19 absent-preserved PASS, custom-preserved ERROR, remaining17 NOT_RUN.
At20:00:46 UTC InstallerLegacyRollbackAdmissionTests.ps1:43 Invoke ArgumentException: PSObject cannot convert to String.
Matches native oracle assertion did not complete. Prior real-shortcut COM correction is NOT yet confirmed.
Core Desktop6394/Instance30/Maintenance126/Shutdown47/HomeVPN4816/Portable91 PASS; Python38 PASS/2 SKIP.
Ubuntu aggregate12/operation16/profile13/provisioning PASS. Pack runner-only/uploads/prune SKIPPED,
no accepted global candidate/delivery/install/PC acceptance. No PASS inherited by new source.

Proven fixture-only reflection boundary bug: untyped argument array supplied PSObject where CLR String was required;
original log does not identify the failing element index. Both new Invoke calls now share an explicit System.String[] with String
conversion; CLR array element type cannot hold PSObject. Before Invoke, assert runtime array type/count,
element type String and both reflected parameter types String. Keep native assertion foreign target true/
current target false and exact bytes/preserved evidence unchanged. No catch/retry/sleep/timeout change.
All19 scenarios/all29 old suites/runtime/scripts/workflow unchanged. Existing local environment has no
PowerShell/.NET compiler/runtime, so actual CLR type/native checks NOT_RUN locally, not simulated PASS.
Local exact reverse diff/review/static/signatures/parity/aggregate12/JSON/secrets/public-content/diff PASS.
See CI706_LEGACY_REFLECTION_TYPES.md. One expected-HEAD guarded push/one automatic CI only.

Next SAME new CI all4 full final logs/exact merge/Legacy19/full30. Legacy original-bytes capture remains
blocked until full CI PASS, separate stage only. All31/nested55 approved scope/weights preserved; delta0.
UNKNOWN Python/rebind/sharing/repair/setup incidents and BLOCKED_TRANSFER/no retry remain open.
No live install/shortcuts/services/network/security/ACL/credentials/VPS/release/uploads/transfer actions.

Acceptance: all19 cases run with native oracle and unchanged strict ownership/origin/bytes/refusal guards; complete all4 exact-SHA aggregate before any next functionality. The new narrow type assertion fails explicitly before invoking native Matches, rather than accepting an invalid fixture.

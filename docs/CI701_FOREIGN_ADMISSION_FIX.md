# CI701: missing foreign-admission injection — bounded test-only fix

Exact source6116754d10f689db057c4719d644760be8019193, mergeba6d1cf978eb05bcf81042100080fb54e58b128f.
Full installer log121215556129: first13 mutation cases PASS; foreign-admission expected blocked,
observed not-applied (helperline13), remaining5 NOT_RUN. Full27 not PASS. Core/Ubuntu succeeded,
exact-SHA aggregate correctly failed on installer result. Core runner package is not a successful
whole-CI candidate; no uploads or owner acceptance.

Cause: test line82 uses `$scenario -eq 'foreign-admission', ...` with six scalar names on its right,
introduced when extending the scenario-list text. For a scalar left operand `-eq` is an equality
comparison, not membership in that right-hand array; the intended injection branch is not selected.
The intended unknown sibling file is consequently absent, while the existing own admission and
physical Menu remain valid/unapplied. Production GetMenuMutationContext correctly scans any foreign
installer-menu-admission prefix and throws before write; GetMenuMutationState translates it to
blocked. The log's not-applied is consistent with the actual unmodified fixture, not proof that
production accepts a real conflicting file. Runtime and native code require no change.

Fix only the fixture: exact scalar case selector; independently assert marker existence iff the
case is foreign-admission, including its absence in all neighbouring cases. Preserve the original
expected blocked, three restart readings, all19 scenario names/order, all previous assertions,
whole-case file/hash checks and cleanup. Add explicit OpenMenuMutationAdmission rejection when the
real marker exists and verify exact own admission, foreign marker and Menu hashes stay unchanged.
The new guard would fail immediately if the old injection condition returned; the explicit call
also exercises the production pre-mutation refusal rather than only its read-only classification.
No expected state is changed; no native identity/origin/hash, coverage, timeout or gate is weakened.

Review scans neighbouring scenario predicates for scalar equality against comma lists; none remain.
Existing Legacy/post-Startup/fault/race cases remain in exactly the same19-case list, with their
original assertions and later Windows execution required. Local static parity/aggregate12/content
checks cannot establish PowerShell/native19 PASS: there is no local Windows or PowerShell runtime.
One automatic exact-source CI will supply that proof; no rerun/blind retry. No next function until
all27 suites/Core/Ubuntu and complete exact-SHA aggregate succeed.

UNKNOWN Python startup/rebind/sharing/repair/setup incidents and BLOCKED_TRANSFER remain unchanged.
No install, live shortcuts, service, ACL/security/network, credentials, VPS, uploads or release action.

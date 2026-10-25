## CURRENT Menu failure-handler integration candidate; CI703 fully verified

Source is CURRENT_TREE (`git rev-parse HEAD`), reviewed local code `4ac32450e72522699a09e0c07906c408165859f7`.
Baseline source `9b784cd4804bf1029088d1f5d48c97d6cf5cb749`, exact merge/build
`93d27c362804592b34ad04faf12ef053034360b8`; prior checkpoint
`bad64b6f911a54c7494fcf31225b06ef49d8e6c0` clean/preserved. PR88 exact HEAD/open draft/unmerged,
sole root writer/no other Git/build/pwsh operations independently verified; old worktrees untouched.

[CI703](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40214882554) complete logs independently
read: Installer121233748474/687lines, Core121233748509/15409, Ubuntu121233748448/607,
aggregate121241298416/167; all4 SUCCESS, all3 tested_sha exact merge93d27c36. Installer28:
Result19/Admission19/Owned18/Menu32/Publication6 and prior installer suites PASS. Core Desktop6394/
Instance30/Maintenance126/Shutdown47/HomeVPN4816/Portable91 PASS; Python38 PASS/2 SKIP (not PASS).
Ubuntu aggregate12/operation16/profile13/provisioning PASS. Public-content391source/44release PASS.
Runner-only ZIP668019bytes SHA256 `345a6d0bd415d37742ea3c822a03f3970f3284f1d97e86dd85d031ef302b0ee1`;
pack SUCCESS, uploads/prune SKIPPED. Delivery/install/PC acceptance NOT_RUN. Installer log proves
real execution about17:07-17:36 UTC (1765368ms for suites); earlier pending metadata was contradictory,
not proof of a hang. No new timeout incident or extra CI justified from that metadata.

One bounded F23 production integration: existing installer catch invokes Menu failure adapter AFTER
Startup failure settlement, inside retained maintenance ownership and before unchanged finally.
Fresh Menu mutation requires actual verified Startup result plus current full Menu context and no
existing Menu result/admission. Existing Menu admission ALWAYS chooses read-only settlement; not-
applied, corrupt, conflicting or missing evidence blocks rather than replaying deletion. Accepted
Menu deletion with missing result can publish only verified Menu-only observation; existing valid
result is observed, corrupt/partial existing result is never rewritten. Original exception rethrown,
no Install OK/Launch/full auxiliary rollback; runtime remains new-verified pending Legacy/full outcome.
Legacy state is preserved, never restored or relabeled. See F23_MENU_FAILURE_INTEGRATION.md.

Code4files250add2del: new helper/ordered catch invocation/scoped log, new19-case actual-body fixture,
29th suite, two additive lines isolating new Menu dependency in historical Startup15. Every old
Startup15 assertion still runs through real Startup path; new19 exercises real Menu adapter. Native
origin/identity/hash/admission/result functions, old error/Stop/runtime restore/Launch/finally bodies,
all other tests/workflow/aggregate/deadlines unchanged. New19 covers fresh/preserved/Legacy paths,
missing/partial receipt, changed/replaced Menu, snapshot, persist failures, Startup refusal, lock,
Stop refusal, and crash before deletion/after deletion/after result plus corrupt records. Exact API
call counts and repeat file/byte inventory prove no replay; only exact new admission/result allowed.
Owned GUID/parent/reparse guards, original error/private/foreign bytes, stage/lease/handle cleanup.

Local review/static/signatures/parity/exact reverse diff/aggregate12/JSON/diff/secrets/public-content
PASS. PowerShell5.1 parse/Windows MenuFailure19/full29/new complete exact-SHA aggregate NOT_RUN.
CI703 PASS is not inherited by changed code. One expected-HEAD guarded push/automatic CI only;
exact source/run preserved in post-publication checkpoint/PR. Next SAME new run all4/final logs/
exact merge/new19/whole29/retainedResult19/Admission19/Owned18/Menu32/Publication6/Core before next stage.
Then separate agreed Legacy rollback prerequisite; no Legacy mutation or full auxiliary outcome now.
All approved31/nested55 scope retained: Legacy/full auxiliary/crash/runtime recovery/retention,
PGA04 explicit opt-in OFF/installer-updater fallback/immutable identity/final F23 package-regression-
owner acceptance, remaining F31/portability/UI/signing and approved commercial scope remain open.
Menu integration alone is not production readiness. UNKNOWN incidents/BLOCKED_TRANSFER/no retry,
fixed weights/scores delta0 retained. No live shortcuts/install/services/network/ACL/security/
credentials/VPS/release/uploads/delivery retry or second functional stage.

## CURRENT published durable Menu result / CI703 in progress

Source `9b784cd4804bf1029088d1f5d48c97d6cf5cb749`, tree `69bf564ed5e3bc9d67fa1ea39f41bec608358620`, parent
`d391473bb343439e868c3cd3561d03efbb5761a4`; reviewed codee2b8990/freeze444b4a6e3bbe6c4eab35a0f3c69c6b5ff0a6cdf9.
Expected-HEAD non-force PR88 update, fetched exact commit/parent/tree equality verified. Prior local
checkpointbd3f6d7d12acc76a83e8458764d4833a11144f23 preserved clean. ONE automatic pull_request
[CI703](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40214882554), run40214882554, exact source9b784cd,
observed in_progress/null. New exact tested merge/final logs not yet read. No rerun/dispatch/duplicate/
full wait. Local post-publication checkpoint only, NOT pushed to avoid a second CI.

Public diff7files483add14del: production helpers/two stricter Menu outcome guards +new19-case fixture/
28th suite, four evidence/state docs. Durable Menu result only: exact bounded pinned admission,
identity/receipt/admission-hash/physical reproof, CreateNew/WriteThrough/Flush/readback/reproof,
independent result reader. False FullRollback/InstallSucceeded/MutationAllowed; no new native mutation,
restore/replay, failure-handler integration or Launch/retry. Existing native/admission/failure-handler/
all old tests/deadlines/27 calls/aggregate/workflow/upload guards unchanged. New test19 covers receipt/
admission/conflict/tamper/partial/persistence and external changes during/after write, duplicate
publication refusal and own handle cleanup; legacy-removed/post-Startup/private/foreign guards retained.

Local review/static/signatures/parity/reverse diff/aggregate12/secrets/public391source0release/JSON/
diff PASS. PowerShell5.1 parser/Windows MenuResult19/whole28/Core+Ubuntu/full exact-SHA aggregate
NOT_RUN on this new source. CI702 baseline all4/whole27/MenuMutation19/Owned18/Menu32/Publication6/
previous suites actual PASS independently verified at merge1788e602, not inherited by new code.
Runner-only pack is not transfer/install/owner acceptance; no delivery performed.

Next SAME CI703 final logs/exact tested merge/all4/MenuResult19/whole28/retained19/18/32/6/Core/
Ubuntu/strict aggregate; then separate Menu failure-handler integration. Entire agreed scope retained:
Legacy/full auxiliary outcome/crash/runtime recovery/retention/PGA04/standalone-updater fallback/
immutable version and final F23 package/regression/owner gates; remaining F31/portability/UI/signing
and full commercial requirements stay open in registry. Menu completion is not production readiness.
UNKNOWN incidents/BLOCKED_TRANSFER/no retry and unchanged scores/weights delta0 preserved. No live
shortcuts/install/services/network/ACL/security/credentials/VPS/release/uploads or second stage.

## CURRENT durable Menu result candidate; CI702 full PASS independently verified

Source is CURRENT_TREE (`git rev-parse HEAD`), reviewed local code `e2b89907e5f246e730a511c0a18710e157636486`.
Baseline source `d391473bb343439e868c3cd3561d03efbb5761a4`, exact tested merge/build
`1788e602609781c4d1c95df46b11c8ddb804cf1e`; prior checkpoint
`bd3f6d7d12acc76a83e8458764d4833a11144f23` clean/preserved. PR88 exact HEAD/open draft,
unmerged; sole root/no other active writer or Git/build/pwsh operations independently checked.

[CI702](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40211667574) complete final logs read:
Installer121224005601/665lines, Core121224005781/15409, Ubuntu121224005785/607,
aggregate121231234015/167; all4 SUCCESS, all3 tested_sha match exact merge1788e602. Actual
Installer27/MenuMutation19 (including foreign-admission and all five previously NOT_RUN),
OwnedMenu18/Menu32/Publication6/all prior installer suites PASS. Core Desktop6394/Instance30/
Maintenance126/Shutdown47/HomeVPN4816/Portable91 PASS; Python38 PASS/2 SKIP (not PASS).
Ubuntu aggregate12/operation16/profile13/provisioning PASS. Public-content389source/44release PASS.
Runner ZIP667566bytes SHA256 `f14dbcdd4bea6c37335f5ca48b035f21608f648e701326eb8959425f10fe922f`;
pack SUCCESS, all uploads/prune SKIPPED. Delivery/install/PC acceptance NOT_RUN. CI701 fixture
selector correction is scoped Windows19 PASS at this exact baseline, not a runtime fix or new-source PASS.

One bounded F23 stage: durable Menu-only result after strictly verified read-only original-observed
settlement. NO failure-handler integration, Legacy rollback, retention or general runtime recovery.
GetMenuResultProof pins exact admission, revalidates full context/identity/trusted locations/snapshot/
runtime/current bytes-or-absence and canonical receipt binding, derives admission/receipt hashes.
CreateNew/WriteThrough/Flush(true)/exact bounded UTF8 readback and repeat proof before completion;
existing foreign/owned result never overwritten. Independent pinned result reader repeats physical
proof, rejects false flags/changed external state. Stored Phase=menu-original-observed, FullRollback,
InstallSucceeded and MutationAllowed=false, RuntimeState=new-verified. Metadata only: never native
mutation/replay/recreation or authorization to Launch/retry. Default automatic pending/terminal gates
block ANY Menu result; foreign result suffixes reject in Menu context. Old native identity/origin/
bytes/admission-before-mutation and all previous regression bodies unchanged.

Code3files196add2del: Menu result helpers/two conservative guards, new19-case fixture, 28th suite.
New19: created/preserved custom/absence, not rolled back, foreign Startup/Menu, snapshot/runtime,
persist fail/short write/conflict/tamper/missing origin, legacy-removed/post-startup, damaged admission,
foreign outcome, external replacement during write and after publication. Existing valid result
cannot be overwritten; result/admission/receipt exclusive reopen proves cleanup; all existing bytes/
private/foreign resources preserved. All27 old calls/deadlines/aggregate/workflow/upload guards retained,
final summary follows all28. Local review/static/signatures/parity/exact reverse diff/aggregate12/
JSON/diff/secrets/public-content PASS; Windows PS5.1 parser/MenuResult19/full28/new complete exact-SHA
CI NOT_RUN. No inherited PASS on new code. See F23_DURABLE_MENU_RESULT.md.

One guarded expected-HEAD publication and automatic CI only; exact SHA/run afterward in PR/local
checkpoint. Next SAME new run full logs/exact merge/all4/new19/whole28/retained19/18/32/6. Then
separate Menu failure-handler integration, respecting original install error/no Launch/no full
auxiliary rollback. Entire approved scope retained: Legacy/full auxiliary outcome, crash/runtime
recovery/retention, PGA04 explicit opt-in OFF, installer/updater fallback and immutable payload,
final F23 regression/package/owner acceptance, remaining F31/portability/UI/signing and full approved
commercial F30 gates stay in registry. Menu completion is NOT production completion.
UNKNOWN Python/rebind/sharing/repair/setup incidents and BLOCKED_TRANSFER/no retry retained; scores/
weights unchanged delta0. No live shortcuts/install/services/network/ACL/security/credentials/VPS/
release/uploads or binary transfer. No second functional stage in this pass.

## CURRENT published CI701 fixture selector correction / CI702 in progress

Source `d391473bb343439e868c3cd3561d03efbb5761a4`, tree `9872a8f21693fc8770c44186a77f1cca068116fc`, parent
`6116754d10f689db057c4719d644760be8019193`; reviewed local code23aaf423559b8c3b7daa97906d62e7a9bdfa5c5d,
freeze29817f962a444c4ca8cc5b64ddfd1b5750e6f1b3. Guarded expected-HEAD non-force PR88 update;
fetched exact parent/tree equals reviewed freeze. Prior checkpoint2e55dc18 preserved clean.
ONE automatic pull_request [CI702](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40211667574), run40211667574, exact source,
observed in_progress/conclusion null. No rerun/dispatch/duplicate/full wait. New tested merge/final
logs not yet read. Local post-publication checkpoint only, intentionally NOT pushed to avoid extra CI.

Public diff5files284add11del: ONE fixture14add1del plus4 evidence/state documents270add10del.
Fix exact scalar foreign-admission selector, physically assert marker only in that case, explicitly
prove admission refusal and own/foreign/Menu byte preservation. Production/native/gates/identity/
origin/bytes/replacement unchanged. All19 names/order/expected blocked/old asserts/timeouts/cleanup,
all27 suites/strict aggregate/workflow/upload guards preserved. Full local review/static/signatures/
parity/secrets/public389source0release/JSON/diff and aggregate12 PASS. New Windows19/full27/complete
exact-SHA CI NOT_RUN. No runtime fix or new function claimed.

CI701 all4 final logs independently read: exact mergeba6d1cf9, Installer19=13PASS/1FAIL/5NOT_RUN,
Core+Ubuntu SUCCESS, aggregate correctly FAIL. Old Owned18/Menu32/Publication6 PASS, runner ZIP
not a successful whole-CI candidate. Proven failing fixture did not inject actual foreign sibling
because scalar -eq was incorrectly supplied an array of six names. Expected blocked preserved.

Next SAME CI702 final logs/exact tested merge/all4/whole19 incl five formerly unreached cases,
retained18/32/6/all27/Core/Ubuntu/strict aggregate before next function. UNKNOWN incidents and
BLOCKED_TRANSFER/no retry preserved; scores/weights unchanged delta0. No live install/services/
network/security/ACL/credentials/VPS/release/uploads or binary delivery. Original failure/noLaunch/
private-file/ownership safeguards unchanged.

## CURRENT CI701 foreign-admission fixture fix; new exact Windows proof NOT_RUN

Candidate source is CURRENT_TREE (`git rev-parse HEAD`); reviewed local code `23aaf423559b8c3b7daa97906d62e7a9bdfa5c5d`.
Baseline published source `6116754d10f689db057c4719d644760be8019193`, tested merge
`ba6d1cf978eb05bcf81042100080fb54e58b128f`; prior local checkpoint
`2e55dc18e6270350ea127770bb9f62ae753c604e` preserved clean. PR88 independently verified exact
HEAD/open draft/unmerged; sole root writer/no active Git/compiler/pwsh process. No operations repeated.

[CI701](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40208853093) complete final logs read:
Installer121215556129/665lines FAILURE, Core121215556250/15409 SUCCESS,
Ubuntu121215556301/607 SUCCESS, aggregate121221499795/168 FAILURE. Exact tested mergeba6d1cf9
in all4 logs. Aggregate correctly rejects installer failure even with all3 SHA outputs identical.
Menu mutation19: first13 PASS, foreign-admission FAIL, last5 NOT_RUN (legacy-removed, post-startup,
startup-result-changed, legacy-appeared, replace-after-persist). Old OwnedMenu18/Menu32/Publication6
and earlier installer suites PASS. Core Desktop6394/Instance30/Maintenance126/Shutdown47/HomeVPN4816/
Portable91 PASS, Python38 PASS/2 SKIP; Ubuntu prior suites PASS. Core pack runner-only667573bytes,
SHA256 `05fb64d901b6f2340443ae1aa2877142c68d2d2e076f16abdaa5bc41269b5211`;
all uploads/prune SKIPPED. Overall CI FAILURE: runner ZIP is not a globally green candidate;
transfer/install/owner acceptance NOT_RUN. No old PASS transferred to changed fixture.

Proven fixture selector defect at InstallerMenuMutationAdmissionTests.ps1:82: scalar scenario -eq
an unintended six-element array instead of one foreign-admission name. The intended conflicting
sibling is not injected. The production context scans/rejects any foreign installer-menu-admission
prefix; absent conflict and valid own admission physically remain not-applied, matching the log.
Production code/native/identity/origin/hash/replacement/gating unchanged. See CI701_FOREIGN_ADMISSION_FIX.md.

Minimal test14add1del: exact scalar -ceq foreign-admission; assert physical marker presence ONLY in
that scenario and absence in all18 neighbours. Original expected blocked/3 repeated read-only state
checks preserved. Added explicit OpenMenuMutationAdmission conflict refusal and hashes proving own
admission, foreign record and exact Menu bytes preserved; existing whole-case file/byte inventory
and cleanup remain. Same19 names/order/all assertions/deadlines/coverage, same27 suites and strict
aggregate/workflow/upload guards. No new features or later phase.

Local PASS review/static/signatures/parity/exact reverse diff/neighbor selector scan,
aggregate12, JSON/diff/secrets/public-content. Windows PowerShell/native19/full27/new complete CI
NOT_RUN (no local Windows/pwsh). One guarded expected-HEAD push/automatic CI only after final checks;
exact source/run preserved in post-publication checkpoint and PR. Next SAME new run all4/final logs/
exact merge/full19+retained18/32/6/whole27; no new function before complete proof. UNKNOWN prior
incidents/BLOCKED_TRANSFER/no retry and fixed scores/weights retained, delta0. No live shortcuts/
install/services/network/ACL/security/credentials/VPS/release/uploads or delivery retry.

## CURRENT published durable Menu admission / CI701 in progress

Source `6116754d10f689db057c4719d644760be8019193`, tree `dd1654b41753755250d380b2869b6807b810ea61`, parent
`3dccd1b7d13eb5c3d96af63444e29395a51f0576`; reviewed code4c4f5aefaf2a663cdea8e8499143f76b2bad34e7,
final freeze6920d540cce03bbccdc39bec1e6f3c18cfd6ab9e. Expected-HEAD non-force PR88 update,
fetched exact commit/parent/tree equality confirmed. Prior checkpointaf4f53b8 preserved clean.
ONE automatic pull_request [CI701](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40208853093), run40208853093,
observed in_progress/conclusion null, exact source6116754. No rerun/dispatch/duplicate/full wait.
Exact new tested merge/final logs not yet read. Local post-publication checkpoint only, NOT pushed.

Public diff10files585add16del: five code/test302add4del plusfive docs283add12del. Durable Menu
CreateNew/WriteThrough/Flush/readback/reproof/pin before unchanged native deletion; read-only crash
settlement; exact canonical post-Startup evidence; Legacy-removed observation; strict default pending
gates. No automatic failure-handler/Menu terminal outcome/Legacy rollback/retention/runtime recovery.
Early Installer summary moved after ALL27 suites. Old assertions/deadlines/aggregate/workflow kept.

PASS local: review/static/signatures/parity, original native mutation/failure-handler exact reverse
diff, reconstructed Startup canonical field order, existing18 assertions+26calls retained, JSON/diff/
secrets/public388source0release, aggregate12. NOT_RUN on new source: PowerShell5.1 parser/C# compiler/
WindowsMenuMutation19/retainedOwned18/full27/Core+Ubuntu/complete aggregate. CI700 baseline all4
exact merge7419458f/Installer26/Owned18/Publication6/Menu32/previous suites PASS independently read;
old success not inherited by new tests. Pack only runner/no delivery/PC acceptance.

Next SAME CI701 final logs/exact tested merge, all4 jobs+strict aggregate/Installer27/new19/retained18/
32/6 and Core regressions. Then one separate scoped durable Menu result/error-handler integration
slice after proof; Legacy/full auxiliary outcome, retention/runtime recovery/PGA04/updater/final
package gates remain open. UNKNOWN incidents and BLOCKED_TRANSFER/no retry retained; weights/scores
unchanged delta0. No live shortcuts/install/services/network/ACL/security/credentials/VPS/release/uploads.

## CURRENT durable Menu admission/read-only settlement candidate; CI700 fully verified

Containing source is CURRENT_TREE (`git rev-parse HEAD`); reviewed local code `4c4f5aefaf2a663cdea8e8499143f76b2bad34e7`.
Baseline published source `3dccd1b7d13eb5c3d96af63444e29395a51f0576`, tested merge/build
`7419458f258f77d10619b9aa920a5db0b1454a9f`; prior checkpoint
`af4f53b8d1bd46406bc031d9c66a09ab41f3c4b0` clean/preserved. PR88 exact expected HEAD,
open draft/unmerged, sole root writer/no active Git/build/pwsh operations independently verified.

[CI700](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40204997216) complete logs read:
Core121203935615/15409lines, Installer121203935657/643, Ubuntu121203935411/607,
aggregate121211271493/167; all4 SUCCESS, all3 tested_sha exact merge7419458f. Installer26:
OwnedMenu18/Publication6/Menu32 actual PASS including legacy-removed/post-startup/refusal. All prior
installer suites PASS. Core Desktop6394/Instance30/Maintenance126/Shutdown47/HomeVPN4816/Portable91
PASS; Python38 PASS/2 SKIP (not PASS). Ubuntu aggregate12/operation16/profile13/provisioning PASS.
Public-content386source/44release PASS. Runner-only ZIP665896bytes SHA256
`abfaed7e79653b246fa4c107669a0f566cfbc44bba12573f715ad704bd508bac`; pack SUCCESS,
all uploads/prune SKIPPED. No transfer/install/PC acceptance. Early Installer26 summary was before
OwnedMenu18, but actual later suite return and strict aggregate succeeded: reporting order defect,
not a bypass. This candidate moves ONE final summary after all27 suites and Stopwatch.Stop.

One bounded F23 slice: durable Menu admission before existing exact-handle native mutation,
plus read-only restart settlement classification. No automatic failure-handler binding, Menu
terminal outcome, Legacy rollback, retention or general runtime recovery. See
F23_MENU_MUTATION_ADMISSION.md. CreateNew/WriteThrough/Flush(true)/strict bounded UTF8 exact readback,
repeat physical/context proof, pin through native settlement/finally. Existing admission reused
only by explicit caller after exact content/current ownership proof; corrupt/partial/conflicting
records never rewritten. Intent does not authorize replay or restore. Read-only settlement returns
not-applied/original-observed/blocked, independently rechecking snapshot/runtime/admissions,
receipt/identity/trusted locations, native origin/bytes where present and exact absence where gone.
Post-Startup requires exact canonical Startup admission AND observed-result records against
independently checked receipt/locations, plus actual physical Startup absence. Legacy-removed
requires original receipt linkage/owned origin/replacement hash and actual Legacy absence; no
Legacy restoration is claimed. Default automatic terminal/new-install gates block any Menu admission.

Code5files302add4del: native observation only, bounded Menu helpers/admission pin/strict default
gates, reporting-order correction+new27th suite, new19-case isolated fixture, only7 additive
OwnedMenu18 assertions/accounting lines. Existing native mutation/origin/publication, all old18
assertions, all other tests, failure-handler, workflow/aggregate/deadlines/upload guards unchanged.
New19 cases cover before/after crash, preserved, persistence/short write, tamper, missing/conflicting
record, runtime/snapshot replacement, sharing denial, cancel-before/after persist, post-Startup,
changed Startup result, legacy-removed/foreign Legacy and equal-byte replacement DURING persist.
Repeated read-only classification/settlement creates no file and changes no byte; exact original
resources and settings/vault/keys/foreign data preserved. No whole auxiliary rollback/success/Launch.

Local review/static/signatures/parity/secrets/public-content/JSON/diff and aggregate12 PASS.
PowerShell5.1 parser/C# compilation/Windows new19+retained18/full27/full exact-SHA aggregate NOT_RUN.
Old CI700 PASS not inherited by new tests. ONE expected-HEAD guarded publication/automatic CI only;
exact source/run added to PR/local checkpoint after observation. Scores/weights unchanged delta0.
UNKNOWN incidents (Python/startup, rebind, receipt sharing, repair timeout, setup cancellation) and
BLOCKED_TRANSFER/no retry retained. No live shortcuts/install/services/network/ACL/security/
credentials/VPS/release/uploads. Next SAME new CI exact merge/all4/27/new19 before another stage;
then separate scoped durable Menu result and failure-handler integration. Legacy/full auxiliary
outcome, retention/runtime recovery/PGA04/updater/final package and owner acceptance remain open.

## CURRENT published owned Menu rollback / CI700 in progress

Published source `3dccd1b7d13eb5c3d96af63444e29395a51f0576`, tree `ac0e42b3346bab71b0fe59bc4ad584efc3b2d7ca`, parent
`71d972a263701e03541566f7054211ed14d629ac`. Local code6675944d3ffdb4cb4ab8d6260920e0d92be232c3
(exact code SHA below resolved by git; public tree equals reviewed freeze069996d0996aaff733986632c00fef61abcf4a20).
Expected-HEAD non-force PR88 publication verified; fetched parent/tree exact, old worktrees retained.
ONE automatic [CI700](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40204997216), run40204997216, pull_request, observed in_progress/null;
no rerun/dispatch/duplicate/full wait. Exact new tested merge not yet read, no new Windows PASS.
Public diff8files508add33del: native28 +installer27 +suite7/1 +new127-line18-case fixture,
new50-line contract and3 synchronized state docs. All old test bodies/25 suite calls and workflow
unchanged. Full review/static/signatures/parity/secrets/public386source0release/JSON/diff PASS;
local aggregate12 PASS. Windows PowerShell/compiler/new native18/full26/Core+Ubuntu+aggregate
NOT_RUN at this exact new source. CI699 baseline full25/Publication6/Menu32 and all4 jobs PASS
independently verified, not transferred to new tests. No binary delivery/install/owner acceptance.

Next SAME CI700 final logs/exact tested merge: all26 Installer/new18/retained32+6/Core/Ubuntu and
strict aggregate required. Only after that a separate durable Menu pre-mutation admission/crash
settlement slice; no automatic failure-handler integration/full auxiliary rollback/Launch in this
stage. Legacy/full outcome/retention/runtime recovery/PGA04/package gates remain open. Original
error/private files/preserved/custom/foreign objects and UNKNOWN incidents/BLOCKED_TRANSFER/no retry
retained. Scores unchanged delta0. No release/uploads/install/live/security/network/VPS actions.
Local post-publication checkpoint only; intentionally not pushed to avoid a second CI.

## CURRENT owned Menu rollback candidate; CI699 full PASS independently verified

This candidate identity is CURRENT_TREE (`git rev-parse HEAD`); reviewed local code `6675944d3ffdb4cb4ab8d6260920e0d92be232c3`.
Baseline published source `71d972a263701e03541566f7054211ed14d629ac`, tested merge
`9b8ea528802fbb52525a85a2a63dcec03f99d35f`; prior local checkpoint
`011cf8f8751c9085ef44052f48fb6c69bf4334fd` preserved clean. PR88 exact expected HEAD,
open draft/unmerged, sole root writer, no active Git/compiler/PowerShell operations verified.

CI699 complete logs independently retrieved/read: Core121192313083 (15410 lines),
Installer121192313269 (622), Ubuntu121192313308 (607), aggregate121199407503 (167).
[CI699](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40201119790) all4 SUCCESS;
all3 tested_sha equal exact merge9b8ea528; strict complete aggregate PASS. Installer25 suites,
Publication6 and FULL MenuAdmission32 PASS, including legacy-removed, post-startup,
post-startup-replaced and unproved-startup-absence. Core Desktop6394/Instance30/Maintenance126/
Shutdown47/HomeVPN4816/Portable91 PASS. Python40 reported:38 PASS,2 SKIP (not PASS).
Public-content384source/44release PASS. Runner-only ZIP665241 bytes SHA256
`d6bd9896deaa3b432c2fe1b5a859380565193a35a54e6cf13ddd853c9ec930a5`;
pack SUCCESS, all uploads/prune SKIPPED. No delivery/install/owner acceptance. CI698 CANCELLED
history retained; capacity split now exact-SHA verified, cancellation initiator still UNKNOWN.

One bounded F23 slice: explicit owned Menu rollback after existing verified admission. No automatic
error-handler binding, durable Menu mutation admission/outcome, Legacy rollback, retention or runtime
recovery in this stage. See docs/F23_OWNED_MENU_ROLLBACK.md. Snapshot/runtime/admissions/auxiliary/
identity/trusted locations revalidated under bounded pinned receipt; native operation rechecks FULL
origin (volume/index/creation) and bytes on exact READ|DELETE handle, denies replacement/write,
deletes only that handle, closes it, verifies actual Menu absence. Preserved/custom is no-op;
external replacement/equal bytes without ownership/conflicts refuse. Cancellation before mutation
propagates; accepted deletion settles before result, no cancellation-after-write false denial.
No Path Delete, receipt refresh, replay, restore, terminal success, retry authorization or Launch.

Code4files189add1del: two new methods, one new18-case real Windows fixture, one suite hook (26total).
All existing test bodies, installer/error handler, Startup/Legacy functions, native interop/origin,
workflow/deadlines/upload gates unchanged. New scenarios include legacy-removed and post-Startup
settlement, unproved Startup absence refusal, exact native replacement with equal bytes AND copied
creation time, lock/explicit Retry, cancellation, snapshot/runtime/trusted location guards via
existing admission; receipt/link exclusive reopen and exact whole-case inventory/private-byte checks.

Local PASS: exact reverse diff/review/signatures/parity, aggregate12, JSON/diff/public-content and
added secrets/PII scan. NOT_RUN: local PowerShell5.1 parsing/C# compilation/new native18/full26/new
complete exact-SHA aggregate (no local Windows/pwsh/compiler). No old PASS transferred to new tests.
Expected-HEAD guarded publication will trigger ONE automatic CI; source/run confirmed afterward in
local checkpoint and PR. Scores/weights unchanged delta0. UNKNOWN repair/sharing/Python/rebind/setup
incidents and BLOCKED_TRANSFER/no retry retained. No live install/services/network/ACL/security/
credentials/VPS/release/uploads. Next verify SAME new CI all4 jobs/full26/new18 before another stage;
then separate durable Menu pre-mutation admission/crash settlement and scoped result/error integration.
Legacy rollback/full auxiliary result/retention/runtime recovery and package acceptance remain open.

## CURRENT published CI partition/strict aggregate / CI699 queued

Published source `71d972a263701e03541566f7054211ed14d629ac`, tree
`56642ec4684e66162fe404c92199e5fcb6a6f54c`, parent48f079c79fb9dbb012dcf08eb75219bb17289127.
Reviewed local code7db31c9687dd53560fdd3521d8a4663ea6dc2138; final freeze74cd4348d15d2590c764bd10600107cf76a20ca3
includes explicit UTF8 GITHUB_OUTPUT for Windows PowerShell5.1; exact canonical tree fetched/verified.
Expected-HEAD guarded non-force PR88 update. ONE automatic pull_request
[CI699](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40201119790), exact source71d972a,
queued/null observed; no parallel rerun/dispatch/duplicate/full wait. Four jobs required: Ubuntu,
windows-build Core+relay, installer-tests all25, ci-complete exact-SHA strict gate. Upload/pack/
prune/events/permissions/concurrency unchanged, no binary transfer/public artifacts.

Diff9files648add51del: six CI/test/evidence417add29del plus3 synchronized state docs231add22del.
All original Core code and test bodies/deadlines/25 Installer invocations/order preserved; default
All still full. No production/native/Menu mutation changes. New per-suite timing and strict result/
SHA aggregate. Both Windows jobs keep40min; no test timeouts raised. Local aggregate12 +CLI2 PASS,
YAML/static/review/signatures/parity/diff/secrets/public384/0/JSON PASS. Windows dispatch/parser/
compilation/partitions25/native6/32/new full exact-SHA aggregate NOT_RUN. No partial-to-full PASS.

Baseline CI698 independently CANCELLED not FAILURE: full Windows10491/Ubuntu578 mergec1de408f;
Publication6 PASS, Menu29 PASS/final3 unconfirmed, later suites/pack NOT_RUN. Cancel cause UNKNOWN:
annotations endpoint connector HTTP400;40m10s vs40min only consistent, not initiator proof. Serial
capacity bottleneck independently measured Core1279s +incompleteInstaller1116s before late suites.
Do not label cancelled run assertion failure or full32 PASS. CI697 origin capture cause proven;
scoped fix6/29 passed at698, full32 acceptance remains pending.

Next SAME699 full logs/exact tested merge and ALL4 jobs/aggregate. Verify actual Installer25/
Publication6/full Menu32, Core/Desktop/Instance/Maintenance/Shutdown +relay/HomeVPN/Portable/Python/
content/pack; no full success if any skip/fail/cancel/missingSHA. Complete gate/whole run required,
not Core-only green. Branch protections not altered; pack runner-only not delivered/accepted.
Only after evidence may separate owned Menu rollback stage begin. F23 residuals/UNKNOWN incidents/
BLOCKED_TRANSFER/no retry retained; scores unchanged delta0. No live install/services/network/
security/ACL/credentials/VPS/release/uploads. Local post-publication checkpoint only, no extra CI.

## CURRENT CI698 CANCELLED; minimal exact-SHA CI partition/aggregate candidate

Containing source is THIS git tree (git rev-parse HEAD); reviewed local code
7db31c9687dd53560fdd3521d8a4663ea6dc2138. Baseline source48f079c79fb9dbb012dcf08eb75219bb17289127,
checkpoint8f556cef6c58611d3be22b81c367486b1fb22418 clean; PR88 exact HEAD/open draft/sole root writer/
no active Git/build/PowerShell verified; older worktrees retained. Full CI698 Windows10491/Ubuntu578
logs independently read, mergec1de408f34bc29903cb032a321e39776d839c61f. Windows121177870869 CANCELLED,
not FAILURE; Ubuntu121177870667 SUCCESS. Publication6 actual PASS, MenuAdmission29 actual PASS
including legacy-removed/origin/replacement refusal; final3 NOT_RUN/unconfirmed (post-startup,
post-startup-replaced, unproved-startup-absence). Full32 NOT PASS. Earlier installer/Startup15/14/13/
14/25/Desktop6394/Instance30/Maintenance126/Shutdown47 PASS; HomeVPN/Portable/relay/aggregate/
public-content/pack NOT_RUN. Capture fix scoped6/29 proven, no whole Menu/auxiliary acceptance.

Cancellation initiator NOT confirmed: raw jobs40m10s vs workflow40m, log operation canceled only.
Check-run annotations API rejected by connector HTTP400 INVALID_ARGUMENT, no bypass. Independently
measured bottleneck: Core throughUpdater1279s, incomplete Installer1116s =2395s before final3 and
mandatory relay/HomeVPN/content/pack. Budget already consumed regardless of cancellation initiator.
No runtime defect/flaky/AV assumption or deadline increase. See docs/CI698_PARTITIONED_CI.md.

Minimal CI-only split: default Test-ProGo Suite All preserves full coverage. Same25 installer calls
extracted verbatim/order into one canonical runner with safe per-suite timing. Suite Core runs all
old pre-tail checks then unchanged relay/HomeVPN/Portable/Python/content/runner pack. Suite Installer
on a separate clean Windows runner runs common hygiene/parse/build then exact25 suites, no Core/SSH
rerun. Each job records/verifies actual checkout vs GITHUB_SHA. Always aggregate requires exact
Ubuntu/Core/Installer set, every result success and exact same tested commit; missing/skip/cancel/
failure/wrong identity fail closed. Existing Windows40/Ubuntu12 deadlines retained; installer40,
aggregate5 metadata gate. No artifact transfer/cache/new uploads. Original upload/pack/relay/prune/
event/permission/concurrency bodies unchanged. Pack alone is not complete CI; branch protections
not modified. No tests/asserts/timeouts removed or weakened. No Menu mutation/production change.

Code/test/evidence6files417add29del plus3 synchronized state docs. Local aggregate12 PASS and CLI
success0/malformed1, YAML/review/static/signatures/parity/full old test bodies/secrets/public384/0/
JSON/diff PASS. Default All/partition dispatch Windows parser/compiler/native25/6/32/exact-SHA
complete gate remain NOT_RUN until new CI. New Python test appears in original relay discover too;
no old suite skipped. Both Windows jobs fresh-build own exact payload cheaply, no binary sharing.

One expected-HEAD guarded PR88 publication and ONE automatic CI; no parallel rerun/full wait.
Next SAME new exact run all4 jobs/full logs/tested merge/aggregate/Publication6/full Menu32/old suites.
Only after complete proof may next separate owned Menu rollback stage begin. All F23 residuals and
original error/no Launch/private settings/vault/keys unchanged. UNKNOWN unrelated incidents and
BLOCKED_TRANSFER/no retry retained; no score increase. No live install/services/network/security/
ACL/credentials/VPS/release/uploads. Older current headings below are historical and superseded.

## CURRENT published Menu origin fix / CI698 in progress

Published source `48f079c79fb9dbb012dcf08eb75219bb17289127`, tree
`83392835e29ef3b1ca5cc90af43a273e7328699b`, parent8caf03239ce6d762aa49a044dad1a3270fd979de.
Reviewed local code27cb4f79af3349096f19b82471f11339a8de8ae1, freezea50162d9ff8aa261a9ea150ead1da652bacef504,
canonical full tree fetched/verified identical. Expected-HEAD non-force guarded existing PR88 update.
ONE automatic pull_request [CI698](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40196340794),
exact source48f079c, in_progress/null observed; no dispatch/rerun/parallel/full wait. Public upload
guards OFF/release/prune ineligible; workflows unchanged. No artifacts delivered or installed.

Diff8files440add24del: five code/test/evidence files221add2del plus3 synchronized state docs219add22del.
Fix addresses PROVEN pre-rename Menu origin capture becoming stale when creation changes during
owned publication. Full native identity retains creation. Menu-only opt-in retains original handle,
proves volume/index continuity and bytes, pins published path/full post-origin before witness and
rechecks full identity/bytes/location afterward. Foreign replacement (identical bytes/copied creation),
partial evidence/witness errors fail closed; no blind receipt refresh or replay. Startup/noncapture/
Legacy/native deletion/failure/Stop/finally unchanged. No Menu rollback mutation/full auxiliary
outcome/success/Launch permission. Original32/all old assertions/timeouts byte-identical; new6 native
regressions before32. Local static/review/signatures/parity/diff/secrets/public380/0/JSON PASS.
No local compiler/Windows: C#/PowerShell/native Publication6/Menu32/prior exact-source suites NOT_RUN.
Independent baseline CI697 full10478/578: volume/index/bytesSame True, creation/originSame False,
probe completed once; origin capture defect proven, but new fix runtime PASS awaits exact CI.

Next SAME698 full final logs/exact tested merge/Publication6/Menu32 and reached previous suites.
After PASS only, next separate owned Menu rollback stage; durable Menu admission/result/error,
Legacy/full auxiliary/partial-receipt/retention/general runtime recovery still open. Unrelated UNKNOWN
Python/rebind/sharing/repair/setup incidents and BLOCKED_TRANSFER/no delivery retry preserved;
no score increase. No live install/services/network/security/ACL/credentials/VPS/release/uploads.
Local post-publication checkpoint only, no push/additional CI. Older headings superseded here.

## CURRENT CI697 proven Menu capture/publication-origin defect; minimal fix candidate

Containing source is THIS git tree (git rev-parse HEAD); reviewed local code
27cb4f79af3349096f19b82471f11339a8de8ae1. Baseline source8caf03239ce6d762aa49a044dad1a3270fd979de,
checkpointcee55133dd6d988f4fd7ca655069481240d84fe1 clean; PR88 exact head/open draft/sole root writer/
no active Git/build/PowerShell verified; older worktrees retained. Full CI697 Windows10478/Ubuntu578
logs independently read, merge5c270df2b6a130a439db2fab5491c3bc235c13dd. Windows121167474515 FAILURE,
Ubuntu121167474319 SUCCESS. Probe1call/bothReads/completed True, volume/index/bytesSame True,
creation/originSame False,11ms. Actual legacy-removed other proof gates True/originSame and durableMenu
False. Menu32:2PASS/1FAIL/29NOT_RUN; earlier installer/Startup15/14/13/14/25/Desktop6394/Instance30/
Maintenance126/Shutdown47 PASS; later HomeVPN/Portable/relay/aggregate/pack NOT_RUN.

Proven defect: existing Menu receipt callback captures full origin BEFORE native rename; the same
owned object's creation time changes at publication. Correct strict full-origin verifier rejects that
stale receipt. No hash-only or creation-removal workaround. Production uses the same EnsureLink as
completed native reproduction; no OS-internal cause beyond observed transition assumed. Old Python/
other incidents UNKNOWN, old PowerShell probe exact inner cause remains UNKNOWN, CLR probe now PASS
only as diagnosis (not whole Menu32). See docs/CI697_MENU_PUBLICATION_ORIGIN_FIX.md.

Minimal Menu-only opt-in: retain native original read handle across no-overwrite rename. Prove native
volume/index continuity on SAME handle, exact expected bytes and no reparse/directory; compute FULL
postpublication origin still including creation; pin independently supplied published path, compare
full identity/bytes before invoking receipt witness. Recheck full identities/bytes/location afterward.
External replacement (even identical bytes/copied creation), races, witness error/unknown results fail
closed without receipt or replay/overwrite. Saved receipts never refreshed blindly. Existing four-arg
witness/Startup/noncapture/Legacy/native deletion/failure/error/Stop/finally paths unchanged. No Menu
rollback mutation, full auxiliary outcome/success/Launch or other feature.

Code/test/evidence5files221add2del plus3 synchronized state documents. New Publication6 covers actual
owned rename creation change, foreign identical/different bytes, partial bytes, exact original witness
error, read-pin replacement race, cleanup. Only own temp file creation metadata set BEFORE open/rename
for deterministic distinction; no live shortcut/permissions/system settings. Original Menu32 and ALL
old assertions/scenarios/timeouts byte-identical. New harness hook before32; no gates bypassed.
Local review/static/signatures/parity/diff/secrets/public380/0/JSON PASS. No local compiler/pwsh/
Windows: new C#/PowerShell/native Publication6/Menu32/exact-source suites NOT_RUN; no old PASS inherited.

One expected-head guarded PR88 source publication and ONE automatic CI; no parallel rerun/full wait.
Public uploads OFF/release/prune ineligible, workflows unchanged. First next SAME new exact final logs/
merge/Publication6/Menu32/prior reached regressions. Only after PASS consider owned Menu rollback in a
separate stage. Durable Menu admission/result/error integration, Legacy/full auxiliary/partial receipt/
retention/runtime recovery remain mandatory separate residuals. UNKNOWN unrelated incidents and
BLOCKED_TRANSFER/no retry retained, weights/score delta0. No live install/services/network/security/
ACL/credentials/VPS/release/uploads. Earlier current headings below superseded by this record.

## CURRENT published test-only CLR probe / CI697 queued

Published source `8caf03239ce6d762aa49a044dad1a3270fd979de`, tree
`aadc0b7c1842a26e2aa03a12666674bd0b1e3de2`, parent4f75a48f356b2b49e823c5b38c1072f8b0453aeb.
Local reviewed code5e9f6a37f603474ebd63b0cec43dd9bd462ea1f1, freezea1f9b7488fbd07ff3b639d1c70bbaab37e15beb8,
canonical tree fetched/verified identical. Expected-HEAD non-force PR88 guarded publication.
ONE automatic pull_request [CI697](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40192886465),
exact source8caf032, queued/null observed. No parallel rerun/third attempt696/dispatch/duplicate/full wait.
Uploads OFF/release/prune ineligible; workflows unchanged.

Diff7files548add46del: test bridge/PS replacement/evidence doc3files172add22del; synchronized state
and prior local Python incident4docs376add24del. STRICTLY test-only+docs; no production or native
API change. All32/old asserts/expected/ordering/deadlines/cleanup exactly retained. Local review/
static/signatures/parity/diff/secrets/public377/0/JSON PASS. New C# compilation/PowerShell/native
bridge32/prior exact-source suites NOT_RUN. Independent CI696 attempt2 full10481/578 baseline:
originSame=False/durableMenu=False, bytes and all other proof gates True; native component mechanism
UNKNOWN. Old probe outer MethodInvocationException lacks inner boundary/root, NOT proven fixed yet.
SSH passed attempt2: non-reproduction, NOT fix of pre-body startup incidents. No blind receipt refresh.

Next SAME697 final full logs/exact tested merge, typed CLR before/after equality or exact phase/
inner exception types. Only then minimal proven correction; no Menu mutation/next feature now.
UNKNOWN incident ledger/BLOCKED_TRANSFER/no delivery retry preserved, scores unchanged delta0.
No full auxiliary rollback/success/Launch authority. No live shortcut/install/service/network/
security/ACL/credentials/VPS/release/uploads. This post-publication checkpoint local only, no push.

## CURRENT CI696 attempt2 origin mismatch; ONE test-only CLR probe replacement

Containing source is THIS git tree (git rev-parse HEAD); reviewed local code
5e9f6a37f603474ebd63b0cec43dd9bd462ea1f1. Baseline source4f75a48f356b2b49e823c5b38c1072f8b0453aeb,
checkpoint2d25eeabf40153232c9211cb2a3319058ae19a1f clean, PR88 exact HEAD/open draft,
sole root writer/no active Git/build/PowerShell checked, old worktrees unchanged.
Full CI696 attempt2 Windows10481/Ubuntu578 logs independently read; Windows121156450377 FAILURE,
Ubuntu121156452022 SUCCESS (reused original checkout/log). Tested merge8237cca8fc21678791d7517e8a96bf7374d849ef.
SSH exit0 passed: non-reproduction, NOT a fix of attempt1/CI662/670. Earlier installer/StartupFailure15/
Mutation14/Result13/Owned14/Admission25/Desktop6394/Instance30/Maintenance126/Shutdown47/Updater5 PASS.
Menu32:2PASS/legacy-removedFAIL/29NOT_RUN; later HomeVPN/Portable/relay/aggregate/pack NOT_RUN.

Proven: snapshot/runtime/pending/auxiliary/context/typed Menu+Startup+Legacy True, context not-applied;
legacyRemoved/replacementSame/bytesSame/originPresent True; durableMenu/originSame False. A recorded
native origin mismatch causes correct fail-closed refusal despite exact bytes. Volume/index/creation
mechanism and capture/rename causality NOT proven. Never refresh receipt or replace identity by hash.
Old rename probe MethodInvocationException has only broad phase/outer PowerShell stack; its inner
cause/boundary is unknown. See docs/CI696_MENU_PROBE_BRIDGE.md; no claimed production correction.

Minimal test-only replacement: separate C# probe uses already-loaded type, exact ctor/Install/
EnsureLink/native-info signatures and typed CLR witness. Original native operations/sequence remain;
no PowerShell delegate/scope or adapted object-array inside reflected call. Fixed subphase per
boundary, callback count, native before/after origin/volume/index/creation and actual byte equality.
Only booleans/count/elapsed/type/max4 inner exceptions x3 method+IL frames; no raw identities/hashes/
paths/timestamps/args/receipt/secret values. New own GUID root guard, read handles using/finally and
original own cleanup. Bridge load separate phase. All32/original expectations/assertions/order/
three reads/deadlines retained. Runtime/native APIs/receipt/failure/Launch/Stop/cleanup unchanged.
Local review/static/signatures/parity/diff/secrets/public377/0/JSON PASS. No local compiler/pwsh/
Windows: C# compilation/PowerShell/bridge/native32/previous exact-source suites NOT_RUN.

One expected-HEAD guarded publication/ONE automatic new CI; NO parallel rerun/third attempt of696.
Next SAME new CI full logs/exact tested merge/read precise probe before/after or phase/inner types.
Then only minimal PROVEN correction; no Menu mutation or Legacy/runtime/retention extension before
PASS. All ownership/fail-closed/private data/original install error/no false success/Launch guarantees
preserved. Full auxiliary outcome remains open. UNKNOWN old incidents and BLOCKED_TRANSFER/no
retry retained; score delta0. No live shortcut/install/service/network/security/ACL/credentials/
VPS/release/uploads. Older current headings superseded here; post-publication exact IDs local/PR.

## CURRENT CI696 attempt1 FAILURE; one bounded same-source Windows retry (attempt2)

Source remains `4f75a48f356b2b49e823c5b38c1072f8b0453aeb`, tested attempt1 merge
8237cca8fc21678791d7517e8a96bf7374d849ef. Baseline checkpointd7db10135667c2e9c9f73f5c3d4da7b17dec0aec
clean/sole root writer/PR88 exact HEAD/no active Git/build/PowerShell verified. Full attempt1 Windows
2803/Ubuntu578 logs independently read. Windows121153171905 FAILURE at exit0-regression5000ms,
Ubuntu121153171695 SUCCESS. Deadline5045ms/start28ms/CPU15.625ms; selected executable matched,
Python DLL/C runtime loaded, body/import/bootstrap/encodings/site markers absent, empty streams after
owned kill/drain and exited. Native validation/MenuAdmission32/rename-gate probe/Desktop/Instance/
installer/relay NOT_RUN. CI695 legacy-removed root remains UNKNOWN; no rename facts reached.

Exact Test-ProGo/SSH fixture/requirements/workflow diff from CI695 is empty. Gate passed in CI695;
CI670 shared empty pre-body markers, CI662 lacked that tracing, CI663 non-reproduced. These facts
do not prove a common cause, AV/runner load/flakiness/site or runtime defect. No guessed fix.
See docs/CI696_PYTHON_STARTUP_INCIDENT.md for evidence/limits/one-retry justification.

Owner expressly allowed ONE justified same-SHA repeat. After fresh run completed/attempt1/failure
and PR HEAD checks, requested ONLY failed Windows job121153171905 via single-job rerun endpoint.
No source/code/test/workflow changes, no push/new run/dispatch/parallel retry. SAME
[CI696](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40188130796) now attempt2,
Windows121156450377 in_progress/null observed. Latest Ubuntu121156452022 SUCCESS metadata only;
no new full-log execution claim. Original attempt1 failure retained, no retry loop.

Next SAME696 attempt2 final full logs/exact checkout/startup then actual Menu diagnostic if reached.
If exit0 passes, non-reproduction NOT a fix; if it fails, preserve evidence and stop without another
same action. No Menu mutation/new phase.5000ms/argv/stdin/ownership/cleanup/assertions/coverage all
unchanged. No code change to test locally: runtime/SSH harness parity PASS; local docs/JSON/public
checks only, new final Windows/installer/Menu results NOT_RUN. Score change0; UNKNOWN incidents/
BLOCKED_TRANSFER unchanged. No live install/service/network/security/ACL/credentials/VPS/release/
uploads. State checkpoint LOCAL ONLY (no second CI). Older current headings superseded here.

## CURRENT CI696 diagnostic publication checkpoint

Published source `4f75a48f356b2b49e823c5b38c1072f8b0453aeb`, tree
`2b3c48a838ea2293cb891596ce787fcb28c3f1e9`, parentd57f23596f5a9920bfd899a3e2ba222d9a3c79df.
Local reviewed codecde99a8d469b10d6966278abb83bc67b72309041, freeze31096a4283cfd7b35019afc7f91f29c5f911eb81,
canonical full tree fetched/verified identical. Expected-HEAD non-force guarded PR88 source update.
Exactly one automatic pull_request [CI696](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40188130796),
exact source4f75a48, in_progress/null observed; no rerun/dispatch/duplicate/full wait.

Diff5files360add30del: one test fixture130add1del including new evidence document; three synchronized
state documents230add29del. STRICTLY test-only instrumentation+docs; no runtime/native/helper/API/
workflow change. Original32/expectations/three reads/assertions/cleanup/timeouts retained.
CI695 root cause UNKNOWN:2MenuPASS/legacy-removedFAIL/29NOT_RUN; reached earlier suites PASS,
HomeVPN/Portable/relay/aggregate/public-content/pack NOT_RUN. Diagnostic source is NOT a fix.
Local review/static/signatures/parity/diff/secrets/public374/0/JSON PASS; new Windows diagnostics/
compilation/native32 and prior exact-source suites NOT_RUN. Current hypothesis only capture-before-
rename origin difference on vacated name; require gate booleans/probe facts before changing code.
Next SAME696 full final logs/exact tested merge/MENU_ADMISSION_REFUSAL/MENU_RENAME_PROBE (or safe
phase/type/stack if diagnostic fails). Only then minimal PROVEN correction; Menu mutation and new
functionality blocked until PASS. No score increase, no changes to ownership/fail-closed contract.
UNKNOWN incidents/BLOCKED_TRANSFER remain. No live install/service/network/ACL/security/credentials/
VPS/release/uploads. Local state checkpoint only, not pushed, no additional CI. Older headings below
superseded here; immutable public tree identifies itself by checkout SHA, exact IDs now recorded here.

## CURRENT CI695 failure / ONE targeted test-only Menu admission diagnostic

Containing source is THIS git tree (git rev-parse HEAD); reviewed diagnostic code
cde99a8d469b10d6966278abb83bc67b72309041. Baseline d57f23596f5a9920bfd899a3e2ba222d9a3c79df,
checkpoint2bb6a518e1d9d36b8b152d469d70c43f40d66270 clean; PR88 exact baseline HEAD/open draft,
sole root writer/no active Git/build/PowerShell checked; all old worktrees retained.
Full CI695 Windows10473/Ubuntu578 logs independently checked: overall FAILURE, Ubuntu SUCCESS,
exact tested merge3261d9af777fcc9883000c8cf3555177274048d3. Windows121145460388 at11:04:31UTC:
InstallerMenuRollbackAdmissionTests Check-MenuAdmission line13, repeated fresh classification
remains fail-closed. created/custom-preserved PASS; legacy-removed FAIL by exact third source case;
remaining29 NOT_RUN. No individual failed proof value/native cause is exposed in existing log.
StartupFailure15/Mutation14/Result13/Owned14/Admission25/all earlier installer/Desktop6394/Instance30/
Maintenance126/Shutdown47 PASS. HomeVPN/Portable/relay/aggregate/public-content/pack NOT_RUN;
uploads/prune SKIPPED. Do not transfer their older PASS or call whole Menu32 PASS.

UNKNOWN root cause: classifier returns null on exception/refusal, individual gate unknown. Source
shows Menu origin captured before publication rename and third fixture reuses a vacated Menu name;
possible capture/rename identity change is a hypothesis, not a proven fix or test-contract error.
No guessed runtime change or changed expected result. Original created/preserved ownership,
identity/origin/bytes/snapshot/receipt/trusted-location fail-closed contract remains intact.
See docs/CI695_MENU_ADMISSION_DIAGNOSTIC.md for exact evidence/distinguishing experiment.

Strict test-only instrumentation +documents. Failure-only legacy-removed probe preserves original
condition evaluated once, same assert/expected/order/all32/three reads/cleanup/deadlines. Read-only
original fixture gate booleans and current-origin/byte equality; one separate own GUID reproduction
of actual existing EnsureLink 4arg witness/vacated-name sequence compares native identity before/
after rename. Emit only booleans/phase/elapsed/type/max5 method+IL frames, no paths/raw identity/
hash/bytes/credentials/receipt contents. All own handles close and guarded own-root cleanup remains.
Production/native APIs/installer handler/ownership checks/other suites/scripts/workflows unchanged.
Local review/static/signatures/parity/diff/secrets/public374source0release/JSON PASS. Windows
PowerShell/reflection/native diagnostics/exact suites NOT_RUN; this diagnostic is NOT a runtime fix.

Pending one guarded expected-HEAD PR88 publication and ONE automatic diagnostic CI on new SHA;
no blind rerun/dispatch/duplicate/timeout increase. Public uploads OFF/release/prune ineligible.
Next SAME new CI full final logs: failed gate, originSame and exact rename-probe identity/bytes
booleans, then minimal PROVEN correction only; no Menu mutation or next functionality before PASS.
Menu32 remains2PASS/1FAIL/29NOT_RUN at CI695; new source all32 NOT_RUN until actually reached.
All Menu/Legacy/durable outcome/retention/runtime recovery dependencies and original error/no Launch
remain. Old incidents UNKNOWN; add CI695 unresolved gate refusal, no fabricated fix. BLOCKED_TRANSFER/
no retry; weights/score unchanged delta0. No live shortcuts/install/services/network/ACL/security/
credentials/VPS/release/uploads. Historical current headings below superseded here.

## CURRENT published Menu admission / CI695 queued

Published source `d57f23596f5a9920bfd899a3e2ba222d9a3c79df`, tree
`79ba537644337e2bf4a3b798994b793f5a18ca4a`, parent1f6b0bb7a9f73b1326fcceece1af80657035f8b3.
Reviewed local code255cc4ba095ebcb41d1e42d587f5592092687cb0; final synchronized/anonymized
freeze b63121ebc187b440c457a50ff38a3e144a7cf00b, identical canonical tree fetched/verified.
Expected-HEAD guarded non-force PR88 update, one automatic pull_request
[CI695](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40185551629), exact sourced57f235,
queued/null observed, no dispatch/rerun/duplicate/full wait. Upload guards OFF/release/prune ineligible.
Published current state now includes CI668-694 local checkpoint history; stale CI667 active_work
is superseded explicitly, historical fields preserved. Immutable tree resolves containing source
with git rev-parse HEAD; this local checkpoint/PR metadata provide post-publication exact IDs.
A private-root pattern in two historical documents was detected before publication and anonymized.
Only final clean tree exported; intermediate local commits/ancestry NOT published. Public373/0 PASS.

Runtime/test/evidence5files256add4del plus3 state files; full published diff8files4073add220del.
New Menu origin capture/read-only classifier/32 cases; no Menu deletion, no ownership grants from
intent, no full auxiliary outcome/success/Launch. Existing tests/assertions/timeouts unchanged.
CI694 full15799/578 independently verified baseline SUCCESS; new source PowerShell/C# compilation/
MenuRollbackAdmission32/previous exact-source suites NOT_RUN. Local review/static/signatures/parity/
diff/secrets/public/JSON PASS; score delta0. Worktree clean after checkpoint commit.

Next SAME695 full final logs/exact tested merge/MenuRollbackAdmission32 and reached prior suites.
After PASS: ONE owned Menu rollback operation with fresh exact origin/byte/handle validation;
its durable admission/result/error integration remains separate, followed by Legacy/full auxiliary
transaction/partial-receipt evidence/retention/general runtime recovery. Do not repeat closed work.
UNKNOWN incidents and BLOCKED_TRANSFER/no delivery retry unchanged. No live shortcut/install/
service/network/ACL/security/credentials/VPS/release/uploads; stop without a second stage.

## CURRENT F23 read-only Menu rollback admission; publication candidate

Authoritative current slice is THIS git tree; resolve its containing source with `git rev-parse HEAD`.
Reviewed local code `255cc4ba095ebcb41d1e42d587f5592092687cb0` precedes this state-only commit.
Baseline published source `1f6b0bb7a9f73b1326fcceece1af80657035f8b3`; local recovery checkpoint
`b87e37a487d95bfa32075b1d7922517c19c3f0c8` verified clean. PR88 exact HEAD/open draft,
sole root writer/no active Git/build/PowerShell checked; all old copies retained.
CI694 full Windows15799/Ubuntu578 logs independently checked: SUCCESS tested merge
9e38b10d878348b2cfb2f495484e7ffa08b8193b. StartupFailure15/MutationAdmission14/Result13/
OwnedStartup14/Admission25/all prior installer/Desktop6394/Instance30/Maintenance126/Shutdown47/
HomeVPN4816/Portable91 PASS. Pack runner-only; uploads/prune SKIPPED, no owner acceptance.

State publication correction: source1f6b0bb still contained the old CI667 active_work because
subsequent state snapshots had been committed locally only. This source now includes the latest
local history and current authoritative fields. Old tables, candidate strings, pending-publication
records and current-labelled older headings are HISTORY, not current claims; requirements and
incident details preserved. Immutable source cannot contain its own SHA, so current-tree resolution
is explicit; exact published source/run/checkpoint follow in PR metadata and local checkpoint.
No score increase, no removal of approved requirements, no inherited CI success for the new code.

Minimal missing guarantee: Menu bytes/hash do not prove object origin after an identical replacement.
The existing capture-enabled EnsureLink now records native origin via its existing pinned witness.
No new creation/deletion operation; non-capture install/legacy/Startup/failure paths unchanged.
Read-only Menu verifier/classifier pins bounded receipt and rechecks snapshot/current runtime/full
admissions/identity/ownership/receipt/independent trusted locations plus actual native Menu origin/
bytes. Accept full auxiliary prestate OR proven Startup admission+durable observation; mere absence
is not proof. Result created-from-absence/preserve-only, MutationAllowed=False, no target path.
Incomplete/unknown/foreign/conflicting/changed state refuses without mutation or new journal.
Old receipts without Menu origin remain ineligible; historical terminal readers unchanged.
See docs/F23_MENU_ROLLBACK_ADMISSION.md for acceptance and exact scope.

Runtime/test/doc slice5files256add4delete plus3 synchronized state documents. MenuRollbackAdmission32:
three fresh reads per case, full inventory/every-byte preservation, exact ownership vs identical-byte
replacement, missing/null/wrong origin, post-Startup proof and unproved absence, receipt/snapshot/
runtime/conflict/trusted-location errors; readers exclusively reopen, guarded own cleanup. Existing
assertions/test bodies/deadlines/native mutation APIs/failure/error/no Launch/Stop/finally unchanged.
Local review/static/signatures/parity/diff/secrets/public373source0release/JSON PASS. No local
PowerShell/compiler/Windows: new parser/C# compilation/native32/exact-source regressions NOT_RUN.

Pending one expected-HEAD guarded PR88 source publication and ONE automatic CI, no rerun/dispatch.
Public uploads OFF/release/prune ineligible; no workflow edits or binary delivery. First next step:
read SAME new CI final logs/exact source/merge/MenuRollbackAdmission32 and actually reached suites.
After PASS, owned Menu rollback with fresh exact origin/byte/handle validation, then durable Menu
admission/result/error integration. Legacy/full auxiliary outcome, partial pre-receipt evidence,
retention/general runtime recovery remain separate mandatory dependencies. No full rollback,
installation success, automatic mutation/retry or Launch authority granted here.
Old setup-cancel/repair/sharing/Python/rebind incidents UNKNOWN; scoped PID/Marshal fixes preserved.
Private candidate BLOCKED_TRANSFER/no retry. No live shortcut/install/service/network/ACL/security/
credentials/VPS/release/uploads. Fixed weights/credited score unchanged, delta0.

## F23 Startup-only production failure integration; CI694 in progress

Published source `1f6b0bb7a9f73b1326fcceece1af80657035f8b3`, tree
`4d854c511760442882a6f52101fc3502817118b9`, parent ddefeef9531b06c646e1c1b01edd8400a7377cf8.
Reviewed local code88b76d55bdf2ccc6ff4fe0f2c40e312688dd93dd canonical tree identical after fetch.
Baseline checkpointec4cac4957b0f51e870bd20dc62fe27cadfa2774 clean; PR88 exact HEAD/open draft,
sole root writer/no Git/build/PowerShell process checked, all older worktrees retained.
Full CI693 Windows15758/Ubuntu578 logs independently checked: SUCCESS exact tested merge
7a3b592645d089c46d4f340cb98aeddbf98e55c1. MutationAdmission14/Result13/OwnedStartup14/Admission25,
previous installer/Desktop6394/Instance30/HomeVPN4816/Portable91 PASS. Pack runner-only;
uploads/prune SKIPPED. No whole F23/production/owner acceptance inferred.

Actual installer failure path now marks auxiliary entry before Initialize-ProGoShortcuts. The
maintenance-owned catch calls existing full context/admission, owned native rollback and durable
observation. Existing admission permits ONLY read-only settlement (including not-applied: no replay).
Missing/partial receipt, changed bytes/origin/snapshot/runtime/ownership/conflict/persistence error
remains blocked. startup-observed is only a verified partial result; no full auxiliary outcome.
After auxiliary entry retain current runtime/snapshot: Menu/Legacy rollback remains open and full
runtime terminal would be dishonest; before entry original runtime rollback stays unchanged.
Original exception rethrown, no Install OK/Launch, stage/maintenance finally unchanged. Status
reports auxiliaryRollbackVerified=False. See docs/F23_STARTUP_FAILURE_INTEGRATION.md.

Diff5files253add1delete: installer helper/entry/catch/status, one harness hook, new real-body
StartupFailure15, explicit auxiliary mock for historical Maintenance19 synthetic shortcut adapter,
evidence doc. ALL original Maintenance19 assertions/scenarios retained byte-for-byte. That mock
covers runtime-only branches, not the real auxiliary integration: the new suite uses actual body,
real runtime/journal/snapshot/native shortcut/receipt/rollback/settlement in own temp directories.
15 cases created/custom/absence, missing/partial receipt, changed/identical replacement/Menu/snapshot,
admission/result persistence, existing unapplied admission/no replay, existing observed result,
locked deletion, Stop refusal. Exact original error/private bytes/untouched Menu Legacy snapshot/
truthful state/current runtime/no full terminal/no Launch/finally/handle cleanup are asserted.
No assertions/deadlines weakened; native/helpers/workflows and all prior fixtures otherwise unchanged.
Local review/static/signatures/parity/diff/secrets/public371source0release/JSON PASS. No local
PowerShell/compiler/Windows: new parser/compilation/native15 and exact-source regressions NOT_RUN.

Expected-HEAD non-force guarded publication into existing PR88. Public upload guards OFF; release/
prune ineligible. ONE automatic [CI694](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40182233205),
exact source1f6b0bb7a9f73b1326fcceece1af80657035f8b3, pull_request, in_progress/null observed.
No dispatch/rerun/duplicate/full wait; local state checkpoint not pushed to avoid another CI.
Next SAME694 full final logs/exact tested merge/StartupFailure15 and actually reached prior suites.
After PASS, smallest Menu rollback admission dependency, before its owned operation/full auxiliary
outcome; partial pre-receipt failure evidence, Legacy rollback, retention and general runtime
recovery remain mandatory separate residuals. No full rollback/success/retry/Launch permission.
UNKNOWN setup cancel/repair/sharing/Python/rebind incidents retained, prior scoped fixture fixes
unchanged. Private candidate BLOCKED_TRANSFER/no retry. No live shortcut/install/service/network/
ACL/security/credentials/VPS/release/uploads. Fixed weights unchanged; score change0.
Historical current-labelled sections below are superseded by this checkpoint.

## F23 Startup durable pre-mutation admission/read-only crash settlement; CI693 queued

Published source `ddefeef9531b06c646e1c1b01edd8400a7377cf8`, tree `087119a9510c142ed93acf5e3676e927bf239657`, parent
cf11c72d2f3dabebf2dfcae1b506447a758b0680. Reviewed local code9057c9f1d6a2d620706931bb9569d4ee77247aae
identical canonical tree fetched/verified. Baseline checkpointe52e471884deefe0195b9ac955e0f8944842fedf
clean; PR88 HEAD/sole root writer/no Git/build operation checked; old worktrees retained.
Full CI692 Windows15743/Ubuntu578 lines independently read: both SUCCESS, tested merge
2efbc5a4dee89d1687e0caffebd1b8d462b085f8. StartupResult13/OwnedStartup14/Admission25 and
previous installer/Desktop6394/Instance30/HomeVPN4816/Portable91 PASS. Pack runner-only,
uploads/prune SKIPPED. No whole F23/production/owner-PC acceptance inferred.

One bounded prerequisite in existing owned Startup operation: before native mutation persist exact
identity/journal/receipt/location/origin/hash/original-presence admission using CreateNew/WriteThrough/
Flush(true)/bounded readback, revalidate snapshot/current runtime/admissions/auxiliary ownership and
actual prestate. Pin admission stream against changes through physical settlement, finally close.
Native operation unchanged. Valid exact not-applied record can be reused only on explicit invocation,
never overwritten. Cancellation/persist failure/short/tampered/foreign/conflict blocks mutation.
Read-only crash state recomputes physical proof twice and validates pinned exact durable record:
not-applied, original-observed or blocked. No automatic delete/restore/replay from intent. Settlement
only records/reuses existing verified Startup-only observation after original-observed; no full
rollback/success/Launch or new attempt permission. Automatic reconcilers/new-journal gate block
admission prefixes; read-only preflight inspection is not mutation authority. See
 docs/F23_STARTUP_MUTATION_ADMISSION.md for acceptance and limits.

Diff5files282add4delete: installer admission/context/state/settlement and explicit operation guard,
harness hook, new StartupMutationAdmission14, existing OwnedStartup14 adds2 asserts/exact new file
accounting, evidence doc. Existing original assertions all retained; expected inventory includes only
one known independently validated admission, explicit retry same hash. New tests before/after crash,
preserved, persistence/short/tamper/replacement/runtime/snapshot/missing/conflict/cancellation/lock;
3 read-only inspections byte-identical, no replay and only actual result settles, own resources closed.
No timeout/coverage weakening. Native/error/Stop/finally code parity retained except pinned admission
lifecycle. Local static/review/signatures/parity/diff/secrets/public369/0 PASS. No local PowerShell/
compiler/Windows: new parser/compilation/native14/regressions NOT_RUN. No old PASS copied to new code.

Expected-HEAD guarded non-force PR88 source publication; upload guards OFF/release/prune ineligible.
ONE automatic [CI693](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40178557146), exact sourceddefeef9531b06c646e1c1b01edd8400a7377cf8, count1,
queued/null observed. No manual dispatch/rerun/duplicate/full wait; local docs checkpoint not pushed.
Next SAME693 final full logs/exact merge/StartupMutationAdmission14 and previous suites if reached.
After PASS, smallest complete Startup auxiliary outcome/error-handler integration dependency,
without merging Menu/Legacy rollback, retention or general runtime recovery. Partial pre-receipt
failure/crash evidence remains separate mandatory work. Original install error/no Launch and
settings/vault/keys/foreign resources unchanged. CI686/repair20s/sharing/Python/rebind UNKNOWN with
diagnostics; previous PID/Marshal/restart fixture fixes scoped PASS. BLOCKED_TRANSFER/no retry.
No live shortcuts/install/services/network/ACL/security/credentials/VPS/release/uploads. Fixed
weights unchanged/score change0. Historical current-labelled sections below superseded here.

## F23 durable Startup-only observed result prerequisite; CI692 in progress

Published source `cf11c72d2f3dabebf2dfcae1b506447a758b0680`, tree `fd7ba080f649b27384fc5cc53683462671c416eb`, parent
330bd6dd31a35f01690a52dae4371a2e1538ac17. Reviewed local codef59c88b90caa41877250030387e26d88a85c9716
identical canonical tree fetched/verified. Baseline checkpoint6ea3a0477b146d2408c3c6cab1f98a4a36a5fb3e
clean; PR88 exact HEAD/sole root writer/no Git/build operations verified; old worktrees retained.
Full CI691 Windows15729/Ubuntu578 logs independently read: both SUCCESS, tested merge
7c0cce30dc5711e51dd674a17c36875c8aa2fef6. OwnedStartup14/Admission25/Publication25/Auxiliary24
and prior installer/Desktop6394/Instance30/HomeVPN4816/Portable91 PASS. Pack runner-only,
uploads/prune SKIPPED. No full F23/production/owner-PC acceptance inferred.

One bounded durable outcome prerequisite: persist startup-original-observed only after real original
Startup state verification and unchanged Menu/Legacy at independent trusted locations plus current
new runtime/snapshot/identity/ownership/admissions proof. Exact receipt pinned/bounded and result
bound by receipt hash/install/location/origin. This observation does not claim who removed the link.
CreateNew own deterministic sidecar under verified backups, WriteThrough/Flush(true)/exact readback/
repeat physical proof; independent bounded reader recomputes actual proof. FullRollback=false,
InstallSucceeded=false, MutationAllowed=false, RuntimeState=new-verified. No flags/file existence
alone can satisfy reader. Incomplete/conflict/tampered/external state fail closed, no overwrite.
Any auxiliary partial artifact conservatively blocks installed/rollback automatic reconciliation,
auxiliary preflight and new-journal/no-pending gate. No intent authorizes automatic mutation.
See docs/F23_DURABLE_STARTUP_RESULT.md for acceptance/exact boundary.

Diff5files277add0delete: installer proof/writer/reader/4 guards, native readonly observer, harness,
StartupResult13, evidence doc. Old owned Startup implementation and existing helpers/asserts/timeouts/
error/Launch/Stop/finally unchanged except conservative guards. Tests actual existing rollback only
as setup in own temporary roots, result successes/refusals/persist/short/conflict/tamper/external
changes; exact receipt/identity/false full flags/all old bytes preserved/no implicit publication/
stream cleanup. Local review/static/signatures/parity/diff/secrets/public367/0 PASS. No local
PowerShell/compiler/Windows: parser/compilation/native13/exact regressions NOT_RUN. No old PASS
transferred to new code. Before any artifact exists, failed observational preflight does not erase
a legitimate prior installed proof; new writer never publishes it.

Expected-HEAD guarded non-force PR88 source update; public uploads OFF/release/prune ineligible.
ONE automatic [CI692](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40175778792), exact sourcecf11c72d2f3dabebf2dfcae1b506447a758b0680, count1,
in_progress/null observed. No dispatch/rerun/duplicate/full wait. Local docs checkpoint not pushed.
Next SAME692 final full logs/exact merge/StartupResult13/prior suites only if actually reached.
After PASS, smallest durable auxiliary pre-mutation admission/crash settlement prerequisite before
complete auxiliary outcome/error-handler integration. Partial pre-receipt failure remains blocked;
Menu/Legacy rollback, retention/general runtime recovery remain mandatory separate work.
Original error/no Launch and settings/vault/keys/foreign resources untouched. CI686/repair20s/sharing/
Python/rebind UNKNOWN with diagnostics; earlier PID/Marshal/restart fixture fixes scoped PASS.
BLOCKED_TRANSFER/no delivery retry. No live shortcuts/install/services/network/ACL/security/credentials/
VPS/release/uploads. Fixed weights unchanged/score change0. Older current-labelled sections superseded.

## F23 owned Startup rollback operation; CI691 in progress

Published source `330bd6dd31a35f01690a52dae4371a2e1538ac17`, tree `38faea0d0bced96965d61c825e151b291709dac5`, parent
dfff6e8cf3ce18739c3335ad5d8c8fd2814c8d9a. Local reviewed code8e848eda4be253d5ce566c05351f41f524b9ba76
identical canonical tree fetched/verified. Baseline checkpoint19f4d856495cd0e9bd6ff1f1ed7d796339746acb
clean, PR88 head/sole root writer/no Git/build operation verified; old worktrees retained.
Full CI690 Windows15714/Ubuntu578 logs independently read: both SUCCESS, tested merge
0c9d485bfe6836489f8aaff3c6f22f2441d4ff3f. StartupAdmission25/Publication25/Auxiliary24 and
all prior installer/Desktop6394/Instance30/HomeVPN4816/Portable91 PASS. Pack runner-only,
uploads/prune SKIPPED. No full F23/production/owner-PC acceptance inferred.

One narrow owned operation Invoke-ProGoInstallerStartupRollback: same bounded pinned receipt,
strict snapshot/identity/ownership/runtime/admissions and independent trusted auxiliary locations.
Startup receipt now includes native OriginIdentity captured from created temporary object's handle
before same object's no-overwrite move; destination verification checks actual origin/hash.
Old incomplete receipts cannot authorize deletion. Existing non-capture shortcut paths retain
original behavior; settings StartupChange and Legacy native deletion unchanged.
Created-from-absence deletion uses one exact READ+DELETE handle with OPEN_REPARSE_POINT/share READ,
verifies actual native origin/hash on that handle before FileDispositionInfo, then verifies real
absence after all handles close. No path-based File.Delete. Preserved/custom verified/no-op.
Changed/replaced/linked/denied evidence refuses; cancellation before mutation propagates, result
settlement after mutation not abandoned. Caller owns maintenance; current general error handler
not wired until durable auxiliary outcome dependencies complete. No complete rollback published.
See docs/F23_OWNED_STARTUP_ROLLBACK.md for exact acceptance and limits.

Diff5files260add6delete: ApplicationShortcuts origin/native operation, installer wrapper, harness,
OwnedStartupRollback14, evidence doc. Tests actual receipts/owned runtime/native only new temp
fixtures: created, preserved custom/absence, changed/identical replacement, missing origin, locked/
explicit retry, cancelled, native changed/replaced/cancelled after full admission, foreign runtime,
wrong trusted location. Exact bytes/absence, non-Startup full inventory, unresolved gate, exclusive
reopen proves cleanup; finally closes own lock/token/stage and guards reparse cleanup.
No assertions/timeouts weakened. Installer error/Stop/finally/Launch and previous tests unchanged;
Legacy/settings path parity and signatures/native handle order reviewed. Local static/review/
signatures/parity/diff/secrets/public365/0 PASS. No local PowerShell/compiler/Windows: new compilation/
parser/native14/exact regression NOT_RUN. No old PASS transferred onto new operation.

Expected-HEAD guarded non-force PR88 publication; upload guards OFF/release/prune ineligible.
ONE automatic [CI691](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40172465991), exact source330bd6dd31a35f01690a52dae4371a2e1538ac17, count1,
in_progress/null observed. No manual dispatch/rerun/duplicate/full wait; local docs checkpoint
not pushed. Next SAME691 final full logs/exact merge/OwnedStartupRollback14 and previous suites.
After PASS, minimal durable auxiliary mutation/outcome evidence before full error-handler integration;
partial pre-receipt failure/crash settlement, Menu/Legacy rollback, retention/general runtime recovery
remain mandatory separate work. Settings/vault/keys/foreign resources preserved.
CI686/repair20s/sharing/Python/rebind UNKNOWN with diagnostics; PID/Marshal/restart fixtures scoped PASS.
BLOCKED_TRANSFER/no delivery retry. No live shortcut/install/services/network/ACL/security/credentials/
VPS/release/uploads. Fixed weights unchanged/score change0. Old entries below historical superseded.

## F23-R02 Startup rollback admission prerequisite; CI690 queued

Published source `dfff6e8cf3ce18739c3335ad5d8c8fd2814c8d9a`, tree `e8ff7333d6a23e1ed0b6878d04b5a597265e2583`, parent
94bda3a9f25d3b41dc2e308381324c8e7b85f20f. Local reviewed code11b0f68091dd8f2b23161904ac7ff55c2fca5891
identical canonical tree fetched/verified. Baseline checkpoint872396ae1672dad542db33dc7f09d96bd11b08d4
clean, PR88 exact head/sole root writer/no Git/build operation verified; old worktrees retained.
Full CI689 Windows15689/Ubuntu578 lines independently read: both SUCCESS, tested merge
e265a3914a8a3207fc822e64e5553ba6680b967e. RestartGate8/PendingPublication25/Auxiliary24,
all prior installer suites/Desktop6394/Instance30/HomeVPN4817/Portable91 PASS. Pack runner-only,
uploads/prune SKIPPED. No whole F23, production or owner-PC acceptance inferred.

One bounded existing F23-R02 auxiliary rollback prerequisite: read-only Startup admission classifies
created-from-absence vs preserve-only after two strict auxiliary/runtime/snapshot/identity/ownership/
admission verifications. Trusted expected locations supplied independently, never by journal fields.
Exact pending receipt pinned read-only1..1048576 bytes through classification; finally closes both
reader/stream. Result contains schema/kind[private-root]/location identity/receipt hash/before-current presence/
hashes only. Missing/unknown/conflicting/changed evidence returns null without mutation.
Existing/custom/preserved Startup is never claimed owned. No deletion/restoration/rollback publication
or wiring to current error handler yet; it is a prerequisite, not an implemented auxiliary rollback.
See docs/F23_STARTUP_ROLLBACK_ADMISSION.md for acceptance and observational limits.

Diff4files212add0delete: installer admission helper, harness hook, StartupRollbackAdmission25, evidence.
Four accepted fixture states include actual absence and custom preservation;21 refusals retain all
existing proof guards; exact bindings/old-current bytes/receipt digest/no path or private bytes in
result, no gate bypass/full file inventory/private and foreign bytes unchanged. All old code/tests/
assertions/timeouts and Stop/finally/cleanup/original error/no Launch unchanged by static parity.
Local review/static/signatures/parity/secrets/public363/0/diff PASS; no local PowerShell/compiler/
Windows: new parser/compilation/admission25/native/regressions NOT_RUN. No old PASS applied to new code.

Expected-HEAD guarded non-force PR88 source update; upload guards OFF, release/prune ineligible.
ONE automatic [CI690](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40169562897), exact sourcedfff6e8cf3ce18739c3335ad5d8c8fd2814c8d9a, pull_request count1,
queued/null last observed. No dispatch/rerun/duplicate/full wait. Local docs checkpoint not pushed.
Next SAME690 final full logs/exact merge/StartupRollbackAdmission25 and actual prior suites.
After PASS, separate smallest actual Startup owned rollback slice: revalidate at mutation boundary,
only created-from-absence may be removed, preserve-only no-op, verify real absence and durable outcome.
Partial auxiliary failure before complete receipt still needs durable pre-change evidence; Menu/
Legacy rollback, retention/general runtime crash recovery remain mandatory separate work.
CI686 recovery/repair20s/sharing/Python/rebind causes UNKNOWN, diagnostic sources not runtime fixes.
PID/Marshal/restart fixture fixes scoped verified PASS. BLOCKED_TRANSFER/no delivery retry.
No live shortcuts/install/services/network/ACL/security/credentials/VPS/release/uploads.
Fixed weights unchanged/score change0. Historical current-labelled sections below superseded here.

## CI688 restart fixture dependency/refusal compatibility fix; CI689 in progress

Published source `94bda3a9f25d3b41dc2e308381324c8e7b85f20f`, tree `c5c4b8032a8e3bd4e432d40de97c32932f03639e`, parent
115748057bcafcae22ead2dcdfa628c257450e39. Local reviewed code5e35bf6f108927f9ff2ef26af09dc60303f7b002
identical canonical tree fetched/verified. Baseline checkpoint784c836be2eccc51bec39779629575e100d82900
clean; PR88 exact source/sole root writer/no Git/build operation checked; older worktrees retained.

Full CI688 Windows10156/Ubuntu578 lines independently read: Windows121045939351 FAILURE
Repository hygiene/build, Ubuntu121045939176 SUCCESS. Tested merge
064e39188b4744a85cb93620db6efc67a8c9e1fd. Desktop6394/Instance30/Maintenance126/Shutdown47,
Payload9/Maintenance19/Stage8/Backup8/SelfCheck9/Rollback8/Journal8 and earlier logged suites PASS.
RestartGate8 first no-parent fails composite reason/Stops/Disposes/Launches/Success, individual
values NOT logged. All later outcome/auxiliary/PendingPublication25/relay NOT_RUN; pack/uploads SKIPPED.

Code-derived cause: isolated fixture invokes body starting InstallDir, omitting preflight; neither
fixture nor caller initializes Payload. New call evaluates Payload.ScriptsDirectory before resolver,
StrictMode throws before Stop. Actual production preflight defines Payload; existing finally still
releases assigned maintenance. Do not mislabel inferred counters as measured CI values.
Minimal fixture sets only trusted ScriptsDirectory; no live install. Subsequent pending cases have a
second deterministic compatibility mismatch: new resolver generic refusal text differed from old
restart gate contract. Restore original refusal wording only; throw conditions/proof/gate unchanged.
See docs/CI688_RESTART_GATE_FIXTURE.md. Other body fixture preflight dependencies inspected.

Diff3files47add1delete: fixture3lines, installer one refusal string, evidence doc43lines.
All8 original assertions/messages/scenarios/private-byte/no journal bypass/Stop/Dispose/Launch
checks and cleanup preserved. All production logic except that one refusal text byte-parity;
no timeout/coverage weakening. Local review/static/signatures/parity/secrets/public361/0/diff PASS.
No local Windows/PowerShell/compiler: new compilation/RestartGate8/PendingPublication25/regressions NOT_RUN.
Expected-HEAD guarded non-force PR88 source publication; upload guards OFF, release/prune ineligible.
ONE automatic [CI689](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40153849502), exact source94bda3a9f25d3b41dc2e308381324c8e7b85f20f, count1,
in_progress/null last observed. No dispatch/rerun/duplicate/full wait; local docs checkpoint not pushed.

Next SAME689 full final logs/exact tested merge/RestartGate8 then PendingPublication25 and prior suites
only if reached. No next functionality before PASS. F23 publication remains pending exact Windows proof.
Old repair20s/sharing/Python/rebind/CI686 causes UNKNOWN and diagnostics retained; PID/Marshal scoped
verified PASS. BLOCKED_TRANSFER/no delivery retry. No live install/services/network/ACL/security/
credentials/release/uploads. Fixed weights unchanged/score change0. Historical entries below superseded.

## F23 pending-installed publication; exact new Windows proof pending

Published source `115748057bcafcae22ead2dcdfa628c257450e39`, tree `12a122a9c8787ad890a440dd558e640aed681114`, parent
a9f8b8feb2cd74749a1503b71a71974441f96b86. Reviewed local code55100972bd24051f8e1967942c5e23c81aff1c07
identical tree fetched/verified. Baseline checkpoint5636103cdfe8dbb7b610b43e4c4fafffcdf1b773
clean; PR88 expected head/sole root writer/no Git/build operation verified, old worktrees retained.
Full CI687 Windows15612/Ubuntu578 logs independently read, both SUCCESS; tested merge
774c0c6b5ee6d5d1f04fae6e08191d022219f346. Auxiliary24, setup recovery cancel-retry,
all previous installer suites, Desktop6394/Instance30/HomeVPN4816/Portable91 PASS.
Pack runner-only/uploads/prune SKIPPED. CI686 not reproduced, causal root UNKNOWN, diagnostic
source not runtime fix. No whole F23/production/owner-PC acceptance inferred.

One bounded production path: Resolve-ProGoInstallerPendingInstalled under existing maintenance
ownership before Stop/new writes. Scan all journal candidates, fail closed unknown/multiple/
conflicting; verified rollback-pending keeps existing separate resolver. Strict auxiliary reader
twice verifies snapshot/current runtime/admissions/identity/ownership and actual Startup/Menu/
Legacy against independent platform locations; pending hash unchanged required. Only atomic
same-directory File.Move metadata rename, no overwrite of installed.json. Rename denial propagates,
no Launch. No intent-driven runtime recovery or auxiliary mutation. See
 docs/F23_PENDING_INSTALLED_PUBLICATION.md for acceptance and concurrency observation limits.

Diff4files228add0delete: installer resolver/hook, harness hook, PendingPublication25, evidence doc.
Tests run actual installer body and resolver with own fixture locations and maintenance/Stop mocks;
positive exact rename/terminal/gate/idempotence, all refusal/private/external bytes, Dispose once/no
Launch; native publication-locked case first proves valid auxiliary receipt then requires IOException.
Old production/tests/guards/timeouts preserved by parity. Local review/static/signatures/parity/
secrets/public360/0/diff PASS. No local PowerShell/compiler/Windows: parser/compilation/new25/native
and exact new regressions NOT_RUN. Source push guarded expected-head/non-force; upload guards OFF,
release/prune ineligible. No manual CI/rerun/duplicate/full wait. ONE automatic [CI688](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40152222091), exact source115748057bcafcae22ead2dcdfa628c257450e39,
pull_request count1, in_progress/null observed. Local docs checkpoint not pushed, avoiding second CI.

Next SAME automatic run exact final logs/PendingPublication25 and prior suites if actually reached.
After PASS, choose smallest existing F23 auxiliary rollback prerequisite (failure/ownership proof)
from registry; retention/runtime crash recovery remain separate mandatory slices, not in this pass.
Stop/finally/settings/vault/keys/external changes unchanged. BLOCKED_TRANSFER/no delivery retry.
Repair20s/sharing/Python/rebind and CI686 incidents UNKNOWN; PID/Marshal fixes scoped verified PASS.
Fixed weights unchanged/score change0. Historical current-labelled entries below superseded here.

## CI686 owned recovery Cancel/Retry UNKNOWN; one test-only diagnostic CI687

Published source `a9f8b8feb2cd74749a1503b71a71974441f96b86`, tree `cafcbd8f58356dc18a3b7543aaa22f4d34d90d6c`, parent
5a6a23d33e784d929a370dd4a0daded4dc64cd6b. Local reviewed code1e0283c3004ab5a96d473b8f3995cf8c1e9313db
identical canonical tree fetched/verified. Baseline checkpoint65b596d01ed4f05018fd768ceb0a5538eacd633f
clean, PR88 exact HEAD/sole root writer/no Git/build process verified; old worktrees preserved.

Full CI686 Windows15227/Ubuntu578 lines independently read. Windows121030565409 FAILURE relay,
Repository hygiene/build SUCCESS; Ubuntu121030565619 SUCCESS. Tested merge
54326758a24ca31f53d1898f9d3f07826213a7a8. Actual AuxiliaryRevalidation24 and all earlier installer
suites PASS, Desktop6394/Instance30 and earlier logged suites PASS. Relay OwnedSetupRecovery
only default-refusal/status-pending complete; cancel-retry fails after held-body check. Overall
HomeVPN/remaining recovery modes/later suites NOT_RUN to completion; pack/public-content/uploads
SKIPPED. No overall green CI or production/owner acceptance claim.

Safe original stack ModalWizardCase IL155 -> OwnedSetupRecovery IL1624 -> Run IL471 -> Main IL4958.
Next composite assertion has six conditions; individual values absent. Exact violated invariant/
causal root UNKNOWN, not linked to independent installer-only diff. No runtime fix justified.

Diff2files93add2delete, strictly test instrumentation + evidence document:
HomeVpnSetupRecoveryTests retains exact original short-circuit expression/order/message/assert.
Only failure collects six named boolean conditions, violated list, bounded safe stack, task enums,
UI owner/modal/handle/disposal and counts, admin/cleanup/resources/release, accepted ID/result/journal/
private-byte equality booleans. One atomic observation before original cancellation throw records
actual copy token state (-1 not reached/0 false/1 true). No raw secret/ID/PID/path/argv/message/body.
ShowDialog, all waits/assertions/no replay/production and finally cleanup unchanged. Diagnostics
are observations after original sample and not a fix; incomplete collection still fails original.
See docs/CI686_SETUP_RECOVERY_CANCEL_DIAGNOSTIC.md.

Local review/static/signatures/full original fixture parity/secrets/public358/0/diff PASS.
No local compiler/PowerShell/Windows: exact diagnostic/native/regressions NOT_RUN.
Expected-HEAD guarded PR88 source update; public upload guards OFF/release/prune ineligible.
Canonical tree guard rejected an initially truncated tool read before any ref update; reread full
file/tree equality verified before sole publication. No wrong tree committed to PR/no duplicate CI.
ONE automatic [CI687](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40149453845) sourcea9f8b8feb2cd74749a1503b71a71974441f96b86, in_progress/null count1.
No full wait or second stage. Local docs checkpoint not pushed (avoids second CI).
Next SAME687 final full logs/exact tested merge/named conditions and actual lifecycle facts.
Non-reproduction alone leaves incident UNKNOWN. No pending-installed publication/new functions
before verified PASS. F23 auxiliary revalidation now scoped CI686 PASS, whole F23 still open.
Repair20s/sharing/Python/rebind UNKNOWN; PID/Marshal fixture fixes scoped PASS. BLOCKED_TRANSFER,
no delivery retry; no live install/services/network/security/credentials/release/uploads.
Weights unchanged/score change0. Older current-labelled sections below are historical snapshots.

## F23 auxiliary durable receipt revalidation prerequisite; CI686 in progress

Published source `5a6a23d33e784d929a370dd4a0daded4dc64cd6b`, tree `938ad1a83b6f63e8d2a9a6b7bde3abdfe893df83`, parent
0b662b38bba5e799755f599ad5ab344a0ab067aa. Reviewed local code67e88df63734025dfad4687a3dbdfda9c79e81df
has identical tree. Baseline checkpoint ca998a01a734f4771f0ffeb51b8f4b2fff9f3412 clean;
PR88 source/sole root writer/no Git/build operations checked; old worktrees preserved.
Full CI685 Windows15587/Ubuntu578 logs independently read, both SUCCESS; tested merge
69fc0c87dac0d43d71d1df0b698c990e50796ce8. Legacy14 actual PASS plus Menu14/Startup13/
PendingProof11/Reconcile13/RollbackOutcome12/Maintenance19 and all installer/SOCKS51/
Desktop6394/Instance30/HomeVPN4816/Portable91 suites. Pack runner-only; uploads/prune SKIPPED.
No owner-PC acceptance, whole F23 or production PASS inferred.

One bounded read-only prerequisite: pinned bounded durable receipt, strict fixed DTO field/type/
schema/hash/presence parsing; existing native Startup/Menu/Legacy verifiers check actual bytes/
absence/legacy origin and replacement against independently supplied expected locations/root.
No journal path is used as a target; runtime/admissions/snapshot/identity/owned inventory and
conflicts rechecked before/after via existing proof. Unknown sibling pending journals block.
New reader never writes/deletes/restores shortcuts/runtime or renames pending metadata.
See docs/F23_AUXILIARY_RECEIPT_REVALIDATION.md for acceptance/observational limits.
A positive observation is not atomic publication authority or hostile-journal authenticity.

Diff5files255add0delete: ApplicationShortcuts strict durable DTO reader, Install-ProGo readonly
auxiliary verifier/scope guard, harness hook, AuxiliaryRevalidation24 own-temp scenarios, evidence doc.
Three positive variants plus21 refusals, all fixture bytes/inventory/private/external preservation,
no automatic retry/publication; own locks/stage closed finally/reparse cleanup refused.
No existing installer/native/test body altered; existing assertions/guards/timeouts byte-parity
verified after removing additions. Static review/signatures/read-only guard/parity/secrets/public357/0 PASS.
No local PowerShell/compiler/Windows: new compilation/parser/auxiliary24/native regressions NOT_RUN.

Expected-HEAD guarded non-force source push PR88; public workflow upload guards remain OFF,
release/prune ineligible. ONE automatic [CI686](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40147067305), exact source5a6a23d33e784d929a370dd4a0daded4dc64cd6b,
pull_request count1, in_progress/null last observed. No manual rerun/duplicate/full-CI wait.
Local docs-only checkpoint not pushed, avoiding second CI. Next SAME686 final logs/exact tested merge/
AuxiliaryRevalidation24 and prior suites only if actually executed. If PASS, separate bounded
pending-installed publication gate with final auxiliary/runtime/ownership/concurrency proof;
no publication implemented in this stage. Runtime crash recovery, retention, auxiliary rollback,
PGA04 installer opt-in and remaining F23 acceptance gates remain mandatory separate work.
PID and SizeOf fixture fixes scoped Windows PASS; repair20s/sharing/Python/rebind incidents UNKNOWN,
diagnostics preserved. Private candidate BLOCKED_TRANSFER, no delivery retry. No live install/
services/security/network/credentials/release/uploads. Weights unchanged; score change0.
Historical entries below are evidence snapshots, superseded for current status by this section.

## CI684 verified; exact SizeOf(Type) fixture fix; CI685 in progress

Published source `0b662b38bba5e799755f599ad5ab344a0ab067aa`, tree `044823a1467026622c7b114872c01f398281d76a`, parent2357a85691f17024a8a20381faa6a5051479c715.
Baseline checkpoint c1d532878b1e30527c6cf46e42185384dc803e44 clean; PR88 HEAD/sole writer verified.
Minimal diff2files38add2delete: test exact MethodInfo SizeOf(System.Type) invocation and evidence doc.
Expected ABI sizes remain1/52; all scenario tail/assertions/guards/timeouts identical; runtime unchanged.
Full CI684 Windows log independently read: tested merge488061a811c1609f6bc8e4cc0a3eb6657e9837c8,
Windows121017174243 FAILURE, Ubuntu121017174418 SUCCESS. Failure test line19 marshals RuntimeType,
not represented structure. Legacy14 NOT_RUN; relay/pack/uploads SKIPPED. Repair suites, SOCKS51,
Desktop6394 including PID admission regression, Instance30/Maintenance126/Shutdown47 and all installer
Payload9/Maintenance19/Stage8/Backup8/SelfCheck9/Rollback8/Journal8/RestartGate8/Outcome10/
RollbackOutcome12/Reconcile13/PendingProof11/Startup13/Menu14 PASS on that merge only.
PID fixture corrected code now scoped Windows PASS. Repair20s/Python/rebind/receipt causes UNKNOWN;
non-reproduction is not a runtime fix. See docs/CI684_LEGACY_SIZEOF_FIXTURE.md.
Local normalized scenario parity/review/signatures/diff/secrets/public355/0 PASS; initial byte parity
check mismatched CRLF vs canonical Git LF, corrected normalized check PASS; no content changes needed.
No local PowerShell/Windows: new ABI/Legacy14/exact regressions NOT_RUN.
Expected-HEAD guarded non-force PR88 update; same public workflows four upload guards OFF,
release ineligible. ONE automatic [CI685](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40144623516) source0b662b38bba5e799755f599ad5ab344a0ab067aa, in_progress/null; no duplicate/rerun/wait.
Next SAME685 full final log: exact merge, ABI and actual Legacy14, downstream only if reached.
No new function/runtime recovery/pending publication before PASS. BLOCKED_TRANSFER, no retry.
Weights unchanged/no score increase. Old entries below are historical snapshots superseded here.

## CI683 complete repair20s UNKNOWN; test-only diagnostic CI684 in progress

Published diagnostic source `2357a85691f17024a8a20381faa6a5051479c715`, tree
`fe813461e9648b76ccfbe6bfa400a8515b7d95a2`, parent
`d4903e818c4c95eadf914a5e077737854071b223`; fetched exact tree/parent equals reviewed
local code `9764d31067c849a36717ff6a4d6e84ef8ea75554`. Baseline checkpoint
`24df24bb633458a041be612bec69d211d94113bf` clean/sole root writer confirmed. No competing
Git/build process; old worktrees preserved. PR88 expected-HEAD non-force source update verified.
Public repo/workflows unchanged/four artifact upload guards OFF; no release eligibility.

Full final CI683 Windows191/Ubuntu578-line logs read independently: Windows121012684350
FAILURE, Ubuntu121012684481 SUCCESS; tested merge
`477bddbfec21431bbd1049bbc7daf86473b1ead7`. EXE build PASS, repair native null interop1 and
four incomplete payload refusal PASS. Complete positive owned repair PowerShell root did not
exit within WaitForExit20000 at fixture line156. No inner phase/process/stream evidence then.
PID fixture regression/Desktop/SSH policy/Instance/all installer/LegacyEvidence14/relay NOT_RUN;
pack/uploads SKIPPED. Repair script/fixture/helper match CI681 source5f84ceb (past PASS), so
no link to test-only PID patch demonstrated. Actual new timeout cause UNKNOWN.

ONE test-only phase experiment, NOT repair fix: only first complete repair uses a wrapper
calling unchanged original script/arguments/environment with 17 exact unique source-line
breakpoints and automatic safe actions. Entry/helper/maintenance/snapshot/copy/verify/
selfcheck-call vs return/commit/transaction-cleanup/script-return/probe-cleanup checkpoints
contain only fixed whitelist names and ms in CreateNew own-GUID trace. No stepping/prompt or
source rewrite. Parent start-enter/start-return, deadline/normal exit/after original cleanup
facts: own root exited-known/value/CPU/RAM, stdout/stderr task state, bounded trace, own journal
whitelist phase/counts and self-check-data existence under guarded own stage folders. No raw
journal/output/path/argv/PID/credential/exception message. Max6 sanitized managed method names;
parent timeout stack is not claimed inner child stack. Native self-check handles not directly
observed. Markers BEFORE lines certify prior boundary only. Any remaining native-loader vs
image-init gap stays explicit. Diagnostic overhead is inside same20s, never timeout-as-PASS.

Original 20s repair, 8s self-check, 3s stream/cleanup waits, asserts, kill only own root, later
fixture scenarios/order preserved. Original arguments restored before later cases. Wrapper
finally removes only own breakpoints/closes trace. Runtime/MaintenanceOperation/PID fix/legacy
and other production code unchanged. Diff3files213add/2delete: RepairPayloadTests diagnostics,
new test-only RepairPhaseProbe, evidence/incident doc. Local review/static17 anchors/timeout/
assert/runtime parity/signatures/diff/secrets/public354/0 PASS. Local PowerShell/compiler/
Windows unavailable: wrapper/phase/native/PID regression/LegacyEvidence14/exact suites NOT_RUN.

ONE automatic [CI684/40142564635](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40142564635),
source2357a85/pull_request/count1/in_progress/conclusion null observed. No dispatch/rerun/duplicate/
full wait. Local docs-only checkpoint not pushed to avoid second CI.
Next SAME684 full final logs/exact merge/last completed repair phase, owned root/stream/cleanup
states and only downstream tests actually reached. No functional stage before exact PASS.

Incident ledger (also CI683_REPAIR_PHASE_DIAGNOSTIC.md): Python startup CI662/670 UNKNOWN;
listener release CI671 UNKNOWN; SOCKS pending receipt busy CI676 UNKNOWN, diagnostics retained.
PID double-read fixture corrected in code d4903e8, Windows still NOT_RUN due earlier CI683;
exact original null branch unmeasured, no runtime leak claim. Repair20s CI683 new UNKNOWN;
this source DIAGNOSTIC, not fix. Prior non-reproduction never closes these incidents.
Auxiliary durable revalidation/pending-installed, runtime recovery/retention/aux rollback and
existing seven F23 acceptance gates stay OPEN. BLOCKED_TRANSFER/no retry. No live install/
service/network/ACL/security/credentials/release/uploads. No score increase. STOP one stage.

## CI682 test-only command admission capture correction; CI683 in progress

Published source `d4903e818c4c95eadf914a5e077737854071b223`, tree
`dbee8576d00770bc64a91066884f3c7b5f4c31f2`, parent
`559a18adf35dfeeb23c16c7fc4880e2e0d41cd9a`; fetched exact parent/tree equals reviewed
local code `0f4424be9aa0fe1480a5fb0b7a353e5d7530268a`. Baseline checkpoint
`b8c3ce39f67529600213b1e5c74db3ba5331b3b4` clean/root-only writer verified. No competing
Git/build process; old worktrees preserved. PR88 expected-HEAD non-force source push verified.
Public repository/workflows unchanged/four upload guards OFF, no release eligibility.

Full final CI682 Windows4293/Ubuntu578-line logs independently read: Windows121009095063
FAILURE, Ubuntu121009094843 SUCCESS; tested merge
`ba5b0470add2c8008bc9251825dc8ac10fc7e251`. CommandAvailabilityWorkflow -> Main failed
original-fixture-pid-retained. capturedPidPresent=False/fixtureAliveKnown=False/fixtureAlive=False/
releaseExists=False; all before-connect/after-connect/after-alias count1/pidPresentTrue/
pidSameFalse/connectingTrue/stoppingFalse/requestedTrue/expectedWaitingLifecycleTrue.
Original pending-route-count-one=True. SOCKS51 and logged earlier repair/vault/log/backup/
updater suites PASS scoped. Desktop NOT complete; SSH policy214, Instance, ALL installer
including LegacyEvidence14, relay NOT_RUN. Pack/uploads SKIPPED; no old PASS inherited.

DesktopStartupTests blob equals preceding5f84ceb: no evidence tying early failure to new
legacy native path, which was not reached. CurrentPid is a nonblocking TryEnter observation;
PumpUntil has already seen a live owned process AFTER actual Process.Start/ownership admission,
but then final DoEvents and UI checks precede a SECOND independent read for expected PID.
Successful first read does not guarantee that second snapshot has a value. CI682 proves
second captured baseline null, not which exact getter branch/gate owner caused it. Contention
is a code-supported mechanism, not measured native cause. Not snapshot-before-admission;
no proven runtime leak/process replacement/infrastructure/legacy defect.

Minimal test-only fix captures PID from THE SAME successful predicate under unchanged 5000ms
PumpUntil. Frozen before UI checks/dispatcher actions; actual liveness asserted before actions.
No recapture/substitution from later observations. Original count and exact original-PID
assertions/order, all tested actions/diagnostics/modal/Stop/shutdown/finally unchanged.
Deterministic metadata regression absent -> admitted synthetic value -> later unavailable
snapshot before wait returns requires original value and no extra read; no-admission wait
must refuse. It supplements, not replaces, the actual owned-child integration scenario.
All runtime/installer/legacy/transport code and old PumpUntil/timeout unchanged.

Public diff2files98add/2delete: only DesktopStartupTests33add/2delete and evidence doc.
Local review/static signatures/capture order/original action/assert/diagnostic/cleanup parity/
diff/secretPII/public352/0 PASS. A review-script PID count initially included an unrelated later
method, corrected to selected workflow; no source/assert changed for it. No local compiler/
Windows: new capture regression/compilation/Desktop/LegacyEvidence14/all exact native suites
NOT_RUN. Specific CI642 original individual failure still not observed; historical cause not
retroactively claimed fixed. Sharing/Python/rebind incidents remain UNKNOWN with diagnostics.

ONE automatic [CI683/40141050466](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40141050466),
source d4903e8/pull_request/count1/in_progress/conclusion null observed. No dispatch/rerun/
duplicate/full wait. Local docs-only checkpoint not pushed to avoid second CI.
Next SAME683 full final logs/tested merge/frozen original admission plus count/PID/lifecycle,
full Desktop/Instance/all installer including LegacyEvidence14/relay. No functional stage
until exact PASS. Auxiliary durable revalidation/pending-installed remains OPEN; runtime
recovery/retention/aux rollback and original seven F23 acceptance gates unchanged.
BLOCKED_TRANSFER/no retry; no release/uploads/install/live services/network/ACL/security/
credentials changes. No score increase or readiness percentage recalculation. STOP one stage.

## F23 legacy migration completion evidence prerequisite; CI682 in progress

Published source `559a18adf35dfeeb23c16c7fc4880e2e0d41cd9a`, tree
`9599223a942085ca68bca3dda00916cd73e480f8`, parent
`5f84ceb5f55d76a1f393e665de87053260b24aeb`; fetched exact parent/tree equals reviewed
local code `e067e9ed96cac73d24817dc91ad5cf28803405ad`. Baseline checkpoint
`8b92a750fdd5c2251e868ce25778a44f73b749d3` clean/root-only writer verified. No competing
Git/build process. Old worktrees preserved; expected-HEAD non-force PR88 source push verified.
Public repo/workflows unchanged; upload guards OFF, no release or artifact uploads authorized.

Full final CI681 Windows15558/Ubuntu578-line logs independently read; Windows120998709418/
Ubuntu120998709215 both SUCCESS; tested merge
`01191ad24b207ce4f6191a65d34545ca71439128`. MenuEvidence14/StartupEvidence13/PendingProof11/
Reconcile13/RollbackOutcome12/Maintenance19/Outcome10/RestartGate8/Journal8/Rollback8/Payload9/
Stage8/Backup8/SelfCheck9/SOCKS51/Desktop6388/policy214/Instance30/Maintenance126/Shutdown47/
HomeVPN4816/Portable91/prior suites PASS. Pack runner-only/uploads-prune SKIPPED. Sharing/
Python/rebind incidents remain UNKNOWN with diagnostics, not claimed runtime fixes.

ONE installer legacy evidence slice: actual source bytes/absence and hashed file-object
origin, semantic match, deleted-owned hash, replacement hash and install/location identities.
Preserved absent/custom/foreign/skipped observations never claim owned deletion. Installer
witness denies write/delete during semantic read; before removal an existing READ+DELETE
handle must match exact origin AND bytes, rejecting changed/replaced identical-byte source.
Only this validated handle is marked for deletion, no path-based delete or DELETE_ON_CLOSE;
primary is pinned/verified until physical deletion settles. New foreign appearance is never
removed and prevents accepted completion. Handle attributes reject linked/directory objects.
Read-only current proof checks absence/preserved bytes/origin and actual replacement, never
repairs mismatch. No claim against all hostile ancestor swaps or future external changes.

Legacy receipt is nested with unchanged Startup/Menu proof semantics in actual durable runtime
outcome, matching identity/inventory/admissions/ownership. Production blocks missing legacy
before metadata creation; maintenance mock only gains synthetic result shape. Existing public
Install and Desktop/token MigrateMenu/COM/guards unchanged. Pending gate remains fail-closed;
original error/no Launch after rollback preserved. No automatic recovery/replay/aux rollback.

LegacyEvidence14 own-GUID/temp actual adapter/native type cases absent/removed/skipped-owned/
skipped-custom/foreign-target/malformed/locked-retry/changed-admission/replaced-identical/
appeared/replacement-changed/other-identity/other-location/durable. Reflection invokes exact
private admission for changed bytes or retained original plus identical-byte foreign object;
requires specific IOException, not arbitrary reflection failure. Lock case requires actual
DELETE-admission denial then explicit retry; durable writer round trip/missing proof refusal/
pending blocked/private bytes intact. Cleanup owns creation flag, held handle/stage/temp only.
No live known-folder shortcut/install/service/ACL/security/network/credential actions.

Diff6files375add/6delete: ApplicationShortcuts; Install-ProGo missing legacy guard; one harness
call; maintenance mock shape; new test and evidence doc. Existing tests/assertions/timeouts/
scenarios/Startup/Menu verifiers/pending/snapshot/runtime rollback helpers preserved. Local
review/static parity/API signatures/C#5/native ABI primary-doc review/diff/secretPII/public351/0
PASS. A documentation wording triggered personal-path guard; reworded generic prose, no secret
found and guard unchanged. No local parser/compiler/Windows: LegacyEvidence14/native/compilation/
exact regressions NOT_RUN. No old PASS inherited.

ONE automatic [CI682/40139850722](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40139850722),
source559a18a/pull_request/count1/in_progress/conclusion null observed. No dispatch/rerun/duplicate/
full wait. Local docs-only checkpoint not pushed to avoid another CI.
Next SAME682 final logs/exact merge/LegacyEvidence14/all prior suites. Then ONE bounded durable
auxiliary receipt parsing/revalidation against trusted expected Startup/Programs identities,
full current Startup/Menu/Legacy result and runtime phase/snapshot/admission/ownership/conflict
proof BEFORE any pending-installed metadata publication. Intent/receipt presence alone cannot
authorize it. Runtime crash/partial recovery, retention and auxiliary rollback remain separate
mandatory F23-R02 residuals. Existing seven whole F23 gates and engineering ZIP/standalone/
commercial dependency matrix remain in F23_INSTALLER_MENU_COMPLETION_EVIDENCE.md, none closed.
BLOCKED_TRANSFER/no retry. No release/uploads/install/live actions. No percentage recalculation
or score increase for unexecuted new code/tests/docs. STOP one stage, no second stage/run.

## F23 Start Menu primary completion evidence prerequisite; CI681 in progress

Published source `5f84ceb5f55d76a1f393e665de87053260b24aeb`, tree
`ccb60a608c581cb83fdd24d935cf64eea5736970`, parent
`c7eb7c307ab7ce616a6feaed21b7aec7a5658d9f`; fetched full tree equals reviewed local
code `b1a004089871518b7d4b793f5d1692b578598977`. Baseline checkpoint
`a143c20a908513f4a6ac00ceb081a385404b9cfb` clean/root-only active writer verified.
No parallel Git/build operations. Old worktrees preserved; expected-HEAD non-force PR88
source update verified. Public repo/workflows unchanged; four upload guards OFF, no release.

Full final CI680 Windows15544/Ubuntu578-line logs read independently; Windows120991364304/
Ubuntu120991364239 both SUCCESS, tested merge
`fd5cf0fcc063089ca302ca163aaa32cff4a09b5c`. StartupEvidence13/PendingProof11/Reconcile13/
RollbackOutcome12/Maintenance19/Outcome10/RestartGate8/Journal8/Rollback8/Payload9/Stage8/
Backup8/SelfCheck9/SOCKS51/Desktop6388/policy214/Instance30/Maintenance126/Shutdown47/
HomeVPN4816/Portable91/prior suites PASS. Pack runner-only, uploads/prune SKIPPED. Previous
sharing/Python/rebind incidents remain UNKNOWN with diagnostics, not claimed fixed.

Selected ONE primary Menu evidence slice; legacy migration proof remains separate. Existing
installer path captures actual bounded before/after bytes/absence of primary link, creation
only if this EnsureLink returned the exact actual bytes; otherwise preserved observation,
never a new ownership claim. Requested Menu must match executable/--show via unchanged native
oracle. Receipt binds install/expected Programs location identities, request/disposition/hashes;
read-only verification rejects foreign/changed/removed/appeared/other-identity/location state
without repair. No raw paths/shortcut bytes/arguments persisted. Startup receipt now nests
Menu proof; production requires matching Menu schema/root before durable outcome creation.
Existing COM writes/MigrateMenu and reparse guards unchanged. Pending gate not relaxed;
legacy/current trusted auxiliary revalidation still required before pending-installed promotion.

MenuEvidence14 own-temp actual adapter/native type cases created/existing-owned/skipped-
absent/skipped-custom/foreign/foreign-target/changed/appeared/removed/other-identity/other-
location/directory-conflict/oversized/durable. Durable case retains exact proof in actual
owned writer payload, ties runtime inventory/admissions/ownership, rejects missing Menu,
keeps all private/settings/vault/key-ref/foreign bytes and pending admission blocked.
Diff6files263add/6delete: ApplicationShortcuts, Install-ProGo guard, Test-ProGo one call,
Maintenance mock Menu shape, new test and evidence/gate document. All old assertions and
timeouts/scenarios preserved. Local review/static parity/signatures/C#5 review/diff/secretPII/
public-content349/0 PASS. Parser/C# compilation/Windows COM/MenuEvidence14/new regressions
NOT_RUN here, no local tools. Baseline PASS never inherited by new SHA.

ONE automatic [CI681/40136394776](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40136394776),
source5f84ceb/pull_request/count1/in_progress/conclusion null observed. No manual dispatch/rerun/
duplicate/full wait. Local docs-only checkpoint not pushed to avoid second CI.
Next SAME681 final logs/tested merge/MenuEvidence14/all prior suites; then ONE legacy-
migration evidence dependency. Pending-installed NOT complete; no runtime recovery, retention
or auxiliary rollback implemented in this stage. Original failure/no Launch after rollback
and private/external file guards unchanged. BLOCKED_TRANSFER, no retry or private candidate delivery/build.
No live shortcuts/install/PC/VPS/services/network/ACL/security/credentials/release/uploads.

Current finite gates and dependencies are in F23_INSTALLER_MENU_COMPLETION_EVIDENCE.md:
- Personal engineering ZIP: supported transfer BLOCKED -> exact Windows build identity,
  actual manifest/SHA256/package content/extracted smoke -> separate owner acceptance.
  Diagnostic ZIP is not whole F23 standalone completion.
- Standalone technical: R01 standalone prebuilt + PGA-04 OFF/default consent; R02 complete
  auxiliary proof/pending installed, crash recovery/retention/aux rollback and shared
  maintenance acceptance; R04 truthful final updater rollback and R05 immutable release
  identity; R01/R02/R04/R05 -> R03 fixed-path verified installer fallback; R01-R05 + F31-R11
  -> R06 unified package clean-install/both updates/cancel/offline/hash/self-check/handoff;
  R06 -> R07 owner installed preservation/no replay. All seven whole gates remain OPEN.
- Full commercial: same F23 gates plus original remaining registry incl F30 real mail/
  account/billing/provider/payment/refund/webhook/session acceptance and final release gates.
Historical old tables are retained as history; CI680 is current bounded dev proof, new SHA
NOT_RUN and package/owner layers NOT_RUN. No new requirements/reweighting/percentages.
STOP one stage, no second run. Score change not awarded for unexecuted code/tests/docs.

## F23 auxiliary Startup completion evidence prerequisite; CI680 in progress

Published source `c7eb7c307ab7ce616a6feaed21b7aec7a5658d9f`, tree
`52885025518b8ed032bd8c118987c308ecb5b671`, parent
`9819aaa632e59f0a439c639f0dae9acf3f17fd75`; fetched full tree equals reviewed local
code `b27075dde795280b99e884c6f49609f87045f605`. Baseline checkpoint
`139e54cd10dbe0a854326b7e1f4b97c58e3fbb13`, clean/root-only writer verified;
no concurrent Git/build operation, old worktrees preserved. PR88 expected-HEAD non-force
source update verified. Public repository/four upload guards unchanged and OFF; no release.

Full final CI679 Windows15529/Ubuntu578-line logs independently read: tested merge
`d994f31fded7861f588f57a4c7b8b4da9ad3235c`, Windows120981548253/Ubuntu120981548324,
both SUCCESS. PendingProof11/Reconcile13/RollbackOutcome12/Maintenance19/Outcome10/
RestartGate8/Journal8/Rollback8/Payload9/Stage8/Backup8/SelfCheck9/SOCKS51/Desktop6388/
policy214/Instance30/Maintenance126/Shutdown47/HomeVPN4816/Portable91/prior suites PASS.
Pack runner-only/uploads-prune SKIPPED. Sharing/Python/rebind incidents UNKNOWN, not fixes.

ONE prerequisite: Startup artifact of existing auxiliary path. Existing Install API remains
compatible; installer opts into evidence via existing Initialize-ProGoShortcuts adapter.
Actual created bytes must equal this operation's EnsureLink result and match expected target/
arguments; preserved existing/customized/disabled or absent Startup state must equal actual
before/after bytes/absence. Observational read-only verifier binds expected install/location
hash identities and current exact bytes; mismatch/foreign edit returns false without repair.
Receipt contains no raw paths/shortcut bytes/arguments, records created vs preserved (not a
false ownership claim for custom links). Production requires receipt/non-null matching root
identity; persists it with durable phase/inventory/admission/runtime ownership proof.
The maintenance fixture adapter now returns synthetic evidence; all old assertions/timeouts/
scenarios/guards remain unchanged. No other lifecycle or transport/runtime recovery changes.

StartupEvidence13 uses only own GUID temporary Programs/Startup overrides and actual adapter/
COM type. Created/existing-absent/existing-custom/existing-owned/fresh-owned/no-startup-
absent/no-startup-custom/conflict/changed/appeared/other-identity/other-location/durable.
Exact preserved custom/absence, no foreign overwrite/repair, rejected identity/location,
missing/foreign receipt refused before pending creation, real durable writer round trip and
private/settings/vault/key-ref/foreign byte preservation. Even valid Startup proof cannot
publish pending-installed or unblock the existing admission gate. No live known folders used.
Read-only observations are not an atomic lock against all future external edits.

Diff7files238add/7delete: ApplicationShortcuts.cs; Shortcuts-ProGo.ps1; Install-ProGo.ps1;
one Test-ProGo harness call; Maintenance mock interface; new StartupEvidence13; evidence doc.
Local review/static parity/API signatures/C#5 review/diff/secrets-PII/public-content347/0
PASS. Old COM writes/guards/MigrateMenu, pure pending/snapshot/rollback helpers, old assertions
and all timeouts unchanged. PowerShell parser/C# compilation/Windows COM/StartupEvidence13/
new exact regressions NOT_RUN (tools unavailable here). No prior PASS inherited.

ONE automatic [CI680/40133945222](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40133945222),
sourcec7eb7c3/pull_request/count1/in_progress/conclusion null observed; no manual dispatch/rerun/
duplicate/full wait. Local docs-only checkpoint not pushed, avoiding another CI.
Next SAME680 final logs/tested merge/StartupEvidence13 plus all prior suites. Mandatory
next bounded auxiliary evidence: Start Menu primary/legacy migration; durable deserialization/
trusted known-folder/current-result revalidation/conflict contract; then safe pending-installed
metadata promotion. NOT completed here. Runtime crash recovery/durable partial ownership,
retention protecting unresolved evidence, auxiliary rollback and other F23/PGA04/fallback/
package/owner gates remain OPEN. Original failure/no Launch after rollback unchanged.
BLOCKED_TRANSFER, no delivery retry. No live installation/PC/VPS/services/ACL/security/network/
credentials/release/uploads actions. STOP one stage, no second stage.
Реализация58–80/100 · Проверенная готовность22/100 · Изменение+0; fixed weights unchanged.

## F23 pending installed proof prerequisite; CI679 in progress

Published source `9819aaa632e59f0a439c639f0dae9acf3f17fd75`, tree
`21732affee9676f0a2a7a8dbbe1b4e1e74d44fa7`, parent
`abdaa54c6593da016e37bdeeb00a2dbfeb2e4344`; fetched parent/tree equals reviewed
local code `c788163` tree. Baseline clean checkpoint
`3a4a7a793147cd451742e62aae27933f73c5ef71`; root-only active writer, no running
Git/build operation; old worktrees preserved. Guarded expected-HEAD non-force push to PR88.

Full CI678 final Windows15517/Ubuntu578 lines independently read, tested merge
`0922838486cb76ed71a8661244516a2727f118d7`, Windows120972858687/Ubuntu120972858686
both SUCCESS. Reconcile13/RollbackOutcome12/Maintenance19/Outcome10/RestartGate8/Journal8/
Rollback8/Payload9/Stage8/Backup8/SelfCheck9/SOCKS51/Desktop6388/policy214/Instance30/
Maintenance126/Shutdown47/HomeVPN4816/Portable91/prior suites PASS. Pack runner-only,
uploads/prune SKIPPED. Sharing/Python/rebind incidents still UNKNOWN, not runtime fixes.

ONE minimum prerequisite: old pending-installed evidence lacked completed phase and owned
byte proof. Actual production writer now records ordered self-check/shortcuts only after
both existing calls successfully return, plus every captured owned hash. Explicit production
completion proof is read-only verified before atomic terminal publication. New reader
requires prior strict snapshot/identity/exact full inventory/linked admissions, completed
phase schema/order, unique complete owned hashes and actual CURRENT new runtime bytes.
No automatic pending publication: old/new pending journals remain fail-closed. Intent or a
file is never authority. Auxiliary-state contract/current external shortcut state remains a
separate dependency before safe restart metadata publication; no automatic runtime restore.
See F23_INSTALLER_PENDING_INSTALLED_PROOF.md for exact acceptance and limits.

Diff4files146 additions/3 deletions: Install-ProGo helper/writer/caller, one harness call,
new PendingProof11, evidence document. Existing old tests/assertions/timeouts, snapshot/
rollback/pending resolver, settings/vault/keys/foreign guards and workflows unchanged.
PendingProof11 complete/fresh/legacy/phases/ownership/inventory/foreign-runtime/snapshot-
corrupt/journal-corrupt/identity/conflict; actual owned writer interrupted after Flush,
all fixture bytes/file count unchanged, ALL pending gates still blocked. No live actions.
Local review/signatures/helper parity/production call order/no reader mutation/diff/secretPII
scan/public-content345/0 PASS. First old fixture byte comparison failed solely worktree CRLF
versus canonical Git LF; canonical blob equality then PASS, no fixture changed. No local
PowerShell: parser/compilation/PendingProof11/exact native regressions NOT_RUN.

ONE automatic [CI679/40130683445](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40130683445),
source9819aaa/pull_request/count1/in_progress/conclusion null observed. No duplicate/rerun/
dispatch/full wait. Public upload guards unchanged/OFF. Local docs-only checkpoint NOT pushed.
Next SAME679 final logs/exact tested merge/PendingProof11 plus prior suites; then one bounded
safe pending-installed publication prerequisite concerning auxiliary completion, without replay.
Mandatory residual: other pending/partial crash states and durable byte ownership/runtime
recovery; retention protecting unresolved proofs; auxiliary-state rollback; F23/PGA04/updater
fallback/package/owner acceptance. BLOCKED_TRANSFER, no retry. No release/uploads/install/
VPS/PC/service/ACL/security/network/credentials actions. STOP one stage, no second stage.
Реализация58–80/100 · Проверенная готовность22/100 · Изменение+0; fixed weights unchanged.

## F23 proven pending rollback metadata reconciliation; CI678 queued

Published source `abdaa54c6593da016e37bdeeb00a2dbfeb2e4344`, tree
`a50d105a9e872d584e0cb3d287df4a12088d5d8a`, parent
`227d83774e82a101c2c2c67dee83a4120d1f2be6`; fetched tree equals reviewed local code
`bb50bf040892735e39b3b65d1917329545660b0d`. Baseline checkpoint
`4f7bd34ef8dc013dda8fc6854350a55bf4f2fab6`, clean/root-only active writer verified;
old worktrees preserved, no unknown checkpoint replay/reset/clean/merge.

Full final CI677 Windows15503/Ubuntu578-line logs independently read, both SUCCESS;
source227d837/tested merge `b6711df3c075c1fe6842be8c9922c4bc6e77d60f`,
Windows120964057190/Ubuntu120964057033. SOCKS51/RollbackOutcome12/Maintenance19/
Outcome10/RestartGate8/Journal8/Rollback8/Payload9/Stage8/Backup8/SelfCheck9/
Desktop6388/policy214/Instance30/Maintenance126/Shutdown47/HomeVPN4816/Portable91/prior
suites PASS. Pack runner-only/uploads-prune SKIPPED. CI676 sharing failure did NOT recur,
root UNKNOWN, not a runtime fix; prior Python/rebind incidents UNKNOWN, diagnostics kept.
No owner-PC/whole installer/production acceptance inferred.

Chosen ONE minimal unfinished phase: only fully durable rollback.pending.json whose saved
snapshot/identity/ownership/admission proof passes existing verifier AND actual CURRENT old
bytes/absence still match. New installer invocation acquires maintenance, resolves this
phase before original pending gate/Stop/staging/write. Scan/validate ALL journals first,
reject unknown/corrupt/conflicting entries or multiple pending rollback candidates before
any rename. Recheck snapshot, actual old runtime and proof immediately before atomic
no-overwrite Move to rolled-back.json. This ONLY moves own metadata without changing its
bytes; it never restores runtime, deletes journal, repeats external action or trusts intent.
Current-process original failed invocation still throws/no Launch; direct pending gate
semantics unchanged until an explicit new production invocation reconciles saved evidence.
Historical verified terminals skipped unchanged. Pending installed/outcome phases, intent-
only/incomplete/invalid/foreign/appeared-old-absence/conflicting states stay fail-closed.
No claim against every hostile external pathname race or directory power-loss case added.

Reconcile13 own-temp/mock cases existing/fresh/partial/corrupt/snapshot/foreign/absence/
identity/ownership/conflict/intent-only/pending-installed/multiple. Build genuine durable
pending by error after Flush on actual rollback writer, then actual installer body as NEW
attempt with no old memory admission passed. Only three proven cases reach fake Stop
(sentinel refusal prevents real setup); all others block before Stop, maintenance releases
once, no Install OK/Launch. Confirm exact metadata hash/name-only change and every runtime/
private/settings/vault/key-ref/foreign/snapshot/other journal byte plus file count unchanged.
Multiple case has two independently valid pending proofs and neither can publish. Original
Outcome10/RollbackOutcome12/Maintenance19/RestartGate8/Journal8/Rollback8 assertions and
all timeouts unchanged. Published diff4files192 additions:37 production lines +1 harness
call +new test/evidence doc. No old fixture/runtime path rewrites or security/live actions.

Local docs3/static/review/signatures/proof helper parity/actual gate order/preflight-before-
rename/no-runtime-write/no-replay/diff/JSON/secrets/public-content343/0 PASS. No local
PowerShell/Windows; new Reconcile13/exact compilation/prior native suites NOT_RUN.
ONE automatic [CI678/40127796450](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40127796450),
sourceabdaa54/count1/pull_request/queued/conclusion null last observed. Expected-HEAD
non-force source push/fetched exact parent+tree verified; public repo/four upload guards
unchanged. Docs-only checkpoint NOT pushed; no rerun/dispatch/duplicateCI/full CI wait.
No install/release/uploads/live/services/ACL/security/network/credentials/transfer actions.
Privatecandidate BLOCKED_TRANSFER, no delivery retry. Readiness58–80/22,+0, weights unchanged.

Next SAME678 final logs/tested merge/Reconcile13 plus all regressions. Mandatory next
bounded dependency: remaining unfinished installed/rollback commit phases with independent
proof; incomplete admission/post-crash partial-byte ownership and bounded runtime recovery
without replay/foreign overwrite; retention protecting unresolved evidence; auxiliary-state/
shortcut transaction; remaining F23/PGA04/fallback/package/owner acceptance. Full crash
recovery or production installer readiness NOT claimed. STOP one stage, no second stage.

## CI676 SOCKS pending receipt sharing: test-only owner/stack experiment, CI677 in progress

Published source `227d83774e82a101c2c2c67dee83a4120d1f2be6`, tree
`1f286822de92c969d67c1b435bdd06a9190d08b9`, parent
`1fff0f05024d7249495b9594fef162074e5aa95a`; fetched tree equals reviewed local code
`0a8273402538cbefc9617628c64f5a3ac32fd6a1`. Baseline checkpoint
`c9548c7f51bcf23a7a4e1c867338a841dcea48dd`, clean/root-only active writer verified.
Old worktrees retained, no unknown operation replay/reset/clean/merge.

Full CI676 Windows1953/Ubuntu578-line final logs independently read; Windows FAILURE,
Ubuntu SUCCESS. Source1fff0f0/tested merge `1eb3bac1d59e9e0e066fd7635e33264f5dfb5181`,
Windows120959851985/Ubuntu120959852104. Last PASS PermanentErrorFallback18 assertions,
then IOException sharing violation on own GUID.json.pending from SocksRecoveryTests.exe,
Test-ProGo.ps1:456. Next registered scenario SshDiagnosticDistinctions. Managed stack and
actual owner absent due native stderr truncation. Earlier repair/vault/logged SOCKS cases
scoped PASS only; whole SOCKS suite not complete. RollbackOutcome12/Maintenance19/Desktop/
Instance/relay and later suites NOT_RUN. Do not inherit previous installer PASS onto676.

No SocksRecoveryTests or ProxyService diff from675 to676; installer regression/runtime
leak not established. Source facts: child writes unique pending receipt with WriteAllText
(disposes stream) -> Move; capture reads only final json. Scenario nested using disposes
Fixture/Proxy then replay then capture, whose env restore precedes Directory.Delete.
Proxy.Dispose cancels and calls owned StopProcessOnly; async Stop has distinct settlement
path. Which operation and lock owner failed remains UNKNOWN; race is only a hypothesis.
See CI676_SOCKS_PENDING_DIAGNOSTIC.md for distinguishing experiment and interpretation.

Published ONLY tests/SocksRecoveryTests.cs + evidence doc (156 additions3 deletions).
On IOException scenario wrapper prints safe operation/scenario/type/HRESULT + up to32
managed method/IL frames on stdout, original exception rethrown. Same Fixture.Dispose
observed before/after without extra Stop/wait/kill; capture pending startup/Stop and owned
process roles. Only capture-directory IOException triggers one own GUID pending-file
Restart Manager owner query: scoped/reparse guards, one file, <=16 owners/2GetList/no retry,
finally EndSession; owner roles/counts only, no PID/path/app/service/credential data.
Temporary diagnostic session registration only; no Shutdown/Restart/service/security
configuration. Name-derived claimed writer is not assumed actual lock owner; process
start stamp/executable role checked when readable. Natural exit may yield no owners and
must not be called proof of no earlier lock. Original scenarios/child writer/assertions/
timeouts/share modes/cleanup order byte-equivalent; production and installer unchanged.
A passing run would only non-reproduce failure, not prove root/fix.

Local docs3/static/review/signatures/scenario+writer parity/asserts+budgets/diff/JSON/secrets/
public-content341/0 PASS. No local C#/Windows compiler/native execution: diagnostic/SOCKS
suite/installer RollbackOutcome12/Maintenance19 and all later suites NOT_RUN new source.
ONE automatic [CI677/40124878568](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40124878568),
source227d837/count1/pull_request/in_progress/conclusion null last observed. Initial metadata
count0 then count1 after fetched source proof; no rerun/dispatch. Expected-HEAD non-force
source update and fetched exact parent/tree verified. Public repo/four upload guards
unchanged. Docs-only checkpoint not pushed, no second CI/full wait.

Next SAME677 full final logs/tested merge; if recurrence inspect SOCKS_FIXTURE_IO/STACK/LOCK
operation and owner roles before any runtime/fixture fix. Functional F23 progression paused
until PASS. Unfinished outcome phases/crash recovery/retention/auxiliary-state rollback
remain OPEN. Old Python/rebind incidents UNKNOWN, diagnostics retained. Privatecandidate
BLOCKED_TRANSFER, no delivery retry. No install/release/uploads/live/VPS/services/ACL/
network/security/credentials changes. Readiness58–80/22,+0; weights unchanged. STOP one stage.

## F23 verified runtime rolled-back terminal; CI676 in progress

Published source `1fff0f05024d7249495b9594fef162074e5aa95a`, tree
`de30cb3d16fea805176e5fe9f9ebc5792477f505`, parent
`a838d021fc631ca40bb9ec521f5b4f6409b68f57`; fetched tree equals reviewed local code
`daaf754e841c232c51fe08e6477c147be086bb8b`. Baseline checkpoint
`63c5122b3f4e92638f9853a9c2c5de7a1398c97a`, clean/root-only active writer verified.
Old worktrees preserved, no unknown checkpoint replay/reset/clean/merge.

Full CI675 Windows15488/Ubuntu578-line final logs independently read, both SUCCESS.
Exact sourcea838d02/tested merge `5342f65d48e99bc85440e8356cf8d9e60f20ca4a`,
Windows120950820808/Ubuntu120950821010. Outcome10/Maintenance17/RestartGate8/Journal8/
Rollback8/Payload9/Stage8/Backup8/SelfCheck9/Desktop6388/policy214/Instance30/
Maintenance126/Shutdown47/HomeVPN4816/Portable91/prior suites PASS. Pack runner-only,
uploads/prune SKIPPED. Python/rebind incidents CI662/670/671 UNKNOWN; diagnostics retained.
Non-reproduction not fixes, no owner-PC or whole installer/production acceptance inferred.

Chosen ONE runtime rollback success terminal, NOT successful installation or automatic
recovery. After current-process rollback and actual full old bytes/absence proof, verify
snapshot/manifest, identity, known journal and exact nonempty one-to-one captured own
write/admission coverage. Persist separate rolled-back/RuntimeState original-verified,
root hash/journal GUID, full whitelisted inventory/snapshot, ordered admission digests and
captured owned hashes. CreateNew/WriteThrough/Flush(true)/same-handle readback, full evidence
validation, then atomic Move rollback.pending.json -> rolled-back.json without overwrite.
Reader shares unchanged installed proof rules and admits known rolled-back with exact
subset/ownership proofs. Restart can admit NEW explicit attempt only on verified terminal.
No restore/replay/journal deletion occurs at restart; current live files still verified by
new install ownership checks. Historical terminal is runtime completion evidence only.

Main catch attempts terminal only after actual priorVerified and writes>0; persistence
failure stays pending, preserves original ErrorRecord and never flips install to success.
Original throw/no Install OK/Launch and maintenance/stage finally unchanged. Additional
rollbackTerminalVerified boolean is not installation success. Unknown/extra journal files,
incomplete/unconfirmed admission or failed restoration stay closed, including unfinished
installed outcome pending files (not deleted/reinterpreted). No-write unproven failures
remain pending. Snapshot/actual rollback/exclusive write/private exclusions unchanged;
settings/vault/keys and foreign changes are never terminal restore targets.

RollbackOutcome12 own-temp/mock cases existing/fresh/partial/unrestored/foreign/snapshot/
ownership/record-corrupt/persist-fail/short/after-write-fail/identity. Actual old bytes or
absence, actual partial hash, invalid material rejection, no installed marker, no runtime
mutation by terminal writer, restart gate pass/refusal and new admission retaining history;
private/foreign bytes intact. Maintenance17 preserved plus2 actual installer-tail rollback
terminal persistence failures -> Maintenance19; precise original self-check failure/old
bytes/no Launch retained, unconfirmed terminal blocks. Known existing copy/partial/selfcheck/
shortcut failure cases also require rolled-back evidence and accepted gate. Outcome10/
RestartGate8/Journal8/Rollback8/prior suites still registered unchanged; no timeout relaxed.
Published diff5files228 additions11 deletions: installer/shared reader, one test call,
maintenance assertions+2cases, new rollback outcome tests and scope/evidence doc.

Local docs3/static/review/signatures/helper parity/installed full-count/order/atomic boundary/
original-error/no Launch/diff/JSON/secrets/public-content340/0 PASS. No PowerShell/Windows
locally: new RollbackOutcome12/Maintenance19/exact compilation/all regressions NOT_RUN.
ONE automatic [CI676/40123483430](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40123483430),
source1fff0f0/count1/pull_request/in_progress/conclusion null last observed. Initial metadata
count0 then count1 after fetch; no dispatch/rerun. Expected-HEAD non-force source push and
fetched exact parent/tree verified; public repo/four upload guards unchanged. Docs-only
checkpoint not pushed. No release/uploads/install/live/services/ACL/security/network/
credential/transfer actions. Privatecandidate BLOCKED_TRANSFER, no retry.

Next SAME676 full final logs/tested merge/RollbackOutcome12/Maintenance19/prior regressions.
Mandatory next separate bounded slice unfinished terminal/outcome phase reconciliation with
independently verified snapshot/identity/ownership, then post-crash partial ownership proof
and bounded interrupted recovery/no replay/foreign overwrite, retention preserving unresolved
evidence and auxiliary-state/shortcut transaction, remaining F23/PGA04/fallback/package/owner
gates. Runtime rolled-back outcome does NOT claim auxiliary rollback or full installer ready.
Readiness58–80/22,+0; weights unchanged. STOP one bounded stage, no full CI wait/second stage.

## F23 bounded installed terminal outcome; CI675 in progress

Published source `a838d021fc631ca40bb9ec521f5b4f6409b68f57`, tree
`066bf27f5c247d45d2f18bb3916e51e94fb66057`, parent
`44bcf31080c685701e9eb8598fa25df7c3d5b57a`; fetched tree equals reviewed local code
`49bfa093e89e124e4befc0c315c2408216121c74`. Baseline checkpoint
`c01abbc1e15e8880822a2d01c3a1baa60e2623fa`, clean/root-only active writer verified.
Old worktrees retained, no lost checkpoint replay/reset/clean/merge.

Full final CI674 Windows15475/Ubuntu578-line logs independently read, both SUCCESS;
source44bcf31/tested merge `99137658839fa18f6440df840db94470bf9792ac`,
Windows120942960299/Ubuntu120942960086. RestartGate8/Journal8/Rollback8/Maintenance15/
Payload9/Stage8/Backup8/SelfCheck9/Desktop6388/policy214/Instance30/Maintenance126/
Shutdown47/HomeVPN4816/Portable91 and prior suites PASS. Pack runner-only/uploads-prune
SKIPPED. Old Python startup CI662/670 and CI671 rebind roots UNKNOWN; diagnostics retained.
Non-reproduction not fixes, no owner-PC or whole installer/production acceptance inferred.

Chosen ONE success terminal path: after existing self-check and shortcuts, verify full
saved snapshot/manifest, exact new runtime hashes, complete own write records and all
admissions. Write immutable outcome.pending.json CreateNew/WriteThrough/Flush(true), exact
same-handle text readback; verify all evidence then atomic File.Move to installed.json
without overwrite. No fallible post-publication validation/rollback on an already-committed
terminal boundary. Even exception after full durable pending write leaves no installed
outcome. Main original failure/rollback/no Install OK/Launch, stage/finally preserved.

Terminal binds install-root SHA256 identity, journal GUID, exact runtime-only whitelist,
snapshot name/hash, all admission digests/sequence/inventory/entries. Restart gate accepts
only this verified installed evidence; old intent-only/unknown/corrupt/missing/extra/wrong
identity remains fail-closed. Historic terminal validates saved completion evidence, not
historical live runtime hashes that later legitimate updates may replace. New install
still captures current runtime and enforces unchanged exclusive ownership checks; terminal
never authorizes restoration/overwrite after new admission. No automatic recovery/deletion.
Payload name list extracted to a shared pure helper unchanged in content; Payload9 AST
loader explicitly loads dependency, existing assertions retained. No private settings/
vault/key contents included or read by terminal schema. Snapshot/owned-write/rollback/
self-check helpers unchanged except success-path terminal integration.

Outcome10 own-temp/mock cases complete/fresh/persist-fail/short/after-write-fail/
foreign-runtime/snapshot-corrupt/journal-corrupt/identity/unknown. Check full actual streams/
bytes, no false installed marker, restart pass/refusal, new admission without deleting old
terminal, rollback after outcome failure stays pending, private/foreign bytes unchanged.
Maintenance15 preserved plus2 actual installer-tail terminal failure cases (17): before
write/after flush both restore exact old bytes, no installed/success/Launch. Old timeout/
assertions unchanged; Journal8/Rollback8/RestartGate8 remain registered. Published diff
6files259 additions8 deletions: installer, one harness call, payload fixture dependency,
maintenance extra cases, new outcome tests and scope/evidence doc.

Local docs3/static/review/signatures/helper parity/list identity/order/atomic boundary/
diff/JSON/secrets/public-content338/0 PASS. No PowerShell/Windows locally; new Outcome10/
Maintenance17/exact compilation and all regressions NOT_RUN until exact final logs.
ONE automatic [CI675/40120509957](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40120509957),
sourcea838d02/count1/pull_request/in_progress/conclusion null last observed; initial metadata
count0 observed then count1 after fetch, no manual dispatch/rerun. Expected-HEAD non-force
source push/fetched exact parent+tree verified. Public repo/four upload guards unchanged;
no release/uploads/install/live/services/ACL/security/network/credential actions.
Docs-only checkpoint not pushed. Next SAME675 full final logs/tested merge/Outcome10/
Maintenance17 and prior suites. Mandatory next separate slice durable rolled-back/failed
outcomes with verified ownership/snapshot/phase transition; then bounded interrupted
recovery/no replay/foreign overwrite, retention and auxiliary-state/shortcut transaction,
remaining F23/PGA04/updater fallback/package/owner gates. Privatecandidate BLOCKED_TRANSFER,
no delivery retry. Readiness58–80/22,+0; weights unchanged. STOP one bounded stage.

## F23 bounded restart pending-detection gate; CI674 in progress

Published source `44bcf31080c685701e9eb8598fa25df7c3d5b57a`, tree
`a26b478949705b01261a59e3958589b47f325b6b`, parent
`f7ae8b2a03a9fea0141dc63118a273648b55bc2f`; fetched tree equals reviewed local code
`183de5e9ee912c57bf75130e6aa3f79304ebf1fa`. Baseline checkpoint
`c881c28714caa2b3152a7644ea04d339ac3f3b19`, clean/root-only active writer checked;
old worktrees retained, no replay/rollback of unknown operations.

Full CI673 Windows15466/Ubuntu578-line final logs independently read, both SUCCESS.
Sourcef7ae8b2/tested merge `8202d37e4687c29e3205893455c8cbab1a29b280`,
Windows120934803351/Ubuntu120934802966. Journal8 ALL PASS (existing/fresh/persist-fail/
short-write/unknown/invalid/tampered/partial); Rollback8/Maintenance15/Payload9/Stage8/
Backup8/SelfCheck9/Desktop6388/policy214/Instance30/Maintenance126/Shutdown47/HomeVPN4816/
Portable91 and previous suites PASS. Pack runner-only/uploads+prune SKIPPED.
Python startup CI662/670 and CI671 rebind roots remain UNKNOWN; diagnostics retained,
non-reproduction NOT fixes. No owner-PC/whole installer/production acceptance inferred.

Chosen ONE minimum production slice: Assert-ProGoInstallerNoPendingJournal under existing
maintenance owner before Stop, staging or writes; also direct NewJournal before creation.
Read only guarded direct backups metadata; no journal/credential contents. Missing parent
or ordinary backups permit continuation. Any installer-journal namespace entry (case
insensitive; directory/file/malformed name; empty/unknown/corrupt/intent) refuses without
restore/delete/new admission. Existing ancestor/reparse/path guards remain active.
Acceptance: pending state cannot reach Stop/new runtime/stage/journal mutation, exact
failure propagated, maintenance disposed, no Install OK/Launch, all existing bytes intact.

There is no durable terminal outcome yet, so even intent retained after previous successful
install is conservatively pending. Normal restart continuation/reinstall is NOT complete;
do not bypass this gate by deleting a journal or trusting a Phase complete string. Intent
alone never proves snapshot identity or ownership after crash. Existing current-session
admitted writes still use prior exact journal/snapshot/owned-write validation unchanged.

New RestartGate8 tests actual production tail with no reconstructed in-memory journal and
mock maintenance/Stop boundary, own temp paths only: no-parent/ordinary/empty/intent/corrupt/
schema/file/malformed. Prove Stop reached only clean cases, exact pending refusal for both
public route/direct journal API, single Dispose/no success/Launch, full runtime/private/
foreign/journal file inventory+hash parity. Published diff4files144 additions:15 production
gate lines +one test registration +new test/evidence doc. Old assertions/timeouts/helpers
unchanged; no real installation/services/ACL/security/network/credentials/PC/VPS actions.

Local docs3/static/review/signatures/order/helper parity/old asserts+budgets/diff/JSON/
secrets/public-content336/0 PASS. No local PowerShell/Windows; RestartGate8/new compilation/
Journal8/Rollback8/Maintenance15 and prior suites NOT_RUN exact new source.
ONE automatic [CI674/40117901265](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40117901265),
source44bcf31/count1/pull_request/in_progress/conclusion null last observed. Expected-HEAD
non-force source update/fetched exact parent+tree verified. Public repo/four upload guards
unchanged, no release/uploads/install/live/transfer action. Docs-only checkpoint NOT pushed.

Next SAME674 exact final logs/tested merge/RestartGate8 and all regressions. Mandatory next
separate slice durable terminal/outcome schema and verified transition with snapshot,
identity and ownership proof, then recognition of terminal state/restart continuation,
bounded interrupted recovery/no replay/foreign overwrite, retention and auxiliary-state/
shortcut transaction plus remaining F23/PGA04/fallback/package gates. No automatic restore
implemented here. Privatecandidate BLOCKED_TRANSFER, no delivery retry. Readiness58–80/22,+0,
weights unchanged. STOP one bounded stage, no full CI wait or second stage.

## F23 current-session durable admission-before-write; CI673 in progress

Published source `f7ae8b2a03a9fea0141dc63118a273648b55bc2f`, tree
`8c4628a3b72131a73c1662628b980d557d4da843`, parent
`efc4cc91c19baff6dfcbdc25ef666df13362a485`; fetched tree equals reviewed local code
`cb2b244cac1a6f13d6e7ba9284eb3a63054e2d8c`. Baseline checkpoint
`7ebfc6143bc83a3c2e45da265b372e448b317110`, clean/root-only active writer checked.
Original/recovery worktrees retained, no unknown operation replayed.

Full final CI672 Windows15457/Ubuntu578-line logs independently read, both SUCCESS.
Exact sourceefc4cc9/tested merge `806d4512dd5355542806e1ad934f8c50e4181e45`,
Windows120927737675/Ubuntu120927737758. Rollback8 ALL PASS (including actual partial
byte42/admission hash), Maintenance15/Payload9/Stage8/Backup8/SelfCheck9,
Desktop6388/policy214/Instance30/Maintenance126/Shutdown47/HomeVPN4816/Portable91 and
prior suites PASS. Pack runner-only; uploads/prune SKIPPED. Python exit0 and original
1s queued Stop rebind PASS; CI662/670 and CI671 roots remain UNKNOWN, diagnostic retained,
non-reproduction NOT incident fixes. No owner-PC/wholeF23/production acceptance inferred.

Acceptance of ONE new bounded production prerequisite: persist a Schema1 admission tied
to exact runtime inventory/snapshot before corresponding runtime leaf creation/modification;
Flush(true) and exact byte readback through SAME WriteThrough/CreateNew handle required.
Pin intent (FileShare.Read) through existing exclusive runtime-write and partial-hash capture.
Nested finally releases runtime/source/admission even if disposal fails. Invalid/unknown/
missing/tampered ACTIVE journal or failed/short persistence refuses before target creation
or mutation. Existing snapshot verification, ownership/hash/rollback, original failure
rethrow/no Install OK/Launch, stage/maintenance Stop/finally and private exclusions retained.
Journal own GUID path guarded, no existing file overwritten; intent retained after rollback.
This is NOT restart discovery or full crash recovery; intent alone does not establish
post-crash partial-byte ownership. No power-loss metadata atomicity claim.

New Journal8 own-temp/mock cases existing/fresh/persist-fail/short-write/unknown/invalid/
tampered/partial. Prove unmodified old bytes/absence at persistence, one confirmed/pinned
intent before actual write, exact partial byte/hash, verified rollback, intent retention,
private/foreign byte preservation. New helper APIs used by existing owned setter, so old
Maintenance15/Rollback8 remain registered untouched and exercise production route too.
Published diff4files/228 additions3 deletions: Install-ProGo journal+admission/finally,
Test-ProGo one test registration, new InstallerJournalTests.ps1 and scope/evidence doc.
No workflow/VERSION/other runtime/server or old fixture changes.

Local docs3/static/review/signatures/helper parity/order/asserts/budgets/diff/JSON/secrets/
public-content334/0 PASS. No local PowerShell/Windows: Journal8/new exact compilation/
Maintenance15/Rollback8/prior native suites NOT_RUN pending exact CI.
ONE automatic [CI673/40115153183](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40115153183),
sourcef7ae8b2/count1/pull_request/in_progress/conclusion null last observed. Expected-HEAD
non-force source update and exact fetched parent/tree verified. Public repo/four upload
guards unchanged. No rerun/dispatch/secondCI/release/uploads/install/live/ACL/security/
network/credentials/services actions. Docs-only checkpoint NOT pushed.

Next SAME673 final logs/tested merge/Journal8 and full regressions. Then separate mandatory
restart discovery/validation of pending journals before new admission; durable outcomes/
phase transitions and post-crash ownership proof; bounded interrupted recovery/no replay;
terminal state/retention plus auxiliary-state/shortcut rollback. These are OPEN production
gates, not waived or bundled into this slice. Privatecandidate BLOCKED_TRANSFER, no retry.
Implementation58–80/100, verified22/100,+0; weights unchanged. STOP one bounded stage.

## CI671 cold queued Stop: bounded owner-table experiment, CI672 queued

Published source `efc4cc91c19baff6dfcbdc25ef666df13362a485`, tree
`991516696fb8e2e551692abe8f1ae8fe625dc293`, parent
`cb7632a9dcd46c3f4fe23b9059ad81f306b5a9b7`; fetched exact parent/tree equal reviewed
local code `abc67d6512ad8690cae23b883555df901c311b96`. Baseline checkpoint
`90d98b932a1fb93b507b0afbeba8bc23eb75f43b`, clean/root-only active writer checked;
original30eb216/recoveryb3ec08 and other previous worktrees unchanged.

Full CI671 Windows3407-line final log independently read. Sourcecb7632a/tested merge
`366a0cca22c5743abf4418ba637619ff9721e4b7`, Windows FAILURE, Ubuntu SUCCESS.
Python exit0 passed; CI662/670 root remains UNKNOWN, non-reproduction not a fix.
New failure AsyncSettingsColdStartHeartbeat -> AssertPortReleased -> Occupy after
queued Stop settles durable cold publication. Original settingsPending/Bridge.IsRunning
check PASS, exclusive loopback rebind after1s AddressAlreadyInUse; loopback LISTEN reported.
Desktop not complete; Instance/maintenance15/Rollback8/relay NOT_RUN on671. Early logged
suites scoped PASS only. No owner PID in old log, no runtime leak or fixture blame proven.

Code review: fixture retains occupied and SOCKS listeners, published fallback already
asserted different from occupied. Candidate and accept thread are in-process, no child
started by this slice. Stop queue waits cleanup then Bridge.Stop clears active, session
Stop sets Running false and calls Server.Stop; false flag alone does not prove release.
Need ownership to distinguish retained own socket versus external port reuse.

Published only DesktopAsyncSettingsTests.cs diagnostic + CI671_COLD_STOP_OWNER_DIAGNOSTIC.md
(95 additions/1 deletion). On original1s rebind failure only, add safe lifecycle/cleanup/
endpoint-identity booleans and one read-only GetExtendedTcpTable owner snapshot for exact
port: own/other-process LISTEN, loopback/wildcard/other-address counts and non-LISTEN rows.
No raw PID/address/process args/paths/credentials. Max1MiB/4096rows, two calls/no retry;
read failure is boolean; unmanaged buffer/current process wrapper released. Original
AssertPortReleased/Occupy/loop20ms/1s and finally cleanup preserved; no runtime changes,
no repeated Stop, socket manipulation, killing other processes or security/network changes.
Experiment interpretation documented; success alone only non-reproduces failure.

Local PASS docs3/static/signatures/review/original asserts+budgets/scope/diff/JSON/secrets/
public-content332/0. New exact C# compiler/native experiment and Rollback8 NOT_RUN.
ONE automatic [CI672/40112760540](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40112760540),
sourceefc4cc9/count1/pull_request/queued/conclusion null last observed. Expected-HEAD
non-force push verified, public repo/four artifact upload guards unchanged. No rerun/
dispatch/secondCI/release/uploads/install/live/ACL/credentials/service operations.
Docs-only checkpoint not pushed. Next SAME672 final logs/tested merge; if failure inspect
COLD_STOP_REBIND/COLD_STOP_OWNER to classify ownership before any runtime change.
Journal deferred. CI662/670 root UNKNOWN; privatecandidate BLOCKED_TRANSFER, no transfer
retry. Readiness58–80/22,+0. STOP one diagnostic stage, no full CI wait/second stage.

## CI670 early Python timeout: test-only diagnostic, CI671 in progress

Published source `cb7632a9dcd46c3f4fe23b9059ad81f306b5a9b7`, tree
`38fa21e60df5ddd143f77080f96e8755739d914a`, parent
`9f08110db63dae3df7310b8d024bde5cd3935aa1`; fetched parent/tree verified equal local
reviewed code `c20b18513ac42ffdcff29cae4dac531c56cdf157`. Baseline checkpoint
`2bf512767c63c1f52f380342399f18dd00a7e160`, clean/root-only active writer checked.
Existing recoveryb3ec08 and original30eb216 worktrees preserved; no replay of unknown writes.

Full CI670 Windows2788-line final log independently read, source9f08110/tested merge
`2be5c1e6d6d86f8b3c7a47a1dffd2bccc7676495`, Windows FAILURE/Ubuntu SUCCESS.
Exit0 child start25ms/deadline5044ms, CPU15.625ms/RAM6000640, stdinClosed=True;
streams waiting before kill, then cleanupExited=True/both completed/zero bytes. Body,
bootstrap/encodings/os/site/banner/cleanup markers False, pthVisits0. Fixture body not
observed; root UNKNOWN. No evidence establishes AV/infrastructure/loader/site cause.
Early Repair/updater/transport74/adapter3/progress134/install-window145/update-check72
and earlier logged suites PASS; Desktop/Instance/maintenance/installer/Rollback8/relay NOT_RUN.
Do not transfer CI669 PASS to this source or claim partial-admission test fixed by670.

CI662 full2782-line and CI663 full15400-line logs independently reread. CI662
source db3adc280d500b97487a7991f25c1206248eb5b6 / merge06ae505a9e49082e74e8d09d50b361d28799b15a
also deadline5137ms/CPU0/RAM5984256/waiting streams, insufficient startup phase facts.
CI663 source44d76b2347cebd23c627003e231de86c261e97e1 / merge86d483cd84da1e3220273a6102f017609e5eaf20
completed exit0 with body/bootstrap/encodings/os/site/cleanupTrue. Non-reproduction is
NOT a fix; both timeout incidents remain UNKNOWN. Comparison in CI670_PYTHON_STARTUP_DIAGNOSTIC.md.

Only test runner instrumentation + evidence document published (100 additions/1 deletion).
Same Get-Command.Source selection, argv/body/stdin/verbose/site normal behavior retained.
Presence-only influencing Python environment flags and executable/application booleans;
on deadline before own kill, read own process main-image match and at most128 module roles/
count/read-error flags (Python DLL/C runtime). No paths/module lists/environment values,
config contents/raw error/secret data emitted. Synthetic redaction/present/empty tests2
added; Windows execution NOT_RUN. Original WaitForExit5000/kill-join5000/streams3000,
all assertions and expected negative timeout remain unchanged. No runtime/security changes.

Local docs3/static/review/signatures/original budgets+assertions/scope/diff/JSON/secrets/
public-content331/0 PASS. New exact PowerShell/native/installer/Rollback8 tests NOT_RUN.
ONE automatic [CI671/40111310628](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40111310628),
sourcecb7632a/count1/pull_request/in_progress/conclusion null last observed. No duplicate
CI670/rerun/dispatch. Guarded expectedHEAD non-force source publication; public repo/four
upload guards verified unchanged. Docs-only checkpoint not pushed, no second CI.
Next SAME671 final logs; inspect named selection/loader facts if failure. Successful run
would only non-reproduce timeout, NOT establish root/fix. Journal not started. CI662 root
UNKNOWN; privatecandidate BLOCKED_TRANSFER, no transfer retries/install/release/uploads/
live/ACL/network/credentials/service operations. Readiness58–80/22,+0. STOP one stage.

## CI669 partial-admission fixture correction; sole CI670 in progress

Published source `9f08110db63dae3df7310b8d024bde5cd3935aa1`, tree
`b0ee307f336797b824b5dd0126f1017132f369ea`, parent
`320c598521993220ae5c057301f2ebdc9a340d3d`. Local reviewed code
`c1e4d5476be8fc57afc3db9204af44872bed15e7` identical tree; fetch/parent/tree verified.
Recovery baseline `b3ec08d46bb41ba154e2014554b8b06d2438cadd`, clean/root-only writer;
old checkpoint30eb216 and recovery worktrees preserved unchanged. Prior uncertain
checkpoint outcome remains UNKNOWN, not rewritten as successful.

CI669 full Windows10115-line log independently read: tested merge
`107014f558643c211775afe1700b682f052d4c9c`, Windows FAILURE, Ubuntu SUCCESS.
Maintenance15 ALL PASS including copy-fail/partial-copy, Payload9/Stage8/Backup8/SelfCheck9,
Desktop6388/Instance30/Maintenance126/Shutdown47 and earlier logged suites PASS.
Rollback complete only PASS (1/8), then original real written/partial bytes retained in
admission assertion FAIL (InstallerRollbackTests.ps1:14). Remaining7 not PASS, relay and
later suites NOT_RUN. Maintenance selector fix confirmed, not conflated with this fault.

Proven separate test scope defect: write mock resolves mode to setter FileMode.Open;
partial branch never writes/throws, writeFailed=false fails first original conjunct.
No hash/finally runtime loss proven. Test now freezes Scenario in fixture state; partial
branch uses it and increments fault count. Added exact one-byte42/one-fault proof before
rollback, original exception message/full compound hash assertion unchanged. Held exclusive
handle/hash-in-finally-before-dispose production bookkeeping unchanged. Published scope
only InstallerRollbackTests.ps1 (9 additions/1 deletion) and CI669_PARTIAL_ADMISSION.md.
No runtime/API/maintenance fixture/guards/timeouts/coverage/private-file behavior changes.

PASS local docs3/static/review/signatures/original asserts/diff/secrets/public-content330/0.
No local PowerShell/Windows; new exact Rollback8/regressions NOT_RUN. ONE automatic
[CI670/40110064441](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40110064441), source9f08110,
count1/pull_request/in_progress/conclusion null last observed. No manual dispatch/rerun.
Expected-HEAD non-force update and source parent/tree proof confirmed. Public repository
and four upload guards checked, workflow unchanged; no release/uploads/install/live/security/
network/credential actions. Local checkpoint docs not pushed to avoid another CI.
Next SAME CI670 final logs/tested merge/Rollback8 and later regressions; no journal yet.
Journal/recovery/retention remain mandatory future slices. CI662 root UNKNOWN, diagnostic
retained; privatecandidate BLOCKED_TRANSFER, no delivery retries. Readiness58–80/22,+0.
STOP after this bounded correction, no full CI wait or second stage.

## Recovery checkpoint: exact published source320c598 restored, 2026-10-24

New recovery record; NOT a replay or proof of the prior uncertain checkpoint operation.
The restored environment snapshot did not contain ProGo-ci668-fixture, its local branches,
source320c598 object or CI669 state entry. The last observed checkpoint was
`30eb21604d109a11ea839644d9db099fa733809a`. Whether the interrupted write never ran or
was lost with a later environment snapshot remains UNKNOWN. No missing work was deleted.

PR88 independently checked OPEN/unmerged, exact head
`320c598521993220ae5c057301f2ebdc9a340d3d`. Root was the sole active agent, no git/PowerShell/
dotnet/msbuild process observed. Exact source fetched successfully, then checked out in
separate worktree ProGo-recovery-source320c598 / work/progo-recovery-source320c598.
Source tree `e790435068a82c8d6cc2810198280726bff6efda`, parent
`1bfc5a35ecad56605d385c02ff9cf85f39aa2532`, clean before documentation checkpoint.
Diff exactly two published files: InstallerMaintenanceTests.ps1 + CI668_INSTALLER_FIXTURE.md,
48 additions/3 deletions. Confirmed FixtureState.Mode correction for copy-fail/partial-copy
and added real stream/admission/one fault/cleanup assertions. No runtime delta.

The original clean worktree remains on branch work/progo-f23-rollback-checkpoint at
`30eb21604d109a11ea839644d9db099fa733809a`, unchanged. This new local docs-only commit
has exact published source as parent; it is not a new tested/published runtime SHA.

CI669 https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40108281318 is the existing run.
Its current status and final logs deliberately NOT checked in this recovery stage.
Exact-source Maintenance15/Rollback8 and subsequent Windows results NOT_RUN/unreviewed
here; no inherited PASS. Next separate step: independently read SAME CI669 final logs,
tested merge and actual suites. No push/new CI/build/install/journal/transfer/release/
upload/live/network/security/credential action. CI662 root UNKNOWN; privatecandidate
BLOCKED_TRANSFER. Implementation58–80/100, verified22/100, change0, weights unchanged.

Earlier checkpoints below are historical; this recovery entry and current JSON fields
supersede their old current/pending pointers without rewriting history.

# ProGo: единый реестр фактического состояния

Обновлено23.10.2026 (МСК). Точные текущие source/merge/CI указаны в последнем checkpoint ниже и JSON ci_candidate; локальный код отдельно в active_work.local_development_sha; PR88 OPEN/DRAFT, интеграция в main NOT_RUN. Исходная аудируемая база — `7861ea919eed24c5fe9964fc35a23d3958ba5a9d`. Машиночитаемые поля и точные критерии: [project_state.json](project_state.json). Это текущий реестр; прежние checkpoint-файлы сохраняются как история. Утверждённые требования остаются в [APPROVED_PLAN_31.md](APPROVED_PLAN_31.md) и [SSH_AGENT_AUTOSTART_SPEC.md](SSH_AGENT_AUTOSTART_SPEC.md).

23/31 и 6/55 — разные исторические области приёмки разработки. Эти исторические счётчики не складываются и не переводятся в процент готовности. Отдельная взвешенная оценка v1.0 приведена ниже. В 55 входят 43 результата до установщика и 12 зависимых приёмок. Шесть исторических Code/Dev PASS: F17-R02, F17-R03, F31-R01, F31-R02, F31-R03, F31-R04. Их полная перепроверка нового кандидата пока NOT_RUN. Верхнеуровневый PASS требует всех применимых слоёв.

Слои каждого ID: реализация; реальный пользовательский маршрут; проверка/commit; интеграция; пакет; целевой ПК. Код находится в PR88, не в main. Общий финальный installer отсутствует, установка и приёмка на ПК владельца NOT_RUN. PARTIAL в таблице не отменяет прежний узкий PASS. FAIL обозначает подтверждённое нарушение критерия; BLOCKED — конкретную внешнюю зависимость.

CI582 независимо проверен через jobs и Windows log: Windows/Ubuntu SUCCESS, source `b02b0e9606fb57518ed18d17de42e5f82fca8fa8`, merge `cf2e19691057c143b4f66d93c0041e2e1c5681a8`. На базе `7861ea9` только четыре Markdown-файла поверх tested source; runtime/tests/scripts/server/workflows/VERSION идентичны. Новые изменения требуют нового CI.

## Текущий этап: F23 verified in-process owned runtime rollback

PR88 OPEN/DRAFT source `5690e559fbaaab452cf74f5b8c8cd0ce841568ad`, checkpoint
`27888f88dea6e95d618085e291d249f747142fa3`, clean/idle/root-only active writer verified.
Full CI667 Windows15442-line/Ubuntu578 final logs independently read; tested merge
`fe740a532c973a9058431d0a12d6f47955ca9449`, both jobs completed SUCCESS. SelfCheck9/Maintenance13/
Backup8/Stage8/Payload9 all PASS; Desktop6388/policy214/Instance30/Maintenance126/Shutdown47/HomeVPN4816/
Portable91 and previous owned transport/repair/update suites PASS. Pack runner-only, uploads/prune SKIPPED.
CI662 root UNKNOWN; non-reproduction not a fix, diagnostic retained. Owner target/transfer NOT_RUN.

Code `502f4054e67f9b6c2be4e396475b86315fb3ad34`: one connected rollback slice, current process only. Existing runtime-copy destinations and
inventory preserved, but calls route through owned leaf admission. Source stream verified against frozen
staged hash; target exclusively opened and old bytes/absence rechecked, then admission recorded. Full or
partial write hash captured through that same held handle before Dispose. No arbitrary folder copy or
untracked runtime entry admitted; only same declared home-vpn descendants. This ownership prerequisite is
needed to distinguish own partial bytes from a subsequently changed external file. Hygiene marker updated
to require equivalent controlled copy, not removed. Snapshot/stage/self-check/Enter/Stop guards unchanged.

On installation failure, verify whole saved snapshot+manifest before restore, reverse only admitted runtime
writes, require current bytes match captured own hash (or already exact old bytes); changed ownership
refused. Old existing leaf copied from verified snapshot and rehashed; originally absent own leaf removed.
Final whole old-byte/absence proof required for rollbackVerified=True. Confirmed snapshot retained on every
outcome. Original installation error always rethrown, even after verified rollback: no Install OK or Launch.
Failure diagnostics fixed booleans/type only, no raw paths/content. Stage cleanup/release remain nested
finally after rollback. Only runtime leaves restored; personal settings/vault/key refs and unrelated paths
never in inventory. No directory/shortcut rollback claimed, no protection against every hostile external
path race claimed. Durable journal/recovery and complete auxiliary-state/retention integration remain open.

Rollback8: exact old restore; actual partial-write exception; restore-copy error; silently wrong restored
bytes; corrupted snapshot refuses before mutation; external changed runtime untouched; unadmitted leaf
preserved and full restore proof fails; fresh own leaf removal. Uses real fixture streams/hashes/exclusive
sharing and simulated stream/restore failures, no live processes/security changes. Maintenance original13
scenarios retained plus rollback-fail/snapshot-corrupt (15). Injection now occurs inside stream write rather
than broad Copy-Item mock. For self-check-fail/partial-copy, old prior=False expectation changes only because
rollback actually restores exact original bytes/absence, now explicitly asserted. Bad-restore still fails
ownership proof; rollback-fail never claims success. Original installation exceptions preserved. Existing
SelfCheck9/Backup8/Stage8/Payload9 unchanged; all data under own new GUID temp fixtures, no real install/IPC/
service/ACL/network/credential changes.

PASS local static/review/signatures, same prior guards/timeout and original destination parity, docs3,
JSON/diff/public-content/secrets. Windows parser/Rollback8/Maintenance15/prior suites NOT_RUN for new source
until ONE automatic CI; old CI667 PASS not inherited. F23 PARTIAL, fixed readiness58–80/22,+0. Next SAME new
CI exact final logs/merge/suites, then ONE bounded durable recovery journal/admission-before-write slice;
retention mandatory next, plus auxiliary-state transaction/fallback/PGA04 opt-in/package/target gates.
Privatecandidate BLOCKED_TRANSFER unchanged; no retry/delivery/install/release/uploads/live actions.
STOP after one guarded source publication/checkpoint, no full CI wait or second stage.

Publication checkpoint: exact source `1bfc5a35ecad56605d385c02ff9cf85f39aa2532`, parent
`5690e559fbaaab452cf74f5b8c8cd0ce841568ad`, tree `bb1491182af4996c6f3bbf586cc3a41886c266e7`
verified after fetch equals reviewed local freeze `466155f10fd115598985726f2757aeaa902014c8`.
One expected-HEAD guarded non-force PR88 update; ONE automatic
[CI668/40106066218](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40106066218), exact source count1,
event pull_request/attempt1/in_progress/conclusion null on last observed metadata. New Windows parser/
Rollback8/Maintenance15/SelfCheck9/Backup8/Stage8/Payload9 and native regressions NOT_RUN pending same run
final logs. CI667 PASS covers only old source5690e55/mergefe740a5. Four public upload guards/workflows
unchanged; no manual CI/release/uploads. Docs-only local checkpoint intentionally not pushed/no second CI;
full SHA via git log -1/PR88 body. Next same CI668 final exact logs/merge/suites; then ONE bounded durable
journal/admission-before-write slice; interrupted recovery/retention/auxiliary transaction remain open.
CI662 UNKNOWN/diagnostics retained; BLOCKED_TRANSFER no delivery retries/install/live actions. STOP here.

## История этапа: F23 installed runtime self-check and truthful outcome prerequisite

PR88 OPEN/DRAFT source `eb4afad106794b192f265ba9d90c4d52584455d9`, checkpoint
`31e0bc49adbf6081e678e7fc52a886bff5afd66f`, clean/idle/root-only active writer independently verified.
Full CI666 Windows15429-line/Ubuntu final logs independently read; tested merge
`8dcf2ca3edb734ca2b6fc55d422f01671dc0e465`, both jobs completed SUCCESS. Backup8/Maintenance10/
Stage8/Payload9 all PASS, Desktop6388/policy214/Instance30/Maintenance126/Shutdown47/HomeVPN4816/
Portable91 and previous owned/repair/update suites PASS. Pack runner-only, uploads/prune SKIPPED.
CI662 timeout root UNKNOWN, non-reproduction not a fix; safe diagnostic retained unchanged.

Code `1b715ff3bd3a00eb88704849270482b4b3c10d4e`: one production self-check/proof prerequisite, NOT automatic rollback. After existing verified
payload/stage/backup and runtime copies, read-only proof verifies every installed runtime hash against the
accepted staged inventory, invokes existing MaintenanceOperation.RunOwnedSelfCheck(ProcessStartInfo,int)
with the existing8000ms bound, exact installed EXE/--self-check, shell disabled and both LOCALAPPDATA/
PROGO_SELF_CHECK_ROOT set to a new own stage child. Require exit0, actual isolated root confirmation and
unchanged runtime hashes after check; then shortcuts. Shared ownership held until stage cleanup/finally
Dispose. Install OK moved after successful cleanup/release; version captured before stage deletion, no
reading deleted VERSION. Launch remains after verification/release. No native helper/API changes.

Catch always rethrows original failure, retains verified backup, emits only booleans priorRuntimeVerified,
rollbackAttempted=False and backupRetained. Prior-runtime proof checks exact old bytes and expected absent
leaves, never trusts a restore callback. Refusal/untouched original differs from partial installation or
incorrect restoration; no automatic restoration is performed or claimed. Self-check/partial-copy failure
cannot report install success or launch. Entire rollback mutation and journal/retention remain explicit
next work, not silently treated as PASS. Existing source/prebuilt fail-closed, stage/backup helpers and
ownership adapters unchanged; original write operations preserved.

InstallerSelfCheck9: complete installed identity/isolated context; exit error; missing data-root proof;
runtime modified after check; partial copy refused before child; prior intact; bad restored bytes refused;
fresh prior absence; mocked owned timeout. Maintenance original10 assertions retained plus3 actual-main
fixture cases self-check-fail/partial-copy/bad-restore, checking no false success/late launch and honest
prior proof. Bad-restore is a synthetic incorrect restore inside mock; real automatic rollback NOT_RUN/not
implemented. All new native boundaries mocked, all files under new GUID temp fixtures; no real process,
install, IPC/service/ACL/security/network or owner data changes. Existing Backup8/Stage8/Payload9 unchanged.

PASS local static/review/signatures/unchanged old helper+workflow+SSH diagnostic parity, docs3, diff/JSON,
public-content/secrets. Windows parser/SelfCheck9/Maintenance13/Backup8/Stage8/Payload9 and regressions
NOT_RUN on new source pending ONE automatic CI; no inherited CI666 PASS. F23 PARTIAL, fixed58–80/22,+0.
Next SAME new exact CI final logs/merge/suites; then ONE bounded verified runtime rollback mutation slice
using retained snapshot/old-byte+absence proof, never false restored success. Durable recovery journal,
retention, updater/fallback, PGA04 OFF/default opt-in and package/target gates remain mandatory.
Privatecandidate BLOCKED_TRANSFER unchanged; no retry/delivery/install/release/uploads/live actions.
STOP after one guarded source publication/checkpoint, no full CI wait or second stage.

Publication checkpoint: exact source `5690e559fbaaab452cf74f5b8c8cd0ce841568ad`, parent
`eb4afad106794b192f265ba9d90c4d52584455d9`, tree `75100833c4b02c4763498d1d6f40fa01e7cc31b4`
verified after fetch equals reviewed local freeze `81457ff34fbb63966b935949c790b255cb79958b`.
One expected-HEAD guarded non-force PR88 source update; ONE automatic
[CI667/40102772473](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40102772473), exact source count1,
event pull_request/attempt1/in_progress/conclusion null on last observed metadata. New Windows parser/
SelfCheck9/Maintenance13/Backup8/Stage8/Payload9 and native regressions NOT_RUN pending same run final
logs. CI666 PASS covers only old sourceeb4afad/merge8dcf2ca. Four public upload guards/workflows unchanged,
release ineligible, no manual CI or uploads. Docs-only local checkpoint intentionally not pushed/no second
CI; full SHA via git log -1/PR88 body. Next same CI667 final exact logs/merge/suites; then ONE bounded
verified rollback mutation slice. Durable journal/recovery and retention remain mandatory. CI662 UNKNOWN/
diagnostics retained; BLOCKED_TRANSFER no delivery retries/install/live actions. STOP, no second stage.

## История этапа: F23-R02 verified runtime backup before overwrite

PR88 OPEN/DRAFT source `094620a2c5697749c882cea3a27dd16bfa4fc6a2`, checkpoint
`b3cd93582066dca49fcadcfe9b31702fd34247de`, clean/idle/root-only active writer independently verified.
Full CI665 Windows15419-line/Ubuntu final logs independently read; exact tested merge
`41ffac4eeb1aef919b584f923cc7f628869d494d`, both jobs completed SUCCESS. Stage8/Maintenance9/Payload9
all PASS, Desktop6388/policy214/Instance30/Maintenance126/Shutdown47/HomeVPN4816/Portable91,
repair/updater/previous owned transport suites PASS. Pack runner-only, uploads/prune SKIPPED.
CI662 timeout UNKNOWN; later non-reproduction does not prove a fix, safe diagnostic unchanged.

Code `d96be642d202b189095930fd9d9d7798b5408683`: one existing installer prerequisite. After verified stage and owned Stop, before first runtime
copy, snapshot only existing runtime inventory leaves which installer can replace (41 leaves; excludes
three distributable docs not overwritten and all personal files). Hash old bytes, copy to new own
backups/installer-GUID folder, write installer-snapshot.json with relative paths, old exists/hash and new
staged hash/version; round-trip persisted inventory and verify digest, every copied byte and unchanged
original/absent target admission. Repeat confirmation immediately before existing writes. Fresh install
without overwritten leaves explicitly proves absence and does not fabricate a backup. No source/stage
identity or target path change; no fallback on failed backup. Backup/manifest/IO errors refuse runtime writes.

Verified snapshot retained on success and later copy failure for next rollback slice; no speculative
rollback or deletion of confirmed recovery material. Only newly created unverified own GUID snapshot is
cleaned on preparation error. Backup namespace, root and link checks precede cleanup; same existing no-follow
leaf/empty-dir deletion body shared with stage cleanup after respective scope guards. Stage guard and
original cleanup algorithm preserved, no recursive-follow delete. Personal settings/vault/keys not read,
copied, changed or deleted; foreign backup dirs untouched. installer-snapshot.json is distinct from ordinary
automatic-backup manifest; retention integration/recovery journal remain open, not claimed complete.
This is local runtime byte snapshot/admission, NOT whole user-data backup, durable transaction recovery,
publisher authenticity/immutable release UID or hostile concurrent-writer hardening.

InstallerBackup8: complete prior bytes+persisted old/new identity; fresh absence; corrupt copy; copy error;
old runtime changes during snapshot; tampered manifest; newly appeared leaf; foreign cleanup refusal.
InstallerMaintenance original9 assertions retained plus tenth backup-fail integration: actual production
main refuses all runtime copies, leaves old EXE/user sentinel unchanged, releases owner. Tests use only
own new GUID temp roots, synthetic old runtime/user bytes and mocked maintenance/shortcuts/process
boundaries. No real install/IPC/mutex/service/ACL/network actions. Existing Stage8/Payload9 unchanged.

PASS local static/review/signatures, unchanged preflight/stage/Enter/Stop/body parity, docs3, JSON/diff,
public-content/secrets. Windows parser/Backup8/Maintenance10/Stage8/Payload9 and relevant regressions
NOT_RUN on new source until one automatic CI. No inherited CI665 PASS. F23 still PARTIAL, fixed scores
58–80/22,+0. Next SAME new exact CI final logs/merge/suites, then separate installed self-check/verified
rollback/journal+retention slice; updater fallback/PGA04 consent/package/target acceptance remain open.
Privatecandidate BLOCKED_TRANSFER unchanged; no retry/export/build-delivery/install/live/release/uploads.
Stop after one guarded source publication/checkpoint; no full CI wait or second stage.

Publication checkpoint: exact source `eb4afad106794b192f265ba9d90c4d52584455d9`, parent
`094620a2c5697749c882cea3a27dd16bfa4fc6a2`, tree `3cf97d89734fa893f7c9f2e9640e4f91148a5c9a`
verified after fetch equals reviewed local freeze `c59ccdac781b463a4cd85360d777d373467a9637`.
One expected-HEAD guarded non-force PR88 update; ONE automatic
[CI666/40099947611](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40099947611), exact source count1,
event pull_request/attempt1, queued/conclusion null at last observed run metadata. New Windows parser/
Backup8/Maintenance10/Stage8/Payload9 and native regressions NOT_RUN pending same run final logs.
CI665 PASS is limited to old source094620a/merge41ffac4. Four public upload guards/workflows unchanged,
release ineligible; source-only publication. Docs-only local checkpoint intentionally not pushed/no second
CI; full SHA via git log -1/PR88 body. Next same CI666 final exact logs/merge/suites, then ONE separate
installed self-check/verified rollback prerequisite. Journal/retention/fallback/PGA04 remain open. CI662
root UNKNOWN/diagnostic retained; BLOCKED_TRANSFER no delivery retry/install/live action. STOP here.

## История этапа: F23-R02 frozen verified staging before runtime writes

PR88 OPEN/DRAFT source `921b3f726ad9b4e833b475442ad8fb1a871f9a91`, checkpoint
`55f62b87f9e2a267b13466c04b7dc74a521399df`, clean/idle/root-only active writer verified.
Full CI664 Windows15409-line and Ubuntu final logs independently read; tested merge
`d3a0e78a849f16106d6e5d3b3fea219695d1e82a`, both jobs completed SUCCESS. InstallerMaintenance8 and
InstallPayload9 all PASS, Desktop6388/policy214/Instance30/Maintenance126/Shutdown47/HomeVPN4816/
Portable91/repair/updater transport/progress/package5 and prior owned suites PASS. Pack runner-only,
uploads/prune SKIPPED. CI662 timeout root cause UNKNOWN; later non-reproduction not a fix; diagnostic retained.

Code `142b0c5a25c0d1411371b4789f6b8a7c2f225658`: one F23 staging prerequisite only. Read-only preflight freezes SHA256 of declared runtime
inventory, preserving EXE/version metadata checks. After existing Enter/owned Stop, stage creates one GUID
child under OS temp; copies only declared files, verifies each copied hash against frozen preflight, repeats
completeness/EXE-version/hash checks. Installer source variables bind to verified stage before any installed
runtime/user-directory writes. Unlisted payload extras cannot enter staged home-vpn tree. Changed/missing/
corrupt/incomplete stage fails before installed writes; no build/download fallback. Original source-build
branch, target program/data paths and shared maintenance adapters unchanged. This proves local byte-copy
identity, NOT publisher authenticity/signature, hostile concurrent-writer hardening or whole release identity.

Cleanup validates direct temp-child GUID scope and refuses redirected paths; scans no links and deletes
only own leaves/empty directories (no recursive-follow deletion). New-stage failure cleans its own resources;
main finally cleans stage then releases maintenance in nested finally even if cleanup fails. No deletion of
source/foreign dirs or user settings/keys. Cleanup failure still propagates; durable cleanup recovery belongs
to later transaction work. No real service/ACL/IPC/network actions run locally or in new tests.

InstallerStage8: exact identity/all hashes; declared-only inventory; stage independent of later source edits
and successful own cleanup; preflight source-changed; source-missing; corrupt-copy; copy-error; foreign
cleanup refusal preserves unrelated sentinel. InstallerMaintenance original8 assertions retained plus ninth
stage-changed integration verifies actual production main refuses before runtime copy, releases owner and
preserves synthetic user bytes. Tests use new GUID temp dirs and mock maintenance/shortcuts/launch; no
real installation. Existing payload9 unchanged. Stage test wired once, no workflow/test timeout changes.

PASS local static/review/signatures/old-branch+helper+workflow+SSH diagnostic parity, docs3, JSON/diff,
public-content/secrets. Windows parser/InstallerStage8/Maintenance9/Payload9 and regressions NOT_RUN for
new source pending one automatic CI. No inherited CI664 PASS. F23 still PARTIAL, not complete installer.
Next same new CI final exact merge/logs/suites; then separately verified backup-before-overwrite/retention,
installed self-check/rollback/recovery, updater fallback fixed paths and PGA04 OFF/default explicit consent.
Privatecandidate BLOCKED_TRANSFER unchanged; no retry/delivery/install/build-only/private transfer/live
security action. Fixed readiness58–80/22,+0. STOP after this bounded publication/checkpoint, no second stage.

Publication checkpoint: exact source `094620a2c5697749c882cea3a27dd16bfa4fc6a2`, parent
`921b3f726ad9b4e833b475442ad8fb1a871f9a91`, tree `e612ed1e55bda39d42ece3d644f7c382394b446b`
verified after fetch, equals local reviewed freeze `9243801b4d620b96bd134818e05fc7775f73c05a`.
One expected-HEAD non-force PR88 source update; ONE automatic
[CI665/40096713452](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40096713452), exact source count1,
event pull_request/attempt1, queued/conclusion null on last observed run metadata. New Windows parser/
InstallerStage8/Maintenance9/Payload9/native regressions NOT_RUN pending same run final logs. CI664
applies only to old source921b3f7/merged3a0e78. Four public upload guards/workflows unchanged, release
ineligible. Docs-only checkpoint intentionally not pushed/no second CI; full SHA via git log -1/PR88 body.
Next same CI665 final logs/exact tested merge; then separate verified backup-before-overwrite prerequisite.
CI662 remains UNKNOWN, safe diagnostics retained; privatecandidate BLOCKED_TRANSFER, no retries/install/
release/uploads/live actions. STOP, no second stage or full CI wait.

## История этапа: F23-R02 installer shared maintenance ownership

PR88 OPEN/DRAFT source `44d76b2347cebd23c627003e231de86c261e97e1`, local checkpoint
`531af3bfabb5a9c3eabcda3b825faa29381014f4`, clean/idle/root-only active writer verified.
Full CI663 Windows15400-line log independently read; source44d76b2 tested merge
`86d483cd84da1e3220273a6102f017609e5eaf20`, both jobs completed SUCCESS. InstallPayload9 all PASS;
Desktop6388/policy214/Instance30/Maintenance126/Shutdown47/UpdaterPackage5/HomeVPN4816/Portable91,
repair/transport/progress/install-window/update-check and prior owned Admin/invitation regressions PASS.
Python exit0 stdout/stderr RanToCompletion, body/importBootstrap/encodings/os/site/cleanup=True,
deadlineExceeded=False. CI662 timeout NOT reproduced, underlying cause UNKNOWN, NOT a runtime fix;
incident and safe startup diagnostics retained unchanged. Pack runner-only SUCCESS, uploads/prune SKIPPED.
No owner target or delivered installer acceptance.

Code `bf06449e403ae01380fdc93c01c8a1065e8b1148`: one existing production installer path only. After explicit local prebuilt payload preflight
(or unchanged source build), Enter uses existing MaintenanceOperation.Enter(), then existing
StopApplication(string) verifies owned shutdown/proxy cleanup. No user writes/copies/shortcut actions
before admission; owner retained through all existing copy/shortcut/status statements and disposed in
finally on success/failure. Launch remains after owner release. No new kernel/security policy API or
changes to existing maintenance implementation. No new service/elevation/ACL/network/credential logic.
Payload resolver/default-source branch and all original write operations preserved; no build/download
fallback for invalid explicit payload. This is ownership only, NOT staging/backup/atomic install/self-check/
rollback/recovery acceptance. Partial file-copy failure remains next existing F23 work, not hidden success.

InstallerMaintenanceTests8 executes actual main installer AST statements after the existing preflight
boundary, scoped GUID temp runtime/data dirs; maintenance Enter/Stop/Dispose, shortcuts and launch mocked.
Cases complete/busy/stop-fail/copy-fail/shortcut-fail/launch/launch-fail/retry-stop require exact ownership,
refusal-before-copy, release on errors, new retry owner, release-before-launch, unchanged synthetic user
bytes and success only after admitted copies/shortcuts. No real installation, IPC, mutex/ACL, service,
shortcuts, process launch or profile changes. Fixture path status output suppressed; only case names emitted.
One test invocation appended to existing Test-ProGo; new tests/native NOT_RUN locally (no Windows/pwsh).
PASS local review/static/call-signature/original-body+resolver+workflow parity, docs3, JSON/diff,
public-content/secrets. CI663 old PASS does not cover new owner slice.

PGA04 remains TODO/OFF by default, requires future explicit install-time consent before any protected
Automatic/Start/elevation. Full F23 still PARTIAL. Next SAME new CI exact final logs/InstallerMaintenance8,
InstallPayload9 and maintenance/repair/update regressions; then ONE separately scoped verified staging/
backup-before-write prerequisite, self-check/rollback remain later. Privatecandidate BLOCKED_TRANSFER
unchanged; no retry/delivery/build-only export/install/live/security action. Fixed scores58–80/22,+0.
Stop after one guarded source publication, one automatic CI and local checkpoint; do not wait final CI.

Publication checkpoint: source `921b3f726ad9b4e833b475442ad8fb1a871f9a91`, parent
`44d76b2347cebd23c627003e231de86c261e97e1`, tree `756d89ebf2f6210b6ee1ab4f44d68bad27db0eeb`
verified after fetch equals reviewed freeze `54fe903deb7d1aab86fe05f94d4f48209d71a072`.
One expected-HEAD guarded non-force PR88 update; ONE automatic
[CI664/40093766505](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40093766505), exact-source count1,
event pull_request/attempt1/in_progress/conclusion null at checkpoint. New Windows parser/native
InstallerMaintenance8/InstallPayload9/regressions final NOT_RUN; CI663 not inherited. Workflows unchanged,
public upload guards4 retained; no release/upload/manual CI triggered. Local docs-only checkpoint not
pushed (no second CI); full SHA via git log -1/PR88 body. Next same CI664 final logs/exact tested merge,
then one separately scoped verified staging/backup prerequisite. CI662 root cause UNKNOWN/diagnostics
retained. Privatecandidate BLOCKED_TRANSFER; no retry/install/live action. STOP after this slice.

## История этапа: CI662 owned exit0 startup — evidence-only test diagnostics

HEAD PR88 source `db3adc280d500b97487a7991f25c1206248eb5b6`, checkpoint
`7dc91de4f2c83240869757e664f596631ec231c8`, clean/idle/root only active agent independently verified.
Full final Windows2782-line log CI662/40089851914/job120860130674 independently read; tested merge
`06ae505a9e49082e74e8d09d50b361d28799b15a` and build identity0.2.2+same merge confirmed.
Both jobs completed: Windows FAILURE Repository hygiene/build; Ubuntu SUCCESS (metadata independently checked).

FAIL at scripts/Test-ProGo.ps1:615 exit0-regression → Invoke-IsolatedSshFixtureTool → Fail line8:
owned Python -c import sys/print body marker/sys.exit(0), UseShellExecute=false, redirected stdin/stdout/stderr;
stdin explicitly closed before STARTED. startMs110/pythonClass hostedtoolcache. At deadline elapsed5137,
exited=False, stdout/stderr WaitingForActivation, CPU0ms, RAM5984256bytes, probeErrorType=none. Original
5000ms exceeded; owned child killed/joined/read streams/disposed, timeout remains FAIL. No body marker in
final log. This proves no successful completion within budget; does NOT prove loader/site/runner/code root
cause or installer resolver defect. No infrastructure diagnosis or runtime fix guessed.

Actual CI662 PASS: production build and Desktop harness compilation; Repair preflight4/null interop1/
runtime copy1/cleanup1/rollback1/owned self-check1/durable recovery1+refusal9; vault read165/daily134/open112/
restart237; SOCKS recovery51; bounded log43/helper9; Rescue26/helper8; home firewall21; shortcuts47;
diagnostic32/helper2; backup retention29/updater parity/integrity82; installed transport74/adapter3;
updater progress134/install window145/update check72. These old isolated suites apply only to exact CI662 merge.
NOT_RUN: InstallPayload9 ALL0/9 (invocation after Shutdown at script end never reached); exit17/stdin-EOF/
expected-timeout/fixture dependencies/contracts; Desktop/native policy/real SSH suites; Instance/Maintenance/
Shutdown/updater-package; subsequent HomeVPN/Portable/relay suites, final public-release check and pack.
Do not inherit CI661 or call whole Windows CI PASS.

Code `ecee68e50b814561cd9f0bda039c94d1922edee7`: ONLY test-only scripts/Test-ProGo.ps1 instrumentation. Original -c argv/body, normal site,
stdin EOF ownership, concurrent reads, 5000ms/negative200ms/cleanup5000ms/read3000ms budgets and all exit
asserts retained. Only exit0 child gets PYTHONVERBOSE=1 in ProcessStartInfo environment; no global env or
-S/-I/-E changes. Trace may affect timing; diagnostic success alone does not establish a fix. Safe summary
shows bodyEntered, importBootstrap, encodingsReady, osReady, siteReady, pthVisits, startupBanner,
cleanupEntered, tracebackPresent, stdout/stderr lengths, input closure, argv shape, owned lifecycle and
post-cleanup exit/task state. Static logical callsite only, not a claimed native stack. No raw trace/path/
argv/environment emitted or persisted for this child. Trace stored only in its existing in-memory readers;
owned output files contain fixed safe summary. Two synthetic phase/redaction checks precede launch.

PASS local: isolated Linux Python same-body+child-only verbose phase extraction smoke (NOT Windows),
static original vector/budget/assert and production/resolver/InstallPayload9/workflow parity, diff, docs3,
JSON/public-content/secrets. PowerShell parser/two formatter checks/native startup/new InstallerPayload9
NOT_RUN locally. Single diagnostic CI justified to distinguish last observed Python startup phase from
no-output/no-body condition, not blind rerun. If failure does not recur, keep root cause unresolved.
Private candidate BLOCKED_TRANSFER unchanged; no transfer retry/build/install/live/security/network action.
Next ONLY same new exact-SHA CI final startup facts and actual suite boundary; no second feature/slice.
Fixed readiness implementation58–80/100, verified22/100,+0. Publication/checkpoint details below.

Publication/checkpoint: source `44d76b2347cebd23c627003e231de86c261e97e1`, parent
`db3adc280d500b97487a7991f25c1206248eb5b6`, tree `7ae59dc018b6d89337d76ded99df734eb8fd3a0a`
verified after fetch; equals local reviewed freeze `88d0e3f9d94b10cb31a957e7ff657c84bfd19d64`.
One expected-HEAD non-force PR88 source update; ONE automatic
[diagnostic CI663/40091168500](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40091168500), source count1,
event pull_request/attempt1/in_progress/conclusion null at checkpoint. Final new Windows parser/startup
facts/InstallerPayload9/old native suites NOT_RUN pending same run logs. No CI result inherited and no
runtime fix claimed. Privatecandidate BLOCKED_TRANSFER. Local docs-only checkpoint intentionally not
pushed/no second CI, full SHA via git log -1/PR88 body. Next ONLY final same CI663 startup facts and
actual suite boundary; if transient PASS, CI662 root cause remains unresolved. Stop, no second stage.

## История этапа: F23-R01 — explicit prebuilt payload preflight, PGA04 prerequisite

Baseline PR88 source `2b068cf909e761c27b9594bec91d78b582fce68d`, local checkpoint
`6e96a7ab8fc4f87e2c8933e05d7c5ee8ed387e7f`, clean worktree/idle/no other active agent verified.
Code commit `2dc5310f4b26600f30e2727bc69fb84bcff700d3`. One bounded existing installer path only: explicit `-PayloadDirectory`
resolves a complete local prebuilt runtime before touching the fixed user install/data directory.
No compilation/download fallback for an explicit invalid payload. Original no-parameter source-build
entry point retained. Read-only resolver checks all runtime/helper/server leaves, semver and EXE metadata
name/version. Script copies come from the same resolved payload, not a mixed bootstrap directory.
This is completeness/version preflight, NOT cryptographic authenticity/verified installer/TOCTOU protection
or full F23 PASS. No new runtime helper dependency; repair journal/owned paths unchanged.

PGA04 remains TODO: separate install-time choice OFF by default; only fresh explicit consent may request
elevation for Automatic+Start. No option, elevation, service discovery/mutation/ACL/owner/security action
implemented in this slice. Update/uninstall/shared service preservation unchanged. Dependencies F23-R01
standalone prebuilt entry → F23-R02 maintenance/staging/verified backup+self-check/rollback → F23-R06
same version/commit updater and fallback with fixed paths/launch → F23-R07 target acceptance remain open.

Nine `InstallPayloadTests.ps1` cases: valid local payload; actual installer prefix skips build; incomplete
explicit payload refuses without fallback; invalid version; EXE/version mismatch; invalid EXE metadata;
missing server resource; absent explicit payload no fallback; exact payload-byte preservation. Test runs
real installer prefix only before `$InstallDir` assignment, isolated GUID temp resources/finally cleanup,
never actual install/launch/service/security. Wired once into existing Test-ProGo; no workflow changes.
PASS local static/call-signature/resource-source review, old runtime/workflow canonical blob parity,
public-content and docs3/Python tests, diff/JSON/secrets. Windows parser/compiler/InstallerPayload9 and
relevant native regressions NOT_RUN pending one automatic new exact-SHA CI; CI661 PASS not inherited.

Private candidate remains BLOCKED_TRANSFER: supported Windows Library download failed including one
bounded retry (owner evidence). No transfer retry/network/proxy/credential workaround. Source/exact-object
exports retained; no build/install executed. Do not publish personal transfer IDs in public source.
Weights unchanged, implementation58–80/100, verified22/100,+0. Next SAME new CI exact logs, InstallerPayload9
and relevant installer/repair/update regressions. Then one separately scoped F23 maintenance/verified-staging
prerequisite; PGA04 protected action executor/consent UI not silently added. STOP after source publication
and checkpoint; do not wait full CI or start another slice.

Publication verified: exact source `db3adc280d500b97487a7991f25c1206248eb5b6`, parent
`2b068cf909e761c27b9594bec91d78b582fce68d`, tree `e2ad18032af355ec3c300dfc7308a3d4afd2c79d`
equals reviewed local freeze `48a7504c25472a15b6e7fe04ab25f3304df507d4`. One expected-HEAD non-force
PR88 source update; one automatic [CI662/40089851914](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40089851914),
exact source count1/event pull_request/attempt1, in_progress/conclusion null at checkpoint. No final
compilation/InstallerPayload9/regression result yet; all new Windows tests NOT_RUN. CI661 covers only old
source2b068cf/merge77904df. Public repository upload guards4 unchanged; no release path eligible. Source
publication only, no transfer retry/binary delivery/install/service action. Local post-publication checkpoint
not pushed (no second CI); obtain full checkpoint SHA with git log -1. Next check SAME run final logs,
exact tested merge, InstallerPayload9 + existing installer/repair/update/native regressions. Stop here.

## История этапа: CI661 final evidence matrix + shortest private candidate plan

PR88 OPEN/DRAFT source `2b068cf909e761c27b9594bec91d78b582fce68d`, checkpoint `a89f0b0d628c6a0939ec0d65a6e563ec22111fb1` сверены;
66 worktrees clean/locks0/root sole active agent. Full final CI661 logs independently read: tested merge
`77904dff703f65002d306bf5acc84a2f15df98ab`, both jobs SUCCESS. Recover-invite10 PASS; Desktop6388/policy214/Instance30/
HomeVPN4816/prior suites PASS. Four actual owned SSH direct/agent startup/recovery latency+CPU/RAM/
process-I/O records already collected and real loopback SSH→SOCKS5→HTTP passed. No repeated harness.

Canonical [criterion→existing evidence→exact gap matrix](CI661_F31_EVIDENCE_MATRIX.md) maps original
F31-R09/R10/R11 and all11 agent scenarios. R09/R10 current isolated dev criterion evidence PASS;
old missing Admin/invitation/performance notes superseded, retained as history. Whole requirements remain
PARTIAL (integration/package/owner target NOT_RUN). R11 additionally has existing PGA04 installer opt-in
code gate, tied to F23; not a reason to repeat the same SSH harness. SCM-start unit fixtures are not live
successful service mutation. Real isolated ssh-add/encrypted key/agent protocol are not owner-key acceptance.
Counter scope is owned SSH child lifetime, not physical disk or whole-app totals; no new threshold invented.

CI ZIP runner-only641099bytes observed SHA256 `a8a3ac56a7cadb711a3901aaa4c728cd5b4fb32456c0142b8481b12c8b9602b3`;
uploads/prune SKIPPED, artifacts API0. ZIP not available/delivered/installed. Exact plan uses VERSION0.2.2,
source `2b068cf909e761c27b9594bec91d78b582fce68d`, tested build identity0.2.2+77904dff703f65002d306bf5acc84a2f15df98ab;
next shortest step build-only pinned merge on available isolated Windows with approved private persistent
output, fresh actual ZIP hash/manifest/content/launch smoke. Do not reuse runner hash for a rebuild.
No accessible Windows/private output here; protected delivery dependency declared, no workflow changes.
Install/check/rollback protocol and personal/technical-production/full-commercial residual in matrix.
Existing source install script is not a standalone prebuilt installer; F23/PGA04 remains package-code work.

PASS local evidence review/criterion mapping/JSON/diff/public-content; docs-only, no code/test/workflow edits,
no source push/new CI/binary build/transfer/install/live/network/security action. Checkpoint local HEAD via
git log -1/PR88 body. STOP after this one evidence stage. Next1 bounded private build-only/package step
when isolated Windows/private output exists; no new functionality or duplicate CI. Fixed readiness58–80/22,+0.

## История этапа: final bounded recover-invite owned transport поверх CI660

PR88 OPEN/DRAFT exact source `ff2ccb0e2b74f9dc3eb86a981536bced6cd2c194`, checkpoint `66d6fd4089fed302d49858f99f6af3951408e913` сверены;
65 prior worktrees clean/locks0/root sole active agent. Isolated worktree preserves all checkpoints.
Полные Windows15255/Ubuntu578 final logs [CI660](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40082601924)
самостоятельно прочитаны; exact tested merge `f72193fb2d755edbfe8027457d5ea0154521246d`. Both jobs SUCCESS, native ConfigAuthority
fixture.dotted/effective field PASS; policy214/Desktop6388/Instance30/Maintenance126/Shutdown47/
UpdaterPackage5/HomeVPN4686/Portable91. Reissue10 incl stop-between/result, revoke10/invite8/initial8/
repeated12/uncertainty8x2/name9/consumption8x3/API8/snapshot12/dispatcher5/ownership8/cleanup4/repair8/
list10/recover10/fresh10/HTTPS11/freshHTTPS10/export11/chooser3 PASS. Python28 incl2SKIPPED; Ubuntu
operation16/QR13/provisioning PASS. Pack runner-only SUCCESS; uploads/prune SKIPPED. Target PC NOT_RUN.

Local code `f28e6a4ebc3a9e13c18807b2a3964966cb79628d`:2 runtime28add/0delete +1 test151add/0delete.
Only recover-invite null-payload default Admin route uses RecoverInviteAdminOwnedAsync, same actual
Friends owner/dispatcher slots as ManageInvitations. Invalid payload still reaches original validation.
Existing retained worker owns key/helper/Load/status/result/RetainResult/cleanup; core single family lease
unchanged/no outer reacquire/Stop self-await. Existing recovery dispatcher checks scope before every native
copy/status/result; explicit UI recovery Copy and WaitForAdmin with original timeout. No caller-supplied
request/source ID/name, no new request or invite/revoke. Original journal drives exact recovery.
Fresh invite/standalone revoke/Reissue/Recover UI/core/protocol and agreed ACL fixtures byte-exact.

Owned recover-invite10: default-refusal/complete/cancel-retry/stop-copy/dispose-copy/owner-dispose/
cancel-command/cleanup-lock/stop-after-status/stop-result. Actual ShowDialog/default Admin/native fixture
process+child/real helper/private DPAPI I/O. Accepted request/source/recovery ID/result retained. Bytes stay
exact when cancelled before result; validated RetainResult may rewrite DPAPI envelope with same IDs/value.
Copy Cancel→explicit Retry only status/result of same request; no invite/revoke ever. Stop blocks subsequent
transport; no cached-token fallback after native cancellation; owner loss suppresses late UI; Stop after
verified result preserves request/no handoff. Cleanup lock retains owner, false Stop→unlock exact delete-only
retry no replay. Protected owner/access bytes unchanged. Original15s modal/5s copy+join deadlines/all old tests
unchanged. PASS local review/static/signatures/parity/lexical(not compiler)/diff/JSON/secrets/public321source0release.
New Windows compiler/recover-invite10/full suites NOT_RUN locally; no Windows/.NET/pwsh. CI660 not inherited.
Publication PASS: source `2b068cf909e761c27b9594bec91d78b582fce68d` one expected-head guarded non-force existing PR88 update;
fetched exact660 parent/tree `665084a4fe9ba6dfc4d3e0dc07b70b1511fd47cf` equals reviewed freeze
`3867bd07494e3b4bfc73edf98c138dffddf68dbe`. Diff6files:2 runtime28add/0delete,
1 test151add/0delete +3docs. ONE automatic [CI661/40085290372](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40085290372),
exact-source count1/pull_request/attempt1,in_progress/conclusion null; Windows/Ubuntu in_progress metadata.
Merge ref `77904dff703f65002d306bf5acc84a2f15df98ab` is NOT final tested merge proof; new compilation/recover-invite10/
full relevant suites NOT_RUN. Public repo/guards4 rechecked/uploads OFF/final skips not observed/release
ineligible. Docs-only local checkpoint intentionally not pushed/full SHA via git log -1/PR88 body. STOP;
next SAME CI661 exact final logs/merge/recover-invite10/all prior suites; no full wait/rerun/second stage.

Concrete remaining gates to personal candidate: (1) exact final recover-invite10/full CI proof; (2) whole
Windows F31-R09/R10/R11 latency/CPU/RAM/I/O + slow disk/process/network cancellation/close recovery,
selected-key readiness/agent/password/host-key refusal and SSH→SOCKS5→HTTP; (3) pinned private checking
package version/SHA/SHA256 with install/check/rollback and separately approved private delivery; (4) separately
authorized owner PC install/launch/profile/data/key/agent/connection/Cancel/recovery/update/data-retention
acceptance on approved resources. Runner pack is not delivered or installed software.
Production additionally F23 standalone installer/updater+inapplicable-patch fallback/correct installer paths/
identity/launch, clean install/update/offline/cancel/hash/self-check/rollback; F19/F22 portability another user/
PC/approved VPS and disconnect/hostkey/idle/transfer; F27 signing/timestamp/Defender+Kaspersky; final integration/
merge/release/live acceptance remain separate approval gates. Full F30 commercial scope retained separately.
Last transport code binding is only a candidate, not whole F31 PASS or complete acceptance.
Реализация:58–80/100 · Проверенная готовность:22/100 · Изменение:+0 · Главный блокер: exact new recovery
CI proof then final dev/delivery/target acceptance; fixed weights and all agreed requirements preserved.

## История этапа: CI659 — authorized SshConfigAuthority fixture-only ACL fix

PR88 OPEN/DRAFT exact source `18bb98df3cf3c660c1ce7be675a87ac76f499ff8`, checkpoint `db95d3e68ba3d42a03fc270887ca6e7029f5fd33`
сверены;64 prior worktrees clean/locks0/root sole active agent. New isolated worktree retains all changes.
Full Windows [CI659](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40080724882) final log independently
read; exact tested merge `708237b20221f63fb65266eff02120ecd2adf48c`. Windows120833528779 FAILURE; Ubuntu120833528529 SUCCESS metadata.
ConfigMatchesNative→SshConfigAuthority fixture.dotted fails native ssh-G exit255; Bad permissions for
Authenticated Users on synthetic Include [fixture]/parts/00-first.conf. Safe stack Check→ConfigMatchesNative→
SshConfigAuthority→Main. Existing default inherited fixture directory rights caused native refusal;
no runtime defect proven. Reissue10/Home VPN NOT_RUN; old PASS not inherited. Subsequent relay/pack SKIPPED.

Owner explicitly authorized only NEW GUID SshConfigAuthority temporary root at creation on isolated
Windows CI with protected inheritable ACL test-user/SYSTEM/Administrators. Local code `bc17e7de62bbbdbca051e4075d91eb774acf7577`:
only DesktopSshConfigTests.cs73add/2delete +3docs. No SetOwner/SetAccessControl, no existing/ancestor/
system/.ssh/SSH-policy/network changes. New synthetic parts/config/identity files inherit allowed roles.
Exact direct work-child ssh-config-authority-GUID path, no preexisting resource; reject all reparse ancestors/
entries. Readonly tree/ACL verification before original oracle; original same ssh-G args/config contents/
asserts/coverage/7000ms/1500ms unchanged. Cleanup walks only owned subtree, rechecks path, uses nonrecursive
child deletes, refuses links/fails closed. Failed post-create validation uses same cleanup. Existing local
behavior no ACL changes. Auth-policy/other fixtures/post-Stop Refresh/Reissue/runtime byte-exact.

PASS local review/static/strict parity/manual Framework signatures/lexical(not compiler)/diff/JSON/secretsPII/
public321source0release. Windows compile/native config oracle/Reissue10/Home VPN/full suites NOT_RUN locally;
no Windows/.NET/pwsh. Other hypothetical native fixture risks outside scope; no speculative changes.
Publication PASS: source `ff2ccb0e2b74f9dc3eb86a981536bced6cd2c194` one expected-HEAD guarded non-force PR88 update;
fetched exact659 parent/tree `ec14de392615a8bdfbeceb0bd7e7fe3626601fba` equals reviewed freeze
`d7dafcf6fd38b8e336a5aae6a81cddce48edd600`. Diff one test file73add/2delete plus3docs.
ONE automatic [CI660/40082601924](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40082601924), exact-source count1/pull_request/attempt1,
in_progress/conclusion null. Merge ref `f72193fb2d755edbfe8027457d5ea0154521246d` is NOT final tested merge proof.
New Windows compilation/native config oracle/Reissue10/Home VPN/full suites NOT_RUN; job metadata only.
Public repo/guards4 rechecked/uploads OFF/final skips not observed/release ineligible unchanged.
Postpublication3-doc local checkpoint intentionally not pushed/full SHA via git log -1/PR88 body. STOP;
next SAME CI660 exact final logs/merge/native config oracle/Reissue10/Home VPN/all relevant regressions.
Реализация:58–80/100 · Проверенная готовность:22/100 · Изменение:+0 · Главный блокер: exact new
Windows native oracle/Reissue proof. Full delivery/personal/production gates retained separately.

## История этапа: Reissue/source-ID invite owned transport поверх CI658

PR88 OPEN/DRAFT/source `469c4fe332a9241cce3820aca7d3fc1d9c30806f`, checkpoint `12e045c15a7aae94b09baf0f733c8e0b1b6c30c3` сверены;
63 prior worktrees clean/locks0/root sole active agent. Isolated worktree сохраняет checkpoints/изменения.
Полные Windows14194/Ubuntu578 logs [CI658](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40077869891)
прочитаны самостоятельно: exact tested merge `da6ac3c5d9d338b5ff0cb6e2f6241bfec951812d`. Both jobs SUCCESS; native policy
Disabled/Excluded/AllowedByClient effective-field assertions PASS, policy214/Desktop5451/Instance30/
HomeVPN4562/Portable91. Revoke10 incl Stop stale gate+explicit Refresh/exact ID/no replay/sibling PASS.
Invite8/list10/initial8/repeated12/uncertainty8x2/name9/consumption8x3/API8/snapshot12/dispatcher5/
ownership8/cleanup4/repair8/recover10/fresh10/HTTPS11/freshHTTPS10/export11/chooser3 PASS. Python28
incl2SKIPPED; Ubuntu operation16/QR13/provisioning PASS. Pack runner-only SUCCESS; uploads/prune SKIPPED.
Это узкий dev PASS; owner PC/install/live acceptance NOT_RUN, historical failure evidence preserved.

Local code `8dfbe6c9f3dbf5ffc82ad8043fe61f54f1fc258f`:3 runtime54add/3delete +1 test161add/0delete.
Scope only selected Reissue→owned revoke dependency→source-ID invite. Existing owned revoke reused,
fresh invite/standalone binding/recover/core/ACL tests untouched. Separate ReissueAdmin keeps old fixtures
backward-compatible; production BindSelectedReissue freezes owner/source ID/StopWork. Source invite
wrapper validates exact24hex ID, owns core family lease/helper/key/request/result/cleanup on worker;
Copy/WaitForAdmin start on explicit Friends UI. Original native timeout unchanged. Post-copy Stop refuses
new SSH; changed scope between acknowledged revoke and invite refuses second mutation. Accepted result
after Stop keeps source-bound request/recovery ID and old-access revoked state, forbids late token handoff.
No new request protocol/automatic revoke or invite retry; existing orchestration/consumption exact baseline.

Owned Reissue10: default-refusal/complete/cancel-refresh/stop-copy/dispose-copy/owner-dispose/cancel-command/
cleanup-lock/stop-between/stop-result. Real ShowDialog/production binding/native fixture children/DPAPI,
exact source/name despite different draft/same-name sibling/one revoke+invite/no replay. Cancel preparation
requires explicit read-only Refresh and fresh selection (not automatic Reissue retry). Stop retains I/O,
owner disposal suppresses late UI; accepted native cancel retains recovery request; failed cleanup retains
exact owner then delete-only Retry after unlock; Stop between steps no invite; Stop after accepted result no
late token. Hold/join5s/modal15s unchanged; all original methods/tests/asserts/timeout coverage preserved.
PASS local review/static/manual signatures/lexical(not compiler)/strict parity/diff/JSON/secrets/public321source0release.
New Windows compilation/Reissue10/full suites NOT_RUN locally (no Windows/.NET/pwsh); CI658 not inherited.
Publication PASS: source `18bb98df3cf3c660c1ce7be675a87ac76f499ff8` one expected-head guarded non-force existing PR88 update;
fetched exact658 parent/tree `c481e0bf45cf8dc4c6949313c284f24061695185` equals reviewed freeze
`a21428357abf74188730a384bcbee1c63d4d513e`. Diff7files:3 runtime54add/3delete,
1 test161add/0delete +3docs. ONE automatic [CI659/40080724882](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40080724882),
exact-source count1/pull_request/attempt1,in_progress/conclusion null; Windows120833528779/Ubuntu120833528529
in_progress metadata. Merge ref `708237b20221f63fb65266eff02120ecd2adf48c` is NOT final tested merge proof;
new compilation/Reissue10/full relevant suites NOT_RUN. Public repo/guards4 rechecked/uploads OFF,
final skips not observed/release ineligible. Docs-only local checkpoint intentionally not pushed/full SHA
via git log -1/PR88 body. STOP; next SAME CI659 exact final logs/merge/tests, no full wait/rerun/second stage.

Remaining: verify this candidate, then ONE recover-invite owned transport path. Personal candidate also
needs whole Windows F31-R09/R10/R11/selected-key-agent/SSH→SOCKS5→HTTP/Cancel recovery/latency-resource
acceptance, pinned private package version/SHA/SHA256 and approved delivery/owner PC install-check-rollback.
Production additionally installer/updater/fallback/portability/signing/AV/final integration/release/live approvals;
full commercial scope retained separately. No delivery/live/security/release actions authorized here.
Реализация:58–80/100 · Проверенная готовность:22/100 · Изменение:+0 · Главный блокер: new exact
Reissue10 Windows proof, then recover-invite and final delivery/acceptance gates. Fixed weights unchanged.

## История этапа: CI657 — authorized new temporary auth-policy fixture ACL

Owner явно согласовал только new temporary auth-policy fixture ACL в isolated Windows CI.
PR88 OPEN/DRAFT exact source `127f266ddfabf0050eda4831693f14b382dc4f9b`, checkpoint `017578ec55a78ea72bd8a17f57cd8f7f313a4557`
сверены;62 prior worktrees clean/locks0/root sole active agent. Новый isolated worktree сохраняет checkpoint.
Full Windows CI657 exact tested merge `48817acd0ef74879d16e7042b4eb0e79313fd32d` повторно прочитан:
policy.disabled exit255, includeNamed=True, badOwnerPermissions=True, inherited otherWrite1 confirmed.
Runtime cause не доказан; exact rejected ACE UNKNOWN. Revoke10/Home VPN в CI657 NOT_RUN.

Local code `a81108a4ae461e9dbe49cdb949b426248aecbfee`: только DesktopSshAuthPolicyTests.cs65add/2delete +3docs.
Fresh direct work child auth-policy-GUID path; refuses existing resource and every reparse ancestor/entry.
Windows CI gate (GITHUB_ACTIONS/RUNNER_OS/Win32NT) creates new root with protected DirectorySecurity
at creation; inheritable FullControl only current test-user/SYSTEM/Administrators. No SetOwner or
SetAccessControl, no existing/ancestor/system/.ssh ACL changes. Local execution keeps original ACL behavior.
Files created by original fixture inherit this allowlist; readonly checks root protected and file roles.
Original fake-identity exclusive lock/native ssh-G Include precedence/auth policy assertions/7000ms timeout
unchanged. Original policy tests and post-Stop/Refresh/exact-ID/no replay fixture byte-exact.
Cleanup traverses only exact own GUID subtree, checks boundaries and reparse points before each action,
uses nonrecursive deletes and fails closed on links; never scans siblings/changes parent. Validation failure
immediately after creation invokes same own cleanup; normal/failed test finally cleans own resources.
No raw paths/SIDs/ACL contents/configs/credentials emitted; existing safe diagnostics retained.

PASS local review/static/strict original method+assert+timeout parity/Framework signatures/lexical (not compiler),
diff/JSON/secretsPII/public321source0release. Windows compilation/ACL native/policy/revoke10/full suites
NOT_RUN locally; no Windows/.NET/pwsh. No coverage reduction, assertion bypass or timeout increase.
Publication PASS: source `469c4fe332a9241cce3820aca7d3fc1d9c30806f` one expected-head guarded non-force PR88 update;
fetched exact parent/tree `57a6888e52b5d33562b8fb60024fa2cdc3554748` matches reviewed freeze
`27b00216d1711ad9e89967753f7c7d9b75368b02`. Source diff one test file65add/2delete plus3docs only.
ONE automatic [CI658/40077869891](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40077869891), exact-source count1/pull_request/attempt1,
in_progress/conclusion null; Windows120825053332/Ubuntu120825053207 in_progress metadata.
Merge ref `da6ac3c5d9d338b5ff0cb6e2f6241bfec951812d` is NOT final tested merge proof. New compilation/ACL/native policy/
revoke10/full relevant suites NOT_RUN until actual final logs. Public repo rechecked/guards4 unchanged,
uploads OFF/final skips not observed; release/prune ineligible. Docs-only local checkpoint intentionally
not pushed/full SHA via git log -1/PR88 body. STOP; next SAME CI658 exact final logs/merge/all required tests.
Реализация:58–80/100 · Проверенная готовность:22/100 · Изменение:+0 · Главный блокер: exact new
Windows fixture/policy/revoke10 proof; delivery/target PC/production gates remain unchanged.

## История этапа: CI657 — read-only OpenSSH fixture permissions diagnosis, BLOCKED

PR88 OPEN/DRAFT exact source `127f266ddfabf0050eda4831693f14b382dc4f9b`, checkpoint
`84e2c6165ea57ee3e9dd63a5bebe921c6b47c7ef` сверены;62 worktrees clean/locks0, root sole active agent,
active git/build0. Полный Windows [CI657](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40071488117)
log самостоятельно прочитан: checkout/build exact tested merge `48817acd0ef74879d16e7042b4eb0e79313fd32d`.
Windows120805683344 terminal FAILURE; Ubuntu120805683270 terminal SUCCESS metadata only.
Это НЕ повтор post-Stop revoke assertion: DesktopTests.AuthPolicyNative policy.disabled упал раньше.
Exit255/permissionsHint=True; roles configNamed=False/includeNamed=True/identityNamed=False,
badOwnerPermissions=True/badPermissionsBanner=False/permissionDenied=False.
Safe stack Check→AuthPolicyNative→SshAuthPolicyWorkflow→Main.
File ACL facts config/include/identity: read=True,ownerCurrent=False,ownerSystem=False,ownerAdmins=True,
protected=False,rules4/inherited4/denied0/otherAllow2/otherWrite1. Directory rules8/inherited8/otherAllow4/otherWrite2.
Raw paths/SIDs/config contents/credentials не сохраняются и не публикуются.

Confirmed: actual OpenSSH refuses fixture Include permissions before effective policy resolution.
DesktopSshAuthPolicyTests.cs:17 creates default inherited directory; :163–168 WriteAllText creates files
without explicit ACL. :170 intentionally locks fake identity; :174 runs native ssh -G -F against fixture,
not a live host. Existing failure-role/ACL diagnostics already locate the failing Include.
Official upstream contract: https://github.com/PowerShell/Win32-OpenSSH/wiki/Security-protection-of-various-files-in-win32-openssh
requires protected client configuration. OwnerAdmins alone is not proof of bad ownership; exact rejected
ACE/principal/installed-version branch UNKNOWN. No runtime bug proven; historical640 cause not declared fixed.
Changing runtime/parser or bypassing native security would not repair this fixture contract.

FAIL: AuthPolicyNative/SSH policy workflow/Desktop run. NOT_RUN: revised revoke10/post-Stop Refresh,
all Home VPN suites and later relay/pack; старый PASS не переносится. No new code/tests/ACL/owner/SSH
policy/system settings changes. All previous exact ID/no replay/Refresh/accepted outcome guarantees retained.
BLOCKED: separate owner authorization for test-only protected ACL on newly created auth-policy temporary
folder and its config/include/fake identity files in isolated Windows CI, current test-user/SYSTEM/Administrators
only, locked-identity test preserved. No existing file owner/ancestor/system/.ssh ACL changes, global repair,
security-policy changes or production fix. After approval: one bounded fixture isolation change and exact CI.
Evidence is sufficient for approval request; no speculative instrumentation or blind rerun justified.
No source push/new CI. Local3-doc checkpoint only; published source unchanged, full checkpoint SHA git log -1.
PASS local read-only evidence review/JSON/diff/public-content; Windows reproduction here NOT_RUN.
Реализация:58–80/100 · Проверенная готовность:22/100 · Изменение:+0 · Главный блокер: narrow temporary
Windows fixture ACL authorization. STOP; no second stage, release/uploads/install/live VPS/network/access actions.

## История этапа: CI656 — post-Stop test-contract correction

PR88 OPEN/DRAFT source `3d3136be67f68f74eea2d27d161f8ec0b56ec5b9`, checkpoint `a5750bdfd922bfd41f43dc700c820493cdbc9c21` сверены;
61 worktrees clean/locks0 перед новым isolated worktree; root sole active agent, build/git operations0.
Full Windows [CI656](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40068891553) log самостоятельно прочитан;
exact tested merge `7652b5f42b78aab12b6c18cbb676596e51df170a`. CS1501 устранён/harness compiled. First8 revoke scenarios PASS.
stop-accepted: accepted result, exact selected ID/untouched sibling/one revoke/completed Stop PASS;
затем FAIL «confirmed revoke restores actions with exact selected ID». Safe stack ModalWizardCase il155 →
OwnedRevoke il1225 → Run il637. Separate needsRefresh/Enabled values UNKNOWN (not logged).
Пройденные list10/initial8/repeated12/uncertainty8x2/Recover name9/consumption8x3/invite8 PASS;
accepted-request и последующие Home VPN suites NOT_RUN. Whole CI/Home VPN не PASS.

Причина доказана контрактом: StopAsync заменяет StopWork; action хранит старый operationStop.
RefreshPendingAsync.checkScope отвергает смену reference, catch устанавливает pendingAdmin/pendingUnreadable;
UpdateActions блокирует изменения. Нельзя включать revoke на основании stale pending snapshot даже после
подтверждённого revoke. Accepted outcome остаётся сохранённым; Stop не откатывает его. Старый fixture
ошибочно объединял обычный success и success после Stop. Runtime/API unchanged.
Local code `d02f0adc7a3bce0e50fac6af04819e21cee7d5eb`: только OwnedRevoke28add/2delete. Complete/cancel-retry original assert сохранён.
Для stop-accepted проверяются stale scope и все mutation gates/no replay; затем явный Refresh читает
existing list callback в новом scope и доказывает исходную гарантию restoration: pending clear, actions
restored, exact selected revoked ID/sibling preserved, one revoke. Общие original outcome/ID/cleanup/bytes
assertions unchanged; modal15s/hold+join5s unchanged; новых функций/transport нет.
PASS local review/static/manual signatures/diff/JSON/secretPII/public321source0release. Windows compilation/
revised revoke10/full suites NOT_RUN locally: no Windows/.NET/pwsh. Score fixed58–80/22 +0.
Publication PASS: source `127f266ddfabf0050eda4831693f14b382dc4f9b`, existing PR88 one expected-head guarded non-force update;
fetched exact parent and tree `ed60f51a4834657f8bda44e9e9bdf4e5a4925915` match reviewed freeze
`611b2500d105f1e62cdc609f638fd98f544b6c64`. Diff only1 test file28add/2delete +3 state documents.
ONE automatic [CI657/40071488117](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40071488117), exact-source count1/pull_request/attempt1,
in_progress/conclusion null. Windows/Ubuntu jobs in_progress at metadata checkpoint.
Merge ref `48817acd0ef74879d16e7042b4eb0e79313fd32d` is NOT final tested merge proof; new compilation/revoke10/full suites NOT_RUN.
Public guards4 unchanged/uploads OFF/final skips not yet observed; no release or prohibited live actions.
Docs-only local checkpoint intentionally not pushed; full SHA via git log -1/PR88 body. STOP;
next SAME CI657 final exact-source/tested-merge/revised revoke10/prior+remaining suites, no rerun/second stage.
Delivery/personal/production gates сохранены ниже; installation/live resources/security/release blocked.

## История этапа: CI655 — минимальная fixture signature correction

PR88 OPEN/DRAFT exact source `3e9971f696e2225472119c8f899170ee0ef8ec79`, checkpoint `74fbc11e69f298a8c237dd5c28aba8f1d419fbf4`
сверены.60 worktrees clean/locks0 до нового isolated worktree; root sole active agent; git/build operations0.
Полный Windows log [CI655](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40052376202)
прочитан самостоятельно: exact checkout/build tested merge `4cf6b66911c91e6b66e9b86387578865054872f7`.
FAIL: `HomeVpnAdminRecoveryTests.cs(1532,51): CS1501: No overload for method 'Register' takes 2 arguments`.
Existing `Register(HomeVpnOwner owner, string name, string source)` source не optional; source=null
означает обычное invite без исходного ID. Fixture ранее забыл третий аргумент. Это test compilation defect,
не runtime/API bug. Desktop5298/SSH policy61/Instance30/Maintenance126/Shutdown47/UpdaterPackage5
PASS exact655; revoke10 и ВСЕ Home VPN suites NOT_RUN из-за несобранного harness. Relay/public-content/
pack/uploads/prune SKIPPED. Ubuntu job SUCCESS metadata; не выдаём metadata за проверку его строк лога.

Local code `cdfafeea073648c540bbb5254e5b2bafac5f1d2e`: ровно одна строка fixture получает explicit null source.
Runtime/API/все assertions/timeouts/coverage/порядок теста unchanged. Все13 Register calls fixture имеют
arity3; manual new revoke7/Admin8/WaitForOwner5/Bind2/modal3/ctor5 signatures проверены с declarations.
PASS static review/exact one-line parity/diff/JSON/public321source0release/added secretsPII checks.
Windows/.NET/pwsh отсутствуют: corrected compilation/revoke10/Home VPN NOT_RUN; старый PASS не переносится.
Publication PASS: source `3d3136be67f68f74eea2d27d161f8ec0b56ec5b9`, one fresh expected-head non-force PR88 update.
Fetched exact655 parent/full tree `ced06d2049266e3e5d839e1ff052a027c9c63086` equals reviewed freeze
`7b3ecf05dafe59c1ddab524f29dbd9a20857cff8`. Diff: one fixture line plus3 state documents;
all runtime/API/asserts/timeouts/coverage exact baseline. ONE automatic [CI656/40068891553](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40068891553),
exact-source count1/pull_request/attempt1, in_progress/conclusion null. Windows120797848377/Ubuntu120797848558
in_progress. Merge ref `7652b5f42b78aab12b6c18cbb676596e51df170a` is NOT final tested merge proof.
Corrected Windows compilation/revoke10/Home VPN suites NOT_RUN until actual final logs.
Public guards4 unchanged/uploads OFF/final skips not yet observed; release/prune ineligible.
Postpublication docs-only checkpoint not pushed to avoid second CI; full SHA via git log -1/PR88 body.
STOP; next SAME CI656 final logs/exact source+tested merge/harness compile/revoke10/relevant regressions.
Остаток transport/delivery/личной и production приёмки ниже сохранён; новую функцию здесь не начинаем.
Реализация:58–80/100 · Проверенная готовность:22/100 · Изменение:+0 · Главный блокер: exact corrected
Windows harness/revoke10 proof; no install/release/uploads/live VPS/network/ACL/credential actions.

## История этапа: standalone selected-ID revoke owned transport поверх CI654

Сверены PR88 OPEN/DRAFT exact source `b72d53e1475410736d2bdd338d13c75e6d9a8c81`, checkpoint `edf65d3458629fc5de5c25bbc562a4a50862e078`;
59 прежних worktrees clean/locks0, только новый work/progo-revoke-owned-on654-20261024 изменяется,
root sole active agent. Никакие изменения не удалены/откачены; действующих git/build операций нет.
Полные final Windows13850/Ubuntu578 logs [CI654](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40048609754)
прочитаны самостоятельно: checkout/build exact tested merge `2170a3caef25e2722171911b6233bee1c06201e2`.
Invite8 и все прежние suites PASS: consumption Reissue8/Create8/Recover8/API8, Recover name9,
uncertainty Create8/Reissue8, API6/UI8/UI12, snapshot12/dispatcher5/ownership8/cleanup4/repair8/list10/
recover10/fresh10/HTTPS recovery11/fresh HTTPS10/export11/chooser3. Home VPN4371/Desktop5298/
SSH policy61/Instance30/Maintenance126/Shutdown47/Portable91. Оба jobs SUCCESS; pack только runner,
uploads/prune SKIPPED. Python28 incl2SKIPPED; Ubuntu operation16/QR13/provisioning PASS.
Исторические причины640/642 не объявлены исправленными; диагностика сохранена.

Local code `4022f598ca795554f5f97b1d09af884addf8866e`:3 runtime36add/1delete +1 test178add/1delete.
Выбран ТОЛЬКО отдельный revoke button Friends→BindSelectedRevoke→RevokeAdminOwnedAsync.
Generic Admin/Reissue internal revoke/SourceID invite/recover-invite и core unchanged; dependencies ready.
Frozen exact ID+owner, retained worker owns helper/key/output/cleanup, explicit Friends owner/dispatcher
for Copy/WaitForOwner; same old 5-minute timeout. Post-copy Stop refuses SSH before admission;
accepted command drains actual outcome; late progress suppressed. No new receipt/protocol/family lease:
existing revoke does not issue/consume invitation request. Uncertain accepted outcome keeps existing
Refresh gate; no auto replay. Scope does not close whole F31-R09/R10/R11.

Owned revoke10: default-refusal/complete/cancel-retry/stop-copy/dispose-copy/owner-dispose/cancel-command/
cleanup-lock/stop-accepted/accepted-request. Real ShowDialog through production binding, exact selected ID,
same-name sibling untouched, single confirmation/admission, heartbeat, native owned process+child,
private output/actual helper staging. Pre-SSH Cancel→explicit Retry submits one revoke; Stop/Dispose
retain held work; no late disposed status; accepted-but-cancelled SSH remains unconfirmed/gated;
accepted response after Stop still marks exact access revoked. Locked cleanup retains owner, false Stop→
unlock delete-only Retry no replay. Existing accepted invitation request/result/recovery ID bytes preserved.
Fixture replaces only copy/SSH executable; no live action. Old tests/asserts/timeouts exact; 5s hold/join,
15s modal deadline and finally cleanup. New native/compiler checks NOT_RUN locally (no Windows/.NET/pwsh).
PASS local review/manual signatures/lexical (not compiler)/strict old-method+test parity/diff/JSON/
public321source0release/added secretsPII0. SUCCESS654 applies only its exact baseline.
Publication PASS: one fresh-head non-force existing PR88 source `3e9971f696e2225472119c8f899170ee0ef8ec79`,
parent exact654 and fetched tree `3276e7cbbfcae1efdb68ce3c4dada381207d7215` equal reviewed freeze
`e33640d1e14c9ca3eaa83eaa43f86071ce331b14`. Public diff7files629add/71delete:3 runtime36add/1delete,
1 test178add/1delete,3docs415add/69delete. Local Python invitation metadata2 PASS.
ONE automatic [CI655/40052376202](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40052376202): exact-source count1,
pull_request attempt1,in_progress/conclusion null. Windows120747624392 Repository/build in_progress;
Ubuntu120747624578 in_progress. Merge ref `4cf6b66911c91e6b66e9b86387578865054872f7` is NOT final tested merge proof;
new Windows compilation/revoke10/all old native suites NOT_RUN until actual final logs.
Public uploads OFF/final skips not yet observed; no release/binary upload/install/live resource action.
Postpublication3-doc local checkpoint intentionally NOT pushed; full SHA via git log -1/PR88 body.
STOP without full wait/second stage; next SAME CI655 final exact source/merge/revoke10/all old suites.

До личного кандидата: exact revoke proof; затем2 bounded transport пути Reissue orchestration+SourceID
invite и recover-invite; финальная Windows F31-R09/R10/R11 проверка UI/selected-key/agent/SSH→SOCKS5→HTTP,
Cancel/Retry/recovery/latency+CPU/RAM/I/O; pinned private package version/SHA/SHA256 и install/check/rollback;
отдельно разрешённая передача/установка/приёмка на ПК владельца. Pack на runner не равен доставке.
До technical production дополнительно F23 installer/updater/fallback с сохранением путей/данных и
clean-update/offline/cancel/hash/self-check/rollback, другой user/PC/approved VPS, F27 signing/timestamp/
Defender+Kaspersky и разрешённая final integration/release/live acceptance. Safety approvals остаются
BLOCKED по соответствующим gates. Полный F30 commercial scope сохранён отдельно и не удалён.
Реализация:58–80/100 · Проверенная готовность:22/100 · Изменение:+0 · Главный блокер: new exact Windows
revoke proof, затем2 transport и delivery/owner acceptance gates; веса не менялись.

## История этапа: fresh Create invite owned transport поверх CI653

PR88 OPEN/DRAFT/source `aad854a0a6a6570f5776e38c5e5bae005928972a` и checkpoint
`2c7ae7e96f11fd9051b5e93bd2f4191bb8ac9b16` сверены;58 worktrees clean/locks0/root sole active agent.
Новая isolated work/progo-invite-owned-on653-20261024 сохраняет прежнюю работу. Полные final
[CI653](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40045109182) Windows13757/Ubuntu578 logs
прочитаны самостоятельно: оба checkout/build identity exact merge `e8f2536015c9bc36883058125f9bc9a6c21911d7`.
Corrected native exact replacement/actions/actual receipt deletion/one revoke+invite PASS. Also PASS
Reissue consumption8/Create consumption8/Recover consumption8/API8/Recover name9/Reissue8/Create8/
API6/UI8/UI12/snapshot12/dispatcher5/ownership8/cleanup4/repair8/list10/recover10/fresh10/HTTPS11/
freshHTTPS10/export11/chooser3. Desktop5298/Instance30/Maintenance126/Shutdown47/HomeVPN4278/
Portable91/SSH policy61. Python reported28 incl2SKIPPED; Ubuntu operation16/QR13/provisioning PASS.
Pack runner-only PASS; uploads/prune SKIPPED; owner PC NOT_RUN. CI652 failure remains history,
not a runtime gating relaxation. Historical640/642 causes not proven fixed/diagnostics retained.

Выбран первый допустимый транспорт: fresh Create→invite, identifier null. Dependencies уже есть;
local code `3a37deaf77772688de44f05a50c4ae0951c4ee84`:3 runtime+1 test168add/0delete.
Default wizard Admin маршрутизирует ТОЛЬКО invite без SourceID в InviteAdminOwnedAsync, сохраняя
реальный Friends owner/dispatcher из ManageInvitations. Reissue invite с ID, revoke/recover-invite unchanged.
Wrapper freezes owner/label, reserves existing retained worker. Core owns single family lease/private
key/helper/DPAPI registration/result/cleanup on worker; no outer reacquire/Stop self-await/protocol change.
Existing repair-named generic fresh scope/transport guard reused unchanged. Native Copy/WaitForAdmin
start crosses explicit UI only; accepted copy drains then Stop gate refuses registration/SSH. Late progress
suppressed; Create original Stop guard prevents late token handoff while accepted request stays recoverable.

Owned invite8: default-refusal,complete,cancel-retry,stop-copy,dispose-copy,owner-dispose,cancel-command,
cleanup-lock. Actual ShowDialog/default Create→default wizard Admin routing with same Friends owner slots
as production. Default binding refuses missing fixture key before transport; other cases replace only copy/
SSH executable at existing core boundary. Actual helper staging/DPAPI/native process+child/compound
request-ID command/receipt/result and cleanup are real. Hold copy5s/modal15s/final joins5s. Single admission/
lease/heartbeat, explicit pre-request Cancel Retry issues once, Stop/Dispose drain before registration,
owner Dispose no late status, accepted native cancellation preserves ID with no repeat, failed cleanup retains
exact owner/receipt; false Stop while locked→unlock cleanup Retry with no SSH replay. Old tests/methods/
asserts/timeouts preserved; no live VPS/credential/network/system action. Consumption and core unchanged.

PASS local review/signatures/strict scope/parity/lexical delimiters/diff/JSON/public321source0release/
added secretsPII0. Lexical не AST/compiler; Windows/.NET Framework/pwsh unavailable:
new compilation/owned invite8/full relevant regressions NOT_RUN. SUCCESS653 не переносится.
Publication PASS: source `b72d53e1475410736d2bdd338d13c75e6d9a8c81`, existing PR88
integration/approved-plan-31-20261011, fresh expected-head non-force update. Fetched exact653 parent,
full tree `29693ea9ad0d892768f186e34ddabf973ba19adb` равен reviewed freeze
`1428a1a33dfef016d631399bb59265054decedf2`. Public diff7files771add/58delete:
3 runtime32add/0delete +1 existing test136add/0delete +3docs603add/58delete.
Merge ref `2170a3caef25e2722171911b6233bee1c06201e2` пока НЕ final tested merge (NOT_RUN).
ONE automatic [CI654/40048609754](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40048609754):
exact-source count1/pull_request/attempt1, in_progress/conclusion null.
Windows120735950255/Ubuntu120735950098 in_progress metadata; new compiler/invite8/full suites NOT_RUN.
Public uploads OFF/release+prune ineligible/workflows unchanged; final skips not yet checked.
Postpublication3-doc local checkpoint intentionally NOT pushed, чтобы не запускать второй CI;
полный SHA в git log -1/PR88 body. STOP без полного wait/новой функции/второго этапа.
Далее ТОТ ЖЕ CI654 exact final logs/merge/owned invite8/all prior suites.

Остаток до личного кандидата: подтвердить этот invite candidate; затем3 bounded transport пути
revoke/Reissue orchestration+SourceID invite/recover-invite; итоговая Windows F31-R09/R10/R11 dev приёмка
и latency/CPU/RAM/I/O report; pinned version/SHA/SHA256 проверочный пакет/install-check-rollback steps
и разрешённая приватная передача; отдельно разрешённая owner PC SSH→SOCKS5→HTTP/cancel/recovery/key/
agent/profile/data-preservation acceptance. NOT_RUN/BLOCKED gates остаются в JSON delivery_gates.
Technical production дополнительно: F23 standalone installer/updater+fallback fixed paths/identity,
clean install/update/offline/hash/Cancel/self-check/rollback, portability другой user/PC/approved VPS,
F27 signing/timestamp/Defender+Kaspersky, разрешённая финальная integration/merge/release/live acceptance.
Полный F30 email/account/billing/commercial scope сохранён отдельно, не удалён и не объявлен готовым.
Нужные внешние safety approvals/private transfer/signing/target install/live resources/merge-release остаются
явными блокерами; ничего из этого не выполняется здесь. Реализация:58–80/100 · Проверенная готовность:
22/100 · Изменение:+0; главный блокер: exact Windows fresh-invite proof, затем3 transport+delivery gates.

## История этапа: CI652 native success fixture correction — test-only

PR88 OPEN/DRAFT/source `099ba265daefabce5758d25c88e15798ca71bd50` и checkpoint
`6440c454be8f84af99b222b3146d4e4535e900bf` сверены;57 worktrees clean/locks0/root sole active agent.
Новая isolated work/progo-ci652-fixture-fix-20261024 сохраняет всю работу без reset/rollback.
Полные final Windows/Ubuntu [CI652](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40041549329) logs самостоятельно прочитаны;
checkout/build identity exact tested merge `91eeafc6584992bced555f6142561171e2db1ecf`.
Windows FAILURE на tests/HomeInvitationReissueTests.cs исходные102–103:
«native success selects replacement and restores actions». Safe stack Check IL12 → Native IL841 →
Run IL1500 → HomeVpnWizardTests.Main IL1748; wrapper Test-HomeVpnRelay.ps1:19.
Предыдущий PASS shown token/old row revoked/sibling not revoked/3 rows подтверждён.
Лог НЕ разделяет selected-ID и reissue.Enabled: отдельные значения UNKNOWN, не выдумываем их.
Compilation/Repository/Desktop5298/SSH policy61/Instance30/Maintenance126/Shutdown47/UpdaterPackage5
PASS exact652. New Reissue consumption8/Create8/Recover8/API8 и последующие HomeVPN suites NOT_RUN
(0 markers), не наследуют651 PASS. Ubuntu operation16/QR13/provisioning PASS. Pack/uploads/prune SKIPPED.

Доказанное несовпадение fixture: callback-only success возвращает токен без request/result и ждёт
только shown!=null. Старый sync ConfirmConsumed при missing request возвращался; async API возвращает
false, а production UI правильно оставляет gating. Показ токена также не означает completion async action.
Нет доказанного runtime/infrastructure defect; production не меняется и gating не ослабляется.

Local code `1d25c59a4ae1abef0a41383523a1778d33c240b0`: один fixture first Native success,61add/27delete (включая indentation).
Сохраняет fixture access, создаёт production Register/RetainResult с исходным ID/именем/exact token,
проверяет durable RequestId/SourceInviteId/Name/Result до inviteGate.SetResult. Сохраняет прежний
shown/old revoke/sibling/3rows assert. Затем bounded PumpUntil actual ConsumptionWork+ActionWork
completion и GetResult до ИСХОДНОГО joint assert exact replacement ID+restored actions.
Дополнительно actual receipt absence/success task/exact one revoke+invite/unchanged neighbour ID.
Finally cancels own gates/disposes own window/joins actual tasks and restores prior fixture file bytes.
Все исходные assertions unchanged ignoring indentation; later scenarios/helper bodies/PumpUntil5s exact.
Никакого ожидания по asserted UI value, расширения timeout, удаления coverage или blind rerun.

PASS local review/structural preservation/signatures/lexical delimiters/diff/JSON/public321source0release/
added secretsPII0. Lexical не AST/compiler; dotnet/csc/mcs/pwsh/Windows отсутствуют:
new compilation/corrected native/Reissue consumption8/full relevant regression NOT_RUN.
Publication PASS: source `aad854a0a6a6570f5776e38c5e5bae005928972a`, existing PR88
integration/approved-plan-31-20261011, fresh expected-head non-force update. Fetched exact652 parent,
full tree `812ca99f700ab309d04d2b19a9b02451dcc23451` равен reviewed freeze
`44e834ffaf20fe61561184f90c5e0bd83887b5ea`. Public diff4files421add/87delete:
1 test61add/27delete including reindent +3docs360add/60delete; runtime/other tests exact652.
Merge ref `e8f2536015c9bc36883058125f9bc9a6c21911d7` пока НЕ final tested merge (NOT_RUN).
ONE automatic [CI653/40045109182](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40045109182):
exact-source count1/pull_request/attempt1, in_progress/conclusion null; никакого old-SHA rerun.
Windows120724941594/Ubuntu120724941809 in_progress metadata; new compiler/native NOT_RUN.
Public uploads OFF/release+prune ineligible/workflows unchanged; final skips not yet observed.
Postpublication3-doc local checkpoint intentionally NOT pushed, чтобы не запускать второй CI;
полный SHA в git log -1/PR88 body. STOP без полного wait/новой функции/второго этапа.
Далее ТОТ ЖЕ CI653 exact final logs/merge/corrected native success/actual consumption/restored actions/
Reissue consumption8/Create8/Recover8/API8/all prior suites; CI652 остаётся FAILURE.
Личный/technical-production/commercial остаток сохранён в delivery_gates_after_reissue_consumption;
ни один gate не закрывается fixture-кодом или старым PASS. Реализация:58–80/100 · Проверенная
готовность:22/100 · Изменение:+0; главный блокер: новое точное Windows proof corrected fixture/suites.

## История этапа: Reissue Complete owned consumption поверх CI651

PR88 OPEN/DRAFT/source `23a924705fcc7ab11de123163c94f92de10c2a45` и checkpoint
`d0871a0c4392547f18b855df33fb7477a26a9141` сверены. До изменений56 worktrees clean/locks0,
root sole active agent; isolated work/progo-reissue-consume-on651-20261024 сохраняет прежнюю работу.
Полные [CI651](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40011476143) Windows13580/Ubuntu578
строк прочитаны самостоятельно. Оба checkout/Windows build identity: exact tested merge
`153be0f9e5ea74aa6cdae54177ebc1670221de83`. JSON ci651_final — actual per-case evidence.
PASS Create consumption8/Recover consumption8/API8/Recover name9/Reissue8/Create8/API6/UI8/UI12/
snapshot12/dispatcher5/ownership8/cleanup4/repair8/list10/recover10/fresh10/HTTPS recovery11/
freshHTTPS10/export11/chooser3. Desktop5298/Instance30/Maintenance126/Shutdown47/HomeVPN4101/
Portable91/SSH policy61; Python reported28 incl2SKIPPED; Ubuntu operation16/QR13/provisioning PASS.
Pack runner-only PASS; uploads/prune SKIPPED. Owner PC NOT_RUN; historic640/642 causes not proven fixed.

ТОЛЬКО Reissue Complete: local code `4c3a84b6801f7f8a2a7efccd1aff091d518ad385`,1 runtime+1 test164add/1delete.
Existing ConsumeRecoveredAsync/ConfirmConsumedAsync переиспользуется без изменений: original Stop scope,
explicit owner/CTS/volatile admission/retained worker/API8 family lease. Guards after handoff/settlement;
неподтверждённое завершение или accepted delete после отмены остаётся gated. No blind repeat.
Create/Recover/shared helper/reissue core+uncertainty/API8/service/dispatcher/transport/scripts/workflows/
VERSION byte-identical. Ни новых функций, ни реальных внешних действий.

Reissue consumption8: complete,close-retry,dispose,stop-retry,service-dispose,locked,changed-request,
changed-result. Actual ShowDialog/default Reissue/core: ровно один frozen-ID revoke acknowledgement,
затем один invite same label+SourceInviteId; old access остаётся revoked, same-name sibling не изменён.
Real protected accepted request/result; consume barrier5s/modal15s/joins5s. Cancel Close→reopen Retry,
Stop/lock→Refresh→Recover сохраняют recovery ID без нового revoke/invite. Newer receipt/private bytes/
draft preserved, no late status; accepted delete stays successful after Stop. Callback transport не live VPS
proof. Все старые bodies/asserts/timeouts/coverage сохранены, cleanup только fixture-owned resources.

PASS local review/signatures/C#5 await scopes/structural isolation/lexical delimiters/diff/JSON/
public321source0release/added secretsPII0. Lexical не AST/compiler; Windows/.NET Framework/pwsh нет:
new compilation/Reissue consumption8/full regressions NOT_RUN. SUCCESS651 не переносится.
Publication PASS: source `099ba265daefabce5758d25c88e15798ca71bd50`, existing PR88
integration/approved-plan-31-20261011, fresh expected-head non-force update. Fetched exact651 parent,
full tree `e48f1249c54114829ca9d7ab2e47f23c8a9350ed` равен reviewed freeze
`75b29a39fd54b53dd30e3e8efe211d020f74c3d3`. Public diff5files826add/42delete:
1 runtime7add/1delete +1 existing test157add/0delete +3docs662add/41delete.
Merge ref `91eeafc6584992bced555f6142561171e2db1ecf` пока НЕ final tested merge (NOT_RUN).
ONE automatic [CI652/40041549329](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40041549329):
exact-source count1/pull_request/attempt1, in_progress/conclusion null.
Windows120713543137/Ubuntu120713543452 in_progress metadata; new suites NOT_RUN.
Public uploads OFF/release+prune ineligible/workflows unchanged; final skips not yet checked.
Postpublication3-doc local checkpoint intentionally NOT pushed, чтобы не запускать второй CI;
полный SHA в git log -1/PR88 body. STOP без полного wait/второго этапа.
Далее ТОТ ЖЕ CI652 final logs/exact source+tested merge/Reissue consumption8/Create8/Recover8/API8/all suites.

### Проверяемый остаток: личный кандидат, technical production, полный коммерческий объём

Это delivery gates, не удаление требований31/55 и не изменение весов. Машиночитаемые критерии:
JSON delivery_gates_after_reissue_consumption. Исторические scoped PASS не заменяют финальную приёмку.

- PC-01 NOT_RUN: exact new CI Reissue consumption8 и все старые suites/merge-source identity.
- PC-02 TODO: четыре production transport пути invite/revoke/reissue/recover-invite — original scope/ID,
  no duplicate lease/replay, Cancel/Retry/Stop/Dispose/cleanup. F31-R09/R10 целиком пока не закрыты.
- PC-03 NOT_RUN: итоговая Windows dev приёмка F31-R09/R10/R11 с selected-key/agent/host-key,
  slow disk/network/process и latency/CPU/RAM/I/O report на финальном SHA.
- PC-04 BLOCKED: version/SHA/SHA256 checking package + install/check/rollback steps; нужен разрешённый
  приватный способ передачи. Runner ZIP не означает полученный пакет; public uploads запрещены.
- PC-05 NOT_RUN: отдельно разрешённая проверка на ПК владельца: запуск/UI/profile/key+agent,
  SSH→SOCKS5→HTTP, отмена/recovery/close и сохранность данных/предыдущей версии. Live ресурс/установка
  в этом этапе не разрешены. Этим закрывается личная приёмка, не весь коммерческий продукт.

Technical production дополнительно требует PR-01 полной технической сверки approved criteria;
PR-02/F23-R01–R06 готового standalone installer/updater/fallback installer с сохранением program/data
paths, immutable identity и actual clean install/update/offline/Cancel/hash/self-check/rollback tests;
PR-03/F19/F22/F31 другого Windows пользователя/ПК/разрешённого VPS, сохранности профилей/ключей/
settings, network drop/host-key/idle/large transfers и phone для включённого маршрута;
PR-04/F27 точного signed payload/timestamp и отдельных Defender/Kaspersky результатов;
PR-05 финальной разрешённой integration/merge/release/публикации/live acceptance на том же SHA.
Signing credentials/security settings/доступы, установка, live ресурсы, merge/release/private transfer
требуют соответствующего отдельного решения владельца: BLOCKED, обходов или новых credentials нет.

F30 почта/кабинет/биллинг и остальные коммерческие обязательства остаются PARTIAL в полном плане:
sandbox/webhook/duplicates/refunds/paid→activation retry/session revocation. Они не объявлены готовыми
и не удалены ради личного кандидата; это отдельный полный коммерческий объём.
Реализация:58–80/100 · Проверенная готовность:22/100 · Изменение:+0;
главный блокер: exact Windows Reissue proof, затем четыре transport paths и delivery gates выше.

## История этапа: Create handoff owned consumption поверх CI650

PR88 OPEN/DRAFT/source `5b4ed7e910dc0fdc7a6ac0ce203b0ceeda858fec` и checkpoint
`052309cabb2ff229930ae605011644c16a608621` сверены. До изменений55 worktrees clean/locks0,
root sole active agent; isolated work/progo-create-consume-on650-20261023 сохраняет прежнюю работу.
Полные [CI650](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40006198834) Windows13422/Ubuntu578
строк самостоятельно прочитаны. Оба checkout/Windows build identity: exact tested merge
`32e6df175b2e295e591794686f23af56fb3f17eb`. JSON ci650_final — actual per-case evidence.
PASS Recover consumption8/API8/Recover name9/Reissue8/Create8/API6/UI8/UI12/snapshot12/dispatcher5/
ownership8/cleanup4/repair8/list10/recover10/fresh10/HTTPS recovery11/freshHTTPS10/export11/chooser3.
Desktop5298/Instance30/Maintenance126/Shutdown47/HomeVPN3943/Portable91/SSH policy61;
Python reported28 incl2SKIPPED; Ubuntu operation16/QR13/provisioning PASS. Pack runner-only PASS,
uploads/prune SKIPPED. Owner PC NOT_RUN; historic640/642 causes not proven fixed/diagnostics retained.

ТОЛЬКО CreateAsync handoff: local code `85cb6f347b7321dc73d4ed0ab5eacc63d4b4727b`,1 runtime+1 test153add/1delete.
Existing ConsumeRecoveredAsync (имя существующего helper сохранено) переиспользуется без изменений,
включая original Stop scope/explicit owner/CTS/volatile admission/retained ConsumptionWork/API8 lease.
Guard после handoff и settlement запрещает late UI/I/O. Неподтверждённое завершение остаётся gated;
принятое delete после отмены не выдаётся за новый запрос. Existing uncertainty/post-action read unchanged.
Shared helper/Recover/Reissue/API8/service/dispatcher/transport/scripts/workflows/VERSION byte-identical.
Не меняем product flow/новых функций нет; Repeat проходит через уже готовый Recover, никогда invite.

Create consumption8: complete,close-retry,dispose,stop-retry,service-dispose,locked,changed-request,
changed-result. Actual ShowDialog/default Create; один fixture invite создаёт real protected accepted
request/result; frozen name/returned ID/token verified. Held existing consume boundary5s/modal15s/
joins5s; heartbeat/reentry/cleanup/private bytes/newer receipt preservation, no late status. Cancel Close→
reopen Retry и Stop/lock→Refresh→Recover используют тот же recovery ID при issues==1. После accepted
actual delete последующий Stop сохраняет успешный Task/absence без повторов. Transport callback-only,
не live VPS proof. Все прежние test bodies/API8/Recover8 и их asserts/timeouts/coverage exact.

PASS local review/signatures/C#5 await scopes/structural isolation/lexical delimiters/diff/JSON/
public321source0release/added secretsPII0. Lexical не AST/compiler; Windows/.NET Framework/pwsh нет:
new compilation/Create consumption8/full relevant regression NOT_RUN. SUCCESS650 не переносится.
Publication PASS: source `23a924705fcc7ab11de123163c94f92de10c2a45`, existing PR88
integration/approved-plan-31-20261011, fresh expected-head non-force update. Fetched exact650 parent,
full tree `38a535e3be126fca5a153a45e3389ff26aae3947` равен reviewed freeze
`57eb7b7b420d4941655ec9da21b1d64353d30919`. Public diff5files689add/47delete:
1 runtime7add/1delete +1 existing test146add/0delete +3docs536add/46delete.
Merge ref `153be0f9e5ea74aa6cdae54177ebc1670221de83` пока НЕ final tested merge (NOT_RUN).
ONE automatic [CI651/40011476143](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40011476143):
exact-source count1/pull_request/attempt1, run queued/conclusion null at latest metadata snapshot.
Jobs snapshot: Windows120612656982 in_progress; Ubuntu120612656538 queued. Новые suites NOT_RUN.
Public uploads OFF/release+prune ineligible/workflows unchanged; final skips not yet checked.
Postpublication3-doc local checkpoint intentionally NOT pushed, чтобы не запускать второй CI;
полный SHA в git log -1/PR88 body. STOP без полного wait/второго этапа.
Далее ТОТ ЖЕ CI651 full final logs/exact source+tested merge/Create consumption8/Recover8/API8/all prior suites.
Остаток: Reissue Complete consumption; invite/revoke/reissue/recover-invite owned transport (отдельно).
FullR09/owner PC acceptance open. Реализация:58–80/100 · Проверенная готовность:22/100 · Изменение:+0;
главный блокер: Windows proof нового Create UI slice, затем Reissue/transport/owner acceptance.

## История этапа: Recover handoff owned consumption поверх CI649

PR88 OPEN/DRAFT/source `928257553a47fc001b2cdaac25817f95d3b6c95c` и checkpoint
`c7abd1580cb9c75dadedda60d60541ce10fe146c` сверены. До изменений54 worktrees clean/locks0,
root sole active agent; isolated work/progo-recover-consume-on649-20261023 сохраняет все прежние ветки.
Полные [CI649](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40000989219) Windows13267/Ubuntu578
строк самостоятельно прочитаны. Оба checkout/Windows build identity: exact tested merge
`80995cb75743dfff071bdb40330e30eee843aa41`. JSON ci649_final содержит actual per-case evidence.
PASS ConsumptionWorker8 (complete,cancel-read,cancel-consume,refused,locked,changed-request,
changed-result,missing), Recover name9/Reissue8/Create8/API6/UI8/UI12/snapshot12/dispatcher5/ownership8/
cleanup4/repair8/list10/recover10/fresh10/HTTPS recovery11/freshHTTPS10/export11/chooser3.
Desktop5298/Instance30/Maintenance126/Shutdown47/HomeVPN3794/Portable91/SSH policy61;
Python reported28 incl2SKIPPED; Ubuntu operation16/QR13/provisioning PASS. Pack runner-only PASS,
uploads/prune SKIPPED. Owner PC NOT_RUN; historic640/642 causes not proven fixed/diagnostics retained.

Выбран один UI handoff — RecoverAsync, существующий read-only recovery без новой выдачи.
Local code `0ec361c0b0bd0e83560f979806716b6c0a0f4f27`:1 runtime+1 existing test171add/8delete.
Только этот ConfirmConsumed заменён на await existing API8 в existing RunAdminOwnedAsync;
ConsumptionWork удерживается до actual settlement. Исходный Stop scope/volatile admission и CTS
проверяют допуск, worker не читает UI controls. Close/Escape отменяет до delete и ждёт фактический
результат, не закрывает ShowDialog неожиданно; Dispose отменяет без late UI. Stop удерживает worker,
тот не ждёт Stop. Принятое physical delete остаётся успешным; late close/Stop не публикует success
и не воссоздаёт request. Остаток требует explicit Refresh/recovery, никогда blind issue/consume replay.
Create/Reissue API call sites, API8/core/service/dispatcher/transport/scripts/workflows/VERSION exact.
Проверка owner после ShowToken предотвращает запуск I/O из уже закрытого handoff callback.

Восемь новых actual modal modes: complete,close-retry,dispose,stop-retry,service-dispose,locked,
changed-request,changed-result. Real protected accepted request/result и original recovery ID; actual
API consume barrier5s/modal15s/joins5s. Lease/ownership, heartbeat/reentry, exact private bytes/newer
record retention, one accepted deletion, no late status. Close→reopen Retry и Stop/lock→Refresh→Recover
используют только прежний recovery без issuing/revoke; original API8 и все старые tests byte-identical.
Transport callback-only fixture не выдаётся за реальный SSH/VPS тест. Только fixture-owned resources
закрываются, actual Tasks/lease/cleanup join. Исходные asserts/timeouts/coverage не ослаблены.

PASS local review/signatures/C#5 await scopes/structural isolation/lexical delimiters/diff/JSON/
public321source0release/added secretsPII0. Lexical не AST/compiler; Windows/.NET Framework/pwsh нет:
new compilation/Recover consumption8/full relevant regression NOT_RUN. SUCCESS649 не наследуется.
Publication PASS: source `5b4ed7e910dc0fdc7a6ac0ce203b0ceeda858fec`, existing PR88
integration/approved-plan-31-20261011, fresh expected-head non-force update. Fetched exact649 parent,
full tree `80550ef5c5c3cc7663bc840bfa308391abfe011b` равен reviewed freeze
`2d758528a3d4059c61888366d525b09faa60beac`. Public diff5files717add/60delete:
1 runtime48add/8delete +1 existing test123add/0delete +3docs546add/52delete.
Merge ref `32e6df175b2e295e591794686f23af56fb3f17eb` пока НЕ final tested merge (NOT_RUN).
ONE automatic [CI650/40006198834](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40006198834):
exact-source count1/pull_request/attempt1, in_progress/conclusion null.
Windows120594618104/Ubuntu120594618663 in_progress (metadata, новые suites NOT_RUN).
Public uploads OFF/release+prune ineligible/workflows unchanged; final skips not yet checked.
Postpublication3-doc local checkpoint intentionally NOT pushed, чтобы не запускать второй CI;
полный SHA в git log -1/PR88 body. STOP без полного wait/второго этапа.
Далее ТОТ ЖЕ CI650 final logs/exact source+tested merge/Recover consumption8/API8/all prior suites.
Остаток измерим: Create consumption; Reissue Complete consumption; invite/revoke/reissue/recover-invite
owned transport (отдельные bounded slices). FullR09/owner PC acceptance open.
Реализация:58–80/100 · Проверенная готовность:22/100 · Изменение:+0;
главный блокер: Windows proof нового UI slice, затем два handoff binding/transport/owner acceptance.

## История этапа: saved ConfirmConsumedAsync API8 integration поверх CI648

Владелец явно разрешил prerequisite API+8 из e8fbd2a отдельным этапом, без UI binding/transport.
PR88 OPEN/DRAFT/source `8e58d70c785227837f2a9063fe4a0688500481b6` и local checkpoint
`db90130869750904985d4c8d12199776287c0571` сверены. До изменений53 worktrees clean/locks0,
root sole active agent; новая isolated work/progo-consumption-api-on648-20261023 сохраняет старые ветки.
[CI648](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39995378692) повторно metadata completed SUCCESS/source exact;
финальные logs/per-case proof уже прочитаны на checkpoint db90130: tested merge
`f332d1cac3d549feac2a436c9cc4e2cb8bb32523`, JSON ci648_final. Не выдаём повторный metadata check
за новое чтение logs. Recover name9/Reissue8/Create8/API6/UI8/UI12/старые suites PASS только648;
HomeVPN3746/Desktop5298/Instance30, pack runner-only/uploads+prune SKIPPED; owner PC NOT_RUN.

Из сохранённого `e8fbd2ae6dad919b95c2b92ed34bda3005aa52d6` перенесены только
ConfirmConsumedAsync28 lines и ConsumptionWorker73 lines+Run call1. Local code `1ed593dfaa53df912c14a4546fabc07f77fb6791`,
2files102add/0delete. Обе добавленные method bodies byte-identical saved commit; all old methods exact.
Никаких родителей saved branch, старой версии reader или всего файла поверх новых исправлений.
Dependencies уже есть: family lease/validated Pending10 fields/Json/File.Delete/imports и fixture helpers;
existing Test-HomeVpnRelay compiler list включает этот source/test. Других зависимостей не требуется.
UI/HomeVpnService/transport/scripts/workflows/VERSION неизменны. Никакого production binding сейчас.

Worker держит lease через actual read/recheck/delete, exact token/full10-field equality, cancel и
caller admission до удаления; принятое delete не превращается в cancelled задним числом. Missing false;
отказ/отмена/смена request/result/delete lock сохраняют receipt. Caller обязан удерживать Task и передать
thread-safe admission callback; UI Cancel/Retry/Stop/Dispose ownership подключается отдельным этапом.
Не объявляем существующий UI I/O закрытым только из-за наличия worker.

Ровно saved8 tests: complete,cancel-read,cancel-consume,refused,locked,changed-request,changed-result,missing.
Actual ShowDialog/DPAPI+delete sharing/lease/admission/request bytes. Исходные heartbeat3s/barrier5s/
settlement5s/modal15s/final cleanup/asserts/coverage без изменений. Существующие regression bodies exact.
PASS local review/signatures/dependency+parity/lexical delimiters/diff/JSON/public321source0release/
added secretsPII0. Lexical не AST/compiler; dotnet/csc/mcs/pwsh/Windows недоступны: compiler/API8/native/
полные релевантные регрессии NOT_RUN на новом candidate. SUCCESS648 не переносится.
Publication PASS: source `928257553a47fc001b2cdaac25817f95d3b6c95c`, existing PR88
integration/approved-plan-31-20261011, fresh expected-head non-force update. Fetched parent exact648,
full tree `fabbbf7d87567a2f93ccf8373418369df0ecd256` равен reviewed freeze
`ca64712d86d9e3967065af4f6f27f1ef07696026`. Public diff5files837add/81delete:
1 runtime28add/0delete +1 existing test74add/0delete +3docs735add/81delete.
Merge ref `80995cb75743dfff071bdb40330e30eee843aa41` пока НЕ final tested merge (NOT_RUN).
ONE automatic [CI649/40000989219](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40000989219):
exact-source count1/pull_request/attempt1, run queued/conclusion null at latest run metadata check.
Jobs snapshot: Windows120576687161 queued; Ubuntu120576687526 in_progress. Это metadata,
new compiler/API8/full regression NOT_RUN, никакого наследования SUCCESS648.
Public uploads OFF/release+prune ineligible/workflows unchanged; final skips not yet observed.
Postpublication3-doc local checkpoint intentionally NOT pushed, чтобы не запускать второй CI;
полный SHA в git log -1/PR88 body. STOP без полного ожидания/второго этапа.
Далее ТОТ ЖЕ CI649 final logs/exact source+tested merge/ConsumptionWorker8/все prior suites.
После proof — отдельный один UI consumption handoff binding; transport remains separate.
Реализация:58–80/100 · Проверенная готовность:22/100 · Изменение:+0;
главный блокер: новый Windows compiler/API8 proof, затем UI binding/transport/owner acceptance.

## История этапа: consumption binding BLOCKED — API сохранён, но не интегрирован

PR88 OPEN/DRAFT/source `8e58d70c785227837f2a9063fe4a0688500481b6`, checkpoint
`fdcd976c8639162b17f1da7e8ac98b7e55747e5a` сверены;52 worktrees clean/locks0,
root sole active agent. Полные [CI648](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39995378692)
Windows13219/Ubuntu578 строк прочитаны самостоятельно. Оба checkout/Windows build identity:
exact tested merge `f332d1cac3d549feac2a436c9cc4e2cb8bb32523`. JSON ci648_final — per-case evidence.
PASS Recover name9/Reissue8/Create8/API6/UI8/UI12/snapshot12/dispatcher5/ownership8/cleanup4/
repair8/list10/recover10/fresh10/HTTPS recovery11/freshHTTPS10/export11/chooser3.
Desktop5298/Instance30/Maintenance126/Shutdown47/HomeVPN3746/Portable91/SSH policy61;
Python reported28 incl2SKIPPED; Ubuntu operation16/QR13/provisioning PASS. Pack PASS runner-only,
uploads/prune SKIPPED. Owner PC acceptance NOT_RUN. Исторические640/642 причины не объявлены fixed.

Доказанная зависимость: в exact source648 src/HomeVpnAdminRecovery.cs есть только синхронный
ConfirmConsumed(string token); ConfirmConsumedAsync ОТСУТСТВУЕТ. Существующий согласованный worker
и ConsumptionWorker8 сохранены в local `e8fbd2ae6dad919b95c2b92ed34bda3005aa52d6`:
2 files102add/0delete (API28 + tests74). Коммит не является ancestor source648.
Предыдущая фраза current plan «existing API8» не различала saved implementation и integration;
она НЕ подтверждала, что API интегрирован или прошёл Windows. Теперь это явно исправлено в реестре.
Все3 UI call sites (Reissue Complete/CreateAsync/RecoverAsync) по-прежнему synchronous.
Saved API compiler/native8 на текущем интегрированном source NOT_RUN, не PASS от CI648.

Запрошенный binding к уже существующему в PR API невозможен без отдельного prerequisite:
сначала включить сохранённый worker+8 tests. Не расширяем binding-only этап молча, не восстанавливаем
API/cherry-pick/runtime по предположению. Никаких source/test правок, push или нового CI; это не rerun.
Изолированная ветка work/progo-consumption-dependency-on648-20261023 хранит только3 документа состояния.
PASS evidence/symbol/ancestry review/JSON/diff/public-content/added secrets check; runtime new NOT_RUN.
Следующее необходимое решение владельца: явно включить saved e8fbd2a API+8 regressions как prerequisite
следующего ограниченного этапа, затем небольшой UI handoff binding с lifecycle tests. Transport excluded.
Реализация:58–80/100 · Проверенная готовность:22/100 · Изменение:+0;
главный блокер: consumption API8 сохранён локально, но отсутствует в PR88; scope prerequisite не включён.

## История этапа: Recover request.Name owned read поверх CI647

PR88 OPEN/DRAFT/source `b22713891873999d83b807670a7b7a9037d8a9ad` и checkpoint
`de03011751d66a1f7fe30133b32ea1e2ea55dbf8` сверены. До изменений51 worktrees clean/locks0,
root sole active agent; изолированная work/progo-recover-name-on647-20261023 сохраняет всю работу.
Полные [CI647](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39990260979) Windows13063/Ubuntu578
строк самостоятельно прочитаны; оба checkout/Windows build identity: tested merge
`8ea08e14c9f41a3b2670f65a523c297898ffa3a5`. JSON ci647_final содержит per-case evidence.
PASS Reissue8/Create8/API6/UI8/UI12, snapshot12/dispatcher5/ownership8/cleanup4/repair8/list10/
recover10/fresh10/HTTPS recovery11/freshHTTPS10/export11/chooser3. Desktop5298/Instance30/
Maintenance126/Shutdown47/HomeVPN3590/Portable91/SSH policy61. Python reported28 incl2SKIPPED;
Ubuntu operation16/QR13/provisioning PASS. Pack runner-only PASS; uploads/prune SKIPPED.
Исторические permissions640/startup642 причины не объявляются исправленными; diagnostics сохранены.

ТОЛЬКО Recover request.Name UI read через existing reader: local code `95178d4fbde191b3f216847aa3fa7b82427da9e8`,
2 runtime+1 existing test167add/12delete. Advisory pending-state получает только readonly Name,
без полного request/token/credentials/journal/lease. Recover ждёт existing RefreshPendingAsync/
RunAdminOwnedAsync с original Stop identity/явным owner, затем фиксирует label до recovery API.
Missing/unreadable/cancelled не запускает recovery. Перед handoff проверяется прежний Stop scope.
Подтверждённый handoff сохраняет прежнее post-consumption flag refresh: pre-read его не заменяет.
Legacy no-service fixtures сохраняют прежнее синхронное чтение, запоминая только имя без лишнего I/O.
Core Pending/admission/recovery ID/consumption/transport byte-identical; Create/Reissue и все прежние
тела тестов сохранены. Новых функций/внешних действий нет.

Recover name9: complete,missing,corrupt,locked,close-retry,dispose,stop-retry,service-dispose,stop-result.
Actual ShowDialog/default Recover + real private-state reader под barrier5s/modal15s/joins5s.
Явный original name/returned ID, distinct draft/selection, original protected request ID и bytes;
UI heartbeat/reentry gate/no late UI/handoff после Stop, Cancel→reopen и Stop→explicit Refresh→Recover.
Ноль invite/revoke/reissue, ровно один explicit recovery; тестовый handoff намеренно прерывается до
неизменённого consumption boundary. Stop-result callback-only, не доказательство owned transport.
Finally release/join actual body/own window+stream/service cleanup; old timeout/assert/coverage unchanged.

PASS local review/structural preservation/signatures/C#5 await scopes/lexical delimiters/diff/JSON/
public-content321source0release/added secretsPII0. Lexical не AST/compiler; dotnet/pwsh/Windows нет,
new compilation/Recover name9/full suites NOT_RUN. SUCCESS647 не переносится на candidate.
Publication PASS: source `8e58d70c785227837f2a9063fe4a0688500481b6` в existing PR88
integration/approved-plan-31-20261011, fresh expected-head non-force update. Fetched exact647 parent,
full tree `3545c5547a3b80e1f208e69a074a68c78e666319` равен reviewed freeze
`60b73dff923d0d56af7d1e8f454f18bffc669f7f`. Public diff6files699add/75delete:
2 runtime32add/12delete +1 existing test135add/0delete +3docs532add/63delete.
Merge ref `f332d1cac3d549feac2a436c9cc4e2cb8bb32523` пока НЕ final tested merge (NOT_RUN).
Один automatic [CI648/39995378692](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39995378692):
exact-source count1/pull_request/attempt1, in_progress/conclusion null.
Windows120557429765 Repository hygiene and build in_progress; Ubuntu120557429408 provisioning
in_progress (metadata, новые suites NOT_RUN). SUCCESS647 не переносится на candidate.
Workflows/VERSION unchanged, public uploads OFF/release/prune ineligible; final skips not yet checked.
Post-publication3-doc local checkpoint намеренно НЕ pushed, чтобы не создавать второй CI;
полный SHA в git log -1/PR88 body. STOP без полного wait/второго этапа.
Далее ТОТ ЖЕ CI648 full final logs/exact source+tested merge/Recover name9/old suites.
Остаток: ConfirmConsumed create/reissue/recover bindings existing API8; invite/revoke/reissue/
recover-invite owned transport (раздельные bounded stages). FullR09/owner PC acceptance open.
Реализация:58–80/100 · Проверенная готовность:22/100 · Изменение:+0;
главный блокер: Windows proof нового slice, затем consumption/transport UI I/O.

## История этапа: Reissue uncertainty owned pending-read поверх CI646

Сверены PR88 OPEN/DRAFT, source `1a0b9ab4a3f75775bb712f418a780fb9f754c40f`, checkpoint
`b0b7a093e0b991b72bfc1a9cb89f2a0c0d61b82f`. До изменений51 worktrees clean/locks0,
root sole agent; работа выполняется в отдельной work/progo-reissue-uncertainty-on646-20261023.
Полные финальные [CI646](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39984905832) Windows12904/Ubuntu578
строк самостоятельно прочитаны. Checkout обоих jobs и Windows build identity: tested merge
`2c1787a60146fc7abeb8555af2e60b82bf5238d6`. Exact per-case evidence: JSON ci646_final.
PASS Create uncertainty8 (empty,pending,corrupt,locked,close-retry,dispose,stop-retry,service-dispose),
API6/UI8/UI12, selection exact ID/missing-ID, snapshot12/dispatcher5/ownership8/cleanup4/repair8/list10/
recover10/fresh10/HTTPS recovery11/freshHTTPS10/export11/chooser3. Desktop5298/Instance30/
Maintenance126/Shutdown47/HomeVPN3431/Portable91/SSH policy61. Python reported28 incl2SKIPPED,
Ubuntu operation16/QR13/provisioning PASS. Pack PASS только runner; uploads/prune SKIPPED.
Прежние CI640 permissions/CI642 startup причины не объявляются исправленными; диагностика сохранена.

Выбран ТОЛЬКО Reissue IssueUnconfirmed pending-read: это следующий согласованный синхронный UI I/O.
Local code `89d68a07c812e41c9dbd0d8133ad42c85f027b2f`:1 runtime+1 existing test147add/2delete.
Existing RefreshPendingAsync/RunAdminOwnedAsync/original Stop scope/явный owner; pending/unknown/
unreadable остаются gated, empty не разрешает слепой повтор без Refresh. Один read/version guard;
RevokeUnconfirmed и Complete/handoff/consume неизменны. Никакого нового API, recovery/consumption/
transport изменения. Create uncertainty8 и все прежние тела тестов byte-identical.

Восемь новых actual ShowDialog lifecycle modes: empty,pending,corrupt,locked,close-retry,dispose,
stop-retry,service-dispose. Реальный reader/DPAPI own fixture под barrier5s, modal15s/joins5s;
existing reissue core получает ровно revoke+invite frozen selected ID/name, без реального SSH.
Проверяются UI heartbeat/gating, confirmed old revocation/same-name sibling/selection/search/draft,
no replacement/handoff/consume/duplicate read, exact private bytes/request. Stop→explicit read-only
Refresh не повторяет revoke/invite и требует нового явного выбора; Close→reopen retained pending;
Dispose не получает late status. Finally releases barrier/disposes own window/stream/joins actual body
и service cleanup; старые timeout/assert/coverage сохранены.

PASS local review/structural isolation/signatures/C#5 await scopes/lexical delimiters/diff/JSON/
public-content321source0release/added secretsPII0. Lexical не AST/compiler.
Windows/.NET Framework/PowerShell недоступны: новая compilation/Reissue uncertainty8/full suites
NOT_RUN; SUCCESS646 не переносится на candidate.
Publication PASS: source `b22713891873999d83b807670a7b7a9037d8a9ad`, existing PR88
integration/approved-plan-31-20261011, fresh expected-head non-force update. Fetched parent exact646,
full tree `4a286e00c54379c9c786f014871de67668699d63` равен reviewed freeze
`9bb54f7866d95a8d40a4fda388309d9b9daf7a36`. Public diff5files658add/63delete:
1 runtime9add/2delete +1 existing test138add/0delete +3docs511add/61delete.
Merge ref `8ea08e14c9f41a3b2670f65a523c297898ffa3a5` пока НЕ final tested merge (NOT_RUN).
Один automatic [CI647/39990260979](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39990260979):
exact-source count1/pull_request/attempt1, in_progress/conclusion null.
Windows120539905497 Repository hygiene and build in_progress; Ubuntu120539905804 provisioning
in_progress (metadata, новые suites NOT_RUN). Не переносить646 SUCCESS.
Workflows/VERSION неизменны, public uploads OFF/release/prune ineligible; финальные skips пока не проверены.
Локальный postpublication3-doc checkpoint намеренно НЕ pushed, чтобы не создавать второй CI;
его полный SHA в git log -1/PR88. STOP без полного ожидания/второго этапа.
Следующий шаг — ТОТ ЖЕ CI647 full final logs/exact source+tested merge/Reissue8/все прежние suites.
Оставшиеся production paths: RecoverAsync Pending request.Name; ConfirmConsumed bindings existing API8;
invite/revoke/reissue/recover-invite owned transport (отдельные ограниченные этапы). FullR09/owner ПК open.
Реализация:58–80/100 · Проверенная готовность:22/100 · Изменение:+0;
главный блокер: Windows proof нового slice и перечисленные незавершённые UI I/O paths.

## История этапа: Create invitation uncertainty read поверх CI645

HEAD PR88/source `7ce8d202a7ddd15b7f7733fbc493a3d27b5e59c1` и checkpoint
`42f4dd2260032ccb23ef8bd1526ba05517fa535c` сверены;49 прежних worktrees clean/locks0,
Git/build processes0/root sole agent/другого писателя нет. Изолированная
work/progo-invite-uncertainty-on645-20261023 сохраняет прежние ветки/исправления.
Самостоятельно прочитаны полные [CI645](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39978779254)
Windows12768/Ubuntu578 строк; exact checkout/build tested merge
`20ed439869dbfbaf536693c639c8951d2a13dbbb`. Per-case evidence в JSON ci645_final.
PASS button exact original ID/escape+close gated clear/all7 other conditions, same-name missing-ID
no selection/destructive buttons disabled/no dispatch. API6/UI8/UI12 фактически PASS. Также PASS
snapshot12/dispatcher5/ownership8/cleanup4/repair8/list10/recover10/fresh10/HTTPS11/freshHTTPS10/
export11/chooser3; Desktop5298/Instance30/Maintenance126/Shutdown47/HomeVPN3295/Portable91/policy61.
Python reported28 incl2SKIPPED; Ubuntu operation16/QR13/provisioning PASS. Pack PASS runner-only,
uploads/prune SKIPPED; передачи/установки/owner acceptance нет. Старые причины640/642 не объявляются
исправленными, diagnostics сохранены. Selection/API/worker уже готовы, повторно не реализуются.

Выбран один ближайший остаток по реестру: синхронный HasPending в generic Create uncertainty UI.
Code local `3dd85ed8d5816355ced59ac98a94de439629450e`:1 runtime+1 existing test124add/2delete.
CreateAsync после неподтверждённого ответа ждёт existing RefreshPendingAsync/RunAdminOwnedAsync,
original operation Stop scope/explicit Friends owner. No new API/feature. Empty snapshot не разрешает
новую выдачу: прежний needsRefresh сохраняется до явного read-only Refresh. Unknown/unreadable остаются
blocked; нет raw exception detail и выдуманного подтверждения pending. Version gate исключает второй
post-action read. Cancel/Close/Dispose сохраняет actual ownership, Stop запрещает поздний результат.
Typed cancellation/pending catch, success/handoff/ConfirmConsumed, Reissue/Recover/transport неизменны.

Новые8 actual ShowDialog cases empty/pending/corrupt/locked/close-retry/dispose/stop-retry/service-dispose:
реальный DPAPI/private-state worker под barrier5s, modal15s/final joins5s. Fixture own request моделирует
retained request перед потерянным ответом, transport delegate только бросает safe canary exception.
Один mutation intent/no automatic replay/no handoff/no consume/no duplicate read; exact private bytes,
draft, UI heartbeat/admission, Stop Retry via read-only Refresh и reopen retained pending. Все старые
API6/UI8/UI12/selection и прочие test bodies byte-identical; никаких реальных VPS/credentials/network.
PASS local manual/static/signatures/lexical delimiter/diff/JSON/public321/0/secretsPII0. AST parser
недоступен (NOT_RUN), lexical не выдаётся за AST/compiler. Windows/.NET Framework/PowerShell нет:
new compilation/uncertainty8/full regression NOT_RUN, SUCCESS645 относится только к exact baseline.
Publication PASS: source `1a0b9ab4a3f75775bb712f418a780fb9f754c40f`, existing PR88 integration/approved-plan-31-20261011,
fresh expected-head non-force update. Fetched parent exact645/full tree `0caa67cf03efd57f3008b545c94eae0525321be1`
равен reviewed freeze `d7a924660df4a15309e0c01de9e4d39fc07117e3`. Public diff5files633add/62delete:
1 runtime+1 test124add/2delete +3docs509add/60delete; прочие routes/API/worker/consumption/transport unchanged.
Merge ref `2c1787a60146fc7abeb8555af2e60b82bf5238d6` пока НЕ final tested merge: нужны checkout/build logs.
Один automatic [CI646/39984905832](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39984905832),
pull_request/attempt1/raw exact-source count1; in_progress/conclusion null, updated16:13:02UTC;
Windows120521444261/Ubuntu120521444000 in_progress. New compilation/uncertainty8/API6/UI8/UI12/full
suites NOT_RUN; SUCCESS645 не переносится. PR uploads OFF/release/prune ineligible, completion
skips ещё не наблюдались. Postpublication3-doc checkpoint local HEAD(git log -1), НЕ pushed для
исключения второго CI; full checkpoint в PR88 body. STOP без полного ожидания/второго этапа.
Далее ТОТ ЖЕ CI646 full final logs/exact merge: uncertainty8/API6/UI8/UI12/selection/mandatory suites.
Остаток: reissue HasPending; recovery request.Name; consumption API8 bindings; invite/revoke/reissue/
recover-invite owned transport. Не закрывать fullR09/приёмку ПК.
Реализация:58–80/100 · Проверенная готовность:22/100 · Изменение:+0;
главный блокер: Windows proof нового Create uncertainty slice, затем названные remaining routes.

## История этапа: доказанное исправление selection test-contract CI644

PR88/source `b5af631eb2285a05a12485b4a45acc565c705f48`, checkpoint
`4ec09012d4d2c2b4904a63525d56348f8159acfe` сверены; все worktrees clean/locks0,
активных Git/build процессов/другого агента или писателя нет. Изолированная
work/progo-ci644-selection-contract-20261023; прежние изменения/ветки сохранены.
Полные финальные [CI644](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39918347267)
Windows10365/Ubuntu578 строк прочитаны самостоятельно. Exact checkout/build tested merge
`9c267269441761be2e3f47360916af632f957689`. Safe exact evidence в JSON ci644_final.
Единственный violated=selection-empty(False), selectedOriginalId=True; copies2/calls2/rows2,
needsRefreshFalse. Остальные7 исходных invariant=True, private/draft preserved. Это доказанный
устаревший test-contract ListRefreshWaiting/button, а не runtime defect.

Изначально Refresh сбрасывал выбор всегда. В утверждённом repeated-read migration владелец потребовал
сохранить selection; source6f8a41e уже реализует Replace(items,!needsRefresh): обычный read-only Refresh
сохраняет ТОЧНЫЙ прежний ID, uncertain-mutation reconciliation очищает выбор для нового явного решения.
Legacy test всё ещё требовал empty и для обычного route button. Исправление не просто снимает assert:
predicate теперь gated ? selectionEmpty : selected!=null && selected.Id==first.Id. Safety исходного
reconciliation сохранена и дополнена проверкой disabled Revoke/Reissue. Все7 других условий прежние.

Code local `4d511bda96dbb8ba45a32f4a5426f50d16ddcd43`:ONLY HomeVpnPreparationTests25add/4delete.
Регрессия missing-ID использует ещё один РЕАЛЬНЫЙ Refresh/native fixture: серверный ответ оставляет
другого друга с ТЕМ ЖЕ именем и вторым ID. Assert exact survivor ID/selected null/index-1/disabled
Revoke+Reissue, create/reconciliation/draft/search/private bytes. Нажатия disabled buttons не добавляют
copy/list calls и не потребляют request. Fixture response восстанавливается finally; исходные ожидания,
Pump/таймаут/cleanup не менялись, coverage увеличен. Runtime/transport/API/consumption/security unchanged.

CI644 PASS production/Desktop/relay compilation, startup both invariants, policy61/Desktop5298/
Instance30/Maintenance126/Shutdown47/export11/chooser3/list10; Ubuntu operation16/QR13/provisioning.
API6/UI8/UI12 и поздние suites NOT_RUN, aggregate HomeVPN/Portable не достигнуты. Pack/uploads/prune
SKIPPED. Исторические причины permissions640/startup642 не объявляются исправленными; diagnostics сохранены.
PASS local manual structural review/signatures/diff/JSON/public321/0/secretsPII0. Временный AST parser
недоступен: AST NOT_RUN, не выдаётся за compilation. .NET Framework/Windows unavailable: новый
compiler/native exact-ID/gated-clear/missing-ID/full regression NOT_RUN; старые PASS не переносятся.
Publication PASS: source `7ce8d202a7ddd15b7f7733fbc493a3d27b5e59c1`, existing PR88 integration/approved-plan-31-20261011,
fresh expected-head non-force update. Fetched parent exact644/full tree `9b352d5f9c01305e9e8f5fb63d2e2b672c2be25a`
равен reviewed freeze `756d0a75c9e4d4605c22a5fd264e8d12872f0aa5`. Public diff4files345add/59delete:
1 test25add/4delete +3docs320add/55delete; runtime/other methods/workflows/VERSION unchanged.
Merge ref `20ed439869dbfbaf536693c639c8951d2a13dbbb` пока НЕ final tested merge: нужны checkout/build logs.
Один automatic [CI645/39978779254](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39978779254),
pull_request/attempt1/raw exact-source count1; in_progress/conclusion null, updated15:30:04UTC;
Windows120500269788/Ubuntu120500270121 in_progress. New compilation/exact-ID/gated-clear/missing-ID/
API6/UI8/UI12/full suites NOT_RUN. PR uploads OFF/release/prune ineligible; completion skips ещё
не наблюдались. Postpublication3-doc checkpoint local HEAD(git log -1), НЕ pushed для исключения
второго CI; exact checkpoint в PR88 body. STOP без полного ожидания/второго этапа.
Далее ТОТ ЖЕ CI645 full final logs/exact merge: button/escape/close, missing-ID/no-dispatch safety,
API6/UI8/UI12 и все mandatory suites. Written regression не объявляется выполненной.
Реализация:58–80/100 · Проверенная готовность:22/100 · Изменение:+0;
главный блокер: Windows proof corrected selection/missing-ID и недостигнутые invitation suites.

## История этапа: CI643 Refresh retry — test-only восемь инвариантов

HEAD PR88/source `ff8e6e25e7f5b4d58606faf1aff5e290c15d393a` и checkpoint
`9d17c59c5098c351cc40274a5e8f5dd581ff3db1` сверены;47 прежних worktrees clean/locks0,
активных Git/build процессов нет/root единственный агент, иной писатель не обнаружен. Отдельная
work/progo-ci643-refresh-diagnostic-20261023 сохраняет все прежние ветки/изменения.
Самостоятельно прочитаны полные [CI643](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39914345110)
Windows10356/Ubuntu578 строк: checkout/build identity exact tested merge
`f99df0878e1c8a0ffa08363ed52a12d162967852`. Exact evidence/markers в JSON ci643_final.

Startup diagnostics: capturedPidPresent/aliveKnown/alive=True, releaseExists=False. Before/after
connect/after alias:count1/pidPresentTrue/pidSameTrue/connectingTrue/stoppingFalse/requestedTrue/
expectedWaitingLifecycleTrue; оба исходных invariant=True, violations пусто. CI642 отказ НЕ
воспроизвёлся; его причина не установлена, runtime/test-contract fix не объявляется. Safe diagnostics
и гипотеза opportunistic CurrentPid null на busy gate сохранены. SSH policy61 PASS643, но причина
исторического permissions CI640 также остаётся невыясненной.
PASS643 Desktop5298/Instance30/Maintenance126/Shutdown47/export modal11/chooser3/owned list10.
Production/Desktop/relay fixture compilation PASS. API6/initialUI8/repeatedUI12 НЕ выполнялись:
HomeVpnPreparationTests.Run(Main88) упал ДО HomeVpnAdminRecoveryTests.Run(Main89). Поздние snapshot/
dispatcher/ownership/cleanup/repair/recover/fresh/HTTPS suites NOT_RUN643; aggregate HomeVPN/Portable
не достигнуты. Ubuntu operation16/QR13/isolated provisioning PASS, pack/uploads/prune SKIPPED.

Новый FAIL07:19:05.0395862UTC: tests/HomeVpnPreparationTests.cs:756–758, ListRefreshWaiting/button,
финальный составной assert после cancellation PASS/explicit retry. Safe stack: HomeVpnWizardTests.Check
il12→ListRefreshWaiting il1250→ListWaiting il351→PreparationTests.Run il656→WizardTests.Main il1794.
Ожидаются copies2/listCalls2/create enabled/needsRefresh false/selection empty/rows2/draft same/private
unchanged. Actual conjunct facts не выведены. Статическое противоречие: обычный non-gated Refresh
теперь сохраняет selected ID по требованию владельца; legacy assert всё ещё ждёт selection empty.
Это НЕ доказательство, что только этот conjunct нарушен; не меняем runtime или ожидание наугад.
В разрешённом объёме выбран ОДИН targeted diagnostic CI, а не повтор без новых данных.

Code local `9cc2fd762ed00ee66d15bfd5dbfab3496bc84735`: ONLY HomeVpnPreparationTests.cs14add/2delete.
Все восемь исходных условий именованы и snapshot-ятся в прежнем порядке ДО output; сохранены
один combined assert/selection-empty/таймауты/coverage/последовательность сценария/fixture cleanup.
Выводятся counts/boolean и все violated names; selectedOriginalId — только boolean сравнения,
не ID. No raw name/path/args/settings/credentials/journal/private bytes. Существующий safe stack
сохранён. Никаких runtime/new features/security/VPS/network/credential действий.
PASS local review/static/signatures/AST syntax1/C#5/diff/public321/0/secretsPII0. Windows/.NET Framework/
PowerShell недоступны: новый compiler/native8-invariant diagnostics/API6/UI8/UI12/full regression NOT_RUN.
Publication PASS: source `b5af631eb2285a05a12485b4a45acc565c705f48`, existing PR88 integration/approved-plan-31-20261011,
fresh expected-head non-force update. Fetched parent exact643/full tree `6c628bf2715183e9966cc6391f6aaad2eba4e0d8`
равен reviewed freeze `de63d71d1567e2b6504bf7a35c080d268ea69ed3`. Public diff4files405add/51delete:
1 test14add/2delete +3 docs391add/49delete; runtime/startup diagnostics/other tests unchanged.
Merge ref `9c267269441761be2e3f47360916af632f957689` пока НЕ final tested merge: нужны checkout/build logs.
Один automatic diagnostic [CI644/39918347267](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39918347267),
pull_request/attempt1/raw exact-source count1; in_progress/conclusion null, updated07:31:42UTC;
Windows120297230336/Ubuntu120297230272 in_progress. New compilation/native8-invariants/API6/UI8/UI12/
full suites NOT_RUN; SUCCESS build643 не переносится. PR uploads OFF/release/prune ineligible,
completion skips пока не наблюдались. Postpublication3-doc checkpoint local HEAD(git log -1), НЕ
pushed во избежание второго CI; exact checkpoint в PR88 body. STOP без полного ожидания/второго этапа.
Следующий шаг — ТОТ ЖЕ CI644 full final logs/exact merge, все8 named conditions, затем отдельно
минимальный доказанный fix с сохранением ordinary selection/gated reconciliation safety.
UI12 PASS только по реально выполненным markers; startup642 причина остаётся unresolved.
Реализация:58–80/100 · Проверенная готовность:22/100 · Изменение:+0;
главный блокер: concrete Refresh retry invariant и недостигнутые invitation API6/UI8/UI12.

## История этапа: разрешённая test-only диагностика CI642 count/PID

PR88/source `6f8a41e67709dae2a9794885e7e975918401f765`, local checkpoint
`b8f4a52e4fedbca2232316b71d7387f02ac890f0` сверены;46 прежних worktrees clean/locks0,
Git/build processes0/root sole agent, иной писатель не обнаружен. Exact-source CI collection содержит
только completed FAILURE642/attempt1, дубликатов/активных CI на baseline нет. Отдельная ветка
work/progo-ci642-command-diagnostic-20261023; прежняя работа не меняется/не откатывается.
CI642 source/merge/failure evidence сохранены в ci642_final; причина code/test/infra всё ещё не доказана.
Владелец отдельно разрешил test-only instrumentation и ОДИН automatic diagnostic CI; предыдущий
permission blocker снят, но guessed runtime fix по-прежнему запрещён.

Code local `4fe4e87df3d61a7d391efe07f3c944eafb6cefcf`: ONLY DesktopStartupTests.cs49add/2delete.
Именованы оба исходных условия: pending-route-count-one (count==1), original-fixture-pid-retained
(CurrentPid==captured nullable pid). Исходный conjunction/nullable equality и один combined Check
сохранены; ничего не исключается. Оба operand snapshot фиксируются в original count→PID порядке
до диагностического вывода. Original calls connect→codex-on не меняются; добавлены nonblocking
metadata samples до connect/после connect/после alias. count/pidPresent/pidSame/connecting/stopping/
requested/expectedWaitingLifecycle — только int/boolean. Own captured fixture liveness измеряется
отдельно: aliveKnown/alive, capturedPidPresent/releaseExists. При ошибке чтения liveness UNKNOWN,
не dead и не PASS. Sequential snapshots не выдаются за атомарный lifecycle.
CurrentPid использует Monitor.TryEnter и может дать null при занятости gate: это source contract,
но НЕ доказанная причина642. Runtime/getters/route admission/process ownership не меняются.

Collect violated names для обоих условий; failure-only safe stack: StackTrace(false), только имена
методов DesktopTests, максимум6. Никаких PID/путей/args/settings/raw exception/credentials/journal/
секретов в новых logs. Нет новых ожиданий/таймаутов/процессов; только readonly query собственного
captured fixture process, без kill/ACL/network. Original test steps/assert count/coverage/timeout/
release gate и finally fixture cleanup неизменны. Прежние API6/UI8/UI12/runtime tests не трогаются.

PASS local manual/static/signatures/AST syntax1/C#5/diff/public321/0/secretsPII0. Temporary review
checker false-positive Wait matched WaitingLifecycle; исправлен token check, повторно PASS до push.
Windows/.NET Framework/PowerShell отсутствуют: new compilation/native diagnostics/UI12/API6/UI8/
Instance/full suites NOT_RUN. CI642 compilation production/Desktop PASS, full Desktop FAIL;
SSH policy/ACL и relay suites NOT_RUN642, Ubuntu PASS — не переносятся на новый SHA.
Publication PASS: source `ff8e6e25e7f5b4d58606faf1aff5e290c15d393a`, existing PR88 integration/approved-plan-31-20261011,
fresh expected-head non-force update. Fetched parent exact642/full tree `b7f61ef2e0a70ae30635c6f23e385d523df13b73`
равен reviewed freeze `36edeb58ed1e268db5c330bf09fe9aaa855753ab`. Public diff4files543add/70delete:
1 test49add/2delete +3 docs494add/68delete; runtime/other tests/workflows/VERSION unchanged.
Merge ref `f99df0878e1c8a0ffa08363ed52a12d162967852` пока НЕ final tested merge (нужны checkout/build logs).
Один automatic diagnostic [CI643/39914345110](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39914345110),
pull_request/attempt1/raw exact-source count1; in_progress/conclusion null, updated06:54:11UTC;
Windows120284369071/Ubuntu120284368749 in_progress. New compilation/native invariant diagnostics/
UI12/API6/UI8/full suites NOT_RUN. PR uploads OFF/release/prune ineligible; completion skips пока
не наблюдались. Postpublication3-doc checkpoint local HEAD(git log -1), НЕ pushed для исключения
второго CI; full checkpoint в PR88 body. STOP без полного ожидания/второго этапа.
Следующий шаг — ТОТ ЖЕ CI643 full final logs/exact merge, именованные count/PID/liveness/lifecycle
facts и фактически выполненные suites. Нет runtime fix; причина642 пока не доказана.
Если отказ не воспроизводится, причина642 остаётся unproven, не объявлять runtime fix или полный UI12 PASS.
Реализация:58–80/100 · Проверенная готовность:22/100 · Изменение:+0;
главный блокер: concrete command availability invariant evidence и новая Windows проверка UI12.

## История этапа: CI642 investigation — причина пока не доказана, STOP

PR88/source `6f8a41e67709dae2a9794885e7e975918401f765` и checkpoint
`b645744846bdf27c5c00201107cb5c3c8c25932e` сверены;45 worktrees чистые, locks0,
активных git/build процессов нет, root единственный агент/других писателей не обнаружено.
Полные финальные [CI642](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39911077389)
Windows4275/Ubuntu578 строк прочитаны самостоятельно; checkout/build identity exact tested merge
`709e070d10138ffa330a0def5c21d61e0bf3e235`. Safe evidence в JSON ci642_final.

FAIL06:35:29.6307562UTC: tests/DesktopStartupTests.cs:150, CommandAvailabilityWorkflow.
После real CLI toggle и gated SOCKS ожидания вызываются Execute(connect), Execute(codex-on).
Составной assert: PendingRouteCount==1 && proxy.CurrentPid==captured pid. Message: dispatcher
rejects stale plain Connect and duplicate alias without replacing pending route.
Safe stack: DesktopTests.Check(Boolean,String)→CommandAvailabilityWorkflow(SettingsService)→Main(String[]).
Предыдущий UI availability assert passed06:35:29.5311042UTC. Actual count/old-new PID equality/
lifecycle state не напечатаны: неизвестно, нарушен первый/второй/оба инварианта. Первопричина
code/test/runner НЕ ДОКАЗАНА. Оригинальный fixture ждёт own release-file; исходный barrier и
route callbacks просмотрены, но нет evidence его состояния/child exit/late route completion.
Нельзя объявить race/fixture/инфраструктуру установленной причиной или чинить runtime наугад.

Это НЕ подтверждённое повторение permissions CI640. CommandAvailabilityWorkflow вызывается Main:118,
SSH policy только Main:162; execution до policy НЕ дошло. Policy и сохранённый failure-only ACL block
NOT_RUN, никаких ACL flags из этого запуска нет. Историческая причина CI640 остаётся невыясненной.
PASS exact642: production Build OK/Desktop compilation; ранее завершённые repair/vault/SOCKS51,
logging/diagnostics/backups/update suites приведены в JSON ci642_final. Полный Desktop FAIL.
Instance30/API6/initialUI8/repeatedUI12 и snapshot/dispatcher/ownership/cleanup/repair/list/recover/
fresh/HTTPS/export/chooser/Portable suites NOT_RUN; relay step SKIPPED, их fixture compilation NOT_RUN.
Ubuntu operation16/QR13/isolated provisioning PASS по full final logs. Pack/uploads/prune SKIPPED.
Результаты641 не переносятся на642.

Изменены ТОЛЬКО3 документа. Никаких code/test/timeout/assert/coverage/workflow/security/SSH/network
изменений. No push/new CI/rerun: команда разрешает новый CI только при обоснованном исправлении,
а доказанного исправления пока нет. Сохранены все предыдущие production changes/diagnostics.
Local docs checkpoint — HEAD(git log -1), не опубликован; PR HEAD остаётся6f8a41e.
Следующий ограниченный шаг — отдельный test-only диагностический этап: именованные ОБА исходных
инварианта, безопасные actual pending count/PID-present/PID-equal/connecting/stopping flags до и
после каждого Execute, без settings/args/credentials. Assert semantics/order/timeout/coverage прежние.
Для diagnostic CI нужно отдельное разрешение в силу текущего ограничения «CI только при обоснованном
исправлении». До него dependent fix BLOCKED, не guessed fix и не blind rerun. Других этапов нет.
Реализация:58–80/100 · Проверенная готовность:22/100 · Изменение:+0;
главный блокер: неразделённый route-count/PID invariant и невыполненные invitation UI12.

## История этапа: повторные invitation pending-read поверх CI641

PR88 HEAD/source `589a6364d42f2fbbcdbbfe4bd80396c27a26f41f`, local checkpoint
`70f0d4db5d645244145a58b2f6f1165016af2654` сверены.45 прежних worktrees чистые/locks0,
root единственный агент; active Git/build processes0 по /proc (ps unavailable). Отдельная ветка
work/progo-invitation-refresh-on641-20261023; сохранены все прежние ветки и checkpoints.
Полные финальные [CI641](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39907256278)
Windows12520/Ubuntu578 строк самостоятельно прочитаны: exact checkout/build tested merge
`517b65079be6d6645f132e313b2acf551cbb887c`. JSON ci641_final хранит exact per-case markers.
PASS SSH policy61/role canary4/all3 native policy targets/API6/initial UI8/reopen, Desktop5298/
Instance30/Maintenance126/Shutdown47/HomeVPN3079/Portable91; snapshot12/dispatcher5/ownership8/
cleanup4/repair8/list10/recover10/fresh10/HTTPS recovery11/freshHTTPS10/export11/chooser3.
Python reported28 incl2SKIPPED; Ubuntu operation16/QR13/isolated provisioning PASS по final logs.
Pack PASS runner-only; uploads/prune SKIPPED, ни передачи пакета, ни owner installation/acceptance.
CI640 permissions refusal НЕ воспроизвёлся: failure-only ACL block NOT_RUN. Конкретный файл/ACL/
principal и первопричина остаются НЕВЫЯСНЕННЫМИ; runtime/ACL fix не объявляется, diagnostics сохранены.

Один выбранный production slice: повторные pending reads после существующих операций и Refresh.
Code local `83db3a07889501395e4c288d38e9b725f36996d3`:2 runtime+1 existing test,204add/36delete.
Shared initial/repeated reader использует НЕИЗМЕНЁННЫЙ readonly API/RunAdminOwnedAsync; explicit
Friends owner/dispatcher. Logical ActionWork и physical PendingReadWork сохраняют владение. Original
operation Stop scope захватывается ДО transport и проверяется before/after private read/publication;
поздний list response после Stop не допускает fresh read/rows. Busy/action gate держится до settlement.
Один pending snapshot/version на action: Refresh не дублирует I/O в post-action path. Cancel Close/
Dispose удерживает реальную работу; explicit Close после settlement, Stop Retry/new owner reopen без
старого результата. Ошибка/отмена не изобретают empty и не снимают pending/reconciliation gate.
Refresh до accepted snapshot не меняет rows; сохраняет выбранный ID/search/draft. Старый safety
контракт uncertain mutation reconciliation по-прежнему очищает destructive selection, требуя явный
новый выбор (старый reissue test не изменён/не ослаблен). Другие Replace callers сохраняют прежний default.

Новые12 actual ShowDialog cases: refresh-empty/pending/corrupt/locked, post-complete/failure/pending,
close-retry/dispose/stop-retry/service-dispose/stale-scope. Real private bytes/held read5s/modal15s/
cleanup joins5s; native UI heartbeat, no parallel admission/duplicate I/O, Stop scope, unchanged request,
selection/search/draft and no late disposed status. Post-action route использует существующий revoke;
только transport delegate заменён fixture, реальные VPS/credentials/network не вызываются.
Old API6/UI8 и все прежние test method bodies сохранены точно. Consumption/direct uncertainty reads/
recovery request.Name/transport core/workflows/VERSION unchanged; никакой новой функции/протокола.
PASS local manual/static/AST syntax3/C#5/signatures/diff/public321/0/secretsPII0. Windows/.NET Framework/
PowerShell недоступны: новая compilation/repeated12/full regression NOT_RUN, SUCCESS641 не переносится.

Publication PASS: source `6f8a41e67709dae2a9794885e7e975918401f765`, existing PR88 integration/approved-plan-31-20261011,
fresh expected-head non-force update. Fetched parent exact641/full tree `553612d55db87d0f3dadfe971d6a545c0e7944a1`
равен reviewed freeze `9c6a66f41e7ae2972408081b1464a22f5767ac80`. Public diff6files730add/88delete:
2 runtime+1 existing test204add/36delete +3docs526add/52delete; остальные files unchanged.
Merge ref `709e070d10138ffa330a0def5c21d61e0bf3e235` пока НЕ final tested merge: нужны checkout/build logs.
Один automatic [CI642/39911077389](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39911077389),
pull_request/attempt1/raw exact-source count1, in_progress/conclusion null, updated06:22:27UTC;
Windows120273983681/Ubuntu120273983952 in_progress. New compilation/repeated12/API6/UI8/full suites
NOT_RUN, SUCCESS641 не переносится. PR uploads OFF/release/prune ineligible; completion skips ещё
не наблюдались. Postpublication3-doc checkpoint local HEAD(git log -1), НЕ pushed во избежание
второго CI; full checkpoint в PR88 body. STOP без полного ожидания/второго этапа.
Следующий шаг — ТОТ ЖЕ CI642 full logs/exact tested merge: repeated12/API6/UI8/selection/
reconciliation и все mandatory old suites; historical CI640 ACL cause unresolved.
Остаток production invitation UI I/O:1) direct create/reissue uncertainty и recovery request.Name;
2) ConfirmConsumed create/reissue/recover (saved consumption API8 separately);3) invite/revoke/reissue/
recover-invite owned transport binding. CI640 ACL cause remains unresolved historical finding.
Реализация:58–80/100 · Проверенная готовность:22/100 · Изменение:+0 — нет нового полного weighted gate;
главный блокер: exact Windows proof нового repeated12, затем remaining UI I/O и owner acceptance.

## История этапа: CI640 native auth-policy — bounded test-only diagnosis

HEAD PR88/source `fcc6e70b69ef591997f26a5b7fd5e12be5f7720b`, local checkpoint
`ce474ce730dc3787a512fbe89f3d76e04bf901ff`; все прежние worktrees чистые, Git locks0,
других git/build операций/агентов/писателей не обнаружено. Отдельная work/progo-ci640-auth-diagnostic-20261023.
Полные финальные [CI640](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39904249694)
Windows7755/Ubuntu578 строк самостоятельно прочитаны. Checkout/build identity exact tested merge
`45a69d39a2e7c7378f50e1202a60d863da5859f2`. JSON ci640_final хранит safe evidence/summary.

FAIL05:26:07.9541087UTC: tests/DesktopSshAuthPolicyTests.cs:131, AuthPolicyNative/policy.disabled,
original oracle assert ExitCode0&&!Truncated. exit255/0x000000FF/truncatedFalse/stdoutChars0/
stderrChars330/permissionsHintTrue/configHintFalse; stack DesktopTests.Check→AuthPolicyNative→
SshAuthPolicyWorkflow→Main. Production Build OK и Desktop compilation PASS; причина НЕ compilation.
Доказан native OpenSSH отказ «Bad owner or permissions» при чтении fixture config tree. КАКОЙ файл/
principal/rule — НЕ доказано; совпадение с новым invitation runtime, runner drift или inherited ACL —
гипотезы. Не изменяем runtime/ACL по предположению и не делаем blind rerun.

Фактически PASS SSH start66/startup62/inventory67/keymatching52/signing55/preparation76/signingUI50;
остальные ранее завершённые suites перечислены в ci640_final. Полный Desktop FAIL.
Instance30/API6/UI8/HTTPS recovery11/freshHTTPS10/snapshot/dispatcher/ownership/cleanup/repair/list/
recover/fresh/export/chooser/Portable — NOT_RUN на640, relay step SKIPPED. Production compilation
не означает compilation invitation fixture: она NOT_RUN. Ubuntu operation16/QR13/isolated provisioning
PASS по финальному логу. Pack/uploads/prune SKIPPED; прежний SUCCESS639 не переносится.

Code local `e40dc17cc709a3194f158a45f0ce52cf31e6cc09`: только DesktopSshAuthPolicyTests,
56add/1delete. При native failure до ПРЕЖНЕГО assert печатаются allowlisted role booleans и
readonly ACL flags/counts для собственных config/include/locked identity/directory. No raw stderr,
paths/SID/SDDL/names/credentials/journal/config contents; exception только typed flags/HResult.
Original three targets/order/7000ms/assert/cleanup unchanged, additional pure role canary4.
Reads не меняют ACL/fixture bytes; runtime/server/workflow/test-runner unchanged. Запуск диагностический,
чтобы определить конкретный file+ACL invariant; не считается исправлением первопричины.

PASS local static/manual/signatures/AST C#5/diff/public321/0/secretsPII0. Windows/.NET Framework/
PowerShell недоступны: новая compiler/native диагностика/роль canary4/API6/UI8/full suites NOT_RUN.
Publication PASS: source `589a6364d42f2fbbcdbbfe4bd80396c27a26f41f`, guarded non-force expected-head existing PR88 branch
integration/approved-plan-31-20261011. Fetched parent exact640/full tree `e939ae31e3eb6c5796448d79267ea997e1ca0bf1`
равен reviewed freeze `598f406abe6391c080c8c2077828d27800503996`. Public diff4files435add/67delete:
1 test56add/1delete+3docs379add/66delete; runtime/workflows/VERSION/other tests byte-identical640.
Merge ref `517b65079be6d6645f132e313b2acf551cbb887c` пока НЕ final tested merge (нужны checkout/build logs).
Один automatic [CI641/39907256278](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39907256278),
pull_request/attempt1/raw exact-source count1; in_progress/conclusion null, updated05:42:45UTC;
Windows120261940053/Ubuntu120261939848 in_progress. Новые compiler/native/canary4/API6/UI8/full suites
NOT_RUN. PR uploads OFF/release/prune ineligible, completion skips ещё не наблюдались.
Postpublication3-doc checkpoint local HEAD(git log -1), НЕ pushed во избежание второго CI;
точный checkpoint в PR88 body. STOP без ожидания полного CI/следующего этапа.
Следующий шаг — ТОТ ЖЕ diagnostic CI: full logs/checkout/build identity, failure role+ACL facts;
при случайном PASS oracle причина ACL остаётся недоказанной. Проверить реально выполненные API6/UI8
и regressions без переноса PASS. Ни второй этап, ни runtime fix в этом проходе.
Реализация:58–80/100 · Проверенная готовность:22/100 · Изменение:+0;
главный блокер: exact native refusal root cause и новая Windows проверка invitation, затем owner acceptance.

## История этапа: initial invitation pending UI поверх CI639

Baseline source `b935fde9770bfcec83913e19d929a9558e4c76e2`, checkpoint
`c3348452409b168a48925e9e4423d667df8f56da`; PR88 head сверён, чужих dirty/lock/build
операций и активных писателей не обнаружено. Изолированная ветка сохраняет все прежние worktrees.
CI639 https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39900231280 completed SUCCESS:
самостоятельно прочитаны полные Windows12366/Ubuntu578 строк; tested merge/build identity
`0b6e71eb6794c03801949a83645917353e76b84e`. Exact per-case markers в JSON ci639_final.
PASS fresh HTTPS10, recovery11 (owner-dispose violations0/liveDraftTrue/late publications0),
Instance30, snapshot12/dispatcher5/ownership8/cleanup4/recover10/fresh10/repair8/list10/export11/chooser3;
Desktop5294/Maintenance126/Shutdown47/HomeVPN2929/Portable91. Python reported28 incl2SKIPPED.
Pack PASS runner-only, uploads/prune SKIPPED; передачи/установки/owner acceptance нет.

Выбор: первичное локальное чтение pending-state при открытии существующего «Доступ друзей».
Сохранённый API и API6 перенесены БЕЗ переписывания из `d500df68629d8af7533e4eca2993284c3675161a`;
в public baseline они отсутствовали. Consumption `e8fbd2a` не переносится. Code local
`ce02533755992e0f66fcb4a0aff06c081ef6c699`: 3 runtime+2 existing test files251add/10delete.
Wizard передаёт explicit Friends owner/service. Actual readonly work принадлежит RunAdminOwnedAsync,
Stop удерживает I/O до settlement, CTS Close/Dispose и old Stop scope запрещают поздний результат.
До validated state изменения заблокированы. Явное закрытие отменяет чтение и ждёт реальную работу
асинхронно; CancelButton не завершает ShowDialog раньше. Reopen делает новый read, без позднего
cancelled результата. Снимок advisory, настоящая family admission прежняя; нет новых функций/записи/
потребления request. Legacy injected 3arg constructor совместим; production factory всегда supplies service.

Тесты: перенесённые API6 +8 actual ShowDialog cases empty/pending/corrupt/locked/close-retry/
dispose/stop/service-dispose; последующий reopen проверяет реальный retry. Held I/O5s/modal15s/
cleanup joins5s, real private bytes, no Admin calls, explicit owner, mutation gate, heartbeat и no duplicate.
Existing owned-list fixture ждёт InitializationWork перед Refresh (2 lines); прежние asserts не ослаблены.
PASS local manual/AST syntax5/C#5/signatures/diff/public321/0/secretsPII0; old API6 и existing test bodies
проверены на точное сохранение. .NET Framework/PowerShell/Windows отсутствуют: compilation/new native
API6/UI8/full regression NOT_RUN. SUCCESS639 относится ТОЛЬКО к baseline, не новому коду.

Publication PASS: source `fcc6e70b69ef591997f26a5b7fd5e12be5f7720b`, existing PR88 integration/approved-plan-31-20261011,
fresh expected-head non-force update. Parent exact639; fetched full tree `9d83e9a490da70528cbaa9a426749fce5be00c5d`
равен reviewed freeze `3600e2cf5b480c0864901aa0bd291f7fe3299421`. Public diff8files822add/82delete:
3 runtime+2 existing test251add/10delete,3 docs571add/72delete; worktree clean before postpub docs.
Merge ref `45a69d39a2e7c7378f50e1202a60d863da5859f2` пока НЕ tested merge: нужна checkout/build identity финальных logs.
Один automatic [CI640/39904249694](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39904249694),
raw exact-source total_count1/pull_request/attempt1; in_progress/conclusion null, updated05:10:04UTC.
Windows120252436528/Ubuntu120252436238 in_progress; new compilation/API6/UI8/full regression NOT_RUN.
Workflows/VERSION прежние, PR uploads OFF/release и prune ineligible; completion skip ещё не проверен.
Postpublication docs-only checkpoint local HEAD (git log -1), НЕ pushed чтобы не создавать второй CI;
full checkpoint также PR88 body. STOP без ожидания/второго этапа. Следующий шаг — ТОТ ЖЕ CI640:
финальные full logs/exact tested merge, API6/UI8/reopen и все mandatory прежние regression.
Остаток production UI I/O invitation (не полный новый scope проекта):
1. Повторный pending-state в RunAsync/RefreshAsync.
2. Pending checks при uncertainty create/reissue и recovery request.Name.
3. ConfirmConsumed после create/reissue/recover (saved API8 отдельно).
4. Invite/revoke/reissue/recover-invite transport через owned worker вместо legacy Admin.
Чужие setup/HTTPS/list/repair/export/EOF/ownership/cleanup routes не повторяются.
Реализация:58–80/100 · Проверенная готовность:22/100 · Изменение:+0 — новый native NOT_RUN;
главный блокер: точная Windows проверка кандидата, затем незавершённые UI I/O paths и приёмка ПК.

## История: новая настройка HTTPS через owned worker поверх CI638

Checkpoint `f75c5a0ba00b2f7aa9e495419ec48823f8acfd86`/HEAD/PR88 сверены; source
`b69e7008bc889d88623669058e95197822b09f1e`, exact tested merge/build identity
`bd8646ce3bb15b2aebeed68b2b6d5ffdd3aad015`. Worktree чистые до начала, root единственный агент,
Git/build/lock операций нет. Сохранена отдельная work/progo-fresh-https-on638-20261023 и прежние ветки.

[CI638/39896043639](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39896043639) completed SUCCESS:
оба полных финальных log самостоятельно прочитаны, Windows120226632093(12105 строк),
Ubuntu120226631811(578 строк). Exact per-case evidence в JSON ci638_final, metadata не заменяют logs.
PASS Instance30; owner-dispose/liveDraftAtDisposal=True/violations0/TextChanged0:0/publications5:5;
все HTTPS recovery11; snapshot12/dispatcher5/ownership8/cleanup4/recover10/fresh10; repair8/list10;
export modal11/chooser3; Desktop5294/HomeVPN2803/Portable91 и mandatory suites. Relay Python
reported28 incl2SKIPPED, skipped не выдаются за выполненные. Ubuntu operation16/QR13/provisioning PASS.
Pack PASS, runner-only ZIP SHA2562bf8ec967e7bed08f527a41807ddb7f418d6e1815f40cfafd1108d3f56602f92;
uploads/prune SKIPPED, артефактов/передачи/установки/owner acceptance нет. Draft contract и recovery11
повторно не реализуются; свежий PASS относится только к точному638 source/merge.

Выбран следующий незавершённый пользовательский путь: существующая «Настроить HTTPS на VPS».
По реестру recovery уже owned, а fresh share оставался static Admin/ActiveForm. Независимые
зависимости core share receipt/owned confirmation/lease/worker/dispatcher/cleanup доказаны638;
invitation/UI overhaul/новый серверный протокол не нужны. Code `414cb610621a91ebd09b71d2ae6126e130bd813e`:
только2 runtime files и1 existing test file222add/2delete +3 docs. Default button вызывает
ShareOwnedAsync с явными configure window/dispatcher, frozen owner/origin. Worker сохраняет
existing single family lease через key/helper/DPAPI/registration/result/cleanup. Native startup
только через UI owner. Stop после удержанного copy ждёт physical task и отказывает registration/
SSH; принятый request сохраняется при отмене native command. Mode1 использует existing Stop scope
на последующих HTTPS verification/save/consumption boundaries, как уже проверенный recovery mode2.
Не добавлены кнопки/серверные функции; static injected overloads и core receipt protocol прежние.

Fresh HTTPS10: default-refusal/copy-fault/cancel-retry/stop-copy/dispose-copy/owner-dispose/
cancel-command/cleanup-lock/result/stop-result. Реальный ShowDialog/default button, живой heartbeat,
held helper read и native fixture command; single lease/admission; до registration request отсутствует;
Cancel→Retry без старого результата; оригинальный durable request ID соответствует тому, что увидел
native child, journal/private bytes сохраняются; exact locked cleanup Stop false→unlock true без
повтора SSH. Result доходит до реальной confirmation boundary, fixture останавливается до внешнего
HTTPS/save; Stop после accepted result сохраняет Task success, но не допускает verification/consume.
Явные owner/dispatcher, исправленный live/disposed draft и finally own child/barrier/lock/work joins.
Не используются реальные VPS/сеть/credentials; только собственные временные fixture ресурсы.
Old recovery11 body, safe14 instrumentation, setup/list/repair/export UI/EOF/ownership/cleanup сохранены.

PASS local: review/AST syntax3/C#5/scope preservation/diff/public321/0/secretPII0. Перед push отдельно
проверены сигнатуры всех project calls: ShareOwned5..7, Admin8 с copy2/command3, Copy3..4,
WaitForShare5 обязательных с прежним TimeoutMs, dispatch4/scope2; ошибка missing-timeout CI634 не повторена.
Это static, не compiler proof. Локально .NET Framework/PowerShell нет: новая compilation/fresh HTTPS10/
full regression NOT_RUN;638 SUCCESS не переносится. Нет wrapper lease reacquire/public Stop self-await.
HeldIO5s/modal15s/final joins5s/native300s прежние, asserts/coverage не отключались.

Публикация PASS: source `b935fde9770bfcec83913e19d929a9558e4c76e2` existing PR88 ветка
integration/approved-plan-31-20261011; fresh expected-head non-force guard на точном638.
Merge ref `0b6e71eb6794c03801949a83645917353e76b84e` пока не подтверждён checkout/build identity нового CI.
Freeze `62b192a144a923a4383a67812494633f3e0fcff5`, tree `6cbea066716dd4ce92e58c71521b798b00248523`;
fetched parent exact638/full tree equal freeze. Public diff6files889add/71delete:
2 runtime+1 existing test222add/2delete +3 docs667add/69delete; прочие routes/core/workflows/VERSION прежние.
Один automatic [CI639/39900231280](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39900231280),
pull_request/attempt1/raw exact-source runs total_count1; run in_progress/conclusion null,
updated_at2026-10-23T04:25:25Z; Windows120239881809/Ubuntu120239882025 in_progress.
Новая compilation/fresh HTTPS10/full regression NOT_RUN; SUCCESS638 не переносится.
Public uploads OFF/release/prune ineligible; completion upload/prune ещё не проверены.
Post-publication docs-only checkpoint — локальный HEAD (git log -1), не push, чтобы не создавать
второй CI; full checkpoint также в PR88 body. STOP без полного ожидания/второго этапа.
Следующий шаг — ТОТ ЖЕ CI639/exact tested merge: compiler/signatures, fresh HTTPS10 + recovery11/
owner-dispose/Instance30/snapshot12/dispatcher5/ownership8/cleanup4/recover10/fresh10/export/chooser/full suites.
После proof отдельно рассмотреть существующий recover-invite owned path; здесь он не реализуется.
Реализация58–80/100 · Проверенная готовность22/100 · Изменение+0: веса/620 anchors прежние;
fullR09/остальные invitation bindings/package delivery/owner acceptance открыты.

## История: исправление draft lifetime test-contract CI637

Checkpoint `118b14aeabb21b78eebef260f8811c477ae857d3`/HEAD/PR88 сверены; public source
`e0a8185ae3deb66e14872735170b64d1f414acf6`, tested merge/build identity
`5aa6cbe43e07dc25d491b94a91f747b41f28e5aa`. Worktree чистые до начала, root единственный
доступный агент; Git/build/lock операций нет. Отдельная ветка work/progo-ci637-draft-lifetime-20261023,
прежние ветки/checkpoint сохранены. Весь runtime/other tests/scripts/workflows/VERSION равны637.

Оба полных финальных log [CI637/39892894242](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39892894242)
самостоятельно прочитаны: Windows120216525306(11089 строк) FAILURE только relay step,
Repository hygiene and build SUCCESS; Ubuntu120216524993(578 строк) SUCCESS с operation16/QR13/
isolated provisioning PASS. Overall FAILURE, pack/uploads/prune SKIPPED. Test-ProGo/Desktop5294/
Instance30(receipt guards)/Maintenance126/Shutdown47/UpdaterPackage5 PASS, export modal11/chooser3/
repair8/list10 PASS. HTTPS recovery11:5PASS(default-refusal/status-pending/cancel-retry/stop-copy/
dispose-copy), owner-dispose FAIL,5NOT_RUN(cancel-command/cleanup-lock/result/stop-result-copy/stop-result).
Поздние snapshot12/dispatcher5/ownership8/cleanup4/setup recover10/fresh10, HomeProfileHttp,
portable/Python relay transport NOT_RUN из-за порядка вызовов harness; старые PASS не наследуются.

Доказанная причина: в owner-dispose единственный violated invariant — draft-unchanged=False,
остальные13=True. Exact03:29:31UTC state: phase7/violations1/calls0/expected0/copies1/verified0,
formDisposed=True/addressDisposed=True/formHandle=False/addressHandle=False/DialogResult=None,
operation=Faulted/stop=RanToCompletion/publications5/closedPublications5. Safe stack:
HomeVpnWizardTests.Check IL12 → OwnedHttpsRecovery callback IL4968; затем ModalWizardCase IL155 →
OwnedHttpsRecovery IL1159 → Run IL1101 → Main IL1861. Wrapper Test-HomeVpnRelay.ps1:19.
Полные14 outcomes/state/timestamps сохранены в JSON ci637_final; C# stack теперь наблюдаемый,
не source inference. Это test-contract failure, не compilation/Instance/runtime transport или
доказанный infrastructure fault. Исходный assert требует Text у native TextBox уже после его
Dispose/уничтожения handles; это не гарантия сохранности живого пользовательского draft.

Code `139e2eb4b4182d73d1807833fdc87096a97f2dcd`: только OwnedHttpsRecovery одного test file,
17add/4delete +3 docs. Перед Dispose на реальном ShowDialog проверяются живые controls/handles
и неизменный edited draft. После завершения physical operation/Stop тот же draft invariant
требует успешной live-проверки, действительного Dispose/отсутствия handles, неизменного счётчика
реального TextChanged после teardown и dispatcher publications. Счётчик фиксируется после
Dispose, поэтому framework teardown не считается поздним worker update. Для открытых окон
исходное address.Text сравнение буквально прежнее. Это не closing||PASS и не исключение условия:
draft проверен в допустимый срок жизни, после него запрещены поздние записи/публикации.
Все14 имён/порядок и13 иных выражений прежние; aggregate требует0 нарушений, прежний отдельный
no-late-publication assert сохранён. Не менялись scenario phases/physical waits/cleanup/deadlines.
Реальная owner-dispose regression дополнена live-boundary assert и late TextChanged guard;
добавлены только safe boolean/count state, текст/credentials/journal/paths/argv не выводятся.

PASS local review/AST14+13 parity/reverse-diff scenario and helper preservation/syntax1/C#5/
diff/public321source/0release/secretPII0. Локально нет .NET Framework/PowerShell/Windows UI:
новая compilation/native owner-dispose/recovery11/full regression NOT_RUN, не PASS.
Публикация PASS: source `b69e7008bc889d88623669058e95197822b09f1e` в existing PR88
integration/approved-plan-31-20261011, fresh expected-head non-force guard на точном637.
Merge ref `bd8646ce3bb15b2aebeed68b2b6d5ffdd3aad015` пока не подтверждён checkout/build identity нового CI.
Freeze `bbf4d53850640efcd2b7877f1d8a0d58c7a07814`, tree `5641ec9975c979b371690ad1ee2b56156284acd3`;
fetched parent точный637/full tree equal freeze. Public diff4files588add/65delete:
1 test method17add/4delete +3 docs571add/61delete. Runtime/прочие tests/scripts/workflows/VERSION прежние.
Один automatic [CI638/39896043639](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39896043639),
pull_request/attempt1/raw exact-head runs total_count1; run in_progress/conclusion null,
updated_at2026-10-23T03:38:02Z. Windows120226632093/Ubuntu120226631811 in_progress.
Новая compilation/native/live-disposed draft regression/owner-dispose/recovery11 NOT_RUN.
Public PR uploads OFF/release/prune ineligible; completion steps ещё не проверены.
Post-publication docs-only checkpoint — локальный HEAD (git log -1), не push, чтобы не создавать
второй CI; full checkpoint также в PR88 body. STOP без полного ожидания/новой функции.
Следующий шаг — ТОТ ЖЕ CI638/exact tested merge: live draft boundary, late TextChanged/publication
counters,14 invariants, actual complete recovery11 и прежние регрессии. Старый637 FAIL сохранён.
Веса/620 anchors прежние: Реализация58–80/100 · Проверенная готовность22/100 · Изменение+0.
VPS/сеть/ключи/credentials/личный deployment/установка/релизы/бинарные uploads не затрагивались.

## История: разрешённая диагностика14 инвариантов CI636, Windows NOT_RUN

Владелец отдельно разрешил test-only instrumentation и один automatic diagnostic CI.
Проверены checkpoint `1c005ecd6772e8f2c1e7d5f95ed9e4410a6e6fc3`, public PR88 OPEN/DRAFT source
`cffa667469498a0245857f0406246c9da3638ddd` / tested636 merge `fa58ba1bcef1e0e8ef8e450f14f37751b7f5b85d`.
Другого доступного писателя/Git/build/lock операций нет; прежние worktree/checkpoint сохранены.
Отдельная ветка work/progo-ci636-invariants-20261023. CI636 completed FAILURE остаётся baseline,
не результатом нового кандидата. Его Instance30/receipt guards PASS; HTTPS recovery11 только5PASS/
1FAIL(owner-dispose)/5NOT_RUN. Поздние snapshot/dispatcher/ownership/cleanup/recover/fresh NOT_RUN.

Code `3ae2766fbe2d4436f192e705a17f27188a07db92`: только2 test files,58add/5delete.
HomeVpnShareRecoveryTests.OwnedHttpsRecovery заменяет один compound assert14 именованными
проверками тех же выражений в том же порядке и на той же phase7 boundary. False и исключение
оценки записываются как нарушение; aggregate assert требует0 нарушений. Остальные проверки,
сценарии, порядок фаз, physical joins, cleanup, реальные file/process operations и deadlines прежние.
После первого false собираются остальные нарушения, но сценарий не получает false PASS.
Draft predicate после Dispose оставлен буквально, гипотеза не исправляется и не обходится.
Вывод: имена/booleans; counts calls/expected/copies/verified/violations/publications; state
phase/task/Stop/DialogResult/IsDisposed/IsHandleCreated. Нет текста address, request IDs,
credentials/journal/private bytes, filenames/paths/argv, Exception.Message/Data/ToString.
Safe stack: только exception type, method identities и IL offsets, до modal rethrow и в
HomeVpnWizardTests.Main catch; stdout не даёт PowerShell Stop на stderr обрезать frames.
Nonzero exit и исходный aggregate FAIL сохранены, все нарушенные условия видны по именам.

PASS local: AST parity всех14 выражений/порядка, побайтовая проверка остальных scenario/cleanup/
deadline/method blocks с нормализацией LF/CRLF, syntax2/C#5 await-scope, diff/public321/0/secretPII0.
Первый локальный parity-script сравнил Wizard без LF/CRLF normalization; исправлена только
проверка, затем PASS. Это не C# compiler/native proof. Windows compilation/diagnostic native
NOT_RUN; owner-dispose/recovery11 не объявляются PASS. Runtime/прочие tests/scripts/workflows/
VERSION равны636. Scope2tests+3docs, новые функции/credentials/system/deployment actions отсутствуют.

Публикация PASS: source `e0a8185ae3deb66e14872735170b64d1f414acf6` в существующей PR88 ветке
integration/approved-plan-31-20261011; fresh expected-head non-force guard на точном636.
Merge ref `5aa6cbe43e07dc25d491b94a91f747b41f28e5aa` пока не подтверждён checkout/build identity нового CI.
Freeze `fe007d3b47d6b43db571ac63e44162c05fa8e02c`, tree `ba3c18754f4989129c4b5aea419cad27e83cc077`;
fetched parent точный636/full tree equal freeze. Public diff5files750add/60delete:
2 test files58add/5delete +3 docs692add/55delete, runtime/other tests/scripts/workflows/VERSION прежние.
Один automatic diagnostic [CI637/39892894242](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39892894242),
pull_request/attempt1/raw exact-head runs total_count1. Run in_progress/conclusion null,
updated_at2026-10-23T03:03:05Z; Windows120216525306/Ubuntu120216524993 in_progress.
New Windows compilation/14-invariant diagnostic/native/recovery11 NOT_RUN. Owner-dispose не PASS.
Workflow public PR uploads OFF, release/prune ineligible; completion upload/prune ещё не проверены.
Post-publication docs-only checkpoint — локальный HEAD (git log -1), не push, чтобы не создавать
второй CI; точный checkpoint также в PR88 body. STOP без полного ожидания/второй функции.
Следующий этап — финальный log ТОГО ЖЕ CI637/exact tested merge: прочитать14 outcomes/all violations/
count/state/safe stack, установить failed invariant и только затем классифицировать runtime/test/
infrastructure причину. Старый636 FAIL сохранён; пока причина BLOCKED и runtime не меняется.
Реализация58–80/100 · Проверенная готовность22/100 · Изменение+0, веса/620 anchors прежние.

## История: CI636 relay-step diagnosis, причина BLOCKED

Восстановлено из `d136325336c9ff43bfc2e94f91c3ce2ede37778b`; HEAD/PR88 сверены самостоятельно.
PR88 OPEN/DRAFT, source `cffa667469498a0245857f0406246c9da3638ddd`, exact checkout и build identity
`0.2.2+fa58ba1bcef1e0e8ef8e450f14f37751b7f5b85d` подтверждены финальным Windows log.
Доступные worktree чистые до начала; root — единственный агент; Git/build/lock операций
не найдено. Inventory codex PID относится к вызову текущего read-only инструмента.
Прежние ветки и checkpoint сохранены, отдельная docs-only ветка work/progo-ci636-relay-diagnosis-20261023.
Runtime/tests/scripts/workflows/VERSION побайтово равны опубликованному source636.

[CI636/39888080167](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39888080167) completed FAILURE,
updated_at2026-10-23T02:36:06Z. Самостоятельно получены и исследованы оба полных финальных job logs:
Windows120200902870 —11006 строк, Ubuntu120200903120 —578 строк. Repository hygiene and build SUCCESS,
поздний IKEv2 relay integration tests on .NET Framework FAILURE. Pack/uploads/prune SKIPPED, артефактов нет.

Подтверждённые узкие результаты точного636, не общий SUCCESS:

| Проверка | Статус | Доказательство |
| --- | --- | --- |
| Test-ProGo.ps1 | PASS | Финальный marker02:31:31UTC; Desktop5294, Instance30, Maintenance126, Shutdown47, UpdaterPackage5 |
| CI635 fixture absence fix | PASS | Instance30 и real missing/disappearing receipt + malformed/settings/foreign I/O guards02:28:19–26UTC |
| Export modal11 / profile chooser3 | PASS | Все11 protected-settings assertions и cancel/missing/error chooser markers; diagnostic chooser3 тоже PASS |
| Repair8 / read-only list10 | PASS | По8/10 финальных owned production slice PASS markers |
| HTTPS recovery11 | PARTIAL |5 PASS: default-refusal, status-pending, cancel-retry, stop-copy, dispose-copy; owner-dispose FAIL;5 остальных NOT_RUN |
| Snapshot12/dispatcher5/ownership8/cleanup4/recover10/fresh10 | NOT_RUN | Вызов HomeVpnSetupRecoveryTests.Run находится после упавшего HomeVpnShareRecoveryTests.Run; прежние PASS остаются историей |
| Ubuntu | PASS | Полный log: operation16 и QR13 OK, isolated provisioning/revocation/HTTPS/restart markers PASS |
| Portable archive/Python relay transport/full Home VPN | NOT_RUN | Harness завершился до этих вызовов; название failing step не доказывает сбой UDP relay |

Fatal02:36:01.6054070UTC: `Home VPN test failed: Exception: owned HTTPS preserves journal draft credentials
exact queries and unrelated resources without share mutation or replay: owner-dispose`.
Assert — tests/HomeVpnShareRecoveryTests.cs:243–246, OwnedHttpsRecovery; наблюдаемый wrapper stack
scripts/Test-HomeVpnRelay.ps1:19 char32. C# stack отсутствует: HomeVpnWizardTests.Main190 печатает только
тип/message. Source trace (вывод из кода, не stack из лога): Main91 → ShareRecoveryTests.Run112 →
OwnedHttpsRecovery → ModalWizardCase timer → compound check243–246.
До него PASS: typed cancellation, physical Stop/body/lease/cleanup, no late dispatcher publication,
no later verification/save/consumption. Новый FAIL не повторяет compilation или Instance defect.

Корневая причина НЕ ДОКАЗАНА:14 boolean predicates объединены одной меткой. Нет значений,
позволяющих отличить query/journal/private bytes/draft/DialogResult/lease/service work/directory/
sibling/relay/unrelated process/owned PID/child. Test assertion failure подтверждён;
runtime defect, fixture defect и infrastructure fault по этому логу не разделяются однозначно.
Гипотеза: address.Text читается после form.Dispose на200; проверка живого draft после уничтожения
TextBox может быть некорректна. Это не установлено на точном .NET Framework и не объявляется причиной;
условие не удалено, не заменено на closing||PASS, assert/coverage/timeouts не изменены.

Сохранены подробные timestamp markers и14 predicates в JSON ci636_relay_diagnosis.
Изменены только3 документа, код и тесты НЕ изменены. Новый CI/rerun/push/PR mutation НЕ выполнялись:
владелец разрешил новую публикацию только при реальном исправлении, доказанного исправления пока нет.
Локально .NET Framework/PowerShell/Windows UI недоступны. Минимальный следующий отдельный этап:
сохранить все14 исходных invariants и deadlines, получить раздельные обезличенные boolean outcomes
и полный C# exception stack в том же owner-dispose на Windows .NET Framework. Для отдельного
diagnostic CI нужно отдельное разрешение; текущую границу не обходим. После доказательства —
исправить только выявленный контракт. Не запускать другую функцию/слепой повтор.
Локальный docs-only checkpoint — HEAD через git log -1; source636/прежний checkpoint сохранены.
Реализация58–80/100 · Проверенная готовность22/100 · Изменение+0: веса/620 anchors неизменны,
узкий Instance PASS не закрывает новый полный взвешенный критерий/приёмку. VPS/сеть/ключи/
credentials/личный deployment/установка/релизы/бинарные uploads не затрагивались.

## История: fixture baseline failure CI635

Checkpoint `3768dc1ff4f9dd346234f188bf4dc4e45ae38f12`/локальный HEAD сверены. PR88 OPEN/DRAFT на source
`18585ce3df454d3280048db4b62fc7dfc1777d19`, tested merge `ceb83663d58a7158227001868d013723bed70225`. Другого доступного
писателя/Git/build/lock операций нет, worktree чистые до начала; временные inventory PID
завершились без принудительной остановки. Отдельная ветка work/progo-ci635-baseline-readiness-20261023,
worktree ProGo-ci635-baseline-readiness, прежние изменения/ветки сохранены.

[CI635/39884480364](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39884480364) completed
FAILURE, final Windows120189329732 log прочитан самостоятельно. Runtime compilation PASS:
build identity0.2.2+ceb83663d58a7158227001868d013723bed70225, прежние CS1501/CS1660 отсутствуют, Desktop5294 PASS;
InstanceHarness компилировался и начал native сценарии. Старый compilation defect исправлен.
Новый fatal01:50:23UTC — InstanceTests.exe FileNotFoundException: proxy-environment-backup.json.
Observed stack: Test-ProGo.ps1:649, NativeCommandError; C# frames в Actions log отсутствуют.
Последний PASS — clean owner disposal releases mutex; actual repeated primary launch PASS
ещё отсутствует. Source inference: Main Wait253–259 → TryCaptureProxyBaseline read97 или
verification107 → ReadProxySnapshot FileStream85. Какое из97/107 сработало, лог не различает;
это не выдаётся за прочитанный C# stack. До HTTPS recovery11 не дошло:0/11, все NOT_RUN,
нет PASS markers, HomeVpnRelay/later checks/pack/uploads/prune SKIPPED. Ubuntu120189329558
SUCCESS metadata only в этом fix этапе, не полная проверка его строк.

Подтверждён дефект sampler: AppliedPort и receipt reads не являются атомарным readiness,
а отсутствие собственного receipt немедленно обрывает existing bounded Wait. Точная
producer timing/transience в том CI не доказана; production defect/исчезновение по вине
нового HTTPS кода не утверждаются. Code `37a1c38f3d1e0c292a5fe61c281322fa0a4add44` меняет только InstanceTests.cs:
38add/1delete +3 docs. FileNotFound только для exact backupPath на receipt/verification
стадиях даёт false/null outputs/code0, никогда PASS. Wait10s/30ms polling неизменён;
постоянное отсутствие обязано завершиться прежним timeout FAIL. Missing settings, foreign
FileNotFound, malformed receipt и прочие I/O остаются fatal, lock32/33 contract прежний.

Регрессии на реальных own temporary files: отсутствует до чтения; удалён между чтением
и verification через optional fixture-only hook; несколько absent probes не готовы;
после публикации matching receipt захватываются точные обе копии. Negative guards:
malformed receipt/missing settings/foreign filename не проглатываются. Прежние wrong-port/
pending-correction/native-lock/atomic replacement asserts и полный Main не изменены.
Finally удаляет только свой временный root; новые тесты не меняют сеть/registry/credentials.
Windows compilation changed fixture/native regressions/recovery11/full regression NOT_RUN:
локально нет .NET Framework/PowerShell. Runtime tree идентичен успешно compiled635, это
не подтверждает изменённый fixture. PASS local review/syntax1/await-scope/diff/public321/0/
secretPII0/method preservation. Все runtime/другие tests/harness/scripts/workflows/VERSION,
signature fix/recovery11/ownership/cleanup/export UI/EOF сохранены. Assertions/deadlines/
coverage не отключены; sampler допускает только ожидание, не ложную готовность.

Публикация PASS: source `cffa667469498a0245857f0406246c9da3638ddd` в существующей PR88 ветке
integration/approved-plan-31-20261011; fresh expected-head non-force update проверен.
Merge ref `fa58ba1bcef1e0e8ef8e450f14f37751b7f5b85d` пока не подтверждён checkout/build identity нового CI.
Freeze `8e414b821b9328e2f16238010961079704dd2387`, tree `b4da8abfda0d27954c339addd5db33589e7def3d`;
fetched parent точный635/full tree equal freeze. Public diff4files355add/43delete:
один fixture38add/1delete +3 docs. Runtime/workflows/scripts/VERSION неизменны.
Один automatic [CI636/39888080167](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39888080167),
attempt1/raw exact-head runs total_count1; status in_progress/conclusion null,
run.updated_at2026-10-23T02:10:14Z. Windows120200902870 build in_progress;
Ubuntu120200903120 completed SUCCESS по jobs metadata, final log здесь не прочитан.
Новая fixture compilation/native/complete Instance/recovery11/full regression NOT_RUN.
Upload guards OFF/release excluded; completion upload/prune ещё не проверены.
Post-publication docs-only checkpoint — локальный HEAD (git log -1), не публикуется,
чтобы не создавать второй CI; точные source/freeze/checkpoint также в PR88 body.
STOP без полного ожидания/следующей функции. Следующий шаг — тот же CI636/exact tested
merge: native absence/fatal-guard regressions, complete Instance suite, actual recovery11
и прежние проверки. Если receipt не публикуется, сохранить timeout FAIL и исследовать
primary, не ослаблять ожидание/assert. CI635 failure и отсутствие его C# frames сохранены.
Uploads OFF/release excluded; main merge/релизы/бинарные uploads/установка/VPS/сеть/ключи/
credentials/личный deployment не выполнялись. Реализация58–80/100 · Проверенная готовность22/100
· Изменение+0: фиксированные веса/620 anchors прежние. Главный блокер — точный Windows proof
исправленного fixture и ещё не выполненные recovery11/fullR09/приёмка.

## История: этап только исправление compile failure CI634

Checkpoint `f959ebc0fcee4eadae30a4e000649b0879d90523`/локальный HEAD сверены. PR88 OPEN/DRAFT на source
`763c730dc5e4d505fac35d592024911d744c2f91`, checkout merge `5a0f6d56cf62b7c06f0e6ab862094519e47c239a`.
Все доступные worktree чистые; другого доступного исполнителя/Git/build/lock операций нет.
Временные PID read-only inventory завершились сами, процессы не завершались принудительно.
Сохранена отдельная ветка work/progo-ci634-wait-signature-20261023/worktree ProGo-ci634-wait-signature.

[CI634/39883511908](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39883511908): final
Windows120186199123 log прочитан самостоятельно. Repository hygiene and build FAILURE:
HomeVpnService.cs(1166,36) CS1501: No overload for method WaitForShareAsync takes4 arguments;
(1165,25) CS1660: Cannot convert lambda expression to type string because it is not a delegate type.
Причина — RecoverShareOwnedAsync пропустил обязательный timeoutMs. Объявление WaitForShareAsync
требует5 аргументов. Это compile defect предыдущего изменения, не infrastructure flake.
Exact checkout merge подтверждён; build identity/native tests не выполнены. Recovery11
NOT_RUN, не PASS. Поздние Windows checks/pack/uploads/prune SKIPPED. Ubuntu120186198956
completed SUCCESS проверен через jobs metadata; финальные строки его логов в этом fix этапе
не перечитывались и не выдаются за полную проверку. Overall634 completed FAILURE.

Code `e9b5f6c9de00549d304dbf0d63cb2549028e835b`: одна runtime строка передаёт пятый аргумент
HomeVpnPreparationProcess.TimeoutMs, прежние300000. Регрессия: существующий cancel-command
дополнительно проверяет фактический modal ShareCommand window/owner/CancelButton и незавершённый
physical native Task перед отменой. Старые typed cancellation/journal ID/cleanup asserts сохранены.
Только HomeVpnService/HomeVpnShareRecoveryTests:6add/2delete +3 docs. Другие функции остановлены;
timeout/overloads/workflows/checks/протоколы не менялись, rerun старого SHA не выполнялся.

PASS local: full diff/review, syntax2/C#5 await-scope, diff --check/public321/0/secretPII0.
Targeted AST arity check воспроизводит bad634 call1166/4, принимает fixed5 и отклоняет
removed-timeout mutation; declaration5 required/constant300000 unchanged. Это static,
не C# compilation.76/77 old service и38/39 old HTTPS test methods неизменны кроме точных
двух методов; каждый другой tracked blob равен634: configure/confirmation/fresh/recover/
list/repair/export UI/EOF/ownership/cleanup/snapshot/dispatcher/invitation/harness/scripts/
workflows/VERSION. Локально нет .NET Framework/PowerShell. Исправленная Windows compilation/
native recovery11/full regression NOT_RUN, старый633 PASS на этот код не переносится.

Публикация PASS: source `18585ce3df454d3280048db4b62fc7dfc1777d19` в существующей PR88 ветке
integration/approved-plan-31-20261011, actual merge ref `ceb83663d58a7158227001868d013723bed70225`.
Freeze `58c3da75984d49e9e0bd620e0aaf1c5ea6b8ba6f`, tree `e319a5161ed816dcb7e306e3b2603739329d95dd`;
fetched parent точный634/full tree equal freeze. Expected-head non-force update проверен.
Public diff5files403add/158delete:1 runtime line + existing regression +3 docs. PR OPEN/DRAFT,
main не изменён. CI634 повторно не запускался: есть конкретное signature исправление.

Единственный новый automatic [CI635/39884480364](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39884480364),
source `18585ce3df454d3280048db4b62fc7dfc1777d19`, attempt1/raw exact-head runs total_count1.
Последнее наблюдение run.updated_at2026-10-23T01:31:36Z in_progress/conclusion null;
Windows120189329732/Ubuntu120189329558 in_progress. Исправленная Windows compilation/
recovery11/full regression NOT_RUN, merge ref ещё не proof build identity635. Upload guards
OFF/release excluded; completion upload/prune пока не проверен. Старый634 compile FAIL
сохранён отдельно, recovery11 не PASS. Релиз/бинарные uploads/установка/VPS/сеть/ключи не выполнялись.

Post-publication docs-only checkpoint локальный HEAD этой ветки (git log -1), не push во
избежание второго CI. Published source contains frozen pre-publication docs, актуальные
source/run/checkpoint также в PR88 body. STOP без полного ожидания CI/другой функции.
Следующий шаг — финальные логи ЭТОГО ЖЕ39884480364/exact merge: compiler без CS1501/CS1660,
actual recovery11 и прежние регрессии. Main merge/release/credentials/личный deployment не выполнялись.
Реализация58–80/100 · Проверенная готовность22/100 · Изменение+0: фиксированные веса и620
anchors прежние. Главный блокер — compile/native proof исправленного кандидата, fullR09/
реальная приёмка остаются открыты.

## История: этап существующий HTTPS recover-share → owned worker на CI633

Checkpoint `65c0c497d72a3c110623c31b100f6351c0148326` прочитан, локальный HEAD ему соответствовал.
PR88 OPEN/DRAFT на source `3a212e47d18ad51a8037ed3944477dbba987a701`, tested merge `99c1737906e67ddaf8303856d24ee71dca7eea7d`.
[CI633/39879011054](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39879011054) проверен
самостоятельно по финальным логам обоих completed SUCCESS jobs Windows120171576507/
Ubuntu120171576142, не только переданным metadata. Checkout/build identity0.2.2+99c1737906e67ddaf8303856d24ee71dca7eea7d.
Fresh10 default-refusal/copy-fault/cancel-retry/stop-copy/dispose-copy/owner-dispose/
cancel-command/cleanup-lock/result/pending-race — explicit PASS. Recovery10/list10/repair8/
ownership8/cleanup4/snapshot12/dispatcher5/export modal11/chooser3 — PASS; Desktop5294/
HomeVPN2674/Portable91, ProGo tests PASS. Windows Python26 PASS/2 SKIP, SKIP не PASS;
Ubuntu16+13 unit OK/isolated actual VPN/HTTPS/invitation smoke PASS. Pack SUCCESS только
runner, uploads/prune SKIPPED, artifacts API empty. ZIP digest в JSON, байты не получены/
не переданы/не установлены. Этот SUCCESS не подтверждает новый HTTPS recovery код.
Все доступные worktree чистые, незавершённых Git/build/lock операций и другого доступного
исполнителя нет. Временные codex/bash/python PID первого read-only inventory завершились;
они не завершались принудительно. Старые ветки/checkpoint сохранены, отдельный worktree.

Выбран один небольшой production путь: существующее «Проверить прежнюю настройку HTTPS».
Причина: core share receipt/owned confirmation/dispatcher уже есть, SSH preparation ещё
legacy; его можно перенести отдельно от fresh share и invitation. Code `4f10ec66e08359dd75fe3e85e06f0bed8a279482`,
ветка work/progo-https-recovery-on633-20261023 / worktree ProGo-https-recovery-on633 от633.
Только HomeVpnService/HomeProfileShare/HomeVpnShareRecoveryTests:197add/3delete +3 docs.

Default configure factory направляет только recover-share через RecoverShareOwnedAsync,
с фактическим configure owner/dispatcher. Owner metadata копируются до scheduling;
selected-key validation/DPAPI Load/helpers/status/result/cleanup принадлежат retained worker.
Single share family lease остаётся в прежнем core до результата и cleanup; wrapper не
захватывает его повторно и не ждёт public Stop. Каждый следующий native startup проверяет
Stop identity, принятый физический Task дренируется до исхода/локальной отмены. Точный
cleanup retained до explicit retry, поздний progress завершённого тела подавлен.
Только mode2 recovery проверяет Stop scope после SSH и при существующих admission gates
verification/save/consumption. Принятый успешный SSH Task остаётся успешным, сменившийся
Stop запрещает последующую HTTPS-проверку/запись/снятие запроса. Fresh share и invitation,
публичный продуктовый scope и веса не меняются; протокол исходного ID/status/result прежний.

Новые HTTPS recovery11: default-refusal/status-pending/cancel-retry/stop-copy/dispose-copy/
owner-dispose/cancel-command/cleanup-lock/result/stop-result-copy/stop-result. Фактические
default/modal кнопка, DPAPI/core lease/helper/sharing/native subprocess tree; заменены
только copy transport/SSH executable и внешний HTTPS verification boundary. Fixture
проверяет исходный ID/отсутствие token в argv/отсутствие новой share команды. UI heartbeat/
single admission, Cancel→Retry без закрытия ShowDialog/замены старого Task, Stop/Dispose/
owner loss, отказ result после второго copy, locked Stop false→unlock true без replay,
typed original error, journal/draft/credentials/sibling/чужой процесс сохранены.
Positive native status/result возвращает исходный origin; test verification явно отменяется
до HTTPS/записи. Stop после принятого SSH success оставляет результат worker и не вызывает
даже test verification/save/consumption. Это не настоящая интеграция VPS/HTTPS/приёмка ПК.
Bounded held5s/modal15s/finally join5s, native300s unchanged. Finally release/unlock/dispose
own windows, join Work/initial/pending/operation/Stop, cleanup только своих fixture pid/child.
Существующие test fixture/harness/scripts/timeout policy не менялись, вызов группы до
прежних final cleanup asserts; Windows compilation/native11/full regression NOT_RUN.

PASS local: full manual diff/review, syntax3/C#5 await-scope (не compilation), diff --check,
public321/0/added secretPII0. Все74 baseline service methods неизменны;20/22 Share methods
кроме default factory/common configure recovery scope и33/34 old HTTPS test methods кроме
Run неизменны. Все другие tracked blobs равны633: fresh/recover/list/repair/ownership/
cleanup/export UI/EOF/BOM/snapshot/dispatcher/receipt/HTTPS verification+confirmation/
invitation/harness/scripts/workflows/VERSION. Локально нет PowerShell/.NET Framework.
FullR09, fresh share/остальные invitation routes и реальная приёмка открыты.

Публикация PASS: source `763c730dc5e4d505fac35d592024911d744c2f91` в существующей PR88 ветке
integration/approved-plan-31-20261011, actual merge ref `5a0f6d56cf62b7c06f0e6ab862094519e47c239a`.
Freeze `52f72318de967f0e83c26675f7478e698ac8cdcb`, tree `b8755d64da13dfab57f19fdaa9f737ad7f116d0d`;
fetched parent точный633, public full tree равен freeze. Expected-head non-force update
после fresh HEAD/diff/secret/public/workflow проверок. Public diff6files791add/63delete:
2 runtime +1 existing test +3 docs. PR88 OPEN/DRAFT, main не изменён.

Единственный automatic [CI634/39883511908](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39883511908),
source `763c730dc5e4d505fac35d592024911d744c2f91`, attempt1/raw exact-head runs total_count1.
Последнее наблюдение run.updated_at2026-10-23T01:21:24Z: in_progress/conclusion null;
Windows120186199123/Ubuntu120186198956 in_progress. Новые Windows compilation/native11/
точная полная регрессия NOT_RUN до completed финальных логов; merge ref ещё не proof
build identity634. Upload guards OFF/release excluded, completion uploads/prune ещё не
проверен. Релиз/бинарные uploads/установка/VPS/credentials/network не выполнялись.

Post-publication docs-only checkpoint только локальный HEAD этой ветки (git log -1),
не push во избежание второго CI; опубликованный source содержит frozen pre-publication
docs, актуальные source/run/checkpoint также в PR88 body. STOP без ожидания/второго этапа.
Следующая точка — финальные логи ЭТОГО ЖЕ39883511908/exact tested merge: HTTPS recovery11
+ fresh10/recovery10/list10/repair8/ownership8/cleanup4/API17/export14/full regression;
после proof отдельный fresh-share owned preparation срез, сейчас не начат.
Main merge/release/бинарные uploads/установка/VPS/credentials/network/личный deployment не выполнялись.
Реализация58–80/100 · Проверенная готовность22/100 · Изменение+0: полный взвешенный критерий
не закрыт; фиксированные веса/620 anchors прежние. Главный блокер — Windows proof нового11,
remaining Admin/fullR09/owner acceptance открыты.

## История: этап fresh setup preparation → owned worker на CI632

Checkpoint `c1e8b509eb0dff03175a737dce806472bffb53b0` прочитан, локальный HEAD ему соответствовал.
Актуальный PR88 OPEN/DRAFT на source `5d5fed17998f77c1e1225127501f3f32ec4c0557`, tested merge `e52e4dfb1702d09ce7d8ec3a123bafc70d4ef370`.
[CI632/39873394320](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39873394320)
проверен самостоятельно по финальным логам обоих completed SUCCESS jobs Windows120153006376/
Ubuntu120153006482, не только metadata владельца. Checkout/build identity0.2.2+e52e4dfb1702d09ce7d8ec3a123bafc70d4ef370.
Recovery10 default-refusal/status-pending/cancel-retry/stop-copy/dispose-copy/owner-dispose/
stop-between/cancel-command/cleanup-lock/result — все explicit PASS. Прежние list10,
repair8, ownership8, cleanup4, snapshot12/dispatcher5, export modal11/chooser3 PASS;
Desktop5294/HomeVPN2569/Portable91, ProGo tests PASS. Windows Python26 PASS/2 SKIP;
SKIP не PASS. Ubuntu16+13 unit OK/isolated actual VPN/HTTPS/invitation smoke PASS.
Pack SUCCESS только runner; uploads/prune SKIPPED, artifacts API empty. ZIP digest в JSON,
байты не получены/не переданы/не установлены. Этот SUCCESS не подтверждает новый fresh код.
Все доступные worktree чисты, незавершённых Git/build операций/lock и другого доступного
исполнителя не обнаружено. Существующие ветки/checkpoint сохранены, работа в отдельном worktree.

Один ограниченный production срез: существующее «Настроить VPS и продолжить» вызывает
PrepareSetup → SetupOwnedAsync. Code `ad66afc42d36b240671187004a0046b09b55bb1f`, ветка
work/progo-fresh-setup-on632-20261023 / worktree ProGo-fresh-setup-on632 от точного632.
Только HomeVpnService/HomeVpnWizardForm/HomeVpnSetupRecoveryTests:230add/1delete +3 docs.
Все зависимости уже в632; новые функции/HTTPS/invitation маршруты не добавляются.

Owner metadata копируются до scheduling; selected-key validation, DPAPI Load/Register,
helper/output/RetainResult/cleanup выполняет retained worker. Copy и native setup/status
окна запускаются через dispatcher мастера без ActiveForm. Существующий core один раз
владеет family lease до результата/cleanup; wrapper не захватывает его повторно и не ждёт
public Stop. Принятый copy дренируется, затем scope recheck при Stop/Dispose запрещает
Register и SSH, не создавая новый ID после остановки. Уже зарегистрированный/принятый
command сохраняет исходный ID для read-only recovery, без автоматического replay.
Если запрос появился между чтением мастера и core Load, прежний core использует status/
result исходного ID; recovery purpose copy определяется на worker под тем же lease.
Поздний progress завершённого тела подавлен, точный cleanup retained до explicit retry.

Новые fresh10: default-refusal/copy-fault/cancel-retry/stop-copy/dispose-copy/owner-dispose/
cancel-command/cleanup-lock/result/pending-race. Фактическая default/modal кнопка/core,
DPAPI journal/helpers/sharing/native subprocess tree; заменены только copy transport и
SSH executable, никакого VPS. До окончания copy journal отсутствует, Cancel→Retry не
создаёт ложный pending ID, старый Task не заменяет успех явного retry. Native fixture
читает настоящий защищённый журнал до «SSH», проверяет ID/отсутствие token в argv и точное
число действий. Cancel после регистрации оставляет journal и recovery-only кнопку.
Stop/Dispose/owner loss не запускает следующий transport, UI heartbeat/single admission,
locked Stop false→unlock true без replay, исходный typed error/credentials/sibling/чужой
процесс проверяются. Positive output RetainResult сохраняет исходный ID; fixture отменяет
local import до credentials write/туннеля, не имитирует приёмку владельца.
Bounded held5s/modal15s/child-start5s/own process-exit2s/finally join5s; native300s unchanged.
Finally release/dispose own windows/processes и join operation/import/initial/snapshot/read/
consumption/driver/Stop. Группа перед terminal NativeRecovery, harness/scripts не менялись.

PASS local: полный manual diff/review, syntax3/C#5 await-scope (не compilation), diff --check,
public321/0, added secretPII0. Все71 прежних service и41 wizard method bodies неизменны;
30/32 old setup-test methods кроме Fixture/Run неизменны, остальные tracked blobs равны632:
recovery/list/repair/export UI/EOF/BOM/snapshot/dispatcher/ownership/cleanup/core receipt/
HTTPS/invitation/harness/scripts/workflows/VERSION. Проверка method preservation сначала
не совпала из-за CRLF/LF в ключах сравнения; после нормализации обоих входов PASS, методы
не менялись. Windows compilation/native fresh10/full regression NOT_RUN: Linux без .NET
Framework/PowerShell. Full R09, остальные Admin-маршруты и реальная приёмка открыты.

Публикация PASS: source `3a212e47d18ad51a8037ed3944477dbba987a701` в существующей PR88 ветке
integration/approved-plan-31-20261011, actual merge ref `99c1737906e67ddaf8303856d24ee71dca7eea7d`.
Freeze `c482b97121d840ebe8d14e7d889ea9597c84f873`, tree `3a8a19954bb1b76431c8977015568e2d508717c5`;
fetched parent точный632, full tree равен freeze. Expected-head non-force ref update
выполнен после fresh HEAD/diff/secret/public/workflow проверок. Public diff6 файлов:
2 runtime +1 existing test +3 docs,806add/68delete. PR88 OPEN/DRAFT, main не изменён.

Единственный automatic [CI633/39879011054](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39879011054),
source `3a212e47d18ad51a8037ed3944477dbba987a701`, attempt1/raw exact-head runs total_count1.
Последнее наблюдение run.updated_at2026-10-23T00:35:26Z: in_progress/conclusion null;
Windows120171576507 и Ubuntu120171576142 in_progress. Новые Windows compilation/fresh10/
точная полная регрессия NOT_RUN до completed логов; merge ref ещё не доказанный build
identity633. Public upload guards OFF/release excluded, completion uploads/prune пока
не проверен. Релиз/бинарные uploads/установка/VPS/credentials/network не выполнялись.

Post-publication docs-only checkpoint только локальный HEAD этой ветки (git log -1),
не push во избежание второго CI; опубликованный source содержит frozen pre-publication
docs, актуальные publication IDs/checkpoint также в PR88 body. STOP без ожидания CI и
без второго этапа. Следующая точка — финальные логи ЭТОГО ЖЕ39879011054 и exact tested merge:
fresh10 + recovery10/list10/repair8/ownership8/cleanup4/API17/export14/full regression.
Main merge/release/бинарники/установка/VPS/credentials/network и личный deployment не выполнялись.
Реализация58–80/100 · Проверенная готовность22/100 · Изменение+0: фиксированные веса и620
anchors прежние, полный взвешенный критерий не закрыт. Главный блокер — Windows proof
нового fresh10; remaining Admin/full R09/owner acceptance остаются открыты.

## История: этап существующий recover-setup → owned worker на CI631

Checkpoint `8a89037677013c1e7e12156a73797800eccb1e19` прочитан. При начале этапа актуальный PR88
OPEN/DRAFT был на source `2882b1f104bbfe469be4b51a615c2fe72f43855c`, tested merge `40dff9acb6c62600ee7495bb06ad76a5fb7332bc`.
[CI631/39868272469](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39868272469)
проверен независимо: получены финальные логи обоих jobs Windows120135894964/Ubuntu120135895252,
а не только переданные владельцем metadata SUCCESS. Build identity0.2.2+40dff9acb6c62600ee7495bb06ad76a5fb7332bc.
Явные mode PASS: list10, repair8, ownership8, cleanup4, snapshot12/dispatcher5, export
modal11/chooser3. ProGo tests PASS; Desktop5294/HomeVPN2465/Portable91. Windows Python28:
26 PASS/2 SKIP, SKIP не PASS. Ubuntu16+13 unit tests OK и isolated native smoke PASS.
Все uploads/prune SKIPPED, артефактов нет. Новый код этим SUCCESS не подтверждается.
Git/worktree чистые, незавершённых Git/build операций и другого доступного исполнителя нет.

Выбран один небольшой production срез подготовки setup/recover-setup: существующая кнопка
«Проверить прошлую настройку», только recover-setup. Code `4976a5bf42ee98d7b986c5ec06c75543ecf9edf6`,
ветка work/progo-setup-recovery-on631-20261023, worktree ProGo-setup-recovery-on631 от точного631.
HomeVpnService/HomeVpnWizardForm/HomeVpnSetupRecoveryTests/HomeVpnWizardTests:228add/2delete.
Fresh setup, HTTPS и invitation mutation/recovery остаются отдельными этапами.

Default recover delegate вызывает RecoverSetupOwnedAsync. SSH metadata копируются до
scheduling; File.Exists выбранного ключа, DPAPI Load журнала, helpers/output/RetainResult
и exact cleanup принадлежат retained worker. Native recovery copy/status/result окна
запускаются через dispatcher конкретного мастера, без ActiveForm. Существующий core
удерживает свой family lease от Load до результата/cleanup; wrapper не захватывает его
повторно и не ждёт public Stop, владеющий его drain. Stop/Dispose до следующего transport
отказывает в его запуске, уже принятый actual Task дренируется до исхода/локальной отмены.
Callback сохраняет exact cleanup, поздний progress завершённого тела подавляется.
Исходный протокол request ID/status/result/RetainResult и дальнейшая import/consumption
логика не меняются. Новую настройку/выдачу recovery не вызывает и не повторяет автоматически.

Новые10 режимов: default-refusal/status-pending/cancel-retry/stop-copy/dispose-copy/
owner-dispose/stop-between/cancel-command/cleanup-lock/result. Реальная production привязка
и modal кнопка, реальный core/DPAPI/helper read/sharing/native subprocess tree; изолированы
только copy transport и SSH executable. Fixture пропускает только status/result с исходным ID,
отклоняет fresh setup и передачу token в аргументах. Проверяются UI heartbeat/single admission,
удержание family lease, Cancel→Retry с неизменными journal/ID и честным running outcome,
Stop между успешным status и result (result не запускается), Dispose/owner loss, native cancel,
locked false→unlock true без replay, typed fault, protected credentials/sibling/unrelated process.
Positive result проверяет два реальных native вывода и сохранение прежнего token в журнале;
fixture отменяет локальный import до записи credentials/запуска туннеля, не подменяет приёмку ПК.
Finally release/dispose own windows/processes и join operation/import/initial/snapshot/read/
consumption/driver/Stop. Held5s/modal15s/cleanup5s/fixture-child5s; исходный native300s timeout
не меняется, modal watchdog закрывает собственные waiting forms/cancel processes. Группа до
terminal NativeRecovery; существующий harness уже компилирует файл, Main добавляет только
маршрутизацию нового native fixture. Windows compilation/native10 NOT_RUN.

PASS локально: full manual review/diff, syntax4/C#5 await-scope (не compilation), diff --check,
public321/0, added secretPII0. Первичный email-pattern hit оказался synthetic SSH target
документационного домена; проверен как не личные данные, сравнение выражено через fixture
metadata, scanner не ослаблен, повторная проверка PASS0. Все68 service и41 wizard methods
сохранены;26 old setup-test methods кроме Run и5 wizard-harness methods кроме Main неизменны.
Другие tracked blobs равны631: repair/list/export UI methods/EOF/BOM/snapshot/dispatcher/
ownership/cleanup/setup core/receipt/HTTPS/invitation/scripts/workflows/VERSION. Локально нет
PowerShell/.NET Framework. Общий R09, fresh setup/другие Admin routes/installer/owner acceptance открыты.

Публикация PASS: source `5d5fed17998f77c1e1225127501f3f32ec4c0557` в существующей ветке PR88
integration/approved-plan-31-20261011, actual merge ref `e52e4dfb1702d09ce7d8ec3a123bafc70d4ef370`.
Freeze `30c8fa142af5a4b8bf794348a680489e33a2b09d`, tree `21bf1d85c7fce56a17a32926f0925665df8c77f9`;
parent точный631, fetched public tree полностью равен freeze. Guarded non-force ref update
выполнен после fresh HEAD/diff/secret/public/workflow проверки. Public diff7 файлов:
4 runtime/test/harness +3 docs,581add/35delete. PR OPEN/DRAFT, main не изменён.

Единственный automatic [CI632/39873394320](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39873394320),
source `5d5fed17998f77c1e1225127501f3f32ec4c0557`, attempt1, raw runs total_count1.
Последнее наблюдение run.updated_at2026-10-22T23:41:06Z: in_progress/conclusion null;
Windows120153006376 и Ubuntu120153006482 in_progress. Новые Windows compilation/native10
и точная полная регрессия NOT_RUN до completed логов. Merge ref выше пока не означает
доказанный build identity632. Workflow uploads OFF/release excluded, исходы upload/prune
ещё не проверены. Бинарники/релиз/установка/VPS/credentials/network не выполнялись.

Post-publication checkpoint только локальный docs-only HEAD этой ветки (git log -1),
не опубликован во избежание второго CI; опубликованный source содержит frozen pre-publication
docs, актуальные publication IDs также в PR88 body. STOP без ожидания всего CI/второго этапа.
Следующая проверка — тот же39873394320: recovery10 + list10/repair8/ownership8/cleanup4/API17/
export14/full regression и exact tested merge. После proof отдельный небольшой fresh-setup
owned preparation срез; он сейчас не начат.
Реализация58–80/100 · Проверенная готовность22/100 · Изменение+0: полный взвешенный критерий
не закрыт, веса/методика/620 anchors прежние. Главный блокер — Windows proof нового recovery10;
fresh setup/остальные Admin-маршруты и реальная приёмка остаются открыты.

## История: этап read-only Admin list → owned worker на CI630

Checkpoint `4ba3ade1699c50bcabdd5c5e0d9f356a2138982f` прочитан. При начале этапа актуальный PR88
OPEN/DRAFT был на source `e8588af677b15f6d72a9543f22658ca7ccdc06a9`, tested merge `75a0e468986846e836f1f430454bc705eaaca5c7`.
[CI630/39860593042](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39860593042)
независимо подтверждён SUCCESS через completed Windows120109980387/Ubuntu120109980505
и точный build identity. Repair8, ownership8, cleanup4, snapshot12/dispatcher5/export
modal11/chooser3, Desktop5294/HomeVPN2347/Portable91 и полная регрессия PASS. Все uploads/
prune SKIPPED, артефактов нет. Новый код этим SUCCESS не подтверждается. Git/worktree
чистые; активных Git/build операций и другого доступного исполнителя не обнаружено.

Один согласованный срез: существующий read-only list при «Доступ друзей…» и Refresh.
Code `f13e12ecfc44651c9de7943cc4cfc359b7bf18e7`, ветка work/progo-admin-list-on630-20261023,
worktree ProGo-admin-list-on630 от точного630. Только HomeVpnService/HomeVpnWizardForm/
HomeVpnPreparationTests:235 добавлений/2 удаления. Все зависимости уже есть в630:
ownership8/cleanup4, core list, dispatcher, list native waiting process/forms и harness.
Repair не повторяется; setup/HTTPS/выдача/отзыв/восстановление invitation не изменены.

Default list delegate использует ListAdminOwnedAsync. Owner копируется до scheduling,
File.Exists выбранного ключа, helpers/output и точный cleanup выполняются в retained worker.
Для первого списка UI owner — мастер; для существующего Refresh — именно окно списка,
с отдельным dispatcher на срок его ShowDialog и восстановлением scope в finally.
Нет Form.ActiveForm или подмены принятого физического Task. Stop/Dispose до следующего
transport отказывает в запуске; уже принятый SSH остаётся owned до исхода/локальной отмены.
Публичный Stop не ожидается внутри тела. Cancel/pre-worker admission refusal сохраняет
HomeVpnListCancelledException и не создаёт неопределённость мутации. Callback удерживает
точную очистку; старый queued progress не перезаписывает новый результат. Initial list
после закрытия мастера/смены Stop identity не открывает новое окно. При закрытии Friends
его dispatcher запрещает поздний transport/publication; данные мутаций не переписываются.

Новые10 native режимов: default-refusal/complete/cancel-retry/stop-copy/dispose-copy/
owner-dispose/stop-command/cleanup-lock/refresh-cancel-retry/refresh-dispose. Тесты используют
исходную production Admin list привязку и реальные кнопки/ShowDialog, включая открытие
Friends и его Refresh. Seam заменяет только copy transport/SSH executable вокруг того же
owned API/core: фактические helper files/read/sharing и native subprocess tree, без VPS.
Проверяются heartbeat/нет duplicate admission, typed Cancel→Retry, Stop/Dispose drain,
отказ следующего SSH, honest locked Stop false→unlock retry true, no replay, сохранность
protected bytes/чужого sibling/процесса и строк/selection/search/draft/старого mutation gate.
Refresh-dispose проверяет фактический child owner и отсутствие late dispatcher updates.
Default-refusal не заменяет production delegate и использует отсутствующий fixture key,
чтобы при ошибке legacy binding не запустить настоящий OpenSSH; paused admission также
проверяет read-only тип отказа. Ограничения: barrier5s/modal15s/cleanup5s/native20s, finally
release/close own windows/stop fixture/join operation+Stop. Existing harness уже включает
этот файл; группа вызывается до прежних list regression. Windows выполнение10 NOT_RUN.

PASS локально: полный manual review/diff, syntax3/C#5 await-scope (не компиляция),
diff --check/public content321/0/added secretPII0. Все65 baseline service methods (включая
repair) сохранены;40 wizard methods кроме ManageInvitations и36 prep-test methods кроме
одного вызова ListWaiting неизменны. Every other tracked blob равен630: repair8, API17,
ownership8/cleanup4, export14/методы UI экспорта, EOF/BOM, invitation mutation/recovery,
HTTPS, scripts/workflows/VERSION. Windows compilation/native10 NOT_RUN: локально нет
PowerShell/.NET Framework. Полный R09, прочие Admin routes/installer/owner acceptance открыты.

Публикация TODO: после финального secret/public/workflow scan один guarded update
существующей PR88 ветки создаёт один автоматический CI нового source. Public uploads OFF,
release workflow исключён; main merge/release/бинарники/установка/VPS/credentials/network
не выполняются. Личный внешний deployment/секреты не читаются и не изменяются. Перед
длительной операцией сохранена эта точка. После публикации записать source/merge/tree/run
и STOP без ожидания всего CI или второго этапа. Следующая проверка — тот же run list10
+ repair8/ownership8/cleanup4/API17/export14/full regression. После proof отдельно небольшой
setup/recover-setup snapshot/preparation worker срез; сейчас он не начинается.
Реализация58–80/100 · Проверенная готовность22/100 · Изменение+0: полный взвешенный критерий
не закрыт, веса/методика/620 anchors прежние. Главный блокер — Windows proof нового list10;
прочие Admin-маршруты и реальная приёмка остаются открыты.

## История: этап production Admin repair → owned worker на CI629

Прочитан checkpoint `51d79ff99e443c36a871314d733d5c142dbd4ef8`; при начале этапа актуальный PR88
OPEN/DRAFT был на source `2b10ed6df31e2f40711c4efd856f98c5c2420a05`, tested merge `15448208d8bcb6428c1fbc8d2538729d978be9d5`.
[CI629/39854845261](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39854845261)
независимо подтверждён SUCCESS через completed Windows120090256338/Ubuntu120090256606
и Windows build identity. Cleanup4 complete/list-lock/preparation-lock/dispose-lock,
ownership8, snapshot12/dispatcher5, export modal11/chooser3, Desktop5294/HomeVPN2263/
Portable91 и прежняя полная регрессия PASS. Все uploads/prune SKIPPED, артефактов нет.
Новый код этим SUCCESS не подтверждается; pending записи ниже являются историей.
Git/worktree чистые, незавершённых Git/build операций и другого исполнителя не обнаружено.

Выбран один реальный production срез вместо слишком большого всего Admin: существующая
кнопка «Исправить выход VPN в интернет». Код `92a6304e5c66db0007878266e5e1eb4e2f5e8b02`,
ветка work/progo-admin-repair-on629-20261023, worktree ProGo-admin-repair-on629 от точного629.
Только HomeVpnService/HomeVpnWizardForm/HomeVpnOwnerWaitingTests:203 добавления/1 удаление.
Dependencies уже в629: owned task8, exact cleanup4, существующий repair core, dispatcher,
нативные preparation forms/process и Windows harness. Receipt/HTTPS/invitation блоки не нужны.

Default Admin delegate мастера направляет только repair в RepairAdminOwnedAsync.
Копия SSH-метаданных делается до scheduling; File.Exists выбранного ключа, чтение helpers,
защищённые каталоги и output/cleanup остаются внутри retained worker. Запуск окна copy/SSH
переходит к dispatcher именно этого мастера, без Form.ActiveForm и утраты фактического Task.
Перед каждым transport повторяется admission: Stop/Dispose не допускает следующий запуск.
Отказ до admission сохраняет typed preparation cancellation; принятый native SSH остаётся
под управлением до исхода/явной локальной отмены. Stop не повторяет внешнюю команду и не
обещает её откат; worker не ждёт public Stop, владеющий его drain. Callback удерживает
точный cleanup; отложенный progress завершённой операции больше не заменяет новый результат.

Новые8 режимов: default-refusal/complete/cancel-retry/stop-copy/dispose-copy/owner-dispose/
stop-command/cleanup-lock. Default-refusal вызывает реальную исходную привязку кнопки с
pending cleanup и отсутствующим fixture key, исключая запуск настоящего OpenSSH при ошибке.
Остальные сохраняют ту же кнопку/owned API/реальный core и окна; заменены только copy
transport и SSH executable: реальные helper-файлы/барьер/sharing и native subprocess tree,
без VPS/личных credentials. ShowDialog, UI heartbeat/duplicate refusal, Cancel→Retry,
первоначальный typed outcome, Stop/Dispose drain, owner loss, locked false→unlock true,
сохранность protected bytes/чужого sibling/процесса и отсутствие replay проверяются тестами.
Ожидания ограничены: held copy5s, modal15s, fixture cleanup5s; native process20s.
Finally освобождает barrier/lock, закрывает только свои окна, останавливает fixture и
дожидается operation/Stop. Existing harness уже компилирует и вызывает этот файл;
новая группа идёт до прежних terminal native loops. Windows выполнение8 пока NOT_RUN.

PASS локально: полный manual diff/review, syntax3/C#5 await-scope (не компиляция),
diff --check, public content321/0, added secret/PII0. Existing Service62/Wizard41 methods
побайтно сохранены; old owner-test24 кроме явного добавления одного вызова Run неизменны.
Все остальные tracked blobs равны629: EOF/BOM, export14 и UI export methods, API17,
ownership8/cleanup4, invitation/HTTPS, scripts/workflows/VERSION. Новых функций/экранов нет.
Windows compilation/native8 NOT_RUN: PowerShell/.NET Framework отсутствуют в Linux.
Остальной Admin остаётся legacy; полный R09, установщик и owner acceptance не закрыты.

Публикация TODO: после финального secret/public/workflow scan один guarded update
существующей PR88 ветки, один автоматический CI нового source. Public uploads OFF,
release workflow не запускается; main merge/release/бинарники/установка/VPS/credentials/
сетевая конфигурация не выполняются. До длительной операции сохранена эта точка.
После публикации записать source/merge/tree/run и STOP, не ждать всего CI и не начинать
второй этап. Следующая проверка — тот же run repair8 + cleanup4/ownership8/API17/export14
и полная регрессия; затем отдельно небольшой read-only Admin list owned-worker срез.
Реализация58–80/100 · Проверенная готовность22/100 · Изменение+0: полный взвешенный критерий
не закрыт, методика/веса/620 anchors прежние. Главный блокер — Windows proof новой
production привязки; остальные Admin-маршруты и реальная приёмка остаются открыты.

## История: этап exact Admin cleanup4 на проверенном CI628

Точка восстановления `28d43e1f9076fd76403a7060f73e0b247c8d8646` прочитана; актуальный PR88 OPEN/DRAFT остаётся
на source `b2c79830d078497fadc284e96b02e6be815aa300`, tested merge `72d546f06ebc680029b202b75ba5489bcfcb7d51`.
[CI628/39849852240](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39849852240)
независимо проверен SUCCESS: Windows120072781434/Ubuntu120072782038; completed Windows
лог подтверждает Desktop5294/HomeVPN2241/Portable91, ownership8, snapshot12/dispatcher5,
export modal11/chooser3 и полную прежнюю регрессию. Все uploads/prune SKIPPED, артефактов нет.
Это доказательство точной базы; новый cleanup4 им не подтверждается. Старые in_progress
ниже — история на момент записи. Других активных Git/build операций и грязных worktree нет.

Один разрешённый блок: код `ef9bd8d7d730912874b2f32ed6589b8038f6e26d` из сохранённого
e1e793e0b0789e8d686454b052fd3b773623ed57, только src/HomeVpnService.cs и
tests/HomeVpnSetupRecoveryTests.cs,111 добавлений/9 удалений. Отдельный worktree
ProGo-admin-cleanup-on628, ветка work/progo-admin-cleanup-on628-20261023 от точного628.
Зависимости уже в базе: оба исходных blob равны родителю сохранённого блока;
существующий Windows relay harness компилирует и вызывает эти сценарии.

RetainAdminCleanup сохраняет только точное удаление созданной командой директории
до завершения её исходной typed ошибки. Stop сначала дожидается принятых тел,
затем повторяет удержанные удаления в worker. Заблокированный файл сохраняет owner
и false от Stop; после освобождения явный Stop завершает удаление. Новая factory
запрещена при pending cleanup; copy/SSH не повторяются, чужие директории не сканируются.
Создание work/upload охвачено общей completion/cleanup оболочкой. Исходный тип ошибки
остаётся на operation task. Тела не должны ожидать public Stop, владеющий их drain.

Четыре тестовых режима: complete/list-lock/preparation-lock/dispose-lock. Они вызывают
реальный Admin core с изолированным transport, реальные helper/result/sharing операции,
проверяют heartbeat, typed failure, false→unlock→retry true, отсутствие повторного copy/SSH,
сохранность защищённых bytes и чужой sibling. Ожидания3s/5s, finally release/drain,
исходный Pump12s сохранён; группа идёт перед terminal NativeRecovery.

PASS локально: полный ручной diff, diff --check, syntax2/await-scope (не компиляция),
source2 канонически равны сохранённому cleanup4; public content321/0, secret/PII0.
Остальные исходные blobs неизменны: UI экспорта и14 тестов, EOF/BOM, snapshot/dispatcher,
ownership8, invitation/HTTPS, scripts/workflows/VERSION. Windows compilation/native4
NOT_RUN: локальная Linux-среда не имеет PowerShell/.NET Framework. Production Admin UI
по-прежнему не передаёт callback; общий R09 открыт. HTTPS/UI/invitation не внедряются.

Публикация пока TODO: после финального scan один guarded PR88 fast-forward создаёт один
автоматический CI нового source; public uploads OFF, release workflow не запускается.
Ни merge/release, ни выгрузка бинарников/установщика, ни установка/VPS/credentials/network
не выполняются. Перед длительной операцией сохранена эта точка; после публикации записать
полные source/merge/tree/run и закончить этап без ожидания всего CI.

Следующий шаг этого этапа: публикация проверенного frozen tree и фиксация единственного CI.
Следующий отдельный этап: проверить тот же CI (cleanup4 + прежняя регрессия); после PASS
отдельно реализовать owned Admin core wrapper, без автоматического перехода сейчас.
Реализация58–80/100 · Проверенная готовность22/100 · Изменение+0: новый полный взвешенный
критерий не закрыт, веса/методика/owner acceptance прежние. Главный блокер: cleanup4
ещё не проверен Windows, production Admin worker binding и общая приёмка остаются открыты.

## История: подготовка этапа владение Admin-задачами и Stop/Dispose на проверенном CI627

Свежий head PR88 source `8637c5e7ffcb19e2ed0622d74532bd88e245f9a0`, merge
`8968a9b0506fb0157a1f130c4a8379a2b78e8820`; [CI627/39829750388](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39829750388)
независимо подтверждён SUCCESS: Windows120002424298/Ubuntu120002424507. Build identity
совпадает с merge; Desktop5294, snapshot12/dispatcher5, export modal11/chooser3 PASS.
Все новые результаты ниже относятся к своим SHA; старые pending записи сохраняют историю.

Выбран следующий минимальный этап по зависимостям: учёт каждой принятой Admin-задачи,
необходимый для последующего удержания cleanup и production worker. Код
`736740009c02fd88e65d4830f204166c953a471a`, ветка
`work/progo-admin-ownership-on627-20261022`, worktree `ProGo-admin-ownership-on627`.
Перенесён только сохранённый437b69e8037a9c33e0f581679f0736dfc1321f2c:2 исходных файла,
107 добавлений/3 удаления. RunAdminOwnedAsync резервирует публичную задачу до scheduling,
проверяет Stop/Dispose admission до factory и выполняет синхронную часть factory в worker.
Stop/Dispose получает snapshot всех принятых задач и удерживает drain до их завершения;
ошибка тела и исходный тип отмены остаются на соответствующей operation task.
HasOwnedResources учитывает pending Admin. Тело не должно ожидать публичный Stop,
который владеет его drain; это обязательный контракт будущего production wrapper.

Новые8 native случаев: complete/stop/dispose/lock/typed/queued-stop/queued-dispose/multiple.
Реальные файловые операции и sharing lock, UI heartbeat, bounded barriers3s/5s,
исходный Pump default12s; группы идут перед terminal NativeRecovery. Проверены зависимости
в точном627 и существующий Windows harness. Snapshot/dispatcher API17, исходное тело
AdminAsync, UI экспорта/тесты, полный EOF launcher/BOM, invitation/HTTPS, workflows и
VERSION сохранены побайтно. Cleanup retention, AdminOwnedAsync и production route migration
не входят в этот этап. Поэтому общий R09 остаётся открытым.

Локальные PASS: полный ручной diff, syntax/await-scope2 (не компиляция), canonical source2
равно сохранённому437b69e, diff --check, public content321/0, добавленные secret/PII0.
Новые Windows compilation/native8 NOT_RUN: PowerShell/.NET отсутствуют в Linux-среде.
CI627 SUCCESS не переносится на новый код. Методика/веса и баллы58–80/22(raw21.75)
не изменяются; новый полный критерий проекта не закрыт.

Внешняя личная проверка: владелец сообщил об успешной работе отдельного MTProto-сервера
с iPhone в мобильной сети без VPN (сообщения/медиа) и включении автозапуска службы.
Это не интегрированная функция ProGo и не приёмка его телефонного VPN-маршрута.
Wi-Fi, перезагрузка сервера и длительная проверка отдельно не подтверждены. Адреса,
домены, пути и секреты внешнего deployment здесь не сохраняются; к VPS агент не подключался.
Баллы ProGo за эту отдельную проверку не начисляются.

Далее только в этом этапе: source-only tree2+3docs, свежие public/PR/workflow guards,
guarded fast-forward существующей ветки PR88 и один автоматический CI на новом SHA.
Public uploads OFF/release workflow исключён. Сохранить source/merge/run и завершить
этап без ожидания CI и без перехода к cleanup. Следующий конкретный этап после этого
результата — retained exact Admin cleanup4; он пока только сохранён отдельно.

## История: подготовка Admin snapshot + dispatcher на CI626

База source `ec5b0e9e2f9c52ab7437ae3b99b79d897a734262`, merge
`041aa00c505bb88b0d1bb876c9a79f004336d23f`; [CI626/39823713132](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39823713132)
независимо подтверждён SUCCESS: Windows119981330817/Ubuntu119981328113. Windows build identity
совпадает с merge; Desktop5294, все11 modal export и3 native chooser (cancel/missing/error)
PASS в завершённом логе. Upload steps SKIPPED. Это узкий результат точной базы; прежние
pending/NOT_RUN записи ниже отражают момент их сохранения. Методика и баллы58–80/22 неизменны,
новый полный критерий проекта/приёмка на ПК не закрыты.

Новый код `30c712c4102aa93cb5f22ac5ce101311bd8468d6`, ветка
`work/progo-admin-api-on626-20261022`, worktree `ProGo-admin-api-on626`.
Перенесены только a1fb9316c32537fdbdd3a30d248ebabfdd963329 и
74f90504778f90ef9d89010e61f4ef2975e13853: src/HomeVpnService.cs и
 tests/HomeVpnSetupRecoveryTests.cs,194 добавления/3 удаления. Snapshot сохраняет копию
owner и читает существующие DPAPI-журналы в worker, возвращает отдельные копии запросов.
Dispatcher запускает transport на явно выбранном UI owner и удерживает фактический Task
после admission даже при закрытии owner. Это подготовительные API; production AdminAsync
и существующие пользовательские маршруты в этом этапе не мигрируются.

Зависимости PASS на точном ec5b0e9: HomeVpnOwner.Validate; существующие request/exception,
Load/HasPending/AcquireAsync API в HomeVpnSetupRecovery/AdminRecovery/ShareRecovery;
HomeProfileUiDispatcher в HomeProfileHttpWaitForm.cs (DispatchAsync/PublicationCount);
HomeVpnWaitingTests.ModalWizardCase. Test-HomeVpnRelay.ps1 уже компилирует эти файлы,
HomeVpnWizardTests вызывает HomeVpnSetupRecoveryTests.Run. Незавершённые ownership/cleanup,
AdminOwnedAsync или новая invitation/HTTPS интеграция не нужны и не переносятся.

17 новых native случаев:12 snapshot (setup/invite/share/empty, missing3, pending2,
cancel/corrupt/missing-key) с реальными защищёнными файлами, lease и UI heartbeat;
5 dispatcher (ready/before-dispose/during-dispose/file-fault/worker-only) с реальным
ShowDialog и file I/O. Новые границы3s/5s, modal helper15s; исходный Pump default12s
сохранён, обе группы вызываются перед NativeRecovery. Windows compilation/native17
NOT_RUN: локальная среда Linux без PowerShell/.NET. SUCCESS626 на новый код не переносится.

PASS локально: полный ручной diff, syntax/await-scope2 (не компиляция), diff --check,
public content321/0, добавленные secret/PII0, каноническое совпадение source2 с frozen
Admin API c1dfc6d. Исходное тело AdminAsync, UI экспорта/его тесты, весь EOF launcher/BOM,
invitation/HTTPS файлы, workflows и VERSION побайтно совпадают с ec5b0e9.

Далее в этом этапе: точный source-only tree (2 code+3 checkpoint docs), свежий PR/public/
workflow guard, guarded fast-forward только существующей ветки PR88 и один автоматический
PR CI. Артефакты public uploads OFF; release/main merge/выгрузка бинарников/установка
не разрешены. После сохранения source/merge/run завершить ответ без ожидания CI и без
следующего этапа. Сохранённые root73ebde9/UI8c89168/Admin-on622 c1dfc6d не изменяются.

## История: подготовка UI экспорта на CI625

База source `cc835548a51e525b4537959d414dc4a4fb17280a`, проверенный merge
`080e54c3312d62cc72977afb94d55c3c1a4f42ce`; [CI625/39811359508](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39811359508)
завершён SUCCESS (Windows119938159124 и Ubuntu119938159595). Это доказательство
относится только к базе: API экспорта5+9, исправления phone fixture и EOF launcher.
Исторические записи ниже сохраняют состояние на момент записи.

Код кандидата `46cb9a253abd1aa8e2f69861e8a50ad216808ed4`, ветка
`work/progo-export-ui-on625-20261022`, worktree `ProGo-export-ui-on625`.
Перенесены только UI9b555e5/modal11 b1f9967/chooser3 a1fb4e7: восемь файлов.
Выбор файла вызывает удерживаемый ExportProfileAsync; Cancel не закрывает ShowDialog,
повтор доступен после фактического завершения I/O; закрытие/Dispose/Stop удерживают
принятый worker, поздняя отмена не подменяет сохранённый результат. Phone command gate
учитывает экспорт. Сохранение файла подтверждает выдачу, а не установку на телефоне.

Проверки PASS: ручное ревью полного diff, syntax/C#5 await-scope7 (не компиляция),
public content321/0, diff --check, добавленные secret/PII0, точный scope восьми файлов.
Канонические Git-блобы семи C# файлов совпадают с сохранённым UI-on624. В Test-ProGo.ps1
добавлен только исходник DesktopProfileNativeChooserTests; после удаления этого одного
аргумента скрипт побайтно совпадает с cc835548, включая EOF-поправку и BOM. Первоначальная
проверка байтов рабочей копии обнаружила ожидаемый checkout CRLF; сравнение канонических
Git-блобов прошло. Admin/invitation/service/access/workflows/VERSION идентичны базе.
Новая Windows compilation/native14: NOT_RUN; PowerShell/.NET в локальной Linux-среде
отсутствуют. SUCCESS625 на новый код не распространяется. Веса и баллы неизменны:
реализация58–80/100, проверенная22/100(raw21.75), изменение0.

Публикация: подготовить точный source-only tree с тремя документами, проверить равенство
деревьев и свежий head PR88; один guarded fast-forward cc835548→новый SHA. Ветка integration
запускает только автоматический PR CI; ручной dispatch не нужен. Public upload guards OFF,
release workflow только main/VERSION либо manual. После публикации сохранить source/merge,
ID единственного запуска и его фактический статус; завершить этап без ожидания всего CI.

Восстановленная более полная локальная разработка сохранена отдельно:73ebde96f769962e4f086ab0af9c1a9cbca2d16f;
проверенный GitHub source checkpoint3b1090f57cc1f123bc8d0a77d5fe3d7f37c64cf3 имеет то же дерево,
не является head PR88 и не имеет CI. Его Admin/invitation не входят в этот кандидат.
Следующий этап только после команды владельца: проверить тот же новый run и его точные
14 UI/modal/chooser сценариев; не создавать повторный CI при неизвестном результате.

## Exact EOF expectation corrected locally — strict protocol, native NOT_RUN

Isolated `c548e3baefd805bca43dbdd0cd9b6ea0081b898a` on exact624; one test script
5add1delete only. Read raw stdin to EOF and compare EXACT declared Console.InputEncoding
preamble passed as safe hex; reject arbitrary/extra/missing bytes with42. Logs lengths/
match only,no input data. This corrects my empty-text expectation against documented
Framework StreamWriter/AutoFlush behavior; no Console.InputEncoding/codepage mutation,
no broad acceptance of unexpected data. Actual624 byte count not invented. All0/17/
200ms/EOF5s/termination/stream/entry marker gates retained. Manual source/quoting/BOM/
diff/public320/0/secretsPII0 PASS; embedded Linux protocol accepted empty/exact BOM2,
rejected arbitrary/extra/missing42 in3 cases,all5s. Actual new PowerShell/native NOT_RUN.
Next only script+latest3docs freeze/canonical tree/fresh public workflow+PR guard and
one exact correction CI; UI/Admin/invitation code stays excluded. I58–80/Y22,delta0.

## CI624 actual startup PASS; new EOF test expectation FAIL — no full SUCCESS

Source63264c4119735220fa560ec0b20fb350d45e7754 /merge3f0b0c14ba3db55ea63b6825d486ea07e04e94b6,
run39807343603 completed Windows119923973872 FAIL/Ubuntu119923974608 SUCCESS.
Production/Desktop compilation PASS15:42:50.408. Actual positive Python exit0/body-entry
PASS15:52:24.5987627(process5780/start15ms,within5s); prior pre-entry stall did not recur.
New EOF probe reached its body15:52:24.6379583(process7316/start1ms) then returned42:
nonempty text,exact bytes/count NOT logged. This is my new test expectation error:
[Framework Process](https://github.com/microsoft/referencesource/blob/main/System/services/monitoring/system/diagnosticts/Process.cs)
creates Console.InputEncoding StreamWriter with AutoFlush; [StreamWriter](https://github.com/microsoft/referencesource/blob/main/mscorlib/system/io/streamwriter.cs)
flush writes its encoding preamble without application text. Next compare raw input to
EXACT declared preamble and EOF,reject all other bytes with42. No encoding/codepage or
system configuration change, deadline/negative-check weakening. Prior622/623 native
stack cause remains UNKNOWN; no retroactive causal certainty. exit17/200ms negatives,
Desktop/phone correction/HomeAPI14/harness/package NOT_RUN after new probe failed;
all uploads/prune SKIPPED. Native fixture failure preserved until corrected exact proof.

New agreed existing receipt-consumption worker `e8fbd2ae6dad919b95c2b92ed34bda3005aa52d6`
first implemented after recovery(pre409 patch never applied). Owns family lease through
actual private read/delete; validates exact token and rechecks all10 fields before
memory admission; cancel/refusal/stale ID or valid changed result preserves journal,
missing false,accepted actual delete true,sharing failure typed/no private error text.
Eight actual ShowDialog/real DPAPI+delete sharing/cancel/race vectors with3s/5s bounds,
full protected bytes/lease cleanup. Static manual/syntax2/C#5/diff/public321/0/secretsPII0
PASS; compiler/native8 NOT_RUN. Invitation view still synchronous/unconnected. Next
freeze its API14(pending6+consume8) separately; native proof before owned UI binding.
I58–80/Y22 exact620 weighted partial gates,delta0/fullR09 FAIL/R11 open/targetA0.

## Profile UI14 retargeted on624 — separate complete checking input

ProGo-export-ui-on624 /work/progo-export-ui-on624-20261022 local
`e15cefc7392a35020b6376a192aa516065a21b83`,tree
`2f8207bd90cc8c182824c5b7eb625c5f3f7eb912`,base63264c. Same reviewed UI9b555e5/
modal11 b1f9967/chooser3 a1fb4e7,now includes current phone/launcher/owned-stdin fixes.
Exactly11 paths(8 source+3docs),all Admin/service/access/recovery/workflow/VERSION
unchanged. Only new explicit Desktop harness source in script; current launcher body
byte-equivalent. Fullscope/syntax7/C#5/diff/public321/0/secretsPII0/JSON86/current620
weighted evidence and published/local pointer consistency PASS. New compiler/native14
NOT_RUN; not published while624 active. Old07363f7 freeze untouched history.
Next independent small agreed R09 component: existing invitation consumption worker8
with actual request/result recheck,cancellation and DPAPI/delete sharing fixtures. This
patch never applied before409,so it is new implementation,not a restored old commit.
I58–80/Y22,delta0; no new source head/CI/points.

## CI624 in progress — owned stdin source published once

Source `63264c4119735220fa560ec0b20fb350d45e7754`, merge
`3f0b0c14ba3db55ea63b6825d486ea07e04e94b6`, full tree
`7a04b4e6b3850bdd52469cdb700854a2e6de7492` equals local freeze4794599 and fetched
source/merge exactly. [CI624/39807343603](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39807343603)
in_progress; one automatic run after guarded e847→63264c, no duplicate623/manual run.
Only test launcher owned stdin+strict EOF regression and three docs; runtime/src/tests/
workflows/VERSION unchanged623. New exact compiler/native/package NOT_RUN until final
completed evidence. Earlier623 zero-CPU pre-entry stall and622/621 failures preserved;
causal stdin hypothesis not retroactively proven. Local development UI14/Admin17/task8/
cleanup4/pending6 are EXCLUDED and compilation/native NOT_RUN. All public uploads OFF;
no release/main merge/install/binary delivery. Next retarget separate UI14 checking
freeze on exact624 with latest fixture/launcher fixes, preserving every local block;
inspect same624 before any new source changes its checking head. Scores I58–80/Y22
exact620 weighted partial evidence,delta0/fullR09 FAIL/R11 open/targetA0.

## Owned stdin publication recovery checkpoint — CAS next

Prepared remote `63264c4119735220fa560ec0b20fb350d45e7754`,parent/expected refe847,
canonical tree `7a04b4e6b3850bdd52469cdb700854a2e6de7492` equals frozen local
`47945999ba7da439622f0561761abb6ca03ee75d`. Four paths only,script+three docs;
src/tests/workflows/VERSION identical623. Fresh public PR e847/base4bb/623completed
FAIL,upload guards OFF/release not triggered. Full scan/diff/JSON/BOM/public320/0 and
embedded Linux0/EOF0/wrong-input42 checks PASS. Actual new Windows NOT_RUN. CAS next;
verify exact result before any repeat after interruption. Local development compiler/
native fields explicitly NOT_RUN; published results kept only with their exact source.

## Owned stdin correction prepared — strict native EOF check, Windows NOT_RUN

Isolated code `2e011b71219c5d7f3032c269911ade6edec3092b` on exact623,one test
script9add1delete. Noninteractive Python fixture tools now own RedirectStandardInput
and immediately close their own input for EOF, removing dependence on CI host input.
Dedicated stdin-eof-regression reads to EOF and requires marker+exit0 within5000ms.
Original positive0/nonzero17/200ms timeout and termination/stream criteria unchanged;
no runtime/interactive SSH/workflow/VERSION or system configuration changes. Busy CI
stdin remains a supported hypothesis for622/623, not an asserted captured native stack.
Manual input/stream/lifetime/PS5.1 quoting/BOM/diff/public320/0/secretsPII0 PASS. Exact
embedded bodies positive0/EOF0/wrong-input42 passed under Linux5s; actual PowerShell/
Windows launcher/native suites NOT_RUN. Next script+latest3docs freeze/scans/canonical
fulltree/fresh public PR+workflow guard and one new exact CI. All local UI/Admin/pending
changes excluded; no points. Current JSON pointers now explicitly separate published
tree and local development tree; obsolete608 current-string preserved as history.
I58–80/Y22 exact620 weighted partial gates,delta0/fullR09 FAIL/R11 open/targetA0.

## CI623 FAIL — native pre-entry facts; stdin ownership correction next

Sourcee84792499b2949602691c349c369d62a533fb444 /merge539e2a23fe7da9640c15971d0b35c7d3b3b47af6,
run39803053552 completed Windows119908864469 FAIL/Ubuntu119908863649 SUCCESS.
Production/Desktop compilation PASS15:15:32.696. Positive Python process started95ms
at15:24:42.145,pid1732,hostedtoolcache. Deadline15:24:47.169 elapsed5115ms: alive,
CPU0ms,RAM5959680bytes,both streams WaitingForActivation,no probe error,entry marker
ABSENT. Own process terminated/streams empty; strict5000ms failure retained. Desktop/
phone correction/API14/later Home/package NOT_RUN; all uploads/prune SKIPPED.

Confirmed launcher ownership gap: stdout/stderr redirected, stdin inherited. Exact
native stack/root cause still UNKNOWN. Busy inherited stdin is a supported HYPOTHESIS,
not a proven explanation: [CPython78961](https://github.com/python/cpython/issues/78961)
documents pre-entry Windows blocking on a pipe with outstanding I/O;
[Microsoft RedirectStandardInput](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.processstartinfo.redirectstandardinput)
documents owning input/closing it for EOF. Next minimal script change: redirect stdin,
close it immediately for noninteractive fixture tools, add strict EOF regression. Keep
all deadlines/exit/error gates, no system/network/security changes or blind same-SHA run.

Local pending-state worker `d500df68629d8af7533e4eca2993284c3675161a` reconstructs
missing9859 as NEW source: Pending/Unreadable booleans, worker actual DPAPI validation,
corrupt/locked stays unreadable+pending,cancel distinct,no token/write/lease. Six actual
ShowDialog worker vectors empty/pending/completed/corrupt/locked/cancel preserve exact
encrypted file set/bytes and caller lease;3s heartbeat/5s settlement+cleanup, old Pump15s
unchanged. Manual/syntax2/C#5/diff/public321/0/secretsPII0 PASS; compiler/native6 NOT_RUN.
Invitation UI binding/consumption worker still TODO; no missing original object restored.
I58–80/Y22 exact620 partial gates,delta0/fullR09 FAIL/R11 open/targetA0.

## Exact Admin cleanup retention4 reconstructed locally — Windows NOT_RUN

Code `e1e793e0b0789e8d686454b052fd3b773623ed57` reconstructs optional core exact
cleanup retention before its original typed error completes; work/upload creation is
inside completion envelope. Service Stop drains all bodies, then retries only retained
closures off UI, keeps locked cleanup owned/false and permits explicit unlock+Stop
without copy/SSH/request replay. New factories refused while cleanup pending. No
production broad directory scan/delete; same exact cleanup routine and typed outcomes.
Four actual-core/injected local transport fixtures complete/list lock/preparation lock/
Dispose lock use real helper/output/DPAPI files, heartbeat/bounded barriers, strict
failed Stop then unlock retry, exact original failure, preserved unrelated sibling and
protected files. Early core failure surfaces directly instead of generic entry timeout.
Manual closure/lease/overload/lifetime/cleanup/diff/syntax2/C#5/public321/0/secretsPII0
PASS; compiler/native4 NOT_RUN, excluded623/API17 freeze. Static Admin UI still legacy;
no complete R09 or VPS claim. Next native API proof before owned production binding;
independent lost invitation pending-state primitive6 can be reconstructed separately.
Same623 Windows119908864469 build in_progress,Ubuntu119908863649 SUCCESS; no duplicate
or head change. I58–80/Y22 exact620 partial gates,delta0/fullR09 FAIL/R11 open/targetA0.

## CI623 in progress — bounded launcher diagnostic source published once

Source `e84792499b2949602691c349c369d62a533fb444`, merge
`539e2a23fe7da9640c15971d0b35c7d3b3b47af6`, full tree
`d527b0007a1cf4ef793cb3ccf2556078e2bb64fc` equals local freezef5484d6 and fetched
source/merge exactly. [CI623/39803053552](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39803053552)
in_progress; one automatic run, no duplicate622/manual dispatch. Script-only owned
Python startup facts/positive entry marker +three docs over622; all runtime/src/tests/
workflows/VERSION unchanged. New exact compiler/native/package NOT_RUN until completed
evidence. Earlier621 phone failure and622 Python timeout preserved separately, neither
silently converted to success. Upload guards OFF/no release/merge/install/binary delivery.
Next independent local exact Admin cleanup closure retention/retry4; UI/API17 frozen and
excluded623. I58–80/Y22 exact620 partial gates,delta0/fullR09 FAIL/R11 open/targetA0.

## Launcher diagnostic publication recovery checkpoint — CAS next

Prepared remote `e84792499b2949602691c349c369d62a533fb444`,parent/expected ref9e6e5a,
canonical tree `d527b0007a1cf4ef793cb3ccf2556078e2bb64fc` equals frozen local
`f5484d65ae5f6b34d155937e5cfb383e116a9366`. Four paths only,script+three docs;
all src/tests/workflows/VERSION identical622. Fresh public PR9e6e5a/base4bb,622completed
FAIL; uploads OFF/release not triggered. Full scope/diff/JSON/BOM/body-Linux/public320/0/
added secretPII0 PASS. CAS next, not yet claimed updated; inspect exact operation on
interruption before repeating. New PowerShell/Windows launch execution NOT_RUN.

## Python launcher diagnostic prepared — unchanged strict native deadlines

Isolated code `7c95af616c38fd146e47e4dac388784d15d474f6` on exact622; one test
script16add2delete. Adds own PID/start duration/interpreter-path category and timeout
HasExited/CPU/RAM/stream-task state/error-type facts, never arguments/environment or
personal paths. Exit0 probe emits an entry marker and must both execute its body and
exit0. Kill is conditional on this owned process still running; deadline remains FAIL
even if it exits at the boundary. Existing5s exit0/exit17,200ms negative timeout,5s
termination/3s stream joins and all exit/error criteria retained. This diagnoses UNKNOWN
622 failure, not an invented cause fix or relaxed check. Manual PS5.1 quoting/lifetime/
strict-criteria/BOM/diff/public320/0/secretsPII0 PASS; exact embedded Python body executed
under Linux5s, required marker/exit0 PASS. Actual PowerShell/Windows launcher NOT_RUN.
Next isolated script+latest3docs freeze/canonical tree/fresh public PR+workflow/secret
check and one new exact diagnostic CI. Product/UI/Admin/workflows/VERSION unchanged622;
local later code excluded. I58–80/Y22 exact620 partial evidence,delta0.

## CI622 FAIL — different earlier Python startup boundary, cause UNKNOWN

Source9e6e5a15e9414d562af6f1788505437f2ac96d9c /merge8db81f1c11cc713690f3c87f9669d2e12b9aba73,
run39800500570 completed Windows119899498867 FAIL/Ubuntu119899499567 SUCCESS.
Production/Desktop compilation PASS, build identity14:59:13.4176149Z. Native owned
Python exit0-regression failed15:08:05.7445169: process exceeded5000ms and was cleaned.
Empty captured stdout/stderr; no old interpreter-entry/CPU/RAM/HasExited/stream-state
facts, so cause UNKNOWN. Desktop execution was NOT_RUN: phone budget correction is
NOT_RUN, not a repeat of621 or a claimed fix PASS. Later Home/API14/harness/package
NOT_RUN; all upload/prune SKIPPED. Existing test failure preserved, no timeout PASS.

Next isolate script-only safe startup/deadline facts and positive interpreter-entry
marker; retain5s positive,exit17/200ms negative criteria and exact owned cleanup. No
production or workflow change, no global timeout increase or blind same-SHA rerun.
UI freeze07363f7/Admin API17 c1dfc6d/ownership437b69e preserved and excluded.
I58–80/Y22 exact620 partial evidence,delta0/fullR09 FAIL/R11 open/targetA0.

## Admin ownership8 reconstructed; CI622 failed step awaiting final evidence

Local `437b69e8037a9c33e0f581679f0736dfc1321f2c` reserves every Admin public task
before scheduling, checks Stop identity/lifetime before factory admission, keeps actual
accepted body outcomes and snapshots/drains all tasks in public Stop/Dispose. Original
typed recovery cancellation survives. Metadata only under lifecycle gate; admitted body
runs on worker and must not await the public Stop that owns its drain. HasOwnedResources
includes pending bodies. Eight bounded real-file cases complete/Stop/Dispose/lock-fault/
typed-fault/queued Stop/queued Dispose/multiple accepted bodies check heartbeat, exact
single drain, no unstarted factory after Stop and protected-file preservation. Errors
also occur while Stop waits. Static manual ownership/lifetime/error/diff/syntax2/C#5/
public321/0/secretPII0 PASS; new compiler/native8 NOT_RUN. Actual Admin route unconnected;
no failed-cleanup retention yet. Excluded API17 freeze c1dfc6d and checking622.

Same622 Windows119899498867 latest job in_progress but build step completed FAILURE;
later test/package/upload steps SKIPPED,Ubuntu119899499567 SUCCESS. Final completed
log/conclusion must be checked before claiming cause or another fix. Next inspect this
same run's exact phone-state diagnostics, preserve every local block, no blind rerun.
I58–80/Y22 exact620 partial evidence,delta0/fullR09 FAIL/R11 open.

## Admin API17 isolated freeze preserved

ProGo-admin-api-on622 /work/progo-admin-api-on622-20261022 local
`c1dfc6d0162bacfbe3a476d4febc3a9d7b0cf0df`,tree
`e7fdf97d077d227ef636b1603d36abfc0a9a5ce8` on published9e6e5a. Exactly HomeVpnService,
HomeVpnSetupRecoveryTests and latest3 docs; production UI/HomeWaiting/scripts/workflows/
VERSION unchanged622. Static scope/diff/public320/0 PASS, new compiler/native17 NOT_RUN;
local only, no publication cancelling622. UI freeze07363f7 separately retained.
Next independent local foundation: owned Admin task admission/accounting and public
Stop/Dispose drain8; actual AdminAsync remains unconnected until owned worker/UI binding.

## Admin transport5 reconstructed — explicit owner, actual Task retained

Local `74f90504778f90ef9d89010e61f4ef2975e13853` reconstructs the reviewed UI
startup adapter and its publication-barrier followup in one coherent block. Only
callback startup uses HomeProfileUiDispatcher; actual returned Task is awaited after
owner disposal. Closed-before-admission refuses callback, null dispatcher permits only
worker-compatible injection. No automatic retry/process/lease creation, no production
AdminAsync binding yet. Five actual ShowDialog local-file vectors ready/before Dispose/
during Dispose/real locked-file IOException/worker-only; lost ambient context, worker
heartbeat, actual outcome and single UI publication, publication signal only AFTER
physical Task assignment.15s modal/5s barriers+join, own GUID outputs/cleanup only.
Manual lifecycle/publication/cleanup,diff,syntax2/C#5/public321/0/secretPII0 PASS;
compiler/native5 NOT_RUN. Snapshot12 a1fb931 also NOT_RUN, both excluded622.
Next freeze these two API blocks17 on published622 without UI or later task ownership.
Same622 Windows119899498867 in_progress(build step),Ubuntu119899499567 SUCCESS;
no duplicate/new checking head. I58–80/Y22 exact620 partial evidence,delta0.

## CI622 queued — narrow phone fixture correction published once

Source `9e6e5a15e9414d562af6f1788505437f2ac96d9c`, merge
`8db81f1c11cc713690f3c87f9669d2e12b9aba73`, canonical tree
`abf6f9e0f568d30833f9917ed2025d302c472a14` equals isolated freezeebe59b1 and fetched
source/merge exactly. [CI622/39800500570](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39800500570)
queued; new compiler/native/full regression/package NOT_RUN. Runtime unchanged621,
only two test files+three docs over2c0cb. Source CAS independently confirmed, no manual
rerun/duplicate621; preserve that failed exact result and620 last full SUCCESS separately.
All uploads OFF/no release/merge/install. UI freeze07363f7/Admin snapshota1fb931 excluded.
Next independent local Admin transport bridge5; poll same622 completed evidence before
any source publication cancels it. I58–80/Y22 exact620 partial gates,delta0.

## Phone fixture publication recovery checkpoint — CAS next

Prepared remote `9e6e5a15e9414d562af6f1788505437f2ac96d9c`, expected parent/ref2c0cb,
canonical tree `abf6f9e0f568d30833f9917ed2025d302c472a14` equals frozen local
`ebe59b124c412f571994d970a5093ce39df93c1a`. Five paths only,two test files+three docs;
source runtime/UI/Admin/workflows/VERSION unchanged621. Full secretPII0/public320/0/
diff/syntax2/C#5/JSON checks PASS; fresh public PR head2c0cb/base4bb/621completedFAIL.
CAS not yet claimed updated; do once then independently inspect PR/new run. Unknown
operation on interruption must be verified before any repeat. New Windows NOT_RUN.

## Narrow CI621 fixture correction prepared — Windows NOT_RUN

Isolated code `c5e35cc62dd40d8ccec2b7409489cbb0ac4a1c32` on exact published2c0cb;
two TEST files11add4delete only. New optional fixture settings budget retains old
shared3000 default; phone vectors explicitly select250 for phone/settings as well as
backup. Original compound refusal/responsiveness/paused/native proxy effects and retry
criteria unchanged byte-for-byte, barrier5s unchanged. Safe per-flag/task-status/fault-
type/elapsed diagnostic prints only if the original predicate fails. No product or
global timeout change; exact historical621 failing flag remains UNKNOWN. Confirmed
configuration mismatch is fixed; fresh Windows evidence must establish the result.
Manual timing/parity/arity/diff/syntax2/C#5/public320/0/secretPII0 PASS; new compiler/
native NOT_RUN. Next latest3 docs freeze on isolated621+c5e35cc, scan/canonical tree/
fresh PR head+workflow guard, publish one new source and inspect one fresh CI. UI freeze
07363f7 and Admin snapshota1fb931 are excluded and remain NOT_RUN. I58–80/Y22,delta0.

## CI621 FAIL — preserve local Admin snapshot; narrow fixture correction next

Exact source2c0cb1418dd2c6dc4b82698f1ee1fc49f1a57850 /merge2d3f1144a9608fdd85b7ed8ed99d99d4238b8d0c,
run39797286590 completed: Windows119887722612 FAIL,Ubuntu119887722929 SUCCESS.
Completed Windows log build identity14:38:00.173 PASS production/Desktop compilation;
diagnostic native chooser3 PASS14:49:54–14:50:00. Desktop fails14:50:27.8268657 in
PhoneStopContextHeld/shutdown original compound phone-cleanup-deadline assertion.
Individual failed flags UNKNOWN. Later Home/API14,harness compilation,package NOT_RUN;
all uploads/prune SKIPPED. Earlier API responses said queued; completed log proves
Windows already ran14:38–14:50, so no duration is falsely attributed to a queue.

Confirmed fixture configuration mismatch: AsyncRestoreContextFixture(root,250) forwards
250 only to backup budget; actual phone/settings budget defaults3000 while its held
cleanup barrier is5000. Sequential preceding drains can consume the remaining margin;
exact failed621 predicate still UNKNOWN. Next isolate two test-file correction: explicit
phone/settings budget250 for these phone vectors plus safe per-flag/task-type/elapsed
failure diagnostic. Retain all original criteria,5s barrier and existing shared defaults;
no production timeout increase/disabled checks/blind same-SHA rerun. Freeze on exact621,
exclude UI/Admin work, publish new source only after review and one fresh CI.

Local readonly Admin snapshot `a1fb9316c32537fdbdd3a30d248ebabfdd963329` preserved:
frozen owner, worker selected-key existence/DPAPI request reads, existing family refusals,
no lease/register/consume/staging/remote action; copied snapshot outputs.12 bounded
native vectors before terminalNativeRecovery; original Pump default12s retained while
new vectors use3s heartbeat/5s barrier+join. Static manual/syntax2/C#5/diff/public321/0/
secretPII0 PASS; new compiler/native NOT_RUN, actual AdminAsync still sync. Excluded621.
UI freeze07363f7 untouched. I58–80/Y22 exact620 partial evidence,delta0/fullR09 FAIL.

## Separate UI freeze preserved before Admin work

UI checking worktree ProGo-export-ui-on621 /work/progo-export-ui-on621-20261022,
local `07363f7cefa52841b03bde9e421871c81cbb730a`, tree
`ed40d9060420be912f53ca11f25cc62467456796` exactly equals development c3f01a6 at
this point. Built on published2c0cb with only UI9b555e5/modalb1f9967/choosera1fb4e7
and latest3 docs. Local freeze only; NOT_RUN/compiler/native14, not published while621
active. Next independent bounded microtask: reconstruct readonly copied-owner Admin
validation/journal snapshot12; no transport or UI binding. Same621 ID remains sole run.

## Native profile chooser3 reconstructed — Windows NOT_RUN

Code/tests `a1fb4e78d6b8a3d1e4e9a1e214db1af7ec7193fa`: actual production profile
Save button inside real outer ShowDialog/nested SaveFileDialog, own child/job20s;
existing PID/thread/root-owner helper unchanged, native discovery/close5s and cleanup
join5s. Positive Cancel verifies modal owner remains open, no export task/file/issuance,
unchanged protected tree. Deliberate missing20 and injected error21 require the exact
nonzero outcome plus cleanup marker and actual native-window observation; real timeout
in positive case fails. Explicit new DesktopTests partial file compiled exactly once;
script BOM preserved. Manual trace/diff/syntax3/C#5/public321/0/added secretPII0 PASS.
PowerShell/compiler/native3 NOT_RUN. Not included in620 or API-only621.

Local export UI9b555e5 +modal11 b1f9967 +chooser3 a1fb4e7 form a separate next checking
cohort; freeze on exact published621 source with latest3 docs, but do not publish while
621 is active (would cancel it). Same621 Windows119887722612 queued/Ubuntu119887722929
SUCCESS. Continue independent reviewed Admin read snapshot locally; preserve both frozen
sources. I58–80/Y22,delta0; fullR09 and target/installer/private delivery remain open.

## Modal export regression reconstructed — Windows NOT_RUN

Tests `b1f99672aaecc0bb3a9916b1af6b51ae11988e36` add11 actual outer ShowDialog
post-chooser scenarios: save,Cancel→Retry,Close,Dispose,accepted Cancel/Close/Dispose,
global Stop/accepted Stop,locked existing→new-name retry,accepted access change.
Checks retain one physical task across native loop turns, DialogResult.None, honest
actual file result, original captured identity, no old result over new retry, unchanged
DPAPI settings, no installation claim/late relay/late disposed controls. Existing15s
modal/5s worker+cleanup budgets, exact owned GUID outputs only; shared production
ExportProfile helper invoked after chooser. Actual native chooser proof is separate.
Manual phase/cleanup/lifetime diff,syntax1/C#5/public320/0/secretsPII0 PASS. New compiler/
native11 NOT_RUN; excluded from621. Next native profile chooser3 with existing owned
PID/thread/root-owner helper and unchanged5s discovery/20s child job budgets.
I58–80/Y22,delta0; do not alter checking621 source while Windows is queued.

## Actual export UI reconstructed locally — NOT_RUN on Windows

Code `9b555e5ea0f4d8de797b516a84ed464db32561cd`, three production files only:
actual SaveFileDialog now selects the path before retained ExportProfile/ExportProfileAsync;
all profile XML/file I/O is worker-owned, CancelButton stays DialogResult.None and
explicit Close/Dispose retains actual work. Cancel before admission permits retry;
accepted save is reported honestly, changed access/home cannot mark the current
profile issued, file saving never confirms installation. IOException messages omit
local paths. Export blocks only another phone route; desktop/settings/Stop remain
available under existing command policy. Manual diff/syntax3/C#5-await/public320/0/
secretPII0 PASS; new compiler/native NOT_RUN. Not included in checking621.
Next reconstruct11 actual ShowDialog post-chooser cases, then native chooser3; preserve
exact621 source while its Windows119887722612 is queued. Ubuntu119887722929 SUCCESS
only for621/2c0cb/2d3f. No duplicate CI or upload. I58–80/Y22,delta0.

## CI621 queued — reconstructed API published once

Published source `2c0cb1418dd2c6dc4b82698f1ee1fc49f1a57850`, merge
`2d3f1144a9608fdd85b7ed8ed99d99d4238b8d0c`, full tree
`d75aa2836c8d83ac1a1ccae39ac9b088ae3940f4` equals local freeze48343aa; fetched source
and merge equal exactly. Guarded source CAS independently confirmed in PR88.
[CI621/39797286590](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39797286590)
queued; new Windows compilation/native14/regression and package NOT_RUN. Do not inherit
620 SUCCESS or launch duplicate CI. Poll this same ID/completed jobs and log. Public
uploads OFF, workflow/VERSION unchanged; no release/merge/install or binary delivery.
First independent local next task: reconstruct actual post-chooser export UI ownership
in a separate commit, without changing621 while it runs. I58–80/Y22(raw21.75),delta0.

## Publication recovery checkpoint — guarded CAS next

Frozen local `48343aac81ea82b835cfd9919b0d3f6e19b22974`, canonical tree
`d75aa2836c8d83ac1a1ccae39ac9b088ae3940f4` equals prepared remote commit
`2c0cb1418dd2c6dc4b82698f1ee1fc49f1a57850` parent published12d5. Six files only;
full added-content scan0, full diff/C#5/public320/0 PASS, unchanged workflows/VERSION.
Ref not yet claimed updated: perform one guarded CAS expected12d5 and verify PR head.
On interruption first inspect PR/ref/new commit runs; never repeat unknown CAS or CI.
New code Windows NOT_RUN; no upload/release/main change. Scores unchanged.

## Reconstructed owned export API — checking publication pending

Local service/test code `8903762ee659efaaa1923aacf146f7201e90c557` follows worker
`0cf5328f5e00f669a18e91baeb43d72842788d98`; these are NEW reconstructed sources,
not restored original missing objects. Service publishes one public export task before
worker/file I/O, joins exact duplicates and refuses conflicting export, captures access,
home and Stop identity with memory-only owner admission. Stop/Dispose cancels before
admission and joins actual accepted file work without UI blocking or self-await. Nine
bounded native cases save/cancel/Stop/Dispose/accepted Stop/accepted Dispose/stale access/
stale home/refused owner also check unchanged real DPAPI files and no started relay.
Local manual ownership/lifetime/diff, syntax/C#5 scope, public320/0 and added secretPII0
PASS; new Windows compilation/native14 NOT_RUN. Actual SaveProfile remains synchronous,
fullR09 FAIL. CI620 is only the unchanged published baseline, never this local code.

Freeze exactly six files: src/HomeVpnAccess.cs,src/HomeVpnService.cs,
tests/HomeVpnWaitingTests.cs,docs/PROJECT_STATE.md,docs/ACCEPTANCE_RUNBOOK.md,
docs/project_state.json. No export UI/chooser/Admin/invitation work or workflow/VERSION
changes. Public PR88 head still12d5/base4bb,620 completed; upload guards OFF/release not
triggered. Next canonical tree equality and guarded source CAS once, record one new CI
ID; new native14 plus existing full regression on its exact SHA. In parallel local UI
reconstruction can proceed in a separate commit without changing that checking source.
I58–80/Y22(raw21.75),delta0; target/installer/private delivery gates stay open.

## Reconstructed export worker — new local source, Windows NOT_RUN

Local code `0cf5328f5e00f669a18e91baeb43d72842788d98` reconstructs the missing reviewed
export primitive on exact published620. Two files,70 additions: immutable value/string
snapshot before worker scheduling; memory-only admission; real XML/file I/O off UI;
CreateNew prevents overwrite; cancellation after admission keeps the actual outcome.
Five bounded native cases cover save/cancel/refusal/locked existing/accepted-cancel,
including original identity/password/CA after caller edits. Own GUID output only,
release and join5s, heartbeat3s; this primitive fixture uses Show(), not modal UI proof.
Manual diff/syntax2/C#5 await-scope/public320/0/added-content secretPII0 PASS. No .NET
or Windows runtime here: new compilation/native NOT_RUN; CI620 does not cover this
code. Scores unchanged I58–80/Y22(raw21.75),delta0. Next reconstruct service ownership,
then publish only reviewed API5+9 and latest evidence docs for one new exact CI.

## Recovery after environment_offline — CI620 verified; local snapshot earlier

Current published/tested source `12d5cbc47b5cf2c1be0a0b16b2186b938da51952`, merge
`7853b8cd1aefe09456392e9b30b765943ea5e5af`, canonical tree `e160e3a481292fa6df0f20628120932f9ea2caab`.
[CI620/39777591168](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39777591168)
Windows119820381219/Ubuntu119820380887 SUCCESS, completed jobs/log verified; local
source and merge trees exactly equal. Build identity12:21:11.777Z, Desktop5287,
HomeVPN1948 at12:47:47.040Z, Portable91, Python28=26PASS2SKIP. Actual modal import/
receipt11 +initial snapshot5 +receipt worker7 PASS, including cancel-edited-retry,
actual retry SSH boundary and terminal recovery order. Signing55/UI50/preparation76
and four actual direct/owned-agent SSH startup/recovery child-lifetime I/O metrics
PASS. These are partial dev criteria, not installed owner/VPS or whole-app performance.

Runner ZIP0.2.2 SHA256 `ba60dcbc4642834baaa8128de3176e1bd751b7a5e5ff8b8dff07dd53b033eae6`;
all uploads/prune SKIPPED. ZIP bytes not retrieved/delivered/installed; private delivery
BLOCKED pending owner-approved channel. No release/merge/install or duplicate CI.
All28 existing weighted partial evidence markers matched exactly in620 completed log:
same weights/D0.5/P0.5/A0, I58–80/Y22(raw21.75)/delta0; no new full weighted gate.

Environment now executes. Observed old local root HEAD b1b2139cf3b338ca4ed965941bfab07d8158cec7,
clean20 worktrees, no pending Git operations/build/SSH processes or another executor.
Later local source/checking objects9859/5387/674e/65590/export/Admin and checkpointfecb718
are MISSING in this filesystem; the reason for the older snapshot is UNKNOWN. Nothing
was reset/deleted. Old root protected by recovery/pre-offline-snapshot-20261022 and
left untouched. Published source+merge fetched successfully; separate current worktree
ProGo-recovered-ci620 /work/progo-recovered-ci620-20261022 starts at tested12d5.
No uncommitted changes existed to save. Git fetch sessions completed, no duplicate CI.

Unpublished export/Admin/default-configure/pending-state work described below remains
historical reviewed work, not present recovered code or Windows PASS. Do not pretend
the missing original commits were restored. First unfinished task: reconstruct the
same bounded immutable profile-export worker/API5+9 cases from saved source review,
with new commits/SHAs and fresh Windows verification. Do not expand features or replay
any external action. Later Admin blocks and invitation pending-state9859 also require
explicit reconstruction/verification; consumption-worker patch never applied before
409. Existing public620 native evidence stays exact; fullR09 FAIL/R11PARTIAL/targetA0.

Historical checkpoint sections below keep their original time/source meaning; their
in-progress/ref-NOT_RUN entries are superseded by this recovery record.

## Frozen actual-SSH boundary fixture fix — one file on exact619

Code `b1eec12581c67a53c7ecc0fbdd512488f0c0b904`, code treebabe3b22bf81ed9bc1e9b12035fa893c9f9d5c76,
ProGo-ci619-boundary on5c5ac7e/f682. Root5387ddeb narrow test fix only,19add6delete.
Unchanged original SSH Close assertions after a bounded actual command admission;
finally releases/joins exact response/task/snapshot. Production files and all
export/Admin extensions/default configure binding excluded. Fullsource manual/diff/
public320/0/syntax/C#5 PASS; new Windows compiler/native NOT_RUN. Next latest3 docs
freeze, added-content secret/PII and fresh public workflow/head checks, fulltree equality,
guarded f682→new source CAS once.619 FAIL/partial proofs and617 last full SUCCESS
kept separately. I58–80/Y22/raw21.75/delta0. History below.

## CI619 FAIL after full Test-ProGo PASS — asynchronous fixture boundary fix

Exact source `f682432f4afd53bc7b3e7c9142d60dcb42a6a22e`, merge
`5d615896c349051606e6212ed119b3a2cb8ce356`, run39773542091 Windows119806729629 FAIL,
Ubuntu119806729392 SUCCESS, completed log checked. Build identity11:50:13.807;
full Test-ProGo step SUCCESS, Desktop5287 PASS12:08:30.718/Instance28/Maintenance126/
Shutdown47 PASS12:12:04.641. Prior618 startup timeout did not reproduce; underlying
predicate cause still UNKNOWN, diagnostic retained (no fabricated root-cause fix).
Four actual direct/agent startup/recovery latency/CPU/RAM/child-I/O collections PASS
12:07:14.820–21.684. HomeWaiting completed actual modal import/receipt11 routes
12:12:32–41, preserving Cancel→Retry/Close/Dispose/global Stop/partial owner behavior.
Later HomePreparation.WizardBoundary failed12:14:18.407 at original protected-SSH
Close assertion. Dedicated initial snapshot5, setup receipt7 and later Home/Admin/
portable/Python relay suites NOT_RUN after this point. ZIP/package NOT_RUN/uploads
SKIPPED; overall619 FAIL, not full SUCCESS.

Confirmed test sequencing defect: second Next immediately followed by Close while
new cached refresh/ReadSetupPending is asynchronous. That tests cancelling a local
read rather than the intended actual held command; old compound assert did not emit
individual flags, so their historical values are not invented. Root narrow fix
`5387ddeb498d8bf3f51189447f26b9e1e03af315`,1file19add6delete: wait actual snapshot
readiness, then actual command callback (bounded5s) before Close; preserve original
close-refusal/status criterion and assert real command entered. Finally releases
pending response, joins exact retry and refresh, observes typed outcome. No product
change, global timeout increase, gate removal or same-SHA blind retry. Syntax/C#5/
manual phase/cleanup/diff PASS, new compiler/native NOT_RUN. Next isolated checking
from exact619 local5c5ac7e +only this fix/latest3 evidence docs; all export/Admin code
excluded. I58–80/Y22 exact617 partial evidence/package, delta0. History below.

## Default configure checking isolated — depends on API proof

Code `19d0c64ddf1e8f10963a6753410b94446054f541`, tree3f547a2f491eda31b9cbda9b6f638692a8803b67
in ProGo-admin-configure on owned API85b634f. Single root0ffc3d2 cherried without
conflict,3 paths67add3delete, same reviewed delta byte-for-byte excluding offsets/blob
IDs. Actual default HTTPS configure→owned worker/exact own dispatcher,2 native modal
Stop/Dispose-before-factory tests. No export UI/native chooser or wizard/friend Admin
binding. Scope/manual/root delta parity/syntax3/C#5/public320/0/secretsPII0 PASS;
new compilation/native NOT_RUN, docs not attached, publication NOT_RUN. Follow the
prepared export→read/dispatch→owned-worker API dependency order and rebuild on any
required predecessor fix. Active619 unchanged; do not push docs-only checkpoints.
I58–80/Y22/raw21.75/delta0. History below.

## Local default HTTPS Admin route connected — Windows NOT_RUN

Source `0ffc3d241962f523d6b02214e908a3c895567da3`,3 paths67add3delete. Default
CreateConfigureForm now binds AdminOwnedAsync to its exact dialog/dispatcher; explicitly
injected test/admin callbacks remain unchanged. Worker uses the existing captured
BeforeSessionIo fixture boundary named admin before factory admission; explicit
per-call barriers keep their precedence. No ActiveForm or caller-UI private validation.

Two native actual ShowDialog default-install routes written: public Stop/service
Dispose while owned preparation is held; assert UI heartbeat, retained shared Stop,
no request/save/acceptance and exact private bytes. A GUID missing key guarantees a
regressed legacy path fails validation before SCP/SSH, never live network. Watch15s/
barrier5s/owned-task cleanup, no timeouts converted to success. Existing default
accessibility path inspected: it only submits an invalid origin, no new remote call.
Manual diff/default callsite/owner scope/fixture fallback/syntax3/C#5/public321/0/
secretsPII0 PASS; compilation/native2 NOT_RUN. This code is excluded from active619
and all prepared API-only cohorts; not published. Only this default HTTPS route is
now source-connected; wizard and invitation routes remain legacy. No dev/owner PASS.

Next exact619 result, then separate export API/read-dispatch/owned Admin API cohorts;
rebuild on any predecessor fix. Verify default route before broader view migration,
including phone-specific pending Admin gate while unrelated desktop/settings/Stop
remain independent. FullR09 FAIL/R11PARTIAL; I58–80/Y22/raw21.75/delta0. History below.

## Admin ownership/cleanup/worker checking isolated — dependent/unpublished

Code `85b634f32112b6a9b758b51ee08fce58ac28ab06`, treea527041049f8ff2b81cff4808a7845e034f5454c
in ProGo-admin-owned-api on unverified read/dispatch8c6a664, in turn export APIe263
on active619 local5c5ac7e. Three original commits e4c23e0/61b289b/a6a263a cherried
without conflict as80ceb31/e31784f/85b634f.3 paths351add12delete: owned task8 +retained
exact cleanup4 +owned core/UI-dispatch worker8 native cases. All normal views still
legacy; actual export UI/native chooser absent. Source/template method parity with
root, receipt body/wrapper preservation and NativeRecovery-last PASS.

Fullscope/manual/syntax3/C#5/public320/0/secretsPII0 (one exact reserved SSH fixture
false positive separately inspected) PASS. New compiler/native20 cases NOT_RUN.
No latest docs attached; no publication/CI for this branch. Must rebuild on any
predecessor correction and verify export API then Admin read/dispatch before sending
this cohort; do not replace active619 head. After Windows proof, bind wizard and
child invitations to each own window/dispatcher, then default HTTPS configure path;
add actual UI route tests and include pending Admin in phone-specific gating without
blocking unrelated desktop/settings/Stop. FullR09 FAIL/R11PARTIAL, scores unchanged.
History below.

## Admin read/dispatch API checking isolated — no view/owned-worker code

Code `8c6a664333811cf4e1a4086d278f1e004ae1e66e`, tree2c67aff52e87e178adc78fd8eb63f15144d89b4e
in ProGo-admin-read-dispatch on unverified export APIe263. Three original code commits
ca3d/819d/760 cherried as26203d6/8720ffc/8c6a664.3 paths193add1delete: readonly request/
key validation snapshot12 cases +transport Task ownership/owner dispatch5 modal cases.
Two cherry conflicts were context only: preserve NativeRecovery last with receipt
wrapper/body identical; retain only AdminTransportOwnership, excluding adjacent future
WizardProfileExport method and its calls. AST method-body parity with reviewed root
and prior receipt cases PASS; no changes to old diagnostic/owner-restore/IO metrics.

Scope/manual/syntax3/C#5/public320/0/secretsPII0/receipt+order parity PASS. Compilation/
native NOT_RUN; actual owned Admin wrapper/views/retained cleanup and export UI/native
chooser absent. Latest docs not attached; publication NOT_RUN and depends on exact619
result then export API Windows acceptance. Rebuild on any required predecessor fix.
I58–80/Y22/raw21.75/delta0. History below.

## Local owned Admin worker/transport binding API — views still unconnected

Source `a6a263afa76feb4740f879e6666200bd404e05b7`,2 paths133add3delete. New
AdminOwnedAsync freezes endpoint/key memory before scheduling instance-owned work.
Actual existing core validation/private DPAPI/journal/helper/result/cleanup runs on
that worker; only native copy/command startup and progress dispatch to the supplied
window's dispatcher. Uses explicit window, not ActiveForm. Recovery HasPending is
read on worker before UI callback. Memory-only service Stop/lifetime admission seals
new transports; admitted physical Tasks still settle and retain exact cleanup. No
lifecycle lock spans ShowDialog. Typed list/preparation/pending errors preserve
known/unknown remote outcome and prevent automatic replay.

Eight actual ShowDialog fixtures written around the real core and real local files,
with isolated copy/command transports: ready/changed owner/Close before copy/Stop
before factory/Close during/Dispose during/Stop during/Stop after copy. They require
snapshot identity, UI callback thread even after context loss, heartbeat, pending
actual I/O ownership, no late progress or new command after Stop, and bounded own
cleanup. Barriers5s/modal15s; not a claim of actual SCP or live VPS/native transport
integration (existing selected-key chain is separate). Static manual owner/admission/
lock/fixture cleanup/syntax2/C#5/public321/0 PASS. Secret/PII scan first flagged one
literal reserved SSH fixture target as email-shaped; exact fixture inspected, no
secret/personal data; remaining counts0. Local commit existed before completing this
false-positive review; no publication occurred. New compilation/native NOT_RUN.

Normal wizard/share/invitations still call legacy static route: API is unconnected,
wholeR09 FAIL/R11PARTIAL. Next validate isolated export API, then small Admin snapshot/
transport and owned-worker cohorts before view routing; each child window must use
its own dispatcher, not borrow the wizard owner. CI619 samef682/5d615 active, this
source excluded. I58–80/Y22/raw21.75/delta0. History below.

## Profile export API checking isolated — waiting for exact619 result

Code `e2630bd565fc3c634ad0323dd884c85ab6add159`, tree31b2f4825eead4dcd5a5c28308beaeefb70b1691
in ProGo-export-api on exact published619 local5c5ac7e. Original6d180fd +320a476
cherried as c4198fa/e2630bd without conflict.3 source/test paths196add2delete:
immutable profile XML worker5 native cases +service ownership/Stop9 cases. All real
FileMode.CreateNew/non-overwrite behavior remains; exact duplicates join, stale
pre-admission scopes cancel, accepted physical writes settle honestly after Cancel.
No actual wizard export/native profile chooser/Admin snapshot/dispatch/drain/cleanup
code included. Those future root changes remain preserved and NOT_RUN.

Final code-only manual diff/admission/Stop/fixture cleanup/callsite/syntax3/C#5/
public320/0/secretsPII0 PASS. New compiler/native NOT_RUN, no inherited618/619 proof.
Do not publish until same619 diagnostic outcome is known; if619 fails, fix its
established cause first and rebuild this cohort on that checked source. Latest3 docs
not attached/frozen yet. I58–80/Y22/raw21.75/delta0. Next same619; independent explicit
Admin owner transport wrapper can be prepared separately without changing active head.
History below.

## Local retained Admin cleanup — exact owner retry, production binding pending

Source `61b289b1b3ce27382688fa4d642b2eacfe83ef26`,2 paths103add9delete on local
unpublished service ownershipe4. Core Admin has an optional owning retention callback:
a failed directory deletion preserves its exact generated-directory closure before
the original typed cleanup error settles. Stop awaits every owned task then retries
only retained closures on its worker; still-locked cleanup returns false/remains owned,
unlock+Stop removes only its own tree without any copy/SSH/request replay. New owned
Admin factories are refused while cleanup remains. Work/upload directory creation
now sits inside the existing completion/cleanup envelope, covering partial preparation.
No broad directory scan/removal, no request/result format changes.

Four bounded native fixtures written around the actual core with isolated transports
and real helper files: complete/list lock/preparation lock/Dispose lock. They require
UI heartbeat, honest failed Stop, unlock+retry, unchanged original operation failure,
no new copy/command, unchanged protected files and an unrelated sibling directory.
Existing seven-family recovery/native order preserved. Manual diff/ownership/callsite/
syntax2/C#5/public321/0/secretsPII0 PASS; Windows compilation/native NOT_RUN.
Normal wizard/share/invitation Admin route is still UNCONNECTED to instance ownership
and still contains UI private I/O; full R09 FAIL. This source does not prove live SCP,
owner-VPS actions or complete UI cancellation. Next validate small API cohorts then
bind explicit owner-dispatched native transports before moving real Admin core to worker.
CI619 same f682/5d615 run39773542091 remains active, this source excluded. Scores
I58–80/Y22/raw21.75/delta0, exact617 last full evidence. History below.

## CI619 actual running — exact diagnostic publication verified

PR88 OPEN/DRAFT source `f682432f4afd53bc7b3e7c9142d60dcb42a6a22e`, merge
`5d615896c349051606e6212ed119b3a2cb8ce356`, tree7da67502463739fd3a427ebf7ebddc629679858c
= local5c5ac7e05da8fde49c66313c6c1779045d478f7a. Guarded c122→f682 CAS PASS;
actual PR/ref/merge independently checked. One automatic [CI619/39773542091](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39773542091),
Windows119806729629/Ubuntu119806729392 in_progress at verification. Initial empty
webhook lookup then one run; no manual duplicate. PR body updated.4 paths300add77:
only readiness diagnostic test19add8 +3 docs; public uploadsOFF/workflows unchanged.
New Windows compiler/native NOT_RUN pending exact completed log. Old618 partial
Desktop/IO success does not cover this SHA or its unexecuted Home modal routes.

Next inspect same619; independent exact-owned Admin cleanup retention foundation
before real worker/owner-dispatch binding. Local task trackinge4 remains unconnected
and NOT_RUN, as do all export/Admin future code. Last full617 I58–80/Y22/raw21.75,
no score growth. Do not push checkpoint-only docs while CI active. History below.

## Diagnostic source object f682 — ref-CAS not yet attempted at checkpoint

Source `f682432f4afd53bc7b3e7c9142d60dcb42a6a22e`, parentc122, full canonical tree
`7da67502463739fd3a427ebf7ebddc629679858c` = local5c5ac7e05da8fde49c66313c6c1779045d478f7a.
4 paths300add77delete: test-only readiness diagnostic19add8 +3 evidence docs.
Fullscope/manual/secretsPII0/public320/0/syntax/C#5/JSON86/fulltree PASS. Fresh public
repo/headc122/base4bb verified, workflows unchanged/uploadsOFF/release not triggered.
New compiler/native NOT_RUN. Ref-CAS NOT_RUN here: recover actual PR/run after any
interruption before retry. Next expectedc122→f682 forcefalse once, automatic exactCI.
No profile export/Admin code included; I58–80/Y22 same617 partial evidence.

## Frozen shutdown diagnostic — one source file on exact618 tree

Isolated source `f4659691b65b2ca957d9473b8cb75ff26069d6a1` in ProGo-ci618-diagnostic
on34aa/c122; only tests/ShutdownTests.cs19add8delete. Root original diagnostic
0292411e9be5608980797be15a5683f5acb47615 +d919cc5ab998c655ce81bf520a505656a7431664.
No production behavior change, readiness predicates/12s limit unchanged; new safe
boolean/listener state only. Full delta source/manual diff/public320/0 PASS; new
Windows compiler/native NOT_RUN. All local export/admin extensions excluded. Next
latest3 evidence docs, full secret/PII/static/canonical-tree check, guarded c122→new
source CAS once.618 failure and617 last full SUCCESS remain separate. I58–80/Y22
unchanged. This pre-publication freeze is history once actual ref/run is verified.

## CI618 FAIL — shutdown startup predicate unknown; narrow diagnostic next

Exact published source `c12200870264832f6ad6d7a325ef19678ee1447e` /merge
`dbed93e150be5eeafb274e88826389aacff78d0a`, run39769965390 Windows119794736280 FAIL,
Ubuntu119794735960 SUCCESS. Completed log checked: build identity11:21:49.067,
Desktop5287 PASS11:40:12.906, Instance28/Maintenance126 PASS. Four actual direct/agent
startup/recovery child-lifetime process I/O collections PASS11:38:55–11:39:02,
with latency/preparation/CPU/RAM and six counters; not full-app/physical-disk or
owner-PC performance acceptance. Full Test-ProGo FAIL at ShutdownTests11:43:44.509:
`Timed out: external-edit fixture fully applied`, third external startup after
attempts1/2 passed. Existing12s bound. Last startup logged no CLI/Windows completion;
individual alive/journal/windows/environment/logged/listener flags were not emitted,
so exact underlying cause is UNKNOWN, not a proven product regression or flake.
HomeVPN wizard new11+5 modal cases and all later suites NOT_RUN because execution
stopped before their stage. Package NOT_RUN; uploads SKIPPED. No618 SUCCESS claimed.

Local narrow diagnostic `0292411` plus scope-preserving probe follow-up
`d919cc5ab998c655ce81bf520a505656a7431664` adds only individual boolean readiness and
owned SOCKS listener Task/error-type facts on timeout. All existing criteria/12s
budget and original guarded port probe retained; no rerun of same SHA, no fallback
PASS. Next isolated checking from34aa/c122, these test edits +latest evidence docs
only, excluding e4 Admin foundation and all profile export/admin helpers. Recheck
scope/secrets/workflows/fulltree, publish once then inspect exact newCI. Windows
compiler/native for this new diagnostic NOT_RUN. CI618 failure preserved below.
I58–80/Y22 exact617 partial evidence unchanged, delta0; no new full weighted gate.

## Local Admin task accounting — foundation only, not yet wired

Source `e4c23e01f2c216162ccf4f5ba3ab535dccb6e122`,2 paths117add2delete. Service
reserves each Admin completion before worker scheduling, checks Stop/lifetime before
factory admission, retains every accepted actual body, and drains their snapshot in
public Stop/Dispose even when an individual body faults. Original typed cancellation/
recovery errors remain on that operation. New factories are refused during Stop;
independent accepted families can coexist. HasOwnedResources includes pending work.

Eight bounded real-file native fixtures written: complete/Stop/Dispose/real lock
failure/typed recovery cancellation/queued Stop/queued Dispose/two accepted bodies.
They check UI heartbeat, immediate Stop return, one drain, no unstarted factory,
physical settlement, original error and own-file cleanup. Static manual diff/lifetime/
syntax2/C#5/public321/0/secretPII0 PASS; compiler/native NOT_RUN (Linux no .NET/Windows).
Actual static AdminAsync/user routes remain UNCONNECTED: this foundation does not
close R09, prove SCP/server effects, or retain/retry a failed work-directory cleanup.
Next separate exact-owned cleanup retention, then explicit owner-dispatched production
transport and worker binding; no whole method Task.Run over native UI callbacks.

CI618 same c122/dbed run39769965390 still Windows in_progress at last check; Ubuntu
SUCCESS, new native result pending. Published tree unchanged; this source excluded.
I58–80/Y22/raw21.75/delta0; full R09 FAIL/R11PARTIAL. History below.

## CI618 actual running — wizard/IO candidate published

PR88 OPEN/DRAFT source `c12200870264832f6ad6d7a325ef19678ee1447e`, merge
`dbed93e150be5eeafb274e88826389aacff78d0a`, tree205e6e6e5583d669e03664ea44eafa9749c2a829
= local34aa821d1e86fc86c5d381a6bad337d28ef76068. CAS expectedf255→c122 PASS,
один автоматический [CI618/39769965390](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39769965390),
Windows119794736280 in_progress/Ubuntu119794735960 SUCCESS atverification. Initial
empty webhook lookup then one run, no manual duplicate. PR body updated.13 paths
957add182delete,10 source/test477add68 +3docs; fullscope/secretsPII0/syntax10/C#5/
public320/0/JSON86/canonicaltree PASS, workflows/VERSION unchanged/uploadsOFF.
New compiler/native NOT_RUN pending exact Windows log, no inherited617 result.
Later profile export/admin snapshot/UI-transport code excluded, their Windows NOT_RUN.

Last full617 sourcef255/merge7f720 remains evidence/package/28partial weighted criteria,
I58–80/Y22/raw21.75/delta0. Next same618 exact result; do not change active head for
checkpoint-only docs. Independently add service tracking/drain of accepted Admin
Tasks before actual static AdminAsync worker migration, then retained-cleanup routing.
Do not claim global task accounting already connected to the user route. History
below; object-before-CAS entries are not current incomplete publication.

## Wizard/IO checking object c122 — before guarded ref-CAS

Source object `c12200870264832f6ad6d7a325ef19678ee1447e`, parent
`f255e8ad7339af4481e0590c31c7db32ab959bfe`, tree `205e6e6e5583d669e03664ea44eafa9749c2a829`
= isolated local `34aa821d1e86fc86c5d381a6bad337d28ef76068` in ProGo-ci617-wizard.
13 paths957add182delete:10 wizard/test477add68 +3 evidence docs. Full secretPII0/
public320/0/syntax10/C#5/diff/JSON86/exclusions/canonicaltree PASS; service/access/
recovery source, scripts/server/workflow/VERSION unchanged. Freshpublic/headf255/
base4bb checked, uploadsOFF;617 completedSUCCESS, not repeated. New compilation/
native NOT_RUN;11+5modal cases and4 I/O collections still unexecuted. Entire profile
export/admin snapshot/transport code excluded. Ref-CAS NOT_RUN at checkpoint:
recover actual PR/run before retry after interruption. Next expectedf255→c122,
forcefalse, one automatic exactCI. I58–80/Y22 exact617 unchanged. History below.

## Wizard checking rebuilt on617 — code89e static-only; F31 anchor corrected

Runtime/test source `89e44d5ea0c9226a0b1444921919049be8043631`, tree3200f55fcf5202d269ff9f226f70dc09b299d307
in ProGo-ci617-wizard, on verified617 localf412/sourcef255.10 paths477add68delete:
3 wizard/context/card +7 tests, async import/receipt, initial cached setup snapshot,
first-success owner restore preserving edits,11 import/receipt+5 initial modal routes,
actual owned SSH child I/O counters4. Only Run call-order cherry conflict resolved;
receipt diagnostic, own3file restoration and terminal NativeRecovery-last preserved.
Service/access/recovery source, scripts/server/workflows/VERSION unchanged. All later
profile export/native profile chooser/admin snapshot/transport helpers excluded.
Manual scoped diff/call-site/conflict/receipt-parity/syntax10/C#5/public320/0/secretPII0
PASS; compiler/native NOT_RUN. Docs still predecessor in worktree until final freeze.

Evidence correction without points/weight change: current F31 representative score
anchor had unrelated `Update check tests PASS:72`. Replaced with exact617 native
`real recovery repeats local signing and actual server authentication against the
same selected owned agent key` (same completed log), matching original F31 table's
signing/UI/SSH basis. Historical entry retained in JSON correction record. Underlying
partial gate, v1.0 weights/I58–80/Y22 raw21.75 unchanged; written metrics not credited.
Next attach latest3 evidence docs, whole delta scan/public workflow/canonicaltree/CAS,
one exact newCI.617 fullSUCCESS applies only to f255/7f720. History below.

## Prepared wizard retry fix3c9 — restore pending endpoint, preserve edits

Source `3c9ddccaf3158d74a858ec9d6b6283603f0fb7f0`,2 paths50add14delete. Finding in
staged a0/d940 (NOT published617): initial snapshot cancellation leaves draftOwner
from saved service owner; successful noninitial retry previously updated cache only,
so an unrelated pending VPS could retain the wrong endpoint/key fields. Reproduction:
saved other-account.example:2222/different +pending vpn.example.org:22/root; Cancel
initial read→actual Back/own-VPS retry. No Windows reproduction claimed; source route
and new native regression define exact scenario.

First successful snapshot now restores untouched draft/fields from pending owner,
borrowing saved key only for matching host/port/login. A field-edit flag prevents
replacing new user edits during first read/retry; programmatic restoration does not
mark them edited. Updates remain cached/no I/O, and an unchanged draft can be restored
while another page is visible. No remote action/format/credential policy changed.
Five native initial ShowDialog routes written (adds cancel-edited-retry to4); force
different saved owner/key, hold first retry while user types, assert correct pending
prefill or preserved edits. Explicit subsequent own-VPS recheck preserves edited
fields and adds exactly one read; typing/status still tested under real request-file
exclusive lock. Barriers5s/modal15s/cleanup unchanged. Static manual route/key-scope/
diff/syntax2/C#5/public321/0/secretPII0 PASS; compiler/native NOT_RUN, excluded617.

Next rebuild isolated wizard on checked617 with diagnostic/order/input restoration
preserved; include only reviewed wizard changes +test-only process I/O metricsf9,
exclude all profile export/admin snapshot/adapter code. Resolve any cherry-pick Run
line conflict by keeping NativeRecovery terminal. Before one newCI: exact scope/
fulltree/secrets/workflow check. I58–80/Y22 exact617 unchanged; wholeR09 FAIL/R11PARTIAL.
History below.

## CI617 SUCCESS — exact import/receipt API cohort, future UI excluded

Exact source `f255e8ad7339af4481e0590c31c7db32ab959bfe`, merge/build identity
`7f72092c47f68617f9fec6bf1b7c2dc273c9f426`, tree323f022e49ac2b485e6d9c32f528ffd0d6822a98
= localf41257f78ba1e8fb4166257c2db9012be16a0fd7.
[CI617/39763459020](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39763459020)
Windows119773047477/Ubuntu119773046886 SUCCESS; completed jobs/log checked. Build
10:29:34.0880510Z, full Test-ProGo PASS10:47:50.1596462Z, Desktop5283/Home1805/
Portable91, Python28=26PASS2SKIP. Pair9/import9 PASS and all7 real receipt worker cases
PASS10:51:26.400–27.174Z: complete/cancel-read/cancel-consume/scope-refused/real delete
lock/changed-request/changed-credentials. Terminal recovery after these also PASS,
confirming corrected fixture ordering/state restoration without a timeout increase.
Full configure initial/readmodal3+3/private-save8, diagnostics modal/chooser3/Context
ownership, selected-key signing55/UI50/prep72, actual direct+owned-agent native SSH→
SOCKS→HTTP/crash→fresh preparation, Instance28/Maintenance126/Shutdown47 also PASS.

Same28 weighted criterion anchors match613 verbatim: I58–80/Y22 raw21.75, delta0.
This validates the published API cohort, NOT future wizard a501/839/a0/bbcc/d940,
profile export6d180/320a/1edef/4c1/0b5, process I/O metricsf9 or admin snapshotca3d/
transport819d9+760. Their compiler/native remain NOT_RUN. Published wizard still uses
sync compatibility importer; full R09 FAIL and R11 PARTIAL, owner/installer gates open.
Next before isolated wizard publication: fix first-success setup snapshot retry
restoring stale saved-owner endpoint for a different pending VPS, preserving edits;
then rebuild on checked617 with diagnostics/order fix retained and review scope.

Runner ZIP0.2.2 SHA256 `91f617b9b9d09ed460aab74c05e12fde6b267994642c2cae77094bc46c6b17d7`.
All upload/prune steps SKIPPED; bytes not retrieved/delivered/installed. Private
transfer BLOCKED pending approved channel; no main merge/release/owner-PC install.
History below; previous running/object states are historical, do not repeat617.

## Admin UI transport adapter819d9 + fixture publication76076 — NOT_RUN

Source `819d9b2cf6e9e4799427b3e4876db47bff5900b1`:2 paths85add0delete. Internal
DispatchAdminTransportAsync uses existing HomeProfileUiDispatcher only to START a
copy/command callback on the owner thread; then awaits its actual Task independently
of SynchronizationContext/owner closure. Closed owner rejects an unstarted callback;
already admitted work preserves its real completion/error (needed before retaining
server result/cleanup). Null dispatcher is only for worker-compatible injected
transports. Caller still owns window dispatcher and existing admin family lease;
helper creates no transport/process/lease itself and is not wired into production yet.

Five actual ShowDialog context/lifetime fixtures written: ready/before-dispatch
Dispose/during-I/O Dispose/real locked destination fault/worker-only transport.
Real local file I/O and held worker, lost UI context, closed dispatcher publication
count, no late callback, original error and actual admitted completion;15s modal/5s
boundaries+join. These prove the adapter contract only, not a live SCP/VPS integration.
Review followup `76076aacf8bb57864197f70864a3d318968e9eff` (1 path4add1delete) adds
explicit publication barrier: I/O entered signal cannot race physical Task reference
assignment in worker-only route. Both owned barriers are released during cleanup.
Static diff/manual lifetime/publication review/syntax/C#5/public321/0/secretPII0 PASS;
Windows compilation/native NOT_RUN, excluded617 and wizardd940. No score credit.

Before actual AdminAsync disk migration, its accepted work and retained cleanup must
also be reachable from app/service shutdown (current static route is not owned by
HomeVpnService.StopAsync). Confirm full existing shutdown path before adding any
registry; do not mistake a manually deleted fixture helper for application cleanup
retry. Next prioritize a source regression found in prepared wizard: after cancelled
initial snapshot, first successful retry keeps stale saved-owner endpoint if a pending
request belongs to another VPS. Add guarded first-snapshot prefill without replacing
user edits, and an exact native case before publishing the wizard cohort. Same617
running, no duplicate or changed head. I58–80/Y22 exact613/fullR09FAIL/R11PARTIAL.

## CI617 actual running — fixture order/state fix only

PR88 OPEN/DRAFT source `f255e8ad7339af4481e0590c31c7db32ab959bfe`, merge
`7f72092c47f68617f9fec6bf1b7c2dc273c9f426`, tree323f022e49ac2b485e6d9c32f528ffd0d6822a98
= localf41257f78ba1e8fb4166257c2db9012be16a0fd7. CAS expected5ee→f255 PASS,
один автоматический [CI617/39763459020](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39763459020),
Windows119773047477/Ubuntu119773046886 in_progress. Empty initial webhook lookup
followed by one run; no manual duplicate. PR body updated.4 paths343add47delete,
only1 fixture11add1delete+3docs, product runtime/scripts/workflow/VERSION unchanged,
fullscan/canonicaltree PASS/public uploadsOFF. New compilation/native NOT_RUN until
exact log. Future wizardd940/export/metricsf9/adminsnapshotca3d excluded and NOT_RUN.
Last fullSUCCESS613/28anchors/runnerZIP remain current evidence; I58–80/Y22/delta0.
Next check same617; do not publish checkpoint-only docs on active head. Independent
next stage: task-preserving UI transport adapter using existing owner dispatcher,
then wire Admin snapshot/staging without moving modal windows onto worker threads.
Historical pending-CAS notes below are not a new unfinished publication.

## Fixture-order object f255 — before guarded ref-CAS

Source object `f255e8ad7339af4481e0590c31c7db32ab959bfe`, parent
`5ee7f1eb2545abef5ba03a1ef2f0e56253f55eaf`, tree `323f022e49ac2b485e6d9c32f528ffd0d6822a98`
= isolated local `f41257f78ba1e8fb4166257c2db9012be16a0fd7` in ProGo-ci616-fix.
4 paths343add47delete:1 fixture11add1delete +3 evidence docs. Full secretPII0/
public320/0/syntax/diff/JSON86/fulltree PASS; runtime/scripts/workflow/VERSION
unchanged. Freshpublic/head5ee/base4bb checked, uploadsOFF;616 completed FAIL,
no same-run retry. New Windows/native NOT_RUN; future wizard/export/IOmetrics/admin
snapshot excluded. Ref-CAS NOT_RUN at checkpoint: inspect actual head/run after
interruption before retry. Next guarded expected5ee→f255 forcefalse, one automatic
exact CI. I58–80/Y22 last full613 unchanged. History below.

## CI616 FAIL — receipt worker held correctly, UI timer did not tick

Exact source `5ee7f1eb2545abef5ba03a1ef2f0e56253f55eaf`, merge/build identity
`e9b8b681b07d1e480bc309303fa52f8508425694`, treeb26fca3af636009cb33fca50d5ec561afc546b34.
[CI616/39758749192](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39758749192):
Windows119757324701 FAIL, Ubuntu119757325146 SUCCESS, completed jobs/log checked.
Build09:52:36.6364113Z; Desktop5283/full Test-ProGo PASS10:12:15.5275318Z. Previous
pair9/import9 and regressions reached. New receipt preconditions10:16:07.1234579Z:
access=True/owner=True/request=True. Failure10:16:10.1283004Z:
`mode=complete; stage=2; entered=True; ticks=0; status=Running`. Actual worker passed
private reads/RequireSavedImport and is held at its intentional consume barrier;
missing condition is native heartbeat, not evidence of a product I/O deadlock.
Remaining receipt7/Home tail/Portable/Python tail NOT_RUN, package/uploads SKIPPED.
Last fullSUCCESS/scoring/package613 only; no616 credit.

Confirmed fixture-order defect: tests/HomeVpnSetupRecoveryTests.cs::Run appended
OwnedConsumption after NativeRecovery; NativeRecovery starts Application.Run and
calls loop.ExitThread. tests/HomeVpnWizardTests.cs::Main already documents that this
terminal recovery must run after all modeless timer checks. New fixture violated
that lifetime contract. Fix `7f1d6f14c7a6ef3bff26f9ff83eeaacfe7b63170`, isolated
`cf4dbce3f78b7d39b7cb87c7c1ad00d6e046af96` on checking9829:1 test path11add1delete.
All timer fixtures now precede terminal NativeRecovery. OwnedConsumption wrapper
also restores its own access/owner/request bytes before subsequent scenarios: its
last changed-credentials case otherwise intentionally leaves invalid access/pending
request and would pollute reordered recovery. Original seven-case body byte-identical,
3s/5s budgets and every assertion unchanged. Static diff/syntax/public321/0(root)/
secretPII0 PASS; new Windows/native NOT_RUN. Exact new CI must confirm causal repair;
no claim old615 individual flags were recorded or exclusive OS mechanism measured.

Next isolate only this test +3 current evidence docs, runtime/scripts/workflows/
VERSION unchanged, public uploadsOFF, canonicaltree/CAS on5ee; one new exactCI,
not a rerun616. Future wizardd940/export/metricsf9/adminsnapshotca3d excluded and
NOT_RUN. After full success rebuild wizard on the latest checked diagnostic/order
base; preserve this terminal ordering when adding future native fixtures. FullR09
FAIL/R11 PARTIAL, I58–80/Y22 exact613 unchanged. History below.

## Admin request snapshot ca3d — read-only primitive, production still sync

Source `ca3d2f3e18bea39a112d068e082a87d16c4a9b09`,2 paths105add1delete. New
HomeVpnAdminRequestSnapshot.LoadAsync freezes owner/action/label and runs key-file
existence validation plus actual setup/admin/share protected reads and existing
pending/recovery refusals in worker. No register/consume/staging/remote command or
lease is created/transferred. Caller keeps its EXISTING family lease through actual
read and later commands/cleanup, so owner-loss awaits cannot discard a returned
lease resource. Existing AdminAsync is not yet rewired: its sync validation/reads/
staging/copies/register/output reads remain an open R09 defect; default setup copy
still does extra HasPending on UI. No claim this primitive fixes the user route.

Twelve native worker vectors written: setup/invite/share/empty; missing recover3;
pending new invite/share2; cancel; corrupted request; missing selected key. Real
DPAPI/request IDs/host/cipher bytes, held worker/native heartbeat, changed caller
owner after capture, actual family lease retained until join.3s heartbeat/5s barrier/
join, no VPS/SSH/system changes. Static manual parity/input/lease contract review,
syntax2/diff/public321/0/secretPII0 PASS; Windows compilation/native NOT_RUN, excluded
current616 and wizardd940. I58–80/Y22 exact613/delta0; fullR09 FAIL/R11 PARTIAL.

Next split: (1) preserve UI dispatch/lifetime of interactive copy+command transports
through existing HomeProfileUiDispatcher, retaining actual admitted Task outcomes;
(2) wire this request snapshot and stage helpers in worker; (3) move remaining
register/result/read/receipt writes; (4) exact native regressions. Do not place all
AdminAsync in Task.Run and create modal windows on pool threads. Same616 first:
Windows build step PASS, integration running, UbuntuSUCCESS; exact receipt flags/log
pending, no rerun or changed checking head. History below.

## F31-R11 native process I/O collection f9f2789 — evidence NOT_RUN

Source `f9f27896d5830dc75e7d9ba526f105446514456a`,1 test path29add2delete, runtime/
workflow/scripts unchanged. Existing real native direct+owned-agent SSH→SOCKS→HTTP
startup/recovery metrics now read Win32 GetProcessIoCounters from the exact tracked
child and emit six operation/byte totals with `ssh_io_scope=child_lifetime` alongside
latency/preparation/CPU/working-set. These are cumulative process I/O counters, NOT
physical-disk traffic or totals for app/UI/agent/server. Each recovery uses a new
child; no cross-child subtraction, no arbitrary performance ceilings added. Missing
native counter collection logsNOT_RUN and fails a separate required dev assertion
outside catch (Check failures cannot be swallowed as collection success).

Primary signatures reviewed: [GetProcessIoCounters](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-getprocessiocounters)
and [IO_COUNTERS](https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-io_counters).
Six ULONGLONG fields/BOOL return/SetLastError; no system settings, privileges, process
termination or live resource introduced. Static manual API/layout/scope review/
syntax/diff/public321/0/secretPII0 PASS; Windows compilation/Win32 execution and
actual values NOT_RUN. Excluded current616 and wizardd940. Final slow hardware/UI/
full-app I/O/owner acceptance remain open, F31-R11 PARTIAL. I58–80/Y22 exact613/delta0.

Same61639758749192 Windows119757324701 Test-ProGo running, Ubuntu119757325146 SUCCESS;
no new push/duplicate. Next independent source trace: default AdminAsync copy
classification still invokes HasPending() from its UI copy callback for setup;
check existing worker preparation metadata before a minimal migration. Do not
invent a replacement recovery/PIN policy or weaken cancellation wording. History below.

## CI616 actual running — receipt-only diagnostic published

PR88 OPEN/DRAFT source `5ee7f1eb2545abef5ba03a1ef2f0e56253f55eaf`, merge
`e9b8b681b07d1e480bc309303fa52f8508425694`, treeb26fca3af636009cb33fca50d5ec561afc546b34
= local9829d870db750c2e32ebd11c60aefb8aac21844a. CAS expected6f8→5ee PASS,
один автоматический [CI616/39758749192](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39758749192),
Windows119757324701/Ubuntu119757325146 in_progress. PR body обновлён.4 paths493add31delete,
only1 receipt test16add2 +3docs, runtime/scripts/workflows/VERSION unchanged,
fullscan/tree equality PASS/public uploadsOFF. New compilation/native NOT_RUN;
615 cause UNKNOWN pending stage/tick/task/input flags. Do not rerun615 or duplicate616.
Future wizard d940 and all profile export source excluded; their native NOT_RUN.
Last fullSUCCESS613/28anchors/runner ZIP remain current evidence, I58–80/Y22/delta0.
Next same616 exact result and minimal causal fix. Independently F31-R11 native
resource report lacks process I/O counters; inspect existing owned SSH metrics scope
and add only that missing test evidence if supported, no product concept change.
Historical object-before-CAS notes below are not current unfinished publication.

## Receipt diagnostic object5ee — before guarded ref-CAS

Source object `5ee7f1eb2545abef5ba03a1ef2f0e56253f55eaf`, parent
`6f8b200cd34910ee167b44e6dab661c86cb506aa`, tree `b26fca3af636009cb33fca50d5ec561afc546b34`
точно равен isolated local `9829d870db750c2e32ebd11c60aefb8aac21844a` в ProGo-ci615-receipt.
4 paths493add31delete:1 receipt fixture16add2delete +3 evidence docs. Full secretPII0/
public320/0/syntax/diff/JSON86/fulltree PASS, product/runtime/scripts/workflow/VERSION
unchanged. Freshpublic/head6f8/base4bb checked, uploads OFF;615 completed FAIL,
no same-run retry. New compilation/native NOT_RUN; cause615 UNKNOWN. Entire future
wizard/export excluded. Ref-CAS NOT_RUN at checkpoint: inspect actual PR/run after
interruption before retry. Next expected6f8→5ee forcefalse, one automatic exact CI.
I58–80/Y22 last full613 unchanged. History below.

## Receipt fixture diagnostic8342 — unchanged budgets, new evidence pending

Source `8342cef684081d2b9e214173dd9c87560dab45f7`, isolated
`5739c916b6384fa6e76a81f28fdbb58708c9f83f` on exact615 checking63dae in
ProGo-ci615-receipt. One test path16add2delete, product code unchanged. Checks actual
saved access/owner/request preconditions (only booleans in log); observes read/consume
stage and premature Task completion before expected held barrier; timeout prints
stage/entered/tick delta/TaskStatus, failure type only (no message, path, token/key).
3s heartbeat/5s worker+join unchanged; no check disabled, no timeout treatedPASS.
Cause615 remains UNKNOWN until actual new runtime values. Static manual diff/
syntax/public321/0(development)/secretPII0 PASS, Windows compilation/native NOT_RUN.

Next publish only isolated test +3 evidence docs on source6f8 after scan/workflow/
fulltree/CAS; isolated tree has320 sources, excludes entire wizardd940 and all
profile export6d180/320a/1edef/4c1/0b5. One new exact diagnostic CI, no615 rerun.
I58–80/Y22 exact613, delta0. After confirmed diagnosis, minimal fix before carrying
prepared wizard to a new checked base. All future exports stay independently saved,
not included or accepted. History below.

## CI615 FAIL — setup consumption heartbeat ambiguity; no blind rerun

Exact source `6f8b200cd34910ee167b44e6dab661c86cb506aa`, merge/build identity
`2c7db226b9705367574ca6806b550e6e40b59cd0`, treec3efc70354edb1b9b880af6e78d29153a78a9b4c.
[CI615/39753790118](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39753790118)
Windows119740579385 FAIL, Ubuntu119740579716 SUCCESS, completed jobs/log checked.
Build09:14:05.8795949Z; full Test-ProGo PASS09:36:51.8168291Z/Desktop5283. Import pair9
PASS09:37:14.265–15.333Z, service import9 PASS09:37:15.555–16.929Z incl real partial
save/cleanup retry/stale access/acceptedStopDispose. Previous configure/UI/key/direct+
agent regressions reached. At09:41:09.246Z first new receipt `complete` returns owned
task;09:41:12.256Z `Setup consumption heartbeat deadline`. Its loop ignores early
operation completion and prints no stage/timer/task state: actual reason UNKNOWN,
not yet a confirmed product deadlock or timer defect. Existing3s deadline remains.
Receipt7 criteria not passed; Home suite tail/Portable/Python tail NOT_RUN, package/
uploads SKIPPED. Last fullSUCCESS/scoring/package remains613, no615 credit.

Next narrow test correction: assert exact real saved request/owner/token preconditions,
record read/consume stage and detect unexpected task completion before held barrier;
on timeout print safe booleans/counters/type only. Do not print credentials/paths or
increase timeouts. Isolate on615 checking63dae, exclude wizard d940 and profile export
work; one new diagnostic SHA/CI is justified by new evidence, not same-run retry.

Independent native profile chooser fixtures source `0b5ca87f65ed20b687864d36ef519874e191bf66`:
4 paths116add1delete (new DesktopProfileNativeChooserTests +entry/list/call). Uses real
production profile button, outer ShowDialog and nested native SaveFileDialog; existing
PID/thread/root-owner helper unmodified. Owned child/job20s, native5s; positiveCancel
must return0, real missing window fails; deliberately unmatched caption/error fixtures
must return20/21 with exact failure and cleanup marker. Cleanup checks native window
absent before owner disposal, joins retained worker/service tasks and unchanged private
input snapshot, closes only its own windows/processes. Clipboard unused, settings not
touched. Static syntax3/diff/public321/0/secretPII0/explicit compile list PASS;
PowerShell execution/Windows compile/native NOT_RUN. Excluded615/wizardd940. FullR09
FAIL/R11 PARTIAL, I58–80/Y22 exact613 unchanged. History below.

## Modal profile export regression4c1 — eleven scenarios written, NOT_RUN

Source `4c1b1840fa9b9077550b550d7adfbc69fa4d9cb5`,1 test path84add0delete. Eleven
actual ShowDialog export scenarios: save, Cancel→Retry, Close/Dispose, accepted
Cancel/Close/Dispose, global Stop before/after admission, real locked existing
file→new-name Retry, accepted old-access completion. Tests call the same ExportProfile
helper used after native chooser; they do NOT prove profile chooser interaction.
Actual XML/file outcomes/one duplicate task/accepted saved messages/unchanged prior
file/zero late disposed control delta/current access scope and separate issuance/
installation state asserted.15s modal +5s barriers/task cleanup; only own GUID outputs,
service work and private fixture backup restored after settlement. No live SSH/VPS.

Manual diff/lifecycle trace/syntax/public320/0/secretPII0 PASS; Windows compilation/
native NOT_RUN, excluded615 and frozen wizard d940. Native profile SaveFileDialog
Cancel/missing/error fixture remains next, distinct from diagnostic chooser3 already
verified613. No score increase: I58–80/Y22 exact613; wholeR09 FAIL/R11 PARTIAL. Next
reuse existing PID/thread/root-owner native chooser helper in isolated owned child,
with5s useful failure/20s outer bound and cleanup, without loosening REVIEW02.
Same615 running; no source-head/doc-only push or duplicate CI. History below.

## Profile export actual UI1edef — wiring saved, modal regression next

Source `1edef0f56e6dff3fe4eaa0713206e8f0c52aeca1`,3 source paths51add13delete.
Existing SaveFileDialog stays on UI, only captures selected path; File.Exists and
WriteProfile no longer run there. Actual ExportProfile runs under existing RunStep
and retained form task, calls service-owned ExportProfileAsync with captured access/
home/public Stop scope. Explicit Cancel is DialogResult.None; Cancel→Retry keeps the
modal, Close waits own work, Dispose closes UI admission. Accepted writes report
saved even after Cancel; file issuance resets installation confirmation only for
matching current access/home, never claims installed. Output error text does not
echo selected/private path. Phone Context/card uses IsExportingProfile as existing
saving gate; Desktop/settings/Stop remain independent. Event callback has no owner-
await after explicit close (no controls after ConfigureAwait(false)), avoiding an
unhandled owner-loss cancellation in asyncvoid after a successful physical save.

Manual diff/actor-shutdown trace/syntax3/C#5/public320/0/secretPII0/call-site scan PASS;
no synchronous file probe/write in actual SaveProfile path. Windows compilation/
native NOT_RUN. Native modal export helper regression is next; actual profile
native chooser selection also NOT_RUN, diagnostic chooser3 evidence613 is not
relabelled as that scenario. Excluded615 and frozen wizard d940, API/worker6d180/
320a also NOT_RUN. Whole R09 FAIL/R11 PARTIAL, I58–80/Y22/delta0. Same615 running,
no duplicate or checkpoint-only push. History below.

## Profile export actor320a — public Stop/Dispose ownership, UI pending

Source `320a4762765819105eeb1dcd576d0dcfc752866a`,2 paths126add2delete, builds on
primitive6d180. Service publishes ProfileExportWork/CTS/frozen access+home/path before
worker I/O; exact same input/token/owner callback joins one task, different request
refused. Memory-only admission checks owner, selected access/home, import/address
work, public Stop identity and lifetime. Before-admission Cancel/Stop/Dispose suppress
write; accepted export finishes and its result remains on ProfileExportWork. Public
Stop cancels off UI and joins physical export even if channel cleanup failed; app's
existing PrepareShutdown already waits this Stop with its own bounded hold-open
policy. No new forced native I/O termination or timeout-as-success. Service actor
has no control access or remote SSH action. Atomic memory cleanup/completion prevents
an observer seeing a cleared request with an unfinished public task.

Nine native service fixtures written: save/cancel/stop/dispose/accepted-stop/
accepted-dispose/stale-access/stale-home/owner-refuse. Actual XML file, private bytes
unchanged, exact duplicate/parallel refusal, UI heartbeat and public Stop join;
5s barrier/task cleanup, no external process/VPS. Manual diff/ownership trace/
syntax2/C#5/public320/0/secretPII0 PASS. Windows compilation/native NOT_RUN; excluded
615 and wizard d940. Actual SaveFileDialog/SaveProfile still synchronous, UI wiring
and modal regression are next separate stages. No full R09/R11 PASS. I58–80/Y22
exact613, delta0. Same615 running, no duplicate/head change. History below.

## Profile export worker primitive6d180 — UI wiring still pending

Source `6d180fd7839abe132c08abb552d25723caaa57cc`,2 paths70add0delete. Existing
SaveProfile→WriteProfile UI I/O finding remains open; this stage prepares the worker
API only. WriteProfileAsync captures a shallow immutable snapshot (sealed access
contains only strings/value types), fixed path/home strings and one memory-only
admission boundary. Cancellation/stale admission before writing creates no output;
after admission the physical save finishes and reports its actual result. Existing
WriteProfile FileMode.CreateNew no-overwrite rule/XML format unchanged. No claim
of crash-atomic export, UI integration, bounded remote filesystem completion or
full R09 acceptance.

Five native worker fixtures written: save/cancel/refuse/actual locked existing file/
accepted-cancel. Held worker requires native heartbeat, mutates original access to
prove captured XML, reads actual generated XML, checks real preserved existing bytes,
and joins own task before deleting only its own GUID output.5s barrier/task cleanup.
Manual diff/syntax2/public320/0/secretPII0 PASS; Windows compilation/native NOT_RUN,
excluded615 and frozen wizard d940. I58–80/Y22/delta0. Next determine application/
window shutdown ownership before wiring actual SaveFileDialog→worker (separate
microtask), preserving truthful issuance vs installation and no duplicate export.
Same61539753790118 remains running: UbuntuSUCCESS, Windows Test-ProGo step running.

## Future wizard UI candidate d940 — source frozen, NOT_RUN

Local `d9405816e6bb54c7adb21f1d442dcf33af5cb2ac`, tree52db924bfca7ff4d24976bbb724ed21bf297b090
в ProGo-ci615-wizard, на checking63dae01967394a25ad735b17353bd50cddeb67d8 (published
source6f8/CI615).9 source/test paths412add66delete: captured async import/receipt,
owned initial/status setup snapshot, Context/card loading gates,11 import/receipt
ShowDialog +4 initial snapshot modal routes and all existing fixture readiness.
Полное дерево runtime/tests/scripts/server/workflows/VERSION совпадает с текущим
локальным development (diff0); будущая публикация не должна потерять compile-fixbc9.
Manual scoped diff/call-sites/syntax9/C#5/public320/0/secretPII0 PASS; compilation/native
NOT_RUN. Docs здесь ещё predecessor snapshot, не готовый public freeze.615 head
не менять. Дождаться exact615 результата; затем актуальные3 evidence docs/full scan/
canonical tree/CAS перед единственным следующим CI. I58–80/Y22 exact613 unchanged.

## CI615 running — exact narrow compile fix, future wizard separate

PR88 head `6f8b200cd34910ee167b44e6dab661c86cb506aa`, merge
`2c7db226b9705367574ca6806b550e6e40b59cd0`, treec3efc70354edb1b9b880af6e78d29153a78a9b4c
= local63dae01967394a25ad735b17353bd50cddeb67d8. GuardedCAS0bfa→6f8 PASS, один
автоматический [CI615/39753790118](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39753790118),
Windows119740579385/Ubuntu119740579716 in_progress. Первоначально пустой webhook lookup
затем один run; ручного дубля нет.4 paths149add83delete (source call1/1 +3docs),
full secretsPII0/tree equality PASS, workflows/VERSION unchanged/public uploadsOFF.
PR body обновлён. Новая compilation/native NOT_RUN до exact completed log;614FAIL
сохранён. Last fullSUCCESS613 — только его SHA/ZIP/28anchors, I58–80/Y22/delta0.

Независимый local `bbcc3356835ff08df01199990b1d67d7313ab757` (1 test path10add4delete)
адаптирует будущие modal import Dispose assertions к подтверждённому613 native-unwind
интервалу: перед release worker требуется завершение ShowDialog unwind, закрытый
OwnerUi publication gate и всё ещё удерживаемый импорт; после actual I/O нулевой
прирост control-events относительно измеренной native baseline. Все параметры
выводятся в assertion. Никакой проверки не удалено/таймаут не увеличен. Ожидается
также завершение настоящего setup snapshot перед проверкой повторного UI-действия
и при очистке. Manual diff/syntax/public320/0 PASS; compilation/native NOT_RUN,
из615 исключено вместе с wizard a501/839/a0e3413.

Далее: подготовить отдельный wizard UI checking с captured async import/snapshot
и11+4 реальными modal fixtures на последней проверенной API базе после615. Проверять
тот же615, не менять его head checkpoint-only публикациями. Полный R09 FAIL/R11
PARTIAL, owner/final installer/delivery BLOCKED или NOT_RUN по прежним критериям.
Ниже история, pending-CAS записи не означают незавершённую фактическую публикацию.

## Narrow compile-fix object6f8 — before ref-CAS

Source object `6f8b200cd34910ee167b44e6dab661c86cb506aa`, parent
`0bfa2f97c3cb627e7bc6a7b1053260084a8f28c8`, tree `c3efc70354edb1b9b880af6e78d29153a78a9b4c`
= isolated local `63dae01967394a25ad735b17353bd50cddeb67d8` (ProGo-ci614-fix).
4 paths149add83delete:1 source call1add1delete +3 failure/evidence docs. SecretsPII0/
public320/0/syntax/all call arities/diff/JSON86/fulltree PASS; freshpublic PR88
source0bfa/base4bb, unchanged workflows/VERSION/uploadsOFF.614 completed FAIL is
preserved. New compilation/native NOT_RUN; wizard excluded. Ref-CAS NOT_RUN at
checkpoint: recover actual head/run before retry. Next guardedCAS0bfa→6f8 and one
exact automatic CI. Last fullSUCCESS613/I58–80/Y22 unchanged.

## CI614 завершён FAIL — narrow callback argument correction saved

[CI614/39753293285](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39753293285)
завершён: Windows119738915213 FAIL CS1501, Ubuntu119738914876 SUCCESS. Source
`0bfa2f97c3cb627e7bc6a7b1053260084a8f28c8`, merge4b25e365a50779395f0a4c99576f7a8041258b61.
Compilation FAIL, full Test-ProGo/native NOT_RUN, pack/uploads SKIPPED; старый
SUCCESS613 не распространяется на этот API. Повтор614 не запускался.

Исправление `bc9abc24ebc2b52af6e7a4205ae5da8d60002809`: явный четвёртый null beforeRead
в вызове LoadShareAsync для импорта, без изменения сигнатуры/поведения. Изолированная
копия `102efb03d869ce5d8ee37b4ffbc3af9a7a4780bc` на locala2b6422 (exact614). Один путь1add1delete,
manual diff/AST syntax +оба вызова с4 аргументами/public320/0 PASS. Это не компиляция:
новый Windows compile/native NOT_RUN. Следующий кандидат — только эта правка плюс
3 evidence docs на том же API; wizard a501/839/a0e3413 исключён. Перед публикацией
повторить full delta scan/workflow/tree/CAS, один новый CI обоснован конкретным fix.
I58–80/Y22 exact613, delta0. Ниже история.

## CI614 actual publication — Windows compilation FAIL, native NOT_RUN

CAS de7→`0bfa2f97c3cb627e7bc6a7b1053260084a8f28c8` PASS; PR88 OPEN/DRAFT,
merge `4b25e365a50779395f0a4c99576f7a8041258b61`, treecfd761a43bfe4e5b736c15460dfd4df8f48342e7
= locala2b6422bed9d0955e612ca1385db2a71994ee63c. Один автоматический
[CI614/39753293285](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39753293285),
Windows119738915213 FAIL, Ubuntu119738914876 пока in_progress. Windows completed log:
09:10:12.8112991Z src/HomeVpnService.cs405 CS1501 — LoadShareAsync вызван с3 аргументами,
объявленная сигнатура91 требует4 (beforeRead). Build failed; full Test-ProGo/native
NOT_RUN, pack/uploads SKIPPED. Причина определена; таймауты/проверки не меняются.
Следующая микрозадача — передать явный null callback только в этом месте, проверить
все вызовы сигнатуры и новый exact SHA. Не повторять тот же614/ошибочный source.
Последний полный SUCCESS613 source de7/merge5262; I58–80/Y22 unchanged. Actual wizard
и snapshot sourcea0e3413 исключены614 и Windows NOT_RUN. Ниже история до/после CAS.

## Import API object0bfa — перед ref-CAS

Создан source object `0bfa2f97c3cb627e7bc6a7b1053260084a8f28c8`, parent
`de7b42628ccd656f369ba0e286e9b406cc20dee6`, tree `cfd761a43bfe4e5b736c15460dfd4df8f48342e7`
точно равен local checking `a2b6422bed9d0955e612ca1385db2a71994ee63c` в ProGo-ci613-import-api.
8 путей930add132delete:5 runtime/test444add32delete +3 evidence docs; manual diff/
syntax5/C#5/full-secretPII0/public320/0/JSON86/tree PASS. Свежая проверка public
visibility/PR88 draft source de7/base4bb PASS, workflows/VERSION неизменны, uploads
OFF/release не сработает.613 полный SUCCESS проверен; его не повторять. Ref-CAS
NOT_RUN в этой точке: при обрыве сначала сверить фактический head/run, не повторять
слепо. Новый API compilation/native NOT_RUN. Wizard a501/839/a0e3413 исключён.
Далее CAS expected de7→0bfa forcefalse, сохранить единственный автоматический CI.
I58–80/Y22 exact613, delta0. Ниже проверенная история.

## CI613 SUCCESS — exact configure UI/owned persistence evidence

Точный source `de7b42628ccd656f369ba0e286e9b406cc20dee6`, merge/build identity
`5262c806c06ba715d61bf3e070b52fbc31b3f2ab`, treebc5d3f70d2ff0833692d91d75eb62d108b33b215
= localbcb699bb9f3641d0d5a870836fd233e155748214. [CI613/39749169861](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39749169861):
Windows119725094348 и Ubuntu119725094777 SUCCESS, проверены jobs и completed Windows
log. Build identity08:38:06.6443827Z; full Test-ProGo PASS08:59:56.1352563Z,
Desktop5283/Home1619/Portable91, Python28=26PASS2SKIP. Native initial configure ready/
Close/Dispose3 и прежний modal read Cancel→Retry/Close/Dispose3 PASS. Actual configure
private-save/recovery8 PASS09:03:42.751–45.129Z: verify-save/close/dispose/accepted-close/
error-retry; recover-save/close/accepted-close. Реальные DPAPI, локальный HTTP и
защищённые файлы; fixture recovery transport не подтверждает работу реального VPS.

Dispose09:00:15.0283133Z: initialized=False/disposed=True/modal=False/controlEvents=2/
nativeDisposeEvents=2/publications=0/disposedAt=0. На613 наблюдались два native unwind
события, ни одной принятой публикации продукта послеDispose и нулевой прирост событий
после physical I/O. Это подтверждает измеренный интервал613; индивидуальные значения
старого612 не зарегистрированы, его failure сохранён как история. Будущий wizard
observer839 ещё требует отдельного аналогичного измерения, SUCCESS не наследуется.
Selected-key signing55/UI50/preparation72, реальная direct+owned-agent SSH→SOCKS→HTTP
и crash→fresh preparation, diagnostics modal/chooser/Context ownership, Instance28/
Maintenance126/Shutdown47 также PASS в полном прогоне.28 score-anchor критериев
совпали дословно с611: I58–80/Y22 raw21.75, delta0, веса неизменны.

Runner ZIP0.2.2 SHA256 `242078d847fb3ac8dcae27f1c739ae507cb4e6e302fb5d901e46733c1b049879`.
Все upload/prune шаги SKIPPED; bytes не получены/не переданы/не установлены. Приватная
передача BLOCKED до согласования канала владельцем. Main merge/release NOT_RUN.
Полный R09 FAIL/R11 PARTIAL, target/installer NOT_RUN. ImportAPI560/7bb/4a/fc,
wizard a501/839/a0e3413 исключены из613: compilation/native NOT_RUN. Далее изолированный
API-кандидат39e05f19b02f2947fa3fd7beff0e0b64f27ffeb2 (5 runtime/test paths444add32delete)
на613 disposal base; финальные scan/workflow/tree/CAS и один новый CI. Ниже история.

## Восстановление09:04Z и локальный wizard setup snapshot

PR88 OPEN/DRAFT, source `de7b42628ccd656f369ba0e286e9b406cc20dee6`, merge
`5262c806c06ba715d61bf3e070b52fbc31b3f2ab`; единственный CI613/39749169861
in_progress: Windows119725094348 build PASS, integration step running;
Ubuntu119725094777 SUCCESS. Completed Windows log ещё недоступен: полный
Test-ProGo/native результат NOT_RUN до его проверки. Публикация direct+agent после
589 состоялась ранее (590 FAIL, исправления595 SUCCESS); повторный push/CI не
создавался. Других активных исполнителей/локальных build/git/SSH процессов не
найдено. Все worktree и изменения сохранены.

Локальный source `a0e34136ca5cce469e39a2a870542c090f40ea99`:7 путей210add49delete.
Constructor мастера удерживает собственные CTS/TCS/driver до фактического окончания
worker PendingOwnerAsync после service initialization. Поля и RefreshStatus читают
только snapshot; смена страницы/завершение операции обновляет его без чтения на
каждое нажатие. Начальная отмена сохраняет окно, явный Close ждёт собственное I/O,
Dispose запрещает позднюю публикацию. Фикстуры ожидают реальную инициализацию без
синхронного тестового обхода. Четыре новых ShowDialog-сценария: ready, Cancel→retry
через реальный выбор своего VPS, Close, Dispose;15s окно/5s worker barrier,
ограниченная очистка удерживаемых задач и ресурсов. Ready/retry дополнительно
редактируют поля и обновляют статус при реальной эксклюзивной блокировке request
file: требуется отсутствие повторного чтения/замены нового ввода.

Manual diff/call-site review, syntax7/C#5 await-scope/diff/public320/0/secretPII0
PASS. Это статические проверки, не компиляция. Windows compilation/native всех
этих новых изменений NOT_RUN, в CI613 они исключены. Local importAPI99ad и wizard
a501/839 также не получают SUCCESS613. Полный R09 остаётся FAIL, R11 PARTIAL.
I58–80/Y22 raw21.75 exact611, delta0: код и невыполненный тест баллы не добавляют.

Далее проверить тот же613. После результата изолировать importAPI560/7bb/4a/fc
на последней проверенной базе с disposal79, исключив дальнейший wizard-код;
финальный diff/секреты/workflow/tree перед единственным новым CI. До проверки613
не менять опубликованный head. Будущий import-modal Dispose observer требует
отдельной проверки native-unwind интервала по фактическим значениям613. История ниже.

## CI613 running — narrow disposal diagnostic source, future import excluded

PR88 head independently verified `de7b42628ccd656f369ba0e286e9b406cc20dee6`, merge
`5262c806c06ba715d61bf3e070b52fbc31b3f2ab`, full treebc5d3f70d2ff0833692d91d75eb62d108b33b215
= localbcb699bb9f3641d0d5a870836fd233e155748214. One automatic
[CI613/39749169861](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39749169861),
Windows119725094348/Ubuntu119725094777 in_progress at verification. CAS expected
17c64→de7 PASS, empty initial webhook lookup followed by single run, no manual
retry.7 paths342add60delete:4 diagnostic source/test paths +3 docs, full scan/tree
PASS, workflows/VERSION unchanged/public uploads OFF. New compilation/native
NOT_RUN. Dispose failure cause remains hypothesis until detailed flags/interval
are observed; do not label narrowed measurement successful before actual result.

ImportAPI99ad/wizard a501+839 remain excluded and NOT_RUN. Next independently
move wizard initial PendingOwner/status probes to owned cached worker state and
adapt actual native lifecycle fixtures; source writer/format unchanged. Do not
change the checked613 head for checkpoint-only commits. Last full SUCCESS611 and
its28 score anchors/ZIP remain the only current readiness evidence: I58–80/Y22
raw21.75, delta0. Full R09 FAIL/R11 PARTIAL; owner/installer/delivery still open.
Ниже object-before-CAS record — история, не повторять публикацию.

## Dispose diagnostic objectde7 — ref CAS pending

Source object `de7b42628ccd656f369ba0e286e9b406cc20dee6`, expected parent17c64b6276567f1b2f2c090ef8130b1c064aa45c,
full tree `bc5d3f70d2ff0833692d91d75eb62d108b33b215` = isolated local
`bcb699bb9f3641d0d5a870836fd233e155748214` (ProGo-ci612-fix).7 paths342add60delete:
4 narrow disposal-measurement source/test paths +3 exact612 failure docs. Source
import/wizard/Stop excluded; deadlines/workflows/VERSION unchanged/public uploads
OFF. Fresh repo public and PR head17c64/base4bb checked. Full secretPII0/public320/0/
syntax/C#5/diff/JSON31+55/full-tree equality PASS.612 completed FAIL, not rerun.
Ref update NOT_RUN at checkpoint; inspect actual PR/run after interruption before
retry. New compiler/native NOT_RUN, dispose hypothesis not confirmed. Next CAS
expected17c64→de7, one exact diagnostic CI and record merge/run/jobs. No binary upload;
I58–80/Y22 exact611 unchanged. Preserve source and measured post-disposal criterion.

## CI612 FAIL — dispose compound assertion, detailed ownership proof pending

Exact source `17c64b6276567f1b2f2c090ef8130b1c064aa45c`, merge/build identity
`27a8d83cd6a1cb704ab36269eabd3406abd402f4`, treecf0eb0f428029c314dcf191fc0e2df1aa24eebcb.
[CI612/39744824752](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39744824752)
Ubuntu119710569884 SUCCESS, Windows119710569658 FAIL at08:25:35.5384376Z:
`configure read publishes only to its live owner; Close waits for physical settlement
and Dispose gets no late controls: dispose`. Compilation and full Test-ProGo PASS
08:25:16.3862166Z/Desktop5283; read-modal old3 and new initial ready/Close cases PASS.
The compound Dispose test gives no individual values; exact failed subcondition
not established. Remaining new modal persistence/recovery8, Home suite tail,
Portable and Python tail NOT_RUN; pack/public release-content steps SKIPPED, all
uploads SKIPPED. Last full SUCCESS/scoring/package still611 only, no new credit.

Hypothesis (not confirmed): controls emit lifecycle events when native ShowDialog
finally unwinds after direct Dispose, before the held private worker is released.
Microsoft Form.Close/ShowDialog contract already distinguishes modal disposal;
ReferenceSource Form/Control full pages were unavailable (DisabledError), only
search snippets were accessible, so no claim they were fully read. Next exact
runtime measurement must establish the event interval instead of assuming cause.

Narrow local diagnostic correction `79fcb6b419935f90e4bf5d3a4f0f26a2ff5217cc`: a
memory-only dispatcher PublicationCount exposed to its configure fixture proves
zero product callback execution after Dispose across the full interval. Existing
control-change observer additionally requires zero delta after real modal unwind
and physical-I/O release; baseline captured while actual work is still held and
publications cannot change. Detailed initialized/disposed/modal/event/callback
values accompany the assertion. Both initial-read and save Dispose cases use the
same proof;15s/5s budgets and owned cleanup unchanged.4 files24add9delete source/
syntax/public320/0/secretPII0 PASS; compilation/native NOT_RUN. No claim fixed until
new exact CI; no612 rerun or success inherited. Publish isolated correction+docs,
exclude all future import and wizard work.

Independent `83945c64026a5eef21637830aa711ff675535089` adds11 actual ShowDialog
import/receipt cases: cancel/different-token retry, explicit Close/Dispose, accepted
Cancel/Close/Dispose/global Stop, actual partial-owner-save→Retry, receipt Cancel/
Close/global Stop. Real DPAPI and fixed private bytes; successful retry uses the
existing owned local SSH substitute (not live VPS/native OpenSSH acceptance).
15s modal/5s barriers and joined service/window cleanup; no new external setup or
late SSH after Stop expected. One file99add2delete source/syntax/public/secretPII0
PASS, compilation/native NOT_RUN; actual wizard sourcea501 also untested. Future
wizard Dispose event observer will adopt this measured interval separately if
validated; not included in isolated correction. Constructor/status PendingOwner
still sync, full R09 FAIL. I58–80/Y22 exact611 unchanged. Ниже история.

## R09 wizard import await — locala501f57, modal regression next

Source `a501f57fff7bf2244c265ea2ba093f74a32edb99`: actual Advance step1 captures
token/own-mode/owner copy/recovery intent and public StopWork identity before any
await. Reads pending setup via owned cancellable helper, preserves readonly recovery
intent and preparedToken/owner identity, awaits UseTokenAsync and then worker same-
request ConfirmConsumedAsync. Two new waits explicitly use DialogResult.None,
retain raw tasks and CTS until actual I/O completion, permit Cancel→Retry/explicit
Close, and suppress disposed-owner publication. Accepted save followed by cancellation
is reported saved without starting the channel. Public Stop identity guards before
admin/import/receipt/start: even an already completed StopAll cannot be followed by
an obsolete late SSH start. Context/card phone persistence gate includes share
saving and importing; Desktop/settings/Stop commands remain independent.

Three source paths98add16delete diff/manual call-site+scope review/syntax/C#5/
public320/0/secretPII0 PASS. Existing regressions retained; native modal token import/
receipt Cancel/Retry/close/dispose/accepted/global-stop/real partial-save tests are
next, compilation/native NOT_RUN. Source unpushed/excluded612 and frozen importAPI99ad.
Constructor and UpdateSetupRecoveryState still call sync PendingOwner, separate
remaining R09 task; not hidden by this actual import migration. Next real modal
regressions before source freeze, then initial/status cached worker migration.
CI61239744824752 Windows119710569658 build in_progress, Ubuntu119710569884 SUCCESS.
I58–80/Y22 exact611 unchanged, full R09 FAIL and target/installer NOT_RUN.

## CI612 running — actual configure UI published, import still separate

PR88 actual source `17c64b6276567f1b2f2c090ef8130b1c064aa45c`, merge
`27a8d83cd6a1cb704ab36269eabd3406abd402f4`, full treecf0eb0f428029c314dcf191fc0e2df1aa24eebcb
= local023bf0779010f22b6a89211668f0c1e5835053e7. Single automatic
[CI612/39744824752](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39744824752)
Windows119710569658/Ubuntu119710569884 in_progress at verification. Initial webhook
lookup empty, then unique run observed; no manual duplicate. CAS expectedd896→17c64
PASS. Source8 frozen UI paths +3 evidence docs,11 paths849add150delete, full scan/
syntax/C#5/diff/JSON/tree PASS. Workflows/VERSION unchanged/public uploads OFF.
New actual UI compilation/native NOT_RUN pending exact logs; no inherited611
success. Last full SUCCESS611 only sourced896/mergef0bf, same28 score anchors.

Import API99ad (Stop/pair/service/setup worker) excluded from612. Next independently:
actual wizard await UseTokenAsync/ConfirmConsumedAsync with captured input/scope,
modal Cancel/close/accepted/partial/retry outcomes and context loading gates. Setup
constructor/text-change pending probes still sync and remain a later separate
worker migration; do not declare full R09 finished after importing alone. Source
head/doc-only pushes must wait for same612 completion, preserve checking source.
I58–80/Y22 raw21.75 exact611 unchanged; target/installer/private delivery blocked.
Ниже object-before-CAS checkpoint — история, не повторять публикацию.

## Actual configure UI object17c64 — ref CAS pending

Created source object `17c64b6276567f1b2f2c090ef8130b1c064aa45c`, expected parentd89673c27aa8ce72ffc5e13eb8e5dc849398ea5a,
full tree `cf0eb0f428029c314dcf191fc0e2df1aa24eebcb` exactly equals isolated local
`023bf0779010f22b6a89211668f0c1e5835053e7` (ProGo-ci611-ui).11 paths849add150delete:
8 frozen actual configure source/test paths +3 evidence docs. Import API99ad/
Stop560/setup changes excluded; workflows/VERSION equal611 and fresh repo public,
automatic uploads OFF, no release trigger. Full scan secretPII0/public320/0/source/
syntax/C#5/diff/JSON31+55/full-tree equality PASS.611 completed full SUCCESS; new
compilation/native NOT_RUN. Ref NOT_RUN at this checkpoint; after interruption
inspect actual PR head/run before retry. Next CAS expectedd896→17c64, one CI,
record exact merge/run/jobs; I58–80/Y22 exact611 unchanged. No binary upload.

## CI611 full SUCCESS — share API, actual UI migration still excluded

[CI611/39740277423](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39740277423)
Windows119695558302/Ubuntu119695558139 SUCCESS. Source
`d89673c27aa8ce72ffc5e13eb8e5dc849398ea5a`, merge/build identity0.2.2+
`f0bf91f6e0a15b5e01e4ecd7f274096565cd1cc7`, tree7da84ab401825c7483d62770ad73114003d57a9a
= localee240bcc5abd69f48dbd0be73283239da5d59b24. Native store9 home+9 share and
invalid-ID5/origin4 refusals, service-save7 home+9 share including stale server/
Access and cross-kind write exclusion PASS07:47:42–46.8 receipt worker cases PASS
07:51:29.407–30.706: actual protected save, live UI checks, cancel/dispose, real
replace/delete locks and changed request keep exact recovery. Actual configure
still uses legacy sync callback at this source; no inherited UI9faa/import success.

Full Desktop5281/HomeVPN1521/Portable91; diagnostics modal6/chooser3/Context ownership,
selected-key signing55/UI50/prep72 +actual direct/owned-agent OpenSSH→SOCKS→HTTP→crash/
fresh preparation, Instance28/Maintenance126/Shutdown47; Python28=26PASS2SKIP.
Runner ZIP0.2.2 SHA256 `321759f1a32fc1c8bfc7250b3fa482ed2687b22f22f91eb685c65ef7bbc9d021`.
Pack PASS, uploads SKIPPED all; bytes not retrieved/delivered/installed. Private
delivery BLOCKED until approved channel; installer/owner acceptance NOT_RUN.
Same28 score anchors exact611 with same criterion text/weight87/D0.5/P0.5/A0=>
21.75/display22, delta0. I58–80, full R09 FAIL/R11 PARTIAL; no target readiness claim.

Independent source `fc257e151856d7e9224c4fe6b1db75dc11b66749`: setup HasPending/
PendingOwner/ConfirmConsumed worker APIs, copied inputs and cancellation/memory
admission, rechecked same request/result and both actual saved credentials before
Delete. Seven native read/cancel/scope/real lock/new request/new credentials vectors
written,98add10delete in2 files; source/syntax/public320/0/secretPII0 PASS, native/
compilation NOT_RUN. Actual wizard still calls sync adapter and synchronous setup
constructor/status probes; these are explicitly remaining R09, not closed.

Frozen later import API: local `99ad0031e48a5e420d6149d2025a5345a6893bb9`, tree
8b8b432942c88a0582ffb30cd29d113dd6fa5803 in ProGo-import-api based UI9faa.5 source/
test paths444add32delete (Stop560,pair7bb,service4a,setupfc), exact root source
match; token/owner format unchanged, partial-save honest, no crash-atomicity claim.
All compilation/native NOT_RUN; later wizard changes excluded. Next publish only
frozen UI9faa+exact611 evidence docs after scan/tree, one CI; preserve import API
source separately while actual wizard/initial setup probes migrate. Ниже история.

## R09 owned import service — local4a4f934, wizard caller still synchronous

Source `4a4f93434e58a270db0f2bea52303f48db04e9a5`: UseTokenAsync reserves immutable
trimmed token/copied owner, task/CTS before parse/Stop/private I/O; exact duplicate
joins across phases, different input/private write/start refuse. Worker parses,
awaits initial state, snapshots previous scope, awaits private physical StopChannel
(no import self-await), reads optional share state, stages access/owner and admits
commit only for live matching scope. Pair metadata publishes after both physical
writes; cancellation before admission preserves bytes, accepted save may finish,
second write failure is typed partial and keeps old live metadata. Retained failed
stage cleanup blocks writes/import until explicit Stop cleanup retry. Owner input
mutations after capture cannot change the persisted identity.

Outer public Stop closes import admission before off-UI cancellation, awaits
physical stop and ImportWork even after a physical stop error, then retries only
owned import cleanup. Dispose returns without waiting and retains this same task;
accepted write completes with no disposed-service metadata. Legacy UseToken now
sync compatibility adapter; actual wizard/ConfirmConsumed still await migration,
so whole R09 remains FAIL. No setup request consumed, no extra SSH/setup action.

9 native service vectors written: save/cancel/stop/dispose/accepted-stop/accepted-
dispose/real second-file sharing error/cleanup-lock→unlock→Stop→Retry/stale Access,
with duplicate identity, fixed owner copy, blocked parallel writes/start, original
receipt byte preservation and5s bounded held I/O/cleanup. Two paths202add19delete
source/manual ownership review/syntax/C#5/public320/0/secretPII0 PASS, compilation/
native NOT_RUN. Excluded611/frozen9faa UI. Stop560 +pair7bb +service4a remain later
checking source. Next actual wizard captures/imports via owned await and worker
same-request setup-consumption check; preserve preparedToken/owner ID to avoid
remote replay, then real modal import Cancel/close/accepted outcomes. I58–80/Y22
exact610 unchanged; current611 Windows build in_progress, Ubuntu SUCCESS.

## R09 staged import pair primitive — local7bb6451, not yet service/UI

Source `7bb6451ea8d611b5dfd753a7165d9736a0e02df3`: HomeVpnImportWrite owns two
fixed-name access/owner HomeVpnAddressWrite stages, prepares/protects/verifies both
on worker before admission, exposes actual AccessCommitted/OwnerCommitted flags.
Accepted cancellation finishes the known pair; second-file failure is partial,
not a claimed rollback. Every Dispose attempts both owned stages and retains any
failed/changed one for exact retry. No new schema/journal/remote action or setup
receipt consumption; this is not a crash-atomic two-file transaction. Existing
single-address commit validation extracted unchanged for reuse.

9 native vectors written: real valid before/after tokens and DPAPI owner, save,
cancel/refuse, changed owner, first/second target sharing failures, accepted Cancel,
locked cleanup→unlock retry and changed-stage preservation. Live UI heartbeat,
5s owned barriers/join/cleanup and finally exact private byte restoration. Old
setup request bytes preserved; no token/private key/owner path printed. Two paths
124add1delete source/manual diff/syntax/public320/0/secretPII0 PASS. Compilation/
native NOT_RUN; factory not yet connected to service or wizard, excluded611 and
frozen9faa UI. Stop560 still untested. Next retained import ownership worker must
parse/capture before physical channel Stop, reserve task/CTS before I/O, join on
outer Stop without self-await, block concurrent writes/start, preserve partial
state and retryable cleanup, publish only to live matching owner. Then actual
wizard and setup-consumption worker with no repeated external setup. I58–80/Y22
exact610 unchanged; R09 FAIL/R10 partial and target acceptance remain open.

## CI611 running — published frozen share API, UI/Stop excluded

PR88 actual head `d89673c27aa8ce72ffc5e13eb8e5dc849398ea5a`, merge
`f0bf91f6e0a15b5e01e4ecd7f274096565cd1cc7`, tree7da84ab401825c7483d62770ad73114003d57a9a
= localee240bcc5abd69f48dbd0be73283239da5d59b24. One automatic
[CI611/39740277423](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39740277423),
Windows119695558302 in_progress (build), Ubuntu119695558139 SUCCESS. Initial run
lookup was empty until webhook appeared; no duplicate manual workflow dispatched.
CAS expected04ca→d896 PASS, independent actual PR head/merge/run verified.
5 frozen API source/test paths +3 evidence docs, full tree/secretPII/public/diff/
syntax/JSON PASS; automatic binary uploads OFF, no release. Current compilation/
native NOT_RUN pending exact completed log. Last full SUCCESS610 only source04ca/
mergec506; its28 score anchors remain current evidence. Root UI9faa and Stop560
not published/tested by611. No source-head/document-only pushes while611 active.

Next independent microtask: staged access+owner persistence primitive under fixed
private destinations, immutable inputs, real DPAPI/file locks/cancel/partial-save/
owned cleanup tests. Preserve existing recovery contract and truthful partial write
outcomes; no new format, no crash-atomicity claim. Then retained import ownership
using split physical/public stop; actual wizard awaits import/receipt later.
I58–80/Y22 raw21.75 unchanged, no credit for pending611 or unexecuted local code.
Ниже object-before-CAS checkpoint — история, не повторять ref update.

## Frozen share API objectd896 — ref CAS pending

Source object `d89673c27aa8ce72ffc5e13eb8e5dc849398ea5a`, expected parent04ca974519bfffdb70a1706066ebaef7ed26bf6b,
full tree `7da84ab401825c7483d62770ad73114003d57a9a` = isolated local
`ee240bcc5abd69f48dbd0be73283239da5d59b24` (ProGo-ci610-followups).8 paths937add155delete:
5 frozen API source/test paths +3 exact610 evidence docs. Actual UI9faa/Stop560
implementation excluded. Full-delta syntax/C#5/diff/JSON31+55/public320/0/actionable
secretPII0 (one reserved synthetic URL fixture reviewed)/full-tree equality PASS.
Fresh PR head04ca/base4bb and public visibility verified, workflows/VERSION
unchanged, automatic uploads OFF and release not triggered.610 completed SUCCESS.
Ref update NOT_RUN at this checkpoint; inspect actual PR head/run after interruption
before any retry. Next CAS expected04ca→d896, one PR CI, record merge/run/jobs.
New compilation/native NOT_RUN; I58–80/Y22 exact610 unchanged. No binary upload.

## CI610 full SUCCESS — home-address/modal block accepted in isolated Windows

[CI610/39736682268](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39736682268)
Windows119683904424/Ubuntu119683904096 SUCCESS. Exact source
`04ca974519bfffdb70a1706066ebaef7ed26bf6b`, merge/build identity0.2.2+
`c506814d33c4ef39f5cb946c41121a70850ff851`, tree36c0e5cb4f0842d9273105b4ae9265e99d4da2e2
= localfa61c973a5c9878fb2154ada0d02b6d5fa618a09. Same run completed, no rerun.
New native: read-modal3 (actual Close order07:12:57.6950548Z), protected store9
07:12:58.162–59.165, service-owned save7 07:12:59.254–13:02, actual write-modal7
07:13:02.344–05.399. Cancel→Retry, explicit Close after physical I/O, Dispose/no
late controls, accepted save after Cancel, real file sharing error→Retry and
retained locked-stage cleanup→unlock→Stop→Retry PASS. Previous609 test defect fixed
without deadline increase or assertion removal. New source home write now compiled
and executed; later root share/init/configure/Stop changes remain NOT_RUN.

Full: Desktop5281/HomeVPN1325/Portable91; selected-key signing55/UI50/prep72,
diagnostics modal6/chooser3/Context ownership, Instance28/Maintenance126/Shutdown47,
actual direct+owned-agent OpenSSH→SOCKS→HTTP→crash→fresh preparation; Python28=
26PASS2SKIP. Native combined SSH measured direct startup1945ms/prep33ms/SSH CPU0ms/
working-set11382784B/peak11460608B, recovery1949ms/prep33,28ms/CPU31.25ms/working-set
11350016B/peak11423744B. These are isolated fixture observations, not full app I/O/
slow hardware/owner acceptance or final performance PASS. Program process GUI /
full ShowQr→real VPS / target Windows still NOT_RUN.

Runner ZIP0.2.2 SHA256 `c472e2b61e6f5f5b2def0c1ebb208b54e662d299380386496f63099a967513df`.
Pack PASS, every upload SKIPPED; bytes not retrieved/delivered/installed. Private
delivery BLOCKED until approved channel; final installer and target acceptance
NOT_RUN. Fixed v1.0 same28 criteria/text anchors verified exact610, weight87 with
D0.5/P0.5/A0 =>21.75/display22 unchanged. No extra credit for more native cases or
reusing criterion/parent-child weights. I58–80/Y22, delta0, R09 full FAIL/R11 PARTIAL.

Independent root `56084e8a5c1bbbdbaf4a72b8c4b1ea6593a2270f` separates public stop task
reservation/completion from retained physical StopChannelAsync. Existing start/read/
write gates now reject both stop owners; cancellation, actual cleanup and retry
body unchanged. This enables future import to stop channel without self-await,
then outer Stop can join import. One file25add7delete diff/manual review/syntax/
C#5/public320/0/secretPII0 PASS; existing native lifecycle regressions NOT_RUN.
No import code/schema changed, not in610 or frozen399/API or9faa/UI checking.
Next publish frozen399/API+exact610 evidence docs after scan/tree and one CI;
actual UI9faa and Stop560 remain independent and untested. Ниже — история.

## Configure UI checking frozen9faa — token import trace, no new implementation

Local `9faa1eafc6006115b52244f5571537d3f495bddd`, tree52780738e01d2d4ab22dcdae3c9650531b99598b
in ProGo-ci610-ui, based frozen API399e88d75e38366cefb8bcf179df4e813dc7e2f4.
8 source/test paths305add60delete (c5 initializer,17 async actions,b1 modal suites).
Every source path equals root; future token/Stop work excluded. Compilation/native
NOT_RUN. Source frozen separately from running610 and next API399 checking; preserve
order, no publishing to cancel610. Later add exact predecessor evidence docs only.

Narrow next trace (source facts, target runtime latency here NOT_RUN):

| File / symbol | Evidence / effect | Bounded next correction / check |
|---|---|---|
| HomeVpnWizardForm.Next step1 | UI calls UseToken then SetupRecovery.ConfirmConsumed before StartChannel | Await retained import then owned receipt check, capture inputs and preserve preparedToken/owner ID so no second setup |
| HomeVpnService.UseToken | Initialization.GetResult + StopAsync.GetResult + parse/DPAPI/access+owner files and per-server share read synchronously | Parse/stage/write worker with immutable token/owner and admission; public Stop must join it without self-await |
| HomeVpnPrivateFiles.Save via UseToken | Sequential access then owner; second failure may leave partial local save | Existing native SetupRecovery failure test retains request/result; do not falsely claim full rollback or consume request |
| HomeVpnSetupRecovery.ConfirmConsumed | Sync semaphore/GetResult, real request/access/owner DPAPI reads and Delete on UI | Retained worker under same lease and same request/result/owner binding; save/consume failure keeps recovery, no external replay |
| HomeVpnService.StopAsync | One retained stopWork currently joins start/init/share-read/address-write | Separate physical channel stop admission from outer stop that also joins import, avoiding import→Stop→import cycles; preserve all native ownership tests |

Existing contracts: old-session cleanup failure forbids new credential write;
second local-save failure retains exact request/result; locked receipt delete
retains matching access and request; only complete access+owner persistence allows
consumption. Source tests/HomeVpnSetupRecoveryTests.cs96–112 and native guard606.
Do not strengthen historical proof into untested full crash atomicity: emergency
restart/power loss and target PC remain R10/NOT_RUN. No new file-format/transaction
mechanism silently chosen here. Proposed microstages: (1) channel/outer Stop split
with existing lifecycle regressions; (2) retained immutable-input import save with
pre-admission cancel/accepted outcome/partial-save/retry; (3) actual wizard awaits
import and worker receipt consumption with modal/owner-close/no-repeat tests.
Current source still610; I58–80/Y22 exact608 unchanged, full R09 FAIL.

## R09 actual modal persistence vectors — localb1b2139, Windows NOT_RUN

Source `b1b2139cf3b338ca4ed965941bfab07d8158cec7`:8 actual configure ShowDialog cases
verify-save/close/dispose/accepted-close/real replace-error→explicit retry, recovery
save/close/accepted-close. Real loopback HTTP /health goes through production
HomeProfileHttp, actual DPAPI/atomic per-server save and private request/proof check
remain in place. Recovery transport supplies synthetic completed status only;
no live VPS/setup action, no claim this is target TLS/VPS acceptance. Tests assert
one GET/save per explicit attempt, no profile POST or repeated setup, exact receipt
preservation on cancellation, consumption after real save, ordered modal close,
zero late disposed controls. Hook5s/modal15s/cleanup5s; release/owned streams/window/
work joins/service Stop/private byte restoration use nested finally. Save failures
now expose local file/access/protection errors rather than a misleading network
message.3 paths115add5delete diff/syntax/C#5/public320/0/secretPII0 PASS, compilation/
native NOT_RUN. Actual UI source c5/17/b1 remains separate from frozen API399 and610.

CI610 Windows119683904424 now IKEv2 relay integration in_progress; earlier build
step SUCCESS. Exact completed log/overall outcome not yet read, no duplicate run.
Next preserve separate UI checking source; read same610 on completion and publish
only frozen399 API+exact evidence docs after scan/tree. Independent next trace:
UseToken/import and setup receipt consumption must move together without losing
pair integrity or repeating remote setup; do not wrap synchronous GetResult on UI.
I58–80/Y22 exact608 unchanged, R09 still FAIL, target/owner/installer NOT_RUN.

## R09 actual configure async persistence — local17d3988, Windows NOT_RUN

Source `17d39886ed05d15b44d90bbfe423a56395bf6057`: production CreateConfigureForm
uses captured Access with service.SetShareOriginAsync. All button routes await
HasPendingAsync; admin admission remains on owner UI (so native SSH preparation
ownership is preserved), then HTTP, retained save and ConfirmOwned receipt flow.
Pure memory owner callbacks gate before/after persistence; verify-only holds the
same request lease across last pending check/save. OK follows actual save and, for
recovery, matching receipt consumption. Closed owner/cancel cannot start a late save;
a save already admitted is reported honestly if it finishes after Cancel. Retained
cleanup failure preserves its explicit Stop/retry instruction. Legacy sync Action
save overload remains fixture adapter on worker; normal product uses the typed API.
No direct HasPending/DPAPI/receipt I/O or blocking Wait/GetResult in configure UI.

Two source paths53add34delete: diff/syntax/C#5/public320/0/secretPII0 PASS. Existing
native regressions retained; new actual modal persistence cases are next. Compiler/
native NOT_RUN, unpushed/excluded frozen399 and current610. Initial readc5f0dda
also still untested. Next8 bounded actual modal cases: verify save/Close/Dispose/
accepted-Close/real save error→explicit retry; recovery save/Close/accepted-Close,
with real loopback HTTP health and real protected save/journal. No live VPS/setup.
I58–80/Y22 exact608 unchanged; no points for written/unexecuted code or tests.

## R09 configure initial read — localc5f0dda, not in frozen API checking

Source `c5f0dda0ee65272eb1f93d743e85e8eae5b0acfd`: actual CreateConfigureForm now
returns HomeProfileConfigureForm with retained Work/InitializationWork and physical
PendingReadWork. Constructor/UI refreshGate no longer read private pending files or
call the legacy ShareOrigin disk getter. Initial HasPending/DPAPI getter execute
on owned workers; loading gates actions, permits Close, respects queued cancellation
and drops publication after disposal. A caller's draft entered during loading is
preserved. Failed later operations refresh pending state on worker before cached
UI gates. Click work remains observed and retained, without detached mutations.

Existing keyboard/owner/HTTP/recovery fixtures now await actual readiness with5s
bounds; wizard child-modal guard waits for its read and finally joins it before
private backup restoration. Three new actual ShowDialog cases (ready/close/dispose)
hold a real-path worker, assert native message turns/close ordering/zero late controls
and source byte preservation; overall15s, barrier5s and finally release/join/restore.
Diff/manual ownership review/syntax/C#5/public320/0/secretPII0 PASS; compilation/native
NOT_RUN.7 paths144add28delete, source not published and excluded frozen399/API
candidate and current610. No higher score for written tests. Actual button run still
has synchronous HasPending/save/legacy Confirm callback; next stage changes only
these routes to owned save/ConfirmOwned, then modal save/receipt regression.
CI610 same run39736682268 Windows119683904424 still build/testing, Ubuntu SUCCESS.
I58–80/Y22 exact608 unchanged, full R09 FAIL and full R11/target acceptance open.

## Share API checking frozen399e — actual configure UI excluded

Local `399e88d75e38366cefb8bcf179df4e813dc7e2f4`, treec45fc44e657928d73b4fd231ad42fdb79217e61d
in ProGo-ci610-followups, basefa61c973a5c9878fb2154ada0d02b6d5fa618a09. Only5 source/
test paths314add75delete: shared store, service ownership, new receipt worker API,
parameterized native home/share suites and8 confirmation cases. Matches root source
exactly for those5 files; further UI work excluded. Compiler/native NOT_RUN; attach
exact610 evidence docs only after same run completes, then scan/tree and one new CI.
Do not publish while610 active or inherit its result for these changes.
CI610 job IDs now known: Windows119683904424 in_progress, Ubuntu119683904096 SUCCESS.
Next UI stage1: retained initial/pending read with pure cached UI gate, meaningful
loading/cancellation/close handling and old fixture readiness adjusted. Stage2 actual
async save/ConfirmOwned routes; stage3 native modal acceptance/cancel/receipt tests.
No new product requirements, scope expansion or score growth: I58–80/Y22 exact608.

## CI610 queued — isolated modal Close proof, source04ca

PR88 head independently verified `04ca974519bfffdb70a1706066ebaef7ed26bf6b`, merge
`c506814d33c4ef39f5cb946c41121a70850ff851`, full tree36c0e5cb4f0842d9273105b4ae9265e99d4da2e2
= localfa61c973a5c9878fb2154ada0d02b6d5fa618a09. One automatic
[CI610/39736682268](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39736682268),
queued at verification; job IDs not yet read. Expected-head CAS3474→04ca PASS.
Only one test file modal Close assertions +3 evidence docs published, secrets/
PII0/full tree PASS, workflows unchanged/public uploads OFF. New compilation/native
NOT_RUN; no duplicate609 retry. Last full SUCCESS608 remains scoring evidence,
609 Test-ProGo PASS followed by native Close-test FAIL remains historical partial.

Root share-address primitivea2fd/service02bc/receipt5610 source is NOT in610.
Next independently: actual configure UI owned pending-read/save/receipt route,
then bounded native modal acceptance/cancel tests; poll same610. I58–80/Y22
raw21.75 unchanged, full R09 FAIL/R11 PARTIAL, no binary delivery/owner acceptance.
Ниже checkpoint до CAS — история, не указание повторить публикацию.

## Narrow modal checking object04ca — ref update pending

Created source object `04ca974519bfffdb70a1706066ebaef7ed26bf6b`, parent34740802206b6bd24ec41838ba25c44980ddec6c,
full tree `36c0e5cb4f0842d9273105b4ae9265e99d4da2e2` exactly equals local
`fa61c973a5c9878fb2154ada0d02b6d5fa618a09` in ProGo-ci609-fix.
4 paths420add58delete: one narrow modal test fix +3 evidence docs. Full delta
secretPII0/public320/0/diff/syntax/JSON31+55 PASS; unpublished share source excluded.
Workflows/VERSION equal609, fresh repo public check and upload guards OFF. CI609
completed failure, no active predecessor cancelled. Ref CAS expected3474→04ca
NOT_RUN at this checkpoint. After interruption inspect actual PR head/run before
retry. New compilation/native NOT_RUN; no inherited609 or608 success. I58–80/Y22
unchanged. Next one expected-head CAS, record actual merge/run/jobs; no binary upload.

## CI609 FAIL — modal test assumption isolated, new private-write vectors NOT_RUN

Exact source `34740802206b6bd24ec41838ba25c44980ddec6c`, merge
`0da565ba835dbf63fc2f402eafdac3f380f23624`, tree4752a5e04f2b38c4516d969f466bf163f10abee6.
[CI609/39733721723](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39733721723):
Ubuntu119674440425 SUCCESS; Windows119674440595 FAIL at06:42:52.7584245Z,
`explicit modal close/disposal waits for its actual read result: close`.
Compilation/build identity0.2.2+merge and complete scripts/Test-ProGo.ps1 PASS
(06:42:36.5499890Z): Desktop5281, signing55/UI50/prep72, diagnostics actual modal/
three chooser/Context ownership, Instance28/Maintenance126/Shutdown47. Later
Test-HomeVpnRelay native fails after actual read-modal Cancel→Retry PASS at
06:42:52.4008475Z. Read Close asserts passed physical cancellation/no late dialog/
source preservation, then incorrectly required IsDisposed. Read Dispose and new
9 store+7 owned-save+7 write-modal vectors were not reached: NOT_RUN. Python tail/
public release-content check/pack NOT_RUN (steps SKIPPED); all uploads SKIPPED.

Root cause: modal ShowDialog Close returns/hides a reusable form and requires
explicit fixture Dispose; it does not establish IsDisposed. Microsoft contract:
[Form.Close](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.form.close?view=netframework-4.8)
and [ShowDialog](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.form.showdialog?view=netframework-4.8).
Isolated fix source `d2512ed83577cb46cb4e10ca6c4d1f83ef00f376` (ProGo-ci609-fix,
root equivalent `784194e5b1fb14f338612303b9732f8126f1d8b2`) changes only18 lines
in tests/HomeVpnWaitingTests.cs. Close now requires actual FormClosed event,
worker-completed/busy-false at that event, and real ShowDialog return (Modal false,
not visible) before fixture Dispose. Explicit Dispose remains independently
asserted and physical worker joined. Both read and write Close expectations fixed;
no assertion disabled, deadline15s and cleanup preserved. Production code unchanged.
Local syntax/diff PASS; fixed assertion Windows/native NOT_RUN. No blind rerun of609:
new checking commit needed, including only this fix and current evidence docs.

Independent root source `5610af7ddec886956ff269543064bf54fb333850` adds worker pending
read and ConfirmOwnedAsync: receipt/proof/private consumption outside UI, two
memory-only owner checks around actual retained save, physical receipt recheck after
the final owner queue. Cancel/save failure/new receipt/closed owner keep the request;
accepted save followed by cancellation is reported as saved with pending receipt.
Eight native vectors written using real DPAPI, real delete/replace locks, changed
request, live UI heartbeat and bounded join/cleanup. Source/syntax/C#5/public320/0/
secretPII0 PASS; compiler/native NOT_RUN. API not yet used by actual configure UI.
Source a2fd/02bc/5610 remains excluded from isolated modal-fix checking. Next after
it: connect actual configure form to owned read/save/confirmation, preserve receipt
and Cancel/close outcomes. Whole R09 FAIL, R11 PARTIAL. I58–80/Y22 raw21.75 exact608,
change0. Last known ZIP remains exact608 only, no delivered/installed package.
Ниже checkpoints — история; running609 status superseded by this completed result.

## R09 common owned service write — local02bc9ef, Windows NOT_RUN

Source `02bc9efe1f15caffc2704a04e37505b5e3e70ae9`: HomeVpnService.SetShareOriginAsync
и SetHomeAddressAsync используют один retained privateAddressWork/addressWrite.
Резервирование task/CTS предшествует private I/O; exact duplicate join учитывает
тип адреса, canonical value, captured server ID и тот же Access object. Иная
запись отказывается. Worker повторно проверяет Access/server при commit; cache/
revision обновляются только после фактической записи в live scope. Stop/Dispose
join/cancel-before-admission/accepted-result/retained cleanup общие для обеих
записей. Sync SetShareOrigin теперь compatibility adapter GetResult над owned API:
actual configure/recovery callbacks пока блокируют UI и остаются следующей задачей.

Прежние7 home native cases сохранены,9 share cases дополнительно покрывают Stop/
Dispose до и после admission, cancel, cleanup-lock→unlock→Stop→Retry, stale server/
access и cross-kind/same-ID-different-access refusal; real DPAPI/files, heartbeat,
bounded waits и finally join/restore. Локально diff/syntax/C#5 structure/public320/0/
secretPII0 PASS, compiler/native NOT_RUN. Source ещё не опубликован/не входит609.
CI609 Windows119674440595 продолжает Repository hygiene and build, Ubuntu SUCCESS.
Следующий этап — async receipt read/acceptance/consume с сохранением same-request/
owner boundary до подключения actual configure UI. I58–80/Y22 exact608,0 роста.

## R09 shared protected-address primitive — local a2fd23e, Windows NOT_RUN

Source `a2fd23e8205e50f69991fb877bee89361f26477a`: HomeVpnAddressWrite.ForShareOrigin
использует прежний staged DPAPI/atomic Move-or-Replace/retained cleanup механизм.
Destination задаётся только canonical32-hex server ID; HTTPS нормализуется прежним
HomeProfileShare.Origin. Home constructor/API и commit/cancel boundary сохранены.
Native suite параметризован: прежние9 home +9 share cases,5 invalid ID +4 invalid
origin refusals, реальные сохранённые home/other-server bytes защищены assertions.
Waits bounded, release/join/own-stage cleanup и backup restore в finally.

Проверки локально: diff, syntax/C#5 await structure и public320/0 PASS; actionable
secret/PII0. Один scanner email-pattern — намеренный отрицательный URL fixture в
зарезервированном .example, вручную классифицирован; реальных секретов не найдено.
Linux без Windows compiler/DPAPI/native: compilation и native NOT_RUN. Код ещё не
подключён к service/UI, не опубликован и не входит в running609. Баллы не изменены.
Следующий этап: единый retained private-write slot для home/share в HomeVpnService,
immutable Access/server capture и logical acceptance, cancel/Stop/Dispose/stale
scope native cases; прежний UI/recovery receipt boundary отдельно. Проверять тот
же609, не создавать дубликат. I58–80/Y22 exact608,0 роста; whole R09 FAIL.

## CI609 опубликован — новый адресный/modal блок проверяется

Публикация подтверждена фактическим head PR88: source
`34740802206b6bd24ec41838ba25c44980ddec6c`, merge
`0da565ba835dbf63fc2f402eafdac3f380f23624`, полный tree
`4752a5e04f2b38c4516d969f466bf163f10abee6` = isolated local
`968fe3d461f402a892ebf8662cf84c2eb59223e6` (ProGo-ci608-followups).
[CI609/39733721723](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39733721723)
единственный известный запуск для этого source: Ubuntu119674440425 SUCCESS,
Windows119674440595 in_progress (Repository hygiene and build). Compilation/
native нового source остаются NOT_RUN до чтения завершённого результата.
Проверенный ранее CI608 SUCCESS относится только к source479addfd/merge36f39fb0.

Перед CAS479a→3474 проверены все11 файлов:8 source/test +3 evidence docs,
secret/PII0, public320/0, source/C#5 structure, diff, JSON31+55 и полное совпадение
tree. Репозиторий public перепроверен; workflows/VERSION unchanged, автоматическая
загрузка бинарных артефактов OFF, release не запускается этим PR/branch push.
Никакого повторного push/CI: после обрыва сначала проверить этот же run.

Новый scope: staged protected home-address write, retained service ownership/
Stop/Dispose/commit admission, actual async wizard save и явный DialogResult.None
после CancelButton для трёх ожиданий. Написаны9 store +7 owned-save +7 write-modal
и3 read-modal cases с ShowDialog(), bounded deadline и owned cleanup. До результата
609 это не Windows PASS. Полный R09/R10/R11/owner/installer acceptance остаётся
открытым. Общий startup Program.Main/ShowQr→VPS и целевой ПК NOT_RUN.

Следующая независимая микрозадача: общий staged private-address primitive для
captured per-server HTTPS origin с безопасным фиксированным destination; сохранить
home-address контракты и добавить native cancellation/refusal/real-file-lock cases.
Далее — owned share persistence и прежний same-request receipt boundary. Изменения
не входят в609; не менять текущий проверяемый source ради checkpoint-документов.
Реализация58–80/100, проверенная готовность22/100 (raw21.75, exact608), изменение0.
Ниже — история, включая запись object до фактического CAS.

## Pending address/modal publication — object3474, ref NOT_RUN

Object `34740802206b6bd24ec41838ba25c44980ddec6c`, parent479addfd6f711831a12e1c7b49019a446f7970ce,
full tree `4752a5e04f2b38c4516d969f466bf163f10abee6` = isolated local
`968fe3d461f402a892ebf8662cf84c2eb59223e6` in ProGo-ci608-followups.
11files1424add299delete: frozen8 related address source/test paths +3 evidence
docs. SecretPII0/public320/0/source/C#5/diff/JSON31+55/full-tree equality PASS.
Workflows/VERSION unchanged/public uploads OFF; integration PR/push не trigger
release.608 completed SUCCESS, новый head не отменяет running predecessor.

New address writer/real modal phone fix Windows compilation/native NOT_RUN;
608 не наследуется. Future share persistence implementation excluded (trace
only in docs). I58–80/Y22 raw21.75 exact608 unchanged. После обрыва сначала actual
PR head/run до retry. Далее CAS expected479a→3474 without force, один automatic
PR CI; record exact merge/run/jobs. No binary/release/main/owner PC/VPS actions.

## CI608 full SUCCESS — live read routes, address/modal-save excluded

[CI608/39730132565](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39730132565)
Windows119663025746/Ubuntu119663025549 SUCCESS. Exact source
`479addfd6f711831a12e1c7b49019a446f7970ce`, merge/build identity
`36f39fb080e0195cad40922793b298a5da500914`, tree22821ad5fdf6f654deea6eb0ab293e629b817b79
= local8417f9a9b9d51cadeac8a29088a689d4b602c0f1. Program selects background API.
New narrow native: real configure action actual protected read/cancel/retry
06:08:26.982–27.238, queued close/Dispose/no late dialog06:08:27.369–744;
production background imported-data reload06:11:56.2206615 and memory share
getter with actual exclusive source lock06:11:56.2207408. Эти wizard parent cases
использовали Show(), не подтверждают реальную modal CancelButton семантику.
Новая explicit-None fix/ShowDialog regression882 в этот source не входят.

Full regressions: Desktop5280/HomeVPN1195/Portable91;51/456, report32/diagnostic
modal6/chooser3/Context ownership, selected-key signing55/UI50/prep72 и actual
direct/owned-agent OpenSSH→SOCKS→HTTP→crash→fresh preparation, Instance28/
Maintenance126/Shutdown47. Python28=26PASS2SKIP. Runner ZIP0.2.2 SHA256
`cc39eef38418287617e8bc2ddb9c175a7b0bbe8474ee477b428bebc4d2bb7ab2`.
All uploads SKIPPED; bytes не получены/не доставлены/не установлены; final setup/
owner acceptance NOT_RUN, delivery BLOCKED до согласованного приватного канала.

V1.0 I58–80, Y21.75→22 unchanged: same28 parents weight87, D0.5/P0.5/A0.
27 прежних anchors выполнены с тем же текстом; F22 copy/import reload теперь
имеет новый stronger background-constructor anchor того же partial критерия,
не другой вес/уровень и не дополнительный балл. Whole R09 FAIL/R11 PARTIAL.

Следующее source — frozen address draft `d6517e871854bb1d161bde55c6a3282a40339a01`
8 source/test paths553add52delete: primitive418/servicecf29/actual modal UI882,
9 store+7 owned-save+7 actual write-modal и3read-modal сценария написаны; compiler/
native NOT_RUN. Добавить только exact608 evidence docs, full scan/tree и один
expected479a publication/CI. Дальнейшие share persistence changes excluded;
узкий trace в PROJECT_STATE сохраняет прежние owner/request/receipt boundaries.
Не приписывать608 успех home-address writer, modal phone fix или всему R09.
No binary/release/main merge/owner PC/VPS actions. Ниже checkpoints — история.

## R09 share persistence trace — acceptance boundary сохранён как требование

Узкая source finding подтверждена, runtime latency под slow I/O здесь NOT_RUN:

| Файл / символ | Подтверждённая работа | Остаток / проверка |
|---|---|---|
| HomeProfileShare.CreateConfigureForm / refreshGate | HasPending читает private root/file на UI, включая constructor/refresh | Owned pending-state read до доступности действий, cancel/closed owner gate |
| HomeProfileShare.run mode0 / dispatcher acceptance | canSave и saveOrigin выполняются на UI, SetShareOrigin содержит DPAPI/Save | Captured origin+access, single private-write owner, подготовка worker и commit admission |
| HomeVpnShareRecovery.ConfirmAsync / finalizeOnOwner | save(origin), повторная проверка request и File.Delete journal в одном UI callback | Сохранить один logical acceptance, перенести file/DPAPI/receipt I/O в retained worker |
| HomeVpnShareRecovery.AcquireAsync | gate.WaitAsync(0) может завершиться сразу; сам await не гарантирует worker для Load/RequireResult | Явный worker для private read/validation, без UI control access с него |
| HomeProfileUiDispatcher.DispatchAsync | owner/queued completion gate защищает late publication; его нельзя обойти fire-and-forget save | Immutable ticket/cancellation identity, сохранение/consume до OK, no new external action |

Существующие native contracts (точно tested607, не новые tests): same request ID/
origin/result, save failure retains journal/former origin, disposed owner prevents
late save, queued health acceptance cancellation, wrong proof refusal, cleanup
failure preserves pending action. Их не заменять моками и не ослаблять.

Следующие микроэтапы после address checking: (1) shared owned private address
writer для home и captured per-server HTTPS origin, compatible APIs и native
serialize/cancel/stale-access cases; (2) typed request/receipt preparation worker
и scope admission, persisted origin→same-journal consumption на worker под прежним
lease, отказ cleanup не выдаёт OK/не повторяет SSH; (3) actual configure verify/
recover routes await physical persistence, modal owner-close/cancel/IO regressions.
Никаких новых продуктовых функций/весов; точные критерии F31-R09 unchanged.
Не реализовывать Action save через Task.Run без этого ownership/receipt boundary.
CI608 и frozen addressd651 source не меняются. I58–80/Y22 exact607,0 роста.

## Address checking draft заморожен — locald6517e8, пока не опубликован

ProGo-ci608-followups `d6517e871854bb1d161bde55c6a3282a40339a01`,
tree `c74dea236b52d32ea449c8463f72ad379f19f24c`, base8417 (published479a tree).
Только8 related source/test paths: protected address primitive418, owned service
writecf29, actual modal wizard async save/cancellation882, command/main gates.
8files553add52delete, source/syntax/C#5 structure/secretPII0/public320/0/diff PASS;
Windows compiler/native NOT_RUN. Source frozen; docs after exact608 result.

CI60839730132565 unchanged, Windows repository in_progress/Ubuntu SUCCESS.
Во время него head-only push/duplicate не выполнять. Дальше exact608 checking,
потом этот address draft +evidence docs. Пока ожидание — narrow trace согласованной
share persistence/recovery acceptance перед async переносом: нельзя просто
Task.Run прежний save callback и потерять owner/request/receipt boundary.
I58–80/Y22 exact607 unchanged,0 роста; full R09 и target acceptance открыты.

## R09 address write stage3 — local88295d0, Windows NOT_RUN

Source `88295d0758f1a6d18e51f9f18761eba0411cc5db`: actual wizard step2 awaits
owned SetHomeAddressAsync with captured UI input; no filesystem wait on UI.
Cancel/close await physical result, pre-admission cancel preserves old address,
already accepted save reports saved without claiming rollback or auto-advancing.
Verification resets on every accepted saved address. Controlled Russian I/O/
permissions/DPAPI errors restore retry. Context/tray/dashboard block competing
phone wizard during owned save; Stop/desktop/settings remain available.

Review found shared CancelButton assignments inherit implicit DialogResult.Cancel,
same pattern as earlier diagnostic defect. Source contract fixed by explicit None
AFTER assignment in channel/read/save routes. This is source-confirmed risk;
actual modal runtime reproduction/fix acceptance NOT_RUN, old Show() tests do
not establish it. Read cases converted to real ShowDialog and native timer turns;
new write cases7 use real ShowDialog too: cancel/retry, close, Dispose, accepted
cancel/close/Dispose, real sharing error→retry. Driver deadline15s +bounded own
worker cleanup; only fixture windows/objects/files. No external SSH/HTTP actions.

Source6-file syntax/C#5 structure/diff/secretPII0/public320/0/actual async route/
3 explicit nonclosing assignments/ShowDialog driver checks PASS; compiler/native
NOT_RUN. Address418/cf29/882 excluded608. CI60839730132565 source479a/merge36f3
continues unchanged; Ubuntu SUCCESS, Windows repository step in_progress.
I58–80/Y22 raw21.75 exact607 unchanged,0 points. Далее freeze these3 related
address blocks separately, await exact608 result, publish one new checking only
after exact scan/tree and evidence docs. Token/share/recovery I/O still open.

## CI608 — live startup/wizard checking опубликован, один run

Source `479addfd6f711831a12e1c7b49019a446f7970ce`, merge candidate
`36f39fb080e0195cad40922793b298a5da500914`, full tree
`22821ad5fdf6f654deea6eb0ab293e629b817b79` = local8417f9a9b9d51cadeac8a29088a689d4b602c0f1.
[CI608/39730132565](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39730132565)
in_progress, Windows119663025746 repository step, Ubuntu119663025549 SUCCESS.
CAS expectedab6a→479a без force PASS; actual PR88 head/merge независимо
подтверждены, body updated.7files1009add260delete scan0/public320/0/source/diff/
JSON/full-tree PASS; workflows/VERSION unchanged/public uploads OFF.

Теперь опубликованы Program background route и wizard configure/QR saved-address
worker, actual UI cancel/retry/close/dispose tests, portable production reload.
Windows compilation/native NOT_RUN для608 до его exact log.607 SUCCESS относится
только кab6a/73f9; address primitive418/servicecf29 и stage3 UI excluded608.
I58–80/Y22 raw21.75 exact607,0 роста. Во время608 head не менять ради docs и не
создавать duplicate. Далее этот же run, независимо local wizard async address
save stage3 и целевые tests. No binary/release/main/owner PC/VPS actions.

## Pending live startup/wizard publication — object479a, ref NOT_RUN

Object `479addfd6f711831a12e1c7b49019a446f7970ce`, parentab6a16fda8a80adb494bc7245af867c04487ac18,
full tree `22821ad5fdf6f654deea6eb0ab293e629b817b79` = isolated local
`8417f9a9b9d51cadeac8a29088a689d4b602c0f1` in ProGo-ci607-followups.
7files1009add260delete:4 frozen source/test production route paths +3 evidence
docs. Exact scan secretPII0/public320/0/source syntax/C#5/diff/JSON31+55/full-tree
PASS; workflows/VERSION unchanged/public uploads OFF, integration push/PR не
trigger release.607 completed SUCCESS, no active source head update отменяется.

Address primitive418/servicecf29 excluded, их Windows NOT_RUN. New479a native/
compiler NOT_RUN;607 не наследуется. I58–80/Y22 raw21.75 exact607,0 роста.
После обрыва actual PR head/run проверить до повторения ref. Далее CAS
expectedab6a→479a without force, один automatic PR CI и точные source/merge/run.
No binary/release/main/owner actions. Затем actual wizard async home save stage3
локально, пока новый checking проверяет неизменный frozen source.

## CI607 full SUCCESS — staged API/gates/refresh native подтверждены

[CI607/39727292233](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39727292233)
Windows119654028621/Ubuntu119654028836 SUCCESS. Exact source
`ab6a16fda8a80adb494bc7245af867c04487ac18`, merge/build identity
`73f972a9a09f3f56d6d120bd0625bc83273f935e`, tree9876155a50502a23e0c377d9e7c9507950f45a76
= local2fbe4d3c8b006136864a24b5911749c8e814148a. Новые scoped native PASS:
actual Context/tray/dashboard held read/no modal/action restore05:30:31.820–854;
owned initialization load/Stop/Dispose/Stop→Dispose05:37:21.410–793;
optional owner/home/share real lock warning +basic own process/local relay
start05:37:21.937–27.900; share cancel/stop/dispose/no late value05:37:28.196–511,
newer-save protection05:37:28.642, real unlock→retry05:37:28.877.

Full native regressions проходят: Desktop5280/HomeVPN1165/Portable90; остальные
наборы51/456, diagnostic report32/modal6/chooser3/Context ownership, selected-key
signing55/UI50/prep72 и actual direct/owned-agent OpenSSH→SOCKS→HTTP→crash→fresh
preparation, Instance28/Maintenance126/Shutdown47. Late Python28=26PASS2SKIP.
ZIP0.2.2 SHA256 `35c034cc25c428f6abc16c5a58ded57c7c8e2792090d806c7bdd5099bc241785`.
All uploads SKIPPED; bytes не получены/не переданы/не установлены, installer и
owner acceptance NOT_RUN. Private delivery BLOCKED до согласованного канала.

Program source607 ещё legacy: не приписывать этому run live default switch/
wizard5cce/eb97, а также storage418/servicecf29. Whole R09 FAIL, R11 PARTIAL.
V1.0 I58–80, verified21.75→22, изменение0: те же28 parent anchors переведены
на exact607, D0.5/P0.5/A0 weight87 без роста уровней и двойного учёта подпунктов.

Следующее проверочное source — frozen production-route localda77228; обновить
только3 evidence docs и выполнить один expectedab6a publication после scan/tree.
Source4 paths не меняются. Local address persistence primitive418 и service
`cf29d23e4e67105ba56024e8c0be346ac2677388` исключить из него: API task-before-I/O,
immutable input/Stop join/cancel-before-commit/accepted-save completion/retained
cleanup+7 native vectors написаны, source/syntax/C#5/public/diff PASS, compiler/
native NOT_RUN. Actual wizard still uses sync address adapter; stage3 отдельно.
No binary/release/main merge/owner PC/VPS actions. Ниже checkpoints — история.

## R09 address write stage1 — local418bf18, primitive only

Source `418bf181114b6a99dc46030ad570d9d14f3bcf9a`: HomeVpnAddressWrite создаётся
и удерживается владельцем до worker I/O; fixed home-address target и unique
CreateNew encrypted stage, DPAPI plaintext array clearing, flush/verification.
Commit checks captured previous/staged bytes then memory-only admission; после
принятия atomic Replace/Move cancellation не изображает rollback. Dispose удаляет
свой stage, отказывает изменённому staged file и сохраняет object для cleanup retry.
Частичная подготовка также остаётся у caller до cleanup, не теряет ownership.

9 native cases written: create/replace/refuse/cancel/real sharing failure/newer
source/changed stage/locked cleanup→retry/cancel after admission. Held actual
DPAPI staging, heartbeat/thread, old bytes and explicit cleanup ownership.
Syntax/C#5 structure/diff/public320/0 PASS; Windows compiler/native NOT_RUN.
No live service/UI route yet; excluded607 и frozen production routedraftda77228.
I58–80/Y22 exact606 unchanged. Далее stage2 owned service task +Stop/Dispose join,
затем actual wizard async save. Token/share/recovery writes остаются отдельными.

## Следующий production-route checking заморожен — localda77228

ProGo-ci607-followups local `da77228cbbae056090e0de6cde6f4804826b8a10`,
tree `5ca2a8c99ef2b0017ec8c440f5ee072627428700`, base2fbe (publishedab6a tree).
Только4 finished source/test paths: Program background ctor, wizard configure/
QR saved-address worker, actual wizard cancel/retry/close/dispose tests, portable
reload background/memory getter test.4files140add11delete, source/syntax/C#5/
secretPII0/public320/0/diff PASS; Windows NOT_RUN. Evidence docs обновить после
607 result; не публиковать до него и не смешивать следующие private writes.

Дальше R09 single-file home-address write разделён заранее:
1. Prepare protected unique temp on worker, flush/verify, cancellation до принятия
   commit; atomic Replace/Move и cleanup только своего temp. Native baseline/
   refusal/cancel/real sharing tests. Пока standalone primitive, UI не подключён.
2. Service publishes owned write task до I/O, immutable input и scope; Stop/Dispose
   отменяют ещё не принятый commit и join actual worker; принятый atomic commit
   завершается с честным результатом. Native held-I/O/cancel/acceptance tests.
3. Wizard ждёт этот task асинхронно с понятной отменой/close; no late owner UI,
   actual native action tests. Token pair/share/recovery write workers отдельно.
Не объявлять helper/написанные tests полным R09 PASS,0 баллов до доказательств.
CI60739727292233 продолжает прежний source; I58–80/Y22 exact606 unchanged.

## Восстановление соединения22.10.2026 — работа после589 сохранена

Actual Git clean во всех16 worktree; других active agents нет, локальные
compiler/git/SSH operations не найдены (только текущие sandbox wrappers).
Ничего не отменялось/не удалялось/не повторялось. Direct+agent candidate был
опубликован: source8487594e6d7d4e4eb16df86aae27f2e980329aaa /merge
20df578c1c2d162f5f373e94aed8107bbfee7d19, CI59039694720495 FAILURE launcher.
Исправления дошли до independently queried59539704288681 SUCCESS source5f3420d7;
последний full SUCCESS60639724309390 exactbf1c/cd20 включает actual direct/agent
SSH→SOCKS→HTTP→crash→fresh preparation. Не повторять старую публикацию после589.

Current PR88 OPEN/DRAFT sourceab6a16fda8a80adb494bc7245af867c04487ac18 /merge
73f972a9a09f3f56d6d120bd0625bc83273f935e; CI60739727292233:
Windows119654028621 repository step in_progress, Ubuntu119654028836 SUCCESS.
Следующая проверка именно этого run, без duplicate/head update. Сборка/пакет
нового source ещё не подтверждены, Windows compilation/native NOT_RUN.

Latest local source `eb97c13e40128d74754811c0e2e2667cf920a4c9` подключает Program
к optional background constructor (true), который уже имеет loading gates и
owned read/Stop. Portable reload test теперь использует именно эту route:
bounded5s read/cleanup и memory share getter при реальном FileShare.None.
Source/syntax/C#5 structure/diff/public320/0 PASS; native NOT_RUN. Legacy1arg/
5arg constructors оставлены совместимыми для существующих fixtures; все
production HomeVpnService creation sites — Program, теперь async. Initial
SettingsService/EnsureDirectories и private writes/recovery остаются отдельным
sync долгом: весь startup/R09 не закрыт. Этот local block и wizard5cce excluded607.

Первая незавершённая работа: проверить consistency/ownership production startup
и wizard route перед следующим exact checking после607. Далее async private
writes отдельными малыми этапами. I58–80, Y22/raw21.75 только606,0 роста.
Public source publication разрешена только после scan/workflow check; binary/
release/main/owner-PC/VPS действия не выполнялись. Ниже checkpoints — история.

## CI607 — staged initialization checking опубликован, один run

Source `ab6a16fda8a80adb494bc7245af867c04487ac18`, merge candidate
`73f972a9a09f3f56d6d120bd0625bc83273f935e`, tree
`9876155a50502a23e0c377d9e7c9507950f45a76` = local2fbe4d3c8b006136864a24b5911749c8e814148a.
[CI607/39727292233](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39727292233)
in_progress, Windows119654028621/Ubuntu119654028836. CAS expectedbf1c→ab6a
без force PASS; после короткой задержки actual PR head/merge независимо
подтверждены. PR88 body обновлён. Source9-file exactscan/tree PASS, workflows/
VERSION unchanged/public uploads OFF; binary/release/main/owner actions не было.

Staged init/warnings/loading gates/share refresh входят. Root wizard5cce и
следующий default startup/private write blocks excluded; все новые Windows
compiler/native NOT_RUN.606 SUCCESS только bf1c/cd20, баллы I58–80/Y22(raw21.75)
не переносятся как SUCCESS на607;0 роста. Во время607 не делать head-only push
или duplicate. Далее этот же run/log; независимая local default startup/cache
integration, затем private write workers. Ниже checkpoints — история.

## Pending staged R09 publication — object создан, ref ещё NOT_RUN

GitHub object `ab6a16fda8a80adb494bc7245af867c04487ac18`, parent
`bf1c1e5cff7151b4d0f11d8831a7e3b7a2e0f1dc`, full tree
`9876155a50502a23e0c377d9e7c9507950f45a76` = isolated local
`2fbe4d3c8b006136864a24b5911749c8e814148a` (ProGo-ci606-followups).
9files1722additions343deletions: frozen6 source/test paths и3 evidence docs;
added secret/PII0/public320/0/C#syntax/C#5 structure/JSON31+55/diff/full-tree
match PASS. Workflows/VERSION unchanged; public uploads OFF; release не
triggered integration PR/push. No other active agent; actual PR headbf1c.

Staged initialization/optional warnings/loading gates/owned share refresh и
native tests входят; wizard5cce/default startup/private writes excluded.
Windows compiler/native NOT_RUN для new object; exact606 SUCCESS не наследуется.
I58–80, verified22/raw21.75 exact606, изменение0.606 completed, следующий head
его не отменит. После обрыва actual PR head/run проверить до ref retry.
Следующее: CAS expectedbf1c→ab6a без force, один новый автоматический PR CI;
source/merge/run записать отдельно. Никаких binary/release/main/owner actions.

## CI606 full SUCCESS — sourcebf1c / mergecd20, staged R09 ещё NOT_RUN

[CI606/39724309390](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39724309390)
Windows119644614336/Ubuntu119644614044 SUCCESS. Build identity
0.2.2+cd20daf85da786d42244e7ce17fc83726b81fbcc проверен по log;
source `bf1c1e5cff7151b4d0f11d8831a7e3b7a2e0f1dc`, full tree
`8cf494f4ab7c2dfc247a2ad8f96689632f3cb983` = local0152426bd4c6b8723e3232a0463fc8c224296cce.

Новые narrow native PASS: committed repair cleanup04:47:52.4180059 (реальный
locked old snapshot, -Recover только residue cleanup); coherent DPAPI reader
05:08:39.9645891 +cancel/conflict/lock/defaults; failed old cleanup refuses token
import before DPAPI/identity change05:09:30.2421268. Это не закрывает весь R09.
Main regression51/456; bounded43/report32/helper2; actual modal Cancel→Retry,
chooser3/Context ownership, signing55/UI50/prep72 +real direct/owned-agent
prepare→native SSH→SOCKS→HTTP→crash/fresh recovery PASS. Desktop5274/Instance28/
Maintenance126/Shutdown47/HomeVPN1051/Portable90; SSH contracts5; Python28=
26PASS2SKIP. Exact28 parent anchors обновлены, веса и уровни не повышены.

Runner ZIP0.2.2 SHA256 `198a7ed7d2e2839e3be023814a0273ef62bc20a08e2a3e4ea0510635b0f73822`.
All uploads SKIPPED. Bytes не получены/не переданы/не установлены; installer и
owner acceptance NOT_RUN, передача BLOCKED до разрешённого приватного канала.
I58–80, verified21.75→22, изменение0: D0.5/P0.5/A0 для тех же28 parents weight87.

Frozen next checking local `dfe70563a136a21edeae1b514a219a067a0a64a7` /tree
`7016b466f779fc02168e538626e6f610780325ea` содержит только staged API/optional
warnings/loading gates/owned share retry +tests и docs:9files661 additions,
scan0 secrets/PII/public320/0/syntax/C#5 structure/diff PASS. Windows NOT_RUN.
Перед publication evidence docs обновить до exact606, source files не менять.

Отдельный root local `5cce98404647a2dd4e0ea4d88f13daf9957a9018` подключает
owned cancellable share read к configure/QR actions wizard. Written actual UI
Cancel→Retry/queued close/Dispose cases с реальным protected read и own configure
modal, без SSH/HTTP действий. Syntax/C#5/source/public/diff PASS; compiler/native
NOT_RUN. Этот UI block исключён из frozen checkingdfe; Program/default ctor и
getter пока legacy. R09 private writes/recovery I/O остаются FAIL/open.

Следующее: обновить checking docs, exact scan/tree, expectedbf1c публикация одного
source в PR88 и один CI. Пока идёт его Windows — отдельно default startup/cache
integration; не приписывать ему606 SUCCESS. No binaries/release/main merge/owner
PC/VPS actions. Ниже все checkpoint-записи — история.

## R09 owned share read/retry — locale385513, routes ещё не переключены

Source `e38551307dd16be3410b3957ddd5a746c888725f`: RefreshShareOriginAsync
публикует retained task до I/O; duplicate join, immutable profile ID/fallback,
DPAPI/cohort reread off UI, очищение временных массивов. Generation guard не
публикует старый результат поверх явно сохранённого нового адреса; внешний
conflict — честный отказ. Stop отменяет admission memory-only до worker cancel
и join actual read; Dispose не даёт late cache update. Source/private writes
не выполняются reader. Синхронный setter остаётся отдельным R09 долгом.

Written native vectors clean/cancel/Stop/Dispose/newer-save/external-change,
real lock→unlock→retry, UI heartbeat/thread/duplicate ownership/source bytes;
каждый worker/Stop наблюдается до восстановления своих private fixtures.
Modern syntax/targeted C#5/diff/public320/0 PASS; Windows compilation/native
NOT_RUN. Program/default ctor/getter и QR/configure routes пока legacy; этот
API не объявлен подключённым. I58–80, verified22 exact605,0 новых баллов.

CI606 sourcebf1/mergecd20, run39724309390 не менялся: Windows119644614336
repository step in_progress, Ubuntu119644614044 SUCCESS. Не делать head-only
push/duplicate. Далее exact606 результат; отдельный checking API+availability+
loading gates+refresh на этой проверенной базе, затем actual UI/startup switch.
Ниже checkpoints — история.

## R09 loading gates — local7d9d80f, default startup ещё legacy

Source `7d9d80fbdf35309c10f13179cf2c0554281df830`: AppCommandState имеет отдельный
PhoneInitializing; actual Context dispatcher и tray не открывают мастер до конца
owned read. MainWindow показывает чтение/ошибку, обе точки входа телефона
согласованы; StopPhone/StopAll/settings/desktop продолжают работать. Добавлен
native Context+dashboard held-read test: heartbeat, pending menu/buttons/status,
rejected stale command, bounded own-modal guard, восстановление entry points.
Fixture освобождает barrier и наблюдает retained initialization/Stop до выхода.

4-file syntax/targeted C#5/diff/public320/0 и public-doc tests3 PASS; Windows
compilation/native NOT_RUN. Это staged route, Program/default ещё не активирован.
Новый block не входит в606. I58–80, verified22 exact605,0 новых баллов. Далее
owned share read/retry, затем default startup integration и один exact checking
после результата606. Private writes R09 остаются открыты.

## R09 optional metadata availability — local00a1831, production ещё legacy

Local source `00a183199da722a1d97441ad4b36838de3100aea` сохраняет valid Access,
если optional owner/home/share недоступны: отдельные warning flags, честный
отрицательный read outcome, без подмены share непроверенным fallback. Mandatory
access/cohort conflict остаются отказом. Добавлены native real-lock cases для
трёх optional файлов с запуском собственного process/local relay и проверкой
неизменности private bytes. Это staged API, не переключение Program.

Diff/public320/0/modern syntax/targeted C#5 structure PASS; Windows compiler/native
NOT_RUN. Source606 не содержит этот блок: Windows119644614336 in_progress,
Ubuntu119644614044 SUCCESS в run39724309390. Без нового push/duplicate CI.
I58–80, verified22/raw21.75 только exact605,0 новых баллов. Далее отдельные
Context/main loading gates и native held-read regression; перед production
activation нужен owned refresh/retry, затем startup switch. Sync private writes
и весь R09 остаются открыты. Ниже checkpoints — история.

## CI606 — finished blocks checking опубликован, один run

Sourcebf1c1e5cff7151b4d0f11d8831a7e3b7a2e0f1dc, merge candidatecd20daf85da786d42244e7ce17fc83726b81fbcc,
full tree8cf494f4ab7c2dfc247a2ad8f96689632f3cb983 = local0152426bd4c6b8723e3232a0463fc8c224296cce.
[CI606/39724309390](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39724309390)
in_progress. Expected163d→bf1c lease/actual PR88 head+merge подтверждены, body
updated.6files2048 additions secret/PII0/public320/0/syntax/diff/tree PASS;
workflow/VERSION unchanged/public uploads OFF. Guard/read primitive/committed
extra native vector впервые входят в source606, их Windows пока NOT_RUN.

Staged initializerad3/optional-metadata/Context/Program switch НЕ входят. Для него
до активации нужно сохранить basic access при ошибке optional metadata; это
следующая local микрозадача. Не приписывать source606 успех605, не запускать
duplicate/head-only docs CI. После обрыва сначала этот же run/head/log.
I58–80, verified22/raw21.75 только exact evaluated605,0 баллов за новый checking.
No binary/release/main merge/owner PC/VPS actions. Private delivery/setup/target
acceptance остаются blocked/NOT_RUN. После606 результата — exact native evidence,
а не новая интерпретация старых SUCCESS.

## Pending finished followup checking publication

Objectbf1c1e5cff7151b4d0f11d8831a7e3b7a2e0f1dc parent163d97436d68f5169a8a22ea9f17d38344f8ad88;
full tree8cf494f4ab7c2dfc247a2ad8f96689632f3cb983 = local0152426bd4c6b8723e3232a0463fc8c224296cce.
6files2048 additions secret/PII0/public320/0/source syntax/diff/tree PASS;
workflow/VERSION unchanged, uploads OFF,605 EOF fixture fix унаследован.
Только законченные source3 blocks (guard/read primitive/committed extra test) и
актуальные3 docs. initializerad3/optional metadata/UI integration excluded.

Ref ещё NOT_RUN в этот момент. После обрыва actual PR head/run проверить прежде
повтора.605 завершён, новый head его не отменит. New Windows compilation/native
NOT_RUN; read primitive всё ещё не подключён к startup. I58–80, verified22/raw21.75
только exact605;0 новых баллов за checking. Далее expected163d→bf1c и один CI,
без binary uploads/release/main merge/owner PC/VPS действий.

## CI605 full SUCCESS — exact163d/522ca, новые local blocks excluded

[run39721135629](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39721135629)
Windows119634576903/Ubuntu119634576608 SUCCESS. Build identity
0.2.2+522ca8616d014fec2ebafd176d9a4365e9281e1e совпадает; source163d97436d68f5169a8a22ea9f17d38344f8ad88,
treec8b4209c4faebfb42ff33774f9bc2db16f43cd7d = local7b3330c35b49306be18740c17f10968532a95921.
Alive assertions PASS04:36:20.697 Waiting и04:37:11.047 Preparation. Own pipe EOF
lifetime исправил unreliable fixture, actual preservation теперь подтверждено.

Repair PASS: null1/preflight4/full41 +actual image/fresh Root/default known-folder
untouched04:13:32.442/real sharing rollback/contained hang/hard-kill recovery
without original payload/9 strict refusals. Regression51/456, bounded43/report32/
helper2/modal6/chooser3/context3/worker4/routes6, signing55/UI50/prep72 +real direct
и owned-agent prepare→native SSH→SOCKS→HTTP→crash recovery; Desktop5274/Instance28/
Maintenance126/Shutdown47/HomeVPN1022/Portable90. Native SSH server contracts5;
late Python28=26PASS2SKIP (SKIP не PASS).

Runner ZIP0.2.2 SHA256 `3f40efcfa8a109e166509e42eb75b0fceecd9dc87c5a959873a7c99ca69e071e`.
All uploads SKIPPED. Bytes не получены/не доставлены/не установлены. Private
пакетная передача BLOCKED до разрешённого владельцем канала; successful runner
archive не является предоставленным installer. Release/main/owner PC/VPS не было.

V1.0:28 parents weight87 с exact anchors, D0.5/P0.5/A0 дают21.75→22. +8.70 raw
к60413.05 — реально выполненный late tail/ZIP, без смены весов/двойного учёта ID.
I58–80 unchanged. Whole F22/23/R08/R09/R10/R11 и target acceptance остаются partial/
blocked. Local checking6353 (guard/read primitive/committed extra case) и staged
initializerad3 НЕ входят в605: их Windows NOT_RUN/баллы0, old SUCCESS не наследуют.

Далее checking6353 плюс актуальные evidence docs, exact diff/secret/workflow/tree
check и один новый source/run. initializerad3 пока отдельно: optional metadata
availability нужно исправить до UI/Program switch; нельзя публиковать случайно
неоконченные стадии вместе с checking. Ниже checkpoints — история.

## R09 stage1 service ownership API — local ad3fa3e, production NOT_RUN

Sourcead3fa3e037a9a8fb82ad31c9e753de1f09759657. Optional background constructor
публикует Task/CTS ДО worker, primitive читает real DPAPI/files; поле состояния
публикуется коротким memory gate только в действующем service scope. Start/Stop
join initial task; Dispose cancels read даже после ранее pending Stop и не даёт
позднего metadata update.1arg/5arg ctor API сохранены; Program/default route ещё
legacy sync. Share cache действует только в new mode, production не переведён.

Written native modes load/Stop/Dispose/Stop→Dispose +locked access: real read
barrier, UI heartbeat/off-thread, no partial identity/session/child, cancelled
initialization settles before cleanup, cached share getter under real file lock.
Все retained workers/starts/stops наблюдаются перед восстановлением private data.
C#syntax/C#5 structure/task-before-I/O/source/diff/public320/0 PASS; Windows
compiler/native NOT_RUN. Не входит в605 и не меняет frozen checking6353.

Review до переключения UI выявил prototype availability regression: full-cohort
fault по optional share/owner/home сейчас оставляет new-mode Access null, хотя
прежний constructor не требовал share для базового канала. Current production
legacy route этого нового поведения ещё не использует. Следующая отдельная
микрозадача — optional metadata warning/совместимая доступность +native test;
не включать новый путь в Program до исправления. После этого loading gates,
startup switch, explicit safe share refresh и private write workers отдельно.
Compatibility UseToken всё ещё sync, весь F31-R09 FAIL/не закрыт.

I58–80, verified13/raw13.05 exact604,0 баллов за new API/tests.605 run39721135629
Windows119634576903 repository step SUCCESS, late native relay in_progress;
Ubuntu119634576608 SUCCESS, full result/ZIP ещё не получены. Current source163d
не менялся, следующие Win checks нельзя приписать этому local API.

## Сохранённые checking drafts и следующий R09 initialization block

Pre605 draft2bbd52615795c5d5123987fc4b7e22d981fc8f4e сохранён отдельным local
commit в ProGo-ci604-followups; stale a859 base, не публиковать с возвратом fixture.
Актуальный checking6353f95cb872d15c3f8302c0d9de887fe0b3942b/tree01dc308e4d8b12a1c6c78739a3d5225104a85e0a
в ProGo-ci605-followups уже на base7b3330c, stdin lifetime fix унаследован. Только
3 finished source/test paths: token cleanup guard/read primitive/committed cleanup
extra native case. Source/syntax/secretPII0/public320/0/diff PASS, Windows NOT_RUN.
Current605 не менялся. Следующий этап не должен случайно попасть в этот draft.

R09 initialization крупнее одной микрозадачи, разбит заранее:
1. Service-owned init task публикуется до worker I/O; Stop/Dispose удерживают/
   join actual read, Dispose cancels/no late metadata. Native held-read cases.
2. Actual Context/main/wizard loading gates и honest pending/error; bounded
   shutdown через retained StopWork, никаких UI getters с ожиданием worker.
3. Program/default startup переключается на worker, legacy constructor/ShareOrigin
   reads удаляются, реальные native ctor expectations адаптируются; producers
   shared-origin cache не должны стать устаревшими. Private writes отдельно.

Stage1 — API/ownership, production ещё не переведён до следующих шагов; ни один
этап не закрывает весь F31-R09. Все новые Windows NOT_RUN/баллы0 до exact evidence.
I58–80, verified13/raw13.05 exact604.605 run39721135629 Window119634576903
in_progress, Ubuntu119634576608 SUCCESS; не отменять/не дублировать CI.

## CI605 — unrelated lifetime fix опубликован, один run

Source163d97436d68f5169a8a22ea9f17d38344f8ad88, merge candidate522ca8616d014fec2ebafd176d9a4365e9281e1e,
full treec8b4209c4faebfb42ff33774f9bc2db16f43cd7d = local7b3330c35b49306be18740c17f10968532a95921.
[CI605/39721135629](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39721135629)
queued/in_progress. Expected46576→163d lease/actual PR88 head+merge подтверждены,
body updated.2files33 additions secret/PII0/public320/0/syntax/diff/tree PASS;
production/src/scripts/workflow/VERSION unchanged, public uploads OFF. New
Windows native/compilation NOT_RUN pending exact log. Не дублировать run/head.

Root token guard a75/7cc, committed repair extra83bcc8a/c39659f, stored-state
primitive501/886 исключены из605. После обрыва сначала этот же run/head. Пока он
идёт — preserve/refresh pre-fix dirty followup worktree перед будущей публикацией,
потом coherent R09 constructor/UI ownership: task до I/O, metadata publication
после scope check, pending action gates и retained bounded shutdown; не простое
Task.Run вокруг синхронного импортера и не getters, ожидающие worker на UI.
I58–80, verified13/raw13.05 exact evaluated604,0 новых баллов. Historical59822/
его ZIP не наследуются. Owner/private package/setup/real target остаются blocked.

## Pending unrelated fixture lifetime publication

Object163d97436d68f5169a8a22ea9f17d38344f8ad88 parent46576c73cf029b4ab0fc2d97ebbac07d15e91fa3;
treec8b4209c4faebfb42ff33774f9bc2db16f43cd7d = local7b3330c35b49306be18740c17f10968532a95921.
2files33 additions secret/PII0/public320/0/syntax/diff/tree PASS. Production/src/
scripts/workflows/VERSION unchanged, uploads OFF. Ref ещё NOT_RUN здесь, после
обрыва actual PR head/run проверить до повтора.604 completed, не будет отменён.
Root pending3 independent blocks не включены. New native NOT_RUN, I58–80,
verified13/raw13.05 exact604, прирост за fixed fixture0. Далее expected46576→163
source и единственный новый CI; не rerun604, alive assertions/2s cleanup сохранены.

## CI604 completed FAILURE позднего fixture; repair/main scoped PASS

Source46576c73cf029b4ab0fc2d97ebbac07d15e91fa3 / merge9e2c214a22d11ee1efded8ec2832e62b0bb7e2dd,
run39717548727 Windows119623188818 FAILURE/Ubuntu119623188527 SUCCESS. Build identity
совпала; repository step SUCCESS. Repair native PASS: null1/preflight4/full41
copy+actual self-check1/real known-folder untouched03:34:35.411/sharing rollback1/
contained hang1/hard-kill then explicit recovery without payload1/strict refusals9.
New explicit Root и native canonical image validation теперь имеют exact evidence.

Main regression PASS:51/456, bounded43/report32/helper2/modal6/chooser3/context3/
worker4/route6, selected signing55/UI50/prep72 +actual direct/owned-agent native
SSH→SOCKS→HTTP→crash recovery, Desktop5274/Instance28/Maintenance126/Shutdown47.
13 phone cases завершились PASS, но в03:56:54.690 final unrelated-alive check FAIL.
Source fixture сам завершался после Thread.Sleep60000; группа длится больше60s,
потому итоговый check не может отличить self-expiration от неверного cleanup.
Actual сохранность с исправленным lifetime ещё NOT_RUN; обвинение production
cleanup не подтверждено. Later Home/Portable/Python/content/package NOT_RUN,
все uploads SKIPPED, installer/private delivery/target не выполнены.

Isolated7b3330c35b49306be18740c17f10968532a95921/treec8b4209c4faebfb42ff33774f9bc2db16f43cd7d
(см. точный tree JSON; root importbe8a1d1d8aa1b28fbb4f23e3477af9ae7ee76a8b) меняет
только2 fixture files: собственная redirected stdin pipe вместо elapsed lifetime,
EOF при cleanup/смерти parent, оба callers Waiting/Preparation используют одну
схему. Alive assertions остаются, cleanup500+1500ms=прежние2s; app deadlines/CI
не увеличены, проверки не отключены. Source syntax/ownership/public320/0/diff
PASS, new native NOT_RUN. Один initial source check ошибочно искал sleep во всём
fixture, исправлен на unrelated branch; separate fake-SSH waits не менялись.

V1.0:28 parents weight87×D0.5×30%=13.05→13, P/A0. Все exact timestamp anchors
в JSON. F22 использует actual durable repair recovery критерий вместо NOT_RUN
portable tail; его вес не менялся/не добавлялся дважды. +13.05 к evaluated6030 —
выполненные native, не написанные tests. Historical59822 с P0.5 остаётся только
source37f02/16bf1/того ZIP. I58–80 unchanged. F22-R03 PARTIAL: узкие native PASS,
extra committed test/final package/owner acceptance всё ещё NOT_RUN.

Далее publish только isolated unrelated fixture fix. ProGo-ci604-followups имеет
3 dirty copied root source/test paths со старой a859 base: это наш preserved draft,
не другой исполнитель. Перед его будущей публикацией обновить base/включить
исправление7b3330c, чтобы не вернуть прежний lifetime. Root token guard/read
primitive/committed cleanup test остаются local NOT_RUN и не смешиваются с fix.

## F31-R09 read preparation primitive — local, constructor/UI NOT_RUN

Source5017167a249256255259669f21f88747bda6021a +
8863ea9a9d440988ce1562fa3485a9c771c3ab8a. HomeVpnStoredState.LoadAsync читает
access/owner/home/share и DPAPI только в actual worker, проверяет повторно тот же
ciphertext cohort, отказывает при concurrent change/cancel, не пишет файлы/ACL.
Результат сохраняет immutable private state, выдаёт отдельные mutable consumer
copies; временные ciphertext/plain arrays очищаются. Это подготовка чтения,
старый service constructor/ShareOrigin/private writes ещё НЕ переведены на неё.
Full F31-R09 остаётся FAIL, фактический UI startup по-прежнему требует следующего
согласованного подключения,0 баллов за невыполненный/unwired code.

Native tests written в existing HomeWaiting suite: real Form/heartbeat + held
read verification, off-UI thread, protected byte preservation; cancel ownership,
concurrent DPAPI change refusal/no rollback, actual locked file, partial/empty
saved state defaults without new files. Все retained tasks наблюдаются после
release native locks до восстановления четырёх своих private fixture files;
свои Form/timer/stream/CTS/events освобождаются. Windows compilation/native
NOT_RUN; syntax/C#5 structure/read-only/coherence/public320/0/diff PASS.

НЕ входит в604, как и local token guard/committed cleanup test. Published source
46576c73cf029b4ab0fc2d97ebbac07d15e91fa3 / merge9e2c214a22d11ee1efded8ec2832e62b0bb7e2dd
не менялся. Run39717548727 Windows119623188818 in_progress, Ubuntu119623188527
SUCCESS. I58–80, verified0 exact evaluated603, прирост0. Далее actual604 final;
после него checking законченных независимых блоков и coherent constructor/UI
ownership. Сначала service single load/epoch/cancel, затем actual UI gating/
bounded shutdown; persistence отдельно, не простой Task.Run вокруг UseToken.

## Independent committed repair cleanup regression — local, not604

Source83bcc8a4bdb336bd47d6c42f473b98c5adbfe9f3 +
c39659f918db611ec06560bfc166ee4188df7e94 добавляет только test.
До actual repair старый installed VERSION отличается от подготовленного.
Свой Windows FileShare.Read на old VERSION snapshot разрешает чтение/самотест,
но запрещает cleanup deletion после durable committed. Проверяются failed
cleanup exit/no completed-success claim, committed+selfchecktrue journal,
неизменность whole pending tree при отказе повторного обычного repair.

После release своего handle explicit -Recover обязан только убрать остатки и
сохранить exact accepted candidate/private snapshot. Во время recovery fixture
держит настоящий application lifetime mutex: случайный self-check не смог бы
вернуть success; residue-only cleanup не требует повторного app startup или
stopped-app gate. Все свои Process/stream/mutex/identity handles bounded/finally
освобождаются, другие процессы/окна не завершаются. New native NOT_RUN, source/
ASCII/public320/0/diff PASS,0 баллов. Runtime code не менялся.

Не входит в604: его source46576/merge9e2c остаётся неизменным. Ubuntu119623188527
SUCCESS, Windows119623188818 всё ещё full repository step in_progress; точные
native604 результаты пока не получены. I58–80, verified0 exact evaluated603,
прирост0. Local import guard также excluded604. После604 final результат — один
checking source для этих небольших завершённых блоков, не отменять текущий run.

## CI604 — fresh self-check/native canonical fixture опубликован

Source46576c73cf029b4ab0fc2d97ebbac07d15e91fa3, merge candidate9e2c214a22d11ee1efded8ec2832e62b0bb7e2dd,
full treec5e54b1beef1cc37a942c5af6884e3181d284949 = locala8599c761280f332e9697b87113b48672070c4da.
[CI604/39717548727](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39717548727)
in_progress. Expected8da→46576 lease/actual PR88 head+merge подтверждены, body
updated.3files72 additions secret/PII0/public320/0/Core+helper syntax/diff/tree
PASS; workflow/VERSION unchanged, public uploads OFF. Новые Windows checks
NOT_RUN pending exact log. Root R09 guard a75df7b/7cc8159 исключён из source604.

Следующий шаг — этот же604, actual known-folder preservation/native path check,
затем mandatory timeout/hard-kill/recovery и поздние native/ZIP. При отказе только
изолированный cause fix. Не повторять push/CI и не отменять текущий run ради docs.
I58–80, verified0 exact evaluated603, прирост0. Historical59822 остаётся его
source37f02/merge16bf1/ZIP. Binary/release/main/owner PC/VPS действий не было.

## Pending fresh self-check/native canonical fixture source

Object46576c73cf029b4ab0fc2d97ebbac07d15e91fa3 parent8da78a6bc6c72709b9f21d43a041c65c66ec61a5;
treec5e54b1beef1cc37a942c5af6884e3181d284949 = locala8599c761280f332e9697b87113b48672070c4da.
3files72 additions secret/PII0/public320/0/Core+embedded C#syntax/diff/tree PASS;
workflow/VERSION unchanged, uploads OFF. Ref ещё NOT_RUN здесь. После обрыва
actual head/run проверить прежде повтора.603 completed; следующий run не отменит
его. New fresh Root/real known-folder preservation/native final-path checks
NOT_RUN. Root R09 guard excluded; I58–80, verified0 exact603, прирост0.

## CI603 FAILURE на следующем fixture; null/copy/rollback scoped PASS

Source8da78a6bc6c72709b9f21d43a041c65c66ec61a5 / merge4a0bac2b788708a51e5051e4802d0d7fa6698eb6,
[run39716559040](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39716559040):
Windows119619939733 FAILURE/Ubuntu119619939421 SUCCESS. PS5 parser/main compile
PASS. Native null-string bad-vs-good1 PASS подтвердил причину602: plain$null
даёт ArgumentException без изменений bytes, explicitNullString атомарно заменяет
без backup.4 preflight/41 manifest/full41 copy+actual installed self-check+
private fixture preservation1 и actual sharing failure rollback1 PASS.

В03:23:50.784 negative hanging-check fixture отказал на raw image-path comparison.
Alias short/long Windows path — hypothesis; значения не печатались. Local
native GetFinalPathNameByHandle compare e2c93f2648f5a8a328ac5762cedf00d9f32c28aa +
755fc3cea05c778606e150bf178df0b4e512d5c7 disposes failed/unadopted handles. Child
kill-on-close job и bounded cleanup остаются; новый native NOT_RUN.

Критичный source gap: старый LOCALAPPDATA-only override НЕ гарантировал isolated
AppPaths, который читает Windows known folder.603 private fixture PASS не
подтверждает неприкосновенность default Windows profile. Старые утверждения
изоляции в истории superseded этим выводом; на ПК владельца ничего не запускалось.
Locala8599c761280f332e9697b87113b48672070c4da/treec5e54b1beef1cc37a942c5af6884e3181d284949
задаёт fresh absolute PROGO_SELF_CHECK_ROOT только для3 прежних self-check args;
обычный app игнорирует env, readonly AppPaths API сохранён. Repair требует actual
созданный child data root. Native test дополнительно сравнит real Windows
known-folder tree existence/all file hashes до/после actual built-image check.

Все новые native NOT_RUN, source Core/helper syntax/contracts/ASCII/public320/0/
diff PASS.3 source paths. I58–80, verified0 exact603, прирост0: полного accepted
parent D/P нет, later recovery/native/ZIP/owner NOT_RUN. Historical59822 остаётся
только exact37f02/16bf1. Далее scanned isolated source и один новый CI, не rerun603;
local R09 token guard не включать. Binary/release/main/owner действий не было.

## Pending explicit-null fix publication

Object8da78a6bc6c72709b9f21d43a041c65c66ec61a5 parent5ecf7738e3f77832737caad3a43bccda34366e6a;
treee3365f1d14d49ea825b98c089686d54915a1789d = local87bcec48a5582a10add3045221525e0cacb4b5fb.
2files20 additions secret/PII0/public320/0/diff/tree PASS, workflow/VERSION unchanged,
upload guards4/4 intact. Ref ещё NOT_RUN здесь; после обрыва actual PR head/run
проверять перед повтором. Native interop/positive repair/остальные критерии NOT_RUN.
R09 local guard excluded. I58–80, verified0 exact602, прирост0.

## CI602 FAILURE локализован: journal File.Replace backup-string boundary

Source5ecf7738e3f77832737caad3a43bccda34366e6a / mergef232bfd91cd25d475a38b917cf9398d8f5128cd7,
run39716108725 Windows119618489243 FAILURE/Ubuntu119618489590 SUCCESS. Parser/
main compile/4 preflight/41-manifest PASS. Safe record03:18:59.814:
ArgumentException; stage=snapshot; script_line=121; hresult=-2147024809;
native=0; check_exit=-1. Подтверждён отказ File.Replace(temp,journal,$null)
при preparing→prepared, ДО runtime admission/self-check. Initial599 не имел
такого direct record; причинная связь того запуска восстанавливается по той же
неизменённой операции, не по вымышленному логированию599.

Local87bcec48a5582a10add3045221525e0cacb4b5fb/treee3365f1d14d49ea825b98c089686d54915a1789d:
оба journal/runtime File.Replace используют явный NullString.Value. Microsoft
описывает этот type как передачу настоящего null строковому .NET parameter;
API File.Replace требует null, когда backup не нужен. Преобразование plain$null
в пустую строку — пока inference из точного отказа/контракта, его отдельно
подтвердит native PS5 vector: прежний вызов обязан дать ArgumentException без
изменения bytes, правильный — заменить owned target/consume temp без extra file.
Primary URLs сохранены в JSON. Никакого восстановления через отключение проверки.

New native NOT_RUN, source/public320/0/diff PASS.41-file guard/таймауты/workflows
не менялись. Positive actual repair/rollback/hard-kill/recovery и поздний полный
native всё ещё обязательны. I58–80, verified0 exact602, прирост0. Далее isolated
null-boundary fix publication и один exact run; R09 guard не включать.

## CI602 — raw safe evidence опубликован, один exact run

Source5ecf7738e3f77832737caad3a43bccda34366e6a, merge candidatef232bfd91cd25d475a38b917cf9398d8f5128cd7,
tree01509d78d50912f975e6fada81202f5ccfc50e4d = localfa7c7ee4545c7d8230b1ea30e7e0188f18f2644f.
[CI602/39716108725](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39716108725)
queued/in_progress. Expected d239→5ecf lease/actual PR88 head/merge подтверждены,
body updated.2files10 additions secret/PII0/public320/0/diff/tree PASS;
workflow/VERSION unchanged, public uploads OFF. New Windows checks NOT_RUN.

Следующий шаг — тот же602 exact logs/raw fixed marker или numeric source line.
Сначала факты, потом изолированный cause fix; исходная599 причина всё ещё UNKNOWN.
Не повторять CI или head update ради docs. Root R09 guard a75df7b/7cc8159 excluded.
I58–80, verified0 exact evaluated601, прирост0; historical59822 сохранён отдельно.

## Pending raw repair evidence source

Object5ecf7738e3f77832737caad3a43bccda34366e6a parentd239bee8c69519086ddcfe611a637a7627a7ffb7;
tree01509d78d50912f975e6fada81202f5ccfc50e4d = localfa7c7ee4545c7d8230b1ea30e7e0188f18f2644f.
2files10 additions0 secrets/PII/public320/0/diff/tree PASS; workflow/VERSION unchanged,
uploads OFF. Ref ещё NOT_RUN здесь. После обрыва actual head/run проверить раньше
повтора. Цель нового source — raw safe phase/type/numeric marker/line, не rerun601,
не утверждение исправленной причины599. I58–80, verified0 exact601, прирост0.

## CI601 FAILURE: manifest41 прошёл, positive child exit1 без classified cause

Source d239bee8c69519086ddcfe611a637a7627a7ffb7 / mergea9600c5511e79f262e688da3d80284e58692119e,
run39715671880 Windows119617063751 FAILURE/Ubuntu119617063695 SUCCESS. PS parser/
main compilation/4 native preflight refusals PASS. Canonical41-runtime guard
теперь PASSED (достигнут следующий positive child); в03:14:02.040 child_exit1,
no classified repair failure. Исходная причина599 всё ещё UNKNOWN; последующие
native/ZIP NOT_RUN. Ошибка форматирования stderr — гипотеза, не подтверждённая
причина самого repair. Не повторять идентичный601.

Localfa7c7ee4545c7d8230b1ea30e7e0188f18f2644f/tree01509d78d50912f975e6fada81202f5ccfc50e4d:
Console.Error.WriteLine fixed PROGO_REPAIR_FAILURE record ДО rollback: type,
phase, script line, HResult/native/check exit только whitelist/numbers. Parent
также извлекает только numeric stderr source line при ошибке вне handler. Никаких
raw exception messages/путей/данных. Source/public/diff PASS, native NOT_RUN;
не ослаблены41 guard/таймауты/реальные проверки. После этого нужны actual codes,
а не очередное предположение о причине. Root R09 guard excluded.

I58–80, verified0 exact601, прирост0. Historical59822 остаётся exact37f02/16bf1.
Далее2-file safe marker checking source, expected d239 head и один новый CI.

## CI601 — canonical test fixture опубликован, один run

Sourced239bee8c69519086ddcfe611a637a7627a7ffb7, merge candidatea9600c5511e79f262e688da3d80284e58692119e,
full treef06412841ab9607333a40b8cf674ba8683cb2732 = local7299b84f647e6176f48638550a8fe64131cb8cb9.
[CI601/39715671880](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39715671880)
queued/in_progress. Actual PR88 head/merge и expected973→d239 lease подтверждены,
body обновлён.1file5 additions secret/PII0/public320/0/diff/tree PASS; unchanged
workflow/VERSION, uploads OFF. Новые Windows compilation/native NOT_RUN.

Далее этот же601: получить actual runtime_count либо sanitized repair phase/
codes. Прежний600 не дал выполнить positive child; первопричина599 всё ещё
UNKNOWN. Не повторять CI для того же SHA и не менять head ради checkpoint docs.
Local R09 guard a75df7b/7cc8159 исключён из601. I58–80, verified0 exact evaluated600,
новые баллы0; usable historical59822 сохранён только на его исходниках/пакете.
Binary/private delivery/owner setup/target acceptance BLOCKED/NOT_RUN, не PASS.

## Pending canonical fixture publication

Objectd239bee8c69519086ddcfe611a637a7627a7ffb7 / treef06412841ab9607333a40b8cf674ba8683cb2732
равен local7299b84f647e6176f48638550a8fe64131cb8cb9. Parent/expected973fb0a40cee9cbfeb745904e9204be9b418cc9b.
1file5 additions0 secrets/PII/public320/0/diff/tree PASS. Workflow/VERSION unchanged,
uploads OFF; exact41 criterion retained. Ref в этот момент NOT_RUN; after
interruption проверить actual head и associated run, не повторять публикацию.
Новое source изменение только test canonical path/count evidence; original599
repair cause остаётся UNKNOWN. I58–80, verified0 exact600, прирост0.

## CI600 FAILURE раньше positive child; первоначальная причина599 UNKNOWN

Source973fb0a40cee9cbfeb745904e9204be9b418cc9b / merge22a413abcf33d62e81d923ad258201728c69a4be,
[run39715222513](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39715222513):
Windows119615596947 FAILURE, Ubuntu119615596679 SUCCESS. PS5.1 parser/main
compilation PASS,4 missing/empty native payload refusals PASS. В03:08:59.248
новый test41-runtime count guard FAIL на98 строке ДО positive child. Безопасная
repair classification пока НЕ выполнена, причина599 всё ещё UNKNOWN. Не
объявлять исправленную сверку доказательством исправленного repair.

Local7299b84f647e6176f48638550a8fe64131cb8cb9/treef06412841ab9607333a40b8cf674ba8683cb2732
меняет только test fixture: canonical payload root, root-directory ownership+
readme/license names вместо slicing ненормализованной строки пути; runtime_count
в безопасном числовом error, exact41 guard остаётся. Это candidate, native NOT_RUN.
Source public320/0/diff PASS; no test disable/deadline increase. Один новый SHA
для новых данных, не identical rerun600. Root R09 guard по-прежнему исключён.

I58–80, verified0 exact600, изменение0. Historical59822/raw21.75 сохранён с его
SHA/ZIP; new package/поздние native/owner NOT_RUN. Далее проверить actual973 head,
один targeted delta source CI, по actual count/phase исправить подтверждённую
причину. Публикация binary/release/установка/реальные VPS не выполнялись.

## Pending safe-failure source перед ref update

GitHub object973fb0a40cee9cbfeb745904e9204be9b418cc9b parent0942445dd268f836c5001ec2bf82d27bafc6f3a5;
full tree1fbf063b4d684240ccb431f24f80d1289335e474 = local85771b3d764c7b36c488ea178b52e49dafa566c1.
2 files27 additions0 secrets/PII/public320/0/diff/tree PASS; workflow/VERSION
unchanged, uploads OFF. Ref ещё NOT_RUN в этот момент. После обрыва проверить
actual PR head, затем единственный associated CI. Новая проверка нужна для
safe child failure phase/codes, которые599 скрывал. Это не identical rerun и не
заявление об исправленной причине599. Local R09 guard excluded, Windows NOT_RUN.
I58–80, verified0 exact599; historical59822 сохранён. Затем exact CI/log cause.

## CI599 FAILURE — positive repair отказал, root cause пока UNKNOWN

Source0942445dd268f836c5001ec2bf82d27bafc6f3a5 / mergea5d6f7c9afcd6d671b6607447fff7648b109ee45,
[run39714516345](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39714516345):
Windows119613322368 FAILURE, Ubuntu119613322153 SUCCESS. Windows PS5.1 parser
preflight и app C#5 compilation PASS; Build identity совпал. Четыре реальных
incomplete/empty payload refusals PASS. В03:01:18.866 positive complete repair
FAIL; fixture скрыл child exit/phase/cause общим сообщением. Причина пока
UNKNOWN. Последующие runtime rollback/self-check/hard-kill/recovery/native suites
и пакет NOT_RUN, uploads SKIPPED. Не повторять идентичный599.

Изолированный470e33c6027e31a01754de4bb458d0ae9a229994 добавляет только безопасную
диагностику: фиксированный stage, root exception type, HResult/native/check exit
numbers; exception messages/private paths не печатаются.85771b3d764c7b36c488ea178b52e49dafa566c1
отдельно исправляет source-confirmed latent test mismatch: Build содержит41
runtime+3 root readme/license; repair test сверяет41 runtime, без пропуска иных
built leaves.599 упал ДО этой сверки — mismatch не объявляется его причиной.
Новые native NOT_RUN, таймауты/проверки не ослаблялись,0 баллов за диагностику.

V1.0 current exact599 verified0: четыре узких preflight guards не подтверждают
parent D при отказе основного repair; поздний Desktop/native transport/package
не выполнялись. Историческая usable59822/raw21.75 сохранена только source37f02/
merge16bf1/его ZIP. Это снижение evidence для нового SHA, не объявление всех
функций FAIL. I58–80 unchanged. Далее один новый exact instrumentation source,
не rerun599; root token guard не включать. Ниже прежний in_progress — история.

## CI599 — repair опубликован, один exact run

Source `0942445dd268f836c5001ec2bf82d27bafc6f3a5`, merge candidate `a5d6f7c9afcd6d671b6607447fff7648b109ee45`,
full tree `4297436ce6a3ad896783a9ea45e070c593aa0eac` = isolated local
`3f545dd2190b23344e200c56b0427ff8cc70e00f`. Expected-head CAS37f02→0942445 без
force подтверждён независимым PR read. [PR88](https://github.com/rkhnorkhan-bit/ProGo/pull/88)
OPEN/DRAFT, body updated. [CI599/39714516345](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39714516345)
in_progress, new repair parser/compiler/native NOT_RUN. Full41 preflight/copy/
rollback/self-check/journal/recovery/native failure vectors теперь входят в этот
source. Local token guard a75df7b/7cc8159 НЕ входит; он остаётся сохранён локально.

7 files2669 additions: secret/PII0/public320/0/diff/C#syntax/target C#5 structure/
full tree PASS; workflows/VERSION unchanged, uploads OFF. Новая публикация не
отменила598 — тот завершён SUCCESS, его score22/raw21.75/package только exact
source37f02/merge16bf1. I58–80, прирост за новый source0. Run599 не дублировался.

После обрыва сначала этот же run ID и actual PR head; не повторять push/CI. Не
менять head ради checkpoint docs, пока599 active. Далее независимый R09 worker
import с cancellation/ownership; если599 упадёт — изолированный exact fix без
смешивания незавершённого R09. Binary/private delivery/owner PC/installer blocked.

## Перед repair publication — pending object, ref ещё NOT_RUN

GitHub object0942445dd268f836c5001ec2bf82d27bafc6f3a5 создан с parent
37f02d2a0648fe2efebd7fc932b81668d26c44be; full tree4297436ce6a3ad896783a9ea45e070c593aa0eac
точно равен isolated local3f545dd2190b23344e200c56b0427ff8cc70e00f.
7 files:4 repair source/test +3 current evidence docs. Secret/PII0/public320/0/
diff/tree PASS; workflows/VERSION unchanged, public uploads OFF. New Windows
compilation/native NOT_RUN. Local token guarda75df7b/7cc8159 исключён.

После обрыва сначала actual PR head: ref мог быть обновлён после этого checkpoint.
Плановый CAS expected37f02→0942445 без force, затем сохранить actual merge/run ID;
не создавать duplicate run. CI598 завершён SUCCESS и не будет отменён новым head.
I58–80, verified22 exact598,0 баллов за candidate/создание object. Публичные binary/
installer/release/main merge/owner PC/VPS действия не выполняются.

## CI598 завершён SUCCESS — exact source37f02 / merge16bf1

[CI598/39711511948](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39711511948),
Windows119603692452 и Ubuntu119603692348 SUCCESS. Build identity
`0.2.2+16bf1e50b66baf1a724a0d2da642b2fe70bb3a46` совпадает. Source `37f02d2a0648fe2efebd7fc932b81668d26c44be`;
matching local85f33/treee6f. Точный native regression full StopAll PASS в
02:40:45.557 UTC: старый queued observer не поглощает новую physical stop operation.
13 новых phone cases PASS:5 Context,3 service preparation/cleanup,3 wizard,
2 relay lifecycle. Actual relay flow drain/EOF/ports tests PASS.

Regression: recovery51/456; bounded43/report32/helper2/modal6/chooser3/
diagnosticContext3/worker4/route6; signing55/UI50/prep72; Desktop5274/Instance28/
Maintenance126/Shutdown47/HomeVPN1022/Portable90. Actual direct и owned-agent
prepare→native OpenSSH→SOCKS→owned HTTP→child crash→fresh recovery PASS. Native
startup direct1966/prep37ms, recovery1957/prep37,42; agent2064/prep139 и
recovery2048/prep139,128. Это CI hardware, owner HDD/whole-app I/O NOT_RUN.
Поздний Python28=26PASS2SKIP; SKIP не принят за native PASS.

Runner ZIP0.2.2 SHA256 `576f6034b4768e148a0533ada7596136ef06950dd01ee8d5a19476fcac8d932b`.
Все uploads SKIPPED: bytes не получены/не доставлены/не установлены. Private
передача BLOCKED до согласования канала владельцем. Нельзя ссылаться на этот ZIP
как на доступный installer. Release/main merge/owner PC/VPS действий не было.

V1.0:28 parents weight87 с timestamp anchors в JSON; D0.5/P0.5/A0 дают21.75→22.
Рост raw+11.25 к partial59710.50 подтверждён полными native/runner package,
не13 написанными tests. I58–80 без изменения. F19/F27/F30/target acceptance0;
full F31-R10/R11 остаются PARTIAL. Локальные repair144c616…fa48f29 и token guard
a75df7b/7cc8159 НЕ входят в598, их Windows NOT_RUN/баллы0.

Далее — isolated repair checking sourceb342 плюс эта evidence документация,
проверка exact delta secrets/PII/workflow guards/tree и expected-head publication.
Один новый run, не rerun598. Независимый import guard остаётся local отдельно.
Ниже checkpoints сохранены как история; их in_progress/NOT_RUN отражали прежний
момент, актуальный итог598 находится в этом разделе и ci_history JSON.

## F31-R09 narrow import guard, local Windows NOT_RUN

Source a75df7b9c62ef1638019886821ecce4b45b76d62 и fixture cleanup followup
7cc8159629d2cc085c575fdd65687f136bceedb7. Подтверждён source defect: прежний
UseToken вызывал void Stop, терял false actual cleanup и затем сохранял новый
доступ поверх неочищенного session. Теперь требуется true physical StopAsync
result ДО первого Save/Access/Owner replacement, иначе явный отказ с retry.

В existing native FileShare.None cleanup fixture добавлен import другого owner:
прежние Access/Owner references и access/owner DPAPI bytes должны остаться точно
исходными, session ownership удерживается, новый relay/PID не появляется. Fixture
держит Task импорта и после выхода из lock scope ждёт его в bounded finally до
восстановления private test data. Source order/syntax/C#5 structure/public320/0/
diff PASS. Windows compilation/native NOT_RUN, код только local и НЕ входит ни
в598, ни в repair checkingb342. Синхронный UI import и multi-file persistence/
owner cancellation всё ещё F31-R09 FAIL; полной готовности не объявляем.

CI598 native relay step SUCCESS, public content step in_progress; итог/ZIP ещё
не подтверждены. I58–80, verified11 exact597, прирост0. Далее final598 logs,
потом отдельный repair checking publication; local import guard сохраняется.

## Repair checking candidate готов локально, публикация ещё NOT_RUN

Isolated local `b342dfc4c055de5b4e8e7d1733fdb39b0e65ada4`, tree
`2b9640811e30ccfc5e79c26e767b50830aa5712c`, worktree ProGo-ci598-repair.
На published matching85f33 включены только4 пути: Repair-ProGo.ps1,
Test-ProGo.ps1, MaintenanceOperation.cs и новый RepairPayloadTests.ps1.
4 files727 additions: secret/PII scan0; public320/0, modern C#syntax/targeted
C#5 structure/diff PASS. Workflow/VERSION unchanged, upload guards4/4 intact.

CI598 источник не меняется; repository build step уже SUCCESS, отдельный native
relay step in_progress. Новые compilation/native repair NOT_RUN. После final
jobs/log проверяется actual PR source37f02, затем candidate допускает одну новую
CI публикацию с full-tree equality. Сборка/доставка бинарей, main merge/release
не разрешались и не выполнялись. I58–80, verified11 exact597; баллы за подготовку0.

## F22-R03 этап7 — recovery отказ/cleanup, local fa48f29d1fb1b74563b83e9b1b58988499bfe4ed

Review выявил возможность пропустить Dispose lease/maintenance при ошибке
удаления своего temporary file. Nested finally теперь освобождает оба owner
независимо от этого отказа; snapshot/journal не объявляются завершёнными.
Non-object journal и занятый обычным файлом stage path имеют явный отказ.
Дополнены native vectors: string вместо bool, неправильный admission prefix,
несогласованный phase, повреждённый old snapshot. Все9 refusal scenarios должны
оставлять exact baseline установки/journal/snapshot bytes; реальное выполнение
Windows пока NOT_RUN. Source cleanup/validation contracts/public320/0/diff PASS.

Новые исходники остаются local, CI598 проверяет только source37f02/merge16bf1.
I58–80, verified11(raw10.50 exact597), прирост0. Следующий этап — isolated repair
checking tree, секреты/PII/diff/workflow guards, публикация только после завершения
того же598. Full F22-R03/приёмка владельца остаются открытыми.

## F22-R03 этап6 — explicit recovery, local source 017553607c1eac01f9eb382bb904c2f0a5a0818e

Отдельный `Repair-ProGo.ps1 -Recover` восстанавливает только известные admitted
runtime paths по проверенным snapshots. Schema1/32-hex GUID/41 canonical paths,
порядок prefix admission, boolean/hash types, bounded64KiB journal и hash тех же
прочитанных bytes проверяются до runtime writes. Lease защищает от второго owner;
redirected stage/runtime paths запрещены, tree проверяется до descent. Foreign
runtime или повреждённый old snapshot сохраняют journal/stage, не перезаписываются.

Applying recovery использует hash-verified staged Maintenance helper и общий
maintenance gate/application-stopped/proxy-cleanup checks. Original payload ему
не нужен. Committed outcome только очищается, без rollback/self-check/launch;
rolled-back cleanup допускает повторное завершение. Temporary atomic write files
теперь внутри owned stage — killed writer не оставляет их рядом с private data.

Добавлен Windows regression: настоящий repair parent принудительно завершается
на barrier двух собственных native check processes; оба должны закончиться через
kill-on-close job. Pending default repair/schema2/traversal/foreign runtime/repeat
recovery дают явный отказ, exact installation snapshots неизменны. Затем payload
удаляется и explicit recovery должен вернуть все исходные runtime/private bytes.
Все child waits ограничены, cleanup касается только observed own process handles.
Source contracts/public320/0/diff PASS; PS parser/C#5 shared helper/native repair
NOT_RUN. Это не Windows PASS и не проверка power-loss durability/целевого ПК.

Рабочее дерево после source commit чистое до этой checkpoint документации.
CI598/39711511948 всё ещё Windows in_progress, Ubuntu SUCCESS; published source
37f02d2a0648fe2efebd7fc932b81668d26c44be/merge16bf1e50b66baf1a724a0d2da642b2fe70bb3a46
не менялся. Jobs Windows119603692452/Ubuntu119603692348; stale older job/pending
fields в active JSON перенесены в historical checkpoint fields. Repair не входит
в598. Следующий этап: review phase/conflict interruption boundaries, затем exact
repair Windows candidate после завершения598. F22-R03 всё ещё открыт.
I58–80, verified11/raw10.50 exact597, прирост0. Публикации/нового CI не было.

## F22-R03 этап5 — durable admission journal, только local

Repair пишет relative schema1/GUID journal: preparing, проверенный prepared,
applying ДО каждой atomic OS replacement, committed только после actual
self-check/повторных hashes, rolled-back перед cleanup. Новый record сначала
Flush(true) в собственном temp, затем atomic move/replace; чужой transaction_id
не перезаписывается. Pending journal запрещает новый repair до payload
validation, установка не меняется повторно. В record нет secret/private values
или прежних Windows absolute paths, только runtime names/hashes/состояния.

При неподтверждённом durable cleanup snapshot/journal сохраняются, restored
bytes не объявляются готовым приложением. Существующий negative native check
наблюдает41 entries/41 admitted paths/phaseapplying/selfcheckfalse во время
своих живых root+child; после обычного отказа ожидается точный rollback без
journal residue. Source Flush/admission/order/guard/public320/0/diff PASS;
PS parser/native NOT_RUN, не входит в598. Hard-kill/recovery ещё не выполнен.

F22-R03 остаётся FAIL/partial local: явный recovery command со строгим schema/
path/hash ownership ещё не реализован. Следующий этап — recovery и настоящий
test termination родителя с его own job, затем malformed/foreign journal refusal
и exact Windows. Recovery не будет повторять self-check/запуск автоматически.
I58–80, verified11(exact597),0 баллов за написанный journal. CI598 в работе;
новый head или дублирующий CI не создавались.

## CI598 — exact physical StopAll fix опубликован, один run

Source `37f02d2a0648fe2efebd7fc932b81668d26c44be`, merge candidate
`16bf1e50b66baf1a724a0d2da642b2fe70bb3a46`, tree
`e6f261e2bdadc227eac3394dc83dc43e43294e05` = local
`85f33fdcae96df1231cfeb91af692eb7ad40f97e`.
[CI598/39711511948](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39711511948)
in_progress, new compilation/native NOT_RUN. Scan2files76additions0 secrets/
PII/public319/0/syntax/C#5structure/diff/full tree PASS. Expected-head lease
подтверждён, PR88 OPEN/DRAFT updated; workflows/VERSION unchanged, uploads OFF.
Repair stages144c616/5157f78/46fdd45/9d88165 excluded, их Windows NOT_RUN.

I58–80, verified11/raw10.50 принадлежит exact597 partial native, не598. Ни5
новых Context cases, ни написанный regression не дают отдельных parent баллов.
Public release/binary upload/main merge/owner PC/VPS/system settings не делались.
После обрыва проверить этот же598/actual PR head, не повторять push/CI. Далее
independent durable repair journal (до каждой atomic runtime записи), затем
восстановление/interrupt regression отдельным local этапом. Blocker full StopAll
остаётся до exact Windows; full F31-R10/R11 не закрыт.

## CI597 scoped native PASS, full StopAll FAIL — exact identity fix pending

Source `b1fad3c6a6804ab6055b146b5cf284f5136d4288`, tested merge
`699b50ee7db44cc6ec851d7fcc36069fa574611a`, run39708048332 FAILURE;
Windows119592612485/Ubuntu119592612681 FAILURE/SUCCESS. Actual build identity
matched. C#5 fix и main/Core/Desktop compilation PASS; later Home harness
compilation/native NOT_RUN. Native51/456/bounded43/report32/helper2/modal6/
chooser3/diagnosticContext3/logger+worker4/routes6 и5 новых phone Context PASS.
Settings budget phone shutdown действительно3s, не250ms:250ms был только
backup budget fixture (прежнее описание уточнено). Native log02:03:28→31.

В02:04:09 CheckStopScopes full-stop assertion FAIL. Подтверждённый user defect:
StopPhoneAsync объединял запросы по незавершённому UI observation, хотя его
физический Stop уже завершился и channel был заново запущен. StopAll возвращал
прежний task и не запускал остановку нового channel. Новый isolated candidate
сначала резервирует physical Home.StopAsync и объединяет UI observers только
для того же самого Task; stale callback не стирает новое состояние.

Isolated local85f33fdcae96df1231cfeb91af692eb7ad40f97e (первый local93ec714),
treee6f261e2bdadc227eac3394dc83dc43e43294e05 = GitHub object
37f02d2a0648fe2efebd7fc932b81668d26c44be. Earlier object2c7fee создан, но ref
не обновлялся; superseded only. Regression явно держит очередь UI: physical
cleanup awaited без pump, свежий real loopback relay, новая stop identity,
confirmed stop обоих каналов/нет timer revival. Fixture peer/startup/cleanup
ограничены5s и освобождают только свои sockets/session, проверки не отключены.
Syntax/target C#5 structural/public319/0/diff/tree PASS;2files76additions
secret/PII0, workflows/VERSION unchanged. New Windows compilation/native NOT_RUN.
Следующий шаг expectedb1fad→37f02 после проверки head; не rerun597. All repair
blocks до9d88165 excluded, source импортируется без потери local repair work.

V1.0 exact59719 timestamp-traced parents weight70×D0.5×30%=raw10.50→11;
P/A0, later full Desktop/selected-key combined/Home/relay/ZIP NOT_RUN. +10.50 к
compile-only596 отражает выполненные native критерии, не written tests. Новые
5 Context cases не дают отдельных дополнительных parent баллов. Historical
59522 сохранён только для его source/merge/package. I58–80 unchanged;
полная готовность F31-R10/R11 не объявляется.

## F22-R03 этап4 — actual installed self-check before commit, local NOT_RUN

Repair участвует в общем Maintenance gate и требует остановленное приложение/
завершённую proxy cleanup до target writes. Actual installed EXE --self-check
выполняется до committed/success/launch. Ему выделен fresh LOCALAPPDATA внутри
owned stage: smoke-check не мигрирует и не переписывает настоящие profiles/
settings/vault. Это не приёмка реального профиля/ПК. После child exit повторно
сверяются все runtime hashes; отказ вызывает прежний conditional rollback.

Shared MaintenanceOperation получил opt-in RunOwnedSelfCheck: suspended child
помещён в kill-on-close Job до исполнения, root wait8s, tree cleanup3s. При
смерти PowerShell owner kernel закрывает только этот job. Default accepted
handoff/старые cleanup budgets не менялись. Fresh prepared helper API требуется;
old-loaded-API transition не принят. Старые missing/locked-copy tests10s
сохранены; новый self-check budget20s обоснован8+3+staging/PS startup, не retry
ради зелёного результата.

Positive case использует настоящий built ProGo. Negative native PE создаёт
своего child и зависает; PID marker публикуется атомарно, parent наблюдает
проверенные process handles, ожидает failed result, оба exited и полный byte
rollback. Это failure/cleanup fixture, не подмена functional application test.
C#syntax/public320/0/self-check-before-commit/diff PASS; target C#5 compiler,
PS parser/native NOT_RUN, не входят в597. F22-R03 остаётся открытым: durable
interruption journal и exact Windows regression ещё отсутствуют.

CI597 теперь completed FAILURE в существующем full Stop scope после5 новых
Context scoped PASS; exact log сохранён, причина разбирается отдельно. Этот
repair block фиксируется независимо до переключения на изолированный StopAll
fix. I58–80, verified0(last evaluated596), новых баллов за repair0;597 evidence
будет оценено только с exact native anchors, не по написанным тестам.

## F22-R03 этап3 — owned atomic runtime rollback, local NOT_RUN

Repair резервирует per-install exclusive file lease, до первой замены готовит
проверенные snapshots старых и новых41 leaves. Каждый file commit атомарный
через собственный verified temp; путь записан как applied до OS call. При
ошибке rollback меняет только байты, совпавшие с этим кандидатом, либо видит
неизменённый baseline. Чужое изменение/неподтверждённый путь не перезаписывается:
owned snapshot сохраняется и выдаётся явная ошибка incomplete rollback.
Target links не используются для перенаправления владения на внешние файлы.
Свои temp files/lease и пустые созданные directories очищаются; existing
private/unknown файлы вне runtime list. Literal .NET directory creation
сохраняет корректность путей с пробелами/brackets.

Native regression удерживает VERSION FileShare.Read (чтение snapshot разрешено,
atomic replacement запрещён) после первой замены ProGo.exe. Проверяет failed
exit, возврат всего исходного runtime/private hash tree и отсутствие residues;
child10s/output+cleanup3s, NoLaunch, только свои процессы/папки. Source atomic/
ownership/order/public320/0/diff PASS, PS parser/native NOT_RUN. Не входит в597.

F22-R03 всё ещё FAIL: self-check перед success/launch и durable interruption
journal ещё не реализованы. Snapshot при конфликте не считается восстановленной
установкой. Следующий этап self-check/failure regression, затем journal/recovery.
I58–80, current verified0(compile-failed596), прирост0; historical59522 сохранён.
CI597 в работе, новый head/дублирующий CI не запускались.

## CI597 — C#5 fix опубликован, один exact-SHA run

Source `b1fad3c6a6804ab6055b146b5cf284f5136d4288`, merge candidate
`699b50ee7db44cc6ec851d7fcc36069fa574611a`, tree
`ca320af4f5a6df1b556101a56c0b7e011eddc018` = local
`272c42697ba7f0928626de2a11bcef6d114d913a`.
[CI597/39708048332](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39708048332)
in_progress; new Windows compilation/native NOT_RUN. Expected-head lease
подтверждён, PR88 OPEN/DRAFT updated. One-file scan29 additions0 secrets/PII,
syntax/targeted C#5 structural/public319/0/diff/tree PASS; workflows/VERSION
unchanged, public uploads OFF. No native test disabled or deadline increased.

Repair144c616/5157f78 и последующие local blocks исключены. Source596 native
NOT_RUN не подменяется историческим595 SUCCESS22. Current verified0 относится
к compile-failed596, I58–80 unchanged,597 ещё0 новых баллов. После обрыва читать
этот run/head, не повторять push/CI. Далее independent repair runtime rollback,
затем self-check и interruption recovery отдельными этапами. Public release/
binary upload/main merge/целевой ПК/VPS/system settings не выполнялись.

## CI596 compilation FAIL — C#5 compatibility fix готов, ref PENDING

Source654551f80feb485c1ca1f7889797d0cf362c0cf4 / merge3f67193a277574e128a8b06cb1a4ac502d873440,
[CI596/39707142123](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39707142123)
FAILURE. Windows119589733115 CS1985 at Ikev2RelayService.cs:116, symbol
StartOwnedAsync: установленный Framework C#5 compiler запрещает await в catch.
Ubuntu119589732810 SUCCESS. Native13 phone cases/relay regression/ZIP NOT_RUN,
uploads SKIPPED. Современный syntax parser этого target limitation не ловил;
его PASS не выдавался за compilation. Подтверждённый source defect, не flake.

Isolated local272c42697ba7f0928626de2a11bcef6d114d913a / tree
ca320af4f5a6df1b556101a56c0b7e011eddc018 = GitHub object
b1fad3c6a6804ab6055b146b5cf284f5136d4288. Catch лишь фиксирует exception;
await cleanup после него остаётся внутри outer CTS lifetime/finally. Прежняя
классификация cancel/cleanup failure, все tests/deadlines сохранены. Syntax и
целевой structural scan C#5 catch/finally/lock await во всех11 C# files PASS;
это ещё не target compiler. Scan1file29additions0 secrets/PII/public319/0/
diff/full tree equality PASS; workflows/VERSION unchanged. Windows fix NOT_RUN.

До публикации проверить actualhead PR88; один expected654551→b1fad без force,
новый SHA CI. Не rerun596. Local repair144c616/5157f78 исключён, импорт fix в
root не стирает его. Текущая exact596 verification0/100(raw0): native/P/A не
достигнуты; это не FAIL всех функций. Historical59522/raw21.75 сохранён только
для5f342/a285. I58–80 unchanged, прирост за code fix0. Главный blocker — exact
Windows compilation и все новые phone/relay cases после target-compatible fix.

## F22-R03 этап2 — полный single-payload runtime copy, local NOT_RUN

Repair больше не смешивает script files из PSScriptRoot с payload и не
пропускает отсутствующие компоненты. Сначала фиксирует SHA256 всех41 runtime
leaves, затем копирует только эти известные пути, сверяет каждый destination
и повторно весь набор. Включены ранее пропускаемые Install helper,2 update CS
и9 home-vpn resources. Settings/vault/keys/profiles/backups/unknown files не
входят в copy list. Окончание называется runtime restored+verified, не Repair OK.

Положительный native child case берёт настоящий Build payload, NoLaunch,
сверяет каждый built file и прежние private bytes. Сохранились4 negative
preflight cases; child10s/output+cleanup3s, только свои процессы/папки.
Static single-source/hash-order/41 parity/public320/0/diff PASS; PS parser/
native NOT_RUN, не входят в596 или phone654551. Native test counts пока0 PASS.

Это ещё не готовый repair: rollback при отказе посреди копирования и self-check
перед success/launch отсутствуют. F22-R03 остаётся FAIL до следующих блоков и
exact Windows. Следующая отдельная микрозадача — transaction/self-check и
failure regression, затем interruption recovery. I58–80, verified22(exact595),
прирост0 за этот local source. Основной596 продолжает только phone/relay scope.

## CI596 — phone/relay checking source опубликован, один run

Source `654551f80feb485c1ca1f7889797d0cf362c0cf4`, merge candidate
`3f67193a277574e128a8b06cb1a4ac502d873440`, tree
`daf59cf31a82ad253a9914f9b17fae8b6bf53034` = local
`3d3301a5f95ddfcb5ad60af5555a6de7ae1025b1`.
[CI596/39707142123](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39707142123)
in_progress; новые phone/relay Windows compilation/native NOT_RUN. Scan exact
16paths1835additions0 secrets/PII/full tree equality PASS, workflows/VERSION
unchanged, uploads guards подтверждены предыдущим595 SKIPPED. Expected-head
без force успешно проверен, PR88 OPEN/DRAFT updated. Root repair144c616 НЕ
входит в checking source, его Windows parser/native NOT_RUN.

Score22/raw21.75 принадлежит exact595 source5f342/mergea285, не596. Будущий
результат596 не распространять на repair, который продолжается независимо.
При восстановлении проверить PRhead и тот же run596, повтор push/CI не делать.
Следующий шаг: exact596 logs; пока он идёт full repair copy/verification,
transaction/self-check отдельными local commits. Release/binary upload/main
merge/ПК владельца/VPS/system settings не выполнялись. I58–80, прирост за push0.

## CI595 completed SUCCESS — exact R08 и selected-key pipeline proof

Source `5f3420d72e5f8c8ac282af6ca7ac57db1d89534c`, tested merge
`a2851df351a8ef86c7965761821f64a66c2dd960`, local matching tree
`ddb95e7ed68f1cf1ad3f27130575f2a4c923df97`.
[CI595/39704288681](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39704288681):
Windows119580544561/Ubuntu119580545361 SUCCESS, identity0.2.2+merge matched.
Exact Windows log прочитан: bounded43/report32/helper2,6 modal Cancel→Retry,
3 chooser/3 diagnostic Context,4 open workers/6 actual viewer routes, actual
SafeLog worker+3 epoch assertions+2 owner shutdown/Dispose cases PASS. Missing
fixture epoch fix native producer/entry/heartbeat/redaction/file PASS, all
later suites completed. Full R08 не закрыт: PS updater cold/helper/viewer I/O,
old-loaded-API transition и owner slow hardware/DPI/Narrator ещё открыты.

Fresh selected-key evidence: signing55/UI50/aggregate72/recovery51/456 PASS;
настоящие direct и owned-agent local preparation→OpenSSH→SOCKS→owned HTTP→
автоматический recovery PASS. Isolated metrics direct startup1954/prep33ms,
recovery1947/prep33,34ms; agent startup2012/prep90ms, recovery2007/prep90,93ms.
Own SSH CPU31.25/15.625ms, RAM~11MB, whole app I/O/owner ПК/VPS NOT_RUN.
Desktop5240/Instance28/Maintenance126/Shutdown47/HomeVPN976/Portable90 PASS.
Prepared native SSH server contracts5 и later relay Python28 (26PASS/2SKIP)
реально выполнены; discovery fix не отключил SSH cases и не требует ambient
Paramiko. NOT_RUN/skipped не включены как PASS.

Runner ZIP0.2.2 SHA256
`f1ba5b17e02d4cc0a36b8265851748b1b2529c4dee228b4279cd5ba5c7e5b27b`.
Uploads SKIPPED, bytes не получены/не переданы/не установлены. Private delivery
BLOCKED до согласованного владельцем канала; binary upload/release запрещены.
V1.0 unchanged:28 parents weight87,D0.5/P0.5/A0 =>raw21.75→22. +11.25 raw к594
из реально выполненных дополнительных native критериев и runner ZIP, не из
числа коммитов. Все28 timestamp anchors в JSON. I58–80 unchanged.

Phone object654551/source3d3301a и repair144c616 НЕ входят в595, их Windows
compilation/native NOT_RUN. Следующий шаг — проверить PR88 actualhead и один
expected5f342→654551 без force, новый exact checking SHA/merge/run. Source scan
16paths1835additions0 secrets/PII/full tree PASS; workflows/VERSION unchanged.
Repair остаётся независимым local block, не добавлять его в phone tree.

## F22-R03 этап1 — repair payload preflight, только local

Repair принимает опциональный PayloadDirectory готового payload (default source
repo/release сохранён). До любого New-Item/копирования в installation проверяет
41 обязательный непустой file. Native exe/helpers/home-vpn отсутствующий или
пустой компонент даёт явный отказ с relative names, не «Repair OK».
Состав статически совпал с независимыми Build outputs:3 root+17PS+12CS+9Home.
Это presence/length preflight, не signature/whole repair proof.

4 новых negative Windows child cases берут настоящий built payload и удаляют
exe/InstalledUpdateTransport.cs/home-vpn profile_share.py либо обнуляют updater.
Только собственный LOCALAPPDATA fixture, hash каждого private fixture file и
отсутствие нового scripts dir после отказа; NoLaunch. Process10s/output3s,
finally kill только созданного child при timeout и cleanup streams3s; timeout
не превращается в PASS. Suite явно wired после Build. PS testASCII и main BOM
сохранены. Source41 parity/order/public320/0/diff PASS; PS parser/native NOT_RUN.
Не входят в running595 или prepared phone object654551.

F22-R03 остаётся FAIL: legacy copy ещё неполон/mixed-source, self-check и
transaction rollback отсутствуют. Следующий отдельный блок — полный prepared
runtime copy+verification; затем transaction/self-check. Никакой установки на
ПК владельца, launch/release/upload не выполнялось. Score11(exact594), I58–80,
прирост0. CI595 repository build уже SUCCESS по jobs, relay in_progress;
точный полный log/итог/ZIP ещё не проверены, не объявлять полный SUCCESS.

## Phone checking object подготовлен — публикация PENDING completion595

Fixed local `3d3301a5f95ddfcb5ad60af5555a6de7ae1025b1`, GitHub object
`654551f80feb485c1ca1f7889797d0cf362c0cf4`, одинаковый полный tree
`daf59cf31a82ad253a9914f9b17fae8b6bf53034`. 16 paths/1835 additions,
secret/PII findings0; public319/0, diff/JSON/syntax11/wiring PASS. Canonical
committed blobs прочитаны полностью, GitHub full tree equality подтверждён;
GitHub commit metadata отличается от local SHA, это не разные runtime bytes.
Workflows/VERSION unchanged, public binary uploads OFF. Новый code Windows
compilation/native NOT_RUN, не входит в595 и не получает его будущий SUCCESS.

Branch пока5f3420d7, единственный CI595/39704288681 in_progress. После обрыва
сначала проверить этот run и PR88 head. Только после установленного completion
и exact logs один expected-head5f342→654551 без force, новый actual merge/run.
Старый595 не перезапускать и не отменять head update заранее. Если595 FAIL,
его причина приоритетнее публикации phone: fix держать изолированно, object
не обновлять вслепую (parent lease проверяется). Исходники/commits сохранены,
ничего не удалено/не откатано. Score11 принадлежит594, прирост за object0.

## Phone/relay final checking review —11 C# syntax PASS, native NOT_RUN

Все5 logical blocks сохранены отдельными commits: service owner df104d5,
wizard f5b2236, Context89dfcf6, Run drain6b4ec6f, public relay a34b0db.
11 изменённых C# files parse0errors; wiring новых Desktop tests и явного
console StopAsync await проверен, PowerShell UTF8 BOM сохранён. Backend Python4
PASS,3 C# cases skipped=NOT_RUN; public319/0 и diff PASS. Это self-review,
independent agent review и Windows compilation/native NOT_RUN. В595 не входят.

Небольшой callback guard предотвращает obsolete Stop failure balloon, если
другая попытка service Stop уже стала текущей; actual old task сохраняет свой
результат, UI/новая попытка не перезаписываются. ОстаткиR09 и аварийная/целевая
приёмка не скрыты. Следующий фиксированный checking tree включает только
phone/relay source/tests и checkpoints, после secret/PII/workflow scan. Ref
обновлять только после установленного completion595; нового CI на старом SHA
не создавать. Score11 относится к completed594, новые изменения дают0.
Реализация58–80; полная готовность R10/R11 не объявляется.

## R10 relay этап5 — public async startup/Stop/Dispose, local NOT_RUN

Start и Stop публикуют свои task до native worker. Дубликат реального pending
Start получает тот же task. Stop сразу закрывает admission, вне metadata gate
отменяет и ждёт startup probe, затем все retiring Run tasks; новый Start не
обходит pending/faulted retire. UDP/timer setup выполняется worker вне service
metadata gate, stale startup не публикуется после Stop/Dispose reservation.
Dispose idempotent, сохраняет StopWork и не ждёт UI. Home cleanup ждёт этот
настоящий relay lifetime на своём worker. Console harness явно ждёт StopAsync
перед exit: process exit не выдаётся за успешно завершённую очистку.

2 actual native Windows Forms cases держат настоящий loopback SOCKS probe:
Stop и Dispose, EOF своего peer до результата, один start/stop owner, новый
Start refused до settlement, heartbeat, оба UDP ports свободны. Barrier/peer/
cleanup waits3/4/5s; finally release, свои listener/client/task закрыты, ни один
чужой process/socket не затрагивается. C#syntax3/public319/0/diff PASS,
Python backend4 PASS, native harness3 skipped=NOT_RUN. New Windows compilation/
native NOT_RUN, не относится к595. Пять local phone/relay этапов не опубликованы.

Остаток: один exact-SHA checking run всех13 новых service/wizard/Context/probe
cases и усиленного real UDP/SOCKS flow drain; token import всё ещё sync Stop/
DPAPI на UI (R09). Forced process kill/restart recovery и целевой ПК/реальный
телефон NOT_RUN: retained managed task живёт лишь пока жив процесс; полная
аварийная приёмка не выдумывается. F31-R10 PARTIAL. Реализация58–80,
verified11(exact594), прирост0 за исходники и невыполненные native tests.
Далее финальный review/scan checking tree и тот же CI595 result.

## R10 relay этап4 — Run receive/flow tasks отслеживаются, local NOT_RUN

Run сохраняет оба receiver task и все незавершённые flow workers. Один stop
reservation ставится до worker, закрывает свои сокеты/flows и ждёт Task.WhenAll
прежде чем завершить результат. Self-stopping Run регистрируется в retiring
list до удаления current: concurrent cleanup больше не может потерять его.
Успешные завершения удаляются, неожиданный fault не превращается в success.
Serialized compatibility Stop ждёт этот drain на Home cleanup worker/console.

Существующий real Windows frontend test усилен без новых моков:4 потока по
обоим UDP ports через own SOCKS, STOPPED0 acknowledgment после task settlement,
EOF всех своих TCP handlers и rebind обоих UDP ports. Fixture teardown теперь
имеет bounded waits и finally убивает только свой subprocess при ошибке/
timeout, закрывает свои servers/tasks; timeout остаётся FAIL. Backend Python4
PASS,3 C# harness cases skipped=NOT_RUN. C#syntax2/public319/0/diff PASS,
Windows compilation/native NOT_RUN, в595 не входит.

Это отдельный private Run этап: public relay startup/StopAsync admission и
Dispose ещё требуют следующей микрозадачи. Полный R10 не закрыт; UseToken
UI Stop/DPAPI остаётся R09. Source не опубликован. Реализация58–80,
verified11(exact594),0 новых баллов. Далее public relay lifetime reservation,
затем checking tree после completion595.

## CI595 — isolated SafeLog fixture fix опубликован, один run

Source `5f3420d72e5f8c8ac282af6ca7ac57db1d89534c`, merge candidate
`a2851df351a8ef86c7965761821f64a66c2dd960`, tree
`ddb95e7ed68f1cf1ad3f27130575f2a4c923df97` = local
`a70943991c96907afd829f715bf4fa75a9ee6552`.
[CI595/39704288681](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39704288681)
in_progress. One-file fix9 additions, secrets/PII0, source syntax/public318/0/
diff/tree PASS, new Windows compilation/native NOT_RUN. Expected-head lease
подтверждён; PR88 OPEN/DRAFT обновлён. Phone stages остаются только local и в595
НЕ входят. Full CI594 не был SUCCESS, его узкие PASS относятся только к6f615/
5cf4. Score11/raw10.50 — completed594, не новая проверка595; I58–80 unchanged.
Никаких public binaries/release/main merge. После обрыва проверить595/head,
не повторять push/CI. Далее independent local R10 relay task ownership stage.

## CI594 — R08 compilation PASS, native PARTIAL; isolated fixture fix pending

[CI594/39702615908](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39702615908)
completed FAILURE, source6f615c9fa7243f1365b721a6b78f17dcda6a6538 /
merge5cf4a131b653439396c79f87f84a3f4dc97544ec, exact build identity matched.
Windows119575163356 FAILURE, Ubuntu119575163598 SUCCESS. Parser/BOM fix PASS,
R08 C# compilation PASS; phone stages excluded. Native recovery51/456,
bounded log43/report32/installed helper2,6 actual modal Cancel→Retry and3
chooser, prepared real Windows SSH server contracts5 PASS. In01:06:17 first
SafeLogWorkerWorkflow elapsed/entered assertion FAIL; full Context/log-open,
selected-agent combined and later Desktop/tail/ZIP NOT_RUN, uploads SKIPPED.

Missing fixture precondition confirmed in source: standalone test never creates
its logger epoch after preceding Context.Dispose seals queue admission. The
exact paused/drop cause of failed combined assertion is an inference (old log
cannot distinguish elapsed vs entry); no false claim of product failure/fix.
Isolated a70943991c96907afd829f715bf4fa75a9ee6552 owns/drains/resumes one epoch,
finally suspends it; producer1000ms and native entry3000ms checked separately,
entry waits with real UI pump. Real sink/heartbeat/redaction/file checks remain.
No production changes or timeout increases,9 added lines. C#syntax1/public318/0/
diff/secrets0/full tree equality PASS; Windows new fix NOT_RUN. GitHub object
5f3420d72e5f8c8ac282af6ca7ac57db1d89534c, tree
 ddb95e7ed68f1cf1ad3f27130575f2a4c923df97, ref update PENDING expected6f615.
All root phone stages remain local and excluded from this checking object.

V1.0 exact59419 traced parents weight70×D0.5×30%, P/A0 =>raw10.50→11/100.
+10.50 raw vs593 preflight-only0 reflects actual native criteria, no new weight
or credit for fix code. Source/merge/timestamps in JSON; all whole IDs remain
open where their full criteria are unmet. Implementation58–80 unchanged.
Next: inspect PR head, publish isolated object once, preserve new run/merge;
no duplicate594. Workflows unchanged, no public binary/release/main merge.

## R10 checking review —9 C# syntax PASS, native остаётся NOT_RUN

Финальный self-review3 local phone этапов: syntax9files0errors, suite wiring
(explicit compile+entrypoint), PS UTF8 BOM, public319/0 и diff PASS. Добавлены
native assertions actual main phone card pending→stopped. Независимое agent
review NOT_RUN, Windows compiler/typecheck/native NOT_RUN, баллов0.

Уточнён остаток: sync compatibility Stop вызывается из UseToken на UI, это
не non-UI путь; его worker/DPAPI migration — F31-R09, пока не исправлен.
Relay.Stop закрывает сокеты, но не предоставляет tracked drain всех receive/
flow tasks; полную lifetime приёмку R10 до этой проверки не объявлять.
Новые native tests не подменяют real phone/VPS/owner integration. Следующий
checking source содержит только phone блоки и их tests/docs после completion
единственного CI594; никаких дубликатов или раннего head update.

## F31-R10 этап3 — actual tray/exit ownership, local Windows NOT_RUN

StopPhone/StopAll используют retained async observation; повтор Stop общий,
phone commands disabled до результата, StopDesktop остаётся независимым.
Result TCS завершается перед RefreshPendingRoutes, чтобы меню не зависло в
pending после cleanup; closed owner завершает observation без late UI.
Main phone card показывает «Остановка…»/«Запуск…»/«Нужна очистка» честно.

Shutdown/maintenance останавливают phone worker до изменений desktop proxy,
ждут реальную очистку в существующем settings budget и отказывают handoff при
timeout/false. Admission новых phone starts заранее paused; после отказа или
отмены prepared handoff явно resumed, после успешной подготовки остаётся sealed.
CompleteShutdown больше не выполняет sync Stop на UI. Forced Context.Dispose
вызывает idempotent Home.Dispose: метаданные резервируются до возврата, worker
сохраняется в StopWork без ожидания файлов/процессов на UI. UI Stop во всём
Context переведён; non-UI compatibility Stop остаётся для UseToken (R09).

5 actual Context fixtures: tray, StopAll, bounded refusal→retry→CancelShutdown,
ForceDispose, реальный locked-file refusal→retry. Реальные собственные session
files, native message loop, deadline250ms и fixture waits3/5s; всегда release и
await cleanup. Не выдаются за реальные SSH/phone integrations: те проверяются
отдельно. Новая suite явно включена в Test-ProGo/entrypoint; shared fixture
теперь ждёт Home.StopWork перед dispose Relay. Source syntax7/public319/0/diff
PASS, Windows compilation/native NOT_RUN; это не scope CI594.

Этапы1 df104d5 и2 f5b2236 сохранены отдельно; этот этап — отдельный local commit.
Далее финальный review/scan всех3 phone блоков и один exact Windows checking
SHA после completion594. R10 остаётся PARTIAL, R09 private IO остаётся открытым.
Реализация58–80, verified0(CI593),0 баллов за новые невыполненные тесты.

## F31-R10 этап2 — actual Wizard Stop использует async owner, local NOT_RUN

Кнопка «Остановить VPN для телефона» теперь идёт через RunStep/StopAsync,
явно сообщает pending/неполную очистку/подтверждённый результат и сбрасывает
только прежнее подтверждение phone internet. Конкурирующие действия disabled.
Title close ждёт successful cleanup, после ошибки окно остаётся для retry;
forced Dispose не прерывает owned task и подавляет late UI. Счётчики не
называют held/failed cleanup завершённой остановкой. Сохранённый доступ остаётся.

3 actual button native fixtures: stay+реальный locked-file failure/retry,
queued close и ForceDispose. Собственный child/session, heartbeat, ожидания
3/5/8s; finally release и await собственной очистки. C#syntax3/public318/0/
diff PASS, Windows compilation/native NOT_RUN (не входят в594). Source review
проверяет и gap между завершением StopChannel и outer RunStep: stopping flag
снимается только в общем finally, queued close не теряется в этом интервале.
Сервисный этап1 df104d5 сохранён отдельно. Полный F31-R10 остаётся открытым:
tray/StopAll/exit ещё sync compatibility, R09 private-load/save отдельно.
Следующий шаг — actual Context Stop/shutdown ownership и exact594 log.
Реализация58–80, verified0(CI593), прирост0 за невыполненные новые тесты.

## CI594 — BOM fix опубликован, один exact-SHA run

Source `6f615c9fa7243f1365b721a6b78f17dcda6a6538`, merge candidate
`5cf4a131b653439396c79f87f84a3f4dc97544ec`, tree
`bd0853c7ce82916a202a0b3c2495e24c824a854a` = local
`5fc228f144d6b91adb8a77f158a93785ae7c7db1`.
[CI594/39702615908](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39702615908)
in_progress, compilation/native NOT_RUN. Точный diff:3 BOM bytes, никаких
изменений assertions/deadlines/guards. PR88 OPEN/DRAFT updated, expected-head
без force подтверждён; public uploads OFF, release/main merge не выполнялись.
Локальный phone lifecycle df104d5 в этот source НЕ входит. Проверить тот же
run после обрыва; второй CI/повтор push не создавать. Реализация58–80;
verified0 относится к completed593 preflight, historical59213 сохранён,
прирост за этот source0. Продолжить независимую local UI часть R10.

## CI593 parser FAILURE — BOM-only fix ready, ref PENDING

Sourcec3a67ebff9a6ac10e93059bc73dc2fadd8fef29a / merge30a39709b1144b99c493c49db0e10ada8f6cef2e,
run39701960142 completed FAILURE. Windows119573054034 остановился в00:49:43
на ParseFile tests/DiagnosticPreviewTests.ps1; Ubuntu119573054253 SUCCESS.
Новые C#/native R08, relay/new discovery/ZIP NOT_RUN; uploads SKIPPED.
Причина: добавленная русская header assertion в UTF8 без BOM. WindowsPS5.1
читает default ANSI; bytesD092 первой В становятся smart quote и ломают literal.
Локальный cp1252 decode воспроизводит smart quote; native parser ещё NOT_RUN.

Isolated local5fc228f144d6b91adb8a77f158a93785ae7c7db1 / treebd0853c7ce82916a202a0b3c2495e24c824a854a
= GitHub object6f615c9fa7243f1365b721a6b78f17dcda6a6538. Ровно3 BOM bytes,
все assertions/timeout/content сохранены (byte equality BOM+old PASS).
Public318/0/diff/tree equality PASS; semantic additions/secret/PII0; workflows
unchanged, public uploads OFF. Phone lifecycle df104d5 excluded, Windows NOT_RUN.
Следующий шаг: проверить actual PR head, один expectedc3a67→6f615 без force,
новый exact-SHA run. Не повторять593. Результат предыдущего source сохранён.

V1.0 current593 raw0→0/100: Windows проверки до критериев не дошли, P/A0.
Это отсутствие доказательств на новом SHA, не FAIL всех функций. CI59213/raw13.05
остаётся историей; его баллы новому source не приписываются. Реализация58–80,
прирост за BOM0. Главный ближайший blocker: exact R08 Windows execution.

## F31-R10 этап1 — owned phone lifecycle, local Windows NOT_RUN

HomeVpnService резервирует StartWork/StopWork до Task.Run. Повторный Start
получает тот же task; Stop/Dispose сразу резервируют один cleanup, отменяют
worker и ждут его фактического завершения перед удалением exact session.
Новый Start во время Stop отклоняется. ACL/key/known_hosts/config подготовка,
relay/native disposal/session delete теперь worker-only, короткий lifecycleGate
не держится на I/O. SSH inputs копируются на admission; отмена проверяется
между файловыми операциями и до child/relay publication. Stop false/fault не
теряет directory/process owner; повторная явная остановка пробует его снова.
Dispose не ждёт UI, StopWork остаётся доступным. RecoveryStatus читает один
snapshot proxy, избегая null-race между двумя чтениями.

3 bounded native fixture scenarios добавлены в существующий Windows harness:
held preparation→Stop, held preparation→Dispose, held cleanup + реальная
FileShare.None блокировка→false→retry. Проверяют native heartbeat, single
flight, отсутствие позднего child/config, saved DPAPI bytes и собственный
child cleanup. Каждый finally release+await cleanup; ожидания3/5/8s ограничены.
C#syntax2files/public318/0/diff PASS; Windows compilation/native NOT_RUN, не
входит в593. Это не end-to-end R10 PASS: tray/wizard/exit пока вызывают sync
compatibility Stop. R09 constructor/share/token/DPAPI I/O остаётся отдельно.
После сохранения этого локального блока приоритет — parser failure CI593,
отдельным isolated checking source без phone изменений. Баллов за source0.

## CI593 — R08 checking source опубликован, один run в работе

Source `c3a67ebff9a6ac10e93059bc73dc2fadd8fef29a`, merge candidate
`30a39709b1144b99c493c49db0e10ada8f6cef2e`, tree
`e3104d9fead1a4fff51d598b6099690cb72bcf4e` = local
`e62d21c9d84b110fd7f588c27c912cf48d861413`.
[CI593/39701960142](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39701960142)
in_progress; новые R08 Windows compilation/native NOT_RUN. Source scan17 paths/
929 additions0 secrets/PII; public318/0, diff/tree equality PASS, workflows/VERSION
unchanged, public binary uploads OFF. Expected-head4dd→c3a67 без force confirmed;
PR88 OPEN/DRAFT updated, main merge/release/installer upload не выполнялись.
Score13/raw13.05 принадлежит только предыдущему exact CI592 scoped native;
новый source даёт0 баллов до logs. Реализация58–80. После обрыва проверять
этот же593 и фактический head, не повторять push/CI. Пока он выполняется,
разрешена независимая локальная работа F31-R10; в593 она не входит.

## Текущее подтверждение CI592 — completed FAILURE, узкие Windows PASS

Source `4dd4987cf6f453847931820a6f9ba1298f99f802`, tested merge
`4cf49375895d64e79a951bfc926a01502f9a7352`.
[CI592/39698918707](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39698918707):
Windows119563052615 FAILURE, Ubuntu119563052956 SUCCESS. Build identity совпала.
Repository build/Test-ProGo SUCCESS: report32/helper2,4 modal Cancel→Retry,
3 chooser,3 diagnostic Context; recovery51/456/guidance10, signing55/UI50,
aggregate72, Desktop5132, Instance28/Maintenance126/Shutdown47 PASS.
Оба настоящих direct/owned-agent сценария прошли выбранный ключ→native
OpenSSH→SOCKS5→owned HTTP→прерывание child→повторная preparation/recovery.
Это локальный SOCKS HTTP payload, не полная проверка HTTP bridge/внешнего VPS.

Измерения из native log: direct startup2014/preparation29ms,
recovery1938/preparation29,28ms; agent startup1999/preparation87ms,
recovery1994/preparation87,82ms. CPU own SSH31.25/15.625ms, RAM~11MB.
Это изолированный runner, owner HDD/whole-process I/O/целевой ПК NOT_RUN.
HomeVPN975/Portable90 PASS; relay Python29 tests завершились с1 import error:
в00:43:53 старое test_ssh_loopback_fixture.py повторно обнаружено без Paramiko
после восстановления PYTHONPATH. Причина теперь подтверждена exact log;
new-name explicit prepared discovery уже исправлен отдельным local e62d21c.
Pack SKIPPED, новый ZIP/hash отсутствует; все uploads SKIPPED.

V1.0 прежние веса:28 родителей кромеF19/F27/F30, сумма87, D0.5/P0/A0.
Raw13.05→13/100; +2.55 raw к591 за реально выполненные native критерии,
не за fix/commit. Все28 timestamp anchors находятся в JSON current_readiness.
Реализация58–80 без изменения. R08 checking objectc3a67/treee3104d9
ещё НЕ опубликован, его Windows compilation/native NOT_RUN,0 новых баллов.
Следующий шаг — один expected-head update4dd→c3a67, сохранить новый run/merge;
пять mandatory SSH contracts не отключаются. F31-R11 остаётся PARTIAL:
R08/R09/R10, полная итоговая performance/owner приёмка не закрыты.

Ниже сохранены прежние checkpoints; их pending/in_progress — исторические
наблюдения, заменённые актуальным разделом и машинными полями выше.

## R08 checking object подготовлен — ref update PENDING completion592

Local `e62d21c9d84b110fd7f588c27c912cf48d861413`, GitHub commit object
`c3a67ebff9a6ac10e93059bc73dc2fadd8fef29a`, одинаковый tree
`e3104d9fead1a4fff51d598b6099690cb72bcf4e`. Scan17 tree paths (включая old/new
имя одного Git rename),929 added lines:0 secrets/PII; public318/0, diff/JSON,
C#syntax10files0errors PASS. Rename не отключает5 mandatory native SSH tests;
проверены новым именем локально5 PASS4.237с. Workflows/VERSION без изменений,
public uploads/release выключены прежними guards. Windows R08 NOT_RUN.

Branch пока остаётся4dd4987c:592 repository build SUCCESS, relay ещё в работе.
Не запускать второй CI/не обновлять head до completion592. Если прервано,
проверить head PR88 и run39698918707 перед любым ref update; затем один
expected-head update от полного4dd4987c без force, новый exact-SHA native run.
R08 object включает logger/open/folders/installed VERSION и discovery boundary;
PS updater cold-load/log/folder GUI I/O остаётся вне исправленного scope.
Evidence11(CI591),0 баллов за подготовленный object. Следующий шаг — exact592
logs и публикация только после установленного результата, не blind retry.

## Test-suite boundary fix — explicit SSH fixture discovery preserved

Пять SSH server contract tests переименованы в ssh_loopback_fixture_tests.py,
Test-ProGo.ps1 явно запускает новый pattern в том же подготовленном pinned
PYTHONPATH и с прежним90с budget. Число/содержание тестов не уменьшено; actual
native Linux new-name suite5 PASS за4.237с. Git rename сохраняет историю.

Причина: Test-HomeVpnRelay.ps1 позднее делает discover test_*.py уже после
restore environment и setup другого Python. Старое имя случайно импортировало
SSH test без его dependency environment. Clean local Python discovery old-name
дал1 import error при отсутствии глобального Paramiko (без запуска сценариев).
Новый filename не входит в relay pattern; mandatory prepared launch остаётся
в Test-ProGo. Это отдельный исправленный harness-boundary source defect, не
отключение проверки и не утверждение о результате ещё работающего CI592.

CI592 сейчас repository build SUCCESS, relay step in_progress; старое имя
входит в его source4dd4987c. До exact log не объявлять end-to-end SUCCESS.
Windows new discovery wiring NOT_RUN. Следующий checking tree включает этот
отдельный rename commit и R08 блок; source/secret/workflow scan перед push,
после completion592, без дубликата. Evidence11(CI591), прирост0 за rename.

## R08 checking scope — source syntax/dependencies reviewed, Windows pending

Текущий code checkpoint `f7ec7f2cc95bb723ee111f1ba58e2d5ce31fc0cd` содержит7
логических R08 этапов отдельными commits. Diff к isolated47f03e3:14 files,
827 additions/158 deletions (до этого checkpoint текста), без workflows/VERSION.
Tree-sitter0.25.2/C#grammar0.23.1: все10 изменённых C# files parse PASS,0 ERROR/
missing nodes. Это только syntax, не C# typecheck/compilation/native. Инструмент
установлен только в /tmp, не стал product/CI dependency. Public318/0, JSON и
diff PASS; shared BoundedLog.cs сохраняет Core/bootstrap/installed helper source
связи, новый LogOpenOperation включается существующим src wildcard desktop build.

Минимальный поддержанный Windows-прогон — scripts/Test-ProGo.ps1 на новом
точном source/merge (narrow Desktop selector отсутствует):
- BoundedLog existing rotation/privacy + new capacity/single-sink/pause/resume/
  refusal/exception lifetime vectors; Core bootstrap и installed Add-Type compile.
- Actual SafeLog held sink/heartbeat/redaction,3 epoch assertions,2 Context logger
  shutdown/Dispose scenarios;4 open worker cases,6 actual log/folder route cases.
-6 actual modal Cancel→Retry cases (4 прежних +2 installed VERSION),3 native
  chooser cases,3 Diagnostic Context ownership cases; fresh installed helper
  privacy/clipboard/header tests. Old loaded-API transition отдельно NOT_RUN.
- Existing full Desktop/instance/maintenance/shutdown/portable and real SSH
  direct/agent/recovery regression; runner package/hash только после success.

CI592 source4dd4987c НЕ содержит этот R08 code. Не отправлять новый head до
completion592; затем one checking tree с secrets/PII/workflow scan и один CI.
Новые tests ещё NOT_RUN, score11(CI591), прирост0 за parser/review/commits.
Остаток R08: installed updater cold dot-source/Add-Type и его PS Open-UpdateLog/
Open-ProGoFolder/Write-UpdateLog GUI I/O. Source confirmed; actual slow-I/O
Windows reproduction/acceptance NOT_RUN, не объявлять эти пути исправленными.

## F31-R08 этап7 — installed VERSION читается в preview worker, local NOT_RUN

Diagnostics-ProGo.ps1 больше не выполняет Test-Path/OpenText/Read VERSION в
Click. Новый ShowInstalled(root) сначала показывает пустой shared modal UI,
затем tracked Build worker читает максимум32 chars, проверяет отмену до/после
native read и передаёт snapshot существующему allowlisted builder. Missing/
denied/invalid version становится unknown; raw VERSION текст не экспортируется.
Cancel не объявляет незавершённый I/O завершённым, Retry вновь читает version.

2 дополнительных настоящих ShowDialog Cancel→Retry cases (click/Escape) держат
version-read boundary, сохраняют responsive loop/одного owner/cleanup; после
отмены меняют VERSION0.2.2→0.2.3, принимается только fresh header и fresh log.
Установленный fresh PowerShell helper test также требует header0.2.2 и прежнюю
privacy/copy гарантию. Все прежние deadlines3с/8с/cleanup5с сохранены.
Source/diff/public318/0 PASS; C#/PS/native Windows NOT_RUN, не входят в592.

Совместимость при update: уже загруженный old API может не иметь ShowInstalled,
хотя helper script уже заменён. Metadata feature detection вызывает старый
Show(root,'') с честной unknown версией без нового UI file read; actual old-API
transition native NOT_RUN, не объявляется принятой интеграцией.

Уточнение прежнего source finding: normal Initialize-UpdateWindow заранее
компилирует diagnostic types до error-dialog. Cold Add-Type на Click остаётся
возможен в early-error/fresh helper path, а dot-source/PS helper log/folder I/O
тоже отдельный остаток. Этот шаг устраняет только unconditional VERSION read,
не закрывает весь R08. Native root log/queue/folder/version блок ещё ждёт своего
checking SHA после592. Evidence11(CI591), прирост0 за source.

## F31-R08 этап6 — folder/backups actions на owned worker, local NOT_RUN

OpenProGoFolder/OpenBackups используют тот же tracked open workflow. Фиксированный
requested path вычисляется без I/O; Ensure/CreateDirectory и native Explorer
start выполняет worker. directoryOnly mode не проверяет/не создаёт log file.
Все4 viewer commands имеют общий pending owner и защищены от duplicate launch;
settings/routes не блокируются этим локальным просмотром. Error остаётся на
UI, пути корректно quoted, принятый пользовательский Explorer не завершается
при закрытии ProGo. Shutdown/Dispose с прежним budget сохраняют ownership и
отменяют admission до позднего launch; fixed error теперь относится к файлу/
папке, не ошибочно только к журналу.

4 дополнительных actual command-route cases (2 folders × shutdown/ForceDispose)
используют настоящий собственный native child вместо системного Explorer.
Проверяют подготовку выбранной папки, heartbeat, duplicate/cancel/retry, один
cleanup и unchanged installed bytes. finally всегда release до cleanup,
свои native child handles и read-only fixture attributes очищаются также при
ошибке/timeout предыдущего ожидания. Source/diff/public318/0 PASS; Windows
compilation/native NOT_RUN, не входят в592. Согласованные функции не расширены.

R08 source блок теперь охватывает logger +3 viewer paths, но ещё требует exact
Windows и installed updater cold diagnostics UI path. Последний не закрыт;
slow target disk/owner measurements также NOT_RUN. Evidence11(CI591), прирост0.
Далее — source acceptance checklist/secret scan R08 checking tree и текущий
CI592; после его completion отдельная проверка всех этих новых Native cases.

## CI592 — delimiter fix опубликован, один run в работе

Head PR88 `4dd4987cf6f453847931820a6f9ba1298f99f802`, merge
`4cf49375895d64e79a951bfc926a01502f9a7352`, tree
`71a53965acd9d4eb19b6fbe6a9a691693d6b56b6` = isolated local47f03e3bc8f054305d810095a3bfc6128c494cb1.
Единственный [CI592/39698918707](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39698918707)
in_progress. Source scan5files/347lines0 secrets/PII, public317/0/diff/tree PASS,
expected-head update без force. Новый Windows compilation/native NOT_RUN до
logs; прежний591 observed FAIL не превращается в592 PASS. Source-import root
`964bf83fb22d40f931d38de972475607a52baa19`; R08 log/queue code до5d30993 остаётся
отдельным неотправленным блоком. Следующий шаг: same run592 exact logs;
независимо довести folder/backups viewer через тот же owned worker, затем
один Windows checking tree R08 после592. Evidence11/raw10.50 остаётся у591;
0 баллов за source/push. Releases/binary uploads/main merge/целевой ПК/VPS/
system settings не выполнялись.

## Delimiter fix object создан, ref update PENDING

Local fix `47f03e3bc8f054305d810095a3bfc6128c494cb1`, GitHub object
`4dd4987cf6f453847931820a6f9ba1298f99f802`, одинаковый tree
`71a53965acd9d4eb19b6fbe6a9a691693d6b56b6`. Scan5files/347addedlines:
secret/PII0, public317/0, diff/tree equality PASS; workflows/VERSION/launcher
не менялись. Следующий шаг — expected-head693de8ab →4dd4987c без force и один
автоматический новый CI. При обрыве сначала проверить PR88/runs, не повторять
внешнюю операцию вслепую. Root импортировал только два code files+docs, R08
работа сохранена отдельными commits и в fixed checking tree отсутствует.
Windows этого fix NOT_RUN; evidence11 относится к591, прирост0 за commit.

## CI591 delimiter-fix candidate — source only, separate from R08

ProxyService constructor теперь отделяет непустой extraOptions от следующего
аргумента пробелом; caller не обязан добавлять trailing space. Пустой production
prefix остаётся пустым. Существующий native combined fixture намеренно передаёт
-F quoted(config) без trailing space и остаётся regression, проверки не ослаблены.
Failure branch печатает только completed/status/preparation count/owned/typed
local reason и уже classified safe SSH guidance, без raw stderr/argv/key paths.
Это устраняет недостаток доказательств старого log, не скрывает сам failure.

Native Linux ssh-G separator reproducer PASS: до255/config-N, после0/expected
host, без TCP/SSH соединения. Windows compilation/combined-native этого нового
source NOT_RUN. Diff/public317/0 PASS. Код isolated worktree содержит только
ProxyService и соответствующий fixture; R08 logger/open source по-прежнему
исключён. Следующий шаг — scan exact commit tree, expected-head693de8ab → новый
source, один CI. Реализация58–80, verified11(CI591), прирост0 за сам source fix.

## CI591 — launcher PASS, combined start FAIL; exact evidence11

[CI591/39696687583](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39696687583)
completed FAILURE: Windows119555663751 FAILURE, Ubuntu119555663596 SUCCESS.
Source `693de8abda98f48108cea97624cbf9cd165b376d`, merge
`774c77a6f94193ea91281c6adf72a08481db82db`, exact identity0.2.2+этого merge.
Launcher exit0/exit17/owned-timeout regressions PASS; real dependency install
прошёл,5 native OpenSSH server contract tests PASS за3.947с на Windows.
Recovery51/456/guidance10 PASS; report32/helper2/modal4/chooser3/Context3,
settings93, signing55 и aggregate prior43 vectors reached PASS. Новый full
aggregate summary и Desktop summary отсутствуют: остановка на первом direct
combined startup assertion. Agent combined/recovery/metrics и последующий
Instance/Shutdown/HomeVPN/ZIP NOT_RUN, не PASS; uploads SKIPPED. Собственные
SSH/server cleanup markers PASS, timeout не скрыт.

Подтверждённый code defect: ProxyService constructor присваивает raw extraOptions,
StartSingleTunnel склеивает его напрямую с -N. Combined fixture передаёт
-F quoted(config) без trailing space, argv получает config-N. Native Linux
isolated ssh-G reproducer: old prefix exit255/config path changed, separated
prefix exit0/expected host. Windows exact underlying StartupError прежний log
не печатал; причинная связь этого FAIL с delimiter — обоснованный вывод из
кода/reproducer, а не прочитанный stderr. Добавляется безопасная typed причина
и state/count/ownership только при failure; сырые stderr/argv/key paths не выводим.

Отдельный worktree ProGo-ci591-fix от localdd9b17a: нормализует boundary prefix
(пустой production default не меняется) и использует существующий real native
combined тест как regression. R08 source commits до5d30993 сохранены отдельно,
не включать их в эту узкую публикацию. New fix Windows compilation/native NOT_RUN.
Следующий шаг — scan new exact tree, expected-head693de8ab, один новый CI.

V1.0/W100/D-P-A30/20/50 прежние. Exact log591 подтверждает partial D0.5 у
19 родителей F01/F02/F04/F05/F06/F07/F08/F09/F10/F11/F12/F14/F15/F16/F17/
F23/F25/F26/F31 (weight70). К прежним590 добавились native CLI migration,
actual typed proxy rollback, bridge, tray settings, restore shutdown heartbeat,
settings persistence, layout и diagnostic keyboard Tab/Shift+Tab. Все anchors
проверены в raw591 log; F17 диагностическая keyboard доступность00:07:35.526.
P0/A0 (нет ZIP/приёмки). Raw70×0.30×0.5=10.50→11, изменение от590+4.35 raw,
потому что реально выполнен дополнительный Desktop scope до FAIL, не за commit
или полный CI SUCCESS. Реализация58–80 неизменна; F31-R11 всё ещё PARTIAL.

## F31-R08 этап5 — logger owner seal и shutdown/exit lifetime, local NOT_RUN

Context получает epoch logger owner. Begin/resume/suspend управляются коротким
metadata gate без I/O; старый Context не может приостановить или возобновить
нового owner. Shutdown немедленно закрывает admission logger и ждёт его
реальный drain совместно с log-open/diagnostic в прежнем settings budget,
до proxy cleanup/ACK. При отказе/отмене обслуживания admission возвращается
только ещё живому текущему owner; early false/exception тоже покрыт wrapper.
ForceDispose сохраняет LogWriterWork без UI wait. Нельзя вернуть success для
всё ещё held sink. Поздние subsystem log attempts во время seal best-effort
отклоняются (счётчик), не создают новые внешние записи после handoff.

Main после UI loop и terminal CLR handler имеют bounded500мс best-effort
финальный flush (timeout печатает NOT_RUN, не выдаёт durability PASS). Это не
замена строгому pre-handoff drain в Context; recoverable UI ThreadException
не ждёт disk. Необратимый process/OS kill по-прежнему может терять best-effort
logs; профили/секреты никогда не передаются в queue и ею не обновляются.

Native fixtures: obsolete owner resume/suspend, actual held global logger +
UI heartbeat, shutdown refusal до cleanup, explicit retry, ForceDispose без
UI pump и unchanged settings/vault/VERSION. Все ожидания/cleanup ограничены;
global test hooks восстанавливаются в finally даже при timeout/ошибке.
Source/diff/public318/0 PASS; C# compilation/native NOT_RUN в Linux. SafeLog
writer source этап4 `da9ad49901ba8eb27f4ac9c39a94023f131a2b1c`.
Не входит в CI591 source693de8ab; evidence6(CI590), прирост0.

Следующий шаг — exact Windows checking SHA всего связанного R08 блока после
completion591; отдельно остаются folder/backups/cold helper preparation I/O,
slow-disk/owner hardware измерения. F31-R08 ещё не PASS. Сначала проверить
native logger/helper/bootstrap regression, не предполагать её по source review.

## F31-R08 этап4 — SafeLog пишет через bounded worker, local native NOT_RUN

SafeLog больше не вызывает EnsureDirectories/BoundedLog.TryWrite на caller:
только worker sink разрешает AppPaths, создаёт каталоги/вращает/пишет файл.
До queue поступает уже redacted immutable string ≤4096chars; Exception/raw
message не захватываются worker. Oversized raw input не режется посреди
секрета, а целиком заменяется фиксированным omitted marker; после redaction
общий bounded record сохраняет корректную UTF16 границу и truncated marker.
Обычный формат времени/level/redaction сохранён. Queue capacity/dropped/failed
контракт этапа3; I/O failure не создаёт автоматического retry внешней записи.

FlushAsync — асинхронный snapshot lifetime для observability, не обещание
долговечности и не запрет новых producers. Новый actual SafeLog native fixture
держит собственный disk boundary, проверяет UI heartbeat, sanitization до sink,
pending drain и реальные bytes после release. Existing clipboard log check
теперь явно ждёт асинхронный sink (5с), не читает файл в ложный момент.
Source/diff/public318/0 PASS; C# compilation/native NOT_RUN в Linux и не входят
в CI591. Shutdown/process-exit admission/drain ещё TODO следующего отдельного
этапа: не считать этот source окончательной logger lifetime приёмкой.
Реализация58–80, evidence6(CI590), прирост0. Далее — owner-scoped pause/resume
перед shutdown/handoff и bounded post-UI exit flush; old actor не должен
возобновлять admission нового owner. Native проверять новым exact source.

## CI591 — launcher fix опубликован, run в работе

Head PR88 `693de8abda98f48108cea97624cbf9cd165b376d`, merge
`774c77a6f94193ea91281c6adf72a08481db82db`, tree
`789f87c6aaab9748a43be493ec175451b2f43e0e` = isolated localdd9b17afb01a0264e6f6a96d7a3f6f1f5ba81b44.
Единственный [CI591/39696687583](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39696687583)
in_progress. Никакого rerun590 или второго dispatch. Новый fix compilation/PS/
native NOT_RUN до exact logs; исходный failure590 сохранён ниже со своими SHA.
Текущая evidence база6/raw6.15 относится к590, не считается PASS591; прирост0.

Root local импортfixb2b510be37b152a1c514dde0a0847d7a6d927286 содержит также
ранее сохранённые R08 log-open/queue source blocks. Их code НЕ входит в591;
source queuecdadbf429aaac3db10ebb921e88a08e22770031f пока только foundation,
SafeLog всё ещё synchronous. Следующий шаг — отдельное wiring SafeLog с
redaction до queue, bounded data и shutdown/exit drain; параллельно читать тот
же run591. Main merge/public release/binary uploads/owner installation не делались.

## Launcher fix commit object создан — ref update PENDING

Local fix `dd9b17afb01a0264e6f6a96d7a3f6f1f5ba81b44`, GitHub object
`693de8abda98f48108cea97624cbf9cd165b376d`, одинаковый tree
`789f87c6aaab9748a43be493ec175451b2f43e0e`. Scan4files/485addedlines,
0 secrets/PII, public317/0, diff PASS; workflows/VERSION неизменны. Следующий
шаг — expected-head8487594e →693de8ab без force. Если прервано, проверить
фактический head PR88/runs и не повторять update/dispatch вслепую. Native fix
ещё NOT_RUN. Root безопасно импортировал только script+docs из isolated
worktree; R08 source commits сохранены и не входят в этот checking tree.

## CI590 fix candidate — isolated launcher source, native pending

Этот worktree основан на local60f5e92 (tree017b33cb = published8487594e).
В code diff только Test-ProGo.ps1 launcher: прямой System.Diagnostics.Process
создаёт и сохраняет native handle; одновременно дренирует stdout/stderr,
обязательно проверяет ненулевой/пустой ExitCode, сохраняет install60с/contract90с
и cleanup5с. Startup/error/timeout finally закрывает только свой процесс/streams.
Нет Start-Process -PassThru или cast null→0. Согласован с upstream PowerShell
https://github.com/PowerShell/PowerShell/issues/5421 и /issues/20400.

Перед pip выполняются3 native regressions: exit0 принимается; exit17 обязан
вернуть именно ошибку17; timeout200мс обязан завершить только своего sleeping
child и вернуть точную timeout ошибку. Missing code/неверный exception/оставшийся
child → FAIL. Negative vector не превращает timeout реальной установки в PASS.
Ни один прежний timeout не увеличен, проверки не выключались. Windows/PS этих
новых изменений NOT_RUN в Linux; diff/public317/0 PASS, source self review.

R08 log-open/queue commits29b98b34/e4dc5a7f/cdadbf42 сохранены в root branch,
их CODE не включён в этот узкий checking candidate. Документация их учитывает
как local PARTIAL/NOT_RUN. Следующий шаг — scan exact committed fix tree,
expected-head8487594e без force, один новый CI с прямыми launcher regressions.
Реализация58–80; проверенная готовность CI5906/raw6.15, прирост0 за fix source.

## CI590 — exact FAILURE launcher exit-code observation; новая evidence база6

[CI590/39694720495](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39694720495)
completed FAILURE: Win119549198567 FAILURE, Ubuntu119549199364 SUCCESS.
Source `8487594e6d7d4e4eb16df86aae27f2e980329aaa`, tested merge
`20df578c1c2d162f5f373e94aed8107bbfee7d19`, identity0.2.2+этого merge.
Actual log: recovery51/456 PASS, включая10 guidance assertions; report32/helper2,
BoundedLog32, retention29/integrity82/updater134/install145/check72 PASS.
Desktop compilation PASS, но весь Desktop native, Python server contracts,
combined direct/agent/recovery/metrics и subsequent Instance/Shutdown/ZIP
не запущены: NOT_RUN. Package/hash отсутствует; uploads SKIPPED.

Подтверждённый FAIL: Test-ProGo launcher выбросил
`Owned SSH fixture install failed with exit code ` с пустым значением.
Actual pip exit неизвестен. Причина получения ExitCode согласуется с известной
Start-Process -PassThru/redirect handle problem (PowerShell issues5421/20400);
проверки и install60с не выключаются/не увеличиваются. Отдельный fix worktree
ProGo-ci590-fix от local60f5e92 не включает unverified R08 code. Следующий шаг —
прямой System.Diagnostics.Process handle и native regressions exit0/exit17/
owned timeout; это новый source, не слепой retry старого CI.

V1.0/W100/D-P-A30/20/50 неизменны. Новый exact snapshot имеет конкретно
трассированные partial D0.5 для F02/F04/F05/F07/F08/F09/F23/F25/F26/F31,
суммарный вес41; остальным D0 до новой трассировки, не автоматически FAIL.
P0 (ZIP не создан), A0. Raw41×0.30×0.5=6.15 →6/100, изменение от589 raw−15.60.
Основания в590 log: fallback23:42:49.150; foreign-SOCKS23:41:34.418;
slow-start23:41:09.912; retention23:46:11.393; integrity23:46:20.222;
wrong-pin23:38:54.296; rollback-progress23:46:44.643; log32/report32;
quiet-start23:46:07.341; preparation51/45623:45:36.178. Не начисляем баллы
недостигнутому Desktop или compiled-only tests. Не трассированные assertions
сейчас не дают дополнительных баллов; это консервативная earned-evidence база.
CI58922/raw21.75 остаётся историческим полным снимком, не PASS source590.
Реализация58–80 прежняя; это изменение подтверждённости, не удаление функций.

## F31-R08 этап3 — bounded queue foundation, source only

BoundedLogQueue в общем BoundedLog.cs (сохраняет bootstrap/helper dependency
boundary): caller TryEnqueue не ждёт queue lock/I/O;128×4096-char bounded
backlog, single drain без queue gate на sink. Full/oversized/contended/paused
rows отклоняются с Dropped counter; false/throw sink — Failed counter, без
автоматических повторов. SuspendAsync закрывает admission сразу и берёт
worker snapshot после уже принятого producer; lifetime остаётся pending до
реального settlement. Resume явный; старый completion не очищает нового owner.

Native fixtures добавлены в BoundedLogTests: blocked real file sink, bounded
caller/backlog/order, pause vs pendingI/O, resume, refusal/throw и продолжение
следующих rows, cleanup своих barriers/workers в5с. Source/diff/public318/0
PASS; C# compilation/native NOT_RUN в Linux. SafeLog ещё НЕ переведён на эту
очередь; это подготовительный source, не исправление всех UI log I/O и не
основание закрыть F31-R08. Далее SafeLog formatter/sink wiring и native/exit
settlement — после отдельного исправления CI590 launcher.

CI590 Win119549198567 FAILURE: launcher получил пустой ExitCode из
Start-Process -PassThru, actual pip exit неизвестен. Это не установленный
dependency FAIL/timeout и не PASS. Ubuntu119549199364 SUCCESS; native direct/
agent Desktop suite не начат. Точный лог будет перенесён в failure checkpoint.
Нельзя отправлять foundation/LogOpen блоки случайно вместе с узким launcher fix.

## F31-R08 этап2 — реальные log commands и shutdown ownership, local NOT_RUN

OpenAppLog/OpenUpdateLog теперь резервируют LogOpenOperation и полный observer
Task до worker queue; существующие log menu actions отключены до completion.
Directory/legacy path/CreateNew и native viewer start идут в worker; путь для
Notepad корректно quoted, handle после принятого запуска освобождается без
закрытия пользовательского editor. Ошибка показывается на native UI через
существующий persistent dispatcher; canceled/closed owner не публикует late UI.

Shutdown и Dispose сразу Cancel обоих diagnostic/log actors. Shutdown ждёт
их settlement совместно в прежнем settingsShutdown budget (не два timeout),
до proxy cleanup/handoff. Agent startup Close остаётся до первого ожидания.
Незавершённый OS call не объявляется отменённым/успешным; выход отказывается
с фиксированной причиной и разрешает явный повтор. ForceDispose сохраняет
LogOpenWork до фактического settlement; старый callback не очищает новый actor.

2 actual command-route native fixtures добавлены к4 worker cases: native UI
heartbeat, duplicate log actions, shutdown timeout без внешнего cleanup,
Cancel→explicit Retry с одним настоящим own child, forced Dispose без late
viewer/UI, повтор shutdown cleanup ровно один раз, installed bytes unchanged.
finally release/cancel всех своих barriers/tasks, cleanup только test child;
локальные5с/3с budgets сохранены. Source/diff/public318/0 PASS; все новые
C# compilation/native NOT_RUN. Worker stage source29b98b34c993fc14f24c5ab6226f5d63519c10f5.

Не входит в CI590 source8487594e. F31-R08 остаётся FAIL по остальным SafeLog/
folder/backups/cold-helper UI I/O; этот узкий путь лишь PARTIAL до exact native
run. Баллы58–80/22 доказанной базы CI589, прирост0. Далее: source commit этого
блока, затем bounded worker logger (отдельный контракт); CI590 читать отдельно.

## F31-R08 этап1 — owned log-open worker +4 fixtures, source PARTIAL

LogOpenOperation публикует Work до Begin, повторный Begin возвращает ту же Task.
Directory/legacy-path/file операции — worker; CreateNew не обнуляет raced файл,
существующий read-only log не требует записи. Cancel не ждёт I/O/native call:
до admission исключает viewer, после admission lifetime остаётся owned до
возврата launch. Отмена не выдаёт blocked read за завершённый; Context будет
ждать Work перед shutdown/handoff. User-owned viewer после принятого запуска
не закрывается приложением. Source не содержит системных изменений.

4 bounded native fixtures: success, held-I/O cancel, prepare error, existing
read-only file. Проверяют published single-flight Task, native UI heartbeat,
worker thread, actual own child invocation/exit, unchanged source bytes,
nonzero failures и cleanup/restore только своих ресурсов. Native NOT_RUN в
Linux, source/diff/public318/0 PASS. Пока runtime Context wiring TODO; helper
не закрывает F31-R08 и не переносит CI590/589 SUCCESS на новый source.
Следующий этап — wiring OpenAppLog/OpenUpdateLog и ожидание/cancel при shutdown/
Dispose с actual command-route fixtures. Баллы58–80/22 прежнего CI589, прирост0.

## CI590 — exact checking source опубликован, compilation/native pending

Head PR88 `8487594e6d7d4e4eb16df86aae27f2e980329aaa`, merge
`20df578c1c2d162f5f373e94aed8107bbfee7d19`, tree
`017b33cbc8504e6af6ff52a619063219bdd6ed69` = local60f5e92e336d78a5d2f438975773e2c03f61fb16.
[CI590/39694720495](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39694720495)
in_progress; повторного dispatch нет. Scope: guidance/recovery51, isolated native
server5, actual direct+agent preparation→SSH→SOCKS→HTTP/recovery, bounded Python
launcher/metrics. Их Windows compilation/native NOT_RUN до exact log review.
CI589 completed SUCCESS на прежнем7964a99a, score22 только его доказанная база;
новых баллов0. Public uploads/release не включались. Source scan9files/1050lines
0 secrets/PII, public317/0, identical tree, expected-head update без force.

Отдельный local F31-R08 worker +4 tests ещё незавершён, не входит в CI590;
не включать его случайно и не приписывать этому source результаты590. Следующий
шаг — wiring worker к actual OpenAppLog/OpenUpdateLog и shutdown/Dispose, затем
отдельный commit/Windows fixtures; параллельно читать только существующий run590.

## CI589 — exact-SHA SUCCESS, общий lifecycle scoped PASS

[CI589/39692239050](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39692239050)
completed SUCCESS, Win119540954252/Ubuntu119540953856 SUCCESS. Source
`7964a99ac4f6adf0d1d9034c855e989f60211c0a`, tested merge
`f8eb30fddc7f0ecdf0f795de76b8f4be56490705`, build identity0.2.2+этого merge.
Raw Windows log сверён: recovery50/446 включая9 новых lifecycle scenarios,
report32/helper2,4 actual modal Cancel→Retry, normal/missing/error chooser3,
Context ownership3, revision lease8, aggregate43/signing55/UI50, Desktop5103,
Instance28/Maintenance126/Shutdown47, HomeVPN975/Portable90. Native sshd.exe
наличиеTrue — только capability, системная служба не менялась этим probe.

Runner ZIP0.2.2 SHA256
`c66a56fda89845e0e4e68c5fb2d1f87c70e74d01f101113ad8d5e1b21f194805`.
Uploads SKIPPED, downloaded bytes/private delivery/owner install NOT_RUN/BLOCKED.
V1.0 сохраняет D0.5/P0.5 для87 весов, A0, raw21.75→22; прирост0, веса прежние.
F31-R11 PARTIAL: combined real preparation/SSH и typed guidance последующих
local commits ещё NOT_RUN. Их подготовленный source8487594e не включён в589.
Untracked src/LogOpenOperation.cs — отдельный незавершённый F31-R08 worker,
пока без wiring/tests/commit, исключён из fixed checking tree017b33cb.
Следующий шаг — expected-head update8487594e от7964a99a, один новый CI.

## Следующий checking tree подготовлен — ref update пока НЕ выполнен

Local `60f5e92e336d78a5d2f438975773e2c03f61fb16`, GitHub object
`8487594e6d7d4e4eb16df86aae27f2e980329aaa`, одинаковый tree
`017b33cbc8504e6af6ff52a619063219bdd6ed69`. Scan9files/1050addedlines,
secret/PII0, public317/0, diff PASS; VERSION/workflows неизменны, upload/release
не включены. Object создан, но source head пока7964a99a: сначала дождаться
completion существующего CI589/39692239050, прочитать его exact log, затем
один expected-head fast-forward от полного7964a99a без force. Не повторять
push/dispatch после обрыва без проверки фактического head/runs. Windows новых
51 recovery/direct+agent combined/launcher/metrics NOT_RUN, score0 за source.

## Проверка CI589 во время подготовки следующего source

Windows119540954252: шаг Repository hygiene and build SUCCESS; relay integration
ещё in_progress. Ubuntu119540953856 SUCCESS. Полный run39692239050 ещё не
завершён; новый head не отправляется, чтобы не отменить его. Загрузка job log
во время работы вернула BlobNotFound404: это недоступность ещё не опубликованного
лога, не FAIL теста. Точное число50 и identity будут сверены после completion.
Следующий local source `de155d5a786fa1965318b7149fb443ec173eb08c` включает
separate guidance/native51, изолированный server, direct+agent combined
start/recovery, bounded launcher и metrics. Все новые Windows сценарии NOT_RUN.
Git clean, logical commits сохранены; score0 за эти невыполненные проверки.
Далее: финальный committed diff/secret scan, после completion CI589 — один
expected-head source update и новый run для изменённого кода, не retry старого SHA.

## F31-R11 test launcher — bounded native Python status

Test-ProGo запускает install и5 server-contract tests как отдельные собственные
processes с budget60/90с. Stdout/stderr читаются после settlement как log text;
обычный unittest progress/ожидаемый отказ handshake в stderr не становится
NativeCommandError PowerShell. Единственный критерий status — проверенный
exit code; nonzero/timeout → FAIL с причиной, NOT_RUN зависимых native сценариев.
После timeout завершается только созданный Process; killed timeout не PASS.
Публичные workflow/upload guards не менялись. Python fixture без subprocess
команд; pip binary-only без сборщиков. PS execution NOT_RUN в текущей Linux.
Diff/source/public317/0 PASS. Оценка58–80/22 прежней CI588 базы, прирост0.
Следующий шаг — результат CI589 и exact-SHA публикация накопленных логических
test/source commits; измерения ещё не выданы за фактический Windows результат.

## F31-R11 dev measurement — source, показатели ещё NOT_RUN

Combined direct/agent startup и recovery печатают elapsed_ms, все длительности
реальных preparation, CPU time/working set/peak working set только своего SSH
child. Счётчики недоступны → явный NOT_RUN с типом ошибки; отсутствие измерения
не превращается в PASS. Это данные изолированного runner, не замер ПК владельца
или HDD/VHDX, и не process I/O всей программы. Последние критерии открыты.
Pinned Paramiko4.0.0 проверяется при создании стенда: другое содержимое старого
build не выдаётся за испытанную зависимость. Python native5 на этом изменённом
контракте PASS за3.665с; Windows C#/metrics/PS NOT_RUN, баллов0.
Agent-only source этап6: `47a7467f4b6d36b33c8ee8b5ad89337ab990e431`.
Далее: bounded Python test launcher с stdout/stderr как обычным log (ожидаемый
native negative-case stderr не должен ломать PowerShell независимо от exit code),
затем checking publication после текущего CI589.

## F31-R11 этап6 — selected agent key в combined native сценарии, NOT_RUN

Второй combined case использует отдельный owned Windows named-pipe agent с
настоящими LIST/SIGN ответами. Единственный private RSA key остаётся в памяти;
на диск попадают только выбранные public references. Ни shared/default agent,
ни mutating ADD/REMOVE, ни системные credentials не используются. Native
ssh-add проверяет подготовку выбранной identity; затем сам OpenSSH должен
подписать server authentication тем же ключом, а Paramiko проверяет signature.
Лишь HTTP payload после SOCKS подтверждает actual SSH, локальная подпись
отдельно не считается доступом на сервер. Recovery повторяет обе реальные фазы.

Accept/clients single-owned, pipe ready deadline3с; Dispose сначала закрывает
только свои pipe handles и затем ждёт accept/clients с deadlines5с и finally,
включая ошибку одного worker. Snapshot env SSH_AUTH_SOCK восстанавливается;
RSA provider удаляется после settlement. Этот тест моделирует уже загруженный
ключ, не ввод passphrase/ACL на ПК владельца. Последние по-прежнему отдельные
критерии; существующий interactive Add suite не заменяется.

Исходный direct combined source `f485cbb0eddd6c05dc9fc21156e9d26d6c7be87e`.
Текущие compilation/native Windows NOT_RUN; source review/diff/public317/0 PASS.
Баллы58–80/22 без прироста; CI589 ещё проверяет source7964a99a без этих local
изменений. Далее — прочитать результат CI589 и опубликовать новый checking
source direct+agent+recovery, вместе с отдельной guidance/native51.

## F31-R11 этап5 — combined native Windows fixture подключён, NOT_RUN

Local source расширяет SshAuthenticationPreparationWorkflow: реальный disposable
keygen → explicit immutable preparation с тем же isolated SSH config → общий
ProxyService StartSingleTunnel → настоящий OpenSSH с pinned host key → SOCKS5
CONNECT → HTTP payload собственного loopback сервера. После прерывания только
своего SSH PID timer recovery обязан вновь выполнить preparation, создать один
новый PID и повторить payload. IdentityAgent none/direct unencrypted сохраняет
штатный вход. Это общий runtime pipeline, но не GUI/real VPS/зашифрованный agent
сценарий; SettingsService final revision lease отдельно проверен прежними suites.

Test-ProGo готовит Paramiko только в build/ssh-fixture-deps через binary wheels,
с install deadline60с; public uploads/workflow не меняются. На dependency FAIL
native NOT_RUN и CI FAIL; тест не пропускается.5 native Python fixture contracts
затем Desktop combined test; временные PYTHONPATH/fixture env восстанавливаются.
Stop/start/server streams и собственные processes имеют локальные deadlines,
явный server cleanup marker обязателен. Уточнён Python cleanup: закрывает все
свои принятые sockets даже до старта Transport; stalled-handshake test сохраняет
клиент открытым до завершения server cleanup (раньше клиент закрывался первым).

C# compilation/native и PowerShell execution NOT_RUN в Linux. Diff/public317/0
PASS; Python native5 на изменённом fixture PASS за4.343с (Linux OpenSSH).
Исходный stand source65b98ead4c901aeae66d009f6eaa7ba6fe4c835a сохранён в истории;
новый Windows source включает также local guidance83cf660e,51 recovery cases.
Никаких баллов за ещё не выполненный combined Windows тест;58–80/22 остаются
привязаны к прежнему CI588. Следующий шаг — завершить текущий CI589 и затем
опубликовать проверенный checking tree этого этапа на одном новом CI.

## F31-R11 dev-стенд — реальный isolated SSH без системной службы

Добавлен test-only Paramiko4.0.0 ServerInterface стенд: signature проверяет сама
библиотека, разрешён только disposable public key пользователя progo-fixture;
SSH/HTTP слушают127.0.0.1 на выделенных портах. Единственный direct-tcpip target —
собственный HTTP listener. Shell/session/password/прочие назначения запрещены.
Никакие службы/accounts/ACL/сеть/VPS владельца не меняются. Зависимость ставилась
только в /tmp/progo-isolated-ssh-deps, не в системный Python и не в release.

Linux native ssh/ssh-keygen:5 unittest PASS за4.179с — действительная signature
→ SOCKS5 → HTTP payload, другой выбранный ключ, changed host key, запрещённый
destination без dial, закрытие stalled SSH handshake с cleanup собственных
transports/threads/child. У всех ожиданий и cleanup есть локальные deadlines;
CLI ready.json атомарен, lifetime90с с обязательным stop/cleanup. Фактический
host key/key bytes ephemeral и в репозиторий не входят. Это проверка стенда,
ещё НЕ ProGo production pipeline: Windows/ProGo combined integration NOT_RUN.
Источник контракта: https://docs.paramiko.org/en/latest/api/server.html и
https://www.paramiko.org/changelog.html (4.0/Python3.9+; frozen test contract).

Следующий этап — native Windows fixture/общая preparation + owned SSH, pin host
key в isolated config, exact-SHA запуск; затем agent signing/missing-key/Cancel.
Средовой Linux blocker /run/sshd для этого тестового пути устранён без изменения
системы. F31-R11 PARTIAL и баллы58–80/22 не меняются: это не приёмка Windows
и не разрешение входа на реальные ресурсы. CI589 по-прежнему отдельный run.

## F31-R11 этап4 — раздельная локальная подсказка при реальном SSH отказе

Source `83cf660ea51674cafa905c60835bd071477acd5b`: минимальный local этап — ProxyService snapshot безопасного typed display
на worker вне process/revision gates. При отказе собственного SSH StartupError
и permanent recovery pause сохраняют исходную классифицированную причину и
отдельно показывают локальное наблюдение preparation и существующий Check/Retry
маршрут. Наблюдение подписи не выдаётся за серверный доступ; UI polling не
выполняет formatter/I/O. Ошибка formatter не запрещает обычную SSH попытку.
Никакого автоматического Add/service/ACL изменения или нового hard gate нет.

Один новый bounded native scenario (51 recovery cases в следующем кандидате)
проверяет separate messages, отсутствие formatter под gate и повторных вызовов
из UI, реальный отказ child, formatter exception fallback и explicit fresh Retry.
finally останавливает свои операции, cleanup budget5с. Source/diff/public315/0
PASS; C# compilation/native NOT_RUN в Linux. Этот local этап не входит в CI589;
не публиковать его до завершения уже запущенного run39692239050, чтобы не
отменять проверяемую версию. CI589: Windows119540954252 in_progress,
Ubuntu119540953856 SUCCESS. Реализация58–80, доказанная база CI58822; рост0.
Далее: читать exact CI589, затем новый checking source этого отдельного этапа;
combined real-preparer/native SSH integration по-прежнему не подтверждён.

## CI589 — опубликован общий lifecycle, native ещё NOT_RUN

Подтверждены head PR88 `7964a99ac4f6adf0d1d9034c855e989f60211c0a`, merge
`f8eb30fddc7f0ecdf0f795de76b8f4be56490705`, tree
`8c4be1b5bd69d06cbac06f6c4f33c1ff0edc9d85` (local c00aeeea8b41a852604b5475cb95f53cb0707e36).
Единственный автоматический [CI589/39692239050](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39692239050)
queued при проверке. Native50 recovery scenarios/compilation этого SHA NOT_RUN.
Никакого повторного dispatch; читать именно этот ID и exact log identity.
Предыдущий CI588 SUCCESS и22/100 — доказанная база прошлого snapshot, не PASS
нового lifecycle. Новых баллов0. Все binary uploads/release остаются запрещены.
Следующий шаг: пока CI выполняется, узкий source этап разделения preparation и
actual SSH причин в существующем error UI; проверять lifecycle run отдельно.
Нижняя запись ref pending — история завершённой операции, не текущая команда.

## Checking lifecycle: commit object создан, branch update ещё PENDING

Local `c00aeeea8b41a852604b5475cb95f53cb0707e36`, GitHub object
`7964a99ac4f6adf0d1d9034c855e989f60211c0a`, одинаковый tree
`8c4be1b5bd69d06cbac06f6c4f33c1ff0edc9d85`. Scan7files/739added lines,
secret/PII0, public315/0, diff PASS, workflows unchanged. Следующий шаг —
expected-head update от b32ac4ece5cd62b98eab22b7e65aaa079544e5bf без force.
При обрыве сначала проверить head PR88/runs; этот object ещё не является
подтверждённо опубликованным head и не имеет Windows PASS. CI588 отдельно.

## Следующий checking candidate — общий lifecycle F31-R11

Local source lifecycle `c0f5610a2931b0230a9839291ff1843b4bb0c787` и capability
probe `494cbf138647fe60affa0ade3ce7882953647050` завершены отдельными commits.
Следующая публикация включает их,9 новых bounded recovery сценариев (всего50)
и checkpoint документы. Она не меняет workflows и не разрешает binary uploads.
Windows compilation/native этого lifecycle NOT_RUN до нового точного CI;
успех CI588 остаётся только у source b32ac4ec / merge e1039ddd.
Прежние пункты ниже с11/100 и in_progress — датированная история CI587/588.
Актуальная доказанная база22/100 относится к завершённому CI588; новых баллов0.
Следующий шаг: scan committed range, tree equality, expected-head fast-forward,
сохранить новый source/merge/run ID. Не делать повторный dispatch того же SHA.

## Dev capability probe следующего Windows run

Test-ProGo теперь выводит только boolean наличия встроенного sshd.exe на
изолированном Windows runner. Это read-only capability probe для планирования
combined native SSH стенда: никакая служба/server/порт/account/ACL не меняются
и не запускаются. Наличие executable не считается интеграционным PASS.
Сам probe локально Windows NOT_RUN, его результат читать на новом exact-SHA CI.
Новых Python/server dependencies и публичных binary uploads не добавлено.

## CI588 — точный SUCCESS; следующий lifecycle отдельно NOT_RUN

[CI588/39688353510](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39688353510)
completed SUCCESS: Windows119528038063 и Ubuntu119528038420. Source
`b32ac4ece5cd62b98eab22b7e65aaa079544e5bf`, build/tested merge
`e1039ddd046528fc75b9d46897e6924f597768d5`, identity0.2.2+того же полного merge.
Raw Windows log повторно проверен, не только зелёная иконка:
SOCKS41/403; DiagnosticReport32; installed helper2;4 real modal Cancel→Retry;
normal/missing/error native chooser3;3 actual Context ownership scenarios;
settings revision lease8; SSH advisory preparation43; signing55/UI50;
Desktop5103; Instance28; Maintenance126; Shutdown47; Portable90.
REVIEW-01/02 narrowly native PASS этого SHA, весь F31-R08/R11 не закрыт.
Старый CI587 FAIL и его исходные SHA остаются в истории.

Pack release zip SUCCESS на runner, SHA256
`7c6febfc2ebda1834ffa1d04ebc0cc13001bb0629de8f505facb78898d223c5c`.
Все uploads SKIPPED; архив не скачан и не передан; version0.2.2. Это не private
пакет у владельца и не установленное приложение. Binary upload/release всё ещё
не разрешены, delivery BLOCKED до согласованного приватного способа.

Новый local lifecycle source `c0f5610a2931b0230a9839291ff1843b4bb0c787`,
план50 recovery cases и последующие изменения НЕ входят в CI588. Их compilation/
native NOT_RUN. Кодверификация отдельных lease/aggregate компонентов не является
доказательством нового общего invocation/Stop/recovery пути.

Оценка v1.0 обновлена только по этому подтверждённому результату:28 требований
(все кроме F19/F27/F30) имеют прежний partial D0.5; новый exact-SHA runner ZIP
возвращает P0.5, A0. Σ87×(0.30×0.5+0.20×0.5)=21.75 →22. От CI587 raw10.50
изменение+11.25; веса и весь implementation interval58–80 не менялись.
Это подтверждение native/пакета нового проверенного снимка, не рост от commits.
До owner install/real SSH/VPS/переноса и критических blockers product PASS нет.

Дополнительная dev-capability проверка: Linux sshd существует, но isolated
sshd -t с временным host key вернул255 из-за отсутствующей /run/sshd; Python
server libraries отсутствуют. Каталоги/account/services не менялись, listener
не запускался. Combined real-server test в этой среде BLOCKED; нужен отдельный
изолированный test-server путь, не реальные VPS владельца. Другие задачи
продолжаются. Следующий шаг — checking commit общего lifecycle и exact-SHA CI.

## F31-R11 этап3: общий owned preparation/admission — local source PARTIAL

Main Program использует настоящий preparer через общий StartSingleTunnel:
manual/auto/fallback/recovery проходят один preparation owner. Snapshot, endpoint,
epoch и settlement публикуются до выхода из process gate; readonly config/signing
и occupied-port probe идут вне gate. Оба private caller владеют ровно одним
Monitor entry; три released scopes всегда возвращают его в finally. Новый manual
ждёт старую recovery preparation вне gate; timer не создаёт второй owner.

Stop/Dispose сразу меняют intent/epoch и cancel, StopTask ждёт preparation и
startup settlement вне process gate. Final revision lease nonblocking; Busy
повторяет admission в существующем budget без shared gates. Changed/cancelled
scope не допускается; Busy/timeout/unconfirmed advisory auth не становятся
hard gates. Перед spawn повторно проверяются token/epoch; child получает ровно
captured SSH_AUTH_SOCK (null удаляет позднее наследование). Вызов Process.Start
остаётся owned до возврата; после поздней отмены завершается только свой child.
Lease освобождается до logging/other callbacks. Stale finally не очищает новый
slot. Observation getter фильтрует epoch/cancellation/intent; новая recovery
подготовка очищает прежнее наблюдение.

Добавлены9 bounded native lifecycle сценариев в существующий SocksRecoveryTests:
async single-flight Cancel→Retry, synchronous owner Stop, recovery Stop,
profile/endpoint mutation, Busy admission (success/change), advisory failure
without new gate, per-fallback preparation, forced Dispose, final admission
cancellation/lease release. Существующий foreign SOCKS fixture дополнительно
требует zero preparation. Каждый барьер3с, cleanup своих tasks5с с cancel/release;
fixtures создают настоящие child/SOCKS процессы, preparer boundary инъецирован
для гонок. Это НЕ live SSH/agent auth: настоящий aggregate/native signer suite
отдельный, combined real preparation/tunnel и presentation ещё требуются.

Local source commit `c0f5610a2931b0230a9839291ff1843b4bb0c787`.
Файлы ProxyService.cs/SocksRecoveryTests.cs; production Main ранее явно wired
в59cec1ae. Независимые/bootstrap/HomeVPN constructor по умолчанию не делают
лишний I/O; их real agent integration этим этапом не заявляется. Полный F31-R11
остаётся PARTIAL. Self source/dependency/diff review и public315/0 PASS;
compilation/native этих новых изменений NOT_RUN, CI588 их не содержит.
Старый WIP checkpoint9826e742 сохранён как история, текущие исходники не удалялись.
Оценка реализации58–80, доказанная база CI58711 без прироста за невыполненные
тесты. Следующий шаг — exact-SHA Windows50 recovery scenarios и затем combined
real-preparer/native tunnel и typed UI statuses. Реальный VPS/ПК не проверены.

## История ожидания CI588 — завершено SUCCESS, см. актуальный результат выше

Head PR88 `b32ac4ece5cd62b98eab22b7e65aaa079544e5bf`, tree
`c987657710561336d54abd77d123babe592769c7`, local commit
`5a170b4c061aeb2b8fd5f6b7b58ff095b75d6303` с тем же деревом. Ref update уже
подтверждён, PR description обновлено. Точный merge
`e1039ddd046528fc75b9d46897e6924f597768d5`, единственный автоматический
[CI588/39688353510](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39688353510)
in_progress. Не дублировать push/dispatch; читать тот же run. Новый selector,
revision lease/composition compilation/native NOT_RUN до logs этого SHA.
Незавершённый root lifecycle не входит; source review/fixtures продолжаются
отдельно, checkpoint9826e742 сохраняет прежнюю WIP версию. CI587 PARTIAL/FAIL
и11/100 conservative evidence остаются со своим точным source. Оценка реализации
58–80; новых баллов за этот push/commit нет. Нижние ref-pending блоки — история.

## История перед публикацией CI588 — ref update впоследствии подтверждён

Local `5a170b4c061aeb2b8fd5f6b7b58ff095b75d6303`, GitHub commit object `b32ac4ece5cd62b98eab22b7e65aaa079544e5bf`,
полное идентичное дерево `c987657710561336d54abd77d123babe592769c7`. Опубликованным head пока
не объявлять: следующий шаг expected-head update от `e943ca348d17cbed3f4b5b4b9bca4e206ffb3d03`.
Diff10files/492addedlines secrets/PII0, public315/0, diff checks PASS; workflow
не менялся, public uploads/release не включены. Unfinished lifecycle исключён.
Если прервано, сначала проверить head PR88 и runs именно нового SHA,
не повторять update/CI вслепую. Native нового source NOT_RUN; прежняя
консервативная доказанная база CI58711/100 сохраняется со своим SHA.

## Исторический результат CI587 и последующее исправление native chooser

Source `e943ca348d17cbed3f4b5b4b9bca4e206ffb3d03`, tested merge
`decfa11496c12c5e65987043fb58fc7388cb5d6c`,
[CI587/39685125066](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39685125066)
completed FAILURE: Windows119517114158 FAILURE, Ubuntu119517113715 SUCCESS.
Точный log identity `0.2.2+decfa11496c12c5e65987043fb58fc7388cb5d6c`.
C# compilation PASS; SOCKS41/403, DiagnosticReport32, fresh installed helper2,
Codex67, settings93 и4 actual modal ShowDialog Cancel→Retry scoped PASS.
Полный Desktop не прошёл; Context ownership и SSH aggregate suites после
chooser не достигнуты, как и Instance/Maintenance/Shutdown/HomeVPN/ZIP хвост.
Они NOT_RUN, не FAIL и не PASS. Public uploads SKIPPED, owner PC/VPS NOT_RUN.

Подтверждённый FAIL — normal native chooser child exit1:
`native SaveFileDialog not found within 5000 ms for the fixture owner`, при
этом child сообщил DIAGNOSTIC_CHOOSER_CLEANUP_COMPLETE до внешнего20с лимита.
Совпадение caption в normal selector оказалось лишним условием поверх
PID/thread/root-owner/#32770. Вывод об этом согласуется с тем, что тот же
ownership-only cleanup смог вернуть modal вызов; фактическое значение caption
старый log не сохранил, язык/точный текст не выдумываются.

Точечное исправление в отдельном worktree: normal/error режимы выбирают
native dialog по прежним строгим ownership guards без caption; missing режим
по-прежнему намеренно не находит его и должен вернуть20 + точный FAIL + cleanup.
Ничего не выключено: real production ShowDialog, choices1/observedNative,
нулевые записи, неизменность logs,5с deadline,20с external job budget и
ошибки cleanup сохранены. Добавлены только безопасные observed/choices/cleaned
flags в log, без window texts/личных путей. Новый selector пока Windows NOT_RUN.

Проверочный worktree базируется на completed local commit
`59cec1ae32485d65d5f578329aea768d59a04347`: включает завершённые source этапы
revision lease `9f6c3615405158a9d69ea81089d3a4beb47bb272` и observation/composition,
которые ещё compilation/native NOT_RUN. Незавершённый actual lifecycle diff
основного worktree НЕ входит: ProxyService SHA256
`b04bfe80e9426f75c6bc9094ef3e01ae9384ab2a7c1debc91c362e9f81a8c137`, backup
/tmp/progo-f31-owned-launch-wip.patch. Дополнительно сохранён local checkpoint
`checkpoint/f31-owned-launch-wip-20261022`, полный SHA `9826e742eb987d18cadc63a6aa3f396b91e32447`; stash create/branch
не очищали working tree и не меняли активный HEAD/index. Не откатывать его и не публиковать без
review/lifecycle tests. После исправления CI продолжить этот отдельный этап.

### Оценка v1.0 после смены проверяемого кандидата

Веса/формулы прежние. Историческая CI586 база22 не присваивается CI587.
На CI587 есть конкретные partial native доказательства D=0.5 для19 родителей:
F01,F02,F04,F05,F06,F07,F08,F09,F10,F11,F12,F14,F15,F16,F17,F23,F25,F26,F31.
Их веса по исходной матрице суммарно70. Источники — те же указанные критерии
и найденные в точном CI587 log native строки (report/modal/keyboard источники
F14/F17 уточнены новым реальным diagnostic workflow). Остальным строкам D=0:
их прежние конкретные примеры не достигнуты/не трассированы в новом log;
не подтверждённые критерии не начисляются. P=0 у всех: ZIP/hash на CI587 нет;
A=0 у всех: приёмки владельцем нет. Σ70×0.30×0.5=10.50, показ11 (half-up).
Это консервативный индекс заработанных доказательств, не оценка доли функций.
Неполно разобранным native свидетельствам баллы не приписываются. Прежняя
матрица21.75 и все SHA ниже сохранены как историческая первая база, веса не
изменены. Реализация58–80/100; текущая подтверждённая готовность11/100;
изменение raw −11.25 (display22→11) из-за нового candidate/package/native scope,
а не исчезновения реализованного кода. Новый selector/lease/composition NOT_RUN.

Следующий шаг: secrets/diff scan checking tree, source publish в ту же ветку
expected-head без force и один новый CI; основание нового запуска — source
исправление подтверждённого chooser FAIL, не слепой retry старого SHA.

## Постоянный порядок работы — действует с22.10.2026

Этот порядок задан владельцем и заменяет прежние «одно продолжай — одна
микрозадача», обязательные остановки после этапа и временный ночной режим.
Нижние датированные checkpoint сохраняются как история, их команды остановки
не ограничивают текущую разрешённую последовательность.

1. Самостоятельно продолжать согласованные F01–F31, пока есть разрешённая
   незаблокированная работа. Завершённый этап — промежуточный отчёт, не запрос
   «продолжать?». Отсутствие ответа не означает паузу; явные стоп/пауза/новый
   приоритет владельца имеют преимущество. Объём и концепцию не расширять.
2. Одна микрозадача — один конкретный проверяемый результат. Крупный пункт
   делить заранее. Цикл: минимальная правка → diff → целевые проверки → отдельный
   логический локальный commit → состояние/реестр → следующая задача. Чужой или
   незавершённый diff не включать; не удалять/откатывать работу ради чистого status.
3. После этапа и перед долгими операциями фиксировать выполненное/остаток,
   полные source/local/published/tested SHA, PR/CI IDs, проверки, блокеры и шаг.
   При восстановлении сначала читать документы и сверять Git/процессы/CI;
   не повторять внешнюю операцию до установления результата прежней попытки.
   Не запускать конкурирующего исполнителя на те же файлы.
4. Разрешены публикация исходников и согласованных исправлений в существующую
   `integration/approved-plan-31-20261011`, обновление PR88 и необходимые CI;
   разрешение относится также к последующим согласованным задачам. Перед каждой
   публикацией проверять diff/новые файлы на secrets/PII и автоматические workflow.
   При найденных secrets остановить публикацию, сообщить без раскрытия значений.
5. Не разрешены merge main, публичный release, выгрузка бинарных пакетов/installer,
   установка на ПК владельца, реальные VPS/системная сеть/права/credentials и платные
   сервисы без отдельного применимого разрешения. Если соблюдение требует правки
   workflow, сначала предложить точечную правку на согласование; продолжать
   независимую локальную работу. Не менять workflow ради тайного обхода границ.
6. Один SHA — один необходимый CI, без дубликатов без новых оснований. Сохранять
   run ID и проверять его; во время ожидания делать независимую работу, не
   приписывать старый CI новому SHA. При FAIL установить причину, выделить
   исправление; не выключать тесты, не заменять native сценарии моками и не
   увеличивать timeout ради зелёного. Повторять неуспешный подход только с
   новыми данными. Долгие операции не превращать в бесконечное polling-ожидание.
7. BLOCKED задаётся конкретной причиной и минимальным действием владельца;
   один блокер не останавливает независимые пункты. Общая остановка — только
   завершённый объём, команда владельца, пределы среды или отсутствие любой
   разрешённой незаблокированной работы. Никакого фактически не созданного
   расписания/автоперезапуска/гарантии непрерывного исполнения не заявлять.
8. Сохранять v1.0 и веса100;31/55 не удваивать. Рост только за подтверждённый
   результат точного SHA; отчёт, commit и невыполненный тест не дают баллов.
   NOT_RUN не PASS. Открытые критические блокеры не компенсируются суммой.
   Формат после этапа: «Сделано: … · Проверки: … · Реализация: X/100 ·
   Проверенная готовность: Y/100 · Изменение баллов и основание: … · Далее: …».

### F31-R11 этап2: production composition и bootstrap boundary — source PARTIAL

Этап1 сохранён local commit `9f6c3615405158a9d69ea81089d3a4beb47bb272`.
Program теперь явно передаёт настоящий SshAuthenticationPreparation preparer
в ProxyService. В самом ProxyService только ISshLaunchPreparationObservation
и revision-admission delegate: minimal SocksRecovery/HomeVPN bootstrap assemblies
не получают зависимость от всей диагностики/agent/UI и скрытые реальные ssh-G.
Независимый test constructor по умолчанию остаётся без дополнительного I/O.

Observation проверяет immutable candidate/executable/endpoint и отбрасывает
Cancelled/Changed; не является private-key/server authorization. Два assertions
добавлены в существующую aggregate suite. Сам preparer пока ещё не вызывается:
common owned lifecycle — следующий этап. F31-R11 не закрыт. Source/dependency
и diff checks PASS, public315/0; compilation/native NOT_RUN и не часть CI587.
При первой diff-проверке исправлена случайная двойная CR-нормализация Program.cs;
итоговый diff содержит только4 нужные строки Program, полная перезапись не входит.
Оценка58–80/22, прирост0. CI587 e943ca34 остаётся единственным проверяемым run.

### F31-R11 этап1: nonblocking launch revision lease — source PARTIAL

`SettingsService.TryAcquireLaunchRevision` добавляет Acquired/Busy/Changed без
ожидания занятого commitGate, проверяет именно опубликованную ревизию и удерживает
её против другого writer до Dispose. Удаление lease идемпотентно на owner thread;
неправильный thread не теряет возможность owner освободить lock. Никаких native
проб/await/логирования/Save/диска внутри admission нет. API пока не используется
ProxyService и само по себе F31-R11 не закрывает.

В существующем isolated SettingsPersistenceWorkflow добавлены8 native assertions:
неизменность bytes, wrong-thread dispose, blocked real writer, освобождение,
Busy без ожидания, stale revision, null snapshot, fresh retry. BeforeReplace
barrier ограничен3с, finally освобождает barrier/lease и объединённо ждёт обе
свои tasks до5с до исходного file restore. Новых entrypoints/fixtures/main hooks
не требуется; harness уже компилирует этот файл. Файлы Core.cs и
DesktopSettingsPersistenceTests.cs; local compile/native NOT_RUN в Linux.

PASS self source/diff check, public-content315/0 и targeted secrets patterns0.
Эти изменения не входят в source e943ca34/CI587. Следующий этап — actual owned
launch reservation/preparation вне process gate, final revision lease перед
spawn, общий manual/recovery путь и Stop settlement. Оценка58–80/22 без прироста.

### Опубликовано: CI587, единственный запуск нового кандидата

Source `e943ca348d17cbed3f4b5b4b9bca4e206ffb3d03`, local logical commit
`cd8c468b48f293fe809aea1d1ed75662a8e5a97f`, общее дерево
`b1bbec0ca9876edafc8cd0cf8b16a605bef2ed45`; PR88 head подтверждён после
expected-head fast-forward без force. PR88 OPEN/DRAFT, description обновлено.
Точный PR merge `decfa11496c12c5e65987043fb58fc7388cb5d6c`.
[CI587/39685125066](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39685125066)
запущен автоматически, дубли не создавать. Ubuntu job119517113715 SUCCESS;
Windows job119517114158 in_progress, compilation/native накопленного блока
ещё NOT_RUN до проверки logs. CI586 не даёт этому source успеха. Upload OFF.

Во время ожидания независимая следующая микрозадача F31-R11: узкий nonblocking
SettingsService revision lease и bounded regression под существующим settings
fixture. ProxyService пока не меняется; новый local diff не входит в CI587.
Сохранять этот source/run при следующих локальных изменениях, не подменять SHA.
Реализация58–80/100, проверенная готовность22/100, прирост0 до новых критериев.
Предыдущие блоки ниже — история предыдущих фаз, ref update уже выполнен.

### Подготовленный source: remote ref update pending

Local commit `cd8c468b48f293fe809aea1d1ed75662a8e5a97f`, GitHub commit object
`e943ca348d17cbed3f4b5b4b9bca4e206ffb3d03`, общее дерево
`b1bbec0ca9876edafc8cd0cf8b16a605bef2ed45`. Metadata commit SHA различается
между local git и GitHub API;16 файлов/полное дерево идентичны. Локальный commit
сохранён, историю не переписывать. Scan2881added lines findings0; public315/0
и diff checks PASS. Native Windows NOT_RUN. Объект ещё не подтверждён как head:
следующее действие — expected-head fast-forward remote integration branch
от656b3d65. При прерывании сначала прочитать PR/head и runs для e943ca34,
а не повторять update/push/CI вслепую. Generated binaries не публиковались.

### Активная точка: проверочный16-file блок и новый Windows CI

Проверка актуальности: local HEAD и PR88 head
`656b3d65e03d540be859ee69635c492631093446`; PR OPEN/DRAFT/unmerged;
CI586/39670153699 completed SUCCESS только на прежнем merge
`7e204c7659cefd8142bb33a3cbfc7cc0ec974bc2`. Активного конкурирующего агента,
Git merge/rebase/index lock и процессов ProGo/Git/Windows build не обнаружено.
Текущий diff — ровно16 согласованных файлов ниже; код REVIEW-01/02 не менялся
после их точек. Linux toolchain не позволяет native Windows прогон локально.

Перед публикацией проверены оба workflow: PR synchronize запускает ci.yml,
на public repository автоматические uploads OFF; release.yml публикует только
при подходящем main push или отдельном dispatch, которые сейчас не выполняются.
Workflow правка для этого push не требуется. Ни public binary upload, ни release
не разрешены и не включаются. Следующий шаг: final diff/secrets check16 файлов,
отдельный local commit, tree-verified fast-forward существующей remote branch,
фиксировать новый source/tested SHA и ID CI; после этого F31-R11 этапами.

Windows compilation/native накопленного блока NOT_RUN до нового exact-SHA CI.
Реализация58–80/100, проверенная готовность22/100, изменение0. Эта новая
документация/авторизация не повышает оценку. После source commit его полный SHA
и последующий published SHA/CI записать отдельным checkpoint; самоописательный
commit SHA заранее не выдумывать.

## REVIEW-03 — PASS только синхронизация metadata CI586

22.10.2026 01:03 МСК. Изменены только `docs/ACCEPTANCE_RUNBOOK.md`,
`docs/project_state.json`, `docs/PROJECT_STATE.md`. Код и тесты сохранены;
новый commit/staging/push/PR mutation/CI не выполнялись. Следующие датированные
блоки этого документа — история на соответствующий момент: прежние pending,
in_progress и открытая REVIEW-03 внутри них не являются текущим статусом.

| Область | Текущий подтверждённый результат |
| --- | --- |
| Published source B | `656b3d65e03d540be859ee69635c492631093446`, PR88 OPEN/DRAFT/unmerged по последней проверке |
| Реально проверенный merge M | `7e204c7659cefd8142bb33a3cbfc7cc0ec974bc2` |
| CI586 | [run39670153699](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39670153699), completed SUCCESS только для B/M |
| Jobs, повторная read-only проверка | Windows119465588823 SUCCESS, Ubuntu119465589235 SUCCESS |
| Build identity | `0.2.2+7e204c7659cefd8142bb33a3cbfc7cc0ec974bc2`, повторно прочитанный Windows log |
| Runner ZIP | Pack SUCCESS, SHA256 `d7a6176ec2da62a5b53e2faea2a86893919a853353a5166f669baa0042896191`; uploads SKIPPED, bytes не скачаны/не переданы |
| Локальный L поверх B | Диагностика/report/Context, advisory SSH preparation, ShowDialog Cancel→Retry и bounded SaveFileDialog: **Windows compilation/native NOT_RUN**, нового source SHA нет |
| Интеграция/пакет/целевой ПК | Не приняты; final setup/fallback не готовы, приватная передача BLOCKED, установка и live ПК/VPS NOT_RUN |

SUCCESS CI586 не переносится на локальный L или целое требование. Уточнены
current поля ci_candidate, fresh_candidate_verification, remote_visibility.ci,
86 integration/candidate записей и текущие scoped evidence для уже проверенных
в B/M retention/updater/shortcut/settings/signing. Это внесение существующего
доказательства, не новый прогон и не новый функциональный PASS. Все86 ID,
31 parent/55 child, их status/remaining/dependencies/package/target сохранены.

История: CI583,584,585 и их failures остаются без изменений в ci_history.
CI586 добавлен туда с completed результатом и отдельным prior_observation —
прежним in_progress снимком. tested_baseline_source/merge остаются историческим
CI582 baseline с явной пометкой; текущая последняя проверка находится в
ci_candidate. Прежние CI584 scoped passes и CI585 shortcut failure не стираются.

REVIEW-03 **PASS только metadata consistency**. REVIEW-01/02 source fixes
по-прежнему PARTIAL до native Windows; F31-R11 остаётся PARTIAL, общий
SSH start/recovery aggregate не подключён. Из этой документационной правки
не следует приёмка31/31,55/55 или готовность установленного приложения.

Доступные проверки этого прохода:

- PASS JSON parse,86 уникальных ID=31+55, сохранность requirement statuses,
  критериев/остатка/зависимостей, package/target и трёх прежних CI history entries.
- PASS consistency check трёх текущих checkpoint: одинаковые B/M/run/jobs/ZIP;
  completed/success только у опубликованного CI; локальные compilation/native
  NOT_RUN. Архивный prior_observation и датированные исторические pending
  проверены как история, не ошибочно удалены общим поиском/заменой.
- PASS состав следующего коммита совпадает с16 фактическими modified/untracked
  файлами и между тремя документами; текущий HEAD и пустой index сохранены.
- PASS scope guard:13 остальных файлов, включая весь production/test/script
  diff, побайтно прежние по SHA256-манифесту начала задачи.
- PASS `git diff --check` трёх документов и `python scripts/check_public_content.py`
  (315 source,0 release). Это проверки документации, не Windows tests.

### Следующий проверочный коммит — только план

Предлагаемый commit: `Bound diagnostic preview lifecycle and add advisory SSH preparation`.
Точный состав —16 файлов, без generated binaries/release payload:

```text
src/DiagnosticPreview.cs
src/DiagnosticReport.cs
src/UpdateAwareTrayApplicationContext.cs
src/SshAuthenticationPreparation.cs
tests/DesktopDiagnosticTests.cs
tests/DesktopTests.cs
tests/DiagnosticPreviewTests.ps1
tests/DiagnosticReportTests.cs
tests/DesktopAsyncDiagnosticPreviewTests.cs
tests/DesktopDiagnosticContextTests.cs
tests/SshAuthenticationPreparationTests.cs
scripts/Test-ProGo.ps1
docs/ACCEPTANCE_RUNBOOK.md
docs/MICROTASK_SSH_AUTH_PREPARATION_SCOPE.md
docs/PROJECT_STATE.md
docs/project_state.json
```

Перед будущим commit повторно сверить весь diff и secrets/PII; source SHA
будет известен после commit. Коммит включает диагностический/advisory блок,
а не реализацию launch/recovery F31-R11, repair или installer fallback.

Минимальный **существующий** Windows entrypoint на новом точном SHA:

```powershell
powershell.exe -NoProfile -STA -ExecutionPolicy Bypass -File .\scripts\Test-ProGo.ps1
```

На изолированном Windows CI/dev runner скрипт сам собирает приложение и harness.
У DesktopTests нет selector этих suites, поэтому этот готовый entrypoint
выполняет общий Desktop и сопутствующие Windows регрессии; отдельно запускать
полный Actions workflow, Ubuntu, Test-HomeVpnRelay или upload не нужно.
Обязательны реально выполненные DiagnosticReportTests, fresh-process installed
DiagnosticPreviewTests.ps1, старый DiagnosticPreviewWorkflow, новый
AsyncDiagnosticPreviewWorkflow (4 modal Cancel→Retry +3 native chooser cases),
DiagnosticContextWorkflow, SshAuthenticationPreparationWorkflow и старые
selected-key/agent/policy, Desktop/Instance/Maintenance/Shutdown ownership cases.
Подробные критерии сохранены в [runbook](ACCEPTANCE_RUNBOOK.md), структурированный
состав/команда — uncommitted_candidate в JSON. Если guard дал SKIP/NOT_RUN,
это не успешная native проверка даже при exit0. Live auth/установка отдельно.

Этот следующий этап сейчас не выполнялся. При будущем долгом запуске сохранить
точный SHA и ID job/сессии, закончить отчёт и проверять тот же запуск следующей
командой. Не начинать новый параллельный CI. Остановиться после REVIEW-03.

Оценка v1.0/веса не менялись; новых Windows/пакетных/целевых результатов нет.
Реализация: 58–80/100 · Проверенная готовность: 22/100 · Изменение: +0 · Главный блокер: F31-R11 — подготовка выбранного ключа не подключена к SSH start/recovery.

## REVIEW-02 — bounded native SaveFileDialog fixture: source исправлен, Windows NOT_RUN

22.10.2026 00:54 МСК. Выполнена только REVIEW-02. HEAD остаётся
`656b3d65e03d540be859ee69635c492631093446`; текущий diff не коммитился,
не публиковался и не запускался в CI. CI586 относится к B/M из оценки ниже,
не к этим локальным изменениям. REVIEW-03 и production source не менялись.

Изменения относительно начала этой микрозадачи — только3 файла:

- `tests/DesktopAsyncDiagnosticPreviewTests.cs`: прежний
  `AsyncDiagnosticNativeChooserCancel` вызывает отдельный дочерний STA fixture
  с настоящим production SaveFileDialog.ShowDialog. Поиск ограничен5с; при
  отсутствии совпадения выдаётся `native SaveFileDialog not found within 5000 ms
  for the fixture owner`, а не бесконечное ожидание suite/job timeout.
  Если окно найдено, но не закрылось, это отдельная timeout failure.
- Поиск и WM_CLOSE больше не используют глобальный FindWindow по caption:
  EnumThreadWindows ограничен native thread своего preview, затем проверяются
  PID, GA_ROOTOWNER и класс #32770. Очистка при неправильном caption использует
  те же проверки ownership; чужие окна по совпавшему заголовку не закрываются.
- Внешнее ожидание ребёнка —20с через существующий `DiagnosticProcess.Run`:
  процесс создаётся suspended и включается в отдельный owned job до исполнения.
  При успехе, исключении и deadline существующий owner завершает только этот
  job и проверяет settlement; бюджет его принудительной очистки — ещё до2с.
  Новый общий CI timeout, process-name kill или поиск чужих PID не добавлялись.
- Внутренний finally останавливает timer, закрывает только свои native dialogs,
  dispose preview и без UI pump ждёт объединённые Work/BuildWork/SaveWork до5с.
  Dispose/settlement выполняются и при ошибке запроса WM_CLOSE. Cleanup marker
  выдаётся только после settlement и проверки отсутствия оставшихся owned окон.
  Если native modal loop вообще перестал обслуживать сообщения, внешний job
  deadline остаётся независимым от его UI timer; такой случай всегда FAIL.
- Добавлены3 сценария одного fixture: обычный native Cancel; намеренно
  неподходящий caption; намеренная ошибка обработчика после нахождения окна.
  Негативные дети возвращают именно ненулевые20/21 с точной ошибкой и cleanup
  marker; harness проверяет оба условия, реальные native window observation,
  отсутствие записи и сохранность источников. Это проверка ожидаемого отказа,
  не преобразование настоящего timeout в PASS. В основном Cancel-сценарии
  любой timeout — FAIL; внешний20с timeout во всех режимах — FAIL. Ошибка
  cleanup либо неожиданный exit тоже FAIL. Если настоящий диалог вообще не
  появился, негативный сценарий не принимается как успешно проверенный отказ.
- `tests/DesktopTests.cs`: ровно один dispatch для
  `--diagnostic-save-chooser-fixture` до общего suite watchdog. Ребёнок исполняет
  только этот fixture и не запускает весь Desktop suite рекурсивно.
- `docs/PROJECT_STATE.md`: эта точка продолжения. Все прежние fixture methods,
  включая REVIEW-01 ShowDialog Cancel→Retry, сохранены побайтно относительно
  начала задачи; исправление REVIEW-03 не выполнялось.

Результат REVIEW-02 — **PARTIAL: source fix и regression scenarios добавлены;
Windows compilation/native execution NOT_RUN**. В этой Linux-среде отсутствуют
Windows Forms, dotnet/csc/mcs/mono/pwsh/powershell. Три native сценария,
изоляция окон/job и регрессия всего диагностического блока здесь не исполнялись;
ни одно из этих утверждений не помечается Windows PASS. Независимый агент
не запускался, выполнено собственное source/diff review.

Завершённые доступные проверки:

- PASS `python scripts/check_public_content.py`:315 source,0 release files.
- PASS целевые source checks: существующий suite вызывает fixture; child route
  достижим до общего watchdog; вызваны все3 режима; локальный5с и внешний20с
  пределы; thread/PID/root-owner guards; finally и объединённый task settlement.
  Это статические проверки wiring/контракта, не C# compilation/native tests.
- PASS `git diff --check` для3 изменённых в этой задаче файлов; сохранено
  существующее предупреждение LF→CRLF для DesktopTests.cs, whitespace errors нет.
- PASS targeted secrets/PII pattern scan137 добавленных строк тестового кода:
  findings0. Это проверка шаблонов, не обещание исчерпывающего secret detection.
- PASS scope guard:13 остальных modified/untracked файлов совпадают с исходным
  SHA256-манифестом; предыдущая часть DesktopAsyncDiagnosticPreviewTests до
  заменённого native chooser метода побайтно прежняя; в DesktopTests добавлен
  только один dispatch. HEAD и пустой staged index не менялись.

Точные текущие test SHA256 (локальный diff поверх HEAD, не commit SHA):

- `tests/DesktopAsyncDiagnosticPreviewTests.cs`:
  `58cf747c4e897862cb9f35ab835caa9d2675cc9edde742cc0e30b01cc6004acd`.
- `tests/DesktopTests.cs`:
  `314d3f00aff6aa3827a1e559b603ea087eb39a5db4d236f3368b23ea83d41f49`.

Остаток: Windows-проверка этих3 chooser cases и ранее добавленного modal
Cancel→Retry на точном будущем candidate SHA — NOT_RUN. REVIEW-03 остаётся
отдельной следующей задачей только по новой команде. Другие пункты сейчас не
исправлялись. Ничего не публиковать, полный CI не запускать; остановиться.

Оценка v1.0 и веса ниже сохранены: функциональность продукта в этой микрозадаче
не добавлялась, новых Windows/пакетных/целевых доказательств нет, прирост0.
Реализация: 58–80/100 · Проверенная готовность: 22/100 · Изменение: +0 · Главный блокер: F31-R11 — подготовка выбранного ключа не подключена к SSH start/recovery.

## Оценка готовности v1.0 — базовая точка, 22.10.2026 00:38 МСК

**Реализация: 58–80/100. Проверенная готовность: 22/100. Изменение: +0.**
Это два разных индекса, а не доля закрытых строк реестра. Они не заменяют
PASS/FAIL/PARTIAL/BLOCKED/NOT_RUN/TODO и обязательные критерии приёмки.
Все31 основных пункта пока PARTIAL; это не означает автоматически «половина».
В этом проходе код не менялся, тесты/CI не запускались, публикации не было.

### Снимки и границы доказательств

- **B — опубликованный source:** `656b3d65e03d540be859ee69635c492631093446`,
  tree `5f9e6589ee195b8a584a131e0ed66a9dbedbd545`,
  [PR88](https://github.com/rkhnorkhan-bit/ProGo/pull/88).
  Последнее подтверждённое состояние PR — OPEN/DRAFT, в main не интегрирован.
- **M — реально проверенный Windows merge:**
  `7e204c7659cefd8142bb33a3cbfc7cc0ec974bc2`,
  [CI586](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39670153699),
  Windows job `119465588823`, Ubuntu job `119465589235`.
  В этой микрозадаче read-only повторно прочитаны jobs и Windows log:
  оба jobs completed/success. В логе build identity
  `0.2.2+7e204c7659cefd8142bb33a3cbfc7cc0ec974bc2` (20:25:38 UTC).
  Указанные ниже native результаты относятся именно к M в CI586 для B;
  это не утверждение о прогоне любого последующего diff или final installer.
- **L — текущий локальный diff поверх B:**12 modified +4 untracked, без staging.
  Его реализация учитывается только как source evidence в диапазоне I;
  Windows compilation/runtime **NOT_RUN**, приращение проверенной готовности0.
  CI586 не проверяет L. Фиксация L без самого изменяемого PROJECT_STATE:
  SHA256 JSON-манифеста15 путей/хэшей
  `73fc808325dafdc80a5f40f2983c811a411a261017f567b840a0f0e13e2e1ade`.
  Ключевые source SHA256:
  `DiagnosticPreview.cs=d408afd28a707fb52e7f4837c6bcce79a040b040e10fe63bba33db120ed700db`,
  `DiagnosticReport.cs=a48223671f45d74152f877d3aeeddaa673937af70099718a3e17604f94f3e563`,
  `SshAuthenticationPreparation.cs=050b76d82f226edb99a2e5196389c60323981845c59b02e3fc999d0858438f86`.
  Наличие этого компонента не подтверждает подключение к SSH start/recovery.
- В CI586 Pack release zip завершён SUCCESS; лог20:47:53 UTC сообщает ZIP SHA256
  `d7a6176ec2da62a5b53e2faea2a86893919a853353a5166f669baa0042896191`.
  Это ZIP на runner, не переданный и не установленный владельцем пакет.
  Upload steps skipped/public artifacts OFF; в последней проверке artifacts=[];
  приватная передача, clean setup/update/rollback и целевой ПК **NOT_RUN**.
- Источники критериев: [APPROVED_PLAN_31.md](APPROVED_PLAN_31.md),
  [SSH_AGENT_AUTOSTART_SPEC.md](SSH_AGENT_AUTOSTART_SPEC.md),
  [project_state.json](project_state.json),
  [RESIDUAL_RECHECK_20261021.md](RESIDUAL_RECHECK_20261021.md),
  source review и REVIEW-01 выше/ниже в этом документе.
  Residual recheck выполнен ранее на `d0f9a6902345eab288b25e8f902df1514db3f2ca`;
  отсутствие последующего исправления указано в текущем реестре, это source
  evidence, не новое native воспроизведение. Полные две исходные ChatGPT-истории
  недоступны; восстановленные требования не выдаются за прочтение всех чатов.
  В момент базовой оценки REVIEW-03 ещё фиксировала устаревшие CI metadata;
  использованы более поздние фактические jobs/logs. Теперь metadata исправлены
  текущим checkpoint REVIEW-03 выше; баллы базовой оценки от этого не повышаются.

### Неизменяемая методика и веса

Каждый из31 основных ID имеет один вес W; сумма W=100. Его вложенные55 ID
ограничивают выполнение родительского критерия, но собственного веса не получают.
Они есть у F16(4), F17(5), F19(5), F22(4), F23(7), F27(4), F30(12), F31(14).
Это декомпозиция остатка, а не исчерпывающая замена всех родительских требований.
Исторические23/31,6/55 и количество assertions/строк/коммитов в формулу не входят.

Большие веса получают сценарии соединения, возврата настроек, сохранности данных,
обновления и готовности SSH. F23=7, F31=8 — их отказ мешает безопасно получить
и использовать результат. F30=8 сохраняет значимость согласованного полного
коммерческого контура даже при большом остатке; он не исключён ради роста оценки.
Навигация, стиль и документация имеют меньший самостоятельный вес.
Пересекающийся код не создаёт дополнительных баллов: F02 оценивает retry,
F04 — истинность статуса, F05 — базовый SSH/SOCKS вход, F31 — дополнительные
agent/key/portability/performance критерии; F07 retention, F08 restore,
F22 перенос/repair, F23 setup/update имеют отдельные пользовательские результаты.

**Реализация I = Σ W × r.** r — экспертная ступень по существующим source и
пользовательскому маршруту:0 — функциональной реализации нет;0.25 — отдельные
работающие компоненты при отсутствии основного согласованного контура;
0.50 — основной контур существует, существенные обязательные части отсутствуют;
0.75 — основной пользовательский маршрут и значительная обработка отказов есть,
полнота остатка ещё не подтверждена;1 — весь согласованный code-критерий доказан.
Это грубые функциональные ступени, не измеренная доля функций с точностью до1%.
Если trace неполон, указывается интервал ступеней: нижняя — обоснованный минимум,
верхняя — допустимая полнота уже существующего кода, которую ещё надо доказать.
Ненаписанные функции верхней границей не оплачиваются; известный отсутствующий
существенный контур ограничивает r сверху. Непроверенный L может дать source
основание для I, но не Windows PASS. Край1 в интервале не присваивает требованию
PASS: он отражает неопределённость полноты, а не подтверждённую полноту.

**Проверенная готовность Y = Σ W × (0.30D +0.20P +0.50A).**
Это консервативный индекс заработанных доказательств для B/M; L не наследует
успех CI586. Значения этапов:

| Этап |0 |0.5 |1 |
| --- | --- | --- | --- |
| D — функциональные dev-проверки | Нет релевантного доказательства/NOT_RUN | Конкретная часть критерия проверена native Windows на точном SHA | Весь dev-критерий с обязательными положительными/отрицательными случаями доказан |
| P — проверочный пакет | Нет доказанного payload соответствующего контура | Сборка с identity, составом runtime и ZIP hash на точном SHA | Полученный пакет проверен по hash/составу, clean dev Windows setup/update/rollback пройдены |
| A — установленный реальный сценарий | NOT_RUN либо нет принятого результата | Основной сценарий на точном установленном кандидате принят, часть обязательных случаев остаётся | Весь согласованный реальный сценарий, отказы/отмена/восстановление приняты |

Общий зелёный CI сам по себе не даёт D=1. ZIP на runner не даёт P=1.
Для F19, F27, F30 пока D=P=A=0: наличие общего EXE не доказывает упаковку
отсутствующего preflight/signing/commercial контура. Для остальных строк
есть приведённая ниже конкретная native проверка M и общий build/runtime/ZIP
из CI586: D=0.5, P=0.5, A=0, то есть Y=W/4. Это частичный зачёт существующего
сценария, а не успешная проверка его известного остатка. Ни по одному ID
реальная установка/приёмка A не начислена. NOT_RUN даёт0 баллов за этап,
сохраняет статус NOT_RUN и не превращается в FAIL.

Веса и этапные доли фиксируются как v1.0. Менять их ради увеличения индекса
нельзя. Изменение согласованного объёма требует явного решения владельца,
новой версии методики и отдельного пересчёта прежней базы.
При смене кандидата доказательства затронутых частей перепроверяются; Y может
снизиться. Старое доказательство хранится со своим SHA, не переносится на L.

### Матрица начислений:31 родителей,100 баллов

Все исходники в столбце «Основание I» относятся к B, кроме явно отмеченного L.
Критерии каждого ID — одноимённая строка APPROVED_PLAN_31 и его дочерние ID
в JSON. Сокращённый native пример в последнем столбце однозначно находится
в Windows log CI586/M; время UTC21.10.2026. Он подтверждает только часть D.
Десятичные начисления служат проверке арифметики, не заявлению точности оценки.

| ID / сценарий | W | r → вклад I | Вклад Y | Основание I и существенный остаток | Native evidence M, частичный D |
| --- | ---: | --- | ---: | --- | --- |
| F01 CLI/Codex |5 |0.75–1 →3.75–5 |1.25 | `CliProxyEnvironmentService`: общий CLI/миграция есть; итоговый пакет и обычный запуск владельца не приняты |20:32:32.173 `CLI migration preserves unrelated preferences` |
| F02 Повторы |4 |0.75–1 →3–4 |1 | `AutomationPlan`, `ProxyService`: retry/fallback/cancel есть; полная итоговая матрица не принята |20:28:53.829 `automatic fallback advances through permanent and transient failures without retrying refused candidates` |
| F03 Один экземпляр |3 |0.50–0.75 →1.5–2.25 |0.75 | `ApplicationInstance`, `MaintenanceOperation`: owner/handoff есть; полный installer ещё не включён |20:40:11.757 `restarted application retains single-instance ownership after handoff` |
| F04 Статус связи |3 |0.75–1 →2.25–3 |0.75 | `ConnectionHealthMonitor`: listener/SOCKS/egress разделены; итоговый реальный маршрут не принят |20:28:03.722 `asynchronous startup refuses a foreign non-SOCKS listener` |
| F05 SSH-вход |5 |0.75–1 →3.75–5 |1.25 | `ProxyService`, `SshInteractiveLogin`: запуск/ожидание SOCKS/первый вход; настоящий VPS NOT_RUN; дополнительная key readiness относится к F31 |20:27:46.940 `asynchronous slow startup shares one process and waits for SOCKS` |
| F06 Возврат proxy |5 |0.75–1 →3.75–5 |1.25 | `CliProxyEnvironmentService`, `WindowsProxyService`: baseline/journal/условный возврат; installer/uninstall и целевой профиль не приняты |20:32:42.684 `failed actual native move conditionally restores its own environment, typed Windows values and exact file bytes` |
| F07 Retention |3 |0.75–1 →2.25–3 |0.75 | `BackupRetention`, `BackupService`: preview/owned deletion/retention есть; единый final installer ещё не принят |20:30:59.042 `Backup retention tests PASS: 29` (suite результата, не29 баллов) |
| F08 Restore |4 |0.75–1 →3–4 |1 | `BackupIntegrity`, `Restore-ProGoBackup.ps1`: staging/rollback/cancel ownership; старый FAIL не объявляется текущим; final package NOT_RUN |20:32:59.168 `pending restore X requests cancellation and retains its native owner until the real hash operation settles` |
| F09 PIN/vault |4 |0.75–1 →3–4 |1 | `VaultService`, `VaultDailyStore`, `VaultOpenService`: утверждённый PIN/ложный vault реализован; итоговая Comfort/midnight/I/O приёмка не полна |20:25:43.380 `wrong-pin read leaves source bytes and file set unchanged` |
| F10 HTTP bridge |4 |0.75–1 →3–4 |1 | `CliProxyBridgeService`, `HttpConnectProxyServer`: consumers/bridge/lifecycle есть; реальная цепочка SSH→SOCKS→HTTP не принята |20:32:32.076 `Windows uses tested HTTP/CONNECT bridge` |
| F11 Навигация |2 |0.75 →1.5 |0.5 | `AppCommand`, tray context: общие команды есть; согласованные новые экраны кабинета отсутствуют, локальная диагностика требует Windows регрессии |20:32:34.807 `real tray command dispatches its expected dialog: Settings` |
| F12 Область Stop |4 |0.50–0.75 →2–3 |1 | tray context/`HomeVpnService`: PC/phone разделены, но phone Stop/exit ownership F31-R10 остаётся FAIL |20:33:03.412 `actual production three-second shutdown refusal keeps the native UI ticking while its restore reader remains owned` — restore owner, не phone Stop |
| F13 Маршрут |2 |0.75–1 →1.5–2 |0.5 | `RouteTester`: старт/отмена/источник результата; итоговый live маршрут NOT_RUN |20:38:56.634 `Escape closes the actual modal route dialog while measurements run` |
| F14 Диагностика |2 |0.75–1 →1.5–2 |0.5 | `SshProfileDiagnostics`, `DiagnosticProcess`: фон/deadlines/cancel есть; aggregate L и modal Cancel→Retry L Windows NOT_RUN |20:38:55.911 `diagnostic workers preserve UI heartbeat after the caller loses its synchronization context` |
| F15 Настройки |3 |0.75–1 →2.25–3 |0.75 | `SettingsService`, `SshProfilesSettingsForm`: atomic save/rollback; установленная итоговая версия не принята |20:32:41.088 settings suite PASS93; native память/диск проверены в CI, не на ПК владельца |
| F16 DPI/layout |2 |0.50–0.75 →1–1.5 |0.5 | `MainWindow`, forms/`Program`: часть адаптивной раскладки; весь набор форм/installer и monitor DPI ещё не завершён |20:34:26.022 `scaled cleanup list and actions do not overlap`; synthetic scale не заменяет реальные DPI100/125/150/200 |
| F17 Доступность |2 |0.50–0.75 →1–1.5 |0.5 | `UiTheme`, native forms: часть keyboard/name/state; новые формы, Narrator/High Contrast NOT_RUN |20:38:56.452 `route measurements complete Tab reaches Button Закрыть` |
| F18 Выдача |2 |0.75–1 →1.5–2 |0.5 | `HomeVpnWizardForm`: issued/installed разделены; установка телефона и интернет NOT_RUN |20:37:27.267 `wizard accessible issuance result never claims installation` |
| F19 Preflight сети |3 |0.25–0.50 →0.75–1.5 |0 | `HomeVpnWizardForm`, `HomeVpnService`: сетевые компоненты/подсказки есть; согласованный NAT/CGNAT/UDP verdict контур не доказан; остаток F19-R01…R05 |Релевантная полная preflight интеграция не доказана; D/P/A0, не общий CI PASS |
| F20 Друзья |2 |0.75 →1.5 |0.5 | `HomeInvitationsForm`, `server/home_vpn_setup.py`: selected revoke/reissue; связь со сроками/тарифами F30 не реализована полностью; live API не принят |20:37:29.887 `native friends Down distinguishes revoked duplicate and names repeat-revoke effect` |
| F21 Clipboard |1 |0.75–1 →0.75–1 |0.25 | `ClipboardService`: owned secret copy/cleanup; итоговые busy/exit сценарии не полностью приняты |20:43:01.811 `secret copy writes current clipboard`;20:43:06.915 logs do not contain secrets |
| F22 Перенос/repair |4 |0.50–0.75 →2–3 |1 | `BackupService`, `HomeVpnPortableTransfer`: защищённый перенос есть; `Repair-ProGo.ps1` F22-R03 source FAIL, другой SID/ПК NOT_RUN |20:46:38.740 `a new real HomeVpnService loads clean imported data without activating SSH or relay`; это не repair PASS |
| F23 Update/setup |7 |0.25–0.50 →1.75–3.5 |1.75 | `UpdateLauncher`, `Update-ProGo.Core.ps1`: updater/rollback есть; standalone setup, installer fallback и release UID/asset контур не завершены |20:31:26.805 `finally requests helper progress clearance even after rollback or host failure: hash-failure`; это не setup acceptance |
| F24 Помощь/версия |1 |0.75 →0.75 |0.25 | `HelpForm`, `README.ru.md`: помощь есть, известная устаревшая CI/acceptance metadata ограничивает верхнюю границу |20:36:09.255 `help Начало820 exposes a focusable read-only instruction pane` |
| F25 Logs/export |2 |0.50–0.75 →1–1.5 |0.5 | `BoundedLog`, `DiagnosticReport`, `DiagnosticPreview`: limits/redaction/preview B, worker build/save + modal fix L; SafeLog/viewer UI I/O остаётся |20:30:26.285 bounded log suite PASS32;20:30:57.110 diagnostic report suite PASS29; L Windows NOT_RUN |
| F26 Автозапуск |1 |0.75–1 →0.75–1 |0.25 | `ApplicationShortcuts`, installer script: отдельные настройки есть; final installer и реальный Windows logon NOT_RUN |20:30:56.448 `startup runs quietly without changing SSH preferences` |
| F27 Подпись/AV |2 |0 →0 |0 | `.github/workflows/release.yml`: signing/verification/timestamp pipeline отсутствует; сертификат/Defender/Kaspersky NOT_RUN |D/P/A0; подпись не выводится из успешной unsigned сборки |
| F28 Команды/отчёт |1 |0.50–0.75 →0.5–0.75 |0.25 | `AppCommand`, `Test-ProGo.ps1`: каталог есть; раздельная отчётность suites/scenarios/assertions не завершена |`DesktopUiWorkflowTests.CommandCatalogContract`: unique round-trippable command IDs;20:35:03.163 diagnostic command opens review without implicit Enter export |
| F29 Стиль/иконка |1 |0.75–1 →0.75–1 |0.25 | `BrandIcon`, `UiTheme`: принятая иконка/стиль; все состояния новых экранов и итоговые screenshots ещё не приняты |20:38:07.907 built icon valid ICO header/all seven resolutions;20:38:07.912 PNG corners16 |
| F30 Кабинет/billing |8 |0.25 →2 |0 | `server/profile_share.py`, friends/reissue и отдельные server limits — компоненты; полный кабинет/roles/orders/payment/mail не реализован, есть внешние зависимости |D/P/A0: QR/issuance тест не считается коммерческой end-to-end проверкой |
| F31 SSH/скорость |8 |0.50–0.75 →4–6 |2 | `SshAgent*`, `SshProfile*`, `ProxyService`: agent status/start, key inspection, IPv6/fallback есть; common fresh key preparation F31-R11 TODO, UI I/O/phone Stop FAIL; aggregate L пока advisory |Signing55/UI50 и service-start62 в M;20:28:22.833 `raw and bracketed IPv6 start a native child and complete SOCKS CONNECT`; encrypted key/VPS/другой ПК NOT_RUN |
| **Итого** |**100** |**58.00–79.25** |**21.75** |**Показать I с округлением наружу58–80, Y с округлением до22** |**Ни одного A>0; Windows L NOT_RUN** |

Разброс I обусловлен неполной трассировкой всех критериев к существующему коду,
незавершёнными специфицированными контурами и непроверенным L. Подмена его точкой
середины создала бы ложную точность. Y=21.75 — сумма явно заработанных частичных
этапов по этой методике, отображаемая как22; это не «22% тестов прошло» и не
разрешение эксплуатации. Для текущего L нет самостоятельного Windows балла:
22 описывает доказательства B/M, а готовность нового изменённого блока NOT_RUN.

### Критические блокеры независимо от суммы

1. **F31-R11:** общий свежий этап подготовки конкретного выбранного ключа перед
   `StartSingleTunnel` и recovery ещё не подключён. Public key match/доступный
   ssh-agent не доказывают разблокировку private key или принятие сервером.
   Реальный encrypted-key/agent и SSH→SOCKS5→HTTP на разрешённом VPS NOT_RUN.
2. **F31-R08/R09/R10:** source FAIL для оставшихся SafeLog/viewer/helper UI I/O,
   private HomeVPN disk/DPAPI/session preparation и phone Stop/exit ownership.
   Worker preview/save L закрывает лишь часть R08; Windows регрессия L NOT_RUN.
   Фактическое зависание/утечка на ПК владельца этим не утверждается.
3. **F22-R03/F23:** repair пропускает12 из41 обязательных runtime paths, допускает
   неполный payload и direct replacement без общей транзакции; final standalone
   installer, подходящий fallback при неприменимом patch и его приёмка не готовы.
   Сохранность профиля/ключей при полном конечном обновлении пока не принята.
4. **Кандидат и приёмка:** L не скомпилирован/не проверен Windows, PR не интегрирован,
   пакет владельцу не передан и не установлен. Startup/UI/profile/key/cancel/
   recovery/update/rollback на целевом ПК NOT_RUN. Согласование приватной передачи
   требуется до использования публичных CI artifacts; сейчас ничего не передаётся.
5. **Полный согласованный объём:** F27 signing/verification отсутствует, реальные
   cert/AV проверки NOT_RUN; F30 коммерческий контур в основном отсутствует,
   провайдерные решения/интеграции BLOCKED. Это блокеры окончательного завершения
   всего объёма, а не новый запрет разрешённого unsigned тестового кандидата.

### Три ближайших шага — только по отдельной команде

1. **REVIEW-02:** ограничить native SaveFileDialog fixture своим deadline и owned
   cleanup. Критерий: отсутствующее/неподходящее окно даёт ограниченный по времени
   FAIL, positive chooser Cancel сохраняется, fixture не оставляет свои окна/tasks.
   Не увеличивать общий CI timeout и не исправлять другие функции в этой задаче.
2. **REVIEW-03:** синхронизировать completed CI586 metadata в JSON/runbook с B/M,
   ссылкой и jobs; L явно NOT_RUN. Критерий: нет двух конкурирующих текущих
   CI-статусов, сохранены86 ID и история. Само исправление метаданных не повышает Y.
3. **Целевая Windows-регрессия отдельного диагностического кандидата:** сначала
   зафиксировать состав и точный candidate SHA, затем отдельным разрешённым
   запуском проверить ShowDialog Cancel→Retry, chooser bounds и прежние
   close/Dispose/single-flight/installed helper сценарии. Критерий: результаты
   именно этого SHA, bounded cleanup и отсутствие stale publication; не общий
   старый CI SUCCESS. Если подготовка/запуск крупнее одной микрозадачи, разделить;
   долгий запуск закончить сообщением ID, результат читать следующей командой.
   Этот шаг даёт доказательство только диагностическому блоку, не всему F31.

### Учёт изменений и завершение этой микрозадачи

Это первая оценка v1.0, сопоставимой предыдущей базы нет: **Изменение +0**.
В дальнейших отчётах Δ означает изменение заработанного Y по той же методике
от предыдущей оценённой точки (расчёт до округления); изменение интервала I
описывается отдельно. Только новое релевантное доказательство на точном SHA
может дать прирост Y. Новый код без проверки, NOT_RUN, новая строка отчёта,
количество тестов и пересчёт весов приростом не считаются.

В этой микрозадаче разрешено изменить только PROJECT_STATE.md. Завершены локальные проверки документа:
PASS арифметика31 уникального веса (сумма100; I58–79.25; Y21.75),
PASS86 ID реестра (31+55), PASS `git diff --check -- docs/PROJECT_STATE.md`,
PASS побайтовая неизменность остальных15 modified/untracked файлов по исходному
SHA256-манифесту. HEAD и пустой staged index не изменились. Это не Windows
функциональные тесты. Read-only PR проверка подтвердила OPEN/DRAFT, merged=false,
head=B и merge=M; старое описание PR о pending CI не подменяет jobs CI586. Ничего не публиковать, CI не запускать,
к следующей задаче не переходить.

Формат конца дальнейшего отчёта:
«Реализация: 58–80/100 · Проверенная готовность: 22/100 · Изменение: +0 · Главный блокер: F31-R11 — подготовка выбранного ключа не подключена к SSH start/recovery».
Числа обновлять только по этой методике и доказательствам; для первого отчёта
использовать указанную базу. Остановиться и ждать следующей команды владельца.

## REVIEW-01: modal Cancel → Retry исправлен в source, Windows NOT_RUN

22.10.2026 00:21 МСК. Выполнена ровно одна разрешённая микрозадача: отмена диагностики
в ShowDialog и её modal regression. HEAD остаётся
`656b3d65e03d540be859ee69635c492631093446`; новые изменения не коммитились,
не публиковались и не проверялись полным CI. CI586 не проверяет этот diff.

Изменены только3 файла относительно начала этой микрозадачи:

- `src/DiagnosticPreview.cs`: после Form.CancelButton assignment явно установлен
  `close.DialogResult = DialogResult.None`. Обычный busy Cancel/Esc вызывает
  RequestCancellation без implicit modal close. Idle Close по-прежнему вызывает
  CancelAndClose → CloseSettled. Title-close, shutdown/Dispose и active-operation
  identity/single-flight guards не менялись.
- `tests/DesktopAsyncDiagnosticPreviewTests.cs`: добавлен
  `AsyncDiagnosticModalCancelRetry`,4 вызова в существующем suite — build/save
  × click/Esc. Каждый случай действительно открывает `form.ShowDialog()`.
  Первый build уже прочитал прежний журнал, но удержан перед owner publication;
  отменённый результат не принимается. После settlement retry в том же окне
  читает изменённый журнал и выдаёт только свежие события. Save-вариант сохраняет
  исходный reviewed snapshot после изменения журнала. Во время обеих фаз duplicate
  requests возвращают тот же Task; старый Task завершён до нового native I/O.
  Результат повторно сверяется через несколько modal message-loop turns, затем
  explicit idle Close действительно закрывает окно.
- `docs/PROJECT_STATE.md`: эта точка продолжения; остальная работа сохранена.

Новый test fixture: собственный8с modal deadline на UI timer, каждый controlled
worker barrier ждёт не более3с. Timer exception освобождает оба barrier и dispose
точно своей формы. В finally timer останавливается, barriers освобождаются, форма
dispose, все first/retry/current/build/save Tasks объединяются и ожидаются без UI
pump с пределом5с. Это ограничение контролируемого fixture, не обещание принудительно
прервать зависший OS filesystem call. Чужие окна/процессы не затрагиваются.

Результат REVIEW-01 — **PARTIAL: source fix + regression added; Windows NOT_RUN**.
В доступной Linux-среде нет dotnet/csc/mcs/mono/pwsh/Windows Forms. Нативные4 новых
сценария, C# compilation и существующие title-close/shutdown/Dispose regressions
не запускались; их PASS не заявляется. Новые функции и SSH launch integration
не добавлялись.

Доступные целевые проверки:

- PASS `git diff --check`; только прежние LF→CRLF notices в двух других файлах.
- PASS `python scripts/check_public_content.py`:315 source,0 release files.
- PASS статическая проверка wiring: четыре варианта нового метода вызываются
  существующим suite; в методе ShowDialog, deadlines и finally cleanup; это
  source check, не выполнение WinForms.
- PASS targeted secrets/PII scan119 добавленных строк production/test, findings0.
- PASS scope guard:13 остальных modified/untracked файлов побайтно совпадают с
  началом микрозадачи. В частности, REVIEW-02 chooser method, REVIEW-03 runbook
  и JSON остались без исправлений. HEAD и пустой staged index не менялись.

Проверенный source SHA256 `DiagnosticPreview.cs`:
`d408afd28a707fb52e7f4837c6bcce79a040b040e10fe63bba33db120ed700db`.
Проверенный test SHA256 `DesktopAsyncDiagnosticPreviewTests.cs`:
`fd4b028149f0fe66e20a6cf04c7085536d1ba8a5ecfe5d3d20a0518b36298304`.

Остаток, сейчас не выполнять:

1. REVIEW-02 — ограничить ожидание native SaveFileDialog cancellation fixture
   собственным deadline/owned cleanup; отдельная предлагаемая следующая микрозадача.
2. REVIEW-03 — синхронизировать completed CI586 metadata в runbook и JSON,
   сохранив этому локальному diff NOT_RUN.
3. Для REVIEW-01 необходима отдельная разрешённая Windows-проверка на точном
   будущем candidate SHA; до неё native acceptance остаётся NOT_RUN.

Публикация, полный CI и переход к следующей задаче в этом проходе не разрешены.
Остановиться и ждать следующей команды владельца.

## История: ревью16 незакоммиченных файлов — завершено с находками

22.10.2026 00:05 МСК. Разрешённая единственная микрозадача: source review12 tracked modified
и4 untracked относительно HEAD `656b3d65e03d540be859ee69635c492631093446`.
HEAD/ветка не менялись. Просмотрены diff12 файлов и полный текст4 новых файлов,
связанные existing helper/API ровно для проверки их совместимости.
В этой микрозадаче изменён только этот PROJECT_STATE.md; source/tests/другие
документы сохранены побайтно. Commit/push/PR mutation/full CI не выполнялись.

**Итог: source review завершено, безусловный CLEAR не выдан.** Подтверждены
две source/control-flow находки и рассинхронизация документов. Исправления
не выполнялись. Windows compile/runtime для локального блока **NOT_RUN**:
в доступной Linux-среде нет dotnet/csc/mcs/mono/pwsh/Windows Forms.
CI586 SUCCESS относится только к published source656b3d65 и tested merge
7e204c7659cefd8142bb33a3cbfc7cc0ec974bc2, не к этому рабочему diff.

### REVIEW-01 — P2: Cancel превращается в закрытие модального окна

- Файл/символы: `src/DiagnosticPreview.cs`, constructor (`CancelButton = close`,
  сейчас108), close Click handler (102), `OnFormClosing` (246),
  `RunOperation` owner finally → `CloseSettled` (193).
- Доказательство: UiTheme создаёт Button с DialogResult.None; setter Form.CancelButton
  в .NET Framework автоматически меняет его на Cancel. Локальный код не сбрасывает
  это значение после назначения. При ShowDialog нажатие кнопки/Esc устанавливает
  результат формы; modal close вызывает OnFormClosing, который при busy задаёт
  closing/closeAfterWork. После settlement окно закрывается. Это вывод по исходникам
  проекта и официальному framework contract; native воспроизведение **NOT_RUN**.
- Влияние: действие «Отменить операцию» лишает пользователя повтора в том же окне,
  хотя новый сценарий и статус предлагают retry. При отмене уже выполненной записи
  подтверждение результата также исчезает вместе с окном. Преждевременное освобождение
  native I/O или потеря исходных журналов этим выводом не утверждаются.
- Воспроизведение: открыть actual tray ExportDiagnostics → ShowDialog, удержать build
  или save на существующем beforeIo barrier, нажать «Отменить операцию»/Esc,
  обработать modal messages, отпустить barrier. По коду окно закроется вместо retry.
- Пробел тестов: `AsyncDiagnosticBuildCancelRetry` и `AsyncDiagnosticSaveCancelRetry`
  вызывают `form.Show()`, поэтому implicit modal DialogResult не проверяется.
  Context modal tests покрывают shutdown/Dispose, а не обычный Cancel+Retry.
- Минимальное исправление: нейтрализовать автоматический button DialogResult после
  назначения CancelButton; явное idle Close, title-close и CancelAndClose сохранить.
- Проверка исправления: настоящий ShowDialog, held build/save, click и Esc; после
  settlement окно остаётся открытым, доступен explicit retry с тем же snapshot;
  title-close/shutdown/forced Dispose по-прежнему закрывают с сохранением ownership.

Framework evidence:
[.NET Framework Form.cs — CancelButton и CheckCloseDialog](https://referencesource.microsoft.com/System.Windows.Forms/winforms/Managed/System/WinForms/Form.cs.html),
[Microsoft Button.DialogResult — ShowDialog contract](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.button.dialogresult?view=netframework-4.8.1).

### REVIEW-02 — P2: native chooser fixture не ограничивает собственное ожидание

- Файл/символ: `tests/DesktopAsyncDiagnosticPreviewTests.cs`,
  `AsyncDiagnosticNativeChooserCancel` (259).
- Доказательство: timer при `FindWindow(...) == IntPtr.Zero` постоянно возвращает;
  deadline/watchdog отсутствует. `PerformClick` синхронно входит в production
  ChooseSavePath → SaveFileDialog.ShowDialog; последующий PumpAsyncRestore ещё
  не выполняется. Ограничение всего CI job не ограничивает этот fixture.
- Влияние: если caption/class matching не сработает, тестовый диалог остаётся открытым
  и общий Windows suite может ждать до job timeout. Это условный source-confirmed
  путь без лимита; реальный сбой поиска окна на Windows **NOT_RUN**, не заявлен.
- Воспроизведение для будущего regression: намеренно не находить native dialog при
  настоящем ShowDialog; ожидание не должно удерживать весь Desktop harness.
- Исправление/проверка: собственный deadline с точным владением test helper/dialog,
  bounded cleanup и явным FAIL при отсутствии окна; сохранить реальную native chooser
  проверку и отдельный негативный сценарий. Не увеличивать общий CI timeout.

### REVIEW-03 — P2: CI586 metadata не согласованы с завершённым прогоном

- Файлы/символы: `docs/ACCEPTANCE_RUNBOOK.md` opening candidate block;
  `docs/project_state.json`: ci_candidate.status/acceptance,
  fresh_candidate_verification, remote_visibility.ci и requirements[].integration.
- Доказательство: runbook пишет «Windows выполняется», JSON содержит in_progress /
  NOT_RUN для CI586; предыдущая точка восстановления уже установила completed SUCCESS
  с точными source/merge/jobs. Structural check этого противоречия не исправляет.
- Влияние/воспроизведение: открыть эти документы рядом с completed-CI586 checkpoint —
  читатель получает два разных текущих статуса. Исторические FAIL583–585 сохраняются.
- Исправление/проверка: отдельная небольшая синхронизация только metadata опубликованного
  tested scope; сохранить локальному diff NOT_RUN,86 ID и package/target gates.
  В текущей микрозадаче эти файлы не изменялись.

### Проверенные границы и доступные результаты

- DiagnosticReport сохраняет источники/allowlist/ограничения; добавленные token checks
  не поглощают OperationCanceledException. Worker Build/save используют reviewed
  snapshot; native chooser/clipboard остаются owner UI. Context удерживает точный
  Work до settlement, включая disposed/reentrant paths. Кроме REVIEW-01 новых
  подтверждённых production дефектов в просмотренном diff не выявлено.
- SSH request/result копируют candidate/config/identity list; exact canonical executable
  и endpoint проверяются, общий budget и прежний signing cap разделены. Нет mutating
  ssh-add, новых hard gates или автоматического выбора первого из нескольких ключей.
  Подпись остаётся local observation; повторный -G не является private-key/server proof.
  Launch/recovery integration явно TODO; его отсутствие не скрыто как готовность.
- Native suites содержат реальные file/WinForms/OpenSSH/test-owned agent integrations
  плюс отдельные deterministic seams. Существующие assertions сохранены; async save
  ожидание добавлено до прежних assertions. Три новых suite зарегистрированы и
  вызываются по одному разу; это static wiring check, не compilation/runtime PASS.
- PASS: `git diff --check` (только LF→CRLF notices в2 файлах).
- PASS: `python scripts/check_public_content.py` —315 source,0 release files.
- PASS: targeted secrets/PII scan1549 добавленных/новых строк во всех16 файлах,
  findings0; новые файлы проверены целиком, включая fixture literals.
- PASS: JSON parse/schema shape —86 уникальных ID=31+55; IDs/statuses совпадают с HEAD,
  обязательные verification/integration/package/target/remaining/dependencies поля есть.
  Семантическое metadata расхождение остаётся REVIEW-03.
- NOT_RUN: C# compilation, DiagnosticReport native harness, новые12 form/3 Context/
  auth aggregate scenarios, installed PowerShell fresh-process suite и реальные Windows
  regressions. Полный Python suite/full CI не запускались как не относящиеся к этой
  ограниченной source-review задаче. Source review не заменяет эти проверки.

Review snapshot15 файлов кроме изменяемого PROJECT_STATE:
SHA256 sorted path→SHA256 JSON manifest
`535c9fc55af868e9003c18095733fae052b46c6d0914cd273ad3749375f6b33c`.
Все16 путей перечислены в историческом checkpoint ниже; новых source файлов
в ходе ревью не создавалось. Итоговая приёмка31/55 не пересчитывалась.

**Предложенная следующая единственная микрозадача, сейчас не выполнять:**
исправить REVIEW-01 — обычный Cancel+Retry настоящего модального diagnostic preview.
Проверяемый результат: нейтрализован implicit DialogResult, добавлен modal regression
на отмену build/save и retry; сохранены title-close/shutdown/Dispose ownership guards.
Никаких других функций, правок SSH launch, публикации или полного CI в этом блоке.
При недоступном Windows native результат остаётся NOT_RUN. REVIEW-02/03 отдельные
остатки; переходить к ним автоматически запрещено. Ждать команды владельца.

## История: восстановление после остановки владельцем — STOP

Проверено 21.10.2026 23:51 МСК. Историческая точка после прерывания;
дальнейшая разработка остановлена по прямой команде владельца. Восстановление
ограничено чтением процессов/Git/GitHub и сохранением этого раздела; новых
коммитов, push, PR mutations, запусков или повторов CI не выполнялось.

### Процессы и Git

- Активных дочерних агентов нет. Локальных ProGo/build/test/git процессов в
  snapshot `ps` нет; обнаружены только службы среды и текущая проверка.
  Процессы не завершались. Проверка не описывает процессы на ПК владельца.
- Ветка `work/progo-ci586-20261021`, HEAD
  `656b3d65e03d540be859ee69635c492631093446`, tree
  `5f9e6589ee195b8a584a131e0ed66a9dbedbd545`.
  Git index пуст: staged0; tracked modified12, untracked4.
  Merge/rebase/cherry-pick/revert/index.lock markers отсутствуют.
  Существующие worktree сохранены. Ничего не удалено, не сброшено и не откатано.
- Последние опубликованные коммиты: `d7e349d63b4d53eba5b7d98277996a89269a5ce8`
  (ShellLink читает snapshot из того же pinned handle через COM stream) и
  `656b3d65e03d540be859ee69635c492631093446` (CI585 failure scope/residual evidence).
  Git содержит14 опубликованных коммитов после исходной точки `7861ea9`;
  первые четыре логических изменения и последующие CI/разработческие исправления
  сохранены, API trees сверялись с локальными. Последний publication scan:
  336 added lines/6 files, secrets/PII findings0.

### Фактический PR и CI

[PR88](https://github.com/rkhnorkhan-bit/ProGo/pull/88) OPEN/DRAFT, merged=false.
GitHub head совпадает с локальным `656b3d65e03d540be859ee69635c492631093446`;
base main `4bb336575bb1216d3047bd2b294aeecad9fdd65a`.

[CI586 /39670153699](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39670153699)
**completed SUCCESS**: Windows job119465588823 SUCCESS, Ubuntu job119465589235
SUCCESS. Checkout и build identity проверены по завершённому Windows log:
tested merge `7e204c7659cefd8142bb33a3cbfc7cc0ec974bc2`,
`0.2.2+7e204c7659cefd8142bb33a3cbfc7cc0ec974bc2`.

Узкие результаты этого точного tested merge: Desktop4930 PASS; Codex shortcut
ownership67 PASS; settings initial-load preservation93 PASS; selected-key
signing55 и UI50 PASS; agent startup62 PASS; Maintenance126 PASS; Shutdown47
PASS; Home VPN975 PASS; Portable Home VPN90 PASS. Это не приёмка всего31/55,
не проверка незакоммиченных файлов и не установленная программа владельца.
F31-R11 остаётся PARTIAL: общая auth preparation перед own SSH/recovery TODO.

CI583/39660551553, CI584/39663669399, CI585/39667773240 перепроверены:
все completed FAILURE; в этих известных запусках активных jobs не осталось.
Новые CI не запускались. Их неуспехи сохранены как история, CI586 их не стирает.

Pack release zip в CI586 SUCCESS. Версия0.2.2, ZIP SHA256 по runner log:
`d7a6176ec2da62a5b53e2faea2a86893919a853353a5166f669baa0042896191`.
Artifact metadata `[]`; screenshots/diagnostics/release folder/ZIP uploads SKIPPED.
ZIP не скачан и не передан: существование файла на временном runner не означает
доступный проверочный пакет. Публичный release/installer не разрешён;
public uploads OFF, способ приватной передачи ещё не согласован. Установка,
реальный ПК/VPS/маршрут, ACL/службы/системная сеть владельца NOT_RUN.

### Сохранённая незавершённая работа

**Не коммитить и не публиковать автоматически.** Два локальных блока не входят
в CI586. Финальное общее review и Windows compilation/runtime для них NOT_RUN.

- F31-R08: worker Build/save диагностического отчёта, Context single-flight,
  cancellation/shutdown ownership; token overload,12 native form сценариев,
  3 actual Context сценария, адаптация прежних UI/installed helper tests.
  Root-owned Context source review получил CLEAR после двух reentrant Disposed
  race fixes. Полное финальное review всего блока не зафиксировано.
  Logger/open/cold helper compilation остаток этим блоком не закрыт.
- F31-R11 блок1: новый immutable advisory aggregate fresh config/signing,
  общий budget и отдельная native suite. Финальное source review не зафиксировано.
  `ProxyService` aggregate ещё не вызывает; launch reservation/revision lease,
  recovery integration и соответствующий native lifecycle matrix остаются TODO.

Tracked modified12: `docs/ACCEPTANCE_RUNBOOK.md`,
`docs/MICROTASK_SSH_AUTH_PREPARATION_SCOPE.md`, `docs/PROJECT_STATE.md`,
`docs/project_state.json`, `scripts/Test-ProGo.ps1`, `src/DiagnosticPreview.cs`,
`src/DiagnosticReport.cs`, `src/UpdateAwareTrayApplicationContext.cs`,
`tests/DesktopDiagnosticTests.cs`, `tests/DesktopTests.cs`,
`tests/DiagnosticPreviewTests.ps1`, `tests/DiagnosticReportTests.cs`.
Новые untracked4: `src/SshAuthenticationPreparation.cs`,
`tests/DesktopAsyncDiagnosticPreviewTests.cs`, `tests/DesktopDiagnosticContextTests.cs`,
`tests/SshAuthenticationPreparationTests.cs`.

`git diff --check` PASS при восстановлении; только предупреждения нормализации
LF→CRLF в двух ранее изменённых файлах. Новый test run не выполнялся.
`project_state.json` и нижние исторические разделы ещё содержат pending/NOT_RUN
до получения completed CI586; они не являются текущим итогом этого запуска.
Этот раздел имеет приоритет для состояния остановки. Новые55/31 PASS не объявлены.

### Где остановились и следующий единственный шаг

До остановки одновременно ожидался CI586 и продолжалось незавершённое review
локальных диагностического/auth блоков. За время прохода последовательно
исправлялись FAIL предыдущих CI и автоматически начинались следующие блоки.
Последний Windows job длился около22м31с (20:25:25–20:47:56 UTC,
23:25:25–23:47:56 МСК), завершился успешно. Точной разбивки всех заявленных
владельцем1ч29м35с по активной работе и ожиданию нет; единственной доказанной
причины всей длительности установить нельзя. Это не подтверждение зависания CI.

**Только после следующего «продолжай»:** одна микрозадача — синхронизировать
completed CI586 в `docs/project_state.json`. Проверяемый результат: один
согласованный machine-readable checkpoint с точными source/merge/run/jobs,
SUCCESS только для опубликованного tested scope; локальные два блока остаются
NOT_RUN, все86 ID и отдельные integration/package/target слои сохранены.
Никакой реализации, нового CI или публикации в этой микрозадаче.

Новый порядок владельца: одно «продолжай» = одна заранее названная микрозадача
с одним критерием результата; ориентир5–15мин активной работы без обещания срока.
Долгий CI/сборку не ждать бесконечно: записать run ID/status и закончить ответ;
следующая команда проверяет тот же запуск. Новую микрозадачу автоматически
не начинать. Прежние ограничения публикации сохраняются.

## Основные 31 пункта

| ID | Требование | Статус | Реализация / область | Остаток / следующий шаг |
| --- | --- | --- | --- | --- |
| F01 | Общий CLI/Codex | PARTIAL | CliProxyEnvironmentService | Перепроверить на итоговом SHA и пакете: Миграции, один запрос CLI, on/off и итоговые переменные окружения. |
| F02 | Автоматика и повторы | PARTIAL | AutomationPlan; ProxyService | Перепроверить на итоговом SHA и пакете: Временный/постоянный отказ, ручная отмена, успешный повтор без перезапуска. |
| F03 | Один экземпляр | PARTIAL | ApplicationInstance; MaintenanceOperation | Перепроверить на итоговом SHA и пакете: Одновременный запуск, авария owner, handoff, update/restore/install без параллельной записи. |
| F04 | Честный статус связи | PARTIAL | ConnectionHealthMonitor | Перепроверить на итоговом SHA и пакете: Чужой listener, silent SOCKS, egress, NO_PROXY, устаревшие данные и частичный CLI. |
| F05 | Один клик и SSH-вход | PARTIAL | ProxyService; SshInteractiveLogin | Перепроверить на итоговом SHA и пакете: Задержка старта, отказ ключа, один клик CLI, отмена, host trust; настоящий VPS позже. |
| F06 | Возврат proxy-настроек | PARTIAL | CliProxyEnvironmentService; WindowsProxyService | Перепроверить на итоговом SHA и пакете: Типы/отсутствие значений, чужие правки, denied write, повтор очистки, uninstall и installer. |
| F07 | Сохранность копий | PARTIAL | BackupRetention; BackupService | Перепроверить на итоговом SHA и пакете: Preview/Cancel, изменившийся manifest, baseline/pre-update, новая копия и junction. |
| F08 | Транзакционный restore | PARTIAL | BackupIntegrity; Restore-ProGoBackup.ps1 | Поздняя отмена/очистка уже исправлялась. Сохранить результат CI582; новая итоговая регрессия restore/path/reparse/tamper/rollback и приёмка пакета. Старый FAIL без нового воспроизведения не считается текущим. |
| F09 | Настоящий и ложный vault | PARTIAL | VaultService; VaultDailyStore; VaultOpenService | PIN и фоновые vault операции уже реализованы. Нужны регрессия на итоговом SHA/пакете и приёмка установленного кандидата; повторная реализация не требуется. |
| F10 | Жизненный цикл bridge | PARTIAL | CliProxyBridgeService; HttpConnectProxyServer | Перепроверить на итоговом SHA и пакете: CLI/Windows/scoped отдельно, последний consumer, Shutdown(Send), нагрузка, Stop и ошибки cleanup. |
| F11 | Навигация и команды | PARTIAL | AppCommand; UpdateAwareTrayApplicationContext | Перепроверить на итоговом SHA и пакете: Главное окно, трей, настройки, закрытие диалогов, симметричные on/off. |
| F12 | Область отключения | PARTIAL | UpdateAwareTrayApplicationContext; HomeVpnService | Перепроверить на итоговом SHA и пакете: Stop во время Connect, независимость каналов, Quit и подтверждённое завершение. |
| F13 | Проверка маршрута | PARTIAL | RouteTester | Перепроверить на итоговом SHA и пакете: Первый/повторный запуск, закрытие окна, timeout и старый ответ. |
| F14 | Фоновая диагностика | PARTIAL | SshProfileDiagnostics; DiagnosticProcess | Перепроверить на итоговом SHA и пакете: Heartbeat UI, медленная сеть, отмена/timeout, отсутствие поздних изменений. |
| F15 | Save и немедленные действия | PARTIAL | SettingsService; SshProfilesSettingsForm | Перепроверить на итоговом SHA и пакете: Память/диск согласованы; Save/Cancel, ошибка поля, конкурентные изменения. |
| F16 | Адаптивность и DPI | PARTIAL | MainWindow; SshProfilesSettingsForm; Program | Перепроверить на итоговом SHA и пакете: Настоящие Windows DPI 100/125/150/200%, 1366×768, длинный текст, доступные главные действия. |
| F17 | Доступность | PARTIAL | UiTheme; native forms | Перепроверить на итоговом SHA и пакете: Tab/Shift+Tab/Enter/Esc, Narrator и High Contrast в среде разработки; недоступную среду отметить. |
| F18 | Выдача и установка | PARTIAL | HomeVpnWizardForm | Перепроверить на итоговом SHA и пакете: Выдача/отзыв/повтор не дают ложный installed; реальная установка проверяется позже. |
| F19 | Условия подключения | PARTIAL | HomeVpnWizardForm; HomeVpnService | Перепроверить на итоговом SHA и пакете: Стендовые ошибки сети; после установки - DNS, смена сети, idle, большие передачи и телефон. |
| F20 | Управление друзьями | PARTIAL | HomeInvitationsForm; server/home_vpn_setup.py | Перепроверить на итоговом SHA и пакете: Отзыв только выбранного доступа, исходный ID запроса, отмена списка, повтор/ошибка API. |
| F21 | Очистка clipboard | PARTIAL | ClipboardService | Перепроверить на итоговом SHA и пакете: Busy clipboard, таймер, закрытие/выход, чужая замена и пустой secret без ложного «скопировано». |
| F22 | Копии и перенос | PARTIAL | BackupService; HomeVpnPortableTransfer; Repair-ProGo.ps1 | Перепроверить на итоговом SHA и пакете: Tamper/path/reparse/cancel, чистая установка и другой Windows-пользователь; обычный rollback сохраняет данные. |
| F23 | Обновление и installer | PARTIAL | UpdateLauncher; Update-ProGo.Core.ps1; Install-ProGo.ps1 | Перепроверить на итоговом SHA и пакете: Прогресс/Cancel/offline, скачивание, hash, fixed paths, self-check, rollback; выпуск совпадает с commit/tag. |
| F24 | Помощь и версия | PARTIAL | HelpForm; README.ru.md | Перепроверить на итоговом SHA и пакете: UI/документы/пакеты совпадают; понятные Windows/CLI/Android/iPhone сценарии без личных примеров. |
| F25 | Журналы и экспорт | PARTIAL | BoundedLog; DiagnosticReport; DiagnosticPreview | Перепроверить на итоговом SHA и пакете: Пределы журнала, отчёт до отправки, ошибки чтения, отсутствие секретов и блокировки запуска. |
| F26 | Автозапуск | PARTIAL | ApplicationShortcuts; Install-ProGo.ps1 | Перепроверить на итоговом SHA и пакете: Комбинации настроек в dev Windows; фактический вход владельца - после установки. |
| F27 | Подпись и антивирусы | PARTIAL | .github/workflows/release.yml | Перепроверить на итоговом SHA и пакете: Совпадение payload, проверяемая подпись, timestamp и отдельные Defender/Kaspersky результаты. |
| F28 | Команды и поддержка | PARTIAL | AppCommand; Test-ProGo.ps1 | Перепроверить на итоговом SHA и пакете: Нет неописанных handlers; отдельно scenarios/assertions, FAIL/SKIP/NOT_CHECKED и общий результат. |
| F29 | Визуальный стиль | PARTIAL | BrandIcon; UiTheme | Перепроверить на итоговом SHA и пакете: Native screenshots общей сборки, 16/24 px на разных фонах, читаемость и геометрия. |
| F30 | Почта, кабинет, биллинг | PARTIAL | server/profile_share.py; HomeInvitationsForm; remaining commercial modules absent | Перепроверить на итоговом SHA и пакете: Sandbox/интеграционные сценарии, проверенный webhook, дубликаты, возврат, paid→activation retry, отзыв активных сеансов. |
| F31 | SSH, переносимость, скорость | PARTIAL | Program; ProxyService; SshAgent*; SshProfile*; UpdateAwareTrayApplicationContext; HomeVpnService | Перепроверить на итоговом SHA и пакете: Dev service/key/ACL/cancel/ownership и slow disk/network fixtures, latency/CPU/I/O; фактический encrypted agent, другой ПК/VPS и Rescue замеры — после установки. |

## Декомпозиция остатка: 55 подпунктов

| ID → основной | Требование | Статус | Историческое свидетельство | Зависимости | Следующий шаг |
| --- | --- | --- | --- | --- | --- |
| F16-R01 → F16 | Полнота адаптивной компоновки существующих вспомогательных окон | PARTIAL | Частично | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F16-R02 → F16 | Адаптивность новых installer и F30 экранов | TODO | Предстоит | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F16-R03 → F16 | DPI awareness и реальная смена монитора | PARTIAL | Частично | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F16-R04 → F16 | Итоговая native DPI/display приёмка | PARTIAL | Dev NOT_CHECKED | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F17-R01 → F17 | Полнота клавиатуры и доступных имён/состояний | PARTIAL | Частично | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F17-R02 → F17 | Читаемые disabled и узнаваемые pending кнопки | PARTIAL | **Code/Dev PASS** | нет внешней блокировки | Подтвердить сохранение исторического Code/Dev критерия на новом SHA. |
| F17-R03 → F17 | PIN hint/status без дублирования | PARTIAL | **Code/Dev PASS** | нет внешней блокировки | Подтвердить сохранение исторического Code/Dev критерия на новом SHA. |
| F17-R04 → F17 | Реальная Narrator приёмка | PARTIAL | Dev NOT_CHECKED | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F17-R05 → F17 | Реальная High Contrast и видимый фокус | PARTIAL | Dev NOT_CHECKED | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F19-R01 → F19 | Доказательный LAN/NAT/CGNAT preflight | TODO | Предстоит | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F19-R02 → F19 | Подтверждённые входящие UDP-пути | TODO | Предстоит | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F19-R03 → F19 | Интегрированная поэтапная диагностика | PARTIAL | Частично | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F19-R04 → F19 | Настоящий телефон, маршрут и DNS | NOT_RUN | Live NOT_CHECKED | F23-R06 | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F19-R05 → F19 | Устойчивость настоящего подключения | NOT_RUN | Live NOT_CHECKED | F23-R06 | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F22-R01 → F22 | Состав обычных копий на итоговом пакете | PARTIAL | Приёмка готовой реализации | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F22-R02 → F22 | Защищённый VPN export/import на чистом dev Windows под другим SID | PARTIAL | Частично; cross-SID NOT_CHECKED | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F22-R03 → F22 | Полный runtime repair | FAIL | Открытый дефект | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F22-R04 → F22 | Фактический перенос владельца между ПК | NOT_RUN | Live NOT_CHECKED | F23-R06 | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F23-R01 → F23 | Готовый standalone setup с полным runtime | TODO | Предстоит | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F23-R02 → F23 | Installer в общем maintenance protocol | PARTIAL | Частично | F23-R01 | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F23-R03 → F23 | Одна кнопка и автоматический выбор updater/installer | TODO | Предстоит | F23-R01, F23-R02, F23-R04, F23-R05 | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F23-R04 → F23 | Подтверждённый честный rollback updater | PARTIAL | Открытый дефект | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F23-R05 → F23 | Неизменяемая идентичность принятого выпуска | PARTIAL | Открытый дефект | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F23-R06 → F23 | Единый итоговый setup/updater и dev приёмка | PARTIAL | Package/Dev PENDING | F23-R01, F23-R02, F23-R03, F23-R04, F23-R05, F31-R11 | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F23-R07 → F23 | Установленный update workflow владельца | NOT_RUN | Live NOT_CHECKED | F23-R06 | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F27-R01 → F27 | Signing/verification pipeline поставляемых артефактов | TODO | Предстоит | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F27-R02 → F27 | Проверка издателя и доверия updater payload | PARTIAL | Частично | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F27-R03 → F27 | Реальный сертификат и подписанный итоговый выпуск | BLOCKED | Внешняя зависимость | F27-R01, F27-R02, owner certificate | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F27-R04 → F27 | Антивирусная приёмка конечных артефактов | NOT_RUN | NOT_CHECKED | F23-R06 | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F30-R01 → F30 | Почтовая выдача и согласованная QR выдача | PARTIAL | Частично | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F30-R02 → F30 | Серверные коммерческие данные и права | TODO | Предстоит | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F30-R03 → F30 | VPS-кабинет и вход в него из ProGo | TODO | Предстоит | F30-R02 | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F30-R04 → F30 | Коммерческий жизненный цикл доступа | TODO | Предстоит | F30-R02 | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F30-R05 → F30 | Серверное исполнение ограничений и активные сеансы | PARTIAL | Частично | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F30-R06 → F30 | Онлайн-заказ и платёжный адаптер | TODO | Предстоит | F30-R02, owner payment provider choice | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F30-R07 → F30 | Проверенные платёжные события и активация | TODO | Предстоит | F30-R06 | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F30-R08 → F30 | Технический агентский контур | TODO | Предстоит | F30-R02, owner supplier/provider data | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F30-R09 → F30 | Финансовый реестр и сверка | TODO | Предстоит | F30-R07, F30-R08 | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F30-R10 → F30 | Реальная сервисная почта | BLOCKED | Внешняя зависимость | owner mail account/provider | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F30-R11 → F30 | Реальные собственные и агентские платежи | BLOCKED | Внешняя зависимость | F30-R06, F30-R07, F30-R08, F30-R09, owner explicit live authorization | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F30-R12 → F30 | Коммерческий lifecycle на реальном VPS и клиентах | NOT_RUN | Live NOT_CHECKED | F23-R06 | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F31-R01 → F31 | SSH IPv6 | PARTIAL | **Code/Dev PASS** | нет внешней блокировки | Подтвердить сохранение исторического Code/Dev критерия на новом SHA. |
| F31-R02 → F31 | Профильный fallback | PARTIAL | **Code/Dev PASS** | нет внешней блокировки | Подтвердить сохранение исторического Code/Dev критерия на новом SHA. |
| F31-R03 → F31 | Постоянные ошибки SSH config/arguments | PARTIAL | **Code/Dev PASS** | нет внешней блокировки | Подтвердить сохранение исторического Code/Dev критерия на новом SHA. |
| F31-R04 → F31 | Include/aliases/IdentityFile tokens в диагностике | PARTIAL | **Code/Dev PASS** | нет внешней блокировки | Подтвердить сохранение исторического Code/Dev критерия на новом SHA. |
| F31-R05 → F31 | Остаточные startup/settings/shortcut операции вне UI | PARTIAL | Частично | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F31-R06 → F31 | Фоновый retention preview/cleanup | PARTIAL | Исправлен source-confirmed UI I/O; native NOT_RUN | новый CI, итоговый пакет | Выполнить AsyncCleanupWorkflow и retention regression на точном SHA; visual/target отдельно. |
| F31-R07 → F31 | Фоновое ожидание updater handoff | PARTIAL | Source fix и независимое review; native NOT_RUN | новый CI, пакет | Выполнить owned backend и actual Update route suites; ACK/cancel/timeout/queued lifecycle regression. |
| F31-R08 → F31 | Фоновая работа logs/diagnostic export | FAIL | Source recheck d0f9a690: UI disk I/O | нет внешней блокировки | Tracked Build/save, затем logger/open; native heartbeat и snapshot/cancel. |
| F31-R09 → F31 | HomeVPN private I/O, DPAPI и session preparation в worker | FAIL | Открытый дефект | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F31-R10 → F31 | Асинхронные phone Stop/cleanup | FAIL | Открытый дефект | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F31-R11 → F31 | Подготовка SSH-agent и итоговая dev приёмка F31 | PARTIAL | Dev PENDING | selected-key readiness, startup ordering, F31-R05, F31-R06, F31-R07, F31-R08, F31-R09, F31-R10 | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F31-R12 → F31 | Настоящий encrypted-key agent и маршрут | NOT_RUN | Live NOT_CHECKED | F23-R06 | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F31-R13 → F31 | SSH-профили на другом Windows ПК/пользователе и VPS | NOT_RUN | Live NOT_CHECKED | F23-R06 | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F31-R14 → F31 | Installed build в Rescue/HDD/VHDX | NOT_RUN | Live NOT_CHECKED | F23-R06 | Выполнить критерий; зафиксировать тесты и точный SHA. |

## Очерёдность завершения

1. Исправить подтверждённые startup/QR/rollback дефекты; новый Windows+Ubuntu CI, независимое ревью diff.
2. Завершить готовность конкретного выбранного SSH-ключа и связать её со всеми путями запуска. Наличие `.pub`/отпечатка не считать proof приватного ключа/подписи/доступа. F31-R11 остаётся PARTIAL.
3. Закрыть оставшиеся UI workers (retention/handoff/logs/HomeVPN), transfer/repair, сетевой preflight и итоговые DPI/accessibility критерии.
4. Реализовать согласованный setup/updater fallback, подпись pipeline и коммерческий dev-контур. Provider-specific адаптер требует выбора реальных провайдеров; это не блокирует независимую серверную модель.
5. Сформировать общий приватный проверочный пакет; после отдельной установки владельцем пройти live gates.

## Протокол передачи и аудит

См. [TAKEOVER_AUDIT_20261021.md](TAKEOVER_AUDIT_20261021.md): источники/пробелы, Git/исполнители, находки, воспроизведение и проверки. Точка продолжения и новые SHA/CI фиксируются там.

## История: исходная точка продолжения перед CI583

Code/test source `5c803be5534601233e0041202890cbd7c04cd575`:
QR `bd9c0fb17b4ca4e84bdcec2cf5e1c6d90820f69d`,
startup `e2fccacb8c7a859217cc854cb91bc7ee7ac57bd8`,
rollback `5c803be5534601233e0041202890cbd7c04cd575`.
Общая Python discovery на этом source:44 reported,41 PASS,3 SKIP (C# harness).
Public-content и diff checks PASS; runtime Windows NOT_RUN. Независимые source
review startup/rollback завершены. Исторический непринятый остаток49=37 до
установщика+12 зависимых; новых закрытых подпунктов55 в этом сеансе нет.
Шесть прежних scoped PASS сохраняют свои свидетельства, но не заменяют новую
итоговую регрессию. Инструкция дальнейшей приёмки/возврата:
[ACCEPTANCE_RUNBOOK.md](ACCEPTANCE_RUNBOOK.md).

Следующим шагом на этом историческом этапе было исправление подтверждённого FAIL CI583, публикация
связанных signing/settings изменений после проверки секретов, новый Windows/Ubuntu CI
на точном SHA. Прежняя privacy decision уже получена; способ передачи пакета
ещё не согласован. Полный F31-R11 остаётся открытым.

## Следующий связанный блок: выбранный ключ

Source review пройден для SshKeyReadiness, frozen Unicode child environment и
явного «Проверить ключ» в SSH диагностике. Есть отдельные native backend/UI suites,
строгое расширение прежнего keyboard test; timeout/Cancel/Close/freshness покрыты
подготовленными сценариями. Фактический новый Windows run пока NOT_RUN.
Протокол: [MICROTASK_SSH_SELECTED_KEY_SIGNING.md](MICROTASK_SSH_SELECTED_KEY_SIGNING.md).
Подпись публичной reference не доказывает private bytes либо вход на VPS;
автоматическая подготовка выбранного auth перед SSH остаётся в F31-R11.

## CI583: подтверждённый результат и исправление

Run [39660551553](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39660551553),
source `e9fcfc3279269075eddd6def306c1ca2500cd28c`,
tested merge `d379acd6a0dc5dc55d5d01417cc5c674237d1f27`.
Ubuntu SUCCESS. Windows FAIL: Windows PowerShell5.1 прочитал новый русский
текст в `tests/MaintenanceTests.ps1` как ANSI: UTF-8 файл не имел BOM.
ParserError на строке256 возник до выполнения MaintenanceTests.
До ошибки успешно выполнены SOCKS recovery41/403assertions,
SSH-agent startup62, Desktop4539, Instance28. Maintenance/shutdown/HomeVPN,
package и полный F23-R04 — NOT_RUN на этом source. CI583 остаётся FAIL.

Исправление сохраняет все assertions и таймауты: MaintenanceTests получает
UTF-8 BOM; `scripts/Test-ProGo.ps1` до build/native suites вызывает ParseFile
для каждого `tests/*.ps1` с тем же декодированием, что Windows PowerShell.
Следующий CI проверит и этот fix, и новый выбранный ключ; старые scoped PASS
не подтверждают более поздний код.

## A11: сохранность настроек при ошибке первоначального чтения

`SettingsService.Load/SaveCore` теперь держит sticky read-failure guard:
файл, который не удалось прочитать/разобрать при создании сервиса, нельзя
заменить fallback defaults через Save/TrySave/tray/rollback/async Save.
Новая ошибка имеет фиксированный текст без пути/профиля; обычное сохранение
через настройки показывает инструкцию проверить доступ/формат и перезапустить.
Genuine missing создаёт defaults через exclusive Move: появившийся параллельно
чужой файл сохраняется. Поздний detached Load не публикует и не снимает guard.
После устранения причины новый запуск читает исходные profiles/unknown fields.

Независимое source review PASS; новый native suite зарегистрирован один раз.
Реальные isolated lock→unlock, malformed/null JSON, missing/creation race,
все write APIs и fresh trusted recovery — подготовлены, Windows NOT_RUN.
A11 становится PARTIAL до точного CI; F15/F22/F31-R05 полностью не закрыты.

## История: опубликованный кандидат CI584

PR88, source `6b3122f0cd375ff1c0ae597bc677df58bbd2e5b8`,
merge `2f6b901a6fd537e0e0eeb81c0431b200e29260b2`,
run [39663669399](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39663669399).
Signing `28b7a8a155abff0e0f073adcf07ac4f68f2d02e9`,
encoding `883787b6a392020fd10bb07e00f80bfff00e3bf5`,
settings `6b3122f0cd375ff1c0ae597bc677df58bbd2e5b8`.
Каждый API tree совпал с локальным; expected-head fast-forward без force.
До публикации17files/1499addedlines: targeted secrets/PII0findings,
public-content305/0 и diff checks PASS. Ubuntu completed SUCCESS;
Windows in_progress, общего PASS нет. Public uploads выключены.

Точный остаток auth preparation и исключения direct/custom/other-auth:
[MICROTASK_SSH_AUTH_PREPARATION_SCOPE.md](MICROTASK_SSH_AUTH_PREPARATION_SCOPE.md).
Реальный общий launch в ProxyService покрывает и StartTunnelCore, и PollRecovery;
одна вставка в Context не закрыла бы F31-R11. Новый запрет SSH по неуспеху
локальной подписи не введён.

## F31-R06: фоновая очистка подтверждённых копий

Tray CleanupBackups теперь запускает одну BackupWorkSession: план в worker,
готовый точный список и явное подтверждение на UI, fresh revalidation/delete
в worker. Повторные cleanup/restore/manual requests разделяют текущий actor;
команды удерживаются до полного workflow и owner completion. Cancel/Close,
shutdown refusal/retry и forced disposal сохраняют ownership; поздняя публикация
на закрытый UI не требуется для завершения worker.

Старые retention APIs сохраняются; token overloads проверяют отмену при чтении,
обходе и перед каждым удалением. Approved list не расширяется; изменённый manifest,
manual/unknown и junction guards сохранены. In-flight Directory.Delete не имеет
принудительной отмены: worker остаётся owned до его возврата. Успешный фактический
результат после последнего delete не превращается в abort при поздней отмене.
При отмене/ошибке UI честно сообщает, что уже удалённые копии не восстанавливаются.

Независимое backend source review и root UI/test review PASS; diff/public scan PASS.
Низкоуровневый pre-cancel/no-delete и10 native actor/preview/revalidation/lifecycle
сценариев подготовлены. Отмена внутри OS delete не исполнена/не заявлена.
Runtime всех новых retention изменений NOT_RUN до следующего CI; F31-R06 PARTIAL.

## CI584 completed FAIL: scoped key/settings PASS, fixture encoding

Source `6b3122f0cd375ff1c0ae597bc677df58bbd2e5b8`,
merge `2f6b901a6fd537e0e0eeb81c0431b200e29260b2`, run39663669399.
Ubuntu SUCCESS, Windows FAILURE. Подтверждены backend signing55, signing UI50,
settings initial-load preservation93, Desktop4739, agent startup62, Instance28.
BOM/ранний parser fix CI583 прошёл; Maintenance дошёл до реального rollback.
Первый новый rollback-copy-fail прошёл критерии failure и отсутствие false-success
markers, затем упал assertion русского текста диалога. Driver записывал текст
через Set-Content без encoding. PS5.1 default ANSI делает capture зависимым
от active code page; потеря кириллицы — вывод по source/failure. Точная ACP и bytes
CI584 не сохранены, поэтому это не отдельное наблюдение байтов старого run.
Это отдельный дефект транспорта тестового evidence, не признание rollback PASS.
Исправлены writer/read encoding на UTF8, исходные semantic assertions сохранены.
Остальные rollback vectors/shutdown/HomeVPN/package NOT_RUN в этом CI.

Source signing/settings criteria имеют scoped PASS на6b3122f0; visual review новых
PNG и целевой ПК NOT_RUN. Новые retention/updater изменения требуют отдельного CI.

## F31-R07: worker handoff и граница подтверждения помощника

Actual Update route теперь StartUpdate → StartUpdaterAsync → UpdaterCore →
LaunchUpdater в tracked BackupWorkSession. Installed-path I/O и ожидание ACK идут
в worker, UI получает progress/Cancel и fixed nonsecret ошибки. Backend использует
StartOwnedHandoff: до ACK владеет suspended/job-contained tree; после ACK поздняя
отмена не отзывает независимого помощника. Legacy StartUpdater wrapper сохранён,
actual route его не вызывает. Guard в Test-ProGo проверяет новый owned API.

Shared restore/update reservation не допускает выход/новые mutations до settlement.
Исправлены найденные независимым review queued races: accepted flags до снятия
reservation и reentrant UI callbacks; старый timeout не сбрасывает Accepted;
старый settle не сбрасывает новый ordinary shutdown; rejected flags очищаются до
callback. Own ordinary completion не зависит от чужого pending reserved task.
Ранее admitted native enable может завершить свою принятую фазу; preflight ждёт
её и затем отключает. Новый reconnect/automation после reservation подавляется,
публикация committed config сохраняется. Это не принудительная отмена OS вызова.

Backend independent review и финальный независимый lifecycle/tests review PASS.
Actual helper/descendant accept/reject/cancel/timeout и10 native Context сценариев
подготовлены, зарегистрированы; реальные установки не выполняются. Старые
WindowsRestore gate assertions перенесены на actual updater route и усилены.
Новый native run NOT_RUN; F31-R07 и общий F31-R11 остаются PARTIAL.

## A12: fresh ownership ярлыка Codex

`CodexProxyService.Enable/Disable` проверяет реальную ShellLink target и точные
arguments текущего launcher до изменения cmd/lnk. Copied description/один Marker
не доказывают ownership ярлыка. Проверенный объект удерживается read/delete handle;
Disable задаёт delete disposition именно ему. Existing owned link не переписывается,
новый link/cmd публикуется Move без overwrite. Missing parent не скрывает junction
выше: проверяются все ancestors до root. Foreign/malformed/denied/read-only/reparse
link даёт отказ с сохранением чужих файлов. Legacy MoveOwned/IsOwned не меняются.

Root source review обнаружил и затем подтвердил исправление ancestor bypass.
Реальные isolated COM/ACL/sharing/NTFS junction/replacement/pinned-handle/repeat
сценарии зарегистрированы. Native Windows NOT_RUN; A12 PARTIAL до точного CI.
Запись existing owned cmd остаётся in-place как прежде; общей атомарной транзакции
двух файлов и сохранности собственных bytes при любой I/O-ошибке не заявляется.
F31-R05 и F06 полностью не закрываются этим узким guard.

## История: CI585 и сверка остатка

Source `d0f9a6902345eab288b25e8f902df1514db3f2ca`, merge
`4b952b8b4e3a6473ceac230732384046801ce7fd`,
[run39667773240](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39667773240).
Ubuntu SUCCESS, Windows in_progress; полного PASS нет.
20files/1905addedlines перед push:0 secrets/PII findings, public-content310/0,
diff PASS; Python41PASS/3SKIP. Новые published commits/tree mapping в JSON.

[RESIDUAL_RECHECK_20261021.md](RESIDUAL_RECHECK_20261021.md) фиксирует повторную
независимую проверку на этом source. A04/F22-R03 и F31-R08/R09/R10 подтверждены
как source FAIL; native latency/воспроизведение/ПК NOT_RUN. Исправленные R06/R07
их не закрывают. Никакие owner PC/VPS/repair действия для проверки не выполнялись.

## CI585 completed FAIL: native ShellLink sharing

Source d0f9a690 / merge4b952b8b / run39667773240: Ubuntu SUCCESS,
Windows FAILURE. В CodexShortcutRepeat настоящий IPersistFile.Load упал с
FileLoadException0x80070020: COM повторно открывает pathname без share-delete,
несовместимо с удерживаемым DELETE handle guard. Это новый production guard bug,
не flaky timeout. Guard/ownership не ослабляются: вместо path-open ShellLink
загружается через IPersistStream из bounded bytes того же pinned file object.
Новый полный Desktop/R06/R07/key run, Maintenance encoding fix, shutdown/HomeVPN
и package остаются NOT_RUN в CI585. Предыдущие55/50/93/4739 — scoped CI584,
их нельзя перенести на source d0f9a690. Новый runtime fix получит отдельный CI.

## A12 stream-loader: исправление конкретного native FAIL

После CI585 ShellLink parsing использует снимок1..1МиБ из того же pinned
FileStream → SHCreateMemStream → IPersistStream.Load. Повторного pathname-open
нет; DELETE/share-read и handle deletion сохранены. Native object и memory stream
освобождаются nested finally, pinned file живёт до mutation caller.
Добавлены empty/oversize и отказ обычного File.OpenRead под DELETE pin.
Independent source review PASS; новый native run NOT_RUN. CI585 не исправляется
задним числом. В нём отдельно PASS29 standalone retention checks, включая новый
real pre-cancel/no-delete; десять cleanup Context сценариев ещё NOT_RUN.

## CURRENT CI708 bounded Installer split; cancellation initiator UNKNOWN

Baseline source d6b25e8037e2387f74970aec92b5cc48001b3554/tree4099f8665abca63d024e319ebfd4e18b5b0f0a76;
checkpoint02cdca5ef9be493897b93d63cecbd8ead121c333 clean/preserved. PR88 independently exact baseline/open/unmerged;
only root active/no Git/compiler/PowerShell writer; no active CI at this source (sole CI708 completed cancelled).
New source CURRENT_TREE (git rev-parse HEAD), reviewed local code9d87fa4e64eb3f884d2e81ff799edbe85abcb197.
Old worktrees untouched; no new production functionality or durable Legacy prerequisite.

CI708 full four logs independently read at exact merge0b948a5b31e1a9d05f83306ac1d626de56b74f27:
Installer121288486845/712lines CANCELLED, Core121288487520/15409 SUCCESS, Ubuntu121288487023/607 SUCCESS,
aggregate121298114383/168 FAILURE correctly rejects cancelled installer despite all3 identical tested SHA.
Run https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40232984506 . Old29 installer suites PASS;
LegacyAdmission19 only absent-preserved/custom-preserved/removed PASS, remaining16 unconfirmed;
SourceCapture18 NOT_RUN, suite not started. No full Legacy19/Installer31/candidate PASS on this source.
Core Desktop6394/Instance30/Maintenance126/Shutdown47/HomeVPN4816/Portable91 and Ubuntu suites PASS.
Runner-only ZIP670118 bytes SHA256 febf2a1d37dabb0665aa6054fc8113d247e1d6ecf060a2f0d987f53e38371f6f;
uploads/prune SKIPPED, no delivery/PC acceptance. Old scoped PASS not inherited by split source.

Exact cancellation cause/initiator UNKNOWN. Final error only operation was canceled at21:34:58 UTC.
Metadata started20:54:52/completed21:35:03 UTC =40m11s, configured installer timeout40m. Compatible, NOT
proof of timeout initiator. Supported check-run annotations request rejected400 INVALID_ARGUMENT; no bypass.
Separate proven capacity issue: completed first27 suite elapsed sum1577655ms (26m17.655s), next2 completed
665866ms (11m05.866s); before Legacy2243521ms (37m23.521s), leaving insufficient margin for observed
Legacy19 work plus unstarted Source18 in a40m sequential envelope. Legacy first3 alone took~117s;
full duration of remaining work is unknown. No runner-flakiness/runtime/AV diagnosis asserted.

Bounded correction: same31 suites split into Foundation(first27) and Recovery(final4: MenuResult,
MenuFailure, LegacyRollbackAdmission, LegacySourceCapture), isolated Windows jobs/checkout/build/temp roots.
No test duplicate or dropped body; within each partition original order. Default All still executes original31
in original order. Each job retains40m cap; no test/assertion timeout or coverage change. New
InstallerPartition validated parameter only selects Installer mode; unknown selection fails. No shared
artifacts/fixtures between jobs. Existing installer-tests now Foundation; new installer-recovery-tests
Recovery. Existing Core/Ubuntu/jobs/events/concurrency/permissions/upload/prune bodies unchanged.
Strict aggregate requires Core+Ubuntu+both installer partitions success on exact same40hex merge SHA;
missing/failed/cancelled/skipped/inprogress/wrong/missingSHA all fail closed. No manual dispatch/rerun.

Tests: aggregate12 expanded over all4 required names plus4 partition coverage-contract tests, total16 PASS.
Every original31 suite is frozen as expected ordered coverage; removal(any31), duplicate Source18, wrong
selector rejected. Exact suite-block reverse parity/native runtime/all old fixtures unchanged; YAML/events/
permissions/deadline/upload/signatures/static/review/JSON/secrets/public-content/diff PASS locally.
PowerShell5.1/Windows split partitions and Source18/full31/new5-job aggregate NOT_RUN until new automatic CI.
See CI708_INSTALLER_PARTITIONS.md. One expected-HEAD guarded publication/one automatic PR CI only.

Next SAME new CI all5 final logs/exact merge/Foundation27+Recovery4/Source18/whole31/Core/Ubuntu/strict
aggregate. No durable Legacy source work before full PASS. UNKNOWN cancellation added without erasing
Python/rebind/sharing/repair/setup UNKNOWN incidents; BLOCKED_TRANSFER/no retry unchanged. Entire31/
nested55 approved scope/weights and scores retained delta0. No live install/services/network/security/
ACL/credentials/VPS/tunnel/release/uploads/delivery retry or second functional stage.

Acceptance of this correction requires both real isolated Windows partitions and strict aggregate on one new exact merge. Local contract tests are not Windows execution. Cancellation origin remains UNKNOWN even if split later passes. Source18 and original31 remain mandatory.

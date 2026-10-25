# CI698 cancellation and independent exact-SHA CI partitions

Source48f079c79fb9dbb012dcf08eb75219bb17289127, tested merge
c1de408f34bc29903cb032a321e39776d839c61f, run40196340794. Full Windows121177870869
log10491 lines and Ubuntu121177870667 log578 lines independently read. Windows CANCELLED,
Ubuntu SUCCESS. This is NOT a failing assertion and NOT full success.
Publication6 all PASS. MenuAdmission29 completed PASS including legacy-removed/native origin/
identical replacement refusals; cancellation during post-startup, final3 not confirmed. Full32 NOT
PASS. Earlier installer/Startup15/14/13/14/25/Desktop6394/Instance30/Maintenance126/Shutdown47 PASS.
Later HomeVPN/Portable/relay/aggregate/public-content/pack NOT_RUN. No old PASS transferred.

## Cancellation cause versus measured capacity

Raw jobs metadata: Windows started13:09:43UTC, completed13:49:53UTC (40m10s including cleanup);
hygiene/build step13:09:51..13:49:49. Workflow timeout-minutes40. Final log only says operation
canceled. Check-run annotations requests were rejected by GitHub connector endpoint validation,
HTTP400 INVALID_ARGUMENT. Exact cancellation initiator/cause remains UNCONFIRMED; duration is
consistent with the job cap, not proof of who cancelled it. No security/access bypass attempted.

Measured checkpoints from this exact log (no extra harness rerun):

| Segment | Observed time | Boundary |
| --- | ---: | --- |
| Tests start through UpdatePackage5 | 1279s /21m19s |13:09:53.962..13:31:13.317UTC |
| Installer tail, incomplete |1116s /18m36s |UpdatePackage5 to cancellation13:49:49.565UTC |
| Publication6 |about1s |StartupFailure15 to Publication6 completion |
| Menu32, incomplete29 |190s /3m10s |Publication6 to cancellation |

Core+partial Installer already consumed2395s, effectively the entire40m budget; final3 and required
HomeVPN/Portable/relay/content/package still remain. This proves a serial capacity bottleneck
independently of whether this particular cancellation was external or timeout-triggered. No reason
to guess a runtime defect, lower coverage, relax revalidation, raise any test/job deadline or retry
same long job. No test body is optimized on an unproved performance assumption.

## Minimal split and acceptance

Test-ProGo retains default Suite=All: all original checks, same ordered25 terminal suites. Extract
that exact25-call tail into Test-ProGoInstallerSuite.ps1. Add safe suite start/pass timing markers;
failures propagate unchanged, no completion marker after an exception. All original fixtures,
assertions/deadlines/source/native paths remain unchanged. The new file is CI-only, not runtime
payload. Core and Installer are explicit partitions; neither alone logs whole ProGo tests PASS.

- windows-build: same hygiene/build/core/desktop/instance/maintenance/shutdown; Suite Core omits
  only the extracted tail. Existing relay/HomeVPN/Portable/Python/content/runner pack remain here.
- installer-tests: separate clean Windows runner, same hygiene and fresh build, then exact25 suites
  in same order, including Publication6 and all Menu32. No repeated Core/Desktop/SSH harness.
- vpn-server-test: original tests preserved; add independent aggregate guard regressions.
- ci-complete always evaluates all three dependency results AND their checked-out commit outputs
  against github.sha. Every job records actual git HEAD and refuses a mismatch before testing.
  Success requires exact required job set, all success and same valid40-hex SHA; missing/failed/
  cancelled/skipped/wrong/missing SHA refuses. Its own checkout is also checked. No continue-on-error.

Existing Windows job ID/name and timeout40 retained; second Windows job also40, Ubuntu12 unchanged.
Aggregate5 is only a small separate metadata gate, not a test timeout increase. Both Windows runners
build the same checkout for their own payload (previous build about4s); no binary artifact transfer,
cache or public uploads added. Upload conditions, pack/relay/content/prune bodies, workflow events/
permissions/concurrency unchanged. Pack on the Core runner is still ONLY runner packaging, not full
acceptance: whole CI must additionally pass the exact-SHA aggregate/Installer. No branch protection
settings changed; reviewers must use complete aggregate/whole workflow, not Core success alone.

check_ci_aggregate.py has12 meaningful positive/refusal tests, including fixed required names,
each non-success, missing/extra jobs, each wrong/missing SHA, malformed/empty fields. Local Linux
12 PASS plus CLI exact success exit0/malformed input exit1. Static parity proves old Core bytecode
text unchanged aside from dispatch, exact25 ordered calls retained and every old fixture unchanged;
YAML/expected-SHA/always gate/upload guard parity PASS. New Windows partition dependency/order/
compilation/native6/32/full aggregate execution NOT_RUN until exact CI; no whole PASS asserted.

No production/installer/runtime/Menu mutation or private-file changes. UNKNOWN incidents and
BLOCKED_TRANSFER preserved, no delivery retry, scores unchanged. One guarded source update and
one automatic CI, no parallel retries/full wait. Next same CI complete logs for ALL required jobs
and exact tested merge/aggregate, with all32 before any next Menu stage. No live install/services/
network/security/ACL/credentials/VPS/release/uploads.

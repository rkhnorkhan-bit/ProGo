# Microtask 07 — independent locked-cleanup baseline and diagnostics

CI run `38161800807` passed both controller regressions, the actual pending form
and all 12 recovery scenarios, then failed `ConfirmedCleanupFailure`.
The successful preceding recovery had persisted the new QR origin. This fixture
still required the earlier hardcoded origin without resetting it, so one term
was necessarily false even when cleanup worked correctly.

The fixture now establishes a distinct prior origin before taking its private
input snapshot. It retains the original requirement that failed setup preserve
that origin and the snapshot's exact bytes. Replacing the expected value with
the attempted origin would weaken that check; this independent setup preserves it.

All former conditions remain in four named checks: command settlement/deadline,
completed proof/journal/inputs/no-save, origin/no-replay/safe terminal UI, and exact
owned-helper retention. Diagnostics include only booleans, counts, timings and
error type, without private paths, origins, tokens or exception messages.
The loop now has the explicit `confirmed-cleanup-failure` scenario label.

The original 2500 ms condition still measures from observed SSH fixture exit
through UI settlement and proof probing. A separate UI-settlement observation
shows that boundary; neither the criterion nor any transport/controller deadline
is widened. This is not a direct measurement of the deletion retry alone.

Application code is unchanged by microtask 07. Other cleanup failures are not
inferred from the old combined assertion.

## Evidence

- Two independent source reviews: PASS; every former condition and deadline is retained.
- Local `git diff --check`: PASS.
- Native proof: PASS in [CI 38164605392](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38164605392),
  Windows job `114544444410`, functional head `00915014df63213524ee793daa2d0a0eec8c852c`.
  Micro07 code is `3c24eaccf5020bfd3450a001710c3b5e2277a713`; the tested synthetic
  merge is `e4098fdbc1d70cb823c5f4b2d79d61af020b3636` against main `4bb336575bb1216d3047bd2b294aeecad9fdd65a`.
  All four named checks passed at 18:49:01 UTC: pending ticks 32, settlement
  1034 ms, original checked duration 1039 ms (<2500), proof/journal/inputs/helper/
  origin retained, no replay, verify/save zero, safe UI and no false completion.
- Subsequent disposed-verification, proof/storage, HTTP, wizard and setup-recovery
  checks executed successfully; the full Home VPN harness passed 975 checks.
- Full Windows candidate: FAIL later in portable RoundTrip, after 14 portable
  checks passed. The combined child-import assertion does not expose which
  operand failed; its cause is not inferred from this cleanup proof.
- Later portable cases and Windows Python discovery: NOT_CHECKED. Final public
  release-content scan, packaging and release uploads: SKIPPED.
- User PC/VPS and installed application: NOT_CHECKED.

Historical 22/31 is unchanged; no full F-point or installer is accepted here.

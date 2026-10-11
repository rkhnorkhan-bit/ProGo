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
- Fresh native locked-cleanup proof and remaining suites: pending CI.
- User PC/VPS and installed application: NOT_CHECKED.

Historical 22/31 is unchanged; no full F-point or installer is accepted here.

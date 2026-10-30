# CI695 Menu admission refusal: targeted diagnostic, NOT a runtime fix

Exact source d57f23596f5a9920bfd899a3e2ba222d9a3c79df, tested merge
3261d9af777fcc9883000c8cf3555177274048d3; run40185551629 Windows121145460388 FAILURE,
Ubuntu121145460224 SUCCESS. Full final Windows10473/Ubuntu578 logs independently read.
At 11:04:31 UTC, InstallerMenuRollbackAdmissionTests Check-MenuAdmission helper line13:
repeated fresh classification remains fail-closed. created/custom-preserved PASS, third
legacy-removed FAIL by exact ordered source, remaining29 NOT_RUN. No violated proof gate or
native exception is exposed: production classifier conservatively catches exceptions/returns null.
No claim of runtime leak, infrastructure failure or incorrect expected result is established.

StartupFailure15/MutationAdmission14/Result13/OwnedStartup14/Admission25 and all earlier installer
suites/Desktop6394/Instance30/Maintenance126/Shutdown47 PASS. Subsequent HomeVPN/Portable/relay/
aggregate/public-content/pack NOT_RUN; uploads skipped. Previous successes not transferred.

## Distinguishing experiment, preserving original contract

Source shows Menu origin (volume/index/creation time digest) is captured on its temporary handle
BEFORE File.Move publishes it. legacy-removed first creates an older primary then moves it to the
legacy name before the real install creates another primary. A rename-related origin change is a
hypothesis, not a proven root cause. Hash-only admission or changing expected PASS would weaken
ownership and is not allowed. No production/native/API change is made here.

Failure-only instrumentation for legacy-removed:
- Original condition evaluated once and still asserted, unchanged; all32 cases, three reads, order,
  expected values, original guarantees/cleanup/deadlines preserved. Diagnostics cannot produce PASS.
- Read-only original fixture facts: snapshot/runtime/pending/auxiliary/context, typed Menu/Startup/
  Legacy verifiers, durable Menu verifier, recorded-origin present/current-origin same, exact byte
  equality and legacy replacement linkage. No raw hashes/identity values/paths/receipt bytes emitted.
- One independent NEW own GUID fixture reproduces the same vacated Menu-name sequence and invokes
  the EXISTING four-argument EnsureLink witness overload. Read the SAME native identity API before
  rename (callback) and afterward. Emit only volume/index/creation/origin/bytes equality booleans
  and elapsed milliseconds. No retry loop, sleep, new process, permission or system setting change.
  Native handles always close; cleanup uses the original guarded own-root finally.
- Any diagnostic failure emits phase + exception type + at most five method/IL stack frames, no
  exception text/Data/ToString/arguments/private contents. It cannot replace the original assertion.

Expected distinction: typed/byte/receipt proofs true but recorded native origin false, with a probe
showing unchanged file index/volume and changed creation field, would establish the capture/rename
boundary problem. Otherwise independent proof booleans identify the next specific failed gate.
No speculative runtime fix and no blind rerun; one diagnostic CI on a new test-only SHA.
Windows execution and new diagnostic observations remain NOT_RUN locally. All assertions stay.
Menu mutation and further functional stages remain blocked pending evidence and actual PASS.

UNKNOWN Python/rebind/sharing/repair/setup incidents and BLOCKED_TRANSFER remain unchanged.
No new features, live shortcuts/install/services/network/ACL/security/credentials/VPS/release/uploads.

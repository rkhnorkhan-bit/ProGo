# CI683 complete repair timeout: bounded test-only phase experiment

CI683 source d4903e818c4c95eadf914a5e077737854071b223, tested merge
477bddbfec21431bbd1049bbc7daf86473b1ead7, run40141050466. Full final Windows191/
Ubuntu578-line logs read independently. Windows121012684350 FAILURE, Ubuntu121012684481
SUCCESS. Production EXE build PASS, Repair null-string interop1 and four incomplete payload
refusals PASS. Complete positive repair child failed WaitForExit(20000) at fixture line156.
PID fixture regression/Desktop/SSH policy/Instance/all installer/LegacyEvidence14/relay
NOT_RUN. Pack/uploads SKIPPED. Checkpoint24df24bb633458a041be612bec69d211d94113bf,
PR88 actual source/clean/sole root writer confirmed before work; old worktrees preserved.

## Evidence boundary

No child phase/output/process/stream facts were printed at deadline. Last confirmed result
is four payload refusals, not completion of positive copy/self-check. Cause UNKNOWN. Current
RepairPayloadTests/Repair-ProGo/MaintenanceOperation equal source5f84ceb from CI681 PASS;
CI683 changed only Desktop fixture. No evidence links that patch to early repair failure.
Past PASS is comparison evidence, never new-source runtime acceptance or a timeout fix.

Actual complete path: start owned powershell -> preflight/hash -> Add-Type maintenance helper
-> ownership/stopped/cleanup gates -> snapshot/stage -> admitted runtime copy -> hash verify
-> owned installed image self-check -> image job exit/cleanup -> verify/commit -> transaction/
lease cleanup -> script exit -> parent process exit -> stdout/stderr drain. Self-check's
8000ms watch starts AFTER OwnedHandoffProcess constructor (CreateProcess suspended, job
assignment, resume); bounded job cleanup can add up to3000ms. Therefore outer20s timeout
alone does not identify a self-check wait or loader/runner/runtime defect.

## One discriminating experiment

Only first complete positive fixture invocation uses tests/RepairPhaseProbe.ps1; original
script remains untouched and runs with the same NoLaunch/payload/environment. Wrapper installs
17 exact unique source-line breakpoints with automatic constant-name actions; no stepping,
interactive prompt, code rewrite or script variable dump. Source anchor mismatch fails clearly.
All markers contain fixed phase names and elapsed milliseconds only, in a CreateNew own-GUID
fixture trace. Markers are BEFORE anchored statements; names such as copy-complete and helper-
loaded mean the preceding phase returned. Selfcheck-return proves RunOwnedSelfCheck including
owned job Dispose returned; cleanup-return means actual script finally returned. Repair-return
and probe-cleanup markers separate script completion from instrumentation/PowerShell exit.
The instrument adds overhead inside the SAME20s budget; a green run is non-reproduction, not fix.

Parent emits start-enter before Process.Start, start-return timing afterward; then at deadline
or normal process exit and again after existing cleanup: elapsed/start times, own root process
known/exited/CPU/RAM and exact stdout/stderr task states. Trace read max8192 bytes, own journal
read max65536 bytes: only whitelisted phase and entry/admission counts, plus existence of
self-check data under up to8 guarded own stage folders. No journal contents/transaction IDs,
paths/argv/PID/credentials/config/raw streams/exception messages are printed. Stack output is
at most6 sanitized managed type/method names, no source paths. Child error stack is filtered
from completed captured stdout only; timeout stack is parent's current managed checkpoint,
not a fabricated inner child stack. Child self-check native handles are NOT directly observed;
phase and own data-root evidence must not be overstated as exact child liveness.

This separates no wrapper/script entry, preflight/helper/maintenance/snapshot/copy/verify,
self-check call vs return, commit/cleanup, process exit and stream drain. Any remaining gap,
including native CreateProcess versus earliest image initialization, must be reported honestly.
Original WaitForExit20000, self-check8000, output3000/cleanup3000, assertions, operation order
and later recovery cases remain unchanged. Original arguments restored before later fixture
runs. Only existing own root child is killed in original finally; no foreign process action.
Breakpoints removed and own trace disposed in wrapper finally. Trace lies outside private-data
snapshot/payload; no live install/service/network/security/ACL/credential change.

Local review/static exact source anchors/timeout+assert/runtime parity/signatures/diff/secrets/
public-content PASS. No local PowerShell/compiler/Windows: wrapper execution and all native
regressions NOT_RUN. This is diagnostic source, NOT an implemented repair fix or blind rerun.

## Incident ledger

| Incident | Current evidence/status |
| --- | --- |
| Python interpreter startup CI662/670 (and older) | UNKNOWN; later passes only non-reproduction, safe diagnostics retained |
| Queued Stop listener release CI671 | UNKNOWN; later1s PASS not a demonstrated fix |
| SOCKS pending receipt busy CI676 | UNKNOWN; CI677+ pass not runtime fix, ownership diagnostic retained |
| Command PID double-read CI682 | Fixture code correction d4903e8 captures original successful observation; Windows test still NOT_RUN because CI683 failed earlier. Exact original getter-null branch not measured; no runtime leak claim |
| Complete repair20s CI683 | New UNKNOWN; this phase experiment awaits exact CI; no AV/load/runtime attribution |

Next same diagnostic CI final logs/source/merge, last completed phase, own process/drain/
cleanup and new downstream tests only if reached. No functional stage before PASS.
BLOCKED_TRANSFER/no retry; no release/uploads/install/live actions. Score unchanged.

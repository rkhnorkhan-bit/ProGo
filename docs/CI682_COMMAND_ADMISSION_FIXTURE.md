# CI682 command fixture original PID capture

Baseline source559a18adf35dfeeb23c16c7fc4880e2e0d41cd9a, tested merge
ba5b0470add2c8008bc9251825dc8ac10fc7e251, run40139850722, Windows121009095063 FAILURE,
Ubuntu121009094843 SUCCESS. Independently read complete Windows4293/Ubuntu578-line logs.
Current baseline checkpoint b8c3ce39f67529600213b1e5c74db3ba5331b3b4 and PR88 head verified,
clean/sole root writer; old worktrees preserved. No previous operation replayed.

## Evidence and code causal chain

Desktop CommandAvailabilityWorkflow -> Main fails original-fixture-pid-retained. Original
count predicate TRUE; capturedPidPresent=False, fixtureAliveKnown=False/fixtureAlive=False,
releaseExists=False. All three observations before-connect/after-connect/after-alias:
count1, pidPresent=True, pidSame=False, connecting=True, stopping=False, requested=True and
expectedWaitingLifecycle=True. No raw PID or secret needed to distinguish this failure.
Safe stack method names CommandAvailabilityWorkflow <- Main. SOCKS51 and logged earlier
repair/vault/log/backup/updater suites PASS scoped. SSH policy214 NOT_RUN; Desktop NOT complete;
LegacyEvidence14 and ALL installer/Instance/relay suites NOT_RUN; pack/uploads skipped.

DesktopStartupTests is byte-identical to preceding source5f84ceb. No code relationship
between this earlier fixture failure and the new legacy native path is evidenced: that path
was not executed. Prior CI642/643 nullable-PID diagnostics were kept, not replaced by a guess.

Original PumpUntil polls CurrentPid.HasValue under the unchanged 5000ms contract. A successful
observation ALREADY implies ProxyService's actual owned live process after Process.Start and
ownership assignment under its gate. Thus snapshot-before-any-admission is NOT the explanation
supported by this code. PumpUntil additionally calls Application.DoEvents after success; test
then checks UI and performs a SECOND CurrentPid read to initialize expected PID.

CurrentPid intentionally uses Monitor.TryEnter(gate), returning null if busy; it also returns
null for no live owned process. Successful predicate does NOT guarantee a later independent
read has a value. CI682 proves that second expected snapshot was null, not that its precise
null branch/gate owner was observed. Gate contention is a supported mechanism, not independently
measured native root cause. No process replacement/runtime leak/infrastructure fault is claimed.
The proven fixture defect is discarding the successful admission and expecting a subsequent
nonblocking observation to be a stable baseline. It compares later real PID against null.

## Minimal test-only correction and regression

Capture the nullable PID from THE SAME successful read inside the existing PumpUntil predicate.
Freeze it before UI checks and all dispatcher actions. Refuse a wait that never observed admission;
assert frozen PID's actual liveness before actions. Do not obtain/substitute expected PID from
before-connect/after-connect/after-alias or from a later process. All original route count and
exact original-PID equality assertions and operand order remain; real replacement still FAILS.
All safe diagnostics remain. No runtime/getter/timer/startup/transport/settings behavior changed.

Deterministic regression feeds absent -> admitted synthetic PID -> unavailable snapshot before
wait returns (models final DoEvents/contention gap). Capture must retain original admitted value,
perform no extra read and reject no-admission wait. This metadata regression supplements, not
replaces, the existing real child/actual modal settings/Stop/shutdown scenario. No fake process
is used for integration. Existing PumpUntil body/5000ms bound and all cleanup/coverage unchanged.
The actual isolated child remains blocked by its existing release file until original scenario
releases it or owned cleanup stops it. No process or security setting is changed here.

Local review/signatures/assertion/timeout/runtime parity/diff/secrets/public-content PASS.
No local C# compiler or Windows: compilation/new regression/Desktop/LegacyEvidence14/all new
exact-source native suites NOT_RUN. New exact CI is justified by a real fixture correction,
not a blind rerun. Old incidents sharing/Python/rebind remain UNKNOWN; CI642's historical
individual failure was unobserved, so do not retroactively claim its exact cause resolved.

Next SAME new CI completed logs/exact tested merge and original command count/PID/lifecycle,
full Desktop/Instance/all installer including LegacyEvidence14/relay regressions. No functional
stage before exact PASS. Auxiliary revalidation/pending-installed remains unimplemented.
BLOCKED_TRANSFER/no retry; no release/uploads/install/live services/network/ACL/security/
credentials actions. No readiness score awarded for unexecuted test/code/docs.

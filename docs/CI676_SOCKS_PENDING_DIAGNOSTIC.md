# CI676 SOCKS receipt sharing: bounded distinguishing experiment

CI676 source1fff0f05024d7249495b9594fef162074e5aa95a, tested merge
1eb3bac1d59e9e0e066fd7635e33264f5dfb5181, run40123483430/job120959851985.
Full Windows final log independently read. Windows FAILURE/Ubuntu SUCCESS.
Last PASS: config and prelaunch failures leave permitted manual and automatic backups
available (18 assertions), then SocksRecoveryTests.exe IOException sharing refusal on
synthetic GUID.json.pending. PowerShell Test-ProGo.ps1:456 truncates managed stack.
Next scenario is SshDiagnosticDistinctions (transport/nonfatal versus key/host-key).
RollbackOutcome12/Maintenance19/Desktop/Instance/relay NOT_RUN on this source. Earlier
repair/vault and actual prior logged suites PASS in scope. No installer regression proven.

## Source facts / missing evidence

The self-hosted FakeSsh child publishes a unique own launch receipt with WriteAllText on
pending, then Move to json; WriteAllText disposes its stream before Move. FallbackCapture
reads only final json. Nested using order for next scenario is Fixture.Dispose (Proxy.Dispose),
SshDiagnosticReplay.Dispose, FallbackCapture.Dispose restoring environment then recursive
delete of only own GUID temp directory. Proxy.Dispose cancels preparation/startup and calls
StopProcessOnly under gate; StopProcessOnly kills only its owned Process, waits1000ms,
retains ownership on failure. Async Stop separately waits previous startup/preparation.
No such Stop/Dispose logic or fixture was changed by CI676 installer-only diff.
Old log does not identify failing parent operation, full managed stack or actual lock owner.
A cleanup-versus-writer race is a hypothesis, not a runtime leak or established root.

## New test-only diagnostic, no fix claimed

Scenario wrapper catches IOException solely to print fixed operation/scenario, exception
TYPE/HRESULT, and up to32 managed method/IL frames on stdout before original rethrow.
Never prints exception messages, source paths, raw arguments or payload contents.
Fixture Dispose still calls same Proxy.Dispose once; record before/after owned-child role,
HasOwnedProcess and read-only startupTask/stopTask pending flags. No extra Stop/wait/kill.
Only if capture Directory.Delete throws IOException, additionally print exact cleanup
operation/safe stack and inspect this own direct progo-fallback-GUID temp directory.
Select one GUID/PID-shaped pending leaf, reject reparse paths, report capped pending count
and claimed-writer liveness/match bool (name alone is NOT proof of lock ownership).

One bounded Restart Manager session registers ONLY that synthetic file and queries its
actual locking processes (at most16 owners; at most2 GetList calls, no retry). Emit only
counts: claimed writer, last disposed child, current test process, same fixture executable
with matching process start stamp, unreadable roles. API session registration is temporary
OS diagnostic bookkeeping, not file/ACL/security/service configuration; always EndSession.
No RmShutdown/RmRestart, process termination/adoption, file retry/rebind or permissions change.
No raw PID/path/SID/app/service names/content emitted by new diagnostics. Queries may race
natural process exit: ownerCount0/read failure is not proof that no earlier lock existed.

Interpretation: capture-delete + owned child alive/locking after Dispose distinguishes
retained process/settlement from a different process owner; scenario stack identifies
other read/move/delete operations if failure differs. Same-process owner versus external
owner has different next investigation. No positive assertion/timeout/share mode/cleanup
order/fixture payload or production code changed. A passing run only non-reproduces incident.
Actual Windows compilation/native diagnostic NOT_RUN locally. No blind same-SHA rerun.
Old Python/rebind incidents UNKNOWN; installer paths preserved, journal progression paused.
Privatecandidate BLOCKED_TRANSFER; no transfer/release/uploads/install/live/services/ACL/
network/security/credentials changes. Readiness58–80/22,+0.

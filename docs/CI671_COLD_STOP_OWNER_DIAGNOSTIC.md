# CI671 cold queued Stop: owner/rebind diagnosis

Sourcecb7632a9dcd46c3f4fe23b9059ad81f306b5a9b7, tested merge
366a0cca22c5743abf4418ba637619ff9721e4b7; run40111310628/job120923441236.
Full Windows final log independently read. Windows FAILURE, Ubuntu SUCCESS.
Python exit0 passed: CI662/670 timeout not reproduced, NOT fixed; diagnostics retained.
Desktop AsyncSettingsColdStartHeartbeat queued Stop completed after durable cold publication,
settingsPending=False/Bridge.IsRunning=False assertion PASS. Exclusive loopback rebind still
fails AddressAlreadyInUse after original1s; remaining loopback listener reported. Stack
Occupy -> AssertPortReleased -> AsyncSettingsColdStartHeartbeat -> AsyncSettingsWorkflow -> Main.
Desktop did not finish; Instance/maintenance15/Rollback8/relay NOT_RUN on this source.

## Evidence boundary

Occupy creates an ExclusiveAddressUse loopback listener. The failed test retains its own
occupied-port ListenerScope and SOCKS listener; published fallback is asserted distinct from
occupied. Candidate listener is created in-process with an accept thread, not an external
helper. Queued Stop waits integration cleanup, then Bridge.Stop clears active and calls
ListenerSession.Stop/Server.Stop. IsRunning=False alone is not proof of kernel port release.
No owner PID was collected: cannot call the remaining socket a runtime leak or foreign reuse.
AddressAlreadyInUse plus LISTEN is stronger than TIME_WAIT alone, but does not name ownership.
No child is started by this cold Ensure/Stop slice; other-process ownership remains unknown.

## One distinguishing experiment, test-only

Only if the original1s exclusive rebind loop fails, read an IPv4 TCP owner table snapshot
before the unchanged original AssertPortReleased. GetExtendedTcpTable owner-PID-all class5,
AF_INET2; no new process, sockets, Stop, port adoption or retry. Counts only for exact test
port: own-current-process LISTEN versus other-process LISTEN, loopback/wildcard/other-address,
non-LISTEN rows. Add operation/Stop settled, settingsPending/bridgeRunning/cleanupPending,
configured/bridge and occupied-fixture identity booleans. No raw PID/address/process args,
module paths, credentials or config data printed by new diagnostic. Existing failure
assert/diagnostic preserved. Table size capped1MiB, rows4096, two API calls with no retry;
read failure recorded safely, native buffer/current-process wrapper disposed in finally/using.

Interpretation: own LISTEN with stopped bridge requires next precise candidate/retained
session investigation; other LISTEN means external port ownership and cannot be killed or
adopted; no LISTEN plus connection rows/read-error must not be called a runtime listener leak.
This experiment does not fix or weaken the1s contract, and a passing run alone will only
non-reproduce the failure. Runtime, queued Stop, guards, assertions and timeouts unchanged.
New Windows compilation/native diagnostic NOT_RUN locally; exact new CI required.
Journal deferred; privatecandidate BLOCKED_TRANSFER; no release/uploads/install/live/security/
network/credential changes. Readiness58–80/22,+0, weights unchanged.

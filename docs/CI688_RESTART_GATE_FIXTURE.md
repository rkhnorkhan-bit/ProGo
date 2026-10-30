# CI688 restart-gate isolated body dependency and refusal compatibility

Source115748057bcafcae22ead2dcdfa628c257450e39, tested merge
064e39188b4744a85cb93620db6efc67a8c9e1fd, run40152222091 Windows121045939351
FAILURE at InstallerRestartGateTests.ps1 helper17, first no-parent scenario.
Full failing log independently read. Composite reason/Stops/Disposes/Launches/Success
values were not logged; do not claim measured individual values.

## Proven code cause

RestartGate fixture extracts installer body starting at InstallDir; original preflight
is intentionally omitted. Its local scope and Test-ProGo caller do not initialize
Payload. New publication call evaluates Payload.ScriptsDirectory before entering the
resolver. StrictMode makes this an exception even when no backups parent exists;
Stop is never reached. Maintenance is already assigned and original finally calls
Dispose. This is deterministic missing fixture dependency, not runner flakiness.
The real installer initializes Payload via preflight before the extracted boundary.

Minimal fixture initialization supplies only ScriptsDirectory pointing at the project's
existing trusted scripts. It does not simulate installation or bypass the real gate.
All8 original scenarios, exact expected reasons/counters, private-byte preservation,
new-journal refusal and cleanup assertions remain unchanged. Stop/Launch stay mocked.
Other body fixtures inspected: Maintenance has actual preflight Payload, Reconcile
has Get-ProGoInstallPayload; new publication fixture also has actual preflight Payload.

A second deterministic compatibility issue would follow in pending cases: the new
resolver's generic refusal changed the existing restart-gate reason. Keep that same
existing public refusal text for failed auxiliary/runtime proof. Only the error text
changes; conditions, verification, throw behavior and gate remain exactly as strict.
No test expectation is broadened or changed. Existing8 assertions exercise refusal
before Stop/maintenance Dispose/no success/no Launch and no journal bypass.

CI688 scoped PASS: Desktop6394/Instance30/Maintenance126/Shutdown47,
Payload9/Maintenance19/Stage8/Backup8/SelfCheck9/Rollback8/Journal8 and earlier logged
suites. RestartGate8 incomplete at first assertion, later outcome/auxiliary/publication
and relay suites NOT_RUN; pack/uploads SKIPPED. No old PASS inherited by new source.

Local review/static/signatures/original assertion parity/secrets/public checks are
separate from Windows execution: new RestartGate8/PendingPublication25/compilation
NOT_RUN locally. New CI is justified by these code-derived fixes, not blind rerun.
CI686 recovery cancellation and repair20s/sharing/Python/rebind causes remain UNKNOWN;
PID/Marshal fixture fixes retain scoped verified PASS. BLOCKED_TRANSFER, no retry.
No new feature, live install/service/security/network/credential/release/upload action.

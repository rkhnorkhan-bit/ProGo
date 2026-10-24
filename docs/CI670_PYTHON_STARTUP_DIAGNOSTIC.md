# CI670: bounded read-only Python startup diagnosis

CI670 source9f08110db63dae3df7310b8d024bde5cd3935aa1, tested merge
2be5c1e6d6d86f8b3c7a47a1dffd2bccc7676495, run40110064441/job120919726226.
Full Windows final log independently read. Windows FAILURE; Ubuntu SUCCESS.
Exit0 child start25ms, deadline5044ms; CPU15.625ms/RAM6000640; stdinClosed=True.
At deadline both read tasks WaitingForActivation. After owned kill cleanupExited=True,
both streams RanToCompletion, stdout/stderr0; body/importBootstrap/encodings/os/site/
startupBanner/cleanup all False, pthVisits0. No fixture body execution observed.
Desktop/Instance/Maintenance/installer/Rollback8/relay NOT_RUN on this source.
Earlier Repair/updater and other logged early suites PASS only in their exact scope.

## Comparison and knowledge boundary

CI662 run40089851914/job120860130674: full2782-line Windows log read, source
 db3adc280d500b97487a7991f25c1206248eb5b6 / merge06ae505a9e49082e74e8d09d50b361d28799b15a.
Start110ms/deadline5137ms/CPU0/RAM5984256/streams waiting. No phase trace existed;
its precise internal startup phase and root remain UNKNOWN.
CI663 run40091168500/job120863969106: full15400-line Windows log read, source
44d76b2347cebd23c627003e231de86c261e97e1 / merge86d483cd84da1e3220273a6102f017609e5eaf20.
Exit0 started16ms and completed without deadline, body/bootstrap/encodings/os/site/
cleanup markers True, streams completed. Same contract can pass; not a CI662 fix.
CI670 has no trace even after drain: fixture body not reached. This does not establish
AV/infrastructure/loader/site as root. Empty verbose trace alone cannot distinguish early
loader/startup, selected executable discrepancy, environment effects or other causes.
Production installer resolver/rollback has no demonstrated causal link.

## New test-only missing evidence

Selected command type/application, executable existence/name booleans and presence-only
PYTHONHOME/PYTHONPATH/PYTHONSTARTUP/PYTHONUSERBASE/PYTHONNOUSERSITE flags before own launch.
On exit0 deadline only, before owned kill: main image matches selected executable boolean,
read/probe-error flags, loaded module count capped128 and Python DLL/C-runtime loaded flags.
Read only the owned child; no module path/name list, values, raw native errors, config or
site contents printed/persisted. Synthetic present/absent/redaction checks use no process.
No additional interpreter, retry, -S/-I/-E/site override, timeout/argv/body/stdin alteration.
Original WaitForExit5000, precise failure, kill/join5000, stream3000 and assertions retained.
Diagnostics cannot turn timeout into PASS; loader snapshot failure is recorded as boolean.
No runtime or installer/test fixture changed. New exact Windows evidence NOT_RUN locally.
Journal deferred; privatecandidate BLOCKED_TRANSFER; no transfer/security/network changes.
Readiness58–80/22,+0; CI662/670 root UNKNOWN pending actual missing evidence.

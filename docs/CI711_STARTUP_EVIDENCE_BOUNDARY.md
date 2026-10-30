## CURRENT CI711 bounded startup diagnosis; no guessed fix or new CI

Published source remains bd823bec071f611ee73d4e9045e737cdcb2d5709, treeca28cd6919be80187a3c9c13d924d6e7fc9b77f3;
prior local checkpointea8f68f99cf1c88552b88f909523fe78109b0746 clean/preserved. PR88 independently exact source/open/unmerged;
sole root/no Git/compiler/PowerShell writer; sole source CI711 completed FAILURE, no active CI. Old worktrees untouched.
This stage changes DOCUMENTS ONLY locally; no push/CI/runtime/fixture/counter additions.

All5 CI711 full final logs independently read at exact mergee245c2466270caeb3729f030b461d919217c5b9b:
Core121315564109/2816lines FAILURE; Foundation121315564321/665 SUCCESS all27; Recovery121315564249/282
SUCCESS all5 including Durable20/Source18/Legacy19/MenuResult19/MenuFailure19. Ubuntu121315564294/611
SUCCESS; aggregate121321513314/174 correctly FAILURE despite all4 identical tested_sha. Run
https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40241990486 . No package created; do not substitute older ZIP.
Native durable source20 now scoped PASS, not journal/pre-delete integration/full rollback/production.

At22:59:40 UTC Core exit0-regression exceeded5000ms; elapsed5042/CPU15.625/RAM5906432, live selected
matching image, moduleCount15/Python DLL/CRT loaded. Stdin closed, argc2/-c/shell-disabled/input-redirection
unchanged. Both streams waiting at deadline; after owned kill/join/drain: cleanupExited=True, streams
RanToCompletion/chars0, all Python body/bootstrap/encodings/site/banner/cleanup markers False. Owned snapshot
4threads waiting, Executive1/Other3, running0/ready0, uncapped and collection/thread/wait probeErrors0.
These are instantaneous OS wait categories, NOT synchronization ownership, native function location or deadlock proof.
No AV/load/site/loader/CRT/stdio/runtime cause demonstrated; root UNKNOWN.

Exact launch-path review: CI709->CI711 Test-ProGo diff only64 additive thread diagnostic lines; argv/body,
Get-Command selection, direct Process ownership, UseShellExecuteFalse, input Close, async concurrent stream
reads, WaitForExit5000, kill/join5000/drain3000 remain identical. SSH fixture/requirements/workflow diff empty.
Input close completed before original wait. Empty drained verbose trace argues against emitted Python startup
logs, but does not locate the blocker or prove absence of unobserved I/O. Successful CI710 non-reproduction
not a fix for709/711/696/670/662. Similar deadline/CPU/pre-body markers do NOT establish common root.

Discriminating hypotheses (not conclusions):
1. Supported synchronization dependency (critical section/mutex/thread/COM/ALPC), possibly same-child owner chain.
   Minimal next fact is dependency relation/object class and whether a cycle exists, NOT more WaitReason counts.
2. Unsupported or unresolved wait, e.g. native stdio/CRT/runtime/bootstrap I/O. A one-node/unavailable chain
   does NOT identify I/O or Python function; state stays UNKNOWN, and any later native stack proposal is separate.
3. Executable/pipe ownership mismatch: selected-image equality/stdin closure/owned cleanup currently oppose
   those simple hypotheses; preserve original gates, do not replace expected interpreter or alter site/env/argv.

Candidate measurement: Windows Wait Chain Traversal on THIS owned failed child only, after original deadline,
flags0 (never follow chains into foreign processes), bounded asynchronous query/cancel, sanitized type/status/
cycle/own-vs-external relation only. No IDs/object names/paths/arguments/secret content/raw memory/dump output,
no debugger attach/injection/foreign process inspection or target mutation. Original failure/5000ms/kill/drain stay.
WCT supports listed synchronization classes; one-node result covers both not-blocked and unsupported waits.
Official requirements: https://learn.microsoft.com/en-us/windows/win32/api/wct/nf-wct-getthreadwaitchain
states caller must have SE_DEBUG_NAME; actual runner token enabled state has NOT been measured. Do not infer
AccessDenied or that privilege change definitely is necessary. If enabling SeDebugPrivilege is needed, current
no-security-changes authorization does NOT permit it. No privilege change or WCT call performed in this stage.
Session bounds: https://learn.microsoft.com/en-us/windows/win32/debug/wait-chain-traversal says synchronous
queries cannot be cancelled; any implementation must use bounded async/cancel or isolated owned helper.

Focused reproduction environment: current executor Linux, no pwsh/.NET/Wine/C# compiler. No available authorized
isolated Windows endpoint in this session; do not change network/credentials or create another writer/runner.
Linux Python/mocks cannot reproduce Windows CLR/CRT/pipe startup and must not be counted as native evidence.
Focused native reproduction/WCT therefore NOT_RUN. Another full CI with unchanged evidence would be blind rerun;
none launched. No speculative production fix or further counters added.

Concrete approval boundary before implementation: one isolated Windows CI diagnostic of own synthetic Python
child only; read WCT using existing rights, and ONLY if required, separately approve temporary process-local
SeDebugPrivilege enable in an owned diagnostic helper. No persistent privileges/ACL/owners/services/security policy,
user PC/VPS/tunnel changes; no out-of-process traversal. If denied/unavailable, report BLOCKED/UNKNOWN, no bypass.
No permission for enabling any privilege has been assumed. After approval, review bounded helper/redaction/focused
contracts before one guarded diagnostic publication/automatic CI. Do not start journal/pre-delete integration before
whole CI PASS. All32 suites/strict4-partition gate/native assertions/deadlines remain untouched.

Local aggregate16/review/static/exact launch-path parity/JSON/diff/secrets/public-content PASS. New native Windows
measurements NOT_RUN; no new CI. UNKNOWN incidents/BLOCKED_TRANSFER/no retry/all31+nested55 approved scope/fixed
weights retained delta0. No live install/network/services/security/ACL/credentials/VPS/tunnel/release/uploads.


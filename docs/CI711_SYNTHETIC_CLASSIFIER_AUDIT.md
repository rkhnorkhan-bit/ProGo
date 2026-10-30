## CURRENT CI712 getter-exception diagnostic fix; new Windows verification NOT_RUN

CI712 all5 full final logs independently read: sourcebfa4cbaa964b5ae79995547c20070d3797474b51,
merge d8dfb08cdc0a670904f6a338313b562b027b80f7, run40247448487. Foundation121331921829
PASS27/665lines; Recovery121331921733 PASS6/297lines including actual DurableInput12 and all33 installer suites.
Ubuntu121331921904 SUCCESS/612lines including aggregate layout17. Core121331921885 FAILURE/2794lines:
2026-10-26 00:12:17 UTC Synthetic thread facts failed: state-getter-error at Assert-Facts helper line44.
Aggregate121336105934/174lines correctly FAIL, all4 tested SHA equal. No classifier26 summary; real owned SSH
launcher and subsequent Core/native/Desktop/Instance/relay/package NOT_RUN. This is NOT the old startup timeout.
PR88 remains exact source/open, queued/in_progress/waiting independently0; clean checkpoint7ee61de verified,
sole root/no Git/compiler/PowerShell writer. Historical local-only checkpoints preserved, no reset/clean.

Proven source-level cause: PowerShell ordinary property-access binder WrapGetMemberInTry intentionally catches
getter exceptions and returns null (including StrictMode). Thus [string]$thread.ThreadState can become empty
rather than reach the diagnostic catch/threadProbeErrors. The test expectation is correct: inaccessible state
must be counted as a probe error, not a successfully read other state. Log does not expose individual count
values; the derived empty/other-state behavior is code analysis, not invented log output.
Primary source: https://github.com/PowerShell/PowerShell/blob/v7.4.0/src/System.Management.Automation/engine/runtime/Binding/Binders.cs
WrapGetMemberInTry explicitly returns NullConstant for ordinary Exception; nearby property branch documents
this behavior since V2 even in StrictMode. Public source corroborates mechanism, not a local PS5.1 execution.
https://github.com/PowerShell/PowerShell/blob/v7.4.0/src/System.Management.Automation/engine/CoreAdapter.cs
GetDotNetMethodImpl exposes named accessor methods; MshMemberInfo.cs PSProperty/PSNoteProperty.Value supplies
actual getter/note value. Use PSObject.Properties[name].get_Value() as a METHOD to let the existing catches
receive exceptions for Threads, ThreadState and WaitReason. No native methods/privileges/process probe added.

Bounded fix: only these3 diagnostic property reads plus explanatory comment. Counters/buckets/schema/cap32/
error ownership remain unchanged. Existing synthetic assertions retained; state case strengthened with exact
StateReads1/ReasonReads0/otherState0, plus direct-getter masking witness under same StrictMode/Stop. All26 vectors
remain and complete26 mandatory; no expectation relaxation. Adjacent reason/state-change/collection errors use
the same explicit access so they cannot silently mask an exception either. Real SSH launch/stdio/ownership/
5000ms/flags/environment/native validation/cleanup unchanged. DurableInput stale not-registered comment corrected;
Foundation27/Recovery6/installer33/strict4aggregate/workflow and all native test bodies unchanged.

Local focused PowerShell execution unsupported: no pwsh/powershell/.NET/Mono/Wine, no install/download or new
permission sought. Python static classifier7 + durable12 + aggregate17 =36 PASS; signature/accessor source
review, reverse parity, JSON/diff/secrets/public-content checks local. New classifier Windows26 NOT_RUN until
new CI full logs; DurableInput12 actual CI712PASS remains exact-source scoped, not automatically a new-sourcePASS.
Old pre-body Python startup and all other UNKNOWN incidents remain UNKNOWN; diagnostic getter fix does not
claim startup repair. BLOCKED_TRANSFER/no retry, WCT/SeDebug forbidden/pending actual owner answer, complete
approved31/nested55 and weights retained delta0. No journal/predelete/next feature.
One expected-HEAD source push and one automatic PR run only; no rerun/dispatch. Next SAME new run all5 full logs,
exact merge/classifier26/previous33+Core/strict aggregate; no early fullPASS based on synthetic output.

## CURRENT local-only independent SSH fixture static audit and synthetic coverage

Published source remains bd823bec071f611ee73d4e9045e737cdcb2d5709/treeca28cd6919be80187a3c9c13d924d6e7fc9b77f3;
last published CI711 FAILURE unchanged. Prior local checkpoint67d3dc4d0a28c69ee3471fbc8aee4612a89777e0 clean/
preserved; earlier ea8f68f checkpoint untouched. PR88 independently read exact source/open, only root active,
no Git/compiler/PowerShell writer. Owner independently confirmed queued/in_progress/waiting=0; no new CI/push.
WCT/SeDebug explicitly NOT AUTHORIZED; approval question PENDING until actual owner response. Do not answer
for owner or treat sleeping/silence/24x7 permission as protected approval. No WCT/native stack/memory/ID probe.

Chosen independent slice: static launch/stdio/ownership audit + prepared focused synthetic unit coverage of
EXISTING thread classifier, without changing its buckets/counters or any launch/runtime behavior. Root UNKNOWN.
Concrete finding is TEST COVERAGE GAP only: existing4 synthetic checks cover role/redaction/empty/cap40/missing
collection, not complete14 enum mapping, invalid enum, state/reason getter failures, state transition, exact32
boundary or preservation of original failure after diagnostic error. No demonstrated runtime defect found.

Enum assessment from https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.threadwaitreason?view=netframework-4.8.1 :
SystemAllocation/EventPairHigh/EventPairLow/LpcReceive/LpcReply/PageOut/Unknown legitimately fall into current
waitOther; PageOut not called waitPage by current coarse schema. Separate counters would distinguish broad OS
semantic categories (e.g. LPC vs paging), NOT object/owner/function/CRT-vs-Python startup or a common root. No
currently authorized corrective decision follows solely from this extra granularity. Keep classifier unchanged;
do not add7 fields merely to collect more counters. ThreadWaitReason valid only in Wait state, can change between
reads; current catch increments waitProbeErrors rather than inventing a reason. CI711 had0 probe errors, so
this race is not its demonstrated cause. Other3 cannot be retrospectively assigned particular enum values.

Static audit findings/limits:
- Get-Command selects exact executable; direct Process object owns start/handle/kill/dispose, no raw-PID lookup
  used for termination. Selected image equality facts already oppose simple interpreter mismatch, not root proof.
- UseShellExecuteFalse, redirected stdin closed before original wait, both output reads started asynchronously.
  WaitForExit5000 follows launch/stdio setup; wall5042 includes setup overhead, not a changed5000 test allowance.
- No argv/site/environment/flag changes, inherited stdin, retry/sleep or bypass. Empty drained verbose streams
  indicate no observed Python markers; they do not identify native startup call or exclude every I/O hypothesis.
- Timeout remains precise FAIL/native validation NOT_RUN, own kill/join5000 and output/error drain3000 preserved.
  Classifier catches read failures; cleanup failures are separate failures, never false PASS. No proven code fix.
- All32 installer suites, source/durable proof paths, strict4 aggregate/workflow/deadlines/assertions untouched.

Prepared tests/TestSshFixtureThreadFacts.ps1: AST parses existing driver, extracts ONLY exact named classifier,
not dot-sourcing build driver. Synthetic CLR getter classes only; no Process.Start/PInvoke/probes/system settings.
26 vectors: all14 ThreadWaitReason values; invalid enum; nonwait Running/Ready with reason getter forbidden;
unknown state; state getter error; reason getter error; Wait->Running before reason; collection getter error;
32 vs40 cap and no getters after32; fixed schema/redaction/empty; original FAIL survives diagnostic getter error.
Invalid enum uses Enum.ToObject, no unsupported PowerShell enum literal conversion. Fully qualified CLR state
references avoid name shadowing. Every output constrained to fixed boolean/count schema; private sentinel denied.
NOT_RUN: actual PowerShell parsing/Add-Type compilation/26 vectors, because local executor Linux/no pwsh/.NET/Wine.
No Windows or model/mocked execution is falsely reported as native PASS. Harness NOT added to32 suites or workflow.

Locally executed tests/test_ssh_thread_facts_contract.py6 PASS: exact classifier and launch hash freeze,
complete prepared enum/vector set, safe AST extraction, no native/process actions, original timeout/FAIL order.
These are STATIC contracts, not an emulator of PowerShell or proof of26 vector execution. Existing aggregate16
PASS; total22 local Python checks PASS. Review/static/signatures/parity/JSON/diff/secrets/public-content PASS.
See CI711_SYNTHETIC_CLASSIFIER_AUDIT.md. Separate clean LOCAL commit only; no publish/new CI.

Next independent authorized work (without claiming Coregate PASS): manual/static review of existing code and
requirements-to-evidence/docs consistency; refinement/review of synthetic tests without protected execution;
read-only planning of remaining package/acceptance gates (no delivery retry). None grants journal/pre-delete/
restore/runtime integration or next gated functionality. Focused actual PowerShell26 execution is pending an
available authorized Windows test environment; no new CI for this unproven hypothesis in this stage.
WCT question stays PENDING FOR OWNER MORNING; do not repeat/answer it on their behalf. If later separately
approved, bounded diagnostic scope still applies. No protected probe or privilege enable performed here.
All31/nested55 scope/fixed weights retained delta0, UNKNOWN incidents/BLOCKED_TRANSFER/no retry retained.
No live install/services/network/security/ACL/credentials/VPS/tunnel/release/uploads; journal/pre-delete untouched.


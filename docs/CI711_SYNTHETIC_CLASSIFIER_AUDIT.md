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


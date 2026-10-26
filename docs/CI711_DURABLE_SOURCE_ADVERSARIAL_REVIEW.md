## CURRENT approved CI wiring for supplemental Durable12 and classifier26

Base local clean680d2360335b6830108c51e0d382e1e166b5ddfd; published parent
bd823bec071f611ee73d4e9045e737cdcb2d5709/treeca28cd6919be80187a3c9c13d924d6e7fc9b77f3.
Whole unpublished chain reviewed: docs + four added test files, plus minimal existing test-driver/layout wiring;
production/native source, original32 suite bodies, fixture launch/flags/environment/5000ms/deadlines unchanged.
PR88 exact parent OPEN, sole root/no competing writer; queued/in_progress/waiting independently0 before changes.
Owner explicitly authorizes one guarded source push and automatic existing PR CI, not WCT/privileges.
New classifier26 invoked once in Core after original4 synthetic checks and before owned SSH fixture launch.
New DurableInput12 invoked once after DurableSource20 in Recovery: Foundation27 unchanged, Recovery6,
installer total33 (original32 retained once). No new job/dispatch/rerun; exact4-partition aggregate unchanged.
Layout tests enforce new suite once/order/partition and classifier once/Core location. Aggregate refusals retained.
Before CI: classifier26/DurableInput12 Windows compilation/native execution NOT_RUN. Static/Python PASS is not
Windows evidence. Source18/Durable20 scoped CI711PASS, whole CI711FAIL at startupUNKNOWN, not fixed by wiring.
One automatic CI intended; exact source/run recorded in next local checkpoint. Public PR artifact guards remain
OFF, no uploads/release/install/delivery. MEDIUM fault injection remains specification only; no hooks added.
WCT/AdjustTokenPrivileges/SeDebug/protected probes, memory/stack/IDs and production mutations absent from added
executable code. Historical docs mentioning a future WCT proposal are NOT implementation or authorization.
UNKNOWN incidents, pending WCT question, BLOCKED_TRANSFER/no retry and all approved31/nested55 retained.
Score delta0. Next read same run full5 logs + exact merge; require classifier26/Durable12 and all prior suites,
strict same-SHA aggregate before any journal/predelete integration. Do not infer root cause from non-reproduction.

## CURRENT local-only supplemental DurableSource input tests (Windows NOT_RUN)

Base clean checkpoint706d280f9330e6cc9dae057d4ba1c193070cb405. Test-only preparation, no production hooks,
runtime/scripts/workflow/existing20 or 32-suite changes, push/CI/network/privilege operations. New standalone
InstallerLegacyDurableInputTests.ps1 prepares12 real Windows cases via existing public native APIs:
read-oversize(70001 bytes), read-empty, read-trailing-data(one appended byte); writer empty GUID, null/short/
nonhex/lowercase snapshot, null source; reader empty GUID, null/nonhex snapshot. Six invalid writer + six
reader cases, every case first proves actual owned capture, valid durable persistence, disposed capture,
actual removed receipt and exact positive-control disk bytes. Invalid writer must throw underlying IOException,
not an unrelated COM/reflection failure, return no proof and preserve full inventory; valid subsequent write/read
must still work. Reader refusal must return no bytes and preserve complete mutated inventory. Private files,
false authority flags and exclusive handle reopening checked; cleanup limited to new guarded GUID temp root.
No live shortcuts: explicit Programs/Startup/install paths all under that root; synthetic executable never run.

Ready for ONE future explicitly authorized focused Windows PowerShell5.1 run from clean source:
`powershell.exe -NoProfile -File tests/InstallerLegacyDurableInputTests.ps1`.
Requires existing Add-Type compiler and native ShellLink COM in isolated Windows. Actual execution/compilation
NOT_RUN here (no PowerShell/.NET/Windows). Not wired into32 suites or CI; existing suites are not rerun now.
Document mutations exercise the public rejection contract: hash/native identity may reject before inner size/
canonical parser branches. Do NOT forge proof via reflection or claim these cover those internal branches.
No new accepted evidence on this source: Source18/Durable20 remain scoped PASS only on CI711 exact merge
 e245c2466270caeb3729f030b461d919217c5b9b; globalFAIL/startupUNKNOWN, classifierPS26 NOT_RUN.

### MEDIUM fault-injection specification — NOT implemented or executed

Existing API hardcodes FileStream, so deterministic Write/Flush/readback fault injection and exact late-cancel
checkpoints are unavailable without a separately reviewed test seam/harness. Do not add hooks now, change
runtime, use sleeps/poll races, permissions/disk-full changes or count textual assertions as crash execution.
Future harness must use only own temp resources and preserve full native identity/sharing and deadline contract.
Every row retains original bytes, private/foreign inventory and no receipt/deletion/restore/Launch authority;
check exact exception/cancellation, proof absence, owned handle release and non-overwrite on re-entry.

| Injection boundary | Specific experiment and expected fail-closed observation |
| --- | --- |
| Write prefix then failure | Deterministic stream seam writes exactly N bytes (0,1,header−1,header,payload−1), then IOException. No returned proof; actual prefix retained; original unchanged; same GUID retry CreateNew refuses; never silently reuse/delete prefix. |
| Flush failure | Full Write accepted, Flush(true) throws injected IOException. No proof even if full bytes visible; read-back is not durable-commit evidence; unclaimed file retained and no mutation authorized. |
| Same-handle readback | After successful flush, deterministic read returns short EOF or throws; separate variant corrupts a byte through a controlled seam before readback. No proof, no fallback acceptance; verify real partial/corrupt bytes and complete cleanup. |
| Independent-path readback | After first readback, controlled open/read failure or mismatched actual origin/bytes at second verification; no proof. Do not attempt replacement while sharing pins forbid it or silently weaken sharing to make injection work. If seam cannot retain semantics, record experiment blocked. |
| Late cancellation | Hook cancels after Flush, after first readback and after second readback but BEFORE final ThrowIfCancellationRequested. OperationCanceledException, no proof, retained file not reusable. Cancellation after final check is not promised to revoke a returned proof; it still grants no mutation authority. |
| Abrupt process termination | Isolated child signals an explicit checkpoint after CreateNew/prefix, full Write before Flush, Flush before readback, and final readback before proof handoff; controller terminates ONLY its owned child and waits/drains boundedly. Fresh verifier inspects actual file/native origin/bytes. No external writer-issued proof or trusted journal reference exists: orphan/partial stays untrusted, no auto restore/delete or success. |
| Power-loss durability | Process termination is NOT storage power loss. Actual power-loss test needs separately approved disposable VM/storage harness; no such test authorized or claimed. |

Crash experiment must execute the actual existing persistence path with bounded out-of-band checkpoint control;
no line/string match or precreated prefix substitutes for a real terminated operation. Restart proof import and
journal/predelete integration remain out of scope and blocked, so do not manufacture a deserialized authority.

Local: static12 (existing8+new4), classifier6, aggregate16 =34 Python PASS. Windows supplemental12 and
classifier26 NOT_RUN; no runtime PASS inferred. Review/parity/signatures/JSON/diff/secrets/public-content local.
LOW gaps now have executable prepared black-box cases, not verified closure. MEDIUM gaps remain open with
specific experiments above. UNKNOWN incidents/BLOCKED_TRANSFER, pending forbidden WCT/SeDebug and all
approved requirements unchanged; score delta0. Next safe work: review prepared fixture/specification or evidence
consistency; focused Windows12 only after authorization/environment availability, no CI or protected probes now.

## CURRENT local-only DurableSource adversarial review

Base clean checkpoint a6ab425de5e85372ea74a4167230e20a841844f1; no competing Git/compiler/PowerShell process observed.
No remote query/network, push, CI, runtime, existing fixture, workflow or security change in this stage.
Published source bd823bec071f611ee73d4e9045e737cdcb2d5709 remains the recorded PR source (not re-queried).
CI711 merge e245c2466270caeb3729f030b461d919217c5b9b: Source18/Durable20 scoped PASS;
whole CI FAILURE at Core startup, root UNKNOWN, package not created. Standalone PS classifier26 NOT_RUN.
BLOCKED_TRANSFER/no retry and pending NOT-AUTHORIZED WCT/SeDebug remain unchanged. Score delta0.

Review result: no demonstrated production defect in the bounded storage/reader prerequisite.
MEDIUM coverage gap: Durable20 partial-existing/partial-after model unclaimed prefix/truncation; no injected
short stream.Write, Flush(true) failure or independent readback failure inside publication, no cancellation
between Flush/readback/proof return, and no actual process/power-loss crash. These are NOT executed crash tests.
LOW coverage gap: durable reader oversize/empty/extra trailing bytes and Guid.Empty/bad snapshot format are
not individual Durable20 cases. Source18 oversize applies to original capture, not durable document reader.
Static guards exist (70000 envelope bound, 65536 payload, canonical full equality/hash, Guid/hash validation);
8 new source-contract tests execute locally but do NOT close native failure-injection/race coverage gaps.
No production patch justified by these coverage observations; future focused Windows cases require a separate stage.

Evidence: ApplicationShortcuts.cs PersistLegacyRollbackSource retains original read pin, validates full native
volume/index/creation and bytes + semantic ownership, pins non-reparse root/backups/Menu directories, creates
only a new fixed GUID filename. WriteThrough + Flush(true) precede exact same-handle and independent-path
readback; independent immutable proof is returned only after final cancellation check. No overwrite/delete or
partial-file reuse/cleanup fallback. Failed publication may retain its new incomplete file, intentionally no proof.
ReadDurableLegacyRollbackBytes derives its own fixed path; validates operation/snapshot/install/location,
removed receipt/original origin/hash, actual absence/current replacement, storage native origin, document hash,
bounded complete read and whole canonical envelope; repeats receipt check before releasing copied bytes.
CreateNew plus sharing pins reject pre-existing hardlink/file destinations; full native identity rejects identical
bytes/copied creation replacements. Static reparse checks and directory pins are present; redirected-backups is
an actual CI711 native case. Concurrent ancestor/final-component substitution is not simulated by this review;
no claim that source-text checks prove all Windows namespace races. Receipt state is an observation, not a lease
for a later mutation; future mutation must revalidate against independently trusted locations immediately before use.

Future journal trust boundary (NOT integrated): snapshot hash here is caller-provided binding, not verified
snapshot authority. Writer-issued proof is not a restart-persisted/importable capability. Never reconstruct it
from self-declared envelope paths/flags or accept hash-only legacy receipts. A future authorized integration
must independently verify snapshot, operation/install/location and origin, persist a validated reference before
delete, verify actual source bytes/storage identity and current receipt/runtime state, and fail closed on missing,
foreign, invalid, conflicting or partial evidence. No intent-based restore, replay, FullRollback/InstallSucceeded
or Launch authority follows from this prerequisite. Journal/predelete integration remains gated on full Core CI.

Candidate acceptance checklist — existing requirements only, no delivery or execution now:
- Identity: accepted version/source/exact tested merge, complete package inventory, SHA256/size and matching
  manifest; successful strict same-SHA aggregate required. CI711 has no accepted candidate package.
- Clean install: standalone prebuilt/offline path without build/download fallback; explicit protected-action
  consent and PGA04 opt-in OFF; preserve settings/vault/keys and custom resources. Live approval still required.
- Upgrade/offline/cancel: both approved update/fallback paths, immutable accepted request/version/hash, maintenance
  ownership, bounded Stop/Cancel/Retry/finally, offline/hash/refusal paths, no duplicate request or unsafe Launch.
- Rollback: controlled copy/self-check failure, verified old bytes/absence, no overwrite of external changes,
  original failure retained and Launch blocked; full auxiliary/Legacy recovery and retention remain open.
- SSH: selected key/direct/agent startup and recovery, SSH→SOCKS5→HTTP, slow-I/O cancellation/close/recovery
  and performance against approved criteria; isolated historical evidence is not user-PC acceptance.
- VPS: separately authorized disposable acceptance environment only; actual live VPS/credentials/network/security
  work remains NOT_RUN and forbidden in this stage. Personal external MTProxy is not ProGo acceptance.
- Other PC: approved portable/standalone installation, paths/choices/private-data preservation and installed
  user scenarios on another Windows PC; delivery/install/owner acceptance NOT_RUN, transfer BLOCKED.
These do not replace full approved31/nested55/F23/F31/commercial requirements or signing/UI/package gates.

Local tests: new static8 + existing classifier static6 + aggregate16 =30 Python PASS; actual PS26 NOT_RUN.
Review/static/signature/parity/JSON/diff/public-content checks recorded with this local checkpoint only.
Safe next work: improve static coverage-map traceability or audit existing acceptance/evidence consistency;
prepare focused native failure-injection specifications without execution. No journal/predelete integration,
WCT, new privilege, delivery retry, runtime recovery/retention or live operations authorized by this review.


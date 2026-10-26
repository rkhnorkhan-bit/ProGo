## CURRENT CI719 verified; actual-operation model blocked, independent test-only counterexample

CI719 all5 full logs independently read: sourced454cfc114c4dbeace0a5f0bda7f88e6142e53b3,
merge936199c90a2c2de62662fc3b8ac2659de9f1ae71; Namespace41/fresh-process1, PhysicalPins50/Pinning39/PreDelete23/
Restart26/JournalReference20/DurableInput12/Foundation27/Recovery6 PASS. Core classifier26/SSH5/Desktop6394/
Instance30/Maintenance126/Shutdown47/HomeVPN4816/Portable91, Python98PASS2SKIP, Ubuntu and aggregate121420106103
exact4SHA PASS. Runner ZIP686643 SHA2568a48240d045f0c6c16aeceff8aecca83a6ff4185451e9b6da91dd95879127a10,
upload skipped; not PC acceptance. Checkpointd226f318e0c5a6679bb4dd069129b0eb95d95cc6 clean, PR88 exactsource/
merge/open; no other active agent or queued/in-progress/waiting CI before this stage.

DEPENDENT MUTATION BLOCKED_MODEL: exact-target safety and global namespace/metadata immutability are different
contracts. Existing Native39/Physical50 positive pins do not reserve missing children; Namespace41 already
proves appearance while pinned and observed-only after the fixture restores current absence. Another enum,
mutex for cooperating ProGo processes, or directory oplock must not be promoted to atomic exclusion. No actual
mutation primitive is implemented, no assertion weakened, no origin field removed and no scope waived. Full
threat analysis, primary sources and the pending owner decision are in F23_LEGACY_DURABLE_SOURCE.md.

Independent allowed work only: test-only native FILE_WRITE_ATTRIBUTES counterexample on our new complete
fixture's synthetic runtime ProGo.exe. Creation time change uses a SECOND native handle with minimal attribute
access, share7, OPEN_REPARSE_POINT, same-handle closure; no new privilege/security/ACL action. Ordinary .NET
Framework File.SetCreationTimeUtc asks for FileAccess.Write (reference source inspected), so would test the
wrong contract; it is not used for the adversarial write. Expected six checks: valid pins, changed creation+
unchanged bytes/object pin, VerifyPinned false, no mutation/success authority, exact current origin restored
under the STILL HELD pin (not no-ABA history), then private/foreign bytes and exclusive reopen preserved.
Scope/reparse/temp-parent guards; metadata cleanup occurs before dropping native pins, never edits a stale
unprotected path. Safe fixed checks/type diagnostics only, no raw path/timestamp/credentials/journal output.
This is a same-process second-handle experiment, not measured cross-process exclusion or power-loss proof.

New Windows MutationThreat6/C# test-shim compilation NOT_RUN. Local78 Python static tests PASS, not Windows
execution. Nested once only in existing complete case; old33/classifier26/strict4aggregate/deadlines/SSHflags/
environment and ALL production C#/installer/workflow bodies remain unchanged. One reviewed guarded test-only
push and automatic CI allowed for this concrete adversarial experiment, no blind repeat or duplicate.
UNKNOWN incidents/BLOCKED_TRANSFER/no delivery retry/deletion-power-lossNOT_RUN/fullscope/weights/delta0 remain.
WCT/SeDebug/new privileges/live install/services/network/security/ACL/credentials/VPS/tunnel/release/uploads
remain prohibited. A green counterexample CI would NOT close BLOCKED_MODEL, authorize deletion, prove durable
outcome/restore/FullRollback/production or change approved requirements.

PENDING OWNER QUESTION, NOT ANSWERED: approve a separately reviewed exact-owned-object authorization model
based on retained positive proofs and target handle (without global namespace/ABA freeze), OR retain strict
namespace exclusion and separately agree a proven isolation design/scope? Neither option authorizes live or
security changes. Until an explicit decision, mutation stays blocked. Independent next work: same new CI full5
logs/MutationThreat6/old33/Core/strictaggregate and safe static requirement audits, not mutation/integration.

## Actual-operation threat model decision — BLOCKED_MODEL, no mutation implementation

Threat actor: an ordinary non-cooperating writer already allowed to access these paths, including another
process of the same account. Kernel/admin compromise is not required for the counterexamples. One Codex/repo
writer or one ProGo maintenance lease is not evidence that every filesystem actor cooperates.

Required distinction:
- Exact held target identity and protected file data can remain bound to pinned positive proofs. A future
  operation on that retained native handle does not re-resolve a replacement pathname.
- Missing names, arbitrary new child names and global namespace histories are not positive objects held by
  those handles. An actor can create/move a child between final observation and operation, or perform ABA.
  Two executions can yield the same endpoint observations but different intervening histories. Current719
  actual tests directory-pins-do-not-reserve-absent-child and current-absence-observation-is-not-ABA-history-
  or-deletion-proof provide scoped Windows evidence, not a new reservation protocol.
- Attribute access is distinct from file data sharing. Mutable creation time in the full native origin must
  not be silently redefined as immutable or removed from current verifiers. Independent MutationThreat6
  specifically measures this boundary on a second ordinary FILE_WRITE_ATTRIBUTES handle.

Primary sources independently inspected:
https://learn.microsoft.com/en-us/windows/win32/api/winioctl/ni-winioctl-fsctl_request_oplock
Directory R/RH child-enumeration breaks are advisory; child changes need not await acknowledgement. This
cannot supply the requested namespace reservation. No oplock or notification probe is implemented.
https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-createfilew
Sharing modes do not constrain attribute/extended-attribute requests; access is not a global metadata freeze.
https://github.com/microsoft/referencesource/blob/main/mscorlib/system/io/file.cs
SetCreationTimeUtc uses OpenFile(FileAccess.Write); its helper constructs a write FileStream. Hence the new
fixture uses minimal native attribute access, not that higher-level setter as a substitute.

No claim is made that every possible Windows primitive is impossible. The existing permitted protocol has no
proved atomic namespace exclusion primitive. Adding ACL/principal isolation, a transactional filesystem
contract or a cooperative-writers-only assumption requires its own proof and authorization; an ACL edit alone
must not be assumed to exclude other same-account actors or pre-existing handles. None is implemented or
approved here. Finite placeholders likewise cannot reserve an arbitrary unknown child namespace or be added
to snapshot/journal schemas unnoticed. No permissive fallback is introduced.

The owner decision is pending: separately agree exact-owned-object semantics with retained positive evidence
and honest outcomes (explicitly not global namespace/ABA immutability), or retain that stronger exclusion
requirement and approve a proven isolation design. This is NOT an automatic reduction of scope; no chosen
option, security action or deletion authorization is inferred. Existing source remains observation-only.
New counterexample tests are independent of this choice and cannot lift the block even when passing.

## CURRENT CI718 verified; namespace operation-boundary observation prerequisite

Baseline full5 logs independently read: sourcea5d0e5d5d070f16157f8db00a0d411ecacd89f63,
mergeaea0706a6e8341ae8c5ddbedc0e1315d194e9a19; CI718/40270033424 SUCCESS. PhysicalPins50/fresh-process1,
Pinning39/PreDelete23/Restart26/JournalReference20/DurableInput12/Foundation27/Recovery6 PASS; Core classifier26/
SSH5/Desktop6394/Instance30/Maintenance126/Shutdown47/HomeVPN4816/Portable91, Python92PASS2SKIP, Ubuntu and
aggregate121408506041 exact4SHA PASS. Runner ZIP685496 SHA2567da5e5259962497eb994cd55c913880a5d062970cab1c6b9e895b50ce3f28b28,
upload skipped; no PC/delivery acceptance. Original checkpoint0c3ca31de153033c08a07378aa1c96a475b32a95 clean,
PR88 HEAD exact baseline/open; no other active agent or queued/in-progress/waiting CI before changes.

Acceptance/scope: read-only namespace observation under physical and exact-original pins. Derive snapshot files,
required directories and missing leaves from the immutable anchored reference, verify raw document hash before
parsing, operation/install/snapshot/journal bindings and payload-name whitelist before enumerating. Snapshot and
journal must have exact observed name/type SETS, not counts alone; reject unknown/duplicate/missing/reparse
nodes before descending. Only runtime leaves in the existing approved inventory are involved; settings/vault/
keys/private/foreign install-root files are not declared forbidden or enumerated as snapshot payload.

Boundary observer first independently rehydrates reference, acquires actual physical pins, checks namespace,
acquires exact original pins, repeats namespace, verifies physical native origins/hashes and checks namespace
again. Nested finally disposes BOTH leases on all returns; cancellation including wrapped PS method exceptions
is propagated, never reported as observed success. No native/API/ordinary installer body changes, no deletion,
metadata publication, restore, callback/action capability or live flow integration. Only blocked/observed-only.

TOCTOU boundary is intentional and explicit: directories DO NOT reserve absent children. Enumeration is not
an atomic namespace transaction, cannot exclude transient ABA events and cannot authorize a later operation.
observed-only means repeated current observations matched while resources were retained; the helper then
releases them, so its string must NEVER be used as permission for subsequent deletion/replay/restore/Launch.
Future mutation still needs a safe in-scope same-handle protocol with namespace/absence policy and retained
coherent evidence at the actual operation boundary. Intent/current absence never proves our deletion. Actual
delete/durable result/post-delete trusted rehydration/restore/error settlement remain separate gates. Full
auxiliary/runtime recovery/retention/PGA04/updater/package/F31/owner/commercial scope remains OPEN, no scores
raised. Original error/no Launch, private-file exclusions and external-change refusal remain unchanged.

Prepared Namespace41 Windows checks: 19 positive/mutation/cancel/cleanup checks plus22 existing refusal states.
Actual synthetic child creation WHILE directory/physical/original pins are held demonstrates non-reservation;
new snapshot leaf is rejected although existing physical bytes still verify. Unknown journal record/snapshot
directory and missing manifest are refused without deletion/repair; own fixture bytes are retained, restoration
of the fixture's current namespace yields observed-only but not ABA/history/operation proof. Read-only inventory
and exclusive reopen prove cleanup; actual fresh-process child requires observed-only with no mutation marker.
Nested once in existing PreDelete23, all33/classifier26/strict4aggregate/deadlines/SSHflags/environment unchanged.
Local72 Python static tests PASS, not Windows/native execution. New Namespace41/fresh-process Windows NOT_RUN.
UNKNOWN incidents/BLOCKED_TRANSFER/no retry/power-lossNOT_RUN/fullscope/fixedweights/delta0 retained. No WCT/
SeDebug/privileges/live install/services/network/security/ACL/credentials/VPS/tunnel/release/uploads.
Next SAME new exact-source CI full5logs/Namespace41/all33/Core/strictaggregate, then separately resolve safe
actual-operation namespace semantics before any owned deletion/result/restore integration. Stop after one stage.

Prior evidence/history:

## CURRENT CI717 verified; existing physical-material pinning prerequisite

The previous execution environment was genuinely unavailable, then one authorized pwd probe executed in the
same workspace. Original checkpoint8a7091377def8cddbece4237cf0a1ca43166d7a4 found clean; parent/source
2cdae18a737eef593c520b155932bc54a82b4a27 matches open PR88. Metadata/HEAD reconfirmed, no other agent or
queued/in-progress/waiting CI. CI717 all five full logs were independently read: merge507a64bc295c3a8849467ec2fbebe8d7cc7d8356,
Pinning39/PreDelete23/Restart26/JournalReference20/DurableInput12/Foundation27/Recovery6, classifier26/SSH5/
Desktop6394/Instance30/Maintenance126/Shutdown47/HomeVPN4817/Portable91 PASS; Python86PASS2SKIP, Ubuntu and
aggregate121392573749 exact4SHA PASS. Runner ZIP683026 SHA25605198ac208fcdc0a54bf88e2162edb8f7de1799a95b785ff47947ffcc74fe6a7,
upload skipped; no delivery or PC acceptance. Startup UNKNOWN is not closed by this non-reproduction.

Acceptance: retain the exact verified existing runtime/snapshot/manifest/journal physical objects and all
required ancestors under non-following native read handles through validation and observation. First derive
a bounded read/pin plan from a reference whose exact UTF8 hash matches the independently rehydrated immutable
anchor. Whitelist payload leaves before opening any candidate; derive fixed operation/snapshot/admission names.
Pin all existing planned files and ancestors, then independently rehydrate actual snapshot/runtime/journal/
source/reference/seal/admission context AGAIN. Compare anchor origin/hash, require actual original-present
state and VerifyPinned native origins/hashes on those same handles. Never trust serialized flags or paths as
proof. Hold resources until idempotent Dispose; cancellation/failure unwinds every acquired handle. Verification
cancellation does not pretend the retained lease is disposed. No mutation, delete, restore, publication or live
installer-flow integration. Public native read/pin plan is NOT evidence; only the trusted wrapper verifies it.

Boundary: existing physical-file pins are not a namespace transaction or reservation for absent snapshot leaves
or new journal records. Existing file bytes/replacement are denied; native metadata/origin is reread on the same
handles by VerifyPinned, which must run at any future operation boundary. This stage does not authorize a future
operation on a one-time bool. Coherent context must be rechecked with pins held at that boundary and combined
with the retained original handle; missing/extra/conflicting namespace state remains fail-closed. No deletion or
post-delete result is inferred from intent/absence. Actual deletion, durable result/post-delete rehydration and
restore/error integration remain separate gates. Full auxiliary rollback/runtime recovery/retention/PGA04/
updater/package/owner/commercial requirements retained; error/no Launch/private-file guarantees unchanged.

Prepared PhysicalPins50 Windows checks reuse all old23 cases: complete positive (28 checks) and22 existing
negative/foreign/replaced/reparse/partial contexts refuse. Positive actual write/replacement denial on runtime,
snapshot/manifest/journal, own required-parent rename denial, original-pin coexistence, cancellation/Dispose,
partial/tamper, busy input, native invalid/duplicate/traversal/late-open unwind and verified reacquisition.
Existing fresh-process child also verifies physical pins from paths only. All33/classifier26/strict4aggregate/
SSH5000/flags/environment/old deadlines preserved; no duplicate job/dispatch. Local66 Python static tests PASS,
not compilation/native proof. New Windows PhysicalPins50/fresh-process/native sharing NOT_RUN locally.
UNKNOWN incidents/BLOCKED_TRANSFER/no retry/deletion-power-lossNOT_RUN/fullscope/weights/delta0 preserved.
WCT/SeDebug/privileges remain unauthorized. No live install/services/network/ACL/security/credentials/VPS/
tunnel/release/uploads/delivery retry. Next SAME new CI full5logs then separately close operation-boundary
namespace/context verification and same-handle deletion with honest durable result; no full rollback claim.

Prior evidence/history:

## CURRENT CI716 verified; bounded Legacy deletion-pinning prerequisite

All five CI716 final logs independently read: source7ccefc17d2b8779adea38ec8d449c664b62c982c,
merge73ddfb86fc22a04239dfb5a0ab334d2dbb716e58. Foundation27/Recovery6, PreDelete23 actual fresh-process1,
Restart26/JournalReference20/DurableInput12 PASS. Core classifier26/SSHnative5/Desktop6394/Instance30/
Maintenance126/Shutdown47/HomeVPN4816/Portable91 PASS, Python80PASS2SKIP; Ubuntu and aggregate121381799552
same4 exactSHA PASS. Runner ZIP681487 SHA256fa36c906e5022e7fad7fd9ed7cfdb8b0c7e02b6b14b29eb7b4ee80e66fdf1234,
uploads skipped; PC/delivery acceptance NOT_RUN. No active queued/waiting/in-progress CI or other agent before
changes; checkpointa4c1665bbcf422bc535ced6a09a0ea874f10961a clean and PR88 exactsource/open/draft.

Acceptance/gate decision: the existing intent is not retained across a future deletion call. Add a disposable
pin-only lease for the verified original-present graph. Hold non-reparse install/backups/Menu directories,
source/reference/seal/admission read handles; verify whole graph; acquire READ|DELETE/shareREAD/open-reparse
handle on original; compare complete native origin and exact bytes to the verified immutable proof, operation,
install/location/snapshot bindings. Keep all handles until Dispose. No write, delete, restore, receipt publication
or live installer integration. Failure/cancellation closes all acquired resources; Dispose is idempotent and
attempts every close. Read-only reacquisition is not mutation replay. All authority/success flags remain false.

Important remaining boundary: this lease does not pin runtime/snapshot/journal material. Trusted PS context is
verified at acquisition, but future mutation must independently pin/recheck physical context immediately before
mutation, use the SAME retained original handle, settle accepted deletion honestly and durably publish a result.
Absence/intent is never proof of our deletion. Post-delete trusted rehydration, restore, full auxiliary outcome,
error integration, retention/runtime recovery and remaining approved installer/updater/PGA04/F31/owner/commercial
requirements remain OPEN; original install error/no Launch and private-file guarantees unchanged.

New PinGate39 checks reuse the existing 23 isolated admission cases: positive exact-handle acquisition/false
authority, 5 leaves x write/replacement refusal, idempotent Dispose/exclusive reopen/reacquisition, busy original
refusal/unwind, cancellation; remaining22 cases refuse pinning. Actual fresh-process child also acquires/releases
pins from paths only. No deletion test claimed. Nested once in existing PreDelete23; all33 suites/classifier26/
strict4aggregate/deadlines/SSHflags/environment/old native and installer bodies preserved. Local60 Python tests
PASS (static contracts, not native compilation). New Windows pin39/native sharing/compilation NOT_RUN locally.
One reviewed expected-HEAD push and automatic CI only; no retry/dispatch. UNKNOWN incidents/BLOCKED_TRANSFER/
power-lossNOT_RUN/full scope/fixed weights/delta0 preserved. WCT/SeDebug/privileges remain unauthorized. No live
shortcuts/install/services/network/credentials/security/VPS/tunnel/release/uploads/delivery retry.

Next: SAME new CI full5logs/exactSHA/pin39/PreDelete23/old33/Core/aggregate. After fullPASS separately scope
physical-context pinning and same-handle owned deletion with trustworthy durable post-delete evidence. No full
rollback, production or PC acceptance claim.

## CURRENT pre-delete admission — durable intent, original-present classification only

Dependency decision: CI715 restart reader intentionally requires the owned original still present. Removing
that check and trusting saved origin/hash after disappearance would invent post-delete ownership. Before
any owned deletion integration, persist the exact verified pre-delete evidence graph in a separate durable
admission. This stage adds publication/read-only classification ONLY; no Legacy deletion or restore method.

New-ProGoInstallerLegacyPreDeleteAdmission validates actual live context with existing snapshot/runtime/
canonical admissions, independently rehydrates sealed source/reference, derives operation/snapshot/document
from that verified context, then invokes native PersistLegacyPreDeleteAdmission. Fixed own path backups/
installer-legacy-admission-GUID.bin, CreateNew/WriteThrough/Flush(true), same-handle exact and independent-path
readback, actual original/source/ref verification before and after publication, final token/context/state check.
Immutable reference proof is a validated input; native serialized admission bytes are NOT caller claims.
Admission canonical bytes bind complete existing seal context (operation/snapshot/install/location/original/
source/ref native identity/origin/hashes), actual seal hash/native origin and admission's actual OWN native
origin. Missing/changed/foreign source or seal cannot publish valid intent; existing/partial/foreign admission
never overwritten/reused/deleted. Failure can retain unclaimed new bytes, no success/mutation authority.

Get-ProGoInstallerLegacyPreDeleteState accepts only journal/install and independently trusted location paths,
rehydrates actual snapshot/runtime/source/reference/seal, derives fixed operation/path, verifies admission by
recomputing expected whole canonical bytes against actual safe native handles, then repeats rehydration and
compares reference origin/hash. OpenLegacyRestartRead rejects reparse final leaves; directory pins retained.
No fields/paths/flags from the admission are parsed or trusted. Only states are not-applied and blocked.
not-applied means the same owned original is CURRENTLY observed; it is not an assertion that no historical
action ever occurred and never permits automatic replay. Intent plus disappearance/foreign replacement,
unknown/partial/mutated evidence -> blocked. No applied/restored/success state. PS publication observation
always MutationAllowed/FullRollback/InstallSucceeded=false. No restore/Launch/automatic installer retry.
Metadata self/native binding and actual original checks are not secret-backed authentication against a
privileged actor coherently rewriting all evidence/current owned inputs; no stronger claim introduced.

Original33 suites/classifier26/strict4aggregate/old deadlines and every prior native/installer body unchanged.
Helpers not called by default installer/migration/delete/failure/terminal route. Future mutation must pin
admission and the exact owned original, revalidate immediately before operation and persist an honest
physical outcome; this stage does not claim such pinning across a future call or post-delete rehydration.
Those, owned restore/full auxiliary recovery/error outcomes/runtime recovery/retention and all other F23/F31/
installer/updater/PGA04/package/signing/UI/owner/commercial gates remain OPEN. Actual installation error/no
Launch contract remains, settings/vault/keys/foreign bytes untouched. Old UNKNOWN incidents and transfer block
remain unchanged. No new native P/Invoke, security right, key/credential or network operation.

PreDelete23 Windows cases prepared: complete, no-seal-before, snapshot-before, journal-before, runtime-before,
foreign-location-before, conflict, cancel-before, partial-after, tampered-after, admission-replaced-same-bytes,
seal-tampered-after, seal-replaced-same-bytes, reference-replaced-same-bytes, source-replaced-same-bytes,
original-replaced-same-bytes, original-absent, snapshot-after, journal-after, wrong-location-after,
reparse-admission, missing-admission, foreign-admission. Publication requires exact counts/no changed retained
bytes; cancellation must be OperationCanceledException; same operation refuses retry without changing bytes.
Same-byte/copied-creation replacements retain old native objects; real own admission junction targets own
retained directory. After clearing parent objects, read-only state gets paths only and checks whole inventory/
private bytes/exclusive reopen. Complete case also launches a new owned PowerShell/CLR process, bounded20s
verification+5s cleanup/drain with fixed safe marker, no witness/hash arguments or raw private output.
Original-absent is a fixture move preserving retained bytes, NOT evidence of owned deletion or real crash.
Partial-after is malformed input refusal, NOT Write/Flush fault injection or power-loss test. Power-lossNOT_RUN.
Nested once after Restart26 inside existing DurableInput Recovery suite before final PASS; no extra job or
rerun of old suites. Localstatic6+prior48 =54PythonPASS; C#/PowerShell/native/new Windows23 NOT_RUN locally.
Next full exact-source CI then separate owned deletion/admission integration or post-delete evidence gate;
full rollback/production not declared.

## CURRENT sealed restart rehydration — original-present phase only

Acceptance: after loss of prior process memory, rebuild immutable LegacySource/reference evidence from the
saved journal and actual files, rechecking operation, snapshot/material, complete admissions, runtime,
independent trusted locations, original/source/reference native volume-index-creation origins and hashes.
No caller Backup/proof/hash input to restart reader. Missing/partial/tampered/foreign/replaced/reparse states
return null, preserve all files and grant no mutation/FullRollback/InstallSucceeded/Launch authority.
This stage deliberately requires the original still present and semantically owned. If it was deleted,
Capture returns no proof and restart refuses; post-delete recovery needs a separately verified admission.

Independent anchor gap in CI714: reference native origin existed only in the in-memory witness. New explicit
Complete-ProGoInstallerLegacyRestartSeal validates actual live context/native witness then writes own fixed
backups/installer-legacy-seal-GUID.bin with CreateNew, WriteThrough/Flush(true), same-handle + independent-path
readback and final context/token checks. Seal canonical bytes include operation/snapshot/install/location,
original origin/hash, source storage origin/document hash, reference native origin/document hash AND seal's
own actual native origin. Same-byte copied/replaced reference or seal therefore differs from persisted
native-object binding. No key/credential/privilege or cryptographic-authentication claim introduced.

Restore-ProGoInstallerLegacyReferenceEvidence accepts only trusted journal/install/Programs/Startup paths.
It bounds strict UTF8 metadata reads to1MiB, treats saved Snapshot/inventory as untrusted candidates, scopes
snapshot GUID under own backups, hashes actual manifest, whitelists full inventory names/types/hash formats
BEFORE snapshot/runtime helpers may read candidate leaves, rebuilds every fixed admission from disk and
reuses full canonical context verification. No saved flag or path supplies mutation authority. Native source
rehydration recomputes the whole source envelope from actual STILL PRESENT owned original bytes/native
origin, actual source storage native origin and independently verified snapshot; no source metadata claims
are used to mint an unchecked proof. Expected reference JSON comes from this proof and fresh verified context,
must exactly match actual saved bytes; native rehydration then recomputes seal from actual reference/seal
native handles and expected hashes. Finally repeat journal/snapshot/runtime context before returning witness.

New native final-leaf reads use existing OpenLegacyForDisposition with GENERIC_READ/share-read/
OPEN_REPARSE_POINT and validate file attributes/native origin before FileStream. This avoids following a leaf
link in a check/open window; non-reparse directory pins also retained. No new P/Invoke/security rights or
SSH flags. Every original native/installer body remains unchanged; new helpers are additive and not invoked
by normal installer/migration/failure/terminal paths. Getters/old33/classifier26/gates/deadlines unchanged.
Reference20 and pre-existing witness API remain valid; old unsealed references are NOT silently upgraded on
restart. Partial/unclaimed seal is never repaired/deleted/reused; explicit new seal publication requires a
currently verified live witness and independent context. No intent replay or default install mutation added.

Trust limit: evidence checks integrity/identity/ownership in scoped app files plus actual owned original,
not secret-backed authentication against an actor coherently rewriting the entire evidence graph/current
owned inputs. No stronger adversary resistance or historical journal-record file IDs are claimed. Journal
records are verified by canonical content/hashes/scope, native origin checks apply original/source/reference/
seal. This is read-only evidence reconstruction, never an authorization to restore. Any future mutation must
revalidate under its own ownership/admission contract. The original-present boundary prevents manufacturing
post-delete ownership from self-declared serialized original bytes; that dependency remains OPEN.

Restart26 cases prepared: complete, no-seal, partial-seal, seal-tampered, seal-replaced-same-bytes, foreign-seal,
reference-tampered, reference-replaced-same-bytes, source-tampered, source-replaced-same-bytes,
original-replaced-same-bytes, journal-tampered, snapshot-tampered, runtime-tampered, wrong-location,
operation-claim, snapshot-claim, location-claim, hash-claim, extra-record, missing-reference, missing-source,
missing-original, reparse-seal, cancel-seal, private-entry-claim. Mutations happen in new own GUID fixtures;
identical-byte/copied-creation replacements retain old native objects to prevent index reuse. Real own seal
junction targets a retained own directory; no live shortcut or foreign resource touched. All recovery calls
receive only paths after clearing old Backup/reference/manager variables, check readonly inventories/private
bytes and exclusive reopen. Positive case ALSO invokes actual new powershell.exe/CLR with only own case path,
no parent witness objects/handles; bounded20s exit +5s owned cleanup/drain, fixed safe marker, no raw args/
paths/PIDs or child stderr printed. New process has no prior proof state. Other negatives are fresh disk
reconstruction calls in the fixture process, NOT separately claimed process terminations.
No crash/power-loss induced; prefix mutation is only partial-input refusal, actual power-loss NOT_RUN.

Test wiring once after JournalReference20 within existing DurableInput Recovery suite before final PASS;
no new job/dispatch/duplicate old suite. New static6 + existing42 =48 localPythonPASS; these are textual/layout/
lexical checks, NOT C#/PS compilation or Windows execution. New Windows Restart26 NOT_RUN until exact new CI.
Next gates: new full CI, then separate pre-delete integration/admission and post-delete rehydration/owned
Legacy restore/crash settlement/error result/full auxiliary outcome/runtime recovery/retention; all F23/F31/
package/updater/PGA04/owner/signing/UI/commercial requirements retained. No whole F23/production PASS.

## CURRENT trusted Legacy journal reference / live-session prerequisite

Acceptance: reference can be returned and revalidated only for an actual verified snapshot, complete canonical
journal admissions and exact current new runtime, with a matching owned original + durable source. Snapshot
hash is read/checked against actual manifest/material, not accepted as a new public parameter. Derive operation
GUID from validated own journal name; bind full inventory + actual record hashes, snapshot/install/location,
original origin/hash, durable storage native origin/document hash. Unknown/missing/mutated/foreign/partial/
replaced/reparse evidence refuses; no overwrite, deletion, restore or success authority.

PS entry points New-ProGoInstallerLegacySourceReference and Test-ProGoInstallerLegacySourceReference take the
existing live Backup/Journal model and independently trusted Programs/Startup locations. Get context validates
actual snapshot/runtime/journal, full approved runtime inventory and every exact schema/sequence/inventory/entry
admission against physical files, then repeats context before/after publication/verification. No fields/paths or
success flags from a saved reference are used as authority. Verifier constructs expected canonical JSON from
fresh context plus immutable writer-issued evidence; does not deserialize a journal into a capability.

Native additive methods PersistLegacyJournalReference / VerifyLegacyJournalReference use sealed evidence with
private setters/internal constructor (Source proof, reference native Origin/hash, false authority flags).
Private VerifyLegacySourceForReference fills missing pre-delete API: re-captures/holds original, exact owner,
install/location/origin/hash, semantic ShellLink ownership, source canonical bytes and native storage origin.
It is observation-only. Native bounded reference reader 1MiB; fixed own backups/installer-legacy-reference-GUID.json,
non-reparse root/backups directory pins, CreateNew/WriteThrough/Flush(true), same-handle exact readback and
independent-path native origin/hash check, final source/cancellation check. Existing/partial/foreign files
never overwritten/reused/deleted; failure retains unclaimed bytes and yields no valid reference witness.
Source/ref read pins prevent write/delete while observed. No new native P/Invoke/privilege, no original method
body changed. No general lease across future operations is claimed: revalidate again at any future mutation.

Journal record format/terminal strict inventory remain unchanged: reference lives beside the journal, bound
to its operation/canonical records, NOT as an unknown extra admission in it. Current caller must retain the
immutable native witness. Durable document does NOT by itself authorize restart import: independent durable
anchor rehydration/validation (including reference origin/hash), pre-delete admission/integration, interrupted
settlement, owned Legacy restore/error result/full auxiliary outcome, runtime recovery/retention remain OPEN.
This is a verified reference publication/live verification prerequisite, not completed crash recovery or
pre-delete integration. New helper is not called from actual migration/deletion/production failure path yet.
Source hash-only historical receipts remain fail-closed. No intent-based automatic mutation/FullRollback/
InstallSucceeded/Launch permission; settings/vault/keys/foreign resources unchanged. Trusted caller locations
remain explicit and are never loaded from reference JSON. No full F23 or production PASS claimed.

JournalReference20 Windows cases (prepared, NOT_RUN on new source): complete, caller-snapshot, snapshot-before,
journal-before, foreign-journal, source-exists, reference-exists, cancel-before, missing-reference, partial-reference,
reference-tampered, reference-replaced-same-bytes, source-tampered, source-replaced-same-bytes,
original-replaced-same-bytes, journal-after, snapshot-after, wrong-location, runtime-after, reference-reparse.
Actual snapshot/journal/runtime owned-file writes and synthetic real native Legacy link, no executable launch.
Identical bytes/copied creation replacement retains old object to avoid native index reuse. Real directory
junction at own reference filename targets own retained fixture directory; bounded owned cmd mklink process,
no privilege/system changes, own kill/drain/finally and guarded no-follow cleanup, target bytes unchanged.
Reader inventories and private bytes preserved; all retained files reopened exclusively after settlement.
Cancellation is BEFORE entry; partial reference models a mutated file, NOT crash/Write/Flush fault injection.
MEDIUM fault-injection specification remains open, unchanged; no new test seam or fake crash test.
Wired once via existing DurableInput12 at end of Recovery before final PASS; no new suite/job/dispatch.
New static6 verifies contract/wiring/case inventory/additive lexical delimiter sanity only (not compilation).
Local42PythonPASS; new Windows20/C#/PS compile NOT_RUN until exact new CI logs. No inherited CI713PASS.

## CURRENT durable Legacy source file prerequisite; CI710 fully verified

Baseline source4de597fba29d91fb8b8242c36f5b2a01d9681598/tree89ad162396260778b7a08679b3cac8885adfcbef;
checkpointe970edf4583da98e632821440ce6f81c76d5f5e2 clean/preserved. PR88 exact baseline/open/unmerged;
sole root/no Git/compiler/PowerShell writer, no competing CI. Old worktrees untouched.
New source CURRENT_TREE (git rev-parse HEAD), reviewed local code912003d80b0bfdafabd20d3fd63376622ec1af4c.

CI710 all5 full final logs independently read, exact merge/build19fe447889d9e62a8809bf372762d0b517328be1:
Foundation121306465403/665lines SUCCESS27; Recovery121306465539/259 SUCCESS4 incl Source18/Legacy19;
Core121306465506/15414 SUCCESS; Ubuntu121306465535/611 SUCCESS; aggregate121312881008/173 SUCCESS
requires identical4 tested_sha. Run https://github.com/rkhnorkhan-bit/ProGo/actions/runs/40238967166 .
Actual diagnostic4 PASS; exit0 startup no deadline: NON-REPRODUCTION, prior startup root UNKNOWN, not fixed.
Desktop6394/Instance30/Maintenance126/Shutdown47/HomeVPN4816/Portable91 PASS; Python42 PASS/2 SKIP;
Ubuntu aggregate16/operation16/profile13/provisioning PASS. Public401/44 PASS. Runner-only ZIP670119 bytes
SHA25610b6c22b38117315f6bff15d0daf29f1350e5db4206b69da60e8731d4043915a; pack SUCCESS/uploads-prune SKIPPED.
No source transfer/build delivery/install/owner PC acceptance. New durable tests do not inherit these PASS.

Bounded F23 slice: native PersistLegacyRollbackSource + ReadDurableLegacyRollbackBytes only. New fixed
own install/backups/installer-legacy-source-<operation GUID>.bin, no path supplied by receipt/document.
Pin exact non-reparse install/backups/Menu directories against rename/delete. Keep original read pin,
repeat native owned semantic match/full original origin(volume/index/creation)/bytes before and after write.
CreateNew ONLY, WriteThrough/Flush(true), full exact same-handle and independently opened path read-back,
new file full native storage origin + document hash, final cancellation before returning immutable evidence.
Existing/partial/foreign destination never overwritten/reused/deleted. No success proof on failed publication.

Canonical bounded envelope binds operation GUID, snapshot hash input, install/location identities, original
origin/hash, storage native origin, exact length and original bytes. Reader derives fixed own path, pins
non-reparse directories/source, requires separate writer-issued evidence (not self-declared envelope flags),
checks actual storage native origin + document hash, canonical full bytes/schema/length, original hash and
actual verified removed receipt/current absence/replacement. Same bytes/copied creation with foreign native
object rejected. Read with capture disposed/new manager proves disk bytes, returned copy changes cannot
change durable file. No byte/path/credentials logging. Receipt or intent never authorizes restore.

Scope limit: snapshot hash is a trusted caller INPUT binding, not a claim that native helper verifies the
installer snapshot. Evidence object not yet durably referenced/reconstructed from journal. Integration must
independently verify actual snapshot/journal/identity/locations, persist the reference and source BEFORE
existing deletion, and define crash settlement. Default installer/migration/delete/failure handlers unchanged;
no restore, automatic runtime recovery or FullRollback/InstallSucceeded/Launch permission added here.
Existing Legacy admission VerifiedRestoreSourceAvailable remains false until full dependency integration.

New DurableSource20 fixtures: complete, disposed, existing/partial destination, partial-after publication,
payload/metadata mutation, identical bytes+creation foreign storage replacement, wrong operation/snapshot/
install/receipt, original same-byte replacement/change before write, reappeared Legacy/changed replacement,
missing proof, busy reader, cancellation, real redirected-backups junction. Actual native capture/migration/
file writes, source disposed before reader. All private/runtime/foreign bytes untouched; own GUID/reparse/
temp-parent cleanup and exclusive reopen; only own bounded junction subprocess. Partial/crash boundaries
represented by unclaimed prefix-only file/truncated real file, NOT an actual OS crash/power-loss experiment.
Original31 exact suite bodies and strict4-partition aggregate preserved. Append32nd suite to Recovery5;
Foundation27 unchanged. Coverage contract adds only this one suite, negative missing/duplicate/gating tests
remain. No assertion/deadline/workflow/upload/SSH fixture/runtime mutation weakening.

Local review/static/signatures/parity/additive reverse diff/header-size check/aggregate16/JSON/secrets/
public-content/diff PASS. No local C#/PowerShell/Windows; compilation/native DurableSource20/all32/new
exact-SHA aggregate NOT_RUN. One expected-HEAD guarded publication/automatic CI only, no retry/dispatch.
See F23_LEGACY_DURABLE_SOURCE.md. Next SAME new CI all5 full logs/merge/27+5/Durable20/all32. After full
PASS, separate durable trusted journal reference/pre-deletion integration prerequisite; old hash-only receipts
stay fail-closed, never reconstructed from arbitrary bytes. Owned restore/admission/crash/result/error path/
full auxiliary outcome/runtime recovery/retention/PGA04/updater/final package/owner/F31/signing/UI/full
commercial scope retained. Full31/nested55 requirements/fixed weights unchanged delta0. UNKNOWN incidents
and BLOCKED_TRANSFER/no retry retained. No live install/services/network/security/ACL/credentials/VPS/tunnel/
release/uploads/delivery retry or second stage.

Acceptance boundary: standalone native durable-byte storage plus immutable evidence link only. Source durability is Flush(true)/WriteThrough/read-back; it is not installer crash recovery. Future journal reference and real snapshot proof must exist before deletion is allowed to depend on this source. No import of arbitrary source metadata or restore API introduced.

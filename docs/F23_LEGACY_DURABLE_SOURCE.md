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

# F23 auxiliary prerequisite: Start Menu primary evidence

Baseline source c7eb7c307ab7ce616a6feaed21b7aec7a5658d9f, checkpoint
a143c20a908513f4a6ac00ceb081a385404b9cfb. Full final CI680 logs independently read:
Windows120991364304/15544 lines, Ubuntu120991364239/578 lines; merge
fd5cf0fcc063089ca302ca163aaa32cff4a09b5c; both jobs SUCCESS. StartupEvidence13/
PendingProof11/Reconcile13/RollbackOutcome12/Maintenance19/Outcome10 and prior installer/
SOCKS51/Desktop6388/policy214/Instance30/Maintenance126/Shutdown47/HomeVPN4816/Portable91
suites PASS. Pack runner-only; uploads/prune SKIPPED. These results are baseline only.

## Contract, acceptance and narrow scope

Select only the primary Start Menu link, not the entire legacy migration. The existing
InstallWithStartupEvidence path captures the bounded exact primary bytes/absence before
and after its existing EnsureLink/MigrateMenu calls. No new shortcut operation is introduced.
Only bytes returned by THIS EnsureLink can claim creation, and they must equal actual result.
Existing links and skipped custom/absent state are preserved observations, not newly owned.
Requested Menu result must also match expected executable and --show using the same native
ShellLink read oracle. Existing public Install signature and non-evidence behavior remain.

Nested Menu receipt records schema1, install/location identity hashes, requested/disposition,
exact before/after hashes and presence. No raw user paths/shortcut bytes/arguments are stored.
A read-only verifier is bound to the caller's expected install/Programs location and actual
current bytes/absence. Foreign/custom requested link, changed/removed/appeared result, wrong
identity/location, directory or oversized observation cannot become accepted owned evidence.
No reparse/foreign overwrite guard is weakened; no future-edit atomicity is claimed.

The existing adapter returns Startup plus Menu proof; production requires Menu schema/root
identity before creating outcome metadata. Both are persisted in the same durable outcome
as exact runtime inventory, admissions and ownership. Only the maintenance mock result gains
synthetic Menu evidence; existing assertions, scenarios and timeouts are unchanged.

MenuEvidence14 uses own GUID temporary Programs/Startup overrides and actual adapter/type:
created/existing-owned/skipped-absent/skipped-custom/foreign/foreign-target/changed/appeared/
removed/other-identity/other-location/directory-conflict/oversized/durable. Foreign bytes stay
untouched; missing Menu proof blocks before pending creation. Actual owned runtime/durable
writer round trip retains matching receipt/full ownership, private settings/vault/key refs/
foreign bytes; even valid Menu plus Startup proof keeps pending-installed admission blocked.
No live known-folder shortcut, service, installation, ACL/network/security/credential action.
Local review/static parity/signatures/diff/secrets/public-content PASS; PowerShell parser,
C# compilation, Windows COM/MenuEvidence14 and exact regressions NOT_RUN locally.

Legacy migration remains a real dependency: actual original legacy ownership and resulting
absence/preservation must be proven together with primary result. Then durable receipt
parsing and current trusted-location validation/conflicts must be covered before any safe
pending-installed metadata promotion. This slice does not claim either completion.
Runtime crash recovery, retention and auxiliary rollback are not implemented here.

## Current finite acceptance gates (existing IDs, not new product requirements)

Authority: APPROVED_PLAN_31 F23, REMAINING_SUBTASKS_8 F23-R01..R07 and SSH_AGENT_AUTOSTART_SPEC
PGA-04. Earlier CI/table counts are historical. CI680 proves only the scoped development
suites above; source changes here await a new exact CI. None of seven whole gates is PASS.

| Existing gate | Verified development evidence / exact remaining acceptance | Dependencies |
| --- | --- | --- |
| F23-R01 standalone setup + PGA-04 | Prebuilt payload fail-closed Payload9 PASS on CI680; still need actual complete prebuilt standalone package without user build, explicit agent opt-in OFF default, correct missing-service/rights/policy errors, uninstall preservation of shared agent | Approved payload; consent/security actions only at separately authorized install-time; real package NOT_RUN |
| F23-R02 shared maintenance + backup/retention/stage/self-check/rollback | CI680 Maintenance19/Stage8/Backup8/SelfCheck9/Rollback8/Journal8/RestartGate8/Outcome10/RollbackOutcome12/Reconcile13/PendingProof11/StartupEvidence13 PASS scoped. Menu proof pending this CI; legacy evidence and durable current auxiliary validation -> pending-installed publication. Remaining incomplete crash ownership/recovery, evidence-safe retention and auxiliary rollback; whole package concurrency/preservation proof | R01 per registry; internal order auxiliary proofs -> pending publication; saved snapshot/identity/ownership/actual bytes before recovery; unresolved proof protected before retention |
| F23-R03 one-button updater or full installer fallback | Existing updater tests are partial evidence, not final fallback acceptance. Need actual verified installer handoff/new window, immutable existing program/data paths and preserved choices, final offline/cancel/hash/download/error scenarios | R01, R02, R04, R05 |
| F23-R04 truthful updater rollback | Installer rollback suites do not close updater criterion. Verify final updater failure/copy/self-check path and actual restored runtime; original error/no false success or Launch | Independent contract; exact final updater/package tests required |
| F23-R05 immutable accepted release identity | Version/commit/tag/hash and same-version old-asset replacement refusal on final actual package; existing identity checks are scoped, not final release acceptance | Verified actual build/package identity, not a historical runner hash |
| F23-R06 unified setup/updater dev acceptance | Same version/commit/release notes; isolated clean install and both update paths, fixed paths, cancel/offline/hash failures, installed self-check, rollback/handoff and preservation | R01-R05 + F31-R11 per registry; actual package and approved isolated install tests NOT_RUN |
| F23-R07 owner installed workflow | Actual owner's data/paths/choices preserved; SSH cancellation retains original request and result checks never reissue access. No owner-PC result inferred from CI | R06; explicit owner install/live-resource approval; target NOT_RUN |

### Distinct delivery goals

- Personal engineering ZIP is a diagnostic package, not F23 standalone completion. Immediate
  gates: supported private transfer (BLOCKED_TRANSFER, no retry), exact Windows materialization/
  build-only identity and new package SHA256/manifest/content scan/extracted smoke; explicit
  unsigned-test status under F27. Then separate owner-approved install/check/rollback with
  launch/responsiveness/profile/selected-key/SSH-SOCKS-HTTP/cancel/recovery. Earlier CI661
  exports and runner packs are not this package and do not prove delivery/install. No extra
  F31 harness is needed just to recount the existing isolated evidence.
- Standalone technical candidate requires the seven F23 gates above at their code/package/
  target layers, with final F19/F22 portability, F27 signing/verification/AV and applicable
  F16/F17 accessibility/owner gates retained in the original registry. Test phase can be
  explicitly unsigned per approved plan; final publisher signature/timestamp/AV is not PASS.
  Merge/public release/install/live/security approvals remain separate, not silently granted.
- Full commercial release reuses these F23 gates and additionally retains the original full
  registry, including F30 mail/account/billing/provider/sandbox/real payment/refund/webhook/
  session-revocation gates. Diagnostic ZIP or isolated CI cannot close those requirements.
  No requirement is dropped, added, reweighted or assigned a fictitious project percentage.

Sharing/Python/rebind incidents stay UNKNOWN with diagnostics; BLOCKED_TRANSFER unchanged.
Next: same new CI full final logs, then bounded legacy-migration completion proof. STOP one stage.

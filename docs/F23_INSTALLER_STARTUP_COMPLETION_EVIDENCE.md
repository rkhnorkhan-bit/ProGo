# F23 auxiliary completion prerequisite: Startup result receipt

Baseline source9819aaa632e59f0a439c639f0dae9acf3f17fd75/checkpoint
139e54cd10dbe0a854326b7e1f4b97c58e3fbb13. Independently read full CI679 logs:
Windows120981548253,15529 lines; Ubuntu120981548324,578 lines; tested merge
d994f31fded7861f588f57a4c7b8b4da9ad3235c, both SUCCESS. PendingProof11/Reconcile13/
RollbackOutcome12/Maintenance19/Outcome10/RestartGate8/Journal8/Rollback8/Payload9/
Stage8/Backup8/SelfCheck9/SOCKS51/Desktop6388/policy214/Instance30/Maintenance126/
Shutdown47/HomeVPN4816/Portable91/prior suites PASS. Pack runner-only/uploads-prune
SKIPPED. This evidence applies only to baseline, not new StartupEvidence13.

## Missing contract and acceptance

Successful return of Initialize-ProGoShortcuts alone is not a durable, independently
checkable description of actual auxiliary state. This slice covers ONLY the Startup
artifact: the existing install path records install/location identity hashes, creation vs
preservation disposition, and exact before/after presence and SHA256. It does NOT claim
all auxiliary phases or pending-installed publication are complete.

Existing Install(bool,bool,bool) remains compatible and retains its original operations.
Only the installer opts into the receipt path through the existing PowerShell adapter.
An existing installation or NoStartup preserves actual Startup bytes/absence; customized
and Windows-disabled existing links are never rewritten or re-enabled. Creation ownership
requires the exact bytes returned by this operation's EnsureLink plus actual target/argument
match and byte re-read. An externally appearing or edited file is not claimed as created.
Post-read verification is observational; it is not an atomic lock over external edits.

Receipt contains schema1, install/location identity (same normalized SHA256 convention as
installer), disposition, before/after existence and hashes only. No raw user paths, shortcut
bytes, arguments, credentials or Windows approval registry state are serialized. Preserved
custom state is an observation, not a claim of creation ownership. The verifier is read-only,
bound to the caller's expected install and Startup directory; checks exact current bytes/
absence and created-link semantic match. Mismatch returns false without cleanup or repair.

Production requires a non-null receipt and matching install identity before outcome creation,
then persists it in the same durable payload as full runtime ownership and linked admissions.
The existing maintenance mock returns a synthetic adapter receipt, with all assertions and
failure/Stop/Dispose/Launch tests unchanged. Legacy direct helper calls stay compatible;
missing receipts never authorize restart publication. No pending-reader/gate is relaxed.

StartupEvidence13 exercises the actual adapter/type with own GUID temp known-folder overrides:
created/existing-absent/existing-custom/existing-owned/fresh-owned/no-startup-absent/
no-startup-custom/conflict/changed/appeared/other-identity/other-location/durable.
The last case uses actual owned runtime writes and durable writer interrupted after Flush;
requires exact persisted receipt, matching runtime identity/ownership, no installed terminal,
pending gate blocked, private/settings/vault/key-ref/foreign bytes preserved. Missing or
foreign receipts are refused before pending creation. External edits remain untouched.
No real known-folder shortcuts, services, ACL/security/network/credentials or install used.

Local review/signatures/old helper and assertion parity/diff/secrets/public-content PASS.
No PowerShell/C# compiler/Windows available locally: parser/compilation/COM/StartupEvidence13
and exact new regressions NOT_RUN until the new CI's completed logs.

## Mandatory residual

Start Menu primary and legacy migration completion evidence; durable receipt deserialization/
revalidation against trusted current known-folder identity and conflict policy; only then a
bounded proven pending-installed metadata publication. Startup receipt alone is insufficient.
Other incomplete crash states/runtime recovery/durable partial ownership, retention protecting
unresolved proof, auxiliary-state rollback and remaining F23/PGA04/updater fallback/package/
owner gates remain OPEN. Runtime rollback still rethrows original error and blocks Launch;
no auxiliary rollback or runtime restoration is added here. Sharing/Python/rebind incidents
remain UNKNOWN with diagnostics. BLOCKED_TRANSFER, no retry. Readiness58–80/22, change0.

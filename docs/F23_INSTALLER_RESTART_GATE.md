# F23 restart pending detection gate (bounded prerequisite)

Acceptance: before Stop, staging, backup/admission or runtime mutation, under the existing
maintenance owner, refuse any prior installer-journal namespace entry. Direct new journal
creation also checks this guard. Unknown/corrupt/empty/intent-only state never authorizes
restore, overwrite or a new session; no journal is removed or interpreted as success.
Ordinary backups and installations with no prior journal are unchanged.

CI673 independently read full Windows15466-line final log and Ubuntu final log, both SUCCESS.
Sourcef7ae8b2a03a9fea0141dc63118a273648b55bc2f, tested merge
8202d37e4687c29e3205893455c8cbab1a29b280, run40115153183/job120934803351.
Journal8 existing/fresh/persist-fail/short-write/unknown/invalid/tampered/partial ALL PASS;
Rollback8/Maintenance15/Payload9/Stage8/Backup8/SelfCheck9/Desktop6388/Instance30/
Maintenance126/Shutdown47/HomeVPN4816/Portable91 and previous suites PASS. Pack runner-only,
uploads/prune SKIPPED. Python startup/rebind incidents remain UNKNOWN, diagnostics retained.

## Minimal production path

Assert-ProGoInstallerNoPendingJournal resolves guarded backups without creating it, rejects
a non-directory parent, enumerates only direct metadata and checks the installer-journal
namespace case-insensitively. Files, malformed names, empty GUID directories and unknown
contents are pending; existing ancestor/reparse/path guards remain active. No config,
credential, journal contents, snapshot or private bytes are read by this metadata gate.
Enter maintenance -> restart gate -> existing Stop -> stage/backup/runtime transaction.
The direct New-ProGoInstallerJournal entry checks the same guard before creating anything.

There are no durable terminal outcomes yet. Therefore even an intent retained after a
successful prior install is conservatively pending. This is an incomplete transaction
prerequisite, not a claim that normal reinstall or crash recovery is finished. It must
not be bypassed by deleting journals, trusting a Phase complete string or treating intent
as post-crash ownership. A current process's already-admitted own session is still validated
by the existing journal/owned-write contract; the new guard only precedes new sessions.

## RestartGate8: actual production body, isolated paths/mock OS boundary

no-parent, ordinary, empty, intent, corrupt, schema, file, malformed. Reconstruct no in-memory
journal or old write admission. Run actual installer tail under fake maintenance lease;
Stop is a sentinel mock that must be reached only for no-parent/ordinary, never for pending.
Assert exact refusal, one Dispose, no Install OK/Launch; direct NewJournal also refuses;
all runtime/settings/vault/key-ref/foreign/journal bytes and file inventory remain identical.
Only test's own new temp resources are cleaned. Existing Journal8/Rollback8/Maintenance15
fixtures/assertions and every timeout unchanged. No actual installation/service/ACL/network/
security/credential/owner-PC/VPS action. Windows execution NOT_RUN until exact new CI.

Mandatory next bounded slice: durable terminal/outcome schema and verified transition,
including snapshot and exact runtime identity/ownership evidence, before permitting any
restart continuation. Then bounded interrupted recovery/no replay/foreign overwrite,
terminal recognition and retention, auxiliary-state/shortcut rollback, installer/updater
fallback and PGA04 gates. No automatic restoration exists in this slice.
Privatecandidate BLOCKED_TRANSFER; no delivery retry/uploads/release/install. Readiness58–80/22,+0.

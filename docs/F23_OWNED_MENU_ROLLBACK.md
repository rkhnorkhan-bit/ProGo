# F23 explicit owned Menu rollback — bounded mutation prerequisite

Acceptance: an explicit caller holding installer maintenance may remove only its own created
`ProGo.lnk` under independently supplied Programs location, after the existing Menu admission
revalidates snapshot/runtime/admissions, install/location identity, receipt/auxiliary state and
trusted locations. A bounded (1 MiB), strict UTF8 pending receipt stays read-pinned until physical
settlement. Already verified Startup settlement is supported; unexplained Startup absence refuses.

Native `TryRollbackDurableMenu(IDictionary, CancellationToken)` strictly re-deserializes the Menu
receipt and verifies it, including native shortcut semantics and full origin. Preserved/custom or
preserved absence returns without mutation. Created-from-absence opens only the fixed primary via
existing OPEN_EXISTING / OPEN_REPARSE_POINT / READ|DELETE, FileShare.Read. Immediately before delete
it rechecks full volume/index/creation origin and exact actual bytes on that same retained handle.
The handle denies external write/replacement; SetFileInformationByHandle deletes only that object.
After disposal actual absence is required. A replacement is never deleted by its path. Failed
ownership, byte mismatch, sharing denial, reparse point, or observed reappearance returns false.
Cancellation before mutation propagates distinctly; after acceptance physical settlement and
verification cannot be skipped by late cancellation. All own handles/readers close in finally/using.

`Invoke-ProGoInstallerMenuRollback` is an explicit production prerequisite, intentionally not yet
wired into automatic failure handling. There is no durable Menu admission/outcome in this slice.
A process crash or partial result does not grant automatic mutation/replay authority; the existing
pending journal continues to block retry and installed publication. A true return means this
Menu-only operation verified its scoped result, never full auxiliary rollback, installation success
or Launch. Legacy-removed means only the new primary is removed: the old Legacy absence is preserved,
not restored or mislabeled full rollback. Error-handler integration requires durable Menu settlement
and remaining Legacy evidence separately. Original installation error/Launch gating unchanged.

New `InstallerOwnedMenuRollbackTests.ps1` runs 18 real isolated cases through actual payload/stage/
backup/runtime/journal/receipt code and native filesystem operations: created, preserved-custom,
preserved-absence, changed, replaced-identical, missing-origin, delete-locked, locked-retry, cancelled,
native-changed, native-replaced, native-cancel, foreign-runtime, wrong-location, legacy-removed,
post-startup, post-startup-replaced, unproved-startup-absence. Replacement retains the original and
copies exact bytes AND creation time to a new object, proving native origin cannot be replaced by
hash/creation alone. Actual existing Startup rollback and durable settlement run in post-startup.
Exclusive reopen proves receipt/link handles settled. Only the exact owned leaf may disappear;
whole-case file inventory, Startup/Legacy/runtime/snapshot/receipts, private and foreign bytes stay
unchanged. Stop cancellation remains exception, delete refusal false, explicit unlocked retry
revalidates all proofs, and a repeat after deletion refuses rather than replays/claims success.
Only new owned temporary fixture roots are cleaned under parent/GUID/reparse guards. No process,
service, live shortcut, ACL, credential or network operations. All old tests and deadlines unchanged.

Local static review is not Windows proof. New PowerShell parser, compiler and native18 remain
NOT_RUN until this exact new source's CI. All26 Installer suites plus Core/Ubuntu and exact-SHA
complete aggregate are required. Baseline CI699 complete25/Publication6/Menu32 PASS is preserved,
not inherited by new code. UNKNOWN incidents and BLOCKED_TRANSFER are not resolved by this change.

Next bounded dependency: durable Menu pre-mutation admission/crash settlement, followed by scoped
Menu result/error-handler integration. Legacy/full auxiliary outcome, runtime recovery, retention,
PGA04 installer/fallback and final package/owner acceptance remain separate agreed gates.

# F23 durable Menu mutation admission and read-only crash settlement

Acceptance: no Menu delete may start before exact durable admission has been created, flushed,
read back and revalidated. The running explicit caller owns maintenance and holds the receipt and
admission streams through the existing native operation. On restart only observation is allowed:
`not-applied`, `original-observed`, or `blocked`; intent never authorizes replay, recreation, runtime
restore, full rollback, installation success, Launch or automatic retry. No failure-handler binding.

The fixed own backup sibling `installer-menu-admission-<journal GUID>.json` is derived from a
validated own journal, never a path in its contents. Strict snapshot/runtime inventory/admissions
and original pending completion are revalidated. The bounded UTF8 receipt reconstructs a canonical
record tying install/journal identity, receipt hash, independently trusted Menu/Startup locations,
Menu origin/hash/prior absence and Startup state. CreateNew with WriteThrough, Flush(true), exact
bounded readback and repeated context validation precede native mutation. A verified existing record
can only be reused by an explicit invocation; invalid/partial/conflicting records remain fail-closed
and are not repaired or overwritten. All own pins dispose in finally, including errors/cancellation.

Native mutation remains unchanged: exact full native origin (volume/index/creation), bytes, own
READ|DELETE handle, no path-based delete, and actual absence after disposal. A replacement during
admission persistence is caught by revalidation before native mutation. The additional native
verifier only observes actual original Menu state; it performs no writes/deletes. Preserved/custom
objects remain no-op. Absent created Menu can be observed only with exact valid durable admission;
absence alone does not establish who removed it or grant new mutation authority.

Post-Startup settlement must still prove actual Startup original absence and exact existing Startup
admission/result records. Because the older Startup result verifier deliberately requires a live
Menu primary, the Menu context independently reconstructs those canonical records from verified
receipt/identity/trusted locations, validates their complete text and physical Startup state; it
does not weaken or bypass the old verifier. Legacy-removed is special only for observation: the
receipt must retain owned original origin/hash, replacement linkage to Menu hash, and actual Legacy
absence. No Legacy recreation/rollback or full auxiliary outcome is claimed. Receipt changes,
foreign same-byte objects, missing/changed Startup result, runtime/snapshot changes, unknown records
or conflicting locations are blocked. Default new-install/terminal reconciliation blocks any Menu
admission, even when its read-only scoped result is original-observed.

New MenuMutationAdmission19 uses only owned GUID temporary directories and real payload/stage/
backup/journal/receipt/native fixtures. Cases: before/after crash, preserved, persist-fail, short-write,
tamper, foreign-replaced, foreign-runtime, foreign-snapshot, missing-admission, delete-locked,
cancelled-before, cancel-after-persist, foreign-admission, legacy-removed, post-startup,
startup-result-changed, legacy-appeared, replace-after-persist. Actual production writer is faulted
at persistence; no mock substitutes native ownership. Three restart classifications and settlement
must preserve every byte/file and never replay. Corrupt admission also refuses explicit retry.
OwnedMenu18 keeps all old assertions and permits only one exact independently validated new record.
All other old tests/deadlines unchanged. Final installer summary now follows all27 suites; strict
four-job exact-SHA aggregate/workflow/upload gates unchanged.

New Windows tests remain NOT_RUN until this source's CI. Baseline CI700 all4/Installer26/Owned18/
Publication6/Menu32 PASS does not establish new19/full27. UNKNOWN incidents and BLOCKED_TRANSFER
remain open. Next: separate durable scoped Menu result and error-handler integration after exact CI
proof; Legacy/full auxiliary rollback, retention/runtime recovery and package gates remain separate.

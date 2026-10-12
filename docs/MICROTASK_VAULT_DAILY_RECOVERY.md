# Micro16: explicit daily-vault recovery and proven auxiliary cleanup

## Scope and safety boundary

`VaultDailyRecovery.Recover(root)` is a synchronous worker-thread seam, called
explicitly before later adoption/Open/UI integration. It uses only the supplied
existing Windows local root and the same protected per-user SID mutex as the
writer. The 1000 ms deadline bounds mutex waiting, not native I/O or DPAPI.
Abandoned mutex ownership permits a fresh inspection; it does not prove a
transaction succeeded. No owner application, PC, VPS, wall clock, expiry or
real vault is inspected. F09 remains open and the historical count stays 22/31.

The ordinary reader remains failclosed while any stable journal or legacy
fixed backup exists. The writer and explicit recovery worker are the gated consumers of `ReadCandidate`;
candidate stamps retain the canonical main-file scope. No directory listing or
glob cleanup is performed. Unknown pre-journal stage/backup files remain
untouched because their ownership has not been established.

## Recovery decision

The bounded CurrentUser DPAPI journal has a separate purpose and attests exact
previous/next ciphertext stamps, canonical data path and transaction-specific
stage/backup basenames. Recovery reads the journal through a regular-file check
and bounded read, then validates every present target, stage and unique backup
before the first mutation. A stage must match Next; a populated backup must
match Previous. A strictly zero-byte backup reserve is allowed only for an
existing Previous: the writer exclusively reserved it before publishing the
journal. Initial Missing transactions reject every backup, including zero.
Any legacy fixed `vault.daily.dat.recovery.previous` blocks recovery even when
a valid modern journal exists; its bytes are never assumed to be owned.

| Observed target | Verified evidence | Decision |
| --- | --- | --- |
| Exact Next | All remaining artifacts valid | Keep the already published Next |
| Exact Previous | All remaining artifacts valid | Abort the pending update and keep Previous |
| Missing | Exact Previous backup | Exclusively move the backup to the target and keep Previous |
| Missing | No Previous image, exact Next stage | Exclusively move the stage to the target and keep Next |
| Missing | No verified recoverable image | Conflict, retain journal; never return a new initial Missing |
| Unknown, corrupt, unsupported, denied or redirected image | Any | Failclosed; no cleanup of unchecked artifacts |

The exact selected target is reread before cleanup. Both auxiliary paths are
preflighted again after the test checkpoint, and each deletion verifies its own
expected image immediately beforehand. The stable journal is removed last,
after another target check, confirmed absence of auxiliaries, root/legacy check
and equality with the originally loaded journal bytes. There is no copy/delete
publication fallback and no generic rollback. A final ordinary read owns the
returned record and immutable stamp; recovery does not increment revision or
change the bound PIN.

Results `NoRecovery`, `RecoveredPrevious` and `RecoveredNext` expose only a
safe fresh record/stamp. Clean Missing is allowed only with NoRecovery and no
journal. Busy, FileUnavailable, InvalidData, UnsupportedFormat and Conflict
expose neither record nor stamp; messages are static and contain no path, SID,
PIN or dataset values.

## Isolated native fixtures

`VaultDailyRecoveryTests` compiles independently with C#5 and AppPaths/SafeLog
guards. All fixtures use unique owned temporary directories, synthetic PINs
and datasets, and native CurrentUser DPAPI. Coverage includes:

- clean Missing/existing roots and untouched unknown pre-journal stage;
- current Previous/Next, missing-stage retry, declared empty backup reserve,
  missing target plus old backup or new stage, and initial transaction recovery;
- undefined Missing retained as Conflict (including an empty old reserve with
  no staged image) and initial backup rejection;
- valid records with unknown digests, corrupt/unsupported artifacts, invalid
  journal lengths, unsupported/corrupt DPAPI journal and copied foreign scope;
- legacy empty witness and fixed orphan backup, including a valid modern journal;
- interrupted cleanup followed by idempotent retry, real stage/journal delete
  sharing locks, an exclusive journal read lock and a runner-owned read-deny ACL
  restored in `finally` with strict ordered access-rule/protection comparison
  before fixture cleanup;
- a backup replaced at the BeforeCleanup hook: second complete preflight catches
  it before deleting the otherwise valid stage; changed journal bytes are also
  retained before auxiliary cleanup, and a real backup Move-sharing lock leaves
  the missing target and all proofs unchanged;
- real writer hooks at JournalPublished, TargetPublished and AuxiliariesRemoved
  for both initial and existing files, with exact declared image selection;
- changed journal/stage after JournalPublished and changed target after
  AuxiliariesRemoved: the writer returns no accepted state, preserves changed
  bytes and retains explicit recovery evidence;
- actual NTFS junctions at journal/stage/backup/root, preserved destination bytes
  and removal of the links themselves before owned fixture cleanup;
- a real child process holding the shared writer mutex, bounded Busy with no
  publication, fresh recovery after an abandoned child mutex, and rejection of
  an existing broad ACL without changing it.

Writer/recovery hooks throw at deterministic checkpoints or pin real native
handles. These fixtures exercise persisted interrupted states and exception
paths; they do not establish actual process crash, power-loss durability,
cross-account/machine DPAPI, network-drive acceptance or a hostile same-user
filesystem sandbox. Path attribute checks do not close adversarial ancestor
replacement races. Child termination is limited to the specific owned PID;
process-tree termination is not claimed.

## Verification checkpoint

Source review and native Windows CI evidence will be appended after execution.
At this source checkpoint, no native PASS is claimed. Root owns harness wiring,
Git publication and the full validation/evidence checkpoint. Recovery remains
unconnected to Open/UI; durable adoption, CRUD persistence, startup/day expiry,
active-session cleanup and owner acceptance are subsequent bounded tasks.

## Completed native checkpoint 15–16, 2026-10-12

[CI #533](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38303786820)
tested functional head **`4e44f3124dcec0136a5461ecd79ff2193e4fc223`**, merge
**`5622ce1d028201ccab5affdf2d3cd69c76443ce9`**, against unchanged main
`4bb336575bb1216d3047bd2b294aeecad9fdd65a`. Windows job `114992783732`, isolated
server job `114992783483` and the complete run finished **SUCCESS**. The source
and completed Recovery log were independently reviewed.

Recovery passed **274 assertions at 20:23:16.5913534 UTC**, starting at
20:23:14.2209408 UTC. These are checks, not 274 separate scenarios. Previous/Next
selection, exact ciphertext, safe initial completion, undefined Missing,
reserved-zero and legacy guards, unknown/invalid/unsupported/foreign images all
executed. Six writer checkpoints and three changed ownership cases passed;
22 assertions established idempotent subsequent recovery for their fixtures.
Real stage/journal delete locks, exclusive journal read lock, backup Move lock,
partial cleanup, retry and second-preflight guards executed. The runner-owned
read-deny ACL was restored with exact ordered-rule/protection comparison at
20:23:14.7251786 UTC. Three artifact junctions, a root junction, shared Busy,
abandoned child gate and broad ACL rejection passed. Application paths/logging
guards reported zero accesses at 20:23:16.5912431 UTC.

The same candidate passed Journal211, modified Store151, File110 and all prior
Windows/server suites. Public source/release scan passed; CI artifacts were
uploaded. No target failure was suppressed or converted to a successful save:
RecoveredPrevious remains distinct from accepting the failed pending update.

This is verified cooperative recovery of the specified interrupted states.
Actual crash inside native publication, hardware/power loss, hostile same-user
sandbox, another Windows account/machine, clock expiry/Open/UI and owner
acceptance remain unverified. Unknown pre-journal leftovers are preserved.
F09 stays open, historical **22/31** is unchanged. CI artifacts are not a
published complete all31 installer. Full evidence and artifact IDs:
[IMPLEMENTATION_STATUS.md](IMPLEMENTATION_STATUS.md).

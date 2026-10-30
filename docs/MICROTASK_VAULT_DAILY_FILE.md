# Micro13: bounded daily sidecar reader and immutable observation stamp

## Scope

This is the read-only disk boundary for F09. It reads only the fixed
`vault.daily.dat` child of an explicitly supplied, existing Windows local
directory. It does not use `AppPaths`, logging, the installed real vault, UI,
wall clock, expiry rules, or implicit creation. F09 remains open until durable
publication, expiry/adoption and UI integration are implemented and verified.

`VaultDailyFile.CheckedPath(root)` canonicalizes a drive-qualified local path,
rejects UNC/device paths and alternate data stream syntax before filesystem
access, and inspects the root and every ancestor for directory/reparse status.
Mapped network drives are rejected through `DriveInfo.DriveType` as well.
The target must be a regular non-reparse file. This shared path check is also
available to the following writer microtask.

## Read and token contract

- `Success` owns the decoded record and an immutable stamp; `Missing` owns only
  an absence stamp. `FileUnavailable`, `InvalidData` and `UnsupportedFormat`
  publish neither a record nor a stamp. Error strings contain no root, PIN or
  payload values.
- Missing means `FileNotFoundException` for the exact fixed child, followed by
  another successful root/ancestor check. A missing parent, inaccessible root,
  denied file, sharing conflict, directory or reparse point is unavailable.
- Any existing `vault.daily.dat.recovery` witness or independent native backup
  `vault.daily.dat.recovery.previous` blocks adoption with
  `FileUnavailable`, including a zero-byte file, directory or reparse point.
  Only `FileNotFoundException` means absent witness; all access errors fail.
  Its existence is checked before the data read, after decoding and before a
  missing-child result. The reader never reads witness bytes, creates one,
  deletes one or repairs interrupted publication. Explicit recovery is a later
  microtask required before store adoption/Open/UI integration.
  An orphan native backup blocks the same way even without the stable witness;
  the stable witness is not used as the `File.Replace` backup destination.
- A `FileStream` opened with `FileShare.Read` holds the byte image while it is
  read. Length is bounded by the codec envelope limit before allocating the
  array; incomplete reads and extra bytes fail. The codec's invalid/unsupported
  result is preserved. The owned ciphertext array is cleared in `finally`.
- The stamp privately owns a defensive copy of SHA256 over the entire observed
  envelope, plus day, generation, revision and canonical path scope. No digest
  array is exposed. Scope uses ordinal case-insensitive comparison. Caller edits
  to the returned mutable fake dataset cannot change the observation stamp.
- The internal `Loaded(path, record, envelope)` factory also serves the next
  writer after a successful publication; its supplied record must already be an
  owned, valid decoded record. `Absent(path)` is used only for confirmed absence.

This is a cooperative local store contract. Attribute checks do not eliminate
adversarial ancestor replacement between checks and handle opening. A digest
does not prevent a malicious process under the same Windows user from restoring
an older complete ciphertext. No such guarantee is claimed.

## Verification

The isolated native Windows harness is `tests/VaultDailyFileTests.cs`. It uses
only unique owned temporary directories and synthetic data. Coverage includes:

- successful bound and empty records, preserved edits, detached datasets,
  unchanged files, no implicit files/directories and zero application path/log
  accesses;
- same image versus different digest, metadata, path scope and missing tokens,
  plus caller mutation of the factory's envelope after token construction;
- absent exact child versus missing parent, nonlocal/device/ADS/relative roots,
  regular file used as root and directory at the fixed filename;
- zero/truncated/oversized files, malformed framing, unsupported format and a
  native DPAPI corruption;
- actual exclusive and compatible read handles, an actual owner read-deny ACL
  restored in `finally`, native readability after restoration and strict ordered
  access-rule comparison, and NTFS junctions at the root, an ancestor and target.
- recovery witnesses with valid or absent data, directory witnesses, preserved
  ciphertext/witness bytes and no implicit creation or repair.
- orphan native backups with valid or absent data, directory backups and
  preservation without creating a stable witness or repairing the data file.

Junctions are removed themselves before recursive cleanup of the owned fixture;
target bytes and directories are asserted intact. The parent agent owns the
PowerShell harness insertion and native CI evidence. No local .NET compiler is
available here, so source review is not presented as a native test pass.
No network drive mapping is created by this harness; the mapped-drive guard has
source review coverage, without a native network-drive acceptance claim.

## Native ACL fixture corrections, 2026-10-12

The original fixture tried to restore an unmodified `FileSecurity` snapshot.
.NET Framework persists changed security sections, so this did not mark the
Access section for restoration. The correction explicitly imports the saved
Access SDDL into a new descriptor before `File.SetAccessControl` in `finally`.
The diagnostic run then proved restored native readability, but failed an
additional raw SDDL text-equality expectation. Text formatting is not the
permission criterion used by the final fixture.

The final comparer preserves rule order and count and checks every rule's SID,
exact `FileSystemRights`, allow/deny type, inheritance and propagation flags,
`IsInherited`, plus protection and canonical status. It does not sort rules,
mask rights or omit identities. Four memory cases accept an equivalent access
descriptor and reject changed rights, SID or protection with the same rule
count. Native readability remains an independent assertion. Failure diagnostics
emit only fixed phase/assertion names, exception type, counts and comparison
booleans; they never emit raw SDDL, SID, paths, PIN or dataset values.

| Run | Candidate / tested merge / Windows job | Observed result |
| --- | --- | --- |
| [#529](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38224406799) | `387c5bcbab23f94dd2270e5c716f0b37ed73d513` / `b67489602a4ab947814625a3495561b7bf39aa1a` / `114728258306` | Windows FAIL, server PASS; 75 file-reader assertions passed, then failure at the ACL-restoration boundary, 08:35:22 UTC |
| [#530](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38225220557) | `cb6ccdd1217398bec6ad8c46ce5e6e3a514cf851` / `ecf0ac3c12a193fb138986a10ad611c6e9ddab82` / `114730954908` | Windows FAIL, server PASS; 75 assertions passed, then failure at the ACL restoration/check boundary, 08:43:09 UTC; the generic log did not identify the exact failed operation |
| [#531](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38226318697) | `60f017d6ed64e65bf7e0daa6c7b795d031dbdecb` / `7a66a92c7961e66ec7e7f5addc7744910d026674` / `114734590349` | Windows FAIL, server PASS; restored native read SUCCESS, 76 assertions passed, then named `AclExactCheck` failed raw SDDL equality, 08:53:42 UTC |

All three runs compiled the production application and passed the existing
reader165, policy134 and record325 harnesses. They stopped before the separate
Store test-harness compilation/execution and later desktop/network suites.
Production `VaultDailyStore.cs` was already included in the application build;
its native behavior was not established by those early runs. Later corrections
changed only `tests/VaultDailyFileTests.cs`; production reader/store code stayed
unchanged. The completed final candidate is recorded below; these failed
checkpoints remain part of the evidence.

## Completed native checkpoint 13–14, 2026-10-12

Final test-correction head **`3eaec3ab46a064fd3072c91cb96c80b416533a66`** retains
the production reader/store implementation from
`387c5bcbab23f94dd2270e5c716f0b37ed73d513` (reader micro13:
`43c1b63d1648bf7ee3facd259c730c74a5ef2936`).
[CI #532](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38227655739)
tested merge **`6bd2ef1764cae9429c626261e17632ad353ea8ab`** with unchanged main
`4bb336575bb1216d3047bd2b294aeecad9fdd65a`. Windows job `114739023300`, isolated
server job `114739024077` and the overall run completed **SUCCESS**.

The independently reviewed completed Windows log reports **110 file-reader
assertions passed at 09:06:18.1525531 UTC**. These are checks, not 110 distinct
scenarios. The four comparer vectors passed at 09:06:18.039; native restored
readability passed at 09:06:18.1059478, and exact ordered access rules/protection
passed at 09:06:18.1060625. Real read locks, owned read-deny ACL restoration,
NTFS junctions, fixed-file limits, detached stamps, recovery witnesses and
orphan backup guards all executed. Guarded application paths and logging were
not accessed. The separate Store harness then passed **149 assertions**.

Desktop3485, the full Test-ProGo script, HomeVPN975, portable90 and Windows
Python discovery also passed. Discovery executed all seven HTTP cases: 24
reported items, 22 passed and only two existing Linux ownership/flock module
skips. Ubuntu ran operations16, sharing9 and isolated provisioning46; independent
QR decoding executed there. Public scan passed for 224 sources/44 release files;
CI artifacts and ZIP were uploaded. These are CI artifacts, not a published
installer containing all 31 completed plan points.

Source review and native CI now establish this bounded reader microtask. They
do not establish mapped-drive acceptance, another Windows account/machine,
hostile filesystem races, actual crash/power-loss durability, Open/UI behavior,
expiry/session revocation or owner acceptance. Explicit recovery must precede
store adoption/Open/UI; F09 stays open and historical **22/31** stays unchanged.
Owner PC/VPS and the installed application were not inspected.

Full checkpoint and artifact IDs: [IMPLEMENTATION_STATUS.md](IMPLEMENTATION_STATUS.md).

## Protocol evolution after checkpoint 13–14

Micro15–16 adds private stamp evidence serialization and a restricted
`ReadCandidate(root, fileName)` seam for gated writer/recovery workers. Candidate
names are limited to the fixed target and transaction-GUID-derived stage/backup
basenames; their stamps retain the canonical target scope. Ordinary `Read`
continues to block on any stable journal or legacy fixed backup. It still never
creates, repairs or implicitly recovers data. Native results for this newer
source are recorded separately in checkpoint 15–16; checkpoint 13–14 above
remains the historical evidence for its exact tested head.

See [MICROTASK_VAULT_DAILY_JOURNAL.md](MICROTASK_VAULT_DAILY_JOURNAL.md) and
[MICROTASK_VAULT_DAILY_RECOVERY.md](MICROTASK_VAULT_DAILY_RECOVERY.md).

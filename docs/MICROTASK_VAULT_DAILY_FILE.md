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
  restored in `finally`, and NTFS junctions at the root, an ancestor and target.
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

using System;
using System.IO;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        private static void BackupMetadataSnapshot()
        {
            var root = Path.Combine(work, "backup-metadata-fixture");
            var dir = Path.Combine(root, "backup-fixture-v0.4.7-to-v0.5.2");
            Directory.CreateDirectory(dir);
            var manifest = Path.Combine(dir, "manifest.txt");
            try
            {
                File.WriteAllLines(manifest, new[] {
                    " version=ignored leading space", "VERSION =ignored key space", "VERSION= 0.7.1 ", "version=late duplicate",
                    "TARGET_VERSION= 0.7.2 ", "target_version=late duplicate", "reason= before=update ", "REASON=late duplicate",
                    "CREATED_BY= updater ", "created_by=late duplicate", "UPDATE_RESULT= pending ", "update_result=late duplicate",
                    "BACKUP_KIND= pre-update ", "backup_kind=late duplicate", "CREATED= 2026-01-02 03:04:05 ", "created=late duplicate",
                    "CONTAINS= settings.json,vault.enc.json ", "contains=late duplicate", "ARCHIVED_ONLY= home-vpn-private/access.dat ", "archived_only=late duplicate"
                });
                var changed = "version=9.9.9\ntarget_version=10.0.0\nreason=changed\ncreated_by=manual\nupdate_result=manual\nbackup_kind=manual\ncreated=changed time\ncontains=changed contents\narchived_only=changed archive\n";
                var expectedTime = Directory.GetLastWriteTime(dir);
                var reads = 0;
                var info = BackupService.ReadBackupInfo(dir, delegate(string path) {
                    reads++;
                    Check(path == manifest, "metadata reader receives the selected manifest path");
                    var snapshot = File.ReadAllLines(path);
                    File.WriteAllText(path, changed);
                    return snapshot;
                });
                Check(reads == 1, "one actual manifest read supplies all nine metadata fields");
                Check(info.Version == "0.7.1" && info.TargetVersion == "0.7.2" && info.Reason == "before=update" &&
                    info.CreatedBy == "updater" && info.Result == "pending" && info.Kind == "pre-update" &&
                    info.Created == "2026-01-02 03:04:05" && info.Contents == "settings.json,vault.enc.json" &&
                    info.ArchiveContents == "home-vpn-private/access.dat",
                    "metadata uses one coherent snapshot despite a real file mutation and retains first case-insensitive matches and trimmed values");
                Check(info.Path == dir && info.LastWriteTime == expectedTime &&
                    info.DisplayName == Path.GetFileName(dir) + " | pre-update | v0.7.1 → 0.7.2 | pending | 2026-01-02 03:04:05",
                    "metadata preserves directory time, path and the established display format");

                reads = 0;
                info = BackupService.ReadBackupInfo(dir, delegate(string path) { reads++; return File.ReadAllLines(path); });
                Check(reads == 1 && info.Version == "9.9.9" && info.TargetVersion == "10.0.0" && info.Reason == "changed" &&
                    info.CreatedBy == "manual" && info.Result == "manual" && info.Kind == "manual" && info.Created == "changed time" &&
                    info.Contents == "changed contents" && info.ArchiveContents == "changed archive",
                    "the next metadata operation performs one fresh read rather than retaining stale cached fields");

                File.WriteAllLines(manifest, new[] {
                    "version= ", "VERSION=late duplicate", "target_version= ", "TARGET_VERSION=late duplicate",
                    "reason= ", "REASON=manual", "created_by= ", "CREATED_BY=manual", "update_result= ", "UPDATE_RESULT=failed",
                    "backup_kind= ", "BACKUP_KIND=manual", "created= ", "CREATED=late duplicate",
                    "contains= ", "CONTAINS=late duplicate", "archived_only= ", "ARCHIVED_ONLY=late duplicate"
                });
                reads = 0;
                info = BackupService.ReadBackupInfo(dir, delegate(string path) { reads++; return File.ReadAllLines(path); });
                Check(reads == 1 && info.Version == "0.4.7" && info.TargetVersion == "0.5.2" && info.Reason == "before-update" &&
                    info.CreatedBy == "unknown" && info.Result == "pre-update" && info.Kind == "pre-update" &&
                    info.Created == info.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss") && info.Contents == String.Empty && info.ArchiveContents == String.Empty,
                    "empty first duplicates retain the established legacy name and timestamp fallbacks for all nine fields");

                File.WriteAllText(manifest, changed);
                reads = 0;
                using (var locked = File.Open(manifest, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    info = BackupService.ReadBackupInfo(dir, delegate(string path) { reads++; return File.ReadAllLines(path); });
                    Check(reads == 1 && info.Version == "0.4.7" && info.TargetVersion == "0.5.2" && info.Reason == "before-update" &&
                        info.CreatedBy == "unknown" && info.Result == "pre-update" && info.Kind == "pre-update" &&
                        info.Contents == String.Empty && info.ArchiveContents == String.Empty,
                        "an actual locked manifest is attempted once and yields legacy metadata without propagating an I/O error");
                }
                reads = 0;
                info = BackupService.ReadBackupInfo(dir, delegate(string path) { reads++; return File.ReadAllLines(path); });
                Check(reads == 1 && info.Version == "9.9.9" && info.ArchiveContents == "changed archive",
                    "metadata recovers on the next read after the manifest lock is released");

                var legacy = Path.Combine(root, "legacy-folder"); Directory.CreateDirectory(legacy);
                reads = 0;
                info = BackupService.ReadBackupInfo(legacy, delegate(string path) { reads++; throw new IOException("Absent manifest must not be read."); });
                Check(reads == 0 && info.Version == "unknown" && info.TargetVersion == String.Empty && info.Reason == "legacy-backup" &&
                    info.CreatedBy == "unknown" && info.Result == "created" && info.Kind == "automatic" &&
                    info.Created == info.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss") && info.Contents == String.Empty && info.ArchiveContents == String.Empty,
                    "a legacy directory without a manifest retains every existing fallback without a file read");
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }
    }
}

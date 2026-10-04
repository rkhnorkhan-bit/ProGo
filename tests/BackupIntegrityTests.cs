using System;
using System.IO;
using ProGo;

internal static class BackupIntegrityTests
{
    private static int passed;
    private static string root;
    private static void Check(bool value, string name)
    { if (!value) throw new Exception(name); passed++; Console.WriteLine("PASS: " + name); }
    private static void Reject(Action action, string name)
    {
        bool rejected = false;
        try { action(); } catch (IOException) { rejected = true; }
        Check(rejected, name);
    }
    private static void FileAt(string name, string contents)
    {
        var path = Path.Combine(root, name.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllText(path, contents);
    }
    private static void Fixture()
    {
        if (Directory.Exists(root)) Directory.Delete(root, true);
        Directory.CreateDirectory(root);
        FileAt("manifest.txt", "product=ProGo\nversion=0.0.1\nbackup_kind=manual\n");
        FileAt("VERSION", "0.0.1\n"); FileAt("ProGo.exe", "synthetic fixture; never execute");
        foreach (var name in new[] { "Start-ProGo.ps1", "Restore-ProGoBackup.ps1", "Update-ProGo.Core.ps1",
            "Maintenance-ProGo.ps1", "MaintenanceOperation.cs" }) FileAt("scripts/" + name, "synthetic script; never execute");
        FileAt("scripts/nested/with spaces.ps1", "nested fixture");
        FileAt("settings.json", "{}"); FileAt("vault.enc.json", "opaque encrypted fixture");
        BackupIntegrity.Write(root);
    }
    private static int Main()
    {
        root = Path.Combine(Path.GetTempPath(), "ProGo-backup-integrity-" + Guid.NewGuid().ToString("N"));
        try
        {
            Fixture(); BackupIntegrity.Validate(root); Check(true, "complete copy with nested paths validates");
            var index = File.ReadAllText(Path.Combine(root, BackupIntegrity.IndexName));
            BackupIntegrity.Write(root);
            Check(File.ReadAllText(Path.Combine(root, BackupIntegrity.IndexName)) == index, "index refresh is deterministic and excludes its own metadata");
            var contents = BackupIntegrity.Contents(root);
            Check(contents.Contains("scripts") && contents.Contains("vault.enc.json") && !contents.Contains("ProGo.ico"), "contents reports only actual payload roots");
            FileAt("manifest.txt", "product=ProGo\nversion=0.0.1\nupdate_result=failed\n");
            BackupIntegrity.Validate(root); Check(true, "mutable update outcome leaves payload digests valid");
            foreach (var path in new[] { "ProGo.exe", "VERSION", "scripts/nested/with spaces.ps1", "settings.json", "vault.enc.json" })
            {
                Fixture(); FileAt(path, "changed fixture");
                Reject(() => BackupIntegrity.Validate(root), "damaged payload refused: " + path);
            }
            Fixture(); File.Delete(Path.Combine(root, "settings.json"));
            Reject(() => BackupIntegrity.Validate(root), "missing optional-but-recorded user data refused");
            Fixture(); FileAt("scripts/extra.ps1", "added fixture");
            Reject(() => BackupIntegrity.Validate(root), "unrecorded script refused");
            Fixture(); File.Delete(Path.Combine(root, "ProGo.exe")); BackupIntegrity.Write(root);
            Reject(() => BackupIntegrity.Validate(root), "fresh index cannot certify incomplete program");
            Fixture(); File.Delete(Path.Combine(root, "scripts", "Start-ProGo.ps1")); BackupIntegrity.Write(root);
            Reject(() => BackupIntegrity.Validate(root), "fresh index cannot certify missing required helper");
            Fixture(); File.Delete(Path.Combine(root, BackupIntegrity.IndexName));
            Reject(() => BackupIntegrity.Validate(root), "legacy copy refused without generating retroactive evidence");
            Check(!File.Exists(Path.Combine(root, BackupIntegrity.IndexName)), "legacy validation never modifies the source");
            foreach (var metadata in new[] { "product=Other\nversion=0.0.1", "product=ProGo\nversion=0.0.2",
                "product=ProGo\nversion=0.0.1\nVERSION=0.0.1", "version=0.0.1" })
            {
                Fixture(); FileAt("manifest.txt", metadata);
                Reject(() => BackupIntegrity.Validate(root), "wrong or ambiguous product/version refused");
            }
            foreach (var path in new[] { "../ProGo.exe", "/ProGo.exe", "scripts/../ProGo.exe", "scripts\\bad.ps1",
                "scripts/C:bad.ps1", "scripts/./bad.ps1", "scripts//bad.ps1", "scripts/bad\t.ps1", "unexpected.json" })
            {
                Fixture(); FileAt(BackupIntegrity.IndexName, "ProGo backup integrity v1\n" + new string('a', 64) + "\t" + path + "\n");
                Reject(() => BackupIntegrity.Validate(root), "unsafe inventory path refused");
            }
            Fixture(); index = File.ReadAllText(Path.Combine(root, BackupIntegrity.IndexName));
            FileAt(BackupIntegrity.IndexName, index + index.Split('\n')[1] + "\n");
            Reject(() => BackupIntegrity.Validate(root), "duplicate inventory record refused");
            Fixture(); FileAt(BackupIntegrity.IndexName, "unknown format\n");
            Reject(() => BackupIntegrity.Validate(root), "unknown index format refused");
            Fixture(); index = File.ReadAllText(Path.Combine(root, BackupIntegrity.IndexName));
            FileAt(BackupIntegrity.IndexName, index.Replace(index.Split('\n')[1].Substring(0, 64), new string('z', 64)));
            Reject(() => BackupIntegrity.Validate(root), "malformed digest refused");
            Fixture();
            using (var locked = File.Open(Path.Combine(root, "settings.json"), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                Reject(() => BackupIntegrity.Validate(root), "locked payload refused without source mutation");
            BackupIntegrity.Validate(root); Check(true, "unlocked unmodified copy remains valid");
            FileAt("unexpected.json", "foreign data");
            Reject(() => BackupIntegrity.Write(root), "writer refuses unexpected payload instead of certifying it");
            Console.WriteLine("Backup integrity tests PASS: " + passed); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}

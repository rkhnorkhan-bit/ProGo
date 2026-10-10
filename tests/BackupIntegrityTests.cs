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
        try { action(); } catch (InvalidDataException) { rejected = true; } catch (IOException) { rejected = true; }
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
            File.WriteAllBytes(Path.Combine(root, "VERSION"), System.Text.Encoding.ASCII.GetBytes("0.0.1\n"));
            BackupIntegrity.Write(root);
            Check(File.ReadAllText(Path.Combine(root, BackupIntegrity.IndexName)).Contains("e6635045e1d2478ec4ca712d8c0e1dfcef8bb7b5b1e8e3bb560d37fe399a9e72\tVERSION\n"),
                "chunked cancellable hashing preserves the existing v1 SHA-256 payload format");
            File.Delete(Path.Combine(root, "scripts", "Maintenance-ProGo.ps1"));
            File.Delete(Path.Combine(root, "scripts", "MaintenanceOperation.cs"));
            BackupIntegrity.Write(root); BackupIntegrity.Validate(root);
            Check(true, "complete older program does not require newer ownership helper files");
            Fixture();
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
            Fixture(); File.Delete(Path.Combine(root, "settings.json")); Directory.CreateDirectory(Path.Combine(root, "settings.json"));
            Reject(() => BackupIntegrity.Write(root), "empty directory cannot stand in for a user-data file");
            Reject(() => BackupIntegrity.Validate(root), "validation also refuses a directory substituted for a payload file");
            Fixture();
            FileAt("unexpected.json", "foreign data");
            Reject(() => BackupIntegrity.Write(root), "writer refuses unexpected payload instead of certifying it");
            Fixture();
            var archiveSource = Path.Combine(root, "..", "ProGo-personal-archive-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(archiveSource, "home-vpn-private", "session-fixture"));
            try
            {
                var protectedBytes = new byte[] { 1, 0, 0, 0, 208, 140, 157, 223, 1, 21, 209, 17, 140, 122, 0, 192, 79, 194, 151, 235, 99 };
                var requestBytes = (byte[])protectedBytes.Clone(); requestBytes[requestBytes.Length - 1] = 42;
                var shareRequestBytes = (byte[])protectedBytes.Clone(); shareRequestBytes[shareRequestBytes.Length - 1] = 43;
                var sourceKey = Path.Combine(archiveSource, "home-vpn-private", "ACCESS.DAT");
                var sourceRequest = Path.Combine(archiveSource, "home-vpn-private", "ADMIN-REQUEST.DAT");
                var sourceShareRequest = Path.Combine(archiveSource, "home-vpn-private", "SHARE-REQUEST.DAT");
                File.WriteAllBytes(sourceKey, protectedBytes);
                File.WriteAllBytes(sourceRequest, requestBytes);
                File.WriteAllBytes(sourceShareRequest, shareRequestBytes);
                foreach (var name in new[] { "owner.dat", "home-address.dat", "setup-request.dat" })
                    File.WriteAllBytes(Path.Combine(archiveSource, "home-vpn-private", name), protectedBytes);
                File.WriteAllBytes(Path.Combine(archiveSource, "home-vpn-private", "SHARE-0123456789ABCDEF0123456789ABCDEF.DAT"), protectedBytes);
                File.WriteAllText(Path.Combine(archiveSource, "home-vpn-private", "session-fixture", "access"), "plaintext session key; must not be copied");
                File.WriteAllText(Path.Combine(archiveSource, "home-vpn-private", "access.dat.new"), "temporary key; must not be copied");
                File.WriteAllText(Path.Combine(archiveSource, "home-vpn-private", "admin-request.dat.fixture.new"), "temporary request; must not be copied");
                File.WriteAllText(Path.Combine(archiveSource, "home-vpn-private", "share-request.dat.fixture.new"), "temporary HTTPS request; must not be copied");
                Directory.CreateDirectory(Path.Combine(archiveSource, "home-vpn-private", "admin-fixture"));
                File.WriteAllText(Path.Combine(archiveSource, "home-vpn-private", "admin-fixture", "result.txt"), "plaintext recovery token; must not be copied");
                File.WriteAllText(Path.Combine(archiveSource, "home-vpn-private", "unknown.dat"), "unknown plaintext; must not be copied");
                File.WriteAllText(Path.Combine(archiveSource, "system-proxy-backup.json"), "historical ownership fixture");
                BackupIntegrity.CopyPersonalArchives(archiveSource, root);
                var composition = BackupIntegrity.CompositionLines(root);
                File.AppendAllLines(Path.Combine(root, "manifest.txt"), composition);
                BackupIntegrity.Write(root); BackupIntegrity.Validate(root);
                Check(Convert.ToBase64String(File.ReadAllBytes(Path.Combine(root, "home-vpn-private", "access.dat"))) == Convert.ToBase64String(protectedBytes), "personal archive canonicalises Windows filenames and preserves recognised DPAPI envelope bytes without decrypting");
                Check(Convert.ToBase64String(File.ReadAllBytes(Path.Combine(root, "home-vpn-private", "admin-request.dat"))) == Convert.ToBase64String(requestBytes), "admin request is canonicalised and archived byte-identically without interpreting its owner or result");
                Check(Convert.ToBase64String(File.ReadAllBytes(Path.Combine(root, "home-vpn-private", "share-request.dat"))) == Convert.ToBase64String(shareRequestBytes), "HTTPS request is canonicalised and archived byte-identically without interpreting its owner or domain");
                Check(Directory.GetFileSystemEntries(Path.Combine(root, "home-vpn-private")).Length == 7, "archive excludes live sessions, admin output, unknown files and temporary plaintext");
                var archives = BackupIntegrity.ArchiveNames(root);
                Check(archives.Length == 8 && Array.IndexOf(archives, "system-proxy-backup.json") >= 0 && Array.IndexOf(archives, "home-vpn-private/share-request.dat") >= 0 &&
                    Array.IndexOf(archives, "home-vpn-private/admin-request.dat") >= 0 && Array.IndexOf(archives, "home-vpn-private/setup-request.dat") >= 0 &&
                    Array.IndexOf(archives, "home-vpn-private/owner.dat") >= 0 && Array.IndexOf(archives, "home-vpn-private/home-address.dat") >= 0 &&
                    Array.IndexOf(archives, "home-vpn-private/share-0123456789abcdef0123456789abcdef.dat") >= 0,
                    "actual archived-only composition includes all known persistent VPN files and proxy ownership evidence");
                Check(Array.IndexOf(composition, "archived_only=" + String.Join(",", archives)) >= 0 &&
                    Array.IndexOf(File.ReadAllLines(Path.Combine(root, "manifest.txt")), "archived_only=" + String.Join(",", archives)) >= 0 &&
                    Array.IndexOf(composition, "home_vpn_protection=DPAPI-CurrentUser;not-a-portable-export;no-automatic-import") >= 0,
                    "composition records the complete archive without permitting portable or automatic import");
                using (var copy = BackupIntegrity.Prepare(root))
                {
                    Check(BackupIntegrity.ArchiveNames(copy.Path).Length == 8 &&
                        Array.IndexOf(File.ReadAllLines(Path.Combine(copy.Path, "manifest.txt")), "archived_only=" + String.Join(",", archives)) >= 0 &&
                        Convert.ToBase64String(File.ReadAllBytes(Path.Combine(copy.Path, "home-vpn-private", "admin-request.dat"))) == Convert.ToBase64String(requestBytes) &&
                        Convert.ToBase64String(File.ReadAllBytes(Path.Combine(copy.Path, "home-vpn-private", "share-request.dat"))) == Convert.ToBase64String(shareRequestBytes),
                        "preparation retains the admin request and complete verified archive independently");
                    foreach (var scope in new[] { "Program", "Data", "All" })
                        Check(Array.IndexOf(BackupIntegrity.RestoreNames(copy.Path, scope, true), "home-vpn-private") < 0 &&
                            Array.IndexOf(BackupIntegrity.RestoreNames(copy.Path, scope, true), "home-vpn-private/admin-request.dat") < 0 &&
                            Array.IndexOf(BackupIntegrity.RestoreNames(copy.Path, scope, true), "home-vpn-private/share-request.dat") < 0 &&
                            Array.IndexOf(BackupIntegrity.RestoreNames(copy.Path, scope, true), "system-proxy-backup.json") < 0,
                            "restore never activates historical pending requests, machine/user ownership or VPN access: " + scope);
                }
                var archivedRequest = Path.Combine(root, "home-vpn-private", "admin-request.dat");
                var alteredRequest = (byte[])requestBytes.Clone(); alteredRequest[alteredRequest.Length - 1] ^= 1;
                File.WriteAllBytes(archivedRequest, alteredRequest);
                Reject(() => BackupIntegrity.Validate(root), "changed admin request with an intact DPAPI header fails the recorded digest");
                File.WriteAllBytes(archivedRequest, requestBytes); BackupIntegrity.Validate(root);
                var archivedShareRequest = Path.Combine(root, "home-vpn-private", "share-request.dat");
                var alteredShareRequest = (byte[])shareRequestBytes.Clone(); alteredShareRequest[alteredShareRequest.Length - 1] ^= 1;
                File.WriteAllBytes(archivedShareRequest, alteredShareRequest);
                Reject(() => BackupIntegrity.Validate(root), "changed HTTPS request with an intact DPAPI header fails the recorded digest");
                File.WriteAllBytes(archivedShareRequest, shareRequestBytes); BackupIntegrity.Validate(root);
                File.AppendAllText(Path.Combine(root, "system-proxy-backup.json"), "changed");
                Reject(() => BackupIntegrity.Validate(root), "damaged archived ownership evidence is not a valid backup");
                foreach (var sourceFile in new[] { sourceKey, sourceRequest, sourceShareRequest })
                {
                    Fixture();
                    var name = Path.GetFileName(sourceFile).ToLowerInvariant();
                    var original = File.ReadAllBytes(sourceFile);
                    using (var locked = File.Open(sourceFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                        Reject(() => BackupIntegrity.CopyPersonalArchives(archiveSource, root), "locked encrypted source is not silently omitted: " + name);
                    Fixture(); File.WriteAllText(sourceFile, "plaintext private fixture; must not archive");
                    Reject(() => BackupIntegrity.CopyPersonalArchives(archiveSource, root), "known VPN filename refuses plaintext: " + name);
                    Check(!File.Exists(Path.Combine(root, "home-vpn-private", name)), "rejected plaintext input is never archived: " + name);
                    Fixture(); FileAt("home-vpn-private/" + name, "plaintext private fixture");
                    Reject(() => BackupIntegrity.Write(root), "writer refuses directly placed plaintext: " + name);
                    Fixture(); File.Delete(sourceFile); Directory.CreateDirectory(sourceFile);
                    Reject(() => BackupIntegrity.CopyPersonalArchives(archiveSource, root), "folder cannot replace a protected source file: " + name);
                    Directory.Delete(sourceFile); File.WriteAllBytes(sourceFile, original);
                }
                Fixture(); FileAt("home-vpn-private/admin-fixture/result.txt", "plaintext recovery token");
                Reject(() => BackupIntegrity.Write(root), "integrity writer refuses temporary VPN recovery trees");
            }
            finally { Directory.Delete(archiveSource, true); }
            Fixture();
            var originalIndex = File.ReadAllText(Path.Combine(root, BackupIntegrity.IndexName));
            string preparedPath;
            using (var copy = BackupIntegrity.Prepare(root))
            {
                preparedPath = copy.Path;
                Check(preparedPath != root, "preparation owns an independent directory");
                Check(File.ReadAllText(Path.Combine(copy.Path, BackupIntegrity.IndexName)) == originalIndex, "preparation retains recorded digests rather than rehashing modified source");
                FileAt("settings.json", "source changed after preparation");
                BackupIntegrity.Validate(copy.Path);
                Check(File.ReadAllText(Path.Combine(copy.Path, "settings.json")) == "{}", "prepared payload remains independent after source mutation");
                var program = BackupIntegrity.RestoreNames(copy.Path, "Program", false);
                Check(Array.IndexOf(program, "ProGo.exe") >= 0 && Array.IndexOf(program, "settings.json") < 0 && Array.IndexOf(program, "vault.enc.json") < 0 && Array.IndexOf(program, "progo.log") < 0, "default program scope excludes user data and historical logs");
                var data = BackupIntegrity.RestoreNames(copy.Path, "Data", true);
                Check(data.Length == 2 && Array.IndexOf(data, "ProGo.exe") < 0 && Array.IndexOf(data, "scripts") < 0, "data scope excludes program and scripts");
                var all = BackupIntegrity.RestoreNames(copy.Path, "All", true);
                Check(all.Length == program.Length + data.Length, "all scope combines only selected program and user payloads");
                Reject(() => BackupIntegrity.RestoreNames(copy.Path, "Data", false), "data scope requires independent consent");
                Reject(() => BackupIntegrity.RestoreNames(copy.Path, "All", false), "combined scope also requires independent consent");
                Reject(() => BackupIntegrity.RestoreNames(copy.Path, "Unknown", true), "unknown scope is rejected");
            }
            Check(!Directory.Exists(preparedPath), "disposing prepared copy leaves original backup intact");
            Check(Directory.Exists(root), "prepared-copy cleanup never deletes source directory");
            Reject(() => BackupIntegrity.Prepare(root), "damaged source cannot become a newly certified prepared copy");
            Fixture(); File.Delete(Path.Combine(root, "vault.enc.json"));
            Check(BackupIntegrity.RestoreNames(root, "Data", true).Length == 1, "missing vault is excluded rather than deleting current vault");
            File.Delete(Path.Combine(root, "settings.json"));
            Reject(() => BackupIntegrity.RestoreNames(root, "All", true), "empty data scope cannot claim user data restoration");
            using (var cancellation = new System.Threading.CancellationTokenSource())
            {
                cancellation.Cancel(); bool cancelled = false;
                try { BackupIntegrity.Prepare(root, cancellation.Token); } catch (OperationCanceledException) { cancelled = true; }
                Check(cancelled, "cancelled preparation refuses new snapshot before any read");
            }
            Console.WriteLine("Backup integrity tests PASS: " + passed); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}

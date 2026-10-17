using System;
using System.IO;
using System.Diagnostics;
using System.Linq;

namespace ProGo
{
    internal static class BackupRetentionTests
    {
        private static int passed;
        private static void Check(bool value, string name) { if (!value) throw new Exception(name); passed++; Console.WriteLine("PASS: " + name); }
        private static string Add(string root, int number, string kind)
        {
            string path = Path.Combine(root, "backup-20260101-" + number.ToString("D4"));
            Directory.CreateDirectory(path);
            File.WriteAllText(Path.Combine(path, "manifest.txt"), "product=ProGo\nbackup_kind=" + kind + "\ncreated_by=app\n");
            File.WriteAllText(Path.Combine(path, "payload.txt"), "synthetic backup");
            return path;
        }
        private static void Link(string link, string target)
        {
            using (var process = Process.Start(new ProcessStartInfo("cmd.exe", "/c mklink /J \"" + link + "\" \"" + target + "\"")
                { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true }))
            {
                if (!process.WaitForExit(5000) || process.ExitCode != 0) throw new Exception("Cannot create isolated junction fixture");
            }
        }
        private static void Junctions(string root)
        {
            string target = Path.Combine(root, "junction-target"), pool = Path.Combine(root, "junction-pool");
            Directory.CreateDirectory(target); File.WriteAllText(Path.Combine(target, "payload.txt"), "must survive");
            for (int i = 0; i < 12; i++) Add(pool, i, "automatic");
            string nested = Path.Combine(pool, "backup-20260101-0000", "linked"), top = Path.Combine(pool, "top-link");
            try
            {
                Link(nested, target); Link(top, target);
                var plan = BackupRetention.Plan(pool);
                Check(!plan.Candidates.Any(c => c.Path == top), "junction at backup root is never a deletion candidate");
                var result = BackupRetention.Apply(plan);
                Check(result.Deleted == 1 && result.Skipped == 1 && result.Failed == 0, "nested junction is skipped without traversing its target");
                Check(File.ReadAllText(Path.Combine(target, "payload.txt")) == "must survive", "junction target contents survive cleanup");
            }
            finally
            {
                foreach (var path in new[] { nested, top })
                    if (Directory.Exists(path)) Directory.Delete(path, false);
            }
        }

        private static int Main()
        {
            string root = Path.Combine(Path.GetTempPath(), "ProGo-retention-" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(root);
                for (int i = 10; i < 40; i++) Add(root, i, "automatic");
                string oldBaseline = Add(root, 1, "baseline"), oldPre = Add(root, 2, "pre-update");
                string baseline = Add(root, 3, "baseline"), pre = Add(root, 4, "pre-update");
                string manual = Add(root, 0, "manual");
                string creatorManual = Add(root, 5, "automatic");
                File.AppendAllText(Path.Combine(creatorManual, "manifest.txt"), "update_result=manual\n");
                string unknown = Add(root, 6, "unrecognised");
                string missing = Path.Combine(root, "unknown-folder"); Directory.CreateDirectory(missing);
                string malformed = Add(root, 7, "automatic"); File.AppendAllText(Path.Combine(malformed, "manifest.txt"), "backup_kind=automatic\n");
                string legacy = Add(root, 8, "automatic"); File.WriteAllText(Path.Combine(legacy, "manifest.txt"), "reason=before-update\n");
                var plan = BackupRetention.Plan(root);
                Check(!BackupRetention.Plan(root, oldBaseline).Candidates.Any(c => c.Path == oldBaseline), "newly created transaction backup is pinned even after clock reversal");
                Check(plan.Candidates.Count == 22, "mixed pool over 20 uses ten automatic slots plus protected kinds");
                Check(plan.Candidates.Any(c => c.Path == oldBaseline) && plan.Candidates.Any(c => c.Path == oldPre), "older known baseline and pre-update may expire");
                foreach (var path in new[] { manual, creatorManual, unknown, missing, malformed, legacy, baseline, pre })
                    Check(!plan.Candidates.Any(c => c.Path == path), "manual, unknown and latest special backups are protected: " + Path.GetFileName(path));
                Check(plan.Candidates.All(c => Path.GetFileName(c.Path).CompareTo("backup-20260101-0030") < 0), "ten latest automatic names remain");
                string changed = plan.Candidates.First(c => c.Path != oldBaseline && c.Path != oldPre).Path;
                File.WriteAllText(Path.Combine(changed, "manifest.txt"), "product=ProGo\nbackup_kind=manual\n");
                string unapproved = Add(root, 9, "automatic");
                var result = BackupRetention.Apply(plan);
                Check(result.Deleted == 21 && result.Skipped == 1 && result.Failed == 0, "revalidation skips a newly manual backup without expanding confirmation");
                Check(Directory.Exists(changed) && Directory.Exists(unapproved), "changed and unapproved folders survive confirmed cleanup");
                foreach (var path in new[] { manual, creatorManual, unknown, missing, malformed, legacy, baseline, pre })
                    Check(Directory.Exists(path), "cleanup preserves protected folder: " + Path.GetFileName(path));
                var next = BackupRetention.Plan(root);
                Check(next.Candidates.Count == 1 && next.Candidates[0].Path == unapproved, "new candidate requires a new review");
                File.Delete(Path.Combine(unapproved, "manifest.txt")); Directory.CreateDirectory(Path.Combine(unapproved, "manifest.txt"));
                Check(BackupRetention.Apply(next).Skipped == 1 && Directory.Exists(unapproved), "unreadable or absent manifest fails closed");
                Check(BackupRetention.Plan(root).Candidates.Count == 0, "repeated automatic cleanup is idempotent");
                Junctions(root);
                Console.WriteLine("Backup retention tests PASS: " + passed); return 0;
            }
            catch (Exception ex) { Console.WriteLine(ex); return 1; }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }
    }
}

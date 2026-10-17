using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ProGo
{
    internal static class BoundedLogTests
    {
        private static int passed;
        private static void Check(bool value, string name)
        {
            if (!value) throw new Exception(name);
            passed++; Console.WriteLine("PASS: " + name);
        }

        private static void Oversize(string path, string tail)
        {
            using (var file = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                file.SetLength(4L * BoundedLog.MaxFileBytes);
                byte[] data = Encoding.UTF8.GetBytes(tail);
                file.Position = file.Length - data.Length;
                file.Write(data, 0, data.Length);
            }
        }

        private static int Main()
        {
            string root = Path.Combine(Path.GetTempPath(), "ProGo-log-tests-" + Guid.NewGuid().ToString("N"));
            try
            {
                string path = Path.Combine(root, "progo.log");
                Check(BoundedLog.TryWrite(path, "first record"), "new log creates its directory");
                Check(File.ReadAllText(path) == "first record" + Environment.NewLine, "UTF-8 record is readable and terminated");
                Check(BoundedLog.TryWrite(path, null), "null record does not fail");
                string unicode = new String('\u0416', BoundedLog.MaxRecordChars + 100);
                Check(BoundedLog.TryWrite(path, unicode), "oversized Unicode record is accepted with truncation");
                Check(File.ReadAllText(path).Contains(" [truncated]"), "oversized record has explicit truncation marker");
                Check(new FileInfo(path).Length < 3 * BoundedLog.MaxRecordChars, "oversized record cannot consume an entire log");
                for (int i = 0; i < 900; i++)
                    if (!BoundedLog.TryWrite(path, i + ":" + new String('x', 4000))) throw new Exception("Sequential write lost");
                Check(File.Exists(path + ".1") && File.Exists(path + ".2"), "repeated logging retains two archives");
                Check(Directory.GetFiles(root).Length == 3, "rotation creates only the active log and two archives");
                foreach (string file in Directory.GetFiles(root))
                    Check(new FileInfo(file).Length <= BoundedLog.MaxFileBytes, "each generated log stays within byte limit: " + Path.GetFileName(file));
                Check(BoundedLog.ReadTail(path, 8192).Contains("899:"), "latest record survives repeated rotation");

                Oversize(path, "old-current-tail"); Oversize(path + ".1", "old-archive-tail");
                Check(BoundedLog.TryWrite(path, "new-current"), "oversized historical active log rotates on next write");
                Check(BoundedLog.ReadTail(path + ".1", 64).EndsWith("old-current-tail"), "migration preserves the bounded recent active tail");
                Check(BoundedLog.ReadTail(path + ".2", 64).EndsWith("old-archive-tail"), "migration preserves the bounded previous archive tail");
                Check(new FileInfo(path + ".1").Length <= BoundedLog.MaxFileBytes && new FileInfo(path + ".2").Length <= BoundedLog.MaxFileBytes, "oversized historical generations are bounded on successful rotation");

                Oversize(path, "locked-archive-test");
                long before = new FileInfo(path).Length;
                using (var locked = new FileStream(path + ".1", FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    var watch = Stopwatch.StartNew();
                    Check(!BoundedLog.TryWrite(path, "must-not-append"), "locked archive skips record instead of unbounded append");
                    Check(watch.ElapsedMilliseconds < 2000, "locked archive does not wait for release");
                }
                Check(new FileInfo(path).Length == before && BoundedLog.ReadTail(path, 64).EndsWith("locked-archive-test"), "failed rotation preserves original active contents");
                Check(BoundedLog.TryWrite(path, "resumed"), "logging resumes after archive unlock");
                using (var locked = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                    Check(!BoundedLog.TryWrite(path, "locked"), "locked active log is a nonfatal skipped write");
                File.SetAttributes(path, FileAttributes.ReadOnly);
                Check(!BoundedLog.TryWrite(path, "readonly"), "read-only active log is a nonfatal skipped write");
                File.SetAttributes(path, FileAttributes.Normal);
                Check(!BoundedLog.TryWrite(root, "directory"), "invalid file target is nonfatal");
                Check(!BoundedLog.TryWrite(null, "missing"), "missing path is nonfatal");

                string parallel = Path.Combine(root, "parallel.log"); int accepted = 0;
                Parallel.For(0, 100, i => { if (BoundedLog.TryWrite(parallel, "record-" + i)) Interlocked.Increment(ref accepted); });
                string[] lines = File.ReadAllLines(parallel);
                Check(accepted > 0 && lines.Length == accepted, "concurrent accepted records are not lost or interleaved");
                foreach (string line in lines) if (!line.StartsWith("record-")) throw new Exception("Interleaved record");

                string legacy = Path.Combine(root, "progo-update.log"), current = Path.Combine(root, "update.log");
                Oversize(legacy, "legacy-tail");
                Check(BoundedLog.UpdaterLogPath(root) == legacy, "legacy updater log remains a readable fallback");
                Check(BoundedLog.ReadTail(legacy, 11) == "legacy-tail", "large legacy file uses bounded tail reading");
                File.WriteAllText(current, "current");
                Check(BoundedLog.UpdaterLogPath(root) == current, "active updater log takes precedence over stale legacy errors");
                Check(new FileInfo(legacy).Length == 4L * BoundedLog.MaxFileBytes, "legacy log is not rewritten or deleted");
                File.WriteAllText(current, "prefix\u0416end", new UTF8Encoding(false));
                Check(BoundedLog.ReadTail(current, 4) == "end", "tail skips partial UTF-8 character");
                Check(BoundedLog.ReadTail(Path.Combine(root, "absent"), 10) == "", "absent diagnostic log is nonfatal");
                Check(BoundedLog.ReadTail(current, 0) == "", "zero-sized diagnostic read is empty");
                Console.WriteLine("Bounded log tests PASS: " + passed);
                return 0;
            }
            catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }
    }
}

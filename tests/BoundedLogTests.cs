using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

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
                QueueOwnership(root);
                Console.WriteLine("Bounded log tests PASS: " + passed);
                return 0;
            }
            catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }

        private static void QueueOwnership(string root)
        {
            string path = Path.Combine(root, "queued.log"); int ownerThread = Thread.CurrentThread.ManagedThreadId, sinkThread = 0, writes = 0, active = 0, maximum = 0;
            using (var entered = new ManualResetEventSlim()) using (var release = new ManualResetEventSlim()) {
                var queue = new BoundedLogQueue(record => {
                    int count = Interlocked.Increment(ref active); maximum = Math.Max(maximum, count);
                    try {
                        sinkThread = Thread.CurrentThread.ManagedThreadId; Interlocked.Increment(ref writes);
                        if (record == "first") { entered.Set(); release.Wait(); }
                        return BoundedLog.TryWrite(path, record);
                    } finally { Interlocked.Decrement(ref active); }
                }, 2);
                Task paused = null;
                try {
                    Check(queue.TryEnqueue("first") && entered.Wait(3000), "queued log enters its owned real file sink on a worker");
                    var first = queue.Work; var watch = Stopwatch.StartNew();
                    Check(queue.TryEnqueue("second") && queue.TryEnqueue("third"), "bounded backlog accepts two rows while its current native sink remains held");
                    Check(!queue.TryEnqueue("overflow") && !queue.TryEnqueue(new String('x', BoundedLog.MaxRecordChars + 1)), "full queue and oversized row are rejected without growing memory or touching disk");
                    Check(watch.ElapsedMilliseconds < 500 && sinkThread != ownerThread && !first.IsCompleted && Object.ReferenceEquals(first, queue.Work),
                        "caller never waits on held file I/O and all accepted rows retain the same single drain lifetime");
                    paused = queue.SuspendAsync();
                    Check(!paused.IsCompleted && !queue.TryEnqueue("paused") && queue.Dropped == 3 && writes == 1,
                        "suspension forbids new rows immediately but retains the in-flight sink and exact backlog");
                    release.Set(); Check(paused.Wait(5000) && first.IsCompleted && maximum == 1 && queue.Failed == 0,
                        "suspension settles one serial worker only after actual file writes finish");
                    Check(File.ReadAllLines(path).SequenceEqual(new[] { "first", "second", "third" }), "accepted queued rows preserve order and never retry an external write");
                    queue.Resume(); Check(queue.TryEnqueue("fresh"), "explicit resume allows a new owned drain after settlement");
                    var next = queue.Work; Check(!Object.ReferenceEquals(first, next) && next.Wait(5000) && File.ReadAllLines(path).Last() == "fresh",
                        "old drain completion cannot erase the resumed drain lifetime or its real file result");
                }
                finally {
                    release.Set(); var finish = queue.SuspendAsync();
                    if (!finish.Wait(5000) || paused != null && !paused.Wait(5000)) throw new TimeoutException("owned log queue cleanup deadline");
                }
            }
            using (var entered = new ManualResetEventSlim()) using (var release = new ManualResetEventSlim()) {
                int calls = 0;
                var queue = new BoundedLogQueue(record => {
                    Interlocked.Increment(ref calls);
                    if (record == "rejected") { entered.Set(); release.Wait(); return false; }
                    if (record == "exception") throw new IOException("owned log fixture failure");
                    return BoundedLog.TryWrite(path, record);
                });
                try {
                    Check(queue.TryEnqueue("rejected") && entered.Wait(3000) && queue.TryEnqueue("exception") && queue.TryEnqueue("after failure"),
                        "failure vectors are queued behind one known pending sink");
                    release.Set(); var finish = queue.SuspendAsync();
                    Check(finish.Wait(5000) && queue.Failed == 2 && calls == 3 && File.ReadAllLines(path).Last() == "after failure",
                        "sink refusal and exception remain observable failures without retries, abandoned rows or a stuck lifetime");
                }
                finally { release.Set(); if (!queue.SuspendAsync().Wait(5000)) throw new TimeoutException("failed log queue cleanup deadline"); }
            }
        }
    }
}

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        private static void UpdaterHandoffWorkflow()
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") {
                Console.WriteLine("SKIP: updater owned-handoff integration requires isolated native CI"); return;
            }
            int before = passed;
            var root = Path.Combine(work, "updater-handoff-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try {
                UpdaterHandoffPreparation();
                foreach (string mode in new[] { "accept", "reject", "cancel", "timeout" }) UpdaterHandoffNative(root, mode);
            } finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
            Console.WriteLine("Updater owned-handoff checks PASS: " + (passed - before));
        }

        private static void UpdaterHandoffPreparation()
        {
            using (var cancel = new CancellationTokenSource()) {
                cancel.Cancel(); int calls = 0;
                var result = UpdateLauncher.LaunchUpdater(cancel.Token, () => {
                    calls++; throw new Exception("Precancelled updater cannot enter a launch factory");
                });
                Check(!result.Accepted && calls == 0 && result.Error.Contains("отменён") && !result.Error.Contains("остановлен"),
                    "a pre-cancelled updater launch touches no launch factory and claims no helper termination");
            }
            using (var cancel = new CancellationTokenSource()) {
                int calls = 0;
                var result = UpdateLauncher.LaunchUpdater(cancel.Token, () => {
                    calls++; cancel.Cancel(); return new ProcessStartInfo("never-launch-updater-fixture.exe") { UseShellExecute = false };
                });
                Check(!result.Accepted && calls == 1 && result.Error.Contains("отменён"),
                    "cancellation during launch preparation prevents the owned-helper launch boundary");
            }
            var missing = UpdateLauncher.LaunchUpdater(CancellationToken.None, () => {
                throw new FileNotFoundException("PRIVATE_UPDATER_PATH");
            });
            Check(!missing.Accepted && missing.Error.Contains("Файл обновления отсутствует") && !missing.Error.Contains("PRIVATE_"),
                "a missing installed updater is a fixed actionable result without a modal window or raw path");
            var failure = UpdateLauncher.LaunchUpdater(CancellationToken.None, () => { throw new IOException("PRIVATE_PREPARATION_ERROR"); });
            Check(!failure.Accepted && !failure.Error.Contains("PRIVATE_") && !failure.Error.Contains("остановлен"),
                "unknown launch or cleanup errors cannot claim successful termination or disclose raw details");
            Check(!UpdateLauncher.LaunchUpdater(CancellationToken.None, () => null).Accepted,
                "a missing launch description cannot produce accepted updater ownership");
            var shell = UpdateLauncher.LaunchUpdater(CancellationToken.None,
                () => new ProcessStartInfo("never-launch-updater-fixture.exe") { UseShellExecute = true });
            Check(!shell.Accepted && !shell.Error.Contains("остановлен"),
                "a shell launch is refused without changing the owned-helper environment contract");
            foreach (string unknown in new[] { null, "", "PRIVATE_HELPER_ERROR" }) {
                string translated = UpdateLauncher.UpdaterHandoffError(unknown);
                Check(!String.IsNullOrEmpty(translated) && !translated.Contains("PRIVATE_") && !translated.Contains("остановлен"),
                    "an unrecognized handoff outcome remains unconfirmed instead of asserting cleanup");
            }
        }

        private static void UpdaterHandoffNative(string root, string mode)
        {
            var source = AsyncRestoreSource(root, "source-" + mode);
            var original = AsyncRestoreHashes(source);
            using (var helper = new AsyncRestoreHelper(root, "updater-" + mode))
            using (var cancel = new CancellationTokenSource())
            using (var heartbeat = new System.Windows.Forms.Timer { Interval = 10 }) {
                int ui = Thread.CurrentThread.ManagedThreadId, factoryThread = 0, factories = 0, ticks = 0;
                heartbeat.Tick += delegate { ticks++; }; heartbeat.Start();
                var watch = Stopwatch.StartNew();
                var launch = Task.Factory.StartNew(() => UpdateLauncher.LaunchUpdater(cancel.Token, () => {
                    factoryThread = Thread.CurrentThread.ManagedThreadId; Interlocked.Increment(ref factories);
                    return helper.Info(source);
                }), CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
                bool captured = false;
                try {
                    PumpAsyncRestore(() => helper.Started || launch.IsCompleted, 10000);
                    Check(helper.Started && !launch.IsCompleted, "owned updater helper and descendant remain pending before ACK: " + mode);
                    helper.CaptureProcesses(); captured = true;
                    string childCopy = helper.ChildCopy; BackupIntegrity.Validate(childCopy);
                    PumpAsyncRestore(() => ticks >= 3);
                    Check(factoryThread != ui && factories == 1 && !helper.Acknowledged && helper.RootAlive &&
                        Directory.Exists(childCopy) && AsyncRestoreTreeMatches(source, original),
                        "updater launch preparation and actual ACK wait run off UI without installing or altering source data: " + mode);
                    if (mode == "accept") {
                        helper.ReleaseAcknowledgement(); PumpAsyncRestore(() => launch.IsCompleted && helper.Acknowledged, 10000);
                        var accepted = launch.GetAwaiter().GetResult(); cancel.Cancel();
                        Check(accepted.Accepted && String.IsNullOrEmpty(accepted.Error) && helper.RootAlive && Directory.Exists(childCopy),
                            "accepted updater acknowledgement survives a late cancellation and transfers its independent helper ownership");
                        helper.ReleaseExit(); PumpAsyncRestore(() => helper.Exited, 10000);
                        Check(!Directory.Exists(childCopy) && AsyncRestoreTreeMatches(source, original),
                            "acknowledged updater fixture settles its descendant and private copy without an installation");
                    } else {
                        if (mode == "cancel") cancel.Cancel();
                        if (mode == "reject") helper.ReleaseExit();
                        PumpAsyncRestore(() => launch.IsCompleted, mode == "timeout" ? 25000 : 10000);
                        var refused = launch.GetAwaiter().GetResult();
                        string expected = mode == "cancel" ? "отменена" : mode == "timeout" ? "15 секунд" : "завершился без подтверждения";
                        Check(!refused.Accepted && refused.Error.Contains(expected) && !refused.Error.Contains("восстановления") &&
                            helper.Exited && !helper.Acknowledged && AsyncRestoreTreeMatches(source, original),
                            "rejected cancelled or timed-out updater handoff reports updater context only after its actual owned tree settles: " + mode);
                        if (mode == "timeout") {
                            Check(watch.ElapsedMilliseconds >= 14500 && watch.ElapsedMilliseconds < 25000 && ticks >= 20,
                                "the actual fifteen-second updater-ready deadline keeps the native UI message pump responsive");
                        }
                    }
                } finally {
                    helper.ReleaseExit(); cancel.Cancel();
                    WaitAsyncRestoreWithoutPump(launch, 25000);
                    if (!captured && helper.Started) helper.CaptureProcesses();
                }
            }
        }
    }
}

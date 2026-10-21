using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        private static void AsyncCleanupWorkflow()
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") {
                Console.WriteLine("SKIP: asynchronous cleanup ownership fixtures require isolated Windows CI"); return;
            }
            var root = Path.Combine(work, "async-cleanup-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try {
                AsyncCleanupActor(root, "cleanup-plan");
                AsyncCleanupActor(root, "cleanup-apply");
                AsyncCleanupPreviewCancel(root);
                AsyncCleanupNoCandidates(root);
                AsyncCleanupChangedApproval(root);
                AsyncCleanupShutdown(root);
                AsyncCleanupDelayedOwnerCompletion(root);
                AsyncCleanupDisposedOwner(root, "cleanup-plan");
                AsyncCleanupDisposedOwner(root, "cleanup-apply");
                AsyncCleanupDisposedOwner(root, "backup-owner-completion");
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }

        // The existing restore fixture isolates the real backup root and native
        // integrations. Also await cleanup before it restores that root.
        private sealed class AsyncCleanupContextFixture : IDisposable
        {
            private readonly AsyncRestoreContextFixture inner;
            internal AsyncCleanupContextFixture(string root) { inner = new AsyncRestoreContextFixture(root); }
            internal UpdateAwareTrayApplicationContext Context { get { return inner.Context; } }
            internal Dictionary<AppCommand, ToolStripItem> Commands { get { return inner.Commands; } }
            internal string Source { get { return inner.Source; } }
            internal Dictionary<string, string> SourceHashes { get { return inner.SourceHashes; } }
            internal int CliCleanup { get { return inner.CliCleanup; } }
            internal int WindowsCleanup { get { return inner.WindowsCleanup; } }
            internal bool InstalledUnchanged() { return inner.InstalledUnchanged(); }
            public void Dispose()
            {
                Context.Dispose();
                var cleanup = Context.CleanupWork;
                if (cleanup != null) WaitAsyncRestoreWithoutPump(cleanup, 25000);
                inner.Dispose();
            }
        }

        private static string[] AsyncCleanupSeed()
        {
            return Enumerable.Range(0, 24).Select(i => {
                var path = Path.Combine(BackupService.BackupsRoot, "backup-20260102-" + i.ToString("D4"));
                Directory.CreateDirectory(path);
                File.WriteAllText(Path.Combine(path, "manifest.txt"), "product=ProGo\nbackup_kind=automatic\n");
                File.WriteAllText(Path.Combine(path, "preserve-or-delete.txt"), "synthetic retention fixture " + i);
                return path;
            }).ToArray();
        }

        private static void AsyncCleanupActor(string root, string slowPhase)
        {
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var fixture = new AsyncCleanupContextFixture(root))
            using (var pulse = new System.Windows.Forms.Timer { Interval = 10 })
            {
                var automatic = AsyncCleanupSeed(); var before = AsyncRestoreHashes(BackupService.BackupsRoot);
                int ownerThread = Thread.CurrentThread.ManagedThreadId, workerThread = 0, reads = 0, confirmations = 0, ticks = 0;
                CancellationToken observed = default(CancellationToken);
                fixture.Context.CleanupConfirm = delegate(BackupRetentionPlan plan) {
                    confirmations++;
                    Check(Thread.CurrentThread.ManagedThreadId == ownerThread && plan.Candidates.Count == 14 &&
                        plan.Candidates.All(c => automatic.Contains(c.Path)),
                        "cleanup approval belongs to the native owner and contains only the worker-produced exact old automatic paths");
                    return true;
                };
                fixture.Context.BackupIoProbe = delegate(string phase, CancellationToken token) {
                    if (phase == slowPhase) {
                        observed = token; workerThread = Thread.CurrentThread.ManagedThreadId;
                        Interlocked.Increment(ref reads); entered.Set(); release.Wait();
                    }
                };
                pulse.Tick += delegate { ticks++; }; pulse.Start();
                fixture.Commands[AppCommand.CleanupBackups].PerformClick(); var pending = fixture.Context.CleanupWork;
                try {
                    PumpAsyncRestore(() => entered.IsSet && ticks >= 3);
                    var progress = (BackupWorkProgressForm)Field(fixture.Context, "backupActivityForm");
                    Check(pending != null && !pending.IsCompleted && fixture.Context.IsBackupRunning && fixture.Context.BackupOperationKind == "cleanup" &&
                        progress.IsBusy && progress.Visible && workerThread != ownerThread && confirmations == (slowPhase == "cleanup-plan" ? 0 : 1),
                        "actual tray cleanup keeps its slow planning or deletion phase off the native owner while progress and heartbeat remain active");
                    Check(Object.ReferenceEquals(pending, fixture.Context.StartCleanupAsync()) && Object.ReferenceEquals(pending, fixture.Context.StartRestoreAsync()) &&
                        Object.ReferenceEquals(pending, fixture.Context.CreateManualBackupAsync()) && Object.ReferenceEquals(pending, fixture.Context.ManualBackupCompletion) && reads == 1,
                        "duplicate cleanup and competing restore or manual-copy requests retain exactly the existing backup actor");
                    Check(new[] { AppCommand.CreateBackup, AppCommand.RestoreBackup, AppCommand.CleanupBackups, AppCommand.Update }.All(c => !fixture.Commands[c].Enabled) &&
                        new[] { AppCommand.Settings, AppCommand.StopDesktop, AppCommand.StopAll }.All(c => fixture.Commands[c].Enabled),
                        "real tray guards competing maintenance while preserving settings and connection stop during slow cleanup");
                    CloseServiceDialog(fixture.Commands[AppCommand.Settings], typeof(SshProfilesSettingsForm), delegate {
                        Check(fixture.Context.IsBackupRunning && !pending.IsCompleted, "native settings remains reachable while cleanup I/O is held");
                    });
                    progress.Close();
                    Check(observed.IsCancellationRequested && !progress.IsDisposed && progress.IsBusy && fixture.Context.IsBackupRunning && !pending.IsCompleted,
                        "closing busy cleanup progress requests cancellation without releasing its still-running worker or commands");
                    release.Set(); PumpAsyncRestore(() => pending.IsCompleted && !fixture.Context.IsBackupRunning);
                    Check(!pending.IsFaulted && !progress.IsBusy && fixture.Context.BackupStatus.Contains("отменена") &&
                        fixture.Context.BackupStatus.Contains("Уже удалённые копии не восстанавливаются") && AsyncRestoreTreeMatches(BackupService.BackupsRoot, before) && fixture.InstalledUnchanged(),
                        "cancelled cleanup phase settles before deletion, keeps every fixture byte, and states the irreversible partial-deletion boundary honestly");
                    Check(new[] { AppCommand.CreateBackup, AppCommand.RestoreBackup, AppCommand.CleanupBackups, AppCommand.Update }.All(c => fixture.Commands[c].Enabled),
                        "cleanup cancellation releases maintenance only after actual worker and owner completion");
                    fixture.Context.BackupIoProbe = null;
                    var retry = fixture.Context.StartCleanupAsync(); PumpAsyncRestore(() => retry.IsCompleted && !fixture.Context.IsBackupRunning);
                    Check(!Object.ReferenceEquals(pending, retry) && !retry.IsFaulted && confirmations == (slowPhase == "cleanup-plan" ? 1 : 2) &&
                        automatic.Count(Directory.Exists) == 10 && Directory.Exists(fixture.Source) && fixture.Context.BackupStatus.Contains("Удалено: 14;") &&
                        AsyncRestoreTreeMatches(fixture.Source, fixture.SourceHashes) && fixture.InstalledUnchanged(),
                        "an explicit cleanup retry builds and approves a fresh plan, deletes only fourteen old automatic copies and retains manual plus installed data");
                }
                finally { release.Set(); fixture.Context.Dispose(); if (pending != null) WaitAsyncRestoreWithoutPump(pending, 25000); }
            }
        }

        private static void AsyncCleanupPreviewCancel(string root)
        {
            using (var fixture = new AsyncCleanupContextFixture(root))
            using (var pulse = new System.Windows.Forms.Timer { Interval = 10 })
            {
                AsyncCleanupSeed(); var before = AsyncRestoreHashes(BackupService.BackupsRoot);
                int previews = 0, applies = 0;
                fixture.Context.BackupIoProbe = delegate(string phase, CancellationToken token) { if (phase == "cleanup-apply") Interlocked.Increment(ref applies); };
                pulse.Tick += delegate {
                    var preview = Field(fixture.Context, "backupDialog") as BackupCleanupForm;
                    if (preview == null || !preview.Visible) return;
                    pulse.Stop(); previews++;
                    var paths = preview.Controls.Find("CleanupCandidates", true).OfType<TextBox>().Single();
                    var progress = (BackupWorkProgressForm)Field(fixture.Context, "backupActivityForm");
                    Check(paths.ReadOnly && paths.Lines.Length == 14 && paths.Lines.All(Directory.Exists) && preview.AcceptButton == null &&
                        fixture.Context.IsBackupRunning && progress.IsBusy && !fixture.Context.CleanupWork.IsCompleted,
                        "actual modal cleanup preview presents only the finished worker plan while the context retains its actor and explicit consent");
                    Check(Object.ReferenceEquals(fixture.Context.CleanupWork, fixture.Context.StartCleanupAsync()),
                        "duplicate cleanup during native preview shares its existing actor instead of opening another plan");
                    ((Button)preview.CancelButton).PerformClick();
                };
                pulse.Start(); var pending = fixture.Context.StartCleanupAsync();
                try {
                    PumpAsyncRestore(() => pending.IsCompleted && !fixture.Context.IsBackupRunning);
                    Check(!pending.IsFaulted && previews == 1 && applies == 0 && fixture.Context.BackupStatus.Contains("отменена") &&
                        AsyncRestoreTreeMatches(BackupService.BackupsRoot, before) && fixture.InstalledUnchanged(),
                        "cancelling the actual preview never starts deletion and preserves all existing manual, automatic and installed bytes");
                    int confirmations = 0; fixture.Context.CleanupConfirm = delegate { confirmations++; return false; };
                    var retry = fixture.Context.StartCleanupAsync(); PumpAsyncRestore(() => retry.IsCompleted && !fixture.Context.IsBackupRunning);
                    Check(confirmations == 1 && applies == 0 && AsyncRestoreTreeMatches(BackupService.BackupsRoot, before),
                        "explicit retry after preview cancellation gets fresh consent and cannot inherit the declined approval");
                }
                finally { pulse.Stop(); fixture.Context.Dispose(); WaitAsyncRestoreWithoutPump(pending, 25000); }
            }
        }

        private static void AsyncCleanupNoCandidates(string root)
        {
            using (var fixture = new AsyncCleanupContextFixture(root)) {
                var before = AsyncRestoreHashes(BackupService.BackupsRoot); int confirmations = 0, applies = 0;
                fixture.Context.CleanupConfirm = delegate { confirmations++; return true; };
                fixture.Context.BackupIoProbe = delegate(string phase, CancellationToken token) { if (phase == "cleanup-apply") Interlocked.Increment(ref applies); };
                var pending = fixture.Context.StartCleanupAsync(); PumpAsyncRestore(() => pending.IsCompleted && !fixture.Context.IsBackupRunning);
                Check(!pending.IsFaulted && confirmations == 0 && applies == 0 && fixture.Context.BackupStatus.Contains("для удаления нет") &&
                    AsyncRestoreTreeMatches(BackupService.BackupsRoot, before) && fixture.InstalledUnchanged(),
                    "empty worker plan completes visibly without a confirmation, deletion phase or modification of manual and installed data");
            }
        }

        private static void AsyncCleanupChangedApproval(string root)
        {
            using (var fixture = new AsyncCleanupContextFixture(root)) {
                var automatic = AsyncCleanupSeed(); string changed = null, late = null; string[] approved = null;
                Dictionary<string, string> changedHashes = null, lateHashes = null; int ownerThread = Thread.CurrentThread.ManagedThreadId;
                fixture.Context.CleanupConfirm = delegate(BackupRetentionPlan plan) {
                    Check(Thread.CurrentThread.ManagedThreadId == ownerThread && plan.Candidates.Count == 14,
                        "real cleanup mutation fixture starts from the exact owner-approved fourteen paths");
                    approved = plan.Candidates.Select(c => c.Path).ToArray(); changed = approved[0];
                    File.AppendAllText(Path.Combine(changed, "manifest.txt"), "created_by=manual\n");
                    changedHashes = AsyncRestoreHashes(changed);
                    late = Path.Combine(BackupService.BackupsRoot, "backup-20000101-late-automatic"); Directory.CreateDirectory(late);
                    File.WriteAllText(Path.Combine(late, "manifest.txt"), "product=ProGo\nbackup_kind=automatic\n");
                    File.WriteAllText(Path.Combine(late, "late.txt"), "not part of approved list"); lateHashes = AsyncRestoreHashes(late);
                    return true;
                };
                fixture.Commands[AppCommand.CleanupBackups].PerformClick(); var pending = fixture.Context.CleanupWork;
                PumpAsyncRestore(() => pending.IsCompleted && !fixture.Context.IsBackupRunning);
                Check(!pending.IsFaulted && approved != null && approved.Where(p => p != changed).All(p => !Directory.Exists(p)) &&
                    automatic.Where(p => !approved.Contains(p)).All(Directory.Exists) && AsyncRestoreTreeMatches(changed, changedHashes) && AsyncRestoreTreeMatches(late, lateHashes),
                    "real cleanup rechecks a changed manifest immediately before deletion and never expands owner consent to an old folder arriving after preview");
                Check(Directory.GetDirectories(BackupService.BackupsRoot).Length == 13 && fixture.Context.BackupStatus.Contains("Удалено: 13;") &&
                    fixture.Context.BackupStatus.Contains("пропущено после повторной проверки: 1;") && AsyncRestoreTreeMatches(fixture.Source, fixture.SourceHashes) && fixture.InstalledUnchanged(),
                    "actual cleanup summary reports its thirteen committed deletes and changed-manifest skip while manual plus installed data remain intact");
            }
        }

        private static void AsyncCleanupShutdown(string root)
        {
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var fixture = new AsyncCleanupContextFixture(root))
            using (var pulse = new System.Windows.Forms.Timer { Interval = 10 })
            {
                AsyncCleanupSeed(); var before = AsyncRestoreHashes(BackupService.BackupsRoot);
                int ticks = 0, confirmations = 0; CancellationToken observed = default(CancellationToken);
                fixture.Context.CleanupConfirm = delegate { confirmations++; return true; };
                fixture.Context.BackupIoProbe = delegate(string phase, CancellationToken token) {
                    if (phase == "cleanup-plan") { observed = token; entered.Set(); release.Wait(); }
                };
                pulse.Tick += delegate { ticks++; }; pulse.Start();
                var pending = fixture.Context.StartCleanupAsync();
                try {
                    PumpAsyncRestore(() => entered.IsSet);
                    var watch = Stopwatch.StartNew(); var shutdown = fixture.Context.RequestShutdownAsync(); var duplicate = fixture.Context.RequestShutdownAsync();
                    PumpAsyncRestore(() => shutdown.IsCompleted, 10000); watch.Stop();
                    Check(Object.ReferenceEquals(shutdown, duplicate) && !shutdown.Result && observed.IsCancellationRequested && !pending.IsCompleted &&
                        fixture.Context.IsBackupRunning && fixture.CliCleanup == 0 && fixture.WindowsCleanup == 0,
                        "blocked cleanup shares duplicate shutdown and prevents premature native restoration while its cancelled worker remains owned");
                    Check(watch.ElapsedMilliseconds >= 2800 && watch.ElapsedMilliseconds < 6000 && ticks >= 20 &&
                        fixture.Commands[AppCommand.Settings].Enabled && fixture.Commands[AppCommand.StopDesktop].Enabled && !fixture.Commands[AppCommand.CleanupBackups].Enabled,
                        "production shutdown refusal preserves native heartbeat, settings and stop while holding cleanup commands until settlement");
                    release.Set(); PumpAsyncRestore(() => pending.IsCompleted && !fixture.Context.IsBackupRunning);
                    var retry = fixture.Context.RequestShutdownAsync(); PumpAsyncRestore(() => retry.IsCompleted);
                    Check(retry.Result && confirmations == 0 && fixture.CliCleanup == 1 && fixture.WindowsCleanup == 1 &&
                        AsyncRestoreTreeMatches(BackupService.BackupsRoot, before) && fixture.InstalledUnchanged(),
                        "explicit shutdown retry performs one native cleanup after cancelled planning settles without opening preview or changing backups");
                    fixture.Context.CancelShutdown(); PumpAsyncRestore(() => fixture.Commands[AppCommand.CleanupBackups].Enabled);
                }
                finally { release.Set(); fixture.Context.Dispose(); WaitAsyncRestoreWithoutPump(pending, 25000); }
            }
        }

        private static void AsyncCleanupDelayedOwnerCompletion(string root)
        {
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var fixture = new AsyncCleanupContextFixture(root))
            using (var pulse = new System.Windows.Forms.Timer { Interval = 10 })
            {
                AsyncCleanupSeed(); var before = AsyncRestoreHashes(BackupService.BackupsRoot); int completions = 0, ticks = 0;
                fixture.Context.CleanupConfirm = delegate { return false; };
                fixture.Context.BackupIoProbe = delegate(string phase, CancellationToken token) {
                    if (phase == "backup-owner-completion") { Interlocked.Increment(ref completions); entered.Set(); release.Wait(); }
                };
                pulse.Tick += delegate { ticks++; }; pulse.Start();
                var pending = fixture.Context.StartCleanupAsync();
                try {
                    PumpAsyncRestore(() => entered.IsSet);
                    var session = (BackupWorkSession)Field(fixture.Context, "backupActivity");
                    Check(session.CurrentWork.IsCompleted && !pending.IsCompleted && fixture.Context.IsBackupRunning &&
                        Object.ReferenceEquals(pending, fixture.Context.StartCleanupAsync()) && !fixture.Commands[AppCommand.CleanupBackups].Enabled,
                        "cleanup retains its exact activity and command lease after disk settlement until final owner completion");
                    var watch = Stopwatch.StartNew(); var shutdown = fixture.Context.RequestShutdownAsync(); var duplicate = fixture.Context.RequestShutdownAsync();
                    PumpAsyncRestore(() => shutdown.IsCompleted, 10000); watch.Stop();
                    Check(Object.ReferenceEquals(shutdown, duplicate) && !shutdown.Result && !pending.IsCompleted &&
                        watch.ElapsedMilliseconds >= 2800 && watch.ElapsedMilliseconds < 6000 && ticks >= 20 && fixture.CliCleanup == 0 && fixture.WindowsCleanup == 0,
                        "shutdown waits for whole cleanup workflow and refuses responsively even after its disk and session cleanup tasks have finished");
                    release.Set(); PumpAsyncRestore(() => pending.IsCompleted && !fixture.Context.IsBackupRunning);
                    var retry = fixture.Context.RequestShutdownAsync(); PumpAsyncRestore(() => retry.IsCompleted);
                    int after = ticks; PumpAsyncRestore(() => ticks >= after + 3);
                    Check(retry.Result && completions == 1 && fixture.CliCleanup == 1 && fixture.WindowsCleanup == 1 &&
                        (bool)Field(fixture.Context, "shutdownPrepared") && AsyncRestoreTreeMatches(BackupService.BackupsRoot, before) && fixture.InstalledUnchanged(),
                        "explicit retry after final cleanup publication accepts one shutdown that cannot be reset by stale activity completion");
                    fixture.Context.CancelShutdown(); PumpAsyncRestore(() => fixture.Commands[AppCommand.CleanupBackups].Enabled);
                }
                finally { release.Set(); fixture.Context.Dispose(); WaitAsyncRestoreWithoutPump(pending, 25000); }
            }
        }

        private static void AsyncCleanupDisposedOwner(string root, string slowPhase)
        {
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var fixture = new AsyncCleanupContextFixture(root))
            {
                AsyncCleanupSeed(); var before = AsyncRestoreHashes(BackupService.BackupsRoot);
                int confirmations = 0; CancellationToken observed = default(CancellationToken);
                fixture.Context.CleanupConfirm = delegate { confirmations++; return slowPhase != "backup-owner-completion"; };
                fixture.Context.BackupIoProbe = delegate(string phase, CancellationToken token) {
                    if (phase == slowPhase) { observed = token; entered.Set(); release.Wait(); }
                };
                var pending = fixture.Context.StartCleanupAsync();
                try {
                    PumpAsyncRestore(() => entered.IsSet);
                    var progress = (BackupWorkProgressForm)Field(fixture.Context, "backupActivityForm");
                    int lateUi = 0; ((Label)Field(progress, "status")).TextChanged += delegate { lateUi++; };
                    var watch = Stopwatch.StartNew(); fixture.Context.Dispose(); watch.Stop();
                    Check(watch.ElapsedMilliseconds < 500 && observed.IsCancellationRequested && !pending.IsCompleted && progress.IsDisposed,
                        "forced native cleanup disposal promptly cancels but retains the still-running phase or final owner completion");
                    release.Set(); WaitAsyncRestoreWithoutPump(pending, 25000);
                    Check(!pending.IsFaulted && confirmations == (slowPhase == "cleanup-plan" ? 0 : 1) && lateUi == 0 &&
                        fixture.CliCleanup == 0 && fixture.WindowsCleanup == 0 && AsyncRestoreTreeMatches(BackupService.BackupsRoot, before) && fixture.InstalledUnchanged(),
                        "ownerless actual cleanup settles without a UI pump, late preview, deletion, native mutation or late publication");
                    Application.DoEvents(); Check(lateUi == 0 && progress.IsDisposed, "resumed native dispatch cannot revive the disposed cleanup owner");
                }
                finally { release.Set(); fixture.Context.Dispose(); WaitAsyncRestoreWithoutPump(pending, 25000); }
            }
        }
    }
}

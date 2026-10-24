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
        private static void AsyncUpdaterWorkflow()
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") {
                Console.WriteLine("SKIP: asynchronous updater ownership fixtures require isolated Windows CI"); return;
            }
            var root = Path.Combine(work, "async-updater-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try {
                AsyncUpdaterMenuWorkflow(root);
                AsyncUpdaterPreparationOwnership(root, false);
                AsyncUpdaterPreparationOwnership(root, true);
                AsyncUpdaterShutdownReservation(root);
                AsyncUpdaterStaleOrdinaryShutdown(root, false);
                AsyncUpdaterStaleOrdinaryShutdown(root, true);
                AsyncUpdaterTimeout(root);
                AsyncUpdaterAcceptedOwnerCompletion(root, "timeout");
                AsyncUpdaterAcceptedOwnerCompletion(root, "queued-timeout");
                AsyncUpdaterAcceptedOwnerCompletion(root, "dispose");
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }

        private sealed class AsyncUpdaterContextFixture : IDisposable
        {
            private readonly string previous;
            private readonly bool moved;
            private readonly byte[] originalSettings;
            internal readonly string Source;
            internal readonly Dictionary<string, string> SourceHashes;
            internal readonly SettingsService Settings;
            private readonly ProxyService proxy;
            internal readonly CliProxyBridgeService Bridge;
            private readonly Ikev2RelayService relay;
            private readonly HomeVpnService home;
            private readonly ClipboardService clipboard;
            private readonly ConnectionHealthMonitor monitor;
            internal readonly UpdateAwareTrayApplicationContext Context;
            internal readonly Dictionary<AppCommand, ToolStripItem> Commands;
            internal int CliCleanup, WindowsCleanup, Checks;
            internal UpdateCheckForm Latest;
            internal Action BeforeWindowsCleanup;
            private readonly Dictionary<string, byte[]> installed;

            internal AsyncUpdaterContextFixture(string root)
            {
                if (File.Exists(CliProxyEnvironmentService.BackupPath) || File.Exists(SystemProxyService.BackupPath))
                    throw new Exception("async updater fixture must not own unrelated native proxy journals");
                originalSettings = File.Exists(AppPaths.SettingsPath) ? File.ReadAllBytes(AppPaths.SettingsPath) : null;
                previous = Path.Combine(AppPaths.Root, "async-updater-originals-" + Guid.NewGuid().ToString("N"));
                if (Directory.Exists(BackupService.BackupsRoot)) { Directory.Move(BackupService.BackupsRoot, previous); moved = true; }
                Source = AsyncRestoreSource(BackupService.BackupsRoot, "backup-20260101-updater-manual"); SourceHashes = AsyncRestoreHashes(Source);
                Settings = new SettingsService();
                // This guard must be exercised with a preference which would
                // otherwise write on a real dashboard close. Restore raw bytes.
                var configured = Settings.Current.Clone(); configured.TrayCloseExplained = false; Settings.Save(configured);
                proxy = new ProxyService(() => Settings.Current, s => Settings.Save(s), "unused-fixture-ssh", () => DateTime.UtcNow, false);
                Bridge = new CliProxyBridgeService(Settings); relay = new Ikev2RelayService(); home = new HomeVpnService(relay);
                clipboard = new ClipboardService(Settings); monitor = new ConnectionHealthMonitor(() => Settings.Current);
                Context = new UpdateAwareTrayApplicationContext(Settings, proxy, Bridge, home, clipboard, false, monitor,
                    windowsRestore: () => {
                        Interlocked.Increment(ref WindowsCleanup); var before = BeforeWindowsCleanup; if (before != null) before();
                        return new WindowsProxyRestoreResult();
                    },
                    updateFormFactory: CreateCheck, cliRestore: () => Interlocked.Increment(ref CliCleanup));
                var tray = (NotifyIcon)Field(Context, "tray");
                Commands = MenuItems(tray.ContextMenuStrip.Items).Where(i => i.Tag is AppCommand).ToDictionary(i => (AppCommand)i.Tag, i => (ToolStripItem)i);
                installed = new[] { AppPaths.SettingsPath, AppPaths.VaultPath, Path.Combine(AppPaths.Root, "VERSION") }
                    .ToDictionary(p => p, p => File.Exists(p) ? File.ReadAllBytes(p) : null);
            }
            private UpdateCheckForm CreateCheck()
            {
                Checks++; Latest = new UpdateCheckForm(token => Task.FromResult(UpdateOffer()), "0.2.2"); return Latest;
            }
            internal bool InstalledUnchanged()
            { return installed.All(p => p.Value == null ? !File.Exists(p.Key) : File.Exists(p.Key) && File.ReadAllBytes(p.Key).SequenceEqual(p.Value)); }
            public void Dispose()
            {
                Context.Dispose();
                var settle = Task.WhenAll(Context.UpdateWork ?? Task.FromResult(false), Context.RestoreWork ?? Task.FromResult(false),
                    Context.CleanupWork ?? Task.FromResult(false), Context.SettingsWork ?? Task.FromResult<SettingsSaveError>(null),
                    Context.IntegrationWork ?? Task.FromResult(false), Context.TrayPreferenceWork);
                WaitAsyncRestoreWithoutPump(settle, 25000);
                monitor.Dispose(); clipboard.Dispose(); home.Dispose(); relay.Dispose(); Bridge.Dispose(); proxy.Dispose(); Settings.Dispose();
                if (Directory.Exists(BackupService.BackupsRoot)) Directory.Delete(BackupService.BackupsRoot, true);
                if (moved) Directory.Move(previous, BackupService.BackupsRoot);
                RestoreAsyncFile(AppPaths.SettingsPath, originalSettings);
            }
        }

        private static Task AsyncUpdaterMenuRequest(AsyncUpdaterContextFixture fixture)
        {
            var previous = fixture.Context.UpdateWork; Exception callbackError = null; int accepted = 0;
            using (var timer = new System.Windows.Forms.Timer { Interval = 10 }) {
                timer.Tick += delegate {
                    var form = fixture.Latest;
                    if (form == null || !form.Visible || form.CheckWork == null || form.IsChecking) return;
                    timer.Stop();
                    try {
                        Check(form.AcceptedResult == null && form.AcceptButton == null && ((Button)Field(form, "primary")).Text == "Установить обновление",
                            "actual tray update check requires explicit reviewed install consent before maintenance");
                        accepted++; ((Button)Field(form, "primary")).PerformClick();
                    } catch (Exception ex) { callbackError = ex; form.CancelAndClose(); }
                };
                timer.Start(); fixture.Commands[AppCommand.Update].PerformClick(); timer.Stop();
            }
            if (callbackError != null) throw callbackError;
            Check(accepted == 1 && fixture.Context.UpdateWork != null && !Object.ReferenceEquals(previous, fixture.Context.UpdateWork) && Field(fixture.Context, "updateForm") == null,
                "actual accepted update dialog releases its check window and starts one new tracked handoff workflow");
            return fixture.Context.UpdateWork;
        }

        private static void AsyncUpdaterMenuWorkflow(string root)
        {
            using (var helper = new AsyncRestoreHelper(root, "updater-context-cancel"))
            using (var retryHelper = new AsyncRestoreHelper(root, "updater-context-retry"))
            using (var fixture = new AsyncUpdaterContextFixture(root))
            using (var pulse = new System.Windows.Forms.Timer { Interval = 10 })
            {
                int ownerThread = Thread.CurrentThread.ManagedThreadId, factoryThread = 0, factories = 0, ticks = 0;
                fixture.Context.UpdaterLaunchInfo = () => { factoryThread = Thread.CurrentThread.ManagedThreadId; Interlocked.Increment(ref factories); return helper.Info(fixture.Source); };
                pulse.Tick += delegate { ticks++; }; pulse.Start();
                var pending = AsyncUpdaterMenuRequest(fixture); Task retry = null;
                try {
                    PumpAsyncRestore(() => helper.Started || pending.IsCompleted, 10000);
                    Check(helper.Started && !pending.IsCompleted, "actual accepted tray update starts its contained helper without blocking the native owner");
                    helper.CaptureProcesses(); PumpAsyncRestore(() => ticks >= 3);
                    var progress = (BackupWorkProgressForm)Field(fixture.Context, "backupActivityForm");
                    Check(progress.IsBusy && progress.Visible && progress.Text == "Обновление ProGo" && fixture.Context.BackupOperationKind == "update" &&
                        factoryThread != ownerThread && factories == 1 && fixture.CliCleanup == 1 && fixture.WindowsCleanup == 1 &&
                        (bool)Field(fixture.Context, "shutdownPrepared") && Object.ReferenceEquals(Field(fixture.Context, "maintenanceReservation"), Field(fixture.Context, "backupActivity")),
                        "updater file preparation and real ACK wait use a worker while native cleanup remains a reserved preflight with visible cancellable progress");
                    Check(Object.ReferenceEquals(pending, fixture.Context.StartUpdaterAsync(UpdateOffer())) && Object.ReferenceEquals(pending, fixture.Context.ManualBackupCompletion) &&
                        new[] { AppCommand.Update, AppCommand.CreateBackup, AppCommand.RestoreBackup, AppCommand.Connect }.All(c => !fixture.Commands[c].Enabled),
                        "repeated updater requests share exactly one pending actor and all route or maintenance changes stay guarded before ACK");
                    Call(fixture.Context, "StartUpdate"); fixture.Context.CompleteShutdown(); Application.DoEvents();
                    Check(fixture.Checks == 1 && factories == 1 && !(bool)Field(fixture.Context, "closing") && helper.RootAlive && !helper.Acknowledged,
                        "duplicate private update action and external completion cannot open a second check, helper or premature exit during reserved ACK wait");
                    KeyboardWalk(progress, new Control[] { (Button)progress.CancelButton }, "updater pending cancellation");
                    progress.Close();
                    Check(!progress.IsDisposed && progress.IsBusy && fixture.Context.IsBackupRunning && !pending.IsCompleted,
                        "title-close on busy updater progress requests cancellation while retaining ownership until the actual helper tree settles");
                    PumpAsyncRestore(() => pending.IsCompleted && !fixture.Context.IsBackupRunning, 10000);
                    Check(!pending.IsFaulted && helper.Exited && !helper.Acknowledged && !progress.IsBusy && fixture.Context.BackupStatus.Contains("отмен") &&
                        (bool)Field(fixture.Context, "lastBackupCancelled") && !(bool)Field(fixture.Context, "lastBackupFailed") &&
                        !(bool)Field(fixture.Context, "closing") && !(bool)Field(fixture.Context, "shutdownPrepared") && Field(fixture.Context, "maintenanceReservation") == null,
                        "cancelled actual updater releases its reservation only after contained root and descendant exit and leaves the owner open for retry");
                    fixture.Context.UpdaterLaunchInfo = () => { Interlocked.Increment(ref factories); return retryHelper.Info(fixture.Source); };
                    retry = AsyncUpdaterMenuRequest(fixture);
                    PumpAsyncRestore(() => retryHelper.Started || retry.IsCompleted, 10000);
                    Check(retryHelper.Started && !retry.IsCompleted && fixture.Checks == 2 && factories == 2,
                        "explicit new tray update action obtains fresh user consent and one fresh helper after a cancelled handoff");
                    retryHelper.CaptureProcesses(); retryHelper.ReleaseExit();
                    PumpAsyncRestore(() => retry.IsCompleted && !fixture.Context.IsBackupRunning, 10000);
                    Check(!retry.IsFaulted && retryHelper.Exited && !retryHelper.Acknowledged && fixture.Context.BackupStatus.Contains("завершился без подтверждения") &&
                        (bool)Field(fixture.Context, "lastBackupFailed") && !(bool)Field(fixture.Context, "maintenanceHandedOff") &&
                        fixture.CliCleanup == 2 && fixture.WindowsCleanup == 2 && fixture.Commands[AppCommand.Update].Enabled &&
                        fixture.InstalledUnchanged() && AsyncRestoreTreeMatches(fixture.Source, fixture.SourceHashes),
                        "real early updater refusal reports failure on the owner, permits a later retry and preserves every installed and selected backup byte");
                }
                finally {
                    helper.ReleaseExit(); retryHelper.ReleaseExit(); fixture.Context.Dispose();
                    WaitAsyncRestoreWithoutPump(fixture.Context.UpdateWork ?? pending, 25000);
                }
            }
        }

        private static void AsyncUpdaterPreparationOwnership(string root, bool forcedDispose)
        {
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var helper = new AsyncRestoreHelper(root, "updater-preparation-" + forcedDispose))
            using (var fixture = new AsyncUpdaterContextFixture(root))
            using (var pulse = new System.Windows.Forms.Timer { Interval = 10 })
            {
                int ownerThread = Thread.CurrentThread.ManagedThreadId, factoryThread = 0, factories = 0, ticks = 0;
                fixture.Context.UpdaterLaunchInfo = () => {
                    factoryThread = Thread.CurrentThread.ManagedThreadId; Interlocked.Increment(ref factories); entered.Set(); release.Wait();
                    return helper.Info(fixture.Source);
                };
                pulse.Tick += delegate { ticks++; }; pulse.Start();
                var pending = fixture.Context.StartUpdaterAsync(UpdateOffer());
                try {
                    PumpAsyncRestore(() => entered.IsSet && ticks >= 3);
                    var session = (BackupWorkSession)Field(fixture.Context, "backupActivity");
                    var progress = (BackupWorkProgressForm)Field(fixture.Context, "backupActivityForm");
                    Check(factoryThread != ownerThread && factories == 1 && !helper.Started && !pending.IsCompleted &&
                        Object.ReferenceEquals(pending, fixture.Context.StartUpdaterAsync(UpdateOffer())),
                        "slow updater launch preparation runs off the native owner and retains exactly one reserved actor before process creation");
                    int lateUi = 0;
                    if (forcedDispose) ((Label)Field(progress, "status")).TextChanged += delegate { lateUi++; };
                    var watch = Stopwatch.StartNew();
                    if (forcedDispose) fixture.Context.Dispose(); else ((Button)progress.CancelButton).PerformClick();
                    watch.Stop();
                    Check(watch.ElapsedMilliseconds < 500 && session.Token.IsCancellationRequested && !pending.IsCompleted &&
                        (forcedDispose ? progress.IsDisposed : progress.IsBusy && fixture.Context.IsBackupRunning),
                        "cancel or forced disposal returns promptly while the uninterruptible preparation call remains owned: " + forcedDispose);
                    release.Set();
                    if (forcedDispose) WaitAsyncRestoreWithoutPump(pending, 25000);
                    else PumpAsyncRestore(() => pending.IsCompleted && !fixture.Context.IsBackupRunning);
                    Check(!pending.IsFaulted && !helper.Started && factories == 1 && lateUi == 0 && fixture.CliCleanup == 1 && fixture.WindowsCleanup == 1 &&
                        fixture.InstalledUnchanged() && AsyncRestoreTreeMatches(fixture.Source, fixture.SourceHashes),
                        "settled late preparation cannot start a helper after cancellation or disposal and preserves original data without late owner publication: " + forcedDispose);
                    Application.DoEvents();
                    Check(!helper.Started && (forcedDispose ? progress.IsDisposed && lateUi == 0 : fixture.Commands[AppCommand.Update].Enabled),
                        "resumed dispatch cannot launch delayed updater work and only a live settled owner regains maintenance commands: " + forcedDispose);
                }
                finally { release.Set(); fixture.Context.Dispose(); WaitAsyncRestoreWithoutPump(pending, 25000); }
            }
        }

        private static void AsyncUpdaterShutdownReservation(string root)
        {
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var helper = new AsyncRestoreHelper(root, "updater-reserved-shutdown"))
            using (var fixture = new AsyncUpdaterContextFixture(root))
            using (var pulse = new System.Windows.Forms.Timer { Interval = 10 })
            {
                int factories = 0, ticks = 0;
                fixture.Context.UpdaterLaunchInfo = () => { Interlocked.Increment(ref factories); entered.Set(); release.Wait(); return helper.Info(fixture.Source); };
                pulse.Tick += delegate { ticks++; }; pulse.Start();
                var pending = fixture.Context.StartUpdaterAsync(UpdateOffer());
                try {
                    PumpAsyncRestore(() => entered.IsSet);
                    var watch = Stopwatch.StartNew(); var shutdown = fixture.Context.RequestShutdownAsync(); var duplicate = fixture.Context.RequestShutdownAsync();
                    PumpAsyncRestore(() => shutdown.IsCompleted, 10000); watch.Stop();
                    fixture.Context.CompleteShutdown(); Application.DoEvents();
                    Check(Object.ReferenceEquals(shutdown, duplicate) && !shutdown.Result && !pending.IsCompleted && fixture.Context.IsBackupRunning &&
                        factories == 1 && !helper.Started && fixture.CliCleanup == 1 && fixture.WindowsCleanup == 1 && !(bool)Field(fixture.Context, "closing"),
                        "external updater shutdown shares its reservation, cancels pending creation and refuses exit before actual worker settlement without repeating native cleanup");
                    Check(watch.ElapsedMilliseconds >= 2800 && watch.ElapsedMilliseconds < 6000 && ticks >= 20 &&
                        new[] { AppCommand.Update, AppCommand.Connect, AppCommand.Settings }.All(c => !fixture.Commands[c].Enabled),
                        "production reserved-shutdown deadline leaves the native UI ticking and prevents fresh mutations even after timeout resets ordinary flags");
                    release.Set(); PumpAsyncRestore(() => pending.IsCompleted && !fixture.Context.IsBackupRunning);
                    var retry = fixture.Context.RequestShutdownAsync(); PumpAsyncRestore(() => retry.IsCompleted);
                    int after = ticks; PumpAsyncRestore(() => ticks >= after + 3);
                    Check(retry.Result && !helper.Started && factories == 1 && fixture.CliCleanup == 2 && fixture.WindowsCleanup == 2 &&
                        (bool)Field(fixture.Context, "shutdownPrepared") && fixture.InstalledUnchanged() && AsyncRestoreTreeMatches(fixture.Source, fixture.SourceHashes),
                        "explicit shutdown retry performs one new preflight only after cancelled updater settlement and stale completion cannot undo the accepted shutdown");
                    fixture.Context.CancelShutdown(); PumpAsyncRestore(() => fixture.Commands[AppCommand.Update].Enabled);
                }
                finally { release.Set(); fixture.Context.Dispose(); WaitAsyncRestoreWithoutPump(pending, 25000); }
            }
        }

        private static void AsyncUpdaterStaleOrdinaryShutdown(string root, bool completeInsideCallback)
        {
            using (var ownerEntered = new ManualResetEventSlim())
            using (var ownerRelease = new ManualResetEventSlim())
            using (var cleanupEntered = new ManualResetEventSlim())
            using (var cleanupRelease = new ManualResetEventSlim())
            using (var fixture = new AsyncUpdaterContextFixture(root))
            {
                int factories = 0, posted = 0; bool callbackFlagsSafe = false;
                fixture.Context.UpdaterLaunchInfo = () => { Interlocked.Increment(ref factories); return null; };
                fixture.Context.BackupIoProbe = delegate(string phase, CancellationToken token) {
                    if (phase == "backup-owner-completion") { ownerEntered.Set(); ownerRelease.Wait(); }
                };
                var pending = fixture.Context.StartUpdaterAsync(UpdateOffer()); Task<bool> ordinary = null, stale = null;
                try {
                    PumpAsyncRestore(() => ownerEntered.IsSet);
                    var progress = (BackupWorkProgressForm)Field(fixture.Context, "backupActivityForm");
                    ((Label)Field(progress, "status")).TextChanged += delegate {
                        if (Interlocked.Increment(ref posted) != 1) return;
                        // Reentrant ordinary shutdown begins after the rejected
                        // actor released its lease but before its callback returns.
                        if (!completeInsideCallback)
                            fixture.BeforeWindowsCleanup = delegate { cleanupEntered.Set(); cleanupRelease.Wait(); };
                        ordinary = fixture.Context.RequestShutdownAsync();
                        if (completeInsideCallback) {
                            PumpAsyncRestore(() => ordinary.IsCompleted, 10000);
                            callbackFlagsSafe = ordinary.Result && (bool)Field(fixture.Context, "shutdownPrepared") &&
                                !(bool)Field(fixture.Context, "shutdownPreparing") && stale != null && !stale.IsCompleted;
                        }
                    };
                    var watch = Stopwatch.StartNew(); stale = fixture.Context.RequestShutdownAsync(); ownerRelease.Set();
                    PumpAsyncRestore(() => stale.IsCompleted && (completeInsideCallback || cleanupEntered.IsSet), 10000); watch.Stop();
                    Check(pending.IsCompleted && !stale.Result && ordinary != null && posted == 1 && factories == 1 &&
                        watch.ElapsedMilliseconds < 2800 && fixture.CliCleanup == 2 && fixture.WindowsCleanup == 2 &&
                        Field(fixture.Context, "backupActivity") == null && Field(fixture.Context, "maintenanceReservation") == null &&
                        (completeInsideCallback ? callbackFlagsSafe && ordinary.IsCompleted && (bool)Field(fixture.Context, "shutdownPrepared") && !(bool)Field(fixture.Context, "shutdownPreparing") :
                            !ordinary.IsCompleted && (bool)Field(fixture.Context, "shutdownPreparing") && !(bool)Field(fixture.Context, "shutdownPrepared")),
                        "rejected form callback may start or finish a fresh ordinary shutdown before obsolete updater callbacks without cached success, erased flags or stuck preparation: " + completeInsideCallback);
                    cleanupRelease.Set(); PumpAsyncRestore(() => ordinary.IsCompleted);
                    Check(ordinary.Result && (bool)Field(fixture.Context, "shutdownPrepared") && !(bool)Field(fixture.Context, "shutdownPreparing") &&
                        fixture.InstalledUnchanged() && AsyncRestoreTreeMatches(fixture.Source, fixture.SourceHashes),
                        "the newer ordinary shutdown completes its own native cleanup once and an obsolete reservation cannot leave preparation stuck or erase success");
                    fixture.Context.CancelShutdown(); PumpAsyncRestore(() => fixture.Commands[AppCommand.Update].Enabled);
                }
                finally {
                    ownerRelease.Set(); cleanupRelease.Set(); fixture.Context.Dispose(); WaitAsyncRestoreWithoutPump(pending, 25000);
                    if (stale != null) WaitAsyncRestoreWithoutPump(stale, 5000);
                    if (ordinary != null) WaitAsyncRestoreWithoutPump(ordinary, 5000);
                }
            }
        }

        private static void AsyncUpdaterTimeout(string root)
        {
            using (var helper = new AsyncRestoreHelper(root, "updater-context-timeout"))
            using (var fixture = new AsyncUpdaterContextFixture(root))
            using (var pulse = new System.Windows.Forms.Timer { Interval = 10 })
            {
                int ticks = 0, factories = 0; pulse.Tick += delegate { ticks++; }; pulse.Start();
                fixture.Context.UpdaterLaunchInfo = () => { Interlocked.Increment(ref factories); return helper.Info(fixture.Source); };
                var watch = Stopwatch.StartNew(); var pending = fixture.Context.StartUpdaterAsync(UpdateOffer());
                try {
                    PumpAsyncRestore(() => helper.Started || pending.IsCompleted, 10000);
                    Check(helper.Started && !pending.IsCompleted, "actual updater context retains a real pending helper before its ACK deadline");
                    helper.CaptureProcesses();
                    PumpAsyncRestore(() => pending.IsCompleted && !fixture.Context.IsBackupRunning, 25000); watch.Stop();
                    Check(!pending.IsFaulted && helper.Exited && !helper.Acknowledged && factories == 1 && fixture.Context.BackupStatus.Contains("15 секунд и остановлен") &&
                        fixture.Commands[AppCommand.Update].Enabled && !(bool)Field(fixture.Context, "closing") && !(bool)Field(fixture.Context, "shutdownPrepared"),
                        "updater owner reports the actual no-ACK timeout only after contained helper and descendant settlement and remains open for retry");
                    Check(watch.ElapsedMilliseconds >= 14500 && watch.ElapsedMilliseconds < 25000 && ticks >= 20,
                        "the real fifteen-second updater-ready timeout runs off the UI while native heartbeat continues");
                    helper.ReleaseAcknowledgement(); Application.DoEvents();
                    Check(helper.Exited && !helper.Acknowledged && fixture.InstalledUnchanged() && AsyncRestoreTreeMatches(fixture.Source, fixture.SourceHashes),
                        "a late ACK request cannot revive a timed-out owned updater or modify selected backup and installed data");
                }
                finally { helper.ReleaseExit(); fixture.Context.Dispose(); WaitAsyncRestoreWithoutPump(pending, 25000); }
            }
        }

        private static void AsyncUpdaterCheckBlockedAdmissions(AsyncUpdaterContextFixture fixture)
        {
            long revision = fixture.Settings.Revision; var previousSettings = fixture.Context.SettingsWork; var previousPreference = fixture.Context.TrayPreferenceWork;
            var proposed = fixture.Settings.Current.Clone(); proposed.AutoStartSocks = !proposed.AutoStartSocks;
            var save = fixture.Context.ApplySettingsAsync(proposed, false, CancellationToken.None);
            var bridge = fixture.Context.EnsureBridgeAsync(CancellationToken.None);
            Check(save.IsCompleted && save.Result != null && bridge.IsCompleted && bridge.Result != null &&
                Object.ReferenceEquals(previousSettings, fixture.Context.SettingsWork) && !fixture.Context.IntegrationPending && !fixture.Bridge.IsRunning &&
                fixture.Settings.Revision == revision && fixture.InstalledUnchanged(),
                "a still-reserved accepted handoff rejects direct settings and bridge admissions after external timeout without a new mutation or listener");
            fixture.Context.RequestShowStatus(); Application.DoEvents(); var dashboard = (MainWindow)Field(fixture.Context, "mainWindow");
            Check(dashboard != null && dashboard.Visible && !fixture.Settings.Current.TrayCloseExplained,
                "the real dashboard opens with an unexplained tray preference during held accepted owner completion");
            dashboard.Close(); Application.DoEvents();
            Check(Object.ReferenceEquals(previousPreference, fixture.Context.TrayPreferenceWork) && fixture.Settings.Revision == revision &&
                !fixture.Settings.Current.TrayCloseExplained && fixture.InstalledUnchanged(),
                "real dashboard close cannot enqueue a tray-preference write while accepted maintenance still owns the preflight");
        }

        private static int AsyncUpdaterQueuedOwnerPublications(UpdateAwareTrayApplicationContext context)
        {
            lock (Field(context, "uiCompletionGate"))
                return ((ICollection<Action>)Field(context, "uiCompletions")).Count;
        }
        private static void AsyncUpdaterWaitWithoutPump(Func<bool> ready, int milliseconds)
        {
            var watch = Stopwatch.StartNew();
            while (!ready()) {
                if (watch.ElapsedMilliseconds > milliseconds) throw new Exception("ownerless updater queue deadline");
                Thread.Sleep(10);
            }
        }

        private static void AsyncUpdaterAcceptedOwnerCompletion(string root, string mode)
        {
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var helper = new AsyncRestoreHelper(root, "updater-accepted-" + mode))
            using (var fixture = new AsyncUpdaterContextFixture(root))
            using (var pulse = new System.Windows.Forms.Timer { Interval = 10 })
            {
                int completions = 0, ticks = 0;
                fixture.Context.UpdaterLaunchInfo = () => helper.Info(fixture.Source);
                fixture.Context.BackupIoProbe = delegate(string phase, CancellationToken token) {
                    if (phase == "backup-owner-completion") { Interlocked.Increment(ref completions); entered.Set(); release.Wait(); }
                };
                pulse.Tick += delegate { ticks++; }; pulse.Start();
                var pending = fixture.Context.StartUpdaterAsync(UpdateOffer()); Task<bool> shutdown = null;
                try {
                    PumpAsyncRestore(() => helper.Started || pending.IsCompleted, 10000);
                    Check(helper.Started && !pending.IsCompleted, "accepted-owner fixture starts a real contained updater helper: " + mode);
                    helper.CaptureProcesses(); helper.ReleaseAcknowledgement();
                    PumpAsyncRestore(() => entered.IsSet && helper.Acknowledged, 10000);
                    var session = (BackupWorkSession)Field(fixture.Context, "backupActivity");
                    var progress = (BackupWorkProgressForm)Field(fixture.Context, "backupActivityForm");
                    Check(session.CurrentWork.IsCompleted && !pending.IsCompleted && helper.RootAlive && fixture.Context.IsBackupRunning &&
                        Field(fixture.Context, "maintenanceReservation") != null && fixture.CliCleanup == 1 && fixture.WindowsCleanup == 1,
                        "real updater ACK transfers helper ownership while the context retains its reservation through final owner completion: " + mode);
                    int lateUi = 0; bool callbackSafe = false;
                    ((Label)Field(progress, "status")).TextChanged += delegate {
                        lateUi++;
                        if (((Label)Field(progress, "status")).Text.Contains("Помощник принял обновление")) {
                            callbackSafe = (bool)Field(fixture.Context, "maintenanceHandedOff") && (bool)Field(fixture.Context, "shutdownPrepared");
                            fixture.Context.CancelShutdown();
                        }
                    };
                    if (mode == "dispose") {
                        var watch = Stopwatch.StartNew(); fixture.Context.Dispose(); watch.Stop();
                        Check(watch.ElapsedMilliseconds < 500 && progress.IsDisposed && !pending.IsCompleted && helper.RootAlive,
                            "forced disposal after real updater ACK cannot revoke or kill the accepted independent helper");
                        release.Set(); WaitAsyncRestoreWithoutPump(pending, 25000); Application.DoEvents();
                        Check(!pending.IsFaulted && lateUi == 0 && progress.IsDisposed && helper.RootAlive && helper.Acknowledged,
                            "accepted ownerless updater settles without a UI pump or late publication while its transferred helper remains alive");
                    } else {
                        fixture.Context.CancelShutdown(); Application.DoEvents(); fixture.Context.CompleteShutdown(); Application.DoEvents();
                        Check((bool)Field(fixture.Context, "shutdownPrepared") && !(bool)Field(fixture.Context, "closing"),
                            "late CancelShutdown and premature CompleteShutdown cannot change a still-reserved accepted preflight: " + mode);
                        shutdown = fixture.Context.RequestShutdownAsync(); var duplicate = fixture.Context.RequestShutdownAsync();
                        Check(Object.ReferenceEquals(shutdown, duplicate), "late external shutdown requests share their accepted-but-unpublished updater reservation: " + mode);
                        if (mode == "timeout") {
                            PumpAsyncRestore(() => shutdown.IsCompleted, 10000);
                            Check(!shutdown.Result && !pending.IsCompleted && helper.RootAlive && !(bool)Field(fixture.Context, "shutdownPrepared") && ticks >= 20 &&
                                new[] { AppCommand.Update, AppCommand.Connect, AppCommand.Settings }.All(c => !fixture.Commands[c].Enabled),
                                "external timeout may refuse exit while accepted owner completion is held but cannot admit new route or settings changes");
                            AsyncUpdaterCheckBlockedAdmissions(fixture);
                            release.Set();
                        } else {
                            int queued = AsyncUpdaterQueuedOwnerPublications(fixture.Context); release.Set();
                            AsyncUpdaterWaitWithoutPump(() => AsyncUpdaterQueuedOwnerPublications(fixture.Context) >= queued + 1, 5000);
                            Check(!pending.IsCompleted && !shutdown.IsCompleted,
                                "accepted updater owner publication is queued first while the native message pump is stopped");
                            AsyncUpdaterWaitWithoutPump(() => AsyncUpdaterQueuedOwnerPublications(fixture.Context) >= queued + 2, 10000);
                            Check(!pending.IsCompleted && !shutdown.IsCompleted,
                                "expired external shutdown publication queues second behind accepted updater completion before the pump resumes");
                        }
                        PumpAsyncRestore(() => pending.IsCompleted && (bool)Field(fixture.Context, "closing"), 10000);
                        WaitAsyncRestoreWithoutPump(shutdown, 5000);
                        Check(!pending.IsFaulted && callbackSafe && completions == 1 && (bool)Field(fixture.Context, "maintenanceHandedOff") &&
                            (bool)Field(fixture.Context, "shutdownPrepared") && !(bool)Field(fixture.Context, "lastBackupCancelled") &&
                            !(bool)Field(fixture.Context, "lastBackupFailed") && helper.RootAlive && helper.Acknowledged &&
                            fixture.CliCleanup == 1 && fixture.WindowsCleanup == 1 && (mode != "queued-timeout" || shutdown.Result),
                            "accepted updater commit is visible before reentrant form callbacks and survives late cancel or stale queued timeout while completing owner exit: " + mode);
                    }
                    helper.ReleaseExit(); PumpAsyncRestore(() => helper.Exited, 10000);
                    Check(fixture.InstalledUnchanged() && AsyncRestoreTreeMatches(fixture.Source, fixture.SourceHashes) && !Directory.Exists(helper.ChildCopy),
                        "accepted updater fixture releases its independent child snapshot and descendant without installing or changing preserved data: " + mode);
                }
                finally {
                    release.Set(); helper.ReleaseExit(); fixture.Context.Dispose(); WaitAsyncRestoreWithoutPump(pending, 25000);
                    if (shutdown != null) WaitAsyncRestoreWithoutPump(shutdown, 5000);
                }
            }
        }
    }
}

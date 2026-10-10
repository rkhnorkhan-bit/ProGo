using System;
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
        private static void StartupBackupWorkflow()
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") return;
            if (File.Exists(CliProxyEnvironmentService.BackupPath) || File.Exists(SystemProxyService.BackupPath))
                throw new Exception("startup backup fixture is not isolated from proxy ownership");
            AppPaths.EnsureDirectories();
            var names = new[] { "ProGo.exe", "VERSION", "settings.json", "vault.enc.json",
                "scripts/Start-ProGo.ps1", "scripts/Restore-ProGoBackup.ps1", "scripts/Update-ProGo.Core.ps1" };
            var originals = names.ToDictionary(n => n, n => File.Exists(Path.Combine(AppPaths.Root, n)) ? File.ReadAllBytes(Path.Combine(AppPaths.Root, n)) : null);
            var hadScripts = Directory.Exists(Path.Combine(AppPaths.Root, "scripts"));
            var previousBackups = Path.Combine(AppPaths.Root, "startup-backup-originals-" + Guid.NewGuid().ToString("N"));
            bool movedBackups = false, ownsBackups = false, sourceTouched = false;
            Task pending = null;
            using (var probeEntered = new ManualResetEventSlim())
            using (var probeRelease = new ManualResetEventSlim())
            using (var copyEntered = new ManualResetEventSlim())
            using (var copyRelease = new ManualResetEventSlim())
            using (var instance = new ApplicationInstance())
            try
            {
                Check(instance.IsOwner, "isolated startup fixture owns the real application instance before touching installed source files");
                // Keep real retention away from unrelated copies and use a same-volume
                // rename even when the CI workspace is on a different Windows drive.
                if (Directory.Exists(BackupService.BackupsRoot)) {
                    Directory.Move(BackupService.BackupsRoot, previousBackups); movedBackups = true;
                }
                ownsBackups = true;
                sourceTouched = true;
                foreach (var name in names) {
                    var path = Path.Combine(AppPaths.Root, name); Directory.CreateDirectory(Path.GetDirectoryName(path));
                    File.WriteAllText(path, name == "VERSION" ? "0.0.1" : name == "settings.json" ? "{}" : "opaque synthetic fixture; never execute");
                }
                var baseline = BackupService.CreateBackup("baseline");
                var baselineHashes = ManualBackupHashes(baseline);
                var sourceHashes = names.ToDictionary(n => n, n => ManualBackupHash(Path.Combine(AppPaths.Root, n)));
                var before = Directory.GetDirectories(BackupService.BackupsRoot);
                int probes = 0, copies = 0, cleanupCalls = 0; bool attachedAtProbe = false;
                CancellationToken probeToken = default(CancellationToken), copyToken = default(CancellationToken);
                Action<CancellationToken> probe = delegate(CancellationToken token) {
                    probeToken = token; attachedAtProbe = instance.IsOwner && Field(instance, "activate") != null && Field(instance, "prepareShutdown") != null;
                    Interlocked.Increment(ref probes); probeEntered.Set(); probeRelease.Wait(token);
                };
                Action<string, CancellationToken> copy = delegate(string path, CancellationToken token) {
                    if (!String.Equals(Path.GetFileName(path), "ProGo.exe", StringComparison.OrdinalIgnoreCase)) return;
                    copyToken = token; Interlocked.Increment(ref copies); copyEntered.Set(); copyRelease.Wait(token);
                };
                using (var settings = new SettingsService())
                using (var proxy = new ProxyService(() => settings.Current, s => settings.Save(s), "unused-test-ssh", () => DateTime.UtcNow, false))
                using (var bridge = new CliProxyBridgeService(settings))
                using (var relay = new Ikev2RelayService())
                using (var home = new HomeVpnService(relay))
                using (var clipboard = new ClipboardService(settings))
                using (var monitor = new ConnectionHealthMonitor(() => settings.Current))
                {
                    int ownerThread = Thread.CurrentThread.ManagedThreadId;
                    Func<Action<string, CancellationToken>, int, Action<CancellationToken>, UpdateAwareTrayApplicationContext> contextFor =
                        (copyHook, timeout, probeHook) => new UpdateAwareTrayApplicationContext(settings, proxy, bridge, home, clipboard, false, monitor,
                            () => { Interlocked.Increment(ref cleanupCalls); return new WindowsProxyRestoreResult(); }, null, copyHook, timeout, probeHook);
                    using (var context = contextFor(copy, 3000, probe))
                    using (var pulse = new System.Windows.Forms.Timer { Interval = 10 })
                    {
                        Check(Volatile.Read(ref probes) == 0 && Volatile.Read(ref copies) == 0 && Field(context, "backupForm") == null,
                            "constructing the native owner does not probe backups, copy files or open a startup dialog");
                        instance.Attach(context.RequestShowStatus, context.RequestShutdown, context.CompleteShutdown, context.CancelShutdown);
                        pending = context.StartStartupBackupAsync();
                        Check(!pending.IsCompleted && Volatile.Read(ref probes) == 0 && !probeEntered.IsSet,
                            "startup baseline scheduling returns before the first owner dispatch and any backup I/O");
                        Check(Object.ReferenceEquals(pending, context.StartupBackupCompletion) && Object.ReferenceEquals(pending, context.StartStartupBackupAsync()),
                            "startup baseline scheduling is one-shot and exposes the same full-operation task");
                        int pulses = 0; pulse.Tick += delegate { pulses++; }; pulse.Start();
                        PumpUntil(() => probeEntered.IsSet && pulses >= 3);
                        Check(attachedAtProbe && context.IsBackupRunning && context.IsManualBackupRunning && context.BackupOperationKind == "baseline" &&
                            Volatile.Read(ref probes) == 1 && Volatile.Read(ref copies) == 0 && Field(context, "backupForm") == null,
                            "one real startup probe runs after owner attachment off the UI with quiet progress and native heartbeat");
                        var commands = MenuItems(((NotifyIcon)Field(context, "tray")).ContextMenuStrip.Items).Where(i => i.Tag is AppCommand).ToDictionary(i => (AppCommand)i.Tag);
                        Check(new[] { AppCommand.CreateBackup, AppCommand.RestoreBackup, AppCommand.CleanupBackups, AppCommand.Update }.All(c => !commands[c].Enabled) &&
                            commands[AppCommand.Settings].Enabled && commands[AppCommand.StopDesktop].Enabled,
                            "startup baseline probe shares the actual tray mutation gate while settings and connection stop remain usable");
                        context.RequestShowStatus(); Application.DoEvents();
                        var main = (MainWindow)Field(context, "mainWindow");
                        var progressLink = Descendants(main).OfType<LinkLabel>().Single(l => l.Name == "BackupProgress");
                        Check(progressLink.Visible && progressLink.Text == context.BackupStatus && progressLink.AccessibleName == "Состояние резервной копии",
                            "native dashboard exposes readable startup backup status while the real baseline probe is blocked");
                        CloseServiceDialog(commands[AppCommand.Diagnostics], typeof(StatusForm), delegate(Form form) {
                            Check(((Label)Field(form, "backupState")).Text == context.BackupStatus,
                                "real tray diagnostics shows the same startup backup status without starting a route test");
                        });
                        CloseServiceDialog(commands[AppCommand.Settings], typeof(SshProfilesSettingsForm), delegate(Form form) {
                            Check(context.IsBackupRunning && !pending.IsCompleted, "native settings dialog opens while the baseline scan is blocked");
                        });
                        int beforeStop = Volatile.Read(ref cleanupCalls);
                        commands[AppCommand.StopDesktop].PerformClick(); PumpUntil(() => !proxy.IsStopping);
                        Check(Volatile.Read(ref cleanupCalls) == beforeStop + 1 && context.IsBackupRunning && !pending.IsCompleted,
                            "real tray desktop stop executes independently while the startup baseline probe remains blocked");
                        var statusItem = (ToolStripMenuItem)Field(context, "backupStatusItem");
                        Check(statusItem != null && statusItem.Enabled && statusItem.ToolTipText.Contains(context.BackupStatus) && statusItem.AccessibleDescription.Contains(context.BackupStatus),
                            "tray exposes a separate current-backup status action without enabling a conflicting create command");
                        statusItem.PerformClick();
                        var manual = context.CreateManualBackupAsync();
                        var progress = (BackupCreationForm)Field(context, "backupForm"); int applied = 0, appliedThread = 0;
                        progress.ResultApplied += delegate { applied++; appliedThread = Thread.CurrentThread.ManagedThreadId; };
                        Check(Object.ReferenceEquals(manual, context.ManualBackupCompletion) && context.BackupOperationKind == "baseline" && progress.Visible &&
                            progress.Message.IndexOf("стартов", StringComparison.OrdinalIgnoreCase) >= 0,
                            "manual request during startup shows the existing baseline actor instead of queuing or mislabelling a manual copy");
                        probeRelease.Set(); PumpUntil(() => pending.IsCompleted && manual.IsCompleted && !context.IsBackupRunning);
                        Check(!pending.IsFaulted && Volatile.Read(ref probes) == 1 && Volatile.Read(ref copies) == 0 &&
                            Directory.GetDirectories(BackupService.BackupsRoot).OrderBy(p => p).SequenceEqual(before.OrderBy(p => p)) && ManualBackupTreeMatches(baseline, baselineHashes),
                            "actual valid indexed baseline is reused without a new copy or rewriting its files");
                        Check(applied == 1 && appliedThread == ownerThread && !progress.IsBusy && progress.Message.IndexOf("создана", StringComparison.OrdinalIgnoreCase) < 0,
                            "baseline reuse applies one honest skip result on the UI without false manual-copy success");
                        Check(progressLink.Text == context.BackupStatus && statusItem.ToolTipText.Contains(context.BackupStatus),
                            "settled baseline result replaces pending status in both native dashboard and tray");
                        ((System.Windows.Forms.Timer)Field(context, "statusTimer")).Interval = 10;
                        int after = pulses;
                        bool sameTask = true;
                        for (int i = 0; i < 100; i++) sameTask &= Object.ReferenceEquals(pending, context.StartStartupBackupAsync());
                        Check(sameTask, "100 repeated startup requests retain the completed one-shot task");
                        PumpUntil(() => pulses >= after + 4);
                        Check(Volatile.Read(ref probes) == 1 && Volatile.Read(ref copies) == 0 && sourceHashes.All(p => ManualBackupHash(Path.Combine(AppPaths.Root, p.Key)) == p.Value),
                            "native status refresh and repeated scheduling never start a backup reread loop or mutate source data");
                        Shot(progress, "startup-baseline-reused"); progress.Close(); pending = null; pulse.Stop();
                    }

                    probes = copies = 0; probeEntered.Reset(); probeRelease.Reset(); copyEntered.Reset(); copyRelease.Set();
                    using (var context = contextFor(copy, 3000, probe))
                    {
                        instance.Attach(context.RequestShowStatus, context.RequestShutdown, context.CompleteShutdown, context.CancelShutdown);
                        pending = context.StartStartupBackupAsync();
                        var manual = context.CreateManualBackupAsync();
                        var progress = (BackupCreationForm)Field(context, "backupForm");
                        Check(Object.ReferenceEquals(manual, pending) && progress.IsBusy && progress.Visible,
                            "manual progress requested before first startup dispatch shares the queued baseline task");
                        PumpUntil(() => probeEntered.IsSet || pending.IsCompleted);
                        Check(probeEntered.IsSet && Volatile.Read(ref probes) == 1 && !probeToken.IsCancellationRequested &&
                            Object.ReferenceEquals(progress, Field(context, "backupForm")) && progress.IsBusy && !pending.IsCompleted,
                            "starting queued baseline keeps its already-open busy form without accidentally closing it and cancelling the worker");
                        Descendants(progress).OfType<Button>().Single(b => b.Name == "BackupCancel").PerformClick();
                        PumpUntil(() => pending.IsCompleted && !context.IsBackupRunning);
                        Check(probeToken.IsCancellationRequested && Volatile.Read(ref copies) == 0 && !progress.IsBusy &&
                            Directory.GetDirectories(BackupService.BackupsRoot).OrderBy(p => p).SequenceEqual(before.OrderBy(p => p)) && ManualBackupTreeMatches(baseline, baselineHashes),
                            "explicit cancellation of pre-dispatch progress settles the real probe and preserves the existing baseline");
                        progress.Close(); pending = null;
                    }

                    // A missing real shared digest index cannot satisfy readiness.
                    var index = File.ReadAllBytes(Path.Combine(baseline, BackupIntegrity.IndexName));
                    File.Delete(Path.Combine(baseline, BackupIntegrity.IndexName));
                    probes = copies = 0; probeEntered.Reset(); probeRelease.Set(); copyEntered.Reset(); copyRelease.Reset();
                    using (var context = contextFor(copy, 3000, probe))
                    using (var pulse = new System.Windows.Forms.Timer { Interval = 10 })
                    {
                        instance.Attach(context.RequestShowStatus, context.RequestShutdown, context.CompleteShutdown, context.CancelShutdown);
                        int pulses = 0; pulse.Tick += delegate { pulses++; }; pulse.Start();
                        pending = context.StartStartupBackupAsync(); PumpUntil(() => copyEntered.IsSet && pulses >= 3);
                        Check(Volatile.Read(ref probes) == 1 && Volatile.Read(ref copies) == 1 && context.IsBackupRunning && context.BackupOperationKind == "baseline" && !pending.IsCompleted,
                            "invalid existing baseline starts one real cancellable baseline copy while native UI remains responsive");
                        var manual = context.CreateManualBackupAsync();
                        var progress = (BackupCreationForm)Field(context, "backupForm"); int applied = 0, appliedThread = 0;
                        progress.ResultApplied += delegate { applied++; appliedThread = Thread.CurrentThread.ManagedThreadId; };
                        copyRelease.Set(); PumpUntil(() => pending.IsCompleted && manual.IsCompleted && !context.IsBackupRunning);
                        var created = Directory.GetDirectories(BackupService.BackupsRoot).Except(before).ToArray();
                        Check(!pending.IsFaulted && created.Length == 1 && applied == 1 && appliedThread == ownerThread,
                            "startup repair completes one indexed copy and applies the real result on the owner UI");
                        BackupIntegrity.Validate(created[0]);
                        var info = BackupService.ListBackups().Single(b => b.Path == created[0]);
                        Check(info.IsBaseline && !info.IsManual && new[] { "ProGo.exe", "VERSION", "settings.json", "vault.enc.json" }.All(n => ManualBackupHash(Path.Combine(created[0], n)) == sourceHashes[n]),
                            "repaired backup keeps baseline metadata and original opaque personal payloads rather than reporting a manual copy");
                        Shot(progress, "startup-baseline-created"); progress.Close(); pending = null; pulse.Stop();
                    }
                    File.WriteAllBytes(Path.Combine(baseline, BackupIntegrity.IndexName), index);
                    Check(ManualBackupTreeMatches(baseline, baselineHashes), "startup repair preserves the original baseline payloads and its saved index bytes");
                    before = Directory.GetDirectories(BackupService.BackupsRoot);

                    probes = copies = 0; probeEntered.Reset(); probeRelease.Reset(); copyEntered.Reset(); copyRelease.Set();
                    using (var context = contextFor(copy, 3000, probe))
                    {
                        instance.Attach(context.RequestShowStatus, context.RequestShutdown, context.CompleteShutdown, context.CancelShutdown);
                        pending = context.StartStartupBackupAsync(); PumpUntil(() => probeEntered.IsSet);
                        context.CreateManualBackupAsync();
                        var progress = (BackupCreationForm)Field(context, "backupForm");
                        var shutdown = context.RequestShutdownAsync(); PumpUntil(() => shutdown.IsCompleted && pending.IsCompleted && !context.IsBackupRunning);
                        Check(shutdown.Result && probeToken.IsCancellationRequested && Volatile.Read(ref copies) == 0 && !progress.IsBusy &&
                            progress.Message.IndexOf("отмен", StringComparison.OrdinalIgnoreCase) >= 0 &&
                            Directory.GetDirectories(BackupService.BackupsRoot).OrderBy(p => p).SequenceEqual(before.OrderBy(p => p)),
                            "normal shutdown cancels an in-flight baseline probe and waits for settlement without a new copy");
                        context.CancelShutdown();
                        var commands = MenuItems(((NotifyIcon)Field(context, "tray")).ContextMenuStrip.Items).Where(i => i.Tag is AppCommand).ToDictionary(i => (AppCommand)i.Tag);
                        PumpUntil(() => commands[AppCommand.Settings].Enabled);
                        Check(commands[AppCommand.CreateBackup].Enabled && commands[AppCommand.StopDesktop].Enabled,
                            "cancelled startup shutdown handoff restores backup and connection controls");
                        progress.Close(); pending = null;
                    }

                    probes = copies = 0; probeEntered.Reset(); probeRelease.Reset();
                    Action<CancellationToken> blockedProbe = delegate(CancellationToken token) {
                        probeToken = token; Interlocked.Increment(ref probes); probeEntered.Set(); probeRelease.Wait();
                    };
                    using (var context = contextFor(copy, 50, blockedProbe))
                    using (var pulse = new System.Windows.Forms.Timer { Interval = 10 })
                    {
                        instance.Attach(context.RequestShowStatus, context.RequestShutdown, context.CompleteShutdown, context.CancelShutdown);
                        int pulses = 0; pulse.Tick += delegate { pulses++; }; pulse.Start();
                        pending = context.StartStartupBackupAsync(); PumpUntil(() => probeEntered.IsSet);
                        var shutdown = context.RequestShutdownAsync(); PumpUntil(() => shutdown.IsCompleted);
                        var commands = MenuItems(((NotifyIcon)Field(context, "tray")).ContextMenuStrip.Items).Where(i => i.Tag is AppCommand).ToDictionary(i => (AppCommand)i.Tag);
                        Check(!shutdown.Result && probeToken.IsCancellationRequested && context.IsBackupRunning && !pending.IsCompleted && pulses > 0 &&
                            commands[AppCommand.Settings].Enabled && commands[AppCommand.StopDesktop].Enabled && !commands[AppCommand.CreateBackup].Enabled,
                            "blocked baseline scan refuses shutdown within its deadline while native settings and stop remain usable");
                        probeRelease.Set(); PumpUntil(() => pending.IsCompleted && !context.IsBackupRunning);
                        Check(Volatile.Read(ref probes) == 1 && Volatile.Read(ref copies) == 0 &&
                            Directory.GetDirectories(BackupService.BackupsRoot).OrderBy(p => p).SequenceEqual(before.OrderBy(p => p)),
                            "late cancelled baseline probe settles without copying or touching previous backups");
                        pending = null; pulse.Stop();
                    }

                    probes = copies = 0; probeEntered.Reset(); probeRelease.Set();
                    using (var context = contextFor(copy, 3000, probe))
                    {
                        instance.Attach(context.RequestShowStatus, context.RequestShutdown, context.CompleteShutdown, context.CancelShutdown);
                        pending = context.StartStartupBackupAsync(); context.Dispose();
                        PumpUntil(() => pending.IsCompleted);
                        Check(Volatile.Read(ref probes) == 0 && Volatile.Read(ref copies) == 0 && Field(context, "backupForm") == null,
                            "disposing an owner before its first dispatch cancels queued startup work without starting backup I/O or UI");
                        pending = null;
                    }

                    // A real denied source read must fail quietly, keep the cause visible,
                    // and never schedule automatic retry scans or report a ready copy.
                    File.WriteAllText(Path.Combine(AppPaths.Root, "VERSION"), "0.0.2");
                    sourceHashes["VERSION"] = ManualBackupHash(Path.Combine(AppPaths.Root, "VERSION"));
                    probes = copies = 0;
                    Action<CancellationToken> countProbe = delegate(CancellationToken token) { Interlocked.Increment(ref probes); };
                    using (var context = contextFor(null, 3000, countProbe))
                    using (var pulse = new System.Windows.Forms.Timer { Interval = 10 })
                    {
                        instance.Attach(context.RequestShowStatus, context.RequestShutdown, context.CompleteShutdown, context.CancelShutdown);
                        int pulses = 0; pulse.Tick += delegate { pulses++; }; pulse.Start();
                        using (var lockedSettings = new FileStream(AppPaths.SettingsPath, FileMode.Open, FileAccess.Read, FileShare.None)) {
                            pending = context.StartStartupBackupAsync(); PumpUntil(() => pending.IsCompleted && !context.IsBackupRunning);
                            Check(!pending.IsFaulted && Field(context, "backupForm") == null && context.BackupStatus.Contains("ошибка") && Volatile.Read(ref probes) == 1 &&
                                Directory.GetDirectories(BackupService.BackupsRoot).OrderBy(p => p).SequenceEqual(before.OrderBy(p => p)),
                                "actual locked source makes startup backup fail quietly with a Russian cause and no reported-ready partial folder");
                        }
                        context.RequestShowStatus(); Application.DoEvents();
                        var main = (MainWindow)Field(context, "mainWindow");
                        var progressLink = Descendants(main).OfType<LinkLabel>().Single(l => l.Name == "BackupProgress");
                        var statusItem = (ToolStripMenuItem)Field(context, "backupStatusItem");
                        Check(progressLink.Text == context.BackupStatus && statusItem.ToolTipText.Contains(context.BackupStatus) && statusItem.Text.Contains("ошибка"),
                            "quiet startup copy failure is visible in the dashboard and accessible tray status");
                        statusItem.PerformClick();
                        var progress = (BackupCreationForm)Field(context, "backupForm");
                        Check(progress.Visible && !progress.IsBusy && progress.Message.Contains("Не удалось") && progress.Message.Contains("Автоматических повторов нет"),
                            "explicit status action opens the settled startup error without retrying or showing a false success");
                        ((System.Windows.Forms.Timer)Field(context, "statusTimer")).Interval = 10;
                        int after = pulses; bool sameTask = true;
                        for (int i = 0; i < 100; i++) sameTask &= Object.ReferenceEquals(pending, context.StartStartupBackupAsync());
                        PumpUntil(() => pulses >= after + 4);
                        Check(sameTask && Volatile.Read(ref probes) == 1 && sourceHashes.All(p => ManualBackupHash(Path.Combine(AppPaths.Root, p.Key)) == p.Value) &&
                            ManualBackupTreeMatches(baseline, baselineHashes) && Directory.GetDirectories(BackupService.BackupsRoot).OrderBy(p => p).SequenceEqual(before.OrderBy(p => p)),
                            "failed one-shot startup operation never retries on native status refresh and preserves source and previous backup hashes");
                        Shot(progress, "startup-baseline-error"); progress.Close(); pending = null; pulse.Stop();
                    }

                    // This version still needs a baseline, so Dispose exercises the real
                    // copy actor rather than only cancelling a simulated slow scan.
                    probes = copies = 0; probeEntered.Reset(); copyEntered.Reset(); copyRelease.Reset();
                    Action<string, CancellationToken> blockedCopy = delegate(string path, CancellationToken token) {
                        if (!String.Equals(Path.GetFileName(path), "ProGo.exe", StringComparison.OrdinalIgnoreCase)) return;
                        copyToken = token; Interlocked.Increment(ref copies); copyEntered.Set(); copyRelease.Wait();
                    };
                    using (var context = contextFor(blockedCopy, 3000, probe))
                    {
                        instance.Attach(context.RequestShowStatus, context.RequestShutdown, context.CompleteShutdown, context.CancelShutdown);
                        pending = context.StartStartupBackupAsync(); PumpUntil(() => copyEntered.IsSet);
                        context.CreateManualBackupAsync();
                        var progress = (BackupCreationForm)Field(context, "backupForm"); int late = 0;
                        progress.ResultApplied += delegate { late++; };
                        var watch = Stopwatch.StartNew(); context.Dispose(); watch.Stop();
                        Check(watch.ElapsedMilliseconds < 500 && copyToken.IsCancellationRequested && !pending.IsCompleted && progress.IsDisposed,
                            "startup copy owner disposal cancels promptly without waiting for an individual blocked disk operation");
                        copyRelease.Set(); PumpUntil(() => pending.IsCompleted);
                        Check(late == 0 && progress.IsDisposed && Volatile.Read(ref copies) == 1 &&
                            Directory.GetDirectories(BackupService.BackupsRoot).OrderBy(p => p).SequenceEqual(before.OrderBy(p => p)) &&
                            ManualBackupTreeMatches(baseline, baselineHashes) && sourceHashes.All(p => ManualBackupHash(Path.Combine(AppPaths.Root, p.Key)) == p.Value),
                            "late cancelled startup copy removes only its partial folder and cannot resurrect UI or alter installed data and old backups");
                        pending = null;
                    }
                }
            }
            finally
            {
                probeRelease.Set(); copyRelease.Set();
                if (pending != null && !pending.IsCompleted) PumpUntil(() => pending.IsCompleted);
                if (ownsBackups && Directory.Exists(BackupService.BackupsRoot)) Directory.Delete(BackupService.BackupsRoot, true);
                if (movedBackups) Directory.Move(previousBackups, BackupService.BackupsRoot);
                if (sourceTouched) foreach (var pair in originals) {
                    var path = Path.Combine(AppPaths.Root, pair.Key);
                    if (pair.Value != null) File.WriteAllBytes(path, pair.Value);
                    else if (File.Exists(path)) File.Delete(path);
                }
                if (sourceTouched && !hadScripts && Directory.Exists(Path.Combine(AppPaths.Root, "scripts"))) Directory.Delete(Path.Combine(AppPaths.Root, "scripts"));
            }
        }
    }
}

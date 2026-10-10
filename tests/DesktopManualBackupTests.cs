using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        private static void ManualBackupWorkflow()
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") return;
            // Stop and Dispose must not find an unrelated owned environment to restore.
            if (File.Exists(CliProxyEnvironmentService.BackupPath) || File.Exists(SystemProxyService.BackupPath))
                throw new Exception("manual backup fixture is not isolated from proxy ownership");
            AppPaths.EnsureDirectories();
            var names = new[] { "ProGo.exe", "VERSION", "settings.json", "vault.enc.json",
                "scripts/Start-ProGo.ps1", "scripts/Restore-ProGoBackup.ps1", "scripts/Update-ProGo.Core.ps1" };
            var originals = names.ToDictionary(n => n, n => File.Exists(Path.Combine(AppPaths.Root, n)) ? File.ReadAllBytes(Path.Combine(AppPaths.Root, n)) : null);
            var hadScripts = Directory.Exists(Path.Combine(AppPaths.Root, "scripts"));
            var previousBackups = Path.Combine(AppPaths.Root, "manual-backup-originals-" + Guid.NewGuid().ToString("N"));
            bool movedBackups = false, ownsBackups = false;
            Task pending = null;
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var cancellationObserved = new ManualResetEventSlim())
            try
            {
                // Exercise the real retention path without offering any previous test/user
                // copies as deletion candidates. Restore that directory in finally.
                if (Directory.Exists(BackupService.BackupsRoot)) {
                    Directory.Move(BackupService.BackupsRoot, previousBackups); movedBackups = true;
                }
                ownsBackups = true;
                foreach (var name in names) {
                    var path = Path.Combine(AppPaths.Root, name); Directory.CreateDirectory(Path.GetDirectoryName(path));
                    File.WriteAllText(path, name == "VERSION" ? "0.0.1" : name == "settings.json" ? "{}" : "opaque synthetic fixture; never execute");
                }
                var previous = BackupService.CreateBackup("manual");
                var previousHashes = ManualBackupHashes(previous);
                var sourceHashes = names.ToDictionary(n => n, n => ManualBackupHash(Path.Combine(AppPaths.Root, n)));
                var before = Directory.GetDirectories(BackupService.BackupsRoot);
                int copies = 0, cleanupCalls = 0; bool delayCancellation = false; CancellationToken captured = default(CancellationToken);
                Action<string, CancellationToken> hold = delegate(string path, CancellationToken token) {
                    if (!String.Equals(Path.GetFileName(path), "ProGo.exe", StringComparison.OrdinalIgnoreCase)) return;
                    captured = token; Interlocked.Increment(ref copies); entered.Set();
                    try { release.Wait(token); }
                    catch (OperationCanceledException) {
                        if (delayCancellation) { cancellationObserved.Set(); release.Wait(); }
                        throw;
                    }
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
                    using (var context = new UpdateAwareTrayApplicationContext(settings, proxy, bridge, home, clipboard, false, monitor,
                        () => { cleanupCalls++; return new WindowsProxyRestoreResult(); }, null, hold))
                    using (var pulse = new System.Windows.Forms.Timer { Interval = 10 })
                    {
                        int pulses = 0; pulse.Tick += delegate { pulses++; }; pulse.Start();
                        pending = context.CreateManualBackupAsync();
                        PumpUntil(() => entered.IsSet);
                        var form = (BackupCreationForm)Field(context, "manualBackupForm");
                        int cancelResults = 0, cancelThread = 0;
                        form.ResultApplied += delegate { cancelResults++; cancelThread = Thread.CurrentThread.ManagedThreadId; };
                        PumpUntil(() => pulses >= 3);
                        Check(context.IsManualBackupRunning && form.IsBusy && form.Visible && !pending.IsCompleted,
                            "manual backup runs the real writer off the owner UI while a native timer keeps ticking");
                        Check(Object.ReferenceEquals(pending, context.ManualBackupCompletion) && Object.ReferenceEquals(pending, context.CreateManualBackupAsync()) && Volatile.Read(ref copies) == 1,
                            "duplicate manual backup shares its existing task and starts only one copy worker");
                        var tray = (NotifyIcon)Field(context, "tray");
                        var commands = MenuItems(tray.ContextMenuStrip.Items).Where(i => i.Tag is AppCommand).ToDictionary(i => (AppCommand)i.Tag);
                        Check(new[] { AppCommand.CreateBackup, AppCommand.RestoreBackup, AppCommand.CleanupBackups, AppCommand.Update }.All(c => !commands[c].Enabled),
                            "real tray disables backup, restore, retention and updater mutations during a manual copy");
                        Check(commands[AppCommand.Settings].Enabled && commands[AppCommand.StopDesktop].Enabled && commands[AppCommand.StopAll].Enabled,
                            "settings and connection stop commands remain enabled in the real tray during a manual copy");
                        CloseServiceDialog(commands[AppCommand.Settings], typeof(SshProfilesSettingsForm), delegate(Form settingsForm) {
                            Check(context.IsManualBackupRunning && form.IsBusy, "real settings dialog can be opened and cancelled without waiting for the copy worker");
                        });
                        commands[AppCommand.StopDesktop].PerformClick();
                        PumpUntil(() => !proxy.IsStopping);
                        Check(cleanupCalls == 1 && context.IsManualBackupRunning && !pending.IsCompleted,
                            "real tray desktop stop executes its independent cleanup while manual backup remains blocked");
                        Shot(form, "manual-backup-pending");
                        Descendants(form).OfType<Button>().Single(b => b.Name == "BackupCancel").PerformClick();
                        PumpUntil(() => pending.IsCompleted && !context.IsManualBackupRunning);
                        Check(captured.IsCancellationRequested && !pending.IsFaulted && !form.IsBusy && form.Message.IndexOf("отмен", StringComparison.OrdinalIgnoreCase) >= 0,
                            "manual copy cancellation reaches the real writer and reports a settled cancelled result");
                        Check(cancelResults == 1 && cancelThread == ownerThread,
                            "cancelled backup completion is applied once on the owner UI thread");
                        Check(Directory.GetDirectories(BackupService.BackupsRoot).OrderBy(p => p).SequenceEqual(before.OrderBy(p => p)) &&
                            ManualBackupTreeMatches(previous, previousHashes),
                            "cancel before first copy removes only its new partial directory and preserves the previous indexed backup");
                        Check(sourceHashes.All(p => ManualBackupHash(Path.Combine(AppPaths.Root, p.Key)) == p.Value),
                            "cancelled manual backup preserves installed program, settings, opaque vault and scripts bytes");
                        Check(new[] { AppCommand.CreateBackup, AppCommand.RestoreBackup, AppCommand.CleanupBackups, AppCommand.Update }.All(c => commands[c].Enabled),
                            "manual cancellation releases maintenance command availability for an explicit retry");
                        Shot(form, "manual-backup-cancelled");

                        entered.Reset(); release.Reset();
                        using (var lockedSettings = new FileStream(AppPaths.SettingsPath, FileMode.Open, FileAccess.Read, FileShare.None)) {
                            pending = context.CreateManualBackupAsync(); PumpUntil(() => entered.IsSet);
                            var failedForm = (BackupCreationForm)Field(context, "manualBackupForm");
                            int failedResults = 0, failedThread = 0;
                            failedForm.ResultApplied += delegate { failedResults++; failedThread = Thread.CurrentThread.ManagedThreadId; };
                            release.Set(); PumpUntil(() => pending.IsCompleted && !context.IsManualBackupRunning);
                            Check(!pending.IsFaulted && !failedForm.IsBusy && failedForm.Message.Contains("Не удалось создать копию") &&
                                failedResults == 1 && failedThread == ownerThread,
                                "real locked settings copy settles as one understandable failure on the owner UI thread");
                            Check(Directory.GetDirectories(BackupService.BackupsRoot).OrderBy(p => p).SequenceEqual(before.OrderBy(p => p)) &&
                                ManualBackupTreeMatches(previous, previousHashes),
                                "failed real file copy cannot leave a reported-ready partial backup or damage previous copies");
                            Shot(failedForm, "manual-backup-error");
                        }

                        entered.Reset(); release.Reset();
                        pending = context.CreateManualBackupAsync(); PumpUntil(() => entered.IsSet);
                        var retryForm = (BackupCreationForm)Field(context, "manualBackupForm");
                        int successResults = 0, successThread = 0;
                        retryForm.ResultApplied += delegate { successResults++; successThread = Thread.CurrentThread.ManagedThreadId; };
                        Check(Volatile.Read(ref copies) == 3 && context.IsManualBackupRunning,
                            "explicit retry starts one fresh real copy after cancelled and failed worker settlement");
                        release.Set(); PumpUntil(() => pending.IsCompleted && !context.IsManualBackupRunning);
                        var created = Directory.GetDirectories(BackupService.BackupsRoot).Except(before).ToArray();
                        Check(!pending.IsFaulted && !pending.IsCanceled && created.Length == 1,
                            "retry produces exactly one completed manual backup through the actual writer");
                        BackupIntegrity.Validate(created[0]);
                        Check(successResults == 1 && successThread == ownerThread && !retryForm.IsBusy && retryForm.Visible &&
                            new[] { "ProGo.exe", "VERSION", "settings.json", "vault.enc.json" }.All(n => ManualBackupHash(Path.Combine(created[0], n)) == sourceHashes[n]),
                            "successful real indexed backup reaches its owner UI once with original opaque personal payloads");
                        Check(ManualBackupTreeMatches(previous, previousHashes), "successful manual retry retains the previous backup unchanged");
                        Shot(retryForm, "manual-backup-complete");
                        retryForm.Close(); pending = null;

                        before = Directory.GetDirectories(BackupService.BackupsRoot);
                        entered.Reset(); release.Reset(); delayCancellation = true;
                        pending = context.CreateManualBackupAsync(); PumpUntil(() => entered.IsSet);
                        var shutdownForm = (BackupCreationForm)Field(context, "manualBackupForm");
                        int shutdownResults = 0; shutdownForm.ResultApplied += delegate { shutdownResults++; };
                        var shutdown = context.RequestShutdownAsync();
                        PumpUntil(() => cancellationObserved.IsSet);
                        int beforeShutdownPulses = pulses; PumpUntil(() => pulses >= beforeShutdownPulses + 3);
                        Check(captured.IsCancellationRequested && !shutdown.IsCompleted && !pending.IsCompleted,
                            "normal shutdown cancels a manual backup and keeps native UI ticking while cancellation settles");
                        release.Set(); PumpUntil(() => shutdown.IsCompleted && pending.IsCompleted && !context.IsManualBackupRunning);
                        Check(shutdown.Result && shutdownResults == 1 && !shutdownForm.IsBusy &&
                            Directory.GetDirectories(BackupService.BackupsRoot).OrderBy(p => p).SequenceEqual(before.OrderBy(p => p)),
                            "successful normal shutdown waits for real cancelled-copy cleanup before accepting shutdown");
                        context.CancelShutdown(); PumpUntil(() => commands[AppCommand.Settings].Enabled);
                        Check(commands[AppCommand.CreateBackup].Enabled && commands[AppCommand.StopDesktop].Enabled,
                            "cancelled shutdown handoff restores maintenance and connection controls after copy settlement");
                        shutdownForm.Close(); pending = null; pulse.Stop();
                    }

                    entered.Reset(); release.Reset();
                    before = Directory.GetDirectories(BackupService.BackupsRoot);
                    CancellationToken disposalToken = default(CancellationToken);
                    // Deliberately model an individual disk operation that has not yet
                    // returned to a cancellation boundary; Dispose must not wait for it.
                    Action<string, CancellationToken> slowDisk = delegate(string path, CancellationToken token) {
                        if (!String.Equals(Path.GetFileName(path), "ProGo.exe", StringComparison.OrdinalIgnoreCase)) return;
                        disposalToken = token; entered.Set(); release.Wait();
                    };
                    using (var context = new UpdateAwareTrayApplicationContext(settings, proxy, bridge, home, clipboard, false, monitor,
                        () => new WindowsProxyRestoreResult(), null, slowDisk, 50))
                    {
                        pending = context.CreateManualBackupAsync(); PumpUntil(() => entered.IsSet);
                        var timedForm = (BackupCreationForm)Field(context, "manualBackupForm"); int timedResults = 0;
                        timedForm.ResultApplied += delegate { timedResults++; };
                        using (var pulse = new System.Windows.Forms.Timer { Interval = 10 }) {
                            int pulses = 0; pulse.Tick += delegate { pulses++; }; pulse.Start();
                            var shutdown = context.RequestShutdownAsync(); PumpUntil(() => shutdown.IsCompleted);
                            Check(!shutdown.Result && pulses > 0 && disposalToken.IsCancellationRequested && context.IsManualBackupRunning && !pending.IsCompleted,
                                "normal shutdown refuses within its deadline while uncancellable current I/O remains active and native UI ticks");
                            var commands = MenuItems(((NotifyIcon)Field(context, "tray")).ContextMenuStrip.Items).Where(i => i.Tag is AppCommand).ToDictionary(i => (AppCommand)i.Tag);
                            Check(commands[AppCommand.Settings].Enabled && commands[AppCommand.StopDesktop].Enabled && !commands[AppCommand.CreateBackup].Enabled && timedForm.Visible,
                                "refused shutdown leaves settings and stop usable without starting conflicting manual copy work");
                            release.Set(); PumpUntil(() => pending.IsCompleted && !context.IsManualBackupRunning);
                            Check(timedResults == 1 && !timedForm.IsBusy &&
                                Directory.GetDirectories(BackupService.BackupsRoot).OrderBy(p => p).SequenceEqual(before.OrderBy(p => p)),
                                "worker released after refused shutdown settles as cancelled and removes only its own partial directory");
                            pulse.Stop();
                        }
                        entered.Reset(); release.Reset();
                        pending = context.CreateManualBackupAsync(); PumpUntil(() => entered.IsSet);
                        var form = (BackupCreationForm)Field(context, "manualBackupForm"); int lateResults = 0;
                        form.ResultApplied += delegate { lateResults++; };
                        var watch = Stopwatch.StartNew(); context.Dispose(); watch.Stop();
                        Check(watch.ElapsedMilliseconds < 500 && disposalToken.IsCancellationRequested && !pending.IsCompleted,
                            "native context disposal cancels but does not wait for an individual blocked copy operation");
                        Check(form.IsDisposed && lateResults == 0, "native context disposal closes manual backup UI without applying a fabricated completion");
                        release.Set(); PumpUntil(() => pending.IsCompleted); Application.DoEvents();
                        Check(!pending.IsFaulted && lateResults == 0 && form.IsDisposed &&
                            Directory.GetDirectories(BackupService.BackupsRoot).OrderBy(p => p).SequenceEqual(before.OrderBy(p => p)),
                            "late cancelled disk worker settles and removes its own partial copy without resurrecting disposed UI");
                        Check(sourceHashes.All(p => ManualBackupHash(Path.Combine(AppPaths.Root, p.Key)) == p.Value) && ManualBackupTreeMatches(previous, previousHashes),
                            "late disposal cleanup preserves installed personal data and existing backup hashes");
                        pending = null;
                    }
                }
            }
            finally
            {
                release.Set();
                if (pending != null && !pending.IsCompleted) PumpUntil(() => pending.IsCompleted);
                if (ownsBackups && Directory.Exists(BackupService.BackupsRoot)) Directory.Delete(BackupService.BackupsRoot, true);
                if (movedBackups) Directory.Move(previousBackups, BackupService.BackupsRoot);
                foreach (var pair in originals) {
                    var path = Path.Combine(AppPaths.Root, pair.Key);
                    if (pair.Value != null) File.WriteAllBytes(path, pair.Value);
                    else if (File.Exists(path)) File.Delete(path);
                }
                if (!hadScripts && Directory.Exists(Path.Combine(AppPaths.Root, "scripts"))) Directory.Delete(Path.Combine(AppPaths.Root, "scripts"));
            }
        }

        private static string ManualBackupHash(string path)
        {
            using (var algorithm = SHA256.Create())
            using (var stream = File.OpenRead(path)) return Convert.ToBase64String(algorithm.ComputeHash(stream));
        }

        private static Dictionary<string, string> ManualBackupHashes(string root)
        { return Directory.GetFiles(root, "*", SearchOption.AllDirectories).ToDictionary(p => p.Substring(root.Length + 1), ManualBackupHash, StringComparer.OrdinalIgnoreCase); }

        private static bool ManualBackupTreeMatches(string root, Dictionary<string, string> expected)
        {
            var actual = ManualBackupHashes(root);
            return actual.Count == expected.Count && expected.All(p => actual.ContainsKey(p.Key) && actual[p.Key] == p.Value);
        }
    }
}

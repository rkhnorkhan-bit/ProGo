using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        private static void AsyncRestoreWorkflow()
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") {
                Console.WriteLine("SKIP: asynchronous restore ownership fixtures require isolated Windows CI"); return;
            }
            var root = Path.Combine(work, "async-restore-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try {
                AsyncRestoreReadAndCancel(root);
                AsyncRestoreRealReadFailures(root);
                AsyncRestoreOwnedCopyDisposal(root);
                AsyncRestorePreparedEvidence(root);
                AsyncRestoreCachedFormAndCancellation(root);
                AsyncRestoreDisposedForm(root);
                AsyncRestoreContextActor(root);
                AsyncRestoreContextShutdown(root);
                AsyncRestoreContextDisposedOwner(root);
                AsyncRestoreContextConfirmation(root);
                AsyncRestoreContextLaunchReservation(root);
                AsyncRestoreContextPendingHelperShutdown(root);
                AsyncRestoreContextDelayedOwnerCompletion(root);
                AsyncRestoreOwnedHandoff(root, "cancel");
                AsyncRestoreOwnedHandoff(root, "timeout");
                AsyncRestoreOwnedHandoff(root, "accept");
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }

        private static string AsyncRestoreSource(string root, string name)
        {
            var source = Path.Combine(root, name); Directory.CreateDirectory(Path.Combine(source, "scripts"));
            File.WriteAllText(Path.Combine(source, "ProGo.exe"), "opaque synthetic fixture; never execute");
            File.WriteAllText(Path.Combine(source, "VERSION"), "0.0.1");
            File.WriteAllText(Path.Combine(source, "manifest.txt"), "product=ProGo\nversion=0.0.1\nbackup_kind=manual\ncreated_by=manual\n");
            File.WriteAllText(Path.Combine(source, "settings.json"), "{\"SocksPort\":12345}");
            File.WriteAllText(Path.Combine(source, "vault.enc.json"), "opaque encrypted synthetic vault; never log");
            foreach (var script in new[] { "Start-ProGo.ps1", "Restore-ProGoBackup.ps1", "Update-ProGo.Core.ps1" })
                File.WriteAllText(Path.Combine(source, "scripts", script), "# opaque fixture; never execute");
            File.WriteAllBytes(Path.Combine(source, "progo.log"), new byte[1024 * 1024]);
            BackupIntegrity.Write(source); return source;
        }

        private static Dictionary<string, string> AsyncRestoreHashes(string root)
        {
            return Directory.GetFiles(root, "*", SearchOption.AllDirectories).ToDictionary(
                p => p.Substring(root.Length + 1), p => {
                    using (var stream = File.OpenRead(p)) using (var hash = SHA256.Create())
                        return Convert.ToBase64String(hash.ComputeHash(stream));
                });
        }

        private static bool AsyncRestoreTreeMatches(string root, Dictionary<string, string> expected)
        {
            if (!Directory.Exists(root)) return false;
            var actual = AsyncRestoreHashes(root);
            return expected.Count == actual.Count && expected.All(p => actual.ContainsKey(p.Key) && actual[p.Key] == p.Value);
        }

        private static void PumpAsyncRestore(Func<bool> ready, int milliseconds = 5000)
        {
            var watch = Stopwatch.StartNew();
            while (!ready()) {
                if (watch.ElapsedMilliseconds > milliseconds) throw new Exception("asynchronous restore fixture deadline");
                Application.DoEvents(); Thread.Sleep(10);
            }
            Application.DoEvents();
        }

        private static void WaitAsyncRestoreWithoutPump(Task pending, int milliseconds = 5000)
        {
            var watch = Stopwatch.StartNew();
            while (!pending.IsCompleted) {
                if (watch.ElapsedMilliseconds > milliseconds) throw new Exception("ownerless restore settlement deadline");
                Thread.Sleep(10);
            }
        }

        private static bool AsyncRestoreObservedCancellation(Task pending)
        {
            return pending.IsCanceled || (pending.IsFaulted && pending.Exception.Flatten().InnerExceptions.Any(e => e is OperationCanceledException));
        }

        private static string[] AsyncRestoreOwnedPaths(BackupWorkSession session)
        {
            var copies = (List<PreparedBackup>)typeof(BackupWorkSession).GetField("owned", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
            // Called only while the real worker is held immediately after registration.
            return copies.Select(c => c.Path).ToArray();
        }

        private static void AsyncRestoreReadAndCancel(string root)
        {
            var source = AsyncRestoreSource(root, "cancel-real-hash"); var before = AsyncRestoreHashes(source);
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var session = new BackupWorkSession(delegate(string phase, CancellationToken token) {
                if (phase != "restore-hash:ProGo.exe") return;
                entered.Set(); release.Wait(token);
            }))
            using (var pulse = new System.Windows.Forms.Timer { Interval = 10 })
            {
                int ticks = 0; pulse.Tick += delegate { ticks++; }; pulse.Start();
                var pending = session.Run("restore-prepare", token => session.Prepare(source, "Program", false, token));
                try {
                    PumpAsyncRestore(() => entered.IsSet && ticks >= 3);
                    Check(!pending.IsCompleted && Object.ReferenceEquals(session.CurrentWork, pending),
                        "real restore hash validation belongs to one tracked worker while native UI heartbeat continues");
                    bool duplicateRejected = false;
                    try { session.Run("duplicate-restore", token => session.Prepare(source, "Program", false, token)); }
                    catch (InvalidOperationException) { duplicateRejected = true; }
                    Check(duplicateRejected && AsyncRestoreOwnedPaths(session).Length == 0,
                        "a duplicate restore phase cannot queue another digest worker or create an untracked copy");
                    session.Cancel(); PumpAsyncRestore(() => pending.IsCompleted);
                    var finish = session.FinishAsync(); PumpAsyncRestore(() => finish.IsCompleted);
                    Check(AsyncRestoreObservedCancellation(pending) && !finish.IsFaulted && AsyncRestoreTreeMatches(source, before),
                        "cancellation reaches actual hash validation before copying and preserves every selected backup byte");
                }
                finally { release.Set(); session.Cancel(); WaitAsyncRestoreWithoutPump(session.FinishAsync()); }
            }
        }

        private static void AsyncRestoreRealReadFailures(string root)
        {
            var source = AsyncRestoreSource(root, "read-failure"); var before = AsyncRestoreHashes(source);
            using (var session = new BackupWorkSession())
            using (var denied = new FileStream(Path.Combine(source, "ProGo.exe"), FileMode.Open, FileAccess.Read, FileShare.None))
            {
                var pending = session.Run("restore-prepare", token => session.Prepare(source, "Program", false, token));
                PumpAsyncRestore(() => pending.IsCompleted);
                Check(pending.IsFaulted && pending.Exception.Flatten().InnerExceptions.Any(e => e is IOException) && AsyncRestoreOwnedPaths(session).Length == 0,
                    "an actual exclusive Windows file lock rejects restore validation before any disposable snapshot is created");
                PumpAsyncRestore(() => session.FinishAsync().IsCompleted);
            }
            Check(AsyncRestoreTreeMatches(source, before), "denied restore input read preserves all indexed source evidence");
            File.AppendAllText(Path.Combine(source, "VERSION"), "damaged-after-index");
            var damaged = AsyncRestoreHashes(source);
            using (var session = new BackupWorkSession()) {
                var pending = session.Run("restore-prepare", token => session.Prepare(source, "All", true, token));
                PumpAsyncRestore(() => pending.IsCompleted);
                Check(pending.IsFaulted && pending.Exception.Flatten().InnerExceptions.Any(e => e is InvalidDataException && e.Message.Contains("повреждён")) &&
                    AsyncRestoreOwnedPaths(session).Length == 0 && AsyncRestoreTreeMatches(source, damaged),
                    "a real digest mismatch reports a Russian cause without rewriting the damaged copy or blessing its live bytes");
                PumpAsyncRestore(() => session.FinishAsync().IsCompleted);
            }
        }

        private static void AsyncRestoreOwnedCopyDisposal(string root)
        {
            var source = AsyncRestoreSource(root, "owner-loss"); var before = AsyncRestoreHashes(source);
            var unrelated = Path.Combine(Path.GetTempPath(), "ProGo-restore-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(unrelated); File.WriteAllText(Path.Combine(unrelated, "unrelated.txt"), "must survive exact ownership cleanup");
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            {
                int ownerThread = Thread.CurrentThread.ManagedThreadId, workerThread = 0, cleanupThread = 0, cleanups = 0;
                CancellationToken observed = default(CancellationToken);
                var session = new BackupWorkSession(delegate(string phase, CancellationToken token) {
                    if (phase == "restore-copy:ProGo.exe") {
                        observed = token; workerThread = Thread.CurrentThread.ManagedThreadId; entered.Set();
                        // Model one individual disk operation which has not reached its next cancellation boundary.
                        release.Wait();
                    }
                    if (phase == "restore-cleanup") { cleanupThread = Thread.CurrentThread.ManagedThreadId; Interlocked.Increment(ref cleanups); }
                });
                Task pending = null;
                try {
                    pending = session.Run("restore-prepare", token => session.Prepare(source, "All", true, token));
                    PumpAsyncRestore(() => entered.IsSet);
                    var owned = AsyncRestoreOwnedPaths(session);
                    Check(owned.Length == 1 && Directory.Exists(owned[0]) && workerThread != ownerThread,
                        "real preparation registers its exact disposable path before the first copied file and executes off the UI owner");
                    var watch = Stopwatch.StartNew(); session.Dispose(); watch.Stop();
                    var finish = session.FinishAsync();
                    Check(watch.ElapsedMilliseconds < 500 && observed.IsCancellationRequested && !pending.IsCompleted && !finish.IsCompleted && Directory.Exists(owned[0]),
                        "owner disposal requests cancellation promptly and retains the still-used disposable copy until actual worker settlement");
                    release.Set(); WaitAsyncRestoreWithoutPump(finish);
                    Check(pending.IsCanceled && !finish.IsFaulted && cleanups == 1 && cleanupThread != ownerThread && !Directory.Exists(owned[0]) &&
                        File.ReadAllText(Path.Combine(unrelated, "unrelated.txt")) == "must survive exact ownership cleanup" && AsyncRestoreTreeMatches(source, before),
                        "late cancelled preparation settles without a UI pump and removes only its exact owned path on a worker");
                }
                finally {
                    release.Set(); session.Cancel(); WaitAsyncRestoreWithoutPump(session.FinishAsync());
                    if (Directory.Exists(unrelated)) Directory.Delete(unrelated, true);
                }
            }
        }

        private static void AsyncRestorePreparedEvidence(string root)
        {
            var source = AsyncRestoreSource(root, "prepared-evidence"); var before = AsyncRestoreHashes(source);
            string owned = null;
            using (var session = new BackupWorkSession()) {
                var preview = session.Run("restore-preview", token => RestorePreview.Read(source, token));
                PumpAsyncRestore(() => preview.IsCompleted); var choices = preview.GetAwaiter().GetResult();
                Check(choices.HasData && choices.Program.SequenceEqual(new[] { "ProGo.exe", "VERSION", "scripts" }) &&
                    choices.Data.SequenceEqual(new[] { "settings.json", "vault.enc.json" }) && choices.All.SequenceEqual(choices.Program.Concat(choices.Data)),
                    "worker-produced restore preview carries the exact safe program and separately authorized data selections");
                var pending = session.Run("restore-prepare", token => session.Prepare(source, "All", true, token));
                PumpAsyncRestore(() => pending.IsCompleted); var result = pending.GetAwaiter().GetResult(); owned = result.Copy.Path;
                BackupIntegrity.Validate(owned);
                Check(result.Version == "0.0.1" && result.Scope == "All" && result.ConfirmData && result.Names.SequenceEqual(choices.All) &&
                    AsyncRestoreTreeMatches(source, before) && AsyncRestoreTreeMatches(owned, before),
                    "real prepared DTO contains verified version, explicit consent and file names without mutating selected source evidence");
                File.WriteAllText(Path.Combine(source, "settings.json"), "source changed after preparation");
                File.WriteAllText(Path.Combine(source, "VERSION"), "9.9.9");
                BackupIntegrity.Validate(owned);
                Check(File.ReadAllText(Path.Combine(owned, "settings.json")) == "{\"SocksPort\":12345}" &&
                    File.ReadAllText(Path.Combine(owned, "VERSION")) == "0.0.1" && result.Version == "0.0.1",
                    "confirmation evidence stays bound to the independent verified copy after original source mutation");
                var finish = session.FinishAsync(); PumpAsyncRestore(() => finish.IsCompleted);
                Check(!Directory.Exists(owned) && String.IsNullOrEmpty(session.CleanupError) && Directory.Exists(source),
                    "settled restore session deletes its own prepared copy while retaining the original backup folder");
            }
        }

        private static void AsyncRestoreCachedFormAndCancellation(string root)
        {
            var source = AsyncRestoreSource(root, "cached-form"); var before = AsyncRestoreHashes(source);
            using (var previewEntered = new ManualResetEventSlim())
            using (var previewRelease = new ManualResetEventSlim())
            using (var hashEntered = new ManualResetEventSlim())
            using (var hashRelease = new ManualResetEventSlim())
            {
                int previewReads = 0, hashReads = 0, ownerThread = Thread.CurrentThread.ManagedThreadId;
                bool ioOnOwner = false; CancellationToken hashToken = default(CancellationToken);
                using (var session = new BackupWorkSession(delegate(string phase, CancellationToken token) {
                    if (Thread.CurrentThread.ManagedThreadId == ownerThread) ioOnOwner = true;
                    if (phase == "restore-preview") { Interlocked.Increment(ref previewReads); previewEntered.Set(); previewRelease.Wait(token); }
                    if (phase == "restore-hash:ProGo.exe") { Interlocked.Increment(ref hashReads); hashToken = token; hashEntered.Set(); hashRelease.Wait(); }
                }))
                using (var form = new RestoreOptionsForm(source, null, session))
                using (var pulse = new System.Windows.Forms.Timer { Interval = 10 })
                {
                    int ticks = 0; pulse.Tick += delegate { ticks++; }; pulse.Start();
                    try {
                        form.Show(); PumpAsyncRestore(() => previewEntered.IsSet && ticks >= 3);
                        var prepare = (Button)Field(form, "prepare"); var consent = (CheckBox)Field(form, "consent");
                        var data = (RadioButton)Field(form, "data"); var all = (RadioButton)Field(form, "all");
                        var program = (RadioButton)Field(form, "program"); var contents = (TextBox)Field(form, "contents");
                        Check(!form.PreviewReady && !prepare.Enabled && !data.Enabled && form.PreviewWork != null && !form.PreviewWork.IsCompleted,
                            "native restore options show cancellable background preview without blocking the owner or granting data consent");
                        previewRelease.Set(); PumpAsyncRestore(() => form.PreviewReady && prepare.Enabled);
                        var moved = source + "-temporarily-unavailable"; Directory.Move(source, moved);
                        try {
                            data.Checked = true;
                            Check(consent.Enabled && !consent.Checked && !prepare.Enabled && contents.Lines.SequenceEqual(new[] { "settings.json", "vault.enc.json" }),
                                "scope selection uses cached worker evidence even while the original backup path is unavailable");
                            consent.Checked = true; all.Checked = true;
                            Check(!consent.Checked && !prepare.Enabled && contents.Text.Contains("ProGo.exe") && contents.Text.Contains("vault.enc.json"),
                                "cached all-scope preview resets earlier separate data consent without another filesystem read");
                            program.Checked = true;
                        }
                        finally { Directory.Move(moved, source); }
                        Check(previewReads == 1 && !ioOnOwner && !consent.Enabled && !form.DataConfirmed,
                            "native restore scope changes neither repeat preview I/O nor carry data consent into safe program scope");
                        prepare.PerformClick(); PumpAsyncRestore(() => hashEntered.IsSet);
                        prepare.PerformClick(); int after = ticks; form.Close(); PumpAsyncRestore(() => ticks >= after + 3);
                        Check(hashToken.IsCancellationRequested && form.Visible && form.DialogResult != DialogResult.OK &&
                            (bool)Field(form, "busy") && !form.PrepareWork.IsCompleted && hashReads == 1,
                            "pending restore X requests cancellation and retains its native owner until the real hash operation settles");
                        hashRelease.Set(); PumpAsyncRestore(() => form.PrepareWork.IsCompleted && !(bool)Field(form, "busy"));
                        var finish = session.FinishAsync(); PumpAsyncRestore(() => finish.IsCompleted);
                        Check(form.PrepareWork.IsCanceled && form.DialogResult == DialogResult.Cancel && !ioOnOwner && AsyncRestoreTreeMatches(source, before),
                            "cancelled native preparation cannot publish accepted restoration and preserves the indexed source bytes");
                        Shot(form, "async-restore-cancelled");
                    }
                    finally {
                        previewRelease.Set(); hashRelease.Set(); session.Cancel(); WaitAsyncRestoreWithoutPump(session.FinishAsync());
                    }
                }
            }
        }

        private static void AsyncRestoreDisposedForm(string root)
        {
            var source = AsyncRestoreSource(root, "disposed-form"); var before = AsyncRestoreHashes(source);
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var session = new BackupWorkSession(delegate(string phase, CancellationToken token) {
                if (phase == "restore-copy:ProGo.exe") { entered.Set(); release.Wait(); }
            }))
            {
                var preview = RestorePreview.Read(source, CancellationToken.None);
                var form = new RestoreOptionsForm(source, preview, session);
                try {
                    form.Show(); Application.DoEvents(); ((Button)Field(form, "prepare")).PerformClick();
                    PumpAsyncRestore(() => entered.IsSet); var worker = form.PrepareWork; var owned = AsyncRestoreOwnedPaths(session);
                    int lateUi = 0; form.TextChanged += delegate { lateUi++; };
                    ((Label)Field(form, "status")).TextChanged += delegate { lateUi++; };
                    var watch = Stopwatch.StartNew(); form.Dispose(); session.Dispose(); watch.Stop();
                    var finish = session.FinishAsync();
                    Check(watch.ElapsedMilliseconds < 500 && form.IsDisposed && !worker.IsCompleted && Directory.Exists(owned.Single()),
                        "forced restore form and actor disposal return promptly while a real copy read still owns its exact temporary folder");
                    release.Set(); WaitAsyncRestoreWithoutPump(finish);
                    Check(worker.IsCanceled && lateUi == 0 && form.DialogResult != DialogResult.OK && !Directory.Exists(owned.Single()) && AsyncRestoreTreeMatches(source, before),
                        "ownerless cancelled copy settles and cleans on workers without resurrecting its disposed native form or accepted result");
                    Application.DoEvents();
                    Check(lateUi == 0 && form.IsDisposed, "late queued restore completion cannot mutate the disposed owner after dispatch resumes");
                }
                finally { release.Set(); session.Cancel(); WaitAsyncRestoreWithoutPump(session.FinishAsync()); form.Dispose(); }
            }
        }

        private sealed class AsyncRestoreContextFixture : IDisposable
        {
            private readonly string previous;
            private readonly bool moved;
            internal readonly string Source;
            internal readonly Dictionary<string, string> SourceHashes;
            internal readonly SettingsService Settings;
            internal readonly ProxyService Proxy;
            internal readonly CliProxyBridgeService Bridge;
            internal readonly Ikev2RelayService Relay;
            internal readonly HomeVpnService Home;
            internal readonly ClipboardService Clipboard;
            internal readonly ConnectionHealthMonitor Monitor;
            internal readonly UpdateAwareTrayApplicationContext Context;
            internal readonly Dictionary<AppCommand, ToolStripItem> Commands;
            internal int CliCleanup, WindowsCleanup;
            private readonly Dictionary<string, byte[]> installed;

            internal AsyncRestoreContextFixture(string root, int shutdownTimeout = 3000)
            {
                if (File.Exists(CliProxyEnvironmentService.BackupPath) || File.Exists(SystemProxyService.BackupPath))
                    throw new Exception("async restore fixture must not own unrelated native proxy journals");
                previous = Path.Combine(AppPaths.Root, "async-restore-originals-" + Guid.NewGuid().ToString("N"));
                if (Directory.Exists(BackupService.BackupsRoot)) { Directory.Move(BackupService.BackupsRoot, previous); moved = true; }
                Source = AsyncRestoreSource(BackupService.BackupsRoot, "backup-20260101-fixture-manual"); SourceHashes = AsyncRestoreHashes(Source);
                Settings = new SettingsService();
                Proxy = new ProxyService(() => Settings.Current, s => Settings.Save(s), "unused-fixture-ssh", () => DateTime.UtcNow, false);
                Bridge = new CliProxyBridgeService(Settings); Relay = new Ikev2RelayService(); Home = new HomeVpnService(Relay);
                Clipboard = new ClipboardService(Settings); Monitor = new ConnectionHealthMonitor(() => Settings.Current);
                Context = new UpdateAwareTrayApplicationContext(Settings, Proxy, Bridge, Home, Clipboard, false, Monitor,
                    () => { Interlocked.Increment(ref WindowsCleanup); return new WindowsProxyRestoreResult(); }, null, null, shutdownTimeout,
                    null, () => Interlocked.Increment(ref CliCleanup));
                var tray = (NotifyIcon)Field(Context, "tray");
                Commands = MenuItems(tray.ContextMenuStrip.Items).Where(i => i.Tag is AppCommand).ToDictionary(i => (AppCommand)i.Tag, i => (ToolStripItem)i);
                installed = new[] { AppPaths.SettingsPath, Path.Combine(AppPaths.Root, "vault.enc.json"), Path.Combine(AppPaths.Root, "VERSION") }
                    .ToDictionary(p => p, p => File.Exists(p) ? File.ReadAllBytes(p) : null);
            }

            internal bool InstalledUnchanged()
            {
                return installed.All(p => p.Value == null ? !File.Exists(p.Key) : File.Exists(p.Key) && File.ReadAllBytes(p.Key).SequenceEqual(p.Value));
            }
            public void Dispose()
            {
                Context.Dispose();
                var restore = Context.RestoreWork;
                if (restore != null) WaitAsyncRestoreWithoutPump(restore, 25000);
                Monitor.Dispose(); Clipboard.Dispose(); Home.Dispose(); Relay.Dispose(); Bridge.Dispose(); Proxy.Dispose(); Settings.Dispose();
                if (Directory.Exists(BackupService.BackupsRoot)) Directory.Delete(BackupService.BackupsRoot, true);
                if (moved) Directory.Move(previous, BackupService.BackupsRoot);
            }
        }

        private static void AsyncRestoreContextActor(string root)
        {
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var fixture = new AsyncRestoreContextFixture(root))
            using (var pulse = new System.Windows.Forms.Timer { Interval = 10 })
            {
                int reads = 0, selections = 0, ticks = 0; CancellationToken observed = default(CancellationToken);
                fixture.Context.BackupIoProbe = delegate(string phase, CancellationToken token) {
                    if (phase == "restore-list") { observed = token; Interlocked.Increment(ref reads); entered.Set(); release.Wait(); }
                };
                fixture.Context.RestorePick = delegate { selections++; return fixture.Source; };
                pulse.Tick += delegate { ticks++; }; pulse.Start();
                fixture.Commands[AppCommand.RestoreBackup].PerformClick(); var pending = fixture.Context.RestoreWork;
                try {
                    PumpAsyncRestore(() => entered.IsSet && ticks >= 3);
                    var form = (BackupWorkProgressForm)Field(fixture.Context, "backupActivityForm");
                    Check(fixture.Context.IsBackupRunning && form.Visible && form.IsBusy && !pending.IsCompleted && selections == 0,
                        "actual tray restore command owns its background list reader while native progress and heartbeat remain active");
                    Check(Object.ReferenceEquals(pending, fixture.Context.StartRestoreAsync()) && Object.ReferenceEquals(pending, fixture.Context.CreateManualBackupAsync()) &&
                        Object.ReferenceEquals(pending, fixture.Context.ManualBackupCompletion) && reads == 1,
                        "duplicate restore and manual-copy requests share the real existing actor without queuing another backup mutation");
                    Check(new[] { AppCommand.CreateBackup, AppCommand.RestoreBackup, AppCommand.CleanupBackups, AppCommand.Update }.All(c => !fixture.Commands[c].Enabled) &&
                        new[] { AppCommand.Settings, AppCommand.StopDesktop, AppCommand.StopAll }.All(c => fixture.Commands[c].Enabled),
                        "real tray disables competing maintenance while preserving settings and connection stop during restore scanning");
                    CloseServiceDialog(fixture.Commands[AppCommand.Settings], typeof(SshProfilesSettingsForm), delegate {
                        Check(fixture.Context.IsBackupRunning && !pending.IsCompleted, "real settings dialog opens during a blocked restore list read");
                    });
                    fixture.Commands[AppCommand.StopDesktop].PerformClick(); WaitIntegration(fixture.Context);
                    Check(fixture.CliCleanup == 1 && fixture.WindowsCleanup == 1 && fixture.Context.IsBackupRunning && !pending.IsCompleted,
                        "actual tray desktop stop settles its independent cleanup while restore input scanning remains blocked");
                    ((Button)form.CancelButton).PerformClick();
                    Check(observed.IsCancellationRequested && form.IsBusy && !pending.IsCompleted,
                        "real restore progress Cancel requests cancellation without releasing a still-running backup reader");
                    release.Set(); PumpAsyncRestore(() => pending.IsCompleted && !fixture.Context.IsBackupRunning);
                    Check(!pending.IsFaulted && selections == 0 && !form.IsBusy && fixture.Context.BackupStatus.Contains("отменено") &&
                        AsyncRestoreTreeMatches(fixture.Source, fixture.SourceHashes) && fixture.InstalledUnchanged(),
                        "cancelled actual restore actor settles before selection and preserves installed data plus every previous indexed copy");
                    Check(new[] { AppCommand.CreateBackup, AppCommand.RestoreBackup, AppCommand.CleanupBackups, AppCommand.Update }.All(c => fixture.Commands[c].Enabled),
                        "settled restore cancellation releases the real maintenance command guard for an explicit retry");
                    Shot(form, "async-restore-list-cancelled");
                }
                finally { release.Set(); if (pending != null) { fixture.Context.Dispose(); WaitAsyncRestoreWithoutPump(pending); } }
            }
        }

        private static void AsyncRestoreContextShutdown(string root)
        {
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var fixture = new AsyncRestoreContextFixture(root))
            using (var pulse = new System.Windows.Forms.Timer { Interval = 10 })
            {
                int ticks = 0; CancellationToken observed = default(CancellationToken);
                fixture.Context.BackupIoProbe = delegate(string phase, CancellationToken token) {
                    if (phase == "restore-list") { observed = token; entered.Set(); release.Wait(); }
                };
                pulse.Tick += delegate { ticks++; }; pulse.Start();
                var pending = fixture.Context.StartRestoreAsync();
                try {
                    PumpAsyncRestore(() => entered.IsSet);
                    var watch = Stopwatch.StartNew();
                    var shutdown = fixture.Context.RequestShutdownAsync(); var duplicate = fixture.Context.RequestShutdownAsync();
                    PumpAsyncRestore(() => shutdown.IsCompleted && ticks >= 3, 10000); watch.Stop();
                    Check(Object.ReferenceEquals(shutdown, duplicate) && !shutdown.Result && observed.IsCancellationRequested && !pending.IsCompleted &&
                        fixture.Context.IsBackupRunning && fixture.CliCleanup == 0 && fixture.WindowsCleanup == 0,
                        "blocked restore reader shares duplicate shutdown, refuses within its deadline and prevents premature native cleanup");
                    Check(watch.ElapsedMilliseconds >= 2800 && watch.ElapsedMilliseconds < 6000 && ticks >= 20,
                        "actual production three-second shutdown refusal keeps the native UI ticking while its restore reader remains owned");
                    Check(fixture.Commands[AppCommand.Settings].Enabled && fixture.Commands[AppCommand.StopDesktop].Enabled && !fixture.Commands[AppCommand.CreateBackup].Enabled,
                        "refused restore shutdown preserves native settings and stop availability while its shared actor remains guarded");
                    release.Set(); PumpAsyncRestore(() => pending.IsCompleted && !fixture.Context.IsBackupRunning);
                    var retry = fixture.Context.RequestShutdownAsync(); PumpAsyncRestore(() => retry.IsCompleted);
                    Check(retry.Result && fixture.CliCleanup == 1 && fixture.WindowsCleanup == 1 && fixture.InstalledUnchanged() &&
                        AsyncRestoreTreeMatches(fixture.Source, fixture.SourceHashes),
                        "explicit shutdown retry performs exactly one native cleanup only after cancelled restore I/O and temporary cleanup settle");
                    fixture.Context.CancelShutdown(); PumpAsyncRestore(() => fixture.Commands[AppCommand.CreateBackup].Enabled);
                }
                finally { release.Set(); fixture.Context.Dispose(); WaitAsyncRestoreWithoutPump(pending); }
            }
        }

        private static void AsyncRestoreContextDisposedOwner(string root)
        {
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var fixture = new AsyncRestoreContextFixture(root))
            {
                int ownerThread = Thread.CurrentThread.ManagedThreadId, choices = 0, confirmations = 0;
                CancellationToken observed = default(CancellationToken);
                fixture.Context.RestorePick = delegate { Check(Thread.CurrentThread.ManagedThreadId == ownerThread, "restore picker callback belongs to its native owner"); return fixture.Source; };
                fixture.Context.RestoreChoose = delegate { choices++; return new RestoreChoice { Scope = "All", ConfirmData = true }; };
                fixture.Context.RestoreConfirm = delegate { confirmations++; return false; };
                fixture.Context.BackupIoProbe = delegate(string phase, CancellationToken token) {
                    if (phase == "restore-copy:ProGo.exe") { observed = token; entered.Set(); release.Wait(); }
                };
                var pending = fixture.Context.StartRestoreAsync();
                try {
                    PumpAsyncRestore(() => entered.IsSet);
                    var session = (BackupWorkSession)Field(fixture.Context, "backupActivity"); var owned = AsyncRestoreOwnedPaths(session);
                    var progress = (BackupWorkProgressForm)Field(fixture.Context, "backupActivityForm");
                    int lateUi = 0; ((Label)Field(progress, "status")).TextChanged += delegate { lateUi++; };
                    var watch = Stopwatch.StartNew(); fixture.Context.Dispose(); watch.Stop();
                    Check(watch.ElapsedMilliseconds < 500 && observed.IsCancellationRequested && !pending.IsCompleted && progress.IsDisposed && Directory.Exists(owned.Single()),
                        "forced native restore context disposal promptly cancels but retains the temporary stage still used by its real copy worker");
                    release.Set(); WaitAsyncRestoreWithoutPump(pending);
                    Check(!pending.IsFaulted && choices == 1 && confirmations == 0 && lateUi == 0 && !Directory.Exists(owned.Single()) &&
                        fixture.CliCleanup == 0 && fixture.WindowsCleanup == 0 && fixture.InstalledUnchanged() && AsyncRestoreTreeMatches(fixture.Source, fixture.SourceHashes),
                        "ownerless actual restore workflow settles without dispatch, helper launch, native mutation or late UI and removes only its stage");
                    Application.DoEvents(); Check(lateUi == 0 && progress.IsDisposed, "resumed dispatch cannot revive the disposed restore owner");
                }
                finally { release.Set(); fixture.Context.Dispose(); WaitAsyncRestoreWithoutPump(pending); }
            }
        }

        private static void AsyncRestoreContextConfirmation(string root)
        {
            using (var fixture = new AsyncRestoreContextFixture(root))
            {
                int ownerThread = Thread.CurrentThread.ManagedThreadId, picked = 0, chosen = 0, confirmed = 0, launches = 0;
                string preparedPath = null;
                fixture.Context.RestorePick = delegate(List<BackupInfo> items) {
                    Check(Thread.CurrentThread.ManagedThreadId == ownerThread && items.Count == 1 && items[0].Path == fixture.Source,
                        "actual worker list reaches the owner selector with the isolated previously indexed copy");
                    picked++; return items[0].Path;
                };
                fixture.Context.RestoreChoose = delegate(string directory, RestorePreview preview) {
                    Check(Thread.CurrentThread.ManagedThreadId == ownerThread && directory == fixture.Source && preview.HasData,
                        "worker-produced exact restore preview reaches the owner before separate data consent");
                    chosen++; return new RestoreChoice { Scope = "All", ConfirmData = true };
                };
                fixture.Context.RestoreConfirm = delegate(RestorePreparedInfo info) {
                    Check(Thread.CurrentThread.ManagedThreadId == ownerThread && info.Version == "0.0.1" && info.Scope == "All" && info.ConfirmData &&
                        info.Names.SequenceEqual(new[] { "ProGo.exe", "VERSION", "scripts", "settings.json", "vault.enc.json" }),
                        "actual verified preparation carries detached version, exact names and explicit consent to its owner confirmation");
                    confirmed++; preparedPath = info.Copy.Path; return false;
                };
                fixture.Context.RestoreLaunchInfo = delegate { launches++; throw new Exception("cancelled confirmation must not launch a helper"); };
                var pending = fixture.Context.StartRestoreAsync(); PumpAsyncRestore(() => pending.IsCompleted && !fixture.Context.IsBackupRunning);
                Check(picked == 1 && chosen == 1 && confirmed == 1 && launches == 0 && preparedPath != null && !Directory.Exists(preparedPath) &&
                    fixture.CliCleanup == 0 && fixture.WindowsCleanup == 0 && fixture.InstalledUnchanged() && AsyncRestoreTreeMatches(fixture.Source, fixture.SourceHashes),
                    "declined actual restore confirmation releases its actor and exact temporary stage before any proxy shutdown or helper launch");
            }
        }

        private sealed class AsyncRestoreLaunchScript : IDisposable
        {
            private readonly string directory = Path.Combine(AppPaths.Root, "scripts");
            private readonly string path = Path.Combine(AppPaths.Root, "scripts", "Restore-ProGoBackup.ps1");
            private readonly bool hadDirectory;
            private readonly byte[] original;
            internal AsyncRestoreLaunchScript()
            {
                hadDirectory = Directory.Exists(directory); original = File.Exists(path) ? File.ReadAllBytes(path) : null;
                if (original == null) { Directory.CreateDirectory(directory); File.WriteAllText(path, "# isolated launch preflight placeholder; never execute"); }
            }
            public void Dispose()
            {
                if (original != null) File.WriteAllBytes(path, original);
                else if (File.Exists(path)) File.Delete(path);
                if (!hadDirectory && Directory.Exists(directory) && Directory.GetFileSystemEntries(directory).Length == 0) Directory.Delete(directory);
            }
        }

        private static void AsyncRestoreAcceptSelections(AsyncRestoreContextFixture fixture)
        {
            int ownerThread = Thread.CurrentThread.ManagedThreadId;
            fixture.Context.RestorePick = delegate { Check(Thread.CurrentThread.ManagedThreadId == ownerThread, "preACK restore source selection remains on its owner"); return fixture.Source; };
            fixture.Context.RestoreChoose = delegate { return new RestoreChoice { Scope = "Program", ConfirmData = false }; };
            fixture.Context.RestoreConfirm = delegate { Check(Thread.CurrentThread.ManagedThreadId == ownerThread, "preACK restore confirmation remains on its owner"); return true; };
        }

        private static void AsyncRestoreContextLaunchReservation(string root)
        {
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var fixture = new AsyncRestoreContextFixture(root))
            using (var pulse = new System.Windows.Forms.Timer { Interval = 10 })
            {
                AsyncRestoreAcceptSelections(fixture);
                int launches = 0, ticks = 0; CancellationToken observed = default(CancellationToken);
                fixture.Context.RestoreLaunchInfo = delegate { launches++; throw new Exception("cancelled launch phase must not start a helper"); };
                fixture.Context.BackupIoProbe = delegate(string phase, CancellationToken token) {
                    if (phase == "restore-launch") { observed = token; entered.Set(); release.Wait(); }
                };
                pulse.Tick += delegate { ticks++; }; pulse.Start();
                fixture.Commands[AppCommand.RestoreBackup].PerformClick(); var pending = fixture.Context.RestoreWork;
                try {
                    PumpAsyncRestore(() => entered.IsSet, 10000);
                    var session = (BackupWorkSession)Field(fixture.Context, "backupActivity"); var owned = AsyncRestoreOwnedPaths(session).Single();
                    Check((bool)Field(fixture.Context, "shutdownPrepared") && fixture.CliCleanup == 1 && fixture.WindowsCleanup == 1 && Directory.Exists(owned),
                        "actual tray restore reserves its verified stage after internal proxy cleanup and before helper acceptance");
                    fixture.Context.CompleteShutdown(); Application.DoEvents();
                    Check(!(bool)Field(fixture.Context, "closing") && ((NotifyIcon)Field(fixture.Context, "tray")).Visible,
                        "internal restore cleanup reservation cannot complete application shutdown before helper acknowledgement");
                    var watch = Stopwatch.StartNew();
                    var shutdown = fixture.Context.RequestShutdownAsync(); var duplicate = fixture.Context.RequestShutdownAsync();
                    PumpAsyncRestore(() => shutdown.IsCompleted, 10000); watch.Stop();
                    Check(Object.ReferenceEquals(shutdown, duplicate) && !shutdown.Result && observed.IsCancellationRequested && !pending.IsCompleted &&
                        watch.ElapsedMilliseconds >= 2800 && watch.ElapsedMilliseconds < 6000 && ticks >= 20 && Directory.Exists(owned) &&
                        fixture.CliCleanup == 1 && fixture.WindowsCleanup == 1 && launches == 0 && !(bool)Field(fixture.Context, "closing"),
                        "external IPC shutdown cancels a preACK launch reservation and refuses after actual three seconds while its worker and stage remain owned");
                    release.Set(); PumpAsyncRestore(() => pending.IsCompleted && !fixture.Context.IsBackupRunning);
                    Check(launches == 0 && !Directory.Exists(owned) && fixture.InstalledUnchanged() && AsyncRestoreTreeMatches(fixture.Source, fixture.SourceHashes),
                        "late release of cancelled real restore launch cannot start a helper or accept restoration and cleans its exact stage");
                    var retry = fixture.Context.RequestShutdownAsync(); PumpAsyncRestore(() => retry.IsCompleted);
                    Check(retry.Result && fixture.CliCleanup == 2 && fixture.WindowsCleanup == 2,
                        "explicit shutdown retry after preACK launch settlement owns one fresh final proxy cleanup");
                    fixture.Context.CancelShutdown(); PumpAsyncRestore(() => fixture.Commands[AppCommand.CreateBackup].Enabled);
                }
                finally { release.Set(); fixture.Context.Dispose(); WaitAsyncRestoreWithoutPump(pending, 25000); }
            }
        }

        private static void AsyncRestoreContextPendingHelperShutdown(string root)
        {
            using (var fixture = new AsyncRestoreContextFixture(root))
            using (var helper = new AsyncRestoreHelper(root, "context-preACK"))
            using (var script = new AsyncRestoreLaunchScript())
            using (var pulse = new System.Windows.Forms.Timer { Interval = 10 })
            {
                AsyncRestoreAcceptSelections(fixture);
                int ownerThread = Thread.CurrentThread.ManagedThreadId, factoryThread = 0, ticks = 0;
                string preparedPath = null;
                fixture.Context.RestoreLaunchInfo = delegate(RestorePreparedInfo info) {
                    factoryThread = Thread.CurrentThread.ManagedThreadId; preparedPath = info.Copy.Path; return helper.Info(info.Copy.Path);
                };
                pulse.Tick += delegate { ticks++; }; pulse.Start();
                fixture.Commands[AppCommand.RestoreBackup].PerformClick(); var pending = fixture.Context.RestoreWork;
                try {
                    PumpAsyncRestore(() => helper.Started || pending.IsCompleted, 10000);
                    Check(helper.Started && !pending.IsCompleted, "actual tray restore starts its contained real helper and descendant without blocking its owner");
                    helper.CaptureProcesses(); PumpAsyncRestore(() => ticks >= 3);
                    Check(factoryThread != ownerThread && (bool)Field(fixture.Context, "shutdownPrepared") && !helper.Acknowledged && Directory.Exists(preparedPath),
                        "real helper-ready wait remains a worker-owned preACK reservation after internal native cleanup");
                    fixture.Context.CompleteShutdown(); Application.DoEvents();
                    Check(!(bool)Field(fixture.Context, "closing"), "preACK contained helper cannot make CompleteShutdown exit its initiating owner");
                    var shutdown = fixture.Context.RequestShutdownAsync(); var duplicate = fixture.Context.RequestShutdownAsync();
                    PumpAsyncRestore(() => shutdown.IsCompleted, 10000);
                    Check(Object.ReferenceEquals(shutdown, duplicate) && shutdown.Result && helper.Exited && !helper.Acknowledged && !Directory.Exists(preparedPath),
                        "external IPC accepts shutdown only after cancelled real preACK helper and descendant exit and parent stage cleanup settle");
                    PumpAsyncRestore(() => pending.IsCompleted && !fixture.Context.IsBackupRunning);
                    Check(!pending.IsFaulted && (bool)Field(fixture.Context, "shutdownPrepared") && !(bool)Field(fixture.Context, "closing") &&
                        fixture.InstalledUnchanged() && AsyncRestoreTreeMatches(fixture.Source, fixture.SourceHashes),
                        "late cancelled restore completion cannot erase a newer externally accepted shutdown or alter installed and previous backup bytes");
                    fixture.Context.CancelShutdown(); PumpAsyncRestore(() => fixture.Commands[AppCommand.CreateBackup].Enabled);
                }
                finally { helper.ReleaseExit(); fixture.Context.Dispose(); WaitAsyncRestoreWithoutPump(pending, 25000); }
            }
        }

        private static void AsyncRestoreContextDelayedOwnerCompletion(string root)
        {
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var fixture = new AsyncRestoreContextFixture(root))
            using (var pulse = new System.Windows.Forms.Timer { Interval = 10 })
            {
                int completions = 0, ticks = 0; string preparedPath = null;
                fixture.Context.RestorePick = delegate { return fixture.Source; };
                fixture.Context.RestoreChoose = delegate { return new RestoreChoice { Scope = "All", ConfirmData = true }; };
                fixture.Context.RestoreConfirm = delegate(RestorePreparedInfo info) { preparedPath = info.Copy.Path; return false; };
                fixture.Context.BackupIoProbe = delegate(string phase, CancellationToken token) {
                    if (phase == "backup-owner-completion") { Interlocked.Increment(ref completions); entered.Set(); release.Wait(); }
                };
                pulse.Tick += delegate { ticks++; }; pulse.Start();
                fixture.Commands[AppCommand.RestoreBackup].PerformClick(); var pending = fixture.Context.RestoreWork;
                try {
                    PumpAsyncRestore(() => entered.IsSet, 10000);
                    var session = (BackupWorkSession)Field(fixture.Context, "backupActivity");
                    Check(session.CurrentWork.IsCompleted && preparedPath != null && !Directory.Exists(preparedPath) && !pending.IsCompleted &&
                        fixture.Context.IsBackupRunning && fixture.CliCleanup == 0 && fixture.WindowsCleanup == 0,
                        "actual restore disk work and exact stage cleanup can finish while its tracked owner publication is still pending");
                    var watch = Stopwatch.StartNew();
                    var shutdown = fixture.Context.RequestShutdownAsync(); var duplicate = fixture.Context.RequestShutdownAsync();
                    PumpAsyncRestore(() => shutdown.IsCompleted, 10000); watch.Stop();
                    Check(Object.ReferenceEquals(shutdown, duplicate) && !shutdown.Result && !pending.IsCompleted &&
                        watch.ElapsedMilliseconds >= 2800 && watch.ElapsedMilliseconds < 6000 && ticks >= 20 &&
                        fixture.CliCleanup == 0 && fixture.WindowsCleanup == 0 && !(bool)Field(fixture.Context, "shutdownPrepared"),
                        "external shutdown awaits whole restore owner completion and refuses after three seconds even when all disk tasks already finished");
                    release.Set(); PumpAsyncRestore(() => pending.IsCompleted && !fixture.Context.IsBackupRunning);
                    var retry = fixture.Context.RequestShutdownAsync(); PumpAsyncRestore(() => retry.IsCompleted);
                    int after = ticks; PumpAsyncRestore(() => ticks >= after + 3);
                    Check(retry.Result && completions == 1 && fixture.CliCleanup == 1 && fixture.WindowsCleanup == 1 &&
                        (bool)Field(fixture.Context, "shutdownPrepared") && fixture.InstalledUnchanged() && AsyncRestoreTreeMatches(fixture.Source, fixture.SourceHashes),
                        "explicit retry after delayed owner publication accepts one final cleanup and cannot be reset by stale restore completion");
                    fixture.Context.CancelShutdown(); PumpAsyncRestore(() => fixture.Commands[AppCommand.CreateBackup].Enabled);
                }
                finally { release.Set(); fixture.Context.Dispose(); WaitAsyncRestoreWithoutPump(pending, 25000); }
            }
        }

        private sealed class AsyncRestoreHelper : IDisposable
        {
            internal readonly string Root;
            private Process rootProcess, descendant;
            internal string ChildCopy { get { return File.ReadAllText(Path.Combine(Root, "child-copy")); } }
            internal bool Started { get { return File.Exists(Path.Combine(Root, "pids")); } }
            internal bool Acknowledged { get { return File.Exists(Path.Combine(Root, "acknowledged")); } }
            internal bool Exited { get { return rootProcess.HasExited && descendant.HasExited; } }
            internal bool RootAlive { get { return !rootProcess.HasExited; } }

            internal AsyncRestoreHelper(string root, string mode)
            {
                Root = Path.Combine(root, "helper-" + mode + "-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(Root);
                File.WriteAllText(Path.Combine(Root, "helper.ps1"), @"
$ErrorActionPreference = 'Stop'
[void][Reflection.Assembly]::LoadFrom($env:PROGO_TEST_RESTORE_ASSEMBLY)
$fixture = $env:PROGO_TEST_RESTORE_FIXTURE
$copy = $null; $lease = $null; $descendant = $null
try {
    $copy = [ProGo.BackupIntegrity]::Prepare($env:PROGO_TEST_RESTORE_INPUT)
    [IO.File]::WriteAllText((Join-Path $fixture 'child-copy'), $copy.Path)
    $lease = [ProGo.MaintenanceOperation]::Enter()
    $descInfo = New-Object Diagnostics.ProcessStartInfo
    $descInfo.FileName = $env:PROGO_TEST_RESTORE_POWERSHELL
    $descInfo.Arguments = '-NoProfile -Command ""Start-Sleep -Seconds 120""'
    $descInfo.UseShellExecute = $false; $descInfo.CreateNoWindow = $true
    $descendant = [Diagnostics.Process]::Start($descInfo)
    [IO.File]::WriteAllLines((Join-Path $fixture 'pids.tmp'), [string[]]@([string]$PID,[string]$descendant.Id))
    [IO.File]::Move((Join-Path $fixture 'pids.tmp'), (Join-Path $fixture 'pids'))
    $deadline = [DateTime]::UtcNow.AddSeconds(60)
    while (-not (Test-Path (Join-Path $fixture 'release-ack'))) {
        if ((Test-Path (Join-Path $fixture 'release-exit')) -or [DateTime]::UtcNow -gt $deadline) { exit 0 }
        Start-Sleep -Milliseconds 30
    }
    [ProGo.MaintenanceOperation]::ConfirmHandoff()
    [IO.File]::WriteAllText((Join-Path $fixture 'acknowledged'), 'ready')
    while (-not (Test-Path (Join-Path $fixture 'release-exit'))) {
        if ([DateTime]::UtcNow -gt $deadline) { exit 0 }
        Start-Sleep -Milliseconds 30
    }
} finally {
    if ($null -ne $descendant) {
        if (-not $descendant.HasExited) { $descendant.Kill(); [void]$descendant.WaitForExit(5000) }
        $descendant.Dispose()
    }
    if ($null -ne $copy) { $copy.Dispose() }
    if ($null -ne $lease) { $lease.Dispose() }
}
");
            }

            internal ProcessStartInfo Info(string prepared)
            {
                var powershell = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "WindowsPowerShell", "v1.0", "powershell.exe");
                var info = new ProcessStartInfo(powershell, "-NoProfile -ExecutionPolicy Bypass -File \"" + Path.Combine(Root, "helper.ps1") + "\"") {
                    UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = Root
                };
                info.EnvironmentVariables["PROGO_TEST_RESTORE_ASSEMBLY"] = typeof(MaintenanceOperation).Assembly.Location;
                info.EnvironmentVariables["PROGO_TEST_RESTORE_FIXTURE"] = Root;
                info.EnvironmentVariables["PROGO_TEST_RESTORE_INPUT"] = prepared;
                info.EnvironmentVariables["PROGO_TEST_RESTORE_POWERSHELL"] = powershell;
                return info;
            }

            internal void CaptureProcesses()
            {
                var lines = File.ReadAllLines(Path.Combine(Root, "pids"));
                rootProcess = Process.GetProcessById(Int32.Parse(lines[0]));
                descendant = Process.GetProcessById(Int32.Parse(lines[1]));
            }
            internal void ReleaseAcknowledgement() { File.WriteAllText(Path.Combine(Root, "release-ack"), "ready"); }
            internal void ReleaseExit() { File.WriteAllText(Path.Combine(Root, "release-exit"), "exit"); }

            public void Dispose()
            {
                // These handles refer only to processes captured from our private child fixture.
                foreach (var process in new[] { descendant, rootProcess }) if (process != null) {
                    try { if (!process.HasExited) { process.Kill(); process.WaitForExit(5000); } }
                    catch (InvalidOperationException) { }
                    finally { process.Dispose(); }
                }
                // A deliberately terminated helper cannot execute its finally.
                // Clean only the exact path it registered, never a wildcard temporary pool.
                var recorded = Path.Combine(Root, "child-copy");
                if (File.Exists(recorded)) {
                    var copy = File.ReadAllText(recorded);
                    if (Directory.Exists(copy)) BackupIntegrity.DeletePrepared(copy);
                }
            }
        }

        private static void AsyncRestoreOwnedHandoff(string root, string mode)
        {
            var source = AsyncRestoreSource(root, "handoff-source-" + mode); var before = AsyncRestoreHashes(source);
            using (var session = new BackupWorkSession())
            using (var helper = new AsyncRestoreHelper(root, mode))
            using (var pulse = new System.Windows.Forms.Timer { Interval = 10 })
            {
                var preparation = session.Run("restore-prepare", token => session.Prepare(source, "Program", false, token));
                PumpAsyncRestore(() => preparation.IsCompleted); var prepared = preparation.GetAwaiter().GetResult();
                int ticks = 0; pulse.Tick += delegate { ticks++; }; pulse.Start();
                var watch = Stopwatch.StartNew();
                var handoff = session.Run("restore-handoff", token => MaintenanceOperation.StartOwnedHandoff(helper.Info(prepared.Copy.Path), token));
                try {
                    PumpAsyncRestore(() => helper.Started || handoff.IsCompleted, 10000);
                    Check(helper.Started && !handoff.IsCompleted, "actual restore helper waits for acknowledgement while its contained descendant is running: " + mode);
                    helper.CaptureProcesses(); var childCopy = helper.ChildCopy; BackupIntegrity.Validate(childCopy);
                    PumpAsyncRestore(() => ticks >= 3);
                    Check(!helper.Acknowledged && Directory.Exists(prepared.Copy.Path) && AsyncRestoreTreeMatches(childCopy, before) && AsyncRestoreTreeMatches(source, before),
                        "native UI heartbeat continues during real helper-ready wait with independently verified child evidence: " + mode);
                    if (mode == "accept") {
                        helper.ReleaseAcknowledgement(); PumpAsyncRestore(() => handoff.IsCompleted && helper.Acknowledged);
                        var accepted = handoff.GetAwaiter().GetResult(); session.Cancel();
                        var finish = session.FinishAsync(); PumpAsyncRestore(() => finish.IsCompleted);
                        Check(accepted.Accepted && helper.RootAlive && Directory.Exists(childCopy) && !Directory.Exists(prepared.Copy.Path),
                            "actual helper acknowledgement transfers accepted ownership before caller cleanup and survives late cancellation");
                        helper.ReleaseExit(); PumpAsyncRestore(() => helper.Exited, 10000);
                        Check(!Directory.Exists(childCopy) && AsyncRestoreTreeMatches(source, before),
                            "acknowledged child releases its independent temporary copy and descendant without touching the original backup");
                    }
                    else {
                        if (mode == "cancel") session.Cancel();
                        int after = ticks; PumpAsyncRestore(() => handoff.IsCompleted, 25000);
                        var refused = handoff.GetAwaiter().GetResult();
                        Check(!refused.Accepted && helper.Exited && !helper.Acknowledged && Directory.Exists(prepared.Copy.Path) &&
                            refused.Error.Contains(mode == "cancel" ? "отменена" : "15 секунд"),
                            "refused handoff settles the exact contained helper and descendant before releasing caller evidence: " + mode);
                        if (mode == "timeout") Check(watch.ElapsedMilliseconds >= 14500 && watch.ElapsedMilliseconds < 25000 && ticks > after + 20,
                            "actual fifteen-second helper-ready deadline runs entirely on a worker while the native UI keeps ticking");
                        var finish = session.FinishAsync(); PumpAsyncRestore(() => finish.IsCompleted);
                        Check(!Directory.Exists(prepared.Copy.Path) && AsyncRestoreTreeMatches(source, before),
                            "failed handoff cleans the parent-owned snapshot only after the actual child tree has exited: " + mode);
                    }
                }
                finally {
                    helper.ReleaseExit(); session.Cancel(); WaitAsyncRestoreWithoutPump(session.FinishAsync(), 25000);
                }
            }
        }
    }
}

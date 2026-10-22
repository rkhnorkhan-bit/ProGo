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
        private static void DiagnosticContextWorkflow()
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") {
                Console.WriteLine("SKIP: diagnostic context ownership fixtures require isolated Windows CI"); return;
            }
            var root = Path.Combine(work, "diagnostic-context-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try {
                SafeLogWorkerWorkflow();
                SafeLogOwnerEpochWorkflow();
                SafeLogContextSettlement(root, false);
                SafeLogContextSettlement(root, true);
                LogOpenWorkerWorkflow(root);
                LogOpenContextWorkflow(root, false);
                LogOpenContextWorkflow(root, true);
                foreach (var folder in new[] { AppCommand.OpenFolder, AppCommand.OpenBackups }) {
                    LogOpenContextWorkflow(root, false, folder);
                    LogOpenContextWorkflow(root, true, folder);
                }
                DiagnosticContextHeldIo(root, "build", false);
                DiagnosticContextHeldIo(root, "save", true);
                DiagnosticContextHeldIo(root, "build", true, true);
            }
            finally { SafeLog.BeginOwner(); if (Directory.Exists(root)) Directory.Delete(root, true); }
        }

        private static void SafeLogOwnerEpochWorkflow()
        {
            const string marker = "safe-log-epoch-fixture"; int persisted = 0;
            var previous = SafeLog.BeforeDiskWrite;
            SafeLog.BeforeDiskWrite = line => { if (line.Contains(marker)) Interlocked.Increment(ref persisted); };
            try {
                long old = SafeLog.BeginOwner(), current = SafeLog.BeginOwner();
                WaitAsyncRestoreWithoutPump(SafeLog.SuspendOwnerAsync(current), 5000);
                SafeLog.ResumeOwner(old); SafeLog.Info(marker + " stale resume");
                WaitAsyncRestoreWithoutPump(SafeLog.FlushAsync(), 5000);
                Check(persisted == 0, "obsolete log owner cannot reopen the newer owner's suspended admission");
                SafeLog.ResumeOwner(current); SafeLog.Info(marker + " current resume");
                WaitAsyncRestoreWithoutPump(SafeLog.FlushAsync(), 5000);
                Check(persisted == 1, "current log owner explicitly resumes native persistence once");
                SafeLog.BeginOwner(); WaitAsyncRestoreWithoutPump(SafeLog.SuspendOwnerAsync(current), 5000);
                SafeLog.Info(marker + " stale suspend"); WaitAsyncRestoreWithoutPump(SafeLog.FlushAsync(), 5000);
                Check(persisted == 2, "obsolete shutdown/disposal cannot suspend a newly-created log owner");
            }
            finally { try { WaitAsyncRestoreWithoutPump(SafeLog.FlushAsync(), 5000); } finally { SafeLog.BeforeDiskWrite = previous; SafeLog.BeginOwner(); } }
        }

        private static void SafeLogContextSettlement(string root, bool forceDispose)
        {
            using (var fixture = new AsyncRestoreContextFixture(root))
            using (var entered = new ManualResetEventSlim()) using (var release = new ManualResetEventSlim())
            using (var pulse = new System.Windows.Forms.Timer { Interval = 10 }) {
                const string marker = "safe-log-context-held-fixture";
                var previous = SafeLog.BeforeDiskWrite; int ticks = 0, sinkThread = 0, ownerThread = Thread.CurrentThread.ManagedThreadId;
                Task pending = null;
                SafeLog.BeforeDiskWrite = line => { if (!line.Contains(marker)) return; sinkThread = Thread.CurrentThread.ManagedThreadId; entered.Set(); release.Wait(); };
                pulse.Tick += delegate { ticks++; }; pulse.Start();
                try {
                    SafeLog.Info(marker); PumpAsyncRestore(() => entered.IsSet && ticks >= 4, 5000);
                    Check(sinkThread != ownerThread && !SafeLog.WriteWork.IsCompleted, "actual global logger holds its disk call independently of native owner heartbeat");
                    if (forceDispose) {
                        fixture.Context.Dispose(); pending = fixture.Context.LogWriterWork;
                        Check(!pending.IsCompleted, "forced native owner disposal retains the complete pending log writer lifetime");
                        release.Set(); WaitAsyncRestoreWithoutPump(pending, 5000);
                        Check(!pending.IsFaulted, "closed log owner drains its accepted disk work without requiring a native UI pump");
                    } else {
                        var shutdown = fixture.Context.RequestShutdownAsync(); pending = fixture.Context.LogWriterWork;
                        Check(Object.ReferenceEquals(shutdown, fixture.Context.RequestShutdownAsync()), "duplicate shutdown shares the actual logger settlement reservation");
                        PumpAsyncRestore(() => shutdown.IsCompleted, 10000);
                        Check(!shutdown.Result && !pending.IsCompleted && ticks > 10 && fixture.CliCleanup == 0 && fixture.WindowsCleanup == 0,
                            "pending native log write refuses shutdown inside the existing budget before any proxy cleanup or handoff");
                        release.Set(); WaitAsyncRestoreWithoutPump(pending, 5000);
                        var retry = fixture.Context.RequestShutdownAsync(); PumpAsyncRestore(() => retry.IsCompleted, 10000);
                        Check(retry.Result && fixture.CliCleanup == 1 && fixture.WindowsCleanup == 1,
                            "explicit shutdown retry accepts only after real logger settlement and cleans native state once");
                    }
                    Check(fixture.InstalledUnchanged(), "logger shutdown never rewrites settings, vault or version while settling");
                }
                finally {
                    pulse.Stop(); release.Set();
                    try {
                        fixture.Context.Dispose();
                        if (pending != null) WaitAsyncRestoreWithoutPump(pending, 5000);
                        WaitAsyncRestoreWithoutPump(fixture.Context.LogWriterWork, 5000);
                        WaitAsyncRestoreWithoutPump(SafeLog.FlushAsync(), 5000);
                    } finally { SafeLog.BeforeDiskWrite = previous; }
                }
            }
        }

        private static void SafeLogWorkerWorkflow()
        {
            string secret = "fixture-only-never-log-" + Guid.NewGuid().ToString("N");
            const string marker = "safe-log-owned-worker-fixture";
            var captured = new System.Collections.Concurrent.ConcurrentQueue<string>();
            var previous = SafeLog.BeforeDiskWrite;
            int ownerThread = Thread.CurrentThread.ManagedThreadId, sinkThread = 0, ticks = 0;
            using (var entered = new ManualResetEventSlim()) using (var release = new ManualResetEventSlim())
            using (var owner = new Form()) using (var pulse = new System.Windows.Forms.Timer { Interval = 10 }) {
                Task pending = null;
                SafeLog.BeforeDiskWrite = record => {
                    if (!record.Contains(marker)) return;
                    sinkThread = Thread.CurrentThread.ManagedThreadId; captured.Enqueue(record); entered.Set(); release.Wait();
                };
                pulse.Tick += delegate { ticks++; }; owner.Show(); pulse.Start();
                try {
                    var watch = Stopwatch.StartNew(); SafeLog.Info(marker + " token=" + secret);
                    Check(watch.ElapsedMilliseconds < 1000 && entered.Wait(3000), "actual SafeLog call returns without waiting for its held native sink");
                    SafeLog.Error(marker + " second", new IOException("password=" + secret));
                    SafeLog.Info(new String('x', BoundedLog.MaxRecordChars + 1) + " token=" + secret);
                    pending = SafeLog.FlushAsync(); PumpAsyncRestore(() => ticks >= 4, 5000);
                    Check(!pending.IsCompleted && !SafeLog.WriteWork.IsCompleted && sinkThread != ownerThread && captured.Count == 1,
                        "actual logger retains one blocked worker while native UI heartbeat and new bounded records remain responsive");
                    Check(captured.All(x => !x.Contains(secret) && x.Contains("<redacted>") && x.Length <= BoundedLog.MaxRecordChars),
                        "the logger sends only redacted bounded data to the native sink, never raw secret or exception references");
                    release.Set(); WaitAsyncRestoreWithoutPump(pending, 5000);
                    string log = File.ReadAllText(AppPaths.LogPath);
                    Check(captured.Count == 2 && captured.All(x => !x.Contains(secret)) && !log.Contains(secret) && log.Contains("[oversized detail omitted]"),
                        "actual file sink drains ordered sanitized records and omits oversized raw input without leaking a cut credential");
                }
                finally {
                    pulse.Stop(); release.Set();
                    try { WaitAsyncRestoreWithoutPump(SafeLog.FlushAsync(), 5000); }
                    finally { SafeLog.BeforeDiskWrite = previous; }
                }
            }
        }

        private static void LogOpenContextWorkflow(string root, bool forceDispose, AppCommand command = AppCommand.OpenAppLog)
        {
            using (var fixture = new AsyncRestoreContextFixture(root))
            using (var entered = new ManualResetEventSlim()) using (var release = new ManualResetEventSlim())
            using (var pulse = new System.Windows.Forms.Timer { Interval = 10 }) {
                int ownerThread = Thread.CurrentThread.ManagedThreadId, workerThread = 0, launches = 0, entries = 0, ticks = 0;
                CancellationToken observed = default(CancellationToken); Task pending = null, retry = null; Process viewer = null;
                fixture.Context.LogOpenIoProbe = token => { workerThread = Thread.CurrentThread.ManagedThreadId; observed = token;
                    Interlocked.Increment(ref entries); entered.Set(); release.Wait(); };
                string expectedPath = command == AppCommand.OpenAppLog ? AppPaths.LogPath : command == AppCommand.OpenFolder ? AppPaths.Root : BackupService.BackupsRoot;
                fixture.Context.LogViewerLaunch = selected => {
                    Check(selected == expectedPath && (command == AppCommand.OpenAppLog || Directory.Exists(selected)),
                        "actual local viewer route prepares and retains its selected file or folder: " + command);
                    Interlocked.Increment(ref launches);
                    viewer = Process.Start(new ProcessStartInfo(Application.ExecutablePath, "--key-readiness-env") {
                        UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true
                    });
                };
                pulse.Tick += delegate { ticks++; }; pulse.Start();
                try {
                    fixture.Commands[command].PerformClick(); pending = fixture.Context.LogOpenWork;
                    PumpAsyncRestore(() => entered.IsSet && ticks >= 4);
                    Check(!pending.IsCompleted && workerThread != ownerThread && entries == 1 &&
                        !fixture.Commands[command].Enabled && !fixture.Commands[AppCommand.OpenUpdateLog].Enabled,
                        "actual log command runs file work off owner, preserves heartbeat and disables duplicate log admissions");
                    fixture.Commands[command].PerformClick(); fixture.Commands[AppCommand.OpenUpdateLog].PerformClick();
                    Check(Object.ReferenceEquals(pending, fixture.Context.LogOpenWork) && entries == 1 && launches == 0,
                        "duplicate actual log routes retain the same exact owned workflow without another worker or viewer");
                    if (forceDispose) {
                        fixture.Context.Dispose();
                        Check(observed.IsCancellationRequested && !pending.IsCompleted && Object.ReferenceEquals(pending, fixture.Context.LogOpenWork),
                            "forced native owner disposal cancels log admission and retains its unsettled I/O");
                        release.Set(); WaitAsyncRestoreWithoutPump(pending, 5000);
                        Check(launches == 0 && !pending.IsFaulted, "closed log owner produces no late native viewer or UI completion");
                    } else {
                        var shutdown = fixture.Context.RequestShutdownAsync();
                        Check(Object.ReferenceEquals(shutdown, fixture.Context.RequestShutdownAsync()), "duplicate shutdown shares the same pending log settlement");
                        PumpAsyncRestore(() => shutdown.IsCompleted, 10000);
                        Check(!shutdown.Result && observed.IsCancellationRequested && !pending.IsCompleted && ticks > 10 &&
                            fixture.CliCleanup == 0 && fixture.WindowsCleanup == 0,
                            "unsettled native log I/O refuses shutdown before proxy cleanup instead of reporting a false handoff");
                        release.Set(); PumpAsyncRestore(() => pending.IsCompleted, 5000);
                        Check(launches == 0 && fixture.Commands[command].Enabled,
                            "cancelled log operation settles without late viewer and leaves explicit retry available");
                        fixture.Context.LogOpenIoProbe = null;
                        fixture.Commands[command].PerformClick(); retry = fixture.Context.LogOpenWork;
                        PumpAsyncRestore(() => retry.IsCompleted, 5000);
                        Check(!Object.ReferenceEquals(pending, retry) && launches == 1 && viewer != null && viewer.WaitForExit(3000) && viewer.ExitCode == 0,
                            "explicit log retry starts exactly one fresh native viewer after old cancellation settles");
                        var next = fixture.Context.RequestShutdownAsync(); PumpAsyncRestore(() => next.IsCompleted, 10000);
                        Check(next.Result && fixture.CliCleanup == 1 && fixture.WindowsCleanup == 1,
                            "shutdown retry after actual log settlement performs native cleanup once");
                    }
                    Check(fixture.InstalledUnchanged(), "log workflow preserves installed settings, vault and version bytes");
                }
                finally {
                    pulse.Stop(); release.Set();
                    try {
                        fixture.Context.Dispose();
                        if (pending != null) WaitAsyncRestoreWithoutPump(pending, 5000);
                        if (retry != null) WaitAsyncRestoreWithoutPump(retry, 5000);
                    } finally {
                        if (viewer != null) try { if (!viewer.HasExited) { viewer.Kill(); if (!viewer.WaitForExit(3000)) throw new TimeoutException("owned routed log viewer cleanup deadline"); } } finally { viewer.Dispose(); }
                    }
                }
            }
        }

        private static void LogOpenWorkerWorkflow(string parent)
        {
            for (int mode = 0; mode < 4; mode++) {
                string root = Path.Combine(parent, "log-worker-" + mode); Directory.CreateDirectory(root);
                string path = Path.Combine(root, "app.log"); File.WriteAllText(path, "owned original log");
                byte[] original = File.ReadAllBytes(path); int ownerThread = Thread.CurrentThread.ManagedThreadId, workerThread = 0, entries = 0, launches = 0, ticks = 0;
                if (mode == 3) File.SetAttributes(path, FileAttributes.ReadOnly);
                using (var entered = new ManualResetEventSlim()) using (var release = new ManualResetEventSlim())
                using (var owner = new Form()) using (var pulse = new System.Windows.Forms.Timer { Interval = 10 }) {
                    Process viewer = null; CancellationToken observed = default(CancellationToken);
                    var operation = new LogOpenOperation(root, path,
                        () => { if (mode == 2) throw new IOException("owned preparation failed"); Directory.CreateDirectory(root); },
                        selected => {
                            Check(selected == path, "log open worker retains the exact requested path");
                            Interlocked.Increment(ref launches);
                            viewer = Process.Start(new ProcessStartInfo(Application.ExecutablePath, "--key-readiness-env") {
                                UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true
                            });
                        }, token => { workerThread = Thread.CurrentThread.ManagedThreadId; observed = token; Interlocked.Increment(ref entries); entered.Set(); release.Wait(); });
                    pulse.Tick += delegate { ticks++; }; owner.Show(); pulse.Start();
                    Task<bool> pending = operation.Work;
                    try {
                        Check(Object.ReferenceEquals(pending, operation.Begin()) && Object.ReferenceEquals(pending, operation.Begin()),
                            "log operation publishes one owned task before queue and duplicate Begin shares it");
                        Check(entered.Wait(3000), "log worker reaches its bounded preparation barrier");
                        PumpAsyncRestore(() => ticks >= 4, 5000);
                        Check(!pending.IsCompleted && workerThread != ownerThread && entries == 1,
                            "held log preparation keeps the native UI heartbeat responsive and remains single flight");
                        if (mode == 1) {
                            operation.Cancel();
                            Check(observed.IsCancellationRequested && !pending.IsCompleted && launches == 0,
                                "log cancellation forbids admission but does not claim an uninterruptible read settled");
                        }
                        release.Set(); WaitAsyncRestoreWithoutPump(pending, 5000);
                        Check((mode == 0 || mode == 3) ? pending.Result && launches == 1 && operation.Error == null :
                            !pending.Result && launches == 0 && (mode == 1 ? operation.Error == null : operation.Error is IOException),
                            "log open distinguishes native launch, cancellation and preparation failure without late or duplicate viewer");
                        Check(File.ReadAllBytes(path).SequenceEqual(original), "log worker preserves existing bytes, including a read-only log");
                        if (viewer != null) Check(viewer.WaitForExit(3000) && viewer.ExitCode == 0, "only the owned native viewer fixture executes and exits");
                    }
                    finally {
                        pulse.Stop(); operation.Cancel(); release.Set();
                        try { WaitAsyncRestoreWithoutPump(pending, 5000); }
                        finally {
                            try {
                                if (viewer != null) try { if (!viewer.HasExited) { viewer.Kill(); if (!viewer.WaitForExit(3000)) throw new TimeoutException("owned log viewer cleanup deadline"); } } finally { viewer.Dispose(); }
                            } finally { if (mode == 3) File.SetAttributes(path, FileAttributes.Normal); }
                        }
                    }
                }
            }
        }

        private static void DiagnosticContextHeldIo(string root, string heldPhase, bool forceDispose, bool formOnly = false)
        {
            var logs = Path.Combine(root, heldPhase + (formOnly ? "-reentrant" : "")); Directory.CreateDirectory(logs);
            var source = Path.Combine(logs, "progo.log");
            File.WriteAllText(source, "2026-01-02 03:04:05 +00:00 [INFO] SOCKS start requested.\n");
            var before = File.ReadAllBytes(source); var destination = Path.Combine(logs, "reviewed-report.txt");
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var fixture = new AsyncRestoreContextFixture(root))
            using (var pulse = new System.Windows.Forms.Timer { Interval = 10 })
            {
                int ownerThread = Thread.CurrentThread.ManagedThreadId, ioThread = 0, factories = 0, entries = 0, ticks = 0, stage = 0;
                CancellationToken observed = default(CancellationToken); Task pending = null; Task<bool> shutdown = null;
                DiagnosticPreviewForm form = null; var watch = Stopwatch.StartNew();
                fixture.Context.DiagnosticFormFactory = delegate(string ignored, string version) {
                    factories++;
                    return form = new DiagnosticPreviewForm(logs, version,
                        chooseSave: delegate(IWin32Window owner) {
                            Check(Thread.CurrentThread.ManagedThreadId == ownerThread && Object.ReferenceEquals(owner, form),
                                "actual diagnostic route chooses the destination on its native owner");
                            return destination;
                        },
                        beforeIo: delegate(string phase, CancellationToken token) {
                            if (phase != heldPhase) return;
                            ioThread = Thread.CurrentThread.ManagedThreadId; observed = token;
                            Interlocked.Increment(ref entries); entered.Set(); release.Wait();
                        });
                };
                pulse.Tick += delegate {
                    ticks++;
                    if (watch.ElapsedMilliseconds > 15000) throw new Exception("diagnostic context fixture deadline");
                    if (heldPhase == "save" && stage == 0 && form != null && form.ReportReady && !form.IsBusy) {
                        stage = 1;
                        Descendants(form).OfType<Button>().Single(b => b.Text == "Сохранить как…").PerformClick();
                    }
                    int expected = heldPhase == "save" ? 1 : 0;
                    if (stage == expected && entered.IsSet && ticks >= 3) {
                        stage = 2; pending = fixture.Context.DiagnosticWork;
                        Check(form.Visible && form.IsBusy && !pending.IsCompleted && ioThread != ownerThread && entries == 1,
                            "actual tray diagnostic " + heldPhase + " keeps real I/O off its owner while the modal heartbeat runs");
                        fixture.Commands[AppCommand.ExportDiagnostics].PerformClick();
                        Check(factories == 1 && Object.ReferenceEquals(pending, fixture.Context.DiagnosticWork),
                            "duplicate actual diagnostic export retains the same form and pending native workflow");
                        if (forceDispose) {
                            if (formOnly) {
                                form.Disposed += delegate {
                                    fixture.Commands[AppCommand.ExportDiagnostics].PerformClick();
                                    Check(factories == 1 && Object.ReferenceEquals(pending, fixture.Context.DiagnosticWork),
                                        "reentrant export from Disposed cannot overtake the old diagnostic workflow before modal unwind");
                                    shutdown = fixture.Context.RequestShutdownAsync();
                                };
                                form.Dispose();
                            }
                            else fixture.Context.Dispose();
                            Check(observed.IsCancellationRequested && !pending.IsCompleted && form.IsDisposed,
                                "forced diagnostic owner disposal cancels publication without claiming an in-flight write has settled");
                            pulse.Stop();
                        }
                        else {
                            shutdown = fixture.Context.RequestShutdownAsync();
                            Check(Object.ReferenceEquals(shutdown, fixture.Context.RequestShutdownAsync()),
                                "duplicate shutdown requests share the exact diagnostic settlement wait");
                        }
                    }
                    if (!forceDispose && stage == 2 && shutdown != null && shutdown.IsCompleted) {
                        stage = 3;
                        Check(!shutdown.Result && observed.IsCancellationRequested && !pending.IsCompleted && ticks > 10 &&
                            fixture.CliCleanup == 0 && fixture.WindowsCleanup == 0 && !(bool)Field(fixture.Context, "shutdownPrepared"),
                            "held diagnostic I/O refuses shutdown within the existing deadline before any native cleanup or handoff");
                        release.Set(); pulse.Stop();
                    }
                };
                pulse.Start();
                try {
                    fixture.Commands[AppCommand.ExportDiagnostics].PerformClick();
                    Check(pending != null && factories == 1, "the actual diagnostic modal route owns its complete build or save workflow");
                    if (forceDispose) {
                        Check(!pending.IsCompleted && Object.ReferenceEquals(pending, fixture.Context.DiagnosticWork),
                            "closed modal context retains unsettled diagnostic work after forced disposal");
                        if (formOnly) {
                            Check(shutdown != null, "native Disposed callback reenters shutdown before the diagnostic modal finally unwinds");
                            PumpAsyncRestore(() => shutdown.IsCompleted, 10000);
                            Check(!shutdown.Result && !pending.IsCompleted && fixture.CliCleanup == 0 && fixture.WindowsCleanup == 0,
                                "reentrant shutdown from disposed diagnostic form still waits for held native I/O and refuses premature handoff");
                        }
                        release.Set(); WaitAsyncRestoreWithoutPump(pending);
                        Check(!File.Exists(destination), "cancelled held native diagnostic write creates no destination after its owner closes");
                    }
                    else {
                        PumpAsyncRestore(() => pending.IsCompleted);
                        var retry = fixture.Context.RequestShutdownAsync(); PumpAsyncRestore(() => retry.IsCompleted, 10000);
                        Check(retry.Result && fixture.CliCleanup == 1 && fixture.WindowsCleanup == 1,
                            "explicit shutdown retry accepts only after real diagnostic settlement and performs native cleanup once");
                    }
                    Check(!pending.IsFaulted && File.ReadAllBytes(source).SequenceEqual(before) && fixture.InstalledUnchanged(),
                        "diagnostic cancellation preserves source logs and installed bytes without private failure text or late publication");
                }
                finally {
                    pulse.Stop(); release.Set(); fixture.Context.Dispose();
                    if (pending != null) WaitAsyncRestoreWithoutPump(pending, 25000);
                }
            }
        }
    }
}

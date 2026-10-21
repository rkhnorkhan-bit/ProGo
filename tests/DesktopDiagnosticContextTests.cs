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
                DiagnosticContextHeldIo(root, "build", false);
                DiagnosticContextHeldIo(root, "save", true);
                DiagnosticContextHeldIo(root, "build", true, true);
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
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

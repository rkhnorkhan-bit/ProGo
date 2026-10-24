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
        private static void VaultContextOwnership()
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") {
                Console.WriteLine("SKIP: vault application ownership fixtures require isolated Windows CI"); return;
            }
            var root = Path.Combine(work, "vault-context-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try {
                VaultContextShutdownWaits(root);
                VaultContextDisposedOwner(root);
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }

        private static bool VaultContextAllowed(UpdateAwareTrayApplicationContext context, string method)
        { return (bool)typeof(UpdateAwareTrayApplicationContext).GetMethod(method, PrivateInstance).Invoke(context, null); }

        private static VaultUnlockForm VaultContextForm(AsyncRestoreContextFixture fixture,
            Func<bool, string, CancellationToken, VaultOpenResult> open)
        {
            var form = new VaultUnlockForm(token => new VaultProbeResult(VaultProbeStatus.Ready), open,
                () => VaultContextAllowed(fixture.Context, "CanOpenVault"),
                () => VaultContextAllowed(fixture.Context, "CanPublishVault"),
                task => Call(fixture.Context, "TrackVaultWork", task));
            typeof(UpdateAwareTrayApplicationContext).GetField("vaultUnlockForm", PrivateInstance).SetValue(fixture.Context, form);
            form.Show(); PumpAsyncRestore(() => form.PresentationWork != null && form.PresentationWork.IsCompleted && !form.IsBusy);
            return form;
        }

        private static void VaultContextShutdownWaits(string root)
        {
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var fixture = new AsyncRestoreContextFixture(root))
            using (var pulse = new System.Windows.Forms.Timer { Interval = 10 })
            {
                int ticks = 0, launches = 0, ownerThread = Thread.CurrentThread.ManagedThreadId, workerThread = 0;
                CancellationToken observed = default(CancellationToken);
                var resultSession = new VaultSession(VaultData.Empty(), "1234", false);
                using (var form = VaultContextForm(fixture, delegate(bool creating, string pin, CancellationToken token) {
                    workerThread = Thread.CurrentThread.ManagedThreadId; Interlocked.Increment(ref launches);
                    observed = token; entered.Set(); release.Wait(); return VaultOpenResult.Succeeded(resultSession);
                })) {
                    pulse.Tick += delegate { ticks++; }; pulse.Start();
                    ((TextBox)Field(form, "pin")).Text = "1234"; ((Button)Field(form, "primary")).PerformClick();
                    var pending = form.Work;
                    try {
                        PumpAsyncRestore(() => entered.IsSet && ticks >= 3);
                        Check(Object.ReferenceEquals(fixture.Context.VaultWork, pending) && fixture.Context.IsVaultRunning &&
                            form.IsBusy && workerThread != ownerThread && launches == 1,
                            "actual application retains the single raw vault worker while native unlock presentation remains responsive");
                        var watch = Stopwatch.StartNew();
                        var shutdown = fixture.Context.RequestShutdownAsync(); var duplicate = fixture.Context.RequestShutdownAsync();
                        PumpAsyncRestore(() => shutdown.IsCompleted, 10000); watch.Stop();
                        Check(Object.ReferenceEquals(shutdown, duplicate) && !shutdown.Result && observed.IsCancellationRequested &&
                            !pending.IsCompleted && fixture.Context.IsVaultRunning && form.IsBusy && form.Visible &&
                            fixture.CliCleanup == 0 && fixture.WindowsCleanup == 0 && !(bool)Field(fixture.Context, "shutdownPrepared"),
                            "shutdown cancels vault opening and refuses before any native proxy cleanup while its raw worker still owns the result");
                        Check(watch.ElapsedMilliseconds >= 2800 && watch.ElapsedMilliseconds < 6000 && ticks >= 20 &&
                            !VaultContextAllowed(fixture.Context, "CanOpenVault"),
                            "actual three-second vault shutdown deadline preserves native UI heartbeat and prevents another opening");
                        release.Set(); PumpAsyncRestore(() => pending.IsCompleted && form.PresentationWork.IsCompleted && !fixture.Context.IsVaultRunning);
                        Check(resultSession.IsDiscarded && resultSession.Pin == null && form.AcceptedSession == null &&
                            form.DialogResult != DialogResult.OK && launches == 1 && fixture.InstalledUnchanged(),
                            "late success after shutdown cancellation drops the unopened session and cannot silently accept or change installed files");
                        var retry = fixture.Context.RequestShutdownAsync(); PumpAsyncRestore(() => retry.IsCompleted);
                        Check(retry.Result && fixture.CliCleanup == 1 && fixture.WindowsCleanup == 1 &&
                            (bool)Field(fixture.Context, "shutdownPrepared") && !VaultContextAllowed(fixture.Context, "CanPublishVault") &&
                            AsyncRestoreTreeMatches(fixture.Source, fixture.SourceHashes),
                            "explicit shutdown retry starts exactly one native cleanup after the vault worker settles and closes publication");
                        fixture.Context.CancelShutdown();
                    }
                    finally {
                        release.Set(); fixture.Context.Dispose(); WaitAsyncRestoreWithoutPump(pending);
                        if (form.PresentationWork != null) WaitAsyncRestoreWithoutPump(form.PresentationWork);
                    }
                }
            }
        }

        private static void VaultContextDisposedOwner(string root)
        {
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var fixture = new AsyncRestoreContextFixture(root))
            {
                CancellationToken observed = default(CancellationToken); int lateUi = 0;
                var resultSession = new VaultSession(VaultData.Empty(), "1234", false);
                using (var form = VaultContextForm(fixture, delegate(bool creating, string pin, CancellationToken token) {
                    observed = token; entered.Set(); release.Wait(); return VaultOpenResult.Succeeded(resultSession);
                })) {
                    ((TextBox)Field(form, "pin")).Text = "1234"; ((Button)Field(form, "primary")).PerformClick();
                    var pending = form.Work;
                    try {
                        PumpAsyncRestore(() => entered.IsSet);
                        var status = (Label)Field(form, "status"); status.TextChanged += delegate { lateUi++; };
                        var watch = Stopwatch.StartNew(); fixture.Context.Dispose(); watch.Stop(); lateUi = 0;
                        Check(watch.ElapsedMilliseconds < 500 && observed.IsCancellationRequested && !pending.IsCompleted && form.IsDisposed &&
                            Object.ReferenceEquals(fixture.Context.VaultWork, pending) && !VaultContextAllowed(fixture.Context, "CanPublishVault"),
                            "forced application disposal promptly cancels and disposes the unlock owner while retaining its actual raw worker");
                        release.Set(); WaitAsyncRestoreWithoutPump(pending); WaitAsyncRestoreWithoutPump(form.PresentationWork);
                        Check(!pending.IsFaulted && resultSession.IsDiscarded && resultSession.Pin == null &&
                            form.AcceptedSession == null && lateUi == 0 && fixture.CliCleanup == 0 && fixture.WindowsCleanup == 0 &&
                            fixture.InstalledUnchanged() && AsyncRestoreTreeMatches(fixture.Source, fixture.SourceHashes),
                            "ownerless vault completion settles without UI pumping, drops its late session and starts no native cleanup or installed writes");
                        Application.DoEvents();
                        Check(lateUi == 0 && form.IsDisposed && form.DialogResult != DialogResult.OK,
                            "resumed native dispatch cannot revive the disposed vault owner or accept its stale worker result");
                    }
                    finally {
                        release.Set(); fixture.Context.Dispose(); WaitAsyncRestoreWithoutPump(pending);
                        if (form.PresentationWork != null) WaitAsyncRestoreWithoutPump(form.PresentationWork);
                    }
                }
            }
        }
    }
}

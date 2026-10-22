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
        private static void PhoneStopContextWorkflow()
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") {
                Console.WriteLine("SKIP: phone context ownership requires isolated Windows CI"); return;
            }
            var root = Path.Combine(work, "phone-context-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try {
                foreach (var route in new[] { "tray", "all", "shutdown", "dispose" }) PhoneStopContextHeld(root, route);
                PhoneStopContextLocked(root);
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }

        private static string PhoneContextSession(AsyncRestoreContextFixture fixture, string root)
        {
            var session = Path.Combine(root, "owned-session-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(session);
            File.WriteAllText(Path.Combine(session, "ssh_config"), "opaque owned local fixture; never execute");
            typeof(HomeVpnService).GetField("sessionDirectory", PrivateInstance).SetValue(fixture.Home, session);
            return session;
        }

        private static void PhoneStopContextHeld(string root, string route)
        {
            using (var fixture = new AsyncRestoreContextFixture(root, 250, settingsShutdownTimeout: 250))
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var pulse = new System.Windows.Forms.Timer { Interval = 10 })
            {
                string session = PhoneContextSession(fixture, root);
                int ticks = 0, removals = 0;
                fixture.Home.BeforeSessionIo = stage => {
                    if (stage == "cleanup") {
                        Interlocked.Increment(ref removals); entered.Set();
                        if (!release.Wait(5000)) throw new TimeoutException("Owned phone context cleanup barrier");
                    }
                };
                pulse.Tick += delegate { ticks++; }; pulse.Start();
                try {
                    Check((int)Field(fixture.Context, "settingsShutdownTimeoutMilliseconds") == 250 && (int)Field(fixture.Context, "backupShutdownTimeoutMilliseconds") == 250,
                        "phone cleanup fixture explicitly selects its short phone/settings deadline as well as backup deadline: " + route);
                    Task<bool> shutdown = null;
                    if (route == "tray") fixture.Commands[AppCommand.ShowMain].PerformClick();
                    var watch = Stopwatch.StartNew();
                    if (route == "shutdown") shutdown = fixture.Context.RequestShutdownAsync();
                    else if (route == "dispose") fixture.Context.Dispose();
                    else fixture.Commands[route == "all" ? AppCommand.StopAll : AppCommand.StopPhone].PerformClick();
                    Check(watch.ElapsedMilliseconds < 500, "actual phone context action returns before native session removal: " + route);
                    var pending = fixture.Context.PhoneStopWork;
                    PumpAsyncRestore(() => entered.IsSet && ticks >= 4, 3000);
                    Check(!pending.IsCompleted && Directory.Exists(session) && removals == 1,
                        "actual phone context retains one cleanup worker while native UI heartbeat continues: " + route);
                    if (route == "tray" || route == "all") {
                        Check(Object.ReferenceEquals(pending, fixture.Context.StopPhoneAsync())
                            && !fixture.Commands[AppCommand.StopPhone].Enabled && !fixture.Commands[AppCommand.Phone].Enabled,
                            "actual phone duplicate requests share ownership and disable only competing phone commands");
                        if (route == "tray") {
                            Check(fixture.Commands[AppCommand.StopDesktop].Enabled && fixture.CliCleanup == 0 && fixture.WindowsCleanup == 0,
                                "held phone Stop preserves independent desktop control and its native proxy state");
                            var main = (MainWindow)Field(fixture.Context, "mainWindow");
                            Check(((Label)Field(main, "phoneState")).Text == "Остановка…", "actual main phone card exposes pending cleanup rather than stopped state");
                        }
                        else {
                            PumpAsyncRestore(() => fixture.Context.IntegrationWork == null || fixture.Context.IntegrationWork.IsCompleted, 5000);
                            Check(fixture.CliCleanup == 1 && fixture.WindowsCleanup == 1,
                                "actual StopAll settles desktop cleanup while phone filesystem removal remains owned");
                        }
                    }
                    if (route == "shutdown") {
                        Check(Object.ReferenceEquals(shutdown, fixture.Context.RequestShutdownAsync()), "duplicate shutdown shares phone cleanup reservation");
                        PumpAsyncRestore(() => shutdown.IsCompleted, 5000);
                        if (shutdown.Result || pending.IsCompleted || ticks <= 5 || fixture.Home.StartsPaused || fixture.CliCleanup != 0 || fixture.WindowsCleanup != 0)
                            Console.WriteLine("PHONE_SHUTDOWN_STATE accepted=" + shutdown.Result + "; phoneTask=" + pending.Status + "; ticks=" + ticks
                                + "; paused=" + fixture.Home.StartsPaused + "; cliCleanup=" + fixture.CliCleanup + "; windowsCleanup=" + fixture.WindowsCleanup
                                + "; elapsedMs=" + watch.ElapsedMilliseconds + "; faultType=" + (pending.IsFaulted ? pending.Exception.GetBaseException().GetType().Name : "none"));
                        Check(!shutdown.Result && !pending.IsCompleted && ticks > 5 && !fixture.Home.StartsPaused
                            && fixture.CliCleanup == 0 && fixture.WindowsCleanup == 0,
                            "phone cleanup deadline refuses handoff before proxy effects and preserves retry with responsive UI");
                    }
                    release.Set();
                    if (route == "dispose") WaitAsyncRestoreWithoutPump(pending, 5000);
                    else PumpAsyncRestore(() => pending.IsCompleted, 5000);
                    Check(pending.GetAwaiter().GetResult() && !Directory.Exists(session) && removals == 1,
                        "phone result completes only after real exact-directory removal including closed-owner case: " + route);
                    if (route == "shutdown") {
                        var retry = fixture.Context.RequestShutdownAsync(); PumpAsyncRestore(() => retry.IsCompleted, 5000);
                        Check(retry.Result && fixture.Home.StartsPaused && fixture.CliCleanup == 1 && fixture.WindowsCleanup == 1,
                            "successful shutdown retry seals late phone starts and cleans native proxy state once");
                        fixture.Context.CancelShutdown(); PumpAsyncRestore(() => !fixture.Home.StartsPaused, 3000);
                        Check(!fixture.Home.StartsPaused, "cancelled prepared handoff explicitly resumes phone admission");
                    }
                    if (route == "tray" || route == "all") Check(fixture.Commands[AppCommand.StopPhone].Enabled && fixture.Commands[AppCommand.Phone].Enabled,
                        "settled phone cleanup restores command availability after its retained observation completes");
                    if (route == "tray") {
                        var main = (MainWindow)Field(fixture.Context, "mainWindow");
                        Check(((Label)Field(main, "phoneState")).Text == "Не запущен", "actual main phone card reports stopped only after real cleanup");
                    }
                    Check(fixture.InstalledUnchanged(), "phone context settlement preserves installed settings, vault and version: " + route);
                }
                finally {
                    release.Set(); fixture.Home.BeforeSessionIo = null;
                    try {
                        var cleanup = fixture.Home.StopAsync(); WaitAsyncRestoreWithoutPump(cleanup, 5000);
                        if (!cleanup.GetAwaiter().GetResult()) throw new IOException("Phone context cleanup incomplete");
                        PumpAsyncRestore(() => fixture.Context.PhoneStopWork.IsCompleted, 5000);
                    }
                    finally { pulse.Stop(); }
                }
            }
        }

        private static void PhoneStopContextLocked(string root)
        {
            using (var fixture = new AsyncRestoreContextFixture(root, 250, settingsShutdownTimeout: 250)) {
                string session = PhoneContextSession(fixture, root);
                try {
                    using (var held = File.Open(Path.Combine(session, "ssh_config"), FileMode.Open, FileAccess.Read, FileShare.None)) {
                        fixture.Commands[AppCommand.StopPhone].PerformClick();
                        var stopped = fixture.Context.PhoneStopWork; PumpAsyncRestore(() => stopped.IsCompleted, 5000);
                        Check(!stopped.Result && Directory.Exists(session) && fixture.Home.HasOwnedResources
                            && fixture.Commands[AppCommand.StopPhone].Enabled && fixture.Commands[AppCommand.Phone].Enabled,
                            "actual locked phone session refuses success and keeps explicit tray retry enabled");
                        var shutdown = fixture.Context.RequestShutdownAsync(); PumpAsyncRestore(() => shutdown.IsCompleted, 5000);
                        Check(!shutdown.Result && Directory.Exists(session) && !fixture.Home.StartsPaused
                            && fixture.CliCleanup == 0 && fixture.WindowsCleanup == 0,
                            "locked phone cleanup refuses shutdown without losing ownership or modifying desktop proxy");
                    }
                    fixture.Commands[AppCommand.StopPhone].PerformClick();
                    var retry = fixture.Context.PhoneStopWork; PumpAsyncRestore(() => retry.IsCompleted, 5000);
                    Check(retry.Result && !Directory.Exists(session) && !fixture.Home.HasOwnedResources,
                        "explicit tray retry cleans the same formerly locked phone session");
                }
                finally {
                    var cleanup = fixture.Home.StopAsync(); WaitAsyncRestoreWithoutPump(cleanup, 5000);
                    if (!cleanup.GetAwaiter().GetResult()) throw new IOException("Locked phone context cleanup incomplete");
                    PumpAsyncRestore(() => fixture.Context.PhoneStopWork.IsCompleted, 5000);
                }
            }
        }
    }
}

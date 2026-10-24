using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        private static void IntegrationMutationWorkflow(SettingsService settings)
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") {
                Console.WriteLine("SKIP: asynchronous native integration mutations require isolated CI"); return;
            }
            if (File.Exists(SystemProxyService.BackupPath) || File.Exists(CliProxyEnvironmentService.BackupPath))
                throw new Exception("integration mutation fixture is not isolated from existing ownership");
            var configuration = settings.Current.Clone();
            var original = SystemProxyService.ReadCurrent();
            var environment = CliProxyEnvironmentService.Names.ToDictionary(n => n, n => Environment.GetEnvironmentVariable(n, EnvironmentVariableTarget.User));
            var socks = Occupy(0); AnswerFixtureSocks(socks);
            try {
                var configured = configuration.Clone();
                configured.SocksHost = "127.0.0.1"; configured.SocksPort = Number(socks);
                configured.AutoCliProxy = configured.AutoSystemProxy = configured.AutoStartSocks = false;
                configured.AutoHttpProxyPort = true; configured.TrayCloseExplained = true;
                settings.Save(configured);
                IntegrationWindowsFailureAndRetry(settings);
                IntegrationCliNotificationFailureAndRetry(settings, environment);
                IntegrationCliApplyCorrectionFailureAndRetry(settings, environment);
                IntegrationEnableThenOff(settings, ProxyFeature.Cli);
                IntegrationEnableThenOff(settings, ProxyFeature.Windows);
                IntegrationShutdownWaits(settings);
                IntegrationDisposedCompletion(settings);
                IntegrationQueuedOffDisposed(settings);
                IntegrationQueuedShutdownDisposed(settings);
                IntegrationStopThenOwnerReconnect(settings);
            } finally {
                socks.Stop();
                if (File.Exists(CliProxyEnvironmentService.BackupPath)) File.Delete(CliProxyEnvironmentService.BackupPath);
                foreach (var pair in environment) Environment.SetEnvironmentVariable(pair.Key, pair.Value, EnvironmentVariableTarget.User);
                SystemProxyService.RestoreSnapshot(original);
                if (File.Exists(SystemProxyService.BackupPath)) File.Delete(SystemProxyService.BackupPath);
                settings.Save(configuration);
            }
        }

        private sealed class IntegrationContextFixture : IDisposable
        {
            internal readonly ProxyService Proxy;
            internal readonly CliProxyBridgeService Bridge;
            internal readonly UpdateAwareTrayApplicationContext Context;
            internal readonly MainWindow Main;
            private readonly Ikev2RelayService relay;
            private readonly HomeVpnService home;
            private readonly ClipboardService clipboard;
            private readonly ConnectionHealthMonitor health;
            internal IntegrationContextFixture(SettingsService settings, Func<WindowsProxyRestoreResult> restore = null,
                Action cliRestore = null, Action<ProxyFeature, AppSettings> apply = null, string sshExecutable = "unused-test-ssh")
            {
                Proxy = new ProxyService(() => settings.Current, s => settings.Save(s), sshExecutable, () => DateTime.UtcNow, false);
                Bridge = new CliProxyBridgeService(settings);
                relay = new Ikev2RelayService(); home = new HomeVpnService(relay);
                clipboard = new ClipboardService(settings); health = new ConnectionHealthMonitor(() => settings.Current);
                Context = new UpdateAwareTrayApplicationContext(settings, Proxy, Bridge, home, clipboard, false, health,
                    restore, null, null, 3000, null, cliRestore, apply);
                Context.RequestShowStatus(); Application.DoEvents(); Main = (MainWindow)Field(Context, "mainWindow");
            }
            public void Dispose()
            {
                if (!((bool)Field(Context, "disposed"))) {
                    var shutdown = Context.RequestShutdownAsync(); PumpIntegrationUntil(() => shutdown.IsCompleted);
                    if (!shutdown.Result) throw new Exception("integration fixture refused ordinary cleanup");
                }
                Context.Dispose();
                // Already active work must settle before fixture dependencies or
                // external snapshots change; emergency disposal starts no new off.
                var pending = Context.IntegrationWork;
                if (pending != null && !pending.Wait(15000)) throw new Exception("disposed integration fixture did not settle");
                clipboard.Dispose(); home.Dispose(); relay.Dispose(); health.Dispose(); Bridge.Dispose(); Proxy.Dispose();
            }
        }

        private static Dictionary<AppCommand, ToolStripItem> IntegrationCommands(UpdateAwareTrayApplicationContext context)
        {
            return MenuItems(((NotifyIcon)Field(context, "tray")).ContextMenuStrip.Items)
                .Where(i => i.Tag is AppCommand).ToDictionary(i => (AppCommand)i.Tag);
        }
        private static void WatchIntegrationCommands(UpdateAwareTrayApplicationContext context, Action callback)
        {
            var field = typeof(UpdateAwareTrayApplicationContext).GetField("CommandStateChanged", PrivateInstance);
            field.SetValue(context, Delegate.Combine((Action)field.GetValue(context), callback));
        }
        private static void WatchIntegrationControls(Control root, Action changed)
        {
            foreach (var control in new[] { root }.Concat(Descendants(root))) {
                control.TextChanged += delegate { changed(); };
                control.EnabledChanged += delegate { changed(); };
                control.VisibleChanged += delegate { changed(); };
            }
        }
        private static void WaitIntegration(UpdateAwareTrayApplicationContext context)
        {
            var task = context.IntegrationWork;
            PumpIntegrationUntil(() => task.IsCompleted && !context.IntegrationPending);
            task.GetAwaiter().GetResult();
        }
        private static void PumpIntegrationUntil(Func<bool> ready)
        {
            var watch = Stopwatch.StartNew();
            // The operation may contain several independently bounded native
            // notifications. Keep the shorter UI-return and heartbeat assertions;
            // allow their worker enough time to finish real Windows I/O.
            while (!ready()) {
                if (watch.ElapsedMilliseconds > 30000) throw new Exception("native integration fixture deadline");
                Application.DoEvents(); Thread.Sleep(10);
            }
            Application.DoEvents();
        }
        private static void IntegrationWait(ManualResetEventSlim release)
        {
            if (!release.Wait(15000)) throw new TimeoutException("native integration fixture release deadline");
        }
        private static void ApplyNativeIntegration(ProxyFeature feature, AppSettings configured)
        {
            if (feature == ProxyFeature.Cli) CliProxyEnvironmentService.ApplyUserEnvironment(configured.HttpProxyPort);
            else {
                string message;
                if (!SystemProxyService.Apply(configured, out message)) throw new IOException(message);
            }
        }
        private static string FinishIntegrationWarning(UpdateAwareTrayApplicationContext context, Action release)
        {
            var contents = new StringBuilder(); bool seen = false;
            using (var timer = new System.Windows.Forms.Timer { Interval = 30 }) {
                timer.Tick += delegate {
                    var window = FindWindow("#32770", "ProGo"); if (window == IntPtr.Zero) return;
                    timer.Stop(); seen = true;
                    EnumChildWindows(window, delegate(IntPtr child, IntPtr data) {
                        var text = new StringBuilder(4096); GetWindowText(child, text, text.Capacity); contents.AppendLine(text.ToString()); return true;
                    }, IntPtr.Zero);
                    PostMessage(window, 0x0010, IntPtr.Zero, IntPtr.Zero);
                };
                timer.Start(); release(); WaitIntegration(context);
            }
            Check(seen, "asynchronous native integration failure displays a readable owner-thread warning");
            return contents.ToString();
        }

        private static void IntegrationWindowsFailureAndRetry(SettingsService settings)
        {
            int workerThread = 0, ownerThread = Thread.CurrentThread.ManagedThreadId, callbacks = 0, wrongThread = 0;
            bool deny = true;
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var fixture = new IntegrationContextFixture(settings, delegate {
                workerThread = Thread.CurrentThread.ManagedThreadId; entered.Set(); IntegrationWait(release);
                return SystemProxyService.RestoreOwned(delegate(RegistryKey key, string name, WindowsProxyValue value) {
                    if (deny && name == "ProxyServer") throw new UnauthorizedAccessException("fixture denied native restore");
                    SystemProxyService.WriteValue(key, name, value);
                });
            }))
            using (var heartbeat = new System.Windows.Forms.Timer { Interval = 15 }) {
                var context = fixture.Context; var bridge = fixture.Bridge; string message;
                Check(bridge.Start(out message) && SystemProxyService.Apply(settings.Current, out message), "async Windows fixture starts the actual owned route and shared listener");
                var consumers = (AppProxyConsumers)Field(context, "appConsumers"); consumers.Observe();
                WatchIntegrationCommands(context, delegate { callbacks++; if (Thread.CurrentThread.ManagedThreadId != ownerThread) wrongThread++; });
                WatchIntegrationControls(fixture.Main, delegate { if (Thread.CurrentThread.ManagedThreadId != ownerThread) wrongThread++; });
                var commands = IntegrationCommands(context); int ticks = 0;
                heartbeat.Tick += delegate { ticks++; }; heartbeat.Start();
                var previousContext = SynchronizationContext.Current;
                try {
                    SynchronizationContext.SetSynchronizationContext(null);
                    var watch = Stopwatch.StartNew(); commands[AppCommand.DisableWindows].PerformClick();
                    Check(watch.ElapsedMilliseconds < 1000 && context.IntegrationPending && !context.IntegrationWork.IsCompleted,
                        "Windows off returns immediately and exposes the complete pending mutation with null synchronization context");
                    PumpUntil(() => entered.IsSet && ticks >= 3);
                    Check(workerThread != ownerThread && bridge.IsRunning && commands[AppCommand.Settings].Enabled && commands[AppCommand.StopDesktop].Enabled,
                        "blocked real registry restore runs off the owner UI and retains its listener while settings inspection and Stop remain usable");
                    var savedSettings = File.ReadAllBytes(AppPaths.SettingsPath); int saveCalls = 0;
                    using (var form = new SshProfilesSettingsForm(settings)) {
                        form.CommandState = () => (AppCommandState)typeof(UpdateAwareTrayApplicationContext).GetMethod("GetCommandState", PrivateInstance).Invoke(context, null);
                        form.SaveRequested = delegate { saveCalls++; return null; };
                        form.Show(); Application.DoEvents();
                        var host = (TextBox)Field(form, "host"); host.Text = "unsaved-async.example.org";
                        form.RefreshCommandAvailability();
                        var save = (Button)form.AcceptButton;
                        Call(form, "Save", save, EventArgs.Empty);
                        Check(!save.Enabled && saveCalls == 0 && savedSettings.SequenceEqual(File.ReadAllBytes(AppPaths.SettingsPath)) &&
                            host.Text == "unsaved-async.example.org" && ((Label)Field(form, "saveError")).Text.Contains("Дождитесь"),
                            "an open native settings form refuses forced Save during mutation, preserves staged edits and leaves persisted preferences untouched");
                        form.Close();
                    }
                    // Change an independent field while the native worker is blocked.
                    using (var key = Registry.CurrentUser.OpenSubKey(ProxyRegistryPath, true))
                        key.SetValue("ProxyOverride", "later-async.example.org", RegistryValueKind.String);
                    var external = SystemProxyService.ReadCurrent().Values["ProxyOverride"];
                    var text = FinishIntegrationWarning(context, release.Set);
                    Check(text.Contains("Windows") && text.Contains("выключить") && !text.Contains("fixture denied native restore"),
                        "async Windows denial explains the retry action without leaking the injected internal exception");
                    consumers.ReleaseIfUnused();
                    Check(consumers.WindowsCleanupPending && bridge.IsRunning && File.Exists(SystemProxyService.BackupPath) && !context.IntegrationPending,
                        "denied asynchronous Windows cleanup retains retry ownership and its listener after settlement");
                    Check(SystemProxyService.ReadCurrent().Values["ProxyOverride"].Matches(external), "async owned Windows cleanup preserves an intervening external bypass value");
                    // A later external endpoint must survive retry of the denied field.
                    using (var key = Registry.CurrentUser.OpenSubKey(ProxyRegistryPath, true))
                        key.SetValue("ProxyServer", "later-async.example.org:9090", RegistryValueKind.String);
                    var externalServer = SystemProxyService.ReadCurrent().Values["ProxyServer"];
                    deny = false; commands[AppCommand.DisableWindows].PerformClick(); WaitIntegration(context);
                    SettleConsumers(consumers, bridge);
                    Check(!consumers.WindowsCleanupPending && !File.Exists(SystemProxyService.BackupPath) && !bridge.IsRunning &&
                        SystemProxyService.ReadCurrent().Values["ProxyServer"].Matches(externalServer),
                        "async Windows retry preserves a later external endpoint, clears only owned cleanup and releases the last listener");
                    Check(callbacks > 0 && wrongThread == 0 && commands[AppCommand.Settings].Enabled && ticks >= 3,
                        "null-context Windows completion updates native command state only on its owner thread and restores settings availability");
                } finally { release.Set(); SynchronizationContext.SetSynchronizationContext(previousContext); }
            }
        }

        private static void IntegrationCliNotificationFailureAndRetry(SettingsService settings, Dictionary<string, string> environment)
        {
            bool deny = true; int ownerThread = Thread.CurrentThread.ManagedThreadId, notificationThread = 0, callbacks = 0, wrongThread = 0;
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var fixture = new IntegrationContextFixture(settings, null, delegate {
                CliProxyEnvironmentService.ClearUserEnvironmentIfOwned(delegate(RegistryKey key, string name, WindowsProxyValue value) {
                    if (deny && (name == "AutoDetect" || name == "ProxyOverride")) throw new UnauthorizedAccessException("fixture correction denied");
                    SystemProxyService.WriteValue(key, name, value);
                }, delegate {
                    notificationThread = Thread.CurrentThread.ManagedThreadId; entered.Set(); IntegrationWait(release);
                    CliProxyEnvironmentService.BroadcastEnvironmentChange();
                    if (deny) using (var key = Registry.CurrentUser.OpenSubKey(ProxyRegistryPath, true)) {
                        key.DeleteValue("AutoDetect", false);
                        key.SetValue("ProxyOverride", "%USERPROFILE%;external.example.org", RegistryValueKind.String);
                    }
                });
            }))
            using (var heartbeat = new System.Windows.Forms.Timer { Interval = 15 }) {
                var context = fixture.Context; var bridge = fixture.Bridge; string message;
                Check(bridge.Start(out message), "async CLI fixture starts its real shared listener");
                CliProxyEnvironmentService.ApplyUserEnvironment(bridge.Port); SeedExternalTypedRoute();
                var external = SystemProxyService.ReadCurrent();
                var consumers = (AppProxyConsumers)Field(context, "appConsumers"); consumers.Observe();
                WatchIntegrationCommands(context, delegate { callbacks++; if (Thread.CurrentThread.ManagedThreadId != ownerThread) wrongThread++; });
                WatchIntegrationControls(fixture.Main, delegate { if (Thread.CurrentThread.ManagedThreadId != ownerThread) wrongThread++; });
                int ticks = 0; heartbeat.Tick += delegate { ticks++; }; heartbeat.Start();
                var previousContext = SynchronizationContext.Current;
                try {
                    SynchronizationContext.SetSynchronizationContext(null);
                    var watch = Stopwatch.StartNew(); Call(context, "ExecuteCommand", AppCommand.StopCli);
                    Check(watch.ElapsedMilliseconds < 1000 && context.IntegrationPending, "CLI off returns before real environment setters and a blocked native notification finish");
                    PumpIntegrationUntil(() => entered.IsSet && ticks >= 3);
                    consumers.ReleaseIfUnused();
                    Check(notificationThread != ownerThread && bridge.IsRunning && !context.IntegrationWork.IsCompleted,
                        "CLI notification stays off the UI and the consumer lease keeps its listener after environment endpoints have been removed");
                    var text = FinishIntegrationWarning(context, release.Set);
                    Check(text.Contains("терминал") && !text.Contains("fixture correction denied"), "async CLI correction failure displays a human-readable retry warning");
                    consumers.ReleaseIfUnused();
                    Check(consumers.CliCleanupPending && bridge.IsRunning && File.Exists(CliProxyEnvironmentService.BackupPath) &&
                        CliProxyEnvironmentService.Names.All(n => Environment.GetEnvironmentVariable(n, EnvironmentVariableTarget.User) == environment[n]),
                        "async denied CLI typed corrections keep journal/listener even after actual environment restoration");
                    using (var key = Registry.CurrentUser.OpenSubKey(ProxyRegistryPath, true))
                        key.SetValue("ProxyOverride", "intervening-async.example.org", RegistryValueKind.String);
                    var intervening = SystemProxyService.ReadCurrent().Values["ProxyOverride"];
                    deny = false; Call(context, "ExecuteCommand", AppCommand.StopCli); WaitIntegration(context);
                    SettleConsumers(consumers, bridge); var restored = SystemProxyService.ReadCurrent();
                    Check(!consumers.CliCleanupPending && !bridge.IsRunning && !File.Exists(CliProxyEnvironmentService.BackupPath) &&
                        restored.Values["AutoDetect"].Matches(external.Values["AutoDetect"]) && restored.Values["ProxyOverride"].Matches(intervening),
                        "async CLI retry settles actual corrections, preserves a later external edit and releases its retained listener");
                    Check(callbacks > 0 && wrongThread == 0 && ticks >= 3, "null-context CLI result updates command state only on the live UI owner");
                } finally { release.Set(); SynchronizationContext.SetSynchronizationContext(previousContext); }
            }
        }

        private static void IntegrationEnableThenOff(SettingsService settings, ProxyFeature feature)
        {
            int active = 0, overlap = 0, applies = 0, restores = 0, ownerThread = Thread.CurrentThread.ManagedThreadId, workerThread = 0;
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var fixture = new IntegrationContextFixture(settings,
                delegate { int live = Interlocked.Increment(ref active); if (live != 1) Interlocked.Exchange(ref overlap, 1); Interlocked.Increment(ref restores);
                    try { return SystemProxyService.RestoreOwned(); } finally { Interlocked.Decrement(ref active); } },
                delegate { int live = Interlocked.Increment(ref active); if (live != 1) Interlocked.Exchange(ref overlap, 1); Interlocked.Increment(ref restores);
                    try { CliProxyEnvironmentService.ClearUserEnvironmentIfOwned(); } finally { Interlocked.Decrement(ref active); } },
                delegate(ProxyFeature selected, AppSettings captured) {
                    int live = Interlocked.Increment(ref active); if (live != 1) Interlocked.Exchange(ref overlap, 1);
                    try {
                        Interlocked.Increment(ref applies); workerThread = Thread.CurrentThread.ManagedThreadId; entered.Set(); IntegrationWait(release);
                        ApplyNativeIntegration(selected, captured);
                    } finally { Interlocked.Decrement(ref active); }
                }))
            using (var heartbeat = new System.Windows.Forms.Timer { Interval = 15 }) {
                var context = fixture.Context; int ticks = 0;
                heartbeat.Tick += delegate { ticks++; }; heartbeat.Start();
                var beforeWindows = SystemProxyService.ReadCurrent();
                var beforeEnvironment = CliProxyEnvironmentService.Names.ToDictionary(n => n, n => Environment.GetEnvironmentVariable(n, EnvironmentVariableTarget.User));
                var on = feature == ProxyFeature.Cli ? AppCommand.StartCli : AppCommand.EnableWindows;
                var off = feature == ProxyFeature.Cli ? AppCommand.StopCli : AppCommand.DisableWindows;
                var previousContext = SynchronizationContext.Current;
                try {
                    SynchronizationContext.SetSynchronizationContext(null); Call(context, "ExecuteCommand", on);
                    PumpUntil(() => entered.IsSet && ticks >= 3); var enabling = context.IntegrationWork;
                    Check(workerThread != ownerThread && context.IntegrationPending && fixture.Bridge.IsRunning && context.PendingRouteCount == 1,
                        "blocked native enable runs off the owner and retains one route intent and listener: " + feature);
                    var watch = Stopwatch.StartNew(); Call(context, "ExecuteCommand", off); var disabling = context.IntegrationWork;
                    Check(watch.ElapsedMilliseconds < 1000 && context.PendingRouteCount == 0 && !Object.ReferenceEquals(enabling, disabling) &&
                        !disabling.IsCompleted && Volatile.Read(ref restores) == 0,
                        "manual off cancels its pending intent immediately and queues cleanup behind the active writer: " + feature);
                    var consumers = (AppProxyConsumers)Field(context, "appConsumers"); consumers.ReleaseIfUnused();
                    Check(fixture.Bridge.IsRunning && context.IntegrationPending && ticks >= 3, "queued off preserves the listener lease while the original native write is blocked: " + feature);
                    release.Set(); PumpIntegrationUntil(() => enabling.IsCompleted && disabling.IsCompleted && !context.IntegrationPending);
                    enabling.GetAwaiter().GetResult(); disabling.GetAwaiter().GetResult(); SettleConsumers(consumers, fixture.Bridge);
                    Check(Volatile.Read(ref overlap) == 0 && applies == 1 && restores == 1 && context.PendingRouteCount == 0 && !fixture.Bridge.IsRunning &&
                        !File.Exists(SystemProxyService.BackupPath) && !File.Exists(CliProxyEnvironmentService.BackupPath),
                        "one mutation gate orders enable then off exactly once without a late enable or leaked listener: " + feature);
                    Check(CliProxyEnvironmentService.Names.All(n => Environment.GetEnvironmentVariable(n, EnvironmentVariableTarget.User) == beforeEnvironment[n]) &&
                        SystemProxyService.FieldNames.All(n => SystemProxyService.ReadCurrent().Values[n].Matches(beforeWindows.Values[n])),
                        "ordered enable/off restores actual pre-operation registry kinds and user environment: " + feature);
                } finally { release.Set(); SynchronizationContext.SetSynchronizationContext(previousContext); }
            }
        }

        private static void IntegrationCliApplyCorrectionFailureAndRetry(SettingsService settings, Dictionary<string, string> environment)
        {
            bool deny = true; int ownerThread = Thread.CurrentThread.ManagedThreadId, notificationThread = 0, wrongThread = 0;
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var fixture = new IntegrationContextFixture(settings, null, null, delegate(ProxyFeature feature, AppSettings captured) {
                if (feature != ProxyFeature.Cli) throw new Exception("fixture expects the independent CLI feature");
                CliProxyEnvironmentService.ApplyUserEnvironment(captured.HttpProxyPort, delegate(RegistryKey key, string name, WindowsProxyValue value) {
                    if (deny && (name == "AutoDetect" || name == "ProxyOverride")) throw new UnauthorizedAccessException("fixture apply correction denied");
                    SystemProxyService.WriteValue(key, name, value);
                }, delegate {
                    notificationThread = Thread.CurrentThread.ManagedThreadId; entered.Set(); IntegrationWait(release);
                    CliProxyEnvironmentService.BroadcastEnvironmentChange();
                    if (deny) using (var key = Registry.CurrentUser.OpenSubKey(ProxyRegistryPath, true)) {
                        key.DeleteValue("AutoDetect", false);
                        key.SetValue("ProxyOverride", "%USERPROFILE%;external.example.org", RegistryValueKind.String);
                    }
                });
            }))
            using (var heartbeat = new System.Windows.Forms.Timer { Interval = 15 }) {
                SeedExternalTypedRoute(); var external = SystemProxyService.ReadCurrent();
                var context = fixture.Context; var bridge = fixture.Bridge; int ticks = 0;
                WatchIntegrationControls(fixture.Main, delegate { if (Thread.CurrentThread.ManagedThreadId != ownerThread) wrongThread++; });
                heartbeat.Tick += delegate { ticks++; }; heartbeat.Start();
                var previousContext = SynchronizationContext.Current;
                try {
                    SynchronizationContext.SetSynchronizationContext(null);
                    var watch = Stopwatch.StartNew(); Call(context, "EnableFeature", ProxyFeature.Cli);
                    Check(watch.ElapsedMilliseconds < 1000 && context.IntegrationPending, "CLI enable returns before actual setters and delayed typed-value notification");
                    PumpIntegrationUntil(() => entered.IsSet && ticks >= 3);
                    Check(notificationThread != ownerThread && bridge.IsRunning && !context.IntegrationWork.IsCompleted,
                        "actual CLI apply notification runs off the UI while heartbeat and its shared listener remain live");
                    var warning = FinishIntegrationWarning(context, release.Set);
                    var consumers = (AppProxyConsumers)Field(context, "appConsumers"); consumers.ReleaseIfUnused();
                    var journal = new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<Dictionary<string, string>>(File.ReadAllText(CliProxyEnvironmentService.BackupPath));
                    Check(warning.Contains("терминал") && !warning.Contains("fixture apply correction denied") && consumers.CliCleanupPending &&
                        bridge.IsRunning && CliProxyEnvironmentService.IsAppliedToUserEnvironment(bridge.Port) && journal.ContainsKey("ProGoPendingWindowsCorrections"),
                        "denied CLI enable correction reports incomplete setup and durably retains actual environment ownership, corrections and listener");
                    using (var key = Registry.CurrentUser.OpenSubKey(ProxyRegistryPath, true))
                        key.SetValue("ProxyOverride", "external-before-reenable.example.org", RegistryValueKind.String);
                    var intervening = SystemProxyService.ReadCurrent().Values["ProxyOverride"];
                    deny = false; Call(context, "EnableFeature", ProxyFeature.Cli); WaitIntegration(context);
                    var restored = SystemProxyService.ReadCurrent();
                    journal = new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<Dictionary<string, string>>(File.ReadAllText(CliProxyEnvironmentService.BackupPath));
                    Check(!consumers.CliCleanupPending && bridge.IsRunning && !journal.ContainsKey("ProGoPendingWindowsCorrections") &&
                        restored.Values["AutoDetect"].Matches(external.Values["AutoDetect"]) && restored.Values["ProxyOverride"].Matches(intervening) &&
                        CliProxyEnvironmentService.IsAppliedToUserEnvironment(bridge.Port),
                        "explicit CLI re-enable retries matching typed corrections, preserves a later external bypass and keeps successful environment ownership");
                    Call(context, "ExecuteCommand", AppCommand.StopCli); WaitIntegration(context); SettleConsumers(consumers, bridge);
                    Check(!bridge.IsRunning && !File.Exists(CliProxyEnvironmentService.BackupPath) && !File.Exists(SystemProxyService.BackupPath) &&
                        CliProxyEnvironmentService.Names.All(n => Environment.GetEnvironmentVariable(n, EnvironmentVariableTarget.User) == environment[n]) && wrongThread == 0,
                        "off after repaired CLI enable restores original user environment and releases only its listener without owner-thread violations");
                } finally { release.Set(); SynchronizationContext.SetSynchronizationContext(previousContext); }
            }
        }

        private static void IntegrationShutdownWaits(SettingsService settings)
        {
            int calls = 0, workerThread = 0, ownerThread = Thread.CurrentThread.ManagedThreadId;
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var fixture = new IntegrationContextFixture(settings, delegate {
                Interlocked.Increment(ref calls); workerThread = Thread.CurrentThread.ManagedThreadId; entered.Set(); IntegrationWait(release);
                return SystemProxyService.RestoreOwned();
            }))
            using (var heartbeat = new System.Windows.Forms.Timer { Interval = 15 }) {
                string message; var context = fixture.Context;
                Check(fixture.Bridge.Start(out message) && SystemProxyService.Apply(settings.Current, out message), "shutdown mutation fixture owns the real Windows route");
                int ticks = 0; heartbeat.Tick += delegate { ticks++; }; heartbeat.Start();
                var previousContext = SynchronizationContext.Current;
                try {
                    SynchronizationContext.SetSynchronizationContext(null);
                    var watch = Stopwatch.StartNew(); var shutdown = context.RequestShutdownAsync();
                    Check(watch.ElapsedMilliseconds < 1000 && !shutdown.IsCompleted, "shutdown returns a pending task before blocked native integration cleanup");
                    PumpUntil(() => entered.IsSet && ticks >= 3);
                    Check(workerThread != ownerThread && !shutdown.IsCompleted && context.IntegrationPending && fixture.Bridge.IsRunning &&
                        !((bool)Field(context, "shutdownPrepared")) && ((NotifyIcon)Field(context, "tray")).Visible,
                        "shutdown keeps its UI heartbeat, tray and shared listener while native cleanup is unfinished");
                    Check(Object.ReferenceEquals(shutdown, context.RequestShutdownAsync()) && Volatile.Read(ref calls) == 1,
                        "duplicate shutdown shares one pending mutation instead of starting competing cleanup");
                    release.Set(); PumpIntegrationUntil(() => shutdown.IsCompleted); Check(shutdown.Result && !context.IntegrationPending &&
                        !fixture.Bridge.IsRunning && !File.Exists(SystemProxyService.BackupPath),
                        "shutdown is prepared only after the actual native journal and listener have settled");
                } finally { release.Set(); SynchronizationContext.SetSynchronizationContext(previousContext); }
            }
        }

        private static void IntegrationDisposedCompletion(SettingsService settings)
        {
            IntegrationDisposedCompletion(settings, false, false);
            IntegrationDisposedCompletion(settings, true, false);
            IntegrationDisposedCompletion(settings, false, true);
        }
        private static void IntegrationDisposedCompletion(SettingsService settings, bool failedRestore, bool nativeFinishedBeforeClose)
        {
            int calls = 0, ownerThread = Thread.CurrentThread.ManagedThreadId, wrongThread = 0, callbacks = 0;
            string scenario = failedRestore ? "failed restore" : nativeFinishedBeforeClose ? "finished native enable awaiting owner completion" : "blocked native enable";
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var nativeFinished = new ManualResetEventSlim())
            using (var fixture = new IntegrationContextFixture(settings, delegate {
                Interlocked.Increment(ref calls); entered.Set(); IntegrationWait(release);
                var result = SystemProxyService.RestoreOwned(delegate(RegistryKey key, string name, WindowsProxyValue value) {
                    if (name == "ProxyServer") throw new UnauthorizedAccessException("fixture late denied restore");
                    SystemProxyService.WriteValue(key, name, value);
                });
                nativeFinished.Set(); return result;
            }, null, delegate(ProxyFeature feature, AppSettings configured) {
                Interlocked.Increment(ref calls); entered.Set(); IntegrationWait(release); ApplyNativeIntegration(feature, configured); nativeFinished.Set();
            })) {
                var context = fixture.Context; string message;
                if (failedRestore)
                    Check(fixture.Bridge.Start(out message) && SystemProxyService.Apply(settings.Current, out message), "dispose failed-restore fixture owns its real listener and registry route");
                WatchIntegrationCommands(context, delegate { callbacks++; if (Thread.CurrentThread.ManagedThreadId != ownerThread) wrongThread++; });
                WatchIntegrationControls(fixture.Main, delegate { callbacks++; if (Thread.CurrentThread.ManagedThreadId != ownerThread) wrongThread++; });
                var previousContext = SynchronizationContext.Current;
                try {
                    SynchronizationContext.SetSynchronizationContext(null);
                    Call(context, "ExecuteCommand", failedRestore ? AppCommand.DisableWindows : AppCommand.EnableWindows);
                    PumpUntil(() => entered.IsSet); var operation = context.IntegrationWork;
                    if (nativeFinishedBeforeClose) {
                        release.Set();
                        // Intentionally withhold DoEvents while the actual native
                        // delegate returns and owner completion is still pending.
                        Check(nativeFinished.Wait(15000) && !operation.IsCompleted && context.IntegrationPending && File.Exists(SystemProxyService.BackupPath),
                            "actual native enable finishes with its ownership journal while the unpumped owner completion remains pending");
                    }
                    var watch = Stopwatch.StartNew();
                    if (nativeFinishedBeforeClose) fixture.Bridge.Dispose();
                    context.Dispose();
                    if (!nativeFinishedBeforeClose) fixture.Bridge.Dispose();
                    Check(watch.ElapsedMilliseconds < 1000 && (nativeFinishedBeforeClose || !operation.IsCompleted) && fixture.Main.IsDisposed && fixture.Bridge.IsRunning &&
                        Object.ReferenceEquals(operation, context.IntegrationWork) && Volatile.Read(ref calls) == 1,
                        "forced disposal closes UI immediately, retains the leased listener and starts no new native cleanup: " + scenario);
                    int afterDispose = callbacks; release.Set();
                    Check(operation.Wait(15000), "disposed integration operation settles without any surviving UI message pump: " + scenario);
                    Check(callbacks == afterDispose && wrongThread == 0 && File.Exists(SystemProxyService.BackupPath) && fixture.Bridge.IsRunning && Volatile.Read(ref calls) == 1,
                        "late successful enable or failed restore preserves its ownership journal and listener without closed UI callbacks: " + scenario);
                    context.Dispose(); fixture.Bridge.Dispose();
                    Check(Volatile.Read(ref calls) == 1 && !fixture.Bridge.IsRunning, "explicit owner disposal releases the settled listener without another native mutation: " + scenario);
                    Check(SystemProxyService.RestoreOwned().Completed && !File.Exists(SystemProxyService.BackupPath),
                        "explicit fixture cleanup can recover the emergency teardown journal without changing the production disposal contract: " + scenario);
                } finally { release.Set(); SynchronizationContext.SetSynchronizationContext(previousContext); }
            }
        }

        private static void IntegrationQueuedOffDisposed(SettingsService settings)
        {
            int nativeCalls = 0, changes = 0, ownerThread = Thread.CurrentThread.ManagedThreadId, wrongThread = 0;
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var fixture = new IntegrationContextFixture(settings,
                delegate { Interlocked.Increment(ref nativeCalls); return SystemProxyService.RestoreOwned(); }, null,
                delegate(ProxyFeature feature, AppSettings captured) {
                    Interlocked.Increment(ref nativeCalls); entered.Set(); IntegrationWait(release); ApplyNativeIntegration(feature, captured);
                })) {
                var context = fixture.Context;
                WatchIntegrationControls(fixture.Main, delegate { changes++; if (Thread.CurrentThread.ManagedThreadId != ownerThread) wrongThread++; });
                var previousContext = SynchronizationContext.Current;
                try {
                    SynchronizationContext.SetSynchronizationContext(null); Call(context, "ExecuteCommand", AppCommand.EnableWindows);
                    PumpUntil(() => entered.IsSet); var active = context.IntegrationWork;
                    Call(context, "ExecuteCommand", AppCommand.DisableWindows); var queued = context.IntegrationWork;
                    Check(!Object.ReferenceEquals(active, queued) && !active.IsCompleted && !queued.IsCompleted && context.PendingRouteCount == 0,
                        "queued off cancels the route intent while the actual enable writer still owns the mutation gate");
                    context.Dispose(); fixture.Bridge.Dispose(); int afterDispose = changes;
                    // Release only the owner's gate wait, not the native transaction.
                    Check(queued.Wait(5000) && !active.IsCompleted && !release.IsSet && Volatile.Read(ref nativeCalls) == 1 && fixture.Bridge.IsRunning,
                        "owner disposal settles queued off without a UI pump or waiting for the active writer and starts no second native transaction");
                    release.Set();
                    Check(active.Wait(15000) && File.Exists(SystemProxyService.BackupPath) && fixture.Bridge.IsRunning &&
                        Volatile.Read(ref nativeCalls) == 1 && changes == afterDispose && wrongThread == 0 && !context.IntegrationPending,
                        "the existing native enable settles after queued cancellation with its emergency journal/listener retained and no late UI mutation");
                    fixture.Bridge.Dispose();
                    Check(!fixture.Bridge.IsRunning && SystemProxyService.RestoreOwned().Completed,
                        "explicit owner cleanup releases the settled queued-disposal fixture without reviving its cancelled off request");
                } finally { release.Set(); SynchronizationContext.SetSynchronizationContext(previousContext); }
            }
        }

        private static int PendingIntegrationUi(UpdateAwareTrayApplicationContext context)
        {
            var gate = Field(context, "uiCompletionGate");
            lock (gate) return ((HashSet<Action>)Field(context, "uiCompletions")).Count;
        }
        private static void IntegrationQueuedShutdownDisposed(SettingsService settings)
        {
            int restoreCalls = 0, changes = 0, ownerThread = Thread.CurrentThread.ManagedThreadId, wrongThread = 0;
            using (var nativeEntered = new ManualResetEventSlim())
            using (var nativeRelease = new ManualResetEventSlim())
            using (var processEntered = new ManualResetEventSlim())
            using (var processRelease = new ManualResetEventSlim())
            using (var fixture = new IntegrationContextFixture(settings, delegate {
                Interlocked.Increment(ref restoreCalls); nativeEntered.Set(); IntegrationWait(nativeRelease); return SystemProxyService.RestoreOwned();
            })) {
                var context = fixture.Context; string message;
                Check(fixture.Bridge.Start(out message) && SystemProxyService.Apply(settings.Current, out message),
                    "queued shutdown fixture starts its actual owned route before exercising final UI completion");
                var processGate = Field(fixture.Proxy, "gate");
                var held = Task.Run(delegate { lock (processGate) {
                    processEntered.Set();
                    if (!processRelease.Wait(45000)) throw new TimeoutException("queued shutdown process gate deadline");
                } });
                var previousContext = SynchronizationContext.Current;
                try {
                    Check(processEntered.Wait(5000), "queued shutdown fixture holds real process ownership while native settings can finish");
                    WatchIntegrationControls(fixture.Main, delegate { changes++; if (Thread.CurrentThread.ManagedThreadId != ownerThread) wrongThread++; });
                    SynchronizationContext.SetSynchronizationContext(null); var shutdown = context.RequestShutdownAsync();
                    PumpUntil(() => nativeEntered.IsSet); nativeRelease.Set();
                    PumpIntegrationUntil(() => !context.IntegrationPending && fixture.Proxy.IsStopping);
                    var native = context.IntegrationWork;
                    Check(!shutdown.IsCompleted && !((bool)Field(context, "shutdownPrepared")) && !File.Exists(SystemProxyService.BackupPath) && !fixture.Bridge.IsRunning,
                        "actual native cleanup and listener stop finish before shutdown can publish its final UI result");
                    // Let process cleanup finish, then withhold the UI pump until the
                    // real final dispatcher callback is demonstrably queued.
                    processRelease.Set(); Check(held.Wait(5000), "queued shutdown releases actual process ownership without pumping owner completion");
                    Check(SpinWait.SpinUntil(() => native.IsCompleted && PendingIntegrationUi(context) > 0, 5000) && !shutdown.IsCompleted &&
                        !((bool)Field(context, "shutdownPrepared")),
                        "shutdown remains pending with a real posted final owner callback after all native work has completed");
                    context.Dispose(); int afterDispose = changes;
                    Check(shutdown.Wait(5000) && !shutdown.Result && native.IsCompleted && PendingIntegrationUi(context) == 0 &&
                        changes == afterDispose && wrongThread == 0 && Volatile.Read(ref restoreCalls) == 1,
                        "disposing the owner settles a lost queued shutdown callback as false without UI pumping, extra native cleanup or late control changes");
                } finally {
                    nativeRelease.Set(); processRelease.Set();
                    if (!held.Wait(15000)) throw new Exception("queued shutdown process gate did not settle");
                    SynchronizationContext.SetSynchronizationContext(previousContext);
                }
            }
        }

        private static void IntegrationStopThenOwnerReconnect(SettingsService settings)
        {
            var previousSettings = settings.Current.Clone();
            var reserve = Occupy(0); int socksPort = Number(reserve); reserve.Stop();
            var configured = previousSettings.Clone(); configured.SshProfile = "ready";
            configured.SocksHost = "127.0.0.1"; configured.SocksPort = socksPort;
            configured.AutoRestartSocks = configured.AutoSwitchSshProfile = false;
            settings.Save(configured);
            string executable = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "SocksRecoveryTests.exe");
            try {
                using (var nativeEntered = new ManualResetEventSlim())
                using (var nativeRelease = new ManualResetEventSlim())
                using (var processEntered = new ManualResetEventSlim())
                using (var processRelease = new ManualResetEventSlim())
                using (var fixture = new IntegrationContextFixture(settings, delegate {
                    nativeEntered.Set(); IntegrationWait(nativeRelease); return SystemProxyService.RestoreOwned();
                }, null, null, executable)) {
                    var context = fixture.Context; var proxy = fixture.Proxy; string message;
                    var start = proxy.StartTunnelAsync(CancellationToken.None); PumpIntegrationUntil(() => start.IsCompleted);
                    Check(start.Result && proxy.CurrentPid.HasValue, "integration stop ordering starts an actual owned SOCKS child");
                    int priorPid = proxy.CurrentPid.Value;
                    Check(fixture.Bridge.Start(out message) && SystemProxyService.Apply(settings.Current, out message),
                        "integration stop ordering owns its actual Windows route and listener");
                    var processGate = Field(proxy, "gate");
                    var held = Task.Run(delegate { lock (processGate) {
                        processEntered.Set();
                        if (!processRelease.Wait(45000)) throw new TimeoutException("integration stop ordering process gate deadline");
                    } });
                    bool armed = false, reconnectRequested = false, stoppedBeforePublish = false;
                    int ownerThread = Thread.CurrentThread.ManagedThreadId, callbackThread = 0;
                    WatchIntegrationCommands(context, delegate {
                        if (!armed || context.IntegrationPending) return;
                        armed = false; callbackThread = Thread.CurrentThread.ManagedThreadId;
                        stoppedBeforePublish = !proxy.ConnectionRequested && proxy.IsStopping;
                        // A real owner command runs in the exact pending-release
                        // callback that formerly preceded the worker's stale Stop.
                        Call(context, "ExecuteCommand", AppCommand.Reconnect);
                        reconnectRequested = context.PendingRouteCount == 1 && proxy.ConnectionRequested;
                    });
                    var previousContext = SynchronizationContext.Current;
                    try {
                        Check(processEntered.Wait(5000), "integration stop ordering holds real process ownership during owner intent publication");
                        SynchronizationContext.SetSynchronizationContext(null);
                        Call(context, "ExecuteCommand", AppCommand.StopDesktop); var stop = context.IntegrationWork;
                        PumpIntegrationUntil(() => nativeEntered.IsSet); armed = true; nativeRelease.Set();
                        PumpIntegrationUntil(() => reconnectRequested);
                        Check(stoppedBeforePublish && callbackThread == ownerThread && proxy.IsStopping && !stop.IsCompleted && proxy.ConnectionRequested,
                            "desktop Stop accepts SSH intent on the owner before enabling commands and tracks blocked cleanup while a newer reconnect remains requested");
                        processRelease.Set(); Check(held.Wait(5000), "integration stop ordering releases its actual process gate");
                        PumpIntegrationUntil(() => stop.IsCompleted && context.PendingRouteCount == 0 && !proxy.IsConnecting && !proxy.IsStopping);
                        stop.GetAwaiter().GetResult();
                        Check(proxy.ConnectionRequested && proxy.CurrentPid.HasValue && proxy.CurrentPid.Value != priorPid &&
                            ConnectionHealthMonitor.CheckSocks(settings.Current, CancellationToken.None) && !fixture.Bridge.IsRunning && !File.Exists(SystemProxyService.BackupPath),
                            "settled desktop Stop cannot cancel the newer owner reconnect, which replaces the real child and serves SOCKS after integration cleanup");
                    } finally {
                        nativeRelease.Set(); processRelease.Set();
                        if (!held.Wait(15000)) throw new Exception("integration stop ordering process gate did not settle");
                        SynchronizationContext.SetSynchronizationContext(previousContext);
                    }
                }
            } finally { settings.Save(previousSettings); }
        }
    }
}

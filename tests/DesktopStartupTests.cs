using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr FindWindow(string cls, string title);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr window, StringBuilder text, int count);
        private delegate bool VisitWindow(IntPtr window, IntPtr data);
        [DllImport("user32.dll")] private static extern bool EnumChildWindows(IntPtr window, VisitWindow visit, IntPtr data);
        [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr window, uint message, IntPtr wparam, IntPtr lparam);
        private static Timer WatchStartupErrors()
        {
            var timer = new Timer { Interval = 200 };
            timer.Tick += delegate {
                var dialog = FindWindow("#32770", "Подключение ProGo");
                if (dialog == IntPtr.Zero) return;
                EnumChildWindows(dialog, delegate(IntPtr window, IntPtr data) {
                    var text = new StringBuilder(2048); GetWindowText(window, text, text.Capacity);
                    if (text.Length > 0) Console.WriteLine("Unexpected startup dialog: " + text);
                    return true;
                }, IntPtr.Zero);
                PostMessage(dialog, 0x0010, IntPtr.Zero, IntPtr.Zero);
            };
            timer.Start(); return timer;
        }
        private static void CommandAvailabilityWorkflow(SettingsService settings)
        {
            var source = new[] { AppCommand.StartCli };
            var waiting = new AppCommandState(source, true, false); source[0] = AppCommand.EnableWindows;
            Check(waiting.IsPending(AppCommand.StartCli) && !waiting.IsPending(AppCommand.EnableWindows), "command state owns an immutable pending snapshot");
            Check(!AppCommands.CanExecute(AppCommand.StartCli, waiting) && !AppCommands.CanExecute(AppCommand.Connect, waiting) &&
                AppCommands.CanExecute(AppCommand.EnableWindows, waiting) && AppCommands.CanExecute(AppCommand.Reconnect, waiting),
                "waiting blocks duplicate/plain connect but permits independent mode and explicit reconnect");
            Check(new[] { AppCommand.StopCli, AppCommand.DisableWindows, AppCommand.StopDesktop, AppCommand.StopPhone, AppCommand.StopAll, AppCommand.Settings }
                .All(c => AppCommands.CanExecute(c, waiting)), "waiting never disables cancellation or settings");
            var stopping = new AppCommandState(new AppCommand[0], false, true);
            Check(Enum.GetValues(typeof(AppCommand)).Cast<AppCommand>().All(c => !AppCommands.CanExecute(c, stopping)), "prepared shutdown blocks every catalogued operation");
            var background = new AppCommandState(new AppCommand[0], true, false);
            Check(!AppCommands.CanExecute(AppCommand.Connect, background) && AppCommands.CanExecute(AppCommand.StartCli, background),
                "manual CLI can join background startup without plain Connect replacing it");
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") return;
            var original = settings.Current.Clone();
            var environment = CliProxyEnvironmentService.Names.ToDictionary(n => n, n => Environment.GetEnvironmentVariable(n, EnvironmentVariableTarget.User));
            var release = Path.Combine(Path.GetTempPath(), "progo-command-ready-" + Guid.NewGuid().ToString("N"));
            var oldRelease = Environment.GetEnvironmentVariable("PROGO_TEST_SSH_RELEASE");
            var reserve = Occupy(0); int port = Number(reserve); reserve.Stop();
            Environment.SetEnvironmentVariable("PROGO_TEST_SSH_RELEASE", release);
            try {
                var config = original.Clone(); config.SocksHost = "127.0.0.1"; config.SocksPort = port; config.SshProfile = "slow";
                config.AutoRestartSocks = false; config.AutoSwitchSshProfile = false; config.AutoCliProxy = false; config.AutoSystemProxy = false;
                config.TestEndpoint = "http://127.0.0.1:1/"; settings.Save(config);
                foreach (var name in CliProxyEnvironmentService.Names) Environment.SetEnvironmentVariable(name, null, EnvironmentVariableTarget.User);
                var executable = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "SocksRecoveryTests.exe");
                using (var proxy = new ProxyService(() => settings.Current, s => settings.Save(s), executable, () => DateTime.UtcNow, false))
                using (var bridge = new CliProxyBridgeService(settings))
                using (var relay = new Ikev2RelayService())
                using (var home = new HomeVpnService(relay))
                using (var clipboard = new ClipboardService(settings))
                using (var context = new UpdateAwareTrayApplicationContext(settings, proxy, bridge, home, clipboard, false)) {
                    context.RequestShowStatus(); var main = (MainWindow)Field(context, "mainWindow");
                    var tray = (NotifyIcon)Field(context, "tray");
                    var menu = MenuItems(tray.ContextMenuStrip.Items).Where(i => i.Tag is AppCommand).ToDictionary(i => (AppCommand)i.Tag);
                    ((Button)Field(main, "cliToggle")).PerformClick(); PumpUntil(() => proxy.CurrentPid.HasValue);
                    Check(!menu[AppCommand.StartCli].Enabled && !menu[AppCommand.Connect].Enabled && menu[AppCommand.EnableWindows].Enabled && menu[AppCommand.StopCli].Enabled &&
                        !((Button)Field(main, "cliToggle")).Enabled && !((Button)Field(main, "connect")).Enabled,
                        "dashboard and tray agree while CLI waits for explicitly gated SOCKS");
                    var pid = proxy.CurrentPid;
                    Call(context, "Execute", "connect"); Call(context, "Execute", "codex-on");
                    Check(context.PendingRouteCount == 1 && proxy.CurrentPid == pid, "dispatcher rejects stale plain Connect and duplicate alias without replacing pending route");
                    var bytes = File.ReadAllBytes(AppPaths.SettingsPath);
                    CheckModal(context, main, "settings", "settings", delegate(Form dialog) {
                        var form = (SshProfilesSettingsForm)dialog;
                        var manual = (System.Collections.Generic.Dictionary<AppCommand, Button>)Field(form, "manualCommands");
                        Check(!manual[AppCommand.StartCli].Enabled && manual[AppCommand.StopCli].Enabled && manual[AppCommand.EnableWindows].Enabled,
                            "newly opened real settings reflects pending CLI and leaves cancellation available");
                        var host = (TextBox)Field(form, "host"); host.Text = "pending.example.org";
                        manual[AppCommand.EnableWindows].PerformClick();
                        Check(context.PendingRouteCount == 2 && !manual[AppCommand.EnableWindows].Enabled && !menu[AppCommand.EnableWindows].Enabled,
                            "settings and tray refresh immediately when independent Windows joins pending route");
                        manual[AppCommand.StopCli].PerformClick();
                        Check(context.PendingRouteCount == 1 && proxy.CurrentPid.HasValue,
                            "manual CLI off immediately cancels only its pending intent while retaining the shared Windows startup");
                        WaitIntegration(context);
                        Check(context.PendingRouteCount == 1 && manual[AppCommand.StartCli].Enabled && menu[AppCommand.StartCli].Enabled &&
                            !manual[AppCommand.EnableWindows].Enabled && proxy.CurrentPid == pid,
                            "settings cancellation re-enables only CLI and preserves the shared Windows startup");
                        Check(host.Text == "pending.example.org" && bytes.SequenceEqual(File.ReadAllBytes(AppPaths.SettingsPath)),
                            "availability refresh and cancellation preserve unsaved fields and saved settings bytes");
                        Shot(form, "settings-command-pending");
                    });
                    Check(Field(context, "CommandStateChanged") == null, "closed modal settings releases command state subscription");
                    menu[AppCommand.DisableWindows].PerformClick(); WaitIntegration(context); PumpUntil(() => !proxy.IsConnecting);
                    Check(context.PendingRouteCount == 0 && !bridge.IsRunning && !SystemProxyService.IsOwned &&
                        !CliProxyEnvironmentService.IsAppliedToUserEnvironment(settings.Current.HttpProxyPort), "last pending mode cancels from tray without a late proxy application");
                    File.WriteAllText(release, "ready");
                    menu[AppCommand.StartCli].PerformClick(); PumpUntil(() => context.PendingRouteCount == 0);
                    Check(bridge.IsRunning && menu[AppCommand.StartCli].Enabled && menu[AppCommand.Connect].Enabled &&
                        ((Button)Field(main, "cliToggle")).Enabled, "successful retry restores command availability on tray and dashboard");
                    int uiThread = System.Threading.Thread.CurrentThread.ManagedThreadId, lifecycleThread = uiThread;
                    var lifecycle = context.GetType().GetEvent("CommandStateChanged", PrivateInstance);
                    Action observeLifecycle = () => { lifecycleThread = System.Threading.Thread.CurrentThread.ManagedThreadId; };
                    lifecycle.GetAddMethod(true).Invoke(context, new object[] { observeLifecycle });
                    var oldContext = System.Threading.SynchronizationContext.Current;
                    System.Threading.Tasks.Task<bool> shutdown;
                    try {
                        System.Threading.SynchronizationContext.SetSynchronizationContext(null);
                        shutdown = context.RequestShutdownAsync();
                    } finally { System.Threading.SynchronizationContext.SetSynchronizationContext(oldContext); }
                    PumpUntil(() => shutdown.IsCompleted);
                    Check(lifecycleThread == uiThread, "shutdown completion returns to the persistent UI dispatcher after a modal context is removed");
                    lifecycle.GetRemoveMethod(true).Invoke(context, new object[] { observeLifecycle });
                    Check(shutdown.Result && menu.Values.All(i => !i.Enabled), "prepared shutdown disables catalogued tray actions");
                    context.CancelShutdown(); PumpUntil(() => menu[AppCommand.StartCli].Enabled);
                    Check(((Button)Field(main, "cliToggle")).Enabled, "cancelled shutdown restores dashboard and tray command availability");
                    main.Close();
                }
            } finally {
                Environment.SetEnvironmentVariable("PROGO_TEST_SSH_RELEASE", oldRelease);
                if (File.Exists(release)) File.Delete(release);
                settings.Save(original);
                foreach (var item in environment) Environment.SetEnvironmentVariable(item.Key, item.Value, EnvironmentVariableTarget.User);
            }
        }
        private static void SettingsSaveConnectionIntent(SettingsService settings)
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") return;
            var before = settings.Current.Clone();
            try {
                var reserve = Occupy(0); int firstPort = Number(reserve); reserve.Stop();
                var prefs = before.Clone(); prefs.SshProfile = "ready"; prefs.SocksHost = "127.0.0.1"; prefs.SocksPort = firstPort;
                prefs.AutoRestartSocks = false; prefs.AutoSwitchSshProfile = false; prefs.AutoStartSocks = false;
                prefs.AutoCliProxy = false; prefs.AutoSystemProxy = false; prefs.TestEndpoint = "http://127.0.0.1:1/"; settings.Save(prefs);
                string fixture = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "SocksRecoveryTests.exe");
                using (var proxy = new ProxyService(() => settings.Current, s => settings.Save(s), fixture, () => DateTime.UtcNow, false))
                using (var bridge = new CliProxyBridgeService(settings)) using (var relay = new Ikev2RelayService())
                using (var home = new HomeVpnService(relay)) using (var clipboard = new ClipboardService(settings))
                using (var context = new UpdateAwareTrayApplicationContext(settings, proxy, bridge, home, clipboard, false)) {
                    var start = proxy.StartTunnelAsync(System.Threading.CancellationToken.None); PumpUntil(() => start.IsCompleted);
                    Check(start.Result && proxy.CurrentPid.HasValue, "settings reconnect regression starts an actual owned SOCKS child");
                    int firstPid = proxy.CurrentPid.Value; context.RequestShowStatus(); var main = (MainWindow)Field(context, "mainWindow");
                    var gate = Field(proxy, "gate");
                    foreach (bool stopped in new[] { false, true }) {
                        if (stopped) { var stop = proxy.StopTunnelAsync(); PumpUntil(() => stop.IsCompleted); Check(stop.Result, "explicit stop settles the owned child before changing settings"); }
                        var next = Occupy(0); int nextPort = Number(next); next.Stop();
                        using (var entered = new System.Threading.ManualResetEventSlim()) using (var release = new System.Threading.ManualResetEventSlim()) {
                            var held = System.Threading.Tasks.Task.Run(delegate { lock (gate) { entered.Set(); release.Wait(5000); } });
                            PumpUntil(() => entered.IsSet);
                            try {
                                Check(!proxy.CurrentPid.HasValue && !proxy.IsConnecting && proxy.ConnectionRequested == !stopped,
                                    "busy process snapshot stays distinct from immediate user connection intent: " + stopped);
                                CheckModal(context, main, "settings", "settings", delegate(Form dialog) {
                                    ((NumericUpDown)Field(dialog, "port")).Value = nextPort;
                                    var watch = Stopwatch.StartNew(); ((Button)dialog.AcceptButton).PerformClick();
                                    Check(watch.ElapsedMilliseconds < 400 && dialog.DialogResult == DialogResult.OK && settings.Current.SocksPort == nextPort,
                                        "actual settings save completes while background ownership is busy: " + stopped);
                                });
                                Check(proxy.ConnectionRequested == !stopped, "settings preserves the original user connection request without waiting for a PID: " + stopped);
                            } finally { release.Set(); PumpUntil(() => held.IsCompleted); }
                        }
                        if (!stopped) {
                            PumpUntil(() => !proxy.IsStopping && !proxy.IsConnecting && proxy.CurrentPid.HasValue && proxy.IsListening());
                            Check(proxy.CurrentPid.Value != firstPid && settings.Current.SocksPort == nextPort &&
                                ConnectionHealthMonitor.CheckSocks(settings.Current, System.Threading.CancellationToken.None),
                                "saved SOCKS port replaces the old owned process despite its unavailable PID snapshot");
                            var old = prefs.Clone(); old.SocksPort = firstPort;
                            Check(!ConnectionHealthMonitor.CheckSocks(old, System.Threading.CancellationToken.None), "previous SOCKS port is released after settings reconnect");
                        } else {
                            PumpUntil(() => !proxy.IsStopping); int ticks = 0;
                            using (var timer = new System.Windows.Forms.Timer { Interval = 20 }) { timer.Tick += delegate { ticks++; }; timer.Start(); PumpUntil(() => ticks >= 3); }
                            Check(!proxy.ConnectionRequested && !proxy.IsConnecting && !proxy.CurrentPid.HasValue && !proxy.IsListening(),
                                "saving another endpoint after explicit stop does not revive a user-disabled connection");
                        }
                    }
                    main.Close();
                }
            } finally { settings.Save(before); }
        }
        private static void AsyncStopUiHeartbeat(SettingsService settings)
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") return;
            var before = settings.Current.Clone();
            var reserve = Occupy(0); int port = Number(reserve); reserve.Stop();
            try {
                var prefs = before.Clone(); prefs.SshProfile = "ready"; prefs.SocksHost = "127.0.0.1"; prefs.SocksPort = port;
                prefs.AutoRestartSocks = false; prefs.AutoSwitchSshProfile = false; prefs.AutoCliProxy = false; prefs.AutoSystemProxy = false;
                prefs.TestEndpoint = "http://127.0.0.1:1/"; settings.Save(prefs);
                string fixture = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "SocksRecoveryTests.exe");
                using (var proxy = new ProxyService(() => settings.Current, s => settings.Save(s), fixture, () => DateTime.UtcNow, false))
                using (var bridge = new CliProxyBridgeService(settings))
                using (var relay = new Ikev2RelayService()) using (var home = new HomeVpnService(relay))
                using (var clipboard = new ClipboardService(settings))
                using (var context = new UpdateAwareTrayApplicationContext(settings, proxy, bridge, home, clipboard, false)) {
                    var start = proxy.StartTunnelAsync(System.Threading.CancellationToken.None); PumpUntil(() => start.IsCompleted);
                    Check(start.Result, "UI stop regression has a real owned loopback SSH process"); int pid = proxy.CurrentPid.Value;
                    context.RequestShowStatus(); var gate = Field(proxy, "gate");
                    using (var entered = new System.Threading.ManualResetEventSlim()) using (var release = new System.Threading.ManualResetEventSlim())
                    using (var timer = new System.Windows.Forms.Timer { Interval = 20 }) {
                        var held = System.Threading.Tasks.Task.Run(delegate { lock (gate) { entered.Set(); release.Wait(5000); } });
                        PumpUntil(() => entered.IsSet); int ticks = 0; timer.Tick += delegate { ticks++; }; timer.Start();
                        try {
                            var watch = Stopwatch.StartNew(); Call(context, "Execute", "restart");
                            Check(watch.ElapsedMilliseconds < 400 && context.PendingRouteCount == 1 && proxy.IsStopping, "dashboard reconnect queues owned cleanup without waiting on background ownership");
                            PumpUntil(() => ticks >= 3);
                            Check(context.PendingRouteCount == 1 && proxy.IsStopping, "native UI timer continues while reconnect cleanup is blocked");
                        } finally { release.Set(); PumpUntil(() => held.IsCompleted); }
                        PumpUntil(() => context.PendingRouteCount == 0);
                        Check(proxy.CurrentPid.HasValue && proxy.CurrentPid.Value != pid, "UI reconnect starts one fresh owned child after old cleanup");
                        entered.Reset(); release.Reset();
                        var heldAgain = System.Threading.Tasks.Task.Run(delegate { lock (gate) { entered.Set(); release.Wait(5000); } });
                        PumpUntil(() => entered.IsSet);
                        int ownerThread = System.Threading.Thread.CurrentThread.ManagedThreadId, completionThread = 0;
                        var lifecycle = context.GetType().GetEvent("CommandStateChanged", PrivateInstance);
                        Action observeLifecycle = () => { completionThread = System.Threading.Thread.CurrentThread.ManagedThreadId; };
                        lifecycle.GetAddMethod(true).Invoke(context, new object[] { observeLifecycle });
                        var oldContext = System.Threading.SynchronizationContext.Current;
                        System.Threading.Tasks.Task<bool> shutdown;
                        try {
                            System.Threading.SynchronizationContext.SetSynchronizationContext(null);
                            shutdown = context.RequestShutdownAsync();
                        } finally { System.Threading.SynchronizationContext.SetSynchronizationContext(oldContext); }
                        int beforeTicks = ticks;
                        try { PumpUntil(() => ticks >= beforeTicks + 3); Check(!shutdown.IsCompleted && proxy.IsStopping, "shutdown awaits owned cleanup while native UI heartbeat remains active"); }
                        finally { release.Set(); PumpUntil(() => heldAgain.IsCompleted); }
                        PumpUntil(() => shutdown.IsCompleted);
                        Check(shutdown.Result && !proxy.CurrentPid.HasValue && !proxy.IsStopping, "shutdown is prepared only after its actual owned SSH child exits");
                        Check(completionThread == ownerThread, "shutdown UI state remains owned by the native UI thread without a captured synchronization context");
                        lifecycle.GetRemoveMethod(true).Invoke(context, new object[] { observeLifecycle });
                    }
                }
            } finally { settings.Save(before); }
        }
        private static void AsyncCliStartup(SettingsService settings)
        {
            SettingsSaveConnectionIntent(settings);
            AsyncStopUiHeartbeat(settings);
            var login = SshInteractiveLogin.CreateStartInfo("my-vps");
            var command = Encoding.Unicode.GetString(Convert.FromBase64String(login.Arguments.Split(' ').Last()));
            Check(login.UseShellExecute && login.WindowStyle == ProcessWindowStyle.Normal && login.Arguments.Contains("-NoExit") && command.Contains("StrictHostKeyChecking=ask") &&
                command.Contains("BatchMode=no") && command.Contains("ClearAllForwardings=yes"), "explicit first login uses visible SSH, asks for host verification and creates no tunnel");
            foreach (var target in new[] { "-V", "my-vps -o ProxyCommand=anything", "host\"", "host\n" }) {
                bool rejected = false;
                try { SshInteractiveLogin.CreateStartInfo(target); } catch (ArgumentException) { rejected = true; }
                Check(rejected, "interactive SSH entry rejects command-line parameters and malformed target");
            }
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") return;
            var original = settings.Current.Clone();
            var environment = CliProxyEnvironmentService.Names.ToDictionary(n => n, n => Environment.GetEnvironmentVariable(n, EnvironmentVariableTarget.User));
            var reserve = Occupy(0); int socksPort = Number(reserve); reserve.Stop();
            try {
                var config = original.Clone(); config.SocksHost = "127.0.0.1"; config.SocksPort = socksPort; config.SshProfile = "slow";
                config.AutoRestartSocks = false; config.AutoSwitchSshProfile = false; config.AutoCliProxy = false; config.AutoSystemProxy = false;
                config.TestEndpoint = "http://127.0.0.1:1/"; settings.Save(config);
                foreach (var name in CliProxyEnvironmentService.Names) Environment.SetEnvironmentVariable(name, null, EnvironmentVariableTarget.User);
                var executable = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "SocksRecoveryTests.exe");
                using (var proxy = new ProxyService(() => settings.Current, s => settings.Save(s), executable, () => DateTime.UtcNow, false))
                using (var bridge = new CliProxyBridgeService(settings))
                using (var relay = new Ikev2RelayService())
                using (var home = new HomeVpnService(relay))
                using (var clipboard = new ClipboardService(settings))
                using (var context = new UpdateAwareTrayApplicationContext(settings, proxy, bridge, home, clipboard, false)) {
                    context.RequestShowStatus(); var main = (MainWindow)Field(context, "mainWindow");
                    var button = (Button)Field(main, "cliToggle"); var watch = Stopwatch.StartNew();
                    button.PerformClick();
                    Check(watch.ElapsedMilliseconds < 400 && context.PendingRouteCount == 1 && button.Text == "Подключаем CLI…" && !button.Enabled,
                        "ordinary Start CLI returns immediately and exposes pending state on one click");
                    Call(context, "Execute", "terminal-on"); Call(context, "Execute", "codex-on");
                    Check(context.PendingRouteCount == 1, "repeated CLI aliases share the one pending user request");
                    var pendingPreferences = File.ReadAllBytes(AppPaths.SettingsPath);
                    Call(context, "Execute", "future-unknown"); Call(context, "Execute", (object)null);
                    Check(context.PendingRouteCount == 1 && pendingPreferences.SequenceEqual(File.ReadAllBytes(AppPaths.SettingsPath)) &&
                        !CliProxyEnvironmentService.IsAppliedToUserEnvironment(settings.Current.HttpProxyPort),
                        "unknown commands neither cancel pending CLI nor apply settings or environment");
                    int ticks = 0;
                    using (var timer = new System.Windows.Forms.Timer { Interval = 20 }) {
                        timer.Tick += delegate { ticks++; }; timer.Start(); PumpUntil(() => ticks >= 3);
                        Check(!CliProxyEnvironmentService.IsAppliedToUserEnvironment(settings.Current.HttpProxyPort), "delayed SSH does not apply environment before SOCKS readiness");
                        Shot(main, "main-cli-connecting");
                        PumpUntil(() => context.PendingRouteCount == 0);
                        Check(ticks >= 3 && bridge.IsRunning && CliProxyEnvironmentService.IsAppliedToUserEnvironment(bridge.Port) && button.Text == "Выключить CLI",
                            "one click finishes slow SSH, bridge and ordinary CLI while the UI keeps responding");
                    }
                    var activePid = proxy.CurrentPid;
                    Call(context, "Execute", "0"); Call(context, "Execute", "cli-start;stop");
                    Check(context.PendingRouteCount == 0 && proxy.CurrentPid == activePid && bridge.IsRunning &&
                        CliProxyEnvironmentService.IsAppliedToUserEnvironment(bridge.Port),
                        "unknown commands leave active ordinary CLI and its connection intact");
                    var pid = proxy.CurrentPid;
                    Call(context, "Execute", "cli-start"); PumpUntil(() => context.PendingRouteCount == 0);
                    Check(proxy.CurrentPid == pid, "ready CLI request keeps the existing owned SSH process");
                    Call(context, "Execute", "stop");
                    WaitIntegration(context);
                    Call(context, "Execute", "cli-start"); Call(context, "Execute", "windows-on");
                    PumpUntil(() => proxy.CurrentPid.HasValue);
                    Check(context.PendingRouteCount == 2, "CLI and Windows can await one shared SSH startup");
                    Call(context, "Execute", "stop-all"); WaitIntegration(context); PumpUntil(() => !proxy.IsConnecting);
                    Check(context.PendingRouteCount == 0 && !proxy.CurrentPid.HasValue && !bridge.IsRunning &&
                        !CliProxyEnvironmentService.IsAppliedToUserEnvironment(settings.Current.HttpProxyPort) && !SystemProxyService.IsOwned,
                        "full stop cancels both waiting actions before any late environment or Windows write");
                    Call(context, "Execute", "cli-start"); Call(context, "Execute", "windows-on");
                    PumpUntil(() => context.PendingRouteCount == 0);
                    Check(proxy.CurrentPid.HasValue && CliProxyEnvironmentService.IsAppliedToUserEnvironment(bridge.Port) && SystemProxyService.IsApplied(settings.Current),
                        "a fresh shared attempt after cancellation applies both requested modes once ready");
                    Call(context, "Execute", "stop"); WaitIntegration(context); Call(context, "Execute", "cli-start");
                    PumpUntil(() => proxy.CurrentPid.HasValue); Call(context, "Execute", "cli-off"); WaitIntegration(context); PumpUntil(() => !proxy.IsConnecting);
                    Check(!CliProxyEnvironmentService.IsAppliedToUserEnvironment(settings.Current.HttpProxyPort) && context.PendingRouteCount == 0,
                        "manual CLI off cancels its pending intent without a delayed on");
                    using (var form = new SshProfilesSettingsForm(settings, SettingsSection.Connections)) {
                        form.Show(); Application.DoEvents();
                        var firstLogin = Descendants(form).OfType<Button>().Single(b => b.Text == "Первый вход");
                        Check(firstLogin.Visible && firstLogin.Right <= firstLogin.Parent.ClientSize.Width, "first-login action fits alongside the existing profile controls");
                        Shot(form, "settings-first-login"); form.Close();
                    }
                    main.Close();
                }
            } finally {
                settings.Save(original);
                foreach (var item in environment) Environment.SetEnvironmentVariable(item.Key, item.Value, EnvironmentVariableTarget.User);
            }
        }
    }
}

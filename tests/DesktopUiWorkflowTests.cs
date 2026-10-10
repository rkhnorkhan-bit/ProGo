using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        private static void CommandCatalogContract()
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (AppCommand command in Enum.GetValues(typeof(AppCommand))) {
                var definition = AppCommands.Get(command); AppCommandDefinition resolved;
                Check(!String.IsNullOrWhiteSpace(definition.Label) && !String.IsNullOrWhiteSpace(definition.CompactLabel) &&
                    !String.IsNullOrWhiteSpace(definition.ManualLabel) && !String.IsNullOrWhiteSpace(definition.Effect), "every command has catalogued presentation: " + command);
                Check(definition.Command == command && !String.IsNullOrWhiteSpace(definition.LegacyId) && ids.Add(definition.LegacyId) &&
                    AppCommands.TryResolve(definition.LegacyId, out resolved) && Object.ReferenceEquals(definition, resolved),
                    "every typed command has one unique round-trippable compatibility ID: " + command);
            }
            foreach (var pair in new[] { new[] { "terminal-on", "cli-start" }, new[] { "codex-on", "cli-start" },
                new[] { "terminal-off", "cli-off" }, new[] { "codex-off", "cli-off" } }) {
                AppCommandDefinition alias, canonical;
                Check(AppCommands.TryResolve(pair[0], out alias) && AppCommands.TryResolve(pair[1], out canonical) && Object.ReferenceEquals(alias, canonical),
                    "legacy alias shares the canonical operation identity: " + pair[0]);
            }
            foreach (string invalid in new[] { null, "", " ", "CONNECT", " connect", "0", "Connect", "cli-start;stop", "future-unknown" }) {
                AppCommandDefinition rejected;
                Check(!AppCommands.TryResolve(invalid, out rejected) && rejected == null, "unknown or nonexact command cannot default to a connection");
            }
        }
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private static object Field(object target, string name) { return target.GetType().GetField(name, PrivateInstance).GetValue(target); }
        private static void Call(object target, string name, params object[] args) { target.GetType().GetMethod(name, PrivateInstance).Invoke(target, args); }
        private static void PumpUntil(Func<bool> ready)
        {
            var watch = Stopwatch.StartNew();
            while (!ready()) { if (watch.ElapsedMilliseconds > 5000) throw new Exception("UI fixture deadline"); Application.DoEvents(); Thread.Sleep(10); }
            Application.DoEvents();
        }
        private static void AnswerFixtureSocks(TcpListener listener)
        {
            // The simulated remote server must not compete with the client probe
            // for thread-pool workers while that probe waits synchronously for it.
            new Thread(delegate() {
                try {
                    while (true) {
                        var client = listener.AcceptTcpClient();
                        new Thread(delegate() {
                            using (client) {
                                try {
                                    var stream = client.GetStream(); stream.ReadTimeout = 1000;
                                    var greet = HealthRead(stream, 2); HealthRead(stream, greet[1]);
                                    if (greet[0] == 5) stream.Write(new byte[] { 5, 0 }, 0, 2);
                                } catch (System.IO.IOException) { } catch (ObjectDisposedException) { }
                            }
                        }) { IsBackground = true }.Start();
                    }
                } catch (SocketException) { } catch (ObjectDisposedException) { }
            }) { IsBackground = true }.Start();
        }
        private static void UiControls(SettingsService settings)
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") { Console.WriteLine("SKIP: UI action state requires isolated CI"); return; }
            var original = settings.Current.Clone();
            var environment = CliProxyEnvironmentService.Names.ToDictionary(n => n, n => Environment.GetEnvironmentVariable(n, EnvironmentVariableTarget.User));
            var listener = Occupy(0);
            AnswerFixtureSocks(listener);
            try {
                var configured = original.Clone(); configured.SocksHost = "127.0.0.1"; configured.SocksPort = Number(listener);
                configured.AutoCliProxy = true; configured.AutoSystemProxy = true; configured.TrayCloseExplained = false;
                settings.Save(configured);
                Check(ConnectionHealthMonitor.CheckSocks(configured, CancellationToken.None), "UI fixture speaks SOCKS before creating the application");
                using (var proxy = new ProxyService(() => settings.Current, s => settings.Save(s), "unused-test-ssh", () => DateTime.UtcNow, false))
                using (var bridge = new CliProxyBridgeService(settings))
                using (var relay = new Ikev2RelayService())
                using (var home = new HomeVpnService(relay))
                using (var clipboard = new ClipboardService(settings))
                using (var context = new UpdateAwareTrayApplicationContext(settings, proxy, bridge, home, clipboard, false)) {
                    context.RequestShowStatus(); Application.DoEvents();
                    var main = (MainWindow)Field(context, "mainWindow");
                    var nav = (Dictionary<string, Button>)Field(main, "navigation");
                    var plan = (AutomationPlan)Field(context, "automation");
                    var tray = (NotifyIcon)Field(context, "tray");
                    var commandItems = MenuItems(tray.ContextMenuStrip.Items).Where(i => i.Tag is AppCommand).ToArray();
                    var expectedTray = new[] { AppCommand.Connect, AppCommand.StopDesktop, AppCommand.StopAll, AppCommand.Reconnect,
                        AppCommand.CheckRoute, AppCommand.Diagnostics, AppCommand.EnableWindows, AppCommand.DisableWindows,
                        AppCommand.StartCli, AppCommand.StopCli, AppCommand.OpenTerminal, AppCommand.CreateCodexShortcut,
                        AppCommand.RemoveCodexShortcut, AppCommand.OpenCodex, AppCommand.Phone, AppCommand.StopPhone,
                        AppCommand.Vault, AppCommand.Settings, AppCommand.Help, AppCommand.Update,
                        AppCommand.ShowMain, AppCommand.CreateBackup, AppCommand.RestoreBackup, AppCommand.OpenBackups, AppCommand.CleanupBackups,
                        AppCommand.ExportHomeVpn, AppCommand.ImportHomeVpn,
                        AppCommand.ExportDiagnostics, AppCommand.OpenAppLog, AppCommand.OpenUpdateLog, AppCommand.OpenFolder, AppCommand.Exit };
                    Check(commandItems.Select(i => (AppCommand)i.Tag).OrderBy(c => c).SequenceEqual(expectedTray.OrderBy(c => c)),
                        "tray exposes every migrated operation exactly once without an alias duplicate");
                    foreach (var item in commandItems)
                        Check(item.Text == AppCommands.Get((AppCommand)item.Tag).Label &&
                            item.ToolTipText == AppCommands.Get((AppCommand)item.Tag).Effect && item.AccessibleDescription == item.ToolTipText && item.Owner.ShowItemToolTips, "tray caption comes from its operation: " + item.Tag);
                    Check(MenuItems(tray.ContextMenuStrip.Items).Any(i => i.Text == "Отключить прокси на ПК") &&
                        MenuItems(tray.ContextMenuStrip.Items).Any(i => i.Text == "Остановить VPN для телефона") &&
                        MenuItems(tray.ContextMenuStrip.Items).Any(i => i.Text == "Остановить все подключения"), "tray exposes three explicit stop scopes");
                    CheckModal(context, main, "connections", "connections", delegate(Form form) {
                        Check(Descendants(form).OfType<TabControl>().Single().SelectedTab.Text == "Подключение", "Connections opens server settings");
                        Shot(form, "settings-connections-route");
                    });
                    CheckModal(context, main, "windows-settings", "settings", delegate(Form form) {
                        var option = Descendants(form).OfType<CheckBox>().Single(c => c.Text == "Включать прокси для приложений Windows");
                        var flow = (FlowLayoutPanel)option.Parent.Parent.Parent;
                        var bounds = flow.RectangleToClient(option.Parent.RectangleToScreen(option.Parent.ClientRectangle));
                        Shot(form, "settings-windows-route");
                        var controls = Descendants(option.Parent).Where(c => c is CheckBox || c is Button).ToArray();
                        Check(Descendants(form).OfType<TabControl>().Single().SelectedIndex == 0 && controls.All(c => {
                            var area = flow.RectangleToClient(c.RectangleToScreen(c.ClientRectangle));
                            return c.Visible && area.Top >= 0 && area.Bottom <= flow.ClientSize.Height;
                        }),
                            "Windows card route scrolls the actual Windows controls into view: " + bounds + " viewport=" + flow.ClientSize);
                    });
                    CheckModal(context, main, "settings", "settings", delegate(Form form) {
                        Check(Descendants(form).OfType<TabControl>().Single().SelectedTab.Text == "Автоматика", "Settings retains automation entry");
                    });
                    Check(ConnectionHealthMonitor.CheckSocks(settings.Current, CancellationToken.None), "UI fixture remains ready after closing settings dialogs");
                    ((Button)Field(main, "windowsToggle")).PerformClick(); Application.DoEvents();
                    PumpUntil(() => context.PendingRouteCount == 0);
                    Check(SystemProxyService.IsApplied(settings.Current) && ((Button)Field(main, "windowsToggle")).Text == "Выключить", "Windows card enables its own mode");
                    ((Button)Field(main, "windowsToggle")).PerformClick();
                    WaitIntegration(context);
                    Check(!SystemProxyService.IsOwned && ((Button)Field(main, "windowsToggle")).Text == "Включить", "Windows card restores with its symmetric off command");
                    ((Button)Field(main, "cliToggle")).PerformClick();
                    PumpUntil(() => context.PendingRouteCount == 0);
                    Check(CliProxyEnvironmentService.IsAppliedToUserEnvironment(settings.Current.HttpProxyPort) && ((Button)Field(main, "cliToggle")).Text == "Выключить CLI", "Start CLI exposes matching off on main");
                    Shot(main, "main-cli-enabled");
                    ((Button)Field(main, "cliToggle")).PerformClick();
                    WaitIntegration(context);
                    Check(((Button)Field(main, "cliToggle")).Text == "Запустить CLI" && !ApplyOnce(plan, ProxyFeature.Cli, true), "CLI off cancels automation and preserves Start CLI");
                    commandItems.Single(i => (AppCommand)i.Tag == AppCommand.StartCli).PerformClick();
                    PumpUntil(() => context.PendingRouteCount == 0); main.RefreshConnectionState();
                    Check(CliProxyEnvironmentService.IsAppliedToUserEnvironment(settings.Current.HttpProxyPort) &&
                        ((Button)Field(main, "cliToggle")).Text == "Выключить CLI", "typed tray Start CLI applies the ordinary mode and updates dashboard");
                    using (var form = new SshProfilesSettingsForm(settings)) {
                        form.ManualActionRequested += action => Call(context, "ExecuteCommand", action);
                        form.Show(); Application.DoEvents();
                        var option = (CheckBox)Field(form, "autoCli");
                        Descendants(option.Parent).OfType<Button>().Single(b => b.Text == "Выключить").PerformClick();
                        WaitIntegration(context);
                        form.Close();
                    }
                    main.RefreshConnectionState();
                    Check(!CliProxyEnvironmentService.IsAppliedToUserEnvironment(settings.Current.HttpProxyPort) &&
                        ((Button)Field(main, "cliToggle")).Text == "Запустить CLI", "typed settings Off reverses tray Start CLI without saving preferences");
                    ServiceCommandDialogs(context, main, commandItems);
                    CheckStopScopes(context, proxy, home, relay, plan, configured);
                    main.Close(); Application.DoEvents();
                    Check(settings.Current.TrayCloseExplained && tray.Visible, "first dashboard close explains tray without exiting");
                    var saved = System.IO.File.ReadAllText(AppPaths.SettingsPath);
                    context.RequestShowStatus(); ((MainWindow)Field(context, "mainWindow")).Close(); Application.DoEvents();
                    Check(System.IO.File.ReadAllText(AppPaths.SettingsPath) == saved, "subsequent closes do not rewrite the tray explanation");
                    Check(SettingsService.DeserializeSettings(saved).TrayCloseExplained && settings.Current.Clone().TrayCloseExplained &&
                        !SettingsService.DeserializeSettings("{}").TrayCloseExplained, "tray explanation survives settings round trip and defaults off for older files");
                    context.RequestShowStatus(); main = (MainWindow)Field(context, "mainWindow");
                    // The real route command uses a closed local port: no external curl request.
                    var stopped = settings.Current.Clone(); stopped.SocksPort = 1; settings.Save(stopped);
                    CheckModal(context, main, "route-check", "diagnostics", delegate(Form form) {
                        PumpUntil(() => ((Label)Field(form, "checkedAt")).Text != "Ещё не проверен");
                        Check(((Label)Field(form, "checkedAt")).Text != "Ещё не проверен", "route command performs first check without second click");
                        Shot(form, "diagnostics-route-command");
                    });
                    main.Close();
                    RouteTimes(settings, proxy);
                    var ownedMenu = tray.ContextMenuStrip; context.Dispose();
                    Check(ownedMenu.IsDisposed, "application teardown disposes its tray menu and theme observer");
                }
            }
            finally {
                listener.Stop(); settings.Save(original);
                foreach (var item in environment) Environment.SetEnvironmentVariable(item.Key, item.Value, EnvironmentVariableTarget.User);
            }
        }
        private static void ServiceCommandDialogs(UpdateAwareTrayApplicationContext context, MainWindow main, ToolStripItem[] items)
        {
            items.Single(i => (AppCommand)i.Tag == AppCommand.ShowMain).PerformClick();
            Check(Object.ReferenceEquals(main, Field(context, "mainWindow")), "catalogued ShowMain reuses the existing dashboard");
            Check(Descendants(main).OfType<Button>().Single(b => b.Text == "Запустить CLI").AccessibleDescription == AppCommands.Get(AppCommand.StartCli).Effect, "dashboard CLI describes its real ordinary-launch effect");
            var root = System.IO.Path.Combine(BackupService.BackupsRoot, "command-cancel-" + Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(root);
            var settingsBytes = System.IO.File.ReadAllBytes(AppPaths.SettingsPath);
            try {
                CloseServiceDialog(items.Single(i => (AppCommand)i.Tag == AppCommand.RestoreBackup), typeof(BackupPickerForm), delegate(Form form) {
                    Check(form.CancelButton != null, "catalogued restore opens the cancellable backup chooser before maintenance");
                });
                Check(!((bool)Field(context, "shutdownPrepared")) && ((NotifyIcon)Field(context, "tray")).Visible &&
                    settingsBytes.SequenceEqual(System.IO.File.ReadAllBytes(AppPaths.SettingsPath)) && System.IO.Directory.Exists(root),
                    "cancelled catalogued restore leaves settings, backup and application intact");
                CloseServiceDialog(items.Single(i => (AppCommand)i.Tag == AppCommand.ExportDiagnostics), typeof(DiagnosticPreviewForm), delegate(Form form) {
                    Check(form.AcceptButton == null && Descendants(form).OfType<TextBox>().Single().ReadOnly,
                        "catalogued diagnostic export opens review without implicit Enter export");
                });
                Check(settingsBytes.SequenceEqual(System.IO.File.ReadAllBytes(AppPaths.SettingsPath)), "closing catalogued report does not write preferences");
            } finally { System.IO.Directory.Delete(root, true); }
        }
        private static void CloseServiceDialog(ToolStripItem command, Type expected, Action<Form> inspect)
        {
            bool seen = false; Exception failure = null;
            using (var timer = new System.Windows.Forms.Timer { Interval = 150 }) {
                timer.Tick += delegate {
                    var form = Application.OpenForms.Cast<Form>().FirstOrDefault(f => expected.IsInstanceOfType(f));
                    if (form == null) return;
                    timer.Stop(); seen = true;
                    try { inspect(form); }
                    catch (Exception ex) { failure = ex; }
                    finally { form.DialogResult = DialogResult.Cancel; form.Close(); }
                };
                timer.Start(); command.PerformClick();
            }
            if (failure != null) throw failure;
            Check(seen, "real tray command dispatches its expected dialog: " + command.Tag);
        }
        private static void CheckModal(UpdateAwareTrayApplicationContext context, MainWindow main, string action, string selected, Action<Form> inspect)
        {
            Exception failure = null;
            using (var timer = new System.Windows.Forms.Timer { Interval = 200 }) {
                timer.Tick += delegate {
                    timer.Stop(); var form = Application.OpenForms.Cast<Form>().FirstOrDefault(f => f != main);
                    try {
                        if (form == null) throw new Exception("Expected modal route: " + action);
                        var nav = (Dictionary<string, Button>)Field(main, "navigation");
                        Check(nav[selected].Tag as string == "primary" && nav["home"].Tag == null, "navigation highlights active dialog " + action);
                        inspect(form);
                    } catch (Exception ex) { failure = ex; }
                    finally { if (form != null) form.Close(); }
                };
                timer.Start();
                var routes = (Dictionary<string, Button>)Field(main, "navigation");
                if (routes.ContainsKey(action)) routes[action].PerformClick();
                else if (action == "windows-settings") {
                    var link = Descendants(main).OfType<LinkLabel>().Single(l => l.Text == "Настройки");
                    typeof(LinkLabel).GetMethod("OnLinkClicked", PrivateInstance).Invoke(link, new object[] { new LinkLabelLinkClickedEventArgs(link.Links[0]) });
                }
                else Descendants(main).OfType<Button>().Single(b => b.Text == "Проверить маршрут").PerformClick();
            }
            if (failure != null) throw failure;
            Check(((Dictionary<string, Button>)Field(main, "navigation"))["home"].Tag as string == "primary", "navigation returns home after closing " + action);
        }
        private static void CheckStopScopes(UpdateAwareTrayApplicationContext context, ProxyService desktop, HomeVpnService home,
            Ikev2RelayService relay, AutomationPlan plan, AppSettings configured)
        {
            using (var phoneProxy = new ProxyService(delegate { return configured; }, delegate { }, "unused-test-ssh", () => DateTime.UtcNow, false)) {
                typeof(HomeVpnService).GetField("proxy", PrivateInstance).SetValue(home, phoneProxy);
                typeof(ProxyService).GetField("wanted", PrivateInstance).SetValue(phoneProxy, true);
                StartFixtureRelay(relay);
                plan.Update(null, configured);
                Call(context, "Execute", "stop"); WaitIntegration(context); PumpUntil(() => !desktop.IsStopping); desktop.PollRecovery();
                Check(relay.IsRunning && ReferenceEquals(Field(home, "proxy"), phoneProxy) && (bool)Field(phoneProxy, "wanted"), "PC stop preserves independently running phone relay and recovery");
                Check(!ApplyOnce(plan, ProxyFeature.Cli, true) && !ApplyOnce(plan, ProxyFeature.Windows, true) && !desktop.CurrentPid.HasValue,
                    "PC stop cancels both automatic proxy modes and recovery");
                plan.Update(null, configured); typeof(ProxyService).GetField("wanted", PrivateInstance).SetValue(desktop, true);
                Call(context, "Execute", "phone-stop");
                Check(!relay.IsRunning && Field(home, "proxy") == null && !(bool)Field(phoneProxy, "wanted") && (bool)Field(desktop, "wanted"), "phone stop cancels phone recovery without stopping desktop");
                Check(ApplyOnce(plan, ProxyFeature.Cli, true) && ApplyOnce(plan, ProxyFeature.Windows, true), "phone stop does not cancel desktop automatic actions");
                StartFixtureRelay(relay); plan.Update(null, configured);
                var main = (MainWindow)Field(context, "mainWindow");
                Descendants(main).OfType<Button>().Single(b => b.Text == "Остановить все\nподключения").PerformClick(); WaitIntegration(context); PumpUntil(() => !desktop.IsStopping); desktop.PollRecovery();
                Check(!relay.IsRunning && !(bool)Field(desktop, "wanted") && !ApplyOnce(plan, ProxyFeature.Cli, true) && !ApplyOnce(plan, ProxyFeature.Windows, true),
                    "full stop stops both channels and no timer revives them");
            }
        }
        private static void StartFixtureRelay(Ikev2RelayService relay)
        {
            var listener = Occupy(0);
            var server = Task.Run(async delegate {
                using (var client = await listener.AcceptTcpClientAsync()) {
                    var stream = client.GetStream();
                    await Ikev2RelayService.ReadExactAsync(stream, 3, CancellationToken.None);
                    var greet = new byte[] { 5, 0 }; await stream.WriteAsync(greet, 0, greet.Length);
                    await Ikev2RelayService.ReadExactAsync(stream, 10, CancellationToken.None);
                    var reply = new byte[] { 5, 0, 0, 1, 127, 0, 0, 1, 0, 0 }; await stream.WriteAsync(reply, 0, reply.Length);
                    var hello = await Ikev2RelayService.ReadExactAsync(stream, 6, CancellationToken.None);
                    await stream.WriteAsync(hello, 0, hello.Length);
                }
            });
            try { relay.StartAsync("127.0.0.1", Number(listener), 0, 0, 12345).GetAwaiter().GetResult(); server.GetAwaiter().GetResult(); }
            finally { listener.Stop(); }
        }
        private static void RouteTimes(SettingsService settings, ProxyService proxy)
        {
            int calls = 0;
            var clock = new DateTime(2020, 1, 1, 12, 0, 0);
            using (var form = new StatusForm(settings, proxy, false, delegate { Interlocked.Increment(ref calls); return "Fixture route answer"; }, () => clock)) {
                form.Show(); Application.DoEvents();
                var route = (Label)Field(form, "route"); var time = (Label)Field(form, "checkedAt"); var button = (Button)Field(form, "checkButton");
                Check(calls == 0 && route.Text == "Маршрут ещё не проверен" && time.Text == "Ещё не проверен", "diagnostics does not invent a first route check");
                button.PerformClick(); PumpUntil(() => button.Text == "Проверить маршрут" && calls == 1);
                Check(route.Text == "Fixture route answer" && time.Text.Contains("2020-01-01 12:00:00") && time.Text.Contains("SOCKS →"), "route result shows its source and actual completion time");
                string previous = time.Text; clock = clock.AddHours(1); Call(form, "RefreshState", false);
                Call(form, "QueuePingMeasure"); PumpUntil(() => ((Label)Field(form, "ping")).Text.Contains("SOCKS ·"));
                Check(time.Text == previous && ((Label)Field(form, "state")).Text.Contains("13:00:00"), "status and ping refresh leave route time unchanged");
                button.PerformClick(); PumpUntil(() => button.Text == "Проверить маршрут" && calls == 2);
                Check(time.Text.Contains("13:00:00"), "explicit repeat refreshes route timestamp");
                form.Size = form.MinimumSize; Application.DoEvents();
                Check(Descendants(form).OfType<Button>().All(b => b.Visible && b.Top >= 0 && b.Bottom <= b.Parent.ClientSize.Height),
                    "diagnostic commands remain visible at minimum window size");
                Shot(form, "diagnostics-timestamps"); form.Close();
            }
            using (var release = new ManualResetEventSlim(false))
            using (var entered = new ManualResetEventSlim(false))
            using (var finished = new ManualResetEventSlim(false))
            using (var form = new StatusForm(settings, proxy, true, delegate { Interlocked.Increment(ref calls); entered.Set(); release.Wait(5000); finished.Set(); throw new InvalidOperationException("Fixture route failure"); }, () => clock)) {
                form.Show(); PumpUntil(() => entered.IsSet);
                Call(form, "QueueRouteMeasure"); Call(form, "QueueRouteMeasure");
                Check(calls == 3 && ((Button)Field(form, "checkButton")).Text == "Отменить проверку", "route runs automatically once and suppresses concurrent checks");
                form.Close(); release.Set(); PumpUntil(() => finished.IsSet); Application.DoEvents();
                Check(form.IsDisposed, "closing diagnostics ignores an in-flight failure without reopening UI");
            }
        }
    }
}

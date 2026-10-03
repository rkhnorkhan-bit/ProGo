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
        [DllImport("user32.dll")] private static extern bool EnumChildWindows(IntPtr window, Func<IntPtr, IntPtr, bool> visit, IntPtr data);
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
        private static void AsyncCliStartup(SettingsService settings)
        {
            var login = SshInteractiveLogin.CreateStartInfo("my-vps");
            Check(login.UseShellExecute && login.WindowStyle == ProcessWindowStyle.Normal && login.Arguments.Contains("-NoExit") && login.Arguments.Contains("StrictHostKeyChecking=ask") &&
                login.Arguments.Contains("BatchMode=no") && login.Arguments.Contains("ClearAllForwardings=yes"), "explicit first login uses visible SSH, asks for host verification and creates no tunnel");
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
                    int ticks = 0;
                    using (var timer = new System.Windows.Forms.Timer { Interval = 20 }) {
                        timer.Tick += delegate { ticks++; }; timer.Start(); PumpUntil(() => ticks >= 3);
                        Check(!CliProxyEnvironmentService.IsAppliedToUserEnvironment(settings.Current.HttpProxyPort), "delayed SSH does not apply environment before SOCKS readiness");
                        Shot(main, "main-cli-connecting");
                        PumpUntil(() => context.PendingRouteCount == 0);
                        Check(ticks >= 3 && bridge.IsRunning && CliProxyEnvironmentService.IsAppliedToUserEnvironment(bridge.Port) && button.Text == "Выключить CLI",
                            "one click finishes slow SSH, bridge and ordinary CLI while the UI keeps responding");
                    }
                    var pid = proxy.CurrentPid;
                    Call(context, "Execute", "cli-start"); PumpUntil(() => context.PendingRouteCount == 0);
                    Check(proxy.CurrentPid == pid, "ready CLI request keeps the existing owned SSH process");
                    Call(context, "Execute", "stop");
                    Call(context, "Execute", "cli-start"); Call(context, "Execute", "windows-on");
                    PumpUntil(() => proxy.CurrentPid.HasValue);
                    Check(context.PendingRouteCount == 2, "CLI and Windows can await one shared SSH startup");
                    Call(context, "Execute", "stop-all"); PumpUntil(() => !proxy.IsConnecting);
                    Check(context.PendingRouteCount == 0 && !proxy.CurrentPid.HasValue && !bridge.IsRunning &&
                        !CliProxyEnvironmentService.IsAppliedToUserEnvironment(settings.Current.HttpProxyPort) && !SystemProxyService.IsOwned,
                        "full stop cancels both waiting actions before any late environment or Windows write");
                    Call(context, "Execute", "cli-start"); Call(context, "Execute", "windows-on");
                    PumpUntil(() => context.PendingRouteCount == 0);
                    Check(proxy.CurrentPid.HasValue && CliProxyEnvironmentService.IsAppliedToUserEnvironment(bridge.Port) && SystemProxyService.IsApplied(settings.Current),
                        "a fresh shared attempt after cancellation applies both requested modes once ready");
                    Call(context, "Execute", "stop"); Call(context, "Execute", "cli-start");
                    PumpUntil(() => proxy.CurrentPid.HasValue); Call(context, "Execute", "cli-off"); PumpUntil(() => !proxy.IsConnecting);
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

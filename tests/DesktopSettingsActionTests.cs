using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        private static void SettingsActionBoundaries(SettingsService settings)
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") { Console.WriteLine("SKIP: settings action fixture requires isolated CI"); return; }
            var original = settings.Current.Clone();
            var environment = CliProxyEnvironmentService.Names.ToDictionary(n => n, n => Environment.GetEnvironmentVariable(n, EnvironmentVariableTarget.User));
            var socks = Occupy(0);
            AnswerFixtureSocks(socks);
            try {
                var configured = original.Clone(); configured.SocksHost = "127.0.0.1"; configured.SocksPort = Number(socks);
                configured.AutoCliProxy = configured.AutoSystemProxy = configured.AutoStartSocks = false;
                configured.AutoHttpProxyPort = true; configured.SshProfile = "saved-fixture";
                settings.Save(configured);
                using (var proxy = new ProxyService(settings))
                using (var bridge = new CliProxyBridgeService(settings))
                using (var relay = new Ikev2RelayService())
                using (var home = new HomeVpnService(relay))
                using (var clipboard = new ClipboardService(settings))
                using (var context = new UpdateAwareTrayApplicationContext(settings, proxy, bridge, home, clipboard, false)) {
                    using (var form = new SshProfilesSettingsForm(settings)) {
                        form.CurrentProxyEndpoint = delegate { return bridge.ProxyUrl; };
                        form.ManualActionRequested += delegate(AppCommand action) { Call(context, "ExecuteCommand", action); };
                        form.Show(); Application.DoEvents();
                        var tabs = Descendants(form).OfType<TabControl>().Single();
                        Check(Descendants(form).OfType<Label>().Any(l => l.Text.Contains("«Отменить изменения» их не откатывает")) &&
                            Descendants(tabs.TabPages[0]).OfType<Label>().Count(l => l.Text == "Ручное управление · применяется сразу") == 3,
                            "settings distinguishes saved preferences from immediate commands before clicking");
                        var host = (TextBox)Field(form, "host"); var port = (NumericUpDown)Field(form, "port");
                        host.Text = "uncommitted.example.org"; port.Value = 19999;
                        ((TextBox)Field(form, "endpoint")).Text = "https://uncommitted.example.org/check";
                        var autoCli = (CheckBox)Field(form, "autoCli"); autoCli.Checked = true;
                        Descendants(autoCli.Parent).OfType<Button>().Single(b => b.Text == "Включить").PerformClick();
                        PumpUntil(() => context.PendingRouteCount == 0 && bridge.IsRunning);
                        Check(CliProxyEnvironmentService.IsAppliedToUserEnvironment(bridge.Port), "manual CLI on uses saved reachable SOCKS despite unsaved address/port");
                        Check(settings.Current.SocksHost == "127.0.0.1" && settings.Current.SocksPort == Number(socks) &&
                            !settings.Current.AutoCliProxy && settings.Current.TestEndpoint == configured.TestEndpoint,
                            "manual command does not commit pending preferences or fields");
                        PumpUntil(() => ((Label)Field(form, "currentAutomation")).Text.Contains(bridge.ProxyUrl));
                        Check(((Label)Field(form, "currentAutomation")).Text.Contains("saved-fixture") &&
                            ((Label)Field(form, "currentConnection")).Text.Contains("127.0.0.1:" + Number(socks)) &&
                            ((Label)Field(form, "currentDiagnostic")).Text.Contains(configured.TestEndpoint),
                            "effective labels show saved SSH, SOCKS, application endpoint and diagnostic URL");
                        Check(host.Text == "uncommitted.example.org" && port.Value == 19999 && autoCli.Checked,
                            "live effective-value refresh preserves all unsaved edits");
                        Shot(form, "settings-immediate-command");
                        form.Hide();
                        using (var cancelTimer = new Timer { Interval = 30 }) {
                            cancelTimer.Tick += delegate { cancelTimer.Stop(); ((Button)form.CancelButton).PerformClick(); };
                            cancelTimer.Start();
                            Check(form.ShowDialog() == DialogResult.Cancel, "the real modal Cancel button closes without saving");
                        }
                        Check(!form.Visible && bridge.IsRunning && CliProxyEnvironmentService.IsAppliedToUserEnvironment(bridge.Port),
                            "cancel discards edits while deliberately retaining the already-applied manual CLI action");
                        Check(!settings.Current.AutoCliProxy && settings.Current.SocksHost == "127.0.0.1", "cancel never persists staged settings");
                        Call(context, "Execute", "cli-off");
                        WaitIntegration(context);
                        Check(CliProxyEnvironmentService.Names.All(n => Environment.GetEnvironmentVariable(n, EnvironmentVariableTarget.User) == environment[n]),
                            "explicit manual off restores environment after cancelled settings dialog");
                    }
                }
                using (var form = new SshProfilesSettingsForm(settings)) {
                    form.Show(); Application.DoEvents();
                    var tabs = Descendants(form).OfType<TabControl>().Single(); tabs.SelectedIndex = 4;
                    var automatic = (CheckBox)Field(form, "autoHttpPort"); var port = (NumericUpDown)Field(form, "httpPort");
                    var pick = (Button)Field(form, "pickPortButton");
                    automatic.Checked = false; port.Value = 31881;
                    var bytes = File.ReadAllBytes(AppPaths.SettingsPath);
                    pick.PerformClick(); Application.DoEvents();
                    Check(pick.Text == "Отменить подбор" && !port.Enabled && (bool)Field(form, "pickFreePort"), "one-time port selection advertises an in-place cancel command");
                    Shot(form, "settings-port-pick-pending");
                    pick.PerformClick(); Application.DoEvents();
                    Check(port.Enabled && port.Value == 31881 && !automatic.Checked && !(bool)Field(form, "pickFreePort"), "cancel port selection restores manual edit and exact entered value");
                    Check(pick.Text == "Подобрать свободный" && bytes.SequenceEqual(File.ReadAllBytes(AppPaths.SettingsPath)), "pick/cancel does not bind, migrate or persist a port");
                    pick.PerformClick(); automatic.Checked = true; pick.PerformClick();
                    Check(!port.Enabled && automatic.Checked && !(bool)Field(form, "pickFreePort"), "cancel selection respects automatic mode rather than enabling manual port");
                    automatic.Checked = false;
                    bool? capturedPick = null; int capturedPort = 0;
                    form.SaveRequested = delegate(AppSettings candidate, bool choose) { capturedPick = choose; capturedPort = candidate.HttpProxyPort; return new SettingsSaveError(SettingsField.HttpProxyPort, "Fixture refuses save"); };
                    ((Button)form.AcceptButton).PerformClick(); Application.DoEvents();
                    Check(capturedPick == false && capturedPort == 31881, "saving after cancellation submits entered port without one-time selection");
                    pick.PerformClick(); ((Button)form.AcceptButton).PerformClick(); Application.DoEvents();
                    Check(capturedPick == true && form.Visible && !automatic.Checked, "failed save retains cancellable selection and original automatic preference");
                    pick.PerformClick();
                    Shot(form, "settings-port-pick-cancelled");
                    form.Close();
                    Check(!((Timer)Field(form, "currentValuesTimer")).Enabled, "closing settings stops effective-value refresh timer");
                }
            } finally {
                socks.Stop(); settings.Save(original);
                foreach (var pair in environment) Environment.SetEnvironmentVariable(pair.Key, pair.Value, EnvironmentVariableTarget.User);
            }
        }
    }
}

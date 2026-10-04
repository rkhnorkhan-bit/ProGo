using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Windows.Forms;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        private static void SettingsValidationAndErrors(SettingsService settings)
        {
            foreach (var host in new[] { "127.0.0.1", "::1", "[::1]", "proxy.example.org", "localhost" }) {
                var valid = AppSettings.Defaults(); valid.SocksHost = host;
                Check(SettingsValidation.Check(valid) == null, "valid SOCKS host is accepted: " + host);
            }
            foreach (var url in new[] { "https://example.org/check?mode=1", "http://localhost:8080/check", "https://[::1]:8443/check" }) {
                var valid = AppSettings.Defaults(); valid.TestEndpoint = url;
                Check(SettingsValidation.Check(valid) == null, "valid complete test URL is accepted: " + url);
            }
            foreach (var host in new[] { "", "user@proxy.example.org", "https://proxy.example.org", "proxy.example.org:1080", "-option", "bad host", "999.999.999.999", "127.0.0.1\n", "[proxy.example.org]", "[[::1]]" }) {
                var invalid = AppSettings.Defaults(); invalid.SocksHost = host;
                var result = SettingsValidation.Check(invalid);
                Check(result != null && result.Field == SettingsField.SocksHost && result.Section == SettingsErrorSection.Connections, "invalid host returns structured connection error: " + host.Replace("\n", "<newline>"));
            }
            foreach (var url in new[] { "", "example.org", "ftp://example.org/check", "https://user:fixture@example.org/check", "https://example.org:0/check", "https://example.org:70000/check", "https://bad host/check", "https://example.org/\ncheck", "https://example.org\\check" }) {
                var invalid = AppSettings.Defaults(); invalid.TestEndpoint = url;
                var result = SettingsValidation.Check(invalid);
                Check(result != null && result.Field == SettingsField.TestEndpoint && result.Section == SettingsErrorSection.Diagnostics, "invalid URL returns structured diagnostic error");
            }
            var candidate = AppSettings.Defaults(); candidate.SocksPort = 0;
            Check(SettingsValidation.Check(candidate).Field == SettingsField.SocksPort, "out-of-range SOCKS port is not silently normalized on Apply");
            candidate = AppSettings.Defaults(); candidate.HttpProxyPort = 65536;
            Check(SettingsValidation.Check(candidate).Field == SettingsField.HttpProxyPort, "out-of-range app port returns its own field");
            candidate = AppSettings.Defaults(); candidate.ClipboardClearSeconds = 1;
            Check(SettingsValidation.Check(candidate).Section == SettingsErrorSection.Storage, "invalid clipboard duration returns Storage section");
            candidate = AppSettings.Defaults(); candidate.AutoStartSocks = true;
            Check(SettingsValidation.Check(candidate).Field == SettingsField.SshProfile, "automatic SSH start requires a selected connection");
            candidate.SshProfile = "fixture"; candidate.SshProfiles.Add(new SshProfileSetting { Target = "fixture", Server = "bad/server", User = "fixture" });
            Check(SettingsValidation.Check(candidate).Field == SettingsField.SshProfile, "invalid saved SSH fields are attributed to the connection list");
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") { Console.WriteLine("SKIP: native settings error fixture requires isolated CI"); return; }
            var original = settings.Current.Clone();
            var environment = CliProxyEnvironmentService.Names.ToDictionary(n => n, n => Environment.GetEnvironmentVariable(n, EnvironmentVariableTarget.User));
            var windows = SystemProxyService.ReadCurrent();
            try {
                var valid = AppSettings.Defaults(); valid.SshProfile = "my-vps"; settings.Save(valid);
                using (var bridge = new CliProxyBridgeService(settings)) {
                    string message; Check(bridge.Start(out message), "validation fixture starts the real application bridge");
                    int originalPort = bridge.Port;
                    var bytes = File.ReadAllBytes(AppPaths.SettingsPath);
                    var occupied = Occupy(0);
                    try {
                        candidate = settings.Current.Clone(); candidate.HttpProxyPort = Number(occupied); candidate.AutoHttpProxyPort = false; candidate.TestEndpoint = "ftp://example.org/check";
                        SettingsSaveError error;
                        Check(!bridge.ReconfigureDetailed(candidate, false, out error) && error.Field == SettingsField.TestEndpoint,
                            "invalid URL is rejected before occupied-port binding or integration migration");
                        Check(bridge.Port == originalPort && bridge.IsRunning && bytes.SequenceEqual(File.ReadAllBytes(AppPaths.SettingsPath)) &&
                            CliProxyEnvironmentService.Names.All(n => Environment.GetEnvironmentVariable(n, EnvironmentVariableTarget.User) == environment[n]) &&
                            SystemProxyService.FieldNames.All(n => SystemProxyService.ReadCurrent().Values[n].Matches(windows.Values[n])),
                            "validation failure preserves listener, settings bytes and Windows/CLI integration state");
                        AssertBridge(originalPort);
                        candidate.TestEndpoint = valid.TestEndpoint;
                        Check(!bridge.ReconfigureDetailed(candidate, false, out error) && error.Field == SettingsField.HttpProxyPort && error.Section == SettingsErrorSection.Ports,
                            "real occupied port returns structured port error");
                    } finally { occupied.Stop(); }
                    using (var form = new SshProfilesSettingsForm(settings)) {
                        form.Show(); Application.DoEvents();
                        var tabs = Descendants(form).OfType<TabControl>().Single();
                        var host = (TextBox)Field(form, "host"); var endpoint = (TextBox)Field(form, "endpoint");
                        var notice = (Label)Field(form, "saveError"); int called = 0;
                        form.SaveRequested = delegate { called++; return null; };
                        host.Text = "https://wrong.example.org"; tabs.SelectedIndex = 0;
                        ((Button)form.AcceptButton).PerformClick(); Application.DoEvents();
                        Check(called == 0 && form.Visible && tabs.SelectedIndex == 1 && host.Focused && notice.Visible && notice.Text.Contains("Адрес SOCKS"),
                            "invalid address focuses Connections and never invokes Save callback");
                        Shot(form, "settings-invalid-address");
                        host.Text = valid.SocksHost; endpoint.Text = "ftp://wrong.example.org";
                        ((Button)form.AcceptButton).PerformClick(); Application.DoEvents();
                        Check(called == 0 && tabs.SelectedIndex == 3 && endpoint.Focused && notice.Text.Contains("Сайт проверки"),
                            "invalid URL focuses Diagnostics without any settings application");
                        Shot(form, "settings-invalid-url");
                        endpoint.Text = "https://new.example.org/check";
                        form.SaveRequested = delegate(AppSettings proposed, bool pick) { called++; SettingsSaveError error; bridge.ReconfigureDetailed(proposed, pick, out error); return error; };
                        using (var locked = File.Open(AppPaths.SettingsPath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                            ((Button)form.AcceptButton).PerformClick(); Application.DoEvents();
                            Check(called == 1 && form.Visible && tabs.SelectedIndex == 3 && notice.Visible && notice.Text.Contains("файл настроек"),
                                "real file-write denial stays on the current page rather than blaming app port");
                            Check(endpoint.Text == "https://new.example.org/check" && bytes.SequenceEqual(File.ReadAllBytes(AppPaths.SettingsPath)) && bridge.Port == originalPort,
                                "write failure retains all edits and exact saved bytes/listener");
                            Shot(form, "settings-write-error");
                        }
                        ((Button)form.AcceptButton).PerformClick(); Application.DoEvents();
                        Check(called == 2 && form.DialogResult == DialogResult.OK && !form.Visible && settings.Current.TestEndpoint == "https://new.example.org/check",
                            "corrected write condition can be retried successfully without reopening settings");
                    }
                    if (File.Exists(CliProxyEnvironmentService.BackupPath)) throw new Exception("Settings migration fixture is not isolated");
                    try {
                        File.WriteAllText(CliProxyEnvironmentService.BackupPath, "{}");
                        using (var locked = File.Open(CliProxyEnvironmentService.BackupPath, FileMode.Open, FileAccess.Read, FileShare.None))
                        using (var form = new SshProfilesSettingsForm(settings)) {
                            form.Show(); Application.DoEvents();
                            var tabs = Descendants(form).OfType<TabControl>().Single(); tabs.SelectedIndex = 4;
                            ((Button)Field(form, "pickPortButton")).PerformClick();
                            form.SaveRequested = delegate(AppSettings proposed, bool pick) { SettingsSaveError error; bridge.ReconfigureDetailed(proposed, pick, out error); return error; };
                            ((Button)form.AcceptButton).PerformClick(); Application.DoEvents();
                            Check(form.Visible && tabs.SelectedIndex == 0 && ((Label)Field(form, "saveError")).Text.Contains("настройки приложений"),
                                "integration-read failure is attributed to application controls rather than the port field");
                            Check(bridge.Port == originalPort && (bool)Field(form, "pickFreePort"), "failed migration preserves owned listener and pending port-pick request");
                            Shot(form, "settings-integration-error"); form.Close();
                        }
                    } finally { File.Delete(CliProxyEnvironmentService.BackupPath); }
                    using (var form = new SshProfilesSettingsForm(settings)) {
                        form.Show(); Application.DoEvents();
                        var tabs = Descendants(form).OfType<TabControl>().Single(); tabs.SelectedIndex = 2;
                        form.SaveRequested = delegate { throw new IOException("fixture callback failure"); };
                        ((Button)form.AcceptButton).PerformClick(); Application.DoEvents();
                        Check(form.Visible && tabs.SelectedIndex == 2 && ((Label)Field(form, "saveError")).Visible, "unexpected callback failure remains a general inline error without crashing or changing pages");
                        form.Close();
                    }
                }
            } finally { settings.Save(original); }
        }
    }
}

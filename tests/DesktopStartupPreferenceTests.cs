using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        private static void StartupSettingsWorkflow(SettingsService settings)
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") return;
            string fixture = Path.Combine(Path.GetTempPath(), "ProGo-startup-ui-" + Guid.NewGuid().ToString("N"));
            string install = Path.Combine(fixture, "app"), programs = Path.Combine(fixture, "Programs"), startup = Path.Combine(fixture, "Startup");
            string link = Path.Combine(startup, "ProGo.lnk");
            var original = settings.Current.Clone();
            try {
                Directory.CreateDirectory(install);
                File.WriteAllText(Path.Combine(install, "ProGo.exe"), "fixture; never executed");
                var shortcuts = new ApplicationShortcuts(install, programs, startup);
                var configured = AppSettings.Defaults(); configured.SshProfile = "fixture";
                configured.AutoStartSocks = false; configured.AutoCliProxy = false; configured.AutoSystemProxy = false;
                settings.Save(configured);
                byte[] bytes = File.ReadAllBytes(AppPaths.SettingsPath);
                int opened = 0;
                using (var form = new SshProfilesSettingsForm(settings, SettingsSection.Connections, shortcuts, () => opened++)) {
                    form.Show(); Application.DoEvents();
                    var launch = (CheckBox)Field(form, "autoLaunch");
                    Check(launch.Enabled && !launch.Checked && launch.AccessibilityObject.Name == "Запускать ProGo при входе в Windows",
                        "startup checkbox reads real absent registration with an accessible name");
                    launch.Checked = true;
                    ((Button)Field(form, "startupSettingsButton")).PerformClick(); form.RefreshCurrentValues();
                    Check(opened == 1 && launch.Checked && !File.Exists(link) && bytes.SequenceEqual(File.ReadAllBytes(AppPaths.SettingsPath)),
                        "editing, refreshing and opening Windows settings never apply pending preferences");
                    Check(((Label)Field(form, "startupNotice")).Text.Contains("Windows может отдельно"), "startup presence is not represented as proof of Windows approval");
                    var ssh = (CheckBox)Field(form, "autoStart");
                    Check(!ssh.Checked, "Windows startup selection does not enable automatic SSH");
                    KeyboardWalk(form, new Control[] { launch, (Button)Field(form, "startupSettingsButton"), ssh }, "startup preferences");
                    DashboardFixtureSize(form, new Size(834, 611));
                    ((ScrollableControl)launch.Parent.Parent).ScrollControlIntoView(launch); Application.DoEvents();
                    Shot(form, "settings-startup-minimum");
                    form.Hide();
                    using (var cancel = new Timer { Interval = 30 }) {
                        cancel.Tick += delegate { cancel.Stop(); ((Button)form.CancelButton).PerformClick(); };
                        cancel.Start(); Check(form.ShowDialog() == DialogResult.Cancel, "Cancel discards startup selection");
                    }
                    Check(!File.Exists(link) && bytes.SequenceEqual(File.ReadAllBytes(AppPaths.SettingsPath)), "Cancel leaves registration and settings bytes intact");
                }
                using (var form = new SshProfilesSettingsForm(settings, SettingsSection.Connections, shortcuts)) {
                    form.Show(); Application.DoEvents(); ((CheckBox)Field(form, "autoLaunch")).Checked = true;
                    ((Button)form.AcceptButton).PerformClick(); Application.DoEvents();
                    Check(form.DialogResult == DialogResult.OK && shortcuts.ReadStartup().Registered && !settings.Current.AutoStartSocks,
                        "real Save enables Windows registration while retaining manual SSH");
                }
                byte[] registration = File.ReadAllBytes(link); bytes = File.ReadAllBytes(AppPaths.SettingsPath);
                using (var form = new SshProfilesSettingsForm(settings, SettingsSection.Connections, shortcuts)) {
                    form.Show(); Application.DoEvents(); ((CheckBox)Field(form, "autoLaunch")).Checked = false;
                    ((TextBox)Field(form, "endpoint")).Text = "invalid-url";
                    ((Button)form.AcceptButton).PerformClick(); Application.DoEvents();
                    Check(form.Visible && registration.SequenceEqual(File.ReadAllBytes(link)), "input validation precedes startup changes");
                    ((TextBox)Field(form, "endpoint")).Text = configured.TestEndpoint;
                    using (var locked = File.Open(AppPaths.SettingsPath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                        ((Button)form.AcceptButton).PerformClick(); Application.DoEvents();
                        Check(form.Visible && registration.SequenceEqual(File.ReadAllBytes(link)) && bytes.SequenceEqual(File.ReadAllBytes(AppPaths.SettingsPath)),
                            "failed settings-file write rolls startup off back to exact previous bytes");
                        Check(!((CheckBox)Field(form, "autoLaunch")).Checked, "failed Save retains pending checkbox for retry");
                    }
                    ((CheckBox)Field(form, "autoStart")).Checked = true;
                    ((Button)form.AcceptButton).PerformClick(); Application.DoEvents();
                    Check(form.DialogResult == DialogResult.OK && !File.Exists(link) && settings.Current.AutoStartSocks,
                        "retry saves manual app launch and automatic SSH independently");
                }
                configured.AutoStartSocks = false; settings.Save(configured);
                using (var form = new SshProfilesSettingsForm(settings, SettingsSection.Connections, shortcuts)) {
                    form.Show(); Application.DoEvents(); ((CheckBox)Field(form, "autoLaunch")).Checked = true;
                    form.SaveRequested = delegate { throw new IOException("synthetic downstream failure"); };
                    ((Button)form.AcceptButton).PerformClick(); Application.DoEvents();
                    Check(form.Visible && !File.Exists(link), "exception during settings application removes a newly created startup link");
                    Shot(form, "settings-startup-rollback"); form.Close();
                }
                shortcuts.ChangeStartup(shortcuts.ReadStartup(), true);
                using (var form = new SshProfilesSettingsForm(settings, SettingsSection.Connections, shortcuts)) {
                    form.Show(); Application.DoEvents(); ((CheckBox)Field(form, "autoLaunch")).Checked = false;
                    int applied = 0; form.SaveRequested = delegate { applied++; return null; };
                    using (var locked = File.Open(link, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                        ((Button)form.AcceptButton).PerformClick(); Application.DoEvents();
                        Check(applied == 0 && form.Visible && File.Exists(link) && ((CheckBox)Field(form, "autoLaunch")).Focused,
                            "startup write failure focuses its field before other settings are applied");
                    }
                    form.Close();
                }
                File.Delete(link);
                using (var form = new SshProfilesSettingsForm(settings, SettingsSection.Connections, shortcuts)) {
                    form.Show(); Application.DoEvents(); ((CheckBox)Field(form, "autoLaunch")).Checked = true;
                    form.SaveRequested = delegate {
                        File.WriteAllText(link, "external edit during failed save");
                        return new SettingsSaveError(SettingsField.SettingsFile, "Fixture failure");
                    };
                    ((Button)form.AcceptButton).PerformClick(); Application.DoEvents();
                    Check(form.Visible && File.ReadAllText(link) == "external edit during failed save" &&
                        ((Label)Field(form, "saveError")).Text.Contains("вернуть к прежнему"),
                        "rollback conflict preserves external edit and reports incomplete recovery");
                    Shot(form, "settings-startup-recovery-conflict"); form.Close();
                }
                using (var form = new SshProfilesSettingsForm(settings, SettingsSection.Connections, shortcuts)) {
                    form.Show(); Application.DoEvents();
                    Check(!((CheckBox)Field(form, "autoLaunch")).Enabled && ((Label)Field(form, "startupNotice")).Text.Contains("недоступен"),
                        "corrupt or foreign shortcut disables only startup editing");
                    form.SaveRequested = delegate { return null; }; ((Button)form.AcceptButton).PerformClick();
                    Check(form.DialogResult == DialogResult.OK && File.ReadAllText(link) == "external edit during failed save",
                        "unrelated settings remain saveable without touching unavailable startup");
                }
                using (var form = new SshProfilesSettingsForm(settings, SettingsSection.Connections)) {
                    Check(!((CheckBox)Field(form, "autoLaunch")).Enabled && ((Label)Field(form, "startupNotice")).Text.Contains("вне папки"),
                        "portable/test executable does not silently register itself at login");
                }
            } finally { settings.Save(original); if (Directory.Exists(fixture)) Directory.Delete(fixture, true); }
        }
    }
}

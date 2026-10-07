using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        private static void SettingsAccessibility(SettingsService settings)
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") { Console.WriteLine("SKIP: settings keyboard fixtures require isolated native CI"); return; }
            var bytes = File.ReadAllBytes(AppPaths.SettingsPath);
            using (var form = new SshProfilesSettingsForm(settings)) {
                form.Show(); Application.DoEvents(); ((Timer)Field(form, "currentValuesTimer")).Stop();
                var tabs = (TabControl)Field(form, "settingsTabs");
                var cancel = (Control)form.CancelButton; var save = (Control)form.AcceptButton;
                foreach (var item in new[] {
                    new[] { "sshProfiles", "Сервер" }, new[] { "host", "Адрес SOCKS-туннеля" },
                    new[] { "port", "Порт SOCKS-туннеля" }, new[] { "clearSeconds", "Очищать буфер через, сек." },
                    new[] { "endpoint", "Сайт проверки" }, new[] { "httpPort", "Порт на этом компьютере" },
                    new[] { "proxyAddress", "Текущий адрес" }
                }) {
                    var field = (Control)Field(form, item[0]);
                    Check(field.AccessibilityObject.Name == item[1], "accessible settings field name: " + item[0]);
                }
                var automatic = Descendants(tabs.TabPages[0]).Where(c => c is CheckBox || c is Button).ToArray();
                KeyboardWalk(form, automatic.Concat(new[] { cancel, save }).ToArray(), "automation");
                var manual = automatic.OfType<Button>().ToArray();
                Check(manual.All(b => !String.IsNullOrEmpty(b.AccessibilityObject.Description)) &&
                    manual.Where(b => b.Text == "Включить").Select(b => b.AccessibilityObject.Description).Distinct().Count() == 2,
                    "same-caption manual buttons expose their target and immediate-action semantics");
                string command = null; form.ManualActionRequested += action => command = AppCommands.Get(action).LegacyId;
                var cli = manual.Single(b => b.Text == "Включить" && b.AccessibleDescription.Contains("Codex"));
                cli.PerformClick(); Check(command == "cli-start", "accessible manual CLI action retains the ordinary cli-start route");
                tabs.SelectedIndex = 1; Application.DoEvents();
                var startupButton = (Control)Field(form, "startupSettingsButton");
                var connectionActions = Descendants(tabs.TabPages[1]).OfType<Button>().Where(b => b != startupButton).Cast<Control>();
                KeyboardWalk(form, new[] { (Control)Field(form, "sshProfiles") }.Concat(connectionActions).Concat(new[] {
                    (Control)Field(form, "host"), (Control)Field(form, "port"), (Control)Field(form, "autoLaunch"), startupButton, (Control)Field(form, "autoStart"),
                    (Control)Field(form, "autoSwitchProfile"), cancel, save }).Where(c => c.Enabled).ToArray(), "connection");
                ((TextBox)Field(form, "host")).Text = "pending.example.org";
                tabs.SelectedIndex = 2; Application.DoEvents();
                KeyboardWalk(form, new[] { (Control)Field(form, "clearSeconds"), cancel, save }, "storage");
                tabs.SelectedIndex = 3; Application.DoEvents();
                var diagnostic = Descendants(tabs.TabPages[3]).OfType<TextBox>().Cast<Control>().ToArray();
                KeyboardWalk(form, diagnostic.Concat(new[] { cancel, save }).ToArray(), "diagnostics");
                Check(diagnostic.Skip(1).All(c => c.AccessibilityObject.Name.Length > 0 && c.AccessibilityObject.Description.Contains("Только чтение")),
                    "read-only diagnostic paths remain named and keyboard selectable for copying");
                tabs.SelectedIndex = 4; Application.DoEvents();
                var auto = (CheckBox)Field(form, "autoHttpPort"); auto.Checked = false;
                var http = (Control)Field(form, "httpPort"); var address = (Control)Field(form, "proxyAddress");
                var portButtons = Descendants(tabs.TabPages[4]).OfType<Button>().Cast<Control>().ToArray();
                KeyboardWalk(form, new[] { (Control)auto, http, address }.Concat(portButtons).Concat(new[] { cancel, save }).ToArray(), "manual port");
                auto.Checked = true;
                KeyboardWalk(form, new[] { (Control)auto, address }.Concat(portButtons).Concat(new[] { cancel, save }).ToArray(), "automatic port skips disabled number");
                auto.Checked = false; var pick = (Button)Field(form, "pickPortButton"); pick.PerformClick();
                Check(pick.AccessibilityObject.Name == "Отменить подбор" && !http.Enabled, "port-pick accessibility follows its current action");
                KeyboardWalk(form, new[] { (Control)auto, address }.Concat(portButtons).Concat(new[] { cancel, save }).ToArray(), "pending port selection");
                pick.PerformClick();
                form.SaveRequested = delegate { return new SettingsSaveError(SettingsField.SocksHost, "Проверьте адрес сервера."); };
                ((Button)save).PerformClick(); Application.DoEvents();
                Check(tabs.SelectedIndex == 1 && ((Control)Field(form, "host")).ContainsFocus && ((TextBox)Field(form, "host")).Text == "pending.example.org",
                    "refused Save still focuses the named field and preserves pending text");
                Shot(form, "keyboard-settings-error"); form.Close();
            }
            Check(File.ReadAllBytes(AppPaths.SettingsPath).SequenceEqual(bytes), "keyboard traversal and cancelled settings do not persist edits");
            using (var form = new SshProfileEditorForm(null)) {
                form.Show(); Application.DoEvents();
                var name = (Control)Field(form, "name"); var mode = (ComboBox)Field(form, "mode");
                var server = (TextBox)Field(form, "server"); var user = (Control)Field(form, "user");
                var port = (Control)Field(form, "port"); var key = (Control)Field(form, "key");
                var browse = (Control)Field(form, "browse"); var alias = (TextBox)Field(form, "target");
                var save = (Control)form.AcceptButton; var cancel = (Control)form.CancelButton;
                foreach (var field in new[] { name, (Control)mode, server, user, port, key, alias })
                    Check(!String.IsNullOrEmpty(field.AccessibilityObject.Name), "SSH editor exposes field name: " + field.Name);
                Check(key.AccessibilityObject.Name == "Закрытый SSH-ключ" && browse.AccessibilityObject.Description.Contains("SSH-ключ"),
                    "SSH key text and picker have distinct accessible purposes");
                KeyboardWalk(form, new[] { name, mode, server, user, port, key, browse, save, cancel }, "direct SSH");
                server.Text = "pending.example.org"; mode.SelectedIndex = 1; alias.Text = "pending-alias";
                KeyboardWalk(form, new[] { name, (Control)mode, alias, save, cancel }, "alias SSH skips disabled direct fields");
                mode.SelectedIndex = 0;
                Check(server.Text == "pending.example.org" && alias.Text == "pending-alias", "keyboard mode switching retains uncommitted direct and alias input");
                Shot(form, "keyboard-ssh-editor"); form.Close();
                Check(!form.Profile.IsDirect && String.IsNullOrEmpty(form.Profile.Server), "cancelled SSH editor does not commit pending fields");
            }
        }
        private static void KeyboardWalk(Form form, Control[] controls, string name)
        {
            Check(controls[0].Focus(), name + " first field accepts focus");
            var key = typeof(Form).GetMethod("ProcessDialogKey", BindingFlags.Instance | BindingFlags.NonPublic);
            for (int i = 1; i < controls.Length; i++) {
                key.Invoke(form, new object[] { Keys.Tab }); Application.DoEvents();
                Check(controls[i].ContainsFocus, name + " Tab reaches " + controls[i].GetType().Name + " " + controls[i].AccessibilityObject.Name + "; focused=" + String.Join(" | ", Descendants(form).Where(c => c.Focused).Select(c => c.GetType().Name + ":" + c.Text + " tab=" + c.TabIndex)));
            }
            for (int i = controls.Length - 2; i >= 0; i--) {
                key.Invoke(form, new object[] { Keys.Tab | Keys.Shift }); Application.DoEvents();
                Check(controls[i].ContainsFocus, name + " Shift+Tab returns to " + controls[i].GetType().Name + " " + controls[i].AccessibilityObject.Name + "; focused=" + String.Join(" | ", Descendants(form).Where(c => c.Focused).Select(c => c.GetType().Name + ":" + c.Text + " tab=" + c.TabIndex)));
            }
        }
    }
}

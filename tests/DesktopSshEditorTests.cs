using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        private static void StructuredSshProfiles(SettingsService settings)
        {
            var old = settings.Current.Clone();
            var path = Path.Combine(work, "SSH key O'Brien $literal"); File.WriteAllText(path, "fixture only");
            var profile = new SshProfileSetting { Name = "Primary", Target = "progo-test-profile", Server = "vpn.example.org", User = "ubuntu", Port = 2222, IdentityFile = path };
            try {
                var legacy = SettingsService.DeserializeSettings("{\"SshProfile\":\"my-vps\",\"SshProfiles\":[{\"Name\":\"Old server\",\"Target\":\"my-vps\"}]}");
                settings.Save(legacy); var loaded = settings.Load();
                Check(!loaded.SshProfiles[0].IsDirect && loaded.SshProfile == "my-vps" && loaded.SshProfiles[0].Name == "Old server", "legacy aliases load and save unchanged");
                Check(SshConnection.CommandArguments(loaded.SshProfiles[0]) == "my-vps", "legacy alias keeps OpenSSH config resolution");
                loaded.SshProfiles.Add(profile); var other = profile.Clone(); other.Target = "progo-second-profile"; other.Port = 2200; loaded.SshProfiles.Add(other); loaded.SshProfile = profile.Target;
                settings.Save(loaded); loaded = settings.Load(); var copy = SshConnection.Resolve(loaded, profile.Target);
                Check(loaded.SshProfiles.Count == 3 && copy.Server == profile.Server && copy.User == profile.User && copy.Port == 2222 && copy.IdentityFile == path, "save/load preserves structured settings and separate ports on one host");
                Check(loaded.Clone().SshProfiles[1].IdentityFile == path && profile.Clone().Port == 2222, "profile and settings clones preserve structured fields");
                Check(!profile.ToString().Contains("progo-test-profile") && profile.ToString().Contains("ubuntu@vpn.example.org:2222"), "UI labels show server and port without opaque selection identifier");
                var renamed = loaded.Clone(); renamed.SshProfiles[1].Name = "Renamed";
                Check(SshConnection.Signature(loaded) == SshConnection.Signature(renamed), "renaming a profile does not change route identity");
                foreach (var field in new[] { "server", "user", "port", "key" }) {
                    var changed = loaded.Clone(); var p = changed.SshProfiles[1];
                    if (field == "server") p.Server = "other.example.org"; else if (field == "user") p.User = "root"; else if (field == "port") p.Port++; else p.IdentityFile = path + "-other";
                    Check(SshConnection.Signature(loaded) != SshConnection.Signature(changed), "editing " + field + " invalidates connection identity even with unchanged selection");
                }
                var args = SshConnection.Arguments(profile);
                Check(args[Array.IndexOf(args, "-p") + 1] == "2222" && args[Array.IndexOf(args, "-l") + 1] == "ubuntu" && args[Array.IndexOf(args, "-i") + 1] == path && args.Last() == profile.Server && args.Contains("IdentitiesOnly=yes"), "shared SSH arguments contain explicit port/user/key and host");
                var login = SshInteractiveLogin.CreateStartInfo(profile);
                var script = Encoding.Unicode.GetString(Convert.FromBase64String(login.Arguments.Split(' ').Last()));
                Check(script.Contains("'-p' '2222'") && script.Contains("'-l' 'ubuntu'") && script.Contains("'-i' '" + path.Replace("'", "''") + "'") && script.Contains("StrictHostKeyChecking=ask"), "first login uses the same fields and literal quoted paths without shell interpolation");
                var loginCapture = Path.Combine(work, "first-login-argv.json");
                var spy = "function ssh.exe { ConvertTo-Json -InputObject @($args) -Compress | Set-Content -LiteralPath $env:PROGO_TEST_LOGIN_ARGV }; " + script;
                var start = new ProcessStartInfo("powershell.exe", "-NoProfile -NonInteractive -EncodedCommand " + Convert.ToBase64String(Encoding.Unicode.GetBytes(spy))) {
                    UseShellExecute = false, CreateNoWindow = true
                };
                start.EnvironmentVariables["PROGO_TEST_LOGIN_ARGV"] = loginCapture;
                using (var process = Process.Start(start)) {
                    if (!process.WaitForExit(10000)) { process.Kill(); throw new Exception("First-login quoting fixture stalled"); }
                    Check(process.ExitCode == 0 && File.Exists(loginCapture), "visible-login command executes with a local PowerShell spy instead of real SSH");
                }
                var seen = new JavaScriptSerializer().Deserialize<string[]>(File.ReadAllText(loginCapture));
                Check(seen[Array.IndexOf(seen, "-i") + 1] == path && seen[Array.IndexOf(seen, "-p") + 1] == "2222" && seen.Last() == profile.Server,
                    "actual PowerShell preserves key path/apostrophe/dollar sign and explicit port as separate SSH arguments");
                var result = SshProfileDiagnostics.Check(profile);
                Check(result.SshAvailable && result.SshResolved && result.ResolvedHostName == profile.Server && result.ResolvedUser == profile.User && result.ResolvedPort == "2222", "real Windows ssh.exe -G resolves structured server/user/port without network access: " + result.Error);
                Check(result.ResolvedIdentityFile == path.Replace('\\', '/') || result.ResolvedIdentityFile == path, "real ssh.exe receives the unchanged key path with spaces and apostrophe");
                foreach (var invalid in new[] { "server-option", "user-option", "port-zero", "port-large", "alias-newline", "alias-parameters", "key-newline", "key-relative" }) {
                    var p = profile.Clone();
                    if (invalid == "server-option") p.Server = "-oProxyCommand=anything";
                    else if (invalid == "user-option") p.User = "ubuntu -o anything";
                    else if (invalid == "port-zero") p.Port = 0;
                    else if (invalid == "port-large") p.Port = 65536;
                    else if (invalid == "alias-newline") { p.Server = ""; p.Target = "my-vps\n"; }
                    else if (invalid == "alias-parameters") { p.Server = ""; p.Target = "my-vps -o anything"; }
                    else if (invalid == "key-newline") p.IdentityFile = path + "\n";
                    else p.IdentityFile = "relative-key";
                    bool rejected = false; try { SshConnection.Arguments(p); } catch (ArgumentException) { rejected = true; }
                    Check(rejected, "malformed SSH field rejected: " + invalid);
                }
                using (var form = new SshProfileEditorForm(null)) {
                    form.Show(); Application.DoEvents();
                    Check(((ComboBox)form.Controls.Find("connectionMode", true)[0]).SelectedIndex == 0 && form.Controls.Find("sshServer", true)[0].Enabled && !form.Controls.Find("sshAlias", true)[0].Enabled, "new connection opens beginner server mode");
                    form.Controls.Find("sshServer", true)[0].Text = profile.Server; form.Controls.Find("sshUser", true)[0].Text = profile.User;
                    ((NumericUpDown)form.Controls.Find("sshPort", true)[0]).Value = 2222; form.Controls.Find("sshKey", true)[0].Text = path;
                    Shot(form, "server-direct-fields");
                    form.Scale(new System.Drawing.SizeF(1.5f, 1.5f)); Application.DoEvents(); Shot(form, "server-direct-fields-150");
                    foreach (var control in new[] { "sshServer", "sshUser", "sshPort", "sshKey", "saveConnection" }) {
                        var c = form.Controls.Find(control, true)[0];
                        Check(c.Width > 0 && c.Height > 0 && c.Visible && c.Parent.ClientRectangle.Contains(c.Bounds), "scaled SSH editor keeps field/button visible: " + control);
                    }
                    ((Button)form.Controls.Find("saveConnection", true)[0]).PerformClick();
                    Check(form.Profile.IsDirect && form.Profile.Port == 2222 && form.Profile.IdentityFile == path && form.Profile.Target.StartsWith("progo-"), "actual editor saves explicit SSH fields");
                    copy = form.Profile.Clone(); form.Close();
                }
                using (var form = new SshProfileEditorForm(copy)) {
                    form.Show(); Application.DoEvents(); ((NumericUpDown)form.Controls.Find("sshPort", true)[0]).Value = 2200;
                    ((Button)form.Controls.Find("saveConnection", true)[0]).PerformClick();
                    Check(form.Profile.Target == copy.Target && form.Profile.Port == 2200, "editing a direct profile preserves its selection identity"); form.Close();
                }
                using (var form = new SshProfileEditorForm(legacy.SshProfiles[0])) {
                    form.Show(); Application.DoEvents();
                    Check(((ComboBox)form.Controls.Find("connectionMode", true)[0]).SelectedIndex == 1 && !form.Controls.Find("sshServer", true)[0].Enabled && form.Controls.Find("sshAlias", true)[0].Text == "my-vps", "editing an old profile preserves advanced alias mode");
                    Shot(form, "server-legacy-alias"); ((Button)form.Controls.Find("saveConnection", true)[0]).PerformClick();
                    Check(!form.Profile.IsDirect && form.Profile.Target == "my-vps", "saving an old alias does not rewrite it into a direct profile"); form.Close();
                }
            } finally { settings.Save(old); File.Delete(path); }
        }
    }
}

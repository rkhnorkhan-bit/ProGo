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
            var path = Path.Combine(work, "SSH key O'Brien $literal ключ"); File.WriteAllText(path, "fixture only");
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
                var portable = profile.Clone(); portable.IdentityFile = "~/.ssh/portable_key";
                var homeA = Path.Combine(work, "user-one"); var homeB = Path.Combine(work, "user-two");
                Check(SshConnection.KeyPath(portable.IdentityFile, homeA) == Path.Combine(homeA, ".ssh", "portable_key")
                    && SshConnection.KeyPath(portable.IdentityFile, homeB) == Path.Combine(homeB, ".ssh", "portable_key"), "portable key follows the current user's home rather than a saved machine path");
                Check(SshConnection.KeyPath(@"~\.ssh\portable_key", homeA) == SshConnection.KeyPath(portable.IdentityFile, homeA), "both portable path separators resolve identically");
                var portableArgs = SshConnection.Arguments(portable);
                Check(portableArgs[Array.IndexOf(portableArgs, "-i") + 1] == SshConnection.KeyPath(portable.IdentityFile)
                    && portable.IdentityFile == "~/.ssh/portable_key", "SSH receives resolved path while portable saved setting remains unchanged");
                settings.Save(new AppSettings { SshProfiles = new System.Collections.Generic.List<SshProfileSetting> { portable }, SshProfile = portable.Target });
                Check(settings.Load().SshProfiles[0].IdentityFile == "~/.ssh/portable_key", "portable SSH key setting survives actual save/reload");
                bool escaped = false; try { SshConnection.KeyPath("~/../outside", homeA); } catch (ArgumentException) { escaped = true; }
                Check(escaped, "portable path cannot escape the current user's profile");
                Check(SshProfileDiagnostics.InspectKey(path).Contains("доступен") && SshProfileDiagnostics.InspectKey(path + "-old-machine").Contains("старый абсолютный"), "diagnostic distinguishes readable and stale machine-specific key paths");
                Check(SshAgentDiagnostics.Describe(4, false).StartsWith("Running") && SshAgentDiagnostics.Describe(1, false).StartsWith("Stopped")
                    && SshAgentDiagnostics.Describe(1, true).StartsWith("Disabled") && !SshAgentDiagnostics.Describe(2, false).StartsWith("Running"), "agent diagnostics distinguish running, stopped, disabled and transitional states");
                var login = SshInteractiveLogin.CreateStartInfo(profile);
                var script = Encoding.Unicode.GetString(Convert.FromBase64String(login.Arguments.Split(' ').Last()));
                Check(script.Contains("'-p' '2222'") && script.Contains("'-l' 'ubuntu'") && script.Contains("'-i' '" + path.Replace("'", "''") + "'") && script.Contains("StrictHostKeyChecking=ask"), "first login uses the same fields and literal quoted paths without shell interpolation");
                var loginCapture = Path.Combine(work, "first-login-argv.json");
                var spyScript = Encoding.Unicode.GetString(Convert.FromBase64String(SshInteractiveLogin.CreateStartInfo(profile, "ssh.exe").Arguments.Split(' ').Last()));
                Check(script.StartsWith("& '" + OpenSshClient.Executable.Replace("'", "''") + "' "), "visible first login uses the same chosen OpenSSH installation as background startup");
                var spy = "function ssh.exe { ConvertTo-Json -InputObject @($args) -Compress | Set-Content -Encoding UTF8 -LiteralPath $env:PROGO_TEST_LOGIN_ARGV }; " + spyScript;
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
                NativeStructuredSshArguments(profile);
                PortableSshEditorPaths(profile);
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
        private static void NativeStructuredSshArguments(SshProfileSetting profile)
        {
            const int timeoutMs = 7000;
            string config = Path.Combine(work, "structured-ssh-empty-config");
            string executable = OpenSshClient.Executable, phase = "prepare empty config";
            var watch = Stopwatch.StartNew();
            try {
                // -F excludes user/system config, including Match exec and hostname
                // canonicalization. This smoke checks native arguments; the owned
                // harness fixtures cover full diagnostic parsing, status and cleanup.
                File.WriteAllText(config, "", new UTF8Encoding(false));
                phase = "run native ssh -G with empty config";
                DiagnosticProcessResult captured;
                try {
                    captured = DiagnosticProcess.Run(executable, "-G -F " + SshConnection.Quote(config) + " " + SshConnection.CommandArguments(profile), timeoutMs, System.Threading.CancellationToken.None);
                } catch (Exception ex) {
                    throw new Exception("Native structured SSH argument smoke failed: phase=" + phase + "; client=" + Path.GetFileName(executable)
                        + "; elapsed=" + watch.ElapsedMilliseconds + "ms; deadline=" + timeoutMs + "ms; exception=" + ex.GetType().Name);
                }
                string details = "; phase=capture complete; client=" + Path.GetFileName(executable) + "; elapsed=" + watch.ElapsedMilliseconds
                    + "ms; deadline=" + timeoutMs + "ms; exit=" + captured.ExitCode + "; truncated=" + captured.Truncated;
                Check(captured.ExitCode == 0 && !captured.Truncated, "native Windows ssh.exe -G with isolated config completes within the unchanged diagnostic budget" + details);
                Check(NativeSshField(captured.Output, "hostname") == profile.Server && NativeSshField(captured.Output, "user") == profile.User
                    && NativeSshField(captured.Output, "port") == profile.Port.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    "real Windows ssh.exe -G resolves exact structured server/user/port without network access" + details);
                string identity = NativeSshField(captured.Output, "identityfile");
                Check(identity == profile.IdentityFile.Replace('\\', '/') || identity == profile.IdentityFile,
                    "real ssh.exe receives the unchanged key path with spaces, apostrophe, dollar sign and Unicode" + details);
            } finally { if (File.Exists(config)) File.Delete(config); }
        }
        private static string NativeSshField(string output, string field)
        {
            string prefix = field + " ";
            foreach (string line in output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                if (line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return line.Substring(prefix.Length);
            return null;
        }
        private static void PortableSshEditorPaths(SshProfileSetting original)
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var folder = "progo-ssh-editor-" + Guid.NewGuid().ToString("N");
            var directory = Path.Combine(home, folder); Directory.CreateDirectory(directory);
            var key = Path.Combine(directory, "fixture_key");
            try {
                File.WriteAllText(key, "fixture only"); File.WriteAllText(key + ".pub", "public fixture only");
                foreach (var separator in new[] { "/", "\\" }) {
                    var portable = "~" + separator + folder + separator + "fixture_key";
                    var profile = original.Clone(); profile.IdentityFile = portable;
                    using (var form = new SshProfileEditorForm(profile)) {
                        form.Show(); Application.DoEvents();
                        ((Button)form.Controls.Find("saveConnection", true)[0]).PerformClick();
                        Check(form.DialogResult == DialogResult.OK && form.Profile.IdentityFile == portable &&
                            form.Profile.Target == original.Target && File.Exists(SshConnection.KeyPath(form.Profile.IdentityFile)),
                            "actual SSH editor saves portable key against current home without rewriting stored path: " + separator);
                        form.Close();
                    }
                }
                foreach (var invalid in new[] { "fixture_key-missing", "fixture_key.pub" }) {
                    var profile = original.Clone(); profile.IdentityFile = "~/" + folder + "/fixture_key";
                    using (var form = new SshProfileEditorForm(profile)) {
                        form.Show(); Application.DoEvents();
                        form.Controls.Find("sshKey", true)[0].Text = "~/" + folder + "/" + invalid;
                        var refusal = SaveRefusedSshEditor(form);
                        Check(form.DialogResult == DialogResult.None && form.Profile.IdentityFile == profile.IdentityFile &&
                            refusal.Contains(invalid.EndsWith(".pub", StringComparison.Ordinal) ? "открытый ключ" : "Файл ключа не найден"),
                            "actual SSH editor rejects portable missing/public key and retains original profile: " + invalid);
                        form.Close();
                    }
                }
            } finally { Directory.Delete(directory, true); }
        }
        private static string SaveRefusedSshEditor(SshProfileEditorForm form)
        {
            var contents = new StringBuilder(); bool seen = false;
            using (var timer = new System.Windows.Forms.Timer { Interval = 100 }) {
                timer.Tick += delegate {
                    var dialog = FindWindow("#32770", form.Text); if (dialog == IntPtr.Zero) return;
                    seen = true; timer.Stop();
                    EnumChildWindows(dialog, delegate(IntPtr child, IntPtr data) {
                        var text = new StringBuilder(2048); GetWindowText(child, text, text.Capacity); contents.AppendLine(text.ToString()); return true;
                    }, IntPtr.Zero);
                    PostMessage(dialog, 0x0010, IntPtr.Zero, IntPtr.Zero);
                };
                timer.Start(); ((Button)form.Controls.Find("saveConnection", true)[0]).PerformClick();
            }
            Check(seen, "SSH key refusal opens a visible native explanation");
            return contents.ToString();
        }
    }
}

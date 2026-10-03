using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ProGo
{
    internal static class DesktopTests
    {
        private static int passed;
        private static string work;
        private static void Check(bool value, string name) { if (!value) throw new Exception(name); passed++; Console.WriteLine("PASS: " + name); }
        [STAThread]
        private static int Main(string[] args)
        {
            try
            {
                work = Path.GetFullPath(args[0]); Directory.CreateDirectory(work);
                var json = new JavaScriptSerializer();
                var old = json.Deserialize<AppSettings>("{\"AutoRestartSocks\":false,\"AutoApplyProxy\":true}");
                Check(!old.AutoRestartSocks && old.AutoApplyProxy && !old.AutoSystemProxy && !old.AutoCodexProxy, "old preferences retained; new options opt in");
                Check(json.Deserialize<AppSettings>("{}").AutoRestartSocks, "missing recovery preference retains default");
                for (int mask = 0; mask < 8; mask++)
                {
                    var s = AppSettings.Defaults(); s.AutoApplyProxy = (mask & 1) != 0; s.AutoSystemProxy = (mask & 2) != 0; s.AutoCodexProxy = (mask & 4) != 0;
                    s = json.Deserialize<AppSettings>(json.Serialize(s));
                    var plan = new AutomationPlan(); plan.Update(null, s);
                    foreach (ProxyFeature feature in Enum.GetValues(typeof(ProxyFeature)))
                    {
                        Check(!ApplyOnce(plan, feature, false), "automation waits for a ready route " + mask + "/" + feature);
                        bool enabled = AutomationPlan.Enabled(s, feature);
                        Check(ApplyOnce(plan, feature, true) == enabled && !ApplyOnce(plan, feature, true), "enabled startup option completes once " + mask + "/" + feature);
                    }
                }
                var before = AppSettings.Defaults(); var after = AppSettings.Defaults(); after.AutoSystemProxy = true;
                var changes = new AutomationPlan(); changes.Update(before, after); changes.Cancel(ProxyFeature.Windows);
                changes.Update(after, after); Check(!ApplyOnce(changes, ProxyFeature.Windows, true), "manual off survives timer and unrelated save");
                changes.Update(after, before); changes.Update(before, after); Check(ApplyOnce(changes, ProxyFeature.Windows, true), "explicit off/on re-arms automation");
                changes.Update(null, after); changes.Update(after, before); Check(!ApplyOnce(changes, ProxyFeature.Windows, true), "unchecked option cancels a pending application");
                string host, header; int port;
                Check(CliProxyBridgeService.TryParseHttpTarget("GET http://example.org/path?q=1 HTTP/1.1\r\nHost: wrong.example\r\nProxy-Authorization: private\r\n\r\n", out host, out port, out header)
                    && host == "example.org" && port == 80 && header.StartsWith("GET /path?q=1 HTTP/1.1\r\nHost: example.org") && !header.Contains("private"), "HTTP proxy rewrites authority and strips proxy credentials");
                Check(!CliProxyBridgeService.TryParseHttpTarget("GET http://user:secret@example.org/ HTTP/1.1\r\n\r\n", out host, out port, out header), "HTTP rejects credentials in URI");
                Check(!CliProxyBridgeService.TryParseHttpTarget("GET https://example.org/ HTTP/1.1\r\n\r\n", out host, out port, out header), "HTTPS requires CONNECT");
                Check(old.AutoHttpProxyPort && old.HttpProxyPort == 1881, "legacy settings start with automatic port selection");
                var manualPort = json.Deserialize<AppSettings>("{\"AutoHttpProxyPort\":false,\"HttpProxyPort\":31881}");
                Check(!manualPort.AutoHttpProxyPort && manualPort.HttpProxyPort == 31881, "explicit manual port survives settings load");
                ScopedCodex();
                RestorePreferences();
                using (var settings = new SettingsService())
                {
                    settings.Current.SshProfile = "my-vps";
                    AutomationRetries(settings);
                    ProxyPorts(settings);
                    PortIntegrations(settings);
                    BridgeRoundTrip(settings, false); BridgeRoundTrip(settings, true);
                    Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException); Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
                    using (var proxy = new ProxyService(settings))
                    using (var relay = new Ikev2RelayService())
                    using (var home = new HomeVpnService(relay))
                    using (var clipboard = new ClipboardService(settings))
                    {
                        string clicked = null;
                        using (var dashboard = new MainWindow(settings, proxy, home, delegate(string action) { clicked = action; }))
                        {
                            dashboard.Show(); Application.DoEvents();
                            Descendants(dashboard).OfType<Button>().Single(b => b.Text == "Запустить CLI").PerformClick();
                            Check(clicked == "cli-start", "dashboard restores direct Start CLI action");
                            Shot(dashboard, "main"); dashboard.Close();
                        }
                        var failedAutomation = new AutomationPlan();
                        var automaticWindows = AppSettings.Defaults(); automaticWindows.AutoSystemProxy = true;
                        failedAutomation.Update(null, automaticWindows);
                        Exception setupError;
                        failedAutomation.TryApply(ProxyFeature.Windows, true, delegate { throw new UnauthorizedAccessException("Test access denied"); }, out setupError);
                        using (var dashboard = new MainWindow(settings, proxy, home, delegate { }, null, failedAutomation))
                        {
                            dashboard.Show(); Application.DoEvents();
                            var statusLabel = Descendants(dashboard).OfType<Label>().Single(l => l.Text.Contains("Автонастройка Windows: ошибка. Проверьте настройки."));
                            Check(statusLabel.Visible && statusLabel.Bottom <= statusLabel.Parent.ClientSize.Height, "dashboard exposes paused automation with visible corrective guidance");
                            Shot(dashboard, "main-automation-error"); dashboard.Close();
                        }
                        using (var form = new SshProfilesSettingsForm(settings))
                        {
                            Snapshot(form, "settings", false);
                            var tabs = Descendants(form).OfType<TabControl>().Single();
                            Check(Descendants(tabs.TabPages[0]).OfType<CheckBox>().Count() == 4, "four clearly separated automatic options");
                            var flow = Descendants(tabs.TabPages[0]).OfType<FlowLayoutPanel>().First();
                            flow.AutoScrollPosition = new Point(0, 1000); Shot(form, "settings-bottom");
                            for (int i = 1; i < tabs.TabCount; i++) { tabs.SelectedIndex = i; Application.DoEvents(); Shot(form, "settings-" + i); }
                            Check(tabs.TabCount == 5 && tabs.TabPages[4].Text == "Порт приложений", "application port controls have a separate settings page");
                            var portTab = tabs.TabPages[4];
                            var autoPort = Descendants(portTab).OfType<CheckBox>().Single();
                            var portNumber = Descendants(portTab).OfType<NumericUpDown>().Single();
                            autoPort.Checked = false; Check(portNumber.Enabled, "manual port is editable");
                            form.SaveRequested = delegate { return "Порт занят. Подберите свободный порт."; };
                            ((Button)form.AcceptButton).PerformClick(); Application.DoEvents();
                            Check(form.Visible && form.DialogResult != DialogResult.OK && tabs.SelectedIndex == 4, "failed save keeps settings open on port controls");
                            Shot(form, "settings-port-conflict");
                            Descendants(portTab).OfType<Button>().Single(b => b.Text == "Подобрать свободный").PerformClick();
                            Check(!portNumber.Enabled && !autoPort.Checked, "pick-free request preserves manual mode until save");
                            form.Close();
                        }
                        Snapshot(new HelpForm(delegate { }), "help");
                        Snapshot(new PinForm(true), "pin");
                        Snapshot(new EntryForm(new VaultEntry()), "entry");
                        Snapshot(new SshProfileEditorForm(null), "server");
                        Snapshot(new VaultForm(new VaultSession(VaultData.Empty(), "1234", false), clipboard, settings), "vault");
                        Snapshot(new BackupPickerForm(new List<BackupInfo>()), "backups");
                        using (var form = new MainWindow(settings, proxy, home, delegate { }))
                        {
                            form.Show(); Application.DoEvents(); form.Scale(new SizeF(1.5f, 1.5f)); Application.DoEvents(); Shot(form, "main-150"); form.Close();
                        }
                    }
                }
                Console.WriteLine("Desktop tests PASS: " + passed); return 0;
            }
            catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        }
        private static IEnumerable<Control> Descendants(Control c)
        {
            foreach (Control child in c.Controls) { yield return child; foreach (var nested in Descendants(child)) yield return nested; }
        }
        private static bool ApplyOnce(AutomationPlan plan, ProxyFeature feature, bool ready)
        {
            Exception error;
            return plan.TryApply(feature, ready, delegate { }, out error) == AutomationResult.Applied;
        }
        private static void AutomationRetries(SettingsService settings)
        {
            DateTime now = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var saved = settings.Current.Clone();
            var configured = AppSettings.Defaults(); configured.AutoApplyProxy = true; configured.AutoHttpProxyPort = false;
            var disabled = configured.Clone(); disabled.AutoApplyProxy = false;
            var occupied = Occupy(0); configured.HttpProxyPort = Number(occupied);
            var plan = new AutomationPlan(delegate { return now; }); plan.Update(null, configured);
            int calls = 0; bool failWrite = true; Exception error;
            string marker = Path.Combine(work, "automatic-apply.txt");
            try
            {
                settings.Save(configured);
                using (var bridge = new CliProxyBridgeService(settings))
                {
                    Action<ProxyFeature> apply = delegate {
                        calls++; string message;
                        if (!bridge.Start(out message)) throw new InvalidOperationException(message);
                        if (failWrite) throw new IOException("Test settings file is locked");
                        File.WriteAllText(marker, bridge.ProxyUrl);
                    };
                    Check(plan.TryApply(ProxyFeature.Terminal, false, apply, out error) == AutomationResult.None && calls == 0, "unready route never applies automatic settings");
                    Check(plan.TryApply(ProxyFeature.Terminal, true, apply, out error) == AutomationResult.RetryScheduled && error != null && calls == 1, "real occupied proxy port keeps its failed action pending");
                    Check(plan.GetStatusText(true).Contains("5 с") && settings.Current.AutoApplyProxy, "retry status is visible without changing the enabled preference");
                    for (int second = 1; second < 5; second++)
                    {
                        now = now.AddSeconds(1);
                        Check(plan.TryApply(ProxyFeature.Terminal, true, apply, out error) == AutomationResult.None && calls == 1, "timer cannot repeat before the deadline " + second);
                    }
                    plan.Update(configured, configured);
                    using (var probe = new TcpClient()) { probe.Connect(IPAddress.Loopback, Number(occupied)); Check(probe.Connected, "automation preserves the foreign occupied listener"); }
                    occupied.Stop(); now = now.AddSeconds(1);
                    Check(plan.TryApply(ProxyFeature.Terminal, true, apply, out error) == AutomationResult.RetryScheduled && calls == 2 && bridge.IsRunning && !File.Exists(marker), "released port advances to a write failure without losing the task");
                    Check(plan.GetStatusText(true).Contains("15 с"), "second failure increases the retry delay");
                    now = now.AddSeconds(15); failWrite = false;
                    Check(plan.TryApply(ProxyFeature.Terminal, true, apply, out error) == AutomationResult.Applied && error == null && calls == 3 && File.ReadAllText(marker) == bridge.ProxyUrl, "automatic setup completes after port and write faults clear without app restart");
                    Check(!ApplyOnce(plan, ProxyFeature.Terminal, true) && plan.GetStatusText(true) == "", "successful configuration is not repeated and clears retry status");
                }
            }
            finally { occupied.Stop(); settings.Save(saved); if (File.Exists(marker)) File.Delete(marker); }

            var retry = new AutomationPlan(delegate { return now; }); retry.Update(null, configured);
            calls = 0;
            Action<ProxyFeature> fail = delegate { calls++; throw new IOException("Test temporary fault"); };
            int[] delay = { 5, 15, 45 };
            for (int attempt = 0; attempt < 4; attempt++)
            {
                var result = retry.TryApply(ProxyFeature.Terminal, true, fail, out error);
                Check(result == (attempt < 3 ? AutomationResult.RetryScheduled : AutomationResult.Paused), "persistent fault has a bounded attempt " + attempt);
                if (attempt < 3) now = now.AddSeconds(delay[attempt]);
            }
            now = now.AddDays(1);
            Check(retry.TryApply(ProxyFeature.Terminal, true, fail, out error) == AutomationResult.None && calls == 4 && retry.GetStatusText(true).Contains("Проверьте настройки"), "exhausted attempts pause with guidance instead of an endless retry loop");
            retry.Update(configured, configured);
            Check(!ApplyOnce(retry, ProxyFeature.Terminal, true), "unrelated settings save cannot reset the retry budget");
            retry.Update(configured, disabled); retry.Update(disabled, configured);
            Check(ApplyOnce(retry, ProxyFeature.Terminal, true), "explicit automation off/on restarts a paused task");

            var manual = new AutomationPlan(delegate { return now; }); manual.Update(null, configured);
            manual.TryApply(ProxyFeature.Terminal, true, fail, out error); manual.Cancel(ProxyFeature.Terminal);
            now = now.AddMinutes(1); int beforeCancel = calls;
            Check(manual.TryApply(ProxyFeature.Terminal, true, fail, out error) == AutomationResult.None && calls == beforeCancel && manual.GetStatusText(true) == "", "manual off cancels a scheduled retry and its status");
            manual.Update(null, configured); manual.TryApply(ProxyFeature.Terminal, true, fail, out error);
            manual.Update(configured, disabled); now = now.AddMinutes(1);
            Check(!ApplyOnce(manual, ProxyFeature.Terminal, true), "unchecking automation cancels a failed pending task");

            var permanent = new AutomationPlan(delegate { return now; }); permanent.Update(null, configured);
            Check(permanent.TryApply(ProxyFeature.Terminal, true, delegate { throw new UnauthorizedAccessException("Test denied"); }, out error) == AutomationResult.Paused, "access denial asks for correction immediately");
            now = now.AddDays(1); Check(!ApplyOnce(permanent, ProxyFeature.Terminal, true), "known permanent error is never retried by the timer");
            foreach (var fault in new Exception[] { new ArgumentException("Test invalid setting"), new InvalidOperationException("Test wrapper", new System.Security.SecurityException("Test denied")) })
            {
                var blocked = new AutomationPlan(delegate { return now; }); blocked.Update(null, configured);
                Check(blocked.TryApply(ProxyFeature.Terminal, true, delegate { throw fault; }, out error) == AutomationResult.Paused, "known permanent fault pauses: " + fault.GetType().Name);
            }

            var both = configured.Clone(); both.AutoSystemProxy = true;
            var separate = new AutomationPlan(delegate { return now; }); separate.Update(null, both);
            separate.TryApply(ProxyFeature.Terminal, true, fail, out error);
            Check(ApplyOnce(separate, ProxyFeature.Windows, true), "failed terminal setup does not block another enabled action");

            var cancelled = new AutomationPlan(delegate { return now; }); cancelled.Update(null, configured);
            Check(cancelled.TryApply(ProxyFeature.Terminal, true, delegate { cancelled.Cancel(ProxyFeature.Terminal); throw new IOException("Test concurrent cancel"); }, out error) == AutomationResult.None && error == null && !ApplyOnce(cancelled, ProxyFeature.Terminal, true), "cancellation during application cannot resurrect its failed task");
            var reentrant = new AutomationPlan(delegate { return now; }); reentrant.Update(null, configured);
            Check(reentrant.TryApply(ProxyFeature.Terminal, true, delegate { Check(!ApplyOnce(reentrant, ProxyFeature.Terminal, true), "same task cannot run recursively"); }, out error) == AutomationResult.Applied, "outer application completes after reentrancy guard");
            var rearmed = new AutomationPlan(delegate { return now; }); rearmed.Update(null, configured);
            Check(rearmed.TryApply(ProxyFeature.Terminal, true, delegate {
                rearmed.Cancel(ProxyFeature.Terminal); rearmed.Update(disabled, configured);
            }, out error) == AutomationResult.None && ApplyOnce(rearmed, ProxyFeature.Terminal, true), "old completion cannot remove a newly rearmed task");
        }
        private static void Snapshot(Form form, string name, bool dispose = true)
        {
            try { form.Show(); Application.DoEvents(); Shot(form, name); Check(form.Visible, "native UI opens: " + name); }
            finally { if (dispose) { form.Close(); form.Dispose(); } }
        }
        private static void Shot(Form form, string name)
        {
            Application.DoEvents();
            using (var bitmap = new Bitmap(form.Width, form.Height)) { form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size)); bitmap.Save(Path.Combine(work, name + ".png")); }
        }
        private static void ScopedCodex()
        {
            var dir = Path.Combine(work, "codex fixture"); Directory.CreateDirectory(dir);
            var capture = Path.Combine(dir, "result.txt");
            File.WriteAllText(Path.Combine(dir, "codex.cmd"), "@echo off\r\necho %HTTP_PROXY%>\"%PROGO_TEST_RESULT%\"\r\necho %NO_PROXY%>>\"%PROGO_TEST_RESULT%\"\r\n", Encoding.ASCII);
            var launcher = Path.Combine(dir, "launch.cmd"); File.WriteAllText(launcher, CodexProxyService.LauncherContent(31881), Encoding.ASCII);
            var start = new ProcessStartInfo("cmd.exe", "/D /C \"\"" + launcher + "\"\"") { UseShellExecute = false, CreateNoWindow = true };
            start.EnvironmentVariables["PATH"] = dir + ";" + Environment.GetEnvironmentVariable("PATH");
            start.EnvironmentVariables["PROGO_TEST_RESULT"] = capture;
            string existing = Environment.GetEnvironmentVariable("HTTP_PROXY");
            using (var process = Process.Start(start)) { if (!process.WaitForExit(10000)) { process.Kill(); throw new Exception("Codex launcher stalled"); } Check(process.ExitCode == 0, "scoped Codex launcher executes local CLI"); }
            Check(File.ReadAllText(capture).Contains(CliProxyBridgeService.UrlFor(31881)) && Environment.GetEnvironmentVariable("HTTP_PROXY") == existing, "Codex gets proxy without changing parent environment");
        }
        private static void RestorePreferences()
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") { Console.WriteLine("SKIP: registry restoration checks require isolated CI"); return; }
            string[] names = { "HTTP_PROXY", "HTTPS_PROXY", "ALL_PROXY", "NO_PROXY" };
            var original = names.ToDictionary(n => n, n => Environment.GetEnvironmentVariable(n, EnvironmentVariableTarget.User));
            var backup = Path.Combine(AppPaths.Root, "proxy-environment-backup.json");
            if (File.Exists(backup) || File.Exists(SystemProxyService.BackupPath)) throw new Exception("CI proxy fixture is not isolated");
            try
            {
                Environment.SetEnvironmentVariable("HTTP_PROXY", "http://prior.example.org:8080", EnvironmentVariableTarget.User);
                bool shortcutExisted = File.Exists(CodexProxyService.LauncherPath);
                CodexProxyService.EnableOrdinaryLaunch(31881);
                Check(CliProxyEnvironmentService.IsAppliedToUserEnvironment(31881), "ordinary Codex launch receives user proxy environment");
                Check(File.Exists(CodexProxyService.LauncherPath) == shortcutExisted, "ordinary Codex setup does not require or create a shortcut");
                CliProxyEnvironmentService.MoveOwned(1881);
                Check(CliProxyEnvironmentService.IsAppliedToUserEnvironment(1881), "ordinary Codex settings follow the actual application port");
                CliProxyEnvironmentService.ApplyUserEnvironment(1881); CliProxyEnvironmentService.ApplyUserEnvironment(1881);
                Environment.SetEnvironmentVariable("HTTPS_PROXY", "http://other.example.org:8080", EnvironmentVariableTarget.User);
                CliProxyEnvironmentService.ClearUserEnvironmentIfOwned();
                Check(Environment.GetEnvironmentVariable("HTTP_PROXY", EnvironmentVariableTarget.User) == "http://prior.example.org:8080", "terminal preferences survive repeated apply/restore");
                Check(Environment.GetEnvironmentVariable("HTTPS_PROXY", EnvironmentVariableTarget.User) == "http://other.example.org:8080", "later external environment changes are preserved");
            }
            finally { foreach (var pair in original) Environment.SetEnvironmentVariable(pair.Key, pair.Value, EnvironmentVariableTarget.User); if (File.Exists(backup)) File.Delete(backup); }
            string error;
            if (!SystemProxyService.Apply(AppSettings.Defaults(), out error)) throw new Exception(error);
            try
            {
                Check(SystemProxyService.IsOwned && SystemProxyService.IsApplied(AppSettings.Defaults()), "Windows proxy ownership is tracked");
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Internet Settings", true))
                {
                    Check(Convert.ToString(key.GetValue("ProxyServer")) == "http=127.0.0.1:1881;https=127.0.0.1:1881", "Windows uses tested HTTP/CONNECT bridge");
                    key.SetValue("ProxyServer", "other.example.org:8080");
                    Check(!SystemProxyService.IsOwned, "exit does not own another application's Windows proxy");
                }
            }
            finally { if (!SystemProxyService.Restore(out error)) throw new Exception(error); }
            Check(!File.Exists(SystemProxyService.BackupPath), "successful Windows restore clears its backup");
            CodexProxyService.Enable(31881);
            try { Check(CodexProxyService.IsConfigured, "Codex Start Menu shortcut created"); }
            finally { CodexProxyService.Disable(); }
            Check(!CodexProxyService.IsConfigured, "Codex manual off removes owned launcher");
        }

        private static TcpListener Occupy(int port)
        {
            var listener = new TcpListener(IPAddress.Loopback, port); listener.ExclusiveAddressUse = true; listener.Start(); return listener;
        }
        private static int Number(TcpListener listener) { return ((IPEndPoint)listener.LocalEndpoint).Port; }
        private static void AssertBridge(int port)
        {
            using (var client = new TcpClient())
            {
                client.Connect(IPAddress.Loopback, port); client.ReceiveTimeout = 3000;
                Write(client.GetStream(), "BAD\r\n\r\n");
                Check(ReadHeader(client.GetStream()).Contains("400"), "selected port accepts HTTP requests " + port);
            }
        }
        private static void ProxyPorts(SettingsService settings)
        {
            var occupied = Occupy(0);
            try
            {
                var s = settings.Current.Clone(); s.HttpProxyPort = Number(occupied); s.AutoHttpProxyPort = true; settings.Save(s);
                using (var bridge = new CliProxyBridgeService(settings))
                {
                    string error; Check(bridge.Start(out error), "auto start succeeds with occupied preferred port");
                    int selected = bridge.Port;
                    Check(selected != Number(occupied) && selected == settings.Load().HttpProxyPort, "fallback is bound and persisted");
                    AssertBridge(selected);
                    Check(bridge.Start(out error) && bridge.Port == selected, "healthy proxy retains its port");
                    using (var connection = new TcpClient()) { connection.Connect(IPAddress.Loopback, Number(occupied)); Check(connection.Connected, "occupied listener remains untouched"); }
                    var before = File.ReadAllText(AppPaths.SettingsPath);
                    s = settings.Current.Clone(); s.AutoHttpProxyPort = false; s.HttpProxyPort = Number(occupied);
                    Check(!bridge.Reconfigure(s, false, out error) && error.Contains("Подобрать свободный"), "fixed conflict has an actionable error");
                    Check(bridge.IsRunning && bridge.Port == selected && File.ReadAllText(AppPaths.SettingsPath) == before, "failed change preserves running listener and settings");
                    AssertBridge(selected);
                    s = settings.Current.Clone(); s.AutoHttpProxyPort = false;
                    Check(bridge.Reconfigure(s, true, out error) && bridge.Port != selected && !settings.Current.AutoHttpProxyPort, "explicit free-port selection keeps fixed mode");
                    selected = bridge.Port; AssertBridge(selected);
                    bridge.Stop(); Check(!bridge.IsRunning, "manual stop does not revive proxy");
                    Check(bridge.Start(out error) && bridge.Port == selected, "restart reuses saved available port");
                    bridge.Stop();
                    var blocker = Occupy(selected);
                    try
                    {
                        Check(!bridge.Start(out error) && !bridge.IsRunning, "manual mode never silently changes a busy port");
                        s = settings.Current.Clone(); s.AutoHttpProxyPort = true; settings.Save(s);
                        Check(bridge.Start(out error) && bridge.Port != selected, "next automatic start recovers from a newly occupied port");
                    }
                    finally { blocker.Stop(); }
                }
            }
            finally { occupied.Stop(); }
        }

        private static void PortIntegrations(SettingsService settings)
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") { Console.WriteLine("SKIP: port integration registry checks require isolated CI"); return; }
            string[] names = CliProxyEnvironmentService.Names;
            var originals = names.ToDictionary(n => n, n => Environment.GetEnvironmentVariable(n, EnvironmentVariableTarget.User));
            string error;
            using (var bridge = new CliProxyBridgeService(settings))
            {
                Check(bridge.Start(out error), "integration listener starts");
                try
                {
                    // Exercise migration from the 0.2.0 plain-dictionary backup.
                    var legacy = names.ToDictionary(n => n, n => (string)null);
                    legacy["HTTP_PROXY"] = "http://original.example.org:8080";
                    File.WriteAllText(CliProxyEnvironmentService.BackupPath, new JavaScriptSerializer().Serialize(legacy));
                    foreach (var name in names) Environment.SetEnvironmentVariable(name, name == "NO_PROXY" ? "localhost,127.0.0.1,::1" : CliProxyBridgeService.UrlFor(1881), EnvironmentVariableTarget.User);
                    CliProxyEnvironmentService.MoveOwned(bridge.Port);
                    Check(CliProxyEnvironmentService.IsAppliedToUserEnvironment(bridge.Port), "legacy environment ownership migrates from default port");
                    Environment.SetEnvironmentVariable("HTTPS_PROXY", "http://external.example.org:8080", EnvironmentVariableTarget.User);
                    Check(SystemProxyService.Apply(settings.Current, out error), "Windows integration enabled for actual port");
                    CodexProxyService.Enable(bridge.Port);
                    int oldPort = bridge.Port;
                    Check(bridge.Reconfigure(settings.Current.Clone(), true, out error), "owned integrations move with listener");
                    Check(bridge.Port != oldPort && Environment.GetEnvironmentVariable("HTTP_PROXY", EnvironmentVariableTarget.User) == bridge.ProxyUrl, "environment follows bound endpoint");
                    Check(Environment.GetEnvironmentVariable("HTTPS_PROXY", EnvironmentVariableTarget.User) == "http://external.example.org:8080", "port move preserves external environment value");
                    Check(SystemProxyService.IsApplied(settings.Current) && File.ReadAllText(CodexProxyService.LauncherPath).Contains(bridge.ProxyUrl), "Windows and Codex use same actual endpoint");
                    var launcher = File.ReadAllText(CodexProxyService.LauncherPath);
                    var envBackup = File.ReadAllText(CliProxyEnvironmentService.BackupPath);
                    var windowsBackup = File.ReadAllText(SystemProxyService.BackupPath);
                    oldPort = bridge.Port;
                    // File.Replace must fail while another process denies delete/replace access.
                    using (var lockedSettings = File.Open(AppPaths.SettingsPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                        Check(!bridge.Reconfigure(settings.Current.Clone(), true, out error), "save failure aborts port transaction");
                    Check(bridge.Port == oldPort && settings.Load().HttpProxyPort == oldPort, "save failure retains previous listener and persisted port");
                    Check(Environment.GetEnvironmentVariable("HTTP_PROXY", EnvironmentVariableTarget.User) == bridge.ProxyUrl && SystemProxyService.IsApplied(settings.Current), "save failure restores dependent environment and Windows settings");
                    Check(File.ReadAllText(CodexProxyService.LauncherPath) == launcher && File.ReadAllText(CliProxyEnvironmentService.BackupPath) == envBackup && File.ReadAllText(SystemProxyService.BackupPath) == windowsBackup, "save failure restores launcher and original backups exactly");
                    AssertBridge(oldPort);
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Internet Settings", true))
                    {
                        key.SetValue("ProxyServer", "external.example.org:8080");
                        File.WriteAllText(CodexProxyService.LauncherPath, "@echo external launcher");
                        Check(bridge.Reconfigure(settings.Current.Clone(), true, out error), "port can change with external integrations");
                        Check(Convert.ToString(key.GetValue("ProxyServer")) == "external.example.org:8080" && File.ReadAllText(CodexProxyService.LauncherPath) == "@echo external launcher", "port change does not overwrite external Windows or Codex configuration");
                    }
                    File.WriteAllText(CodexProxyService.LauncherPath, launcher);
                    CliProxyEnvironmentService.ClearUserEnvironmentIfOwned();
                    Check(Environment.GetEnvironmentVariable("HTTP_PROXY", EnvironmentVariableTarget.User) == "http://original.example.org:8080", "multiple port moves preserve original environment backup");
                    Check(Environment.GetEnvironmentVariable("HTTPS_PROXY", EnvironmentVariableTarget.User) == "http://external.example.org:8080", "restore keeps external changes after port moves");
                }
                finally
                {
                    foreach (var pair in originals) Environment.SetEnvironmentVariable(pair.Key, pair.Value, EnvironmentVariableTarget.User);
                    if (File.Exists(CliProxyEnvironmentService.BackupPath)) File.Delete(CliProxyEnvironmentService.BackupPath);
                    SystemProxyService.Restore(out error);
                    CodexProxyService.Disable();
                }
            }
        }

        private static string ReadHeader(NetworkStream stream)
        {
            var text = new StringBuilder(); int value;
            while ((value = stream.ReadByte()) >= 0) { text.Append((char)value); if (text.ToString().EndsWith("\r\n\r\n")) return text.ToString(); }
            throw new IOException("No HTTP header");
        }
        private static void Write(NetworkStream stream, string text) { var bytes = Encoding.ASCII.GetBytes(text); stream.Write(bytes, 0, bytes.Length); }
        private static void BridgeRoundTrip(SettingsService settings, bool connect)
        {
            var socks = new TcpListener(IPAddress.Loopback, 0); socks.Start();
            settings.Current.SocksHost = "127.0.0.1"; settings.Current.SocksPort = ((IPEndPoint)socks.LocalEndpoint).Port;
            var server = Task.Run(delegate
            {
                using (var socket = socks.AcceptTcpClient())
                {
                    socket.ReceiveTimeout = 5000; var stream = socket.GetStream();
                    for (int i = 0; i < 3; i++) if (stream.ReadByte() < 0) throw new IOException();
                    stream.Write(new byte[] { 5, 0 }, 0, 2);
                    for (int i = 0; i < 3; i++) stream.ReadByte();
                    int type = stream.ReadByte(); int count = type == 3 ? stream.ReadByte() : type == 1 ? 4 : 16;
                    for (int i = 0; i < count + 2; i++) stream.ReadByte();
                    stream.Write(new byte[] { 5, 0, 0, 1, 127, 0, 0, 1, 0, 80 }, 0, 10);
                    if (connect) { for (int i = 0; i < 4; i++) stream.ReadByte(); Write(stream, "pong"); }
                    else { string request = ReadHeader(stream); if (!request.StartsWith("GET /demo HTTP/1.1")) throw new Exception("bad HTTP rewrite"); Write(stream, "HTTP/1.1 200 OK\r\nContent-Length: 5\r\nConnection: close\r\n\r\nhello"); }
                }
            });
            try
            {
                using (var bridge = new CliProxyBridgeService(settings))
                {
                    string error; if (!bridge.Start(out error)) throw new Exception(error);
                    using (var client = new TcpClient())
                    {
                        client.Connect(CliProxyBridgeService.Host, bridge.Port); client.ReceiveTimeout = 7000; var stream = client.GetStream();
                        Write(stream, connect ? "CONNECT example.org:443 HTTP/1.1\r\n\r\n" : "GET http://example.org/demo HTTP/1.1\r\nHost: example.org\r\n\r\n");
                        if (connect) { Check(ReadHeader(stream).Contains("200"), "CONNECT handshake succeeds"); Write(stream, "ping"); }
                        string response = new StreamReader(stream).ReadToEnd();
                        Check(response.Contains(connect ? "pong" : "hello"), connect ? "CONNECT duplex traffic uses SOCKS" : "plain HTTP traffic uses SOCKS");
                    }
                    if (!server.Wait(10000)) throw new Exception("SOCKS fixture stalled");
                }
            }
            finally { socks.Stop(); }
        }
    }
}

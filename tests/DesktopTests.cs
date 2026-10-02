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
                        Check(!plan.Take(feature, false), "automation waits for a ready route " + mask + "/" + feature);
                        bool enabled = AutomationPlan.Enabled(s, feature);
                        Check(plan.Take(feature, true) == enabled && !plan.Take(feature, true), "independent one-time startup option " + mask + "/" + feature);
                    }
                }
                var before = AppSettings.Defaults(); var after = AppSettings.Defaults(); after.AutoSystemProxy = true;
                var changes = new AutomationPlan(); changes.Update(before, after); changes.Cancel(ProxyFeature.Windows);
                changes.Update(after, after); Check(!changes.Take(ProxyFeature.Windows, true), "manual off survives timer and unrelated save");
                changes.Update(after, before); changes.Update(before, after); Check(changes.Take(ProxyFeature.Windows, true), "explicit off/on re-arms automation");
                changes.Update(null, after); changes.Update(after, before); Check(!changes.Take(ProxyFeature.Windows, true), "unchecked option cancels a pending application");
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

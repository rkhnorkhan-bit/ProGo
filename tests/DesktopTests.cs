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
                ScopedCodex();
                RestorePreferences();
                using (var settings = new SettingsService())
                {
                    settings.Current.SshProfile = "my-vps";
                    BridgeRoundTrip(settings, false); BridgeRoundTrip(settings, true);
                    Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException); Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
                    using (var proxy = new ProxyService(settings))
                    using (var relay = new Ikev2RelayService())
                    using (var home = new HomeVpnService(relay))
                    using (var clipboard = new ClipboardService(settings))
                    {
                        Snapshot(new MainWindow(settings, proxy, home, delegate { }), "main");
                        using (var form = new SshProfilesSettingsForm(settings))
                        {
                            Snapshot(form, "settings", false);
                            var tabs = Descendants(form).OfType<TabControl>().Single();
                            Check(Descendants(tabs.TabPages[0]).OfType<CheckBox>().Count() == 4, "four clearly separated automatic options");
                            var flow = Descendants(tabs.TabPages[0]).OfType<FlowLayoutPanel>().First();
                            flow.AutoScrollPosition = new Point(0, 1000); Shot(form, "settings-bottom");
                            for (int i = 1; i < tabs.TabCount; i++) { tabs.SelectedIndex = i; Application.DoEvents(); Shot(form, "settings-" + i); }
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
            var launcher = Path.Combine(dir, "launch.cmd"); File.WriteAllText(launcher, CodexProxyService.LauncherContent(), Encoding.ASCII);
            var start = new ProcessStartInfo("cmd.exe", "/D /C \"\"" + launcher + "\"\"") { UseShellExecute = false, CreateNoWindow = true };
            start.EnvironmentVariables["PATH"] = dir + ";" + Environment.GetEnvironmentVariable("PATH");
            start.EnvironmentVariables["PROGO_TEST_RESULT"] = capture;
            string existing = Environment.GetEnvironmentVariable("HTTP_PROXY");
            using (var process = Process.Start(start)) { if (!process.WaitForExit(10000)) { process.Kill(); throw new Exception("Codex launcher stalled"); } Check(process.ExitCode == 0, "scoped Codex launcher executes local CLI"); }
            Check(File.ReadAllText(capture).Contains(CliProxyEnvironmentService.ProxyUrl) && Environment.GetEnvironmentVariable("HTTP_PROXY") == existing, "Codex gets proxy without changing parent environment");
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
                CliProxyEnvironmentService.ApplyUserEnvironment(); CliProxyEnvironmentService.ApplyUserEnvironment();
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
            CodexProxyService.Enable();
            try { Check(CodexProxyService.IsConfigured, "Codex Start Menu shortcut created"); }
            finally { CodexProxyService.Disable(); }
            Check(!CodexProxyService.IsConfigured, "Codex manual off removes owned launcher");
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
                        client.Connect(CliProxyBridgeService.Host, CliProxyBridgeService.Port); client.ReceiveTimeout = 7000; var stream = client.GetStream();
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

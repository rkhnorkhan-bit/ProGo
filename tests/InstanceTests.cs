using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Web.Script.Serialization;

namespace ProGo
{
    internal static class InstanceTests
    {
        private static int passed;
        private static void Check(bool value, string name) { if (!value) throw new Exception(name); passed++; Console.WriteLine("PASS: " + name); }
        private delegate bool WindowVisitor(IntPtr window, IntPtr state);
        [DllImport("user32.dll")] private static extern bool EnumWindows(WindowVisitor callback, IntPtr state);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr window, StringBuilder text, int count);
        [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr window);
        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr window, int command);

        private static void Wait(Func<bool> condition, string name)
        {
            var deadline = DateTime.UtcNow.AddSeconds(10);
            while (!condition())
            {
                if (DateTime.UtcNow >= deadline) throw new Exception("Timed out: " + name);
                System.Threading.Thread.Sleep(30);
            }
        }
        private static Process Start(string executable, string arguments)
        {
            return Process.Start(new ProcessStartInfo(executable, arguments) { UseShellExecute = false, CreateNoWindow = true });
        }
        private static int Exit(Process process)
        {
            using (process)
            {
                if (!process.WaitForExit(7000)) { process.Kill(); throw new Exception("Secondary launch did not exit"); }
                return process.ExitCode;
            }
        }
        private static string Snapshot()
        {
            if (!Directory.Exists(AppPaths.Root)) return "";
            using (var hash = SHA256.Create())
                return String.Join("\n", Directory.GetFiles(AppPaths.Root, "*", SearchOption.AllDirectories).OrderBy(p => p)
                    .Select(p => p + ":" + Convert.ToBase64String(hash.ComputeHash(File.ReadAllBytes(p)))).ToArray());
        }
        private static IntPtr[] Windows(int pid)
        {
            var found = new System.Collections.Generic.List<IntPtr>();
            EnumWindows(delegate(IntPtr window, IntPtr state) {
                uint owner; GetWindowThreadProcessId(window, out owner);
                var title = new StringBuilder(256); GetWindowText(window, title, title.Capacity);
                if (owner == pid && title.ToString() == "ProGo · Ваше подключение") found.Add(window);
                return true;
            }, IntPtr.Zero);
            return found.ToArray();
        }
        private static bool PortOpen(int port)
        {
            try { using (var client = new TcpClient()) { client.Connect(IPAddress.Loopback, port); return true; } }
            catch (SocketException) { return false; }
        }
        private static int AppliedPort()
        {
            try
            {
                var settings = SettingsService.DeserializeSettings(File.ReadAllText(AppPaths.SettingsPath));
                return CliProxyEnvironmentService.IsAppliedToUserEnvironment(settings.HttpProxyPort) && PortOpen(settings.HttpProxyPort) ? settings.HttpProxyPort : 0;
            }
            catch (IOException) { return 0; }
        }
        private static string ReadProxySnapshot(string path)
        {
            // Keep the opened version immutable while allowing the application to
            // atomically replace its receipt without this fixture blocking it.
            using (var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete))
            using (var reader = new StreamReader(input, Encoding.UTF8, true)) return reader.ReadToEnd();
        }
        private static bool TryCaptureProxyBaseline(string settingsPath, string backupPath, int expectedPort,
            out string settingsText, out string backupText, out int sharingCode, Action beforeReceiptVerification = null)
        {
            settingsText = backupText = null; sharingCode = 0;
            string stage = "settings";
            try {
                string settings = ReadProxySnapshot(settingsPath);
                if (SettingsService.DeserializeSettings(settings).HttpProxyPort != expectedPort) return false;
                stage = "receipt";
                string backup = ReadProxySnapshot(backupPath);
                var saved = new JavaScriptSerializer().Deserialize<Dictionary<string, string>>(backup);
                string owned; int port;
                if (saved == null || CliProxyEnvironmentService.Names.Any(name => !saved.ContainsKey(name)) ||
                    !saved.TryGetValue("ProGoAppliedPort", out owned) || !Int32.TryParse(owned, out port) || port < 1 || port > 65535)
                    throw new IOException("Instance fixture received an invalid proxy ownership receipt.");
                if (port != expectedPort || CliProxyEnvironmentService.ReadTypedCorrections(saved).Count != 0) return false;
                stage = "settings verification";
                if (ReadProxySnapshot(settingsPath) != settings) return false;
                stage = "receipt verification";
                if (beforeReceiptVerification != null) beforeReceiptVerification();
                if (ReadProxySnapshot(backupPath) != backup) return false;
                settingsText = settings; backupText = backup; return true;
            } catch (FileNotFoundException ex) {
                // AppliedPort is only a preliminary observation, not an atomic
                // readiness receipt. Keep waiting within the existing deadline
                // if our receipt is not yet published or disappears on recheck.
                // Missing settings or a different file remain real failures.
                if ((stage != "receipt" && stage != "receipt verification") || ex.FileName == null ||
                    !String.Equals(Path.GetFullPath(ex.FileName), Path.GetFullPath(backupPath), StringComparison.OrdinalIgnoreCase)) throw;
                Console.WriteLine("INFO: instance baseline " + stage + " waits for its ownership receipt.");
                return false;
            } catch (IOException ex) {
                int code = ex.HResult & 0xffff;
                if (code != 32 && code != 33) throw;
                sharingCode = code;
                Console.WriteLine("INFO: instance baseline " + stage + " waits for native sharing/lock code " + code + ".");
                return false;
            }
        }
        private static void ProxyBaselineNativeReadiness()
        {
            string root = Path.Combine(Path.GetTempPath(), "ProGo-instance-baseline-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try {
                string settingsPath = Path.Combine(root, "settings.json"), backupPath = Path.Combine(root, "receipt.json");
                var settings = AppSettings.Defaults(); settings.HttpProxyPort = 1887;
                var json = new JavaScriptSerializer(); string settingsBytes = json.Serialize(settings);
                File.WriteAllText(settingsPath, settingsBytes);
                var receipt = CliProxyEnvironmentService.Names.ToDictionary(name => name, name => (string)null);
                receipt["ProGoAppliedPort"] = "1887"; receipt["FixtureVersion"] = "old";
                string oldReceipt = json.Serialize(receipt); File.WriteAllText(backupPath, oldReceipt);
                receipt["FixtureVersion"] = "new"; string newReceipt = json.Serialize(receipt);
                string capturedSettings, capturedBackup; int sharing;
                using (var held = new FileStream(backupPath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete)) {
                    string staged = backupPath + ".staging"; File.WriteAllText(staged, newReceipt);
                    File.Replace(staged, backupPath, null);
                    string heldBytes;
                    using (var reader = new StreamReader(held, Encoding.UTF8, true, 1024, true)) heldBytes = reader.ReadToEnd();
                    bool ready = TryCaptureProxyBaseline(settingsPath, backupPath, 1887, out capturedSettings, out capturedBackup, out sharing);
                    Check(ready && sharing == 0 && capturedSettings == settingsBytes && capturedBackup == newReceipt && heldBytes == oldReceipt,
                        "instance baseline reader permits real atomic receipt replacement while its old snapshot remains immutable");
                }
                receipt["ProGoAppliedPort"] = "1888"; File.WriteAllText(backupPath, json.Serialize(receipt));
                bool wrongPortRejected = !TryCaptureProxyBaseline(settingsPath, backupPath, 1887, out capturedSettings, out capturedBackup, out sharing) &&
                    capturedSettings == null && capturedBackup == null && sharing == 0;
                receipt["ProGoAppliedPort"] = "1887";
                receipt["ProGoPendingWindowsCorrections"] = json.Serialize(new[] { new WindowsProxyFieldBackup {
                    Name = "ProxyServer", Pending = true,
                    Original = WindowsProxyValue.From("fixture", Microsoft.Win32.RegistryValueKind.ExpandString),
                    Applied = WindowsProxyValue.From("fixture", Microsoft.Win32.RegistryValueKind.String)
                } });
                File.WriteAllText(backupPath, json.Serialize(receipt));
                bool pendingRejected = !TryCaptureProxyBaseline(settingsPath, backupPath, 1887, out capturedSettings, out capturedBackup, out sharing) &&
                    capturedSettings == null && capturedBackup == null && sharing == 0;
                File.WriteAllText(backupPath, newReceipt);
                bool locked;
                using (var writer = new FileStream(backupPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                    locked = !TryCaptureProxyBaseline(settingsPath, backupPath, 1887, out capturedSettings, out capturedBackup, out sharing) &&
                        (sharing == 32 || sharing == 33) && capturedSettings == null && capturedBackup == null;
                Wait(() => TryCaptureProxyBaseline(settingsPath, backupPath, 1887, out capturedSettings, out capturedBackup, out sharing),
                    "released settled proxy receipt");
                Check(wrongPortRejected && pendingRejected && locked && sharing == 0 && capturedSettings == settingsBytes && capturedBackup == newReceipt,
                    "instance readiness rejects wrong port pending corrections and a native writer lock before accepting its released complete receipt");
                File.Delete(backupPath);
                bool missingBefore = !TryCaptureProxyBaseline(settingsPath, backupPath, 1887, out capturedSettings, out capturedBackup, out sharing) &&
                    capturedSettings == null && capturedBackup == null && sharing == 0;
                File.WriteAllText(backupPath, newReceipt);
                bool missingDuring = !TryCaptureProxyBaseline(settingsPath, backupPath, 1887, out capturedSettings, out capturedBackup, out sharing,
                    () => File.Delete(backupPath)) && capturedSettings == null && capturedBackup == null && sharing == 0;
                bool stillMissing = Enumerable.Range(0, 3).All(attempt =>
                    !TryCaptureProxyBaseline(settingsPath, backupPath, 1887, out capturedSettings, out capturedBackup, out sharing) &&
                    capturedSettings == null && capturedBackup == null && sharing == 0);
                File.WriteAllText(backupPath, newReceipt);
                Wait(() => TryCaptureProxyBaseline(settingsPath, backupPath, 1887, out capturedSettings, out capturedBackup, out sharing),
                    "published complete proxy receipt after absence");
                Check(missingBefore && missingDuring && stillMissing && capturedSettings == settingsBytes && capturedBackup == newReceipt && sharing == 0,
                    "instance baseline never accepts an absent or disappearing receipt and waits for its published matching bytes");
                File.WriteAllText(backupPath, "{}"); bool invalidFailed = false, foreignMissingFailed = false, settingsMissingFailed = false;
                try { TryCaptureProxyBaseline(settingsPath, backupPath, 1887, out capturedSettings, out capturedBackup, out sharing); }
                catch (IOException ex) { invalidFailed = !(ex is FileNotFoundException); }
                File.WriteAllText(backupPath, newReceipt);
                try { TryCaptureProxyBaseline(settingsPath, backupPath, 1887, out capturedSettings, out capturedBackup, out sharing,
                    () => { throw new FileNotFoundException("Unrelated fixture file", Path.Combine(root, "unrelated.json")); }); }
                catch (FileNotFoundException) { foreignMissingFailed = true; }
                string heldSettings = settingsPath + ".held"; File.Move(settingsPath, heldSettings);
                try { TryCaptureProxyBaseline(settingsPath, backupPath, 1887, out capturedSettings, out capturedBackup, out sharing); }
                catch (FileNotFoundException) { settingsMissingFailed = true; }
                finally { File.Move(heldSettings, settingsPath); }
                Check(invalidFailed && foreignMissingFailed && settingsMissingFailed,
                    "instance readiness still fails malformed receipts missing settings and unrelated I/O instead of treating them as startup readiness");
            } finally { Directory.Delete(root, true); }
        }
        private static void Stop(Process process)
        {
            if (process == null) return;
            if (!process.HasExited) { process.Kill(); process.WaitForExit(5000); }
            process.Dispose();
        }
        [STAThread]
        private static int Main(string[] args)
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true")
            { Console.WriteLine("SKIP: instance integration requires isolated Windows CI"); return 0; }
            string installed = Path.Combine(AppPaths.Root, "ProGo.exe");
            Process primary = null, replacement = null;
            var originalSettings = File.Exists(AppPaths.SettingsPath) ? File.ReadAllBytes(AppPaths.SettingsPath) : null;
            var originals = CliProxyEnvironmentService.Names.ToDictionary(n => n, n => Environment.GetEnvironmentVariable(n, EnvironmentVariableTarget.User));
            var listener = new TcpListener(IPAddress.Loopback, 0);
            if (File.Exists(installed) || Process.GetProcessesByName("ProGo").Length != 0 || File.Exists(CliProxyEnvironmentService.BackupPath))
                throw new Exception("Instance fixture is not isolated");
            try
            {
                ProxyBaselineNativeReadiness();
                AppPaths.EnsureDirectories(); File.Copy(args[0], installed);
                string scriptRoot = args[1];
                string powershell = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "WindowsPowerShell", "v1.0", "powershell.exe");
                var snapshot = Snapshot();
                using (var owner = new ApplicationInstance())
                {
                    Check(owner.IsOwner, "first launch owns the per-user lifetime mutex");
                    Check(Exit(Start(installed, "--show")) == 0, "secondary activation succeeds while the owner is still starting");
                    int activations = 0;
                    owner.Attach(delegate { System.Threading.Interlocked.Increment(ref activations); });
                    Check(activations == 1, "startup activation is queued until the UI attaches");
                    foreach (var arguments in new[] { "", "--show", "/show", "show" })
                        Check(Exit(Start(installed, arguments)) == 0, "normal and compatible show launches reuse the owner: " + arguments);
                    Wait(() => activations == 5, "normal activation callbacks");
                    var parallel = Enumerable.Range(0, 4).Select(i => Start(installed, "--show")).ToArray();
                    foreach (var child in parallel) Check(Exit(child) == 0, "concurrent secondary launch exits after activation");
                    Wait(() => activations == 9, "concurrent activation callbacks");
                    foreach (var script in new[] { "Start-ProGo.ps1", "Show-ProGo.ps1" })
                        Check(Exit(Start(powershell, "-NoProfile -File \"" + Path.Combine(scriptRoot, script) + "\"")) == 0, "launcher script reuses existing owner: " + script);
                    Wait(() => activations == 11, "script activation callbacks");
                    Check(Exit(Start(installed, "--self-check")) == 3, "secondary self-check refuses shared state without creating services");
                    using (var client = new NamedPipeClientStream(".", "ProGo.Show." + WindowsIdentity.GetCurrent().User.Value, PipeDirection.InOut))
                    {
                        client.Connect(3000); client.WriteByte(99);
                        Check(client.ReadByte() == 0, "IPC rejects unknown commands");
                    }
                    int cleanupCalls = 0, completedCalls = 0;
                    owner.Attach(delegate { System.Threading.Interlocked.Increment(ref activations); },
                        () => { cleanupCalls++; return false; }, () => completedCalls++);
                    using (var refused = new NamedPipeClientStream(".", "ProGo.Show." + WindowsIdentity.GetCurrent().User.Value, PipeDirection.InOut)) {
                        refused.Connect(3000); refused.WriteByte(2);
                        Check(refused.ReadByte() == 0 && cleanupCalls == 1 && completedCalls == 0, "IPC refusal cannot acknowledge or schedule shutdown");
                    }
                    owner.Attach(delegate { System.Threading.Interlocked.Increment(ref activations); }, () => true, () => completedCalls++);
                    using (var accepted = new NamedPipeClientStream(".", "ProGo.Show." + WindowsIdentity.GetCurrent().User.Value, PipeDirection.InOut)) {
                        accepted.Connect(3000); accepted.WriteByte(2);
                        Check(accepted.ReadByte() == 2, "IPC confirms only completed cleanup");
                    }
                    Wait(() => completedCalls == 1, "completion follows cleanup acknowledgement");
                    using (var idle = new NamedPipeClientStream(".", "ProGo.Show." + WindowsIdentity.GetCurrent().User.Value, PipeDirection.InOut))
                    {
                        idle.Connect(3000);
                        Check(idle.ReadByte() == 0, "idle IPC client is timed out without blocking the owner indefinitely");
                    }
                    Check(Exit(Start(installed, "--show")) == 0, "activation remains available after malformed and idle clients");
                    Wait(() => activations == 12, "activation after invalid clients");
                    Check(Snapshot() == snapshot, "secondary launches leave settings, snapshots, backups and logs unchanged");
                }
                Check(Exit(Start(installed, "--self-check")) == 0, "clean owner disposal releases the mutex for a later launch");

                listener.Start();
                System.Threading.Tasks.Task.Run(async delegate {
                    try {
                        while (true) {
                            using (var client = await listener.AcceptTcpClientAsync()) {
                                var stream = client.GetStream(); stream.ReadTimeout = 1500;
                                if (stream.ReadByte() == 5 && stream.ReadByte() == 1 && stream.ReadByte() == 0)
                                    stream.Write(new byte[] { 5, 0 }, 0, 2);
                            }
                        }
                    } catch (SocketException) { } catch (ObjectDisposedException) { }
                });
                var configured = AppSettings.Defaults(); configured.AutoCliProxy = true; configured.AutoRestartSocks = false;
                configured.SocksPort = ((IPEndPoint)listener.LocalEndpoint).Port;
                configured.TestEndpoint = "http://127.0.0.1:1/";
                using (var settings = new SettingsService()) settings.Save(configured);
                primary = Start(installed, "");
                int port = 0, sharingCode;
                string settingsBefore = null, backupBefore = null;
                Wait(delegate {
                    if (primary.HasExited) return false;
                    int applied = AppliedPort(); string observedSettings, observedBackup;
                    if (applied == 0 || !TryCaptureProxyBaseline(AppPaths.SettingsPath, CliProxyEnvironmentService.BackupPath,
                        applied, out observedSettings, out observedBackup, out sharingCode) || AppliedPort() != applied) return false;
                    port = applied; settingsBefore = observedSettings; backupBefore = observedBackup; return true;
                }, "actual primary proxy startup with a settled ownership baseline");
                Check(Exit(Start(installed, "--show")) == 0, "actual repeated launch activates the existing application");
                Wait(() => Windows(primary.Id).Length == 1, "primary dashboard activation");
                var window = Windows(primary.Id).Single(); ShowWindow(window, 6);
                Wait(() => IsIconic(window), "dashboard minimize");
                Check(Exit(Start(installed, "--show")) == 0, "repeat show exits without another UI owner");
                Wait(() => !IsIconic(window), "dashboard restored from minimized state");
                Check(Windows(primary.Id).Length == 1 && Process.GetProcessesByName("ProGo").Count(p => !p.HasExited) == 1, "one persistent process and one dashboard remain");
                Check(!primary.HasExited && AppliedPort() == port && ReadProxySnapshot(AppPaths.SettingsPath) == settingsBefore &&
                    ReadProxySnapshot(CliProxyEnvironmentService.BackupPath) == backupBefore,
                    "exiting secondary launch preserves the primary proxy and its original settings backup");
                Stop(primary); primary = null;
                replacement = Start(installed, "--show");
                Wait(() => !replacement.HasExited && AppliedPort() > 0 && Windows(replacement.Id).Length == 1, "recovery after crashed owner");
                Check(Process.GetProcessesByName("ProGo").Count(p => !p.HasExited) == 1, "abandoned mutex permits a new owner after a crash");
                Console.WriteLine("Instance tests PASS: " + passed); return 0;
            }
            catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
            finally
            {
                Stop(primary); Stop(replacement); listener.Stop();
                CliProxyEnvironmentService.ClearUserEnvironmentIfOwned();
                foreach (var pair in originals) Environment.SetEnvironmentVariable(pair.Key, pair.Value, EnvironmentVariableTarget.User);
                if (originalSettings != null) File.WriteAllBytes(AppPaths.SettingsPath, originalSettings);
                else if (File.Exists(AppPaths.SettingsPath)) File.Delete(AppPaths.SettingsPath);
                File.Delete(installed);
            }
        }
    }
}

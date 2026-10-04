using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        private const string ProxyRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings";
        private static void WindowsOwnedRestoration(SettingsService settings)
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") { Console.WriteLine("SKIP: owned Windows restore needs isolated CI"); return; }
            var original = SystemProxyService.ReadCurrent();
            var configuration = settings.Current.Clone();
            var environment = CliProxyEnvironmentService.Names.ToDictionary(n => n, n => Environment.GetEnvironmentVariable(n, EnvironmentVariableTarget.User));
            if (File.Exists(SystemProxyService.BackupPath)) throw new Exception("Windows restore fixture is not isolated");
            try {
                using (var key = Registry.CurrentUser.CreateSubKey(ProxyRegistryPath)) {
                    key.SetValue("ProxyEnable", 0, RegistryValueKind.DWord);
                    key.SetValue("ProxyServer", "prior.example.org:8080", RegistryValueKind.String);
                    key.SetValue("ProxyOverride", "%USERPROFILE%;prior.example.org", RegistryValueKind.ExpandString);
                    key.SetValue("AutoConfigURL", "https://prior.example.org/%USERNAME%/proxy.pac", RegistryValueKind.ExpandString);
                    key.SetValue("AutoDetect", 1, RegistryValueKind.DWord);
                }
                var baseline = SystemProxyService.ReadCurrent(); string message;
                var prefs = AppSettings.Defaults(); prefs.HttpProxyPort = 31881;
                Check(SystemProxyService.Apply(prefs, out message), "Windows owned restore fixture applies route");
                Check(SystemProxyService.Apply(prefs, out message), "repeated Windows apply preserves the original baseline");
                var result = SystemProxyService.RestoreOwned();
                Check(result.Completed && result.Fields.Count == 5 && !File.Exists(SystemProxyService.BackupPath), "owned Windows restore settles all five fields and removes completed journal");
                foreach (var name in SystemProxyService.FieldNames)
                    Check(SystemProxyService.ReadCurrent().Values[name].Matches(baseline.Values[name]), "original registry value and kind restored: " + name + "; expected=" + new JavaScriptSerializer().Serialize(baseline.Values[name]) + "; actual=" + new JavaScriptSerializer().Serialize(SystemProxyService.ReadCurrent().Values[name]) + "; outcome=" + result.Fields.Single(f => f.Name == name).State);
                Check(SystemProxyService.RestoreOwned().Completed, "Windows off without ownership is idempotent");

                Check(SystemProxyService.Apply(prefs, out message), "external Windows route fixture applies ProGo first");
                using (var key = Registry.CurrentUser.OpenSubKey(ProxyRegistryPath, true)) {
                    key.SetValue("ProxyServer", "later.example.org:9090", RegistryValueKind.String);
                    key.SetValue("ProxyEnable", 1, RegistryValueKind.DWord);
                    key.SetValue("ProxyOverride", "later.example.org", RegistryValueKind.String);
                    key.SetValue("AutoConfigURL", "https://later.example.org/proxy.pac", RegistryValueKind.String);
                    key.SetValue("AutoDetect", 0, RegistryValueKind.DWord);
                }
                var external = SystemProxyService.ReadCurrent(); result = SystemProxyService.RestoreOwned();
                Check(result.Completed && result.PreservedExternal && result.Fields.Any(f => f.Name == "ProxyServer" && f.State == WindowsProxyRestoreState.PreservedExternal), "per-field result reports a later external Windows route");
                foreach (var name in SystemProxyService.FieldNames)
                    Check(SystemProxyService.ReadCurrent().Values[name].Matches(external.Values[name]), "later external route retains its flags/PAC/bypass: " + name);
                // Re-enabling explicitly must capture that external route as the new baseline.
                Check(SystemProxyService.Apply(prefs, out message) && SystemProxyService.RestoreOwned().Completed, "explicit re-enable restores the latest external baseline instead of a stale one");
                foreach (var name in SystemProxyService.FieldNames)
                    Check(SystemProxyService.ReadCurrent().Values[name].Matches(external.Values[name]), "re-enable baseline retained: " + name);

                SystemProxyService.RestoreSnapshot(baseline);
                Check(SystemProxyService.Apply(prefs, out message), "partial ownership fixture applies route");
                using (var key = Registry.CurrentUser.OpenSubKey(ProxyRegistryPath, true)) {
                    key.SetValue("ProxyOverride", "new-bypass.example.org", RegistryValueKind.String);
                    key.SetValue("AutoConfigURL", "https://new-pac.example.org/proxy.pac", RegistryValueKind.String);
                    key.SetValue("AutoDetect", 1, RegistryValueKind.DWord);
                }
                result = SystemProxyService.RestoreOwned();
                Check(result.Completed && result.Fields.Count(f => f.State == WindowsProxyRestoreState.PreservedExternal) == 2, "only changed bypass and PAC are marked external; an original-valued flag is left alone");
                using (var key = Registry.CurrentUser.OpenSubKey(ProxyRegistryPath)) {
                    Check(Convert.ToString(key.GetValue("ProxyOverride")) == "new-bypass.example.org" && Convert.ToString(key.GetValue("AutoConfigURL")) == "https://new-pac.example.org/proxy.pac", "changed auxiliary Windows values survive owned off");
                    Check(Convert.ToString(key.GetValue("ProxyServer")) == baseline.ProxyServer && Convert.ToInt32(key.GetValue("ProxyEnable")) == 0, "unchanged owned endpoint/enable restore independently");
                }

                SystemProxyService.RestoreSnapshot(baseline);
                Check(SystemProxyService.Apply(prefs, out message), "denied-field fixture applies route");
                using (var key = Registry.CurrentUser.OpenSubKey(ProxyRegistryPath, true)) key.SetValue("ProxyOverride", "external-bypass.example.org", RegistryValueKind.String);
                result = SystemProxyService.RestoreOwned(delegate(RegistryKey key, string name, WindowsProxyValue value) {
                    if (name == "ProxyServer") throw new UnauthorizedAccessException("fixture write denial");
                    SystemProxyService.WriteValue(key, name, value);
                });
                Check(!result.Completed && result.Fields.Single(f => f.Name == "ProxyServer").State == WindowsProxyRestoreState.Failed && result.Message.Contains("Windows — выключить") && File.Exists(SystemProxyService.BackupPath), "one denied registry write is reported and keeps a retryable journal");
                var saved = new JavaScriptSerializer().Deserialize<SystemProxyBackup>(File.ReadAllText(SystemProxyService.BackupPath));
                Check(saved.OwnedFields.Count(f => f.Pending) == 1 && saved.OwnedFields.Single(f => f.Pending).Name == "ProxyServer", "partial cleanup persists only the failed field as pending");
                // A settled external field can later look like ProGo again; a retry must not claim it.
                using (var key = Registry.CurrentUser.OpenSubKey(ProxyRegistryPath, true)) key.SetValue("ProxyOverride", "localhost;127.0.0.1;::1;<local>", RegistryValueKind.String);
                result = SystemProxyService.RestoreOwned();
                Check(result.Completed && result.Fields.Count == 1 && !File.Exists(SystemProxyService.BackupPath), "cleanup retry touches only its pending field");
                using (var key = Registry.CurrentUser.OpenSubKey(ProxyRegistryPath))
                    Check(Convert.ToString(key.GetValue("ProxyOverride")) == "localhost;127.0.0.1;::1;<local>", "retry never reclaims a settled external field merely because its value matches ProGo");

                SystemProxyService.RestoreSnapshot(baseline);
                Check(SystemProxyService.Apply(prefs, out message), "backup-write failure fixture applies route");
                using (var locked = File.Open(SystemProxyService.BackupPath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    result = SystemProxyService.RestoreOwned();
                    Check(!result.Completed && File.Exists(SystemProxyService.BackupPath) && result.Message.Contains("Копия сохранена"), "a locked cleanup journal is not silently discarded or treated as success");
                }
                Check(SystemProxyService.RestoreOwned().Completed && !File.Exists(SystemProxyService.BackupPath), "retry after releasing journal lock safely completes");

                SystemProxyService.RestoreSnapshot(baseline);
                Check(SystemProxyService.Apply(prefs, out message), "WinINet type-correction failure fixture applies route");
                int overrideWrites = 0;
                result = SystemProxyService.RestoreOwned(delegate(RegistryKey key, string name, WindowsProxyValue value) {
                    if (name == "ProxyOverride" && ++overrideWrites > 1) throw new UnauthorizedAccessException("fixture kind correction denied");
                    SystemProxyService.WriteValue(key, name, value);
                });
                Check(!result.Completed && result.Fields.Single(f => f.Name == "ProxyOverride").State == WindowsProxyRestoreState.Failed && File.Exists(SystemProxyService.BackupPath), "denied WinINet kind correction remains a reported pending field");
                saved = new JavaScriptSerializer().Deserialize<SystemProxyBackup>(File.ReadAllText(SystemProxyService.BackupPath));
                Check(saved.OwnedFields.Single(f => f.Pending).Name == "ProxyOverride", "kind retry records the actual normalization value without reviving settled fields");
                result = SystemProxyService.RestoreOwned();
                Check(result.Completed && SystemProxyService.ReadCurrent().Values["ProxyOverride"].Matches(baseline.Values["ProxyOverride"]) && SystemProxyService.ReadCurrent().Values["AutoConfigURL"].Matches(baseline.Values["AutoConfigURL"]), "kind retry preserves both its failed field and other live typed values across another refresh");

                SystemProxyService.RestoreSnapshot(baseline);
                Check(SystemProxyService.Apply(prefs, out message), "WinINet flag-correction failure fixture applies route");
                int autoDetectWrites = 0;
                result = SystemProxyService.RestoreOwned(delegate(RegistryKey key, string name, WindowsProxyValue value) {
                    if (name == "AutoDetect" && ++autoDetectWrites > 1) throw new UnauthorizedAccessException("fixture flag correction denied");
                    SystemProxyService.WriteValue(key, name, value);
                });
                Check(!result.Completed && result.Fields.Single(f => f.Name == "AutoDetect").State == WindowsProxyRestoreState.Failed, "denied AutoDetect refresh correction is reported instead of declaring cleanup complete");
                saved = new JavaScriptSerializer().Deserialize<SystemProxyBackup>(File.ReadAllText(SystemProxyService.BackupPath));
                Check(saved.OwnedFields.Single(f => f.Pending).Name == "AutoDetect", "removed AutoDetect stays the sole retryable field");
                result = SystemProxyService.RestoreOwned();
                Check(result.Completed && SystemProxyService.ReadCurrent().Values["AutoDetect"].Matches(baseline.Values["AutoDetect"]), "retry restores the original AutoDetect presence and value after notification");

                // Old snapshots have AppliedServer but no new ownership/type journal.
                SystemProxyService.RestoreSnapshot(baseline);
                Check(SystemProxyService.Apply(prefs, out message), "legacy backup migration fixture applies route");
                saved = new JavaScriptSerializer().Deserialize<SystemProxyBackup>(File.ReadAllText(SystemProxyService.BackupPath));
                saved.OwnedFields = null; saved.Values = null;
                File.WriteAllText(SystemProxyService.BackupPath, new JavaScriptSerializer().Serialize(saved));
                result = SystemProxyService.RestoreOwned();
                Check(result.Completed && result.Fields.Count == 5 && !File.Exists(SystemProxyService.BackupPath), "legacy AppliedServer snapshots migrate to owned per-field restore");
                SystemProxyService.RestoreSnapshot(baseline);
                WindowsCleanupFailureUi(settings);
            } finally {
                settings.Save(configuration);
                SystemProxyService.RestoreSnapshot(original);
                if (File.Exists(SystemProxyService.BackupPath)) File.Delete(SystemProxyService.BackupPath);
                foreach (var pair in environment) Environment.SetEnvironmentVariable(pair.Key, pair.Value, EnvironmentVariableTarget.User);
                if (File.Exists(CliProxyEnvironmentService.BackupPath)) File.Delete(CliProxyEnvironmentService.BackupPath);
            }
        }
        private static void WindowsCleanupFailureUi(SettingsService settings)
        {
            using (var proxy = new ProxyService(() => settings.Current, s => settings.Save(s), "unused-test-ssh", () => DateTime.UtcNow, false))
            using (var bridge = new CliProxyBridgeService(settings))
            using (var relay = new Ikev2RelayService())
            using (var home = new HomeVpnService(relay))
            using (var clipboard = new ClipboardService(settings)) {
                string message; bool deny = true;
                Check(bridge.Start(out message) && SystemProxyService.Apply(settings.Current, out message), "UI cleanup failure fixture starts an owned Windows route");
                using (var context = new UpdateAwareTrayApplicationContext(settings, proxy, bridge, home, clipboard, false, null,
                    () => SystemProxyService.RestoreOwned(delegate(RegistryKey key, string name, WindowsProxyValue value) {
                        if (deny && name == "ProxyServer") throw new UnauthorizedAccessException("fixture write denial");
                        SystemProxyService.WriteValue(key, name, value);
                    }))) {
                    context.RequestShowStatus(); Application.DoEvents();
                    var text = CleanupDialog(context, "Execute", "stop");
                    Check(text.Contains("Очистка прокси Windows не завершена") && text.Contains("продолжает работать") && text.Contains("Windows — выключить"), "actual stop dialog exposes incomplete cleanup and the concrete retry action");
                    Check(bridge.IsRunning && File.Exists(SystemProxyService.BackupPath), "desktop stop preserves its bridge when a registry field cannot be restored");
                    text = CleanupDialog(context, "ExitProGo");
                    Check(text.Contains("Очистка прокси Windows не завершена") && bridge.IsRunning && ((NotifyIcon)Field(context, "tray")).Visible,
                        "normal Quit refuses teardown and keeps the application usable after cleanup failure");
                    deny = false; Call(context, "Execute", "stop");
                    Check(!bridge.IsRunning && !File.Exists(SystemProxyService.BackupPath), "retrying actual desktop stop completes cleanup before stopping the service");
                }
            }
        }
        private static string CleanupDialog(object context, string method, params object[] args)
        {
            var contents = new StringBuilder(); bool seen = false;
            using (var timer = new System.Windows.Forms.Timer { Interval = 100 }) {
                timer.Tick += delegate {
                    var window = FindWindow("#32770", "ProGo"); if (window == IntPtr.Zero) return;
                    seen = true; timer.Stop();
                    EnumChildWindows(window, delegate(IntPtr child, IntPtr data) {
                        var text = new StringBuilder(4096); GetWindowText(child, text, text.Capacity); contents.AppendLine(text.ToString()); return true;
                    }, IntPtr.Zero);
                    PostMessage(window, 0x0010, IntPtr.Zero, IntPtr.Zero);
                };
                timer.Start(); Call(context, method, args);
            }
            Check(seen, "native cleanup warning is visible for " + method);
            return contents.ToString();
        }
    }
}

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;
using Microsoft.Win32;

namespace ProGo
{
    internal sealed class SystemProxyBackup
    {
        public bool HadProxyEnable { get; set; }
        public int ProxyEnable { get; set; }
        public bool HadProxyServer { get; set; }
        public string ProxyServer { get; set; }
        public bool HadProxyOverride { get; set; }
        public string ProxyOverride { get; set; }
        public bool HadAutoConfigUrl { get; set; }
        public string AutoConfigUrl { get; set; }
        public bool HadAutoDetect { get; set; }
        public int AutoDetect { get; set; }
        public string CreatedAtUtc { get; set; }
        public string AppliedServer { get; set; }
        public Dictionary<string, WindowsProxyValue> Values { get; set; }
        public List<WindowsProxyFieldBackup> OwnedFields { get; set; }
    }

    // Raw registry values preserve absence and REG_EXPAND_SZ without expanding paths.
    internal sealed class WindowsProxyValue
    {
        public bool Exists { get; set; }
        public RegistryValueKind Kind { get; set; }
        public string Data { get; set; }
        internal static WindowsProxyValue From(object value, RegistryValueKind kind)
        {
            return new WindowsProxyValue { Exists = true, Kind = kind, Data = new JavaScriptSerializer().Serialize(value) };
        }
        internal bool Matches(WindowsProxyValue other)
        {
            return other != null && Exists == other.Exists && (!Exists || (Kind == other.Kind && Data == other.Data));
        }
    }
    internal sealed class WindowsProxyFieldBackup
    {
        public string Name { get; set; }
        public WindowsProxyValue Original { get; set; }
        public WindowsProxyValue Applied { get; set; }
        public bool Pending { get; set; }
    }
    internal enum WindowsProxyRestoreState { Restored, AlreadyOriginal, PreservedExternal, Failed }
    internal sealed class WindowsProxyFieldResult
    {
        internal string Name;
        internal WindowsProxyRestoreState State;
    }
    internal sealed class WindowsProxyRestoreResult
    {
        internal readonly List<WindowsProxyFieldResult> Fields = new List<WindowsProxyFieldResult>();
        internal bool Completed = true;
        internal string Error;
        internal bool PreservedExternal { get { return Fields.Any(f => f.State == WindowsProxyRestoreState.PreservedExternal); } }
        internal string Message {
            get {
                if (Completed) return PreservedExternal ? "Позднейшие настройки Windows сохранены: они больше не принадлежат ProGo." : "Настройки Windows восстановлены.";
                var failed = Fields.Where(f => f.State == WindowsProxyRestoreState.Failed).Select(f => f.Name).ToArray();
                return "Очистка прокси Windows не завершена." + (failed.Length == 0 ? "" : " Не восстановлены: " + String.Join(", ", failed) + ".") +
                    " " + (Error ?? "Проверьте права записи в настройки текущего пользователя.") + " Копия сохранена; повторите «Windows — выключить» после устранения причины.";
            }
        }
    }

    internal static class SystemProxyService
    {
        private const string InternetSettingsKey = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings";
        private const int INTERNET_OPTION_REFRESH = 37;
        private const int INTERNET_OPTION_SETTINGS_CHANGED = 39;
        private const int HWND_BROADCAST = 0xffff;
        private const int WM_SETTINGCHANGE = 0x001A;
        private const int SMTO_ABORTIFHUNG = 0x0002;
        private const string BackupFileName = "system-proxy-backup.json";
        private const string DefaultProxyOverride = "localhost;127.0.0.1;::1;<local>";
        internal static readonly string[] FieldNames = { "ProxyEnable", "ProxyServer", "ProxyOverride", "AutoDetect", "AutoConfigURL" };

        [DllImport("wininet.dll", SetLastError = true)]
        private static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SendMessageTimeout(IntPtr hWnd, int Msg, IntPtr wParam, string lParam, int fuFlags, int uTimeout, out IntPtr lpdwResult);

        public static string BackupPath
        {
            get { return Path.Combine(AppPaths.Root, BackupFileName); }
        }

        public static bool Apply(AppSettings settings, out string message)
        {
            message = null;
            if (settings == null)
            {
                message = "Настройки ProGo недоступны.";
                return false;
            }

            if (settings.HttpProxyPort < 1 || settings.HttpProxyPort > 65535)
            {
                message = "Некорректный порт приложений.";
                return false;
            }

            try
            {
                AppPaths.EnsureDirectories();
                var current = ReadCurrent();
                var owned = File.Exists(BackupPath) ? LoadBackup() : current;
                if (owned == null) throw new IOException("Windows proxy backup is unreadable.");
                var oldFields = GetOwnedFields(owned);
                var priorServer = oldFields == null ? null : oldFields.First(f => f.Name == "ProxyServer");
                var serverOwned = priorServer != null && priorServer.Pending && current.Values["ProxyServer"].Matches(priorServer.Applied);
                var applied = AppliedValues(BuildProxyServer(settings));
                owned.OwnedFields = FieldNames.Select(name => {
                    var old = oldFields == null ? null : oldFields.First(f => f.Name == name);
                    var original = serverOwned && old != null && old.Pending && current.Values[name].Matches(old.Applied) ? old.Original : current.Values[name];
                    return new WindowsProxyFieldBackup { Name = name, Original = original, Applied = applied[name], Pending = true };
                }).ToList();
                owned.AppliedServer = BuildProxyServer(settings);
                if (String.IsNullOrEmpty(owned.CreatedAtUtc)) owned.CreatedAtUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                SaveBackup(owned); // Persist intended ownership before the first registry mutation.

                using (var key = Registry.CurrentUser.CreateSubKey(InternetSettingsKey))
                {
                    if (key == null)
                    {
                        message = "Не удалось открыть настройки Internet Settings текущего пользователя.";
                        return false;
                    }

                    key.SetValue("ProxyEnable", 1, RegistryValueKind.DWord);
                    key.SetValue("ProxyServer", BuildProxyServer(settings), RegistryValueKind.String);
                    key.SetValue("ProxyOverride", DefaultProxyOverride, RegistryValueKind.String);
                    key.SetValue("AutoDetect", 0, RegistryValueKind.DWord);
                    key.DeleteValue("AutoConfigURL", false);
                }

                RefreshPreservingValues(null, null, WriteValue);
                SafeLog.Info("Current-user Windows proxy enabled. port=" + settings.HttpProxyPort + ".");
                return true;
            }
            catch (Exception ex)
            {
                SafeLog.Error("Current-user Windows proxy enable failed.", ex);
                message = "Не удалось включить системный прокси Windows. Подробности записаны в журнал.";
                return false;
            }
        }

        // Compatibility entry point; every production off path is ownership-aware.
        public static bool Restore(out string message)
        {
            var result = RestoreOwned(); message = result.Message; return result.Completed;
        }
        internal static WindowsProxyRestoreResult RestoreOwned()
        {
            return RestoreOwned(WriteValue);
        }
        // Writer seam is used by isolated native CI to simulate a single denied field.
        internal static WindowsProxyRestoreResult RestoreOwned(Action<RegistryKey, string, WindowsProxyValue> writer)
        {
            var result = new WindowsProxyRestoreResult();
            if (!File.Exists(BackupPath)) return result; // An idempotent off must not modify unrelated settings.
            try {
                var backup = LoadBackup(); var fields = GetOwnedFields(backup);
                if (fields == null) throw new IOException("В копии нет подтверждённых значений ProGo. Чужие настройки не изменены.");
                backup.OwnedFields = fields;
                using (var key = Registry.CurrentUser.CreateSubKey(InternetSettingsKey)) {
                    if (key == null) throw new IOException("Не удалось открыть настройки текущего пользователя.");
                    // Refuse the entire old route when a later application replaced its endpoint.
                    // Original values can also be present after a partly completed restore/apply.
                    var server = fields.First(f => f.Name == "ProxyServer");
                    foreach (var field in fields.Where(f => f.Pending)) {
                        var current = ReadValue(key, field.Name);
                        var endpoint = ReadValue(key, "ProxyServer");
                        bool externalRoute = !endpoint.Matches(server.Applied) && !endpoint.Matches(server.Original);
                        var state = WindowsProxyRestoreState.AlreadyOriginal;
                        if (!current.Matches(field.Original)) {
                            if (externalRoute || !current.Matches(field.Applied)) state = WindowsProxyRestoreState.PreservedExternal;
                            else try {
                                // Recheck each field immediately before writing; there is no registry-wide compare-and-swap.
                                if (!ReadValue(key, field.Name).Matches(field.Applied)) state = WindowsProxyRestoreState.PreservedExternal;
                                else { writer(key, field.Name, field.Original); state = WindowsProxyRestoreState.Restored; }
                            } catch (Exception ex) {
                                state = WindowsProxyRestoreState.Failed; result.Completed = false;
                                SafeLog.Error("Windows proxy field restore failed: " + field.Name, ex);
                            }
                        }
                        field.Pending = state == WindowsProxyRestoreState.Failed;
                        result.Fields.Add(new WindowsProxyFieldResult { Name = field.Name, State = state });
                    }
                }
                if (result.Fields.Any(f => f.State == WindowsProxyRestoreState.Restored))
                    RefreshPreservingValues(backup, result, writer);
                // Persist settled fields even after partial failure; retries must not revisit external values.
                SaveBackup(backup);
                if (result.Completed) File.Delete(BackupPath);
                SafeLog.Info(result.Completed ? "Owned Windows proxy settings settled." : "Windows proxy cleanup incomplete; backup retained.");
            } catch (Exception ex) {
                result.Completed = false; result.Error = "Не удалось прочитать, сохранить или удалить копию восстановления. Подробности в журнале.";
                SafeLog.Error("Owned Windows proxy cleanup failed.", ex);
            }
            return result;
        }

        public static bool IsApplied(AppSettings settings)
        {
            if (settings == null) return false;

            try
            {
                var snapshot = ReadCurrent();
                if (!snapshot.HadProxyEnable || snapshot.ProxyEnable == 0) return false;
                if (!snapshot.HadProxyServer || String.IsNullOrWhiteSpace(snapshot.ProxyServer)) return false;
                return String.Equals(snapshot.ProxyServer, BuildProxyServer(settings), StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        public static bool IsOwned
        {
            get
            {
                try {
                    var fields = GetOwnedFields(LoadBackup()); var now = ReadCurrent();
                    var server = fields == null ? null : fields.First(f => f.Name == "ProxyServer");
                    return server != null && server.Pending && now.Values["ProxyServer"].Matches(server.Applied);
                }
                catch { return false; }
            }
        }

        public static string StatusText(AppSettings settings)
        {
            try
            {
                var snapshot = ReadCurrent();
                if (!snapshot.HadProxyEnable || snapshot.ProxyEnable == 0) return "выключен";
                if (settings != null && IsApplied(settings)) return "включён для " + CliProxyBridgeService.Host + ":" + settings.HttpProxyPort;
                return String.IsNullOrWhiteSpace(snapshot.ProxyServer) ? "включён" : "включён: " + snapshot.ProxyServer;
            }
            catch
            {
                return "неизвестно";
            }
        }

        private static void SaveBackup(SystemProxyBackup backup)
        {
            AppPaths.EnsureDirectories();
            var pending = BackupPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try {
                File.WriteAllText(pending, new JavaScriptSerializer().Serialize(backup));
                if (File.Exists(BackupPath)) File.Replace(pending, BackupPath, null); else File.Move(pending, BackupPath);
            } finally { if (File.Exists(pending)) File.Delete(pending); }
        }
        private static Dictionary<string, WindowsProxyValue> AppliedValues(string server)
        {
            return new Dictionary<string, WindowsProxyValue> {
                { "ProxyEnable", WindowsProxyValue.From(1, RegistryValueKind.DWord) },
                { "ProxyServer", WindowsProxyValue.From(server, RegistryValueKind.String) },
                { "ProxyOverride", WindowsProxyValue.From(DefaultProxyOverride, RegistryValueKind.String) },
                { "AutoDetect", WindowsProxyValue.From(0, RegistryValueKind.DWord) },
                { "AutoConfigURL", new WindowsProxyValue() }
            };
        }
        private static List<WindowsProxyFieldBackup> GetOwnedFields(SystemProxyBackup backup)
        {
            if (backup == null) return null;
            if (backup.OwnedFields != null) {
                if (backup.OwnedFields.Count != FieldNames.Length || FieldNames.Any(n => backup.OwnedFields.Count(f => f != null && f.Name == n && f.Original != null && f.Applied != null) != 1))
                    throw new IOException("Некорректный список полей восстановления.");
                return backup.OwnedFields;
            }
            if (String.IsNullOrWhiteSpace(backup.AppliedServer)) return null;
            var originals = OriginalValues(backup); var applied = AppliedValues(backup.AppliedServer);
            return FieldNames.Select(n => new WindowsProxyFieldBackup { Name = n, Original = originals[n], Applied = applied[n], Pending = true }).ToList();
        }
        private static Dictionary<string, WindowsProxyValue> OriginalValues(SystemProxyBackup backup)
        {
            if (backup.Values != null) return backup.Values;
            // Older backups did not record kinds; retain the original documented DWord/String contract.
            return new Dictionary<string, WindowsProxyValue> {
                { "ProxyEnable", backup.HadProxyEnable ? WindowsProxyValue.From(backup.ProxyEnable, RegistryValueKind.DWord) : new WindowsProxyValue() },
                { "ProxyServer", backup.HadProxyServer ? WindowsProxyValue.From(backup.ProxyServer ?? "", RegistryValueKind.String) : new WindowsProxyValue() },
                { "ProxyOverride", backup.HadProxyOverride ? WindowsProxyValue.From(backup.ProxyOverride ?? "", RegistryValueKind.String) : new WindowsProxyValue() },
                { "AutoDetect", backup.HadAutoDetect ? WindowsProxyValue.From(backup.AutoDetect, RegistryValueKind.DWord) : new WindowsProxyValue() },
                { "AutoConfigURL", backup.HadAutoConfigUrl ? WindowsProxyValue.From(backup.AutoConfigUrl ?? "", RegistryValueKind.String) : new WindowsProxyValue() }
            };
        }
        internal static WindowsProxyValue ReadValue(RegistryKey key, string name)
        {
            var value = key == null ? null : key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
            return value == null ? new WindowsProxyValue() : WindowsProxyValue.From(value, key.GetValueKind(name));
        }
        internal static void WriteValue(RegistryKey key, string name, WindowsProxyValue value)
        {
            if (!value.Exists) { key.DeleteValue(name, false); return; }
            var json = new JavaScriptSerializer(); object raw;
            switch (value.Kind) {
                case RegistryValueKind.DWord: raw = json.Deserialize<int>(value.Data); break;
                case RegistryValueKind.QWord: raw = json.Deserialize<long>(value.Data); break;
                case RegistryValueKind.Binary:
                case RegistryValueKind.None: raw = json.Deserialize<byte[]>(value.Data); break;
                case RegistryValueKind.MultiString: raw = json.Deserialize<string[]>(value.Data); break;
                case RegistryValueKind.String:
                case RegistryValueKind.ExpandString: raw = json.Deserialize<string>(value.Data); break;
                default: throw new IOException("Неподдерживаемый тип поля " + name + ".");
            }
            key.SetValue(name, raw, value.Kind);
        }
        internal static void UpdateOwnedServer(SystemProxyBackup backup, string server)
        {
            var fields = GetOwnedFields(backup);
            if (fields != null) {
                var field = fields.First(f => f.Name == "ProxyServer");
                field.Applied = WindowsProxyValue.From(server, RegistryValueKind.String); field.Pending = true;
                backup.OwnedFields = fields;
            }
            backup.AppliedServer = server;
        }

        private static SystemProxyBackup LoadBackup()
        {
            var serializer = new JavaScriptSerializer();
            return serializer.Deserialize<SystemProxyBackup>(File.ReadAllText(BackupPath));
        }

        internal static SystemProxyBackup ReadCurrent()
        {
            var backup = new SystemProxyBackup { Values = new Dictionary<string, WindowsProxyValue>() };
            using (var key = Registry.CurrentUser.OpenSubKey(InternetSettingsKey, false))
            {
                foreach (var name in FieldNames) backup.Values[name] = ReadValue(key, name);
                if (key == null) return backup;

                object value;

                value = key.GetValue("ProxyEnable", null);
                backup.HadProxyEnable = value != null;
                backup.ProxyEnable = ReadDword(value, 0);

                value = key.GetValue("ProxyServer", null);
                backup.HadProxyServer = value != null;
                backup.ProxyServer = value == null ? null : Convert.ToString(value);

                value = key.GetValue("ProxyOverride", null);
                backup.HadProxyOverride = value != null;
                backup.ProxyOverride = value == null ? null : Convert.ToString(value);

                value = key.GetValue("AutoConfigURL", null);
                backup.HadAutoConfigUrl = value != null;
                backup.AutoConfigUrl = value == null ? null : Convert.ToString(value);

                value = key.GetValue("AutoDetect", null);
                backup.HadAutoDetect = value != null;
                backup.AutoDetect = ReadDword(value, 0);
            }

            return backup;
        }

        // Full snapshots are restricted to local transaction rollback and isolated fixture cleanup.
        internal static void RestoreSnapshot(SystemProxyBackup backup)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(InternetSettingsKey)) {
                if (key == null) throw new IOException("Не удалось открыть настройки прокси Windows.");
                var originals = OriginalValues(backup);
                foreach (var name in FieldNames) WriteValue(key, name, originals[name]);
            }
            RefreshPreservingValues(null, null, WriteValue);
        }

        private static int ReadDword(object value, int fallback)
        {
            if (value == null) return fallback;
            try { return Convert.ToInt32(value); }
            catch { return fallback; }
        }

        private static string BuildProxyServer(AppSettings settings)
        {
            return "http=" + CliProxyBridgeService.Host + ":" + settings.HttpProxyPort + ";https=" + CliProxyBridgeService.Host + ":" + settings.HttpProxyPort;
        }

        private static void RefreshPreservingValues(SystemProxyBackup backup, WindowsProxyRestoreResult result,
            Action<RegistryKey, string, WindowsProxyValue> writer)
        {
            if (backup == null || result == null) {
                PreserveTypedValues(RefreshSystemProxy, writer, null); return;
            }
            PreserveTypedValues(RefreshSystemProxy, writer, delegate(WindowsProxyFieldBackup correction, Exception ex) {
                var field = backup.OwnedFields.First(f => f.Name == correction.Name);
                field.Original = correction.Original; field.Applied = correction.Applied; field.Pending = true;
                var outcome = result.Fields.FirstOrDefault(f => f.Name == correction.Name);
                if (outcome == null) { outcome = new WindowsProxyFieldResult { Name = correction.Name }; result.Fields.Add(outcome); }
                outcome.State = WindowsProxyRestoreState.Failed; result.Completed = false;
                SafeLog.Error("Windows proxy refresh correction failed: " + correction.Name, ex);
            });
        }

        internal static void PreserveTypedValues(Action notification)
        {
            PreserveTypedValues(notification, WriteValue, null);
        }
        // Protect live typed values around our own notifications, never an old route snapshot.
        // Environment.SetEnvironmentVariable itself broadcasts before the final CLI notification.
        internal static void PreserveTypedValues(Action notification,
            Action<RegistryKey, string, WindowsProxyValue> writer,
            Action<WindowsProxyFieldBackup, Exception> failed)
        {
            PreserveTypedValues(notification, writer, failed, null);
        }
        internal static void PreserveTypedValues(Action notification,
            Action<RegistryKey, string, WindowsProxyValue> writer,
            Action<WindowsProxyFieldBackup, Exception> failed,
            Action<Dictionary<string, WindowsProxyValue>> prepare)
        {
            var before = ReadCurrent().Values;
            // A failed guard write must abort before any setter/notification.
            // Use the exact same live snapshot for preparation and correction.
            if (prepare != null) prepare(before);
            try { notification(); }
            finally { CorrectTypedValues(before, writer, failed); }
        }
        internal static bool IsTypedNormalization(string name, WindowsProxyValue expected, WindowsProxyValue observed)
        {
            if (expected == null || observed == null) return false;
            bool changedKind = expected.Exists && expected.Kind == RegistryValueKind.ExpandString &&
                observed.Exists && observed.Kind == RegistryValueKind.String && observed.Data == expected.Data;
            bool removedAutoDetect = name == "AutoDetect" && expected.Exists &&
                expected.Kind == RegistryValueKind.DWord && (expected.Data == "0" || expected.Data == "1") && !observed.Exists;
            return changedKind || removedAutoDetect;
        }
        private static void CorrectTypedValues(Dictionary<string, WindowsProxyValue> before,
            Action<RegistryKey, string, WindowsProxyValue> writer,
            Action<WindowsProxyFieldBackup, Exception> failed)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(InternetSettingsKey)) {
                foreach (var name in FieldNames) {
                    var expected = before[name]; var current = ReadValue(key, name);
                    // WinINet can normalize string kinds or remove AutoDetect during notification.
                    // Correct only these observed effects from live pre-notification values.
                    if (!IsTypedNormalization(name, expected, current)) continue;
                    if (!ReadValue(key, name).Matches(current)) continue;
                    try { writer(key, name, expected); }
                    catch (Exception ex) {
                        if (failed == null) throw;
                        failed(new WindowsProxyFieldBackup { Name = name, Original = expected, Applied = current, Pending = true }, ex);
                    }
                }
            }
        }

        internal static void RetryTypedValues(List<WindowsProxyFieldBackup> corrections,
            Action<RegistryKey, string, WindowsProxyValue> writer)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(InternetSettingsKey)) {
                foreach (var correction in corrections.Where(f => f.Pending)) {
                    var current = ReadValue(key, correction.Name);
                    if (current.Matches(correction.Applied) && !current.Matches(correction.Original) &&
                        ReadValue(key, correction.Name).Matches(current)) writer(key, correction.Name, correction.Original);
                    // A later different value is external; a retry must leave it alone.
                    correction.Pending = false;
                }
            }
        }

        internal static void RefreshSystemProxy()
        {
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);

            IntPtr result;
            SendMessageTimeout((IntPtr)HWND_BROADCAST, WM_SETTINGCHANGE, IntPtr.Zero, InternetSettingsKey, SMTO_ABORTIFHUNG, 5000, out result);
        }
    }
}

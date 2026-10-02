using System;
using System.IO;
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
                SaveBackupIfNeeded();
                var owned = LoadBackup();
                owned.AppliedServer = BuildProxyServer(settings);
                File.WriteAllText(BackupPath, new JavaScriptSerializer().Serialize(owned));

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
                    DeleteValueSafe(key, "AutoConfigURL");
                }

                RefreshSystemProxy();
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

        public static bool Restore(out string message)
        {
            message = null;
            try
            {
                AppPaths.EnsureDirectories();
                if (!File.Exists(BackupPath))
                {
                    message = "Резервная копия предыдущих Windows proxy-настроек не найдена. Нечего восстанавливать.";
                    return false;
                }

                var backup = LoadBackup();
                if (backup == null)
                {
                    message = "Не удалось прочитать резервную копию Windows proxy-настроек.";
                    return false;
                }

                RestoreSnapshot(backup);
                try { File.Delete(BackupPath); } catch { }
                SafeLog.Info("Current-user Windows proxy restored.");
                return true;
            }
            catch (Exception ex)
            {
                SafeLog.Error("Current-user Windows proxy restore failed.", ex);
                message = "Не удалось восстановить системный прокси Windows. Подробности записаны в журнал.";
                return false;
            }
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
                try { var saved = LoadBackup(); var now = ReadCurrent(); return saved != null && !String.IsNullOrWhiteSpace(saved.AppliedServer) && now.ProxyServer == saved.AppliedServer; }
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

        private static void SaveBackupIfNeeded()
        {
            if (File.Exists(BackupPath)) return;

            var backup = ReadCurrent();
            backup.CreatedAtUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var serializer = new JavaScriptSerializer();
            File.WriteAllText(BackupPath, serializer.Serialize(backup));
        }

        private static SystemProxyBackup LoadBackup()
        {
            var serializer = new JavaScriptSerializer();
            return serializer.Deserialize<SystemProxyBackup>(File.ReadAllText(BackupPath));
        }

        internal static SystemProxyBackup ReadCurrent()
        {
            var backup = new SystemProxyBackup();
            using (var key = Registry.CurrentUser.OpenSubKey(InternetSettingsKey, false))
            {
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

        internal static void RestoreSnapshot(SystemProxyBackup backup)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(InternetSettingsKey))
            {
                if (key == null) throw new IOException("Не удалось открыть настройки прокси Windows.");
                RestoreDword(key, "ProxyEnable", backup.HadProxyEnable, backup.ProxyEnable);
                RestoreString(key, "ProxyServer", backup.HadProxyServer, backup.ProxyServer);
                RestoreString(key, "ProxyOverride", backup.HadProxyOverride, backup.ProxyOverride);
                RestoreString(key, "AutoConfigURL", backup.HadAutoConfigUrl, backup.AutoConfigUrl);
                RestoreDword(key, "AutoDetect", backup.HadAutoDetect, backup.AutoDetect);
            }
            RefreshSystemProxy();
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

        private static void RestoreDword(RegistryKey key, string name, bool hadValue, int value)
        {
            if (hadValue)
            {
                key.SetValue(name, value, RegistryValueKind.DWord);
            }
            else
            {
                DeleteValueSafe(key, name);
            }
        }

        private static void RestoreString(RegistryKey key, string name, bool hadValue, string value)
        {
            if (hadValue)
            {
                key.SetValue(name, value ?? String.Empty, RegistryValueKind.String);
            }
            else
            {
                DeleteValueSafe(key, name);
            }
        }

        private static void DeleteValueSafe(RegistryKey key, string name)
        {
            try { key.DeleteValue(name, false); }
            catch { }
        }

        private static void RefreshSystemProxy()
        {
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);

            IntPtr result;
            SendMessageTimeout((IntPtr)HWND_BROADCAST, WM_SETTINGCHANGE, IntPtr.Zero, InternetSettingsKey, SMTO_ABORTIFHUNG, 5000, out result);
        }
    }
}

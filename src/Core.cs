using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace ProGo
{
    internal static class AppConstants
    {
        public const string ProductName = "ProGo";
        public const string ExeName = "ProGo.exe";
        public const string VaultFileName = "vault.enc.json";
        public const string SettingsFileName = "settings.json";
        public const string LogFileName = "progo.log";
    }

    internal static class AppPaths
    {
        public static readonly string Root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppConstants.ProductName);
        public static readonly string SettingsPath = Path.Combine(Root, AppConstants.SettingsFileName);
        public static readonly string VaultPath = Path.Combine(Root, AppConstants.VaultFileName);
        public static readonly string LogPath = Path.Combine(Root, AppConstants.LogFileName);

        public static void EnsureDirectories()
        {
            Directory.CreateDirectory(Root);
        }
    }

    internal sealed class AppSettings
    {
        public string SocksHost { get; set; }
        public int SocksPort { get; set; }
        public string SshProfile { get; set; }
        public bool AutoStartSocks { get; set; }
        public bool AutoApplyProxy { get; set; }
        public int ClipboardClearSeconds { get; set; }
        public string TestEndpoint { get; set; }

        public static AppSettings Defaults()
        {
            return new AppSettings
            {
                SocksHost = "127.0.0.1",
                SocksPort = 1080,
                SshProfile = "",
                AutoStartSocks = false,
                AutoApplyProxy = false,
                ClipboardClearSeconds = 30,
                TestEndpoint = "https://api.openai.com/v1/models"
            };
        }
    }

    internal sealed class SettingsService : IDisposable
    {
        private readonly JavaScriptSerializer serializer = new JavaScriptSerializer();
        public AppSettings Current { get; private set; }

        public SettingsService()
        {
            Current = Load();
        }

        public AppSettings Load()
        {
            AppPaths.EnsureDirectories();
            if (!File.Exists(AppPaths.SettingsPath))
            {
                var defaults = AppSettings.Defaults();
                Save(defaults);
                return defaults;
            }

            try
            {
                var text = File.ReadAllText(AppPaths.SettingsPath);
                var settings = serializer.Deserialize<AppSettings>(text) ?? AppSettings.Defaults();
                Normalize(settings);
                return settings;
            }
            catch (Exception ex)
            {
                SafeLog.Error("Settings load failed; using defaults.", ex);
                return AppSettings.Defaults();
            }
        }

        public void Save(AppSettings settings)
        {
            Normalize(settings);
            AppPaths.EnsureDirectories();
            File.WriteAllText(AppPaths.SettingsPath, serializer.Serialize(settings));
            Current = settings;
            SafeLog.Info("Settings saved.");
        }

        private static void Normalize(AppSettings settings)
        {
            if (String.IsNullOrWhiteSpace(settings.SocksHost)) settings.SocksHost = "127.0.0.1";
            if (settings.SocksPort < 1 || settings.SocksPort > 65535) settings.SocksPort = 1080;
            if (settings.ClipboardClearSeconds < 5 || settings.ClipboardClearSeconds > 3600) settings.ClipboardClearSeconds = 30;
            if (String.IsNullOrWhiteSpace(settings.TestEndpoint)) settings.TestEndpoint = "https://api.openai.com/v1/models";
        }

        public void Dispose()
        {
        }
    }

    internal static class SafeLog
    {
        private static readonly object Gate = new object();
        private static readonly Regex[] Redactions = new[]
        {
            new Regex("(?i)(pin|secret|token|password|authorization|api[_ -]?key)\\s*[:=]\\s*[^\\s,;]+", RegexOptions.Compiled),
            new Regex("(?i)bearer\\s+[a-z0-9._~+/=-]+", RegexOptions.Compiled)
        };

        public static void Info(string message)
        {
            Write("INFO", message, null);
        }

        public static void Error(string message, Exception ex)
        {
            Write("ERROR", message, ex);
        }

        private static void Write(string level, string message, Exception ex)
        {
            try
            {
                AppPaths.EnsureDirectories();
                var line = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz") + " [" + level + "] " + Redact(message);
                if (ex != null)
                {
                    line += " | " + Redact(ex.GetType().Name + ": " + ex.Message);
                }

                lock (Gate)
                {
                    File.AppendAllText(AppPaths.LogPath, line + Environment.NewLine);
                }
            }
            catch
            {
                // Logging must never crash the tray application.
            }
        }

        public static string Redact(string value)
        {
            if (String.IsNullOrEmpty(value)) return String.Empty;
            var result = value;
            foreach (var regex in Redactions)
            {
                result = regex.Replace(result, "$1=<redacted>");
            }
            return result;
        }
    }

    internal sealed class ProxyService : IDisposable
    {
        private readonly SettingsService settings;
        private Process sshProcess;

        public ProxyService(SettingsService settingsService)
        {
            settings = settingsService;
        }

        public bool IsListening()
        {
            return IsTcpOpen(settings.Current.SocksHost, settings.Current.SocksPort, 700);
        }

        public int? CurrentPid
        {
            get
            {
                try
                {
                    return sshProcess != null && !sshProcess.HasExited ? (int?)sshProcess.Id : null;
                }
                catch
                {
                    return null;
                }
            }
        }

        public void StartTunnel(bool showErrors)
        {
            var current = settings.Current;
            if (String.IsNullOrWhiteSpace(current.SshProfile))
            {
                if (showErrors) System.Windows.Forms.MessageBox.Show("Укажите SSH-профиль в настройках.", AppConstants.ProductName);
                return;
            }

            if (IsListening())
            {
                SafeLog.Info("SOCKS already listens on port " + current.SocksPort + ".");
                return;
            }

            try
            {
                var args = String.Format("-N -D {0}:{1} -o ExitOnForwardFailure=yes -o ServerAliveInterval=30 -o ServerAliveCountMax=3 {2}", current.SocksHost, current.SocksPort, current.SshProfile);
                var psi = new ProcessStartInfo("ssh.exe", args)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                sshProcess = Process.Start(psi);
                SafeLog.Info("SOCKS start requested. PID=" + (sshProcess == null ? "unknown" : sshProcess.Id.ToString()) + "; port=" + current.SocksPort + ".");
            }
            catch (Exception ex)
            {
                SafeLog.Error("Failed to start SSH tunnel.", ex);
                if (showErrors) System.Windows.Forms.MessageBox.Show("Не удалось запустить SSH. Проверьте SSH-профиль и соединение.", AppConstants.ProductName);
            }
        }

        public void StopTunnel()
        {
            try
            {
                if (sshProcess != null && !sshProcess.HasExited)
                {
                    sshProcess.Kill();
                    sshProcess.WaitForExit(3000);
                    SafeLog.Info("SOCKS process stopped. PID=" + sshProcess.Id + ".");
                }
            }
            catch (Exception ex)
            {
                SafeLog.Error("Failed to stop SSH tunnel.", ex);
            }
        }

        public void RestartTunnel()
        {
            StopTunnel();
            StartTunnel(true);
        }

        private static bool IsTcpOpen(string host, int port, int timeoutMs)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    var ar = client.BeginConnect(host, port, null, null);
                    if (!ar.AsyncWaitHandle.WaitOne(timeoutMs)) return false;
                    client.EndConnect(ar);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            StopTunnel();
        }
    }

    internal static class EnvironmentProxyService
    {
        private const int HWND_BROADCAST = 0xffff;
        private const int WM_SETTINGCHANGE = 0x001A;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SendMessageTimeout(IntPtr hWnd, int Msg, IntPtr wParam, string lParam, int fuFlags, int uTimeout, out IntPtr lpdwResult);

        public static void Apply(AppSettings settings)
        {
            var value = "socks5h://" + settings.SocksHost + ":" + settings.SocksPort;
            SetUser("ALL_PROXY", value);
            SetUser("HTTPS_PROXY", value);
            SetUser("HTTP_PROXY", value);
            SetUser("all_proxy", value);
            SetUser("https_proxy", value);
            SetUser("http_proxy", value);
            SetUser("NO_PROXY", "localhost,127.0.0.1,::1");
            BroadcastEnvironmentChange();
            SafeLog.Info("Proxy environment applied. port=" + settings.SocksPort + ".");
        }

        private static void SetUser(string name, string value)
        {
            Environment.SetEnvironmentVariable(name, value, EnvironmentVariableTarget.User);
        }

        private static void BroadcastEnvironmentChange()
        {
            IntPtr result;
            SendMessageTimeout((IntPtr)HWND_BROADCAST, WM_SETTINGCHANGE, IntPtr.Zero, "Environment", 0, 5000, out result);
        }
    }

    internal static class RouteTester
    {
        public static string Test(AppSettings settings, ProxyService proxy)
        {
            if (!proxy.IsListening())
            {
                return "SOCKS-порт не слушает. Сначала запустите SOCKS-туннель.";
            }

            try
            {
                var psi = new ProcessStartInfo("curl.exe", "--socks5-hostname " + settings.SocksHost + ":" + settings.SocksPort + " -I -sS -m 15 " + settings.TestEndpoint)
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                using (var process = Process.Start(psi))
                {
                    var output = process.StandardOutput.ReadToEnd();
                    var error = process.StandardError.ReadToEnd();
                    process.WaitForExit(20000);
                    var text = output + Environment.NewLine + error;
                    if (text.IndexOf(" 401", StringComparison.OrdinalIgnoreCase) >= 0) return "SOCKS-маршрут работает. Сервер ответил HTTP 401 без авторизации — это ожидаемый результат.";
                    if (text.IndexOf(" 200", StringComparison.OrdinalIgnoreCase) >= 0) return "Соединение работает. HTTP 200.";
                    if (text.IndexOf("timed out", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0) return "Тайм-аут соединения.";
                    return "Маршрут проверен. Технический ответ: " + SafeLog.Redact(text.Trim());
                }
            }
            catch (Exception ex)
            {
                SafeLog.Error("Route test failed.", ex);
                return "Не удалось проверить маршрут через SOCKS.";
            }
        }
    }
}

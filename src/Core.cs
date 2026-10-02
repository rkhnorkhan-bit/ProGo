using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Globalization;
using System.Net.Sockets;
using System.Text;
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

    internal sealed class SshProfileSetting
    {
        public string Name { get; set; }
        public string Target { get; set; }

        public SshProfileSetting Clone()
        {
            return new SshProfileSetting
            {
                Name = Name,
                Target = Target
            };
        }

        public override string ToString()
        {
            if (String.IsNullOrWhiteSpace(Name)) return Target ?? String.Empty;
            if (String.Equals(Name, Target, StringComparison.OrdinalIgnoreCase)) return Name;
            return Name + " — " + Target;
        }
    }

    internal sealed class AppSettings
    {
        public string SocksHost { get; set; }
        public int SocksPort { get; set; }
        public string SshProfile { get; set; }
        public List<SshProfileSetting> SshProfiles { get; set; }
        public bool AutoSwitchSshProfile { get; set; }
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
                SshProfiles = new List<SshProfileSetting>(),
                AutoSwitchSshProfile = false,
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
            if (settings.SshProfile == null) settings.SshProfile = String.Empty;
            NormalizeProfiles(settings);
        }

        private static void NormalizeProfiles(AppSettings settings)
        {
            var normalized = new List<SshProfileSetting>();
            if (settings.SshProfiles != null)
            {
                foreach (var profile in settings.SshProfiles)
                {
                    if (profile == null) continue;
                    var target = (profile.Target ?? String.Empty).Trim();
                    if (String.IsNullOrWhiteSpace(target)) continue;
                    var name = (profile.Name ?? String.Empty).Trim();
                    if (String.IsNullOrWhiteSpace(name)) name = target;
                    if (ContainsTarget(normalized, target)) continue;
                    normalized.Add(new SshProfileSetting { Name = name, Target = target });
                }
            }

            var selected = (settings.SshProfile ?? String.Empty).Trim();
            if (!String.IsNullOrWhiteSpace(selected) && !ContainsTarget(normalized, selected))
            {
                normalized.Insert(0, new SshProfileSetting { Name = selected, Target = selected });
            }

            settings.SshProfiles = normalized;
            if (String.IsNullOrWhiteSpace(settings.SshProfile) && normalized.Count > 0)
            {
                settings.SshProfile = normalized[0].Target;
            }
        }

        private static bool ContainsTarget(List<SshProfileSetting> profiles, string target)
        {
            foreach (var profile in profiles)
            {
                if (String.Equals(profile.Target, target, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
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
            var targets = BuildProfileTargets(current);
            if (targets.Count == 0)
            {
                if (showErrors)
                {
                    System.Windows.Forms.MessageBox.Show(
                        "SSH-профиль не выбран.\n\nSSH-профиль — это короткое имя подключения из файла ~/.ssh/config, например my-vps, или прямой SSH-target вида user@vpn.example.org. ProGo использует его для создания локального SOCKS-туннеля.",
                        AppConstants.ProductName);
                }
                return;
            }

            if (IsListening())
            {
                SafeLog.Info("SOCKS already listens on port " + current.SocksPort + ".");
                return;
            }

            if (!current.AutoSwitchSshProfile)
            {
                StartSingleTunnel(targets[0], showErrors, false);
                return;
            }

            foreach (var target in targets)
            {
                if (StartSingleTunnel(target, false, true))
                {
                    if (!String.Equals(current.SshProfile, target, StringComparison.OrdinalIgnoreCase))
                    {
                        current.SshProfile = target;
                        settings.Save(current);
                        SafeLog.Info("SSH profile auto-switched to " + target + ".");
                    }
                    return;
                }
            }

            if (showErrors)
            {
                System.Windows.Forms.MessageBox.Show("Не удалось запустить SOCKS ни через один SSH-профиль. Проверьте список профилей и доступность серверов.", AppConstants.ProductName);
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

        private bool StartSingleTunnel(string sshTarget, bool showErrors, bool requireListening)
        {
            try
            {
                var current = settings.Current;
                var endpoint = current.SocksHost + ":" + current.SocksPort;
                var args = String.Format("-N -D {0} -o ExitOnForwardFailure=yes -o ServerAliveInterval=30 -o ServerAliveCountMax=3 {1}", QuoteArg(endpoint), QuoteArg(sshTarget));
                var psi = new ProcessStartInfo("ssh.exe", args)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                sshProcess = Process.Start(psi);
                SafeLog.Info("SOCKS start requested. PID=" + (sshProcess == null ? "unknown" : sshProcess.Id.ToString()) + "; port=" + current.SocksPort + "; sshProfile=" + sshTarget + ".");

                if (!requireListening) return true;

                for (var i = 0; i < 10; i++)
                {
                    System.Threading.Thread.Sleep(500);
                    if (IsListening()) return true;
                    if (sshProcess != null && sshProcess.HasExited) break;
                }

                StopTunnel();
                SafeLog.Info("SSH profile did not become ready: " + sshTarget + ".");
                return false;
            }
            catch (Exception ex)
            {
                SafeLog.Error("Failed to start SSH tunnel for profile " + sshTarget + ".", ex);
                if (showErrors) System.Windows.Forms.MessageBox.Show("Не удалось запустить SSH. Проверьте выбранный SSH-профиль и соединение.", AppConstants.ProductName);
                return false;
            }
        }

        private static List<string> BuildProfileTargets(AppSettings settings)
        {
            var targets = new List<string>();
            var selected = (settings.SshProfile ?? String.Empty).Trim();
            AddTarget(targets, selected);

            if (settings.SshProfiles != null)
            {
                foreach (var profile in settings.SshProfiles)
                {
                    if (profile == null) continue;
                    AddTarget(targets, profile.Target);
                }
            }

            return targets;
        }

        private static void AddTarget(List<string> targets, string target)
        {
            target = (target ?? String.Empty).Trim();
            if (String.IsNullOrWhiteSpace(target)) return;
            foreach (var existing in targets)
            {
                if (String.Equals(existing, target, StringComparison.OrdinalIgnoreCase)) return;
            }
            targets.Add(target);
        }

        private static string QuoteArg(string value)
        {
            if (value == null) return "\"\"";
            if (value.IndexOfAny(new[] { ' ', '\t', '\"' }) < 0) return value;
            return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
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

    internal static class ConnectionMetrics
    {
        private const string SpeedTestUrl = "https://speed.cloudflare.com/__down?bytes=10000000";

        public static int? MeasureSocksLatencyMs(AppSettings settings, int timeoutMs)
        {
            if (settings == null) return null;

            Uri target;
            if (!Uri.TryCreate(settings.TestEndpoint, UriKind.Absolute, out target)) return null;

            var host = target.DnsSafeHost;
            if (String.IsNullOrWhiteSpace(host)) return null;
            var port = target.IsDefaultPort
                ? (String.Equals(target.Scheme, "https", StringComparison.OrdinalIgnoreCase) ? 443 : 80)
                : target.Port;

            try
            {
                var stopwatch = Stopwatch.StartNew();
                using (var client = new TcpClient())
                {
                    var connect = client.BeginConnect(settings.SocksHost, settings.SocksPort, null, null);
                    if (!connect.AsyncWaitHandle.WaitOne(Math.Max(1000, timeoutMs)))
                    {
                        return null;
                    }

                    client.EndConnect(connect);
                    using (var stream = client.GetStream())
                    {
                        stream.ReadTimeout = Math.Max(1000, timeoutMs);
                        stream.WriteTimeout = Math.Max(1000, timeoutMs);

                        var greeting = new byte[] { 0x05, 0x01, 0x00 };
                        stream.Write(greeting, 0, greeting.Length);

                        var greetingReply = new byte[2];
                        if (!ReadExact(stream, greetingReply, 0, greetingReply.Length)) return null;
                        if (greetingReply[0] != 0x05 || greetingReply[1] != 0x00) return null;

                        var hostBytes = Encoding.ASCII.GetBytes(host);
                        if (hostBytes.Length < 1 || hostBytes.Length > 255) return null;

                        var request = new byte[7 + hostBytes.Length];
                        request[0] = 0x05;
                        request[1] = 0x01;
                        request[2] = 0x00;
                        request[3] = 0x03;
                        request[4] = (byte)hostBytes.Length;
                        Buffer.BlockCopy(hostBytes, 0, request, 5, hostBytes.Length);
                        request[5 + hostBytes.Length] = (byte)((port >> 8) & 0xff);
                        request[6 + hostBytes.Length] = (byte)(port & 0xff);
                        stream.Write(request, 0, request.Length);

                        var replyHeader = new byte[4];
                        if (!ReadExact(stream, replyHeader, 0, replyHeader.Length)) return null;
                        if (replyHeader[0] != 0x05 || replyHeader[1] != 0x00) return null;

                        var addressBytes = 0;
                        if (replyHeader[3] == 0x01)
                        {
                            addressBytes = 4;
                        }
                        else if (replyHeader[3] == 0x04)
                        {
                            addressBytes = 16;
                        }
                        else if (replyHeader[3] == 0x03)
                        {
                            var length = new byte[1];
                            if (!ReadExact(stream, length, 0, 1)) return null;
                            addressBytes = length[0];
                        }
                        else
                        {
                            return null;
                        }

                        var remainder = new byte[addressBytes + 2];
                        if (!ReadExact(stream, remainder, 0, remainder.Length)) return null;

                        stopwatch.Stop();
                        return (int)Math.Max(1, Math.Round(stopwatch.Elapsed.TotalMilliseconds));
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        public static double? MeasureDownloadMbps(AppSettings settings, out string error)
        {
            error = null;
            if (settings == null)
            {
                error = "Настройки недоступны.";
                return null;
            }

            try
            {
                var testUrl = SpeedTestUrl + "&t=" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture);
                var args =
                    "--socks5-hostname \"" + settings.SocksHost + ":" + settings.SocksPort + "\" " +
                    "-L -sS --connect-timeout 10 --max-time 35 -o NUL -w \"%{speed_download}\" \"" + testUrl + "\"";

                var psi = new ProcessStartInfo("curl.exe", args)
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    if (process == null)
                    {
                        error = "Не удалось запустить curl.exe.";
                        return null;
                    }

                    var output = process.StandardOutput.ReadToEnd();
                    var stderr = process.StandardError.ReadToEnd();
                    if (!process.WaitForExit(40000))
                    {
                        try { process.Kill(); } catch { }
                        error = "Тест скорости превысил лимит времени.";
                        return null;
                    }

                    if (process.ExitCode != 0)
                    {
                        error = String.IsNullOrWhiteSpace(stderr) ? "curl завершился с ошибкой." : SafeLog.Redact(stderr.Trim());
                        return null;
                    }

                    double bytesPerSecond;
                    if (!Double.TryParse(output.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out bytesPerSecond) || bytesPerSecond <= 0)
                    {
                        error = "Не удалось разобрать результат теста скорости.";
                        return null;
                    }

                    return bytesPerSecond * 8.0 / 1000000.0;
                }
            }
            catch (Exception ex)
            {
                error = SafeLog.Redact(ex.Message);
                return null;
            }
        }

        private static bool ReadExact(NetworkStream stream, byte[] buffer, int offset, int count)
        {
            var read = 0;
            while (read < count)
            {
                var current = stream.Read(buffer, offset + read, count - read);
                if (current <= 0) return false;
                read += current;
            }
            return true;
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

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
        // Target remains the stable selection key; old profiles use an OpenSSH alias/address.
        public string Target { get; set; }
        public string Server { get; set; }
        public string User { get; set; }
        public int Port { get; set; }
        public string IdentityFile { get; set; }
        public SshProfileSetting() { Port = 22; }
        internal bool IsDirect { get { return !String.IsNullOrWhiteSpace(Server); } }
        internal string Address { get { return IsDirect ? User + "@" + Server + ":" + Port : Target ?? ""; } }

        public SshProfileSetting Clone()
        {
            return new SshProfileSetting
            {
                Name = Name,
                Target = Target, Server = Server, User = User, Port = Port, IdentityFile = IdentityFile
            };
        }

        public override string ToString()
        {
            if (String.IsNullOrWhiteSpace(Name)) return Address;
            if (String.Equals(Name, Address, StringComparison.OrdinalIgnoreCase)) return Name;
            return Name + " — " + Address;
        }
    }


    // One source of SSH arguments for background tunnels, first login and diagnostics.
    internal static class SshConnection
    {
        internal static SshProfileSetting Resolve(AppSettings settings, string target)
        {
            if (settings.SshProfiles != null)
                foreach (var profile in settings.SshProfiles)
                    if (profile != null && String.Equals(profile.Target, target, StringComparison.OrdinalIgnoreCase)) return profile.Clone();
            return new SshProfileSetting { Target = target };
        }
        internal static string Signature(AppSettings settings)
        {
            var p = Resolve(settings, settings.SshProfile);
            // Display names do not change the route. Use serialization to avoid ambiguous separators.
            return new JavaScriptSerializer().Serialize(new[] { p.Target, p.Server, p.User,
                p.IsDirect ? p.Port.ToString(CultureInfo.InvariantCulture) : "", p.IdentityFile });
        }
        internal static void Validate(SshProfileSetting profile)
        {
            if (profile == null) throw new ArgumentException("Выберите SSH-подключение.");
            if (!profile.IsDirect) {
                var target = profile.Target ?? "";
                if (target.Length == 0 || target[0] == '-' || !Regex.IsMatch(target, @"\A[A-Za-z0-9_.@:\[\]-]+\z"))
                    throw new ArgumentException("Укажите имя из SSH config или user@host без пробелов и параметров команды.");
                return;
            }
            var server = profile.Server ?? "";
            var host = server.Trim('[', ']');
            if (server.Length == 0 || server[0] == '-' || !Regex.IsMatch(server, @"\A[A-Za-z0-9_.:\[\]-]+\z") ||
                Uri.CheckHostName(host) == UriHostNameType.Unknown)
                throw new ArgumentException("Сервер: укажите IP-адрес или доменное имя без логина, порта и https://.");
            if (!Regex.IsMatch(profile.User ?? "", @"\A[A-Za-z0-9_][A-Za-z0-9_.-]*\$?\z"))
                throw new ArgumentException("Логин SSH: укажите пользователя сервера, например ubuntu или root.");
            if (profile.Port < 1 || profile.Port > 65535) throw new ArgumentException("Порт SSH должен быть от 1 до 65535.");
            var key = profile.IdentityFile ?? "";
            if (key.IndexOfAny(new[] { '\r', '\n', '\0' }) >= 0 || (key.Length > 0 && !Path.IsPathRooted(key)))
                throw new ArgumentException("Выберите файл закрытого SSH-ключа на этом компьютере или оставьте поле пустым.");
        }
        internal static string[] Arguments(SshProfileSetting profile)
        {
            Validate(profile);
            if (!profile.IsDirect) return new[] { profile.Target };
            var args = new List<string> { "-p", profile.Port.ToString(CultureInfo.InvariantCulture), "-l", profile.User };
            if (!String.IsNullOrWhiteSpace(profile.IdentityFile)) {
                args.Add("-i"); args.Add(profile.IdentityFile); args.Add("-o"); args.Add("IdentitiesOnly=yes");
            }
            args.Add(profile.Server);
            return args.ToArray();
        }
        internal static string CommandArguments(SshProfileSetting profile)
        {
            return String.Join(" ", Array.ConvertAll(Arguments(profile), Quote));
        }
        // Windows CRT/OpenSSH quoting: only slashes before quotes or the closing quote are doubled.
        internal static string Quote(string value)
        {
            if (value == null) return "\"\"";
            if (value.Length > 0 && value.IndexOfAny(new[] { ' ', '\t', '"' }) < 0) return value;
            var result = new StringBuilder("\""); int slashes = 0;
            foreach (var c in value) {
                if (c == '\\') { slashes++; continue; }
                result.Append('\\', c == '"' ? slashes * 2 + 1 : slashes);
                result.Append(c); slashes = 0;
            }
            result.Append('\\', slashes * 2); result.Append('"'); return result.ToString();
        }
    }

    internal sealed class AppSettings
    {
        public string SocksHost { get; set; }
        public int SocksPort { get; set; }
        public int HttpProxyPort { get; set; }
        public bool AutoHttpProxyPort { get; set; }
        public string SshProfile { get; set; }
        public List<SshProfileSetting> SshProfiles { get; set; }
        public bool AutoSwitchSshProfile { get; set; }
        public bool AutoStartSocks { get; set; }
        public bool AutoRestartSocks { get; set; }
        public bool AutoCliProxy { get; set; }
        public bool AutoSystemProxy { get; set; }
        public bool TrayCloseExplained { get; set; }
        public int ClipboardClearSeconds { get; set; }
        public string TestEndpoint { get; set; }

        public AppSettings()
        {
            // Older settings files omit this property; explicit false is preserved.
            AutoRestartSocks = true;
            HttpProxyPort = 1881;
            AutoHttpProxyPort = true;
        }

        public AppSettings Clone()
        {
            return new JavaScriptSerializer().Deserialize<AppSettings>(new JavaScriptSerializer().Serialize(this));
        }

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
                AutoRestartSocks = true,
                AutoCliProxy = false,
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
                var settings = DeserializeSettings(text);
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
            var pending = AppPaths.SettingsPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(pending, serializer.Serialize(settings));
                if (File.Exists(AppPaths.SettingsPath)) File.Replace(pending, AppPaths.SettingsPath, null);
                else File.Move(pending, AppPaths.SettingsPath);
            }
            finally { if (File.Exists(pending)) File.Delete(pending); }
            Current = settings;
            SafeLog.Info("Settings saved.");
        }

        internal static AppSettings DeserializeSettings(string text)
        {
            var json = new JavaScriptSerializer();
            var settings = json.Deserialize<AppSettings>(text) ?? AppSettings.Defaults();
            var values = json.Deserialize<Dictionary<string, object>>(text);
            if (values == null) return settings;
            values = new Dictionary<string, object>(values, StringComparer.OrdinalIgnoreCase);
            // A new explicit false wins over stale legacy fields. Save writes
            // only AutoCliProxy, so turning it off cannot resurrect a legacy on.
            if (!values.ContainsKey("AutoCliProxy"))
                settings.AutoCliProxy = LegacyFlag(values, "AutoApplyProxy") || LegacyFlag(values, "AutoCodexProxy");
            return settings;
        }

        private static bool LegacyFlag(Dictionary<string, object> values, string name)
        {
            object value;
            return values.TryGetValue(name, out value) && value is bool && (bool)value;
        }

        private static void Normalize(AppSettings settings)
        {
            if (String.IsNullOrWhiteSpace(settings.SocksHost)) settings.SocksHost = "127.0.0.1";
            if (settings.SocksPort < 1 || settings.SocksPort > 65535) settings.SocksPort = 1080;
            if (settings.HttpProxyPort < 1 || settings.HttpProxyPort > 65535) settings.HttpProxyPort = 1881;
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
                    var copy = profile.Clone(); copy.Name = name; copy.Target = target;
                    copy.Server = (copy.Server ?? "").Trim(); copy.User = (copy.User ?? "").Trim();
                    copy.IdentityFile = (copy.IdentityFile ?? "").Trim();
                    normalized.Add(copy);
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

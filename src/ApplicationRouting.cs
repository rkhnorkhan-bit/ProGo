using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ProGo
{
    // Rules are opt-in. "Auto" currently means the safe default: DIRECT.
    // It is reserved for an adaptive policy in a later version.
    internal sealed class ApplicationRouteRule
    {
        public string ExecutablePath { get; set; }
        public string Mode { get; set; }

        public ApplicationRouteRule Clone()
        {
            return new ApplicationRouteRule { ExecutablePath = ExecutablePath, Mode = Mode };
        }
    }

    internal sealed class DiscoveredApplication
    {
        public string Name { get; set; }
        public string ExecutablePath { get; set; }
    }

    internal static class ApplicationRouting
    {
        public const string Auto = "auto";
        public const string Proxy = "proxy";
        public const string Direct = "direct";
        public const int MaxRules = 1000;

        internal static string NormalizeMode(string value)
        {
            if (String.Equals(value, Proxy, StringComparison.OrdinalIgnoreCase)) return Proxy;
            if (String.Equals(value, Direct, StringComparison.OrdinalIgnoreCase)) return Direct;
            return Auto;
        }

        internal static string NormalizePath(string path)
        {
            if (String.IsNullOrWhiteSpace(path) || path.Length > 2048 || path.IndexOf('\0') >= 0) return null;
            try
            {
                var expanded = Environment.ExpandEnvironmentVariables(path.Trim().Trim('"'));
                if (!Path.IsPathRooted(expanded)) return null;
                var full = Path.GetFullPath(expanded);
                if (!String.Equals(Path.GetExtension(full), ".exe", StringComparison.OrdinalIgnoreCase)) return null;
                return full;
            }
            catch (Exception) { return null; }
        }

        internal static bool IsProtectedPath(string path)
        {
            var normalized = NormalizePath(path);
            if (normalized == null) return true;
            var windows = Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.Windows))
                .TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return normalized.StartsWith(windows, StringComparison.OrdinalIgnoreCase)
                || String.Equals(normalized, Application.ExecutablePath, StringComparison.OrdinalIgnoreCase)
                || String.Equals(Path.GetFileName(normalized), "sing-box.exe", StringComparison.OrdinalIgnoreCase);
        }

        internal static List<ApplicationRouteRule> Normalize(IEnumerable<ApplicationRouteRule> rules)
        {
            var normalized = new List<ApplicationRouteRule>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (rules == null) return normalized;
            foreach (var rule in rules)
            {
                if (rule == null) continue;
                var path = NormalizePath(rule.ExecutablePath);
                if (path == null || !seen.Add(path)) continue;
                if (normalized.Count == MaxRules) break;
                var mode = NormalizeMode(rule.Mode);
                // Never forward Windows system components, ProGo or the routing engine.
                if (mode == Proxy && IsProtectedPath(path)) mode = Direct;
                normalized.Add(new ApplicationRouteRule { ExecutablePath = path, Mode = mode });
            }
            return normalized;
        }

        internal static List<DiscoveredApplication> Discover()
        {
            var applications = new Dictionary<string, DiscoveredApplication>(StringComparer.OrdinalIgnoreCase);
            foreach (var process in Process.GetProcesses())
            {
                using (process)
                {
                    try { AddApplication(applications, process.ProcessName, process.MainModule.FileName); }
                    catch (Exception) { /* Other users and protected processes are inaccessible. */ }
                }
            }

            foreach (var hive in new[] { RegistryHive.CurrentUser, RegistryHive.LocalMachine })
            foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                try
                {
                    using (var root = RegistryKey.OpenBaseKey(hive, view))
                    using (var uninstall = root.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"))
                    {
                        if (uninstall == null) continue;
                        foreach (var subName in uninstall.GetSubKeyNames())
                        {
                            try
                            {
                                using (var sub = uninstall.OpenSubKey(subName))
                                {
                                    if (sub == null) continue;
                                    var icon = Convert.ToString(sub.GetValue("DisplayIcon", ""));
                                    var name = Convert.ToString(sub.GetValue("DisplayName", ""));
                                    AddApplication(applications, name, ExecutableFromIcon(icon));
                                }
                            }
                            catch (Exception) { /* Broken uninstall entries are normal. */ }
                        }
                    }
                }
                catch (Exception) { /* Registry view may be unavailable. */ }
            }
            return applications.Values.OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
        }

        private static string ExecutableFromIcon(string icon)
        {
            if (String.IsNullOrWhiteSpace(icon)) return null;
            // Typical DisplayIcon: "C:\Program Files\Foo\foo.exe",0
            var match = Regex.Match(icon, @"^\s*""([^""]+\.exe)""(?:,\d+)?\s*$", RegexOptions.IgnoreCase);
            if (match.Success) return match.Groups[1].Value;
            match = Regex.Match(icon, @"^\s*(.+?\.exe)(?:,\d+)?\s*$", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : null;
        }

        private static void AddApplication(IDictionary<string, DiscoveredApplication> apps, string name, string path)
        {
            var normalized = NormalizePath(path);
            if (normalized == null || IsProtectedPath(normalized) || !File.Exists(normalized)) return;
            if (apps.ContainsKey(normalized)) return;
            apps[normalized] = new DiscoveredApplication
            {
                Name = String.IsNullOrWhiteSpace(name) ? Path.GetFileNameWithoutExtension(normalized) : name.Trim(),
                ExecutablePath = normalized
            };
        }

        internal static string ManualDirectPath
        {
            get { return Path.Combine(AppPaths.Root, "smart-routing", "manual-direct.json"); }
        }
        internal static string ManualProxyPath
        {
            get { return Path.Combine(AppPaths.Root, "smart-routing", "manual-proxy.json"); }
        }

        internal static string GenerateApplicationRuleSet(AppSettings settings, string mode)
        {
            var paths = Normalize(settings == null ? null : settings.ApplicationRoutes)
                .Where(r => r.Mode == mode).Select(r => r.ExecutablePath)
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToArray();
            var rules = paths.Length == 0 ? new object[0] :
                new object[] { new { process_path = paths } };
            return new JavaScriptSerializer().Serialize(new { version = 1, rules });
        }

        internal static void SaveManualRuleSets(AppSettings settings)
        {
            SmartDomainPolicy.WriteAtomic(ManualDirectPath, GenerateApplicationRuleSet(settings, Direct));
            SmartDomainPolicy.WriteAtomic(ManualProxyPath, GenerateApplicationRuleSet(settings, Proxy));
        }

        // sing-box 1.14.4: strict rule order, manual app rules precede auto domains.
        // Local files hot-reload without dropping active TCP sessions.
        internal static string GenerateSingBoxConfig(AppSettings settings, string autoRuleSet = null,
            string manualDirectRuleSet = null, string manualProxyRuleSet = null)
        {
            if (settings == null) throw new ArgumentNullException("settings");
            if (String.IsNullOrWhiteSpace(autoRuleSet)) autoRuleSet = SmartDomainPolicy.RuleSetPath;
            if (String.IsNullOrWhiteSpace(manualDirectRuleSet)) manualDirectRuleSet = ManualDirectPath;
            if (String.IsNullOrWhiteSpace(manualProxyRuleSet)) manualProxyRuleSet = ManualProxyPath;

            var routing = new List<object>();
            routing.Add(new { action = "sniff" });
            routing.Add(new { protocol = "dns", action = "hijack-dns" });
            routing.Add(new { ip_is_private = true, action = "route", outbound = Direct });
            routing.Add(new { process_path = new[] { Application.ExecutablePath },
                              action = "route", outbound = Direct });
            routing.Add(new { rule_set = "manual-direct", action = "route", outbound = Direct });
            routing.Add(new { rule_set = "manual-proxy", network = "udp", action = "reject" });
            routing.Add(new { rule_set = "manual-proxy", network = "tcp", action = "route", outbound = Proxy });
            routing.Add(new { domain_suffix = new[] { "lencr.org", "battle.net", "blizzard.com" },
                              action = "route", outbound = Direct });
            routing.Add(new { rule_set = "auto-proxy", network = "udp", action = "reject" });
            routing.Add(new { rule_set = "auto-proxy", network = "tcp", action = "route", outbound = Proxy });

            var config = new Dictionary<string, object>
            {
                { "log", new { level = "warn", timestamp = true } },
                { "dns", new { servers = new object[] { new { type = "local", tag = "local" } }, final = "local" } },
                { "inbounds", new object[] {
                    new {
                        type = "tun", tag = "progo-tun", interface_name = "ProGoTUN",
                        address = new[] { "172.25.248.1/30", "fdfe:dcba:9876::1/126" },
                        auto_route = true, dns_mode = "hijack",
                        strict_route = false
                    }
                } },
                { "outbounds", new object[] {
                    new { type = "direct", tag = Direct },
                    new {
                        type = "socks", tag = Proxy, server = settings.SocksHost,
                        server_port = settings.SocksPort, version = "5", network = "tcp"
                    }
                } },
                { "route", new {
                    auto_detect_interface = true,
                    rule_set = new object[] {
                        new { type = "local", tag = "manual-direct", format = "source", path = manualDirectRuleSet },
                        new { type = "local", tag = "manual-proxy", format = "source", path = manualProxyRuleSet },
                        new { type = "local", tag = "auto-proxy", format = "source", path = autoRuleSet }
                    },
                    rules = routing,
                    final = Direct
                } }
            };
            return new JavaScriptSerializer { MaxJsonLength = 10 * 1024 * 1024 }.Serialize(config);
        }
    }
}

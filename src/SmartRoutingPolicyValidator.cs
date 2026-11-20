using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

namespace ProGo
{
    // Runs inside the protected TUN host before executing any privileged
    // engine. It rejects unrecognized sing-box operations even if an
    // unprivileged app edits active.json in its own user profile.
    internal static class SmartRoutingPolicyValidator
    {
        private static IDictionary<string, object> Map(object value)
        {
            return value as IDictionary<string, object>;
        }
        private static object[] Items(object value)
        {
            return value as object[];
        }
        private static bool Has(IDictionary<string, object> value, params string[] keys)
        {
            return value != null && value.Count == keys.Length && keys.All(value.ContainsKey);
        }
        private static object Get(IDictionary<string, object> value, string key)
        {
            object found;
            return value != null && value.TryGetValue(key, out found) ? found : null;
        }
        private static bool Eq(object value, string expected)
        {
            return String.Equals(Convert.ToString(value), expected, StringComparison.Ordinal);
        }
        private static bool IsTrue(object value)
        {
            return value is bool && (bool)value;
        }
        private static bool IsFalse(object value)
        {
            return value is bool && !(bool)value;
        }
        private static bool SamePath(object actual, string expected)
        {
            try
            {
                return String.Equals(Path.GetFullPath(Convert.ToString(actual)),
                    Path.GetFullPath(expected), StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }
        private static bool IsLoopback(object value)
        {
            var ip = Convert.ToString(value);
            return ip == "127.0.0.1" || ip == "::1";
        }

        internal static bool Validate(string configFile)
        {
            try
            {
                if (!File.Exists(configFile) || new FileInfo(configFile).Length > 262144) return false;
                var root = Path.GetDirectoryName(Path.GetFullPath(configFile));
                var document = Map(new JavaScriptSerializer().DeserializeObject(File.ReadAllText(configFile)));
                if (!Has(document, "log", "dns", "inbounds", "outbounds", "route")) return false;

                var log = Map(Get(document, "log"));
                if (!Has(log, "level", "timestamp") || !Eq(Get(log, "level"), "warn") ||
                    !IsTrue(Get(log, "timestamp"))) return false;

                var dns = Map(Get(document, "dns"));
                if (!Has(dns, "servers", "final") || !Eq(Get(dns, "final"), "local")) return false;
                var servers = Items(Get(dns, "servers"));
                if (servers == null || servers.Length != 1) return false;
                var server = Map(servers[0]);
                if (!Has(server, "type", "tag") || !Eq(Get(server, "type"), "local") ||
                    !Eq(Get(server, "tag"), "local")) return false;

                var inbounds = Items(Get(document, "inbounds"));
                if (inbounds == null || inbounds.Length != 1) return false;
                var inbound = Map(inbounds[0]);
                if (!Has(inbound, "type", "tag", "interface_name", "address", "auto_route", "dns_mode", "strict_route"))
                    return false;
                if (!Eq(Get(inbound, "type"), "tun") || !Eq(Get(inbound, "tag"), "progo-tun") ||
                    !Eq(Get(inbound, "interface_name"), "ProGoTUN") ||
                    !IsTrue(Get(inbound, "auto_route")) || !IsFalse(Get(inbound, "strict_route")) ||
                    !Eq(Get(inbound, "dns_mode"), "hijack")) return false;
                var addresses = Items(Get(inbound, "address"));
                if (addresses == null || addresses.Length != 2 ||
                    !Eq(addresses[0], "172.25.248.1/30") ||
                    !Eq(addresses[1], "fdfe:dcba:9876::1/126")) return false;

                var outbounds = Items(Get(document, "outbounds"));
                if (outbounds == null || outbounds.Length != 2) return false;
                var direct = Map(outbounds[0]);
                var socks = Map(outbounds[1]);
                if (!Has(direct, "type", "tag") || !Eq(Get(direct, "type"), "direct") ||
                    !Eq(Get(direct, "tag"), "direct")) return false;
                if (!Has(socks, "type", "tag", "server", "server_port", "version", "network") ||
                    !Eq(Get(socks, "type"), "socks") || !Eq(Get(socks, "tag"), "proxy") ||
                    !IsLoopback(Get(socks, "server")) || !Eq(Get(socks, "version"), "5") ||
                    !Eq(Get(socks, "network"), "tcp")) return false;
                var port = Convert.ToInt32(Get(socks, "server_port"));
                if (port < 1 || port > 65535) return false;

                var route = Map(Get(document, "route"));
                if (!Has(route, "auto_detect_interface", "rule_set", "rules", "final") ||
                    !IsTrue(Get(route, "auto_detect_interface")) || !Eq(Get(route, "final"), "direct")) return false;

                var setDefinitions = Items(Get(route, "rule_set"));
                if (setDefinitions == null || setDefinitions.Length != 3) return false;
                var names = new[] { "manual-direct", "manual-proxy", "auto-proxy" };
                for (int i = 0; i < names.Length; i++)
                {
                    var set = Map(setDefinitions[i]);
                    var full = Path.Combine(root, names[i] + ".json");
                    if (!Has(set, "type", "tag", "format", "path") ||
                        !Eq(Get(set, "type"), "local") || !Eq(Get(set, "format"), "source") ||
                        !Eq(Get(set, "tag"), names[i]) || !SamePath(Get(set, "path"), full) ||
                        !ValidateSet(full, names[i] == "auto-proxy")) return false;
                }

                var rules = Items(Get(route, "rules"));
                if (rules == null || rules.Length != 10) return false;
                var r0 = Map(rules[0]);
                var r1 = Map(rules[1]);
                var r2 = Map(rules[2]);
                var r3 = Map(rules[3]);
                var r4 = Map(rules[4]);
                var r5 = Map(rules[5]);
                var r6 = Map(rules[6]);
                var r7 = Map(rules[7]);
                var r8 = Map(rules[8]);
                var r9 = Map(rules[9]);

                if (!Has(r0, "action") || !Eq(Get(r0, "action"), "sniff")) return false;
                if (!Has(r1, "protocol", "action") || !Eq(Get(r1, "protocol"), "dns") ||
                    !Eq(Get(r1, "action"), "hijack-dns")) return false;
                if (!Has(r2, "ip_is_private", "action", "outbound") ||
                    !IsTrue(Get(r2, "ip_is_private")) ||
                    !Eq(Get(r2, "action"), "route") || !Eq(Get(r2, "outbound"), "direct")) return false;
                if (!Has(r3, "process_path", "action", "outbound") ||
                    !Eq(Get(r3, "action"), "route") || !Eq(Get(r3, "outbound"), "direct")) return false;
                var ownPaths = Items(Get(r3, "process_path"));
                if (ownPaths == null || ownPaths.Length != 1 ||
                    !Eq(Path.GetFileName(Convert.ToString(ownPaths[0])), "ProGo.exe")) return false;
                if (!Rule(r4, "manual-direct", null, "route", "direct")) return false;
                if (!Rule(r5, "manual-proxy", "udp", "reject", null)) return false;
                if (!Rule(r6, "manual-proxy", "tcp", "route", "proxy")) return false;
                if (!Has(r7, "domain_suffix", "action", "outbound") ||
                    !Eq(Get(r7, "action"), "route") || !Eq(Get(r7, "outbound"), "direct")) return false;
                var exceptions = Items(Get(r7, "domain_suffix"));
                if (exceptions == null || exceptions.Length != 3 ||
                    !Eq(exceptions[0], "lencr.org") || !Eq(exceptions[1], "battle.net") ||
                    !Eq(exceptions[2], "blizzard.com")) return false;
                if (!Rule(r8, "auto-proxy", "udp", "reject", null)) return false;
                if (!Rule(r9, "auto-proxy", "tcp", "route", "proxy")) return false;
                return true;
            }
            catch (Exception) { return false; }
        }

        private static bool Rule(IDictionary<string, object> rule, string set,
            string network, string action, string outbound)
        {
            return (network == null ? Has(rule, "rule_set", "action", "outbound")
                : outbound == null ? Has(rule, "rule_set", "network", "action")
                : Has(rule, "rule_set", "network", "action", "outbound")) &&
                Eq(Get(rule, "rule_set"), set) &&
                (network == null || Eq(Get(rule, "network"), network)) &&
                Eq(Get(rule, "action"), action) &&
                (outbound == null || Eq(Get(rule, "outbound"), outbound));
        }

        private static bool ValidateSet(string file, bool domainSet)
        {
            if (!File.Exists(file) || new FileInfo(file).Length > 524288) return false;
            var root = Map(new JavaScriptSerializer().DeserializeObject(File.ReadAllText(file)));
            if (!Has(root, "version", "rules") || Convert.ToInt32(Get(root, "version")) != 1) return false;
            var rules = Items(Get(root, "rules"));
            if (rules == null || rules.Length > 1) return false;
            if (rules.Length == 0) return true;
            var rule = Map(rules[0]);
            var key = domainSet ? "domain_suffix" : "process_path";
            if (!Has(rule, key)) return false;
            var entries = Items(Get(rule, key));
            if (entries == null || entries.Length == 0 || entries.Length > 1000) return false;
            var approved = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
                "chatgpt.com", "openai.com", "oaistatic.com", "oaiusercontent.com",
                "github.com", "githubusercontent.com", "githubassets.com",
                "claude.ai", "anthropic.com"
            };
            foreach (var entry in entries)
            {
                var text = entry as string;
                if (String.IsNullOrWhiteSpace(text)) return false;
                if (domainSet)
                {
                    if (!approved.Contains(text)) return false;
                }
                else
                {
                    if (text.Length > 2048 || !Path.IsPathRooted(text) ||
                        !String.Equals(Path.GetExtension(text), ".exe", StringComparison.OrdinalIgnoreCase)) return false;
                }
            }
            return true;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace ProGo
{
    internal static class ApplicationRoutingSelfTest
    {
        public static void Run()
        {
            var rules = ApplicationRouting.Normalize(new[] {
                new ApplicationRouteRule { ExecutablePath = @"C:\Apps\Example.exe", Mode = "proxy" },
                new ApplicationRouteRule { ExecutablePath = @"c:\apps\EXAMPLE.exe", Mode = "direct" },
                new ApplicationRouteRule { ExecutablePath = @"C:\Apps\Other.exe", Mode = "direct" },
                new ApplicationRouteRule { ExecutablePath = "relative.exe", Mode = "proxy" },
                new ApplicationRouteRule { ExecutablePath = @"C:\Apps\Third.exe", Mode = "unknown" }
            });
            Assert(rules.Count == 3, "duplicate/invalid rule normalization");
            Assert(rules[0].Mode == ApplicationRouting.Proxy, "first explicit route has precedence");
            Assert(rules[2].Mode == ApplicationRouting.Auto, "unknown modes default to auto");
            var settings = AppSettings.Defaults();
            settings.ApplicationRoutes = rules;
            var json = ApplicationRouting.GenerateSingBoxConfig(settings);
            var root = new JavaScriptSerializer().DeserializeObject(json) as Dictionary<string, object>;
            Assert(root != null && root.ContainsKey("route") && root.ContainsKey("outbounds"), "routing JSON parses");
            Assert(json.Contains("\"process_path\""), "rules are emitted");
            Assert(json.Contains("\"action\":\"reject\""), "unsupported proxy UDP is explicitly rejected");
            Assert(json.Contains("\"final\":\"direct\""), "safe default is direct");
            Assert(json.Contains("\"auto_detect_interface\":true"), "avoid TUN route loop");
            Assert(ApplicationRouting.NormalizePath("relative.exe") == null, "relative paths must be rejected");
            var decision = new SmartRouteDecision();
            Assert(!decision.Observe(false, true), "first failed direct probe must not switch");
            Assert(decision.Observe(false, true), "two independent positive proxy checks enable routing");
            Assert(decision.ViaProxy, "proxy route enabled");
            Assert(!decision.Observe(false, false), "two failed routes must not change decision");
            Assert(!decision.Observe(true, false), "first direct success requires hysteresis");
            Assert(!decision.Observe(true, false), "second direct success requires hysteresis");
            Assert(decision.Observe(true, false) && !decision.ViaProxy, "third direct success restores direct route");

            var empty = SmartDomainPolicy.GenerateRuleSet(new string[0]);
            Assert(empty.Contains("\"version\":1") && empty.Contains("\"rules\":[]"), "valid empty hot reload rule-set");
            var source = SmartDomainPolicy.GenerateRuleSet(new[] { "chatgpt.com", "CHATGPT.COM" });
            Assert(source.Contains("chatgpt.com") && !source.Contains("CHATGPT.COM"), "normalized rule set");
            bool rejected = false;
            try { SmartDomainPolicy.GenerateRuleSet(new[] { "127.0.0.1", "example.invalid" }); }
            catch (InvalidOperationException) { rejected = true; }
            Assert(rejected, "automatic monitor may not proxy arbitrary destinations");

            // Manual application routing has priority over automatic domain rules.
            var idxDirect = json.IndexOf("\"process_path\"", StringComparison.Ordinal);
            var idxAuto = json.IndexOf("\"rule_set\":\"auto-proxy\"", StringComparison.Ordinal);
            Assert(idxDirect >= 0 && idxAuto > idxDirect, "explicit application rules precede auto rules");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Routing self-test: " + message);
        }
    }
}

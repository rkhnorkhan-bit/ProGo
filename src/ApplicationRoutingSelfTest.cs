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
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Routing self-test: " + message);
        }
    }
}

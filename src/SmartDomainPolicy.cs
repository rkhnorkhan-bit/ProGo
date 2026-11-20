using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;

namespace ProGo
{
    // Conservative service catalogue. Never infer blocked sites from geographic
    // location, HTTP 4xx, a single timeout or an unsolicited network response.
    internal sealed class SmartDomainGroup
    {
        internal string Name;
        internal string CheckUrl;
        internal string[] Suffixes;
    }

    internal sealed class SmartRouteDecision
    {
        internal bool ViaProxy;
        internal int DirectFailures;
        internal int ProxySuccesses;
        internal int DirectSuccesses;

        internal bool Observe(bool directOk, bool proxyOk)
        {
            if (directOk)
            {
                DirectFailures = 0;
                ProxySuccesses = 0;
                DirectSuccesses++;
                if (ViaProxy && DirectSuccesses >= 3)
                {
                    ViaProxy = false;
                    DirectSuccesses = 0;
                    return true;
                }
                return false;
            }

            DirectSuccesses = 0;
            if (!proxyOk)
            {
                // Both paths failed: do NOT treat as evidence that the site is blocked.
                DirectFailures = 0;
                ProxySuccesses = 0;
                return false;
            }
            DirectFailures++;
            ProxySuccesses++;
            if (!ViaProxy && DirectFailures >= 2 && ProxySuccesses >= 2)
            {
                ViaProxy = true;
                DirectFailures = 0;
                ProxySuccesses = 0;
                return true;
            }
            return false;
        }
    }

    internal static class SmartDomainPolicy
    {
        // Approved, broad service categories are small and auditable.
        // Do not list local, banking, certificate-validation or game services here.
        internal static readonly SmartDomainGroup[] Groups =
        {
            new SmartDomainGroup {
                Name = "OpenAI", CheckUrl = "https://chatgpt.com/",
                Suffixes = new[] { "chatgpt.com", "openai.com", "oaistatic.com", "oaiusercontent.com" }
            },
            new SmartDomainGroup {
                Name = "GitHub", CheckUrl = "https://github.com/",
                Suffixes = new[] { "github.com", "githubusercontent.com", "githubassets.com" }
            },
            new SmartDomainGroup {
                Name = "Claude", CheckUrl = "https://claude.ai/",
                Suffixes = new[] { "claude.ai", "anthropic.com" }
            }
        };

        internal static string RuleSetPath
        {
            get { return Path.Combine(AppPaths.Root, "smart-routing", "auto-proxy.json"); }
        }

        internal static string GenerateRuleSet(IEnumerable<string> suffixes)
        {
            var list = (suffixes ?? Enumerable.Empty<string>())
                .Where(s => s != null)
                .Select(s => s.Trim().ToLowerInvariant())
                .Where(s => s.Length > 0 && s.Length <= 253)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s, StringComparer.Ordinal)
                .ToArray();

            // The list is not arbitrary user input: refuse unknown suffixes.
            var allow = new HashSet<string>(Groups.SelectMany(g => g.Suffixes), StringComparer.OrdinalIgnoreCase);
            foreach (var suffix in list)
                if (!allow.Contains(suffix)) throw new InvalidOperationException("Unapproved automatic domain: " + suffix);

            var rule = new Dictionary<string, object> { { "version", 1 }, { "rules", list.Length == 0
                ? new object[0]
                : new object[] { new Dictionary<string, object> { { "domain_suffix", list } } } } };
            return new JavaScriptSerializer().Serialize(rule);
        }

        internal static void SaveRuleSet(IEnumerable<string> suffixes)
        {
            var next = GenerateRuleSet(suffixes);
            var path = RuleSetPath;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            if (File.Exists(path) && String.Equals(File.ReadAllText(path), next, StringComparison.Ordinal)) return;
            var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temp, next, new UTF8Encoding(false));
                if (File.Exists(path)) File.Replace(temp, path, null);
                else File.Move(temp, path);
            }
            finally
            {
                try { if (File.Exists(temp)) File.Delete(temp); } catch (Exception) { }
            }
        }
    }

    // This monitor NEVER changes the global Windows HTTP proxy, DNS, firewall,
    // installed software or VPN settings. Only its own local sing-box rule-set.
    internal sealed class SmartRouteMonitor : IDisposable
    {
        private readonly string bridgeUrl;
        private readonly Dictionary<string, SmartRouteDecision> state =
            new Dictionary<string, SmartRouteDecision>(StringComparer.OrdinalIgnoreCase);
        private readonly ManualResetEvent stop = new ManualResetEvent(false);
        private Thread thread;
        private bool disposed;
        private readonly object gate = new object();

        public SmartRouteMonitor(string proxyBridgeUrl)
        {
            bridgeUrl = proxyBridgeUrl;
            foreach (var group in SmartDomainPolicy.Groups)
                state[group.Name] = new SmartRouteDecision();
        }

        public void Start()
        {
            lock (gate)
            {
                if (disposed || thread != null) return;
                // Safe startup: no stale decision is automatically trusted.
                SmartDomainPolicy.SaveRuleSet(new string[0]);
                thread = new Thread(Run) { Name = "ProGo SmartRoute Monitor", IsBackground = true };
                thread.Start();
            }
        }

        private void Run()
        {
            // Two observations 15 seconds apart at startup, then every 5 minutes.
            int pass = 0;
            while (!stop.WaitOne(pass == 0 ? 1000 : pass == 1 ? 15000 : 300000))
            {
                if (stop.WaitOne(0)) return;
                try
                {
                    bool changed = false;
                    foreach (var group in SmartDomainPolicy.Groups)
                    {
                        if (stop.WaitOne(0)) return;
                        var direct = Probe(group.CheckUrl, false);
                        var viaProxy = !direct && Probe(group.CheckUrl, true);
                        if (state[group.Name].Observe(direct, viaProxy))
                        {
                            changed = true;
                            SafeLog.Info("SmartRoute decision: " + group.Name + " = " +
                                (state[group.Name].ViaProxy ? "VPS" : "DIRECT") +
                                " after repeated independent reachability probes.");
                        }
                    }

                    if (changed && !stop.WaitOne(0))
                    {
                        var suffixes = SmartDomainPolicy.Groups
                            .Where(group => state[group.Name].ViaProxy)
                            .SelectMany(group => group.Suffixes);
                        SmartDomainPolicy.SaveRuleSet(suffixes);
                    }
                }
                catch (Exception ex) { SafeLog.Error("SmartRoute monitor cycle failed.", ex); }
                pass++;
            }
        }

        private bool Probe(string url, bool viaProxy)
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "HEAD";
                request.Timeout = 6000;
                request.ReadWriteTimeout = 6000;
                request.AllowAutoRedirect = false;
                request.KeepAlive = false;
                request.UserAgent = "ProGoSmartRouting/1.0";
                request.Proxy = viaProxy ? new WebProxy(bridgeUrl) : null;
                using (var response = (HttpWebResponse)request.GetResponse()) { return true; }
            }
            catch (WebException ex)
            {
                // HTTP 401/403/405/429 and other HTTP responses are evidence that
                // TLS and the route reached a server, not evidence of a blockade.
                if (ex.Status == WebExceptionStatus.ProtocolError && ex.Response != null)
                {
                    try { ex.Response.Dispose(); } catch (Exception) { }
                    return true;
                }
                return false;
            }
            catch (Exception) { return false; }
        }

        public void Dispose()
        {
            lock (gate)
            {
                if (disposed) return;
                disposed = true;
                stop.Set();
            }
            var worker = thread;
            if (worker != null && worker != Thread.CurrentThread) worker.Join(200);
            // Background requests can still be finishing. Run() owns this event's
            // final disposal; disposing it here would crash the worker on WaitOne.
        }
    }
}

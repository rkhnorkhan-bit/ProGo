using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace ProGo
{
    // The optional scheduled task runs ONLY the official, hash-pinned sing-box
    // executable from protected Program Files. The main ProGo UI stays unelevated.
    internal static class SmartRoutingTask
    {
        internal const string TaskName = "ProGoSmartRouting";
        internal static string ProtectedEnginePath
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "ProGoRoutingEngine", "sing-box.exe"); }
        }

        internal static string State()
        {
            // Query the user's own fixed task through the built-in Windows module.
            // Validate action and argument before treating it as ProGo-owned.
            var expectedExe = ProtectedEnginePath.Replace("'", "''");
            var expectedArgs = ("run -c \"" + Path.Combine(AppPaths.Root, "smart-routing", "active.json") + "\"")
                .Replace("'", "''");
            var script = "$t=Get-ScheduledTask -TaskName '" + TaskName +
                "' -ErrorAction SilentlyContinue; if($null -eq $t){'MISSING'}" +
                " elseif(@($t.Actions).Count -ne 1){'INVALID'}" +
                " elseif($t.Actions[0].Execute -ine '" + expectedExe +
                "' -or $t.Actions[0].Arguments -cne '" + expectedArgs + "'){'INVALID'}" +
                " else {$t.State.ToString()}";
            var encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
            var psi = new ProcessStartInfo("powershell.exe", "-NoProfile -NonInteractive -EncodedCommand " + encoded)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            try
            {
                using (var p = Process.Start(psi))
                {
                    if (p == null) return "UNKNOWN";
                    if (!p.WaitForExit(5000))
                    {
                        try { p.Kill(); } catch (Exception) { }
                        return "UNKNOWN";
                    }
                    var result = p.StandardOutput.ReadToEnd().Trim();
                    return p.ExitCode == 0 && result.Length != 0 ? result : "UNKNOWN";
                }
            }
            catch (Exception) { return "UNKNOWN"; }
        }

        internal static bool IsManagedState(string state)
        {
            return String.Equals(state, "Ready", StringComparison.OrdinalIgnoreCase) ||
                   String.Equals(state, "Running", StringComparison.OrdinalIgnoreCase) ||
                   String.Equals(state, "Queued", StringComparison.OrdinalIgnoreCase);
        }

        internal static bool IsRunningState(string state)
        {
            return String.Equals(state, "Running", StringComparison.OrdinalIgnoreCase);
        }

        internal static bool Run(out string message)
        {
            return ExecuteTask("/Run /TN \"" + TaskName + "\"", out message);
        }

        internal static bool Stop(out string message)
        {
            return ExecuteTask("/End /TN \"" + TaskName + "\"", out message);
        }

        private static bool ExecuteTask(string arguments, out string message)
        {
            message = null;
            var psi = new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "schtasks.exe"), arguments)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            try
            {
                using (var p = Process.Start(psi))
                {
                    if (p == null) { message = "Планировщик не запустился."; return false; }
                    if (!p.WaitForExit(7000))
                    {
                        try { p.Kill(); } catch (Exception) { }
                        message = "Планировщик не ответил вовремя."; return false;
                    }
                    var output = p.ExitCode == 0 ? p.StandardOutput.ReadToEnd() : p.StandardError.ReadToEnd();
                    if (p.ExitCode == 0) return true;
                    message = output.Trim();
                    if (message.Length > 400) message = message.Substring(0, 400);
                    return false;
                }
            }
            catch (Exception ex) { message = ex.Message; return false; }
        }
    }
}

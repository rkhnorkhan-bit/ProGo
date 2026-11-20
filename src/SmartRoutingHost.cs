using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Web.Script.Serialization;

namespace ProGo
{
    // Standalone elevated engine guard. This is installed into protected
    // Program Files and is NOT the normal ProGo GUI. It never opens a network
    // listener or accepts arbitrary shell/command input.
    internal static class SmartRoutingHost
    {
        private const string EngineSha256 =
            "8f30682ee198855440d2a7fe070d5d3653793b2d01bb8c85eea88f93bbe7c8b9";
        private const uint KillOnJobClose = 0x2000;
        private const int ExtendedLimitInfo = 9;

        [StructLayout(LayoutKind.Sequential)]
        private struct IoCounters
        {
            public ulong ReadOperationCount, WriteOperationCount, OtherOperationCount;
            public ulong ReadTransferCount, WriteTransferCount, OtherTransferCount;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct BasicLimit
        {
            public long PerProcessUserTimeLimit, PerJobUserTimeLimit;
            public uint LimitFlags;
            public UIntPtr MinimumWorkingSetSize, MaximumWorkingSetSize;
            public uint ActiveProcessLimit;
            public UIntPtr Affinity;
            public uint PriorityClass, SchedulingClass;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct ExtendedLimit
        {
            public BasicLimit BasicLimitInformation;
            public IoCounters IoInfo;
            public UIntPtr ProcessMemoryLimit, JobMemoryLimit, PeakProcessMemoryUsed, PeakJobMemoryUsed;
        }
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateJobObject(IntPtr attributes, string name);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetInformationJobObject(IntPtr job, int type,
            IntPtr buffer, uint length);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr handle);

        private static readonly object LogGate = new object();

        private static int Main(string[] args)
        {
            if (args != null && args.Length == 2 && args[0] == "--validate-config")
                return SmartRoutingPolicyValidator.Validate(args[1]) ? 0 : 25;
            if (args != null && args.Length == 1 && args[0] == "--self-check")
            {
                // Side-effect free: creates no scheduled tasks, network adapters
                // or background processes on the Windows CI worker.
                var valid = new Dictionary<string, object> {
                    { "SmartRoutingEnabled", true }, { "AutoSystemProxy", false }
                };
                if (!Enabled(valid)) return 20;
                valid["AutoSystemProxy"] = true;
                if (Enabled(valid)) return 21;
                valid["SmartRoutingEnabled"] = false;
                if (Enabled(valid)) return 22;
                if (Marshal.SizeOf(typeof(ExtendedLimit)) < 128) return 23;
                var job = CreateIsolatedJob();
                if (job == IntPtr.Zero) return 24;
                CloseHandle(job);
                return 0;
            }
            if (args != null && args.Length != 0) return 10;
            try { return Run(); }
            catch (Exception ex)
            {
                Log("Guard failure: " + ex.GetType().Name + " (" + ex.HResult.ToString("X") + ")");
                return 1;
            }
        }

        private static string UserRoot
        {
            get { return Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData), "ProGo"); }
        }
        private static string ConfigPath
        {
            get { return Path.Combine(UserRoot, "smart-routing", "active.json"); }
        }
        private static void ReportStatus(string state)
        {
            try
            {
                var path = Path.Combine(UserRoot, "smart-routing", "guard-status.txt");
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, state + "|" +
                    DateTime.UtcNow.Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            catch (Exception) { }
        }
        private static string EnginePath
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sing-box.exe"); }
        }

        private static IDictionary<string, object> ReadSettings()
        {
            var path = Path.Combine(UserRoot, "settings.json");
            var json = File.ReadAllText(path);
            return new JavaScriptSerializer().DeserializeObject(json)
                as IDictionary<string, object>;
        }

        private static bool Enabled(IDictionary<string, object> settings)
        {
            if (settings == null) return false;
            object enabled, legacy;
            if (!settings.TryGetValue("SmartRoutingEnabled", out enabled) ||
                !(enabled is bool) || !(bool)enabled) return false;
            return !settings.TryGetValue("AutoSystemProxy", out legacy) ||
                (!(legacy is bool) || !(bool)legacy);
        }

        private static bool CheckSocks(IDictionary<string, object> settings)
        {
            object hostObj, portObj;
            if (!settings.TryGetValue("SocksHost", out hostObj) ||
                !settings.TryGetValue("SocksPort", out portObj)) return false;
            var host = Convert.ToString(hostObj);
            int port;
            if (!Int32.TryParse(Convert.ToString(portObj), out port) || port < 1 || port > 65535)
                return false;
            IPAddress ip;
            if (!IPAddress.TryParse(host, out ip) || !IPAddress.IsLoopback(ip))
                return false;
            try
            {
                using (var socket = new TcpClient(ip.AddressFamily))
                {
                    var result = socket.BeginConnect(ip, port, null, null);
                    using (result.AsyncWaitHandle)
                    {
                        if (!result.AsyncWaitHandle.WaitOne(1250)) return false;
                        socket.EndConnect(result);
                    }
                    socket.ReceiveTimeout = 1200;
                    socket.SendTimeout = 1200;
                    using (var stream = socket.GetStream())
                    {
                        stream.Write(new byte[] { 5, 1, 0 }, 0, 3);
                        var version = stream.ReadByte();
                        var method = stream.ReadByte();
                        return version == 5 && method == 0;
                    }
                }
            }
            catch (Exception) { return false; }
        }

        private static bool VerifyEngine()
        {
            if (!File.Exists(EnginePath)) return false;
            using (var file = File.OpenRead(EnginePath))
            using (var sha = SHA256.Create())
            {
                var digest = BitConverter.ToString(sha.ComputeHash(file))
                    .Replace("-", "").ToLowerInvariant();
                return String.Equals(digest, EngineSha256, StringComparison.Ordinal);
            }
        }

        private static bool CheckConfig()
        {
            if (!File.Exists(ConfigPath)) return false;
            var start = new ProcessStartInfo(EnginePath, "check -c \"" + ConfigPath + "\"")
            {
                UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true
            };
            using (var process = Process.Start(start))
            {
                if (process == null) return false;
                if (!process.WaitForExit(14000))
                {
                    try { process.Kill(); } catch (Exception) { }
                    return false;
                }
                return process.ExitCode == 0;
            }
        }

        private static IntPtr CreateIsolatedJob()
        {
            var job = CreateJobObject(IntPtr.Zero, null);
            if (job == IntPtr.Zero) throw new InvalidOperationException("Could not create job");
            var limits = new ExtendedLimit();
            limits.BasicLimitInformation.LimitFlags = KillOnJobClose;
            int size = Marshal.SizeOf(typeof(ExtendedLimit));
            IntPtr buffer = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(limits, buffer, false);
                if (!SetInformationJobObject(job, ExtendedLimitInfo, buffer, (uint)size))
                    throw new InvalidOperationException("Could not enable kill-on-close");
                return job;
            }
            catch { CloseHandle(job); throw; }
            finally { Marshal.FreeHGlobal(buffer); }
        }

        private static int Run()
        {
            IDictionary<string, object> settings;
            try { settings = ReadSettings(); }
            catch (Exception) { return 11; }
            if (!Enabled(settings)) return 0;
            if (!VerifyEngine()) { Log("Refused to start: official engine hash mismatch."); return 12; }

            // Wait for SSH as long as Smart Routing is enabled; a temporary
            // outage must not exhaust Task Scheduler retries.
            while (true)
            {
                try { settings = ReadSettings(); } catch (Exception) { return 13; }
                if (!Enabled(settings))
                {
                    ReportStatus("STOPPED");
                    return 0;
                }
                if (CheckSocks(settings)) break;
                ReportStatus("WAITING");
                Thread.Sleep(1800);
            }
            ReportStatus("STARTING");
            if (!SmartRoutingPolicyValidator.Validate(ConfigPath))
            {
                Log("Routing config rejected by privileged allowlist.");
                return 15;
            }
            if (!CheckConfig()) { Log("Routing configuration rejected by sing-box."); return 15; }

            IntPtr job = CreateIsolatedJob();
            try
            {
                var start = new ProcessStartInfo(EnginePath, "run -c \"" + ConfigPath + "\"")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = Path.GetDirectoryName(ConfigPath)
                };
                using (var child = Process.Start(start))
                {
                    if (child == null) return 16;
                    if (!AssignProcessToJobObject(job, child.Handle))
                    {
                        try { child.Kill(); } catch (Exception) { }
                        Log("Could not assign engine to kill-on-close job.");
                        return 17;
                    }
                    child.OutputDataReceived += delegate(object sender, DataReceivedEventArgs args)
                    {
                        if (args.Data != null) Log("engine: " + args.Data);
                    };
                    child.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs args)
                    {
                        if (args.Data != null) Log("engine: " + args.Data);
                    };
                    child.BeginOutputReadLine();
                    child.BeginErrorReadLine();
                    while (!child.WaitForExit(1750))
                    {
                        ReportStatus("RUNNING");
                        try { settings = ReadSettings(); }
                        catch (Exception) { break; }
                        if (!Enabled(settings) ||
                            !SmartRoutingPolicyValidator.Validate(ConfigPath))
                            break;
                    }
                    if (!child.HasExited)
                    {
                        try { child.Kill(); } catch (Exception) { }
                        child.WaitForExit(4000);
                    }
                    ReportStatus("STOPPED");
                    Log("TUN engine stopped, exit=" + child.ExitCode);
                    return child.ExitCode == 0 ? 0 : 18;
                }
            }
            finally { CloseHandle(job); }
        }

        private static void Log(string message)
        {
            // Warn/error level only; never log credentials, keys or the full config.
            try
            {
                var path = Path.Combine(UserRoot, "smart-routing", "guard.log");
                lock (LogGate)
                {
                    if (File.Exists(path) && new FileInfo(path).Length > 1048576)
                        File.Delete(path);
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    File.AppendAllText(path, DateTime.UtcNow.ToString("o") + " " +
                        (message.Length > 600 ? message.Substring(0, 600) : message) + Environment.NewLine);
                }
            }
            catch (Exception) { }
        }
    }
}

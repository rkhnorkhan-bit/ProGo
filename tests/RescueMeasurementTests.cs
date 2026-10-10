using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;

[assembly: AssemblyFileVersion("0.2.2.0")]
[assembly: AssemblyInformationalVersion("0.2.2+measurement-fixture")]
namespace ProGo
{
    internal static class RescueMeasurementTests
    {
        private static int passed;
        private static void Check(bool value, string name)
        { if (!value) throw new Exception(name); passed++; Console.WriteLine("PASS: " + name); }
        private sealed class FakeProbe : IRescueMeasurementProbe
        {
            internal readonly ManualResetEvent Release = new ManualResetEvent(false), Finished = new ManualResetEvent(false);
            internal bool BlockInventory, BlockSample, Throw;
            internal int Inventories, Samples;
            public RescueInventory Inventory()
            { Interlocked.Increment(ref Inventories); if (BlockInventory) { Release.WaitOne(); Finished.Set(); }
                return new RescueInventory { ProcessSelected = true, ProcessAvailable = true }; }
            public RescueSample Sample()
            { int count = Interlocked.Increment(ref Samples); if (BlockSample) { Release.WaitOne(); Finished.Set(); }
                if (Throw) throw new InvalidOperationException("private-error-fixture");
                return new RescueSample { CpuMilliseconds = count * 10, WorkingSetBytes = 1048576, PrivateBytes = 2097152,
                    ReadBytes = (ulong)count * 1024, WriteBytes = (ulong)count * 2048, IoAvailable = true, Responding = true, SshChildren = 2 }; }
            internal void Close()
            { Release.Set(); if (BlockInventory || BlockSample) Finished.WaitOne(1000); Release.Dispose(); Finished.Dispose(); }
        }
        private static void Synthetic()
        {
            var result = new RescueMeasurementResult { ProcessorCount = 1, SocksPort = 1080, HttpPort = 1881, DurationSeconds = 2, StopReason = "completed",
                Inventory = new RescueInventory { ProcessSelected = true, ProcessAvailable = true, ExecutableExists = true,
                    ExecutablePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "ProGo", "ProGo.exe"),
                    FileVersion = "0.2.2.0", BuildIdentity = "0.2.2+reviewed", SshPath = "ssh.exe", SshFileVersion = "0.2.2.0",
                    AgentState = "Stopped", AgentStartMode = "Disabled", SocksListening = true, HttpListening = false } };
            result.Samples.Add(new RescueSample { ElapsedSeconds = 0, CpuMilliseconds = 0, WorkingSetBytes = 1048576, PrivateBytes = 2097152,
                ReadBytes = 1024, WriteBytes = 0, OtherBytes = 0, IoAvailable = true, Responding = true, SshChildren = 1 });
            result.Samples.Add(new RescueSample { ElapsedSeconds = 2, CpuMilliseconds = 1000, WorkingSetBytes = 2097152, PrivateBytes = 3145728,
                ReadBytes = 3072, WriteBytes = 4096, OtherBytes = 1024, IoAvailable = true, Responding = false, SshChildren = 3 });
            string text = RescueMeasurement.Report(result);
            Check(text.Contains("CPU 50.00%") && text.Contains("working set 2.00") && text.Contains("private bytes 3.00"), "mock counters produce normalized CPU and peak memory");
            Check(text.Contains("чтение 1.00") && text.Contains("запись 2.00") && text.Contains("прочее 0.50"), "mock cumulative I/O uses measured time deltas");
            Check(text.Contains("FAIL — Windows Responding") && text.Contains("процессов ssh.exe: 3"), "mock hung window and peak SSH count remain visible");
            Check(text.Contains("Stopped; режим запуска: Disabled") && text.Contains("PASS — SOCKS TCP") && text.Contains("FAIL — HTTP bridge TCP"), "SCM disabled mode and independent actual ports are not merged into connectivity success");
            Check(text.Contains("%USERPROFILE%") && !text.Contains(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)), "current Windows profile prefix is masked");
            result.Inventory.BuildIdentity = "private-version-fixture\nsecret"; result.Inventory.AgentState = "private-state-fixture";
            result.Inventory.AgentStartMode = "private-mode-fixture";
            text = RescueMeasurement.Report(result);
            Check(!text.Contains("private-version-fixture") && !text.Contains("private-state-fixture") && !text.Contains("private-mode-fixture"), "untrusted metadata and state cannot inject report text");
            result.Samples[1].ReadBytes = 0;
            Check(RescueMeasurement.Report(result).Contains("NOT_CHECKED — сопоставимые счётчики"), "counter reset cannot produce underflow or fake I/O rate");
        }
        private static void Bounds()
        {
            var fake = new FakeProbe();
            try
            {
                bool denied = false;
                try { RescueMeasurement.Collect(fake, 0, 100, 0, 0, CancellationToken.None); } catch (ArgumentOutOfRangeException) { denied = true; }
                Check(denied && fake.Inventories == 0, "invalid duration is rejected before reading");
                denied = false;
                try { RescueMeasurement.Collect(fake, 500, 100, 65536, 0, CancellationToken.None); } catch (ArgumentOutOfRangeException) { denied = true; }
                Check(denied && fake.Inventories == 0, "invalid port is rejected before reading");
                using (var cancel = new CancellationTokenSource())
                { cancel.Cancel(); var cancelled = RescueMeasurement.Collect(fake, 500, 100, 0, 0, cancel.Token);
                    Check(cancelled.StopReason == "cancelled" && fake.Inventories == 0, "pre-cancelled measurement launches no worker"); }
                var sample = RescueMeasurement.Collect(fake, 350, 100, 0, 0, CancellationToken.None);
                Check(sample.StopReason == "completed" && sample.Samples.Count >= 2 && sample.Samples.Count <= 4, "bounded mock sampling never polls faster than requested interval");
            }
            finally { fake.Close(); }
            foreach (bool inventory in new[] { true, false })
            {
                fake = new FakeProbe { BlockInventory = inventory, BlockSample = !inventory };
                try
                {
                    var clock = Stopwatch.StartNew(); var result = RescueMeasurement.Collect(fake, 300, 100, 0, 0, CancellationToken.None);
                    Check(clock.ElapsedMilliseconds < 1500 && result.StopReason == (inventory ? "inventory-timeout" : "sample-timeout")
                        && fake.Inventories == 1 && fake.Samples == (inventory ? 0 : 1), "blocked " + (inventory ? "inventory" : "sample") + " ends session without repeated workers");
                }
                finally { fake.Close(); }
            }
            fake = new FakeProbe { Throw = true };
            try { var failed = RescueMeasurement.Collect(fake, 500, 100, 0, 0, CancellationToken.None);
                Check(failed.StopReason == "probe-failed" && !RescueMeasurement.Report(failed).Contains("private-error-fixture"), "probe failure does not export exception text or retry"); }
            finally { fake.Close(); }
            fake = new FakeProbe { BlockSample = true };
            try
            { using (var cancel = new CancellationTokenSource()) { cancel.CancelAfter(100); var clock = Stopwatch.StartNew();
                var result = RescueMeasurement.Collect(fake, 5000, 100, 0, 0, cancel.Token);
                Check(result.StopReason == "cancelled" && clock.ElapsedMilliseconds < 1500 && fake.Samples == 1, "cancellation settles a blocked probe without waiting for its completion"); } }
            finally { fake.Close(); }
        }
        private static void Native()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            string executable = Process.GetCurrentProcess().MainModule.FileName;
            try
            {
                using (var probe = new WindowsRescueMeasurementProbe(Process.GetCurrentProcess().Id, executable, port, 0))
                {
                    var before = probe.Inventory(); var result = RescueMeasurement.Collect(probe, 1500, 250, port, 0, CancellationToken.None);
                    var after = probe.Inventory(); string report = RescueMeasurement.Report(result);
                    Check(result.StopReason == "completed" && result.Samples.Count >= 2 && result.Samples[0].WorkingSetBytes > 0
                        && result.Samples[0].PrivateBytes > 0 && result.Samples[0].IoAvailable, "real current-process sampling reads Win32 CPU/RAM/I/O");
                    Check(before.ProcessAvailable && before.ExpectedPathMatches && Path.GetFullPath(before.ExecutablePath) == Path.GetFullPath(executable)
                        && before.FileVersion == "0.2.2.0" && before.BuildIdentity == "0.2.2+measurement-fixture", "running image identity comes from actual executable resources");
                    Check(before.SocksListening == true && !listener.Pending() && after.SocksListening == true,
                        "listener inventory performs no connection and leaves the listener active");
                    Check(before.AgentState == after.AgentState && before.AgentStartMode == after.AgentStartMode,
                        "SCM status/start mode is unchanged after sampling");
                    Check(result.Samples[0].SshChildren >= 0 && report.Contains("NOT_CHECKED — реальные задержки")
                        && report.Contains("NOT_CHECKED — ключи/аутентификация") && report.Contains("фактический порт не указан"),
                        "native SSH count and unselected port preserve limits of observation");
                }
                using (var mismatch = new WindowsRescueMeasurementProbe(Process.GetCurrentProcess().Id, executable + ".different", 0, 0))
                { var inventory = mismatch.Inventory(); Check(!inventory.ExpectedPathMatches && inventory.ExecutablePath == executable,
                    "PID/path mismatch retains actual running executable identity"); }
                using (var file = new WindowsRescueMeasurementProbe(0, executable, 0, 0))
                { var result = RescueMeasurement.Collect(file, 500, 100, 0, 0, CancellationToken.None);
                    Check(result.StopReason == "no-process" && result.Samples.Count == 0 && result.Inventory.ExecutableExists,
                        "file-only selection never implies that application is running"); }
                string temporary = Path.Combine(Path.GetTempPath(), "ProGo-measurement-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(temporary);
                try
                {
                    string childFile = Path.Combine(temporary, "ssh.exe"); File.Copy(executable, childFile);
                    using (var child = Process.Start(new ProcessStartInfo(childFile, "--child private-command-fixture") { UseShellExecute = false, CreateNoWindow = true }))
                    {
                        try
                        {
                            using (var own = new WindowsRescueMeasurementProbe(Process.GetCurrentProcess().Id, executable, 0, 0))
                            { var sample = own.Sample(); Check(sample.SshChildren >= 1, "Toolhelp counts owned SSH-named children without command-line reads"); }
                            using (var probe = new WindowsRescueMeasurementProbe(child.Id, childFile, 0, 0))
                            { var result = RescueMeasurement.Collect(probe, 2500, 100, 0, 0, CancellationToken.None);
                                Check(result.StopReason == "exited" && result.DurationSeconds < 2.4 && !RescueMeasurement.Report(result).Contains("private-command-fixture"),
                                    "real child exit ends measurement and command-line fixture is absent"); }
                        }
                        finally { if (!child.WaitForExit(1000)) { child.Kill(); child.WaitForExit(); } }
                    }
                }
                finally { Directory.Delete(temporary, true); }
            }
            finally { listener.Stop(); }
        }
        private static int Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--child") { Thread.Sleep(900); return 0; }
            try { Synthetic(); Bounds(); Native(); Console.WriteLine("Rescue measurement tests PASS: " + passed); return 0; }
            catch (Exception error) { Console.Error.WriteLine(error); return 1; }
        }
    }
}

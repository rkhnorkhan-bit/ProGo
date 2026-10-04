using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        // The harness doubles as a disposable local ssh -G/Match exec fixture.
        private static int DiagnosticFixture(string[] args)
        {
            string marker = Environment.GetEnvironmentVariable("PROGO_DIAGNOSTIC_MARKER");
            if (args[0] == "--diagnostic-child") {
                File.WriteAllText(Path.Combine(marker, "child.pid"), Process.GetCurrentProcess().Id.ToString());
                Thread.Sleep(Timeout.Infinite); return 0;
            }
            string mode = args[args.Length - 1];
            if (mode == "fixture-large") {
                string block = new string('x', 8192);
                for (int i = 0; i < 40; i++) { Console.Out.Write(block); Console.Error.Write(block); }
                return 0;
            }
            if (mode == "fixture-tree" || mode == "fixture-exit-tree") {
                File.WriteAllText(Path.Combine(marker, "root.pid"), Process.GetCurrentProcess().Id.ToString());
                using (var child = Process.Start(new ProcessStartInfo(Application.ExecutablePath, "--diagnostic-child") { UseShellExecute = false, CreateNoWindow = true })) {
                    var watch = Stopwatch.StartNew();
                    while (!File.Exists(Path.Combine(marker, "child.pid"))) { if (watch.ElapsedMilliseconds > 3000) return 2; Thread.Sleep(10); }
                    if (mode == "fixture-tree") Thread.Sleep(Timeout.Infinite);
                }
            }
            Console.WriteLine("hostname fixture.example.org\nuser fixture\nport 2222\nidentityfile C:/fixture/key");
            return 0;
        }
        private static bool DiagnosticGone(string path)
        {
            if (!File.Exists(path)) return false;
            try { using (var p = Process.GetProcessById(Int32.Parse(File.ReadAllText(path)))) return p.HasExited; }
            catch (ArgumentException) { return true; }
        }
        private static void DiagnosticMarkers(string marker)
        { foreach (string p in Directory.GetFiles(marker, "*.pid")) File.Delete(p); }
        private static SshProfileDiagnosticResult FixtureCheck(SshProfileSetting profile, CancellationToken token)
        { return SshProfileDiagnostics.Check(profile, token, Application.ExecutablePath, 1500); }
        private static void SshDiagnostics()
        {
            string marker = Path.Combine(work, "ssh-diagnostic"); Directory.CreateDirectory(marker);
            string previous = Environment.GetEnvironmentVariable("PROGO_DIAGNOSTIC_MARKER");
            Environment.SetEnvironmentVariable("PROGO_DIAGNOSTIC_MARKER", marker);
            Process unrelated = null;
            try {
                using (var cts = new CancellationTokenSource()) {
                    cts.Cancel(); bool cancelled = false;
                    try { DiagnosticProcess.Run(Application.ExecutablePath, "-G fixture-tree", 1500, cts.Token); } catch (OperationCanceledException) { cancelled = true; }
                    Check(cancelled && !File.Exists(Path.Combine(marker, "root.pid")), "SSH pre-cancel starts no process");
                }
                bool missing = false;
                try { DiagnosticProcess.Run(Path.Combine(marker, "missing.exe"), "", 1500, CancellationToken.None); } catch (System.ComponentModel.Win32Exception) { missing = true; }
                Check(missing, "SSH missing executable fails without a ready result");
                var captured = DiagnosticProcess.Run(Application.ExecutablePath, "-G fixture-large", 3000, CancellationToken.None);
                Check(captured.ExitCode == 0 && captured.Truncated && captured.Output.Length == DiagnosticProcess.CaptureLimit && captured.Error.Length == DiagnosticProcess.CaptureLimit, "SSH drains oversized stdout and stderr together with bounded capture");
                var oversized = FixtureCheck(new SshProfileSetting { Target = "fixture-large" }, CancellationToken.None);
                Check(!oversized.SshResolved && oversized.Error.Contains("размер"), "SSH truncated output never reports success");
                unrelated = Process.Start(new ProcessStartInfo(Application.ExecutablePath, "--diagnostic-child") { UseShellExecute = false, CreateNoWindow = true });
                PumpUntil(() => File.Exists(Path.Combine(marker, "child.pid"))); DiagnosticMarkers(marker);
                var watch = Stopwatch.StartNew();
                var timeout = FixtureCheck(new SshProfileSetting { Target = "fixture-tree" }, CancellationToken.None);
                Check(!timeout.SshResolved && timeout.Error.Contains("лимит") && watch.ElapsedMilliseconds < 4200, "SSH hung process returns a bounded timeout");
                Check(DiagnosticGone(Path.Combine(marker, "root.pid")) && DiagnosticGone(Path.Combine(marker, "child.pid")), "SSH timeout closes its root and descendant");
                Check(!unrelated.HasExited, "SSH cleanup preserves an unrelated owned fixture");
                DiagnosticMarkers(marker);
                var success = FixtureCheck(new SshProfileSetting { Target = "fixture-exit-tree" }, CancellationToken.None);
                Check(success.SshResolved && success.ResolvedHostName == "fixture.example.org" && success.ResolvedPort == "2222", "SSH parses output after normal root exit");
                Check(DiagnosticGone(Path.Combine(marker, "root.pid")) && DiagnosticGone(Path.Combine(marker, "child.pid")), "SSH normal exit also closes pipe-holding descendants");
                DiagnosticMarkers(marker);
                using (var cts = new CancellationTokenSource()) {
                    var task = Task.Run(() => DiagnosticProcess.Run(Application.ExecutablePath, "-G fixture-tree", 4000, cts.Token));
                    PumpUntil(() => File.Exists(Path.Combine(marker, "child.pid"))); watch.Restart(); cts.Cancel();
                    PumpUntil(() => task.IsCompleted);
                    bool cancelled = false;
                    try { task.GetAwaiter().GetResult(); } catch (OperationCanceledException) { cancelled = true; }
                    Check(cancelled && watch.ElapsedMilliseconds < 2500, "SSH cancellation interrupts an active process");
                    Check(DiagnosticGone(Path.Combine(marker, "root.pid")) && DiagnosticGone(Path.Combine(marker, "child.pid")), "SSH cancellation settles its entire diagnostic tree");
                }
                DiagnosticMarkers(marker);
                var selected = new SshProfileSetting { Target = "fixture-tree" }; int calls = 0, ticks = 0;
                using (var form = new SshDiagnosticForm(selected, (p, token) => { Interlocked.Increment(ref calls); return FixtureCheck(p, token); }))
                using (var timer = new System.Windows.Forms.Timer { Interval = 20 }) {
                    timer.Tick += delegate { ticks++; }; timer.Start(); watch.Restart(); form.Show();
                    Check(watch.ElapsedMilliseconds < 500, "SSH diagnostic opens without waiting for its process");
                    selected.Target = "fixture-ok";
                    PumpUntil(() => File.Exists(Path.Combine(marker, "child.pid")) && ticks >= 3);
                    var retry = (Button)Field(form, "retry"); var cancel = (Button)Field(form, "cancel");
                    Check(!retry.Enabled && calls == 1 && !form.Work.IsCompleted, "SSH busy guard and UI heartbeat remain active on a stalled profile snapshot");
                    retry.PerformClick(); Check(calls == 1, "SSH repeat cannot start during a running check");
                    form.Refresh(); Shot(form, "ssh-diagnostic-pending");
                    cancel.PerformClick(); PumpUntil(() => form.Work.IsCompleted);
                    Check(((Label)Field(form, "status")).Text.Contains("отменена") && retry.Enabled && cancel.Text == "Закрыть", "SSH cancel restores retry and close controls");
                    Check(DiagnosticGone(Path.Combine(marker, "root.pid")) && DiagnosticGone(Path.Combine(marker, "child.pid")), "SSH UI cancel closes owned processes");
                    form.Refresh(); Shot(form, "ssh-diagnostic-cancelled"); DiagnosticMarkers(marker);
                    retry.PerformClick(); PumpUntil(() => calls == 2 && File.Exists(Path.Combine(marker, "child.pid")));
                    var running = form.Work; form.Close(); PumpUntil(() => running.IsCompleted);
                    Check(form.IsDisposed && DiagnosticGone(Path.Combine(marker, "child.pid")), "SSH closing the window cancels retry without late UI access");
                }
                using (var form = new SshDiagnosticForm(new SshProfileSetting { Target = "fixture-ok" }, FixtureCheck)) {
                    form.Show(); PumpUntil(() => form.Work != null && form.Work.IsCompleted);
                    Check(((TextBox)Field(form, "report")).Text.Contains("fixture.example.org") && ((Label)Field(form, "status")).Text.Contains("прочитаны"), "SSH successful diagnostic displays local resolution without claiming server reachability");
                    form.Refresh(); Shot(form, "ssh-diagnostic-success"); form.Close();
                }
                Check(!unrelated.HasExited, "SSH UI cleanup leaves the unrelated process running");
            }
            finally {
                if (unrelated != null) { if (!unrelated.HasExited) { unrelated.Kill(); unrelated.WaitForExit(2000); } unrelated.Dispose(); }
                Environment.SetEnvironmentVariable("PROGO_DIAGNOSTIC_MARKER", previous);
            }
        }
    }
}

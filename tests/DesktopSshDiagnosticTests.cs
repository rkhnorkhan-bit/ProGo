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
            if (mode == "fixture-agent-keys") { Console.WriteLine("256 SHA256:fixture PRIVATE_COMMENT_ONE (ED25519)\n256 SHA256:fixture PRIVATE_COMMENT_TWO (ED25519)"); return 0; }
            if (mode == "fixture-agent-empty") { Console.Error.WriteLine("PRIVATE_EMPTY_ERROR"); return 1; }
            if (mode == "fixture-agent-unavailable") { Console.Error.WriteLine("PRIVATE_CONNECTION_ERROR"); return 2; }
            if (mode == "fixture-identities") {
                Console.WriteLine("hostname fixture.example.org\nidentityfile C:/fixture/old-key\nidentityfile ~/.ssh/second-key\nidentityagent none\nidentitiesonly yes"); return 0;
            }
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
            string windows = Path.Combine(work, "synthetic-windows"), git = Path.Combine(work, "synthetic-git");
            string nativeSsh = Path.Combine(windows, "System32", "OpenSSH", "ssh.exe"), gitSsh = Path.Combine(git, "ssh.exe");
            Check(OpenSshClient.Select(windows, false, git, p => p == nativeSsh || p == gitSsh) == nativeSsh,
                "Windows OpenSSH wins over another client's PATH entry so service-agent guidance matches the client");
            string redirectedSsh = Path.Combine(windows, "Sysnative", "OpenSSH", "ssh.exe");
            Check(OpenSshClient.Select(windows, true, git, p => p == redirectedSsh || p == gitSsh) == redirectedSsh,
                "32-bit process resolves the native Windows OpenSSH directory on a 64-bit OS");
            Check(OpenSshClient.Select(windows, false, "relative" + Path.PathSeparator + git, p => p == gitSsh) == gitSsh,
                "missing optional Windows client keeps an explicit absolute PATH fallback and ignores relative entries");
            Check(OpenSshClient.Select(windows, false, "", p => false) == "ssh.exe" && OpenSshClient.AgentExecutable("ssh.exe") == null,
                "missing OpenSSH cannot claim an unrelated PATH agent utility belongs to its installation");
            using (var proxy = new ProxyService(() => new AppSettings(), delegate { }, "unused.exe", () => DateTime.UtcNow, false))
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim()) {
                var gate = typeof(ProxyService).GetField("gate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(proxy);
                var worker = Task.Run(delegate { lock (gate) { entered.Set(); release.Wait(3000); } });
                entered.Wait();
                try {
                    var elapsed = Stopwatch.StartNew(); var state = proxy.RecoveryStatus;
                    Check(elapsed.ElapsedMilliseconds < 200 && state.Contains("Проверяем"), "UI recovery status never waits behind a stalled background connection lock");
                } finally { release.Set(); worker.GetAwaiter().GetResult(); }
            }
            string marker = Path.Combine(work, "ssh-diagnostic"); Directory.CreateDirectory(marker);
            string previous = Environment.GetEnvironmentVariable("PROGO_DIAGNOSTIC_MARKER");
            Environment.SetEnvironmentVariable("PROGO_DIAGNOSTIC_MARKER", marker);
            Process unrelated = null;
            try {
                foreach (var mode in new[] { "fixture-agent-keys", "fixture-agent-empty", "fixture-agent-unavailable" }) {
                    var keys = SshAgentDiagnostics.Keys(Application.ExecutablePath, CancellationToken.None, (exe, args, ms, token) => {
                        Check(args == "-l" && ms == 3000, "agent inventory is a bounded read-only list operation: " + mode);
                        return DiagnosticProcess.Run(exe, "-G " + mode, ms, token);
                    });
                    Check(!keys.Contains("PRIVATE_") && !keys.Contains("SHA256"), "agent inventory omits fingerprints, comments and raw error content: " + mode);
                    Check(mode == "fixture-agent-keys" ? keys.Contains("ключей: 2") && keys.Contains("ещё не подтверждает") :
                        mode == "fixture-agent-empty" ? keys.Contains("Загрузите") : keys.Contains("не получил ответ"), "agent inventory distinguishes reply, no usable keys and unavailable agent: " + mode);
                }
                var multi = FixtureCheck(new SshProfileSetting { Target = "fixture-identities" }, CancellationToken.None);
                Check(multi.IdentityFiles.Count == 2 && multi.ToReport().Contains("second-key") && multi.ToReport().Contains("Использование агента отключено")
                    && multi.ToReport().Contains("IdentitiesOnly: yes") && multi.ExecutablePath == Application.ExecutablePath,
                    "diagnostics retain all resolved identity paths, chosen client and profile agent restrictions");
                Check(SshAgentDiagnostics.Keys(null, CancellationToken.None).Contains("не найден"), "missing sibling ssh-add is not substituted with another installation");
                using (var cancelled = new CancellationTokenSource()) {
                    cancelled.Cancel(); bool rejected = false;
                    try { SshAgentDiagnostics.Keys(Application.ExecutablePath, cancelled.Token); } catch (OperationCanceledException) { rejected = true; }
                    Check(rejected, "cancelled agent inventory launches no process");
                }
                using (var cts = new CancellationTokenSource()) {
                    cts.Cancel(); bool cancelled = false;
                    try { DiagnosticProcess.Run(Application.ExecutablePath, "-G fixture-tree", 1500, cts.Token); } catch (OperationCanceledException) { cancelled = true; }
                    Check(cancelled && !File.Exists(Path.Combine(marker, "root.pid")), "SSH pre-cancel starts no process");
                }
                bool missing = false;
                try { DiagnosticProcess.Run(Path.Combine(marker, "missing.exe"), "", 1500, CancellationToken.None); } catch (System.ComponentModel.Win32Exception) { missing = true; }
                Check(missing, "SSH missing executable fails without a ready result");
                for (int attempt = 0; attempt < 5; attempt++) {
                    var captured = DiagnosticProcess.Run(Application.ExecutablePath, "-G fixture-large", 3000, CancellationToken.None);
                    Check(captured.ExitCode == 0 && captured.Truncated && captured.Output.Length == DiagnosticProcess.CaptureLimit && captured.Error.Length == DiagnosticProcess.CaptureLimit, "SSH drains oversized stdout and stderr together with bounded capture " + attempt);
                }
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
                    var report = (TextBox)Field(form, "report"); var status = (Label)Field(form, "status");
                    Check(report.ReadOnly && report.AccessibilityObject.Name == "Результат проверки настроек SSH" &&
                        report.AccessibilityObject.Description.Contains("Только чтение"), "SSH report has a named read-only keyboard surface");
                    Check(status.AccessibilityObject.Name == "Состояние проверки SSH" && status.AccessibilityObject.Description == status.Text,
                        "SSH accessible status exposes current progress text");
                    Check(cancel.AccessibilityObject.Name == "Отменить проверку" && cancel.AccessibilityObject.Description.Contains("Окно остаётся"),
                        "SSH active cancel explains its scoped effect");
                    KeyboardWalk(form, new Control[] { report, cancel }, "SSH running skips unavailable retry");
                    retry.PerformClick(); Check(calls == 1, "SSH repeat cannot start during a running check");
                    form.Refresh(); Shot(form, "ssh-diagnostic-pending");
                    typeof(Form).GetMethod("ProcessDialogKey", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                        .Invoke(form, new object[] { Keys.Escape });
                    PumpUntil(() => form.Work.IsCompleted);
                    Check(((Label)Field(form, "status")).Text.Contains("отменена") && retry.Enabled && cancel.Text == "Закрыть", "SSH cancel restores retry and close controls");
                    Check(form.Visible && cancel.AccessibilityObject.Name == "Закрыть" &&
                        cancel.AccessibilityObject.Description.Contains("Закрывает результаты") && status.AccessibilityObject.Description == status.Text,
                        "SSH Escape cancels without closing and updates accessible close action and status");
                    KeyboardWalk(form, new Control[] { report, retry, cancel }, "SSH cancelled offers retry before close");
                    Check(DiagnosticGone(Path.Combine(marker, "root.pid")) && DiagnosticGone(Path.Combine(marker, "child.pid")), "SSH UI cancel closes owned processes");
                    form.Refresh(); Shot(form, "ssh-diagnostic-cancelled"); DiagnosticMarkers(marker);
                    retry.PerformClick(); PumpUntil(() => calls == 2 && File.Exists(Path.Combine(marker, "child.pid")));
                    Check(cancel.AccessibilityObject.Name == "Отменить проверку" && !retry.Enabled,
                        "SSH repeated check restores accessible cancel semantics");
                    var running = form.Work; form.Close(); PumpUntil(() => running.IsCompleted);
                    Check(form.IsDisposed && DiagnosticGone(Path.Combine(marker, "child.pid")), "SSH closing the window cancels retry without late UI access");
                }
                using (var form = new SshDiagnosticForm(new SshProfileSetting { Target = "fixture-ok" }, FixtureCheck)) {
                    form.Show(); PumpUntil(() => form.Work != null && form.Work.IsCompleted);
                    Check(((TextBox)Field(form, "report")).Text.Contains("fixture.example.org") && ((Label)Field(form, "status")).Text.Contains("прочитаны"), "SSH successful diagnostic displays local resolution without claiming server reachability");
                    var report = (TextBox)Field(form, "report"); var retry = (Button)Field(form, "retry"); var cancel = (Button)Field(form, "cancel");
                    KeyboardWalk(form, new Control[] { report, retry, cancel }, "SSH completed report");
                    Check(form.AcceptButton == null && cancel.AccessibilityObject.Name == "Закрыть", "SSH results do not implicitly repeat a check on Enter");
                    int instruction = report.Text.IndexOf("Если агент отключён:", StringComparison.Ordinal);
                    Check(instruction >= 0 && report.WordWrap && report.ScrollBars == ScrollBars.Vertical, "SSH agent instructions use vertical reading rather than horizontal scrolling");
                    report.Select(instruction, 0); report.ScrollToCaret();
                    var startLine = report.GetPositionFromCharIndex(instruction);
                    var endLine = report.GetPositionFromCharIndex(report.Text.IndexOf("Без прав администратора", instruction, StringComparison.Ordinal));
                    Check(endLine.Y > startLine.Y, "native report actually wraps long agent instructions onto subsequent lines");
                    form.Refresh(); Shot(form, "ssh-agent-guidance");
                    report.Select(0, 0); report.ScrollToCaret();
                    form.Refresh(); Shot(form, "ssh-diagnostic-success");
                    typeof(Form).GetMethod("ProcessDialogKey", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                        .Invoke(form, new object[] { Keys.Escape });
                    Check(form.IsDisposed, "SSH Escape closes completed results");
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

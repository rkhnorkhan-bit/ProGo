using System;
using System.Diagnostics;
using System.Drawing;
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
            if (mode == "fixture-expanded-identity") {
                Console.WriteLine("hostname " + new string('h', 4096) + "\nuser fixture\nport 2222\nidentityfile C:/fixture/%h%h%h%h%h%h%h%h%h"); return 0;
            }
            if (mode == "fixture-agent-keys") { Console.WriteLine("256 " + AgentFingerprintA + " PRIVATE_COMMENT_ONE (ED25519)\n256 " + AgentFingerprintB + " PRIVATE_COMMENT_TWO (ED25519)"); return 0; }
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
                        Check(args == "-l -E sha256" && ms == 3000, "agent inventory is a bounded read-only list operation: " + mode);
                        return DiagnosticProcess.Run(exe, "-G " + mode, ms, token);
                    });
                    Check(!keys.Contains("PRIVATE_") && !keys.Contains("SHA256"), "agent inventory omits fingerprints, comments and raw error content: " + mode);
                    Check(mode == "fixture-agent-keys" ? keys.Contains("ключей: 2") && keys.Contains("ещё не подтверждает") :
                        mode == "fixture-agent-empty" ? keys.Contains("Команда проверки агента завершилась ошибкой") && keys.Contains("не подтверждён") : keys.Contains("не получил ответ"), "agent inventory distinguishes reply, command failure and unavailable agent: " + mode);
                }
                var multi = FixtureCheck(new SshProfileSetting { Target = "fixture-identities" }, CancellationToken.None);
                Check(multi.IdentityFiles.Count == 2 && multi.ToReport().Contains("second-key") && multi.ToReport().Contains("Использование агента отключено")
                    && multi.ToReport().Contains("IdentitiesOnly: yes") && multi.ExecutablePath == Application.ExecutablePath
                    && multi.AgentInventory.State == SshAgentInventoryState.DisabledByProfile,
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
        private static SshProfileDiagnosticResult KeyUiProfile(string root, params string[] identities)
        {
            var result = KeyMatchProfile(root, identities);
            result.PubkeyAuthentication = "yes"; result.PreferredAuthentications = "publickey,password";
            return result;
        }
        private static void KeyUiReady(SshDiagnosticForm form)
        { form.Show(); PumpUntil(() => form.Work != null && form.Work.IsCompleted); form.Work.GetAwaiter().GetResult(); }
        private static void SshKeyAddUiWorkflow()
        {
            int before = passed; Console.WriteLine("SSH key addition UI checks BEGIN");
            string root = Path.Combine(work, "key-add-ui-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
            bool previous = Control.CheckForIllegalCrossThreadCalls; Control.CheckForIllegalCrossThreadCalls = true;
            try {
                File.WriteAllText(Path.Combine(root, "ssh-add.exe"), "");
                KeyUiPolicies(root); KeyUiPending(root); KeyUiOutcomes(root);
                KeyUiClosed(root, false); KeyUiClosed(root, true); KeyUiQueuedCancellation(root);
            } finally { Control.CheckForIllegalCrossThreadCalls = previous; Directory.Delete(root, true); }
            Console.WriteLine("SSH key addition UI checks PASS: " + (passed - before));
        }
        private static void KeyUiPolicies(string root)
        {
            string[] reasons = { "подтверждённого ключа", "Политика входа", "не использует", "не использует", "IdentityAgent none", "Отдельный агент", "не найден", "путь", "подтверждённого ключа" };
            for (int variant = 0; variant < reasons.Length; variant++) {
                var profile = KeyUiProfile(root, "selected key");
                if (variant == 0) profile.SshResolved = false;
                if (variant == 1) profile.PubkeyAuthentication = null;
                if (variant == 2) profile.PubkeyAuthentication = "no";
                if (variant == 3) profile.PreferredAuthentications = "password";
                if (variant == 4) profile.IdentityAgent = "none";
                if (variant == 5) profile.IdentityAgent = "fixture-custom-endpoint";
                if (variant == 6) profile.ExecutablePath = Path.Combine(root, "missing", "ssh.exe");
                if (variant == 7) profile.IdentityPaths["selected key"] = "relative";
                if (variant == 8) profile.IdentityFiles.Clear();
                int calls = 0;
                using (var form = new SshDiagnosticForm(new SshProfileSetting { Target = "selected.fixture" }, (p, t) => profile,
                    (request, token) => { calls++; return new SshAgentKeyAddResult(SshAgentKeyAddState.CommandCompleted); })) {
                    KeyUiReady(form); var add = (Button)Field(form, "addKey"); add.PerformClick();
                    Check(!add.Enabled && !((ComboBox)Field(form, "identities")).Enabled && calls == 0 &&
                        ((Label)Field(form, "keyHint")).Text.IndexOf(reasons[variant], StringComparison.OrdinalIgnoreCase) >= 0,
                        "key addition UI refuses an ineligible profile with its specific reason: " + variant);
                    if (variant == 5) Shot(form, "ssh-key-add-unavailable");
                    form.Close();
                }
            }
        }
        private static void KeyUiPending(string root)
        {
            using (var entered = new ManualResetEventSlim()) using (var release = new ManualResetEventSlim())
            using (var timer = new System.Windows.Forms.Timer { Interval = 15 }) {
                int ui = Thread.CurrentThread.ManagedThreadId, worker = 0, checks = 0, adds = 0, ticks = 0;
                SshProfileDiagnosticResult diagnostic = null; SshAgentKeyAddRequest selected = null; CancellationToken ownerToken = CancellationToken.None;
                var profile = new SshProfileSetting { Target = "selected.fixture" };
                using (var form = new SshDiagnosticForm(profile, (p, t) => {
                    Interlocked.Increment(ref checks); worker = Thread.CurrentThread.ManagedThreadId;
                    if (p.Target != "selected.fixture") throw new Exception("Profile snapshot changed");
                    return diagnostic = KeyUiProfile(root, "first key", "second key");
                }, (request, token) => SshAgentKeyAdd.Run(request, token, (e, a, ep, ms, t) => {
                    selected = request; ownerToken = t; Interlocked.Increment(ref adds); worker = Thread.CurrentThread.ManagedThreadId;
                    entered.Set(); if (!release.Wait(15000)) throw new TimeoutException("Key UI release deadline"); return 0;
                }))) {
                    profile.Target = "changed.outside"; timer.Tick += delegate { ticks++; }; timer.Start(); KeyUiReady(form);
                    var choices = (ComboBox)Field(form, "identities"); var add = (Button)Field(form, "addKey");
                    var retry = (Button)Field(form, "retry"); var cancel = (Button)Field(form, "cancel"); var report = (TextBox)Field(form, "report");
                    Check(worker != ui && checks == 1 && choices.Items.Count == 2 && choices.SelectedIndex == -1 && choices.Enabled && !add.Enabled,
                        "profile snapshot and key preparation run off UI; multiple identities require an explicit selection");
                    choices.SelectedIndex = 1;
                    Check(add.Enabled && choices.AccessibilityObject.Name.Contains("SSH-профиля") && add.AccessibilityObject.Description.Contains("OpenSSH") &&
                        form.AcceptButton == null && ((Label)Field(form, "keyHint")).AccessibilityObject.Description.Contains("Парольная фраза"),
                        "selected key action explains native secret entry and has no implicit Enter action");
                    KeyboardWalk(form, new Control[] { report, choices, add, (Button)Field(form, "verifyKey"), retry, cancel }, "SSH key addition ready");
                    form.ClientSize = new Size(650, 560); form.PerformLayout();
                    Check(report.Height > 60 && add.Right <= add.Parent.ClientSize.Width && add.Bottom <= add.Parent.ClientSize.Height &&
                        add.ClientSize.Height >= TextRenderer.MeasureText(add.Text, add.Font).Height + add.Padding.Vertical &&
                        choices.Right <= choices.Parent.ClientSize.Width &&
                        ((Label)Field(form, "keyHint")).Height >= TextRenderer.MeasureText(((Label)Field(form, "keyHint")).Text, form.Font,
                            new Size(((Label)Field(form, "keyHint")).Width, Int32.MaxValue), TextFormatFlags.WordBreak).Height,
                        "minimum-size key selection keeps its controls and explanation inside the form");
                    Shot(form, "ssh-key-add-ready-minimum"); form.ClientSize = new Size(790, 660);
                    diagnostic.IdentityPaths["second key"] = Path.Combine(root, "changed key"); diagnostic.ExecutablePath = "changed.exe";
                    var watch = Stopwatch.StartNew(); add.PerformClick(); var work = form.Work;
                    try {
                        PumpUntil(() => entered.IsSet && ticks >= 3);
                        Check(watch.ElapsedMilliseconds < 1500 && worker != ui && !work.IsCompleted && !retry.Enabled && !choices.Enabled && !add.Enabled && cancel.Enabled &&
                            cancel.AccessibilityObject.Name == "Отменить добавление" && cancel.AccessibilityObject.Description.Contains("не удаляется"),
                            "key addition starts on an owned background worker while UI heartbeat and cancel stay active");
                        Check(selected.IdentityPath == Path.Combine(root, "second key") && selected.Executable == Path.Combine(root, "ssh-add.exe"),
                            "the chosen key request remains immutable after the diagnostic object changes");
                        Shot(form, "ssh-key-add-pending"); add.PerformClick(); retry.PerformClick(); cancel.PerformClick();
                        Check(ownerToken.IsCancellationRequested && !cancel.Enabled && !work.IsCompleted && adds == 1 && checks == 1 &&
                            SshAgentServiceProbe.Shared.ReadAsync(1000, CancellationToken.None).GetAwaiter().GetResult().State == SshAgentProbeState.Busy,
                            "cancelled UI addition retains the shared native owner and rejects duplicate add or retry until cleanup");
                        release.Set(); PumpUntil(() => work.IsCompleted); work.GetAwaiter().GetResult();
                        Check(form.Visible && ((Label)Field(form, "status")).Text.Contains("Операция отменена") && report.TextLength == 0 &&
                            choices.Items.Count == 0 && !add.Enabled && retry.Enabled && cancel.Text == "Закрыть" && checks == 1,
                            "late addition success cannot replace cancellation or leave stale key inventory available");
                        Shot(form, "ssh-key-add-cancelled"); retry.PerformClick(); PumpUntil(() => form.Work.IsCompleted);
                        Check(checks == 2 && choices.Items.Count == 2 && choices.SelectedIndex == -1 && !add.Enabled,
                            "a fresh explicit check is required to choose a key again after an attempted mutation");
                    } finally { release.Set(); AgentProbeSettled(work); form.Close(); }
                }
            }
        }
        private static void KeyUiOutcomes(string root)
        {
            foreach (var state in new[] { SshAgentKeyAddState.CommandCompleted, SshAgentKeyAddState.CommandFailed, SshAgentKeyAddState.AgentUnavailable,
                SshAgentKeyAddState.TimedOut, SshAgentKeyAddState.Busy, SshAgentKeyAddState.Failed }) {
                using (var form = new SshDiagnosticForm(new SshProfileSetting { Target = "selected.fixture" }, (p, t) => KeyUiProfile(root, "selected key"),
                    (request, token) => new SshAgentKeyAddResult(state, state == SshAgentKeyAddState.CommandFailed ? (int?)1 : null,
                        state == SshAgentKeyAddState.Failed ? (int?)5 : null))) {
                    KeyUiReady(form); var add = (Button)Field(form, "addKey");
                    bool ready = add.Enabled; add.PerformClick(); PumpUntil(() => form.Work.IsCompleted); form.Work.GetAwaiter().GetResult();
                    string status = ((Label)Field(form, "status")).Text;
                    Check(ready && status.Contains(new SshAgentKeyAddResult(state).ToDisplay()) &&
                        (state != SshAgentKeyAddState.CommandFailed || status.Contains("Код ssh-add: 1")) &&
                        (state != SshAgentKeyAddState.Failed || status.Contains("Код Windows: 5")) &&
                        ((ComboBox)Field(form, "identities")).Items.Count == 0 && !add.Enabled && ((Button)Field(form, "retry")).Enabled,
                        "typed key addition UI outcome invalidates old key evidence without claiming authentication: " + state);
                    if (state == SshAgentKeyAddState.CommandCompleted) Shot(form, "ssh-key-add-completed");
                    if (state == SshAgentKeyAddState.AgentUnavailable) Shot(form, "ssh-key-add-agent-error");
                    form.Close();
                }
            }
        }
        private static void KeyUiClosed(string root, bool dispose)
        {
            using (var entered = new ManualResetEventSlim()) using (var release = new ManualResetEventSlim()) {
                CancellationToken token = CancellationToken.None; int late = 0;
                using (var form = new SshDiagnosticForm(new SshProfileSetting { Target = "selected.fixture" }, (p, t) => KeyUiProfile(root, "selected key"),
                    (request, owner) => SshAgentKeyAdd.Run(request, owner, (e, a, ep, ms, t) => {
                        token = t; entered.Set(); if (!release.Wait(15000)) throw new TimeoutException("Closed key UI release deadline"); return 0;
                    }))) {
                    KeyUiReady(form); ((Button)Field(form, "addKey")).PerformClick();
                    var work = form.Work;
                    try {
                        Check(entered.Wait(3000), "closable key UI reaches its native owner: " + dispose);
                        ((Label)Field(form, "status")).TextChanged += delegate { late++; };
                        var watch = Stopwatch.StartNew(); if (dispose) form.Dispose(); else form.Close();
                        Check(watch.ElapsedMilliseconds < 1000 && token.IsCancellationRequested && !work.IsCompleted &&
                            SshAgentServiceProbe.Shared.ReadAsync(1000, CancellationToken.None).GetAwaiter().GetResult().State == SshAgentProbeState.Busy,
                            "Close or Dispose cancels immediately without freeing held key cleanup or waiting on UI: " + dispose);
                        release.Set(); AgentProbeSettled(work); work.GetAwaiter().GetResult(); Application.DoEvents();
                        using (var lease = SshReadOnlyLease.TryAcquire(CancellationToken.None)) {
                            Check(form.IsDisposed && form.KeyWork.IsCompleted && late == 0 && lease != null,
                                "late key completion settles without a live UI pump, stale publication or leaked lease: " + dispose);
                        }
                    } finally { release.Set(); AgentProbeSettled(work); }
                }
            }
        }
        private static void KeyUiQueuedCancellation(string root)
        {
            using (var form = new SshDiagnosticForm(new SshProfileSetting { Target = "selected.fixture" }, (p, t) => KeyUiProfile(root, "selected key"),
                (request, token) => new SshAgentKeyAddResult(SshAgentKeyAddState.CommandCompleted))) {
                KeyUiReady(form); ((Button)Field(form, "addKey")).PerformClick(); AgentProbeSettled(form.KeyWork);
                bool queued = !form.Work.IsCompleted; ((Button)Field(form, "cancel")).PerformClick(); PumpUntil(() => form.Work.IsCompleted);
                Check(queued && ((Label)Field(form, "status")).Text.Contains("Операция отменена") && !((Button)Field(form, "addKey")).Enabled,
                    "cancel before queued key publication suppresses an already completed successful result"); form.Close();
            }
            using (var entered = new ManualResetEventSlim()) using (var release = new ManualResetEventSlim())
            using (var form = new SshDiagnosticForm(new SshProfileSetting { Target = "selected.fixture" }, (p, t) => {
                entered.Set(); if (!release.Wait(10000)) throw new TimeoutException("Queued key UI diagnostic release deadline");
                return KeyUiProfile(root, "selected key");
            })) {
                try {
                    form.Show(); PumpUntil(() => entered.IsSet);
                    release.Set(); AgentProbeSettled(form.CheckWork); bool queued = !form.Work.IsCompleted;
                    ((Button)Field(form, "cancel")).PerformClick(); PumpUntil(() => form.Work.IsCompleted);
                    Check(queued && ((ComboBox)Field(form, "identities")).Items.Count == 0 && !((Button)Field(form, "addKey")).Enabled &&
                        ((Label)Field(form, "keyHint")).Text.Contains("отменена"), "cancel before queued diagnostics cannot publish eligible key choices"); form.Close();
                } finally { release.Set(); AgentProbeSettled(form.CheckWork); }
            }
        }
        private static SshProfileDiagnosticResult AgentStartUiProfile(SshAgentRuntimeState runtime, SshAgentStartMode mode,
            SshAgentProbeState probeState = SshAgentProbeState.Completed)
        {
            var result = new SshProfileDiagnosticResult { Target = "agent.fixture", SshResolved = true };
            result.AgentProbe = new SshAgentProbeResult(probeState,
                new SshAgentServiceSnapshot { RuntimeState = runtime, StartMode = mode });
            result.AgentService = result.AgentProbe.Snapshot;
            result.AgentState = result.AgentProbe.ToDisplay();
            return result;
        }
        private static SshAgentServiceStartResult AgentStartUiSuccess()
        {
            return new SshAgentServiceStartResult { State = SshAgentStartState.Running,
                Snapshot = new SshAgentServiceSnapshot { RuntimeState = SshAgentRuntimeState.Running, StartMode = SshAgentStartMode.Manual } };
        }
        private static void SshAgentStartUiWorkflow()
        {
            int before = passed; Console.WriteLine("SSH-agent explicit start UI checks BEGIN");
            bool previous = Control.CheckForIllegalCrossThreadCalls; Control.CheckForIllegalCrossThreadCalls = true;
            try {
                AgentStartUiEligibility(); AgentStartUiCompleted(); AgentStartUiCancellation(false);
                AgentStartUiCancellation(true); AgentStartUiClosed(false); AgentStartUiClosed(true);
                AgentStartUiQueuedCancellation();
            } finally { Control.CheckForIllegalCrossThreadCalls = previous; }
            Console.WriteLine("SSH-agent explicit start UI checks PASS: " + (passed - before));
        }
        private static void AgentStartUiEligibility()
        {
            var cases = new[] {
                new { Runtime = SshAgentRuntimeState.Stopped, Mode = SshAgentStartMode.Manual, State = SshAgentProbeState.Completed, Ready = true, Reason = "текущими правами" },
                new { Runtime = SshAgentRuntimeState.Stopped, Mode = SshAgentStartMode.Unknown, State = SshAgentProbeState.Completed, Ready = true, Reason = "текущими правами" },
                new { Runtime = SshAgentRuntimeState.Stopped, Mode = SshAgentStartMode.Disabled, State = SshAgentProbeState.Completed, Ready = false, Reason = "Disabled" },
                new { Runtime = SshAgentRuntimeState.Running, Mode = SshAgentStartMode.Manual, State = SshAgentProbeState.Completed, Ready = false, Reason = "уже запущена" },
                new { Runtime = SshAgentRuntimeState.Missing, Mode = SshAgentStartMode.Unknown, State = SshAgentProbeState.Completed, Ready = false, Reason = "не найдена" },
                new { Runtime = SshAgentRuntimeState.StartPending, Mode = SshAgentStartMode.Manual, State = SshAgentProbeState.Completed, Ready = false, Reason = "Stopped" },
                new { Runtime = SshAgentRuntimeState.Stopped, Mode = SshAgentStartMode.Manual, State = SshAgentProbeState.Busy, Ready = false, Reason = "не подтверждено" }
            };
            foreach (var item in cases) {
                int starts = 0;
                using (var form = new SshDiagnosticForm(new SshProfileSetting { Target = "agent.fixture" },
                    (p, t) => AgentStartUiProfile(item.Runtime, item.Mode, item.State), null,
                    new SshAgentServiceProbe(t => { throw new Exception("Unexpected read"); }, t => { Interlocked.Increment(ref starts); return AgentStartUiSuccess(); }))) {
                    var start = (Button)Field(form, "startAgent");
                    Check(!start.Enabled, "service start is disabled until diagnostics finish: " + item.Runtime + "/" + item.Mode);
                    KeyUiReady(form);
                    Check(start.Enabled == item.Ready && start.AccessibilityObject.Description.Contains(item.Reason),
                        "service start is offered only for confirmed stopped, non-disabled service: " + item.Runtime + "/" + item.Mode + "/" + item.State);
                    if (!item.Ready) { start.PerformClick(); Check(starts == 0, "ineligible start cannot mutate service: " + item.Runtime + "/" + item.Mode); }
                    if (item.Ready && item.Mode == SshAgentStartMode.Manual) {
                        form.ClientSize = new Size(650, 560); form.PerformLayout();
                        var report = (TextBox)Field(form, "report"); var retry = (Button)Field(form, "retry"); var cancel = (Button)Field(form, "cancel");
                        KeyboardWalk(form, new Control[] { report, start, retry, cancel }, "SSH service start ready");
                        Check(start.Right <= start.Parent.ClientSize.Width && start.Bottom <= start.Parent.ClientSize.Height &&
                            start.ClientSize.Height >= TextRenderer.MeasureText(start.Text, start.Font).Height + start.Padding.Vertical &&
                            form.AcceptButton == null, "minimum-size service start action stays visible without implicit Enter start");
                        report.Select(0, 0); start.Focus(); Shot(form, "ssh-agent-start-ready-minimum");
                    }
                    if (item.Mode == SshAgentStartMode.Disabled) Shot(form, "ssh-agent-start-disabled");
                    form.Close();
                }
            }
        }
        private static void AgentStartUiCompleted()
        {
            foreach (var kind in new[] { "running", "denied", "unavailable" }) {
                int starts = 0, reads = 0;
                var probe = new SshAgentServiceProbe(t => { Interlocked.Increment(ref reads); throw new Exception("Unexpected read"); }, t => {
                    Interlocked.Increment(ref starts);
                    if (kind == "unavailable") throw new Exception("PRIVATE_NATIVE_ERROR");
                    if (kind == "denied") return new SshAgentServiceStartResult {
                        State = SshAgentStartState.AccessDenied,
                        Snapshot = new SshAgentServiceSnapshot { RuntimeState = SshAgentRuntimeState.Stopped, StartMode = SshAgentStartMode.Manual },
                        Error = new SshAgentServiceError { Stage = SshAgentServiceStage.OpenStartService, Kind = SshAgentServiceErrorKind.NativeFailure, Win32Code = 5 }
                    };
                    return AgentStartUiSuccess();
                });
                using (var form = new SshDiagnosticForm(new SshProfileSetting { Target = "agent.fixture" },
                    (p, t) => AgentStartUiProfile(SshAgentRuntimeState.Stopped, SshAgentStartMode.Manual), null, probe)) {
                    KeyUiReady(form); var start = (Button)Field(form, "startAgent"); start.PerformClick();
                    PumpUntil(() => form.Work.IsCompleted); form.Work.GetAwaiter().GetResult();
                    var status = ((Label)Field(form, "status")).Text;
                    Check(starts == 1 && reads == 0 && !status.Contains("PRIVATE_") &&
                        status.Contains(kind == "running" ? "работает" : kind == "denied" ? "Недостаточно прав" : "не подтверждено") &&
                        (kind != "denied" || status.Contains("Win32 5")) &&
                        ((TextBox)Field(form, "report")).Text.Contains("Результаты предыдущей проверки сброшены") &&
                        !((TextBox)Field(form, "report")).Text.Contains("ssh-agent: Stopped") && !start.Enabled &&
                        ((Button)Field(form, "retry")).Enabled && ((ComboBox)Field(form, "identities")).Items.Count == 0,
                        "typed service start result clears stale diagnostic evidence without claiming server login: " + kind);
                    start.PerformClick(); Check(starts == 1, "a second start requires an explicit fresh service check: " + kind);
                    if (kind == "running") Shot(form, "ssh-agent-start-completed");
                    if (kind == "denied") Shot(form, "ssh-agent-start-denied");
                    form.Close();
                }
            }
        }
        private static void AgentStartUiCancellation(bool timeout)
        {
            using (var entered = new ManualResetEventSlim()) using (var release = new ManualResetEventSlim())
            using (var timer = new System.Windows.Forms.Timer { Interval = 15 }) {
                int starts = 0, ticks = 0; CancellationToken workerToken = CancellationToken.None;
                var probe = new SshAgentServiceProbe(t => { throw new Exception("Unexpected read"); }, t => {
                    workerToken = t; Interlocked.Increment(ref starts); entered.Set();
                    if (!release.Wait(15000)) throw new TimeoutException("UI start fixture release deadline");
                    return AgentStartUiSuccess();
                });
                using (var form = new SshDiagnosticForm(new SshProfileSetting { Target = "agent.fixture" },
                    (p, t) => AgentStartUiProfile(SshAgentRuntimeState.Stopped, SshAgentStartMode.Manual), null, probe)) {
                    try {
                        timer.Tick += delegate { ticks++; }; timer.Start(); KeyUiReady(form);
                        var start = (Button)Field(form, "startAgent"); var cancel = (Button)Field(form, "cancel");
                        start.PerformClick(); var work = form.Work;
                        PumpUntil(() => entered.IsSet && ticks >= 3);
                        Check(!work.IsCompleted && !start.Enabled && !((Button)Field(form, "retry")).Enabled &&
                            cancel.Enabled && cancel.AccessibilityObject.Description.Contains("не остановит службу"),
                            "service start keeps UI responsive and action guarded while native call is held: " + timeout);
                        if (!timeout) Shot(form, "ssh-agent-start-pending");
                        if (!timeout) cancel.PerformClick();
                        PumpUntil(() => work.IsCompleted);
                        Check(workerToken.IsCancellationRequested && !probe.PendingWork.IsCompleted &&
                            SshAgentServiceProbe.Shared.ReadAsync(1000, CancellationToken.None).GetAwaiter().GetResult().State == SshAgentProbeState.Busy &&
                            ((Label)Field(form, "status")).Text.Contains(timeout ? "истекло" : "отменено") &&
                            !start.Enabled && starts == 1 && cancel.Text == "Закрыть",
                            "timeout/cancel never releases an unfinished native owner or reports agent ready: " + timeout);
                        if (!timeout) Shot(form, "ssh-agent-start-cancelled");
                        release.Set(); AgentProbeSettled(probe.PendingWork);
                        Check(!start.Enabled, "late native completion cannot republish stale start availability: " + timeout);
                    } finally { release.Set(); AgentProbeSettled(probe.PendingWork); form.Close(); }
                }
            }
        }
        private static void AgentStartUiClosed(bool dispose)
        {
            using (var entered = new ManualResetEventSlim()) using (var release = new ManualResetEventSlim()) {
                CancellationToken token = CancellationToken.None; int late = 0;
                var probe = new SshAgentServiceProbe(t => { throw new Exception("Unexpected read"); }, t => {
                    token = t; entered.Set(); if (!release.Wait(15000)) throw new TimeoutException("Closed start UI release deadline");
                    return AgentStartUiSuccess();
                });
                using (var form = new SshDiagnosticForm(new SshProfileSetting { Target = "agent.fixture" },
                    (p, t) => AgentStartUiProfile(SshAgentRuntimeState.Stopped, SshAgentStartMode.Manual), null, probe)) {
                    KeyUiReady(form); ((Button)Field(form, "startAgent")).PerformClick(); var work = form.Work;
                    try {
                        Check(entered.Wait(3000), "service start reaches its owned worker: " + dispose);
                        ((Label)Field(form, "status")).TextChanged += delegate { late++; };
                        var watch = Stopwatch.StartNew(); if (dispose) form.Dispose(); else form.Close();
                        AgentProbeSettled(work);
                        Check(watch.ElapsedMilliseconds < 1000 && token.IsCancellationRequested && !probe.PendingWork.IsCompleted &&
                            late == 0, "Close/Dispose cancels start waiter without blocking UI or publishing a late result: " + dispose);
                        release.Set(); AgentProbeSettled(probe.PendingWork);
                        using (var lease = SshReadOnlyLease.TryAcquire(CancellationToken.None))
                            Check(lease != null && form.IsDisposed && late == 0, "native start cleanup releases shared lease after closed owner: " + dispose);
                    } finally { release.Set(); AgentProbeSettled(probe.PendingWork); }
                }
            }
        }
        private static void AgentStartUiQueuedCancellation()
        {
            var probe = new SshAgentServiceProbe(t => { throw new Exception("Unexpected read"); }, t => AgentStartUiSuccess());
            using (var form = new SshDiagnosticForm(new SshProfileSetting { Target = "agent.fixture" },
                (p, t) => AgentStartUiProfile(SshAgentRuntimeState.Stopped, SshAgentStartMode.Manual), null, probe)) {
                KeyUiReady(form); ((Button)Field(form, "startAgent")).PerformClick(); AgentProbeSettled(form.StartWork);
                bool queued = !form.Work.IsCompleted; ((Button)Field(form, "cancel")).PerformClick(); PumpUntil(() => form.Work.IsCompleted);
                Check(queued && ((Label)Field(form, "status")).Text.Contains("отменено") &&
                    !((Button)Field(form, "startAgent")).Enabled, "cancel before queued service result publication suppresses accepted start as UI success");
                form.Close();
            }
        }
    }
}

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
        private static void SshKeyReadinessUiWorkflow()
        {
            int before = passed; Console.WriteLine("SSH selected-key signing UI checks BEGIN");
            string root = Path.Combine(work, "key-signing-ui-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
            bool previous = Control.CheckForIllegalCrossThreadCalls; Control.CheckForIllegalCrossThreadCalls = true;
            try {
                File.WriteAllText(Path.Combine(root, "ssh-add.exe"), "");
                SigningUiEligibility(root); SigningUiPending(root); SigningUiOutcomes(root);
                SigningUiFreshness(root); SigningUiSelection(root); SigningUiMutations(root);
                SigningUiClosed(root, false); SigningUiClosed(root, true); SigningUiQueuedCancel(root);
            } finally { Control.CheckForIllegalCrossThreadCalls = previous; Directory.Delete(root, true); }
            Console.WriteLine("SSH selected-key signing UI checks PASS: " + (passed - before));
        }
        private static SshKeyReadinessResult SigningUiSuccess()
        { return new SshKeyReadinessResult(SshKeyReadinessState.AgentSigningVerified, 0); }
        private static void SigningUiEligibility(string root)
        {
            for (int variant = 0; variant < 6; variant++) {
                int calls = 0, current = variant;
                using (var form = new SshDiagnosticForm(new SshProfileSetting { Target = "selected.fixture" }, (p, token) => {
                    var value = KeyUiProfile(root, "selected key");
                    if (current == 0) value.SshResolved = false;
                    if (current == 1) value.PubkeyAuthentication = null;
                    if (current == 2) value.PreferredAuthentications = "password";
                    if (current == 3) value.IdentityAgent = "none";
                    if (current == 4) value.IdentityAgent = "custom.fixture";
                    if (current == 5) value.IdentityPaths["selected key"] = "relative";
                    return value;
                }, null, null, (profile, identity, token) => { calls++; return SigningUiSuccess(); })) {
                    KeyUiReady(form); var verify = (Button)Field(form, "verifyKey"); verify.PerformClick();
                    Check(!verify.Enabled && calls == 0 && !String.IsNullOrEmpty(((Label)Field(form, "keyHint")).Text),
                        "signing UI explains an ineligible selection without probing another authentication method: " + variant);
                    form.Close();
                }
            }
        }
        private static void SigningUiPending(string root)
        {
            using (var entered = new ManualResetEventSlim()) using (var release = new ManualResetEventSlim())
            using (var heartbeat = new System.Windows.Forms.Timer { Interval = 15 }) {
                int checks = 0, signatures = 0, ticks = 0, thread = 0, ui = Thread.CurrentThread.ManagedThreadId;
                string selected = null; CancellationToken owner = CancellationToken.None;
                var source = new SshProfileSetting { Target = "selected.fixture" };
                using (var form = new SshDiagnosticForm(source, (p, token) => {
                    if (p.Target != "selected.fixture") throw new Exception("Form profile changed");
                    Interlocked.Increment(ref checks); return KeyUiProfile(root, "first key", "second key");
                }, null, null, (profile, identity, token) => {
                    thread = Thread.CurrentThread.ManagedThreadId; selected = identity; owner = token; Interlocked.Increment(ref signatures);
                    entered.Set(); if (!release.Wait(15000)) throw new TimeoutException("Signing UI release deadline"); return SigningUiSuccess();
                })) {
                    source.Target = "changed.outside"; heartbeat.Tick += delegate { ticks++; }; heartbeat.Start(); KeyUiReady(form);
                    var choices = (ComboBox)Field(form, "identities"); var verify = (Button)Field(form, "verifyKey");
                    var add = (Button)Field(form, "addKey"); var retry = (Button)Field(form, "retry"); var cancel = (Button)Field(form, "cancel");
                    var report = (TextBox)Field(form, "report");
                    Check(choices.SelectedIndex == -1 && !verify.Enabled, "multiple signing identities require an explicit selection");
                    choices.SelectedIndex = 1;
                    Check(verify.Enabled && verify.AccessibilityObject.Description.Contains("вход на сервер не подтверждается") && form.AcceptButton == null,
                        "selected-key signing has its own accessible purpose and no implicit Enter action");
                    form.ClientSize = new Size(650, 560); form.PerformLayout();
                    Check(report.Height > 60 && choices.Width > 100 && add.Right <= add.Parent.ClientSize.Width && verify.Right <= verify.Parent.ClientSize.Width &&
                        verify.Bottom <= verify.Parent.ClientSize.Height && verify.ClientSize.Width >= TextRenderer.MeasureText(verify.Text, verify.Font).Width + verify.Padding.Horizontal &&
                        verify.ClientSize.Height >= TextRenderer.MeasureText(verify.Text, verify.Font).Height + verify.Padding.Vertical,
                        "minimum-size signing row retains the selector and both complete button captions inside the form");
                    KeyboardWalk(form, new Control[] { report, choices, add, verify, retry, cancel }, "SSH selected-key signing ready");
                    Shot(form, "ssh-key-signing-ready-minimum");
                    var watch = Stopwatch.StartNew(); verify.PerformClick(); var work = form.Work;
                    try {
                        PumpUntil(() => entered.IsSet && ticks >= 3);
                        Check(watch.ElapsedMilliseconds < 1500 && thread != ui && checks == 2 && signatures == 1 && selected == "second key" &&
                            !work.IsCompleted && !form.ReadinessWork.IsCompleted && !choices.Enabled && !add.Enabled && !verify.Enabled && !retry.Enabled && cancel.Enabled,
                            "one explicit signing action refreshes the frozen profile and runs the selected key off UI while heartbeat continues");
                        Shot(form, "ssh-key-signing-pending"); verify.PerformClick(); retry.PerformClick(); cancel.PerformClick();
                        Check(owner.IsCancellationRequested && !cancel.Enabled && !work.IsCompleted && signatures == 1 && checks == 2,
                            "signing cancellation preserves worker ownership and rejects duplicate probes or profile checks");
                        release.Set(); PumpUntil(() => work.IsCompleted); work.GetAwaiter().GetResult();
                        Check(form.Visible && ((Label)Field(form, "status")).Text.Contains("отменена") && report.TextLength == 0 && choices.Items.Count == 0 &&
                            !verify.Enabled && retry.Enabled && cancel.Text == "Закрыть", "late signing success cannot replace cancellation or retain stale key evidence");
                        Shot(form, "ssh-key-signing-cancelled");
                    } finally { release.Set(); AgentProbeSettled(work); form.Close(); }
                }
            }
        }
        private static void SigningUiOutcomes(string root)
        {
            foreach (var state in new[] { SshKeyReadinessState.AgentSigningVerified, SshKeyReadinessState.CommandFailed, SshKeyReadinessState.AgentUnavailable,
                SshKeyReadinessState.Busy, SshKeyReadinessState.TimedOut, SshKeyReadinessState.InvalidOutput, SshKeyReadinessState.Failed }) {
                var expected = state;
                using (var form = new SshDiagnosticForm(new SshProfileSetting { Target = "selected.fixture" }, (p, token) => KeyUiProfile(root, "selected key"),
                    null, null, (profile, identity, token) => new SshKeyReadinessResult(expected, expected == SshKeyReadinessState.CommandFailed ? (int?)1 : null))) {
                    KeyUiReady(form); var verify = (Button)Field(form, "verifyKey"); verify.PerformClick(); PumpUntil(() => form.Work.IsCompleted); form.Work.GetAwaiter().GetResult();
                    string result = ((Label)Field(form, "status")).Text;
                    Check(result.Contains(new SshKeyReadinessResult(state).ToDisplay()) && ((Button)Field(form, "retry")).Enabled && verify.Enabled &&
                        (state != SshKeyReadinessState.CommandFailed || result.Contains("Код ssh-add: 1")), "typed signing UI outcome remains local evidence with retry available: " + state);
                    if (state == SshKeyReadinessState.AgentSigningVerified) Shot(form, "ssh-key-signing-completed");
                    if (state == SshKeyReadinessState.CommandFailed) Shot(form, "ssh-key-signing-refused");
                    form.Close();
                }
            }
        }
        private static void SigningUiFreshness(string root)
        {
            for (int variant = 0; variant < 6; variant++) {
                int checks = 0, signatures = 0, current = variant;
                using (var form = new SshDiagnosticForm(new SshProfileSetting { Target = "selected.fixture" }, (p, token) => {
                    var value = KeyUiProfile(root, "selected key");
                    if (Interlocked.Increment(ref checks) > 1) {
                        if (current == 0) value.IdentityPaths["selected key"] = Path.Combine(root, "replacement key");
                        if (current == 1) value.ExecutablePath = Path.Combine(root, "replacement", "ssh.exe");
                        if (current == 2) value.ResolvedHostName = "changed.fixture";
                        if (current == 3) value.PreferredAuthentications = "password";
                        if (current == 4) value.IdentityFiles.Add("another key");
                        if (current == 5) value.IdentityAgent = "changed.fixture";
                    }
                    return value;
                }, null, null, (profile, identity, token) => { signatures++; return SigningUiSuccess(); })) {
                    KeyUiReady(form); ((Button)Field(form, "verifyKey")).PerformClick(); PumpUntil(() => form.Work.IsCompleted);
                    Check(checks == 2 && signatures == 0 && ((Label)Field(form, "status")).Text.Contains("изменились") &&
                        ((ComboBox)Field(form, "identities")).Items.Count == 0 && !((Button)Field(form, "verifyKey")).Enabled,
                        "a changed effective profile or selected path cannot sign a silently substituted identity: " + variant); form.Close();
                }
            }
        }
        private static void SigningUiSelection(string root)
        {
            using (var form = new SshDiagnosticForm(new SshProfileSetting { Target = "selected.fixture" }, (p, token) => KeyUiProfile(root, "first key", "second key"),
                null, null, (profile, identity, token) => SigningUiSuccess())) {
                KeyUiReady(form); var choices = (ComboBox)Field(form, "identities"); var verify = (Button)Field(form, "verifyKey");
                choices.SelectedIndex = 0; verify.PerformClick(); PumpUntil(() => form.Work.IsCompleted);
                Check(((Label)Field(form, "status")).Text.Contains("Агент выполнил подпись"), "successful signing is shown only after the selected-key operation completes");
                choices.SelectedIndex = 1;
                Check(!((Label)Field(form, "status")).Text.Contains("Агент выполнил подпись") && ((Label)Field(form, "status")).Text.Contains("ещё не проверена"),
                    "choosing another identity invalidates the previous signing observation immediately");
                ((Button)Field(form, "retry")).PerformClick(); PumpUntil(() => form.Work.IsCompleted);
                Check(choices.SelectedIndex == -1 && !verify.Enabled, "a repeated profile check clears the signing selection and result"); form.Close();
            }
            using (var entered = new ManualResetEventSlim()) using (var release = new ManualResetEventSlim())
            using (var form = new SshDiagnosticForm(new SshProfileSetting { Target = "selected.fixture" }, (p, token) => KeyUiProfile(root, "first key", "second key"),
                null, null, (profile, identity, token) => { entered.Set(); release.Wait(10000); return SigningUiSuccess(); })) {
                KeyUiReady(form); var choices = (ComboBox)Field(form, "identities"); choices.SelectedIndex = 0;
                ((Button)Field(form, "verifyKey")).PerformClick(); var work = form.Work;
                try {
                    Check(entered.Wait(3000), "held signing fixture owns its selected identity before a programmatic selection change");
                    choices.SelectedIndex = 1; release.Set(); PumpUntil(() => work.IsCompleted);
                    Check(((Label)Field(form, "status")).Text.Contains("изменились") && choices.Items.Count == 0,
                        "programmatic selection changes during disabled UI cannot publish signing evidence for another key");
                } finally { release.Set(); AgentProbeSettled(work); form.Close(); }
            }
        }
        private static void SigningUiMutations(string root)
        {
            foreach (bool service in new[] { false, true }) {
                int starts = 0, adds = 0;
                var probe = new SshAgentServiceProbe(t => { throw new Exception("Unexpected service read"); }, t => { starts++; return AgentStartUiSuccess(); });
                using (var form = new SshDiagnosticForm(new SshProfileSetting { Target = "selected.fixture" }, (p, token) => {
                    var value = KeyUiProfile(root, "selected key");
                    value.AgentProbe = new SshAgentProbeResult(SshAgentProbeState.Completed,
                        new SshAgentServiceSnapshot { RuntimeState = SshAgentRuntimeState.Stopped, StartMode = SshAgentStartMode.Manual });
                    return value;
                }, (request, token) => { adds++; return new SshAgentKeyAddResult(SshAgentKeyAddState.CommandCompleted); }, probe,
                    (profile, identity, token) => SigningUiSuccess())) {
                    KeyUiReady(form); ((Button)Field(form, "verifyKey")).PerformClick(); PumpUntil(() => form.Work.IsCompleted);
                    ((Button)Field(form, service ? "startAgent" : "addKey")).PerformClick(); PumpUntil(() => form.Work.IsCompleted);
                    Check(((ComboBox)Field(form, "identities")).Items.Count == 0 && !((Button)Field(form, "verifyKey")).Enabled &&
                        !((Label)Field(form, "status")).Text.Contains("Агент выполнил подпись") && (service ? starts == 1 && adds == 0 : adds == 1 && starts == 0),
                        "service start or key addition invalidates the old signing result and requires a fresh profile check: " + service); form.Close();
                }
            }
        }
        private static void SigningUiClosed(string root, bool dispose)
        {
            using (var entered = new ManualResetEventSlim()) using (var release = new ManualResetEventSlim()) {
                CancellationToken owner = CancellationToken.None; int late = 0;
                using (var form = new SshDiagnosticForm(new SshProfileSetting { Target = "selected.fixture" }, (p, token) => KeyUiProfile(root, "selected key"),
                    null, null, (profile, identity, token) => {
                        using (var lease = SshReadOnlyLease.TryAcquire(token)) {
                            if (lease == null) throw new Exception("Signing fixture ownership unavailable");
                            owner = token; entered.Set(); if (!release.Wait(15000)) throw new TimeoutException("Closed signing release deadline"); return SigningUiSuccess();
                        }
                    })) {
                    KeyUiReady(form); ((Button)Field(form, "verifyKey")).PerformClick(); var work = form.Work;
                    try {
                        Check(entered.Wait(3000), "closable signing UI reaches its owned worker: " + dispose);
                        ((Label)Field(form, "status")).TextChanged += delegate { late++; };
                        var watch = Stopwatch.StartNew(); if (dispose) form.Dispose(); else form.Close();
                        Check(watch.ElapsedMilliseconds < 1000 && owner.IsCancellationRequested && !work.IsCompleted &&
                            SshAgentServiceProbe.Shared.ReadAsync(1000, CancellationToken.None).GetAwaiter().GetResult().State == SshAgentProbeState.Busy,
                            "Close or Dispose cancels signing without waiting on UI or releasing unsettled ownership: " + dispose);
                        release.Set(); AgentProbeSettled(work); work.GetAwaiter().GetResult(); Application.DoEvents();
                        using (var lease = SshReadOnlyLease.TryAcquire(CancellationToken.None)) {
                            Check(form.IsDisposed && form.ReadinessWork.IsCompleted && late == 0 && lease != null,
                                "late signing completion settles without UI pumping, publication or a leaked lease: " + dispose);
                        }
                    } finally { release.Set(); AgentProbeSettled(work); }
                }
            }
        }
        private static void SigningUiQueuedCancel(string root)
        {
            using (var form = new SshDiagnosticForm(new SshProfileSetting { Target = "selected.fixture" }, (p, token) => KeyUiProfile(root, "selected key"),
                null, null, (profile, identity, token) => SigningUiSuccess())) {
                KeyUiReady(form); ((Button)Field(form, "verifyKey")).PerformClick(); AgentProbeSettled(form.ReadinessWork);
                bool queued = !form.Work.IsCompleted; ((Button)Field(form, "cancel")).PerformClick(); PumpUntil(() => form.Work.IsCompleted);
                Check(queued && ((Label)Field(form, "status")).Text.Contains("отменена") && ((TextBox)Field(form, "report")).TextLength == 0 &&
                    !((Button)Field(form, "verifyKey")).Enabled, "cancel before queued signing publication suppresses an already completed successful observation"); form.Close();
            }
        }
    }
}

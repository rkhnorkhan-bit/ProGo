using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        private static void AsyncDiagnosticPreviewWorkflow()
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") {
                Console.WriteLine("SKIP: asynchronous diagnostic file fixtures require isolated Windows CI"); return;
            }
            var root = Path.Combine(work, "async-diagnostic-preview-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
            try {
                AsyncDiagnosticBuildCancelRetry(root);
                AsyncDiagnosticModalCancelRetry(root, false, false);
                AsyncDiagnosticModalCancelRetry(root, false, true);
                AsyncDiagnosticModalCancelRetry(root, true, false);
                AsyncDiagnosticModalCancelRetry(root, true, true);
                AsyncDiagnosticClose(root, "build", false);
                AsyncDiagnosticClose(root, "build", true);
                AsyncDiagnosticClose(root, "save", false);
                AsyncDiagnosticClose(root, "save", true);
                AsyncDiagnosticQueuedDispose(root);
                AsyncDiagnosticLargeAndLockedSource(root);
                AsyncDiagnosticSaveCancelRetry(root);
                AsyncDiagnosticSaveCommitted(root, false);
                AsyncDiagnosticSaveCommitted(root, true);
                AsyncDiagnosticSaveFailureAndChooserCancel(root);
                AsyncDiagnosticNativeChooserCancel(root);
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }
        private static string AsyncDiagnosticSource(string root)
        {
            var source = Path.Combine(root, "source-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(source);
            File.WriteAllText(Path.Combine(source, "progo.log"), "2026-01-02 03:04:05 +00:00 [ERROR] Failed to start SSH tunnel. password=private-preview-fixture\nunknown private-preview-fixture\n");
            File.WriteAllText(Path.Combine(source, "progo.log.1"), "SOCKS listener is ready. path=private-preview-fixture");
            File.WriteAllText(Path.Combine(source, "progo.log.2"), "Application HTTP proxy started. authorization=private-preview-fixture");
            File.WriteAllText(Path.Combine(source, "progo-update.log"), "TRANSACTION FAILED: private-preview-fixture");
            File.WriteAllText(Path.Combine(source, "progo-restore.log"), "ProGo restore completed. private-preview-fixture");
            return source;
        }
        private static Button AsyncDiagnosticButton(DiagnosticPreviewForm form, string name)
        { return form.Controls.Find(name, true).OfType<Button>().Single(); }
        private static string AsyncDiagnosticText(DiagnosticPreviewForm form)
        { return ((TextBox)Field(form, "preview")).Text; }
        private static void AsyncDiagnosticReady(DiagnosticPreviewForm form)
        {
            form.Show(); PumpAsyncRestore(() => form.ReportReady && !form.IsBusy && form.BuildWork.IsCompleted);
            form.BuildWork.GetAwaiter().GetResult();
        }
        private static void AsyncDiagnosticBuildCancelRetry(string root)
        {
            string source = AsyncDiagnosticSource(root); var hashes = AsyncRestoreHashes(source);
            int owner = Thread.CurrentThread.ManagedThreadId, worker = 0, builds = 0, ticks = 0;
            CancellationToken seen = default(CancellationToken);
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var pulse = new System.Windows.Forms.Timer { Interval = 10 })
            using (var form = new DiagnosticPreviewForm(source, "0.2.2", beforeIo: delegate(string phase, CancellationToken token) {
                if (phase == "build") { worker = Thread.CurrentThread.ManagedThreadId; seen = token; if (Interlocked.Increment(ref builds) == 1) { entered.Set(); release.Wait(); } }
            })) {
                pulse.Tick += delegate { ticks++; }; pulse.Start();
                form.Show(); PumpAsyncRestore(() => entered.IsSet); var pending = form.BuildWork;
                try {
                    PumpAsyncRestore(() => ticks >= 3);
                    var copy = AsyncDiagnosticButton(form, "DiagnosticCopy"); var save = AsyncDiagnosticButton(form, "DiagnosticSave"); var close = (Button)form.CancelButton;
                    Check(form.Visible && form.IsBusy && !form.ReportReady && AsyncDiagnosticText(form) == "" && !pending.IsCompleted && worker != owner &&
                        !copy.Enabled && !save.Enabled && close.Enabled && close.Focused,
                        "real diagnostic modal content starts empty while its held file-build worker keeps native heartbeat and cancellation active");
                    Check(Object.ReferenceEquals(pending, form.StartBuildAsync()) && Object.ReferenceEquals(pending, form.StartSaveAsync()) && builds == 1,
                        "duplicate build or premature save shares the exact pending diagnostic actor without another disk read or export");
                    close.PerformClick();
                    Check(seen.IsCancellationRequested && form.Visible && form.IsBusy && !pending.IsCompleted,
                        "diagnostic Cancel requests cancellation while preserving ownership of an uninterruptible pending read phase");
                    release.Set(); PumpAsyncRestore(() => pending.IsCompleted && !form.IsBusy);
                    Check(!pending.IsFaulted && !form.ReportReady && AsyncDiagnosticText(form) == "" && ((Label)Field(form, "status")).Text.Contains("отменена") &&
                        copy.Enabled && !save.Enabled && AsyncRestoreTreeMatches(source, hashes),
                        "cancelled diagnostic build is never shown as a ready report and preserves all actual source bytes for explicit retry");
                    copy.PerformClick(); var retry = form.BuildWork; PumpAsyncRestore(() => retry.IsCompleted && !form.IsBusy);
                    string text = AsyncDiagnosticText(form);
                    Check(!Object.ReferenceEquals(pending, retry) && builds == 2 && form.ReportReady && text.Contains("SSH_START_FAILED") && text.Contains("SOCKS_READY") &&
                        text.Contains("HTTP_PROXY_START") && text.Contains("UPDATE_FAILED") && text.Contains("RESTORE_COMPLETE") && !text.Contains("private-preview-fixture") &&
                        copy.Text == "Копировать отчёт" && save.Enabled && AsyncRestoreTreeMatches(source, hashes),
                        "explicit retry runs real bounded log projection and presents only its sanitized known events from current archives and legacy sources");
                    form.ClientSize = new Size(700, 500); Application.DoEvents();
                    KeyboardWalk(form, new Control[] { (TextBox)Field(form, "preview"), copy, save, close }, "async diagnostic ready minimum");
                    Check(new[] { copy, save, close }.All(b => b.Parent.ClientRectangle.Contains(b.Bounds)),
                        "asynchronous diagnostic completion preserves existing minimum-size keyboard actions");
                }
                finally { release.Set(); form.Dispose(); WaitAsyncRestoreWithoutPump(form.Work); }
            }
        }
        private static void AsyncDiagnosticModalCancelRetry(string root, bool saving, bool escape)
        {
            var source = Path.Combine(root, "modal-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(source);
            var log = Path.Combine(source, "progo.log"); var destination = Path.Combine(source, "reviewed.txt");
            File.WriteAllText(log, "SOCKS start requested.\n");
            int builds = 0, writes = 0, stage = 0, ticks = 0, afterCancel = 0, afterRetry = 0;
            string snapshot = null, accepted = null; Task first = null, retry = null; Exception failure = null;
            CancellationToken firstToken = default(CancellationToken);
            using (var firstEntered = new ManualResetEventSlim())
            using (var releaseFirst = new ManualResetEventSlim())
            using (var retryEntered = new ManualResetEventSlim())
            using (var releaseRetry = new ManualResetEventSlim())
            using (var form = new DiagnosticPreviewForm(source, "0.2.2", chooseSave: owner => destination,
                beforeIo: delegate(string phase, CancellationToken token) {
                    int attempt = phase == "build" ? Interlocked.Increment(ref builds) :
                        phase == "save" ? Interlocked.Increment(ref writes) : 0;
                    // First build has already read the old log: cancellation must
                    // discard its real report even when publication is delayed.
                    if ((!saving && phase == "owner-completion" && Volatile.Read(ref builds) == 1) || (saving && phase == "save" && attempt == 1)) {
                        firstToken = token; firstEntered.Set();
                        if (!releaseFirst.Wait(3000)) throw new TimeoutException("modal first phase deadline");
                    }
                    if ((!saving && phase == "build" || saving && phase == "save") && attempt == 2) {
                        retryEntered.Set();
                        if (!releaseRetry.Wait(3000)) throw new TimeoutException("modal retry phase deadline");
                    }
                }))
            using (var pulse = new System.Windows.Forms.Timer { Interval = 10 })
            {
                var deadline = Stopwatch.StartNew();
                pulse.Tick += delegate {
                    try {
                        if (deadline.ElapsedMilliseconds >= 8000) throw new TimeoutException("modal Cancel/Retry fixture deadline");
                        ticks++;
                        if (saving && stage == 0 && form.ReportReady && !form.IsBusy && form.BuildWork.IsCompleted) {
                            snapshot = AsyncDiagnosticText(form); first = form.StartSaveAsync(); stage = 1;
                        }
                        if (stage == (saving ? 1 : 0) && firstEntered.IsSet) {
                            first = form.Work;
                            Check(form.Modal && form.Visible && form.IsBusy && !first.IsCompleted,
                                "Cancel/Retry runs in a real modal dialog while its old file result or save remains owned");
                            Check(Object.ReferenceEquals(first, form.StartBuildAsync()) && Object.ReferenceEquals(first, form.StartSaveAsync()),
                                "modal duplicate diagnostics cannot overtake the first owned operation");
                            stage = 2; afterCancel = ticks;
                            if (escape) typeof(Form).GetMethod("ProcessDialogKey", PrivateInstance).Invoke(form, new object[] { Keys.Escape });
                            else ((Button)form.CancelButton).PerformClick();
                            Check(firstToken.IsCancellationRequested && form.Visible && !first.IsCompleted,
                                "modal click or Escape requests cancellation without releasing the held operation");
                        }
                        if (stage == 2 && ticks >= afterCancel + 3) {
                            // Return to the modal loop before releasing I/O, so an
                            // implicit DialogResult.Cancel cannot hide in one Tick.
                            Check(form.Visible && form.IsBusy && !first.IsCompleted && Object.ReferenceEquals(first, form.StartBuildAsync()),
                                "modal cancellation preserves single flight across subsequent native message-loop turns");
                            File.WriteAllText(log, "SOCKS listener is ready.\n");
                            releaseFirst.Set(); stage = 3;
                        }
                        if (stage == 3 && first.IsCompleted && !form.IsBusy) {
                            Check(!first.IsFaulted && form.Visible && form.Modal,
                                "cancelled modal work settles without an unexpected close or private failure");
                            if (saving) Check(form.ReportReady && AsyncDiagnosticText(form) == snapshot && !File.Exists(destination) &&
                                ((Label)Field(form, "status")).Text == "Сохранение отменено.",
                                "modal cancelled save retains its reviewed snapshot and creates no late file");
                            else Check(!form.ReportReady && AsyncDiagnosticText(form) == "" && ((Label)Field(form, "status")).Text.Contains("отменена"),
                                "the completed but cancelled old read is never accepted as the current report");
                            var button = AsyncDiagnosticButton(form, saving ? "DiagnosticSave" : "DiagnosticCopy");
                            Check(button.Enabled, "explicit retry is available in the same modal window after settlement");
                            button.PerformClick(); retry = form.Work; stage = 4;
                            Check(!Object.ReferenceEquals(first, retry), "modal explicit retry has a fresh operation identity");
                        }
                        if (stage == 4 && retryEntered.IsSet) {
                            Check(first.IsCompleted && !retry.IsCompleted && form.IsBusy &&
                                Object.ReferenceEquals(retry, form.StartBuildAsync()) && Object.ReferenceEquals(retry, form.StartSaveAsync()) &&
                                builds == (saving ? 1 : 2) && writes == (saving ? 2 : 0),
                                "only the fresh retry owns diagnostic I/O; duplicates never create a parallel worker");
                            releaseRetry.Set(); stage = 5;
                        }
                        if (stage == 5 && retry.IsCompleted && !form.IsBusy) {
                            accepted = AsyncDiagnosticText(form);
                            Check(!retry.IsFaulted && form.Visible && form.ReportReady &&
                                (saving ? accepted == snapshot && File.ReadAllText(destination) == snapshot :
                                    accepted.Contains("SOCKS_READY") && !accepted.Contains("SOCKS_START")),
                                "modal retry publishes only its fresh report or the exact reviewed save snapshot");
                            afterRetry = ticks; stage = 6;
                        }
                        if (stage == 6 && ticks >= afterRetry + 3) {
                            Check(AsyncDiagnosticText(form) == accepted && !form.IsBusy && form.Visible &&
                                ((Label)Field(form, "status")).Text == (saving ? "Отчёт сохранён в выбранный файл." : "Проверьте текст, затем выберите действие."),
                                "cancelled old completion cannot overwrite the fresh modal retry result on later UI turns");
                            stage = 7; ((Button)form.CancelButton).PerformClick();
                        }
                    }
                    catch (Exception ex) {
                        failure = ex; pulse.Stop(); releaseFirst.Set(); releaseRetry.Set(); form.Dispose();
                    }
                };
                pulse.Start();
                try {
                    var result = form.ShowDialog();
                    if (failure != null) throw new Exception("modal Cancel/Retry fixture failed: save=" + saving + ", Escape=" + escape, failure);
                    Check(stage == 7 && !form.Visible && result == DialogResult.Cancel && first.IsCompleted && retry.IsCompleted,
                        "explicit idle Close dismisses the modal window only after both diagnostic operations settle");
                }
                finally {
                    pulse.Stop(); releaseFirst.Set(); releaseRetry.Set();
                    var pending = Task.WhenAll(first ?? Task.FromResult(false), retry ?? Task.FromResult(false), form.Work, form.BuildWork, form.SaveWork);
                    form.Dispose(); WaitAsyncRestoreWithoutPump(pending, 5000);
                }
            }
        }

        private static void AsyncDiagnosticClose(string root, string phase, bool forcedDispose)
        {
            string source = AsyncDiagnosticSource(root), destination = Path.Combine(root, "closed-" + Guid.NewGuid().ToString("N") + ".txt");
            var hashes = AsyncRestoreHashes(source); CancellationToken seen = default(CancellationToken);
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var form = new DiagnosticPreviewForm(source, "0.2.2", chooseSave: owner => destination,
                beforeIo: delegate(string boundary, CancellationToken token) { if (boundary == phase) { seen = token; entered.Set(); release.Wait(); } })) {
                if (phase == "save") { AsyncDiagnosticReady(form); form.StartSaveAsync(); } else form.Show();
                PumpAsyncRestore(() => entered.IsSet); var pending = form.Work;
                try {
                    int lateUi = 0;
                    if (forcedDispose) {
                        ((TextBox)Field(form, "preview")).TextChanged += delegate { lateUi++; };
                        ((Label)Field(form, "status")).TextChanged += delegate { lateUi++; };
                    }
                    var watch = Stopwatch.StartNew(); if (forcedDispose) form.Dispose(); else form.Close(); watch.Stop();
                    Check(watch.ElapsedMilliseconds < 500 && seen.IsCancellationRequested && !pending.IsCompleted &&
                        (forcedDispose ? form.IsDisposed : form.Visible && form.IsBusy),
                        "diagnostic title-close or forced disposal returns promptly but retains its actual pending file phase: " + phase + "/" + forcedDispose);
                    release.Set();
                    if (forcedDispose) WaitAsyncRestoreWithoutPump(pending); else PumpAsyncRestore(() => pending.IsCompleted && !form.Visible);
                    Check(!pending.IsFaulted && !File.Exists(destination) && lateUi == 0 && AsyncRestoreTreeMatches(source, hashes) &&
                        (forcedDispose || !form.ReportReady || phase == "save"),
                        "closed diagnostic work settles without late read publication or file creation and preserves every real log byte: " + phase + "/" + forcedDispose);
                    Application.DoEvents(); Check(!form.Visible && lateUi == 0, "resumed native dispatch cannot reopen a closed diagnostic preview: " + phase + "/" + forcedDispose);
                }
                finally { release.Set(); form.Dispose(); WaitAsyncRestoreWithoutPump(pending); }
            }
        }
        private static void AsyncDiagnosticQueuedDispose(string root)
        {
            string source = AsyncDiagnosticSource(root); var hashes = AsyncRestoreHashes(source);
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var form = new DiagnosticPreviewForm(source, "0.2.2", beforeIo: delegate(string phase, CancellationToken token) {
                if (phase == "owner-completion") { entered.Set(); release.Wait(); }
            })) {
                form.Show(); PumpAsyncRestore(() => entered.IsSet); var pending = form.Work;
                try {
                    Check(!form.ReportReady && !pending.IsCompleted && AsyncDiagnosticText(form) == "", "a completed real diagnostic read remains owned until its separate owner publication");
                    release.Set();
                    AsyncUpdaterWaitWithoutPump(() => {
                        lock (Field(form, "completionGate")) return ((ICollection<Action>)Field(form, "completions")).Count > 0;
                    }, 5000);
                    int lateUi = 0; ((TextBox)Field(form, "preview")).TextChanged += delegate { lateUi++; }; ((Label)Field(form, "status")).TextChanged += delegate { lateUi++; };
                    form.Dispose(); WaitAsyncRestoreWithoutPump(pending); Application.DoEvents();
                    Check(!pending.IsFaulted && lateUi == 0 && form.IsDisposed && !form.ReportReady && AsyncRestoreTreeMatches(source, hashes),
                        "disposing after actual report completion but before queued publication drops the owner callback and settles without a UI pump or stale ready state");
                }
                finally { release.Set(); form.Dispose(); WaitAsyncRestoreWithoutPump(pending); }
            }
        }
        private static void AsyncDiagnosticLargeAndLockedSource(string root)
        {
            string source = AsyncDiagnosticSource(root), path = Path.Combine(source, "progo.log");
            using (var file = new FileStream(path, FileMode.Create, FileAccess.Write)) {
                file.SetLength(8 * 1024 * 1024); file.Position = file.Length;
                byte[] tail = Encoding.UTF8.GetBytes("\nSOCKS start requested. private-preview-fixture"); file.Write(tail, 0, tail.Length);
            }
            var hashes = AsyncRestoreHashes(source);
            using (var form = new DiagnosticPreviewForm(source, "0.2.2")) {
                AsyncDiagnosticReady(form); string text = AsyncDiagnosticText(form);
                Check(text.Contains("SOCKS_START") && text.Contains("64 КиБ") && text.Length < 40000 && !text.Contains("private-preview-fixture") &&
                    AsyncRestoreTreeMatches(source, hashes),
                    "actual preview worker bounds a huge historical log and projects only its complete final events without source mutation");
            }
            using (var locked = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            using (var form = new DiagnosticPreviewForm(source, "0.2.2")) {
                AsyncDiagnosticReady(form); string text = AsyncDiagnosticText(form);
                Check(text.Contains("Файл недоступен.") && text.Contains("UPDATE_FAILED") && !text.Contains(path) && !text.Contains("private-preview-fixture"),
                    "actual locked source becomes a fixed unavailable section while remaining real report sources stay readable and sanitized");
            }
            Check(AsyncRestoreTreeMatches(source, hashes), "unavailable-source preview preserves every locked log byte after release");
        }
        private static void AsyncDiagnosticSaveCancelRetry(string root)
        {
            string source = AsyncDiagnosticSource(root), destination = Path.Combine(root, "reviewed-" + Guid.NewGuid().ToString("N") + ".txt");
            int ownerThread = Thread.CurrentThread.ManagedThreadId, workerThread = 0, writes = 0, choices = 0, copies = 0, ticks = 0;
            string copied = null; CancellationToken seen = default(CancellationToken);
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var pulse = new System.Windows.Forms.Timer { Interval = 10 })
            using (var form = new DiagnosticPreviewForm(source, "0.2.2", copy: text => { copies++; copied = text; },
                chooseSave: owner => { choices++; Check(Thread.CurrentThread.ManagedThreadId == ownerThread, "diagnostic file choice remains on its native STA owner"); return destination; },
                beforeIo: delegate(string phase, CancellationToken token) {
                    if (phase == "save") { workerThread = Thread.CurrentThread.ManagedThreadId; seen = token; if (Interlocked.Increment(ref writes) == 1) { entered.Set(); release.Wait(); } }
                })) {
                AsyncDiagnosticReady(form); string snapshot = AsyncDiagnosticText(form);
                File.AppendAllText(Path.Combine(source, "progo.log"), "\nProGo started. private-after-preview\n"); var hashes = AsyncRestoreHashes(source);
                pulse.Tick += delegate { ticks++; }; pulse.Start();
                var pending = form.StartSaveAsync();
                try {
                    PumpAsyncRestore(() => entered.IsSet && ticks >= 3);
                    Check(form.IsBusy && !pending.IsCompleted && workerThread != ownerThread && choices == 1 && writes == 1 && !File.Exists(destination) &&
                        Object.ReferenceEquals(pending, form.StartSaveAsync()) && Object.ReferenceEquals(pending, form.StartBuildAsync()),
                        "real save owns one frozen reviewed snapshot and selected path while its slow write boundary stays off UI and ignores duplicate export requests");
                    ((Button)form.CancelButton).PerformClick(); Check(seen.IsCancellationRequested && !pending.IsCompleted, "save cancellation retains a still-running actual file operation");
                    release.Set(); PumpAsyncRestore(() => pending.IsCompleted && !form.IsBusy);
                    Check(!File.Exists(destination) && form.ReportReady && AsyncDiagnosticText(form) == snapshot && ((Label)Field(form, "status")).Text == "Сохранение отменено.",
                        "cancel before native write creates no export and preserves the exact reviewed report for retry");
                    var retry = form.StartSaveAsync(); PumpAsyncRestore(() => retry.IsCompleted && !form.IsBusy);
                    Check(!retry.IsFaulted && choices == 2 && writes == 2 && File.ReadAllText(destination) == snapshot &&
                        File.ReadAllBytes(destination).Take(3).SequenceEqual(new byte[] { 0xef, 0xbb, 0xbf }) && AsyncRestoreTreeMatches(source, hashes),
                        "explicit save retry writes the exact reviewed UTF-8 snapshot after source logs change without rereading or altering those sources");
                    AsyncDiagnosticButton(form, "DiagnosticCopy").PerformClick();
                    Check(copies == 1 && copied == snapshot && !copied.Contains("private-after-preview") && !copied.Contains("private-preview-fixture"),
                        "explicit copy after asynchronous save still exports only the same sanitized reviewed snapshot");
                }
                finally { release.Set(); form.Dispose(); WaitAsyncRestoreWithoutPump(form.Work); }
            }
        }
        private static void AsyncDiagnosticSaveCommitted(string root, bool forcedDispose)
        {
            string source = AsyncDiagnosticSource(root), destination = Path.Combine(root, "committed-" + Guid.NewGuid().ToString("N") + ".txt");
            var hashes = AsyncRestoreHashes(source);
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var form = new DiagnosticPreviewForm(source, "0.2.2", chooseSave: owner => destination,
                beforeIo: delegate(string phase, CancellationToken token) { if (phase == "save-written") { entered.Set(); release.Wait(); } })) {
                AsyncDiagnosticReady(form); string snapshot = AsyncDiagnosticText(form); var pending = form.StartSaveAsync();
                try {
                    PumpAsyncRestore(() => entered.IsSet);
                    Check(File.ReadAllText(destination) == snapshot && !pending.IsCompleted && form.IsBusy,
                        "a real completed export write remains tracked until child and owner completion settle");
                    int lateUi = 0;
                    if (forcedDispose) { ((Label)Field(form, "status")).TextChanged += delegate { lateUi++; }; form.Dispose(); }
                    else ((Button)form.CancelButton).PerformClick();
                    release.Set();
                    if (forcedDispose) WaitAsyncRestoreWithoutPump(pending); else PumpAsyncRestore(() => pending.IsCompleted && !form.IsBusy);
                    Check(!pending.IsFaulted && ((Task<bool>)pending).Result && File.ReadAllText(destination) == snapshot && AsyncRestoreTreeMatches(source, hashes) &&
                        (forcedDispose ? lateUi == 0 && form.IsDisposed : ((Label)Field(form, "status")).Text == "Отчёт сохранён в выбранный файл."),
                        "late cancellation or owner disposal never relabels a completed real write as an aborted save or publishes into a disposed form: " + forcedDispose);
                }
                finally { release.Set(); form.Dispose(); WaitAsyncRestoreWithoutPump(pending); }
            }
        }
        private static void AsyncDiagnosticSaveFailureAndChooserCancel(string root)
        {
            string source = AsyncDiagnosticSource(root), destination = Path.Combine(root, "locked-" + Guid.NewGuid().ToString("N") + ".txt");
            File.WriteAllText(destination, "previous reviewed export"); var hashes = AsyncRestoreHashes(source); int choices = 0, writes = 0;
            using (var form = new DiagnosticPreviewForm(source, "0.2.2", chooseSave: owner => ++choices == 1 ? null : destination,
                beforeIo: delegate(string phase, CancellationToken token) { if (phase == "save") Interlocked.Increment(ref writes); })) {
                AsyncDiagnosticReady(form); string snapshot = AsyncDiagnosticText(form);
                var cancelled = form.StartSaveAsync(); PumpAsyncRestore(() => cancelled.IsCompleted && !form.IsBusy);
                Check(writes == 0 && File.ReadAllText(destination) == "previous reviewed export" && ((Label)Field(form, "status")).Text == "Сохранение отменено.",
                    "cancelled diagnostic path choice starts no write worker and preserves a previous export");
                using (var locked = new FileStream(destination, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) {
                    var failed = form.StartSaveAsync(); PumpAsyncRestore(() => failed.IsCompleted && !form.IsBusy);
                    Check(!failed.IsFaulted && writes == 1 && form.ReportReady && AsyncDiagnosticText(form) == snapshot &&
                        ((Label)Field(form, "status")).Text == "Не удалось сохранить отчёт. Проверьте доступ к выбранной папке." &&
                        !String.Join(" ", Descendants(form).Select(c => c.Text)).Contains(destination),
                        "actual locked export destination produces a fixed correction without a private path or false saved state");
                }
                Check(File.ReadAllText(destination) == "previous reviewed export", "failed locked write leaves the previous export bytes intact");
                var retry = form.StartSaveAsync(); PumpAsyncRestore(() => retry.IsCompleted && !form.IsBusy);
                Check(writes == 2 && File.ReadAllText(destination) == snapshot && AsyncRestoreTreeMatches(source, hashes),
                    "explicit retry after an actual destination unlock writes only the reviewed report and preserves source logs");
            }
        }
        private const string DiagnosticChooserCleanup = "DIAGNOSTIC_CHOOSER_CLEANUP_COMPLETE";
        private const string DiagnosticChooserMissing = "native SaveFileDialog not found within 5000 ms for the fixture owner";
        private const string DiagnosticChooserInjected = "injected native SaveFileDialog fixture failure";
        private static void AsyncDiagnosticNativeChooserCancel(string root)
        {
            AsyncDiagnosticRunChooserChild(root, "cancel", 0, null);
            AsyncDiagnosticRunChooserChild(root, "missing", 20, DiagnosticChooserMissing);
            AsyncDiagnosticRunChooserChild(root, "error", 21, DiagnosticChooserInjected);
        }
        private static void AsyncDiagnosticRunChooserChild(string root, string mode, int expectedExit, string expectedFailure)
        {
            string childRoot = Path.Combine(root, "native-chooser-" + Guid.NewGuid().ToString("N"));
            DiagnosticProcessResult result;
            try {
                // Existing diagnostic ownership contains the new child before it
                // runs. Its job also bounds a native modal call that stops pumping.
                result = DiagnosticProcess.Run(Application.ExecutablePath,
                    "--diagnostic-save-chooser-fixture " + SshConnection.Quote(childRoot) + " " + mode, 20000, CancellationToken.None);
            } catch (TimeoutException ex) {
                throw new TimeoutException("native SaveFileDialog fixture exceeded 20000 ms: " + mode, ex);
            }
            bool valid = result.ExitCode == expectedExit && !result.Truncated && result.Error.Length == 0 &&
                result.Output.Contains(DiagnosticChooserCleanup) &&
                (expectedFailure == null || result.Output.Contains("FAIL: " + expectedFailure));
            string expectation = expectedExit == 0 ? "actual native save chooser cancels and cleans up its owned fixture" :
                "negative native chooser fixture returns explicit failure and confirms cleanup: " + mode;
            Check(valid, expectation + (valid ? "" : "; exit=" + result.ExitCode + "; stdout=" + result.Output + "; stderr=" + result.Error));
        }
        private static int AsyncDiagnosticNativeChooserFixture(string[] args)
        {
            try {
                Console.OutputEncoding = new UTF8Encoding(false);
                if (args.Length != 3 || !new[] { "cancel", "missing", "error" }.Contains(args[2]))
                    throw new ArgumentException("invalid native chooser fixture arguments");
                string mode = args[2], source = AsyncDiagnosticSource(Path.GetFullPath(args[1]));
                var hashes = AsyncRestoreHashes(source); int choices = 0, writes = 0;
                bool observedNative = false, cleaned = false; Exception failure = null;
                using (var form = new DiagnosticPreviewForm(source, "0.2.2", beforeIo: delegate(string phase, CancellationToken token) {
                    if (phase == "save") Interlocked.Increment(ref writes);
                }))
                using (var timer = new System.Windows.Forms.Timer { Interval = 30 }) {
                    IntPtr owner = form.Handle; uint processId;
                    uint threadId = DiagnosticChooserWindows.GetWindowThreadProcessId(owner, out processId);
                    var watch = new Stopwatch();
                    timer.Tick += delegate {
                        try {
                            if (DiagnosticChooserWindows.Find(owner, threadId, processId, null) != IntPtr.Zero) observedNative = true;
                            if (watch.ElapsedMilliseconds >= 5000)
                                throw new TimeoutException(choices == 0 ? DiagnosticChooserMissing : "native SaveFileDialog did not close within 5000 ms");
                            string title = mode == "missing" ? "deliberately unmatched fixture caption" : "Сохранить диагностический отчёт";
                            var dialog = DiagnosticChooserWindows.Find(owner, threadId, processId, title);
                            if (dialog == IntPtr.Zero || choices != 0) return;
                            choices++;
                            if (mode == "error") throw new InvalidOperationException(DiagnosticChooserInjected);
                            DiagnosticChooserWindows.CloseOwned(owner, threadId, processId);
                        } catch (Exception ex) {
                            failure = ex; timer.Stop();
                            try { DiagnosticChooserWindows.CloseOwned(owner, threadId, processId); }
                            catch (Exception cleanup) { failure = new AggregateException(ex, cleanup); }
                        }
                    };
                    try {
                        AsyncDiagnosticReady(form);
                        watch.Start(); timer.Start();
                        // This is the actual production SaveFileDialog.ShowDialog,
                        // not an injected chooser or an asynchronous approximation.
                        AsyncDiagnosticButton(form, "DiagnosticSave").PerformClick();
                        timer.Stop();
                        if (failure != null) throw failure;
                        PumpAsyncRestore(() => form.SaveWork.IsCompleted && !form.IsBusy);
                        Check(choices == 1 && observedNative && writes == 0 &&
                            ((Label)Field(form, "status")).Text == "Сохранение отменено." && AsyncRestoreTreeMatches(source, hashes),
                            "native chooser Cancel creates no export and preserves original logs");
                    } catch (Exception ex) { failure = ex; }
                    finally {
                        timer.Stop();
                        try {
                            var pending = Task.WhenAll(form.Work, form.BuildWork, form.SaveWork);
                            try { DiagnosticChooserWindows.CloseOwned(owner, threadId, processId); }
                            finally {
                                try { form.Dispose(); }
                                finally { WaitAsyncRestoreWithoutPump(pending, 5000); pending.GetAwaiter().GetResult(); }
                            }
                            if (!form.IsDisposed || DiagnosticChooserWindows.Find(owner, threadId, processId, null) != IntPtr.Zero)
                                throw new Exception("native chooser fixture cleanup left an owned window");
                            cleaned = true;
                        } catch (Exception ex) { failure = failure == null ? ex : new AggregateException(failure, ex); }
                    }
                }
                if (cleaned) Console.WriteLine(DiagnosticChooserCleanup);
                if (failure == null) return 0;
                Console.WriteLine("FAIL: " + failure.Message);
                // Only deliberately negative cases get these exact nonzero codes.
                // A real timeout in the cancel case always fails its parent test.
                if (cleaned && observedNative && writes == 0 && AsyncRestoreTreeMatches(source, hashes)) {
                    if (mode == "missing" && choices == 0 && failure is TimeoutException && failure.Message == DiagnosticChooserMissing) return 20;
                    if (mode == "error" && choices == 1 && failure is InvalidOperationException && failure.Message == DiagnosticChooserInjected) return 21;
                }
                return 1;
            } catch (Exception ex) { Console.Error.WriteLine("native SaveFileDialog fixture failed: " + ex); return 1; }
        }
        private static class DiagnosticChooserWindows
        {
            [System.Runtime.InteropServices.DllImport("user32.dll")]
            internal static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
            [System.Runtime.InteropServices.DllImport("user32.dll")]
            private static extern IntPtr GetAncestor(IntPtr window, uint flags);
            [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
            private static extern int GetClassName(IntPtr window, StringBuilder text, int count);
            [System.Runtime.InteropServices.DllImport("user32.dll")]
            private static extern bool EnumThreadWindows(uint threadId, VisitWindow visit, IntPtr data);
            private static bool IsOwned(IntPtr window, IntPtr owner, uint threadId, uint processId)
            {
                uint actualProcess;
                if (owner == IntPtr.Zero || GetWindowThreadProcessId(window, out actualProcess) != threadId ||
                    actualProcess != processId || GetAncestor(window, 3) != owner) return false; // GA_ROOTOWNER
                var cls = new StringBuilder(64); GetClassName(window, cls, cls.Capacity);
                return cls.ToString() == "#32770";
            }
            internal static IntPtr Find(IntPtr owner, uint threadId, uint processId, string title)
            {
                IntPtr found = IntPtr.Zero;
                EnumThreadWindows(threadId, delegate(IntPtr window, IntPtr unused) {
                    if (!IsOwned(window, owner, threadId, processId)) return true;
                    var text = new StringBuilder(256); GetWindowText(window, text, text.Capacity);
                    if (title != null && text.ToString() != title) return true;
                    found = window; return false;
                }, IntPtr.Zero);
                return found;
            }
            internal static void CloseOwned(IntPtr owner, uint threadId, uint processId)
            {
                bool posted = true;
                EnumThreadWindows(threadId, delegate(IntPtr window, IntPtr unused) {
                    if (IsOwned(window, owner, threadId, processId) && !PostMessage(window, 0x0010, IntPtr.Zero, IntPtr.Zero)) posted = false;
                    return true;
                }, IntPtr.Zero);
                if (!posted) throw new Exception("could not close a test-owned native chooser");
            }
        }
    }
}

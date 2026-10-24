using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProGo
{
    public static class DiagnosticPreview
    {
        public static void Show(string root, string version)
        {
            using (var form = new DiagnosticPreviewForm(root, version)) form.ShowDialog();
        }
        public static void ShowInstalled(string root)
        {
            using (var form = new DiagnosticPreviewForm(root, null, readInstalledVersion: true)) form.ShowDialog();
        }
    }

    internal sealed class DiagnosticPreviewForm : ProGoForm
    {
        private sealed class Operation
        {
            internal readonly CancellationTokenSource Cancellation = new CancellationTokenSource();
            internal readonly CancellationToken Token;
            internal readonly TaskCompletionSource<bool> Completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            internal Task Worker, Workflow;
            internal Operation() { Token = Cancellation.Token; }
            internal void Cancel() { try { Cancellation.Cancel(); } catch (ObjectDisposedException) { } }
        }
        private sealed class Outcome
        {
            internal DiagnosticReport Report;
            internal bool Cancelled, Failed, Saved;
        }
        private readonly string root, version;
        private readonly bool readInstalledVersion;
        private readonly Action<string> copy;
        private readonly Func<string, bool> legacySave;
        private readonly Func<IWin32Window, string> chooseSave;
        private readonly Action<string, CancellationToken> beforeIo;
        private readonly int ownerThread = Thread.CurrentThread.ManagedThreadId;
        private readonly Control dispatcher = new Control();
        private readonly object completionGate = new object();
        private readonly List<Action> completions = new List<Action>();
        private DiagnosticReport report;
        private Operation active;
        private readonly Label status;
        private readonly TextBox preview;
        private readonly Button copyButton, saveButton, close;
        private volatile bool disposed, closing;
        private bool closeAfterWork, buildStarted;
        internal Task Work { get; private set; }
        internal Task BuildWork { get; private set; }
        internal Task SaveWork { get; private set; }
        internal bool IsBusy { get { return active != null; } }
        internal bool ReportReady { get { return report != null; } }

        // The installed PowerShell helper uses the same root/version path. No
        // log read is performed while constructing or showing the empty window.
        internal DiagnosticPreviewForm(string root, string version, Action<string> copy = null,
            Func<IWin32Window, string> chooseSave = null, Action<string, CancellationToken> beforeIo = null, bool readInstalledVersion = false)
            : this(null, root, version, copy, null, chooseSave, beforeIo, readInstalledVersion) { }

        // Existing snapshot callers retain their reviewed immutable report. The
        // optional legacy test writer runs on the save worker, never on the owner.
        internal DiagnosticPreviewForm(DiagnosticReport report, Action<string> copy = null, Func<string, bool> save = null)
            : this(RequireReport(report), null, null, copy, save, null, null) { }
        private static DiagnosticReport RequireReport(DiagnosticReport report)
        { if (report == null) throw new ArgumentNullException("report"); return report; }

        private DiagnosticPreviewForm(DiagnosticReport report, string root, string version, Action<string> copy,
            Func<string, bool> legacySave, Func<IWin32Window, string> chooseSave, Action<string, CancellationToken> beforeIo, bool readInstalledVersion = false)
        {
            this.report = report; this.root = root; this.version = version; this.copy = copy; this.readInstalledVersion = readInstalledVersion;
            this.legacySave = legacySave; this.chooseSave = chooseSave; this.beforeIo = beforeIo;
            Work = BuildWork = SaveWork = Task.FromResult(false); dispatcher.CreateControl();
            Text = "Передать диагностику · ProGo"; ClientSize = new Size(820, 600); MinimumSize = new Size(700, 500);
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 1, RowCount = 4 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var notice = new Label { AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 12),
                AccessibleName = "Перед передачей диагностики",
                Text = "Просмотрите отчёт перед передачей. Здесь только время и известные события — без исходного текста ошибок, адресов и личных путей. Оригиналы остаются на компьютере. Ничего не отправляется автоматически." };
            notice.AccessibleDescription = notice.Text;
            preview = new TextBox { Name = "diagnosticText", Multiline = true, ReadOnly = true, WordWrap = false,
                ScrollBars = ScrollBars.Both, Dock = DockStyle.Fill, Text = report == null ? "" : report.Text, TabIndex = 0,
                AccessibleName = "Предпросмотр диагностического отчёта", AccessibleDescription = "Только чтение. Именно этот снимок отчёта будет скопирован или сохранён; исходные журналы не перечитываются. Можно выделить текст для просмотра." };
            status = new Label { AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 10, 0, 10),
                Text = report == null ? "Подготавливаем отчёт в фоне. Можно отменить подготовку." : "Проверьте текст, затем выберите действие.",
                AccessibleName = "Результат передачи диагностики" };
            status.AccessibleDescription = status.Text;
            status.TextChanged += delegate { status.AccessibleDescription = status.Text; };
            var actions = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = true, Margin = new Padding(0), TabIndex = 1 };
            copyButton = UiTheme.Button("Копировать отчёт", delegate {
                if (disposed || closing || IsBusy) return;
                if (this.report == null) { StartBuildAsync(); return; }
                try { (this.copy ?? new Action<string>(Clipboard.SetText))(this.report.Text); status.Text = "Отчёт скопирован. Внешняя отправка остаётся за вами."; }
                catch { status.Text = "Не удалось скопировать. Буфер может быть занят — попробуйте ещё раз."; }
            }, true);
            saveButton = UiTheme.Button("Сохранить как…", delegate { StartSaveAsync(); }, false);
            close = UiTheme.Button("Закрыть", delegate { if (IsBusy) RequestCancellation(); else CancelAndClose(); }, false);
            copyButton.Name = "DiagnosticCopy"; saveButton.Name = "DiagnosticSave"; close.Name = "DiagnosticClose";
            saveButton.AccessibleDescription = "Открывает выбор файла для сохранения только показанного отчёта. Отмена выбора не сохраняет файл; внешней отправки нет.";
            close.AccessibleDescription = "Закрывает предпросмотр без копирования и сохранения. Исходные журналы остаются на компьютере; подключение ProGo не отключается. Во время операции запрашивает отмену и ждёт завершения текущего обращения к файлу.";
            actions.Controls.Add(copyButton); actions.Controls.Add(saveButton); actions.Controls.Add(close);
            layout.Controls.Add(notice, 0, 0); layout.Controls.Add(preview, 0, 1); layout.Controls.Add(status, 0, 2); layout.Controls.Add(actions, 0, 3);
            Controls.Add(layout); CancelButton = close; AcceptButton = null;
            // Assigning Form.CancelButton implicitly sets DialogResult.Cancel.
            // Busy cancellation must leave a modal preview available for retry;
            // idle/title/shutdown close paths already close it explicitly.
            close.DialogResult = DialogResult.None;
            layout.SizeChanged += delegate { notice.MaximumSize = new Size(Math.Max(200, layout.ClientSize.Width - layout.Padding.Horizontal), 0); };
            Shown += delegate {
                if (this.report == null && !buildStarted && !closing) StartBuildAsync();
                else { preview.Focus(); preview.Select(0, 0); }
            };
            UiTheme.ConfigureKeyboardOrder(this); RefreshActions();
        }

        private void RequireOwner()
        { if (Thread.CurrentThread.ManagedThreadId != ownerThread) throw new InvalidOperationException("Операцией отчёта управляет владелец интерфейса."); }
        private void Probe(string phase, CancellationToken token)
        { if (beforeIo != null) beforeIo(phase, token); }
        private Operation BeginOperation(bool saving)
        {
            var operation = new Operation(); active = operation; Work = operation.Completion.Task;
            if (saving) SaveWork = Work; else BuildWork = Work;
            RefreshActions(); close.Focus(); return operation;
        }
        internal Task StartBuildAsync()
        {
            RequireOwner();
            if (disposed || closing) return Work;
            if (IsBusy) return Work;
            if (report != null) return BuildWork;
            buildStarted = true; var operation = BeginOperation(false);
            status.Text = "Подготавливаем отчёт в фоне. Можно отменить подготовку.";
            operation.Workflow = Task.Run(() => RunOperation(operation, false, null, null)); return operation.Completion.Task;
        }
        internal Task StartSaveAsync()
        {
            RequireOwner();
            if (disposed || closing || report == null) return Work;
            if (IsBusy) return Work;
            var snapshot = report.Text; var operation = BeginOperation(true);
            string path = null; Outcome early = null;
            // File selection and overwrite consent remain on the native STA owner.
            try {
                if (legacySave == null) path = chooseSave == null ? ChooseSavePath(this) : chooseSave(this);
                if (operation.Token.IsCancellationRequested || disposed || closing || (legacySave == null && path == null))
                    early = new Outcome { Cancelled = true };
            } catch { early = new Outcome { Failed = true }; }
            if (!disposed && !closing && early == null) status.Text = "Сохраняем показанный отчёт в выбранный файл. Можно запросить отмену.";
            operation.Workflow = Task.Run(() => RunOperation(operation, true, path, snapshot, early)); return operation.Completion.Task;
        }
        private static string ReadInstalledVersion(string root, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            try {
                using (var reader = File.OpenText(Path.Combine(root, "VERSION"))) {
                    var buffer = new char[32]; int count = reader.Read(buffer, 0, buffer.Length);
                    token.ThrowIfCancellationRequested(); return new string(buffer, 0, count).Trim();
                }
            }
            catch (OperationCanceledException) { throw; }
            catch { token.ThrowIfCancellationRequested(); return String.Empty; }
        }

        private async Task RunOperation(Operation operation, bool saving, string path, string snapshot, Outcome early = null)
        {
            Outcome outcome = early;
            try {
                if (outcome == null) {
                    try {
                        var worker = Task.Run(delegate {
                            operation.Token.ThrowIfCancellationRequested();
                            Probe(saving ? "save" : "build", operation.Token); operation.Token.ThrowIfCancellationRequested();
                            if (!saving) {
                                string snapshotVersion = version;
                                if (readInstalledVersion) {
                                    Probe("version-read", operation.Token); operation.Token.ThrowIfCancellationRequested();
                                    snapshotVersion = ReadInstalledVersion(root, operation.Token);
                                }
                                return new Outcome { Report = DiagnosticReport.Build(root, snapshotVersion, operation.Token) };
                            }
                            if (legacySave != null) return new Outcome { Saved = legacySave(snapshot), Cancelled = false };
                            operation.Token.ThrowIfCancellationRequested();
                            File.WriteAllText(path, snapshot, new UTF8Encoding(true));
                            // A returned native write is already committed. Late
                            // cancellation cannot turn it into an aborted save.
                            try { Probe("save-written", operation.Token); } catch (Exception) { }
                            return new Outcome { Saved = true };
                        });
                        operation.Worker = worker; outcome = await worker.ConfigureAwait(false);
                        if (saving && !outcome.Saved) outcome.Cancelled = true;
                    }
                    catch (OperationCanceledException) { outcome = new Outcome { Cancelled = true }; }
                    catch { outcome = new Outcome { Failed = true }; }
                }
                try { Probe("owner-completion", operation.Token); } catch (Exception) { }
                var completed = outcome;
                await Publish(delegate {
                    if (!Object.ReferenceEquals(active, operation)) return;
                    try {
                        if (saving) {
                            status.Text = completed.Saved ? "Отчёт сохранён в выбранный файл." : completed.Failed ?
                                "Не удалось сохранить отчёт. Проверьте доступ к выбранной папке." : "Сохранение отменено.";
                        } else if (operation.Token.IsCancellationRequested || completed.Cancelled) {
                            status.Text = "Подготовка отчёта отменена. Можно повторить подготовку.";
                        } else if (completed.Failed || completed.Report == null) {
                            status.Text = "Не удалось подготовить отчёт. Повторите подготовку; исходные журналы сохранены.";
                        } else {
                            report = completed.Report; preview.Text = report.Text; preview.Select(0, 0);
                            status.Text = "Проверьте текст, затем выберите действие."; preview.Focus();
                        }
                    } finally {
                        active = null; RefreshActions();
                        if (closeAfterWork) CloseSettled();
                    }
                }).ConfigureAwait(false);
            }
            catch (Exception) { /* A vanished owner cannot strand its worker or task. */ }
            finally {
                operation.Cancellation.Dispose();
                if (disposed) Interlocked.CompareExchange(ref active, null, operation);
                operation.Completion.TrySetResult(outcome != null && !outcome.Failed && !outcome.Cancelled);
            }
        }
        private Task<bool> Publish(Action action)
        {
            if (disposed || dispatcher.IsDisposed) return Task.FromResult(false);
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            Action drop = delegate { completion.TrySetResult(false); };
            lock (completionGate) { if (disposed) return Task.FromResult(false); completions.Add(drop); }
            try {
                dispatcher.BeginInvoke(new Action(delegate {
                    try { if (disposed || IsDisposed) drop(); else { action(); completion.TrySetResult(true); } }
                    catch (Exception ex) { completion.TrySetException(ex); }
                    finally { lock (completionGate) completions.Remove(drop); }
                }));
            } catch (InvalidOperationException) {
                lock (completionGate) completions.Remove(drop); drop();
            }
            return completion.Task;
        }
        private void RefreshActions()
        {
            copyButton.Text = report == null ? "Повторить подготовку" : "Копировать отчёт";
            copyButton.AccessibleDescription = report == null ? "Повторяет подготовку отчёта в фоне; исходные журналы не меняются." :
                "Копирует только показанный отчёт в буфер обмена. Ничего не отправляется; исходные журналы не копируются.";
            copyButton.Enabled = !IsBusy && !closing; saveButton.Enabled = report != null && !IsBusy && !closing;
            close.Text = IsBusy ? "Отменить операцию" : "Закрыть";
        }
        private void RequestCancellation()
        {
            var operation = active; if (operation == null) return;
            operation.Cancel(); if (!disposed && !IsDisposed) status.Text = "Отмена запрошена. Ждём завершения текущей операции с файлами; интерфейс продолжает работать.";
        }
        internal Task CancelAndClose()
        {
            RequireOwner(); closing = closeAfterWork = true;
            var pending = Work;
            if (IsBusy) { RequestCancellation(); if (!disposed) RefreshActions(); }
            else if (!disposed) CloseSettled();
            return pending;
        }
        private void CloseSettled()
        { DialogResult = DialogResult.Cancel; Close(); }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (IsBusy) { e.Cancel = true; closing = closeAfterWork = true; RequestCancellation(); RefreshActions(); }
            else closing = true;
            base.OnFormClosing(e);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing && !disposed) {
                disposed = closing = true; var operation = active; if (operation != null) operation.Cancel();
                lock (completionGate) { foreach (var drop in completions) drop(); completions.Clear(); }
                dispatcher.Dispose();
            }
            base.Dispose(disposing);
        }
        private static string ChooseSavePath(IWin32Window owner)
        {
            using (var dialog = new SaveFileDialog { Title = "Сохранить диагностический отчёт", Filter = "Текстовый отчёт (*.txt)|*.txt",
                FileName = "ProGo-diagnostics.txt", DefaultExt = "txt", AddExtension = true, OverwritePrompt = true, RestoreDirectory = true })
                return dialog.ShowDialog(owner) == DialogResult.OK ? dialog.FileName : null;
        }
    }
}

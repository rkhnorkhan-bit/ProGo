using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace ProGo
{
    public sealed class UpdateDownloadCancelledException : OperationCanceledException
    {
        public UpdateDownloadCancelledException() : base("Скачивание отменено до замены файлов ProGo.") { }
    }

    // The PowerShell transaction retains its owning thread/mutex. Only this local
    // presentation runs on another STA thread; no script runs on the UI thread.
    public sealed class UpdateInstallSession : IDisposable
    {
        private readonly object gate = new object();
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        private readonly ManualResetEventSlim ready = new ManualResetEventSlim();
        private Thread thread;
        private UpdateInstallForm window;
        private Exception startupError;
        private bool cancelable, requested, finished;
        private string title = "Подготавливаю обновление", detail = "Дождитесь начала скачивания. Файлы ещё не заменяются.";
        public CancellationToken Token { get { return cancellation.Token; } }
        public bool CanCancel { get { lock (gate) return cancelable && !requested && !finished; } }
        public bool CancellationRequested { get { lock (gate) return requested; } }
        internal bool Finished { get { lock (gate) return finished; } }
        internal UpdateInstallForm Window { get { lock (gate) return window; } }

        public static UpdateInstallSession Open()
        {
            var session = new UpdateInstallSession();
            session.thread = new Thread(session.RunWindow) { IsBackground = true, Name = "ProGo update progress" };
            session.thread.SetApartmentState(ApartmentState.STA);
            session.thread.Start();
            if (!session.ready.Wait(3000) || session.startupError != null) {
                session.Dispose();
                throw new InvalidOperationException("Не удалось открыть окно обновления. Файлы ProGo не изменены.");
            }
            return session;
        }
        private void RunWindow()
        {
            try {
                Application.EnableVisualStyles();
                using (var form = new UpdateInstallForm(this)) {
                    lock (gate) window = form;
                    form.Shown += delegate { Refresh(form); ready.Set(); if (Finished) form.Close(); };
                    Application.Run(form);
                }
            }
            catch (Exception error) { startupError = error; RequestCancellation(); }
            finally { ready.Set(); lock (gate) window = null; }
        }
        public void ShowPhase(string phaseTitle, string explanation)
        {
            lock (gate) { title = phaseTitle; detail = explanation; }
            Render();
        }
        public void BeginDownload()
        {
            lock (gate) {
                if (finished) throw new ObjectDisposedException("UpdateInstallSession");
                cancelable = true;
            }
            Render();
        }
        public void EndDownload()
        {
            // Serialize the cancellation/validation boundary, independently of
            // rendering. A stale enabled button cannot cancel commit or rollback.
            lock (gate) {
                if (requested) throw new OperationCanceledException(Token);
                cancelable = false;
            }
            Render();
        }
        public bool RequestCancellation()
        {
            lock (gate) {
                if (!cancelable || requested || finished) return false;
                requested = true;
            }
            Render();
            cancellation.Cancel();
            return true;
        }
        private void Render()
        {
            var form = Window;
            if (form == null || form.IsDisposed || !form.IsHandleCreated) return;
            try { form.BeginInvoke(new Action(delegate { if (!form.IsDisposed) Refresh(form); })); }
            catch (InvalidOperationException) { /* Presentation cannot break a transaction. */ }
        }
        private void Refresh(UpdateInstallForm form)
        {
            lock (gate) form.ShowPhase(requested ? "Отменяю скачивание…" : title,
                requested ? "Завершаю загрузку и удаляю неполный архив. Файлы ProGo не заменяются; дождитесь результата." : detail,
                cancelable && !requested && !finished);
        }
        public void Dispose()
        {
            lock (gate) { if (finished) return; finished = true; cancelable = false; }
            var form = Window;
            if (form != null && form.IsHandleCreated && !form.IsDisposed)
                try { form.BeginInvoke(new Action(form.Close)); } catch (InvalidOperationException) { }
            // Called after the transfer has unwound; never abort the transaction.
            // If a host refuses UI closure, maintenance cleanup still completes.
            if (thread == null || thread.Join(5000)) { cancellation.Dispose(); ready.Dispose(); }
        }
    }

    internal sealed class UpdateInstallForm : ProGoForm
    {
        private readonly UpdateInstallSession session;
        internal readonly Label Status;
        internal readonly TextBox Explanation;
        internal readonly Button CancelDownload;
        private readonly ProgressBar progress;
        internal UpdateInstallForm(UpdateInstallSession owner)
        {
            session = owner;
            Text = "Установка обновления ProGo";
            ClientSize = new Size(640, 340); MinimumSize = new Size(560, 300);
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 1, RowCount = 4 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Status = new Label { AutoSize = true, Dock = DockStyle.Fill, Font = UiTheme.Heading,
                AccessibleName = "Этап установки обновления", Margin = new Padding(0, 0, 0, 12) };
            progress = new ProgressBar { Dock = DockStyle.Fill, Height = 18, Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30, AccessibleName = "Выполняется обновление", Margin = new Padding(0, 0, 0, 12) };
            Explanation = new TextBox { Multiline = true, ReadOnly = true, WordWrap = true, ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill, AccessibleName = "Пояснение к этапу обновления", TabIndex = 0 };
            CancelDownload = UiTheme.Button("Отменить скачивание", delegate { session.RequestCancellation(); }, false);
            CancelDownload.Enabled = false;
            CancelDownload.AccessibleDescription = "Отменяет только скачивание архива. Во время проверки, установки и восстановления отмена недоступна.";
            var actions = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, TabIndex = 1,
                WrapContents = true, Margin = new Padding(0, 12, 0, 0) };
            actions.Controls.Add(CancelDownload);
            layout.Controls.Add(Status, 0, 0); layout.Controls.Add(progress, 0, 1);
            layout.Controls.Add(Explanation, 0, 2); layout.Controls.Add(actions, 0, 3);
            Controls.Add(layout); CancelButton = CancelDownload; AcceptButton = null;
            Status.TextChanged += delegate { Status.AccessibleDescription = Status.Text; };
            layout.SizeChanged += delegate { Status.MaximumSize = new Size(Math.Max(200, layout.ClientSize.Width - layout.Padding.Horizontal), 0); };
            UiTheme.ConfigureKeyboardOrder(this);
        }
        internal void ShowPhase(string title, string detail, bool canCancel)
        {
            Status.Text = title; Explanation.Text = detail; Explanation.AccessibleDescription = detail;
            CancelDownload.Enabled = canCancel;
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!session.Finished) { e.Cancel = true; session.RequestCancellation(); }
            base.OnFormClosing(e);
        }
    }
}

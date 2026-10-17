using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProGo
{
    // The constructor thread owns the handle even when a modal dialog removes
    // SynchronizationContext. Disposal releases queued completions without UI.
    internal sealed class HomeProfileUiDispatcher : IDisposable
    {
        private readonly Control handle = new Control();
        private readonly Form owner;
        private readonly int thread = Thread.CurrentThread.ManagedThreadId;
        private readonly object gate = new object();
        private readonly HashSet<TaskCompletionSource<bool>> pending = new HashSet<TaskCompletionSource<bool>>();
        private bool closed;
        internal HomeProfileUiDispatcher(Form owner)
        {
            this.owner = owner; handle.CreateControl();
            owner.FormClosed += delegate { Dispose(); };
            owner.Disposed += delegate { Dispose(); };
        }
        internal Task<bool> DispatchAsync(Action action)
        {
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (gate) { if (closed) return Task.FromResult(false); pending.Add(completion); }
            Action run = delegate {
                try {
                    lock (gate) { if (closed) { completion.TrySetResult(false); return; } }
                    if (owner.IsDisposed || owner.Disposing) { completion.TrySetResult(false); return; }
                    // A callback may intentionally close its owner. Its own
                    // acceptance must settle before Dispose drops queued work.
                    lock (gate) pending.Remove(completion);
                    action(); completion.TrySetResult(true);
                } catch (Exception ex) { completion.TrySetException(ex); }
                finally { lock (gate) pending.Remove(completion); }
            };
            try { if (Thread.CurrentThread.ManagedThreadId == thread) run(); else handle.BeginInvoke(run); }
            catch (InvalidOperationException) { lock (gate) pending.Remove(completion); completion.TrySetResult(false); }
            return completion.Task;
        }
        internal void Post(Action action) { Observe(DispatchAsync(action)); }
        private static async void Observe(Task<bool> task) { try { await task.ConfigureAwait(false); } catch (Exception) { } }
        public void Dispose()
        {
            TaskCompletionSource<bool>[] completions;
            lock (gate) {
                if (closed) return; closed = true;
                completions = new TaskCompletionSource<bool>[pending.Count]; pending.CopyTo(completions); pending.Clear();
            }
            foreach (var completion in completions) completion.TrySetResult(false);
            handle.Dispose();
        }
    }

    internal sealed class HomeProfileHttpWaitForm : ProGoForm
    {
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        private readonly HomeProfileUiDispatcher dispatcher;
        private readonly Func<CancellationToken, Task> work;
        private readonly Label status = UiTheme.StatusLabel(UiTheme.Muted);
        private readonly Button cancel = new Button { Text = "Прервать ожидание HTTPS", AutoSize = true, MinimumSize = new Size(230, 38) };
        private readonly TaskCompletionSource<object> completion = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        private volatile bool running = true;
        private bool started;
        internal Task Completion { get { return completion.Task; } }
        internal HomeProfileHttpWaitForm(string title, Func<CancellationToken, Task> work)
        {
            this.work = work; dispatcher = new HomeProfileUiDispatcher(this);
            Text = title; ClientSize = new Size(610, 275); MinimumSize = new Size(450, 300);
            AutoScaleMode = AutoScaleMode.Dpi; StartPosition = FormStartPosition.CenterParent;
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), ColumnCount = 1, RowCount = 2 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            status.Text = "Проверка HTTPS и создание QR вместе занимают до 20 секунд. Можно прервать локальное ожидание; VPS мог уже создать ссылку и заменить прежнюю. ProGo не повторяет запрос автоматически.";
            status.AutoSize = true; status.MaximumSize = new Size(565, 0);
            layout.SizeChanged += delegate { status.MaximumSize = new Size(Math.Max(1, layout.ClientSize.Width - layout.Padding.Horizontal), 0); };
            HomeProfileShare.DescribeStatus(status, "Ход HTTPS-запроса");
            cancel.AccessibleDescription = "Прерывает собственный локальный HTTPS-запрос и ждёт его завершения. Не откатывает изменения VPS и не повторяет запрос.";
            cancel.Click += delegate { CancelWait(); }; CancelButton = cancel;
            layout.Controls.Add(status, 0, 0); layout.Controls.Add(cancel, 0, 1); Controls.Add(layout);
            UiTheme.ConfigureKeyboardOrder(this);
        }
        protected override async void OnShown(EventArgs e)
        { base.OnShown(e); if (started || IsDisposed || Disposing) return; started = true; cancel.Focus(); await Execute().ConfigureAwait(false); }
        private async Task Execute()
        {
            Exception failure = null;
            try { cancellation.Token.ThrowIfCancellationRequested(); await work(cancellation.Token).ConfigureAwait(false); }
            catch (Exception ex) { failure = ex; }
            bool published = await dispatcher.DispatchAsync(delegate {
                if (cancellation.IsCancellationRequested) failure = failure as HomeProfileHttpCancelledException ?? Cancelled();
                running = false; cancel.Enabled = false;
                if (failure == null) completion.TrySetResult(null); else completion.TrySetException(failure);
                Close();
            }).ConfigureAwait(false);
            if (!published) { running = false; completion.TrySetException(failure ?? Cancelled()); }
            cancellation.Dispose();
        }
        private static HomeProfileHttpCancelledException Cancelled()
        { return new HomeProfileHttpCancelledException("Ожидание создания QR прервано. VPS мог создать ссылку и заменить прежнюю. ProGo не повторяет запрос автоматически."); }
        private void CancelWait()
        {
            if (!running || cancellation.IsCancellationRequested) return;
            cancel.Enabled = false; status.Text = "Прерываем локальный HTTPS-запрос. Дождитесь завершения; результат VPS может остаться неподтверждённым.";
            cancellation.Cancel();
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        { if (running) { e.Cancel = true; CancelWait(); } base.OnFormClosing(e); }
        protected override void Dispose(bool disposing)
        {
            if (disposing) {
                if (running) {
                    cancellation.Cancel();
                    if (!started) { running = false; completion.TrySetException(Cancelled()); cancellation.Dispose(); }
                }
                dispatcher.Dispose();
            }
            base.Dispose(disposing);
        }
        internal static async Task<T> WaitAsync<T>(Form owner, string title, Func<CancellationToken, Task<T>> work)
        {
            T result = default(T);
            var dialog = new HomeProfileHttpWaitForm(title, async token => { result = await work(token).ConfigureAwait(false); });
            EventHandler ownerDisposed = delegate { dialog.Dispose(); };
            if (owner != null) owner.Disposed += ownerDisposed;
            try { dialog.ShowDialog(owner); }
            finally {
                if (owner != null) owner.Disposed -= ownerDisposed;
                // Dispose on the constructor/UI thread before awaiting a worker.
                dialog.Dispose();
            }
            await dialog.Completion.ConfigureAwait(false); return result;
        }
    }
}

using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProGo
{
    internal sealed class HomeVpnPreparationCancelledException : OperationCanceledException
    {
        internal HomeVpnPreparationCancelledException()
            : base("Подготовка отменена. Команды настройки VPS не запускались. Можно повторить подготовку.") { }
    }

    internal sealed class HomeVpnPreparationForm : ProGoForm
    {
        private readonly Label status = UiTheme.StatusLabel(UiTheme.Muted);
        private readonly Button cancel = new Button { Text = "Отменить подготовку", AutoSize = true, MinimumSize = new Size(220, 38) };
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        private readonly TaskCompletionSource<object> completion = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly Func<CancellationToken, Task> copy;
        private bool running = true, started;
        internal Task Completion { get { return completion.Task; } }

        internal HomeVpnPreparationForm(Func<CancellationToken, Task> copy)
        {
            this.copy = copy;
            Text = "Подготовка VPS"; ClientSize = new Size(610, 280); MinimumSize = new Size(450, 300);
            AutoScaleMode = AutoScaleMode.Dpi; StartPosition = FormStartPosition.CenterParent;
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), ColumnCount = 1, RowCount = 3 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var heading = new Label { Text = "Копирование помощника на VPS", Font = UiTheme.Heading, AutoSize = true, Margin = new Padding(0, 0, 0, 12) };
            status.Text = "Ожидание — до 5 минут. Если SSH запросит пароль или подтверждение ключа, ответьте в отдельном окне. Команды настройки VPS ещё не запускались. Можно отменить подготовку.";
            status.AutoSize = true; status.Margin = new Padding(0, 0, 0, 12);
            status.AccessibleName = "Ход подготовки VPS"; status.AccessibleDescription = status.Text;
            status.TextChanged += delegate { status.AccessibleDescription = status.Text; };
            var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Margin = Padding.Empty }; scroll.Controls.Add(status);
            Action wrap = delegate {
                heading.MaximumSize = new Size(Math.Max(1, layout.ClientSize.Width - layout.Padding.Horizontal), 0);
                status.MaximumSize = new Size(Math.Max(1, scroll.ClientSize.Width - SystemInformation.VerticalScrollBarWidth), 0);
            };
            layout.SizeChanged += delegate { wrap(); }; scroll.SizeChanged += delegate { wrap(); };
            cancel.AccessibleName = cancel.Text;
            cancel.AccessibleDescription = "Прерывает только копирование. Окно дождётся остановки его процессов; команды настройки VPS не запускаются.";
            cancel.Click += delegate { CancelCopy(); }; CancelButton = cancel;
            layout.Controls.Add(heading, 0, 0); layout.Controls.Add(scroll, 0, 1); layout.Controls.Add(cancel, 0, 2); Controls.Add(layout);
            UiTheme.ConfigureKeyboardOrder(this);
        }
        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e); if (started) return; started = true; cancel.Focus();
            await Execute();
        }
        private async Task Execute()
        {
            Exception failure = null;
            try { cancellation.Token.ThrowIfCancellationRequested(); await copy(cancellation.Token); cancellation.Token.ThrowIfCancellationRequested(); }
            catch (OperationCanceledException) {
                if (cancellation.IsCancellationRequested) failure = new HomeVpnPreparationCancelledException();
                else failure = new InvalidOperationException("Подготовка прервана. Команды настройки VPS не запускались; проверьте SSH.");
            }
            catch (Exception ex) { failure = ex; }
            finally { running = false; cancellation.Dispose(); }
            if (failure == null) completion.TrySetResult(null); else completion.TrySetException(failure);
            if (!IsDisposed && !Disposing) { cancel.Enabled = false; Close(); }
        }
        private void CancelCopy()
        {
            if (!running || cancellation.IsCancellationRequested) return;
            if (!IsDisposed && !Disposing) {
                cancel.Enabled = false;
                status.Text = "Останавливаем копирование и его процессы. Дождитесь результата. Команды настройки VPS не запускались.";
            }
            cancellation.Cancel();
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (running) { e.Cancel = true; CancelCopy(); }
            base.OnFormClosing(e);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing && running) {
                CancelCopy();
                // A never-shown dialog has no worker that could release this source.
                if (!started) { running = false; cancellation.Dispose(); completion.TrySetException(new HomeVpnPreparationCancelledException()); }
            }
            base.Dispose(disposing);
        }
        internal static async Task CopyAsync(Form owner, string executable, string arguments)
        {
            using (var dialog = new HomeVpnPreparationForm(token => HomeVpnPreparationProcess.CopyAsync(executable, arguments, HomeVpnPreparationProcess.TimeoutMs, token))) {
                dialog.ShowDialog(owner); await dialog.Completion;
            }
        }
    }
}

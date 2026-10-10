using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProGo
{
    internal sealed class HomeVpnSetupWaitCancelledException : OperationCanceledException
    {
        internal HomeVpnSetupWaitCancelledException()
            : base("Ожидание SSH остановлено. Запрос настройки VPS сохранён; команда могла завершиться или прерваться. Нажмите «Проверить прошлую настройку». Новый доступ не запрашивается.") { }
    }

    internal sealed class HomeVpnSetupWaitForm : ProGoForm
    {
        private readonly Label heading = UiTheme.Label("Ожидание ответа VPS", UiTheme.Heading, UiTheme.Text);
        private readonly Label status = UiTheme.StatusLabel(UiTheme.Muted);
        private readonly FlowLayoutPanel viewport = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true,
            FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = Padding.Empty };
        private readonly Button cancel = new Button { Text = "Остановить ожидание", AutoSize = true, MinimumSize = new Size(220, 38) };
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        private readonly TaskCompletionSource<object> completion = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly Func<CancellationToken, Task> wait;
        private bool running = true, started;
        internal Task Completion { get { return completion.Task; } }

        internal HomeVpnSetupWaitForm(Func<CancellationToken, Task> wait)
        {
            this.wait = wait;
            Text = "Ответ настройки VPS"; ClientSize = new Size(610, 310); MinimumSize = new Size(450, 300);
            AutoScaleMode = AutoScaleMode.Dpi; StartPosition = FormStartPosition.CenterParent;
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), ColumnCount = 1, RowCount = 2 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            heading.Margin = new Padding(0, 0, 0, 12);
            status.Text = "Один ответ SSH ожидается не более 10 минут. Если SSH спросит пароль или подтверждение ключа, ответьте в отдельном окне. Можно остановить ожидание на этом ПК. Это не подтверждает отмену команды VPS: сохранённый запрос позволит проверить её результат без повторной выдачи доступа.";
            status.AutoSize = true; status.Margin = new Padding(0, 0, 0, 12);
            status.AccessibleName = "Ожидание результата настройки VPS"; status.AccessibleDescription = status.Text;
            status.TextChanged += delegate { status.AccessibleDescription = status.Text; };
            viewport.Controls.Add(heading); viewport.Controls.Add(status);
            viewport.SizeChanged += delegate {
                int width = Math.Max(1, viewport.ClientSize.Width - SystemInformation.VerticalScrollBarWidth);
                heading.MaximumSize = status.MaximumSize = new Size(width, 0);
            };
            cancel.AccessibleName = cancel.Text;
            cancel.AccessibleDescription = "Останавливает только ожидание и локальные процессы SSH ProGo. Окно дождётся их завершения. Запрос VPS остаётся сохранённым; результат сервера нужно проверить.";
            cancel.Click += delegate { CancelWait(); }; CancelButton = cancel; cancel.DialogResult = DialogResult.None;
            layout.Controls.Add(viewport, 0, 0); layout.Controls.Add(cancel, 0, 1); Controls.Add(layout);
            UiTheme.ConfigureKeyboardOrder(this);
        }
        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e); if (started || IsDisposed || Disposing) return; started = true; cancel.Focus();
            Exception failure = null;
            try { cancellation.Token.ThrowIfCancellationRequested(); await wait(cancellation.Token); cancellation.Token.ThrowIfCancellationRequested(); }
            catch (OperationCanceledException) {
                failure = cancellation.IsCancellationRequested ? (Exception)new HomeVpnSetupWaitCancelledException()
                    : new HomeVpnSetupPendingException(HomeVpnSetupRecovery.PendingMessage);
            }
            catch (Exception ex) { failure = ex; }
            finally { running = false; cancellation.Dispose(); }
            if (failure == null) completion.TrySetResult(null); else completion.TrySetException(failure);
            if (!IsDisposed && !Disposing) { cancel.Enabled = false; Close(); }
        }
        private void CancelWait()
        {
            if (!running || cancellation.IsCancellationRequested) return;
            if (!IsDisposed && !Disposing) {
                cancel.Enabled = false;
                status.Text = "Останавливаем ожидание и локальные процессы SSH. Дождитесь подтверждения остановки. Запрос VPS сохранён; состояние команды на сервере ещё нужно проверить.";
            }
            cancellation.Cancel();
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (running) { e.Cancel = true; CancelWait(); }
            base.OnFormClosing(e);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing && running) {
                CancelWait();
                if (!started) { running = false; cancellation.Dispose(); completion.TrySetException(new HomeVpnSetupWaitCancelledException()); }
            }
            base.Dispose(disposing);
        }
        internal static async Task WaitAsync(Form owner, string executable, string arguments, string output)
        {
            using (var dialog = new HomeVpnSetupWaitForm(token => HomeVpnSetupWaitProcess.CommandAsync(executable, arguments, output, HomeVpnSetupWaitProcess.TimeoutMs, token))) {
                dialog.ShowDialog(owner); await dialog.Completion;
            }
        }
    }
}

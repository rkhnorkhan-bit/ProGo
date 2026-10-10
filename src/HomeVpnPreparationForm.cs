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
            : this(false) { }
        internal HomeVpnPreparationCancelledException(bool recovery)
            : base(recovery ? "Подготовка проверки отменена. Прежняя команда VPS могла завершиться; её запрос сохранён. Можно повторить проверку."
                : "Подготовка отменена. Команды настройки VPS не запускались. Можно повторить подготовку.") { }
    }

    internal sealed class HomeVpnListCancelledException : OperationCanceledException
    {
        internal HomeVpnListCancelledException()
            : base("Получение списка отменено. Существующий доступ, текущий список и введённые данные сохранены. Можно повторить получение списка.") { }
    }

    internal sealed class HomeVpnPreparationForm : ProGoForm
    {
        private readonly Label status = UiTheme.StatusLabel(UiTheme.Muted);
        private readonly Label heading = UiTheme.Label("Копирование помощника на VPS", UiTheme.Heading, UiTheme.Text);
        private readonly FlowLayoutPanel viewport = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true,
            FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = Padding.Empty };
        private readonly Button cancel = new Button { Text = "Отменить подготовку", AutoSize = true, MinimumSize = new Size(220, 38) };
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        private readonly TaskCompletionSource<object> completion = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly Func<CancellationToken, Task> copy;
        private readonly bool recovery, list;
        private readonly bool serverWait;
        private bool running = true, started;
        internal Task Completion { get { return completion.Task; } }

        internal HomeVpnPreparationForm(Func<CancellationToken, Task> copy) : this(copy, false) { }
        internal HomeVpnPreparationForm(Func<CancellationToken, Task> copy, bool recovery) : this(copy, recovery, false) { }
        internal HomeVpnPreparationForm(Func<CancellationToken, Task> copy, bool recovery, bool serverWait)
            : this(copy, serverWait ? HomeVpnWaitPurpose.SetupCommand : recovery ? HomeVpnWaitPurpose.RecoveryCopy : HomeVpnWaitPurpose.Copy) { }
        internal HomeVpnPreparationForm(Func<CancellationToken, Task> copy, HomeVpnWaitPurpose purpose)
        {
            this.copy = copy; recovery = purpose == HomeVpnWaitPurpose.RecoveryCopy || purpose == HomeVpnWaitPurpose.SetupCommand;
            serverWait = purpose == HomeVpnWaitPurpose.SetupCommand || purpose == HomeVpnWaitPurpose.ListCommand;
            list = purpose == HomeVpnWaitPurpose.ListCopy || purpose == HomeVpnWaitPurpose.ListCommand;
            Text = recovery ? "Подготовка проверки VPS" : "Подготовка VPS"; ClientSize = new Size(610, 280); MinimumSize = new Size(450, 300);
            if (recovery) heading.Text = "Подготовка проверки VPS";
            AutoScaleMode = AutoScaleMode.Dpi; StartPosition = FormStartPosition.CenterParent;
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), ColumnCount = 1, RowCount = 2 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            heading.Margin = new Padding(0, 0, 0, 12);
            status.Text = recovery ? "Ожидание — до 5 минут. Если SSH запросит пароль или подтверждение ключа, ответьте в отдельном окне. Прежняя команда VPS могла завершиться. Можно отменить копирование; её запрос сохранён."
                : "Ожидание — до 5 минут. Если SSH запросит пароль или подтверждение ключа, ответьте в отдельном окне. Команды настройки VPS ещё не запускались. Можно отменить подготовку.";
            status.AutoSize = true; status.Margin = new Padding(0, 0, 0, 12);
            status.AccessibleName = "Ход подготовки VPS"; status.AccessibleDescription = status.Text;
            status.TextChanged += delegate { status.AccessibleDescription = status.Text; };
            viewport.Controls.Add(heading); viewport.Controls.Add(status);
            viewport.SizeChanged += delegate {
                int width = Math.Max(1, viewport.ClientSize.Width - SystemInformation.VerticalScrollBarWidth);
                heading.MaximumSize = status.MaximumSize = new Size(width, 0);
            };
            cancel.AccessibleName = cancel.Text;
            cancel.AccessibleDescription = recovery ? "Прерывает только копирование для проверки. Окно дождётся остановки его процессов; прежний запрос VPS сохранён, его результат ещё не подтверждён."
                : "Прерывает только копирование. Окно дождётся остановки его процессов; команды настройки VPS не запускаются.";
            if (serverWait) {
                Text = "Ожидание команды VPS"; heading.Text = "Ожидание результата SSH";
                status.Text = "Ожидание — до 5 минут. Ответьте на запрос SSH в отдельном окне. Можно прервать локальное ожидание: команда VPS могла уже применить изменения. Запрос сохранён; затем проверьте прежнюю настройку.";
                cancel.Text = "Прервать ожидание SSH"; cancel.AccessibleName = cancel.Text;
                cancel.AccessibleDescription = "Останавливает только локальные процессы ожидания. Не отменяет изменения VPS. Запрос сохранён для проверки исходного результата.";
            }
            if (list) {
                Text = serverWait ? "Получение списка с VPS" : "Подготовка получения списка";
                heading.Text = serverWait ? "Получение списка друзей" : "Подготовка получения списка";
                status.Text = "Ожидание — до 5 минут. Если SSH запросит пароль или подтверждение ключа, ответьте в отдельном окне. Получение списка не создаёт и не отзывает доступ. Можно отменить ожидание; текущий список и введённые данные сохранятся.";
                status.AccessibleName = "Ход получения списка друзей";
                cancel.Text = "Отменить получение списка"; cancel.AccessibleName = cancel.Text;
                cancel.AccessibleDescription = "Останавливает только локальные процессы получения списка. Существующий доступ, текущий список и введённые данные сохраняются. Новые токены не создаются.";
            }
            cancel.Click += delegate { CancelCopy(); }; CancelButton = cancel; cancel.DialogResult = DialogResult.None;
            layout.Controls.Add(viewport, 0, 0); layout.Controls.Add(cancel, 0, 1); Controls.Add(layout);
            UiTheme.ConfigureKeyboardOrder(this);
        }
        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e); if (started || IsDisposed || Disposing) return; started = true; cancel.Focus();
            await Execute();
        }
        private async Task Execute()
        {
            Exception failure = null;
            try { cancellation.Token.ThrowIfCancellationRequested(); await copy(cancellation.Token); cancellation.Token.ThrowIfCancellationRequested(); }
            catch (OperationCanceledException) {
                if (list) failure = cancellation.IsCancellationRequested ? (Exception)new HomeVpnListCancelledException()
                    : new InvalidOperationException("Получение списка прервано. Существующий доступ и текущий список сохранены; проверьте SSH и повторите получение.");
                else if (serverWait) failure = new HomeVpnSetupPendingException("Ожидание SSH прервано. Команда VPS могла завершиться; запрос сохранён. Проверьте прежнюю настройку, не запускайте её повторно.");
                else if (cancellation.IsCancellationRequested) failure = new HomeVpnPreparationCancelledException(recovery);
                else failure = new InvalidOperationException(recovery ? "Подготовка проверки прервана. Прежний запрос VPS сохранён; проверьте SSH."
                    : "Подготовка прервана. Команды настройки VPS не запускались; проверьте SSH.");
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
                status.Text = list ? "Останавливаем локальные процессы получения списка. Дождитесь результата. Существующий доступ, текущий список и введённые данные сохраняются."
                    : serverWait ? "Останавливаем локальные процессы SSH. Дождитесь результата. Команда VPS могла завершиться; запрос сохранён для проверки."
                    : recovery ? "Останавливаем копирование и его процессы. Дождитесь результата. Прежний запрос VPS сохранён; его результат ещё не проверен."
                    : "Останавливаем копирование и его процессы. Дождитесь результата. Команды настройки VPS не запускались.";
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
                if (!started) { running = false; cancellation.Dispose(); completion.TrySetException(list ? (Exception)new HomeVpnListCancelledException()
                    : serverWait ? (Exception)new HomeVpnSetupPendingException(HomeVpnSetupRecovery.PendingMessage) : new HomeVpnPreparationCancelledException(recovery)); }
            }
            base.Dispose(disposing);
        }
        internal static async Task WaitForCommandAsync(Form owner, string executable, string arguments, string output)
        {
            using (var dialog = new HomeVpnPreparationForm(token => HomeVpnPreparationProcess.CommandAsync(executable, arguments, output, HomeVpnPreparationProcess.TimeoutMs, token), true, true)) {
                dialog.ShowDialog(owner); await dialog.Completion;
            }
        }
        internal static async Task CopyAsync(Form owner, string executable, string arguments, bool recovery = false)
        {
            using (var dialog = new HomeVpnPreparationForm(token => HomeVpnPreparationProcess.CopyAsync(executable, arguments, HomeVpnPreparationProcess.TimeoutMs, token), recovery)) {
                dialog.ShowDialog(owner); await dialog.Completion;
            }
        }
        internal static async Task WaitForListAsync(Form owner, string executable, string arguments, string output, int timeoutMs = HomeVpnPreparationProcess.TimeoutMs)
        {
            using (var dialog = new HomeVpnPreparationForm(token => HomeVpnPreparationProcess.ListAsync(executable, arguments, output, timeoutMs, token), HomeVpnWaitPurpose.ListCommand)) {
                dialog.ShowDialog(owner); await dialog.Completion;
            }
        }
        internal static async Task CopyForListAsync(Form owner, string executable, string arguments)
        {
            using (var dialog = new HomeVpnPreparationForm(token => HomeVpnPreparationProcess.CopyAsync(executable, arguments, HomeVpnPreparationProcess.TimeoutMs, token, true), HomeVpnWaitPurpose.ListCopy)) {
                dialog.ShowDialog(owner); await dialog.Completion;
            }
        }
    }
}

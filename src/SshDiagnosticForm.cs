using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProGo
{
    internal sealed class SshDiagnosticForm : ProGoForm
    {
        private readonly SshProfileSetting profile;
        private readonly Func<SshProfileSetting, CancellationToken, SshProfileDiagnosticResult> check;
        private readonly Label status = UiTheme.Label("", UiTheme.Body, UiTheme.Muted);
        private readonly TextBox report = new TextBox { Name = "SshDiagnosticReport", ReadOnly = true, Multiline = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Vertical, WordWrap = true };
        private readonly Button retry, cancel;
        private CancellationTokenSource cancellation;
        private readonly TaskCompletionSource<bool> ownerClosed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        private bool busy, closing;
        internal Task Work { get; private set; }
        internal Task CheckWork { get; private set; }
        internal SshDiagnosticForm(SshProfileSetting selected, Func<SshProfileSetting, CancellationToken, SshProfileDiagnosticResult> check = null)
        {
            profile = selected.Clone(); this.check = check ?? ((p, token) => SshProfileDiagnostics.Check(p, token));
            CheckWork = Task.FromResult(false);
            Text = "Проверка настроек SSH · ProGo"; ClientSize = new Size(790, 550); MinimumSize = new Size(650, 420);
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = UiTheme.DialogPadding, ColumnCount = 1, RowCount = 4 };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
            root.Controls.Add(UiTheme.Label("Проверяем настройки подключения", UiTheme.Heading, UiTheme.Text), 0, 0); root.Controls.Add(report, 0, 1);
            status.AutoSize = false; status.Dock = DockStyle.Fill; root.Controls.Add(status, 0, 2);
            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
            cancel = UiTheme.Button("Отменить проверку", delegate { if (busy) CancelWork(); else Close(); }, false); cancel.Name = "SshDiagnosticCancel";
            retry = UiTheme.Button("Проверить ещё раз", delegate { Work = Run(); }, true); retry.Name = "SshDiagnosticRetry";
            actions.Controls.Add(cancel); actions.Controls.Add(retry); root.Controls.Add(actions, 0, 3); Controls.Add(root);
            report.AccessibleName = "Результат проверки настроек SSH";
            report.AccessibleDescription = "Только чтение. Настройки OpenSSH и локальная проверка; не подтверждает доступность сервера. Можно выделить и скопировать текст.";
            status.AccessibleName = "Состояние проверки SSH";
            status.TextChanged += delegate { status.AccessibleDescription = status.Text; };
            retry.AccessibleDescription = "Повторяет локальную проверку выбранного сервера. Во время проверки недоступна.";
            cancel.TextChanged += delegate { DescribeCancel(); };
            DescribeCancel();
            UiTheme.ConfigureKeyboardOrder(this);
            CancelButton = cancel;
            Shown += delegate { Work = Run(); cancel.Focus(); };
            FormClosing += delegate { closing = true; ownerClosed.TrySetResult(true); CancelWork(); };
        }
        private void DescribeCancel()
        {
            cancel.AccessibleName = cancel.Text;
            cancel.AccessibleDescription = cancel.Text == "Закрыть"
                ? "Закрывает результаты проверки. Повторная проверка не запускается."
                : "Прекращает ожидание и останавливает только процессы этой проверки. Системный запрос может завершиться позже. Окно остаётся открытым для повтора.";
        }
        private void CancelWork()
        {
            var source = cancellation;
            if (source == null) return;
            try { source.Cancel(); } catch (ObjectDisposedException) { }
            if (!closing) { status.Text = "Отменяем проверку и закрываем её процессы…"; cancel.Enabled = false; }
        }
        private async Task Run()
        {
            if (busy || closing) return;
            busy = true; retry.Enabled = false; cancel.Enabled = true; cancel.Text = "Отменить проверку";
            report.Clear(); status.Text = "Проверяем SSH и ключи без подключения к серверу. Лимиты фаз: SSH — 7 с; служба — 3 с; агент — 3 с; отпечатки — 3 с. Очистка своих процессов — до 2 с. Можно отменить.";
            var source = new CancellationTokenSource(); cancellation = source; var token = source.Token;
            try {
                string text = "", notice;
                try {
                    var checking = Task.Run(() => check(profile.Clone(), token)); CheckWork = checking;
                    var result = await checking.ConfigureAwait(false);
                    token.ThrowIfCancellationRequested();
                    text = result.ToReport();
                    notice = result.SshResolved ? "Настройки SSH прочитаны. Доступность сервера проверяется подключением." : "Настройки SSH не проверены. Причина указана выше.";
                }
                catch (OperationCanceledException) { notice = "Проверка отменена. Можно повторить."; }
                catch { text = "Проверка не завершена. Проверьте OpenSSH и права доступа к настройкам."; notice = "Не удалось завершить проверку. Можно повторить."; }
                await Publish(delegate {
                    if (token.IsCancellationRequested) { report.Clear(); status.Text = "Проверка отменена. Можно повторить."; }
                    else { report.Text = text; status.Text = notice; }
                    cancellation = null; busy = false;
                    retry.Enabled = cancel.Enabled = true; cancel.Text = "Закрыть";
                }).ConfigureAwait(false);
            }
            finally {
                Interlocked.CompareExchange(ref cancellation, null, source); source.Dispose();
            }
        }
        private async Task<bool> Publish(Action action)
        {
            if (ownerClosed.Task.IsCompleted) return false;
            var delivered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            try {
                BeginInvoke(new Action(delegate {
                    try { if (!closing && !IsDisposed) { action(); delivered.TrySetResult(true); } else delivered.TrySetResult(false); }
                    catch (Exception ex) { delivered.TrySetException(ex); var ignored = delivered.Task.Exception; }
                }));
            } catch (InvalidOperationException) { return false; }
            if (await Task.WhenAny(delivered.Task, ownerClosed.Task).ConfigureAwait(false) != delivered.Task) return false;
            return await delivered.Task.ConfigureAwait(false);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) { closing = true; ownerClosed.TrySetResult(true); CancelWork(); }
            base.Dispose(disposing);
        }
    }
}

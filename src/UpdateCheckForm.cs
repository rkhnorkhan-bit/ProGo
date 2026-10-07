using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProGo
{
    internal sealed class UpdateCheckForm : ProGoForm
    {
        private readonly Func<CancellationToken, Task<UpdateCheckResult>> check;
        private readonly Label status, version, notice;
        private readonly TextBox details;
        private readonly ProgressBar progress;
        private readonly Button primary, close;
        private CancellationTokenSource running;
        private long generation;
        private UpdateCheckResult offered;
        internal UpdateCheckResult AcceptedResult { get; private set; }
        internal Task CheckWork { get; private set; }
        internal bool IsChecking { get { return running != null; } }

        internal UpdateCheckForm(Func<CancellationToken, Task<UpdateCheckResult>> checker = null, string localVersion = null)
        {
            check = checker ?? UpdateLauncher.CheckForUpdateAsync;
            Text = "Обновление ProGo"; ClientSize = new Size(640, 440); MinimumSize = new Size(560, 380);
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 1, RowCount = 6 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < 6; i++) layout.RowStyles.Add(new RowStyle(i == 4 ? SizeType.Percent : SizeType.AutoSize, i == 4 ? 100 : 0));
            status = new Label { AutoSize = true, Dock = DockStyle.Fill, Font = UiTheme.Strong, AccessibleName = "Состояние проверки обновлений", Margin = new Padding(0, 0, 0, 8) };
            version = new Label { AutoSize = true, Dock = DockStyle.Fill, Text = "Установлена версия: " + (localVersion ?? UpdateLauncher.ReadInstalledVersion()), AccessibleName = "Версии ProGo", Margin = new Padding(0, 0, 0, 8) };
            notice = new Label { AutoSize = true, Dock = DockStyle.Fill, AccessibleName = "Следующее действие с обновлением", Margin = new Padding(0, 0, 0, 10) };
            progress = new ProgressBar { Dock = DockStyle.Fill, Height = 18, Style = ProgressBarStyle.Marquee, MarqueeAnimationSpeed = 30, AccessibleName = "Выполняется проверка обновлений", Margin = new Padding(0, 0, 0, 10) };
            details = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, WordWrap = true, ScrollBars = ScrollBars.Vertical,
                AccessibleName = "Результат проверки и что нового", AccessibleDescription = "Только чтение. Описание релиза показано как обычный текст; ссылки и команды не выполняются.", TabIndex = 0, Margin = new Padding(0) };
            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, Margin = new Padding(0, 12, 0, 0), TabIndex = 1 };
            primary = UiTheme.Button("Повторить проверку", delegate {
                if (running != null) return;
                if (offered != null && offered.Availability == UpdateAvailability.Available) {
                    AcceptedResult = offered; DialogResult = DialogResult.OK; Close();
                } else StartCheck();
            }, true);
            close = UiTheme.Button("Отменить", delegate { CancelAndClose(); }, false);
            close.DialogResult = DialogResult.Cancel;
            actions.Controls.Add(primary); actions.Controls.Add(close);
            layout.Controls.Add(status, 0, 0); layout.Controls.Add(version, 0, 1); layout.Controls.Add(notice, 0, 2);
            layout.Controls.Add(progress, 0, 3); layout.Controls.Add(details, 0, 4); layout.Controls.Add(actions, 0, 5);
            Controls.Add(layout); CancelButton = close; AcceptButton = null;
            foreach (var label in new[] { status, version, notice }) {
                label.TextChanged += delegate(object sender, EventArgs e) { var current = (Label)sender; current.AccessibleDescription = current.Text; };
                label.AccessibleDescription = label.Text;
            }
            layout.SizeChanged += delegate { foreach (var label in new[] { status, version, notice }) label.MaximumSize = new Size(Math.Max(200, layout.ClientSize.Width - layout.Padding.Horizontal), 0); };
            SetChecking(); UiTheme.ConfigureKeyboardOrder(this);
            Shown += delegate { StartCheck(); close.Focus(); };
        }

        private void SetChecking()
        {
            offered = null; AcceptedResult = null;
            status.Text = "Проверяю обновления…";
            notice.Text = "Проверка занимает до 15 секунд. Её можно отменить.";
            details.Text = "Получаю сведения о последнем релизе ProGo на GitHub.";
            progress.Visible = true; primary.Visible = false; primary.Enabled = false;
            close.Text = "Отменить"; close.AccessibleDescription = "Отменяет только проверку обновлений и закрывает это окно. Подключения ProGo продолжают работать.";
        }
        private void StartCheck()
        {
            if (running != null || IsDisposed || Disposing) return;
            SetChecking(); close.Focus(); running = new CancellationTokenSource();
            CheckWork = RunCheck(running, ++generation);
        }
        private async Task RunCheck(CancellationTokenSource cancellation, long request)
        {
            UpdateCheckResult result = null;
            bool cancelled = false;
            try { result = await Task.Run(() => check(cancellation.Token)).ConfigureAwait(false); }
            catch (OperationCanceledException) { cancelled = true; }
            catch (Exception ex) { SafeLog.Error("Update check failed.", ex); }
            finally { cancellation.Dispose(); }
            if (IsDisposed || Disposing || !IsHandleCreated) return;
            try { BeginInvoke(new Action(delegate {
                if (IsDisposed || Disposing || request != generation) return;
                running = null;
                if (cancelled) { CancelAndClose(); return; }
                ShowResult(result);
            })); } catch (InvalidOperationException) { /* Window was closed after the check finished. */ }
        }
        private void ShowResult(UpdateCheckResult result)
        {
            progress.Visible = false; close.Text = "Закрыть";
            close.AccessibleDescription = "Закрывает результат проверки. Обновление не устанавливается; подключения ProGo продолжают работать.";
            if (result != null && !String.IsNullOrWhiteSpace(result.LocalVersion)) version.Text = "Установлена версия: " + result.LocalVersion;
            if (result == null || result.Availability == UpdateAvailability.Error) {
                status.Text = "Не удалось проверить обновления";
                notice.Text = "Проверьте подключение к GitHub и повторите попытку. Подробности — в журнале приложения.";
                details.Text = result == null ? "Не удалось получить сведения о релизе." : result.ErrorMessage ?? "Не удалось получить сведения о релизе.";
                primary.Text = "Повторить проверку";
                primary.AccessibleDescription = "Запускает новую проверку обновлений. Ничего не устанавливает.";
                primary.Visible = true; primary.Enabled = true;
            } else if (result.Availability == UpdateAvailability.UpToDate) {
                status.Text = "Установлена актуальная версия";
                notice.Text = "Обновление не требуется."; details.Text = "Новых версий ProGo на GitHub не найдено.";
                primary.Visible = false; primary.Enabled = false;
            } else {
                offered = result;
                status.Text = "Доступна версия " + result.RemoteVersion;
                version.Text += " · Новая версия: " + result.RemoteVersion;
                notice.Text = "Установка закроет ProGo и запустит его снова. Начать её можно кнопкой ниже.";
                details.Text = "Что нового\r\n\r\n" + UpdateLauncher.SummarizeReleaseNotes(result.ReleaseNotes).Replace("\n", "\r\n");
                primary.Text = "Установить обновление";
                primary.AccessibleDescription = "Подтверждает установку показанной новой версии. ProGo закроется и передаст управление установленному помощнику обновления.";
                primary.Visible = true; primary.Enabled = true;
                close.Text = "Позже";
            }
            // Completion must not move focus onto an installation action.
            UiTheme.ConfigureKeyboardOrder(this);
        }
        internal void CancelAndClose()
        {
            DialogResult = DialogResult.Cancel; AcceptedResult = null; Close();
        }
        private void CancelPending()
        {
            generation++; var pending = running; running = null;
            if (pending != null) try { pending.Cancel(); } catch (ObjectDisposedException) { }
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (!e.Cancel) CancelPending();
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) CancelPending();
            base.Dispose(disposing);
        }
    }
}

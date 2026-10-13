using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProGo
{
    // The raw worker is owned independently of modal/UI completion. Native file
    // calls may finish after cancellation; only their settled result may be adopted.
    internal sealed class VaultUnlockForm : ProGoForm
    {
        private const string Unavailable = "Не удалось открыть хранилище. Проверьте доступность файла и повторите попытку.";
        private const string WaitToStart = "Дождитесь завершения текущей операции ProGo и повторите попытку.";
        private readonly OwnerUiDispatcher ownerUi = new OwnerUiDispatcher();
        private readonly Func<CancellationToken, VaultProbeResult> probe;
        private readonly Func<bool, string, CancellationToken, VaultOpenResult> open;
        private readonly Func<bool> canStart, canPublish;
        private readonly Action<Task> workStarted;
        private readonly Func<DateTime> clock;
        private readonly Action<VaultSession, DateTime> expired;
        private readonly TextBox pin = new TextBox { UseSystemPasswordChar = true, Dock = DockStyle.Fill };
        private readonly TextBox confirm = new TextBox { UseSystemPasswordChar = true, Dock = DockStyle.Fill };
        private readonly Label confirmLabel, status;
        private readonly TableLayoutPanel layout;
        private readonly Button primary, cancel;
        private CancellationTokenSource cancellation;
        private long generation;
        private volatile bool disposed;
        private bool create, modeKnown, cancelRequested, closeAfterWork;
        internal VaultSession AcceptedSession { get; private set; }
        internal Task Work { get; private set; }
        internal Task PresentationWork { get; private set; }
        internal bool IsBusy { get; private set; }

        internal VaultUnlockForm(Func<CancellationToken, VaultProbeResult> probe = null,
            Func<bool, string, CancellationToken, VaultOpenResult> open = null,
            Func<bool> canStart = null, Func<bool> canPublish = null, Action<Task> workStarted = null,
            Func<DateTime> clock = null, Action<VaultSession, DateTime> expired = null)
        {
            this.clock = clock ?? (() => DateTime.Now);
            this.expired = expired;
            this.probe = probe ?? (token => VaultOpenService.Probe(AppPaths.VaultPath, token));
            this.open = open ?? ((creating, value, token) => creating
                ? VaultOpenService.Create(AppPaths.VaultPath, value, token)
                : VaultOpenService.Open(AppPaths.VaultPath, AppPaths.Root, value, this.clock,
                    () => VaultFakeDataFactory.Create(this.clock()), token));
            this.canStart = canStart; this.canPublish = canPublish; this.workStarted = workStarted;
            Text = "Хранилище секретов"; ClientSize = new Size(550, 290); MinimumSize = new Size(500, 310);
            layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = UiTheme.DialogPadding, ColumnCount = 2, RowCount = 5 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150)); layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 0));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56)); Controls.Add(layout);
            var pinLabel = UiTheme.Label("PIN-код", UiTheme.Body, UiTheme.Text);
            confirmLabel = UiTheme.Label("Повторите PIN-код", UiTheme.Body, UiTheme.Text);
            pin.AccessibleName = "PIN-код"; pin.AccessibleDescription = "Введите PIN-код из четырёх цифр. Ввод скрыт.";
            confirm.AccessibleName = "Повторите PIN-код"; confirm.AccessibleDescription = "Повторите PIN-код из четырёх цифр. Ввод скрыт.";
            layout.Controls.Add(pinLabel, 0, 0); layout.Controls.Add(pin, 1, 0);
            layout.Controls.Add(confirmLabel, 0, 1); layout.Controls.Add(confirm, 1, 1);
            confirmLabel.Visible = confirm.Visible = false;
            var hint = UiTheme.Label("PIN-код должен содержать ровно 4 цифры.", UiTheme.Body, UiTheme.Muted);
            hint.Dock = DockStyle.Fill; layout.Controls.Add(hint, 0, 2); layout.SetColumnSpan(hint, 2);
            status = UiTheme.Label("Проверяем доступность хранилища…", UiTheme.Body, UiTheme.Muted);
            status.Dock = DockStyle.Fill; status.AutoSize = false; status.AccessibleName = "Состояние открытия хранилища";
            status.AccessibleDescription = status.Text; status.TextChanged += delegate { status.AccessibleDescription = status.Text; };
            layout.Controls.Add(status, 0, 3); layout.SetColumnSpan(status, 2);
            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
            primary = UiTheme.Button("Открыть", delegate { Submit(); }, true); primary.Name = "VaultUnlockSubmit";
            cancel = UiTheme.Button("Отмена", delegate { CancelCurrent(); }, false); cancel.Name = "VaultUnlockCancel";
            primary.AccessibleDescription = "Проверяет PIN-код в фоне. Хранилище открывается после завершения проверки и необходимого сохранения.";
            cancel.AccessibleDescription = "Запрашивает отмену и закрывает окно после завершения текущего файлового вызова. Хранилище не открывается.";
            actions.Controls.Add(cancel); actions.Controls.Add(primary); layout.Controls.Add(actions, 0, 4); layout.SetColumnSpan(actions, 2);
            pin.Enabled = confirm.Enabled = primary.Enabled = false;
            AcceptButton = primary; CancelButton = cancel;
            // WinForms assigns Cancel automatically when CancelButton is set.
            // Busy Escape must request cancellation, not close before settlement.
            primary.DialogResult = cancel.DialogResult = DialogResult.None; UiTheme.ConfigureKeyboardOrder(this);
            Shown += delegate { StartProbe(); };
        }

        private bool Live { get { return ownerUi.OnOwner && !ownerUi.Closed && !disposed && !IsDisposed && !Disposing; } }
        private bool Allowed(Func<bool> check)
        { if (!Live) return false; try { return check == null || check(); } catch (Exception) { return false; } }
        private void StartProbe()
        {
            if (!Live || IsBusy) return;
            bool allowed = Allowed(canStart); if (!Live || IsBusy) return;
            if (!allowed) { status.Text = WaitToStart; primary.Text = "Повторить проверку"; primary.Enabled = true; return; }
            BeginWork("Проверяем доступность хранилища…"); var source = cancellation; var token = source.Token; long request = generation;
            var worker = Task.Run(delegate { try { return probe(token); } finally { source.Dispose(); } });
            Work = worker; NotifyStarted(); PresentationWork = FinishProbe(worker, token, request);
        }
        private void BeginWork(string message)
        {
            IsBusy = true; cancelRequested = closeAfterWork = false; AcceptedSession = null;
            cancellation = new CancellationTokenSource(); generation++;
            pin.Enabled = confirm.Enabled = primary.Enabled = false; cancel.Enabled = true;
            status.Text = message; cancel.Focus();
        }
        private void NotifyStarted()
        { try { if (workStarted != null) workStarted(Work); } catch (Exception) { CancelCurrent(); } }
        private async Task FinishProbe(Task<VaultProbeResult> worker, CancellationToken token, long request)
        {
            VaultProbeResult result = null;
            try {
                try { result = await worker.ConfigureAwait(false); } catch (Exception) { }
                await ownerUi.Await(Task.FromResult(true));
                if (!Live || request != generation) return;
                if (cancelRequested || token.IsCancellationRequested) { FinishCancelled(); return; }
                IsBusy = false; cancellation = null;
                if (result == null || (result.Status != VaultProbeStatus.Ready && result.Status != VaultProbeStatus.CreateRequired)) {
                    modeKnown = false; status.Text = Unavailable; primary.Text = "Повторить проверку"; primary.Enabled = true; return;
                }
                modeKnown = true; create = result.Status == VaultProbeStatus.CreateRequired;
                confirmLabel.Visible = confirm.Visible = create; layout.RowStyles[1].Height = create ? 38 : 0;
                Text = create ? "Создание хранилища" : "Разблокировка"; primary.Text = create ? "Создать" : "Открыть";
                status.Text = create ? "Введите новый PIN-код и повторите его." : "Введите PIN-код для открытия хранилища.";
                pin.Enabled = primary.Enabled = true; confirm.Enabled = create; pin.Focus();
            } catch (OperationCanceledException) { }
        }
        private void Submit()
        {
            if (!Live || IsBusy) return;
            if (!modeKnown) { StartProbe(); return; }
            bool allowed = Allowed(canStart); if (!Live || IsBusy) return;
            if (!allowed) { status.Text = WaitToStart; return; }
            string value = pin.Text;
            if (!VaultDailyPolicy.IsValidPin(value)) { status.Text = "PIN-код должен содержать ровно 4 цифры."; pin.Focus(); return; }
            if (create && value != confirm.Text) { status.Text = "PIN-коды не совпадают."; confirm.Focus(); return; }
            bool creating = create; BeginWork(create ? "Создаём хранилище…" : "Открываем хранилище…");
            pin.Clear(); confirm.Clear(); var source = cancellation; var token = source.Token; long request = generation;
            var worker = Task.Run(delegate { try { return open(creating, value, token); } finally { source.Dispose(); } });
            Work = worker; NotifyStarted(); PresentationWork = FinishOpen(worker, token, request);
        }
        private async Task FinishOpen(Task<VaultOpenResult> worker, CancellationToken token, long request)
        {
            VaultOpenResult result = null; bool accepted = false;
            try {
                try { result = await worker.ConfigureAwait(false); } catch (Exception) { }
                await ownerUi.Await(Task.FromResult(true));
                if (!Live || request != generation) return;
                if (cancelRequested || token.IsCancellationRequested) { FinishCancelled(); return; }
                bool allowed = Allowed(canPublish);
                if (!Live || request != generation) return;
                if (cancelRequested || token.IsCancellationRequested) { FinishCancelled(); return; }
                if (!allowed) { FinishError(WaitToStart, false); return; }
                if (result != null && result.Status == VaultOpenStatus.Success && result.Session != null) {
                    bool current = false;
                    var observed = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
                    // Real authentication remains independent of a broken clock.
                    try {
                        observed = result.Session.Persistence == null ? default(DateTime) : clock();
                        current = result.Session.IsCurrent(observed);
                    } catch (Exception) { }
                    if (!Live || request != generation) return;
                    if (cancelRequested || token.IsCancellationRequested) { FinishCancelled(); return; }
                    if (!current) {
                        result.Session.Revoke();
                        try { if (expired != null) expired(result.Session, observed); } catch (Exception) { }
                        if (!Live || request != generation) return;
                        if (cancelRequested || token.IsCancellationRequested) { FinishCancelled(); return; }
                        FinishError(VaultOpenResult.Failed(VaultOpenStatus.DateChanged).ErrorMessage, false); return;
                    }
                    IsBusy = false; cancellation = null;
                    AcceptedSession = result.Session; accepted = true; DialogResult = DialogResult.OK; Close(); return;
                }
                FinishError(result == null ? Unavailable : result.ErrorMessage ?? Unavailable, create);
            } catch (OperationCanceledException) { }
            finally { if (!accepted && result != null && result.Session != null) result.Session.DropSensitive(); }
        }
        private void RestoreInput()
        { if (!Live) return; pin.Enabled = primary.Enabled = true; confirm.Enabled = create; cancel.Enabled = true; pin.Focus(); }
        private void FinishError(string message, bool resetMode)
        {
            if (!Live) return;
            IsBusy = false; cancellation = null; status.Text = message;
            if (resetMode) { modeKnown = false; pin.Enabled = confirm.Enabled = false; primary.Text = "Повторить проверку"; primary.Enabled = cancel.Enabled = true; }
            else RestoreInput();
        }
        private void FinishCancelled()
        {
            if (!Live) return;
            IsBusy = false; cancellation = null; AcceptedSession = null;
            if (closeAfterWork) { DialogResult = DialogResult.Cancel; Close(); }
            else { status.Text = "Открытие хранилища отменено."; RestoreInput(); }
        }
        internal void CancelCurrent()
        {
            if (!Live) return;
            if (!IsBusy) { DialogResult = DialogResult.Cancel; Close(); return; }
            cancelRequested = closeAfterWork = true; var source = cancellation;
            if (source != null) try { source.Cancel(); } catch (ObjectDisposedException) { }
            cancel.Enabled = false; status.Text = "Отмена запрошена. Ждём завершения текущего вызова без блокировки интерфейса…";
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (!e.Cancel && IsBusy && !disposed) { e.Cancel = true; CancelCurrent(); }
        }
        protected override void OnFormClosed(FormClosedEventArgs e)
        { ownerUi.Close(); base.OnFormClosed(e); }
        protected override void Dispose(bool disposing)
        {
            if (disposing && !disposed) {
                disposed = true; generation++; cancelRequested = true;
                var source = cancellation; if (source != null) try { source.Cancel(); } catch (ObjectDisposedException) { }
                ownerUi.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}

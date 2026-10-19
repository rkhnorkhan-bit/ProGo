using System;
using System.Collections.Generic;
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
        private readonly Func<SshAgentKeyAddRequest, CancellationToken, SshAgentKeyAddResult> add;
        private readonly Label status = UiTheme.Label("", UiTheme.Body, UiTheme.Muted);
        private readonly TextBox report = new TextBox { Name = "SshDiagnosticReport", ReadOnly = true, Multiline = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Vertical, WordWrap = true };
        private readonly Button retry, cancel, addKey;
        private readonly ComboBox identities = new ComboBox { Name = "SshDiagnosticIdentity", DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
        private readonly Label keyHint = UiTheme.Label("Сначала дождитесь проверки SSH-профиля.", UiTheme.Body, UiTheme.Muted);
        private CancellationTokenSource cancellation;
        private readonly TaskCompletionSource<bool> ownerClosed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        private bool busy, closing, adding;
        internal Task Work { get; private set; }
        internal Task CheckWork { get; private set; }
        internal Task KeyWork { get; private set; }
        private sealed class KeyChoice
        {
            internal readonly SshAgentKeyAddRequest Request;
            internal readonly SshAgentKeyAddState State;
            private readonly string label;
            internal KeyChoice(string identity, int index, SshAgentKeyAddRequest request, SshAgentKeyAddState state)
            {
                Request = request; State = state;
                char[] labelChars = (identity ?? "").ToCharArray();
                for (int i = 0; i < labelChars.Length; i++) if (Char.IsControl(labelChars[i])) labelChars[i] = ' ';
                string name = new string(labelChars);
                label = "Ключ " + index + ": " + (name.Length > 240 ? name.Substring(0, 240) + "…" : name);
            }
            public override string ToString() { return label; }
        }
        internal SshDiagnosticForm(SshProfileSetting selected, Func<SshProfileSetting, CancellationToken, SshProfileDiagnosticResult> check = null,
            Func<SshAgentKeyAddRequest, CancellationToken, SshAgentKeyAddResult> add = null)
        {
            profile = selected.Clone(); this.check = check ?? ((p, token) => SshProfileDiagnostics.Check(p, token));
            this.add = add ?? ((request, token) => SshAgentKeyAdd.Run(request, token));
            CheckWork = KeyWork = Task.FromResult(false);
            Text = "Проверка настроек SSH · ProGo"; ClientSize = new Size(790, 660); MinimumSize = new Size(650, 560);
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = UiTheme.DialogPadding, ColumnCount = 1, RowCount = 6 };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 90)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
            root.Controls.Add(UiTheme.Label("Проверяем настройки подключения", UiTheme.Heading, UiTheme.Text), 0, 0); root.Controls.Add(report, 0, 1);
            var keys = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Margin = Padding.Empty };
            keys.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 65)); keys.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            keys.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
            keys.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            keys.Controls.Add(UiTheme.Label("Ключ:", UiTheme.Body, UiTheme.Text), 0, 0); keys.Controls.Add(identities, 1, 0);
            addKey = UiTheme.Button("Добавить ключ", delegate { if (!busy && SelectedRequest != null) Work = RunAdd(); }, true);
            addKey.Name = "SshDiagnosticAddKey"; addKey.AutoSize = false; addKey.Dock = DockStyle.Fill;
            addKey.TextAlign = ContentAlignment.MiddleCenter; keys.Controls.Add(addKey, 2, 0);
            root.Controls.Add(keys, 0, 2);
            keyHint.Name = "SshDiagnosticKeyHint"; keyHint.AutoSize = false; keyHint.Dock = DockStyle.Fill; root.Controls.Add(keyHint, 0, 3);
            status.AutoSize = false; status.Dock = DockStyle.Fill; root.Controls.Add(status, 0, 4);
            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
            cancel = UiTheme.Button("Отменить проверку", delegate { if (busy) CancelWork(); else Close(); }, false); cancel.Name = "SshDiagnosticCancel";
            retry = UiTheme.Button("Проверить ещё раз", delegate { if (!busy) Work = Run(); }, true); retry.Name = "SshDiagnosticRetry";
            actions.Controls.Add(cancel); actions.Controls.Add(retry); root.Controls.Add(actions, 0, 5); Controls.Add(root);
            report.AccessibleName = "Результат проверки настроек SSH";
            report.AccessibleDescription = "Только чтение. Настройки OpenSSH и локальная проверка; не подтверждает доступность сервера. Можно выделить и скопировать текст.";
            status.AccessibleName = "Состояние проверки SSH";
            status.TextChanged += delegate { status.AccessibleDescription = status.Text; };
            identities.AccessibleName = "Ключ выбранного SSH-профиля";
            identities.AccessibleDescription = "Выберите один IdentityFile из результата проверки. Полный путь указан в отчёте. Во время операции выбор недоступен.";
            addKey.AccessibleDescription = "Добавляет выбранный ключ через отдельное окно OpenSSH. Парольная фраза вводится только там; ProGo её не читает. Вход на сервер этим не подтверждается.";
            keyHint.AccessibleName = "Доступность добавления ключа";
            keyHint.TextChanged += delegate { keyHint.AccessibleDescription = keyHint.Text; };
            keyHint.AccessibleDescription = keyHint.Text;
            identities.SelectedIndexChanged += delegate { RefreshKeyAction(); };
            identities.Enabled = addKey.Enabled = false;
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
                : adding ? "Отменяет добавление и закрывает только его окно OpenSSH после очистки. Уже принятый агентом ключ не удаляется. Окно остаётся открытым."
                : "Прекращает ожидание и останавливает только процессы этой проверки. Системный запрос может завершиться позже. Окно остаётся открытым для повтора.";
        }
        private void CancelWork()
        {
            var source = cancellation;
            if (source == null) return;
            try { source.Cancel(); } catch (ObjectDisposedException) { }
            if (!closing) { status.Text = adding ? "Отменяем добавление и ожидаем очистки окна OpenSSH…" : "Отменяем проверку и закрываем её процессы…"; cancel.Enabled = false; }
        }
        private SshAgentKeyAddRequest SelectedRequest
        { get { var choice = identities.SelectedItem as KeyChoice; return choice == null ? null : choice.Request; } }
        private void ClearKeys(string hint)
        { identities.Items.Clear(); identities.Enabled = addKey.Enabled = false; keyHint.Text = hint; }
        private void RefreshKeyAction()
        {
            addKey.Enabled = !busy && !closing && SelectedRequest != null;
            if (busy) return;
            var choice = identities.SelectedItem as KeyChoice;
            keyHint.Text = choice == null ? "Выберите ключ из проверенного профиля." : KeyHint(choice.State);
        }
        private static string KeyHint(SshAgentKeyAddState state)
        {
            switch (state) {
                case SshAgentKeyAddState.Prepared: return "Парольная фраза вводится только в отдельном окне OpenSSH. Добавление не проверяет вход на сервер.";
                case SshAgentKeyAddState.AuthenticationPolicyUnconfirmed: return "Политика входа по ключу не подтверждена. Добавление недоступно; повторите проверку профиля.";
                case SshAgentKeyAddState.NotUsedByAuthentication: return "Этот профиль не использует вход по публичному ключу; добавление не требуется.";
                case SshAgentKeyAddState.DisabledByProfile: return "Использование агента отключено профилем (IdentityAgent none).";
                case SshAgentKeyAddState.UnsupportedEndpoint: return "Отдельный агент профиля пока не поддерживается этим действием; стандартный агент не подставляется.";
                case SshAgentKeyAddState.MissingExecutable: return "ssh-add.exe из выбранной установки OpenSSH не найден. Проверьте установку OpenSSH.";
                case SshAgentKeyAddState.PathUnresolved: return "Полный путь выбранного ключа не подтверждён. Повторите проверку SSH-профиля.";
                default: return "Нет подтверждённого ключа для добавления. Повторите проверку SSH-профиля.";
            }
        }
        private static List<KeyChoice> PrepareKeys(SshProfileDiagnosticResult result, CancellationToken token)
        {
            var choices = new List<KeyChoice>(); var seen = new HashSet<string>(StringComparer.Ordinal);
            if (result != null) foreach (string identity in result.IdentityFiles) {
                token.ThrowIfCancellationRequested();
                if (choices.Count == SshProfileKeyMatching.IdentityLimit) break;
                if (!seen.Add(identity)) continue;
                SshAgentKeyAddState state; var request = SshAgentKeyAddRequest.Prepare(result, identity, out state);
                choices.Add(new KeyChoice(identity, choices.Count + 1, request, state));
            }
            return choices;
        }
        private async Task Run()
        {
            if (busy || closing) return;
            busy = true; adding = false; retry.Enabled = false; cancel.Enabled = true; cancel.Text = "Отменить проверку";
            ClearKeys("Сначала дождитесь проверки SSH-профиля.");
            report.Clear(); status.Text = "Проверяем SSH и ключи без подключения к серверу. Лимиты фаз: SSH — 7 с; служба — 3 с; агент — 3 с; отпечатки — 3 с. Очистка своих процессов — до 2 с. Можно отменить.";
            var source = new CancellationTokenSource(); cancellation = source; var token = source.Token;
            try {
                string text = "", notice, unavailable = KeyHint(SshAgentKeyAddState.NotChecked); var choices = new List<KeyChoice>();
                try {
                    SshProfileDiagnosticResult result = null;
                    var checking = Task.Run(() => {
                        result = check(profile.Clone(), token);
                        text = result.ToReport(); choices = PrepareKeys(result, token);
                        SshAgentKeyAddState state; SshAgentKeyAddRequest.Prepare(result, null, out state); unavailable = KeyHint(state);
                    }); CheckWork = checking;
                    await checking.ConfigureAwait(false);
                    token.ThrowIfCancellationRequested();
                    notice = result.SshResolved ? "Настройки SSH прочитаны. Доступность сервера проверяется подключением." : "Настройки SSH не проверены. Причина указана выше.";
                }
                catch (OperationCanceledException) { choices.Clear(); notice = "Проверка отменена. Можно повторить."; }
                catch { choices.Clear(); text = "Проверка не завершена. Проверьте OpenSSH и права доступа к настройкам."; notice = "Не удалось завершить проверку. Можно повторить."; }
                await Publish(delegate {
                    if (token.IsCancellationRequested) { report.Clear(); status.Text = "Проверка отменена. Можно повторить."; }
                    else { report.Text = text; status.Text = notice; }
                    cancellation = null; busy = false;
                    retry.Enabled = cancel.Enabled = true; cancel.Text = "Закрыть";
                    if (token.IsCancellationRequested) ClearKeys("Проверка отменена. Повторите её для выбора ключа.");
                    else if (choices.Count == 0) ClearKeys(unavailable);
                    else {
                        bool usable = false;
                        foreach (var choice in choices) { identities.Items.Add(choice); usable |= choice.Request != null; }
                        identities.Enabled = usable;
                        if (choices.Count == 1 || !usable && choices.Count > 0) identities.SelectedIndex = 0;
                        RefreshKeyAction();
                    }
                }).ConfigureAwait(false);
            }
            finally {
                Interlocked.CompareExchange(ref cancellation, null, source); source.Dispose();
            }
        }
        private async Task RunAdd()
        {
            var request = SelectedRequest;
            if (busy || closing || request == null) return;
            busy = adding = true; retry.Enabled = identities.Enabled = addKey.Enabled = false;
            cancel.Enabled = true; cancel.Text = "Отменить добавление"; cancel.Focus();
            status.Text = "Открываем отдельное окно OpenSSH. Введите парольную фразу там. Лимит — 2 минуты; после отмены ожидаем очистки своих процессов.";
            var source = new CancellationTokenSource(); cancellation = source; var token = source.Token;
            try {
                SshAgentKeyAddResult result;
                try {
                    var operation = Task.Factory.StartNew(() => add(request, token), CancellationToken.None,
                        TaskCreationOptions.LongRunning, TaskScheduler.Default); KeyWork = operation;
                    result = await operation.ConfigureAwait(false) ?? new SshAgentKeyAddResult(SshAgentKeyAddState.Failed);
                } catch (OperationCanceledException) { result = new SshAgentKeyAddResult(SshAgentKeyAddState.Cancelled); }
                catch { result = new SshAgentKeyAddResult(SshAgentKeyAddState.Failed); }
                await Publish(delegate {
                    if (token.IsCancellationRequested) result = new SshAgentKeyAddResult(SshAgentKeyAddState.Cancelled);
                    // An attempted mutation invalidates inventory/fingerprint evidence.
                    // Require a fresh check before another addition, including Busy/error.
                    report.Clear(); ClearKeys("Сведения о ключах сброшены. Нажмите «Проверить ещё раз» перед следующим добавлением.");
                    status.Text = result.ToDisplay() + (result.ExitCode.HasValue ? " Код ssh-add: " + result.ExitCode.Value + "." : "") +
                        (result.NativeError.HasValue ? " Код Windows: " + result.NativeError.Value + "." : "");
                    cancellation = null; busy = adding = false;
                    retry.Enabled = cancel.Enabled = true; cancel.Text = "Закрыть";
                }).ConfigureAwait(false);
            } finally { Interlocked.CompareExchange(ref cancellation, null, source); source.Dispose(); }
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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProGo
{
    internal sealed class HomeVpnPortableForm : ProGoForm
    {
        private readonly bool export;
        private readonly string privateRoot;
        private readonly Action imported;
        private readonly Func<bool, string> chooseFile;
        private readonly Action<string, CancellationToken> beforeIo;
        private readonly int timeoutMilliseconds;
        private readonly TextBox password = new TextBox { Name = "PortablePassword", UseSystemPasswordChar = true, MaxLength = 1024, Dock = DockStyle.Fill };
        private readonly TextBox repeat = new TextBox { Name = "PortablePasswordRepeat", UseSystemPasswordChar = true, MaxLength = 1024, Dock = DockStyle.Fill };
        private readonly TextBox preview = new TextBox { Name = "PortablePreview", Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical };
        private readonly Label status = UiTheme.Label("", UiTheme.Body, UiTheme.Text);
        private readonly Button prepare, accept, close;
        private readonly Control dispatcher = new Control();
        private readonly object completionsGate = new object();
        private readonly HashSet<TaskCompletionSource<bool>> completions = new HashSet<TaskCompletionSource<bool>>();
        private CancellationTokenSource cancellation;
        private HomeVpnPortablePrepared prepared;
        private volatile bool disposed;
        private volatile bool userCancelled;
        private bool closeAfterWork;
        internal Task Work { get; private set; }
        internal bool IsBusy { get; private set; }
        internal void CancelCurrentOperation() { if (IsBusy) RequestCancel(false); }
        internal event Action ResultApplied;

        internal HomeVpnPortableForm(bool export, string privateRoot, Action imported = null,
            Func<bool, string> chooseFile = null, Action<string, CancellationToken> beforeIo = null, int timeoutMilliseconds = 60000)
        {
            this.export = export; this.privateRoot = privateRoot; this.imported = imported; this.chooseFile = chooseFile ?? SelectFile; this.beforeIo = beforeIo;
            this.timeoutMilliseconds = Math.Max(1, timeoutMilliseconds);
            Text = export ? "Экспорт VPN на другой ПК · ProGo" : "Импорт защищённого VPN · ProGo";
            ClientSize = new Size(760, 570); MinimumSize = new Size(720, 540);
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = UiTheme.DialogPadding, ColumnCount = 1, RowCount = 8 };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            foreach (int height in new[] { 68, 24, 34, 24, 34, 62 }) root.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
            var explanation = UiTheme.Label(export ?
                "Переносимый архив содержит личный VPN-доступ и незавершённые запросы. Задайте отдельную длинную парольную фразу и сохраните её отдельно от файла. Обычная DPAPI-копия не переносима между пользователями Windows." :
                "Импорт предназначен для чистой установки. Сначала проверьте архив и состав. Импорт сохранит исходный VPS и запросы; настройку другого VPS выполняют отдельно в мастере.", UiTheme.Body, UiTheme.Muted);
            explanation.Dock = DockStyle.Fill; explanation.AutoSize = false;
            root.Controls.Add(explanation, 0, 0);
            root.Controls.Add(UiTheme.Label("Отдельная парольная фраза экспорта", UiTheme.Body, UiTheme.Text), 0, 1); root.Controls.Add(password, 0, 2);
            password.AccessibleName = "Парольная фраза защищённого экспорта VPN";
            password.AccessibleDescription = "Не менее 16 символов. Не сохраняется в ProGo; PIN хранилища и passphrase SSH не используются.";
            var repeatLabel = UiTheme.Label("Повторите парольную фразу", UiTheme.Body, UiTheme.Text);
            repeatLabel.Visible = repeat.Visible = export; root.Controls.Add(repeatLabel, 0, 3); root.Controls.Add(repeat, 0, 4);
            repeat.AccessibleName = "Повтор парольной фразы экспорта VPN";
            if (!export) { root.RowStyles[3].Height = 0; root.RowStyles[4].Height = 0; }
            status.Dock = DockStyle.Fill; status.AutoSize = false; status.AccessibleName = "Состояние переноса VPN";
            status.TextChanged += delegate { status.AccessibleDescription = status.Text; };
            status.Text = "Ничего не запускается автоматически. .ssh, vault и настройки Windows не изменяются.";
            root.Controls.Add(status, 0, 5); root.Controls.Add(preview, 0, 6);
            preview.AccessibleName = "Состав и результат переноса VPN";
            preview.AccessibleDescription = "Только чтение. Показывает имена данных и исходный VPS, без токенов и ключей.";
            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Padding = new Padding(0, 8, 0, 0) };
            close = UiTheme.Button("Закрыть", delegate { if (IsBusy) RequestCancel(false); else Close(); }, false);
            close.Name = "PortableCancel"; close.AccessibleDescription = "Во время операции запрашивает отмену. Завершение файлового вызова ожидается без блокировки интерфейса.";
            accept = UiTheme.Button("Импортировать этот состав", delegate { ImportPrepared(); }, false);
            accept.Name = "PortableImport"; accept.Visible = !export; accept.Enabled = false;
            accept.AccessibleDescription = "После проверки сохраняет указанный VPN-доступ в чистую установку. Подключение и настройки Windows не включает.";
            prepare = UiTheme.Button(export ? "Создать защищённый экспорт" : "Проверить архив", delegate { PrepareArchive(); }, false);
            prepare.Name = "PortablePrepare";
            prepare.AccessibleDescription = export ? "Выбрать новый файл и создать зашифрованный переносимый экспорт. Исходный доступ и VPS не изменяются." :
                "Выбрать архив и проверить парольную фразу и состав в памяти. Эта кнопка ещё не импортирует данные.";
            actions.Controls.Add(close); actions.Controls.Add(accept); actions.Controls.Add(prepare); root.Controls.Add(actions, 0, 7);
            Controls.Add(root); CancelButton = close; UiTheme.ConfigureKeyboardOrder(root); dispatcher.CreateControl();
            FormClosing += delegate(object sender, FormClosingEventArgs e) { if (IsBusy) { e.Cancel = true; RequestCancel(true); } };
        }

        private string SelectFile(bool exporting)
        {
            if (exporting) {
                using (var dialog = new SaveFileDialog { Filter = "Защищённый VPN ProGo (*.progo-vpn)|*.progo-vpn", DefaultExt = "progo-vpn", AddExtension = true,
                    Title = "Выберите новое имя файла экспорта VPN", FileName = "ProGo-VPN-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".progo-vpn", OverwritePrompt = false })
                    return dialog.ShowDialog(this) == DialogResult.OK ? dialog.FileName : null;
            }
            using (var dialog = new OpenFileDialog { Filter = "Защищённый VPN ProGo (*.progo-vpn)|*.progo-vpn", CheckFileExists = true })
                return dialog.ShowDialog(this) == DialogResult.OK ? dialog.FileName : null;
        }
        private void PrepareArchive()
        {
            if (IsBusy) return;
            byte[] bytes = null;
            try {
                if (export && password.Text != repeat.Text) throw new HomeVpnPortableException("Парольные фразы не совпадают. Повторите ввод; файл не создан.");
                bytes = new UTF8Encoding(false, true).GetBytes(password.Text); HomeVpnPortableArchive.ValidatePassword(bytes);
                var path = chooseFile(export); if (String.IsNullOrEmpty(path)) return;
                password.Clear(); repeat.Clear(); if (prepared != null) { prepared.Dispose(); prepared = null; } accept.Enabled = false;
                var secret = bytes; bytes = null;
                try {
                    Start(delegate(CancellationToken token) {
                        try {
                            if (export) return (object)HomeVpnPortableArchive.Export(privateRoot, path, secret, token, beforeIo);
                            return HomeVpnPortableArchive.Prepare(path, secret, token, beforeIo);
                        } finally { Array.Clear(secret, 0, secret.Length); }
                    }, delegate(object result) {
                        if (export) { preview.Text = ((HomeVpnPortablePreview)result).Description; status.Text = "Защищённый экспорт создан. Сохраните парольную фразу отдельно; старый доступ не отозван."; }
                        else {
                            prepared = (HomeVpnPortablePrepared)result; preview.Text = prepared.Preview.Description;
                            status.Text = "Архив проверен. Просмотрите состав и отдельно нажмите «Импортировать этот состав»."; accept.Enabled = true;
                        }
                    });
                } catch { Array.Clear(secret, 0, secret.Length); throw; }
            } catch (HomeVpnPortableException ex) { status.Text = ex.Message; }
            catch (Exception) { status.Text = "Не удалось подготовить перенос VPN. Проверьте ввод и выбранный файл."; }
            finally { if (bytes != null) Array.Clear(bytes, 0, bytes.Length); }
        }
        private void ImportPrepared()
        {
            if (IsBusy || prepared == null) return;
            var current = prepared; prepared = null; accept.Enabled = false;
            try {
                Start(delegate(CancellationToken token) {
                    try { HomeVpnPortableArchive.Import(current, privateRoot, token, beforeIo); return (object)current.Preview; }
                    finally { current.Dispose(); }
                }, delegate(object result) {
                    preview.Text = ((HomeVpnPortablePreview)result).Description;
                    status.Text = "VPN-доступ импортирован с защитой текущего пользователя Windows. Перезапустите ProGo для загрузки доступа. Подключение не запускалось; SSH-ключ владельца выберите на этом ПК.";
                    if (imported != null) imported();
                });
            } catch { current.Dispose(); throw; }
        }
        private void RequestCancel(bool closeWhenDone)
        {
            if (closeWhenDone) closeAfterWork = true;
            userCancelled = true;
            var current = cancellation; if (current != null) try { current.Cancel(); } catch (ObjectDisposedException) { }
            close.Enabled = false; status.Text = "Отмена запрошена. Ждём завершения текущего вызова без блокировки интерфейса. Прежние файлы сохраняются.";
        }
        private void Start(Func<CancellationToken, object> operation, Action<object> apply)
        {
            IsBusy = true; prepare.Enabled = accept.Enabled = password.Enabled = repeat.Enabled = false; close.Text = "Отменить";
            userCancelled = false; cancellation = new CancellationTokenSource(); var source = cancellation; source.CancelAfter(timeoutMilliseconds);
            status.Text = "Защищённый перенос выполняется в фоне. Интерфейс остаётся доступным.";
            Work = Run(source, operation, apply);
        }
        private async Task Run(CancellationTokenSource source, Func<CancellationToken, object> operation, Action<object> apply)
        {
            object result = null; bool cancelled = false, timedOut = false; string failure = null;
            try { result = await Task.Run(() => operation(source.Token)).ConfigureAwait(false); }
            catch (OperationCanceledException) { cancelled = true; timedOut = source.IsCancellationRequested && !userCancelled; }
            catch (HomeVpnPortableException ex) { failure = ex.Message; }
            catch (Exception) { failure = "Перенос VPN не завершён. Прежние данные сохранены; автоматических повторов нет."; }
            finally { source.Dispose(); }
            bool applied = false;
            try {
                applied = await Dispatch(delegate {
                    cancellation = null; IsBusy = false; prepare.Enabled = password.Enabled = repeat.Enabled = true; close.Enabled = true; close.Text = "Закрыть";
                    if (timedOut) status.Text = "Время переноса VPN истекло. Операция отменена до публикации; прежние данные сохранены. Проверьте накопитель и повторите вручную.";
                    else if (cancelled) status.Text = "Перенос отменён. Исходные данные и предыдущие файлы сохранены.";
                    else if (failure != null) status.Text = failure;
                    else apply(result);
                    if (ResultApplied != null) ResultApplied(); if (closeAfterWork) Close();
                }).ConfigureAwait(false);
            } finally { if (!applied) { var abandoned = result as IDisposable; if (abandoned != null) abandoned.Dispose(); } }
        }
        private Task<bool> Dispatch(Action action)
        {
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (completionsGate) { if (disposed) return Task.FromResult(false); completions.Add(completion); }
            try {
                dispatcher.BeginInvoke(new Action(delegate {
                    try {
                        if (disposed || IsDisposed) { completion.TrySetResult(false); return; }
                        action(); completion.TrySetResult(true);
                    } catch (Exception) {
                        status.Text = "Не удалось обновить окно результата. Закройте и снова откройте мастер VPN; подключение не запускалось.";
                        completion.TrySetResult(true);
                    } finally { lock (completionsGate) completions.Remove(completion); }
                }));
            } catch (InvalidOperationException) { lock (completionsGate) completions.Remove(completion); completion.TrySetResult(false); }
            return completion.Task;
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing && !disposed) {
                disposed = true; userCancelled = true; var current = cancellation; if (current != null) try { current.Cancel(); } catch (ObjectDisposedException) { }
                lock (completionsGate) { foreach (var completion in completions) completion.TrySetResult(false); completions.Clear(); }
                if (prepared != null) { prepared.Dispose(); prepared = null; }
                password.Clear(); repeat.Clear(); dispatcher.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}

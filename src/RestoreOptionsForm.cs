using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProGo
{
    internal sealed class RestoreOptionsForm : ProGoForm
    {
        private readonly string source;
        private readonly RadioButton program = new RadioButton { Name = "RestoreProgram", Text = "Программа — текущие настройки и хранилище сохранятся", Checked = true, AutoSize = true };
        private readonly RadioButton data = new RadioButton { Name = "RestoreData", Text = "Настройки и хранилище — версия программы сохранится", AutoSize = true };
        private readonly RadioButton all = new RadioButton { Name = "RestoreAll", Text = "Программа, настройки и хранилище", AutoSize = true };
        private readonly CheckBox consent = new CheckBox { Name = "RestoreDataConsent", Text = "Подтверждаю замену перечисленных ниже пользовательских данных", AutoSize = true };
        private readonly TextBox contents = new TextBox { Name = "RestoreContents", AccessibleName = "Состав восстановления", AccessibleDescription = "Только чтение. Точный список файлов для выбранного состава восстановления; его можно выделить и скопировать.", Multiline = true, ReadOnly = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Vertical };
        private readonly Label status = UiTheme.Label("", UiTheme.Body, UiTheme.Muted);
        private readonly BackupWorkSession work;
        private readonly bool ownsWork;
        private RestorePreview preview;
        private readonly Button prepare;
        private readonly Button cancel;
        private bool busy;
        private bool disposed;
        private RestorePreparedInfo prepared;
        internal Task<RestorePreview> PreviewWork { get; private set; }
        internal Task<RestorePreparedInfo> PrepareWork { get; private set; }
        internal bool PreviewReady { get { return preview != null; } }
        public string Scope { get { return data.Checked ? "Data" : (all.Checked ? "All" : "Program"); } }
        public bool DataConfirmed { get { return Scope != "Program" && consent.Checked; } }

        public RestoreOptionsForm(string backupDir) : this(backupDir, null, null) { }
        internal RestoreOptionsForm(string backupDir, RestorePreview preview, BackupWorkSession work)
        {
            this.work = work ?? new BackupWorkSession(); ownsWork = work == null; this.preview = preview;
            source = backupDir; Text = "Что восстановить из копии";
            Width = 850; Height = 630;
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), ColumnCount = 1, RowCount = 6 };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            foreach (var height in new[] { 48, 126, 46 }) root.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
            root.Controls.Add(UiTheme.Label("Выберите состав восстановления", UiTheme.Heading, UiTheme.Text), 0, 0);
            var choices = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
            choices.Controls.Add(program); choices.Controls.Add(data); choices.Controls.Add(all);
            root.Controls.Add(choices, 0, 1); root.Controls.Add(consent, 0, 2); root.Controls.Add(contents, 0, 3);
            status.AutoSize = false; status.Dock = DockStyle.Fill; root.Controls.Add(status, 0, 4);
            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 8, 0, 0) };
            cancel = UiTheme.Button("Отмена", delegate { if (busy) { this.work.Cancel(); status.Text = "Отмена подготовки…"; } else DialogResult = DialogResult.Cancel; }, false);
            prepare = UiTheme.Button("Проверить и подготовить копию", async delegate { await PrepareCopy(); }, true);
            actions.Controls.Add(cancel); actions.Controls.Add(prepare); root.Controls.Add(actions, 0, 5);
            status.AccessibleName = "Состояние подготовки копии";
            consent.AccessibleDescription = "Разрешение заменить пользовательские данные. Смена состава восстановления сбрасывает подтверждение.";
            UiTheme.ConfigureKeyboardOrder(root);
            Controls.Add(root); CancelButton = cancel; // No destructive Enter default.
            data.Enabled = all.Enabled = preview != null && preview.HasData;
            program.CheckedChanged += ChoiceChanged; data.CheckedChanged += ChoiceChanged; all.CheckedChanged += ChoiceChanged;
            consent.CheckedChanged += delegate { RefreshSelection(); };
            FormClosing += delegate(object sender, FormClosingEventArgs e) { if (busy) { e.Cancel = true; this.work.Cancel(); status.Text = "Отмена подготовки…"; } };
            Shown += delegate { cancel.Focus(); if (this.preview == null) LoadPreview(); };
            RefreshSelection();
        }

        private void ChoiceChanged(object sender, EventArgs e)
        { consent.Checked = false; RefreshSelection(); }

        private void RefreshSelection()
        {
            consent.Enabled = Scope != "Program" && !busy;
            if (preview == null) { contents.Clear(); prepare.Enabled = false; status.Text = "Читаем состав копии в фоне… Приложение и подключения продолжают работать."; return; }
            {
                contents.Lines = preview.Names(Scope) ?? new string[0];
                prepare.Enabled = !busy && (Scope == "Program" || consent.Checked);
                status.Text = "Ниже — точный состав. Текущие VPN-доступы, снимки прокси и журналы сохранятся.\nАрхив VPN защищён DPAPI: это не перенос на другой ПК/пользователя; автоматического импорта нет.\nСначала проверим отдельную копию; ProGo пока останется запущенным.";
            }
        }

        private async void LoadPreview()
        {
            RestorePreview result = null; string error = null;
            try { PreviewWork = work.Run("restore-preview", token => RestorePreview.Read(source, token)); result = await PreviewWork.ConfigureAwait(false); }
            catch (OperationCanceledException) { error = "Чтение состава отменено. Закройте окно и выберите копию снова."; }
            catch (Exception ex) { SafeLog.Error("Restore preview failed.", ex); error = "Не удалось прочитать состав копии. Проверьте доступ и журнал ProGo."; }
            Publish(delegate { if (result == null) { status.Text = error; prepare.Enabled = false; return; }
                preview = result; data.Enabled = all.Enabled = preview.HasData; RefreshSelection(); });
        }

        private void Publish(Action action)
        {
            if (disposed || !IsHandleCreated) return;
            try { BeginInvoke(new Action(delegate { if (!disposed && !IsDisposed) action(); })); } catch (InvalidOperationException) { }
        }

        private async Task PrepareCopy()
        {
            if (busy || preview == null || (Scope != "Program" && !consent.Checked)) return;
            var scope = Scope; var confirm = DataConfirmed;
            busy = true; prepare.Enabled = false; program.Enabled = data.Enabled = all.Enabled = consent.Enabled = false;
            status.Text = "Проверяем файлы и подготавливаем отдельную копию. Можно отменить подготовку.";
            try
            {
                PrepareWork = work.Run("restore-prepare", token => work.Prepare(source, scope, confirm, token));
                var result = await PrepareWork.ConfigureAwait(false);
                Publish(delegate { if (work.Token.IsCancellationRequested) { busy = false; DialogResult = DialogResult.Cancel; return; }
                    prepared = result; busy = false; DialogResult = DialogResult.OK; });
            }
            catch (OperationCanceledException) { Publish(delegate { busy = false; DialogResult = DialogResult.Cancel; }); }
            catch (Exception ex)
            {
                SafeLog.Error("Restore preparation failed.", ex);
                Publish(delegate { busy = false; prepare.Enabled = false;
                    status.Text = "Копия не подготовлена: ошибка проверки или чтения.\nПриложение и подключения продолжают работать. Проверьте журнал и доступ к копии; автоматических повторов нет."; });
            }
        }

        internal PreparedBackup TakePreparedCopy()
        {
            if (DialogResult != DialogResult.OK || prepared == null) throw new InvalidOperationException("Копия ещё не подготовлена.");
            var result = prepared.Copy; work.ReleaseCopy(result); prepared = null; return result;
        }
        internal RestorePreparedInfo TakePreparedInfo()
        { if (DialogResult != DialogResult.OK || prepared == null) throw new InvalidOperationException("Копия ещё не подготовлена."); return prepared; }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !disposed)
            {
                disposed = true;
                // The worker session owns cleanup, including a result produced
                // after this form loses its owner. Never recursively delete on UI.
                if (ownsWork) work.FinishAsync();
            }
            base.Dispose(disposing);
        }
    }
}

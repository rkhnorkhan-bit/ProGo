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
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        private readonly Button prepare;
        private readonly Button cancel;
        private bool busy;
        private bool disposed;
        private PreparedBackup prepared;
        public string Scope { get { return data.Checked ? "Data" : (all.Checked ? "All" : "Program"); } }
        public bool DataConfirmed { get { return Scope != "Program" && consent.Checked; } }

        public RestoreOptionsForm(string backupDir)
        {
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
            cancel = UiTheme.Button("Отмена", delegate { if (busy) { cancellation.Cancel(); status.Text = "Отмена подготовки…"; } else DialogResult = DialogResult.Cancel; }, false);
            prepare = UiTheme.Button("Проверить и подготовить копию", async delegate { await PrepareCopy(); }, true);
            actions.Controls.Add(cancel); actions.Controls.Add(prepare); root.Controls.Add(actions, 0, 5);
            status.AccessibleName = "Состояние подготовки копии";
            consent.AccessibleDescription = "Разрешение заменить пользовательские данные. Смена состава восстановления сбрасывает подтверждение.";
            UiTheme.ConfigureKeyboardOrder(root);
            Controls.Add(root); CancelButton = cancel; // No destructive Enter default.
            var hasData = File.Exists(Path.Combine(source, "settings.json")) || File.Exists(Path.Combine(source, "vault.enc.json"));
            data.Enabled = all.Enabled = hasData;
            program.CheckedChanged += ChoiceChanged; data.CheckedChanged += ChoiceChanged; all.CheckedChanged += ChoiceChanged;
            consent.CheckedChanged += delegate { RefreshSelection(); };
            FormClosing += delegate(object sender, FormClosingEventArgs e) { if (busy) { e.Cancel = true; cancellation.Cancel(); status.Text = "Отмена подготовки…"; } };
            Shown += delegate { cancel.Focus(); };
            RefreshSelection();
        }

        private void ChoiceChanged(object sender, EventArgs e)
        { consent.Checked = false; RefreshSelection(); }

        private void RefreshSelection()
        {
            consent.Enabled = Scope != "Program" && !busy;
            try
            {
                // Preview may list data, but permission is checked separately before preparation.
                contents.Lines = BackupIntegrity.RestoreNames(source, Scope, true);
                prepare.Enabled = !busy && (Scope == "Program" || consent.Checked);
                status.Text = "Ниже — точный состав. Текущие VPN-доступы, снимки прокси и журналы сохранятся.\nАрхив VPN защищён DPAPI: это не перенос на другой ПК/пользователя; автоматического импорта нет.\nСначала проверим отдельную копию; ProGo пока останется запущенным.";
            }
            catch (Exception ex)
            {
                contents.Clear(); prepare.Enabled = false;
                status.Text = "Не удалось прочитать состав копии: " + ex.Message + "\nЗакройте окно и выберите доступную копию.";
            }
        }

        private async Task PrepareCopy()
        {
            if (busy || (Scope != "Program" && !consent.Checked)) return;
            busy = true; prepare.Enabled = false; program.Enabled = data.Enabled = all.Enabled = consent.Enabled = false;
            status.Text = "Проверяем файлы и подготавливаем отдельную копию. Можно отменить подготовку.";
            try
            {
                prepared = await Task.Run(() => BackupIntegrity.Prepare(source, cancellation.Token));
                cancellation.Token.ThrowIfCancellationRequested();
                BackupIntegrity.RestoreNames(prepared.Path, Scope, DataConfirmed);
                busy = false; DialogResult = DialogResult.OK;
            }
            catch (OperationCanceledException) { busy = false; DialogResult = DialogResult.Cancel; }
            catch (Exception ex)
            {
                busy = false; prepare.Enabled = false;
                status.Text = "Копия не подготовлена: " + ex.Message + "\nПриложение и подключения продолжают работать. Закройте окно и повторите после исправления.";
            }
        }

        internal PreparedBackup TakePreparedCopy()
        {
            if (DialogResult != DialogResult.OK || prepared == null) throw new InvalidOperationException("Копия ещё не подготовлена.");
            var result = prepared; prepared = null; return result;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !disposed)
            {
                disposed = true;
                cancellation.Cancel(); cancellation.Dispose();
                if (prepared != null) { prepared.Dispose(); prepared = null; }
            }
            base.Dispose(disposing);
        }
    }
}

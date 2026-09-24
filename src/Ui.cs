using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ProGo
{
    internal sealed class TrayApplicationContext : ApplicationContext
    {
        private readonly SettingsService settings;
        private readonly ProxyService proxy;
        private readonly ClipboardService clipboard;
        private readonly NotifyIcon tray;

        public TrayApplicationContext(SettingsService settingsService, ProxyService proxyService, ClipboardService clipboardService)
        {
            settings = settingsService;
            proxy = proxyService;
            clipboard = clipboardService;

            tray = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Text = AppConstants.ProductName,
                Visible = true,
                ContextMenuStrip = BuildMenu()
            };
            tray.DoubleClick += delegate { ShowStatus(); };
            UpdateTooltip();
        }

        private ContextMenuStrip BuildMenu()
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("Состояние", null, delegate { ShowStatus(); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Запустить SOCKS", null, delegate { proxy.StartTunnel(true); UpdateTooltip(); });
            menu.Items.Add("Остановить SOCKS", null, delegate { proxy.StopTunnel(); UpdateTooltip(); });
            menu.Items.Add("Перезапустить SOCKS", null, delegate { proxy.RestartTunnel(); UpdateTooltip(); });
            menu.Items.Add("Изменить порт SOCKS...", null, delegate { ChangePort(); });
            menu.Items.Add("Применить настройки прокси", null, delegate { EnvironmentProxyService.Apply(settings.Current); MessageBox.Show("Настройки прокси применены. Уже запущенным процессам может потребоваться перезапуск.", AppConstants.ProductName); });
            menu.Items.Add("Проверить соединение", null, delegate { MessageBox.Show(RouteTester.Test(settings.Current, proxy), "Проверка соединения"); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Хранилище секретов", null, delegate { ShowVault(); });
            menu.Items.Add("Настройки", null, delegate { ShowSettings(); });
            menu.Items.Add("Открыть журнал", null, delegate { OpenLog(); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Выход", null, delegate { ExitThread(); });
            return menu;
        }

        private void ShowStatus()
        {
            using (var form = new StatusForm(settings, proxy)) form.ShowDialog();
            UpdateTooltip();
        }

        private void ShowSettings()
        {
            using (var form = new SettingsForm(settings)) form.ShowDialog();
            UpdateTooltip();
        }

        private void ShowVault()
        {
            VaultSession session;
            if (!PinForm.OpenSession(out session)) return;
            using (var form = new VaultForm(session, clipboard, settings)) form.ShowDialog();
        }

        private void ChangePort()
        {
            int port;
            if (!PortForm.TryGetPort(settings.Current.SocksPort, out port)) return;
            var next = settings.Current;
            next.SocksPort = port;
            settings.Save(next);
            proxy.RestartTunnel();
            EnvironmentProxyService.Apply(settings.Current);
            MessageBox.Show("Порт SOCKS изменён на " + port + ".", "Порт SOCKS");
            UpdateTooltip();
        }

        private void OpenLog()
        {
            try
            {
                AppPaths.EnsureDirectories();
                if (!File.Exists(AppPaths.LogPath)) File.WriteAllText(AppPaths.LogPath, "");
                Process.Start("notepad.exe", AppPaths.LogPath);
            }
            catch (Exception ex)
            {
                SafeLog.Error("Open log failed.", ex);
                MessageBox.Show("Не удалось открыть журнал.", AppConstants.ProductName);
            }
        }

        private void UpdateTooltip()
        {
            tray.Text = AppConstants.ProductName + " — SOCKS: " + (proxy.IsListening() ? "работает" : "остановлен");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) tray.Dispose();
            base.Dispose(disposing);
        }
    }

    internal sealed class StatusForm : Form
    {
        private readonly SettingsService settings;
        private readonly ProxyService proxy;
        private readonly Label state;
        private readonly Label address;
        private readonly Label pid;
        private readonly Label env;
        private readonly Label route;
        private readonly Label checkedAt;

        public StatusForm(SettingsService settingsService, ProxyService proxyService)
        {
            settings = settingsService;
            proxy = proxyService;
            Text = "Состояние";
            AutoScaleMode = AutoScaleMode.Font;
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(520, 300);

            var table = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 2, RowCount = 8 };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            Controls.Add(table);

            state = AddRow(table, 0, "SOCKS-туннель");
            address = AddRow(table, 1, "Адрес");
            pid = AddRow(table, 2, "SSH-процесс");
            env = AddRow(table, 3, "Системный прокси");
            route = AddRow(table, 4, "Маршрут");
            checkedAt = AddRow(table, 5, "Последняя проверка");

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            var close = new Button { Text = "Закрыть", Width = 110, DialogResult = DialogResult.Cancel };
            var restart = new Button { Text = "Перезапустить SOCKS", Width = 160 };
            var check = new Button { Text = "Проверить снова", Width = 140 };
            restart.Click += delegate { proxy.RestartTunnel(); RefreshState(false); };
            check.Click += delegate { RefreshState(true); };
            buttons.Controls.Add(close);
            buttons.Controls.Add(restart);
            buttons.Controls.Add(check);
            table.Controls.Add(buttons, 0, 7);
            table.SetColumnSpan(buttons, 2);
            CancelButton = close;
            RefreshState(false);
        }

        private static Label AddRow(TableLayoutPanel table, int row, string name)
        {
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            table.Controls.Add(new Label { Text = name, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
            var value = new Label { Text = "—", AutoSize = true, Anchor = AnchorStyles.Left };
            table.Controls.Add(value, 1, row);
            return value;
        }

        private void RefreshState(bool testRoute)
        {
            state.Text = proxy.IsListening() ? "Работает" : "Остановлен";
            address.Text = settings.Current.SocksHost + ":" + settings.Current.SocksPort;
            pid.Text = proxy.CurrentPid.HasValue ? "PID " + proxy.CurrentPid.Value : "Не запущен этим экземпляром";
            env.Text = Environment.GetEnvironmentVariable("ALL_PROXY", EnvironmentVariableTarget.User) ?? "Не настроен";
            if (testRoute) route.Text = RouteTester.Test(settings.Current, proxy);
            checkedAt.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }

    internal sealed class SettingsForm : Form
    {
        private readonly SettingsService service;
        private readonly TextBox host = new TextBox();
        private readonly NumericUpDown port = new NumericUpDown();
        private readonly TextBox ssh = new TextBox();
        private readonly CheckBox autoStart = new CheckBox();
        private readonly CheckBox autoProxy = new CheckBox();
        private readonly NumericUpDown clearSeconds = new NumericUpDown();
        private readonly TextBox endpoint = new TextBox();

        public SettingsForm(SettingsService settingsService)
        {
            service = settingsService;
            Text = "Настройки";
            AutoScaleMode = AutoScaleMode.Font;
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(620, 420);

            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 2, RowCount = 12 };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            Controls.Add(panel);

            AddHeader(panel, 0, "Соединение");
            AddLabeled(panel, 1, "Адрес SOCKS", host);
            port.Minimum = 1; port.Maximum = 65535;
            AddLabeled(panel, 2, "Порт SOCKS", port);
            AddLabeled(panel, 3, "SSH-профиль", ssh);
            autoStart.Text = "Запускать SOCKS вместе с ProGo";
            panel.Controls.Add(autoStart, 1, 4);
            autoProxy.Text = "Автоматически применять настройки прокси";
            panel.Controls.Add(autoProxy, 1, 5);
            AddHeader(panel, 6, "Безопасность");
            clearSeconds.Minimum = 5; clearSeconds.Maximum = 3600;
            AddLabeled(panel, 7, "Очищать буфер через, сек.", clearSeconds);
            AddHeader(panel, 8, "Диагностика");
            AddLabeled(panel, 9, "Адрес проверки", endpoint);
            var logPath = new TextBox { ReadOnly = true, Text = AppPaths.LogPath };
            AddLabeled(panel, 10, "Журнал", logPath);

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            var save = new Button { Text = "Сохранить", Width = 110, DialogResult = DialogResult.OK };
            var cancel = new Button { Text = "Отмена", Width = 110, DialogResult = DialogResult.Cancel };
            save.Click += Save;
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(save);
            panel.Controls.Add(buttons, 0, 11);
            panel.SetColumnSpan(buttons, 2);
            AcceptButton = save;
            CancelButton = cancel;
            LoadValues();
        }

        private static void AddHeader(TableLayoutPanel panel, int row, string text)
        {
            var label = new Label { Text = text, Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold), AutoSize = true, Anchor = AnchorStyles.Left };
            panel.Controls.Add(label, 0, row);
            panel.SetColumnSpan(label, 2);
        }

        private static void AddLabeled(TableLayoutPanel panel, int row, string label, Control control)
        {
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            panel.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
            control.Dock = DockStyle.Fill;
            panel.Controls.Add(control, 1, row);
        }

        private void LoadValues()
        {
            var s = service.Current;
            host.Text = s.SocksHost;
            port.Value = s.SocksPort;
            ssh.Text = s.SshProfile ?? "";
            autoStart.Checked = s.AutoStartSocks;
            autoProxy.Checked = s.AutoApplyProxy;
            clearSeconds.Value = s.ClipboardClearSeconds;
            endpoint.Text = s.TestEndpoint;
        }

        private void Save(object sender, EventArgs e)
        {
            service.Save(new AppSettings
            {
                SocksHost = host.Text.Trim(),
                SocksPort = (int)port.Value,
                SshProfile = ssh.Text.Trim(),
                AutoStartSocks = autoStart.Checked,
                AutoApplyProxy = autoProxy.Checked,
                ClipboardClearSeconds = (int)clearSeconds.Value,
                TestEndpoint = endpoint.Text.Trim()
            });
            Close();
        }
    }

    internal sealed class VaultForm : Form
    {
        private readonly VaultSession session;
        private readonly ClipboardService clipboard;
        private readonly SettingsService settings;
        private readonly TextBox search = new TextBox();
        private readonly ComboBox typeFilter = new ComboBox();
        private readonly DataGridView grid = new DataGridView();

        public VaultForm(VaultSession vaultSession, ClipboardService clipboardService, SettingsService settingsService)
        {
            session = vaultSession;
            clipboard = clipboardService;
            settings = settingsService;
            Text = "Хранилище секретов";
            AutoScaleMode = AutoScaleMode.Font;
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(860, 520);

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), RowCount = 3, ColumnCount = 1 };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            Controls.Add(root);

            var filters = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
            filters.Controls.Add(new Label { Text = "Поиск", AutoSize = true, Padding = new Padding(0, 7, 0, 0) });
            search.Width = 300;
            search.TextChanged += delegate { Reload(); };
            filters.Controls.Add(search);
            filters.Controls.Add(new Label { Text = "Фильтр по типу", AutoSize = true, Padding = new Padding(12, 7, 0, 0) });
            typeFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            typeFilter.Items.AddRange(new object[] { "Все", "API-ключ", "Пароль", "Токен", "SSH", "Заметка", "Другое" });
            typeFilter.SelectedIndex = 0;
            typeFilter.SelectedIndexChanged += delegate { Reload(); };
            filters.Controls.Add(typeFilter);
            root.Controls.Add(filters, 0, 0);

            grid.Dock = DockStyle.Fill;
            grid.ReadOnly = true;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.DoubleClick += delegate { EditSelected(); };
            root.Controls.Add(grid, 0, 1);

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            var close = new Button { Text = "Закрыть", Width = 100, DialogResult = DialogResult.Cancel };
            var lockButton = new Button { Text = "Заблокировать", Width = 120, DialogResult = DialogResult.Cancel };
            var copy = new Button { Text = "Копировать секрет", Width = 150 };
            var delete = new Button { Text = "Удалить", Width = 100 };
            var edit = new Button { Text = "Изменить", Width = 100 };
            var add = new Button { Text = "Добавить", Width = 100 };
            add.Click += delegate { AddEntry(); };
            edit.Click += delegate { EditSelected(); };
            delete.Click += delegate { DeleteSelected(); };
            copy.Click += delegate { CopySelected(); };
            buttons.Controls.Add(close);
            buttons.Controls.Add(lockButton);
            buttons.Controls.Add(copy);
            buttons.Controls.Add(delete);
            buttons.Controls.Add(edit);
            buttons.Controls.Add(add);
            root.Controls.Add(buttons, 0, 2);
            CancelButton = close;
            Reload();
        }

        private void Reload()
        {
            grid.Columns.Clear();
            grid.Rows.Clear();
            grid.Columns.Add("name", "Название");
            grid.Columns.Add("type", "Тип");
            grid.Columns.Add("login", "Логин");
            grid.Columns.Add("host", "Сайт / сервер");
            grid.Columns.Add("tags", "Метки");
            grid.Columns.Add("updated", "Изменено");

            var q = search.Text.Trim().ToLowerInvariant();
            foreach (var entry in session.Data.entries.OrderByDescending(e => e.updated_at))
            {
                if (!MatchesSearch(entry, q) || !MatchesType(entry)) continue;
                var row = grid.Rows.Add(entry.name, DisplayType(entry.type), entry.login, entry.url_or_host, entry.tags, entry.updated_at);
                grid.Rows[row].Tag = entry;
            }
        }

        private bool MatchesType(VaultEntry entry)
        {
            if (typeFilter.SelectedIndex <= 0) return true;
            return DisplayType(entry.type) == typeFilter.SelectedItem.ToString();
        }

        private static bool MatchesSearch(VaultEntry entry, string q)
        {
            if (String.IsNullOrEmpty(q)) return true;
            var hay = String.Join(" ", new[] { entry.name, entry.login, entry.url_or_host, entry.tags, entry.notes }).ToLowerInvariant();
            return hay.IndexOf(q, StringComparison.Ordinal) >= 0;
        }

        private VaultEntry SelectedEntry()
        {
            if (grid.SelectedRows.Count == 0) return null;
            return grid.SelectedRows[0].Tag as VaultEntry;
        }

        private void AddEntry()
        {
            var entry = VaultEntry.New();
            using (var form = new EntryForm(entry))
            {
                if (form.ShowDialog() != DialogResult.OK) return;
                session.Data.entries.Add(form.Entry);
                VaultService.Save(session);
                Reload();
            }
        }

        private void EditSelected()
        {
            var selected = SelectedEntry();
            if (selected == null) return;
            using (var form = new EntryForm(selected.Clone()))
            {
                if (form.ShowDialog() != DialogResult.OK) return;
                var index = session.Data.entries.FindIndex(e => e.id == selected.id);
                if (index >= 0) session.Data.entries[index] = form.Entry;
                VaultService.Save(session);
                Reload();
            }
        }

        private void DeleteSelected()
        {
            var selected = SelectedEntry();
            if (selected == null) return;
            var answer = MessageBox.Show("Удалить запись «" + selected.name + "»?\nЭто действие нельзя отменить.", "Удаление записи", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
            if (answer != DialogResult.OK) return;
            session.Data.entries.RemoveAll(e => e.id == selected.id);
            VaultService.Save(session);
            Reload();
        }

        private void CopySelected()
        {
            var selected = SelectedEntry();
            if (selected == null) return;
            clipboard.CopySecret(selected.secret);
            MessageBox.Show("Секрет скопирован. Буфер обмена будет очищен через " + settings.Current.ClipboardClearSeconds + " сек.", "Хранилище секретов");
        }

        internal static string DisplayType(string type)
        {
            switch ((type ?? "").ToLowerInvariant())
            {
                case "api_key": return "API-ключ";
                case "password": return "Пароль";
                case "token": return "Токен";
                case "ssh": return "SSH";
                case "note": return "Заметка";
                case "custom": return "Другое";
                default: return "Другое";
            }
        }

        internal static string InternalType(string display)
        {
            switch (display)
            {
                case "API-ключ": return "api_key";
                case "Пароль": return "password";
                case "Токен": return "token";
                case "SSH": return "ssh";
                case "Заметка": return "note";
                default: return "custom";
            }
        }
    }

    internal sealed class EntryForm : Form
    {
        private readonly TextBox name = new TextBox();
        private readonly ComboBox type = new ComboBox();
        private readonly TextBox login = new TextBox();
        private readonly TextBox secret = new TextBox();
        private readonly TextBox host = new TextBox();
        private readonly TextBox tags = new TextBox();
        private readonly TextBox notes = new TextBox();
        public VaultEntry Entry { get; private set; }

        public EntryForm(VaultEntry entry)
        {
            Entry = entry;
            Text = String.IsNullOrEmpty(entry.name) ? "Новая запись" : "Редактирование записи";
            AutoScaleMode = AutoScaleMode.Font;
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(560, 520);

            var table = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 2, RowCount = 9 };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            Controls.Add(table);

            type.DropDownStyle = ComboBoxStyle.DropDownList;
            type.Items.AddRange(new object[] { "API-ключ", "Пароль", "Токен", "SSH", "Заметка", "Другое" });
            secret.UseSystemPasswordChar = true;
            notes.Multiline = true;
            notes.Height = 80;

            Add(table, 0, "Название", name);
            Add(table, 1, "Тип", type);
            Add(table, 2, "Логин", login);
            Add(table, 3, "Секрет", secret);
            Add(table, 4, "Сайт / сервер", host);
            Add(table, 5, "Метки", tags);
            Add(table, 6, "Заметки", notes);

            var show = new CheckBox { Text = "Показать секрет", AutoSize = true };
            show.CheckedChanged += delegate { secret.UseSystemPasswordChar = !show.Checked; };
            table.Controls.Add(show, 1, 7);

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            var save = new Button { Text = "Сохранить", Width = 110, DialogResult = DialogResult.OK };
            var cancel = new Button { Text = "Отмена", Width = 110, DialogResult = DialogResult.Cancel };
            save.Click += Save;
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(save);
            table.Controls.Add(buttons, 0, 8);
            table.SetColumnSpan(buttons, 2);
            AcceptButton = save;
            CancelButton = cancel;
            LoadEntry(entry);
        }

        private static void Add(TableLayoutPanel table, int row, string label, Control control)
        {
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, row == 6 ? 92 : 32));
            table.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
            control.Dock = DockStyle.Fill;
            table.Controls.Add(control, 1, row);
        }

        private void LoadEntry(VaultEntry entry)
        {
            name.Text = entry.name ?? "";
            type.SelectedItem = VaultForm.DisplayType(entry.type);
            if (type.SelectedIndex < 0) type.SelectedItem = "Другое";
            login.Text = entry.login ?? "";
            secret.Text = entry.secret ?? "";
            host.Text = entry.url_or_host ?? "";
            tags.Text = entry.tags ?? "";
            notes.Text = entry.notes ?? "";
        }

        private void Save(object sender, EventArgs e)
        {
            if (String.IsNullOrWhiteSpace(name.Text))
            {
                MessageBox.Show("Укажите название записи.", "Проверьте заполненные поля.");
                DialogResult = DialogResult.None;
                return;
            }
            var internalType = VaultForm.InternalType(type.SelectedItem == null ? "Другое" : type.SelectedItem.ToString());
            if (internalType != "note" && String.IsNullOrEmpty(secret.Text))
            {
                MessageBox.Show("Укажите секрет.", "Проверьте заполненные поля.");
                DialogResult = DialogResult.None;
                return;
            }

            Entry.name = name.Text.Trim();
            Entry.type = internalType;
            Entry.login = login.Text.Trim();
            Entry.secret = secret.Text;
            Entry.url_or_host = host.Text.Trim();
            Entry.tags = tags.Text.Trim();
            Entry.notes = notes.Text;
            if (String.IsNullOrEmpty(Entry.created_at)) Entry.created_at = DateTimeOffset.UtcNow.ToString("o");
            Entry.updated_at = DateTimeOffset.UtcNow.ToString("o");
        }
    }

    internal sealed class PinForm : Form
    {
        private readonly TextBox pin = new TextBox();
        private readonly TextBox confirm = new TextBox();
        private readonly bool create;
        public string Pin { get { return pin.Text; } }

        public PinForm(bool createVault)
        {
            create = createVault;
            Text = create ? "Создание хранилища" : "Разблокировка";
            AutoScaleMode = AutoScaleMode.Font;
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(360, create ? 210 : 170);

            var table = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 2, RowCount = create ? 4 : 3 };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            Controls.Add(table);
            pin.UseSystemPasswordChar = true;
            confirm.UseSystemPasswordChar = true;
            Add(table, 0, "PIN-код", pin);
            if (create) Add(table, 1, "Повторите PIN-код", confirm);
            var hint = new Label { Text = "PIN-код должен содержать ровно 4 цифры.", AutoSize = true };
            table.Controls.Add(hint, 0, create ? 2 : 1);
            table.SetColumnSpan(hint, 2);

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            var ok = new Button { Text = create ? "Создать" : "Открыть", Width = 100, DialogResult = DialogResult.OK };
            var cancel = new Button { Text = "Отмена", Width = 100, DialogResult = DialogResult.Cancel };
            ok.Click += ValidatePin;
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(ok);
            table.Controls.Add(buttons, 0, create ? 3 : 2);
            table.SetColumnSpan(buttons, 2);
            AcceptButton = ok;
            CancelButton = cancel;
        }

        private static void Add(TableLayoutPanel table, int row, string label, Control control)
        {
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            table.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
            control.Dock = DockStyle.Fill;
            table.Controls.Add(control, 1, row);
        }

        private void ValidatePin(object sender, EventArgs e)
        {
            if (pin.Text.Length != 4 || !pin.Text.All(Char.IsDigit))
            {
                MessageBox.Show("PIN-код должен содержать ровно 4 цифры.", Text);
                DialogResult = DialogResult.None;
                return;
            }
            if (create && pin.Text != confirm.Text)
            {
                MessageBox.Show("PIN-коды не совпадают.", Text);
                DialogResult = DialogResult.None;
            }
        }

        public static bool OpenSession(out VaultSession session)
        {
            session = null;
            var create = !VaultService.Exists();
            using (var form = new PinForm(create))
            {
                if (form.ShowDialog() != DialogResult.OK) return false;
                try
                {
                    session = create ? VaultService.Create(form.Pin) : VaultService.Open(form.Pin);
                    return true;
                }
                catch (Exception ex)
                {
                    SafeLog.Error("Vault unlock/create failed.", ex);
                    MessageBox.Show("Не удалось открыть хранилище.", AppConstants.ProductName);
                    return false;
                }
            }
        }
    }

    internal sealed class PortForm : Form
    {
        private readonly NumericUpDown port = new NumericUpDown();
        public int Port { get { return (int)port.Value; } }

        private PortForm(int current)
        {
            Text = "Порт SOCKS";
            AutoScaleMode = AutoScaleMode.Font;
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(360, 150);
            port.Minimum = 1;
            port.Maximum = 65535;
            port.Value = current >= 1 && current <= 65535 ? current : 1080;

            var table = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 1, RowCount = 3 };
            Controls.Add(table);
            table.Controls.Add(new Label { Text = "Укажите локальный порт SOCKS:", AutoSize = true }, 0, 0);
            port.Dock = DockStyle.Fill;
            table.Controls.Add(port, 0, 1);
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            var ok = new Button { Text = "Сохранить", Width = 110, DialogResult = DialogResult.OK };
            var cancel = new Button { Text = "Отмена", Width = 110, DialogResult = DialogResult.Cancel };
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(ok);
            table.Controls.Add(buttons, 0, 2);
            AcceptButton = ok;
            CancelButton = cancel;
        }

        public static bool TryGetPort(int current, out int value)
        {
            using (var form = new PortForm(current))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    value = form.Port;
                    return true;
                }
            }
            value = current;
            return false;
        }
    }
}

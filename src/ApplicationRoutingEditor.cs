using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ProGo
{
    // Editor is deliberately separate from the routing engine. Editing does not
    // touch network state; settings are committed only by the parent Save button.
    internal sealed class ApplicationRoutingEditor : UserControl
    {
        private readonly DataGridView table = new DataGridView();
        private readonly TextBox search = new TextBox();
        private readonly TextBox enginePath = new TextBox();
        private readonly CheckBox enabled = new CheckBox();
        private readonly Label hint = new Label();
        private readonly Dictionary<string, DiscoveredApplication> discovered =
            new Dictionary<string, DiscoveredApplication>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> modes =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> pinned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private bool loading;

        public bool SmartRoutingEnabled { get { return enabled.Checked; } }
        public string EnginePath { get { return enginePath.Text.Trim(); } }

        public ApplicationRoutingEditor()
        {
            Dock = DockStyle.Fill;
            BackColor = UiTheme.Background;
            var layout = new TableLayoutPanel {
                Dock = DockStyle.Fill, Padding = new Padding(12),
                ColumnCount = 1, RowCount = 7, BackColor = UiTheme.Background
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            Controls.Add(layout);

            var heading = UiTheme.Label("Правила приложений — Авто / VPS / Прямо", UiTheme.Strong, UiTheme.Text);
            heading.Dock = DockStyle.Fill;
            layout.Controls.Add(heading, 0, 0);

            var description = UiTheme.Label("Авто: проверяемые сервисы (OpenAI, GitHub, Claude) переключаются после повторных проверок. Остальные — напрямую. VPS: TCP через SSH; UDP запрещён. Ручные правила важнее авто.", UiTheme.Body, UiTheme.Muted);
            description.Dock = DockStyle.Fill;
            layout.Controls.Add(description, 0, 1);

            search.Dock = DockStyle.Fill;
            search.TextChanged += delegate { RefreshRows(); };
            var searchPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
            searchPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 78));
            searchPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            searchPanel.Controls.Add(UiTheme.Label("Поиск:", UiTheme.Body, UiTheme.Muted), 0, 0);
            searchPanel.Controls.Add(search, 1, 0);
            layout.Controls.Add(searchPanel, 0, 2);

            table.Dock = DockStyle.Fill;
            table.AllowUserToAddRows = false;
            table.AllowUserToDeleteRows = false;
            table.AllowUserToResizeRows = false;
            table.MultiSelect = false;
            table.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            table.RowHeadersVisible = false;
            table.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            table.BackgroundColor = UiTheme.Background;
            table.ForeColor = UiTheme.Text;
            table.GridColor = UiTheme.Muted;
            table.BorderStyle = BorderStyle.None;
            table.EnableHeadersVisualStyles = false;
            table.ColumnHeadersDefaultCellStyle.BackColor = UiTheme.Field;
            table.ColumnHeadersDefaultCellStyle.ForeColor = UiTheme.Text;
            table.DefaultCellStyle.BackColor = UiTheme.Background;
            table.DefaultCellStyle.ForeColor = UiTheme.Text;
            table.DefaultCellStyle.SelectionBackColor = UiTheme.Field;
            table.DefaultCellStyle.SelectionForeColor = UiTheme.Text;
            table.Columns.Add(new DataGridViewTextBoxColumn {
                HeaderText = "Программа", ReadOnly = true, FillWeight = 32
            });
            table.Columns.Add(new DataGridViewTextBoxColumn {
                HeaderText = "Путь к .exe", ReadOnly = true, FillWeight = 55
            });
            var modeColumn = new DataGridViewComboBoxColumn {
                HeaderText = "Маршрут", FillWeight = 22, FlatStyle = FlatStyle.Flat
            };
            modeColumn.Items.AddRange("Авто", "VPS", "Прямо");
            table.Columns.Add(modeColumn);
            table.CurrentCellDirtyStateChanged += delegate {
                if (table.IsCurrentCellDirty) table.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            table.CellValueChanged += OnModeChanged;
            table.DataError += delegate(object sender, DataGridViewDataErrorEventArgs args) {
                args.ThrowException = false;
            };
            layout.Controls.Add(table, 0, 3);

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
            buttons.Controls.Add(UiTheme.Button("Выбрать программу…", delegate { ChooseExecutable(); }, true));
            buttons.Controls.Add(UiTheme.Button("Обновить список", delegate { DiscoverApps(); }, false));
            buttons.Controls.Add(UiTheme.Button("Убрать правило", delegate { RemoveSelected(); }, false));
            layout.Controls.Add(buttons, 0, 4);

            var enginePanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3 };
            enginePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            enginePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            enginePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 125));
            enginePanel.Controls.Add(UiTheme.Label("TUN-движок:", UiTheme.Body, UiTheme.Muted), 0, 0);
            enginePath.Dock = DockStyle.Fill;
            enginePanel.Controls.Add(enginePath, 1, 0);
            enginePanel.Controls.Add(UiTheme.Button("Выбрать .exe", delegate { ChooseEngine(); }, false), 2, 0);
            layout.Controls.Add(enginePanel, 0, 5);

            var footer = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
            enabled.Text = "Умная маршрутизация при запуске ProGo (первоначальная настройка требует UAC)";
            enabled.AutoSize = true;
            footer.Controls.Add(enabled);
            hint.Text = "Для запуска без прав администратора установите защищённую задачу из scripts/Install-SmartRoutingEngine.ps1. Глобальный HTTP-прокси должен быть выключен.";
            hint.ForeColor = UiTheme.Muted;
            hint.AutoSize = true;
            footer.Controls.Add(hint);
            layout.Controls.Add(footer, 0, 6);
        }

        public void LoadSettings(AppSettings settings)
        {
            loading = true;
            try
            {
                modes.Clear();
                pinned.Clear();
                discovered.Clear();
                enabled.Checked = settings.SmartRoutingEnabled;
                enginePath.Text = String.IsNullOrWhiteSpace(settings.SmartRoutingEnginePath)
                    ? SmartRoutingEngine.DefaultEnginePath : settings.SmartRoutingEnginePath;

                foreach (var rule in ApplicationRouting.Normalize(settings.ApplicationRoutes))
                {
                    pinned.Add(rule.ExecutablePath);
                    modes[rule.ExecutablePath] = rule.Mode;
                    discovered[rule.ExecutablePath] = new DiscoveredApplication {
                        ExecutablePath = rule.ExecutablePath,
                        Name = Path.GetFileNameWithoutExtension(rule.ExecutablePath)
                    };
                }
                DiscoverApps();
            }
            finally { loading = false; }
            RefreshRows();
        }

        public List<ApplicationRouteRule> GetRules()
        {
            return ApplicationRouting.Normalize(
                pinned.Select(path => new ApplicationRouteRule {
                    ExecutablePath = path,
                    Mode = modes.ContainsKey(path) ? modes[path] : ApplicationRouting.Auto
                }).ToList());
        }

        private void DiscoverApps()
        {
            foreach (var app in ApplicationRouting.Discover())
                if (!discovered.ContainsKey(app.ExecutablePath))
                    discovered[app.ExecutablePath] = app;
            RefreshRows();
        }

        private void RefreshRows()
        {
            if (loading) return;
            loading = true;
            try
            {
                var query = search.Text.Trim();
                table.Rows.Clear();
                foreach (var app in discovered.Values.OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase))
                {
                    if (query.Length != 0 && app.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0
                        && app.ExecutablePath.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    string mode;
                    if (!modes.TryGetValue(app.ExecutablePath, out mode)) mode = ApplicationRouting.Auto;
                    var index = table.Rows.Add(app.Name, app.ExecutablePath, DisplayMode(mode));
                    table.Rows[index].Tag = app.ExecutablePath;
                    if (!File.Exists(app.ExecutablePath)) table.Rows[index].DefaultCellStyle.ForeColor = UiTheme.Muted;
                }
            }
            finally { loading = false; }
        }

        private static string DisplayMode(string mode)
        {
            return mode == ApplicationRouting.Proxy ? "VPS" : mode == ApplicationRouting.Direct ? "Прямо" : "Авто";
        }

        private void OnModeChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (loading || e.RowIndex < 0 || e.ColumnIndex != 2) return;
            var path = table.Rows[e.RowIndex].Tag as string;
            if (path == null) return;
            var label = Convert.ToString(table.Rows[e.RowIndex].Cells[2].Value);
            var mode = label == "VPS" ? ApplicationRouting.Proxy
                : label == "Прямо" ? ApplicationRouting.Direct : ApplicationRouting.Auto;
            if (mode == ApplicationRouting.Proxy && ApplicationRouting.IsProtectedPath(path))
            {
                MessageBox.Show("Системные процессы и компоненты ProGo нельзя принудительно направлять через VPS.", "Маршрутизация", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                mode = ApplicationRouting.Direct;
                loading = true;
                try { table.Rows[e.RowIndex].Cells[2].Value = DisplayMode(mode); }
                finally { loading = false; }
            }
            pinned.Add(path);
            modes[path] = mode;
        }

        private void ChooseExecutable()
        {
            using (var dialog = new OpenFileDialog {
                Title = "Выберите исполняемый файл программы",
                Filter = "Программы (*.exe)|*.exe",
                CheckFileExists = true,
                Multiselect = false
            })
            {
                if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;
                var path = ApplicationRouting.NormalizePath(dialog.FileName);
                if (path == null || ApplicationRouting.IsProtectedPath(path))
                {
                    MessageBox.Show("Нельзя добавить системный компонент или процесс ProGo.", "Маршрутизация", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                discovered[path] = new DiscoveredApplication { Name = Path.GetFileNameWithoutExtension(path), ExecutablePath = path };
                pinned.Add(path);
                if (!modes.ContainsKey(path)) modes[path] = ApplicationRouting.Auto;
                search.Text = "";
                RefreshRows();
                foreach (DataGridViewRow row in table.Rows)
                    if (String.Equals(Convert.ToString(row.Tag), path, StringComparison.OrdinalIgnoreCase))
                    {
                        row.Selected = true;
                        table.FirstDisplayedScrollingRowIndex = row.Index;
                        break;
                    }
            }
        }

        private void ChooseEngine()
        {
            using (var dialog = new OpenFileDialog {
                Title = "Выберите официальный sing-box.exe",
                Filter = "sing-box.exe|sing-box.exe",
                CheckFileExists = true
            })
                if (dialog.ShowDialog(FindForm()) == DialogResult.OK)
                    enginePath.Text = dialog.FileName;
        }

        private void RemoveSelected()
        {
            if (table.CurrentRow == null) return;
            var path = table.CurrentRow.Tag as string;
            if (path == null) return;
            pinned.Remove(path);
            modes.Remove(path);
            // Remove from the displayed catalog only when it is no longer installed/running.
            discovered.Remove(path);
            RefreshRows();
        }
    }
}

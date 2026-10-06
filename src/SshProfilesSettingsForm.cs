using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ProGo
{
    internal enum SettingsSection { Automation, Connections, Windows }
    internal sealed class SshProfilesSettingsForm : ProGoForm
    {
        private readonly SettingsService service;
        private readonly TextBox host = new TextBox();
        private readonly NumericUpDown port = new NumericUpDown();
        private readonly ComboBox sshProfiles = new ComboBox();
        private readonly CheckBox autoSwitchProfile = new CheckBox();
        private readonly CheckBox autoStart = new CheckBox();
        private readonly CheckBox autoLaunch = new CheckBox();
        private readonly Label startupNotice = UiTheme.Label("", UiTheme.Body, UiTheme.Muted);
        private readonly ApplicationShortcuts startupShortcuts;
        private ApplicationShortcuts.StartupSnapshot startupSnapshot;
        private Button startupSettingsButton;
        private readonly CheckBox autoCli = new CheckBox();
        private readonly NumericUpDown clearSeconds = new NumericUpDown();
        private readonly TextBox endpoint = new TextBox();
        private readonly List<SshProfileSetting> profiles = new List<SshProfileSetting>();

        private readonly CheckBox autoRestart = new CheckBox();
        private readonly CheckBox autoWindows = new CheckBox();
        public event Action<string> ManualActionRequested;
        public Func<AppSettings, bool, SettingsSaveError> SaveRequested;
        private readonly CheckBox autoHttpPort = new CheckBox();
        private readonly NumericUpDown httpPort = new NumericUpDown();
        private readonly TextBox proxyAddress = new TextBox { ReadOnly = true };
        private readonly Label portNotice = UiTheme.Label("", UiTheme.Body, UiTheme.Muted);
        private TabControl settingsTabs;
        private bool pickFreePort;
        private int initialHttpPort;
        private readonly Timer currentValuesTimer = new Timer { Interval = 500 };
        private readonly Label currentAutomation = UiTheme.Label("", UiTheme.Body, UiTheme.Muted);
        private readonly Label currentConnection = UiTheme.Label("", UiTheme.Body, UiTheme.Muted);
        private readonly Label currentDiagnostic = UiTheme.Label("", UiTheme.Body, UiTheme.Muted);
        private Button pickPortButton;
        private readonly Label saveError = UiTheme.Label("", UiTheme.Body, UiTheme.Error);
        public Func<string> CurrentProxyEndpoint;
        public string ProxyEndpointText { set { proxyAddress.Text = value; } }


        public SshProfilesSettingsForm(SettingsService settingsService, SettingsSection section = SettingsSection.Automation, ApplicationShortcuts shortcuts = null, Action openStartupSettings = null)
        {
            service = settingsService;
            startupShortcuts = shortcuts;
            if (startupShortcuts == null && String.Equals(Application.ExecutablePath,
                System.IO.Path.Combine(AppPaths.Root, "ProGo.exe"), StringComparison.OrdinalIgnoreCase))
                startupShortcuts = new ApplicationShortcuts(AppPaths.Root,
                    Environment.GetFolderPath(Environment.SpecialFolder.Programs), Environment.GetFolderPath(Environment.SpecialFolder.Startup));
            Text = "Настройки · ProGo";
            ClientSize = new Size(900, 700); MinimumSize = new Size(850, 650);
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), ColumnCount = 1, RowCount = 5 };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.Controls.Add(UiTheme.Label("Под ваш ритм", UiTheme.Title, UiTheme.Text), 0, 0);
            root.Controls.Add(UiTheme.Label("Поля и галочки — после «Сохранить».\nРучные команды — сразу; «Отменить изменения» их не откатывает.", UiTheme.Body, UiTheme.Muted), 0, 1);
            var tabs = new ProGoTabs { Dock = DockStyle.Fill, ItemSize = new Size(153, 38), SizeMode = TabSizeMode.Fixed, Multiline = true };
            root.Controls.Add(tabs, 0, 2); settingsTabs = tabs;
            var automation = Page(tabs, "Автоматика");
            var autoFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, AutoScrollMargin = new Size(0, 18), FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(14) };
            automation.Controls.Add(autoFlow);
            bool fittingAutomation = false;
            Action fitAutomation = delegate {
                if (fittingAutomation || autoFlow.ClientSize.Width < 1) return;
                fittingAutomation = true;
                try {
                    // Reserve a vertical scrollbar gutter; changing text must not oscillate widths.
                    int width = Math.Max(1, autoFlow.ClientSize.Width - autoFlow.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth);
                    foreach (Control child in autoFlow.Controls) {
                        int available = Math.Max(1, width - child.Margin.Horizontal);
                        var label = child as Label;
                        if (label != null) label.MaximumSize = new Size(available, 0);
                        var card = child as SurfacePanel;
                        if (card == null) continue;
                        card.MaximumSize = new Size(available, 0);
                        card.MinimumSize = new Size(available, 0);
                    }
                } finally { fittingAutomation = false; }
            };
            autoFlow.ClientSizeChanged += delegate { fitAutomation(); };
            autoFlow.Layout += delegate { fitAutomation(); };
            autoFlow.Controls.Add(UiTheme.Label("Предпочтения: галочки начинают действовать после сохранения.", UiTheme.Body, UiTheme.Muted));
            currentAutomation.Name = "currentAutomationSettings";
            currentAutomation.MaximumSize = new Size(740, 0);
            autoFlow.Controls.Add(currentAutomation);
            AutomationCard(autoFlow, autoRestart, "Восстанавливать подключение при обрыве", "Повторять соединение, если туннель перестал работать. После вашей команды отключения он сам не включится.", "Перезапустить", "restart", "Отключить прокси на ПК", "stop");
            AutomationCard(autoFlow, autoCli, "Включать прокси для терминалов и Codex", "Включать общий прокси после подключения ProGo. Затем откройте новый терминал или перезапустите уже открытый Codex. Отдельный ярлык не нужен.", "Включить", "cli-start", "Выключить", "cli-off");
            AutomationCard(autoFlow, autoWindows, "Включать прокси для приложений Windows", "Применять системный прокси при запуске ProGo. Работает для приложений, которые используют настройки прокси Windows.", "Включить", "windows-on", "Выключить", "windows-off");
            var connection = FormTable(Page(tabs, "Подключение"));
            sshProfiles.DropDownStyle = ComboBoxStyle.DropDownList;
            AddLabeled(connection, 0, "Сервер", sshProfiles);
            var actions = SettingsActions();
            actions.Controls.Add(UiTheme.Button("Добавить", delegate { AddProfile(); }, false));
            actions.Controls.Add(UiTheme.Button("Изменить", delegate { EditProfile(); }, false));
            actions.Controls.Add(UiTheme.Button("Удалить", delegate { RemoveProfile(); }, false));
            actions.Controls.Add(UiTheme.Button("Проверить", delegate { CheckSelectedProfile(); }, false));
            actions.Controls.Add(UiTheme.Button("Первый вход", delegate {
                var selected = SelectedProfile();
                if (selected == null) { MessageBox.Show(this, "Сначала добавьте и выберите сервер.", "Первый вход SSH"); return; }
                try { SshInteractiveLogin.Open(selected); }
                catch (Exception ex) { MessageBox.Show(this, ex.Message, "Первый вход SSH", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            }, false));
            AddSettingsRow(connection, 1, actions);
            AddLabeled(connection, 2, "Адрес SOCKS-туннеля", host);
            port.Minimum = 1; port.Maximum = 65535; AddLabeled(connection, 3, "Порт SOCKS-туннеля", port);
            autoStart.Text = "Подключаться к серверу при запуске ProGo"; autoStart.AutoSize = true;
            autoSwitchProfile.Text = "Пробовать другой сервер при недоступности"; autoSwitchProfile.AutoSize = true;
            autoLaunch.Text = "Запускать ProGo при входе в Windows";
            autoLaunch.AccessibleDescription = "Применяется после сохранения. Запуск приложения и подключение к серверу настраиваются отдельно.";
            AddSettingsRow(connection, 4, autoLaunch);
            AddSettingsRow(connection, 5, startupNotice);
            var startupActions = SettingsActions();
            startupSettingsButton = UiTheme.Button("Автозагрузка в Windows…", delegate {
                try {
                    if (openStartupSettings != null) openStartupSettings();
                    else System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("ms-settings:startupapps") { UseShellExecute = true });
                } catch {
                    ShowSaveError(new SettingsSaveError(SettingsField.Startup, "Не удалось открыть параметры Windows. Откройте «Параметры» → «Приложения» → «Автозагрузка»."));
                }
            }, false);
            startupSettingsButton.AccessibleDescription = "Открывает параметры Windows. Изменения в этом окне ProGo не сохраняются.";
            startupActions.Controls.Add(startupSettingsButton);
            AddSettingsRow(connection, 6, startupActions);
            AddSettingsRow(connection, 7, autoStart);
            AddSettingsRow(connection, 8, autoSwitchProfile);
            var help = UiTheme.Label("Фоновое подключение использует SSH-ключ и не запрашивает пароль. «Первый вход» открывает видимое окно SSH: сверьте отпечаток ключа сервера, войдите и завершите сеанс командой exit. Вход по паролю сам по себе не настраивает SSH-ключ для ProGo. Затем нажмите «Запустить CLI» один раз — ProGo дождётся готовности. Для iPhone и Android используйте «VPN для телефона».", UiTheme.Body, UiTheme.Muted);
            AddSettingsRow(connection, 10, help);
            currentConnection.Name = "currentConnectionSettings";
            AddSettingsRow(connection, 9, currentConnection);
            var privacy = FormTable(Page(tabs, "Хранилище"));
            clearSeconds.Minimum = 5; clearSeconds.Maximum = 3600;
            AddLabeled(privacy, 0, "Очищать буфер через, сек.", clearSeconds);
            var privacyHint = UiTheme.Label("Секреты хранилища, токены друзей и ссылки QR очищаются из текущего буфера через указанное время и при выходе из ProGo, если после них ничего не скопировано. История буфера и синхронизированные копии не очищаются. При аварийном завершении очистка не гарантируется.", UiTheme.Body, UiTheme.Muted);
            AddSettingsRow(privacy, 1, privacyHint);
            var diagnostic = FormTable(Page(tabs, "Диагностика"));
            AddLabeled(diagnostic, 0, "Сайт проверки", endpoint);
            AddLabeled(diagnostic, 1, "Журнал приложения", new TextBox { ReadOnly = true, Text = AppPaths.LogPath });
            AddLabeled(diagnostic, 2, "Файл подключений SSH", new TextBox { ReadOnly = true, Text = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ssh", "config") });
            var av = UiTheme.Label("Если обновление блокирует антивирус: откройте «Помощь» → «Антивирус». Там есть журнал и ссылка на официальный выпуск.", UiTheme.Body, UiTheme.Muted);
            AddSettingsRow(diagnostic, 4, av);
            currentDiagnostic.Name = "currentDiagnosticSettings";
            AddSettingsRow(diagnostic, 3, currentDiagnostic);
            var appPorts = FormTable(Page(tabs, "Порт приложений"));
            autoHttpPort.Text = "Выбирать свободный порт автоматически";
            autoHttpPort.AutoSize = true;
            AddSettingsRow(appPorts, 0, autoHttpPort);
            httpPort.Minimum = 1; httpPort.Maximum = 65535;
            AddLabeled(appPorts, 1, "Порт на этом компьютере", httpPort);
            autoHttpPort.CheckedChanged += delegate { httpPort.Enabled = !autoHttpPort.Checked && !pickFreePort; };
            AddLabeled(appPorts, 2, "Текущий адрес", proxyAddress);
            var portActions = SettingsActions();
            pickPortButton = UiTheme.Button("Подобрать свободный", delegate { TogglePortSelection(); }, false);
            pickPortButton.Name = "togglePortSelection";
            portActions.Controls.Add(pickPortButton);
            portActions.Controls.Add(UiTheme.Button("Скопировать адрес", delegate { Clipboard.SetText(proxyAddress.Text); }, false));
            AddSettingsRow(appPorts, 3, portActions);
            var portHelp = UiTheme.Label("Автоматически: ProGo сначала использует последний порт. Если он занят — выбирает другой. Без галочки используется только указанный порт.\n\nПри смене порта настройки Windows, терминала и ярлыка Codex, включённые через ProGo, обновятся вместе. Открытые терминалы и Codex нужно перезапустить.", UiTheme.Body, UiTheme.Muted);
            AddSettingsRow(appPorts, 4, portHelp);
            AddSettingsRow(appPorts, 5, portNotice);
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.RightToLeft, WrapContents = true, Margin = new Padding(0), Padding = new Padding(0, 12, 0, 0) };
            var save = UiTheme.Button("Сохранить", Save, true);
            var cancel = UiTheme.Button("Отменить изменения", null, false); cancel.DialogResult = DialogResult.Cancel;
            buttons.Controls.Add(save); buttons.Controls.Add(cancel); root.Controls.Add(buttons, 0, 4);
            saveError.Name = "settingsSaveError"; saveError.Visible = false;
            saveError.MaximumSize = new Size(830, 0);
            root.Layout += delegate {
                foreach (Control child in root.Controls) {
                    var label = child as Label;
                    if (label != null) label.MaximumSize = new Size(Math.Max(1, root.ClientSize.Width - root.Padding.Horizontal - label.Margin.Horizontal), 0);
                }
            };
            root.Controls.Add(saveError, 0, 3);
            UiTheme.ConfigureKeyboardOrder(root);
            Controls.Add(root); AcceptButton = save; CancelButton = cancel; LoadValues();
            settingsTabs.SelectedIndex = section == SettingsSection.Connections ? 1 : 0;
            currentValuesTimer.Tick += delegate { RefreshCurrentValues(); };
            RefreshCurrentValues();
            FormClosed += delegate { currentValuesTimer.Stop(); };
            Shown += delegate { RefreshCurrentValues(); currentValuesTimer.Start(); };
            Shown += delegate {
                if (section == SettingsSection.Windows) {
                    autoWindows.Focus();
                    BeginInvoke((Action)delegate {
                        if (IsDisposed) return;
                        var card = autoWindows.Parent.Parent;
                        autoFlow.ScrollControlIntoView(card);
                        int overflow = card.Bottom + autoFlow.Padding.Bottom - autoFlow.ClientSize.Height;
                        if (overflow > 0) autoFlow.AutoScrollPosition = new Point(0, -autoFlow.AutoScrollPosition.Y + overflow);
                    });
                }
                else if (section == SettingsSection.Connections) sshProfiles.Focus();
            };
        }
        private static TabPage Page(TabControl tabs, string title)
        {
            var page = new TabPage(title) { AccessibleName = title, BackColor = UiTheme.WindowBackground, Padding = new Padding(6) }; tabs.TabPages.Add(page); return page;
        }
        private static TableLayoutPanel FormTable(TabPage page)
        {
            var viewport = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Name = "settingsViewport" };
            var table = new TableLayoutPanel { Dock = DockStyle.None, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Padding = new Padding(18), ColumnCount = 1, RowCount = 0, AutoSize = false, Name = "settingsPageBody" };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            viewport.Controls.Add(table); page.Controls.Add(viewport);
            bool sizing = false;
            Action fit = delegate {
                if (sizing || viewport.ClientSize.Width < 1) return;
                sizing = true;
                try {
                    int width = Math.Max(1, viewport.ClientSize.Width - SystemInformation.VerticalScrollBarWidth);
                    if (table.Width != width) table.Width = width;
                    foreach (Control child in table.Controls) {
                        int available = Math.Max(1, width - table.Padding.Horizontal - child.Margin.Horizontal);
                        var label = child as Label;
                        if (label != null) label.MaximumSize = new Size(available, 0);
                        var option = child as CheckBox;
                        if (option != null) FitPreferenceCaption(option, available);
                    }
                    int height = table.GetPreferredSize(new Size(width, 0)).Height;
                    if (table.Height != height) table.Height = height;
                } finally { sizing = false; }
            };
            viewport.ClientSizeChanged += delegate { fit(); };
            table.Layout += delegate { fit(); };
            return table;
        }
        private static FlowLayoutPanel SettingsActions()
        {
            return new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = true, Margin = new Padding(0) };
        }
        private static void AddSettingsRow(TableLayoutPanel table, int row, Control control)
        {
            table.RowCount = Math.Max(table.RowCount, row + 1);
            while (table.RowStyles.Count < table.RowCount) table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            control.Dock = DockStyle.Top;
            var option = control as CheckBox;
            if (option != null) {
                option.AutoSize = false; option.TextAlign = ContentAlignment.TopLeft; option.CheckAlign = ContentAlignment.TopLeft;
                option.Margin = new Padding(0, 4, 0, 8);
                option.TextChanged += delegate { table.PerformLayout(); };
                option.FontChanged += delegate { table.PerformLayout(); };
            }
            table.Controls.Add(control, 0, row);
        }
        private static void FitPreferenceCaption(CheckBox option, int width)
        {
            // AutoSize can report a single-line height for a width-constrained caption.
            int textWidth = Math.Max(1, width - SystemInformation.MenuCheckSize.Width - 8);
            int height = TextRenderer.MeasureText(option.Text, option.Font,
                new Size(textWidth, int.MaxValue), TextFormatFlags.WordBreak).Height;
            option.Height = Math.Max(SystemInformation.MenuCheckSize.Height, height) + 4;
        }
        private void AutomationCard(FlowLayoutPanel flow, CheckBox toggle, string title, string description, string onLabel, string on, string offLabel, string off)
        {
            var card = new SurfacePanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 6, 0, 8), Padding = new Padding(16) };
            var stack = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1, RowCount = 4, Margin = new Padding(0) };
            stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int row = 0; row < 4; row++) stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            toggle.AccessibleDescription = "Автоматическое действие после сохранения настроек. " + description;
            toggle.Text = title; toggle.AutoSize = false; toggle.Font = UiTheme.Strong;
            toggle.Dock = DockStyle.Top; toggle.TextAlign = ContentAlignment.TopLeft; toggle.CheckAlign = ContentAlignment.TopLeft;
            toggle.Margin = new Padding(0, 0, 0, 8);
            var hint = UiTheme.Label(description, UiTheme.Body, UiTheme.Muted);
            var immediate = UiTheme.Label("Ручное управление · применяется сразу", UiTheme.Body, UiTheme.Accent);
            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = true, Margin = new Padding(0) };
            var onButton = UiTheme.Button(onLabel, delegate { RequestManualAction(on); }, false);
            var offButton = UiTheme.Button(offLabel, delegate { RequestManualAction(off); }, false);
            onButton.AccessibleDescription = offButton.AccessibleDescription = title + ". Ручное действие применяется сразу.";
            actions.Controls.Add(onButton); actions.Controls.Add(offButton);
            stack.Controls.Add(toggle, 0, 0); stack.Controls.Add(hint, 0, 1);
            stack.Controls.Add(immediate, 0, 2); stack.Controls.Add(actions, 0, 3);
            bool sizing = false;
            stack.Layout += delegate {
                if (sizing) return; sizing = true;
                try {
                    foreach (Control child in stack.Controls) {
                        int width = Math.Max(1, stack.ClientSize.Width - child.Margin.Horizontal);
                        if (child is Label) child.MaximumSize = new Size(width, 0);
                        var option = child as CheckBox;
                        if (option != null) {
                            FitPreferenceCaption(option, width);
                        }
                    }
                } finally { sizing = false; }
            };
            toggle.TextChanged += delegate { stack.PerformLayout(); };
            toggle.FontChanged += delegate { stack.PerformLayout(); };
            card.Controls.Add(stack); flow.Controls.Add(card);
        }
        private void RequestManualAction(string action)
        {
            // Manual commands deliberately use the application's saved settings,
            // never the uncommitted controls in this dialog.
            if (ManualActionRequested != null) ManualActionRequested(action);
            RefreshCurrentValues();
        }
        internal void RefreshCurrentValues()
        {
            var current = service.Current;
            var appEndpoint = CurrentProxyEndpoint == null ? CliProxyBridgeService.UrlFor(current.HttpProxyPort) : CurrentProxyEndpoint();
            proxyAddress.Text = appEndpoint;
            currentAutomation.Text = "Сохранено для ручных команд: SSH " + (String.IsNullOrWhiteSpace(current.SshProfile) ? "не выбран" : current.SshProfile) + "\nSOCKS " + current.SocksHost + ":" + current.SocksPort + " · приложения " + appEndpoint;
            currentConnection.Text = "Сохранено: SSH " + (String.IsNullOrWhiteSpace(current.SshProfile) ? "не выбран" : current.SshProfile) +
                "\nSOCKS " + current.SocksHost + ":" + current.SocksPort + ". Поля выше — изменения до сохранения.";
            currentDiagnostic.Text = "Сохранённый сайт проверки: " + current.TestEndpoint;
        }
        private void TogglePortSelection()
        {
            pickFreePort = !pickFreePort;
            httpPort.Enabled = !autoHttpPort.Checked && !pickFreePort;
            pickPortButton.Text = pickFreePort ? "Отменить подбор" : "Подобрать свободный";
            pickPortButton.AccessibleName = pickPortButton.Text;
            portNotice.ForeColor = UiTheme.Accent;
            portNotice.Text = pickFreePort
                ? "Свободный порт будет выбран только при сохранении. Нажмите «Отменить подбор», чтобы оставить введённый порт."
                : "Разовый подбор отменён. При сохранении используется выбранный вами режим и порт.";
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) currentValuesTimer.Dispose();
            base.Dispose(disposing);
        }
        private static void AddLabeled(TableLayoutPanel panel, int row, string label, Control control)
        {
            var field = new TableLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1, RowCount = 2, Margin = new Padding(0, 0, 0, 12) };
            field.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            field.RowStyles.Add(new RowStyle(SizeType.AutoSize)); field.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var caption = UiTheme.Label(label, UiTheme.Body, UiTheme.Text);
            caption.Dock = DockStyle.Top; caption.Margin = new Padding(0, 0, 0, 6);
            control.AccessibleName = label;
            var readOnly = control as TextBox;
            control.AccessibleDescription = readOnly != null && readOnly.ReadOnly
                ? "Только чтение. Значение можно выделить и скопировать."
                : "Изменения применяются после сохранения настроек.";
            control.Dock = DockStyle.Top; control.Margin = new Padding(0);
            field.Controls.Add(caption, 0, 0); field.Controls.Add(control, 0, 1);
            if (control is ComboBox) {
                // Owner-drawn ComboBox height follows ItemHeight; preferred size may
                // still report the smaller font-based height. Reserve the native height.
                field.RowStyles[1].SizeType = SizeType.Absolute;
                field.RowStyles[1].Height = control.Height;
                control.SizeChanged += delegate { field.RowStyles[1].Height = control.Height; };
            }
            field.Layout += delegate { caption.MaximumSize = new Size(Math.Max(1, field.ClientSize.Width), 0); };
            AddSettingsRow(panel, row, field);
        }

        private void LoadValues()
        {
            var s = service.Current;
            host.Text = s.SocksHost;
            port.Value = s.SocksPort;
            httpPort.Value = s.HttpProxyPort;
            initialHttpPort = s.HttpProxyPort;
            autoHttpPort.Checked = s.AutoHttpProxyPort;
            httpPort.Enabled = !autoHttpPort.Checked;
            proxyAddress.Text = CliProxyBridgeService.UrlFor(s.HttpProxyPort);
            autoSwitchProfile.Checked = s.AutoSwitchSshProfile;
            autoStart.Checked = s.AutoStartSocks;
            autoLaunch.Enabled = false;
            if (startupShortcuts == null) startupNotice.Text = "Эта копия запущена вне папки установки ProGo. Настройка автозапуска доступна в установленной программе.";
            else {
                try {
                    startupSnapshot = startupShortcuts.ReadStartup();
                    autoLaunch.Checked = startupSnapshot.Registered;
                    autoLaunch.Enabled = true;
                    startupNotice.Text = (startupSnapshot.Registered ? "ProGo добавлен в автозагрузку." : "ProGo не добавлен в автозагрузку.") +
                        " Галочка применяется после сохранения. Windows может отдельно запретить запуск: проверьте разрешение кнопкой ниже. Подключение к серверу задаётся следующей галочкой.";
                } catch {
                    startupNotice.Text = "Не удалось проверить автозапуск: ярлык недоступен или изменён вне ProGo. Он сохранён без изменений. Проверьте параметры Windows и снова откройте это окно.";
                }
            }
            autoCli.Checked = s.AutoCliProxy;
            autoRestart.Checked = s.AutoRestartSocks;
            autoWindows.Checked = s.AutoSystemProxy;
            clearSeconds.Value = s.ClipboardClearSeconds;
            endpoint.Text = s.TestEndpoint;

            profiles.Clear();
            if (s.SshProfiles != null)
            {
                foreach (var profile in s.SshProfiles)
                {
                    if (profile == null) continue;
                    if (String.IsNullOrWhiteSpace(profile.Target)) continue;
                    profiles.Add(profile.Clone());
                }
            }

            if (profiles.Count == 0 && !String.IsNullOrWhiteSpace(s.SshProfile))
            {
                profiles.Add(new SshProfileSetting { Name = s.SshProfile, Target = s.SshProfile });
            }

            ReloadProfiles(s.SshProfile);
        }

        private void ReloadProfiles(string selectedTarget)
        {
            sshProfiles.Items.Clear();
            foreach (var profile in profiles)
            {
                sshProfiles.Items.Add(profile);
            }

            var selectedIndex = -1;
            for (var i = 0; i < profiles.Count; i++)
            {
                if (String.Equals(profiles[i].Target, selectedTarget, StringComparison.OrdinalIgnoreCase))
                {
                    selectedIndex = i;
                    break;
                }
            }

            if (selectedIndex < 0 && profiles.Count > 0) selectedIndex = 0;
            if (selectedIndex >= 0) sshProfiles.SelectedIndex = selectedIndex;
        }

        private SshProfileSetting SelectedProfile()
        {
            if (sshProfiles.SelectedIndex < 0 || sshProfiles.SelectedIndex >= profiles.Count) return null;
            return profiles[sshProfiles.SelectedIndex];
        }

        private void AddProfile()
        {
            using (var form = new SshProfileEditorForm(null))
            {
                if (form.ShowDialog(this) != DialogResult.OK) return;
                if (ContainsTarget(form.Profile.Target))
                {
                    MessageBox.Show("Такой SSH-профиль уже есть в списке.", "SSH-профили", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                profiles.Add(form.Profile);
                ReloadProfiles(form.Profile.Target);
            }
        }

        private void RemoveProfile()
        {
            var selected = SelectedProfile();
            if (selected == null) return;
            var answer = MessageBox.Show("Удалить SSH-профиль «" + selected.Name + "» из списка ProGo?\n\nФайл ~/.ssh/config не изменяется.", "SSH-профили", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (answer != DialogResult.Yes) return;
            profiles.Remove(selected);
            ReloadProfiles(null);
        }

        private void EditProfile()
        {
            var selected = SelectedProfile();
            if (selected == null)
            {
                MessageBox.Show("Сначала добавьте сервер кнопкой «Добавить»: укажите адрес, логин SSH, порт и ключ.", "Подключение к серверу", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var form = new SshProfileEditorForm(selected.Clone()))
            {
                if (form.ShowDialog(this) != DialogResult.OK) return;
                if (!String.Equals(selected.Target, form.Profile.Target, StringComparison.OrdinalIgnoreCase) && ContainsTarget(form.Profile.Target))
                {
                    MessageBox.Show("Такой SSH-профиль уже есть в списке.", "SSH-профили", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                var index = profiles.IndexOf(selected);
                if (index >= 0) profiles[index] = form.Profile;
                ReloadProfiles(form.Profile.Target);
            }
        }

        private void CheckSelectedProfile()
        {
            var selected = SelectedProfile();
            if (selected == null)
            {
                MessageBox.Show("SSH-профиль не выбран. Добавьте профиль кнопкой «Добавить».", "Проверить SSH-профиль", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var diagnostic = new SshDiagnosticForm(selected)) diagnostic.ShowDialog(this);
        }

        private bool ContainsTarget(string target)
        {
            foreach (var profile in profiles)
            {
                if (String.Equals(profile.Target, target, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        private void Save(object sender, EventArgs e)
        {
            var selected = SelectedProfile();
            var selectedTarget = selected == null ? String.Empty : selected.Target;
            var proposed = new AppSettings
            {
                SocksHost = host.Text.Trim(),
                SocksPort = (int)port.Value,
                HttpProxyPort = (int)httpPort.Value == initialHttpPort && !pickFreePort ? service.Current.HttpProxyPort : (int)httpPort.Value,
                AutoHttpProxyPort = autoHttpPort.Checked,
                SshProfile = selectedTarget,
                SshProfiles = CloneProfiles(),
                AutoSwitchSshProfile = autoSwitchProfile.Checked,
                AutoStartSocks = autoStart.Checked,
                AutoRestartSocks = autoRestart.Checked,
                AutoSystemProxy = autoWindows.Checked,
                AutoCliProxy = autoCli.Checked,
                ClipboardClearSeconds = (int)clearSeconds.Value,
                TestEndpoint = endpoint.Text.Trim()
            };
            var error = SettingsValidation.Check(proposed);
            ApplicationShortcuts.StartupChange startupChange = null;
            if (error == null && startupSnapshot != null && autoLaunch.Checked != startupSnapshot.Registered) {
                try { startupChange = startupShortcuts.ChangeStartup(startupSnapshot, autoLaunch.Checked); }
                catch { error = new SettingsSaveError(SettingsField.Startup, "Не удалось изменить автозапуск. Ярлык недоступен или изменился вне ProGo. Проверьте его и снова откройте настройки; остальные параметры не применены."); }
            }
            if (error == null) {
                try {
                    if (SaveRequested != null) error = SaveRequested(proposed, pickFreePort);
                    else using (var bridge = new CliProxyBridgeService(service)) bridge.ReconfigureDetailed(proposed, pickFreePort, out error);
                } catch (Exception ex) {
                    SafeLog.Error("Settings application failed.", ex);
                    error = new SettingsSaveError(SettingsField.General, "Не удалось завершить применение настроек. Проверьте текущее состояние и журнал ProGo.");
                }
            }
            if (error != null) {
                if (startupChange != null && !startupChange.TryRollback())
                    error = new SettingsSaveError(SettingsField.Startup, "Настройки не удалось полностью применить, а автозапуск — вернуть к прежнему состоянию. Проверьте автозагрузку Windows и снова откройте настройки.");
                ShowSaveError(error); return;
            }
            DialogResult = DialogResult.OK; Close();
        }

        private void ShowSaveError(SettingsSaveError error)
        {
            DialogResult = DialogResult.None;
            Control field = null;
            switch (error.Field) {
                case SettingsField.Startup: settingsTabs.SelectedIndex = 1; field = autoLaunch.Enabled ? (Control)autoLaunch : startupSettingsButton; break;
                case SettingsField.SocksHost: settingsTabs.SelectedIndex = 1; field = host; break;
                case SettingsField.SocksPort: settingsTabs.SelectedIndex = 1; field = port; break;
                case SettingsField.SshProfile: settingsTabs.SelectedIndex = 1; field = sshProfiles; break;
                case SettingsField.TestEndpoint: settingsTabs.SelectedIndex = 3; field = endpoint; break;
                case SettingsField.HttpProxyPort: settingsTabs.SelectedIndex = 4; field = httpPort; break;
                case SettingsField.ClipboardClearSeconds: settingsTabs.SelectedIndex = 2; field = clearSeconds; break;
                case SettingsField.Applications: settingsTabs.SelectedIndex = 0; break;
                // A file-write or unclassified failure belongs to the whole Save,
                // so keep the user's current page and all uncommitted controls.
            }
            saveError.Text = error.Message;
            saveError.Visible = true;
            if (field != null) {
                var panel = field.Parent as ScrollableControl;
                if (panel != null) panel.ScrollControlIntoView(field);
                field.Focus();
            }
        }

        private List<SshProfileSetting> CloneProfiles()
        {
            var result = new List<SshProfileSetting>();
            foreach (var profile in profiles)
            {
                result.Add(profile.Clone());
            }
            return result;
        }
    }

    internal sealed class SshProfileEditorForm : ProGoForm
    {
        private readonly TextBox name = new TextBox { Name = "connectionName" };
        private readonly ComboBox mode = new ComboBox { Name = "connectionMode", DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly TextBox server = new TextBox { Name = "sshServer" };
        private readonly TextBox user = new TextBox { Name = "sshUser" };
        private readonly NumericUpDown port = new NumericUpDown { Name = "sshPort", Minimum = 1, Maximum = 65535, Value = 22 };
        private readonly TextBox key = new TextBox { Name = "sshKey" };
        private readonly TextBox target = new TextBox { Name = "sshAlias" };
        private readonly Button browse = new Button { Text = "Выбрать…", Width = 105, Dock = DockStyle.Right };
        private readonly Label guidance = UiTheme.Label("", UiTheme.Body, UiTheme.Muted);
        public SshProfileSetting Profile { get; private set; }

        public SshProfileEditorForm(SshProfileSetting profile)
        {
            guidance.Dock = DockStyle.Fill; guidance.Margin = new Padding(3);
            Profile = profile == null ? new SshProfileSetting() : profile.Clone();
            Text = profile == null ? "Добавить подключение" : "Изменить подключение";
            AutoScaleMode = AutoScaleMode.Dpi;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(760, 610);
            MinimumSize = new Size(760, 610);
            var table = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), ColumnCount = 2, RowCount = 11 };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
            for (int i = 0; i < 7; i++) table.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            Controls.Add(table);
            var hint = UiTheme.Label("Введите данные SSH из панели вашего VPS. Пароль здесь не сохраняется: фоновое подключение использует SSH-ключ.", UiTheme.Body, UiTheme.Muted);
            hint.Dock = DockStyle.Fill; hint.Margin = new Padding(3);
            table.Controls.Add(hint, 0, 0); table.SetColumnSpan(hint, 2);
            mode.Items.AddRange(new object[] { "По адресу сервера", "Из SSH config (для опытных)" });
            Add(table, 1, "Название", name);
            Add(table, 2, "Способ подключения", mode);
            Add(table, 3, "Сервер (IP или домен)", server);
            Add(table, 4, "Логин SSH", user);
            Add(table, 5, "Порт SSH", port);
            var keyPanel = new Panel { Dock = DockStyle.Fill };
            key.AccessibleName = "Закрытый SSH-ключ";
            key.AccessibleDescription = "Путь к закрытому файлу ключа. Пустое поле использует стандартные ключи и SSH-агент Windows.";
            browse.AccessibleDescription = "Выбрать закрытый SSH-ключ на этом компьютере.";
            key.Dock = DockStyle.Fill; keyPanel.Controls.Add(key); keyPanel.Controls.Add(browse);
            Add(table, 6, "Закрытый SSH-ключ", keyPanel);
            Add(table, 7, "Имя из SSH config", target);
            table.Controls.Add(guidance, 0, 8); table.SetColumnSpan(guidance, 2);
            browse.Click += delegate {
                using (var picker = new OpenFileDialog { Title = "Выберите закрытый SSH-ключ (не .pub)", Filter = "Все файлы (*.*)|*.*", CheckFileExists = true }) {
                    if (picker.ShowDialog(this) == DialogResult.OK) key.Text = picker.FileName;
                }
            };
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            var save = new Button { Name = "saveConnection", Text = "Сохранить", Width = 110, DialogResult = DialogResult.OK, Tag = "primary" };
            var cancel = new Button { Text = "Отмена", Width = 110, DialogResult = DialogResult.Cancel };
            save.Click += Save; buttons.Controls.Add(cancel); buttons.Controls.Add(save);
            table.Controls.Add(buttons, 0, 10); table.SetColumnSpan(buttons, 2);
            AcceptButton = save; CancelButton = cancel;
            name.Text = Profile.Name ?? ""; server.Text = Profile.Server ?? ""; user.Text = Profile.User ?? "";
            port.Value = Math.Max(1, Math.Min(65535, Profile.Port)); key.Text = Profile.IdentityFile ?? "";
            target.Text = Profile.IsDirect ? "" : Profile.Target ?? "";
            mode.SelectedIndexChanged += delegate { UpdateMode(); };
            // Existing aliases remain aliases until the user explicitly switches modes.
            mode.SelectedIndex = profile == null || Profile.IsDirect ? 0 : 1;
            UpdateMode();
            UiTheme.ConfigureKeyboardOrder(table);
        }
        private void UpdateMode()
        {
            bool direct = mode.SelectedIndex == 0;
            server.Enabled = user.Enabled = port.Enabled = key.Enabled = browse.Enabled = direct;
            target.Enabled = !direct;
            guidance.Text = direct ? "Сервер: например vpn.example.org. Порт SSH обычно 22 — это не порт SOCKS.\r\nКлюч: выберите закрытый файл. Пустое поле использует стандартные ключи и SSH-агент Windows. Ключ должен быть разрешён на VPS; для проверки используйте «Первый вход»." :
                "Введите имя из %USERPROFILE%\\.ssh\\config, например my-vps, или прежний адрес user@host. Сервер, порт и ключ берутся из настроек OpenSSH. Файл config не изменяется.";
        }
        private static void Add(TableLayoutPanel table, int row, string label, Control control)
        {
            table.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
            control.AccessibleName = label;
            control.Dock = DockStyle.Fill; table.Controls.Add(control, 1, row);
        }
        private void Save(object sender, EventArgs e)
        {
            var candidate = Profile.Clone();
            if (mode.SelectedIndex == 0) {
                candidate.Server = server.Text.Trim(); candidate.User = user.Text.Trim(); candidate.Port = (int)port.Value;
                candidate.IdentityFile = key.Text.Trim();
                if (String.IsNullOrWhiteSpace(candidate.Server)) { Refuse("Укажите IP-адрес или домен в поле «Сервер»."); return; }
                if (String.IsNullOrWhiteSpace(candidate.Target)) candidate.Target = "progo-" + Guid.NewGuid().ToString("N");
            } else {
                candidate.Target = target.Text.Trim(); candidate.Server = ""; candidate.User = ""; candidate.Port = 22; candidate.IdentityFile = "";
            }
            try {
                SshConnection.Validate(candidate);
                if (candidate.IsDirect && !String.IsNullOrWhiteSpace(candidate.IdentityFile)) {
                    if (!System.IO.File.Exists(candidate.IdentityFile)) throw new ArgumentException("Файл ключа не найден. Выберите существующий файл на этом компьютере.");
                    if (candidate.IdentityFile.EndsWith(".pub", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Это открытый ключ (.pub). Выберите закрытый ключ — обычно файл без расширения .pub.");
                }
            } catch (ArgumentException ex) { Refuse(ex.Message); return; }
            candidate.Name = String.IsNullOrWhiteSpace(name.Text) ? candidate.Address : name.Text.Trim();
            Profile = candidate;
        }
        private void Refuse(string message)
        {
            DialogResult = DialogResult.None;
            MessageBox.Show(this, message, Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}

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
        private readonly CheckBox autoCli = new CheckBox();
        private readonly NumericUpDown clearSeconds = new NumericUpDown();
        private readonly TextBox endpoint = new TextBox();
        private readonly List<SshProfileSetting> profiles = new List<SshProfileSetting>();

        private readonly CheckBox autoRestart = new CheckBox();
        private readonly CheckBox autoWindows = new CheckBox();
        public event Action<string> ManualActionRequested;
        public Func<AppSettings, bool, string> SaveRequested;
        private readonly CheckBox autoHttpPort = new CheckBox();
        private readonly NumericUpDown httpPort = new NumericUpDown();
        private readonly TextBox proxyAddress = new TextBox { ReadOnly = true };
        private readonly Label portNotice = UiTheme.Label("", UiTheme.Body, UiTheme.Muted);
        private TabControl settingsTabs;
        private bool pickFreePort;
        private int initialHttpPort;
        public string ProxyEndpointText { set { proxyAddress.Text = value; } }


        public SshProfilesSettingsForm(SettingsService settingsService, SettingsSection section = SettingsSection.Automation)
        {
            service = settingsService;
            Text = "Настройки · ProGo";
            ClientSize = new Size(900, 700); MinimumSize = new Size(850, 650);
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), ColumnCount = 1, RowCount = 4 };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            root.Controls.Add(UiTheme.Label("Под ваш ритм", UiTheme.Title, UiTheme.Text), 0, 0);
            root.Controls.Add(UiTheme.Label("Автоматика, подключения и личные настройки — в одном месте.", UiTheme.Body, UiTheme.Muted), 0, 1);
            var tabs = new ProGoTabs { Dock = DockStyle.Fill, ItemSize = new Size(153, 38), SizeMode = TabSizeMode.Fixed };
            root.Controls.Add(tabs, 0, 2); settingsTabs = tabs;
            var automation = Page(tabs, "Автоматика");
            var autoFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, AutoScrollMargin = new Size(0, 18), FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(14) };
            automation.Controls.Add(autoFlow);
            autoFlow.SizeChanged += delegate {
                foreach (Control child in autoFlow.Controls) {
                    var card = child as SurfacePanel; if (card == null) continue;
                    card.Width = Math.Max(600, autoFlow.ClientSize.Width - autoFlow.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth - 4);
                    foreach (Control item in card.Controls) {
                        var hint = item as Label; if (hint != null) hint.MaximumSize = new Size(card.Width - 38, 0);
                    }
                }
            };
            autoFlow.Controls.Add(UiTheme.Label("Галочка — автоматически. Кнопки — вручную в любой момент.", UiTheme.Body, UiTheme.Muted));
            AutomationCard(autoFlow, autoRestart, "Восстанавливать подключение при обрыве", "Повторять соединение, если туннель перестал работать. После вашей команды отключения он сам не включится.", "Перезапустить", "restart", "Отключить прокси на ПК", "stop");
            AutomationCard(autoFlow, autoCli, "Включать прокси для терминалов и Codex", "Включать общий прокси после подключения ProGo. Затем откройте новый терминал или перезапустите уже открытый Codex. Отдельный ярлык не нужен.", "Включить", "cli-start", "Выключить", "cli-off");
            AutomationCard(autoFlow, autoWindows, "Включать прокси для приложений Windows", "Применять системный прокси при запуске ProGo. Работает для приложений, которые используют настройки прокси Windows.", "Включить", "windows-on", "Выключить", "windows-off");
            var connection = FormTable(Page(tabs, "Подключение"));
            sshProfiles.DropDownStyle = ComboBoxStyle.DropDownList;
            AddLabeled(connection, 0, "Сервер", sshProfiles);
            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
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
            connection.Controls.Add(actions, 0, 1); connection.SetColumnSpan(actions, 2); connection.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            AddLabeled(connection, 2, "Адрес SOCKS-туннеля", host);
            port.Minimum = 1; port.Maximum = 65535; AddLabeled(connection, 3, "Порт SOCKS-туннеля", port);
            autoStart.Text = "Подключаться к серверу при запуске ProGo"; autoStart.AutoSize = true;
            autoSwitchProfile.Text = "Пробовать другой сервер при недоступности"; autoSwitchProfile.AutoSize = true;
            connection.Controls.Add(autoStart, 0, 4); connection.SetColumnSpan(autoStart, 2); connection.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            connection.Controls.Add(autoSwitchProfile, 0, 5); connection.SetColumnSpan(autoSwitchProfile, 2); connection.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            var help = UiTheme.Label("Фоновое подключение использует SSH-ключ и не запрашивает пароль. «Первый вход» открывает видимое окно SSH: сверьте отпечаток ключа сервера, войдите и завершите сеанс командой exit. Вход по паролю сам по себе не настраивает SSH-ключ для ProGo. Затем нажмите «Запустить CLI» один раз — ProGo дождётся готовности. Для iPhone используйте отдельный мастер.", UiTheme.Body, UiTheme.Muted);
            help.MaximumSize = new Size(740, 0); connection.Controls.Add(help, 0, 6); connection.SetColumnSpan(help, 2);
            var privacy = FormTable(Page(tabs, "Хранилище"));
            clearSeconds.Minimum = 5; clearSeconds.Maximum = 3600;
            AddLabeled(privacy, 0, "Очищать буфер через, сек.", clearSeconds);
            var privacyHint = UiTheme.Label("Скопированный секрет исчезнет из буфера через указанное время. Хранилище открывается вашим PIN-кодом.", UiTheme.Body, UiTheme.Muted);
            privacyHint.MaximumSize = new Size(710, 0); privacy.Controls.Add(privacyHint, 0, 1); privacy.SetColumnSpan(privacyHint, 2);
            var diagnostic = FormTable(Page(tabs, "Диагностика"));
            AddLabeled(diagnostic, 0, "Сайт проверки", endpoint);
            AddLabeled(diagnostic, 1, "Журнал приложения", new TextBox { ReadOnly = true, Text = AppPaths.LogPath });
            AddLabeled(diagnostic, 2, "Файл подключений SSH", new TextBox { ReadOnly = true, Text = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ssh", "config") });
            var av = UiTheme.Label("Если обновление блокирует антивирус: откройте «Помощь» → «Антивирус и обновления». Там есть журнал и ссылка на официальный выпуск.", UiTheme.Body, UiTheme.Muted);
            av.MaximumSize = new Size(710, 0); diagnostic.Controls.Add(av, 0, 3); diagnostic.SetColumnSpan(av, 2);
            var appPorts = FormTable(Page(tabs, "Порт приложений"));
            autoHttpPort.Text = "Выбирать свободный порт автоматически";
            autoHttpPort.AutoSize = true;
            appPorts.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            appPorts.Controls.Add(autoHttpPort, 0, 0); appPorts.SetColumnSpan(autoHttpPort, 2);
            httpPort.Minimum = 1; httpPort.Maximum = 65535;
            AddLabeled(appPorts, 1, "Порт на этом компьютере", httpPort);
            autoHttpPort.CheckedChanged += delegate { httpPort.Enabled = !autoHttpPort.Checked && !pickFreePort; };
            AddLabeled(appPorts, 2, "Текущий адрес", proxyAddress);
            var portActions = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
            portActions.Controls.Add(UiTheme.Button("Подобрать свободный", delegate {
                pickFreePort = true; httpPort.Enabled = false;
                portNotice.ForeColor = UiTheme.Accent;
                portNotice.Text = "Новый свободный порт будет выбран при сохранении. Режим выбора порта останется прежним.";
            }, false));
            portActions.Controls.Add(UiTheme.Button("Скопировать адрес", delegate { Clipboard.SetText(proxyAddress.Text); }, false));
            appPorts.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
            appPorts.Controls.Add(portActions, 0, 3); appPorts.SetColumnSpan(portActions, 2);
            var portHelp = UiTheme.Label("Автоматически: ProGo сначала использует последний порт. Если он занят — выбирает другой. Без галочки используется только указанный порт.\n\nПри смене порта настройки Windows, терминала и ярлыка Codex, включённые через ProGo, обновятся вместе. Открытые терминалы и Codex нужно перезапустить.", UiTheme.Body, UiTheme.Muted);
            portHelp.MaximumSize = new Size(740, 0);
            appPorts.RowStyles.Add(new RowStyle(SizeType.Absolute, 155));
            appPorts.Controls.Add(portHelp, 0, 4); appPorts.SetColumnSpan(portHelp, 2);
            portNotice.MaximumSize = new Size(740, 0);
            appPorts.Controls.Add(portNotice, 0, 5); appPorts.SetColumnSpan(portNotice, 2);
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 12, 0, 0) };
            var save = UiTheme.Button("Сохранить", Save, true);
            var cancel = UiTheme.Button("Отмена", null, false); cancel.DialogResult = DialogResult.Cancel;
            buttons.Controls.Add(save); buttons.Controls.Add(cancel); root.Controls.Add(buttons, 0, 3);
            Controls.Add(root); AcceptButton = save; CancelButton = cancel; LoadValues();
            settingsTabs.SelectedIndex = section == SettingsSection.Connections ? 1 : 0;
            Shown += delegate {
                if (section == SettingsSection.Windows) {
                    autoWindows.Focus();
                    BeginInvoke((Action)delegate {
                        if (IsDisposed) return;
                        var card = autoWindows.Parent;
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
            var page = new TabPage(title) { BackColor = UiTheme.Background, Padding = new Padding(6) }; tabs.TabPages.Add(page); return page;
        }
        private static TableLayoutPanel FormTable(TabPage page)
        {
            var table = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 2, AutoScroll = true };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230)); table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            page.Controls.Add(table); return table;
        }
        private void AutomationCard(FlowLayoutPanel flow, CheckBox toggle, string title, string description, string onLabel, string on, string offLabel, string off)
        {
            var card = new SurfacePanel { Width = 740, Height = 148, Margin = new Padding(0, 6, 0, 8), Padding = new Padding(16) };
            toggle.Text = title; toggle.AutoSize = true; toggle.Font = UiTheme.Strong; toggle.Location = new Point(16, 12);
            var hint = UiTheme.Label(description, UiTheme.Body, UiTheme.Muted); hint.MaximumSize = new Size(694, 0); hint.Location = new Point(16, 42);
            var actions = new FlowLayoutPanel { Location = new Point(16, 100), Width = 698, Height = 42 };
            actions.Controls.Add(UiTheme.Button(onLabel, delegate { if (ManualActionRequested != null) ManualActionRequested(on); }, false));
            actions.Controls.Add(UiTheme.Button(offLabel, delegate { if (ManualActionRequested != null) ManualActionRequested(off); }, false));
            card.Controls.Add(toggle); card.Controls.Add(hint); card.Controls.Add(actions); flow.Controls.Add(card);
        }
        private static void AddLabeled(TableLayoutPanel panel, int row, string label, Control control)
        {
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            panel.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
            control.Dock = DockStyle.Fill; control.Margin = new Padding(0, 8, 0, 8); panel.Controls.Add(control, 1, row);
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
            string error = null;
            if (SaveRequested != null) error = SaveRequested(proposed, pickFreePort);
            else using (var bridge = new CliProxyBridgeService(service)) bridge.Reconfigure(proposed, pickFreePort, out error);
            if (error != null)
            {
                DialogResult = DialogResult.None; settingsTabs.SelectedIndex = 4;
                portNotice.ForeColor = Color.FromArgb(255, 152, 128); portNotice.Text = error; return;
            }
            DialogResult = DialogResult.OK; Close();
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
        private readonly Label guidance = new Label { AutoSize = true, Dock = DockStyle.Fill, ForeColor = UiTheme.Muted, Tag = "styled" };
        public SshProfileSetting Profile { get; private set; }

        public SshProfileEditorForm(SshProfileSetting profile)
        {
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
            var hint = new Label { Text = "Введите данные SSH из панели вашего VPS. Пароль здесь не сохраняется: фоновое подключение использует SSH-ключ.",
                AutoSize = true, Dock = DockStyle.Fill, ForeColor = UiTheme.Muted, Tag = "styled" };
            table.Controls.Add(hint, 0, 0); table.SetColumnSpan(hint, 2);
            mode.Items.AddRange(new object[] { "По адресу сервера", "Из SSH config (для опытных)" });
            Add(table, 1, "Название", name);
            Add(table, 2, "Способ подключения", mode);
            Add(table, 3, "Сервер (IP или домен)", server);
            Add(table, 4, "Логин SSH", user);
            Add(table, 5, "Порт SSH", port);
            var keyPanel = new Panel { Dock = DockStyle.Fill };
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

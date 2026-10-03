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
            var autoFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(14) };
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
            connection.Controls.Add(actions, 0, 1); connection.SetColumnSpan(actions, 2); connection.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            AddLabeled(connection, 2, "Адрес SOCKS-туннеля", host);
            port.Minimum = 1; port.Maximum = 65535; AddLabeled(connection, 3, "Порт SOCKS-туннеля", port);
            autoStart.Text = "Подключаться к серверу при запуске ProGo"; autoStart.AutoSize = true;
            autoSwitchProfile.Text = "Пробовать другой сервер при недоступности"; autoSwitchProfile.AutoSize = true;
            connection.Controls.Add(autoStart, 0, 4); connection.SetColumnSpan(autoStart, 2); connection.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            connection.Controls.Add(autoSwitchProfile, 0, 5); connection.SetColumnSpan(autoSwitchProfile, 2); connection.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            var help = UiTheme.Label("Сервер — это имя подключения SSH или адрес вида user@vpn.example.org. Автоматические прокси включатся, когда соединение будет готово. Для подключения iPhone используйте отдельный мастер.", UiTheme.Body, UiTheme.Muted);
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
                if (section == SettingsSection.Windows) { autoFlow.ScrollControlIntoView(autoWindows.Parent); autoWindows.Focus(); }
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
                MessageBox.Show("SSH-профиль — это имя подключения для ssh.exe. Пример: my-vps.\n\nСначала добавьте профиль кнопкой «Добавить».", "Что такое SSH-профиль", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

            var result = SshProfileDiagnostics.Check(selected.Target);
            MessageBox.Show(result.ToReport(), "Проверить SSH-профиль", MessageBoxButtons.OK, result.SshResolved ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
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
        private readonly TextBox name = new TextBox();
        private readonly TextBox target = new TextBox();
        public SshProfileSetting Profile { get; private set; }

        public SshProfileEditorForm(SshProfileSetting profile)
        {
            Profile = profile == null ? new SshProfileSetting() : profile.Clone();
            Text = String.IsNullOrWhiteSpace(Profile.Target) ? "Добавить подключение" : "Изменить подключение";
            AutoScaleMode = AutoScaleMode.Dpi;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(690, 340);
            MinimumSize = new Size(690, 340);

            var table = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), ColumnCount = 2, RowCount = 6 };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            Controls.Add(table);

            var hint = new Label
            {
                Text = "Укажите адрес сервера с именем пользователя или готовое имя подключения из файла SSH. ProGo подключается встроенным клиентом Windows.",
                AutoSize = true,
                MaximumSize = new Size(620, 0), ForeColor = UiTheme.Muted, Tag = "styled"
            };
            table.Controls.Add(hint, 0, 0);
            table.SetColumnSpan(hint, 2);

            Add(table, 1, "Название", name);
            Add(table, 2, "Адрес подключения", target);

            var examples = new Label
            {
                Text = "Примеры: my-vps или user@vpn.example.org",
                AutoSize = true
            };
            table.Controls.Add(examples, 1, 3);

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            var save = new Button { Text = "Сохранить", Width = 110, DialogResult = DialogResult.OK, Tag = "primary" };
            var cancel = new Button { Text = "Отмена", Width = 110, DialogResult = DialogResult.Cancel };
            save.Click += Save;
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(save);
            table.Controls.Add(buttons, 0, 5);
            table.SetColumnSpan(buttons, 2);
            AcceptButton = save;
            CancelButton = cancel;

            name.Text = Profile.Name ?? String.Empty;
            target.Text = Profile.Target ?? String.Empty;
        }

        private static void Add(TableLayoutPanel table, int row, string label, Control control)
        {
            table.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
            control.Dock = DockStyle.Fill;
            table.Controls.Add(control, 1, row);
        }

        private void Save(object sender, EventArgs e)
        {
            var targetText = target.Text.Trim();
            if (String.IsNullOrWhiteSpace(targetText))
            {
                MessageBox.Show("Укажите Адрес подключения: например my-vps или user@vpn.example.org.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.None;
                return;
            }

            Profile.Target = targetText;
            Profile.Name = String.IsNullOrWhiteSpace(name.Text) ? targetText : name.Text.Trim();
        }
    }
}

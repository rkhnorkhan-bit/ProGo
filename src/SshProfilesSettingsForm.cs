using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ProGo
{
    internal sealed class SshProfilesSettingsForm : Form
    {
        private readonly SettingsService service;
        private readonly TextBox host = new TextBox();
        private readonly NumericUpDown port = new NumericUpDown();
        private readonly ComboBox sshProfiles = new ComboBox();
        private readonly CheckBox autoSwitchProfile = new CheckBox();
        private readonly CheckBox autoStart = new CheckBox();
        private readonly CheckBox autoProxy = new CheckBox();
        private readonly NumericUpDown clearSeconds = new NumericUpDown();
        private readonly TextBox endpoint = new TextBox();
        private readonly List<SshProfileSetting> profiles = new List<SshProfileSetting>();

        public SshProfilesSettingsForm(SettingsService settingsService)
        {
            service = settingsService;
            Text = "Настройки";
            AutoScaleMode = AutoScaleMode.Font;
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(820, 560);

            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 2, RowCount = 16 };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            Controls.Add(panel);

            AddHeader(panel, 0, "Соединение");
            AddLabeled(panel, 1, "Адрес SOCKS", host);
            port.Minimum = 1; port.Maximum = 65535;
            AddLabeled(panel, 2, "Порт SOCKS", port);

            var hint = new Label
            {
                Text = "SSH-профиль — это сохранённое имя подключения из ~/.ssh/config, например progo-kz, или прямой target вида root@109.235.116.85. ProGo использует выбранный профиль, чтобы поднять локальный SOCKS-туннель.",
                AutoSize = true,
                MaximumSize = new Size(560, 0)
            };
            panel.Controls.Add(hint, 1, 3);

            var profilePanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, RowCount = 1 };
            profilePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            profilePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 36));
            profilePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 36));
            profilePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 36));
            profilePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            sshProfiles.DropDownStyle = ComboBoxStyle.DropDownList;
            sshProfiles.Dock = DockStyle.Fill;
            profilePanel.Controls.Add(sshProfiles, 0, 0);

            var add = new Button { Text = "+", Dock = DockStyle.Fill };
            var remove = new Button { Text = "-", Dock = DockStyle.Fill };
            var edit = new Button { Text = "?", Dock = DockStyle.Fill };
            var check = new Button { Text = "Проверить", Dock = DockStyle.Fill };
            add.Click += delegate { AddProfile(); };
            remove.Click += delegate { RemoveProfile(); };
            edit.Click += delegate { EditProfile(); };
            check.Click += delegate { CheckSelectedProfile(); };
            profilePanel.Controls.Add(add, 1, 0);
            profilePanel.Controls.Add(remove, 2, 0);
            profilePanel.Controls.Add(edit, 3, 0);
            profilePanel.Controls.Add(check, 4, 0);
            AddLabeled(panel, 4, "SSH-профиль", profilePanel);

            autoSwitchProfile.Text = "Автоматически менять профиль при недоступности";
            panel.Controls.Add(autoSwitchProfile, 1, 5);
            autoStart.Text = "Запускать SOCKS вместе с ProGo";
            panel.Controls.Add(autoStart, 1, 6);
            autoProxy.Text = "Автоматически применять настройки прокси";
            panel.Controls.Add(autoProxy, 1, 7);

            var help = new Label
            {
                Text = "+ добавить профиль, - удалить выбранный, ? изменить выбранный, Проверить — выполнить ssh.exe -G без подключения к серверу.",
                AutoSize = true,
                MaximumSize = new Size(560, 0)
            };
            panel.Controls.Add(help, 1, 8);

            AddHeader(panel, 9, "Безопасность");
            clearSeconds.Minimum = 5; clearSeconds.Maximum = 3600;
            AddLabeled(panel, 10, "Очищать буфер через, сек.", clearSeconds);
            AddHeader(panel, 11, "Диагностика");
            AddLabeled(panel, 12, "Адрес проверки", endpoint);
            var logPath = new TextBox { ReadOnly = true, Text = AppPaths.LogPath };
            AddLabeled(panel, 13, "Журнал", logPath);

            var sshConfigHint = new TextBox
            {
                ReadOnly = true,
                Text = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ssh", "config")
            };
            AddLabeled(panel, 14, "SSH config", sshConfigHint);

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            var save = new Button { Text = "Сохранить", Width = 110, DialogResult = DialogResult.OK };
            var cancel = new Button { Text = "Отмена", Width = 110, DialogResult = DialogResult.Cancel };
            save.Click += Save;
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(save);
            panel.Controls.Add(buttons, 0, 15);
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
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, row == 3 || row == 8 ? 56 : 32));
            panel.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
            control.Dock = DockStyle.Fill;
            panel.Controls.Add(control, 1, row);
        }

        private void LoadValues()
        {
            var s = service.Current;
            host.Text = s.SocksHost;
            port.Value = s.SocksPort;
            autoSwitchProfile.Checked = s.AutoSwitchSshProfile;
            autoStart.Checked = s.AutoStartSocks;
            autoProxy.Checked = s.AutoApplyProxy;
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
                MessageBox.Show("SSH-профиль — это имя подключения для ssh.exe. Пример: progo-kz.\n\nСначала добавьте профиль кнопкой +.", "Что такое SSH-профиль", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                MessageBox.Show("SSH-профиль не выбран. Добавьте профиль кнопкой +.", "Проверить SSH-профиль", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            service.Save(new AppSettings
            {
                SocksHost = host.Text.Trim(),
                SocksPort = (int)port.Value,
                SshProfile = selectedTarget,
                SshProfiles = CloneProfiles(),
                AutoSwitchSshProfile = autoSwitchProfile.Checked,
                AutoStartSocks = autoStart.Checked,
                AutoApplyProxy = autoProxy.Checked,
                ClipboardClearSeconds = (int)clearSeconds.Value,
                TestEndpoint = endpoint.Text.Trim()
            });
            Close();
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

    internal sealed class SshProfileEditorForm : Form
    {
        private readonly TextBox name = new TextBox();
        private readonly TextBox target = new TextBox();
        public SshProfileSetting Profile { get; private set; }

        public SshProfileEditorForm(SshProfileSetting profile)
        {
            Profile = profile == null ? new SshProfileSetting() : profile.Clone();
            Text = String.IsNullOrWhiteSpace(Profile.Target) ? "Новый SSH-профиль" : "SSH-профиль";
            AutoScaleMode = AutoScaleMode.Font;
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(560, 300);

            var table = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 2, RowCount = 6 };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            Controls.Add(table);

            var hint = new Label
            {
                Text = "Профиль не хранит пароль. Это только имя/target для ssh.exe. Лучше использовать алиас из ~/.ssh/config, например progo-kz.",
                AutoSize = true,
                MaximumSize = new Size(360, 0)
            };
            table.Controls.Add(hint, 0, 0);
            table.SetColumnSpan(hint, 2);

            Add(table, 1, "Название", name);
            Add(table, 2, "SSH target", target);

            var examples = new Label
            {
                Text = "Примеры: progo-kz или root@109.235.116.85",
                AutoSize = true
            };
            table.Controls.Add(examples, 1, 3);

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            var save = new Button { Text = "Сохранить", Width = 110, DialogResult = DialogResult.OK };
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
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            table.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
            control.Dock = DockStyle.Fill;
            table.Controls.Add(control, 1, row);
        }

        private void Save(object sender, EventArgs e)
        {
            var targetText = target.Text.Trim();
            if (String.IsNullOrWhiteSpace(targetText))
            {
                MessageBox.Show("Укажите SSH target: например progo-kz или root@109.235.116.85.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.None;
                return;
            }

            Profile.Target = targetText;
            Profile.Name = String.IsNullOrWhiteSpace(name.Text) ? targetText : name.Text.Trim();
        }
    }
}

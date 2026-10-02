using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace ProGo
{
    internal sealed class Ikev2RelayForm : Form
    {
        private readonly Ikev2RelayService relay;
        private readonly SettingsService settings;
        private readonly Label state = new Label();
        private readonly Label counters = new Label();
        private readonly Label error = new Label();
        private readonly Button start = new Button();
        private readonly Button stop = new Button();
        private readonly System.Windows.Forms.Timer refresh = new System.Windows.Forms.Timer();
        private bool busy;

        public Ikev2RelayForm(SettingsService settings, Ikev2RelayService relay)
        {
            this.settings = settings; this.relay = relay;
            Text = "VPN для iPhone — экспериментальный режим";
            AutoScaleMode = AutoScaleMode.Font;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(660, 470);
            MinimumSize = new Size(600, 490);
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 1, RowCount = 7 };
            Controls.Add(panel);
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (var i = 0; i < 7; i++) panel.RowStyles.Add(new RowStyle(i == 2 ? SizeType.Percent : SizeType.AutoSize, i == 2 ? 100 : 0));
            var heading = new Label { Text = "iPhone через домашний ПК", AutoSize = true, Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold), Margin = new Padding(0, 0, 0, 12) };
            panel.Controls.Add(heading, 0, 0);
            panel.Controls.Add(new Label
            {
                Text = "Телефон подключается к домашнему адресу по IKEv2. ProGo пересылает зашифрованные пакеты через SSH/SOCKS. Логин и пароль проверяет VPN-сервер на VPS.",
                AutoSize = true, MaximumSize = new Size(610, 0), Margin = new Padding(0, 0, 0, 12)
            }, 0, 1);
            panel.Controls.Add(new Label
            {
                Text = "Перед первым запуском:\n\n1. Установите приёмник на VPS по инструкции.\n2. Разрешите входящие UDP-порты ProGo в брандмауэре.\n3. На роутере направьте UDP 500 → ПК:15000, UDP 4500 → ПК:14500.\n4. Укажите домашний адрес в профиле iPhone.\n\nПК должен оставаться включённым. SOCKS-туннель должен вести на VPS с VPN-сервером.",
                AutoSize = true, MaximumSize = new Size(610, 0), Margin = new Padding(0, 0, 0, 12)
            }, 0, 2);
            state.AutoSize = true; state.Margin = new Padding(0, 6, 0, 6);
            counters.AutoSize = true; counters.Margin = new Padding(0, 6, 0, 6);
            error.AutoSize = true; error.MaximumSize = new Size(610, 0); error.ForeColor = Color.DarkRed;
            panel.Controls.Add(state, 0, 3); panel.Controls.Add(counters, 0, 4); panel.Controls.Add(error, 0, 5);
            var buttons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 12, 0, 0) };
            start.Text = "Запустить"; start.AutoSize = true; start.Click += Start;
            stop.Text = "Остановить"; stop.AutoSize = true;
            stop.Click += delegate { relay.Stop(); RefreshStatus(); };
            var instructions = new Button { Text = "Инструкция", AutoSize = true };
            instructions.Click += delegate
            {
                try { Process.Start("https://github.com/rkhnorkhan-bit/ProGo/blob/main/docs/HOME_IKEV2.md"); }
                catch { MessageBox.Show("Инструкция находится в репозитории ProGo: docs/HOME_IKEV2.md.", Text); }
            };
            buttons.Controls.Add(start); buttons.Controls.Add(stop); buttons.Controls.Add(instructions);
            panel.Controls.Add(buttons, 0, 6);
            refresh.Interval = 1000; refresh.Tick += delegate { RefreshStatus(); }; refresh.Start();
            RefreshStatus();
        }

        private async void Start(object sender, EventArgs args)
        {
            busy = true; RefreshStatus();
            try { await relay.StartAsync(settings.Current.SocksHost, settings.Current.SocksPort); }
            catch (Exception)
            {
                if (!IsDisposed) MessageBox.Show(relay.LastError ?? "Не удалось запустить пересылку.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally { busy = false; if (!IsDisposed) RefreshStatus(); }
        }

        private void RefreshStatus()
        {
            state.Text = busy ? "Проверяем связь с приёмником на VPS…" : relay.IsRunning
                ? "Пересылка включена. Статус самого VPN смотрите на iPhone." : "Пересылка выключена.";
            counters.Text = "Пакеты: с телефона " + relay.Received + "; на VPS " + relay.Sent + "; обратно " + relay.Returned + "; отброшено " + relay.Dropped;
            error.Text = relay.LastError == null ? "" : "Последняя ошибка: " + relay.LastError;
            start.Enabled = !busy && !relay.IsRunning; stop.Enabled = busy || relay.IsRunning;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { refresh.Stop(); refresh.Dispose(); }
            base.Dispose(disposing);
        }
    }
}

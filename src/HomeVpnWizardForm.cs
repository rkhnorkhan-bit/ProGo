using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace ProGo
{
    internal sealed class HomeVpnWizardForm : ProGoForm
    {
        private readonly HomeVpnService service;
        private readonly FlowLayoutPanel body = new FlowLayoutPanel();
        private readonly WizardProgress progress = new WizardProgress();
        private readonly Label heading = new Label();
        private readonly Label status = new Label();
        private readonly Button back = new Button();
        private readonly Button next = new Button();
        private readonly System.Windows.Forms.Timer refresh = new System.Windows.Forms.Timer();
        private int step;
        private bool own, busy, routerDone, profileSaved;
        private TextBox host, login, key, token, home;
        private NumericUpDown port;
        private CheckBox routerCheck;
        private HomeVpnOwner draftOwner;
        private string draftToken = "", draftHome;
        private string preparedToken, preparedOwner;
        private Label counters;

        internal HomeVpnWizardForm(HomeVpnService service)
        {
            this.service = service;
            Text = "iPhone через домашний ПК"; AutoScaleMode = AutoScaleMode.Dpi;
            Font = new Font("Segoe UI", 10); StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(750, 650); MinimumSize = new Size(700, 620);
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), RowCount = 5, ColumnCount = 1 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < 5; i++) layout.RowStyles.Add(new RowStyle(i == 2 ? SizeType.Percent : SizeType.AutoSize, i == 2 ? 100 : 0));
            heading.AutoSize = true; heading.Font = UiTheme.Heading; heading.Margin = new Padding(0, 0, 0, 8);
            progress.Dock = DockStyle.Top; progress.Height = 64; progress.Margin = new Padding(0, 0, 0, 16);
            body.Dock = DockStyle.Fill; body.FlowDirection = FlowDirection.TopDown; body.WrapContents = false; body.AutoScroll = true;
            status.AutoSize = true; status.MaximumSize = new Size(680, 0); status.Margin = new Padding(0, 8, 0, 8);
            var footer = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
            back.Text = "Назад"; back.MinimumSize = new Size(120, 38); back.AutoSize = true; back.Click += delegate { Remember(); ShowStep(Math.Max(0, step - 1)); };
            next.Tag = "primary"; next.MinimumSize = new Size(180, 38); next.AutoSize = true; next.Click += async delegate { await Advance(); };
            footer.Controls.Add(back); footer.Controls.Add(next);
            layout.Controls.Add(heading, 0, 0); layout.Controls.Add(progress, 0, 1); layout.Controls.Add(body, 0, 2);
            layout.Controls.Add(status, 0, 3); layout.Controls.Add(footer, 0, 4); Controls.Add(layout);
            draftOwner = service.Owner ?? new HomeVpnOwner { Host = "", Port = 22, Login = "root", KeyFile = "" };
            draftHome = service.HomeAddress;
            refresh.Interval = 1000; refresh.Tick += delegate { RefreshStatus(); }; refresh.Start();
            ShowStep(service.Access == null ? 0 : 4);
        }

        private void ShowStep(int value)
        {
            step = value; progress.Step = value; body.SuspendLayout();
            foreach (Control control in body.Controls.Cast<Control>().ToArray()) control.Dispose();
            body.Controls.Clear(); counters = null; host = login = key = token = home = null; port = null; routerCheck = null;
            status.Text = ""; status.ForeColor = UiTheme.Muted;
            string[] titles = { "Как подключаемся?", own ? "Данные вашего VPS" : "Токен приглашения", "Подготовьте домашний роутер", "Добавьте VPN на телефон", "Проверка и управление" };
            heading.Text = (step + 1) + ". " + titles[step];
            back.Visible = step > 0; next.Visible = step > 0; next.Text = step == 4 ? "Закрыть" : "Далее";
            if (step == 0)
            {
                Paragraph("Мастер подключит телефон через этот ПК и VPS. iPhone использует встроенный IKEv2, Android — strongSwan VPN Client. Компьютер должен оставаться включённым и иметь доступный извне домашний адрес.");
                Action("Подключиться к готовому VPS", delegate { own = false; ShowStep(1); });
                Paragraph("Владелец VPS выдаёт вам токен. Пароль администратора не требуется.");
                Action("Добавить свой VPS", delegate { own = true; ShowStep(1); });
                Paragraph("Введите SSH-данные сервера Ubuntu. ProGo установит VPN и создаст личный доступ. Здесь же можно будет выдать отдельные токены друзьям.");
                if (service.Access != null) Action("Вернуться к текущему подключению", delegate { ShowStep(4); });
            }
            else if (step == 1 && own)
            {
                Paragraph("Укажите свой Ubuntu VPS. ProGo добавит VPN-сервер, ограниченные SSH-учётные записи и правила выхода VPN в интернет.");
                host = Field("Адрес VPS (IP или имя)", draftOwner.Host);
                Paragraph("SSH-порт"); port = new NumericUpDown { Minimum = 1, Maximum = 65535, Value = draftOwner.Port, Width = 120 }; body.Controls.Add(port);
                login = Field("SSH-пользователь: root или пользователь с sudo без пароля", draftOwner.Login);
                key = Field("Файл SSH-ключа (необязательно)", draftOwner.KeyFile);
                Action("Выбрать файл ключа…", delegate { using (var dialog = new OpenFileDialog()) if (dialog.ShowDialog(this) == DialogResult.OK) key.Text = dialog.FileName; });
                Paragraph("Если ключ не указан, SSH использует ваш агент/обычные ключи или попросит пароль в отдельном окне. Пароль не сохраняется в ProGo. При первом входе сверяйте отпечаток ключа сервера.");
                next.Text = "Настроить VPS и продолжить";
            }
            else if (step == 1)
            {
                Paragraph("Попросите владельца открыть «Доступ друзей» → «Создать токен». Вставьте полученный токен целиком. Он даёт доступ к VPN и должен оставаться личным.");
                token = Field("Токен PROGO1.…", draftToken); token.UseSystemPasswordChar = true;
                Action("Вставить из буфера", delegate { if (Clipboard.ContainsText()) token.Text = Clipboard.GetText().Trim(); });
                Paragraph("Токен содержит отдельный SSH-ключ только для VPN и учётную запись iPhone. Это не пароль администратора. Импортируйте токен только от знакомого владельца VPS.");
                next.Text = "Проверить токен и подключиться";
            }
            else if (step == 2)
            {
                Paragraph("Теперь телефону нужно попасть на этот компьютер через ваш роутер.");
                var addresses = String.Join(", ", NetworkInterface.GetAllNetworkInterfaces()
                    .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    .SelectMany(n => n.GetIPProperties().UnicastAddresses).Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
                    .Select(a => a.Address.ToString()).ToArray());
                Paragraph("Локальные IPv4-адреса ПК: " + addresses + ". Выберите адрес подключения к домашнему роутеру.");
                Action("1. Разрешить соединения в Windows", async delegate { await RunStep(AllowFirewall); });
                Paragraph("2. На роутере откройте «Переадресация портов» / «Port forwarding». Создайте две записи:");
                var grid = new DataGridView { Width = 655, Height = 102, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                    RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
                grid.Columns.Add("protocol", "Протокол"); grid.Columns.Add("external", "Порт снаружи"); grid.Columns.Add("destination", "Адрес назначения"); grid.Columns.Add("local", "Порт на ПК");
                grid.Rows.Add("UDP", "500", "Локальный IP этого ПК", "15000"); grid.Rows.Add("UDP", "4500", "Локальный IP этого ПК", "14500"); body.Controls.Add(grid);
                Paragraph("Закрепите выбранный IP за ПК в DHCP роутера. При CGNAT попросите провайдера подключить публичный IPv4. ProGo не может изменить настройки вашего роутера самостоятельно.");
                home = Field("3. Внешний IPv4 дома или DDNS (из настроек WAN роутера)", draftHome);
                Paragraph("Это адрес домашнего роутера, а не VPS и не локальный IP ПК.");
                routerCheck = new CheckBox { Text = "Оба UDP-порта направлены на этот ПК", AutoSize = true, Checked = routerDone }; body.Controls.Add(routerCheck);
            }
            else if (step == 3)
            {
                Paragraph("Сканируйте QR камерой телефона. Откроется защищённая страница с выбором iPhone или Android; сервер, логин и пароль уже будут заполнены.");
                Action("Установить на телефон по QR", async delegate { await ShowQr(); });
                Action("Настроить адрес выдачи QR…", delegate { HomeProfileShare.Configure(this, service); });
                Paragraph("iPhone: откройте страницу в Safari, разрешите загрузку и подтвердите установку в Настройки → Основные → VPN и управление устройством. Android: импортируйте профиль в strongSwan VPN Client.");
                Paragraph("Первая выдача требует HTTPS-домена на VPS. Владелец настраивает его здесь один раз; для друзей адрес сохраняется в новых токенах.");
                Action("Сохранить профиль iPhone файлом…", SaveProfile);
                Paragraph("После установки выключите Wi-Fi на телефоне, выберите «ProGo — домашний VPN» и включите VPN. Компьютер и канал ProGo должны оставаться включёнными.");
                next.Text = "Профиль установлен — проверить";
            }
            else
            {
                Paragraph(service.Access == null ? "Сначала добавьте VPS или токен." : "VPS: " + service.Access.Host + "\nДомашний адрес: " + (service.HomeAddress ?? "ещё не указан"));
                Action("Запустить канал", async delegate { await RunStep(async delegate { await service.StartAsync(); RefreshStatus(); }); });
                Action("Остановить канал", delegate { service.Stop(); RefreshStatus(); });
                counters = Paragraph("");
                Paragraph("На iPhone выключите Wi-Fi, выберите профиль ProGo и включите VPN. Затем откройте сайт проверки IP: должен отображаться выход вашего VPS. Счётчики подтверждают пересылку, а статус «Подключено» проверяется на телефоне.");
                Action("Установить на телефон по QR", async delegate { await ShowQr(); });
                Action("Настроить адрес выдачи QR…", delegate { HomeProfileShare.Configure(this, service); });
                Action("Настроить роутер / создать профиль снова", delegate { ShowStep(2); });
                Action("Выбрать другой VPS или токен", delegate { ShowStep(0); });
                if (service.Owner != null) Action("Доступ друзей…", async delegate { await ManageInvitations(); });
                if (service.Owner != null) Action("Исправить выход VPN в интернет", async delegate
                {
                    await RunStep(async delegate
                    {
                        await HomeVpnService.AdminAsync(service.Owner, "repair", null, null, SetProgress);
                        SetProgress("Правила выхода VPN обновлены. Переподключите VPN на телефоне и откройте сайт для проверки.");
                    });
                });
                Paragraph("Режим экспериментальный: IKEv2 нужно проверить с вашим iPhone и провайдером. При обрыве SSH/SOCKS при включённом автовосстановлении ProGo повторяет подключение; телефон может переподключать VPN несколько секунд.");
            }
            UiTheme.Apply(body); body.ResumeLayout(); RefreshStatus();
        }

        private void Remember()
        {
            if (host != null) draftOwner = new HomeVpnOwner { Host = host.Text.Trim(), Port = (int)port.Value, Login = login.Text.Trim(), KeyFile = key.Text.Trim() };
            if (token != null) draftToken = token.Text;
            if (home != null) draftHome = home.Text.Trim();
            if (routerCheck != null) routerDone = routerCheck.Checked;
        }
        private async Task Advance()
        {
            Remember(); if (step == 4) { Close(); return; }
            await RunStep(async delegate
            {
                if (step == 1)
                {
                    var value = draftToken;
                    if (own)
                    {
                        var ownerId = new JavaScriptSerializer().Serialize(draftOwner);
                        if (preparedToken == null || preparedOwner != ownerId)
                        {
                            preparedToken = await HomeVpnService.AdminAsync(draftOwner, "setup", "My iPhone", null, SetProgress);
                            preparedOwner = ownerId;
                        }
                        value = preparedToken;
                    }
                    service.UseToken(value, own ? draftOwner : null);
                    SetProgress("Проверяем защищённый канал к VPS…"); await service.StartAsync(); draftToken = "";
                }
                else if (step == 2)
                {
                    if (!routerDone) throw new InvalidOperationException("Сначала настройте обе записи на роутере и отметьте галочку.");
                    service.SetHomeAddress(draftHome);
                    profileSaved = false;
                }
                else if (step == 3 && !profileSaved) throw new InvalidOperationException("Сначала откройте QR или сохраните профиль, затем установите его на телефон.");
                ShowStep(step + 1);
            });
        }
        private async Task RunStep(Func<Task> action)
        {
            if (busy) return;
            busy = true; body.Enabled = false; back.Enabled = next.Enabled = false; status.ForeColor = UiTheme.Muted;
            try { await action(); }
            catch (Exception ex) { status.ForeColor = UiTheme.Error; status.Text = ex.Message; }
            finally { busy = false; if (!IsDisposed) { body.Enabled = true; back.Enabled = next.Enabled = true; } }
        }
        private void SetProgress(string text) { status.Text = text; }
        private Label Paragraph(string text)
        {
            var label = new Label { Text = text, AutoSize = true, MaximumSize = new Size(650, 0), Margin = new Padding(0, 0, 0, 12) };
            body.Controls.Add(label); return label;
        }
        private TextBox Field(string title, string value)
        {
            Paragraph(title); var field = new TextBox { Text = value ?? "", Width = 640, Margin = new Padding(0, 0, 0, 12) }; body.Controls.Add(field); return field;
        }
        private void Action(string title, EventHandler handler)
        {
            var button = new Button { Text = title, AutoSize = true, MinimumSize = new Size(260, 36), Margin = new Padding(0, 0, 0, 12) };
            button.Click += handler; body.Controls.Add(button);
        }
        private async Task AllowFirewall()
        {
            var script = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scripts", "Enable-HomeVpnFirewall.ps1");
            using (var process = Process.Start(new ProcessStartInfo("powershell.exe", "-NoProfile -ExecutionPolicy Bypass -File "
                + HomeVpnService.Argument(script) + " -ProGoExe " + HomeVpnService.Argument(Application.ExecutablePath)) { UseShellExecute = true, Verb = "runas" }))
            {
                await Task.Run(delegate { process.WaitForExit(); });
                if (process.ExitCode != 0) throw new InvalidOperationException("Windows не подтвердила добавление правил. Повторите и разрешите запрос администратора.");
            }
            SetProgress("Windows разрешает входящие UDP 15000 и 14500 для ProGo. Следующий шаг — роутер.");
        }
        private async Task ShowQr()
        {
            if (service.Access == null || !HomeVpnAccess.ValidHost(service.HomeAddress))
            { ShowStep(2); status.Text = "Сначала укажите внешний домашний адрес."; return; }
            if (String.IsNullOrEmpty(service.ShareOrigin) && !HomeProfileShare.Configure(this, service)) return;
            await RunStep(async delegate
            {
                SetProgress("Создаём QR на 15 минут. Предыдущая ссылка этого доступа перестанет работать…");
                var origin = service.ShareOrigin;
                var access = service.Access;
                var link = await HomeProfileShare.CreateAsync(origin, access, service.HomeAddress);
                using (var dialog = new PhoneProfileQrForm(link, delegate { return HomeProfileShare.RevokeAsync(origin, access); }))
                    dialog.ShowDialog(this);
                profileSaved = true;
                SetProgress("После установки выберите VPN ProGo на телефоне. Закрытие окна не отзывает ссылку; она действует до истечения срока.");
            });
        }
        private void SaveProfile(object sender, EventArgs args)
        {
            using (var dialog = new SaveFileDialog { Filter = "Профиль iPhone|*.mobileconfig", FileName = "ProGo-iPhone.mobileconfig" })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    if (File.Exists(dialog.FileName)) throw new IOException("Выберите новое имя файла: старый профиль сохранён.");
                    service.Access.WriteProfile(dialog.FileName, service.HomeAddress);
                    profileSaved = true; status.Text = "Профиль сохранён. Передайте файл на iPhone и установите его.";
                }
                catch (Exception ex) { status.ForeColor = UiTheme.Error; status.Text = ex.Message; }
            }
        }
        private async Task ManageInvitations()
        {
            await RunStep(async delegate
            {
                var response = await HomeVpnService.AdminAsync(service.Owner, "list", null, null, SetProgress);
                var items = new JavaScriptSerializer().Deserialize<HomeVpnInvitation[]>(response);
                using (var dialog = new ProGoForm { Text = "Доступ друзей", Size = new Size(630, 440), StartPosition = FormStartPosition.CenterParent })
                {
                    var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), FlowDirection = FlowDirection.TopDown, WrapContents = false };
                    var list = new ListBox { Width = 570, Height = 175 }; foreach (var item in items) list.Items.Add(item);
                    var label = new TextBox { Width = 570, Text = "Друг" };
                    var create = new Button { AutoSize = true, Text = "Создать отдельный токен" };
                    var revoke = new Button { AutoSize = true, Text = "Отозвать выбранный доступ" };
                    panel.Controls.Add(list); panel.Controls.Add(new Label { Text = "Имя для нового приглашения", AutoSize = true }); panel.Controls.Add(label);
                    panel.Controls.Add(create); panel.Controls.Add(revoke); dialog.Controls.Add(panel);
                    bool working = false;
                    dialog.FormClosing += delegate(object s, FormClosingEventArgs e) { if (working) e.Cancel = true; };
                    create.Click += async delegate
                    {
                        working = true; create.Enabled = revoke.Enabled = false;
                        try
                        {
                            var value = await HomeVpnService.AdminAsync(service.Owner, "invite", label.Text, null, delegate { });
                            var access = HomeVpnAccess.Parse(value);
                            using (var share = new ProGoForm { Text = "Личный токен для друга", Size = new Size(630, 290), StartPosition = FormStartPosition.CenterParent })
                            {
                                var description = new Label { Dock = DockStyle.Top, Height = 80, Padding = new Padding(12), Text = "Отправьте токен другу лично. Он выберет в ProGo «Подключиться к готовому VPS». Токен даёт доступ к VPN до отзыва владельцем; не публикуйте его." };
                                var secret = new TextBox { Dock = DockStyle.Top, UseSystemPasswordChar = true, Text = value };
                                var copy = new Button { Dock = DockStyle.Top, Text = "Скопировать токен", Height = 36 };
                                copy.Click += delegate { Clipboard.SetText(value); copy.Text = "Скопировано — передайте другу"; };
                                share.Controls.Add(copy); share.Controls.Add(secret); share.Controls.Add(description); share.ShowDialog(dialog);
                            }
                            list.Items.Add(new HomeVpnInvitation { Id = access.InviteId, Name = label.Text });
                        }
                        catch (Exception ex) { MessageBox.Show(dialog, ex.Message, "Не удалось создать токен"); }
                        finally { working = false; create.Enabled = revoke.Enabled = true; }
                    };
                    revoke.Click += async delegate
                    {
                        var selected = list.SelectedItem as HomeVpnInvitation;
                        if (selected == null || selected.Revoked) return;
                        if (MessageBox.Show(dialog, "Отключить доступ «" + selected.Name + "»? Его действующие соединения будут закрыты.", "Отзыв доступа", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
                        working = true; create.Enabled = revoke.Enabled = false;
                        try
                        {
                            await HomeVpnService.AdminAsync(service.Owner, "revoke", null, selected.Id, delegate { });
                            selected.Revoked = true; var index = list.SelectedIndex; list.Items[index] = selected;
                        }
                        catch (Exception ex) { MessageBox.Show(dialog, ex.Message, "Не удалось отозвать доступ"); }
                        finally { working = false; create.Enabled = revoke.Enabled = true; }
                    };
                    dialog.ShowDialog(this);
                }
                status.Text = "";
            });
        }
        private void RefreshStatus()
        {
            if (step != 4 || counters == null) return;
            counters.Text = (service.Relay.IsRunning ? "Канал ПК → VPS запущен. Подключение телефона и интернет ещё не проверены." : "Канал остановлен.") + "\nSOCKS: " + service.RecoveryStatus
                + "\nПакеты: от телефона " + service.Relay.Received + ", на VPS " + service.Relay.Sent + ", обратно " + service.Relay.Returned
                + (service.Relay.Received == 0 ? "\nПакетов от телефона пока нет: проверьте выбранный профиль и проброс портов." : "")
                + (service.Relay.LastError == null ? "" : "\n" + service.Relay.LastError);
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (busy) { e.Cancel = true; status.Text = "Дождитесь окончания операции. При запросе SSH ответьте в открывшемся окне."; }
            base.OnFormClosing(e);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) { refresh.Stop(); refresh.Dispose(); }
            base.Dispose(disposing);
        }
    }
}

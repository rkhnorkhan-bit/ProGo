using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace ProGo
{
    internal sealed class HomeVpnWizardForm : ProGoForm
    {
        private readonly OwnerUiDispatcher ownerUi = new OwnerUiDispatcher();
        private readonly HomeVpnService service;
        private readonly ClipboardService clipboard;
        private readonly HomeProfileUiDispatcher httpDispatcher;
        private readonly FlowLayoutPanel body = new FlowLayoutPanel();
        private readonly WizardProgress progress = new WizardProgress();
        private readonly Label heading = new Label();
        private readonly Label status = UiTheme.StatusLabel(UiTheme.Muted);
        private readonly Button back = new Button();
        private readonly Button next = new Button();
        private readonly Button cancelWait = new Button();
        private readonly object channelWaitGate = new object();
        private CancellationTokenSource channelWait;
        private bool closeAfterWait, cancelledWait;
        private readonly System.Windows.Forms.Timer refresh = new System.Windows.Forms.Timer();
        private int step;
        private bool own, routerDone, recoverSetup, setupBlocked, fittingSetup;
        private volatile bool busy;
        private readonly PhoneVerification verification = new PhoneVerification();
        private CheckBox installedCheck;
        private ComboBox internetCheck;
        private Label profileState, setupState;
        private TextBox host, login, key, token, home;
        private NumericUpDown port;
        private CheckBox routerCheck;
        private HomeVpnOwner draftOwner;
        private string draftToken = "", draftHome;
        private string preparedToken, preparedOwner;
        private Label counters;
        internal Func<HomeVpnOwner, string, string, string, Action<string>, Task<string>> Admin = HomeVpnService.AdminAsync;

        internal HomeVpnWizardForm(HomeVpnService service, ClipboardService clipboard)
        {
            this.service = service; this.clipboard = clipboard; httpDispatcher = new HomeProfileUiDispatcher(this);
            Text = "VPN для телефона"; AutoScaleMode = AutoScaleMode.Dpi;
            Font = new Font("Segoe UI", 10); StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(750, 650); MinimumSize = new Size(700, 620);
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), RowCount = 5, ColumnCount = 1 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < 5; i++) layout.RowStyles.Add(new RowStyle(i == 2 ? SizeType.Percent : SizeType.AutoSize, i == 2 ? 100 : 0));
            heading.AutoSize = true; heading.Font = UiTheme.Heading; heading.Margin = new Padding(0, 0, 0, 8);
            DescribeStatus(heading, "Текущий шаг настройки VPN");
            DescribeStatus(status, "Результат операции настройки VPN");
            progress.TabStop = false; progress.AccessibleRole = AccessibleRole.StaticText;
            back.AccessibleDescription = "Возвращает к предыдущему шагу. Введённые данные остаются в этом мастере.";
            progress.Dock = DockStyle.Top; progress.Height = 64; progress.Margin = new Padding(0, 0, 0, 16);
            body.Dock = DockStyle.Fill; body.FlowDirection = FlowDirection.TopDown; body.WrapContents = false; body.AutoScroll = true;
            body.ClientSizeChanged += delegate { FitSetupWidth(); };
            body.Layout += delegate { FitSetupWidth(); };
            status.AutoSize = true; status.MaximumSize = new Size(680, 0); status.Margin = new Padding(0, 8, 0, 8);
            layout.SizeChanged += delegate { status.MaximumSize = new Size(Math.Max(1, layout.ClientSize.Width - layout.Padding.Horizontal - status.Margin.Horizontal), 0); };
            var footer = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
            back.Text = "Назад"; back.MinimumSize = new Size(120, 38); back.AutoSize = true; back.Click += delegate { Remember(); ShowStep(Math.Max(0, step - 1)); };
            next.Tag = "primary"; next.MinimumSize = new Size(180, 38); next.AutoSize = true; next.Click += async delegate { await Advance(); };
            next.TextChanged += delegate { next.AccessibleName = next.Text; };
            footer.Controls.Add(back); footer.Controls.Add(next);
            cancelWait.Text = "Отменить запуск канала"; cancelWait.AutoSize = true; cancelWait.MinimumSize = new Size(180, 38);
            cancelWait.AccessibleName = cancelWait.Text;
            cancelWait.AccessibleDescription = "Отменяет только запуск канала к VPS. Сохранённый доступ остаётся; окно ждёт остановки процессов.";
            cancelWait.Visible = cancelWait.Enabled = false; cancelWait.Click += delegate { CancelChannelWait(); };
            footer.Controls.Add(cancelWait);
            layout.Controls.Add(heading, 0, 0); layout.Controls.Add(progress, 0, 1); layout.Controls.Add(body, 0, 2);
            layout.Controls.Add(status, 0, 3); layout.Controls.Add(footer, 0, 4); Controls.Add(layout);
            draftOwner = service.Owner ?? new HomeVpnOwner { Host = "", Port = 22, Login = "root", KeyFile = "" };
            draftHome = service.HomeAddress;
            string pendingError = null;
            try {
                var pending = HomeVpnSetupRecovery.PendingOwner();
                if (pending != null) {
                    if (String.Equals(pending.Host, draftOwner.Host, StringComparison.OrdinalIgnoreCase) && pending.Port == draftOwner.Port && pending.Login == draftOwner.Login)
                        pending.KeyFile = draftOwner.KeyFile;
                    draftOwner = pending; own = true;
                }
            } catch (HomeVpnSetupPendingException ex) { own = true; pendingError = ex.Message; }
            refresh.Interval = 1000; refresh.Tick += delegate { RefreshStatus(); }; refresh.Start();
            ShowStep(own ? 1 : service.Access == null ? 0 : 4);
            if (pendingError != null) { status.ForeColor = UiTheme.Error; status.Text = pendingError; }
        }

        private void ShowStep(int value)
        {
            if (IsDisposed || Disposing) return;
            step = value; progress.Step = value; body.SuspendLayout();
            foreach (Control control in body.Controls.Cast<Control>().ToArray()) control.Dispose();
            body.Controls.Clear(); counters = null; installedCheck = null; internetCheck = null; profileState = setupState = null; host = login = key = token = home = null; port = null; routerCheck = null;
            setupBlocked = recoverSetup = false;
            status.Text = ""; status.ForeColor = UiTheme.Muted;
            string[] titles = { "Как подключаемся?", own ? "Данные вашего VPS" : "Токен приглашения", "Подготовьте домашний роутер", "Добавьте VPN на телефон", "Проверка и управление" };
            heading.Text = (step + 1) + ". " + titles[step];
            progress.AccessibleDescription = heading.Text;
            next.AccessibleDescription = step == 4 ? "Закрывает мастер. Работающий канал VPN остаётся включённым."
                : step == 3 ? "Переходит к проверке после вашего подтверждения установки. Интернет на телефоне автоматически не проверяется."
                : step == 2 ? "Сохраняет внешний домашний адрес после подтверждения переадресации портов. Настройки роутера автоматически не проверяются."
                : own ? "Настраивает VPN на вашем VPS через SSH и запускает канал. Изменяет настройки сервера."
                : "Проверяет токен приглашения, сохраняет доступ и запускает канал к VPS.";
            back.Visible = step > 0; next.Visible = step > 0; next.Text = step == 4 ? "Закрыть" : "Далее";
            if (step == 0)
            {
                Paragraph("Мастер подключит телефон через этот ПК и VPS. iPhone использует встроенный IKEv2, Android — strongSwan VPN Client. Компьютер должен оставаться включённым и иметь доступный извне домашний адрес.");
                Action("Подключиться к готовому VPS", delegate { own = false; ShowStep(1); }, "Открывает ввод личного токена приглашения владельца VPS.");
                Paragraph("Владелец VPS выдаёт вам токен. Пароль администратора не требуется.");
                Action("Добавить свой VPS", delegate { own = true; ShowStep(1); }, "Открывает ввод SSH-данных вашего VPS. Настройка сервера начнётся после подтверждения следующего шага.");
                Paragraph("Введите SSH-данные сервера Ubuntu. ProGo установит VPN и создаст личный доступ. Здесь же можно будет выдать отдельные токены друзьям.");
                if (service.Access != null) Action("Вернуться к текущему подключению", delegate { ShowStep(4); }, "Открывает состояние текущего канала без повторной настройки VPS.");
            }
            else if (step == 1 && own)
            {
                setupState = Paragraph(""); DescribeStatus(setupState, "Режим настройки своего VPS");
                host = Field("Адрес VPS (IP или имя)", draftOwner.Host);
                Paragraph("SSH-порт"); port = new NumericUpDown { Minimum = 1, Maximum = 65535, Value = draftOwner.Port, Width = 120, AccessibleName = "SSH-порт", AccessibleDescription = "Порт подключения SSH к вашему VPS." }; body.Controls.Add(port);
                login = Field("SSH-пользователь: root или пользователь с sudo без пароля", draftOwner.Login);
                key = Field("Файл SSH-ключа (необязательно)", draftOwner.KeyFile);
                Action("Выбрать файл ключа…", delegate { using (var dialog = new OpenFileDialog()) if (dialog.ShowDialog(this) == DialogResult.OK) key.Text = dialog.FileName; }, "Выбирает локальный файл закрытого SSH-ключа. Подключение к серверу ещё не выполняется.");
                Paragraph("Если ключ не указан, SSH использует ваш агент/обычные ключи или попросит пароль в отдельном окне. Пароль не сохраняется в ProGo. При первом входе сверяйте отпечаток ключа сервера.");
                host.TextChanged += delegate { UpdateSetupRecoveryState(); }; login.TextChanged += delegate { UpdateSetupRecoveryState(); };
                key.TextChanged += delegate { UpdateSetupRecoveryState(); }; port.ValueChanged += delegate { UpdateSetupRecoveryState(); };
                UpdateSetupRecoveryState();
            }
            else if (step == 1)
            {
                Paragraph("Попросите владельца открыть «Доступ друзей» → «Создать токен». Вставьте полученный токен целиком. Он даёт доступ к VPN и должен оставаться личным.");
                token = Field("Токен PROGO1.…", draftToken); token.UseSystemPasswordChar = true;
                token.AccessibleDescription = "Личный токен приглашения. Содержимое скрыто; вставьте токен целиком.";
                Action("Вставить из буфера", delegate { if (Clipboard.ContainsText()) token.Text = Clipboard.GetText().Trim(); }, "Вставляет личный токен из текущего буфера обмена. Проверка выполняется при продолжении.");
                Paragraph("Токен содержит отдельный SSH-ключ только для VPN и учётную запись VPN телефона. Это не пароль администратора. Импортируйте токен только от знакомого владельца VPS.");
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
                Action("1. Разрешить соединения в Windows", async delegate { await RunStep(AllowFirewall); }, "Запрашивает права администратора и добавляет входящие UDP-правила Windows для VPN. Роутер настраивается отдельно.");
                Paragraph("2. На роутере откройте «Переадресация портов» / «Port forwarding». Создайте две записи:");
                var grid = new DataGridView { Width = 655, Height = 102, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                    RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, StandardTab = true,
                    AccessibleName = "Правила переадресации портов", AccessibleDescription = "Только чтение: две UDP-записи для домашнего роутера. Стрелки перемещаются по таблице; Tab переходит к домашнему адресу." };
                grid.Columns.Add("protocol", "Протокол"); grid.Columns.Add("external", "Порт снаружи"); grid.Columns.Add("destination", "Адрес назначения"); grid.Columns.Add("local", "Порт на ПК");
                grid.Rows.Add("UDP", "500", "Локальный IP этого ПК", "15000"); grid.Rows.Add("UDP", "4500", "Локальный IP этого ПК", "14500"); body.Controls.Add(grid);
                Paragraph("Закрепите выбранный IP за ПК в DHCP роутера. При CGNAT попросите провайдера подключить публичный IPv4. ProGo не может изменить настройки вашего роутера самостоятельно.");
                home = Field("3. Внешний IPv4 дома или DDNS (из настроек WAN роутера)", draftHome);
                Paragraph("Это адрес домашнего роутера, а не VPS и не локальный IP ПК.");
                routerCheck = new CheckBox { Text = "Оба UDP-порта направлены на этот ПК", AutoSize = true, Checked = routerDone,
                    AccessibleDescription = "Ваше подтверждение настройки роутера. ProGo не проверяет эти правила автоматически." }; body.Controls.Add(routerCheck);
            }
            else if (step == 3)
            {
                Paragraph("Сканируйте QR камерой телефона. Откроется защищённая страница с выбором iPhone или Android; сервер, логин и пароль уже будут заполнены.");
                Action("Установить на телефон по QR", async delegate { await ShowQr(); }, "Создаёт временную ссылку и QR для установки профиля. Предыдущая ссылка доступа перестаёт работать; установка телефоном не подтверждается.");
                Action("Настроить адрес выдачи QR…", delegate { HomeProfileShare.Configure(this, service); }, "Открывает настройку HTTPS-адреса выдачи профилей на VPS.");
                Action("Сохранить профиль iPhone файлом…", SaveProfile, "Открывает выбор нового файла профиля iPhone. Сохранение файла не подтверждает установку на телефоне.");
                profileState = Paragraph(verification.IssuanceText); DescribeStatus(profileState, "Выдача профиля VPN");
                Paragraph("iPhone: откройте страницу в Safari, разрешите загрузку и подтвердите установку в Настройки → Основные → VPN и управление устройством. Android: импортируйте профиль в strongSwan VPN Client.");
                AddInstallationConfirmation();
                Paragraph("Первая выдача требует HTTPS-домена на VPS. Владелец настраивает его здесь один раз; для друзей адрес сохраняется в новых токенах.");
                Paragraph("После установки выключите Wi-Fi на телефоне, выберите «ProGo — домашний VPN» и включите VPN. Компьютер и канал ProGo должны оставаться включёнными.");
                next.Text = "Перейти к проверке";
            }
            else
            {
                Paragraph(service.Access == null ? "Сначала добавьте VPS или токен." : "VPS: " + service.Access.Host + "\nДомашний адрес: " + (service.HomeAddress ?? "ещё не указан"));
                Action("Запустить канал", async delegate { await RunStep(async delegate { await ownerUi.Await(StartChannel()); verification.SetInternet(PhoneInternet.Unknown); RefreshStatus(); }); }, "Запускает канал этого ПК к VPS. Подключение телефона и интернет проверяются отдельно.");
                Action("Остановить VPN для телефона", delegate { service.Stop(); verification.SetInternet(PhoneInternet.Unknown); RefreshStatus(); }, "Останавливает только VPN телефона и сбрасывает результат проверки интернета. Прокси на ПК не отключается.");
                AddInstallationConfirmation();
                Paragraph("Проверка на телефоне: 1. Выключите Wi-Fi и включите VPN ProGo. 2. Убедитесь, что телефон показывает «Подключено». 3. Откройте сайт проверки IP и сравните IPv4 с адресом выхода VPS. 4. Отметьте результат ниже. Это ваша проверка, ProGo не выполняет её на телефоне автоматически.");
                internetCheck = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 640, AccessibleName = "Результат проверки интернета на телефоне",
                    AccessibleDescription = "Выберите результат своей проверки на телефоне. Доступно после подтверждения установки профиля; счётчики пакетов не подтверждают интернет." };
                internetCheck.Items.AddRange(new object[] { "Ещё не проверял", "VPN подключён, но интернета нет", "Сайт открылся, IP совпадает с VPS" });
                internetCheck.SelectedIndex = (int)verification.Internet; internetCheck.Enabled = verification.Installed;
                internetCheck.SelectedIndexChanged += delegate { if (internetCheck.SelectedIndex >= 0) verification.SetInternet((PhoneInternet)internetCheck.SelectedIndex); RefreshStatus(); };
                body.Controls.Add(internetCheck);
                counters = Paragraph(""); DescribeStatus(counters, "Состояние канала и проверки телефона");
                Paragraph("На телефоне выключите Wi-Fi, выберите профиль ProGo и включите VPN. Затем откройте сайт проверки IP: должен отображаться выход вашего VPS. Счётчики подтверждают пересылку, а статус «Подключено» проверяется на телефоне.");
                Action("Установить на телефон по QR", async delegate { await ShowQr(); }, "Создаёт временную ссылку и QR для установки профиля. Предыдущая ссылка доступа перестаёт работать; установка телефоном не подтверждается.");
                Action("Настроить адрес выдачи QR…", delegate { HomeProfileShare.Configure(this, service); }, "Открывает настройку HTTPS-адреса выдачи профилей на VPS.");
                Action("Настроить роутер / создать профиль снова", delegate { ShowStep(2); }, "Возвращает к домашнему адресу и правилам роутера для повторной выдачи профиля.");
                Action("Выбрать другой VPS или токен", delegate { ShowStep(0); }, "Открывает выбор другого доступа. Сам переход не останавливает текущий канал.");
                if (service.Owner != null) Action("Доступ друзей…", async delegate { await ManageInvitations(); }, "Запрашивает список друзей на VPS и открывает управление отдельными токенами доступа.");
                if (service.Owner != null) Action("Исправить выход VPN в интернет", async delegate
                {
                    await RunStep(async delegate
                    {
                        await ownerUi.Await(Admin(service.Owner, "repair", null, null, SetProgress));
                        SetProgress("Правила выхода VPN обновлены. Переподключите VPN на телефоне и откройте сайт для проверки.");
                    });
                }, "Обновляет правила выхода VPN на VPS через SSH. После этого переподключите VPN на телефоне и проверьте интернет.");
                Paragraph("Режим экспериментальный: IKEv2 нужно проверить с вашим телефоном и провайдером. При обрыве SSH/SOCKS при включённом автовосстановлении ProGo повторяет подключение; телефон может переподключать VPN несколько секунд.");
            }
            UiTheme.ConfigureKeyboardOrder(this);
            UiTheme.Apply(body); body.ResumeLayout(); FitSetupWidth(); RefreshStatus();
        }

        private static void DescribeStatus(Label label, string name)
        {
            label.AccessibleName = name; label.AccessibleDescription = label.Text;
            label.TextChanged += delegate { label.AccessibleDescription = label.Text; };
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
                        // A recovery action stays read-only even if the pending file
                        // disappears after the button was presented to the user.
                        bool checking = recoverSetup || HomeVpnSetupRecovery.HasPending();
                        var ownerId = new JavaScriptSerializer().Serialize(draftOwner);
                        if (checking || preparedToken == null || preparedOwner != ownerId)
                        {
                            preparedToken = await ownerUi.Await(Admin(draftOwner, checking ? "recover-setup" : "setup", "My iPhone", null, SetProgress));
                            preparedOwner = ownerId;
                        }
                        value = preparedToken;
                    }
                    service.UseToken(value, own ? draftOwner : null); verification.Reset();
                    if (own) HomeVpnSetupRecovery.ConfirmConsumed(draftOwner, value);
                    await ownerUi.Await(StartChannel()); draftToken = "";
                }
                else if (step == 2)
                {
                    if (!routerDone) throw new InvalidOperationException("Сначала настройте обе записи на роутере и отметьте галочку.");
                    service.SetHomeAddress(draftHome);
                    verification.Reset();
                }
                else if (step == 3 && !verification.Installed) throw new InvalidOperationException("Подтвердите установку профиля в настройках телефона. Создание QR или сохранение файла не подтверждает установку.");
                ShowStep(step + 1);
            });
        }
        private async Task RunStep(Func<Task> action)
        {
            if (busy || ownerUi.Closed || IsDisposed || Disposing) return;
            cancelledWait = closeAfterWait = false;
            busy = true; body.Enabled = false; back.Enabled = next.Enabled = false; status.ForeColor = UiTheme.Muted;
            try { await ownerUi.Await(action()); }
            catch (HomeVpnListCancelledException ex)
            {
                closeAfterWait = false;
                if (!ownerUi.Closed && !IsDisposed && !Disposing) { status.ForeColor = UiTheme.Muted; status.Text = ex.Message; }
            }
            catch (HomeVpnPreparationCancelledException ex)
            {
                closeAfterWait = false;
                if (!ownerUi.Closed && !IsDisposed && !Disposing) { status.ForeColor = UiTheme.Muted; status.Text = ex.Message; }
            }
            catch (HomeProfileHttpCancelledException ex)
            {
                closeAfterWait = false;
                if (!ownerUi.Closed && !IsDisposed && !Disposing) { status.ForeColor = UiTheme.Muted; status.Text = ex.Message; }
            }
            catch (OperationCanceledException)
            {
                if (!ownerUi.Closed && !IsDisposed && !Disposing) {
                    status.ForeColor = cancelledWait ? UiTheme.Muted : UiTheme.Error;
                    status.Text = cancelledWait ? "Запуск канала отменён. Сохранённый доступ к VPS остаётся; можно повторить запуск." : "Операция прервана. Результат не подтверждён.";
                }
            }
            catch (Exception ex) { closeAfterWait = false; if (!ownerUi.Closed && !IsDisposed && !Disposing) { status.ForeColor = UiTheme.Error; status.Text = ex.Message; } }
            finally {
                busy = false;
                if (!ownerUi.Closed && !IsDisposed && !Disposing) {
                    body.Enabled = true; back.Enabled = true; next.Enabled = step != 3 || verification.Installed;
                    UpdateSetupRecoveryState();
                    if (closeAfterWait) Close();
                }
            }
        }
        private async Task StartChannel()
        {
            using (var source = new CancellationTokenSource()) {
                lock (channelWaitGate) channelWait = source;
                cancelWait.Visible = cancelWait.Enabled = true; CancelButton = cancelWait;
                UiTheme.ConfigureKeyboardOrder(this); cancelWait.Focus();
                SetProgress("Ожидаем канал к VPS: до 10 секунд для SOCKS, затем до 10 секунд для приёмника. Можно отменить запуск; сохранённый доступ остаётся.");
                try { await ownerUi.Await(service.StartAsync(source.Token)); SetProgress("Канал ПК → VPS запущен. Подключение VPN и интернет проверяются на телефоне отдельно."); }
                catch (OperationCanceledException) { cancelledWait = source.IsCancellationRequested; throw; }
                finally {
                    // Closed-owner continuations can run off the UI thread.
                    // A concurrent Dispose must finish cancellation before the
                    // using scope disposes this source.
                    lock (channelWaitGate) channelWait = null;
                    if (!ownerUi.Closed && !IsDisposed && !Disposing) { cancelWait.Visible = cancelWait.Enabled = false; CancelButton = null; }
                }
            }
        }
        private void CancelChannelWait()
        {
            lock (channelWaitGate) {
                var source = channelWait;
                if (source == null || source.IsCancellationRequested) return;
                if (!ownerUi.Closed && !IsDisposed && !Disposing) cancelWait.Enabled = false;
                SetProgress("Отменяем запуск канала и закрываем его процессы. Дождитесь результата…");
                source.Cancel();
            }
        }
        private void SetProgress(string text)
        {
            if (ownerUi.OnOwner) { if (!ownerUi.Closed && !IsDisposed && !Disposing) status.Text = text; }
            else ownerUi.PostUpdate(delegate { if (!ownerUi.Closed && !IsDisposed && !Disposing) status.Text = text; });
        }
        private void FitSetupWidth()
        {
            if (step != 1 || !own || fittingSetup || body.IsDisposed) return;
            fittingSetup = true;
            try {
                int width = Math.Max(1, body.ClientSize.Width - body.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth - 6);
                foreach (Control control in body.Controls) {
                    int available = Math.Max(1, width - control.Margin.Horizontal);
                    var label = control as Label;
                    if (label != null) label.MaximumSize = new Size(Math.Min(650, available), 0);
                    else if (control is TextBox) {
                        // Constrain the preferred size too: FlowLayout uses it to
                        // calculate the scroll extent, not just the visible bounds.
                        int fieldWidth = Math.Min(640, available);
                        control.MaximumSize = new Size(fieldWidth, 0);
                        if (control.Width != fieldWidth) control.Width = fieldWidth;
                    }
                }
            } finally { fittingSetup = false; }
        }
        private void UpdateSetupRecoveryState()
        {
            if (step != 1 || !own || setupState == null) return;
            var entered = new HomeVpnOwner { Host = host.Text.Trim(), Port = (int)port.Value, Login = login.Text.Trim(), KeyFile = key.Text.Trim() };
            bool retained = preparedToken != null && preparedOwner == new JavaScriptSerializer().Serialize(entered);
            try {
                recoverSetup = HomeVpnSetupRecovery.PendingOwner() != null; setupBlocked = false;
                setupState.Text = recoverSetup
                    ? "Ответ прошлой настройки не подтверждён. ProGo сохранил запрос и проверит его результат. Новый доступ не выдаётся. Используйте исходные SSH-данные; пароль при необходимости вводится в отдельном окне."
                    : retained ? "Доступ к VPS уже сохранён. Продолжение запустит канал с этим доступом; повторная настройка сервера не требуется."
                    : "Укажите свой Ubuntu VPS. ProGo добавит VPN-сервер, ограниченные SSH-учётные записи и правила выхода VPN в интернет.";
            } catch (HomeVpnSetupPendingException ex) { recoverSetup = setupBlocked = true; setupState.Text = ex.Message; }
            next.Text = recoverSetup ? "Проверить прошлую настройку" : retained ? "Запустить канал и продолжить" : "Настроить VPS и продолжить";
            next.AccessibleName = next.Text;
            next.AccessibleDescription = recoverSetup ? "Проверяет сохранённый запрос на VPS и получает исходный доступ. Новая настройка и повторная выдача доступа не выполняются."
                : retained ? "Использует уже сохранённый доступ и запускает канал к VPS. Настройка сервера не повторяется."
                : "Настраивает VPN на вашем VPS через SSH и запускает канал. Изменяет настройки сервера.";
            next.Enabled = !busy && !setupBlocked;
        }
        private Label Paragraph(string text)
        {
            var label = new Label { Text = text, AutoSize = true, MaximumSize = new Size(650, 0), Margin = new Padding(0, 0, 0, 12) };
            body.Controls.Add(label); return label;
        }
        private TextBox Field(string title, string value)
        {
            Paragraph(title); var field = new TextBox { Text = value ?? "", AccessibleName = title, Width = 640, Margin = new Padding(0, 0, 0, 12) }; body.Controls.Add(field); return field;
        }
        private void Action(string title, EventHandler handler, string description)
        {
            var button = new Button { Text = title, AccessibleDescription = description, AutoSize = true, MinimumSize = new Size(260, 36), Margin = new Padding(0, 0, 0, 12) };
            button.Click += handler; body.Controls.Add(button);
        }
        private async Task AllowFirewall()
        {
            var script = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scripts", "Enable-HomeVpnFirewall.ps1");
            using (var process = Process.Start(new ProcessStartInfo("powershell.exe", "-NoProfile -ExecutionPolicy Bypass -File "
                + HomeVpnService.Argument(script) + " -ProGoExe " + HomeVpnService.Argument(Application.ExecutablePath)) { UseShellExecute = true, Verb = "runas" }))
            {
                await ownerUi.Await(Task.Run(delegate { process.WaitForExit(); }));
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
                string homeAddress = service.HomeAddress;
                var link = await ownerUi.Await(HomeProfileHttpWaitForm.WaitAsync(this, "Создание QR: ожидание HTTPS",
                    token => HomeProfileShare.CreateAsync(origin, access, homeAddress, token)));
                await httpDispatcher.DispatchAsync(delegate {
                    if (!Object.ReferenceEquals(service.Access, access)) throw new InvalidOperationException("Доступ к VPS изменился во время создания QR. Результат не показан; ProGo не повторяет запрос автоматически.");
                    RecordProfileIssue();
                    using (var dialog = new PhoneProfileQrForm(link, delegate { return HomeProfileShare.RevokeAsync(origin, access); }, clipboard))
                        dialog.ShowDialog(this);
                    SetProgress("QR создан; установка не подтверждена. Если код истёк или ссылка отозвана, создайте новый QR. Закрытие окна не подтверждает сканирование и не отзывает ссылку.");
                }).ConfigureAwait(false);
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
                    RecordProfileIssue(); status.Text = "Профиль сохранён. Передайте файл на iPhone и установите его.";
                }
                catch (Exception ex) { status.ForeColor = UiTheme.Error; status.Text = ex.Message; }
            }
        }
        private async Task ManageInvitations()
        {
            await RunStep(async delegate
            {
                var response = await ownerUi.Await(Admin(service.Owner, "list", null, null, SetProgress));
                var items = new JavaScriptSerializer().Deserialize<HomeVpnInvitation[]>(response);
                using (var dialog = new HomeInvitationsForm(items,
                    (action, label, id) => Admin(service.Owner, action, label, id, delegate { }), clipboard))
                    dialog.ShowDialog(this);
                status.Text = "";
            });
        }
        private void AddInstallationConfirmation()
        {
            installedCheck = new CheckBox { Text = "Я установил профиль ProGo в настройках телефона", AutoSize = true,
                Checked = verification.Installed, AccessibleDescription = "Подтверждение пользователя. QR и сохранение файла сами по себе не подтверждают установку." };
            installedCheck.CheckedChanged += delegate {
                verification.SetInstalled(installedCheck.Checked);
                if (internetCheck != null) { internetCheck.SelectedIndex = (int)verification.Internet; internetCheck.Enabled = verification.Installed; }
                RefreshStatus();
            };
            body.Controls.Add(installedCheck);
        }
        private void RecordProfileIssue()
        {
            verification.RecordIssue();
            if (installedCheck != null) installedCheck.Checked = false;
            if (internetCheck != null) { internetCheck.SelectedIndex = 0; internetCheck.Enabled = false; }
            RefreshStatus();
        }
        private void RefreshStatus()
        {
            if (!busy) next.Enabled = !setupBlocked && (step != 3 || verification.Installed);
            if (internetCheck != null && internetCheck.SelectedIndex != (int)verification.Internet) internetCheck.SelectedIndex = (int)verification.Internet;
            if (profileState != null) profileState.Text = verification.IssuanceText;
            if (step != 4 || counters == null) return;
            counters.Text = (service.Relay.IsRunning ? "Канал ПК → VPS запущен. Это ещё не подтверждение подключения телефона." : "Канал остановлен.") + "\nSOCKS: " + service.RecoveryStatus
                + "\n" + verification.Describe(service.Relay.Received, service.Relay.Returned)
                + "\nПакеты: от телефона " + service.Relay.Received + ", на VPS " + service.Relay.Sent + ", обратно " + service.Relay.Returned
                + (service.Relay.LastError == null ? "" : "\n" + service.Relay.LastError);
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (busy) {
                e.Cancel = true;
                if (channelWait != null) { closeAfterWait = true; CancelChannelWait(); }
                else status.Text = "Дождитесь окончания операции. При запросе SSH ответьте в открывшемся окне. Закрытие окна не отменяет текущую операцию и не подтверждает её завершение.";
            }
            base.OnFormClosing(e);
        }
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            ownerUi.Close(); base.OnFormClosed(e);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) { ownerUi.Dispose(); if (channelWait != null) CancelChannelWait(); refresh.Stop(); refresh.Dispose(); }
            base.Dispose(disposing);
        }
    }
    internal enum PhoneInternet { Unknown, Failed, Passed }
    internal sealed class PhoneVerification
    {
        internal bool Issued { get; private set; }
        internal bool Installed { get; private set; }
        internal PhoneInternet Internet { get; private set; }
        internal void SetInternet(PhoneInternet value) { Internet = Installed ? value : PhoneInternet.Unknown; }
        internal string IssuanceText { get { return Issued
            ? "Профиль выдан: файл сохранён или ссылка создана. Получение телефоном не подтверждено."
            : "В этом мастере профиль ещё не выдан. Ранее установленный профиль можно подтвердить вручную."; } }
        internal void Reset() { Issued = Installed = false; Internet = PhoneInternet.Unknown; }
        internal void RecordIssue() { Reset(); Issued = true; }
        internal void SetInstalled(bool value) { if (Installed != value) Internet = PhoneInternet.Unknown; Installed = value; }
        internal string Describe(long received, long returned)
        {
            string packets = received == 0 ? "Пакетов от телефона пока нет: проверьте профиль и проброс портов."
                : returned == 0 ? "Пакеты от телефона получены, но ответа VPS пока нет."
                : "Ответы VPS приходят. Авторизация VPN и интернет этим не подтверждаются.";
            string internet = Internet == PhoneInternet.Failed ? "По вашей проверке: VPN подключён, но доступа в интернет нет."
                : Internet == PhoneInternet.Passed ? "По вашей проверке: сайт открылся и выходной IP совпал с VPS."
                : "Интернет на телефоне ещё не проверен.";
            return IssuanceText + "\nУстановка: " + (Installed ? "подтверждена вами." : "не подтверждена.") + "\n" + packets + "\n" + internet;
        }
    }

}

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
        private bool closeAfterWait, cancelledWait, stoppingChannel, readingShare, cancelledShareWait, savingAddress, cancelledAddressSave, savingAccess, cancelledAccessSave, readingSetup, cancelledSetupRead, savingProfile, cancelledProfileSave;
        internal Action<string> BeforeShareRead; // Fixture boundary around the real protected-file read.
        internal Action<string> BeforeHomeAddressIo; // Fixture boundaries surround actual protected persistence.
        internal Action<string> BeforeAccessIo, BeforeSetupConsumeIo;
        internal Action<string> BeforeProfileIo;
        private Task profileExportWork = Task.FromResult(0);
        internal Task ProfileExportWork { get { return profileExportWork; } }
        private HomeVpnOwner setupOwnerSnapshot;
        private string setupSnapshotError;
        private bool setupSnapshotKnown, setupReadBusy, initializingSetup, setupFieldsEdited, applyingSetupOwner;
        private CancellationTokenSource setupSnapshotCancellation;
        private Task<bool> setupSnapshotWork = Task.FromResult(true), initializationWork = Task.FromResult(true);
        private Task setupSnapshotDriver = Task.FromResult(0);
        internal Task<bool> InitializationWork { get { return initializationWork; } }
        internal Task<bool> SetupSnapshotWork { get { return setupSnapshotWork; } }
        internal Action BeforeSetupSnapshotRead;
        private Task<bool> accessImportWork = Task.FromResult(true);
        private Task setupConsumptionWork = Task.FromResult(0), setupReadWork = Task.FromResult(0);
        internal Task<bool> AccessImportWork { get { return accessImportWork; } }
        internal Task SetupConsumptionWork { get { return setupConsumptionWork; } }
        internal Task SetupReadWork { get { return setupReadWork; } }
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
        internal Func<HomeVpnOwner, string, string, string, Action<string>, Task<string>> Admin;
        internal Func<HomeVpnOwner, Form, HomeProfileUiDispatcher, Action<string>, Task<string>> ReadAdminList;
        internal Func<HomeVpnOwner, Form, HomeProfileUiDispatcher, string, Action<string>, Task<string>> ReadSetupResult;
        internal Func<HomeVpnOwner, Form, HomeProfileUiDispatcher, string, Action<string>, Task<string>> PrepareSetup;
        private Form readOnlyListOwner;
        private HomeProfileUiDispatcher readOnlyListUi;

        internal HomeVpnWizardForm(HomeVpnService service, ClipboardService clipboard, Action beforeSetupSnapshotRead = null)
        {
            this.service = service; this.clipboard = clipboard; BeforeSetupSnapshotRead = beforeSetupSnapshotRead; httpDispatcher = new HomeProfileUiDispatcher(this);
            ReadAdminList = (owner, window, dispatcher, report) => service.ListAdminOwnedAsync(owner, window, dispatcher, report);
            ReadSetupResult = (owner, window, dispatcher, label, report) => service.RecoverSetupOwnedAsync(owner, window, dispatcher, label, report);
            PrepareSetup = (owner, window, dispatcher, label, report) => service.SetupOwnedAsync(owner, window, dispatcher, label, report);
            Admin = (owner, action, label, identifier, report) => action == "repair"
                ? service.RepairAdminOwnedAsync(owner, this, httpDispatcher, report)
                : action == "list" ? ReadAdminList(owner, readOnlyListOwner ?? this, readOnlyListUi ?? httpDispatcher, report)
                : action == "recover-setup" ? ReadSetupResult(owner, this, httpDispatcher, label, report)
                : action == "setup" ? PrepareSetup(owner, this, httpDispatcher, label, report)
                : HomeVpnService.AdminAsync(owner, action, label, identifier, report);
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
            refresh.Interval = 1000; refresh.Tick += delegate { RefreshStatus(); }; refresh.Start();
            ShowStep(service.Access == null ? 0 : 4);
            StartSetupSnapshot(true);
        }

        private Task<bool> StartSetupSnapshot(bool initial)
        {
            if (ownerUi.Closed || IsDisposed || Disposing) return Task.FromResult(false);
            if (!setupSnapshotWork.IsCompleted) return setupSnapshotWork;
            if (!initial && (busy || step != 1 || !own)) return setupSnapshotWork;
            var source = new CancellationTokenSource();
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (channelWaitGate) { setupSnapshotCancellation = source; if (initial) channelWait = source; }
            setupReadBusy = true; initializingSetup = initial; setupSnapshotWork = completion.Task;
            if (initial) {
                initializationWork = completion.Task; busy = true; body.Enabled = false; back.Enabled = next.Enabled = false;
                readingSetup = true; cancelWait.Text = "Отменить чтение настройки"; cancelWait.AccessibleName = cancelWait.Text;
                cancelWait.AccessibleDescription = "Прерывает локальное чтение прежнего запроса. Данные не изменяются; окно ждёт завершения своего чтения.";
                cancelWait.Visible = cancelWait.Enabled = true; CancelButton = cancelWait; cancelWait.DialogResult = DialogResult.None;
                SetProgress("Читаем сохранённую настройку VPS… Можно отменить чтение или закрыть окно.");
            }
            UpdateSetupRecoveryState();
            setupSnapshotDriver = ReadSetupSnapshotOwned(initial, source, completion); // Retained completion precedes worker I/O.
            setupSnapshotDriver.ContinueWith(delegate(Task failed) { completion.TrySetException(failed.Exception.InnerExceptions); }, TaskContinuationOptions.OnlyOnFaulted);
            return completion.Task;
        }
        private async Task ReadSetupSnapshotOwned(bool initial, CancellationTokenSource source, TaskCompletionSource<bool> completion)
        {
            HomeVpnOwner pending = null; Exception failure = null; bool applied = false, firstSnapshot = !setupSnapshotKnown;
            try {
                source.Token.ThrowIfCancellationRequested();
                await ownerUi.Await(service.InitializationWork); source.Token.ThrowIfCancellationRequested();
                var read = HomeVpnSetupRecovery.PendingOwnerAsync(source.Token, BeforeSetupSnapshotRead); setupReadWork = read;
                pending = await ownerUi.Await(read); source.Token.ThrowIfCancellationRequested();
            } catch (Exception ex) { failure = ex; }
            try {
                if (ownerUi.Closed || IsDisposed || Disposing) return;
                if (failure == null) { setupOwnerSnapshot = pending; setupSnapshotError = null; setupSnapshotKnown = true; }
                else if (!(failure is OperationCanceledException)) {
                    setupSnapshotKnown = false; setupSnapshotError = failure is HomeVpnSetupPendingException ? failure.Message
                        : "Не удалось прочитать прежнюю настройку. Данные сохранены; повторите проверку после освобождения файлов.";
                } else if (initial) { setupSnapshotKnown = false; setupSnapshotError = "Чтение прежней настройки отменено. Перед настройкой своего VPS повторите проверку."; }
                if (!initial && firstSnapshot && failure == null && pending != null && !setupFieldsEdited) {
                    // A cancelled initial read has not restored the pending VPS.
                    // First successful retry may fill untouched fields, including
                    // the in-memory draft when navigation already left this page.
                    draftOwner = RestoreSetupOwner(pending, service.Owner);
                    if (step == 1 && own && host != null) {
                        applyingSetupOwner = true;
                        try { host.Text = draftOwner.Host; port.Value = draftOwner.Port; login.Text = draftOwner.Login; key.Text = draftOwner.KeyFile; }
                        finally { applyingSetupOwner = false; }
                    }
                }
                if (initial && !closeAfterWait) {
                    var savedOwner = service.Owner;
                    draftOwner = savedOwner == null ? new HomeVpnOwner { Host = "", Port = 22, Login = "root", KeyFile = "" }
                        : new HomeVpnOwner { Host = savedOwner.Host, Port = savedOwner.Port, Login = savedOwner.Login, KeyFile = savedOwner.KeyFile };
                    draftHome = service.HomeAddress;
                    if (failure == null && pending != null) {
                        draftOwner = RestoreSetupOwner(pending, draftOwner); own = true;
                    } else if (failure is HomeVpnSetupPendingException) own = true;
                    ShowStep(own ? 1 : service.Access == null ? 0 : 4);
                    if (failure != null) { status.ForeColor = failure is OperationCanceledException ? UiTheme.Muted : UiTheme.Error; status.Text = setupSnapshotError; }
                    else status.Text = "";
                }
                applied = true;
            } catch (Exception ex) { failure = ex; }
            finally {
                lock (channelWaitGate) { if (setupSnapshotCancellation == source) setupSnapshotCancellation = null; if (initial && channelWait == source) channelWait = null; }
                setupReadBusy = initializingSetup = false;
                if (initial) { busy = false; readingSetup = false; }
                try {
                    if (!ownerUi.Closed && !IsDisposed && !Disposing) {
                        if (initial) { body.Enabled = back.Enabled = true; cancelWait.Visible = cancelWait.Enabled = false; CancelButton = null; }
                        UpdateSetupRecoveryState(); RefreshStatus();
                        if (initial && closeAfterWait) Close();
                    }
                } finally { source.Dispose(); }
                if (failure != null && !(failure is HomeVpnSetupPendingException) && !(failure is OperationCanceledException)) completion.TrySetException(failure);
                else completion.TrySetResult(applied && failure == null);
            }
        }
        private static HomeVpnOwner RestoreSetupOwner(HomeVpnOwner pending, HomeVpnOwner saved)
        {
            bool sameEndpoint = saved != null && String.Equals(pending.Host, saved.Host, StringComparison.OrdinalIgnoreCase) && pending.Port == saved.Port && pending.Login == saved.Login;
            return new HomeVpnOwner { Host = pending.Host, Port = pending.Port, Login = pending.Login, KeyFile = sameEndpoint ? saved.KeyFile : "" };
        }
        private void SetupFieldsChanged()
        { if (!applyingSetupOwner) setupFieldsEdited = true; UpdateSetupRecoveryState(); }

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
                host.TextChanged += delegate { SetupFieldsChanged(); }; login.TextChanged += delegate { SetupFieldsChanged(); };
                key.TextChanged += delegate { SetupFieldsChanged(); }; port.ValueChanged += delegate { SetupFieldsChanged(); };
                UpdateSetupRecoveryState(); if (!initializingSetup) StartSetupSnapshot(false);
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
                Action("Настроить адрес выдачи QR…", async delegate { await ConfigureShare(); }, "Открывает настройку HTTPS-адреса выдачи профилей на VPS.");
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
                Action("Остановить VPN для телефона", async delegate { await RunStep(StopChannel); }, "Останавливает только VPN телефона и сбрасывает результат проверки интернета. Прокси на ПК не отключается.");
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
                Action("Настроить адрес выдачи QR…", async delegate { await ConfigureShare(); }, "Открывает настройку HTTPS-адреса выдачи профилей на VPS.");
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
                    var value = draftToken; bool ownAccess = own, recoveryRequested = recoverSetup;
                    var owner = ownAccess ? new HomeVpnOwner { Host = draftOwner.Host, Port = draftOwner.Port, Login = draftOwner.Login, KeyFile = draftOwner.KeyFile } : null;
                    var stopScope = service.StopWork;
                    if (service.IsStopping || service.StartsPaused) throw new InvalidOperationException("Дождитесь остановки или обслуживания перед настройкой доступа.");
                    if (ownAccess)
                    {
                        // A recovery action stays read-only even if the pending
                        // file disappears after its button was presented.
                        bool checking = recoveryRequested || await ownerUi.Await(ReadSetupPending(stopScope));
                        var ownerId = new JavaScriptSerializer().Serialize(owner);
                        if (!AccessScopeCurrent(stopScope)) throw new OperationCanceledException();
                        if (checking || preparedToken == null || preparedOwner != ownerId)
                        {
                            preparedToken = await ownerUi.Await(Admin(owner, checking ? "recover-setup" : "setup", "My iPhone", null, SetProgress));
                            preparedOwner = ownerId;
                        }
                        value = preparedToken;
                    }
                    if (!AccessScopeCurrent(stopScope)) throw new OperationCanceledException();
                    if (!await ownerUi.Await(SaveAccess(value, owner, ownAccess, stopScope))) return;
                    if (closeAfterWait || !AccessScopeCurrent(stopScope)) return;
                    await ownerUi.Await(StartChannel()); draftToken = "";
                }
                else if (step == 2)
                {
                    if (!routerDone) throw new InvalidOperationException("Сначала настройте обе записи на роутере и отметьте галочку.");
                    if (!await ownerUi.Await(SaveHomeAddress(draftHome))) return;
                    if (closeAfterWait) return;
                }
                else if (step == 3 && !verification.Installed) throw new InvalidOperationException("Подтвердите установку профиля в настройках телефона. Создание QR или сохранение файла не подтверждает установку.");
                ShowStep(step + 1);
            });
        }
        private async Task RunStep(Func<Task> action)
        {
            if (busy || ownerUi.Closed || IsDisposed || Disposing) return;
            cancelledWait = closeAfterWait = cancelledShareWait = cancelledAddressSave = cancelledAccessSave = cancelledSetupRead = cancelledProfileSave = false;
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
                    status.Text = cancelledWait ? (cancelledAccessSave ? "Импорт доступа отменён до принятия записи. Прежние локальные данные сохранены; можно повторить импорт этого же токена."
                        : cancelledProfileSave ? "Сохранение профиля отменено до начала записи. Файл не создан; можно повторить сохранение."
                        : cancelledSetupRead ? "Проверка сохранённой настройки отменена. Данные не изменены; можно повторить проверку."
                        : cancelledAddressSave ? "Сохранение адреса отменено до начала записи. Прежний адрес сохранён; можно повторить изменение."
                        : cancelledShareWait ? "Чтение адреса отменено. Сохранённые данные остаются; можно повторить чтение."
                        : "Запуск канала отменён. Сохранённый доступ к VPS остаётся; можно повторить запуск.") : "Операция прервана. Результат не подтверждён.";
                }
            }
            catch (Exception ex) { closeAfterWait = false; if (!ownerUi.Closed && !IsDisposed && !Disposing) { status.ForeColor = UiTheme.Error; status.Text = ex.Message; } }
            finally {
                stoppingChannel = false;
                busy = false;
                if (!ownerUi.Closed && !IsDisposed && !Disposing) {
                    body.Enabled = true; back.Enabled = true; next.Enabled = step != 3 || verification.Installed;
                    UpdateSetupRecoveryState();
                    if (closeAfterWait) Close(); else StartSetupSnapshot(false);
                }
            }
        }
        private bool AccessScopeCurrent(Task<bool> stopScope)
        { return !ownerUi.Closed && !service.StartsPaused && !service.IsStopping && Object.ReferenceEquals(service.StopWork, stopScope); }
        private async Task<bool> ReadSetupPending(Task<bool> stopScope)
        {
            using (var source = new CancellationTokenSource()) {
                lock (channelWaitGate) channelWait = source; readingSetup = true;
                cancelWait.Text = "Отменить проверку настройки"; cancelWait.AccessibleName = cancelWait.Text;
                cancelWait.AccessibleDescription = "Прерывает локальное чтение прежнего запроса настройки. Команда VPS не запускается; окно ждёт завершения чтения.";
                cancelWait.Visible = cancelWait.Enabled = true; CancelButton = cancelWait; cancelWait.DialogResult = DialogResult.None;
                UiTheme.ConfigureKeyboardOrder(this); cancelWait.Focus(); SetProgress("Проверяем прежний запрос настройки VPS… Можно отменить чтение.");
                try {
                    var read = HomeVpnSetupRecovery.HasPendingAsync(source.Token); setupReadWork = read;
                    bool pending = await ownerUi.Await(read);
                    if (!AccessScopeCurrent(stopScope)) throw new OperationCanceledException();
                    return pending;
                } catch (OperationCanceledException) { cancelledWait = source.IsCancellationRequested; cancelledSetupRead = true; throw; }
                finally {
                    lock (channelWaitGate) channelWait = null; readingSetup = false;
                    if (!ownerUi.Closed && !IsDisposed && !Disposing) { cancelWait.Visible = cancelWait.Enabled = false; CancelButton = null; }
                }
            }
        }
        private Task<bool> SaveAccess(string value, HomeVpnOwner owner, bool ownAccess, Task<bool> stopScope)
        { accessImportWork = SaveAccessOwned(value, owner, ownAccess, stopScope); return accessImportWork; }
        private async Task<bool> SaveAccessOwned(string value, HomeVpnOwner owner, bool ownAccess, Task<bool> stopScope)
        {
            using (var source = new CancellationTokenSource()) {
                lock (channelWaitGate) channelWait = source; savingAccess = true;
                cancelWait.Text = "Отменить импорт доступа"; cancelWait.AccessibleName = cancelWait.Text;
                cancelWait.AccessibleDescription = "Отменяет ещё не принятое сохранение доступа. Принятая запись может завершиться; отмена не запускает канал и не повторяет настройку VPS.";
                cancelWait.Visible = cancelWait.Enabled = true; CancelButton = cancelWait; cancelWait.DialogResult = DialogResult.None;
                UiTheme.ConfigureKeyboardOrder(this); cancelWait.Focus(); SetProgress("Сохраняем доступ к VPS… Можно отменить до принятия записи; дождитесь фактического результата.");
                bool saved = false;
                try {
                    if (!AccessScopeCurrent(stopScope)) throw new OperationCanceledException();
                    if (!await ownerUi.Await(service.UseTokenAsync(value, owner, source.Token, BeforeAccessIo)))
                        throw new InvalidOperationException("Сохранение доступа не подтверждено. Повторите импорт этого же токена.");
                    saved = true; verification.Reset();
                    if (source.IsCancellationRequested || !AccessScopeCurrent(stopScope)) {
                        SetProgress("Доступ уже сохранён. Отмена не откатила принятую запись; запуск канала отменён. Можно проверить прежнюю настройку или продолжить явно."); return false;
                    }
                    if (ownAccess) {
                        var acceptedAccess = service.Access;
                        SetProgress("Подтверждаем локальное сохранение доступа и прежнего запроса… Можно отменить ожидание.");
                        setupConsumptionWork = HomeVpnSetupRecovery.ConfirmConsumedAsync(owner, value, source.Token,
                            () => AccessScopeCurrent(stopScope) && !service.IsImporting && Object.ReferenceEquals(service.Access, acceptedAccess), BeforeSetupConsumeIo);
                        await ownerUi.Await(setupConsumptionWork);
                    }
                    bool proceed = !source.IsCancellationRequested && AccessScopeCurrent(stopScope);
                    SetProgress(proceed ? "Доступ к VPS сохранён." : "Доступ уже сохранён. Запуск канала отменён; для продолжения нужно явное действие.");
                    return proceed;
                } catch (OperationCanceledException) {
                    cancelledWait = source.IsCancellationRequested; cancelledAccessSave = true;
                    if (saved) { SetProgress("Доступ уже сохранён. Ожидание прервано; запуск канала отменён, прежний запрос можно проверить отдельно."); return false; }
                    throw;
                } catch (HomeVpnImportPartialException) { throw; }
                catch (HomeVpnLocalCleanupException) { throw; }
                catch (IOException) { throw new InvalidOperationException("Не удалось сохранить доступ. Освободите локальные файлы и повторите импорт этого же токена; новая команда VPS не нужна."); }
                catch (UnauthorizedAccessException) { throw new InvalidOperationException("Нет доступа для сохранения данных ProGo. Проверьте доступ к личной папке и повторите импорт этого же токена."); }
                catch (System.Security.Cryptography.CryptographicException) { throw new InvalidOperationException("Windows не подтвердила защиту доступа для текущего пользователя. Прежний запрос настройки сохранён; повторите импорт после восстановления профиля пользователя."); }
                finally {
                    lock (channelWaitGate) channelWait = null; savingAccess = false;
                    if (!ownerUi.Closed && !IsDisposed && !Disposing) { cancelWait.Visible = cancelWait.Enabled = false; CancelButton = null; }
                }
            }
        }

        private async Task StartChannel()
        {
            using (var source = new CancellationTokenSource()) {
                lock (channelWaitGate) channelWait = source;
                cancelWait.Text = "Отменить запуск канала"; cancelWait.AccessibleName = cancelWait.Text;
                cancelWait.AccessibleDescription = "Отменяет только запуск канала к VPS. Сохранённый доступ остаётся; окно ждёт остановки процессов.";
                cancelWait.Visible = cancelWait.Enabled = true; CancelButton = cancelWait;
                cancelWait.DialogResult = DialogResult.None; // CancelButton assignment otherwise supplies an implicit modal close result.
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
        private async Task ReadShareOrigin()
        {
            using (var source = new CancellationTokenSource()) {
                lock (channelWaitGate) channelWait = source;
                readingShare = true;
                cancelWait.Text = "Отменить чтение адреса"; cancelWait.AccessibleName = cancelWait.Text;
                cancelWait.AccessibleDescription = "Отменяет чтение сохранённого HTTPS-адреса. Данные не изменяются; окно ждёт завершения принадлежащего ему чтения.";
                cancelWait.Visible = cancelWait.Enabled = true; CancelButton = cancelWait;
                cancelWait.DialogResult = DialogResult.None; // CancelButton assignment otherwise supplies an implicit modal close result.
                UiTheme.ConfigureKeyboardOrder(this); cancelWait.Focus();
                SetProgress("Читаем сохранённый HTTPS-адрес выдачи QR. Можно отменить; данные не изменяются…");
                try {
                    if (!await ownerUi.Await(service.RefreshShareOriginAsync(source.Token, BeforeShareRead)))
                        throw new InvalidOperationException("Сохранённый HTTPS-адрес недоступен. Освободите файл и повторите настройку; прежние данные не изменены.");
                    SetProgress("Чтение HTTPS-адреса завершено. Можно проверить или настроить адрес.");
                } catch (OperationCanceledException) { cancelledWait = source.IsCancellationRequested; cancelledShareWait = true; throw; }
                finally {
                    lock (channelWaitGate) channelWait = null;
                    readingShare = false;
                    if (!ownerUi.Closed && !IsDisposed && !Disposing) { cancelWait.Visible = cancelWait.Enabled = false; CancelButton = null; }
                }
            }
        }
        private async Task<bool> SaveHomeAddress(string address)
        {
            using (var source = new CancellationTokenSource()) {
                lock (channelWaitGate) channelWait = source;
                savingAddress = true;
                cancelWait.Text = "Отменить сохранение адреса"; cancelWait.AccessibleName = cancelWait.Text;
                cancelWait.AccessibleDescription = "Отменяет ещё не принятое сохранение домашнего адреса. Если запись уже принята, окно ждёт её результата и не обещает откат.";
                cancelWait.Visible = cancelWait.Enabled = true; CancelButton = cancelWait;
                cancelWait.DialogResult = DialogResult.None; // CancelButton assignment otherwise supplies an implicit modal close result.
                UiTheme.ConfigureKeyboardOrder(this); cancelWait.Focus();
                SetProgress("Сохраняем домашний адрес. Можно отменить до принятия записи; дождитесь результата…");
                try {
                    if (!await ownerUi.Await(service.SetHomeAddressAsync(address, source.Token, BeforeHomeAddressIo)))
                        throw new InvalidOperationException("Сохранение домашнего адреса не подтверждено. Повторите проверку перед выдачей профиля.");
                    verification.Reset();
                    bool proceed = !source.IsCancellationRequested;
                    SetProgress(proceed ? "Домашний адрес сохранён." : "Домашний адрес уже сохранён. Отмена не откатила принятую запись; можно изменить адрес или продолжить.");
                    return proceed;
                } catch (OperationCanceledException) { cancelledWait = source.IsCancellationRequested; cancelledAddressSave = true; throw; }
                catch (HomeVpnLocalCleanupException) { throw; }
                catch (IOException) { throw new InvalidOperationException("Не удалось подтвердить сохранение домашнего адреса. Освободите файл и повторите сохранение перед выдачей профиля."); }
                catch (UnauthorizedAccessException) { throw new InvalidOperationException("Нет доступа для сохранения домашнего адреса. Проверьте доступ к личным данным ProGo и повторите сохранение."); }
                catch (System.Security.Cryptography.CryptographicException) { throw new InvalidOperationException("Windows не подтвердила защиту домашнего адреса. Проверьте профиль текущего пользователя и повторите сохранение."); }
                finally {
                    lock (channelWaitGate) channelWait = null;
                    savingAddress = false;
                    if (!ownerUi.Closed && !IsDisposed && !Disposing) { cancelWait.Visible = cancelWait.Enabled = false; CancelButton = null; }
                }
            }
        }
        private async Task ConfigureShare()
        {
            await RunStep(async delegate {
                await ownerUi.Await(ReadShareOrigin());
                HomeProfileShare.Configure(this, service);
            });
        }

        private async Task StopChannel()
        {
            stoppingChannel = true;
            verification.SetInternet(PhoneInternet.Unknown);
            SetProgress("Останавливаем VPN для телефона и удаляем его временные файлы. Дождитесь подтверждения…");
            if (!await ownerUi.Await(service.StopAsync()))
                throw new InvalidOperationException("VPN для телефона остановлен не полностью: процесс или временные файлы ещё принадлежат ProGo. Закройте программы, использующие эти файлы, и повторите остановку.");
            RefreshStatus();
            SetProgress("VPN для телефона остановлен; его процессы и временные файлы очищены. Сохранённый доступ к VPS остаётся.");
        }

        private void CancelChannelWait()
        {
            lock (channelWaitGate) {
                var source = channelWait;
                if (source == null || source.IsCancellationRequested) return;
                if (!ownerUi.Closed && !IsDisposed && !Disposing) cancelWait.Enabled = false;
                SetProgress(savingAccess ? "Отмена импорта запрошена. Уже принятая запись может завершиться; ждём результата без запуска канала."
                    : savingProfile ? "Отмена сохранения профиля запрошена. Уже принятая запись завершается; дождитесь результата."
                    : readingSetup ? "Отменяем чтение прежней настройки. Дождитесь результата; команда VPS не запускается."
                    : savingAddress ? "Отмена сохранения запрошена. Дождитесь результата: уже принятая запись завершается."
                    : readingShare ? "Отменяем чтение адреса. Дождитесь завершения чтения; данные не изменяются…" : "Отменяем запуск канала и закрываем его процессы. Дождитесь результата…");
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
            recoverSetup = setupOwnerSnapshot != null; setupBlocked = !setupSnapshotKnown || setupSnapshotError != null;
            setupState.Text = setupReadBusy ? "Проверяем прежний запрос настройки… Введённые SSH-данные сохраняются."
                : setupSnapshotError != null ? setupSnapshotError
                : recoverSetup ? "Ответ прошлой настройки не подтверждён. ProGo сохранил запрос и проверит его результат. Новый доступ не выдаётся. Используйте исходные SSH-данные; пароль при необходимости вводится в отдельном окне."
                : retained ? "Доступ к VPS уже сохранён. Продолжение запустит канал с этим доступом; повторная настройка сервера не требуется."
                : "Укажите свой Ubuntu VPS. ProGo добавит VPN-сервер, ограниченные SSH-учётные записи и правила выхода VPN в интернет.";
            next.Text = recoverSetup ? "Проверить прошлую настройку" : retained ? "Запустить канал и продолжить" : "Настроить VPS и продолжить";
            next.AccessibleName = next.Text;
            next.AccessibleDescription = recoverSetup ? "Проверяет сохранённый запрос на VPS и получает исходный доступ. Новая настройка и повторная выдача доступа не выполняются."
                : retained ? "Использует уже сохранённый доступ и запускает канал к VPS. Настройка сервера не повторяется."
                : "Настраивает VPN на вашем VPS через SSH и запускает канал. Изменяет настройки сервера.";
            next.Enabled = !busy && !setupReadBusy && !setupBlocked;
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
            await RunStep(async delegate
            {
                await ownerUi.Await(ReadShareOrigin());
                if (String.IsNullOrEmpty(service.ShareOrigin) && !HomeProfileShare.Configure(this, service)) return;
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
        private async void SaveProfile(object sender, EventArgs args)
        {
            if (busy || ownerUi.Closed || IsDisposed || Disposing) return;
            string path;
            using (var dialog = new SaveFileDialog { Filter = "Профиль iPhone|*.mobileconfig", FileName = "ProGo-iPhone.mobileconfig" })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                path = dialog.FileName;
            }
            // The owned helper handles explicit owner loss. Do not resume this
            // async event through an owner await after it has safely settled.
            await ExportProfile(path).ConfigureAwait(false);
        }
        private Task ExportProfile(string path)
        {
            if (busy || ownerUi.Closed || IsDisposed || Disposing) return profileExportWork;
            profileExportWork = RunStep(async delegate {
                using (var source = new CancellationTokenSource()) {
                    lock (channelWaitGate) channelWait = source; savingProfile = true;
                    var access = service.Access; string address = service.HomeAddress; var stop = service.StopWork;
                    cancelWait.Text = "Отменить сохранение профиля"; cancelWait.AccessibleName = cancelWait.Text;
                    cancelWait.AccessibleDescription = "Отменяет запись до её принятия. Принятая запись завершается; окно ждёт её фактического результата.";
                    cancelWait.Visible = cancelWait.Enabled = true; CancelButton = cancelWait; cancelWait.DialogResult = DialogResult.None;
                    UiTheme.ConfigureKeyboardOrder(this); cancelWait.Focus(); SetProgress("Сохраняем профиль iPhone. Можно отменить до начала записи; дождитесь результата…");
                    try {
                        await ownerUi.Await(service.ExportProfileAsync(path, source.Token,
                            () => AccessScopeCurrent(stop) && Object.ReferenceEquals(service.Access, access) && service.HomeAddress == address, BeforeProfileIo));
                        bool current = Object.ReferenceEquals(service.Access, access) && service.HomeAddress == address;
                        if (current) RecordProfileIssue();
                        SetProgress(!current ? "Профиль прежнего доступа уже сохранён. Доступ или домашний адрес изменился; для текущего подключения сохраните новый профиль. Установка на телефоне не подтверждена."
                            : source.IsCancellationRequested || !AccessScopeCurrent(stop) ? "Профиль уже сохранён. Отмена не откатила принятую запись; установка на телефоне не подтверждена."
                            : "Профиль сохранён. Передайте файл на iPhone и установите его; сохранение файла не подтверждает установку.");
                    } catch (OperationCanceledException) { cancelledWait = cancelledProfileSave = true; throw; }
                    catch (IOException) { throw new InvalidOperationException("Не удалось сохранить профиль. Выберите новое имя файла или освободите место и повторите сохранение; существующий профиль не заменяется."); }
                    catch (UnauthorizedAccessException) { throw new InvalidOperationException("Нет доступа к выбранному месту. Выберите доступную папку и новое имя профиля."); }
                    finally {
                        lock (channelWaitGate) { if (channelWait == source) channelWait = null; }
                        savingProfile = false;
                        if (!ownerUi.Closed && !IsDisposed && !Disposing) { cancelWait.Visible = cancelWait.Enabled = false; CancelButton = null; }
                    }
                }
            });
            return profileExportWork;
        }
        private async Task ManageInvitations()
        {
            await RunStep(async delegate
            {
                var stop = service.StopWork;
                var response = await ownerUi.Await(Admin(service.Owner, "list", null, null, SetProgress));
                if (ownerUi.Closed || IsDisposed || Disposing) return;
                if (service.IsStopping || service.StartsPaused || !Object.ReferenceEquals(service.StopWork, stop)) throw new HomeVpnListCancelledException();
                var items = new JavaScriptSerializer().Deserialize<HomeVpnInvitation[]>(response);
                using (var dialog = new HomeInvitationsForm(items,
                    (action, label, id) => Admin(service.Owner, action, label, id, delegate { }), clipboard))
                using (var listUi = new HomeProfileUiDispatcher(dialog)) {
                    readOnlyListOwner = dialog; readOnlyListUi = listUi;
                    try { dialog.ShowDialog(this); }
                    finally { readOnlyListOwner = null; readOnlyListUi = null; }
                }
                if (!ownerUi.Closed && !IsDisposed && !Disposing) status.Text = "";
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
            if (!busy) next.Enabled = !setupReadBusy && !setupBlocked && (step != 3 || verification.Installed);
            if (internetCheck != null && internetCheck.SelectedIndex != (int)verification.Internet) internetCheck.SelectedIndex = (int)verification.Internet;
            if (profileState != null) profileState.Text = verification.IssuanceText;
            if (step != 4 || counters == null) return;
            counters.Text = (service.IsStopping ? "Остановка канала ещё не завершена; временные файлы остаются под управлением ProGo."
                : service.Relay.IsRunning ? "Канал ПК → VPS запущен. Это ещё не подтверждение подключения телефона."
                : service.HasOwnedResources ? "Канал остановлен не полностью. Повторите остановку для очистки временных файлов." : "Канал остановлен.") + "\nSOCKS: " + service.RecoveryStatus
                + "\n" + verification.Describe(service.Relay.Received, service.Relay.Returned)
                + "\nПакеты: от телефона " + service.Relay.Received + ", на VPS " + service.Relay.Sent + ", обратно " + service.Relay.Returned
                + (service.Relay.LastError == null ? "" : "\n" + service.Relay.LastError);
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (busy) {
                e.Cancel = true;
                if (channelWait != null) { closeAfterWait = true; CancelChannelWait(); }
                else if (stoppingChannel) { closeAfterWait = true; SetProgress("Дождитесь подтверждения остановки и очистки VPN для телефона; затем окно закроется."); }
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
            if (disposing) { ownerUi.Dispose();
                lock (channelWaitGate) { var snapshot = setupSnapshotCancellation; if (snapshot != null) try { snapshot.Cancel(); } catch (ObjectDisposedException) { } }
                if (channelWait != null) CancelChannelWait(); refresh.Stop(); refresh.Dispose(); }
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

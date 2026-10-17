using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace ProGo
{
    internal sealed class PhoneProfileLink
    {
        public string Url { get; set; }
        public long Expires { get; set; }
        public string[] Matrix { get; set; }
    }

    internal static class HomeProfileShare
    {
        internal static string Origin(string value)
        {
            Uri uri;
            value = (value ?? "").Trim();
            if (!value.Contains("://")) value = "https://" + value;
            if (!Uri.TryCreate(value, UriKind.Absolute, out uri) || uri.Scheme != "https" || uri.Port != 443
                || uri.HostNameType != UriHostNameType.Dns || uri.Host.IndexOf('.') < 1 || uri.UserInfo != ""
                || uri.AbsolutePath != "/" || uri.Query != "" || uri.Fragment != ""
                || !Regex.IsMatch(uri.Host, @"\A[a-z0-9.-]{3,253}\z")
                || Array.Exists(uri.Host.Split('.'), label => !Regex.IsMatch(label, @"\A[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\z")))
                throw new ArgumentException("Укажите HTTPS-домен выдачи без пути и порта, например vpn.example.org.");
            return "https://" + uri.Host;
        }

        internal static Task VerifyAsync(string origin, HomeVpnAccess access)
        { return VerifyAsync(origin, access, CancellationToken.None, HomeProfileHttp.TimeoutMs, null); }
        internal static async Task VerifyAsync(string origin, HomeVpnAccess access, CancellationToken token, int timeoutMs = HomeProfileHttp.TimeoutMs, Func<Uri, HttpWebRequest> factory = null)
        {
            using (var http = new HomeProfileHttp(HomeProfileHttpPurpose.Verify, token, timeoutMs, factory)) {
                await VerifyAsync(http, origin, access.ServerId).ConfigureAwait(false); http.Check();
            }
        }
        private static async Task VerifyAsync(HomeProfileHttp http, string origin, string serverId)
        {
            string text = await http.RequestAsync(origin, "/health", "GET", null, null).ConfigureAwait(false);
            System.Collections.Generic.Dictionary<string, object> json;
            try { json = new JavaScriptSerializer { MaxJsonLength = 65536, RecursionLimit = 8 }.DeserializeObject(text) as System.Collections.Generic.Dictionary<string, object>; }
            catch (Exception ex) { if (ex is OutOfMemoryException) throw; throw new HomeProfileHttpFailureException("HTTPS-выдача вернула неподтверждённый ответ. Адрес не сохранён."); }
            http.Check();
            if (json == null || !json.ContainsKey("ServerId") || json["ServerId"] as string != serverId)
                throw new HomeProfileHttpFailureException("Этот адрес выдачи относится к другому VPS. Уточните домен у владельца.");
        }

        internal static Task<PhoneProfileLink> CreateAsync(string origin, HomeVpnAccess access, string home)
        { return CreateAsync(origin, access, home, CancellationToken.None, HomeProfileHttp.TimeoutMs, null); }
        internal static async Task<PhoneProfileLink> CreateAsync(string origin, HomeVpnAccess access, string home, CancellationToken token, int timeoutMs = HomeProfileHttp.TimeoutMs, Func<Uri, HttpWebRequest> factory = null)
        {
            origin = Origin(origin);
            if (!HomeVpnAccess.ValidHost(home)) throw new ArgumentException("Сначала укажите внешний домашний адрес в мастере.");
            using (var http = new HomeProfileHttp(HomeProfileHttpPurpose.Create, token, timeoutMs, factory)) {
                await VerifyAsync(http, origin, access.ServerId).ConfigureAwait(false);
                var json = new JavaScriptSerializer { MaxJsonLength = 65536, RecursionLimit = 8 };
                string text = await http.RequestAsync(origin, "/api/share", "POST", access, json.Serialize(new { home = home })).ConfigureAwait(false);
                PhoneProfileLink link;
                try { link = json.Deserialize<PhoneProfileLink>(text); Validate(link, origin); }
                catch (Exception ex) { if (ex is OutOfMemoryException) throw; throw new HomeProfileHttpFailureException("Создание QR не подтверждено: сервис вернул неверный ответ. ProGo не повторяет запрос автоматически."); }
                http.Check(); return link;
            }
        }

        internal static void Validate(PhoneProfileLink link, string origin)
        {
            if (link == null || !Regex.IsMatch(link.Url ?? "", "\\A" + Regex.Escape(Origin(origin)) + @"/#[A-Za-z0-9_-]{43}\z")
                || link.Expires < 1 || link.Expires > 253402300799L || link.Matrix == null
                || link.Matrix.Length < 21 || link.Matrix.Length > 177 || (link.Matrix.Length - 21) % 4 != 0)
                throw new InvalidOperationException("Сервис вернул неверный QR. Повторите выдачу.");
            foreach (var row in link.Matrix)
                if (row == null || row.Length != link.Matrix.Length || !Regex.IsMatch(row, @"\A[01]+\z"))
                    throw new InvalidOperationException("Сервис вернул неверный QR. Повторите выдачу.");
        }

        internal static Task RevokeAsync(string origin, HomeVpnAccess access)
        { return RevokeAsync(origin, access, CancellationToken.None, HomeProfileHttp.TimeoutMs, null); }
        internal static async Task RevokeAsync(string origin, HomeVpnAccess access, CancellationToken token, int timeoutMs = HomeProfileHttp.TimeoutMs, Func<Uri, HttpWebRequest> factory = null)
        {
            using (var http = new HomeProfileHttp(HomeProfileHttpPurpose.Revoke, token, timeoutMs, factory)) {
                await http.RequestAsync(origin, "/api/share", "DELETE", access, null).ConfigureAwait(false); http.Check();
            }
        }

        internal static Bitmap Render(PhoneProfileLink link, int available)
        {
            int size = link.Matrix.Length, scale = Math.Max(1, available / (size + 8));
            var bitmap = new Bitmap((size + 8) * scale, (size + 8) * scale);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.White);
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                        if (link.Matrix[y][x] == '1') graphics.FillRectangle(Brushes.Black, (x + 4) * scale, (y + 4) * scale, scale, scale);
            }
            return bitmap;
        }

        internal static bool Configure(IWin32Window parent, HomeVpnService service)
        {
            using (var dialog = CreateConfigureForm(service)) return dialog.ShowDialog(parent) == DialogResult.OK;
        }

        internal static ProGoForm CreateConfigureForm(HomeVpnService service)
        { return CreateConfigureForm(service, HomeVpnService.AdminAsync, (origin, access, token) => VerifyAsync(origin, access, token), service.SetShareOrigin); }

        // Tests substitute only external transports, retaining the real controls
        // and the normal save boundary after SSH and HTTPS verification.
        internal static ProGoForm CreateConfigureForm(HomeVpnService service,
            Func<HomeVpnOwner, string, string, string, Action<string>, Task<string>> admin,
            Func<string, HomeVpnAccess, Task> verifyOrigin)
        { return CreateConfigureForm(service, admin, verifyOrigin, service.SetShareOrigin); }

        internal static ProGoForm CreateConfigureForm(HomeVpnService service,
            Func<HomeVpnOwner, string, string, string, Action<string>, Task<string>> admin,
            Func<string, HomeVpnAccess, Task> verifyOrigin, Action<string> saveOrigin)
        { return CreateConfigureForm(service, admin, (origin, access, token) => verifyOrigin(origin, access), saveOrigin); }

        internal static ProGoForm CreateConfigureForm(HomeVpnService service,
            Func<HomeVpnOwner, string, string, string, Action<string>, Task<string>> admin,
            Func<string, HomeVpnAccess, CancellationToken, Task> verifyOrigin, Action<string> saveOrigin)
        {
            var dialog = new ProGoForm { Text = "QR: адрес выдачи профиля", ClientSize = new Size(660, 480), StartPosition = FormStartPosition.CenterParent, AutoScaleMode = AutoScaleMode.Dpi };
            {
                var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };
                panel.Controls.Add(new Label { Text = "Один раз настройте защищённую выдачу", AutoSize = true, Font = UiTheme.Heading, MaximumSize = new Size(605, 0), Margin = new Padding(0, 0, 0, 16) });
                panel.Controls.Add(new Label { AutoSize = true, MaximumSize = new Size(600, 0), Text = service.Owner == null
                    ? "Попросите владельца VPS прислать HTTPS-адрес выдачи профилей. Для новых токенов он заполняется автоматически."
                    : "Создайте отдельный поддомен, например vpn.example.org. Его DNS-записи A/AAAA должны указывать на ваш VPS. Разрешите TCP 80 и 443 в панели хостинга. ProGo установит сервис выдачи и настроит сертификат через Caddy. Существующие сайты сохраняются; при конфликте настройка остановится.", Margin = new Padding(0, 0, 0, 16) });
                panel.Controls.Add(new Label { Text = "HTTPS-адрес выдачи профиля", AutoSize = true });
                var address = new TextBox { Width = 600, Text = service.ShareOrigin ?? "", AccessibleName = "HTTPS-адрес выдачи профиля",
                    AccessibleDescription = "Домен без пути и порта, например vpn.example.org. Адрес сохраняется только после успешной проверки принадлежности VPS." }; panel.Controls.Add(address);
                var install = new Button { Text = "Настроить HTTPS на VPS", AutoSize = true, MinimumSize = new Size(260, 38), Visible = service.Owner != null };
                var verify = new Button { Text = "Адрес уже настроен — проверить", AutoSize = true, MinimumSize = new Size(300, 38) };
                var recover = new Button { Text = "Проверить прежнюю настройку HTTPS", AutoSize = true, MinimumSize = new Size(300, 38), Visible = false,
                    AccessibleDescription = "Проверяет статус и адрес сохранённой команды на исходном VPS. Введённый новый домен не используется. Не запускает настройку повторно. Запрос снимается только после подтверждения и сохранения адреса." };
                var status = new Label { AutoSize = true, MaximumSize = new Size(600, 0) };
                install.AccessibleDescription = "Устанавливает HTTPS-выдачу профилей на VPS через SSH, затем проверяет её и сохраняет адрес. Изменяет настройки сервера.";
                verify.AccessibleDescription = "Проверяет HTTPS и принадлежность вашему VPS. После успешной проверки сохраняет адрес; не запускает настройку сервера.";
                DescribeStatus(status, "Результат настройки HTTPS-выдачи");
                var close = new Button { Text = "Закрыть", AutoSize = true, MinimumSize = new Size(230, 38), DialogResult = DialogResult.Cancel,
                    AccessibleDescription = "Закрывает окно без сохранения введённого адреса. Во время HTTPS-запроса прерывает локальное ожидание и ждёт его завершения. Если запрос настройки VPS сохранён, он остаётся." };
                close.Click += delegate { dialog.Close(); };
                dialog.CancelButton = close;
                panel.Controls.Add(install); panel.Controls.Add(verify); panel.Controls.Add(recover); panel.Controls.Add(close); panel.Controls.Add(status); dialog.Controls.Add(panel);
                var dispatcher = new HomeProfileUiDispatcher(dialog);
                bool working = false, closeAfterHttp = false, httpStage = false, trackedHttp = false;
                CancellationTokenSource activeHttp = null;
                Action<bool> refreshGate = delegate(bool showMessage) {
                    bool pending = true;
                    try { pending = HomeVpnShareRecovery.HasPending(); if (pending && showMessage) status.Text = HomeVpnShareRecovery.PendingMessage; }
                    catch (HomeVpnSharePendingException ex) { status.Text = ex.Message; }
                    install.Enabled = !working && !pending; verify.Enabled = !working && !pending;
                    recover.Visible = pending && service.Owner != null; recover.Enabled = !working;
                    address.Enabled = !working; close.Enabled = !working || httpStage;
                };
                Action cancelHttp = delegate {
                    if (!working || activeHttp == null) return;
                    closeAfterHttp = true;
                    if (!activeHttp.IsCancellationRequested) activeHttp.Cancel();
                    close.Enabled = false;
                    status.Text = httpStage ? trackedHttp ? "Прерываем локальную проверку HTTPS. Дождитесь завершения; адрес не сохраняется, запрос настройки VPS остаётся."
                        : "Прерываем локальную проверку HTTPS. Дождитесь завершения; прежний адрес и доступ сохраняются."
                        : "Дождитесь завершения локального ожидания SSH. Проверка HTTPS и сохранение адреса отменены. Если запрос настройки создан, он остаётся для проверки.";
                };
                Func<int, Task> run = async delegate(int mode)
                {
                    if (working) return;
                    working = true; closeAfterHttp = httpStage = false; trackedHttp = mode != 0;
                    install.Enabled = verify.Enabled = recover.Enabled = address.Enabled = close.Enabled = false;
                    var source = new CancellationTokenSource(); activeHttp = source;
                    Exception failure = null;
                    try
                    {
                        var access = service.Access;
                        var currentOwner = service.Owner;
                        var owner = currentOwner == null ? null : new HomeVpnOwner { Host = currentOwner.Host, Port = currentOwner.Port, Login = currentOwner.Login, KeyFile = currentOwner.KeyFile };
                        string origin;
                        Action<string> progress = text => dispatcher.Post(delegate { status.Text = text; });
                        if (mode == 2) origin = await admin(owner, "recover-share", null, null, progress).ConfigureAwait(false);
                        else {
                            if (HomeVpnShareRecovery.HasPending()) throw new HomeVpnSharePendingException(HomeVpnShareRecovery.PendingMessage);
                            origin = Origin(address.Text);
                            if (mode == 1) origin = await admin(owner, "share", null, origin, progress).ConfigureAwait(false);
                        }
                        source.Token.ThrowIfCancellationRequested();
                        bool shown = await dispatcher.DispatchAsync(delegate {
                            source.Token.ThrowIfCancellationRequested(); httpStage = true; close.Enabled = true;
                            status.Text = "Проверяем HTTPS и принадлежность VPS: " + origin + "… До 20 секунд; можно закрыть окно и отменить проверку.";
                        }).ConfigureAwait(false);
                        if (!shown) throw new HomeProfileHttpCancelledException(mode == 0 ? "Проверка HTTPS отменена. Прежний адрес и доступ сохранены."
                            : "Проверка HTTPS отменена. Прежний адрес и запрос настройки сохранены.");
                        if (mode != 0) await HomeVpnShareRecovery.ConfirmAsync(owner, access, origin,
                            () => verifyOrigin(origin, access, source.Token), saveOrigin, () => !dialog.IsDisposed && !dialog.Disposing && Object.ReferenceEquals(service.Access, access)
                                && service.Owner != null && service.Owner.Host == owner.Host && service.Owner.Port == owner.Port && service.Owner.Login == owner.Login,
                            source.Token, action => dispatcher.DispatchAsync(delegate {
                                action(); activeHttp = null; working = httpStage = false; dialog.DialogResult = DialogResult.OK;
                            })).ConfigureAwait(false);
                        else {
                            await verifyOrigin(origin, access, source.Token).ConfigureAwait(false);
                            bool accepted = await dispatcher.DispatchAsync(delegate {
                                source.Token.ThrowIfCancellationRequested();
                                if (!Object.ReferenceEquals(service.Access, access)) throw new HomeVpnSharePendingException("Доступ к VPS изменился во время проверки. Адрес не сохранён; проверьте его для текущего VPS.");
                                if (HomeVpnShareRecovery.HasPending()) throw new HomeVpnSharePendingException(HomeVpnShareRecovery.PendingMessage);
                                try { saveOrigin(origin); }
                                catch (Exception ex) { if (ex is OutOfMemoryException) throw; throw new HomeProfileHttpFailureException("HTTPS-адрес подтверждён, но сохранить его не удалось. Проверьте локальные настройки перед повтором."); }
                                activeHttp = null; working = httpStage = false; dialog.DialogResult = DialogResult.OK;
                            }).ConfigureAwait(false);
                            if (!accepted) throw new HomeProfileHttpCancelledException(mode == 0 ? "Проверка HTTPS отменена. Прежний адрес и доступ сохранены."
                            : "Проверка HTTPS отменена. Прежний адрес и запрос настройки сохранены.");
                        }
                    }
                    catch (Exception ex) { failure = ex; }
                    try { await dispatcher.DispatchAsync(delegate {
                        activeHttp = null; working = httpStage = false;
                        if (failure == null) return;
                        if (closeAfterHttp) dialog.Close();
                        else {
                            status.Text = ConfigureFailure(failure, mode != 0);
                            refreshGate(false);
                        }
                    }).ConfigureAwait(false); }
                    finally { source.Dispose(); }
                };
                install.Click += async delegate { await run(1).ConfigureAwait(false); };
                verify.Click += async delegate { await run(0).ConfigureAwait(false); };
                recover.Click += async delegate { await run(2).ConfigureAwait(false); };
                dialog.FormClosing += delegate(object s, FormClosingEventArgs e) {
                    if (working && dialog.DialogResult != DialogResult.OK) { e.Cancel = true; dialog.DialogResult = DialogResult.None; cancelHttp(); }
                };
                dialog.Disposed += delegate { var source = activeHttp; if (source != null && !source.IsCancellationRequested) source.Cancel(); };
                refreshGate(true);
                UiTheme.ConfigureKeyboardOrder(dialog);
                return dialog;
            }
        }

        private static string ConfigureFailure(Exception failure, bool tracked)
        {
            if (failure is HomeProfileHttpFailureException || failure is HomeProfileHttpCancelledException
                || failure is HomeVpnSharePendingException || failure is HomeVpnAdminPendingException
                || failure is HomeVpnSetupPendingException || failure is HomeVpnOwnerUnconfirmedException
                || failure is HomeVpnPreparationCancelledException) return failure.Message;
            if (failure is OperationCanceledException) return tracked ? "Проверка HTTPS отменена. Прежний адрес и запрос настройки сохранены."
                : "Проверка HTTPS отменена. Прежний адрес и доступ сохранены.";
            if (failure is ArgumentException) return "Укажите HTTPS-домен выдачи без пути и порта, например vpn.example.org.";
            return tracked ? "Не удалось подтвердить настройку HTTPS. Проверьте соединение и сохранённый запрос; ProGo не повторяет команду автоматически."
                : "Не удалось подтвердить проверку HTTPS. Проверьте соединение и домен; ProGo не повторяет запрос автоматически.";
        }

        internal static void DescribeStatus(Label label, string name)
        {
            label.AccessibleName = name; label.AccessibleDescription = label.Text;
            label.TextChanged += delegate { label.AccessibleDescription = label.Text; };
        }
    }

    internal sealed class PhoneProfileQrForm : ProGoForm
    {
        private readonly System.Windows.Forms.Timer clock = new System.Windows.Forms.Timer();
        private readonly Bitmap bitmap;
        private readonly Control revokeDispatcher = new Control();
        private readonly object revokeCompletionGate = new object();
        private readonly System.Collections.Generic.HashSet<TaskCompletionSource<bool>> revokeCompletions = new System.Collections.Generic.HashSet<TaskCompletionSource<bool>>();
        private volatile bool closed;
        internal Task RevokeWork { get; private set; }
        internal PhoneProfileQrForm(PhoneProfileLink link, Func<Task> revoke, ClipboardService clipboard)
        {
            Text = "VPN на телефоне — сканируйте QR"; ClientSize = new Size(570, 700);
            AutoScaleMode = AutoScaleMode.Dpi; StartPosition = FormStartPosition.CenterParent;
            var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };
            panel.Controls.Add(new Label { AutoSize = true, MaximumSize = new Size(510, 0), Text = "Откройте камеру телефона и наведите на код. На странице выберите iPhone или Android.", Margin = new Padding(0, 0, 0, 12) });
            bitmap = HomeProfileShare.Render(link, 400);
            var picture = new PictureBox { Image = bitmap, Size = bitmap.Size, SizeMode = PictureBoxSizeMode.Normal, BackColor = Color.White,
                TabStop = false, AccessibleRole = AccessibleRole.Graphic, AccessibleName = "QR-код выдачи профиля VPN",
                AccessibleDescription = "Личная временная ссылка. Наведите камеру телефона или используйте кнопку копирования ссылки. Установка профиля подтверждается отдельно." };
            panel.Controls.Add(picture);
            var status = new Label { AutoSize = true, MaximumSize = new Size(510, 0) }; panel.Controls.Add(status);
            var copy = new Button { Text = "Скопировать ссылку", AutoSize = true, MinimumSize = new Size(230, 38) };
            copy.AccessibleDescription = "Копирует личную временную ссылку. " + clipboard.CopyNotice;
            var copyNotice = new Label { AutoSize = true, MaximumSize = new Size(510, 0), Text = clipboard.CopyNotice };
            HomeProfileShare.DescribeStatus(status, "Срок действия ссылки на профиль");
            HomeProfileShare.DescribeStatus(copyNotice, "Результат копирования ссылки");
            clipboard.BindSecretCopy(copy, delegate { return link.Url; }, copyNotice);
            var cancel = new Button { Text = "Отозвать ссылку", AutoSize = true, MinimumSize = new Size(230, 38) };
            cancel.AccessibleDescription = "Отзывает временную ссылку на сервере. Уже установленный VPN продолжает работать.";
            var error = new Label { AutoSize = true, MaximumSize = new Size(510, 0), Visible = false };
            HomeProfileShare.DescribeStatus(error, "Результат отзыва ссылки");
            var close = new Button { Text = "Закрыть", AutoSize = true, MinimumSize = new Size(230, 38), DialogResult = DialogResult.Cancel,
                AccessibleDescription = "Закрывает окно без отзыва ссылки. Ссылка остаётся доступной до использования, отзыва или истечения срока." };
            close.Click += delegate { Close(); };
            CancelButton = close;
            cancel.Click += delegate
            {
                if (closed || !cancel.Enabled) return;
                cancel.Enabled = false; error.Text = ""; error.Visible = false;
                RevokeWork = RunRevoke(revoke, delegate {
                    clock.Stop(); status.Text = "Ссылка отозвана. Уже установленный VPN продолжает работать."; picture.Visible = false; copy.Enabled = false;
                }, delegate(Exception failure) {
                    error.Text = "Отозвать не удалось подтвердить. " + (failure is HomeProfileHttpFailureException || failure is HomeProfileHttpCancelledException ? failure.Message
                        : "VPS мог выполнить отзыв. ProGo не повторяет запрос автоматически; установленный VPN не изменяется.");
                    error.Visible = true; cancel.Enabled = true;
                });
            };
            panel.Controls.Add(copy); panel.Controls.Add(copyNotice); panel.Controls.Add(cancel);
            panel.Controls.Add(new Label { AutoSize = true, MaximumSize = new Size(510, 0), Text = "Ссылка для одного телефона: после «Получить профиль» повторно воспользоваться QR нельзя. Для другого телефона создайте новый QR. Не публикуйте код. Android использует strongSwan VPN Client.", Margin = new Padding(0, 8, 0, 8) });
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = Padding.Empty };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var footer = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = false, FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(18, 0, 18, 12), Margin = Padding.Empty };
            footer.Controls.Add(error); footer.Controls.Add(close); layout.Controls.Add(panel, 0, 0); layout.Controls.Add(footer, 0, 1); Controls.Add(layout);
            UiTheme.ConfigureKeyboardOrder(this);
            // Modal windows can remove SynchronizationContext. Keep a handle owned by
            // this constructor's UI thread for completion, independently of that context.
            revokeDispatcher.CreateControl();
            FormClosed += delegate { CloseRevoke(); clock.Stop(); };
            clock.Interval = 1000;
            EventHandler update = delegate
            {
                if (closed) return;
                var left = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(link.Expires) - DateTime.UtcNow;
                status.Text = left.TotalSeconds > 0 ? "Осталось " + ((int)left.TotalMinutes) + ":" + left.Seconds.ToString("00") : "Время истекло. Закройте окно и создайте новый QR.";
                if (left.TotalSeconds <= 0) { clock.Stop(); picture.Visible = false; copy.Enabled = false; }
            };
            clock.Tick += update; update(this, EventArgs.Empty); clock.Start();
        }
        private async Task RunRevoke(Func<Task> revoke, Action succeeded, Action<Exception> failed)
        {
            Exception failure = null;
            try { await revoke().ConfigureAwait(false); }
            catch (Exception ex) { failure = ex; }
            await DispatchRevokeCompletion(failure == null ? succeeded : new Action(delegate { failed(failure); })).ConfigureAwait(false);
        }
        private Task<bool> DispatchRevokeCompletion(Action action)
        {
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (revokeCompletionGate) {
                if (closed) return Task.FromResult(false);
                revokeCompletions.Add(completion);
            }
            try {
                revokeDispatcher.BeginInvoke(new Action(delegate {
                    try {
                        if (closed || IsDisposed || Disposing) { completion.TrySetResult(false); return; }
                        action(); completion.TrySetResult(true);
                    } catch (Exception ex) { completion.TrySetException(ex); }
                    finally { lock (revokeCompletionGate) revokeCompletions.Remove(completion); }
                }));
            } catch (InvalidOperationException) {
                lock (revokeCompletionGate) revokeCompletions.Remove(completion);
                completion.TrySetResult(false);
            }
            return completion.Task;
        }
        private void CloseRevoke()
        {
            closed = true;
            lock (revokeCompletionGate) {
                // Closing discards native callbacks. Their tasks must settle without a
                // message loop; the already dispatched server request still completes.
                foreach (var completion in revokeCompletions) completion.TrySetResult(false);
                revokeCompletions.Clear();
            }
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) { CloseRevoke(); revokeDispatcher.Dispose(); clock.Dispose(); if (bitmap != null) bitmap.Dispose(); }
            base.Dispose(disposing);
        }
    }
}

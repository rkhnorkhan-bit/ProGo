using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
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
                || !Regex.IsMatch(uri.Host, @"\A[a-z0-9.-]{3,253}\z"))
                throw new ArgumentException("Укажите HTTPS-домен выдачи без пути и порта, например vpn.example.org.");
            return "https://" + uri.Host;
        }

        private static string Request(string origin, string path, string method, HomeVpnAccess access, string body)
        {
            var request = (HttpWebRequest)WebRequest.Create(Origin(origin) + path);
            request.Method = method; request.AllowAutoRedirect = false;
            request.Proxy = null; request.Timeout = request.ReadWriteTimeout = 20000;
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            if (access != null)
                request.Headers[HttpRequestHeader.Authorization] = "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(access.User + ":" + access.Password));
            try
            {
                if (method == "POST")
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(body);
                    request.ContentType = "application/json"; request.ContentLength = bytes.Length;
                    using (var stream = request.GetRequestStream()) stream.Write(bytes, 0, bytes.Length);
                }
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    if ((int)response.StatusCode < 200 || (int)response.StatusCode >= 300) throw new IOException();
                    using (var stream = response.GetResponseStream())
                    using (var output = new MemoryStream())
                    {
                        var buffer = new byte[4096]; int count;
                        while ((count = stream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            if (output.Length + count > 65536) throw new IOException();
                            output.Write(buffer, 0, count);
                        }
                        return Encoding.UTF8.GetString(output.ToArray());
                    }
                }
            }
            catch (WebException ex)
            {
                var response = ex.Response as HttpWebResponse;
                var code = response == null ? 0 : (int)response.StatusCode;
                if (response != null) response.Dispose();
                throw new InvalidOperationException(code == 401 ? "Доступ к выдаче профиля отозван. Попросите владельца проверить приглашение."
                    : "HTTPS-выдача недоступна. Проверьте домен, сертификат и порты 80/443 на VPS. После первой настройки сертификат может выпускаться несколько минут.");
            }
        }

        internal static Task VerifyAsync(string origin, HomeVpnAccess access)
        {
            return Task.Run(delegate
            {
                var json = new JavaScriptSerializer().DeserializeObject(Request(origin, "/health", "GET", null, null)) as System.Collections.Generic.Dictionary<string, object>;
                if (json == null || !json.ContainsKey("ServerId") || json["ServerId"] as string != access.ServerId)
                    throw new InvalidOperationException("Этот адрес выдачи относится к другому VPS. Уточните домен у владельца.");
            });
        }

        internal static async Task<PhoneProfileLink> CreateAsync(string origin, HomeVpnAccess access, string home)
        {
            origin = Origin(origin);
            if (!HomeVpnAccess.ValidHost(home)) throw new ArgumentException("Сначала укажите внешний домашний адрес в мастере.");
            await VerifyAsync(origin, access);
            return await Task.Run(delegate
            {
                var json = new JavaScriptSerializer { MaxJsonLength = 65536, RecursionLimit = 8 };
                var link = json.Deserialize<PhoneProfileLink>(Request(origin, "/api/share", "POST", access, json.Serialize(new { home = home })));
                Validate(link, origin);
                return link;
            });
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
        {
            return Task.Run(delegate { Request(origin, "/api/share", "DELETE", access, null); });
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
                var status = new Label { AutoSize = true, MaximumSize = new Size(600, 0) };
                install.AccessibleDescription = "Устанавливает HTTPS-выдачу профилей на VPS через SSH, затем проверяет её и сохраняет адрес. Изменяет настройки сервера.";
                verify.AccessibleDescription = "Проверяет HTTPS и принадлежность вашему VPS. После успешной проверки сохраняет адрес; не запускает настройку сервера.";
                DescribeStatus(status, "Результат настройки HTTPS-выдачи");
                var close = new Button { Text = "Закрыть", AutoSize = true, MinimumSize = new Size(230, 38), DialogResult = DialogResult.Cancel,
                    AccessibleDescription = "Закрывает окно без сохранения введённого адреса. Пока настройка выполняется, закрытие недоступно." };
                close.Click += delegate { dialog.Close(); };
                dialog.CancelButton = close;
                panel.Controls.Add(install); panel.Controls.Add(verify); panel.Controls.Add(close); panel.Controls.Add(status); dialog.Controls.Add(panel);
                bool working = false;
                Func<bool, Task> run = async delegate(bool setup)
                {
                    working = true; install.Enabled = verify.Enabled = address.Enabled = close.Enabled = false;
                    try
                    {
                        string origin = Origin(address.Text);
                        if (setup) await HomeVpnService.AdminAsync(service.Owner, "share", null, origin, delegate(string text) { status.Text = text; });
                        status.Text = "Проверяем HTTPS и принадлежность VPS…";
                        await VerifyAsync(origin, service.Access);
                        service.SetShareOrigin(origin); dialog.DialogResult = DialogResult.OK;
                    }
                    catch (Exception ex) { status.Text = ex.Message; }
                    finally { working = false; install.Enabled = verify.Enabled = address.Enabled = close.Enabled = true; }
                };
                install.Click += async delegate { await run(true); };
                verify.Click += async delegate { await run(false); };
                dialog.FormClosing += delegate(object s, FormClosingEventArgs e) { if (working && dialog.DialogResult != DialogResult.OK) e.Cancel = true; };
                UiTheme.ConfigureKeyboardOrder(dialog);
                return dialog;
            }
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
            var error = new Label { AutoSize = true, MaximumSize = new Size(510, 0) };
            HomeProfileShare.DescribeStatus(error, "Результат отзыва ссылки");
            var close = new Button { Text = "Закрыть", AutoSize = true, MinimumSize = new Size(230, 38), DialogResult = DialogResult.Cancel,
                AccessibleDescription = "Закрывает окно без отзыва ссылки. Ссылка остаётся доступной до использования, отзыва или истечения срока." };
            close.Click += delegate { Close(); };
            CancelButton = close;
            cancel.Click += async delegate
            {
                cancel.Enabled = false;
                try { await revoke(); clock.Stop(); status.Text = "Ссылка отозвана. Уже установленный VPN продолжает работать."; picture.Visible = false; copy.Enabled = false; }
                catch (Exception) { error.Text = "Отозвать не удалось: проверьте соединение. Ссылка автоматически истечёт через 15 минут после создания."; cancel.Enabled = true; }
            };
            panel.Controls.Add(copy); panel.Controls.Add(copyNotice); panel.Controls.Add(cancel);
            panel.Controls.Add(new Label { AutoSize = true, MaximumSize = new Size(510, 0), Text = "Ссылка для одного телефона: после «Получить профиль» повторно воспользоваться QR нельзя. Для другого телефона создайте новый QR. Не публикуйте код. Android использует strongSwan VPN Client.", Margin = new Padding(0, 8, 0, 8) });
            panel.Controls.Add(error);
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = Padding.Empty };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var footer = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = false,
                Padding = new Padding(18, 0, 18, 12), Margin = Padding.Empty };
            footer.Controls.Add(close); layout.Controls.Add(panel, 0, 0); layout.Controls.Add(footer, 0, 1); Controls.Add(layout);
            UiTheme.ConfigureKeyboardOrder(this);
            clock.Interval = 1000;
            EventHandler update = delegate
            {
                var left = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(link.Expires) - DateTime.UtcNow;
                status.Text = left.TotalSeconds > 0 ? "Осталось " + ((int)left.TotalMinutes) + ":" + left.Seconds.ToString("00") : "Время истекло. Закройте окно и создайте новый QR.";
                if (left.TotalSeconds <= 0) { clock.Stop(); picture.Visible = false; copy.Enabled = false; }
            };
            clock.Tick += update; update(this, EventArgs.Empty); clock.Start();
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) { clock.Dispose(); if (bitmap != null) bitmap.Dispose(); }
            base.Dispose(disposing);
        }
    }
}

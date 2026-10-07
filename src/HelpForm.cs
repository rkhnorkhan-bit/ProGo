using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace ProGo
{
    internal sealed class HelpForm : ProGoForm
    {
        internal HelpForm(Action<AppCommand> action)
        {
            Text = "Помощь · ProGo"; ClientSize = new Size(820, 620); MinimumSize = new Size(700, 540);
            var tabs = new TabControl { Dock = DockStyle.Fill, Multiline = true, AccessibleName = "Темы помощи ProGo" };
            tabs.AccessibleDescription = "Темы справки. Стрелки влево и вправо переключают тему. Tab переводит фокус в инструкцию и к её действиям.";
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));
            root.Controls.Add(tabs, 0, 0);
            var footer = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16, 8, 16, 8), ColumnCount = 2, RowCount = 1 };
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            var guidance = UiTheme.Label("Темы: ← →. Чтение: Tab, ↑ ↓, Page Up / Page Down, Home / End.", UiTheme.Body, UiTheme.Muted);
            guidance.AutoSize = false; guidance.Dock = DockStyle.Fill; guidance.TextAlign = ContentAlignment.MiddleLeft;
            guidance.AccessibleName = "Управление помощью с клавиатуры"; guidance.AccessibleDescription = guidance.Text;
            var close = UiTheme.Button("Закрыть", delegate { Close(); }, false);
            close.AccessibleDescription = "Закрыть справку без изменения настроек и без остановки подключений.";
            close.Anchor = AnchorStyles.Right; footer.Controls.Add(guidance, 0, 0); footer.Controls.Add(close, 1, 0);
            root.Controls.Add(footer, 0, 1); Controls.Add(root); CancelButton = close;
            var start = Topic(tabs, "Начало", "Подключение и проверка");
            Paragraph(start, "Версия программы: " + typeof(HelpForm).Assembly.GetName().Version.ToString(3) + ". Главная показывает отдельные состояния подключения ПК, прокси приложений и VPN для телефона.");
            Paragraph(start, "1. Настройки → Подключение → Добавить → По адресу сервера: укажите адрес, SSH-порт, пользователя и путь к ключу. Режим «Из SSH config (для опытных)» использует уже настроенное подключение OpenSSH.");
            Paragraph(start, "2. «Первый вход» открывает обычное окно SSH. Сверьте отпечаток ключа сервера. Парольный вход сам по себе не разрешает фоновое подключение: для него нужен доступный SSH-ключ или агент.");
            Paragraph(start, "3. Нажмите «Запустить CLI» для Codex и терминалов либо включите Windows. Проверка SOCKS подтверждает готовность прокси; «Проверить маршрут» отдельно проверяет интернет. Это не VPN для всего трафика ПК.");
            Paragraph(start, "«Диагностика» проверяет настройки, маршрут и скорость. При ошибке исправьте указанную причину и повторите проверку. «Передать диагностику…» открывает отчёт из известных событий без исходных параметров. Проверьте его перед копированием или сохранением. Личный журнал содержит больше подробностей и может включать адреса и пути: не публикуйте его целиком.");
            AddAction(start, AppCommandUi.Button(AppCommand.ExportDiagnostics, delegate { action(AppCommand.ExportDiagnostics); }, true), "Открыть предпросмотр отчёта. Копирование и сохранение требуют отдельного действия; отчёт не отправляется автоматически.");
            AddAction(start, AppCommandUi.Button(AppCommand.OpenAppLog, delegate { action(AppCommand.OpenAppLog); }), "Открыть личный журнал приложения. Он может содержать адреса и пути; это не подготовленный отчёт для передачи.");

            Paragraph(start, "Настройки → Подключение: «Запускать ProGo при входе в Windows» запускает ProGo после входа пользователя в Windows. «Подключаться к серверу при запуске ProGo» — отдельная настройка соединения. Обе применяются после сохранения. Если Windows запретила автозапуск, откройте «Автозагрузка в Windows…» и проверьте разрешение для ProGo.");

            var cli = Topic(tabs, "Codex и терминалы", "Обычный запуск без обязательного ярлыка");
            Paragraph(cli, "«Запустить CLI» включает общий локальный прокси и переменные прокси текущего пользователя. После этого запускайте codex обычным способом. Сам Codex CLI устанавливается отдельно.");
            Paragraph(cli, "Полностью закройте и снова откройте уже работающие терминалы, IDE и Codex, чтобы они получили новое окружение. Новая вкладка старого терминала может сохранить прежние значения. «Открыть Codex CLI с прокси» задаёт окружение отдельному запуску.");
            Paragraph(cli, "Автоматика «Включать прокси для терминалов и Codex» общая. Ручное выключение останавливает её повторы на этот сеанс; Windows и телефон управляются отдельно. Дополнительный ярлык Codex необязателен и не заменяет общий режим.");
            Paragraph(cli, "Поля и галочки настроек применяются после «Сохранить». Кнопки ручного управления действуют сразу по сохранённым настройкам; «Отменить изменения» их не отменяет.");

            var ports = Topic(tabs, "Порты", "Если порт приложений занят");
            Paragraph(ports, "Настройки → Порт приложений: «Выбирать свободный порт автоматически» пробует сохранённый порт и выбирает свободный при конфликте. Для постоянного порта снимите галочку. Точный адрес смотрите в приложении.");
            Paragraph(ports, "«Подобрать свободный» планирует новый порт только после «Сохранить»; «Отменить подбор» отменяет этот выбор. Режим автоматического или постоянного порта сохраняется. Ошибка сохранения не заменяет работающее подключение.");
            Paragraph(ports, "После смены порта перезапустите старые терминалы и Codex. Порт локального HTTP-прокси, SSH-порт сервера, SOCKS-порт и порты VPN на роутере — разные настройки.");

            var phone = Topic(tabs, "Телефон", "VPN для iPhone и Android");
            Paragraph(phone, "Откройте «VPN для телефона». Выберите «Подключиться к готовому VPS» с личным токеном владельца или «Добавить свой VPS» с SSH-доступом администратора. Токен не даёт права управления сервером.");
            Paragraph(phone, "Для домашнего входа нужны включённый ПК, доступный извне домашний IPv4 и два UDP-проброса роутера. Мастер показывает записи; роутер настраивается вручную. Канал телефона отдельный от прокси ПК.");
            Paragraph(phone, "QR открывает HTTPS-страницу: iPhone устанавливает профиль через Safari и настройки iOS; Android импортирует профиль в strongSwan VPN Client. Файл .mobileconfig предназначен только для iPhone. QR содержит ссылку, а не сам профиль.");
            Paragraph(phone, "Выдача QR или файла не подтверждает установку. Подтвердите установку в мастере и на мобильной сети проверьте VPN, открытие сайта и IPv4 выхода VPS. Пакеты канала не доказывают наличие интернета. Режим экспериментальный; выход IPv6 автоматически блокируется на VPS.");
            Paragraph(phone, "Владелец создаёт отдельные токены в «Доступ друзей…» и отзывает конкретный доступ. QR-ссылка временная; её отзыв не удаляет установленный VPN. Не публикуйте токены, ссылки и профили.");

            var restore = Topic(tabs, "Восстановление", "Копии, остановка и возврат настроек");
            Paragraph(restore, "Резервные копии → Создать копию сейчас. Восстановить из копии… предлагает состав: программа, настройки и хранилище либо оба набора. Для замены личных данных требуется отдельное согласие. До остановки проверяется и подготавливается выбранная копия.");
            Paragraph(restore, "Состав заменяемых файлов показан в окне: копия не обещает перенос всего доступа VPN на другой ПК. Старые копии без контрольных сумм сохраняются, но новый механизм восстановления их не применяет. После обновления создайте новую копию.");
            Paragraph(restore, "«Отключить прокси на ПК» не останавливает телефон. «Остановить VPN для телефона» не меняет прокси ПК. «Остановить все подключения» выключает оба канала. Закрытие главного окна оставляет ProGo в трее.");
            Paragraph(restore, "При неполном восстановлении настроек Windows/терминалов ProGo сохраняет журнал для повтора и сообщает ошибку. Исправьте доступ к файлу или реестру и повторите выключение. Не удаляйте данные восстановления вручную.");

            var antivirus = Topic(tabs, "Антивирус", "Антивирус и обновления");
            Paragraph(antivirus, "ProGo не отключает защиту Windows и не создаёт исключения. В отчёте антивируса сохраните название обнаружения и имя файла; пароли и токены не отправляйте.");
            Paragraph(antivirus, "«Обновить ProGo» проверяет опубликованный выпуск. Обновлятор сверяет SHA-256 пакета, проверяет архив и промежуточную установку, делает копию и заменяет программу с откатом при обычной ошибке. Проверка обновления сама не устанавливает пакет.");
            Paragraph(antivirus, "Исполняемый файл пока без подписи Authenticode. SHA-256 проверяет целостность, но не заменяет подпись издателя и не гарантирует доверие антивируса. CI не подтверждает отсутствие обнаружений.");
            Paragraph(antivirus, "Если официальный файл блокируется, запросите проверку у вендора, например через Kaspersky OpenTIP. Не отключайте защиту ради установки. Восстановление заблокированного обновлятора описано в инструкции официального выпуска.");
            AddAction(antivirus, UiTheme.Button("Официальный выпуск", delegate { Open("https://github.com/rkhnorkhan-bit/ProGo/releases/latest"); }, true), "Открыть страницу официального выпуска в браузере. Пакет не устанавливается автоматически.");
            AddAction(antivirus, AppCommandUi.Button(AppCommand.OpenUpdateLog, delegate { action(AppCommand.OpenUpdateLog); }), "Открыть журнал обновлятора; обновление программы не запускается.");
            AddAction(antivirus, UiTheme.Button("Kaspersky OpenTIP", delegate { Open("https://opentip.kaspersky.com/"); }, false), "Открыть сайт проверки вендора в браузере. Файлы не отправляются автоматически, защита не отключается.");
            UiTheme.ConfigureKeyboardOrder(this);
        }
        private static FlowLayoutPanel Topic(TabControl tabs, string name, string heading)
        {
            var page = new TabPage(name);
            var body = new HelpTopicPanel { Dock = DockStyle.Fill, Padding = new Padding(20), AutoScroll = true,
                FlowDirection = FlowDirection.TopDown, WrapContents = false, AccessibleName = name + ": инструкция",
                AccessibleRole = AccessibleRole.Pane,
                AccessibleDescription = "Текст инструкции, только чтение. При фокусе здесь стрелки вверх и вниз, Page Up и Page Down прокручивают текст; Home и End переходят к началу и концу." };
            page.Controls.Add(body); tabs.TabPages.Add(page);
            body.Controls.Add(UiTheme.Label(heading, UiTheme.Heading, UiTheme.Text));
            body.SizeChanged += delegate {
                foreach (Control control in body.Controls) if (control is Label)
                    control.MaximumSize = new Size(Math.Max(1, body.ClientSize.Width - body.Padding.Horizontal - 24), 0);
            };
            return body;
        }
        private static void AddAction(FlowLayoutPanel body, Button button, string description)
        {
            button.AccessibleDescription = description;
            button.Enter += delegate {
                body.ScrollControlIntoView(button);
                // The containing tab may finish its own focus scroll after Enter.
                // Reconcile once focus is settled, without moving inactive topics.
                body.BeginInvoke(new Action(delegate {
                    if (!body.IsDisposed && !button.IsDisposed && button.Focused) body.ScrollControlIntoView(button);
                }));
            };
            body.Controls.Add(button);
        }
        private static void Paragraph(Control body, string text)
        {
            var label = UiTheme.Label(text, UiTheme.Body, UiTheme.Muted); label.MaximumSize = new Size(740, 0);
            label.Margin = new Padding(0, 8, 0, 14); body.Controls.Add(label);
        }
        private sealed class HelpTopicPanel : FlowLayoutPanel
        {
            internal HelpTopicPanel() { SetStyle(ControlStyles.Selectable, true); TabStop = true; }
            protected override void OnLayout(LayoutEventArgs e)
            {
                base.OnLayout(e);
                // Flow layout's native scroll extent may omit the bottom inset.
                // Include every laid-out child, its margin and the reading padding.
                int bottom = Padding.Top;
                foreach (Control child in Controls)
                    bottom = Math.Max(bottom, child.Bottom - AutoScrollPosition.Y + child.Margin.Bottom);
                var extent = new Size(0, bottom + Padding.Bottom);
                if (AutoScrollMinSize != extent) AutoScrollMinSize = extent;
            }
            protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
            {
                if (Focused) {
                    int position = -AutoScrollPosition.Y;
                    int line = Font.Height + 8; int page = Math.Max(line, ClientSize.Height - 40);
                    int maximum = VerticalScroll.Visible ? Math.Max(0, VerticalScroll.Maximum - VerticalScroll.LargeChange + 1) : 0;
                    switch (keyData) {
                        case Keys.Up: position -= line; break;
                        case Keys.Down: position += line; break;
                        case Keys.PageUp: position -= page; break;
                        case Keys.PageDown: position += page; break;
                        case Keys.Home: position = 0; break;
                        case Keys.End: position = maximum; break;
                        default: return base.ProcessCmdKey(ref msg, keyData);
                    }
                    AutoScrollPosition = new Point(-AutoScrollPosition.X, Math.Max(0, Math.Min(maximum, position)));
                    return true;
                }
                return base.ProcessCmdKey(ref msg, keyData);
            }
            protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
            protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                if (Focused) ControlPaint.DrawFocusRectangle(e.Graphics, new Rectangle(1, 1, Math.Max(0, ClientSize.Width - 3), Math.Max(0, ClientSize.Height - 3)), UiTheme.WindowText, BackColor);
            }
        }

        private static void Open(string url)
        {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
            catch (Exception ex) { MessageBox.Show("Не удалось открыть браузер: " + ex.Message, "ProGo"); }
        }
    }
}

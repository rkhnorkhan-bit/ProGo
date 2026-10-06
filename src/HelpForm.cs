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
            Controls.Add(tabs);
            var start = Topic(tabs, "Начало", "Подключение и проверка");
            Paragraph(start, "Версия программы: " + typeof(HelpForm).Assembly.GetName().Version.ToString(3) + ". Главная показывает отдельные состояния подключения ПК, прокси приложений и VPN для телефона.");
            Paragraph(start, "1. Настройки → Подключение → Добавить → По адресу сервера: укажите адрес, SSH-порт, пользователя и путь к ключу. Режим «Из SSH config (для опытных)» использует уже настроенное подключение OpenSSH.");
            Paragraph(start, "2. «Первый вход» открывает обычное окно SSH. Сверьте отпечаток ключа сервера. Парольный вход сам по себе не разрешает фоновое подключение: для него нужен доступный SSH-ключ или агент.");
            Paragraph(start, "3. Нажмите «Запустить CLI» для Codex и терминалов либо включите Windows. Проверка SOCKS подтверждает готовность прокси; «Проверить маршрут» отдельно проверяет интернет. Это не VPN для всего трафика ПК.");
            Paragraph(start, "«Диагностика» проверяет настройки, маршрут и скорость. При ошибке исправьте указанную причину и повторите проверку. «Передать диагностику…» открывает отчёт из известных событий без исходных параметров. Проверьте его перед копированием или сохранением. Личный журнал содержит больше подробностей и может включать адреса и пути: не публикуйте его целиком.");
            start.Controls.Add(AppCommandUi.Button(AppCommand.ExportDiagnostics, delegate { action(AppCommand.ExportDiagnostics); }, true));
            start.Controls.Add(AppCommandUi.Button(AppCommand.OpenAppLog, delegate { action(AppCommand.OpenAppLog); }));

            Paragraph(start, "Настройки → Подключение: «Запускать ProGo при входе в Windows» управляет запуском программы. «Подключаться к серверу при запуске ProGo» — отдельная настройка соединения. Обе применяются после сохранения. Если Windows запретила автозапуск, откройте «Автозагрузка в Windows…» и проверьте разрешение для ProGo.");

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
            antivirus.Controls.Add(UiTheme.Button("Официальный выпуск", delegate { Open("https://github.com/rkhnorkhan-bit/ProGo/releases/latest"); }, true));
            antivirus.Controls.Add(AppCommandUi.Button(AppCommand.OpenUpdateLog, delegate { action(AppCommand.OpenUpdateLog); }));
            antivirus.Controls.Add(UiTheme.Button("Kaspersky OpenTIP", delegate { Open("https://opentip.kaspersky.com/"); }, false));
            UiTheme.ConfigureKeyboardOrder(this);
        }
        private static FlowLayoutPanel Topic(TabControl tabs, string name, string heading)
        {
            var page = new TabPage(name);
            var body = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), AutoScroll = true,
                FlowDirection = FlowDirection.TopDown, WrapContents = false, AccessibleName = name + ": инструкция" };
            page.Controls.Add(body); tabs.TabPages.Add(page);
            body.Controls.Add(UiTheme.Label(heading, UiTheme.Heading, UiTheme.Text));
            body.SizeChanged += delegate {
                foreach (Control control in body.Controls) if (control is Label)
                    control.MaximumSize = new Size(Math.Max(1, body.ClientSize.Width - body.Padding.Horizontal - 24), 0);
            };
            return body;
        }
        private static void Paragraph(Control body, string text)
        {
            var label = UiTheme.Label(text, UiTheme.Body, UiTheme.Muted); label.MaximumSize = new Size(740, 0);
            label.Margin = new Padding(0, 8, 0, 14); body.Controls.Add(label);
        }
        private static void Open(string url)
        {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
            catch (Exception ex) { MessageBox.Show("Не удалось открыть браузер: " + ex.Message, "ProGo"); }
        }
    }
}

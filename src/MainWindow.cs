using System;
using System.Drawing;
using System.Windows.Forms;

namespace ProGo
{
    internal sealed class MainWindow : ProGoForm
    {
        private readonly SettingsService settings;
        private readonly ProxyService proxy;
        private readonly HomeVpnService home;
        private readonly CliProxyBridgeService appProxy;
        private readonly Label connection, subtitle, recovery, windowsState, terminalState, phoneState;
        private readonly Button connect;
        private readonly Timer timer = new Timer { Interval = 2000 };
        private readonly Bitmap logo = BrandIcon.Draw(56);
        internal MainWindow(SettingsService settings, ProxyService proxy, HomeVpnService home, Action<string> action, CliProxyBridgeService appProxy = null)
        {
            this.appProxy = appProxy;
            this.settings = settings; this.proxy = proxy; this.home = home;
            Text = "ProGo · Ваше подключение"; ClientSize = new Size(1040, 710); MinimumSize = new Size(970, 680);
            var viewport = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            Controls.Add(viewport);
            var shell = new TableLayoutPanel { ColumnCount = 2, RowCount = 1, MinimumSize = new Size(970, 680), Size = ClientSize };
            viewport.Controls.Add(shell);
            viewport.SizeChanged += delegate { shell.Size = new Size(Math.Max(viewport.ClientSize.Width, shell.MinimumSize.Width), Math.Max(viewport.ClientSize.Height, shell.MinimumSize.Height)); };
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210)); shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var rail = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Surface, Tag = "styled", Padding = new Padding(20) };
            shell.Controls.Add(rail, 0, 0);
            var brand = new PictureBox { Image = logo, Location = new Point(24, 28), Size = new Size(52, 52), SizeMode = PictureBoxSizeMode.Zoom };
            var wordmark = UiTheme.Label("ProGo", UiTheme.Heading, UiTheme.Text); wordmark.Location = new Point(84, 38);
            rail.Controls.Add(brand); rail.Controls.Add(wordmark);
            var nav = new FlowLayoutPanel { Location = new Point(20, 124), Width = 170, Height = 450, FlowDirection = FlowDirection.TopDown, WrapContents = false };
            Nav(nav, "Главная", null, true);
            Nav(nav, "iPhone через ПК", delegate { action("iphone"); }, false);
            Nav(nav, "Подключения", delegate { action("settings"); }, false);
            Nav(nav, "Хранилище", delegate { action("vault"); }, false);
            Nav(nav, "Диагностика", delegate { action("diagnostics"); }, false);
            Nav(nav, "Настройки", delegate { action("settings"); }, false);
            rail.Controls.Add(nav);
            var version = UiTheme.Label("DESKTOP  /  0.2.1\nЛёгкий. Ваш. Под контролем.", UiTheme.Body, UiTheme.Muted);
            version.AutoSize = false; version.Size = new Size(178, 65); version.Dock = DockStyle.Bottom; rail.Controls.Add(version);
            var content = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(28), ColumnCount = 1, RowCount = 5 };
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 85)); content.RowStyles.Add(new RowStyle(SizeType.Absolute, 207));
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 176)); content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 42)); shell.Controls.Add(content, 1, 0);
            var heading = new Panel { Dock = DockStyle.Fill };
            heading.Controls.Add(UiTheme.Label("Ваш интернет. Ваш маршрут.", UiTheme.Title, UiTheme.Text));
            var intro = UiTheme.Label("Подключение к серверу и настройки приложений.", UiTheme.Body, UiTheme.Muted); intro.Location = new Point(0, 48); heading.Controls.Add(intro); content.Controls.Add(heading, 0, 0);
            var hero = new SurfacePanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 18) };
            var eyebrow = UiTheme.Label("ПОДКЛЮЧЕНИЕ К СЕРВЕРУ", UiTheme.Strong, UiTheme.Accent); eyebrow.Location = new Point(22, 17); hero.Controls.Add(eyebrow);
            connection = UiTheme.Label("Готовы подключиться?", UiTheme.Title, UiTheme.Text); connection.Location = new Point(20, 44); hero.Controls.Add(connection);
            subtitle = UiTheme.Label("", UiTheme.Body, UiTheme.Muted); subtitle.Location = new Point(22, 95); subtitle.MaximumSize = new Size(630, 30); subtitle.AutoEllipsis = true; hero.Controls.Add(subtitle);
            var heroActions = new FlowLayoutPanel { Location = new Point(22, 131), Width = 650, Height = 48 };
            connect = UiTheme.Button("Подключиться", delegate { action("connect"); RefreshState(); }, true);
            heroActions.Controls.Add(connect); heroActions.Controls.Add(UiTheme.Button("Отключиться", delegate { action("stop"); RefreshState(); }, false));
            heroActions.Controls.Add(UiTheme.Button("Проверить маршрут", delegate { action("diagnostics"); }, false)); hero.Controls.Add(heroActions); content.Controls.Add(hero, 0, 1);
            var cards = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Margin = new Padding(0) };
            for (int i = 0; i < 3; i++) cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 3));
            windowsState = Card(cards, 0, "WINDOWS", "Приложения", "windows-on", action);
            terminalState = Card(cards, 1, "CLI И CODEX", "Обычный запуск", "cli-start", action);
            phoneState = Card(cards, 2, "IPHONE", "Через домашний ПК", "iphone", action); content.Controls.Add(cards, 0, 2);
            var bottom = new SurfacePanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 14) };
            var title = UiTheme.Label("Соединение под контролем", UiTheme.Strong, UiTheme.Text); title.Location = new Point(20, 16); bottom.Controls.Add(title);
            recovery = UiTheme.Label("", UiTheme.Body, UiTheme.Muted); recovery.Location = new Point(20, 48); recovery.MaximumSize = new Size(620, 0); bottom.Controls.Add(recovery); content.Controls.Add(bottom, 0, 3);
            var footer = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = new Padding(0) };
            footer.Controls.Add(UiTheme.Button("Обновить ProGo", delegate { action("update"); }, false));
            footer.Controls.Add(UiTheme.Button("Открыть Codex", delegate { action("codex-open"); }, false));
            footer.Controls.Add(UiTheme.Button("Помощь", delegate { action("help"); }, false)); content.Controls.Add(footer, 0, 4);
            timer.Tick += delegate { RefreshState(); }; timer.Start(); RefreshState();
        }
        private static void Nav(FlowLayoutPanel panel, string text, EventHandler action, bool selected)
        {
            var button = UiTheme.Button(text, action, selected); button.AutoSize = false; button.Size = new Size(168, 42);
            button.TextAlign = ContentAlignment.MiddleLeft; button.Margin = new Padding(0, 0, 0, 10); panel.Controls.Add(button);
        }
        private static Label Card(TableLayoutPanel cards, int column, string title, string description, string actionName, Action<string> action)
        {
            var card = new SurfacePanel { Dock = DockStyle.Fill, Margin = new Padding(column == 0 ? 0 : 6, 0, column == 2 ? 0 : 6, 18), Padding = new Padding(16) };
            var name = UiTheme.Label(title, UiTheme.Strong, UiTheme.Muted); name.Location = new Point(16, 14); card.Controls.Add(name);
            var state = UiTheme.Label("Выключено", UiTheme.Heading, UiTheme.Text); state.Location = new Point(14, 40); card.Controls.Add(state);
            var caption = UiTheme.Label(description, UiTheme.Body, UiTheme.Muted); caption.Location = new Point(16, 72); card.Controls.Add(caption);
            var open = UiTheme.Button(column == 2 ? "Открыть мастер" : column == 1 ? "Запустить CLI" : "Настроить", delegate { action(column == 0 ? "settings" : actionName); }, false);
            open.Location = new Point(16, 100); open.MinimumSize = new Size(100, 32); open.Height = 32; open.Padding = new Padding(7, 0, 7, 0); card.Controls.Add(open); cards.Controls.Add(card, column, 0); return state;
        }
        private void RefreshState()
        {
            bool ready = proxy.IsListening();
            connection.Text = ready ? "Туннель работает" : "Готовы подключиться?";
            connection.ForeColor = ready ? UiTheme.Accent : UiTheme.Text;
            subtitle.Text = String.IsNullOrWhiteSpace(settings.Current.SshProfile) ? "Добавьте сервер в настройках, чтобы начать." : "Сервер: " + settings.Current.SshProfile;
            connect.Text = ready ? "Переподключиться" : "Подключиться";
            windowsState.Text = SystemProxyService.IsApplied(settings.Current) ? "Включено" : "Выключено";
            terminalState.Text = CliProxyEnvironmentService.IsAppliedToUserEnvironment(settings.Current.HttpProxyPort) ? "Включено" : "Выключено";
            phoneState.Text = home.Relay.IsRunning ? "Канал включён" : "Не запущен";
            recovery.Text = proxy.RecoveryStatus + "\nПрокси приложений: " + CliProxyBridgeService.UrlFor(settings.Current.HttpProxyPort) +
                (appProxy != null && appProxy.IsRunning ? " · работает" : " · выключен");
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) { timer.Stop(); timer.Dispose(); logo.Dispose(); }
            base.Dispose(disposing);
        }
    }
}

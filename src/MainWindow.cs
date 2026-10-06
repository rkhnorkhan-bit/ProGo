using System;
using System.Collections.Generic;
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
        private readonly AutomationPlan automation;
        private readonly AppProxyConsumers appConsumers;
        private readonly ConnectionHealthMonitor health;
        private readonly Label connection, subtitle, recovery, windowsState, terminalState, phoneState;
        private readonly Button connect;
        private Button windowsToggle, cliToggle;
        private readonly Func<AppCommandState> commandState;
        private Button openCodex;
        private readonly Dictionary<string, Button> navigation = new Dictionary<string, Button>();
        private readonly Timer timer = new Timer { Interval = 2000 };
        private readonly Bitmap logo = BrandIcon.Draw(56);
        private readonly Panel viewport;
        private readonly TableLayoutPanel body, cards;
        private readonly List<SurfacePanel> statusCards = new List<SurfacePanel>();
        private bool fitting;
        internal MainWindow(SettingsService settings, ProxyService proxy, HomeVpnService home, Action<AppCommand> action, CliProxyBridgeService appProxy = null, AutomationPlan automation = null, ConnectionHealthMonitor health = null, AppProxyConsumers appConsumers = null, Func<AppCommandState> commandState = null)
        {
            this.commandState = commandState;
            this.appProxy = appProxy; this.appConsumers = appConsumers;
            this.automation = automation;
            this.health = health;
            this.settings = settings; this.proxy = proxy; this.home = home;
            Text = "ProGo · Ваше подключение"; ClientSize = new Size(1040, 710); MinimumSize = new Size(760, 560);
            var shell = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0) };
            Controls.Add(shell);
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210)); shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var rail = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3,
                BackColor = UiTheme.SurfaceBackground, Tag = "styled", Padding = new Padding(20), Margin = new Padding(0) };
            rail.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            rail.RowStyles.Add(new RowStyle(SizeType.AutoSize)); rail.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            rail.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            shell.Controls.Add(rail, 0, 0);
            var brand = new PictureBox { Image = logo, Size = new Size(52, 52), SizeMode = PictureBoxSizeMode.Zoom, Margin = new Padding(0, 0, 12, 0) };
            var wordmark = UiTheme.Label("ProGo", UiTheme.Heading, UiTheme.Text); wordmark.Margin = new Padding(0, 12, 0, 0);
            var branding = Actions(brand, wordmark); branding.Margin = new Padding(0, 8, 0, 30); rail.Controls.Add(branding, 0, 0);
            var nav = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, Margin = new Padding(0),
                FlowDirection = FlowDirection.TopDown, WrapContents = false };
            Nav(nav, "Главная", "home", delegate { SetNavigation("home"); });
            CommandNav(nav, AppCommand.Phone, action);
            CommandNav(nav, AppCommand.Connections, action);
            CommandNav(nav, AppCommand.Vault, action);
            CommandNav(nav, AppCommand.Diagnostics, action);
            CommandNav(nav, AppCommand.Settings, action);
            var stopAll = AppCommandUi.Button(AppCommand.StopAll, delegate { action(AppCommand.StopAll); RefreshState(); }, false);
            stopAll.AutoSize = false; stopAll.Size = new Size(168, 60); nav.Controls.Add(stopAll);
            rail.Controls.Add(nav, 0, 1);
            var version = UiTheme.Label("DESKTOP  /  " + typeof(MainWindow).Assembly.GetName().Version.ToString(3) + "\nЛёгкий. Ваш. Под контролем.", UiTheme.Body, UiTheme.Muted);
            version.Dock = DockStyle.Fill; version.Margin = new Padding(0, 12, 0, 0); rail.Controls.Add(version, 0, 2);
            rail.SizeChanged += delegate { version.MaximumSize = new Size(Math.Max(1, rail.ClientSize.Width - rail.Padding.Horizontal), 0); };
            nav.ClientSizeChanged += delegate {
                int width = Math.Max(1, nav.ClientSize.Width - SystemInformation.VerticalScrollBarWidth);
                foreach (Control button in nav.Controls) {
                    button.MinimumSize = Size.Empty; button.Width = Math.Max(1, width - button.Margin.Horizontal);
                    int textHeight = TextRenderer.MeasureText(button.Text, button.Font,
                        new Size(Math.Max(1, button.Width - button.Padding.Horizontal - 12), int.MaxValue), TextFormatFlags.WordBreak).Height;
                    button.Height = Math.Max(Math.Max(42, button.GetPreferredSize(new Size(button.Width, 0)).Height), textHeight + button.Padding.Vertical + 12);
                }
            };
            var content = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), ColumnCount = 1, RowCount = 2, Margin = new Padding(0) };
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); content.RowStyles.Add(new RowStyle(SizeType.AutoSize)); shell.Controls.Add(content, 1, 0);
            viewport = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Margin = new Padding(0) }; content.Controls.Add(viewport, 0, 0);
            // A right anchor constrains horizontal scrolling; keeping this child undocked
            // lets vertical reflow retain its scroll origin instead of redocking at the top.
            body = Stack(); body.Dock = DockStyle.None; body.AutoSize = false;
            body.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; viewport.Controls.Add(body);
            var heading = Stack(UiTheme.Label("Ваш интернет. Ваш маршрут.", UiTheme.Title, UiTheme.Text),
                UiTheme.Label("Подключение к серверу и настройки приложений.", UiTheme.Body, UiTheme.Muted));
            heading.Margin = new Padding(0, 0, 0, 14); body.Controls.Add(heading);
            var eyebrow = UiTheme.Label("ПОДКЛЮЧЕНИЕ К СЕРВЕРУ", UiTheme.Strong, UiTheme.Accent);
            connection = UiTheme.Label("Готовы подключиться?", UiTheme.Title, UiTheme.Text);
            subtitle = UiTheme.Label("", UiTheme.Body, UiTheme.Muted);
            connect = AppCommandUi.Button(AppCommand.Connect, delegate { action(AppCommand.Connect); RefreshState(); }, true);
            var heroActions = Actions(connect, AppCommandUi.Button(AppCommand.StopDesktop, delegate { action(AppCommand.StopDesktop); RefreshState(); }, false),
                AppCommandUi.Button(AppCommand.CheckRoute, delegate { action(AppCommand.CheckRoute); }, false));
            body.Controls.Add(Surface(eyebrow, connection, subtitle, heroActions));
            cards = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 3, RowCount = 1, Margin = new Padding(0) };
            for (int i = 0; i < 3; i++) cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 3));
            cards.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            windowsState = Card(cards, 0, "WINDOWS", "Параметры Windows", AppCommand.EnableWindows, action);
            terminalState = Card(cards, 1, "CODEX И ТЕРМИНАЛЫ", "Для новых терминалов", AppCommand.StartCli, action);
            phoneState = Card(cards, 2, "ТЕЛЕФОН", "Через домашний ПК", AppCommand.Phone, action); body.Controls.Add(cards);
            recovery = UiTheme.Label("", UiTheme.Body, UiTheme.Muted);
            body.Controls.Add(Surface(UiTheme.Label("Соединение под контролем", UiTheme.Strong, UiTheme.Text), recovery));
            openCodex = AppCommandUi.Button(AppCommand.OpenCodex, delegate { action(AppCommand.OpenCodex); }, false);
            var footer = Actions(AppCommandUi.Button(AppCommand.Update, delegate { action(AppCommand.Update); }, false),
                openCodex, AppCommandUi.Button(AppCommand.Help, delegate { action(AppCommand.Help); }, false));
            footer.Margin = new Padding(0, 12, 0, 0); content.Controls.Add(footer, 0, 1);
            viewport.ClientSizeChanged += delegate { FitDashboard(); };
            body.Layout += delegate { FitDashboard(); };
            Shown += delegate { FitDashboard(); };
            timer.Tick += delegate { RefreshState(); }; timer.Start(); RefreshState();
        }
        private static TableLayoutPanel Stack(params Control[] controls)
        {
            var stack = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1, RowCount = 0, Margin = new Padding(0) };
            stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            foreach (var control in controls) { stack.RowStyles.Add(new RowStyle(SizeType.AutoSize)); stack.Controls.Add(control, 0, stack.RowCount++); }
            bool sizing = false;
            stack.Layout += delegate {
                if (sizing) return; sizing = true;
                try {
                    foreach (Control child in stack.Controls) {
                        int width = Math.Max(1, stack.ClientSize.Width - stack.Padding.Horizontal - child.Margin.Horizontal);
                        var label = child as Label;
                        if (label != null) label.MaximumSize = new Size(width, 0);
                    }
                } finally { sizing = false; }
            };
            return stack;
        }
        private static FlowLayoutPanel Actions(params Control[] controls)
        {
            var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight, WrapContents = true, Margin = new Padding(0) };
            flow.Controls.AddRange(controls); return flow;
        }
        private static SurfacePanel Surface(params Control[] controls)
        {
            var panel = new SurfacePanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(18), Margin = new Padding(0, 0, 0, 16) };
            panel.Controls.Add(Stack(controls)); return panel;
        }
        private void FitDashboard()
        {
            if (fitting || cards == null || viewport.ClientSize.Width < 1) return;
            fitting = true;
            try {
                // Reserve a vertical scrollbar gutter so changing text cannot oscillate widths.
                int width = Math.Max(1, viewport.ClientSize.Width - SystemInformation.VerticalScrollBarWidth);
                if (body.Width != width) body.Width = width;
                int minimumCard = 210;
                foreach (var card in statusCards) {
                    var stack = (TableLayoutPanel)card.Controls[0];
                    foreach (Control control in stack.Controls) {
                        var button = control as Button;
                        if (button != null) minimumCard = Math.Max(minimumCard, button.GetPreferredSize(Size.Empty).Width + card.Padding.Horizontal);
                    }
                }
                int columns = body.Width >= minimumCard * 3 + 24 ? 3 : 1;
                if (cards.ColumnCount != columns) {
                    cards.SuspendLayout();
                    cards.ColumnCount = columns; cards.RowCount = columns == 3 ? 1 : 3;
                    cards.ColumnStyles.Clear(); cards.RowStyles.Clear();
                    for (int i = 0; i < columns; i++) cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / columns));
                    for (int i = 0; i < cards.RowCount; i++) cards.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                    for (int i = 0; i < statusCards.Count; i++) cards.SetCellPosition(statusCards[i], new TableLayoutPanelCellPosition(columns == 3 ? i : 0, columns == 3 ? 0 : i));
                    cards.ResumeLayout(true);
                }
                for (int i = 0; i < statusCards.Count; i++) statusCards[i].Margin = new Padding(columns == 3 && i != 0 ? 6 : 0, 0, columns == 3 && i != 2 ? 6 : 0, 16);
                int height = body.GetPreferredSize(new Size(width, 0)).Height;
                if (body.Height != height) body.Height = height;
            } finally { fitting = false; }
        }
        private void CommandNav(FlowLayoutPanel panel, AppCommand command, Action<AppCommand> action)
        {
            var definition = AppCommands.Get(command);
            Nav(panel, definition.CompactLabel, definition.Navigation, delegate { action(command); }, definition.Effect);
        }
        private void Nav(FlowLayoutPanel panel, string text, string route, EventHandler action, string effect = null)
        {
            var button = UiTheme.Button(text, action, route == "home"); button.AutoSize = false; button.Size = new Size(168, 42);
            button.TextAlign = ContentAlignment.MiddleLeft; button.Margin = new Padding(0, 0, 0, 10); panel.Controls.Add(button);
            button.AccessibleDescription = effect;
            navigation.Add(route, button);
        }
        internal void SetNavigation(string route)
        {
            foreach (var item in navigation) { item.Value.Tag = item.Key == route ? "primary" : null; UiTheme.Apply(item.Value); }
        }
        private Label Card(TableLayoutPanel cards, int column, string title, string description, AppCommand command, Action<AppCommand> action)
        {
            var name = UiTheme.Label(title, UiTheme.Strong, UiTheme.Muted);
            var heading = Actions(name);
            var state = UiTheme.Label("Выключено", UiTheme.Heading, UiTheme.Text);
            var caption = UiTheme.Label(description, UiTheme.Body, UiTheme.Muted);
            var open = UiTheme.Button(AppCommands.Get(command).CardLabel, delegate {
                action(column == 0 ? (SystemProxyService.IsApplied(settings.Current) ? AppCommand.DisableWindows : AppCommand.EnableWindows) :
                    column == 1 ? (CliProxyEnvironmentService.IsAppliedToUserEnvironment(settings.Current.HttpProxyPort) ? AppCommand.StopCli : AppCommand.StartCli) : command);
                RefreshState();
            }, false);
            open.AccessibleDescription = AppCommands.Get(command).Effect;
            if (column == 0) {
                windowsToggle = open;
                var configure = new LinkLabel { Text = AppCommands.Get(AppCommand.WindowsSettings).CompactLabel, AccessibleDescription = AppCommands.Get(AppCommand.WindowsSettings).Effect, AutoSize = true, Margin = new Padding(12, 0, 0, 8), Font = UiTheme.Body,
                    LinkColor = UiTheme.Accent, ActiveLinkColor = UiTheme.Text, VisitedLinkColor = UiTheme.Accent, AccessibleName = "Настройки прокси Windows" };
                configure.LinkClicked += delegate { action(AppCommand.WindowsSettings); }; heading.Controls.Add(configure);
            }
            if (column == 1) cliToggle = open;
            open.MinimumSize = new Size(100, 32); open.Padding = new Padding(7, 0, 7, 0);
            var card = Surface(heading, state, caption, open); card.Dock = DockStyle.Fill; card.Padding = new Padding(16);
            statusCards.Add(card); cards.Controls.Add(card, column, 0); return state;
        }
        internal void RefreshConnectionState() { RefreshState(); }
        private void RefreshState()
        {
            var commands = commandState == null ? new AppCommandState(new AppCommand[0], proxy.IsConnecting, false) : commandState();
            bool cliPending = commands.IsPending(AppCommand.StartCli), windowsPending = commands.IsPending(AppCommand.EnableWindows);
            var status = health == null ? new ConnectionHealthSnapshot("", ConnectionProbeState.Unknown, ConnectionProbeState.Unknown) : health.Current;
            bool ready = status.SocksReady;
            connection.Text = status.Title;
            if (proxy.IsConnecting && !status.SocksReady) connection.Text = "Подключаемся к серверу…";
            connection.ForeColor = status.InternetVerified ? UiTheme.Accent : status.Socks == ConnectionProbeState.Failed ? UiTheme.Error : UiTheme.Text;
            subtitle.Text = status.Summary;
            connect.Text = AppCommands.Get(ready ? AppCommand.Reconnect : AppCommand.Connect).CompactLabel;
            connect.AccessibleDescription = AppCommands.Get(ready ? AppCommand.Reconnect : AppCommand.Connect).Effect;
            connect.Enabled = AppCommands.CanExecute(AppCommand.Connect, commands);
            openCodex.Enabled = AppCommands.CanExecute(AppCommand.OpenCodex, commands);
            bool windowsApplied = SystemProxyService.IsApplied(settings.Current), cliApplied = CliProxyEnvironmentService.IsAppliedToUserEnvironment(settings.Current.HttpProxyPort);
            windowsState.Text = windowsApplied ? "Настроено" : "Не настроено";
            terminalState.Text = cliApplied ? "Настроено" : CliProxyEnvironmentService.IsPartiallyApplied(settings.Current.HttpProxyPort) ? "Частично" : "Не настроено";
            windowsToggle.Text = AppCommands.Get(windowsApplied ? AppCommand.DisableWindows : AppCommand.EnableWindows).CompactLabel;
            cliToggle.Text = AppCommands.Get(cliApplied ? AppCommand.StopCli : AppCommand.StartCli).CompactLabel;
            if (cliPending) cliToggle.Text = "Подключаем CLI…";
            if (windowsPending) windowsToggle.Text = "Подключаем…";
            cliToggle.Enabled = AppCommands.CanExecute(cliPending || !cliApplied ? AppCommand.StartCli : AppCommand.StopCli, commands);
            windowsToggle.Enabled = AppCommands.CanExecute(windowsPending || !windowsApplied ? AppCommand.EnableWindows : AppCommand.DisableWindows, commands);
            cliToggle.AccessibleDescription = AppCommands.Get(cliPending || !cliApplied ? AppCommand.StartCli : AppCommand.StopCli).Effect;
            windowsToggle.AccessibleDescription = AppCommands.Get(windowsPending || !windowsApplied ? AppCommand.EnableWindows : AppCommand.DisableWindows).Effect;
            windowsToggle.AccessibleName = windowsToggle.Text + " прокси Windows"; cliToggle.AccessibleName = cliToggle.Text;
            phoneState.Text = home.Relay.IsRunning ? "Канал включён" : "Не запущен";
            recovery.Text = proxy.RecoveryStatus + "\nПрокси приложений: " + CliProxyBridgeService.UrlFor(settings.Current.HttpProxyPort) +
                (appProxy != null && appProxy.IsRunning ? " · работает" +
                    (appConsumers == null ? "" : " для: " + appConsumers.Summary) : " · служба остановлена");
            if (automation != null)
            {
                string automaticStatus = automation.GetStatusText(ready);
                if (automaticStatus.Length > 0) recovery.Text += "\n" + automaticStatus;
            }
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) { timer.Stop(); timer.Dispose(); logo.Dispose(); }
            base.Dispose(disposing);
        }
    }
}

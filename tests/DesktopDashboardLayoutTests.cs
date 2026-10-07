using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct DashboardRect { public int Left, Top, Right, Bottom; }
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetClientRect(IntPtr window, out DashboardRect rectangle);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int width, int height, uint flags);
        private static Size DashboardNativeClient(Form form)
        {
            DashboardRect rect;
            if (!GetClientRect(form.Handle, out rect)) throw new System.ComponentModel.Win32Exception();
            return new Size(rect.Right - rect.Left, rect.Bottom - rect.Top);
        }
        private static void DashboardFixtureSize(Form form, Size available)
        {
            form.MinimumSize = Size.Empty;
            form.MaximumSize = new Size(available.Width + 80, available.Height + 100);
            form.ClientSize = available;
            var actual = DashboardNativeClient(form);
            // Form.SetBoundsCore unconditionally clamps to MaxWindowTrackSize.
            // MaximumSize permits the native tracking bounds, then resize only this
            // fixture HWND to render a full target width even when
            // the CI virtual desktop is smaller; never change display settings.
            if (actual != available && !SetWindowPos(form.Handle, IntPtr.Zero, 0, 0,
                form.Width + available.Width - actual.Width, form.Height + available.Height - actual.Height, 0x0016))
                throw new System.ComponentModel.Win32Exception();
        }
        private static void DashboardKeyboard(SettingsService settings)
        {
            int commands = 0; AppCommand? last = null;
            var pending = new AppCommandState(new AppCommand[0], false, false);
            using (var health = new ConnectionHealthMonitor(() => settings.Current,
                delegate { return true; }, delegate { return new InternetProbeResult(true, 200); }))
            using (var proxy = new ProxyService(settings))
            using (var relay = new Ikev2RelayService())
            using (var home = new HomeVpnService(relay))
            using (var form = new MainWindow(settings, proxy, home, command => { commands++; last = command; },
                null, null, health, null, () => pending)) {
                form.Show(); Application.DoEvents(); ((Timer)Field(form, "timer")).Stop();
                var navigation = (System.Collections.Generic.Dictionary<string, Button>)Field(form, "navigation");
                var connect = (Button)Field(form, "connect"); var windows = (Button)Field(form, "windowsToggle");
                var cli = (Button)Field(form, "cliToggle");
                Func<AppCommand, Button> button = command => Descendants(form).OfType<Button>().Single(b => b.Text == AppCommands.Get(command).CompactLabel);
                var order = new Control[] {
                    navigation["home"], navigation["iphone"], navigation["connections"], navigation["vault"], navigation["diagnostics"], navigation["settings"],
                    button(AppCommand.StopAll), connect, button(AppCommand.StopDesktop), button(AppCommand.CheckRoute),
                    Descendants(form).OfType<LinkLabel>().Single(), windows, cli,
                    Descendants(form).OfType<Button>().Single(b => b.Text == "Открыть мастер"),
                    button(AppCommand.Update), button(AppCommand.OpenCodex), button(AppCommand.Help)
                };
                foreach (var size in new[] { new Size(1200, 710), new Size(744, 521) }) {
                    DashboardFixtureSize(form, size); Application.DoEvents();
                    Check(((TableLayoutPanel)Field(form, "cards")).ColumnCount == (size.Width > 1000 ? 3 : 1),
                        "dashboard keyboard fixture reaches its wide/narrow layout " + size.Width);
                    Console.WriteLine("Dashboard traversal: " + String.Join(" | ", Descendants(form).Where(c => c.TabStop).Select(c => c.GetType().Name + ":" + c.Text + " tab=" + c.TabIndex)));
                    KeyboardWalk(form, order, "dashboard " + size.Width);
                    Check(commands == 0, "dashboard keyboard traversal dispatches no actions " + size.Width);
                }
                Check(form.AcceptButton == null && form.CancelButton == null, "dashboard has no implicit connect or disconnect default");
                Check(order.All(c => !String.IsNullOrEmpty(c.AccessibilityObject.Name) && !String.IsNullOrEmpty(c.AccessibilityObject.Description)),
                    "all dashboard actions expose their name and effect");
                foreach (string field in new[] { "connection", "subtitle", "windowsState", "terminalState", "phoneState", "recovery" }) {
                    var value = (Label)Field(form, field);
                    Check(!String.IsNullOrEmpty(value.AccessibilityObject.Name) && value.AccessibilityObject.Description == value.Text,
                        "dashboard status has a named current result: " + field);
                }
                Check(connect.AccessibilityObject.Name == "Подключиться", "dashboard announces unverified connection action");
                HealthRefresh(health); form.RefreshConnectionState();
                Check(connect.AccessibilityObject.Name == "Переподключиться" &&
                    ((Label)Field(form, "connection")).AccessibilityObject.Description == ((Label)Field(form, "connection")).Text,
                    "dashboard accessible action and result track fresh connection evidence");
                string purpose = windows.AccessibilityObject.Name;
                UiTheme.Apply(form);
                Check(windows.AccessibilityObject.Name == purpose && purpose.Contains("прокси Windows"),
                    "palette application preserves dashboard-specific button purpose");
                pending = new AppCommandState(new[] { AppCommand.StartCli, AppCommand.EnableWindows }, true, false);
                form.RefreshConnectionState();
                Check(!connect.Enabled && !cli.Enabled && !windows.Enabled && cli.AccessibilityObject.Name == "Подключаем CLI…" &&
                    windows.AccessibilityObject.Name.Contains("Подключаем"), "dashboard pending actions are named and disabled");
                KeyboardWalk(form, order.Where(c => c.Enabled).ToArray(), "dashboard pending skips unavailable actions");
                Check(commands == 0, "pending keyboard traversal never dispatches a disabled action");
                pending = new AppCommandState(new AppCommand[0], false, false); form.RefreshConnectionState();
                Check(cli.Enabled && windows.Enabled && connect.Enabled, "dashboard cancelled pending state restores keyboard actions");
                var expected = cli.Text == "Выключить CLI" ? AppCommand.StopCli : AppCommand.StartCli;
                cli.PerformClick(); Check(commands == 1 && last == expected, "dashboard keyboard changes preserve ordinary CLI dispatch");
                Shot(form, "keyboard-dashboard-narrow"); form.Close();
            }
            using (var button = UiTheme.Button("Первое действие", null, false)) {
                button.Text = "Новое действие"; UiTheme.Apply(button);
                Check(button.AccessibilityObject.Name == "Новое действие", "native button name follows its caption without a stale style override");
            }
        }

        private static void DashboardLayout(SettingsService settings)
        {
            using (var proxy = new ProxyService(settings))
            using (var relay = new Ikev2RelayService())
            using (var home = new HomeVpnService(relay))
            {
                DashboardLayoutCase(settings, proxy, home, 1f, new Size(1040, 710), false, "dashboard-wide");
                DashboardLayoutCase(settings, proxy, home, 1f, new Size(744, 521), false, "dashboard-minimum");
                DashboardLayoutCase(settings, proxy, home, 1f, new Size(1320, 690), true, "dashboard-1366-long");
                foreach (float scale in new[] { 1.25f, 1.5f, 2f })
                    DashboardLayoutCase(settings, proxy, home, scale, new Size(1040, 710), true,
                        "dashboard-scale-" + (int)(scale * 100));
                // The same instance must return to three columns after a narrow layout.
                using (var form = new MainWindow(settings, proxy, home, delegate { })) {
                    form.Show(); form.ClientSize = new Size(760, 560); Application.DoEvents();
                    var cards = (TableLayoutPanel)Field(form, "cards");
                    Check(cards.ColumnCount == 1, "dashboard narrow cards stack in one column");
                    form.ClientSize = new Size(1200, 710); Application.DoEvents();
                    Check(cards.ColumnCount == 3 && cards.Controls.Count == 3, "dashboard widening restores all three cards without duplication");
                    form.Close();
                }
            }
        }
        private static void DashboardLayoutCase(SettingsService settings, ProxyService proxy, HomeVpnService home,
            float scale, Size available, bool longText, string name)
        {
            string action = null;
            using (var form = new MainWindow(settings, proxy, home, delegate(AppCommand value) { action = AppCommands.Get(value).LegacyId; })) {
                form.Show(); Application.DoEvents();
                ((Timer)Field(form, "timer")).Stop();
                // This is intentionally a constrained form.Scale stress test, not native DPI coverage.
                if (scale != 1f) form.Scale(new SizeF(scale, scale));
                DashboardFixtureSize(form, available);
                if (longText) {
                    ((Label)Field(form, "connection")).Text = "Соединение с сервером требует повторной проверки";
                    ((Label)Field(form, "subtitle")).Text = "Прокси отвечает, но доступ в интернет пока не подтверждён. Проверьте маршрут и настройки подключения.";
                    ((Label)Field(form, "recovery")).Text = string.Join("\n", Enumerable.Repeat(
                        "Автонастройка Windows: ошибка. Проверьте настройки и повторите подключение.", 4));
                }
                Application.DoEvents(); form.PerformLayout(); Application.DoEvents();
                Check(form.ClientSize == available && DashboardNativeClient(form) == available,
                    name + " renders the requested client area inside the actual window: window=" + form.Size + " native=" + DashboardNativeClient(form));
                var viewport = (Panel)Field(form, "viewport");
                var cards = (TableLayoutPanel)Field(form, "cards");
                var body = (TableLayoutPanel)Field(form, "body");
                Shot(form, name);
                Check(!viewport.HorizontalScroll.Visible && body.Width <= viewport.ClientSize.Width,
                    name + " has no horizontal scrolling: body=" + body.Bounds + " viewport=" + viewport.ClientSize +
                    " display=" + viewport.DisplayRectangle + " scroll=" + viewport.AutoScrollPosition);
                Check(cards.Controls.Cast<Control>().All(c => c.Width > 0 && c.Right <= cards.ClientSize.Width), name + " keeps cards inside content width");
                Check(cards.ColumnCount == 1 || cards.Controls.Cast<Control>().Select(c => c.Height).Distinct().Count() == 1,
                    name + " aligns card heights in a shared row");
                foreach (string field in new[] { "connection", "subtitle", "recovery", "windowsState", "terminalState", "phoneState" }) {
                    var label = (Label)Field(form, field);
                    Check(label.AutoSize && !label.AutoEllipsis && label.Right <= label.Parent.ClientSize.Width && label.Bottom <= label.Parent.ClientSize.Height,
                        name + " grows and wraps " + field + ": " + label.Bounds + " parent=" + label.Parent.ClientSize);
                }
                var tables = Descendants(body).OfType<TableLayoutPanel>().Where(t => t.ColumnCount == 1).ToArray();
                Check(tables.All(t => {
                    var controls = t.Controls.Cast<Control>().OrderBy(c => t.GetRow(c)).ToArray();
                    return controls.Zip(controls.Skip(1), (a, b) => a.Bottom <= b.Top).All(v => v);
                }), name + " has no overlapping stack rows");
                foreach (string text in new[] { "Обновить ProGo", "Открыть Codex CLI с прокси", "Помощь" }) {
                    var button = Descendants(form).OfType<Button>().Single(b => b.Text == text);
                    Check(form.ClientRectangle.Contains(form.RectangleToClient(button.RectangleToScreen(button.ClientRectangle))), name + " keeps footer visible: " + text);
                }
                Shot(form, name);
                if (longText) { viewport.ScrollControlIntoView((Label)Field(form, "recovery")); Application.DoEvents(); Shot(form, name + "-status"); }
                var actions = Descendants(body).OfType<Button>().ToArray();
                foreach (var button in actions) {
                    viewport.ScrollControlIntoView(button); Application.DoEvents();
                    var area = viewport.RectangleToClient(button.RectangleToScreen(button.ClientRectangle));
                    Check(viewport.ClientRectangle.Contains(area), name + " makes action reachable by vertical scroll: " + button.Text + " " + area);
                }
                var cli = (Button)Field(form, "cliToggle"); cli.PerformClick();
                Check(action == (cli.Text == "Выключить CLI" ? "cli-off" : "cli-start"), name + " keeps ordinary CLI command");
                var nav = (System.Collections.Generic.Dictionary<string, Button>)Field(form, "navigation");
                var navPanel = (FlowLayoutPanel)nav["settings"].Parent;
                Check(navPanel.Controls.Cast<Control>().All(b => {
                    var textArea = new Size(Math.Max(1, b.ClientSize.Width - b.Padding.Horizontal), int.MaxValue);
                    return TextRenderer.MeasureText(b.Text, b.Font, textArea, TextFormatFlags.WordBreak).Height <= b.ClientSize.Height - b.Padding.Vertical;
                }), name + " gives wrapped navigation names enough height");
                navPanel.ScrollControlIntoView(nav["settings"]); Application.DoEvents();
                Check(!navPanel.HorizontalScroll.Visible && navPanel.ClientRectangle.Contains(nav["settings"].Bounds), name + " keeps navigation reachable");
                form.Close();
            }
        }
    }
}

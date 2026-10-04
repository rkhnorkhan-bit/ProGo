using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ProGo
{
    internal static partial class DesktopTests
    {
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
            using (var form = new MainWindow(settings, proxy, home, delegate(string value) { action = value; })) {
                form.Show(); Application.DoEvents();
                ((Timer)Field(form, "timer")).Stop();
                // This is intentionally a constrained form.Scale stress test, not native DPI coverage.
                if (scale != 1f) form.Scale(new SizeF(scale, scale));
                form.MinimumSize = Size.Empty; form.ClientSize = available;
                if (longText) {
                    ((Label)Field(form, "connection")).Text = "Соединение с сервером требует повторной проверки";
                    ((Label)Field(form, "subtitle")).Text = "Прокси отвечает, но доступ в интернет пока не подтверждён. Проверьте маршрут и настройки подключения.";
                    ((Label)Field(form, "recovery")).Text = string.Join("\n", Enumerable.Repeat(
                        "Автонастройка Windows: ошибка. Проверьте настройки и повторите подключение.", 4));
                }
                Application.DoEvents(); form.PerformLayout(); Application.DoEvents();
                var viewport = (Panel)Field(form, "viewport");
                var cards = (TableLayoutPanel)Field(form, "cards");
                var body = (TableLayoutPanel)Field(form, "body");
                Shot(form, name);
                Check(!viewport.HorizontalScroll.Visible && body.Width <= viewport.ClientSize.Width,
                    name + " has no horizontal scrolling: body=" + body.Bounds + " viewport=" + viewport.ClientSize +
                    " display=" + viewport.DisplayRectangle + " scroll=" + viewport.AutoScrollPosition);
                Check(cards.Controls.Cast<Control>().All(c => c.Width > 0 && c.Right <= cards.ClientSize.Width), name + " keeps cards inside content width");
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
                foreach (string text in new[] { "Обновить ProGo", "Открыть Codex", "Помощь" }) {
                    var button = Descendants(form).OfType<Button>().Single(b => b.Text == text);
                    Check(form.ClientRectangle.Contains(form.RectangleToClient(button.RectangleToScreen(button.ClientRectangle))), name + " keeps footer visible: " + text);
                }
                Shot(form, name);
                foreach (string field in new[] { "connect", "windowsToggle", "cliToggle" }) {
                    var button = (Button)Field(form, field);
                    viewport.ScrollControlIntoView(button); Application.DoEvents();
                    var area = viewport.RectangleToClient(button.RectangleToScreen(button.ClientRectangle));
                    Check(viewport.ClientRectangle.Contains(area), name + " makes action reachable by vertical scroll: " + field + " " + area);
                }
                var cli = (Button)Field(form, "cliToggle"); cli.PerformClick();
                Check(action == (cli.Text == "Выключить CLI" ? "cli-off" : "cli-start"), name + " keeps ordinary CLI command");
                var nav = (System.Collections.Generic.Dictionary<string, Button>)Field(form, "navigation");
                var navPanel = (FlowLayoutPanel)nav["settings"].Parent;
                navPanel.ScrollControlIntoView(nav["settings"]); Application.DoEvents();
                Check(!navPanel.HorizontalScroll.Visible && navPanel.ClientRectangle.Contains(nav["settings"].Bounds), name + " keeps navigation reachable");
                if (longText) { viewport.ScrollControlIntoView((Label)Field(form, "recovery")); Application.DoEvents(); Shot(form, name + "-status"); }
                form.Close();
            }
        }
    }
}

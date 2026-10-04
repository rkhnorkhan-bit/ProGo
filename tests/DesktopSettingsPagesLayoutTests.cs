using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        private static void SettingsPagesLayout(SettingsService settings)
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") { Console.WriteLine("SKIP: settings pages require isolated native CI"); return; }
            SettingsPagesLayoutCase(settings, 1f, new Size(900, 700), false, "settings-pages-normal");
            SettingsPagesLayoutCase(settings, 1f, new Size(834, 611), false, "settings-pages-minimum");
            SettingsPagesLayoutCase(settings, 1f, new Size(1320, 690), true, "settings-pages-1366");
            foreach (float scale in new[] { 1.25f, 1.5f, 2f })
                SettingsPagesLayoutCase(settings, scale, new Size(900, 700), true, "settings-pages-scale-" + (int)(scale * 100));
        }
        private static void SettingsPagesLayoutCase(SettingsService settings, float scale, Size available, bool longText, string name)
        {
            var bytes = File.ReadAllBytes(AppPaths.SettingsPath);
            using (var form = new SshProfilesSettingsForm(settings)) {
                form.Show(); Application.DoEvents(); ((Timer)Field(form, "currentValuesTimer")).Stop();
                // Geometry stress only: form.Scale does not simulate an actual DPI/display change.
                if (scale != 1f) form.Scale(new SizeF(scale, scale));
                DashboardFixtureSize(form, available);
                var tabs = Descendants(form).OfType<TabControl>().Single();
                ((TextBox)Field(form, "host")).Text = "pending.example.org";
                ((NumericUpDown)Field(form, "port")).Value = 19998;
                ((NumericUpDown)Field(form, "clearSeconds")).Value = 59;
                ((TextBox)Field(form, "endpoint")).Text = "https://example.org/check?fixture=long-pending-diagnostic-endpoint";
                ((CheckBox)Field(form, "autoStart")).Checked = false;
                ((CheckBox)Field(form, "autoSwitchProfile")).Checked = false;
                ((CheckBox)Field(form, "autoHttpPort")).Checked = false;
                ((NumericUpDown)Field(form, "httpPort")).Value = 31881;
                Check(form.ClientSize == available && DashboardNativeClient(form) == available, name + " renders the requested physical client area");
                for (int index = 1; index < tabs.TabCount; index++) {
                    tabs.SelectedIndex = index; Application.DoEvents();
                    var page = tabs.TabPages[index];
                    var viewport = Descendants(page).OfType<Panel>().Single(c => c.Name == "settingsViewport");
                    var body = Descendants(page).OfType<TableLayoutPanel>().Single(c => c.Name == "settingsPageBody");
                    if (longText) {
                        foreach (var label in body.Controls.OfType<Label>()) label.Text += "\nДополнительная подсказка для проверки переноса длинного текста при ограниченной ширине окна. Проверьте параметры перед сохранением.";
                        foreach (var option in body.Controls.OfType<CheckBox>()) option.Text += " после проверки выбранного подключения и доступности свободного порта";
                    }
                    Application.DoEvents(); form.PerformLayout(); Application.DoEvents();
                    string prefix = name + " tab=" + index;
                    Shot(form, name + "-" + index);
                    Check(!viewport.HorizontalScroll.Visible && body.Width <= viewport.ClientSize.Width,
                        prefix + " fits without horizontal scrolling: body=" + body.Bounds + " viewport=" + viewport.ClientSize);
                    Check(Descendants(body).Where(c => c.Parent == body || c.Parent is TableLayoutPanel || c.Parent is FlowLayoutPanel)
                        .All(c => c.Left >= 0 && c.Right <= c.Parent.ClientSize.Width && c.Top >= 0 && c.Bottom <= c.Parent.ClientSize.Height),
                        prefix + " keeps rows, fields and buttons inside their containers");
                    Check(Descendants(body).OfType<TableLayoutPanel>().Concat(new[] { body }).All(t => {
                        var rows = t.Controls.Cast<Control>().OrderBy(c => c.Top).ToArray();
                        return rows.Zip(rows.Skip(1), (a, b) => a.Bottom <= b.Top).All(v => v);
                    }), prefix + " separates labels, fields, hints and action rows");
                    Check(Descendants(body).OfType<Label>().All(l => !l.AutoEllipsis && l.GetPreferredSize(new Size(l.Width, 0)).Height <= l.Height),
                        prefix + " provides enough height for complete hints and field captions");
                    Check(Descendants(body).OfType<CheckBox>().All(c => TextRenderer.MeasureText(c.Text, c.Font,
                        new Size(Math.Max(1, c.ClientSize.Width - SystemInformation.MenuCheckSize.Width - 8), int.MaxValue), TextFormatFlags.WordBreak).Height <= c.ClientSize.Height),
                        prefix + " wraps complete preference captions");
                    foreach (var control in Descendants(body).Where(c => c is Button || c is CheckBox || c is NumericUpDown || c is ComboBox || c is TextBox).ToArray()) {
                        viewport.ScrollControlIntoView(control); Application.DoEvents();
                        Check(viewport.ClientRectangle.Contains(viewport.RectangleToClient(control.RectangleToScreen(control.ClientRectangle))),
                            prefix + " reaches field/action through vertical scroll: " + control.GetType().Name + " " + control.Text);
                    }
                    foreach (var button in new[] { (Button)form.AcceptButton, (Button)form.CancelButton })
                        Check(form.ClientRectangle.Contains(form.RectangleToClient(button.RectangleToScreen(button.ClientRectangle))), prefix + " keeps footer visible: " + button.Text);
                    Shot(form, name + "-" + index + "-bottom");
                }
                var pick = (Button)Field(form, "pickPortButton"); pick.PerformClick(); Application.DoEvents();
                Check((bool)Field(form, "pickFreePort") && !((NumericUpDown)Field(form, "httpPort")).Enabled, name + " port selection remains immediate and cancellable");
                Shot(form, name + "-port-pending"); pick.PerformClick();
                Check(!(bool)Field(form, "pickFreePort") && ((NumericUpDown)Field(form, "httpPort")).Value == 31881 && ((NumericUpDown)Field(form, "httpPort")).Enabled,
                    name + " cancelling port selection retains the edited manual value");
                Descendants(tabs.TabPages[4]).OfType<Button>().Single(b => b.Text == "Скопировать адрес").PerformClick();
                Check(Clipboard.GetText() == ((TextBox)Field(form, "proxyAddress")).Text && ((TextBox)Field(form, "proxyAddress")).ReadOnly,
                    name + " copy keeps the read-only effective proxy address"); Clipboard.Clear();
                AppSettings captured = null;
                form.SaveRequested = delegate(AppSettings candidate, bool choose) { captured = candidate; return new SettingsSaveError(SettingsField.SettingsFile, "Проверка сохранения: изменения оставлены в окне."); };
                ((Button)form.AcceptButton).PerformClick(); Application.DoEvents();
                Check(captured != null && captured.SocksHost == "pending.example.org" && captured.SocksPort == 19998 && captured.ClipboardClearSeconds == 59 &&
                    captured.TestEndpoint == "https://example.org/check?fixture=long-pending-diagnostic-endpoint" && captured.HttpProxyPort == 31881 && !captured.AutoHttpProxyPort,
                    name + " tab changes and reflow retain all edited values for Save");
                Check(bytes.SequenceEqual(File.ReadAllBytes(AppPaths.SettingsPath)) && form.Visible, name + " refused Save preserves settings bytes and editable form");
                Shot(form, name + "-save-error"); form.Close();
            }
        }
    }
}

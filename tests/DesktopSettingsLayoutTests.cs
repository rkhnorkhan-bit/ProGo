using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        private static void SettingsAutomationLayout(SettingsService settings)
        {
            SettingsAutomationLayoutCase(settings, 1f, new Size(900, 700), false, "settings-layout-normal");
            SettingsAutomationLayoutCase(settings, 1f, new Size(834, 611), false, "settings-layout-minimum");
            SettingsAutomationLayoutCase(settings, 1f, new Size(1320, 690), true, "settings-layout-1366-long");
            foreach (float scale in new[] { 1.25f, 1.5f, 2f })
                SettingsAutomationLayoutCase(settings, scale, new Size(900, 700), true,
                    "settings-layout-scale-" + (int)(scale * 100));
            using (var form = new SshProfilesSettingsForm(settings)) {
                form.Show(); Application.DoEvents();
                var toggle = (CheckBox)Field(form, "autoCli");
                var card = toggle.Parent.Parent;
                DashboardFixtureSize(form, new Size(650, 610)); Application.DoEvents();
                int narrowHeight = card.Height;
                DashboardFixtureSize(form, new Size(1320, 690)); Application.DoEvents();
                Check(card.Height <= narrowHeight && Descendants(form).OfType<CheckBox>().Count(c => c == toggle) == 1,
                    "settings widening reflows existing automation controls without duplication");
                form.Close();
            }
        }
        private static void SettingsAutomationLayoutCase(SettingsService settings, float scale, Size available, bool longText, string name)
        {
            var bytes = File.ReadAllBytes(AppPaths.SettingsPath);
            using (var form = new SshProfilesSettingsForm(settings)) {
                form.Show(); Application.DoEvents();
                ((Timer)Field(form, "currentValuesTimer")).Stop();
                // Constrained synthetic scaling is a stress test, not actual Windows DPI coverage.
                if (scale != 1f) form.Scale(new SizeF(scale, scale));
                DashboardFixtureSize(form, available);
                var tabs = Descendants(form).OfType<TabControl>().Single();
                var flow = Descendants(tabs.TabPages[0]).OfType<FlowLayoutPanel>().First();
                if (longText) {
                    ((Label)Field(form, "currentAutomation")).Text = string.Join("\n", Enumerable.Repeat(
                        "Сохранено для ручных команд: SSH example-fixture · адрес приложений уточняется после подключения.", 3));
                    foreach (var card in flow.Controls.OfType<SurfacePanel>()) {
                        var hint = Descendants(card).OfType<Label>().First();
                        hint.Text += " Настройки сохраняются отдельно. Проверьте выбранное подключение и повторите команду после исправления причины.";
                    }
                    ((CheckBox)Field(form, "autoRestart")).Text += " и повторять попытку после проверки подключения";
                }
                Application.DoEvents(); form.PerformLayout(); Application.DoEvents();
                Check(form.ClientSize == available && DashboardNativeClient(form) == available, name + " uses the actual requested client area");
                Check(!flow.HorizontalScroll.Visible && flow.Controls.Cast<Control>().All(c => c.Right <= flow.ClientSize.Width),
                    name + " fits automation content without horizontal scrolling: viewport=" + flow.ClientSize + " display=" + flow.DisplayRectangle);
                var ordered = flow.Controls.Cast<Control>().OrderBy(c => c.Top).ToArray();
                Check(ordered.Zip(ordered.Skip(1), (a, b) => a.Bottom <= b.Top).All(v => v), name + " separates preferences and cards vertically");
                foreach (var card in flow.Controls.OfType<SurfacePanel>()) {
                    var stack = Descendants(card).OfType<TableLayoutPanel>().Single();
                    var rows = stack.Controls.Cast<Control>().OrderBy(c => c.Top).ToArray();
                    Check(rows.Zip(rows.Skip(1), (a, b) => a.Bottom <= b.Top).All(v => v) &&
                        rows.All(c => c.Right <= stack.ClientSize.Width && c.Bottom <= stack.ClientSize.Height), name + " keeps card text and actions in separate fitting rows");
                    Check(Descendants(card).OfType<Label>().All(l => l.AutoSize && !l.AutoEllipsis &&
                        l.GetPreferredSize(new Size(l.Width, 0)).Height <= l.Height), name + " gives complete wrapped hints enough height");
                    var toggle = Descendants(card).OfType<CheckBox>().Single();
                    var textSize = TextRenderer.MeasureText(toggle.Text, toggle.Font,
                        new Size(Math.Max(1, toggle.ClientSize.Width - SystemInformation.MenuCheckSize.Width - 8), int.MaxValue), TextFormatFlags.WordBreak);
                    Check(textSize.Height <= toggle.ClientSize.Height, name + " gives the complete wrapped preference caption enough height: text=" + textSize + " control=" + toggle.ClientSize);
                }
                var root = (TableLayoutPanel)tabs.Parent;
                Check(root.Controls.OfType<Label>().Where(l => l.Visible).All(l => l.GetPreferredSize(new Size(l.Width, 0)).Height <= l.Height),
                    name + " wraps the explanatory header instead of clipping it: " + string.Join("; ", root.Controls.OfType<Label>().Where(l => l.Visible).Select(l => l.Bounds + " preferred=" + l.GetPreferredSize(new Size(l.Width, 0)))));
                Check(Enumerable.Range(0, tabs.TabCount).All(i => tabs.ClientRectangle.Contains(tabs.GetTabRect(i))),
                    name + " exposes all five tab headers with wrapped tab rows");
                foreach (var button in new[] { (Button)form.AcceptButton, (Button)form.CancelButton })
                    Check(form.ClientRectangle.Contains(form.RectangleToClient(button.RectangleToScreen(button.ClientRectangle))), name + " keeps footer visible: " + button.Text);
                Shot(form, name);
                string action = null; int count = 0;
                form.ManualActionRequested += delegate(string value) { action = value; count++; };
                var host = (TextBox)Field(form, "host"); host.Text = "pending.example.org";
                var option = (CheckBox)Field(form, "autoCli"); option.Checked = !option.Checked;
                bool pendingOption = option.Checked;
                var manual = flow.Controls.OfType<SurfacePanel>().SelectMany(c => Descendants(c).OfType<Button>()).ToArray();
                string[] expected = { "restart", "stop", "cli-start", "cli-off", "windows-on", "windows-off" };
                for (int i = 0; i < manual.Length; i++) {
                    flow.ScrollControlIntoView(manual[i]); Application.DoEvents();
                    var area = flow.RectangleToClient(manual[i].RectangleToScreen(manual[i].ClientRectangle));
                    Check(flow.ClientRectangle.Contains(area), name + " reaches manual command by vertical scrolling: " + expected[i] + " bounds=" + area);
                    manual[i].PerformClick(); Application.DoEvents();
                    Check(action == expected[i] && count == i + 1 && host.Text == "pending.example.org" && option.Checked == pendingOption,
                        name + " retains exact command routing and uncommitted edits: " + expected[i]);
                }
                Check(manual.Length == expected.Length && bytes.SequenceEqual(File.ReadAllBytes(AppPaths.SettingsPath)), name + " layout and manual controls never save preferences");
                Shot(form, name + "-commands");
                form.SaveRequested = delegate { return new SettingsSaveError(SettingsField.SettingsFile,
                    "Не удалось сохранить файл настроек. Исправьте причину и повторите сохранение. Все изменения доступны в этом окне."); };
                ((CheckBox)Field(form, "autoStart")).Checked = false;
                ((CheckBox)Field(form, "autoSwitchProfile")).Checked = false;
                ((TextBox)Field(form, "endpoint")).Text = "https://example.org/check";
                ((Button)form.AcceptButton).PerformClick(); Application.DoEvents();
                var error = (Label)Field(form, "saveError");
                Check(error.Visible && form.ClientRectangle.Contains(form.RectangleToClient(error.RectangleToScreen(error.ClientRectangle))) &&
                    error.GetPreferredSize(new Size(error.Width, 0)).Height <= error.Height &&
                    form.ClientRectangle.Contains(form.RectangleToClient(((Button)form.AcceptButton).RectangleToScreen(((Button)form.AcceptButton).ClientRectangle))),
                    name + " keeps the wrapped Save error and retry button inside the actual window");
                Shot(form, name + "-error");
                form.Close();
            }
        }
    }
}

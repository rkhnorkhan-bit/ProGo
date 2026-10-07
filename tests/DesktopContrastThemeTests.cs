using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        private static void ContrastThemes(SettingsService settings)
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") { Console.WriteLine("SKIP: contrast fixtures require isolated native CI"); return; }
            var source = typeof(UiTheme).GetField("contrastSource", BindingFlags.Static | BindingFlags.NonPublic);
            var original = source.GetValue(null); bool contrast = false;
            var bytes = File.ReadAllBytes(AppPaths.SettingsPath);
            source.SetValue(null, (Func<bool>)(() => contrast));
            try {
                using (var proxy = new ProxyService(settings))
                using (var relay = new Ikev2RelayService())
                using (var home = new HomeVpnService(relay))
                using (var dashboard = new MainWindow(settings, proxy, home, delegate(AppCommand route) { Check(route == AppCommand.StartCli, "theme changes preserve ordinary CLI routing"); }))
                using (var form = new SshProfilesSettingsForm(settings)) {
                    dashboard.Show(); form.Show(); Application.DoEvents();
                    ((Timer)Field(form, "currentValuesTimer")).Stop();
                    ((Timer)Field(dashboard, "timer")).Stop();
                    ((TextBox)Field(form, "host")).Text = "pending.example.org";
                    ((NumericUpDown)Field(form, "httpPort")).Value = 31881;
                    form.SaveRequested = delegate { return new SettingsSaveError(SettingsField.SettingsFile, "Ошибка сохранения: изменения оставлены в окне."); };
                    ((Button)form.AcceptButton).PerformClick();
                    var error = (Label)Field(form, "saveError");
                    Check(error.ForeColor == UiTheme.Error && error.Visible, "normal theme exposes a textual save error");
                    string windowsPurpose = ((Button)Field(dashboard, "windowsToggle")).AccessibilityObject.Name;
                    contrast = true; ContrastNotify(form); ContrastNotify(dashboard);
                    Check(((Button)Field(dashboard, "windowsToggle")).AccessibilityObject.Name == windowsPurpose && windowsPurpose.Contains("прокси Windows"),
                        "live contrast changes preserve the explicit Windows proxy action name");
                    ContrastCheck(form, "settings"); ContrastCheck(dashboard, "dashboard");
                    Check(error.Text.Contains("Ошибка сохранения") && error.ForeColor == SystemColors.WindowText, "contrast error retains its message with readable system text");
                    var tabs = Descendants(form).OfType<TabControl>().Single();
                    for (int i = 0; i < tabs.TabCount; i++) { tabs.SelectedIndex = i; Application.DoEvents(); Shot(form, "contrast-settings-" + i); }
                    Shot(dashboard, "contrast-dashboard");
                    Descendants(dashboard).OfType<Button>().Single(b => b.Text == "Запустить CLI").PerformClick();
                    Check(((TextBox)Field(form, "host")).Text == "pending.example.org" && ((NumericUpDown)Field(form, "httpPort")).Value == 31881,
                        "live contrast refresh preserves pending fields");
                    contrast = false; ContrastNotify(form); ContrastNotify(dashboard);
                    Check(form.BackColor == UiTheme.Background && Descendants(form).OfType<SurfacePanel>().All(p => p.BackColor == UiTheme.Surface), "leaving contrast restores dark forms and surfaces");
                    Check(error.ForeColor == UiTheme.Error && Descendants(form).OfType<ComboBox>().All(c => c.DrawMode == DrawMode.OwnerDrawFixed), "leaving contrast restores error semantics and themed selectors");
                    Check(Descendants(form).OfType<Button>().Where(b => Equals(b.Tag, "primary")).All(b => b.BackColor == UiTheme.Accent), "leaving contrast restores primary actions");
                    Shot(form, "contrast-settings-restored");
                    var observer = ContrastObserver(form);
                    contrast = true; ContrastQueue(observer); form.Close(); Application.DoEvents();
                    Check((bool)observer.GetType().GetField("disposed", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(observer), "closing a window releases its system preference subscription and queued refresh");
                }
                contrast = true;
                // Startup and custom rendering also use the palette before a window is loaded.
                using (var fixture = new ProGoForm { Text = "Проверка контрастной темы", ClientSize = new Size(700, 560) })
                using (var menu = new ContextMenuStrip()) {
                    var label = UiTheme.Label("Состояние: ошибка проверки", UiTheme.Body, UiTheme.Error);
                    var surface = new SurfacePanel { Dock = DockStyle.Top, Height = 160 };
                    var stack = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown };
                    surface.Controls.Add(stack); stack.Controls.Add(label);
                    var combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList }; combo.Items.Add("Сервер для проверки"); combo.SelectedIndex = 0; stack.Controls.Add(combo);
                    var link = new LinkLabel { Text = "Настройки Windows", AutoSize = true }; stack.Controls.Add(link);
                    var progress = new WizardProgress { Dock = DockStyle.Top, Step = 2 };
                    var grid = new DataGridView { Dock = DockStyle.Fill }; grid.Columns.Add("name", "Название"); grid.Rows.Add("Пример");
                    fixture.Controls.Add(grid); fixture.Controls.Add(progress); fixture.Controls.Add(surface);
                    Check(fixture.BackColor == SystemColors.Window && surface.BackColor == SystemColors.Window && label.ForeColor == SystemColors.WindowText,
                        "constructors select system colors before Load");
                    fixture.Show(); Application.DoEvents(); ContrastCheck(fixture, "custom controls");
                    Check(grid.DefaultCellStyle.SelectionBackColor == SystemColors.Highlight && grid.DefaultCellStyle.SelectionForeColor == SystemColors.HighlightText,
                        "grid selection uses the system highlight pair");
                    Check(link.LinkColor == SystemColors.HotTrack && combo.DrawMode == DrawMode.Normal, "contrast links and selectors use system rendering");
                    using (var bitmap = new Bitmap(surface.Width, surface.Height)) {
                        surface.DrawToBitmap(bitmap, surface.ClientRectangle);
                        Check(bitmap.GetPixel(0, surface.Height - 2).ToArgb() == SystemColors.WindowText.ToArgb(), "custom surface draws its contrast border in system text color");
                    }
                    using (var bitmap = new Bitmap(progress.Width, progress.Height)) {
                        progress.DrawToBitmap(bitmap, progress.ClientRectangle);
                        Check(bitmap.GetPixel(5, 17).ToArgb() == SystemColors.Highlight.ToArgb(), "wizard progress draws its active step with the system highlight");
                    }
                    label.ForeColor = UiTheme.Accent;
                    Check(label.ForeColor == SystemColors.WindowText, "a live status update cannot reintroduce a dark-theme color in contrast mode");
                    menu.Items.Add("Подключение"); var parent = new ToolStripMenuItem("Приложения"); parent.DropDownItems.Add("Запустить CLI"); menu.Items.Add(parent);
                    UiTheme.Menu(menu); menu.Show(fixture, new Point(400, 30)); Application.DoEvents();
                    Check(menu.Renderer is ToolStripSystemRenderer && parent.DropDown.Renderer is ToolStripSystemRenderer && parent.ForeColor == SystemColors.MenuText,
                        "tray menu and nested commands use the system renderer");
                    Shot(fixture, "contrast-custom-controls"); menu.Close();
                    contrast = false; ContrastNotify(fixture); menu.Show(fixture, new Point(400, 30)); Application.DoEvents();
                    Check(label.ForeColor == UiTheme.Accent && menu.Renderer is ToolStripProfessionalRenderer && parent.ForeColor == UiTheme.Text,
                        "theme return preserves the latest status color and restores menu styling");
                    menu.Close(); fixture.Close();
                }
                Check(File.ReadAllBytes(AppPaths.SettingsPath).SequenceEqual(bytes), "theme transitions never persist settings");
            } finally { source.SetValue(null, original); }
        }
        private static object ContrastObserver(ProGoForm form)
        {
            return typeof(ProGoForm).GetField("themeObservation", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(form);
        }
        private static void ContrastQueue(object observer)
        {
            var notify = observer.GetType().GetMethod("Changed", BindingFlags.Instance | BindingFlags.NonPublic);
            Check(Task.Run(() => notify.Invoke(observer, new object[] { null, new UserPreferenceChangedEventArgs(UserPreferenceCategory.Accessibility) })).Wait(3000),
                "system preference notification queues refresh without blocking on the UI thread");
        }
        private static void ContrastNotify(ProGoForm form)
        {
            ContrastQueue(ContrastObserver(form)); Application.DoEvents();
        }
        private static void ContrastCheck(ProGoForm form, string name)
        {
            var controls = Descendants(form).ToArray();
            Check(form.BackColor == SystemColors.Window && controls.OfType<Panel>().All(p => p.BackColor == SystemColors.Window), name + " has no dark surfaces in contrast mode");
            Check(controls.OfType<Label>().All(l => l.ForeColor == SystemColors.WindowText), name + " labels remain readable in contrast mode");
            Check(controls.OfType<Button>().All(b => b.FlatStyle == FlatStyle.Standard && b.UseVisualStyleBackColor && b.ForeColor == SystemColors.ControlText), name + " buttons use native contrast rendering");
            Check(controls.OfType<TextBoxBase>().All(b => b.BackColor == SystemColors.Window && b.ForeColor == SystemColors.WindowText), name + " fields use the system text/background pair");
        }
    }
}

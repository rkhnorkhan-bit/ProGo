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
                    Check(Descendants(form).OfType<Button>().Where(b => Equals(b.Tag, "primary")).All(b => b.BackColor == (b.Enabled ? UiTheme.Accent : UiTheme.Field)), "leaving contrast restores enabled primary accents and keeps disabled primary actions subdued");
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
                contrast = false;
                DisabledButtonRendering(value => contrast = value);
                Check(File.ReadAllBytes(AppPaths.SettingsPath).SequenceEqual(bytes), "theme transitions never persist settings");
            } finally { source.SetValue(null, original); }
        }
        private static void DisabledButtonRendering(Action<bool> setContrast)
        {
            using (var form = new ProGoForm { Text = "Выключенные действия", ClientSize = new Size(680, 280) })
            using (var image = new Bitmap(16, 16)) {
                using (var graphics = Graphics.FromImage(image)) graphics.Clear(Color.Magenta);
                var primary = UiTheme.Button("Открыть", null, true);
                var secondary = UiTheme.Button("Сохранить", null, false);
                var cancel = UiTheme.Button("Отмена", null, false, DialogResult.Cancel);
                var imageButton = new Button { Text = "С изображением", Image = image, ImageAlign = ContentAlignment.MiddleLeft,
                    TextImageRelation = TextImageRelation.ImageBeforeText, Bounds = new Rectangle(30, 170, 280, 60) };
                primary.AutoSize = secondary.AutoSize = cancel.AutoSize = false;
                primary.Bounds = new Rectangle(30, 30, 280, 50); secondary.Bounds = new Rectangle(350, 30, 280, 50);
                cancel.Bounds = new Rectangle(350, 170, 280, 60);
                form.Controls.Add(primary); form.Controls.Add(secondary); form.Controls.Add(cancel); form.Controls.Add(imageButton);
                form.AcceptButton = primary; form.CancelButton = cancel;
                primary.AccessibleName = "Проверка PIN"; primary.AccessibleDescription = "Открытие после проверки.";
                int clicks = 0; primary.Click += delegate { clicks++; };
                form.Show(); Application.DoEvents(); cancel.Focus();
                Check(primary.GetType() == typeof(Button) && primary.BackColor == UiTheme.Accent, "the primary action remains a native Button with the enabled accent");
                primary.Enabled = secondary.Enabled = imageButton.Enabled = false; Application.DoEvents();
                Check(primary.BackColor == UiTheme.Field && primary.Cursor == Cursors.Default && primary.FlatAppearance.BorderSize == 1,
                    "direct disabling removes the primary accent and interactive cursor");
                Check(secondary.BackColor == UiTheme.Field && secondary.ForeColor == UiTheme.Muted, "secondary disabled actions select the subdued palette");
                Check((primary.AccessibilityObject.State & AccessibleStates.Unavailable) != 0 && primary.AccessibilityObject.Name == "Проверка PIN" && primary.AccessibleDescription == "Открытие после проверки.",
                    "disabled styling preserves the native unavailable state and explicit accessible purpose");
                primary.PerformClick(); Check(clicks == 0, "a disabled primary action cannot execute through PerformClick");
                Check(ReferenceEquals(form.AcceptButton, primary) && ReferenceEquals(form.CancelButton, cancel) && cancel.DialogResult == DialogResult.Cancel && primary.TabStop,
                    "styling retains native accept/cancel roles and the declared tab stop");
                using (var rendered = ButtonBitmap(primary)) CheckReadableDisabledGlyph(rendered, primary.BackColor, "primary");
                using (var rendered = ButtonBitmap(secondary)) CheckReadableDisabledGlyph(rendered, secondary.BackColor, "secondary Save");
                using (var reference = NativeButtonReference(imageButton))
                using (var expected = ButtonBitmap(reference))
                using (var actual = ButtonBitmap(imageButton)) Check(SameButtonPixels(actual, expected, "disabled-image-reference"),
                    "image buttons retain the complete native disabled image/text layout and rendering");
                Shot(form, "contrast-disabled-dark-actions");
                primary.Enabled = true; Application.DoEvents(); primary.PerformClick();
                Check(primary.BackColor == UiTheme.Accent && primary.ForeColor == UiTheme.Background && primary.Cursor == Cursors.Hand && clicks == 1,
                    "reenabling restores the enabled primary palette and action exactly once");
                primary.Enabled = false;
                for (int i = 0; i < 3; i++) UiTheme.Apply(form);
                setContrast(true); ContrastNotify(form); ContrastCheck(form, "disabled actions");
                Check(!primary.Enabled && primary.FlatStyle == FlatStyle.Standard && primary.UseVisualStyleBackColor, "disabled High Contrast actions use the native standard renderer");
                using (var reference = NativeButtonReference(primary))
                using (var expected = ButtonBitmap(reference))
                using (var actual = ButtonBitmap(primary)) Check(SameButtonPixels(actual, expected, "disabled-system-reference"), "the contrast branch leaves disabled native glyphs and border unchanged");
                primary.Enabled = true;
                Check(primary.FlatStyle == FlatStyle.Standard && primary.BackColor == SystemColors.Control, "enabled changes within the contrast branch keep the system palette");
                primary.Enabled = false; Shot(form, "contrast-disabled-system-actions");
                setContrast(false); ContrastNotify(form);
                Check(!primary.Enabled && primary.BackColor == UiTheme.Field && primary.FlatStyle == FlatStyle.Flat, "theme return keeps a pending primary action subdued");
                using (var rendered = ButtonBitmap(primary)) CheckReadableDisabledGlyph(rendered, primary.BackColor, "primary after theme return");
                using (var rendered = ButtonBitmap(secondary)) CheckReadableDisabledGlyph(rendered, secondary.BackColor, "Save after theme return");
                primary.Text = "Save"; primary.TextAlign = ContentAlignment.TopLeft; primary.Padding = new Padding(15, 2, 5, 7);
                Rectangle left;
                using (var rendered = ButtonBitmap(primary)) left = GlyphBounds(rendered, primary.BackColor);
                primary.TextAlign = ContentAlignment.BottomRight;
                using (var rendered = ButtonBitmap(primary)) {
                    var right = GlyphBounds(rendered, primary.BackColor);
                    Check(!left.IsEmpty && !right.IsEmpty && right.Left > left.Right && right.Top > left.Top,
                        "disabled captions preserve padded horizontal and vertical text alignment");
                }
                primary.RightToLeft = RightToLeft.Yes;
                using (var rendered = ButtonBitmap(primary)) Check(GlyphBounds(rendered, primary.BackColor).Left < primary.Width / 2, "disabled RTL captions mirror the declared text alignment");
                primary.RightToLeft = RightToLeft.No; primary.TextAlign = ContentAlignment.MiddleCenter; primary.Padding = UiTheme.ActionPadding;
                primary.Text = "&Save"; primary.UseMnemonic = true;
                int mnemonicWidth;
                using (var rendered = ButtonBitmap(primary)) mnemonicWidth = GlyphBounds(rendered, primary.BackColor).Width;
                primary.UseMnemonic = false;
                using (var rendered = ButtonBitmap(primary)) Check(GlyphBounds(rendered, primary.BackColor).Width > mnemonicWidth, "disabled captions honor UseMnemonic rather than always drawing a literal ampersand");
                primary.UseCompatibleTextRendering = true;
                using (var rendered = ButtonBitmap(primary)) CheckReadableDisabledGlyph(rendered, primary.BackColor, "compatible GDI+ caption");
                primary.UseCompatibleTextRendering = false; primary.UseMnemonic = true; primary.Text = "Открыть";
                Shot(form, "contrast-disabled-dark-restored");
                primary.Enabled = true; primary.PerformClick();
                Check(clicks == 2 && primary.BackColor == UiTheme.Accent && primary.AccessibilityObject.Name == "Проверка PIN",
                    "repeated Apply and theme changes do not duplicate handlers or replace native purpose");
                form.Close();
            }
        }
        private static Bitmap ButtonBitmap(Button button)
        {
            var bitmap = new Bitmap(button.Width, button.Height);
            button.DrawToBitmap(bitmap, button.ClientRectangle); return bitmap;
        }
        private static void CheckReadableDisabledGlyph(Bitmap bitmap, Color background, string name)
        {
            var glyphs = GlyphBounds(bitmap, background);
            Check(!glyphs.IsEmpty && glyphs.Width > 8 && glyphs.Height > 5, "native rendered " + name + " has readable disabled glyph cores at contrast >= 4.5");
        }
        private static Rectangle GlyphBounds(Bitmap bitmap, Color background)
        {
            int left = bitmap.Width, top = bitmap.Height, right = -1, bottom = -1, cores = 0;
            for (int y = 5; y < bitmap.Height - 5; y++) for (int x = 5; x < bitmap.Width - 5; x++) {
                var color = bitmap.GetPixel(x, y);
                if (VisualLuminance(color) <= VisualLuminance(background) || VisualContrast(color, background) < 4.5) continue;
                cores++; left = Math.Min(left, x); right = Math.Max(right, x); top = Math.Min(top, y); bottom = Math.Max(bottom, y);
            }
            return cores < 12 ? Rectangle.Empty : Rectangle.FromLTRB(left, top, right + 1, bottom + 1);
        }
        private static Button NativeButtonReference(Button source)
        {
            var result = new Button { Text = source.Text, Font = source.Font, Size = source.Size, Padding = source.Padding,
                FlatStyle = source.FlatStyle, BackColor = source.BackColor, ForeColor = source.ForeColor,
                TextAlign = source.TextAlign, Image = source.Image, ImageAlign = source.ImageAlign, TextImageRelation = source.TextImageRelation,
                RightToLeft = source.RightToLeft, UseMnemonic = source.UseMnemonic, UseCompatibleTextRendering = source.UseCompatibleTextRendering,
                UseVisualStyleBackColor = source.UseVisualStyleBackColor, Enabled = source.Enabled };
            result.FlatAppearance.BorderSize = source.FlatAppearance.BorderSize; result.FlatAppearance.BorderColor = source.FlatAppearance.BorderColor;
            result.FlatAppearance.MouseOverBackColor = source.FlatAppearance.MouseOverBackColor;
            result.FlatAppearance.MouseDownBackColor = source.FlatAppearance.MouseDownBackColor;
            // Standard themed buttons paint transparent border/corner pixels from
            // their parent. A parked standalone HWND has a different native background.
            // Match the complete native context rather than cropping those pixels away.
            if (source.Parent == null) throw new InvalidOperationException("The native button fixture must have a parent.");
            result.Bounds = source.Bounds; source.Parent.Controls.Add(result);
            var isDefault = typeof(ButtonBase).GetProperty("IsDefault", BindingFlags.Instance | BindingFlags.NonPublic);
            ((IButtonControl)result).NotifyDefault((bool)isDefault.GetValue(source, null)); result.CreateControl();
            Console.WriteLine("Native button reference: sameParent=" + ReferenceEquals(result.Parent, source.Parent) +
                "; bounds=" + result.Bounds + "; parentBackground=" + result.Parent.BackColor.ToArgb() + "; default=" + isDefault.GetValue(source, null));
            return result;
        }
        private static bool SameButtonPixels(Bitmap left, Bitmap right, string name)
        {
            if (left.Size != right.Size) { Console.WriteLine("Native button pixels " + name + ": size mismatch " + left.Size + "/" + right.Size); return false; }
            int mismatches = 0, edgeMismatches = 0; Point first = Point.Empty; Color actual = Color.Empty, expected = Color.Empty;
            for (int y = 0; y < left.Height; y++) for (int x = 0; x < left.Width; x++) {
                var a = left.GetPixel(x, y); var b = right.GetPixel(x, y);
                if (a.ToArgb() == b.ToArgb()) continue;
                if (mismatches == 0) { first = new Point(x, y); actual = a; expected = b; }
                mismatches++;
                if (x < 2 || y < 2 || x >= left.Width - 2 || y >= left.Height - 2) edgeMismatches++;
            }
            Console.WriteLine("Native button pixels " + name + ": mismatch=" + mismatches + "; edge=" + edgeMismatches +
                "; first=" + first + "; actual=" + actual.ToArgb() + "; expected=" + expected.ToArgb());
            if (mismatches != 0) { left.Save(Path.Combine(work, name + "-actual.png")); right.Save(Path.Combine(work, name + "-expected.png")); }
            return mismatches == 0;
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

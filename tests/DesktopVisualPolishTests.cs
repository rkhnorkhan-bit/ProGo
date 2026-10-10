using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        private static void VisualPolish(SettingsService settings)
        {
            SmallBrandIcons();
            VisualActionMetrics(settings);
            VaultEmptyStates(settings);
        }
        private static void SmallBrandIcons()
        {
            // Read the actual build input, including native 16/24 px PNG frames.
            string path = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "ProGo.ico");
            using (var stream = File.OpenRead(path))
            using (var reader = new BinaryReader(stream)) {
                Check(reader.ReadUInt16() == 0 && reader.ReadUInt16() == 1, "built icon has a valid Windows ICO header");
                int count = reader.ReadUInt16(); Check(count == 7, "built icon retains all seven resolutions");
                var sizes = new int[count]; var lengths = new int[count]; var offsets = new int[count];
                for (int i = 0; i < count; i++) {
                    int width = reader.ReadByte(); int height = reader.ReadByte();
                    sizes[i] = width == 0 ? 256 : width;
                    Check(sizes[i] == (height == 0 ? 256 : height), "built icon frame is square " + sizes[i]);
                    reader.ReadByte(); reader.ReadByte(); reader.ReadUInt16();
                    Check(reader.ReadUInt16() == 32, "built icon keeps 32-bit color/alpha " + sizes[i]);
                    lengths[i] = reader.ReadInt32(); offsets[i] = reader.ReadInt32();
                }
                Check(sizes.SequenceEqual(new[] { 16, 24, 32, 48, 64, 128, 256 }), "built icon includes real 16/24 px frames rather than relying on resizing");
                for (int i = 0; i < count; i++) {
                    Check(lengths[i] > 0 && offsets[i] >= 6 + 16 * count && offsets[i] + lengths[i] <= stream.Length &&
                        (i == 0 || offsets[i] == offsets[i - 1] + lengths[i - 1]), "built icon frame is bounded and disjoint " + sizes[i]);
                    stream.Position = offsets[i];
                    using (var frame = new MemoryStream(reader.ReadBytes(lengths[i])))
                    using (var image = new Bitmap(frame)) {
                        Check(image.Width == sizes[i] && image.Height == sizes[i] && image.GetPixel(0, 0).A == 0,
                            "built icon PNG has its declared size and transparent corners " + sizes[i]);
                    }
                }
            }
            using (var sheet = new Bitmap(560, 320))
            using (var graphics = Graphics.FromImage(sheet)) {
                graphics.Clear(Color.FromArgb(238, 241, 245));
                for (int row = 0; row < 2; row++) {
                    var background = row == 0 ? Color.White : Color.FromArgb(30, 30, 30);
                    for (int column = 0; column < 2; column++) {
                        int size = column == 0 ? 16 : 24;
                        var cell = new Rectangle(column * 280, row * 160, 280, 160);
                        using (var brush = new SolidBrush(background)) graphics.FillRectangle(brush, cell);
                        using (var icon = BrandIcon.Create(size))
                        using (var native = icon.ToBitmap()) {
                            Check(icon.Size == new Size(size, size) && native.Size == icon.Size, "native HICON uses requested pixel size " + size);
                            int opaque = 0, mark = 0, contrasting = 0;
                            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++) {
                                var color = native.GetPixel(x, y);
                                if (color.A < 128) continue;
                                opaque++;
                                if (color.G > color.R + 30 && color.G > color.B + 20) mark++;
                                if (VisualContrast(color, background) >= 3) contrasting++;
                            }
                            Check(native.GetPixel(0, 0).A == 0 && opaque > size * size / 2 && mark >= size / 2,
                                "native icon retains transparent outline and recognizable colored mark " + size + "/" + row);
                            Check(contrasting >= size * size / 20, "native icon retains contrasting visible pixels on light/dark background " + size + "/" + row);
                            TextRenderer.DrawText(graphics, size + " px · " + (row == 0 ? "светлый фон" : "тёмный фон"), UiTheme.Body,
                                new Point(cell.X + 14, cell.Y + 10), row == 0 ? Color.Black : Color.White);
                            graphics.DrawImageUnscaled(native, cell.X + 22, cell.Y + 65);
                            // The enlarged copy is nearest-neighbor; the native copy above is never resized.
                            graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                            graphics.PixelOffsetMode = PixelOffsetMode.Half;
                            graphics.DrawImage(native, new Rectangle(cell.X + 120, cell.Y + 40, size * 4, size * 4),
                                new Rectangle(0, 0, size, size), GraphicsUnit.Pixel);
                        }
                    }
                }
                sheet.Save(Path.Combine(work, "brand-icons-16-24-light-dark.png"), ImageFormat.Png);
            }
        }
        private static double VisualContrast(Color left, Color right)
        {
            double a = VisualLuminance(left), b = VisualLuminance(right);
            return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
        }
        private static double VisualLuminance(Color value)
        { return 0.2126 * VisualChannel(value.R) + 0.7152 * VisualChannel(value.G) + 0.0722 * VisualChannel(value.B); }
        private static double VisualChannel(byte value)
        { double c = value / 255.0; return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4); }

        private static void VisualActionMetrics(SettingsService settings)
        {
            using (var proxy = new ProxyService(() => settings.Current, delegate { }, "unused-visual-fixture", () => DateTime.UtcNow, false)) {
                var forms = new ProGoForm[] {
                    new EntryForm(VaultEntry.New()), new PinForm(true), new SshProfileEditorForm(null),
                    new StatusForm(settings, proxy, false, (s,p,t) => "Fixture", null, null,
                        (s,t) => null, (s,t) => Tuple.Create((double?)null, (string)null))
                };
                try {
                    for (int i = 0; i < forms.Length; i++) {
                        var form = forms[i]; form.Show(); Application.DoEvents();
                        form.Size = form.MinimumSize; Application.DoEvents();
                        var actions = Descendants(form).OfType<Button>().Where(b => b.Dock == DockStyle.None).ToArray();
                        Check(actions.Length >= 2 && actions.All(b => b.MinimumSize.Height == UiTheme.ActionHeight && b.Padding == UiTheme.ActionPadding && b.Margin == UiTheme.ActionMargin),
                            "legacy dialog actions share standard height/padding/margins " + form.Text);
                        foreach (var button in actions) {
                            var rectangle = form.RectangleToClient(button.RectangleToScreen(button.ClientRectangle));
                            Check(form.ClientRectangle.Contains(rectangle), "standard action remains visible at dialog minimum " + form.Text + "/" + button.Text);
                            var textArea = new Size(Math.Max(1, button.ClientSize.Width - button.Padding.Horizontal), Int32.MaxValue);
                            Check(TextRenderer.MeasureText(button.Text, button.Font, textArea, TextFormatFlags.WordBreak).Height <= button.ClientSize.Height - button.Padding.Vertical,
                                "standard action caption fits without clipping " + form.Text + "/" + button.Text);
                        }
                        var bounds = actions.Select(b => b.Bounds).ToArray(); UiTheme.Apply(form); Application.DoEvents();
                        Check(actions.Select(b => b.Bounds).SequenceEqual(bounds), "palette refresh preserves established action layout " + form.Text);
                        Shot(form, "polish-standard-actions-" + i); form.Close();
                    }
                } finally { foreach (var form in forms) form.Dispose(); }
            }
            using (var proxy = new ProxyService(() => settings.Current, delegate { }, "unused-visual-fixture", () => DateTime.UtcNow, false))
            using (var relay = new Ikev2RelayService())
            using (var home = new HomeVpnService(relay))
            using (var dashboard = new MainWindow(settings, proxy, home, delegate { }))
            using (var preferences = new SshProfilesSettingsForm(settings)) {
                dashboard.Show(); preferences.Show(); Application.DoEvents();
                Check(Descendants(dashboard).OfType<Label>().Any(l => l.Text == "Подключение" && l.Font == UiTheme.Heading) &&
                    Descendants(preferences).OfType<Label>().Any(l => l.Text == "Настройки" && l.Font == UiTheme.Title),
                    "window headings name the current task while preserving the brand");
                foreach (var field in new[] { "windowsToggle", "cliToggle" }) {
                    var compact = (Button)Field(dashboard, field);
                    Check(compact.MinimumSize.Height == UiTheme.CompactActionHeight && compact.Padding == new Padding(7, 0, 7, 0),
                        "compact dashboard card action keeps its intentional variant " + field);
                }
                Shot(dashboard, "polish-task-dashboard"); Shot(preferences, "polish-task-settings");
                dashboard.Close(); preferences.Close();
            }
        }
        private static void VaultEmptyStates(SettingsService settings)
        {
            using (var clipboard = new ClipboardService(settings)) {
                var data = VaultData.Empty(); var entry = VaultEntry.New(); entry.name = "Пример записи"; data.entries.Add(entry);
                var saved = new JavaScriptSerializer().Serialize(data);
                using (var form = new VaultForm(new VaultSession(data, "1234", false), clipboard, settings)) {
                    form.Show(); Application.DoEvents();
                    var search = (TextBox)Field(form, "search"); var type = (ComboBox)Field(form, "typeFilter");
                    var state = (Label)Field(form, "emptyState"); var grid = (DataGridView)Field(form, "grid");
                    Check(!state.Visible && grid.Rows.Count == 1, "populated vault hides the empty-state explanation");
                    search.Text = "нет такого имени"; Application.DoEvents();
                    Check(state.Visible && state.Text.StartsWith("Совпадений нет") && state.AccessibilityObject.Name == "Состояние списка хранилища" &&
                        state.AccessibilityObject.Description == state.Text && grid.AccessibilityObject.Description.Contains(state.Text),
                        "empty vault search exposes corrective native text instead of claiming the first record is missing");
                    Shot(form, "polish-vault-no-matches");
                    search.Text = ""; type.SelectedItem = "SSH"; Application.DoEvents();
                    Check(state.Visible && state.Text.StartsWith("Совпадений нет") && grid.Rows.Count == 0, "empty type filter uses the same corrective state");
                    type.SelectedIndex = 0; Application.DoEvents();
                    Check(!state.Visible && state.Text == "" && grid.Rows.Count == 1 && !grid.AccessibilityObject.Description.Contains("Совпадений"),
                        "clearing filters restores records and removes stale empty-state text");
                    Check(new JavaScriptSerializer().Serialize(data) == saved, "empty-state navigation never changes vault records"); form.Close();
                }
                using (var form = new VaultForm(new VaultSession(VaultData.Empty(), "1234", false), clipboard, settings)) {
                    form.Show(); Application.DoEvents(); form.Size = form.MinimumSize; Application.DoEvents();
                    var state = (Label)Field(form, "emptyState"); var grid = (DataGridView)Field(form, "grid");
                    Check(state.Visible && state.Text.StartsWith("Записей пока нет") && state.AccessibilityObject.Description == state.Text &&
                        grid.AccessibilityObject.Description.Contains(state.Text) && !state.Text.Contains("сохранить"),
                        "empty vault exposes neutral accessible guidance without a persistence promise");
                    Check(state.Parent.ClientRectangle.Contains(state.Bounds), "empty-vault explanation remains within its native container");
                    Shot(form, "polish-vault-empty"); form.Close();
                }
            }
        }
    }
}

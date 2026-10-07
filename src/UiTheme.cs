using System;
using System.Drawing;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ProGo
{
    // Native controls keep keyboard navigation, screen-reader names and DPI scaling.
    internal class ProGoForm : Form
    {
        private readonly Icon brand = BrandIcon.Create();
        private IDisposable themeObservation;
        public ProGoForm()
        {
            Font = UiTheme.Body;
            AutoScaleDimensions = new SizeF(96, 96);
            AutoScaleMode = AutoScaleMode.Dpi;
            Icon = brand;
            BackColor = UiTheme.WindowBackground;
            ForeColor = UiTheme.WindowText;
            StartPosition = FormStartPosition.CenterScreen;
            DoubleBuffered = true;
        }
        protected override void OnLoad(EventArgs e)
        {
            UiTheme.Apply(this);
            base.OnLoad(e);
        }
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (themeObservation == null) themeObservation = UiTheme.Observe(this, RefreshTheme);
            RefreshTheme();
        }
        private void RefreshTheme()
        {
            UiTheme.Apply(this);
            if (IsHandleCreated)
                try { int dark = UiTheme.HighContrast ? 0 : 1; DwmSetWindowAttribute(Handle, 20, ref dark, 4); } catch { }
            Invalidate(true);
        }
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr handle, int attribute, ref int value, int size);
        protected override void Dispose(bool disposing)
        {
            if (disposing) {
                if (themeObservation != null) { themeObservation.Dispose(); themeObservation = null; }
                brand.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    internal static class UiTheme
    {
        public static readonly Color Background = Color.FromArgb(12, 17, 27);
        public static readonly Color Surface = Color.FromArgb(22, 30, 44);
        public static readonly Color Field = Color.FromArgb(30, 40, 57);
        public static readonly Color Border = Color.FromArgb(50, 66, 86);
        public static readonly Color Text = Color.FromArgb(235, 241, 250);
        public static readonly Color Muted = Color.FromArgb(162, 179, 200);
        public static readonly Color Accent = Color.FromArgb(90, 232, 187);
        public static readonly Color Error = Color.FromArgb(255, 153, 156);
        public static readonly Font Body = new Font("Segoe UI", 10f);
        public static readonly Font Strong = new Font("Segoe UI", 10f, FontStyle.Bold);
        public static readonly Font Title = new Font("Segoe UI", 23f, FontStyle.Bold);
        public static readonly Font Heading = new Font("Segoe UI", 14f, FontStyle.Bold);

        // The source is private and always reads Windows in production. Tests replace it
        // only inside an isolated process; they never change the user's system theme.
        private static Func<bool> contrastSource = () => SystemInformation.HighContrast;
        internal static bool HighContrast { get { return contrastSource(); } }
        internal static Color WindowBackground { get { return HighContrast ? SystemColors.Window : Background; } }
        internal static Color WindowText { get { return HighContrast ? SystemColors.WindowText : Text; } }
        internal static Color SurfaceBackground { get { return HighContrast ? SystemColors.Window : Surface; } }
        internal static Color TextColor(Color normal) { return HighContrast ? SystemColors.WindowText : normal; }

        // Follow visual rows rather than construction order (some status rows are added later).
        // Reversed footer flows are traversed left-to-right; native input internals are untouched.
        internal static void ConfigureKeyboardOrder(Control parent)
        {
            var children = new List<Control>();
            foreach (Control child in parent.Controls) children.Add(child);
            var table = parent as TableLayoutPanel;
            if (table != null) children.Sort(delegate(Control a, Control b) {
                int row = table.GetRow(a).CompareTo(table.GetRow(b));
                return row != 0 ? row : table.GetColumn(a).CompareTo(table.GetColumn(b));
            });
            var flow = parent as FlowLayoutPanel;
            if (flow != null && flow.FlowDirection == FlowDirection.RightToLeft) children.Reverse();
            for (int i = 0; i < children.Count; i++) {
                var child = children[i]; child.TabIndex = i;
                if (child is Panel || child is TabControl || child is TabPage) ConfigureKeyboardOrder(child);
            }
        }

        internal static IDisposable Observe(Control owner, Action refresh)
        {
            return new PreferenceObservation(owner, refresh);
        }
        private sealed class PreferenceObservation : IDisposable
        {
            private readonly Control owner;
            private readonly Action refresh;
            private volatile bool disposed;
            internal PreferenceObservation(Control owner, Action refresh)
            {
                this.owner = owner; this.refresh = refresh;
                SystemEvents.UserPreferenceChanged += Changed;
            }
            private void Changed(object sender, UserPreferenceChangedEventArgs e)
            {
                if (disposed || owner.IsDisposed || !owner.IsHandleCreated) return;
                // SystemEvents may run off the UI thread. Do not block that thread,
                // and recheck disposal when the queued refresh reaches the window.
                try { owner.BeginInvoke((Action)delegate {
                    if (!disposed && !owner.IsDisposed && owner.IsHandleCreated) refresh();
                }); } catch (InvalidOperationException) { }
            }
            public void Dispose()
            {
                if (disposed) return;
                disposed = true; SystemEvents.UserPreferenceChanged -= Changed;
            }
        }
        private sealed class ThemeLabel : Label
        {
            private Color normalForeground = UiTheme.Text;
            public override Color ForeColor {
                get { return base.ForeColor; }
                set { normalForeground = value; RefreshPalette(); }
            }
            internal void RefreshPalette() { base.ForeColor = TextColor(normalForeground); BackColor = Color.Transparent; }
        }
        internal static Label StatusLabel(Color color) { return new ThemeLabel { ForeColor = color }; }
        public static Label Label(string text, Font font, Color color)
        {
            return new ThemeLabel { Text = text, Font = font, ForeColor = color, AutoSize = true, Tag = "styled", Margin = new Padding(0, 0, 0, 8) };
        }
        public static Button Button(string text, EventHandler click, bool primary)
        {
            var b = new Button { Text = text, AutoSize = true, MinimumSize = new Size(118, 38), Padding = new Padding(12, 4, 12, 4), Margin = new Padding(0, 0, 10, 8), Tag = primary ? "primary" : null };
            if (click != null) b.Click += click;
            StyleButton(b);
            return b;
        }
        public static void Apply(Control root)
        {
            ApplyControl(root);
            // Parents must receive their palette before panels inherit it.
            foreach (Control c in root.Controls) Apply(c);
            root.Invalidate();
        }
        private static void ApplyControl(Control root)
        {
            var label = root as ThemeLabel;
            if (label != null) { label.RefreshPalette(); return; }
            root.ForeColor = WindowText;
            var button = root as Button;
            if (button != null) { StyleButton(button); return; }
            var grid = root as DataGridView;
            if (grid != null)
            {
                grid.BackgroundColor = SurfaceBackground; grid.GridColor = HighContrast ? SystemColors.WindowText : Border; grid.BorderStyle = BorderStyle.None;
                grid.EnableHeadersVisualStyles = false;
                grid.ColumnHeadersDefaultCellStyle.BackColor = HighContrast ? SystemColors.Control : Field;
                grid.ColumnHeadersDefaultCellStyle.ForeColor = HighContrast ? SystemColors.ControlText : Muted;
                grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = grid.ColumnHeadersDefaultCellStyle.BackColor;
                grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = grid.ColumnHeadersDefaultCellStyle.ForeColor;
                grid.DefaultCellStyle.BackColor = SurfaceBackground; grid.DefaultCellStyle.ForeColor = WindowText;
                grid.DefaultCellStyle.SelectionBackColor = HighContrast ? SystemColors.Highlight : Color.FromArgb(38, 76, 84);
                grid.DefaultCellStyle.SelectionForeColor = HighContrast ? SystemColors.HighlightText : Text;
                grid.AlternatingRowsDefaultCellStyle.BackColor = HighContrast ? SystemColors.Window : Color.FromArgb(25, 34, 49);
                grid.RowTemplate.Height = 32; grid.RowHeadersVisible = false;
                return;
            }
            if (root is TextBoxBase || root is ComboBox || root is NumericUpDown || root is ListBox)
            {
                root.BackColor = HighContrast ? SystemColors.Window : Field;
                var combo = root as ComboBox;
                if (combo != null)
                {
                    combo.DrawItem -= DrawComboItem;
                    combo.FlatStyle = HighContrast ? FlatStyle.Standard : FlatStyle.Flat;
                    combo.DrawMode = HighContrast ? DrawMode.Normal : DrawMode.OwnerDrawFixed;
                    if (!HighContrast) {
                        combo.ItemHeight = Math.Max(24, combo.Font.Height + 8);
                        combo.DrawItem += DrawComboItem;
                    }
                }
                var box = root as TextBoxBase; if (box != null) box.BorderStyle = BorderStyle.FixedSingle;
                var list = root as ListBox; if (list != null) { list.BorderStyle = HighContrast ? BorderStyle.FixedSingle : BorderStyle.None; list.ItemHeight = 28; }
            }
            else if (root is TabControl || root is TabPage || root is Form || root is WizardProgress) root.BackColor = WindowBackground;
            else if (root is Panel) root.BackColor = Equals(root.Tag, "styled") ? SurfaceBackground : root.Parent == null ? WindowBackground : root.Parent.BackColor;
            else if (root is Label || root is CheckBox || root is RadioButton || root is PictureBox) root.BackColor = Color.Transparent;
            var link = root as LinkLabel;
            if (link != null) {
                link.LinkColor = HighContrast ? SystemColors.HotTrack : Accent;
                link.ActiveLinkColor = HighContrast ? SystemColors.WindowText : Text;
                link.VisitedLinkColor = HighContrast ? SystemColors.HotTrack : Accent;
                link.DisabledLinkColor = HighContrast ? SystemColors.GrayText : Muted;
            }
        }
        private static void DrawComboItem(object sender, DrawItemEventArgs e)
        {
            var combo = (ComboBox)sender;
            bool selected = (e.State & DrawItemState.Selected) != 0;
            using (var brush = new SolidBrush(HighContrast ? (selected ? SystemColors.Highlight : SystemColors.Window) : selected ? Border : Field)) e.Graphics.FillRectangle(brush, e.Bounds);
            string value = e.Index >= 0 ? combo.GetItemText(combo.Items[e.Index]) : combo.Text;
            var bounds = e.Bounds; bounds.Inflate(-5, 0);
            TextRenderer.DrawText(e.Graphics, value, combo.Font, bounds, HighContrast ? (selected ? SystemColors.HighlightText : SystemColors.WindowText) : Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            if ((e.State & DrawItemState.Focus) != 0) e.DrawFocusRectangle();
        }
        private static void StyleButton(Button b)
        {
            // Styling must not replace an explicit accessible purpose. Without an
            // override, the native button automatically announces its current Text.
            if (HighContrast) {
                b.FlatStyle = FlatStyle.Standard;
                b.BackColor = SystemColors.Control; b.ForeColor = SystemColors.ControlText; b.UseVisualStyleBackColor = true;
                return;
            }
            bool primary = Equals(b.Tag, "primary");
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = primary ? 0 : 1;
            b.FlatAppearance.BorderColor = Border;
            b.FlatAppearance.MouseOverBackColor = primary ? Color.FromArgb(128, 244, 208) : Color.FromArgb(39, 54, 73);
            b.FlatAppearance.MouseDownBackColor = primary ? Color.FromArgb(55, 200, 157) : Border;
            b.BackColor = primary ? Accent : Field; b.ForeColor = primary ? Background : Text;
            b.Cursor = Cursors.Hand; b.UseVisualStyleBackColor = false;
            if (b.Height < 30 && b.Dock == DockStyle.None) b.Height = 32;
        }
        public static void Menu(ContextMenuStrip menu)
        {
            menu.Font = Body; menu.ShowImageMargin = false; menu.ShowCheckMargin = true;
            ApplyMenu(menu);
            menu.Opening -= MenuOpening; menu.Opening += MenuOpening;
            var observation = Observe(menu, () => ApplyMenu(menu));
            menu.Disposed += delegate { observation.Dispose(); };
        }
        private static void MenuOpening(object sender, System.ComponentModel.CancelEventArgs e) { ApplyMenu((ContextMenuStrip)sender); }
        private static void ApplyMenu(ContextMenuStrip menu)
        {
            menu.Renderer = HighContrast ? (ToolStripRenderer)new ToolStripSystemRenderer() : new ToolStripProfessionalRenderer(new MenuColors());
            menu.ForeColor = HighContrast ? SystemColors.MenuText : Text;
            menu.BackColor = HighContrast ? SystemColors.Menu : Surface;
            StyleItems(menu.Items); menu.Invalidate(true);
        }
        private static void StyleItems(ToolStripItemCollection items)
        {
            foreach (ToolStripItem item in items)
            {
                item.ForeColor = HighContrast ? SystemColors.MenuText : Text; item.BackColor = HighContrast ? SystemColors.Menu : Surface;
                item.Padding = new Padding(6, 5, 6, 5);
                var sub = item as ToolStripMenuItem;
                if (sub != null) { sub.DropDown.Font = Body; sub.DropDown.Renderer = HighContrast ? (ToolStripRenderer)new ToolStripSystemRenderer() : new ToolStripProfessionalRenderer(new MenuColors()); StyleItems(sub.DropDownItems); }
            }
        }
        private sealed class MenuColors : ProfessionalColorTable
        {
            public override Color ToolStripDropDownBackground { get { return Surface; } }
            public override Color MenuItemSelected { get { return Field; } }
            public override Color MenuItemBorder { get { return Border; } }
            public override Color MenuBorder { get { return Border; } }
            public override Color SeparatorDark { get { return Border; } }
            public override Color SeparatorLight { get { return Surface; } }
            public override Color CheckBackground { get { return Field; } }
            public override Color CheckSelectedBackground { get { return Field; } }
            public override Color ImageMarginGradientBegin { get { return Surface; } }
            public override Color ImageMarginGradientMiddle { get { return Surface; } }
            public override Color ImageMarginGradientEnd { get { return Surface; } }
        }
    }

    internal sealed class ProGoTabs : TabControl
    {
        internal ProGoTabs()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var background = UiTheme.HighContrast ? SystemColors.Window : UiTheme.Background;
            e.Graphics.Clear(background);
            for (int i = 0; i < TabCount; i++)
            {
                var bounds = GetTabRect(i); bool selected = i == SelectedIndex;
                var fill = UiTheme.HighContrast ? (selected ? SystemColors.Highlight : SystemColors.Window) : (selected ? UiTheme.Field : UiTheme.Background);
                var color = UiTheme.HighContrast ? (selected ? SystemColors.HighlightText : SystemColors.WindowText) : (selected ? UiTheme.Accent : UiTheme.Muted);
                using (var brush = new SolidBrush(fill)) e.Graphics.FillRectangle(brush, bounds);
                TextRenderer.DrawText(e.Graphics, TabPages[i].Text, UiTheme.Strong, bounds, color, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                if (selected) using (var pen = new Pen(color, 2)) e.Graphics.DrawLine(pen, bounds.Left + 16, bounds.Bottom - 2, bounds.Right - 16, bounds.Bottom - 2);
                if (selected && Focused) ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(bounds, -4, -4), color, fill);
            }
        }
        protected override void OnSelectedIndexChanged(EventArgs e) { base.OnSelectedIndexChanged(e); Invalidate(); }
    }

    internal sealed class SurfacePanel : Panel
    {
        public SurfacePanel() { DoubleBuffered = true; BackColor = UiTheme.SurfaceBackground; Tag = "styled"; Padding = new Padding(22); }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (var pen = new Pen(UiTheme.HighContrast ? SystemColors.WindowText : UiTheme.Border)) e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }
    }
}

namespace ProGo
{
    internal sealed class WizardProgress : Control
    {
        private int step;
        internal int Step { get { return step; } set { step = value; AccessibleName = "Шаг " + (value + 1) + " из 5"; Invalidate(); } }
        internal WizardProgress() { DoubleBuffered = true; Tag = "styled"; Height = 64; BackColor = UiTheme.WindowBackground; }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e); e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            string[] titles = { "Доступ", "Сервер", "Роутер", "Телефон", "Проверка" };
            int width = Math.Max(1, Width / 5);
            for (int i = 0; i < 5; i++)
            {
                int x = i * width;
                using (var fill = new SolidBrush(UiTheme.HighContrast ? (i <= step ? SystemColors.Highlight : SystemColors.Window) : i <= step ? UiTheme.Accent : UiTheme.Field)) e.Graphics.FillEllipse(fill, x + 1, 4, 26, 26);
                TextRenderer.DrawText(e.Graphics, (i + 1).ToString(), UiTheme.Strong, new Rectangle(x + 1, 4, 26, 26), UiTheme.HighContrast ? (i <= step ? SystemColors.HighlightText : SystemColors.WindowText) : i <= step ? UiTheme.Background : UiTheme.Muted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                if (i < 4) using (var pen = new Pen(UiTheme.HighContrast ? SystemColors.WindowText : UiTheme.Border, 2)) e.Graphics.DrawLine(pen, x + 39, 17, x + width - 12, 17);
                TextRenderer.DrawText(e.Graphics, titles[i], UiTheme.HighContrast && i == step ? UiTheme.Strong : UiTheme.Body, new Point(x, 38), UiTheme.TextColor(i == step ? UiTheme.Accent : UiTheme.Muted));
            }
        }
    }
}

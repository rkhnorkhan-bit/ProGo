using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ProGo
{
    // Native controls keep keyboard navigation, screen-reader names and DPI scaling.
    internal class ProGoForm : Form
    {
        private readonly Icon brand = BrandIcon.Create();
        public ProGoForm()
        {
            Font = UiTheme.Body;
            AutoScaleDimensions = new SizeF(96, 96);
            AutoScaleMode = AutoScaleMode.Dpi;
            Icon = brand;
            BackColor = UiTheme.Background;
            ForeColor = UiTheme.Text;
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
            if (!SystemInformation.HighContrast)
                try { int dark = 1; DwmSetWindowAttribute(Handle, 20, ref dark, 4); } catch { }
        }
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr handle, int attribute, ref int value, int size);
        protected override void Dispose(bool disposing)
        {
            if (disposing) brand.Dispose();
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

        public static Label Label(string text, Font font, Color color)
        {
            return new Label { Text = text, Font = font, ForeColor = color, AutoSize = true, Tag = "styled", Margin = new Padding(0, 0, 0, 8) };
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
            if (SystemInformation.HighContrast) return;
            foreach (Control c in root.Controls) Apply(c);
            if (Equals(root.Tag, "styled")) return;
            root.ForeColor = Text;
            var button = root as Button;
            if (button != null) { StyleButton(button); return; }
            var grid = root as DataGridView;
            if (grid != null)
            {
                grid.BackgroundColor = Surface; grid.GridColor = Border; grid.BorderStyle = BorderStyle.None;
                grid.EnableHeadersVisualStyles = false;
                grid.ColumnHeadersDefaultCellStyle.BackColor = Field; grid.ColumnHeadersDefaultCellStyle.ForeColor = Muted;
                grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Field;
                grid.DefaultCellStyle.BackColor = Surface; grid.DefaultCellStyle.ForeColor = Text;
                grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(38, 76, 84); grid.DefaultCellStyle.SelectionForeColor = Text;
                grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(25, 34, 49);
                grid.RowTemplate.Height = 32; grid.RowHeadersVisible = false;
                return;
            }
            if (root is TextBoxBase || root is ComboBox || root is NumericUpDown || root is ListBox)
            {
                root.BackColor = Field;
                var box = root as TextBoxBase; if (box != null) box.BorderStyle = BorderStyle.FixedSingle;
                var list = root as ListBox; if (list != null) { list.BorderStyle = BorderStyle.None; list.ItemHeight = 28; }
            }
            else if (root is Panel || root is TabPage) root.BackColor = root.Parent == null ? Background : root.Parent.BackColor;
            else if (root is Form) root.BackColor = Background;
            else if (root is Label || root is CheckBox || root is RadioButton || root is PictureBox) root.BackColor = Color.Transparent;
        }
        private static void StyleButton(Button b)
        {
            if (SystemInformation.HighContrast) return;
            bool primary = Equals(b.Tag, "primary");
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = primary ? 0 : 1;
            b.FlatAppearance.BorderColor = Border;
            b.FlatAppearance.MouseOverBackColor = primary ? Color.FromArgb(128, 244, 208) : Color.FromArgb(39, 54, 73);
            b.FlatAppearance.MouseDownBackColor = primary ? Color.FromArgb(55, 200, 157) : Border;
            b.BackColor = primary ? Accent : Field; b.ForeColor = primary ? Background : Text;
            b.Cursor = Cursors.Hand; b.UseVisualStyleBackColor = false;
            if (b.Height < 30 && b.Dock == DockStyle.None) b.Height = 32;
            b.AccessibleName = b.Text;
        }
        public static void Menu(ContextMenuStrip menu)
        {
            menu.Font = Body; menu.ShowImageMargin = false; menu.ShowCheckMargin = true;
            if (SystemInformation.HighContrast) return;
            menu.Renderer = new ToolStripProfessionalRenderer(new MenuColors());
            StyleItems(menu.Items);
        }
        private static void StyleItems(ToolStripItemCollection items)
        {
            foreach (ToolStripItem item in items)
            {
                item.ForeColor = Text; item.BackColor = Surface;
                item.Padding = new Padding(6, 5, 6, 5);
                var sub = item as ToolStripMenuItem;
                if (sub != null) { sub.DropDown.Font = Body; StyleItems(sub.DropDownItems); }
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

    internal sealed class SurfacePanel : Panel
    {
        public SurfacePanel() { DoubleBuffered = true; BackColor = UiTheme.Surface; Tag = "styled"; Padding = new Padding(22); }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (var pen = new Pen(UiTheme.Border)) e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }
    }
}

namespace ProGo
{
    internal sealed class WizardProgress : Control
    {
        private int step;
        internal int Step { get { return step; } set { step = value; AccessibleName = "Шаг " + (value + 1) + " из 5"; Invalidate(); } }
        internal WizardProgress() { DoubleBuffered = true; Tag = "styled"; Height = 64; }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e); e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            string[] titles = { "Доступ", "Сервер", "Роутер", "iPhone", "Проверка" };
            int width = Math.Max(1, Width / 5);
            for (int i = 0; i < 5; i++)
            {
                int x = i * width;
                using (var fill = new SolidBrush(i <= step ? UiTheme.Accent : UiTheme.Field)) e.Graphics.FillEllipse(fill, x + 1, 4, 26, 26);
                TextRenderer.DrawText(e.Graphics, (i + 1).ToString(), UiTheme.Strong, new Rectangle(x + 1, 4, 26, 26), i <= step ? UiTheme.Background : UiTheme.Muted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                if (i < 4) using (var pen = new Pen(UiTheme.Border, 2)) e.Graphics.DrawLine(pen, x + 39, 17, x + width - 12, 17);
                TextRenderer.DrawText(e.Graphics, titles[i], UiTheme.Body, new Point(x, 38), i == step ? UiTheme.Accent : UiTheme.Muted);
            }
        }
    }
}

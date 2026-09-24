using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace ProGo
{
    internal static class BrandIcon
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        public static Icon Create()
        {
            using (var bitmap = new Bitmap(64, 64))
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                using (var shadow = new SolidBrush(Color.FromArgb(70, 0, 0, 0)))
                using (var background = new SolidBrush(Color.FromArgb(24, 64, 160)))
                using (var accent = new SolidBrush(Color.FromArgb(69, 214, 147)))
                using (var white = new SolidBrush(Color.White))
                using (var font = new Font("Segoe UI", 36f, FontStyle.Bold, GraphicsUnit.Pixel))
                using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.FillEllipse(shadow, 6, 7, 54, 54);
                    g.FillEllipse(background, 4, 4, 56, 56);
                    g.FillEllipse(accent, 43, 10, 10, 10);
                    g.DrawString("P", font, white, new RectangleF(0, 5, 64, 54), format);
                }

                IntPtr hIcon = bitmap.GetHicon();
                try
                {
                    using (var icon = Icon.FromHandle(hIcon))
                    {
                        return (Icon)icon.Clone();
                    }
                }
                finally
                {
                    DestroyIcon(hIcon);
                }
            }
        }
    }
}

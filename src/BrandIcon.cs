using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace ProGo
{
    public static class BrandIcon
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        public static Bitmap Draw(int size)
        {
            var bitmap = new Bitmap(size, size);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                g.ScaleTransform(size / 64f, size / 64f);
                using (var shape = new GraphicsPath())
                using (var background = new LinearGradientBrush(new Rectangle(0, 0, 64, 64), Color.FromArgb(40, 67, 92), Color.FromArgb(11, 21, 35), 60f))
                using (var edge = new Pen(Color.FromArgb(81, 126, 148), 1f))
                using (var mark = new Pen(Color.FromArgb(90, 232, 187), 7f))
                {
                    shape.AddArc(2, 2, 24, 24, 180, 90); shape.AddArc(38, 2, 24, 24, 270, 90);
                    shape.AddArc(38, 38, 24, 24, 0, 90); shape.AddArc(2, 38, 24, 24, 90, 90); shape.CloseFigure();
                    g.FillPath(background, shape); g.DrawPath(edge, shape);
                    mark.StartCap = mark.EndCap = LineCap.Round; mark.LineJoin = LineJoin.Round;
                    g.DrawLine(mark, 21, 47, 21, 18); g.DrawLine(mark, 21, 18, 35, 18);
                    g.DrawArc(mark, 25, 18, 20, 20, 270, 180); g.DrawLine(mark, 35, 38, 29, 38);
                    using (var arrow = new Pen(Color.White, 3.5f))
                    {
                        arrow.StartCap = arrow.EndCap = LineCap.Round;
                        g.DrawLine(arrow, 36, 48, 47, 37); g.DrawLine(arrow, 39, 37, 47, 37); g.DrawLine(arrow, 47, 37, 47, 45);
                    }
                }
            }
            return bitmap;
        }
        public static Icon Create()
        {
            using (var bitmap = Draw(64))
            {
                var handle = bitmap.GetHicon();
                try { using (var icon = Icon.FromHandle(handle)) return (Icon)icon.Clone(); }
                finally { DestroyIcon(handle); }
            }
        }
        // The executable and tray use exactly the same drawing at every icon size.
        public static void WriteIcon(string path)
        {
            int[] sizes = { 16, 24, 32, 48, 64, 128, 256 };
            var frames = new byte[sizes.Length][];
            for (int i = 0; i < sizes.Length; i++)
                using (var bitmap = Draw(sizes[i]))
                using (var stream = new MemoryStream()) { bitmap.Save(stream, ImageFormat.Png); frames[i] = stream.ToArray(); }
            using (var writer = new BinaryWriter(File.Create(path)))
            {
                writer.Write((ushort)0); writer.Write((ushort)1); writer.Write((ushort)sizes.Length);
                int offset = 6 + 16 * sizes.Length;
                for (int i = 0; i < sizes.Length; i++)
                {
                    writer.Write((byte)(sizes[i] == 256 ? 0 : sizes[i])); writer.Write((byte)(sizes[i] == 256 ? 0 : sizes[i]));
                    writer.Write((byte)0); writer.Write((byte)0); writer.Write((ushort)1); writer.Write((ushort)32);
                    writer.Write(frames[i].Length); writer.Write(offset); offset += frames[i].Length;
                }
                foreach (var frame in frames) writer.Write(frame);
            }
        }
    }
}

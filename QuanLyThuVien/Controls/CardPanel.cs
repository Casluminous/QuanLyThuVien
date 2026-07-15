using System.Drawing.Drawing2D;

namespace QuanLyThuVien.Controls
{
    public class CardPanel : Panel
    {
        public Color AccentColor { get; set; } = Color.FromArgb(52, 152, 219);
        public int AccentWidth { get; set; } = 5;
        public int BorderRadius { get; set; } = 12;

        public CardPanel()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);
            BackColor = Color.White;
            Padding = new Padding(20);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (var path = CreatePath(ClientRectangle, BorderRadius))
            {
                Region = new Region(path);

                using (var brush = new SolidBrush(BackColor))
                    g.FillPath(brush, path);
            }

            var accentRect = new Rectangle(0, 0, AccentWidth, Height);
            using (var path = CreateLeftAccentPath(accentRect, BorderRadius))
            using (var brush = new SolidBrush(AccentColor))
                g.FillPath(brush, path);
        }

        private GraphicsPath CreatePath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            if (d > rect.Width) d = rect.Width;
            if (d > rect.Height) d = rect.Height;

            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private GraphicsPath CreateLeftAccentPath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            if (d > rect.Height) d = rect.Height;

            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddLine(rect.Right, rect.Y, rect.Right, rect.Bottom);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 0, 90);
            path.CloseFigure();
            return path;
        }
    }
}

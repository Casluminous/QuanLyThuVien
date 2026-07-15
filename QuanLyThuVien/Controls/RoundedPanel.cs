using System.Drawing.Drawing2D;

namespace QuanLyThuVien.Controls
{
    public class RoundedPanel : Panel
    {
        public int BorderRadius { get; set; } = 20;
        public Color BorderColor { get; set; } = Color.Transparent;
        public int BorderSize { get; set; } = 0;
        public bool HasShadow { get; set; } = false;

        public RoundedPanel()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor,
                true);
            BackColor = Color.White;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            if (HasShadow)
            {
                using (var shadowPath = CreatePath(4, 4))
                using (var shadowBrush = new SolidBrush(Color.FromArgb(30, 0, 0, 0)))
                    g.FillPath(shadowBrush, shadowPath);
            }

            using (var path = CreatePath())
            using (var brush = new SolidBrush(BackColor))
                g.FillPath(brush, path);

            if (BorderSize > 0 && BorderColor != Color.Transparent)
            {
                using (var path = CreatePath())
                using (var pen = new Pen(BorderColor, BorderSize))
                    g.DrawPath(pen, path);
            }

            Region = new Region(CreatePath());
        }

        private GraphicsPath CreatePath(int offsetX = 0, int offsetY = 0)
        {
            var path = new GraphicsPath();
            int d = BorderRadius * 2;
            var rect = new Rectangle(offsetX, offsetY, Width - offsetX * 2, Height - offsetY * 2);

            if (d > rect.Width) d = rect.Width;
            if (d > rect.Height) d = rect.Height;

            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();

            return path;
        }
    }
}

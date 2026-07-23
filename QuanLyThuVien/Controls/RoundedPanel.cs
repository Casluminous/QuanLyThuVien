using System.Drawing.Drawing2D;

using QuanLyThuVien.Helpers;

namespace QuanLyThuVien.Controls
{
    public class RoundedPanel : Panel
    {
        public int BorderRadius { get; set; } = 14;
        public Color BorderColor { get; set; } = AppColors.Border;
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
            BackColor = AppColors.CardBg;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (Width > 0 && Height > 0)
            {
                using var path = CreatePath();
                Region = new Region(path);
            }
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
                using (var shadowBrush = new SolidBrush(AppColors.Shadow))
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

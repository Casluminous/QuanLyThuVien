using System.Drawing.Drawing2D;
using QuanLyThuVien.Helpers;

namespace QuanLyThuVien.Controls
{
    public class StatCard : Panel
    {
        private string _title = "";
        private string _value = "";
        private string _unit = "";
        private Color _accentColor = Color.FromArgb(232, 132, 107);

        public string Title { get => _title; set { _title = value; Invalidate(); } }
        public string Value { get => _value; set { _value = value; Invalidate(); } }
        public string Unit { get => _unit; set { _unit = value; Invalidate(); } }
        public Color AccentColor { get => _accentColor; set { _accentColor = value; Invalidate(); } }
        public int BorderRadius { get; set; } = 12;

        private static readonly Font _titleFont = new Font("Segoe UI", 9F, FontStyle.Regular);
        private static readonly Font _valueFont = new Font("Segoe UI", 26F, FontStyle.Bold);
        private static readonly Font _unitFont = new Font("Segoe UI", 9F, FontStyle.Regular);

        public StatCard()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);
            BackColor = Color.White;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int shadowSize = 4;
            var cardRect = new Rectangle(0, shadowSize, Width, Height - shadowSize);

            // Draw subtle shadow
            for (int i = shadowSize; i >= 1; i--)
            {
                var shadowRect = new Rectangle(i, shadowSize + i, Width - i * 2, Height - shadowSize - i * 2);
                using (var path = CreateRoundedPath(shadowRect, BorderRadius))
                using (var brush = new SolidBrush(Color.FromArgb(6, 0, 0, 0)))
                    g.FillPath(brush, path);
            }

            // Draw card background
            using (var path = CreateRoundedPath(cardRect, BorderRadius))
            {
                Region = new Region(path);
                using (var brush = new SolidBrush(BackColor))
                    g.FillPath(brush, path);
            }

            // Draw bottom accent line
            int accentHeight = 3;
            var accentRect = new Rectangle(cardRect.X + BorderRadius, cardRect.Bottom - accentHeight, cardRect.Width - BorderRadius * 2, accentHeight);
            using (var brush = new SolidBrush(_accentColor))
                g.FillRectangle(brush, accentRect);

            int textLeft = cardRect.X + 20;
            int textWidth = cardRect.Width - 40;

            // Draw title text
            using (var brush = new SolidBrush(AppColors.TextSecondary))
            {
                var titleRect = new Rectangle(textLeft, cardRect.Y + 18, textWidth, 20);
                var titleFormat = new StringFormat
                {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Near,
                    Trimming = StringTrimming.EllipsisCharacter,
                    FormatFlags = StringFormatFlags.NoWrap
                };
                g.DrawString(_title.ToUpperInvariant(), _titleFont, brush, titleRect, titleFormat);
            }

            // Draw value text
            using (var brush = new SolidBrush(AppColors.TextPrimary))
            {
                g.DrawString(_value, _valueFont, brush, new PointF(textLeft, cardRect.Y + 42));
            }

            // Draw unit text
            using (var brush = new SolidBrush(AppColors.TextMuted))
            {
                g.DrawString(_unit, _unitFont, brush, new PointF(textLeft, cardRect.Y + 80));
            }
        }

        private GraphicsPath CreateRoundedPath(Rectangle rect, int radius)
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
    }
}

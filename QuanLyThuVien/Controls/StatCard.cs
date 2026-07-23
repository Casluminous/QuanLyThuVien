using System.Drawing.Drawing2D;
using QuanLyThuVien.Helpers;

namespace QuanLyThuVien.Controls
{
    public class StatCard : Panel
    {
        private string _title = "";
        private string _value = "";
        private string _unit = "";
        private Color _accentColor = AppColors.Primary;

        public string Title { get => _title; set { _title = value; UpdateAccessibility(); Invalidate(); } }
        public string Value { get => _value; set { _value = value; UpdateAccessibility(); Invalidate(); } }
        public string Unit { get => _unit; set { _unit = value; UpdateAccessibility(); Invalidate(); } }
        public Color AccentColor { get => _accentColor; set { _accentColor = value; Invalidate(); } }
        public int BorderRadius { get; set; } = 14;

        private static readonly Font _titleFont = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
        private static readonly Font _valueFont = new Font("Segoe UI", 24F, FontStyle.Bold);
        private static readonly Font _unitFont = new Font("Segoe UI", 9F, FontStyle.Regular);

        public StatCard()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor,
                true);
            BackColor = Color.Transparent;
            TabStop = false;
            AccessibleRole = AccessibleRole.StaticText;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (Width > 0 && Height > 0)
            {
                using var path = CreateRoundedPath(ClientRectangle, BorderRadius);
                Region = new Region(path);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            const int shadowSize = 3;
            var cardRect = new Rectangle(1, 1, Math.Max(1, Width - shadowSize - 2), Math.Max(1, Height - shadowSize - 2));
            var shadowRect = new Rectangle(cardRect.X + 2, cardRect.Y + 2, cardRect.Width, cardRect.Height);

            using (var shadowPath = CreateRoundedPath(shadowRect, BorderRadius))
            using (var shadowBrush = new SolidBrush(AppColors.Shadow))
                g.FillPath(shadowBrush, shadowPath);

            // Draw card background
            using (var path = CreateRoundedPath(cardRect, BorderRadius))
            {
                using (var brush = new SolidBrush(AppColors.WorkbenchSurface))
                    g.FillPath(brush, path);
                using var borderPen = new Pen(AppColors.Border, 1F);
                g.DrawPath(borderPen, path);
            }

            int textLeft = cardRect.X + 16;
            int textWidth = Math.Max(1, cardRect.Width - 32);

            // Draw title text
            using (var brush = new SolidBrush(AppColors.TextSecondary))
            {
                var titleRect = new Rectangle(textLeft, cardRect.Y + 14, textWidth, 20);
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
                var valueRect = new Rectangle(textLeft, cardRect.Y + 36, textWidth, 42);
                var valueFormat = new StringFormat
                {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Center,
                    Trimming = StringTrimming.EllipsisCharacter,
                    FormatFlags = StringFormatFlags.NoWrap
                };
                g.DrawString(_value, _valueFont, brush, valueRect, valueFormat);
            }

            // Draw unit text
            using (var brush = new SolidBrush(AppColors.TextMuted))
            {
                var unitRect = new Rectangle(textLeft, cardRect.Bottom - 25, textWidth, 18);
                var unitFormat = new StringFormat
                {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Center,
                    Trimming = StringTrimming.EllipsisCharacter,
                    FormatFlags = StringFormatFlags.NoWrap
                };
                g.DrawString(_unit, _unitFont, brush, unitRect, unitFormat);
            }
        }

        private void UpdateAccessibility()
        {
            AccessibleName = string.Join(" ", new[] { _title, _value, _unit }.Where(part => !string.IsNullOrWhiteSpace(part)));
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

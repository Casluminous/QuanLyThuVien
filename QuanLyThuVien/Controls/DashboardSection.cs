using System.Drawing.Drawing2D;
using QuanLyThuVien.Helpers;

namespace QuanLyThuVien.Controls
{
    /// <summary>
    /// Warm, border-led section surface for the operational Dashboard.
    /// It owns presentation only; callers provide all business content through Body.
    /// </summary>
    public sealed class DashboardSection : Panel
    {
        private readonly Label _titleLabel;

        public Panel Body { get; }
        public int BorderRadius { get; set; } = 14;

        public string Title
        {
            get => _titleLabel.Text;
            set
            {
                _titleLabel.Text = value;
                AccessibleName = value;
            }
        }

        public DashboardSection(string title)
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
            AccessibleRole = AccessibleRole.Grouping;
            AccessibleName = title;
            Margin = Padding.Empty;

            _titleLabel = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 48,
                Padding = new Padding(16, 0, 16, 0),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                ForeColor = AppColors.TextPrimary,
                BackColor = Color.Transparent,
                AutoEllipsis = true
            };

            Body = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 0, 16, 16),
                BackColor = Color.Transparent,
                Margin = Padding.Empty
            };

            Controls.Add(Body);
            Controls.Add(_titleLabel);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = new Rectangle(1, 1, Math.Max(1, Width - 3), Math.Max(1, Height - 3));
            using var path = CreateRoundedPath(rect, BorderRadius);
            using var surfaceBrush = new SolidBrush(AppColors.WorkbenchSurface);
            using var borderPen = new Pen(AppColors.Border, 1F);
            e.Graphics.FillPath(surfaceBrush, path);
            e.Graphics.DrawPath(borderPen, path);
        }

        private static GraphicsPath CreateRoundedPath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int diameter = Math.Min(radius * 2, Math.Min(rect.Width, rect.Height));
            if (diameter <= 1)
            {
                path.AddRectangle(rect);
                return path;
            }

            path.AddArc(rect.Left, rect.Top, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.Left, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}

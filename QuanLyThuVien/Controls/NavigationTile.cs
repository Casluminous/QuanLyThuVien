using System.Drawing.Drawing2D;

using QuanLyThuVien.Helpers;

namespace QuanLyThuVien.Controls
{
    // Hallmark · pre-emit critique: P5 H4 E4 S4 R5 V5
    // Hallmark · genre: modern-minimal · macrostructure: Index-First · design-system: design.md · designed-as-app · contrast: pass (40-41) · slop: pass
    public sealed class NavigationTile : Button
    {
        private const int Radius = 14;
        private const int OuterInset = 3;
        private readonly Font _titleFont = new("Segoe UI", 12F, FontStyle.Bold);
        private readonly Font _descriptionFont = new("Segoe UI", 9F, FontStyle.Regular);
        private readonly Font _arrowFont = new("Segoe UI", 15F, FontStyle.Regular);
        private string _title = string.Empty;
        private string _description = string.Empty;
        private Color _accentColor = AppColors.Primary;
        private bool _isHovered;
        private bool _isPressed;
        private bool _isFocused;

        public string Title
        {
            get => _title;
            set
            {
                _title = value ?? string.Empty;
                AccessibleName = _title;
                Invalidate();
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                _description = value ?? string.Empty;
                AccessibleDescription = _description;
                Invalidate();
            }
        }

        public Color AccentColor
        {
            get => _accentColor;
            set
            {
                _accentColor = value;
                Invalidate();
            }
        }

        public string TargetTag { get; set; } = string.Empty;

        public NavigationTile()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            FlatAppearance.MouseDownBackColor = Color.Transparent;
            FlatAppearance.MouseOverBackColor = Color.Transparent;
            BackColor = Color.Transparent;
            ForeColor = AppColors.TextPrimary;
            Cursor = Cursors.Hand;
            MinimumSize = new Size(180, 104);
            Size = new Size(260, 112);
            Margin = new Padding(0);
            Padding = Padding.Empty;
            TabStop = true;
            AccessibleRole = AccessibleRole.PushButton;
            UseVisualStyleBackColor = false;

            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor,
                true);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            _isFocused = true;
            Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            _isFocused = false;
            _isPressed = false;
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _isHovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isHovered = false;
            _isPressed = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                _isPressed = true;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _isPressed = false;
            Invalidate();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode is Keys.Space or Keys.Enter)
            {
                _isPressed = true;
                Invalidate();
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (e.KeyCode is Keys.Space or Keys.Enter)
            {
                _isPressed = false;
                Invalidate();
            }
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Cursor = Enabled ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (Width <= 0 || Height <= 0) return;

            using var path = CreateRoundedPath(
                new Rectangle(OuterInset, OuterInset, Math.Max(1, Width - OuterInset * 2), Math.Max(1, Height - OuterInset * 2)),
                Radius);
            Region?.Dispose();
            Region = new Region(path);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics graphics = e.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(AppColors.ContentBg);

            var surfaceRect = new Rectangle(
                OuterInset,
                OuterInset,
                Math.Max(1, Width - OuterInset * 2 - 1),
                Math.Max(1, Height - OuterInset * 2 - 1));
            using var surfacePath = CreateRoundedPath(surfaceRect, Radius);

            Color surfaceColor = !Enabled
                ? AppColors.AlternateSurface
                : _isPressed
                    ? AppColors.SelectedSurface
                    : _isHovered
                        ? AppColors.HoverSurface
                        : AppColors.CardBg;
            Color borderColor = Enabled && (_isHovered || _isPressed)
                ? AppColors.Primary
                : AppColors.Border;

            using (var surfaceBrush = new SolidBrush(surfaceColor))
                graphics.FillPath(surfaceBrush, surfacePath);
            using (var borderPen = new Pen(borderColor, 1F))
                graphics.DrawPath(borderPen, surfacePath);

            var accentRect = new Rectangle(surfaceRect.Left + 18, surfaceRect.Top + 17, 34, 4);
            using (var accentBrush = new SolidBrush(Enabled ? _accentColor : AppColors.TextMuted))
                graphics.FillRectangle(accentBrush, accentRect);

            Color titleColor = Enabled ? AppColors.TextPrimary : AppColors.TextMuted;
            Color descriptionColor = Enabled ? AppColors.TextSecondary : AppColors.TextMuted;
            var titleRect = new Rectangle(surfaceRect.Left + 18, surfaceRect.Top + 30, Math.Max(20, surfaceRect.Width - 68), 27);
            var descriptionRect = new Rectangle(surfaceRect.Left + 18, surfaceRect.Top + 59, Math.Max(20, surfaceRect.Width - 68), 37);
            var arrowRect = new Rectangle(surfaceRect.Right - 42, surfaceRect.Top + 34, 24, 34);

            TextRenderer.DrawText(
                graphics,
                _title,
                _titleFont,
                titleRect,
                titleColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            TextRenderer.DrawText(
                graphics,
                _description,
                _descriptionFont,
                descriptionRect,
                descriptionColor,
                TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            TextRenderer.DrawText(
                graphics,
                "\u2192",
                _arrowFont,
                arrowRect,
                Enabled ? AppColors.PrimaryDark : AppColors.TextMuted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

            if (_isFocused)
            {
                var focusRect = Rectangle.Inflate(surfaceRect, -3, -3);
                using var focusPath = CreateRoundedPath(focusRect, Radius - 3);
                using var focusPen = new Pen(AppColors.Focus, 2F);
                graphics.DrawPath(focusPen, focusPath);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _titleFont.Dispose();
                _descriptionFont.Dispose();
                _arrowFont.Dispose();
                Region?.Dispose();
            }

            base.Dispose(disposing);
        }

        private static GraphicsPath CreateRoundedPath(Rectangle rectangle, int radius)
        {
            var path = new GraphicsPath();
            int diameter = Math.Max(1, Math.Min(radius * 2, Math.Min(rectangle.Width, rectangle.Height)));
            path.AddArc(rectangle.Left, rectangle.Top, diameter, diameter, 180, 90);
            path.AddArc(rectangle.Right - diameter, rectangle.Top, diameter, diameter, 270, 90);
            path.AddArc(rectangle.Right - diameter, rectangle.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rectangle.Left, rectangle.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}

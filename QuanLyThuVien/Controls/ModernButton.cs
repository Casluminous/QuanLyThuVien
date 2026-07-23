using System.Drawing.Drawing2D;

using QuanLyThuVien.Helpers;

namespace QuanLyThuVien.Controls
{
    public class ModernButton : Button
    {
        private Color _baseColor = AppColors.Primary;
        private Color _hoverColor = AppColors.PrimaryDark;
        private Color _pressedColor = Color.FromArgb(19, 78, 74);
        private Color _textColor = Color.White;
        private int _borderRadius = 12;
        private bool _isHovered = false;
        private bool _isPressed = false;
        private bool _isFocused = false;

        public Color BaseColor { get => _baseColor; set { _baseColor = value; Invalidate(); } }
        public Color HoverColor { get => _hoverColor; set { _hoverColor = value; Invalidate(); } }
        public Color PressedColor { get => _pressedColor; set { _pressedColor = value; Invalidate(); } }
        public Color TextColor { get => _textColor; set { _textColor = value; Invalidate(); } }
        public int BorderRadius { get => _borderRadius; set { _borderRadius = value; Invalidate(); } }

        public ModernButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor = Color.Transparent;
            ForeColor = Color.White;
            Cursor = Cursors.Hand;
            Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            Size = new Size(120, 40);
            MinimumSize = new Size(40, 40);
            TabStop = true;
            AccessibleRole = AccessibleRole.PushButton;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
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
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            _isPressed = true;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _isPressed = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color bgColor = !Enabled ? Color.FromArgb(190, 205, 202) : _isPressed ? _pressedColor : (_isHovered ? _hoverColor : _baseColor);

            using (var path = CreatePath(ClientRectangle, _borderRadius))
            using (var brush = new SolidBrush(bgColor))
                g.FillPath(brush, path);

            if (_isFocused)
            {
                using var focusPen = new Pen(AppColors.Focus, 2F);
                var focusRect = new Rectangle(1, 1, Math.Max(1, Width - 3), Math.Max(1, Height - 3));
                using var focusPath = CreatePath(focusRect, Math.Max(2, _borderRadius - 1));
                g.DrawPath(focusPen, focusPath);
            }

            TextRenderer.DrawText(g, Text, Font, ClientRectangle, Enabled ? _textColor : AppColors.TextMuted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
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
    }
}

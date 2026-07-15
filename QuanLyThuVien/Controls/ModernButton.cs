using System.Drawing.Drawing2D;

namespace QuanLyThuVien.Controls
{
    public class ModernButton : Button
    {
        private Color _baseColor = Color.FromArgb(232, 132, 107); // #E8846B Coral
        private Color _hoverColor = Color.FromArgb(210, 110, 85); // #D26E55 Darker coral
        private Color _pressedColor = Color.FromArgb(185, 95, 75);
        private Color _textColor = Color.White;
        private int _borderRadius = 20; // Pill-shaped rounded button
        private bool _isHovered = false;
        private bool _isPressed = false;

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
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
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

            Color bgColor = _isPressed ? _pressedColor : (_isHovered ? _hoverColor : _baseColor);

            using (var path = CreatePath(ClientRectangle, _borderRadius))
            using (var brush = new SolidBrush(bgColor))
                g.FillPath(brush, path);

            TextRenderer.DrawText(g, Text, Font, ClientRectangle, _textColor,
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

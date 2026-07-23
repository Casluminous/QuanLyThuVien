using System.Drawing.Drawing2D;
using QuanLyThuVien.Helpers;

namespace QuanLyThuVien.Controls
{
    public class ModernTextBox : UserControl
    {
        private readonly TextBox _textBox;
        private string _placeholder = "";
        private Color _placeholderColor = AppColors.TextMuted;
        private bool _isPlaceholderActive = true;
        private int _borderRadius = 10;
        private Color _borderColor = AppColors.Border;
        private Color _focusedBorderColor = AppColors.Focus;
        private Color _hoverBorderColor = AppColors.TextSecondary;
        private bool _isFocused = false;
        private bool _isHovered = false;

        public string Placeholder
        {
            get => _placeholder;
            set
            {
                _placeholder = value;
                if (_isPlaceholderActive)
                {
                    _textBox.Text = value;
                    _textBox.ForeColor = _placeholderColor;
                }
            }
        }

        public Color PlaceholderColor
        {
            get => _placeholderColor;
            set { _placeholderColor = value; if (_isPlaceholderActive) _textBox.ForeColor = value; }
        }

        public int BorderRadius
        {
            get => _borderRadius;
            set { _borderRadius = value; Invalidate(); }
        }

        public Color BorderColor
        {
            get => _borderColor;
            set { _borderColor = value; Invalidate(); }
        }

        public Color FocusedBorderColor
        {
            get => _focusedBorderColor;
            set { _focusedBorderColor = value; Invalidate(); }
        }

        [System.Diagnostics.CodeAnalysis.AllowNull]
        public override string Text
        {
            get => _isPlaceholderActive ? "" : _textBox.Text;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    _isPlaceholderActive = true;
                    _textBox.Text = _placeholder;
                    _textBox.ForeColor = _placeholderColor;
                }
                else
                {
                    _isPlaceholderActive = false;
                    _textBox.Text = value;
                    _textBox.ForeColor = AppColors.TextPrimary;
                }
                Invalidate();
            }
        }

        public bool Multiline
        {
            get => _textBox.Multiline;
            set
            {
                _textBox.Multiline = value;
                UpdateTextBoxPosition();
            }
        }

        public bool UseSystemPasswordChar
        {
            get => _textBox.UseSystemPasswordChar;
            set => _textBox.UseSystemPasswordChar = value;
        }

        public bool ReadOnly
        {
            get => _textBox.ReadOnly;
            set
            {
                _textBox.ReadOnly = value;
                _textBox.BackColor = value ? AppColors.HoverSurface : Color.White;
                BackColor = value ? AppColors.HoverSurface : Color.White;
                Invalidate();
            }
        }

        [System.Diagnostics.CodeAnalysis.AllowNull]
        public override Font Font
        {
            get => base.Font;
            set
            {
                base.Font = value;
                _textBox.Font = value;
                UpdateTextBoxPosition();
            }
        }

        public override Color BackColor
        {
            get => base.BackColor;
            set
            {
                base.BackColor = value;
                _textBox.BackColor = value;
                Invalidate();
            }
        }

        public ModernTextBox()
        {
            _textBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10F),
                ForeColor = _placeholderColor,
                BackColor = AppColors.CardBg,
                Width = Width - 20
            };

            BackColor = AppColors.CardBg;
            Padding = new Padding(10, 8, 10, 8);
            Size = new Size(250, 36);
            TabStop = true;
            AccessibleRole = AccessibleRole.Text;
            DoubleBuffered = true;

            Controls.Add(_textBox);

            _textBox.GotFocus += (s, e) =>
            {
                _isFocused = true;
                if (_isPlaceholderActive)
                {
                    _isPlaceholderActive = false;
                    _textBox.Text = "";
                    _textBox.ForeColor = AppColors.TextPrimary;
                }
                Invalidate();
            };

            _textBox.LostFocus += (s, e) =>
            {
                _isFocused = false;
                if (string.IsNullOrWhiteSpace(_textBox.Text))
                {
                    _isPlaceholderActive = true;
                    _textBox.Text = _placeholder;
                    _textBox.ForeColor = _placeholderColor;
                }
                Invalidate();
            };

            _textBox.TextChanged += (s, e) =>
            {
                OnTextChanged(e);
            };
            _textBox.KeyDown += (s, e) => OnKeyDown(e);

            MouseEnter += (s, e) => { _isHovered = true; Invalidate(); };
            MouseLeave += (s, e) => { _isHovered = false; Invalidate(); };
            _textBox.MouseEnter += (s, e) => { _isHovered = true; Invalidate(); };
            _textBox.MouseLeave += (s, e) => { _isHovered = false; Invalidate(); };

            UpdateTextBoxPosition();
        }

        protected override void OnEnter(EventArgs e)
        {
            base.OnEnter(e);
            if (!_textBox.Focused)
                _textBox.Focus();
        }

        private void UpdateTextBoxPosition()
        {
            if (Multiline)
            {
                _textBox.Dock = DockStyle.Fill;
            }
            else
            {
                _textBox.Dock = DockStyle.None;
                _textBox.Width = Width - Padding.Left - Padding.Right - 4;
                _textBox.Location = new Point(Padding.Left, (Height - _textBox.Height) / 2);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateTextBoxPosition();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            Color drawBorderColor = _isFocused ? _focusedBorderColor : (_isHovered ? _hoverBorderColor : _borderColor);
            int borderThickness = _isFocused ? 2 : 1;

            using (var path = CreateRoundedPath(rect, _borderRadius))
            {
                // Background
                using (var brush = new SolidBrush(BackColor))
                    g.FillPath(brush, path);

                // Border
                using (var pen = new Pen(drawBorderColor, borderThickness))
                {
                    if (_isFocused)
                    {
                        // Draw inner border slightly to avoid clipping
                        var focusedRect = new Rectangle(borderThickness / 2, borderThickness / 2, Width - borderThickness - 1, Height - borderThickness - 1);
                        using (var focusedPath = CreateRoundedPath(focusedRect, _borderRadius - 1))
                            g.DrawPath(pen, focusedPath);
                    }
                    else
                    {
                        g.DrawPath(pen, path);
                    }
                }
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

        public string GetRealText() => Text ?? string.Empty;
        public void SetRealText(string value) => Text = value;
        public void FocusInput() => _textBox.Focus();
    }
}

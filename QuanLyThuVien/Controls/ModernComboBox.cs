using System.Drawing.Drawing2D;
using QuanLyThuVien.Helpers;

namespace QuanLyThuVien.Controls
{
    public class ModernComboBox : UserControl
    {
        private readonly ComboBox _comboBox;
        private int _borderRadius = 10;
        private Color _borderColor = AppColors.Border;
        private Color _focusedBorderColor = AppColors.Focus;
        private Color _hoverBorderColor = AppColors.TextSecondary;
        private bool _isFocused = false;
        private bool _isHovered = false;

        public ComboBox.ObjectCollection Items => _comboBox.Items;

        public object? SelectedItem
        {
            get => _comboBox.SelectedItem;
            set => _comboBox.SelectedItem = value;
        }

        public int SelectedIndex
        {
            get => _comboBox.SelectedIndex;
            set => _comboBox.SelectedIndex = value;
        }

        public ComboBoxStyle DropDownStyle
        {
            get => _comboBox.DropDownStyle;
            set => _comboBox.DropDownStyle = value;
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
        public override Font Font
        {
            get => base.Font;
            set
            {
                base.Font = value;
                _comboBox.Font = value;
                UpdateComboBoxPosition();
            }
        }

        public override Color BackColor
        {
            get => base.BackColor;
            set
            {
                base.BackColor = value;
                _comboBox.BackColor = value;
                Invalidate();
            }
        }

        public ModernComboBox()
        {
            _comboBox = new ComboBox
            {
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F),
                BackColor = AppColors.CardBg,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            BackColor = AppColors.CardBg;
            // The inner WinForms ComboBox already renders its own drop-down button.
            // Keep symmetric padding so the selected text can use the full control
            // width instead of reserving space for a second, custom arrow.
            Padding = new Padding(8, 6, 8, 6);
            Size = new Size(250, 36);
            TabStop = true;
            AccessibleRole = AccessibleRole.ComboBox;
            DoubleBuffered = true;

            Controls.Add(_comboBox);

            _comboBox.GotFocus += (s, e) => { _isFocused = true; Invalidate(); };
            _comboBox.LostFocus += (s, e) => { _isFocused = false; Invalidate(); };
            _comboBox.SelectedIndexChanged += (s, e) => OnSelectedIndexChanged(e);

            MouseEnter += (s, e) => { _isHovered = true; Invalidate(); };
            MouseLeave += (s, e) => { _isHovered = false; Invalidate(); };
            _comboBox.MouseEnter += (s, e) => { _isHovered = true; Invalidate(); };
            _comboBox.MouseLeave += (s, e) => { _isHovered = false; Invalidate(); };

            UpdateComboBoxPosition();
        }

        private void UpdateComboBoxPosition()
        {
            _comboBox.Dock = DockStyle.None;
            _comboBox.Width = Math.Max(1, Width - Padding.Left - Padding.Right);
            _comboBox.Location = new Point(Padding.Left, (Height - _comboBox.Height) / 2);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateComboBoxPosition();
        }

        public event EventHandler? SelectedIndexChanged;
        protected virtual void OnSelectedIndexChanged(EventArgs e)
        {
            SelectedIndexChanged?.Invoke(this, e);
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
    }
}

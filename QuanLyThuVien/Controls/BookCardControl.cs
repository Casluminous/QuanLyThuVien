using System.Drawing.Drawing2D;

namespace QuanLyThuVien.Controls
{
    public class BookCardControl : UserControl
    {
        private string _title = "";
        private string _author = "";
        private string _price = "";
        private string _genre = "";
        private Image? _coverImage;
        private bool _isHovered = false;

        private static readonly Font _titleFont = new Font("Segoe UI", 11F, FontStyle.Bold);
        private static readonly Font _authorFont = new Font("Segoe UI", 9.5F, FontStyle.Regular);
        private static readonly Font _priceFont = new Font("Segoe UI", 12F, FontStyle.Bold);
        private static readonly Font _genreFont = new Font("Segoe UI", 8F, FontStyle.Bold);
        private static readonly Font _placeholderFont = new Font("Segoe UI", 24F, FontStyle.Bold);

        private static readonly Color _bgColor = Color.FromArgb(252, 250, 247);
        private static readonly Color _textPrimary = Color.FromArgb(45, 42, 38);
        private static readonly Color _textSecondary = Color.FromArgb(120, 115, 108);
        private static readonly Color _accentCoral = Color.FromArgb(232, 132, 107);
        private static readonly Color _accentMint = Color.FromArgb(143, 188, 143);
        private static readonly Color _accentLavender = Color.FromArgb(180, 160, 210);
        private static readonly Color _accentPeach = Color.FromArgb(245, 180, 150);
        private static readonly Color _accentSky = Color.FromArgb(140, 190, 220);

        private static readonly Color[] _genreColors = new[]
        {
            _accentCoral, _accentMint, _accentLavender, _accentPeach, _accentSky
        };

        public string Title { get => _title; set { _title = value; Invalidate(); } }
        public string Author { get => _author; set { _author = value; Invalidate(); } }
        public string Price { get => _price; set { _price = value; Invalidate(); } }
        public string Genre { get => _genre; set { _genre = value; Invalidate(); } }
        public Image? CoverImage { get => _coverImage; set { _coverImage = value; Invalidate(); } }

        public int BorderRadius { get; set; } = 14;

        public BookCardControl()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);
            Size = new Size(240, 340);
            Cursor = Cursors.Hand;
            Margin = new Padding(6);

            MouseEnter += (s, e) => { _isHovered = true; Invalidate(); };
            MouseLeave += (s, e) => { _isHovered = false; Invalidate(); };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int shadowOffset = _isHovered ? 6 : 3;
            var cardRect = new Rectangle(0, shadowOffset, Width, Height - shadowOffset);

            // Shadow
            for (int i = shadowOffset; i >= 1; i--)
            {
                int shrink = i * 2;
                var sr = new Rectangle(i, shadowOffset + i, Width - shrink, Height - shadowOffset - shrink);
                using var path = CreateRoundedPath(sr, BorderRadius);
                using var brush = new SolidBrush(Color.FromArgb(_isHovered ? 25 : 10, 0, 0, 0));
                g.FillPath(brush, path);
            }

            // White card background
            using (var path = CreateRoundedPath(cardRect, BorderRadius))
            {
                Region = new Region(path);
                using var brush = new SolidBrush(_bgColor);
                g.FillPath(brush, path);
            }

            int pad = 12;
            int imageH = 165;
            var imageRect = new Rectangle(cardRect.X + pad, cardRect.Y + pad, cardRect.Width - pad * 2, imageH);

            // Image or placeholder
            if (_coverImage != null)
            {
                using var imgPath = CreateRoundedPath(imageRect, 10);
                g.SetClip(imgPath);

                var imgAspect = (float)_coverImage.Width / _coverImage.Height;
                var rectAspect = (float)imageRect.Width / imageRect.Height;
                int srcX, srcY, srcW, srcH;
                if (imgAspect > rectAspect)
                {
                    srcH = _coverImage.Height;
                    srcW = (int)(srcH * rectAspect);
                    srcX = (_coverImage.Width - srcW) / 2;
                    srcY = 0;
                }
                else
                {
                    srcW = _coverImage.Width;
                    srcH = (int)(srcW / rectAspect);
                    srcX = 0;
                    srcY = (_coverImage.Height - srcH) / 2;
                }
                g.DrawImage(_coverImage, imageRect, srcX, srcY, srcW, srcH, GraphicsUnit.Pixel);
                g.ResetClip();
            }
            else
            {
                // Light placeholder
                using var bgBrush = new SolidBrush(Color.FromArgb(240, 238, 234));
                g.FillRectangle(bgBrush, imageRect);

                // Book icon
                int iconSize = 44;
                var iconRect = new Rectangle(
                    imageRect.X + (imageRect.Width - iconSize) / 2,
                    imageRect.Y + (imageRect.Height - iconSize) / 2,
                    iconSize, iconSize);
                using var iconPen = new Pen(Color.FromArgb(200, 200, 195), 2);
                g.DrawRectangle(iconPen, iconRect);

                // + symbol
                using var plusBrush = new SolidBrush(Color.FromArgb(180, 180, 175));
                g.DrawString("+", _placeholderFont, plusBrush, imageRect,
                    new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
            }

            // Thin colored line under image
            int lineY = cardRect.Y + pad + imageH + 6;
            var genreColor = GetGenreColor(_genre);
            using var linePen = new Pen(genreColor, 2.5f);
            g.DrawLine(linePen, cardRect.X + pad, lineY, cardRect.X + pad + 40, lineY);

            // Genre tag
            int textY = lineY + 8;
            if (!string.IsNullOrEmpty(_genre))
            {
                using var genreBrush = new SolidBrush(genreColor);
                g.DrawString(_genre.ToUpperInvariant(), _genreFont, genreBrush,
                    new PointF(cardRect.X + pad, textY));
                textY += 18;
            }

            // Title
            using (var brush = new SolidBrush(_textPrimary))
            {
                var titleRect = new RectangleF(cardRect.X + pad, textY, cardRect.Width - pad * 2, 28);
                var titleFormat = new StringFormat
                {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Near,
                    Trimming = StringTrimming.EllipsisCharacter,
                    FormatFlags = StringFormatFlags.NoWrap
                };
                g.DrawString(_title, _titleFont, brush, titleRect, titleFormat);
            }
            textY += 28;

            // Author
            using (var brush = new SolidBrush(_textSecondary))
            {
                var authorRect = new RectangleF(cardRect.X + pad, textY, cardRect.Width - pad * 2, 18);
                var authorFormat = new StringFormat
                {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Near,
                    Trimming = StringTrimming.EllipsisCharacter,
                    FormatFlags = StringFormatFlags.NoWrap
                };
                g.DrawString(_author, _authorFont, brush, authorRect, authorFormat);
            }

            // Price at bottom
            using (var brush = new SolidBrush(_accentCoral))
            {
                var priceFormat = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Far };
                var priceRect = new RectangleF(cardRect.X + pad, cardRect.Bottom - pad - 22, cardRect.Width - pad * 2, 22);
                g.DrawString(_price, _priceFont, brush, priceRect, priceFormat);
            }
        }

        private static Color GetGenreColor(string genre)
        {
            if (string.IsNullOrEmpty(genre)) return _accentCoral;
            int hash = genre.ToLowerInvariant().GetHashCode();
            return _genreColors[Math.Abs(hash) % _genreColors.Length];
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

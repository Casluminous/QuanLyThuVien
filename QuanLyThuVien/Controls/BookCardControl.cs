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
        private Color _gradientStart = Color.FromArgb(232, 132, 107);  // Coral
        private Color _gradientEnd = Color.FromArgb(210, 110, 85);    // Dark coral
        private bool _isHovered = false;

        public string Title { get => _title; set { _title = value; Invalidate(); } }
        public string Author { get => _author; set { _author = value; Invalidate(); } }
        public string Price { get => _price; set { _price = value; Invalidate(); } }
        public string Genre { get => _genre; set { _genre = value; Invalidate(); } }
        public Image? CoverImage { get => _coverImage; set { _coverImage = value; Invalidate(); } }
        public Color GradientStart { get => _gradientStart; set { _gradientStart = value; Invalidate(); } }
        public Color GradientEnd { get => _gradientEnd; set { _gradientEnd = value; Invalidate(); } }

        public int BorderRadius { get; set; } = 16;

        private static readonly Font _titleFont = new Font("Segoe UI", 12F, FontStyle.Bold);
        private static readonly Font _authorFont = new Font("Segoe UI", 10F, FontStyle.Regular);
        private static readonly Font _priceFont = new Font("Segoe UI", 13F, FontStyle.Bold);
        private static readonly Font _genreFont = new Font("Segoe UI", 9F, FontStyle.Regular);
        private static readonly Font _placeholderFont = new Font("Segoe UI", 28F, FontStyle.Bold);

        public BookCardControl()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);
            Size = new Size(250, 360);
            Cursor = Cursors.Hand;

            MouseEnter += (s, e) => { _isHovered = true; Invalidate(); };
            MouseLeave += (s, e) => { _isHovered = false; Invalidate(); };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int shadowOffset = _isHovered ? 8 : 4;
            var cardRect = new Rectangle(0, shadowOffset, Width, Height - shadowOffset);

            // Draw shadow
            for (int i = shadowOffset; i >= 1; i--)
            {
                var shadowRect = new Rectangle(i, shadowOffset + i, Width - i * 2, Height - shadowOffset - i * 2);
                using (var path = CreateRoundedPath(shadowRect, BorderRadius))
                using (var brush = new SolidBrush(Color.FromArgb(_isHovered ? 30 : 15, 0, 0, 0)))
                    g.FillPath(brush, path);
            }

            // Draw gradient background
            using (var path = CreateRoundedPath(cardRect, BorderRadius))
            {
                Region = new Region(path);
                using (var brush = new LinearGradientBrush(cardRect, _gradientStart, _gradientEnd, LinearGradientMode.Vertical))
                    g.FillPath(brush, path);
            }

            int padding = 14;
            int imageHeight = 160;
            var imageRect = new Rectangle(cardRect.X + padding, cardRect.Y + padding, cardRect.Width - padding * 2, imageHeight);

            // Draw image or placeholder
            if (_coverImage != null)
            {
                using (var imgPath = CreateRoundedPath(imageRect, 10))
                {
                    g.SetClip(imgPath);
                    // Center-crop the image
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
            }
            else
            {
                // Draw placeholder
                using (var brush = new SolidBrush(Color.FromArgb(40, 255, 255, 255)))
                    g.FillRectangle(brush, imageRect);

                using (var pen = new Pen(Color.FromArgb(60, 255, 255, 255), 2))
                {
                    pen.DashStyle = DashStyle.Dash;
                    g.DrawRectangle(pen, imageRect);
                }

                // Book icon
                var iconRect = new Rectangle(imageRect.X + imageRect.Width / 2 - 25, imageRect.Y + imageRect.Height / 2 - 30, 50, 50);
                using (var brush = new SolidBrush(Color.FromArgb(80, 255, 255, 255)))
                    g.FillRectangle(brush, iconRect);
                using (var pen = new Pen(Color.FromArgb(120, 255, 255, 255), 2))
                    g.DrawRectangle(pen, iconRect);

                using (var brush = new SolidBrush(Color.FromArgb(100, 255, 255, 255)))
                    g.DrawString("+", _placeholderFont, brush, imageRect, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
            }

            // Draw genre tag
            int textY = cardRect.Y + padding + imageHeight + 10;
            if (!string.IsNullOrEmpty(_genre))
            {
                var genreSize = g.MeasureString(_genre.ToUpperInvariant(), _genreFont);
                var genreRect = new RectangleF(cardRect.X + padding, textY, genreSize.Width + 12, genreSize.Height + 4);
                using (var brush = new SolidBrush(Color.FromArgb(50, 255, 255, 255)))
                    g.FillRectangle(brush, genreRect);
                using (var brush = new SolidBrush(Color.FromArgb(200, 255, 255, 255)))
                    g.DrawString(_genre.ToUpperInvariant(), _genreFont, brush, new PointF(cardRect.X + padding + 6, textY + 2));
                textY += (int)genreSize.Height + 10;
            }

            // Draw title
            using (var brush = new SolidBrush(Color.White))
            {
                var titleRect = new RectangleF(cardRect.X + padding, textY, cardRect.Width - padding * 2, 30);
                var titleFormat = new StringFormat
                {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Near,
                    Trimming = StringTrimming.EllipsisCharacter,
                    FormatFlags = StringFormatFlags.NoWrap
                };
                g.DrawString(_title, _titleFont, brush, titleRect, titleFormat);
            }
            textY += 30;

            // Draw author
            using (var brush = new SolidBrush(Color.FromArgb(180, 255, 255, 255)))
            {
                var authorRect = new RectangleF(cardRect.X + padding, textY, cardRect.Width - padding * 2, 20);
                var authorFormat = new StringFormat
                {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Near,
                    Trimming = StringTrimming.EllipsisCharacter,
                    FormatFlags = StringFormatFlags.NoWrap
                };
                g.DrawString(_author, _authorFont, brush, authorRect, authorFormat);
            }
            textY += 24;

            // Draw price
            using (var brush = new SolidBrush(Color.White))
            {
                g.DrawString(_price, _priceFont, brush, new PointF(cardRect.X + padding, textY));
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

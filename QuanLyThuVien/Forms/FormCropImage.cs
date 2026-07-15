using System.Drawing.Drawing2D;
using QuanLyThuVien.Helpers;

namespace QuanLyThuVien.Forms
{
    public class FormCropImage : Form
    {
        private Image _originalImage;
        private Rectangle _cropRect;
        private bool _isDragging = false;
        private Point _dragOffset;
        private Panel _picPreview;
        private float _scale;
        private int _offsetX, _offsetY;

        public Image? CroppedImage { get; private set; }

        public FormCropImage(Image image, float aspectRatio = 3f / 4f)
        {
            _originalImage = image;
            Text = "Cắt ảnh bìa";
            Size = new Size(600, 550);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;

            int maxW = 560, maxH = 380;
            float ratioW = (float)maxW / image.Width;
            float ratioH = (float)maxH / image.Height;
            _scale = Math.Min(ratioW, ratioH);

            int dispW = (int)(image.Width * _scale);
            int dispH = (int)(image.Height * _scale);
            _offsetX = (maxW - dispW) / 2;
            _offsetY = (maxH - dispH) / 2;

            _picPreview = new CropPanel
            {
                Size = new Size(maxW, maxH),
                Location = new Point(10, 10),
                BackColor = Color.FromArgb(240, 240, 240)
            };
            _picPreview.Paint += PicPreview_Paint;
            _picPreview.MouseDown += PicPreview_MouseDown;
            _picPreview.MouseMove += PicPreview_MouseMove;
            _picPreview.MouseUp += PicPreview_MouseUp;
            Controls.Add(_picPreview);

            // Initial crop rect (centered, 3:4 ratio)
            int cropW = dispW;
            int cropH = (int)(cropW / aspectRatio);
            if (cropH > dispH)
            {
                cropH = dispH;
                cropW = (int)(cropH * aspectRatio);
            }
            _cropRect = new Rectangle(
                _offsetX + (dispW - cropW) / 2,
                _offsetY + (dispH - cropH) / 2,
                cropW, cropH);

            var btnAccept = new Button
            {
                Text = "Chấp nhận",
                Size = new Size(120, 40),
                Location = new Point(200, 480),
                FlatStyle = FlatStyle.Flat,
                BackColor = AppColors.Primary,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAccept.Click += (s, e) =>
            {
                CroppedImage = CropImage();
                DialogResult = DialogResult.OK;
                Close();
            };
            Controls.Add(btnAccept);

            var btnCancel = new Button
            {
                Text = "Hủy",
                Size = new Size(100, 40),
                Location = new Point(340, 480),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(200, 200, 200),
                ForeColor = AppColors.TextPrimary,
                Font = new Font("Segoe UI", 10F),
                Cursor = Cursors.Hand
            };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(btnCancel);

            var lblHint = new Label
            {
                Text = "Kéo để di chuyển vùng cắt",
                Font = new Font("Segoe UI", 9F),
                ForeColor = AppColors.TextSecondary,
                AutoSize = true,
                Location = new Point(10, 495)
            };
            Controls.Add(lblHint);
        }

        private void PicPreview_Paint(object? sender, PaintEventArgs e)
        {
            if (_picPreview == null) return;
            var g = e.Graphics;
            g.Clear(Color.FromArgb(240, 240, 240));

            g.DrawImage(_originalImage, _offsetX, _offsetY, _picPreview.Width - _offsetX * 2, _picPreview.Height - _offsetY * 2);

            using var darkBrush = new SolidBrush(Color.FromArgb(120, 0, 0, 0));
            g.FillRectangle(darkBrush, 0, 0, _picPreview.Width, _cropRect.Y);
            g.FillRectangle(darkBrush, 0, _cropRect.Bottom, _picPreview.Width, _picPreview.Height - _cropRect.Bottom);
            g.FillRectangle(darkBrush, 0, _cropRect.Y, _cropRect.X, _cropRect.Height);
            g.FillRectangle(darkBrush, _cropRect.Right, _cropRect.Y, _picPreview.Width - _cropRect.Right, _cropRect.Height);

            using var pen = new Pen(Color.White, 2);
            g.DrawRectangle(pen, _cropRect);

            int hs = 8;
            using var handleBrush = new SolidBrush(Color.White);
            g.FillRectangle(handleBrush, _cropRect.X - hs / 2, _cropRect.Y - hs / 2, hs, hs);
            g.FillRectangle(handleBrush, _cropRect.Right - hs / 2, _cropRect.Y - hs / 2, hs, hs);
            g.FillRectangle(handleBrush, _cropRect.X - hs / 2, _cropRect.Bottom - hs / 2, hs, hs);
            g.FillRectangle(handleBrush, _cropRect.Right - hs / 2, _cropRect.Bottom - hs / 2, hs, hs);

            using var gridPen = new Pen(Color.FromArgb(80, 255, 255, 255), 1);
            int thirdW = _cropRect.Width / 3;
            int thirdH = _cropRect.Height / 3;
            for (int i = 1; i < 3; i++)
            {
                g.DrawLine(gridPen, _cropRect.X + thirdW * i, _cropRect.Y, _cropRect.X + thirdW * i, _cropRect.Bottom);
                g.DrawLine(gridPen, _cropRect.X, _cropRect.Y + thirdH * i, _cropRect.Right, _cropRect.Y + thirdH * i);
            }
        }

        private void PicPreview_MouseDown(object? sender, MouseEventArgs e)
        {
            if (_cropRect.Contains(e.Location))
            {
                _isDragging = true;
                _dragOffset = new Point(e.X - _cropRect.X, e.Y - _cropRect.Y);
                _picPreview.Cursor = Cursors.SizeAll;
            }
        }

        private void PicPreview_MouseMove(object? sender, MouseEventArgs e)
        {
            if (!_isDragging) return;

            int newX = e.X - _dragOffset.X;
            int newY = e.Y - _dragOffset.Y;

            // Clamp to image bounds
            newX = Math.Max(_offsetX, Math.Min(newX, _picPreview.Width - _offsetX - _cropRect.Width));
            newY = Math.Max(_offsetY, Math.Min(newY, _picPreview.Height - _offsetY - _cropRect.Height));

            _cropRect = new Rectangle(newX, newY, _cropRect.Width, _cropRect.Height);
            _picPreview.Invalidate();
        }

        private void PicPreview_MouseUp(object? sender, MouseEventArgs e)
        {
            _isDragging = false;
            _picPreview.Cursor = Cursors.Hand;
        }

        private Image CropImage()
        {
            // Convert screen coords to original image coords
            int imgAreaW = _picPreview.Width - _offsetX * 2;
            int imgAreaH = _picPreview.Height - _offsetY * 2;

            float ratioX = (float)_originalImage.Width / imgAreaW;
            float ratioY = (float)_originalImage.Height / imgAreaH;

            int srcX = (int)((_cropRect.X - _offsetX) * ratioX);
            int srcY = (int)((_cropRect.Y - _offsetY) * ratioY);
            int srcW = (int)(_cropRect.Width * ratioX);
            int srcH = (int)(_cropRect.Height * ratioY);

            // Clamp
            srcX = Math.Max(0, Math.Min(srcX, _originalImage.Width));
            srcY = Math.Max(0, Math.Min(srcY, _originalImage.Height));
            srcW = Math.Min(srcW, _originalImage.Width - srcX);
            srcH = Math.Min(srcH, _originalImage.Height - srcY);

            var bmp = new Bitmap(srcW, srcH);
            using var g = Graphics.FromImage(bmp);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(_originalImage, new Rectangle(0, 0, srcW, srcH), srcX, srcY, srcW, srcH, GraphicsUnit.Pixel);
            return bmp;
        }
    }

    internal class CropPanel : Panel
    {
        public CropPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }
    }
}
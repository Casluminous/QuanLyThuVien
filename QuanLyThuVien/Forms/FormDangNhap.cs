using System.Drawing.Drawing2D;
using QuanLyThuVien.Controls;
using QuanLyThuVien.Data;
using QuanLyThuVien.Helpers;

namespace QuanLyThuVien.Forms
{
    public class FormDangNhap : Form
    {
        private ModernTextBox txtTenDangNhap;
        private ModernTextBox txtMatKhau;
        private Label lblError;

        public FormDangNhap()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "Đăng nhập - Quản lý Thư viện";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.None;
            BackColor = Color.White;

            // Left panel - Login form
            var pnlLeft = new Panel
            {
                Dock = DockStyle.Left,
                Width = 380,
                BackColor = Color.White
            };

            var lblTitle = new Label
            {
                Text = "Đăng nhập",
                Font = new Font("Segoe UI", 22F, FontStyle.Bold),
                ForeColor = AppColors.TextPrimary,
                AutoSize = false,
                Size = new Size(300, 50),
                Location = new Point(40, 60),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var lblUser = new Label
            {
                Text = "TÊN NGƯỜI DÙNG",
                Font = new Font("Segoe UI", 9F),
                ForeColor = AppColors.TextSecondary,
                Location = new Point(40, 130),
                AutoSize = true
            };

            txtTenDangNhap = new ModernTextBox
            {
                Placeholder = "",
                Location = new Point(40, 155),
                Size = new Size(300, 45),
                Font = new Font("Segoe UI", 11F),
                PlaceholderColor = Color.Gray,
                BackColor = Color.FromArgb(245, 245, 245)
            };

            var lblPass = new Label
            {
                Text = "MẬT KHẨU",
                Font = new Font("Segoe UI", 9F),
                ForeColor = AppColors.TextSecondary,
                Location = new Point(40, 220),
                AutoSize = true
            };

            txtMatKhau = new ModernTextBox
            {
                Placeholder = "",
                Location = new Point(40, 245),
                Size = new Size(300, 45),
                Font = new Font("Segoe UI", 11F),
                PlaceholderColor = Color.Gray,
                UseSystemPasswordChar = true,
                BackColor = Color.FromArgb(245, 245, 245)
            };

            lblError = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9F),
                ForeColor = AppColors.Danger,
                Location = new Point(40, 300),
                AutoSize = true,
                Visible = false
            };

            // Circular arrow button
            var btnLogin = new PictureBox
            {
                Size = new Size(60, 60),
                Location = new Point(160, 370),
                BackColor = Color.FromArgb(245, 245, 245),
                Cursor = Cursors.Hand,
                SizeMode = PictureBoxSizeMode.CenterImage
            };

            var btnBitmap = new Bitmap(60, 60);
            using (var g = Graphics.FromImage(btnBitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.FromArgb(245, 245, 245));
                using var pen = new Pen(Color.FromArgb(220, 220, 220), 2);
                g.DrawEllipse(pen, 1, 1, 56, 56);
                using var arrowPen = new Pen(Color.FromArgb(150, 150, 150), 2.5f);
                g.DrawLine(arrowPen, 20, 30, 40, 30);
                g.DrawLine(arrowPen, 33, 23, 40, 30);
                g.DrawLine(arrowPen, 33, 37, 40, 30);
            }
            btnLogin.Image = btnBitmap;
            btnLogin.Click += BtnLogin_Click;

            var lblFooter = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8F),
                ForeColor = AppColors.TextSecondary,
                Location = new Point(40, 460),
                AutoSize = true
            };

            pnlLeft.Controls.AddRange(new Control[] { lblTitle, lblUser, txtTenDangNhap, lblPass, txtMatKhau, lblError, btnLogin, lblFooter });

            // Right panel - Banner
            var pnlRight = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 30, 40)
            };

            var picBanner = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(30, 30, 40)
            };
            var bannerPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "Banner", "banner1.jpg");
            if (File.Exists(bannerPath))
            {
                try
                {
                    using var fs = new FileStream(bannerPath, FileMode.Open, FileAccess.Read);
                    picBanner.Image = Image.FromStream(fs);
                }
                catch { }
            }
            pnlRight.Controls.Add(picBanner);

            // Set form size based on image
            int imgW = 693, imgH = 442;
            if (picBanner.Image != null)
            {
                imgW = picBanner.Image.Width;
                imgH = picBanner.Image.Height;
            }
            Size = new Size(380 + imgW, imgH);
            MaximumSize = Size;
            MinimumSize = Size;

            Controls.Add(pnlRight);
            Controls.Add(pnlLeft);

            // Drag form
            Point lastPoint = Point.Empty;
            pnlLeft.MouseDown += (s, e) => { lastPoint = e.Location; };
            pnlLeft.MouseMove += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                    Location = new Point(Location.X + e.X - lastPoint.X, Location.Y + e.Y - lastPoint.Y);
            };
            lblTitle.MouseDown += (s, e) => { lastPoint = e.Location; };
            lblTitle.MouseMove += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                    Location = new Point(Location.X + e.X - lastPoint.X, Location.Y + e.Y - lastPoint.Y);
            };
        }

        private void BtnLogin_Click(object? sender, EventArgs e)
        {
            string tdn = txtTenDangNhap.GetRealText();
            string mk = txtMatKhau.GetRealText();

            if (string.IsNullOrWhiteSpace(tdn) || string.IsNullOrWhiteSpace(mk))
            {
                ShowError("Vui lòng nhập đầy đủ thông tin!");
                return;
            }

            try
            {
                var nv = DataAccess.DangNhap(tdn, mk);
                if (nv == null)
                {
                    ShowError("Tên đăng nhập hoặc mật khẩu không đúng!");
                    return;
                }

                Session.CurrentUser = nv;
                var mainForm = new FormMain();
                mainForm.FormClosed += (s, args) => Close();
                mainForm.Show();
                Hide();
            }
            catch (Exception ex)
            {
                ShowError("Lỗi: " + ex.Message);
            }
        }

        private void ShowError(string msg)
        {
            lblError.Text = msg;
            lblError.Visible = true;
        }
    }
}
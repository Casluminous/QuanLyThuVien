using System.Drawing.Drawing2D;
using QuanLyThuVien.Controls;
using QuanLyThuVien.Helpers;

namespace QuanLyThuVien.Forms
{
    public class FormMain : Form
    {
        private Panel pnlTopNav;
        private Panel pnlContent;
        private Label lblUser;
        private Button? activeMenuButton;
        private const int TOPNAV_HEIGHT = 60;
        private Font _menuFontRegular = new Font("Segoe UI", 10F, FontStyle.Regular);
        private Font _menuFontBold = new Font("Segoe UI", 10F, FontStyle.Bold);

        public FormMain()
        {
            InitializeComponent();
            LoadDashboard();
        }

        private void InitializeComponent()
        {
            Text = "Quản lý Thư viện";
            Size = new Size(1280, 780);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.None;
            BackColor = Color.FromArgb(248, 246, 242);
            MinimumSize = new Size(1024, 600);

            // Top navigation bar
            pnlTopNav = new Panel
            {
                Height = TOPNAV_HEIGHT,
                Dock = DockStyle.Top,
                BackColor = Color.White,
                Padding = new Padding(20, 0, 20, 0)
            };

            // Logo
            var lblLogo = new Label
            {
                Text = "QLTV",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = AppColors.TextPrimary,
                AutoSize = true,
                Location = new Point(20, 18)
            };

            // Menu items
            string[] menuItems = { "Tổng quan", "Kho sách", "Mượn trả", "Trả sách", "Độc giả", "Thủ thư", "Danh mục", "Báo cáo" };
            string[] menuTags = { "Dashboard", "Sách", "Phiếu mượn", "Phiếu trả", "Độc giả", "Thủ thư", "Danh mục", "Báo cáo" };

            int xPos = 120;
            for (int i = 0; i < menuItems.Length; i++)
            {
                if (menuTags[i] == "Thủ thư" && !Session.IsAdmin)
                    continue;

                var btn = CreateMenuButton(menuItems[i], menuTags[i]);
                btn.Location = new Point(xPos, 0);
                pnlTopNav.Controls.Add(btn);
                xPos += btn.Width + 5;
            }

            // User info
            lblUser = new Label
            {
                Text = $"Phiên làm việc : {Session.CurrentUser?.HoTen}",
                Font = new Font("Segoe UI", 9F),
                ForeColor = AppColors.TextSecondary,
                AutoSize = true,
                Location = new Point(Width - 300, 22),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            // Minimize button
            var btnMinimize = new Button
            {
                Text = "\u2500",
                Size = new Size(35, 35),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                BackColor = Color.Transparent,
                ForeColor = AppColors.TextSecondary,
                Font = new Font("Segoe UI", 12F),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnMinimize.Location = new Point(Width - 120, 12);
            btnMinimize.Click += (s, e) => WindowState = FormWindowState.Minimized;

            // Maximize button
            var btnMaximize = new Button
            {
                Text = "\u25A1",
                Size = new Size(35, 35),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                BackColor = Color.Transparent,
                ForeColor = AppColors.TextSecondary,
                Font = new Font("Segoe UI", 12F),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnMaximize.Location = new Point(Width - 80, 12);
            btnMaximize.Click += (s, e) => WindowState = WindowState == FormWindowState.Normal ? FormWindowState.Maximized : FormWindowState.Normal;

            // Close button
            var btnClose = new Button
            {
                Text = "\u2715",
                Size = new Size(35, 35),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                BackColor = Color.Transparent,
                ForeColor = AppColors.TextSecondary,
                Font = new Font("Segoe UI", 12F),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnClose.Location = new Point(Width - 40, 12);
            btnClose.Click += (s, e) => Close();

            // Separator line under nav
            var pnlSeparator = new Panel
            {
                Height = 1,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(230, 230, 230)
            };

            pnlTopNav.Controls.AddRange(new Control[] { lblLogo, lblUser });

            // Content panel
            pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 246, 242),
                Padding = new Padding(0)
            };

            Controls.Add(pnlContent);
            Controls.Add(pnlSeparator);
            Controls.Add(pnlTopNav);

            // Window buttons in a dedicated panel on top of everything
            var pnlWindowBtns = new Panel
            {
                Size = new Size(120, 40),
                Location = new Point(Width - 140, 10),
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            pnlWindowBtns.Controls.Add(btnMinimize);
            pnlWindowBtns.Controls.Add(btnMaximize);
            pnlWindowBtns.Controls.Add(btnClose);
            btnMinimize.Location = new Point(0, 3);
            btnMaximize.Location = new Point(40, 3);
            btnClose.Location = new Point(80, 3);

            Controls.Add(pnlWindowBtns);
            pnlWindowBtns.BringToFront();

            // Drag form
            Point lastPoint = Point.Empty;
            pnlTopNav.MouseDown += (s, e) => { lastPoint = e.Location; };
            pnlTopNav.MouseMove += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    Location = new Point(Location.X + e.X - lastPoint.X, Location.Y + e.Y - lastPoint.Y);
                }
            };
            pnlTopNav.MouseDoubleClick += (s, e) =>
            {
                WindowState = WindowState == FormWindowState.Normal ? FormWindowState.Maximized : FormWindowState.Normal;
            };
        }

        private Button CreateMenuButton(string text, string tag)
        {
            var btn = new Button
            {
                Text = text,
                Size = new Size(text.Length * 10 + 30, TOPNAV_HEIGHT),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                ForeColor = AppColors.TextSecondary,
                BackColor = Color.Transparent,
                Font = _menuFontRegular,
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand,
                Tag = tag
            };
            btn.Click += MenuButton_Click;
            btn.MouseEnter += (s, e) =>
            {
                if (btn != activeMenuButton)
                    btn.ForeColor = AppColors.TextPrimary;
            };
            btn.MouseLeave += (s, e) =>
            {
                if (btn != activeMenuButton)
                    btn.ForeColor = AppColors.TextSecondary;
            };
            return btn;
        }

        private void MenuButton_Click(object? sender, EventArgs e)
        {
            if (sender is not Button btn) return;

            if (activeMenuButton != null)
            {
                activeMenuButton.ForeColor = AppColors.TextSecondary;
                activeMenuButton.Font = _menuFontRegular;
            }

            activeMenuButton = btn;
            btn.ForeColor = AppColors.TextPrimary;
            btn.Font = _menuFontBold;

            string tag = btn.Tag?.ToString() ?? "";
            switch (tag)
            {
                case "Dashboard": LoadDashboard(); break;
                case "Sách": LoadForm(new FormSach()); break;
                case "Thể loại": LoadForm(new FormTheLoai()); break;
                case "Nhà xuất bản": LoadForm(new FormNhaXuatBan()); break;
                case "Độc giả": LoadForm(new FormDocGia()); break;
                case "Thủ thư": LoadForm(new FormNhanVien()); break;
                case "Phiếu mượn": LoadForm(new FormPhieuMuon()); break;
                case "Phiếu trả": LoadForm(new FormPhieuTra()); break;
                case "Danh mục": LoadForm(new FormDanhMuc()); break;
                case "Báo cáo": LoadForm(new FormBaoCao()); break;
            }
        }

        private void LoadDashboard()
        {
            if (activeMenuButton != null)
            {
                activeMenuButton.ForeColor = AppColors.TextSecondary;
                activeMenuButton.Font = _menuFontRegular;
            }
            foreach (Control c in pnlTopNav.Controls)
            {
                if (c is Button b && b.Tag?.ToString() == "Dashboard")
                {
                    activeMenuButton = b;
                    b.ForeColor = AppColors.TextPrimary;
                    b.Font = _menuFontBold;
                }
            }
            var oldControls = new Control[pnlContent.Controls.Count];
            pnlContent.Controls.CopyTo(oldControls, 0);
            pnlContent.Controls.Clear();
            foreach (Control c in oldControls) c.Dispose();
            var dashboard = new FormDashboard { Dock = DockStyle.Fill };
            pnlContent.Controls.Add(dashboard);
        }

        private void LoadForm(UserControl control)
        {
            var oldControls = new Control[pnlContent.Controls.Count];
            pnlContent.Controls.CopyTo(oldControls, 0);
            pnlContent.Controls.Clear();
            foreach (Control c in oldControls) c.Dispose();
            control.Dock = DockStyle.Fill;
            pnlContent.Controls.Add(control);
        }
    }
}

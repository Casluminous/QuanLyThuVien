using System.Drawing.Drawing2D;
using QuanLyThuVien.Controls;
using QuanLyThuVien.Helpers;

using QuanLyThuVien.Services;

namespace QuanLyThuVien.Forms
{
    public class FormMain : Form
    {
        private Panel pnlTopNav = null!;
        private Panel pnlContent = null!;
        private FlowLayoutPanel pnlMenu = null!;
        private Label lblUser = null!;
        private ChatApiClient? _chatClient;
        private ChatPanel? _chatPanel;
        private ChatLauncherButton? _chatLauncher;
        private Button? activeMenuButton;
        private const int TOPNAV_HEIGHT = 72;
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
            ClientSize = new Size(1280, 780);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.None;
            BackColor = AppColors.ContentBg;
            MinimumSize = new Size(900, 600);

            // Top navigation bar
            pnlTopNav = new Panel
            {
                Height = TOPNAV_HEIGHT,
                Dock = DockStyle.Top,
                BackColor = AppColors.HeaderBg,
                Padding = new Padding(20, 0, 20, 0)
            };

            // Logo
            var lblLogo = new Label
            {
                Text = "QLTV",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = AppColors.TextPrimary,
                AutoSize = true,
                Location = new Point(20, 23)
            };

            pnlMenu = new FlowLayoutPanel
            {
                Location = new Point(110, 0),
                Size = new Size(Math.Max(280, Width - 520), TOPNAV_HEIGHT),
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Color.Transparent,
                Padding = new Padding(4, 0, 4, 0)
            };

            // Menu items
            string[] menuItems = { "Tổng quan", "Kho sách", "Mượn trả", "Trả sách", "Độc giả", "Thủ thư", "Danh mục", "Báo cáo" };
            string[] menuTags = { "Dashboard", "Sách", "Phiếu mượn", "Phiếu trả", "Độc giả", "Thủ thư", "Danh mục", "Báo cáo" };

            for (int i = 0; i < menuItems.Length; i++)
            {
                if (menuTags[i] == "Thủ thư" && !Session.IsAdmin)
                    continue;

                var btn = CreateMenuButton(menuItems[i], menuTags[i]);
                pnlMenu.Controls.Add(btn);
            }

            // User info
            lblUser = new Label
            {
                Text = $"Phiên làm việc : {Session.CurrentUser?.HoTen}",
                Font = new Font("Segoe UI", 9F),
                ForeColor = AppColors.TextSecondary,
                AutoSize = true,
                Location = new Point(Math.Max(260, Width - 360), 27),
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
                BackColor = AppColors.Border
            };

            pnlTopNav.Controls.AddRange(new Control[] { pnlMenu, lblLogo, lblUser });

            // Content panel
            pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppColors.ContentBg,
                Padding = new Padding(0)
            };

            Controls.Add(pnlContent);
            Controls.Add(pnlSeparator);
            Controls.Add(pnlTopNav);

            // Window buttons in a dedicated panel on top of everything
            var pnlWindowBtns = new Panel
            {
                Size = new Size(120, 40),
                Location = new Point(Width - 140, 16),
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

            if (IsChatAssistantEnabled())
                InitializeChat();

            Resize += (s, e) =>
            {
                lblUser.Visible = Width >= 1120;
                pnlMenu.Width = Math.Max(280, pnlTopNav.ClientSize.Width - (lblUser.Visible ? 520 : 260));
                LayoutChat();
            };

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

        private void InitializeChat()
        {
            _chatClient = new ChatApiClient();
            _chatLauncher = new ChatLauncherButton
            {
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            _chatLauncher.Click += (_, _) => OpenChat();

            _chatPanel = new ChatPanel(_chatClient, CloseChat, OpenBookFromChat)
            {
                Width = 400,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right,
                Visible = false
            };
            Controls.Add(_chatPanel);
            Controls.Add(_chatLauncher);
            LayoutChat();
        }

        private static bool IsChatAssistantEnabled()
        {
            string? configured = System.Configuration.ConfigurationManager.AppSettings["ChatAssistantEnabled"];
            return bool.TryParse(configured, out bool enabled) && enabled;
        }

        private void LayoutChat()
        {
            if (_chatPanel == null || _chatLauncher == null) return;
            const int margin = 16;
            _chatPanel.Width = Math.Min(400, Math.Max(320, ClientSize.Width - 32));
            _chatPanel.Height = Math.Max(300, ClientSize.Height - TOPNAV_HEIGHT - 16);
            _chatPanel.Location = new Point(ClientSize.Width - _chatPanel.Width - margin, TOPNAV_HEIGHT + 1);
            _chatLauncher.Location = new Point(ClientSize.Width - _chatLauncher.Width - margin, ClientSize.Height - _chatLauncher.Height - margin);
            _chatLauncher.BringToFront();
            if (_chatPanel.Visible) _chatPanel.BringToFront();
        }

        private void OpenChat()
        {
            if (_chatPanel == null || _chatLauncher == null) return;
            _chatPanel.Visible = true;
            _chatLauncher.Visible = false;
            _chatPanel.BringToFront();
            _chatPanel.FocusInput();
        }

        private void CloseChat()
        {
            if (_chatPanel == null || _chatLauncher == null) return;
            _chatPanel.Visible = false;
            _chatLauncher.Visible = true;
            _chatLauncher.Focus();
        }

        private void OpenBookFromChat(int maSach)
        {
            CloseChat();
            var books = new FormSach();
            LoadForm(books);
            books.OpenBookDetail(maSach);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _chatClient?.Dispose();
            base.OnFormClosed(e);
        }

        private Button CreateMenuButton(string text, string tag)
        {
            var btn = new Button
            {
                Text = text,
                Size = new Size(text.Length * 10 + 30, 48),
                Margin = new Padding(0, 4, 0, 0),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                ForeColor = AppColors.TextSecondary,
                BackColor = Color.Transparent,
                Font = _menuFontRegular,
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand,
                Tag = tag,
                TabStop = true,
                AccessibleRole = AccessibleRole.PushButton,
                AccessibleName = text
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
            string tag = btn.Tag?.ToString() ?? "";
            NavigateTo(tag);
        }

        private void LoadDashboard()
        {
            NavigateTo("Dashboard");
        }

        private void NavigateTo(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return;
            if (tag == "Thủ thư" && !Session.IsAdmin)
            {
                System.Diagnostics.Debug.WriteLine("Blocked navigation to Thủ thư for non-admin session.");
                NavigateTo("Danh mục");
                return;
            }

            if (tag == "Dashboard")
            {
                SetActiveMenu("Dashboard");
                var dashboard = new FormDashboard { Dock = DockStyle.Fill };
                ReplaceContent(dashboard);
                return;
            }

            UserControl? destination = null;
            try
            {
                destination = tag switch
                {
                    "Sách" => new FormSach(),
                    "Thể loại" => new FormTheLoai(),
                    "Tác giả" => new FormTacGia(),
                    "Nhà xuất bản" => new FormNhaXuatBan(),
                    "Độc giả" => new FormDocGia(),
                    "Thủ thư" => new FormNhanVien(),
                    "Phiếu mượn" => new FormPhieuMuon(),
                    "Phiếu trả" => new FormPhieuTra(),
                    "Danh mục" => CreateCategoryLauncher(),
                    "Báo cáo" => new FormBaoCao(),
                    _ => null
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation failed for '{tag}': {ex}");
                MessageBox.Show(
                    this,
                    "Không thể mở trang này. Vui lòng thử lại.",
                    "Không thể điều hướng",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (destination == null)
            {
                System.Diagnostics.Debug.WriteLine($"Unknown navigation tag: '{tag}'.");
                return;
            }

            SetActiveMenu(tag);
            ReplaceContent(destination);
        }

        private FormDanhMuc CreateCategoryLauncher()
        {
            var launcher = new FormDanhMuc();
            launcher.NavigationRequested += childTag => NavigateTo(childTag);
            return launcher;
        }

        private void SetActiveMenu(string destinationTag)
        {
            string activeTag = destinationTag switch
            {
                "Thể loại" or "Tác giả" or "Nhà xuất bản" => "Danh mục",
                _ => destinationTag
            };

            if (activeMenuButton != null && activeMenuButton.Tag?.ToString() == activeTag)
                return;

            if (activeMenuButton != null)
            {
                activeMenuButton.ForeColor = AppColors.TextSecondary;
                activeMenuButton.Font = _menuFontRegular;
                activeMenuButton.BackColor = Color.Transparent;
            }

            activeMenuButton = pnlMenu.Controls
                .OfType<Button>()
                .FirstOrDefault(button => button.Tag?.ToString() == activeTag);
            if (activeMenuButton == null) return;

            activeMenuButton.ForeColor = AppColors.TextPrimary;
            activeMenuButton.Font = _menuFontBold;
            activeMenuButton.BackColor = AppColors.PrimaryLight;
        }

        private void LoadForm(UserControl control) => ReplaceContent(control);

        private void ReplaceContent(UserControl control)
        {
            ResponsiveUi.DisposeChildren(pnlContent);
            control.Dock = DockStyle.Fill;
            pnlContent.Controls.Add(control);
        }
    }
}

using QuanLyThuVien.Controls;
using QuanLyThuVien.Data;
using QuanLyThuVien.Helpers;

namespace QuanLyThuVien.Forms
{
    public class FormDangNhap : Form
    {
        private ModernTextBox txtTenDangNhap = null!;
        private ModernTextBox txtMatKhau = null!;
        private ModernButton btnLogin = null!;
        private Label lblError = null!;
        private readonly ToolTip _toolTip = new();
        private bool _isAuthenticating;
        private Point _dragMouseStart;
        private Point _dragFormStart;

        public FormDangNhap()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            Text = "Đăng nhập - Quản lý Thư viện";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.None;
            BackColor = AppColors.WorkbenchBg;
            ClientSize = new Size(920, 560);
            MinimumSize = new Size(760, 500);
            AutoScaleMode = AutoScaleMode.Font;
            KeyPreview = true;

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                BackColor = AppColors.WorkbenchBg
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var identityPane = BuildIdentityPane();
            var formPane = BuildFormPane(out ModernButton btnClose);
            root.Controls.Add(identityPane, 0, 0);
            root.Controls.Add(formPane, 1, 0);
            Controls.Add(root);

            AcceptButton = btnLogin;
            CancelButton = btnClose;
            Shown += (_, _) => txtTenDangNhap.FocusInput();

            ResumeLayout(true);
        }

        private Control BuildIdentityPane()
        {
            var pane = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppColors.SidebarBg,
                Padding = new Padding(48, 36, 40, 32),
                Margin = Padding.Empty,
                AccessibleRole = AccessibleRole.Grouping,
                AccessibleName = "Nhận diện hệ thống Quản lý Thư viện"
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.Transparent,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));

            var wordmark = new Label
            {
                Text = "QLTV",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = AppColors.TextLight,
                TextAlign = ContentAlignment.TopLeft,
                BackColor = Color.Transparent,
                AccessibleName = "QLTV"
            };

            var message = new TableLayoutPanel
            {
                Anchor = AnchorStyles.Left,
                Width = 300,
                Height = 205,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = Color.Transparent,
                Margin = Padding.Empty
            };
            message.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            message.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            message.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            message.RowStyles.Add(new RowStyle(SizeType.Absolute, 92F));
            message.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var accent = new Panel
            {
                Width = 48,
                Height = 3,
                Anchor = AnchorStyles.Left,
                BackColor = AppColors.Accent,
                Margin = Padding.Empty
            };
            var eyebrow = new Label
            {
                Text = "QUẢN LÝ THƯ VIỆN",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                ForeColor = AppColors.SidebarText,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };
            var title = new Label
            {
                Text = "Một ngày đọc sách\nbắt đầu từ đây.",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 25F, FontStyle.Bold),
                ForeColor = AppColors.TextLight,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };
            var description = new Label
            {
                Text = "Đăng nhập để tiếp tục ca làm việc và theo dõi những đầu việc cần chú ý trong ngày.",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F),
                ForeColor = AppColors.SidebarText,
                TextAlign = ContentAlignment.TopLeft,
                BackColor = Color.Transparent
            };
            message.Controls.Add(accent, 0, 0);
            message.Controls.Add(eyebrow, 0, 1);
            message.Controls.Add(title, 0, 2);
            message.Controls.Add(description, 0, 3);

            var footer = new Label
            {
                Text = "HỆ THỐNG NỘI BỘ · LIBRARY TEAL",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                ForeColor = AppColors.SidebarText,
                TextAlign = ContentAlignment.BottomLeft,
                BackColor = Color.Transparent
            };

            layout.Controls.Add(wordmark, 0, 0);
            layout.Controls.Add(message, 0, 1);
            layout.Controls.Add(footer, 0, 2);
            pane.Controls.Add(layout);
            AttachWindowDrag(pane);
            return pane;
        }

        private Control BuildFormPane(out ModernButton btnClose)
        {
            var pane = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppColors.WorkbenchBg,
                Margin = Padding.Empty,
                AccessibleRole = AccessibleRole.Grouping,
                AccessibleName = "Biểu mẫu đăng nhập"
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.Transparent,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));

            var closeBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Padding = new Padding(0, 10, 12, 0),
                Margin = Padding.Empty,
                BackColor = Color.Transparent
            };
            btnClose = new ModernButton
            {
                Text = "×",
                Size = new Size(44, 44),
                MinimumSize = new Size(44, 44),
                Font = new Font("Segoe UI", 16F, FontStyle.Regular),
                BaseColor = Color.Transparent,
                HoverColor = AppColors.WorkbenchMuted,
                PressedColor = AppColors.Border,
                TextColor = AppColors.TextPrimary,
                BorderRadius = 10,
                AccessibleName = "Đóng cửa sổ đăng nhập",
                TabIndex = 4,
                DialogResult = DialogResult.Cancel,
                Margin = Padding.Empty
            };
            btnClose.Click += (_, _) => Close();
            _toolTip.SetToolTip(btnClose, "Đóng");
            closeBar.Controls.Add(btnClose);

            var form = CreateLoginForm();
            layout.Controls.Add(closeBar, 0, 0);
            layout.Controls.Add(form, 0, 1);
            pane.Controls.Add(layout);
            return pane;
        }

        private Control CreateLoginForm()
        {
            var form = new TableLayoutPanel
            {
                Anchor = AnchorStyles.None,
                Size = new Size(380, 390),
                ColumnCount = 1,
                RowCount = 10,
                BackColor = Color.Transparent,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 12F));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));

            var title = new Label
            {
                Text = "Chào mừng trở lại",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = AppColors.TextPrimary,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };
            var subtitle = new Label
            {
                Text = "Dùng tài khoản được cấp để vào hệ thống.",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F),
                ForeColor = AppColors.TextSecondary,
                TextAlign = ContentAlignment.TopLeft,
                BackColor = Color.Transparent
            };

            var userLabel = CreateFieldLabel("Tên đăng nhập");
            txtTenDangNhap = CreateInput("Tên đăng nhập", false, 0);
            var passwordLabel = CreateFieldLabel("Mật khẩu");
            txtMatKhau = CreateInput("Mật khẩu", true, 1);

            var showPassword = new CheckBox
            {
                Text = "Hiện mật khẩu",
                Dock = DockStyle.Fill,
                AutoSize = false,
                Font = new Font("Segoe UI", 9F),
                ForeColor = AppColors.TextSecondary,
                BackColor = Color.Transparent,
                CheckAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleLeft,
                AccessibleName = "Hiện mật khẩu",
                TabIndex = 2
            };
            showPassword.CheckedChanged += (_, _) => txtMatKhau.UseSystemPasswordChar = !showPassword.Checked;

            lblError = new Label
            {
                Text = string.Empty,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F),
                ForeColor = AppColors.Danger,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent,
                AutoEllipsis = true,
                AccessibleRole = AccessibleRole.Alert,
                AccessibleName = "Lỗi đăng nhập"
            };

            btnLogin = new ModernButton
            {
                Text = "Đăng nhập",
                Dock = DockStyle.Fill,
                MinimumSize = new Size(120, 44),
                BaseColor = AppColors.Primary,
                HoverColor = AppColors.PrimaryDark,
                PressedColor = Color.FromArgb(19, 78, 74),
                TextColor = AppColors.WorkbenchSurface,
                BorderRadius = 10,
                AccessibleName = "Đăng nhập",
                TabIndex = 3,
                Margin = Padding.Empty
            };
            btnLogin.Click += BtnLogin_Click;

            form.Controls.Add(title, 0, 0);
            form.Controls.Add(subtitle, 0, 1);
            form.Controls.Add(userLabel, 0, 2);
            form.Controls.Add(txtTenDangNhap, 0, 3);
            form.Controls.Add(passwordLabel, 0, 5);
            form.Controls.Add(txtMatKhau, 0, 6);
            form.Controls.Add(showPassword, 0, 7);
            form.Controls.Add(lblError, 0, 8);
            form.Controls.Add(btnLogin, 0, 9);
            return form;
        }

        private static Label CreateFieldLabel(string text) => new()
        {
            Text = text,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
            ForeColor = AppColors.TextPrimary,
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.Transparent
        };

        private static ModernTextBox CreateInput(string accessibleName, bool password, int tabIndex) => new()
        {
            Placeholder = string.Empty,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 11F),
            BackColor = AppColors.WorkbenchSurface,
            BorderColor = AppColors.Border,
            FocusedBorderColor = AppColors.Focus,
            PlaceholderColor = AppColors.TextMuted,
            BorderRadius = 10,
            UseSystemPasswordChar = password,
            AccessibleName = accessibleName,
            TabIndex = tabIndex,
            Margin = Padding.Empty
        };

        private async void BtnLogin_Click(object? sender, EventArgs e)
        {
            if (_isAuthenticating) return;

            string username = txtTenDangNhap.GetRealText().Trim();
            string password = txtMatKhau.GetRealText();
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ShowError("Nhập đầy đủ tên đăng nhập và mật khẩu.");
                (string.IsNullOrWhiteSpace(username) ? txtTenDangNhap : txtMatKhau).FocusInput();
                return;
            }

            ShowError(string.Empty);
            SetLoginBusy(true);
            try
            {
                var employee = await Task.Run(() => DataAccess.DangNhap(username, password));
                if (employee == null)
                {
                    ShowError("Tên đăng nhập hoặc mật khẩu không đúng. Hãy kiểm tra và thử lại.");
                    txtMatKhau.FocusInput();
                    return;
                }

                Session.CurrentUser = employee;
                var mainForm = new FormMain();
                mainForm.FormClosed += (_, _) =>
                {
                    if (!IsDisposed) Close();
                };
                mainForm.Show();
                Hide();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Đăng nhập thất bại: {ex}");
                ShowError("Không thể kết nối để đăng nhập. Hãy kiểm tra SQL Server rồi thử lại.");
            }
            finally
            {
                if (!IsDisposed)
                    SetLoginBusy(false);
            }
        }

        private void SetLoginBusy(bool busy)
        {
            _isAuthenticating = busy;
            btnLogin.Enabled = !busy;
            btnLogin.Text = busy ? "Đang đăng nhập…" : "Đăng nhập";
            UseWaitCursor = busy;
        }

        private void ShowError(string message)
        {
            lblError.Text = message;
            lblError.AccessibleDescription = message;
        }

        private void AttachWindowDrag(Control root)
        {
            root.MouseDown += WindowDrag_MouseDown;
            root.MouseMove += WindowDrag_MouseMove;
            foreach (Control child in root.Controls)
                AttachWindowDrag(child);
        }

        private void WindowDrag_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            _dragMouseStart = Cursor.Position;
            _dragFormStart = Location;
        }

        private void WindowDrag_MouseMove(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            Point current = Cursor.Position;
            Location = new Point(
                _dragFormStart.X + current.X - _dragMouseStart.X,
                _dragFormStart.Y + current.Y - _dragMouseStart.Y);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _toolTip.Dispose();
            base.Dispose(disposing);
        }
    }
}

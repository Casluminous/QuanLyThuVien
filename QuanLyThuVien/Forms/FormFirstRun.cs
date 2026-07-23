using QuanLyThuVien.Controls;
using QuanLyThuVien.Data;
using QuanLyThuVien.Helpers;

namespace QuanLyThuVien.Forms
{
    public class FormFirstRun : Form
    {
        private ModernTextBox txtHoTen = null!;
        private ModernTextBox txtTenDN = null!;
        private ModernTextBox txtMatKhau = null!;
        private ModernTextBox txtXacNhan = null!;
        private Label lblError = null!;

        public FormFirstRun()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "Thiết lập lần đầu - Quản lý Thư viện";
            ClientSize = new Size(480, 500);
            MinimumSize = new Size(440, 460);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = false;
            BackColor = AppColors.ContentBg;

            var lblTitle = new Label
            {
                Text = "Thiết lập tài khoản Admin đầu tiên",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = AppColors.TextPrimary,
                AutoSize = true,
                Location = new Point(40, 20)
            };

            var lblInfo = new Label
            {
                Text = "Chưa có nhân viên nào trong hệ thống.\nVui lòng tạo tài khoản Admin để bắt đầu.",
                Font = new Font("Segoe UI", 9F),
                ForeColor = AppColors.TextSecondary,
                AutoSize = true,
                Location = new Point(40, 55)
            };

            var lbl1 = new Label { Text = "Họ tên:", Location = new Point(40, 100), AutoSize = true };
            txtHoTen = new ModernTextBox { Location = new Point(40, 125), Size = new Size(400, 35), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

            var lbl2 = new Label { Text = "Tên đăng nhập:", Location = new Point(40, 170), AutoSize = true };
            txtTenDN = new ModernTextBox { Location = new Point(40, 195), Size = new Size(400, 35), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

            var lbl3 = new Label { Text = "Mật khẩu:", Location = new Point(40, 240), AutoSize = true };
            txtMatKhau = new ModernTextBox { Location = new Point(40, 265), Size = new Size(400, 35), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, UseSystemPasswordChar = true };

            var lbl4 = new Label { Text = "Xác nhận mật khẩu:", Location = new Point(40, 310), AutoSize = true };
            txtXacNhan = new ModernTextBox { Location = new Point(40, 335), Size = new Size(400, 35), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, UseSystemPasswordChar = true };

            lblError = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9F),
                ForeColor = AppColors.Danger,
                Location = new Point(40, 382),
                AutoSize = true,
                Visible = false
            };

            var btnCreate = new ModernButton
            {
                Text = "Tạo tài khoản",
                Location = new Point(160, 430),
                Size = new Size(160, 42),
                Anchor = AnchorStyles.Bottom,
                BaseColor = AppColors.Primary,
                BorderRadius = 12
            };
            btnCreate.Click += BtnCreate_Click;

            AcceptButton = btnCreate;
            txtHoTen.AccessibleName = "Họ tên";
            txtTenDN.AccessibleName = "Tên đăng nhập";
            txtMatKhau.AccessibleName = "Mật khẩu";
            txtXacNhan.AccessibleName = "Xác nhận mật khẩu";

            Controls.AddRange(new Control[] { lblTitle, lblInfo, lbl1, txtHoTen, lbl2, txtTenDN, lbl3, txtMatKhau, lbl4, txtXacNhan, lblError, btnCreate });
            ActiveControl = txtHoTen;
        }

        private void BtnCreate_Click(object? sender, EventArgs e)
        {
            string hoTen = txtHoTen.Text.Trim();
            string tenDN = txtTenDN.Text.Trim();
            string mk = txtMatKhau.GetRealText();
            string xacNhan = txtXacNhan.GetRealText();

            if (string.IsNullOrWhiteSpace(hoTen))
            { ShowError("Vui lòng nhập họ tên!"); return; }

            if (string.IsNullOrWhiteSpace(tenDN))
            { ShowError("Vui lòng nhập tên đăng nhập!"); return; }

            if (string.IsNullOrWhiteSpace(mk))
            { ShowError("Vui lòng nhập mật khẩu!"); return; }

            if (mk != xacNhan)
            { ShowError("Mật khẩu xác nhận không khớp!"); return; }

            if (mk.Length < 6)
            { ShowError("Mật khẩu phải có ít nhất 6 ký tự!"); return; }

            try
            {
                DataAccess.InsertNhanVien(hoTen, tenDN, mk, "Admin");
                MessageBox.Show("Tạo tài khoản Admin thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Tạo Admin đầu tiên thất bại: {ex}");
                ShowError("Không thể tạo tài khoản. Vui lòng kiểm tra kết nối và dữ liệu.");
            }
        }

        private void ShowError(string msg)
        {
            lblError.Text = msg;
            lblError.Visible = true;
        }
    }
}

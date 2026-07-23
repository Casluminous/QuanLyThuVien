using System.Data;
using QuanLyThuVien.Controls;
using QuanLyThuVien.Data;
using QuanLyThuVien.Helpers;

namespace QuanLyThuVien.Forms
{
    public class FormNhanVien : UserControl
    {
        private DataGridView dgv = null!;

        public FormNhanVien()
        {
            BackColor = AppColors.ContentBg;
            Padding = new Padding(10);
            Load += (s, e) => LoadData();
        }

        private void LoadData()
        {
            ResponsiveUi.DisposeChildren(this);

            if (!Session.IsAdmin)
            {
                Controls.Add(new Label
                {
                    Text = "Chỉ Admin mới có quyền quản lý nhân viên!",
                    Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                    ForeColor = AppColors.Danger,
                    AutoSize = true,
                    Location = new Point(10, 10)
                });
                return;
            }

            var btnThem = PageHeader.CreatePrimaryAction("+ Th\u00eam th\u1ee7 th\u01b0", (_, _) => ShowInputDialog(), 150);

            dgv = new ModernDataGridView
            {
                Location = new Point(10, 105),
                Size = new Size(Width - 30, Height - 120),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                BackgroundColor = AppColors.CardBg,
                BorderStyle = BorderStyle.None,
                GridColor = AppColors.Border,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = AppColors.Primary,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold)
                },
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 40,
                DefaultCellStyle = new DataGridViewCellStyle { Font = new Font("Segoe UI", 10F) }
            };
            dgv.Columns.Add("MaNV", "M\u00e3");
            dgv.Columns.Add("HoTen", "H\u1ecd t\u00ean");
            dgv.Columns.Add("TenDangNhap", "T\u00ean \u0111\u0103ng nh\u1eadp");
            dgv.Columns.Add("VaiTro", "Vai tr\u00f2");
            dgv.Columns.Add("TrangThai", "Tr\u1ea1ng th\u00e1i");
            dgv.Columns.Add("btnS\u1eeda", "S\u1eeda");
            dgv.Columns.Add("btnX\u00f3a", "X\u00f3a");
            dgv.CellClick += Dgv_CellClick;
            ResponsiveUi.AddListPage(this, dgv, "Qu\u1ea3n l\u00fd Th\u1ee7 th\u01b0", btnThem);

            try
            {
                var dt = DataAccess.GetAllNhanVien();
                foreach (DataRow row in dt.Rows)
                {
                    bool tt = (bool)row["TrangThai"];
                    dgv.Rows.Add(row["MaNV"], row["HoTen"], row["TenDangNhap"],
                        row["VaiTro"], tt ? "Ho\u1ea1t \u0111\u1ed9ng" : "Kh\u00f3a",
                        "S\u1eeda", "X\u00f3a");
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Tải nhân viên thất bại: {ex}"); MessageBox.Show("Không thể tải dữ liệu nhân viên.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void Dgv_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            int maNV = Convert.ToInt32(dgv.Rows[e.RowIndex].Cells["MaNV"].Value);
            var row = dgv.Rows[e.RowIndex];
            string columnName = dgv.Columns[e.ColumnIndex].Name;

            if (columnName == "btnS\u1eeda")
            {
                ShowInputDialog(maNV,
                    row.Cells["HoTen"].Value?.ToString() ?? "",
                    row.Cells["TenDangNhap"].Value?.ToString() ?? "",
                    row.Cells["VaiTro"].Value?.ToString() ?? "NhanVien",
                    row.Cells["TrangThai"].Value?.ToString() == "Ho\u1ea1t \u0111\u1ed9ng");
            }
            else if (columnName == "btnX\u00f3a" && MessageBox.Show("X\u00f3a nh\u00e2n vi\u00ean n\u00e0y?", "X\u00e1c nh\u1eadn", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                try
                {
                    if (!DataAccess.TryDeleteNhanVien(maNV, Session.CurrentUser!.MaNV, out string? reason))
                    {
                        MessageBox.Show(reason ?? "Không thể xóa nhân viên này.", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    LoadData();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Xóa nhân viên thất bại: {ex}");
                    MessageBox.Show("Không thể xóa nhân viên này!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ShowInputDialog(int? maNV = null, string hoTen = "", string tenDN = "", string vaiTro = "NhanVien", bool trangThai = true)
        {
            if (!Session.IsAdmin)
            {
                MessageBox.Show("Chỉ Admin mới có quyền quản lý nhân viên!", "Không có quyền", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var frm = new Form
            {
                Text = maNV.HasValue ? "S\u1eeda nh\u00e2n vi\u00ean" : "Th\u00eam nh\u00e2n vi\u00ean",
                ClientSize = new Size(400, 350),
                MinimumSize = new Size(370, 320),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.Sizable,
                MaximizeBox = true,
                MinimizeBox = false
            };

            var lbl1 = new Label { Text = "H\u1ecd t\u00ean:", Location = new Point(20, 20), AutoSize = true };
            var txt1 = new ModernTextBox { Text = hoTen, Location = new Point(150, 17), Size = new Size(210, 30), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

            var lbl2 = new Label { Text = "T\u00ean \u0111\u0103ng nh\u1eadp:", Location = new Point(20, 60), AutoSize = true };
            var txt2 = new ModernTextBox { Text = tenDN, Location = new Point(150, 57), Size = new Size(210, 30), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

            var lbl3 = new Label { Text = "M\u1eadt kh\u1ea9u:", Location = new Point(20, 100), AutoSize = true };
            var txt3 = new ModernTextBox { Location = new Point(150, 97), Size = new Size(210, 30), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, UseSystemPasswordChar = true };
            if (maNV.HasValue) txt3.Placeholder = "Giữ trống nếu không đổi";

            var lbl4 = new Label { Text = "Vai tr\u00f2:", Location = new Point(20, 140), AutoSize = true };
            var cboVT = new ModernComboBox { Location = new Point(150, 137), Size = new Size(210, 30), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, DropDownStyle = ComboBoxStyle.DropDownList };
            cboVT.Items.AddRange(new object[] { "Admin", "NhanVien" });
            cboVT.SelectedItem = vaiTro;

            var lbl5 = new Label { Text = "Tr\u1ea1ng th\u00e1i:", Location = new Point(20, 180), AutoSize = true };
            var chkTT = new CheckBox { Text = "Ho\u1ea1t \u0111\u1ed9ng", Location = new Point(150, 178), Checked = trangThai, AutoSize = true };

            var btnOk = new ModernButton
            {
                Text = "L\u01b0u", Location = new Point(100, 230), Size = new Size(100, 38), Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                BaseColor = AppColors.Primary, BorderRadius = 12
            };
            var btnCancel = new ModernButton
            {
                Text = "H\u1ee7y", Location = new Point(220, 230), Size = new Size(100, 38), Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                BaseColor = AppColors.TextSecondary, BorderRadius = 12
            };

            btnOk.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txt1.Text)) { MessageBox.Show("Nh\u1eadp h\u1ecd t\u00ean!"); return; }
                if (string.IsNullOrWhiteSpace(txt2.Text)) { MessageBox.Show("Nh\u1eadp t\u00ean \u0111\u0103ng nh\u1eadp!"); return; }

                try
                {
                    if (maNV.HasValue)
                    {
                        string? newPassword = string.IsNullOrWhiteSpace(txt3.GetRealText()) ? null : txt3.GetRealText();
                        if (!DataAccess.TryUpdateNhanVien(maNV.Value, txt1.Text.Trim(), txt2.Text.Trim(), cboVT.SelectedItem?.ToString() ?? "NhanVien", chkTT.Checked,
                            newPassword, Session.CurrentUser!.MaNV, out string? reason))
                        {
                            MessageBox.Show(reason ?? "Không thể cập nhật nhân viên.", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(txt3.GetRealText())) { MessageBox.Show("Nh\u1eadp m\u1eadt kh\u1ea9u!"); return; }
                        DataAccess.InsertNhanVien(txt1.Text.Trim(), txt2.Text.Trim(), txt3.GetRealText(), cboVT.SelectedItem?.ToString() ?? "NhanVien");
                    }
                    frm.Close();
                    LoadData();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Lưu nhân viên thất bại: {ex}");
                    MessageBox.Show("Không thể lưu thông tin nhân viên. Vui lòng kiểm tra dữ liệu và thử lại.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            btnCancel.Click += (s, e) => frm.Close();

            frm.Controls.AddRange(new Control[] { lbl1, txt1, lbl2, txt2, lbl3, txt3, lbl4, cboVT, lbl5, chkTT, btnOk, btnCancel });
            frm.AcceptButton = btnOk;
            frm.CancelButton = btnCancel;
            frm.ActiveControl = txt1;
            txt1.AccessibleName = "Họ tên nhân viên";
            txt2.AccessibleName = "Tên đăng nhập";
            txt3.AccessibleName = "Mật khẩu";
            frm.ShowDialog();
        }
    }
}


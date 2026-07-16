using System.Data;
using QuanLyThuVien.Controls;
using QuanLyThuVien.Data;
using QuanLyThuVien.Helpers;

namespace QuanLyThuVien.Forms
{
    public class FormNhanVien : UserControl
    {
        private DataGridView dgv;

        public FormNhanVien()
        {
            BackColor = AppColors.ContentBg;
            Padding = new Padding(10);
            Load += (s, e) => LoadData();
            Resize += (s, e) => { if (dgv != null) { dgv.Width = Width - 30; dgv.Height = Height - 120; } };
        }

        private void LoadData()
        {
            Controls.Clear();

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

            Controls.Add(new Label
            {
                Text = "Qu\u1ea3n l\u00fd Th\u1ee7 th\u01b0",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = AppColors.TextPrimary,
                AutoSize = true,
                Location = new Point(10, 10)
            });

            var btnThem = new ModernButton
            {
                Text = "+ Th\u00eam m\u1edbi",
                Location = new Point(10, 55),
                Size = new Size(130, 38),
                BaseColor = AppColors.Success,
                HoverColor = Color.FromArgb(39, 174, 96),
                BorderRadius = 8
            };
            btnThem.Click += (s, e) => ShowInputDialog();
            Controls.Add(btnThem);

            dgv = new ModernDataGridView
            {
                Location = new Point(10, 105),
                Size = new Size(Width - 30, Height - 120),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                GridColor = Color.FromArgb(230, 230, 230),
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
            Controls.Add(dgv);

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
            catch (Exception ex) { MessageBox.Show("L\u1ed7i thao t\u00e1c: " + ex.Message, "L\u1ed7i", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void Dgv_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            int maNV = Convert.ToInt32(dgv.Rows[e.RowIndex].Cells["MaNV"].Value);
            var selectedRow = dgv.Rows[e.RowIndex];

            if (dgv.Columns[e.ColumnIndex].Name == "btnS\u1eeda")
            {
                var row = dgv.Rows[e.RowIndex];
                ShowInputDialog(maNV,
                    row.Cells["HoTen"].Value?.ToString() ?? "",
                    row.Cells["TenDangNhap"].Value?.ToString() ?? "",
                    row.Cells["VaiTro"].Value?.ToString() ?? "NhanVien",
                    row.Cells["TrangThai"].Value?.ToString() == "Ho\u1ea1t \u0111\u1ed9ng");
            }
            else if (dgv.Columns[e.ColumnIndex].Name == "btnX\u00f3a")
            {
                if (maNV == Session.CurrentUser!.MaNV)
                {
                    MessageBox.Show("Không thể xóa tài khoản đang đăng nhập!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                bool deletingActiveAdmin =
                    selectedRow.Cells["VaiTro"].Value?.ToString() == "Admin" &&
                    selectedRow.Cells["TrangThai"].Value?.ToString() == "Hoạt động";

                if (deletingActiveAdmin && DataAccess.CountActiveAdmins() <= 1)
                {
                    MessageBox.Show("Không thể xóa admin cuối cùng!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (MessageBox.Show("X\u00f3a nh\u00e2n vi\u00ean n\u00e0y?", "X\u00e1c nh\u1eadn", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    try
                    {
                        DataAccess.DeleteNhanVien(maNV);
                        LoadData();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Không thể xóa nhân viên này!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
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
                Size = new Size(400, 350),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var lbl1 = new Label { Text = "H\u1ecd t\u00ean:", Location = new Point(20, 20), AutoSize = true };
            var txt1 = new ModernTextBox { Text = hoTen, Location = new Point(150, 17), Size = new Size(210, 30) };

            var lbl2 = new Label { Text = "T\u00ean \u0111\u0103ng nh\u1eadp:", Location = new Point(20, 60), AutoSize = true };
            var txt2 = new ModernTextBox { Text = tenDN, Location = new Point(150, 57), Size = new Size(210, 30) };

            var lbl3 = new Label { Text = "M\u1eadt kh\u1ea9u:", Location = new Point(20, 100), AutoSize = true };
            var txt3 = new ModernTextBox { Location = new Point(150, 97), Size = new Size(210, 30), UseSystemPasswordChar = true };
            if (maNV.HasValue) txt3.Text = "(Gi\u1eef tr\u1ed1ng n\u1ebfu kh\u00f4ng \u0111\u1ed5i)";

            var lbl4 = new Label { Text = "Vai tr\u00f2:", Location = new Point(20, 140), AutoSize = true };
            var cboVT = new ModernComboBox { Location = new Point(150, 137), Size = new Size(210, 30), DropDownStyle = ComboBoxStyle.DropDownList };
            cboVT.Items.AddRange(new object[] { "Admin", "NhanVien" });
            cboVT.SelectedItem = vaiTro;

            var lbl5 = new Label { Text = "Tr\u1ea1ng th\u00e1i:", Location = new Point(20, 180), AutoSize = true };
            var chkTT = new CheckBox { Text = "Ho\u1ea1t \u0111\u1ed9ng", Location = new Point(150, 178), Checked = trangThai, AutoSize = true };

            var btnOk = new ModernButton
            {
                Text = "L\u01b0u", Location = new Point(100, 230), Size = new Size(100, 38),
                BaseColor = AppColors.Primary, BorderRadius = 8
            };
            var btnCancel = new ModernButton
            {
                Text = "H\u1ee7y", Location = new Point(220, 230), Size = new Size(100, 38),
                BaseColor = AppColors.TextSecondary, BorderRadius = 8
            };

            btnOk.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txt1.Text)) { MessageBox.Show("Nh\u1eadp h\u1ecd t\u00ean!"); return; }
                if (string.IsNullOrWhiteSpace(txt2.Text)) { MessageBox.Show("Nh\u1eadp t\u00ean \u0111\u0103ng nh\u1eadp!"); return; }

                try
                {
                    if (maNV.HasValue)
                    {
                        if (maNV.Value == Session.CurrentUser!.MaNV && cboVT.SelectedItem?.ToString() != "Admin")
                        {
                            MessageBox.Show("Không thể hạ cấp tài khoản đang đăng nhập!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        if (maNV.Value == Session.CurrentUser!.MaNV && !chkTT.Checked && DataAccess.CountActiveAdmins() <= 1)
                        {
                            MessageBox.Show("Không thể vô hiệu hóa admin cuối cùng!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        DataAccess.UpdateNhanVien(maNV.Value, txt1.Text.Trim(), txt2.Text.Trim(), cboVT.SelectedItem?.ToString() ?? "NhanVien", chkTT.Checked);
                        if (!string.IsNullOrWhiteSpace(txt3.Text) && txt3.Text != "(Gi\u1eef tr\u1ed1ng n\u1ebfu kh\u00f4ng \u0111\u1ed5i)")
                            DataAccess.UpdateNhanVienPassword(maNV.Value, txt3.Text);
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(txt3.Text)) { MessageBox.Show("Nh\u1eadp m\u1eadt kh\u1ea9u!"); return; }
                        DataAccess.InsertNhanVien(txt1.Text.Trim(), txt2.Text.Trim(), txt3.Text, cboVT.SelectedItem?.ToString() ?? "NhanVien");
                    }
                    frm.Close();
                    LoadData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("L\u1ed7i thao t\u00e1c: " + ex.Message, "L\u1ed7i", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            btnCancel.Click += (s, e) => frm.Close();

            frm.Controls.AddRange(new Control[] { lbl1, txt1, lbl2, txt2, lbl3, txt3, lbl4, cboVT, lbl5, chkTT, btnOk, btnCancel });
            frm.ShowDialog();
        }
    }
}


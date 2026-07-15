using System.Data;
using QuanLyThuVien.Controls;
using QuanLyThuVien.Data;
using QuanLyThuVien.Helpers;
using QuanLyThuVien.Models;

namespace QuanLyThuVien.Forms
{
    public class FormDocGia : UserControl
    {
        private DataGridView dgv;

        public FormDocGia()
        {
            BackColor = AppColors.ContentBg;
            Padding = new Padding(10);
            Load += (s, e) => LoadData();
            Resize += (s, e) => { if (dgv != null) { dgv.Width = Width - 30; dgv.Height = Height - 120; } };
        }

        private void LoadData()
        {
            Controls.Clear();

            Controls.Add(new Label
            {
                Text = "Quản lý Độc giả",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = AppColors.TextPrimary,
                AutoSize = true,
                Location = new Point(10, 10)
            });

            var btnThem = new ModernButton
            {
                Text = "+ Thêm mới",
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
            dgv.Columns.Add("MaDG", "Mã");
            dgv.Columns.Add("HoTen", "Họ tên");
            dgv.Columns.Add("GioiTinh", "Giới tính");
            dgv.Columns.Add("SoDienThoai", "SĐT");
            dgv.Columns.Add("Email", "Email");
            dgv.Columns.Add("HanSuDung", "Hạn thẻ");
            dgv.Columns.Add("TrangThai", "Trạng thái");
            dgv.Columns.Add("btnSửa", "Sửa");
            dgv.Columns.Add("btnXóa", "Xóa");
            dgv.CellClick += Dgv_CellClick;
            Controls.Add(dgv);

            try
            {
                var dt = DataAccess.GetAllDocGia();
                foreach (DataRow row in dt.Rows)
                {
                    bool tt = (bool)row["TrangThai"];
                    DateTime hsd = (DateTime)row["HanSuDung"];
                    string trangThai = hsd < DateTime.Now ? "Hết hạn" : (tt ? "Hoạt động" : "Khóa");
                    dgv.Rows.Add(row["MaDG"], row["HoTen"], row["GioiTinh"], row["SoDienThoai"],
                        row["Email"], hsd.ToString("dd/MM/yyyy"), trangThai, "✏️ Sửa", "🗑 Xóa");
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi thao tác: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void Dgv_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            int maDG = Convert.ToInt32(dgv.Rows[e.RowIndex].Cells["MaDG"].Value);

            if (dgv.Columns[e.ColumnIndex].Name == "btnSửa")
            {
                var dt = DataAccess.ExecuteQuery(
                    "SELECT * FROM DocGia WHERE MaDG=@ma",
                    new System.Data.SqlClient.SqlParameter("@ma", maDG));
                if (dt.Rows.Count == 0) return;
                var row = dt.Rows[0];
                var dg = new DocGia
                {
                    MaDG = maDG,
                    HoTen = row["HoTen"].ToString() ?? "",
                    GioiTinh = row["GioiTinh"].ToString() ?? "Nam",
                    SoDienThoai = row["SoDienThoai"].ToString() ?? "",
                    Email = row["Email"].ToString() ?? "",
                    NgaySinh = row["NgaySinh"] == DBNull.Value ? DateTime.Now.AddYears(-20) : (DateTime)row["NgaySinh"],
                    NgayLapThe = row["NgayLapThe"] == DBNull.Value ? DateTime.Now : (DateTime)row["NgayLapThe"],
                    HanSuDung = row["HanSuDung"] == DBNull.Value ? DateTime.Now.AddYears(1) : (DateTime)row["HanSuDung"],
                    TrangThai = (bool)row["TrangThai"]
                };
                ShowInputDialog(dg);
            }
            else if (dgv.Columns[e.ColumnIndex].Name == "btnXóa")
            {
                if (MessageBox.Show("Xóa độc giả này?", "Xác nhận", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    try
                    {
                        DataAccess.DeleteDocGia(maDG);
                        LoadData();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Không thể xóa độc giả này!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void ShowInputDialog(DocGia? existing = null)
        {
            var frm = new Form
            {
                Text = existing != null ? "Sửa độc giả" : "Thêm độc giả",
                Size = new Size(420, 450),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false, MinimizeBox = false
            };

            var lbl1 = new Label { Text = "Họ tên:", Location = new Point(20, 20), AutoSize = true };
            var txt1 = new ModernTextBox { Text = existing?.HoTen ?? "", Location = new Point(140, 17), Size = new Size(240, 30) };

            var lbl2 = new Label { Text = "Ngày sinh:", Location = new Point(20, 60), AutoSize = true };
            var dtpNS = new DateTimePicker { Location = new Point(140, 57), Size = new Size(240, 30), Format = DateTimePickerFormat.Short, Value = existing?.NgaySinh ?? DateTime.Now.AddYears(-20) };

            var lbl3 = new Label { Text = "Giới tính:", Location = new Point(20, 100), AutoSize = true };
            var cboGT = new ModernComboBox { Location = new Point(140, 97), Size = new Size(150, 30), DropDownStyle = ComboBoxStyle.DropDownList };
            cboGT.Items.AddRange(new object[] { "Nam", "Nữ" });
            cboGT.SelectedIndex = existing?.GioiTinh == "Nữ" ? 1 : 0;

            var lbl4 = new Label { Text = "SĐT:", Location = new Point(20, 140), AutoSize = true };
            var txt4 = new ModernTextBox { Text = existing?.SoDienThoai ?? "", Location = new Point(140, 137), Size = new Size(240, 30) };

            var lbl5 = new Label { Text = "Email:", Location = new Point(20, 180), AutoSize = true };
            var txt5 = new ModernTextBox { Text = existing?.Email ?? "", Location = new Point(140, 177), Size = new Size(240, 30) };

            var lbl6 = new Label { Text = "Ngày lập thẻ:", Location = new Point(20, 220), AutoSize = true };
            var dtpLT = new DateTimePicker { Location = new Point(140, 217), Size = new Size(240, 30), Format = DateTimePickerFormat.Short, Value = existing?.NgayLapThe ?? DateTime.Now };

            var lbl7 = new Label { Text = "Hạn sử dụng:", Location = new Point(20, 260), AutoSize = true };
            var dtpHSD = new DateTimePicker { Location = new Point(140, 257), Size = new Size(240, 30), Format = DateTimePickerFormat.Short, Value = existing?.HanSuDung ?? DateTime.Now.AddYears(1) };

            var btnOk = new ModernButton { Text = "Lưu", Location = new Point(140, 320), Size = new Size(120, 40), BaseColor = AppColors.Primary, BorderRadius = 8 };
            var btnCancel = new ModernButton { Text = "Hủy", Location = new Point(280, 320), Size = new Size(120, 40), BaseColor = AppColors.TextSecondary, BorderRadius = 8 };

            btnOk.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txt1.Text)) { MessageBox.Show("Nhập họ tên!"); return; }
                try
                {
                    var dg = new DocGia
                    {
                        MaDG = existing?.MaDG ?? 0,
                        HoTen = txt1.Text.Trim(),
                        NgaySinh = dtpNS.Value,
                        GioiTinh = cboGT.SelectedItem?.ToString() ?? "Nam",
                        SoDienThoai = txt4.Text.Trim(),
                        Email = txt5.Text.Trim(),
                        NgayLapThe = dtpLT.Value,
                        HanSuDung = dtpHSD.Value,
                        TrangThai = existing?.TrangThai ?? true
                    };
                    if (existing != null) DataAccess.UpdateDocGia(dg);
                    else DataAccess.InsertDocGia(dg);
                    frm.Close();
                    LoadData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi thao tác: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            btnCancel.Click += (s, e) => frm.Close();

            frm.Controls.AddRange(new Control[] { lbl1, txt1, lbl2, dtpNS, lbl3, cboGT, lbl4, txt4, lbl5, txt5, lbl6, dtpLT, lbl7, dtpHSD, btnOk, btnCancel });
            frm.ShowDialog();
        }
    }
}

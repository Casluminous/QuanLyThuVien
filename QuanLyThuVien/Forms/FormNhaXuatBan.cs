using System.Data;
using QuanLyThuVien.Controls;
using QuanLyThuVien.Data;
using QuanLyThuVien.Helpers;

namespace QuanLyThuVien.Forms
{
    public class FormNhaXuatBan : UserControl
    {
        private DataGridView dgv;

        public FormNhaXuatBan()
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
                Text = "Quản lý Nhà xuất bản",
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
            dgv.Columns.Add("MaNXB", "Mã");
            dgv.Columns.Add("TenNXB", "Tên NXB");
            dgv.Columns.Add("DiaChi", "Địa chỉ");
            dgv.Columns.Add("SoDienThoai", "SĐT");
            dgv.Columns.Add("btnSửa", "Sửa");
            dgv.Columns.Add("btnXóa", "Xóa");
            dgv.CellClick += Dgv_CellClick;
            Controls.Add(dgv);

            try
            {
                var dt = DataAccess.GetAllNXB();
                foreach (DataRow row in dt.Rows)
                    dgv.Rows.Add(row["MaNXB"], row["TenNXB"], row["DiaChi"], row["SoDienThoai"], "✏️ Sửa", "🗑 Xóa");
            }
            catch (Exception ex) { MessageBox.Show("Lỗi thao tác: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void Dgv_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            int maNXB = Convert.ToInt32(dgv.Rows[e.RowIndex].Cells["MaNXB"].Value);

            if (dgv.Columns[e.ColumnIndex].Name == "btnSửa")
            {
                ShowInputDialog(maNXB,
                    dgv.Rows[e.RowIndex].Cells["TenNXB"].Value?.ToString() ?? "",
                    dgv.Rows[e.RowIndex].Cells["DiaChi"].Value?.ToString() ?? "",
                    dgv.Rows[e.RowIndex].Cells["SoDienThoai"].Value?.ToString() ?? "");
            }
            else if (dgv.Columns[e.ColumnIndex].Name == "btnXóa")
            {
                if (MessageBox.Show("Xóa NXB này?", "Xác nhận", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    try
                    {
                        DataAccess.DeleteNXB(maNXB);
                        LoadData();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Không thể xóa NXB này!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void ShowInputDialog(int? maNXB = null, string ten = "", string dc = "", string sdt = "")
        {
            var frm = new Form
            {
                Text = maNXB.HasValue ? "Sửa NXB" : "Thêm NXB",
                Size = new Size(400, 300),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false, MinimizeBox = false
            };

            var lbl1 = new Label { Text = "Tên NXB:", Location = new Point(20, 20), AutoSize = true };
            var txt1 = new ModernTextBox { Text = ten, Location = new Point(20, 45), Size = new Size(340, 30) };
            var lbl2 = new Label { Text = "Địa chỉ:", Location = new Point(20, 85), AutoSize = true };
            var txt2 = new ModernTextBox { Text = dc, Location = new Point(20, 110), Size = new Size(340, 30) };
            var lbl3 = new Label { Text = "Số điện thoại:", Location = new Point(20, 150), AutoSize = true };
            var txt3 = new ModernTextBox { Text = sdt, Location = new Point(20, 175), Size = new Size(340, 30) };

            var btnOk = new ModernButton { Text = "Lưu", Location = new Point(20, 225), Size = new Size(100, 35), BaseColor = AppColors.Primary, BorderRadius = 8 };
            var btnCancel = new ModernButton { Text = "Hủy", Location = new Point(130, 225), Size = new Size(100, 35), BaseColor = AppColors.TextSecondary, BorderRadius = 8 };

            btnOk.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txt1.Text)) { MessageBox.Show("Nhập tên NXB!"); return; }
                try
                {
                    if (maNXB.HasValue)
                        DataAccess.UpdateNXB(maNXB.Value, txt1.Text.Trim(), txt2.Text.Trim(), txt3.Text.Trim());
                    else
                        DataAccess.InsertNXB(txt1.Text.Trim(), txt2.Text.Trim(), txt3.Text.Trim());
                    frm.Close();
                    LoadData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi thao tác: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            btnCancel.Click += (s, e) => frm.Close();

            frm.Controls.AddRange(new Control[] { lbl1, txt1, lbl2, txt2, lbl3, txt3, btnOk, btnCancel });
            frm.ShowDialog();
        }
    }
}

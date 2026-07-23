using System.Data;
using QuanLyThuVien.Controls;
using QuanLyThuVien.Data;
using QuanLyThuVien.Helpers;
using QuanLyThuVien.Models;

namespace QuanLyThuVien.Forms
{
    public class FormTheLoai : UserControl
    {
        private DataGridView dgv = null!;

        public FormTheLoai()
        {
            BackColor = AppColors.ContentBg;
            Padding = new Padding(10);
            Load += (s, e) => LoadData();
        }

        private void LoadData()
        {
            ResponsiveUi.DisposeChildren(this);

            var btnThem = PageHeader.CreatePrimaryAction("+ Thêm thể loại", (_, _) => ShowInputDialog(null), 150);

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
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = AppColors.Primary,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                },
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 40,
                DefaultCellStyle = new DataGridViewCellStyle { Font = new Font("Segoe UI", 10F) }
            };
            dgv.Columns.Add("MaTL", "Mã");
            dgv.Columns.Add("TenTheLoai", "Tên thể loại");
            dgv.Columns.Add("btnSửa", "Sửa");
            dgv.Columns.Add("btnXóa", "Xóa");
            dgv.CellClick += Dgv_CellClick;
            ResponsiveUi.AddListPage(this, dgv, "Quản lý Thể loại", btnThem);

            try
            {
                var dt = DataAccess.GetAllTheLoai();
                foreach (DataRow row in dt.Rows)
                    dgv.Rows.Add(row["MaTL"], row["TenTheLoai"], "✏️ Sửa", "🗑 Xóa");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Tải thể loại thất bại: {ex}");
                MessageBox.Show("Không thể tải dữ liệu thể loại.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Dgv_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            int maTL = Convert.ToInt32(dgv.Rows[e.RowIndex].Cells["MaTL"].Value);
            string tenTL = dgv.Rows[e.RowIndex].Cells["TenTheLoai"].Value?.ToString() ?? "";

            if (dgv.Columns[e.ColumnIndex].Name == "btnSửa")
                ShowInputDialog(maTL, tenTL);
            else if (dgv.Columns[e.ColumnIndex].Name == "btnXóa")
            {
                if (MessageBox.Show("Xóa thể loại này?", "Xác nhận", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    try
                    {
                        DataAccess.DeleteTheLoai(maTL);
                        LoadData();
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Không thể xóa thể loại này!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void ShowInputDialog(int? maTL, string currentName = "")
        {
            var frm = new Form
            {
                Text = maTL.HasValue ? "Sửa thể loại" : "Thêm thể loại",
                ClientSize = new Size(380, 200),
                MinimumSize = new Size(340, 180),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.Sizable,
                MaximizeBox = true,
                MinimizeBox = false
            };

            var lbl = new Label { Text = "Tên thể loại:", Location = new Point(20, 25), AutoSize = true };
            var txt = new ModernTextBox { Text = currentName, Location = new Point(20, 55), Size = new Size(320, 30), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            var btnOk = new ModernButton
            {
                Text = "Lưu",
                Location = new Point(20, 100),
                Size = new Size(100, 38),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                BaseColor = AppColors.Primary,
                BorderRadius = 12
            };
            var btnCancel = new ModernButton
            {
                Text = "Hủy",
                Location = new Point(140, 100),
                Size = new Size(100, 38),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                BaseColor = AppColors.TextSecondary,
                BorderRadius = 12
            };

            btnOk.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txt.Text))
                {
                    MessageBox.Show("Nhập tên thể loại!");
                    return;
                }
                try
                {
                    if (maTL.HasValue)
                        DataAccess.UpdateTheLoai(maTL.Value, txt.Text.Trim());
                    else
                        DataAccess.InsertTheLoai(txt.Text.Trim());
                    frm.Close();
                    LoadData();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Lưu thể loại thất bại: {ex}");
                    MessageBox.Show("Không thể lưu dữ liệu thể loại.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            btnCancel.Click += (s, e) => frm.Close();

            frm.Controls.AddRange(new Control[] { lbl, txt, btnOk, btnCancel });
            frm.AcceptButton = btnOk;
            frm.CancelButton = btnCancel;
            frm.ActiveControl = txt;
            txt.AccessibleName = "Tên thể loại";
            frm.ShowDialog();
        }
    }
}

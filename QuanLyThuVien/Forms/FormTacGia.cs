using System.Data;
using QuanLyThuVien.Controls;
using QuanLyThuVien.Data;
using QuanLyThuVien.Helpers;

namespace QuanLyThuVien.Forms
{
    public class FormTacGia : UserControl
    {
        private DataGridView dgv = null!;

        public FormTacGia()
        {
            BackColor = AppColors.ContentBg;
            Padding = new Padding(10);
            Load += (s, e) => LoadData();
        }

        private void LoadData()
        {
            ResponsiveUi.DisposeChildren(this);

            var btnThem = PageHeader.CreatePrimaryAction("+ Thêm tác giả", (_, _) => ShowInputDialog(), 150);

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
            dgv.Columns.Add("MaTG", "Mã");
            dgv.Columns.Add("TenTG", "Tên tác giả");
            dgv.Columns.Add("QuocTich", "Quốc tịch");
            dgv.Columns.Add("GhiChu", "Ghi chú");
            dgv.Columns.Add("btnSửa", "Sửa");
            dgv.Columns.Add("btnXóa", "Xóa");
            dgv.CellClick += Dgv_CellClick;
            ResponsiveUi.AddListPage(this, dgv, "Quản lý Tác giả", btnThem);

            try
            {
                var dt = DataAccess.GetAllTacGia();
                foreach (DataRow row in dt.Rows)
                    dgv.Rows.Add(row["MaTG"], row["TenTG"], row["QuocTich"], row["GhiChu"], "✏️ Sửa", "🗑 Xóa");
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Tải tác giả thất bại: {ex}"); MessageBox.Show("Không thể tải dữ liệu tác giả.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void Dgv_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            int maTG = Convert.ToInt32(dgv.Rows[e.RowIndex].Cells["MaTG"].Value);

            if (dgv.Columns[e.ColumnIndex].Name == "btnSửa")
            {
                ShowInputDialog(maTG,
                    dgv.Rows[e.RowIndex].Cells["TenTG"].Value?.ToString() ?? "",
                    dgv.Rows[e.RowIndex].Cells["QuocTich"].Value?.ToString() ?? "",
                    dgv.Rows[e.RowIndex].Cells["GhiChu"].Value?.ToString() ?? "");
            }
            else if (dgv.Columns[e.ColumnIndex].Name == "btnXóa")
            {
                if (MessageBox.Show("Xóa tác giả này?", "Xác nhận", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    try
                    {
                        DataAccess.DeleteTacGia(maTG);
                        LoadData();
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Không thể xóa tác giả này!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void ShowInputDialog(int? maTG = null, string ten = "", string qt = "", string gc = "")
        {
            var frm = new Form
            {
                Text = maTG.HasValue ? "Sửa tác giả" : "Thêm tác giả",
                ClientSize = new Size(400, 300),
                MinimumSize = new Size(360, 280),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.Sizable,
                MaximizeBox = true,
                MinimizeBox = false
            };

            var lbl1 = new Label { Text = "Tên tác giả:", Location = new Point(20, 20), AutoSize = true };
            var txt1 = new ModernTextBox { Text = ten, Location = new Point(20, 45), Size = new Size(340, 30), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            var lbl2 = new Label { Text = "Quốc tịch:", Location = new Point(20, 85), AutoSize = true };
            var txt2 = new ModernTextBox { Text = qt, Location = new Point(20, 110), Size = new Size(340, 30), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            var lbl3 = new Label { Text = "Ghi chú:", Location = new Point(20, 150), AutoSize = true };
            var txt3 = new ModernTextBox { Text = gc, Location = new Point(20, 175), Size = new Size(340, 60), Multiline = true, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

            var btnOk = new ModernButton
            {
                Text = "Lưu", Location = new Point(20, 245), Size = new Size(100, 35), Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                BaseColor = AppColors.Primary, BorderRadius = 12
            };
            var btnCancel = new ModernButton
            {
                Text = "Hủy", Location = new Point(130, 245), Size = new Size(100, 35), Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                BaseColor = AppColors.TextSecondary, BorderRadius = 12
            };

            btnOk.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txt1.Text)) { MessageBox.Show("Nhập tên tác giả!"); return; }
                try
                {
                    if (maTG.HasValue)
                        DataAccess.UpdateTacGia(maTG.Value, txt1.Text.Trim(), txt2.Text.Trim(), txt3.Text.Trim());
                    else
                        DataAccess.InsertTacGia(txt1.Text.Trim(), txt2.Text.Trim(), txt3.Text.Trim());
                    frm.Close();
                    LoadData();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Lưu tác giả thất bại: {ex}");
                    MessageBox.Show("Không thể lưu dữ liệu tác giả.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            btnCancel.Click += (s, e) => frm.Close();

            frm.Controls.AddRange(new Control[] { lbl1, txt1, lbl2, txt2, lbl3, txt3, btnOk, btnCancel });
            frm.AcceptButton = btnOk;
            frm.CancelButton = btnCancel;
            frm.ActiveControl = txt1;
            txt1.AccessibleName = "Tên tác giả";
            txt2.AccessibleName = "Quốc tịch";
            txt3.AccessibleName = "Ghi chú";
            frm.ShowDialog();
        }
    }
}

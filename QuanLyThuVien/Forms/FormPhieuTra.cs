using System.Data;
using System.Data.SqlClient;
using QuanLyThuVien.Controls;
using QuanLyThuVien.Data;
using QuanLyThuVien.Helpers;

namespace QuanLyThuVien.Forms
{
    public class FormPhieuTra : UserControl
    {
        private DataGridView dgv;
        private const decimal PhatMoiNgay = 10000;

        public FormPhieuTra()
        {
            BackColor = AppColors.ContentBg;
            Padding = new Padding(10);
            Load += (s, e) => LoadData();
            Resize += (s, e) => { if (dgv != null) { dgv.Width = Width - 30; dgv.Height = Height - 70; } };
        }

        private void LoadData()
        {
            Controls.Clear();

            Controls.Add(new Label
            {
                Text = "Trả sách",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = AppColors.TextPrimary,
                AutoSize = true,
                Location = new Point(10, 10)
            });

            dgv = new ModernDataGridView
            {
                Location = new Point(10, 55),
                Size = new Size(Width - 30, Height - 70),
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
            dgv.Columns.Add("MaPhieuMuon", "Mã PM");
            dgv.Columns.Add("TenDocGia", "Độc giả");
            dgv.Columns.Add("NgayMuon", "Ngày mượn");
            dgv.Columns.Add("HanTra", "Hạn trả");
            dgv.Columns.Add("SoNgayQuaHan", "Quá hạn (ngày)");
            dgv.Columns.Add("TienPhat", "Tiền phạt");
            dgv.Columns.Add("btnTra", "Trả sách");
            dgv.CellClick += Dgv_CellClick;
            Controls.Add(dgv);

            try
            {
                var dt = DataAccess.ExecuteQuery(
                    @"SELECT pm.MaPhieuMuon, pm.NgayMuon, pm.HanTra, dg.HoTen AS TenDocGia
                      FROM PhieuMuon pm
                      JOIN DocGia dg ON pm.MaDG=dg.MaDG
                      WHERE pm.TrangThai=N'Đang mượn' AND EXISTS (
                          SELECT 1 FROM ChiTietPhieuMuon ctpm WHERE ctpm.MaPhieuMuon=pm.MaPhieuMuon AND ctpm.NgayTra IS NULL
                      )
                      ORDER BY pm.HanTra ASC");

                foreach (DataRow row in dt.Rows)
                {
                    DateTime hanTra = (DateTime)row["HanTra"];
                    int soNgayQuaHan = (DateTime.Now - hanTra).Days;
                    if (soNgayQuaHan < 0) soNgayQuaHan = 0;
                    decimal tienPhat = soNgayQuaHan * PhatMoiNgay;

                    dgv.Rows.Add(row["MaPhieuMuon"], row["TenDocGia"],
                        ((DateTime)row["NgayMuon"]).ToString("dd/MM/yyyy"),
                        hanTra.ToString("dd/MM/yyyy"),
                        soNgayQuaHan > 0 ? soNgayQuaHan.ToString() : "0",
                        tienPhat.ToString("N0") + "đ",
                        "Trả sách");
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi: " + ex.Message); }
        }

        private void Dgv_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (dgv.Columns[e.ColumnIndex].Name != "btnTra") return;

            int maPM = Convert.ToInt32(dgv.Rows[e.RowIndex].Cells["MaPhieuMuon"].Value);
            string tenDG = dgv.Rows[e.RowIndex].Cells["TenDocGia"].Value?.ToString() ?? "";
            decimal tienPhat = Convert.ToDecimal(dgv.Rows[e.RowIndex].Cells["TienPhat"].Value.ToString()!.Replace("đ", "").Replace(",", ""));

            ShowTraForm(maPM, tenDG, tienPhat);
        }

        private void ShowTraForm(int maPM, string tenDG, decimal tienPhatCu)
        {
            var frm = new Form
            {
                Text = $"Trả sách - PM #{maPM}",
                Size = new Size(600, 450),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false, MinimizeBox = false
            };

            var lblInfo = new Label
            {
                Text = $"Phiếu mượn #{maPM} - {tenDG}",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = AppColors.TextPrimary,
                AutoSize = true,
                Location = new Point(20, 15)
            };
            frm.Controls.Add(lblInfo);

            var dgvCT = new ModernDataGridView
            {
                Location = new Point(20, 55),
                Size = new Size(540, 280),
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = true,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = AppColors.Primary,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold)
                },
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 30,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                DefaultCellStyle = new DataGridViewCellStyle { Font = new Font("Segoe UI", 10F) }
            };
            dgvCT.Columns.Add("MaSach", "Mã sách");
            dgvCT.Columns.Add("TenSach", "Tên sách");
            dgvCT.Columns.Add("SoLuong", "Số lượng");
            dgvCT.Columns.Add("DaTra", "Đã trả");

            try
            {
                var dt = DataAccess.GetChiTietPhieuMuon(maPM);
                foreach (DataRow row in dt.Rows)
                {
                    bool daTra = row["NgayTra"] != DBNull.Value;
                    dgvCT.Rows.Add(row["MaSach"], row["TenSach"], row["SoLuong"], daTra ? "Y" : "N");
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Load chi tiet error: " + ex.Message); }
            frm.Controls.Add(dgvCT);

            var lblPhat = new Label
            {
                Text = $"Tiền phạt: {tienPhatCu:N0}đ (10,000đ/ngày trễ)",
                Font = new Font("Segoe UI", 10F),
                ForeColor = tienPhatCu > 0 ? AppColors.Danger : AppColors.Success,
                AutoSize = true,
                Location = new Point(20, 350)
            };
            frm.Controls.Add(lblPhat);

            var btnTra = new ModernButton
            {
                Text = "Xác nhận trả",
                Location = new Point(350, 380),
                Size = new Size(130, 40),
                BaseColor = AppColors.Success,
                HoverColor = Color.FromArgb(39, 174, 96),
                BorderRadius = 8
            };
            btnTra.Click += (s, e) =>
            {
                var items = new List<(int maSach, int soLuong, decimal tienPhat)>();
                int unreturnedCount = 0;
                foreach (DataGridViewRow row in dgvCT.Rows)
                {
                    if (row.Cells["DaTra"].Value?.ToString() == "N")
                        unreturnedCount++;
                }
                decimal phatPerItem = unreturnedCount > 0 ? tienPhatCu / unreturnedCount : 0;
                foreach (DataGridViewRow row in dgvCT.Rows)
                {
                    if (row.Cells["DaTra"].Value?.ToString() == "N")
                    {
                        int maSach = Convert.ToInt32(row.Cells["MaSach"].Value);
                        int sl = Convert.ToInt32(row.Cells["SoLuong"].Value);
                        items.Add((maSach, sl, phatPerItem));
                    }
                }
                if (items.Count == 0) { frm.Close(); return; }
                try
                {
                    if (!DataAccess.TraNhieuSach(maPM, items))
                    {
                        MessageBox.Show("Không thể xác nhận trả sách vì dữ liệu đã thay đổi. Vui lòng tải lại danh sách.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Không thể xác nhận trả sách: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                frm.Close();
                LoadData();
            };
            frm.Controls.Add(btnTra);

            var btnCancel = new ModernButton
            {
                Text = "Hủy",
                Location = new Point(200, 380),
                Size = new Size(100, 40),
                BaseColor = AppColors.TextSecondary,
                BorderRadius = 8
            };
            btnCancel.Click += (s, e) => frm.Close();
            frm.Controls.Add(btnCancel);

            frm.ShowDialog();
        }
    }
}

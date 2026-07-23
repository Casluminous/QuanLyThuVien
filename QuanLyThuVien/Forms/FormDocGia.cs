using System.Data;
using QuanLyThuVien.Controls;
using QuanLyThuVien.Data;
using QuanLyThuVien.Helpers;
using QuanLyThuVien.Models;
using QuanLyThuVien.Pdf;

namespace QuanLyThuVien.Forms
{
    public class FormDocGia : UserControl
    {
        private DataGridView dgv = null!;
        private FilterBar filterBar = null!;

        public FormDocGia()
        {
            BackColor = AppColors.ContentBg;
            Padding = new Padding(10);
            Load += (s, e) => LoadData();
        }

        private void LoadData()
        {
            ResponsiveUi.DisposeChildren(this);

            var btnThem = PageHeader.CreatePrimaryAction("+ Thêm độc giả", (_, _) => ShowInputDialog(), 150);
            var btnExport = PageHeader.CreatePrimaryAction("Xuất PDF", (_, _) => PdfExportService.ExportGrid(dgv, "Danh sách độc giả", "doc_gia", FindForm() is IWin32Window owner ? owner : this), 105);

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
            dgv.Columns.Add("MaDG", "Mã");
            dgv.Columns.Add("HoTen", "Họ tên");
            dgv.Columns.Add("GioiTinh", "Giới tính");
            dgv.Columns.Add("SoDienThoai", "SĐT");
            dgv.Columns.Add("Email", "Email");
            dgv.Columns.Add("HanSuDung", "Hạn thẻ");
            dgv.Columns.Add("TrangThai", "Trạng thái");
            dgv.Columns.Add("btnLichSu", "Lịch sử");
            dgv.Columns.Add("btnSửa", "Sửa");
            dgv.Columns.Add("btnXóa", "Xóa");
            dgv.CellClick += Dgv_CellClick;
            var header = ResponsiveUi.AddListPage(this, dgv, "Quản lý Độc giả", btnThem, btnExport);

            filterBar = new FilterBar("Tìm tên, số điện thoại, email...");
            var statusFilter = new ModernComboBox { Size = new Size(160, 36), DropDownStyle = ComboBoxStyle.DropDownList };
            statusFilter.Items.AddRange(new object[] { "Tất cả", "Hoạt động", "Hết hạn / khóa", "Sắp hết hạn" });
            statusFilter.SelectedIndex = 0;
            filterBar.AddFilter(statusFilter, "Lọc trạng thái độc giả");
            filterBar.FilterChanged += (_, _) => RefreshRows(statusFilter.SelectedItem?.ToString());
            ResponsiveUi.AddFilterBar(header, filterBar);

            RefreshRows(statusFilter.SelectedItem?.ToString());
        }

        private void RefreshRows(string? statusFilter)
        {
            if (dgv == null) return;
            dgv.Rows.Clear();
            try
            {
                bool? activeOnly = statusFilter switch
                {
                    "Hoạt động" => true,
                    "Hết hạn / khóa" => false,
                    _ => null
                };
                bool? expiring = statusFilter == "Sắp hết hạn" ? true : null;
                var dt = DataAccess.GetAllDocGia(filterBar?.SearchBox.Text, activeOnly, expiring);
                foreach (DataRow row in dt.Rows)
                {
                    bool tt = (bool)row["TrangThai"];
                    DateTime hsd = (DateTime)row["HanSuDung"];
                    string trangThai = hsd < DateTime.Now ? "Hết hạn" : (tt ? "Hoạt động" : "Khóa");
                    dgv.Rows.Add(row["MaDG"], row["HoTen"], row["GioiTinh"], row["SoDienThoai"],
                        row["Email"], hsd.ToString("dd/MM/yyyy"), trangThai, "Lịch sử", "✏️ Sửa", "🗑 Xóa");
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Tải độc giả thất bại: {ex}"); MessageBox.Show("Không thể tải dữ liệu độc giả.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void Dgv_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            int maDG = Convert.ToInt32(dgv.Rows[e.RowIndex].Cells["MaDG"].Value);

            if (dgv.Columns[e.ColumnIndex].Name == "btnLichSu")
            {
                ShowHistoryDialog(maDG);
            }
            else if (dgv.Columns[e.ColumnIndex].Name == "btnSửa")
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
                    catch (Exception)
                    {
                        MessageBox.Show("Không thể xóa độc giả này!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void ShowHistoryDialog(int maDG)
        {
            DataTable history;
            try
            {
                history = DataAccess.GetDocGiaHistory(maDG);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Không tải được lịch sử độc giả: {ex}");
                MessageBox.Show("Không thể tải lịch sử độc giả. Hãy kiểm tra kết nối database và migration hiện tại.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            string readerName = "Độc giả";
            var reader = DataAccess.GetAllDocGia().Select($"MaDG={maDG}").FirstOrDefault();
            if (reader != null) readerName = reader["HoTen"].ToString() ?? readerName;
            var frm = ResponsiveUi.CreateDialog($"Lịch sử độc giả · {readerName}", new Size(980, 580), new Size(760, 460));
            var grid = new ModernDataGridView { Dock = DockStyle.Fill, BackgroundColor = AppColors.CardBg, BorderStyle = BorderStyle.None, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
            grid.Columns.Add("MaPhieuMuon", "Mã PM");
            grid.Columns.Add("NgayMuon", "Ngày mượn");
            grid.Columns.Add("HanTra", "Hạn trả");
            grid.Columns.Add("NgayTraCuoi", "Ngày trả cuối");
            grid.Columns.Add("TrangThai", "Trạng thái");
            grid.Columns.Add("SoDauSach", "Số sách");
            grid.Columns.Add("TongTienPhat", "Phạt");
            grid.Columns.Add("TongTienDen", "Tiền đền");
            foreach (DataRow row in history.Rows)
            {
                grid.Rows.Add(row["MaPhieuMuon"], Convert.ToDateTime(row["NgayMuon"]).ToString("dd/MM/yyyy"), Convert.ToDateTime(row["HanTra"]).ToString("dd/MM/yyyy"), row["NgayTraCuoi"] == DBNull.Value ? "—" : Convert.ToDateTime(row["NgayTraCuoi"]).ToString("dd/MM/yyyy"), row["TrangThai"], row["SoDauSach"], $"{Convert.ToDecimal(row["TongTienPhat"]):N0}đ", $"{Convert.ToDecimal(row["TongTienDen"]):N0}đ");
            }
            if (grid.Rows.Count == 0) grid.Rows.Add("—", "Chưa có lịch sử", "—", "—", "—", "—", "0đ", "0đ");
            var footer = ResponsiveUi.AddDialogFooter(frm, 56);
            var btnPrint = PageHeader.CreatePrimaryAction("Lưu lịch sử PDF", (_, _) => PdfExportService.ExportReaderHistory(maDG, frm), 135);
            var btnClose = new ModernButton { Text = "Đóng", Size = new Size(90, 36), BaseColor = AppColors.TextSecondary, BorderRadius = 10, DialogResult = DialogResult.Cancel, AccessibleName = "Đóng lịch sử độc giả" };
            footer.Controls.Add(btnClose);
            footer.Controls.Add(btnPrint);
            frm.Controls.Add(grid);
            frm.AcceptButton = btnClose;
            frm.CancelButton = btnClose;
            frm.ActiveControl = grid;
            frm.ShowDialog();
        }

        private void ShowInputDialog(DocGia? existing = null)
        {
            var frm = new Form
            {
                Text = existing != null ? "Sửa độc giả" : "Thêm độc giả",
                ClientSize = new Size(420, 450),
                MinimumSize = new Size(390, 410),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.Sizable,
                MaximizeBox = true, MinimizeBox = false
            };

            var lbl1 = new Label { Text = "Họ tên:", Location = new Point(20, 20), AutoSize = true };
            var txt1 = new ModernTextBox { Text = existing?.HoTen ?? "", Location = new Point(140, 17), Size = new Size(240, 30), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

            var lbl2 = new Label { Text = "Ngày sinh:", Location = new Point(20, 60), AutoSize = true };
            var dtpNS = new DateTimePicker { Location = new Point(140, 57), Size = new Size(240, 30), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, Format = DateTimePickerFormat.Short, Value = existing?.NgaySinh ?? DateTime.Now.AddYears(-20) };

            var lbl3 = new Label { Text = "Giới tính:", Location = new Point(20, 100), AutoSize = true };
            var cboGT = new ModernComboBox { Location = new Point(140, 97), Size = new Size(150, 30), DropDownStyle = ComboBoxStyle.DropDownList };
            cboGT.Items.AddRange(new object[] { "Nam", "Nữ" });
            cboGT.SelectedIndex = existing?.GioiTinh == "Nữ" ? 1 : 0;

            var lbl4 = new Label { Text = "SĐT:", Location = new Point(20, 140), AutoSize = true };
            var txt4 = new ModernTextBox { Text = existing?.SoDienThoai ?? "", Location = new Point(140, 137), Size = new Size(240, 30), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

            var lbl5 = new Label { Text = "Email:", Location = new Point(20, 180), AutoSize = true };
            var txt5 = new ModernTextBox { Text = existing?.Email ?? "", Location = new Point(140, 177), Size = new Size(240, 30), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

            var lbl6 = new Label { Text = "Ngày lập thẻ:", Location = new Point(20, 220), AutoSize = true };
            var dtpLT = new DateTimePicker { Location = new Point(140, 217), Size = new Size(240, 30), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, Format = DateTimePickerFormat.Short, Value = existing?.NgayLapThe ?? DateTime.Now };

            var lbl7 = new Label { Text = "Hạn sử dụng:", Location = new Point(20, 260), AutoSize = true };
            var dtpHSD = new DateTimePicker { Location = new Point(140, 257), Size = new Size(240, 30), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, Format = DateTimePickerFormat.Short, Value = existing?.HanSuDung ?? DateTime.Now.AddYears(1) };

            void EnsureExpiryIsValid()
            {
                if (dtpHSD.Value.Date < dtpLT.Value.Date)
                    dtpHSD.Value = dtpLT.Value.Date;
                dtpHSD.MinDate = dtpLT.Value.Date;
            }
            dtpLT.ValueChanged += (s, e) => EnsureExpiryIsValid();
            EnsureExpiryIsValid();

            var btnOk = new ModernButton { Text = "Lưu", Location = new Point(140, 320), Size = new Size(120, 40), Anchor = AnchorStyles.Bottom | AnchorStyles.Left, BaseColor = AppColors.Primary, BorderRadius = 12 };
            var btnCancel = new ModernButton { Text = "Hủy", Location = new Point(280, 320), Size = new Size(120, 40), Anchor = AnchorStyles.Bottom | AnchorStyles.Left, BaseColor = AppColors.TextSecondary, BorderRadius = 12 };

            btnOk.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txt1.Text)) { MessageBox.Show("Nhập họ tên!"); return; }
                if (dtpHSD.Value.Date < dtpLT.Value.Date) { MessageBox.Show("Hạn sử dụng không được trước ngày lập thẻ!"); return; }
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
                    System.Diagnostics.Debug.WriteLine($"Lưu độc giả thất bại: {ex}");
                    MessageBox.Show("Không thể lưu dữ liệu độc giả.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            btnCancel.Click += (s, e) => frm.Close();

            frm.Controls.AddRange(new Control[] { lbl1, txt1, lbl2, dtpNS, lbl3, cboGT, lbl4, txt4, lbl5, txt5, lbl6, dtpLT, lbl7, dtpHSD, btnOk, btnCancel });
            frm.AcceptButton = btnOk;
            frm.CancelButton = btnCancel;
            frm.ActiveControl = txt1;
            txt1.AccessibleName = "Họ tên độc giả";
            txt4.AccessibleName = "Số điện thoại";
            txt5.AccessibleName = "Email";
            frm.ShowDialog();
        }
    }
}

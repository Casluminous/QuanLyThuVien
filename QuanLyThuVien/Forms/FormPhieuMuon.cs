using System.Data;
using System.Data.SqlClient;
using QuanLyThuVien.Controls;
using QuanLyThuVien.Data;
using QuanLyThuVien.Helpers;
using QuanLyThuVien.Models;

namespace QuanLyThuVien.Forms
{
    public class FormPhieuMuon : UserControl
    {
        private DataGridView dgv;

        public FormPhieuMuon()
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
                Text = "Quản lý Phiếu mượn",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = AppColors.TextPrimary,
                AutoSize = true,
                Location = new Point(10, 10)
            });

            var btnThem = new ModernButton
            {
                Text = "+ Tạo phiếu mượn",
                Location = new Point(10, 55),
                Size = new Size(170, 38),
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
            dgv.Columns.Add("MaPhieuMuon", "Mã PM");
            dgv.Columns.Add("TenDocGia", "Độc giả");
            dgv.Columns.Add("TenNhanVien", "Nhân viên");
            dgv.Columns.Add("NgayMuon", "Ngày mượn");
            dgv.Columns.Add("HanTra", "Hạn trả");
            dgv.Columns.Add("TrangThai", "Trạng thái");
            dgv.Columns.Add("btnChiTiet", "Chi tiết");
            dgv.CellClick += Dgv_CellClick;
            Controls.Add(dgv);

            try
            {
                var dt = DataAccess.GetAllPhieuMuon();
                foreach (DataRow row in dt.Rows)
                {
                    DateTime hanTra = (DateTime)row["HanTra"];
                    string trangThai = row["TrangThai"].ToString()!;
                    if (trangThai == "Đang mượn" && hanTra < DateTime.Now)
                        trangThai = "Quá hạn";
                    dgv.Rows.Add(row["MaPhieuMuon"], row["TenDocGia"], row["TenNhanVien"],
                        ((DateTime)row["NgayMuon"]).ToString("dd/MM/yyyy"),
                        hanTra.ToString("dd/MM/yyyy"), trangThai, "Xem");
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi: " + ex.Message); }
        }

        private void Dgv_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            int maPM = Convert.ToInt32(dgv.Rows[e.RowIndex].Cells["MaPhieuMuon"].Value);

            if (dgv.Columns[e.ColumnIndex].Name == "btnChiTiet")
                ShowChiTiet(maPM);
        }

        private void ShowChiTiet(int maPM)
        {
            var frm = new Form
            {
                Text = $"Chi tiết Phiếu mượn #{maPM}",
                Size = new Size(600, 350),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false, MinimizeBox = false
            };

            var dgvCT = new ModernDataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
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
                ColumnHeadersHeight = 35,
                DefaultCellStyle = new DataGridViewCellStyle { Font = new Font("Segoe UI", 10F) }
            };
            dgvCT.Columns.Add("TenSach", "Tên sách");
            dgvCT.Columns.Add("SoLuong", "Số lượng");
            dgvCT.Columns.Add("NgayTra", "Ngày trả");
            dgvCT.Columns.Add("TienPhat", "Tiền phạt");

            try
            {
                var dt = DataAccess.GetChiTietPhieuMuon(maPM);
                foreach (DataRow row in dt.Rows)
                {
                    string ngayTra = row["NgayTra"] == DBNull.Value ? "Chưa trả" : ((DateTime)row["NgayTra"]).ToString("dd/MM/yyyy");
                    dgvCT.Rows.Add(row["TenSach"], row["SoLuong"], ngayTra, ((decimal)row["TienPhat"]).ToString("N0") + "đ");
                }
            }
            catch { }

            frm.Controls.Add(dgvCT);
            frm.ShowDialog();
        }

        private void ShowInputDialog()
        {
            var frm = new Form
            {
                Text = "Tạo phiếu mượn mới",
                Size = new Size(500, 450),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false, MinimizeBox = false
            };

            var dgData = DataAccess.GetBorrowEligibleReaders();
            var sachData = DataAccess.GetSachAvailable();

            var lbl1 = new Label { Text = "Độc giả:", Location = new Point(20, 20), AutoSize = true };
            var cboDG = new ModernComboBox { Location = new Point(140, 17), Size = new Size(310, 30), DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (DataRow row in dgData.Rows)
                cboDG.Items.Add(new ComboItem(row["HoTen"].ToString()!, Convert.ToInt32(row["MaDG"])));
            if (cboDG.Items.Count > 0) cboDG.SelectedIndex = 0;

            var lbl2 = new Label { Text = "Ngày mượn:", Location = new Point(20, 65), AutoSize = true };
            var dtpMuon = new DateTimePicker { Location = new Point(140, 62), Size = new Size(200, 30), Format = DateTimePickerFormat.Short, MaxDate = DateTime.Today, Value = DateTime.Today };

            var lbl3 = new Label { Text = "Số ngày mượn:", Location = new Point(20, 105), AutoSize = true };
            var nudDays = new NumericUpDown { Location = new Point(140, 102), Size = new Size(100, 30), Minimum = 1, Maximum = 90, Value = 14 };

            var lbl4 = new Label { Text = "Sách:", Location = new Point(20, 150), AutoSize = true };
            var dgvSach = new ModernDataGridView
            {
                Location = new Point(20, 175),
                Size = new Size(430, 180),
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = AppColors.Primary,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold)
                },
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 30,
                DefaultCellStyle = new DataGridViewCellStyle { Font = new Font("Segoe UI", 10F) }
            };
            dgvSach.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Chon", HeaderText = "Chọn", Width = 50 });
            dgvSach.Columns.Add("MaSach", "Mã");
            dgvSach.Columns.Add("TenSach", "Tên sách");
            dgvSach.Columns.Add("SoLuongKhaDung", "SL khả dụng");
            dgvSach.Columns.Add("SoLuongMuon", "SL mượn");
            foreach (DataRow row in sachData.Rows)
                dgvSach.Rows.Add(false, row["MaSach"], row["TenSach"], row["SoLuong"], 1);

            dgvSach.CellClick += (s, e) =>
            {
                if (e.ColumnIndex == 0 && e.RowIndex >= 0)
                {
                    var cell = dgvSach.Rows[e.RowIndex].Cells["Chon"];
                    bool current = Convert.ToBoolean(cell.Value);
                    cell.Value = !current;
                    dgvSach.Rows[e.RowIndex].Cells["SoLuongMuon"].Value = !current ? 1 : 0;
                }
            };

            var btnOk = new ModernButton { Text = "Tạo phiếu", Location = new Point(200, 370), Size = new Size(130, 40), BaseColor = AppColors.Primary, BorderRadius = 8 };
            var btnCancel = new ModernButton { Text = "Hủy", Location = new Point(350, 370), Size = new Size(100, 40), BaseColor = AppColors.TextSecondary, BorderRadius = 8 };

            btnOk.Click += (s, e) =>
            {
                if (cboDG.SelectedItem is not ComboItem dg)
                { MessageBox.Show("Chọn độc giả!"); return; }
                if (dtpMuon.Value.Date > DateTime.Today)
                { MessageBox.Show("Ngày mượn không được ở tương lai!"); return; }

                var sachMuon = new List<(int maSach, int sl)>();
                foreach (DataGridViewRow row in dgvSach.Rows)
                {
                    bool checked_ = Convert.ToBoolean(row.Cells["Chon"].Value);
                    int sl = Convert.ToInt32(row.Cells["SoLuongMuon"].Value ?? 0);
                    if (checked_ && sl > 0)
                        sachMuon.Add((Convert.ToInt32(row.Cells["MaSach"].Value), sl));
                }
                if (sachMuon.Count == 0) { MessageBox.Show("Chọn ít nhất 1 cuốn sách!"); return; }

                var pm = new PhieuMuon
                {
                    MaDG = dg.Value,
                    MaNV = Session.CurrentUser!.MaNV,
                    NgayMuon = dtpMuon.Value,
                    HanTra = dtpMuon.Value.AddDays((double)nudDays.Value),
                    TrangThai = "Đang mượn"
                };

                bool ok = DataAccess.InsertPhieuMuonFull(pm, sachMuon, out string? failureReason);
                if (!ok)
                {
                    MessageBox.Show(failureReason ?? "Không thể tạo phiếu mượn.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                frm.Close();
                LoadData();
            };
            btnCancel.Click += (s, e) => frm.Close();

            frm.Controls.AddRange(new Control[] { lbl1, cboDG, lbl2, dtpMuon, lbl3, nudDays, lbl4, dgvSach, btnOk, btnCancel });
            frm.ShowDialog();
        }

        private class ComboItem
        {
            public string Text { get; }
            public int Value { get; }
            public ComboItem(string text, int value) { Text = text; Value = value; }
            public override string ToString() => Text;
        }
    }
}

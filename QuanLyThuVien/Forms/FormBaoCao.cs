using System.Data;
using QuanLyThuVien.Controls;
using QuanLyThuVien.Data;
using QuanLyThuVien.Helpers;

namespace QuanLyThuVien.Forms
{
    public class FormBaoCao : UserControl
    {
        public FormBaoCao()
        {
            BackColor = AppColors.ContentBg;
            Padding = new Padding(10);
            AutoScroll = true;
            Load += (s, e) => BuildUI();
        }

        private void BuildUI()
        {
            Controls.Clear();

            Controls.Add(new Label
            {
                Text = "Báo cáo & Thống kê",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = AppColors.TextPrimary,
                AutoSize = true,
                Location = new Point(10, 10)
            });

            int y = 60;

            int tongSach = DataAccess.CountSach();
            int tongDG = DataAccess.CountDocGia();
            int dangMuon = DataAccess.CountPhieuMuonDangMo();
            decimal tienPhat = DataAccess.GetTongTienPhat();

            var stats = new (string label, string value, Color color)[]
            {
                ("Tổng sách", tongSach.ToString(), AppColors.Primary),
                ("Tổng ĐG", tongDG.ToString(), AppColors.Success),
                ("Đang mượn", dangMuon.ToString(), AppColors.Warning),
                ("Tổng tiền phạt", $"{tienPhat:N0}đ", AppColors.TextPrimary)
            };

            var statCards = new RoundedPanel[stats.Length];
            int gap = 12;
            int cardW = (Width - 30 - gap * (stats.Length - 1)) / stats.Length;
            for (int i = 0; i < stats.Length; i++)
            {
                var (label, value, color) = stats[i];
                int cx = 10 + i * (cardW + gap);

                statCards[i] = new RoundedPanel
                {
                    Location = new Point(cx, y),
                    Size = new Size(cardW, 110),
                    BackColor = Color.White,
                    BorderRadius = 12
                };

                var lblV = new Label
                {
                    Text = value,
                    Font = new Font("Segoe UI", 22F, FontStyle.Bold),
                    ForeColor = color,
                    AutoSize = true,
                    Location = new Point(15, 15)
                };
                var lblL = new Label
                {
                    Text = label,
                    Font = new Font("Segoe UI", 9F),
                    ForeColor = AppColors.TextSecondary,
                    AutoSize = true,
                    Location = new Point(15, 75)
                };
                statCards[i].Controls.Add(lblV);
                statCards[i].Controls.Add(lblL);
                Controls.Add(statCards[i]);
            }
            y += 110;

            // Top sách mượn nhiều
            var lblTopSach = new Label
            {
                Text = "Top sách được mượn nhiều nhất",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = AppColors.TextPrimary,
                AutoSize = true,
                Location = new Point(10, y)
            };
            Controls.Add(lblTopSach);
            y += 35;

            var dgvTopSach = CreateDataGridView();
            dgvTopSach.Location = new Point(10, y);
            dgvTopSach.Size = new Size(Width - 30, 250);
            dgvTopSach.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            dgvTopSach.Columns.Add("STT", "#");
            dgvTopSach.Columns.Add("TenSach", "Tên sách");
            dgvTopSach.Columns.Add("SoLanMuon", "Số lần mượn");
            dgvTopSach.Columns[0].FillWeight = 40;
            dgvTopSach.Columns[2].FillWeight = 120;

            try
            {
                var dt = DataAccess.GetSachMuonNhieuNhat(10);
                int stt = 1;
                foreach (DataRow row in dt.Rows)
                    dgvTopSach.Rows.Add(stt++, row["TenSach"], row["SoLanMuon"]);
            }
            catch { }
            Controls.Add(dgvTopSach);
            y += 270;

            // Sách sắp hết
            var lblHetSach = new Label
            {
                Text = "Sách sắp hết (SL <= 2)",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = AppColors.Danger,
                AutoSize = true,
                Location = new Point(10, y)
            };
            Controls.Add(lblHetSach);
            y += 35;

            var dgvHetSach = CreateDataGridView();
            dgvHetSach.Location = new Point(10, y);
            dgvHetSach.Size = new Size(Width - 30, 180);
            dgvHetSach.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            dgvHetSach.Columns.Add("TenSach", "Tên sách");
            dgvHetSach.Columns.Add("SoLuong", "Số lượng còn");

            try
            {
                var dt = DataAccess.ExecuteQuery("SELECT TenSach, SoLuong FROM Sach WHERE SoLuong<=2 ORDER BY SoLuong ASC");
                foreach (DataRow row in dt.Rows)
                    dgvHetSach.Rows.Add(row["TenSach"], row["SoLuong"]);
            }
            catch { }
            Controls.Add(dgvHetSach);

            Resize += (s, e) =>
            {
                int gap = 12;
                int cardW = (Width - 30 - gap * (stats.Length - 1)) / stats.Length;
                for (int i = 0; i < statCards.Length; i++)
                {
                    statCards[i].Location = new Point(10 + i * (cardW + gap), statCards[i].Location.Y);
                    statCards[i].Width = cardW;
                }
                dgvTopSach.Width = Width - 30;
                dgvHetSach.Width = Width - 30;
            };
        }

        private DataGridView CreateDataGridView()
        {
            return new ModernDataGridView
            {
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
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                },
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 35,
                DefaultCellStyle = new DataGridViewCellStyle { Font = new Font("Segoe UI", 10F) },
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(245, 245, 245) }
            };
        }
    }
}

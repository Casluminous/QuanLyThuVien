using System.Data;
using QuanLyThuVien.Controls;
using QuanLyThuVien.Data;
using QuanLyThuVien.Helpers;
using QuanLyThuVien.Models;

namespace QuanLyThuVien.Forms
{
    public class FormSach : UserControl
    {
        private DataGridView dgv;
        private Panel pnlCatalog;
        private Panel pnlToggle;
        private Button btnCatalog;
        private Button btnTable;
        private bool isCatalogView = true;

        private static readonly Color[] GradientPairs_Start = {
            Color.FromArgb(247, 194, 194),   // Soft Coral Pink
            Color.FromArgb(210, 235, 212),   // Soft Mint Green
            Color.FromArgb(212, 230, 241),   // Soft Sky Blue
            Color.FromArgb(254, 249, 231),   // Soft Yellow
            Color.FromArgb(232, 218, 239),   // Soft Lavender
            Color.FromArgb(253, 235, 208),   // Soft Peach
            Color.FromArgb(224, 242, 241),   // Soft Teal
            Color.FromArgb(242, 239, 233),   // Soft Sand Gray
        };

        private static readonly Color[] GradientPairs_End = {
            Color.FromArgb(245, 158, 158),
            Color.FromArgb(163, 217, 165),
            Color.FromArgb(169, 204, 227),
            Color.FromArgb(249, 231, 159),
            Color.FromArgb(195, 155, 211),
            Color.FromArgb(245, 203, 167),
            Color.FromArgb(178, 223, 219),
            Color.FromArgb(222, 217, 206),
        };

        private string imagesDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuanLyThuVien", "Images", "Sach");

        public FormSach()
        {
            BackColor = AppColors.ContentBg;
            Padding = new Padding(10);
            Load += (s, e) => LoadData();
            Resize += (s, e) =>
            {
                if (dgv != null) { dgv.Width = Width - 30; dgv.Height = Height - 80; }
                if (pnlCatalog != null) { pnlCatalog.Width = Width - 30; pnlCatalog.Height = Height - 80; LayoutCatalog(); }
            };
        }

        private void LoadData()
        {
            Controls.Clear();
            Directory.CreateDirectory(imagesDir);

            // Header
            Controls.Add(new Label
            {
                Text = "Quản lý Sách",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = AppColors.TextPrimary,
                AutoSize = true,
                Location = new Point(10, 18)
            });

            var btnThem = new ModernButton
            {
                Text = "+ Thêm mới",
                Location = new Point(210, 14),
                Size = new Size(130, 38),
                BaseColor = AppColors.Success,
                HoverColor = Color.FromArgb(39, 174, 96),
                BorderRadius = 8
            };
            btnThem.Click += (s, e) => ShowInputDialog();
            Controls.Add(btnThem);

            // Toggle buttons
            pnlToggle = new Panel
            {
                Location = new Point(Width - 180, 14),
                Size = new Size(150, 36),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.FromArgb(230, 230, 230)
            };

            btnTable = new Button
            {
                Text = "Bảng",
                Size = new Size(75, 36),
                Location = new Point(0, 0),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                BackColor = Color.Transparent,
                ForeColor = AppColors.TextSecondary,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                Cursor = Cursors.Hand
            };
            btnTable.Click += (s, e) => SwitchView(false);

            btnCatalog = new Button
            {
                Text = "Thư viện",
                Size = new Size(75, 36),
                Location = new Point(75, 0),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                BackColor = Color.White,
                ForeColor = AppColors.TextPrimary,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCatalog.Click += (s, e) => SwitchView(true);

            pnlToggle.Controls.AddRange(new Control[] { btnTable, btnCatalog });
            Controls.Add(pnlToggle);

            // DataGridView (hidden by default)
            dgv = new ModernDataGridView
            {
                Location = new Point(10, 65),
                Size = new Size(Width - 30, Height - 80),
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
                DefaultCellStyle = new DataGridViewCellStyle { Font = new Font("Segoe UI", 10F) },
                Visible = false
            };
            dgv.Columns.Add("MaSach", "Mã");
            dgv.Columns.Add("TenSach", "Tên sách");
            dgv.Columns.Add("MaISBN", "ISBN");
            dgv.Columns.Add("TenTheLoai", "Thể loại");
            dgv.Columns.Add("TenTacGia", "Tác giả");
            dgv.Columns.Add("TenNXB", "NXB");
            dgv.Columns.Add("NamXB", "Năm XB");
            dgv.Columns.Add("SoLuong", "SL");
            dgv.Columns.Add("GiaTien", "Giá tiền");
            dgv.Columns.Add("btnSửa", "Sửa");
            dgv.Columns.Add("btnXóa", "Xóa");
            dgv.CellClick += Dgv_CellClick;
            Controls.Add(dgv);

            // Catalog panel
            pnlCatalog = new Panel
            {
                Location = new Point(10, 65),
                Size = new Size(Width - 30, Height - 80),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                AutoScroll = true,
                BackColor = Color.Transparent
            };
            Controls.Add(pnlCatalog);

            LoadCatalogData();
            LoadTableData();
        }

        private void SwitchView(bool catalog)
        {
            isCatalogView = catalog;
            pnlCatalog.Visible = catalog;
            dgv.Visible = !catalog;

            if (catalog)
            {
                btnCatalog.BackColor = Color.White;
                btnCatalog.ForeColor = AppColors.TextPrimary;
                btnCatalog.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
                btnTable.BackColor = Color.Transparent;
                btnTable.ForeColor = AppColors.TextSecondary;
                btnTable.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            }
            else
            {
                btnTable.BackColor = Color.White;
                btnTable.ForeColor = AppColors.TextPrimary;
                btnTable.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
                btnCatalog.BackColor = Color.Transparent;
                btnCatalog.ForeColor = AppColors.TextSecondary;
                btnCatalog.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            }
        }

        private void LoadCatalogData()
        {
            pnlCatalog.Controls.Clear();
            try
            {
                var dt = DataAccess.GetAllSach();
                int index = 0;
                foreach (DataRow row in dt.Rows)
                {
                    int maSach = Convert.ToInt32(row["MaSach"]);
                    string tenSach = row["TenSach"].ToString() ?? "";
                    string tacGia = row["TenTacGia"].ToString() ?? "";
                    string theLoai = row["TenTheLoai"].ToString() ?? "";
                    decimal giaTien = Convert.ToDecimal(row["GiaTien"]);
                    string hinhAnh = row["HinhAnh"].ToString() ?? "";

                    int colorIndex = index % GradientPairs_Start.Length;

                    var card = new BookCardControl
                    {
                        Title = tenSach,
                        Author = tacGia,
                        Price = $"{giaTien:N0}đ",
                        Genre = theLoai,
                        GradientStart = GradientPairs_Start[colorIndex],
                        GradientEnd = GradientPairs_End[colorIndex],
                        Tag = maSach
                    };

                    // Load cover image
                    if (!string.IsNullOrEmpty(hinhAnh) && File.Exists(hinhAnh))
                    {
                        try
                        {
                            using (var fs = new FileStream(hinhAnh, FileMode.Open, FileAccess.Read))
                            using (var original = Image.FromStream(fs))
                                card.CoverImage = original.GetThumbnailImage(400, 300, null, IntPtr.Zero);
                        }
                        catch { }
                    }

                    card.Click += BookCard_Click;
                    foreach (Control c in card.Controls)
                        c.Click += BookCard_Click;

                    pnlCatalog.Controls.Add(card);
                    index++;
                }
                LayoutCatalog();
            }
            catch (Exception ex) { MessageBox.Show("Lỗi: " + ex.Message); }
        }

        private void LayoutCatalog()
        {
            if (pnlCatalog.Controls.Count == 0) return;

            int cols = 3;
            int cardWidth = 250;
            int cardHeight = 340;
            int spacingX = 20;
            int spacingY = 20;
            int startX = 10;
            int startY = 10;

            for (int i = 0; i < pnlCatalog.Controls.Count; i++)
            {
                int row = i / cols;
                int col = i % cols;
                pnlCatalog.Controls[i].Location = new Point(startX + col * (cardWidth + spacingX), startY + row * (cardHeight + spacingY));
            }
        }

        private void LoadTableData()
        {
            dgv.Rows.Clear();
            try
            {
                var dt = DataAccess.GetAllSach();
                foreach (DataRow row in dt.Rows)
                    dgv.Rows.Add(row["MaSach"], row["TenSach"], row["MaISBN"], row["TenTheLoai"],
                        row["TenTacGia"], row["TenNXB"], row["NamXB"], row["SoLuong"],
                        row["GiaTien"], "Sửa", "Xóa");
            }
            catch { }
        }

        private void BookCard_Click(object? sender, EventArgs e)
        {
            Control? ctrl = sender as Control;
            while (ctrl != null && ctrl is not BookCardControl)
                ctrl = ctrl.Parent;

            if (ctrl is BookCardControl card && card.Tag is int maSach)
            {
                ShowBookDetail(maSach);
            }
        }

        private void ShowBookDetail(int maSach)
        {
            var dt = DataAccess.GetAllSach();
            DataRow? targetRow = null;
            foreach (DataRow row in dt.Rows)
            {
                if (Convert.ToInt32(row["MaSach"]) == maSach)
                {
                    targetRow = row;
                    break;
                }
            }
            if (targetRow == null) return;

            var sach = new Sach
            {
                MaSach = maSach,
                TenSach = targetRow["TenSach"].ToString() ?? "",
                MaISBN = targetRow["MaISBN"].ToString() ?? "",
                MaTL = Convert.ToInt32(targetRow["MaTL"]),
                MaTG = Convert.ToInt32(targetRow["MaTG"]),
                MaNXB = Convert.ToInt32(targetRow["MaNXB"]),
                NamXB = Convert.ToInt32(targetRow["NamXB"]),
                SoLuong = Convert.ToInt32(targetRow["SoLuong"]),
                GiaTien = Convert.ToDecimal(targetRow["GiaTien"]),
                MoTa = targetRow["MoTa"].ToString() ?? "",
                HinhAnh = targetRow["HinhAnh"].ToString() ?? ""
            };

            var frm = new Form
            {
                Text = sach.TenSach,
                Size = new Size(520, 700),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = AppColors.ContentBg,
                Font = new Font("Segoe UI", 10F),
                AutoScroll = true
            };

            // Cover image
            var pbCover = new PictureBox
            {
                Size = new Size(480, 280),
                Location = new Point(20, 15),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(230, 230, 230),
                BorderStyle = BorderStyle.None
            };

            if (!string.IsNullOrEmpty(sach.HinhAnh) && File.Exists(sach.HinhAnh))
            {
                try
                {
                    using (var fs = new FileStream(sach.HinhAnh, FileMode.Open, FileAccess.Read))
                    using (var original = Image.FromStream(fs))
                        pbCover.Image = (Image)original.Clone();
                }
                catch { }
            }

            var btnUpload = new ModernButton
            {
                Text = "Crop ảnh",
                Location = new Point(20, 305),
                Size = new Size(140, 32),
                BaseColor = AppColors.Info,
                HoverColor = Color.FromArgb(124, 58, 237),
                BorderRadius = 6
            };

            Image? pendingCroppedImage = null;

            var btnLuuAnh = new ModernButton
            {
                Text = "Lưu ảnh",
                Location = new Point(120, 305),
                Size = new Size(100, 32),
                BaseColor = AppColors.Success,
                HoverColor = Color.FromArgb(39, 174, 96),
                BorderRadius = 6,
                Visible = false
            };

            btnLuuAnh.Click += (s, e) =>
            {
                if (pendingCroppedImage == null) return;
                try
                {
                    string destFile = Path.Combine(imagesDir, $"{maSach}_{Guid.NewGuid():N}.jpg");
                    pendingCroppedImage.Save(destFile, System.Drawing.Imaging.ImageFormat.Jpeg);
                    sach.HinhAnh = destFile;

                    using (var conn = DataAccess.GetConnection())
                    {
                        conn.Open();
                        using var cmd = new System.Data.SqlClient.SqlCommand("UPDATE Sach SET HinhAnh = @HinhAnh WHERE MaSach = @MaSach", conn);
                        cmd.Parameters.AddWithValue("@HinhAnh", destFile);
                        cmd.Parameters.AddWithValue("@MaSach", maSach);
                        cmd.ExecuteNonQuery();
                    }

                    using var fs = new FileStream(destFile, FileMode.Open, FileAccess.Read);
                    pbCover.Image = Image.FromStream(fs);

                    pendingCroppedImage.Dispose();
                    pendingCroppedImage = null;
                    btnLuuAnh.Visible = false;
                    btnUpload.Visible = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi khi lưu ảnh: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            btnUpload.Click += (s, e) =>
            {
                using (var ofd = new OpenFileDialog())
                {
                    ofd.Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp;*.gif|All files|*.*";
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        try
                        {
                            var original = Image.FromFile(ofd.FileName);
                            using var cropForm = new FormCropImage(original);
                            var result = cropForm.ShowDialog();
                            if (result == DialogResult.OK && cropForm.CroppedImage != null)
                            {
                                pendingCroppedImage = cropForm.CroppedImage;
                                pbCover.Image = pendingCroppedImage;
                                btnUpload.Visible = false;
                                btnLuuAnh.Visible = true;
                            }
                            original.Dispose();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Lỗi khi mở form crop: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            };

            int y = 350;
            var lblTenSach = new Label { Text = sach.TenSach, Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold), ForeColor = AppColors.TextPrimary, AutoSize = true, Location = new Point(20, y) };
            y += 35;

            var lblISBN = new Label { Text = $"ISBN: {sach.MaISBN}", Font = new Font("Segoe UI", 10F), ForeColor = AppColors.TextSecondary, AutoSize = true, Location = new Point(20, y) };
            y += 25;

            var lblNamXB = new Label { Text = $"Năm XB: {sach.NamXB}", Font = new Font("Segoe UI", 10F), ForeColor = AppColors.TextSecondary, AutoSize = true, Location = new Point(20, y) };
            y += 25;

            var lblSoLuong = new Label { Text = $"Số lượng: {sach.SoLuong}", Font = new Font("Segoe UI", 10F), ForeColor = AppColors.TextSecondary, AutoSize = true, Location = new Point(20, y) };
            y += 25;

            var lblGia = new Label { Text = $"Giá: {sach.GiaTien:N0}đ", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = AppColors.Primary, AutoSize = true, Location = new Point(20, y) };
            y += 30;

            var lblMoTa = new Label { Text = sach.MoTa, Font = new Font("Segoe UI", 10F), ForeColor = AppColors.TextSecondary, AutoSize = false, Size = new Size(470, 60), Location = new Point(20, y) };
            y += 70;

            var btnEdit = new ModernButton
            {
                Text = "Sửa",
                Location = new Point(20, y),
                Size = new Size(100, 38),
                BaseColor = AppColors.Primary,
                HoverColor = AppColors.PrimaryDark,
                BorderRadius = 8
            };
            btnEdit.Click += (s, e) => { frm.Close(); ShowInputDialog(sach); };

            var btnDelete = new ModernButton
            {
                Text = "Xóa",
                Location = new Point(130, y),
                Size = new Size(100, 38),
                BaseColor = AppColors.Danger,
                HoverColor = Color.FromArgb(220, 53, 69),
                BorderRadius = 8
            };
            btnDelete.Click += (s, e) =>
            {
                if (MessageBox.Show("Xóa sách này?", "Xác nhận", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    try
                    {
                        DataAccess.DeleteSach(maSach);
                        frm.Close();
                        LoadData();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Không thể xóa sách này!\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };

            var btnClose = new ModernButton
            {
                Text = "Đóng",
                Location = new Point(380, y),
                Size = new Size(100, 38),
                BaseColor = AppColors.TextSecondary,
                HoverColor = Color.FromArgb(100, 100, 100),
                BorderRadius = 8
            };
            btnClose.Click += (s, e) => frm.Close();

            frm.Controls.AddRange(new Control[] { pbCover, btnUpload, btnLuuAnh, lblTenSach, lblISBN, lblNamXB, lblSoLuong, lblGia, lblMoTa, btnEdit, btnDelete, btnClose });
            frm.ShowDialog();
        }

        private void Dgv_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            int maSach = Convert.ToInt32(dgv.Rows[e.RowIndex].Cells["MaSach"].Value);

            if (dgv.Columns[e.ColumnIndex].Name == "btnSửa")
            {
                var row = dgv.Rows[e.RowIndex];
                var fkRow = DataAccess.ExecuteQuery(
                    "SELECT MaTL, MaTG, MaNXB, MoTa, HinhAnh FROM Sach WHERE MaSach=@ma",
                    new System.Data.SqlClient.SqlParameter("@ma", maSach));
                var sach = new Sach
                {
                    MaSach = maSach,
                    TenSach = row.Cells["TenSach"].Value?.ToString() ?? "",
                    MaISBN = row.Cells["MaISBN"].Value?.ToString() ?? "",
                    MaTL = fkRow.Rows.Count > 0 ? Convert.ToInt32(fkRow.Rows[0]["MaTL"]) : 0,
                    MaTG = fkRow.Rows.Count > 0 ? Convert.ToInt32(fkRow.Rows[0]["MaTG"]) : 0,
                    MaNXB = fkRow.Rows.Count > 0 ? Convert.ToInt32(fkRow.Rows[0]["MaNXB"]) : 0,
                    NamXB = Convert.ToInt32(row.Cells["NamXB"].Value),
                    SoLuong = Convert.ToInt32(row.Cells["SoLuong"].Value),
                    GiaTien = Convert.ToDecimal(row.Cells["GiaTien"].Value),
                    MoTa = fkRow.Rows.Count > 0 ? fkRow.Rows[0]["MoTa"].ToString() ?? "" : "",
                    HinhAnh = fkRow.Rows.Count > 0 ? fkRow.Rows[0]["HinhAnh"].ToString() ?? "" : ""
                };
                ShowInputDialog(sach);
            }
            else if (dgv.Columns[e.ColumnIndex].Name == "btnXóa")
            {
                if (MessageBox.Show("Xóa sách này?", "Xác nhận", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    try
                    {
                        DataAccess.DeleteSach(maSach);
                        LoadData();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Không thể xóa sách này!\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void ShowInputDialog(Sach? existing = null)
        {
            var frm = new Form
            {
                Text = existing != null ? "Sửa sách" : "Thêm sách",
                Size = new Size(450, 650),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 10F),
                AutoScroll = true
            };

            var tlData = DataAccess.GetAllTheLoai();
            var tgData = DataAccess.GetAllTacGia();
            var nxbData = DataAccess.GetAllNXB();

            int y = 15;
            var lbl1 = new Label { Text = "Tên sách:", Location = new Point(20, y + 3), AutoSize = true };
            var txt1 = new ModernTextBox { Text = existing?.TenSach ?? "", Location = new Point(140, y), Size = new Size(270, 30) };
            y += 40;

            var lbl2 = new Label { Text = "ISBN:", Location = new Point(20, y + 3), AutoSize = true };
            var txt2 = new ModernTextBox { Text = existing?.MaISBN ?? "", Location = new Point(140, y), Size = new Size(270, 30) };
            y += 40;

            var lbl3 = new Label { Text = "Thể loại:", Location = new Point(20, y + 3), AutoSize = true };
            var cboTL = new ModernComboBox { Location = new Point(140, y), Size = new Size(270, 30), DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (DataRow row in tlData.Rows) cboTL.Items.Add(new ComboItem(row["TenTheLoai"].ToString()!, Convert.ToInt32(row["MaTL"])));
            if (existing != null)
            {
                foreach (ComboItem item in cboTL.Items)
                    if (item.Value == existing.MaTL) { cboTL.SelectedItem = item; break; }
            }
            if (cboTL.SelectedIndex < 0 && cboTL.Items.Count > 0) cboTL.SelectedIndex = 0;
            y += 40;

            var lbl4 = new Label { Text = "Tác giả:", Location = new Point(20, y + 3), AutoSize = true };
            var cboTG = new ModernComboBox { Location = new Point(140, y), Size = new Size(270, 30), DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (DataRow row in tgData.Rows) cboTG.Items.Add(new ComboItem(row["TenTG"].ToString()!, Convert.ToInt32(row["MaTG"])));
            if (existing != null)
            {
                foreach (ComboItem item in cboTG.Items)
                    if (item.Value == existing.MaTG) { cboTG.SelectedItem = item; break; }
            }
            if (cboTG.SelectedIndex < 0 && cboTG.Items.Count > 0) cboTG.SelectedIndex = 0;
            y += 40;

            var lbl5 = new Label { Text = "NXB:", Location = new Point(20, y + 3), AutoSize = true };
            var cboNXB = new ModernComboBox { Location = new Point(140, y), Size = new Size(270, 30), DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (DataRow row in nxbData.Rows) cboNXB.Items.Add(new ComboItem(row["TenNXB"].ToString()!, Convert.ToInt32(row["MaNXB"])));
            if (existing != null)
            {
                foreach (ComboItem item in cboNXB.Items)
                    if (item.Value == existing.MaNXB) { cboNXB.SelectedItem = item; break; }
            }
            if (cboNXB.SelectedIndex < 0 && cboNXB.Items.Count > 0) cboNXB.SelectedIndex = 0;
            y += 40;

            var lbl6 = new Label { Text = "Năm XB:", Location = new Point(20, y + 3), AutoSize = true };
            var nudNam = new NumericUpDown { Location = new Point(140, y), Size = new Size(100, 30), Minimum = 1900, Maximum = 2030, Value = existing?.NamXB ?? 2024 };
            y += 40;

            var lbl7 = new Label { Text = "Số lượng:", Location = new Point(20, y + 3), AutoSize = true };
            var nudSL = new NumericUpDown { Location = new Point(140, y), Size = new Size(100, 30), Minimum = 0, Maximum = 10000, Value = existing?.SoLuong ?? 1 };
            y += 40;

            var lbl8 = new Label { Text = "Giá tiền:", Location = new Point(20, y + 3), AutoSize = true };
            var nudGia = new NumericUpDown { Location = new Point(140, y), Size = new Size(150, 30), Minimum = 0, Maximum = 100000000, DecimalPlaces = 0, Value = existing?.GiaTien ?? 0 };
            y += 40;

            var lbl9 = new Label { Text = "Mô tả:", Location = new Point(20, y + 3), AutoSize = true };
            var txtMoTa = new ModernTextBox { Text = existing?.MoTa ?? "", Location = new Point(140, y), Size = new Size(270, 60), Multiline = true };
            y += 70;

            // Image upload in dialog
            var lbl10 = new Label { Text = "Ảnh bìa:", Location = new Point(20, y + 3), AutoSize = true };
            var txtHinhAnh = new ModernTextBox { Text = existing?.HinhAnh ?? "", Location = new Point(140, y), Size = new Size(170, 30), ReadOnly = true };
            var btnChonAnh = new ModernButton { Text = "...", Location = new Point(320, y), Size = new Size(40, 30), BaseColor = AppColors.TextSecondary, BorderRadius = 4 };
            var pbPreview = new PictureBox { Size = new Size(100, 80), Location = new Point(140, y + 35), SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.FromArgb(240, 240, 240) };

            if (!string.IsNullOrEmpty(existing?.HinhAnh) && File.Exists(existing.HinhAnh))
            {
                try
                {
                    using var fs = new FileStream(existing.HinhAnh, FileMode.Open, FileAccess.Read);
                    using var original = Image.FromStream(fs);
                    pbPreview.Image = (Image)original.Clone();
                }
                catch { }
            }

            btnChonAnh.Click += (s, e) =>
            {
                using (var ofd = new OpenFileDialog())
                {
                    ofd.Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp;*.gif|All files|*.*";
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        txtHinhAnh.Text = ofd.FileName;
                        try
                        {
                            using var fs = new FileStream(ofd.FileName, FileMode.Open, FileAccess.Read);
                            pbPreview.Image = Image.FromStream(fs);
                        }
                        catch { }
                    }
                }
            };

            var btnOk = new ModernButton { Text = "Lưu", Location = new Point(140, y + 125), Size = new Size(120, 40), BaseColor = AppColors.Primary, BorderRadius = 8 };
            var btnCancel = new ModernButton { Text = "Hủy", Location = new Point(280, y + 125), Size = new Size(120, 40), BaseColor = AppColors.TextSecondary, BorderRadius = 8 };

            btnOk.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txt1.Text)) { MessageBox.Show("Nhập tên sách!"); return; }
                if (cboTL.SelectedItem is not ComboItem tl || cboTG.SelectedItem is not ComboItem tg || cboNXB.SelectedItem is not ComboItem nxb)
                { MessageBox.Show("Chọn đầy đủ thông tin!"); return; }

                string hinhAnh = txtHinhAnh.Text.Trim();
                if (!string.IsNullOrEmpty(hinhAnh) && File.Exists(hinhAnh))
                {
                    string destFile = Path.Combine(imagesDir, $"{existing?.MaSach ?? 0}_{Path.GetFileName(hinhAnh)}");
                    try
                    {
                        File.Copy(hinhAnh, destFile, true);
                        hinhAnh = destFile;
                    }
                    catch { }
                }

                var sach = new Sach
                {
                    MaSach = existing?.MaSach ?? 0,
                    TenSach = txt1.Text.Trim(),
                    MaISBN = txt2.Text.Trim(),
                    MaTL = tl.Value,
                    MaTG = tg.Value,
                    MaNXB = nxb.Value,
                    NamXB = (int)nudNam.Value,
                    SoLuong = (int)nudSL.Value,
                    GiaTien = nudGia.Value,
                    MoTa = txtMoTa.Text.Trim(),
                    HinhAnh = hinhAnh
                };

                if (existing != null) DataAccess.UpdateSach(sach);
                else DataAccess.InsertSach(sach);
                frm.Close();
                LoadData();
            };
            btnCancel.Click += (s, e) => frm.Close();

            frm.Controls.AddRange(new Control[] { lbl1, txt1, lbl2, txt2, lbl3, cboTL, lbl4, cboTG, lbl5, cboNXB, lbl6, nudNam, lbl7, nudSL, lbl8, nudGia, lbl9, txtMoTa, lbl10, txtHinhAnh, btnChonAnh, pbPreview, btnOk, btnCancel });
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

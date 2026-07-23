using System.Data;
using QuanLyThuVien.Controls;
using QuanLyThuVien.Data;
using QuanLyThuVien.Helpers;
using QuanLyThuVien.Models;
using QuanLyThuVien.Pdf;

namespace QuanLyThuVien.Forms
{
    public class FormSach : UserControl
    {
        private DataGridView dgv = null!;
        private Panel pnlCatalog = null!;
        private ModernButton btnCatalog = null!;
        private ModernButton btnTable = null!;
        private bool isCatalogView = true;
        private int? _pendingBookDetail;
        private string _bookKeyword = string.Empty;
        private int? _bookGenre;
        private bool _bookOnlyAvailable;

        private readonly BookImageStorage _bookImageStorage = new();

        public FormSach()
        {
            BackColor = AppColors.ContentBg;
            Padding = new Padding(10);
            Load += (s, e) => LoadData();
            Resize += (s, e) =>
            {
                if (pnlCatalog != null) LayoutCatalog();
            };
        }

        private void LoadData()
        {
            ResponsiveUi.DisposeChildren(this);

            var btnThem = PageHeader.CreatePrimaryAction("+ Thêm sách", (_, _) => ShowInputDialog(), 140);

            btnTable = new ModernButton
            {
                Text = "Bảng",
                Size = new Size(88, 40),
                BaseColor = AppColors.CardBg,
                HoverColor = AppColors.HoverSurface,
                PressedColor = AppColors.SelectedSurface,
                TextColor = AppColors.TextSecondary,
                BorderRadius = 12,
                Font = new Font("Segoe UI", 10F),
                AccessibleName = "Xem sách dạng bảng"
            };
            btnTable.Click += (s, e) => SwitchView(false);

            btnCatalog = new ModernButton
            {
                Text = "Thư viện",
                Size = new Size(104, 40),
                BaseColor = AppColors.PrimaryLight,
                HoverColor = AppColors.SelectedSurface,
                PressedColor = AppColors.Focus,
                TextColor = AppColors.PrimaryDark,
                BorderRadius = 12,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                AccessibleName = "Xem sách dạng thư viện"
            };
            btnCatalog.Click += (s, e) => SwitchView(true);

            var content = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = Padding.Empty
            };
            var viewHost = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                BackColor = Color.Transparent
            };

            // DataGridView (hidden by default)
            dgv = new ModernDataGridView
            {
                Dock = DockStyle.Fill,
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
            viewHost.Controls.Add(dgv);

            // Catalog panel
            pnlCatalog = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.Transparent
            };
            pnlCatalog.Resize += (_, _) => LayoutCatalog();
            viewHost.Controls.Add(pnlCatalog);

            var filterBar = new FilterBar("Tìm tên sách, ISBN, tác giả...");
            filterBar.SearchBox.Text = _bookKeyword;
            var genreFilter = new ModernComboBox { Size = new Size(160, 36), DropDownStyle = ComboBoxStyle.DropDownList };
            genreFilter.Items.Add(new ComboItem("Tất cả thể loại", 0));
            foreach (DataRow row in DataAccess.GetAllTheLoai().Rows)
                genreFilter.Items.Add(new ComboItem(row["TenTheLoai"].ToString() ?? string.Empty, Convert.ToInt32(row["MaTL"])));
            genreFilter.SelectedIndex = Math.Max(0, genreFilter.Items.Cast<ComboItem>().ToList().FindIndex(item => item.Value == (_bookGenre ?? 0)));
            filterBar.AddFilter(genreFilter, "Lọc thể loại sách");
            var availableFilter = new ModernComboBox { Size = new Size(170, 36), DropDownStyle = ComboBoxStyle.DropDownList };
            availableFilter.Items.AddRange(new object[] { "Tất cả số lượng", "Còn trong kho" });
            availableFilter.SelectedIndex = _bookOnlyAvailable ? 1 : 0;
            filterBar.AddFilter(availableFilter, "Lọc sách còn trong kho");
            filterBar.FilterChanged += (_, _) =>
            {
                _bookKeyword = filterBar.SearchBox.Text;
                _bookGenre = (genreFilter.SelectedItem as ComboItem)?.Value is int genre && genre > 0 ? genre : null;
                _bookOnlyAvailable = availableFilter.SelectedIndex == 1;
                LoadCatalogData();
                LoadTableData();
            };
            content.Controls.Add(viewHost);

            var btnExport = PageHeader.CreatePrimaryAction("Xuất PDF", (_, _) => PdfExportService.ExportGrid(dgv, "Danh sách sách", "sach", FindForm() is IWin32Window owner ? owner : this), 105);
            var header = new PageHeader("Quản lý Sách", btnThem, btnTable, btnCatalog, btnExport);
            header.SetFilterBar(filterBar);
            Controls.Add(content);
            Controls.Add(header);

            LoadCatalogData();
            LoadTableData();
            if (_pendingBookDetail is int pendingBookId)
            {
                _pendingBookDetail = null;
                ShowBookDetail(pendingBookId);
            }
        }

        private void SwitchView(bool catalog)
        {
            isCatalogView = catalog;
            pnlCatalog.Visible = catalog;
            dgv.Visible = !catalog;

            if (catalog)
            {
                btnCatalog.BaseColor = AppColors.PrimaryLight;
                btnCatalog.TextColor = AppColors.PrimaryDark;
                btnCatalog.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
                btnTable.BaseColor = AppColors.CardBg;
                btnTable.TextColor = AppColors.TextSecondary;
                btnTable.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            }
            else
            {
                btnTable.BaseColor = AppColors.PrimaryLight;
                btnTable.TextColor = AppColors.PrimaryDark;
                btnTable.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
                btnCatalog.BaseColor = AppColors.CardBg;
                btnCatalog.TextColor = AppColors.TextSecondary;
                btnCatalog.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            }
        }

        private void LoadCatalogData()
        {
            ResponsiveUi.DisposeChildren(pnlCatalog);
            try
            {
                var dt = DataAccess.GetAllSach(_bookKeyword, _bookGenre, _bookOnlyAvailable);
                int index = 0;
                foreach (DataRow row in dt.Rows)
                {
                    int maSach = Convert.ToInt32(row["MaSach"]);
                    string tenSach = row["TenSach"].ToString() ?? "";
                    string tacGia = row["TenTacGia"].ToString() ?? "";
                    string theLoai = row["TenTheLoai"].ToString() ?? "";
                    decimal giaTien = Convert.ToDecimal(row["GiaTien"]);
                    string hinhAnh = row["HinhAnh"].ToString() ?? "";

                    var card = new BookCardControl
                    {
                        Title = tenSach,
                        Author = tacGia,
                        Price = $"{giaTien:N0}đ",
                        Genre = theLoai,
                        Tag = maSach
                    };

                    using var coverImage = _bookImageStorage.LoadImage(hinhAnh);
                    if (coverImage != null)
                    {
                        card.CoverImage = coverImage.GetThumbnailImage(400, 300, null, IntPtr.Zero);
                    }

                    card.Click += BookCard_Click;
                    foreach (Control c in card.Controls)
                        c.Click += BookCard_Click;

                    pnlCatalog.Controls.Add(card);
                    index++;
                }
                LayoutCatalog();
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Tải sách thất bại: {ex}"); MessageBox.Show("Không thể tải dữ liệu sách.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void LayoutCatalog()
        {
            if (pnlCatalog == null) return;

            const int cardWidth = 240;
            const int cardHeight = 340;
            const int spacingX = 20;
            const int spacingY = 20;
            const int startX = 10;
            const int startY = 10;

            if (pnlCatalog.Controls.Count == 0)
            {
                pnlCatalog.AutoScrollMinSize = Size.Empty;
                return;
            }

            int GetColumnCount(int panelWidth)
            {
                int availableWidth = Math.Max(0, panelWidth - startX * 2);
                return Math.Max(1, (availableWidth + spacingX) / (cardWidth + spacingX));
            }

            int GetContentHeight(int columns)
            {
                int rowCount = (pnlCatalog.Controls.Count + columns - 1) / columns;
                return startY * 2 + rowCount * cardHeight + (rowCount - 1) * spacingY;
            }

            int cols = GetColumnCount(pnlCatalog.Width);
            int contentHeight = GetContentHeight(cols);

            if (contentHeight > pnlCatalog.Height)
            {
                cols = GetColumnCount(pnlCatalog.Width - SystemInformation.VerticalScrollBarWidth);
                contentHeight = GetContentHeight(cols);
            }

            for (int i = 0; i < pnlCatalog.Controls.Count; i++)
            {
                int row = i / cols;
                int col = i % cols;
                Control card = pnlCatalog.Controls[i];
                card.Size = new Size(cardWidth, cardHeight);
                card.Location = new Point(
                    startX + col * (cardWidth + spacingX),
                    startY + row * (cardHeight + spacingY));
            }

            pnlCatalog.AutoScrollMinSize = new Size(0, contentHeight);
        }

        private void LoadTableData()
        {
            dgv.Rows.Clear();
            try
            {
                var dt = DataAccess.GetAllSach(_bookKeyword, _bookGenre, _bookOnlyAvailable);
                foreach (DataRow row in dt.Rows)
                    dgv.Rows.Add(row["MaSach"], row["TenSach"], row["MaISBN"], row["TenTheLoai"],
                        row["TenTacGia"], row["TenNXB"], row["NamXB"], row["SoLuong"],
                        row["GiaTien"], "Sửa", "Xóa");
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Không tải được ảnh bìa: {ex}"); }
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

        public void OpenBookDetail(int maSach)
        {
            if (dgv == null)
            {
                _pendingBookDetail = maSach;
                return;
            }
            ShowBookDetail(maSach);
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
                ClientSize = new Size(520, 700),
                MinimumSize = new Size(440, 560),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.Sizable,
                MaximizeBox = true,
                MinimizeBox = false,
                BackColor = AppColors.ContentBg,
                Font = new Font("Segoe UI", 10F),
                AutoScroll = true,
                AutoScrollMinSize = new Size(500, 650)
            };

            // Cover image
            var pbCover = new PictureBox
            {
                Size = new Size(480, 280),
                Location = new Point(20, 15),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = AppColors.HoverSurface,
                BorderStyle = BorderStyle.None
            };

            SetPictureImage(pbCover, _bookImageStorage.LoadImage(sach.HinhAnh));

            var btnUpload = new ModernButton
            {
                Text = "Crop ảnh",
                Location = new Point(20, 305),
                Size = new Size(140, 32),
                BaseColor = AppColors.Info,
                HoverColor = AppColors.InfoDark,
                BorderRadius = 6
            };

            Image? pendingCroppedImage = null;

            var btnLuuAnh = new ModernButton
            {
                Text = "Lưu ảnh",
                Location = new Point(120, 305),
                Size = new Size(100, 32),
                BaseColor = AppColors.Success,
                HoverColor = AppColors.SuccessDark,
                BorderRadius = 6,
                Visible = false
            };

            btnLuuAnh.Click += (s, e) =>
            {
                Image? croppedImage = pendingCroppedImage;
                if (croppedImage == null) return;

                string? createdImageKey = null;
                try
                {
                    createdImageKey = _bookImageStorage.SaveCroppedImage(croppedImage);
                    DataAccess.UpdateSachImage(maSach, createdImageKey);
                    sach.HinhAnh = createdImageKey;
                    SetPictureImage(pbCover, _bookImageStorage.LoadImage(createdImageKey));

                    croppedImage.Dispose();
                    pendingCroppedImage = null;
                    btnLuuAnh.Visible = false;
                    btnUpload.Visible = true;
                }
                catch (Exception ex)
                {
                    _bookImageStorage.DeleteLocalAsset(createdImageKey);
                    System.Diagnostics.Debug.WriteLine($"Lưu ảnh sách thất bại: {ex}");
                    MessageBox.Show("Không thể lưu ảnh bìa.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                            using var original = Image.FromFile(ofd.FileName);
                            using var cropForm = new FormCropImage(original);
                            var result = cropForm.ShowDialog();
                            if (result == DialogResult.OK && cropForm.CroppedImage != null)
                            {
                                pendingCroppedImage?.Dispose();
                                pendingCroppedImage = cropForm.CroppedImage;
                                SetPictureImage(pbCover, (Image)pendingCroppedImage.Clone());
                                btnUpload.Visible = false;
                                btnLuuAnh.Visible = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Mở crop ảnh thất bại: {ex}");
                            MessageBox.Show("Không thể mở công cụ cắt ảnh.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                BorderRadius = 12
            };
            btnEdit.Click += (s, e) => { frm.Close(); ShowInputDialog(sach); };

            var btnDelete = new ModernButton
            {
                Text = "Xóa",
                Location = new Point(130, y),
                Size = new Size(100, 38),
                BaseColor = AppColors.Danger,
                HoverColor = AppColors.DangerDark,
                BorderRadius = 12
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
                        System.Diagnostics.Debug.WriteLine($"Xóa sách thất bại: {ex}");
                        MessageBox.Show("Không thể xóa sách này.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };

            var btnClose = new ModernButton
            {
                Text = "Đóng",
                Location = new Point(380, y),
                Size = new Size(100, 38),
                BaseColor = AppColors.TextSecondary,
                HoverColor = AppColors.PrimaryDark,
                BorderRadius = 12
            };
            btnClose.Click += (s, e) => frm.Close();

            frm.Controls.AddRange(new Control[] { pbCover, btnUpload, btnLuuAnh, lblTenSach, lblISBN, lblNamXB, lblSoLuong, lblGia, lblMoTa, btnEdit, btnDelete, btnClose });
            frm.CancelButton = btnClose;
            btnEdit.AccessibleName = "Sửa thông tin sách";
            btnDelete.AccessibleName = "Xóa sách";
            btnClose.AccessibleName = "Đóng chi tiết sách";
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
                        System.Diagnostics.Debug.WriteLine($"Xóa sách thất bại: {ex}");
                        MessageBox.Show("Không thể xóa sách này.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void ShowInputDialog(Sach? existing = null)
        {
            var frm = new Form
            {
                Text = existing != null ? "Sửa sách" : "Thêm sách",
                ClientSize = new Size(450, 650),
                MinimumSize = new Size(420, 520),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.Sizable,
                MaximizeBox = true,
                MinimizeBox = false,
                BackColor = AppColors.CardBg,
                Font = new Font("Segoe UI", 10F),
                AutoScroll = true,
                AutoScrollMinSize = new Size(430, 610)
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
            var nudNam = new NumericUpDown { Location = new Point(140, y), Size = new Size(100, 30), Minimum = 1900, Maximum = DateTime.Now.Year + 1, Value = existing?.NamXB ?? DateTime.Now.Year };
            y += 40;

            var lbl7 = new Label { Text = "Số lượng còn sẵn:", Location = new Point(20, y + 3), AutoSize = true };
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
            btnChonAnh.AccessibleName = "Chọn ảnh bìa";
            var imageToolTip = new ToolTip();
            imageToolTip.SetToolTip(btnChonAnh, "Chọn ảnh bìa");
            var pbPreview = new PictureBox { Size = new Size(100, 80), Location = new Point(140, y + 35), SizeMode = PictureBoxSizeMode.Zoom, BackColor = AppColors.HoverSurface };
            string? selectedImagePath = null;

            SetPictureImage(pbPreview, _bookImageStorage.LoadImage(existing?.HinhAnh));

            btnChonAnh.Click += (s, e) =>
            {
                using (var ofd = new OpenFileDialog())
                {
                    ofd.Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp;*.gif|All files|*.*";
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        selectedImagePath = ofd.FileName;
                        txtHinhAnh.Text = Path.GetFileName(ofd.FileName);
                        SetPictureImage(pbPreview, _bookImageStorage.LoadImage(selectedImagePath));
                    }
                }
            };

            var btnOk = new ModernButton { Text = "Lưu", Location = new Point(140, y + 125), Size = new Size(120, 40), BaseColor = AppColors.Primary, BorderRadius = 12 };
            var btnCancel = new ModernButton { Text = "Hủy", Location = new Point(280, y + 125), Size = new Size(120, 40), BaseColor = AppColors.TextSecondary, BorderRadius = 12 };

            btnOk.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txt1.Text)) { MessageBox.Show("Nhập tên sách!"); return; }
                if (cboTL.SelectedItem is not ComboItem tl || cboTG.SelectedItem is not ComboItem tg || cboNXB.SelectedItem is not ComboItem nxb)
                { MessageBox.Show("Chọn đầy đủ thông tin!"); return; }

                string hinhAnh = existing?.HinhAnh ?? "";
                string? createdImageKey = null;
                try
                {
                    if (!string.IsNullOrWhiteSpace(selectedImagePath))
                    {
                        createdImageKey = _bookImageStorage.ImportFile(selectedImagePath);
                        hinhAnh = createdImageKey;
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
                }
                catch (Exception ex)
                {
                    _bookImageStorage.DeleteLocalAsset(createdImageKey);
                    System.Diagnostics.Debug.WriteLine($"Lưu ảnh bìa thất bại: {ex}");
                    MessageBox.Show("Không thể lưu ảnh bìa.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                frm.Close();
                LoadData();
            };
            btnCancel.Click += (s, e) => frm.Close();

            frm.Controls.AddRange(new Control[] { lbl1, txt1, lbl2, txt2, lbl3, cboTL, lbl4, cboTG, lbl5, cboNXB, lbl6, nudNam, lbl7, nudSL, lbl8, nudGia, lbl9, txtMoTa, lbl10, txtHinhAnh, btnChonAnh, pbPreview, btnOk, btnCancel });
            frm.AcceptButton = btnOk;
            frm.CancelButton = btnCancel;
            frm.ActiveControl = txt1;
            txt1.AccessibleName = "Tên sách";
            txt2.AccessibleName = "Mã ISBN";
            txtMoTa.AccessibleName = "Mô tả sách";
            frm.ShowDialog();
        }

        private static void SetPictureImage(PictureBox pictureBox, Image? image)
        {
            Image? previousImage = pictureBox.Image;
            pictureBox.Image = image;
            if (!ReferenceEquals(previousImage, image))
                previousImage?.Dispose();
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

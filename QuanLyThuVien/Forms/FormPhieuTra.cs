using System.Data;
using QuanLyThuVien.Controls;
using QuanLyThuVien.Data;
using QuanLyThuVien.Helpers;
using QuanLyThuVien.Models;
using QuanLyThuVien.Pdf;

namespace QuanLyThuVien.Forms
{
    public class FormPhieuTra : UserControl
    {
        private DataGridView dgv = null!;
        private FilterBar filterBar = null!;
        private ModernComboBox overdueFilter = null!;
        private const decimal PhatMoiNgay = 10000m;

        public FormPhieuTra()
        {
            BackColor = AppColors.ContentBg;
            Padding = new Padding(10);
            Load += (_, _) => LoadData();
        }

        private void LoadData()
        {
            ResponsiveUi.DisposeChildren(this);
            dgv = new ModernDataGridView
            {
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
            dgv.Columns.Add("MaPhieuMuon", "Mã PM");
            dgv.Columns.Add("TenDocGia", "Độc giả");
            dgv.Columns.Add("NgayMuon", "Ngày mượn");
            dgv.Columns.Add("HanTra", "Hạn trả");
            dgv.Columns.Add("SoNgayQuaHan", "Quá hạn (ngày)");
            dgv.Columns.Add("TienPhat", "Tiền phạt gợi ý");
            int fineCol = dgv.Columns.Add("TienPhatValue", string.Empty);
            dgv.Columns[fineCol].Visible = false;
            dgv.Columns.Add("btnTra", "Thao tác");
            dgv.CellClick += Dgv_CellClick;
            var btnExport = PageHeader.CreatePrimaryAction("Xuất PDF", (_, _) => PdfExportService.ExportGrid(dgv, "Danh sách phiếu trả", "phieu_tra", FindForm() is IWin32Window owner ? owner : this), 105);
            var header = ResponsiveUi.AddListPage(this, dgv, "Trả sách", btnExport);
            filterBar = new FilterBar("Tìm mã phiếu hoặc tên độc giả...");
            overdueFilter = new ModernComboBox { Size = new Size(150, 36), DropDownStyle = ComboBoxStyle.DropDownList };
            overdueFilter.Items.AddRange(new object[] { "Tất cả phiếu", "Chỉ quá hạn" });
            overdueFilter.SelectedIndex = 0;
            filterBar.AddFilter(overdueFilter, "Lọc phiếu quá hạn");
            filterBar.FilterChanged += (_, _) => RefreshRows();
            ResponsiveUi.AddFilterBar(header, filterBar);
            RefreshRows();
        }

        private void RefreshRows()
        {
            if (dgv == null) return;
            dgv.Rows.Clear();
            try
            {
                bool overdueOnly = overdueFilter?.SelectedIndex == 1;
                var dt = DataAccess.GetPhieuTra(filterBar?.SearchBox.Text, overdueOnly);
                foreach (DataRow row in dt.Rows)
                {
                    DateTime hanTra = Convert.ToDateTime(row["HanTra"]);
                    int overdueDays = hanTra.Date < DateTime.Today ? (DateTime.Today - hanTra.Date).Days : 0;
                    int copies = Convert.ToInt32(row["SoLuongChuaTra"]);
                    decimal fine = overdueDays * PhatMoiNgay * copies;
                    dgv.Rows.Add(
                        row["MaPhieuMuon"],
                        row["TenDocGia"],
                        Convert.ToDateTime(row["NgayMuon"]).ToString("dd/MM/yyyy"),
                        hanTra.ToString("dd/MM/yyyy"),
                        overdueDays,
                        fine.ToString("N0") + "đ",
                        fine,
                        "Xử lý trả");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Không tải được danh sách trả sách: {ex}");
                MessageBox.Show("Không thể tải danh sách trả sách.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Dgv_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || dgv.Columns[e.ColumnIndex].Name != "btnTra") return;
            int maPM = Convert.ToInt32(dgv.Rows[e.RowIndex].Cells["MaPhieuMuon"].Value);
            string tenDG = dgv.Rows[e.RowIndex].Cells["TenDocGia"].Value?.ToString() ?? string.Empty;
            int overdueDays = Convert.ToInt32(dgv.Rows[e.RowIndex].Cells["SoNgayQuaHan"].Value);
            ShowTraForm(maPM, tenDG, overdueDays, PhatMoiNgay);
        }

        private void ShowTraForm(int maPM, string tenDG, int overdueDays, decimal suggestedRate)
        {
            var frm = ResponsiveUi.CreateDialog(
                $"Trả sách - PM #{maPM}",
                new Size(1100, 640),
                new Size(860, 560));

            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 58,
                Padding = new Padding(20, 10, 20, 8),
                BackColor = AppColors.ContentBg
            };
            header.Controls.Add(new Label
            {
                Text = $"Phiếu mượn #{maPM} · {tenDG}",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = AppColors.TextPrimary,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            });

            var footer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 150,
                BackColor = AppColors.HeaderBg,
                Padding = new Padding(20, 10, 20, 10)
            };
            var gridHost = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 0, 20, 12),
                BackColor = AppColors.ContentBg
            };

            var dgvCT = CreateReturnGrid();
            gridHost.Controls.Add(dgvCT);
            frm.Controls.Add(gridHost);
            frm.Controls.Add(footer);
            frm.Controls.Add(header);

            try
            {
                foreach (DataRow row in DataAccess.GetChiTietPhieuMuon(maPM).Rows)
                {
                    bool returned = row["NgayTra"] != DBNull.Value;
                    int quantity = Convert.ToInt32(row["SoLuong"]);
                    int lostQuantity = Convert.ToInt32(row["SoLuongMat"]);
                    decimal price = Convert.ToDecimal(row["GiaTien"]);
                    decimal fine = Convert.ToDecimal(row["TienPhat"]);
                    decimal compensation = Convert.ToDecimal(row["TienDenMatSach"]);
                    string status = returned
                        ? FormatReturnStatus(quantity, lostQuantity)
                        : "Chưa trả";

                    int index = dgvCT.Rows.Add(
                        false,
                        lostQuantity > 0,
                        lostQuantity,
                        row["MaSach"],
                        row["TenSach"],
                        quantity,
                        price,
                        fine,
                        compensation,
                        fine + compensation,
                        status);
                    DataGridViewRow gridRow = dgvCT.Rows[index];
                    gridRow.Tag = returned;
                    gridRow.Cells["Chon"].ReadOnly = returned;
                    gridRow.Cells["MatSach"].ReadOnly = returned;
                    gridRow.Cells["SoLuongMat"].ReadOnly = returned || lostQuantity == 0;
                    if (returned)
                    {
                        gridRow.DefaultCellStyle.ForeColor = AppColors.TextMuted;
                        gridRow.DefaultCellStyle.BackColor = AppColors.AlternateSurface;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Không tải được chi tiết trả sách: {ex}");
                MessageBox.Show(
                    "Không thể tải chi tiết phiếu. Hãy kiểm tra migration database mới nhất.",
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                frm.Close();
                return;
            }

            var lblRate = new Label
            {
                Text = "Mức phạt/ngày/cuốn (VNĐ)",
                Location = new Point(20, 15),
                AutoSize = true,
                ForeColor = AppColors.TextPrimary
            };
            var nudFine = new NumericUpDown
            {
                Location = new Point(220, 10),
                Size = new Size(180, 32),
                Minimum = 0,
                Maximum = 1000000000,
                DecimalPlaces = 0,
                ThousandsSeparator = true,
                Value = Math.Min(1000000000m, suggestedRate),
                AccessibleName = "Mức phạt mỗi ngày mỗi cuốn"
            };
            var lblFineTotal = CreateSummaryLabel("Phạt quá hạn: 0đ", new Point(20, 54));
            var lblCompensationTotal = CreateSummaryLabel("Tiền đền: 0đ", new Point(230, 54));
            var lblGrandTotal = CreateSummaryLabel("Tổng cần thu: 0đ", new Point(420, 54), true);
            var lblHint = new Label
            {
                Text = "Chọn “Mất sách” rồi nhập số lượng mất; phần còn lại sẽ được hoàn về kho.",
                Location = new Point(20, 96),
                AutoSize = true,
                ForeColor = AppColors.TextSecondary,
                Font = new Font("Segoe UI", 8.5F)
            };

            var btnCancel = new ModernButton
            {
                Text = "Hủy",
                Size = new Size(100, 40),
                Location = new Point(footer.Width - 270, 92),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                BaseColor = AppColors.TextSecondary,
                HoverColor = AppColors.TextMuted,
                BorderRadius = 12,
                DialogResult = DialogResult.Cancel,
                AccessibleName = "Hủy trả sách"
            };
            var btnTra = new ModernButton
            {
                Text = "Xác nhận trả",
                Size = new Size(150, 40),
                Location = new Point(footer.Width - 160, 92),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                BaseColor = AppColors.Primary,
                HoverColor = AppColors.PrimaryDark,
                BorderRadius = 12,
                AccessibleName = "Xác nhận trả sách"
            };
            var btnPrint = new ModernButton
            {
                Text = "Lưu xem trước",
                Size = new Size(100, 40),
                Location = new Point(footer.Width - 390, 92),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                BaseColor = AppColors.CardBg,
                HoverColor = AppColors.HoverSurface,
                PressedColor = AppColors.SelectedSurface,
                TextColor = AppColors.PrimaryDark,
                BorderRadius = 12,
                AccessibleName = "Lưu bản xem trước trả sách PDF"
            };
            btnPrint.Click += (_, _) => PdfExportService.ExportGrid(dgvCT, $"Bản xem trước trả sách #{maPM}", $"phieu_tra_du_kien_{maPM}", frm, "Bản xem trước - hóa đơn chính thức chỉ được tạo sau khi xác nhận trả sách");
            footer.Controls.AddRange(new Control[]
            {
                lblRate, nudFine, lblFineTotal, lblCompensationTotal, lblGrandTotal,
                lblHint, btnPrint, btnCancel, btnTra
            });

            bool updatingGrid = false;

            bool IsReturned(DataGridViewRow row) => row.Tag is bool value && value;

            bool TryGetLostQuantity(DataGridViewRow row, bool showMessage, out int lostQuantity)
            {
                lostQuantity = 0;
                bool isLost = Convert.ToBoolean(row.Cells["MatSach"].Value ?? false);
                if (!isLost) return true;

                int quantity = Convert.ToInt32(row.Cells["SoLuong"].Value);
                if (!int.TryParse(row.Cells["SoLuongMat"].Value?.ToString(), out lostQuantity)
                    || lostQuantity < 1
                    || lostQuantity > quantity)
                {
                    if (showMessage)
                    {
                        MessageBox.Show(
                            $"Số lượng mất phải từ 1 đến {quantity}.",
                            "Dữ liệu chưa hợp lệ",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                    }
                    return false;
                }
                return true;
            }

            void UpdatePreview()
            {
                if (updatingGrid) return;
                updatingGrid = true;
                try
                {
                    decimal totalFine = 0;
                    decimal totalCompensation = 0;
                    foreach (DataGridViewRow row in dgvCT.Rows)
                    {
                        if (IsReturned(row)) continue;
                        bool selected = Convert.ToBoolean(row.Cells["Chon"].Value ?? false);
                        if (!selected)
                        {
                            row.Cells["TienPhat"].Value = 0m;
                            row.Cells["TienDen"].Value = 0m;
                            row.Cells["TongThu"].Value = 0m;
                            continue;
                        }

                        int quantity = Convert.ToInt32(row.Cells["SoLuong"].Value);
                        decimal price = Convert.ToDecimal(row.Cells["GiaTien"].Value);
                        int lostQuantity = TryGetLostQuantity(row, false, out int parsedLost) ? parsedLost : 0;
                        decimal fine = overdueDays * nudFine.Value * quantity;
                        decimal compensation = price * lostQuantity;
                        row.Cells["TienPhat"].Value = fine;
                        row.Cells["TienDen"].Value = compensation;
                        row.Cells["TongThu"].Value = fine + compensation;
                        totalFine += fine;
                        totalCompensation += compensation;
                    }

                    lblFineTotal.Text = $"Phạt quá hạn: {totalFine:N0}đ";
                    lblCompensationTotal.Text = $"Tiền đền: {totalCompensation:N0}đ";
                    lblGrandTotal.Text = $"Tổng cần thu: {totalFine + totalCompensation:N0}đ";
                }
                finally
                {
                    updatingGrid = false;
                }
            }

            dgvCT.CurrentCellDirtyStateChanged += (_, _) =>
            {
                if (dgvCT.IsCurrentCellDirty)
                    dgvCT.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            dgvCT.CellBeginEdit += (_, e) =>
            {
                if (e.RowIndex < 0) return;
                DataGridViewRow row = dgvCT.Rows[e.RowIndex];
                if (IsReturned(row)) { e.Cancel = true; return; }
                if (dgvCT.Columns[e.ColumnIndex].Name == "SoLuongMat"
                    && !Convert.ToBoolean(row.Cells["MatSach"].Value ?? false))
                    e.Cancel = true;
            };
            dgvCT.CellValueChanged += (_, e) =>
            {
                if (updatingGrid || e.RowIndex < 0 || e.ColumnIndex < 0) return;
                DataGridViewRow row = dgvCT.Rows[e.RowIndex];
                if (IsReturned(row)) return;
                string columnName = dgvCT.Columns[e.ColumnIndex].Name;
                updatingGrid = true;
                try
                {
                    if (columnName == "Chon")
                    {
                        bool selected = Convert.ToBoolean(row.Cells["Chon"].Value ?? false);
                        if (!selected)
                        {
                            row.Cells["MatSach"].Value = false;
                            row.Cells["SoLuongMat"].Value = 0;
                            row.Cells["SoLuongMat"].ReadOnly = true;
                        }
                    }
                    else if (columnName == "MatSach")
                    {
                        bool lost = Convert.ToBoolean(row.Cells["MatSach"].Value ?? false);
                        row.Cells["Chon"].Value = true;
                        row.Cells["SoLuongMat"].ReadOnly = !lost;
                        row.Cells["SoLuongMat"].Value = lost
                            ? Math.Max(1, int.TryParse(row.Cells["SoLuongMat"].Value?.ToString(), out int current) ? current : 0)
                            : 0;
                    }
                }
                finally
                {
                    updatingGrid = false;
                }
                UpdatePreview();
            };
            dgvCT.CellValidating += (_, e) =>
            {
                if (e.RowIndex < 0 || dgvCT.Columns[e.ColumnIndex].Name != "SoLuongMat") return;
                DataGridViewRow row = dgvCT.Rows[e.RowIndex];
                if (IsReturned(row) || !Convert.ToBoolean(row.Cells["MatSach"].Value ?? false)) return;
                int quantity = Convert.ToInt32(row.Cells["SoLuong"].Value);
                if (!int.TryParse(e.FormattedValue?.ToString(), out int lostQuantity)
                    || lostQuantity < 1
                    || lostQuantity > quantity)
                {
                    e.Cancel = true;
                    row.ErrorText = $"Số lượng mất phải từ 1 đến {quantity}.";
                }
                else
                {
                    row.ErrorText = string.Empty;
                }
            };
            dgvCT.CellEndEdit += (_, e) =>
            {
                if (e.RowIndex >= 0) dgvCT.Rows[e.RowIndex].ErrorText = string.Empty;
                UpdatePreview();
            };
            dgvCT.DataError += (_, e) =>
            {
                e.ThrowException = false;
                e.Cancel = true;
            };
            nudFine.ValueChanged += (_, _) => UpdatePreview();

            btnCancel.Click += (_, _) => frm.Close();
            btnTra.Click += (_, _) =>
            {
                if (!dgvCT.EndEdit()) return;
                var requests = new List<ReturnBookRequest>();
                foreach (DataGridViewRow row in dgvCT.Rows)
                {
                    if (IsReturned(row) || !Convert.ToBoolean(row.Cells["Chon"].Value ?? false)) continue;
                    if (!TryGetLostQuantity(row, true, out int lostQuantity)) return;
                    requests.Add(new ReturnBookRequest(
                        Convert.ToInt32(row.Cells["MaSach"].Value),
                        lostQuantity));
                }

                if (requests.Count == 0)
                {
                    MessageBox.Show(
                        "Chọn ít nhất một dòng sách chưa trả.",
                        "Chưa chọn sách",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                try
                {
                    if (!DataAccess.ReturnSelectedBooks(maPM, requests, nudFine.Value, out string? reason))
                    {
                        MessageBox.Show(
                            reason ?? "Không thể xác nhận trả sách.",
                            "Không thể trả sách",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        return;
                    }
                    PdfExportService.ExportReturnInvoice(maPM, frm);
                    frm.Close();
                    LoadData();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Xác nhận trả sách thất bại: {ex}");
                    MessageBox.Show(
                        "Không thể xác nhận trả sách. Vui lòng thử lại.",
                        "Lỗi",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            };

            UpdatePreview();
            frm.AcceptButton = btnTra;
            frm.CancelButton = btnCancel;
            frm.ActiveControl = dgvCT;
            dgvCT.AccessibleName = "Danh sách sách đang mượn";
            frm.ShowDialog();
        }

        private static ModernDataGridView CreateReturnGrid()
        {
            var grid = new ModernDataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = AppColors.CardBg,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                ReadOnly = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                EditMode = DataGridViewEditMode.EditOnEnter,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = AppColors.Primary,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold)
                },
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 38,
                DefaultCellStyle = new DataGridViewCellStyle { Font = new Font("Segoe UI", 9.5F) },
                ScrollBars = ScrollBars.Both
            };

            grid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Chon", HeaderText = "Chọn trả", Width = 72 });
            grid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "MatSach", HeaderText = "Mất sách", Width = 78 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "SoLuongMat", HeaderText = "SL mất", Width = 70, ValueType = typeof(int) });
            AddReadOnlyColumn(grid, "MaSach", "Mã sách", 72);
            AddReadOnlyColumn(grid, "TenSach", "Tên sách", 220);
            AddReadOnlyColumn(grid, "SoLuong", "SL mượn", 75);
            AddMoneyColumn(grid, "GiaTien", "Giá sách", 110);
            AddMoneyColumn(grid, "TienPhat", "Phạt quá hạn", 120);
            AddMoneyColumn(grid, "TienDen", "Tiền đền", 110);
            AddMoneyColumn(grid, "TongThu", "Tổng thu", 120);
            AddReadOnlyColumn(grid, "TrangThai", "Kết quả", 130);
            return grid;
        }

        private static void AddReadOnlyColumn(DataGridView grid, string name, string header, int width)
        {
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = header,
                Width = width,
                ReadOnly = true
            });
        }

        private static void AddMoneyColumn(DataGridView grid, string name, string header, int width)
        {
            var column = new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = header,
                Width = width,
                ReadOnly = true,
                ValueType = typeof(decimal)
            };
            column.DefaultCellStyle.Format = "N0";
            column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            grid.Columns.Add(column);
        }

        private static Label CreateSummaryLabel(string text, Point location, bool emphasized = false)
        {
            return new Label
            {
                Text = text,
                Location = location,
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F, emphasized ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = emphasized ? AppColors.PrimaryDark : AppColors.TextPrimary
            };
        }

        private static string FormatReturnStatus(int quantity, int lostQuantity)
        {
            if (lostQuantity <= 0) return "Đã trả";
            if (lostQuantity >= quantity) return $"Mất {quantity} cuốn";
            return $"Trả {quantity - lostQuantity} / Mất {lostQuantity}";
        }
    }
}

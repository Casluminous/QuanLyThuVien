using System.Data;
using QuanLyThuVien.Controls;
using QuanLyThuVien.Data;
using QuanLyThuVien.Helpers;
using QuanLyThuVien.Models;
using QuanLyThuVien.Pdf;

namespace QuanLyThuVien.Forms
{
    public class FormPhieuMuon : UserControl
    {
        private DataGridView dgv = null!;
        private FilterBar filterBar = null!;
        private ModernComboBox statusFilter = null!;
        private ContextMenuStrip? _actionsMenu;

        public FormPhieuMuon()
        {
            BackColor = AppColors.ContentBg;
            Padding = new Padding(10);
            Load += (s, e) => LoadData();
        }

        private void LoadData()
        {
            ResponsiveUi.DisposeChildren(this);
            var btnThem = PageHeader.CreatePrimaryAction("+ Tạo phiếu mượn", (_, _) => ShowInputDialog(), 170);
            var btnExport = PageHeader.CreatePrimaryAction("Xuất PDF", (_, _) => PdfExportService.ExportGrid(dgv, "Danh sách phiếu mượn", "phieu_muon", FindForm() is IWin32Window owner ? owner : this), 105);

            dgv = new ModernDataGridView
            {
                Location = new Point(10, 105), Size = new Size(Math.Max(500, Width - 30), Math.Max(200, Height - 120)),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                BackgroundColor = AppColors.CardBg, BorderStyle = BorderStyle.None, GridColor = AppColors.Border,
                RowHeadersVisible = false, AllowUserToAddRows = false, ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = AppColors.Primary, ForeColor = Color.White, Font = new Font("Segoe UI", 10F, FontStyle.Bold) },
                EnableHeadersVisualStyles = false, ColumnHeadersHeight = 40, DefaultCellStyle = new DataGridViewCellStyle { Font = new Font("Segoe UI", 10F) }
            };
            dgv.Columns.Add("MaPhieuMuon", "Mã PM");
            dgv.Columns.Add("TenDocGia", "Độc giả");
            dgv.Columns.Add("TenNhanVien", "Nhân viên");
            dgv.Columns.Add("NgayMuon", "Ngày mượn");
            dgv.Columns.Add("HanTra", "Hạn trả");
            dgv.Columns.Add("TrangThai", "Trạng thái");
            int actionColumnIndex = dgv.Columns.Add("btnThaoTac", "Thao tác");
            dgv.Columns[actionColumnIndex].Width = 116;
            dgv.Columns[actionColumnIndex].MinimumWidth = 108;
            dgv.CellClick += Dgv_CellClick;
            dgv.KeyDown += Dgv_KeyDown;
            var header = ResponsiveUi.AddListPage(this, dgv, "Quản lý Phiếu mượn", btnThem, btnExport);

            filterBar = new FilterBar("Tìm mã phiếu hoặc tên độc giả...");
            statusFilter = new ModernComboBox { Size = new Size(170, 36), DropDownStyle = ComboBoxStyle.DropDownList };
            statusFilter.Items.AddRange(new object[] { "Tất cả", "Đang mượn", "Đã trả một phần", "Đã trả", "Quá hạn" });
            statusFilter.SelectedIndex = 0;
            filterBar.AddFilter(statusFilter, "Lọc trạng thái phiếu mượn");
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
                string selectedStatus = statusFilter?.SelectedItem?.ToString() ?? "Tất cả";
                bool overdueOnly = selectedStatus == "Quá hạn";
                string? status = overdueOnly || selectedStatus == "Tất cả" ? null : selectedStatus;
                var loans = DataAccess.GetAllPhieuMuon(filterBar?.SearchBox.Text, status, overdueOnly: overdueOnly).AsEnumerable()
                    .Select(row =>
                    {
                        DateTime hanTra = Convert.ToDateTime(row["HanTra"]);
                        DateTime ngayMuon = Convert.ToDateTime(row["NgayMuon"]);
                        string rawStatus = row["TrangThai"].ToString() ?? "";
                        int unreturned = Convert.ToInt32(row["SoDongChuaTra"]);
                        int returned = Convert.ToInt32(row["SoDongDaTra"]);
                        bool overdue = unreturned > 0 && hanTra.Date < DateTime.Today;
                        string status = rawStatus;
                        if (unreturned > 0 && returned > 0) status = "Đã trả một phần";
                        else if (overdue) status = "Quá hạn";
                        else if (unreturned == 0 && returned > 0) status = "Đã trả";

                        int priority = unreturned > 0 && returned == 0 && overdue ? 0
                            : unreturned > 0 && returned == 0 ? 1
                            : unreturned > 0 && returned > 0 ? 2
                            : 3;
                        return new { Row = row, HanTra = hanTra, NgayMuon = ngayMuon, Unreturned = unreturned, Returned = returned, Status = status, Priority = priority };
                    })
                    .OrderBy(item => item.Priority)
                    .ThenBy(item => item.HanTra)
                    .ThenByDescending(item => item.NgayMuon)
                    .ThenByDescending(item => Convert.ToInt32(item.Row["MaPhieuMuon"]))
                    .ToList();

                foreach (var item in loans)
                {
                    DataRow row = item.Row;
                    int index = dgv.Rows.Add(row["MaPhieuMuon"], row["TenDocGia"], row["TenNhanVien"], item.NgayMuon.ToString("dd/MM/yyyy"), item.HanTra.ToString("dd/MM/yyyy"), item.Status, "Thao tác");
                    dgv.Rows[index].Tag = new LoanRowState(item.Unreturned, item.Returned);
                    dgv.Rows[index].Cells["btnThaoTac"].ToolTipText = "Mở các thao tác cho phiếu mượn này";
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Không tải được danh sách phiếu mượn: {ex}"); MessageBox.Show("Không thể tải danh sách phiếu mượn.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void Dgv_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (dgv.Columns[e.ColumnIndex].Name == "btnThaoTac")
                ShowActionsMenu(e.RowIndex, e.ColumnIndex);
        }

        private void Dgv_KeyDown(object? sender, KeyEventArgs e)
        {
            DataGridViewCell? currentCell = dgv.CurrentCell;
            if (e.KeyCode is not (Keys.Enter or Keys.Space)
                || currentCell?.OwningColumn?.Name != "btnThaoTac") return;

            e.Handled = true;
            e.SuppressKeyPress = true;
            ShowActionsMenu(currentCell.RowIndex, currentCell.ColumnIndex);
        }

        private void ShowActionsMenu(int rowIndex, int columnIndex)
        {
            if (rowIndex < 0 || rowIndex >= dgv.Rows.Count) return;
            DataGridViewRow row = dgv.Rows[rowIndex];
            if (row.Tag is not LoanRowState state) return;

            int maPM = Convert.ToInt32(row.Cells["MaPhieuMuon"].Value);
            if (_actionsMenu != null)
            {
                if (!_actionsMenu.IsDisposed)
                    _actionsMenu.Close();
                if (!_actionsMenu.IsDisposed)
                    _actionsMenu.Dispose();
                _actionsMenu = null;
            }

            var menu = new ContextMenuStrip
            {
                ShowImageMargin = false,
                BackColor = AppColors.CardBg,
                ForeColor = AppColors.TextPrimary,
                Font = new Font("Segoe UI", 9.5F),
                AccessibleName = $"Thao tác phiếu mượn {maPM}"
            };

            ToolStripMenuItem CreateAction(string text, bool enabled, Action action)
            {
                var item = new ToolStripMenuItem(text)
                {
                    Enabled = enabled,
                    AccessibleName = text,
                    AutoSize = false,
                    Size = new Size(190, 34)
                };
                item.Click += (_, _) => action();
                return item;
            }

            menu.Items.Add(CreateAction("Xem chi tiết", true, () => ShowChiTiet(maPM)));
            menu.Items.Add(CreateAction("Sửa phiếu", state.Unreturned > 0 && state.Returned == 0, () => ShowInputDialog(maPM)));
            menu.Items.Add(CreateAction("Gia hạn", state.Unreturned > 0, () => ShowRenewDialog(maPM)));
            menu.Items.Add(CreateAction("Thiết lập trạng thái", Session.IsAdmin, () => ShowStatusSetupDialog(maPM)));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(CreateAction("Thu tiền", true, () => ShowPaymentDialog(maPM)));
            menu.Items.Add(CreateAction("Sửa tiền phạt", Session.IsAdmin && state.Unreturned == 0 && state.Returned > 0, () => ShowPenaltyEditDialog(maPM)));
            _actionsMenu = menu;
            menu.Closed += (_, _) => DeferActionsMenuCleanup(menu);

            Rectangle cellBounds = dgv.GetCellDisplayRectangle(columnIndex, rowIndex, true);
            menu.Show(dgv, new Point(cellBounds.Left, cellBounds.Bottom));
        }

        private void DeferActionsMenuCleanup(ContextMenuStrip menu)
        {
            if (ReferenceEquals(_actionsMenu, menu))
                _actionsMenu = null;

            void Cleanup()
            {
                if (!menu.IsDisposed)
                    menu.Dispose();
                if (dgv != null && !dgv.IsDisposed && dgv.IsHandleCreated)
                    dgv.Focus();
            }

            if (IsDisposed || Disposing || !IsHandleCreated)
            {
                Cleanup();
                return;
            }

            try
            {
                BeginInvoke((Action)Cleanup);
            }
            catch (InvalidOperationException)
            {
                Cleanup();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _actionsMenu != null)
            {
                if (!_actionsMenu.IsDisposed)
                    _actionsMenu.Close();
                if (!_actionsMenu.IsDisposed)
                    _actionsMenu.Dispose();
                _actionsMenu = null;
            }

            base.Dispose(disposing);
        }

        private void ShowStatusSetupDialog(int maPM)
        {
            if (!Session.IsAdmin) return;
            var data = DataAccess.GetPhieuMuonById(maPM);
            if (data.Rows.Count == 0)
            {
                MessageBox.Show("Không tìm thấy phiếu mượn.", "Thiết lập trạng thái", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DataRow row = data.Rows[0];
            DateTime dueDate = Convert.ToDateTime(row["HanTra"]).Date;
            int unreturned = Convert.ToInt32(row["SoDongChuaTra"]);
            int returned = Convert.ToInt32(row["SoDongDaTra"]);
            string storedStatus = row["TrangThai"]?.ToString() ?? string.Empty;
            string expectedStatus = GetHeaderStatus(unreturned, returned);
            string displayStatus = unreturned > 0 && dueDate < DateTime.Today ? "Quá hạn" : expectedStatus;

            var frm = ResponsiveUi.CreateDialog($"Thiết lập trạng thái · Phiếu #{maPM}", new Size(620, 420), new Size(540, 360));
            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 20, 24, 12),
                ColumnCount = 2,
                AutoSize = false,
                BackColor = AppColors.ContentBg
            };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < 7; i++) body.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

            void AddSummaryRow(string label, string value, int rowIndex, Color? valueColor = null)
            {
                body.Controls.Add(new Label
                {
                    Text = label,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                    ForeColor = AppColors.TextSecondary
                }, 0, rowIndex);
                body.Controls.Add(new Label
                {
                    Text = value,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = new Font("Segoe UI", 10F, rowIndex is 4 or 5 ? FontStyle.Bold : FontStyle.Regular),
                    ForeColor = valueColor ?? AppColors.TextPrimary,
                    AutoEllipsis = true
                }, 1, rowIndex);
            }

            AddSummaryRow("Độc giả", row["TenDocGia"]?.ToString() ?? "—", 0);
            AddSummaryRow("Ngày mượn", Convert.ToDateTime(row["NgayMuon"]).ToString("dd/MM/yyyy"), 1);
            AddSummaryRow("Hạn trả", dueDate.ToString("dd/MM/yyyy"), 2);
            AddSummaryRow("Trạng thái đang lưu", storedStatus, 3, AppColors.PrimaryDark);
            AddSummaryRow("Trạng thái suy ra", displayStatus, 4, displayStatus == "Quá hạn" ? AppColors.Warning : AppColors.Primary);
            AddSummaryRow("Chi tiết sách", $"{returned} dòng đã trả · {unreturned} dòng chưa trả", 5);
            AddSummaryRow("Tiền phải thu", $"{GetLoanDueTotal(maPM):N0}đ", 6, AppColors.TextPrimary);

            var footer = ResponsiveUi.AddDialogFooter(frm, 68);
            var close = new ModernButton
            {
                Text = "Đóng",
                Size = new Size(90, 38),
                BaseColor = AppColors.TextSecondary,
                HoverColor = AppColors.TextMuted,
                BorderRadius = 10,
                DialogResult = DialogResult.Cancel,
                AccessibleName = "Đóng thiết lập trạng thái"
            };
            var demo = new ModernButton
            {
                Text = "Tạo kịch bản demo",
                Size = new Size(150, 38),
                BaseColor = AppColors.Accent,
                HoverColor = AppColors.Warning,
                BorderRadius = 10,
                AccessibleName = "Tạo kịch bản dữ liệu demo"
            };
            var sync = new ModernButton
            {
                Text = "Đồng bộ trạng thái",
                Size = new Size(155, 38),
                BaseColor = AppColors.Primary,
                HoverColor = AppColors.PrimaryDark,
                BorderRadius = 10,
                AccessibleName = "Đồng bộ trạng thái phiếu mượn"
            };
            sync.Click += (_, _) =>
            {
                try
                {
                    if (!DataAccess.SyncPhieuMuonStatus(maPM, Session.CurrentUser!.MaNV, out string? reason))
                    {
                        MessageBox.Show(reason ?? "Không thể đồng bộ trạng thái.", "Không thể thực hiện", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    MessageBox.Show("Trạng thái phiếu đã được đồng bộ theo chi tiết sách.", "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    frm.Close();
                    LoadData();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Đồng bộ trạng thái thất bại: {ex}");
                    MessageBox.Show("Không thể đồng bộ trạng thái. Vui lòng thử lại.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            demo.Click += (_, _) => ShowDemoScenarioDialog();
            footer.Controls.Add(close);
            footer.Controls.Add(demo);
            footer.Controls.Add(sync);
            frm.Controls.Add(body);
            frm.AcceptButton = sync;
            frm.CancelButton = close;
            frm.ActiveControl = sync;
            frm.ShowDialog();
        }

        private void ShowDemoScenarioDialog()
        {
            if (!Session.IsAdmin) return;
            var frm = ResponsiveUi.CreateDialog("Tạo kịch bản demo mượn–trả", new Size(860, 650), new Size(760, 560));

            var readerLabel = new Label { Text = "Độc giả:", Location = new Point(24, 22), AutoSize = true };
            var readerBox = new ModernComboBox { Location = new Point(120, 17), Size = new Size(330, 36), DropDownStyle = ComboBoxStyle.DropDownList, AccessibleName = "Độc giả cho dữ liệu demo" };
            foreach (DataRow reader in DataAccess.GetBorrowEligibleReaders().Rows)
                readerBox.Items.Add(new DemoComboItem(reader["HoTen"]?.ToString() ?? "—", Convert.ToInt32(reader["MaDG"])));
            if (readerBox.Items.Count > 0) readerBox.SelectedIndex = 0;

            var scenarioLabel = new Label { Text = "Kịch bản:", Location = new Point(480, 22), AutoSize = true };
            var scenarioBox = new ModernComboBox { Location = new Point(560, 17), Size = new Size(270, 36), DropDownStyle = ComboBoxStyle.DropDownList, AccessibleName = "Kịch bản trạng thái demo" };
            scenarioBox.Items.AddRange(new object[]
            {
                new DemoScenarioItem("Đang mượn", DemoLoanScenario.DangMuon),
                new DemoScenarioItem("Quá hạn", DemoLoanScenario.QuaHan),
                new DemoScenarioItem("Đã trả một phần", DemoLoanScenario.DaTraMotPhan),
                new DemoScenarioItem("Đã trả", DemoLoanScenario.DaTra),
                new DemoScenarioItem("Có sách mất", DemoLoanScenario.CoSachMat)
            });
            scenarioBox.SelectedIndex = 0;

            var borrowLabel = new Label { Text = "Ngày mượn:", Location = new Point(24, 72), AutoSize = true };
            var borrowPicker = new DateTimePicker { Location = new Point(120, 67), Size = new Size(150, 32), Format = DateTimePickerFormat.Short, MaxDate = DateTime.Today, Value = DateTime.Today };
            var dueLabel = new Label { Text = "Hạn trả:", Location = new Point(300, 72), AutoSize = true };
            var duePicker = new DateTimePicker { Location = new Point(365, 67), Size = new Size(150, 32), Format = DateTimePickerFormat.Short, MinDate = DateTime.Today, Value = DateTime.Today.AddDays(14) };
            borrowPicker.ValueChanged += (_, _) => { duePicker.MinDate = borrowPicker.Value.Date; if (duePicker.Value.Date < borrowPicker.Value.Date) duePicker.Value = borrowPicker.Value.Date; };

            var lostLabel = new Label { Text = "SL mất (dòng đầu):", Location = new Point(545, 72), AutoSize = true, Visible = false };
            var lostBox = new NumericUpDown { Location = new Point(690, 67), Size = new Size(110, 32), Minimum = 1, Maximum = 1000, Value = 1, Visible = false, ThousandsSeparator = true, AccessibleName = "Số lượng mất trong kịch bản demo" };

            var grid = new ModernDataGridView
            {
                Location = new Point(24, 125),
                Size = new Size(806, 420),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackgroundColor = AppColors.CardBg,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AccessibleName = "Danh sách sách cho kịch bản demo"
            };
            grid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Chon", HeaderText = "Chọn", FillWeight = 12 });
            var idCol = grid.Columns.Add("MaSach", "Mã"); grid.Columns[idCol].ReadOnly = true;
            var nameCol = grid.Columns.Add("TenSach", "Tên sách"); grid.Columns[nameCol].ReadOnly = true;
            var stockCol = grid.Columns.Add("TonKho", "Tồn kho"); grid.Columns[stockCol].ReadOnly = true;
            grid.Columns.Add("SoLuong", "SL mượn");
            var availableBooks = DataAccess.GetSachAvailable();
            foreach (DataRow book in availableBooks.Rows)
            {
                int index = grid.Rows.Add(false, book["MaSach"], book["TenSach"], book["SoLuong"], 1);
                grid.Rows[index].Cells["SoLuong"].Value = 1;
            }
            grid.CurrentCellDirtyStateChanged += (_, _) => { if (grid.IsCurrentCellDirty) grid.CommitEdit(DataGridViewDataErrorContexts.Commit); };

            void UpdateScenarioFields()
            {
                var scenario = (scenarioBox.SelectedItem as DemoScenarioItem)?.Value ?? DemoLoanScenario.DangMuon;
                bool overdue = scenario == DemoLoanScenario.QuaHan;
                bool lost = scenario == DemoLoanScenario.CoSachMat;
                lostLabel.Visible = lost;
                lostBox.Visible = lost;
                if (overdue)
                {
                    DateTime due = DateTime.Today.AddDays(-3);
                    duePicker.MinDate = borrowPicker.Value.Date;
                    if (borrowPicker.Value.Date > due) borrowPicker.Value = due.AddDays(-14);
                    duePicker.Value = due;
                }
                else
                {
                    duePicker.MinDate = borrowPicker.Value.Date;
                    if (duePicker.Value.Date < DateTime.Today) duePicker.Value = DateTime.Today.AddDays(14);
                }
            }
            scenarioBox.SelectedIndexChanged += (_, _) => UpdateScenarioFields();
            UpdateScenarioFields();

            var footer = ResponsiveUi.AddDialogFooter(frm, 68);
            var cancel = new ModernButton { Text = "Hủy", Size = new Size(90, 38), BaseColor = AppColors.TextSecondary, HoverColor = AppColors.TextMuted, BorderRadius = 10, DialogResult = DialogResult.Cancel, AccessibleName = "Hủy tạo kịch bản demo" };
            var create = new ModernButton { Text = "Tạo dữ liệu demo", Size = new Size(150, 38), BaseColor = AppColors.Primary, HoverColor = AppColors.PrimaryDark, BorderRadius = 10, AccessibleName = "Tạo dữ liệu demo" };
            create.Click += (_, _) =>
            {
                if (readerBox.SelectedItem is not DemoComboItem reader)
                {
                    MessageBox.Show("Chọn độc giả trước khi tạo.", "Thiếu dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                grid.EndEdit();
                var selected = new List<(int MaSach, int SoLuong)>();
                foreach (DataGridViewRow gridRow in grid.Rows)
                {
                    bool checkedRow = Convert.ToBoolean(gridRow.Cells["Chon"].Value ?? false);
                    if (!checkedRow) continue;
                    if (!int.TryParse(gridRow.Cells["SoLuong"].Value?.ToString(), out int quantity) || quantity <= 0)
                    {
                        MessageBox.Show("Số lượng mượn phải lớn hơn 0.", "Dữ liệu chưa hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    int available = Convert.ToInt32(gridRow.Cells["TonKho"].Value);
                    if (quantity > available)
                    {
                        MessageBox.Show("Số lượng mượn vượt quá tồn kho.", "Dữ liệu chưa hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    selected.Add((Convert.ToInt32(gridRow.Cells["MaSach"].Value), quantity));
                }

                var scenario = (scenarioBox.SelectedItem as DemoScenarioItem)?.Value ?? DemoLoanScenario.DangMuon;
                if (selected.Count == 0)
                {
                    MessageBox.Show("Chọn ít nhất một đầu sách.", "Thiếu dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (scenario == DemoLoanScenario.DaTraMotPhan && selected.Count < 2)
                {
                    MessageBox.Show("Kịch bản trả một phần cần ít nhất hai đầu sách.", "Thiếu dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int lostQuantity = scenario == DemoLoanScenario.CoSachMat ? Convert.ToInt32(lostBox.Value) : 0;
                if (scenario == DemoLoanScenario.CoSachMat && lostQuantity > selected[0].SoLuong)
                {
                    MessageBox.Show("Số lượng mất không được vượt quá số lượng mượn ở dòng đầu tiên.", "Dữ liệu chưa hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    var request = new DemoLoanScenarioRequest(reader.Value, borrowPicker.Value.Date, duePicker.Value.Date, selected, scenario, lostQuantity);
                    if (!DataAccess.CreateDemoLoanScenario(request, Session.CurrentUser!.MaNV, out int maPM, out string? reason))
                    {
                        MessageBox.Show(reason ?? "Không thể tạo kịch bản demo.", "Không thể thực hiện", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    MessageBox.Show($"Đã tạo phiếu demo #{maPM}.", "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    frm.Close();
                    LoadData();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Tạo kịch bản demo thất bại: {ex}");
                    MessageBox.Show("Không thể tạo kịch bản demo. Vui lòng thử lại.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            footer.Controls.Add(cancel);
            footer.Controls.Add(create);
            frm.Controls.Add(grid);
            frm.Controls.Add(readerLabel);
            frm.Controls.Add(readerBox);
            frm.Controls.Add(scenarioLabel);
            frm.Controls.Add(scenarioBox);
            frm.Controls.Add(borrowLabel);
            frm.Controls.Add(borrowPicker);
            frm.Controls.Add(dueLabel);
            frm.Controls.Add(duePicker);
            frm.Controls.Add(lostLabel);
            frm.Controls.Add(lostBox);
            frm.AcceptButton = create;
            frm.CancelButton = cancel;
            frm.ActiveControl = readerBox;
            frm.ShowDialog();
        }

        private static string GetHeaderStatus(int unreturned, int returned)
        {
            if (unreturned == 0 && returned > 0) return "Đã trả";
            if (unreturned > 0 && returned > 0) return "Đã trả một phần";
            return "Đang mượn";
        }

        private static decimal GetLoanDueTotal(int maPM)
        {
            try
            {
                var summary = DataAccess.GetLoanPaymentSummary(maPM);
                return summary.Rows.Count == 0 ? 0m : Convert.ToDecimal(summary.Rows[0]["TongPhaiThu"]);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Không tải được tổng tiền phiếu {maPM}: {ex}");
                return 0m;
            }
        }

        private void ShowRenewDialog(int maPM)
        {
            var data = DataAccess.GetPhieuMuonById(maPM);
            if (data.Rows.Count == 0) return;
            DataRow row = data.Rows[0];
            string status = row["TrangThai"].ToString() ?? string.Empty;
            if (status == "Đã trả")
            {
                MessageBox.Show("Phiếu đã trả hết nên không thể gia hạn.", "Không thể gia hạn", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            DateTime currentDue = Convert.ToDateTime(row["HanTra"]).Date;
            var frm = ResponsiveUi.CreateDialog($"Gia hạn phiếu mượn #{maPM}", new Size(430, 190), new Size(390, 180));
            var label = new Label { Text = $"Hạn hiện tại: {currentDue:dd/MM/yyyy}", Location = new Point(24, 24), AutoSize = true, ForeColor = AppColors.TextSecondary };
            var dateLabel = new Label { Text = "Hạn mới:", Location = new Point(24, 70), AutoSize = true };
            var picker = new DateTimePicker { Location = new Point(130, 66), Size = new Size(240, 30), Format = DateTimePickerFormat.Short, MinDate = currentDue, Value = currentDue.AddDays(14), AccessibleName = "Hạn trả mới" };
            var footer = ResponsiveUi.AddDialogFooter(frm, 58);
            var cancel = new ModernButton { Text = "Hủy", Size = new Size(82, 36), BaseColor = AppColors.TextSecondary, BorderRadius = 10, DialogResult = DialogResult.Cancel, AccessibleName = "Hủy gia hạn" };
            var save = new ModernButton { Text = "Lưu", Size = new Size(82, 36), BaseColor = AppColors.Primary, BorderRadius = 10, AccessibleName = "Lưu gia hạn" };
            save.Click += (_, _) =>
            {
                try
                {
                    if (!DataAccess.ExtendPhieuMuon(maPM, picker.Value.Date, out string? reason))
                    {
                        MessageBox.Show(reason ?? "Không thể gia hạn phiếu.", "Không thể gia hạn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    frm.Close();
                    RefreshRows();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Gia hạn phiếu thất bại: {ex}");
                    MessageBox.Show("Không thể gia hạn phiếu. Vui lòng thử lại.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            footer.Controls.Add(cancel);
            footer.Controls.Add(save);
            frm.Controls.Add(label);
            frm.Controls.Add(dateLabel);
            frm.Controls.Add(picker);
            frm.AcceptButton = save;
            frm.CancelButton = cancel;
            frm.ActiveControl = picker;
            frm.ShowDialog();
        }

        private void ShowPaymentDialog(int maPM)
        {
            DataTable summary;
            try
            {
                summary = DataAccess.GetLoanPaymentSummary(maPM);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Không tải được thông tin thanh toán: {ex}");
                MessageBox.Show("Chưa có bảng ThanhToanPhat. Hãy chạy migration_2026-07-21-003.sql rồi thử lại.", "Thiếu cấu hình dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (summary.Rows.Count == 0)
            {
                MessageBox.Show("Không tìm thấy phiếu mượn.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            DataRow summaryRow = summary.Rows[0];
            decimal totalDue = Convert.ToDecimal(summaryRow["TongPhaiThu"]);
            decimal paid = Convert.ToDecimal(summaryRow["DaThu"]);
            decimal remaining = Math.Max(0, totalDue - paid);
            var frm = ResponsiveUi.CreateDialog($"Thu tiền · Phiếu mượn #{maPM}", new Size(620, 460), new Size(540, 400));
            var summaryLabel = new Label { Text = $"Phải thu: {totalDue:N0}đ   ·   Đã thu: {paid:N0}đ   ·   Còn lại: {remaining:N0}đ", Dock = DockStyle.Top, Height = 42, Padding = new Padding(18, 12, 18, 0), Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = remaining > 0 ? AppColors.Warning : AppColors.Success };
            var history = new ModernDataGridView { Dock = DockStyle.Fill, BackgroundColor = AppColors.CardBg, BorderStyle = BorderStyle.None, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, AccessibleName = "Lịch sử thu tiền" };
            history.Columns.Add("NgayThu", "Ngày thu");
            history.Columns.Add("SoTien", "Số tiền");
            history.Columns.Add("TenNhanVien", "Nhân viên");
            history.Columns.Add("GhiChu", "Ghi chú");
            try
            {
                foreach (DataRow payment in DataAccess.GetLoanPayments(maPM).Rows)
                    history.Rows.Add(Convert.ToDateTime(payment["NgayThu"]).ToString("dd/MM/yyyy HH:mm"), $"{Convert.ToDecimal(payment["SoTien"]):N0}đ", payment["TenNhanVien"], payment["GhiChu"]);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Không tải được lịch sử thanh toán: {ex}");
            }
            var amountLabel = new Label { Text = "Số tiền thu:", Location = new Point(18, 0), AutoSize = true };
            var amount = new NumericUpDown { Minimum = 0, Maximum = Math.Min(1000000000m, remaining), DecimalPlaces = 0, ThousandsSeparator = true, Size = new Size(190, 32), Location = new Point(110, -5), AccessibleName = "Số tiền thu" };
            if (remaining > 0) amount.Value = remaining;
            var note = new ModernTextBox { Placeholder = "Ghi chú (không bắt buộc)", Size = new Size(250, 36), Location = new Point(315, -7), AccessibleName = "Ghi chú thanh toán" };
            var editor = new Panel { Dock = DockStyle.Bottom, Height = 62, Padding = new Padding(18, 14, 18, 8), BackColor = AppColors.HeaderBg };
            editor.Controls.Add(amountLabel);
            editor.Controls.Add(amount);
            editor.Controls.Add(note);
            var footer = ResponsiveUi.AddDialogFooter(frm, 58);
            var cancel = new ModernButton { Text = "Đóng", Size = new Size(84, 36), BaseColor = AppColors.TextSecondary, BorderRadius = 10, DialogResult = DialogResult.Cancel, AccessibleName = "Đóng thu tiền" };
            var collect = new ModernButton { Text = "Thu tiền", Size = new Size(100, 36), BaseColor = AppColors.Primary, BorderRadius = 10, Enabled = remaining > 0, AccessibleName = "Ghi nhận thu tiền" };
            collect.Click += (_, _) =>
            {
                try
                {
                    if (!DataAccess.AddFinePayment(maPM, amount.Value, Session.CurrentUser!.MaNV, note.GetRealText(), out string? reason))
                    {
                        MessageBox.Show(reason ?? "Không thể ghi nhận thanh toán.", "Không thể thu tiền", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    frm.Close();
                    RefreshRows();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ghi nhận thanh toán thất bại: {ex}");
                    MessageBox.Show("Không thể ghi nhận thanh toán. Hãy chạy migration ThanhToanPhat trước.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            footer.Controls.Add(cancel);
            footer.Controls.Add(collect);
            frm.Controls.Add(history);
            frm.Controls.Add(editor);
            frm.Controls.Add(summaryLabel);
            frm.AcceptButton = collect;
            frm.CancelButton = cancel;
            frm.ActiveControl = amount;
            frm.ShowDialog();
        }

        private void ShowChiTiet(int maPM)
        {
            var frm = new Form { Text = $"Chi tiết Phiếu mượn #{maPM}", ClientSize = new Size(900, 420), MinimumSize = new Size(700, 340), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.Sizable, MaximizeBox = true, MinimizeBox = false };
            var dgvCT = new ModernDataGridView { Dock = DockStyle.Fill, BackgroundColor = AppColors.CardBg, BorderStyle = BorderStyle.None, RowHeadersVisible = false, AllowUserToAddRows = false, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = AppColors.Primary, ForeColor = Color.White, Font = new Font("Segoe UI", 10F, FontStyle.Bold) }, EnableHeadersVisualStyles = false, ColumnHeadersHeight = 35, DefaultCellStyle = new DataGridViewCellStyle { Font = new Font("Segoe UI", 10F) } };
            dgvCT.Columns.Add("TenSach", "Tên sách");
            dgvCT.Columns.Add("SoLuong", "SL mượn");
            dgvCT.Columns.Add("KetQua", "Kết quả");
            dgvCT.Columns.Add("NgayTra", "Ngày trả");
            dgvCT.Columns.Add("TienPhat", "Phạt quá hạn");
            dgvCT.Columns.Add("TienDen", "Tiền đền");
            dgvCT.Columns.Add("TongThu", "Tổng thu");
            bool hasReturned = false;
            try
            {
                foreach (DataRow row in DataAccess.GetChiTietPhieuMuon(maPM).Rows)
                {
                    int quantity = Convert.ToInt32(row["SoLuong"]);
                    int lostQuantity = Convert.ToInt32(row["SoLuongMat"]);
                    bool returned = row["NgayTra"] != DBNull.Value;
                    decimal fine = Convert.ToDecimal(row["TienPhat"]);
                    decimal compensation = Convert.ToDecimal(row["TienDenMatSach"]);
                    hasReturned |= returned;
                    string result = !returned
                        ? "Chưa trả"
                        : lostQuantity <= 0
                            ? "Đã trả"
                            : lostQuantity >= quantity
                                ? $"Mất {quantity} cuốn"
                                : $"Trả {quantity - lostQuantity} / Mất {lostQuantity}";
                    dgvCT.Rows.Add(
                        row["TenSach"],
                        quantity,
                        result,
                        returned ? Convert.ToDateTime(row["NgayTra"]).ToString("dd/MM/yyyy") : "—",
                        fine.ToString("N0") + "đ",
                        compensation.ToString("N0") + "đ",
                        (fine + compensation).ToString("N0") + "đ");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Không tải được chi tiết phiếu mượn: {ex}");
                MessageBox.Show("Không thể tải chi tiết phiếu mượn.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            var footer = ResponsiveUi.AddDialogFooter(frm, 58);
            var close = new ModernButton { Text = "Đóng", Size = new Size(84, 36), BaseColor = AppColors.TextSecondary, BorderRadius = 10, DialogResult = DialogResult.Cancel, AccessibleName = "Đóng chi tiết phiếu" };
            var print = new ModernButton { Text = "Lưu phiếu PDF", Size = new Size(112, 36), BaseColor = AppColors.Primary, BorderRadius = 10, AccessibleName = "Lưu phiếu mượn PDF" };
            var payment = new ModernButton { Text = "Thu tiền", Size = new Size(98, 36), BaseColor = AppColors.Warning, BorderRadius = 10, AccessibleName = "Thu tiền phiếu mượn" };
            var invoice = new ModernButton { Text = "Hóa đơn PDF", Size = new Size(108, 36), BaseColor = AppColors.Success, BorderRadius = 10, AccessibleName = "Lưu hóa đơn trả sách PDF", Visible = hasReturned };
            print.Click += (_, _) => PdfExportService.ExportLoanReceipt(maPM, frm);
            invoice.Click += (_, _) => PdfExportService.ExportReturnInvoice(maPM, frm);
            payment.Click += (_, _) => ShowPaymentDialog(maPM);
            footer.Controls.Add(close);
            footer.Controls.Add(print);
            footer.Controls.Add(payment);
            if (hasReturned) footer.Controls.Add(invoice);
            frm.Controls.Add(dgvCT);
            frm.AcceptButton = close;
            frm.CancelButton = close;
            frm.ShowDialog();
        }

        private void ShowInputDialog(int? editMaPM = null)
        {
            PhieuMuon? existing = null;
            var existingDetails = new Dictionary<int, int>();
            if (editMaPM.HasValue)
            {
                var pmTable = DataAccess.GetPhieuMuonById(editMaPM.Value);
                if (pmTable.Rows.Count == 0) { MessageBox.Show("Không tìm thấy phiếu mượn."); return; }
                var pmRow = pmTable.Rows[0];
                if (Convert.ToInt32(pmRow["SoDongDaTra"]) > 0 || pmRow["TrangThai"].ToString() == "Đã trả") { MessageBox.Show("Phiếu đã có sách trả nên không thể sửa toàn bộ."); return; }
                existing = new PhieuMuon { MaPhieuMuon = editMaPM.Value, MaDG = Convert.ToInt32(pmRow["MaDG"]), MaNV = Convert.ToInt32(pmRow["MaNV"]), NgayMuon = Convert.ToDateTime(pmRow["NgayMuon"]), HanTra = Convert.ToDateTime(pmRow["HanTra"]), TrangThai = pmRow["TrangThai"].ToString() ?? "Đang mượn" };
                foreach (DataRow row in DataAccess.GetChiTietPhieuMuon(editMaPM.Value).Rows) existingDetails[Convert.ToInt32(row["MaSach"])] = Convert.ToInt32(row["SoLuong"]);
            }

            var frm = new Form { Text = editMaPM.HasValue ? $"Sửa phiếu mượn #{editMaPM}" : "Tạo phiếu mượn mới", ClientSize = new Size(760, 560), MinimumSize = new Size(700, 500), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.Sizable, MaximizeBox = true, MinimizeBox = false };
            var lbl1 = new Label { Text = "Độc giả:", Location = new Point(20, 20), AutoSize = true };
            var cboDG = new ModernComboBox { Location = new Point(140, 17), Size = new Size(580, 30), DropDownStyle = ComboBoxStyle.DropDownList, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            var dgData = DataAccess.GetBorrowEligibleReaders();
            foreach (DataRow row in dgData.Rows) cboDG.Items.Add(new ComboItem(row["HoTen"].ToString()!, Convert.ToInt32(row["MaDG"])));
            if (existing != null && !cboDG.Items.Cast<ComboItem>().Any(x => x.Value == existing.MaDG))
            { var all = DataAccess.GetAllDocGia(); var row = all.Select($"MaDG={existing.MaDG}").FirstOrDefault(); if (row != null) cboDG.Items.Add(new ComboItem(row["HoTen"].ToString()!, existing.MaDG)); }
            if (existing != null) cboDG.SelectedItem = cboDG.Items.Cast<ComboItem>().FirstOrDefault(x => x.Value == existing.MaDG); else if (cboDG.Items.Count > 0) cboDG.SelectedIndex = 0;

            var lbl2 = new Label { Text = "Ngày mượn:", Location = new Point(20, 65), AutoSize = true };
            var dtpMuon = new DateTimePicker { Location = new Point(140, 62), Size = new Size(200, 30), Format = DateTimePickerFormat.Short, MaxDate = DateTime.Today, Value = existing?.NgayMuon ?? DateTime.Today };
            var lbl3 = new Label { Text = "Hạn trả:", Location = new Point(390, 65), AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            var dtpHan = new DateTimePicker { Location = new Point(470, 62), Size = new Size(200, 30), Format = DateTimePickerFormat.Short, MinDate = dtpMuon.Value.Date, Value = existing?.HanTra ?? DateTime.Today.AddDays(14), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            dtpMuon.ValueChanged += (s, e) => { if (dtpHan.Value.Date < dtpMuon.Value.Date) dtpHan.Value = dtpMuon.Value.Date; dtpHan.MinDate = dtpMuon.Value.Date; };

            var lbl4 = new Label { Text = "Sách (chỉ sửa chọn và số lượng):", Location = new Point(20, 110), AutoSize = true };
            var dgvSach = new ModernDataGridView { Location = new Point(20, 135), Size = new Size(700, 330), Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right, BackgroundColor = AppColors.CardBg, BorderStyle = BorderStyle.FixedSingle, RowHeadersVisible = false, AllowUserToAddRows = false, AllowUserToDeleteRows = false, ReadOnly = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = AppColors.Primary, ForeColor = Color.White, Font = new Font("Segoe UI", 9F, FontStyle.Bold) }, EnableHeadersVisualStyles = false, ColumnHeadersHeight = 30, DefaultCellStyle = new DataGridViewCellStyle { Font = new Font("Segoe UI", 10F) } };
            dgvSach.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Chon", HeaderText = "Chọn", FillWeight = 12 });
            var maCol = dgvSach.Columns.Add("MaSach", "Mã"); dgvSach.Columns[maCol].ReadOnly = true;
            var tenCol = dgvSach.Columns.Add("TenSach", "Tên sách"); dgvSach.Columns[tenCol].ReadOnly = true;
            var tonCol = dgvSach.Columns.Add("SoLuongKhaDung", "SL khả dụng"); dgvSach.Columns[tonCol].ReadOnly = true;
            dgvSach.Columns.Add("SoLuongMuon", "SL mượn");
            var sachData = editMaPM.HasValue ? DataAccess.GetSachAvailableForLoanEdit(editMaPM.Value) : DataAccess.GetSachAvailable();
            foreach (DataRow row in sachData.Rows)
            {
                int id = Convert.ToInt32(row["MaSach"]); int qty = existingDetails.GetValueOrDefault(id); int available = editMaPM.HasValue ? Convert.ToInt32(row["SoLuongKhaDung"]) : Convert.ToInt32(row["SoLuong"]); int index = dgvSach.Rows.Add(qty > 0, id, row["TenSach"], available, qty);
                dgvSach.Rows[index].Cells["SoLuongMuon"].Value = qty;
            }
            dgvSach.CurrentCellDirtyStateChanged += (s, e) => { if (dgvSach.IsCurrentCellDirty) dgvSach.CommitEdit(DataGridViewDataErrorContexts.Commit); };
            dgvSach.CellValueChanged += (s, e) => { if (e.RowIndex >= 0 && e.ColumnIndex == dgvSach.Columns["Chon"]!.Index) { bool selected = Convert.ToBoolean(dgvSach.Rows[e.RowIndex].Cells["Chon"]!.Value ?? false); object? oldValue = dgvSach.Rows[e.RowIndex].Cells["SoLuongMuon"]!.Value; int oldQty = int.TryParse(oldValue?.ToString(), out int parsed) ? parsed : 0; dgvSach.Rows[e.RowIndex].Cells["SoLuongMuon"]!.Value = selected ? Math.Max(1, oldQty) : 0; } };

            var btnOk = new ModernButton { Text = editMaPM.HasValue ? "Lưu thay đổi" : "Tạo phiếu", Location = new Point(470, 490), Size = new Size(130, 40), Anchor = AnchorStyles.Bottom | AnchorStyles.Right, BaseColor = AppColors.Primary, BorderRadius = 12 };
            var btnCancel = new ModernButton { Text = "Hủy", Location = new Point(620, 490), Size = new Size(100, 40), Anchor = AnchorStyles.Bottom | AnchorStyles.Right, BaseColor = AppColors.TextSecondary, BorderRadius = 12 };
            btnOk.Click += (s, e) =>
            {
                if (cboDG.SelectedItem is not ComboItem dg) { MessageBox.Show("Chọn độc giả!"); return; }
                if (dtpHan.Value.Date < dtpMuon.Value.Date) { MessageBox.Show("Hạn trả không được trước ngày mượn!"); return; }
                dgvSach.EndEdit();
                var selected = new List<(int maSach, int soLuong)>();
                foreach (DataGridViewRow row in dgvSach.Rows)
                {
                    bool check = Convert.ToBoolean(row.Cells["Chon"].Value ?? false); string text = row.Cells["SoLuongMuon"].Value?.ToString() ?? "0";
                    if (check && (!int.TryParse(text, out int qty) || qty <= 0)) { MessageBox.Show("Số lượng mượn phải là số nguyên lớn hơn 0."); return; }
                    if (check) { int max = Convert.ToInt32(row.Cells["SoLuongKhaDung"].Value); int qty2 = Convert.ToInt32(text); if (qty2 > max) { MessageBox.Show("Số lượng mượn vượt quá tồn kho khả dụng."); return; } selected.Add((Convert.ToInt32(row.Cells["MaSach"].Value), qty2)); }
                }
                if (selected.Count == 0) { MessageBox.Show("Chọn ít nhất một đầu sách!"); return; }
                var pm = existing ?? new PhieuMuon { MaNV = Session.CurrentUser!.MaNV };
                pm.MaDG = dg.Value; pm.NgayMuon = dtpMuon.Value.Date; pm.HanTra = dtpHan.Value.Date; pm.TrangThai = "Đang mượn";
                try
                {
                    bool ok = editMaPM.HasValue ? DataAccess.UpdatePhieuMuonFull(pm, selected, out string? reason) : DataAccess.InsertPhieuMuonFull(pm, selected, out reason);
                    if (!ok) { MessageBox.Show(reason ?? "Không thể lưu phiếu mượn.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                    frm.Close(); LoadData();
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Lưu phiếu mượn thất bại: {ex}"); MessageBox.Show("Không thể lưu phiếu mượn. Vui lòng kiểm tra dữ liệu.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            };
            btnCancel.Click += (s, e) => frm.Close();
            frm.Controls.AddRange(new Control[] { lbl1, cboDG, lbl2, dtpMuon, lbl3, dtpHan, lbl4, dgvSach, btnOk, btnCancel });
            frm.AcceptButton = btnOk;
            frm.CancelButton = btnCancel;
            frm.ActiveControl = cboDG;
            cboDG.AccessibleName = "Độc giả";
            dgvSach.AccessibleName = "Danh sách sách có thể mượn";
            frm.ShowDialog();
        }

        private void ShowPenaltyEditDialog(int maPM)
        {
            if (!Session.IsAdmin) return;
            decimal current = 0; foreach (DataRow row in DataAccess.GetChiTietPhieuMuon(maPM).Rows) current += Convert.ToDecimal(row["TienPhat"]);
            var frm = new Form { Text = $"Sửa tiền phạt - PM #{maPM}", ClientSize = new Size(400, 180), MinimumSize = new Size(360, 180), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.Sizable, MaximizeBox = true, MinimizeBox = false };
            frm.Controls.Add(new Label { Text = "Tổng phạt quá hạn (không gồm tiền đền):", Location = new Point(25, 25), AutoSize = true });
            var nud = new NumericUpDown { Location = new Point(25, 55), Size = new Size(340, 30), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, Minimum = 0, Maximum = 1000000000, DecimalPlaces = 0, ThousandsSeparator = true, Value = Math.Min(1000000000m, current) };
            var btnSave = new ModernButton { Text = "Lưu", Location = new Point(185, 110), Size = new Size(85, 38), Anchor = AnchorStyles.Bottom | AnchorStyles.Right, BaseColor = AppColors.Primary, BorderRadius = 12 };
            var btnCancel = new ModernButton { Text = "Hủy", Location = new Point(280, 110), Size = new Size(85, 38), Anchor = AnchorStyles.Bottom | AnchorStyles.Right, BaseColor = AppColors.TextSecondary, BorderRadius = 12 };
            btnSave.Click += (s, e) => { try { if (!DataAccess.UpdateReturnedLoanPenalty(maPM, nud.Value, Session.CurrentUser!.MaNV, out string? reason)) { MessageBox.Show(reason ?? "Không thể cập nhật tiền phạt."); return; } frm.Close(); LoadData(); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Cập nhật tiền phạt thất bại: {ex}"); MessageBox.Show("Không thể cập nhật tiền phạt. Vui lòng thử lại.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); } };
            btnCancel.Click += (s, e) => frm.Close();
            frm.Controls.AddRange(new Control[] { nud, btnSave, btnCancel });
            frm.AcceptButton = btnSave;
            frm.CancelButton = btnCancel;
            frm.ActiveControl = nud;
            nud.AccessibleName = "Tổng tiền phạt";
            frm.ShowDialog();
        }

        private sealed record LoanRowState(int Unreturned, int Returned);

        private sealed class DemoComboItem
        {
            public string Text { get; }
            public int Value { get; }
            public DemoComboItem(string text, int value) { Text = text; Value = value; }
            public override string ToString() => Text;
        }

        private sealed class DemoScenarioItem
        {
            public string Text { get; }
            public DemoLoanScenario Value { get; }
            public DemoScenarioItem(string text, DemoLoanScenario value) { Text = text; Value = value; }
            public override string ToString() => Text;
        }

        private sealed class ComboItem
        {
            public string Text { get; } public int Value { get; }
            public ComboItem(string text, int value) { Text = text; Value = value; }
            public override string ToString() => Text;
        }
    }
}

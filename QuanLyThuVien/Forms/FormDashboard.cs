using System.Data;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.WinForms;
using QuanLyThuVien.Controls;
using QuanLyThuVien.Data;
using QuanLyThuVien.Helpers;
using SkiaSharp;

namespace QuanLyThuVien.Forms
{
    public class FormDashboard : UserControl
    {
        private Panel _scrollHost = null!;
        private TableLayoutPanel _content = null!;
        private TableLayoutPanel _kpiGrid = null!;
        private TableLayoutPanel _workGrid = null!;
        private TableLayoutPanel _analyticsGrid = null!;
        private DashboardSection _attentionSection = null!;
        private DashboardSection _recentSection = null!;
        private DashboardSection _trendSection = null!;
        private DashboardSection _categorySection = null!;
        private bool? _compactLayout;

        private static readonly SKTypeface VietnameseTypeface = SKTypeface.FromFamilyName("Segoe UI");

        public FormDashboard()
        {
            BackColor = AppColors.WorkbenchBg;
            Padding = Padding.Empty;
            AutoScroll = false;
            Load += (_, _) => BuildUI();
        }

        public void BuildUI()
        {
            ResponsiveUi.DisposeChildren(this);
            _compactLayout = null;

            _scrollHost = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(24, 20, 24, 24),
                BackColor = AppColors.WorkbenchBg,
                AccessibleRole = AccessibleRole.Grouping,
                AccessibleName = "Nội dung tổng quan thư viện"
            };
            _scrollHost.Resize += (_, _) => ApplyResponsiveLayout();

            _content = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 1,
                RowCount = 4,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                BackColor = Color.Transparent
            };
            _content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            var header = BuildHeader();
            _kpiGrid = BuildKpiGrid();

            DataTable? loans = null;
            Exception? loanError = null;
            try
            {
                loans = DataAccess.GetAllPhieuMuon();
            }
            catch (Exception ex)
            {
                loanError = ex;
                System.Diagnostics.Debug.WriteLine($"Không tải được dữ liệu phiếu mượn cho Dashboard: {ex}");
            }

            _attentionSection = BuildAttentionSection(loans, loanError);
            _recentSection = BuildRecentSection(loans, loanError);
            _workGrid = BuildWorkGrid(_attentionSection, _recentSection);

            _trendSection = BuildTrendSection();
            _categorySection = BuildCategorySection();
            _analyticsGrid = BuildAnalyticsGrid(_trendSection, _categorySection);

            AddContentRow(header, 78, 16);
            AddContentRow(_kpiGrid, 112, 16);
            AddContentRow(_workGrid, 190, 16);
            AddContentRow(_analyticsGrid, 300, 0);

            _scrollHost.Controls.Add(_content);
            Controls.Add(_scrollHost);

            ApplyResponsiveLayout();
            BeginInvoke(ApplyResponsiveLayout);
        }

        private void AddContentRow(Control control, int height, int bottomMargin)
        {
            control.Dock = DockStyle.Top;
            control.Height = height;
            control.Margin = new Padding(0, 0, 0, bottomMargin);
            _content.Controls.Add(control, 0, _content.Controls.Count);
        }

        private Control BuildHeader()
        {
            var header = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Margin = Padding.Empty,
                AccessibleRole = AccessibleRole.Grouping,
                AccessibleName = "Tổng quan thư viện"
            };

            var greeting = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 22F, FontStyle.Bold),
                ForeColor = AppColors.TextPrimary,
                Text = GetGreeting(),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(0, 0, 16, 0),
                AutoEllipsis = true
            };

            var datePanel = new TableLayoutPanel
            {
                Dock = DockStyle.Right,
                Width = 170,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            datePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            datePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 62F));
            datePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 38F));
            datePanel.Controls.Add(new Label
            {
                Text = DateTime.Now.ToString("dd/MM/yyyy"),
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                ForeColor = AppColors.TextPrimary,
                TextAlign = ContentAlignment.BottomRight,
                AccessibleName = "Ngày hiện tại"
            }, 0, 0);
            datePanel.Controls.Add(new Label
            {
                Text = "HÔM NAY · CA LÀM VIỆC",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI Semibold", 8F, FontStyle.Bold),
                ForeColor = AppColors.TextSecondary,
                TextAlign = ContentAlignment.TopRight
            }, 0, 1);

            header.Controls.Add(datePanel);
            header.Controls.Add(greeting);
            return header;
        }

        private string GetGreeting()
        {
            string name = Session.CurrentUser?.HoTen?.Trim() ?? "Thủ thư";
            string greeting = DateTime.Now.Hour switch
            {
                < 12 => "Chào buổi sáng",
                < 18 => "Chào buổi chiều",
                _ => "Chào buổi tối"
            };
            return $"{greeting}, {name}";
        }

        private TableLayoutPanel BuildKpiGrid()
        {
            int openLoans = TryGetMetric(() => DataAccess.CountPhieuMuonDangMo(), "phiếu đang mở");
            int books = TryGetMetric(DataAccess.CountSach, "sách trong kho");
            int readers = TryGetMetric(DataAccess.CountDocGia, "bạn đọc");
            decimal outstanding = TryGetMetric(DataAccess.GetTongTienChuaThu, "tiền chưa thu");

            var cards = new[]
            {
                CreateKpiCard("Phiếu đang mở", openLoans.ToString("D2"), "phiếu đang sử dụng", AppColors.Primary),
                CreateKpiCard("Sách trong kho", books.ToString("N0"), "đầu sách trong danh mục", AppColors.Primary),
                CreateKpiCard("Bạn đọc", readers.ToString("N0"), "hồ sơ bạn đọc", AppColors.Success),
                CreateKpiCard("Chưa thu", $"{outstanding:N0}đ", "còn phải xử lý", AppColors.Warning)
            };

            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                AccessibleRole = AccessibleRole.Grouping,
                AccessibleName = "Chỉ số thư viện"
            };
            foreach (var card in cards)
                grid.Controls.Add(card);
            return grid;
        }

        private static StatCard CreateKpiCard(string title, string value, string unit, Color accent)
        {
            return new StatCard
            {
                Title = title,
                Value = value,
                Unit = unit,
                AccentColor = accent,
                BorderRadius = 14,
                Dock = DockStyle.Fill,
                Margin = Padding.Empty
            };
        }

        private DashboardSection BuildAttentionSection(DataTable? loans, Exception? loadError)
        {
            var section = new DashboardSection("Cần xử lý hôm nay");
            if (loadError != null)
            {
                AddErrorState(section.Body, "Không thể tải danh sách cần xử lý.", BuildReloadButton());
                return section;
            }

            var alerts = new List<(string Title, string Detail, Color Color)>();
            try
            {
                if (loans != null)
                {
                    foreach (var item in loans.AsEnumerable()
                                 .Select(row =>
                                 {
                                     DateTime due = Convert.ToDateTime(row["HanTra"]);
                                     int unreturned = Convert.ToInt32(row["SoDongChuaTra"]);
                                     int returned = Convert.ToInt32(row["SoDongDaTra"]);
                                     int days = due.Date < DateTime.Today ? (DateTime.Today - due.Date).Days : 0;
                                     return new { Row = row, Due = due, Unreturned = unreturned, Returned = returned, Days = days, Priority = unreturned > 0 && days > 0 ? 0 : 1 };
                                 })
                                 .Where(item => item.Unreturned > 0 && (item.Days > 0 || item.Due.Date <= DateTime.Today.AddDays(3)))
                                 .OrderBy(item => item.Priority)
                                 .ThenBy(item => item.Due)
                                 .Take(4))
                    {
                        string name = item.Row["TenDocGia"]?.ToString() ?? "Độc giả";
                        string detail = item.Days > 0 ? $"Quá hạn {item.Days} ngày" : $"Hạn trả {item.Due:dd/MM/yyyy}";
                        alerts.Add(($"PM #{item.Row["MaPhieuMuon"]} · {name}", detail, item.Days > 0 ? AppColors.Danger : AppColors.Warning));
                    }
                }

                AddSafeAlert(alerts, () =>
                {
                    int expiring = DataAccess.CountDocGiaSapHetHan();
                    return expiring > 0 ? ("Thẻ độc giả sắp hết hạn", $"{expiring} thẻ trong 30 ngày", AppColors.Warning) : null;
                });
                AddSafeAlert(alerts, () =>
                {
                    int lowStock = Convert.ToInt32(DataAccess.ExecuteScalar("SELECT COUNT(*) FROM Sach WHERE SoLuong<=2"));
                    return lowStock > 0 ? ("Sách sắp hết", $"{lowStock} đầu sách còn không quá 2 cuốn", AppColors.Warning) : null;
                });
                AddSafeAlert(alerts, () =>
                {
                    decimal amount = DataAccess.GetTongTienChuaThu();
                    return amount > 0 ? ("Khoản phạt chưa thu", $"Còn {amount:N0}đ cần xử lý", AppColors.Danger) : null;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Không tải được khu cần xử lý: {ex}");
                AddErrorState(section.Body, "Không thể tải đầy đủ các cảnh báo.", BuildReloadButton());
                return section;
            }

            if (alerts.Count == 0)
            {
                section.Body.Controls.Add(new Label
                {
                    Text = "Không có việc cần xử lý ngay.",
                    Dock = DockStyle.Fill,
                    Font = new Font("Segoe UI", 10F),
                    ForeColor = AppColors.SuccessDark,
                    TextAlign = ContentAlignment.MiddleLeft,
                    AccessibleName = "Không có việc cần xử lý ngay"
                });
                return section;
            }

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Color.Transparent,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                AccessibleRole = AccessibleRole.List,
                AccessibleName = "Danh sách việc cần xử lý"
            };
            foreach (var alert in alerts)
                flow.Controls.Add(CreateAlertRow(alert.Title, alert.Detail, alert.Color));
            flow.Resize += (_, _) => ResizeFlowChildren(flow);
            section.Body.Controls.Add(flow);
            ResizeFlowChildren(flow);
            return section;
        }

        private static void AddSafeAlert(List<(string Title, string Detail, Color Color)> alerts, Func<(string Title, string Detail, Color Color)?> factory)
        {
            try
            {
                var alert = factory();
                if (alert.HasValue) alerts.Add(alert.Value);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Không tải được một cảnh báo Dashboard: {ex.Message}");
            }
        }

        private static Panel CreateAlertRow(string title, string detail, Color detailColor)
        {
            var row = new TableLayoutPanel
            {
                Height = 30,
                Width = 300,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 2),
                Padding = Padding.Empty,
                AccessibleRole = AccessibleRole.ListItem,
                AccessibleName = $"{title}, {detail}"
            };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 66F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34F));
            row.Controls.Add(new Label
            {
                Text = title,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                ForeColor = AppColors.TextPrimary,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true,
                Margin = Padding.Empty
            }, 0, 0);
            row.Controls.Add(new Label
            {
                Text = detail,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F),
                ForeColor = detailColor,
                TextAlign = ContentAlignment.MiddleRight,
                AutoEllipsis = true,
                Margin = Padding.Empty
            }, 1, 0);
            return row;
        }

        private DashboardSection BuildRecentSection(DataTable? loans, Exception? loadError)
        {
            var section = new DashboardSection("Phiếu mượn gần đây");
            var grid = new ModernDataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = AppColors.WorkbenchSurface,
                ColumnHeadersHeight = 32,
                RowTemplate = { Height = 28 },
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ScrollBars = ScrollBars.Vertical,
                TabIndex = 0,
                AccessibleName = "Phiếu mượn gần đây"
            };
            grid.DefaultCellStyle.BackColor = AppColors.WorkbenchSurface;
            grid.AlternatingRowsDefaultCellStyle.BackColor = AppColors.WorkbenchBg;
            grid.Columns.Add("MaPhieuMuon", "Mã PM");
            grid.Columns.Add("TenDocGia", "Độc giả");
            grid.Columns.Add("HanTra", "Hạn trả");
            grid.Columns.Add("TrangThai", "Trạng thái");

            if (loadError != null)
            {
                grid.Rows.Add("—", "Không tải được dữ liệu", "—", "—");
            }
            else
            {
                foreach (DataRow row in (loans?.AsEnumerable() ?? Enumerable.Empty<DataRow>())
                             .OrderByDescending(item => Convert.ToDateTime(item["NgayMuon"]))
                             .ThenByDescending(item => Convert.ToInt32(item["MaPhieuMuon"]))
                             .Take(4))
                {
                    DateTime due = Convert.ToDateTime(row["HanTra"]);
                    int unreturned = Convert.ToInt32(row["SoDongChuaTra"]);
                    int returned = Convert.ToInt32(row["SoDongDaTra"]);
                    grid.Rows.Add(row["MaPhieuMuon"], row["TenDocGia"], due.ToString("dd/MM/yyyy"), ResolveLoanStatus(row, due, unreturned, returned));
                }
                if (grid.Rows.Count == 0)
                    grid.Rows.Add("—", "Chưa có phiếu mượn", "—", "—");
            }

            section.Body.Controls.Add(grid);
            return section;
        }

        private static string ResolveLoanStatus(DataRow row, DateTime due, int unreturned, int returned)
        {
            if (unreturned > 0 && returned > 0) return "Đã trả một phần";
            if (unreturned > 0 && due.Date < DateTime.Today) return "Quá hạn";
            if (unreturned == 0 && returned > 0) return "Đã trả";
            return row["TrangThai"]?.ToString() ?? "Đang mượn";
        }

        private TableLayoutPanel BuildWorkGrid(Control attention, Control recent)
        {
            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                AccessibleRole = AccessibleRole.Grouping,
                AccessibleName = "Công việc và hoạt động gần đây"
            };
            attention.Dock = DockStyle.Fill;
            recent.Dock = DockStyle.Fill;
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            attention.Margin = new Padding(0, 0, 8, 0);
            recent.Margin = new Padding(8, 0, 0, 0);
            grid.Controls.Add(attention, 0, 0);
            grid.Controls.Add(recent, 1, 0);
            return grid;
        }

        private DashboardSection BuildTrendSection()
        {
            var section = new DashboardSection("Xu hướng mượn sách");
            var chart = CreateTrendChart();
            chart.Dock = DockStyle.Fill;
            chart.AccessibleName = "Biểu đồ xu hướng mượn sách trong năm";
            section.Body.Controls.Add(chart);
            return section;
        }

        private DashboardSection BuildCategorySection()
        {
            var section = new DashboardSection("Kho sách theo thể loại");
            var chart = CreateCategoryChart();
            chart.Dock = DockStyle.Fill;
            chart.AccessibleName = "Biểu đồ số sách theo thể loại";
            section.Body.Controls.Add(chart);
            return section;
        }

        private TableLayoutPanel BuildAnalyticsGrid(Control trend, Control category)
        {
            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                AccessibleRole = AccessibleRole.Grouping,
                AccessibleName = "Phân tích thư viện"
            };
            trend.Dock = DockStyle.Fill;
            category.Dock = DockStyle.Fill;
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 66F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34F));
            trend.Margin = new Padding(0, 0, 8, 0);
            category.Margin = new Padding(8, 0, 0, 0);
            grid.Controls.Add(trend, 0, 0);
            grid.Controls.Add(category, 1, 0);
            return grid;
        }

        private CartesianChart CreateTrendChart()
        {
            var chart = new CartesianChart
            {
                LegendPosition = LiveChartsCore.Measure.LegendPosition.Hidden,
                AnimationsSpeed = TimeSpan.Zero,
                BackColor = AppColors.WorkbenchSurface
            };
            try
            {
                var dt = DataAccess.ExecuteQuery(
                    "SELECT MONTH(pm.NgayMuon) AS Thang, SUM(ctpm.SoLuong) AS SoLuong FROM PhieuMuon pm JOIN ChiTietPhieuMuon ctpm ON pm.MaPhieuMuon=ctpm.MaPhieuMuon WHERE YEAR(pm.NgayMuon)=YEAR(GETDATE()) GROUP BY MONTH(pm.NgayMuon) ORDER BY Thang");
                var labels = new List<string>();
                var values = new List<double>();
                foreach (DataRow row in dt.Rows)
                {
                    labels.Add($"T{Convert.ToInt32(row["Thang"])}");
                    values.Add(Convert.ToDouble(row["SoLuong"]));
                }
                bool empty = labels.Count == 0;
                if (empty)
                {
                    labels.AddRange(new[] { "T1", "T2", "T3", "T4", "T5", "T6" });
                    values.AddRange(new double[] { 0, 0, 0, 0, 0, 0 });
                }

                var axisColor = new SKColor(AppColors.TextSecondary.R, AppColors.TextSecondary.G, AppColors.TextSecondary.B);
                var separator = new SolidColorPaint(new SKColor(AppColors.Border.R, AppColors.Border.G, AppColors.Border.B));
                chart.XAxes = new[]
                {
                    new Axis
                    {
                        Labels = labels.ToArray(),
                        LabelsRotation = 0,
                        TextSize = 10,
                        LabelsPaint = new SolidColorPaint(axisColor) { SKTypeface = VietnameseTypeface },
                        SeparatorsPaint = separator
                    }
                };
                chart.YAxes = new[]
                {
                    new Axis
                    {
                        TextSize = 10,
                        LabelsPaint = new SolidColorPaint(axisColor) { SKTypeface = VietnameseTypeface },
                        SeparatorsPaint = separator
                    }
                };
                var primary = new SKColor(AppColors.Primary.R, AppColors.Primary.G, AppColors.Primary.B);
                chart.Series = new ISeries[]
                {
                    new LineSeries<double>
                    {
                        Values = values.ToArray(),
                        Fill = new SolidColorPaint(new SKColor(AppColors.Primary.R, AppColors.Primary.G, AppColors.Primary.B, 30)),
                        Stroke = new SolidColorPaint(primary) { StrokeThickness = 3 },
                        GeometryFill = new SolidColorPaint(primary),
                        GeometrySize = 9,
                        LineSmoothness = 0.35
                    }
                };
                if (empty)
                    AddChartMessage(chart, "Chưa có dữ liệu mượn trong năm.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Không tải được biểu đồ xu hướng: {ex}");
                AddChartMessage(chart, "Không thể tải biểu đồ xu hướng.", AppColors.Danger);
            }
            return chart;
        }

        private PieChart CreateCategoryChart()
        {
            var chart = new PieChart
            {
                LegendPosition = LiveChartsCore.Measure.LegendPosition.Bottom,
                LegendTextSize = 10,
                AnimationsSpeed = TimeSpan.Zero,
                BackColor = AppColors.WorkbenchSurface
            };
            try
            {
                var dt = DataAccess.GetSachByTheLoai();
                var colors = new[]
                {
                    AppColors.Primary,
                    AppColors.Success,
                    AppColors.CardPurple,
                    AppColors.Accent,
                    AppColors.Info,
                    AppColors.Danger
                };
                var series = new List<ISeries>();
                int index = 0;
                foreach (DataRow row in dt.Rows)
                {
                    series.Add(new PieSeries<double>
                    {
                        Values = new[] { Convert.ToDouble(row["SoLuong"]) },
                        Name = row["TenTheLoai"]?.ToString() ?? "Khác",
                        Fill = new SolidColorPaint(new SKColor(colors[index % colors.Length].R, colors[index % colors.Length].G, colors[index % colors.Length].B))
                    });
                    index++;
                }
                chart.Series = series.ToArray();
                if (series.Count == 0)
                    AddChartMessage(chart, "Chưa có dữ liệu thể loại.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Không tải được biểu đồ thể loại: {ex}");
                AddChartMessage(chart, "Không thể tải biểu đồ thể loại.", AppColors.Danger);
            }
            return chart;
        }

        private static void AddChartMessage(Control chart, string message, Color? color = null)
        {
            chart.Controls.Add(new Label
            {
                Text = message,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                ForeColor = color ?? AppColors.TextSecondary,
                Font = new Font("Segoe UI", 9F),
                TextAlign = ContentAlignment.MiddleCenter,
                AccessibleName = message
            });
        }

        private ModernButton BuildReloadButton()
        {
            var button = new ModernButton
            {
                Text = "Tải lại",
                Size = new Size(96, 40),
                BaseColor = AppColors.Primary,
                BorderRadius = 10,
                AccessibleName = "Tải lại dữ liệu Dashboard",
                Margin = new Padding(0, 8, 0, 0)
            };
            button.Click += (_, _) => BeginInvoke(BuildUI);
            return button;
        }

        private static void AddErrorState(Control host, string message, Control? action = null)
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = action == null ? 1 : 2,
                BackColor = Color.Transparent,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, action == null ? 100F : 65F));
            if (action != null) panel.RowStyles.Add(new RowStyle(SizeType.Percent, 35F));
            panel.Controls.Add(new Label
            {
                Text = message,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F),
                ForeColor = AppColors.Danger,
                TextAlign = action == null ? ContentAlignment.MiddleLeft : ContentAlignment.BottomLeft,
                AutoEllipsis = true,
                AccessibleName = message
            }, 0, 0);
            if (action != null)
                panel.Controls.Add(action, 0, 1);
            host.Controls.Add(panel);
        }

        private static void ResizeFlowChildren(FlowLayoutPanel flow)
        {
            int width = Math.Max(1, flow.ClientSize.Width - (flow.VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0));
            foreach (Control child in flow.Controls)
                child.Width = width;
        }

        private static int TryGetMetric(Func<int> getter, string metricName)
        {
            try { return getter(); }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Không tải được KPI {metricName}: {ex.Message}");
                return 0;
            }
        }

        private static decimal TryGetMetric(Func<decimal> getter, string metricName)
        {
            try { return getter(); }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Không tải được KPI {metricName}: {ex.Message}");
                return 0m;
            }
        }

        private void ApplyResponsiveLayout()
        {
            if (_scrollHost == null || _kpiGrid == null || _workGrid == null || _analyticsGrid == null) return;
            int availableWidth = Math.Max(320, _scrollHost.ClientSize.Width - _scrollHost.Padding.Horizontal);
            bool compact = availableWidth < 920;
            if (_compactLayout == compact && _kpiGrid.Width == availableWidth) return;
            _compactLayout = compact;

            _kpiGrid.Width = availableWidth;
            _workGrid.Width = availableWidth;
            _analyticsGrid.Width = availableWidth;
            _content.Width = availableWidth;

            ConfigureKpiGrid(compact);
            ConfigureWorkGrid(compact);
            ConfigureAnalyticsGrid(compact);
            _content.PerformLayout();
            _scrollHost.PerformLayout();
        }

        private void ConfigureKpiGrid(bool compact)
        {
            _kpiGrid.SuspendLayout();
            _kpiGrid.ColumnStyles.Clear();
            _kpiGrid.RowStyles.Clear();
            _kpiGrid.ColumnCount = compact ? 2 : 4;
            _kpiGrid.RowCount = compact ? 2 : 1;
            for (int col = 0; col < _kpiGrid.ColumnCount; col++)
                _kpiGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / _kpiGrid.ColumnCount));
            for (int row = 0; row < _kpiGrid.RowCount; row++)
                _kpiGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / _kpiGrid.RowCount));
            _kpiGrid.Height = compact ? 224 : 112;

            var cards = _kpiGrid.Controls.Cast<Control>().ToArray();
            for (int i = 0; i < cards.Length; i++)
            {
                int row = compact ? i / 2 : 0;
                int col = compact ? i % 2 : i;
                _kpiGrid.SetCellPosition(cards[i], new TableLayoutPanelCellPosition(col, row));
                cards[i].Margin = compact
                    ? new Padding(col == 0 ? 0 : 4, row == 0 ? 0 : 4, col == 1 ? 0 : 4, row == 1 ? 0 : 4)
                    : new Padding(i == 0 ? 0 : 4, 0, i == cards.Length - 1 ? 0 : 4, 0);
            }
            _kpiGrid.ResumeLayout(true);
        }

        private void ConfigureWorkGrid(bool compact)
        {
            _workGrid.SuspendLayout();
            _workGrid.ColumnStyles.Clear();
            _workGrid.RowStyles.Clear();
            _workGrid.ColumnCount = compact ? 1 : 2;
            _workGrid.RowCount = compact ? 2 : 1;
            for (int col = 0; col < _workGrid.ColumnCount; col++)
                _workGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, compact ? 100F : col == 0 ? 60F : 40F));
            for (int row = 0; row < _workGrid.RowCount; row++)
                _workGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / _workGrid.RowCount));
            _workGrid.Height = compact ? 380 : 190;
            _workGrid.SetCellPosition(_attentionSection, new TableLayoutPanelCellPosition(0, 0));
            _workGrid.SetCellPosition(_recentSection, new TableLayoutPanelCellPosition(compact ? 0 : 1, compact ? 1 : 0));
            _attentionSection.Margin = compact ? new Padding(0, 0, 0, 8) : new Padding(0, 0, 8, 0);
            _recentSection.Margin = compact ? new Padding(0, 8, 0, 0) : new Padding(8, 0, 0, 0);
            _workGrid.ResumeLayout(true);
        }

        private void ConfigureAnalyticsGrid(bool compact)
        {
            _analyticsGrid.SuspendLayout();
            _analyticsGrid.ColumnStyles.Clear();
            _analyticsGrid.RowStyles.Clear();
            _analyticsGrid.ColumnCount = 1;
            _analyticsGrid.RowCount = compact ? 2 : 1;
            _analyticsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (int row = 0; row < _analyticsGrid.RowCount; row++)
                _analyticsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / _analyticsGrid.RowCount));
            _analyticsGrid.Height = compact ? 600 : 300;
            if (compact)
            {
                _analyticsGrid.SetCellPosition(_trendSection, new TableLayoutPanelCellPosition(0, 0));
                _analyticsGrid.SetCellPosition(_categorySection, new TableLayoutPanelCellPosition(0, 1));
                _trendSection.Margin = new Padding(0, 0, 0, 8);
                _categorySection.Margin = new Padding(0, 8, 0, 0);
            }
            else
            {
                _analyticsGrid.ColumnCount = 2;
                _analyticsGrid.ColumnStyles.Clear();
                _analyticsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 66F));
                _analyticsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34F));
                _analyticsGrid.SetCellPosition(_trendSection, new TableLayoutPanelCellPosition(0, 0));
                _analyticsGrid.SetCellPosition(_categorySection, new TableLayoutPanelCellPosition(1, 0));
                _trendSection.Margin = new Padding(0, 0, 8, 0);
                _categorySection.Margin = new Padding(8, 0, 0, 0);
            }
            _analyticsGrid.ResumeLayout(true);
        }
    }
}

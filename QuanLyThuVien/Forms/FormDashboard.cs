using System.Data;
using System.Text;
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
        private PieChart? _pieChart;
        private CartesianChart? _lineChart;
        private Label? _lblDate;
        private Label? _lblHeroTitle;
        private Label? _lblHeroSub;
        private Panel? _panelDarkCard;
        private Panel? _panelRightStats;
        private PictureBox? _picBanner;
        private System.Windows.Forms.Timer? _bannerTimer;
        private int _bannerIndex = 0;
        private Image?[] _bannerImages = Array.Empty<Image?>();

        private static readonly SKTypeface _vietnameseFont = SKTypeface.FromFamilyName("Segoe UI");

        public FormDashboard()
        {
            BackColor = AppColors.ContentBg;
            Padding = new Padding(25, 20, 25, 20);
            AutoScroll = true;
            Load += FormDashboard_Load;
            Resize += (s, e) => LayoutControls();
        }

        private void FormDashboard_Load(object? sender, EventArgs e)
        {
            BuildUI();
        }

        public void BuildUI()
        {
            Controls.Clear();

            // Hero section
            _lblHeroTitle = new Label
            {
                Text = "Sách hay - tri thức",
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = AppColors.TextPrimary,
                AutoSize = true,
                Location = new Point(Padding.Left, 5)
            };
            Controls.Add(_lblHeroTitle);

            _lblDate = new Label
            {
                Text = DateTime.Now.ToString("dd/MM/yyyy").ToUpper(),
                Font = new Font("Segoe UI", 10F),
                ForeColor = AppColors.TextSecondary,
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            Controls.Add(_lblDate);

            _lblHeroSub = new Label
            {
                Text = "HÔM NAY",
                Font = new Font("Segoe UI", 9F),
                ForeColor = AppColors.TextSecondary,
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            Controls.Add(_lblHeroSub);

            // Dark card - Phiếu mượn đang mở
            int dangMuon = DataAccess.CountPhieuMuonDangMo();
            _panelDarkCard = new Panel
            {
                BackColor = AppColors.SidebarBg,
                Size = new Size(320, 220)
            };

            var lblDarkTitle = new Label
            {
                Text = "PHIẾU MƯỢN ĐANG MỞ",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(180, 180, 180),
                AutoSize = true,
                Location = new Point(20, 20)
            };
            _panelDarkCard.Controls.Add(lblDarkTitle);

            var lblDarkValue = new Label
            {
                Text = dangMuon.ToString("D2"),
                Font = new Font("Segoe UI", 48F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(15, 50)
            };
            _panelDarkCard.Controls.Add(lblDarkValue);

            var lblDarkSub = new Label
            {
                Text = "Phiếu đang được sử dụng trong ngày hôm nay",
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.FromArgb(140, 140, 140),
                AutoSize = true,
                Location = new Point(20, 185)
            };
            _panelDarkCard.Controls.Add(lblDarkSub);

            Controls.Add(_panelDarkCard);

            // Right side stats
            _panelRightStats = new Panel
            {
                BackColor = Color.Transparent,
                Size = new Size(260, 180)
            };
            BuildRightStats();
            Controls.Add(_panelRightStats);

            // Banner - auto switch
            LoadBannerImages();
            _picBanner = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(230, 230, 230),
                Image = _bannerImages.Length > 0 ? _bannerImages[0] : null
            };
            Controls.Add(_picBanner);

            _bannerTimer?.Stop();
            _bannerTimer?.Dispose();
            _bannerTimer = new System.Windows.Forms.Timer { Interval = 4000 };
            _bannerTimer.Tick += (s, e) =>
            {
                if (_bannerImages.Length < 2) return;
                _bannerIndex = (_bannerIndex + 1) % _bannerImages.Length;
                _picBanner.Image = _bannerImages[_bannerIndex];
            };
            if (_bannerImages.Length > 1)
                _bannerTimer.Start();

            // Charts
            _pieChart = CreatePieChart();
            _lineChart = CreateLineChart();
            Controls.Add(_pieChart);
            Controls.Add(_lineChart);

            LayoutControls();
        }

        private void LoadBannerImages()
        {
            var bannerDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "Banner");
            if (!Directory.Exists(bannerDir)) return;

            var files = Directory.GetFiles(bannerDir, "*.jpg").OrderBy(f => f).ToArray();
            foreach (var img in _bannerImages)
                img?.Dispose();
            _bannerImages = new Image?[files.Length];
            for (int i = 0; i < files.Length; i++)
            {
                try
                {
                    using var fs = new FileStream(files[i], FileMode.Open, FileAccess.Read);
                    _bannerImages[i] = Image.FromStream(fs).Clone() as Image;
                }
                catch { _bannerImages[i] = null; }
            }
        }

        private void BuildRightStats()
        {
            _panelRightStats!.Controls.Clear();

            int tongSach = DataAccess.CountSach();
            int tongDG = DataAccess.CountDocGia();
            int quaHan = DataAccess.CountQuaHan();

            var stats = new[]
            {
                ("SÁCH TRONG KHO", tongSach.ToString(), AppColors.Primary),
                ("BẠN ĐỌC", tongDG.ToString(), AppColors.CardGreen),
                ("SÁCH QUÁ HẠN", quaHan.ToString(), AppColors.CardOrange),
            };

            int y = 0;
            foreach (var (title, value, color) in stats)
            {
                var panel = new Panel
                {
                    BackColor = Color.White,
                    Size = new Size(250, 48),
                    Location = new Point(0, y),
                    Padding = new Padding(15, 8, 15, 8)
                };

                var lblTitle = new Label
                {
                    Text = title,
                    Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                    ForeColor = AppColors.TextSecondary,
                    AutoSize = true,
                    Location = new Point(15, 8)
                };
                panel.Controls.Add(lblTitle);

                var lblValue = new Label
                {
                    Text = value,
                    Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                    ForeColor = AppColors.TextPrimary,
                    AutoSize = true,
                    Location = new Point(150, 3)
                };
                panel.Controls.Add(lblValue);

                var accentLine = new Panel
                {
                    BackColor = color,
                    Size = new Size(panel.Width, 3),
                    Location = new Point(0, panel.Height - 3),
                    Dock = DockStyle.Bottom
                };
                panel.Controls.Add(accentLine);

                _panelRightStats.Controls.Add(panel);
                y += 58;
            }
        }

        private void LayoutControls()
        {
            int availableWidth = Width - Padding.Left - Padding.Right - 10;

            // Hero title and date
            if (_lblHeroTitle != null)
                _lblHeroTitle.Location = new Point(Padding.Left, 15);

            if (_lblDate != null)
                _lblDate.Location = new Point(availableWidth - 80, 15);
            if (_lblHeroSub != null)
                _lblHeroSub.Location = new Point(availableWidth - 80, 35);

            // Dark card - push below hero title
            if (_panelDarkCard != null)
                _panelDarkCard.Location = new Point(Padding.Left, 130);

            // Right stats - push below hero title
            if (_panelRightStats != null)
                _panelRightStats.Location = new Point(availableWidth - 260, 130);

            // Banner - right side, top aligned
            if (_picBanner != null)
            {
                int bannerX = 350;
                int bannerW = availableWidth - 350 - 280;
                int bannerY = 70;
                int bannerH = 280;
                if (bannerW < 200) bannerW = 200;
                _picBanner.Location = new Point(bannerX, bannerY);
                _picBanner.Size = new Size(bannerW, bannerH);
            }

            // Charts - push below banner
            int chartY = 370;
            int chartWidth = (availableWidth - 20) / 2;
            int chartHeight = 300;

            if (_pieChart != null)
            {
                _pieChart.Location = new Point(Padding.Left, chartY);
                _pieChart.Size = new Size(chartWidth, chartHeight);
            }
            if (_lineChart != null)
            {
                _lineChart.Location = new Point(Padding.Left + chartWidth + 20, chartY);
                _lineChart.Size = new Size(chartWidth, chartHeight);
            }
        }

        private PieChart CreatePieChart()
        {
            var chart = new PieChart
            {
                LegendPosition = LiveChartsCore.Measure.LegendPosition.Right,
                LegendTextSize = 12
            };

            try
            {
                var dt = DataAccess.GetSachByTheLoai();
                var seriesList = new List<ISeries>();
                var colors = new SKColor[]
                {
                    new SKColor(232, 132, 107),
                    new SKColor(143, 188, 143),
                    new SKColor(180, 160, 210),
                    new SKColor(240, 190, 100),
                    new SKColor(100, 180, 220),
                    new SKColor(220, 140, 170)
                };

                int i = 0;
                foreach (DataRow row in dt.Rows)
                {
                    var val = Convert.ToDouble(row["SoLuong"]);
                    var name = row["TenTheLoai"].ToString() ?? "";
                    seriesList.Add(new PieSeries<double>
                    {
                        Values = new double[] { val },
                        Name = name,
                        Fill = new SolidColorPaint(colors[i % colors.Length])
                    });
                    i++;
                }

                chart.Series = seriesList.ToArray();

                var lblChart = new Label
                {
                    Text = "KHO SÁCH THEO THỂ LOẠI",
                    Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                    ForeColor = AppColors.TextPrimary,
                    AutoSize = true,
                    Location = new Point(10, 5)
                };
                chart.Controls.Add(lblChart);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Pie chart error: " + ex.Message); }

            return chart;
        }

        private CartesianChart CreateLineChart()
        {
            var chart = new CartesianChart
            {
                LegendPosition = LiveChartsCore.Measure.LegendPosition.Hidden
            };

            try
            {
                var dt = DataAccess.ExecuteQuery(
                    "SELECT MONTH(pm.NgayMuon) AS Thang, SUM(ctpm.SoLuong) AS SoLuong FROM PhieuMuon pm JOIN ChiTietPhieuMuon ctpm ON pm.MaPhieuMuon=ctpm.MaPhieuMuon WHERE YEAR(pm.NgayMuon)=YEAR(GETDATE()) GROUP BY MONTH(pm.NgayMuon) ORDER BY Thang");

                var labels = new List<string>();
                var values = new List<double>();

                foreach (DataRow row in dt.Rows)
                {
                    int thang = Convert.ToInt32(row["Thang"]);
                    labels.Add($"T{thang}");
                    values.Add(Convert.ToDouble(row["SoLuong"]));
                }

                if (labels.Count == 0)
                {
                    labels.AddRange(new[] { "T1", "T2", "T3", "T4", "T5", "T6" });
                    values.AddRange(new double[] { 0, 0, 0, 0, 0, 0 });
                }

                var separatorPaint = new SolidColorPaint(new SKColor(240, 237, 232));

                chart.XAxes = new[]
                {
                    new Axis
                    {
                        Labels = labels.ToArray(),
                        LabelsRotation = 0,
                        TextSize = 11,
                        LabelsPaint = new SolidColorPaint(new SKColor(136, 136, 136)) { SKTypeface = _vietnameseFont },
                        SeparatorsPaint = separatorPaint
                    }
                };

                chart.YAxes = new[]
                {
                    new Axis
                    {
                        TextSize = 11,
                        LabelsPaint = new SolidColorPaint(new SKColor(136, 136, 136)) { SKTypeface = _vietnameseFont },
                        SeparatorsPaint = separatorPaint
                    }
                };

                chart.Series = new ISeries[]
                {
                    new LineSeries<double>
                    {
                        Values = values.ToArray(),
                        Fill = new SolidColorPaint(new SKColor(232, 132, 107, 40)),
                        Stroke = new SolidColorPaint(new SKColor(232, 132, 107)) { StrokeThickness = 3 },
                        GeometryFill = new SolidColorPaint(new SKColor(232, 132, 107)),
                        GeometryStroke = null,
                        GeometrySize = 10,
                        LineSmoothness = 0.5
                    }
                };

                var lblChart = new Label
                {
                    Text = "XU HƯỚNG MƯỢN SÁCH",
                    Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                    ForeColor = AppColors.TextPrimary,
                    AutoSize = true,
                    Location = new Point(10, 5)
                };
                chart.Controls.Add(lblChart);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Line chart error: " + ex.Message); }

            return chart;
        }
    }
}

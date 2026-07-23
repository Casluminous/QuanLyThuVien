using QuanLyThuVien.Controls;
using QuanLyThuVien.Helpers;

namespace QuanLyThuVien.Forms
{
    public sealed class FormDanhMuc : UserControl
    {
        private const int PageInset = 24;
        private const int CardHeight = 112;
        private const int CardGap = 12;
        private readonly List<TableLayoutPanel> _cardGrids = new();
        private Panel? _scrollHost;
        private TableLayoutPanel? _page;
        private int _columnCount;
        private bool _isBuilding;

        public event Action<string>? NavigationRequested;

        public FormDanhMuc()
        {
            BackColor = AppColors.ContentBg;
            Padding = Padding.Empty;
            AutoScroll = false;
            TabStop = false;
            AccessibleRole = AccessibleRole.Grouping;
            AccessibleName = "Các khu vực quản lý thư viện";
            Load += (_, _) => BuildUI();
            Resize += (_, _) => UpdateResponsiveLayout();
        }

        private void BuildUI()
        {
            if (_isBuilding) return;
            _isBuilding = true;
            try
            {
                ResponsiveUi.DisposeChildren(this);
                _cardGrids.Clear();

                _scrollHost = new Panel
                {
                    Dock = DockStyle.Fill,
                    AutoScroll = true,
                    BackColor = Color.Transparent,
                    Padding = new Padding(PageInset, 16, PageInset, PageInset),
                    AccessibleRole = AccessibleRole.Grouping,
                    AccessibleName = "Danh mục thư viện"
                };

                _page = new TableLayoutPanel
                {
                    ColumnCount = 1,
                    RowCount = 0,
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowOnly,
                    BackColor = Color.Transparent,
                    Margin = Padding.Empty,
                    Padding = Padding.Empty,
                    TabStop = false
                };
                _page.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

                var header = new PageHeader("Danh mục")
                {
                    Dock = DockStyle.Top,
                    Height = 56,
                    Margin = Padding.Empty,
                    TabIndex = 0
                };
                _page.Controls.Add(header, 0, _page.RowCount++);

                var subtitle = new Label
                {
                    Text = "Truy cập nhanh các khu vực quản lý thư viện",
                    Dock = DockStyle.Top,
                    Height = 28,
                    Font = new Font("Segoe UI", 10F),
                    ForeColor = AppColors.TextSecondary,
                    Margin = new Padding(0, 0, 0, 16),
                    TabStop = false
                };
                _page.Controls.Add(subtitle, 0, _page.RowCount++);

                AddGroup("Vận hành", new[]
                {
                    CreateTileSpec("Kho sách", "Tìm kiếm và quản lý đầu sách", AppColors.Info, "Sách"),
                    CreateTileSpec("Mượn trả", "Tạo và theo dõi phiếu mượn", AppColors.Primary, "Phiếu mượn"),
                    CreateTileSpec("Trả sách", "Ghi nhận trả sách và tiền phạt", AppColors.Accent, "Phiếu trả")
                });

                var people = new List<TileSpec>
                {
                    CreateTileSpec("Độc giả", "Quản lý hồ sơ bạn đọc", AppColors.Success, "Độc giả")
                };
                if (Session.IsAdmin)
                    people.Add(CreateTileSpec("Thủ thư", "Quản lý tài khoản nhân viên", AppColors.PrimaryDark, "Thủ thư"));
                AddGroup("Con người", people.ToArray());

                AddGroup("Dữ liệu nền", new[]
                {
                    CreateTileSpec("Thể loại", "Quản lý thể loại sách", AppColors.Info, "Thể loại"),
                    CreateTileSpec("Tác giả", "Quản lý thông tin tác giả", AppColors.Success, "Tác giả"),
                    CreateTileSpec("Nhà xuất bản", "Quản lý nhà xuất bản", AppColors.Accent, "Nhà xuất bản")
                });

                AddGroup("Phân tích", new[]
                {
                    CreateTileSpec("Báo cáo", "Xem báo cáo hoạt động thư viện", AppColors.Primary, "Báo cáo")
                });

                _scrollHost.Controls.Add(_page);
                Controls.Add(_scrollHost);
                _columnCount = GetColumnCount(GetContentWidth());
                UpdateResponsiveLayout();
            }
            finally
            {
                _isBuilding = false;
            }
        }

        private void AddGroup(string title, IReadOnlyList<TileSpec> tiles)
        {
            if (_page == null || tiles.Count == 0) return;

            var group = new TableLayoutPanel
            {
                ColumnCount = 1,
                RowCount = 2,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowOnly,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 24),
                Padding = Padding.Empty,
                TabStop = false,
                AccessibleRole = AccessibleRole.Grouping,
                AccessibleName = title
            };
            group.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            var heading = new Panel
            {
                Height = 32,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Margin = Padding.Empty,
                TabStop = false
            };
            heading.Controls.Add(new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 24,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = AppColors.TextPrimary,
                Margin = Padding.Empty,
                TabStop = false
            });
            heading.Controls.Add(new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = AppColors.Border,
                TabStop = false
            });
            group.Controls.Add(heading, 0, 0);

            var grid = new TableLayoutPanel
            {
                ColumnCount = 1,
                RowCount = 1,
                AutoSize = false,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 8, 0, 0),
                Padding = Padding.Empty,
                TabStop = false
            };
            grid.Tag = tiles;
            group.Controls.Add(grid, 0, 1);
            _cardGrids.Add(grid);
            _page.Controls.Add(group, 0, _page.RowCount++);
        }

        private void UpdateResponsiveLayout()
        {
            if (_page == null || _scrollHost == null) return;

            int contentWidth = GetContentWidth();
            _columnCount = GetColumnCount(contentWidth);
            _page.Width = Math.Max(320, contentWidth);
            foreach (var grid in _cardGrids)
                ApplyGridLayout(grid, _columnCount, _page.Width);
        }

        private void ApplyGridLayout(TableLayoutPanel grid, int columns, int width)
        {
            if (grid.Tag is not IReadOnlyList<TileSpec> tiles) return;

            grid.SuspendLayout();
            try
            {
                ResponsiveUi.DisposeChildren(grid);
                grid.ColumnStyles.Clear();
                grid.RowStyles.Clear();
                grid.ColumnCount = Math.Max(1, Math.Min(columns, tiles.Count));
                grid.RowCount = (int)Math.Ceiling(tiles.Count / (double)grid.ColumnCount);
                grid.Width = Math.Max(280, width);
                grid.Height = grid.RowCount * (CardHeight + CardGap) - CardGap;

                for (int column = 0; column < grid.ColumnCount; column++)
                    grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / grid.ColumnCount));
                for (int row = 0; row < grid.RowCount; row++)
                    grid.RowStyles.Add(new RowStyle(SizeType.Absolute, CardHeight + CardGap));

                for (int i = 0; i < tiles.Count; i++)
                {
                    var spec = tiles[i];
                    var tile = new NavigationTile
                    {
                        Title = spec.Title,
                        Description = spec.Description,
                        AccentColor = spec.AccentColor,
                        TargetTag = spec.TargetTag,
                        Dock = DockStyle.Fill,
                        Margin = new Padding(0, 0, i % grid.ColumnCount == grid.ColumnCount - 1 ? 0 : CardGap, CardGap),
                        TabIndex = i,
                        AccessibleName = spec.Title,
                        AccessibleDescription = spec.Description + ". Mở " + spec.Title + "."
                    };
                    tile.Click += (_, _) => NavigationRequested?.Invoke(tile.TargetTag);
                    grid.Controls.Add(tile, i % grid.ColumnCount, i / grid.ColumnCount);
                }
            }
            finally
            {
                grid.ResumeLayout(true);
            }
        }

        private int GetContentWidth()
        {
            int hostWidth = _scrollHost?.ClientSize.Width ?? ClientSize.Width;
            return Math.Max(320, hostWidth - PageInset * 2 - 4);
        }

        private static int GetColumnCount(int width)
        {
            return width >= 920 ? 3 : width >= 620 ? 2 : 1;
        }

        private static TileSpec CreateTileSpec(string title, string description, Color accentColor, string targetTag)
            => new(title, description, accentColor, targetTag);

        private readonly record struct TileSpec(string Title, string Description, Color AccentColor, string TargetTag);
    }
}

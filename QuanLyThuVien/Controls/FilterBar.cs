using QuanLyThuVien.Helpers;

namespace QuanLyThuVien.Controls;

public sealed class FilterBar : UserControl
{
    private const int ToolbarHeight = 36;
    private readonly FlowLayoutPanel _layout;

    public ModernTextBox SearchBox { get; }
    public ModernButton ClearButton { get; }

    public event EventHandler? FilterChanged;

    internal int ToolbarPreferredWidth => _layout.Controls.Cast<Control>()
        .Sum(control => control.Width + control.Margin.Horizontal);

    public FilterBar(string placeholder = "Tìm kiếm...")
    {
        Dock = DockStyle.Top;
        Height = 52;
        BackColor = Color.Transparent;
        // Keep the 52px filter row, but align its 36px controls closer to the
        // header and leave the extra breathing room above the table.
        Padding = new Padding(0, 4, 8, 12);
        AccessibleRole = AccessibleRole.Grouping;
        AccessibleName = "Bộ lọc dữ liệu";

        _layout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = false,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        SearchBox = new ModernTextBox
        {
            Placeholder = placeholder,
            Size = new Size(280, 36),
            Margin = new Padding(0, 0, 8, 0),
            AccessibleName = placeholder
        };
        ClearButton = new ModernButton
        {
            Text = "Xóa lọc",
            Size = new Size(82, 36),
            Margin = new Padding(0, 0, 8, 0),
            BaseColor = AppColors.CardBg,
            HoverColor = AppColors.HoverSurface,
            PressedColor = AppColors.SelectedSurface,
            TextColor = AppColors.PrimaryDark,
            BorderRadius = 10,
            AccessibleName = "Xóa tất cả bộ lọc"
        };
        SearchBox.TextChanged += (_, _) => FilterChanged?.Invoke(this, EventArgs.Empty);
        ClearButton.Click += (_, _) =>
        {
            SearchBox.Text = string.Empty;
            foreach (ModernComboBox control in _layout.Controls.OfType<ModernComboBox>()) control.SelectedIndex = 0;
            FilterChanged?.Invoke(this, EventArgs.Empty);
        };
        _layout.Controls.Add(SearchBox);
        _layout.Controls.Add(ClearButton);
        Controls.Add(_layout);
    }

    public void AddFilter(ModernComboBox combo, string accessibleName)
    {
        combo.Margin = new Padding(0, 0, 8, 0);
        combo.AccessibleName = accessibleName;
        combo.SelectedIndexChanged += (_, _) => FilterChanged?.Invoke(this, EventArgs.Empty);
        _layout.Controls.Add(combo);
    }

    internal void UseToolbarLayout()
    {
        Dock = DockStyle.None;
        Height = ToolbarHeight;
        Padding = Padding.Empty;
        Margin = Padding.Empty;
    }
}

using QuanLyThuVien.Chat.Contracts;
using QuanLyThuVien.Helpers;

namespace QuanLyThuVien.Controls;

public sealed class BookSuggestionCard : UserControl
{
    public event EventHandler<int>? BookClicked;

    public BookSuggestionCard(BookSuggestion book)
    {
        AutoSize = true;
        Width = 344;
        Padding = new Padding(12, 8, 12, 8);
        Margin = new Padding(4, 2, 4, 6);
        BackColor = AppColors.CardBg;
        BorderStyle = BorderStyle.FixedSingle;
        Cursor = Cursors.Hand;
        AccessibleRole = AccessibleRole.Link;
        AccessibleName = $"Mở chi tiết sách {book.TenSach}";

        var title = new Label { Text = book.TenSach, AutoSize = true, Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = AppColors.PrimaryDark, MaximumSize = new Size(318, 0) };
        var meta = new Label { Text = $"{book.TenTacGia} · {book.TenTheLoai}", AutoSize = true, Font = new Font("Segoe UI", 9F), ForeColor = AppColors.TextSecondary, MaximumSize = new Size(318, 0) };
        var stock = new Label { Text = book.SoLuong > 0 ? $"Còn {book.SoLuong} cuốn" : "Hết sách", AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = book.SoLuong > 0 ? AppColors.Success : AppColors.Danger };
        var price = new Label { Text = $"{book.GiaTien:N0}đ", AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = AppColors.Primary };
        var row = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = false, BackColor = Color.Transparent, Padding = Padding.Empty, Margin = Padding.Empty };
        row.Controls.Add(stock);
        row.Controls.Add(new Label { Text = "  ·  ", AutoSize = true, ForeColor = AppColors.TextMuted });
        row.Controls.Add(price);
        var stack = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, BackColor = Color.Transparent, Padding = Padding.Empty, Margin = Padding.Empty };
        stack.Controls.Add(title);
        stack.Controls.Add(meta);
        stack.Controls.Add(row);
        Controls.Add(stack);
        AttachClick(this, book.MaSach);
    }

    private void AttachClick(Control control, int maSach)
    {
        control.Click += (_, _) => BookClicked?.Invoke(this, maSach);
        foreach (Control child in control.Controls) AttachClick(child, maSach);
    }
}

using QuanLyThuVien.Helpers;

namespace QuanLyThuVien.Controls;

public sealed class ChatMessageControl : UserControl
{
    private readonly Label _messageLabel;
    private readonly bool _isUser;

    public ChatMessageControl(bool isUser, string text = "")
    {
        _isUser = isUser;
        AutoSize = true;
        Margin = new Padding(4, 4, 4, 8);
        Padding = new Padding(12, 8, 12, 8);
        BackColor = isUser ? AppColors.Primary : AppColors.SelectedSurface;
        ForeColor = isUser ? Color.White : AppColors.TextPrimary;
        AccessibleRole = AccessibleRole.Text;
        AccessibleName = isUser ? "Tin nhắn của bạn" : "Tin nhắn của trợ lý";

        _messageLabel = new Label
        {
            AutoSize = true,
            Text = text,
            Font = new Font("Segoe UI", 10F),
            ForeColor = ForeColor,
            MaximumSize = new Size(330, 0),
            BackColor = Color.Transparent
        };
        Controls.Add(_messageLabel);
        Resize += (_, _) => UpdateLabelWidth();
        UpdateLabelWidth();
    }

    public string MessageText => _messageLabel.Text;

    public void AppendText(string text)
    {
        _messageLabel.Text += text;
        UpdateLabelWidth();
    }

    private void UpdateLabelWidth()
    {
        _messageLabel.MaximumSize = new Size(Math.Max(160, Math.Min(330, Width - Padding.Horizontal)), 0);
        Height = _messageLabel.Height + Padding.Vertical;
    }
}

using QuanLyThuVien.Helpers;

namespace QuanLyThuVien.Controls;

public sealed class ChatLauncherButton : ModernButton
{
    public ChatLauncherButton()
    {
        Text = "Trợ lý";
        Size = new Size(120, 44);
        BaseColor = AppColors.Primary;
        HoverColor = AppColors.PrimaryDark;
        PressedColor = AppColors.SidebarBg;
        BorderRadius = 14;
        Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        AccessibleName = "Mở trợ lý thư viện";
        TabIndex = 0;
    }
}

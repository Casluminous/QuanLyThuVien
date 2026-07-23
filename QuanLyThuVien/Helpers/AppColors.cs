using System.Drawing;

namespace QuanLyThuVien.Helpers
{
    public static class AppColors
    {
        // Library Teal light theme — semantic tokens shared by every screen.
        public static readonly Color Primary = Color.FromArgb(15, 118, 110);       // #0F766E
        public static readonly Color PrimaryDark = Color.FromArgb(17, 94, 89);     // #115E59
        public static readonly Color PrimaryLight = Color.FromArgb(204, 251, 241); // #CCFBF1
        public static readonly Color Accent = Color.FromArgb(217, 119, 6);         // #D97706
        public static readonly Color Success = Color.FromArgb(21, 128, 61);        // #15803D
        public static readonly Color SuccessDark = Color.FromArgb(22, 101, 52);   // #166534
        public static readonly Color Warning = Color.FromArgb(180, 83, 9);        // #B45309
        public static readonly Color Danger = Color.FromArgb(220, 38, 38);        // #DC2626
        public static readonly Color DangerDark = Color.FromArgb(185, 28, 28);    // #B91C1C
        public static readonly Color Info = Color.FromArgb(3, 105, 161);           // #0369A1
        public static readonly Color InfoDark = Color.FromArgb(7, 89, 133);        // #075985

        // Stat Card Accent Colors
        public static readonly Color CardBlue = Color.FromArgb(3, 105, 161);
        public static readonly Color CardRed = Danger;
        public static readonly Color CardGreen = Success;
        public static readonly Color CardOrange = Accent;
        public static readonly Color CardPurple = Color.FromArgb(109, 40, 217);

        // Sidebar - deep teal navigation surface
        public static readonly Color SidebarBg = Color.FromArgb(18, 59, 57);       // #123B39
        public static readonly Color SidebarHover = Color.FromArgb(21, 78, 74);    // #154E4A
        public static readonly Color SidebarActive = Primary;
        public static readonly Color SidebarText = Color.FromArgb(204, 251, 241);  // #CCFBF1
        public static readonly Color SidebarTextActive = Color.White;

        // Content areas - light teal-tinted surfaces
        public static readonly Color HeaderBg = Color.White;
        public static readonly Color ContentBg = Color.FromArgb(246, 250, 249);     // #F6FAF9
        public static readonly Color CardBg = Color.White;
        public static readonly Color Border = Color.FromArgb(216, 229, 226);       // #D8E5E2
        public static readonly Color HoverSurface = Color.FromArgb(240, 253, 250); // #F0FDFA
        public static readonly Color AlternateSurface = Color.FromArgb(248, 252, 251); // #F8FCFB
        public static readonly Color SelectedSurface = Color.FromArgb(217, 243, 239); // #D9F3EF
        public static readonly Color Focus = Color.FromArgb(20, 184, 166);         // #14B8A6
        public static readonly Color Shadow = Color.FromArgb(18, 23, 65, 62);

        // Operational Workbench surfaces used by Login and Dashboard only.
        // They keep Library Teal intact while making the two entry screens feel warmer.
        public static readonly Color WorkbenchBg = Color.FromArgb(244, 247, 243);          // #F4F7F3
        public static readonly Color WorkbenchMuted = Color.FromArgb(232, 242, 237);       // #E8F2ED
        public static readonly Color WorkbenchSurface = Color.FromArgb(255, 253, 248);     // #FFFDF8

        // Typography
        public static readonly Color TextPrimary = Color.FromArgb(23, 48, 45);      // #17302D
        public static readonly Color TextSecondary = Color.FromArgb(91, 112, 108);  // #5B706C
        public static readonly Color TextMuted = Color.FromArgb(107, 124, 120);     // #6B7C78
        public static readonly Color TextLight = Color.White;
    }
}

using QuanLyThuVien.Models;

namespace QuanLyThuVien.Helpers
{
    public static class Session
    {
        public static NhanVien? CurrentUser { get; set; }
        public static bool IsAdmin => CurrentUser?.VaiTro == "Admin";
    }
}

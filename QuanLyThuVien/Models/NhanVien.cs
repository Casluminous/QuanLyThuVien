namespace QuanLyThuVien.Models
{
    public class NhanVien
    {
        public int MaNV { get; set; }
        public string HoTen { get; set; } = "";
        public string TenDangNhap { get; set; } = "";
        public string MatKhau { get; set; } = "";
        public string VaiTro { get; set; } = "NhanVien";
        public bool TrangThai { get; set; } = true;
    }
}

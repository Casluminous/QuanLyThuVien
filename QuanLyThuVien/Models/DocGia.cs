namespace QuanLyThuVien.Models
{
    public class DocGia
    {
        public int MaDG { get; set; }
        public string HoTen { get; set; } = "";
        public DateTime NgaySinh { get; set; }
        public string GioiTinh { get; set; } = "Nam";
        public string SoDienThoai { get; set; } = "";
        public string Email { get; set; } = "";
        public DateTime NgayLapThe { get; set; }
        public DateTime HanSuDung { get; set; }
        public bool TrangThai { get; set; } = true;
    }
}

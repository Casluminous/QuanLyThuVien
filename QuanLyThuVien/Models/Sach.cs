namespace QuanLyThuVien.Models
{
    public class Sach
    {
        public int MaSach { get; set; }
        public string TenSach { get; set; } = "";
        public string MaISBN { get; set; } = "";
        public int MaTL { get; set; }
        public int MaTG { get; set; }
        public int MaNXB { get; set; }
        public int NamXB { get; set; }
        public int SoLuong { get; set; }
        public decimal GiaTien { get; set; }
        public string MoTa { get; set; } = "";
        public string HinhAnh { get; set; } = "";

        public string TenTheLoai { get; set; } = "";
        public string TenTacGia { get; set; } = "";
        public string TenNXB { get; set; } = "";
    }
}

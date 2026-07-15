namespace QuanLyThuVien.Models
{
    public class PhieuMuon
    {
        public int MaPhieuMuon { get; set; }
        public int MaDG { get; set; }
        public int MaNV { get; set; }
        public DateTime NgayMuon { get; set; }
        public DateTime HanTra { get; set; }
        public string TrangThai { get; set; } = "Đang mượn";

        public string TenDocGia { get; set; } = "";
        public string TenNhanVien { get; set; } = "";
    }

    public class ChiTietPhieuMuon
    {
        public int MaPhieuMuon { get; set; }
        public int MaSach { get; set; }
        public int SoLuong { get; set; }
        public DateTime? NgayTra { get; set; }
        public decimal TienPhat { get; set; }

        public string TenSach { get; set; } = "";
    }
}

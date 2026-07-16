using System.Data;
using System.Data.SqlClient;
using QuanLyThuVien.Models;

namespace QuanLyThuVien.Data
{
    public static class DataAccess
    {
        private static string connectionString = InitConnectionString();

        private static string InitConnectionString()
        {
            try
            {
                var cs = System.Configuration.ConfigurationManager.ConnectionStrings["QuanLyThuVien"];
                if (cs != null && !string.IsNullOrEmpty(cs.ConnectionString))
                    return cs.ConnectionString;
            }
            catch { }

            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "QuanLyThuVien.dll.config");
                if (File.Exists(configPath))
                {
                    var map = new System.Configuration.ExeConfigurationFileMap { ExeConfigFilename = configPath };
                    var config = System.Configuration.ConfigurationManager.OpenMappedExeConfiguration(map, System.Configuration.ConfigurationUserLevel.None);
                    var cs = config.ConnectionStrings.ConnectionStrings["QuanLyThuVien"];
                    if (cs != null && !string.IsNullOrEmpty(cs.ConnectionString))
                        return cs.ConnectionString;
                }
            }
            catch { }

            Console.WriteLine("[WARNING] No connection string found, using default.");
            return @"Server=.\SQLEXPRESS;Database=QuanLyThuVien;Trusted_Connection=True;TrustServerCertificate=True;";
        }

        public static SqlConnection GetConnection()
        {
            return new SqlConnection(connectionString);
        }

        public static DataTable ExecuteQuery(string query, params SqlParameter[] parameters)
        {
            using (var conn = GetConnection())
            using (var cmd = new SqlCommand(query, conn))
            {
                if (parameters != null)
                    cmd.Parameters.AddRange(parameters);
                var dt = new DataTable();
                var adapter = new SqlDataAdapter(cmd);
                adapter.Fill(dt);
                return dt;
            }
        }

        public static int ExecuteNonQuery(string query, params SqlParameter[] parameters)
        {
            using (var conn = GetConnection())
            using (var cmd = new SqlCommand(query, conn))
            {
                if (parameters != null)
                    cmd.Parameters.AddRange(parameters);
                conn.Open();
                return cmd.ExecuteNonQuery();    
            }
        }

        public static object? ExecuteScalar(string query, params SqlParameter[]? parameters)
        {
            using (var conn = GetConnection())
            using (var cmd = new SqlCommand(query, conn))
            {
                if (parameters != null)
                    cmd.Parameters.AddRange(parameters);
                conn.Open();
                return cmd.ExecuteScalar();
            }
        }

        // ---- NhanVien ----
        public static NhanVien? DangNhap(string tenDangNhap, string matKhau)
        {
            var dt = ExecuteQuery(
                "SELECT MaNV, HoTen, TenDangNhap, MatKhau, VaiTro, TrangThai FROM NhanVien WHERE TenDangNhap=@tdn AND TrangThai=1",
                new SqlParameter("@tdn", tenDangNhap));
            if (dt.Rows.Count == 0) return null;
            var row = dt.Rows[0];
            string storedHash = row["MatKhau"].ToString()!;
            if (!Helpers.PasswordHelper.VerifyPassword(matKhau, storedHash))
                return null;

            // Rehash legacy SHA-256 to PBKDF2 on successful login
            if (!storedHash.StartsWith("PBKDF2$"))
            {
                string newHash = Helpers.PasswordHelper.HashPassword(matKhau);
                ExecuteNonQuery("UPDATE NhanVien SET MatKhau=@mk WHERE MaNV=@ma",
                    new SqlParameter("@mk", newHash),
                    new SqlParameter("@ma", (int)row["MaNV"]));
            }

            return new NhanVien
            {
                MaNV = (int)row["MaNV"],
                HoTen = row["HoTen"].ToString()!,
                TenDangNhap = row["TenDangNhap"].ToString()!,
                VaiTro = row["VaiTro"].ToString()!,
                TrangThai = (bool)row["TrangThai"]
            };
        }

        public static DataTable GetAllNhanVien() =>
            ExecuteQuery("SELECT * FROM NhanVien");

        public static int InsertNhanVien(string hoTen, string tenDN, string matKhau, string vaiTro)
        {
            string hash = Helpers.PasswordHelper.HashPassword(matKhau);
            return ExecuteNonQuery(
                "INSERT INTO NhanVien(HoTen,TenDangNhap,MatKhau,VaiTro) VALUES(@ten,@tdn,@mk,@vt)",
                new SqlParameter("@ten", hoTen), new SqlParameter("@tdn", tenDN),
                new SqlParameter("@mk", hash), new SqlParameter("@vt", vaiTro));
        }

        public static int UpdateNhanVien(int maNV, string hoTen, string tenDN, string vaiTro, bool trangThai)
        {
            return ExecuteNonQuery(
                "UPDATE NhanVien SET HoTen=@ten,TenDangNhap=@tdn,VaiTro=@vt,TrangThai=@tt WHERE MaNV=@ma",
                new SqlParameter("@ten", hoTen), new SqlParameter("@tdn", tenDN),
                new SqlParameter("@vt", vaiTro), new SqlParameter("@tt", trangThai),
                new SqlParameter("@ma", maNV));
        }

        public static int UpdateNhanVienPassword(int maNV, string matKhau)
        {
            string hash = Helpers.PasswordHelper.HashPassword(matKhau);
            return ExecuteNonQuery("UPDATE NhanVien SET MatKhau=@mk WHERE MaNV=@ma",
                new SqlParameter("@mk", hash), new SqlParameter("@ma", maNV));
        }

        public static int CountActiveAdmins() =>
            Convert.ToInt32(ExecuteScalar("SELECT COUNT(*) FROM NhanVien WHERE VaiTro='Admin' AND TrangThai=1"));

        public static int DeleteNhanVien(int maNV) =>
            ExecuteNonQuery("DELETE FROM NhanVien WHERE MaNV=@ma", new SqlParameter("@ma", maNV));

        // ---- TheLoai ----
        public static DataTable GetAllTheLoai() =>
            ExecuteQuery("SELECT * FROM TheLoai");

        public static int InsertTheLoai(string tenTL) =>
            ExecuteNonQuery("INSERT INTO TheLoai(TenTheLoai) VALUES(@ten)",
                new SqlParameter("@ten", tenTL));

        public static int UpdateTheLoai(int maTL, string tenTL) =>
            ExecuteNonQuery("UPDATE TheLoai SET TenTheLoai=@ten WHERE MaTL=@ma",
                new SqlParameter("@ten", tenTL), new SqlParameter("@ma", maTL));

        public static int DeleteTheLoai(int maTL) =>
            ExecuteNonQuery("DELETE FROM TheLoai WHERE MaTL=@ma",
                new SqlParameter("@ma", maTL));

        // ---- TacGia ----
        public static DataTable GetAllTacGia() =>
            ExecuteQuery("SELECT * FROM TacGia");

        public static int InsertTacGia(string tenTG, string quocTia, string ghiChu) =>
            ExecuteNonQuery("INSERT INTO TacGia(TenTG,QuocTia,GhiChu) VALUES(@ten,@qt,@gc)",
                new SqlParameter("@ten", tenTG), new SqlParameter("@qt", quocTia), new SqlParameter("@gc", ghiChu));

        public static int UpdateTacGia(int maTG, string tenTG, string quocTia, string ghiChu) =>
            ExecuteNonQuery("UPDATE TacGia SET TenTG=@ten,QuocTia=@qt,GhiChu=@gc WHERE MaTG=@ma",
                new SqlParameter("@ten", tenTG), new SqlParameter("@qt", quocTia), new SqlParameter("@gc", ghiChu), new SqlParameter("@ma", maTG));

        public static int DeleteTacGia(int maTG) =>
            ExecuteNonQuery("DELETE FROM TacGia WHERE MaTG=@ma",
                new SqlParameter("@ma", maTG));

        // ---- NhaXuatBan ----
        public static DataTable GetAllNXB() =>
            ExecuteQuery("SELECT * FROM NhaXuatBan");

        public static int InsertNXB(string tenNXB, string diaChi, string sdt) =>
            ExecuteNonQuery("INSERT INTO NhaXuatBan(TenNXB,DiaChi,SoDienThoai) VALUES(@ten,@dc,@sdt)",
                new SqlParameter("@ten", tenNXB), new SqlParameter("@dc", diaChi), new SqlParameter("@sdt", sdt));

        public static int UpdateNXB(int maNXB, string tenNXB, string diaChi, string sdt) =>
            ExecuteNonQuery("UPDATE NhaXuatBan SET TenNXB=@ten,DiaChi=@dc,SoDienThoai=@sdt WHERE MaNXB=@ma",
                new SqlParameter("@ten", tenNXB), new SqlParameter("@dc", diaChi), new SqlParameter("@sdt", sdt), new SqlParameter("@ma", maNXB));

        public static int DeleteNXB(int maNXB) =>
            ExecuteNonQuery("DELETE FROM NhaXuatBan WHERE MaNXB=@ma",
                new SqlParameter("@ma", maNXB));

        // ---- Sach ----
        public static DataTable GetAllSach() =>
            ExecuteQuery(@"SELECT s.*, tl.TenTheLoai, tg.TenTG AS TenTacGia, nxb.TenNXB
                           FROM Sach s
                           LEFT JOIN TheLoai tl ON s.MaTL=tl.MaTL
                           LEFT JOIN TacGia tg ON s.MaTG=tg.MaTG
                           LEFT JOIN NhaXuatBan nxb ON s.MaNXB=nxb.MaNXB");

        public static int InsertSach(Sach s) =>
            ExecuteNonQuery(@"INSERT INTO Sach(TenSach,MaISBN,MaTL,MaTG,MaNXB,NamXB,SoLuong,GiaTien,MoTa,HinhAnh)
                             VALUES(@ten,@isbn,@malt,@matg,@manxb,@namxb,@sl,@gia,@mota,@ha)",
                new SqlParameter("@ten", s.TenSach), new SqlParameter("@isbn", s.MaISBN),
                new SqlParameter("@malt", s.MaTL), new SqlParameter("@matg", s.MaTG),
                new SqlParameter("@manxb", s.MaNXB), new SqlParameter("@namxb", s.NamXB),
                new SqlParameter("@sl", s.SoLuong), new SqlParameter("@gia", s.GiaTien),
                new SqlParameter("@mota", s.MoTa), new SqlParameter("@ha", s.HinhAnh));

        public static int UpdateSach(Sach s) =>
            ExecuteNonQuery(@"UPDATE Sach SET TenSach=@ten,MaISBN=@isbn,MaTL=@malt,MaTG=@matg,
                             MaNXB=@manxb,NamXB=@namxb,SoLuong=@sl,GiaTien=@gia,MoTa=@mota,HinhAnh=@ha
                             WHERE MaSach=@ma",
                new SqlParameter("@ten", s.TenSach), new SqlParameter("@isbn", s.MaISBN),
                new SqlParameter("@malt", s.MaTL), new SqlParameter("@matg", s.MaTG),
                new SqlParameter("@manxb", s.MaNXB), new SqlParameter("@namxb", s.NamXB),
                new SqlParameter("@sl", s.SoLuong), new SqlParameter("@gia", s.GiaTien),
                new SqlParameter("@mota", s.MoTa), new SqlParameter("@ha", s.HinhAnh),
                new SqlParameter("@ma", s.MaSach));

        public static int UpdateSachImage(int maSach, string hinhAnh) =>
            ExecuteNonQuery("UPDATE Sach SET HinhAnh=@ha WHERE MaSach=@ma",
                new SqlParameter("@ha", hinhAnh),
                new SqlParameter("@ma", maSach));

        public static int DeleteSach(int maSach) =>
            ExecuteNonQuery("DELETE FROM Sach WHERE MaSach=@ma", new SqlParameter("@ma", maSach));

        // ---- DocGia ----
        public static DataTable GetAllDocGia() =>
            ExecuteQuery("SELECT * FROM DocGia");

        public static int InsertDocGia(DocGia dg) =>
            ExecuteNonQuery(@"INSERT INTO DocGia(HoTen,NgaySinh,GioiTinh,SoDienThoai,Email,NgayLapThe,HanSuDung,TrangThai)
                             VALUES(@ten,@ns,@gt,@sdt,@email,@nlt,@hsd,@tt)",
                new SqlParameter("@ten", dg.HoTen), new SqlParameter("@ns", dg.NgaySinh),
                new SqlParameter("@gt", dg.GioiTinh), new SqlParameter("@sdt", dg.SoDienThoai),
                new SqlParameter("@email", dg.Email), new SqlParameter("@nlt", dg.NgayLapThe),
                new SqlParameter("@hsd", dg.HanSuDung), new SqlParameter("@tt", dg.TrangThai));

        public static int UpdateDocGia(DocGia dg) =>
            ExecuteNonQuery(@"UPDATE DocGia SET HoTen=@ten,NgaySinh=@ns,GioiTinh=@gt,SoDienThoai=@sdt,
                             Email=@email,NgayLapThe=@nlt,HanSuDung=@hsd,TrangThai=@tt WHERE MaDG=@ma",
                new SqlParameter("@ten", dg.HoTen), new SqlParameter("@ns", dg.NgaySinh),
                new SqlParameter("@gt", dg.GioiTinh), new SqlParameter("@sdt", dg.SoDienThoai),
                new SqlParameter("@email", dg.Email), new SqlParameter("@nlt", dg.NgayLapThe),
                new SqlParameter("@hsd", dg.HanSuDung), new SqlParameter("@tt", dg.TrangThai),
                new SqlParameter("@ma", dg.MaDG));

        public static int DeleteDocGia(int maDG) =>
            ExecuteNonQuery("DELETE FROM DocGia WHERE MaDG=@ma", new SqlParameter("@ma", maDG));

        public static DataTable GetBorrowEligibleReaders() =>
            ExecuteQuery("SELECT * FROM DocGia WHERE TrangThai=1 AND HanSuDung>=CAST(GETDATE() AS DATE)");

        // ---- PhieuMuon ----
        public static DataTable GetAllPhieuMuon() =>
            ExecuteQuery(@"SELECT pm.*, dg.HoTen AS TenDocGia, nv.HoTen AS TenNhanVien
                           FROM PhieuMuon pm
                           LEFT JOIN DocGia dg ON pm.MaDG=dg.MaDG
                           LEFT JOIN NhanVien nv ON pm.MaNV=nv.MaNV
                           ORDER BY pm.NgayMuon DESC");

        public static DataTable GetChiTietPhieuMuon(int maPM) =>
            ExecuteQuery(@"SELECT ctpm.*, s.TenSach
                           FROM ChiTietPhieuMuon ctpm
                           LEFT JOIN Sach s ON ctpm.MaSach=s.MaSach
                           WHERE ctpm.MaPhieuMuon=@ma",
                new SqlParameter("@ma", maPM));

        public static int InsertPhieuMuon(PhieuMuon pm)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        using (var cmd = new SqlCommand(@"INSERT INTO PhieuMuon(MaDG,MaNV,NgayMuon,HanTra,TrangThai)
                                                         VALUES(@madg,@manv,@nm,@ht,@tt); SELECT SCOPE_IDENTITY();", conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@madg", pm.MaDG);
                            cmd.Parameters.AddWithValue("@manv", pm.MaNV);
                            cmd.Parameters.AddWithValue("@nm", pm.NgayMuon);
                            cmd.Parameters.AddWithValue("@ht", pm.HanTra);
                            cmd.Parameters.AddWithValue("@tt", pm.TrangThai);
                            int maPM = Convert.ToInt32(cmd.ExecuteScalar());

                            tran.Commit();
                            return maPM;
                        }
                    }
                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                }
            }
        }

        public static bool InsertPhieuMuonFull(PhieuMuon pm, List<(int maSach, int soLuong)> chiTiet, out string? failureReason)
        {
            failureReason = null;
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        using (var cmdReader = new SqlCommand(@"SELECT 1
                            FROM DocGia WITH (UPDLOCK, HOLDLOCK)
                            WHERE MaDG=@ma AND TrangThai=1 AND HanSuDung>=CAST(GETDATE() AS DATE)", conn, tran))
                        {
                            cmdReader.Parameters.AddWithValue("@ma", pm.MaDG);
                            if (cmdReader.ExecuteScalar() == null)
                            {
                                failureReason = "Thẻ độc giả đã hết hạn hoặc không còn hoạt động.";
                                tran.Rollback();
                                return false;
                            }
                        }

                        int maPM;
                        using (var cmd = new SqlCommand(@"INSERT INTO PhieuMuon(MaDG,MaNV,NgayMuon,HanTra,TrangThai)
                                                         VALUES(@madg,@manv,@nm,@ht,@tt); SELECT SCOPE_IDENTITY();", conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@madg", pm.MaDG);
                            cmd.Parameters.AddWithValue("@manv", pm.MaNV);
                            cmd.Parameters.AddWithValue("@nm", pm.NgayMuon);
                            cmd.Parameters.AddWithValue("@ht", pm.HanTra);
                            cmd.Parameters.AddWithValue("@tt", pm.TrangThai);
                            maPM = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        foreach (var (maSach, soLuong) in chiTiet)
                        {
                            using (var cmdStock = new SqlCommand("UPDATE Sach SET SoLuong=SoLuong-@sl WHERE MaSach=@ma AND SoLuong>=@sl", conn, tran))
                            {
                                cmdStock.Parameters.AddWithValue("@sl", soLuong);
                                cmdStock.Parameters.AddWithValue("@ma", maSach);
                                int affected = cmdStock.ExecuteNonQuery();
                                if (affected == 0)
                                {
                                    failureReason = "Không đủ tồn kho cho một hoặc nhiều sách.";
                                    tran.Rollback();
                                    return false;
                                }
                            }

                            using (var cmdDetail = new SqlCommand(@"INSERT INTO ChiTietPhieuMuon(MaPhieuMuon,MaSach,SoLuong)
                                                                   VALUES(@mam,@mas,@sl)", conn, tran))
                            {
                                cmdDetail.Parameters.AddWithValue("@mam", maPM);
                                cmdDetail.Parameters.AddWithValue("@mas", maSach);
                                cmdDetail.Parameters.AddWithValue("@sl", soLuong);
                                cmdDetail.ExecuteNonQuery();
                            }
                        }

                        tran.Commit();
                        return true;
                    }
                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                }
            }
        }

        public static bool TraSach(int maPM, int maSach, decimal tienPhat, int soLuong)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        using (var cmd1 = new SqlCommand(@"UPDATE ChiTietPhieuMuon SET NgayTra=GETDATE(), TienPhat=@tp
                                                           WHERE MaPhieuMuon=@mam AND MaSach=@mas AND NgayTra IS NULL", conn, tran))
                        {
                            cmd1.Parameters.AddWithValue("@tp", tienPhat);
                            cmd1.Parameters.AddWithValue("@mam", maPM);
                            cmd1.Parameters.AddWithValue("@mas", maSach);
                            int affected = cmd1.ExecuteNonQuery();
                            if (affected == 0) { tran.Rollback(); return false; }
                        }
                        using (var cmd2 = new SqlCommand("UPDATE Sach SET SoLuong=SoLuong+@sl WHERE MaSach=@ma", conn, tran))
                        {
                            cmd2.Parameters.AddWithValue("@sl", soLuong);
                            cmd2.Parameters.AddWithValue("@ma", maSach);
                            cmd2.ExecuteNonQuery();
                        }
                        tran.Commit();
                        return true;
                    }
                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                }
            }
        }

        public static bool TraNhieuSach(int maPM, List<(int maSach, int soLuong, decimal tienPhat)> items)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        foreach (var (maSach, soLuong, tienPhat) in items)
                        {
                            using (var cmd1 = new SqlCommand(@"UPDATE ChiTietPhieuMuon SET NgayTra=GETDATE(), TienPhat=@tp
                                                               WHERE MaPhieuMuon=@mam AND MaSach=@mas AND NgayTra IS NULL", conn, tran))
                            {
                                cmd1.Parameters.AddWithValue("@tp", tienPhat);
                                cmd1.Parameters.AddWithValue("@mam", maPM);
                                cmd1.Parameters.AddWithValue("@mas", maSach);
                                int affected = cmd1.ExecuteNonQuery();
                                if (affected == 0) { tran.Rollback(); return false; }
                            }
                            using (var cmd2 = new SqlCommand("UPDATE Sach SET SoLuong=SoLuong+@sl WHERE MaSach=@ma", conn, tran))
                            {
                                cmd2.Parameters.AddWithValue("@sl", soLuong);
                                cmd2.Parameters.AddWithValue("@ma", maSach);
                                int affected = cmd2.ExecuteNonQuery();
                                if (affected == 0) { tran.Rollback(); return false; }
                            }
                        }
                        using (var cmd3 = new SqlCommand("UPDATE PhieuMuon SET TrangThai=N'Đã trả' WHERE MaPhieuMuon=@ma", conn, tran))
                        {
                            cmd3.Parameters.AddWithValue("@ma", maPM);
                            cmd3.ExecuteNonQuery();
                        }
                        tran.Commit();
                        return true;
                    }
                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                }
            }
        }

        public static int CapNhatTrangThaiPhieuMuon(int maPM, string trangThai) =>
            ExecuteNonQuery("UPDATE PhieuMuon SET TrangThai=@tt WHERE MaPhieuMuon=@ma",
                new SqlParameter("@tt", trangThai), new SqlParameter("@ma", maPM));

        // ---- Dashboard Statistics ----
        public static int CountSach() =>
            Convert.ToInt32(ExecuteScalar("SELECT COUNT(*) FROM Sach"));

        public static int CountDocGia() =>
            Convert.ToInt32(ExecuteScalar("SELECT COUNT(*) FROM DocGia"));

        public static int CountPhieuMuonDangMo() =>
            Convert.ToInt32(ExecuteScalar("SELECT COUNT(*) FROM PhieuMuon WHERE TrangThai=N'Đang mượn'"));

        public static int CountQuaHan() =>
            Convert.ToInt32(ExecuteScalar("SELECT COUNT(*) FROM PhieuMuon WHERE HanTra<GETDATE() AND TrangThai=N'Đang mượn'"));

        public static DataTable GetSachMuonNhieuNhat(int top = 5) =>
            ExecuteQuery($@"SELECT TOP {top} s.TenSach, COUNT(*) AS SoLanMuon
                            FROM ChiTietPhieuMuon ctpm
                            JOIN Sach s ON ctpm.MaSach=s.MaSach
                            GROUP BY s.TenSach
                            ORDER BY SoLanMuon DESC");

        public static decimal GetTongTienPhat() =>
            Convert.ToDecimal(ExecuteScalar("SELECT ISNULL(SUM(TienPhat),0) FROM ChiTietPhieuMuon"));

        public static int CountNhanVien() =>
            Convert.ToInt32(ExecuteScalar("SELECT COUNT(*) FROM NhanVien WHERE TrangThai=1"));

        public static DataTable GetSachByTheLoai() =>
            ExecuteQuery(@"SELECT tl.TenTheLoai, COUNT(s.MaSach) AS SoLuong
                          FROM TheLoai tl
                          LEFT JOIN Sach s ON tl.MaTL=s.MaTL
                          GROUP BY tl.TenTheLoai
                          ORDER BY SoLuong DESC");

        // ---- Sach available ----
        public static DataTable GetSachAvailable() =>
            ExecuteQuery("SELECT MaSach, TenSach, SoLuong FROM Sach WHERE SoLuong>0");

        public static bool HasAnyNhanVien() =>
            Convert.ToInt32(ExecuteScalar("SELECT COUNT(*) FROM NhanVien")) > 0;
    }
}

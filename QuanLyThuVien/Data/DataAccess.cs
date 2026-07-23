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
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Không đọc được cấu hình kết nối mặc định: {ex}"); }

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
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Không đọc được cấu hình kết nối triển khai: {ex}"); }

            throw new InvalidOperationException("Không tìm thấy chuỗi kết nối cơ sở dữ liệu QuanLyThuVien.");
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

        public static bool TryUpdateNhanVien(int maNV, string hoTen, string tenDN, string vaiTro, bool trangThai,
            string? matKhauMoi, int actorMaNV, out string? failureReason)
        {
            failureReason = null;
            if (string.IsNullOrWhiteSpace(hoTen) || string.IsNullOrWhiteSpace(tenDN))
            {
                failureReason = "Họ tên và tên đăng nhập không được để trống.";
                return false;
            }
            if (vaiTro != "Admin" && vaiTro != "NhanVien")
            {
                failureReason = "Vai trò không hợp lệ.";
                return false;
            }

            using var conn = GetConnection();
            conn.Open();
            using var tran = conn.BeginTransaction(IsolationLevel.Serializable);
            try
            {
                var adminRows = new List<(int MaNV, bool TrangThai)>();
                using (var cmd = new SqlCommand("SELECT MaNV,TrangThai FROM NhanVien WITH (UPDLOCK,HOLDLOCK) WHERE VaiTro=N'Admin'", conn, tran))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        adminRows.Add((reader.GetInt32(0), !reader.IsDBNull(1) && reader.GetBoolean(1)));
                }

                string? actorRole = null;
                bool actorActive = false;
                using (var cmd = new SqlCommand("SELECT VaiTro,TrangThai FROM NhanVien WITH (UPDLOCK,HOLDLOCK) WHERE MaNV=@ma", conn, tran))
                {
                    cmd.Parameters.AddWithValue("@ma", actorMaNV);
                    using var reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        actorRole = reader.IsDBNull(0) ? null : reader.GetString(0);
                        actorActive = !reader.IsDBNull(1) && reader.GetBoolean(1);
                    }
                }

                if (actorRole != "Admin" || !actorActive)
                {
                    failureReason = "Chỉ Admin đang hoạt động mới được quản lý nhân viên.";
                    tran.Rollback();
                    return false;
                }

                string? currentRole = null;
                bool currentActive = false;
                using (var cmd = new SqlCommand("SELECT VaiTro,TrangThai FROM NhanVien WITH (UPDLOCK,HOLDLOCK) WHERE MaNV=@ma", conn, tran))
                {
                    cmd.Parameters.AddWithValue("@ma", maNV);
                    using var reader = cmd.ExecuteReader();
                    if (!reader.Read())
                    {
                        failureReason = "Không tìm thấy nhân viên.";
                        tran.Rollback();
                        return false;
                    }
                    currentRole = reader.IsDBNull(0) ? null : reader.GetString(0);
                    currentActive = !reader.IsDBNull(1) && reader.GetBoolean(1);
                }

                if (maNV == actorMaNV && (vaiTro != "Admin" || !trangThai))
                {
                    failureReason = "Không thể hạ quyền hoặc vô hiệu hóa tài khoản đang đăng nhập.";
                    tran.Rollback();
                    return false;
                }

                int activeAdminCount = adminRows.Count(x => x.TrangThai);
                if (currentRole == "Admin" && currentActive) activeAdminCount--;
                if (vaiTro == "Admin" && trangThai) activeAdminCount++;
                if (activeAdminCount < 1)
                {
                    failureReason = "Hệ thống phải luôn có ít nhất một Admin đang hoạt động.";
                    tran.Rollback();
                    return false;
                }

                using (var cmd = new SqlCommand("UPDATE NhanVien SET HoTen=@ten,TenDangNhap=@tdn,VaiTro=@vt,TrangThai=@tt WHERE MaNV=@ma", conn, tran))
                {
                    cmd.Parameters.AddWithValue("@ten", hoTen.Trim());
                    cmd.Parameters.AddWithValue("@tdn", tenDN.Trim());
                    cmd.Parameters.AddWithValue("@vt", vaiTro);
                    cmd.Parameters.AddWithValue("@tt", trangThai);
                    cmd.Parameters.AddWithValue("@ma", maNV);
                    cmd.ExecuteNonQuery();
                }

                if (!string.IsNullOrWhiteSpace(matKhauMoi))
                {
                    using var cmd = new SqlCommand("UPDATE NhanVien SET MatKhau=@mk WHERE MaNV=@ma", conn, tran);
                    cmd.Parameters.AddWithValue("@mk", Helpers.PasswordHelper.HashPassword(matKhauMoi));
                    cmd.Parameters.AddWithValue("@ma", maNV);
                    cmd.ExecuteNonQuery();
                }

                tran.Commit();
                return true;
            }
            catch (SqlException ex) when (ex.Number is 2601 or 2627)
            {
                tran.Rollback();
                failureReason = "Tên đăng nhập đã tồn tại.";
                return false;
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }

        public static bool TryDeleteNhanVien(int maNV, int actorMaNV, out string? failureReason)
        {
            failureReason = null;
            using var conn = GetConnection();
            conn.Open();
            using var tran = conn.BeginTransaction(IsolationLevel.Serializable);
            try
            {
                string? actorRole = null;
                bool actorActive = false;
                using (var cmd = new SqlCommand("SELECT VaiTro,TrangThai FROM NhanVien WITH (UPDLOCK,HOLDLOCK) WHERE MaNV=@ma", conn, tran))
                {
                    cmd.Parameters.AddWithValue("@ma", actorMaNV);
                    using var reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        actorRole = reader.IsDBNull(0) ? null : reader.GetString(0);
                        actorActive = !reader.IsDBNull(1) && reader.GetBoolean(1);
                    }
                }
                if (actorRole != "Admin" || !actorActive)
                {
                    failureReason = "Chỉ Admin đang hoạt động mới được xóa nhân viên.";
                    tran.Rollback();
                    return false;
                }
                if (maNV == actorMaNV)
                {
                    failureReason = "Không thể xóa tài khoản đang đăng nhập.";
                    tran.Rollback();
                    return false;
                }

                string? targetRole = null;
                bool targetActive = false;
                using (var cmd = new SqlCommand("SELECT VaiTro,TrangThai FROM NhanVien WITH (UPDLOCK,HOLDLOCK) WHERE MaNV=@ma", conn, tran))
                {
                    cmd.Parameters.AddWithValue("@ma", maNV);
                    using var reader = cmd.ExecuteReader();
                    if (!reader.Read())
                    {
                        failureReason = "Không tìm thấy nhân viên.";
                        tran.Rollback();
                        return false;
                    }
                    targetRole = reader.IsDBNull(0) ? null : reader.GetString(0);
                    targetActive = !reader.IsDBNull(1) && reader.GetBoolean(1);
                }

                if (targetRole == "Admin" && targetActive)
                {
                    using var countCmd = new SqlCommand("SELECT COUNT(*) FROM NhanVien WITH (UPDLOCK,HOLDLOCK) WHERE VaiTro=N'Admin' AND TrangThai=1", conn, tran);
                    if (Convert.ToInt32(countCmd.ExecuteScalar()) <= 1)
                    {
                        failureReason = "Hệ thống phải luôn có ít nhất một Admin đang hoạt động.";
                        tran.Rollback();
                        return false;
                    }
                }

                using (var cmd = new SqlCommand("DELETE FROM NhanVien WHERE MaNV=@ma", conn, tran))
                {
                    cmd.Parameters.AddWithValue("@ma", maNV);
                    cmd.ExecuteNonQuery();
                }
                tran.Commit();
                return true;
            }
            catch (SqlException ex) when (ex.Number == 547)
            {
                tran.Rollback();
                failureReason = "Không thể xóa nhân viên đã có dữ liệu liên quan.";
                return false;
            }
            catch
            {
                tran.Rollback();
                throw;
            }
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

        public static int InsertTacGia(string tenTG, string quocTich, string ghiChu) =>
            ExecuteNonQuery("INSERT INTO TacGia(TenTG,QuocTich,GhiChu) VALUES(@ten,@qt,@gc)",
                new SqlParameter("@ten", tenTG), new SqlParameter("@qt", quocTich), new SqlParameter("@gc", ghiChu));

        public static int UpdateTacGia(int maTG, string tenTG, string quocTich, string ghiChu) =>
            ExecuteNonQuery("UPDATE TacGia SET TenTG=@ten,QuocTich=@qt,GhiChu=@gc WHERE MaTG=@ma",
                new SqlParameter("@ten", tenTG), new SqlParameter("@qt", quocTich), new SqlParameter("@gc", ghiChu), new SqlParameter("@ma", maTG));

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
        public static DataTable GetAllSach(string? keyword = null, int? maTL = null, bool onlyAvailable = false)
        {
            var conditions = new List<string>();
            var parameters = new List<SqlParameter>();
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                conditions.Add("(s.TenSach LIKE @kw OR s.MaISBN LIKE @kw OR tg.TenTG LIKE @kw OR tl.TenTheLoai LIKE @kw OR nxb.TenNXB LIKE @kw)");
                parameters.Add(new SqlParameter("@kw", SqlDbType.NVarChar, 500) { Value = $"%{keyword.Trim()}%" });
            }
            if (maTL.HasValue && maTL.Value > 0)
            {
                conditions.Add("s.MaTL=@malt");
                parameters.Add(new SqlParameter("@malt", SqlDbType.Int) { Value = maTL.Value });
            }
            if (onlyAvailable) conditions.Add("s.SoLuong > 0");

            string where = conditions.Count == 0 ? string.Empty : "WHERE " + string.Join(" AND ", conditions);
            return ExecuteQuery($@"SELECT s.*, tl.TenTheLoai, tg.TenTG AS TenTacGia, nxb.TenNXB
                           FROM Sach s
                           LEFT JOIN TheLoai tl ON s.MaTL=tl.MaTL
                           LEFT JOIN TacGia tg ON s.MaTG=tg.MaTG
                           LEFT JOIN NhaXuatBan nxb ON s.MaNXB=nxb.MaNXB
                           {where}
                           ORDER BY s.TenSach ASC", parameters.ToArray());
        }

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
        public static DataTable GetAllDocGia(string? keyword = null, bool? activeOnly = null, bool? expiringWithin30Days = null)
        {
            var conditions = new List<string>();
            var parameters = new List<SqlParameter>();
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                conditions.Add("(HoTen LIKE @kw OR SoDienThoai LIKE @kw OR Email LIKE @kw OR CONVERT(NVARCHAR(20), MaDG) LIKE @kw)");
                parameters.Add(new SqlParameter("@kw", SqlDbType.NVarChar, 500) { Value = $"%{keyword.Trim()}%" });
            }
            if (activeOnly == true) conditions.Add("TrangThai=1 AND HanSuDung>=CAST(GETDATE() AS DATE)");
            if (activeOnly == false) conditions.Add("(TrangThai=0 OR HanSuDung<CAST(GETDATE() AS DATE))");
            if (expiringWithin30Days == true) conditions.Add("TrangThai=1 AND HanSuDung>=CAST(GETDATE() AS DATE) AND HanSuDung<=DATEADD(DAY,30,CAST(GETDATE() AS DATE))");

            string where = conditions.Count == 0 ? string.Empty : "WHERE " + string.Join(" AND ", conditions);
            return ExecuteQuery($"SELECT * FROM DocGia {where} ORDER BY HoTen ASC", parameters.ToArray());
        }

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

        public static DataTable GetDocGiaById(int maDG) =>
            ExecuteQuery("SELECT * FROM DocGia WHERE MaDG=@ma",
                new SqlParameter("@ma", SqlDbType.Int) { Value = maDG });

        // ---- PhieuMuon ----
        public static DataTable GetAllPhieuMuon(string? keyword = null, string? status = null, DateTime? fromDate = null, DateTime? toDate = null, bool overdueOnly = false)
        {
            var conditions = new List<string>();
            var parameters = new List<SqlParameter>();
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                conditions.Add("(dg.HoTen LIKE @kw OR CONVERT(NVARCHAR(20), pm.MaPhieuMuon) LIKE @kw)");
                parameters.Add(new SqlParameter("@kw", SqlDbType.NVarChar, 500) { Value = $"%{keyword.Trim()}%" });
            }
            if (!string.IsNullOrWhiteSpace(status) && status != "Tất cả")
            {
                conditions.Add("pm.TrangThai=@status");
                parameters.Add(new SqlParameter("@status", SqlDbType.NVarChar, 30) { Value = status });
            }
            if (fromDate.HasValue) { conditions.Add("pm.NgayMuon>=@fromDate"); parameters.Add(new SqlParameter("@fromDate", SqlDbType.Date) { Value = fromDate.Value.Date }); }
            if (toDate.HasValue) { conditions.Add("pm.NgayMuon<=@toDate"); parameters.Add(new SqlParameter("@toDate", SqlDbType.Date) { Value = toDate.Value.Date }); }
            if (overdueOnly) conditions.Add("pm.HanTra<CAST(GETDATE() AS DATE) AND pm.TrangThai IN (N'Đang mượn',N'Đã trả một phần')");

            string where = conditions.Count == 0 ? string.Empty : "WHERE " + string.Join(" AND ", conditions);
            return ExecuteQuery($@"SELECT pm.*, dg.HoTen AS TenDocGia, nv.HoTen AS TenNhanVien,
                                  (SELECT COUNT(*) FROM ChiTietPhieuMuon x WHERE x.MaPhieuMuon=pm.MaPhieuMuon AND x.NgayTra IS NULL) AS SoDongChuaTra,
                                  (SELECT COUNT(*) FROM ChiTietPhieuMuon x WHERE x.MaPhieuMuon=pm.MaPhieuMuon AND x.NgayTra IS NOT NULL) AS SoDongDaTra
                           FROM PhieuMuon pm
                           LEFT JOIN DocGia dg ON pm.MaDG=dg.MaDG
                           LEFT JOIN NhanVien nv ON pm.MaNV=nv.MaNV
                           {where}
                           ORDER BY pm.NgayMuon DESC, pm.MaPhieuMuon DESC", parameters.ToArray());
        }

        public static DataTable GetPhieuMuonById(int maPM) =>
            ExecuteQuery(@"SELECT pm.*, dg.HoTen AS TenDocGia, nv.HoTen AS TenNhanVien,
                                  (SELECT COUNT(*) FROM ChiTietPhieuMuon x WHERE x.MaPhieuMuon=pm.MaPhieuMuon AND x.NgayTra IS NULL) AS SoDongChuaTra,
                                  (SELECT COUNT(*) FROM ChiTietPhieuMuon x WHERE x.MaPhieuMuon=pm.MaPhieuMuon AND x.NgayTra IS NOT NULL) AS SoDongDaTra
                           FROM PhieuMuon pm
                           LEFT JOIN DocGia dg ON pm.MaDG=dg.MaDG
                           LEFT JOIN NhanVien nv ON pm.MaNV=nv.MaNV
                           WHERE pm.MaPhieuMuon=@ma",
                new SqlParameter("@ma", maPM));

        public static DataTable GetChiTietPhieuMuon(int maPM) =>
            ExecuteQuery(@"SELECT ctpm.*, s.TenSach, s.GiaTien
                           FROM ChiTietPhieuMuon ctpm
                           LEFT JOIN Sach s ON ctpm.MaSach=s.MaSach
                           WHERE ctpm.MaPhieuMuon=@ma",
                new SqlParameter("@ma", maPM));

        public static DataTable GetPhieuTra(string? keyword = null, bool overdueOnly = false)
        {
            var conditions = new List<string>
            {
                "pm.TrangThai IN (N'Đang mượn',N'Đã trả một phần')",
                "EXISTS (SELECT 1 FROM ChiTietPhieuMuon ctpm WHERE ctpm.MaPhieuMuon=pm.MaPhieuMuon AND ctpm.NgayTra IS NULL)"
            };
            var parameters = new List<SqlParameter>();
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                conditions.Add("(dg.HoTen LIKE @kw OR CONVERT(NVARCHAR(20), pm.MaPhieuMuon) LIKE @kw)");
                parameters.Add(new SqlParameter("@kw", SqlDbType.NVarChar, 500) { Value = $"%{keyword.Trim()}%" });
            }
            if (overdueOnly) conditions.Add("pm.HanTra<CAST(GETDATE() AS DATE)");

            return ExecuteQuery($@"SELECT pm.MaPhieuMuon, pm.NgayMuon, pm.HanTra, dg.HoTen AS TenDocGia,
                      (SELECT ISNULL(SUM(ctpm.SoLuong),0) FROM ChiTietPhieuMuon ctpm WHERE ctpm.MaPhieuMuon=pm.MaPhieuMuon AND ctpm.NgayTra IS NULL) AS SoLuongChuaTra
                      FROM PhieuMuon pm JOIN DocGia dg ON pm.MaDG=dg.MaDG
                      WHERE {string.Join(" AND ", conditions)}
                      ORDER BY pm.HanTra ASC, pm.MaPhieuMuon DESC", parameters.ToArray());
        }

        public static DataTable GetDocGiaHistory(int maDG) =>
            ExecuteQuery(@"SELECT pm.MaPhieuMuon, pm.NgayMuon, pm.HanTra, pm.TrangThai,
                                  MAX(ct.NgayTra) AS NgayTraCuoi,
                                  COUNT(ct.MaSach) AS SoDauSach,
                                  ISNULL(SUM(ct.TienPhat),0) AS TongTienPhat,
                                  ISNULL(SUM(ct.TienDenMatSach),0) AS TongTienDen
                           FROM PhieuMuon pm
                           LEFT JOIN ChiTietPhieuMuon ct ON ct.MaPhieuMuon=pm.MaPhieuMuon
                           WHERE pm.MaDG=@madg
                           GROUP BY pm.MaPhieuMuon, pm.NgayMuon, pm.HanTra, pm.TrangThai
                           ORDER BY pm.NgayMuon DESC, pm.MaPhieuMuon DESC",
                new SqlParameter("@madg", SqlDbType.Int) { Value = maDG });

        public static DataTable GetDocGiaHistoryDetails(int maDG) =>
            ExecuteQuery(@"SELECT pm.MaPhieuMuon, pm.NgayMuon, pm.HanTra, pm.TrangThai,
                                  s.TenSach, ct.SoLuong, ct.NgayTra, ct.TienPhat, ct.TienDenMatSach,
                                  ISNULL(tt.DaThu,0) AS DaThu
                           FROM PhieuMuon pm
                           INNER JOIN ChiTietPhieuMuon ct ON ct.MaPhieuMuon=pm.MaPhieuMuon
                           LEFT JOIN Sach s ON s.MaSach=ct.MaSach
                           OUTER APPLY (SELECT SUM(tp.SoTien) AS DaThu FROM ThanhToanPhat tp WHERE tp.MaPhieuMuon=pm.MaPhieuMuon) tt
                           WHERE pm.MaDG=@madg
                           ORDER BY pm.NgayMuon DESC, pm.MaPhieuMuon DESC, s.TenSach",
                new SqlParameter("@madg", SqlDbType.Int) { Value = maDG });

        private static bool ValidateDetails(List<(int maSach, int soLuong)>? chiTiet, out string? failureReason)
        {
            failureReason = null;
            if (chiTiet == null || chiTiet.Count == 0) { failureReason = "Phiếu phải có ít nhất một đầu sách."; return false; }
            if (chiTiet.Any(x => x.maSach <= 0 || x.soLuong <= 0)) { failureReason = "Số lượng mượn phải lớn hơn 0."; return false; }
            if (chiTiet.Select(x => x.maSach).Distinct().Count() != chiTiet.Count) { failureReason = "Không được chọn trùng đầu sách."; return false; }
            return true;
        }

        public static bool InsertPhieuMuonFull(PhieuMuon pm, List<(int maSach, int soLuong)> chiTiet, out string? failureReason)
        {
            failureReason = null;
            if (!ValidateDetails(chiTiet, out failureReason)) return false;
            if (pm.NgayMuon.Date > DateTime.Today)
            {
                failureReason = "Ngày mượn không được ở tương lai.";
                return false;
            }

            if (pm.HanTra.Date < pm.NgayMuon.Date)
            {
                failureReason = "Hạn trả không được trước ngày mượn.";
                return false;
            }

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
                            cmd.Parameters.AddWithValue("@tt", "Đang mượn");
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

        public static DataTable GetSachAvailableForLoanEdit(int maPM) =>
            ExecuteQuery(@"SELECT s.MaSach, s.TenSach,
                                  s.SoLuong + ISNULL(ct.SoLuong,0) AS SoLuongKhaDung,
                                  ISNULL(ct.SoLuong,0) AS SoLuongMuon
                           FROM Sach s
                           LEFT JOIN ChiTietPhieuMuon ct ON ct.MaSach=s.MaSach AND ct.MaPhieuMuon=@ma
                           WHERE s.SoLuong>0 OR ct.MaSach IS NOT NULL
                           ORDER BY s.TenSach", new SqlParameter("@ma", maPM));

        public static bool UpdatePhieuMuonFull(PhieuMuon pm, List<(int maSach, int soLuong)> chiTiet, out string? failureReason)
        {
            failureReason = null;
            if (!ValidateDetails(chiTiet, out failureReason)) return false;
            if (pm.NgayMuon.Date > DateTime.Today) { failureReason = "Ngày mượn không được ở tương lai."; return false; }
            if (pm.HanTra.Date < pm.NgayMuon.Date) { failureReason = "Hạn trả không được trước ngày mượn."; return false; }

            using var conn = GetConnection(); conn.Open(); using var tran = conn.BeginTransaction();
            try
            {
                string? status;
                using (var cmd = new SqlCommand("SELECT TrangThai FROM PhieuMuon WITH (UPDLOCK,HOLDLOCK) WHERE MaPhieuMuon=@ma", conn, tran))
                { cmd.Parameters.AddWithValue("@ma", pm.MaPhieuMuon); status = cmd.ExecuteScalar()?.ToString(); }
                if (status == null) { failureReason = "Không tìm thấy phiếu mượn."; tran.Rollback(); return false; }
                if (status == "Đã trả") { failureReason = "Phiếu đã trả hết, không thể sửa."; tran.Rollback(); return false; }

                var old = new Dictionary<int, int>();
                using (var cmd = new SqlCommand("SELECT MaSach,SoLuong,NgayTra FROM ChiTietPhieuMuon WITH (UPDLOCK,HOLDLOCK) WHERE MaPhieuMuon=@ma", conn, tran))
                { cmd.Parameters.AddWithValue("@ma", pm.MaPhieuMuon); using var rd = cmd.ExecuteReader(); while (rd.Read()) { if (!rd.IsDBNull(2)) { failureReason = "Phiếu đã có sách trả, không thể sửa toàn bộ."; rd.Close(); tran.Rollback(); return false; } old[rd.GetInt32(0)] = rd.GetInt32(1); } }
                if (old.Count == 0) { failureReason = "Phiếu không có chi tiết sách."; tran.Rollback(); return false; }

                using (var cmdReader = new SqlCommand("SELECT 1 FROM DocGia WITH (UPDLOCK,HOLDLOCK) WHERE MaDG=@ma AND TrangThai=1 AND HanSuDung>=CAST(GETDATE() AS DATE)", conn, tran))
                { cmdReader.Parameters.AddWithValue("@ma", pm.MaDG); if (cmdReader.ExecuteScalar() == null) { failureReason = "Thẻ độc giả đã hết hạn hoặc không còn hoạt động."; tran.Rollback(); return false; } }

                var ids = old.Keys.Union(chiTiet.Select(x => x.maSach)).Distinct().ToList();
                var placeholders = ids.Select((_, i) => "@b" + i).ToArray();
                using var cmdBooks = new SqlCommand($"SELECT MaSach,SoLuong FROM Sach WITH (UPDLOCK,HOLDLOCK) WHERE MaSach IN ({string.Join(",", placeholders)})", conn, tran);
                for (int i = 0; i < ids.Count; i++) cmdBooks.Parameters.AddWithValue(placeholders[i], ids[i]);
                var stock = new Dictionary<int, int>(); using (var rd = cmdBooks.ExecuteReader()) while (rd.Read()) stock[rd.GetInt32(0)] = rd.GetInt32(1);
                foreach (var id in ids) if (!stock.ContainsKey(id)) { failureReason = "Một đầu sách không còn tồn tại."; tran.Rollback(); return false; }
                foreach (var id in ids)
                {
                    int newQty = chiTiet.FirstOrDefault(x => x.maSach == id).soLuong;
                    int available = stock[id] + old.GetValueOrDefault(id) - newQty;
                    if (available < 0) { failureReason = "Không đủ tồn kho cho số lượng mới."; tran.Rollback(); return false; }
                    using var cmd = new SqlCommand("UPDATE Sach SET SoLuong=@sl WHERE MaSach=@ma", conn, tran); cmd.Parameters.AddWithValue("@sl", available); cmd.Parameters.AddWithValue("@ma", id); cmd.ExecuteNonQuery();
                }
                using (var cmd = new SqlCommand("UPDATE PhieuMuon SET MaDG=@dg,NgayMuon=@nm,HanTra=@ht,TrangThai=N'Đang mượn' WHERE MaPhieuMuon=@ma", conn, tran))
                { cmd.Parameters.AddWithValue("@dg", pm.MaDG); cmd.Parameters.AddWithValue("@nm", pm.NgayMuon); cmd.Parameters.AddWithValue("@ht", pm.HanTra); cmd.Parameters.AddWithValue("@ma", pm.MaPhieuMuon); cmd.ExecuteNonQuery(); }
                using (var cmd = new SqlCommand("DELETE FROM ChiTietPhieuMuon WHERE MaPhieuMuon=@ma", conn, tran)) { cmd.Parameters.AddWithValue("@ma", pm.MaPhieuMuon); cmd.ExecuteNonQuery(); }
                foreach (var item in chiTiet)
                { using var cmd = new SqlCommand("INSERT INTO ChiTietPhieuMuon(MaPhieuMuon,MaSach,SoLuong) VALUES(@pm,@s,@q)", conn, tran); cmd.Parameters.AddWithValue("@pm", pm.MaPhieuMuon); cmd.Parameters.AddWithValue("@s", item.maSach); cmd.Parameters.AddWithValue("@q", item.soLuong); cmd.ExecuteNonQuery(); }
                tran.Commit(); return true;
            }
            catch { tran.Rollback(); throw; }
        }

        public static bool ReturnSelectedBooks(int maPM, List<ReturnBookRequest> requests, decimal finePerDayPerBook, out string? failureReason)
        {
            failureReason = null;
            if (requests == null || requests.Count == 0) { failureReason = "Chọn ít nhất một đầu sách để trả."; return false; }
            if (requests.Any(x => x.MaSach <= 0 || x.SoLuongMat < 0) || requests.Select(x => x.MaSach).Distinct().Count() != requests.Count) { failureReason = "Danh sách sách trả không hợp lệ."; return false; }
            if (finePerDayPerBook < 0 || decimal.Truncate(finePerDayPerBook) != finePerDayPerBook) { failureReason = "Mức phạt phải là số nguyên không âm."; return false; }

            using var conn = GetConnection(); conn.Open(); using var tran = conn.BeginTransaction();
            try
            {
                string? status;
                DateTime dueDate;
                using (var cmd = new SqlCommand("SELECT TrangThai,HanTra FROM PhieuMuon WITH (UPDLOCK,HOLDLOCK) WHERE MaPhieuMuon=@ma", conn, tran))
                {
                    cmd.Parameters.AddWithValue("@ma", maPM);
                    using var rd = cmd.ExecuteReader();
                    if (!rd.Read()) { failureReason = "Không tìm thấy phiếu mượn."; tran.Rollback(); return false; }
                    status = rd.IsDBNull(0) ? null : rd.GetString(0);
                    dueDate = rd.GetDateTime(1);
                }
                if (status != "Đang mượn" && status != "Đã trả một phần") { failureReason = "Phiếu này không còn sách chưa trả."; tran.Rollback(); return false; }
                int overdueDays = dueDate.Date < DateTime.Today ? (DateTime.Today - dueDate.Date).Days : 0;
                var placeholders = requests.Select((_, i) => "@b" + i).ToArray();
                using var cmdDetails = new SqlCommand($@"SELECT ct.MaSach,ct.SoLuong,ct.NgayTra,s.GiaTien
                    FROM ChiTietPhieuMuon ct WITH (UPDLOCK,HOLDLOCK)
                    INNER JOIN Sach s WITH (UPDLOCK,HOLDLOCK) ON s.MaSach=ct.MaSach
                    WHERE ct.MaPhieuMuon=@pm AND ct.MaSach IN ({string.Join(",", placeholders)})
                    ORDER BY ct.MaSach", conn, tran);
                cmdDetails.Parameters.AddWithValue("@pm", maPM);
                for (int i = 0; i < requests.Count; i++) cmdDetails.Parameters.AddWithValue(placeholders[i], requests[i].MaSach);
                var details = new Dictionary<int, (int SoLuong, decimal GiaTien)>();
                using (var rd = cmdDetails.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        if (!rd.IsDBNull(2)) { failureReason = "Một sách đã được trả trước đó."; rd.Close(); tran.Rollback(); return false; }
                        details[rd.GetInt32(0)] = (rd.GetInt32(1), rd.GetDecimal(3));
                    }
                }
                if (details.Count != requests.Count) { failureReason = "Một sách không thuộc phiếu mượn này hoặc đã bị xóa."; tran.Rollback(); return false; }

                foreach (ReturnBookRequest request in requests)
                {
                    var detail = details[request.MaSach];
                    if (request.SoLuongMat > detail.SoLuong)
                    {
                        failureReason = "Số lượng sách mất không được vượt quá số lượng đang mượn.";
                        tran.Rollback();
                        return false;
                    }

                    int returnedQuantity = detail.SoLuong - request.SoLuongMat;
                    decimal fine = overdueDays * finePerDayPerBook * detail.SoLuong;
                    decimal compensation = detail.GiaTien * request.SoLuongMat;
                    using var cmd = new SqlCommand(@"UPDATE ChiTietPhieuMuon
                        SET NgayTra=CAST(GETDATE() AS DATE),TienPhat=@tp,SoLuongMat=@mat,TienDenMatSach=@den
                        WHERE MaPhieuMuon=@pm AND MaSach=@s AND NgayTra IS NULL", conn, tran);
                    cmd.Parameters.AddWithValue("@tp", fine);
                    cmd.Parameters.AddWithValue("@mat", request.SoLuongMat);
                    cmd.Parameters.AddWithValue("@den", compensation);
                    cmd.Parameters.AddWithValue("@pm", maPM);
                    cmd.Parameters.AddWithValue("@s", request.MaSach);
                    if (cmd.ExecuteNonQuery() != 1) { failureReason = "Dữ liệu đã thay đổi, vui lòng tải lại."; tran.Rollback(); return false; }

                    if (returnedQuantity > 0)
                    {
                        using var cmdStock = new SqlCommand("UPDATE Sach SET SoLuong=SoLuong+@q WHERE MaSach=@s", conn, tran);
                        cmdStock.Parameters.AddWithValue("@q", returnedQuantity);
                        cmdStock.Parameters.AddWithValue("@s", request.MaSach);
                        if (cmdStock.ExecuteNonQuery() != 1) { failureReason = "Không tìm thấy sách để hoàn tồn kho."; tran.Rollback(); return false; }
                    }
                }
                using (var cmd = new SqlCommand("IF EXISTS (SELECT 1 FROM ChiTietPhieuMuon WHERE MaPhieuMuon=@pm AND NgayTra IS NULL) UPDATE PhieuMuon SET TrangThai=N'Đã trả một phần' WHERE MaPhieuMuon=@pm ELSE UPDATE PhieuMuon SET TrangThai=N'Đã trả' WHERE MaPhieuMuon=@pm", conn, tran)) { cmd.Parameters.AddWithValue("@pm", maPM); cmd.ExecuteNonQuery(); }
                tran.Commit(); return true;
            }
            catch { tran.Rollback(); throw; }
        }

        public static bool SyncPhieuMuonStatus(int maPM, int actorMaNV, out string? failureReason)
        {
            failureReason = null;
            using var conn = GetConnection();
            conn.Open();
            using var tran = conn.BeginTransaction(IsolationLevel.Serializable);
            try
            {
                using (var actorCmd = new SqlCommand("SELECT 1 FROM NhanVien WITH (UPDLOCK,HOLDLOCK) WHERE MaNV=@nv AND VaiTro=N'Admin' AND TrangThai=1", conn, tran))
                {
                    actorCmd.Parameters.AddWithValue("@nv", actorMaNV);
                    if (actorCmd.ExecuteScalar() == null)
                    {
                        failureReason = "Chỉ Admin đang hoạt động mới được đồng bộ trạng thái.";
                        tran.Rollback();
                        return false;
                    }
                }

                string? currentStatus;
                using (var loanCmd = new SqlCommand("SELECT TrangThai FROM PhieuMuon WITH (UPDLOCK,HOLDLOCK) WHERE MaPhieuMuon=@pm", conn, tran))
                {
                    loanCmd.Parameters.AddWithValue("@pm", maPM);
                    currentStatus = loanCmd.ExecuteScalar()?.ToString();
                }

                if (currentStatus == null)
                {
                    failureReason = "Không tìm thấy phiếu mượn.";
                    tran.Rollback();
                    return false;
                }

                int unreturned = 0;
                int returned = 0;
                using (var detailCmd = new SqlCommand("SELECT NgayTra FROM ChiTietPhieuMuon WITH (UPDLOCK,HOLDLOCK) WHERE MaPhieuMuon=@pm", conn, tran))
                {
                    detailCmd.Parameters.AddWithValue("@pm", maPM);
                    using var reader = detailCmd.ExecuteReader();
                    while (reader.Read())
                    {
                        if (reader.IsDBNull(0)) unreturned++;
                        else returned++;
                    }
                }

                if (unreturned + returned == 0)
                {
                    failureReason = "Phiếu không có chi tiết sách để đồng bộ.";
                    tran.Rollback();
                    return false;
                }

                string expectedStatus = unreturned == 0
                    ? "Đã trả"
                    : returned == 0
                        ? "Đang mượn"
                        : "Đã trả một phần";

                if (!string.Equals(currentStatus, expectedStatus, StringComparison.Ordinal))
                {
                    using var updateCmd = new SqlCommand("UPDATE PhieuMuon SET TrangThai=@status WHERE MaPhieuMuon=@pm", conn, tran);
                    updateCmd.Parameters.AddWithValue("@status", expectedStatus);
                    updateCmd.Parameters.AddWithValue("@pm", maPM);
                    if (updateCmd.ExecuteNonQuery() != 1)
                    {
                        failureReason = "Dữ liệu đã thay đổi, vui lòng tải lại.";
                        tran.Rollback();
                        return false;
                    }
                }

                tran.Commit();
                return true;
            }
            catch
            {
                if (tran.Connection != null) tran.Rollback();
                throw;
            }
        }

        public static bool CreateDemoLoanScenario(
            DemoLoanScenarioRequest request,
            int actorMaNV,
            out int maPM,
            out string? failureReason)
        {
            maPM = 0;
            failureReason = null;
            if (!Enum.IsDefined(request.Scenario))
            {
                failureReason = "Kịch bản demo không hợp lệ.";
                return false;
            }

            if (request.MaDG <= 0 || request.NgayMuon.Date > DateTime.Today || request.HanTra.Date < request.NgayMuon.Date)
            {
                failureReason = "Thông tin ngày hoặc độc giả không hợp lệ.";
                return false;
            }

            if (request.ChiTiet == null || request.ChiTiet.Count == 0
                || request.ChiTiet.Any(x => x.MaSach <= 0 || x.SoLuong <= 0)
                || request.ChiTiet.Select(x => x.MaSach).Distinct().Count() != request.ChiTiet.Count)
            {
                failureReason = "Danh sách sách demo không hợp lệ.";
                return false;
            }

            if (request.Scenario == DemoLoanScenario.QuaHan && request.HanTra.Date >= DateTime.Today)
            {
                failureReason = "Kịch bản quá hạn cần hạn trả trước hôm nay.";
                return false;
            }

            if (request.Scenario == DemoLoanScenario.DangMuon && request.HanTra.Date < DateTime.Today)
            {
                failureReason = "Kịch bản đang mượn không được có hạn trả đã qua.";
                return false;
            }

            if (request.Scenario == DemoLoanScenario.DaTraMotPhan && request.ChiTiet.Count < 2)
            {
                failureReason = "Kịch bản trả một phần cần ít nhất hai đầu sách.";
                return false;
            }

            var firstDetail = request.ChiTiet[0];
            if (request.Scenario == DemoLoanScenario.CoSachMat
                && (request.LostQuantity < 1 || request.LostQuantity > firstDetail.SoLuong))
            {
                failureReason = $"Số lượng mất phải từ 1 đến {firstDetail.SoLuong}.";
                return false;
            }

            using var conn = GetConnection();
            conn.Open();
            using var tran = conn.BeginTransaction(IsolationLevel.Serializable);
            try
            {
                using (var actorCmd = new SqlCommand("SELECT 1 FROM NhanVien WITH (UPDLOCK,HOLDLOCK) WHERE MaNV=@nv AND VaiTro=N'Admin' AND TrangThai=1", conn, tran))
                {
                    actorCmd.Parameters.AddWithValue("@nv", actorMaNV);
                    if (actorCmd.ExecuteScalar() == null)
                    {
                        failureReason = "Chỉ Admin đang hoạt động mới được tạo kịch bản demo.";
                        tran.Rollback();
                        return false;
                    }
                }

                using (var readerCmd = new SqlCommand("SELECT 1 FROM DocGia WITH (UPDLOCK,HOLDLOCK) WHERE MaDG=@dg AND TrangThai=1 AND HanSuDung>=CAST(GETDATE() AS DATE)", conn, tran))
                {
                    readerCmd.Parameters.AddWithValue("@dg", request.MaDG);
                    if (readerCmd.ExecuteScalar() == null)
                    {
                        failureReason = "Thẻ độc giả đã hết hạn hoặc không còn hoạt động.";
                        tran.Rollback();
                        return false;
                    }
                }

                var bookIds = request.ChiTiet.Select(x => x.MaSach).ToList();
                var placeholders = bookIds.Select((_, i) => "@b" + i).ToArray();
                var stock = new Dictionary<int, int>();
                using (var bookCmd = new SqlCommand($"SELECT MaSach,SoLuong FROM Sach WITH (UPDLOCK,HOLDLOCK) WHERE MaSach IN ({string.Join(",", placeholders)})", conn, tran))
                {
                    for (int i = 0; i < bookIds.Count; i++) bookCmd.Parameters.AddWithValue(placeholders[i], bookIds[i]);
                    using var reader = bookCmd.ExecuteReader();
                    while (reader.Read()) stock[reader.GetInt32(0)] = reader.GetInt32(1);
                }

                foreach (var item in request.ChiTiet)
                {
                    if (!stock.TryGetValue(item.MaSach, out int available) || available < item.SoLuong)
                    {
                        failureReason = "Không đủ tồn kho cho kịch bản demo.";
                        tran.Rollback();
                        return false;
                    }
                }

                using (var loanCmd = new SqlCommand(@"INSERT INTO PhieuMuon(MaDG,MaNV,NgayMuon,HanTra,TrangThai)
                                                     VALUES(@dg,@nv,@nm,@ht,N'Đang mượn'); SELECT CAST(SCOPE_IDENTITY() AS INT);", conn, tran))
                {
                    loanCmd.Parameters.AddWithValue("@dg", request.MaDG);
                    loanCmd.Parameters.AddWithValue("@nv", actorMaNV);
                    loanCmd.Parameters.AddWithValue("@nm", request.NgayMuon.Date);
                    loanCmd.Parameters.AddWithValue("@ht", request.HanTra.Date);
                    maPM = Convert.ToInt32(loanCmd.ExecuteScalar());
                }

                foreach (var item in request.ChiTiet)
                {
                    using (var stockCmd = new SqlCommand("UPDATE Sach SET SoLuong=SoLuong-@q WHERE MaSach=@s AND SoLuong>=@q", conn, tran))
                    {
                        stockCmd.Parameters.AddWithValue("@q", item.SoLuong);
                        stockCmd.Parameters.AddWithValue("@s", item.MaSach);
                        if (stockCmd.ExecuteNonQuery() != 1)
                        {
                            failureReason = "Dữ liệu tồn kho đã thay đổi, vui lòng thử lại.";
                            tran.Rollback();
                            return false;
                        }
                    }

                    using var detailCmd = new SqlCommand("INSERT INTO ChiTietPhieuMuon(MaPhieuMuon,MaSach,SoLuong) VALUES(@pm,@s,@q)", conn, tran);
                    detailCmd.Parameters.AddWithValue("@pm", maPM);
                    detailCmd.Parameters.AddWithValue("@s", item.MaSach);
                    detailCmd.Parameters.AddWithValue("@q", item.SoLuong);
                    detailCmd.ExecuteNonQuery();
                }

                if (request.Scenario is DemoLoanScenario.DaTraMotPhan or DemoLoanScenario.DaTra or DemoLoanScenario.CoSachMat)
                {
                    var returns = request.Scenario == DemoLoanScenario.DaTraMotPhan
                        ? new List<ReturnBookRequest> { new(firstDetail.MaSach, 0) }
                        : request.ChiTiet.Select((item, index) => new ReturnBookRequest(item.MaSach, request.Scenario == DemoLoanScenario.CoSachMat && index == 0 ? request.LostQuantity : 0)).ToList();
                    if (!ReturnSelectedBooksInTransaction(conn, tran, maPM, returns, 10000m, out failureReason))
                    {
                        tran.Rollback();
                        return false;
                    }
                }

                tran.Commit();
                return true;
            }
            catch
            {
                if (tran.Connection != null) tran.Rollback();
                throw;
            }
        }

        private static bool ReturnSelectedBooksInTransaction(
            SqlConnection conn,
            SqlTransaction tran,
            int maPM,
            List<ReturnBookRequest> requests,
            decimal finePerDayPerBook,
            out string? failureReason)
        {
            failureReason = null;
            var placeholders = requests.Select((_, i) => "@b" + i).ToArray();
            DateTime dueDate;
            using (var loanCmd = new SqlCommand("SELECT HanTra,TrangThai FROM PhieuMuon WITH (UPDLOCK,HOLDLOCK) WHERE MaPhieuMuon=@pm", conn, tran))
            {
                loanCmd.Parameters.AddWithValue("@pm", maPM);
                using var reader = loanCmd.ExecuteReader();
                if (!reader.Read()) { failureReason = "Không tìm thấy phiếu mượn."; return false; }
                dueDate = reader.GetDateTime(0).Date;
                string status = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                if (status is not ("Đang mượn" or "Đã trả một phần")) { failureReason = "Phiếu không còn sách chưa trả."; return false; }
            }

            int overdueDays = dueDate < DateTime.Today ? (DateTime.Today - dueDate).Days : 0;
            var details = new Dictionary<int, (int Quantity, decimal Price)>();
            using (var detailCmd = new SqlCommand($@"SELECT ct.MaSach,ct.SoLuong,ct.NgayTra,s.GiaTien
                                                     FROM ChiTietPhieuMuon ct WITH (UPDLOCK,HOLDLOCK)
                                                     INNER JOIN Sach s WITH (UPDLOCK,HOLDLOCK) ON s.MaSach=ct.MaSach
                                                     WHERE ct.MaPhieuMuon=@pm AND ct.MaSach IN ({string.Join(",", placeholders)})", conn, tran))
            {
                detailCmd.Parameters.AddWithValue("@pm", maPM);
                for (int i = 0; i < requests.Count; i++) detailCmd.Parameters.AddWithValue(placeholders[i], requests[i].MaSach);
                using var reader = detailCmd.ExecuteReader();
                while (reader.Read())
                {
                    if (!reader.IsDBNull(2)) { failureReason = "Một sách đã được trả trước đó."; return false; }
                    details[reader.GetInt32(0)] = (reader.GetInt32(1), reader.GetDecimal(3));
                }
            }

            if (details.Count != requests.Count) { failureReason = "Một sách không thuộc phiếu mượn này."; return false; }
            foreach (var request in requests)
            {
                var detail = details[request.MaSach];
                if (request.SoLuongMat < 0 || request.SoLuongMat > detail.Quantity) { failureReason = "Số lượng sách mất không hợp lệ."; return false; }
                decimal fine = overdueDays * finePerDayPerBook * detail.Quantity;
                decimal compensation = detail.Price * request.SoLuongMat;
                using (var updateCmd = new SqlCommand(@"UPDATE ChiTietPhieuMuon SET NgayTra=CAST(GETDATE() AS DATE),TienPhat=@fine,SoLuongMat=@lost,TienDenMatSach=@comp WHERE MaPhieuMuon=@pm AND MaSach=@s AND NgayTra IS NULL", conn, tran))
                {
                    updateCmd.Parameters.AddWithValue("@fine", fine);
                    updateCmd.Parameters.AddWithValue("@lost", request.SoLuongMat);
                    updateCmd.Parameters.AddWithValue("@comp", compensation);
                    updateCmd.Parameters.AddWithValue("@pm", maPM);
                    updateCmd.Parameters.AddWithValue("@s", request.MaSach);
                    if (updateCmd.ExecuteNonQuery() != 1) { failureReason = "Dữ liệu đã thay đổi, vui lòng thử lại."; return false; }
                }

                int returnedQuantity = detail.Quantity - request.SoLuongMat;
                if (returnedQuantity > 0)
                {
                    using var stockCmd = new SqlCommand("UPDATE Sach SET SoLuong=SoLuong+@q WHERE MaSach=@s", conn, tran);
                    stockCmd.Parameters.AddWithValue("@q", returnedQuantity);
                    stockCmd.Parameters.AddWithValue("@s", request.MaSach);
                    if (stockCmd.ExecuteNonQuery() != 1) { failureReason = "Không tìm thấy sách để hoàn tồn kho."; return false; }
                }
            }

            using (var statusCmd = new SqlCommand(@"IF EXISTS (SELECT 1 FROM ChiTietPhieuMuon WHERE MaPhieuMuon=@pm AND NgayTra IS NULL)
                                                   UPDATE PhieuMuon SET TrangThai=N'Đã trả một phần' WHERE MaPhieuMuon=@pm
                                                   ELSE UPDATE PhieuMuon SET TrangThai=N'Đã trả' WHERE MaPhieuMuon=@pm", conn, tran))
            {
                statusCmd.Parameters.AddWithValue("@pm", maPM);
                statusCmd.ExecuteNonQuery();
            }
            return true;
        }

        public static bool UpdateReturnedLoanPenalty(int maPM, decimal totalPenalty, int actorMaNV, out string? failureReason)
        {
            failureReason = null;
            if (totalPenalty < 0 || decimal.Truncate(totalPenalty) != totalPenalty) { failureReason = "Tiền phạt phải là số nguyên không âm."; return false; }
            using var conn = GetConnection(); conn.Open(); using var tran = conn.BeginTransaction();
            try
            {
                using (var cmd = new SqlCommand("SELECT 1 FROM NhanVien WITH (UPDLOCK,HOLDLOCK) WHERE MaNV=@nv AND VaiTro='Admin' AND TrangThai=1", conn, tran)) { cmd.Parameters.AddWithValue("@nv", actorMaNV); if (cmd.ExecuteScalar() == null) { failureReason = "Chỉ Admin đang hoạt động mới được sửa tiền phạt."; tran.Rollback(); return false; } }
                using (var cmd = new SqlCommand("SELECT TrangThai FROM PhieuMuon WITH (UPDLOCK,HOLDLOCK) WHERE MaPhieuMuon=@pm", conn, tran)) { cmd.Parameters.AddWithValue("@pm", maPM); if (cmd.ExecuteScalar()?.ToString() != "Đã trả") { failureReason = "Chỉ phiếu đã trả hết mới được sửa tiền phạt."; tran.Rollback(); return false; } }
                var details = new List<(int MaSach, int SoLuong)>(); using (var cmd = new SqlCommand("SELECT MaSach,SoLuong FROM ChiTietPhieuMuon WITH (UPDLOCK,HOLDLOCK) WHERE MaPhieuMuon=@pm AND NgayTra IS NOT NULL ORDER BY MaSach", conn, tran)) { cmd.Parameters.AddWithValue("@pm", maPM); using var rd = cmd.ExecuteReader(); while (rd.Read()) details.Add((rd.GetInt32(0), rd.GetInt32(1))); }
                int totalQuantity = details.Sum(x => x.SoLuong);
                if (totalQuantity <= 0) { failureReason = "Phiếu không có sách đã trả."; tran.Rollback(); return false; }
                decimal baseFine = decimal.Floor(totalPenalty / totalQuantity);
                decimal allocated = 0;
                for (int i = 0; i < details.Count; i++)
                {
                    decimal fine = i == details.Count - 1 ? totalPenalty - allocated : baseFine * details[i].SoLuong;
                    allocated += fine;
                    using var cmd = new SqlCommand("UPDATE ChiTietPhieuMuon SET TienPhat=@tp WHERE MaPhieuMuon=@pm AND MaSach=@s AND NgayTra IS NOT NULL", conn, tran); cmd.Parameters.AddWithValue("@tp", fine); cmd.Parameters.AddWithValue("@pm", maPM); cmd.Parameters.AddWithValue("@s", details[i].MaSach); cmd.ExecuteNonQuery();
                }
                tran.Commit(); return true;
            }
            catch { tran.Rollback(); throw; }
        }

        public static bool ExtendPhieuMuon(int maPM, DateTime newDueDate, out string? failureReason)
        {
            failureReason = null;
            using var conn = GetConnection();
            conn.Open();
            using var tran = conn.BeginTransaction(IsolationLevel.Serializable);
            try
            {
                string? status = null;
                DateTime currentDue = default;
                using (var cmd = new SqlCommand("SELECT TrangThai,HanTra FROM PhieuMuon WITH (UPDLOCK,HOLDLOCK) WHERE MaPhieuMuon=@ma", conn, tran))
                {
                    cmd.Parameters.Add(new SqlParameter("@ma", SqlDbType.Int) { Value = maPM });
                    using var reader = cmd.ExecuteReader();
                    if (!reader.Read())
                    {
                        failureReason = "Không tìm thấy phiếu mượn.";
                        tran.Rollback();
                        return false;
                    }
                    status = reader.IsDBNull(0) ? null : reader.GetString(0);
                    currentDue = reader.GetDateTime(1).Date;
                }
                if (status is not ("Đang mượn" or "Đã trả một phần"))
                {
                    failureReason = "Chỉ phiếu còn sách chưa trả mới được gia hạn.";
                    tran.Rollback();
                    return false;
                }
                if (newDueDate.Date < currentDue)
                {
                    failureReason = "Hạn mới không được sớm hơn hạn hiện tại.";
                    tran.Rollback();
                    return false;
                }
                using (var cmd = new SqlCommand("UPDATE PhieuMuon SET HanTra=@han WHERE MaPhieuMuon=@ma", conn, tran))
                {
                    cmd.Parameters.Add(new SqlParameter("@han", SqlDbType.Date) { Value = newDueDate.Date });
                    cmd.Parameters.Add(new SqlParameter("@ma", SqlDbType.Int) { Value = maPM });
                    if (cmd.ExecuteNonQuery() != 1)
                    {
                        failureReason = "Dữ liệu đã thay đổi, vui lòng tải lại.";
                        tran.Rollback();
                        return false;
                    }
                }
                tran.Commit();
                return true;
            }
            catch
            {
                if (tran.Connection != null) tran.Rollback();
                throw;
            }
        }

        public static DataTable GetLoanPaymentSummary(int maPM) =>
            ExecuteQuery(@"SELECT
                               ISNULL((SELECT SUM(ct.TienPhat + ct.TienDenMatSach) FROM ChiTietPhieuMuon ct WHERE ct.MaPhieuMuon=pm.MaPhieuMuon),0) AS TongPhaiThu,
                               ISNULL((SELECT SUM(tp.SoTien) FROM ThanhToanPhat tp WHERE tp.MaPhieuMuon=pm.MaPhieuMuon),0) AS DaThu,
                               ISNULL((SELECT SUM(ct.TienPhat + ct.TienDenMatSach) FROM ChiTietPhieuMuon ct WHERE ct.MaPhieuMuon=pm.MaPhieuMuon),0)
                                   - ISNULL((SELECT SUM(tp.SoTien) FROM ThanhToanPhat tp WHERE tp.MaPhieuMuon=pm.MaPhieuMuon),0) AS ConLai
                           FROM PhieuMuon pm
                           WHERE pm.MaPhieuMuon=@ma",
                new SqlParameter("@ma", SqlDbType.Int) { Value = maPM });

        public static DataTable GetLoanPayments(int maPM) =>
            ExecuteQuery(@"SELECT tp.MaThanhToan, tp.SoTien, tp.NgayThu, tp.GhiChu, nv.HoTen AS TenNhanVien
                           FROM ThanhToanPhat tp
                           LEFT JOIN NhanVien nv ON nv.MaNV=tp.MaNV
                           WHERE tp.MaPhieuMuon=@ma
                           ORDER BY tp.NgayThu DESC, tp.MaThanhToan DESC",
                new SqlParameter("@ma", SqlDbType.Int) { Value = maPM });

        public static bool AddFinePayment(int maPM, decimal amount, int actorMaNV, string? note, out string? failureReason)
        {
            failureReason = null;
            if (amount <= 0)
            {
                failureReason = "Số tiền thu phải lớn hơn 0.";
                return false;
            }
            using var conn = GetConnection();
            conn.Open();
            using var tran = conn.BeginTransaction(IsolationLevel.Serializable);
            try
            {
                decimal totalDue;
                decimal paid;
                using (var cmd = new SqlCommand(@"SELECT ISNULL(SUM(TienPhat + TienDenMatSach),0)
                                                   FROM ChiTietPhieuMuon WITH (UPDLOCK,HOLDLOCK)
                                                   WHERE MaPhieuMuon=@ma", conn, tran))
                {
                    cmd.Parameters.Add(new SqlParameter("@ma", SqlDbType.Int) { Value = maPM });
                    totalDue = Convert.ToDecimal(cmd.ExecuteScalar() ?? 0m);
                }
                using (var cmd = new SqlCommand("SELECT ISNULL(SUM(SoTien),0) FROM ThanhToanPhat WITH (UPDLOCK,HOLDLOCK) WHERE MaPhieuMuon=@ma", conn, tran))
                {
                    cmd.Parameters.Add(new SqlParameter("@ma", SqlDbType.Int) { Value = maPM });
                    paid = Convert.ToDecimal(cmd.ExecuteScalar() ?? 0m);
                }
                decimal remaining = totalDue - paid;
                if (remaining <= 0)
                {
                    failureReason = "Phiếu này đã thu đủ tiền.";
                    tran.Rollback();
                    return false;
                }
                if (amount > remaining)
                {
                    failureReason = $"Số tiền thu không được vượt quá {remaining:N0}đ còn lại.";
                    tran.Rollback();
                    return false;
                }
                using (var cmd = new SqlCommand(@"INSERT INTO ThanhToanPhat(MaPhieuMuon,SoTien,MaNV,GhiChu)
                                                  VALUES(@pm,@soTien,@nv,@ghiChu)", conn, tran))
                {
                    cmd.Parameters.Add(new SqlParameter("@pm", SqlDbType.Int) { Value = maPM });
                    cmd.Parameters.Add(new SqlParameter("@soTien", SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = amount });
                    cmd.Parameters.Add(new SqlParameter("@nv", SqlDbType.Int) { Value = actorMaNV });
                    cmd.Parameters.Add(new SqlParameter("@ghiChu", SqlDbType.NVarChar, 250) { Value = note?.Trim() ?? string.Empty });
                    cmd.ExecuteNonQuery();
                }
                tran.Commit();
                return true;
            }
            catch (SqlException ex) when (ex.Number == 547)
            {
                if (tran.Connection != null) tran.Rollback();
                failureReason = "Không thể ghi nhận thanh toán cho phiếu này.";
                return false;
            }
            catch
            {
                if (tran.Connection != null) tran.Rollback();
                throw;
            }
        }

        // ---- Dashboard Statistics ----
        public static int CountSach() =>
            Convert.ToInt32(ExecuteScalar("SELECT COUNT(*) FROM Sach"));

        public static int CountDocGia() =>
            Convert.ToInt32(ExecuteScalar("SELECT COUNT(*) FROM DocGia"));

        public static int CountPhieuMuonDangMo() =>
            Convert.ToInt32(ExecuteScalar("SELECT COUNT(*) FROM PhieuMuon WHERE TrangThai IN (N'Đang mượn',N'Đã trả một phần')"));

        public static int CountQuaHan() =>
            Convert.ToInt32(ExecuteScalar("SELECT COUNT(*) FROM PhieuMuon WHERE HanTra<CAST(GETDATE() AS DATE) AND TrangThai IN (N'Đang mượn',N'Đã trả một phần')"));

        public static int CountDocGiaSapHetHan() =>
            Convert.ToInt32(ExecuteScalar("SELECT COUNT(*) FROM DocGia WHERE TrangThai=1 AND HanSuDung BETWEEN CAST(GETDATE() AS DATE) AND DATEADD(DAY,30,CAST(GETDATE() AS DATE))"));

        public static decimal GetTongTienChuaThu() =>
            Convert.ToDecimal(ExecuteScalar(@"SELECT ISNULL(SUM(t.TongPhaiThu - t.DaThu),0)
                                              FROM (
                                                  SELECT pm.MaPhieuMuon,
                                                         ISNULL((SELECT SUM(ct.TienPhat + ct.TienDenMatSach) FROM ChiTietPhieuMuon ct WHERE ct.MaPhieuMuon=pm.MaPhieuMuon),0) AS TongPhaiThu,
                                                         ISNULL((SELECT SUM(tp.SoTien) FROM ThanhToanPhat tp WHERE tp.MaPhieuMuon=pm.MaPhieuMuon),0) AS DaThu
                                                  FROM PhieuMuon pm
                                              ) t
                                              WHERE t.TongPhaiThu > t.DaThu"));

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

-- Du lieu demo cho do an QuanLyThuVien.
-- Script chi them cac ban ghi DEMO chua ton tai va co the chay lai an toan.
USE QuanLyThuVien;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.NhanVien', N'U') IS NULL
        THROW 51001, 'Khong tim thay bang NhanVien.', 1;
    IF OBJECT_ID(N'dbo.TheLoai', N'U') IS NULL
        THROW 51002, 'Khong tim thay bang TheLoai.', 1;
    IF OBJECT_ID(N'dbo.TacGia', N'U') IS NULL
        THROW 51003, 'Khong tim thay bang TacGia.', 1;
    IF OBJECT_ID(N'dbo.NhaXuatBan', N'U') IS NULL
        THROW 51004, 'Khong tim thay bang NhaXuatBan.', 1;
    IF OBJECT_ID(N'dbo.Sach', N'U') IS NULL
        THROW 51005, 'Khong tim thay bang Sach.', 1;
    IF OBJECT_ID(N'dbo.DocGia', N'U') IS NULL
        THROW 51006, 'Khong tim thay bang DocGia.', 1;
    IF OBJECT_ID(N'dbo.PhieuMuon', N'U') IS NULL
        THROW 51007, 'Khong tim thay bang PhieuMuon.', 1;
    IF OBJECT_ID(N'dbo.ChiTietPhieuMuon', N'U') IS NULL
        THROW 51008, 'Khong tim thay bang ChiTietPhieuMuon.', 1;
    IF OBJECT_ID(N'dbo.ThanhToanPhat', N'U') IS NULL
        THROW 51009, 'Khong tim thay bang ThanhToanPhat. Hay chay migration 2026-07-21-003 truoc.', 1;
    IF COL_LENGTH(N'dbo.ChiTietPhieuMuon', N'SoLuongMat') IS NULL
       OR COL_LENGTH(N'dbo.ChiTietPhieuMuon', N'TienDenMatSach') IS NULL
        THROW 51010, 'Thieu cot xu ly sach mat. Hay chay migration 2026-07-21-002 truoc.', 1;

    DECLARE @Genres TABLE
    (
        TenTheLoai NVARCHAR(100) NOT NULL PRIMARY KEY
    );

    INSERT INTO @Genres(TenTheLoai)
    VALUES
        (N'Văn học'),
        (N'Khoa học'),
        (N'Lịch sử'),
        (N'Tin học'),
        (N'Truyện tranh'),
        (N'Thiếu nhi'),
        (N'Kỹ năng sống'),
        (N'Trinh thám');

    INSERT INTO dbo.TheLoai(TenTheLoai)
    SELECT g.TenTheLoai
    FROM @Genres AS g
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.TheLoai AS currentGenre
        WHERE currentGenre.TenTheLoai = g.TenTheLoai
    );

    DECLARE @Authors TABLE
    (
        TenTG NVARCHAR(100) NOT NULL PRIMARY KEY,
        QuocTich NVARCHAR(50) NOT NULL,
        GhiChu NVARCHAR(200) NOT NULL
    );

    INSERT INTO @Authors(TenTG, QuocTich, GhiChu)
    VALUES
        (N'Nguyễn An Nhiên', N'Việt Nam', N'Tác giả dữ liệu demo'),
        (N'Trần Hải Đăng', N'Việt Nam', N'Tác giả dữ liệu demo'),
        (N'Phạm Thu Lam', N'Việt Nam', N'Tác giả dữ liệu demo'),
        (N'Vũ Thành Nam', N'Việt Nam', N'Tác giả dữ liệu demo'),
        (N'Đỗ Minh Khoa', N'Việt Nam', N'Tác giả dữ liệu demo'),
        (N'Nguyễn Gia Bảo', N'Việt Nam', N'Tác giả dữ liệu demo'),
        (N'Mai Linh Chi', N'Việt Nam', N'Tác giả dữ liệu demo'),
        (N'Bùi An Khánh', N'Việt Nam', N'Tác giả dữ liệu demo'),
        (N'Lê Quang Vũ', N'Việt Nam', N'Tác giả dữ liệu demo');

    INSERT INTO dbo.TacGia(TenTG, QuocTich, GhiChu)
    SELECT a.TenTG, a.QuocTich, a.GhiChu
    FROM @Authors AS a
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.TacGia AS currentAuthor
        WHERE currentAuthor.TenTG = a.TenTG
    );

    DECLARE @Publishers TABLE
    (
        TenNXB NVARCHAR(150) NOT NULL PRIMARY KEY,
        DiaChi NVARCHAR(200) NOT NULL,
        SoDienThoai VARCHAR(20) NOT NULL
    );

    INSERT INTO @Publishers(TenNXB, DiaChi, SoDienThoai)
    VALUES
        (N'NXB Trẻ', N'TP. Hồ Chí Minh', '02838223333'),
        (N'NXB Kim Đồng', N'Hà Nội', '02439434730'),
        (N'NXB Văn Học', N'Hà Nội', '02438222135'),
        (N'NXB Khoa Học và Kỹ Thuật', N'Hà Nội', '02438220686'),
        (N'NXB Tổng Hợp TP.HCM', N'TP. Hồ Chí Minh', '02838225340');

    INSERT INTO dbo.NhaXuatBan(TenNXB, DiaChi, SoDienThoai)
    SELECT p.TenNXB, p.DiaChi, p.SoDienThoai
    FROM @Publishers AS p
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.NhaXuatBan AS currentPublisher
        WHERE currentPublisher.TenNXB = p.TenNXB
    );

    DECLARE @Books TABLE
    (
        MaISBN VARCHAR(20) NOT NULL PRIMARY KEY,
        TenSach NVARCHAR(200) NOT NULL,
        TenTheLoai NVARCHAR(100) NOT NULL,
        TenTG NVARCHAR(100) NOT NULL,
        TenNXB NVARCHAR(150) NOT NULL,
        NamXB INT NOT NULL,
        SoLuong INT NOT NULL,
        GiaTien DECIMAL(18,2) NOT NULL,
        MoTa NVARCHAR(500) NOT NULL
    );

    INSERT INTO @Books(MaISBN, TenSach, TenTheLoai, TenTG, TenNXB, NamXB, SoLuong, GiaTien, MoTa)
    VALUES
        ('DEMO-QLTV-0001', N'Phố Ven Sông', N'Văn học', N'Nguyễn An Nhiên', N'NXB Trẻ', 2023, 8, 92000, N'Một câu chuyện nhẹ nhàng về ký ức và tình bạn bên dòng sông.'),
        ('DEMO-QLTV-0002', N'Ngọn Đèn Trong Thư Viện', N'Thiếu nhi', N'Trần Hải Đăng', N'NXB Kim Đồng', 2024, 7, 78000, N'Chuyến phiêu lưu kỳ ảo trong một thư viện chỉ mở cửa lúc nửa đêm.'),
        ('DEMO-QLTV-0003', N'Mưa Qua Rừng Sâu', N'Văn học', N'Phạm Thu Lam', N'NXB Văn Học', 2022, 6, 105000, N'Hành trình đi tìm bình yên giữa một mùa mưa dài.'),
        ('DEMO-QLTV-0004', N'La Bàn Của Thời Gian', N'Lịch sử', N'Vũ Thành Nam', N'NXB Tổng Hợp TP.HCM', 2021, 5, 135000, N'Những câu chuyện lịch sử được kể qua các tấm bản đồ cổ.'),
        ('DEMO-QLTV-0005', N'Bầu Trời Trong Một Trang Sách', N'Khoa học', N'Đỗ Minh Khoa', N'NXB Khoa Học và Kỹ Thuật', 2024, 9, 118000, N'Kiến thức thiên văn cơ bản dành cho người mới bắt đầu.'),
        ('DEMO-QLTV-0006', N'Thành Phố Mã Nguồn', N'Tin học', N'Nguyễn Gia Bảo', N'NXB Khoa Học và Kỹ Thuật', 2025, 10, 149000, N'Nhập môn tư duy lập trình qua các bài toán gần gũi.'),
        ('DEMO-QLTV-0007', N'Con Thuyền Giấy Ra Khơi', N'Thiếu nhi', N'Mai Linh Chi', N'NXB Kim Đồng', 2023, 8, 68000, N'Câu chuyện về lòng dũng cảm của một con thuyền giấy nhỏ.'),
        ('DEMO-QLTV-0008', N'Từng Bậc Đến Bình Minh', N'Kỹ năng sống', N'Bùi An Khánh', N'NXB Trẻ', 2024, 11, 98000, N'Những thói quen nhỏ giúp xây dựng một ngày làm việc hiệu quả.'),
        ('DEMO-QLTV-0009', N'Bí Mật Căn Phòng Số 8', N'Trinh thám', N'Lê Quang Vũ', N'NXB Văn Học', 2022, 6, 112000, N'Một vụ án khép kín bắt đầu từ căn phòng không có cửa sổ.'),
        ('DEMO-QLTV-0010', N'Đường Về Qua Mùa Mưa', N'Văn học', N'Phạm Thu Lam', N'NXB Văn Học', 2021, 7, 89000, N'Những cuộc gặp gỡ tình cờ trên đường trở về quê cũ.'),
        ('DEMO-QLTV-0011', N'Atlas Những Vì Sao Gần', N'Khoa học', N'Đỗ Minh Khoa', N'NXB Khoa Học và Kỹ Thuật', 2025, 5, 165000, N'Bản đồ minh họa các chòm sao dễ quan sát từ Việt Nam.'),
        ('DEMO-QLTV-0012', N'Lập Trình Từ Những Điều Nhỏ', N'Tin học', N'Nguyễn Gia Bảo', N'NXB Khoa Học và Kỹ Thuật', 2024, 12, 155000, N'Các dự án nhỏ giúp rèn luyện kỹ năng giải quyết vấn đề.'),
        ('DEMO-QLTV-0013', N'Chuyến Tàu Đến Hải Đăng', N'Thiếu nhi', N'Trần Hải Đăng', N'NXB Kim Đồng', 2022, 9, 72000, N'Một chuyến tàu đêm đưa nhóm bạn đến hòn đảo bí ẩn.'),
        ('DEMO-QLTV-0014', N'Dấu Chân Trên Bản Đồ Cổ', N'Lịch sử', N'Vũ Thành Nam', N'NXB Tổng Hợp TP.HCM', 2020, 5, 128000, N'Khám phá những tuyến giao thương xưa trên đất Việt.'),
        ('DEMO-QLTV-0015', N'Khoảng Lặng Cuối Con Phố', N'Văn học', N'Nguyễn An Nhiên', N'NXB Trẻ', 2023, 8, 95000, N'Tập truyện ngắn về những con người bình dị trong thành phố.'),
        ('DEMO-QLTV-0016', N'Mật Thư Dưới Gầm Cầu', N'Trinh thám', N'Lê Quang Vũ', N'NXB Văn Học', 2024, 6, 119000, N'Một mật thư cũ mở ra chuỗi manh mối xuyên thành phố.'),
        ('DEMO-QLTV-0017', N'Thói Quen Của Ngày Mới', N'Kỹ năng sống', N'Bùi An Khánh', N'NXB Trẻ', 2025, 10, 99000, N'Hướng dẫn thực hành xây dựng thói quen tích cực.'),
        ('DEMO-QLTV-0018', N'Khoa Học Trong Căn Bếp', N'Khoa học', N'Đỗ Minh Khoa', N'NXB Khoa Học và Kỹ Thuật', 2023, 7, 108000, N'Giải thích các hiện tượng khoa học qua những món ăn quen thuộc.'),
        ('DEMO-QLTV-0019', N'Robot Học Cách Kết Bạn', N'Truyện tranh', N'Mai Linh Chi', N'NXB Kim Đồng', 2025, 12, 82000, N'Truyện tranh vui nhộn về một robot lần đầu đến trường.'),
        ('DEMO-QLTV-0020', N'Biển Gọi Tên Mùa Hè', N'Văn học', N'Nguyễn An Nhiên', N'NXB Trẻ', 2022, 8, 88000, N'Câu chuyện tuổi trẻ bên một làng chài nhỏ.');

    INSERT INTO dbo.Sach(TenSach, MaISBN, MaTL, MaTG, MaNXB, NamXB, SoLuong, GiaTien, MoTa, HinhAnh)
    SELECT
        b.TenSach,
        b.MaISBN,
        genre.MaTL,
        author.MaTG,
        publisher.MaNXB,
        b.NamXB,
        b.SoLuong,
        b.GiaTien,
        b.MoTa,
        N''
    FROM @Books AS b
    CROSS APPLY
    (
        SELECT TOP (1) tl.MaTL
        FROM dbo.TheLoai AS tl
        WHERE tl.TenTheLoai = b.TenTheLoai
        ORDER BY tl.MaTL
    ) AS genre
    CROSS APPLY
    (
        SELECT TOP (1) tg.MaTG
        FROM dbo.TacGia AS tg
        WHERE tg.TenTG = b.TenTG
        ORDER BY tg.MaTG
    ) AS author
    CROSS APPLY
    (
        SELECT TOP (1) nxb.MaNXB
        FROM dbo.NhaXuatBan AS nxb
        WHERE nxb.TenNXB = b.TenNXB
        ORDER BY nxb.MaNXB
    ) AS publisher
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.Sach AS currentBook
        WHERE currentBook.MaISBN = b.MaISBN
    );

    DECLARE @Today DATE = CAST(GETDATE() AS DATE);
    DECLARE @Readers TABLE
    (
        Email VARCHAR(100) NOT NULL PRIMARY KEY,
        HoTen NVARCHAR(100) NOT NULL,
        NgaySinh DATE NOT NULL,
        GioiTinh NVARCHAR(5) NOT NULL,
        SoDienThoai VARCHAR(20) NOT NULL
    );

    INSERT INTO @Readers(Email, HoTen, NgaySinh, GioiTinh, SoDienThoai)
    VALUES
        ('demo.dg01@qltv.local', N'Nguyễn Minh Anh', '2001-03-12', N'Nữ', '0901000001'),
        ('demo.dg02@qltv.local', N'Trần Quốc Bảo', '1999-07-25', N'Nam', '0901000002'),
        ('demo.dg03@qltv.local', N'Lê Thanh Chi', '2002-11-08', N'Nữ', '0901000003'),
        ('demo.dg04@qltv.local', N'Phạm Hoàng Duy', '1998-05-19', N'Nam', '0901000004'),
        ('demo.dg05@qltv.local', N'Võ Ngọc Hà', '2000-09-30', N'Nữ', '0901000005'),
        ('demo.dg06@qltv.local', N'Đặng Tuấn Kiệt', '1997-01-14', N'Nam', '0901000006'),
        ('demo.dg07@qltv.local', N'Bùi Khánh Linh', '2003-06-21', N'Nữ', '0901000007'),
        ('demo.dg08@qltv.local', N'Hồ Gia Minh', '2001-12-03', N'Nam', '0901000008'),
        ('demo.dg09@qltv.local', N'Ngô Phương Nhi', '1999-04-17', N'Nữ', '0901000009'),
        ('demo.dg10@qltv.local', N'Dương Đức Phúc', '2002-08-09', N'Nam', '0901000010');

    INSERT INTO dbo.DocGia(HoTen, NgaySinh, GioiTinh, SoDienThoai, Email, NgayLapThe, HanSuDung, TrangThai)
    SELECT
        r.HoTen,
        r.NgaySinh,
        r.GioiTinh,
        r.SoDienThoai,
        r.Email,
        DATEADD(DAY, -45, @Today),
        DATEADD(YEAR, 1, @Today),
        1
    FROM @Readers AS r
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.DocGia AS currentReader
        WHERE currentReader.Email = r.Email
    );

    DECLARE @LegacyDemoPasswordHash VARCHAR(256) =
        'ff96673205dc722320598ebf8f88325b2ac56922d5a2164b5765868274bc0d73';

    DECLARE @Employees TABLE
    (
        TenDangNhap VARCHAR(50) NOT NULL PRIMARY KEY,
        HoTen NVARCHAR(100) NOT NULL,
        VaiTro NVARCHAR(20) NOT NULL
    );

    INSERT INTO @Employees(TenDangNhap, HoTen, VaiTro)
    VALUES
        ('demo_admin', N'Quản trị Demo', N'Admin'),
        ('demo_thuthu01', N'Thủ thư Lan', N'NhanVien'),
        ('demo_thuthu02', N'Thủ thư Minh', N'NhanVien'),
        ('demo_thuthu03', N'Thủ thư Phương', N'NhanVien');

    INSERT INTO dbo.NhanVien(HoTen, TenDangNhap, MatKhau, VaiTro, TrangThai)
    SELECT e.HoTen, e.TenDangNhap, @LegacyDemoPasswordHash, e.VaiTro, 1
    FROM @Employees AS e
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.NhanVien AS currentEmployee
        WHERE currentEmployee.TenDangNhap = e.TenDangNhap
    );

    DECLARE @DemoLoans TABLE
    (
        DemoCode VARCHAR(20) NOT NULL PRIMARY KEY,
        ReaderEmail VARCHAR(100) NOT NULL,
        StaffUser VARCHAR(50) NOT NULL,
        NgayMuon DATE NOT NULL,
        HanTra DATE NOT NULL,
        TrangThai NVARCHAR(20) NOT NULL,
        MaPhieuMuon INT NULL
    );

    INSERT INTO @DemoLoans(DemoCode, ReaderEmail, StaffUser, NgayMuon, HanTra, TrangThai)
    VALUES
        ('DEMO-PM-01', 'demo.dg01@qltv.local', 'demo_thuthu01', '2026-07-20', '2026-08-03', N'Đang mượn'),
        ('DEMO-PM-02', 'demo.dg02@qltv.local', 'demo_thuthu01', '2026-07-18', '2026-08-01', N'Đang mượn'),
        ('DEMO-PM-03', 'demo.dg03@qltv.local', 'demo_thuthu02', '2026-06-28', '2026-07-12', N'Đang mượn'),
        ('DEMO-PM-04', 'demo.dg04@qltv.local', 'demo_thuthu02', '2026-06-25', '2026-07-09', N'Đã trả một phần'),
        ('DEMO-PM-05', 'demo.dg05@qltv.local', 'demo_thuthu03', '2026-06-15', '2026-06-29', N'Đã trả'),
        ('DEMO-PM-06', 'demo.dg06@qltv.local', 'demo_thuthu01', '2026-06-10', '2026-06-24', N'Đã trả'),
        ('DEMO-PM-07', 'demo.dg07@qltv.local', 'demo_thuthu02', '2026-05-20', '2026-06-03', N'Đã trả'),
        ('DEMO-PM-08', 'demo.dg08@qltv.local', 'demo_thuthu03', '2026-07-10', '2026-07-24', N'Đang mượn'),
        ('DEMO-PM-09', 'demo.dg09@qltv.local', 'demo_thuthu01', '2026-07-01', '2026-07-15', N'Đang mượn'),
        ('DEMO-PM-10', 'demo.dg10@qltv.local', 'demo_thuthu02', '2026-06-01', '2026-06-15', N'Đã trả'),
        ('DEMO-PM-11', 'demo.dg01@qltv.local', 'demo_thuthu03', '2026-06-20', '2026-07-04', N'Đã trả một phần'),
        ('DEMO-PM-12', 'demo.dg02@qltv.local', 'demo_thuthu02', '2026-05-25', '2026-06-08', N'Đã trả');

    DECLARE @DemoLoanDetails TABLE
    (
        DemoCode VARCHAR(20) NOT NULL,
        MaISBN VARCHAR(20) NOT NULL,
        SoLuong INT NOT NULL,
        NgayTra DATE NULL,
        TienPhat DECIMAL(18,2) NOT NULL,
        SoLuongMat INT NOT NULL,
        PRIMARY KEY (DemoCode, MaISBN)
    );

    INSERT INTO @DemoLoanDetails(DemoCode, MaISBN, SoLuong, NgayTra, TienPhat, SoLuongMat)
    VALUES
        ('DEMO-PM-01', 'DEMO-QLTV-0001', 1, NULL, 0, 0),
        ('DEMO-PM-02', 'DEMO-QLTV-0002', 1, NULL, 0, 0),
        ('DEMO-PM-02', 'DEMO-QLTV-0005', 1, NULL, 0, 0),
        ('DEMO-PM-03', 'DEMO-QLTV-0003', 1, NULL, 0, 0),
        ('DEMO-PM-04', 'DEMO-QLTV-0004', 1, '2026-07-08', 0, 0),
        ('DEMO-PM-04', 'DEMO-QLTV-0009', 1, NULL, 0, 0),
        ('DEMO-PM-05', 'DEMO-QLTV-0005', 1, '2026-06-28', 0, 0),
        ('DEMO-PM-06', 'DEMO-QLTV-0006', 1, '2026-06-27', 15000, 0),
        ('DEMO-PM-07', 'DEMO-QLTV-0007', 1, '2026-06-03', 0, 1),
        ('DEMO-PM-08', 'DEMO-QLTV-0008', 1, NULL, 0, 0),
        ('DEMO-PM-09', 'DEMO-QLTV-0010', 1, NULL, 0, 0),
        ('DEMO-PM-10', 'DEMO-QLTV-0011', 1, '2026-06-18', 24000, 0),
        ('DEMO-PM-11', 'DEMO-QLTV-0012', 1, '2026-07-02', 0, 0),
        ('DEMO-PM-11', 'DEMO-QLTV-0013', 1, NULL, 0, 0),
        ('DEMO-PM-12', 'DEMO-QLTV-0014', 1, '2026-06-12', 32000, 0);

    DECLARE @DemoCode VARCHAR(20);
    DECLARE @ReaderEmail VARCHAR(100);
    DECLARE @StaffUser VARCHAR(50);
    DECLARE @NgayMuon DATE;
    DECLARE @HanTra DATE;
    DECLARE @TrangThai NVARCHAR(20);
    DECLARE @MaPhieuMuon INT;
    DECLARE @MaDG INT;
    DECLARE @MaNV INT;
    DECLARE @StockDelta TABLE
    (
        MaSach INT NOT NULL PRIMARY KEY,
        SoLuongCanTru INT NOT NULL
    );

    DECLARE demo_loan_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT DemoCode, ReaderEmail, StaffUser, NgayMuon, HanTra, TrangThai
        FROM @DemoLoans
        ORDER BY DemoCode;

    OPEN demo_loan_cursor;
    FETCH NEXT FROM demo_loan_cursor
        INTO @DemoCode, @ReaderEmail, @StaffUser, @NgayMuon, @HanTra, @TrangThai;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @MaPhieuMuon = NULL;
        SET @MaDG = NULL;
        SET @MaNV = NULL;

        SELECT TOP (1) @MaDG = dg.MaDG
        FROM dbo.DocGia AS dg
        WHERE dg.Email = @ReaderEmail
        ORDER BY dg.MaDG;

        SELECT TOP (1) @MaNV = nv.MaNV
        FROM dbo.NhanVien AS nv
        WHERE nv.TenDangNhap = @StaffUser
        ORDER BY nv.MaNV;

        IF @MaDG IS NULL OR @MaNV IS NULL
            THROW 51011, 'Khong tim thay doc gia hoac nhan vien demo cho phieu muon.', 1;

        SELECT TOP (1) @MaPhieuMuon = pm.MaPhieuMuon
        FROM dbo.PhieuMuon AS pm
        WHERE pm.MaDG = @MaDG
          AND pm.MaNV = @MaNV
          AND pm.NgayMuon = @NgayMuon
          AND pm.HanTra = @HanTra
        ORDER BY pm.MaPhieuMuon;

        IF @MaPhieuMuon IS NULL
        BEGIN
            INSERT INTO dbo.PhieuMuon(MaDG, MaNV, NgayMuon, HanTra, TrangThai)
            VALUES(@MaDG, @MaNV, @NgayMuon, @HanTra, @TrangThai);

            SET @MaPhieuMuon = CONVERT(INT, SCOPE_IDENTITY());
            DELETE FROM @StockDelta;

            INSERT INTO @StockDelta(MaSach, SoLuongCanTru)
            SELECT
                s.MaSach,
                SUM(CASE WHEN d.NgayTra IS NULL THEN d.SoLuong ELSE d.SoLuongMat END)
            FROM @DemoLoanDetails AS d
            JOIN dbo.Sach AS s ON s.MaISBN = d.MaISBN
            WHERE d.DemoCode = @DemoCode
            GROUP BY s.MaSach;

            IF EXISTS
            (
                SELECT 1
                FROM @StockDelta AS delta
                JOIN dbo.Sach AS s ON s.MaSach = delta.MaSach
                WHERE s.SoLuong < delta.SoLuongCanTru
            )
                THROW 51012, 'Khong du ton kho de tao phieu muon demo.', 1;

            UPDATE s
            SET s.SoLuong = s.SoLuong - delta.SoLuongCanTru
            FROM dbo.Sach AS s
            JOIN @StockDelta AS delta ON delta.MaSach = s.MaSach
            WHERE delta.SoLuongCanTru > 0;

            INSERT INTO dbo.ChiTietPhieuMuon
                (MaPhieuMuon, MaSach, SoLuong, NgayTra, TienPhat, SoLuongMat, TienDenMatSach)
            SELECT
                @MaPhieuMuon,
                s.MaSach,
                d.SoLuong,
                d.NgayTra,
                d.TienPhat,
                d.SoLuongMat,
                s.GiaTien * d.SoLuongMat
            FROM @DemoLoanDetails AS d
            JOIN dbo.Sach AS s ON s.MaISBN = d.MaISBN
            WHERE d.DemoCode = @DemoCode;
        END;

        UPDATE @DemoLoans
        SET MaPhieuMuon = @MaPhieuMuon
        WHERE DemoCode = @DemoCode;

        FETCH NEXT FROM demo_loan_cursor
            INTO @DemoCode, @ReaderEmail, @StaffUser, @NgayMuon, @HanTra, @TrangThai;
    END;

    CLOSE demo_loan_cursor;
    DEALLOCATE demo_loan_cursor;

    DECLARE @DemoPayments TABLE
    (
        DemoCode VARCHAR(20) NOT NULL,
        SoTien DECIMAL(18,2) NOT NULL,
        NgayThu DATETIME2 NOT NULL,
        StaffUser VARCHAR(50) NOT NULL,
        GhiChu NVARCHAR(250) NOT NULL,
        PRIMARY KEY (DemoCode, GhiChu)
    );

    INSERT INTO @DemoPayments(DemoCode, SoTien, NgayThu, StaffUser, GhiChu)
    VALUES
        ('DEMO-PM-06', 15000, '2026-06-27T10:00:00', 'demo_thuthu01', N'DEMO: PM06 thu đủ tiền phạt'),
        ('DEMO-PM-07', 34000, '2026-06-03T15:30:00', 'demo_thuthu02', N'DEMO: PM07 thu một phần tiền đền'),
        ('DEMO-PM-12', 10000, '2026-06-12T09:15:00', 'demo_thuthu02', N'DEMO: PM12 thu một phần tiền phạt');

    INSERT INTO dbo.ThanhToanPhat(MaPhieuMuon, SoTien, NgayThu, MaNV, GhiChu)
    SELECT
        loan.MaPhieuMuon,
        payment.SoTien,
        payment.NgayThu,
        employee.MaNV,
        payment.GhiChu
    FROM @DemoPayments AS payment
    JOIN @DemoLoans AS loan ON loan.DemoCode = payment.DemoCode
    CROSS APPLY
    (
        SELECT TOP (1) nv.MaNV
        FROM dbo.NhanVien AS nv
        WHERE nv.TenDangNhap = payment.StaffUser
        ORDER BY nv.MaNV
    ) AS employee
    WHERE loan.MaPhieuMuon IS NOT NULL
      AND NOT EXISTS
      (
          SELECT 1
          FROM dbo.ThanhToanPhat AS currentPayment
          WHERE currentPayment.MaPhieuMuon = loan.MaPhieuMuon
            AND currentPayment.GhiChu = payment.GhiChu
      );

    COMMIT TRANSACTION;

    PRINT 'Demo seed completed successfully.';
    SELECT
        (SELECT COUNT(*) FROM dbo.Sach WHERE MaISBN LIKE 'DEMO-QLTV-%') AS DemoBooks,
        (SELECT COUNT(*) FROM dbo.DocGia WHERE Email LIKE 'demo.dg%@qltv.local') AS DemoReaders,
        (SELECT COUNT(*) FROM dbo.NhanVien WHERE TenDangNhap LIKE 'demo_%') AS DemoEmployees,
        (SELECT COUNT(*)
         FROM dbo.PhieuMuon AS pm
         JOIN dbo.DocGia AS dg ON dg.MaDG = pm.MaDG
         WHERE dg.Email LIKE 'demo.dg%@qltv.local'
           AND pm.NgayMuon BETWEEN '2026-05-01' AND '2026-07-31') AS DemoLoans;
END TRY
BEGIN CATCH
    IF CURSOR_STATUS('local', 'demo_loan_cursor') >= 0
        CLOSE demo_loan_cursor;
    IF CURSOR_STATUS('local', 'demo_loan_cursor') > -3
        DEALLOCATE demo_loan_cursor;
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;

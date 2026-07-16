IF DB_ID('QuanLyThuVien') IS NULL
    CREATE DATABASE QuanLyThuVien
GO
USE QuanLyThuVien
GO

IF OBJECT_ID('NhanVien', 'U') IS NULL
CREATE TABLE NhanVien (
    MaNV INT IDENTITY(1,1) PRIMARY KEY,
    HoTen NVARCHAR(100) NOT NULL,
    TenDangNhap VARCHAR(50) UNIQUE NOT NULL,
    MatKhau VARCHAR(256) NOT NULL,
    VaiTro NVARCHAR(20) DEFAULT N'NhanVien',
    TrangThai BIT DEFAULT 1
)

IF OBJECT_ID('TheLoai', 'U') IS NULL
CREATE TABLE TheLoai (
    MaTL INT IDENTITY(1,1) PRIMARY KEY,
    TenTheLoai NVARCHAR(100) NOT NULL
)

IF OBJECT_ID('TacGia', 'U') IS NULL
CREATE TABLE TacGia (
    MaTG INT IDENTITY(1,1) PRIMARY KEY,
    TenTG NVARCHAR(100) NOT NULL,
    QuocTich NVARCHAR(50) DEFAULT '',
    GhiChu NVARCHAR(200) DEFAULT ''
)

IF OBJECT_ID('NhaXuatBan', 'U') IS NULL
CREATE TABLE NhaXuatBan (
    MaNXB INT IDENTITY(1,1) PRIMARY KEY,
    TenNXB NVARCHAR(150) NOT NULL,
    DiaChi NVARCHAR(200) DEFAULT '',
    SoDienThoai VARCHAR(20) DEFAULT ''
)

IF OBJECT_ID('Sach', 'U') IS NULL
CREATE TABLE Sach (
    MaSach INT IDENTITY(1,1) PRIMARY KEY,
    TenSach NVARCHAR(200) NOT NULL,
    MaISBN VARCHAR(20) DEFAULT '',
    MaTL INT FOREIGN KEY REFERENCES TheLoai(MaTL),
    MaTG INT FOREIGN KEY REFERENCES TacGia(MaTG),
    MaNXB INT FOREIGN KEY REFERENCES NhaXuatBan(MaNXB),
    NamXB INT DEFAULT YEAR(GETDATE()),
    SoLuong INT DEFAULT 0,
    GiaTien DECIMAL(18,2) DEFAULT 0,
    MoTa NVARCHAR(500) DEFAULT '',
    HinhAnh NVARCHAR(500) DEFAULT ''
)

IF OBJECT_ID('DocGia', 'U') IS NULL
CREATE TABLE DocGia (
    MaDG INT IDENTITY(1,1) PRIMARY KEY,
    HoTen NVARCHAR(100) NOT NULL,
    NgaySinh DATE,
    GioiTinh NVARCHAR(5) DEFAULT N'Nam',
    SoDienThoai VARCHAR(20) DEFAULT '',
    Email VARCHAR(100) DEFAULT '',
    NgayLapThe DATE DEFAULT GETDATE(),
    HanSuDung DATE DEFAULT DATEADD(YEAR,1,GETDATE()),
    TrangThai BIT DEFAULT 1
)

IF OBJECT_ID('PhieuMuon', 'U') IS NULL
CREATE TABLE PhieuMuon (
    MaPhieuMuon INT IDENTITY(1,1) PRIMARY KEY,
    MaDG INT FOREIGN KEY REFERENCES DocGia(MaDG),
    MaNV INT FOREIGN KEY REFERENCES NhanVien(MaNV),
    NgayMuon DATE DEFAULT GETDATE(),
    HanTra DATE NOT NULL DEFAULT DATEADD(DAY, 14, GETDATE()),
    TrangThai NVARCHAR(20) DEFAULT N'Đang mượn'
)

IF OBJECT_ID('ChiTietPhieuMuon', 'U') IS NULL
CREATE TABLE ChiTietPhieuMuon (
    MaPhieuMuon INT FOREIGN KEY REFERENCES PhieuMuon(MaPhieuMuon),
    MaSach INT FOREIGN KEY REFERENCES Sach(MaSach),
    SoLuong INT DEFAULT 1,
    NgayTra DATE NULL,
    TienPhat DECIMAL(18,2) DEFAULT 0,
    PRIMARY KEY (MaPhieuMuon, MaSach)
)

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_NhanVien_TenDangNhap' AND object_id = OBJECT_ID('NhanVien'))
CREATE INDEX IX_NhanVien_TenDangNhap ON NhanVien(TenDangNhap)

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PhieuMuon_MaDG' AND object_id = OBJECT_ID('PhieuMuon'))
CREATE INDEX IX_PhieuMuon_MaDG ON PhieuMuon(MaDG)

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PhieuMuon_TrangThai' AND object_id = OBJECT_ID('PhieuMuon'))
CREATE INDEX IX_PhieuMuon_TrangThai ON PhieuMuon(TrangThai)

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ChiTietPM_MaPhieuMuon' AND object_id = OBJECT_ID('ChiTietPhieuMuon'))
CREATE INDEX IX_ChiTietPM_MaPhieuMuon ON ChiTietPhieuMuon(MaPhieuMuon)

IF NOT EXISTS (SELECT 1 FROM NhanVien WHERE TenDangNhap = 'admin')
INSERT INTO NhanVien(HoTen, TenDangNhap, MatKhau, VaiTro) VALUES
(N'Admin', 'admin', '8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918', 'Admin'),
(N'Nhân viên 1', 'nhanvien1', '8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918', 'NhanVien')

IF NOT EXISTS (SELECT 1 FROM TheLoai)
INSERT INTO TheLoai(TenTheLoai) VALUES
(N'Văn học'), (N'Khoa học'), (N'Lịch sử'), (N'Tin học'), (N'Truyện tranh')

IF NOT EXISTS (SELECT 1 FROM TacGia)
INSERT INTO TacGia(TenTG, QuocTich) VALUES
(N'Nguyễn Nhật Ánh', N'Việt Nam'),
(N'J.K. Rowling', N'Anh'),
(N'Haruki Murakami', N'Nhật Bản'),
(N'Dan Brown', N'Mỹ')

IF NOT EXISTS (SELECT 1 FROM NhaXuatBan)
INSERT INTO NhaXuatBan(TenNXB, DiaChi, SoDienThoai) VALUES
(N'NXB Trẻ', N'TP.HCM', '0281234567'),
(N'NXB Kim Đồng', N'Hà Nội', '0241234567'),
(N'NXB Văn Học', N'Hà Nội', '0249876543')

IF NOT EXISTS (SELECT 1 FROM Sach)
INSERT INTO Sach(TenSach, MaISBN, MaTL, MaTG, MaNXB, NamXB, SoLuong, GiaTien, MoTa) VALUES
(N'Mắt biếc', '9786041234567', 1, 1, 1, 2020, 10, 85000, N'Truyện dài của Nguyễn Nhật Ánh'),
(N'Harry Potter và Hòn đá phù thủy', '9786041234568', 5, 2, 2, 2000, 15, 120000, N'Tập 1 series Harry Potter'),
(N'Rừng Na Uy', '9786041234569', 1, 3, 3, 2005, 8, 95000, N'Truyện nổi tiếng của Murakami'),
(N'Mật mã Da Vinci', '9786041234570', 1, 4, 1, 2003, 12, 110000, N'Thriller của Dan Brown'),
(N'Đắc Nhân Tâm', '9786041234571', 2, 4, 1, 2019, 20, 68000, N'Sách phát triển bản thân')

IF NOT EXISTS (SELECT 1 FROM DocGia)
INSERT INTO DocGia(HoTen, NgaySinh, GioiTinh, SoDienThoai, Email) VALUES
(N'Nguyễn Văn An', '2000-05-15', N'Nam', '0901234567', 'an@gmail.com'),
(N'Trần Thị Bình', '1998-08-20', N'Nữ', '0912345678', 'binh@gmail.com'),
(N'Lê Minh Cường', '2002-01-10', N'Nam', '0923456789', 'cuong@gmail.com')

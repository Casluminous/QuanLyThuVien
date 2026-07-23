-- Migration: synchronize and harden the live QuanLyThuVien database.
-- Run manually with sqlcmd/SSMS. This script never runs from the application.
USE QuanLyThuVien;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID('dbo.NhanVien', 'U') IS NULL
        THROW 50001, 'Khong tim thay bang NhanVien.', 1;
    IF OBJECT_ID('dbo.TacGia', 'U') IS NULL
        THROW 50002, 'Khong tim thay bang TacGia.', 1;
    IF OBJECT_ID('dbo.PhieuMuon', 'U') IS NULL
        THROW 50003, 'Khong tim thay bang PhieuMuon.', 1;
    IF OBJECT_ID('dbo.Sach', 'U') IS NULL OR OBJECT_ID('dbo.ChiTietPhieuMuon', 'U') IS NULL
        THROW 50004, 'Khong tim thay bang Sach/ChiTietPhieuMuon.', 1;

    -- Do not silently choose a user if the old database already contains duplicates.
    IF EXISTS (
        SELECT 1
        FROM dbo.NhanVien
        GROUP BY TenDangNhap
        HAVING COUNT(*) > 1
    )
        THROW 50005, 'TenDangNhap dang bi trung. Hay xu ly du lieu trung truoc khi chay migration.', 1;

    -- Keep valid business states intact; unknown/legacy values require explicit review.
    IF EXISTS (
        SELECT 1
        FROM dbo.PhieuMuon
        WHERE TrangThai IS NOT NULL
          AND TrangThai NOT IN (N'Đang mượn', N'Đã trả', N'Đã trả một phần')
    )
        THROW 50006, 'PhieuMuon co TrangThai khong duoc nhan dien. Hay chuan hoa thu cong truoc khi chay migration.', 1;

    IF EXISTS (SELECT 1 FROM dbo.Sach WHERE SoLuong < 0 OR GiaTien < 0)
        THROW 50007, 'Sach co SoLuong/GiaTien am. Hay sua du lieu truoc khi them constraint.', 1;

    IF EXISTS (SELECT 1 FROM dbo.ChiTietPhieuMuon WHERE SoLuong <= 0 OR TienPhat < 0)
        THROW 50008, 'ChiTietPhieuMuon co SoLuong/TienPhat khong hop le. Hay sua du lieu truoc khi them constraint.', 1;

    -- Widen the password column before PBKDF2 hashes are used.
    IF COL_LENGTH('dbo.NhanVien', 'MatKhau') IS NOT NULL
       AND COL_LENGTH('dbo.NhanVien', 'MatKhau') < 256
        ALTER TABLE dbo.NhanVien ALTER COLUMN MatKhau VARCHAR(256) NOT NULL;

    -- Canonical author nationality column name.
    IF COL_LENGTH('dbo.TacGia', 'QuocTia') IS NOT NULL
       AND COL_LENGTH('dbo.TacGia', 'QuocTich') IS NULL
        EXEC sys.sp_rename N'dbo.TacGia.QuocTia', N'QuocTich', N'COLUMN';

    IF COL_LENGTH('dbo.TacGia', 'QuocTich') IS NULL
        THROW 50009, 'Khong tim thay cot TacGia.QuocTich sau khi dong bo schema.', 1;

    -- Replace an old/non-unique helper index with a unique username index.
    IF EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.NhanVien')
          AND name = N'IX_NhanVien_TenDangNhap'
          AND is_unique = 0
    )
        DROP INDEX IX_NhanVien_TenDangNhap ON dbo.NhanVien;

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes i
        WHERE i.object_id = OBJECT_ID(N'dbo.NhanVien')
          AND i.is_unique = 1
          AND EXISTS (
              SELECT 1
              FROM sys.index_columns ic
              JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
              WHERE ic.object_id = i.object_id
                AND ic.index_id = i.index_id
                AND c.name = N'TenDangNhap'
          )
    )
        CREATE UNIQUE INDEX UX_NhanVien_TenDangNhap ON dbo.NhanVien(TenDangNhap);

    -- Replace any old default attached to TrangThai with the canonical value.
    DECLARE @statusDefaultName sysname;
    SELECT @statusDefaultName = dc.name
    FROM sys.default_constraints dc
    JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
    WHERE dc.parent_object_id = OBJECT_ID(N'dbo.PhieuMuon')
      AND c.name = N'TrangThai';

    IF @statusDefaultName IS NOT NULL
    BEGIN
        DECLARE @dropDefaultSql nvarchar(400) = N'ALTER TABLE dbo.PhieuMuon DROP CONSTRAINT ' + QUOTENAME(@statusDefaultName) + N';';
        EXEC sys.sp_executesql @dropDefaultSql;
    END;

    ALTER TABLE dbo.PhieuMuon ADD CONSTRAINT DF_PhieuMuon_TrangThai DEFAULT N'Đang mượn' FOR TrangThai;

    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Sach_SoLuong_NonNegative' AND parent_object_id = OBJECT_ID(N'dbo.Sach'))
        ALTER TABLE dbo.Sach ADD CONSTRAINT CK_Sach_SoLuong_NonNegative CHECK (SoLuong >= 0);
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Sach_GiaTien_NonNegative' AND parent_object_id = OBJECT_ID(N'dbo.Sach'))
        ALTER TABLE dbo.Sach ADD CONSTRAINT CK_Sach_GiaTien_NonNegative CHECK (GiaTien >= 0);
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_ChiTietPM_SoLuong_Positive' AND parent_object_id = OBJECT_ID(N'dbo.ChiTietPhieuMuon'))
        ALTER TABLE dbo.ChiTietPhieuMuon ADD CONSTRAINT CK_ChiTietPM_SoLuong_Positive CHECK (SoLuong > 0);
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_ChiTietPM_TienPhat_NonNegative' AND parent_object_id = OBJECT_ID(N'dbo.ChiTietPhieuMuon'))
        ALTER TABLE dbo.ChiTietPhieuMuon ADD CONSTRAINT CK_ChiTietPM_TienPhat_NonNegative CHECK (TienPhat >= 0);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PhieuMuon_MaDG' AND object_id = OBJECT_ID(N'dbo.PhieuMuon'))
        CREATE INDEX IX_PhieuMuon_MaDG ON dbo.PhieuMuon(MaDG);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PhieuMuon_TrangThai' AND object_id = OBJECT_ID(N'dbo.PhieuMuon'))
        CREATE INDEX IX_PhieuMuon_TrangThai ON dbo.PhieuMuon(TrangThai);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ChiTietPM_MaPhieuMuon' AND object_id = OBJECT_ID(N'dbo.ChiTietPhieuMuon'))
        CREATE INDEX IX_ChiTietPM_MaPhieuMuon ON dbo.ChiTietPhieuMuon(MaPhieuMuon);

    IF OBJECT_ID('dbo.SchemaMigrations', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.SchemaMigrations (
            Id INT IDENTITY(1,1) PRIMARY KEY,
            Version NVARCHAR(50) NOT NULL UNIQUE,
            AppliedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
            Checksum NVARCHAR(64) NOT NULL,
            Description NVARCHAR(500)
        );
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE Version = N'2026-07-15-001')
        INSERT INTO dbo.SchemaMigrations (Version, Checksum, Description)
        VALUES (N'2026-07-15-001', N'initial', N'Dong bo schema tac gia, password, trang thai, constraint va index');

    COMMIT TRANSACTION;
    PRINT 'Migration 2026-07-15-001 completed successfully';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

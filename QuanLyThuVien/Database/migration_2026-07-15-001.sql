-- Migration: Fix live QuanLyThuVien database schema
-- Run ONCE against the live database. Idempotent where possible.

USE QuanLyThuVien;
GO

-- 1. Widen MatKhau column for PBKDF2 hashes (VARCHAR(83) minimum)
IF COL_LENGTH('dbo.NhanVien', 'MatKhau') IS NOT NULL
BEGIN
    DECLARE @currentLen INT = COL_LENGTH('dbo.NhanVien', 'MatKhau');
    IF @currentLen < 256
    BEGIN
        ALTER TABLE NhanVien ALTER COLUMN MatKhau VARCHAR(256) NOT NULL;
        PRINT 'Widened NhanVien.MatKhau to VARCHAR(256)';
    END
END
GO

-- 2. Fix TacGia column name typo (QuocTia → QuocTich)
IF COL_LENGTH('dbo.TacGia', 'QuocTia') IS NOT NULL AND COL_LENGTH('dbo.TacGia', 'QuocTich') IS NULL
BEGIN
    EXEC sp_rename 'dbo.TacGia.QuocTia', 'QuocTich', 'COLUMN';
    PRINT 'Renamed TacGia.QuocTia to QuocTich';
END
GO

-- 3. Fix PhieuMuon.TrangThai default and mojibake values
-- Remove mojibake default value
IF EXISTS (SELECT * FROM sys.default_constraints WHERE parent_object_id = OBJECT_ID('dbo.PhieuMuon') AND parent_column_id = COLUMN_PROPERTY_ID(OBJECT_ID('dbo.PhieuMuon'), 'TrangThai'))
BEGIN
    DECLARE @constraintName NVARCHAR(128);
    SELECT @constraintName = dc.name
    FROM sys.default_constraints dc
    WHERE dc.parent_object_id = OBJECT_ID('dbo.PhieuMuon')
      AND dc.parent_column_id = COLUMN_PROPERTY_ID(OBJECT_ID('dbo.PhieuMuon'), 'TrangThai');
    
    IF @constraintName IS NOT NULL
    BEGIN
        DECLARE @sql NVARCHAR(500) = 'ALTER TABLE PhieuMuon DROP CONSTRAINT ' + QUOTENAME(@constraintName);
        EXEC(@sql);
        PRINT 'Dropped old TrangThai default constraint';
    END
END
GO

-- Add correct default
IF NOT EXISTS (SELECT * FROM sys.default_constraints WHERE parent_object_id = OBJECT_ID('dbo.PhieuMuon') AND parent_column_id = COLUMN_PROPERTY_ID(OBJECT_ID('dbo.PhieuMuon'), 'TrangThai'))
BEGIN
    ALTER TABLE PhieuMuon ADD CONSTRAINT DF_PhieuMuon_TrangThai DEFAULT N'Đang mượn' FOR TrangThai;
    PRINT 'Added correct TrangThai default';
END
GO

-- Fix any existing mojibake values (match corrupted UTF-8 stored as Win1252)
UPDATE PhieuMuon SET TrangThai = N'Đang mượn' WHERE TrangThai NOT IN (N'Đang mượn', N'Đã trả') AND TrangThai IS NOT NULL;
PRINT 'Fixed mojibake TrangThai values';
GO

-- 4. Add check constraints
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_Sach_SoLuong_NonNegative')
BEGIN
    ALTER TABLE Sach ADD CONSTRAINT CK_Sach_SoLuong_NonNegative CHECK (SoLuong >= 0);
    PRINT 'Added CK_Sach_SoLuong_NonNegative';
END
GO

IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_Sach_GiaTien_NonNegative')
BEGIN
    ALTER TABLE Sach ADD CONSTRAINT CK_Sach_GiaTien_NonNegative CHECK (GiaTien >= 0);
    PRINT 'Added CK_Sach_GiaTien_NonNegative';
END
GO

IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_ChiTietPM_SoLuong_Positive')
BEGIN
    ALTER TABLE ChiTietPhieuMuon ADD CONSTRAINT CK_ChiTietPM_SoLuong_Positive CHECK (SoLuong > 0);
    PRINT 'Added CK_ChiTietPM_SoLuong_Positive';
END
GO

IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_ChiTietPM_TienPhat_NonNegative')
BEGIN
    ALTER TABLE ChiTietPhieuMuon ADD CONSTRAINT CK_ChiTietPM_TienPhat_NonNegative CHECK (TienPhat >= 0);
    PRINT 'Added CK_ChiTietPM_TienPhat_NonNegative';
END
GO

-- 5. Add indexes for performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_NhanVien_TenDangNhap' AND object_id = OBJECT_ID('dbo.NhanVien'))
BEGIN
    CREATE INDEX IX_NhanVien_TenDangNhap ON NhanVien(TenDangNhap);
    PRINT 'Added IX_NhanVien_TenDangNhap';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PhieuMuon_MaDG' AND object_id = OBJECT_ID('dbo.PhieuMuon'))
BEGIN
    CREATE INDEX IX_PhieuMuon_MaDG ON PhieuMuon(MaDG);
    PRINT 'Added IX_PhieuMuon_MaDG';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PhieuMuon_TrangThai' AND object_id = OBJECT_ID('dbo.PhieuMuon'))
BEGIN
    CREATE INDEX IX_PhieuMuon_TrangThai ON PhieuMuon(TrangThai);
    PRINT 'Added IX_PhieuMuon_TrangThai';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ChiTietPM_MaPhieuMuon' AND object_id = OBJECT_ID('dbo.ChiTietPhieuMuon'))
BEGIN
    CREATE INDEX IX_ChiTietPM_MaPhieuMuon ON ChiTietPhieuMuon(MaPhieuMuon);
    PRINT 'Added IX_ChiTietPM_MaPhieuMuon';
END
GO

-- 6. Create SchemaMigrations table for version tracking
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SchemaMigrations')
BEGIN
    CREATE TABLE SchemaMigrations (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Version NVARCHAR(50) NOT NULL UNIQUE,
        AppliedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        Checksum NVARCHAR(64) NOT NULL,
        Description NVARCHAR(500)
    );
    PRINT 'Created SchemaMigrations table';
END
GO

-- Record this migration version (always, regardless of table creation)
IF NOT EXISTS (SELECT * FROM SchemaMigrations WHERE Version = '2026-07-15-001')
BEGIN
    INSERT INTO SchemaMigrations (Version, Checksum, Description)
    VALUES ('2026-07-15-001', 'initial', 'Fix MatKhau width, QuocTich typo, TrangThai default, add constraints and indexes');
    PRINT 'Recorded migration version 2026-07-15-001';
END
GO

PRINT 'Migration 2026-07-15-001 completed successfully';
GO

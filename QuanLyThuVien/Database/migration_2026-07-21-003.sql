-- Migration: theo dõi các lần thu tiền phạt/tiền đền.
USE QuanLyThuVien;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.PhieuMuon', N'U') IS NULL
        THROW 50110, 'Khong tim thay bang PhieuMuon.', 1;
    IF OBJECT_ID(N'dbo.NhanVien', N'U') IS NULL
        THROW 50111, 'Khong tim thay bang NhanVien.', 1;

    IF OBJECT_ID(N'dbo.ThanhToanPhat', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.ThanhToanPhat
        (
            MaThanhToan INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ThanhToanPhat PRIMARY KEY,
            MaPhieuMuon INT NOT NULL,
            SoTien DECIMAL(18,2) NOT NULL,
            NgayThu DATETIME2 NOT NULL CONSTRAINT DF_ThanhToanPhat_NgayThu DEFAULT SYSDATETIME(),
            MaNV INT NOT NULL,
            GhiChu NVARCHAR(250) NOT NULL CONSTRAINT DF_ThanhToanPhat_GhiChu DEFAULT N'',
            CONSTRAINT FK_ThanhToanPhat_PhieuMuon FOREIGN KEY (MaPhieuMuon) REFERENCES dbo.PhieuMuon(MaPhieuMuon),
            CONSTRAINT FK_ThanhToanPhat_NhanVien FOREIGN KEY (MaNV) REFERENCES dbo.NhanVien(MaNV),
            CONSTRAINT CK_ThanhToanPhat_SoTien_Positive CHECK (SoTien > 0)
        );
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_ThanhToanPhat_MaPhieuMuon' AND object_id=OBJECT_ID(N'dbo.ThanhToanPhat'))
        CREATE INDEX IX_ThanhToanPhat_MaPhieuMuon ON dbo.ThanhToanPhat(MaPhieuMuon);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_ThanhToanPhat_NgayThu' AND object_id=OBJECT_ID(N'dbo.ThanhToanPhat'))
        CREATE INDEX IX_ThanhToanPhat_NgayThu ON dbo.ThanhToanPhat(NgayThu);

    IF OBJECT_ID(N'dbo.SchemaMigrations', N'U') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE Version=N'2026-07-21-003')
        INSERT INTO dbo.SchemaMigrations(Version,Checksum,Description)
        VALUES(N'2026-07-21-003',N'library-operations-payments',N'Them bang theo doi thu tien phat va tien den');

    COMMIT TRANSACTION;
    PRINT 'Migration 2026-07-21-003 completed successfully';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

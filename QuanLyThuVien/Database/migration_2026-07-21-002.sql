SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.ChiTietPhieuMuon', N'U') IS NULL
        THROW 50100, 'Khong tim thay bang ChiTietPhieuMuon.', 1;

    IF COL_LENGTH(N'dbo.ChiTietPhieuMuon', N'SoLuongMat') IS NULL
        ALTER TABLE dbo.ChiTietPhieuMuon ADD SoLuongMat INT NOT NULL
            CONSTRAINT DF_ChiTietPM_SoLuongMat DEFAULT 0 WITH VALUES;

    IF COL_LENGTH(N'dbo.ChiTietPhieuMuon', N'TienDenMatSach') IS NULL
        ALTER TABLE dbo.ChiTietPhieuMuon ADD TienDenMatSach DECIMAL(18,2) NOT NULL
            CONSTRAINT DF_ChiTietPM_TienDenMatSach DEFAULT 0 WITH VALUES;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.default_constraints
        WHERE parent_object_id = OBJECT_ID(N'dbo.ChiTietPhieuMuon')
          AND parent_column_id = COLUMNPROPERTY(OBJECT_ID(N'dbo.ChiTietPhieuMuon'), N'SoLuongMat', 'ColumnId')
    )
        ALTER TABLE dbo.ChiTietPhieuMuon ADD CONSTRAINT DF_ChiTietPM_SoLuongMat
            DEFAULT 0 FOR SoLuongMat;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.default_constraints
        WHERE parent_object_id = OBJECT_ID(N'dbo.ChiTietPhieuMuon')
          AND parent_column_id = COLUMNPROPERTY(OBJECT_ID(N'dbo.ChiTietPhieuMuon'), N'TienDenMatSach', 'ColumnId')
    )
        ALTER TABLE dbo.ChiTietPhieuMuon ADD CONSTRAINT DF_ChiTietPM_TienDenMatSach
            DEFAULT 0 FOR TienDenMatSach;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.check_constraints
        WHERE name = N'CK_ChiTietPM_SoLuongMat_Valid'
          AND parent_object_id = OBJECT_ID(N'dbo.ChiTietPhieuMuon')
    )
        EXEC(N'ALTER TABLE dbo.ChiTietPhieuMuon ADD CONSTRAINT CK_ChiTietPM_SoLuongMat_Valid
            CHECK (SoLuongMat >= 0 AND SoLuongMat <= SoLuong);');

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.check_constraints
        WHERE name = N'CK_ChiTietPM_TienDenMatSach_NonNegative'
          AND parent_object_id = OBJECT_ID(N'dbo.ChiTietPhieuMuon')
    )
        EXEC(N'ALTER TABLE dbo.ChiTietPhieuMuon ADD CONSTRAINT CK_ChiTietPM_TienDenMatSach_NonNegative
            CHECK (TienDenMatSach >= 0);');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

-- ============================================================
-- DATABASE SETUP: Estimate Pending & Cost Management System
-- SQL Server 2019
-- ============================================================

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'EstimateDB')
BEGIN
    CREATE DATABASE EstimateDB;
END
GO

USE EstimateDB;
GO

-- ============================================================
-- TABLE: T_MaterialMaster
-- ============================================================
IF OBJECT_ID('dbo.T_MaterialMaster', 'U') IS NOT NULL
    DROP TABLE dbo.T_MaterialMaster;
GO

CREATE TABLE dbo.T_MaterialMaster (
    ItemID          INT IDENTITY(1,1) PRIMARY KEY,
    ItemDesc        NVARCHAR(255)   NOT NULL,
    UOM             NVARCHAR(50)    NOT NULL,
    MaterialCost    DECIMAL(18, 2)  NOT NULL DEFAULT 0,
    ServiceCost     DECIMAL(18, 2)  NOT NULL DEFAULT 0,
    CreatedBy       NVARCHAR(100)   NOT NULL DEFAULT 'SYSTEM',
    CreatedOn       DATETIME        NOT NULL DEFAULT GETDATE(),
    UpdatedBy       NVARCHAR(100)   NULL,
    UpdatedOn       DATETIME        NULL,
    IsActive        BIT             NOT NULL DEFAULT 1
);
GO

-- ============================================================
-- TABLE: T_EstimateHeader
-- ============================================================
IF OBJECT_ID('dbo.T_EstimateHeader', 'U') IS NOT NULL
    DROP TABLE dbo.T_EstimateHeader;
GO

CREATE TABLE dbo.T_EstimateHeader (
    EstimateID      INT IDENTITY(1,1) PRIMARY KEY,
    EstimateNo      NVARCHAR(50)    NOT NULL,
    SubmittedBy     NVARCHAR(100)   NOT NULL DEFAULT 'USER',
    SubmittedOn     DATETIME        NOT NULL DEFAULT GETDATE(),
    TotalCost       DECIMAL(18, 2)  NOT NULL DEFAULT 0,
    Status          NVARCHAR(50)    NOT NULL DEFAULT 'Submitted',
    Remarks         NVARCHAR(500)   NULL
);
GO

-- ============================================================
-- TABLE: T_EstimateDetail
-- ============================================================
IF OBJECT_ID('dbo.T_EstimateDetail', 'U') IS NOT NULL
    DROP TABLE dbo.T_EstimateDetail;
GO

CREATE TABLE dbo.T_EstimateDetail (
    DetailID        INT IDENTITY(1,1) PRIMARY KEY,
    EstimateID      INT             NOT NULL FOREIGN KEY REFERENCES T_EstimateHeader(EstimateID),
    SerialNo        INT             NOT NULL,
    ItemDesc        NVARCHAR(255)   NOT NULL,
    UOM             NVARCHAR(50)    NOT NULL,
    Quantity        DECIMAL(18, 3)  NOT NULL DEFAULT 0,
    MaterialCost    DECIMAL(18, 2)  NOT NULL DEFAULT 0,
    ServiceCost     DECIMAL(18, 2)  NOT NULL DEFAULT 0,
    TotalCost       DECIMAL(18, 2)  NOT NULL DEFAULT 0,
    IsVerified      BIT             NOT NULL DEFAULT 0,
    MismatchFlag    BIT             NOT NULL DEFAULT 0
);
GO

-- ============================================================
-- SEED DATA: T_MaterialMaster
-- ============================================================
INSERT INTO dbo.T_MaterialMaster (ItemDesc, UOM, MaterialCost, ServiceCost, CreatedBy)
VALUES
    ('Steel Plate 10mm',        'KG',   85.50,  12.00, 'ADMIN'),
    ('Steel Plate 20mm',        'KG',   90.00,  15.00, 'ADMIN'),
    ('Mild Steel Rod 12mm',     'KG',   72.00,   8.50, 'ADMIN'),
    ('Mild Steel Rod 16mm',     'KG',   74.50,   9.00, 'ADMIN'),
    ('Structural Angle 50x50',  'MT',  6800.00, 500.00,'ADMIN'),
    ('Structural Channel 100',  'MT',  7200.00, 600.00,'ADMIN'),
    ('Hex Bolt M12',            'NOS',   4.50,   1.00, 'ADMIN'),
    ('Hex Bolt M16',            'NOS',   6.00,   1.25, 'ADMIN'),
    ('Welding Electrode 3.15',  'PKT',  320.00,  25.00,'ADMIN'),
    ('Welding Electrode 4.0',   'PKT',  350.00,  28.00,'ADMIN'),
    ('Grinding Disc 180mm',     'NOS',   45.00,   5.00,'ADMIN'),
    ('Safety Helmet',           'NOS',  250.00,   0.00,'ADMIN'),
    ('Safety Gloves',           'PAIR',  85.00,   0.00,'ADMIN'),
    ('Paint Primer Red Oxide',  'LTR',  180.00,  20.00,'ADMIN'),
    ('Paint Enamel Black',      'LTR',  220.00,  22.00,'ADMIN');
GO

-- ============================================================
-- STORED PROCEDURE: usp_SubmitEstimate
-- ============================================================
IF OBJECT_ID('dbo.usp_SubmitEstimate', 'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_SubmitEstimate;
GO

CREATE PROCEDURE dbo.usp_SubmitEstimate
    @EstimateNo     NVARCHAR(50),
    @SubmittedBy    NVARCHAR(100),
    @TotalCost      DECIMAL(18,2),
    @Remarks        NVARCHAR(500),
    @EstimateID     INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.T_EstimateHeader (EstimateNo, SubmittedBy, TotalCost, Remarks)
    VALUES (@EstimateNo, @SubmittedBy, @TotalCost, @Remarks);
    SET @EstimateID = SCOPE_IDENTITY();
END
GO

-- ============================================================
-- STORED PROCEDURE: usp_SubmitEstimateDetail
-- ============================================================
IF OBJECT_ID('dbo.usp_SubmitEstimateDetail', 'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_SubmitEstimateDetail;
GO

CREATE PROCEDURE dbo.usp_SubmitEstimateDetail
    @EstimateID     INT,
    @SerialNo       INT,
    @ItemDesc       NVARCHAR(255),
    @UOM            NVARCHAR(50),
    @Quantity       DECIMAL(18,3),
    @MaterialCost   DECIMAL(18,2),
    @ServiceCost    DECIMAL(18,2),
    @TotalCost      DECIMAL(18,2),
    @IsVerified     BIT,
    @MismatchFlag   BIT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.T_EstimateDetail
        (EstimateID, SerialNo, ItemDesc, UOM, Quantity, MaterialCost, ServiceCost, TotalCost, IsVerified, MismatchFlag)
    VALUES
        (@EstimateID, @SerialNo, @ItemDesc, @UOM, @Quantity, @MaterialCost, @ServiceCost, @TotalCost, @IsVerified, @MismatchFlag);
END
GO

-- ============================================================
-- STORED PROCEDURE: usp_GetMaterialMaster
-- ============================================================
IF OBJECT_ID('dbo.usp_GetMaterialMaster', 'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_GetMaterialMaster;
GO

CREATE PROCEDURE dbo.usp_GetMaterialMaster
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ItemID, ItemDesc, UOM, MaterialCost, ServiceCost
    FROM dbo.T_MaterialMaster
    WHERE IsActive = 1
    ORDER BY ItemDesc;
END
GO

-- ============================================================
-- STORED PROCEDURE: usp_GetEstimateHistory
-- ============================================================
IF OBJECT_ID('dbo.usp_GetEstimateHistory', 'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_GetEstimateHistory;
GO

CREATE PROCEDURE dbo.usp_GetEstimateHistory
AS
BEGIN
    SET NOCOUNT ON;
    SELECT h.EstimateID, h.EstimateNo, h.SubmittedBy, h.SubmittedOn,
           h.TotalCost, h.Status,
           COUNT(d.DetailID) AS ItemCount
    FROM dbo.T_EstimateHeader h
    LEFT JOIN dbo.T_EstimateDetail d ON h.EstimateID = d.EstimateID
    GROUP BY h.EstimateID, h.EstimateNo, h.SubmittedBy, h.SubmittedOn, h.TotalCost, h.Status
    ORDER BY h.SubmittedOn DESC;
END
GO

PRINT 'Database setup completed successfully.';
GO

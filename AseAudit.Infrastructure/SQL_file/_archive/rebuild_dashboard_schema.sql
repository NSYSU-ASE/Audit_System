USE [Ase_Audit];

IF OBJECT_ID('dbo.AuditFinding', 'U') IS NOT NULL DROP TABLE dbo.AuditFinding;
IF OBJECT_ID('dbo.AuditResult', 'U') IS NOT NULL DROP TABLE dbo.AuditResult;
IF OBJECT_ID('dbo.Device', 'U') IS NOT NULL DROP TABLE dbo.Device;
IF OBJECT_ID('dbo.Building', 'U') IS NOT NULL DROP TABLE dbo.Building;
IF OBJECT_ID('dbo.Area', 'U') IS NOT NULL DROP TABLE dbo.Area;
GO

CREATE TABLE dbo.Area
(
    AreaId INT IDENTITY(1,1) PRIMARY KEY,
    AreaCode NVARCHAR(50) NOT NULL UNIQUE,
    AreaName NVARCHAR(100) NOT NULL,
    OwnerName NVARCHAR(100) NULL
);
GO

CREATE TABLE dbo.Building
(
    BuildingId INT IDENTITY(1,1) PRIMARY KEY,
    BuildingCode NVARCHAR(50) NOT NULL UNIQUE,
    BuildingName NVARCHAR(100) NOT NULL,
    AreaId INT NOT NULL,
    OwnerName NVARCHAR(100) NULL,
    FOREIGN KEY (AreaId) REFERENCES dbo.Area(AreaId)
);
GO

CREATE TABLE dbo.Device
(
    DeviceId NVARCHAR(50) PRIMARY KEY,
    HostName NVARCHAR(255) NULL,
    IP NVARCHAR(50) NULL,
    DeviceType NVARCHAR(50) NULL,
    BuildingId INT NOT NULL,
    OwnerName NVARCHAR(100) NULL,
    FOREIGN KEY (BuildingId) REFERENCES dbo.Building(BuildingId)
);
GO

CREATE TABLE dbo.AuditResult
(
    AuditResultId INT IDENTITY(1,1) PRIMARY KEY,
    DeviceId NVARCHAR(50) NOT NULL,
    AuditPeriod NVARCHAR(20) NOT NULL,
    FR1 INT NOT NULL,
    FR2 INT NOT NULL,
    FR3 INT NOT NULL,
    FR4 INT NOT NULL,
    FR5 INT NOT NULL,
    FR6 INT NOT NULL,
    FR7 INT NOT NULL,
    TotalScore AS ((FR1 + FR2 + FR3 + FR4 + FR5 + FR6 + FR7) / 7),
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (DeviceId) REFERENCES dbo.Device(DeviceId)
);
GO

CREATE TABLE dbo.AuditFinding
(
    FindingId INT IDENTITY(1,1) PRIMARY KEY,
    AuditResultId INT NOT NULL,
    FRCode NVARCHAR(10) NOT NULL,
    Reason NVARCHAR(MAX) NULL,
    FOREIGN KEY (AuditResultId) REFERENCES dbo.AuditResult(AuditResultId)
);
GO

INSERT INTO dbo.Area (AreaCode, AreaName, OwnerName)
VALUES
    (N'EAST', N'East (東區)', N'owner1'),
    (N'WEST', N'West (西區)', N'owner2'),
    (N'MTL', N'MTL', N'owner3'),
    (N'CRD', N'CRD', N'owner1'),
    (N'ZONEII', N'ZoneII', N'owner2'),
    (N'ADMIN', N'Admin (行政)', N'owner3');

DECLARE @Buildings TABLE
(
    BuildingCode NVARCHAR(50),
    BuildingName NVARCHAR(100),
    AreaCode NVARCHAR(50),
    OwnerName NVARCHAR(100)
);

INSERT INTO @Buildings (BuildingCode, BuildingName, AreaCode, OwnerName)
VALUES
    (N'K11', N'K11', N'EAST', N'蔡佳玲'),
    (N'K12', N'K12', N'EAST', N'吳建宏'),
    (N'K27', N'K27', N'EAST', N'林明哲'),
    (N'K15', N'K15', N'EAST', N'陳怡君'),
    (N'K9', N'K9', N'EAST', N'黃俊凱'),
    (N'K3K4', N'K3/K4', N'WEST', N'周柏廷'),
    (N'K5', N'K5', N'WEST', N'許芳瑜'),
    (N'K7', N'K7', N'WEST', N'鄭雅文'),
    (N'K8', N'K8', N'WEST', N'劉冠廷'),
    (N'K16', N'K16', N'MTL', N'簡志成'),
    (N'K1K2', N'K1/K2', N'MTL', N'羅家豪'),
    (N'K13B', N'K13B', N'MTL', N'謝佩珊'),
    (N'K17', N'K17', N'CRD', N'郭子豪'),
    (N'K9B', N'K9B', N'CRD', N'何雅婷'),
    (N'K18', N'K18', N'ZONEII', N'王小明'),
    (N'K21', N'K21', N'ZONEII', N'李小華'),
    (N'K22', N'K22', N'ZONEII', N'陳志明'),
    (N'K24', N'K24', N'ZONEII', N'林雅婷'),
    (N'K25', N'K25', N'ZONEII', N'張育誠'),
    (N'K26', N'K26', N'ZONEII', N'黃俊傑'),
    (N'K14B', N'K14B', N'ADMIN', N'吳佳蓉'),
    (N'K14C', N'K14C', N'ADMIN', N'曾建豪');

INSERT INTO dbo.Building (BuildingCode, BuildingName, AreaId, OwnerName)
SELECT b.BuildingCode, b.BuildingName, a.AreaId, b.OwnerName
FROM @Buildings b
INNER JOIN dbo.Area a ON a.AreaCode = b.AreaCode;

INSERT INTO dbo.Device (DeviceId, HostName, IP, DeviceType, BuildingId, OwnerName)
SELECT
    N'DEV-' + b.BuildingCode + N'-001',
    b.BuildingName + N'-PC-001',
    N'192.168.' + CAST(a.AreaId AS NVARCHAR(10)) + N'.' + CAST(ROW_NUMBER() OVER (ORDER BY b.BuildingId) AS NVARCHAR(10)),
    CASE
        WHEN b.BuildingCode LIKE N'K2%' THEN N'SCADA'
        WHEN b.BuildingCode IN (N'K16', N'K17', N'K18') THEN N'HMI'
        ELSE N'PC'
    END,
    b.BuildingId,
    b.OwnerName
FROM dbo.Building b
INNER JOIN dbo.Area a ON b.AreaId = a.AreaId;

DECLARE @Scores TABLE
(
    BuildingCode NVARCHAR(50),
    AuditPeriod NVARCHAR(20),
    FR1 INT,
    FR2 INT,
    FR3 INT,
    FR4 INT,
    FR5 INT,
    FR6 INT,
    FR7 INT
);

INSERT INTO @Scores (BuildingCode, AuditPeriod, FR1, FR2, FR3, FR4, FR5, FR6, FR7)
VALUES
    (N'K11', N'2025-11', 88,86,84,85,87,89,86),
    (N'K3K4', N'2025-11', 72,74,70,73,71,75,72),
    (N'K12', N'2025-11', 79,77,75,76,78,79,77),
    (N'K27', N'2025-11', 81,78,76,79,80,82,78),
    (N'K5', N'2025-11', 83,79,77,80,82,84,81),
    (N'K7', N'2025-11', 76,73,71,74,72,75,77),
    (N'K8', N'2025-11', 69,68,70,67,66,69,71),
    (N'K15', N'2025-11', 82,80,78,79,81,83,80),
    (N'K9', N'2025-11', 82,80,78,79,81,83,80),
    (N'K21', N'2025-11', 85,83,81,82,84,85,83),
    (N'K22', N'2025-11', 84,82,80,81,83,84,82),
    (N'K24', N'2025-11', 86,84,82,83,85,86,84),
    (N'K25', N'2025-11', 87,85,83,84,86,87,85),
    (N'K18', N'2025-11', 88,86,84,85,87,88,86),
    (N'K16', N'2025-11', 85,82,80,81,83,85,82),
    (N'K1K2', N'2025-11', 80,78,76,77,79,80,78),
    (N'K13B', N'2025-11', 82,79,77,78,80,82,79),
    (N'K17', N'2025-11', 85,81,79,80,82,85,81),
    (N'K9B', N'2025-11', 83,80,78,79,81,83,80),
    (N'K26', N'2025-11', 88,84,82,83,85,88,84),
    (N'K14B', N'2025-11', 87,83,81,82,84,87,83),
    (N'K14C', N'2025-11', 88,84,82,83,85,88,84),
    (N'K11', N'2025-12', 87,87,83,86,88,88,85),
    (N'K3K4', N'2025-12', 71,75,71,72,70,76,71),
    (N'K12', N'2025-12', 78,78,76,75,79,78,78),
    (N'K27', N'2025-12', 82,77,75,80,79,83,77),
    (N'K5', N'2025-12', 84,78,76,81,83,85,82),
    (N'K7', N'2025-12', 77,72,72,73,73,76,78),
    (N'K8', N'2025-12', 68,67,71,66,65,68,70),
    (N'K15', N'2025-12', 83,79,79,78,82,82,81),
    (N'K9', N'2025-12', 83,79,79,78,82,82,81),
    (N'K21', N'2025-12', 84,84,80,83,85,84,82),
    (N'K22', N'2025-12', 85,81,81,80,84,83,81),
    (N'K24', N'2025-12', 86,83,83,82,86,85,83),
    (N'K25', N'2025-12', 88,84,82,85,87,86,84),
    (N'K18', N'2025-12', 87,87,83,86,88,88,85),
    (N'K16', N'2025-12', 84,81,79,80,82,84,81),
    (N'K1K2', N'2025-12', 81,79,77,76,78,81,79),
    (N'K13B', N'2025-12', 80,81,74,77,78,80,77),
    (N'K17', N'2025-12', 85,80,68,72,74,81,73),
    (N'K9B', N'2025-12', 85,82,80,81,83,82,79),
    (N'K26', N'2025-12', 90,85,73,72,74,81,73),
    (N'K14B', N'2025-12', 88,84,82,83,85,88,84),
    (N'K14C', N'2025-12', 88,84,82,83,85,88,84);

INSERT INTO dbo.AuditResult (DeviceId, AuditPeriod, FR1, FR2, FR3, FR4, FR5, FR6, FR7)
SELECT d.DeviceId, s.AuditPeriod, s.FR1, s.FR2, s.FR3, s.FR4, s.FR5, s.FR6, s.FR7
FROM @Scores s
INNER JOIN dbo.Building b ON b.BuildingCode = s.BuildingCode
INNER JOIN dbo.Device d ON d.BuildingId = b.BuildingId;

INSERT INTO dbo.AuditFinding (AuditResultId, FRCode, Reason)
SELECT ar.AuditResultId, f.FRCode, f.Reason
FROM dbo.AuditResult ar
CROSS APPLY
(
    VALUES
        (N'FR1', ar.FR1, N'帳號與識別管理成熟度需補強'),
        (N'FR2', ar.FR2, N'權限審核與存取控管需補強'),
        (N'FR3', ar.FR3, N'稽核軌跡與紀錄保存需補強'),
        (N'FR4', ar.FR4, N'遠端存取與網路限制需補強'),
        (N'FR5', ar.FR5, N'資產清冊與變更控管需補強'),
        (N'FR6', ar.FR6, N'系統維運流程需補強'),
        (N'FR7', ar.FR7, N'備援與復原演練需補強')
) f(FRCode, Score, Reason)
WHERE ar.AuditPeriod = N'2025-12'
  AND f.Score < 80;

SELECT 'Area' AS TableName, COUNT(*) AS [Rows] FROM dbo.Area
UNION ALL SELECT 'Building', COUNT(*) FROM dbo.Building
UNION ALL SELECT 'Device', COUNT(*) FROM dbo.Device
UNION ALL SELECT 'AuditResult', COUNT(*) FROM dbo.AuditResult
UNION ALL SELECT 'AuditFinding', COUNT(*) FROM dbo.AuditFinding;

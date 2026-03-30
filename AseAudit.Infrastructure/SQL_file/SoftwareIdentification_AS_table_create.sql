--Anomaly software
CREATE TABLE [dbo].[SoftwareIdentification_AS]
(
    -- 主鍵
    [ID] INT PRIMARY KEY IDENTITY(1,1),
    -- 時間、主機名稱、IP位址
    [CreatedTime] DATETIME DEFAULT GETDATE(),
    [HostName] NVARCHAR(255) NOT NULL,
    [MACAddress] NVARCHAR(45) NOT NULL,
    -- 欄位定義
    [SoftwareName] NVARCHAR(100) NOT NULL,
    [AnomalyType] NVARCHAR(100) NOT NULL, --未安裝 未授權


)

-- 建立索引
-- 需優化
CREATE INDEX [IX_SoftwareIdentification_AS_HostName] ON [dbo].[SoftwareIdentification_AS]([HostName])
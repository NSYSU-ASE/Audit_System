--Required software

CREATE TABLE [dbo].[SoftwareIdentification_RS]
(
    -- 主鍵
    [ID] INT PRIMARY KEY IDENTITY(1,1),
    -- 時間、主機名稱、IP位址
    [CreatedTime] DATETIME DEFAULT GETDATE(),
    -- 欄位定義
    [SoftwareName] NVARCHAR(100) NOT NULL,--授權軟體
    [hash] NVARCHAR(255) NOT NULL , --授權軟體
    [version] NVARCHAR(50) NULL, --軟體版本
    [lastUpdated] DATETIME NULL --最後更新時間

    Unique(SoftwareName, hash) --確保軟體名稱和hash的組合是唯一的

)

-- 建立索引
-- 需優化
CREATE INDEX [IX_SoftwareIdentification_RS_SoftwareName] ON [dbo].[SoftwareIdentification_RS]([SoftwareName])
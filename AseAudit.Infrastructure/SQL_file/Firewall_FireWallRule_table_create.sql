-- Firewall_FireWallRule_table_create.sql
-- [dbo].[FireWallRule] — 防火牆規則快照
-- 每次稽核，每條規則寫入一列；LocalPort 欄位對應 C# Entity 的 Port 屬性。

CREATE TABLE [dbo].[FireWallRule]
(
    [ID]            INT           PRIMARY KEY IDENTITY(1,1),
    [CreatedTime]   DATETIME      NOT NULL DEFAULT GETDATE(),
    [HostName]      NVARCHAR(255) NOT NULL,
    [MACAddress]    NVARCHAR(45)  NULL,
    [RuleName]      NVARCHAR(100) NOT NULL,
    [DisplayName]   NVARCHAR(100) NULL,
    [Status]        NVARCHAR(100) NULL,
    [Profile]       NVARCHAR(100) NULL,
    [Action]        NVARCHAR(100) NULL,
    [Direction]     NVARCHAR(100) NULL,
    [LocalPort]     NVARCHAR(100) NULL,
    [RemotePort]    NVARCHAR(100) NULL,
    [Protocol]      NVARCHAR(100) NULL,
    [SourceIP]      NVARCHAR(100) NULL,
    [DestinationIP] NVARCHAR(100) NULL
)

-- 依主機名稱建索引，加速查詢指定主機的規則清單
CREATE INDEX [IX_FireWallRule_HostName] ON [dbo].[FireWallRule]([HostName])

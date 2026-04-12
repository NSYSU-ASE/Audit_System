CREATE TABLE [dbo].[Identification_AM_rule]
(
    -- 主鍵
    [ID] INT PRIMARY KEY IDENTITY(1,1),

    -- 時間、主機名稱、MAC 位址
    [CreatedTime] DATETIME NOT NULL DEFAULT GETDATE(),
    [HostName] NVARCHAR(255) NOT NULL,
    [MACAddress] NVARCHAR(45) NOT NULL,

    -- 匿名存取設定
    [RestrictAnonymousSAM] BIT NOT NULL,
    [EveryoneIncludesAnonymous] BIT NOT NULL,
    [RestrictAnonymous] BIT NOT NULL,

    -- 網域資訊
    [UserDomain] NVARCHAR(100) NULL,
    [DomainRole] INT NOT NULL
)

-- 建立索引
-- 主機名稱
CREATE INDEX [IX_Identification_AM_rule_HostName] ON [dbo].[Identification_AM_rule]([HostName])

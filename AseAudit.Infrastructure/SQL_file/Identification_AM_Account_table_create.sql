--Account Management
CREATE TABLE [dbo].[Identification_AM_Account]
(
    -- 主鍵
    [ID] INT PRIMARY KEY IDENTITY(1,1),
    -- 時間、主機名稱、IP位址
    [CreatedTime] DATETIME DEFAULT GETDATE(),
    [HostName] NVARCHAR(255) NOT NULL,
    [MACAddress] NVARCHAR(45) ,
    -- 欄位定義

    [AccountName] NVARCHAR(100) NOT NULL,
    [Status] NVARCHAR(100) ,
    [PasswordRequired] BIT ,




)




-- 建立索引
-- 需優化
CREATE INDEX [IX_Identification_AM_Account_HostName] ON [dbo].[Identification_AM_Account]([HostName])
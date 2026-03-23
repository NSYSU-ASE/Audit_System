--UserGroup
CREATE TABLE [dbo].[Identification_UG_GroupList]
(
    -- 主鍵
    [ID] INT PRIMARY KEY IDENTITY(1,1),
    -- 時間、主機名稱、IP位址
    [CreatedTime] DATETIME DEFAULT GETDATE(),
    -- 欄位定義
    [UserGroup] NVARCHAR(100) NOT NULL,
    [UserName] NVARCHAR(100) NOT NULL, 


)

-- 建立索引
-- 需優化
CREATE INDEX [IX_Identification_UG_GroupList_UserGroup] ON [dbo].[Identification_UG_GroupList]([UserGroup])
-- ============================================================
-- 一次性 baseline 同步腳本
--
-- 用途：當 DB 已存在透過 EnsureCreated 或手動 SQL 建立的舊表，
--       但 __EFMigrationsHistory 沒有對應紀錄時，本腳本會：
--         1. 補建 InitialSchema 中尚未存在的表 (FireWallRule)
--         2. 將 InitialSchema 標記為「已套用」
--       讓後續 dotnet ef migrations add / Migrate() 能正常增量運作。
--
-- 執行：sqlcmd -S "(localdb)\MSSQLLocalDB" -d Ase_Audit -E -i sync_baseline_InitialSchema.sql
--
-- 注意：腳本為 idempotent，重複執行不會壞事。執行成功後即可刪除本檔。
-- ============================================================

-- 1) 補建 FireWallRule（若尚未存在）
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'FireWallRule')
BEGIN
    CREATE TABLE [FireWallRule] (
        [ID]            int            IDENTITY(1,1) NOT NULL,
        [CreatedTime]   datetime2      NOT NULL CONSTRAINT [DF_FireWallRule_CreatedTime] DEFAULT (GETDATE()),
        [HostName]      nvarchar(255)  NOT NULL,
        [MACAddress]    nvarchar(45)   NULL,
        [RuleName]      nvarchar(100)  NOT NULL,
        [DisplayName]   nvarchar(100)  NULL,
        [Status]        nvarchar(100)  NULL,
        [Profile]       nvarchar(100)  NULL,
        [Action]        nvarchar(100)  NULL,
        [Direction]     nvarchar(100)  NULL,
        [LocalPort]     nvarchar(100)  NULL,
        [RemotePort]    nvarchar(100)  NULL,
        [Protocol]      nvarchar(100)  NULL,
        [SourceIP]      nvarchar(100)  NULL,
        [DestinationIP] nvarchar(100)  NULL,
        CONSTRAINT [PK_FireWallRule] PRIMARY KEY ([ID])
    );

    CREATE INDEX [IX_FireWallRule_HostName] ON [FireWallRule] ([HostName]);
END;
GO

-- 2) 將 InitialSchema 標記為已套用（若尚未紀錄）
IF NOT EXISTS (
    SELECT 1 FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505044342_InitialSchema'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260505044342_InitialSchema', N'8.0.26');
END;
GO

PRINT 'Baseline sync complete. Subsequent migrations will be applied incrementally.';

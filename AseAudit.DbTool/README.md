# AseAudit.DbTool

ASE Audit 資料庫管理 CLI。雙模式：
- **部屬模式**（DB 未初始化）：只能初始化 DB
- **開發模式**（DB 已存在 manifest 表）：完整的 Backup / Reset / Restore

## 執行

```bash
# 互動式（預設）
dotnet run --project AseAudit.DbTool

# 或從 publish 過的 exe
AseAudit.DbTool.exe
```

## 新增資料表的標準流程

1. 在 `../AseAudit.Infrastructure/SQL_file/` 加入 `NewTable_table_create.sql`
2. 在 `schema-manifest.json` 加入 `tables` 條目（注意 loadOrder 在 FK 父表之後）
3. 在 `AseAudit.Core/Entities/` 加 POCO
4. 在 `AseAudit.Infrastructure/Data/AuditDbContext.cs` 加 `DbSet<NewTable>`
5. （若需要寫入邏輯）加 Repository
6. 跑 `dotnet run --project AseAudit.DbTool`，選「重建資料庫」

## manifest 欄位

| 欄位 | 說明 |
|---|---|
| name | 表名（必須匹配 `[A-Za-z_][A-Za-z0-9_]*`） |
| loadOrder | FK 父表先載入，建議用 10/20/30 留間距 |
| backupable | 是否在備份選單中列出 |
| createScript | 相對於 scriptRoot 的 .sql 路徑 |
| description | 選填說明 |

## Exit Code

| Code | 意義 |
|---|---|
| 0 | 成功 |
| 1 | 一般錯誤 |
| 2 | 環境問題（LocalDB 無法連線） |
| 3 | 依賴缺失（bcp.exe） |
| 4 | manifest 不合法 |
| 5 | 使用者取消 |
| 6 | DB 狀態不符預期 |
| 130 | Ctrl+C |

## 常見問題

**Q：跑了之後說 bcp 不存在？**
請安裝 [SQL Server Command Line Utilities](https://learn.microsoft.com/sql/tools/sqlcmd-utility)，安裝後 bcp.exe 會在 `C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\<ver>\Tools\Binn\`。

**Q：Reset 之後想救資料？**
看 `db-backups/_autoBackup-<最新時間戳>/`，Reset 每次都會先做這個快照（保留最近 5 份）。

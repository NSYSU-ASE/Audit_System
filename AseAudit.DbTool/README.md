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

分兩部分：**db-tool 端**讓工具認得新表，**系統端**讓 API/Agent 能讀寫新表。

### db-tool 端（讓 DbTool 能建立 / 備份新表）

1. **在 `AseAudit.Infrastructure/SQL_file/` 加入 `NewTable_table_create.sql`**
   - 這支建表 SQL 是新表**結構的唯一來源**（欄位、型別、PK/FK 全寫在這）
   - DbTool 不解析欄位，只把整個腳本交給 SQL Server 執行
   - build 時 csproj 的 `*.sql` Target 會自動複製到 bin，**不需改 csproj**
2. 在 `schema-manifest.json` 的 `tables` 加入條目：
   - `createScript`：填上一步的 .sql 檔名
   - `loadOrder`：FK 父表的數字要比子表小；現有最大為 120，無 FK 關係的新表接 130 即可
   - `backupable`：`true` 會出現在備份選單，靜態查找表可設 `false`

只要這兩步，DbTool 就能建立、備份、還原新表。`ManifestLoader` 啟動時會驗證
表名合法性、name/loadOrder 唯一性、createScript 檔案存在性。

> **db-tool 不儲存表結構。** manifest 只記「表名 + 順序 + 能否備份」；
> 結構定義只存在於 `SQL_file/*.sql`。改欄位時只改該 .sql 即可，manifest 不用動。
> 但注意：bcp 備份採 native 格式，**改過結構後舊的 bcp 備份檔即失效**，需重新備份。

### 系統端（讓 API / Agent 能讀寫新表，與 DbTool 無關）

3. 在 `AseAudit.Core/Entities/` 加 POCO
4. 在 `AseAudit.Infrastructure/Data/AuditDbContext.cs` 加 `DbSet<NewTable>`
5. （若需要寫入邏輯）加 Repository

### 套用

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

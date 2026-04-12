# Audit System (ASE Audit)

NSYSU 資訊安全稽核系統，用於自動化執行稽核腳本、收集主機資訊並產出評分報告。
系統部署為兩個獨立執行環境：Server 端 (API + 網頁託管) 與 Agent 端 (Collector)。

- **技術棧**: .NET 8 / C#、ASP.NET Core Web API (Swagger/OpenAPI)、PowerShell SDK、SQL Server
- **架構模式**: Clean Architecture + 前後端分離 (API 兼託管靜態網頁)
- **GitHub repo**: NSYSU-ASE/Audit_System

## 系統組成

| 元件 | 產出物 | 部署位置 | 說明 |
|------|--------|----------|------|
| **Server (API)** | `AseAudit.Api.exe` | 稽核伺服器 | 接收 Agent/Fluent Bit 回報、執行評分邏輯、寫入資料庫、託管前端靜態頁面 |
| **Agent (Collector)** | `AseAudit.Collector.exe` | 被稽核主機 (Client) | 背景服務，蒐集系統資訊 (帳號、狀態、事件日誌) 並透過 HTTP 回報 Server |
| **Shared** | Class Library | 編譯時引用 | 定義前後端與 Agent 溝通的資料格式 (DTO)，確保 JSON 格式一致 |

## 資料流

```
Agent (Collector)                        Server (API)
  執行 PowerShell 稽核腳本                   IngestController 接收 JSON
  → Script_lib/ 蒐集主機資訊                 → Core Rules 評分判定
  → ToJSON/ 轉換為 JSON                     → Infrastructure 寫入 SQL Server
  → HTTP POST ──────────────────────────→   → wwwroot 前端顯示報告

Fluent Bit (Log 蒐集) ─── HTTP POST ──→ LogsController (未來擴充)
Browser ─── HTTP GET ──────────────────→ API / wwwroot 靜態頁面
```

## 專案架構 (Solution Folders)

### 01.Core — `AseAudit.Core` (核心層，不依賴任何外部專案)

領域模型、稽核規則 (判定邏輯)、實體定義。按稽核模組分類：

- `Entities/` — 資料庫實體定義
  - `Source_Sturcture.cs` — 資料來源結構
- `Modules/Identity/` — 身份識別與存取管理模組
  - `Dtos/` — HostAccountSnapshotDto、PasswordPolicySnapshotDto、UserGroupSnapshotDto、EmployeeDirectoryRecordDto、HostIdentitySnapshotDto、UiControlSnapshotDto、UiControlOcrTextDto
  - `Rules/` — AdAccountProtectionRule、PasswordPolicyRule、UserGroupProtectionRule、EmployeeDirectoryProtectionRule、SystemUseNoticeRule、ErrorFeedbackRule
- `Modules/Firewall/` — 防火牆政策模組
  - `Dtos/` — FirewallPolicySnapshotDto、FirewallWhitelistEntryDto、DomainTableRecordDto、AccessNetworkSegmentRecordDto、DeviceConnectionPathRecordDto、JumpHostInventoryRecordDto
  - `Rules/` — FirewallPolicyRule、JumpHostConnectionRule
- `Modules/SoftwareControl/` — 軟體控管模組
  - `Dtos/` — SoftwareInventorySnapshotDto、InstalledProgramRecordDto、AntivirusStatusRecordDto、AntivirusBaselineDto、AuthorizedSoftwareWhitelistEntryDto
  - `Rules/` — AuthorizedSoftwareRule、AntivirusProtectionRule
- `ScheduleTask/` — 排程任務 (待實作)

### 02.Infrastructure — `AseAudit.Infrastructure` (基礎設施層，實作 Core 的介面)

資料存取、SQL 腳本。未來將加入 EF Core DbContext 與 Repository。

- `SQL_file/` — SQL Server 建表腳本
  - 身份識別相關: `Identification_AM_Account_table_create.sql`、`Identification_AM_rule_table_create.sql`、`Identification_UG_GroupList_table_create.sql`
  - 軟體控管相關: `SoftwareIdentification_AS_table_create.sql`、`SoftwareIdentification_RS_table_create.sql`、`SoftwareIdentification_WL_table_create.sql`

### 03.Presentation — `AseAudit.Api` (表現層，API + 網頁託管)

ASP.NET Core Web API，提供 Ingest endpoint 接收稽核資料，並託管靜態前端。

- `Controllers/`
  - `IngestController.cs` — 接收 Agent 的稽核資料回報 (主要入口)
  - `WeatherForecastController.cs` — 範本 (可移除)
- `Services/`
  - `IAuditIngestService.cs` / `FileSystemAuditIngestService.cs` — 稽核資料寫入服務
- `Models/Ingest/` — 接收用 DTO (AuditSnapshotUpload、AuditIngestResponse)
- `DemoAuditController.cs` / `TestAuditController.cs` / `testAPI.cs` — 測試/展示用控制器
- `Asseet/DemoData/` — 展示用假資料 JSON (good/bad/mid 情境)
- `wwwroot/` — 靜態前端 (`demo.html`)
- `appsettings.json` — 設定檔 (含 SQL 連線字串等)

### 04.Agent — `AseAudit.Collector` (稽核端，獨立運作的背景服務)

Worker Service，部署於被稽核主機，定期蒐集系統資訊後回報 Server。

- `Worker.cs` — 背景迴圈 (蒐集 → 打包 → 發送 → 休眠)
- `ScriptEngine.cs` — PowerShell 腳本執行引擎
- `Script_lib/` — 稽核腳本蒐集器 (每個檔案對應一種主機資訊)
  - 主要: `HostAccountRuleSnapshot.cs` (帳號規則)、`HostStatusSnapshot.cs` (狀態)、`EventStatusSnapshot.cs` (事件日誌)
  - `ScriptRegistry.cs` — 腳本註冊表
  - `test/` — 各稽核項目蒐集器 (共 27 個)，涵蓋密碼政策、防毒、防火牆、安裝程式、網路介面、使用者群組、稽核日誌等
- `ToJSON/` — 腳本結果轉 JSON 的轉換器
  - `IScriptJsonConverter.cs` — 轉換器介面
  - `JsonConverterRegistry.cs` — 轉換器註冊
  - 實作: HostAccountRuleSnapshotConverter、HostStatusSnapshotConverter、EventStatusSnapshotConverter

### 05.Shared — `ASEAudit.Shared` (共用層，溝通橋樑)

定義 Agent ↔ Server 之間的資料格式 (DTO)，確保 JSON 序列化一致。

- `Contracts/` — API 傳輸 DTO
  - `HostAccountSnapshotPayload.cs` — 帳號快照傳輸格式 (對應資料表 Identification_AM_Account)
  - `HostAccountRuleSnapshotPayload.cs` — 帳號規則 (網域/匿名存取) 傳輸格式 (對應資料表 Identification_AM_rule)
- `Scoring/` — 評分模型
  - `AuditItemResult.cs` — 單一稽核項目評分結果
  - `ScoreAggregator.cs` — 評分彙總

## 編譯時依賴關係

```
AseAudit.Api         → Core, Infrastructure, Shared
AseAudit.Collector   → Shared (不引用 Core/Infrastructure)
AseAudit.Infrastructure → Core
AseAudit.Core        → 不依賴任何內部專案 (獨立)
```

## 慣例

- 使用繁體中文溝通
- 程式碼中的註解可使用中文
- Commit message 使用中文
- 稽核項目需符合 MODA 資安規範（如 SR 2.8 事件日誌保留 6 個月以上）

## 建置與執行

```bash
dotnet build ASE_Audit.sln
dotnet run --project AseAudit.Api        # 啟動 API (Server 端)
dotnet run --project AseAudit.Collector  # 啟動 Collector (Agent 端)
```

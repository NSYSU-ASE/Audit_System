# Audit System (ASE Audit)

NSYSU 資訊安全稽核系統，用於自動化執行稽核腳本並產出評分報告。

## 技術棧

- .NET 8 / C#
- ASP.NET Core Web API (Swagger/OpenAPI)
- PowerShell SDK (`Microsoft.PowerShell.SDK`) 用於執行稽核腳本
- GitHub repo: NSYSU-ASE/Audit_System

## 專案架構

Solution 分為五個層級（Solution Folders）：

| Solution Folder | 專案 | 說明 |
|----------------|------|------|
| **Presentation** | `AseAudit.Api` | Web API 層，提供 Ingest endpoint 接收稽核資料 |
| **Agent** | `AseAudit.Collector` | Worker Service，執行稽核腳本收集主機資訊 |
| **Core** | `AseAudit.Core` | 領域模型、Entities、排程任務 |
| **Infrastructure** | `AseAudit.Infrastructure` | 資料存取、SQL |
| **Shared** | `ASEAudit.Shared` | 共用模型（如 `AuditItemResult` 評分結果） |

## 資料流

```
Collector (執行 PowerShell 稽核腳本)
  → 收集主機資訊 (帳號、狀態、事件日誌等)
  → 轉換為 JSON
  → POST 到 Api 的 IngestController
  → 儲存 / 評分
```

## 慣例

- 使用繁體中文溝通
- 程式碼中的註解可使用中文
- Commit message 使用中文
- 稽核項目需符合 MODA 資安規範（如 SR 2.8 事件日誌保留 6 個月以上）

## 建置與執行

```bash
dotnet build ASE_Audit.sln
dotnet run --project AseAudit.Api        # 啟動 API
dotnet run --project AseAudit.Collector  # 啟動 Collector
```

# FirewallRuleSnapshot Pipeline 實作計畫

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 完成 FirewallRuleSnapshot Server 端串接，讓 Agent 收集的 Windows 防火牆規則能正確寫入 `[dbo].[FireWallRule]` 資料表。

**Architecture:** Agent 端（Script / Converter / Payload / JsonConverterRegistry）已全部完成；Core Entity `FireWallRule` 亦已存在。本計畫補完 ScriptRegistry 登錄 + Infrastructure 層（SQL / DbContext / Repository）+ Server 層（Mapper / Handler / DI 註冊）共 8 個步驟。Firewall 規則為 1:N（一次 Payload 含多條規則），Mapper 回傳 `List<FireWallRule>`，Repository 提供 `AddRangeAsync`。

**Tech Stack:** .NET 8 / C#, ASP.NET Core, Entity Framework Core 8, SQL Server

---

## 檔案對應

| 狀態 | 檔案 | 說明 |
|------|------|------|
| ✅ 已存在 | `AseAudit.Collector/Script_lib/FirewallRuleSnapshot.cs` | PowerShell 腳本 |
| ✅ 已存在 | `ASEAudit.Shared/Contracts/FirewallRuleSnapshotPayload.cs` | Contract + Entry |
| ✅ 已存在 | `AseAudit.Collector/ToJSON/FirewallRuleSnapshotConverter.cs` | JSON 轉換器 |
| ✅ 已存在 | `AseAudit.Collector/ToJSON/JsonConverterRegistry.cs` | 已含 FirewallRuleSnapshotConverter |
| ✅ 已存在 | `AseAudit.Core/Entities/FireWallRule.cs` | EF Entity |
| **修改** | `AseAudit.Collector/Script_lib/ScriptRegistry.cs` | 加入 FirewallRuleSnapshot 登錄 |
| **新建** | `AseAudit.Infrastructure/SQL_file/Firewall_FireWallRule_table_create.sql` | 建表腳本 |
| **修改** | `AseAudit.Infrastructure/Data/AuditDbContext.cs` | 加 DbSet + OnModelCreating |
| **新建** | `AseAudit.Infrastructure/Repositories/IFireWallRuleRepository.cs` | Repository 介面 |
| **新建** | `AseAudit.Infrastructure/Repositories/FireWallRuleRepository.cs` | Repository 實作 |
| **修改** | `AseAudit.Infrastructure/Repositories/RepositoryRegistration.cs` | 加 AddScoped |
| **新建** | `AseAudit.Infrastructure/Mapping/FirewallRuleSnapshotMapper.cs` | Payload → List\<FireWallRule\> |
| **新建** | `AseAudit.Api/Services/Ingest/Firewall/FirewallRuleSnapshotHandler.cs` | ISnapshotHandler 實作 |
| **修改** | `AseAudit.Api/Services/Ingest/SnapshotHandlerRegistration.cs` | 加 AddScoped |

---

## Task 1: ScriptRegistry — 登錄 FirewallRuleSnapshot

**Files:**
- Modify: `AseAudit.Collector/Script_lib/ScriptRegistry.cs`

目前 `ScriptRegistry.All` 缺少 `FirewallRuleSnapshot`，Agent 不會執行此腳本。

- [ ] **Step 1: 加入登錄**

在 `ScriptRegistry.cs` 的 `All` 字典加入一行（仿照現有格式，key 使用 `FirewallRuleSnapshotPayload.Script` 常數）：

```csharp
using ASEAudit.Shared.Contracts;

namespace AseAudit.Collector.Script_lib;

public static class ScriptRegistry
{
    public static readonly IReadOnlyDictionary<string, string> All =
        new Dictionary<string, string>
        {
            [HostAccountSnapshotPayload.Script]     = HostAccountSnapshot.Content,
            [HostAccountRuleSnapshotPayload.Script] = HostAccountRuleSnapshot.Content,
            [PasswordPolicySnapshotPayload.Script]  = PasswordPolicySnapshot.Content,
            [nameof(EventStatusSnapshot)]           = EventStatusSnapshot.Content,
            [FirewallRuleSnapshotPayload.Script]    = FirewallRuleSnapshot.Content,
        };
}
```

- [ ] **Step 2: 建置確認**

```powershell
dotnet build AseAudit.Collector --no-restore
```

預期：`Build succeeded.`（0 errors）

- [ ] **Step 3: Commit**

```powershell
git add AseAudit.Collector/Script_lib/ScriptRegistry.cs
git commit -m "feat(agent): 將 FirewallRuleSnapshot 加入 ScriptRegistry"
```

---

## Task 2: SQL 建表腳本

**Files:**
- Create: `AseAudit.Infrastructure/SQL_file/Firewall_FireWallRule_table_create.sql`

建立 `[dbo].[FireWallRule]` 資料表，欄位需與 `FireWallRule.cs` Entity 一一對應。

- [ ] **Step 1: 新建 SQL 檔案**

```sql
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
```

> **注意**：`LocalPort` 欄位名稱對應 `FireWallRule.cs` 中的 `[Column("LocalPort")]` — C# 屬性名為 `Port`，DB 欄位名為 `LocalPort`，兩者不同。

- [ ] **Step 2: Commit**

```powershell
git add "AseAudit.Infrastructure/SQL_file/Firewall_FireWallRule_table_create.sql"
git commit -m "feat(infra): 新增 FireWallRule 建表 SQL 腳本"
```

---

## Task 3: AuditDbContext — 加入 DbSet 與 OnModelCreating

**Files:**
- Modify: `AseAudit.Infrastructure/Data/AuditDbContext.cs`

- [ ] **Step 1: 加入 DbSet 屬性與 ModelBuilder 設定**

完整更新後的 `AuditDbContext.cs`（在現有內容基礎上，加入標注 `// ← 新增` 的兩處）：

```csharp
using AseAudit.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace AseAudit.Infrastructure.Data
{
    public class AuditDbContext : DbContext
    {
        public AuditDbContext(DbContextOptions<AuditDbContext> options)
            : base(options)
        {
        }

        public DbSet<IdentificationAmAccount> IdentificationAmAccounts { get; set; }
        public DbSet<IdentificationAmRule> IdentificationAmRules { get; set; }
        public DbSet<FireWallRule> FireWallRules { get; set; }  // ← 新增

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<IdentificationAmAccount>(entity =>
            {
                entity.HasIndex(e => e.HostName)
                      .HasDatabaseName("IX_Identification_AM_Account_HostName");
                entity.Property(e => e.CreatedTime)
                      .HasDefaultValueSql("GETDATE()");
            });

            modelBuilder.Entity<IdentificationAmRule>(entity =>
            {
                entity.HasIndex(e => e.HostName)
                      .HasDatabaseName("IX_Identification_AM_rule_HostName");
                entity.Property(e => e.CreatedTime)
                      .HasDefaultValueSql("GETDATE()");
            });

            // ← 新增
            modelBuilder.Entity<FireWallRule>(entity =>
            {
                entity.HasIndex(e => e.HostName)
                      .HasDatabaseName("IX_FireWallRule_HostName");
                entity.Property(e => e.CreatedTime)
                      .HasDefaultValueSql("GETDATE()");
            });
        }
    }
}
```

- [ ] **Step 2: 建置確認**

```powershell
dotnet build AseAudit.Infrastructure --no-restore
```

預期：`Build succeeded.`

- [ ] **Step 3: Commit**

```powershell
git add AseAudit.Infrastructure/Data/AuditDbContext.cs
git commit -m "feat(infra): DbContext 加入 FireWallRule DbSet 與 model 設定"
```

---

## Task 4: Repository 介面 + 實作

**Files:**
- Create: `AseAudit.Infrastructure/Repositories/IFireWallRuleRepository.cs`
- Create: `AseAudit.Infrastructure/Repositories/FireWallRuleRepository.cs`

防火牆規則為 1:N，使用 `AddRangeAsync` 而非 `AddAsync`。

- [ ] **Step 1: 建立介面**

新建 `AseAudit.Infrastructure/Repositories/IFireWallRuleRepository.cs`：

```csharp
using AseAudit.Core.Entities;

namespace AseAudit.Infrastructure.Repositories;

/// <summary>
/// <see cref="FireWallRule"/> 的持久化介面。
/// </summary>
public interface IFireWallRuleRepository
{
    /// <summary>批次寫入多筆規則，回傳寫入筆數。</summary>
    Task<int> AddRangeAsync(IEnumerable<FireWallRule> entities, CancellationToken cancellationToken);
}
```

- [ ] **Step 2: 建立實作**

新建 `AseAudit.Infrastructure/Repositories/FireWallRuleRepository.cs`：

```csharp
using AseAudit.Core.Entities;
using AseAudit.Infrastructure.Data;

namespace AseAudit.Infrastructure.Repositories;

public sealed class FireWallRuleRepository : IFireWallRuleRepository
{
    private readonly AuditDbContext _db;

    public FireWallRuleRepository(AuditDbContext db) => _db = db;

    public async Task<int> AddRangeAsync(IEnumerable<FireWallRule> entities, CancellationToken cancellationToken)
    {
        var list = entities.ToList();
        if (list.Count == 0) return 0;

        _db.FireWallRules.AddRange(list);
        await _db.SaveChangesAsync(cancellationToken);
        return list.Count;
    }
}
```

- [ ] **Step 3: 建置確認**

```powershell
dotnet build AseAudit.Infrastructure --no-restore
```

預期：`Build succeeded.`

- [ ] **Step 4: Commit**

```powershell
git add AseAudit.Infrastructure/Repositories/IFireWallRuleRepository.cs
git add AseAudit.Infrastructure/Repositories/FireWallRuleRepository.cs
git commit -m "feat(infra): 新增 IFireWallRuleRepository 介面與 FireWallRuleRepository 實作"
```

---

## Task 5: RepositoryRegistration — 登錄 FireWallRuleRepository

**Files:**
- Modify: `AseAudit.Infrastructure/Repositories/RepositoryRegistration.cs`

- [ ] **Step 1: 加入 AddScoped**

```csharp
using Microsoft.Extensions.DependencyInjection;

namespace AseAudit.Infrastructure.Repositories;

public static class RepositoryRegistration
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        // Identity 模組
        services.AddScoped<IIdentificationAmAccountRepository, IdentificationAmAccountRepository>();
        services.AddScoped<IIdentificationAmRuleRepository, IdentificationAmRuleRepository>();

        // Firewall 模組
        services.AddScoped<IFireWallRuleRepository, FireWallRuleRepository>();

        return services;
    }
}
```

- [ ] **Step 2: 建置確認**

```powershell
dotnet build AseAudit.Infrastructure --no-restore
```

預期：`Build succeeded.`

- [ ] **Step 3: Commit**

```powershell
git add AseAudit.Infrastructure/Repositories/RepositoryRegistration.cs
git commit -m "feat(infra): RepositoryRegistration 加入 FireWallRuleRepository"
```

---

## Task 6: Mapper — Payload 攤平為 List\<FireWallRule\>

**Files:**
- Create: `AseAudit.Infrastructure/Mapping/FirewallRuleSnapshotMapper.cs`

每條 `FirewallRuleEntry` 對應一筆 `FireWallRule`。注意 `Port` 屬性對應 `LocalPort` 欄位（Entity 的屬性名與欄位名不同，請勿改動 Entity）。

- [ ] **Step 1: 新建 Mapper**

新建 `AseAudit.Infrastructure/Mapping/FirewallRuleSnapshotMapper.cs`：

```csharp
using AseAudit.Core.Entities;
using ASEAudit.Shared.Contracts;

namespace AseAudit.Infrastructure.Mapping;

/// <summary>
/// 將 <see cref="FirewallRuleSnapshotPayload"/> 中每條規則攤平為獨立的
/// <see cref="FireWallRule"/> 實體；回傳清單長度等於 Payload.Rules 筆數。
/// </summary>
public static class FirewallRuleSnapshotMapper
{
    public static List<FireWallRule> ToEntities(FirewallRuleSnapshotPayload payload)
    {
        if (payload is null) throw new ArgumentNullException(nameof(payload));

        return payload.Payload.Rules
            .Select(rule => new FireWallRule
            {
                HostName      = payload.Hostname,
                MACAddress    = null,               // Host Inventory 尚未整合，保留為 null
                RuleName      = rule.Name,
                DisplayName   = rule.DisplayName,
                Status        = rule.Enabled,
                Profile       = rule.Profile,
                Direction     = rule.Direction,
                Action        = rule.Action,
                Protocol      = rule.Protocol,
                Port          = rule.LocalPort,     // Entity [Column("LocalPort")] → Port 屬性
                RemotePort    = rule.RemotePort,
                SourceIP      = rule.LocalAddress,
                DestinationIP = rule.RemoteAddress,
            })
            .ToList();
    }
}
```

- [ ] **Step 2: 建置確認**

```powershell
dotnet build AseAudit.Infrastructure --no-restore
```

預期：`Build succeeded.`

- [ ] **Step 3: Commit**

```powershell
git add AseAudit.Infrastructure/Mapping/FirewallRuleSnapshotMapper.cs
git commit -m "feat(infra): 新增 FirewallRuleSnapshotMapper，Payload → List<FireWallRule>"
```

---

## Task 7: Handler — FirewallRuleSnapshotHandler

**Files:**
- Create: `AseAudit.Api/Services/Ingest/Firewall/FirewallRuleSnapshotHandler.cs`

依 snapshot-pipeline 規範，**入口必加 `if (!upload.Success) return 0;` 守衛**；envelope 欄位覆寫 wire Payload 中的 HostId/Hostname。

- [ ] **Step 1: 新建 Handler（先建目錄再建檔）**

```powershell
New-Item -ItemType Directory -Path "AseAudit.Api/Services/Ingest/Firewall" -Force
```

新建 `AseAudit.Api/Services/Ingest/Firewall/FirewallRuleSnapshotHandler.cs`：

```csharp
using System.Text.Json;
using AseAudit.Api.Models.Ingest;
using AseAudit.Infrastructure.Mapping;
using AseAudit.Infrastructure.Repositories;
using ASEAudit.Shared.Contracts;

namespace AseAudit.Api.Services.Ingest.Firewall;

/// <summary>
/// 處理 <see cref="FirewallRuleSnapshotPayload.Script"/>：防火牆規則批次寫入 FireWallRule。
/// 每個 FirewallRuleEntry 展開為一列；空規則清單回傳 0。
/// </summary>
public sealed class FirewallRuleSnapshotHandler : ISnapshotHandler
{
    private static readonly JsonSerializerOptions DeserializeOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IFireWallRuleRepository _repo;

    public FirewallRuleSnapshotHandler(IFireWallRuleRepository repo) => _repo = repo;

    public string ScriptName => FirewallRuleSnapshotPayload.Script;

    public async Task<int> HandleAsync(AuditSnapshotUpload upload, CancellationToken cancellationToken)
    {
        if (!upload.Success) return 0;

        var wire = upload.Payload.Deserialize<FirewallRuleSnapshotPayload>(DeserializeOptions)
            ?? throw new ArgumentException(
                $"Failed to deserialize Payload as {nameof(FirewallRuleSnapshotPayload)}.");

        var payload = new FirewallRuleSnapshotPayload
        {
            HostId   = string.IsNullOrEmpty(upload.HostId)   ? wire.HostId   : upload.HostId,
            Hostname = string.IsNullOrEmpty(upload.HostName) ? wire.Hostname : upload.HostName,
            Payload  = wire.Payload,
        };

        var entities = FirewallRuleSnapshotMapper.ToEntities(payload);
        return await _repo.AddRangeAsync(entities, cancellationToken);
    }
}
```

- [ ] **Step 2: 建置確認**

```powershell
dotnet build AseAudit.Api --no-restore
```

預期：`Build succeeded.`

- [ ] **Step 3: Commit**

```powershell
git add "AseAudit.Api/Services/Ingest/Firewall/FirewallRuleSnapshotHandler.cs"
git commit -m "feat(api): 新增 FirewallRuleSnapshotHandler，防火牆規則批次寫入"
```

---

## Task 8: SnapshotHandlerRegistration — 登錄 Handler

**Files:**
- Modify: `AseAudit.Api/Services/Ingest/SnapshotHandlerRegistration.cs`

- [ ] **Step 1: 加入 using + AddScoped**

```csharp
using AseAudit.Api.Services.Ingest.Firewall;
using AseAudit.Api.Services.Ingest.Identity;

namespace AseAudit.Api.Services.Ingest;

public static class SnapshotHandlerRegistration
{
    public static IServiceCollection AddSnapshotHandlers(this IServiceCollection services)
    {
        // Identity 模組
        services.AddScoped<ISnapshotHandler, HostAccountSnapshotHandler>();
        services.AddScoped<ISnapshotHandler, HostAccountRuleSnapshotHandler>();

        // Firewall 模組
        services.AddScoped<ISnapshotHandler, FirewallRuleSnapshotHandler>();

        return services;
    }
}
```

- [ ] **Step 2: 完整 Solution 建置**

```powershell
dotnet build ASE_Audit.sln
```

預期：`Build succeeded.`（所有專案 0 errors）

- [ ] **Step 3: Commit**

```powershell
git add AseAudit.Api/Services/Ingest/SnapshotHandlerRegistration.cs
git commit -m "feat(api): SnapshotHandlerRegistration 加入 FirewallRuleSnapshotHandler"
```

---

## 驗收確認清單

完成全部 Task 後，確認以下項目：

- [ ] `dotnet build ASE_Audit.sln` 無 error / warning
- [ ] SQL 腳本已在目標 SQL Server 執行，`[dbo].[FireWallRule]` 資料表存在
- [ ] 執行 Agent 端（`dotnet run --project AseAudit.Collector`），確認 `FirewallRuleSnapshot.json` 落地到 `bin/Debug/net8.0/`
- [ ] Server 啟動後（`dotnet run --project AseAudit.Api`），Agent 上傳不回傳 400 BadRequest
- [ ] 確認 `[dbo].[FireWallRule]` 有新增資料列

## 注意事項

- **Q10 (spec 待釐清)**：`ScriptRegistry.All` 的另外兩個腳本（`EventStatusSnapshot`、`PasswordPolicySnapshot`）目前仍無 Handler，收到時 Server 會回 400。本計畫範圍僅限 FirewallRuleSnapshot，其餘待使用者另行指派。
- **MACAddress**：Mapper 永遠填 `null`，等 Host Inventory 功能上線後統一補齊，不需另行處理。
- **`FireWallRule.CreatedTime`**：不在 C# 端設值，由 SQL `DEFAULT GETDATE()` 自動填入。EF Core 已在 `OnModelCreating` 設定 `HasDefaultValueSql("GETDATE()")`。

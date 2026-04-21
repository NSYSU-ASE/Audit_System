namespace AseAudit.Collector.Script_lib;

/// <summary>
/// [SystemEvent] 事件日誌內容快照 — 收集各類稽核事件的日誌記錄。
///
/// 每筆記錄包含欄位：時間戳記、來源、類別、類型、事件ID、事件結果
///
/// 涵蓋驗證項目：
///   存取控制
///     a. 基本登入/登出 — Event ID: 4624, 4634
///     b. 帳號鎖定事件 — Event ID: 4740
///     c. 網域帳號稽核 — Event ID: 4768, 4769, 4771
///     d. 細粒度稽核 (SACL) — Event ID: 4663, 4656
///
///   請求錯誤
///     a. OS 層認證失敗 — Event ID: 4625, 4771
///
///   作業系統事件
///     a. 系統啟動/關機 — Event ID: 6005, 6006, 6008, 1074 (System Log)
///     b. 服務啟停 — Event ID: 7036, 7045 (System Log)
///     c. 排程工作建立/修改 — Event ID: 4698, 4702 (Security), 106, 140, 141 (TaskScheduler)
///     d. 時鐘變更 — Event ID: 4616
///
///   控制系統事件
///     a. 寫入 Windows 事件日誌 — Application Log 中 SCADA 相關來源
///
///   備份和恢復事件
///     a. Windows 備份 — Event ID: 4, 5, 8, 9 (Backup Log)
///     b. VSS 事件 — Application Log, Provider: VSS
///
///   設定更改
///     a. 帳號管理 — Event ID: 4720, 4722, 4725, 4726, 4738, 4732, 4733
///     b. 登錄檔稽核 — Event ID: 4657
///     c. 稽核政策變更 — Event ID: 4719
///     d. 軟體安裝/移除 — Event ID: 11707, 11708, 11724 (Application Log)
///
///   潛在偵察活動
///     a. 連續登入失敗 — Event ID: 4625, 4740
///
///   稽核日誌事件
///     a. 事件日誌清除 — Event ID: 1102 (Security), 104 (System)
///     b. 稽核政策變更 — Event ID: 4719
///     c. 日誌儲存空間警告 — Event ID: 1105, 1108
///
/// 備註（無法以腳本自動蒐集的項目）：
///   - 控制系統 (SCADA) 事件：需 SCADA 系統設定將紀錄寫入 Windows Event Log，
///     腳本僅搜尋 Application Log 中常見 SCADA 來源名稱。
///   - 細粒度 SACL 事件 (4663, 4656)：需事先對目標物件設定 SACL，否則不會產生事件。
///   - 登錄檔稽核事件 (4657)：需事先對目標機碼設定 SACL，否則不會產生事件。
///
/// 輸出：JSON 物件（各類事件最近 50 筆，包含時間戳記、來源、類別、類型、事件ID、事件結果）
/// </summary>
public static class EventLogSnapshot
{
    public const string Content = @"
# ══════════════════════════════════════════════════════════════
#  EventLogSnapshot — 事件日誌內容收集
#  每筆記錄包含：時間戳記、來源、類別、類型、事件ID、事件結果
# ══════════════════════════════════════════════════════════════

$maxEvents = 50
$daysBack  = 7
$startTime = (Get-Date).AddDays(-$daysBack)

# ── 共用函式：查詢事件日誌並格式化輸出 ──
function Get-FormattedEvents {
    param(
        [string]$LogName = 'Security',
        [int[]]$Ids,
        [string]$ProviderName = $null,
        [int]$Max = $maxEvents
    )
    try {
        $filter = @{ LogName = $LogName; StartTime = $startTime }
        if ($Ids)          { $filter['Id'] = $Ids }
        if ($ProviderName) { $filter['ProviderName'] = $ProviderName }

        Get-WinEvent -FilterHashtable $filter -MaxEvents $Max -ErrorAction SilentlyContinue |
        ForEach-Object {
            # 從 Message 中擷取簡短結果描述（第一行，最多 200 字）
            $rawLine = if ($_.Message) { $_.Message.Split(""`n"")[0].Trim() } else { '' }
            $msgFirstLine = if ($rawLine.Length -gt 200) { $rawLine.Substring(0, 200) + '...' } else { $rawLine }

            @{
                TimeStamp = $_.TimeCreated.ToString('yyyy-MM-dd HH:mm:ss')
                Source    = $_.ProviderName
                Category  = $_.Task.ToString()
                Type      = $_.LevelDisplayName
                EventId   = $_.Id
                Result    = $msgFirstLine
            }
        }
    } catch { @() }
}

# ════════════════════════════════════════
# 1. 存取控制
# ════════════════════════════════════════

# a. 基本登入/登出
$logonLogoff = Get-FormattedEvents -Ids @(4624, 4634)

# b. 帳號鎖定事件
$accountLockout = Get-FormattedEvents -Ids @(4740)

# c. 網域帳號稽核 (Kerberos)
$domainAccount = Get-FormattedEvents -Ids @(4768, 4769, 4771)

# d. 細粒度稽核 (SACL 觸發的物件存取事件)
$saclEvents = Get-FormattedEvents -Ids @(4663, 4656)

# ════════════════════════════════════════
# 2. 請求錯誤 — OS 層認證失敗
# ════════════════════════════════════════
$authFailure = Get-FormattedEvents -Ids @(4625, 4771)

# ════════════════════════════════════════
# 3. 作業系統事件
# ════════════════════════════════════════

# a. 系統啟動/關機 (System Log)
$startupShutdown = Get-FormattedEvents -LogName 'System' -Ids @(6005, 6006, 6008, 1074)

# b. 服務啟停 (System Log)
$serviceEvents = Get-FormattedEvents -LogName 'System' -Ids @(7036, 7045)

# c. 排程工作建立/修改 (Security Log)
$scheduledTaskSecurity = Get-FormattedEvents -Ids @(4698, 4702)
# TaskScheduler Operational Log
$scheduledTaskOps = Get-FormattedEvents -LogName 'Microsoft-Windows-TaskScheduler/Operational' -Ids @(106, 140, 141)

# d. 時鐘變更
$timeChange = Get-FormattedEvents -Ids @(4616)

# ════════════════════════════════════════
# 4. 控制系統事件 — Application Log 中 SCADA 相關來源
# ════════════════════════════════════════
$scadaEvents = @()
$scadaSources = @('OPC', 'OPCServer', 'Wonderware', 'InTouch', 'iFIX', 'FactoryTalk',
                  'WinCC', 'SCADA', 'Ignition', 'CitectSCADA', 'RSLinx')
foreach ($src in $scadaSources) {
    try {
        $found = Get-WinEvent -FilterHashtable @{
            LogName      = 'Application'
            ProviderName = $src
            StartTime    = $startTime
        } -MaxEvents 10 -ErrorAction SilentlyContinue
        if ($found) {
            $scadaEvents += $found | ForEach-Object {
                @{
                    TimeStamp = $_.TimeCreated.ToString('yyyy-MM-dd HH:mm:ss')
                    Source    = $_.ProviderName
                    Category  = $_.Task.ToString()
                    Type      = $_.LevelDisplayName
                    EventId   = $_.Id
                    Result    = if ($_.Message) { $_.Message.Substring(0, [Math]::Min(200, $_.Message.Length)) } else { '' }
                }
            }
        }
    } catch { }
}

# ════════════════════════════════════════
# 5. 備份和恢復事件
# ════════════════════════════════════════

# a. Windows 備份工具事件
$backupEvents = Get-FormattedEvents -LogName 'Application' -ProviderName 'Microsoft-Windows-Backup'

# b. VSS 事件
$vssEvents = Get-FormattedEvents -LogName 'Application' -ProviderName 'VSS'

# ════════════════════════════════════════
# 6. 設定更改
# ════════════════════════════════════════

# a. 帳號管理稽核
$accountMgmt = Get-FormattedEvents -Ids @(4720, 4722, 4725, 4726, 4738, 4732, 4733)

# b. 登錄檔稽核 (需 SACL)
$registryAudit = Get-FormattedEvents -Ids @(4657)

# c. 稽核政策變更
$auditPolicyChange = Get-FormattedEvents -Ids @(4719)

# d. 軟體安裝/移除 (Application Log, MsiInstaller)
$softwareInstall = Get-FormattedEvents -LogName 'Application' -Ids @(11707, 11708, 11724)

# ════════════════════════════════════════
# 7. 潛在偵察活動
# ════════════════════════════════════════

# a. 連續登入失敗與帳號鎖定
$reconEvents = Get-FormattedEvents -Ids @(4625, 4740)

# ════════════════════════════════════════
# 8. 稽核日誌事件
# ════════════════════════════════════════

# a. 事件日誌清除
$logCleared = @()
$logCleared += Get-FormattedEvents -Ids @(1102)
$logCleared += Get-FormattedEvents -LogName 'System' -Ids @(104)

# b. 稽核政策變更 (同上 4719)
# 已在 $auditPolicyChange 中收集

# c. 日誌儲存空間警告
$logStorageWarning = Get-FormattedEvents -Ids @(1105, 1108)

# ── 輸出 ──
@{
    AccessControl = @{
        LogonLogoff    = @($logonLogoff)
        AccountLockout = @($accountLockout)
        DomainAccount  = @($domainAccount)
        SaclEvents     = @($saclEvents)
    }
    AuthenticationFailure = @($authFailure)
    OperatingSystem = @{
        StartupShutdown      = @($startupShutdown)
        ServiceEvents        = @($serviceEvents)
        ScheduledTaskSec     = @($scheduledTaskSecurity)
        ScheduledTaskOps     = @($scheduledTaskOps)
        TimeChange           = @($timeChange)
    }
    ControlSystem = @{
        ScadaEvents = @($scadaEvents)
        Note        = 'SCADA 紀錄需由控制系統本身設定寫入 Windows Event Log'
    }
    BackupRestore = @{
        BackupEvents = @($backupEvents)
        VssEvents    = @($vssEvents)
    }
    ConfigChange = @{
        AccountManagement  = @($accountMgmt)
        RegistryAudit      = @($registryAudit)
        AuditPolicyChange  = @($auditPolicyChange)
        SoftwareInstall    = @($softwareInstall)
    }
    ReconActivity = @($reconEvents)
    AuditLogEvents = @{
        LogCleared         = @($logCleared)
        AuditPolicyChange  = @($auditPolicyChange)
        LogStorageWarning  = @($logStorageWarning)
    }
} | ConvertTo-Json -Depth 5
";
}

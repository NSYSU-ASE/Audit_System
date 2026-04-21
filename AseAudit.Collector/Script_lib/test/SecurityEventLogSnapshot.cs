namespace AseAudit.Collector.Script_lib;

/// <summary>
/// [SystemEvent] 安全性事件日誌快照 — 收集授權相關的稽核事件。
///
/// 涵蓋驗證項目：
///   RE 3 #2 — 確認覆蓋（Override）行為完整記錄於稽核日誌
///              查詢特殊權限指派、權限提升等事件
///   RE 4 #3 — 確認雙人核准紀錄存於稽核日誌
///              查詢帳號管理與授權變更事件
///
/// 收集的 Windows Security Event ID：
///   4672 — 特殊權限登入（Special Privileges Assigned，Override 指標）
///   4673 — 特權服務呼叫（Privileged Service Called）
///   4674 — 對特權物件的操作嘗試
///   4720 — 帳號建立（偵測未授權新帳號）
///   4732 — 成員加入安全群組（權限提升偵測）
///   4728 — 成員加入全域安全群組
///   4756 — 成員加入通用安全群組
///   4625 — 登入失敗（暴力破解偵測）
///   4648 — 使用明確憑證登入（RunAs / 代理登入）
///
/// 輸出：JSON 物件（各類事件最近 100 筆）
///   - PrivilegeAssignEvents:  特殊權限登入事件（4672）
///   - PrivilegedServiceCalls: 特權服務呼叫事件（4673, 4674）
///   - AccountChangeEvents:    帳號 / 群組變更事件（4720, 4732, 4728, 4756）
///   - FailedLogonEvents:      登入失敗事件（4625）
///   - ExplicitCredEvents:     明確憑證登入事件（4648）
///   - AuditPolicyStatus:      目前稽核原則啟用狀態
/// </summary>
public static class SecurityEventLogSnapshot
{
    public const string Content = @"
$maxEvents = 100
$daysBack  = 7
$startTime = (Get-Date).AddDays(-$daysBack)

# ── 共用函式：安全地查詢事件日誌 ──
function Get-SafeEventLog {
    param([int[]]$Ids, [int]$Max = $maxEvents)
    try {
        Get-WinEvent -FilterHashtable @{
            LogName   = 'Security'
            Id        = $Ids
            StartTime = $startTime
        } -MaxEvents $Max -ErrorAction SilentlyContinue |
        Select-Object Id, TimeCreated, LevelDisplayName, Message |
        ForEach-Object {
            @{
                EventId     = $_.Id
                TimeCreated = $_.TimeCreated.ToString('o')
                Level       = $_.LevelDisplayName
                Message     = $_.Message.Substring(0, [Math]::Min(500, $_.Message.Length))
            }
        }
    } catch { @() }
}

# ── RE 3 #2：特殊權限登入（Override 指標） ──
$privAssign = Get-SafeEventLog -Ids @(4672)

# ── 特權服務呼叫 ──
$privService = Get-SafeEventLog -Ids @(4673, 4674)

# ── RE 4 #3 / SR 2.1 #3：帳號與群組變更事件 ──
$accountChanges = Get-SafeEventLog -Ids @(4720, 4732, 4728, 4756)

# ── 登入失敗（暴力破解偵測） ──
$failedLogon = Get-SafeEventLog -Ids @(4625)

# ── 明確憑證登入（RunAs / 代理操作） ──
$explicitCred = Get-SafeEventLog -Ids @(4648)

# ── 目前稽核原則狀態 ──
$auditPolicy = auditpol /get /category:* | Out-String

@{
    PrivilegeAssignEvents  = @($privAssign)
    PrivilegedServiceCalls = @($privService)
    AccountChangeEvents    = @($accountChanges)
    FailedLogonEvents      = @($failedLogon)
    ExplicitCredEvents     = @($explicitCred)
    AuditPolicyStatus      = $auditPolicy
} | ConvertTo-Json -Depth 4
";
}

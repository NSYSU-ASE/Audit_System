namespace AseAudit.Collector.Script_lib;

/// <summary>
/// [DataManagement] 保護稽核資訊快照 — 驗證稽核記錄、設定、報告的存取保護。
///
/// 涵蓋驗證項目：
///   SR 3.9 #1 — 控制系統是否保護稽核資訊（記錄、設定、報告）防止未經授權的存取（SL 1 未選用）
///   SR 3.9 #2 — 控制系統是否保護稽核資訊防止未經授權的修改和刪除（SL 2～3）
///               收集事件日誌檔案 ACL、稽核原則設定
///   SR 3.9 #3 — 稽核工具是否受到保護
///               收集稽核相關執行檔的完整性與權限
///   SR 3.9 RE(1) #4 — 是否在硬體強制的一次寫入媒體上產生稽核記錄（SL 4）
///               → 需實體稽核確認 WORM 儲存裝置；此腳本檢查是否有 Syslog 遠端轉發設定作為替代
///
///   【區域層級 Zone】
///   #5 — 各區域是否依據安全等級對稽核資訊保護納入區域安全政策
///   #6 — 區域內稽核資訊的存取權限是否已明確定義和控制
///       → 需搭配架構文件與政策審查
///
///   【元件層級 CR 3.9】
///   #7 — 元件是否保護稽核資訊、稽核來源和稽核工具（如存儲）防止未經授權的存取、修改和刪除
///         （SL 1 未選用）
///   #8 — 稽核資訊（記錄、設定、報告）的完整性是否得到保障（SL 2～3）
///   CR 3.9 RE(1) #9 — 元件是否能在硬體強制的一次寫入媒體上儲存稽核記錄（SL 4）
///       → 需個別元件測試
///
/// 輸出：JSON 物件
///   - EventLogFileACL: 各事件日誌檔案的存取控制清單
///   - AuditPolicy: 系統稽核原則設定
///   - AuditToolIntegrity: 稽核相關工具（auditpol.exe、wevtutil.exe）的檔案雜湊與 ACL
///   - EventLogServiceConfig: 事件日誌服務設定
///   - SyslogForwarding: Windows 事件轉發（WEF）設定（作為遠端稽核備份指標）
/// </summary>
public static class AuditInfoProtectionSnapshot
{
    public const string Content = @"
# ── SR 3.9 #1 #2：事件日誌檔案 ACL ──
$logFiles = @(
    ""$env:SystemRoot\System32\winevt\Logs\Security.evtx"",
    ""$env:SystemRoot\System32\winevt\Logs\System.evtx"",
    ""$env:SystemRoot\System32\winevt\Logs\Application.evtx""
)

$logAcls = foreach ($f in $logFiles) {
    if (Test-Path $f) {
        $acl = Get-Acl -Path $f -ErrorAction SilentlyContinue
        @{
            FilePath = $f
            Owner    = $acl.Owner
            Access   = @($acl.Access | ForEach-Object {
                @{
                    Identity   = $_.IdentityReference.Value
                    Rights     = $_.FileSystemRights.ToString()
                    AccessType = $_.AccessControlType.ToString()
                    Inherited  = $_.IsInherited
                }
            })
        }
    }
}

# ── SR 3.9 #2：系統稽核原則設定 ──
$auditPolicy = auditpol /get /category:* 2>$null | Out-String

# ── SR 3.9 #3：稽核工具完整性與權限 ──
$auditTools = @(
    ""$env:SystemRoot\System32\auditpol.exe"",
    ""$env:SystemRoot\System32\wevtutil.exe"",
    ""$env:SystemRoot\System32\eventvwr.exe""
)

$toolIntegrity = foreach ($tool in $auditTools) {
    if (Test-Path $tool) {
        $hash = Get-FileHash -Path $tool -Algorithm SHA256 -ErrorAction SilentlyContinue
        $acl = Get-Acl -Path $tool -ErrorAction SilentlyContinue
        $fileInfo = Get-Item $tool -ErrorAction SilentlyContinue
        @{
            FilePath     = $tool
            SHA256       = $hash.Hash
            Owner        = $acl.Owner
            Size         = $fileInfo.Length
            LastModified = $fileInfo.LastWriteTime.ToString('o')
            Access       = @($acl.Access | Select-Object -First 5 | ForEach-Object {
                @{
                    Identity   = $_.IdentityReference.Value
                    Rights     = $_.FileSystemRights.ToString()
                    AccessType = $_.AccessControlType.ToString()
                }
            })
        }
    }
}

# ── SR 3.9 #2：事件日誌服務設定（確保服務受保護） ──
$eventLogSvc = try {
    $svc = Get-Service -Name EventLog -ErrorAction SilentlyContinue
    $wmi = Get-WmiObject Win32_Service -Filter ""Name='EventLog'"" -ErrorAction SilentlyContinue
    @{
        Status     = $svc.Status.ToString()
        StartType  = $svc.StartType.ToString()
        LogOnAs    = $wmi.StartName
        PathName   = $wmi.PathName
    }
} catch { @{ Error = $_.Exception.Message } }

# ── SR 3.9 RE(1) #4：Windows 事件轉發（WEF）設定 ──
$wefConfig = try {
    $wecSvc = Get-Service -Name Wecsvc -ErrorAction SilentlyContinue
    $subscriptions = wecutil es 2>$null
    @{
        WECServiceStatus  = if ($wecSvc) { $wecSvc.Status.ToString() } else { 'NotInstalled' }
        Subscriptions     = @($subscriptions)
    }
} catch { @{ Error = $_.Exception.Message } }

# 檢查 Windows Event Forwarding（WEF）客戶端設定
$wefClient = try {
    $regPath = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\EventLog\EventForwarding\SubscriptionManager'
    if (Test-Path $regPath) {
        $props = Get-ItemProperty -Path $regPath -ErrorAction SilentlyContinue
        @{
            Configured = $true
            Settings   = $props | Select-Object -ExcludeProperty PSPath,PSParentPath,PSChildName,PSProvider | ConvertTo-Json -Depth 2
        }
    } else { @{ Configured = $false } }
} catch { @{ Configured = $false } }

@{
    EventLogFileACL       = @($logAcls)
    AuditPolicy           = $auditPolicy
    AuditToolIntegrity    = @($toolIntegrity)
    EventLogServiceConfig = $eventLogSvc
    SyslogForwarding      = @{
        WECServer = $wefConfig
        WEFClient = $wefClient
    }
} | ConvertTo-Json -Depth 5
";
}

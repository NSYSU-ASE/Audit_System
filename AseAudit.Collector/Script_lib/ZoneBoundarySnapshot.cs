namespace AseAudit.Collector.Script_lib;

/// <summary>
/// [Firewall] 區域邊界保護快照 — 驗證 SR 5.2 Zone Boundary Protection。
///
/// 涵蓋驗證項目：
///   SR 5.2      — 是否具備監視和控制區域邊界通訊的能力，是否透過管理介面（防火牆、代理等）與外部連接
///   SR 5.2 RE(1) — 是否實施預設拒絕所有流量、僅允許例外的策略（Deny All, Permit by Exception）
///   SR 5.2 RE(2) — 是否具備島嶼模式能力（安全阻斷所有邊界通訊，用於偵測到入侵或攻擊時）
///   SR 5.2 RE(3) — 邊界保護機制失敗時是否自動通道失敗（Fail Close），且不影響 SIS 安全功能
///
/// 註釋：
///   RE(2) 島嶼模式為系統架構層級設計，需搭配網路架構與應急程序文件驗證。
///   本腳本收集防火牆設定檔預設動作與進階設定，供判斷是否具備快速隔離能力。
///   RE(3) Fail Close 行為需搭配 SIS 系統獨立性評估，本腳本僅收集防火牆服務
///   自動啟動與依賴關係設定。
///
/// 輸出：JSON 物件
///   - FirewallProfiles:     各防火牆設定檔的預設動作、日誌設定、通知設定
///   - DefaultDenyCheck:     各設定檔是否符合預設拒絕策略（RE(1) 關鍵指標）
///   - FirewallServiceInfo:  防火牆服務狀態與啟動類型（RE(3) Fail Close 佐證）
///   - IpsecRules:           IPsec 規則清單（邊界加密與驗證）
///   - FirewallLogSettings:  防火牆日誌設定（監視能力佐證）
///   - InboundBlockStats:    入站封鎖規則統計
/// </summary>
public static class ZoneBoundarySnapshot
{
    public const string Content = @"
# ── SR 5.2：防火牆設定檔詳細資訊 ──
$fwProfiles = Get-NetFirewallProfile -ErrorAction SilentlyContinue |
    ForEach-Object {
        @{
            Name                  = $_.Name
            Enabled               = $_.Enabled
            DefaultInboundAction  = $_.DefaultInboundAction.ToString()
            DefaultOutboundAction = $_.DefaultOutboundAction.ToString()
            AllowInboundRules     = $_.AllowInboundRules.ToString()
            AllowLocalFirewallRules = $_.AllowLocalFirewallRules.ToString()
            NotifyOnListen        = $_.NotifyOnListen
            LogFileName           = $_.LogFileName
            LogMaxSizeKilobytes   = $_.LogMaxSizeKilobytes
            LogAllowed            = $_.LogAllowed
            LogBlocked            = $_.LogBlocked
        }
    }

# ── SR 5.2 RE(1)：預設拒絕策略檢查 ──
# 合規條件：入站預設動作應為 Block，出站理想上也應為 Block
$defaultDenyCheck = Get-NetFirewallProfile -ErrorAction SilentlyContinue |
    ForEach-Object {
        @{
            Profile              = $_.Name
            InboundDefaultBlock  = ($_.DefaultInboundAction.ToString() -eq 'Block')
            OutboundDefaultBlock = ($_.DefaultOutboundAction.ToString() -eq 'Block')
            IsCompliant_RE1      = ($_.DefaultInboundAction.ToString() -eq 'Block')
        }
    }

# ── SR 5.2 RE(3)：防火牆服務狀態與啟動類型（Fail Close 佐證） ──
# 防火牆服務應設為自動啟動，確保開機即啟用保護
$fwService = @{}
try {
    $svc = Get-Service -Name 'MpsSvc' -ErrorAction SilentlyContinue
    $svcWmi = Get-CimInstance -ClassName Win32_Service -Filter ""Name='MpsSvc'"" -ErrorAction SilentlyContinue
    $fwService = @{
        ServiceName = 'MpsSvc'
        DisplayName = $svc.DisplayName
        Status      = $svc.Status.ToString()
        StartType   = $svcWmi.StartMode
        # 檢查防火牆服務依賴項（確保不會因依賴服務失敗而停止）
        DependentServices = @($svc.DependentServices | ForEach-Object { $_.Name })
        ServicesDependedOn = @($svc.ServicesDependedOn | ForEach-Object { $_.Name })
    }
} catch { }

# ── SR 5.2：IPsec 規則（邊界通訊加密與驗證） ──
$ipsecRules = Get-NetIPsecRule -ErrorAction SilentlyContinue |
    Select-Object DisplayName, Enabled, InboundSecurity, OutboundSecurity, Mode, Profile |
    ForEach-Object {
        @{
            DisplayName      = $_.DisplayName
            Enabled          = $_.Enabled
            InboundSecurity  = $_.InboundSecurity.ToString()
            OutboundSecurity = $_.OutboundSecurity.ToString()
            Mode             = $_.Mode.ToString()
            Profile          = $_.Profile.ToString()
        }
    }

# ── SR 5.2：防火牆日誌設定（監視能力佐證） ──
$logSettings = Get-NetFirewallProfile -ErrorAction SilentlyContinue |
    ForEach-Object {
        $logFile = $_.LogFileName
        $logExists = if ($logFile) { Test-Path $logFile -ErrorAction SilentlyContinue } else { $false }
        @{
            Profile           = $_.Name
            LogEnabled        = ($_.LogBlocked -or $_.LogAllowed)
            LogBlocked        = [bool]$_.LogBlocked
            LogAllowed        = [bool]$_.LogAllowed
            LogFilePath       = $logFile
            LogFileExists     = $logExists
            LogMaxSizeKB      = $_.LogMaxSizeKilobytes
        }
    }

# ── SR 5.2：入站封鎖規則統計 ──
$inboundStats = @{
    TotalInboundRules  = (Get-NetFirewallRule -Direction Inbound -ErrorAction SilentlyContinue | Measure-Object).Count
    EnabledBlockRules  = (Get-NetFirewallRule -Direction Inbound -Enabled True -Action Block -ErrorAction SilentlyContinue | Measure-Object).Count
    EnabledAllowRules  = (Get-NetFirewallRule -Direction Inbound -Enabled True -Action Allow -ErrorAction SilentlyContinue | Measure-Object).Count
    TotalOutboundRules = (Get-NetFirewallRule -Direction Outbound -ErrorAction SilentlyContinue | Measure-Object).Count
}

@{
    FirewallProfiles   = @($fwProfiles)
    DefaultDenyCheck   = @($defaultDenyCheck)
    FirewallServiceInfo = $fwService
    IpsecRules         = @($ipsecRules)
    FirewallLogSettings = @($logSettings)
    InboundBlockStats  = $inboundStats
} | ConvertTo-Json -Depth 4
";
}

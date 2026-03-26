namespace AseAudit.Collector.Script_lib;

/// <summary>
/// [DataManagement] 控制系統恢復與重建快照 — 驗證系統災後恢復能力。
///
/// 涵蓋驗證項目：
///   SR 7.4 #1 — 控制系統是否能在中斷或障礙後恢復和重建到已知安全狀態（SL 1～4）
///               收集系統還原設定、WinRE 狀態
///   SR 7.4 #2 — 所有系統參數（預設或可設定）是否設置為安全值
///               收集安全基準比對（Secure Boot、UAC、安全選項）
///   SR 7.4 #3 — 是否能執行重新安裝安全關鍵修補程序
///               收集 Windows Update 設定與最近安裝的更新
///   SR 7.4 #4 — 是否妥善重新建立安全相關設定設置、系統檔案和操作程序
///               收集 SFC（系統檔案檢查器）與 DISM 健康狀態
///   SR 7.4 #5 — 是否能重新安裝應用程式和系統軟體並使用安全設置進行設定
///               收集已安裝更新歷史
///   SR 7.4 #6 — 是否能載入來自最新已知安全備份的資訊
///               → 與 SR 7.3 SystemBackupSnapshot 交叉驗證
///   SR 7.4 #7 — 恢復後是否對系統進行全面測試和運作驗證
///               → 需人工恢復測試程序，無法完全程式化；收集最近的系統完整性檢查結果
///
///   【區域層級 Zone】
///   #8 — 各區域是否依據安全等級對恢復和重建程序納入區域安全政策
///   #9 — 區域內是否已建立恢復優先順序和程序
///       → 需搭配架構文件與政策審查
///
///   【元件層級 CR 7.4】
///   #10 — 元件是否能在中斷或障礙後恢復並重建為已知安全狀態（SL 1～4）
///   #11 — 所有系統參數是否設置為安全值、安全關鍵修補程序是否重新安裝
///   #12 — 安全相關設定設置和操作程序是否可用
///   #13 — 元件是否能使用已建立的設定檔重新安裝和設定
///   #14 — 是否能載入最新已知安全備份的資訊並完成全面測試
///       → 需個別元件測試
///
/// 輸出：JSON 物件
///   - WinREStatus: Windows 復原環境狀態
///   - SecureBoot: 安全開機設定
///   - UACSettings: 使用者帳號控制設定
///   - SystemFileIntegrity: SFC 與 DISM 掃描結果摘要
///   - WindowsUpdateConfig: Windows Update 設定
///   - RecentUpdates: 最近安裝的更新
///   - SystemRestoreConfig: 系統還原設定
/// </summary>
public static class SystemRecoverySnapshot
{
    public const string Content = @"
# ── SR 7.4 #1：Windows 復原環境（WinRE）狀態 ──
$winRE = try {
    $reagentc = reagentc /info 2>$null | Out-String
    @{ Output = $reagentc.Trim() }
} catch { @{ Output = 'N/A' } }

# ── SR 7.4 #2：安全開機（Secure Boot）設定 ──
$secureBoot = try {
    $sbState = Confirm-SecureBootUEFI -ErrorAction SilentlyContinue
    @{ SecureBootEnabled = $sbState }
} catch { @{ SecureBootEnabled = 'NotSupported' } }

# ── SR 7.4 #2：UAC 設定 ──
$uac = try {
    $regPath = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System'
    if (Test-Path $regPath) {
        $props = Get-ItemProperty -Path $regPath -ErrorAction SilentlyContinue
        @{
            EnableLUA                  = $props.EnableLUA
            ConsentPromptBehaviorAdmin = $props.ConsentPromptBehaviorAdmin
            ConsentPromptBehaviorUser  = $props.ConsentPromptBehaviorUser
            PromptOnSecureDesktop      = $props.PromptOnSecureDesktop
            EnableInstallerDetection   = $props.EnableInstallerDetection
            EnableSecureUIAPaths       = $props.EnableSecureUIAPaths
            FilterAdministratorToken   = $props.FilterAdministratorToken
            EnableVirtualization       = $props.EnableVirtualization
        }
    } else { @{ RegistryExists = $false } }
} catch { @{ Error = $_.Exception.Message } }

# ── SR 7.4 #4：系統檔案完整性（SFC 最近掃描結果） ──
$sfcResult = try {
    $cbsLog = ""$env:SystemRoot\Logs\CBS\CBS.log""
    if (Test-Path $cbsLog) {
        $lastLines = Get-Content -Path $cbsLog -Tail 50 -ErrorAction SilentlyContinue |
            Where-Object { $_ -match 'SFC|Verify|Repair|corrupt' } |
            Select-Object -Last 10
        @{
            CBSLogExists   = $true
            RecentSFCLines = @($lastLines)
        }
    } else { @{ CBSLogExists = $false } }
} catch { @{ Error = $_.Exception.Message } }

# ── SR 7.4 #4：DISM 元件存放區健康狀態 ──
$dismHealth = try {
    $dismLog = ""$env:SystemRoot\Logs\DISM\dism.log""
    if (Test-Path $dismLog) {
        $lastLines = Get-Content -Path $dismLog -Tail 30 -ErrorAction SilentlyContinue |
            Where-Object { $_ -match 'Health|Repair|Error|Warning' } |
            Select-Object -Last 10
        @{
            DISMLogExists   = $true
            RecentDISMLines = @($lastLines)
        }
    } else { @{ DISMLogExists = $false } }
} catch { @{ Error = $_.Exception.Message } }

# ── SR 7.4 #3 #5：Windows Update 設定 ──
$wuConfig = try {
    $auRegPath = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU'
    $wuRegPath = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate'
    $auSettings = @{}
    if (Test-Path $auRegPath) {
        $props = Get-ItemProperty -Path $auRegPath -ErrorAction SilentlyContinue
        $auSettings.NoAutoUpdate = $props.NoAutoUpdate
        $auSettings.AUOptions = $props.AUOptions
        $auSettings.ScheduledInstallDay = $props.ScheduledInstallDay
        $auSettings.ScheduledInstallTime = $props.ScheduledInstallTime
    }
    $wuSettings = @{}
    if (Test-Path $wuRegPath) {
        $props = Get-ItemProperty -Path $wuRegPath -ErrorAction SilentlyContinue
        $wuSettings.WUServer = $props.WUServer
        $wuSettings.WUStatusServer = $props.WUStatusServer
    }
    @{ AutoUpdate = $auSettings; WUServer = $wuSettings }
} catch { @{ Error = $_.Exception.Message } }

# ── SR 7.4 #3 #5：最近安裝的更新 ──
$recentUpdates = try {
    Get-HotFix -ErrorAction SilentlyContinue |
        Sort-Object InstalledOn -Descending -ErrorAction SilentlyContinue |
        Select-Object -First 20 |
        ForEach-Object {
            @{
                HotFixID    = $_.HotFixID
                Description = $_.Description
                InstalledBy = $_.InstalledBy
                InstalledOn = if ($_.InstalledOn) { $_.InstalledOn.ToString('o') } else { $null }
            }
        }
} catch { @() }

# ── SR 7.4 #1：系統還原設定 ──
$restoreConfig = try {
    $sr = Get-CimInstance -Namespace 'root\default' -ClassName SystemRestoreConfig -ErrorAction SilentlyContinue
    $srEnabled = (Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore' -ErrorAction SilentlyContinue).RPSessionInterval
    $vssStorage = vssadmin list shadowstorage 2>$null | Out-String
    @{
        RPSessionInterval = $srEnabled
        VSSStorageInfo    = $vssStorage.Trim()
    }
} catch { @{ Error = $_.Exception.Message } }

@{
    WinREStatus          = $winRE
    SecureBoot           = $secureBoot
    UACSettings          = $uac
    SystemFileIntegrity  = @{
        SFC  = $sfcResult
        DISM = $dismHealth
    }
    WindowsUpdateConfig  = $wuConfig
    RecentUpdates        = @($recentUpdates)
    SystemRestoreConfig  = $restoreConfig
} | ConvertTo-Json -Depth 5
";
}

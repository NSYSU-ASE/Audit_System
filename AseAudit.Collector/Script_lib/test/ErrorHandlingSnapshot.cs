namespace AseAudit.Collector.Script_lib;

/// <summary>
/// [DataManagement] 錯誤處理快照 — 驗證系統的錯誤處理與資訊洩露防護機制。
///
/// 涵蓋驗證項目：
///   SR 3.7 #1 — 控制系統是否以能進行有效裁判的方式識別和處理錯誤狀態（SL 1 未選用）
///   SR 3.7 #2 — 錯誤訊息是否避免洩露可被攻擊者用來攻擊 IACS 的資訊（SL 2～4）
///               收集 Windows 錯誤報告設定、IIS 自訂錯誤頁設定
///   SR 3.7 #3 — 錯誤訊息結構和內容是否經過仔細設計，兼顧判斷用途和安全性
///               收集 Dr. Watson/WER 設定、事件日誌溢位策略
///   SR 3.7 #4 — 事件分析期間是否可容易地存取所有錯誤訊息
///               收集事件日誌大小上限與保留策略
///
///   【區域層級 Zone】
///   #5 — 各區域是否依據安全等級對錯誤處理要求納入區域安全政策
///   #6 — 區域內錯誤處理策略是否已統一定義
///       → 需搭配架構文件與政策審查
///
///   【元件層級 CR 3.7】
///   #7 — 元件是否以不提供攻擊者可利用資訊的方式識別和處理錯誤
///   #8 — 錯誤訊息是否避免透進身份驗證失敗的詳細原因（如不分辨無效使用者/無效密碼）
///   #9 — 元件是否遵循 OWASP 準則處理錯誤訊息
///       → 需個別元件/應用程式測試
///
/// 輸出：JSON 物件
///   - WindowsErrorReporting: WER 設定（是否傳送報告、佇列行為等）
///   - EventLogRetention: 各主要事件日誌的大小上限與保留策略
///   - IISCustomErrors: IIS 自訂錯誤頁設定（若已安裝）
///   - DetailedErrorDisplay: 系統層級的詳細錯誤顯示設定
/// </summary>
public static class ErrorHandlingSnapshot
{
    public const string Content = @"
# ── SR 3.7 #2 #3：Windows 錯誤報告（WER）設定 ──
$wer = try {
    $werReg = 'HKLM:\SOFTWARE\Microsoft\Windows\Windows Error Reporting'
    $werConsent = 'HKLM:\SOFTWARE\Microsoft\Windows\Windows Error Reporting\Consent'
    $werProps = @{}
    if (Test-Path $werReg) {
        $props = Get-ItemProperty -Path $werReg -ErrorAction SilentlyContinue
        $werProps.Disabled = $props.Disabled
        $werProps.DontSendAdditionalData = $props.DontSendAdditionalData
        $werProps.LoggingDisabled = $props.LoggingDisabled
        $werProps.DontShowUI = $props.DontShowUI
    }
    if (Test-Path $werConsent) {
        $consent = Get-ItemProperty -Path $werConsent -ErrorAction SilentlyContinue
        $werProps.DefaultConsent = $consent.DefaultConsent
        $werProps.DefaultOverrideBehavior = $consent.DefaultOverrideBehavior
    }
    $werProps
} catch { @{ Error = $_.Exception.Message } }

# ── SR 3.7 #4：事件日誌大小上限與保留策略 ──
$logNames = @('Application','Security','System','Setup','ForwardedEvents')
$eventLogRetention = foreach ($logName in $logNames) {
    try {
        $log = Get-WinEvent -ListLog $logName -ErrorAction SilentlyContinue
        @{
            LogName        = $logName
            MaxSizeKB      = [math]::Round($log.MaximumSizeInBytes / 1024, 0)
            RetentionDays  = $log.LogRetentionDays
            LogMode        = $log.LogMode.ToString()
            IsEnabled      = $log.IsEnabled
            RecordCount    = $log.RecordCount
            FileSize       = $log.FileSize
            LogFilePath    = $log.LogFilePath
        }
    } catch { @{ LogName = $logName; Error = $_.Exception.Message } }
}

# ── SR 3.7 #2：IIS 自訂錯誤頁設定（避免洩露系統資訊） ──
$iisErrors = try {
    if (Get-Command Get-WebConfigurationProperty -ErrorAction SilentlyContinue) {
        $httpErrors = Get-WebConfigurationProperty -Filter 'system.webServer/httpErrors' `
            -PSPath 'IIS:\' -Name '.' -ErrorAction SilentlyContinue
        @{
            ErrorMode         = $httpErrors.errorMode.ToString()
            ExistingResponse  = $httpErrors.existingResponse.ToString()
            DetailedLocalOnly = ($httpErrors.errorMode -eq 'DetailedLocalOnly')
        }
    } else { @{ IISInstalled = $false } }
} catch { @{ IISInstalled = $false } }

# ── SR 3.7 #2：系統層級詳細錯誤顯示控制 ──
$detailedErrors = @{}
# 登錄檔：是否在登入畫面顯示上次登入使用者名稱
$regPath = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System'
if (Test-Path $regPath) {
    $sysPol = Get-ItemProperty -Path $regPath -ErrorAction SilentlyContinue
    $detailedErrors.DontDisplayLastUserName = $sysPol.DontDisplayLastUserName
    $detailedErrors.DisableCAD = $sysPol.DisableCAD
    $detailedErrors.LegalNoticeCaption = $sysPol.LegalNoticeCaption
    $detailedErrors.LegalNoticeText = $sysPol.LegalNoticeText
}
# 登入失敗時是否顯示詳細原因
$regPath2 = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System'
if (Test-Path $regPath2) {
    $props2 = Get-ItemProperty -Path $regPath2 -ErrorAction SilentlyContinue
    $detailedErrors.VerboseStatus = $props2.VerboseStatus
}

@{
    WindowsErrorReporting = $wer
    EventLogRetention     = @($eventLogRetention)
    IISCustomErrors       = $iisErrors
    DetailedErrorDisplay  = $detailedErrors
} | ConvertTo-Json -Depth 4
";
}

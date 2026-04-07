namespace AseAudit.Collector.Script_lib;

/// <summary>
/// [DataManagement] 確定性輸出快照 — 驗證系統失效時的預設安全狀態設定。
///
/// 涵蓋驗證項目：
///   SR 3.6 #1 — 當攻擊導致無法維持正常操作時，控制系統是否能將輸出設置為預定狀態
///               收集服務故障復原設定（失敗後自動重啟、停止等）
///   SR 3.6 #2 — 預定狀態的選擇是否已依據應用程式設計明確定義（無動作/保持/已修復）
///               收集關鍵服務的復原動作設定
///   SR 3.6 #3 — 預定狀態是否可由使用者設定
///               收集 Windows 啟動與復原設定
///
///   【區域層級 Zone】
///   #4 — 各區域是否依據安全等級對確定性輸出要求納入區域安全政策
///   #5 — 區域內各控制迴路是否已定義失敗安全狀態
///       → 需搭配架構文件與政策審查
///
///   【元件層級 CR 3.6】
///   #6 — 實體或邏輯連接到自動化過程的元件是否能將輸出設置為預定狀態
///   #7 — 元件是否支援多種預定狀態選項（無動作、保持、已修復、關閉）
///   #8 — 當元件無法維持依據區域定義的正常操作時，是否自動觸發輸出至預定狀態
///       → 需個別元件測試，此腳本收集系統層級可驗證的設定
///
/// 輸出：JSON 物件
///   - ServiceRecoveryOptions: 關鍵服務的故障復原設定
///   - StartupRecovery: Windows 啟動與復原設定
///   - CrashControl: 系統當機控制設定（藍屏後行為）
///   - BootConfiguration: 開機設定（安全開機、偵錯模式等）
/// </summary>
public static class DeterministicOutputSnapshot
{
    public const string Content = @"
# ── SR 3.6 #1 #2：關鍵服務故障復原設定 ──
$criticalServices = @(
    'WinDefend','EventLog','W32Time','wuauserv','MSSQLSERVER',
    'MpsSvc','BFE','Dnscache','LanmanServer','LanmanWorkstation',
    'Spooler','Schedule','TermService','WinRM','CryptSvc'
)

$serviceRecovery = foreach ($svcName in $criticalServices) {
    $svc = Get-Service -Name $svcName -ErrorAction SilentlyContinue
    if ($svc) {
        $recovery = sc.exe qfailure $svcName 2>$null | Out-String
        @{
            ServiceName  = $svcName
            DisplayName  = $svc.DisplayName
            Status       = $svc.Status.ToString()
            StartType    = $svc.StartType.ToString()
            RecoveryInfo = $recovery.Trim()
        }
    }
}

# ── SR 3.6 #3：Windows 啟動與復原設定 ──
$startupRecovery = try {
    $os = Get-CimInstance Win32_OSRecoveryConfiguration -ErrorAction SilentlyContinue
    @{
        AutoReboot           = $os.AutoReboot
        DebugInfoType        = $os.DebugInfoType
        WriteToSystemLog     = $os.WriteToSystemLog
        OverwriteExistingDebugFile = $os.OverwriteExistingDebugFile
        DebugFilePath        = $os.DebugFilePath
    }
} catch { @{ Error = $_.Exception.Message } }

# ── SR 3.6 #1：系統當機控制設定（CrashControl 登錄） ──
$crashControl = try {
    $regPath = 'HKLM:\SYSTEM\CurrentControlSet\Control\CrashControl'
    if (Test-Path $regPath) {
        $props = Get-ItemProperty -Path $regPath -ErrorAction SilentlyContinue
        @{
            AutoReboot          = $props.AutoReboot
            CrashDumpEnabled    = $props.CrashDumpEnabled
            LogEvent            = $props.LogEvent
            DumpFile            = $props.DumpFile
            Overwrite           = $props.Overwrite
            AlwaysKeepMemoryDump = $props.AlwaysKeepMemoryDump
        }
    } else { @{ RegistryExists = $false } }
} catch { @{ Error = $_.Exception.Message } }

# ── SR 3.6 #3：開機設定（BCD） ──
$bootConfig = try {
    $bcd = bcdedit /enum '{current}' 2>$null | Out-String
    @{
        BcdOutput = $bcd.Trim()
    }
} catch { @{ Error = $_.Exception.Message } }

@{
    ServiceRecoveryOptions = @($serviceRecovery)
    StartupRecovery        = $startupRecovery
    CrashControl           = $crashControl
    BootConfiguration      = $bootConfig
} | ConvertTo-Json -Depth 4
";
}

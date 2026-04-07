namespace AseAudit.Collector.Script_lib;

/// <summary>
/// [DataManagement] 輸入驗證快照 — 驗證系統層級的輸入驗證機制。
///
/// 涵蓋驗證項目：
///   SR 3.5 #1 — 控制系統是否驗證所有用作工業程序控制輸入的語法和內容
///   SR 3.5 #2 — 是否制定檢查輸入所有語法（如設定點）的規則
///               收集 IIS 請求過濾設定、ASP.NET 驗證設定
///   SR 3.5 #4 — 是否驗證已定義欄位類型的超出範圍值、無效字元、資料遺失/不完整和緩衝區溢位
///               收集 DEP（資料執行防止）、ASLR、CFG 等記憶體保護設定
///   SR 3.5 #5 — 是否防範 SQL 注入、跨站腳本（XSS）和格式錯誤等資料入侵
///               收集 Windows Defender Exploit Guard 設定
///
///   【無法完全程式化驗證項目】
///   SR 3.5 #3 — 傳送給程式碼解釋器的輸入是否經過篩選，防止被誤解為命令
///               → 需應用程式層級程式碼審查
///
///   【區域層級 Zone】
///   #6 — 各區域是否依據目標安全等級對輸入驗證要求納入區域安全政策
///   #7 — 區域邊界的輸入資料是否進行驗證
///       → 需搭配架構文件與政策審查
///
///   【元件層級 CR 3.5】
///   #8 — 元件是否驗證任何輸入資料的語法、長度和內容
///   #9 — 元件是否針對外部介面輸入進行有效性檢查
///   #10 — 元件是否遵循 OWASP 等公認準則進行輸入驗證
///       → 需個別元件/應用程式測試
///
/// 輸出：JSON 物件
///   - DEP: 資料執行防止設定
///   - ASLR: 位址空間隨機化設定
///   - ExploitProtection: Windows Defender Exploit Guard 系統層級設定
///   - IISRequestFiltering: IIS 請求過濾設定（若已安裝）
///   - ASPNetValidation: ASP.NET 請求驗證設定
/// </summary>
public static class InputValidationSnapshot
{
    public const string Content = @"
# ── SR 3.5 #4：資料執行防止（DEP）設定 ──
$dep = try {
    $bcdDep = bcdedit /enum '{current}' 2>$null | Out-String
    $wmi = Get-CimInstance Win32_OperatingSystem -ErrorAction SilentlyContinue |
        Select-Object DataExecutionPrevention_Available,
                      DataExecutionPrevention_32BitApplications,
                      DataExecutionPrevention_Drivers,
                      DataExecutionPrevention_SupportPolicy
    @{
        SupportPolicy     = $wmi.DataExecutionPrevention_SupportPolicy
        Available         = $wmi.DataExecutionPrevention_Available
        For32BitApps      = $wmi.DataExecutionPrevention_32BitApplications
        ForDrivers        = $wmi.DataExecutionPrevention_Drivers
        BcdOutput         = $bcdDep
    }
} catch { @{ Error = $_.Exception.Message } }

# ── SR 3.5 #4：ASLR 與 CFG（Control Flow Guard）系統設定 ──
$exploitProt = try {
    $sysMit = Get-ProcessMitigation -System -ErrorAction SilentlyContinue
    @{
        DEP_Enable               = $sysMit.DEP.Enable
        DEP_EmulateAtlThunks     = $sysMit.DEP.EmulateAtlThunks
        ASLR_ForceRelocateImages = $sysMit.ASLR.ForceRelocateImages
        ASLR_RequireInfo         = $sysMit.ASLR.RequireInfo
        ASLR_BottomUp            = $sysMit.ASLR.BottomUp
        ASLR_HighEntropy         = $sysMit.ASLR.HighEntropy
        CFG_Enable               = $sysMit.CFG.Enable
        CFG_SuppressExports      = $sysMit.CFG.SuppressExports
        SEHOP_Enable             = $sysMit.SEHOP.Enable
        Heap_TerminateOnError    = $sysMit.Heap.TerminateOnError
    }
} catch { @{ Error = $_.Exception.Message } }

# ── SR 3.5 #5：Windows Defender Exploit Guard — ASR 規則 ──
$asrRules = try {
    $prefs = Get-MpPreference -ErrorAction SilentlyContinue
    $ruleIds = $prefs.AttackSurfaceReductionRules_Ids
    $ruleActions = $prefs.AttackSurfaceReductionRules_Actions
    if ($ruleIds) {
        for ($i = 0; $i -lt $ruleIds.Count; $i++) {
            @{
                RuleId = $ruleIds[$i]
                Action = if ($i -lt $ruleActions.Count) { $ruleActions[$i] } else { 'N/A' }
            }
        }
    } else { @() }
} catch { @() }

# ── SR 3.5 #2 #5：IIS 請求過濾設定（若已安裝） ──
$iisFiltering = try {
    if (Get-Command Get-WebConfigurationProperty -ErrorAction SilentlyContinue) {
        $reqFilter = Get-WebConfigurationProperty -Filter 'system.webServer/security/requestFiltering' `
            -PSPath 'IIS:\' -Name '.' -ErrorAction SilentlyContinue
        $reqLimits = Get-WebConfigurationProperty -Filter 'system.webServer/security/requestFiltering/requestLimits' `
            -PSPath 'IIS:\' -Name '.' -ErrorAction SilentlyContinue
        @{
            AllowDoubleEscaping = $reqFilter.allowDoubleEscaping
            AllowHighBitCharacters = $reqFilter.allowHighBitCharacters
            MaxAllowedContentLength = $reqLimits.maxAllowedContentLength
            MaxUrl = $reqLimits.maxUrl
            MaxQueryString = $reqLimits.maxQueryString
        }
    } else { @{ Installed = $false } }
} catch { @{ Installed = $false } }

# ── SR 3.5 #5：ASP.NET 請求驗證設定 ──
$aspnetValidation = try {
    $machineConfig = [System.Runtime.InteropServices.RuntimeEnvironment]::GetRuntimeDirectory() + 'Config\machine.config'
    if (Test-Path $machineConfig) {
        [xml]$config = Get-Content $machineConfig -ErrorAction SilentlyContinue
        $httpRuntime = $config.configuration.'system.web'.httpRuntime
        @{
            RequestValidationMode  = $httpRuntime.requestValidationMode
            MaxRequestLength       = $httpRuntime.maxRequestLength
            EnableHeaderChecking   = $httpRuntime.enableHeaderChecking
            MachineConfigPath      = $machineConfig
        }
    } else { @{ Available = $false } }
} catch { @{ Available = $false } }

@{
    DEP                 = $dep
    ExploitProtection   = $exploitProt
    ASRRules            = @($asrRules)
    IISRequestFiltering = $iisFiltering
    ASPNetValidation    = $aspnetValidation
} | ConvertTo-Json -Depth 5
";
}

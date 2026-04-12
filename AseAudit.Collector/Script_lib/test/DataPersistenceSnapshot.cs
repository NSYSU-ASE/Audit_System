namespace AseAudit.Collector.Script_lib;

/// <summary>
/// [DataManagement] 資訊持久性快照 — 驗證從退役元件中清除資訊的能力。
///
/// 涵蓋驗證項目：
///   SR 4.2 #1 — 控制系統是否能清除從運作中或從退役元件中所有支持或確認存取權限的資訊
///               （SL 1 未選用）
///   SR 4.2 #2 — 資訊（如連接密鑰、加密資訊）在元件退役時是否被妥善清除（SL 2）
///               收集分頁檔清除設定、暫存檔案狀態
///   SR 4.2 #3 — 使用者動作產生的資訊是否避免以不可控方式向不同使用者或角色公開
///               收集使用者設定檔隔離與暫存目錄權限
///   SR 4.2 RE(1) #4 — 是否防止透過喪失控制的共享記憶體進行未授權和非蓄意的資訊傳輸（SL 3～4）
///   SR 4.2 RE(1) #5 — 記憶體釋放時是否清除所有可辨識資料和相關連結
///               收集記憶體轉儲設定、分頁檔設定
///
///   【區域層級 Zone】
///   #6 — 各區域是否依據安全等級對資訊持久性控制納入區域安全政策
///   #7 — 區域內元件退役/更替的資訊清除程序是否已建立
///       → 需搭配架構文件與政策審查
///
///   【元件層級 CR 4.2】
///   #8 — 元件是否能抹除所有支持或顯式變更授權的資訊（含喪失控制的儲存中的認證資訊、網路設定）
///         （SL 1 未選用）
///   #9 — 元件退役或移除時，是否能防止敏感資訊（加密密鑰等）被無條件釋放（SL 2）
///   CR 4.2 RE(1) #10 — 元件是否能防止透過喪失控制的共享記憶體進行未授權資訊傳輸（SL 3～4）
///   CR 4.2 RE(2) #11 — 元件是否能驗證資訊抹除是否成功
///       → 需個別元件測試
///
/// 輸出：JSON 物件
///   - PagefileClearing: 分頁檔關機時清除設定
///   - MemoryDumpConfig: 記憶體轉儲設定
///   - TempFileStatus: 系統暫存目錄大小與檔案數量
///   - UserProfileIsolation: 使用者設定檔目錄權限
///   - CredentialGuard: Credential Guard / LSA 保護設定
///   - HibernationConfig: 休眠檔設定（可能殘留記憶體資料）
/// </summary>
public static class DataPersistenceSnapshot
{
    public const string Content = @"
# ── SR 4.2 #2 RE(1) #5：分頁檔關機清除設定 ──
$pagefileClearing = try {
    $regPath = 'HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management'
    if (Test-Path $regPath) {
        $props = Get-ItemProperty -Path $regPath -ErrorAction SilentlyContinue
        @{
            ClearPageFileAtShutdown = $props.ClearPageFileAtShutdown
            PagingFiles             = $props.PagingFiles
            ExistingPageFiles       = $props.ExistingPageFiles
        }
    } else { @{ RegistryExists = $false } }
} catch { @{ Error = $_.Exception.Message } }

# ── SR 4.2 RE(1) #4 #5：記憶體轉儲設定 ──
$memDump = try {
    $regPath = 'HKLM:\SYSTEM\CurrentControlSet\Control\CrashControl'
    if (Test-Path $regPath) {
        $props = Get-ItemProperty -Path $regPath -ErrorAction SilentlyContinue
        @{
            CrashDumpEnabled     = $props.CrashDumpEnabled
            DumpFile             = $props.DumpFile
            Overwrite            = $props.Overwrite
            AutoReboot           = $props.AutoReboot
            AlwaysKeepMemoryDump = $props.AlwaysKeepMemoryDump
        }
    } else { @{ RegistryExists = $false } }
} catch { @{ Error = $_.Exception.Message } }

# ── SR 4.2 #2：系統暫存目錄狀態 ──
$tempDirs = @($env:TEMP, ""$env:SystemRoot\Temp"")
$tempStatus = foreach ($dir in $tempDirs) {
    if (Test-Path $dir) {
        $files = Get-ChildItem -Path $dir -Recurse -Force -ErrorAction SilentlyContinue
        $totalSize = ($files | Measure-Object -Property Length -Sum -ErrorAction SilentlyContinue).Sum
        @{
            Path       = $dir
            FileCount  = $files.Count
            TotalSizeMB = [math]::Round($totalSize / 1MB, 2)
            ACL        = @((Get-Acl -Path $dir -ErrorAction SilentlyContinue).Access | Select-Object -First 5 | ForEach-Object {
                @{
                    Identity   = $_.IdentityReference.Value
                    Rights     = $_.FileSystemRights.ToString()
                    AccessType = $_.AccessControlType.ToString()
                }
            })
        }
    }
}

# ── SR 4.2 #3：使用者設定檔隔離（確認各使用者目錄權限獨立） ──
$profileIsolation = try {
    $profileDir = (Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList' -ErrorAction SilentlyContinue).ProfilesDirectory
    if (-not $profileDir) { $profileDir = 'C:\Users' }
    $profiles = Get-ChildItem -Path $profileDir -Directory -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -notin @('Public','Default','Default User','All Users') } |
        Select-Object -First 10
    foreach ($p in $profiles) {
        $acl = Get-Acl -Path $p.FullName -ErrorAction SilentlyContinue
        @{
            ProfilePath = $p.FullName
            Owner       = $acl.Owner
            AccessCount = $acl.Access.Count
            Access      = @($acl.Access | Select-Object -First 5 | ForEach-Object {
                @{
                    Identity   = $_.IdentityReference.Value
                    Rights     = $_.FileSystemRights.ToString()
                    AccessType = $_.AccessControlType.ToString()
                    Inherited  = $_.IsInherited
                }
            })
        }
    }
} catch { @() }

# ── SR 4.2 RE(1) #4：Credential Guard / LSA 保護設定 ──
$credGuard = try {
    $regPath = 'HKLM:\SYSTEM\CurrentControlSet\Control\LSA'
    $dgPath = 'HKLM:\SYSTEM\CurrentControlSet\Control\DeviceGuard'
    $lsa = if (Test-Path $regPath) {
        $props = Get-ItemProperty -Path $regPath -ErrorAction SilentlyContinue
        @{
            RunAsPPL              = $props.RunAsPPL
            LsaCfgFlags           = $props.LsaCfgFlags
            DisableRestrictedAdmin = $props.DisableRestrictedAdmin
        }
    } else { @{} }
    $dg = if (Test-Path $dgPath) {
        $props = Get-ItemProperty -Path $dgPath -ErrorAction SilentlyContinue
        @{
            EnableVirtualizationBasedSecurity = $props.EnableVirtualizationBasedSecurity
            RequirePlatformSecurityFeatures    = $props.RequirePlatformSecurityFeatures
            LsaCfgFlags                        = $props.LsaCfgFlags
        }
    } else { @{} }
    @{ LSA = $lsa; DeviceGuard = $dg }
} catch { @{ Error = $_.Exception.Message } }

# ── SR 4.2 RE(1) #5：休眠檔設定（可能殘留記憶體資料） ──
$hibernation = try {
    $hibFile = ""$env:SystemDrive\hiberfil.sys""
    $hibEnabled = Test-Path $hibFile
    $powerCfg = powercfg /a 2>$null | Out-String
    @{
        HibernationFileExists = $hibEnabled
        PowerAvailability     = $powerCfg.Trim()
    }
} catch { @{ Error = $_.Exception.Message } }

@{
    PagefileClearing     = $pagefileClearing
    MemoryDumpConfig     = $memDump
    TempFileStatus       = @($tempStatus)
    UserProfileIsolation = @($profileIsolation)
    CredentialGuard      = $credGuard
    HibernationConfig    = $hibernation
} | ConvertTo-Json -Depth 5
";
}

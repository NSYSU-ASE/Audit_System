namespace AseAudit.Collector.Script_lib;

/// <summary>
/// [DataManagement] 應用程式分割快照 — 驗證基於關鍵性的應用程式/服務隔離。
///
/// 涵蓋驗證項目：
///   SR 5.4 #1 — 控制系統是否支援基於關鍵性的分割資料、應用程式/服務（SL 1～4）
///               收集系統上執行的程序所屬使用者帳號隔離情況
///   SR 5.4 #2 — 是否透過實體或邏輯手段（不同電腦、不同 CPU、不同 OS 實例、不同網路位址等）完成分割
///               收集虛擬化/容器環境偵測、多網卡設定、Hyper-V 角色
///   SR 5.4 #3 — 緊急/安全系統、迴路控制、操作員工作站、工程工作站是否已適當分割
///               收集服務帳號隔離與 Windows 功能角色安裝狀態
///
///   【區域層級 Zone】
///   #4 — 各區域是否依據安全等級和關鍵性進行適當的應用程式分割
///   #5 — 區域安全政策是否已定義分割模型和分割邊界
///       → 需搭配架構文件與政策審查
///
///   【元件層級 CR 5.4】
///   沒有與 SR 5.4 相關的元件層級要求
///
/// 輸出：JSON 物件
///   - ProcessIsolation: 各服務/程序的執行帳號與完整性等級
///   - VirtualizationStatus: 虛擬化/容器環境偵測
///   - InstalledRoles: Windows Server 角色與功能安裝狀態
///   - NetworkAdapters: 多網卡設定（實體分割指標）
///   - AppLockerPolicy: AppLocker 應用程式控制政策（若已設定）
/// </summary>
public static class ApplicationPartitionSnapshot
{
    public const string Content = @"
# ── SR 5.4 #1：各程序的執行帳號與完整性等級隔離 ──
$processIsolation = try {
    $procs = Get-Process -IncludeUserName -ErrorAction SilentlyContinue |
        Where-Object { $_.UserName } |
        Group-Object UserName |
        ForEach-Object {
            @{
                RunAsUser    = $_.Name
                ProcessCount = $_.Count
                Processes    = @($_.Group | Select-Object -First 10 | ForEach-Object {
                    @{ Name = $_.ProcessName; Id = $_.Id; WorkingSetMB = [math]::Round($_.WorkingSet64 / 1MB, 1) }
                })
            }
        }
    @($procs)
} catch { @() }

# ── SR 5.4 #2：虛擬化/容器環境偵測 ──
$virtStatus = try {
    $cs = Get-CimInstance Win32_ComputerSystem -ErrorAction SilentlyContinue
    $bios = Get-CimInstance Win32_BIOS -ErrorAction SilentlyContinue
    $hyperv = Get-WindowsOptionalFeature -Online -FeatureName Microsoft-Hyper-V -ErrorAction SilentlyContinue
    @{
        Manufacturer         = $cs.Manufacturer
        Model                = $cs.Model
        IsVirtualMachine     = $cs.Model -match 'Virtual|VMware|VirtualBox|KVM|Xen|HyperV'
        HypervisorPresent    = $cs.HypervisorPresent
        BIOSManufacturer     = $bios.Manufacturer
        HyperVEnabled        = if ($hyperv) { $hyperv.State.ToString() } else { 'NotAvailable' }
        IsContainer          = (Test-Path '\.dockerenv') -or ($env:DOTNET_RUNNING_IN_CONTAINER -eq 'true')
    }
} catch { @{ Error = $_.Exception.Message } }

# ── SR 5.4 #2 #3：Windows Server 角色與功能 ──
$installedRoles = try {
    if (Get-Command Get-WindowsFeature -ErrorAction SilentlyContinue) {
        Get-WindowsFeature | Where-Object { $_.Installed } |
            Select-Object -First 30 |
            ForEach-Object {
                @{
                    Name        = $_.Name
                    DisplayName = $_.DisplayName
                    FeatureType = $_.FeatureType
                }
            }
    } else {
        # 非 Server 版本：列出已安裝的 Windows 選用功能
        Get-WindowsOptionalFeature -Online -ErrorAction SilentlyContinue |
            Where-Object { $_.State -eq 'Enabled' } |
            Select-Object -First 30 |
            ForEach-Object {
                @{
                    Name  = $_.FeatureName
                    State = $_.State.ToString()
                }
            }
    }
} catch { @() }

# ── SR 5.4 #2：多網卡設定（實體網路分割指標） ──
$networkAdapters = Get-NetAdapter -ErrorAction SilentlyContinue |
    Where-Object { $_.Status -eq 'Up' } |
    ForEach-Object {
        $ipConfig = Get-NetIPAddress -InterfaceIndex $_.ifIndex -ErrorAction SilentlyContinue
        @{
            Name           = $_.Name
            InterfaceDesc  = $_.InterfaceDescription
            MacAddress     = $_.MacAddress
            LinkSpeed      = $_.LinkSpeed
            MediaType      = $_.MediaType
            IPAddresses    = @($ipConfig | ForEach-Object {
                @{ Address = $_.IPAddress; PrefixLength = $_.PrefixLength; AddressFamily = $_.AddressFamily.ToString() }
            })
        }
    }

# ── SR 5.4 #3：AppLocker 政策（應用程式分割控制） ──
$appLocker = try {
    $regPath = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\SrpV2'
    if (Test-Path $regPath) {
        $collections = Get-ChildItem -Path $regPath -ErrorAction SilentlyContinue
        $rules = foreach ($col in $collections) {
            $colName = $col.PSChildName
            $ruleCount = (Get-ChildItem -Path $col.PSPath -ErrorAction SilentlyContinue).Count
            @{ Collection = $colName; RuleCount = $ruleCount }
        }
        @{ Configured = $true; Collections = @($rules) }
    } else { @{ Configured = $false } }
} catch { @{ Configured = $false } }

@{
    ProcessIsolation     = @($processIsolation)
    VirtualizationStatus = $virtStatus
    InstalledRoles       = @($installedRoles)
    NetworkAdapters      = @($networkAdapters)
    AppLockerPolicy      = $appLocker
} | ConvertTo-Json -Depth 5
";
}

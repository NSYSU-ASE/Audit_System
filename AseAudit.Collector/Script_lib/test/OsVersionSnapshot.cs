namespace AseAudit.Collector.Script_lib;

/// <summary>
/// [Identity] 識別 Windows 作業系統版本、版本號、架構與安裝資訊。
/// 輸出：JSON 對應 OsVersionSnapshotDto
/// </summary>
public static class OsVersionSnapshot
{
    public const string Content = @"
$os  = Get-CimInstance Win32_OperatingSystem
$reg = Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion'

# 解析 UBR（Update Build Revision），若不存在則為 0
$ubr = if ($reg.PSObject.Properties['UBR']) { $reg.UBR } else { 0 }

# 完整版本字串，例如 10.0.19045.5608
$fullVersion = '{0}.{1}' -f $os.Version, $ubr

# 功能更新標籤，例如 22H2 / 23H2；舊版 Windows 以 ReleaseId 回退
$releaseLabel = if ($reg.PSObject.Properties['DisplayVersion']) {
    $reg.DisplayVersion
} elseif ($reg.PSObject.Properties['ReleaseId']) {
    $reg.ReleaseId
} else {
    ''
}

# 判斷產品類型：1=Workstation，2=Domain Controller，3=Server
$productTypeMap = @{ 1 = 'Workstation'; 2 = 'DomainController'; 3 = 'Server' }
$productType    = $productTypeMap[[int]$os.ProductType]

$result = @{
    HostId           = $env:COMPUTERNAME

    # 作業系統基本資訊
    Caption          = $os.Caption                    # 例：Microsoft Windows 11 Pro
    ProductName      = $reg.ProductName               # 登錄機碼中的產品名稱
    EditionId        = $reg.EditionID                 # 例：Professional / Enterprise
    ReleaseLabel     = $releaseLabel                  # 例：22H2
    InstallationType = $reg.InstallationType          # Client / Server / Server Core

    # 版本號
    Version          = $os.Version                    # 例：10.0.19045
    BuildNumber      = $os.BuildNumber                # 例：19045
    UpdateBuildRevision = $ubr                        # 例：5608
    FullVersion      = $fullVersion                   # 例：10.0.19045.5608

    # 硬體架構
    Architecture     = $os.OSArchitecture             # 例：64-bit

    # 產品類型與授權
    ProductType      = $productType                   # Workstation / DomainController / Server
    SerialNumber     = $os.SerialNumber

    # 時間資訊
    InstallDate      = if ($os.InstallDate) { $os.InstallDate.ToString('o') } else { $null }
    LastBootUpTime   = if ($os.LastBootUpTime) { $os.LastBootUpTime.ToString('o') } else { $null }

    # 系統磁碟空間（MB）
    TotalVisibleMemoryMB = [math]::Round($os.TotalVisibleMemorySize / 1KB, 0)
    FreePhysicalMemoryMB = [math]::Round($os.FreePhysicalMemory     / 1KB, 0)
}

$result | ConvertTo-Json -Depth 3
";
}

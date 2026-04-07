namespace AseAudit.Collector.Script_lib;

/// <summary>
/// [Firewall] 網路分段快照 — 驗證 SR 5.1 Network Segmentation。
///
/// 涵蓋驗證項目：
///   SR 5.1      — 控制系統網路是否已從非控制系統網路邏輯分段，關鍵網路是否已隔離
///   SR 5.1 RE(1) — 控制系統網路是否已與非控制系統網路實體隔離
///   SR 5.1 RE(2) — 控制系統是否能獨立提供網路服務（DHCP、DNS）而無需連接非控制系統網路
///   SR 5.1 RE(3) — 關鍵控制系統網路是否已透過邏輯或實體方式與非關鍵網路隔離
///
/// 註釋：
///   RE(1) 實體網路隔離需透過現場網路拓撲圖與實體線路稽核確認，
///   本腳本僅收集 OS 可觀測的邏輯分段資訊作為佐證。
///   RE(2) 獨立網路服務需確認 DHCP/DNS 伺服器部署位置，
///   本腳本收集 DNS 設定供比對，但完整驗證需網路架構文件。
///
/// 輸出：JSON 物件
///   - NetworkAdapters:     各網路介面的 IP、子網路、VLAN、網路設定檔類型
///   - RoutingTable:        路由表（顯示網段劃分與閘道設定）
///   - IpForwardingStatus:  IP 轉發狀態（判斷是否作為路由器使用）
///   - DnsConfiguration:    各介面 DNS 伺服器設定
///   - NetworkProfiles:     各連線的網路設定檔（Domain/Private/Public）
///   - VlanInfo:            VLAN 標記資訊（若有設定）
/// </summary>
public static class NetworkSegmentationSnapshot
{
    public const string Content = @"
# ── SR 5.1：收集網路介面與子網路配置 ──
$adapters = Get-NetAdapter -ErrorAction SilentlyContinue | Where-Object { $_.Status -eq 'Up' } |
    ForEach-Object {
        $adapter = $_
        $ipConfig = Get-NetIPAddress -InterfaceIndex $adapter.ifIndex -ErrorAction SilentlyContinue
        $ipv4 = $ipConfig | Where-Object { $_.AddressFamily -eq 'IPv4' -and $_.IPAddress -ne '127.0.0.1' }
        $ipv6 = $ipConfig | Where-Object { $_.AddressFamily -eq 'IPv6' -and $_.IPAddress -notlike 'fe80*' }
        @{
            InterfaceAlias  = $adapter.InterfaceAlias
            InterfaceIndex  = $adapter.ifIndex
            MacAddress      = $adapter.MacAddress
            LinkSpeed       = $adapter.LinkSpeed
            MediaType       = $adapter.MediaType
            IPv4Addresses   = @($ipv4 | ForEach-Object { @{ Address = $_.IPAddress; PrefixLength = $_.PrefixLength } })
            IPv6Addresses   = @($ipv6 | ForEach-Object { @{ Address = $_.IPAddress; PrefixLength = $_.PrefixLength } })
            VlanId          = (Get-NetAdapterAdvancedProperty -Name $adapter.Name -RegistryKeyword 'VlanID' -ErrorAction SilentlyContinue).RegistryValue
        }
    }

# ── SR 5.1：路由表（驗證網段隔離與閘道配置） ──
$routes = Get-NetRoute -ErrorAction SilentlyContinue |
    Where-Object { $_.DestinationPrefix -ne '0.0.0.0/0' -and $_.DestinationPrefix -notlike 'ff*' } |
    Select-Object DestinationPrefix, NextHop, RouteMetric, InterfaceAlias, AddressFamily |
    ForEach-Object {
        @{
            DestinationPrefix = $_.DestinationPrefix
            NextHop           = $_.NextHop
            RouteMetric       = $_.RouteMetric
            InterfaceAlias    = $_.InterfaceAlias
            AddressFamily     = $_.AddressFamily.ToString()
        }
    }

# ── SR 5.1 RE(3)：IP 轉發狀態（判斷主機是否充當路由器） ──
$ipForwarding = @{
    IPv4Forwarding = (Get-NetIPInterface -AddressFamily IPv4 -ErrorAction SilentlyContinue |
        Where-Object { $_.Forwarding -eq 'Enabled' } |
        Select-Object InterfaceAlias, Forwarding)
    IPv6Forwarding = (Get-NetIPInterface -AddressFamily IPv6 -ErrorAction SilentlyContinue |
        Where-Object { $_.Forwarding -eq 'Enabled' } |
        Select-Object InterfaceAlias, Forwarding)
    RegistryForwarding = (Get-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters' -Name 'IPEnableRouter' -ErrorAction SilentlyContinue).IPEnableRouter
}

# ── SR 5.1 RE(2)：DNS 配置（驗證是否使用獨立 DNS 伺服器） ──
$dnsConfig = Get-DnsClientServerAddress -ErrorAction SilentlyContinue |
    Where-Object { $_.ServerAddresses.Count -gt 0 } |
    ForEach-Object {
        @{
            InterfaceAlias  = $_.InterfaceAlias
            AddressFamily   = $_.AddressFamily
            ServerAddresses = $_.ServerAddresses
        }
    }

# ── SR 5.1：網路連線設定檔（Domain/Private/Public 分類） ──
$profiles = Get-NetConnectionProfile -ErrorAction SilentlyContinue |
    ForEach-Object {
        @{
            Name               = $_.Name
            InterfaceAlias     = $_.InterfaceAlias
            NetworkCategory    = $_.NetworkCategory.ToString()
            IPv4Connectivity   = $_.IPv4Connectivity.ToString()
            IPv6Connectivity   = $_.IPv6Connectivity.ToString()
        }
    }

# ── SR 5.1：VLAN 設定彙總 ──
$vlanInfo = Get-NetAdapterAdvancedProperty -RegistryKeyword 'VlanID' -ErrorAction SilentlyContinue |
    Where-Object { $_.RegistryValue -and $_.RegistryValue[0] -ne '0' } |
    ForEach-Object {
        @{
            InterfaceName = $_.Name
            VlanId        = $_.RegistryValue
        }
    }

@{
    NetworkAdapters    = @($adapters)
    RoutingTable       = @($routes)
    IpForwardingStatus = $ipForwarding
    DnsConfiguration   = @($dnsConfig)
    NetworkProfiles    = @($profiles)
    VlanInfo           = @($vlanInfo)
} | ConvertTo-Json -Depth 5
";
}

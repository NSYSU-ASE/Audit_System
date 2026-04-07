namespace AseAudit.Collector.Script_lib;

/// <summary>
/// [Firewall] 阻斷服務攻擊防護快照 — 驗證 SR 7.1 Denial of Service Protection。
///
/// 涵蓋驗證項目：
///   SR 7.1      — 控制系統是否具備在 DoS 攻擊事件期間仍以降級模式運行的能力，
///                 DoS 事件是否不會對安全相關系統產生不利影響
///   SR 7.1 RE(1) — 是否具備管理通訊負載的能力（如速率限制），
///                  減輕資料流氾濫類型 DoS 事件影響
///   SR 7.1 RE(2) — 是否能限制惡意使用者引起影響其他控制系統或網路的 DoS 事件
///
/// 註釋：
///   RE(2) 完整驗證需搭配網路架構與 QoS 政策文件。
///   本腳本收集 OS 層可觀測的 TCP/IP 堆疊強化設定、連線限制與網路配額資訊。
///   實際的速率限制可能由上層防火牆/IPS 設備實施，需另行確認。
///
/// 輸出：JSON 物件
///   - TcpIpHardening:      TCP/IP 堆疊強化設定（SYN 攻擊防護等）
///   - ConnectionLimits:    TCP 連線數限制與目前狀態
///   - NetworkAdapterPower: 網路介面卡進階電源與效能設定
///   - WindowsServiceRecovery: 關鍵服務復原設定（降級模式佐證）
///   - ResourceQuotas:      系統資源配額設定
/// </summary>
public static class DosProtectionSnapshot
{
    public const string Content = @"
# ── SR 7.1 RE(1)：TCP/IP 堆疊強化設定（防範 SYN Flood 等攻擊） ──
$tcpParams = 'HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters'
$tcpHardening = @{
    # SYN 攻擊保護（0=無, 1=降低重試, 2=額外延遲 + 降低重試）
    SynAttackProtect     = (Get-ItemProperty -Path $tcpParams -Name 'SynAttackProtect' -ErrorAction SilentlyContinue).SynAttackProtect
    # 半開連線佇列上限
    TcpMaxHalfOpen       = (Get-ItemProperty -Path $tcpParams -Name 'TcpMaxHalfOpen' -ErrorAction SilentlyContinue).TcpMaxHalfOpen
    TcpMaxHalfOpenRetried = (Get-ItemProperty -Path $tcpParams -Name 'TcpMaxHalfOpenRetried' -ErrorAction SilentlyContinue).TcpMaxHalfOpenRetried
    # TCP Keep-Alive 設定（偵測失效連線）
    KeepAliveTime        = (Get-ItemProperty -Path $tcpParams -Name 'KeepAliveTime' -ErrorAction SilentlyContinue).KeepAliveTime
    KeepAliveInterval    = (Get-ItemProperty -Path $tcpParams -Name 'KeepAliveInterval' -ErrorAction SilentlyContinue).KeepAliveInterval
    # TCP 連線最大重試次數
    TcpMaxConnectRetransmissions = (Get-ItemProperty -Path $tcpParams -Name 'TcpMaxConnectRetransmissions' -ErrorAction SilentlyContinue).TcpMaxConnectRetransmissions
    TcpMaxDataRetransmissions    = (Get-ItemProperty -Path $tcpParams -Name 'TcpMaxDataRetransmissions' -ErrorAction SilentlyContinue).TcpMaxDataRetransmissions
    # 啟用 Dead Gateway 偵測
    EnableDeadGWDetect   = (Get-ItemProperty -Path $tcpParams -Name 'EnableDeadGWDetect' -ErrorAction SilentlyContinue).EnableDeadGWDetect
    # 停用 IP 來源路由（防止路由操縱）
    DisableIPSourceRouting = (Get-ItemProperty -Path $tcpParams -Name 'DisableIPSourceRouting' -ErrorAction SilentlyContinue).DisableIPSourceRouting
    # 啟用 ICMP 重導向
    EnableICMPRedirect   = (Get-ItemProperty -Path $tcpParams -Name 'EnableICMPRedirect' -ErrorAction SilentlyContinue).EnableICMPRedirect
}

# ── SR 7.1：目前 TCP 連線狀態統計 ──
$tcpConnections = Get-NetTCPConnection -ErrorAction SilentlyContinue
$connStats = @{
    Established  = ($tcpConnections | Where-Object { $_.State -eq 'Established' } | Measure-Object).Count
    Listen       = ($tcpConnections | Where-Object { $_.State -eq 'Listen' } | Measure-Object).Count
    TimeWait     = ($tcpConnections | Where-Object { $_.State -eq 'TimeWait' } | Measure-Object).Count
    CloseWait    = ($tcpConnections | Where-Object { $_.State -eq 'CloseWait' } | Measure-Object).Count
    SynSent      = ($tcpConnections | Where-Object { $_.State -eq 'SynSent' } | Measure-Object).Count
    SynReceived  = ($tcpConnections | Where-Object { $_.State -eq 'SynReceived' } | Measure-Object).Count
    TotalConnections = ($tcpConnections | Measure-Object).Count
}

# ── SR 7.1 RE(1)：動態埠範圍設定 ──
$dynamicPortRange = @{}
try {
    $tcpRange = netsh int ipv4 show dynamicport tcp 2>$null | Out-String
    $udpRange = netsh int ipv4 show dynamicport udp 2>$null | Out-String
    $dynamicPortRange = @{
        TcpDynamicPorts = $tcpRange.Trim()
        UdpDynamicPorts = $udpRange.Trim()
    }
} catch { }

# ── SR 7.1 RE(1)：網路介面卡 RSS/RSC 設定（流量處理能力） ──
$adapterPower = Get-NetAdapter -ErrorAction SilentlyContinue | Where-Object { $_.Status -eq 'Up' } |
    ForEach-Object {
        $adapterName = $_.Name
        $rss = Get-NetAdapterRss -Name $adapterName -ErrorAction SilentlyContinue
        $rsc = Get-NetAdapterRsc -Name $adapterName -ErrorAction SilentlyContinue
        @{
            AdapterName   = $adapterName
            RssEnabled    = if ($rss) { $rss.Enabled } else { $null }
            RscIPv4       = if ($rsc) { $rsc.IPv4Enabled } else { $null }
            RscIPv6       = if ($rsc) { $rsc.IPv6Enabled } else { $null }
            LinkSpeed     = $_.LinkSpeed
            ReceiveBuffers  = (Get-NetAdapterAdvancedProperty -Name $adapterName -RegistryKeyword '*ReceiveBuffers' -ErrorAction SilentlyContinue).RegistryValue
            TransmitBuffers = (Get-NetAdapterAdvancedProperty -Name $adapterName -RegistryKeyword '*TransmitBuffers' -ErrorAction SilentlyContinue).RegistryValue
        }
    }

# ── SR 7.1：關鍵網路服務復原設定（降級模式佐證） ──
$criticalServices = @('MpsSvc', 'Dnscache', 'WinRM', 'LanmanServer', 'LanmanWorkstation')
$serviceRecovery = foreach ($svcName in $criticalServices) {
    try {
        $svc = Get-Service -Name $svcName -ErrorAction SilentlyContinue
        if ($svc) {
            # 使用 sc.exe 取得復原設定
            $recoveryInfo = sc.exe qfailure $svcName 2>$null | Out-String
            @{
                ServiceName  = $svcName
                DisplayName  = $svc.DisplayName
                Status       = $svc.Status.ToString()
                StartType    = (Get-CimInstance -ClassName Win32_Service -Filter ""Name='$svcName'"" -ErrorAction SilentlyContinue).StartMode
                RecoveryInfo = $recoveryInfo.Trim()
            }
        }
    } catch { }
}

# ── SR 7.1 RE(2)：系統資源配額（限制單一使用者/程序的資源消耗） ──
$resourceQuotas = @{
    # 磁碟配額狀態
    DiskQuotas = @(Get-CimInstance -ClassName Win32_QuotaSetting -ErrorAction SilentlyContinue |
        ForEach-Object {
            @{
                VolumePath      = $_.VolumePath
                State           = $_.State
                DefaultLimit    = $_.DefaultLimit
                DefaultWarning  = $_.DefaultWarningLimit
            }
        })
    # 最大工作處理程序數
    MaxProcesses = (Get-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\SubSystems' -Name 'Windows' -ErrorAction SilentlyContinue).Windows
    # Windows 資源保護狀態
    SfcDisable = (Get-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon' -Name 'SfcDisable' -ErrorAction SilentlyContinue).SfcDisable
}

@{
    TcpIpHardening         = $tcpHardening
    ConnectionLimits       = $connStats
    DynamicPortRange       = $dynamicPortRange
    NetworkAdapterPower    = @($adapterPower)
    WindowsServiceRecovery = @($serviceRecovery)
    ResourceQuotas         = $resourceQuotas
} | ConvertTo-Json -Depth 4
";
}

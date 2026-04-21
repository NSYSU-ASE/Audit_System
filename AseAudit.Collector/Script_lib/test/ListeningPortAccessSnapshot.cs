namespace AseAudit.Collector.Script_lib;

/// <summary>
/// [DataManagement] 監聽埠與存取控制快照 — 驗證各介面的存取限制。
///
/// 涵蓋驗證項目：
///   SR 2.1 #4 — 確認各介面（HMI、工程師站、遠端）均套用存取控制
///               查詢系統上所有監聽埠、對應的處理程序與服務，
///               並列出防火牆對該埠的存取規則，確保無未受保護的介面暴露
///
/// 輸出：JSON 物件
///   - ListeningPorts: 所有 TCP/UDP 監聽埠、綁定位址、所屬處理程序
///   - PortFirewallCoverage: 每個監聽埠是否有對應的防火牆規則覆蓋
///   - ExposedPorts: 綁定 0.0.0.0 且無防火牆規則的高風險埠
/// </summary>
public static class ListeningPortAccessSnapshot
{
    public const string Content = @"
# ── SR 2.1 #4：收集所有 TCP 監聽埠與對應處理程序 ──
$tcpListeners = Get-NetTCPConnection -State Listen -ErrorAction SilentlyContinue |
    ForEach-Object {
        $proc = Get-Process -Id $_.OwningProcess -ErrorAction SilentlyContinue
        @{
            Protocol      = 'TCP'
            LocalAddress  = $_.LocalAddress
            LocalPort     = $_.LocalPort
            ProcessId     = $_.OwningProcess
            ProcessName   = if ($proc) { $proc.Name } else { 'N/A' }
        }
    }

# 收集所有 UDP 監聽埠
$udpListeners = Get-NetUDPEndpoint -ErrorAction SilentlyContinue |
    ForEach-Object {
        $proc = Get-Process -Id $_.OwningProcess -ErrorAction SilentlyContinue
        @{
            Protocol      = 'UDP'
            LocalAddress  = $_.LocalAddress
            LocalPort     = $_.LocalPort
            ProcessId     = $_.OwningProcess
            ProcessName   = if ($proc) { $proc.Name } else { 'N/A' }
        }
    }

$allListeners = @($tcpListeners) + @($udpListeners)

# ── 取得所有啟用中的入站防火牆規則涵蓋的埠 ──
$fwRules = Get-NetFirewallRule -Direction Inbound -Enabled True -ErrorAction SilentlyContinue
$fwPortFilters = foreach ($rule in $fwRules) {
    $portFilter = $rule | Get-NetFirewallPortFilter -ErrorAction SilentlyContinue
    if ($portFilter.LocalPort -and $portFilter.LocalPort -ne 'Any') {
        @{
            RuleName  = $rule.DisplayName
            Action    = $rule.Action.ToString()
            Ports     = $portFilter.LocalPort
            Protocol  = $portFilter.Protocol
        }
    }
}

# ── 比對監聽埠是否受防火牆規則覆蓋 ──
$coveredPorts = $fwPortFilters | ForEach-Object { $_.Ports } |
    ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } |
    Where-Object { $_ -match '^\d+$' } | Sort-Object -Unique

$portCoverage = $allListeners | ForEach-Object {
    $port = $_.LocalPort.ToString()
    @{
        Port        = $_.LocalPort
        Protocol    = $_.Protocol
        Address     = $_.LocalAddress
        ProcessName = $_.ProcessName
        HasFirewallRule = ($coveredPorts -contains $port)
    }
}

# ── 識別高風險暴露埠：綁定 0.0.0.0/:: 且無防火牆規則 ──
$exposedPorts = $portCoverage | Where-Object {
    ($_.Address -eq '0.0.0.0' -or $_.Address -eq '::') -and
    -not $_.HasFirewallRule
}

@{
    ListeningPorts       = @($allListeners)
    PortFirewallCoverage = @($portCoverage)
    ExposedPorts         = @($exposedPorts)
} | ConvertTo-Json -Depth 4
";
}

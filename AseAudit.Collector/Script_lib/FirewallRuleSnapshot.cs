namespace AseAudit.Collector.Script_lib;

/// <summary>
/// [Firewall] 收集 Windows 防火牆規則 (Get-NetFirewallRule + 對應的 PortFilter / AddressFilter)。
/// 腳本僅輸出 Payload 內容；主機識別 (HostId / Hostname) 由 <see cref="HostInfoSnapshot"/> 共同收集，
/// 於 <see cref="ToJSON.FirewallRuleSnapshotConverter"/> 組裝成完整 Contract Payload。
///
/// 輸出 JSON 對應 <c>FirewallRuleSnapshotContent</c>：
/// <code>
/// { "Rules": [ { "Name": "...", "DisplayName": "...", "Enabled": "...", ... }, ... ] }
/// </code>
///
/// 欄位來源：
///   Get-NetFirewallRule         → Name, DisplayName, Enabled, Profile, Direction, Action
///   Get-NetFirewallPortFilter   → Protocol, LocalPort, RemotePort
///   Get-NetFirewallAddressFilter→ LocalAddress, RemoteAddress
///
/// 多值欄位 (LocalPort / RemotePort / LocalAddress / RemoteAddress) 在 PowerShell 端
/// 以逗號 (,) join 為單一字串，便於 C# 端攤平寫入單欄。
/// </summary>
public static class FirewallRuleSnapshot
{
    public const string Content = @"
# ══════════════════════════════════════════════════════════════
#  FirewallRuleSnapshot — Windows 防火牆規則收集 (Payload only)
#  以 CIM 一次撈齊 Rule / PortFilter / AddressFilter 三張表，
#  再透過 InstanceID hashtable lookup 在記憶體中 join，避免 N+1 query。
# ══════════════════════════════════════════════════════════════

try {
    # ── 1. 一次撈齊三張表（共 3 次 module 呼叫，不管規則有多少條）──
    #     注意：MSFT_NetFirewall*Filter 是 CIM 關聯類別，不能 Get-CimInstance 直接列舉，
    #     必須透過 cmdlet 走 association class 取得。以下 cmdlet 不帶參數即回傳全部。
    $allRules = Get-NetFirewallRule
    $allPort  = Get-NetFirewallPortFilter
    $allAddr  = Get-NetFirewallAddressFilter

    # ── 2. 以 InstanceID 建立 O(1) lookup table ──
    $portLookup = $allPort | Group-Object InstanceID -AsHashTable -AsString
    $addrLookup = $allAddr | Group-Object InstanceID -AsHashTable -AsString

    # ── 3. 在記憶體中 join，不再發 query ──
    $rules = @(
        $allRules | ForEach-Object {
            $r  = $_
            $pf = if ($portLookup) { $portLookup[$r.InstanceID] } else { $null }
            $af = if ($addrLookup) { $addrLookup[$r.InstanceID] } else { $null }

            [PSCustomObject]@{
                Name          = $r.Name
                DisplayName   = $r.DisplayName
                Enabled       = $r.Enabled.ToString()
                Profile       = $r.Profile.ToString()
                Direction     = $r.Direction.ToString()
                Action        = $r.Action.ToString()
                Protocol      = if ($pf) { $pf.Protocol }                                           else { $null }
                LocalPort     = if ($pf) { (@($pf.LocalPort)     | Where-Object { $_ }) -join ',' } else { $null }
                RemotePort    = if ($pf) { (@($pf.RemotePort)    | Where-Object { $_ }) -join ',' } else { $null }
                LocalAddress  = if ($af) { (@($af.LocalAddress)  | Where-Object { $_ }) -join ',' } else { $null }
                RemoteAddress = if ($af) { (@($af.RemoteAddress) | Where-Object { $_ }) -join ',' } else { $null }
            }
        }
    )

    @{ Rules = $rules } | ConvertTo-Json -Depth 4 -Compress
}
catch {
    @{
        Error   = 'Failed to retrieve firewall rule snapshot'
        Message = $_.Exception.Message
    } | ConvertTo-Json
}
";
}

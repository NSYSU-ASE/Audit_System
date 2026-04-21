namespace AseAudit.Collector.Script_lib;

/// <summary>
/// [Firewall] 收集主機網路介面設定（IP、DNS），用於判斷連線路徑與網段歸屬。
/// 輸出：JSON 陣列對應 DeviceConnectionPathRecordDto
/// </summary>
public static class NetworkInterfaceSnapshot
{
    public const string Content = @"
$adapters = Get-NetIPConfiguration |
    Select-Object InterfaceAlias, IPv4Address, IPv6Address, DNSServer
$adapters | ConvertTo-Json -Depth 4
";
}

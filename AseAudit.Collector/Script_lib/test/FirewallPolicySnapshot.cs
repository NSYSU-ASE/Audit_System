namespace AseAudit.Collector.Script_lib;

/// <summary>
/// [Firewall] 收集 Windows 防火牆所有規則（名稱、方向、動作、啟用狀態、套用設定檔）。
/// 輸出：JSON 陣列對應 FirewallPolicySnapshotDto
/// </summary>
public static class FirewallPolicySnapshot
{
    public const string Content = @"
$rules = Get-NetFirewallRule |
    Select-Object Name, DisplayName, Enabled, Direction, Action, Profile
$rules | ConvertTo-Json -Depth 3
";
}

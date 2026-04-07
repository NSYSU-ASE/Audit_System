namespace AseAudit.Collector.Script_lib;

/// <summary>
/// [Identity] 列舉本機所有群組及其成員清單。
/// 輸出：JSON 陣列，每項含 GroupName / Members[]
/// </summary>
public static class UserGroupSnapshot
{
    public const string Content = @"
$groups = Get-LocalGroup | ForEach-Object {
    $g = $_
    $members = Get-LocalGroupMember -Group $g.Name -ErrorAction SilentlyContinue |
               Select-Object -ExpandProperty Name
    @{ GroupName = $g.Name; Members = @($members) }
}
$groups | ConvertTo-Json -Depth 4
";
}

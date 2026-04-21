namespace AseAudit.Collector.Script_lib;

/// <summary>
/// [Identity] 收集本機密碼原則設定（最短長度、複雜度、鎖定閾值等）。
/// 輸出：JSON 對應 PasswordPolicySnapshotDto
/// </summary>
public static class PasswordPolicySnapshot
{
    public const string Content = @"
$policy = net accounts | Out-String
# TODO: 解析 net accounts 輸出並結構化為 JSON
@{ Raw = $policy } | ConvertTo-Json
";
}

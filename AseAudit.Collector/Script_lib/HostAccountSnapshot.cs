namespace AseAudit.Collector.Script_lib;

/// <summary>
/// [Identity] 收集本機帳號、AD 加入狀態、預設帳號狀態、匿名存取設定。
/// 輸出：JSON 對應 HostAccountSnapshotDto
/// </summary>
public static class HostAccountSnapshot
{
    public const string Content = @"
# ══════════════════════════════════════════════════════════════
#  HostAccountSnapshot — 本機帳號與安全設定收集
#  收集本機帳號、AD 加入狀態、預設帳號狀態、匿名存取設定
# ══════════════════════════════════════════════════════════════

try {
    $hostAccount = @{
        HostId             = $env:COMPUTERNAME
        Hostname           = $env:COMPUTERNAME
        LoginRequirement   = @(Get-LocalUser | Select-Object Name, PasswordRequired, Enabled)

        # 預設帳號與 Administrator 狀態
        DefaultAccounts    = @(
            Get-LocalUser -Name ""Administrator"", ""Guest"", ""DefaultAccount"" -ErrorAction SilentlyContinue |
            Select-Object Name, Enabled, PasswordRequired
        )
    }

    $hostAccount | ConvertTo-Json -Depth 3
}
catch {
    @{
        Error   = 'Failed to retrieve host account snapshot'
        Message = $_.Exception.Message
    } | ConvertTo-Json
}
";
}

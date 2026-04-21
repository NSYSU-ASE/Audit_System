namespace AseAudit.Collector.Script_lib;

/// <summary>
/// [Identity] 收集本機帳號與預設帳號狀態。
/// 腳本僅輸出 Payload 內容；主機識別 (HostId / Hostname) 由 <see cref="HostInfoSnapshot"/> 共同收集，
/// 於 <see cref="ToJSON.HostAccountSnapshotConverter"/> 組裝成完整 Contract Payload。
///
/// 輸出 JSON 對應 <c>HostAccountSnapshotContent</c>：
/// <code>
/// { "LoginRequirement": [...], "DefaultAccounts": [...] }
/// </code>
/// </summary>
public static class HostAccountSnapshot
{
    public const string Content = @"
# ══════════════════════════════════════════════════════════════
#  HostAccountSnapshot — 本機帳號與預設帳號狀態收集 (Payload only)
# ══════════════════════════════════════════════════════════════

try {
    $defaultNames = @(""Administrator"", ""Guest"", ""DefaultAccount"")

    @{
        LoginRequirement = @(
            Get-LocalUser |
            Where-Object { $defaultNames -notcontains $_.Name } |
            Select-Object Name, PasswordRequired, Enabled
        )

        # 預設帳號與 Administrator 狀態
        DefaultAccounts  = @(
            Get-LocalUser -Name ""Administrator"", ""Guest"", ""DefaultAccount"" -ErrorAction SilentlyContinue |
            Select-Object Name, Enabled, PasswordRequired
        )
    } | ConvertTo-Json -Depth 4
}
catch {
    @{
        Error   = 'Failed to retrieve host account snapshot'
        Message = $_.Exception.Message
    } | ConvertTo-Json
}
";
}

namespace AseAudit.Collector.Script_lib;

/// <summary>
/// [Identity] 收集 AD 加入狀態與匿名存取設定。
/// 腳本僅輸出 Payload 內容；主機識別 (HostId / Hostname) 由 <see cref="HostInfoSnapshot"/> 共同收集，
/// 於 <see cref="ToJSON.HostAccountRuleSnapshotConverter"/> 組裝成完整 Contract Payload。
///
/// 輸出 JSON 對應 <c>HostAccountRuleSnapshotContent</c>：
/// <code>
/// { "SystemInfo": {...}, "AnonymousAccess": {...} }
/// </code>
/// </summary>
public static class HostAccountRuleSnapshot
{
    public const string Content = @"
# ══════════════════════════════════════════════════════════════
#  HostAccountRuleSnapshot — 本機帳號規則收集 (Payload only)
#  收集 AD 加入狀態、匿名存取設定
# ══════════════════════════════════════════════════════════════

try {
    @{
        SystemInfo      = Get-WmiObject Win32_ComputerSystem | Select-Object Name, Domain, DomainRole

        # 匿名存取設定
        AnonymousAccess = @{
            RestrictAnonymousSAM      = @(Get-ItemProperty -Path ""HKLM:\SYSTEM\CurrentControlSet\Control\Lsa"" -Name ""RestrictAnonymousSAM"" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty RestrictAnonymousSAM)
            RestrictAnonymous         = @(Get-ItemProperty -Path ""HKLM:\SYSTEM\CurrentControlSet\Control\Lsa"" -Name ""RestrictAnonymous"" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty RestrictAnonymous)
            EveryoneIncludesAnonymous = @(Get-ItemProperty -Path ""HKLM:\SYSTEM\CurrentControlSet\Control\Lsa"" -Name ""EveryoneIncludesAnonymous"" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty EveryoneIncludesAnonymous)
        }
    } | ConvertTo-Json -Depth 4
}
catch {
    @{
        Error   = 'Failed to retrieve host account rule snapshot'
        Message = $_.Exception.Message
    } | ConvertTo-Json
}
";
}

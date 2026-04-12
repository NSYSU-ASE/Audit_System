namespace AseAudit.Collector.Script_lib;

/// <summary>
/// [Identity] 收集本機帳號、AD 加入狀態、預設帳號狀態、匿名存取設定。
/// 輸出：JSON 對應 HostAccountRuleSnapshotDto
/// </summary>
public static class HostAccountRuleSnapshot
{
    public const string Content = @"
# ══════════════════════════════════════════════════════════════
#  HostAccountRuleSnapshot — 本機帳號規則收集
#  收集 AD 加入狀態、預設帳號狀態、匿名存取設定
# ══════════════════════════════════════════════════════════════

try {
    $hostAccountRule = @{
        HostId             = $env:COMPUTERNAME
        Hostname           = $env:COMPUTERNAME
        SystemInfo         = Get-WmiObject Win32_ComputerSystem | Select-Object Name, Domain, DomainRole

        # 匿名存取設定
        AnonymousAccess    = @{
            RestrictAnonymousSAM      = @(Get-ItemProperty -Path ""HKLM:\SYSTEM\CurrentControlSet\Control\Lsa"" -Name ""RestrictAnonymousSAM"" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty RestrictAnonymousSAM)
            RestrictAnonymous         = @(Get-ItemProperty -Path ""HKLM:\SYSTEM\CurrentControlSet\Control\Lsa"" -Name ""RestrictAnonymous"" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty RestrictAnonymous)
            EveryoneIncludesAnonymous = @(Get-ItemProperty -Path ""HKLM:\SYSTEM\CurrentControlSet\Control\Lsa"" -Name ""EveryoneIncludesAnonymous"" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty EveryoneIncludesAnonymous)
        }
    }

    $hostAccountRule | ConvertTo-Json -Depth 3
}
catch {
    @{
        Error   = 'Failed to retrieve host account rule snapshot'
        Message = $_.Exception.Message
    } | ConvertTo-Json
}
";
}
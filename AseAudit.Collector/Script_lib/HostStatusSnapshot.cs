namespace AseAudit.Collector.Script_lib;

/// <summary>
/// [Identity] 收集本機帳號、AD 加入狀態、預設帳號狀態、匿名存取設定。
/// 輸出：JSON 對應 HostStatusSnapshotDto
/// </summary>
public static class HostStatusSnapshot
{
    public const string Content = @"
# ══════════════════════════════════════════════════════════════
#  HostStatusSnapshot — 本機狀態與安全設定收集
#  收集 AD 加入狀態、預設帳號狀態、匿名存取設定
# ══════════════════════════════════════════════════════════════

try {
    $hostStatus = @{
        HostId             = $env:COMPUTERNAME
        Hostname           = $env:COMPUTERNAME
        SystemInfo         = Get-WmiObject Win32_ComputerSystem | Select-Object Name, Domain, DomainRole

        # 預設帳號與 Administrator 狀態
        DefaultAccounts    = @(
            Get-LocalUser -Name ""Administrator"", ""Guest"", ""DefaultAccount"" -ErrorAction SilentlyContinue |
            Select-Object Name, Enabled, PasswordRequired
        )

        # 匿名存取設定
        AnonymousAccess    = @{
            RestrictAnonymousSAM      = @(Get-ItemProperty -Path ""HKLM:\SYSTEM\CurrentControlSet\Control\Lsa"" -Name ""RestrictAnonymousSAM"" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty RestrictAnonymousSAM)
            RestrictAnonymous         = @(Get-ItemProperty -Path ""HKLM:\SYSTEM\CurrentControlSet\Control\Lsa"" -Name ""RestrictAnonymous"" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty RestrictAnonymous)
            EveryoneIncludesAnonymous = @(Get-ItemProperty -Path ""HKLM:\SYSTEM\CurrentControlSet\Control\Lsa"" -Name ""EveryoneIncludesAnonymous"" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty EveryoneIncludesAnonymous)
        }
    }

    $hostStatus | ConvertTo-Json -Depth 3
}
catch {
    @{
        Error   = 'Failed to retrieve host status snapshot'
        Message = $_.Exception.Message
    } | ConvertTo-Json
}
";
}
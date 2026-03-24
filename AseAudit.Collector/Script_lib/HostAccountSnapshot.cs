namespace AseAudit.Collector.Script_lib;

/// <summary>
/// [Identity] 收集本機帳號、AD 加入狀態、登入帳號、本機 Admin 權限。
/// 輸出：JSON 對應 HostAccountSnapshotDto
/// </summary>
public static class HostAccountSnapshot
{
    public const string Content = @"
$result = @{
    HostId       = $env:COMPUTERNAME
    Hostname     = $env:COMPUTERNAME
    HasAd        = [bool](Get-WmiObject Win32_ComputerSystem).PartOfDomain
    IsAdAccount  = $null   # TODO: 判斷登入帳號是否為網域帳號
    IsLocalAdmin = ([Security.Principal.WindowsPrincipal]
                    [Security.Principal.WindowsIdentity]::GetCurrent()
                   ).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    LoginAccount = [System.Security.Principal.WindowsIdentity]::GetCurrent().Name
}
$result | ConvertTo-Json -Depth 3
";
}

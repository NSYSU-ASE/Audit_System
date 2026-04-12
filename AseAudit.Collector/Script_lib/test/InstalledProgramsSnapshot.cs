namespace AseAudit.Collector.Script_lib;

/// <summary>
/// [SoftwareControl] 收集已安裝程式清單（32 位元 + 64 位元登錄路徑）。
/// 輸出：JSON 陣列，每項含 DisplayName / DisplayVersion / Publisher / InstallDate
/// </summary>
public static class InstalledProgramsSnapshot
{
    public const string Content = @"
$x64 = Get-ItemProperty HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\* `
       -ErrorAction SilentlyContinue
$x86 = Get-ItemProperty HKLM:\Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\* `
       -ErrorAction SilentlyContinue
$programs = ($x64 + $x86) |
    Where-Object { $_.DisplayName } |
    Select-Object DisplayName, DisplayVersion, Publisher, InstallDate
$programs | ConvertTo-Json -Depth 3
";
}

namespace AseAudit.Collector.Script_lib;

/// <summary>
/// [SoftwareControl] 透過 WMI SecurityCenter2 取得防毒軟體狀態。
/// 輸出：JSON 對應 AntivirusStatusRecordDto（displayName / productState / pathToSignedProductExe）
/// </summary>
public static class AntivirusStatusSnapshot
{
    public const string Content = @"
$av = Get-WmiObject -Namespace 'root\SecurityCenter2' -Class AntiVirusProduct `
      -ErrorAction SilentlyContinue |
    Select-Object displayName, productState, pathToSignedProductExe
if (-not $av) { $av = @{ displayName = 'NONE'; productState = 0 } }
$av | ConvertTo-Json -Depth 3
";
}

namespace AseAudit.Collector.Script_lib;

/// <summary>
/// [Common] 主機識別資訊收集腳本。
/// 所有 payload 腳本共用的前置腳本，由 Worker 先執行取得 <c>HostId</c> / <c>Hostname</c>，
/// 再於組裝各 Contract Payload (<see cref="ASEAudit.Shared.Contracts.IScriptPayload"/>) 時填入。
///
/// 輸出 JSON 結構：
/// <code>
/// { "HostId": "...", "Hostname": "..." }
/// </code>
/// </summary>
public static class HostInfoSnapshot
{
    public const string Content = @"
# ══════════════════════════════════════════════════════════════
#  HostInfoSnapshot — 主機識別資訊收集 (共同前置腳本)
# ══════════════════════════════════════════════════════════════

try {
    @{
        HostId   = $env:COMPUTERNAME
        Hostname = $env:COMPUTERNAME
    } | ConvertTo-Json -Compress
}
catch {
    @{
        Error   = 'Failed to retrieve host info'
        Message = $_.Exception.Message
    } | ConvertTo-Json
}
";
}

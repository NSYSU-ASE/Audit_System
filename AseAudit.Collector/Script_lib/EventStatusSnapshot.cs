namespace AseAudit.Collector.Script_lib;

/// <summary>
/// [AuditPolicy] 稽核政策狀態快照 — 收集 Windows 稽核政策設定。
///
/// 執行 auditpol /get /category:* 命令，蒐集所有稽核類別的當前設定狀態。
///
/// 輸出格式：JSON 物件，包含各稽核類別及其子政策的設定狀態（成功/失敗/無）
/// </summary>
public static class EventStatusSnapshot
{
    public const string Content = @"
# ══════════════════════════════════════════════════════════════
#  EventStatusSnapshot — 稽核政策狀態收集
#  執行 auditpol /get /category:* 獲取所有稽核政策設定
# ══════════════════════════════════════════════════════════════

try {
    # 執行 auditpol 命令，取得所有稽核類別的政策設定
    $auditpolOutput = & auditpol /get /category:*

    if (-not $auditpolOutput) {
        @{ Error = 'Failed to retrieve audit policy' } | ConvertTo-Json
        exit
    }

    # 輸出原始文字，由 C# 解析為 JSON
    $auditpolOutput | Out-String
}
catch {
    @{
        Error   = 'Failed to retrieve audit policy'
        Message = $_.Exception.Message
    } | ConvertTo-Json
}
";
}

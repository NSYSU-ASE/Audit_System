using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASEAudit.Shared.Scoring;

public sealed class AuditItemResult
{
    // 識別
    public string ItemKey { get; init; } = string.Empty;

    // 顯示用（報表 / UI）
    public string Title { get; init; } = string.Empty;     
    public string Message { get; init; } = string.Empty;   

    // 評分用
    public double Score { get; init; }        // 0~100
    public double Weight { get; init; } = 1.0;
    public bool? Passed { get; init; }

    // 擴充資訊
    public Dictionary<string, object>? Detail { get; init; }
}


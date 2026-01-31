using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASEAudit.Shared.Scoring;

public sealed class AuditItemResult
{
    public string ItemKey { get; init; } = string.Empty;
    public double Score { get; init; }                    // 0~100
    public double Weight { get; init; } = 1.0;            // default 1
    public bool? Passed { get; init; }                    // optional
    public Dictionary<string, object?>? Detail { get; init; } // optional
}


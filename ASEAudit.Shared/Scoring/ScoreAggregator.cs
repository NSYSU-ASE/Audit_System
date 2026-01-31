using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASEAudit.Shared.Scoring;

public static class ScoreAggregator
{
    public static double Aggregate(IEnumerable<AuditItemResult> items)
    {
        var list = items?.ToList() ?? new();
        if (list.Count == 0) return 0;

        var totalWeight = list.Sum(x => Math.Max(0, x.Weight));
        if (totalWeight <= 0)
            return Math.Round(list.Average(x => x.Score), 2);

        var weightedSum = list.Sum(x => x.Score * Math.Max(0, x.Weight));
        return Math.Round(weightedSum / totalWeight, 2);
    }
}


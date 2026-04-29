using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AseAudit.Core.Modules.Identity.Dtos;

public sealed class IdentityAuditDeviceSummaryDto
{
    public string HostName { get; set; } = "";

    public double AverageScore { get; set; }

    public string RiskLevel { get; set; } = "";

    public int IncludedRuleCount { get; set; }

    public int TotalRuleCount { get; set; }

    public List<IdentityRuleResultDto> Rules { get; set; } = new();
}
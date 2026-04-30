using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AseAudit.Core.Modules.Identity.Dtos;

public class IdentityRuleResultDto
{
    public string SrMapping { get; set; } = "";

    public string RuleName { get; set; } = "";

    public double? Score { get; set; }

    public bool IncludedInScore { get; set; }

    public string Status { get; set; } = "";

    public List<string> Issues { get; set; } = new();

    public List<double> DetailScores { get; set; } = new();
}
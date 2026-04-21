using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.Generic;
using ASEAudit.Shared.Scoring;

namespace AseAudit.Core.Modules.ResourceManagement.Dtos
{
    public sealed class ResourceManagementAuditSummaryDto
    {
        public string DeviceId { get; set; } = "";
        public List<AuditItemResult> Results { get; set; } = new();
        public int TotalScore { get; set; }
    }
}
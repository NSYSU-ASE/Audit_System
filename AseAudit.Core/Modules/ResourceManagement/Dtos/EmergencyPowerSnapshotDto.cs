using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AseAudit.Core.Modules.ResourceManagement.Dtos
{
    public sealed class EmergencyPowerSnapshotDto
    {
        public string DeviceId { get; set; } = "";
        public bool HasUps { get; set; }
        public bool HasBackupPower { get; set; }
        public bool HasMaintenanceRecord { get; set; }
        public bool HasManualReviewEvidence { get; set; }
        public string? EvidenceFileName { get; set; }
    }
}

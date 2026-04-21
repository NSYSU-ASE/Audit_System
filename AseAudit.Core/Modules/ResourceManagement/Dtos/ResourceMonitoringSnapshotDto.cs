using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AseAudit.Core.Modules.ResourceManagement.Dtos
{
    public sealed class ResourceMonitoringSnapshotDto
    {
        public string DeviceId { get; set; } = "";
        public bool HasMonitoringFeature { get; set; }
        public bool HasCpuMonitoring { get; set; }
        public bool HasMemoryMonitoring { get; set; }
        public bool HasThresholdConfigured { get; set; }
        public string? EvidenceSource { get; set; }
    }
}
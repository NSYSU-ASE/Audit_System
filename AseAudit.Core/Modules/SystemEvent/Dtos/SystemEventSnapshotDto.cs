using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AseAudit.Core.Modules.SystemEvent.Dtos
{
    public sealed class SystemEventSnapshotDto
    {
        public string DeviceId { get; set; } = "";
        public string DeviceName { get; set; } = "";

        public AuditLogStatusDto AlarmLog { get; set; } = new();
        public SessionControlStatusDto SessionControl { get; set; } = new();
        public StorageMonitoringStatusDto StorageMonitoring { get; set; } = new();
        public ResponseHandlingStatusDto ResponseHandling { get; set; } = new();
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AseAudit.Core.Modules.SystemEvent.Dtos
{
    public sealed class StorageMonitoringStatusDto
    {
        // SR2.9 稽核儲存容量
        public bool DiskSpaceAlertEnabled { get; set; }
        public int? AlertThresholdPercent { get; set; }  // 例如 20 代表剩餘空間低於20%告警
        public bool HasNotificationMechanism { get; set; }
        public string? NotificationChannel { get; set; } // Email / SMS / SCADA alarm / Ticket ...
    }
}

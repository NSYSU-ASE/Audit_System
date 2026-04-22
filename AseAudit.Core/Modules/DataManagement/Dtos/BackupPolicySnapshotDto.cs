using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AseAudit.Core.Modules.DataManagement.Dtos
{
    public sealed class BackupPolicySnapshotDto
    {
        public string DeviceId { get; set; } = "";

        public bool HasBackupMechanism { get; set; }
        public bool HasBackupSchedule { get; set; }
        public bool HasBackupScopeDefined { get; set; }

        /// <summary>例如 Daily / Weekly / Every 4 hours</summary>
        public string? BackupFrequency { get; set; }

        public string? Comment { get; set; }
    }
}
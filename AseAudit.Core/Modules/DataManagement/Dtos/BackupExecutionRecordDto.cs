using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AseAudit.Core.Modules.DataManagement.Dtos
{
    public sealed class BackupExecutionRecordDto
    {
        public string DeviceId { get; set; } = "";

        public DateTime? LastBackupTime { get; set; }
        public DateTime? PreviousBackupTime { get; set; }

        public bool IsBackupSuccessful { get; set; }

        /// <summary>實際 RPO（分鐘），可由系統算好後塞進來</summary>
        public int? ActualRpoMinutes { get; set; }
    }
}
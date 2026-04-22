using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AseAudit.Core.Modules.DataManagement.Dtos
{
    public sealed class RecoveryExecutionRecordDto
    {
        public string DeviceId { get; set; } = "";

        public bool HasRecoveryTest { get; set; }
        public DateTime? LastRecoveryTestTime { get; set; }
        public bool IsRecoverySuccessful { get; set; }

        /// <summary>實際 RTO（分鐘）</summary>
        public int? ActualRtoMinutes { get; set; }

        public bool HasRebuildProcedure { get; set; }
        public bool HasSafeStateRecovery { get; set; }
    }
}
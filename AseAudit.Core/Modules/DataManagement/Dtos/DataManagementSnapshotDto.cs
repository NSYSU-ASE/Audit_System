using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AseAudit.Core.Modules.DataManagement.Dtos
{
    public sealed class DataManagementSnapshotDto
    {
        public string DeviceId { get; set; } = "";

        public List<ManualReviewResultDto> ManualReviewResults { get; set; } = new();

        public RecoveryTargetBaselineDto? RecoveryBaseline { get; set; }
        public BackupPolicySnapshotDto? BackupPolicy { get; set; }
        public BackupExecutionRecordDto? BackupExecution { get; set; }
        public RecoveryExecutionRecordDto? RecoveryExecution { get; set; }
        public ApplicationPartitionSnapshotDto? AppPartition { get; set; }
    }
}
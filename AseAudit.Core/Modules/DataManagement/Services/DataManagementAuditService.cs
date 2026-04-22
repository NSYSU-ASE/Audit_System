using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.Generic;
using System.Linq;
using ASEAudit.Shared.Scoring;
using AseAudit.Core.Modules.DataManagement.Dtos;
using AseAudit.Core.Modules.DataManagement.Rules;

namespace AseAudit.Core.Modules.DataManagement.Services
{
    public sealed class DataManagementAuditService
    {
        private readonly ManualReviewResultRule _manualRule = new();
        private readonly ApplicationPartitionRule _r54 = new();
        private readonly BackupPolicyRule _r73 = new();
        private readonly BackupVerificationRule _r73re = new();
        private readonly RecoveryRebuildRule _r74 = new();

        public List<AuditItemResult> Evaluate(DataManagementSnapshotDto snapshot)
        {
            var results = new List<AuditItemResult>
            {
                EvaluateManual(snapshot, "data.3.1", "通訊完整性檢查（SR3.1）"),
                EvaluateManual(snapshot, "data.3.5", "輸入驗證檢查（SR3.5）"),
                EvaluateManual(snapshot, "data.3.6", "確定性輸出檢查（SR3.6）"),
                EvaluateManual(snapshot, "data.3.7", "錯誤處理檢查（SR3.7）"),
                EvaluateManual(snapshot, "data.4.2", "資訊持續性檢查（SR4.2）"),
                _r54.Evaluate(snapshot.AppPartition),
                _r73.Evaluate(snapshot.BackupPolicy, snapshot.BackupExecution, snapshot.RecoveryBaseline),
                _r73re.Evaluate(snapshot.RecoveryExecution, snapshot.RecoveryBaseline),
                _r74.Evaluate(snapshot.RecoveryExecution)
            };

            return results;
        }

        private AuditItemResult EvaluateManual(DataManagementSnapshotDto snapshot, string itemKey, string title)
        {
            var row = snapshot.ManualReviewResults
                .FirstOrDefault(x => x.ItemKey == itemKey && x.DeviceId == snapshot.DeviceId);

            if (row is null)
            {
                return _manualRule.Evaluate(itemKey, title, false, false, false, "找不到人工審查結果。");
            }

            return _manualRule.Evaluate(
                itemKey,
                title,
                row.IsReviewed,
                row.IsPass,
                row.IsPartial,
                row.Comment);
        }
    }
}
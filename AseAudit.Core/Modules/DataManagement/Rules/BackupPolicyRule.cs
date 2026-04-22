using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ASEAudit.Shared.Scoring;
using AseAudit.Core.Modules.DataManagement.Dtos;

namespace AseAudit.Core.Modules.DataManagement.Rules
{
    public sealed class BackupPolicyRule
    {
        public AuditItemResult Evaluate(
            BackupPolicySnapshotDto? policy,
            BackupExecutionRecordDto? execution,
            RecoveryTargetBaselineDto? baseline)
        {
            if (policy is null || execution is null || baseline is null)
            {
                return Fail("備份策略或 RPO 基準資料不足。");
            }

            if (!policy.HasBackupMechanism || !policy.HasBackupSchedule)
            {
                return Fail("未建立完整備份機制或排程。");
            }

            if (!baseline.TargetRpoMinutes.HasValue || !execution.ActualRpoMinutes.HasValue)
            {
                return new AuditItemResult
                {
                    ItemKey = "data.7.3",
                    Title = "控制系統備份檢查（SR7.3）",
                    Score = 50,
                    Weight = 1,
                    Message = "已建立備份機制，但尚未定義或量化 RPO。"
                };
            }

            if (execution.ActualRpoMinutes.Value <= baseline.TargetRpoMinutes.Value)
            {
                return Pass($"符合 RPO。目標：{baseline.TargetRpoMinutes} 分鐘；實際：{execution.ActualRpoMinutes} 分鐘。", 100);
            }

            return new AuditItemResult
            {
                ItemKey = "data.7.3",
                Title = "控制系統備份檢查（SR7.3）",
                Score = 50,
                Weight = 1,
                Message = $"備份存在，但未符合 RPO。目標：{baseline.TargetRpoMinutes} 分鐘；實際：{execution.ActualRpoMinutes} 分鐘。"
            };
        }

        private static AuditItemResult Pass(string msg, int score) => new()
        {
            ItemKey = "data.7.3",
            Title = "控制系統備份檢查（SR7.3）",
            Score = score,
            Weight = 1,
            Message = msg
        };

        private static AuditItemResult Fail(string msg) => new()
        {
            ItemKey = "data.7.3",
            Title = "控制系統備份檢查（SR7.3）",
            Score = 0,
            Weight = 1,
            Message = msg
        };
    }
}
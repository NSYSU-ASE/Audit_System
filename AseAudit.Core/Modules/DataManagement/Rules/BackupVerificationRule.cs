using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ASEAudit.Shared.Scoring;
using AseAudit.Core.Modules.DataManagement.Dtos;

namespace AseAudit.Core.Modules.DataManagement.Rules
{
    public sealed class BackupVerificationRule
    {
        public AuditItemResult Evaluate(
            RecoveryExecutionRecordDto? recovery,
            RecoveryTargetBaselineDto? baseline)
        {
            if (recovery is null || baseline is null)
            {
                return Fail("備份查證或 RTO 基準資料不足。");
            }

            if (!recovery.HasRecoveryTest)
            {
                return Fail("未執行備份還原驗證。");
            }

            if (!baseline.TargetRtoMinutes.HasValue || !recovery.ActualRtoMinutes.HasValue)
            {
                return new AuditItemResult
                {
                    ItemKey = "data.7.3re",
                    Title = "備份查證檢查（SR7.3 RE(1)）",
                    Score = 50,
                    Weight = 1,
                    Message = "已執行還原驗證，但尚未量化 RTO。"
                };
            }

            if (recovery.IsRecoverySuccessful && recovery.ActualRtoMinutes.Value <= baseline.TargetRtoMinutes.Value)
            {
                return Pass($"還原驗證成功且符合 RTO。目標：{baseline.TargetRtoMinutes} 分鐘；實際：{recovery.ActualRtoMinutes} 分鐘。", 100);
            }

            return new AuditItemResult
            {
                ItemKey = "data.7.3re",
                Title = "備份查證檢查（SR7.3 RE(1)）",
                Score = 50,
                Weight = 1,
                Message = "已執行還原驗證，但未符合 RTO 或還原失敗。"
            };
        }

        private static AuditItemResult Pass(string msg, int score) => new()
        {
            ItemKey = "data.7.3re",
            Title = "備份查證檢查（SR7.3 RE(1)）",
            Score = score,
            Weight = 1,
            Message = msg
        };

        private static AuditItemResult Fail(string msg) => new()
        {
            ItemKey = "data.7.3re",
            Title = "備份查證檢查（SR7.3 RE(1)）",
            Score = 0,
            Weight = 1,
            Message = msg
        };
    }
}

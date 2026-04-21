using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ASEAudit.Shared.Scoring;
using AseAudit.Core.Modules.ResourceManagement.Dtos;

namespace AseAudit.Core.Modules.ResourceManagement.Rules
{
    public sealed class EmergencyPowerProtectionRule
    {
        public AuditItemResult Evaluate(EmergencyPowerSnapshotDto? dto)
        {
            if (dto is null)
            {
                return Fail("找不到緊急電源資料。");
            }

            if (dto.HasUps || dto.HasBackupPower)
            {
                if (dto.HasManualReviewEvidence)
                {
                    return Pass("已具備 UPS / 備援電源，且有人工審查佐證。", 100);
                }

                return new AuditItemResult
                {
                    ItemKey = "resource.7.5",
                    Title = "緊急電源檢查（SR7.5）",
                    Score = 50,
                    Weight = 1,
                    Message = "已有 UPS / 備援電源資料，但缺少人工審查佐證。"
                };
            }

            return Fail("未建立 UPS / 備援電源機制。");
        }

        private static AuditItemResult Pass(string msg, int score) => new()
        {
            ItemKey = "resource.7.5",
            Title = "緊急電源檢查（SR7.5）",
            Score = score,
            Weight = 1,
            Message = msg
        };

        private static AuditItemResult Fail(string msg) => new()
        {
            ItemKey = "resource.7.5",
            Title = "緊急電源檢查（SR7.5）",
            Score = 0,
            Weight = 1,
            Message = msg
        };
    }
}
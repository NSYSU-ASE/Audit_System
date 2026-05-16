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
        public AuditItemResult Evaluate(ResourceManualReviewResultDto? review)
        {
            if (review is null)
            {
                return Fail("找不到 UPS / 備援電源人工審查結果。");
            }

            if (!review.IsReviewed)
            {
                return Fail("尚未完成 UPS / 備援電源人工審查。");
            }

            if (review.IsPass)
            {
                return Pass(BuildMessage("人工審查通過，已確認 UPS / 備援電源機制。", review), 100);
            }

            if (review.IsPartial)
            {
                return new AuditItemResult
                {
                    ItemKey = "resource.7.5",
                    Title = "緊急電源檢查（SR7.5）",
                    Score = 50,
                    Weight = 1,
                    Message = BuildMessage("人工審查部分符合，UPS / 備援電源機制尚未完整。", review)
                };
            }

            return Fail(BuildMessage("人工審查未通過，未確認具備 UPS / 備援電源機制。", review));
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

        private static string BuildMessage(string prefix, ResourceManualReviewResultDto review)
        {
            var parts = new List<string> { prefix };

            if (!string.IsNullOrWhiteSpace(review.Comment))
            {
                parts.Add(review.Comment.Trim());
            }

            if (!string.IsNullOrWhiteSpace(review.EvidenceFileName))
            {
                parts.Add($"佐證檔案：{review.EvidenceFileName.Trim()}");
            }

            return string.Join("；", parts);
        }
    }
}

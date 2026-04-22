using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ASEAudit.Shared.Scoring;

namespace AseAudit.Core.Modules.DataManagement.Rules
{
    public sealed class ManualReviewResultRule
    {
        public AuditItemResult Evaluate(
            string itemKey,
            string title,
            bool isReviewed,
            bool isPass,
            bool isPartial,
            string? comment)
        {
            if (!isReviewed)
            {
                return Fail(itemKey, title, "尚未完成人工審查。");
            }

            if (isPass)
            {
                return Pass(itemKey, title, "人工審查通過。", 100);
            }

            if (isPartial)
            {
                return new AuditItemResult
                {
                    ItemKey = itemKey,
                    Title = title,
                    Score = 50,
                    Weight = 1,
                    Message = $"人工審查部分符合。{comment}"
                };
            }

            return Fail(itemKey, title, $"人工審查未通過。{comment}");
        }

        private static AuditItemResult Pass(string itemKey, string title, string msg, int score) => new()
        {
            ItemKey = itemKey,
            Title = title,
            Score = score,
            Weight = 1,
            Message = msg
        };

        private static AuditItemResult Fail(string itemKey, string title, string msg) => new()
        {
            ItemKey = itemKey,
            Title = title,
            Score = 0,
            Weight = 1,
            Message = msg
        };
    }
}
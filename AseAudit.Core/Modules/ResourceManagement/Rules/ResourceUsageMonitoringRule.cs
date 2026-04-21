using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ASEAudit.Shared.Scoring;
using AseAudit.Core.Modules.ResourceManagement.Dtos;

namespace AseAudit.Core.Modules.ResourceManagement.Rules
{
    public sealed class ResourceUsageMonitoringRule
    {
        public AuditItemResult Evaluate(ResourceMonitoringSnapshotDto? dto)
        {
            if (dto is null)
            {
                return Fail("找不到資源監控資料。");
            }

            if (!dto.HasMonitoringFeature)
            {
                return new AuditItemResult
                {
                    ItemKey = "resource.7.2",
                    Title = "資源使用監控檢查（SR7.2）",
                    Score = 0,
                    Weight = 1,
                    Message = "不具有資源監控功能。"
                };
            }

            if (dto.HasCpuMonitoring && dto.HasMemoryMonitoring && dto.HasThresholdConfigured)
            {
                return Pass("已設定 CPU / Memory 監控與門檻。", 100);
            }

            return new AuditItemResult
            {
                ItemKey = "resource.7.2",
                Title = "資源使用監控檢查（SR7.2）",
                Score = 50,
                Weight = 1,
                Message = "已有監控功能，但未完整設定 CPU / Memory 門檻。"
            };
        }

        private static AuditItemResult Pass(string msg, int score) => new()
        {
            ItemKey = "resource.7.2",
            Title = "資源使用監控檢查（SR7.2）",
            Score = score,
            Weight = 1,
            Message = msg
        };

        private static AuditItemResult Fail(string msg) => new()
        {
            ItemKey = "resource.7.2",
            Title = "資源使用監控檢查（SR7.2）",
            Score = 0,
            Weight = 1,
            Message = msg
        };
    }
}
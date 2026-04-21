using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ASEAudit.Shared.Scoring;
using AseAudit.Core.Modules.ResourceManagement.Dtos;

namespace AseAudit.Core.Modules.ResourceManagement.Rules
{
    public sealed class ComponentInventoryRule
    {
        public AuditItemResult Evaluate(
            System.Collections.Generic.IEnumerable<TopologyAssetRecordDto> topologyAssets,
            System.Collections.Generic.IEnumerable<ComponentInventoryRecordDto> components)
        {
            var topo = topologyAssets?.ToList() ?? new();
            var comp = components?.ToList() ?? new();

            if (comp.Count == 0)
            {
                return Fail("未建立控制系統組件清冊。");
            }

            int matched = topo.Count(t =>
                comp.Any(c => c.DeviceId == t.AssetId || c.ComponentId == t.AssetId));

            if (matched == topo.Count && topo.Count > 0)
            {
                return Pass("已建立完整控制系統組件清冊，並可對應拓譜圖資產。", 100);
            }

            if (matched > 0 && topo.Count > 0)
            {
                int score = (int)System.Math.Round((double)matched / topo.Count * 100, MidpointRounding.AwayFromZero);
                return new AuditItemResult
                {
                    ItemKey = "resource.7.8",
                    Title = "控制系統組件清冊檢查（SR7.8）",
                    Score = score,
                    Weight = 1,
                    Message = $"拓譜圖資產共 {topo.Count} 筆，已建立清冊對應 {matched} 筆。"
                };
            }

            return new AuditItemResult
            {
                ItemKey = "resource.7.8",
                Title = "控制系統組件清冊檢查（SR7.8）",
                Score = 50,
                Weight = 1,
                Message = "已有部分組件清冊，但無法完整對應拓譜圖資產。"
            };
        }

        private static AuditItemResult Pass(string msg, int score) => new()
        {
            ItemKey = "resource.7.8",
            Title = "控制系統組件清冊檢查（SR7.8）",
            Score = score,
            Weight = 1,
            Message = msg
        };

        private static AuditItemResult Fail(string msg) => new()
        {
            ItemKey = "resource.7.8",
            Title = "控制系統組件清冊檢查（SR7.8）",
            Score = 0,
            Weight = 1,
            Message = msg
        };
    }
}
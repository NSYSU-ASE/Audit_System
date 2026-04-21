using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ASEAudit.Shared.Scoring;
using AseAudit.Core.Modules.ResourceManagement.Dtos;

namespace AseAudit.Core.Modules.ResourceManagement.Rules
{
    public sealed class NetworkSecurityBaselineRule
    {
        public AuditItemResult Evaluate(
            System.Collections.Generic.IEnumerable<TopologyAssetRecordDto> topologyAssets,
            System.Collections.Generic.IEnumerable<NetworkSecurityBaselineSnapshotDto> baselines)
        {
            var topo = topologyAssets?.ToList() ?? new();
            var baseRows = baselines?.ToList() ?? new();

            if (topo.Count == 0)
            {
                return Fail("無拓譜圖資產資料，無法檢查網路與安全組態。");
            }

            int matched = topo.Count(t =>
                baseRows.Any(b =>
                    b.AssetId == t.AssetId &&
                    b.HasSecurityBaseline &&
                    b.HasZoneAssignment &&
                    b.HasAssetRegistration));

            if (matched == topo.Count)
            {
                return Pass("拓譜圖中的資產皆已建立安全組態與納管資訊。", 100);
            }

            if (matched > 0)
            {
                int score = (int)System.Math.Round((double)matched / topo.Count * 100, MidpointRounding.AwayFromZero);
                return new AuditItemResult
                {
                    ItemKey = "resource.7.6",
                    Title = "網路及安全組態檢查（SR7.6）",
                    Score = score,
                    Weight = 1,
                    Message = $"拓譜圖資產共 {topo.Count} 筆，已完成安全組態納管 {matched} 筆。"
                };
            }

            return Fail("尚未建立有效的網路安全組態與資產納管機制。");
        }

        private static AuditItemResult Pass(string msg, int score) => new()
        {
            ItemKey = "resource.7.6",
            Title = "網路及安全組態檢查（SR7.6）",
            Score = score,
            Weight = 1,
            Message = msg
        };

        private static AuditItemResult Fail(string msg) => new()
        {
            ItemKey = "resource.7.6",
            Title = "網路及安全組態檢查（SR7.6）",
            Score = 0,
            Weight = 1,
            Message = msg
        };
    }
}

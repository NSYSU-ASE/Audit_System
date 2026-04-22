using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ASEAudit.Shared.Scoring;
using AseAudit.Core.Modules.DataManagement.Dtos;

namespace AseAudit.Core.Modules.DataManagement.Rules
{
    public sealed class RecoveryRebuildRule
    {
        public AuditItemResult Evaluate(RecoveryExecutionRecordDto? dto)
        {
            if (dto is null)
            {
                return Fail("找不到復原與重建資料。");
            }

            if (dto.HasRebuildProcedure && dto.HasSafeStateRecovery)
            {
                return Pass("已建立復原程序與安全狀態回復機制。", 100);
            }

            if (dto.HasRebuildProcedure || dto.HasSafeStateRecovery)
            {
                return new AuditItemResult
                {
                    ItemKey = "data.7.4",
                    Title = "控制系統復原及重建檢查（SR7.4）",
                    Score = 50,
                    Weight = 1,
                    Message = "已建立部分復原 / 重建能力，但尚未完整。"
                };
            }

            return Fail("未建立復原及重建機制。");
        }

        private static AuditItemResult Pass(string msg, int score) => new()
        {
            ItemKey = "data.7.4",
            Title = "控制系統復原及重建檢查（SR7.4）",
            Score = score,
            Weight = 1,
            Message = msg
        };

        private static AuditItemResult Fail(string msg) => new()
        {
            ItemKey = "data.7.4",
            Title = "控制系統復原及重建檢查（SR7.4）",
            Score = 0,
            Weight = 1,
            Message = msg
        };
    }
}
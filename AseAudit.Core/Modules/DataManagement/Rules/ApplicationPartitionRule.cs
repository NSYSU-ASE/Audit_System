using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ASEAudit.Shared.Scoring;
using AseAudit.Core.Modules.DataManagement.Dtos;

namespace AseAudit.Core.Modules.DataManagement.Rules
{
    public sealed class ApplicationPartitionRule
    {
        public AuditItemResult Evaluate(ApplicationPartitionSnapshotDto? dto)
        {
            if (dto is null)
            {
                return Fail("找不到應用分割資料。");
            }

            int count = 0;
            if (dto.HasSystemDisk) count++;
            if (dto.HasAppDisk) count++;
            if (dto.HasDataDisk) count++;
            if (dto.HasLogDisk) count++;

            if (count >= 4)
            {
                return Pass("作業系統、應用程式、資料與 Log 已分區。", 100);
            }

            if (count >= 2)
            {
                return new AuditItemResult
                {
                    ItemKey = "data.5.4",
                    Title = "應用分割檢查（SR5.4）",
                    Score = 50,
                    Weight = 1,
                    Message = "已有部分分區，但尚未完整分離系統、應用、資料與 Log。"
                };
            }

            return Fail("未建立有效的應用分割。");
        }

        private static AuditItemResult Pass(string msg, int score) => new()
        {
            ItemKey = "data.5.4",
            Title = "應用分割檢查（SR5.4）",
            Score = score,
            Weight = 1,
            Message = msg
        };

        private static AuditItemResult Fail(string msg) => new()
        {
            ItemKey = "data.5.4",
            Title = "應用分割檢查（SR5.4）",
            Score = 0,
            Weight = 1,
            Message = msg
        };
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ASEAudit.Shared.Scoring;
using AseAudit.Core.Modules.Firewall.Dtos;

namespace AseAudit.Core.Modules.Firewall.Rules
{
    /// <summary>
    /// 跳板主機連線檢查
    /// 對應 SR2.6
    /// </summary>
    public sealed class JumpHostConnectionRule
    {
        public AuditItemResult Evaluate(
            string deviceId,
            IEnumerable<DeviceConnectionPathRecordDto> connectionRows)
        {
            deviceId = (deviceId ?? "").Trim();

            if (string.IsNullOrWhiteSpace(deviceId))
            {
                return Fail("未提供 deviceId，無法判斷是否透過跳板主機連線。");
            }

            var row = (connectionRows ?? Enumerable.Empty<DeviceConnectionPathRecordDto>())
                .FirstOrDefault(x => string.Equals((x.DeviceId ?? "").Trim(), deviceId, StringComparison.OrdinalIgnoreCase));

            if (row is null)
            {
                return Fail("找不到該設備的連線路徑紀錄。");
            }

            if (!row.ViaJumpHost)
            {
                return new AuditItemResult
                {
                    ItemKey = "firewall.jump_host_connection",
                    Title = "跳板主機連線檢查（SR2.6）",
                    Score = 0,
                    Weight = 1,
                    Message = "連線未經由跳板主機。"
                };
            }

            return new AuditItemResult
            {
                ItemKey = "firewall.jump_host_connection",
                Title = "跳板主機連線檢查（SR2.6）",
                Score = 100,
                Weight = 1,
                Message = $"連線已經由跳板主機：{row.JumpHostName ?? "未標示名稱"}。"
            };
        }

        private static AuditItemResult Fail(string reason)
            => new AuditItemResult
            {
                ItemKey = "firewall.jump_host_connection",
                Title = "跳板主機連線檢查（SR2.6）",
                Score = 0,
                Weight = 1,
                Message = reason
            };
    }
}

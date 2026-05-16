using System;
using System.Collections.Generic;
using System.Linq;
using ASEAudit.Shared.Scoring;
using AseAudit.Core.Modules.SystemEvent.Dtos;

namespace AseAudit.Core.Modules.SystemEvent.Rules
{
    /// <summary>
    /// SR2.11 時戳：確認 Windows 工作排程與 OA 時間同步設定。
    /// </summary>
    public sealed class SystemEventTimeSyncRule
    {
        public AuditItemResult Evaluate(
            string deviceId,
            IEnumerable<SystemEventSnapshotDto> rows)
        {
            deviceId = (deviceId ?? "").Trim();

            if (string.IsNullOrWhiteSpace(deviceId))
            {
                return Fail("未提供 deviceId，無法執行時戳 / 時間同步稽核。");
            }

            var row = (rows ?? Enumerable.Empty<SystemEventSnapshotDto>())
                .FirstOrDefault(x =>
                    string.Equals((x.DeviceId ?? "").Trim(), deviceId, StringComparison.OrdinalIgnoreCase));

            if (row is null)
            {
                return Fail("找不到該設備的系統事件模組資料。");
            }

            var timeSync = row.TimeSync;
            if (timeSync is null || !timeSync.FeatureAvailable)
            {
                return Fail("不具有時戳 / 時間同步功能。");
            }

            var messages = new List<string>();

            if (timeSync.WindowsScheduleConfigured)
            {
                messages.Add("已設定 Windows 工作排程");
            }
            else
            {
                messages.Add("未設定 Windows 工作排程");
            }

            if (timeSync.OaTimeSyncEnabled)
            {
                messages.Add("已設定 OA 時間同步");
            }
            else
            {
                messages.Add("未設定 OA 時間同步");
            }

            if (!string.IsNullOrWhiteSpace(timeSync.TimeSource))
            {
                messages.Add($"時間來源：{timeSync.TimeSource.Trim()}");
            }

            if (timeSync.LastSyncAt.HasValue)
            {
                messages.Add($"最後同步時間：{timeSync.LastSyncAt.Value:yyyy-MM-dd HH:mm:ss}");
            }

            if (timeSync.WindowsScheduleConfigured && timeSync.OaTimeSyncEnabled)
            {
                return Pass($"SR2.11 通過：{string.Join("；", messages)}。", 100);
            }

            return new AuditItemResult
            {
                ItemKey = "system_event.time_sync",
                Title = "時戳 / 時間同步檢查（SR2.11）",
                Score = 50,
                Weight = 1,
                Message = $"SR2.11 部分符合：{string.Join("；", messages)}。"
            };
        }

        private static AuditItemResult Pass(string msg, int score) => new()
        {
            ItemKey = "system_event.time_sync",
            Title = "時戳 / 時間同步檢查（SR2.11）",
            Score = score,
            Weight = 1,
            Message = msg
        };

        private static AuditItemResult Fail(string reason) => new()
        {
            ItemKey = "system_event.time_sync",
            Title = "時戳 / 時間同步檢查（SR2.11）",
            Score = 0,
            Weight = 1,
            Message = reason
        };
    }
}

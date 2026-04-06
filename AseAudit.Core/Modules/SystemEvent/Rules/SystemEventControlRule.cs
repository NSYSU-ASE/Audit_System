using System;
using System.Collections.Generic;
using System.Linq;
using ASEAudit.Shared.Scoring;
using AseAudit.Core.Modules.SystemEvent.Dtos;

using System.Text;
using System.Threading.Tasks;
namespace AseAudit.Core.Modules.SystemEvent.Rules
{
    /// <summary>
    /// 系統事件模組 - 使用控制 / 事件紀錄檢查
    /// 對應：
    /// SR2.5, SR2.8, SR2.9, SR2.10
    ///
    /// 這個模組目前是依照單一流程圖直接計算總分，
    /// 不另外拆成多個 Rule 後再做平均。
    /// </summary>
    public sealed class SystemEventControlRule
    {
        public AuditItemResult Evaluate(
            string deviceId,
            IEnumerable<SystemEventSnapshotDto> rows)
        {
            deviceId = (deviceId ?? "").Trim();

            if (string.IsNullOrWhiteSpace(deviceId))
            {
                return Fail("未提供 deviceId，無法執行系統事件模組稽核。");
            }

            var row = (rows ?? Enumerable.Empty<SystemEventSnapshotDto>())
                .FirstOrDefault(x =>
                    string.Equals((x.DeviceId ?? "").Trim(), deviceId, StringComparison.OrdinalIgnoreCase));

            if (row is null)
            {
                return Fail("找不到該設備的系統事件模組資料。");
            }

            var messages = new List<string>();

            // =========================
            // SR2.8：可稽核事件（前置條件）
            // Alarm File 正常記錄 + Windows事件紀錄正常記錄
            // 若不成立，依流程圖直接 0 分
            // =========================
            bool alarmOk =
                row.AlarmLog is not null &&
                row.AlarmLog.AlarmFileEnabled &&
                row.AlarmLog.AlarmFileIsRecordingNormally;

            bool windowsOk =
                row.AlarmLog is not null &&
                row.AlarmLog.WindowsEventEnabled &&
                row.AlarmLog.WindowsEventIsRecordingNormally;

            if (!alarmOk || !windowsOk)
            {
                if (!alarmOk && !windowsOk)
                {
                    messages.Add("SR2.8 未通過：Alarm File 與 Windows 事件紀錄皆無法正常記錄。");
                }
                else if (!alarmOk)
                {
                    messages.Add("SR2.8 未通過：Alarm File 未啟用或未正常記錄。");
                }
                else
                {
                    messages.Add("SR2.8 未通過：Windows 事件紀錄未啟用或未正常記錄。");
                }

                return new AuditItemResult
                {
                    ItemKey = "system_event.control",
                    Title = "系統事件模組-使用控制 / 事件紀錄檢查",
                    Score = 0,
                    Weight = 1,
                    Message = string.Join("；", messages)
                };
            }

            messages.Add("SR2.8 通過：Alarm File 與 Windows 事件紀錄皆正常。");

            // =========================
            // 上半支線：SR2.9 + SR2.10
            //
            // 流程圖對應：
            // 1. 是否開啟空間不足之警告
            //    否 -> 10
            //    是 -> 往下看
            //
            // 2. 一段時間後是否還有空間不足之警告
            //    無相同警告 -> 50
            //    未處理 -> 30
            //
            // 目前依 DTO 對應方式：
            // - DiskSpaceAlertEnabled = 是否開啟空間不足警告
            // - HasResponseProcedure / HasExecutionRecord = 是否具備處理能力
            //
            // 因為 DTO 目前沒有「一段時間後是否還有相同警告」這個欄位，
            // 先用下列方式近似：
            //
            // A. 未開啟警告 -> 10
            // B. 已開啟警告，且有處理流程 + 有執行紀錄 -> 50
            // C. 已開啟警告，但流程或執行不完整 -> 30
            // =========================
            int upperBranchScore = 0;

            bool diskAlertEnabled =
                row.StorageMonitoring is not null &&
                row.StorageMonitoring.DiskSpaceAlertEnabled;

            bool hasNotificationMechanism =
                row.StorageMonitoring is not null &&
                row.StorageMonitoring.HasNotificationMechanism;

            bool hasResponseProcedure =
                row.ResponseHandling is not null &&
                row.ResponseHandling.HasResponseProcedure;

            bool hasExecutionRecord =
                row.ResponseHandling is not null &&
                row.ResponseHandling.HasExecutionRecord;

            if (!diskAlertEnabled)
            {
                upperBranchScore = 10;
                messages.Add("SR2.9：未開啟磁碟空間不足警告，上半支線得 10 分。");
            }
            else
            {
                if (hasResponseProcedure && hasExecutionRecord)
                {
                    upperBranchScore = 50;
                    messages.Add("SR2.9 / SR2.10：已開啟磁碟空間不足警告，且具備處理流程與執行紀錄，上半支線得 50 分。");
                }
                else
                {
                    upperBranchScore = 30;

                    if (!hasNotificationMechanism)
                    {
                        messages.Add("SR2.9：已開啟磁碟空間不足警告，但通知機制不足。");
                    }

                    if (!hasResponseProcedure && !hasExecutionRecord)
                    {
                        messages.Add("SR2.10：已有警告機制，但無處理流程且無執行紀錄，上半支線得 30 分。");
                    }
                    else if (!hasResponseProcedure)
                    {
                        messages.Add("SR2.10：已有警告機制，但缺少處理流程文件，上半支線得 30 分。");
                    }
                    else
                    {
                        messages.Add("SR2.10：已有警告機制與處理流程，但缺少執行紀錄，上半支線得 30 分。");
                    }
                }
            }

            // =========================
            // 下半支線：SR2.5 會期鎖 / 自動登出
            //
            // 流程圖對應：
            // 是否有列出自動登出
            // 是 -> 50
            // 否 -> 10
            //
            // DTO 對應：
            // - FeatureAvailable
            // - HasScreenSaverLock
            // - HasAutoLogout
            // - IdleTimeoutMinutes
            //
            // 目前先採：
            // 只要設備具備功能，且有螢幕保護或自動登出其中一個，就算成立
            // =========================
            int lowerBranchScore = 0;

            bool sessionFeatureAvailable =
                row.SessionControl is not null &&
                row.SessionControl.FeatureAvailable;

            bool hasSessionControl =
                row.SessionControl is not null &&
                (row.SessionControl.HasScreenSaverLock || row.SessionControl.HasAutoLogout);

            if (sessionFeatureAvailable && hasSessionControl)
            {
                lowerBranchScore = 50;

                if (row.SessionControl!.IdleTimeoutMinutes.HasValue)
                {
                    messages.Add($"SR2.5：已設定螢幕保護或自動登出，閒置時間 {row.SessionControl.IdleTimeoutMinutes.Value} 分鐘，下半支線得 50 分。");
                }
                else
                {
                    messages.Add("SR2.5：已設定螢幕保護或自動登出，下半支線得 50 分。");
                }
            }
            else
            {
                lowerBranchScore = 10;

                if (!sessionFeatureAvailable)
                {
                    messages.Add("SR2.5：設備不具備會期鎖 / 自動登出功能，下半支線得 10 分。");
                }
                else
                {
                    messages.Add("SR2.5：未設定螢幕保護或自動登出，下半支線得 10 分。");
                }
            }

            // =========================
            // 總分
            // =========================
            int totalScore = upperBranchScore + lowerBranchScore;

            return new AuditItemResult
            {
                ItemKey = "system_event.control",
                Title = "系統事件模組-使用控制 / 事件紀錄檢查",
                Score = totalScore,
                Weight = 1,
                Message = string.Join("；", messages)
            };
        }

        private static AuditItemResult Fail(string reason)
            => new AuditItemResult
            {
                ItemKey = "system_event.control",
                Title = "系統事件模組-使用控制 / 事件紀錄檢查",
                Score = 0,
                Weight = 1,
                Message = reason
            };
    }
}
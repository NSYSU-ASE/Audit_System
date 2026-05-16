using System;
using System.Collections.Generic;
using System.Linq;
using ASEAudit.Shared.Scoring;
using AseAudit.Core.Modules.SoftwareRecognition.Dtos;

namespace AseAudit.Core.Modules.SoftwareRecognition.Rules
{
    /// <summary>
    /// SR2.3 / SR2.4 IP Guard 端點控管檢查。
    /// </summary>
    public sealed class EndpointControlRule
    {
        public AuditItemResult EvaluatePortableDeviceControl(
            string deviceId,
            IEnumerable<IpGuardStatusRecordDto> ipGuardRows,
            IEnumerable<AntivirusStatusRecordDto> antivirusRows)
        {
            return Evaluate(
                deviceId,
                ipGuardRows,
                antivirusRows,
                "software.portable_device_control",
                "可攜式裝置及行動裝置使用控制（SR2.3）",
                ipGuard => ipGuard.PortableDeviceControlEnabled,
                "可攜式/行動裝置使用控制");
        }

        public AuditItemResult EvaluateMobileCodeControl(
            string deviceId,
            IEnumerable<IpGuardStatusRecordDto> ipGuardRows,
            IEnumerable<AntivirusStatusRecordDto> antivirusRows)
        {
            return Evaluate(
                deviceId,
                ipGuardRows,
                antivirusRows,
                "software.mobile_code_control",
                "行動程式碼使用限制（SR2.4）",
                ipGuard => ipGuard.MobileCodeControlEnabled,
                "行動程式碼執行限制");
        }

        private static AuditItemResult Evaluate(
            string deviceId,
            IEnumerable<IpGuardStatusRecordDto> ipGuardRows,
            IEnumerable<AntivirusStatusRecordDto> antivirusRows,
            string itemKey,
            string title,
            Func<IpGuardStatusRecordDto, bool> isRequiredControlEnabled,
            string controlName)
        {
            deviceId = (deviceId ?? "").Trim();
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                return BuildResult(itemKey, title, 0, "未提供 deviceId，無法查詢 IP Guard 與防毒狀態。");
            }

            var ipGuard = (ipGuardRows ?? Enumerable.Empty<IpGuardStatusRecordDto>())
                .FirstOrDefault(x => IsSameDevice(x.DeviceId, deviceId));

            var antivirus = (antivirusRows ?? Enumerable.Empty<AntivirusStatusRecordDto>())
                .FirstOrDefault(x => IsSameDevice(x.DeviceId, deviceId));

            var messages = new List<string>();

            var hasIpGuardData = ipGuard is not null;
            var ipGuardInstalled = ipGuard?.IsInstalled == true;
            var ipGuardConnected = ipGuard?.IsConnected == true;
            var ipGuardStatusOk = IsStatusOk(ipGuard?.Status);
            var controlEnabled = ipGuard is not null && isRequiredControlEnabled(ipGuard);
            var ipGuardOk = ipGuardInstalled && ipGuardConnected && ipGuardStatusOk && controlEnabled;

            if (!hasIpGuardData)
            {
                messages.Add("IP Guard 資料庫中找不到該機台紀錄");
            }
            else
            {
                messages.Add(ipGuardInstalled ? "IP Guard 已安裝" : "IP Guard 未安裝");
                messages.Add(ipGuardConnected ? "IP Guard 連線正常" : "IP Guard 未連線或連線異常");
                messages.Add(ipGuardStatusOk ? "IP Guard 狀態正常" : $"IP Guard 狀態異常：{ipGuard?.Status ?? "Unknown"}");
                messages.Add(controlEnabled ? $"{controlName}已啟用" : $"{controlName}未啟用");
            }

            var hasAntivirusData = antivirus is not null;
            var antivirusInstalled = antivirus?.IsInstalled == true;
            var antivirusStatusOk = IsStatusOk(antivirus?.Status);
            var definitionOk = IsDefinitionOk(antivirus);
            var antivirusOk = antivirusInstalled && antivirusStatusOk && definitionOk;

            if (!hasAntivirusData)
            {
                messages.Add("防毒資料庫中找不到該機台紀錄");
            }
            else
            {
                messages.Add(antivirusInstalled ? "防毒軟體已安裝" : "防毒軟體未安裝");
                messages.Add(antivirusStatusOk ? "防毒狀態正常" : $"防毒狀態異常：{antivirus?.Status ?? "Unknown"}");
                messages.Add(definitionOk ? "病毒碼更新狀態正常" : "病毒碼更新狀態異常或未更新");
            }

            var score = CalculateScore(ipGuardOk, antivirusOk, hasIpGuardData || hasAntivirusData);
            var summary = score switch
            {
                100 => $"{controlName}、IP Guard 連線與防毒保護皆正常。",
                50 => $"{controlName}、IP Guard 或防毒保護僅部分符合。",
                _ => $"未具備{controlName}所需的 IP Guard / 防毒保護能力。"
            };

            messages.Insert(0, summary);
            return BuildResult(itemKey, title, score, string.Join("；", messages));
        }

        private static double CalculateScore(bool ipGuardOk, bool antivirusOk, bool hasAnyData)
        {
            if (ipGuardOk && antivirusOk) return 100;
            if (hasAnyData && (ipGuardOk || antivirusOk)) return 50;
            return 0;
        }

        private static bool IsSameDevice(string? source, string target)
            => string.Equals((source ?? "").Trim(), target, StringComparison.OrdinalIgnoreCase);

        private static bool IsStatusOk(string? status)
        {
            if (string.IsNullOrWhiteSpace(status)) return true;

            var s = status.Trim();
            return s.Equals("OK", StringComparison.OrdinalIgnoreCase)
                || s.Equals("Healthy", StringComparison.OrdinalIgnoreCase)
                || s.Equals("Running", StringComparison.OrdinalIgnoreCase)
                || s.Equals("Connected", StringComparison.OrdinalIgnoreCase)
                || s.Equals("Normal", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsDefinitionOk(AntivirusStatusRecordDto? antivirus)
        {
            if (antivirus is null) return false;
            return antivirus.IsDefinitionUpdated ?? true;
        }

        private static AuditItemResult BuildResult(string itemKey, string title, double score, string message)
            => new AuditItemResult
            {
                ItemKey = itemKey,
                Title = title,
                Score = score,
                Weight = 1,
                Passed = score >= 100,
                Message = message
            };
    }
}

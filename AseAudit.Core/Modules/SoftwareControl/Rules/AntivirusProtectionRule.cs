using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASEAudit.Shared.Scoring;
using AseAudit.Core.Modules.SoftwareRecognition.Dtos;

namespace AseAudit.Core.Modules.SoftwareRecognition.Rules
{
    /// <summary>
    /// SR6.2 防毒檢查：
    /// - 未安裝 or 狀態異常 or 版本非最新 => 0
    /// - 已安裝且版本符合最新基準 => 100
    /// </summary>
    public sealed class AntivirusProtectionRule
    {
        public AuditItemResult Evaluate(
            string deviceId,
            IEnumerable<AntivirusStatusRecordDto> antivirusDbRows,
            AntivirusBaselineDto baseline)
        {
            deviceId = (deviceId ?? "").Trim();

            if (string.IsNullOrWhiteSpace(deviceId))
            {
                return Fail("未提供 deviceId，無法查詢防毒狀態。");
            }

            if (baseline is null || string.IsNullOrWhiteSpace(baseline.LatestVersion))
            {
                // ✅ 最新版必須由他們提供，沒提供就視為無法稽核
                return new AuditItemResult
                {
                    ItemKey = "software.antivirus",
                    Title = "防毒軟體版本/狀態檢查（SR6.2）",
                    Score = 0,
                    Weight = 1,
                    Message = "未提供防毒最新版本基準（由對方維護輸入），無法判定是否為最新版本。"
                };
            }

            var row = (antivirusDbRows ?? Enumerable.Empty<AntivirusStatusRecordDto>())
                .FirstOrDefault(x => string.Equals((x.DeviceId ?? "").Trim(), deviceId, StringComparison.OrdinalIgnoreCase));

            if (row is null)
            {
                return Fail("防毒資料庫中找不到該機台紀錄。");
            }

            if (row.IsInstalled == false)
            {
                return Fail("未安裝防毒軟體。");
            }

            // 可選：狀態異常就 0（你可以依 DB 狀態值調整）
            if (!IsStatusOk(row.Status))
            {
                return Fail($"防毒狀態異常：{row.Status ?? "Unknown"}");
            }

            // 版本檢查：版本欄位名稱你忘了，會在 DTO 那邊改
            var installedVer = (row.InstalledVersion ?? "").Trim();
            if (string.IsNullOrWhiteSpace(installedVer))
            {
                return Fail("未取得防毒版本欄位，無法判定是否為最新版本。");
            }

            var latestVer = baseline.LatestVersion.Trim();

            var versionOk = IsVersionAcceptable(installedVer, baseline);

            if (!versionOk)
            {
                return new AuditItemResult
                {
                    ItemKey = "software.antivirus",
                    Title = "防毒軟體版本/狀態檢查（SR6.2）",
                    Score = 0,
                    Weight = 1,
                    Message = $"防毒版本非最新或不符合基準。已裝版本：{installedVer}；基準最新版：{latestVer}"
                };
            }

            return new AuditItemResult
            {
                ItemKey = "software.antivirus",
                Title = "防毒軟體版本/狀態檢查（SR6.2）",
                Score = 100,
                Weight = 1,
                Message = $"防毒已安裝且符合版本基準（已裝：{installedVer}；基準：{latestVer}）。"
            };
        }

        private static AuditItemResult Fail(string reason)
            => new AuditItemResult
            {
                ItemKey = "software.antivirus",
                Title = "防毒軟體版本/狀態檢查（SR6.2）",
                Score = 0,
                Weight = 1,
                Message = reason
            };

        private static bool IsStatusOk(string? status)
        {
            if (string.IsNullOrWhiteSpace(status)) return true; // 沒狀態欄位就先放行
            var s = status.Trim();

            // TODO: 若他們 DB 有固定狀態碼，請在這裡調整判斷規則
            // 例如：OK/Healthy/Running 代表正常，其它代表異常
            return s.Equals("OK", StringComparison.OrdinalIgnoreCase)
                || s.Equals("Healthy", StringComparison.OrdinalIgnoreCase)
                || s.Equals("Running", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsVersionAcceptable(string installedVer, AntivirusBaselineDto baseline)
        {
            // 如果對方用「允許版本白名單」
            if (baseline.AllowedVersions is not null && baseline.AllowedVersions.Count > 0)
            {
                return baseline.AllowedVersions.Any(v =>
                    string.Equals(v?.Trim(), installedVer.Trim(), StringComparison.OrdinalIgnoreCase));
            }

            // 否則用 LatestVersion 單一基準
            if (baseline.AllowGreaterOrEqual)
            {
                // 版本比較：這裡用 System.Version 嘗試解析
                // TODO: 若版本格式不是 x.y.z 這種（例如帶字母），改成你們的比較方式（字串相等或自訂 parser）
                if (Version.TryParse(installedVer, out var installed) &&
                    Version.TryParse(baseline.LatestVersion, out var latest))
                {
                    return installed >= latest;
                }

                // 解析失敗就退回「至少要相等」
                return string.Equals(installedVer.Trim(), baseline.LatestVersion.Trim(), StringComparison.OrdinalIgnoreCase);
            }

            // 嚴格：必須完全等於最新版
            return string.Equals(installedVer.Trim(), baseline.LatestVersion.Trim(), StringComparison.OrdinalIgnoreCase);
        }
    }
}

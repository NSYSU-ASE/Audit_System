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

            if (baseline is null || !HasVersionBaseline(baseline))
            {
                // ✅ 最新版必須由他們提供，沒提供就視為無法稽核
                return new AuditItemResult
                {
                    ItemKey = "software.antivirus",
                    Title = "防毒軟體版本/狀態檢查（SR6.2）",
                    Score = 0,
                    Weight = 1,
                    Message = "未提供防毒版本基準（LatestVersion 或 AllowedVersions 由對方維護輸入），無法判定是否符合版本要求。"
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
                return Fail(BuildDeviceMessage(deviceId, row, "未安裝防毒軟體。"));
            }

            // 可選：狀態異常就 0（你可以依 DB 狀態值調整）
            if (!IsStatusOk(row.Status))
            {
                return Fail(BuildDeviceMessage(deviceId, row, $"防毒狀態異常：{row.Status ?? "Unknown"}"));
            }

            // 版本檢查：版本欄位名稱你忘了，會在 DTO 那邊改
            var installedVer = (row.InstalledVersion ?? "").Trim();
            if (string.IsNullOrWhiteSpace(installedVer))
            {
                return Fail(BuildDeviceMessage(deviceId, row, "未取得防毒版本欄位，無法判定是否符合版本要求。"));
            }

            var versionOk = IsVersionAcceptable(installedVer, baseline);

            if (!versionOk)
            {
                return new AuditItemResult
                {
                    ItemKey = "software.antivirus",
                    Title = "防毒軟體版本/狀態檢查（SR6.2）",
                    Score = 0,
                    Weight = 1,
                    Message = BuildDeviceMessage(deviceId, row, $"防毒版本不符合基準。{BuildVersionBaselineMessage(installedVer, baseline)}")
                };
            }

            var definitionCheck = CheckDefinition(row, baseline);
            if (!definitionCheck.IsAcceptable)
            {
                return new AuditItemResult
                {
                    ItemKey = "software.antivirus",
                    Title = "防毒軟體版本/狀態檢查（SR6.2）",
                    Score = 0,
                    Weight = 1,
                    Message = BuildDeviceMessage(deviceId, row, definitionCheck.Message)
                };
            }

            return new AuditItemResult
            {
                ItemKey = "software.antivirus",
                Title = "防毒軟體版本/狀態檢查（SR6.2）",
                Score = 100,
                Weight = 1,
                Passed = true,
                Message = BuildDeviceMessage(
                    deviceId,
                    row,
                    $"防毒已安裝、狀態正常且符合版本基準。{BuildVersionBaselineMessage(installedVer, baseline)}；{definitionCheck.Message}")
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
                || s.Equals("Running", StringComparison.OrdinalIgnoreCase)
                || s.Equals("Normal", StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasVersionBaseline(AntivirusBaselineDto baseline)
            => !string.IsNullOrWhiteSpace(baseline.LatestVersion)
                || (baseline.AllowedVersions is not null && baseline.AllowedVersions.Any(v => !string.IsNullOrWhiteSpace(v)));

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

        private static DefinitionCheckResult CheckDefinition(
            AntivirusStatusRecordDto row,
            AntivirusBaselineDto baseline)
        {
            if (row.IsDefinitionUpdated == false)
            {
                return new DefinitionCheckResult(false, "病毒碼更新狀態異常或未更新。");
            }

            if (!string.IsNullOrWhiteSpace(baseline.LatestDefinitionVersion))
            {
                var installedDefinition = (row.DefinitionVersion ?? "").Trim();
                if (string.IsNullOrWhiteSpace(installedDefinition))
                {
                    return new DefinitionCheckResult(false, "未取得病毒碼版本，無法比對病毒碼基準。");
                }

                if (!string.Equals(installedDefinition, baseline.LatestDefinitionVersion.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return new DefinitionCheckResult(
                        false,
                        $"病毒碼版本不符合基準。已裝病毒碼：{installedDefinition}；基準病毒碼：{baseline.LatestDefinitionVersion.Trim()}。");
                }

                return new DefinitionCheckResult(true, $"病毒碼版本符合基準（已裝：{installedDefinition}；基準：{baseline.LatestDefinitionVersion.Trim()}）。");
            }

            if (baseline.MaxDefinitionAgeDays is > 0)
            {
                if (row.DefinitionUpdatedAt is null)
                {
                    return new DefinitionCheckResult(false, "未取得病毒碼更新時間，無法判定是否在允許天數內。");
                }

                var newestAllowedDate = DateTime.UtcNow.Date.AddDays(-baseline.MaxDefinitionAgeDays.Value);
                if (row.DefinitionUpdatedAt.Value.ToUniversalTime().Date < newestAllowedDate)
                {
                    return new DefinitionCheckResult(
                        false,
                        $"病毒碼更新時間過舊。最後更新：{row.DefinitionUpdatedAt.Value:yyyy-MM-dd}；允許天數：{baseline.MaxDefinitionAgeDays.Value} 天。");
                }

                return new DefinitionCheckResult(
                    true,
                    $"病毒碼更新時間符合基準（最後更新：{row.DefinitionUpdatedAt.Value:yyyy-MM-dd}；允許天數：{baseline.MaxDefinitionAgeDays.Value} 天）。");
            }

            if (row.IsDefinitionUpdated == true)
            {
                return new DefinitionCheckResult(true, "病毒碼更新狀態正常。");
            }

            return new DefinitionCheckResult(true, "未提供病毒碼基準或更新狀態，本次僅檢查防毒安裝、狀態與版本。");
        }

        private static string BuildVersionBaselineMessage(string installedVer, AntivirusBaselineDto baseline)
        {
            if (baseline.AllowedVersions is not null && baseline.AllowedVersions.Count > 0)
            {
                var allowed = string.Join(", ", baseline.AllowedVersions.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v.Trim()));
                return $"已裝版本：{installedVer}；允許版本：{allowed}";
            }

            return $"已裝版本：{installedVer}；基準最新版：{baseline.LatestVersion.Trim()}；比較方式：{(baseline.AllowGreaterOrEqual ? "大於或等於基準" : "必須完全相等")}";
        }

        private static string BuildDeviceMessage(string deviceId, AntivirusStatusRecordDto? row, string reason)
        {
            var productName = string.IsNullOrWhiteSpace(row?.ProductName) ? "Unknown" : row.ProductName!.Trim();
            var status = string.IsNullOrWhiteSpace(row?.Status) ? "Unknown" : row.Status!.Trim();
            return $"DeviceId：{deviceId}；產品：{productName}；狀態：{status}；{reason}";
        }

        private sealed record DefinitionCheckResult(bool IsAcceptable, string Message);
    }
}

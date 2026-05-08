using Microsoft.AspNetCore.Mvc;
using AseAudit.Infrastructure.Repositories;
using AseAudit.Infrastructure.Mapping;
using AseAudit.Core.Modules.Identity.Rules;
using AseAudit.Core.Modules.Identity.Dtos;
using ASEAudit.Shared.Scoring;

namespace AseAudit.Api.Controllers;

[ApiController]
[Route("api/identity")]
public class IdentityController : ControllerBase
{
    private readonly IdentityRepository _repo;

    public IdentityController(IdentityRepository repo)
    {
        _repo = repo;
    }

    [HttpGet("accounts")]
    public IActionResult GetAccounts()
    {
        return Ok(_repo.GetAccounts());
    }

    [HttpGet("rules")]
    public IActionResult GetRules()
    {
        return Ok(_repo.GetRules());
    }

    [HttpGet("host-account-snapshots")]
    public IActionResult GetHostAccountSnapshots()
    {
        var data = _repo.GetAccounts()
            .Select(x => x.ToHostAccountSnapshotDto());

        return Ok(data);
    }

    [HttpGet("host-identity-snapshots")]
    public IActionResult GetHostIdentitySnapshots()
    {
        var data = _repo.GetAccounts()
            .Select(x => x.ToHostIdentitySnapshotDto());

        return Ok(data);
    }

    [HttpGet("employee-directory")]
    public IActionResult GetEmployeeDirectory()
    {
        var data = _repo.GetAccounts()
            .Select(x => x.ToEmployeeDirectoryRecordDto());

        return Ok(data);
    }

    [HttpGet("password-policy")]
    public IActionResult GetPasswordPolicy()
    {
        var data = _repo.GetRules()
            .Select(x => x.ToPasswordPolicySnapshotDto());

        return Ok(data);
    }

    [HttpGet("audit")]
    public IActionResult Audit()
    {
        var results = RunIdentityRules();

        var totalScore = results.Count == 0
            ? 0
            : Math.Round(results.Average(x => x.Score), 2);

        return Ok(new
        {
            module = "Identity",
            totalScore,
            itemCount = results.Count,
            results
        });
    }

    [HttpGet("audit-device-summary")]
    public IActionResult GetAuditDeviceSummary()
    {
        return Ok(BuildDeviceSummaries());
    }

    [HttpPost("audit-zone-k18")]
    public IActionResult AuditZoneK18(
        [FromQuery] string period = "2025-12",
        [FromQuery] string buildingCode = "K18",
        [FromQuery] string? deviceId = null)
    {
        var summaries = BuildDeviceSummaries();

        foreach (var summary in summaries.Where(x => !string.IsNullOrWhiteSpace(x.HostName)))
        {
            var target = _repo.UpsertIdentityDevice(buildingCode, summary.HostName);
            _repo.UpsertIdentityAuditResult(period, target, summary.Rules);
        }

        return Ok(BuildDetailReportFromDb(period, buildingCode, deviceId));
    }

    [HttpGet("detail-report")]
    public IActionResult GetDetailReport(
        [FromQuery] string period = "2025-12",
        [FromQuery] string buildingCode = "K18",
        [FromQuery] string? deviceId = null)
    {
        return Ok(BuildDetailReportFromDb(period, buildingCode, deviceId));
    }

    private List<IdentityAuditDeviceSummaryDto> BuildDeviceSummaries()
    {
        var accounts = _repo.GetAccounts().ToList();
        var ruleRecords = _repo.GetRules().ToList();

        var result = new List<IdentityAuditDeviceSummaryDto>();

        var devices = accounts.GroupBy(x => x.HostName);

        foreach (var device in devices)
        {
            var hostName = device.Key;

            var hostAccountDtos = device
                .Select(x => x.ToHostAccountSnapshotDto())
                .ToList();

            var hostIdentityDtos = device
                .Select(x => x.ToHostIdentitySnapshotDto())
                .ToList();

            var employeeDtos = device
                .Select(x => x.ToEmployeeDirectoryRecordDto())
                .ToList();

            var passwordDtos = ruleRecords
                .Where(x => x.HostName == hostName)
                .Select(x => x.ToPasswordPolicySnapshotDto())
                .ToList();

            var rules = new List<IdentityRuleResultDto>();

            var adRule = new AdAccountProtectionRule();
            var employeeRule = new EmployeeDirectoryProtectionRule();
            var passwordRule = new PasswordPolicyRule();

            var adResults = hostAccountDtos
                .Select(x => adRule.Evaluate(x))
                .ToList();
            var adScoreList = adResults
                .Select(x => x.Score)
                .ToList();

            rules.Add(ToRuleRow(
                srMapping: "SR1.1 / SR1.1RE(1)",
                ruleName: "AD 管理與帳號識別",
                results: adResults,
                detailScores: adScoreList
            ));

            var employeeResults = hostIdentityDtos
                .Select(x => employeeRule.Evaluate(x, employeeDtos))
                .ToList();

            rules.Add(ToRuleRow(
                srMapping: "SR1.3 / SR1.4",
                ruleName: "AD 帳號比對黃頁",
                results: employeeResults
            ));

            var passwordResults = passwordDtos
                .Select(x => passwordRule.Evaluate(x))
                .ToList();

            rules.Add(ToRuleRow(
                srMapping: "SR1.5 / SR1.7 / SR1.11",
                ruleName: "密碼政策 / 嘗試限制 / ALM log",
                results: passwordResults
            ));

            rules.Add(NoDataRule(
                srMapping: "SR1.10",
                ruleName: "鑑別回饋"
            ));

            rules.Add(NoDataRule(
                srMapping: "SR1.12",
                ruleName: "系統使用通知"
            ));

            rules.Add(NoDataRule(
                srMapping: "SR2.1 / SR2.1RE(1) / SR2.1RE(2)",
                ruleName: "使用者群組權限"
            ));

            rules.Add(NoDataRule(
                srMapping: "Manual Review",
                ruleName: "人工審查項目"
            ));

            var includedRules = rules
                .Where(x => x.IncludedInScore && x.Score.HasValue)
                .ToList();

            var averageScore = includedRules.Count == 0
                ? 0
                : Math.Round(includedRules.Average(x => x.Score!.Value), 2);

            result.Add(new IdentityAuditDeviceSummaryDto
            {
                HostName = hostName,
                AverageScore = averageScore,
                RiskLevel = GetRiskLevel(averageScore),
                IncludedRuleCount = includedRules.Count,
                TotalRuleCount = rules.Count,
                Rules = rules
            });
        }

        return result;
    }

    private List<AuditItemResult> RunIdentityRules()
    {
        var results = new List<AuditItemResult>();

        var accounts = _repo.GetAccounts().ToList();
        var ruleRecords = _repo.GetRules().ToList();

        var hostAccountDtos = accounts
            .Select(x => x.ToHostAccountSnapshotDto())
            .ToList();

        var hostIdentityDtos = accounts
            .Select(x => x.ToHostIdentitySnapshotDto())
            .ToList();

        var employeeDtos = accounts
            .Select(x => x.ToEmployeeDirectoryRecordDto())
            .ToList();

        var passwordPolicyDtos = ruleRecords
            .Select(x => x.ToPasswordPolicySnapshotDto())
            .ToList();

        var adRule = new AdAccountProtectionRule();
        var employeeRule = new EmployeeDirectoryProtectionRule();
        var passwordRule = new PasswordPolicyRule();

        foreach (var dto in hostAccountDtos)
        {
            results.Add(adRule.Evaluate(dto));
        }

        foreach (var dto in hostIdentityDtos)
        {
            results.Add(employeeRule.Evaluate(dto, employeeDtos));
        }

        foreach (var dto in passwordPolicyDtos)
        {
            results.Add(passwordRule.Evaluate(dto));
        }

        return results;
    }

    private static IdentityRuleResultDto ToRuleRow(
        string srMapping,
        string ruleName,
        List<AuditItemResult> results,
        List<double>? detailScores = null)
    {
        if (results.Count == 0)
        {
            return NoDataRule(srMapping, ruleName);
        }

        var score = Math.Round(results.Average(x => x.Score), 2);

        var issues = results
            .Where(x => x.Passed == false)
            .Select(x => x.Message)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        if (score < 80 && issues.Count == 0)
        {
            issues.Add("此項目未達標準，但未取得詳細扣分原因");
        }

        return new IdentityRuleResultDto
        {
            SrMapping = srMapping,
            RuleName = ruleName,
            Score = score,
            IncludedInScore = true,
            Status = score >= 80 ? "通過" : "未通過",
            Issues = issues,
            DetailScores = detailScores ?? new List<double>() 
        };
    }

    private static IdentityRuleResultDto NoDataRule(
        string srMapping,
        string ruleName)
    {
        return new IdentityRuleResultDto
        {
            SrMapping = srMapping,
            RuleName = ruleName,
            Score = null,
            IncludedInScore = false,
            Status = "未接入資料",

            Issues = new List<string>
        {
            "目前尚未取得此控制項資料，因此不參與本次平均分數計算"
        },

            DetailScores = new List<double>()
        };
    }

    private static string GetRiskLevel(double score)
    {
        if (score >= 80) return "低風險";
        if (score >= 70) return "中風險";
        return "高風險";
    }

    private static List<IdentityRuleResultDto> AggregateRulesForTarget(
        List<IdentityAuditDeviceSummaryDto> summaries)
    {
        var allRules = summaries
            .SelectMany(x => x.Rules)
            .ToList();

        var templateOrder = new[]
        {
            ("SR1.1 / SR1.1RE(1)", "AD 管理與帳號識別"),
            ("SR1.3 / SR1.4", "AD 帳號比對黃頁"),
            ("SR1.5 / SR1.7 / SR1.11", "密碼政策 / 嘗試限制 / ALM log"),
            ("SR1.10", "鑑別回饋"),
            ("SR1.12", "系統使用通知"),
            ("SR2.1 / SR2.1RE(1) / SR2.1RE(2)", "使用者群組權限"),
            ("Manual Review", "人工審查項目")
        };

        return templateOrder.Select(template =>
        {
            var rules = allRules
                .Where(x => x.SrMapping == template.Item1 && x.RuleName == template.Item2)
                .ToList();

            if (rules.Count == 0)
            {
                return NoDataRule(template.Item1, template.Item2);
            }

            var scoredRules = rules
                .Where(x => x.IncludedInScore && x.Score.HasValue)
                .ToList();

            if (scoredRules.Count == 0)
            {
                return NoDataRule(template.Item1, template.Item2);
            }

            var score = Math.Round(scoredRules.Average(x => x.Score!.Value), 2);
            var issues = scoredRules
                .SelectMany(x => x.Issues)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            if (score < 80 && issues.Count == 0)
            {
                issues.Add("此項目未達標準，但未取得詳細扣分原因");
            }

            var detailScores = scoredRules
                .SelectMany(x => x.DetailScores.Count > 0 ? x.DetailScores : new List<double> { x.Score!.Value })
                .ToList();

            return new IdentityRuleResultDto
            {
                SrMapping = template.Item1,
                RuleName = template.Item2,
                Score = score,
                IncludedInScore = true,
                Status = score >= 80 ? "通過" : "未通過",
                Issues = issues,
                DetailScores = detailScores
            };
        }).ToList();
    }

    private object BuildDetailReportFromDb(string period, string buildingCode, string? deviceId = null)
    {
        var target = !string.IsNullOrWhiteSpace(deviceId)
            ? _repo.GetTargetDeviceByDeviceId(deviceId) ?? _repo.GetTargetDevice(buildingCode)
            : _repo.GetTargetDevice(buildingCode);

        var result = _repo.GetAuditResult(period, target.DeviceId);
        var findings = result is null
            ? new List<AuditFindingRecord>()
            : _repo.GetAuditFindings(result.AuditResultId).ToList();

        var identityFindings = findings
            .Where(x => x.FRCode.Equals("FR1", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var identityItems = identityFindings.Count == 0
            ? new List<object>
            {
                new
                {
                    sr = "FR1",
                    title = "身分識別檢核",
                    score = result?.FR1,
                    status = result is null ? "--" : result.FR1 >= 80 ? "通過" : "未通過",
                    reason = result is null
                        ? "--"
                        : result.FR1 >= 80
                            ? "此功能需求目前未列出缺失。"
                            : "此功能需求未達標準，但未取得詳細扣分原因。"
                }
            }
            : identityFindings.Select(row =>
            {
                var parsed = ParseFindingReason(row.Reason);
                return new
                {
                    sr = parsed.sr,
                    title = parsed.title,
                    score = result?.FR1,
                    status = result is null ? "--" : result.FR1 >= 80 ? "通過" : "未通過",
                    reason = parsed.reason
                };
            }).Cast<object>().ToList();

        var hasIdentitySource = !string.IsNullOrWhiteSpace(target.HostName)
            && BuildDeviceSummaries().Any(x => x.HostName == target.HostName);

        var frScores = result is null
            ? new double?[] { null, null, null, null, null, null, null }
            : new double?[]
            {
                result.FR1,
                hasIdentitySource && result.FR2 == 0 ? null : result.FR2,
                hasIdentitySource && result.FR3 == 0 ? null : result.FR3,
                hasIdentitySource && result.FR4 == 0 ? null : result.FR4,
                hasIdentitySource && result.FR5 == 0 ? null : result.FR5,
                hasIdentitySource && result.FR6 == 0 ? null : result.FR6,
                hasIdentitySource && result.FR7 == 0 ? null : result.FR7
            };

        var identityScore = result is null ? (double?)null : result.FR1;

        var modules = new List<object>
        {
            new
            {
                key = "Identity",
                name = "身分識別模組",
                score = identityScore,
                items = identityItems
            }
        };

        foreach (var module in PlaceholderModules())
        {
            modules.Add(new
            {
                key = module.key,
                name = module.name,
                score = (double?)null,
                items = new[]
                {
                    new
                    {
                        sr = "--",
                        title = "尚未接入資料",
                        score = (double?)null,
                        status = "--",
                        reason = "--"
                    }
                }
            });
        }

        var availableModuleScores = modules
            .Select(x => x.GetType().GetProperty("score")?.GetValue(x))
            .OfType<double>()
            .ToList();

        return new
        {
            period,
            target = new
            {
                areaCode = target.AreaCode,
                areaName = target.AreaName,
                buildingCode = target.BuildingCode,
                buildingName = target.BuildingName,
                deviceId = target.DeviceId,
                deviceType = target.DeviceType ?? "--",
                hostName = target.HostName ?? "--"
            },
            auditResultId = result?.AuditResultId,
            frScores,
            overallScore = availableModuleScores.Count == 0
                ? (double?)null
                : Math.Round(availableModuleScores.Average(), 2),
            modules
        };
    }

    private static (string sr, string title, string reason) ParseFindingReason(string reason)
    {
        var parts = reason.Split('｜', 3, StringSplitOptions.TrimEntries);
        if (parts.Length == 3)
        {
            return (parts[0], parts[1], parts[2]);
        }

        if (parts.Length == 2)
        {
            return (parts[0], "身分識別缺失", parts[1]);
        }

        return ("FR1", "身分識別缺失", reason);
    }

    private static IEnumerable<(string key, string name)> PlaceholderModules()
    {
        yield return ("SoftwareControl", "軟體識別模組");
        yield return ("Firewall", "防火牆模組");
        yield return ("DataManagement", "資料管理模組");
        yield return ("ResourceManagement", "資源管理模組");
        yield return ("SystemEvent", "系統事件模組");
        yield return ("ManualReview", "稽核流程模組");
    }
}

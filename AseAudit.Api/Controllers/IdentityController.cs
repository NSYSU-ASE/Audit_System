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

        return Ok(result);
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
        if (score >= 60) return "中風險";
        return "高風險";
    }
}
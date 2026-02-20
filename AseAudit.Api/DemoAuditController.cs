using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

using ASEAudit.Shared.Scoring;
using AseAudit.Core.Modules.Identity.Dtos;
using AseAudit.Core.Modules.Identity.Rules;

namespace AseAudit.Api.Controllers;

[ApiController]
[Route("api/demo")]
public class DemoAuditController : ControllerBase
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private string DemoRoot => Path.Combine(AppContext.BaseDirectory, "Asseet", "DemoData");

    // itemKey 對應你各 Rule 的 ItemKey（建議你統一用這些）
    // identity.host_account
    // identity.employee_directory
    // identity.password_policy
    // ui.error_feedback
    // ui.system_use_notice
    // identity.user_group
    [HttpGet("item/{itemKey}")]
    public IActionResult GetItem(string itemKey, [FromQuery] string @case = "good")
    {
        // 依 itemKey 決定要讀哪個 json、用哪個 dto、哪個 rule
        AuditItemResult result = itemKey switch
        {
            "identity.user_group" => Eval(
                fileName: $"user_group.{@case}.json",
                deserialize: (json) => JsonSerializer.Deserialize<UserGroupSnapshotDto>(json, _jsonOptions)!,
                evaluate: (dto) => new UserGroupProtectionRule().Evaluate(dto)
            ),

            "identity.password_policy" => Eval(
                fileName: $"password_policy.{@case}.json",
                deserialize: (json) => JsonSerializer.Deserialize<PasswordPolicySnapshotDto>(json, _jsonOptions)!,
                evaluate: (dto) => new PasswordPolicyRule().Evaluate(dto)
            ),

            "ui.error_feedback" => Eval(
                fileName: $"ui_errorfeedback{@caseSuffix(@case)}.json",
                deserialize: (json) => JsonSerializer.Deserialize<UiControlSnapshotDto>(json, _jsonOptions)!,
                evaluate: (dto) => new ErrorFeedbackRule().Evaluate(dto)
            ),

            "ui.system_use_notice" => Eval(
                fileName: $"ui_systemusenotice{@caseSuffix(@case)}.json",
                deserialize: (json) => JsonSerializer.Deserialize<UiControlSnapshotDto>(json, _jsonOptions)!,
                evaluate: (dto) => new SystemUseNoticeRule().Evaluate(dto)
            ),

            "identity.employee_directory" => Eval(
                fileName: "employee_directory.json",
                deserialize: (json) => JsonSerializer.Deserialize<EmployeeDirectoryRecordDto[]>(json, _jsonOptions)!,
                evaluate: (dto) => new EmployeeDirectoryProtectionRule().Evaluate(dto)
            ),

            "identity.host_account" => Eval(
                fileName: $"host_account.{@case}.json",
                deserialize: (json) => JsonSerializer.Deserialize<HostAccountSnapshotDto>(json, _jsonOptions)!,
                evaluate: (dto) => new AdAccountProtectionRule().Evaluate(dto)
            ),

            _ => new AuditItemResult
            {
                ItemKey = itemKey,
                Score = 0,
                Weight = 1,
                Passed = false,
                Title = "Demo",
                Message = $"未知的 itemKey: {itemKey}"
            }
        };

        return Ok(result);
    }

    [HttpGet("summary")]
    public IActionResult GetSummary([FromQuery] string @case = "good")
    {
        // 你目前的 Rule 清單（依你專案現況）
        var keys = new[]
        {
            "identity.host_account",
            "identity.employee_directory",
            "identity.password_policy",
            "ui.error_feedback",
            "ui.system_use_notice",
            "identity.user_group"
        };

        var results = new List<AuditItemResult>();

        foreach (var k in keys)
        {
            // 直接 call 同 controller 的方法邏輯
            var r = (GetItem(k, @case) as OkObjectResult)?.Value as AuditItemResult;
            if (r != null) results.Add(r);
        }

        // 加總 / 加權平均
        double totalWeight = results.Sum(r => r.Weight <= 0 ? 1 : r.Weight);
        double weightedAvg = results.Sum(r => r.Score * (r.Weight <= 0 ? 1 : r.Weight)) / (totalWeight == 0 ? 1 : totalWeight);

        return Ok(new
        {
            caseName = @case,
            totalItems = results.Count,
            weightedAverage = Math.Round(weightedAvg, 2),
            items = results.Select(r => new {
                r.ItemKey,
                r.Title,
                r.Score,
                r.Weight,
                r.Passed,
                r.Message,
                r.Detail
            })
        });
    }

    private static string @caseSuffix(string @case)
        => @case.Equals("good", StringComparison.OrdinalIgnoreCase) ? "_good"
         : @case.Equals("bad", StringComparison.OrdinalIgnoreCase) ? "_bad"
         : "_mid";

    private AuditItemResult Eval<TDto>(
        string fileName,
        Func<string, TDto> deserialize,
        Func<TDto, AuditItemResult> evaluate)
    {
        var path = Path.Combine(DemoRoot, fileName);

        if (!System.IO.File.Exists(path))
        {
            return new AuditItemResult
            {
                ItemKey = "demo.file_missing",
                Score = 0,
                Weight = 1,
                Passed = false,
                Title = "Demo 資料缺失",
                Message = $"找不到 Demo 檔案：{fileName}",
                Detail = new Dictionary<string, object?>
                {
                    ["ExpectedPath"] = path
                }
            };
        }

        var json = System.IO.File.ReadAllText(path);

        try
        {
            var dto = deserialize(json);
            return evaluate(dto);
        }
        catch (Exception ex)
        {
            return new AuditItemResult
            {
                ItemKey = "demo.parse_error",
                Score = 0,
                Weight = 1,
                Passed = false,
                Title = "Demo 解析失敗",
                Message = "Demo JSON 解析或規則評估失敗",
                Detail = new Dictionary<string, object?>
                {
                    ["File"] = fileName,
                    ["Error"] = ex.Message
                }
            };
        }
    }
}

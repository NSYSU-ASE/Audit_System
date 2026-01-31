using Microsoft.AspNetCore.Mvc;
using AseAudit.Core.Modules.Identity.Dtos;
using AseAudit.Core.Modules.Identity.Rules;

namespace AseAudit.Api.Controllers;

[ApiController]
[Route("api/test/audit")]
public class TestAuditController : ControllerBase
{
    [HttpGet("module1")]
    //Ad帳號保護規則測試
    public IActionResult TestIdentity()
    {
        var rule = new AdAccountProtectionRule();

        var a = new HostAccountSnapshotDto { HasAd = true, IsAdAccount = true, IsLocalAdmin = false };
        var b = new HostAccountSnapshotDto { HasAd = true, IsAdAccount = false, IsLocalAdmin = false };
        var c = new HostAccountSnapshotDto { HasAd = false, IsLocalAdmin = false };
        var d = new HostAccountSnapshotDto { HasAd = false, IsLocalAdmin = true };

        return Ok(new
        {
            A = rule.Evaluate(a).Score, // 100
            B = rule.Evaluate(b).Score, // 80
            C = rule.Evaluate(c).Score, // 40
            D = rule.Evaluate(d).Score  // 0
        });
    }
    //員工資料庫測試
    [HttpGet("employee-check")]
    public IActionResult EmployeeDirectoryCheck()
    {
        // 假資料：登入帳號
        var host = new HostIdentitySnapshotDto
        {
            LoggedInAdAccount = "ASE\\james.chen"
        };

        // 假資料：員工資料庫（你說先假設都有的情況）
        var employees = new[]
        {
            new EmployeeDirectoryRecordDto { AdAccount = "james.chen", IsActive = true },
            new EmployeeDirectoryRecordDto { AdAccount = "amy.lin", IsActive = true }
        };

        var rule = new EmployeeDirectoryProtectionRule();
        var result = rule.Evaluate(host, employees);

        return Ok(new
        {
            result.Score,
            result.Title,
            result.Message
        });
    }
    //密碼原則測試
    [HttpGet("password-policy")]
    public IActionResult PasswordPolicy()
    {
        var rule = new PasswordPolicyRule();

        // 先用假資料（你說「假設他都有的情況下」）
        var snapshot = new PasswordPolicySnapshotDto
        {
            MinPasswordLength = 10,
            MaxPasswordAgeDays = 90,
            MinPasswordAgeDays = 1,
            PasswordHistoryLength = 10,
            LockoutThreshold = 10,
            LockoutDurationMinutes = 15,
            LockoutObservationWindowMinutes = 15,
            PasswordComplexityEnabled = true,
            SameAsDomainPolicy = true
        };

        var r = rule.Evaluate(snapshot);

        // 直接回傳 AuditItemResult（Swagger 就會看到 score/title/message）
        return Ok(new
        {
            score = r.Score,
            title = r.Title,
            message = r.Message,
            detail = r.Detail
        });
    }
}

using Microsoft.AspNetCore.Mvc;
using AseAudit.Core.Modules.Module1.Dtos;
using AseAudit.Core.Modules.Module1.Rules;

namespace AseAudit.Api.Controllers;

[ApiController]
[Route("api/test/audit")]
public class TestAuditController : ControllerBase
{
    [HttpGet("module1")]
    public IActionResult TestModule1()
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
}

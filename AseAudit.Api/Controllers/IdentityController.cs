using Microsoft.AspNetCore.Mvc;
using AseAudit.Infrastructure.Repositories;
using AseAudit.Infrastructure.Mapping;

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

    /// <summary>
    /// 取得原始帳號 Entity 資料
    /// </summary>
    [HttpGet("accounts")]
    public IActionResult GetAccounts()
    {
        var data = _repo.GetAccounts();
        return Ok(data);
    }

    /// <summary>
    /// 取得原始帳號管理規則 Entity 資料
    /// </summary>
    [HttpGet("rules")]
    public IActionResult GetRules()
    {
        var data = _repo.GetRules();
        return Ok(data);
    }

    /// <summary>
    /// 帳號資料轉成 Rule 可使用的 HostAccountSnapshotDto
    /// </summary>
    [HttpGet("host-account-snapshots")]
    public IActionResult GetHostAccountSnapshots()
    {
        var data = _repo.GetAccounts()
            .Select(x => x.ToHostAccountSnapshotDto());

        return Ok(data);
    }

    /// <summary>
    /// 登入帳號資料轉成 HostIdentitySnapshotDto
    /// </summary>
    [HttpGet("host-identity-snapshots")]
    public IActionResult GetHostIdentitySnapshots()
    {
        var data = _repo.GetAccounts()
            .Select(x => x.ToHostIdentitySnapshotDto());

        return Ok(data);
    }

    /// <summary>
    /// 員工目錄資料 DTO
    /// </summary>
    [HttpGet("employee-directory")]
    public IActionResult GetEmployeeDirectory()
    {
        var data = _repo.GetAccounts()
            .Select(x => x.ToEmployeeDirectoryRecordDto());

        return Ok(data);
    }

    /// <summary>
    /// 密碼政策資料 DTO
    /// </summary>
    [HttpGet("password-policy")]
    public IActionResult GetPasswordPolicy()
    {
        var data = _repo.GetRules()
            .Select(x => x.ToPasswordPolicySnapshotDto());

        return Ok(data);
    }
}
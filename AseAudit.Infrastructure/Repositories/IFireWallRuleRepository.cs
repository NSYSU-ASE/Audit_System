using AseAudit.Core.Entities;

namespace AseAudit.Infrastructure.Repositories;

/// <summary>
/// <see cref="FireWallRule"/> 的持久化介面。
/// </summary>
public interface IFireWallRuleRepository
{
    /// <summary>批次寫入多筆規則，回傳寫入筆數。</summary>
    Task<int> AddRangeAsync(IEnumerable<FireWallRule> entities, CancellationToken cancellationToken);
}

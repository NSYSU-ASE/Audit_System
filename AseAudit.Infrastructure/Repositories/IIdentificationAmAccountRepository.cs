using AseAudit.Core.Entities;

namespace AseAudit.Infrastructure.Repositories;

/// <summary>
/// <see cref="IdentificationAmAccount"/> 的持久化介面。
/// </summary>
public interface IIdentificationAmAccountRepository
{
    /// <summary>批次寫入帳號列，回傳實際寫入筆數。</summary>
    Task<int> AddRangeAsync(IEnumerable<IdentificationAmAccount> entities, CancellationToken cancellationToken);
}

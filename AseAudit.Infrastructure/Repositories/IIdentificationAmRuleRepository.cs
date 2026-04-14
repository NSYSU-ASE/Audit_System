using AseAudit.Core.Entities;

namespace AseAudit.Infrastructure.Repositories;

/// <summary>
/// <see cref="IdentificationAmRule"/> 的持久化介面。
/// </summary>
public interface IIdentificationAmRuleRepository
{
    /// <summary>寫入單列規則，回傳寫入筆數 (1)。</summary>
    Task<int> AddAsync(IdentificationAmRule entity, CancellationToken cancellationToken);
}

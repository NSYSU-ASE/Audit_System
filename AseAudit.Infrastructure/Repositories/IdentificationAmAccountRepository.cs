using AseAudit.Core.Entities;
using AseAudit.Infrastructure.Data;

namespace AseAudit.Infrastructure.Repositories;

public sealed class IdentificationAmAccountRepository : IIdentificationAmAccountRepository
{
    private readonly AuditDbContext _db;

    public IdentificationAmAccountRepository(AuditDbContext db) => _db = db;

    public async Task<int> AddRangeAsync(IEnumerable<IdentificationAmAccount> entities, CancellationToken cancellationToken)
    {
        var list = entities as ICollection<IdentificationAmAccount> ?? entities.ToList();
        if (list.Count == 0) return 0;

        _db.IdentificationAmAccounts.AddRange(list);
        await _db.SaveChangesAsync(cancellationToken);
        return list.Count;
    }
}

using AseAudit.Core.Entities;
using AseAudit.Infrastructure.Data;

namespace AseAudit.Infrastructure.Repositories;

public sealed class FireWallRuleRepository : IFireWallRuleRepository
{
    private readonly AuditDbContext _db;

    public FireWallRuleRepository(AuditDbContext db) => _db = db;

    public async Task<int> AddRangeAsync(IEnumerable<FireWallRule> entities, CancellationToken cancellationToken)
    {
        var list = entities.ToList();
        if (list.Count == 0) return 0;

        _db.FireWallRules.AddRange(list);
        await _db.SaveChangesAsync(cancellationToken);
        return list.Count;
    }
}

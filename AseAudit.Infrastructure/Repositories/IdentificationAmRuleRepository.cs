using AseAudit.Core.Entities;
using AseAudit.Infrastructure.Data;

namespace AseAudit.Infrastructure.Repositories;

public sealed class IdentificationAmRuleRepository : IIdentificationAmRuleRepository
{
    private readonly AuditDbContext _db;

    public IdentificationAmRuleRepository(AuditDbContext db) => _db = db;

    public async Task<int> AddAsync(IdentificationAmRule entity, CancellationToken cancellationToken)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));

        _db.IdentificationAmRules.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return 1;
    }
}

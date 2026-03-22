using Microsoft.EntityFrameworkCore;
using SignalForge.Application;
using SignalForge.Domain;

namespace SignalForge.Infrastructure.Persistence;

public sealed class EfRuleAuditRepository(SignalForgeDbContext db) : IRuleAuditRepository
{
    public async Task AddAsync(RuleAuditEntry entry, CancellationToken cancellationToken)
    {
        db.RuleAuditEntries.Add(entry);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RuleAuditEntry>> ListByRuleIdAsync(
        Guid ruleId,
        CancellationToken cancellationToken)
    {
        var list = await db.RuleAuditEntries
            .AsNoTracking()
            .Where(e => e.RuleId == ruleId)
            .OrderByDescending(e => e.OccurredAtUtc)
            .ThenBy(e => e.Id)
            .ToListAsync(cancellationToken);

        return list;
    }
}

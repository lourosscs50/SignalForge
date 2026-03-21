using Microsoft.EntityFrameworkCore;
using SignalForge.Application;
using SignalForge.Domain;

namespace SignalForge.Infrastructure.Persistence;

public sealed class EfRuleRepository(SignalForgeDbContext db) : IRuleRepository
{
    public async Task AddAsync(Rule rule, CancellationToken cancellationToken)
    {
        db.Rules.Add(rule);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Rule>> ListActiveAsync(CancellationToken cancellationToken)
    {
        var list = await db.Rules
            .AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return list;
    }
}

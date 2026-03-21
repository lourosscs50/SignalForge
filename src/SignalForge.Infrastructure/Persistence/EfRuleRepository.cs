using Microsoft.EntityFrameworkCore;
using SignalForge.Application;
using SignalForge.Application.Queries;
using SignalForge.Contracts;
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

    public async Task<PagedResult<Rule>> ListPagedAsync(RuleListQuery query, CancellationToken cancellationToken)
    {
        var page = ListQueryNormalization.NormalizePage(query.Page);
        var pageSize = ListQueryNormalization.NormalizePageSize(query.PageSize);

        IQueryable<Rule> q = db.Rules.AsNoTracking();

        if (query.IsActive.HasValue)
            q = q.Where(r => r.IsActive == query.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(query.RuleType))
        {
            var rt = query.RuleType.Trim();
            q = q.Where(r => r.RuleType == rt);
        }

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(r => r.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Rule>(items, page, pageSize, total);
    }
}

using Microsoft.EntityFrameworkCore;
using SignalForge.Application;
using SignalForge.Application.Queries;
using SignalForge.Contracts;
using SignalForge.Domain;

namespace SignalForge.Infrastructure.Persistence;

public sealed class EfAlertRepository(SignalForgeDbContext db) : IAlertRepository
{
    public async Task AddAsync(Alert alert, CancellationToken cancellationToken)
    {
        db.Alerts.Add(alert);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<Alert?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await db.Alerts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task UpdateAsync(Alert alert, CancellationToken cancellationToken)
    {
        db.Alerts.Update(alert);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Alert>> ListAllAsync(CancellationToken cancellationToken)
    {
        return await db.Alerts
            .AsNoTracking()
            .OrderByDescending(a => a.CreatedAtUtc)
            .ThenBy(a => a.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Alert>> ListByRuleIdAsync(Guid ruleId, CancellationToken cancellationToken)
    {
        return await db.Alerts
            .AsNoTracking()
            .Where(a => a.RuleId == ruleId)
            .OrderByDescending(a => a.CreatedAtUtc)
            .ThenBy(a => a.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<Alert>> ListPagedAsync(AlertListQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page;
        var pageSize = query.PageSize;

        IQueryable<Alert> q = db.Alerts.AsNoTracking();

        if (query.RuleId.HasValue)
            q = q.Where(a => a.RuleId == query.RuleId.Value);

        if (query.SignalId.HasValue)
            q = q.Where(a => a.SignalId == query.SignalId.Value);

        if (query.FromCreatedUtc.HasValue)
            q = q.Where(a => a.CreatedAtUtc >= query.FromCreatedUtc.Value);

        if (query.ToCreatedUtc.HasValue)
            q = q.Where(a => a.CreatedAtUtc <= query.ToCreatedUtc.Value);

        if (query.IsAcknowledged.HasValue)
            q = q.Where(a => a.IsAcknowledged == query.IsAcknowledged.Value);

        if (query.IsResolved.HasValue)
            q = q.Where(a => a.IsResolved == query.IsResolved.Value);

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(a => a.CreatedAtUtc)
            .ThenBy(a => a.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Alert>(items, page, pageSize, total);
    }
}

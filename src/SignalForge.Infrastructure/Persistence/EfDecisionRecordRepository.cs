using Microsoft.EntityFrameworkCore;
using SignalForge.Application;
using SignalForge.Application.Queries;
using SignalForge.Contracts;
using SignalForge.Domain;

namespace SignalForge.Infrastructure.Persistence;

public sealed class EfDecisionRecordRepository(SignalForgeDbContext db) : IDecisionRecordRepository
{
    public async Task AddAsync(DecisionRecord record, CancellationToken cancellationToken)
    {
        db.DecisionRecords.Add(record);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<DecisionRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await db.DecisionRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<PagedResult<DecisionRecord>> ListPagedAsync(DecisionListQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page;
        var pageSize = query.PageSize;

        IQueryable<DecisionRecord> q = db.DecisionRecords.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.DecisionCategory))
        {
            var c = query.DecisionCategory.Trim();
            q = q.Where(d => d.DecisionCategory == c);
        }

        if (!string.IsNullOrWhiteSpace(query.DecisionType))
        {
            var t = query.DecisionType.Trim();
            q = q.Where(d => d.DecisionType == t);
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var s = query.Status.Trim();
            q = q.Where(d => d.Status == s);
        }

        if (query.FromOccurredUtc.HasValue)
            q = q.Where(d => d.OccurredAtUtc >= query.FromOccurredUtc.Value);

        if (query.ToOccurredUtc.HasValue)
            q = q.Where(d => d.OccurredAtUtc <= query.ToOccurredUtc.Value);

        if (query.CorrelationId.HasValue)
            q = q.Where(d => d.CorrelationId == query.CorrelationId.Value);

        if (!string.IsNullOrWhiteSpace(query.TraceId))
        {
            var tr = query.TraceId.Trim();
            q = q.Where(d => d.TraceId == tr);
        }

        if (query.ExecutionId.HasValue)
            q = q.Where(d => d.ExecutionId == query.ExecutionId.Value);

        if (query.RuleId.HasValue)
            q = q.Where(d => d.RuleId == query.RuleId.Value);

        if (!string.IsNullOrWhiteSpace(query.PolicyProfileKey))
        {
            var p = query.PolicyProfileKey.Trim();
            q = q.Where(d => d.PolicyProfileKey == p);
        }

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(d => d.OccurredAtUtc)
            .ThenBy(d => d.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<DecisionRecord>(items, page, pageSize, total);
    }

    public async Task<IReadOnlyList<DecisionRecord>> ListAllAsync(CancellationToken cancellationToken)
    {
        return await db.DecisionRecords
            .AsNoTracking()
            .OrderByDescending(d => d.OccurredAtUtc)
            .ThenBy(d => d.Id)
            .ToListAsync(cancellationToken);
    }
}

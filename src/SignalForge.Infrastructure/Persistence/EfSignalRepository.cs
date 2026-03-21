using Microsoft.EntityFrameworkCore;
using SignalForge.Application;
using SignalForge.Application.Queries;
using SignalForge.Contracts;
using SignalForge.Domain;

namespace SignalForge.Infrastructure.Persistence;

public sealed class EfSignalRepository(SignalForgeDbContext db) : ISignalRepository
{
    public async Task AddAsync(Signal signal, CancellationToken cancellationToken)
    {
        db.Signals.Add(signal);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<Signal>> ListPagedAsync(SignalListQuery query, CancellationToken cancellationToken)
    {
        var page = ListQueryNormalization.NormalizePage(query.Page);
        var pageSize = ListQueryNormalization.NormalizePageSize(query.PageSize);

        IQueryable<Signal> q = db.Signals.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Type))
        {
            var t = query.Type.Trim();
            q = q.Where(s => s.Type == t);
        }

        if (!string.IsNullOrWhiteSpace(query.Source))
        {
            var src = query.Source.Trim();
            q = q.Where(s => s.Source == src);
        }

        if (query.FromOccurredUtc.HasValue)
            q = q.Where(s => s.OccurredAtUtc >= query.FromOccurredUtc.Value);

        if (query.ToOccurredUtc.HasValue)
            q = q.Where(s => s.OccurredAtUtc <= query.ToOccurredUtc.Value);

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(s => s.OccurredAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Signal>(items, page, pageSize, total);
    }
}

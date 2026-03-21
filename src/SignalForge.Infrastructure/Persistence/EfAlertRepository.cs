using Microsoft.EntityFrameworkCore;
using SignalForge.Application;
using SignalForge.Domain;

namespace SignalForge.Infrastructure.Persistence;

public sealed class EfAlertRepository(SignalForgeDbContext db) : IAlertRepository
{
    public async Task AddAsync(Alert alert, CancellationToken cancellationToken)
    {
        db.Alerts.Add(alert);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Alert>> ListAsync(CancellationToken cancellationToken)
    {
        var list = await db.Alerts
            .AsNoTracking()
            .OrderBy(a => a.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return list;
    }
}

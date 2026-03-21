using Microsoft.EntityFrameworkCore;
using SignalForge.Application;
using SignalForge.Domain;

namespace SignalForge.Infrastructure.Persistence;

public sealed class EfSignalRepository(SignalForgeDbContext db) : ISignalRepository
{
    public async Task AddAsync(Signal signal, CancellationToken cancellationToken)
    {
        db.Signals.Add(signal);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Signal>> ListAsync(CancellationToken cancellationToken)
    {
        var list = await db.Signals
            .AsNoTracking()
            .OrderBy(s => s.IngestedAtUtc)
            .ToListAsync(cancellationToken);

        return list;
    }
}

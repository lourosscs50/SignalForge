using Microsoft.EntityFrameworkCore;

namespace SignalForge.Infrastructure.Persistence;

public class SignalForgeDbContext : DbContext
{
    public SignalForgeDbContext(DbContextOptions<SignalForgeDbContext> options)
        : base(options)
    {
    }

    // TEMP placeholders (we will wire real domain entities next)
    public DbSet<object> Users => Set<object>();
    public DbSet<object> Signals => Set<object>();
    public DbSet<object> Rules => Set<object>();
    public DbSet<object> Alerts => Set<object>();
}
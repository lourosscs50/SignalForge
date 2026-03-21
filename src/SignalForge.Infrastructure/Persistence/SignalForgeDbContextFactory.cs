using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SignalForge.Infrastructure.Persistence;

/// <summary>Design-time factory for EF Core CLI (migrations). Uses local docker-compose PostgreSQL defaults.</summary>
public sealed class SignalForgeDbContextFactory : IDesignTimeDbContextFactory<SignalForgeDbContext>
{
    public SignalForgeDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SignalForgeDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5434;Database=signalforge;Username=postgres;Password=postgres");
        return new SignalForgeDbContext(optionsBuilder.Options);
    }
}

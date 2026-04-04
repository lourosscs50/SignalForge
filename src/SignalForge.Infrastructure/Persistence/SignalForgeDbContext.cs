using Microsoft.EntityFrameworkCore;
using SignalForge.Domain;

namespace SignalForge.Infrastructure.Persistence;

public sealed class SignalForgeDbContext : DbContext
{
    public SignalForgeDbContext(DbContextOptions<SignalForgeDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Rule> Rules => Set<Rule>();
    public DbSet<RuleAuditEntry> RuleAuditEntries => Set<RuleAuditEntry>();
    public DbSet<Signal> Signals => Set<Signal>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<DecisionRecord> DecisionRecords => Set<DecisionRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SignalForgeDbContext).Assembly);
    }
}

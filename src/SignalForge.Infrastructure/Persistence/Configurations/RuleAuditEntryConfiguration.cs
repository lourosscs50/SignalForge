using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SignalForge.Domain;

namespace SignalForge.Infrastructure.Persistence.Configurations;

public sealed class RuleAuditEntryConfiguration : IEntityTypeConfiguration<RuleAuditEntry>
{
    public void Configure(EntityTypeBuilder<RuleAuditEntry> builder)
    {
        builder.ToTable("rule_audit_entries");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.RuleId)
            .IsRequired();

        builder.Property(e => e.Action)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(e => e.OccurredAtUtc)
            .IsRequired();

        builder.HasIndex(e => e.RuleId);

        builder.HasOne<Rule>()
            .WithMany()
            .HasForeignKey(e => e.RuleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

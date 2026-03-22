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

        builder.OwnsOne(e => e.UpdateDetail, od =>
        {
            od.Property(d => d.PreviousName).HasColumnName("update_previous_name").HasMaxLength(500);
            od.Property(d => d.NewName).HasColumnName("update_new_name").HasMaxLength(500);
            od.Property(d => d.PreviousMatchValue).HasColumnName("update_previous_match_value").HasMaxLength(512);
            od.Property(d => d.NewMatchValue).HasColumnName("update_new_match_value").HasMaxLength(512);
        });

        builder.Navigation(e => e.UpdateDetail).IsRequired(false);

        builder.HasIndex(e => e.RuleId);

        builder.HasOne<Rule>()
            .WithMany()
            .HasForeignKey(e => e.RuleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

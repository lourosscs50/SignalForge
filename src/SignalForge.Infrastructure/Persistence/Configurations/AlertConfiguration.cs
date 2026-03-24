using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SignalForge.Domain;

namespace SignalForge.Infrastructure.Persistence.Configurations;

public sealed class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    public void Configure(EntityTypeBuilder<Alert> builder)
    {
        builder.ToTable("alerts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.SignalId)
            .IsRequired();

        builder.Property(a => a.RuleId)
            .IsRequired();

        builder.Property(a => a.CreatedAtUtc)
            .IsRequired();

        builder.Property(a => a.IsAcknowledged)
            .IsRequired();

        builder.Property(a => a.AcknowledgedAtUtc);

        builder.Property(a => a.IsResolved)
            .IsRequired();

        builder.Property(a => a.ResolvedAtUtc);

        builder.HasIndex(a => a.SignalId);
        builder.HasIndex(a => a.RuleId);

        builder.HasOne<Signal>()
            .WithMany()
            .HasForeignKey(a => a.SignalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Rule>()
            .WithMany()
            .HasForeignKey(a => a.RuleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

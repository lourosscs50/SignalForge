using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SignalForge.Domain;

namespace SignalForge.Infrastructure.Persistence.Configurations;

public sealed class RuleConfiguration : IEntityTypeConfiguration<Rule>
{
    public void Configure(EntityTypeBuilder<Rule> builder)
    {
        builder.ToTable("rules");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(r => r.RuleType)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(r => r.MatchValue)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(r => r.IsActive)
            .IsRequired();

        builder.Property(r => r.IsArchived)
            .IsRequired();

        builder.Property(r => r.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(r => r.IsActive);
        builder.HasIndex(r => r.IsArchived);
    }
}

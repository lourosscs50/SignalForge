using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SignalForge.Domain;

namespace SignalForge.Infrastructure.Persistence.Configurations;

public sealed class SignalConfiguration : IEntityTypeConfiguration<Signal>
{
    public void Configure(EntityTypeBuilder<Signal> builder)
    {
        builder.ToTable("signals");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Source)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(s => s.Type)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(s => s.Payload)
            .HasMaxLength(8000)
            .IsRequired();

        builder.Property(s => s.Value);

        builder.Property(s => s.OccurredAtUtc)
            .IsRequired();

        builder.Property(s => s.IngestedAtUtc)
            .IsRequired();

        builder.HasIndex(s => s.IngestedAtUtc);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SignalForge.Domain;

namespace SignalForge.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(u => u.DisplayName)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(u => u.PasswordHash)
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(u => u.CreatedAtUtc)
            .IsRequired();

        // RegisterUser normalizes email to lowercase; uniqueness matches application duplicate checks.
        builder.HasIndex(u => u.Email).IsUnique();
    }
}

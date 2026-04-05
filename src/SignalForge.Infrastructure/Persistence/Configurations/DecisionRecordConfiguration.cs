using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SignalForge.Domain;
using SignalForge.Infrastructure.Persistence.Conversions;

namespace SignalForge.Infrastructure.Persistence.Configurations;

public sealed class DecisionRecordConfiguration : IEntityTypeConfiguration<DecisionRecord>
{
    public void Configure(EntityTypeBuilder<DecisionRecord> builder)
    {
        builder.ToTable("decision_records");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.DecisionCategory).HasMaxLength(128).IsRequired();
        builder.Property(d => d.DecisionType).HasMaxLength(128).IsRequired();
        builder.Property(d => d.Status).HasMaxLength(64).IsRequired();
        builder.Property(d => d.TraceId).HasMaxLength(256);
        builder.Property(d => d.SelectedOptionId).HasMaxLength(256);
        builder.Property(d => d.ChronoFlowExecutionInstanceId);
        builder.Property(d => d.DecisionOptions)
            .HasColumnType("jsonb")
            .HasConversion(DecisionOptionsValueConverter.Instance);
        builder.Property(d => d.PolicyProfileKey).HasMaxLength(256);
        builder.Property(d => d.StrategyPathKey).HasMaxLength(256);
        builder.Property(d => d.ProviderModelSummary).HasMaxLength(256);
        builder.Property(d => d.InputSummary).HasMaxLength(2048);
        builder.Property(d => d.OutputSummary).HasMaxLength(2048);
        builder.Property(d => d.ExplanationSummary).HasMaxLength(2048);
        builder.Property(d => d.ConfidenceBand).HasMaxLength(64);
        builder.Property(d => d.RecommendedActionSummary).HasMaxLength(1024);
        builder.Property(d => d.AuditActorUserId).HasMaxLength(256);

        builder.Property(d => d.OccurredAtUtc).IsRequired();

        builder.HasIndex(d => d.OccurredAtUtc);
        builder.HasIndex(d => d.DecisionType);
        builder.HasIndex(d => d.CorrelationId);
        builder.HasIndex(d => d.ExecutionId);
        builder.HasIndex(d => d.RuleId);
        builder.HasIndex(d => d.TraceId);
    }
}

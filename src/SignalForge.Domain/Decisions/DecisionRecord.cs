namespace SignalForge.Domain;

/// <summary>
/// Append-only operator visibility snapshot for a decision moment (observation, not execution authority).
/// </summary>
public sealed record DecisionRecord(
    Guid Id,
    DateTime OccurredAtUtc,
    string DecisionCategory,
    string DecisionType,
    string Status,
    Guid? CorrelationId,
    Guid? ExecutionId,
    string? TraceId,
    Guid? AlertId,
    Guid? RuleId,
    Guid? SignalId,
    string? PolicyProfileKey,
    string? StrategyPathKey,
    string? ProviderModelSummary,
    string? InputSummary,
    string? OutputSummary,
    bool ExplanationAvailable,
    string? ExplanationSummary,
    string? ConfidenceBand,
    int? FallbackUsageCount,
    int? RetryUsageCount,
    string? RecommendedActionSummary,
    string? AuditActorUserId);

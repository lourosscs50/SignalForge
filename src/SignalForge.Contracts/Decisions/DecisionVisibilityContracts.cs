namespace SignalForge.Contracts.Decisions;

public sealed record DecisionInputSummary(string NormalizedSummary);

public sealed record DecisionOutputSummary(string ResultSummary);

/// <summary>Bounded, operator-safe explanation surface (no hidden chain-of-thought).</summary>
public sealed record DecisionExplanationSummary(
    bool ExplanationAvailable,
    string? SummaryText,
    IReadOnlyList<string>? ReasonCodes,
    string? ConfidenceBand,
    int? FallbackUsageCount,
    int? RetryUsageCount);

public sealed record DecisionTraceSummary(
    Guid? CorrelationId,
    Guid? ExecutionId,
    string? TraceId,
    IReadOnlyList<Guid> RelatedEntityIds);

public sealed record DecisionVisibilityResponse(
    Guid DecisionId,
    string DecisionCategory,
    string DecisionType,
    DateTimeOffset OccurredAtUtc,
    string Status,
    DecisionInputSummary Input,
    DecisionOutputSummary Output,
    string? PolicyProfileKey,
    string? StrategyPathKey,
    string? ProviderModelSummary,
    DecisionExplanationSummary Explanation,
    string? RecommendedDownstreamSummary,
    string? AuditActorUserId,
    DecisionTraceSummary Trace);

public sealed record DecisionTypeCountRow(string DecisionType, int Count);

public sealed record DecisionVisibilityMetricsResponse(
    int TotalDecisions,
    IReadOnlyList<DecisionTypeCountRow> CountsByDecisionType);

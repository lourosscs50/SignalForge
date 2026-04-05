namespace SignalForge.Contracts.Decisions;

public sealed record DecisionInputSummary(string NormalizedSummary);

public sealed record DecisionOutputSummary(string ResultSummary);

/// <summary>Operator-safe option row for visibility consumers; order is explicit via <see cref="Ordinal"/>.</summary>
public sealed record DecisionOptionSummary(string OptionId, string? Summary, int Ordinal);

/// <summary>Bounded, operator-safe explanation surface (no hidden chain-of-thought).</summary>
public sealed record DecisionExplanationSummary(
    bool ExplanationAvailable,
    string? SummaryText,
    IReadOnlyList<string>? ReasonCodes,
    string? ConfidenceBand,
    int? FallbackUsageCount,
    int? RetryUsageCount);

/// <summary>
/// Trace and correlation handles for operator visibility.
/// <list type="bullet">
/// <item><description><see cref="CorrelationId"/> — legacy JSON name; value is the SignalForge signal entity id (filter param).</description></item>
/// <item><description><see cref="ExecutionId"/> — legacy JSON name; value is the SignalForge alert entity id (not ChronoFlow execution instance id).</description></item>
/// <item><description><see cref="TraceId"/> — distributed trace thread id when propagated; often null.</description></item>
/// <item><description><see cref="SignalEntityId"/> / <see cref="AlertEntityId"/> — additive explicit entity ids (prefer for new clients).</description></item>
/// <item><description><see cref="ChronoFlowExecutionInstanceId"/> — reserved for peer execution instance id when wired; null until then.</description></item>
/// </list>
/// </summary>
public sealed record DecisionTraceSummary(
    Guid? CorrelationId,
    Guid? ExecutionId,
    string? TraceId,
    IReadOnlyList<Guid> RelatedEntityIds,
    Guid? SignalEntityId = null,
    Guid? AlertEntityId = null,
    Guid? ChronoFlowExecutionInstanceId = null);

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
    /// <summary>Winning option id when persisted; null when unknown or not applicable.</summary>
    string? SelectedOptionId,
    /// <summary>Considered options when persisted; null when absent.</summary>
    IReadOnlyList<DecisionOptionSummary>? DecisionOptions,
    DecisionTraceSummary Trace);

public sealed record DecisionTypeCountRow(string DecisionType, int Count);

public sealed record DecisionVisibilityMetricsResponse(
    int TotalDecisions,
    IReadOnlyList<DecisionTypeCountRow> CountsByDecisionType);

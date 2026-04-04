namespace SignalForge.Application.Decisions;

/// <summary>Optional structured hints for observation rows (never raw prompts or full payloads).</summary>
public sealed record DecisionObservationContext(
    string? EvaluatorStrategyKey = null,
    string? InputSummary = null,
    string? OutputSummaryOverride = null);

using SignalForge.Domain;

namespace SignalForge.Application.Decisions;

/// <summary>Optional structured hints for observation rows (never raw prompts or full payloads).</summary>
public sealed record DecisionObservationContext(
    string? EvaluatorStrategyKey = null,
    string? InputSummary = null,
    string? OutputSummaryOverride = null,
    /// <summary>Selected option id when explicitly known to the caller — never guessed.</summary>
    string? SelectedOptionId = null,
    /// <summary>Option set when explicitly known; order is preserved via <see cref="DecisionOptionSnapshot.Ordinal"/>.</summary>
    IReadOnlyList<DecisionOptionSnapshot>? DecisionOptions = null,
    /// <summary>ChronoFlow execution instance id only when the caller holds a trusted value.</summary>
    Guid? ChronoFlowExecutionInstanceId = null);

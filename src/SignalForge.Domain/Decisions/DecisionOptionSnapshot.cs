namespace SignalForge.Domain;

/// <summary>
/// Operator-safe option row persisted with a decision snapshot. Order is carried by <see cref="Ordinal"/>.
/// </summary>
public sealed record DecisionOptionSnapshot(string OptionId, string? Summary, int Ordinal);

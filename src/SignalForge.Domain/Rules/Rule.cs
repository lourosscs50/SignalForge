namespace SignalForge.Domain;

/// <param name="RuleType">Evaluator discriminator, e.g. <see cref="RuleTypes.SignalTypeEquals"/>.</param>
/// <param name="MatchValue">Evaluator-specific match payload (for SignalTypeEquals: the signal type to match).</param>
public sealed record Rule(
    Guid Id,
    string Name,
    string RuleType,
    string MatchValue,
    bool IsActive,
    DateTime CreatedAtUtc
);

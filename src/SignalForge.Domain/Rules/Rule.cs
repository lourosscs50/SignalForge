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
)
{
    /// <summary>Marks the rule active. Idempotent when already active.</summary>
    public Rule Activate() => this with { IsActive = true };

    /// <summary>Marks the rule inactive. Idempotent when already inactive.</summary>
    public Rule Deactivate() => this with { IsActive = false };
}

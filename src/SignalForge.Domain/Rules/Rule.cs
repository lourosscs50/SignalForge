namespace SignalForge.Domain;

/// <param name="RuleType">Evaluator discriminator, e.g. <see cref="RuleTypes.SignalTypeEquals"/>.</param>
/// <param name="MatchValue">Evaluator-specific match payload (for SignalTypeEquals: the signal type to match).</param>
public sealed record Rule(
    Guid Id,
    string Name,
    string RuleType,
    string MatchValue,
    bool IsActive,
    bool IsArchived,
    DateTime CreatedAtUtc
)
{
    /// <summary>Marks the rule active. Idempotent when already active. Throws if the rule is archived.</summary>
    public Rule Activate()
    {
        if (IsArchived)
            throw new InvalidOperationException("Cannot activate an archived rule.");

        return this with { IsActive = true };
    }

    /// <summary>Marks the rule inactive. Idempotent when already inactive.</summary>
    public Rule Deactivate() => this with { IsActive = false };

    /// <summary>Retires the rule from evaluation. Sets <see cref="IsArchived"/> and clears <see cref="IsActive"/>. Idempotent when already archived.</summary>
    public Rule Archive() =>
        IsArchived ? this : this with { IsArchived = true, IsActive = false };

    /// <summary>Updates operator-maintained fields. Preserves <see cref="Id"/>, <see cref="RuleType"/>, <see cref="IsActive"/>, <see cref="IsArchived"/>, and <see cref="CreatedAtUtc"/>.</summary>
    public Rule UpdateDetails(string name, string matchValue) =>
        this with { Name = name, MatchValue = matchValue };
}

namespace SignalForge.Domain;

public sealed record Alert(
    Guid Id,
    Guid SignalId,
    Guid RuleId,
    DateTime CreatedAtUtc,
    bool IsAcknowledged = false,
    DateTime? AcknowledgedAtUtc = null,
    bool IsResolved = false,
    DateTime? ResolvedAtUtc = null)
{
    /// <summary>Marks the alert acknowledged. Idempotent when already acknowledged; preserves the first <see cref="AcknowledgedAtUtc"/>.</summary>
    public Alert Acknowledge(DateTime utcNow) =>
        IsAcknowledged ? this : this with { IsAcknowledged = true, AcknowledgedAtUtc = utcNow };

    /// <summary>Marks the alert resolved. Idempotent when already resolved; preserves the first <see cref="ResolvedAtUtc"/>.
    /// Ensures acknowledgment: unacknowledged alerts become acknowledged using the same timestamp as resolution.</summary>
    public Alert Resolve(DateTime utcNow) =>
        IsResolved
            ? this
            : this with
            {
                IsResolved = true,
                ResolvedAtUtc = utcNow,
                IsAcknowledged = true,
                AcknowledgedAtUtc = IsAcknowledged ? AcknowledgedAtUtc : utcNow
            };
}

namespace SignalForge.Domain;

public sealed record Alert(
    Guid Id,
    Guid SignalId,
    Guid RuleId,
    DateTime CreatedAtUtc,
    bool IsAcknowledged = false,
    DateTime? AcknowledgedAtUtc = null)
{
    /// <summary>Marks the alert acknowledged. Idempotent when already acknowledged; preserves the first <see cref="AcknowledgedAtUtc"/>.</summary>
    public Alert Acknowledge(DateTime utcNow) =>
        IsAcknowledged ? this : this with { IsAcknowledged = true, AcknowledgedAtUtc = utcNow };
}

namespace SignalForge.Domain;

public sealed record Alert(
    Guid Id,
    Guid SignalId,
    Guid RuleId,
    DateTime CreatedAtUtc,
    bool IsAcknowledged = false,
    DateTime? AcknowledgedAtUtc = null,
    string? AcknowledgedByUserId = null,
    bool IsResolved = false,
    DateTime? ResolvedAtUtc = null,
    string? ResolvedByUserId = null,
    DateTime? ReopenedAtUtc = null,
    string? ReopenedByUserId = null)
{
    /// <summary>Marks the alert acknowledged. Idempotent when already acknowledged; preserves the first acknowledgment actor and timestamp.</summary>
    public Alert Acknowledge(DateTime utcNow, string actorUserId)
    {
        var actor = RequireActor(actorUserId);
        return IsAcknowledged
            ? this
            : this with
            {
                IsAcknowledged = true,
                AcknowledgedAtUtc = utcNow,
                AcknowledgedByUserId = actor
            };
    }

    /// <summary>Marks the alert resolved. Idempotent when already resolved; preserves the first resolution actor and timestamp.
    /// Does not change acknowledgment state or acknowledgment attribution.</summary>
    public Alert Resolve(DateTime utcNow, string actorUserId)
    {
        var actor = RequireActor(actorUserId);
        return IsResolved
            ? this
            : this with
            {
                IsResolved = true,
                ResolvedAtUtc = utcNow,
                ResolvedByUserId = actor
            };
    }

    /// <summary>Clears resolution and resolution attribution; records reopen attribution. Idempotent when already unresolved; preserves prior reopen actor and timestamp.</summary>
    public Alert Reopen(DateTime utcNow, string actorUserId)
    {
        var actor = RequireActor(actorUserId);
        return !IsResolved
            ? this
            : this with
            {
                IsResolved = false,
                ResolvedAtUtc = null,
                ResolvedByUserId = null,
                ReopenedAtUtc = utcNow,
                ReopenedByUserId = actor
            };
    }

    private static string RequireActor(string actorUserId)
    {
        var trimmed = (actorUserId ?? string.Empty).Trim();
        if (trimmed.Length == 0)
            throw new ArgumentException("Actor user id is required.", nameof(actorUserId));
        return trimmed;
    }
}

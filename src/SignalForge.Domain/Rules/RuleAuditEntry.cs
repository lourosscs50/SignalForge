namespace SignalForge.Domain;

public sealed class RuleAuditEntry
{
    public Guid Id { get; init; }
    public Guid RuleId { get; init; }
    public string Action { get; init; } = string.Empty;
    public DateTime OccurredAtUtc { get; init; }
    public RuleAuditUpdateDetail? UpdateDetail { get; init; }

    public RuleAuditEntry()
    {
    }

    public RuleAuditEntry(
        Guid Id,
        Guid RuleId,
        string Action,
        DateTime OccurredAtUtc,
        RuleAuditUpdateDetail? UpdateDetail = null)
    {
        this.Id = Id;
        this.RuleId = RuleId;
        this.Action = Action;
        this.OccurredAtUtc = OccurredAtUtc;
        this.UpdateDetail = UpdateDetail;
    }
}

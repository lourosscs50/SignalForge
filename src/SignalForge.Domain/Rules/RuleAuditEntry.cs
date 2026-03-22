namespace SignalForge.Domain;

public sealed record RuleAuditEntry(
    Guid Id,
    Guid RuleId,
    string Action,
    DateTime OccurredAtUtc);

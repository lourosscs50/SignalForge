namespace SignalForge.Domain;

public sealed record Alert(
    Guid Id,
    Guid SignalId,
    Guid RuleId,
    DateTime CreatedAtUtc
);


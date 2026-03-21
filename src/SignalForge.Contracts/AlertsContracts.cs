namespace SignalForge.Contracts.Alerts;

public sealed record AlertResponse(
    Guid Id,
    Guid SignalId,
    Guid RuleId,
    DateTimeOffset CreatedAtUtc
);

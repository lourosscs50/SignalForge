namespace SignalForge.Contracts.Alerts;

public sealed record AlertResponse(
    Guid Id,
    Guid SignalId,
    Guid RuleId,
    DateTimeOffset CreatedAtUtc
);

/// <summary>Lightweight rule context for alert detail reads.</summary>
public sealed record AlertRuleSummary(
    Guid Id,
    string Name,
    string RuleType,
    bool IsActive);

/// <summary>Lightweight signal context for alert detail reads.</summary>
public sealed record AlertSignalSummary(
    Guid Id,
    string Source,
    string Type,
    DateTimeOffset OccurredAtUtc);

/// <summary>Single-alert read with linked rule and signal visibility (get-by-id).</summary>
public sealed record AlertDetailResponse(
    Guid Id,
    Guid SignalId,
    Guid RuleId,
    DateTimeOffset CreatedAtUtc,
    AlertRuleSummary Rule,
    AlertSignalSummary Signal);

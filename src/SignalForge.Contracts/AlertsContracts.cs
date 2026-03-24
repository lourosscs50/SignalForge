namespace SignalForge.Contracts.Alerts;

public sealed record AlertResponse(
    Guid Id,
    Guid SignalId,
    Guid RuleId,
    DateTimeOffset CreatedAtUtc,
    bool IsAcknowledged,
    DateTimeOffset? AcknowledgedAtUtc,
    bool IsResolved,
    DateTimeOffset? ResolvedAtUtc);

/// <summary>Lightweight rule context for alert detail reads (current rule state).</summary>
public sealed record AlertRuleSummary(
    Guid Id,
    string Name,
    string RuleType,
    string MatchValue,
    bool IsActive,
    bool IsArchived,
    DateTimeOffset CreatedAtUtc);

/// <summary>Lightweight signal context for alert detail reads.</summary>
public sealed record AlertSignalSummary(
    Guid Id,
    string Source,
    string Type,
    double? Value,
    DateTimeOffset OccurredAtUtc);

/// <summary>Single-alert read with linked rule and signal visibility (get-by-id).</summary>
public sealed record AlertDetailResponse(
    Guid Id,
    Guid SignalId,
    Guid RuleId,
    DateTimeOffset CreatedAtUtc,
    bool IsAcknowledged,
    DateTimeOffset? AcknowledgedAtUtc,
    bool IsResolved,
    DateTimeOffset? ResolvedAtUtc,
    AlertRuleSummary Rule,
    AlertSignalSummary Signal);

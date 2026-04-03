namespace SignalForge.Contracts.Rules;

public sealed record CreateRuleRequest(
    string Name,
    string RuleType,
    string MatchValue,
    bool IsActive
);

public sealed record UpdateRuleRequest(
    string Name,
    string MatchValue);

public sealed record RuleResponse(
    Guid Id,
    string Name,
    string RuleType,
    string MatchValue,
    bool IsActive,
    bool IsArchived,
    DateTimeOffset CreatedAtUtc
);

/// <summary>Operational metrics for alerts associated with a single rule (current state only).</summary>
public sealed record RuleMetricsSummaryResponse(
    Guid RuleId,
    string RuleName,
    int TotalAlertsGenerated,
    int OpenAlerts,
    int AcknowledgedUnresolvedAlerts,
    int ResolvedAlerts,
    int ReopenedAlerts,
    double? AverageTimeToAcknowledgeSeconds,
    double? AverageTimeToResolveSeconds);

public sealed record RuleAuditUpdateDetailResponse(
    string PreviousName,
    string NewName,
    string PreviousMatchValue,
    string NewMatchValue);

public sealed record RuleAuditEntryResponse(
    Guid Id,
    Guid RuleId,
    string Action,
    DateTimeOffset OccurredAtUtc,
    RuleAuditUpdateDetailResponse? UpdateDetail = null);

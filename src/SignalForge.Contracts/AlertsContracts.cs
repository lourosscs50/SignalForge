namespace SignalForge.Contracts.Alerts;

public sealed record AlertResponse(
    Guid Id,
    Guid SignalId,
    Guid RuleId,
    DateTimeOffset CreatedAtUtc,
    bool IsAcknowledged,
    DateTimeOffset? AcknowledgedAtUtc,
    string? AcknowledgedByUserId,
    bool IsResolved,
    DateTimeOffset? ResolvedAtUtc,
    string? ResolvedByUserId,
    DateTimeOffset? ReopenedAtUtc,
    string? ReopenedByUserId,
    double? TimeToAcknowledgeSeconds,
    double? TimeToResolveSeconds,
    bool HasBeenReopened,
    string CurrentStatus,
    double AgeSeconds);

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
    string? AcknowledgedByUserId,
    bool IsResolved,
    DateTimeOffset? ResolvedAtUtc,
    string? ResolvedByUserId,
    DateTimeOffset? ReopenedAtUtc,
    string? ReopenedByUserId,
    double? TimeToAcknowledgeSeconds,
    double? TimeToResolveSeconds,
    bool HasBeenReopened,
    string CurrentStatus,
    double AgeSeconds,
    AlertRuleSummary Rule,
    AlertSignalSummary Signal);

/// <summary>Repository-derived lifecycle metrics for operator visibility (current alert state only).</summary>
public sealed record AlertMetricsSummaryResponse(
    int TotalAlerts,
    int OpenAlerts,
    int AcknowledgedUnresolvedAlerts,
    int ResolvedAlerts,
    int ReopenedAlerts,
    double? AverageTimeToAcknowledgeSeconds,
    double? AverageTimeToResolveSeconds);

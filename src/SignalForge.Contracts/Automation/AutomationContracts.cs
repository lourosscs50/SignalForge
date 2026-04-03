namespace SignalForge.Contracts.Automation;

/// <summary>Real alert lifecycle transitions that may be published as automation hooks (not idempotent no-ops).</summary>
public enum AlertLifecycleTransitionType
{
    AlertCreated,
    AlertAcknowledged,
    AlertResolved,
    AlertReopened
}

/// <summary>Normalized lifecycle truth for downstream subscribers (transport-agnostic, no workflow identifiers).</summary>
public sealed record AlertLifecycleEvent(
    AlertLifecycleTransitionType LifecycleTransitionType,
    Guid AlertId,
    Guid RuleId,
    Guid SignalId,
    DateTimeOffset OccurredAtUtc,
    string CurrentStatus,
    bool IsAcknowledged,
    bool IsResolved,
    DateTimeOffset? AcknowledgedAtUtc,
    DateTimeOffset? ResolvedAtUtc,
    DateTimeOffset? ReopenedAtUtc,
    string? AcknowledgedByUserId,
    string? ResolvedByUserId,
    string? ReopenedByUserId,
    string? RuleName);

/// <summary>Normalized automation trigger for external orchestrators; contains only SignalForge-local truth.</summary>
public sealed record ControlAutomationTriggerRequest(
    string TriggerType,
    Guid AlertId,
    Guid RuleId,
    Guid SignalId,
    DateTimeOffset OccurredAtUtc,
    string CurrentStatus,
    string LifecycleEventType,
    string? AcknowledgedByUserId,
    string? ResolvedByUserId,
    string? ReopenedByUserId,
    string? RuleName,
    bool HasBeenReopened);

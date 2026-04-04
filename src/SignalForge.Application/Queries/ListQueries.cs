namespace SignalForge.Application.Queries;

public sealed record RuleListQuery(
    int Page,
    int PageSize,
    bool? IsActive,
    string? RuleType
);

public sealed record SignalListQuery(
    int Page,
    int PageSize,
    string? Type,
    string? Source,
    DateTime? FromOccurredUtc,
    DateTime? ToOccurredUtc
);

public sealed record AlertListQuery(
    int Page,
    int PageSize,
    Guid? RuleId,
    Guid? SignalId,
    DateTime? FromCreatedUtc,
    DateTime? ToCreatedUtc,
    bool? IsAcknowledged,
    bool? IsResolved);

public sealed record DecisionListQuery(
    int Page,
    int PageSize,
    string? DecisionCategory,
    string? DecisionType,
    string? Status,
    DateTime? FromOccurredUtc,
    DateTime? ToOccurredUtc,
    Guid? CorrelationId,
    string? TraceId,
    Guid? ExecutionId,
    Guid? RuleId,
    string? PolicyProfileKey);

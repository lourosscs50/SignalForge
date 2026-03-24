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

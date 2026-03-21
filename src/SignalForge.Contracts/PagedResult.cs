namespace SignalForge.Contracts;

/// <summary>Offset pagination envelope for list endpoints.</summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount
);

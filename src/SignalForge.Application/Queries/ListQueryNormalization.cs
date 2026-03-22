namespace SignalForge.Application.Queries;

/// <summary>
/// Paging defaults and validation for list queries.
/// Invalid explicit paging values throw <see cref="ArgumentException"/> (API maps to 400).
/// </summary>
public static class ListQueryNormalization
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    /// <summary>Default for omitted page: 1. Explicit values must be &gt;= 1.</summary>
    public static int ResolvePageOrDefault(int? page)
    {
        if (page is null)
            return 1;
        if (page.Value < 1)
            throw new ArgumentException("Page must be >= 1.", nameof(page));
        return page.Value;
    }

    /// <summary>Default for omitted page size: <see cref="DefaultPageSize"/>. Explicit values must be 1..<see cref="MaxPageSize"/>.</summary>
    public static int ResolvePageSizeOrDefault(int? pageSize)
    {
        if (pageSize is null)
            return DefaultPageSize;
        if (pageSize.Value < 1 || pageSize.Value > MaxPageSize)
            throw new ArgumentException(
                $"PageSize must be between 1 and {MaxPageSize}.",
                nameof(pageSize));
        return pageSize.Value;
    }

    /// <summary>Guards programmatic callers; API paths should already use <see cref="ResolvePageOrDefault"/> / <see cref="ResolvePageSizeOrDefault"/>.</summary>
    public static void EnsureValidPaging(int page, int pageSize)
    {
        if (page < 1)
            throw new ArgumentException("Page must be >= 1.", nameof(page));
        if (pageSize < 1 || pageSize > MaxPageSize)
            throw new ArgumentException(
                $"PageSize must be between 1 and {MaxPageSize}.",
                nameof(pageSize));
    }
}

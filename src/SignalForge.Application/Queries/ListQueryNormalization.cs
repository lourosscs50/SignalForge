namespace SignalForge.Application.Queries;

public static class ListQueryNormalization
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public static int NormalizePage(int page) => page < 1 ? 1 : page;

    public static int NormalizePageSize(int pageSize)
    {
        if (pageSize < 1)
            return DefaultPageSize;
        return Math.Min(pageSize, MaxPageSize);
    }
}

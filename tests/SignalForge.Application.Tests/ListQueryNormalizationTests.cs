using SignalForge.Application.Queries;

namespace SignalForge.Application.Tests;

public sealed class ListQueryNormalizationTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(1, 1)]
    [InlineData(5, 5)]
    public void NormalizePage_clamps_to_at_least_one(int input, int expected)
    {
        Assert.Equal(expected, ListQueryNormalization.NormalizePage(input));
    }

    [Theory]
    [InlineData(0, ListQueryNormalization.DefaultPageSize)]
    [InlineData(-3, ListQueryNormalization.DefaultPageSize)]
    [InlineData(1, 1)]
    [InlineData(50, 50)]
    [InlineData(200, ListQueryNormalization.MaxPageSize)]
    public void NormalizePageSize_clamps_to_range(int input, int expected)
    {
        Assert.Equal(expected, ListQueryNormalization.NormalizePageSize(input));
    }
}

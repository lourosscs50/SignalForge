using SignalForge.Application.Queries;

namespace SignalForge.Application.Tests;

public sealed class ListQueryNormalizationTests
{
    [Fact]
    public void ResolvePageOrDefault_uses_1_when_omitted()
    {
        Assert.Equal(1, ListQueryNormalization.ResolvePageOrDefault(null));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void ResolvePageOrDefault_returns_explicit_valid_page(int page)
    {
        Assert.Equal(page, ListQueryNormalization.ResolvePageOrDefault(page));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ResolvePageOrDefault_throws_when_page_invalid(int page)
    {
        Assert.Throws<ArgumentException>(() => ListQueryNormalization.ResolvePageOrDefault(page));
    }

    [Fact]
    public void ResolvePageSizeOrDefault_uses_default_when_omitted()
    {
        Assert.Equal(ListQueryNormalization.DefaultPageSize, ListQueryNormalization.ResolvePageSizeOrDefault(null));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(100)]
    public void ResolvePageSizeOrDefault_returns_explicit_valid_size(int size)
    {
        Assert.Equal(size, ListQueryNormalization.ResolvePageSizeOrDefault(size));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(500)]
    public void ResolvePageSizeOrDefault_throws_when_out_of_range(int size)
    {
        Assert.Throws<ArgumentException>(() => ListQueryNormalization.ResolvePageSizeOrDefault(size));
    }

    [Fact]
    public void EnsureValidPaging_matches_resolved_values()
    {
        ListQueryNormalization.EnsureValidPaging(1, ListQueryNormalization.MaxPageSize);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(10, 0)]
    [InlineData(1, 101)]
    public void EnsureValidPaging_throws_for_invalid_pairs(int page, int pageSize)
    {
        Assert.Throws<ArgumentException>(() => ListQueryNormalization.EnsureValidPaging(page, pageSize));
    }
}

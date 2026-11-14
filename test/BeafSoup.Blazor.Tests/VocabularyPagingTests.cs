using BeafSoup.Blazor.Shared.Services;
using FluentAssertions;
using Xunit;

namespace BeafSoup.Blazor.Tests;

public sealed class VocabularyPagingTests
{
    [Theory]
    [InlineData(0, 15, 1)]
    [InlineData(1, 15, 1)]
    [InlineData(15, 15, 1)]
    [InlineData(16, 15, 2)]
    [InlineData(45, 15, 3)]
    [InlineData(10, 20, 1)]
    [InlineData(10, 30, 1)]
    public void TotalPages_按页大小向上取整且至少一页(int count, int pageSize, int expected)
        => VocabularyPaging.TotalPages(count, pageSize).Should().Be(expected);

    [Fact]
    public void Slice_返回当前页内容且页码越界被钳制()
    {
        var items = Enumerable.Range(1, 45).ToArray();

        VocabularyPaging.Slice(items, page: 1, pageSize: 15).Should().Equal(Enumerable.Range(1, 15));
        VocabularyPaging.Slice(items, page: 3, pageSize: 15).Should().Equal(Enumerable.Range(31, 15));
        VocabularyPaging.Slice(items, page: 99, pageSize: 15).Should().Equal(Enumerable.Range(31, 15));
        VocabularyPaging.Slice(items, page: -5, pageSize: 15).Should().Equal(Enumerable.Range(1, 15));
    }

    [Fact]
    public void PageSizes_只允许15_20_30()
        => VocabularyPaging.PageSizes.Should().Equal(15, 20, 30);
}

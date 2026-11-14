namespace BeafSoup.Blazor.Shared.Services;

/// <summary>单词表分页计算：纯函数，便于单元测试；页大小只允许 15 / 20 / 30。</summary>
public static class VocabularyPaging
{
    public static readonly int[] PageSizes = [15, 20, 30];

    public static int TotalPages(int itemCount, int pageSize)
        => Math.Max(1, (Math.Max(0, itemCount) + Math.Max(1, pageSize) - 1) / Math.Max(1, pageSize));

    public static int ClampPage(int page, int totalPages)
        => Math.Clamp(page, 1, Math.Max(1, totalPages));

    public static IReadOnlyList<T> Slice<T>(IReadOnlyList<T> items, int page, int pageSize)
    {
        var totalPages = TotalPages(items.Count, pageSize);
        var current = ClampPage(page, totalPages);
        var skip = (current - 1) * pageSize;
        return items.Skip(skip).Take(pageSize).ToArray();
    }
}

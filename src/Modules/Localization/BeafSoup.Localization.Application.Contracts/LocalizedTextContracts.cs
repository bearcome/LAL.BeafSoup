namespace BeafSoup.Localization;

/// <summary>按语义键批量读取某语言文本（内容库、例句等非 UI 文本共用）；缺失键不出现在结果中。</summary>
public interface ILocalizedTextCatalogQuery
{
    Task<IReadOnlyDictionary<string, string>> GetTextsAsync(
        IReadOnlyCollection<string> keys, string language, CancellationToken cancellationToken);
}

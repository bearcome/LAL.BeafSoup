using BeafSoup.SharedKernel;

namespace BeafSoup.Localization;

/// <summary>按语义键批量读取文本：内容库与例句查询复用同一本地化数据源。</summary>
public sealed class LocalizedTextCatalogQuery : ILocalizedTextCatalogQuery
{
    private readonly ILocalizationReader _reader;

    public LocalizedTextCatalogQuery(ILocalizationReader reader) => _reader = reader;

    public async Task<IReadOnlyDictionary<string, string>> GetTextsAsync(
        IReadOnlyCollection<string> keys, string language, CancellationToken cancellationToken)
    {
        var semanticKeys = keys.Distinct(StringComparer.Ordinal).Select(SemanticKey.Of).ToArray();
        var rows = await _reader.GetTextsByKeysAsync(semanticKeys, cancellationToken);
        return rows
            .Where(r => r.Language.Value == language)
            .GroupBy(r => r.Key.Value, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First().Text, StringComparer.Ordinal);
    }
}

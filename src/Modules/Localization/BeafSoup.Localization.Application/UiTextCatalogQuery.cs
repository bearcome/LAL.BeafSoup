namespace BeafSoup.Localization;

public sealed class UiTextCatalogQuery : IUiTextCatalogQuery
{
    private readonly ILocalizationReader _reader;

    public UiTextCatalogQuery(ILocalizationReader reader) => _reader = reader;

    public async Task<UiTextCatalogDto> GetCatalogAsync(string domain, CancellationToken cancellationToken)
    {
        var rows = await _reader.GetTextsByDomainAsync(domain, cancellationToken);
        var texts = rows
            .GroupBy(r => r.Key.Value)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyDictionary<string, string>)g.ToDictionary(
                    r => r.Language.Value, r => r.Text));
        return new UiTextCatalogDto(domain, texts);
    }
}

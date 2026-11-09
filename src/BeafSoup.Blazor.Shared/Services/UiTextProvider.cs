using BeafSoup.Localization;

namespace BeafSoup.Blazor.Shared.Services;

/// <summary>界面文案提供器：所有展示文字来自本地化数据层（零硬编码）；回退链 基础语言 → en-US → 语义键本身。</summary>
public interface IUiTextProvider
{
    Task EnsureLoadedAsync(string baseLanguage, CancellationToken cancellationToken);
    string Text(string key);
}

public sealed class UiTextProvider : IUiTextProvider
{
    private static readonly string[] Domains = ["ui", "content"];

    private readonly IUiTextCatalogQuery _catalogQuery;
    private readonly string _baseLanguage;
    private IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> _texts = new Dictionary<string, IReadOnlyDictionary<string, string>>();
    private Task? _loading;

    public UiTextProvider(IUiTextCatalogQuery catalogQuery, string baseLanguage)
        => (_catalogQuery, _baseLanguage) = (catalogQuery, baseLanguage);

    public Task EnsureLoadedAsync(string baseLanguage, CancellationToken cancellationToken)
        => _loading ??= LoadAsync(cancellationToken);

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        // UI 文案与内容标题（课程/单元/课时）走同一套语义键机制，分域拉取后合并
        var merged = new Dictionary<string, IReadOnlyDictionary<string, string>>();
        foreach (var domain in Domains)
        {
            var catalog = await _catalogQuery.GetCatalogAsync(domain, cancellationToken);
            foreach (var (key, byLanguage) in catalog.TextsByKeyAndLanguage)
                merged[key] = byLanguage;
        }
        _texts = merged;
    }

    public string Text(string key)
    {
        if (!_texts.TryGetValue(key, out var byLanguage))
            return key;
        if (byLanguage.TryGetValue(_baseLanguage, out var text))
            return text;
        if (byLanguage.TryGetValue("en-US", out var fallback))
            return fallback;
        // 缺失翻译时显示键并保留空白，编辑工具侧显示"未翻译"角标（数据层职责）
        return key;
    }
}

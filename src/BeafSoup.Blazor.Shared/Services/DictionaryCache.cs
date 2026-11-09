using System.Text.Json;

namespace BeafSoup.Blazor.Shared.Services;

/// <summary>词典条目（概念词典 K-V 缓存的客户端视图；只读）。</summary>
public sealed record DictionaryEntry(
    string ConceptKey,
    Guid ConceptId,
    string PartOfSpeech,
    string Kind,
    IReadOnlyDictionary<string, JsonElement> ByLanguage);

public interface IDictionaryCache
{
    Task EnsureLoadedAsync(CancellationToken cancellationToken);

    IReadOnlyCollection<DictionaryEntry> AllEntries { get; }

    /// <summary>按规范化表层形式检索候选（同一形式可命中多个概念/词形，不假定全局唯一）。</summary>
    IReadOnlyList<DictionaryEntry> FindBySurface(string surface, string language);

    DictionaryEntry? FindById(Guid conceptId);
}

/// <summary>概念词典 K-V 缓存：发布期导出的只读静态 JSON（dictionary.json），整载内存、按键命中；不回写。</summary>
public sealed class DictionaryCache : IDictionaryCache
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly string _dictionaryUrl;
    private List<DictionaryEntry> _entries = [];
    private Task? _loading;

    public DictionaryCache(HttpClient http, string dictionaryUrl = "dictionary.json")
        => (_http, _dictionaryUrl) = (http, dictionaryUrl);

    public Task EnsureLoadedAsync(CancellationToken cancellationToken)
        => _loading ??= LoadAsync(cancellationToken);

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        // 词典随静态站点部署；不存在时置空（页面显示空态），不影响学习闭环
        HttpResponseMessage response;
        try
        {
            response = await _http.GetAsync(_dictionaryUrl, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return;
        }
        if (!response.IsSuccessStatusCode)
            return;
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var document = JsonDocument.Parse(json);
        var entries = new List<DictionaryEntry>();
        foreach (var property in document.RootElement.EnumerateObject())
        {
            var value = property.Value;
            var byLanguage = new Dictionary<string, JsonElement>();
            foreach (var lang in value.EnumerateObject())
            {
                if (lang.Name is "conceptId" or "pos" or "kind")
                    continue;
                byLanguage[lang.Name] = lang.Value.Clone();
            }
            entries.Add(new DictionaryEntry(
                property.Name,
                Guid.Parse(value.GetProperty("conceptId").GetString()!),
                value.GetProperty("pos").GetString() ?? "",
                value.GetProperty("kind").GetString() ?? "",
                byLanguage));
        }
        _entries = entries;
    }

    public IReadOnlyCollection<DictionaryEntry> AllEntries => _entries;

    public IReadOnlyList<DictionaryEntry> FindBySurface(string surface, string language)
    {
        var normalized = surface.Trim();
        if (normalized.Length == 0)
            return [];
        var matches = new List<DictionaryEntry>();
        foreach (var entry in _entries)
        {
            if (!entry.ByLanguage.TryGetValue(language, out var languageEntry))
                continue;
            foreach (var lexeme in languageEntry.GetProperty("lexemes").EnumerateArray())
            {
                var lemma = lexeme.GetProperty("lemma").GetString() ?? "";
                if (string.Equals(lemma.Replace('_', ' '), normalized, StringComparison.OrdinalIgnoreCase))
                {
                    matches.Add(entry);
                    break;
                }
                // 范式槽位表层形式命中（变格/变位形态 → 原型）
                if (lexeme.TryGetProperty("forms", out var forms)
                    && forms.EnumerateObject().Any(paradigm
                        => paradigm.Value.EnumerateArray().Any(slot
                            => string.Equals(slot.GetString(), normalized, StringComparison.OrdinalIgnoreCase))))
                {
                    matches.Add(entry);
                    break;
                }
            }
        }
        return matches;
    }

    public DictionaryEntry? FindById(Guid conceptId)
        => _entries.FirstOrDefault(e => e.ConceptId == conceptId);
}

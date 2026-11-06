using BeafSoup.SharedKernel;

namespace BeafSoup.Localization;

/// <summary>某语义键在某种语言下的文本版本。同一键可存 N 种语言；文本变更必须递增修订号。</summary>
public sealed record LocalizedText
{
    public SemanticKey Key { get; }
    public LanguageCode Language { get; }
    public string Text { get; }
    public int Revision { get; }
    public SemanticKey? LemmaKey { get; }

    private LocalizedText(SemanticKey key, LanguageCode language, string text, int revision, SemanticKey? lemmaKey)
        => (Key, Language, Text, Revision, LemmaKey) = (key, language, text, revision, lemmaKey);

    public static LocalizedText Create(SemanticKey key, LanguageCode language, string text, SemanticKey? lemmaKey = null)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new DomainException(LocalizationErrorCodes.EmptyText, "本地化文本不能为空。");
        return new LocalizedText(key, language, text.Trim(), 1, lemmaKey);
    }

    /// <summary>持久化还原专用：按既有列值原样重建（含修订号），不重排修订；正常运行路径必须使用 Create/Revise。</summary>
    public static LocalizedText Restore(SemanticKey key, LanguageCode language, string text, int revision, SemanticKey? lemmaKey)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new DomainException(LocalizationErrorCodes.EmptyText, "本地化文本不能为空。");
        if (revision < 1)
            throw new DomainException(LocalizationErrorCodes.InvalidRevision, "文本修订号必须不小于 1。");
        return new LocalizedText(key, language, text.Trim(), revision, lemmaKey);
    }

    /// <summary>文本修订：内容变化时递增修订号；同值重写视为无操作（不重排修订号）。</summary>
    public LocalizedText Revise(string newText)
    {
        if (string.IsNullOrWhiteSpace(newText))
            throw new DomainException(LocalizationErrorCodes.EmptyText, "本地化文本不能为空。");
        var trimmed = newText.Trim();
        return trimmed == Text ? this : new LocalizedText(Key, Language, trimmed, Revision + 1, LemmaKey);
    }

    /// <summary>标注绑定文本修订号；不匹配即失效，必须重新校验或生成。</summary>
    public bool IsStaleFor(int expectedRevision) => expectedRevision != Revision;
}

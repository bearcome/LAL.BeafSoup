using BeafSoup.SharedKernel;

namespace BeafSoup.Content;

/// <summary>跨词元短语标注：索引绑定语言与文本修订号，仅适用于该版本；修订失配即失效，禁止按旧位置静默展示。</summary>
public sealed record SentenceSpanLink
{
    public string SentenceId { get; }
    public LanguageCode Language { get; }
    public int TextRevision { get; }
    public int StartTokenIndex { get; }
    public int TokenCount { get; }
    public SemanticKey LexicalEntryKey { get; }

    public SentenceSpanLink(string sentenceId, LanguageCode language, int textRevision, int startTokenIndex, int tokenCount, SemanticKey lexicalEntryKey)
    {
        if (startTokenIndex < 0 || tokenCount < 1)
            throw new DomainException(ContentErrorCodes.InvalidSpanRange, "短语标注的起点必须非负且跨度至少一个词元。");
        (SentenceId, Language, TextRevision, StartTokenIndex, TokenCount, LexicalEntryKey)
            = (SlugId.Validate(sentenceId, nameof(sentenceId)), language, textRevision, startTokenIndex, tokenCount, lexicalEntryKey);
    }

    public bool IsStaleFor(int currentTextRevision) => TextRevision != currentTextRevision;

    public void EnsureValid(int currentTextRevision)
    {
        if (IsStaleFor(currentTextRevision))
            throw new DomainException(ContentErrorCodes.StaleAnnotation, $"标注绑定修订号 {TextRevision}，当前文本修订号为 {currentTextRevision}，标注已失效，必须重新校验或生成。");
    }
}

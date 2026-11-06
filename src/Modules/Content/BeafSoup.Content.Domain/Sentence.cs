using BeafSoup.SharedKernel;

namespace BeafSoup.Content;

/// <summary>分词片段：零基 Unicode scalar 起点/长度，不复制原文。</summary>
public sealed record SentenceToken
{
    public int Start { get; }
    public int Length { get; }
    public SentenceTokenKind Kind { get; }
    public Guid? InflectionFormId { get; }

    public SentenceToken(int start, int length, SentenceTokenKind kind, Guid? inflectionFormId = null)
    {
        if (start < 0 || length < 1)
            throw new DomainException(ContentErrorCodes.InvalidTokenRange, "分词起点必须非负且长度必须为正。");
        Start = start;
        Length = length;
        Kind = kind;
        InflectionFormId = inflectionFormId;
    }

    public int End => Start + Length;
}

/// <summary>某语言全文的一次分词结果，绑定 LocalizedText 修订号。</summary>
public sealed class SentenceTokenization
{
    public LanguageCode Language { get; }
    public int TextRevision { get; }
    public IReadOnlyList<SentenceToken> Tokens { get; }

    public SentenceTokenization(LanguageCode language, int textRevision, IReadOnlyList<SentenceToken> tokens, int fullTextLength)
    {
        if (textRevision < 1)
            throw new DomainException(ContentErrorCodes.InvalidTokenCoverage, "文本修订号必须不小于 1。");
        if (!CoversFullText(tokens, fullTextLength))
            throw new DomainException(ContentErrorCodes.InvalidTokenCoverage, "分词必须按序、无重叠且完整覆盖全文。");
        (Language, TextRevision, Tokens) = (language, textRevision, tokens.ToArray());
    }

    private static bool CoversFullText(IReadOnlyList<SentenceToken> tokens, int fullTextLength)
    {
        var position = 0;
        foreach (var token in tokens.OrderBy(t => t.Start))
        {
            if (token.Start != position) return false;
            position = token.End;
        }
        return position == fullTextLength;
    }

    public bool IsCurrentFor(int textRevision) => TextRevision == textRevision;
}

/// <summary>句子实体：全文只存语义键；每种语言独立持有带修订号的分词。</summary>
public sealed class Sentence
{
    public string Id { get; }
    public SemanticKey Key { get; }

    private readonly List<SentenceTokenization> _tokenizations = [];
    public IReadOnlyList<SentenceTokenization> Tokenizations => _tokenizations;

    public Sentence(string id, SemanticKey key)
    {
        Id = SlugId.Validate(id, nameof(id));
        Key = key;
    }

    public SentenceTokenization SetTokenization(LanguageCode language, int textRevision, IReadOnlyList<SentenceToken> tokens, int fullTextLength)
    {
        if (_tokenizations.Any(t => t.Language == language))
            throw new DomainException(ContentErrorCodes.DuplicateTokenization, $"句子 {Id} 已存在语言 {language.Value} 的分词；文本修订后须重建标注。");
        var tokenization = new SentenceTokenization(language, textRevision, tokens, fullTextLength);
        _tokenizations.Add(tokenization);
        return tokenization;
    }
}

using BeafSoup.SharedKernel;

namespace BeafSoup.Vocabulary;

/// <summary>词汇聚合根：一个语义单元（单词/短语），跨语言只存一份，用概念键标识；下挂各语言词元。本类不存任何注释语言的解释文本。</summary>
public sealed class LexicalConcept
{
    public Guid Id { get; }
    public SemanticKey ConceptKey { get; }
    public PartOfSpeech PartOfSpeech { get; }
    public LexemeKind Kind { get; }

    private readonly List<Lexeme> _lexemes = [];
    public IReadOnlyList<Lexeme> Lexemes => _lexemes;

    public LexicalConcept(SemanticKey conceptKey, PartOfSpeech partOfSpeech, LexemeKind kind)
        : this(StableGuid.For($"concept:{conceptKey.Value}"), conceptKey, partOfSpeech, kind, [])
    {
    }

    /// <summary>持久化还原专用：以既有 Id 与词元集合恢复概念；校验每个 (语言) 恰一个主词元。</summary>
    public static LexicalConcept Restore(Guid id, SemanticKey conceptKey, PartOfSpeech partOfSpeech, LexemeKind kind, IReadOnlyList<Lexeme> lexemes)
    {
        if (id == Guid.Empty)
            throw new DomainException(VocabularyErrorCodes.InvalidLexemeId, "概念 Id 不能为空。");
        var concept = new LexicalConcept(id, conceptKey, partOfSpeech, kind, lexemes);
        concept.ValidatePrimaryLexemes();
        return concept;
    }

    private LexicalConcept(Guid id, SemanticKey conceptKey, PartOfSpeech partOfSpeech, LexemeKind kind, IReadOnlyList<Lexeme> lexemes)
        => (Id, ConceptKey, PartOfSpeech, Kind, _lexemes) = (id, conceptKey, partOfSpeech, kind, [..lexemes]);

    /// <summary>添加某语言的一个词元；同语言下 lemma 不可重复，主词元至多一个。短语的 lemma 用 `_` 连接词与词。</summary>
    public Lexeme AddLexeme(
        LanguageCode language, string lemma, bool isPrimary, LexemeRegister register,
        GermanArticle? article, CefrLevel cefr, string? displayWord = null)
    {
        ValidateLemma(lemma);
        if (article is not null && PartOfSpeech != PartOfSpeech.Noun)
            throw new DomainException(VocabularyErrorCodes.ArticleOnlyForNouns, $"冠词仅名词可用：{ConceptKey.Value} 的词性为 {PartOfSpeech}。");
        var normalized = lemma.Trim();
        if (_lexemes.Any(l => l.Language == language && l.Lemma == normalized))
            throw new DomainException(VocabularyErrorCodes.DuplicateLexeme, $"概念 {ConceptKey.Value} 在 {language.Value} 已存在 lemma '{normalized}'。");
        if (isPrimary && _lexemes.Any(l => l.Language == language && l.IsPrimary))
            throw new DomainException(VocabularyErrorCodes.MultiplePrimaryLexemes, $"概念 {ConceptKey.Value} 在 {language.Value} 已存在主词元，每个 (概念, 语言) 至多一个主词元。");

        var lexeme = Lexeme.Restore(
            Lexeme.ComputeId(ConceptKey.Value, language.Value, normalized),
            language, normalized,
            string.IsNullOrWhiteSpace(displayWord) ? normalized : displayWord.Trim(),
            isPrimary, register, article, cefr);
        _lexemes.Add(lexeme);
        return lexeme;
    }

    /// <summary>某语言的主词元（解释的默认来源）；不存在时抛领域异常。</summary>
    public Lexeme PrimaryLexeme(LanguageCode language)
    {
        ValidatePrimaryLexemes();
        return _lexemes.Single(l => l.Language == language && l.IsPrimary);
    }

    private void ValidateLemma(string lemma)
    {
        if (string.IsNullOrWhiteSpace(lemma))
            throw new DomainException(VocabularyErrorCodes.EmptyLemma, "lemma 不能为空。");
        if (lemma.Trim().Any(char.IsWhiteSpace))
            throw new DomainException(VocabularyErrorCodes.LemmaContainsWhitespace, $"lemma '{lemma}' 含空白字符；短语的词与词用 '_' 连接（如 guten_Tag）。");
        // 单词不允许含 '_'；短语的词与词用 '_' 连接（对无语间边界的书写系统如中文，短语 lemma 可不含 '_'）
        if (Kind == LexemeKind.Word && lemma.Contains('_'))
            throw new DomainException(VocabularyErrorCodes.WordLemmaMustNotContainJoiner, $"单词 lemma 不允许含 '_'：{lemma}。多词请建 Phrase 概念。");
    }

    private void ValidatePrimaryLexemes()
    {
        foreach (var group in _lexemes.GroupBy(l => l.Language))
        {
            var primaries = group.Count(l => l.IsPrimary);
            if (primaries == 0)
                throw new DomainException(VocabularyErrorCodes.MissingPrimaryLexeme, $"概念 {ConceptKey.Value} 在 {group.Key.Value} 缺少主词元，每个 (概念, 语言) 恰一个主词元。");
            if (primaries > 1)
                throw new DomainException(VocabularyErrorCodes.MultiplePrimaryLexemes, $"概念 {ConceptKey.Value} 在 {group.Key.Value} 存在 {primaries} 个主词元，每个 (概念, 语言) 恰一个主词元。");
        }
    }
}

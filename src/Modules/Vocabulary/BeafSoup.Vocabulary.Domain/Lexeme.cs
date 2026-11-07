using BeafSoup.SharedKernel;

namespace BeafSoup.Vocabulary;

/// <summary>词元：某概念在一种语言中的一个词典词（含同义/语体变体）。Id 由 (概念键, 语言, lemma) 确定性生成，跨读写稳定。</summary>
public sealed class Lexeme
{
    public Guid Id { get; }
    public LanguageCode Language { get; }
    public string Lemma { get; }
    public string DisplayWord { get; }
    public bool IsPrimary { get; }
    public LexemeRegister Register { get; }
    public GermanArticle? Article { get; }
    public CefrLevel Cefr { get; }

    private readonly List<ParadigmCell> _cells = [];
    public IReadOnlyList<ParadigmCell> Cells => _cells;

    private readonly List<string> _tags = [];
    public IReadOnlyList<string> Tags => _tags;

    private Lexeme(Guid id, LanguageCode language, string lemma, string displayWord, bool isPrimary, LexemeRegister register, GermanArticle? article, CefrLevel cefr)
        => (Id, Language, Lemma, DisplayWord, IsPrimary, Register, Article, Cefr) = (id, language, lemma, displayWord, isPrimary, register, article, cefr);

    /// <summary>持久化还原专用：以既有 Id 重建词元；正常运行路径使用 LexicalConcept.AddLexeme。</summary>
    public static Lexeme Restore(Guid id, LanguageCode language, string lemma, string displayWord, bool isPrimary, LexemeRegister register, GermanArticle? article, CefrLevel cefr)
    {
        if (id == Guid.Empty)
            throw new DomainException(VocabularyErrorCodes.InvalidLexemeId, "词元 Id 不能为空。");
        return new Lexeme(id, language, lemma, displayWord, isPrimary, register, article, cefr);
    }

    /// <summary>词元稳定 Id：由概念键、语言、lemma 共同决定。</summary>
    public static Guid ComputeId(string conceptKey, string languageCode, string lemma)
        => StableGuid.For($"lexeme:{conceptKey}|{languageCode}|{lemma}");

    /// <summary>添加一个范式槽位；同 (范式, 槽位) 不允许重复。Id 由调用方按 ParadigmCell.ComputeId 确定性生成。</summary>
    public ParadigmCell AddParadigmCell(Guid id, string paradigmKey, int slotOrdinal, string surface)
    {
        var key = string.IsNullOrWhiteSpace(paradigmKey) ? paradigmKey : paradigmKey.Trim();
        if (_cells.Any(c => c.ParadigmKey == key && c.SlotOrdinal == slotOrdinal))
            throw new DomainException(VocabularyErrorCodes.DuplicateParadigmCell, $"词元 '{Lemma}' 已存在范式槽位 {key}/{slotOrdinal}。");
        var cell = ParadigmCell.Create(id, paradigmKey, slotOrdinal, surface);
        _cells.Add(cell);
        return cell;
    }

    public void AddTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            throw new DomainException(VocabularyErrorCodes.EmptyTag, "标签不能为空。");
        var normalized = tag.Trim();
        if (_tags.Contains(normalized))
            throw new DomainException(VocabularyErrorCodes.DuplicateTag, $"标签 '{normalized}' 已存在。");
        _tags.Add(normalized);
    }
}

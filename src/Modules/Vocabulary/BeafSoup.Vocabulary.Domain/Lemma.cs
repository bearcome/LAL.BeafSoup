using BeafSoup.SharedKernel;

namespace BeafSoup.Vocabulary;

/// <summary>词元聚合根：一种语言中的一个词典词义。解释走同一语义键的本地化版本，本类不存文本。</summary>
public sealed class Lemma
{
    public Guid Id { get; }
    public LanguageCode Language { get; }
    public SemanticKey LemmaKey { get; }
    public PartOfSpeech PartOfSpeech { get; }
    public GermanArticle? Article { get; }
    public CefrLevel Cefr { get; }

    private readonly List<InflectionForm> _forms = [];
    public IReadOnlyList<InflectionForm> Forms => _forms;

    private readonly List<string> _tags = [];
    public IReadOnlyList<string> Tags => _tags;

    private Lemma(Guid id, LanguageCode language, SemanticKey lemmaKey, PartOfSpeech partOfSpeech, GermanArticle? article, CefrLevel cefr, InflectionForm initialForm)
    {
        (Id, Language, LemmaKey, PartOfSpeech, Article, Cefr) = (id, language, lemmaKey, partOfSpeech, article, cefr);
        _forms.Add(initialForm);
    }

    public static Lemma Create(Guid id, LanguageCode language, SemanticKey lemmaKey, PartOfSpeech partOfSpeech, GermanArticle? article, CefrLevel cefr, InflectionForm initialForm)
    {
        if (initialForm is null)
            throw new DomainException(VocabularyErrorCodes.InitialFormRequired, "词元创建必须携带原形词形。");
        if (initialForm.SurfaceKey != lemmaKey)
            throw new DomainException(VocabularyErrorCodes.InitialFormMustMatchLemmaKey, "原形词形的语义键必须等于词元键（原形本身建模为词形，避免词元卡与形态卡重复）。");
        return new Lemma(id, language, lemmaKey, partOfSpeech, article, cefr, initialForm);
    }

    public InflectionForm AddForm(SemanticKey surfaceKey, IReadOnlyList<GrammaticalAnalysis> analyses)
    {
        var form = InflectionForm.Create(surfaceKey, analyses);
        _forms.Add(form);
        return form;
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

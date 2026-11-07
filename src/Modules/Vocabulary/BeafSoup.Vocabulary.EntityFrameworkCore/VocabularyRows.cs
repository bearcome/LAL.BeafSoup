namespace BeafSoup.Vocabulary.EntityFrameworkCore;

/// <summary>概念行：一个语义单元（单词/短语）的跨语言聚合根。</summary>
public sealed class LexicalConceptRow
{
    public Guid Id { get; set; }
    public string ConceptKey { get; set; } = "";
    public string PartOfSpeech { get; set; } = "";
    public string Kind { get; set; } = "";
    public List<LexemeRow> Lexemes { get; set; } = [];
}

/// <summary>词元行：某概念在一种语言中的一个词典词；Ordinal 在 (概念, 语言) 内保序。</summary>
public sealed class LexemeRow
{
    public Guid Id { get; set; }
    public Guid ConceptId { get; set; }
    public string LanguageCode { get; set; } = "";
    public int Ordinal { get; set; }
    public string Lemma { get; set; } = "";
    public string? DisplayWord { get; set; }
    public bool IsPrimary { get; set; }
    public string Register { get; set; } = "";
    public string? Article { get; set; }
    public string Cefr { get; set; } = "";
    public List<LexemeTagRow> Tags { get; set; } = [];
    public List<ParadigmCellRow> Cells { get; set; } = [];
}

/// <summary>词元受控标签行；Ordinal 保序。</summary>
public sealed class LexemeTagRow
{
    public Guid LexemeId { get; set; }
    public int Ordinal { get; set; }
    public string Tag { get; set; } = "";
}

/// <summary>范式槽位行：范式名 + 槽位序号 + 表层形式；Id 跨读写稳定。</summary>
public sealed class ParadigmCellRow
{
    public Guid Id { get; set; }
    public Guid LexemeId { get; set; }
    public string ParadigmKey { get; set; } = "";
    public int SlotOrdinal { get; set; }
    public string Surface { get; set; } = "";
}

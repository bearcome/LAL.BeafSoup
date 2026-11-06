namespace BeafSoup.Vocabulary.EntityFrameworkCore;

/// <summary>词元行：一种语言中的一个词典词义；解释走本地化文本，本表不存文本。</summary>
public sealed class LemmaRow
{
    public Guid Id { get; set; }
    public string LanguageCode { get; set; } = "";
    public string LemmaKey { get; set; } = "";
    public string PartOfSpeech { get; set; } = "";
    public string? Article { get; set; }
    public string Cefr { get; set; } = "";
    public List<LemmaTagRow> Tags { get; set; } = [];
    public List<InflectionFormRow> Forms { get; set; } = [];
}

/// <summary>词元受控标签行；Ordinal 保序。</summary>
public sealed class LemmaTagRow
{
    public Guid LemmaId { get; set; }
    public int Ordinal { get; set; }
    public string Tag { get; set; } = "";
}

/// <summary>词形行：词元的一个表层形式（含原形本身），关联表层文本语义键；Ordinal 在同词元内保序。</summary>
public sealed class InflectionFormRow
{
    public Guid Id { get; set; }
    public Guid LemmaId { get; set; }
    public int Ordinal { get; set; }
    public string SurfaceKey { get; set; } = "";
    public List<FormAnalysisRow> Analyses { get; set; } = [];
}

/// <summary>词形语法分析行；各维度可空表示不适用，Ordinal 保序。</summary>
public sealed class FormAnalysisRow
{
    public Guid FormId { get; set; }
    public int Ordinal { get; set; }
    public string? Case { get; set; }
    public string? Number { get; set; }
    public string? Person { get; set; }
    public string? Tense { get; set; }
    public string? Gender { get; set; }
    public string? Degree { get; set; }
}

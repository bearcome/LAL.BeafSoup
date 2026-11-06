using BeafSoup.SharedKernel;

namespace BeafSoup.Vocabulary;

/// <summary>词元的一个表层形式（含词典原形本身）；关联表层文本语义键与一个或多个语法分析。</summary>
public sealed class InflectionForm
{
    public Guid Id { get; }
    public SemanticKey SurfaceKey { get; }
    public IReadOnlyList<GrammaticalAnalysis> Analyses { get; }

    private InflectionForm(Guid id, SemanticKey surfaceKey, IReadOnlyList<GrammaticalAnalysis> analyses)
        => (Id, SurfaceKey, Analyses) = (id, surfaceKey, analyses);

    public static InflectionForm Create(SemanticKey surfaceKey, IReadOnlyList<GrammaticalAnalysis> analyses)
    {
        if (analyses.Count == 0)
            throw new DomainException(VocabularyErrorCodes.FormRequiresAnalysis, "词形必须至少携带一个语法分析。");
        if (analyses.Distinct().Count() != analyses.Count)
            throw new DomainException(VocabularyErrorCodes.DuplicateAnalysis, "同一词形不允许重复相同的语法分析。");
        return new InflectionForm(Guid.NewGuid(), surfaceKey, analyses.ToArray());
    }
}

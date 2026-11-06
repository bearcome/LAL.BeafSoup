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

    /// <summary>持久化还原专用：以既有 Id 重建词形，保证 InflectionFormId 跨读写稳定；正常运行路径使用 Create。</summary>
    public static InflectionForm Restore(Guid id, SemanticKey surfaceKey, IReadOnlyList<GrammaticalAnalysis> analyses)
    {
        if (id == Guid.Empty)
            throw new DomainException(VocabularyErrorCodes.InvalidFormId, "词形 Id 不能为空。");
        if (analyses.Count == 0)
            throw new DomainException(VocabularyErrorCodes.FormRequiresAnalysis, "词形必须至少携带一个语法分析。");
        if (analyses.Distinct().Count() != analyses.Count)
            throw new DomainException(VocabularyErrorCodes.DuplicateAnalysis, "同一词形不允许重复相同的语法分析。");
        return new InflectionForm(id, surfaceKey, analyses.ToArray());
    }
}

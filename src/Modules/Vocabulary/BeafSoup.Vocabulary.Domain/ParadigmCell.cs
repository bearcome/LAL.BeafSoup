using BeafSoup.SharedKernel;

namespace BeafSoup.Vocabulary;

/// <summary>词元的一个范式槽位：范式名 + 槽位序号 + 表层形式。Id 由调用方按 (概念键, 语言, lemma, 范式, 槽位) 确定性生成，跨读写稳定。</summary>
public sealed class ParadigmCell
{
    public Guid Id { get; }
    public string ParadigmKey { get; }
    public int SlotOrdinal { get; }
    public string Surface { get; }

    private ParadigmCell(Guid id, string paradigmKey, int slotOrdinal, string surface)
        => (Id, ParadigmKey, SlotOrdinal, Surface) = (id, paradigmKey, slotOrdinal, surface);

    /// <summary>范式槽位稳定 Id：由概念键、语言、lemma、范式名、槽位序号共同决定；形态掌握度与句子标注引用它。</summary>
    public static Guid ComputeId(string conceptKey, string languageCode, string lemma, string paradigmKey, int slotOrdinal)
        => StableGuid.For($"cell:{conceptKey}|{languageCode}|{lemma}|{paradigmKey}|{slotOrdinal}");

    public static ParadigmCell Create(Guid id, string paradigmKey, int slotOrdinal, string surface)
    {
        Validate(id, paradigmKey, slotOrdinal, surface);
        return new ParadigmCell(id, paradigmKey.Trim(), slotOrdinal, surface.Trim());
    }

    private static void Validate(Guid id, string paradigmKey, int slotOrdinal, string surface)
    {
        if (id == Guid.Empty)
            throw new DomainException(VocabularyErrorCodes.InvalidLexemeId, "范式槽位 Id 不能为空。");
        if (string.IsNullOrWhiteSpace(paradigmKey))
            throw new DomainException(VocabularyErrorCodes.EmptyParadigmKey, "范式名不能为空。");
        if (slotOrdinal < 0)
            throw new DomainException(VocabularyErrorCodes.InvalidSlotOrdinal, "槽位序号必须非负。");
        if (string.IsNullOrWhiteSpace(surface))
            throw new DomainException(VocabularyErrorCodes.EmptySurface, "表层形式不能为空。");
    }
}

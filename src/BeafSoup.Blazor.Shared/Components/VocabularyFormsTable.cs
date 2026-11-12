using System.Text.Json;

namespace BeafSoup.Blazor.Shared.Components;

/// <summary>背词卡形态视图：德语动词按“人称（列）× 时态（行）”排成表格，其余范式保持分组平铺。</summary>
public sealed record FormsTable(
    IReadOnlyList<FormsRow> Rows,
    IReadOnlyList<FormsGroup> Groups)
{
    /// <summary>是否存在可排成变位表格的动词范式。</summary>
    public bool HasRows => Rows.Count > 0;
}

/// <summary>变位表格的一行：范式名（如 indicative_present）与按人称槽位顺序排列的形式。</summary>
public sealed record FormsRow(string ParadigmKey, IReadOnlyList<string> Cells);

/// <summary>非动词范式的分组（如名词 declension、形容词 comparison），按原槽位顺序平铺。</summary>
public sealed record FormsGroup(string ParadigmKey, IReadOnlyList<string> Cells);

/// <summary>形态表构表规则：人称列顺序与 docs/domain/model.md 的槽位约定一致（ich/du/er/sie/es/wir/ihr/sie/Sie）。</summary>
public static class VocabularyFormsTable
{
    /// <summary>人称列头语义键，索引即变位范式槽位序号。</summary>
    public static readonly IReadOnlyList<string> PersonKeys =
    [
        "ui.trainer.person.ich",
        "ui.trainer.person.du",
        "ui.trainer.person.er",
        "ui.trainer.person.wir",
        "ui.trainer.person.ihr",
        "ui.trainer.person.sie",
    ];

    /// <summary>动词时态行的固定顺序：现在时 → 过去式 → 现在完成时；未知范式排在最后并保持数据顺序。</summary>
    private static readonly IReadOnlyDictionary<string, int> ParadigmOrder =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["indicative_present"] = 0,
            ["indicative_past"] = 1,
            ["indicative_perfect"] = 2,
        };

    /// <summary>把词元的 forms 对象拆成变位表格行与非动词分组；槽位数不等于人称数的范式不进表格，避免错位。</summary>
    public static FormsTable Build(JsonElement forms, string partOfSpeech)
    {
        var rows = new List<FormsRow>();
        var groups = new List<FormsGroup>();
        var isVerb = string.Equals(partOfSpeech, "Verb", StringComparison.OrdinalIgnoreCase);
        if (forms.ValueKind is not JsonValueKind.Object)
            return new FormsTable(rows, groups);

        foreach (var paradigm in forms.EnumerateObject())
        {
            var cells = paradigm.Value.ValueKind is JsonValueKind.Array
                ? paradigm.Value.EnumerateArray().Select(SlotText).ToArray()
                : [];
            if (isVerb && cells.Length == PersonKeys.Count)
                rows.Add(new FormsRow(paradigm.Name, cells));
            else
                groups.Add(new FormsGroup(paradigm.Name, cells));
        }
        // OrderBy 是稳定排序：已知时态按“现在/过去/完成”排列，未知范式保留原始相对顺序
        return new FormsTable(
            rows.OrderBy(row => ParadigmOrder.GetValueOrDefault(row.ParadigmKey, int.MaxValue)).ToArray(),
            groups);
    }

    private static string SlotText(JsonElement slot)
        => slot.ValueKind is JsonValueKind.String ? slot.GetString() ?? "" : slot.ToString();
}

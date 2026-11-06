using BeafSoup.SharedKernel;

namespace BeafSoup.Vocabulary.EntityFrameworkCore;

/// <summary>词汇模块领域对象与持久化行之间的显式映射；仓储与种子管线共用，保证读写对称。</summary>
public static class VocabularyPersistenceMapper
{
    public static LemmaRow ToRow(Lemma lemma)
        => new()
        {
            Id = lemma.Id,
            LanguageCode = lemma.Language.Value,
            LemmaKey = lemma.LemmaKey.Value,
            PartOfSpeech = lemma.PartOfSpeech.ToString(),
            Article = lemma.Article?.ToString(),
            Cefr = lemma.Cefr.ToString(),
            Tags = lemma.Tags.Select((tag, index) => new LemmaTagRow { LemmaId = lemma.Id, Ordinal = index, Tag = tag }).ToList(),
            Forms = lemma.Forms.Select((form, index) => new InflectionFormRow
            {
                Id = form.Id,
                LemmaId = lemma.Id,
                Ordinal = index,
                SurfaceKey = form.SurfaceKey.Value,
                Analyses = form.Analyses.Select((analysis, index) => new FormAnalysisRow
                {
                    FormId = form.Id,
                    Ordinal = index,
                    Case = analysis.Case?.ToString(),
                    Number = analysis.Number?.ToString(),
                    Person = analysis.Person?.ToString(),
                    Tense = analysis.Tense?.ToString(),
                    Gender = analysis.Gender?.ToString(),
                    Degree = analysis.Degree?.ToString(),
                }).ToList(),
            }).ToList(),
        };

    public static Lemma ToDomain(LemmaRow row)
    {
        var analysesByForm = row.Forms.ToDictionary(
            f => f.Id,
            f => (IReadOnlyList<GrammaticalAnalysis>)f.Analyses
                .OrderBy(a => a.Ordinal)
                .Select(a => new GrammaticalAnalysis(
                    ParseNullable<GrammaticalCase>(a.Case),
                    ParseNullable<GrammaticalNumber>(a.Number),
                    ParseNullable<GrammaticalPerson>(a.Person),
                    ParseNullable<GrammaticalTense>(a.Tense),
                    ParseNullable<GrammaticalGender>(a.Gender),
                    ParseNullable<GrammaticalDegree>(a.Degree)))
                .ToArray());

        var orderedForms = row.Forms.OrderBy(f => f.Ordinal).ToArray();
        // 原形词形的语义键等于词元键（领域不变量），据此还原初始词形
        var initialRow = orderedForms.First(f => f.SurfaceKey == row.LemmaKey);
        var initialForm = InflectionForm.Restore(
            initialRow.Id, SemanticKey.Of(initialRow.SurfaceKey), analysesByForm[initialRow.Id]);

        var lemma = Lemma.Create(
            row.Id,
            LanguageCode.Of(row.LanguageCode),
            SemanticKey.Of(row.LemmaKey),
            Enum.Parse<PartOfSpeech>(row.PartOfSpeech),
            row.Article is null ? null : Enum.Parse<GermanArticle>(row.Article),
            Enum.Parse<CefrLevel>(row.Cefr),
            initialForm);

        foreach (var tag in row.Tags.OrderBy(t => t.Ordinal))
            lemma.AddTag(tag.Tag);
        foreach (var formRow in orderedForms.Skip(1))
            lemma.RestoreForm(SemanticKey.Of(formRow.SurfaceKey), formRow.Id, analysesByForm[formRow.Id]);

        return lemma;
    }

    private static TEnum? ParseNullable<TEnum>(string? value) where TEnum : struct
        => value is null ? null : Enum.Parse<TEnum>(value);
}

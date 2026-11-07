using BeafSoup.SharedKernel;

namespace BeafSoup.Vocabulary.EntityFrameworkCore;

/// <summary>词汇模块领域对象与持久化行之间的显式映射；仓储与种子管线共用，保证读写对称。Id 全部按领域规则确定性生成，读写往返不变。</summary>
public static class VocabularyPersistenceMapper
{
    public static LexicalConceptRow ToRow(LexicalConcept concept)
        => new()
        {
            Id = concept.Id,
            ConceptKey = concept.ConceptKey.Value,
            PartOfSpeech = concept.PartOfSpeech.ToString(),
            Kind = concept.Kind.ToString(),
            Lexemes = concept.Lexemes.Select((lexeme, index) => new LexemeRow
            {
                Id = lexeme.Id,
                ConceptId = concept.Id,
                LanguageCode = lexeme.Language.Value,
                Ordinal = index,
                Lemma = lexeme.Lemma,
                DisplayWord = lexeme.DisplayWord == lexeme.Lemma ? null : lexeme.DisplayWord,
                IsPrimary = lexeme.IsPrimary,
                Register = lexeme.Register.ToString(),
                Article = lexeme.Article?.ToString(),
                Cefr = lexeme.Cefr.ToString(),
                Tags = lexeme.Tags.Select((tag, tagIndex) => new LexemeTagRow { LexemeId = lexeme.Id, Ordinal = tagIndex, Tag = tag }).ToList(),
                Cells = lexeme.Cells.Select(c => new ParadigmCellRow
                {
                    Id = c.Id,
                    LexemeId = lexeme.Id,
                    ParadigmKey = c.ParadigmKey,
                    SlotOrdinal = c.SlotOrdinal,
                    Surface = c.Surface,
                }).ToList(),
            }).ToList(),
        };

    public static LexicalConcept ToDomain(LexicalConceptRow row)
    {
        var lexemes = row.Lexemes
            .OrderBy(l => l.Ordinal)
            .Select(l =>
            {
                var lexeme = Lexeme.Restore(
                    l.Id,
                    LanguageCode.Of(l.LanguageCode),
                    l.Lemma,
                    l.DisplayWord ?? l.Lemma,
                    l.IsPrimary,
                    Enum.Parse<LexemeRegister>(l.Register),
                    l.Article is null ? null : Enum.Parse<GermanArticle>(l.Article),
                    Enum.Parse<CefrLevel>(l.Cefr));
                foreach (var tag in l.Tags.OrderBy(t => t.Ordinal))
                    lexeme.AddTag(tag.Tag);
                foreach (var cell in l.Cells.OrderBy(c => c.SlotOrdinal))
                    lexeme.AddParadigmCell(cell.Id, cell.ParadigmKey, cell.SlotOrdinal, cell.Surface);
                return lexeme;
            })
            .ToArray();
        return LexicalConcept.Restore(
            row.Id,
            SemanticKey.Of(row.ConceptKey),
            Enum.Parse<PartOfSpeech>(row.PartOfSpeech),
            Enum.Parse<LexemeKind>(row.Kind),
            lexemes);
    }
}

using BeafSoup.Audio.Generation.Seed;
using BeafSoup.Content;
using BeafSoup.Localization;
using BeafSoup.SharedKernel;
using BeafSoup.Vocabulary;

namespace BeafSoup.Audio.Generation;

public sealed record ValidatedSeed(
    IReadOnlyList<VoiceRoleConfig> VoiceRoles,
    IReadOnlyList<LexicalConcept> Concepts,
    IReadOnlyList<Sentence> Sentences,
    IReadOnlyList<Dialogue> Dialogues,
    IReadOnlyList<Course> Courses,
    IReadOnlyList<LocalizedText> LocalizedTexts,
    IReadOnlyDictionary<(string ConceptKey, string Surface), Guid> CellIdByRef);

public sealed record ValidationResult(bool IsValid, IReadOnlyList<string> Errors, ValidatedSeed? Value);

/// <summary>种子 → 领域对象的映射与校验：全部错误收集后一次性报告，不写半成品。</summary>
public static class SeedValidator
{
    public static ValidationResult Validate(SeedData seed)
    {
        var errors = new List<string>();
        var localized = new List<LocalizedText>();
        var cellIdByRef = new Dictionary<(string, string), Guid>();

        var voiceRoles = MapVoiceRoles(seed, errors);
        var concepts = MapConcepts(seed, errors, cellIdByRef);
        var sentences = MapSentences(seed, errors, localized);
        var dialogues = MapDialogues(seed, errors);
        var courses = MapCourses(seed, errors);

        ValidateDialogueReferences(seed, sentences, errors);
        ValidateTokenFormReferences(seed, cellIdByRef, errors);

        return errors.Count > 0
            ? new ValidationResult(false, errors, null)
            : new ValidationResult(true, [], new ValidatedSeed(voiceRoles, concepts, sentences, dialogues, courses, localized, cellIdByRef));
    }

    private static IReadOnlyList<VoiceRoleConfig> MapVoiceRoles(SeedData seed, List<string> errors)
    {
        var result = new List<VoiceRoleConfig>();
        foreach (var group in seed.VoiceRoles.GroupBy(v => v.Language))
        {
            try
            {
                var configs = group.Select(v => VoiceRoleConfig.Create(
                    LanguageCode.Of(group.Key),
                    Enum.Parse<VoiceRole>(v.Role),
                    v.AssetFileToken,
                    v.TtsVoiceName)).ToList();
                result.AddRange(VoiceRoleConfig.EnsureCompleteSet(LanguageCode.Of(group.Key), configs));
            }
            catch (Exception ex) when (ex is DomainException or ArgumentException)
            {
                errors.Add($"voices.json[{group.Key}]：{ex.Message}");
            }
        }
        return result;
    }

    private static IReadOnlyList<LexicalConcept> MapConcepts(
        SeedData seed, List<string> errors, Dictionary<(string, string), Guid> cellIdByRef)
    {
        var result = new List<LexicalConcept>();
        foreach (var seedConcept in seed.Concepts)
        {
            try
            {
                var concept = new LexicalConcept(
                    SemanticKey.Of(seedConcept.Key),
                    Enum.Parse<PartOfSpeech>(seedConcept.PartOfSpeech),
                    Enum.Parse<LexemeKind>(seedConcept.Kind));

                foreach (var (language, lexemes) in seedConcept.Lexemes)
                {
                    var languageCode = LanguageCode.Of(language);
                    foreach (var seedLexeme in lexemes)
                    {
                        var lexeme = concept.AddLexeme(
                            languageCode,
                            seedLexeme.Lemma,
                            seedLexeme.IsPrimary,
                            Enum.Parse<LexemeRegister>(seedLexeme.Register),
                            seedLexeme.Article is null ? null : Enum.Parse<GermanArticle>(seedLexeme.Article),
                            Enum.Parse<CefrLevel>(seedLexeme.Cefr),
                            seedLexeme.Display);
                        foreach (var tag in seedLexeme.Tags)
                            lexeme.AddTag(tag);
                        foreach (var (paradigmKey, surfaces) in seedLexeme.Forms)
                        {
                            for (var slot = 0; slot < surfaces.Count; slot++)
                            {
                                var cell = lexeme.AddParadigmCell(
                                    ParadigmCell.ComputeId(seedConcept.Key, language, seedLexeme.Lemma, paradigmKey, slot),
                                    paradigmKey, slot, surfaces[slot]);
                                cellIdByRef[(seedConcept.Key, surfaces[slot])] = cell.Id;
                            }
                        }
                    }
                }
                result.Add(concept);
            }
            catch (Exception ex) when (ex is DomainException or ArgumentException or InvalidOperationException)
            {
                errors.Add($"concepts.json[{seedConcept.Key}]：{ex.Message}");
            }
        }
        return result;
    }

    private static IReadOnlyList<Sentence> MapSentences(SeedData seed, List<string> errors, List<LocalizedText> localized)
    {
        var result = new List<Sentence>();
        foreach (var seedSentence in seed.Sentences)
        {
            try
            {
                var key = SemanticKey.Of(seedSentence.Key);
                var sentence = new Sentence(seedSentence.Id, key);
                foreach (var loc in seedSentence.Localizations)
                {
                    localized.Add(LocalizedText.Create(key, LanguageCode.Of(loc.Language), loc.Text));
                    ApplyTokenization(sentence, loc, errors);
                }
                result.Add(sentence);
            }
            catch (Exception ex) when (ex is DomainException or ArgumentException)
            {
                errors.Add($"sentences.json[{seedSentence.Id}]：{ex.Message}");
            }
        }
        return result;

        static void ApplyTokenization(Sentence sentence, SeedSentenceLocalization loc, List<string> errors)
        {
            // 空 token 列表 = 该语言尚未分词（如 zh-CN），跳过覆盖校验
            if (loc.Tokens.Count == 0)
                return;
            try
            {
                var tokens = loc.Tokens
                    .Select(t => new SentenceToken(t.Start, t.Length, Enum.Parse<SentenceTokenKind>(t.Kind)))
                    .ToArray();
                sentence.SetTokenization(LanguageCode.Of(loc.Language), loc.Revision, tokens, loc.Text.Length);
            }
            catch (Exception ex) when (ex is DomainException or ArgumentException)
            {
                errors.Add($"sentences.json[{sentence.Id}/{loc.Language}]：{ex.Message}");
            }
        }
    }

    private static IReadOnlyList<Dialogue> MapDialogues(SeedData seed, List<string> errors)
    {
        var result = new List<Dialogue>();
        foreach (var seedDialogue in seed.Dialogues)
        {
            try
            {
                var dialogue = new Dialogue(seedDialogue.Id);
                foreach (var line in seedDialogue.Lines)
                    dialogue.AddLine(line.SpeakerLabel, line.SentenceId, Enum.Parse<VoiceRole>(line.VoiceRole));
                result.Add(dialogue);
            }
            catch (Exception ex) when (ex is DomainException or ArgumentException)
            {
                errors.Add($"dialogues.json[{seedDialogue.Id}]：{ex.Message}");
            }
        }
        return result;
    }

    private static IReadOnlyList<Course> MapCourses(SeedData seed, List<string> errors)
    {
        var result = new List<Course>();
        foreach (var seedCourse in seed.Courses)
        {
            try
            {
                var course = new Course(seedCourse.Id, SemanticKey.Of(seedCourse.Key));
                foreach (var seedUnit in seedCourse.Units)
                {
                    var unit = new SkillUnit(seedUnit.Id, SemanticKey.Of(seedUnit.Key));
                    foreach (var seedLesson in seedUnit.Lessons)
                        unit.AddLesson(new Lesson(seedLesson.Id, SemanticKey.Of(seedLesson.Key)));
                    course.AddSkillUnit(unit);
                }
                result.Add(course);
            }
            catch (Exception ex) when (ex is DomainException or ArgumentException)
            {
                errors.Add($"courses.json[{seedCourse.Id}]：{ex.Message}");
            }
        }
        return result;
    }

    private static void ValidateDialogueReferences(SeedData seed, IReadOnlyList<Sentence> sentences, List<string> errors)
    {
        var known = sentences.Select(s => s.Id).ToHashSet();
        foreach (var dialogue in seed.Dialogues)
        {
            foreach (var line in dialogue.Lines)
            {
                if (!known.Contains(line.SentenceId))
                    errors.Add($"dialogues.json[{dialogue.Id}]：引用了不存在的句子 {line.SentenceId}。");
            }
        }
    }

    private static void ValidateTokenFormReferences(
        SeedData seed, IReadOnlyDictionary<(string, string), Guid> cellIdByRef, List<string> errors)
    {
        foreach (var sentence in seed.Sentences)
        {
            foreach (var token in sentence.Localizations.SelectMany(l => l.Tokens))
            {
                if (token.ConceptKey is null && token.FormSurface is null)
                    continue;
                if (token.ConceptKey is null || token.FormSurface is null)
                {
                    errors.Add($"sentences.json[{sentence.Id}]：词形链接必须同时给出 conceptKey 与 formSurface。");
                    continue;
                }
                if (!cellIdByRef.ContainsKey((token.ConceptKey, token.FormSurface)))
                    errors.Add($"sentences.json[{sentence.Id}]：词形链接 {token.ConceptKey}/{token.FormSurface} 在 concepts.json 中不存在（须命中某个范式槽位）。");
            }
        }
    }
}

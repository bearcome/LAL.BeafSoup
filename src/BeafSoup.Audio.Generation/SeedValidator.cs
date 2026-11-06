using BeafSoup.Audio.Generation.Seed;
using BeafSoup.Content;
using BeafSoup.Localization;
using BeafSoup.SharedKernel;
using BeafSoup.Vocabulary;

namespace BeafSoup.Audio.Generation;

public sealed record ValidatedSeed(
    IReadOnlyList<VoiceRoleConfig> VoiceRoles,
    IReadOnlyList<Lemma> Lemmas,
    IReadOnlyList<Sentence> Sentences,
    IReadOnlyList<Dialogue> Dialogues,
    IReadOnlyList<Course> Courses,
    IReadOnlyList<LocalizedText> LocalizedTexts,
    IReadOnlyDictionary<(string LemmaKey, string Surface), Guid> FormIdByRef);

public sealed record ValidationResult(bool IsValid, IReadOnlyList<string> Errors, ValidatedSeed? Value);

/// <summary>种子 → 领域对象的映射与校验：全部错误收集后一次性报告，不写半成品。</summary>
public static class SeedValidator
{
    public static ValidationResult Validate(SeedData seed)
    {
        var errors = new List<string>();
        var localized = new List<LocalizedText>();
        var formIdByRef = new Dictionary<(string, string), Guid>();

        var voiceRoles = MapVoiceRoles(seed, errors);
        var lemmas = MapLemmas(seed, errors, localized, formIdByRef);
        var sentences = MapSentences(seed, errors, localized);
        var dialogues = MapDialogues(seed, errors);
        var courses = MapCourses(seed, errors);

        ValidateDialogueReferences(seed, sentences, errors);
        ValidateTokenFormReferences(seed, formIdByRef, errors);

        return errors.Count > 0
            ? new ValidationResult(false, errors, null)
            : new ValidationResult(true, [], new ValidatedSeed(voiceRoles, lemmas, sentences, dialogues, courses, localized, formIdByRef));
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

    private static IReadOnlyList<Lemma> MapLemmas(
        SeedData seed, List<string> errors, List<LocalizedText> localized, Dictionary<(string, string), Guid> formIdByRef)
    {
        var result = new List<Lemma>();
        foreach (var seedLemma in seed.Lemmas)
        {
            try
            {
                var language = LanguageCode.Of(seedLemma.Language);
                var lemmaKey = SemanticKey.Of(seedLemma.Key);
                foreach (var (lang, text) in seedLemma.Translations)
                    localized.Add(LocalizedText.Create(lemmaKey, LanguageCode.Of(lang), text));

                var firstForm = seedLemma.Forms.First();
                var initialForm = InflectionForm.Create(lemmaKey, [MapAnalysis(firstForm.Analyses.First())]);
                var lemma = Lemma.Create(
                    StableId.For(seedLemma.Key), language, lemmaKey,
                    Enum.Parse<PartOfSpeech>(seedLemma.PartOfSpeech),
                    seedLemma.Article is null ? null : Enum.Parse<GermanArticle>(seedLemma.Article),
                    Enum.Parse<CefrLevel>(seedLemma.Cefr),
                    initialForm);
                formIdByRef[(seedLemma.Key, firstForm.Surface)] = initialForm.Id;

                foreach (var tag in seedLemma.Tags)
                    lemma.AddTag(tag);
                foreach (var form in seedLemma.Forms.Skip(1))
                {
                    var mapped = lemma.AddForm(SemanticKey.Of(form.Key), form.Analyses.Select(MapAnalysis).ToArray());
                    formIdByRef[(seedLemma.Key, form.Surface)] = mapped.Id;
                    localized.Add(LocalizedText.Create(SemanticKey.Of(form.Key), language, form.Surface));
                }
                result.Add(lemma);
            }
            catch (Exception ex) when (ex is DomainException or ArgumentException or InvalidOperationException)
            {
                errors.Add($"lemmas.json[{seedLemma.Key}]：{ex.Message}");
            }
        }
        return result;
    }

    private static GrammaticalAnalysis MapAnalysis(SeedAnalysis a) => new(
        Case: ParseEnum<GrammaticalCase>(a.Case),
        Number: ParseEnum<GrammaticalNumber>(a.Number),
        Person: ParseEnum<GrammaticalPerson>(a.Person),
        Tense: ParseEnum<GrammaticalTense>(a.Tense),
        Gender: ParseEnum<GrammaticalGender>(a.Gender),
        Degree: ParseEnum<GrammaticalDegree>(a.Degree));

    private static TEnum? ParseEnum<TEnum>(string? value) where TEnum : struct
        => value is null ? null : Enum.Parse<TEnum>(value);

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
        SeedData seed, IReadOnlyDictionary<(string, string), Guid> formIdByRef, List<string> errors)
    {
        foreach (var sentence in seed.Sentences)
        {
            foreach (var token in sentence.Localizations.SelectMany(l => l.Tokens))
            {
                if (token.FormLemmaKey is null && token.FormSurface is null)
                    continue;
                if (token.FormLemmaKey is null || token.FormSurface is null)
                {
                    errors.Add($"sentences.json[{sentence.Id}]：词形链接必须同时给出 lemmaKey 与 surface。");
                    continue;
                }
                if (!formIdByRef.ContainsKey((token.FormLemmaKey, token.FormSurface)))
                    errors.Add($"sentences.json[{sentence.Id}]：词形链接 {token.FormLemmaKey}/{token.FormSurface} 在 lemmas.json 中不存在。");
            }
        }
    }
}

using BeafSoup.Audio.Generation.Seed;
using BeafSoup.Content;
using BeafSoup.Exercises;
using BeafSoup.Localization;
using BeafSoup.SharedKernel;
using BeafSoup.Vocabulary;

namespace BeafSoup.Audio.Generation;

public sealed record ValidatedSeed(
    IReadOnlyList<VoiceRoleConfig> VoiceRoles,
    IReadOnlyList<LexicalConcept> Concepts,
    IReadOnlyList<Sentence> Sentences,
    IReadOnlyList<Dialogue> Dialogues,
    IReadOnlyList<Essay> Essays,
    IReadOnlyList<Course> Courses,
    IReadOnlyList<LocalizedText> LocalizedTexts,
    IReadOnlyList<SemanticKey> UiKeys,
    IReadOnlyList<ExerciseInstance> Exercises,
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
        var sentences = MapSentences(seed, errors, localized, cellIdByRef);
        var dialogues = MapDialogues(seed, errors);
        var essays = MapEssays(seed, errors);
        var courses = MapCourses(seed, errors, localized);
        var uiKeys = MapUiTexts(seed, errors, localized);
        var exercises = MapExercises(seed, errors, cellIdByRef);

        ValidateDialogueReferences(seed, sentences, errors);
        ValidateEssayReferences(seed, sentences, errors);
        ValidateTokenFormReferences(seed, cellIdByRef, errors);

        return errors.Count > 0
            ? new ValidationResult(false, errors, null)
            : new ValidationResult(true, [], new ValidatedSeed(voiceRoles, concepts, sentences, dialogues, essays, courses, localized, uiKeys, exercises, cellIdByRef));
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

    private static IReadOnlyList<Sentence> MapSentences(
        SeedData seed, List<string> errors, List<LocalizedText> localized,
        IReadOnlyDictionary<(string ConceptKey, string Surface), Guid> cellIdByRef)
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
                    ApplyTokenization(sentence, loc, cellIdByRef);
                }
                result.Add(sentence);
            }
            catch (Exception ex) when (ex is DomainException or ArgumentException)
            {
                errors.Add($"sentences.json[{seedSentence.Id}]：{ex.Message}");
            }
        }
        return result;

        static void ApplyTokenization(
            Sentence sentence, SeedSentenceLocalization loc,
            IReadOnlyDictionary<(string ConceptKey, string Surface), Guid> cellIdByRef)
        {
            // 空 token 列表 = 该语言尚未分词（如 zh-CN），跳过覆盖校验
            if (loc.Tokens.Count == 0)
                return;
            // 词形链接在发布期解析为稳定槽位 ID 与概念键；未命中由后续 ValidateTokenFormReferences 统一报告
            var tokens = loc.Tokens
                .Select(t => new SentenceToken(
                    t.Start, t.Length, Enum.Parse<SentenceTokenKind>(t.Kind),
                    t.ConceptKey is not null && t.FormSurface is not null
                        && cellIdByRef.TryGetValue((t.ConceptKey, t.FormSurface), out var cellId)
                        ? cellId
                        : null,
                    t.ConceptKey is null ? null : SemanticKey.Of(t.ConceptKey)))
                .ToArray();
            sentence.SetTokenization(LanguageCode.Of(loc.Language), loc.Revision, tokens, loc.Text.Length);
        }
    }

    private static IReadOnlyList<Essay> MapEssays(SeedData seed, List<string> errors)
    {
        var result = new List<Essay>();
        foreach (var seedEssay in seed.Essays)
        {
            try
            {
                var essay = new Essay(seedEssay.Id);
                foreach (var sentenceId in seedEssay.SentenceIds)
                    essay.AppendSentence(sentenceId);
                result.Add(essay);
            }
            catch (Exception ex) when (ex is DomainException or ArgumentException)
            {
                errors.Add($"essays.json[{seedEssay.Id}]：{ex.Message}");
            }
        }
        return result;
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

    private static IReadOnlyList<Course> MapCourses(SeedData seed, List<string> errors, List<LocalizedText> localized)
    {
        var exercisesBySeedId = seed.Exercises.ToDictionary(e => e.Id, e => e);
        var result = new List<Course>();
        foreach (var seedCourse in seed.Courses)
        {
            try
            {
                var course = new Course(seedCourse.Id, SemanticKey.Of(seedCourse.Key));
                AddTitleTexts(seedCourse.Key, seedCourse.TitleLocalizations, localized, errors, $"courses.json[{seedCourse.Id}]");
                foreach (var seedUnit in seedCourse.Units)
                {
                    var unit = new SkillUnit(seedUnit.Id, SemanticKey.Of(seedUnit.Key));
                    AddTitleTexts(seedUnit.Key, seedUnit.TitleLocalizations, localized, errors, $"courses.json[{seedUnit.Id}]");
                    foreach (var seedLesson in seedUnit.Lessons)
                    {
                        var lesson = new Lesson(seedLesson.Id, SemanticKey.Of(seedLesson.Key));
                        AddTitleTexts(seedLesson.Key, seedLesson.TitleLocalizations, localized, errors, $"courses.json[{seedLesson.Id}]");
                        foreach (var exerciseRef in seedLesson.Exercises)
                        {
                            var instanceId = StableGuid.For($"exercise:{exerciseRef.InstanceId}");
                            var instance = exercisesBySeedId.GetValueOrDefault(exerciseRef.InstanceId);
                            if (instance is null)
                                throw new DomainException(ContentErrorCodes.EmptyExerciseTypeKey,
                                    $"课时 {seedLesson.Id} 引用了不存在的习题 {exerciseRef.InstanceId}。");
                            if (instance.TypeKey != exerciseRef.TypeKey)
                                throw new DomainException(ContentErrorCodes.EmptyExerciseTypeKey,
                                    $"课时 {seedLesson.Id} 习题 {exerciseRef.InstanceId} 题型键不匹配（{exerciseRef.TypeKey} ≠ {instance.TypeKey}）。");
                            lesson.Content.AddExercise(instanceId, exerciseRef.TypeKey);
                        }
                        unit.AddLesson(lesson);
                    }
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

    private static void AddTitleTexts(string key, IReadOnlyList<SeedTitleLocalization> localizations,
        List<LocalizedText> localized, List<string> errors, string location)
    {
        foreach (var title in localizations)
        {
            try
            {
                localized.Add(LocalizedText.Create(SemanticKey.Of(key), LanguageCode.Of(title.Language), title.Text));
            }
            catch (Exception ex) when (ex is DomainException or ArgumentException)
            {
                errors.Add($"{location}：{ex.Message}");
            }
        }
    }

    /// <summary>习题种子 → 领域实例：经题型注册表全量校验（发布即失败原则）；correctCell 引用解析为稳定 ParadigmCellId。</summary>
    private static IReadOnlyList<ExerciseInstance> MapExercises(
        SeedData seed, List<string> errors, IReadOnlyDictionary<(string ConceptKey, string Surface), Guid> cellIdByRef)
    {
        var registry = new ExerciseRegistry(
        [
            new WordDefinitionMcqHandler(), new SentenceClozeMcqHandler(), new SentenceClozeInputHandler(),
            new SentenceTranslationOrderHandler(), new DialogueListeningMcqHandler(), new DialogueClozeHandler(),
        ]);
        var result = new List<ExerciseInstance>();
        foreach (var seedExercise in seed.Exercises)
        {
            try
            {
                var instanceId = StableGuid.For($"exercise:{seedExercise.Id}");
                // correctCell { conceptKey, surface } → 稳定词形 ID 注入负载
                var payloadNode = System.Text.Json.Nodes.JsonNode.Parse(seedExercise.Payload.GetRawText())
                    ?? throw new DomainException(ExerciseErrorCodes.InvalidPayload, "习题负载为空。");
                if (payloadNode["correctCell"] is { } cellRef)
                {
                    var conceptKey = cellRef["conceptKey"]?.GetValue<string>()
                        ?? throw new DomainException(ExerciseErrorCodes.InvalidPayload, "correctCell 缺少 conceptKey。");
                    var surface = cellRef["surface"]?.GetValue<string>()
                        ?? throw new DomainException(ExerciseErrorCodes.InvalidPayload, "correctCell 缺少 surface。");
                    if (cellIdByRef.TryGetValue((conceptKey, surface), out var cellId))
                    {
                        payloadNode["correctCellId"] = cellId.ToString();
                    }
                    else if (!IsPhraseConcept(seed, conceptKey))
                    {
                        throw new DomainException(ExerciseErrorCodes.InvalidPayload,
                            $"习题 {seedExercise.Id} 的 correctCell ({conceptKey}, {surface}) 未命中任何范式槽位。");
                    }
                    // 短语无范式槽位：保留 conceptKey 供复习关联，不注入槽位 ID
                    payloadNode.AsObject().Remove("correctCell");
                }
                var payloadJson = payloadNode.ToJsonString();
                var version = payloadNode["version"]?.GetValue<int>()
                    ?? throw new DomainException(ExerciseErrorCodes.UnsupportedPayloadVersion, "习题负载缺少 version。");
                var handler = registry.Resolve(seedExercise.TypeKey, version);
                using var document = System.Text.Json.JsonDocument.Parse(payloadJson);
                handler.ValidatePayload(document.RootElement.Clone());
                result.Add(new ExerciseInstance(instanceId, seedExercise.TypeKey, version, payloadJson, seedExercise.Difficulty));
            }
            catch (Exception ex) when (ex is DomainException or ArgumentException or System.Text.Json.JsonException)
            {
                errors.Add($"exercises.json[{seedExercise.Id}]：{ex.Message}");
            }
        }
        return result;
    }

    private static IReadOnlyList<SemanticKey> MapUiTexts(SeedData seed, List<string> errors, List<LocalizedText> localized)
    {
        var result = new List<SemanticKey>();
        foreach (var uiText in seed.UiTexts)
        {
            try
            {
                var key = SemanticKey.Of(uiText.Key);
                foreach (var loc in uiText.Localizations)
                    localized.Add(LocalizedText.Create(key, LanguageCode.Of(loc.Language), loc.Text));
                result.Add(key);
            }
            catch (Exception ex) when (ex is DomainException or ArgumentException)
            {
                errors.Add($"ui-texts.json[{uiText.Key}]：{ex.Message}");
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

    private static void ValidateEssayReferences(SeedData seed, IReadOnlyList<Sentence> sentences, List<string> errors)
    {
        var known = sentences.Select(s => s.Id).ToHashSet();
        foreach (var essay in seed.Essays)
        {
            foreach (var sentenceId in essay.SentenceIds)
            {
                if (!known.Contains(sentenceId))
                    errors.Add($"essays.json[{essay.Id}]：引用了不存在的句子 {sentenceId}。");
            }
            if (essay.SentenceIds.Count == 0)
                errors.Add($"essays.json[{essay.Id}]：短文至少需要一个句子。");
            if (essay.SentenceIds.Distinct().Count() != essay.SentenceIds.Count)
                errors.Add($"essays.json[{essay.Id}]：短文句子不能重复。");
        }
    }

    /// <summary>短语概念没有任何形态范式：练习只记录概念键，不注入范式槽位 ID。</summary>
    private static bool IsPhraseConcept(SeedData seed, string conceptKey)
    {
        var concept = seed.Concepts.FirstOrDefault(c => c.Key == conceptKey);
        return concept is not null
            && !concept.Lexemes.Values.SelectMany(lexemes => lexemes).Any(lexeme => lexeme.Forms.Count > 0);
    }

    private static void ValidateTokenFormReferences(
        SeedData seed, IReadOnlyDictionary<(string, string), Guid> cellIdByRef, List<string> errors)
    {
        // 概念集合：短语概念没有形态范式，允许只标注概念键（formSurface 为短语原文）
        var conceptsByKey = seed.Concepts.ToDictionary(c => c.Key, c => c);
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
                if (!conceptsByKey.TryGetValue(token.ConceptKey, out var concept))
                {
                    errors.Add($"sentences.json[{sentence.Id}]：词形链接 {token.ConceptKey} 在 concepts.json 中不存在。");
                    continue;
                }
                if (cellIdByRef.ContainsKey((token.ConceptKey, token.FormSurface)))
                    continue;
                // 短语无范式槽位：标注只记录概念键，供词↔句互链使用
                var hasForms = concept.Lexemes.Values
                    .SelectMany(lexemes => lexemes)
                    .Any(lexeme => lexeme.Forms.Count > 0);
                if (hasForms)
                    errors.Add($"sentences.json[{sentence.Id}]：词形链接 {token.ConceptKey}/{token.FormSurface} 在 concepts.json 中不存在（须命中某个范式槽位）。");
            }
        }
    }
}

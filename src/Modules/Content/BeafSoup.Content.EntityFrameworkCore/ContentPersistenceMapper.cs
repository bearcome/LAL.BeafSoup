using BeafSoup.SharedKernel;

namespace BeafSoup.Content.EntityFrameworkCore;

/// <summary>内容模块领域对象与持久化行之间的显式映射；仓储与种子管线共用，保证读写对称。</summary>
public static class ContentPersistenceMapper
{
    public static SentenceRow ToRow(Sentence sentence)
        => new()
        {
            Id = sentence.Id,
            Key = sentence.Key.Value,
            Tokenizations = sentence.Tokenizations.Select(t => new SentenceTokenizationRow
            {
                SentenceId = sentence.Id,
                LanguageCode = t.Language.Value,
                TextRevision = t.TextRevision,
                Tokens = t.Tokens.Select((token, index) => new SentenceTokenRow
                {
                    SentenceId = sentence.Id,
                    LanguageCode = t.Language.Value,
                    Ordinal = index,
                    Start = token.Start,
                    Length = token.Length,
                    Kind = token.Kind.ToString(),
                    FormCellId = token.FormCellId,
                }).ToList(),
            }).ToList(),
        };

    public static Sentence ToDomain(SentenceRow row)
    {
        var sentence = new Sentence(row.Id, SemanticKey.Of(row.Key));
        foreach (var tokenization in row.Tokenizations)
        {
            var tokens = tokenization.Tokens
                .OrderBy(t => t.Ordinal)
                .Select(t => new SentenceToken(t.Start, t.Length, Enum.Parse<SentenceTokenKind>(t.Kind), t.FormCellId))
                .ToArray();
            // 覆盖性由领域构造校验；全文长度由末片段终点还原
            var fullTextLength = tokens.Max(t => t.End);
            sentence.SetTokenization(
                LanguageCode.Of(tokenization.LanguageCode), tokenization.TextRevision, tokens, fullTextLength);
        }
        return sentence;
    }

    public static DialogueRow ToRow(Dialogue dialogue)
        => new()
        {
            Id = dialogue.Id,
            Lines = dialogue.Lines.Select((line, index) => new DialogueLineRow
            {
                DialogueId = dialogue.Id,
                Ordinal = index,
                SpeakerLabel = line.SpeakerLabel,
                SentenceId = line.SentenceId,
                VoiceRole = line.VoiceRole.ToString(),
            }).ToList(),
        };

    public static Dialogue ToDomain(DialogueRow row)
    {
        var dialogue = new Dialogue(row.Id);
        foreach (var line in row.Lines.OrderBy(l => l.Ordinal))
            dialogue.AddLine(line.SpeakerLabel, line.SentenceId, Enum.Parse<VoiceRole>(line.VoiceRole));
        return dialogue;
    }

    public static EssayRow ToRow(Essay essay)
        => new()
        {
            Id = essay.Id,
            Items = essay.SentenceIds.Select((sentenceId, index) => new EssaySentenceRow
            {
                EssayId = essay.Id,
                Ordinal = index,
                SentenceId = sentenceId,
            }).ToList(),
        };

    public static Essay ToDomain(EssayRow row)
    {
        var essay = new Essay(row.Id);
        foreach (var item in row.Items.OrderBy(i => i.Ordinal))
            essay.AppendSentence(item.SentenceId);
        return essay;
    }

    public static CourseRow ToRow(Course course)
        => new()
        {
            Id = course.Id,
            TitleKey = course.TitleKey.Value,
            Units = course.Units.Select((unit, unitIndex) => new SkillUnitRow
            {
                Id = unit.Id,
                CourseId = course.Id,
                Ordinal = unitIndex,
                TitleKey = unit.TitleKey.Value,
                Lessons = unit.Lessons.Select((lesson, lessonIndex) => new LessonRow
                {
                    Id = lesson.Id,
                    SkillUnitId = unit.Id,
                    Ordinal = lessonIndex,
                    TitleKey = lesson.TitleKey.Value,
                    Exercises = lesson.Content.Refs.Select((reference, exerciseIndex) => new LessonExerciseRow
                    {
                        LessonId = lesson.Id,
                        Ordinal = exerciseIndex,
                        ExerciseInstanceId = reference.ExerciseInstanceId,
                        ExerciseTypeKey = reference.ExerciseTypeKey,
                    }).ToList(),
                }).ToList(),
            }).ToList(),
        };

    public static Course ToDomain(CourseRow row)
    {
        var course = new Course(row.Id, SemanticKey.Of(row.TitleKey));
        foreach (var unitRow in row.Units.OrderBy(u => u.Ordinal))
        {
            var unit = new SkillUnit(unitRow.Id, SemanticKey.Of(unitRow.TitleKey));
            foreach (var lessonRow in unitRow.Lessons.OrderBy(l => l.Ordinal))
            {
                var lesson = new Lesson(lessonRow.Id, SemanticKey.Of(lessonRow.TitleKey));
                foreach (var exercise in lessonRow.Exercises.OrderBy(e => e.Ordinal))
                    lesson.Content.AddExercise(exercise.ExerciseInstanceId, exercise.ExerciseTypeKey);
                unit.AddLesson(lesson);
            }
            course.AddSkillUnit(unit);
        }
        return course;
    }
}

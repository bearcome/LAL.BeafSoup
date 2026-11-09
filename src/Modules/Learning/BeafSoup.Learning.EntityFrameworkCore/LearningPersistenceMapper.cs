using BeafSoup.SharedKernel;

namespace BeafSoup.Learning.EntityFrameworkCore;

/// <summary>Learning 领域对象与持久化行之间的显式映射；仓储与种子管线共用，保证读写对称。</summary>
public static class LearningPersistenceMapper
{
    public static LearnerProfileRow ToRow(LearnerProfile profile)
        => new()
        {
            Id = profile.Id.ToString(),
            UserId = profile.UserId.ToString(),
            BaseLanguage = profile.BaseLanguage.Value,
            TargetLanguage = profile.TargetLanguage.Value,
            TargetRetentionRate = profile.TargetRetentionRate,
            ThemePreference = profile.ThemePreference,
            InflectionDisplay = profile.InflectionDisplay,
            InspectorTrigger = profile.InspectorTrigger,
        };

    public static LearnerProfile ToDomain(LearnerProfileRow row)
    {
        var profile = LearnerProfile.Create(
            Guid.Parse(row.Id), Guid.Parse(row.UserId),
            LanguageCode.Of(row.BaseLanguage), LanguageCode.Of(row.TargetLanguage), row.TargetRetentionRate);
        profile.ChangeTheme(row.ThemePreference);
        profile.ChangeInflectionDisplay(row.InflectionDisplay);
        profile.ChangeInspectorTrigger(row.InspectorTrigger);
        return profile;
    }

    public static ReviewCardRow ToRow(ReviewCard card)
        => new()
        {
            ProfileId = card.LearnerProfileId.ToString(),
            ConceptId = card.ConceptId.ToString(),
            Status = card.Fsrs.Status.ToString(),
            DueUtc = card.Fsrs.DueUtc,
            Stability = card.Fsrs.Stability,
            Difficulty = card.Fsrs.Difficulty,
            SchedulingVersion = card.Fsrs.SchedulingVersion,
        };

    public static ReviewCard ToDomain(ReviewCardRow row)
        => ReviewCard.Restore(
            Guid.Parse(row.ProfileId), Guid.Parse(row.ConceptId),
            FsrsCardState.Restore(
                Enum.Parse<FsrsCardStatus>(row.Status), row.DueUtc,
                row.Stability, row.Difficulty, row.SchedulingVersion));

    public static FormMasteryRow ToRow(FormMastery formMastery)
        => new()
        {
            ProfileId = formMastery.LearnerProfileId.ToString(),
            CellId = formMastery.CellId.ToString(),
            CorrectCount = formMastery.CorrectCount,
            IncorrectCount = formMastery.IncorrectCount,
            LastSeenAtUtc = formMastery.LastSeenAtUtc,
        };

    public static FormMastery ToDomain(FormMasteryRow row)
    {
        var mastery = new FormMastery(Guid.Parse(row.ProfileId), Guid.Parse(row.CellId));
        for (var i = 0; i < row.CorrectCount; i++)
            mastery.RecordOutcome(correct: true, row.LastSeenAtUtc ?? DateTime.UnixEpoch);
        for (var i = 0; i < row.IncorrectCount; i++)
            mastery.RecordOutcome(correct: false, row.LastSeenAtUtc ?? DateTime.UnixEpoch);
        return mastery;
    }

    public static LearningActivityRow ToRow(LearningActivity activity)
        => new()
        {
            Id = activity.Id.ToString(),
            DedupeKey = activity.DedupeKey,
            ProfileId = activity.LearnerProfileId.ToString(),
            Type = activity.Type.ToString(),
            OccurredAtUtc = activity.OccurredAtUtc,
            ExerciseInstanceId = activity.ExerciseInstanceId?.ToString(),
            LessonId = activity.LessonId?.ToString(),
            IsCorrect = activity.IsCorrect,
        };

    public static LearningActivity ToDomain(LearningActivityRow row)
        => LearningActivity.Create(
            Guid.Parse(row.Id), row.DedupeKey, Guid.Parse(row.ProfileId),
            Enum.Parse<LearningActivityType>(row.Type), row.OccurredAtUtc,
            row.ExerciseInstanceId is null ? null : Guid.Parse(row.ExerciseInstanceId),
            row.LessonId is null ? null : Guid.Parse(row.LessonId),
            row.IsCorrect);

    public static LearnerProgressStateRow ToRow(LearnerProgressState state)
        => new()
        {
            ProfileId = state.LearnerProfileId.ToString(),
            Hearts = state.Hearts,
            LastHeartChangeAtUtc = state.LastHeartChangeAtUtc,
            LastActivityDateUtc = state.LastActivityDateUtc,
            DayStreak = state.DayStreak,
        };

    public static LearnerProgressState ToDomain(LearnerProgressStateRow row)
        => LearnerProgressState.Restore(
            Guid.Parse(row.ProfileId), row.Hearts, row.LastHeartChangeAtUtc,
            row.LastActivityDateUtc, row.DayStreak);
}

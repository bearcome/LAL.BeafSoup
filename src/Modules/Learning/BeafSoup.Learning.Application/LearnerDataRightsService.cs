namespace BeafSoup.Learning;

/// <summary>
/// 学习者数据权利实现（ADR-008）：导出本人学习数据快照，账户注销时擦除学习配置及其全部学习事实。
/// 删除顺序先依赖行、后配置行；操作幂等，重复执行不会报错。
/// </summary>
public sealed class LearnerDataRightsService : ILearnerDataExportQuery, IAccountDataEraser
{
    private readonly ILearningProfileRepository _profiles;
    private readonly IReviewCardRepository _cards;
    private readonly IFormMasteryRepository _formMastery;
    private readonly ILearningActivityRepository _activities;
    private readonly ILearnerProgressStateRepository _progress;

    public LearnerDataRightsService(
        ILearningProfileRepository profiles,
        IReviewCardRepository cards,
        IFormMasteryRepository formMastery,
        ILearningActivityRepository activities,
        ILearnerProgressStateRepository progress)
    {
        _profiles = profiles;
        _cards = cards;
        _formMastery = formMastery;
        _activities = activities;
        _progress = progress;
    }

    public async Task<LearnerDataExportDto> ExportAsync(Guid userId, CancellationToken cancellationToken)
    {
        var profiles = await _profiles.FindByUserAsync(userId, cancellationToken);
        var activities = new List<LearningActivityExportDto>();
        var reviewCards = new List<ReviewCardExportDto>();
        var mastery = new List<FormMasteryExportDto>();

        foreach (var profile in profiles)
        {
            activities.AddRange((await _activities.GetByProfileAsync(profile.Id, cancellationToken))
                .Select(a => new LearningActivityExportDto(
                    a.Id, a.Type.ToString(), a.DedupeKey, a.ExerciseInstanceId, a.LessonId,
                    a.IsCorrect, a.OccurredAtUtc)));
            reviewCards.AddRange((await _cards.GetByProfileAsync(profile.Id, cancellationToken))
                .Select(c => new ReviewCardExportDto(
                    c.ConceptId, c.Fsrs.Status.ToString(), c.Fsrs.DueUtc,
                    c.Fsrs.Stability, c.Fsrs.Difficulty, c.Fsrs.SchedulingVersion)));
            mastery.AddRange((await _formMastery.GetByProfileAsync(profile.Id, cancellationToken))
                .Select(m => new FormMasteryExportDto(
                    m.CellId, m.CorrectCount, m.IncorrectCount, m.LastSeenAtUtc)));
        }

        return new LearnerDataExportDto(
            profiles.Select(p => new LearnerProfileExportDto(
                p.Id, p.BaseLanguage.Value, p.TargetLanguage.Value, p.TargetRetentionRate,
                p.ThemePreference, p.InflectionDisplay, p.InspectorTrigger)).ToArray(),
            activities.OrderBy(a => a.OccurredAtUtc).ToArray(),
            reviewCards.OrderBy(c => c.ConceptId).ToArray(),
            mastery.OrderBy(m => m.CellId).ToArray());
    }

    public async Task EraseAsync(Guid userId, CancellationToken cancellationToken)
    {
        var profiles = await _profiles.FindByUserAsync(userId, cancellationToken);
        if (profiles.Count == 0)
            return;
        var profileIds = profiles.Select(p => p.Id).ToArray();

        await _activities.DeleteByProfilesAsync(profileIds, cancellationToken);
        await _cards.DeleteByProfilesAsync(profileIds, cancellationToken);
        await _formMastery.DeleteByProfilesAsync(profileIds, cancellationToken);
        await _progress.DeleteByProfilesAsync(profileIds, cancellationToken);
        foreach (var profile in profiles)
            await _profiles.DeleteAsync(profile.Id, cancellationToken);
    }
}

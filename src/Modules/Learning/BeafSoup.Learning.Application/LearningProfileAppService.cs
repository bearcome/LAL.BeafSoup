using Microsoft.Extensions.Options;

namespace BeafSoup.Learning;

/// <summary>进度快照组合：唯一把活动投影与游戏化状态合成快照的地方，保证各端点口径一致。</summary>
public static class ProgressSnapshotFactory
{
    public static ProgressSnapshotDto Create(
        Guid learnerProfileId,
        IReadOnlyList<LearningActivity> activities,
        LearnerProgressState? state,
        LearningGamificationOptions options,
        DateTime nowUtc)
    {
        var record = ProgressRecord.FromActivities(learnerProfileId, activities,
            new Dictionary<LearningActivityType, int>
            {
                [LearningActivityType.GradedAnswer] = options.XpPerGradedAnswer,
                [LearningActivityType.LessonCompleted] = options.XpPerLessonCompleted,
                [LearningActivityType.ReviewSubmitted] = options.XpPerReviewSubmitted,
            });
        state?.ApplyRegen(nowUtc);
        return new ProgressSnapshotDto(
            learnerProfileId,
            record.Xp,
            record.CompletedLessons,
            state?.Hearts ?? LearnerProgressState.MaxHearts,
            state?.DayStreak ?? 0,
            state?.LastActivityDateUtc?.Date == nowUtc.Date);
    }
}

public sealed class LearningProfileAppService : ILearningProfileAppService
{
    private readonly ILearningProfileRepository _profiles;
    private readonly ILearnerProgressStateRepository _states;
    private readonly TimeProvider _timeProvider;

    public LearningProfileAppService(
        ILearningProfileRepository profiles,
        ILearnerProgressStateRepository states,
        TimeProvider timeProvider)
    {
        _profiles = profiles;
        _states = states;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<LearnerProfileDto>> GetMineAsync(Guid userId, CancellationToken cancellationToken)
        => (await _profiles.FindByUserAsync(userId, cancellationToken)).Select(Map).ToArray();

    public async Task<LearnerProfileDto> CreateAsync(Guid userId, CreateLearnerProfileRequest request, CancellationToken cancellationToken)
    {
        var profile = LearnerProfile.Create(
            Guid.NewGuid(), userId,
            SharedKernel.LanguageCode.Of(request.BaseLanguage),
            SharedKernel.LanguageCode.Of(request.TargetLanguage),
            targetRetentionRate: 0.9);
        await _profiles.InsertAsync(profile, cancellationToken);
        // 游戏化状态与配置同事务创建，快照口径自洽
        await _states.InsertAsync(
            LearnerProgressState.Create(profile.Id, _timeProvider.GetUtcNow().UtcDateTime), cancellationToken);
        await _profiles.SaveChangesAsync(cancellationToken);
        return Map(profile);
    }

    public async Task<LearnerProfileDto> UpdatePreferencesAsync(
        Guid profileId, UpdateProfilePreferencesRequest request, CancellationToken cancellationToken)
    {
        var profile = await _profiles.FindAsync(profileId, cancellationToken)
            ?? throw new SharedKernel.DomainException(LearningErrorCodes.EmptyDedupeKey, $"学习配置 {profileId} 不存在。");
        if (request.Theme is not null)
            profile.ChangeTheme(request.Theme);
        if (request.TargetRetentionRate is not null)
            profile.ChangeTargetRetentionRate(request.TargetRetentionRate.Value);
        if (request.InflectionDisplay is not null)
            profile.ChangeInflectionDisplay(request.InflectionDisplay);
        if (request.InspectorTrigger is not null)
            profile.ChangeInspectorTrigger(request.InspectorTrigger);
        await _profiles.UpdateAsync(profile, cancellationToken);
        await _profiles.SaveChangesAsync(cancellationToken);
        return Map(profile);
    }

    private static LearnerProfileDto Map(LearnerProfile profile)
        => new(profile.Id, profile.BaseLanguage.Value, profile.TargetLanguage.Value,
            profile.TargetRetentionRate, profile.ThemePreference, profile.InflectionDisplay, profile.InspectorTrigger);
}

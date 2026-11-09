using Microsoft.Extensions.Options;

namespace BeafSoup.Learning;

/// <summary>学习活动应用服务：幂等提交（ActivityId 与按配置唯一的 DedupeKey 双重去重），进度/红心/连胜不重复变更。</summary>
public sealed class LearningActivityAppService : ILearningActivityAppService
{
    private readonly ILearningActivityRepository _activities;
    private readonly ILearningProfileRepository _profiles;
    private readonly ILearnerProgressStateRepository _states;
    private readonly LearningGamificationOptions _options;
    private readonly TimeProvider _timeProvider;

    public LearningActivityAppService(
        ILearningActivityRepository activities,
        ILearningProfileRepository profiles,
        ILearnerProgressStateRepository states,
        IOptions<LearningGamificationOptions> options,
        TimeProvider timeProvider)
    {
        _activities = activities;
        _profiles = profiles;
        _states = states;
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    public async Task<SubmitActivityResponse> SubmitAsync(
        Guid learnerProfileId, SubmitActivityRequest request, CancellationToken cancellationToken)
    {
        var profile = await _profiles.FindAsync(learnerProfileId, cancellationToken)
            ?? throw new SharedKernel.DomainException(LearningErrorCodes.EmptyDedupeKey, $"学习配置 {learnerProfileId} 不存在。");

        // 幂等：同一 ActivityId 重试（客户端超时重放）返回既有结果，不重复计分
        var byId = await _activities.FindAsync(request.ActivityId, cancellationToken);
        if (byId is not null)
            return new(false, await GetProgressAsync(learnerProfileId, cancellationToken));
        var byDedupeKey = await _activities.FindByDedupeKeyAsync(learnerProfileId, request.DedupeKey, cancellationToken);
        if (byDedupeKey is not null)
            return new(false, await GetProgressAsync(learnerProfileId, cancellationToken));

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var activity = LearningActivity.Create(
            request.ActivityId, request.DedupeKey, learnerProfileId, request.Type, now,
            request.ExerciseInstanceId, request.LessonId, request.IsCorrect);
        await _activities.InsertAsync(activity, cancellationToken);

        var state = await _states.FindAsync(learnerProfileId, cancellationToken)
            ?? LearnerProgressState.Create(learnerProfileId, now);
        if (request.IsCorrect)
            state.RecordActivity(now);
        else
            state.RecordMistake(now);
        if (await _states.FindAsync(learnerProfileId, cancellationToken) is null)
            await _states.InsertAsync(state, cancellationToken);
        else
            await _states.UpdateAsync(state, cancellationToken);

        await _activities.SaveChangesAsync(cancellationToken);
        return new(true, await GetProgressAsync(learnerProfileId, cancellationToken));
    }

    private async Task<ProgressSnapshotDto> GetProgressAsync(Guid learnerProfileId, CancellationToken cancellationToken)
    {
        var activities = await _activities.GetByProfileAsync(learnerProfileId, cancellationToken);
        var state = await _states.FindAsync(learnerProfileId, cancellationToken);
        return ProgressSnapshotFactory.Create(
            learnerProfileId, activities, state, _options, _timeProvider.GetUtcNow().UtcDateTime);
    }
}
